using Microsoft.AspNetCore.OutputCaching;

namespace Api.Services;

/// <summary>
/// Service for invalidating output cache entries when data is modified
/// </summary>
public interface IOutputCacheInvalidationService
{
    /// <summary>
    /// Invalidates cache entries for a specific entity type
    /// </summary>
    Task InvalidateEntityCacheAsync(string entityType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates cache entries for a specific entity and its collection
    /// </summary>
    Task InvalidateEntityCacheAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates all cache entries with the given tags
    /// </summary>
    Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default);
}

public class OutputCacheInvalidationService : IOutputCacheInvalidationService
{
    private readonly IOutputCacheStore _cacheStore;
    private readonly ILogger<OutputCacheInvalidationService> _logger;

    public OutputCacheInvalidationService(
        IOutputCacheStore cacheStore,
        ILogger<OutputCacheInvalidationService> logger)
    {
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvalidateEntityCacheAsync(string entityType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
        }

        var tag = entityType.ToLowerInvariant();
        _logger.LogDebug("Invalidating cache for entity type: {EntityType} with tag: {Tag}", entityType, tag);
        
        try
        {
            await _cacheStore.EvictByTagAsync(tag, cancellationToken);
            _logger.LogInformation("Successfully invalidated cache for entity type: {EntityType}", entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for entity type: {EntityType}", entityType);
            // Don't throw - cache invalidation failures shouldn't break the operation
        }
    }

    public async Task InvalidateEntityCacheAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));
        }

        // Invalidate both the specific entity and the collection
        var tags = new[]
        {
            entityType.ToLowerInvariant(),
            $"{entityType.ToLowerInvariant()}:{entityId}"
        };

        _logger.LogDebug("Invalidating cache for entity: {EntityType}/{EntityId} with tags: {Tags}", 
            entityType, entityId, string.Join(", ", tags));
        
        await InvalidateByTagsAsync(tags, cancellationToken);
    }

    public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        if (tags == null || tags.Length == 0)
        {
            return;
        }

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            try
            {
                await _cacheStore.EvictByTagAsync(tag, cancellationToken);
                _logger.LogDebug("Successfully invalidated cache for tag: {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for tag: {Tag}", tag);
                // Continue with other tags even if one fails
            }
        }
    }
}
