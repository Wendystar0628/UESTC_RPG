using System.Numerics;

namespace Campus.Contracts;

/// <summary>WP-02's map-wide baked lighting inventory, ready after Mount and before MapAware.OnMapLoaded.</summary>
public interface ILightInventory
{
    /// <summary>The same numbering snapshot used to bake these declarations. Formal inventories publish it atomically with their current map data.</summary>
    WindowNumberingSnapshot? WindowNumbering { get; }
    long Rev { get; } // Increments when a map changes or further baked inventory is incorporated.
    IReadOnlyList<LightEmitter> Lamps { get; } // Includes outskirts. Consumers must not depend on list order.
    IReadOnlyList<WindowFacadeDecl> Facades { get; }
    IReadOnlyList<WindowDecl> Windows { get; }
    /// <summary>The complete resident lamp catalog; it does not change with tile demand or current time/weather.</summary>
    NightlightCatalog Nightlight { get; }
}

/// <summary>
/// WP-03 service. Submit immediately uploads the 32x1 RGF light_channels texture in Channels phase.
/// Exactly 32 finite (L, delta) pairs are required; wrong length/non-finite input throws ArgumentException.
/// Window slots 0-11 use lit-window proportions (normally at most 0.85, library at most 0.9).
/// Lamp slots 12-22 and 24-27 use retained proportion * (1 + delta), so full retention lights every threshold kappa below 1.
/// sys.poolGain uses L for pool-area gain (winter night 1.5); it must not be clamped to [0,1].
/// </summary>
public interface ILightChannelSink
{
    void Submit(ReadOnlySpan<Vector2> lDelta);
}
