using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services.Caching;

public class CacheStatisticsService : ICacheStatisticsService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CacheStatisticsService> _logger;
    private readonly DateTime _startTime;
    
    // In-memory counters for tracking statistics
    private readonly ConcurrentDictionary<string, long> _hits = new();
    private readonly ConcurrentDictionary<string, long> _misses = new();
    private readonly ConcurrentDictionary<string, long> _keyAccess = new();
    private readonly ConcurrentQueue<double> _operationTimes = new();
    private readonly object _statsLock = new();

    public CacheStatisticsService(IConnectionMultiplexer? redis, ILogger<CacheStatisticsService> logger)
    {
        _redis = redis;
        _logger = logger;
        _startTime = DateTime.UtcNow;
    }

    public async Task<CacheStatistics> GetCurrentStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalHits = _hits.Values.Sum();
            var totalMisses = _misses.Values.Sum();
            var totalOperations = totalHits + totalMisses;
            
            var hitRatio = totalOperations > 0 ? (double)totalHits / totalOperations : 0.0;
            
            var redisConnected = false;
            var keyCount = 0L;
            var memoryUsageMB = 0.0;
            
            if (_redis?.IsConnected == true)
            {
                redisConnected = true;
                try
                {
                    var database = _redis.GetDatabase();
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    
                    // Get key count (using DBSIZE for performance)
                    keyCount = (long)await database.ExecuteAsync("DBSIZE");
                    
                    // Get memory usage - simplified for now
                    try
                    {
                        var info = await server.InfoAsync("memory");
                        // For now, we'll just set a default value since Redis info parsing is complex
                        // In a real implementation, you would parse the info dictionary properly
                        memoryUsageMB = 0.0; // Placeholder
                    }
                    catch
                    {
                        memoryUsageMB = 0.0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get Redis statistics");
                    redisConnected = false;
                }
            }

            // Calculate average response time
            var avgResponseTime = 0.0;
            lock (_statsLock)
            {
                if (_operationTimes.Count > 0)
                {
                    avgResponseTime = _operationTimes.Average();
                }
            }

            return new CacheStatistics
            {
                HitRatio = hitRatio,
                TotalHits = totalHits,
                TotalMisses = totalMisses,
                KeyCount = keyCount,
                MemoryUsageMB = memoryUsageMB,
                RedisConnected = redisConnected,
                Uptime = DateTime.UtcNow - _startTime,
                AverageResponseTimeMs = avgResponseTime,
                MostAccessedKeys = GetTopAccessedKeys(10),
                HitsByType = new Dictionary<string, long>(_hits),
                MissesByType = new Dictionary<string, long>(_misses)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            throw;
        }
    }

    public void RecordHit(string cacheType)
    {
        _hits.AddOrUpdate(cacheType, 1, (key, value) => value + 1);
        _logger.LogDebug("Cache hit recorded for type: {CacheType}", cacheType);
    }

    public void RecordMiss(string cacheType)
    {
        _misses.AddOrUpdate(cacheType, 1, (key, value) => value + 1);
        _logger.LogDebug("Cache miss recorded for type: {CacheType}", cacheType);
    }

    public void RecordOperation(string operationType, TimeSpan duration)
    {
        var durationMs = duration.TotalMilliseconds;
        
        lock (_statsLock)
        {
            _operationTimes.Enqueue(durationMs);
            
            // Keep only the last 1000 operation times to prevent memory growth
            while (_operationTimes.Count > 1000)
            {
                _operationTimes.TryDequeue(out _);
            }
        }
        
        _logger.LogDebug("Cache operation recorded: {OperationType} took {Duration}ms", operationType, durationMs);
    }

    private Dictionary<string, long> GetTopAccessedKeys(int count)
    {
        return _keyAccess
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}