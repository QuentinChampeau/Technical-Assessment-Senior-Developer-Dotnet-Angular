using System.Text.Json;
using StackExchange.Redis;

namespace WebApi.Common.Caching;

public sealed class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        var json = value.ToString();

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await database.StringSetAsync(key, serializedValue, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await database.KeyDeleteAsync(key);
    }

    public async Task RegisterKeyAsync(string setKey, string key, CancellationToken cancellationToken)
    {
        await database.SetAddAsync(setKey, key);
    }

    public async Task RemoveRegisteredKeysAsync(string setKey, CancellationToken cancellationToken)
    {
        var keys = await database.SetMembersAsync(setKey);

        foreach (var key in keys)
        {
            await database.KeyDeleteAsync(key.ToString());
        }

        await database.KeyDeleteAsync(setKey);
    }
}