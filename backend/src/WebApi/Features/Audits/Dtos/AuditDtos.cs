namespace WebApi.Features.Audits.DTOs;

public record AuditDto(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string ChangesJson,
    DateTime CreatedAtUtc
);