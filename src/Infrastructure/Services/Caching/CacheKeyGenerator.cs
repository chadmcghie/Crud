using System;
using System.Text;

namespace Infrastructure.Services.Caching;

/// <summary>
/// Default implementation of cache key generation strategy
/// </summary>
public class CacheKeyGenerator : ICacheKeyGenerator
{
    private const string EntityKeyPrefix = "entity";
    private const string ListKeyPrefix = "list";
    private const string NameKeyPrefix = "name";
    private const string KeySeparator = ":";

    public string GenerateEntityKey<TEntity>(Guid id) where TEntity : class
    {
        var entityName = GetEntityName<TEntity>();
        return $"{EntityKeyPrefix}{KeySeparator}{entityName}{KeySeparator}{id:N}";
    }

    public string GenerateListKey<TEntity>() where TEntity : class
    {
        var entityName = GetEntityName<TEntity>();
        return $"{ListKeyPrefix}{KeySeparator}{entityName}";
    }

    public string GenerateNameKey<TEntity>(string name) where TEntity : class
    {
        var entityName = GetEntityName<TEntity>();
        var normalizedName = NormalizeName(name);
        return $"{NameKeyPrefix}{KeySeparator}{entityName}{KeySeparator}{normalizedName}";
    }

    public string GeneratePatternKey<TEntity>() where TEntity : class
    {
        var entityName = GetEntityName<TEntity>();
        return $"{EntityKeyPrefix}{KeySeparator}{entityName}{KeySeparator}*";
    }

    private static string GetEntityName<TEntity>() where TEntity : class
    {
        var typeName = typeof(TEntity).Name;

        // Remove common suffixes and convert to lowercase
        if (typeName.EndsWith("Entity"))
        {
            typeName = typeName[..^6]; // Remove "Entity" suffix
        }

        return typeName.ToLowerInvariant();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "empty";
        }

        // Convert to lowercase and replace spaces/special characters with underscores
        var normalized = new StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                normalized.Append(c);
            }
            else
            {
                normalized.Append('_');
            }
        }

        return normalized.ToString();
    }
}
