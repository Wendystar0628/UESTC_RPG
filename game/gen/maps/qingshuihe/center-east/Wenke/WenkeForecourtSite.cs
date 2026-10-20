using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Sites;
using F = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;

/// <summary>
/// The lawn in front of 文科楼's atrium door (MAP-QSH-CENTER-EAST), a functional open space (MAP-QSH-ADDITIONS R6)
/// that frames the view east to the training hall and the moonrise: a granite stop in front of the shut door and a walk
/// from it to the road junction, the friendship tree's three timber stakes and green trunk cloth (the camphor itself is
/// WP-23's, at the registered point), and the granite landscape stone beside it with the gold original emblem on its
/// dressed face (ART-STYLE). The lawn's middle and the 2.5 m outside the door stay clear. The paving, the tree and the
/// stone stand where the overlay registered them, read back by ID (D-239); modelling sizes in <see cref="F"/>, <see cref="K"/>.
/// </summary>
public sealed class WenkeForecourtSite : ZoneSite
{
    internal static PlanFrame StoneFrame(IWorldModel world) => PlanMath.Facing(world.Feature(CenterEastIds.Friendship.StoneId).Point(), F.StoneFacing);

    /// <summary>The friendship tree's spot.</summary>
    internal static Vector2 TreeAt(IWorldModel world) => world.Feature(CenterEastIds.Friendship.TreeId).Point();

    /// <summary>The walk's centre line from the forecourt end: its frame (X along it), length and width.</summary>
    internal static (PlanFrame Frame, float Length, float Width) Walk(IWorldModel world)
    {
        var feature = world.Feature(CenterEastIds.Wenke.WalkId);
        var rectangle = feature.Rectangle(feature.Pair("axis"));
        var a = rectangle.Frame.P(-rectangle.Width * 0.5f, 0);
        var b = rectangle.Frame.P(rectangle.Width * 0.5f, 0);
        return (PlanFrame.Toward(a, b), rectangle.Width, rectangle.Depth);
    }

    /// <summary>The forecourt paving squared to the door frame: its local (u, w) corners.</summary>
    internal static (Vector2 Min, Vector2 Max) ForecourtRect(IWorldModel world)
    {
        var frame = WenkeSite.Door(world);
        var rectangle = world.Feature(CenterEastIds.Wenke.ForecourtId).Rectangle(frame.U);
        var centre = frame.Local(rectangle.Frame.Origin);
        var half = new Vector2(rectangle.Width, rectangle.Depth) * 0.5f;
        return (centre - half, centre + half);
    }

    public override string Id => "21c.wenke-forecourt";

