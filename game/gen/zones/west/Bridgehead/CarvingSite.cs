using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.West.Motto;
using C = Campus.Gen.Zones.West.WestTune.Carving;
using L = Campus.Gen.Zones.West.WestTune.Look;

namespace Campus.Gen.Zones.West.Bridgehead;

/// <summary>
/// 国防科技园石刻 (MAP-QSH-WEST): the campus stone-carving manner (粗凿的白色卧石, as the 体育运动中心 stone), here the
/// motto stone's faceted body at about half size: a few big cut planes, a low tilted top and one flat dressed face toward
/// the bridgehead junction. It lies in a low oval of pebbles so its foot meets the lawn softly. The face is blank until the
/// scene text atlas (contract-26) carries "国防科技园" in dark green running script. Night role: dark area (§11).
/// </summary>
public sealed class CarvingSite : ZoneSite
{
    public override string Id => "19c.carving";

    /// <summary>The carving's position: its node's anchor in the world model.</summary>
    internal static Vector2 At(IWorldModel world) =>
        world.Features.TryGetValue(new(C.Entity), out var feature) && feature.Geometry is PointGeometry point
            ? new(point.Position.X, point.Position.Z)
            : throw new InvalidOperationException($"国防科技园石刻 {C.Entity} is missing from the world model.");

    public override void Build(SiteBuilder b)
    {
        var at = At(b.Context.World);
        var f = PlanMath.Facing(at, C.Facing);
        var y = b.Ground(at);
        var around = b.Detail switch { SiteDetail.Full => 36, SiteDetail.Plain => 24, _ => 12 };
        // The pebble oval rises a little over WP-15's grass (GroundSection.GrassY 0.10), the stone sinks into it.
        Vector2[] dome = [new(1f, 0f), new(0.95f, 0.08f), new(0.7f, 0.15f), new(0f, 0.18f)];
        b.Add(Solids.Lathe(dome, around, L.Pebble, C.Length * 0.5f + 0.6f, C.Depth * 0.5f + 0.5f), f.At(0, y + 0.02f, 0));
        b.Add(Scaled(MottoStone.Build(L.WhiteStone), C.Scale), f.At(0, y + 0.06f, 0));
        // Text hook: "国防科技园" in dark green on the dressed face (MottoStone.OnFace scaled by C.Scale), once contract-26 lands.
    }

    /// <summary>Scales a geometry about its origin; normals by the inverse scale so the facets keep their true shading.</summary>
    internal static PartGeometry Scaled(PartGeometry g, Vector3 s)
    {
        var meshes = new List<GeometryBundle>();
        foreach (var mesh in g.Meshes)
        {
            var p = (float[])mesh.Position.Clone();
            var n = (float[])mesh.Normal.Clone();
            for (var i = 0; i < p.Length; i += 3)
            {
                p[i] *= s.X; p[i + 1] *= s.Y; p[i + 2] *= s.Z;
                float nx = n[i] / s.X, ny = n[i + 1] / s.Y, nz = n[i + 2] / s.Z;
                var l = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
                n[i] = nx / l; n[i + 1] = ny / l; n[i + 2] = nz / l;
            }
            meshes.Add(mesh with { Position = p, Normal = n });
        }
        return new(meshes, new(g.Bounds.Min * s, g.Bounds.Max * s));
    }

    /// <summary>One box with a top (SYS-WALK).</summary>
    public override void Collide(SiteColliders c)
    {
        var at = At(c.Context.World);
        var y = c.Ground(at.X, at.Y);
        c.Box(PlanMath.Facing(at, C.Facing).At(0, y + C.Height * 0.5f, 0), new(C.Length * 0.5f, C.Height * 0.5f, C.Depth * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }
}
