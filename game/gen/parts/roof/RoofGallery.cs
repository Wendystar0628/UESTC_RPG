using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>One registered roof part with review parameters: what the part gallery lays out in a row.</summary>
public sealed record RoofSample(string Id, string Label, PartId Part, BoxPartParams Params, Func<LodLevel, PartGeometry?> Build, Box3 Bounds);

/// <summary>
/// Review set for the part gallery (WP-01 `--inspect=gallery`) and the tests: every WP-12 part at typical sizes,
/// including the real-size outdoor unit inside its grille cage as the dorm facades use it.
/// </summary>
public static class RoofGallery
{
    /// <summary>Outdoor unit and grille cage at real size: about 20% under the building generator's stand-in.</summary>
    public static readonly EquipmentParams DormUnit = new(0.64f, 0.48f, 0.26f);
    public static readonly PanelParams DormCage = new(0.8f, 0.64f, 0.36f);

    public static IReadOnlyList<RoofSample> CreateSamples()
    {
        var trim = new ToonSpec(new("wall.greyWhite.lit"), new("wall.greyWhite.shade"));
        return Array.AsReadOnly<RoofSample>(
        [
            Sample("parapet-cap", "女儿墙压顶", PartCatalog.ParapetCap, new ParapetCapPart(), new TrimParams(3.6f, 0.3f, 0.5f) { Material = trim }),
            Sample("lightning-strip", "避雷带（K1 支架）", PartCatalog.LightningStrip, new LightningStripPart(), new TrimParams(3.6f, 0.15f, 0.08f) { Variation = 1 }),
            Sample("skylight", "采光顶", PartCatalog.Skylight, new SkylightPart(), new RoofParams(3f, 0.9f, 2.2f) { Material = trim }),
            Sample("water-tank", "水箱", PartCatalog.WaterTank, new WaterTankPart(), new EquipmentParams(2f, 2.5f, 2f)),
            Sample("water-tank-stand", "高架水箱", PartCatalog.WaterTank, new WaterTankPart(), new EquipmentParams(3f, 4.2f, 2f) { Variation = 1 }),
            Sample("heat-pump", "热泵", PartCatalog.HeatPump, new HeatPumpPart(), new EquipmentParams(1.6f, 1.5f, 0.9f)),
            Sample("air-conditioner", "空调外机", PartCatalog.AirConditioner, new AirConditionerPart(), DormUnit),
            Sample("grille-cage", "机位格栅（笼）", PartCatalog.EquipmentGrille, new EquipmentGrillePart(), DormCage),
            Sample("grille-panel", "机位格栅（平）", PartCatalog.EquipmentGrille, new EquipmentGrillePart(), new PanelParams(0.9f, 0.75f, 0.08f)),
            Sample("canopy", "雨棚（拉杆）", PartCatalog.Canopy, new CanopyPart(), new RoofParams(3.6f, 0.3f, 1.8f) { Variation = 1 }),
            Sample("downpipe", "落水管", PartCatalog.Downpipe, new DownpipePart(), new ColumnParams(0.12f, 6f, 0.16f) { Material = trim }),
            Sample("antenna-dish", "抛物面天线", PartCatalog.Antenna, new AntennaPart(), new EquipmentParams(1.4f, 2.2f, 1.4f)),
            Sample("antenna-mast", "通信天线杆", PartCatalog.Antenna, new AntennaPart(), new EquipmentParams(0.8f, 3.5f, 0.8f) { Variation = 1 }),
            Sample("equipment-fence", "设备围挡（带门）", PartCatalog.EquipmentFence, new EquipmentFencePart(), new RailingParams(4f, 1.8f, 0.06f, 1.3f) { Variation = 1 }),
        ]);
    }

    /// <summary>Near-level geometry of every sample, in the same order (WP-01 gallery examples).</summary>
    public static IReadOnlyList<GalleryExample> CreateExamples() =>
        Array.AsReadOnly(CreateSamples().Select(s => new GalleryExample(s.Id, s.Label, s.Build(LodLevel.L0)!)).ToArray());

    private static RoofSample Sample<TParams>(string id, string label, string part, IPartDef<TParams> def, TParams p) where TParams : BoxPartParams =>
        new(id, label, new(part), p, level => def.Lods.TryGetValue(level, out var build) ? build(p) : null, def.Bounds(p));
}
