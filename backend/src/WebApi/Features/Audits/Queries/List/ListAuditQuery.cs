
using MediatR;
using WebApi.Features.Audits.DTOs;

namespace WebApi.Features.Audits.Queries.List;

public sealed record ListAuditQuery(
    Guid Id
) : IRequest<IReadOnlyCollection<AuditDto>>;
