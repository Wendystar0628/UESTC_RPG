using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.South.Framework;
using K = Campus.Gen.Zones.South.SouthTune.Lake;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.EastLake;

/// <summary>
/// East lake shore (MAP-QSH-SOUTH §4). The water is WP-24's, the trees, reeds and lotus WP-23's and WP-38's, the paving of the
/// waterside platform WP-15's; this site builds:
/// - the Liren-side hard bank along the lake's rectilinear north edge: brick-red retaining wall, red sandstone coping and a
///   white vase-baluster balustrade (1.08 m, collision 1.5 m, SYS-WALK §7), opened in the bay for the wade step (§4.3,
///   design:south-eastlake-wade-step): two treads down from the platform, the second 0.15 m under the normal water level;
/// - two wooden rowing boats moored off the wade step (§4.6) and two stone mooring posts;
/// - the Wu Liren statue on its polished dark stone pedestal, facing the lake (§4.9);
/// - the double-eaved hexagonal timber pavilion on the south-bank hill top (§4.5), standing on the terrain height.
/// </summary>
public sealed class EastLakeSite : SouthSite
{
    public const string Lake = "qsh:relation:7849760";
    public const string Statue = "qsh:node:7867995301";
    public const string WadeStep = "design:south-eastlake-wade-step";

    private static readonly StepsPart Steps = new();

    public override string Id => "18b.east-lake";

    /// <summary>The wade step bay: x along the bank back edge, +Z out over the water.</summary>
    internal static PlanFrame WadeFrame => PlanFrame.Toward(K.HardBank[K.WadeSegment], K.HardBank[K.WadeSegment + 1]);

