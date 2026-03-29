using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

namespace CacheFramework.Services;

public class TimestampManager : ITimestampManager
{
    private readonly ConcurrentDictionary<ShapeType, DateTime> _timestamps = new();

    public void Update(ShapeType shape, DateTime? timestamp = null)
    {
        _timestamps[shape] = timestamp ?? DateTime.UtcNow;
    }

    public DateTime? Get(ShapeType shape)
    {
        return _timestamps.TryGetValue(shape, out var ts) ? ts : null;
    }
}