    private static Campus.Contracts.Maps.MapRect TreeRange(IWorldModel world) =>
        SiteRanges.Point(TreeAt(world), F.StakeFoot + F.StakeRadius);
    private static Campus.Contracts.Maps.MapRect StoneRange(IWorldModel world) =>
        SiteRanges.Point(StoneFrame(world).Origin, F.Stone.Length + MathF.Abs(F.Stone.Shear));

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.ForecourtId), K.PavingKerb),
         SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.WalkId), K.PavingKerb), TreeRange(world), StoneRange(world)];

    /// <summary>The forecourt and the walk as world rings (outside on the right), as registered in the overlay.</summary>
    internal static Vector2[] ForecourtRing(IWorldModel world)
    {
        var (min, max) = ForecourtRect(world);
        return PlanMath.OutsideRight(WenkeSite.Door(world).Rect(min.X, min.Y, max.X, max.Y));
    }

    internal static Vector2[] WalkRing(IWorldModel world)
    {
        var (frame, length, width) = Walk(world);
        return PlanMath.OutsideRight(frame.Rect(0, -width * 0.5f, length, width * 0.5f));
    }

    /// <summary>Foot and top of each stake (world plan, heights above the ground).</summary>
    internal static IEnumerable<(Vector2 Foot, Vector2 Top)> Stakes(Vector2 tree)
    {
        for (var i = 0; i < 3; i++)
        {
            var h = Angles.Heading(F.StakeBearing + 120f * i);
            yield return (tree + h * F.StakeFoot, tree + h * (F.WrapRadius + F.StakeRadius));
        }
    }

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        Paving(b);
        var tree = TreeAt(world);
        if (b.Includes(TreeRange(world))) Tree(b, tree, b.Ground(tree));
        var stone = StoneFrame(world);
        if (b.Includes(StoneRange(world))) Stone(b, stone, b.Ground(stone.Origin));
    }

    // ---- Paving: granite stop and walk, a few centimetres over WP-15's lawn, kerbed in light stone ----

    private static void Paving(SiteBuilder b)
    {
        var world = b.Context.World;
        var door = WenkeSite.Door(world);
        var (min, max) = ForecourtRect(world);
        var mid = (min + max) * 0.5f;
        var bevel = b.Detail == SiteDetail.Mass ? 0f : 0.01f;
        var forecourt = b.Includes(SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.ForecourtId), K.PavingKerb));
        var walking = b.Includes(SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.WalkId), K.PavingKerb));
        var g = forecourt ? b.Ground(door.P(mid.X, mid.Y)) : 0f;
        if (forecourt) b.Add(PartSolids.BeveledBox(max.X - min.X, K.PavingTop + 0.1f, max.Y - min.Y, bevel, L.Paving), door.At(mid.X, g - 0.1f, mid.Y));
        var (walk, walkLength, walkWidth) = Walk(world);
        var gw = walking ? b.Ground(walk.P(walkLength * 0.5f, 0)) : 0f;
        if (walking) b.Add(PartSolids.BeveledBox(walkLength, K.PavingTop + 0.1f, walkWidth, bevel, L.Paving), walk.At(walkLength * 0.5f, gw - 0.1f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        var kerb = PartSweeps.BeveledRectangleProfile(K.PavingKerb, K.PavingTop + 0.12f, 0.015f);
        if (forecourt) b.Add(PartSweeps.Sweep(kerb, ForecourtRing(world), L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.1f, 0));
        // The walk's kerbs run along its two long sides only, so it opens into the forecourt and onto the road.
        foreach (var s in new[] { -1f, 1f }.Where(_ => walking))
        {
            var w = s * (walkWidth * 0.5f - K.PavingKerb * 0.5f);
            Vector2 a = walk.P(0.1f, w), e = walk.P(walkLength, w);
            b.Add(PartSweeps.Sweep(kerb, [a, e], L.Granite), Matrix4x4.CreateTranslation(0, gw - 0.1f, 0));
        }
    }

    // ---- 友谊树: three stakes, a collar where they meet the trunk, and the cloth round the trunk ----

    private static void Tree(SiteBuilder b, Vector2 at, float g)
    {
        var segments = b.Detail switch { SiteDetail.Full => 16, SiteDetail.Plain => 10, _ => 6 };
        b.Add(Shapes.Shaft(F.WrapRadius, F.WrapRadius * 0.94f, F.WrapTop - F.WrapBottom, segments, L.Cloth), Matrix4x4.CreateTranslation(at.X, g + F.WrapBottom, at.Y));
        foreach (var (foot, top) in Stakes(at))
        {
            Vector3 from = new(foot.X, g - 0.3f, foot.Y), to = new(top.X, g + F.StakeTop, top.Y);
            var axis = to - from;
            b.Add(Shapes.Shaft(F.StakeRadius, F.StakeRadius * 0.85f, axis.Length(), Math.Max(6, segments / 2), L.Timber), WenkeSite.Along(from, axis / axis.Length()));
        }
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(Shapes.Shaft(F.WrapRadius + 0.05f, F.WrapRadius + 0.05f, 0.14f, segments, L.Timber), Matrix4x4.CreateTranslation(at.X, g + F.StakeTop - 0.2f, at.Y));
    }

    // ---- The landscape stone and its gold emblem ----

    /// <summary>
    /// The granite stone sunk 8 cm into the lawn, its dressed face toward the forecourt. The emblem (ART-STYLE: a
    /// round badge, a fine outer ring round an octagonal device) is a single-colour gold relief on the face: a disc, a
    /// raised ring near its rim and an octagon in the middle; the circuit traces wait for the detail pass.
    /// </summary>
    private static void Stone(SiteBuilder b, PlanFrame f, float g)
    {
        var at = f.At(0, g - 0.08f, 0);
        var (segments, rings) = b.Detail switch { SiteDetail.Full => (40, 12), SiteDetail.Plain => (24, 8), _ => (14, 5) };
        b.Add(Solids.Boulder(F.Stone, segments, rings, L.GreyStone), at);
        if (b.Detail == SiteDetail.Mass) return;
        var face = GeometryPlacement.Multiply(EmblemAt(), at);
        var small = b.Detail == SiteDetail.Full ? 32 : 16;
        b.Add(Shapes.Shaft(F.EmblemRadius, F.EmblemRadius, F.EmblemRelief, small, L.Gold), face);
        b.Add(Shapes.Ring(Vector2.Zero, F.EmblemRadius * 0.82f, 0.022f, F.EmblemRelief * 1.8f, small, L.Gold, 0f), face);
        b.Add(Shapes.Shaft(F.EmblemRadius * 0.5f, F.EmblemRadius * 0.5f, F.EmblemRelief * 2.2f, 8, L.Gold), face);
    }

    /// <summary>The emblem's frame on the stone face: local Y out of the face, X across it, set 5 mm into the stone.</summary>
    internal static Matrix4x4 EmblemAt()
    {
        var stone = F.Stone;
        Vector3 x = Vector3.UnitX, y = stone.FaceNormal;
        var z = Vector3.Cross(x, y);
        var p = stone.OnFace(0, F.EmblemY) - y * 0.005f;
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, p.X, p.Y, p.Z, 1);
    }

    // ---- Collision: paving to walk on, posts for the stakes, a box for the stone ----

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var door = WenkeSite.Door(world);
        var (min, max) = ForecourtRect(world);
        var mid = (min + max) * 0.5f;
        if (c.Includes(SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.ForecourtId), K.PavingKerb)))
        {
            var g = c.Ground(door.P(mid.X, mid.Y).X, door.P(mid.X, mid.Y).Y);
            c.Box(door.At(mid.X, g + (K.PavingTop - 0.1f) * 0.5f, mid.Y), new((max.X - min.X) * 0.5f, (K.PavingTop + 0.1f) * 0.5f, (max.Y - min.Y) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        }
        var (walk, walkLength, walkWidth) = Walk(world);
        if (c.Includes(SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.WalkId), K.PavingKerb)))
        {
            var centre = walk.P(walkLength * 0.5f, 0);
            var gw = c.Ground(centre.X, centre.Y);
            c.Box(walk.At(walkLength * 0.5f, gw + (K.PavingTop - 0.1f) * 0.5f, 0), new(walkLength * 0.5f, (K.PavingTop + 0.1f) * 0.5f, walkWidth * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        }
        var tree = TreeAt(world);
        if (c.Includes(TreeRange(world)))
        {
            var gt = c.Ground(tree.X, tree.Y);
            foreach (var (foot, top) in Stakes(tree))
                c.Add(new PostCollider((foot * 0.6f + top * 0.4f), F.StakeRadius + 0.1f, gt, gt + F.StakeTop * 0.6f, SurfaceTag.Prop) { Foot = FootMat.Bark });
        }
        if (!c.Includes(StoneRange(world))) return;
        var stoneFrame = StoneFrame(world);
        var st = F.Stone;
        var sg = c.Ground(stoneFrame.Origin.X, stoneFrame.Origin.Y);
        var back = st.Depth * 0.5f;
        c.Box(stoneFrame.At(st.Shear * 0.5f, sg + st.Height * 0.5f - 0.08f, (st.FaceOut - back) * 0.5f),
            new(st.Length * 0.475f, st.Height * 0.5f, (st.FaceOut + back) * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }
}
