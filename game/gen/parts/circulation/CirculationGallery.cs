using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Circulation;

/// <summary>One gallery sample: a registered part with representative parameters, buildable at any level.</summary>
public sealed record CirculationSample(string Id, string Label, PartId Part, Box3 Bounds,
    Func<LodLevel, PartGeometry> Build, Func<IReadOnlyList<Collider>> Collide);

/// <summary>
/// Samples for the part gallery (--inspect=gallery, WP-01) and the review sandbox: each WP-11 part with values a
/// region package would actually use. Pure; build on a bake worker.
/// </summary>
public static class CirculationGallery
{
    public static IReadOnlyList<CirculationSample> Samples()
    {
        var mainSteps = (StairParams)TerrainDrop.Plan(1.2f, 3f, mainNetwork: true).Parts[0].Params;
        return Array.AsReadOnly(
        [
            Sample("railing-balusters", "立杆栏杆", PartCatalog.Railing, new RailingPart(), new RailingParams(3.6f, 1.1f, 0.1f, 1.8f)),
            Sample("railing-panel", "实心栏板栏杆", PartCatalog.Railing, new RailingPart(), new RailingParams(3.6f, 1.1f, 0.1f, 1.8f) { Variation = 1 }),
            Sample("railing-bars", "横杆栏杆", PartCatalog.Railing, new RailingPart(), new RailingParams(3.6f, 1.1f, 0.1f, 1.8f) { Variation = 2 }),
            Sample("handrail", "靠墙扶手", PartCatalog.Handrail, new HandrailPart(), new RailingParams(2.4f, 0.08f, 0.08f)),
            Sample("steps-main", "主网台阶（规则生成）", PartCatalog.Steps, new StepsPart(), mainSteps),
            Sample("steps-short", "四级台阶", PartCatalog.Steps, new StepsPart(), new StairParams(3f, 0.6f, 1.4f, 4)),
            Sample("steps-stoop", "两级门前台阶", PartCatalog.Steps, new StepsPart(), new StairParams(2f, 0.3f, 0.7f, 2) { Variation = 1 }),
            Sample("ramp", "无障碍坡道 1:12", PartCatalog.Ramp, new RampPart(), new RampParams(1.8f, 0.45f, 5.4f)),
            Sample("platform", "平台", PartCatalog.Platform, new PlatformPart(), new PanelParams(2.4f, 0.3f, 1.6f)),
            Sample("stair-straight", "直跑楼梯", PartCatalog.StairStraight, new StairStraightPart(), new StairParams(1.4f, 1.8f, 11 * 0.27f, 11)),
            Sample("stair-open", "敞开楼梯", PartCatalog.StairOpen, new StairOpenPart(), new StairParams(1.6f, 1.8f, 11 * 0.27f, 11)),
            Sample("stair-double", "双跑楼梯（金样一层）", PartCatalog.StairDouble, new StairDoublePart(), new StairParams(3.1f, 3.6f, 12 * 0.28f + 1.5f, 12)),
            Sample("balcony-solid", "阳台（实心栏板）", PartCatalog.Balcony, new BalconyPart(), new BalconyParams(3.3f, 1.2f, 1.4f)),
            Sample("balcony-rail", "阳台（金属栏杆）", PartCatalog.Balcony, new BalconyPart(), new BalconyParams(3.3f, 1.2f, 1.4f) { Variation = 2 }),
            Sample("drying-rail", "晾衣杆", PartCatalog.DryingRail, new DryingRailPart(), new RailingParams(3f, 0.12f, 0.6f)),
            Sample("planter", "花箱", PartCatalog.Planter, new PlanterPart(), new FurnitureParams(1.6f, 0.55f, 0.6f)),
            Sample("iron-gate", "铁栅门", PartCatalog.IronGate, new IronGatePart(), new DoorParams(1.6f, 2.1f, 0.08f, Leaves: 2)),
            Sample("folding-gate", "折叠铁闸（铝灰）", PartCatalog.FoldingGate, new FoldingGatePart(), new DoorParams(3.6f, 1.6f, 0.3f)),
            Sample("folding-gate-green", "折叠铁闸（灰绿）", PartCatalog.FoldingGate, new FoldingGatePart(), new DoorParams(3.6f, 1.6f, 0.3f) { Variation = 1 }),
            Sample("folding-gate-pair", "折叠铁闸（双扇，深色）", PartCatalog.FoldingGate, new FoldingGatePart(), new DoorParams(4.2f, 1.8f, 0.3f) { Variation = 2 }),
            Sample("lift-door", "电梯门（不锈钢）", PartCatalog.ElevatorDoor, new ElevatorDoorPart(), new DoorParams(1.2f, 2.2f, 0.1f)),
            Sample("lift-door-white", "电梯门（白）", PartCatalog.ElevatorDoor, new ElevatorDoorPart(), new DoorParams(1.2f, 2.2f, 0.1f) { Variation = 1 }),
            Sample("raised-floor", "架空地板（砖面）", PartCatalog.RaisedFloor, new RaisedFloorPart(), new PanelParams(3.6f, 0.2f, 3.6f)),
            Sample("raised-floor-stage", "舞台地板（木）", PartCatalog.RaisedFloor, new RaisedFloorPart(), new PanelParams(3.6f, 0.4f, 2.4f) { Variation = 1 }),
            Sample("tiered-floor", "阶梯地面", PartCatalog.TieredFloor, new TieredFloorPart(), new StairParams(6f, 1.5f, 2.7f)),
            Sample("tiered-floor-hall", "阶梯教室地面", PartCatalog.TieredFloor, new TieredFloorPart(), new StairParams(8f, 1.2f, 7.2f, 8)),
        ]);
    }

    /// <summary>L0 geometry of every sample, in the WP-09 gallery shape.</summary>
    public static IReadOnlyList<GalleryExample> CreateExamples() =>
        Samples().Select(s => new GalleryExample(s.Id, s.Label, s.Build(LodLevel.L0))).ToArray();

    private static CirculationSample Sample<TParams>(string id, string label, string part, IPartDef<TParams> def, TParams p) where TParams : notnull =>
        new(id, label, new(part), def.Bounds(p), level => def.Lods[level](p), () => def.Collision(p));
}
