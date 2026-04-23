using WebApi.Domain.Entities;

namespace WebApi.Repositories.Interfaces;

public interface IExpenseRepository
{
    Task AddAsync(Expense expense, CancellationToken cancellationToken);
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Expense> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? category,
        string? search,
        CancellationToken cancellationToken);
    void Delete(Expense entity);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}