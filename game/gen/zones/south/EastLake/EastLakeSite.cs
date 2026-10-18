using Campus.Gen.Geometry;
using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.South.Framework;
using Campus.Gen.Zones.Common;
using Campus.Gen.World;
using K = Campus.Gen.Zones.South.SouthTune.Lake;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.EastLake;

/// <summary>
/// East lake shore (MAP-QSH-SOUTH). The water is WP-24's, the trees, reeds and lotus WP-23's and WP-38's, the paving of the
/// waterside platform WP-15's; this site builds:
/// - the Liren-side hard bank along the lake's rectilinear north edge: brick-red retaining wall, red sandstone coping and a
///   white vase-baluster balustrade (1.08 m, collision 1.5 m, SYS-WALK), opened in the bay for the wade step (§4.3,
///   design:south-eastlake-wade-step): two treads down from the platform, the second 0.15 m under the normal water level;
/// - two wooden rowing boats moored off the wade step (§4.6) and two stone mooring posts;
/// - the Wu Liren statue on its polished dark stone pedestal, facing the lake (§4.9);
/// - the double-eaved hexagonal timber pavilion on the south-bank hill top (§4.5), standing on the terrain height.
/// </summary>
public sealed class EastLakeSite : ZoneSite
{
    public const string Lake = "qsh:relation:7849760";
    public const string Statue = "qsh:node:7867995301";
    public const string WadeStep = "design:south-eastlake-wade-step";

    private static readonly StepsPart Steps = new();

    public override string Id => "18b.east-lake";

    /// <summary>
    /// The shore read from the map data (D-239): the Liren-side hard bank (the lake outline's last <see cref="K.BankVertices"/>
    /// vertices, the rectilinear north edge, west to east), the bank segment the wade step lies on, the step's width and two
    /// tread levels (design:south-eastlake-wade-step), and the statue's place.
    /// </summary>
    internal sealed record Shore(Vector2[] Bank, int WadeSegment, float WadeWidth, float Tread1, float Tread2, Vector2 StatueAt)
    {
        /// <summary>The wade step bay: x along the bank back edge, +Z out over the water.</summary>
        public PlanFrame WadeFrame => PlanFrame.Toward(Bank[WadeSegment], Bank[WadeSegment + 1]);

        public float WadeLength => Vector2.Distance(Bank[WadeSegment], Bank[WadeSegment + 1]);

        /// <summary>The wade opening along a segment (local x), empty elsewhere.</summary>
        public (float U0, float U1)[] Openings(int index, float length) =>
            index == WadeSegment ? [(length * 0.5f - WadeWidth * 0.5f, length * 0.5f + WadeWidth * 0.5f)] : [];
    }

    private static readonly ConditionalWeakTable<IWorldModel, Shore> Shores = new();

    internal static Shore ShoreOf(IWorldModel world) => Shores.GetValue(world, ReadShore);

