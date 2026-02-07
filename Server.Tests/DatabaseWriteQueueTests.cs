/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Exceptions;
using Server.Services;

namespace Server.Tests
{
    /// <summary>
    /// Unit tests for DatabaseWriteQueue.
    /// Tests timeout handling, failure isolation, and queue full (503) behavior.
    /// 
    /// Validates: Requirements 5.4, 5.5, 5.6
    /// </summary>
    public class DatabaseWriteQueueTests : IAsyncLifetime
    {
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ILogger<DatabaseWriteQueue>> _loggerMock;
        private readonly IConfiguration _configuration;
        private DatabaseWriteQueue _queue = null!;

        public DatabaseWriteQueueTests()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loggerMock = new Mock<ILogger<DatabaseWriteQueue>>();

            // Set up scope factory to return a mock scope
            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

            // Use real configuration with in-memory values
            var configValues = new Dictionary<string, string?>
            {
                { "WriteQueue:Capacity", "5" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
        }

        public async Task InitializeAsync()
        {
            _queue = new DatabaseWriteQueue(
                _scopeFactoryMock.Object,
                _loggerMock.Object,
                _configuration);

            // Start the queue processing
            await _queue.StartAsync(CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            await _queue.StopAsync(CancellationToken.None);
        }

        /// <summary>
        /// Creates a DatabaseWriteQueue with a specific capacity for testing.
        /// </summary>
        private DatabaseWriteQueue CreateQueueWithCapacity(int capacity)
        {
            var configValues = new Dictionary<string, string?>
            {
                { "WriteQueue:Capacity", capacity.ToString() }
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            return new DatabaseWriteQueue(
                _scopeFactoryMock.Object,
                _loggerMock.Object,
                config);
        }

        #region Timeout Handling Tests (Requirement 5.5)

        /// <summary>
        /// Test: Operation that exceeds timeout is cancelled and TimeoutException is thrown.
        /// Validates: Requirement 5.5 - THE Concurrency_Controller SHALL implement timeout handling for queued operations
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_OperationExceedsTimeout_ThrowsTimeoutException()
        {
            // Arrange
            var operation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromMilliseconds(100), // Very short timeout
                Operation = async (ct) =>
                {
                    // Simulate a long-running operation that doesn't respect cancellation
                    await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _queue.EnqueueAndWaitAsync(operation));

            Assert.Contains("timed out", exception.Message);
        }

        /// <summary>
        /// Test: Operation that respects cancellation token is cancelled properly.
        /// Validates: Requirement 5.5
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_OperationRespectsCancellation_CancelsGracefully()
        {
            // Arrange
            var operationStarted = new TaskCompletionSource<bool>();
            var operation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromMilliseconds(100),
                Operation = async (ct) =>
                {
                    operationStarted.SetResult(true);
                    // This operation respects the cancellation token
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _queue.EnqueueAndWaitAsync(operation));

            Assert.Contains("timed out", exception.Message);
        }

        /// <summary>
        /// Test: Operation that completes within timeout succeeds.
        /// Validates: Requirement 5.5
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_OperationCompletesWithinTimeout_Succeeds()
        {
            // Arrange
            var operationExecuted = false;
            var operation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromSeconds(5),
                Operation = async (ct) =>
                {
                    await Task.Delay(10, ct); // Quick operation
                    operationExecuted = true;
                }
            };

            // Act
            await _queue.EnqueueAndWaitAsync(operation);

            // Assert
            Assert.True(operationExecuted);
        }

        #endregion

        #region Failure Isolation Tests (Requirement 5.4)

        /// <summary>
        /// Test: Failed operation notifies client with exception.
        /// Validates: Requirement 5.4 - IF a queued operation fails, THEN THE Concurrency_Controller SHALL notify the client
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_OperationFails_NotifiesClientWithException()
        {
            // Arrange
            var expectedMessage = "Test operation failure";
            var operation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromSeconds(5),
                Operation = (ct) => throw new InvalidOperationException(expectedMessage)
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _queue.EnqueueAndWaitAsync(operation));

            Assert.Equal(expectedMessage, exception.Message);
        }

        /// <summary>
        /// Test: Failed operation does not block subsequent operations.
        /// Validates: Requirement 5.4 - not block subsequent operations
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_FailedOperationDoesNotBlockQueue()
        {
            // Arrange
            var secondOperationExecuted = false;

            var failingOperation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromSeconds(5),
                Operation = (ct) => throw new InvalidOperationException("First operation fails")
            };

            var successfulOperation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "2",
                Timeout = TimeSpan.FromSeconds(5),
                Operation = async (ct) =>
                {
                    await Task.Delay(10, ct);
                    secondOperationExecuted = true;
                }
            };

