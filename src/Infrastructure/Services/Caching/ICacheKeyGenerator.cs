using System;

namespace Infrastructure.Services.Caching;

/// <summary>
/// Interface for generating cache keys for different entity types and operations
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for a specific entity by ID
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="id">The entity ID</param>
    /// <returns>Cache key string</returns>
    string GenerateEntityKey<TEntity>(Guid id) where TEntity : class;

    /// <summary>
    /// Generates a cache key for a list of entities
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>Cache key string</returns>
    string GenerateListKey<TEntity>() where TEntity : class;

    /// <summary>
    /// Generates a cache key for an entity by name (for entities that support name-based lookup)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="name">The entity name</param>
    /// <returns>Cache key string</returns>
    string GenerateNameKey<TEntity>(string name) where TEntity : class;

    /// <summary>
    /// Generates a cache key pattern for bulk operations
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>Cache key pattern string</returns>
    string GeneratePatternKey<TEntity>() where TEntity : class;
}
