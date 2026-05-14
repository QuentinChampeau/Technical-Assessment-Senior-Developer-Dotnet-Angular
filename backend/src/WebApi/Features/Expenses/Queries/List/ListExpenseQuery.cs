namespace WebApi.Features.Expenses.Queries.List;

public record ListExpenseQuery : IRequest<List<ExpenseDto>>;
