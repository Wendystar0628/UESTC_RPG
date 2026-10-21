using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.West.Motto;
using C = Campus.Gen.Maps.Qingshuihe.West.WestTune.Carving;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.West.Bridgehead;

/// <summary>
/// 国防科技园石刻 (MAP-QSH-WEST): the campus stone-carving manner (粗凿的白色卧石, as the 体育运动中心 stone), here the
/// motto stone's faceted body at about half size: a few big cut planes, a low tilted top and one flat dressed face toward
/// the bridgehead junction. It lies in a low oval of pebbles so its foot meets the lawn softly. The face carries "国防科技园" through the shared scene-text atlas. Night role: dark area (§11).
/// </summary>
public sealed class CarvingSite : ZoneSite
{
    public override string Id => "19c.carving";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(PlanMath.Facing(At(world), C.Facing).Rect(-C.Length * 0.5f - 0.6f,
            -C.Depth * 0.5f - 0.5f, C.Length * 0.5f + 0.6f, C.Depth * 0.5f + 0.5f))];

    /// <summary>The carving's position: its node's anchor in the world model.</summary>
    internal static Vector2 At(IWorldModel world) =>
        world.Feature(C.Entity).Point();

    public override void Build(SiteBuilder b)
    {
        var at = At(b.Context.World);
        var f = PlanMath.Facing(at, C.Facing);
        var y = b.Ground(at);
        var around = b.Detail switch { SiteDetail.Full => 36, SiteDetail.Plain => 24, _ => 12 };
        // The pebble oval rises a little over game/gen/ground/'s grass (GroundSection.GrassY 0.10), the stone sinks into it.
        Vector2[] dome = [new(1f, 0f), new(0.95f, 0.08f), new(0.7f, 0.15f), new(0f, 0.18f)];
        b.Add(Solids.Lathe(dome, around, L.Pebble, C.Length * 0.5f + 0.6f, C.Depth * 0.5f + 0.5f), f.At(0, y + 0.02f, 0));
        b.Add(Scaled(MottoStone.Build(L.WhiteStone), C.Scale), f.At(0, y + 0.06f, 0));
        var textAt = MottoStone.OnFace(0, 1f) * C.Scale;
        Framework.WestLettering.Write(b, "国防科技园", .3f, L.Hedge, L.WhiteStone,
            Matrix4x4.CreateRotationX(-MathF.Atan(.16f * C.Scale.Z / C.Scale.Y)) *
            f.At(textAt.X, y + .06f + textAt.Y, textAt.Z + .015f));
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
