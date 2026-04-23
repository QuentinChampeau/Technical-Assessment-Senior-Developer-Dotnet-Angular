using WebApi.Domain.Entities;

namespace WebApi.Repositories.Interfaces;

public interface IAuditRepository
{
    Task<IReadOnlyCollection<AuditEntry>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken);
}