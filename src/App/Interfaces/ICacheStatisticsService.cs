using System.Threading;
using System.Threading.Tasks;

namespace App.Interfaces;

public interface ICacheStatisticsService
{
    Task<CacheStatistics> GetCurrentStatisticsAsync(CancellationToken cancellationToken = default);
    void RecordHit(string cacheType);
    void RecordMiss(string cacheType);
    void RecordOperation(string operationType, TimeSpan duration);
}

public class CacheStatistics
{
    public double HitRatio { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long KeyCount { get; set; }
    public double MemoryUsageMB { get; set; }
    public bool RedisConnected { get; set; }
    public TimeSpan Uptime { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public Dictionary<string, long> MostAccessedKeys { get; set; } = new();
    public Dictionary<string, long> HitsByType { get; set; } = new();
    public Dictionary<string, long> MissesByType { get; set; } = new();
}
