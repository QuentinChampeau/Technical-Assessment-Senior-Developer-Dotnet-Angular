using MediatR;
using Microsoft.EntityFrameworkCore;
using WebApi.Common.Caching;
using WebApi.Common.Pagination;
using WebApi.Domain.Entities;
using WebApi.Features.Expenses.DTOs;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Features.Expenses.Queries.List;

public class ListExpenseQueryHandler(AppDbContext dbContext,
        ICacheService cacheService,
        ILogger<ListExpenseQueryHandler> logger
    ) : IRequestHandler<ListExpenseQuery, PagedResponse<ExpenseDto>>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string ExpenseListCacheKeysSet = "expenses:list:keys";

    public async Task<PagedResponse<ExpenseDto>> Handle(
        ListExpenseQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);
        var category = string.IsNullOrWhiteSpace(query.Category) ? null : query.Category.Trim();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "date" : query.SortBy.Trim().ToLowerInvariant();
        var sortDirection = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : "desc";

        logger.LogInformation(
            "Retrieving expenses. Page={Page}, PageSize={PageSize}, Category={Category}, Search={Search}, SortBy={SortBy}, SortDirection={SortDirection}.",
            page,
            pageSize,
            category,
            search,
            sortBy,
            sortDirection);

        var cacheKey =
            $"expenses:list:page={page}:pageSize={pageSize}:category={category}:search={search}:sortBy={sortBy}:sortDirection={sortDirection}";

        var cachedResult = await cacheService.GetAsync<PagedResponse<ExpenseDto>>(
            cacheKey,
            cancellationToken);

        if (cachedResult is not null)
        {
            logger.LogInformation("Expense list cache hit. CacheKey={CacheKey}.", cacheKey);
            return cachedResult;
        }

        logger.LogInformation("Expense list cache miss. CacheKey={CacheKey}. Querying repository.", cacheKey);

        // Build query
        IQueryable<Expense> q = dbContext.Expenses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            q = q.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(x => x.Description.Contains(search));
        }

        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        q = sortBy?.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? q.OrderByDescending(x => x.Amount)
                : q.OrderBy(x => x.Amount),

            "category" => isDescending
                ? q.OrderByDescending(x => x.Category)
                : q.OrderBy(x => x.Category),

            "description" => isDescending
                ? q.OrderByDescending(x => x.Description)
                : q.OrderBy(x => x.Description),

            "date" or _ => isDescending
                ? q.OrderByDescending(x => x.Date)
                : q.OrderBy(x => x.Date)
        };

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ExpenseDto(
                item.Id,
                 item.Description,
                 item.Amount,
                 item.Category,
                 item.Date,
                 item.CreatedAtUtc,
                 item.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedResponse<ExpenseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        await cacheService.SetAsync(cacheKey, result, CacheDuration, cancellationToken);
        await cacheService.RegisterKeyAsync(ExpenseListCacheKeysSet, cacheKey, cancellationToken);

        return result;
    }
}
