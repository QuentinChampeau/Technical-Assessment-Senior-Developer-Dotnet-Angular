namespace WebApi.Domain.Entities;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}