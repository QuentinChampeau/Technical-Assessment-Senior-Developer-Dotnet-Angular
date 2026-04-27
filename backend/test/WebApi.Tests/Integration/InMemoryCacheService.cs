using System.Collections.Concurrent;
using WebApi.Common.Caching;

namespace WebApi.Tests.Integration;

internal sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> cache = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> sets = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        if (value is not null)
        {
            cache[key] = value;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RegisterKeyAsync(string setKey, string key, CancellationToken cancellationToken)
    {
        var set = sets.GetOrAdd(setKey, _ => []);
        set.Add(key);

        return Task.CompletedTask;
    }

    public Task RemoveRegisteredKeysAsync(string setKey, CancellationToken cancellationToken)
    {
        if (sets.TryRemove(setKey, out var keys))
        {
            foreach (var key in keys)
            {
                cache.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }
}