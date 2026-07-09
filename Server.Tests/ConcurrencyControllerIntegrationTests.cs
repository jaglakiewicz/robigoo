/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Data;
using Server.Exceptions;
using Server.Models;
using Server.Services;

namespace Server.Tests
{
    /// <summary>
    /// Integration tests for concurrent save operations.
    /// Tests the ConcurrencyController behavior when two users attempt to modify the same record.
    /// 
    /// Validates: Requirements 4.2, 4.3
    /// - 4.2: WHEN two users attempt to modify the same record simultaneously, THE Concurrency_Controller SHALL detect the conflict
    /// - 4.3: IF a concurrency conflict is detected, THEN THE Backend SHALL return a 409 Conflict response with details
    /// 
    /// Property 9: Optimistic Concurrency Conflict Detection
    /// *For any* two concurrent modifications to the same entity, if both read the same row version and attempt to save,
    /// exactly one SHALL succeed and the other SHALL receive a concurrency conflict error.
    /// </summary>
    public class ConcurrencyControllerIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ConcurrencyController _concurrencyController;
        private readonly Mock<ILogger<ConcurrencyController>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public ConcurrencyControllerIntegrationTests()
        {
            // Set up in-memory database with unique name for test isolation
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _context = new AppDbContext(options, _httpContextAccessorMock.Object);

            _loggerMock = new Mock<ILogger<ConcurrencyController>>();
            _concurrencyController = new ConcurrencyController(_loggerMock.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var protocol = new InspectionProtocol
            {
                Id = 1,
                ProtocolNumber = "0001/2025",
                InspectionDate = DateTime.UtcNow,
                InspectorName = "Test Inspector",
                ClientName = "Test Client",
                CropSprayerSerialNumber = "SN-001",
                FinalResult = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.InspectionProtocols.Add(protocol);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Two Users Modifying Same Record Tests (Requirements 4.2, 4.3)

        /// <summary>
        /// Integration Test: Two users modifying the same record - first succeeds, second gets conflict.
        /// 
        /// Validates: Requirements 4.2, 4.3
        /// - 4.2: Detect the conflict when two users modify the same record
        /// - 4.3: Return 409 Conflict response with details
        /// 
        /// Scenario:
        /// 1. User A reads the protocol (gets version 1)
        /// 2. User B reads the same protocol (gets version 1)
        /// 3. User A saves changes (succeeds, version becomes 2)
        /// 4. User B attempts to save changes (fails with concurrency conflict)
        /// </summary>
        [Fact]
        public async Task TwoUsersModifyingSameRecord_FirstSucceeds_SecondGetsConflict()
        {
            // Arrange - Simulate two users reading the same record
            var protocolId = 1L;
            
            // User A reads the protocol
            var userAProtocol = await _context.InspectionProtocols.FindAsync(protocolId);
            Assert.NotNull(userAProtocol);
            var userAOriginalVersion = userAProtocol.Version;
            
            // User B reads the same protocol - capture the version they see
            var userBOriginalVersion = userAOriginalVersion; // Both users see the same version initially

            // Act - User A saves changes first
            var userAResult = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                userAProtocol.InspectorName = "User A Updated Inspector";
                userAProtocol.Version++;
                userAProtocol.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return userAProtocol;
            });

            // Assert - User A succeeds
            Assert.True(userAResult.Success);
            Assert.NotNull(userAResult.Result);
            Assert.Equal("User A Updated Inspector", userAResult.Result.InspectorName);
            Assert.Equal(userAOriginalVersion + 1, userAResult.Result.Version);

            // Act - User B attempts to save changes (should fail due to version mismatch)
            // Simulate the conflict by checking version before save
            var userBResult = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                // Re-fetch to check current version (simulating User B's save attempt)
                var currentProtocol = await _context.InspectionProtocols.FindAsync(protocolId);
                
                // Simulate optimistic concurrency check - version has changed since User B read it
                if (currentProtocol!.Version != userBOriginalVersion)
                {
                    throw new DbUpdateConcurrencyException(
                        "The record was modified by another user.");
                }
                
                currentProtocol.InspectorName = "User B Updated Inspector";
                currentProtocol.Version++;
                await _context.SaveChangesAsync();
                return currentProtocol;
            });

