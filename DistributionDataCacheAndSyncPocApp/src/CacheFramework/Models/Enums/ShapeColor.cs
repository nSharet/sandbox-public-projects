namespace CacheFramework.Models.Enums;

/// <summary>
/// Colors that can be applied to shapes.
/// </summary>
public enum ShapeColor
{
    Yellow,
    Blue,
    Red
}

public static class ShapeColorExtensions
{
    public static string ToHex(this ShapeColor color) => color switch
    {
        ShapeColor.Yellow => "#FFD700",
        ShapeColor.Blue => "#1E90FF",
        ShapeColor.Red => "#FF4444",
        _ => "#FFFFFF"
    };
}
