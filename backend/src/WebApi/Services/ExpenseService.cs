using System.Text.Json;
using WebApi.Common.Caching;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Domain.Entities;
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

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        logger.LogInformation(
            "Creating expense. Category={Category}, Amount={Amount}.",
            request.Category,
            request.Amount);

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
            ChangesJson = JsonSerializer.Serialize(new
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

        logger.LogInformation(
            "Expense {ExpenseId} created successfully and list cache invalidated.",
            expense.Id);

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

        category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "date" : sortBy.Trim().ToLowerInvariant();
        sortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
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

        var cachedResult = await cacheService.GetAsync<PagedResponse<ExpenseResponse>>(
            cacheKey,
            cancellationToken);

        if (cachedResult is not null)
        {
            logger.LogInformation("Expense list cache hit. CacheKey={CacheKey}.", cacheKey);
            return cachedResult;
        }

        logger.LogInformation("Expense list cache miss. CacheKey={CacheKey}. Querying repository.", cacheKey);

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

        logger.LogInformation(
            "Expense list retrieved and cached. CacheKey={CacheKey}, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}.",
            cacheKey,
            totalCount,
            mappedItems.Count);

        return result;
    }

    public async Task<ExpenseResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving expense {ExpenseId}.", id);

        var cacheKey = GetExpenseCacheKey(id);

        var cachedExpense = await cacheService.GetAsync<ExpenseResponse>(
            cacheKey,
            cancellationToken);

        if (cachedExpense is not null)
        {
            logger.LogInformation("Expense {ExpenseId} cache hit.", id);
            return cachedExpense;
        }

        logger.LogInformation("Expense {ExpenseId} cache miss. Querying repository.", id);

        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            logger.LogWarning("Expense {ExpenseId} was not found.", id);
            return null;
        }

        var response = MapToResponse(expense);

        await cacheService.SetAsync(cacheKey, response, CacheDuration, cancellationToken);

        logger.LogInformation("Expense {ExpenseId} retrieved and cached.", id);

        return response;
    }

    public async Task<ExpenseResponse?> UpdateAsync(
        Guid id,
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        logger.LogInformation("Updating expense {ExpenseId}.", id);

        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            logger.LogWarning("Expense {ExpenseId} could not be updated because it was not found.", id);
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
            ChangesJson = JsonSerializer.Serialize(new
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

        logger.LogInformation(
            "Expense {ExpenseId} updated successfully. Detail, history and list caches invalidated.",
            expense.Id);

        return MapToResponse(expense);
    }

    public async Task<bool> DeleteByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting expense {ExpenseId}.", id);

        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            logger.LogWarning("Expense {ExpenseId} could not be deleted because it was not found.", id);
            return false;
        }

        var now = DateTime.UtcNow;

        await auditRepository.AddAsync(new AuditEntry
        {
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Action = "Deleted",
            ChangesJson = JsonSerializer.Serialize(new
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

        logger.LogInformation(
            "Expense {ExpenseId} deleted successfully. Detail, history and list caches invalidated.",
            expense.Id);

        return true;
    }

    public async Task<IReadOnlyCollection<AuditEntryResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving audit history for expense {ExpenseId}.", id);

        var cacheKey = GetExpenseHistoryCacheKey(id);

        var cachedHistory = await cacheService.GetAsync<IReadOnlyCollection<AuditEntryResponse>>(
            cacheKey,
            cancellationToken);

        if (cachedHistory is not null)
        {
            logger.LogInformation("Audit history cache hit for expense {ExpenseId}.", id);
            return cachedHistory;
        }

        logger.LogInformation("Audit history cache miss for expense {ExpenseId}. Querying repository.", id);

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

        logger.LogInformation(
            "Audit history retrieved and cached for expense {ExpenseId}. EntryCount={EntryCount}.",
            id,
            result.Count);

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

        var description = request.Description.Trim();

        if (description.Length < 5)
        {
            throw new ArgumentException("Description must contain at least 5 characters.", nameof(request));
        }

        if (description.Length > 200)
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

        var category = request.Category.Trim();

        if (category.Length > 100)
        {
            throw new ArgumentException("Category cannot exceed 100 characters.", nameof(request));
        }
    }
}