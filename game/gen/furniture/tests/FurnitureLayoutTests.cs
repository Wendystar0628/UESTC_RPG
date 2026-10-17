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
        foreach (var overlay in Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Order(StringComparer.Ordinal))
            files[Path.GetRelativePath(data, overlay).Replace('\\', '/')] = File.ReadAllText(overlay);
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
        // A bin at every other rest stop along a road; node benches (on the plazas) come without.
        var rest = a.Items.Count(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:", StringComparison.Ordinal));
        // The design walks (0h) reach the layout through WP-08's effective network: say how many benches line one.
        var site = new StreetSite(World.Value);
        var onDesign = a.Items.Where(i => i.Kind == FurnitureKind.Bench && site.PavingEdgeFacing(i.Anchor + Front(i.Part.At) * (((FurnitureParams)i.Part.Params).Depth * 0.5f + 0.4f)) is { Road: { } w } && w.Id.Value.StartsWith("design:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"benches facing a design walk: {onDesign.Count} " + string.Join(", ", onDesign.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")));
        Check(a.Count(FurnitureKind.Bin) * 2 >= rest - 2 && a.Count(FurnitureKind.Bin) <= rest, "a bin at every other rest stop");
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
        // An anchor whose point lands on a carriageway, in water or in a building gets nothing and one structured diagnostic. Judged from the world data (StreetSite.LandingAt), not by anchor ID: moving an anchor needs no change here.
        var world = World.Value;
        var layout = Build(world);
        Console.WriteLine("furniture diagnostics: " + string.Join(" | ", layout.Diagnostics.Select(d => d.Message)));
        var landings = AnchorLandings(world);
        foreach (var (feature, form, _, landing) in landings)
        {
            var id = feature.Id.Value;
            var placed = layout.Items.Any(i => i.Key == "anchor:" + id);
            var left = layout.Diagnostics.Where(d => d.Feature == feature.Id && d.Code == "wp13.anchor.not-placed").ToList();
            if (landing == Landing.Open)
            {
                Check(placed, $"{id} lands on open ground but is not placed");
                Check(left.Count == 0, $"{id} is placed but logged as not placed");
                continue;
            }
            Check(!placed, $"{id} placed, though its point lands {Where(landing)}");
            Check(left.Count == 1 && left[0].Level == DiagnosticLevel.Warning && left[0].Message == $"anchor {id} ({form}) not placed: its point lands {Where(landing)}", $"{id} not logged once as {Where(landing)}");
        }
        Check(layout.Diagnostics.Count(d => d.Code == "wp13.anchor.not-placed") == landings.Count(l => l.Landing != Landing.Open), "a diagnostic message without a left-out anchor");
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
        Check(indoors.Diagnostics.Any(d => d.Feature == anchor.Feature.Id && d.Code == "wp13.anchor.not-placed" && d.Level == DiagnosticLevel.Warning), "the building fallback is not logged");
        Check(indoors.Items.Count == layout.Items.Count - 1, "the building fallback changed other furniture");
    }

    [TestCase]
    public void AnchorsStandOnOrBesidePaving()
    {
        // Opus review 12:15: a kiosk's apron or a post's foot touches pedestrian paving (≤ 0.5 m off it); the facing never changes.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
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
            // Opus review 17:28 (A03): a kiosk's apron stands whole on the paving, not half over its edge.
            if (item.Kind != FurnitureKind.AtlasKiosk) continue;
            var outside = 0; var samples = 0;
            for (var u = -hw; u <= hw + 1e-3f; u += 0.25f)
                for (var v = -hd; v <= hd + 1e-3f; v += 0.25f)
                {
                    if (MathF.Abs(u) < hw - 1e-3f && MathF.Abs(v) < hd - 1e-3f) continue;
                    var q = Xf.Point(item.Part.At, new(u, 0, v));
                    samples++;
                    if (site.PavingEdgeFacing(new(q.X, q.Z)) is not { Gap: <= 0f }) outside++;
                }
            Check(outside == 0, $"{item.Key}: {outside}/{samples} outline points of its apron off the paving");
        }
    }

    [TestCase]
    public void TreeRowsComeFromThePlantingInputAndFallBackWithOneWarning()
    {
        // The rows are read from the map's frozen planting rules, not copied: the real file gives the avenue's rows and its band,
        // and one row on each secondary road, with nothing to warn about.
        var (roads, diagnostics) = PlantingRows.Resolve(Inputs());
        Check(diagnostics.Count == 0, "warnings on the real planting file: " + string.Join(" | ", diagnostics.Select(d => d.Message)));
        Check(roads.Select(r => r.Name).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Name)) && roads.Select(r => r.Tier).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Tier)), "the roads or their tiers changed");
        // Expected offsets are parsed from the same file here (WP-23 owns the values and may change them): every row, then the band.
        var file = JsonNode.Parse(PlantingBytes.Value)!["roads"]!.AsArray();
        float[] Offsets(int i) => [.. file[i]!["rows"]!.AsArray().Select(r => r!["offset"]!.GetValue<float>()),
            .. file[i]!["band"] is JsonNode band ? [band["offset"]!.GetValue<float>()] : Array.Empty<float>()];
        Check(Enumerable.Range(0, 3).All(i => roads[i].Rows.SequenceEqual(Offsets(i))) && roads[0].Rows.Count >= 2, "rows differ from the planting file");
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
            Check(layout.Diagnostics.Where(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal)).SequenceEqual(warnings) && layout.Items.Count == Build(World.Value).Items.Count, "the fallback layout differs from the real one, whose rows are the same");
        }
        Check(!Build(World.Value).Diagnostics.Any(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal)), "the real layout warns");
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
        // Signposts, fountains and the service things (13c2c) come last and keep off the rows and what stands before them: they may move too.
        foreach (var item in before.Items.Where(i => !i.Key.StartsWith("rest:0:", StringComparison.Ordinal) && !i.Key.StartsWith("lamp:0:", StringComparison.Ordinal)
            && i.Kind is not (FurnitureKind.Signpost or FurnitureKind.DrinkingFountain or FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker
                or FurnitureKind.BicycleBay or FurnitureKind.ParkedBicycle)))
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
        Console.WriteLine($"node benches at {spot}: " + string.Join(", ", benches.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")) + "; diagnostics: " + string.Join(" | ", layout.Diagnostics.Select(d => d.Message).Where(l => l.Contains("node", StringComparison.Ordinal))));
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches, $"{benches.Count} benches at the node");
        Check(!layout.Diagnostics.Any(d => d.Code == "wp13.node.no-bench" && d.Message.Contains("test node", StringComparison.Ordinal)), "a node with benches logs 'no bench'");
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
        // or looks at it); its two nodes take benches under the same rules or report why none fits, never by relaxing them.
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
            var says = layout.Diagnostics.Where(d => d.Code == "wp13.node.no-bench" && d.Message.StartsWith($"road {rule.Name} node {node.Label} ", StringComparison.Ordinal)).Select(d => d.Message).ToList();
            Console.WriteLine($"{rule.Name} node {node.Label}: {placed} benches" + (says.Count > 0 ? "; " + says[0] : ""));
            Check(placed > 0 ? says.Count == 0 : says.Count == 1 && says[0].Length > 60, $"node {node.Label}: {placed} benches and {says.Count} 'no bench' lines");
        }
        // Opus review 17:28: a node bench may stand on the plaza, its back to the paving edge and parallel to it, facing into the
        // plaza, wholly on its paving and off the walks' strips, 3 m off carriageways (judged in general on a made-up plaza below).
        var site = new StreetSite(world);
        var standing = layout.Items.Where(i => i.Key.StartsWith($"node:{index}:", StringComparison.Ordinal) && i.Pad is null).ToList();
        var plaza = standing.Where(b => site.OnPlaza(b.Anchor)).ToList();
        var walk = standing.Except(plaza).ToList();
        Console.WriteLine($"{plaza.Count} benches on the osmanthus road's plazas, {walk.Count} at the edges of its wide walks");
        CheckPlazaBenches(site, plaza);
        // Opus review 13c2b 21:05: or on a walk 4 m or wider, at its edge, back to it, 2.5 m left to pass.
        CheckWalkEdgeBenches(world, walk);
    }

    [TestCase]
    public void ANodeOnAPlazaSeatsItsBenchesOnItWithTheirBacksToItsEdge()
    {
        // Opus review 17:28, judged on a made-up 12 × 10 m pedestrian square on open ground with a rest node at its middle: the
        // benches stand on the square (no bay on flat ground), backs to its edge, facing in; a square that is not there takes none.
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        Vector2[] ring = [spot + new Vector2(-6f, -5f), spot + new Vector2(6f, -5f), spot + new Vector2(6f, 5f), spot + new Vector2(-6f, 5f)];
        var props = JsonSerializer.SerializeToElement(new { roadType = "pedestrian" });
        var square = new WorldFeature(new("design:furniture-test-square"), FeatureSource.Design, new("road"), new PolygonGeometry(new PolygonRings(ring, []))) { Props = props };
        IWorldModel world = new TreedWorld(new WalkWorld(open, null, square), SquareTrees(spot));
        var layout = FurniturePlanner.Build(world, null, Inputs(), NodeRules(spot));
        var benches = layout.Items.Where(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"square at {spot}: " + string.Join(", ", benches.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")));
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches && benches.All(b => b.Pad is null), $"{benches.Count} benches on the square, or on bays");
        CheckPlazaBenches(new StreetSite(world), benches);
        var bare = FurniturePlanner.Build(open, null, Inputs(), NodeRules(spot));
        Check(!bare.Items.Any(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)), "benches at a node with no square");
    }

    /// <summary>
    /// Walk-edge node benches (Opus review 13c2b 21:05): on a walk at least 4 m wide, the whole footprint on its strip, the back
    /// <see cref="FurnitureStyle.WalkBenchClear"/> inside its edge and facing across it, at least <see cref="FurnitureStyle.NodeWalkPass"/>
    /// of the walk left beyond the sitters' feet; 3 m off carriageways and never facing one within 12 m.
    /// </summary>
    private static void CheckWalkEdgeBenches(IWorldModel world, IEnumerable<FurnitureItem> benches)
    {
        var walks = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian").ToList();
        var site = new StreetSite(world);
        foreach (var bench in benches)
        {
            var p = (FurnitureParams)bench.Part.Params;
            var facing = Front(bench.Part.At);
            var right = new Vector2(-facing.Y, facing.X);
            var walk = walks.Where(w => Distance(bench.Anchor, w) <= Half(w)).MinBy(w => Distance(bench.Anchor, w))
                ?? throw new InvalidOperationException($"{bench.Key} does not stand on a walk");
            Check(2f * Half(walk) >= FurnitureStyle.NodeWalkMin, $"{bench.Key} stands on a walk {2f * Half(walk):F1} m wide");
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
                foreach (var v in new[] { -p.Depth * 0.5f, p.Depth * 0.5f + FurnitureStyle.FeetReach })
                    Check(Distance(bench.Anchor + right * u + facing * v, walk) <= Half(walk) + 1e-3f, $"{bench.Key} leaves its walk at ({u:F2}, {v:F2})");
            var back = bench.Anchor - facing * (p.Depth * 0.5f);
            Check(MathF.Abs(Half(walk) - Distance(back, walk) - FurnitureStyle.WalkBenchClear) < 0.05f, $"{bench.Key} has its back {Half(walk) - Distance(back, walk):F2} m inside the walk's edge");
            var toLine = Vector2.Normalize(Nearest(back, walk) - back);
            Check(Vector2.Dot(toLine, facing) > 0.97f, $"{bench.Key} does not face across its walk");
            var feet = bench.Anchor + facing * (p.Depth * 0.5f + FurnitureStyle.FeetReach);
            var across = Vector2.Dot(feet - Nearest(feet, walk), facing);
            Check(Half(walk) - across >= FurnitureStyle.NodeWalkPass - 1e-3f, $"{bench.Key} leaves {Half(walk) - across:F2} m to pass");
            Check(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f), $"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway");
            Check(!site.CarriagewayAhead(bench.Anchor + facing * (p.Depth * 0.5f), facing, FurnitureStyle.FaceClear), $"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m");
            Console.WriteLine($"{bench.Key} at the edge of {walk.Id.Value} ({2f * Half(walk):F1} m) at ({bench.Anchor.X:F1}, {bench.Anchor.Y:F1}), {Half(walk) - across:F2} m to pass");
        }
    }

    [TestCase]
    public void ANodeOnAWideWalkSeatsItsBenchesAtTheWalksEdge()
    {
        // Opus review 13c2b 21:05, judged on made-up walks on open ground with a rest node on them: a 5 m walk seats the node's benches on
        // itself at its edge (backs to the edge, 2.5 m left to pass); a 3.5 m walk takes none on itself (they stand beside it, 13c1).
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        foreach (var (width, takes) in new[] { (5f, true), (3.5f, false) })
        {
            var (line, feature) = DesignWalk(spot, width);
            var world = new WalkWorld(open, line, feature);
            var layout = FurniturePlanner.Build(world, null, Inputs(), NodeRules(spot));
            var benches = layout.Items.Where(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)).ToList();
            var onWalk = benches.Where(b => Distance(b.Anchor, line) <= Half(line)).ToList();
            Console.WriteLine($"{width} m walk at {spot}: {benches.Count} node benches, {onWalk.Count} on the walk");
            if (!takes)
            {
                Check(onWalk.Count == 0, $"{onWalk.Count} benches on a {width} m walk");
                continue;
            }
            Check(onWalk.Count is >= 1 and <= FurnitureStyle.NodeBenches && onWalk.Count == benches.Count && benches.All(b => b.Pad is null),
                $"{onWalk.Count} of {benches.Count} node benches on the {width} m walk, or on bays");
            CheckWalkEdgeBenches(world, onWalk);
        }
    }

    [TestCase]
    public void SignpostsStandAtLandmarkJunctionsOneToALineOfSight()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): a signpost only by a junction of three or more walks with a landmark within
        // 50 m, beside the paving, facing it; no two within 80 m (one to a line of sight). Judged from the network, not by place names.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var signs = layout.Items.Where(i => i.Kind == FurnitureKind.Signpost).ToList();
        var junctions = FurniturePlanner.Junctions(site).Where(j => j.Branches >= FurnitureStyle.SignpostBranches).ToList();
        Console.WriteLine($"signposts: {signs.Count} " + string.Join(", ", signs.Select(s => $"{s.Key} ({s.Anchor.X:F1}, {s.Anchor.Y:F1})")) + "; " +
            layout.Diagnostics.Single(d => d.Code == "wp13.signposts.summary").Message);
        foreach (var sign in signs)
        {
            var junction = junctions.MinBy(j => Vector2.Distance(j.At, sign.Anchor));
            Check(Vector2.Distance(junction.At, sign.Anchor) <= FurnitureStyle.SignpostReach + 1f, $"{sign.Key} stands {Vector2.Distance(junction.At, sign.Anchor):F1} m from a junction of three walks");
            var facing = Front(sign.Part.At);
            Check(site.PavingEdgeFacing(sign.Anchor + facing * 0.6f) is { Gap: < 0.3f }, $"{sign.Key} does not face the paving");
            Check(sign.Pad is not null, $"{sign.Key} has no bay");
        }
        for (var a = 0; a < signs.Count; a++)
            for (var b = a + 1; b < signs.Count; b++)
                Check(Vector2.Distance(signs[a].Anchor, signs[b].Anchor) >= FurnitureStyle.SignpostSpacing - 2f * (FurnitureStyle.SignpostReach + 1f), $"{signs[a].Key} and {signs[b].Key} share a line of sight");
    }

    [TestCase]
    public void AJunctionOfThreeWalksByALandmarkTakesOneSignpost()
    {
        // Made up on open ground: three walks meeting at a point, a landmark 20 m off: one signpost at the junction. Without the landmark,
        // none; a second such junction 40 m away (inside the 80 m line of sight) adds none.
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        var props = JsonSerializer.SerializeToElement(new { roadType = "footway", width = 2.5f });
        NetworkLine Walk(string id, Vector2 a, Vector2 b) => new(new($"design:furniture-test-{id}"), new Polyline([a, b]), 2.5f) { Props = props };
        List<NetworkLine> Star(string tag, Vector2 at) => [Walk(tag + "-w", at, at + new Vector2(-14f, 0f)), Walk(tag + "-e", at, at + new Vector2(14f, 0f)), Walk(tag + "-n", at, at + new Vector2(0f, 14f))];
        var landmarkAt = spot + new Vector2(6f, -20f);
        var landmark = new WorldFeature(new("design:furniture-test-landmark"), FeatureSource.Design, new("poi"), new PointGeometry(new Vector3(landmarkAt.X, 0f, landmarkAt.Y)))
        { Props = JsonSerializer.SerializeToElement(new { category = "landscape", subcategory = "landmark" }), Bounds = new(new(landmarkAt.X, 0f, landmarkAt.Y), new(landmarkAt.X, 0f, landmarkAt.Y)) };
        int Signs(IWorldModel world) => FurniturePlanner.Build(world, null, Inputs()).Count(FurnitureKind.Signpost);
        var baseline = Signs(open);
        var one = Signs(new MadeWorld(open, Star("a", spot), [landmark]));
        var bare = Signs(new MadeWorld(open, Star("a", spot), []));
        var two = Signs(new MadeWorld(open, [.. Star("a", spot), .. Star("b", spot + new Vector2(0f, 40f))], [landmark]));
        Console.WriteLine($"made-up junction at {spot}: baseline {baseline}, with a landmark {one}, without {bare}, two junctions {two}");
        Check(one == baseline + 1, $"{one - baseline} signposts at a three-walk junction by a landmark");
        Check(bare == baseline, $"{bare - baseline} signposts at a junction with no landmark near");
        Check(two == baseline + 1, $"{two - baseline} signposts at two junctions 40 m apart");
    }

    [TestCase]
    public void FountainsStandOutsideTheMainEntrancesOfSportsAndTeachingBuildings()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): one drinking fountain a building, by the main entrance of a sports venue or a
        // teaching building (its first open entrance), 10–20 m in front of the door, beside the paving on a bay, facing it.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var fountains = layout.Items.Where(i => i.Kind == FurnitureKind.DrinkingFountain).ToList();
        Console.WriteLine($"fountains: {fountains.Count} " + string.Join(", ", fountains.Select(f => $"{f.Key} ({f.Anchor.X:F1}, {f.Anchor.Y:F1})")) + "; " +
            layout.Diagnostics.Single(d => d.Code == "wp13.fountains.summary").Message);
        Check(fountains.Count >= 1, "no drinking fountain on the map");
        var buildings = new HashSet<FeatureId>();
        foreach (var fountain in fountains)
        {
            var doorId = fountain.Key["fountain:".Length..];
            var entry = world.Features.Values.Select(f => world.Interiors.Get(f.Id)).First(e => e is not null && e.Entrances.Any(d => d.Id == doorId))!;
            var kinds = world.Features.Values.Where(f => world.Interiors.Get(f.Id)?.Feature == entry.Feature).Select(f => Text(f.Props, "category") ?? "").ToList();
            Check(kinds.Any(FurnitureStyle.FountainCategories.Contains), $"{fountain.Key} is not by a sports or teaching building ({string.Join(", ", kinds)})");
            Check(buildings.Add(entry.Feature), $"{fountain.Key}: a second fountain for one building");
            var door = entry.Entrances.Where(d => d.Open).OrderBy(d => d.Id, StringComparer.Ordinal).First();
            Check(door.Id == doorId, $"{fountain.Key} is not by the building's main entrance");
            var plan = new Vector2(door.Center.X, door.Center.Z);
            var away = fountain.Anchor - plan;
            Check(away.Length() is >= FurnitureStyle.FountainNear - 1.5f and <= FurnitureStyle.FountainFar + 1.5f, $"{fountain.Key} stands {away.Length():F1} m from its door");
            Check(Vector2.Dot(away, FurniturePlanner.Facing(door.YawDeg)) > 0f, $"{fountain.Key} stands behind its door");
            Check(fountain.Pad is not null && site.PavingEdgeFacing(fountain.Anchor + Front(fountain.Part.At) * 0.6f) is { Gap: < 0.3f }, $"{fountain.Key} is not on a bay facing the paving");
        }
    }

    [TestCase]
    public void VendingLockersAndBaysStandWhereTheyAreUsedAndFew()
    {
        // 13c2c: vending groups of 2–3 at the cluster gates, canteens and sports venues, one to a line of sight and none on the main
        // axis; at most two lockers, each beside a gate's group; bicycle bays apart, their stand-in bikes inside the frame.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        foreach (var line in layout.Diagnostics.Where(d => d.Code is "wp13.vending.summary" or "wp13.lockers.summary" or "wp13.bicycles.summary").Select(d => d.Message))
            Console.WriteLine(line);
        bool Serves(FurnitureItem item, float front)
        {
            var q = item.Anchor + Front(item.Part.At) * (((BoxPartParams)item.Part.Params).Depth * 0.5f + front + 0.3f);
            return site.PavingEdgeFacing(q) is { Gap: <= 0f } || !site.OffCarriageways(q, 0f, 0f);
        }
        var groups = layout.Items.Where(i => i.Kind == FurnitureKind.VendingMachine).GroupBy(i => i.Key[..i.Key.LastIndexOf(':')]).ToList();
        Check(groups.Count >= 3, $"{groups.Count} vending groups");
        var axis = world.Network.Roads.Where(l => StreetSite.RoadName(l) == FurnitureStyle.Roads.First(r => r.Tier == RoadTier.Main).Name).ToList();
        var centres = new List<(string Site, Vector2 At)>();
        foreach (var group in groups)
        {
            var machines = group.ToList();
            var facing = Front(machines[0].Part.At);
            Check(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax, $"{group.Key}: {machines.Count} machines");
            Check(machines.Count(m => ((EquipmentParams)m.Part.Params).Variation == 1) <= 1, $"{group.Key}: more than one odd machine");
            Check(machines.All(m => Vector2.Dot(Front(m.Part.At), facing) > 0.999f && MathF.Abs(m.Part.At.M42 - machines[0].Part.At.M42) < 1e-5f), $"{group.Key}: machines not in one row");
            for (var k = 1; k < machines.Count; k++)
                Check(MathF.Abs(Vector2.Distance(machines[k].Anchor, machines[k - 1].Anchor) - FurnitureStyle.Vending.Width - FurnitureStyle.VendingSideGap) < 1e-3f, $"{group.Key}: machines not side by side");
            Check(machines[0].Pad is { } bay && MathF.Abs(bay.Top - machines[0].Part.At.M42) < 1e-4f, $"{group.Key} is off its bay");
            Check(machines.All(m => Serves(m, FurnitureStyle.ServiceFront)), $"{group.Key} does not face the paving or the road");
            var centre = machines.Aggregate(Vector2.Zero, (a, m) => a + m.Anchor) / machines.Count;
            Check(axis.All(l => Distance(centre, l) >= FurnitureStyle.MainAxisClear - FurnitureStyle.ServiceReach - 2f), $"{group.Key} stands on the main axis");
            var siteId = group.Key["vending:".Length..];
            if (world.Features.TryGetValue(new(siteId), out var feature) && feature.Kind.Value == "clusterGate")
            {
                var cluster = world.Features[new(feature.Props!.Value.GetProperty("cluster").GetString()!)];
                var rings = Polygons(cluster).Single();
                foreach (var m in machines)
                    foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                        Check(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, rings.Outer) >= FurnitureStyle.GateOutside - 1e-3f, $"{m.Key} is inside its cluster's fence line");
                var gate = (PointGeometry)feature.Geometry;
                Check(Vector2.Distance(centre, new(gate.Position.X, gate.Position.Z)) <= FurnitureStyle.ServiceReach + 2f, $"{group.Key} strays from its gate");
            }
            centres.Add((siteId, centre));
        }
        for (var a = 0; a < centres.Count; a++)
            for (var b = a + 1; b < centres.Count; b++)
                Check(Vector2.Distance(centres[a].At, centres[b].At) >= FurnitureStyle.VendingSpacing - 2f * (FurnitureStyle.ServiceReach + FurnitureStyle.BayNear), $"{centres[a].Site} and {centres[b].Site} share a line of sight");
        var lockers = layout.Items.Where(i => i.Kind == FurnitureKind.DeliveryLocker).ToList();
        Check(lockers.Count is >= 1 and <= FurnitureStyle.LockerSites, $"{lockers.Count} delivery lockers");
        foreach (var locker in lockers)
        {
            var at = centres.Single(c => c.Site == locker.Key["locker:".Length..]).At;
            Check(Vector2.Distance(at, locker.Anchor) <= 2f * FurnitureStyle.ServiceReach, $"{locker.Key} is not beside its gate's vending group");
            Check(Serves(locker, FurnitureStyle.ServiceFront) && locker.Pad is not null, $"{locker.Key} does not face the paving on its bay");
        }
        var bays = layout.Items.Where(i => i.Kind == FurnitureKind.BicycleBay).ToList();
        Check(bays.Count >= 1, "no bicycle bay");
        for (var a = 0; a < bays.Count; a++)
            for (var b = a + 1; b < bays.Count; b++)
                Check(Vector2.Distance(bays[a].Anchor, bays[b].Anchor) >= FurnitureStyle.BaySpacing - 2f * FurnitureStyle.BayFar, $"{bays[a].Key} and {bays[b].Key} crowd each other");
        foreach (var bay in bays)
        {
            Check(Serves(bay, FurnitureStyle.WalkGap) && bay.Pad is not null, $"{bay.Key} does not open onto the paving on its bay");
            var p = (RoofParams)bay.Part.Params;
            Matrix4x4.Invert(bay.Part.At, out var local);
            var bikes = layout.Items.Where(i => i.Key.StartsWith(bay.Key + ":bike:", StringComparison.Ordinal)).ToList();
            Check(bikes.Count <= FurnitureStyle.BayBikesMax, $"{bay.Key}: {bikes.Count} bikes");
            foreach (var bike in bikes)
            {
                var q = Vector3.Transform(new Vector3(bike.Part.At.M41, bike.Part.At.M42, bike.Part.At.M43), local);
                var bp = (VehicleParams)bike.Part.Params;
                Check(MathF.Abs(q.X) + bp.Width * 0.5f <= p.Width * 0.5f + 1e-3f && MathF.Abs(q.Z) + bp.Depth * 0.5f <= p.Depth * 0.5f + 1e-3f, $"{bike.Key} leaves its bay's frame");
                Check(Vector2.Dot(Front(bike.Part.At), Front(bay.Part.At)) < -0.999f && bike.Colliders.Count == 0, $"{bike.Key}: front wheel not to the back line, or a collider");
            }
        }
    }

    [TestCase]
    public void AClusterGateTakesAVendingGroupOutsideItsFence()
    {
        // Made up on open ground: a 5 m service road running north into a 40 × 36 m cluster, a closed gate where it crosses the outline.
        // The gate takes one vending group (and, as the first closed gate's, a locker) beside the road, outside the cluster, facing the road.
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        var road = new NetworkLine(new("design:furniture-test-drive"), new Polyline([spot + new Vector2(0f, 24f), spot + new Vector2(0f, -30f)]), 5f)
        { Props = JsonSerializer.SerializeToElement(new { highway = "service" }) };
        Vector2[] ring = [spot + new Vector2(-20f, -3f), spot + new Vector2(20f, -3f), spot + new Vector2(20f, -39f), spot + new Vector2(-20f, -39f)];
        var cluster = new WorldFeature(new("design:furniture-test-cluster"), FeatureSource.Design, new("poi"), new PolygonGeometry(new PolygonRings(ring, [])));
        var gateAt = spot + new Vector2(0f, -3f);
        var gate = new WorldFeature(new("design:furniture-test-gate"), FeatureSource.Design, new("clusterGate"), new PointGeometry(new Vector3(gateAt.X, 0f, gateAt.Y)))
        { Props = JsonSerializer.SerializeToElement(new { cluster = "design:furniture-test-cluster", open = false }) };
        var layout = Build(new MadeWorld(open, [road], [cluster, gate]));
        var machines = layout.Items.Where(i => i.Key.StartsWith("vending:design:furniture-test-gate:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"made-up gate at {gateAt}: " + string.Join(", ", machines.Select(m => $"{m.Key} ({m.Anchor.X:F1}, {m.Anchor.Y:F1})")) +
            $"; locker {layout.Items.Any(i => i.Key == "locker:design:furniture-test-gate")}");
        Check(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax, $"{machines.Count} machines at the made-up gate");
        var rings = new PolygonRings(ring, []);
        foreach (var m in machines)
        {
            foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                Check(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, ring) >= FurnitureStyle.GateOutside - 1e-3f, $"{m.Key} is inside the cluster");
            var facing = Front(m.Part.At);
            Check(MathF.Abs(facing.X) > 0.99f && Vector2.Dot(facing, new Vector2(spot.X - m.Anchor.X, 0f)) > 0f, $"{m.Key} does not face the road");
            Check(Distance(m.Anchor, road) - 2.5f >= FurnitureStyle.ServiceFront + FurnitureStyle.Vending.Depth * 0.5f - 0.05f, $"{m.Key} crowds the road");
        }
        Check(layout.Items.Any(i => i.Key == "locker:design:furniture-test-gate"), "the first closed gate's group has no locker beside it");
    }

    /// <summary>Plaza benches: on the plaza's own paving with their feet, backs to its edge, 3 m off carriageways, never facing one.</summary>
    private static void CheckPlazaBenches(StreetSite site, IEnumerable<FurnitureItem> plaza)
    {
        foreach (var bench in plaza)
        {
            var facing = Front(bench.Part.At);
            var right = new Vector2(-facing.Y, facing.X);
            var p = (FurnitureParams)bench.Part.Params;
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
                foreach (var v in new[] { -p.Depth * 0.5f, p.Depth * 0.5f + FurnitureStyle.FeetReach })
                    Check(site.OnPlaza(bench.Anchor + right * u + facing * v), $"{bench.Key} leaves the plaza at ({u:F2}, {v:F2})");
            var behind = site.PavingEdgeFacing(bench.Anchor - facing * (p.Depth * 0.5f + FurnitureStyle.PlazaBenchClear + 0.2f));
            Check(behind is { Gap: > 0f } edge && Vector2.Dot(edge.Inward, facing) > 0.95f, $"{bench.Key} does not have its back to the plaza's edge");
            Check(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f), $"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway");
            Check(!site.CarriagewayAhead(bench.Anchor + facing * (p.Depth * 0.5f), facing, FurnitureStyle.FaceClear), $"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m");
            // Opus review 13c2b rework: at least 2.5 m of paving in front of the sitters' feet, and never across a walk line.
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
            {
                var ahead = bench.Anchor + right * u + facing * (p.Depth * 0.5f + FurnitureStyle.FeetReach + FurnitureStyle.PlazaPass);
                Check(site.OnPavedGround(ahead), $"{bench.Key} has less than {FurnitureStyle.PlazaPass} m of paving in front at {u:F2}");
            }
            Check(!site.NearWalkLine([bench.Anchor], p.Depth * 0.5f), $"{bench.Key} sits across a walk line");
            Console.WriteLine($"{bench.Key} on the plaza at ({bench.Anchor.X:F1}, {bench.Anchor.Y:F1}), facing ({facing.X:F2}, {facing.Y:F2})");
        }
    }

    [TestCase]
    public void APlazaCarryingAWalkLineStillSeatsItsNodeBenches()
    {
        // Opus review 13c2b rework, on the made-up 12 × 10 m square with a 2.5 m footway running through its middle: the walk line does
        // not count against the benches (they stand on the square, backs to its edge, 2.5 m of paving in front), but none sits across it.
        var open = new TerrainWorld(World.Value, FlatTerrain.Default);
        var spot = OpenGround(open);
        Vector2[] ring = [spot + new Vector2(-6f, -5f), spot + new Vector2(6f, -5f), spot + new Vector2(6f, 5f), spot + new Vector2(-6f, 5f)];
        var square = new WorldFeature(new("design:furniture-test-square"), FeatureSource.Design, new("road"), new PolygonGeometry(new PolygonRings(ring, [])))
        { Props = JsonSerializer.SerializeToElement(new { roadType = "pedestrian" }) };
        var walkProps = JsonSerializer.SerializeToElement(new { roadType = "footway", width = 2.5f });
        var walk = new NetworkLine(new("design:furniture-test-through"), new Polyline([spot + new Vector2(-14f, 0f), spot + new Vector2(14f, 0f)]), 2.5f) { Props = walkProps };
        IWorldModel world = new TreedWorld(new MadeWorld(open, [walk], [square]), SquareTrees(spot));
        var layout = FurniturePlanner.Build(world, null, Inputs(), NodeRules(spot));
        var benches = layout.Items.Where(i => i.Key.StartsWith("node:0:0:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"square with a walk through it at {spot}: " + string.Join(", ", benches.Select(b => $"{b.Key} ({b.Anchor.X:F1}, {b.Anchor.Y:F1})")));
        var site = new StreetSite(world);
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches && benches.All(b => site.OnPlaza(b.Anchor)), $"{benches.Count} benches on the square");
        CheckPlazaBenches(site, benches);
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
        Check(Benches(none).Count == 0 && none.Diagnostics.Any(d => d.Code == "wp13.node.no-bench" && d.Message.Contains("open node", StringComparison.Ordinal)), "a node with no walk has benches or no diagnostic message");
        Check(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches, $"{benches.Count} benches at the node of a walk filed twice");
        Check(benches.Count < 2 || Vector2.Distance(benches[0].Anchor, benches[1].Anchor) >= FurnitureStyle.NodeBenchGap - 1e-3f, "the walk's node benches stand closer than the gap");
        Check(both.Items.Count == none.Items.Count + benches.Count && !both.Diagnostics.Any(d => d.Code == "wp13.node.no-bench" && d.Message.Contains("open node", StringComparison.Ordinal)), "the walk changed more than its node's benches");
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
        Check(hidden.Items.Count == none.Items.Count && Benches(hidden).Count == 0 && hidden.Diagnostics.SequenceEqual(none.Diagnostics), "a hidden design walk still gets furniture");
    }

    /// <summary>The avenue's rule with one rest node at <paramref name="spot"/> (the others as they are): the node's benches are keyed <c>node:0:0:</c>.</summary>
    private static IReadOnlyList<RoadRule> NodeRules(Vector2 spot) =>
        [FurnitureStyle.Roads[0] with { Nodes = [new RestNode("open node", spot)] }, FurnitureStyle.Roads[1], FurnitureStyle.Roads[2]];

    /// <summary>A 16 m east–west design walk centred on <paramref name="spot"/>, as the network line and as the feature of one ID.</summary>
    private static (NetworkLine Line, WorldFeature Feature) DesignWalk(Vector2 spot, float width = 2.5f)
    {
        var id = new FeatureId("design:furniture-test-walk");
        var points = new Polyline([spot + new Vector2(-8f, 0f), spot + new Vector2(8f, 0f)]);
        var props = JsonSerializer.SerializeToElement(new { roadType = "footway", width });
        return (new NetworkLine(id, points, width) { Props = props }, new WorldFeature(id, FeatureSource.Design, new("road"), new PolylineGeometry(points)) { Props = props });
    }

    /// <summary>The real world with an overlay that adds the made-up design walk, and takes it away again when <paramref name="hide"/>.</summary>
    private static IWorldModel Loaded(Vector2 spot, bool hide)
    {
        var data = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var overlay in Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Order(StringComparer.Ordinal))
            files[Path.GetRelativePath(data, overlay).Replace('\\', '/')] = File.ReadAllText(overlay);
        var (line, feature) = DesignWalk(spot);
        var ops = new List<OverlayOp> { new AddOp(feature.Id, feature.Kind, feature.Geometry, null, feature.Props, "furniture test walk") };
        if (hide) ops.Add(new HideOp(feature.Id, "furniture test: hidden again"));
        files["overlay/zz-furniture-test.d/0000-fixture.json"] = JsonSerializer.Serialize(new OverlayFile(new("furniture-test"), new("WP-13"), ops) { Map = new("qingshuihe") }, ContractJson.Options);
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
        // edge and its face towards the paving, and the layout diagnosticss the yaw (and centre) the anchor data should take over. Judged on
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
        var turned = Turned(layout, moved.Id.Value) ?? throw new InvalidOperationException("the kiosk off the paving is not logged as turned: " + string.Join(" | ", layout.Diagnostics.Select(d => d.Message)));
        var item = layout.Items.Single(i => i.Key == "anchor:" + moved.Id.Value);
        Console.WriteLine($"kiosk off paving: yaw {turned.Yaw}, centre {turned.Centre}; diagnostics: {layout.Diagnostics.Single(d => d.Feature == moved.Id && d.Code == "wp13.anchor.turned").Message}");
        var facing = Front(item.Part.At);
        Check(Vector2.Dot(facing, FurniturePlanner.Facing(turned.Yaw)) > 0.999f && Vector2.Distance(item.Anchor, turned.Centre) < 0.01f, "the diagnostic does not give the placement");
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
        var line = layout.Diagnostics.Where(d => d.Feature?.Value == id && d.Code == "wp13.anchor.turned").Select(d => d.Message).SingleOrDefault();
        if (line is null) return null;
        var m = System.Text.RegularExpressions.Regex.Match(line, @"yawDeg (-?\d+\.\d+) \(data -?\d+\.\d+\), centre \((-?\d+\.\d+), (-?\d+\.\d+)\) \(data");
        Check(m.Success, "unreadable diagnostic message: " + line);
        float F(int g) => float.Parse(m.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture);
        return (F(1), new(F(2), F(3)));
    }

    /// <summary>The facing an anchor has in the layout: its data's yaw, or the yaw its "turned onto paving" diagnostic message gives.</summary>
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
    public void CarsParkSparselyInTheLotsAndKerbRampsMeetTheWalks()
    {
        // WLD-LIFE §3.3: a few cars in each car park, whole inside it, noses to its longest edge; never more than the cap a lot.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var cars = layout.Items.Where(i => i.Kind == FurnitureKind.ParkedCar).ToList();
        Console.WriteLine($"cars {cars.Count} in {cars.Select(c => c.Key.Split(':')[1]).Distinct().Count()} of {site.ParkingLots.Count} lots; " +
            string.Join(", ", cars.Take(6).Select(c => $"{c.Key} ({c.Anchor.X:F1}, {c.Anchor.Y:F1})")));
        Check(cars.Count > 0, "no parked cars");
        foreach (var group in cars.GroupBy(c => c.Key.Split(':')[1]))
            Check(group.Count() <= FurnitureStyle.CarsPerLot, $"lot {group.Key} holds {group.Count()} cars");
        foreach (var car in cars)
        {
            var p = (VehicleParams)car.Part.Params;
            var lot = site.ParkingLots[int.Parse(car.Key.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture)];
            foreach (var (u, v) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var q = Xf.Point(car.Part.At, new(u * p.Width * 0.5f, 0, v * p.Depth * 0.5f));
                Check(PlanGeometry.Contains(lot, new(q.X, q.Z)), $"{car.Key} sticks out of its lot");
            }
            Check(car.Colliders.Count == 1 && car.Colliders[0].Foot == FootMat.Metal, $"{car.Key}: one Metal box (WLD-LIFE §5)");
            Check(StreetStyle.CarBodies.Any(c => p.Material.Lit.Value == c + ".lit"), $"{car.Key} is not in a car body colour");
        }
        // Kerb ramps on the osmanthus road where its walks reach the kerb: on the carriageway against the kerb, rising towards the walk.
        var index = FurnitureStyle.Roads.ToList().FindIndex(r => r.KerbRamps);
        var rule = FurnitureStyle.Roads[index];
        var ramps = layout.Items.Where(i => i.Kind == FurnitureKind.KerbRamp).ToList();
        var line = layout.Diagnostics.Single(d => d.Code == "wp13.kerb-ramps.summary" && d.Message.StartsWith($"road {rule.Name}: ", StringComparison.Ordinal)).Message;
        Console.WriteLine(line + "; " + string.Join(", ", ramps.Select(r => $"{r.Key} ({r.Anchor.X:F1}, {r.Anchor.Y:F1})")));
        Check(ramps.All(r => r.Key.StartsWith($"ramp:{index}:", StringComparison.Ordinal)), "a kerb ramp on another road");
        var road = world.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name && StreetSite.Carriageway(l)).ToList();
        foreach (var ramp in ramps)
        {
            var p = (RampParams)ramp.Part.Params;
            var back = ramp.Anchor - Front(ramp.Part.At) * (p.Depth * 0.5f);
            var kerb = road.Min(l => Distance(back, l) - StreetSite.HalfWidth(l));
            Check(MathF.Abs(kerb) < 0.05f, $"{ramp.Key}: its top edge is {kerb:F2} m off the kerb");
            Check(road.Min(l => Distance(ramp.Anchor + Front(ramp.Part.At) * 2f, l)) < road.Min(l => Distance(ramp.Anchor, l)), $"{ramp.Key} does not face the road");
        }
        for (var i = 0; i < ramps.Count; i++)
            for (var k = i + 1; k < ramps.Count; k++)
                Check(Vector2.Distance(ramps[i].Anchor, ramps[k].Anchor) >= FurnitureStyle.RampGap - 1e-3f, $"{ramps[i].Key} and {ramps[k].Key} crowd each other");
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
        Check(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - (i.Pad is null ? FurnitureStyle.PavingLift : top)) < 1e-5f),
            "benches off their bays (or, on a plaza, off its paving)");
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
                var at = collider switch
                {
                    PostCollider post => post.C,
                    BoxCollider box => new Vector2(box.At.M41, box.At.M43),
                    RampCollider ramp => new Vector2(ramp.P0.X + ramp.P2.X, ramp.P0.Z + ramp.P2.Z) * 0.5f,
                    _ => throw new InvalidOperationException(),
                };
                Check(Vector2.Distance(at, item.Anchor) < 2f, $"{item.Key}: a collider strayed {Vector2.Distance(at, item.Anchor):F2} m");
            }
            var expected = item.Kind switch
            {
                FurnitureKind.StreetLamp or FurnitureKind.AtlasPost => 1,
                FurnitureKind.BusStop => 2,
                // Seat and back, and its bay; a plaza bench stands on the paving itself.
                FurnitureKind.Bench => item.Pad is null ? 2 : 3,
                FurnitureKind.AtlasKiosk => 4,
                FurnitureKind.Bin => 2,
                FurnitureKind.OpenSign or FurnitureKind.ParkedCar or FurnitureKind.KerbRamp => 1,
                // Two posts and the boards, and the bay; the fountain's box and its bay.
                FurnitureKind.Signpost => item.Pad is null ? 3 : 4,
                FurnitureKind.DrinkingFountain => item.Pad is null ? 1 : 2,
                // A vending group's bay rides on its first machine; a locker's box and a bay's sign, with their bays.
                FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker or FurnitureKind.BicycleBay => item.Pad is null ? 1 : 2,
                // Parked bikes have no collision (WLD-LIFE §5).
                FurnitureKind.EntranceMat or FurnitureKind.EntranceLamp or FurnitureKind.ParkedBicycle => 0,
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
                Check(layout.Diagnostics.Any(d => d.Feature == feature.Id && d.Code == "wp13.anchor.not-placed"), $"{feature.Id.Value} missing without a diagnostic message");
                continue;
            }
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            // On its footprint, or moved to the paving edge nearby (AnchorsStandOnOrBesidePaving).
            Check(Vector2.Distance(centre, item.Anchor) < FurnitureStyle.AnchorPavingReach + 0.1f, $"{item.Key} strayed {Vector2.Distance(centre, item.Anchor):F2} m from its footprint");
            var yaw = feature.Props!.Value.GetProperty("yawDeg").GetSingle();
            Check(Vector2.Dot(Front(item.Part.At), ExpectedFacing(layout, feature)) > 0.999f, $"{item.Key} does not face {yaw}° (or the yaw its diagnostic message gives)");
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
    public void LayoutDiagnosticsTravelWithTheFirstCellOnly()
    {
        // Structured diagnostics (ContentOutput.Diagnostics) are reported once per layout: with the first cell's L0 output.
        var module = new FurnitureModule();
        var quiet = Context(Inputs());
        Check(module.Tiles(quiet).All(t => !module.Build(quiet, t, LodLevel.L0).Diagnostics.Any(d => d.Code == PlantingRows.MissingCode)), "warnings with the real planting input");
        var ctx = Context(new Dictionary<string, ReadOnlyMemory<byte>>());
        var tiles = module.Tiles(ctx).ToList();
        var warned = tiles.Where(t => module.Build(ctx, t, LodLevel.L0).Diagnostics.Count > 0).ToList();
        Check(warned.Count == 1 && warned[0] == tiles[0], $"{warned.Count} cells carry the warning");
        var diagnostics = module.Build(ctx, tiles[0], LodLevel.L0).Diagnostics;
        foreach (var diagnostic in diagnostics) diagnostic.Validate();
        Check(diagnostics.SequenceEqual(FurnitureModule.Layout(ctx).Diagnostics), "layout diagnostics changed in the module output");
        Check(diagnostics.Count(d => d.Code == PlantingRows.MissingCode && d.Level == DiagnosticLevel.Warning) == 1, "not one missing-input warning");
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
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World.Value, Map = map, WorldSeed = 1u, SourceInputs = inputs };
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

    /// <summary>The corners of a box part's footprint and of the standing room <paramref name="front"/> ahead of it (local −Z).</summary>
    private static IEnumerable<Vector2> Corners(FurnitureItem item, float front)
    {
        var p = (BoxPartParams)item.Part.Params;
        foreach (var i in new[] { -1f, 1f })
            foreach (var z in new[] { p.Depth * 0.5f, -p.Depth * 0.5f - front })
            {
                var q = Xf.Point(item.Part.At, new(i * p.Width * 0.5f, 0, z));
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

    /// <summary>The world with made-up network lines and features added.</summary>
    private sealed class MadeWorld(IWorldModel inner, IReadOnlyList<NetworkLine> lines, IReadOnlyList<WorldFeature> features) : IWorldModel
    {
        private readonly Dictionary<FeatureId, WorldFeature> _features = Merge(inner, features);
        private readonly WorldNetwork _network = inner.Network with { Roads = [.. inner.Network.Roads, .. lines] };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => _features;
        public WorldNetwork Network => _network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;

        private static Dictionary<FeatureId, WorldFeature> Merge(IWorldModel inner, IReadOnlyList<WorldFeature> features)
        {
            var all = new Dictionary<FeatureId, WorldFeature>(inner.Features);
            foreach (var f in features) all[f.Id] = f;
            return all;
        }
    }

    /// <summary>Trees 1.5 m outside the made-up 12 × 10 m square's edges (a plaza bench's back stands against planting, Opus review 13c2c).</summary>
    private static IReadOnlyList<NetworkTree> SquareTrees(Vector2 spot)
    {
        var list = new List<NetworkTree>();
        var n = 0;
        for (var x = -6f; x <= 6.01f; x += 3f)
            foreach (var z in new[] { -6.5f, 6.5f }) list.Add(new NetworkTree(new($"design:furniture-test-tree-{n++}"), new Vector3(spot.X + x, 0f, spot.Y + z)));
        for (var z = -3f; z <= 3.01f; z += 3f)
            foreach (var x in new[] { -7.5f, 7.5f }) list.Add(new NetworkTree(new($"design:furniture-test-tree-{n++}"), new Vector3(spot.X + x, 0f, spot.Y + z)));
        return list;
    }

    /// <summary>The world with made-up trees added to its network.</summary>
    private sealed class TreedWorld(IWorldModel inner, IReadOnlyList<NetworkTree> trees) : IWorldModel
    {
        private readonly WorldNetwork _network = inner.Network with { Trees = [.. inner.Network.Trees, .. trees] };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => inner.Features;
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
