using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Street;

/// <summary>One registered street part with review parameters: what the part gallery lays out in a row.</summary>
public sealed record StreetSample(string Id, string Label, PartId Part, BoxPartParams Params, Func<LodLevel, PartGeometry?> Build, Box3 Bounds)
{
    /// <summary>Lamp-inventory markers of this sample placed at <paramref name="at"/> (empty for unlit parts).</summary>
    public Func<Matrix4x4, IReadOnlyList<LightEmitter>> Emitters { get; init; } = _ => [];
}

/// <summary>
/// Review set for the part gallery and the tests: every WP-13 part of sub-task 13a at typical sizes and every variation,
/// in the row order the review screenshots use.
/// </summary>
public static class StreetGallery
{
    public static IReadOnlyList<StreetSample> CreateSamples()
    {
        var discLamp = new LampParams(0.85f, 6f, 0.85f);
        var armLamp = new LampParams(1.7f, 7f, 0.5f) { Variation = 1 };
        var doorLamp = new LampParams(0.28f, 0.3f, 0.18f);
        var kiosk = new SignParams(2f, 2.6f, 0.8f);
        var post = new SignParams(0.3f, 1.05f, 0.3f);
        return Array.AsReadOnly<StreetSample>(
        [
            Sample("lamp-disc", "路灯（白杆圆盘，一级）", PartCatalog.StreetLamp, new StreetLampPart(), discLamp) with { Emitters = at => [StreetLampPart.Emitter(discLamp, at)] },
            Sample("lamp-arm", "路灯（灰色单臂 LED，二级）", PartCatalog.StreetLamp, new StreetLampPart(), armLamp) with { Emitters = at => [StreetLampPart.Emitter(armLamp, at)] },
            Sample("bench", "长椅（带靠背）", PartCatalog.Bench, new BenchPart(), new FurnitureParams(1.8f, 0.8f, 0.62f)),
            Sample("bench-backless", "长椅（无靠背）", PartCatalog.Bench, new BenchPart(), new FurnitureParams(1.6f, 0.45f, 0.5f) { Variation = 1 }),
            Sample("bin-four", "四联分类垃圾箱", PartCatalog.BinFour, new BinFourPart(), new FurnitureParams(2f, 1f, 0.55f)),
            Sample("bollard-steel", "护柱（不锈钢）", PartCatalog.Bollard, new BollardPart(), new ColumnParams(0.18f, 0.8f, 0.18f)),
            Sample("bollard-crash", "防撞柱（黄黑）", PartCatalog.Bollard, new BollardPart(), new ColumnParams(0.22f, 0.85f, 0.22f) { Variation = 1 }),
            Sample("bollard-stone", "石护柱", PartCatalog.Bollard, new BollardPart(), new ColumnParams(0.25f, 0.7f, 0.25f) { Variation = 2 }),
            Sample("sphere-white", "石球（白）", PartCatalog.StoneSphere, new StoneSpherePart(), new FurnitureParams(0.6f, 0.6f, 0.6f)),
            Sample("sphere-grey", "石球（灰）", PartCatalog.StoneSphere, new StoneSpherePart(), new FurnitureParams(0.6f, 0.6f, 0.6f) { Variation = 1 }),
            Sample("bus-stop", "班车站牌", PartCatalog.BusStop, new BusStopPart(), new SignParams(1.2f, 2.5f, 0.18f)),
            Sample("atlas-kiosk", "导览亭", PartCatalog.AtlasKiosk, new AtlasKioskPart(), kiosk) with { Emitters = at => [AtlasKioskPart.Emitter(kiosk, at)] },
            Sample("atlas-post", "导览桩", PartCatalog.AtlasPost, new AtlasPostPart(), post) with { Emitters = at => [AtlasPostPart.Emitter(post, at)] },
            Sample("entrance-mat", "开放入口地垫", PartCatalog.EntranceMat, new EntranceMatPart(), new PanelParams(1.6f, 0.025f, 1f)),
            Sample("open-sign-stand", "绿色开放牌（落地）", PartCatalog.OpenSign, new OpenSignPart(), new SignParams(0.45f, 1.3f, 0.32f) { Variation = 1 }),
            Sample("open-sign-wall", "绿色开放牌（贴墙）", PartCatalog.OpenSign, new OpenSignPart(), new SignParams(0.42f, 0.24f, 0.02f)),
            Sample("entrance-lamp", "门口灯灯具", PartCatalog.EntranceLamp, new EntranceLampPart(), doorLamp) with { Emitters = at => [EntranceLampPart.Emitter(doorLamp, at)] },
        ]);
    }

    /// <summary>Near-level geometry of every sample, in the same order (part gallery examples).</summary>
    public static IReadOnlyList<GalleryExample> CreateExamples() =>
        Array.AsReadOnly(CreateSamples().Select(s => new GalleryExample(s.Id, s.Label, s.Build(LodLevel.L0)!)).ToArray());

    private static StreetSample Sample<TParams>(string id, string label, string part, IPartDef<TParams> def, TParams p) where TParams : BoxPartParams =>
        new(id, label, new(part), p, level => def.Lods.TryGetValue(level, out var build) ? build(p) : null, def.Bounds(p));
}
