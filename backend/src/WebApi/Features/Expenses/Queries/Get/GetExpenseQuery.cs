namespace WebApi.Features.Expenses.Queries.Get;

public record GetExpenseQuery(Guid Id) : IRequest<ExpenseDto?>;