using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Persistence;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            AddAuditEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddAuditEntries(DbContext context)
    {
        var entries = context.ChangeTracker
            .Entries<Expense>()                           // scope to auditable entities
            .Where(e => e.State is EntityState.Added
                           or EntityState.Modified
                           or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => null
            };

            if (action is null) continue;

            string changesJson = action switch
            {
                "Created" => JsonSerializer.Serialize(new
                {
                    entry.Entity.Id,
                    entry.Entity.Description,
                    entry.Entity.Amount,
                    entry.Entity.Category,
                    entry.Entity.Date
                }),
                "Updated" => JsonSerializer.Serialize(new
                {
                    OldValue = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    NewValue = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue)
                }),
                _ => JsonSerializer.Serialize(new
                {
                    entry.Entity.Id,
                    entry.Entity.Description,
                    entry.Entity.Amount
                })
            };

            var audits = new List<AuditEntry>
            {
                new AuditEntry
                {
                    EntityName = nameof(Expense),
                    EntityId = entry.Entity.Id.ToString(),
                    Action = action,
                    ChangesJson = changesJson,
                    CreatedAtUtc = DateTime.UtcNow
                }
            };

            context.Set<AuditEntry>().AddRange(audits);
        }
    }
}