    internal static float WadeLength => Vector2.Distance(K.HardBank[K.WadeSegment], K.HardBank[K.WadeSegment + 1]);

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            var eye = WadeFrame.P(WadeLength * 0.5f - 1.5f, K.Tread1Depth + K.Tread2Depth * 0.5f);
            return [Probe("Q3", Lake, new(eye.X, K.Tread2 + 1.62f, eye.Y), WadeFrame.W, -12f, 2f)];
        }
    }

    public override void Build(SiteBuilder b)
    {
        var water = Water(b.Context.World);
        var y = b.Ground(K.HardBank[K.WadeSegment]);
        var segments = BankSegments(water).ToArray();
        var rail = RailLine(segments);
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        foreach (var (f, l, i) in segments)
        {
            Bank(b, f, l, i, y);
            Balustrade(b, rail[i], rail[i + 1], RailOpenings(f, l, i, rail[i], rail[i + 1]), y + K.CopingTop, b.Detail);
        }
        // A raised coping block and a newel at every bank vertex: the mitred segments meet under it.
        foreach (var p in rail)
        {
            var corner = new PlanFrame(p, Vector2.UnitX);
            b.Add(PartGeometryKernel.BeveledBox(0.85f, K.CopingTop + 0.14f, 0.85f, bevel, L.Sandstone), corner.At(0, y - 0.1f, 0));
            Newel(b, corner, 0, y + K.CopingTop + 0.03f, bevel);
        }
        WadeSteps(b, y);
        Boats(b, y);
        WuLiren(b, b.Ground(K.Statue));
        Pavilion(b, b.Ground(K.Pavilion));
    }

    /// <summary>The lake's water area: outer ring and island holes (WP-24 draws the surface on the same polygon).</summary>
    internal static PolygonRings Water(IWorldModel world) =>
        world.Features.TryGetValue(new(Lake), out var lake) && lake.Geometry is PolygonGeometry g
            ? g.Rings
            : throw new InvalidOperationException($"East lake {Lake} is missing or not a single polygon.");

    internal static bool InWater(PolygonRings water, Vector2 p) => PlanMath.Inside(water.Outer, p) && !water.Holes.Any(h => PlanMath.Inside(h, p));

    /// <summary>Hard-bank segments as frames with +Z over the water, x from 0 to the segment length.</summary>
    internal static IEnumerable<(PlanFrame Frame, float Length, int Index)> BankSegments(PolygonRings water)
    {
        for (var i = 0; i + 1 < K.HardBank.Length; i++)
        {
            Vector2 a = K.HardBank[i], c = K.HardBank[i + 1];
            var f = PlanFrame.Toward(a, c);
            var l = Vector2.Distance(a, c);
            if (!InWater(water, f.P(l * 0.5f, 0.6f))) f = PlanFrame.Toward(c, a);
            yield return (f, l, i);
        }
    }

    /// <summary>The wade opening along a segment (local x), empty elsewhere.</summary>
    internal static (float U0, float U1)[] Openings(int index, float length) =>
        index == K.WadeSegment ? [(length * 0.5f - K.WadeWidth * 0.5f, length * 0.5f + K.WadeWidth * 0.5f)] : [];

    /// <summary>
    /// Points on the balustrade line, RailSetback back from the waterline, at every bank vertex: mitred between the two
    /// segments' water normals, so the rails of neighbouring segments meet on one newel.
    /// </summary>
    internal static Vector2[] RailLine((PlanFrame Frame, float Length, int Index)[] segments)
    {
        var n = K.HardBank.Length;
        var points = new Vector2[n];
        for (var v = 0; v < n; v++)
        {
            Vector2 offset;
            if (v == 0) offset = segments[0].Frame.W;
            else if (v == n - 1) offset = segments[^1].Frame.W;
            else
            {
                Vector2 n1 = segments[v - 1].Frame.W, n2 = segments[v].Frame.W;
                offset = (n1 + n2) / (1f + n1.X * n2.X + n1.Y * n2.Y);
            }
            points[v] = K.HardBank[v] - offset * K.RailSetback;
        }
        return points;
    }

    /// <summary>A segment's openings carried over to its balustrade line from a to c.</summary>
    private static (float U0, float U1)[] RailOpenings(PlanFrame segment, float length, int index, Vector2 a, Vector2 c)
    {
        var line = PlanFrame.Toward(a, c);
        return [.. Openings(index, length).Select(o =>
        {
            var u0 = line.Local(segment.P(o.U0, 0)).X;
            var u1 = line.Local(segment.P(o.U1, 0)).X;
            return (MathF.Min(u0, u1), MathF.Max(u0, u1));
        })];
    }

    // ---- Hard bank (§4.3) ----

    private static void Bank(SiteBuilder b, PlanFrame f, float l, int index, float y)
    {
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        foreach (var (u0, u1) in GridFacade.Open(0, l, Openings(index, l)))
        {
            // Retaining wall from below the water to the coping, its face on the waterline; the coping overhangs 8 cm.
            GridFacade.Box(b, f, u0 - 0.2f, u1 + 0.2f, y + K.WallBottom, y - 0.1f, -0.5f, 0, L.BrickRed, 0);
            GridFacade.Box(b, f, u0, u1, y - 0.1f, y + K.CopingTop, -0.55f, 0.08f, L.Sandstone, bevel);
        }
    }

    /// <summary>
    /// White stone balustrade along its own line from a to c, outside the openings: plinth rail, vase balusters (lathed at
    /// L0, square at L1, none further out), top rail; newels at the opening ends and at most NewelPitch apart (the vertex
    /// newels are placed by the caller).
    /// </summary>
    private static void Balustrade(SiteBuilder b, Vector2 a, Vector2 c, (float U0, float U1)[] openings, float y, SiteDetail detail)
    {
        const float newel = 0.34f;
        var f = PlanFrame.Toward(a, c);
        var l = Vector2.Distance(a, c);
        var bevel = detail == SiteDetail.Full ? 0.02f : 0f;
        var top = y + K.RailTop - K.CopingTop;
        foreach (var (p0, p1) in GridFacade.Open(0, l, openings))
        {
            var bays = Math.Max(1, (int)MathF.Ceiling((p1 - p0) / K.NewelPitch));
            var pitch = (p1 - p0) / bays;
            for (var k = 0; k <= bays; k++)
            {
                var vertex = (k == 0 && p0 < 0.01f) || (k == bays && p1 > l - 0.01f);
                if (!vertex) Newel(b, f, p0 + k * pitch, y, bevel);
            }
            for (var k = 0; k < bays; k++)
            {
                float u0 = p0 + k * pitch + newel * 0.5f, u1 = p0 + (k + 1) * pitch - newel * 0.5f;
                if (u1 - u0 < 0.3f) continue;
                GridFacade.Box(b, f, u0, u1, y, y + 0.16f, -0.14f, 0.14f, L.Baluster, bevel);
                GridFacade.Box(b, f, u0, u1, top - 0.18f, top, -0.15f, 0.15f, L.Baluster, bevel);
                if (detail == SiteDetail.Mass) continue;
                var count = Math.Max(1, (int)MathF.Floor((u1 - u0) / K.BalusterPitch));
                var step = (u1 - u0) / count;
                var height = top - 0.18f - (y + 0.16f);
                for (var j = 0; j < count; j++)
                {
                    var x = u0 + step * (j + 0.5f);
                    if (detail == SiteDetail.Full)
                        b.Add(Solids.Lathe(Baluster(height), 8, L.Baluster), f.At(x, y + 0.16f, 0));
                    else
                        b.Add(PartGeometryKernel.BeveledBox(0.11f, height, 0.11f, 0, L.Baluster), f.At(x, y + 0.16f, 0));
                }
            }
        }
    }

    /// <summary>Vase baluster profile (radius, y) for a given height: foot, belly, neck, head.</summary>
    internal static Vector2[] Baluster(float h) =>
        [new(0.06f, 0), new(0.06f, 0.08f * h), new(0.035f, 0.15f * h), new(0.07f, 0.45f * h), new(0.072f, 0.52f * h),
         new(0.038f, 0.8f * h), new(0.055f, 0.9f * h), new(0.055f, h)];

    private static void Newel(SiteBuilder b, PlanFrame f, float u, float y, float bevel)
    {
        var top = y + K.RailTop - K.CopingTop + 0.06f;
        GridFacade.Box(b, f, u - 0.17f, u + 0.17f, y, top, -0.17f, 0.17f, L.Baluster, bevel);
        GridFacade.Box(b, f, u - 0.21f, u + 0.21f, top, top + 0.08f, -0.21f, 0.21f, L.Baluster, bevel);
    }

    // ---- Wade step (design:south-eastlake-wade-step) and the boats (§4.3, §4.6) ----

    private static void WadeSteps(SiteBuilder b, float y)
    {
        var f = WadeFrame;
        var (u0, u1) = Openings(K.WadeSegment, WadeLength)[0];
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        // Riser from the platform down to the first tread, then the two treads; no per-step outline (18a review).
        GridFacade.Box(b, f, u0, u1, y + K.Tread1, y, -0.45f, 0, L.StepStone, 0);
        GridFacade.Box(b, f, u0, u1, y + K.WallBottom, y + K.Tread1, 0, K.Tread1Depth, L.StepStone, bevel);
        GridFacade.Box(b, f, u0, u1, y + K.WallBottom, y + K.Tread2, K.Tread1Depth, K.Tread1Depth + K.Tread2Depth, L.StepStone, bevel);
        // Mooring posts on the platform at the opening ends.
        foreach (var u in new[] { u0 - 0.6f, u1 + 0.6f })
            GridFacade.Box(b, f, u - 0.15f, u + 0.15f, y, y + 0.65f, -0.75f, -0.45f, L.Granite, bevel);
    }

    private static void Boats(SiteBuilder b, float y)
    {
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        var f = WadeFrame;
        var z = K.Tread1Depth + K.Tread2Depth + 0.35f + K.BoatBeam * 0.5f;
        foreach (var x in new[] { WadeLength * 0.5f - K.BoatLength * 0.6f, WadeLength * 0.5f + K.BoatLength * 0.6f })
        {
            var keel = y + K.BoatKeel;
            b.Add(Solids.Hull(K.BoatLength, K.BoatBeam, K.BoatDepth, L.BoatWood, L.BoatInside), f.AtTurned(x, keel, z));
            if (b.Detail == SiteDetail.Mass) continue;
            // Thwarts across the hull (the hull's length runs along the frame's −x in the turned placement).
            foreach (var (along, width) in new[] { (-0.7f, K.BoatBeam * 0.84f), (0.6f, K.BoatBeam * 0.8f) })
                b.Add(PartGeometryKernel.BeveledBox(0.26f, 0.05f, width, 0, L.BoatWood), f.At(x - along, keel + K.BoatDepth * 0.62f, z));
        }
    }

    // ---- Wu Liren statue (§4.9) ----

    private static void WuLiren(SiteBuilder b, float y)
    {
        var f = PlanMath.Facing(K.Statue, K.StatueFacing);
        var bevel = b.Detail == SiteDetail.Full ? 0.03f : 0f;
        b.Add(PartGeometryKernel.BeveledBox(2.0f, 0.3f, 2.0f, bevel, L.DarkStone), f.At(0, y - 0.05f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, 1.5f, 1.3f, bevel, L.DarkStone), f.At(0, y + 0.25f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.5f, 0.2f, 1.5f, bevel, L.DarkStone), f.At(0, y + 1.75f, 0));
        var foot = y + 1.95f;
        var s = K.StatueScale;
        var segments = b.Detail switch { SiteDetail.Full => 24, SiteDetail.Plain => 14, _ => 8 };
        Vector2 P(float r, float h) => new(r * s, h * s);
        // A standing figure in a long coat: plinth slab, coat to the shoulders, neck and head, arms at the sides.
        b.Add(PartGeometryKernel.BeveledBox(0.8f * s, 0.08f, 0.6f * s, 0, L.Bronze), f.At(0, foot, 0));
        b.Add(Solids.Lathe([P(0.2f, 0), P(0.24f, 0.06f), P(0.27f, 0.5f), P(0.255f, 0.72f), P(0.215f, 1.0f), P(0.25f, 1.32f), P(0.3f, 1.5f),
            P(0.29f, 1.58f), P(0.12f, 1.64f), P(0.075f, 1.7f)], segments, L.Bronze, 1f, 0.62f), f.At(0, foot + 0.08f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(Solids.Lathe([P(0.07f, 0), P(0.095f, 0.04f), P(0.11f, 0.12f), P(0.105f, 0.2f), P(0.08f, 0.26f), P(0, 0.29f)], segments, L.Bronze, 1f, 0.95f),
            f.At(0, foot + 0.08f + 1.69f * s, 0.01f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(Shapes.Shaft(0.07f * s, 0.078f * s, 0.78f * s, segments / 2, L.Bronze), f.At(side * 0.305f * s, foot + 0.08f + 0.72f * s, 0.02f));
    }

    // ---- Hexagonal double-eaved pavilion on the south-bank hill (§4.5, MAP-QSH-TERRAIN §5.2) ----

    /// <summary>Corner angle offset: one face of the hexagon looks north, back across the lake to the hard bank.</summary>
    private const float Phase = MathF.PI / 3f;

    private static Vector2 Corner(PlanFrame f, int i, float r)
    {
        var a = Phase + MathF.PI / 3f * i;
        return f.P(r * DetMath.Cos(a), r * DetMath.Sin(a));
    }

    private static Vector2[] Hexagon(PlanFrame f, float r) => [.. Enumerable.Range(0, 6).Select(i => Corner(f, i, r))];

    internal static PlanFrame PavilionFrame => PlanMath.Facing(K.Pavilion, 0f);

    private static void Pavilion(SiteBuilder b, float y)
    {
        var f = PavilionFrame;
        var detail = b.Detail;
        var bevel = detail == SiteDetail.Full ? 0.02f : 0f;
        var floor = y + K.PavilionFloor;
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(Hexagon(f, K.PavilionPlatform)), K.PavilionFloor + 0.1f, L.Granite, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(Hexagon(f, K.PavilionPlatform + 0.08f)), 0.08f, L.StoneTrim, floor - 0.08f), Matrix4x4.Identity);
        var flight = PavilionFlight();
        b.Add(Steps.Lods[b.Lod](flight), f.AtReversed(0, y, Apothem(K.PavilionPlatform) + flight.Depth * 0.5f));
        var segments = detail == SiteDetail.Full ? 16 : 10;
        for (var i = 0; i < 6; i++)
        {
            var c = Corner(f, i, K.PavilionColumns);
            var at = new PlanFrame(c, f.U);
            b.Add(PartGeometryKernel.BeveledBox(0.42f, 0.22f, 0.42f, bevel, L.StoneTrim), at.At(0, floor, 0));
            b.Add(Shapes.Shaft(0.14f, 0.125f, K.PavilionColumnHeight - 0.22f, segments, L.RedTimber), at.At(0, floor + 0.22f, 0));
            // Ring beam between column heads; bench boards on every side but the entrance (north) one.
            var next = Corner(f, i + 1, K.PavilionColumns);
            var beam = PlanFrame.Toward(c, next);
            var length = Vector2.Distance(c, next);
            b.Add(PartGeometryKernel.BeveledBox(length + 0.2f, 0.3f, 0.2f, bevel, L.Wood), beam.At(length * 0.5f, floor + K.PavilionColumnHeight - 0.3f, 0));
            if (detail == SiteDetail.Mass || IsEntrance(f, c, next)) continue;
            b.Add(PartGeometryKernel.BeveledBox(length - 0.4f, 0.07f, 0.38f, 0, L.Wood), beam.At(length * 0.5f, floor + 0.42f, -0.2f));
            b.Add(PartGeometryKernel.BeveledBox(length - 0.4f, 0.42f, 0.06f, 0, L.Wood), beam.At(length * 0.5f, floor, -0.02f));
        }
        // Lower eave: its soffit meets the ring beam at the column circle, so the eave edge hangs below the column heads.
        var head = floor + K.PavilionColumnHeight;
        var perSide = detail == SiteDetail.Full ? 4 : 2;
        var lower = head + 0.16f - K.LowerRise * (K.LowerEave - K.PavilionColumns) / (K.LowerEave - K.DrumRadius + 0.05f);
        b.Add(Solids.PolyRoof(6, K.LowerEave, K.DrumRadius - 0.05f, K.LowerRise, 0.35f, 0.16f, perSide, Phase, L.Tiles, L.Wood), f.At(0, lower, 0));
        // Drum between the eaves, then the upper roof, its soffit meeting the drum, and the finial.
        var drumTop = lower + K.LowerRise + 0.6f;
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(Hexagon(f, K.DrumRadius)), drumTop - (lower + K.LowerRise - 0.2f), L.Wood, lower + K.LowerRise - 0.2f), Matrix4x4.Identity);
        var upper = drumTop + 0.14f - K.UpperRise * (K.UpperEave - K.DrumRadius) / K.UpperEave;
        b.Add(Solids.PolyRoof(6, K.UpperEave, 0, K.UpperRise, 0.3f, 0.14f, perSide, Phase, L.Tiles, L.Wood), f.At(0, upper, 0));
        if (detail != SiteDetail.Mass)
            b.Add(Solids.Lathe([new(0.15f, 0), new(0.19f, 0.12f), new(0.1f, 0.26f), new(0.13f, 0.36f), new(0, 0.5f)], 12, L.Tiles), f.At(0, upper + K.UpperRise - 0.05f, 0));
    }

    private static StairParams PavilionFlight() =>
        new(2.2f, K.PavilionFloor, 3 * 0.34f, 3) { Variation = 2, Material = L.StepStone, Foot = FootMat.StonePaving };

    private static float Apothem(float r) => r * 0.8660254f;

    /// <summary>The side whose outward normal points along the frame's +Z (north) carries the steps and has no bench.</summary>
    private static bool IsEntrance(PlanFrame f, Vector2 a, Vector2 c)
    {
        var mid = f.Local((a + c) * 0.5f);
        return mid.Y > 0 && MathF.Abs(mid.X) < 0.5f;
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c)
    {
        var water = Water(c.Context.World);
        var y = c.Ground(K.HardBank[K.WadeSegment].X, K.HardBank[K.WadeSegment].Y);
        var segments = BankSegments(water).ToArray();
        var rail = RailLine(segments);
        foreach (var (f, l, i) in segments)
        {
            var line = PlanFrame.Toward(rail[i], rail[i + 1]);
            foreach (var (u0, u1) in GridFacade.Open(-0.17f, Vector2.Distance(rail[i], rail[i + 1]) + 0.17f, RailOpenings(f, l, i, rail[i], rail[i + 1])))
                c.Wall(line.P(u0, 0), line.P(u1, 0), 0.17f, y - 1f, y + 1.5f, SurfaceTag.Barrier, FootMat.StonePaving);
        }
        // Wade treads (auto-step 0.30 and 0.35 m) and the mooring posts.
        var w = WadeFrame;
        var (o0, o1) = Openings(K.WadeSegment, WadeLength)[0];
        var half = (o1 - o0) * 0.5f;
        var mid = (o0 + o1) * 0.5f;
        c.Box(w.At(mid, y + K.Tread1 - 0.15f, K.Tread1Depth * 0.5f), new(half, 0.15f, K.Tread1Depth * 0.5f), SurfaceTag.Stair, FootMat.StonePaving);
        c.Box(w.At(mid, y + K.Tread2 - 0.15f, K.Tread1Depth + K.Tread2Depth * 0.5f), new(half, 0.15f, K.Tread2Depth * 0.5f), SurfaceTag.Stair, FootMat.StonePaving);
        foreach (var u in new[] { o0 - 0.6f, o1 + 0.6f })
            c.Add(new PostCollider(w.P(u, -0.6f), 0.2f, y, y + 0.65f, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        // Statue pedestal.
        var sy = c.Ground(K.Statue.X, K.Statue.Y);
        c.Box(PlanMath.Facing(K.Statue, K.StatueFacing).At(0, sy + 1.0f, 0), new(1.0f, 1.0f, 1.0f), SurfaceTag.Prop, FootMat.PolishedStone);
        // Pavilion: platform, steps, columns, benches.
        var p = PavilionFrame;
        var py = c.Ground(K.Pavilion.X, K.Pavilion.Y);
        c.Add(new SlabCollider(Hexagon(p, K.PavilionPlatform), py + K.PavilionFloor, K.PavilionFloor + 0.1f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
        var flight = PavilionFlight();
        c.Add(Steps.Collision(flight), p.AtReversed(0, py, Apothem(K.PavilionPlatform) + flight.Depth * 0.5f));
        for (var i = 0; i < 6; i++)
        {
            var a = Corner(p, i, K.PavilionColumns);
            var e = Corner(p, i + 1, K.PavilionColumns);
            c.Add(new PostCollider(a, 0.16f, py + K.PavilionFloor, py + K.PavilionFloor + K.PavilionColumnHeight, SurfaceTag.Prop) { Foot = FootMat.WoodDeck });
            if (IsEntrance(p, a, e)) continue;
            var beam = PlanFrame.Toward(a, e);
            var length = Vector2.Distance(a, e);
            c.Box(beam.At(length * 0.5f, py + K.PavilionFloor + 0.245f, -0.2f), new((length - 0.4f) * 0.5f, 0.245f, 0.19f), SurfaceTag.Prop, FootMat.WoodDeck);
        }
    }
}
