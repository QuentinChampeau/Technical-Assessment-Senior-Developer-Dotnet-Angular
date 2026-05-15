using MediatR;

namespace WebApi.Features.Expenses.Commands.Update;

public record UpdateExpenseCommand(
    Guid Id,
    string Description,
    double Amount,
    string Category,
    DateTime Date
) : IRequest<bool>;
