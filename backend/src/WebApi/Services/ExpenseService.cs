using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Domain.Entities;
using WebApi.Repositories.Interfaces;
using WebApi.Services.Interfaces;

namespace WebApi.Services;

public sealed class ExpenseService(
    IExpenseRepository expenseRepository,
    IAuditRepository auditRepository,
    ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Category = request.Category.Trim(),
            Date = request.Date
        };

        await expenseRepository.AddAsync(expense, cancellationToken);
        await expenseRepository.SaveChangesAsync(cancellationToken);

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

        var (items, totalCount) = await expenseRepository.GetPagedAsync(
            page,
            pageSize,
            category,
            search,
            sortBy,
            sortDirection,
            cancellationToken);

        var mappedItems = items.Select(MapToResponse).ToList();

        return new PagedResponse<ExpenseResponse>
        {
            Items = mappedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);
        if (expense is null)
        {
            return null;
        }

        var response = MapToResponse(expense);

        return response;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense is null)
        {
            return false;
        }

        expenseRepository.Delete(expense);
        await expenseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<AuditEntryResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var history = await auditRepository.GetEntityHistoryAsync(
            nameof(Expense),
            id.ToString(),
            cancellationToken);

        return history.Select(x => new AuditEntryResponse
        {
            Id = x.Id,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            Action = x.Action,
            ChangesJson = x.ChangesJson,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
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
}