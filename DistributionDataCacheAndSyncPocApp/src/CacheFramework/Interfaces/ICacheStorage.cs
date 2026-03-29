using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

namespace CacheFramework.Interfaces;

public interface ICacheStorage
{
    void Store(ShapeType shape, ShapeData data);

    ShapeData? Get(ShapeType shape);
}
