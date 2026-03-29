using CacheFramework.Models.Enums;

namespace CacheFramework.Interfaces;

public interface ITimestampManager
{
    void Update(ShapeType shape, DateTime? timestamp = null);

    DateTime? Get(ShapeType shape);
}
