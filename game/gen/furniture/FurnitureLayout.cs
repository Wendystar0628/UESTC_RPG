using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public enum FurnitureKind : byte { StreetLamp, Bench, Bin, BusStop, AtlasKiosk, AtlasPost, EntranceMat, OpenSign, EntranceLamp, ParkedCar, KerbRamp, Signpost, DrinkingFountain, VendingMachine, DeliveryLocker, BicycleBay, ParkedBicycle, Barrier }

/// <summary>
/// One placed piece of street furniture: the catalogue part with its world matrix, its lamp-inventory markers, its world-space
/// colliders (the part's own, plus its pad's) and the pad it brings, if any (a rest stop's bay, a kiosk's apron, a plinth).
/// </summary>
public sealed record FurnitureItem(string Key, FurnitureKind Kind, PlacedPart Part)
{
    public IReadOnlyList<LightEmitter> Emitters { get; init; } = [];
    public IReadOnlyList<Collider> Colliders { get; init; } = [];
    public Pad? Pad { get; init; }
    public Vector2 Anchor => new(Part.At.M41, Part.At.M43);
}

/// <summary>All street furniture of one map, in a stable order (anchors, entrances, stops, then roads in rule order).</summary>
public sealed record FurnitureLayout(IReadOnlyList<FurnitureItem> Items)
{
    /// <summary>Deterministic placement and planting diagnostics, returned through ContentOutput.Diagnostics.</summary>
    public IReadOnlyList<BuildDiagnostic> Diagnostics { get; init; } = [];

    public int Count(FurnitureKind kind) => Items.Count(i => i.Kind == kind);
}
