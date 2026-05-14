namespace WebApi.Features.Expenses.Commands.Update;

public class UpdateExpenseCommandHandler(AppDbContext context)
    : IRequestHandler<UpdateExpenseCommand, bool>
{
    public async Task<bool> Handle(
        UpdateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await context.Expenses.FindAsync([command.Id], cancellationToken);
        if (expense is null) return false;
        expense.Update(command.Description, command.Amount, command.Category, command.Date);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
