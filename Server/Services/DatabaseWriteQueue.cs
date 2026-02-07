/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System.Collections.Concurrent;
using System.Threading.Channels;
using Server.Exceptions;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Interface for managing database write operations through a queue.
    /// Provides FIFO processing of write operations to prevent race conditions.
    /// </summary>
    /// <remarks>
    /// Validates: Requirement 5.1 - THE Concurrency_Controller SHALL implement a write queue for critical database operations
    /// Validates: Requirement 5.2 - WHEN multiple write requests arrive for the same entity, THE Concurrency_Controller SHALL process them in order
    /// </remarks>
    public interface IDatabaseWriteQueue
    {
        /// <summary>
        /// Enqueues a write operation for processing.
        /// </summary>
        /// <param name="operation">The write operation to enqueue.</param>
        /// <returns>A result containing the operation ID and queue position.</returns>
        /// <exception cref="ServiceUnavailableException">Thrown when the queue is full.</exception>
        Task<QueuedOperationResult> EnqueueAsync(WriteOperation operation);

        /// <summary>
        /// Enqueues a write operation and waits for it to complete.
        /// </summary>
        /// <param name="operation">The write operation to enqueue.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        /// <exception cref="ServiceUnavailableException">Thrown when the queue is full.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <remarks>
        /// Validates: Requirement 5.4 - IF a queued operation fails, THEN THE Concurrency_Controller SHALL notify the client and not block subsequent operations
        /// Validates: Requirement 5.5 - THE Concurrency_Controller SHALL implement timeout handling for queued operations
        /// </remarks>
        Task EnqueueAndWaitAsync(WriteOperation operation);

        /// <summary>
        /// Gets the current length of the write queue.
        /// </summary>
        /// <returns>The number of operations waiting in the queue.</returns>
        Task<int> GetQueueLengthAsync();

        /// <summary>
        /// Gets the position of a specific operation in the queue.
        /// </summary>
        /// <param name="operationId">The ID of the operation to find.</param>
        /// <returns>The position information for the operation.</returns>
        Task<QueuePosition> GetPositionAsync(string operationId);
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a write operation to be queued for processing.
    /// </summary>
    public class WriteOperation
    {
        /// <summary>
        /// Unique identifier for this operation.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The type of entity being modified (e.g., "InspectionProtocol", "CropSprayer").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the entity being modified.
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// The async operation to execute. Accepts a CancellationToken for timeout handling.
        /// </summary>
        /// <remarks>
        /// Validates: Requirement 5.5 - THE Concurrency_Controller SHALL implement timeout handling for queued operations
        /// The operation should respect the cancellation token to allow proper timeout handling.
        /// </remarks>
        public Func<CancellationToken, Task> Operation { get; set; } = (_) => Task.CompletedTask;

        /// <summary>
        /// The time when this operation was enqueued.
        /// </summary>
        public DateTime EnqueuedAt { get; set; }

        /// <summary>
        /// The maximum time to wait for this operation to complete.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Task completion source for signaling operation completion.
        /// </summary>
        internal TaskCompletionSource<bool> CompletionSource { get; set; } = new();
    }

    /// <summary>
    /// Result returned when an operation is successfully enqueued.
    /// </summary>
    public class QueuedOperationResult
    {
        /// <summary>
        /// The unique identifier assigned to the operation.
        /// </summary>
        public string OperationId { get; set; } = string.Empty;

        /// <summary>
        /// The position of the operation in the queue (1-based).
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// Estimated time until the operation will be processed.
        /// </summary>
        public TimeSpan EstimatedWaitTime { get; set; }
    }

    /// <summary>
    /// Information about an operation's position in the queue.
    /// </summary>
    public class QueuePosition
    {
        /// <summary>
        /// The unique identifier of the operation.
        /// </summary>
        public string OperationId { get; set; } = string.Empty;

        /// <summary>
        /// The current position in the queue (1-based), or 0 if not found.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Whether the operation was found in the queue.
        /// </summary>
        public bool Found { get; set; }

        /// <summary>
        /// Whether the operation has been completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Estimated time until the operation will be processed.
        /// </summary>
        public TimeSpan EstimatedWaitTime { get; set; }
    }

    #endregion

    #region Implementation

    /// <summary>
    /// Implements a database write queue using System.Threading.Channels for bounded, FIFO processing.
    /// Runs as a hosted service to process operations in the background.
    /// </summary>
    /// <remarks>
    /// Validates: Requirement 5.1 - THE Concurrency_Controller SHALL implement a write queue for critical database operations
    /// Validates: Requirement 5.2 - WHEN multiple write requests arrive for the same entity, THE Concurrency_Controller SHALL process them in order
    /// 
    /// Implementation details:
    /// - Uses Channel&lt;WriteOperation&gt; for thread-safe, bounded queue
    /// - Queue capacity is configurable (default: 100 operations)
    /// - Operations are processed in FIFO order
    /// - Failed operations do not block subsequent operations
    /// </remarks>
    public class DatabaseWriteQueue : IDatabaseWriteQueue, IHostedService
    {
        #region Declarations

        private readonly Channel<WriteOperation> _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseWriteQueue> _logger;
        private readonly IConfiguration _configuration;
        private Task? _processingTask;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Default queue capacity if not configured.
        /// </summary>
        private const int DefaultQueueCapacity = 100;

        /// <summary>
        /// Average time per operation for estimating wait times.
        /// </summary>
        private static readonly TimeSpan AverageOperationTime = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Tracks pending operations for position lookup.
        /// </summary>
        private readonly ConcurrentDictionary<string, WriteOperation> _pendingOperations = new();

        /// <summary>
        /// Tracks completed operation IDs for a short period.
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _completedOperations = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DatabaseWriteQueue.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="configuration">Application configuration.</param>
        public DatabaseWriteQueue(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseWriteQueue> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;

            // Get queue capacity from configuration
            var queueCapacity = _configuration.GetValue<int>(
                "WriteQueue:Capacity",
                DefaultQueueCapacity);

            // Create bounded channel with configured capacity
            // FullMode.Wait means EnqueueAsync will wait if queue is full
            _queue = Channel.CreateBounded<WriteOperation>(new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,  // Only one consumer (the processing task)
                SingleWriter = false  // Multiple producers can enqueue
            });

            _logger.LogInformation(
                "DatabaseWriteQueue initialized with capacity of {Capacity} operations",
                queueCapacity);
        }

        #endregion

        #region IHostedService Implementation

        /// <summary>
        /// Starts the background processing task.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for startup.</param>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DatabaseWriteQueue service is starting");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _processingTask = ProcessQueueAsync(_cts.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the background processing task gracefully.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for shutdown.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DatabaseWriteQueue service is stopping");

            // Signal the processing task to stop
            _cts?.Cancel();

            // Complete the channel writer to signal no more items
            _queue.Writer.Complete();

            // Wait for the processing task to complete
            if (_processingTask != null)
            {
                try
                {
                    await _processingTask.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _logger.LogInformation("DatabaseWriteQueue service has stopped");
        }

        #endregion

        #region IDatabaseWriteQueue Implementation

        /// <summary>
        /// Enqueues a write operation for processing.
        /// </summary>
        /// <param name="operation">The write operation to enqueue.</param>
        /// <returns>A result containing the operation ID and queue position.</returns>
        /// <exception cref="ServiceUnavailableException">Thrown when the queue is full and cannot accept more operations.</exception>
        public async Task<QueuedOperationResult> EnqueueAsync(WriteOperation operation)
        {
            // Generate unique ID if not provided
            if (string.IsNullOrEmpty(operation.Id))
            {
                operation.Id = Guid.NewGuid().ToString();
            }

            operation.EnqueuedAt = DateTime.UtcNow;
            operation.CompletionSource = new TaskCompletionSource<bool>();

            // Track the operation for position lookup
            _pendingOperations[operation.Id] = operation;

            _logger.LogDebug(
                "Enqueuing write operation {OperationId} for {EntityType}/{EntityId}",
                operation.Id,
                operation.EntityType,
                operation.EntityId);

            // Try to write to the channel
            // Using TryWrite first to check if queue is full
            if (!_queue.Writer.TryWrite(operation))
            {
                // Queue is full, remove from pending and throw
                _pendingOperations.TryRemove(operation.Id, out _);

                _logger.LogWarning(
                    "Write queue is full. Rejecting operation {OperationId} for {EntityType}/{EntityId}",
                    operation.Id,
                    operation.EntityType,
                    operation.EntityId);

                throw new ServiceUnavailableException(
                    "Write queue is full. Please try again later.");
            }

            // Calculate queue position and estimated wait time
            var queueLength = _queue.Reader.Count;
            var estimatedWaitTime = TimeSpan.FromMilliseconds(queueLength * AverageOperationTime.TotalMilliseconds);

            _logger.LogInformation(
                "Enqueued write operation {OperationId} for {EntityType}/{EntityId}. Queue position: {Position}",
                operation.Id,
                operation.EntityType,
                operation.EntityId,
                queueLength);

            return new QueuedOperationResult
            {
                OperationId = operation.Id,
                QueuePosition = queueLength,
                EstimatedWaitTime = estimatedWaitTime
            };
        }

        /// <summary>
        /// Enqueues a write operation and waits for it to complete.
        /// </summary>
        /// <param name="operation">The write operation to enqueue.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        /// <exception cref="ServiceUnavailableException">Thrown when the queue is full.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <remarks>
        /// Validates: Requirement 5.4 - IF a queued operation fails, THEN THE Concurrency_Controller SHALL notify the client and not block subsequent operations
        /// Validates: Requirement 5.5 - THE Concurrency_Controller SHALL implement timeout handling for queued operations
        /// Validates: Requirement 5.6 - WHEN the queue exceeds a configurable threshold, THE Backend SHALL return a 503 Service Unavailable response
        /// </remarks>
        public async Task EnqueueAndWaitAsync(WriteOperation operation)
        {
            // Enqueue the operation (throws ServiceUnavailableException if queue is full)
            await EnqueueAsync(operation);

            // Wait for the operation to complete
            // The CompletionSource will be set by ProcessOperationAsync
            try
            {
                await operation.CompletionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Write operation {OperationId} completed with error",
                    operation.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets the current length of the write queue.
        /// </summary>
        /// <returns>The number of operations waiting in the queue.</returns>
        public Task<int> GetQueueLengthAsync()
        {
            return Task.FromResult(_queue.Reader.Count);
        }

        /// <summary>
        /// Gets the position of a specific operation in the queue.
        /// </summary>
        /// <param name="operationId">The ID of the operation to find.</param>
        /// <returns>The position information for the operation.</returns>
        public Task<QueuePosition> GetPositionAsync(string operationId)
        {
            // Check if operation was completed
            if (_completedOperations.ContainsKey(operationId))
            {
                return Task.FromResult(new QueuePosition
                {
                    OperationId = operationId,
                    Position = 0,
                    Found = true,
                    IsCompleted = true,
                    EstimatedWaitTime = TimeSpan.Zero
                });
            }

            // Check if operation is pending
            if (_pendingOperations.TryGetValue(operationId, out var operation))
            {
                // Estimate position based on enqueue time
                // This is an approximation since we can't easily get exact position in channel
                var pendingCount = _pendingOperations.Count;
                var position = _pendingOperations.Values
                    .Where(op => op.EnqueuedAt <= operation.EnqueuedAt)
                    .Count();

                var estimatedWaitTime = TimeSpan.FromMilliseconds(position * AverageOperationTime.TotalMilliseconds);

                return Task.FromResult(new QueuePosition
                {
                    OperationId = operationId,
                    Position = position,
                    Found = true,
                    IsCompleted = false,
                    EstimatedWaitTime = estimatedWaitTime
                });
            }

            // Operation not found
            return Task.FromResult(new QueuePosition
            {
                OperationId = operationId,
                Position = 0,
                Found = false,
                IsCompleted = false,
                EstimatedWaitTime = TimeSpan.Zero
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Background task that processes operations from the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop processing.</param>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Write queue processing started");

            try
            {
                await foreach (var operation in _queue.Reader.ReadAllAsync(cancellationToken))
                {
                    await ProcessOperationAsync(operation, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Write queue processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in write queue processing");
            }

            _logger.LogInformation("Write queue processing stopped");
        }

        /// <summary>
        /// Processes a single write operation.
        /// </summary>
        /// <param name="operation">The operation to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Validates: Requirement 5.4 - IF a queued operation fails, THEN THE Concurrency_Controller SHALL notify the client and not block subsequent operations
        /// Validates: Requirement 5.5 - THE Concurrency_Controller SHALL implement timeout handling for queued operations
        /// </remarks>
        private async Task ProcessOperationAsync(WriteOperation operation, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogDebug(
                "Processing write operation {OperationId} for {EntityType}/{EntityId}",
                operation.Id,
                operation.EntityType,
                operation.EntityId);

            try
            {
                // Create a timeout cancellation token linked to the service cancellation token
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(operation.Timeout);

                // Create a scope for the operation
                using var scope = _scopeFactory.CreateScope();

                // Execute the operation with timeout enforcement
                // Use Task.WhenAny to enforce timeout even if operation doesn't respect cancellation token
                var operationTask = operation.Operation(timeoutCts.Token);
                var timeoutTask = Task.Delay(operation.Timeout, timeoutCts.Token);

                var completedTask = await Task.WhenAny(operationTask, timeoutTask);

                if (completedTask == timeoutTask && !operationTask.IsCompleted)
                {
                    // Timeout occurred before operation completed
                    _logger.LogWarning(
                        "Write operation {OperationId} for {EntityType}/{EntityId} timed out after {Timeout}ms",
                        operation.Id,
                        operation.EntityType,
                        operation.EntityId,
                        operation.Timeout.TotalMilliseconds);

                    operation.CompletionSource.TrySetException(
                        new TimeoutException($"Write operation timed out after {operation.Timeout.TotalMilliseconds}ms"));
                    return;
                }

                // Await the operation to propagate any exceptions
                await operationTask;

                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Processed write operation {OperationId} for {EntityType}/{EntityId} in {Duration}ms",
                    operation.Id,
                    operation.EntityType,
                    operation.EntityId,
                    duration.TotalMilliseconds);

                // Signal completion
                operation.CompletionSource.TrySetResult(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Service is shutting down
                _logger.LogWarning(
                    "Write operation {OperationId} cancelled due to service shutdown",
                    operation.Id);

                operation.CompletionSource.TrySetCanceled(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Operation timed out (cancellation token was triggered by timeout)
                _logger.LogWarning(
                    "Write operation {OperationId} for {EntityType}/{EntityId} timed out after {Timeout}ms",
                    operation.Id,
                    operation.EntityType,
                    operation.EntityId,
                    operation.Timeout.TotalMilliseconds);

                operation.CompletionSource.TrySetException(
                    new TimeoutException($"Write operation timed out after {operation.Timeout.TotalMilliseconds}ms"));
            }
            catch (Exception ex)
            {
                // Operation failed - log but don't block queue
                // Requirement 5.4: IF a queued operation fails, THEN THE Concurrency_Controller SHALL notify the client and not block subsequent operations
                _logger.LogError(
                    ex,
                    "Failed to process write operation {OperationId} for {EntityType}/{EntityId}",
                    operation.Id,
                    operation.EntityType,
                    operation.EntityId);

                operation.CompletionSource.TrySetException(ex);
            }
            finally
            {
                // Remove from pending and add to completed
                _pendingOperations.TryRemove(operation.Id, out _);
                _completedOperations[operation.Id] = DateTime.UtcNow;

                // Clean up old completed operations (keep for 5 minutes)
                CleanupCompletedOperations();
            }
        }

        /// <summary>
        /// Removes old entries from the completed operations dictionary.
        /// </summary>
        private void CleanupCompletedOperations()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var oldOperations = _completedOperations
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var operationId in oldOperations)
            {
                _completedOperations.TryRemove(operationId, out _);
            }
        }

        #endregion
    }

    #endregion
}
