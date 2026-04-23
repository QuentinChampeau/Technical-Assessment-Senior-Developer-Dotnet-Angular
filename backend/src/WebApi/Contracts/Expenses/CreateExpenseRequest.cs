using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Expenses;


public sealed class CreateExpenseRequest
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 99999.99)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }
}