using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.South.Framework;
using Campus.Gen.Sites;
using T = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Stand;
using L = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.South.Sports;

/// <summary>
/// The stadium's stand (看台, MAP-QSH-SOUTH), which WP-14 would extrude as a 15 m C-shaped block, so this site claims it:
/// - tiers round the west half of the track, sampled on rays from the track's centre: a front wall over the apron, a cross
///   walk, rows of seats (muted yellow in the middle, pale lilac grey to the sides), the back concourse and its parapet;
/// - the silver-white pillow canopy (枕形罩棚) over the main stand, thick in the middle and rounded off at both ends, with a
///   pale gold middle section divided by vertical seams, carried by dark grey steel columns and struts on the concourse;
/// - closed concourse portals in the back wall where the sports platform's deck meets it;
/// - the low dark seats across the track (design:south-stadium-east-seats in world/south.json).
/// The track, the pitch and their markings are WP-15's ground. The stand has no glass: its window declaration is empty.
/// </summary>
public sealed class GrandstandSite : ZoneSite
{
    public static readonly string Feature = SouthIds.Grandstand.Entity;
    private static readonly string Part = SouthIds.Grandstand.Part;
    public static readonly string Track = SouthIds.Track.Entity;
    public const string LowSeats = "design:south-stadium-east-seats";

    public override string Id => "18c.stand";

