using System.Text.Json.Serialization;

namespace WebApi.Infrastructure.Api;

internal static class UserEndpoints
{
    public sealed record UserInfo
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        [JsonPropertyName("employee_id")]
        public required Guid CompanyId { get; init; }
    }

    private static readonly Guid N2JSoftCompanyId = Guid.CreateVersion7();
    private static readonly Guid N2JSoftEmployeeId = Guid.CreateVersion7();
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/users/me", () => Results.Ok(new UserInfo
        {
            Id = N2JSoftEmployeeId,
            Name = "N2JSoft",
            CompanyId = N2JSoftCompanyId
        }));
        return endpoints;
    }
}