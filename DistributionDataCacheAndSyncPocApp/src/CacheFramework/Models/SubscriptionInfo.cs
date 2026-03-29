using CacheFramework.Models.Enums;

namespace CacheFramework.Models;

public class SubscriptionInfo
{
    public required string AppId { get; init; }

    public required ShapeType Shape { get; init; }

    public required FocusLevel FocusLevel { get; init; }

    public required DateTime LastSync { get; init; }
}
