namespace WebApi.Common.Caching;

/// <summary>
/// Stores and retrieves serialized DTOs in Redis.
/// DTOs are cached instead of EF entities to avoid tracking issues
/// and keep cache payloads persistence-agnostic.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
    Task RegisterKeyAsync(string setKey, string key, CancellationToken cancellationToken);
    Task RemoveRegisteredKeysAsync(string setKey, CancellationToken cancellationToken);
}