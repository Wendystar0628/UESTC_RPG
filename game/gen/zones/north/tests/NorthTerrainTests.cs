using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Zones.North.Framework;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

using static Campus.Gen.Zones.North.Tests.NorthTestSupport;

[TestSuite]
public sealed class NorthTerrainTests
{
    [TestCase]
    public void TerrainOverlayParsesAndReferencesRealRoads()
    {
        var file = Terrain();
        Require(file.Zone.Value == "north" && file.Owner.Value == "WP-20", "file zone and owner");
        var roads = World().Network.Roads.ToDictionary(r => r.Id);
        foreach (var op in file.Ops)
            switch (op)
            {
                case TerrainPlatformOp p:
                    Require(p.Y is NorthTune.Terrain.Upper or NorthTune.Terrain.Lower, $"{op.Id}: terrace at +3.0 or +1.5"); break;
                case TerrainMoundOp or TerrainBasinOp:
                    var (shape, height) = op is TerrainMoundOp m ? (m.Shape, m.Height) : (((TerrainBasinOp)op).Shape, ((TerrainBasinOp)op).Height);
                    var ellipse = (TerrainEllipse)shape;
                    var steepest = 1.5 * height / Math.Min(ellipse.Radii.X, ellipse.Radii.Y);
                    Require(height <= 1.5 && steepest <= 1.0 / 3 + 0.001, $"{op.Id}: R6 relief ≤ 1.5 m, no steeper than 1:3");
                    // The courtyard hill is an activity lawn (Opus 13:32, R6): short radius at least 7.5 × height (1:5).
                    if (op.Id == "design:terrain:north-xz1-court-hill")
                        Require(Math.Min(ellipse.Radii.X, ellipse.Radii.Y) >= 7.5 * height - 0.01, $"{op.Id}: activity hill no steeper than 1:5");
                    break;
                case TerrainSlopeOp s:
                    Require(s.Geometry is PolylineGeometry && Math.Abs(s.Y1 - s.Y0) / ((PolylineGeometry)s.Geometry).Line.Points.Zip(((PolylineGeometry)s.Geometry).Line.Points.Skip(1), Vector2.Distance).Sum() <= 0.0501,
                        $"{op.Id}: the 通学坡 stays at or below 5 %"); break;
                case TerrainCorridorOp c:
                    Require(roads.TryGetValue(c.Road, out var road), $"{op.Id}: road {c.Road.Value} exists");
                    var length = road!.Line.Points.Zip(road.Line.Points.Skip(1), (a, b) => (double)Vector2.Distance(a, b)).Sum();
                    Require(Math.Abs(c.Anchors[^1].S - length) < 0.02, $"{op.Id}: last anchor at the full centreline length {length:0.00}");
                    break;
                default: throw new InvalidOperationException($"{op.Id}: unexpected operation for 20a-2");
            }
        Require(file.Ops.OfType<TerrainPlatformOp>().Count() == 3, "upper terrace and the two lower wings");
    }

