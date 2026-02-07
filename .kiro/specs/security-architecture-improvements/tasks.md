# Implementation Plan: Security and Architecture Improvements

## Overview

This implementation plan breaks down the security hardening and architecture improvements into discrete, incremental tasks. Each task builds on previous work and includes testing to validate correctness. The implementation follows a defense-in-depth approach, starting with foundational security services and building up to complete integration.

## Tasks

- [ ] 1. Set up foundational security infrastructure
  - [x] 1.1 Create base exception classes and error handling infrastructure
    - Create `RobigooException` base class and derived exception types
    - Create `GlobalExceptionHandler` implementing `IExceptionHandler`
    - Register exception handler in Program.cs
    - _Requirements: 11.1, 11.3, 9.2_
  
  - [x] 1.2 Implement correlation ID middleware
    - Create middleware to generate and attach correlation IDs to requests
    - Add correlation ID to response headers
    - Configure structured logging with correlation ID
    - _Requirements: 11.4_
  
  - [ ]* 1.3 Write property tests for safe error responses
    - **Property 27: Safe Error Responses**
    - **Validates: Requirements 9.2, 11.1, 11.3**
  
  - [ ]* 1.4 Write property tests for correlation ID tracing
    - **Property 32: Correlation ID Tracing**
    - **Validates: Requirements 11.4**

- [ ] 2. Implement input validation service
  - [x] 2.1 Create SQL injection detection service
    - Implement `SqlInjectionDetector` class with pattern matching
    - Define comprehensive list of SQL injection patterns
    - Add logging for detected injection attempts
    - _Requirements: 1.3, 1.6_
  
  - [x] 2.2 Create input validation service
    - Implement `IInputValidationService` interface
    - Implement `ValidateAndSanitize<T>` method with reflection-based validation
    - Implement `SanitizeString` with HTML encoding and length limiting
    - Implement `IsValidEmail` and `IsValidPhoneNumber` validators
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_
  
  - [x] 2.3 Create validation middleware
    - Create middleware to validate all incoming request bodies
    - Return 400 Bad Request with validation errors on failure
    - _Requirements: 2.7_
  
  - [ ]* 2.4 Write property tests for SQL injection prevention
    - **Property 1: SQL Injection Prevention**
    - **Validates: Requirements 1.3, 1.4, 1.6**
  
  - [ ]* 2.5 Write property tests for input validation
    - **Property 2: Input String Sanitization**
    - **Property 3: Email and Phone Validation**
    - **Property 4: Numeric Range Validation**
    - **Property 5: Validation Error Response**
    - **Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.6, 2.7**

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Fix session management
  - [x] 4.1 Enhance UserSession model
    - Add `SessionTimeoutMinutes` nullable property
    - Add `InvalidatedAt` datetime property
    - Add `InvalidationReason` string property
    - Create database migration for new columns
    - _Requirements: 3.4, 3.7_
  
  - [x] 4.2 Implement session management service
    - Create `ISessionManagementService` interface
    - Implement `CheckExistingSessionsAsync` with proper active session detection
    - Fix the bug: only return conflict when truly active session exists
    - Implement `InvalidateSessionAsync` and `InvalidateAllUserSessionsAsync`
    - Implement `CleanupExpiredSessionsAsync`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_
  
  - [x] 4.3 Update login endpoint to use session management service
    - Refactor `/api/auth/login` to use `ISessionManagementService`
    - Ensure conflict is only returned when active session truly exists
    - Update session creation logic
    - _Requirements: 3.2, 3.3_
  
  - [x] 4.4 Add session cleanup background service
    - Create hosted service to periodically clean up expired sessions
    - Configure cleanup interval via appsettings
    - _Requirements: 3.5_
  
  - [ ]* 4.5 Write property tests for session management
    - **Property 6: Session Conflict Detection Accuracy**
    - **Property 7: Session Invalidation on Logout**
    - **Property 8: Expired Session Cleanup**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7**
  
  - [ ]* 4.6 Write unit tests for session bug fix
    - Test: Login with no existing session succeeds without conflict
    - Test: Login with expired session succeeds without conflict
    - Test: Login with active session returns conflict
    - _Requirements: 3.2, 3.3_

