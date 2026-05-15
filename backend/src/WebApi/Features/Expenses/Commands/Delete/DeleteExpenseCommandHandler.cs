using MediatR;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Features.Expenses.Commands.Delete;

public class DeleteExpenseCommandHandler(AppDbContext context)
    : IRequestHandler<DeleteExpenseCommand, bool>
{
    public async Task<bool> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.Expenses.FindAsync([request.Id], cancellationToken);

        if (expense is null) return false;

        context.Expenses.Remove(expense);

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}