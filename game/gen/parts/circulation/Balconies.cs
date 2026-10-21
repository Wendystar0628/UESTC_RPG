using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Cantilevered balcony against a wall at +Z: Width along the wall, Depth the projection, Height from the slab soffit
/// to the top of the guard (slab 0.15 m, so 1.05 m of guard by default). Material colours the slab and parapet (the
/// building's wall by default); copings are trim. Variation: 0 solid parapet (ART-MODELING prefers it), 1 low
/// upstand under a metal railing, 2 metal railing on the slab edge. Collision: the walkable slab and barrier walls on
/// the three open edges, top 1.5 m over the balcony floor.
/// </summary>
[Part(PartCatalog.Balcony)]
public sealed class BalconyPart() : CirculationPart<BalconyParams>(new(400, 120))
{
    private protected override Box3 BoundsOf(BalconyParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(BalconyParams p)
    {
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, floor = S.BalconySlab;
        var top = floor + Math.Max(p.Height - floor, WalkRules.BarrierHeight);
        var foot = Foot(p.Foot, S.SlabFoot);
        var barrierFoot = p.Variation == 2 ? S.MetalFoot : foot;
        var at = Matrix4x4.Identity; at.M42 = floor * 0.5f;
        var half = S.ParapetThickness * 0.5f;
        float inner = w - S.CopingOverhang - half, front = -d + S.CopingOverhang + half;
        return
        [
            new BoxCollider(at, new(w, floor * 0.5f, d), SurfaceTag.Ground) { Foot = foot },
            new WallCollider(new(-w, front), new(w, front), half, 0, top, SurfaceTag.Barrier) { Foot = barrierFoot },
            new WallCollider(new(-inner, front), new(-inner, d), half, 0, top, SurfaceTag.Barrier) { Foot = barrierFoot },
            new WallCollider(new(inner, front), new(inner, d), half, 0, top, SurfaceTag.Barrier) { Foot = barrierFoot },
        ];
    }

    private protected override void Compose(MeshBuilder mesh, BalconyParams p, Detail detail)
    {
        Range(p.Height, S.BalconySlab + 0.6f, 2f, nameof(p.Height));
        Range(p.Width, 0.8f, 30f, nameof(p.Width));
        Range(p.Depth, 0.5f, 4f, nameof(p.Depth));
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, slab = S.BalconySlab;
        var full = detail == Detail.Full;
        mesh.Box(p.Material, new(-w, 0, -d), new(w, slab, d), full ? S.SlabBevel : 0, Frame.Identity);
        float t = S.ParapetThickness, po = S.CopingOverhang, ov = S.CopingOverhang, fi = S.FaceInset, j = S.Joint;
        var solidTop = p.Variation switch
        {
            0 => p.Height,
            1 => slab + S.BalconyUpstand + S.BalconyCoping,
            2 => slab,
            _ => throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Balcony variation must be 0–2."),
        };
        if (solidTop > slab)
        {
            var panelTop = solidTop - S.BalconyCoping;
            var bevel = full ? S.PostBevel : 0;
            mesh.Box(p.Material, new(-w + po, slab - j, -d + po), new(w - po, panelTop + j, -d + po + t), bevel, Frame.Identity);
            mesh.Box(p.Material, new(-w + po + fi, slab - j, -d + po + t - j), new(-w + po + t, panelTop + j, d), bevel, Frame.Identity);
            mesh.Box(p.Material, new(w - po - t, slab - j, -d + po + t - j), new(w - po - fi, panelTop + j, d), bevel, Frame.Identity);
            if (detail != Detail.Mass)
            {
                var cb = full ? S.RailBevel : 0;
                var reach = po + t + ov;
                mesh.Box(S.Trim, new(-w, panelTop, -d), new(w, solidTop, -d + reach), cb, Frame.Identity);
                mesh.Box(S.Trim, new(-w + fi, panelTop, -d + reach - j), new(-w + reach, solidTop, d), cb, Frame.Identity);
                mesh.Box(S.Trim, new(w - reach, panelTop, -d + reach - j), new(w - fi, solidTop, d), cb, Frame.Identity);
            }
        }
        if (p.Variation == 0) return;
        // Metal guard on the front and both sides; the side runs use a frame turned so local X runs toward the wall.
        var metal = S.Metal;
        var z = -d + po + t * 0.5f;
        RailKit.Guard(mesh, metal, -w + po, w - po, z, solidTop, p.Height, t, 1.8f, Infill.Balusters, detail, Frame.Identity);
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * (w - po - t * 0.5f);
            var turned = new Frame(new(x, 0, 0), Vector3.UnitZ, Vector3.UnitY, -Vector3.UnitX);
            RailKit.Guard(mesh, metal, -d + po + t - j, d, 0, solidTop, p.Height, t, 1.8f, Infill.Balusters, detail, turned, startPost: false);
        }
    }
}

