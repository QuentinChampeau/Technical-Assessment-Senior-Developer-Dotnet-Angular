namespace WebApi.Features.Expenses.Commands.Create;

public record CreateExpenseCommand(
    string Description,
    double Amount,
    string Category,
    DateTime Date
) : IRequest<Guid>;
