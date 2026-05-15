using MediatR;
using Microsoft.EntityFrameworkCore;
using WebApi.Common.Caching;
using WebApi.Domain.Entities;
using WebApi.Features.Audits.DTOs;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Features.Audits.Queries.List;

public class ListAuditQueryHandler(AppDbContext dbContext,
    ICacheService cacheService,
    ILogger<ListAuditQueryHandler> logger
    ) : IRequestHandler<ListAuditQuery, IReadOnlyCollection<AuditDto>>
{
    private static string GetExpenseHistoryCacheKey(Guid id) => $"expenses:{id}:history";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyCollection<AuditDto>> Handle(
        ListAuditQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving audit history for expense {ExpenseId}.", query.Id);

        var cacheKey = GetExpenseHistoryCacheKey(query.Id);

        var cachedHistory = await cacheService.GetAsync<IReadOnlyCollection<AuditDto>>(
                   cacheKey,
                   cancellationToken);

        if (cachedHistory is not null)
        {
            logger.LogInformation("Audit history cache hit for expense {ExpenseId}.", query.Id);
            return cachedHistory;
        }

        logger.LogInformation("Audit history cache miss for expense {ExpenseId}. Querying repository.", query.Id);

        var history = await dbContext.AuditEntries
            .AsNoTracking()
            .Where(x => x.EntityName == nameof(Expense) && x.EntityId == query.Id.ToString())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditDto(x.Id, x.EntityName, x.EntityId, x.Action, x.ChangesJson, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var result = history.Select(x => new AuditDto(x.Id, x.EntityName, x.EntityId, x.Action, x.ChangesJson, x.CreatedAtUtc)).ToList();

        await cacheService.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        logger.LogInformation(
            "Audit history retrieved and cached for expense {ExpenseId}. EntryCount={EntryCount}.",
            query.Id,
            result.Count);

        return result;
    }

}