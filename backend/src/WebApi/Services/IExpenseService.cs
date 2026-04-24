using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;

namespace WebApi.Services.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<ExpenseResponse>> GetPagedAsync(
        int page,
        int pageSize,
        string? category,
        string? search,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);
    Task<ExpenseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditEntryResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}