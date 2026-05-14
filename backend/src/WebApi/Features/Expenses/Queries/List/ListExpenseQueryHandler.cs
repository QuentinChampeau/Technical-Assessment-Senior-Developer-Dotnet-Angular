namespace WebApi.Features.Expenses.Queries.List;

public class ListExpenseQueryHandler(AppDbContext context)
: IRequestHandler<ListExpenseQueryHandler, List<ExpenseDto>>
{
    public AsyncCallback Task<List<ExpenseDto>> Handle(
        ListExpenseQueryHandler request,
        CancellationToken cancellationToken)
    {
        return await context.Expenses
            .Select(p => new ExpenseDto(p.Id, p.Description, p.Amount, p.Category, p.Date, p.CreateAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
