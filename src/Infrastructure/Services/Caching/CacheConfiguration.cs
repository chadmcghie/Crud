using System;
using System.Collections.Generic;
using Domain.Entities;

namespace Infrastructure.Services.Caching;

/// <summary>
/// Default implementation of cache configuration with entity-specific TTL settings
/// </summary>
public class CacheConfiguration : ICacheConfiguration
{
    private readonly Dictionary<Type, TimeSpan> _entityTtlSettings;

    public CacheConfiguration()
    {
        _entityTtlSettings = new Dictionary<Type, TimeSpan>
        {
            // Core entities with longer TTL (they change less frequently)
            { typeof(Role), TimeSpan.FromMinutes(30) },
            { typeof(Person), TimeSpan.FromMinutes(15) },
            { typeof(Wall), TimeSpan.FromMinutes(10) },
            { typeof(Window), TimeSpan.FromMinutes(10) },
            
            // Authentication entities with shorter TTL (security sensitive)
            { typeof(Domain.Entities.Authentication.User), TimeSpan.FromMinutes(5) },
            { typeof(Domain.Entities.Authentication.RefreshToken), TimeSpan.FromMinutes(2) }
        };
    }

    public CacheEntryOptions GetCacheOptions<TEntity>() where TEntity : class
    {
        var entityType = typeof(TEntity);
        var ttl = GetTtlForEntity(entityType);

        return new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.Normal
        };
    }

    public CacheEntryOptions GetCacheOptions<TEntity>(TimeSpan ttl) where TEntity : class
    {
        return new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.Normal
        };
    }

    private TimeSpan GetTtlForEntity(Type entityType)
    {
        // Check for exact type match first
        if (_entityTtlSettings.TryGetValue(entityType, out var ttl))
        {
            return ttl;
        }

        // Check for base type matches (for inheritance scenarios)
        foreach (var kvp in _entityTtlSettings)
        {
            if (kvp.Key.IsAssignableFrom(entityType) || entityType.IsAssignableFrom(kvp.Key))
            {
                return kvp.Value;
            }
        }

        // Default TTL for unknown entity types
        return TimeSpan.FromMinutes(15);
    }
}
