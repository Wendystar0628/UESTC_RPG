using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using R = Campus.Gen.Zones.CenterEast.CenterEastTune.Research;

namespace Campus.Gen.Zones.CenterEast.Research;

/// <summary>
/// 基础实验大楼 (科 A, §1.2): the コ-shaped main block open to the west, closed by the west block and its link strip, the lens
/// hall in the courtyard behind the east door, five storeys. Its roof carries the renovated old blocks' high fence and
/// tower tank (MAP-QSH-ADDITIONS) on the east wing, seen from 银杏大道 and the river.
/// </summary>
public sealed class KeASite : ResearchSite
{
    public override string Id => "21b1.ke-a";
    internal override string Entity => CenterEastIds.Research.KeA;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(CenterEastIds.Research.KeAMain, "main"), (CenterEastIds.Research.KeAWest, "west")];
    internal override string HallId => CenterEastIds.Research.KeAHallId;
    internal override string HallBackId => CenterEastIds.Layout.KeAHallBack;
    internal override string HallPart => CenterEastIds.Research.KeAHall;
    internal override string? BlockDoorId => CenterEastIds.Layout.KeADoor1;
    internal override IReadOnlyList<string> StairIds => [CenterEastIds.Layout.KeAStair1, CenterEastIds.Layout.KeAStair2, CenterEastIds.Layout.KeAStair3, CenterEastIds.Layout.KeAStair4];
    internal override IReadOnlyList<string> PlantIds => [CenterEastIds.Layout.KeAPlant1];

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        var world = b.Context.World;
        // The tank's spot is registered (world) in the roof feature's props; the fence runs round its ring.
        var t = Frame.Local(world.Feature(CenterEastIds.Research.KeARoofId).Pair("tank", "at"));
        var size = R.KeATankSize;
        if (b.Detail == SiteDetail.Mass)
        {
            b.Add(PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, 0, L.LightTrim), Frame.At(t.X, roof, t.Y));
            return;
        }
        b.Place(PartCatalog.WaterTank, new EquipmentParams(size.X, size.Y, size.Z) { Variation = 1 }, Frame.At(t.X, roof, t.Y));
        // The fence: grey aluminium bars between posts, drawn as a frame only (ART-STYLE), in runs of at most 6 m.
        var rectangle = world.Feature(CenterEastIds.Research.KeARoofId).Rectangle(Frame.U);
        var centre = Frame.Local(rectangle.Frame.Origin);
        var min = centre - new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        var max = centre + new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        foreach (var (a, e) in new[] { (min, new Vector2(max.X, min.Y)), (new Vector2(max.X, min.Y), max), (max, new Vector2(min.X, max.Y)), (new Vector2(min.X, max.Y), min) })
        {
            var length = Vector2.Distance(a, e);
            var n = (int)MathF.Ceiling(length / 6f);
            var along = MathF.Abs(e.X - a.X) > MathF.Abs(e.Y - a.Y);
            for (var i = 0; i < n; i++)
            {
                var c = Vector2.Lerp(a, e, (i + 0.5f) / n);
                var rail = new RailingParams(length / n, R.KeAFenceHeight, 0.06f, R.KeAFencePosts) { Variation = 2 };
                b.Place(PartCatalog.Railing, rail, along ? Frame.At(c.X, roof, c.Y) : Frame.AtTurned(c.X, roof, c.Y));
            }
        }
    }
}

/// <summary>
/// 一号科研实验楼 · 科研楼 B 区 (§1.1–§1.2): the longer twin of 科 A with a middle block across its courtyard, five storeys
/// with the east wing one storey higher as a set-back attic, the door facing the statue lawn, and the 一号科研楼 dishes on
/// the attic roof (MAP-QSH-ADDITIONS). Joined to 科 C on the north.
/// </summary>
public sealed class KeBSite : ResearchSite
{
    internal static readonly GridStyle AtticStyle = Style with
    {
        GroundStorey = R.AtticStorey,
        Storeys = 1,
        GroundSill = 0.9f,
        GroundWindow = 2.4f,
        PlinthHeight = 0.3f,
        Cornice = 0.5f,
        CorniceProjection = 0.3f,
        Parapet = 0.5f,
        PilasterEvery = 0,
    };

