using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Features.Audits.Queries.List;
using WebApi.Features.Expenses.Commands.Create;
using WebApi.Features.Expenses.Commands.Delete;
using WebApi.Features.Expenses.Commands.Update;
using WebApi.Features.Expenses.Queries.Get;
using WebApi.Features.Expenses.Queries.List;

namespace WebApi.Controllers;

[ApiController]
[Route("expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExpensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense(
        [FromBody] CreateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expenseId = await _mediator.Send(command, cancellationToken);

        if (Guid.Empty == expenseId) return BadRequest();

        return Created($"/expenses/{expenseId}", new { id = expenseId });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> GetExpenses(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortDirection = "desc")
    {
        var result = await _mediator.Send(
            new ListExpenseQuery(page, pageSize, category, search, sortBy, sortDirection), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> GetExpenseById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var expense = await _mediator.Send(new GetExpenseQuery(id), cancellationToken);

        if (expense is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expense not found",
                Detail = $"Expense '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(expense);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> UpdateExpense(
    Guid id,
    [FromBody] UpdateExpenseCommand command,
    CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();

        var result = await _mediator.Send(command, cancellationToken);

        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpenseById(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteExpenseCommand(id), cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AuditEntryResponse>>> GetExpenseHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await _mediator.Send(new ListAuditQuery(id), cancellationToken);
        return Ok(history);
    }
}