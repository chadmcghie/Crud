using System.Diagnostics;
using Api.Dtos;
using App.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = "AdminOnly")]
[Tags("Cache Management")]
public class CacheController : ControllerBase
{
    private readonly ICacheStatisticsService _statisticsService;
    private readonly ICacheManagementService _managementService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheStatisticsService statisticsService,
        ICacheManagementService managementService,
        ILogger<CacheController> logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current cache statistics including hit/miss ratios, memory usage, and key counts
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _statisticsService.GetCurrentStatisticsAsync(cancellationToken);

            var response = new CacheStatsResponse(
                stats.HitRatio,
                stats.TotalHits,
                stats.TotalMisses,
                stats.KeyCount,
                stats.MemoryUsageMB,
                stats.RedisConnected,
                FormatUptime(stats.Uptime),
                stats.AverageResponseTimeMs,
                stats.MostAccessedKeys,
                stats.HitsByType,
                stats.MissesByType
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "Error retrieving cache statistics" });
        }
    }

    /// <summary>
    /// Clear all cached items
    /// </summary>
    [HttpDelete("clear")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearAllCaches(CancellationToken cancellationToken)
    {
        try
        {
            var keyCountBefore = await _managementService.GetKeyCountAsync(cancellationToken);

            await _managementService.ClearAllAsync(cancellationToken);

            var response = new CacheClearResponse(
                true,
                keyCountBefore,
                "Cache cleared successfully"
            );

            _logger.LogInformation("All caches cleared by admin user. Keys removed: {KeysRemoved}", keyCountBefore);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all caches");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "Error clearing caches" });
        }
    }

    /// <summary>
    /// Clear cached items by pattern
    /// </summary>
    [HttpDelete("clear/{pattern}")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearCachesByPattern(string pattern, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return BadRequest(new { Message = "Pattern cannot be empty" });
            }

            // Get keys before clearing to count them
            var keysBefore = await _managementService.GetKeysAsync(pattern, int.MaxValue, cancellationToken);
            var keyCount = keysBefore.Count();

            await _managementService.ClearByPatternAsync(pattern, cancellationToken);

            var response = new CacheClearResponse(
                true,
                keyCount,
                $"Cache cleared successfully for pattern: {pattern}"
            );

            _logger.LogInformation("Cache cleared by pattern '{Pattern}' by admin user. Keys removed: {KeysRemoved}",
                pattern, keyCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = $"Error clearing cache by pattern: {pattern}" });
        }
    }

    /// <summary>
    /// Remove a specific cache key
    /// </summary>
    [HttpDelete("key/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveKey(string key, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new { Message = "Key cannot be empty" });
            }

            var exists = await _managementService.KeyExistsAsync(key, cancellationToken);
            if (!exists)
            {
                return NotFound(new { Message = $"Key '{key}' not found" });
            }

            await _managementService.RemoveKeyAsync(key, cancellationToken);

            _logger.LogInformation("Cache key '{Key}' removed by admin user", key);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = $"Error removing cache key: {key}" });
        }
    }

    /// <summary>
    /// Trigger cache warming for critical data
    /// </summary>
    [HttpPost("warm")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> WarmCaches(CancellationToken cancellationToken)
    {
        try
        {
            // Start cache warming in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _managementService.WarmCriticalDataAsync(cancellationToken);
                    _logger.LogInformation("Cache warming completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache warming");
                }
            }, cancellationToken);

            _logger.LogInformation("Cache warming initiated by admin user");

            return Accepted(new { Message = "Cache warming initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating cache warming");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "Error initiating cache warming" });
        }
    }

    /// <summary>
    /// Get cache keys by pattern
    /// </summary>
    [HttpGet("keys")]
    [ProducesResponseType(typeof(CacheKeyListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKeys(
        [FromQuery] string pattern = "*",
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = await _managementService.GetKeysAsync(pattern, limit, cancellationToken);
            var totalCount = await _managementService.GetKeyCountAsync(cancellationToken);

            var response = new CacheKeyListResponse(
                keys,
                totalCount,
                pattern
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache keys with pattern: {Pattern}", pattern);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "Error retrieving cache keys" });
        }
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }
        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m";
        }
        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }
}
