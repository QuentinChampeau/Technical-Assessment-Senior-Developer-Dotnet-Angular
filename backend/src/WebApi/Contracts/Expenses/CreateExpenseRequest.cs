using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Expenses;

public sealed class CreateExpenseRequest
{
    [Required(ErrorMessage = "Description is required.")]
    [MinLength(5, ErrorMessage = "Description must contain at least 5 characters.")]
    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string Description { get; init; } = string.Empty;

    [Range(0.01, 99999.99, ErrorMessage = "Amount must be between 0.01 and 99999.99.")]
    public decimal Amount { get; init; }

    [Required(ErrorMessage = "Category is required.")]
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    public string Category { get; init; } = string.Empty;

    [Required(ErrorMessage = "Date is required.")]
    public DateTime Date { get; init; }
}