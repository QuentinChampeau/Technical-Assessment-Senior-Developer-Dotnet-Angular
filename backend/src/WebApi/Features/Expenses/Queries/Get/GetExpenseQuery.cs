using MediatR;
using WebApi.Features.Expenses.DTOs;

namespace WebApi.Features.Expenses.Queries.Get;

public record GetExpenseQuery(Guid Id) : IRequest<ExpenseDto?>;
