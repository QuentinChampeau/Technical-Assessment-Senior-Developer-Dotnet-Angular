using WebApi.Domain.Entities;

namespace WebApi.Repositories;

public interface IAuditRepository
{
    Task AddAsync(AuditEntry auditEntry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditEntry>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken);
}