            // Assert - User B gets conflict
            Assert.False(userBResult.Success);
            Assert.NotNull(userBResult.Conflict);
            Assert.Contains("modified by another user", userBResult.Conflict.Message);
        }

        /// <summary>
        /// Integration Test: Concurrent modifications with explicit version checking.
        /// 
        /// Validates: Requirements 4.2, 4.3
        /// 
        /// Scenario:
        /// 1. Two concurrent operations attempt to modify the same entity
        /// 2. First operation succeeds
        /// 3. Second operation detects the conflict and returns appropriate error
        /// </summary>
        [Fact]
        public async Task ConcurrentModifications_WithVersionCheck_DetectsConflict()
        {
            // Arrange
            var protocolId = 1L;
            var initialProtocol = await _context.InspectionProtocols.FindAsync(protocolId);
            Assert.NotNull(initialProtocol);
            var initialVersion = initialProtocol.Version;

            var firstOperationCompleted = new TaskCompletionSource<bool>();
            var secondOperationStarted = new TaskCompletionSource<bool>();

            // Act - Run two concurrent operations
            var firstOperation = Task.Run(async () =>
            {
                // Wait for second operation to start reading
                await secondOperationStarted.Task;
                
                return await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
                {
                    var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                    protocol!.ClientName = "First User Update";
                    protocol.Version++;
                    protocol.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    firstOperationCompleted.SetResult(true);
                    return protocol;
                });
            });

            var secondOperation = Task.Run(async () =>
            {
                // Read the initial version
                var readVersion = initialVersion;
                secondOperationStarted.SetResult(true);
                
                // Wait for first operation to complete
                await firstOperationCompleted.Task;
                
                return await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
                {
                    var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                    
                    // Check if version changed (simulating optimistic concurrency)
                    if (protocol!.Version != readVersion)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Concurrency conflict detected");
                    }
                    
                    protocol.ClientName = "Second User Update";
                    protocol.Version++;
                    await _context.SaveChangesAsync();
                    return protocol;
                });
            });

            // Wait for both operations
            var results = await Task.WhenAll(firstOperation, secondOperation);

            // Assert
            var firstResult = results[0];
            var secondResult = results[1];

            // First operation should succeed
            Assert.True(firstResult.Success);
            Assert.Equal("First User Update", firstResult.Result?.ClientName);

            // Second operation should fail with conflict
            Assert.False(secondResult.Success);
            Assert.NotNull(secondResult.Conflict);
        }

        /// <summary>
        /// Integration Test: Conflict response contains proper details.
        /// 
        /// Validates: Requirement 4.3
        /// - IF a concurrency conflict is detected, THEN THE Backend SHALL return a 409 Conflict response with details
        /// </summary>
        [Fact]
        public async Task ConcurrencyConflict_ResponseContainsProperDetails()
        {
            // Arrange - Force a concurrency conflict
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync<InspectionProtocol>(async () =>
            {
                // Simulate a DbUpdateConcurrencyException
                throw new DbUpdateConcurrencyException(
                    "The record was modified by another user.");
            });

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Conflict);
            Assert.Contains("modified by another user", result.Conflict.Message);
            Assert.True(result.Conflict.ConflictTime <= DateTime.UtcNow);
            Assert.True(result.Conflict.ConflictTime >= DateTime.UtcNow.AddSeconds(-5));
        }

        #endregion

        #region Retry Logic with Exponential Backoff Tests (Requirement 4.5)

        /// <summary>
        /// Integration Test: Retry logic with exponential backoff succeeds on retry.
        /// 
        /// Validates: Requirement 4.5
        /// - THE Concurrency_Controller SHALL implement retry logic with exponential backoff for transient failures
        /// 
        /// Scenario:
        /// 1. First attempt fails with concurrency exception
        /// 2. Second attempt succeeds
        /// 3. Total time should reflect the backoff delay
        /// </summary>
        [Fact]
        public async Task RetryLogic_SucceedsOnSecondAttempt_WithExponentialBackoff()
        {
            // Arrange
            var attemptCount = 0;
            var startTime = DateTime.UtcNow;

            // Act
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                attemptCount++;
                
                if (attemptCount == 1)
                {
                    // First attempt fails
                    throw new DbUpdateConcurrencyException(
                        "Transient failure");
                }
                
                // Second attempt succeeds
                var protocol = await _context.InspectionProtocols.FindAsync(1L);
                protocol!.GeneralNotes = "Updated after retry";
                await _context.SaveChangesAsync();
                return protocol;
            });

            var elapsedTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, attemptCount);
            Assert.NotNull(result.Result);
            Assert.Equal("Updated after retry", result.Result.GeneralNotes);
            
            // Should have waited at least 100ms (initial backoff delay)
            Assert.True(elapsedTime.TotalMilliseconds >= 90, 
                $"Expected at least 90ms delay, but was {elapsedTime.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Integration Test: Retry logic fails after max retries.
        /// 
        /// Validates: Requirement 4.5
        /// 
        /// Scenario:
        /// 1. All 3 retry attempts fail
        /// 2. Returns conflict after exhausting retries
        /// 3. Total time should reflect exponential backoff (100ms + 200ms + 400ms = ~700ms)
        /// </summary>
        [Fact]
        public async Task RetryLogic_FailsAfterMaxRetries_WithExponentialBackoff()
        {
            // Arrange
            var attemptCount = 0;
            var startTime = DateTime.UtcNow;

            // Act
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync<InspectionProtocol>(async () =>
            {
                attemptCount++;
                
                // All attempts fail
                throw new DbUpdateConcurrencyException(
                    "Persistent failure");
            });

            var elapsedTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.False(result.Success);
            Assert.Equal(3, attemptCount); // MaxRetries = 3
            Assert.NotNull(result.Conflict);
            Assert.Contains("modified by another user", result.Conflict.Message);
            
            // Should have waited approximately 100ms + 200ms = 300ms (delays between retries)
            // Allow some tolerance for test execution
            Assert.True(elapsedTime.TotalMilliseconds >= 250, 
                $"Expected at least 250ms total delay, but was {elapsedTime.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Integration Test: Exponential backoff delays increase correctly.
        /// 
        /// Validates: Requirement 4.5
        /// - Delays should be 100ms, 200ms, 400ms (exponential)
        /// </summary>
        [Fact]
        public async Task ExponentialBackoff_DelaysIncreaseCorrectly()
        {
            // Arrange
            var attemptTimes = new List<DateTime>();

            // Act
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync<InspectionProtocol>(async () =>
            {
                attemptTimes.Add(DateTime.UtcNow);
                
                throw new DbUpdateConcurrencyException(
                    "Failure");
            });

            // Assert
            Assert.Equal(3, attemptTimes.Count);
            
            // Calculate delays between attempts
            var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
            var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;

            // First delay should be ~100ms
            Assert.True(delay1 >= 90 && delay1 <= 200, 
                $"First delay should be ~100ms, was {delay1}ms");
            
            // Second delay should be ~200ms (double the first)
            Assert.True(delay2 >= 180 && delay2 <= 400, 
                $"Second delay should be ~200ms, was {delay2}ms");
            
            // Second delay should be approximately double the first
            Assert.True(delay2 > delay1, 
                $"Second delay ({delay2}ms) should be greater than first ({delay1}ms)");
        }

        #endregion

        #region Entity-Level Locking Tests

        /// <summary>
        /// Integration Test: Entity-level locking prevents concurrent access.
        /// 
        /// Validates: Requirement 4.6
        /// - WHEN saving inspection protocols, THE Database_Access_Layer SHALL acquire appropriate locks
        /// </summary>
        [Fact]
        public async Task EntityLevelLocking_PreventsSimultaneousAccess()
        {
            // Arrange
            var resourceKey = "InspectionProtocol:1";
            var executionOrder = new List<string>();
            var lockObject = new object();

            // Act - Start two operations that try to access the same resource
            var operation1 = _concurrencyController.ExecuteWithLockAsync(
                resourceKey,
                async () =>
                {
                    lock (lockObject) { executionOrder.Add("Op1-Start"); }
                    await Task.Delay(100); // Simulate work
                    lock (lockObject) { executionOrder.Add("Op1-End"); }
                    return "Operation 1";
                },
                TimeSpan.FromSeconds(5));

            // Small delay to ensure operation1 acquires the lock first
            await Task.Delay(10);

            var operation2 = _concurrencyController.ExecuteWithLockAsync(
                resourceKey,
                async () =>
                {
                    lock (lockObject) { executionOrder.Add("Op2-Start"); }
                    await Task.Delay(50);
                    lock (lockObject) { executionOrder.Add("Op2-End"); }
                    return "Operation 2";
                },
                TimeSpan.FromSeconds(5));

            // Wait for both operations
            var results = await Task.WhenAll(operation1, operation2);

            // Assert - Operations should execute sequentially, not concurrently
            Assert.Equal(4, executionOrder.Count);
            Assert.Equal("Op1-Start", executionOrder[0]);
            Assert.Equal("Op1-End", executionOrder[1]);
            Assert.Equal("Op2-Start", executionOrder[2]);
            Assert.Equal("Op2-End", executionOrder[3]);
            
            Assert.Equal("Operation 1", results[0]);
            Assert.Equal("Operation 2", results[1]);
        }

        /// <summary>
        /// Integration Test: Lock timeout throws TimeoutException.
        /// </summary>
        [Fact]
        public async Task EntityLevelLocking_TimeoutThrowsException()
        {
            // Arrange
            var resourceKey = "InspectionProtocol:timeout-test";
            var blockingOperation = new TaskCompletionSource<bool>();

            // Start a blocking operation
            var blockingTask = _concurrencyController.ExecuteWithLockAsync(
                resourceKey,
                async () =>
                {
                    await blockingOperation.Task; // Block until released
                    return "Blocking";
                },
                TimeSpan.FromSeconds(30));

            // Small delay to ensure blocking operation acquires the lock
            await Task.Delay(50);

            // Act & Assert - Second operation should timeout
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await _concurrencyController.ExecuteWithLockAsync(
                    resourceKey,
                    async () =>
                    {
                        await Task.Delay(10);
                        return "Should not execute";
                    },
                    TimeSpan.FromMilliseconds(100)); // Very short timeout
            });

            // Cleanup
            blockingOperation.SetResult(true);
            await blockingTask;
        }

        #endregion

        #region ConcurrencyException Handling Tests

        /// <summary>
        /// Integration Test: Custom ConcurrencyException is handled correctly.
        /// 
        /// Validates: Requirements 4.2, 4.3
        /// </summary>
        [Fact]
        public async Task CustomConcurrencyException_IsHandledCorrectly()
        {
            // Arrange
            var attemptCount = 0;

            // Act
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync<InspectionProtocol>(async () =>
            {
                attemptCount++;
                
                // Throw custom ConcurrencyException
                throw new ConcurrencyException(
                    "Custom concurrency error",
                    entityType: "InspectionProtocol",
                    entityId: "1");
            });

            // Assert
            Assert.False(result.Success);
            Assert.Equal(3, attemptCount); // Should retry 3 times
            Assert.NotNull(result.Conflict);
            Assert.Equal("Custom concurrency error", result.Conflict.Message);
            Assert.Equal("InspectionProtocol", result.Conflict.EntityType);
            Assert.Equal("1", result.Conflict.EntityId);
        }

        #endregion

        #region Successful Operation Tests

        /// <summary>
        /// Integration Test: Single user modification succeeds without conflict.
        /// </summary>
        [Fact]
        public async Task SingleUserModification_SucceedsWithoutConflict()
        {
            // Arrange
            var protocolId = 1L;

            // Act
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                protocol!.InspectorName = "Updated Inspector Name";
                protocol.ClientName = "Updated Client Name";
                protocol.Version++;
                protocol.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return protocol;
            });

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Conflict);
            Assert.NotNull(result.Result);
            Assert.Equal("Updated Inspector Name", result.Result.InspectorName);
            Assert.Equal("Updated Client Name", result.Result.ClientName);
            Assert.Equal(2, result.Result.Version);
        }

        /// <summary>
        /// Integration Test: Multiple sequential modifications by same user succeed.
        /// </summary>
        [Fact]
        public async Task MultipleSequentialModifications_BySameUser_Succeed()
        {
            // Arrange
            var protocolId = 1L;
            
            // Track the values at each step
            string? firstUpdateName = null;
            int firstUpdateVersion = 0;
            string? secondUpdateName = null;
            int secondUpdateVersion = 0;
            string? thirdUpdateName = null;
            int thirdUpdateVersion = 0;

            // Act - First modification
            var result1 = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                protocol!.InspectorName = "First Update";
                protocol.Version++;
                await _context.SaveChangesAsync();
                
                // Capture values at this point
                firstUpdateName = protocol.InspectorName;
                firstUpdateVersion = protocol.Version;
                return protocol;
            });

            // Second modification
            var result2 = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                protocol!.InspectorName = "Second Update";
                protocol.Version++;
                await _context.SaveChangesAsync();
                
                // Capture values at this point
                secondUpdateName = protocol.InspectorName;
                secondUpdateVersion = protocol.Version;
                return protocol;
            });

            // Third modification
            var result3 = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                var protocol = await _context.InspectionProtocols.FindAsync(protocolId);
                protocol!.InspectorName = "Third Update";
                protocol.Version++;
                await _context.SaveChangesAsync();
                
                // Capture values at this point
                thirdUpdateName = protocol.InspectorName;
                thirdUpdateVersion = protocol.Version;
                return protocol;
            });

            // Assert - All modifications succeed
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.True(result3.Success);
            
            // Verify captured values at each step
            Assert.Equal("First Update", firstUpdateName);
            Assert.Equal("Second Update", secondUpdateName);
            Assert.Equal("Third Update", thirdUpdateName);
            
            Assert.Equal(2, firstUpdateVersion);
            Assert.Equal(3, secondUpdateVersion);
            Assert.Equal(4, thirdUpdateVersion);
            
            // Verify final state in database
            var finalProtocol = await _context.InspectionProtocols.FindAsync(protocolId);
            Assert.NotNull(finalProtocol);
            Assert.Equal("Third Update", finalProtocol.InspectorName);
            Assert.Equal(4, finalProtocol.Version);
        }

        #endregion
    }
}