            // Act
            // First operation should fail
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _queue.EnqueueAndWaitAsync(failingOperation));

            // Second operation should succeed
            await _queue.EnqueueAndWaitAsync(successfulOperation);

            // Assert
            Assert.True(secondOperationExecuted);
        }

        /// <summary>
        /// Test: Multiple failed operations don't block the queue.
        /// Validates: Requirement 5.4
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_MultipleFailedOperations_QueueContinuesProcessing()
        {
            // Arrange
            var successfulOperationExecuted = false;

            // Create multiple failing operations
            var failingOperations = Enumerable.Range(1, 3).Select(i => new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = i.ToString(),
                Timeout = TimeSpan.FromSeconds(5),
                Operation = (ct) => throw new InvalidOperationException($"Operation {i} fails")
            }).ToList();

            var successfulOperation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "success",
                Timeout = TimeSpan.FromSeconds(5),
                Operation = async (ct) =>
                {
                    await Task.Delay(10, ct);
                    successfulOperationExecuted = true;
                }
            };

            // Act
            foreach (var op in failingOperations)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _queue.EnqueueAndWaitAsync(op));
            }

            await _queue.EnqueueAndWaitAsync(successfulOperation);

            // Assert
            Assert.True(successfulOperationExecuted);
        }

        #endregion

        #region Queue Full Tests (Requirement 5.6)

        /// <summary>
        /// Test: When queue is full, ServiceUnavailableException (503) is thrown.
        /// Validates: Requirement 5.6 - WHEN the queue exceeds a configurable threshold, THE Backend SHALL return a 503 Service Unavailable response
        /// </summary>
        [Fact]
        public async Task EnqueueAsync_QueueFull_ThrowsServiceUnavailableException()
        {
            // Arrange - Create a queue with capacity of 3
            var smallQueue = CreateQueueWithCapacity(3);

            // Create blocking operations that will fill the queue
            var blockingTcs = new TaskCompletionSource<bool>();
            
            // Start the queue
            await smallQueue.StartAsync(CancellationToken.None);

            try
            {
                // Enqueue first operation - this will start processing and block
                await smallQueue.EnqueueAsync(new WriteOperation
                {
                    EntityType = "TestEntity",
                    EntityId = "1",
                    Timeout = TimeSpan.FromSeconds(30),
                    Operation = async (ct) => await blockingTcs.Task
                });
                
                // Give the processor time to pick up the first operation
                await Task.Delay(100);

                // Now fill the queue with 3 more operations (capacity is 3)
                await smallQueue.EnqueueAsync(new WriteOperation
                {
                    EntityType = "TestEntity",
                    EntityId = "2",
                    Timeout = TimeSpan.FromSeconds(30),
                    Operation = async (ct) => await blockingTcs.Task
                });

                await smallQueue.EnqueueAsync(new WriteOperation
                {
                    EntityType = "TestEntity",
                    EntityId = "3",
                    Timeout = TimeSpan.FromSeconds(30),
                    Operation = async (ct) => await blockingTcs.Task
                });

                await smallQueue.EnqueueAsync(new WriteOperation
                {
                    EntityType = "TestEntity",
                    EntityId = "4",
                    Timeout = TimeSpan.FromSeconds(30),
                    Operation = async (ct) => await blockingTcs.Task
                });

                // This should fail with ServiceUnavailableException since queue is full
                var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
                    () => smallQueue.EnqueueAsync(new WriteOperation
                    {
                        EntityType = "TestEntity",
                        EntityId = "5",
                        Timeout = TimeSpan.FromSeconds(30),
                        Operation = async (ct) => await blockingTcs.Task
                    }));

                // Assert
                Assert.Contains("queue is full", exception.Message.ToLower());
            }
            finally
            {
                // Cleanup - unblock operations and stop queue
                blockingTcs.SetResult(true);
                await smallQueue.StopAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Test: ServiceUnavailableException has correct error code.
        /// Validates: Requirement 5.6
        /// </summary>
        [Fact]
        public async Task EnqueueAsync_QueueFull_ExceptionHasCorrectCode()
        {
            // Arrange - Create a queue with capacity of 1
            var smallQueue = CreateQueueWithCapacity(1);

            var blockingTcs = new TaskCompletionSource<bool>();
            var blockingOperation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromSeconds(30),
                Operation = async (ct) => await blockingTcs.Task
            };

            var overflowOperation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "2",
                Timeout = TimeSpan.FromSeconds(30),
                Operation = (ct) => Task.CompletedTask
            };

            await smallQueue.StartAsync(CancellationToken.None);

            try
            {
                // Fill the queue - first operation will be picked up by processor
                await smallQueue.EnqueueAsync(blockingOperation);
                
                // Give the processor time to pick up the first operation
                await Task.Delay(50);

                // Now the queue should be empty but processor is blocked
                // Enqueue another blocking operation to fill the queue
                var secondBlockingOperation = new WriteOperation
                {
                    EntityType = "TestEntity",
                    EntityId = "3",
                    Timeout = TimeSpan.FromSeconds(30),
                    Operation = async (ct) => await blockingTcs.Task
                };
                await smallQueue.EnqueueAsync(secondBlockingOperation);

                // Act - This should fail because queue is full
                var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
                    () => smallQueue.EnqueueAsync(overflowOperation));

                // Assert
                Assert.Equal("SERVICE_UNAVAILABLE", exception.Code);
            }
            finally
            {
                blockingTcs.SetResult(true);
                await smallQueue.StopAsync(CancellationToken.None);
            }
        }

        #endregion

        #region Queue Position and Length Tests

        /// <summary>
        /// Test: GetQueueLengthAsync returns correct count.
        /// </summary>
        [Fact]
        public async Task GetQueueLengthAsync_ReturnsCorrectCount()
        {
            // Arrange - Create a queue and add some operations
            var blockingTcs = new TaskCompletionSource<bool>();
            var operations = Enumerable.Range(1, 3).Select(i => new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = i.ToString(),
                Timeout = TimeSpan.FromSeconds(30),
                Operation = async (ct) => await blockingTcs.Task
            }).ToList();

            // Enqueue operations
            foreach (var op in operations)
            {
                await _queue.EnqueueAsync(op);
            }

            // Act
            var length = await _queue.GetQueueLengthAsync();

            // Assert - Length should be at least 2 (one might be processing)
            Assert.True(length >= 2);

            // Cleanup
            blockingTcs.SetResult(true);
        }

        /// <summary>
        /// Test: GetPositionAsync returns correct position for pending operation.
        /// </summary>
        [Fact]
        public async Task GetPositionAsync_PendingOperation_ReturnsCorrectPosition()
        {
            // Arrange
            var blockingTcs = new TaskCompletionSource<bool>();
            var operation = new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = "1",
                Timeout = TimeSpan.FromSeconds(30),
                Operation = async (ct) => await blockingTcs.Task
            };

            var result = await _queue.EnqueueAsync(operation);

            // Act
            var position = await _queue.GetPositionAsync(result.OperationId);

            // Assert
            Assert.True(position.Found);
            Assert.False(position.IsCompleted);
            Assert.True(position.Position >= 0);

            // Cleanup
            blockingTcs.SetResult(true);
        }

        /// <summary>
        /// Test: GetPositionAsync returns not found for unknown operation.
        /// </summary>
        [Fact]
        public async Task GetPositionAsync_UnknownOperation_ReturnsNotFound()
        {
            // Act
            var position = await _queue.GetPositionAsync("unknown-operation-id");

            // Assert
            Assert.False(position.Found);
            Assert.False(position.IsCompleted);
            Assert.Equal(0, position.Position);
        }

        #endregion

        #region FIFO Order Tests

        /// <summary>
        /// Test: Operations are processed in FIFO order.
        /// Validates: Requirement 5.2 - WHEN multiple write requests arrive for the same entity, THE Concurrency_Controller SHALL process them in order
        /// </summary>
        [Fact]
        public async Task EnqueueAndWaitAsync_OperationsProcessedInFIFOOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            var operations = Enumerable.Range(1, 5).Select(i => new WriteOperation
            {
                EntityType = "TestEntity",
                EntityId = i.ToString(),
                Timeout = TimeSpan.FromSeconds(5),
                Operation = async (ct) =>
                {
                    await Task.Delay(10, ct);
                    lock (executionOrder)
                    {
                        executionOrder.Add(i);
                    }
                }
            }).ToList();

            // Act - Enqueue all operations and wait for them
            var tasks = operations.Select(op => _queue.EnqueueAndWaitAsync(op)).ToList();
            await Task.WhenAll(tasks);

            // Assert - Operations should be executed in order
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, executionOrder);
        }

        #endregion
    }
}
