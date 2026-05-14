namespace WebApi.Features.Expenses.Commands.Delete;

public class DeleteExpenseCommandHandler(AppDbContext context)
    : IRequestHandler<DeleteExpenseCommand>
{
    public async Task Handle(
        DeleteExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var expense = await context.Expenses.FindAsync([request.Id], cancellationToken);
        if (expense is null) return;

        context.Expenses.Remove(expense);

        await context.SaveChangesAsync(cancellationToken);
    }
}