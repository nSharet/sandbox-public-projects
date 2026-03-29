using CacheFramework.Models.Enums;

namespace CacheFramework.Interfaces;

/// <summary>
/// Base interface for shape changes.
/// </summary>
public interface IShapeChange
{
    ShapeType Shape { get; }

    ChangeType ChangeType { get; }
}
