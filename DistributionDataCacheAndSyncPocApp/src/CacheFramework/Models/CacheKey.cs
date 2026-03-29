using CacheFramework.Models.Enums;

namespace CacheFramework.Models;

/// <summary>
/// Composite key for cache lookups.
/// </summary>
public readonly struct CacheKey : IEquatable<CacheKey>
{
    public CacheKey(ShapeType shape)
    {
        Shape = shape;
    }

    public ShapeType Shape { get; }

    public bool Equals(CacheKey other) => Shape == other.Shape;

    public override bool Equals(object? obj) => obj is CacheKey key && Equals(key);

    public override int GetHashCode() => Shape.GetHashCode();

    public override string ToString() => Shape.ToString();
}
