using ApiGateway.DTOs;

namespace ApiGateway.Services;

public interface ICacheMetricsService
{
    void RecordHit();
    void RecordMiss();
    void RecordEviction();
    CacheMetricsDto GetMetrics();
}

public class CacheMetricsService : ICacheMetricsService
{
    private long _hits;
    private long _misses;
    private long _evictions;

    public void RecordHit()
    {
        Interlocked.Increment(ref _hits);
    }

    public void RecordMiss()
    {
        Interlocked.Increment(ref _misses);
    }

    public void RecordEviction()
    {
        Interlocked.Increment(ref _evictions);
    }

    public CacheMetricsDto GetMetrics()
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var evictions = Interlocked.Read(ref _evictions);
        var total = hits + misses;
        var ratio = total > 0 ? Math.Round((double)hits / total, 4) : 0.0;

        return new CacheMetricsDto(hits, misses, total, ratio, evictions);
    }
}