    /// <summary>
    /// 20a-3: a road with no corridor keeps the automatic 0 m profile and would cut a trench across the terrace, so every
    /// line road that runs onto a terrace carries anchors; every shared centreline vertex has one elevation; the authored
    /// profile keeps to 5 %, and to 3 % within 10 m of a junction (WLD-TERRAIN).
    /// </summary>
    [TestCase]
    public void EveryRoadOnATerraceHasAnchorsAndJunctionsAgree()
    {
        var file = Terrain();
        var roads = World().Network.Roads.Where(r => r.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("highway", out _)
            && !(p.TryGetProperty("area", out var area) && area.GetString() == "yes")).ToArray();
        var corridors = file.Ops.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var terraces = file.Ops.OfType<TerrainPlatformOp>().Select(p => p.Rings.Outer).ToArray();
        foreach (var road in roads)
            if (Samples(road.Line.Points).Any(p => terraces.Any(t => PlanGeometry.InsideByCrossings(t, p))))
                Require(corridors.ContainsKey(road.Id), $"{road.Id.Value} runs onto a terrace without corridor anchors");
        foreach (var c in corridors.Values)
            for (var i = 0; i + 1 < c.Anchors.Count; i++)
                Require(Math.Abs(Grade(c.Anchors, i)) <= 0.05 + 0.001, $"{c.Id}: piece at s {c.Anchors[i].S} is steeper than 5 %");

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
                Require(Math.Abs(Elevation(corridors, road, s) - first) <= 0.01, $"junction {key} of {road.Id.Value}: {Elevation(corridors, road, s):0.000} vs {first:0.000}");
                if (!corridors.TryGetValue(road.Id, out var c)) continue;
                for (var i = 0; i + 1 < c.Anchors.Count; i++)
                    if (c.Anchors[i + 1].S > s - 10 && c.Anchors[i].S < s + 10)
                        Require(Math.Abs(Grade(c.Anchors, i)) <= 0.03 + 0.001, $"{c.Id}: steeper than 3 % within 10 m of the junction at s {s:0.0}");
            }
        }
        Require(checkedJunctions > 1000, "the whole network's junctions were checked");
    }

    /// <summary>
    /// Main session 2026-10-16 15:0x: 未命名建筑 5561 must not straddle a terrace edge (it showed a 1.4 m step, over the
    /// 1.2 m allowed without a structure). Its whole footprint stands on one level and every terrace crest keeps 6 m off it.
    /// </summary>
    [TestCase]
    public void Building5561StandsOnOneLevelClearOfTheTerraceEdge()
    {
        var footprint = Framework.Plan.BuildingRings(World().Features[new("qsh:way:550365561")]).Single();
        foreach (var terrace in Terrain().Ops.OfType<TerrainPlatformOp>())
        {
            var ring = terrace.Rings.Outer;
            var inside = footprint.Count(p => PlanGeometry.InsideByCrossings(ring, p));
            Require(inside == 0 || inside == footprint.Count, $"5561 straddles {terrace.Id}");
            var clear = float.MaxValue;
            for (var i = 0; i < footprint.Count; i++)
                for (var j = 0; j < ring.Count; j++)
                {
                    Vector2 a0 = footprint[i], a1 = footprint[(i + 1) % footprint.Count], b0 = ring[j], b1 = ring[(j + 1) % ring.Count];
                    clear = Framework.Plan.Intersect(a0, a1, b0, b1) is null ? MathF.Min(clear, MathF.Min(DistanceToSegment(a0, b0, b1), DistanceToSegment(b0, a0, a1))) : 0f;
                }
            Require(clear >= 6f, $"{terrace.Id}: crest {clear:0.00} m from 5561, at least 6 m");
        }
    }

    /// <summary>
    /// max-108 0h-b (Opus 14:20; geometry 14:26/28): the 丹桂路 design walks. The overlay parses strictly; each walk is a 2.5 m
    /// footway 7.5 m off the road centre (3 m outside the 4.5 m osmanthus row); its surface keeps 3 m off every building and
    /// water edge; it lies on the 0 m ground 丹桂路 is pinned to, clear of every terrace's side slope and every raised
    /// corridor (a design road takes no corridor anchors); and each end lies on the surface it names in <c>joins</c>: an
    /// existing walk or pedestrian area, or the kerb of a carriageway that meets 丹桂路 (a junction).
    /// </summary>
    [TestCase]
    public void DanguiWalksKeepOffWallsAndWaterAndJoinTheNetwork()
    {
        const string road = "qsh:source:way:630914103";
        const float offset = 7.5f, half = 1.25f, wallClear = 3f;
        string[] ids = ["design:north-dangui-walk-w1", "design:north-dangui-walk-w2", "design:north-dangui-walk-e1"];
        var strict = new JsonSerializerOptions(ContractJson.Options) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
        var overlayRoot = Path.Combine(DataDirectory(), "overlay");
        var overlays = Directory.GetFiles(Path.Combine(overlayRoot, "north.d"), "*.json").Order(StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<OverlayFile>(File.ReadAllBytes(path), strict)
                ?? throw new InvalidOperationException(path + " is empty")).ToArray();
        Require(overlays.All(overlay => overlay.Zone.Value == "north" && overlay.Owner.Value == "WP-20"), "overlay zone and owner");
        var walks = overlays.SelectMany(overlay => overlay.Ops).OfType<AddOp>().Where(a => a.Id.Value.StartsWith("design:north-dangui-walk-", StringComparison.Ordinal)).ToArray();
        Require(walks.Select(a => a.Id.Value).SequenceEqual(ids), "the three 丹桂路 walks, in order");

        var world = World();
        var dangui = world.Network.Roads.Single(r => r.Id.Value == road).Line.Points;
        var walls = world.Features.Values.SelectMany(f => f.Parts.Count > 0
                ? f.Parts.Where(p => (Text(p.Props, "kind") ?? f.Kind.Value) is "building" or "water").SelectMany(p => Framework.Plan.Polygons(p.Geometry))
                : f.Kind.Value is "building" or "water" ? Framework.Plan.Polygons(f.Geometry) : [])
            .Select(r => r.Outer).ToArray();
        var terrain = Terrain().Ops;
        var corridors = terrain.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var roads = world.Network.Roads.ToDictionary(r => r.Id);
        foreach (var add in walks)
        {
            var props = add.Props;
            Require(add.Kind.Value == "road" && Text(props, "roadType") == "footway" && props!.Value.GetProperty("width").GetSingle() == 2.5f, $"{add.Id.Value}: a 2.5 m footway WP-15 paves");
            Require(!string.IsNullOrWhiteSpace(add.Reason), $"{add.Id.Value}: reason given");
            var line = ((PolylineGeometry)add.Geometry).Line.Points;
            Require(world.Features[add.Id].Geometry is PolylineGeometry loaded && loaded.Line.Points.SequenceEqual(line), $"{add.Id.Value}: loaded into the world model");
            foreach (var p in line)
            {
                var d = PolylineDistance(p, dangui);
                Require(MathF.Abs(d - offset) <= 0.2f, $"{add.Id.Value}: {p} is {d:0.00} m off the road centre, not {offset}");
                Require(p.X is > -260 and < 760 && p.Y is > -1000 and < -150, $"{add.Id.Value}: {p} is in the north zone");
            }
            foreach (var p in Samples(line))
            {
                var wall = walls.Min(r => PlanGeometry.InsideByCrossings(r, p) ? 0f : RingDistance(p, r)) - half;
                Require(wall >= wallClear, $"{add.Id.Value}: surface at {p} is {wall:0.00} m from a wall or water edge");
                foreach (var water in world.Network.Waterways)
                    Require(PolylineDistance(p, water.Line.Points) - water.Width * 0.5f - half >= wallClear, $"{add.Id.Value}: {p} keeps off {water.Id.Value}");
                foreach (var op in terrain)
                    switch (op)
                    {
                        // The side slope reaches y·n beyond the crest, plus half the fillet (WLD-TERRAIN).
                        case TerrainPlatformOp t:
                            var n = double.Parse(t.Side.Fill.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);
                            var foot = Math.Abs(t.Y) * n + t.Fillet * 0.5;
                            Require(!PlanGeometry.InsideByCrossings(t.Rings.Outer, p) && RingDistance(p, t.Rings.Outer) - half >= foot, $"{add.Id.Value}: {p} is on {t.Id} or its side slope");
                            break;
                        case TerrainMoundOp or TerrainBasinOp:
                            var e = (TerrainEllipse)(op is TerrainMoundOp m ? m.Shape : ((TerrainBasinOp)op).Shape);
                            double yaw = e.YawDeg * Math.PI / 180, dx = p.X - e.Center.X, dz = p.Y - e.Center.Y;
                            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
                            Require(Math.Pow(along / (e.Radii.X + half), 2) + Math.Pow(across / (e.Radii.Y + half), 2) >= 1, $"{add.Id.Value}: {p} is on {op.Id}");
                            break;
                    }
                // Where a road crosses or runs beside the walk, its authored profile is 0 m there.
                foreach (var (id, c) in corridors)
                {
                    var hit = Project(p, roads[id].Line.Points);
                    if (hit.Distance <= roads[id].Width * 0.5f + c.Shoulder + half)
                        Require(Math.Abs(Elevation(corridors, roads[id], hit.S)) < 0.01, $"{add.Id.Value}: {p} meets {c.Id} at {Elevation(corridors, roads[id], hit.S):0.00} m");
                }
            }
            var joins = props!.Value.GetProperty("joins").EnumerateArray().Select(j => j.GetString()!).ToArray();
            Require(joins.Length == 2, $"{add.Id.Value}: names what each end joins");
            foreach (var (end, to) in new[] { (line[0], joins[0]), (line[^1], joins[1]) })
            {
                if (roads.TryGetValue(new(to), out var target))
                {
                    var type = Text(target.Props, "highway");
                    var reach = type is "footway" or "path" or "pedestrian" or "steps" ? MathF.Max(target.Width, 2f) * 0.5f : target.Width * 0.5f;
                    Require(PolylineDistance(end, target.Line.Points) <= reach + 0.05f, $"{add.Id.Value}: end {end} reaches {to}");
                    Require(type is "footway" or "path" or "pedestrian" or "steps" || Meets(target.Line.Points, dangui), $"{add.Id.Value}: {to} is a walk or a road that meets 丹桂路");
                }
                else
                {
                    var area = world.Features[new(to)];
                    Require(area.Kind.Value == "road" && Text(area.Props, "roadType") == "pedestrian", $"{add.Id.Value}: {to} is a pedestrian area");
                    var rings = Framework.Plan.Polygons(area.Geometry).Select(r => r.Outer).ToArray();
                    Require(rings.Any(r => PlanGeometry.InsideByCrossings(r, end) || RingDistance(end, r) <= 0.05f), $"{add.Id.Value}: end {end} reaches {to}");
                }
            }
        }
    }

    private static bool Meets(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b) =>
        a.Any(p => b.Contains(p)) || Enumerable.Range(0, a.Count - 1).Any(i => Enumerable.Range(0, b.Count - 1).Any(k => Framework.Plan.Intersect(a[i], a[i + 1], b[k], b[k + 1]) is not null));

    private static (float Distance, double S) Project(Vector2 p, IReadOnlyList<Vector2> line)
    {
        (float Distance, double S) best = (float.MaxValue, 0);
        var run = 0.0;
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var closest = Framework.Plan.Closest(p, line[i], line[i + 1]);
            var d = Vector2.Distance(p, closest);
            if (d < best.Distance) best = (d, run + Vector2.Distance(line[i], closest));
            run += Vector2.Distance(line[i], line[i + 1]);
        }
        return best;
    }

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
