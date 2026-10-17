using System.Numerics;

namespace Campus.Contracts;

/// <summary>
/// One facade's window layout. Origin is the lower-left opening corner of local column 0, row 0
/// on the outer facade surface. Right/Up are unit vectors; Right cross Up points outdoors.
/// SillY contains one height per row, relative to Origin along Up.
/// Window ID = BaseId + row * TotalColumns + ColumnStart + local column.
/// </summary>
public sealed record WindowFacadeDecl(int BaseId, int TotalColumns, ChannelId Channel,
    int ColumnStart, int Columns, int Rows, Vector3 Origin, Vector3 Right, Vector3 Up,
    float Pitch, float Width, float Height, IReadOnlyList<float> SillY)
{
    // Empty local cells, such as a doorway: row * Columns + local column.
    public IReadOnlyList<int> Skip { get; init; } = [];
}

/// <summary>An irregular individual window, sharing the glass surface's building-level numbering.</summary>
public sealed record WindowDecl(int Id, int BaseId, int TotalColumns, ChannelId Channel,
    Vector3 Center, Vector3 Right, Vector3 Up, float Width, float Height);

/// <summary>Map lighting declarations from source layouts, without building tile geometry.</summary>
public interface ILightMetadataSource
{
    IReadOnlyList<LightEmitter> DescribeLights(BuildContext context);
}

/// <summary>WP-02's map-wide baked lighting inventory, ready after Mount and before MapAware.OnMapLoaded.</summary>
public interface ILightInventory
{
    /// <summary>The same numbering snapshot used to bake these declarations. Formal inventories publish it atomically with their current map data.</summary>
    WindowNumberingSnapshot? WindowNumbering => null;
    long Rev { get; } // Increments when a map changes or further baked inventory is incorporated.
    IReadOnlyList<LightEmitter> Lamps { get; } // Includes outskirts. Consumers must not depend on list order.
    IReadOnlyList<WindowFacadeDecl> Facades { get; }
    IReadOnlyList<WindowDecl> Windows { get; }
}

/// <summary>Stable 32-slot ABI shared by vertex packing, light schedules and shaders. Names are ordinal and case-sensitive.</summary>
public static class LightChannelCatalog
{
    public const int Count = 32;
    private static readonly string[] Names =
    [
        "window.general", "window.dorm", "window.teach", "window.library",
        "window.lab", "window.canteen", "window.outskirts", "window.allnight",
        "window.cafe", "window.venue", "window.gym", "interior.public",
        "lamp.first", "lamp.first.half", "lamp.street", "lamp.landscape",
        "lamp.statue", "lamp.canopy", "lamp.pilotis", "lamp.corridor",
        "lamp.court", "sign.shop", "sign.signal", "lamp.cue",
        "lamp.fountain", "clock.face", "lamp.aviation", "water.pool",
        "reserved.28", "reserved.29", "sys.cue", "sys.poolGain"
    ];

    public static ChannelId Id(int slot)
    {
        if ((uint)slot >= Count) throw new ArgumentOutOfRangeException(nameof(slot));
        return new(Names[slot]);
    }

    public static int Slot(ChannelId id)
    {
        for (var slot = 0; slot < Count; slot++)
            if (string.Equals(Names[slot], id.Value, StringComparison.Ordinal)) return slot;
        return -1;
    }
}

/// <summary>
/// WP-03 service. Submit immediately uploads the 32x1 RGF light_channels texture in Channels phase.
/// Exactly 32 finite (L, delta) pairs are required; wrong length/non-finite input throws ArgumentException.
/// Window slots 0-11 use lit-window proportions (normally at most 0.85, library at most 0.9).
/// Lamp slots 12-27 use retained proportion * (1 + delta), so full retention lights every threshold kappa below 1.
/// lamp.cue normally equals lamp.first; only a cinematic override supplies the route front.
/// sys.cue uses L=0 normally, L=1 during override and delta=target effective solar altitude (possibly negative).
/// sys.poolGain uses L for pool-area gain (winter night 1.5); it must not be clamped to [0,1].
/// </summary>
public interface ILightChannelSink
{
    void Submit(ReadOnlySpan<Vector2> lDelta);
}
