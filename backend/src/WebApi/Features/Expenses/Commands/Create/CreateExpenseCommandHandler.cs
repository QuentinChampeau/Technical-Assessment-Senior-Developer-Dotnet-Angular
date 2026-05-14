namespace WebApi.Features.Expenses.Commands.Create;

public class CreateExpenseCommandHandler(AppDbContext context)
    : IRequestHandler<CreateExpenseCommand, Guid>
{
    public async Task<Guid> Handle(
            CreateExpenseCommand command,
            CancellationToken cancellationToken)
    {
        var expense = new Expense(command.Name, command.Description, command.Price);

        await context.Expenses.AddAsync(expense, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return expense.Id;
    }
}
