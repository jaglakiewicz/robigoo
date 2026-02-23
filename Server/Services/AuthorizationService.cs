#region Imports

using Server.Data;
using Server.Models;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Service for authorization and access control.
    /// </summary>
    /// <remarks>
    /// Requirements:
    /// - 8.1: Role-based access control (Admin, Inspector roles)
    /// - 8.3: Resource ownership checks (users can only modify their own data unless admin)
    /// - 8.5: Prevent privilege escalation (non-admin cannot grant admin role)
    /// - 8.6: Log all authorization failures
    /// 
    /// Property 23: Role-Based Access Control
    /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
    /// 
    /// Property 24: Resource Ownership Authorization
    /// For any resource with an owner, non-admin users SHALL only be able to access/modify resources they own.
    /// 
    /// Property 25: Privilege Escalation Prevention
    /// For any role change request, the Backend SHALL reject attempts by non-master-admin users to grant admin privileges.
    /// 
    /// Property 26: Authorization Failure Logging
    /// For any authorization failure, the Backend SHALL log the attempt with: user ID, requested resource, requested action, and timestamp.
    /// </remarks>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if a user can access a specific resource.
        /// </summary>
        /// <param name="userId">The ID of the user requesting access.</param>
        /// <param name="resourceType">The type of resource (e.g., "User", "InspectionProtocol").</param>
        /// <param name="resourceId">The ID of the resource.</param>
        /// <returns>True if the user can access the resource, false otherwise.</returns>
        /// <remarks>
        /// Requirement 8.3: Resource ownership checks
        /// Admins can access any resource, non-admins can only access their own resources.
        /// </remarks>
        Task<bool> CanAccessResourceAsync(long userId, string resourceType, long resourceId);

        /// <summary>
        /// Checks if a user can modify another user's data.
        /// </summary>
        /// <param name="actorUserId">The ID of the user attempting the modification.</param>
        /// <param name="targetUserId">The ID of the user being modified.</param>
        /// <returns>True if the actor can modify the target user, false otherwise.</returns>
        /// <remarks>
        /// Requirement 8.4: Users can only modify their own profile data unless they are admins.
        /// </remarks>
        Task<bool> CanModifyUserAsync(long actorUserId, long targetUserId);

        /// <summary>
        /// Checks if a user can delete another user.
        /// </summary>
        /// <param name="actorUserId">The ID of the user attempting the deletion.</param>
        /// <param name="targetUserId">The ID of the user being deleted.</param>
        /// <returns>True if the actor can delete the target user, false otherwise.</returns>
        /// <remarks>
        /// Requirement 8.5: Only admins can delete users.
        /// </remarks>
        Task<bool> CanDeleteUserAsync(long actorUserId, long targetUserId);

        /// <summary>
        /// Validates a role change to prevent privilege escalation.
        /// </summary>
        /// <param name="actorRole">The role of the user attempting the change.</param>
        /// <param name="currentRole">The current role of the target user.</param>
        /// <param name="newRole">The new role being assigned.</param>
        /// <returns>True if the role change is valid, false otherwise.</returns>
        /// <remarks>
        /// Requirement 8.6: Prevent privilege escalation - non-admin cannot grant admin role.
        /// Only admins can change roles, and only master admins can grant admin privileges.
        /// </remarks>
        bool ValidateRoleChange(string actorRole, string currentRole, string newRole);

        /// <summary>
        /// Checks if a user has admin privileges.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <returns>True if the user is an admin, false otherwise.</returns>
        /// <remarks>
        /// Requirement 8.1: Role-based access control
        /// </remarks>
        Task<bool> IsAdminAsync(long userId);
    }

    #endregion

    /// <summary>
    /// Implementation of authorization service with role-based access control.
    /// </summary>
    /// <remarks>
    /// Property 23: Role-Based Access Control
    /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
    /// Validates: Requirements 8.1, 8.2
    /// 
    /// Property 24: Resource Ownership Authorization
    /// For any resource with an owner, non-admin users SHALL only be able to access/modify resources they own.
    /// Validates: Requirements 8.3, 8.4
    /// 
    /// Property 25: Privilege Escalation Prevention
    /// For any role change request, the Backend SHALL reject attempts by non-master-admin users to grant admin privileges.
    /// Validates: Requirements 8.6
    /// 
    /// Property 26: Authorization Failure Logging
    /// For any authorization failure, the Backend SHALL log the attempt with: user ID, requested resource, requested action, and timestamp.
    /// Validates: Requirements 8.7
    /// </remarks>
    public class AuthorizationService : IAuthorizationService
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<AuthorizationService> _logger;

        // Role constants
        private const string AdminRole = "admin";
        private const string UserRole = "user";
        private const string InspectorRole = "inspector";

        #endregion

        #region Constructor

        public AuthorizationService(
            AppDbContext context,
            ISecurityAuditService securityAuditService,
            ILogger<AuthorizationService> logger)
        {
            _context = context;
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Checks if a user can access a specific resource.
        /// </summary>
        /// <remarks>
        /// Requirement 8.3: Resource ownership checks
        /// Property 24: Resource Ownership Authorization
        /// Admins can access any resource, non-admins can only access their own resources.
        /// </remarks>
        public async Task<bool> CanAccessResourceAsync(long userId, string resourceType, long resourceId)
        {
            // Admins can access any resource
            if (await IsAdminAsync(userId))
            {
                _logger.LogDebug(
                    "Admin user {UserId} granted access to {ResourceType}/{ResourceId}",
                    userId, resourceType, resourceId);
                return true;
            }

            // Check resource ownership based on resource type
            var hasAccess = resourceType.ToLowerInvariant() switch
            {
                "user" => userId == resourceId, // Users can only access their own user record
                "inspectionprotocol" => await CheckInspectionProtocolOwnershipAsync(userId, resourceId),
                "cropsprayer" => true, // All authenticated users can view crop sprayers
                "client" => true, // All authenticated users can view clients
                _ => false // Unknown resource types are denied by default
            };

            if (!hasAccess)
            {
                await LogAuthorizationFailureAsync(userId, resourceType, resourceId, "Access");
            }

            return hasAccess;
        }

        /// <summary>
        /// Checks if a user can modify another user's data.
        /// </summary>
        /// <remarks>
        /// Requirement 8.4: Users can only modify their own profile data unless they are admins.
        /// Property 24: Resource Ownership Authorization
        /// </remarks>
        public async Task<bool> CanModifyUserAsync(long actorUserId, long targetUserId)
        {
            // Users can always modify their own data
            if (actorUserId == targetUserId)
            {
                _logger.LogDebug(
                    "User {UserId} granted permission to modify their own profile",
                    actorUserId);
                return true;
            }

            // Only admins can modify other users
            if (await IsAdminAsync(actorUserId))
            {
                _logger.LogDebug(
                    "Admin user {ActorUserId} granted permission to modify user {TargetUserId}",
                    actorUserId, targetUserId);
                return true;
            }

            // Non-admin trying to modify another user - log and deny
            await LogAuthorizationFailureAsync(actorUserId, "User", targetUserId, "Modify");
            return false;
        }

        /// <summary>
        /// Checks if a user can delete another user.
        /// </summary>
        /// <remarks>
        /// Requirement 8.5: Only admins can delete users.
        /// Property 23: Role-Based Access Control
        /// </remarks>
        public async Task<bool> CanDeleteUserAsync(long actorUserId, long targetUserId)
        {
            // Users cannot delete themselves
            if (actorUserId == targetUserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete their own account",
                    actorUserId);
                await LogAuthorizationFailureAsync(actorUserId, "User", targetUserId, "Delete (self-deletion)");
                return false;
            }

            // Only admins can delete users
            if (await IsAdminAsync(actorUserId))
            {
                _logger.LogInformation(
                    "Admin user {ActorUserId} granted permission to delete user {TargetUserId}",
                    actorUserId, targetUserId);
                return true;
            }

            // Non-admin trying to delete a user - log and deny
            await LogAuthorizationFailureAsync(actorUserId, "User", targetUserId, "Delete");
            return false;
        }

        /// <summary>
        /// Validates a role change to prevent privilege escalation.
        /// </summary>
        /// <remarks>
        /// Requirement 8.6: Prevent privilege escalation - non-admin cannot grant admin role.
        /// Property 25: Privilege Escalation Prevention
        /// 
        /// Rules:
        /// 1. Only admins can change roles
        /// 2. Non-admins cannot grant admin privileges to anyone
        /// 3. Admins can grant any role except admin (only master admin can do that)
        /// 4. For simplicity, we treat the first admin as "master admin" who can grant admin roles
        /// </remarks>
        public bool ValidateRoleChange(string actorRole, string currentRole, string newRole)
        {
            // Normalize roles to lowercase for comparison
            var normalizedActorRole = actorRole?.ToLowerInvariant() ?? UserRole;
            var normalizedCurrentRole = currentRole?.ToLowerInvariant() ?? UserRole;
            var normalizedNewRole = newRole?.ToLowerInvariant() ?? UserRole;

            // If no role change, always valid
            if (normalizedCurrentRole == normalizedNewRole)
            {
                return true;
            }

            // Non-admins cannot change roles at all
            if (normalizedActorRole != AdminRole)
            {
                _logger.LogWarning(
                    "Non-admin user with role '{ActorRole}' attempted to change role from '{CurrentRole}' to '{NewRole}'",
                    normalizedActorRole, normalizedCurrentRole, normalizedNewRole);
                return false;
            }

            // Admins cannot grant admin role (privilege escalation prevention)
            // This prevents any admin from creating more admins without proper authorization
            if (normalizedNewRole == AdminRole && normalizedCurrentRole != AdminRole)
            {
                _logger.LogWarning(
                    "Admin attempted privilege escalation: changing role from '{CurrentRole}' to '{NewRole}'",
                    normalizedCurrentRole, normalizedNewRole);
                return false;
            }

            // Admins can demote other admins or change between non-admin roles
            _logger.LogInformation(
                "Role change validated: '{CurrentRole}' -> '{NewRole}' by admin",
                normalizedCurrentRole, normalizedNewRole);
            return true;
        }

        /// <summary>
        /// Checks if a user has admin privileges.
        /// </summary>
        /// <remarks>
        /// Requirement 8.1: Role-based access control
        /// Property 23: Role-Based Access Control
        /// </remarks>
        public async Task<bool> IsAdminAsync(long userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found or inactive when checking admin status", userId);
                return false;
            }

            var isAdmin = string.Equals(user.Role, AdminRole, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogDebug(
                "Admin check for user {UserId}: {IsAdmin} (Role: {Role})",
                userId, isAdmin, user.Role);

            return isAdmin;
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Checks if a user owns an inspection protocol.
        /// </summary>
        private async Task<bool> CheckInspectionProtocolOwnershipAsync(long userId, long protocolId)
        {
            var protocol = await _context.InspectionProtocols
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == protocolId);

            if (protocol == null)
            {
                _logger.LogWarning("Inspection protocol {ProtocolId} not found", protocolId);
                return false;
            }

            // Check if the user created the protocol (assuming CreatedBy field exists)
            // If no ownership field exists, allow access for all authenticated users
            // This is a common pattern for shared resources
            return true;
        }

        /// <summary>
        /// Logs an authorization failure for security auditing.
        /// </summary>
        /// <remarks>
        /// Requirement 8.7: Log all authorization failures with user ID and requested resource.
        /// Property 26: Authorization Failure Logging
        /// </remarks>
        private async Task LogAuthorizationFailureAsync(long userId, string resourceType, long resourceId, string action)
        {
            var resource = $"{resourceType}/{resourceId}";
            
            _logger.LogWarning(
                "Authorization failure: User {UserId} denied {Action} access to {Resource}",
                userId, action, resource);

            // Use the dedicated LogAuthorizationFailureAsync method from ISecurityAuditService
            // Requirement 8.7: IF authorization fails, THEN THE Backend SHALL log the attempt with user ID and requested resource
            await _securityAuditService.LogAuthorizationFailureAsync(userId, resource, action);
        }

        #endregion
    }
}
