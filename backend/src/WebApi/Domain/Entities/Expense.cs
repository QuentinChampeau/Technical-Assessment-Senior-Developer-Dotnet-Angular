namespace WebApi.Domain.Entities;

public sealed class Expense
{
     public Guid Id { get; set; }
     public string Description { get; set; } = string.Empty;
     public decimal Amount { get; set; }
     public string Category { get; set; } = string.Empty;
     public DateTime Date { get; set; }
     public DateTime CreatedAtUtc { get; set; }
     public DateTime UpdatedAtUtc { get; set; }

     public void Update(string description, decimal amount, string category, DateTime date)
     {
          this.Description = description;
          this.Amount = amount;
          this.Category = category;
          this.Date = date;
          this.UpdatedAtUtc = DateTime.Now;
     }
}