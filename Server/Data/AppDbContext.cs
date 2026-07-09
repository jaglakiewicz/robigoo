#region Imports

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Server.Exceptions;
using Server.Models;
using System.Security.Claims;

#endregion

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        #region Declarations

        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _isAuditingSuppressed = false;

        #endregion

        #region Constructor

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Properties

        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<InspectionProtocol> InspectionProtocols { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<CropSprayer> CropSprayers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ChangeLog> ChangeLogs { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<SecurityEventLog> SecurityEventLogs { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        #endregion

        #region Methods - Protected

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Map CropSprayer entity to existing Machines table
            modelBuilder.Entity<CropSprayer>().ToTable("Machines");

            // Configure optimistic concurrency for CropSprayer
            // Requirement 4.1: THE Database_Access_Layer SHALL implement optimistic concurrency control using row version tokens
            modelBuilder.Entity<CropSprayer>()
                .Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure optimistic concurrency for InspectionProtocol
            // Requirement 4.1: THE Database_Access_Layer SHALL implement optimistic concurrency control using row version tokens
            modelBuilder.Entity<InspectionProtocol>()
                .Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure Version as a concurrency token for application-level versioning
            modelBuilder.Entity<InspectionProtocol>()
                .Property(e => e.Version)
                .IsConcurrencyToken();

            // Configure indexes for security event queries
            // Requirement 7.2: Support efficient querying of security events
            modelBuilder.Entity<SecurityEventLog>()
                .HasIndex(e => e.OccurredAt);
            
            modelBuilder.Entity<SecurityEventLog>()
                .HasIndex(e => new { e.EventType, e.OccurredAt });
            
            modelBuilder.Entity<SecurityEventLog>()
                .HasIndex(e => new { e.IpAddress, e.OccurredAt });
        }

        #endregion

        #region Methods - Public

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                await OnAfterSaveChanges(auditEntries, cancellationToken);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                // Requirement 4.2: WHEN two users attempt to modify the same record simultaneously, 
                // THE Concurrency_Controller SHALL detect the conflict
                throw new ConcurrencyException("The record was modified by another user. Please refresh and try again.", ex);
            }
        }

        #endregion

        #region Methods - Private

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            // Skip auditing if suppressed (e.g., when saving audit logs themselves)
            if (_isAuditingSuppressed)
                return new List<AuditEntry>();

            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var user = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value 
                       ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name 
                       ?? "Unknown";

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ChangeLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name; 
                auditEntry.UserId = user;
                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary)
                    {
                        // Values will be handled in OnAfterSaveChanges
                        auditEntry.TemporaryProperties.Add(property);
                        continue;
                    }

                    string? propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.Changes.Add($"'{propertyName}': {property.CurrentValue}");
                            break;

                        case EntityState.Deleted:
                            // Nie logujemy wszystkich pół przy usuwaniu, tylko fakt usunięcia
                            // auditEntry.Changes.Add($"'{propertyName}' was: {property.OriginalValue}");
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                var original = property.OriginalValue?.ToString();
                                var current = property.CurrentValue?.ToString();
                                if (original != current)
                                {
                                    auditEntry.Changes.Add($"'{propertyName}': {original} -> {current}");
                                }
                            }
                            break;
                    }
                }
            }
            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken = default)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                // Get the final value of temporary properties (mostly PKs)
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        auditEntry.Changes.Add($"'{prop.Metadata.Name}': {prop.CurrentValue}");
                    }
                }

                // Create ChangeLog record
                var changeLog = auditEntry.ToChangeLog();
                if (changeLog != null)
                {
                    ChangeLogs.Add(changeLog);
                }
            }

            // Save the logs directly without triggering the audit trail again
            // This prevents infinite recursion by suppressing auditing for this save
            try
            {
                _isAuditingSuppressed = true;
                await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // If audit log save fails, don't fail the entire operation
                // The main entity save already succeeded
                // Log the error but continue
            }
            finally
            {
                _isAuditingSuppressed = false;
            }
        }

        #endregion

        #region Helper Classes

        private class AuditEntry
        {
            public EntityEntry Entry { get; }
            public EntityState State { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public Dictionary<string, object?> KeyValues { get; } = new Dictionary<string, object?>();
            public List<string> Changes { get; } = new List<string>();
            public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();

            public AuditEntry(EntityEntry entry)
            {
                Entry = entry;
                State = entry.State;
            }

            public ChangeLog? ToChangeLog()
            {
                if (Changes.Count == 0 && State == EntityState.Modified)
                {
                    return null;
                }

                var keyString = string.Join(", ", KeyValues.Values);
                var changesString = string.Join(", ", Changes);

                string typeOfChange = State == EntityState.Added ? "Utworzono" :
                                      State == EntityState.Deleted ? "Usunięto" : "Zmodyfikowano";

                string fullChanges = typeOfChange;
                if ((State == EntityState.Modified || State == EntityState.Added) && !string.IsNullOrEmpty(changesString))
                {
                     fullChanges += $": {changesString}";
                }
                
                return new ChangeLog
                {
                    EntityName = TableName,
                    EntityId = keyString,
                    Who = UserId,
                    When = DateTime.UtcNow,
                    Changes = fullChanges
                };
            }
        }

        #endregion
    }
}
