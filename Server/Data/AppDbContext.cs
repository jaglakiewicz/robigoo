/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Server.Models;
using System.Security.Claims;

#endregion

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        #region Declarations

        private readonly IHttpContextAccessor _httpContextAccessor;

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
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<ChangeLog> ChangeLogs { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }

        #endregion

        #region Methods - Public

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        #endregion

        #region Methods - Private

        private List<AuditEntry> OnBeforeSaveChanges()
        {
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

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
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

            // Save the logs
            await base.SaveChangesAsync();
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
