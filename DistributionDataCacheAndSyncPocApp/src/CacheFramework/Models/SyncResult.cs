using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

namespace CacheFramework.Models;

/// <summary>
/// Returned when a focus switch requires syncing data.
/// </summary>
public class SyncResult
{
    public required ShapeType Shape { get; init; }

    public required ShapeData Data { get; init; }

    public required DateTime Timestamp { get; init; }
}
