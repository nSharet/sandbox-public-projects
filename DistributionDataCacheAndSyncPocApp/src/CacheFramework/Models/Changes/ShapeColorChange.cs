using CacheFramework.Models.Enums;

namespace CacheFramework.Models.Changes;

/// <summary>
/// Intent object representing a requested color change.
/// </summary>
public class ShapeColorChange
{
    public required ShapeType Shape { get; init; }

    public required ShapeColor NewColor { get; init; }
}