- [x] 5. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Implement database concurrency control
  - [x] 6.1 Add optimistic concurrency to entity models
    - Add `RowVersion` property to `CropSprayer` entity
    - Add `RowVersion` property to `InspectionProtocol` entity
    - Add `Version` property for application-level versioning
    - Configure EF Core to use row version for concurrency
    - Create database migration
    - _Requirements: 4.1_
  
  - [x] 6.2 Implement concurrency controller
    - Create `IConcurrencyController` interface
    - Implement `ExecuteWithOptimisticConcurrencyAsync` with retry logic
    - Implement exponential backoff (100ms, 200ms, 400ms)
    - Implement `ExecuteWithLockAsync` for entity-level locking
    - _Requirements: 4.2, 4.3, 4.5_
  
  - [x] 6.3 Update controllers to use concurrency controller
    - Update `CropSprayersController.UpdateCropSprayer` to handle concurrency
    - Update `InspectionProtocolsController.UpdateProtocol` to handle concurrency
    - Return 409 Conflict with details on concurrency failure
    - _Requirements: 4.3, 4.6_
  
  - [x] 6.4 Configure SQLite WAL mode
    - Update database connection string to enable WAL mode
    - Add initialization code to set PRAGMA journal_mode=WAL
    - _Requirements: 4.7_
  
  - [ ]* 6.5 Write property tests for concurrency control
    - **Property 9: Optimistic Concurrency Conflict Detection**
    - **Property 10: Transaction Atomicity**
    - **Property 11: Retry with Exponential Backoff**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**

- [x] 7. Implement database write queue
  - [x] 7.1 Create write queue service
    - Implement `IDatabaseWriteQueue` interface
    - Create `DatabaseWriteQueue` as `IHostedService`
    - Use `Channel<WriteOperation>` for bounded queue
    - Implement FIFO processing
    - _Requirements: 5.1, 5.2_
  
  - [x] 7.2 Implement queue timeout and failure handling
    - Add timeout handling for queued operations
    - Ensure failed operations don't block queue
    - Return 503 when queue is full
    - _Requirements: 5.4, 5.5, 5.6_
  
  - [x] 7.3 Integrate write queue with critical operations
    - Update inspection protocol save to use write queue
    - Add queue position feedback to API responses
    - _Requirements: 5.3_
  
  - [ ]* 7.4 Write property tests for write queue
    - **Property 12: Write Queue Ordering**
    - **Property 13: Queue Failure Isolation**
    - **Property 14: Queue Operation Timeout**
    - **Validates: Requirements 5.1, 5.2, 5.4, 5.5**

- [x] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Enhance authentication security
  - [x] 9.1 Implement account lockout
    - Add lockout tracking to `ISecurityAuditService`
    - Implement configurable failed attempt threshold
    - Implement lockout duration
    - Update login endpoint to check lockout status
    - _Requirements: 7.1_
  
  - [x] 9.2 Implement progressive login delays
    - Add delay calculation based on failed attempt count
    - Implement delay before responding to failed login
    - _Requirements: 7.4_
  
  - [x] 9.3 Enhance authentication logging
    - Ensure all login attempts are logged with IP and user agent
    - Add security event logging for suspicious activity
    - _Requirements: 7.2_
  
  - [x] 9.4 Implement generic lockout error response
    - Ensure locked account error is indistinguishable from invalid credentials
    - _Requirements: 7.3_
  
  - [x] 9.5 Implement refresh token rotation
    - Update refresh endpoint to issue new refresh token
    - Invalidate old refresh token after use
    - Prevent token replay attacks
    - _Requirements: 7.7_
  
  - [ ]* 9.6 Write property tests for authentication
    - **Property 18: Account Lockout**
    - **Property 19: Authentication Logging**
    - **Property 20: Generic Lockout Error**
    - **Property 21: Token Validation**
    - **Property 22: Refresh Token Rotation**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.5, 7.6, 7.7**

- [x] 10. Enhance authorization
  - [x] 10.1 Create authorization service
    - Implement `IAuthorizationService` interface
    - Implement `CanAccessResourceAsync` for resource-level authorization
    - Implement `CanModifyUserAsync` and `CanDeleteUserAsync`
    - Implement `ValidateRoleChange` for privilege escalation prevention
    - _Requirements: 8.1, 8.3, 8.5, 8.6_
  
  - [x] 10.2 Update controllers to use authorization service
    - Add authorization checks to all protected endpoints
    - Ensure non-admin users get 403 for admin operations
    - Implement resource ownership checks
    - _Requirements: 8.2, 8.4_
  
  - [x] 10.3 Implement authorization failure logging
    - Log all authorization failures with user ID and resource
    - _Requirements: 8.7_
  
  - [ ]* 10.4 Write property tests for authorization
    - **Property 23: Role-Based Access Control**
    - **Property 24: Resource Ownership Authorization**
    - **Property 25: Privilege Escalation Prevention**
    - **Property 26: Authorization Failure Logging**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.6, 8.7**

