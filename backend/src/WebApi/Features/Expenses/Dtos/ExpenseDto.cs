
namespace WebApi.Features.Expenses.DTOs;

public record ExpenseDto(
    Guid Id,
    string Description,
    decimal Amount,
    string Category,
    DateTime Date,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);