using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Lattice test tower (格构测试塔, MAP-QSH-ADDITIONS-VOCAB): a white steel lattice mast on a research-building roof, its legs leaning in
/// to 55% at the top, a horizontal ring and a zig-zag brace in every panel, a small platform on top and a mast standing out of that
/// with a cross-arm. Width × Depth is the footprint at the foot, Height the top of the mast. Variation 0: four legs. 1: three legs on
/// the footprint's ellipse. The roof is not walkable, so the part has no collision.
/// </summary>
[Part(PartCatalog.TestTower, Owner = "WP-12")]
public sealed class TestTowerPart : IPartDef<EquipmentParams>
{
    public TestTowerPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1600, 360);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        // Distant levels keep the tapered legs, platform and mast; braces and cross-arm are near-only.
        var near = lod == LodLevel.L0;
        var seg = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
        var legs = p.Variation == 1 ? 3 : 4;
        var lattice = PartMaterials.Pipe(PartMaterials.Casing(RoofStyle.CasingWhite));
        var b = new PartBuilder();
        var top = p.Height - RoofStyle.TowerMast;
        var panels = Math.Max(2, (int)MathF.Round(top / RoofStyle.TowerPanelHeight));
        var inset = RoofStyle.TowerLeg + 0.02f;
        // The lattice starts a little above the footprint plane so no leaning leg or ring dips under it.
        var lift = RoofStyle.TowerRing + 0.01f;
        var hx = p.Width * 0.5f - inset; var hz = p.Depth * 0.5f - inset;
        // Leg k at level i (0 = foot, panels = top): the corner (or ellipse point) pulled in towards the axis with height.
        Vector3 At(int k, int i)
        {
            var t = (float)i / panels;
            var scale = 1f - (1f - RoofStyle.TowerTopRatio) * t;
            var y = lift + (top - lift) * t;
            if (legs == 4) return new(((k & 1) == 0 ? -1f : 1f) * (k < 2 ? 1f : -1f) * hx * scale, y, (k < 2 ? -1f : 1f) * hz * scale);
            var a = MathF.PI * 0.5f + MathF.PI * 2 * k / 3;
            return new(hx * scale * MathF.Cos(a), y, hz * scale * MathF.Sin(a));
        }
        for (var k = 0; k < legs; k++)
            b.AddPipe(At(k, 0), At(k, panels), RoofStyle.TowerLeg, seg, lattice);
        for (var i = 0; i <= panels; i++)
        {
            if (!near && i % 2 == 1 && i != panels) continue;
            for (var k = 0; k < legs; k++)
                b.AddPipe(At(k, i), At((k + 1) % legs, i), RoofStyle.TowerRing, seg, lattice);
        }
        if (near)
            for (var i = 0; i < panels; i++)
                for (var k = 0; k < legs; k++)
                {
                    var j = (k + 1) % legs;
                    if ((i + k) % 2 == 0) b.AddPipe(At(k, i), At(j, i + 1), RoofStyle.TowerBrace, 5, lattice);
                    else b.AddPipe(At(j, i), At(k, i + 1), RoofStyle.TowerBrace, 5, lattice);
                }
        // Platform on the legs' tops, the mast standing out of it, a cross-arm near its head.
        var topScale = RoofStyle.TowerTopRatio;
        var px = Math.Min(hx * topScale + 0.12f, p.Width * 0.5f - 0.01f); var pz = Math.Min(hz * topScale + 0.12f, p.Depth * 0.5f - 0.01f);
        b.AddBox(new(-px, top - 0.01f, -pz), new(px, top + RoofStyle.TowerDeck, pz), near ? 0.012f : 0, PartMaterials.Trim(RoofStyle.Aluminium));
        var steel = PartMaterials.Pipe(PartMaterials.Metal(RoofStyle.Steel));
        b.AddPipe(new(0, top, 0), new(0, p.Height, 0), RoofStyle.TowerMastRadius, seg, steel);
        if (near)
        {
            var arm = Math.Min(0.45f, px);
            b.AddPipe(new(-arm, p.Height - 0.3f, 0), new(arm, p.Height - 0.3f, 0), 0.025f, 6, steel);
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown test-tower variation.");
        if (p.Width < 1f || p.Depth < 1f || p.Height < RoofStyle.TowerMast + 2f) throw new ArgumentOutOfRangeException(nameof(p), "A test tower is at least 1 × 1 m at the foot and 3.2 m tall.");
    }
}

/// <summary>
/// Photovoltaic module (光伏板): a rectangular module lying in the XZ plane (Width × Depth), Height the frame's thickness; a frame round
/// dark teal cells with their grid lines (near). Variation 0: silver aluminium frame. 1: dark frame. A host tilts and mounts it
/// (rooftop frames, the car-shelter row); the part has no collision.
/// </summary>
[Part(PartCatalog.SolarPanel, Owner = "WP-12")]
public sealed class SolarPanelPart : IPartDef<PanelParams>
{
    public SolarPanelPart() => Lods = Levels.Of<PanelParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4);
    public IReadOnlyDictionary<LodLevel, Func<PanelParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(300, 60);

    public Box3 Bounds(PanelParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(PanelParams p, LodLevel lod)
    {
        Validate(p);
        // The framed panel is the canopy silhouette at every level; the cell grid is near-only.
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height; var f = RoofStyle.SolarFrame;
        b.AddBox(new(-hx, 0, -hz), new(hx, h - 0.005f, hz), near ? 0.01f : 0, PartMaterials.Casing(p.Variation == 1 ? RoofStyle.FenceSteel : RoofStyle.Aluminium));
        b.AddBox(new(-hx + f, h - 0.006f, -hz + f), new(hx - f, h - 0.002f, hz - f), 0, PartMaterials.Board(RoofStyle.Glass), VertexFlag.NoShadow);
        if (!near) return b.Finish(Bounds(p));
        // Cell grid: lines along both directions, a little proud of the cells.
        var line = PartMaterials.Metal(RoofStyle.Aluminium);
        var w = p.Width - 2 * f; var d = p.Depth - 2 * f;
        for (var c = 1; c < RoofStyle.SolarColumns; c++)
        {
            var x = -hx + f + w * c / RoofStyle.SolarColumns;
            b.AddBox(new(x - RoofStyle.SolarLine * 0.5f, h - 0.003f, -hz + f), new(x + RoofStyle.SolarLine * 0.5f, h, hz - f), 0, line, VertexFlag.NoShadow);
        }
        for (var r = 1; r < RoofStyle.SolarRows; r++)
        {
            var z = -hz + f + d * r / RoofStyle.SolarRows;
            b.AddBox(new(-hx + f, h - 0.003f, z - RoofStyle.SolarLine * 0.5f), new(hx - f, h, z + RoofStyle.SolarLine * 0.5f), 0, line, VertexFlag.NoShadow);
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(PanelParams p)
    {
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown solar-panel variation.");
        if (p.Width < 0.5f || p.Depth < 0.5f || p.Height < 0.03f || p.Height > 0.15f) throw new ArgumentOutOfRangeException(nameof(p), "A photovoltaic module is at least 0.5 × 0.5 m and 3–15 cm thick.");
    }
}