    private static Shore ReadShore(IWorldModel world)
    {
        var outline = Water(world).Outer;
        var bank = outline.Skip(outline.Count - K.BankVertices).ToArray();
        if (bank[0].X > bank[^1].X) Array.Reverse(bank);
        var step = world.Feature(WadeStep);
        var at = step.Point();
        var wade = Enumerable.Range(0, bank.Length - 1).MinBy(i => DistanceToSegment(at, bank[i], bank[i + 1]));
        var treads = step.Numbers("treads");
        return new(bank, wade, step.Number("width"), treads[0], treads[1], world.Feature(Statue).Point());
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(Vector2.Dot(ab, ab), 1e-9f), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    public override IReadOnlyList<InspectionProbe> Probes =>
        // Q3: on the wade step's second tread, looking out over the water at the boats (a stand, not map data).
        [Probe("Q3", Lake, new(154.52f, -0.65f + 1.62f, 300.54f), new(-0.1789f, 0.9839f), -12f, 2f)];

    public override void Build(SiteBuilder b)
    {
        var shore = ShoreOf(b.Context.World);
        var water = Water(b.Context.World);
        var y = b.Ground(shore.Bank[shore.WadeSegment]);
        var segments = BankSegments(shore, water).ToArray();
        var rail = RailLine(shore, segments);
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        foreach (var (f, l, i) in segments)
        {
            Bank(b, shore, f, l, i, y);
            Balustrade(b, rail[i], rail[i + 1], RailOpenings(shore, f, l, i, rail[i], rail[i + 1]), y + K.CopingTop, b.Detail);
        }
        // A raised coping block and a newel at every bank vertex: the mitred segments meet under it.
        foreach (var p in rail)
        {
            var corner = new PlanFrame(p, Vector2.UnitX);
            b.Add(PartGeometryKernel.BeveledBox(0.85f, K.CopingTop + 0.14f, 0.85f, bevel, L.Sandstone), corner.At(0, y - 0.1f, 0));
            Newel(b, corner, 0, y + K.CopingTop + 0.03f, bevel);
        }
        WadeSteps(b, shore, y);
        Boats(b, shore, y);
        WuLiren(b, shore, b.Ground(shore.StatueAt));
        Pavilion(b, b.Ground(K.Pavilion));
    }

    /// <summary>
    /// The lake's water area: outer ring and island holes (WP-24 draws the surface on the same polygon). Read from its own source
    /// part; the entity's Geometry is only an aggregate envelope in some world models.
    /// </summary>
    internal static PolygonRings Water(IWorldModel world) =>
        world.Features.TryGetValue(new(Lake), out var lake) && (lake.Parts.FirstOrDefault(p => p.SourceId == LakePart)?.Geometry ?? lake.Geometry) is PolygonGeometry g
            ? g.Rings
            : throw new InvalidOperationException($"East lake {Lake} is missing or not a single polygon.");

    private const string LakePart = "osm:relation:7849760";

    internal static bool InWater(PolygonRings water, Vector2 p) => PlanGeometry.InsideByCrossings(water.Outer, p) && !water.Holes.Any(h => PlanGeometry.InsideByCrossings(h, p));

    /// <summary>Hard-bank segments as frames with +Z over the water, x from 0 to the segment length.</summary>
    internal static IEnumerable<(PlanFrame Frame, float Length, int Index)> BankSegments(Shore shore, PolygonRings water)
    {
        for (var i = 0; i + 1 < shore.Bank.Length; i++)
        {
            Vector2 a = shore.Bank[i], c = shore.Bank[i + 1];
            var f = PlanFrame.Toward(a, c);
            var l = Vector2.Distance(a, c);
            if (!InWater(water, f.P(l * 0.5f, 0.6f))) f = PlanFrame.Toward(c, a);
            yield return (f, l, i);
        }
    }

    /// <summary>
    /// Points on the balustrade line, RailSetback back from the waterline, at every bank vertex: mitred between the two
    /// segments' water normals, so the rails of neighbouring segments meet on one newel.
    /// </summary>
    internal static Vector2[] RailLine(Shore shore, (PlanFrame Frame, float Length, int Index)[] segments)
    {
        var n = shore.Bank.Length;
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
            points[v] = shore.Bank[v] - offset * K.RailSetback;
        }
        return points;
    }

    /// <summary>A segment's openings carried over to its balustrade line from a to c.</summary>
    private static (float U0, float U1)[] RailOpenings(Shore shore, PlanFrame segment, float length, int index, Vector2 a, Vector2 c)
    {
        var line = PlanFrame.Toward(a, c);
        return [.. shore.Openings(index, length).Select(o =>
        {
            var u0 = line.Local(segment.P(o.U0, 0)).X;
            var u1 = line.Local(segment.P(o.U1, 0)).X;
            return (MathF.Min(u0, u1), MathF.Max(u0, u1));
        })];
    }

    // ---- Hard bank (§4.3) ----

