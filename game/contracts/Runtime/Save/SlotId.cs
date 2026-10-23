using Campus.Contracts.Maps;

namespace Campus.Contracts.Save;

[DataId]
public readonly record struct SlotId(string Value);   // 存档位目录名就是 Value

/// <summary>The production auto-save directories, shared by slot discovery, input parsing and file validation.</summary>
public static class SaveSlots
{
    public const int AutoLimit = 3;
    public const int ManualLimit = 10;
    public static IReadOnlyList<SlotId> Auto { get; } = Array.AsReadOnly<SlotId>([new("s1"), new("s2"), new("s3")]);
}