    public override string Id => "21b1.ke-b";
    internal override string Entity => CenterEastIds.Research.KeB;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(CenterEastIds.Research.KeBMain, "main"), (CenterEastIds.Research.KeBMiddle, "middle"), (CenterEastIds.Research.KeBWest, "west")];
    internal override IReadOnlyList<string> Neighbours => [CenterEastIds.Research.KeC];
    internal override string HallId => CenterEastIds.Research.KeBHallId;
    internal override string HallBackId => CenterEastIds.Layout.KeBHallBack;
    internal override string HallPart => CenterEastIds.Research.KeBHall;
    internal override string? BlockDoorId => CenterEastIds.Layout.KeBDoor1;
    internal override IReadOnlyList<string> StairIds => [CenterEastIds.Layout.KeBStair1, CenterEastIds.Layout.KeBStair2, CenterEastIds.Layout.KeBStair3, CenterEastIds.Layout.KeBStair4];
    internal override IReadOnlyList<string> PlantIds => [CenterEastIds.Layout.KeBPlant1];

    /// <summary>The attic over the east wing: the registered roof ring squared to the research frame.</summary>
    internal override IReadOnlyList<Vector2>? RaisedRing(IWorldModel world)
    {
        var rectangle = world.Feature(CenterEastIds.Research.KeBRoofId).Rectangle(Frame.U);
        var centre = Frame.Local(rectangle.Frame.Origin);
        var min = centre - new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        var max = centre + new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        return Frame.Rect(min.X, min.Y, max.X, max.Y);
    }

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Q5 (mid, K3): 22 m out from the east face over the statue lawn, eye height: stone grid, paired windows, plinth and cornice.
            var eye = Frame.P(94f, -151f);
            var q5 = Probe("Q5", CenterEastIds.Research.KeB, new(eye.X, WalkRules.StandingEye, eye.Y), Frame.P(72f, -143.8f) - eye, 8f, 22f);
            // Q8a (baseline, fly only): 2 m over the attic parapet at its north end, looking along the roof at the dishes.
            var top = Style.RoofTop + AtticStyle.Top + 2f;
            var over = Frame.P(65.5f, -179.0f);
            var q8 = Probe("Q8", CenterEastIds.Research.KeB, new(over.X, top, over.Y), Frame.P(65.5f, -158f) - over, -25f, 2f);
            return [q5, q8];
        }
    }

    /// <summary>The five-storey blocks and the set-back attic over the east wing, its own window grid one row high.</summary>
    internal override IEnumerable<(string Key, IReadOnlyList<Vector2> Ring, GridStyle Style, float Lift, Func<GridEdgeInfo, GridEdge> Dress)> GridBlocks(Plan plan) =>
        base.GridBlocks(plan).Append(("attic", plan.Raised!, AtticStyle, Style.RoofTop, _ => new(EdgeKind.Plain)));

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        if (b.Detail == SiteDetail.Mass) return;
        // Dishes turned so their faces look east (the part tilts its dish towards −Z; AtTurned sends −Z along +X).
        var top = roof + AtticStyle.RoofTop;
        foreach (var dish in b.Context.World.Feature(CenterEastIds.Research.KeBRoofId).RequiredProps().GetProperty("dishes").EnumerateArray())
        {
            var d = dish.Number("diameter");
            var at = Frame.Local(dish.Pair("at"));
            b.Place(PartCatalog.Antenna, new EquipmentParams(d, 0.95f * d + 0.6f, d), Frame.AtTurned(at.X, top, at.Y));
        }
    }
}

/// <summary>
/// 一号科研实验楼 · 科研楼 C 区 (§1.1–§1.2): the five-storey bar on the north, the link block to 科 B on the west and the
/// two-storey C hall (C 区大厅) between them on the east, its arc and doors facing the statue lawn; the 一号科研楼 roof
/// garden (MAP-QSH-ADDITIONS) on the bar.
/// </summary>
public sealed class KeCSite : ResearchSite
{
    public override string Id => "21b1.ke-c";
    internal override string Entity => CenterEastIds.Research.KeC;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(CenterEastIds.Research.KeCBar, "bar"), (CenterEastIds.Research.KeCLink, "link")];
    internal override IReadOnlyList<string> Neighbours => [CenterEastIds.Research.KeB];
    internal override string HallId => CenterEastIds.Research.KeCHallId;
    internal override string HallBackId => CenterEastIds.Layout.KeCHallBack;
    internal override string HallPart => CenterEastIds.Research.KeCHall;
    internal override string? BlockDoorId => null;
    internal override IReadOnlyList<string> StairIds => [CenterEastIds.Layout.KeCStair1, CenterEastIds.Layout.KeCStair2];
    internal override IReadOnlyList<string> PlantIds => [CenterEastIds.Layout.KeCPlant1];

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        var rectangle = b.Context.World.Feature(CenterEastIds.Research.KeCGardenId).Rectangle(Frame.U);
        var centre = Frame.Local(rectangle.Frame.Origin);
        var min = centre - new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        var max = centre + new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        float midU = (min.X + max.X) * 0.5f, midW = (min.Y + max.Y) * 0.5f, lu = max.X - min.X, lw = max.Y - min.Y;
        if (b.Detail == SiteDetail.Mass)
        {
            b.Add(PartGeometryKernel.BeveledBox(lu, 0.6f, lw, 0, L.Bed), Frame.At(midU, roof, midW));
            return;
        }
        // Light timber frame: three rows of posts with beams, joists across on top (L0 only).
        var h = R.GardenFrameHeight;
        var n = Math.Max(1, (int)MathF.Round(lu / R.GardenPostPitch));
        float[] rows = [min.Y + 0.3f, midW, max.Y - 0.3f];
        foreach (var w in rows)
        {
            for (var i = 0; i <= n; i++)
                b.Add(PartGeometryKernel.BeveledBox(0.14f, h, 0.14f, 0.01f, L.Timber), Frame.At(min.X + lu * i / n, roof, w));
            b.Add(PartGeometryKernel.BeveledBox(lu + 0.4f, 0.2f, 0.12f, 0.01f, L.Timber), Frame.At(midU, roof + h - 0.2f, w));
        }
        if (b.Detail == SiteDetail.Full)
            for (var i = 0; i <= n; i++)
                b.Add(PartGeometryKernel.BeveledBox(0.08f, 0.14f, rows[^1] - rows[0] + 0.4f, 0.01f, L.Timber), Frame.At(min.X + lu * i / n, roof + h, midW));
        // Planter boxes between the post rows, shrubs and groundcover alternating, and a rain tank at the east end.
        foreach (var w in new[] { (rows[0] + rows[1]) * 0.5f, (rows[1] + rows[2]) * 0.5f })
            for (var i = 0; i < n; i++)
                b.Place(PartCatalog.Planter, new FurnitureParams(lu / n - 0.5f, 0.6f, 1.2f) { Variation = (i % 2) }, Frame.At(min.X + lu * (i + 0.5f) / n, roof, w));
        b.Place(PartCatalog.WaterTank, new EquipmentParams(1.2f, 1.4f, 1.2f), Frame.At(max.X + 1.6f, roof, midW));
    }
}
