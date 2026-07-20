using System.Text.Json;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigitalBookLibrary.Infrastructure.Auditing
{
    /// <summary>
    /// Writes an <see cref="AuditLog"/> row for every create/update/delete of an audited entity,
    /// capturing old/new values, the acting user and their IP (FR-AUD-1).
    /// Hooking SaveChanges means no service can forget to audit.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<string> AuditedEntities = new()
        {
            nameof(Book), nameof(Author), nameof(Category), nameof(UserAccount),
            // Granting or revoking a role — Admin above all — is the most security-sensitive change
            // in the system and must leave a trace. Its key is composite, so EntityId records the
            // UserId (the "who"); the full pair is in the old/new value JSON.
            nameof(UserRole)
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ICurrentUser _currentUser;
        private readonly List<PendingAudit> _pending = new();

        public AuditSaveChangesInterceptor(ICurrentUser currentUser) => _currentUser = currentUser;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _pending.Clear();

            if (eventData.Context is { } context)
            {
                Capture(context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            // Inserted rows only get their key after the save, so their audit rows are written here.
            // AuditLog itself is not audited, so this second save cannot recurse.
            if (eventData.Context is { } context && _pending.Count > 0)
            {
                foreach (var pending in _pending)
                {
                    context.Set<AuditLog>().Add(pending.ToAuditLog());
                }

                _pending.Clear();
                await context.SaveChangesAsync(cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void Capture(DbContext context)
        {
            context.ChangeTracker.DetectChanges();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                var entityName = entry.Entity.GetType().Name;
                if (!AuditedEntities.Contains(entityName) || entry.State is EntityState.Unchanged or EntityState.Detached)
                {
                    continue;
                }

                // OldValues must be read now — SaveChanges resets original values afterwards.
                // NewValues is deferred to after the save, so generated keys are real rather than
                // EF's temporary placeholders.
                var isAdded = entry.State is EntityState.Added;
                var wasDeleted = entry.State is EntityState.Deleted;

                _pending.Add(new PendingAudit(
                    entry,
                    entityName,
                    entry.State switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Modified => "Update",
                        EntityState.Deleted => "Delete",
                        _ => "Unknown"
                    },
                    IsAdded: isAdded,
                    WasDeleted: wasDeleted,
                    // An inserted row has only a temporary key here, so its id is resolved after the
                    // save; for updates/deletes the key is already final (and gone afterwards).
                    KnownEntityId: isAdded ? null : KeyOf(entry),
                    OldValues: isAdded ? null : Serialize(entry, original: true),
                    UserId: _currentUser.UserId,
                    IpAddress: _currentUser.IpAddress));
            }
        }

        /// <summary>Serializes the entry's values, skipping anything sensitive.</summary>
        private static string Serialize(EntityEntry entry, bool original)
        {
            var values = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    continue;   // never let secrets reach the audit trail
                }

                values[name] = original ? property.OriginalValue : property.CurrentValue;
            }

            return JsonSerializer.Serialize(values, JsonOptions);
        }

        private static string? KeyOf(EntityEntry entry)
            => entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

        private sealed record PendingAudit(
            EntityEntry Entry,
            string EntityName,
            string Action,
            bool IsAdded,
            bool WasDeleted,
            string? KnownEntityId,
            string? OldValues,
            int? UserId,
            string? IpAddress)
        {
            /// <summary>Built after SaveChanges, so generated keys and current values are final.</summary>
            public AuditLog ToAuditLog() => new()
            {
                EntityName = EntityName,
                EntityId = KnownEntityId ?? (IsAdded ? KeyOf(Entry) : null),
                Action = Action,
                OldValues = OldValues,
                NewValues = WasDeleted ? null : Serialize(Entry, original: false),
                UserId = UserId,
                IpAddress = IpAddress,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
