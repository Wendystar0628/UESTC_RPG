using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using R = Campus.Gen.Zones.CenterEast.CenterEastTune.Research;

namespace Campus.Gen.Zones.CenterEast.Research;

/// <summary>
/// 基础实验大楼 (科 A, §1.2): the コ-shaped main block open to the west, closed by the west block and its link strip, the lens
/// hall in the courtyard behind the east door, five storeys. Its roof carries the renovated old blocks' high fence and
/// tower tank (MAP-QSH-ADDITIONS §2.16) on the east wing, seen from 银杏大道 and the river.
/// </summary>
public sealed class KeASite : ResearchSite
{
    public override string Id => "21b1.ke-a";
    internal override string Entity => R.KeA;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(R.KeAMain, "main"), (R.KeAWest, "west")];
    internal override IReadOnlyList<Vector2> HallBack => R.KeAHallBack;
    internal override float HallSagitta => R.KeAHallSagitta;
    internal override int HallStoreys => 1;
    internal override Vector2? BlockDoor => R.KeADoor;
    internal override IReadOnlyList<Vector2> Stairs => R.KeAStairs;
    internal override IReadOnlyList<Vector2> Plants => R.KeAPlant;

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        var t = R.KeATank;
        var size = R.KeATankSize;
        if (b.Detail == SiteDetail.Mass)
        {
            b.Add(PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, 0, L.LightTrim), Frame.At(t.X, roof, t.Y));
            return;
        }
        b.Place(PartCatalog.WaterTank, new EquipmentParams(size.X, size.Y, size.Z) { Variation = 1 }, Frame.At(t.X, roof, t.Y));
        // The fence: grey aluminium bars between posts, drawn as a frame only (ART-STYLE §4.4), in runs of at most 6 m.
        Vector2 min = R.KeAFenceMin, max = R.KeAFenceMax;
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
/// the attic roof (MAP-QSH-ADDITIONS §2.9). Joined to 科 C on the north.
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
    internal override string Entity => R.KeB;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(R.KeBMain, "main"), (R.KeBMiddle, "middle"), (R.KeBWest, "west")];
    internal override IReadOnlyList<string> Neighbours => [R.KeC];
    internal override IReadOnlyList<Vector2> HallBack => R.KeBHallBack;
    internal override float HallSagitta => R.KeBHallSagitta;
    internal override int HallStoreys => 1;
    internal override Vector2? BlockDoor => R.KeBDoor;
    internal override IReadOnlyList<Vector2> Stairs => R.KeBStairs;
    internal override IReadOnlyList<Vector2> Plants => R.KeBPlant;

    internal static Vector2[] AtticRing => Frame.Rect(R.KeBAtticMin.X, R.KeBAtticMin.Y, R.KeBAtticMax.X, R.KeBAtticMax.Y);

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Q5 (mid, K3): 22 m out from the east face over the statue lawn, eye height: stone grid, paired windows, plinth and cornice.
            var eye = Frame.P(94f, -151f);
            var q5 = Probe("Q5", R.KeB, new(eye.X, 1.62f, eye.Y), Frame.P(72f, -143.8f) - eye, 8f, 22f);
            // Q8a (baseline, fly only): 2 m over the attic parapet at its north end, looking along the roof at the dishes.
            var top = Style.RoofTop + AtticStyle.Top + 2f;
            var over = Frame.P(65.5f, -179.0f);
            var q8 = Probe("Q8", R.KeB, new(over.X, top, over.Y), Frame.P(65.5f, -158f) - over, -25f, 2f);
            return [q5, q8];
        }
    }

    /// <summary>The five-storey blocks and the set-back attic over the east wing, its own window grid one row high.</summary>
    internal override IEnumerable<(string Key, IReadOnlyList<Vector2> Ring, GridStyle Style, float Lift, Func<GridEdgeInfo, GridEdge> Dress)> GridBlocks(Plan plan) =>
        base.GridBlocks(plan).Append(("attic", AtticRing, AtticStyle, Style.RoofTop, _ => new(EdgeKind.Plain)));

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        if (b.Detail == SiteDetail.Mass) return;
        // Dishes turned so their faces look east (the part tilts its dish towards −Z; AtTurned sends −Z along +X).
        var top = roof + AtticStyle.RoofTop;
        for (var i = 0; i < R.KeBDishes.Length; i++)
        {
            var d = R.KeBDishDiameters[i];
            b.Place(PartCatalog.Antenna, new EquipmentParams(d, 0.95f * d + 0.6f, d), Frame.AtTurned(R.KeBDishes[i].X, top, R.KeBDishes[i].Y));
        }
    }
}

/// <summary>
/// 一号科研实验楼 · 科研楼 C 区 (§1.1–§1.2): the five-storey bar on the north, the link block to 科 B on the west and the
/// two-storey C hall (C 区大厅) between them on the east, its arc and doors facing the statue lawn; the 一号科研楼 roof
/// garden (MAP-QSH-ADDITIONS §2.9) on the bar.
/// </summary>
public sealed class KeCSite : ResearchSite
{
    public override string Id => "21b1.ke-c";
    internal override string Entity => R.KeC;
    internal override IReadOnlyList<(string Source, string Key)> Blocks => [(R.KeCBar, "bar"), (R.KeCLink, "link")];
    internal override IReadOnlyList<string> Neighbours => [R.KeB];
    internal override IReadOnlyList<Vector2> HallBack => R.KeCHallBack;
    internal override float HallSagitta => R.KeCHallSagitta;
    internal override int HallStoreys => 2;
    internal override Vector2? BlockDoor => null;
    internal override IReadOnlyList<Vector2> Stairs => R.KeCStairs;
    internal override IReadOnlyList<Vector2> Plants => R.KeCPlant;

    private protected override void Roof(SiteBuilder b, Plan plan, float y)
    {
        var roof = y + Style.RoofTop;
        Vector2 min = R.KeCGardenMin, max = R.KeCGardenMax;
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
