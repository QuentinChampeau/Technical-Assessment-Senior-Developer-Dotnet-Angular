namespace WebApi.Contracts.Expenses;

public sealed class AuditEntryResponse
{
    public long Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ChangesJson { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}