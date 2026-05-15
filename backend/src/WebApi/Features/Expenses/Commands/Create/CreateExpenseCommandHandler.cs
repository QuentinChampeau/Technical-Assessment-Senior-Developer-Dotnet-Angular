using MediatR;
using WebApi.Common.Caching;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Features.Expenses.Commands.Create;

public class CreateExpenseCommandHandler(AppDbContext context, ICacheService cacheService, ILogger<CreateExpenseCommandHandler> logger)
    : IRequestHandler<CreateExpenseCommand, Guid>
{
    private static string GetExpenseHistoryCacheKey(Guid id) => $"expenses:{id}:history";
    private const string ExpenseListCacheKeysSet = "expenses:list:keys";

    public async Task<Guid> Handle(
            CreateExpenseCommand command,
            CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating expense. Category={Category}, Amount={Amount}.",
            command.Category,
            command.Amount);

        var now = DateTime.UtcNow;

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Description = command.Description.Trim(),
            Amount = command.Amount,
            Category = command.Category.Trim(),
            Date = command.Date,
            CreatedAtUtc = now,
        };

        await context.Expenses.AddAsync(expense, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(GetExpenseHistoryCacheKey(expense.Id), cancellationToken);
        await cacheService.RemoveRegisteredKeysAsync(ExpenseListCacheKeysSet, cancellationToken);

        logger.LogInformation(
            "Expense {ExpenseId} created successfully and list cache invalidated.",
            expense.Id);

        return expense.Id;
    }
}
