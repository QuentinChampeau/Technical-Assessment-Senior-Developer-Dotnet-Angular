using MediatR;

namespace WebApi.Features.Expenses.Commands.Delete;

public record DeleteExpenseCommand(Guid Id) : IRequest;
