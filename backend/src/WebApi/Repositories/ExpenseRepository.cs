using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Persistence;
using WebApi.Repositories.Interfaces;

namespace WebApi.Repositories;

public sealed class ExpenseRepository(AppDbContext dbContext) : IExpenseRepository
{
    public async Task AddAsync(Expense expense, CancellationToken cancellationToken)
    {
        expense.CreatedAtUtc = DateTime.UtcNow;
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

        query = query.OrderByDescending(x => x.Date)
                     .ThenByDescending(x => x.CreatedAtUtc);

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