using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

[TestSuite]
public sealed class FurnitureLayoutTests
{
    private static readonly Lazy<IWorldModel> World = new(Load);
    private static readonly Lazy<byte[]> PlantingBytes = new(() => File.ReadAllBytes(Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe", "vegetation", "planting.json")));

    /// <summary>The map's frozen source inputs as the loader supplies them: the real planting rules.</summary>
    private static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs(byte[]? planting = null) =>
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { [FurnitureStyle.PlantingKey] = planting ?? PlantingBytes.Value };

    private static FurnitureLayout Build(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy = null, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs = null) =>
        FurniturePlanner.Build(world, claimedBy, inputs ?? Inputs());

    private static IWorldModel Load()
    {
        var data = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var overlay in Directory.GetFiles(Path.Combine(data, "overlay"), "*.overlay.json").Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetFileName(overlay)] = File.ReadAllText(overlay);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    [TestCase]
    public void TheMainRoadAndTwoSecondaryRoadsAreFurnishedDeterministically()
    {
        var a = Build(World.Value);
        var b = Build(World.Value);
        var lamps = a.Items.Where(i => i.Kind == FurnitureKind.StreetLamp).ToList();
        var disc = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 0);
        var arm = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 1);
        Console.WriteLine("furniture: " + string.Join(", ", Enum.GetValues<FurnitureKind>().Select(k => $"{k} {a.Count(k)}")) + $"; disc {disc}, arm {arm}");
        Check(a.Items.Select(i => (i.Key, i.Part.At, i.Part.Params)).SequenceEqual(b.Items.Select(i => (i.Key, i.Part.At, i.Part.Params))), "two builds differ");
        Check(a.Items.Select(i => i.Key).Distinct().Count() == a.Items.Count, "duplicate keys");
        // The avenue is about 1.43 km: 52 slots a side at 27.5 m, less junctions and crossings; the two secondary roads 0.69 and 0.62 km at 35 m.
        Check(disc is >= 70 and <= 104, $"{disc} avenue lamps");
        Check(arm is >= 20 and <= 36, $"{arm} secondary lamps");
        // Rest stops only where a walk takes a bench (Opus review 12:15): the avenue's north half and Shuisha Road's walk.
        Check(a.Count(FurnitureKind.Bench) >= 8, $"{a.Count(FurnitureKind.Bench)} benches");
        Check(a.Count(FurnitureKind.Bin) * 2 >= a.Count(FurnitureKind.Bench) - 2 && a.Count(FurnitureKind.Bin) <= a.Count(FurnitureKind.Bench), "a bin at every other rest stop");
        // SYS-ATLAS §6: a kiosk or a post on every anchor whose point lands on open ground. The counts follow the anchor data, so
        // moving an anchor (WP-48c) never needs a new number here.
        var landings = AnchorLandings(World.Value);
        var kiosks = landings.Count(l => l.Form == "kiosk" && l.Landing == Landing.Open);
        var posts = landings.Count(l => l.Form == "post" && l.Landing == Landing.Open);
        Check(kiosks + posts > 0, "no anchor lands on open ground");
        Check(a.Count(FurnitureKind.AtlasKiosk) == kiosks && a.Count(FurnitureKind.AtlasPost) == posts,
            $"{a.Count(FurnitureKind.AtlasKiosk)} kiosks and {a.Count(FurnitureKind.AtlasPost)} posts for {kiosks} and {posts} anchors on open ground");
        Check(a.Count(FurnitureKind.BusStop) == 2, $"{a.Count(FurnitureKind.BusStop)} shuttle stops");
        var open = World.Value.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open).Select(e => e.Id).Distinct().Count();
        Check(new[] { FurnitureKind.EntranceMat, FurnitureKind.OpenSign, FurnitureKind.EntranceLamp }.All(k => a.Count(k) == open), $"one kit at each of {open} open entrances");
    }

    [TestCase]
    public void BenchesLineAWalkAndBinsStandApart()
    {
        // Opus review 12:15: a bench lines a walk and faces it, its bay joined to the walk, a passage of 1.2 m left in front; it
        // never faces a carriageway or sits in the strip between the kerb and the tree rows. Its bin stands 2–4 m away on its own bay.
        var world = World.Value;
        var layout = Build(world);
        var walks = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian").ToList();
        var lanes = world.Network.Roads.Where(Carriageway).ToList();
        // Rest stops along walks; a rest node's benches (key "node:") stand at a plaza or walk end and are judged by their own test.
        var benches = layout.Items.Where(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:", StringComparison.Ordinal)).ToList();
        foreach (var item in benches.Concat(layout.Items.Where(i => i.Kind == FurnitureKind.Bin)))
        {
            var p = (FurnitureParams)item.Part.Params;
            var facing = Front(item.Part.At);
            var front = item.Anchor + facing * (p.Depth * 0.5f);
            var walk = walks.MinBy(w => Distance(front, w));
            var edge = Distance(front, walk!) - Half(walk!);
            Check(edge is >= -0.02f and <= FurnitureStyle.WalkGap + 0.1f, $"{item.Key}: front {edge:F2} m off its walk's edge");
            var toWalk = Vector2.Normalize(Nearest(front, walk!) - item.Anchor);
            Check(Vector2.Dot(toWalk, facing) > 0.97f, $"{item.Key} does not face its walk");
            Check(2f * Half(walk!) - FurnitureStyle.FeetReach >= FurnitureStyle.PassClear, $"{item.Key}: walk too narrow to pass");
            var pad = item.Pad ?? throw new InvalidOperationException($"{item.Key} has no bay");
            var padFront = new Vector2(pad.Frame.M41, pad.Frame.M43) + facing * pad.HalfDepth;
            Check(Distance(padFront, walk!) < Half(walk!), $"{item.Key}: its bay does not reach the walk");
            Check(MathF.Abs(pad.Top - item.Part.At.M42) < 1e-4f, $"{item.Key} is off its bay");
            var radius = p.Depth * 0.5f;
            foreach (var lane in lanes)
                Check(Distance(item.Anchor, lane) - Half(lane) - radius >= FurnitureStyle.RoadSetback - 0.6f, $"{item.Key} stands in the kerbside strip of {lane.Id.Value}");
            if (item.Kind != FurnitureKind.Bench) continue;
            for (var d = 0f; d <= FurnitureStyle.FaceClear; d += 0.5f)
                Check(lanes.All(l => Distance(front + facing * d, l) >= Half(l)), $"{item.Key} faces a carriageway {d:F1} m ahead");
        }
        foreach (var bin in layout.Items.Where(i => i.Kind == FurnitureKind.Bin))
        {
            var bench = benches.Single(i => i.Key == bin.Key.Replace(":bin", ":bench", StringComparison.Ordinal));
            var gap = Vector2.Distance(bin.Anchor, bench.Anchor) - (FurnitureStyle.Bench.Width + FurnitureStyle.Bin.Width) * 0.5f;
            Check(gap is >= FurnitureStyle.BinGapMin - 0.15f and <= FurnitureStyle.BinGapMax + 0.15f, $"{bin.Key} stands {gap:F2} m from its bench");
            Check(!ReferenceEquals(bin.Pad, bench.Pad), $"{bin.Key} shares the bench's bay");
        }
    }

    [TestCase]
    public void AnchorsOnACarriagewayInWaterOrInABuildingAreLeftOutAndLogged()
    {
        // An anchor whose point lands on a carriageway, in water or in a building gets nothing, and the layout says so in one log
        // line. Judged from the world data (StreetSite.LandingAt), not by anchor ID: moving an anchor needs no change here.
        var world = World.Value;
        var layout = Build(world);
        Console.WriteLine("furniture log: " + string.Join(" | ", layout.Log));
        var landings = AnchorLandings(world);
        foreach (var (feature, form, _, landing) in landings)
        {
            var id = feature.Id.Value;
            var placed = layout.Items.Any(i => i.Key == "anchor:" + id);
            var left = layout.Log.Where(l => l.StartsWith($"anchor {id} ", StringComparison.Ordinal) && l.Contains("not placed", StringComparison.Ordinal)).ToList();
            if (landing == Landing.Open)
            {
                Check(placed, $"{id} lands on open ground but is not placed");
                Check(left.Count == 0, $"{id} is placed but logged as not placed");
                continue;
            }
            Check(!placed, $"{id} placed, though its point lands {Where(landing)}");
            Check(left.SequenceEqual([$"anchor {id} ({form}) not placed: its point lands {Where(landing)}"]), $"{id} not logged once as {Where(landing)}");
        }
        Check(layout.Log.Count(l => l.Contains("not placed", StringComparison.Ordinal)) == landings.Count(l => l.Landing != Landing.Open), "a log line without a left-out anchor");
        // A building: a placed anchor moved onto a building's centre is left out too, and nothing else changes.
        var building = world.Features.Values.Where(f => f.Kind.Value == "building" && f.Geometry is PolygonGeometry).OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => (Feature: f, Centre: PlanGeometry.Centroid(((PolygonGeometry)f.Geometry).Rings.Outer)))
            .First(b => PlanGeometry.Contains(((PolygonGeometry)b.Feature.Geometry).Rings, b.Centre) && PlanGeometry.DistanceToRing(b.Centre, ((PolygonGeometry)b.Feature.Geometry).Rings.Outer) > 3f);
        var anchor = landings.First(l => l.Landing == Landing.Open);
        var ring = ((PolygonGeometry)anchor.Feature.Geometry).Rings.Outer;
        var shift = building.Centre - anchor.Centre;
        var moved = anchor.Feature with { Geometry = new PolygonGeometry(new PolygonRings([.. ring.Select(p => p + shift)], [])) };
        var indoors = Build(new FeatureWorld(world, moved));
        var name = anchor.Feature.Id.Value;
        Check(!indoors.Items.Any(i => i.Key == "anchor:" + name), "an anchor's furniture stands in a building");
        Check(indoors.Log.Contains($"anchor {name} ({anchor.Form}) not placed: its point lands in a building"), "the building fallback is not logged");
        Check(indoors.Items.Count == layout.Items.Count - 1, "the building fallback changed other furniture");
    }

    [TestCase]
    public void AnchorsStandOnOrBesidePaving()
    {
        // Opus review 12:15: a kiosk's apron or a post's foot touches pedestrian paving (≤ 0.5 m off it); the facing never changes.
        var world = World.Value;
        var layout = Build(world);
        var lines = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian" or "steps").ToList();
        var areas = new List<PolygonRings>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "road"))
        {
            var sources = feature.Parts.Count == 0 ? [(feature.Props, feature.Geometry)] : feature.Parts.Select(p => (p.Props ?? feature.Props, p.Geometry)).ToList();
            foreach (var (props, geometry) in sources)
                if (RoadType(props) is "footway" or "path" or "pedestrian" or "steps")
                    areas.AddRange(geometry switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => [] });
        }
        float Gap(Vector2 q) => MathF.Max(0f, MathF.Min(
            lines.Select(l => Distance(q, l) - Half(l)).DefaultIfEmpty(float.MaxValue).Min(),
            areas.Select(a => PlanGeometry.Contains(a, q) ? 0f : a.Holes.Append(a.Outer).Min(r => PlanGeometry.DistanceToRing(q, r))).DefaultIfEmpty(float.MaxValue).Min()));
        foreach (var item in layout.Items.Where(i => i.Kind is FurnitureKind.AtlasKiosk or FurnitureKind.AtlasPost))
        {
            var feature = world.Features[new(item.Key["anchor:".Length..])];
            Check(Vector2.Dot(Front(item.Part.At), ExpectedFacing(layout, feature)) > 0.999f, $"{item.Key} turned");
            if (feature.Props!.Value.TryGetProperty("groundY", out var deck) && deck.GetSingle() > 0.5f) continue;
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            if (Vector2.Distance(centre, item.Anchor) < 1e-3f && Gap(centre) > FurnitureStyle.AnchorPavingReach)
            {
                Console.WriteLine($"{item.Key}: no pedestrian paving within {FurnitureStyle.AnchorPavingReach} m, left for the anchor data");
                continue;
            }
            var hw = item.Pad is { } apron ? apron.HalfWidth : ((SignParams)item.Part.Params).Width * 0.5f;
            var hd = item.Pad is { } pad ? pad.HalfDepth : ((SignParams)item.Part.Params).Depth * 0.5f;
            var gap = float.MaxValue;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                    if (i != 0 || k != 0)
                    {
                        var q = Xf.Point(item.Part.At, new(i * hw, 0, k * hd));
                        gap = MathF.Min(gap, Gap(new(q.X, q.Z)));
                    }
            Console.WriteLine($"{item.Key}: footprint {gap:F2} m off paving");
            Check(gap <= FurnitureStyle.AnchorPavingGap + 0.1f, $"{item.Key}: footprint {gap:F2} m off pedestrian paving");
        }
    }

    [TestCase]
    public void TreeRowsComeFromThePlantingInputAndFallBackWithOneWarning()
    {
        // The rows are read from the map's frozen planting rules, not copied: the real file gives the avenue's two rows and its band,
        // and one row on each secondary road, with nothing to warn about.
        var (roads, diagnostics) = PlantingRows.Resolve(Inputs());
        Check(diagnostics.Count == 0, "warnings on the real planting file: " + string.Join(" | ", diagnostics.Select(d => d.Message)));
        Check(roads.Select(r => r.Name).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Name)) && roads.Select(r => r.Tier).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Tier)), "the roads or their tiers changed");
        Check(roads[0].Rows.SequenceEqual([7f, 14f, 17.5f]) && roads[1].Rows.SequenceEqual([5f]) && roads[2].Rows.SequenceEqual([4.5f]), "rows differ from the planting file");
        // A changed offset is read back; a road the file leaves out keeps its fallback and is warned about once.
        var edited = Edit(root => root["roads"]![1]!["rows"] = new JsonArray(new JsonObject { ["offset"] = 6.5f }));
        Check(PlantingRows.Resolve(Inputs(edited)).Roads[1].Rows.SequenceEqual([6.5f]), "an edited offset is not read");
        var (kept, gap) = PlantingRows.Resolve(Inputs(Edit(root => root["roads"]!.AsArray().RemoveAt(2))));
        Check(kept[2].Rows.SequenceEqual(FurnitureStyle.Roads[2].Rows) && gap.Count == 1 && gap[0].Level == DiagnosticLevel.Warning && gap[0].Code == PlantingRows.RoadCode && gap[0].Message.Contains("丹桂路", StringComparison.Ordinal),
            "a road missing from the planting file is not warned about once");
        // Nothing to read, or nothing readable: the fallback stands, with exactly one warning, and the layout still builds.
        (IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? Inputs, string Code)[] unreadable =
        [
            (null, PlantingRows.MissingCode), (new Dictionary<string, ReadOnlyMemory<byte>>(), PlantingRows.MissingCode),
            (Inputs([0x7B, 0x22]), PlantingRows.UnreadableCode), (Inputs("[]"u8.ToArray()), PlantingRows.UnreadableCode),
        ];
        foreach (var (inputs, code) in unreadable)
        {
            var (fallback, warnings) = PlantingRows.Resolve(inputs);
            Check(fallback.Select(r => r.Rows).Zip(FurnitureStyle.Roads.Select(r => r.Rows), (a, b) => a.SequenceEqual(b)).All(same => same), "the fallback rows are not the style's");
            Check(warnings.Count == 1 && warnings[0].Level == DiagnosticLevel.Warning && warnings[0].Code == code, $"{warnings.Count} diagnostics for {code}");
            var layout = FurniturePlanner.Build(World.Value, null, inputs);
            Check(layout.Diagnostics.SequenceEqual(warnings) && layout.Items.Count == Build(World.Value).Items.Count, "the fallback layout differs from the real one, whose rows are the same");
        }
        Check(Build(World.Value).Diagnostics.Count == 0, "the real layout warns");
    }

    [TestCase]
    public void ChangingARowOffsetInThePlantingInputMovesTheBenches()
    {
        // A bench keeps RowClear off the planted rows, so moving a row onto an avenue bench forces that bench off its slot (or off
        // the walk altogether); the other roads' benches stay where they were.
        var world = World.Value;
        var before = Build(world);
        var avenue = world.Network.Roads.Where(l => StreetSite.RoadName(l) == FurnitureStyle.Roads[0].Name).ToList();
        var bench = before.Items.First(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:0:", StringComparison.Ordinal));
        var offset = avenue.Min(l => Distance(bench.Anchor, l));
        var moved = Build(world, null, Inputs(Edit(root => root["roads"]![0]!["rows"] = new JsonArray(new JsonObject { ["offset"] = offset }))));
        Console.WriteLine($"{bench.Key} stood {offset:F2} m off the avenue's centre line; a row there: {(moved.Items.FirstOrDefault(i => i.Key == bench.Key) is { } m ? $"moved {Vector2.Distance(m.Anchor, bench.Anchor):F2} m" : "dropped")}");
        var after = moved.Items.FirstOrDefault(i => i.Key == bench.Key);
        Check(after is null || Vector2.Distance(after.Anchor, bench.Anchor) > 0.5f, $"{bench.Key} stands where it did, on a tree row");
        // And the row it now keeps clear of is the new one: every bench of the avenue is off the offset line.
        foreach (var item in moved.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin && i.Key.StartsWith("rest:0:", StringComparison.Ordinal)))
            Check(MathF.Abs(avenue.Min(l => Distance(item.Anchor, l)) - offset) >= FurnitureStyle.RowClear, $"{item.Key} stands on the changed row");
        foreach (var item in before.Items.Where(i => !i.Key.StartsWith("rest:0:", StringComparison.Ordinal) && !i.Key.StartsWith("lamp:0:", StringComparison.Ordinal)))
            Check(moved.Items.Any(m => m.Key == item.Key && Vector2.Distance(m.Anchor, item.Anchor) < 1e-3f), $"{item.Key} moved with the avenue's rows");
    }

    /// <summary>The real planting rules, edited by <paramref name="change"/>, as the bytes the loader would supply.</summary>
    private static byte[] Edit(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(PlantingBytes.Value)!;
        change(root);
        return JsonSerializer.SerializeToUtf8Bytes(root);
    }

    [TestCase]
    public void ARoadWithRestNodesHasItsBenchesAtTheNodesNotAlongItsLine()
    {
        // Opus review 16:23: a road with rest nodes has no rest stops along its line; its benches stand at the nodes, on the pedestrian
        // paving there, facing it, under the walk benches' rules (3 m off carriageways, no carriageway 12 m ahead). Judged on the
        // avenue with a node put where one of its own benches stands, so the spot is known to take a bench.
        var world = World.Value;
        var site = new StreetSite(world);
        var baseline = Build(world);
        var spot = baseline.Items.First(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:0:", StringComparison.Ordinal)).Anchor;
        var avenue = FurnitureStyle.Roads[0] with { Nodes = [new RestNode("test node", spot)] };
        var layout = FurniturePlanner.Build(world, null, Inputs(), [avenue, FurnitureStyle.Roads[1], FurnitureStyle.Roads[2]]);
        Check(!layout.Items.Any(i => i.Key.StartsWith("rest:0:", StringComparison.Ordinal)), "rest stops along a road with nodes");
        Check(layout.Items.Count(i => i.Key.StartsWith("lamp:0:", StringComparison.Ordinal)) == baseline.Items.Count(i => i.Key.StartsWith("lamp:0:", StringComparison.Ordinal)), "the node rule changed the road's lamps");
        var benches = layout.Items.Where(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"node benches at {spot}: " + string.Join(", ", benches.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")) + "; log: " + string.Join(" | ", layout.Log.Where(l => l.Contains("node", StringComparison.Ordinal))));
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches, $"{benches.Count} benches at the node");
        Check(!layout.Log.Any(l => l.Contains("has no bench", StringComparison.Ordinal) && l.Contains("test node", StringComparison.Ordinal)), "a node with benches logs 'no bench'");
        foreach (var bench in benches)
        {
            var facing = Front(bench.Part.At);
            var p = (FurnitureParams)bench.Part.Params;
            var front = bench.Anchor + facing * (p.Depth * 0.5f);
            Check(Vector2.Distance(bench.Anchor, spot) <= FurnitureStyle.NodeReach + 0.1f, $"{bench.Key} is {Vector2.Distance(bench.Anchor, spot):F1} m from its node");
            Check(site.PavingEdgeFacing(front + facing * 0.3f) is { Gap: <= 0f }, $"{bench.Key} does not face pedestrian paving");
            Check(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f), $"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway");
            Check(!site.CarriagewayAhead(front, facing, FurnitureStyle.FaceClear), $"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m");
            Check(bench.Pad is { } bay && MathF.Abs(bay.Top - bench.Part.At.M42) < 1e-4f, $"{bench.Key} is off its bay");
        }
        Check(benches.Count < 2 || Vector2.Distance(benches[0].Anchor, benches[1].Anchor) >= FurnitureStyle.NodeBenchGap - 1e-3f, "the node's benches stand closer than the gap");
    }

    [TestCase]
    public void TheOsmanthusRoadsNodesTakeBenchesOrSayWhyNot()
    {
        // The road's walks carry no benches (the design walks sit 7.5 m off the road: a bench on either side is too near the carriageway
        // or looks at it); its two nodes take benches under the same rules or log why none fits, never by relaxing them.
        var world = World.Value;
        var layout = Build(world);
        var index = FurnitureStyle.Roads.ToList().FindIndex(r => r.Nodes is not null);
        Check(index >= 0, "no road has rest nodes");
        var rule = FurnitureStyle.Roads[index];
        Check(!layout.Items.Any(i => i.Key.StartsWith($"rest:{index}:", StringComparison.Ordinal)), $"benches or bins along {rule.Name}");
        for (var n = 0; n < rule.Nodes!.Count; n++)
        {
            var node = rule.Nodes[n];
            var placed = layout.Items.Count(i => i.Key.StartsWith($"node:{index}:{n}:", StringComparison.Ordinal));
            var says = layout.Log.Where(l => l.StartsWith($"road {rule.Name} node {node.Label} has no bench: ", StringComparison.Ordinal)).ToList();
            Console.WriteLine($"{rule.Name} node {node.Label}: {placed} benches" + (says.Count > 0 ? "; " + says[0] : ""));
            Check(placed > 0 ? says.Count == 0 : says.Count == 1 && says[0].Length > 60, $"node {node.Label}: {placed} benches and {says.Count} 'no bench' lines");
        }
    }

    [TestCase]
    public void ADesignWalkFiledAsFeatureAndAsNetworkLineHasItsNodeBenchesOnce()
    {
        // WP-08's effective network files a design walk twice under one ID: as the feature the overlay added and as a network line.
        // The walk is one walk: whichever way it is filed (line only, feature only, both) its node gets the same benches, standing
        // just off its edge and facing it, and a walk that is filed nowhere (hidden) gets none. Judged on a walk made up on open
        // ground, so it holds on a world with or without the effective network.
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        var (line, feature) = DesignWalk(spot);
        var rules = NodeRules(spot);
        FurnitureLayout Layout(NetworkLine? asLine, WorldFeature? asFeature) => FurniturePlanner.Build(new WalkWorld(open, asLine, asFeature), null, Inputs(), rules);
        List<FurnitureItem> Benches(FurnitureLayout layout) => layout.Items.Where(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)).ToList();
        var none = Layout(null, null);
        var both = Layout(line, feature);
        var benches = Benches(both);
        Console.WriteLine($"design walk at {spot}: {benches.Count} node benches " + string.Join(", ", benches.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")));
        Check(Benches(none).Count == 0 && none.Log.Any(l => l.Contains("node open node has no bench", StringComparison.Ordinal)), "a node with no walk has benches or no log line");
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches, $"{benches.Count} benches at the node of a walk filed twice");
        Check(benches.Count < 2 || Vector2.Distance(benches[0].Anchor, benches[1].Anchor) >= FurnitureStyle.NodeBenchGap - 1e-3f, "the walk's node benches stand closer than the gap");
        Check(both.Items.Count == none.Items.Count + benches.Count && !both.Log.Any(l => l.Contains("open node", StringComparison.Ordinal)), "the walk changed more than its node's benches");
        var site = new StreetSite(new WalkWorld(open, line, feature));
        foreach (var bench in benches)
        {
            var facing = Front(bench.Part.At);
            var p = (FurnitureParams)bench.Part.Params;
            var front = bench.Anchor + facing * (p.Depth * 0.5f);
            var edge = Distance(front, line) - Half(line);
            Check(edge is >= -0.02f and <= FurnitureStyle.WalkGap + 0.1f, $"{bench.Key}: front {edge:F2} m off the walk's edge");
            Check(site.PavingEdgeFacing(front + facing * 0.3f) is { Gap: <= 0f, Road: { } walk } && walk.Id == line.Id, $"{bench.Key} does not face the walk");
        }
        foreach (var (name, filed) in new[] { ("line only", Layout(line, null)), ("feature only", Layout(null, feature)) })
            Check(Benches(filed).Select(b => (b.Key, b.Anchor, b.Part.Params)).SequenceEqual(benches.Select(b => (b.Key, b.Anchor, b.Part.Params))), $"the walk filed as {name} has other node benches than filed twice");
        // The same through the loader: an overlay adds the walk (with and without a hide that takes it away again).
        var shown = FurniturePlanner.Build(new TerrainWorld(Loaded(spot, hide: false), FlatTerrain.Default), null, Inputs(), rules);
        var hidden = FurniturePlanner.Build(new TerrainWorld(Loaded(spot, hide: true), FlatTerrain.Default), null, Inputs(), rules);
        Check(Benches(shown).Select(b => (b.Key, b.Anchor)).SequenceEqual(benches.Select(b => (b.Key, b.Anchor))), "the loaded walk has other node benches than the walk filed by hand");
        Check(hidden.Items.Count == none.Items.Count && Benches(hidden).Count == 0 && hidden.Log.SequenceEqual(none.Log), "a hidden design walk still gets furniture");
    }

    /// <summary>The avenue's rule with one rest node at <paramref name="spot"/> (the others as they are): the node's benches are keyed <c>node:0:0:</c>.</summary>
    private static IReadOnlyList<RoadRule> NodeRules(Vector2 spot) =>
        [FurnitureStyle.Roads[0] with { Nodes = [new RestNode("open node", spot)] }, FurnitureStyle.Roads[1], FurnitureStyle.Roads[2]];

    /// <summary>A 16 m east–west design walk centred on <paramref name="spot"/>, as the network line and as the feature of one ID.</summary>
    private static (NetworkLine Line, WorldFeature Feature) DesignWalk(Vector2 spot)
    {
        var id = new FeatureId("design:furniture-test-walk");
        var points = new Polyline([spot + new Vector2(-8f, 0f), spot + new Vector2(8f, 0f)]);
        var props = JsonSerializer.SerializeToElement(new { roadType = "footway", width = 2.5f });
        return (new NetworkLine(id, points, 2.5f) { Props = props }, new WorldFeature(id, FeatureSource.Design, new("road"), new PolylineGeometry(points)) { Props = props });
    }

    /// <summary>The real world with an overlay that adds the made-up design walk, and takes it away again when <paramref name="hide"/>.</summary>
    private static IWorldModel Loaded(Vector2 spot, bool hide)
    {
        var data = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var overlay in Directory.GetFiles(Path.Combine(data, "overlay"), "*.overlay.json").Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetFileName(overlay)] = File.ReadAllText(overlay);
        var (line, feature) = DesignWalk(spot);
        var ops = new List<OverlayOp> { new AddOp(feature.Id, feature.Kind, feature.Geometry, null, feature.Props, "furniture test walk") };
        if (hide) ops.Add(new HideOp(feature.Id, "furniture test: hidden again"));
        files["overlay/zz-furniture-test.overlay.json"] = JsonSerializer.Serialize(new OverlayFile(new("furniture-test"), new("WP-13"), ops) { Map = new("qingshuihe") }, ContractJson.Options);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static readonly Vector2[] Compass = [new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f), new(0.7f, 0.7f), new(-0.7f, 0.7f), new(0.7f, -0.7f), new(-0.7f, -0.7f)];

    /// <summary>The first spot (scanning west to east, north to south) with open ground all round: no outline, tree, road or paving near, inside the campus.</summary>
    private static Vector2 OpenGround(IWorldModel world)
    {
        const float Clear = 30f;
        var site = new StreetSite(world);
        var lines = world.Network.Roads.Concat(world.Network.Bridges).ToList();
        for (var x = -1200f; x <= 1200f; x += 24f)
            for (var z = -1200f; z <= 1200f; z += 24f)
            {
                var p = new Vector2(x, z);
                if (!PlanGeometry.Contains(world.Boundary, p) || site.Blocker(p, 14f) is not null || site.PavingEdgeFacing(p) is not null) continue;
                if (!Compass.All(d => PlanGeometry.Contains(world.Boundary, p + d * Clear))) continue;
                if (lines.Any(l => Distance(p, l) < Half(l) + Clear)) continue;
                return p;
            }
        throw new InvalidOperationException("The map has no open ground for a made-up walk.");
    }

    [TestCase]
    public void AKioskOffThePavingTurnsOntoItWholeWithItsBackToTheEdge()
    {
        // Opus review 14:20: an apron that does not touch paving stands whole on it, 0.5 m inside the edge, its back parallel to the
        // edge and its face towards the paving, and the layout logs the yaw (and centre) the anchor data should take over. Judged on
        // a synthetic anchor: the first kiosk of the data moved to a grass notch inside the plaza near (120, -65) (A04's old spot).
        var world = World.Value;
        var site = new StreetSite(world);
        var spot = new Vector2(120f, -65f);
        Check(site.PavingEdgeFacing(spot) is { Gap: > 1f }, "the notch is not off the paving any more: pick another spot");
        var original = world.Features.Values.Where(f => f.Kind.Value == "anchor" && Text(f.Props, "form") == "kiosk" && f.Props!.Value.TryGetProperty("groundY", out var y) && y.GetSingle() < 0.5f)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal).First();
        var ring = new List<Vector2> { spot + new Vector2(-0.4f, -1f), spot + new Vector2(-0.4f, 1f), spot + new Vector2(0.4f, 1f), spot + new Vector2(0.4f, -1f) };
        var moved = original with { Geometry = new PolygonGeometry(new PolygonRings(ring, [])), Props = Edit(original.Props!.Value, "yawDeg", 90f) };
        var layout = Build(new FeatureWorld(world, moved));
        var turned = Turned(layout, moved.Id.Value) ?? throw new InvalidOperationException("the kiosk off the paving is not logged as turned: " + string.Join(" | ", layout.Log));
        var item = layout.Items.Single(i => i.Key == "anchor:" + moved.Id.Value);
        Console.WriteLine($"kiosk off paving: yaw {turned.Yaw}, centre {turned.Centre}; log: {layout.Log.Single(l => l.StartsWith($"anchor {moved.Id.Value} ", StringComparison.Ordinal) && l.Contains("turned onto", StringComparison.Ordinal))}");
        var facing = Front(item.Part.At);
        Check(Vector2.Dot(facing, FurniturePlanner.Facing(turned.Yaw)) > 0.999f && Vector2.Distance(item.Anchor, turned.Centre) < 0.01f, "the log does not give the placement");
        Check(Vector2.Dot(facing, FurniturePlanner.Facing(90f)) < 0.99f, "the yaw of the data stands, though the apron was off the paving");
        var apron = item.Pad ?? throw new InvalidOperationException("no apron");
        var right = new Vector2(-facing.Y, facing.X);
        // The whole outline of the apron is on paving.
        for (var u = -apron.HalfWidth; u <= apron.HalfWidth + 1e-3f; u += 0.25f)
            foreach (var v in new[] { -apron.HalfDepth, apron.HalfDepth })
                Check(site.PavingEdgeFacing(item.Anchor + right * u + facing * v) is { Gap: <= 0f }, $"the apron's outline leaves the paving at ({u:F2}, {v:F2})");
        for (var v = -apron.HalfDepth; v <= apron.HalfDepth + 1e-3f; v += 0.25f)
            foreach (var u in new[] { -apron.HalfWidth, apron.HalfWidth })
                Check(site.PavingEdgeFacing(item.Anchor + right * u + facing * v) is { Gap: <= 0f }, $"the apron's side leaves the paving at ({u:F2}, {v:F2})");
        // The back edge runs along the paving edge, 0.5 m inside it: stepping 0.7 m behind the back edge's middle reaches 0.2 m past the edge.
        var back = item.Anchor - facing * apron.HalfDepth;
        var behind = site.PavingEdgeFacing(back - facing * (FurnitureStyle.ApronPavingClear + 0.2f)) ?? throw new InvalidOperationException("no paving edge behind the kiosk");
        Check(behind.Gap is > 0.05f and < 0.35f, $"the paving edge lies {behind.Gap + FurnitureStyle.ApronPavingClear:F2} m behind the back edge, not 0.5 m");
        Check(Vector2.Dot(behind.Inward, facing) > 0.98f, "the back edge is not parallel to the paving edge, or the face looks at it");
    }

    private static (float Yaw, Vector2 Centre)? Turned(FurnitureLayout layout, string id)
    {
        var line = layout.Log.SingleOrDefault(l => l.StartsWith($"anchor {id} (kiosk) turned onto paving: ", StringComparison.Ordinal));
        if (line is null) return null;
        var m = System.Text.RegularExpressions.Regex.Match(line, @"yawDeg (-?\d+\.\d+) \(data -?\d+\.\d+\), centre \((-?\d+\.\d+), (-?\d+\.\d+)\) \(data");
        Check(m.Success, "unreadable log line: " + line);
        float F(int g) => float.Parse(m.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture);
        return (F(1), new(F(2), F(3)));
    }

    /// <summary>The facing an anchor has in the layout: its data's yaw, or the yaw its "turned onto paving" log line gives.</summary>
    private static Vector2 ExpectedFacing(FurnitureLayout layout, WorldFeature feature) =>
        FurniturePlanner.Facing(Turned(layout, feature.Id.Value)?.Yaw ?? feature.Props!.Value.GetProperty("yawDeg").GetSingle());

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>A copy of an anchor's props with one number replaced.</summary>
    private static JsonElement Edit(JsonElement props, string name, float value)
    {
        var node = JsonNode.Parse(props.GetRawText())!;
        node[name] = value;
        return JsonSerializer.SerializeToElement(node);
    }

    [TestCase]
    public void AvenueLampsKeepTheirRhythm()
    {
        // MAP-QSH-ADDITIONS §8.5: the avenue's lamps keep a regular 25–30 m; only junctions may nudge one by a step or two.
        foreach (var side in new[] { ":l:", ":r:" })
        {
            var row = Build(World.Value).Items.Where(i => i.Kind == FurnitureKind.StreetLamp && i.Key.StartsWith("lamp:0:0" + side, StringComparison.Ordinal)).ToList();
            var gaps = row.Zip(row.Skip(1), (p, q) => Vector2.Distance(p.Anchor, q.Anchor)).ToList();
            Check(gaps.Count > 30, $"{gaps.Count} gaps on side {side}");
            var regular = gaps.Count(g => g is >= 24f and <= 31f);
            Check(regular >= gaps.Count * 0.75f, $"side {side}: only {regular} of {gaps.Count} gaps are 25–30 m");
        }
    }

    [TestCase]
    public void RoadFurnitureKeepsOffRoadsBuildingsAndWater()
    {
        var world = World.Value;
        var outlines = world.Features.Values.Where(f => f.Kind.Value is "building" or "water").SelectMany(Polygons).ToList();
        foreach (var item in Build(world).Items.Where(i => i.Kind is FurnitureKind.StreetLamp or FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.BusStop))
        {
            var p = item.Anchor;
            Check(PlanGeometry.Contains(world.Boundary, p), $"{item.Key} outside the campus");
            Check(!outlines.Any(o => PlanGeometry.Contains(o, p)), $"{item.Key} inside a building or water");
            foreach (var road in world.Network.Roads)
            {
                var d = MathF.Sqrt(road.Line.Points.Zip(road.Line.Points.Skip(1), (u, v) => PlanGeometry.SegmentDistanceSquared(p, u, v)).DefaultIfEmpty(float.MaxValue).Min());
                Check(d > MathF.Max(road.Width, 2f) * 0.5f + 0.2f, $"{item.Key} stands on {road.Id.Value} ({d:F2} m from its centre line)");
            }
        }
    }

    [TestCase]
    public void FurnitureLandsOnTheTerrain()
    {
        // 8% everywhere: poles sink 0.05–0.3 m below the lawn level at their centre; benches and bins share a concrete plinth.
        var terrain = new Tilted(0.08f);
        var gentle = Build(new TerrainWorld(World.Value, terrain));
        foreach (var lamp in gentle.Items.Where(i => i.Kind == FurnitureKind.StreetLamp))
        {
            var burial = terrain.HeightAt(lamp.Part.At.M41, lamp.Part.At.M43) + FurnitureStyle.LawnLift - lamp.Part.At.M42;
            Check(burial is >= 0.049f and <= 0.301f, $"{lamp.Key} buried {burial:F3} m");
        }
        var benches = gentle.Items.Where(i => i.Kind == FurnitureKind.Bench).ToList();
        Check(benches.Count > 10, "no benches on 8%");
        foreach (var bench in benches)
        {
            Check(bench.Pad is { Plinth: true } pad && MathF.Abs(pad.Top - bench.Part.At.M42) < 1e-4f, $"{bench.Key} has no plinth on 8%");
            var plinth = bench.Pad!;
            var high = Corners(bench).Max(c => terrain.HeightAt(c.X, c.Y));
            var low = Corners(bench).Min(c => terrain.HeightAt(c.X, c.Y));
            Check(plinth.Top >= high + FurnitureStyle.PavingLift - 1e-3f && plinth.Bottom <= low - FurnitureStyle.PadSkirt + 1e-3f, $"{bench.Key}: plinth does not cover the slope");
            Check(bench.Colliders.OfType<BoxCollider>().Any(c => c.Foot == FootMat.Concrete), $"{bench.Key}: plinth has no collider");
            Check(plinth.Geometry().Meshes.Sum(m => m.Index.Length) > 0, $"{bench.Key}: empty plinth mesh");
            var bin = gentle.Items.SingleOrDefault(i => i.Key == bench.Key.Replace(":bench", ":bin", StringComparison.Ordinal));
            Check(bin is null || bin.Pad is { Plinth: true } own && MathF.Abs(bin.Part.At.M42 - own.Top) < 1e-4f, $"{bench.Key}: the bin is off its own plinth");
        }
        // Flat: rest stops stand on paved bays just proud of the paving; the kiosk on the A06 deck stays on its deck.
        var flat = Build(World.Value);
        var top = FurnitureStyle.PavingLift + FurnitureStyle.PadProud;
        Check(flat.Items.Where(i => i.Pad is not null).All(i => !i.Pad!.Plinth), "plinths on flat ground");
        Check(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - top) < 1e-5f), "benches off their bays");
        var kiosks = flat.Items.Where(i => i.Kind == FurnitureKind.AtlasKiosk).ToList();
        Check(kiosks.Count(k => MathF.Abs(k.Part.At.M42 - 5f - top) < 1e-3f) == 1 && kiosks.Count(k => MathF.Abs(k.Part.At.M42 - top) < 1e-5f) == kiosks.Count - 1,
            "kiosks: one on the A06 deck, the others on their pads");
        // 30%: no pole stands.
        var steep = Build(new TerrainWorld(World.Value, new Tilted(0.3f)));
        Check(steep.Count(FurnitureKind.StreetLamp) == 0 && steep.Count(FurnitureKind.AtlasPost) == 0, "poles on a 30% grade");
    }

    [TestCase]
    public void CollidersFollowThePartsWithTheirFootMaterial()
    {
        foreach (var item in Build(World.Value).Items)
        {
            Check(item.Colliders.All(c => c.Foot != FootMat.Auto), $"{item.Key} leaves a collider on Auto (G21i)");
            foreach (var collider in item.Colliders)
            {
                var at = collider switch { PostCollider post => post.C, BoxCollider box => new Vector2(box.At.M41, box.At.M43), _ => throw new InvalidOperationException() };
                Check(Vector2.Distance(at, item.Anchor) < 2f, $"{item.Key}: a collider strayed {Vector2.Distance(at, item.Anchor):F2} m");
            }
            var expected = item.Kind switch
            {
                FurnitureKind.StreetLamp or FurnitureKind.AtlasPost => 1,
                FurnitureKind.BusStop => 2,
                FurnitureKind.Bench => 3,
                FurnitureKind.AtlasKiosk => 4,
                FurnitureKind.Bin => 2,
                FurnitureKind.OpenSign => 1,
                FurnitureKind.EntranceMat or FurnitureKind.EntranceLamp => 0,
                _ => throw new InvalidOperationException(),
            };
            Check(item.Colliders.Count == expected, $"{item.Key}: {item.Colliders.Count} colliders");
        }
    }

    [TestCase]
    public void AnchorsAndEntrancesFaceTheirData()
    {
        var world = World.Value;
        var layout = Build(world);
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "anchor"))
        {
            if (layout.Items.SingleOrDefault(i => i.Key == "anchor:" + feature.Id.Value) is not { } item)
            {
                Check(layout.Log.Any(l => l.StartsWith($"anchor {feature.Id.Value} ", StringComparison.Ordinal) && l.Contains("not placed", StringComparison.Ordinal)), $"{feature.Id.Value} missing without a log line");
                continue;
            }
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            // On its footprint, or moved to the paving edge nearby (AnchorsStandOnOrBesidePaving).
            Check(Vector2.Distance(centre, item.Anchor) < FurnitureStyle.AnchorPavingReach + 0.1f, $"{item.Key} strayed {Vector2.Distance(centre, item.Anchor):F2} m from its footprint");
            var yaw = feature.Props!.Value.GetProperty("yawDeg").GetSingle();
            Check(Vector2.Dot(Front(item.Part.At), ExpectedFacing(layout, feature)) > 0.999f, $"{item.Key} does not face {yaw}° (or the yaw its log line gives)");
        }
        foreach (var door in world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open))
        {
            var outward = FurniturePlanner.Facing(door.YawDeg);
            var mat = layout.Items.Single(i => i.Key == $"door:{door.Id}:mat");
            var lamp = layout.Items.Single(i => i.Key == $"door:{door.Id}:lamp");
            Check(Vector2.Dot(mat.Anchor - new Vector2(door.Center.X, door.Center.Z), outward) > 0.4f, $"{door.Id}: mat not in front of the door");
            // On the entrance floor, never below the paving (flat terrain: 0.15 m).
            var floor = MathF.Max(door.Center.Y, FurnitureStyle.PavingLift);
            Check(MathF.Abs(mat.Part.At.M42 - floor) < 1e-4f && lamp.Part.At.M42 > floor + 2f, $"{door.Id}: kit off the entrance floor");
            Check(Vector2.Dot(Front(lamp.Part.At), outward) > 0.999f && lamp.Emitters.Single().Channel == StreetStyle.FirstGroup, $"{door.Id}: lamp");
        }
        // A building whose props another package claims keeps its own kit.
        var claimed = world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).First(e => e.Open);
        var owner = world.Features.Keys.First(f => world.Interiors.Get(f)?.Entrances.Any(e => e.Id == claimed.Id) == true);
        var building = world.Interiors.Get(owner)!.Feature;
        var skipped = Build(world, (f, a) => f == building && a == Aspects.Props ? new WpId("WP-18") : null);
        Check(!skipped.Items.Any(i => i.Key.StartsWith($"door:{claimed.Id}:", StringComparison.Ordinal)), "kit placed on a claimed building");
        Check(skipped.Count(FurnitureKind.EntranceMat) == layout.Count(FurnitureKind.EntranceMat) - world.Interiors.Get(owner)!.Entrances.Count(e => e.Open), "claim skipped other kits");
    }

    [TestCase]
    public void ModuleTilesCarryEveryItemOnceWithinBudget()
    {
        var ctx = Context(Inputs());
        var module = new FurnitureModule();
        var layout = FurnitureModule.Layout(ctx);
        var tiles = module.Tiles(ctx).ToList();
        var parts = 0; var emitters = 0; var colliders = 0;
        var heaviest = 0; var heaviestL1 = 0; var draws = 0;
        var triangles = new Dictionary<(string, object), (int L0, int L1, int Meshes)>();
        foreach (var tile in tiles)
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            parts += output.Parts.Count;
            emitters += output.Emitters?.Count ?? 0;
            colliders += module.Collide(ctx, tile).Count;
            var l0 = 0; var l1 = 0;
            foreach (var part in output.Parts)
            {
                var key = (part.Part.Value, part.Params);
                if (!triangles.TryGetValue(key, out var t)) triangles[key] = t = Measure(part);
                l0 += t.L0; l1 += t.L1;
            }
            l0 += output.Meshes.Sum(m => m.Index.Length / 3);
            heaviest = Math.Max(heaviest, l0);
            heaviestL1 = Math.Max(heaviestL1, l1);
            draws = Math.Max(draws, output.Parts.Select(p => (p.Part.Value, p.Params)).Distinct().Sum(k => triangles[k].Meshes) + output.Meshes.Count);
            Check(module.Build(ctx, tile, LodLevel.L1).Emitters is null, "emitters outside L0");
        }
        Check(parts == layout.Items.Count, $"{parts} placed parts for {layout.Items.Count} items");
        Check(emitters == layout.Items.Sum(i => i.Emitters.Count), "emitters lost or repeated");
        Check(colliders == layout.Items.Sum(i => i.Colliders.Count), "colliders lost or repeated");
        Check(heaviest <= module.Budget.TrianglesL0 && heaviestL1 <= module.Budget.TrianglesL1 && draws <= module.Budget.DrawCalls,
            $"heaviest cell L0 {heaviest}, L1 {heaviestL1}, draws {draws} over {module.Budget}");
        Console.WriteLine($"furniture module: {tiles.Count} cells, {parts} parts, {emitters} lamp markers, {colliders} colliders; heaviest cell L0 {heaviest} / L1 {heaviestL1} triangles, {draws} draws");
    }

    [TestCase]
    public void TheFallbackWarningTravelsWithTheFirstCellOnly()
    {
        // Structured diagnostics (ContentOutput.Diagnostics) are reported once per layout: with the first cell's L0 output.
        var module = new FurnitureModule();
        var quiet = Context(Inputs());
        Check(module.Tiles(quiet).All(t => module.Build(quiet, t, LodLevel.L0).Diagnostics.Count == 0), "warnings with the real planting input");
        var ctx = Context(new Dictionary<string, ReadOnlyMemory<byte>>());
        var tiles = module.Tiles(ctx).ToList();
        var warned = tiles.Where(t => module.Build(ctx, t, LodLevel.L0).Diagnostics.Count > 0).ToList();
        Check(warned.Count == 1 && warned[0] == tiles[0], $"{warned.Count} cells carry the warning");
        var diagnostics = module.Build(ctx, tiles[0], LodLevel.L0).Diagnostics;
        Check(diagnostics.Count == 1 && diagnostics[0].Code == PlantingRows.MissingCode && diagnostics[0].Level == DiagnosticLevel.Warning, "not one missing-input warning");
        Check(tiles.All(t => module.Build(ctx, t, LodLevel.L1).Diagnostics.Count == 0), "warnings outside L0");
    }

    private static BuildContext Context(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> inputs)
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        var map = new MapDescriptor(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero,
            new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f),
            new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { World = World.Value, Map = map, WorldSeed = 1u, SourceInputs = inputs };
    }

    private static (int L0, int L1, int Meshes) Measure(PlacedPart part)
    {
        var type = typeof(StreetGallery).Assembly.GetTypes().Single(t => t.GetCustomAttributes(typeof(PartAttribute), false).OfType<PartAttribute>().Any(a => a.Id == part.Part.Value));
        var def = Activator.CreateInstance(type)!;
        var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(def)!;
        PartGeometry? Level(LodLevel l) => lods.Contains(l) ? (PartGeometry)((Delegate)lods[l]!).DynamicInvoke(part.Params)! : null;
        var l0 = Level(LodLevel.L0)!;
        return (l0.Meshes.Sum(m => m.Index.Length / 3), Level(LodLevel.L1)?.Meshes.Sum(m => m.Index.Length / 3) ?? 0, l0.Meshes.Count);
    }

    /// <summary>
    /// Every kiosk and post anchor of the world in ID order, with its footprint centre and what that point lands on: the data the
    /// planner places by, so the tests' expectations follow the anchor data.
    /// </summary>
    private static List<(WorldFeature Feature, string Form, Vector2 Centre, Landing Landing)> AnchorLandings(IWorldModel world)
    {
        var site = new StreetSite(world);
        var found = new List<(WorldFeature, string, Vector2, Landing)>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            if (feature.Props is not { ValueKind: JsonValueKind.Object } props || !props.TryGetProperty("form", out var value) || value.ValueKind != JsonValueKind.String) continue;
            var form = value.GetString();
            if (form is not ("kiosk" or "post")) continue;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            found.Add((feature, form, centre, site.LandingAt(centre)));
        }
        return found;
    }

    private static string Where(Landing landing) => landing switch
    {
        Landing.Carriageway => "on a carriageway",
        Landing.Water => "in water",
        Landing.Building => "in a building",
        _ => throw new ArgumentOutOfRangeException(nameof(landing), landing, "Open ground is not a fallback."),
    };

    private static Vector2 Front(Matrix4x4 at) => Vector2.Normalize(new(-at.M31, -at.M33));

    private static string? Highway(NetworkLine line) => RoadType(line.Props);

    private static string? RoadType(JsonElement? props) =>
        props is { ValueKind: JsonValueKind.Object } p && (p.TryGetProperty("roadType", out var t) || p.TryGetProperty("highway", out t)) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;

    private static bool Carriageway(NetworkLine line) => Highway(line) is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";

    private static float Half(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    private static Vector2 Nearest(Vector2 p, NetworkLine line) =>
        line.Line.Points.Zip(line.Line.Points.Skip(1), (u, v) =>
        {
            var d = v - u;
            var t = d.LengthSquared() < 1e-8f ? 0f : Math.Clamp(Vector2.Dot(p - u, d) / d.LengthSquared(), 0f, 1f);
            return u + d * t;
        }).MinBy(q => Vector2.DistanceSquared(p, q));

    private static float Distance(Vector2 p, NetworkLine line) => Vector2.Distance(p, Nearest(p, line));

    private static IEnumerable<Vector2> Corners(FurnitureItem item)
    {
        var p = (FurnitureParams)item.Part.Params;
        for (var i = -1; i <= 1; i += 2)
            for (var k = -1; k <= 1; k += 2)
            {
                var q = Xf.Point(item.Part.At, new(i * p.Width * 0.5f, 0, k * p.Depth * 0.5f));
                yield return new(q.X, q.Z);
            }
    }

    private static IEnumerable<PolygonRings> Polygons(WorldFeature feature) =>
        (feature.Parts.Count == 0 ? [feature.Geometry] : feature.Parts.Where(p => (p.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var k) ? k.GetString() : feature.Kind.Value) == feature.Kind.Value).Select(p => p.Geometry))
        .SelectMany(g => g switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => Array.Empty<PolygonRings>() });

    /// <summary>A uniform tilt along x with a constant grade.</summary>
    private sealed class Tilted(float grade) : ITerrain
    {
        public bool IsFlat(TileKey tile) => false;
        public float HeightAt(float x, float z) => grade * x;
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(grade, TerrainClass.Slope, 0f, 0f);
    }

    /// <summary>The world with one feature replaced.</summary>
    private sealed class FeatureWorld(IWorldModel inner, WorldFeature feature) : IWorldModel
    {
        private readonly Dictionary<FeatureId, WorldFeature> _features = new(inner.Features) { [feature.Id] = feature };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => _features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    /// <summary>The world with a walk filed the way the effective network files a design walk: as a network line, as a feature, or both (null leaves that stack alone).</summary>
    private sealed class WalkWorld(IWorldModel inner, NetworkLine? line, WorldFeature? feature) : IWorldModel
    {
        private readonly Dictionary<FeatureId, WorldFeature> _features = feature is null ? new(inner.Features) : new(inner.Features) { [feature.Id] = feature };
        private readonly WorldNetwork _network = line is null ? inner.Network : inner.Network with { Roads = [.. inner.Network.Roads, line] };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => _features;
        public WorldNetwork Network => _network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    private sealed class TerrainWorld(IWorldModel inner, ITerrain terrain) : IWorldModel
    {
        public ITerrain Terrain => terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => inner.Features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