/// <summary>
/// Planter box: Width × Depth × Height to the rim, stone body on a recessed plinth, a trim rim, soil 5 cm down.
/// Variation: 0 a clipped shrub cushion rising 0.28 m over the rim, 1 low groundcover, 2 soil only (planted by game/gen/vegetation/).
/// Collision: one box to the rim; people can step up and sit (SYS-WALK "花池").
/// </summary>
[Part(PartCatalog.Planter)]
public sealed class PlanterPart() : CirculationPart<FurnitureParams>(new(480, 220))
{
    private static float Crown(int variation) => variation switch
    {
        0 => S.ShrubHeight,
        1 => S.GroundcoverHeight,
        2 => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(variation), variation, "Planter variation must be 0–2."),
    };

    private protected override Box3 BoundsOf(FurnitureParams p) => Centered(p.Width, 0, p.Height + Crown(p.Variation), p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(FurnitureParams p)
    {
        var at = Matrix4x4.Identity; at.M42 = p.Height * 0.5f;
        return [new BoxCollider(at, new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop) { Foot = Foot(p.Foot, S.OutdoorStepFoot) }];
    }

    private protected override void Compose(MeshBuilder mesh, FurnitureParams p, Detail detail)
    {
        Range(p.Width, 0.3f, 20f, nameof(p.Width)); Range(p.Depth, 0.3f, 20f, nameof(p.Depth)); Range(p.Height, 0.2f, 1.5f, nameof(p.Height));
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, h = p.Height, e = S.PlanterRimOverhang, rw = S.PlanterRimWidth, j = S.Joint;
        var stone = Pick(p.Material, S.Stone);
        var full = detail == Detail.Full;
        var rimBottom = h - S.PlanterRimHeight;
        float pi = e + S.PlanterPlinthInset;
        mesh.Box(stone, new(-w + pi, 0, -d + pi), new(w - pi, S.PlanterPlinth, d - pi), 0, Frame.Identity);
        mesh.Box(stone, new(-w + e, S.PlanterPlinth - j, -d + e), new(w - e, rimBottom + j, d - e), full ? S.PostBevel : 0, Frame.Identity);
        var crown = Crown(p.Variation);
        if (detail == Detail.Mass)
        {
            if (crown > 0) mesh.Box(S.Shrub, new(-w + rw, rimBottom, -d + rw), new(w - rw, h + crown, d - rw), 0, Frame.Identity);
            return;
        }
        var rb = full ? S.RailBevel : 0;
        mesh.Box(S.Trim, new(-w, rimBottom, -d), new(w, h, -d + rw), rb, Frame.Identity);
        mesh.Box(S.Trim, new(-w, rimBottom, d - rw), new(w, h, d), rb, Frame.Identity);
        mesh.Box(S.Trim, new(-w + S.FaceInset, rimBottom, -d + rw - j), new(-w + rw, h, d - rw + j), rb, Frame.Identity);
        mesh.Box(S.Trim, new(w - rw, rimBottom, -d + rw - j), new(w - S.FaceInset, h, d - rw + j), rb, Frame.Identity);
        mesh.Box(S.Soil, new(-w + rw - j, rimBottom - j, -d + rw - j), new(w - rw + j, h - S.SoilDrop, d - rw + j), 0, Frame.Identity);
        if (crown <= 0) return;
        // Clipped mounds: overlapping soft ellipsoids along the long side, sunk into the soil, tops at the crown.
        float gx = w - rw - S.ShrubGap, gz = d - rw - S.ShrubGap, soil = h - S.SoilDrop;
        var along = gx >= gz;
        float length = along ? gx : gz, across = along ? gz : gx;
        var count = Math.Max(1, (int)Math.Round(length / (across * 1.3f)));
        var ry = (crown + S.SoilDrop) / 1.45f;
        var reach = length / count;
        var (rings, segments) = detail switch { Detail.Full => (5, 10), Detail.Plain => (4, 8), _ => (3, 6) };
        for (var i = 0; i < count; i++)
        {
            var c = -length + reach * (2 * i + 1);
            var r = Math.Min(reach * 1.15f, length - Math.Abs(c));
            var centre = along ? new Vector3(c, soil + ry * 0.45f, 0) : new Vector3(0, soil + ry * 0.45f, c);
            var radii = along ? new Vector3(r, ry, across) : new Vector3(across, ry, r);
            mesh.Ellipsoid(S.Shrub, centre, radii, rings, segments, Frame.Identity);
        }
    }
}
