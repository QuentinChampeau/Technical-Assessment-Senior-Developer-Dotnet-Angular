using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IExpenseService _expenseService;

    public ExpensesController(IMediator mediator, IExpenseService expenseService)
    {
        this._mediator = mediator;
        this._expenseService = expenseService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense(
        [FromBody] CreateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expenseId = await _mediator.Send(command, ct);

        if (Guid.Empty == expenseId) return BadRequest();

        return Created($"/expenses/{expenseId}", new { id = expenseId });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> GetExpenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetPagedAsync(
            page,
            pageSize,
            category,
            search,
            sortBy,
            sortDirection,
            cancellationToken);

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
        if (id != command.Id) return Results.BadRequest();

        var result = await _mediator.Send(command, ct);

        return result ? Results.NoContent() : Results.NotFound();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpenseById(
    Guid id,
    CancellationToken cancellationToken)
    {
        await mediatr.Send(new DeleteExpenseCommand(id), ct);

        return Results.NoContent();
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AuditEntryResponse>>> GetExpenseHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await _expenseService.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}