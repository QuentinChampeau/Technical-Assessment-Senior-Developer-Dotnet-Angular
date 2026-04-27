using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Domain.Entities;
using WebApi.Common.Caching;
using WebApi.Repositories;

namespace WebApi.Services;

public sealed class ExpenseService(
    IExpenseRepository expenseRepository,
    IAuditRepository auditRepository,
    ICacheService cacheService,
    ILogger<ExpenseService> logger) : IExpenseService
{
    // Tracks all paginated list cache keys so they can be invalidated
    // efficiently after write operations.
    private const string ExpenseListCacheKeysSet = "expenses:list:keys";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static string GetExpenseCacheKey(Guid id) => $"expenses:{id}";
    private static string GetExpenseHistoryCacheKey(Guid id) => $"expenses:{id}:history";

    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var now = DateTime.UtcNow;

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Category = request.Category.Trim(),
            Date = request.Date,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await expenseRepository.AddAsync(expense, cancellationToken);

        await auditRepository.AddAsync(new AuditEntry
        {
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Action = "Created",
            ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                expense.Id,
                expense.Description,
                expense.Amount,
                expense.Category,
                expense.Date,
                expense.CreatedAtUtc,
                expense.UpdatedAtUtc
            }),
            CreatedAtUtc = now
        }, cancellationToken);

        await expenseRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(GetExpenseHistoryCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveRegisteredKeysAsync(ExpenseListCacheKeysSet, cancellationToken);

        logger.LogInformation("Expense {ExpenseId} created.", expense.Id);

        return MapToResponse(expense);
    }

    public async Task<PagedResponse<ExpenseResponse>> GetPagedAsync(
        int page,
        int pageSize,
        string? category,
        string? search,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        // Normalization
        category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "date" : sortBy.Trim().ToLowerInvariant();
        sortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : "desc";

        var cacheKey =
    $"expenses:list:page={page}:pageSize={pageSize}:category={category}:search={search}:sortBy={sortBy}:sortDirection={sortDirection}";

        var cachedResult = await cacheService.GetAsync<PagedResponse<ExpenseResponse>>(
            cacheKey,
            cancellationToken);

        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var (items, totalCount) = await expenseRepository.GetPagedAsync(
            page,
            pageSize,
            category,
            search,
            sortBy,
            sortDirection,
            cancellationToken);

        var mappedItems = items.Select(MapToResponse).ToList();

        var result = new PagedResponse<ExpenseResponse>
        {
            Items = mappedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        await cacheService.SetAsync(cacheKey, result, CacheDuration, cancellationToken);
        await cacheService.RegisterKeyAsync(ExpenseListCacheKeysSet, cacheKey, cancellationToken);

        return result;
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = GetExpenseCacheKey(id);

        var cachedExpense = await cacheService.GetAsync<ExpenseResponse>(
            cacheKey,
            cancellationToken);

        if (cachedExpense is not null)
        {
            return cachedExpense;
        }

        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            return null;
        }

        var response = MapToResponse(expense);

        await cacheService.SetAsync(cacheKey, response, CacheDuration, cancellationToken);

        return response;
    }

    public async Task<ExpenseResponse?> UpdateAsync(
    Guid id,
    CreateExpenseRequest request,
    CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var oldValues = new
        {
            expense.Description,
            expense.Amount,
            expense.Category,
            expense.Date
        };

        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.Category = request.Category.Trim();
        expense.Date = request.Date;
        expense.UpdatedAtUtc = now;

        var newValues = new
        {
            expense.Description,
            expense.Amount,
            expense.Category,
            expense.Date
        };

        await auditRepository.AddAsync(new AuditEntry
        {
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Action = "Updated",
            ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                OldValue = oldValues,
                NewValue = newValues
            }),
            CreatedAtUtc = now
        }, cancellationToken);

        await expenseRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(GetExpenseCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveAsync(GetExpenseHistoryCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveRegisteredKeysAsync(ExpenseListCacheKeysSet, cancellationToken);

        logger.LogInformation("Expense {ExpenseId} updated.", expense.Id);

        return MapToResponse(expense);
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;


        await auditRepository.AddAsync(new AuditEntry
        {
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Action = "Deleted",
            ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                expense.Id,
                expense.Description,
                expense.Amount,
                expense.Category,
                expense.Date,
                expense.CreatedAtUtc,
                expense.UpdatedAtUtc
            }),
            CreatedAtUtc = now
        }, cancellationToken);

        expenseRepository.Delete(expense);
        await expenseRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(GetExpenseCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveAsync(GetExpenseHistoryCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveRegisteredKeysAsync(ExpenseListCacheKeysSet, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<AuditEntryResponse>> GetHistoryAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        var cacheKey = GetExpenseHistoryCacheKey(id);

        var cachedHistory = await cacheService.GetAsync<IReadOnlyCollection<AuditEntryResponse>>(
            cacheKey,
            cancellationToken);

        if (cachedHistory is not null)
        {
            return cachedHistory;
        }

        var history = await auditRepository.GetEntityHistoryAsync(
            nameof(Expense),
            id.ToString(),
            cancellationToken);

        var result = history.Select(x => new AuditEntryResponse
        {
            Id = x.Id,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            Action = x.Action,
            ChangesJson = x.ChangesJson,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

        await cacheService.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        return result;
    }

    private static ExpenseResponse MapToResponse(Expense expense) =>
        new()
        {
            Id = expense.Id,
            Description = expense.Description,
            Amount = expense.Amount,
            Category = expense.Category,
            Date = expense.Date,
            CreatedAtUtc = expense.CreatedAtUtc,
            UpdatedAtUtc = expense.UpdatedAtUtc
        };

    private static void ValidateRequest(CreateExpenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.", nameof(request));
        }

        if (request.Description.Trim().Length < 5)
        {
            throw new ArgumentException("Description must contain at least 5 characters.", nameof(request));
        }

        if (request.Description.Trim().Length > 200)
        {
            throw new ArgumentException("Description cannot exceed 200 characters.", nameof(request));
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request));
        }

        if (request.Category.Trim().Length > 100)
        {
            throw new ArgumentException("Category cannot exceed 100 characters.", nameof(request));
        }
    }
}