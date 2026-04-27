namespace WebApi.Common.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
    Task RegisterKeyAsync(string setKey, string key, CancellationToken cancellationToken);
    Task RemoveRegisteredKeysAsync(string setKey, CancellationToken cancellationToken);
}