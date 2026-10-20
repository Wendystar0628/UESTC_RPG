using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public sealed record LightEmitter(ChannelId Channel, Vector3 Position, PaletteToken Color, float Energy, float Range);

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

/// <summary>Resident map lighting declarations and the mandatory prepacked lamp catalog, published as one snapshot.</summary>
public sealed record LightInventoryDocument(MapId Map,
    IReadOnlyList<LightEmitter> Lamps, IReadOnlyList<WindowFacadeDecl> Facades, IReadOnlyList<WindowDecl> Windows,
    [property: JsonRequired] NightlightCatalog Nightlight)
{
    public WindowNumberingSnapshot? WindowNumbering { get; init; }
}

/// <summary>Map lighting declarations from source layouts, without building tile geometry.</summary>
public interface ILightMetadataSource
{
    IReadOnlyList<LightEmitter> DescribeLights(BuildContext context);
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
        "lamp.court", "sign.shop", "sign.signal", "reserved.23",
        "lamp.fountain", "clock.face", "lamp.aviation", "water.pool",
        "reserved.28", "reserved.29", "reserved.30", "sys.poolGain"
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
