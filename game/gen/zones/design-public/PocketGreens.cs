using System.Numerics;
using System.Text.Json;
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
/// Pure .NET: reads the world model and terrain only, angles through DetMath.
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
            float r = Number(f, "radius"), from = Number(f, "from"), to = Number(f, "to");
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
            if (Text(f, "stone") != "site") continue;
            if (f.Geometry is not PointGeometry point) throw new InvalidDataException($"{f.Id.Value}: a round pit is a point with props.radius.");
            var r = Number(f, "radius");
            if (!(r > K.KerbWidth * 2f)) throw new InvalidDataException($"{f.Id.Value}: pit radius too small.");
            list.Add(new(f.Id, new(point.Position.X, point.Position.Z), r));
        }
        return list;
    }

    private static IEnumerable<WorldFeature> Ours(IWorldModel world, string kind) =>
        world.Features.Values.Where(f => f.Id.Value.StartsWith(PublicTune.Prefix, StringComparison.Ordinal) && f.Kind.Value == kind)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal);

    private static float Number(WorldFeature f, string name) =>
        f.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetSingle(out var x) && float.IsFinite(x)
            ? x : throw new InvalidDataException($"{f.Id.Value}: props.{name} must be a finite number.");

    private static string? Text(WorldFeature f, string name) =>
        f.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    // ---- Plan helpers ----

    /// <summary>Unit plan direction (world x east, z south) of a bearing in degrees.</summary>
    public static Vector2 Dir(float bearing)
    {
        var a = bearing * (MathF.PI / 180f);
        return new(DetMath.Sin(a), -DetMath.Cos(a));
    }

    /// <summary>Points on the arc from <paramref name="from"/> to <paramref name="to"/>, segments + 1 of them.</summary>
    public static Vector2[] Arc(Vector2 c, float r, float from, float to, int segments)
    {
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++) points[i] = c + Dir(from + (to - from) * i / segments) * r;
        return points;
    }

    /// <summary>Counterclockwise (in x, z) closed circle without a repeated point.</summary>
    public static Vector2[] Circle(Vector2 c, float r, int segments)
    {
        var points = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            points[i] = new(c.X + r * DetMath.Cos(a), c.Y + r * DetMath.Sin(a));
        }
        return points;
    }

    /// <summary>
    /// Rigid placement whose local +Z points along plan direction <paramref name="outward"/> and local X along the arc
    /// (the PlanFrame convention: Z = (−U.z, U.x)), origin at plan <paramref name="p"/>, height <paramref name="y"/>.
    /// </summary>
    public static Matrix4x4 Facing(Vector2 p, Vector2 outward, float y)
    {
        var u = new Vector2(outward.Y, -outward.X);
        return new(u.X, 0, u.Y, 0, 0, 1, 0, 0, outward.X, 0, outward.Y, 0, p.X, y, p.Y, 1);
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
            var o = Dir(bearing);
            Add(output, PartGeometryKernel.BeveledBox(B.PlinthLength, B.PlinthHeight, B.PlinthDepth, lod == LodLevel.L0 ? B.PlinthBevel : 0.01f, L.Granite),
                Facing(bench.Centre + o * bench.Radius, o, deck));
            if (lod >= LodLevel.L2) continue;
            Add(output, PartGeometryKernel.BeveledBox(B.CarrierLength, B.CarrierHeight, B.CarrierDepth, 0.005f, L.Steel),
                Facing(bench.Centre + o * bench.Radius, o, deck + B.PlinthHeight));
            Add(output, PartGeometryKernel.BeveledBox(B.PostSize, B.BackTop - B.SeatTop + B.SlatThickness, B.PostSize, 0.008f, L.Steel),
                Facing(bench.Centre + o * (bench.Radius + B.PostOffset), o, deck + B.SeatTop - B.SlatThickness));
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
        var deck = ground(pit.Centre) + PublicTune.PavingLevel;
        var kerb = PartGeometryKernel.BeveledRectangleProfile(K.KerbWidth, K.KerbHeight + 0.05f, lod == LodLevel.L0 ? K.KerbBevel : 0f);
        var ring = Circle(pit.Centre, pit.Radius - K.KerbWidth * 0.5f, segments);
        AddWorld(output, PartGeometryKernel.Sweep(kerb, ring, L.Granite, closed: true, baseY: deck - 0.05f));
        var soil = Circle(pit.Centre, pit.Radius - K.KerbWidth + 0.01f, segments);
        AddWorld(output, PartGeometryKernel.Extrude(new PolygonRings(soil), K.KerbHeight - K.SoilBelow + 0.05f, L.Soil, deck - 0.05f));
    }

    /// <summary>The paving level under a bench: WP-15's paving over the highest ground under the arc.</summary>
    public static float Paving(ArcBench bench, Func<Vector2, float> ground)
    {
        var top = float.MinValue;
        foreach (var p in Arc(bench.Centre, bench.Radius, bench.From, bench.To, 4)) top = MathF.Max(top, ground(p));
        return top + PublicTune.PavingLevel;
    }

    // ---- Colliders and seats (same parameters as the geometry, SYS-WALK) ----

    /// <summary>Jumpable boxes along the arc, one per chord, from the paving to the seat top (MAP-QSH-ADDITIONS: 可跳上).</summary>
    public static IReadOnlyList<BoxCollider> SeatBoxes(ArcBench bench, Func<Vector2, float> ground)
    {
        var deck = Paving(bench, ground);
        var n = B.ColliderBoxes;
        var step = (bench.To - bench.From) / n;
        var half = bench.Radius * DetMath.Sin(step * 0.5f * (MathF.PI / 180f));
        var boxes = new BoxCollider[n];
        for (var i = 0; i < n; i++)
        {
            var mid = bench.From + step * (i + 0.5f);
            var o = Dir(mid);
            var centre = bench.Centre + o * (bench.Radius * DetMath.Cos(step * 0.5f * (MathF.PI / 180f)));
            boxes[i] = new BoxCollider(Facing(centre, o, deck + B.SeatTop * 0.5f), new(half, B.SeatTop * 0.5f, B.HalfDepth), SurfaceTag.Prop) { Foot = FootMat.WoodDeck };
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
        var deck = ground(pit.Centre) + PublicTune.PavingLevel;
        return new PostCollider(pit.Centre, pit.Radius, deck - 0.05f, deck + K.KerbHeight, SurfaceTag.Prop) { Foot = FootMat.StonePaving };
    }

    // ---- Writers ----

    private static void Sweep(ICollection<GeometryBundle> output, float width, float height, float bevel, ToonSpec material, Vector2[] path, float baseY) =>
        AddWorld(output, PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(width, height, bevel), path, material, closed: false, baseY: baseY));

    private static void AddWorld(ICollection<GeometryBundle> output, PartGeometry geometry)
    {
        foreach (var mesh in geometry.Meshes) output.Add(mesh);
    }

    /// <summary>Places part geometry by a rigid row-vector matrix, written out in scalar arithmetic (no SIMD paths).</summary>
    private static void Add(ICollection<GeometryBundle> output, PartGeometry geometry, Matrix4x4 at)
    {
        foreach (var mesh in geometry.Meshes)
        {
            var count = mesh.Position.Length / 3;
            var position = new float[mesh.Position.Length];
            var normal = new float[mesh.Normal.Length];
            for (var i = 0; i < count; i++)
            {
                float x = mesh.Position[3 * i], y = mesh.Position[3 * i + 1], z = mesh.Position[3 * i + 2];
                position[3 * i] = x * at.M11 + y * at.M21 + z * at.M31 + at.M41;
                position[3 * i + 1] = x * at.M12 + y * at.M22 + z * at.M32 + at.M42;
                position[3 * i + 2] = x * at.M13 + y * at.M23 + z * at.M33 + at.M43;
                float nx = mesh.Normal[3 * i], ny = mesh.Normal[3 * i + 1], nz = mesh.Normal[3 * i + 2];
                normal[3 * i] = nx * at.M11 + ny * at.M21 + nz * at.M31;
                normal[3 * i + 1] = nx * at.M12 + ny * at.M22 + nz * at.M32;
                normal[3 * i + 2] = nx * at.M13 + ny * at.M23 + nz * at.M33;
            }
            output.Add(mesh with { Position = position, Normal = normal });
        }
    }
}
