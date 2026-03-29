using CacheFramework.Models.Enums;

namespace CacheFramework.Models.Changes;

/// <summary>
/// The data stored in cache for each shape.
/// </summary>
public class ShapeData
{
    public required ShapeType Shape { get; init; }

    public required ShapeColor Color { get; init; }

    public DateTime LastModified { get; init; } = DateTime.UtcNow;
}
