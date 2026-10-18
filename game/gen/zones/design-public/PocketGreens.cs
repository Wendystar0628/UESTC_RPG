using System.Numerics;
using Campus.Gen.World;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using B = Campus.Gen.Zones.DesignPublic.PublicTune.Bench;
using K = Campus.Gen.Zones.DesignPublic.PublicTune.Pit;
using L = Campus.Gen.Zones.DesignPublic.PublicTune.Look;

namespace Campus.Gen.Zones.DesignPublic;

/// <summary>
/// A curved bench of a pocket green (MAP-QSH-ADDITIONS): wood slats on granite plinths with a dark steel back,
/// swept along an arc about <see cref="Centre"/> from bearing <see cref="From"/> to <see cref="To"/> (degrees, 0 north,
/// 90 east). Sitters face the centre; the back faces outward, so a bench centred on bearing 241° turns its back to the
/// low autumn sun and is lit (MAP-QSH-ADDITIONS R1).
/// </summary>
public sealed record ArcBench(FeatureId Id, Vector2 Centre, float Radius, float From, float To);

/// <summary>A round tree pit whose kerb and soil this package builds (props stone = site); WP-23 plants the tree.</summary>
public sealed record RoundPit(FeatureId Id, Vector2 Centre, float Radius);

/// <summary>
/// WP-29's pocket greens read from the world model: every <c>design:public-</c> curved bench and site-built tree pit.
/// The paving under them is a pedestrian polygon in the same overlay, paved by WP-15 like every plaza; the tree is WP-23's.
/// Pure .NET: reads the world model and terrain only, angles through MathF.
/// </summary>
public static class PocketGreens
{
    public static IReadOnlyList<ArcBench> Benches(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var list = new List<ArcBench>();
        foreach (var f in Ours(world, PublicTune.ArcBenchKind))
        {
            if (f.Geometry is not PointGeometry point) throw new InvalidDataException($"{f.Id.Value}: an arc bench is a point (its arc centre).");
            float r = f.Number("radius"), from = f.Number("from"), to = f.Number("to");
            if (!(r > 1f) || !(to > from) || to - from > 180f) throw new InvalidDataException($"{f.Id.Value}: radius must exceed 1 m and the arc run from < to within 180°.");
            list.Add(new(f.Id, new(point.Position.X, point.Position.Z), r, from, to));
        }
        return list;
    }

    public static IReadOnlyList<RoundPit> Pits(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var list = new List<RoundPit>();
        foreach (var f in Ours(world, PublicTune.TreePitKind))
        {
            if (f.TryText("stone") != "site") continue;
            if (f.Geometry is not PointGeometry point) throw new InvalidDataException($"{f.Id.Value}: a round pit is a point with props.radius.");
            var r = f.Number("radius");
            if (!(r > K.KerbWidth * 2f)) throw new InvalidDataException($"{f.Id.Value}: pit radius too small.");
            list.Add(new(f.Id, new(point.Position.X, point.Position.Z), r));
        }
        return list;
    }

