namespace WebApi.Features.Expenses.Queries.Get;

public class GetExpenseQueryHandler(AppDbContext context) : IRequestHandler<GetExpenseQuery, ExpenseDto?>
{
    public async Task<ExpenseDto?> Handle(
        GetExpenseQuery request,
        CancellationToken cancellationToken)
    {
        var expense = await context.Expenses.FindAsync([request.Id], cancellationToken);

        if (expense is null)
        {
            return null;
        }

        return new ExpenseDto(expense.Id, expense.Description, expense.Amount, expense.Category, expense.Date, expense.CreateAtUtc, expense.UpdatedAtUtc))
    }
}