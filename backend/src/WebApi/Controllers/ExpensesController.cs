using Microsoft.AspNetCore.Mvc;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expenses")]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var createdExpense = await expenseService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetExpenseById),
            new { id = createdExpense.Id },
            createdExpense);
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
        var result = await expenseService.GetPagedAsync(
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
        var expense = await expenseService.GetByIdAsync(id, cancellationToken);

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
    [FromBody] CreateExpenseRequest request,
    CancellationToken cancellationToken)
    {
        var updatedExpense = await expenseService.UpdateAsync(id, request, cancellationToken);

        if (updatedExpense is null)
        {
            return NotFound();
        }

        return Ok(updatedExpense);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpenseById(
    Guid id,
    CancellationToken cancellationToken)
    {
        var isDeleted = await expenseService.DeleteByIdAsync(id, cancellationToken);

        if (!isDeleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AuditEntryResponse>>> GetExpenseHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await expenseService.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}