- [x] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Implement secure data handling
  - [x] 12.1 Verify BCrypt password hashing
    - Audit existing password hashing implementation
    - Ensure work factor is at least 12
    - Add password hash format validation
    - _Requirements: 9.4_
  
  - [x] 12.2 Enhance audit logging
    - Ensure all data modifications create audit entries
    - Add user attribution to all audit logs
    - Add sensitive data access logging
    - _Requirements: 9.6, 9.7_
  
  - [x] 12.3 Implement log injection prevention
    - Sanitize user input before logging
    - Remove/escape newlines and control characters
    - _Requirements: 11.7_
  
  - [ ]* 12.4 Write property tests for secure data handling
    - **Property 28: BCrypt Password Hashing**
    - **Property 29: Audit Trail Completeness**
    - **Property 33: Log Injection Prevention**
    - **Validates: Requirements 9.4, 9.6, 11.7**

- [x] 13. Enhance security headers and CORS
  - [x] 13.1 Review and enhance security headers middleware
    - Verify Content-Security-Policy header
    - Verify X-Content-Type-Options header
    - Verify X-Frame-Options header
    - Add Cache-Control headers for sensitive endpoints
    - Add Strict-Transport-Security for production
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.7_
  
  - [x] 13.2 Enhance CORS configuration
    - Restrict allowed origins in production
    - Configure allowed methods and headers
    - _Requirements: 10.5_
  
  - [x] 13.3 Add Content-Disposition for file downloads
    - Update file download endpoints to include proper headers
    - _Requirements: 10.6_
  
  - [ ]* 13.4 Write property tests for security headers
    - **Property 30: Security Headers Presence**
    - **Property 31: CORS Restriction**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.5, 10.7**

- [x] 14. Enhance rate limiting
  - [x] 14.1 Implement per-user rate limiting
    - Add user-based rate limiting in addition to IP-based
    - Configure separate limits for authenticated users
    - _Requirements: 12.2_
  
  - [x] 14.2 Implement sliding window rate limiting
    - Replace fixed window with sliding window algorithm
    - _Requirements: 12.4_
  
  - [x] 14.3 Implement rate limit violation logging
    - Log all rate limit violations with source details
    - _Requirements: 12.7_
  
  - [x] 14.4 Implement temporary blocking for sustained violations
    - Track sustained violations
    - Implement temporary IP/user blocking
    - _Requirements: 12.6_
  
  - [ ]* 14.5 Write property tests for rate limiting
    - **Property 34: Rate Limiting Enforcement**
    - **Property 35: Per-User Rate Limiting**
    - **Property 36: Rate Limit Violation Logging**
    - **Validates: Requirements 12.1, 12.2, 12.3, 12.7**

- [ ] 15. Server-side business logic migration
  - [x] 15.1 Move protocol number generation to server
    - Ensure protocol numbers are generated server-side only
    - Remove any client-side protocol number logic
    - _Requirements: 6.3_
  
  - [x] 15.2 Consolidate validation on server
    - Review all client-side validation
    - Ensure equivalent server-side validation exists
    - Add any missing server-side validations
    - _Requirements: 6.1, 6.4_
  
  - [x] 15.3 Enhance file upload validation
    - Validate file size server-side
    - Validate content type server-side
    - Add magic bytes validation for file type verification
    - _Requirements: 6.6_
  
  - [ ]* 15.4 Write property tests for server-side logic
    - **Property 15: Protocol Number Generation**
    - **Property 16: Server-Side Constraint Enforcement**
    - **Property 17: File Upload Validation**
    - **Validates: Requirements 6.3, 6.4, 6.6**

- [x] 16. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 17. Integration testing and documentation
  - [x] 17.1 Write integration tests for session bug fix
    - Test complete login flow with no existing session
    - Test complete login flow with expired session
    - Test complete login flow with active session
    - _Requirements: 3.2, 3.3_
  
  - [x] 17.2 Write integration tests for concurrent save
    - Test two users modifying same record
    - Verify first succeeds, second gets 409
    - _Requirements: 4.2, 4.3_
  
  - [-] 17.3 Update SECURITY.md documentation
    - Document all new security features
    - Update configuration options
    - Add deployment checklist
    - _Requirements: All_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The session management fix (Task 4) addresses the specific bug reported by the user
- Database concurrency (Tasks 6-7) prevents race conditions when multiple users save simultaneously
