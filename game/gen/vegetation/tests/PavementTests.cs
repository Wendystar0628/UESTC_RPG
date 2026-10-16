using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>
/// Hard pavement of the effective world takes no tree except in a tree pit, and only the trees on it give way: every other
/// tree keeps its key, look and place. The layouts before pavement gave way are the 23b/23c1 fingerprints, taken from
/// the planter before this rule existed.
/// </summary>
[TestSuite]
public sealed class PavementTests
{
    private const int CampusTrees = 3142;

    /// <summary>Planting order with every overlay: 3142 plus the 67 ginkgo the hide of the two OSM sidewalks (0h) gives back along the avenue; it includes that hide and is merged in the same batch as 0h.</summary>
    private const int OverlaidTrees = 3209;

    /// <summary>The layout of the campus data alone (no overlays), as 23c1 fixed it in VegetationInputsTests.</summary>
    internal const string BareFingerprint = "7456706a925937f6dcebbb74931f978ae8c010f397c99bf3bc27fe76a123d52f";

    /// <summary>The layout of the campus data with every authored overlay of the map, the world as the kernel loads it (refixed after 0j with the hide of 0h, merged in the same batch as 0h).</summary>
    private const string OverlaidFingerprint = "3ab4bd7c71ecaf42c73e4b21ce886408402e1c7373067d6fb8afe2141258f352";

    /// <summary>
    /// The centre line of 0h-b's second Dangui Road walk (design:north-dangui-walk-w2, 021ba14), copied as a fixture so the case is
    /// covered whether or not that overlay is in the map yet.
    /// </summary>
    private const string DanguiWalkW2 = "[[-23.09, -628.26], [-22.13, -630.805], [-21.997, -631.157], [-20.365, -635.483], [-18.6, -640.162], [-16.836, -644.84], " +
        "[-15.071, -649.518], [-13.306, -654.196], [-11.541, -658.874], [-9.776, -663.553], [-8.012, -668.231], [-6.247, -672.909], [-4.482, -677.587], " +
        "[-2.7, -682.31], [-2.184, -683.675], [-0.709, -687.165], [1.239, -691.77], [3.187, -696.375], [5.135, -700.98], [7.083, -705.585], [9.031, -710.189], " +
        "[10.98, -714.794], [12.928, -719.399], [14.876, -724.004], [16.824, -728.609], [18.772, -733.214], [20.72, -737.819], [22.812, -742.742], " +
        "[22.978, -743.127], [25.46, -747.556], [27.511, -751.172], [27.628, -751.465], [29.414, -755.758], [31.342, -760.371], [33.271, -764.984], " +
        "[35.199, -769.598], [37.127, -774.211], [39.055, -778.824], [40.984, -783.437], [42.912, -788.051], [44.84, -792.664], [46.769, -797.277], " +
        "[48.697, -801.89], [50.625, -806.503], [52.553, -811.117], [54.482, -815.73], [56.41, -820.343], [58.338, -824.956], [60.266, -829.569], [61.728, -833.067]]";

    private static readonly string[] DanguiOsmTrees = ["qsh:source:node:9633125237", "qsh:source:node:9633125264"];

