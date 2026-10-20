using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Raised floor (架空地板 and the stage floor): a thin finished plate on a dark recessed skirt, Width × Depth with its top at Height.
/// Variation 0: pale tiles in 0.6 m modules with dark joint lines. 1: warm wood with plank seams (a stage). 2: grey steel plate
/// in modules (a machine-room floor). The caller's Material replaces the finish. Collision: one walkable box, Ground; the foot follows
/// the finish (tile, wood, steel plate) unless the caller sets one.
/// </summary>
[Part(PartCatalog.RaisedFloor)]
public sealed class RaisedFloorPart() : CirculationPart<PanelParams>(new(420, 90))
{
    private protected override Box3 BoundsOf(PanelParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(PanelParams p) =>
        [new BoxCollider(Frame.At(0, p.Height * 0.5f, 0).ToMatrix(), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Ground)
            { Foot = Foot(p.Foot, p.Variation switch { 1 => S.WoodFoot, 2 => S.SteelFoot, _ => S.TileFoot }) }];

    private protected override void Compose(MeshBuilder mesh, PanelParams p, Detail detail)
    {
        Range(p.Width, 0.6f, 30f, nameof(p.Width)); Range(p.Depth, 0.6f, 30f, nameof(p.Depth)); Range(p.Height, 0.08f, 0.6f, nameof(p.Height));
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Raised-floor variation must be 0–2.");
        var finish = Pick(p.Material, p.Variation switch { 1 => S.FloorWood, 2 => S.FloorGrey, _ => S.StairBody });
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, h = p.Height, j = S.Joint;
        if (detail == Detail.Mass)
        {
            mesh.Box(finish, new(-w, 0, -d), new(w, h, d), 0, Frame.Identity);
            return;
        }
        var plate = S.RaisedPlate; var top = h - 0.002f;
        var inset = S.RaisedSkirtInset;
        mesh.Box(S.GateDark, new(-w + inset, 0, -d + inset), new(w - inset, top - plate + j, d - inset), 0, Frame.Identity);
        mesh.Box(finish, new(-w, top - plate, -d), new(w, top, d), detail == Detail.Full ? S.PostBevel : 0, Frame.Identity);
        if (detail == Detail.Coarse) return;
        // Joint lines a hair above the plate: a grid of modules, or plank seams across the stage.
        var line = S.Insert; var half = S.RaisedJoint * 0.5f;
        if (p.Variation == 1)
        {
            var pitch = detail == Detail.Full ? 0.15f : 0.6f;
            var count = (int)MathF.Floor(p.Width / pitch);
            for (var i = 1; i < count; i++)
            {
                var x = -w + i * p.Width / count;
                mesh.Box(line, new(x - half, top - 0.002f, -d), new(x + half, h, d), 0, Frame.Identity, VertexFlag.NoShadow);
            }
            return;
        }
        var nx = Math.Max(1, (int)MathF.Round(p.Width / S.RaisedModule)); var nz = Math.Max(1, (int)MathF.Round(p.Depth / S.RaisedModule));
        for (var i = 1; i < nx; i++)
        {
            var x = -w + i * p.Width / nx;
            mesh.Box(line, new(x - half, top - 0.002f, -d), new(x + half, h, d), 0, Frame.Identity, VertexFlag.NoShadow);
        }
        for (var i = 1; i < nz; i++)
        {
            var z = -d + i * p.Depth / nz;
            mesh.Box(line, new(-w, top - 0.002f, z - half), new(w, h, z + half), 0, Frame.Identity, VertexFlag.NoShadow);
        }
    }
}

/// <summary>
/// Tiered floor (阶梯地面, the lecture hall's stepped floor): a stepped block of Steps tiers across Width, rising Height over Depth, the
/// first riser on the front edge (−Z) and the top tread flush with the upper level at +Z, sunk 0.1 m below y = 0. Treads are deep
/// (0.25–1.5 m) and risers low (0.08–0.25 m); the aisle stairs and seats are the host's. Collision: one box per tier (each rise is
/// within auto-step), Stair, the foot Terrazzo unless set.
/// </summary>
[Part(PartCatalog.TieredFloor)]
public sealed class TieredFloorPart() : CirculationPart<StairParams>(new(300, 120))
{
    private protected override Box3 BoundsOf(StairParams p) => Centered(p.Width, -S.Sink, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(StairParams p)
    {
        var (r, t) = Plan(p);
        var foot = Foot(p.Foot, S.IndoorStairFoot);
        var colliders = new List<Collider>(p.Steps);
        for (var k = 1; k <= p.Steps; k++)
        {
            float y0 = -S.Sink, y1 = k * r, z0 = (k - 1) * t - p.Depth * 0.5f, z1 = p.Depth * 0.5f;
            colliders.Add(new BoxCollider(Frame.At(0, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f).ToMatrix(), new(p.Width * 0.5f, (y1 - y0) * 0.5f, (z1 - z0) * 0.5f), SurfaceTag.Stair) { Foot = foot });
        }
        return colliders;
    }

    private protected override void Compose(MeshBuilder mesh, StairParams p, Detail detail)
    {
        Plan(p);
        var stone = Pick(p.Material, S.StairBody);
        var (ring, chamfer, triangles) = StepProfile.Block(p.Steps, p.Height, p.Depth, detail, S.NosingBevel);
        for (var i = 0; i < ring.Count; i++) ring[i] = new(ring[i].X - p.Depth * 0.5f, ring[i].Y);
        mesh.Extrude(stone, ring, chamfer, -p.Width * 0.5f, p.Width * 0.5f, Frame.Identity, triangles);
    }

    /// <summary>The riser and tread of a tier, checked against the lecture-hall ranges.</summary>
    private static (float Riser, float Tread) Plan(StairParams p)
    {
        if (p.Variation != 0) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Tiered-floor variation must be 0.");
        if (p.Steps < 2) throw new ArgumentOutOfRangeException(nameof(p), "A tiered floor needs at least two tiers.");
        Range(p.Width, 1f, 40f, nameof(p.Width));
        var r = p.Height / p.Steps; var t = p.Depth / p.Steps;
        Range(r, 0.08f, 0.25f, "riser height");
        Range(t, 0.25f, 1.5f, "tread depth");
        return (r, t);
    }
}
