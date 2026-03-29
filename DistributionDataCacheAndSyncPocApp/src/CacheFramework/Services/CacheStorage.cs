using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

namespace CacheFramework.Services;

public class CacheStorage : ICacheStorage
{
    private readonly ConcurrentDictionary<ShapeType, ShapeData> _storage = new();

    public void Store(ShapeType shape, ShapeData data)
    {
        _storage[shape] = data;
    }

    public ShapeData? Get(ShapeType shape)
    {
        return _storage.TryGetValue(shape, out var data) ? data : null;
    }
}
