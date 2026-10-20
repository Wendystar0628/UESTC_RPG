using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>Measures L0 catalogue appearances and admits complete groups within each cell budget.</summary>
internal sealed class FurnitureCapacity
{
    private readonly Dictionary<(string, object), (int Meshes, int Triangles)> _cache = new();

    /// <summary>
    /// True when <paramref name="extra"/> keeps every L0 cell it falls in within the module's triangle and draw budget, counted the way
    /// the module emits them: each distinct part look once per cell for its meshes, its triangles per instance, the pads as one mesh.
    /// </summary>
    public bool Allows(List<FurnitureItem> items, IReadOnlyList<FurnitureItem> extra)
    {
        foreach (var cell in extra.Select(i => TileGrid.At(i.Anchor)).Distinct())
        {
            var inCell = items.Where(i => TileGrid.At(i.Anchor) == cell).Concat(extra.Where(i => TileGrid.At(i.Anchor) == cell)).ToList();
            int draws = inCell.Any(i => i.Pad is not null) ? 1 : 0, triangles = 0;
            foreach (var look in inCell.Select(i => (i.Part.Part.Value, i.Part.Params)).Distinct()) draws += Measure(look.Value, look.Params).Meshes;
            foreach (var item in inCell) triangles += Measure(item.Part.Part.Value, item.Part.Params).Triangles;
            if (draws > S.ServiceCellMaxDraws || triangles > S.ServiceCellMaxTriangles) return false;
        }
        return true;
    }

    private (int Meshes, int Triangles) Measure(string part, object p)
    {
        if (_cache.TryGetValue((part, p), out var known)) return known;
        var geometry = part switch
        {
            PartCatalog.StreetLamp => Lamp.Lods[LodLevel.L0]((LampParams)p),
            PartCatalog.EntranceLamp => DoorLampDef.Lods[LodLevel.L0]((LampParams)p),
            PartCatalog.Bench => BenchDef.Lods[LodLevel.L0]((FurnitureParams)p),
            PartCatalog.BinFour => BinDef.Lods[LodLevel.L0]((FurnitureParams)p),
            PartCatalog.BusStop => StopDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.AtlasKiosk => KioskDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.AtlasPost => PostDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.OpenSign => SignDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.EntranceMat => MatDef.Lods[LodLevel.L0]((PanelParams)p),
            PartCatalog.ParkedCar => CarDef.Lods[LodLevel.L0]((VehicleParams)p),
            PartCatalog.Signpost => SignpostDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.DrinkingFountain => FountainDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.VendingMachine => VendingDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.DeliveryLocker => LockerDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.BicycleShelter => BayDef.Lods[LodLevel.L0]((RoofParams)p),
            PartCatalog.ParkedBicycle => BikeDef.Lods[LodLevel.L0]((VehicleParams)p),
            PartCatalog.StoneSphere => SphereDef.Lods[LodLevel.L0]((FurnitureParams)p),
            StreetStyle.KerbRampId => RampDef.Lods[LodLevel.L0]((RampParams)p),
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown street part."),
        };
        var measured = (geometry.Meshes.Count, geometry.Meshes.Sum(m => m.Index.Length / 3));
        _cache.Add((part, p), measured);
        return measured;
    }
}