    private static void Bank(SiteBuilder b, Shore shore, PlanFrame f, float l, int index, float y)
    {
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        foreach (var (u0, u1) in GridFacade.Open(0, l, shore.Openings(index, l)))
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

    private static void WadeSteps(SiteBuilder b, Shore shore, float y)
    {
        var f = shore.WadeFrame;
        var (u0, u1) = shore.Openings(shore.WadeSegment, shore.WadeLength)[0];
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0f;
        // Riser from the platform down to the first tread, then the two treads; no per-step outline (18a review).
        GridFacade.Box(b, f, u0, u1, y + shore.Tread1, y, -0.45f, 0, L.StepStone, 0);
        GridFacade.Box(b, f, u0, u1, y + K.WallBottom, y + shore.Tread1, 0, K.Tread1Depth, L.StepStone, bevel);
        GridFacade.Box(b, f, u0, u1, y + K.WallBottom, y + shore.Tread2, K.Tread1Depth, K.Tread1Depth + K.Tread2Depth, L.StepStone, bevel);
        // Mooring posts on the platform at the opening ends.
        foreach (var u in new[] { u0 - 0.6f, u1 + 0.6f })
            GridFacade.Box(b, f, u - 0.15f, u + 0.15f, y, y + 0.65f, -0.75f, -0.45f, L.Granite, bevel);
    }

    private static void Boats(SiteBuilder b, Shore shore, float y)
    {
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        var f = shore.WadeFrame;
        var wadeLength = shore.WadeLength;
        var z = K.Tread1Depth + K.Tread2Depth + 0.35f + K.BoatBeam * 0.5f;
        foreach (var x in new[] { wadeLength * 0.5f - K.BoatLength * 0.6f, wadeLength * 0.5f + K.BoatLength * 0.6f })
        {
            var keel = y + K.BoatKeel;
            b.Add(LandmarkSolids.Hull(K.BoatLength, K.BoatBeam, K.BoatDepth, L.BoatWood, L.BoatInside), f.AtTurned(x, keel, z));
            if (b.Detail == SiteDetail.Mass) continue;
            // Thwarts across the hull (the hull's length runs along the frame's −x in the turned placement).
            foreach (var (along, width) in new[] { (-0.7f, K.BoatBeam * 0.84f), (0.6f, K.BoatBeam * 0.8f) })
                b.Add(PartGeometryKernel.BeveledBox(0.26f, 0.05f, width, 0, L.BoatWood), f.At(x - along, keel + K.BoatDepth * 0.62f, z));
        }
    }

    // ---- Wu Liren statue (§4.9) ----

    internal static PlanFrame StatueFrame(Shore shore) => PlanMath.Facing(shore.StatueAt, K.StatueFacing);

    /// <summary>
    /// Hook points for the detail pass (§4.9), world space: the seasonal leaf on the left shoulder (one season-masked instance
    /// set, SYS-TIME item 5) and the in-ground uplights in front of the pedestal (lamp.statue, off 1:00–5:30, §11).
    /// </summary>
    internal static (Vector3 Leaf, Vector3[] Uplights) StatueHooks(Shore shore, float y)
    {
        var f = StatueFrame(shore);
        Vector3 W(Vector3 local)
        {
            var p = f.P(local.X, local.Z);
            return new(p.X, y + K.PedestalTop + local.Y, p.Y);
        }
        return (W(K.StatueLeaf), [.. K.StatueUplights.Select(W)]);
    }

    /// <summary>
    /// Polished black stone pedestal (base slab, die, cap) and the bronze figure on it, facing the lake. The figure is a
    /// sculpture's simplified volumes at 1.25 × a 2 m model: an overcoat cinched at the waist whose skirt hangs in folds, square shoulders and a
    /// stand collar, trouser legs and shoes under the hem; the left arm hangs with the hand by the thigh, the right forearm is
    /// bent across the waist holding a rolled document. The head is an ovoid with a hair mass only: no
    /// features and no likeness (CORE-INDEX).
    /// </summary>
    private static void WuLiren(SiteBuilder b, Shore shore, float y)
    {
        var f = StatueFrame(shore);
        var detail = b.Detail;
        var bevel = detail == SiteDetail.Full ? 0.03f : 0f;
        b.Add(PartGeometryKernel.BeveledBox(2.0f, 0.3f, 2.0f, bevel, L.DarkStone), f.At(0, y - 0.05f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, 1.5f, 1.3f, bevel, L.DarkStone), f.At(0, y + 0.25f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.5f, 0.2f, 1.5f, bevel, L.DarkStone), f.At(0, y + 1.75f, 0));
        var s = K.StatueScale;
        var segments = detail switch { SiteDetail.Full => 48, SiteDetail.Plain => 28, _ => 16 };
        var small = Math.Max(8, segments / 3);
        var plate = 0.06f * s;
        var foot = y + K.PedestalTop;
        var body = foot + plate;
        Vector2 P(float r, float h) => new(r * s, h * s);
        // Model point (x left, y up from the plate top, z forward) to a world placement origin.
        Matrix4x4 At(float x, float h, float z) => f.At(x * s, body + h * s, z * s);
        b.Add(Solids.Lathe([P(0.43f, 0), P(0.43f, 0.04f), P(0.41f, 0.06f)], segments, L.Bronze, 1f, 0.76f), f.At(0, foot, 0));
        // Overcoat: hem flared, folds strongest at the hem and gone by the waist; ellipse section, square shoulders, collar base.
        float[] heights = [0.3f, 0.36f, 0.48f, 0.62f, 0.76f, 0.88f, 0.97f, 1.05f, 1.22f, 1.38f, 1.46f, 1.52f, 1.56f, 1.59f];
        float[] radii = [0.29f, 0.281f, 0.265f, 0.25f, 0.236f, 0.224f, 0.214f, 0.218f, 0.245f, 0.262f, 0.27f, 0.24f, 0.17f, 0.095f];
        var coat = heights.Select((h, i) => P(radii[i], h)).ToArray();
        float Fold(float a, float h)
        {
            var depth = 0.16f * Math.Clamp((0.95f * s - h) / (0.65f * s), 0f, 1f);
            return depth * (0.6f * MathF.Cos(6f * a) + 0.4f * MathF.Cos(10f * a + 1.3f));
        }
        b.Add(LandmarkSolids.Draped(coat, segments, L.Bronze, 1f, 0.66f, detail == SiteDetail.Mass ? (_, _) => 0f : Fold), At(0, 0, 0));
        // Head on its neck: ovoid wider front to back than side to side.
        b.Add(Shapes.Shaft(0.056f * s, 0.05f * s, 0.12f * s, small, L.Bronze), At(0, 1.55f, 0.012f));
        b.Add(Solids.Lathe([P(0, 0), P(0.045f, 0.01f), P(0.075f, 0.05f), P(0.088f, 0.1f), P(0.092f, 0.15f), P(0.088f, 0.19f), P(0.07f, 0.225f),
            P(0.04f, 0.245f), P(0, 0.252f)], small * 2, L.Bronze, 0.86f, 1f), At(0, 1.64f, 0.015f));
        if (detail == SiteDetail.Mass) return;
        // Stand collar.
        b.Add(Solids.Lathe([P(0.1f, 0), P(0.122f, 0.04f), P(0.118f, 0.085f), P(0.098f, 0.095f)], small * 2, L.Bronze, 1f, 0.85f), At(0, 1.545f, 0.005f));
        // Trouser legs and shoes under the hem.
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(Shapes.Shaft(0.064f * s, 0.07f * s, 0.31f * s, small, L.Bronze), At(side * 0.105f, 0.05f, 0.01f));
            b.Add(Solids.Lathe([P(0.05f, 0), P(0.056f, 0.025f), P(0.046f, 0.065f), P(0, 0.075f)], small, L.Bronze, 1f, 2.1f), At(side * 0.11f, 0, 0.055f));
        }
        // Arms in coat sleeves: shoulder, elbow, wrist; the hand continues the forearm.
        Vector3 shoulderL = new(0.215f, 1.43f, 0f), elbowL = new(0.265f, 1.1f, 0.03f), wristL = new(0.275f, 0.83f, 0.075f);
        Vector3 shoulderR = new(-0.215f, 1.43f, 0f), elbowR = new(-0.27f, 1.12f, 0.05f), wristR = new(-0.1f, 1.04f, 0.2f);
        foreach (var (sh, el, wr) in new[] { (shoulderL, elbowL, wristL), (shoulderR, elbowR, wristR) })
        {
            Limb(b, f, body, s, sh, el, 0.085f, 0.07f, small);
            Limb(b, f, body, s, el, wr, 0.07f, 0.068f, small);
            b.Add(Solids.Lathe([P(0, 0), P(0.047f, 0.019f), P(0.066f, 0.066f), P(0.047f, 0.113f), P(0, 0.132f)], small, L.Bronze), At(el.X, el.Y - 0.066f, el.Z));
            var along = Unit(wr - el);
            Hand(b, f, body, s, wr - along * 0.03f, along, small);
        }
        // Rolled document held across the right hand, sticking out either side.
        var grip = wristR + Unit(wristR - elbowR) * 0.075f;
        Limb(b, f, body, s, grip - Vector3.UnitX * 0.15f, grip + Vector3.UnitX * 0.15f, 0.029f, 0.029f, small);
        if (detail != SiteDetail.Full) return;
        // Hair mass over the crown and back; the face stays one smooth volume (an outlined nose reads as an eye).
        b.Add(Solids.Lathe([P(0.097f, 0), P(0.099f, 0.035f), P(0.087f, 0.085f), P(0.052f, 0.118f), P(0, 0.128f)], small * 2, L.Bronze, 0.9f, 1f), At(0, 1.765f, -0.006f));
    }

