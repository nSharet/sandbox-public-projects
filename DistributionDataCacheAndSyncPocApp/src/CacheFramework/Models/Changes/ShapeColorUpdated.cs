using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

namespace CacheFramework.Models.Changes;

/// <summary>
/// Represents a color update that has been applied.
/// </summary>
public class ShapeColorUpdated : IShapeChange
{
    public ShapeType Shape { get; init; }

    public ChangeType ChangeType => ChangeType.Updated;

    public required ShapeColor NewColor { get; init; }
}