    private static readonly Lazy<(SpeciesTable Species, PlantingRules Rules)> Rules = new(() =>
    {
        var species = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(Root, "game", "data", "vegetation", "species.json")));
        return (species, PlantingRules.Parse(File.ReadAllBytes(Path.Combine(Root, "game", "data", "maps", "qingshuihe", "vegetation", "planting.json")), species));
    });

    private static readonly Lazy<IWorldModel> Bare = new(() => Load(false));
    private static readonly Lazy<IWorldModel> Overlaid = new(() => Load(true));
    private static readonly Lazy<PlantingLayout> BareLayout = new(() => Plant(Bare.Value));

    private static string Root => FindRepository();

    [TestCase]
    public void NoTreeStandsOnHardPavementAndOnlyThoseTreesGiveWay()
    {
        // The campus data alone, and the world as the kernel loads it (every overlay: design walks, plazas, tree pits).
        foreach (var (label, world, fingerprint) in new[] { ("campus data", Bare.Value, BareFingerprint), ("with overlays", Overlaid.Value, OverlaidFingerprint) })
        {
            var layout = Verify(world, fingerprint, label);
            if (layout.Avoided.Count == 0) throw new InvalidOperationException($"{label}: no tree gave way, so the check proved nothing");
            Console.WriteLine($"pavement ({label}): {layout.Avoided.Count} of {layout.BeforeGivingWay().Count} trees give way, {layout.Trees.Count} stay; "
                + string.Join(", ", layout.Avoided.GroupBy(a => a.Tree.Source).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
        }
    }

    [TestCase]
    public void TheDesignWalksAndPlazasOfTheOverlaysAreRead()
    {
        // The census is of the effective world, not of the original roads: the design walks of the overlays are in it.
        var census = Census.Of(Overlaid.Value);
        foreach (var id in new[] { "design:core-ginkgo-walk-west", "design:core-ginkgo-walk-east", "design:center-east-riverside-walk",
                     "design:north-dangui-walk-w1", "design:north-dangui-walk-w2", "design:north-dangui-walk-e1",
                     "design:south-clock-walk", "design:south-clock-forecourt", "design:south-a01-plaza" })
            if (census.Paving.All(p => p.Id != id)) throw new InvalidOperationException($"{id} is not read as pavement");
        if (census.Pits.Count < 11) throw new InvalidOperationException($"{census.Pits.Count} tree pits in the overlays");
        var layout = Plant(Overlaid.Value);
        foreach (var t in layout.Trees)
            foreach (var pave in census.Paving.Where(p => p.Id.StartsWith("design:", StringComparison.Ordinal)))
                if (census.Within(pave, new(t.Position.X, t.Position.Z), 0f))
                    throw new InvalidOperationException($"{t.Key} stands on {pave.Id}");
    }

    [TestCase]
    public void ADesignWalkAcrossTheAvenueTakesTheTreesOnIt()
    {
        var bare = BareLayout.Value;
        var tree = bare.Trees.Where(t => t.Source == PlantSource.Road && t.Species.Id == "ginkgo").ElementAt(300);
        var (x, z) = (tree.Position.X, tree.Position.Z);
        var world = Load(false, ("fixture-avenue-walk.overlay.json", Overlay(Road("design:fixture-avenue-cross-walk", "footway", 3f, [new(x - 5f, z - 5f), new(x + 5f, z + 5f)]))));
        var layout = Verify(world, BareFingerprint, "avenue walk");
        if (layout.Avoided.All(a => a.Tree.Key != tree.Key) || layout.Trees.Any(t => t.Key == tree.Key))
            throw new InvalidOperationException($"{tree.Key} stands on the walk and did not give way");
        var more = layout.Avoided.Count - bare.Avoided.Count;
        if (more is < 1 or > 6) throw new InvalidOperationException($"{more} more trees gave way for a 14 m walk");
        Console.WriteLine($"pavement (avenue walk): {more} more trees give way, {string.Join(", ", layout.Avoided.Select(a => a.Tree.Key).Except(bare.Avoided.Select(a => a.Tree.Key)))}");
    }

    [TestCase]
    public void TheTwoOsmTreesOnDanguiWalkW2GiveWay()
    {
        var bare = BareLayout.Value;
        foreach (var id in DanguiOsmTrees)
            if (bare.Trees.All(t => t.Key != id) || bare.Avoided.Any(a => a.Tree.Key == id))
                throw new InvalidOperationException($"{id} is not a planted OSM tree before the walk");
        var world = Load(false, ("fixture-dangui-w2.overlay.json", Overlay(Road("design:fixture-dangui-walk-w2", "footway", 2.5f, DanguiWalkW2))));
        var layout = Verify(world, BareFingerprint, "w2");
        foreach (var id in DanguiOsmTrees)
        {
            var given = layout.Avoided.SingleOrDefault(a => a.Tree.Key == id);
            if (given is null || given.Tree.Source != PlantSource.OsmTree || layout.Trees.Any(t => t.Key == id))
                throw new InvalidOperationException($"{id} did not give way to the walk");
        }
        var more = layout.Avoided.Select(a => a.Tree).Where(t => bare.Avoided.All(b => b.Tree.Key != t.Key)).ToList();
        Console.WriteLine($"pavement (w2): {more.Count} more trees give way: " + string.Join(", ", more.Select(t => $"{t.Source}/{t.Species.Id} {t.Key}")));
    }

    [TestCase]
    public void TheTwoOsmTreesOnDanguiWalkW2GiveWayInTheMap()
    {
        // The same case on the map as the kernel loads it, now that 0h-b's walks are in the overlays.
        var layout = Plant(Overlaid.Value);
        foreach (var id in DanguiOsmTrees)
        {
            var given = layout.Avoided.SingleOrDefault(a => a.Tree.Key == id);
            if (given is null || given.Tree.Source != PlantSource.OsmTree || layout.Trees.Any(t => t.Key == id))
                throw new InvalidOperationException($"{id} did not give way to design:north-dangui-walk-w2");
        }
        Console.WriteLine($"pavement (map): {layout.Avoided.Count(a => a.Tree.Key.StartsWith("qsh:source:node:96331252", StringComparison.Ordinal))} OSM trees of the Dangui Road walks give way");
    }

    [TestCase]
    public void ADesignWalkFiledAsFeatureAndAsNetworkLineIsPavedOnce()
    {
        // WP-08's effective network files a design walk twice under one ID: as the feature the overlay added and as a network line.
        // The walk is one walk: paved once, from the line, so the trees on it give way (they still count as planted, so the planting
        // order is that of the campus data alone) instead of vanishing as they would from a road; filed as the feature alone it takes
        // the same trees. A network line alone is an ordinary road and keeps the trunks off all the same.
        var bare = BareLayout.Value;
        var tree = bare.Trees.Where(t => t.Source == PlantSource.Road && t.Species.Id == "ginkgo").ElementAt(300);
        var (x, z) = (tree.Position.X, tree.Position.Z);
        const string id = "design:fixture-twice-filed-walk";
        var world = Load(false, ("fixture-twice-filed.overlay.json", Overlay(Road(id, "footway", 3f, [new(x - 5f, z - 5f), new(x + 5f, z + 5f)]))));
        RequireFiled(world, id, line: true, feature: true);
        var both = Verify(world, BareFingerprint, "filed twice");
        if (both.Avoided.Count(a => a.Tree.Key == tree.Key) != 1 || both.Trees.Any(t => t.Key == tree.Key))
            throw new InvalidOperationException($"{tree.Key} stands on the walk and did not give way exactly once");
        var featureOnly = new Filed(world, road => road.Id.Value != id, _ => true);
        RequireFiled(featureOnly, id, line: false, feature: true);
        var asFeature = Verify(featureOnly, BareFingerprint, "filed as the feature");
        if (!SameLayout(both, asFeature)) throw new InvalidOperationException("the walk filed as the feature alone takes other trees than filed twice");
        var lineOnly = new Filed(world, _ => true, feature => feature.Id.Value != id);
        RequireFiled(lineOnly, id, line: true, feature: false);
        var asLine = Plant(lineOnly);
        var walk = lineOnly.Network.Roads.Single(r => r.Id.Value == id);
        foreach (var t in asLine.Trees)
            if (Off(walk, new(t.Position.X, t.Position.Z)) < walk.Width * 0.5f + Planting.TrunkClearance)
                throw new InvalidOperationException($"{t.Key} stands on the walk filed as a network line");
    }

    [TestCase]
    public void AHiddenDesignWalkTakesNoTree()
    {
        // A walk the overlays add and then hide leaves both stacks, and nothing of it comes back: the layout is the campus data's.
        var bare = BareLayout.Value;
        var tree = bare.Trees.Where(t => t.Source == PlantSource.Road && t.Species.Id == "ginkgo").ElementAt(300);
        var (x, z) = (tree.Position.X, tree.Position.Z);
        const string id = "design:fixture-hidden-walk";
        var world = Load(false, ("fixture-hidden.overlay.json", Overlay(Road(id, "footway", 3f, [new(x - 5f, z - 5f), new(x + 5f, z + 5f)]),
            "{\"op\":\"hide\",\"target\":\"" + id + "\",\"reason\":\"Fixture: the walk is taken away again.\"}")));
        RequireFiled(world, id, line: false, feature: false);
        var layout = Verify(world, BareFingerprint, "hidden walk");
        if (!SameLayout(layout, bare) || layout.Trees.All(t => t.Key != tree.Key)) throw new InvalidOperationException("a hidden design walk still takes trees");
    }

    [TestCase]
    public void TheOsmTreePointsOnAWalkFiledInBothStacksGiveWayAndDoNotVanish()
    {
        // The OSM tree points on a design walk (Dangui Road's second walk, 9633125237 and its neighbours) give way: each is in the
        // layout's Avoided list at its place in the planting order, none is dropped before it is planted, none stands.
        var bare = BareLayout.Value;
        const string id = "design:fixture-dangui-walk-w2";
        var world = Load(false, ("fixture-dangui-w2-both.overlay.json", Overlay(Road(id, "footway", 2.5f, DanguiWalkW2))));
        RequireFiled(world, id, line: true, feature: true);
        var layout = Verify(world, BareFingerprint, "w2 filed twice");
        var walk = world.Network.Roads.Single(r => r.Id.Value == id);
        var onWalk = bare.Trees.Where(t => t.Source == PlantSource.OsmTree && Off(walk, new(t.Position.X, t.Position.Z)) < walk.Width * 0.5f + Planting.TrunkClearance)
            .Select(t => t.Key).ToList();
        foreach (var key in DanguiOsmTrees)
            if (!onWalk.Contains(key)) throw new InvalidOperationException($"{key} does not stand on the walk");
        var before = layout.BeforeGivingWay();
        foreach (var key in onWalk)
        {
            var given = layout.Avoided.Where(a => a.Tree.Key == key).ToList();
            if (given.Count != 1 || before[given[0].Index].Key != key || layout.Trees.Any(t => t.Key == key))
                throw new InvalidOperationException($"{key} on the walk did not give way once, at its place");
        }
        Console.WriteLine($"pavement (w2 in both stacks): {onWalk.Count} OSM tree points give way: {string.Join(", ", onWalk)}");
    }

    [TestCase]
    public void TheTreePitsOfTheMapKeepTheDanguiColumnAndThePlazaTrees()
    {
        // The map's pavement takes trees that must stay: the left column of osmanthus along Dangui Road (39 of the avenue's own rows) and
        // the OSM tree points of the plaza (camphor 30, osmanthus 19), because the pavement there is a big pedestrian area. Their tree
        // pits (WP-20) keep them: with the pits taken out they give way, with the pits in they stand.
        var withPits = Verify(Overlaid.Value, OverlaidFingerprint, "with pits");
        var noPits = Verify(new Filed(Overlaid.Value, _ => true, f => f.Kind.Value != Planting.TreePitKind), OverlaidFingerprint, "without pits");
        var standing = withPits.Trees.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        var kept = noPits.Avoided.Where(a => standing.Contains(a.Tree.Key)).Select(a => a.Tree).ToList();
        var column = kept.Count(t => t.Source == PlantSource.Road && t.Species.Id == "osmanthus");
        var plaza = kept.Count(t => t.Source == PlantSource.OsmTree);
        if (column < 39 || plaza < 49 || kept.Count < 88) throw new InvalidOperationException($"the pits keep {kept.Count} trees ({column} in the column, {plaza} OSM points), 88 expected");
        if (withPits.Avoided.Count != noPits.Avoided.Count - kept.Count) throw new InvalidOperationException("the pits changed which other trees give way");
        Console.WriteLine($"pavement (pits): {noPits.Avoided.Count} trees give way without the pits, {withPits.Avoided.Count} with them; the pits keep {kept.Count} " +
            string.Join(", ", kept.GroupBy(t => $"{t.Source}/{t.Species.Id}").OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} {g.Count()}")));
    }

    [TestCase]
    public void ATreePitLetsATreeStandOnPavement()
    {
        // Two trees that give way on the campus data: one gets a square pit, the other a round bed; both stay, nothing else moves.
        var bare = BareLayout.Value;
        var square = bare.Avoided.First(a => a.Tree.Source == PlantSource.Road).Tree;
        var round = bare.Avoided.First(a => a.Tree.Source == PlantSource.OsmTree).Tree;
        var (sx, sz) = (square.Position.X, square.Position.Z);
        var ring = $"[[{F(sx - 0.9f)},{F(sz - 0.9f)}],[{F(sx + 0.9f)},{F(sz - 0.9f)}],[{F(sx + 0.9f)},{F(sz + 0.9f)}],[{F(sx - 0.9f)},{F(sz + 0.9f)}]]";
        var world = Load(false, ("fixture-tree-pits.overlay.json", Overlay(
            "{\"op\":\"add\",\"id\":\"design:fixture-square-pit\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"polygon\",\"rings\":{\"outer\":" + ring +
            ",\"holes\":[]}},\"height\":0.42,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"size\":1.8},\"reason\":\"Fixture: a square tree pit.\"}",
            "{\"op\":\"add\",\"id\":\"design:fixture-round-bed\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"point\",\"position\":[" + F(round.Position.X) + ",0," + F(round.Position.Z) +
            "]},\"height\":0.45,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"radius\":1.0},\"reason\":\"Fixture: a round tree bed.\"}")));
        var layout = Verify(world, BareFingerprint, "tree pits");
        foreach (var tree in new[] { square, round })
            if (layout.Trees.All(t => t.Key != tree.Key) || layout.Avoided.Any(a => a.Tree.Key == tree.Key))
                throw new InvalidOperationException($"{tree.Key} stands in a pit and gave way");
        var inPits = bare.Avoided.Count(a => (MathF.Abs(a.Tree.Position.X - sx) <= 0.9f && MathF.Abs(a.Tree.Position.Z - sz) <= 0.9f) ||
            Vector2.Distance(new(a.Tree.Position.X, a.Tree.Position.Z), new(round.Position.X, round.Position.Z)) <= 1f);
        if (inPits < 2 || layout.Avoided.Count != bare.Avoided.Count - inPits)
            throw new InvalidOperationException($"{layout.Avoided.Count} trees give way, {bare.Avoided.Count} did before, {inPits} stand in the pits");
    }

    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The walk is filed (or not) as a network line and as a feature of its ID in this world: the precondition of a case.</summary>
    private static void RequireFiled(IWorldModel world, string id, bool line, bool feature)
    {
        if (world.Network.Roads.Any(r => r.Id.Value == id) != line || world.Features.ContainsKey(new FeatureId(id)) != feature)
            throw new InvalidOperationException($"{id} is not filed as expected: network line {line}, feature {feature}");
    }

    private static bool SameLayout(PlantingLayout a, PlantingLayout b) =>
        a.Trees.Select(t => t.Key).SequenceEqual(b.Trees.Select(t => t.Key)) &&
        a.Avoided.Select(t => (t.Index, t.Tree.Key)).SequenceEqual(b.Avoided.Select(t => (t.Index, t.Tree.Key)));

    private static float Off(NetworkLine line, Vector2 p)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Line.Points.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line.Line.Points[i - 1], line.Line.Points[i]));
        return MathF.Sqrt(best);
    }

    /// <summary>A world with some of its network roads and features left out, to file a walk one way only.</summary>
    private sealed class Filed(IWorldModel inner, Func<NetworkLine, bool> keepRoad, Func<WorldFeature, bool> keepFeature) : IWorldModel
    {
        private readonly Dictionary<FeatureId, WorldFeature> _features = inner.Features.Where(f => keepFeature(f.Value)).ToDictionary(f => f.Key, f => f.Value);
        private readonly WorldNetwork _network = inner.Network with { Roads = inner.Network.Roads.Where(keepRoad).ToList() };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => _features;
        public WorldNetwork Network => _network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    /// <summary>
    /// Builds the layout and checks it against the world by an independent census: the trees before pavement gave way are the
    /// fingerprinted planting order, exactly the trees on pavement and outside every pit gave way (key and place), and none that
    /// stayed stands on pavement.
    /// </summary>
    private static PlantingLayout Verify(IWorldModel world, string fingerprint, string label)
    {
        var layout = Plant(world);
        var before = layout.BeforeGivingWay();
        if (before.Count != (fingerprint == OverlaidFingerprint ? OverlaidTrees : CampusTrees) || Fingerprint(before) != fingerprint)
            throw new InvalidOperationException($"{label}: the planting order changed: {before.Count} trees, {Fingerprint(before)}");
        var census = Census.Of(world);
        var expected = before.Select((t, i) => (Index: i, Tree: t)).Where(e => census.GivesWay(new(e.Tree.Position.X, e.Tree.Position.Z))).ToList();
        if (!expected.Select(e => (e.Index, e.Tree.Key)).SequenceEqual(layout.Avoided.Select(a => (a.Index, a.Tree.Key))))
            throw new InvalidOperationException($"{label}: {layout.Avoided.Count} trees gave way, the census says {expected.Count}");
        foreach (var t in layout.Trees)
        {
            var p = new Vector2(t.Position.X, t.Position.Z);
            if (census.Paving.Any(pave => census.Within(pave, p, 0f)) && !census.InPit(p)) throw new InvalidOperationException($"{label}: {t.Key} stands on pavement");
        }
        return layout;
    }

    private static PlantingLayout Plant(IWorldModel world) => Planting.Build(world, Rules.Value.Species, Rules.Value.Rules, 1u);

    private static IWorldModel Load(bool overlays, params (string Name, string Text)[] extra)
    {
        var data = Path.Combine(Root, "game", "data", "maps", "qingshuihe");
        var names = new[] { "campus.json", "network.json", "interiors.json", "map.json" }.AsEnumerable();
        if (overlays) names = names.Concat(Directory.GetFiles(Path.Combine(data, "overlay"), "*.overlay.json").Select(p => "overlay/" + Path.GetFileName(p)));
        var files = names.OrderBy(name => name, StringComparer.Ordinal).ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var (name, text) in extra) files["overlay/" + name] = text;
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static string Overlay(params string[] ops) =>
        "{\"zone\":\"fixture\",\"owner\":\"WP-23\",\"map\":\"qingshuihe\",\"ops\":[" + string.Join(',', ops) + "]}";

    private static string Road(string id, string type, float width, IEnumerable<Vector2> points) =>
        Road(id, type, width, "[" + string.Join(',', points.Select(p => $"[{F(p.X)},{F(p.Y)}]")) + "]");

    private static string Road(string id, string type, float width, string points) =>
        "{\"op\":\"add\",\"id\":\"" + id + "\",\"kind\":\"road\",\"geometry\":{\"type\":\"polyline\",\"line\":{\"points\":" + points +
        "}},\"props\":{\"roadType\":\"" + type + "\",\"width\":" + F(width) + "},\"reason\":\"Fixture: a design walk.\"}";

    private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    internal static string Fingerprint(IEnumerable<PlantedTree> trees)
    {
        var text = new StringBuilder();
        foreach (var t in trees)
            text.Append(string.Join('|', t.Key, t.Species.Id, t.Variant.ToString(CultureInfo.InvariantCulture), Bits(t.Position.X), Bits(t.Position.Y),
                Bits(t.Position.Z), Bits(t.Yaw), Bits(t.Scale), t.Wash ? "1" : "0", ((int)t.Source).ToString(CultureInfo.InvariantCulture))).Append('\n');
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("x8", CultureInfo.InvariantCulture);

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }

    /// <summary>
    /// The hard pavement and tree pits of a world, read again from its features and network the way WP-15 paves them: the
    /// pedestrian, footway, path, steps and cycleway areas and lines of its features (parts of kind road, and design roads) and of
    /// its network.
    /// </summary>
    private sealed class Census
    {
        private static readonly string[] Paved = ["pedestrian", "footway", "path", "steps", "cycleway"];

        public List<Pave> Paving { get; } = [];
        public List<Pit> Pits { get; } = [];

        public sealed record Pave(string Id, PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half);
        public sealed record Pit(PolygonRings? Rings, Vector2 Centre, float Radius);

        public static Census Of(IWorldModel world)
        {
            var census = new Census();
            foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            {
                foreach (var (props, geometry) in RoadSources(feature))
                {
                    var type = Text(props, "roadType") ?? Text(props, "highway");
                    if (type is null || !Paved.Contains(type)) continue;
                    switch (geometry)
                    {
                        case PolygonGeometry polygon: census.Paving.Add(new(feature.Id.Value, polygon.Rings, null, 0f)); break;
                        case MultiPolygonGeometry multi: census.Paving.AddRange(multi.Polygons.Select(rings => new Pave(feature.Id.Value, rings, null, 0f))); break;
                        case PolylineGeometry line:
                            var width = Number(props, "width") is { } w and > 0f ? w : type switch { "path" => 2f, "cycleway" => 3f, "pedestrian" => 5f, _ => 2.5f };
                            census.Paving.Add(new(feature.Id.Value, null, line.Line.Points, width * 0.5f));
                            break;
                    }
                }
                if (feature.Kind.Value != Planting.TreePitKind) continue;
                switch (feature.Geometry)
                {
                    case PolygonGeometry polygon: census.Pits.Add(new(polygon.Rings, default, 0f)); break;
                    case MultiPolygonGeometry multi: census.Pits.AddRange(multi.Polygons.Select(rings => new Pit(rings, default, 0f))); break;
                    case PointGeometry point:
                        var radius = Number(feature.Props, "radius") is { } r and > 0f ? r : Number(feature.Props, "size") is { } size and > 0f ? size * 0.5f : 0.9f;
                        census.Pits.Add(new(null, new(point.Position.X, point.Position.Z), radius));
                        break;
                }
            }
            foreach (var road in world.Network.Roads)
            {
                var type = Text(road.Props, "roadType") ?? Text(road.Props, "highway");
                if (type is not null && Paved.Contains(type)) census.Paving.Add(new(road.Id.Value, null, road.Line.Points, MathF.Max(road.Width, 2f) * 0.5f));
            }
            return census;
        }

        private static IEnumerable<(JsonElement? Props, OverlayGeometry Geometry)> RoadSources(WorldFeature feature)
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
                yield break;
            }
            foreach (var part in feature.Parts)
            {
                var kind = part.Props is { ValueKind: JsonValueKind.Object } own && own.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
                    ? k.GetString() : feature.Kind.Value;
                if (kind == "road") yield return (part.Props ?? feature.Props, part.Geometry);
            }
        }

        /// <summary>True when <paramref name="p"/> is on the pavement, or within <paramref name="clearance"/> of it.</summary>
        public bool Within(Pave pave, Vector2 p, float clearance)
        {
            if (pave.Rings is { } rings)
                return PlanGeometry.Contains(rings, p) ||
                    rings.Holes.Prepend(rings.Outer).Any(ring => PlanGeometry.DistanceToRing(p, ring) < clearance);
            var r = pave.Half + clearance;
            for (var i = 1; i < pave.Line!.Count; i++)
                if (PlanGeometry.SegmentDistanceSquared(p, pave.Line[i - 1], pave.Line[i]) < r * r) return true;
            return false;
        }

        public bool InPit(Vector2 p) => Pits.Any(pit => pit.Rings is { } rings ? PlanGeometry.Contains(rings, p) : Vector2.Distance(p, pit.Centre) <= pit.Radius);

        /// <summary>A trunk here gives way: on pavement or within the trunk clearance of it, and in no pit.</summary>
        public bool GivesWay(Vector2 p) => !InPit(p) && Paving.Any(pave => Within(pave, p, Planting.TrunkClearance));

        private static string? Text(JsonElement? props, string name) =>
            props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static float? Number(JsonElement? props, string name) =>
            props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number &&
            v.TryGetSingle(out var number) && float.IsFinite(number) ? number : null;
    }
}