    /// <summary>A round limb or rod between two model points (x left, y up from the plate top, z forward), scaled by s.</summary>
    private static void Limb(SiteBuilder b, PlanFrame f, float body, float s, Vector3 from, Vector3 to, float r0, float r1, int segments)
    {
        var axis = to - from;
        var length = MathF.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z) * s;
        var y = Unit(axis);
        b.Add(Shapes.Shaft(r0 * s, r1 * s, length, segments, L.Bronze), Orient(f, body, s, from, y));
    }

    /// <summary>A hand as a flattened ovoid along the given model direction, its wrist starting inside the sleeve end.</summary>
    private static void Hand(SiteBuilder b, PlanFrame f, float body, float s, Vector3 wrist, Vector3 along, int segments) =>
        b.Add(Solids.Lathe([new(0.036f * s, 0), new(0.038f * s, 0.05f * s), new(0.046f * s, 0.09f * s), new(0.04f * s, 0.15f * s), new(0.022f * s, 0.19f * s), new(0, 0.2f * s)],
            segments, L.Bronze, 0.62f, 1f), Orient(f, body, s, wrist, along));

    /// <summary>Rigid placement whose local Y runs along a model direction from a model point (statue frame, scaled by s).</summary>
    private static Matrix4x4 Orient(PlanFrame f, float body, float s, Vector3 at, Vector3 direction)
    {
        Vector3 World(Vector3 v) => new(f.U.X * v.X + f.W.X * v.Z, v.Y, f.U.Y * v.X + f.W.Y * v.Z);
        var y = Unit(World(direction));
        var reference = MathF.Abs(y.Y) < 0.95f ? Vector3.UnitY : World(Vector3.UnitZ);
        var x = Unit(Cross(y, reference));
        var z = Cross(x, y);
        var p = f.P(at.X * s, at.Z * s);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, p.X, body + at.Y * s, p.Y, 1);
    }

    private static Vector3 Cross(Vector3 a, Vector3 c) => new(a.Y * c.Z - a.Z * c.Y, a.Z * c.X - a.X * c.Z, a.X * c.Y - a.Y * c.X);

    private static Vector3 Unit(Vector3 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return new(v.X / length, v.Y / length, v.Z / length);
    }

    // ---- Hexagonal double-eaved pavilion on the south-bank hill (§4.5, MAP-QSH-TERRAIN) ----

    /// <summary>Corner angle offset: one face of the hexagon looks north, back across the lake to the hard bank.</summary>
    private const float Phase = MathF.PI / 3f;

    private static Vector2 Corner(PlanFrame f, int i, float r)
    {
        var a = Phase + MathF.PI / 3f * i;
        return f.P(r * MathF.Cos(a), r * MathF.Sin(a));
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
        b.Add(LandmarkSolids.PolyRoof(6, K.LowerEave, K.DrumRadius - 0.05f, K.LowerRise, 0.35f, 0.16f, perSide, Phase, L.Tiles, L.Wood), f.At(0, lower, 0));
        // Drum between the eaves, then the upper roof, its soffit meeting the drum, and the finial.
        var drumTop = lower + K.LowerRise + 0.6f;
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(Hexagon(f, K.DrumRadius)), drumTop - (lower + K.LowerRise - 0.2f), L.Wood, lower + K.LowerRise - 0.2f), Matrix4x4.Identity);
        var upper = drumTop + 0.14f - K.UpperRise * (K.UpperEave - K.DrumRadius) / K.UpperEave;
        b.Add(LandmarkSolids.PolyRoof(6, K.UpperEave, 0, K.UpperRise, 0.3f, 0.14f, perSide, Phase, L.Tiles, L.Wood), f.At(0, upper, 0));
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
        var shore = ShoreOf(c.Context.World);
        var water = Water(c.Context.World);
        var y = c.Ground(shore.Bank[shore.WadeSegment].X, shore.Bank[shore.WadeSegment].Y);
        var segments = BankSegments(shore, water).ToArray();
        var rail = RailLine(shore, segments);
        foreach (var (f, l, i) in segments)
        {
            var line = PlanFrame.Toward(rail[i], rail[i + 1]);
            foreach (var (u0, u1) in GridFacade.Open(-0.17f, Vector2.Distance(rail[i], rail[i + 1]) + 0.17f, RailOpenings(shore, f, l, i, rail[i], rail[i + 1])))
                c.Wall(line.P(u0, 0), line.P(u1, 0), 0.17f, y - 1f, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.StonePaving);
        }
        // Wade treads (auto-step 0.30 and 0.35 m) and the mooring posts.
        var w = shore.WadeFrame;
        var (o0, o1) = shore.Openings(shore.WadeSegment, shore.WadeLength)[0];
        var half = (o1 - o0) * 0.5f;
        var mid = (o0 + o1) * 0.5f;
        c.Box(w.At(mid, y + shore.Tread1 - 0.15f, K.Tread1Depth * 0.5f), new(half, 0.15f, K.Tread1Depth * 0.5f), SurfaceTag.Stair, FootMat.StonePaving);
        c.Box(w.At(mid, y + shore.Tread2 - 0.15f, K.Tread1Depth + K.Tread2Depth * 0.5f), new(half, 0.15f, K.Tread2Depth * 0.5f), SurfaceTag.Stair, FootMat.StonePaving);
        foreach (var u in new[] { o0 - 0.6f, o1 + 0.6f })
            c.Add(new PostCollider(w.P(u, -0.6f), 0.2f, y, y + 0.65f, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        // Statue pedestal.
        var sy = c.Ground(shore.StatueAt.X, shore.StatueAt.Y);
        c.Box(StatueFrame(shore).At(0, sy + 1.0f, 0), new(1.0f, 1.0f, 1.0f), SurfaceTag.Prop, FootMat.PolishedStone);
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
