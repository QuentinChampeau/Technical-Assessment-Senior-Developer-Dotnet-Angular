using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Persistence;
using WebApi.Repositories.Interfaces;

namespace WebApi.Repositories;

public sealed class ExpenseRepository(AppDbContext dbContext) : IExpenseRepository
{
    public async Task AddAsync(Expense expense, CancellationToken cancellationToken)
    {
        await dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Expenses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public void Delete(Expense entity)
    {
        dbContext.Expenses.Remove(entity);
    }

    public async Task<(IReadOnlyCollection<Expense> Items, int TotalCount)> GetPagedAsync(
     int page,
     int pageSize,
     string? category,
     string? search,
     string? sortBy,
     string? sortDirection,
     CancellationToken cancellationToken)
    {
        IQueryable<Expense> query = dbContext.Expenses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Description.Contains(search));
        }

        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy?.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? query.OrderByDescending(x => x.Amount)
                : query.OrderBy(x => x.Amount),

            "category" => isDescending
                ? query.OrderByDescending(x => x.Category)
                : query.OrderBy(x => x.Category),

            "description" => isDescending
                ? query.OrderByDescending(x => x.Description)
                : query.OrderBy(x => x.Description),

            "date" or _ => isDescending
                ? query.OrderByDescending(x => x.Date)
                : query.OrderBy(x => x.Date)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}