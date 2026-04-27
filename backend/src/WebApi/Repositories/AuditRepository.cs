using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Persistence;
using WebApi.Repositories.Interfaces;

namespace WebApi.Repositories;

public sealed class AuditRepository(AppDbContext dbContext) : IAuditRepository
{
    public async Task AddAsync(AuditEntry auditEntry, CancellationToken cancellationToken)
    {
        await dbContext.AuditEntries.AddAsync(auditEntry, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditEntry>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AuditEntries
            .AsNoTracking()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}