    private static Campus.Contracts.Maps.MapRect StationRange(IEnumerable<Station> stations) => SiteRanges.Bounds(
        stations.SelectMany(s => new[] { s.Front, s.Back }), T.CanopyBack + T.CanopyThickness * .5f + T.Column);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var p = LayoutOf(world);
        return [StationRange(p.Stations), .. (p.Low.Count > 0 ? new[] { StationRange(p.Low) } : []),
            SiteRanges.Point(p.Centre), SiteRanges.Point(PlanGeometry.Centroid(
                world.Feature(PlatformSite.Feature).Contour(SouthIds.Platform.Part), meanIfDegenerate: true))];
    }

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    /// <summary>One ray through the tiers: its angle (degrees), the front point on the track side, the back point, the outward unit.</summary>
    internal readonly record struct Station(float Angle, Vector2 Front, Vector2 Back, Vector2 Out)
    {
        public float Depth => Vector2.Distance(Front, Back);
        public Vector3 At(float u, float y) => new(Front.X + Out.X * u, y, Front.Y + Out.Y * u);
    }

    internal sealed record Plan(float Y, Vector2 Centre, IReadOnlyList<Station> Stations, IReadOnlyList<Station> Low);

    internal static Plan PlanOf(IWorldModel world)
    {
        var p = LayoutOf(world);
        return p with { Y = world.Terrain.HeightAt(p.Centre.X, p.Centre.Y) };
    }

    private static Plan LayoutOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var feature))
            throw new InvalidOperationException($"Stand {Feature} is missing from the world model.");
        var ring = PlanGeometry.DeduplicateRing(feature.Contour(Part));
        var track = world.Features.TryGetValue(new(Track), out var t) ? t.Contour() : null;
        var centre = PlanGeometry.Centroid(track ?? ring, meanIfDegenerate: true);
        var found = new List<Station>();
        var count = (int)MathF.Round(360f / T.StationStep);
        for (var i = 0; i < count; i++)
        {
            var angle = i * T.StationStep;
            var a = angle * (MathF.PI / 180f);
            var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
            float near = float.MaxValue, far = float.MinValue;
            for (var k = 0; k < ring.Length; k++)
                if (RayStation(centre, d, ring[k], ring[(k + 1) % ring.Length]) is { } hit) { near = MathF.Min(near, hit); far = MathF.Max(far, hit); }
            if (far - near >= T.MinDepth) found.Add(new(angle, centre + d * near, centre + d * far, d));
        }
        // The C is one run of rays: start it after the widest gap in angle, so it never wraps.
        var start = 0;
        var widest = 0f;
        for (var i = 0; i < found.Count; i++)
        {
            var gap = (found[i].Angle - found[(i + found.Count - 1) % found.Count].Angle + 360f) % 360f;
            if (gap > widest) { widest = gap; start = i; }
        }
        var stations = found.Skip(start).Concat(found.Take(start)).ToArray();

        var low = new List<Station>();
        if (world.Features.TryGetValue(new(LowSeats), out var seats) && seats.Geometry is PolylineGeometry line && line.Line.Points.Count >= 2)
        {
            Vector2 p0 = line.Line.Points[0], p1 = line.Line.Points[^1];
            var along = Vector2.Normalize(p1 - p0);
            var out_ = new Vector2(along.Y, -along.X);
            if (Vector2.Dot(out_, (p0 + p1) * 0.5f - centre) < 0) out_ = -out_;
            var depth = T.LowWalk + T.LowRows * T.LowRow + T.LowBack;
            low.Add(new(0, p0, p0 + out_ * depth, out_));
            low.Add(new(0, p1, p1 + out_ * depth, out_));
        }
        return new(0, centre, stations, low);
    }

    /// <summary>Surfaces of a section's segments: walls and walks, seat treads, the risers between rows.</summary>
    internal const byte Wall = 0, Seat = 1, Riser = 2;

    /// <summary>
    /// Section of the tiers at one depth, (u from the front, y above the ground), front wall first and back wall last, with
    /// each segment's surface (<see cref="Wall"/>, <see cref="Seat"/>, <see cref="Riser"/>). No rows gives one sloped rake.
    /// </summary>
    internal static (Vector2[] Points, byte[] Face, float Top) Profile(float depth, int rows, float front, float walk, float concourse,
        float parapet, float slope, float topMax, int fullRows)
    {
        var pts = new List<Vector2> { new(0, -0.1f), new(0, front), new(walk, front) };
        var face = new List<byte> { Wall, Wall };
        var t = (depth - concourse - walk) / fullRows;
        var rise = MathF.Min(t * slope, (topMax - front) / fullRows);
        var top = front + fullRows * rise;
        if (rows == 0)
        {
            pts.Add(new(depth - concourse, top));
            face.Add(Seat);
        }
        else
        {
            var step = fullRows / rows;
            for (var k = 0; k < rows; k++)
            {
                var u = walk + k * step * t;
                pts.Add(new(u, front + (k + 1) * step * rise));
                face.Add(Riser);
                pts.Add(new(u + step * t, front + (k + 1) * step * rise));
                face.Add(Seat);
            }
        }
        foreach (var p in new Vector2[] { new(depth - 0.3f, top), new(depth - 0.3f, top + parapet), new(depth, top + parapet), new(depth, -0.1f) })
        {
            pts.Add(p);
            face.Add(Wall);
        }
        return ([.. pts], [.. face], top);
    }

    private static (Vector2[] Points, byte[] Face, float Top) Main(Station s, int rows) =>
        Profile(s.Depth, rows, T.Front, T.FrontWalk, T.Concourse, T.Parapet, T.Slope, T.TopMax, T.Rows);

    private static (Vector2[] Points, byte[] Face, float Top) LowProfile(int rows) =>
        Profile(T.LowWalk + T.LowRows * T.LowRow + T.LowBack, rows, T.LowFront, T.LowWalk, T.LowBack, 0.9f, T.LowRise / T.LowRow, 99f, T.LowRows);

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext ctx, LodLevel lod)
    {
        var p = PlanOf(ctx.World);
        var stride = lod switch { LodLevel.L0 => 1, LodLevel.L1 => 2, _ => 4 };
        var picked = p.Stations.Where((_, i) => i % stride == 0).ToList();
        if (picked[^1] != p.Stations[^1]) picked.Add(p.Stations[^1]);
        yield return SiteSourceTrace.Build(ctx, lod, Id + "/main", new(Feature), b => Build(b, true, false), c => Collide(c, true, false), Contours(picked));
        if (p.Low.Count == 2)
            yield return SiteSourceTrace.Build(ctx, lod, Id + "/east-seats", new(LowSeats), b => Build(b, false, true), c => Collide(c, false, true), Contours(p.Low));
    }

    private static IReadOnlyList<PolygonRings> Contours(IReadOnlyList<Station> stations) =>
        [.. Enumerable.Range(0, stations.Count - 1).Select(i => new PolygonRings(
            [stations[i].Front, stations[i + 1].Front, stations[i + 1].Back, stations[i].Back]))];

    // ---- Geometry ----

    public override void Build(SiteBuilder b) => Build(b, true, true);

    private static void Build(SiteBuilder b, bool main, bool low)
    {
        var p = LayoutOf(b.Context.World);
        main &= b.Includes(StationRange(p.Stations));
        low &= p.Low.Count > 0 && b.Includes(StationRange(p.Low));
        if (!main && !low) return;
        p = p with { Y = b.Ground(p.Centre) };
        var (stride, rows) = b.Detail switch { SiteDetail.Full => (1, T.Rows), SiteDetail.Plain => (2, T.Rows / 2), _ => (4, 0) };
        var picked = new List<Station>();
        for (var i = 0; i < p.Stations.Count; i += stride) picked.Add(p.Stations[i]);
        if (picked[^1] != p.Stations[^1]) picked.Add(p.Stations[^1]);
        if (main) Tiers(b, p, picked, s => Main(s, rows), s => s.Angle is >= T.YellowFrom and <= T.YellowTo ? L.SeatYellow : L.SeatLilac);
        if (low && p.Low.Count == 2) Tiers(b, p, p.Low, _ => LowProfile(b.Detail == SiteDetail.Mass ? 0 : T.LowRows), _ => L.SeatDark);
        if (!main) return;
        Canopy(b, p, picked);
        if (b.Detail == SiteDetail.Mass) return;
        Copings(b, p, picked);
        Portals(b, p);
        Supports(b, p);
    }

    /// <summary>Lofts the section between consecutive stations, eight segments to a mesh so the cells share the load; caps both ends.</summary>
    private static void Tiers(SiteBuilder b, Plan p, IReadOnlyList<Station> stations, Func<Station, (Vector2[] Points, byte[] Face, float Top)> section,
        Func<Station, ToonSpec> seats)
    {
        for (var c0 = 0; c0 + 1 < stations.Count; c0 += 8)
        {
            if (!b.Includes(StationRange(stations.Skip(c0).Take(Math.Min(9, stations.Count - c0))))) continue;
            var writers = new Dictionary<ToonSpec, (Shapes.Writer Writer, int Count)>();
            void Quad(ToonSpec m, Vector3 a, Vector3 e, Vector3 f, Vector3 g, Vector3 n)
            {
                if (!writers.TryGetValue(m, out var w)) w = (new Shapes.Writer(m), 0);
                var ia = w.Writer.Vertex(a, n, new(a.X + a.Z, a.Y));
                var ie = w.Writer.Vertex(e, n, new(e.X + e.Z, e.Y));
                var iff = w.Writer.Vertex(f, n, new(f.X + f.Z, f.Y));
                var ig = w.Writer.Vertex(g, n, new(g.X + g.Z, g.Y));
                w.Writer.Quad(ia, ie, iff, ig, n);
                writers[m] = (w.Writer, w.Count + 1);
            }
            for (var i = c0; i < Math.Min(c0 + 8, stations.Count - 1); i++)
            {
                Station s0 = stations[i], s1 = stations[i + 1];
                var (q0, faces, _) = section(s0);
                var (q1, _, _) = section(s1);
                var mid = Vector2.Normalize(s0.Out + s1.Out);
                var seatSpec = seats(s0);
                for (var k = 0; k + 1 < q0.Length; k++)
                {
                    var du = q0[k + 1].X - q0[k].X;
                    var dy = q0[k + 1].Y - q0[k].Y;
                    var n = Vector3.Normalize(new(-dy * mid.X, du, -dy * mid.Y));
                    Quad(faces[k] switch { Seat => seatSpec, Riser => L.TierRiser, _ => L.StandConcrete }, s0.At(q0[k].X, p.Y + q0[k].Y), s0.At(q0[k + 1].X, p.Y + q0[k + 1].Y),
                        s1.At(q1[k + 1].X, p.Y + q1[k + 1].Y), s1.At(q1[k].X, p.Y + q1[k].Y), n);
                }
            }
            foreach (var (_, (writer, n)) in writers.OrderBy(w => w.Key.Lit.Value, StringComparer.Ordinal))
                if (n > 0) b.Add(writer.Finish(), Matrix4x4.Identity);
        }
        foreach (var (s, sign) in new[] { (stations[0], -1f), (stations[^1], 1f) })
        {
            if (!b.Includes(StationRange([s]))) continue;
            var face = Simplify(section(s).Points);
            // The cap's plane holds the ray; its local X runs out along the ray, Z across it.
            var across = new Vector2(-s.Out.Y, s.Out.X) * sign;
            var at = new Matrix4x4(s.Out.X, 0, s.Out.Y, 0, 0, 1, 0, 0, across.X, 0, across.Y, 0, s.Front.X, p.Y, s.Front.Y, 1);
            if (PlanGeometry.SignedArea(face) < 0) Array.Reverse(face);
            b.Add(PartGeometryKernel.WallPanel(new PolygonRings(face), 0.2f, L.StandConcrete), at);
        }
    }

    /// <summary>A stone coping on the back parapet and a granite plinth along the back wall, one piece per station.</summary>
    private static void Copings(SiteBuilder b, Plan p, IReadOnlyList<Station> stations)
    {
        for (var i = 0; i + 1 < stations.Count; i++)
        {
            Station s0 = stations[i], s1 = stations[i + 1];
            if (!b.Includes(StationRange([s0, s1]))) continue;
            var top0 = Main(s0, 0).Top;
            Vector2 a = s0.Back - s0.Out * 0.15f, c = s1.Back - s1.Out * 0.15f;
            var f = PlanFrame.Toward(a, c);
            var length = Vector2.Distance(a, c) + 0.05f;
            b.Add(PartGeometryKernel.BeveledBox(length, 0.14f, 0.5f, 0.02f, L.StoneTrim), f.At(length * 0.5f, p.Y + top0 + T.Parapet, 0));
            b.Add(PartGeometryKernel.BeveledBox(length, 0.6f, 0.42f, 0.02f, L.Granite), f.At(length * 0.5f, p.Y - 0.1f, 0.1f * MathF.Sign(Vector2.Dot(f.W, s0.Out))));
        }
    }

    /// <summary>
    /// The pillow canopy: a lens section from just behind the back wall out over the rows, lofted round the main stand. It thins
    /// and pulls back toward both ends, so it reads as one cushion; the middle section is pale gold with dark vertical seams.
    /// </summary>
    private static void Canopy(SiteBuilder b, Plan p, IReadOnlyList<Station> picked)
    {
        var stations = picked.Where(s => s.Angle >= T.CanopyFrom && s.Angle <= T.CanopyTo).ToArray();
        if (stations.Length < 2) return;
        if (!b.Includes(StationRange(stations))) return;
        var n = b.Detail == SiteDetail.Full ? T.CanopySamples : T.CanopySamples / 2;
        var rings = stations.Select(s => CanopyRing(p, s, n)).ToArray();
        var count = rings[0].Length;
        var normals = new Vector3[stations.Length][];
        for (var i = 0; i < stations.Length; i++)
        {
            normals[i] = new Vector3[count];
            var centre = rings[i].Aggregate(Vector3.Zero, (acc, v) => acc + v) / count;
            for (var k = 0; k < count; k++)
            {
                var along = rings[Math.Min(i + 1, stations.Length - 1)][k] - rings[Math.Max(i - 1, 0)][k];
                var round = rings[i][(k + 1) % count] - rings[i][(k + count - 1) % count];
                var normal = Vector3.Cross(along, round);
                normal = normal.LengthSquared() > 1e-12f ? Vector3.Normalize(normal) : Vector3.UnitY;
                if (Vector3.Dot(normal, rings[i][k] - centre) < 0) normal = -normal;
                normals[i][k] = normal;
            }
        }
        for (var c0 = 0; c0 + 1 < stations.Length; c0 += 8)
        {
            if (!b.Includes(StationRange(stations.Skip(c0).Take(Math.Min(9, stations.Length - c0))))) continue;
            Shapes.Writer silver = new(L.Silver), gold = new(L.CanopyGold);
            int silverCount = 0, goldCount = 0;
            for (var i = c0; i < Math.Min(c0 + 8, stations.Length - 1); i++)
            {
                var midAngle = (stations[i].Angle + stations[i + 1].Angle) * 0.5f;
                var isGold = midAngle >= T.GoldFrom && midAngle <= T.GoldTo;
                var w = isGold ? gold : silver;
                if (isGold) goldCount++; else silverCount++;
                var r0 = new int[count];
                var r1 = new int[count];
                for (var k = 0; k < count; k++)
                {
                    r0[k] = w.Vertex(rings[i][k], normals[i][k], new(stations[i].Angle, k));
                    r1[k] = w.Vertex(rings[i + 1][k], normals[i + 1][k], new(stations[i + 1].Angle, k));
                }
                for (var k = 0; k < count; k++)
                    w.Quad(r0[k], r0[(k + 1) % count], r1[(k + 1) % count], r1[k], normals[i][k] + normals[i + 1][(k + 1) % count]);
            }
            if (silverCount > 0) b.Add(silver.Finish(), Matrix4x4.Identity);
            if (goldCount > 0) b.Add(gold.Finish(), Matrix4x4.Identity);
        }
        // Caps over the thin ends.
        foreach (var (i, sign) in new[] { (0, -1f), (stations.Length - 1, 1f) })
        {
            if (!b.Includes(StationRange([stations[i]]))) continue;
            var cap = new Shapes.Writer(L.Silver);
            var normal = new Vector3(-stations[i].Out.Y, 0, stations[i].Out.X) * sign;
            var centre = rings[i].Aggregate(Vector3.Zero, (acc, v) => acc + v) / count;
            var c = cap.Vertex(centre, normal, Vector2.Zero);
            var ids = rings[i].Select(v => cap.Vertex(v, normal, new(v.X, v.Y))).ToArray();
            for (var k = 0; k < count; k++) cap.Triangle(c, ids[k], ids[(k + 1) % count], normal);
            b.Add(cap.Finish(), Matrix4x4.Identity);
        }
        if (b.Detail != SiteDetail.Full) return;
        // Vertical division of the gold section: a slim dark seam over the top at every other station.
        for (var i = 0; i < stations.Length; i += 2)
        {
            if (!b.Includes(StationRange([stations[i]]))) continue;
            if (stations[i].Angle < T.GoldFrom || stations[i].Angle > T.GoldTo) continue;
            for (var k = 0; k < n; k++)
            {
                var a = rings[i][k] + normals[i][k] * 0.03f;
                var c = rings[i][k + 1] + normals[i][k + 1] * 0.03f;
                b.Add(PartGeometryKernel.BeveledBox(0.12f, Vector3.Distance(a, c) + 0.04f, 0.05f, 0, L.DarkFrame), LandmarkSolids.Along(a, c, normals[i][k] + normals[i][k + 1]));
            }
        }
    }

    /// <summary>
    /// The canopy's section at a station, world points: the upper surface from the back edge to the front edge, then the flatter
    /// belly back again. Thickness and reach shrink toward the canopy's ends (a sine ease over <see cref="T.CanopyEnd"/> degrees).
    /// </summary>
    private static Vector3[] CanopyRing(Plan p, Station s, int samples)
    {
        var top = Main(s, 0).Top;
        var ease = Math.Clamp(MathF.Min(s.Angle - T.CanopyFrom, T.CanopyTo - s.Angle) / T.CanopyEnd, 0f, 1f);
        var g = MathF.Sqrt(MathF.Sin(ease * MathF.PI * 0.5f));
        var thickness = T.CanopyThickness * (0.15f + 0.85f * g);
        var reach = T.CanopyReach * (0.35f + 0.65f * g) * (s.Depth - T.Concourse);
        Vector2 back = new(s.Depth + T.CanopyBack, top + T.CanopyBackRise), front = new(s.Depth - reach, top + T.CanopyFrontRise);
        var dir = Vector2.Normalize(front - back);
        var up = new Vector2(-dir.Y, dir.X);
        if (up.Y < 0) up = -up;
        var ring = new List<Vector3>();
        for (var j = 0; j <= samples; j++)
        {
            var v = j / (float)samples;
            var q = Vector2.Lerp(back, front, v) + up * Half(v, thickness);
            ring.Add(s.At(q.X, p.Y + q.Y));
        }
        for (var j = samples - 1; j >= 1; j--)
        {
            var v = j / (float)samples;
            var q = Vector2.Lerp(back, front, v) - up * (Half(v, thickness) * T.CanopyBelly);
            ring.Add(s.At(q.X, p.Y + q.Y));
        }
        return [.. ring];
    }

    private static float Half(float v, float thickness) => thickness * 0.5f * MathF.Pow(MathF.Max(0f, MathF.Sin(v * MathF.PI)), T.CanopyRound);

    /// <summary>Dark grey steel on the concourse: a column up to the canopy's belly and a raking strut out under it.</summary>
    private static void Supports(SiteBuilder b, Plan p)
    {
        var stations = p.Stations.Where(s => s.Angle >= T.CanopyFrom + T.CanopyEnd * 0.5f && s.Angle <= T.CanopyTo - T.CanopyEnd * 0.5f).ToArray();
        for (var i = 0; i < stations.Length; i += T.SupportEvery)
        {
            var s = stations[i];
            if (!b.Includes(StationRange([s]))) continue;
            var ring = CanopyRing(p, s, T.CanopySamples);
            var top = Main(s, 0).Top;
            var foot = s.At(s.Depth - T.Concourse * 0.5f, p.Y + top);
            // Belly points run from the front edge back toward the back edge.
            var belly = ring.Skip(T.CanopySamples + 1).ToArray();
            var head = belly.OrderBy(v => Vector2.Distance(new(v.X, v.Z), new(foot.X, foot.Z))).First();
            var height = head.Y - foot.Y;
            if (height <= 0.5f) continue;
            b.Add(PartGeometryKernel.BeveledBox(T.Column, height, T.Column, 0.03f, L.DarkFrame), Matrix4x4.CreateTranslation(foot.X, foot.Y, foot.Z));
            var across = new Vector3(-s.Out.Y, 0, s.Out.X);
            var reachPoint = belly[belly.Length / 3];
            var from = foot + new Vector3(0, height * 0.55f, 0);
            b.Add(PartGeometryKernel.BeveledBox(0.3f, Vector3.Distance(from, reachPoint), 0.3f, 0.02f, L.DarkFrame), LandmarkSolids.Along(from, reachPoint, across));
        }
    }

    /// <summary>Closed concourse portals in the back wall where the platform deck meets it, at the deck's level.</summary>
    private static void Portals(SiteBuilder b, Plan p)
    {
        var deck = PlatformSite.PlanOf(b.Context.World);
        var last = float.MinValue;
        var arc = 0f;
        Station? previous = null;
        foreach (var s in p.Stations)
        {
            if (previous is { } prev) arc += Vector2.Distance(prev.Back, s.Back);
            previous = s;
            if (arc - last < T.PortalPitch) continue;
            if (!deck.Decks.Any(r => PlanGeometry.InsideByCrossings(r, s.Back + s.Out * 1.0f))) continue;
            last = arc;
            if (!b.Includes(StationRange([s]))) continue;
            var f = new PlanFrame(s.Back, new(s.Out.Y, -s.Out.X));
            float floor = deck.Top, hw = T.PortalWidth * 0.5f;
            GridFacade.Box(b, f, -hw - 0.3f, hw + 0.3f, floor, floor + T.PortalHeight + 0.3f, -0.02f, 0.12f, L.White, 0.02f);
            GridFacade.Box(b, f, -hw, hw, floor, floor + T.PortalHeight, 0.1f, 0.16f, L.DarkFrame, 0);
        }
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c) => Collide(c, true, true);

    private static void Collide(SiteColliders c, bool main, bool includeLow)
    {
        var p = LayoutOf(c.Context.World);
        main &= c.Includes(StationRange(p.Stations));
        includeLow &= p.Low.Count > 0 && c.Includes(StationRange(p.Low));
        if (!main && !includeLow) return;
        p = p with { Y = c.Ground(p.Centre) };
        foreach (var stations in new[] { p.Stations, p.Low }.Where(s => ReferenceEquals(s, p.Low) ? includeLow : main))
        {
            if (stations.Count < 2) continue;
            var low = ReferenceEquals(stations, p.Low);
            for (var i = 0; i + 1 < stations.Count; i++)
            {
                Station s0 = stations[i], s1 = stations[i + 1];
                if (!c.Includes(StationRange([s0, s1]))) continue;
                var top0 = low ? LowProfile(0).Top : Main(s0, 0).Top;
                var top1 = low ? LowProfile(0).Top : Main(s1, 0).Top;
                float front = low ? T.LowFront : T.Front, walk = low ? T.LowWalk : T.FrontWalk, concourse = low ? T.LowBack : T.Concourse;
                c.Wall(s0.Front, s1.Front, 0.2f, p.Y - 0.2f, p.Y + front + 0.4f, SurfaceTag.Wall, FootMat.Concrete);
                c.Wall(s0.Back, s1.Back, 0.2f, p.Y - 0.2f, p.Y + MathF.Max(top0, top1) + (low ? 0.9f : T.Parapet), SurfaceTag.Wall, FootMat.Concrete);
                Vector3 f0 = s0.At(walk, p.Y + front), f1 = s1.At(walk, p.Y + front);
                Vector3 r0 = s0.At(s0.Depth - concourse, p.Y + top0), r1 = s1.At(s1.Depth - concourse, p.Y + top1);
                c.Add(new RampCollider(f0, f1, r1, r0, 0.4f, SurfaceTag.Ground) { Foot = FootMat.Concrete });
                c.Add(new RampCollider(r0, r1, s1.At(s1.Depth, p.Y + top1), s0.At(s0.Depth, p.Y + top0), 0.4f, SurfaceTag.Ground) { Foot = FootMat.Concrete });
            }
            foreach (var s in new[] { stations[0], stations[^1] })
            {
                if (!c.Includes(StationRange([s]))) continue;
                var top = low ? LowProfile(0).Top : Main(s, 0).Top;
                c.Wall(s.Front, s.Back, 0.2f, p.Y - 0.2f, p.Y + top + 1.1f, SurfaceTag.Wall, FootMat.Concrete);
            }
        }
    }

    // ---- Helpers ----

    /// <summary>Distance along the ray (origin o, unit d) to segment a–b, or null when it misses.</summary>
    private static float? RayStation(Vector2 origin, Vector2 direction, Vector2 a, Vector2 b)
    {
        var edge = b - a;
        if (PlanLines.LineIntersection(origin, direction, a, edge, 1e-9f) is not { } hit) return null;
        var station = Vector2.Dot(hit - origin, direction);
        var alongEdge = Vector2.Dot(hit - a, edge);
        return station > 0 && alongEdge >= 0 && alongEdge <= edge.LengthSquared() ? station : null;
    }

    /// <summary>The section without collinear runs, for the end caps' triangulation.</summary>
    private static Vector2[] Simplify(IReadOnlyList<Vector2> points)
    {
        var list = new List<Vector2>();
        for (var i = 0; i < points.Count; i++)
        {
            Vector2 a = points[(i + points.Count - 1) % points.Count], v = points[i], e = points[(i + 1) % points.Count];
            var cross = (v.X - a.X) * (e.Y - v.Y) - (v.Y - a.Y) * (e.X - v.X);
            if (MathF.Abs(cross) > 1e-5f) list.Add(v);
        }
        return [.. list];
    }

}