    private static IEnumerable<WorldFeature> Ours(IWorldModel world, string kind) =>
        world.Features.Values.Where(f => f.Id.Value.StartsWith(PublicIds.Prefix, StringComparison.Ordinal) && f.Kind.Value == kind)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal);

    // ---- Plan helpers ----

    /// <summary>Points on the arc from <paramref name="from"/> to <paramref name="to"/>, segments + 1 of them.</summary>
    public static Vector2[] Arc(Vector2 c, float r, float from, float to, int segments)
    {
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++) points[i] = c + Angles.Heading(from + (to - from) * i / segments) * r;
        return points;
    }

    // ---- Geometry ----

    public static void Build(ArcBench bench, Func<Vector2, float> ground, LodLevel lod, ICollection<GeometryBundle> output)
    {
        var segments = lod switch { LodLevel.L0 => B.SegmentsL0, LodLevel.L1 => B.SegmentsL1, _ => B.SegmentsL2 };
        var deck = Paving(bench, ground);
        // Granite plinths, steel carriers and back posts at the same three stations along the arc.
        foreach (var f in B.PlinthAt)
        {
            var bearing = bench.From + (bench.To - bench.From) * f;
            var o = Angles.Heading(bearing);
            Add(output, PartGeometryKernel.BeveledBox(B.PlinthLength, B.PlinthHeight, B.PlinthDepth, lod == LodLevel.L0 ? B.PlinthBevel : 0.01f, L.Granite),
                new PlanFrame(bench.Centre + o * bench.Radius, new(o.Y, -o.X)).At(0, deck, 0));
            if (lod >= LodLevel.L2) continue;
            Add(output, PartGeometryKernel.BeveledBox(B.CarrierLength, B.CarrierHeight, B.CarrierDepth, 0.005f, L.Steel),
                new PlanFrame(bench.Centre + o * bench.Radius, new(o.Y, -o.X)).At(0, deck + B.PlinthHeight, 0));
            Add(output, PartGeometryKernel.BeveledBox(B.PostSize, B.BackTop - B.SeatTop + B.SlatThickness, B.PostSize, 0.008f, L.Steel),
                new PlanFrame(bench.Centre + o * (bench.Radius + B.PostOffset), new(o.Y, -o.X)).At(0, deck + B.SeatTop - B.SlatThickness, 0));
        }
        var seatBase = deck + B.SeatTop - B.SlatThickness;
        if (lod >= LodLevel.L2)
        {
            // One board for the seat and one for the back: the silhouette without the slat gaps.
            Sweep(output, B.HalfDepth * 2f - 0.04f, B.SlatThickness, 0.01f, L.Wood, Arc(bench.Centre, bench.Radius, bench.From, bench.To, segments), seatBase);
            Sweep(output, B.BackSlatThickness, B.BackTop - B.BackSlatBottoms[0], 0.005f, L.Wood,
                Arc(bench.Centre, bench.Radius + B.BackSlatOffset, bench.From, bench.To, segments), deck + B.BackSlatBottoms[0]);
            return;
        }
        foreach (var offset in B.SlatOffsets)
            Sweep(output, B.SlatWidth, B.SlatThickness, B.SlatBevel, L.Wood, Arc(bench.Centre, bench.Radius + offset, bench.From, bench.To, segments), seatBase);
        foreach (var bottom in B.BackSlatBottoms)
            Sweep(output, B.BackSlatThickness, B.BackSlatHeight, 0.006f, L.Wood,
                Arc(bench.Centre, bench.Radius + B.BackSlatOffset, bench.From, bench.To, segments), deck + bottom);
    }

    public static void Build(RoundPit pit, Func<Vector2, float> ground, LodLevel lod, ICollection<GeometryBundle> output)
    {
        var segments = lod switch { LodLevel.L0 => K.SegmentsL0, LodLevel.L1 => K.SegmentsL1, _ => K.SegmentsL2 };
        var deck = ground(pit.Centre) + GroundLevels.Paving;
        var kerb = PartGeometryKernel.BeveledRectangleProfile(K.KerbWidth, K.KerbHeight + 0.05f, lod == LodLevel.L0 ? K.KerbBevel : 0f);
        var ring = Shapes.Circle(pit.Centre, pit.Radius - K.KerbWidth * 0.5f, segments);
        AddWorld(output, PartGeometryKernel.Sweep(kerb, ring, L.Granite, closed: true, baseY: deck - 0.05f));
        var soil = Shapes.Circle(pit.Centre, pit.Radius - K.KerbWidth + 0.01f, segments);
        AddWorld(output, PartGeometryKernel.Extrude(new PolygonRings(soil), K.KerbHeight - K.SoilBelow + 0.05f, L.Soil, deck - 0.05f));
    }

    /// <summary>The paving level under a bench: WP-15's paving over the highest ground under the arc.</summary>
    public static float Paving(ArcBench bench, Func<Vector2, float> ground)
    {
        var top = float.MinValue;
        foreach (var p in Arc(bench.Centre, bench.Radius, bench.From, bench.To, 4)) top = MathF.Max(top, ground(p));
        return top + GroundLevels.Paving;
    }

    // ---- Colliders and seats (same parameters as the geometry, SYS-WALK) ----

    /// <summary>Jumpable boxes along the arc, one per chord, from the paving to the seat top (MAP-QSH-ADDITIONS: 可跳上).</summary>
    public static IReadOnlyList<BoxCollider> SeatBoxes(ArcBench bench, Func<Vector2, float> ground)
    {
        var deck = Paving(bench, ground);
        var n = B.ColliderBoxes;
        var step = (bench.To - bench.From) / n;
        var half = bench.Radius * MathF.Sin(step * 0.5f * (MathF.PI / 180f));
        var boxes = new BoxCollider[n];
        for (var i = 0; i < n; i++)
        {
            var mid = bench.From + step * (i + 0.5f);
            var o = Angles.Heading(mid);
            var centre = bench.Centre + o * (bench.Radius * MathF.Cos(step * 0.5f * (MathF.PI / 180f)));
            boxes[i] = new BoxCollider(new PlanFrame(centre, new(o.Y, -o.X)).At(0, deck + B.SeatTop * 0.5f, 0), new(half, B.SeatTop * 0.5f, B.HalfDepth), SurfaceTag.Prop) { Foot = FootMat.WoodDeck };
        }
        return boxes;
    }

    /// <summary>Seat edges on the inner (centre-facing) side of each seat box, with a backrest.</summary>
    public static IReadOnlyList<SeatDecl> Seats(ArcBench bench, Func<Vector2, float> ground)
    {
        var deck = Paving(bench, ground);
        return SeatBoxes(bench, ground).SelectMany(box => Seating.FromBox(box, SeatSide.Front, back: true, baseY: deck)).ToArray();
    }

    /// <summary>The pit kerb as a low post (0.2 m, stepped onto automatically); the trunk's collider is WP-23's.</summary>
    public static PostCollider PitPost(RoundPit pit, Func<Vector2, float> ground)
    {
        var deck = ground(pit.Centre) + GroundLevels.Paving;
        return new PostCollider(pit.Centre, pit.Radius, deck - 0.05f, deck + K.KerbHeight, SurfaceTag.Prop) { Foot = FootMat.StonePaving };
    }

    // ---- Writers ----

    private static void Sweep(ICollection<GeometryBundle> output, float width, float height, float bevel, ToonSpec material, Vector2[] path, float baseY) =>
        AddWorld(output, PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(width, height, bevel), path, material, closed: false, baseY: baseY));

    private static void AddWorld(ICollection<GeometryBundle> output, PartGeometry geometry)
    {
        foreach (var mesh in geometry.Meshes) output.Add(mesh);
    }

    private static void Add(ICollection<GeometryBundle> output, PartGeometry geometry, Matrix4x4 at)
    {
        foreach (var mesh in geometry.Meshes) output.Add(GeometryPlacement.Place(mesh, at).Mesh);
    }
}
