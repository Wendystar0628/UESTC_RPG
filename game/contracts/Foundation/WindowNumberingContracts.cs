using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>A named rectangular logical grid, independent of LOD, tile order and the number of drawn panes.</summary>
/// <remarks>
/// Key is a stable, case-sensitive local name, not an array index or a display label. Columns and Rows are positive.
/// Blank cells, doors and other skipped cells still occupy IDs. Multiple panes of the same logical cell share its ID.
/// This is a size declaration only; it does not allocate a base or replace FacadeGrid geometry.
/// </remarks>
public sealed record WindowGridSpec(string Key, int Columns, int Rows);

/// <summary>Replaces every generic window block of one formally claimed facade feature.</summary>
/// <remarks>
/// Grids is the complete set for this feature, not an additional range. An empty list explicitly declares no windows.
/// A single custom grid may cover several source parts; several named grids may cover one custom building.
/// The current map must contain Feature. The collector validates the declaring module against ClaimedBy(Feature, Facade).
/// Ownership is taken from the registered module, never trusted from an author-supplied owner field.
/// </remarks>
public sealed record ClaimedWindowLayout(FeatureId Feature, IReadOnlyList<WindowGridSpec> Grids);

/// <summary>Full-map window positions using the existing lighting declarations and the authoritative numbering.</summary>
public sealed record WindowMetadata(IReadOnlyList<WindowFacadeDecl> Facades, IReadOnlyList<WindowDecl> Windows);

/// <summary>Optional two-phase pure metadata source implemented by an already registered IContentModule.</summary>
/// <remarks>
/// Neither method may call Tiles, Build or Collide, create meshes, perform IO, or use mutable runtime state.
/// Layouts are queried first, without an allocation, and must not allocate IDs or call WindowNumbering.For.
/// Positions are queried only after the kernel has assigned the authoritative BuildContext.WindowNumbering.
/// Both methods reuse the module's immutable layout also consumed by geometry generation.
/// </remarks>
public interface IWindowMetadataSource
{
    IReadOnlyList<ClaimedWindowLayout> DescribeWindowLayouts(IWorldModel world, uint worldSeed) => [];
    WindowMetadata DescribeWindows(BuildContext context);
}

/// <summary>Optional pure numbering capability of an already registered content module; implemented by WP-14 BuildingsModule.</summary>
/// <remarks>
/// The kernel selects exactly one provider from the current map's active modules. No KernelService or separate registry is used.
/// The kernel supplies validated, snapshotted layouts and calls this protocol rather than referencing a generator implementation.
/// The provider alone runs the WP-14 numbering algorithm. It must not call Tiles, Build or Collide, create geometry, perform IO,
/// consult mutable runtime state, or resolve a snapshot from a global side-table. The returned snapshot is published by the caller.
/// </remarks>
public interface IWindowNumberingProvider
{
    WindowNumberingSnapshot PrepareWindowNumbering(BuildContext prepass, IReadOnlyList<ClaimedWindowLayout> validatedLayouts);
}

/// <summary>A shared read-only view of WP-14's one allocation. This type does not allocate any IDs.</summary>
/// <remarks>
/// For generic buildings Key remains the existing BuildingPlanner source key, including any polygon suffix.
/// For a claimed facade it is the WindowGridSpec.Key. BaseId is shared by VF-B, WindowFacadeDecl and WindowDecl.
/// </remarks>
public readonly record struct WindowAllocation(FeatureId Feature, string Key, int BaseId, int Columns, int Rows)
{
    public int Count => checked(Columns * Rows);

    public int IdAt(int column, int row)
    {
        if (Columns <= 0 || Rows <= 0 || BaseId < 0)
            throw new InvalidOperationException("The window allocation is invalid.");
        if ((uint)column >= (uint)Columns) throw new ArgumentOutOfRangeException(nameof(column));
        if ((uint)row >= (uint)Rows) throw new ArgumentOutOfRangeException(nameof(row));
        return checked(BaseId + row * Columns + column);
    }
}

/// <summary>The immutable campus allocation for one map, prepared by WP-14 and shared by every producer and consumer.</summary>
/// <remarks>
/// The constructor validates an already allocated table; it never assigns, sorts, truncates or wraps IDs.
/// Inputs must be in ordinal (Feature.Value, Key) order with contiguous half-open ranges starting at zero.
/// Fingerprint is the lowercase SHA-256 of the complete resolved numbering input and table, including empty declarations.
/// It belongs in every VF-B and lighting-inventory cache identity. Numbers are stable for identical inputs, not across
/// edits that add, remove or resize earlier grids. This snapshot covers campus IDs only; the existing outskirts ABI is unchanged.
/// </remarks>
public sealed class WindowNumberingSnapshot
{
    public const int OverrideTextureSide = 256;
    public const int Capacity = OverrideTextureSide * OverrideTextureSide;
    private readonly Dictionary<(FeatureId Feature, string Key), WindowAllocation> _byKey = [];

    public WindowNumberingSnapshot(MapId map, string fingerprint, IReadOnlyList<WindowAllocation> allocations)
    {
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("Map is required.", nameof(map));
        if (fingerprint is null || fingerprint.Length != 64)
            throw new ArgumentException("A lowercase SHA-256 fingerprint is required.", nameof(fingerprint));
        foreach (char character in fingerprint)
            if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                throw new ArgumentException("A lowercase SHA-256 fingerprint is required.", nameof(fingerprint));
        ArgumentNullException.ThrowIfNull(allocations);
        var copy = new WindowAllocation[allocations.Count];
        long next = 0;
        WindowAllocation? previous = null;
        for (int i = 0; i < allocations.Count; i++)
        {
            WindowAllocation item = allocations[i];
            if (string.IsNullOrWhiteSpace(item.Feature.Value) || string.IsNullOrWhiteSpace(item.Key) ||
                item.Columns <= 0 || item.Rows <= 0)
                throw new ArgumentException("A window grid needs a feature, key and positive dimensions.", nameof(allocations));
            if (previous is { } before)
            {
                int order = StringComparer.Ordinal.Compare(before.Feature.Value, item.Feature.Value);
                if (order > 0 || (order == 0 && StringComparer.Ordinal.Compare(before.Key, item.Key) >= 0))
                    throw new ArgumentException("Window allocations must have unique keys and canonical order.", nameof(allocations));
            }
            long count = (long)item.Columns * item.Rows;
            if (item.BaseId != next || count > Capacity - next)
                throw new ArgumentException("Window ranges must be contiguous and fit the current 65536-ID ABI.", nameof(allocations));
            next += count;
            copy[i] = item;
            _byKey.Add((item.Feature, item.Key), item);
            previous = item;
        }
        Map = map;
        Fingerprint = fingerprint;
        Allocations = new ReadOnlyCollection<WindowAllocation>(copy);
        WindowCount = (int)next;
    }

    public MapId Map { get; }
    public string Fingerprint { get; }
    public int WindowCount { get; }
    public IReadOnlyList<WindowAllocation> Allocations { get; }

    /// <summary>Exact lookup; absent declarations fail instead of falling back to a temporary or generic base.</summary>
    public WindowAllocation Get(FeatureId feature, string key) => _byKey.TryGetValue((feature, key), out var value)
        ? value : throw new KeyNotFoundException($"No window allocation for {feature.Value} / {key} in map {Map.Value}.");
}
