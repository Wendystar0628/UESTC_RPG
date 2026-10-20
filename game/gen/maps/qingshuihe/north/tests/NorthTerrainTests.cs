using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Sites;
using GdUnit4;

namespace Campus.Gen.Maps.Qingshuihe.North.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Maps.Qingshuihe.North.Tests.NorthTestSupport;

[TestSuite]
public sealed class NorthTerrainTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TerrainOverlayParsesAndReferencesRealRoads()
    {
        var file = Terrain();
        AssertThat(file.Zone.Value == "north").OverrideFailureMessage("north terrain zone").IsTrue();
        var world = RealMapFixture.Load().World;
        var roads = world.Network.Roads.ToDictionary(r => r.Id);
        foreach (var op in file.Ops)
            switch (op)
            {
                case TerrainPlatformOp p:
                    AssertThat(p.Y > 0).OverrideFailureMessage($"{op.Id}: terrace is raised above the base ground").IsTrue(); break;
                case TerrainPadOp pad:
                    // TerrainOverlayJson validates pad fields; its feature reference must resolve in the real map.
                    AssertThat(world.Features.ContainsKey(pad.Feature)).OverrideFailureMessage($"{op.Id}: pad feature {pad.Feature.Value} exists").IsTrue(); break;
                case TerrainMoundOp or TerrainBasinOp:
                    var (shape, height) = op is TerrainMoundOp m ? (m.Shape, m.Height) : (((TerrainBasinOp)op).Shape, ((TerrainBasinOp)op).Height);
                    var ellipse = (TerrainEllipse)shape;
                    var steepest = 1.5 * height / Math.Min(ellipse.Radii.X, ellipse.Radii.Y);
                    AssertThat(height <= 1.5 && steepest <= 1.0 / 3 + TestTolerance.Position).OverrideFailureMessage($"{op.Id}: R6 relief ≤ 1.5 m, no steeper than 1:3").IsTrue();
                    // The courtyard hill is an activity lawn (Opus 13:32, R6): short radius at least 7.5 × height (1:5).
                    if (op.Id == NorthFeatureIds.Hill)
                        AssertThat(Math.Min(ellipse.Radii.X, ellipse.Radii.Y) >= 7.5 * height - TestTolerance.Position).OverrideFailureMessage($"{op.Id}: activity hill no steeper than 1:5").IsTrue();
                    break;
                case TerrainSlopeOp s:
                    AssertThat(s.Geometry is PolylineGeometry && Math.Abs(s.Y1 - s.Y0) / PlanLines.Length(((PolylineGeometry)s.Geometry).Line.Points) <= 0.05 + TestTolerance.FloatRelative).OverrideFailureMessage($"{op.Id}: the 通学坡 stays at or below 5 %").IsTrue(); break;
                case TerrainCorridorOp c:
                    AssertThat(roads.TryGetValue(c.Road, out var road)).OverrideFailureMessage($"{op.Id}: road {c.Road.Value} exists").IsTrue();
                    var length = PlanLines.Length(road!.Line.Points);
                    AssertThat(Math.Abs(c.Anchors[^1].S - length) < TestTolerance.Layout).OverrideFailureMessage($"{op.Id}: last anchor at the full centreline length {length:0.00}").IsTrue();
                    break;
                default: throw new InvalidOperationException($"{op.Id}: unexpected operation for 20a-2");
            }
        AssertThat(file.Ops.OfType<TerrainPlatformOp>().Any()).OverrideFailureMessage("raised terraces are registered").IsTrue();
    }

    /// <summary>
    /// 20a-3: a road with no corridor keeps the automatic 0 m profile and would cut a trench across the terrace, so every
    /// line road that runs onto a terrace carries anchors; every shared centreline vertex has one elevation; the authored
    /// profile keeps to 5 %, and to 3 % within 10 m of a junction (WLD-TERRAIN).
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryRoadOnATerraceHasAnchorsAndJunctionsAgree()
    {
        var file = Terrain();
        var roads = RealMapFixture.Load().World.Network.Roads.Where(r => r.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("highway", out _)
            && !(p.TryGetProperty("area", out var area) && area.GetString() == "yes")).ToArray();
        var corridors = file.Ops.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var terraces = file.Ops.OfType<TerrainPlatformOp>().Select(p => p.Rings.Outer).ToArray();
        foreach (var road in roads)
            if (Samples(road.Line.Points).Any(p => terraces.Any(t => PlanRings.InsideByCrossings(t, p))))
                AssertThat(corridors.ContainsKey(road.Id)).OverrideFailureMessage($"{road.Id.Value} runs onto a terrace without corridor anchors").IsTrue();
        foreach (var c in corridors.Values)
            for (var i = 0; i + 1 < c.Anchors.Count; i++)
                AssertThat(Math.Abs(Grade(c.Anchors, i)) <= 0.05 + TestTolerance.Position).OverrideFailureMessage($"{c.Id}: piece at s {c.Anchors[i].S} is steeper than 5 %").IsTrue();

        var junctions = new Dictionary<(long, long), List<(NetworkLine Road, double S)>>();
        foreach (var road in roads)
        {
            var s = 0.0;
            for (var i = 0; i < road.Line.Points.Count; i++)
            {
                if (i > 0) s += Vector2.Distance(road.Line.Points[i - 1], road.Line.Points[i]);
                var p = road.Line.Points[i];
                var key = ((long)Math.Round(p.X * 1000d), (long)Math.Round(p.Y * 1000d));
                if (!junctions.TryGetValue(key, out var list)) junctions[key] = list = [];
                if (!list.Any(e => e.Road.Id == road.Id)) list.Add((road, s));
            }
        }
        var checkedJunctions = 0;
        foreach (var (key, list) in junctions.Where(j => j.Value.Count > 1))
        {
            checkedJunctions++;
            var first = Elevation(corridors, list[0].Road, list[0].S);
            foreach (var (road, s) in list)
            {
                AssertThat(Math.Abs(Elevation(corridors, road, s) - first) <= TestTolerance.Position).OverrideFailureMessage($"junction {key} of {road.Id.Value}: {Elevation(corridors, road, s):0.000} vs {first:0.000}").IsTrue();
                if (!corridors.TryGetValue(road.Id, out var c)) continue;
                for (var i = 0; i + 1 < c.Anchors.Count; i++)
                    if (c.Anchors[i + 1].S > s - 10 && c.Anchors[i].S < s + 10)
                        AssertThat(Math.Abs(Grade(c.Anchors, i)) <= 0.03 + TestTolerance.Position).OverrideFailureMessage($"{c.Id}: steeper than 3 % within 10 m of the junction at s {s:0.0}").IsTrue();
            }
        }
        AssertThat(checkedJunctions > 0).OverrideFailureMessage("the whole network's junctions were checked").IsTrue();
    }

    /// <summary>
    /// Main session 2026-10-16 15:0x: 未命名建筑 5561 must not straddle a terrace edge (it showed a 1.4 m step, over the
    /// 1.2 m allowed without a structure). Its whole footprint stands on one level and every terrace crest keeps 6 m off it.
    /// </summary>
    [TestCase, TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
    public void Building5561StandsOnOneLevelClearOfTheTerraceEdge()
    {
        var footprint = NorthBuildings.Rings(RealMapFixture.Load().World.Features[new(NorthFeatureIds.Building5561.Entity)]).Single();
        foreach (var terrace in Terrain().Ops.OfType<TerrainPlatformOp>())
        {
            var ring = terrace.Rings.Outer;
            var inside = footprint.Count(p => PlanRings.InsideByCrossings(ring, p));
            AssertThat(inside == 0 || inside == footprint.Count).OverrideFailureMessage($"5561 straddles {terrace.Id}").IsTrue();
            var clear = float.MaxValue;
            for (var i = 0; i < footprint.Count; i++)
                for (var j = 0; j < ring.Count; j++)
                {
                    Vector2 a0 = footprint[i], a1 = footprint[(i + 1) % footprint.Count], b0 = ring[j], b1 = ring[(j + 1) % ring.Count];
                    clear = PlanLines.SegmentIntersection(a0, a1, b0, b1, 1e-9f) is null ? MathF.Min(clear, MathF.Min(MathF.Sqrt(PlanLines.SegmentDistanceSquared(a0, b0, b1)), MathF.Sqrt(PlanLines.SegmentDistanceSquared(b0, a0, a1)))) : 0f;
                }
            AssertThat(clear >= 6f).OverrideFailureMessage($"{terrace.Id}: crest {clear:0.00} m from 5561, at least 6 m").IsTrue();
        }
    }

    /// <summary>
    /// max-108 0h-b (Opus 14:20; geometry 14:26/28): the 丹桂路 design walks. Each authored walk is a 2.5 m
    /// footway 7.5 m off the road centre (3 m outside the 4.5 m osmanthus row); its surface keeps 3 m off every building and
    /// water edge; it lies on the 0 m ground 丹桂路 is pinned to, clear of every terrace's side slope and every raised
    /// corridor (a design road takes no corridor anchors); and each end lies on the surface it names in <c>joins</c>: an
    /// existing walk or pedestrian area, or the kerb of a carriageway that meets 丹桂路 (a junction).
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void DanguiWalksKeepOffWallsAndWaterAndJoinTheNetwork()
    {
        var road = NorthFeatureIds.DanguiRoad.Source;
        const float offset = 7.5f, half = 1.25f, wallClear = 3f;
        var world = RealMapFixture.Load().World;
        var walks = world.Features.Values.Where(feature => feature.Id.Value.StartsWith(NorthFeatureIds.DanguiWalkPrefix, StringComparison.Ordinal)).ToArray();
        AssertThat(walks.Length > 0 && walks.All(feature => feature.Zone == new ZoneId("north"))).OverrideFailureMessage("丹桂路 walks belong to north").IsTrue();
        var dangui = world.Network.Roads.Single(r => r.Id.Value == road).Line.Points;
        var walls = world.Features.Values.SelectMany(f => f.Parts.Count > 0
                ? f.Parts.Where(p => ((WorldProps.TryText(p.Props, "kind") ?? f.Kind.Value) is "building" or "water")
                    && (p.Geometry is PolygonGeometry or MultiPolygonGeometry))
                    .SelectMany(p => (f with { Geometry = p.Geometry, Parts = [] }).Polygons())
                : (f.Kind.Value is "building" or "water") && (f.Geometry is PolygonGeometry or MultiPolygonGeometry) ? f.Polygons() : [])
            .Select(r => r.Outer).ToArray();
        var terrain = Terrain().Ops;
        var corridors = terrain.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var roads = world.Network.Roads.ToDictionary(r => r.Id);
        foreach (var add in walks)
        {
            var props = add.Props;
            AssertThat(add.Kind.Value == "road" && WorldProps.TryText(props, "highway") == "footway" && props!.Value.GetProperty("width").GetSingle() == 2.5f).OverrideFailureMessage($"{add.Id.Value}: a 2.5 m footway WP-15 paves").IsTrue();
            var line = ((PolylineGeometry)add.Geometry).Line.Points;
            AssertThat(roads.TryGetValue(add.Id, out var loaded) && loaded.Line.Points.SequenceEqual(line) && loaded.Width == WorldProps.TryNumber(props, "width")).OverrideFailureMessage($"{add.Id.Value}: network follows its record").IsTrue();
            foreach (var p in line)
            {
                var d = MathF.Sqrt(PlanLines.PolylineDistanceSquared(dangui, p));
                AssertThat(MathF.Abs(d - offset) <= 4 * TestTolerance.Layout).OverrideFailureMessage($"{add.Id.Value}: {p} is {d:0.00} m off the road centre, not {offset}").IsTrue();
            }
            foreach (var p in Samples(line))
            {
                var wall = walls.Min(r => PlanRings.InsideByCrossings(r, p) ? 0f : PlanRings.DistanceToRing(p, r)) - half;
                AssertThat(wall >= wallClear).OverrideFailureMessage($"{add.Id.Value}: surface at {p} is {wall:0.00} m from a wall or water edge").IsTrue();
                foreach (var water in world.Network.Waterways)
                    AssertThat(MathF.Sqrt(PlanLines.PolylineDistanceSquared(water.Line.Points, p)) - water.Width * 0.5f - half >= wallClear).OverrideFailureMessage($"{add.Id.Value}: {p} keeps off {water.Id.Value}").IsTrue();
                foreach (var op in terrain)
                    switch (op)
                    {
                        // The side slope reaches y·n beyond the crest, plus half the fillet (WLD-TERRAIN).
                        case TerrainPlatformOp t:
                            var n = double.Parse(t.Side.Fill.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);
                            var foot = Math.Abs(t.Y) * n + t.Fillet * 0.5;
                            AssertThat(!PlanRings.InsideByCrossings(t.Rings.Outer, p) && PlanRings.DistanceToRing(p, t.Rings.Outer) - half >= foot).OverrideFailureMessage($"{add.Id.Value}: {p} is on {t.Id} or its side slope").IsTrue();
                            break;
                        case TerrainMoundOp or TerrainBasinOp:
                            var e = (TerrainEllipse)(op is TerrainMoundOp m ? m.Shape : ((TerrainBasinOp)op).Shape);
                            double yaw = e.YawDeg * Math.PI / 180, dx = p.X - e.Center.X, dz = p.Y - e.Center.Y;
                            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
                            AssertThat(Math.Pow(along / (e.Radii.X + half), 2) + Math.Pow(across / (e.Radii.Y + half), 2) >= 1).OverrideFailureMessage($"{add.Id.Value}: {p} is on {op.Id}").IsTrue();
                            break;
                    }
                // Where a road crosses or runs beside the walk, its authored profile is 0 m there.
                foreach (var (id, c) in corridors)
                {
                    var hit = PlanLines.Project(roads[id].Line.Points, p);
                    if (hit.Distance <= roads[id].Width * 0.5f + c.Shoulder + half)
                        AssertThat(Math.Abs(Elevation(corridors, roads[id], hit.Station)) < TestTolerance.Position).OverrideFailureMessage($"{add.Id.Value}: {p} meets {c.Id} at {Elevation(corridors, roads[id], hit.Station):0.00} m").IsTrue();
                }
            }
            foreach (var end in new[] { line[0], line[^1] })
            {
                var joinsRoad = roads.Values.Where(target => target.Id != add.Id && WorldProps.TryText(target.Props, "area") != "yes").Any(target =>
                {
                    var type = WorldProps.TryText(target.Props, "highway");
                    var walk = type is "footway" or "path" or "pedestrian" or "steps";
                    var reach = walk ? MathF.Max(target.Width, 2f) * 0.5f : target.Width * 0.5f;
                    return (walk || Meets(target.Line.Points, dangui)) &&
                        MathF.Sqrt(PlanLines.PolylineDistanceSquared(target.Line.Points, end)) <= reach + TestTolerance.Layout;
                });
                var joinsArea = world.Features.Values.Where(target => target.Id != add.Id && target.Kind.Value == "road" &&
                    WorldProps.TryText(target.Props, "highway") == "pedestrian").SelectMany(target => WorldFeatureGeometry.PolygonsOf(target.Geometry))
                    .Any(rings => PlanRings.Contains(rings, end) || PlanRings.DistanceToRing(end, rings.Outer) <= TestTolerance.Layout);
                AssertThat(joinsRoad || joinsArea).OverrideFailureMessage($"{add.Id.Value}: endpoint {end} joins existing paving").IsTrue();
            }

        }
    }

    private static bool Meets(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b) =>
        a.Any(p => b.Contains(p)) || Enumerable.Range(0, a.Count - 1).Any(i => Enumerable.Range(0, b.Count - 1).Any(k => PlanLines.SegmentIntersection(a[i], a[i + 1], b[k], b[k + 1], 1e-9f) is not null));

    private static double Grade(IReadOnlyList<TerrainAnchor> a, int i) => (a[i + 1].Y - a[i].Y) / (a[i + 1].S - a[i].S);

    /// <summary>Authored profile at a station (anchors interpolate linearly; nodes sit on anchors or inside straight pieces); 0 without a corridor.</summary>
    private static double Elevation(Dictionary<FeatureId, TerrainCorridorOp> corridors, NetworkLine road, double s)
    {
        if (!corridors.TryGetValue(road.Id, out var c)) return 0;
        var a = c.Anchors;
        for (var i = 0; i + 1 < a.Count; i++)
            if (s <= a[i + 1].S) return a[i].Y + (a[i + 1].Y - a[i].Y) * Math.Clamp((s - a[i].S) / (a[i + 1].S - a[i].S), 0, 1);
        return a[^1].Y;
    }
}
