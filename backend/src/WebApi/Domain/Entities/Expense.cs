namespace WebApi.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category  { get; set; } = string.Empty;
    public DateTime Date  { get; set; }
    public DateTime CreatedAtUtc  { get; set; }
    public DateTime UpdatedAtUtc  { get; set; }
}