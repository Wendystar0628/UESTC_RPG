using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Openings;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>
/// The zone's side of the map-wide window numbering (contract-29): sites declare named grids from their LOD-free layouts
/// (CenterEastModule.DescribeWindowLayouts), WP-14 allocates them, and every pane and declaration reads its base from the
/// kernel's snapshot in BuildContext.WindowNumbering. The zone never numbers a window by itself. Same rules as the south
/// zone's helper, kept in this assembly because zones do not reference each other (G4).
/// </summary>
public static class WindowNumbers
{
    /// <summary>
    /// The grid's allocation, or null when the context carries no snapshot: legacy and no-window contexts, where the glass stays
    /// unnumbered (WindowNumberingContracts). A snapshot of another map, or one without this grid, is an error.
    /// </summary>
    public static WindowAllocation? Allocation(BuildContext ctx, FeatureId feature, string key)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.WindowNumbering is not { } snapshot) return null;
        if (snapshot.Map != ctx.Map.Id)
            throw new InvalidOperationException($"Window numbering for map {snapshot.Map.Value} handed to a build of {ctx.Map.Id.Value}.");
        return snapshot.Get(feature, key);
    }

    /// <summary>The allocation window positions need: the kernel assigns the snapshot before it asks for them.</summary>
    public static WindowAllocation Required(BuildContext ctx, FeatureId feature, string key) =>
        Allocation(ctx, feature, key) ?? throw new InvalidOperationException("Window positions are described only after BuildContext.WindowNumbering is assigned.");

    /// <summary>Fails when a block's layout does not fit the grid it was declared into.</summary>
    public static void Fit(GridLayout layout, GridNumbering numbering, WindowAllocation allocation)
    {
        if (numbering.ColumnOffset < 0 || numbering.ColumnOffset + layout.Columns > allocation.Columns || layout.Rows != allocation.Rows)
            throw new InvalidOperationException(
                $"{numbering.Feature.Value} / {numbering.Key}: layout {layout.Columns}×{layout.Rows} at column {numbering.ColumnOffset} does not fit the allocated {allocation.Columns}×{allocation.Rows}.");
    }

    /// <summary>Fails when a set of individual openings does not match its grid.</summary>
    public static void Fit(int columns, int rows, FeatureId feature, string key, WindowAllocation allocation)
    {
        if (columns != allocation.Columns || rows != allocation.Rows)
            throw new InvalidOperationException($"{feature.Value} / {key}: {columns}×{rows} openings against the allocated {allocation.Columns}×{allocation.Rows}.");
    }

    /// <summary>One numbered rectangular pane per opening (WP-10 OpeningGlass).</summary>
    public static void Panes(SiteBuilder b, IEnumerable<GlazedOpening> openings, WindowAllocation allocation, ChannelId channel, byte card)
    {
        ArgumentNullException.ThrowIfNull(b);
        foreach (var o in openings)
            b.Add(OpeningGlass.Grid(o.Width, o.Height, 1, 1, allocation.BaseId, o.Column, o.Row, allocation.Columns, channel, card),
                OpeningPlacement.Frame(o.Origin, o.Outward));
    }

    /// <summary>The same openings as individual window declarations (ContentOutput.LooseWindows semantics).</summary>
    public static void Declare(ICollection<WindowDecl> windows, IEnumerable<GlazedOpening> openings, WindowAllocation allocation, ChannelId channel)
    {
        ArgumentNullException.ThrowIfNull(windows);
        foreach (var o in openings)
            windows.Add(OpeningGlass.Declare(OpeningPlacement.Frame(o.Origin, o.Outward), o.Width, o.Height, allocation.BaseId, o.Column, o.Row, allocation.Columns, channel));
    }
}

/// <summary>A glazed opening in a named grid: its cell, the bottom centre of the pane on the glass plane (world), the outward normal and the pane size.</summary>
public readonly record struct GlazedOpening(int Column, int Row, Vector3 Origin, Vector3 Outward, float Width, float Height);
