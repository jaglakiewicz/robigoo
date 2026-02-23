#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Exceptions;
using System.Collections.Concurrent;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Interface for managing database concurrency control.
    /// Provides optimistic concurrency with retry logic and entity-level locking.
    /// </summary>
    public interface IConcurrencyController
    {
        /// <summary>
        /// Executes an operation with entity-level locking.
        /// Acquires a lock for the specified resource key before executing the operation.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="resourceKey">A unique key identifying the resource to lock (e.g., "CropSprayer:ABC123").</param>
        /// <param name="operation">The async operation to execute while holding the lock.</param>
        /// <param name="timeout">Maximum time to wait for acquiring the lock.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TimeoutException">Thrown when the lock cannot be acquired within the timeout period.</exception>
        Task<T> ExecuteWithLockAsync<T>(string resourceKey, Func<Task<T>> operation, TimeSpan timeout);

        /// <summary>
        /// Enqueues a write operation for sequential processing.
        /// Operations for the same entity are processed in order.
        /// </summary>
        /// <param name="entityType">The type of entity being modified.</param>
        /// <param name="entityId">The unique identifier of the entity.</param>
        /// <param name="operation">The async operation to execute.</param>
        Task EnqueueWriteOperationAsync(string entityType, string entityId, Func<Task> operation);

        /// <summary>
        /// Executes an operation with optimistic concurrency control and automatic retry logic.
        /// Implements exponential backoff (100ms, 200ms, 400ms) for transient failures.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <returns>A ConcurrencyResult containing the result or conflict information.</returns>
        Task<ConcurrencyResult<T>> ExecuteWithOptimisticConcurrencyAsync<T>(Func<Task<T>> operation) where T : class;
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// Represents the result of an operation executed with optimistic concurrency control.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    public class ConcurrencyResult<T>
    {
        /// <summary>
        /// Indicates whether the operation completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result of the operation if successful.
        /// </summary>
        public T? Result { get; set; }

        /// <summary>
        /// Details about the concurrency conflict if the operation failed.
        /// </summary>
        public ConcurrencyConflict? Conflict { get; set; }
    }

    /// <summary>
    /// Contains details about a concurrency conflict.
    /// </summary>
    public class ConcurrencyConflict
    {
        /// <summary>
        /// The type of entity that had the conflict.
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// The identifier of the entity that had the conflict.
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// The time when the conflict was detected.
        /// </summary>
        public DateTime ConflictTime { get; set; }

        /// <summary>
        /// A user-friendly message describing the conflict.
        /// </summary>
        public string? Message { get; set; }
    }

    #endregion

    #region Implementation

    /// <summary>
    /// Implements concurrency control for database operations.
    /// Provides optimistic concurrency with retry logic and entity-level locking.
    /// 
    /// Requirements:
    /// - 4.2: WHEN two users attempt to modify the same record simultaneously, THE Concurrency_Controller SHALL detect the conflict
    /// - 4.3: IF a concurrency conflict is detected, THEN THE Backend SHALL return a 409 Conflict response with details
    /// - 4.5: THE Concurrency_Controller SHALL implement retry logic with exponential backoff for transient failures
    /// </summary>
    public class ConcurrencyController : IConcurrencyController
    {
        #region Declarations

        private readonly ILogger<ConcurrencyController> _logger;
        
        /// <summary>
        /// Dictionary of entity-level locks, keyed by resource key.
        /// Each lock is a SemaphoreSlim that allows only one operation at a time.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _entityLocks = new();

        /// <summary>
        /// Maximum number of retry attempts for optimistic concurrency operations.
        /// </summary>
        private const int MaxRetries = 3;

        /// <summary>
        /// Initial delay for exponential backoff (100ms).
        /// </summary>
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(100);

        #endregion

        #region Constructor

        public ConcurrencyController(ILogger<ConcurrencyController> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Executes an operation with optimistic concurrency control and automatic retry logic.
        /// Implements exponential backoff (100ms, 200ms, 400ms) for transient failures.
        /// 
        /// Requirement 4.5: THE Concurrency_Controller SHALL implement retry logic with exponential backoff for transient failures
        /// </summary>
        public async Task<ConcurrencyResult<T>> ExecuteWithOptimisticConcurrencyAsync<T>(
            Func<Task<T>> operation) where T : class
        {
            var delay = InitialDelay;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var result = await operation();
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "Operation succeeded after {Attempt} retry attempts",
                            attempt);
                    }

                    return new ConcurrencyResult<T>
                    {
                        Success = true,
                        Result = result
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Requirement 4.2: Detect the conflict
                    _logger.LogWarning(
                        ex,
                        "Concurrency conflict detected on attempt {Attempt} of {MaxRetries}. Entity: {EntityType}",
                        attempt + 1,
                        MaxRetries,
                        GetEntityTypeFromException(ex));

                    // If this was the last attempt, return failure with conflict details
                    if (attempt == MaxRetries - 1)
                    {
                        // Requirement 4.3: Return conflict details
                        return new ConcurrencyResult<T>
                        {
                            Success = false,
                            Conflict = new ConcurrencyConflict
                            {
                                EntityType = GetEntityTypeFromException(ex),
                                EntityId = GetEntityIdFromException(ex),
                                Message = "The record was modified by another user. Please refresh and try again.",
                                ConflictTime = DateTime.UtcNow
                            }
                        };
                    }

                    // Exponential backoff: 100ms, 200ms, 400ms
                    _logger.LogDebug(
                        "Waiting {Delay}ms before retry attempt {NextAttempt}",
                        delay.TotalMilliseconds,
                        attempt + 2);

                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                }
                catch (ConcurrencyException ex)
                {
                    // Handle our custom ConcurrencyException (thrown by AppDbContext)
                    _logger.LogWarning(
                        ex,
                        "Concurrency conflict detected (ConcurrencyException) on attempt {Attempt} of {MaxRetries}",
                        attempt + 1,
                        MaxRetries);

                    if (attempt == MaxRetries - 1)
                    {
                        return new ConcurrencyResult<T>
                        {
                            Success = false,
                            Conflict = new ConcurrencyConflict
                            {
                                EntityType = ex.EntityType,
                                EntityId = ex.EntityId,
                                Message = ex.Message,
                                ConflictTime = DateTime.UtcNow
                            }
                        };
                    }

                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                }
            }

            // This should not be reached, but return failure as a safety net
            return new ConcurrencyResult<T>
            {
                Success = false,
                Conflict = new ConcurrencyConflict
                {
                    Message = "Operation failed after maximum retry attempts.",
                    ConflictTime = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// Executes an operation with entity-level locking.
        /// Acquires a lock for the specified resource key before executing the operation.
        /// </summary>
        public async Task<T> ExecuteWithLockAsync<T>(
            string resourceKey,
            Func<Task<T>> operation,
            TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                throw new ArgumentException("Resource key cannot be null or empty.", nameof(resourceKey));
            }

            // Get or create a semaphore for this resource
            var entityLock = _entityLocks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));

            _logger.LogDebug(
                "Attempting to acquire lock for resource: {ResourceKey} with timeout: {Timeout}ms",
                resourceKey,
                timeout.TotalMilliseconds);

            // Try to acquire the lock within the timeout period
            if (!await entityLock.WaitAsync(timeout))
            {
                _logger.LogWarning(
                    "Failed to acquire lock for resource: {ResourceKey} within timeout: {Timeout}ms",
                    resourceKey,
                    timeout.TotalMilliseconds);

                throw new TimeoutException(
                    $"Could not acquire lock for resource '{resourceKey}' within {timeout.TotalMilliseconds}ms. " +
                    "The resource may be in use by another operation.");
            }

            try
            {
                _logger.LogDebug("Lock acquired for resource: {ResourceKey}", resourceKey);
                return await operation();
            }
            finally
            {
                entityLock.Release();
                _logger.LogDebug("Lock released for resource: {ResourceKey}", resourceKey);

                // Clean up unused locks to prevent memory leaks
                // Only remove if no one is waiting and the lock is not held
                if (entityLock.CurrentCount == 1)
                {
                    _entityLocks.TryRemove(resourceKey, out _);
                }
            }
        }

        /// <summary>
        /// Enqueues a write operation for sequential processing.
        /// Uses entity-level locking to ensure operations for the same entity are processed in order.
        /// </summary>
        public async Task EnqueueWriteOperationAsync(
            string entityType,
            string entityId,
            Func<Task> operation)
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type cannot be null or empty.", nameof(entityType));
            }

            if (string.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("Entity ID cannot be null or empty.", nameof(entityId));
            }

            var resourceKey = $"{entityType}:{entityId}";
            var timeout = TimeSpan.FromSeconds(30); // Default timeout for queued operations

            _logger.LogDebug(
                "Enqueuing write operation for {EntityType}:{EntityId}",
                entityType,
                entityId);

            await ExecuteWithLockAsync(
                resourceKey,
                async () =>
                {
                    await operation();
                    return true; // Return value is not used
                },
                timeout);
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Extracts the entity type from a DbUpdateConcurrencyException.
        /// </summary>
        private static string? GetEntityTypeFromException(DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            return entry?.Entity.GetType().Name;
        }

        /// <summary>
        /// Extracts the entity ID from a DbUpdateConcurrencyException.
        /// </summary>
        private static string? GetEntityIdFromException(DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            if (entry == null) return null;

            // Try to get the primary key value
            var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
            if (keyProperties == null || !keyProperties.Any()) return null;

            var keyValues = keyProperties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .Where(v => v != null);

            return string.Join(",", keyValues);
        }

        #endregion
    }

    #endregion
}
