using MediatR;

namespace WebApi.Features.Expenses.Commands.Create;

public record CreateExpenseCommand(
    string Description,
    decimal Amount,
    string Category,
    DateTime Date
) : IRequest<Guid>;
