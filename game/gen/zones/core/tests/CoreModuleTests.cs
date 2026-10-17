using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Axis;
using Campus.Gen.Zones.Core.Framework;
using GdUnit4;

namespace Campus.Gen.Zones.Core.Tests;

[TestSuite]
public sealed class CoreModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void WholeSourceTracesRestoreProductionGeometryAndCollisions()
    {
        var world = RealWorld();
        var context = Context(world);
        var module = new CoreModule();
        var pieces = CoreSite.Pieces(world);
        var buildings = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Roof) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L3 })
        {
            var traces = module.TraceSources(context, lod).ToArray();
            Require(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length, "source IDs are unique");
            Require(traces.Select(t => t.SourceId).SequenceEqual(pieces.Select(p => $"core/{p.Id}")), "whole pieces retain production order and stable IDs across levels");
            Require(traces.Where(t => t.Feature is { } feature && buildings.Contains(feature)).Select(t => t.Feature!.Value).ToHashSet().SetEquals(buildings),
                "every claimed building has its own actual source");
            foreach (var trace in traces.Where(t => t.Feature is { } feature && buildings.Contains(feature)))
                Require(trace.Output.Meshes.Count > 0, $"{trace.SourceId}: actual generated building geometry");
            foreach (var tile in pieces.Select(p => CoreTiles.Of(p.Anchor, lod)).Distinct())
            {
                var sources = traces.Zip(pieces).Where(pair => CoreTiles.Of(pair.Second.Anchor, lod) == tile).Select(pair => pair.First).ToArray();
                var output = module.Build(context, tile, lod);
                var expected = Triangles([output]);
                var actual = Triangles(sources.Select(t => t.Output));
                Require(expected.Count == actual.Count && expected.All(t => actual.TryGetValue(t.Key, out var count) && count == t.Value),
                    $"{lod} {tile}: source triangles restore the batched visible geometry without clipping");
                Require(JsonSerializer.Serialize(output.Parts, ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Output.Parts).ToArray(), ContractJson.Options),
                    $"{lod} {tile}: placed parts and transforms survive tracing");
                Require(JsonSerializer.Serialize(output.Emitters ?? [], ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Output.Emitters ?? []).ToArray(), ContractJson.Options),
                    $"{lod} {tile}: emitters retain production order");
            }
            foreach (var tile in module.Tiles(context))
            {
                var sources = traces.Zip(pieces).Where(pair => CoreTiles.Of(pair.Second.Anchor, LodLevel.L0) == tile).Select(pair => pair.First);
                Require(JsonSerializer.Serialize(module.Collide(context, tile), ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Colliders).ToArray(), ContractJson.Options),
                    $"{tile}: whole sources restore production collisions");
            }
        }
    }

    private static Dictionary<(ToonRef Material, Vector3 A, Vector3 B, Vector3 C), int> Triangles(IEnumerable<ContentOutput> outputs) =>
        outputs.SelectMany(o => o.Meshes).SelectMany(mesh => Enumerable.Range(0, mesh.Index.Length / 3).Select(t =>
        {
            Vector3 At(int k)
            {
                var i = 3 * mesh.Index[3 * t + k];
                return new(mesh.Position[i], mesh.Position[i + 1], mesh.Position[i + 2]);
            }
            return (mesh.Material, At(0), At(1), At(2));
        })).GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

    [TestCase]
    public void RealMapLoadsTheOverlayAndBothAxisPieces()
    {
        var world = RealWorld();
        var pieces = CoreSite.Pieces(world);
        Require(pieces.Select(p => p.Id).SequenceEqual(["time-plaza", "sundial", "library-shell", "library-entrance", "museum", "annex-northannex", "annex-courtwing", "annex-lowblock", "annex-link", "annex-joint", "quiet-garden"]),
            "plaza, sundial, library, its entrance, the museum, the five north blocks and the quiet garden in order");
        foreach (var claim in CoreSite.Claims.OfType<FeatureClaim>())
            Require(world.Features.ContainsKey(claim.Feature), $"claimed feature {claim.Feature.Value} exists");
        var declared = CoreSite.Claims.Select(Describe).Order(StringComparer.Ordinal).ToArray();
        var fromPieces = pieces.SelectMany(p => p.Claims).Select(Describe).Order(StringComparer.Ordinal).ToArray();
        Require(declared.SequenceEqual(fromPieces), "module claims are exactly the pieces' claims");
        var plaza = (TimePlaza)pieces[0];
        Require(MathF.Abs(plaza.Radius - 26.9f) < 0.3f, $"plaza radius {plaza.Radius} from the OSM circle");
        var bearing = Shapes.BearingOf(plaza.Axis);
        Require(bearing is > 300f and < 315f, $"axis bearing {bearing} points northwest to the library stair");
        Require(!plaza.Sunken, "flat terrain stub: no ring steps yet");
    }

    [TestCase]
    public void FlatPlazaHasInlaysFlagsLampsAndBenches()
    {
        var world = RealWorld();
        var module = new CoreModule();
        var ctx = Context(world);
        var plaza = (TimePlaza)CoreSite.Pieces(world)[0];
        var output = module.Build(ctx, CoreTiles.Of(plaza.Anchor, LodLevel.L0), LodLevel.L0);
        Require(output.Parts.Count(p => p.Part.Value == PartCatalog.StreetLamp) == CoreStyle.RimLamps, "sixteen rim lamps");
        Require(output.Parts.Count(p => p.Part.Value == PartCatalog.Bench) == 3, "three benches from the overlay");
        Require(output.Emitters!.Count(e => e.Channel == CoreStyle.RimLampChannel) == CoreStyle.RimLamps, "one first-light emitter per lamp");
        Require(output.Emitters!.Count(e => e.Channel == CoreStyle.FlagLightChannel) == 3, "three flag uplights");
        Require(output.Meshes.Any(m => m.Flags!.All(f => (f & VertexFlags.Decal) != 0)), "inlays are decals");
        foreach (var lamp in plaza.LampPositions())
        {
            var off = Vector2.Distance(lamp, plaza.Centre);
            Require(MathF.Abs(off - plaza.Radius - CoreStyle.RimLampOffset) < 0.01f, "lamps on the rim ring");
        }
    }

    [TestCase]
    public void SunkenTerrainGivesSixRisersTwoRampsAndStairColliders()
    {
        var flat = RealWorld();
        var plazaFlat = (TimePlaza)CoreSite.Pieces(flat)[0];
        var world = new TerrainWorld(flat, new BowlTerrain(plazaFlat.Centre, plazaFlat.Radius - 2.1f + 0.3f, -0.9f, 0.5f));
        var plaza = (TimePlaza)CoreSite.Pieces(world)[0];
        Require(plaza.Sunken && plaza.Risers == 6, $"six risers for a 0.9 m drop (got {plaza.Risers})");
        Require(MathF.Abs(plaza.Riser - 0.15f) < 1e-3f && MathF.Abs(plaza.Tread - 0.35f) < 1e-3f, "R 0.15, T 0.35");
        Require(MathF.Abs(plaza.FootRadius - (plaza.Radius - 2.1f)) < 0.01f, "the step band is 2.1 m wide");
        var colliders = plaza.Collide();
        Require(colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 6 }), "stair ramps carry the six-step spec");
        Require(colliders.OfType<WallCollider>().Count() >= 8, "ramp cheek walls collide");
        Require(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every collider has a foot material");
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog);
        plaza.Build(build, LodLevel.L0);
        var bundles = build.Mesh.Finish();
        Require(build.Mesh.Triangles > 4000, $"steps and ramps add geometry ({build.Mesh.Triangles} triangles)");
        CheckBundles(bundles);
    }

    [TestCase]
    public void EveryLevelIsValidWoundAndWithinBudget()
    {
        var world = RealWorld();
        var palette = Palette();
        var module = new CoreModule();
        var ctx = Context(world, palette);
        foreach (var lod in Levels)
            foreach (var tile in CoreSite.Pieces(world).Select(p => CoreTiles.Of(p.Anchor, lod)).Distinct())
            {
                var output = module.Build(ctx, tile, lod);
                CheckBundles(output.Meshes);
                foreach (var m in output.Meshes)
                {
                    Require(m.ColorIndices is not null, "palette IDs per vertex");
                    palette.Validate(m.Material.Spec);
                }
                var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                var limit = lod == LodLevel.L0 ? module.Budget.TrianglesL0 : module.Budget.TrianglesL1;
                Require(triangles <= limit, $"{lod} {tile}: {triangles} triangles within {limit}");
                Require(output.Meshes.Count <= module.Budget.DrawCalls, $"{lod} {tile}: {output.Meshes.Count} bundles");
                Console.WriteLine($"core {lod} {tile}: {triangles} triangles, {output.Meshes.Count} bundles, {output.Parts.Count} parts");
            }
        var first = module.Build(ctx, CoreTiles.Of(CoreSite.Pieces(world)[0].Anchor, LodLevel.L0), LodLevel.L0);
        var second = new CoreModule().Build(Context(RealWorld(), palette), CoreTiles.Of(CoreSite.Pieces(world)[0].Anchor, LodLevel.L0), LodLevel.L0);
        Require(first.Meshes.Count == second.Meshes.Count && first.Meshes.Zip(second.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position)), "deterministic");
    }

    [TestCase]
    public void L2CellsCarryNoPartThatOnlyBuildsAtL0AndL1()
    {
        var world = RealWorld();
        var module = new CoreModule();
        var ctx = Context(world, Palette());
        // The oracle is the part definitions themselves; the context carries the catalogue captured from them (contract-36).
        var bench = new Campus.Gen.Parts.Street.BenchPart().Lods.Keys.ToArray();
        Require(bench.SequenceEqual([LodLevel.L0, LodLevel.L1]), "the bench builds at L0 and L1 only");
        var catalog = ctx.PartLods ?? throw new InvalidOperationException("the fixture context carries the part catalogue");
        Require(catalog.SupportedLods(new PartId(PartCatalog.Bench)).SequenceEqual(bench), "the catalogue reads the bench's levels");
        Require(catalog.Supports(new PartId(PartCatalog.StreetLamp), LodLevel.L2), "the street lamp builds at L2");

        var plaza = CoreSite.Pieces(world)[0];
        var near = module.Build(ctx, CoreTiles.Of(plaza.Anchor, LodLevel.L0), LodLevel.L0);
        Require(near.Parts.Any(p => p.Part.Value == PartCatalog.Bench), "the plaza cell still seats its benches at L0");
        var l2Tiles = CoreSite.Pieces(world).Select(p => CoreTiles.Of(p.Anchor, LodLevel.L2)).Distinct().ToArray();
        Require(l2Tiles.Length > 0, "the zone has L2 cells");
        foreach (var lod in new[] { LodLevel.L2, LodLevel.L3, LodLevel.L4 })
            foreach (var tile in CoreSite.Pieces(world).Select(p => CoreTiles.Of(p.Anchor, lod)).Distinct())
                foreach (var part in module.Build(ctx, tile, lod).Parts)
                {
                    Require(catalog.Supports(part.Part, lod), $"{lod} {tile}: {part.Part.Value} has no {lod} builder");
                    Require(part.Part.Value != PartCatalog.Bench, $"{lod} {tile}: no bench");
                }
        var far = module.Build(ctx, CoreTiles.Of(plaza.Anchor, LodLevel.L2), LodLevel.L2);
        Require(far.Parts.Count(p => p.Part.Value == PartCatalog.StreetLamp) == CoreStyle.RimLamps, "the rim lamps (L0 to L2) stay at L2");
    }

    [TestCase]
    public void PlacedPartsAreCheckedAgainstTheContextCatalogueOnly()
    {
        // contract-36: a missing catalogue is "not supplied" and an unknown part is an error; neither passes the part through.
        var world = RealWorld();
        var plaza = CoreSite.Pieces(world)[0];
        var tile = CoreTiles.Of(plaza.Anchor, LodLevel.L0);
        var bare = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
        Require(Raises<InvalidOperationException>(() => new CoreModule().Build(bare, tile, LodLevel.L0)), "placed parts without BuildContext.PartLods are refused");
        var empty = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u, PartLods = new PartLodCatalog([]) };
        Require(Raises<KeyNotFoundException>(() => new CoreModule().Build(empty, tile, LodLevel.L0)), "a part the catalogue does not know is refused");
    }

    [TestCase]
    public void GnomonPointsTrueNorthAtTheLatitude()
    {
        var sundial = (Sundial)CoreSite.Pieces(RealWorld())[1];
        var g = sundial.Gnomon;
        Require(MathF.Abs(g.X) < 1e-6f && g.Z < 0, "gnomon in the north meridian plane, pointing north (−z)");
        var elevation = MathF.Asin(g.Y) * 180f / MathF.PI;
        Require(MathF.Abs(elevation - 30.75f) < 0.01f, $"elevation {elevation}° equals the latitude");
        Require(g.Y < 0.55f, "dial face normal y below the snow threshold (MAP-QSH-CORE §3.4)");
        Require(sundial.Collide().All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every collider has a foot material");
    }

    [TestCase]
    public void TerrainOverlayIsWellFormed()
    {
        var path = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe", "overlay", "terrain", "core.terrain.json");
        // The contract parser checks field names, discriminators and numeric bounds (terrain.schema.json).
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(path));
        Require(file.Zone.Value == "core" && file.Owner.Value == "WP-16", "zone and owner");
        var world = RealWorld();
        var roads = world.Network.Roads.Select(r => r.Id).ToHashSet();
        bool Exists(FeatureId id) => world.Features.ContainsKey(id) || roads.Contains(id);
        foreach (var op in file.Ops)
        {
            Require(op.Id.StartsWith("design:terrain:core-", StringComparison.Ordinal), $"core terrain ID {op.Id}");
            Require(op.Reason.Length > 10, $"{op.Id} has a reason");
            IEnumerable<FeatureId> references = op switch
            {
                TerrainPadOp pad => [pad.Feature],
                TerrainCorridorOp corridor => [corridor.Road, .. corridor.Steps.Select(s => s.Structure)],
                TerrainChannelOp channel => channel.Crossings.Select(c => c.Road),
                _ => [],
            };
            foreach (var id in references) Require(Exists(id), $"{op.Id} references the real entity {id.Value}");
        }
    }

    [TestCase]
    public void OverlayParsesStrictly()
    {
        // Every key of the core overlay maps onto the contract records: a misspelt or stray field fails here, not silently in a build.
        var path = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe", "overlay", "core.d");
        var strict = new JsonSerializerOptions(ContractJson.Options)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
        };
        var files = Directory.GetFiles(path, "*.json").Order(StringComparer.Ordinal)
            .Select(file => JsonSerializer.Deserialize<OverlayFile>(File.ReadAllBytes(file), strict)!).ToArray();
        Require(files.All(file => file.Zone.Value == "core" && file.Owner.Value == "WP-16" && file.Map?.Value == "qingshuihe"), "zone, owner and map");
        var operations = files.SelectMany(file => file.Ops).ToArray();
        foreach (var op in operations.OfType<AddOp>())
        {
            Require(op.Id.Value.StartsWith("design:core-", StringComparison.Ordinal), $"core design ID {op.Id.Value}");
            Require(op.Reason.Length > 10, $"{op.Id.Value} has a reason");
        }
        Require(operations.OfType<AddOp>().Where(op => op.Kind.Value == "road" && op.Geometry is PolylineGeometry).Select(op => op.Id.Value)
            .SequenceEqual(GinkgoWalks.Concat(GinkgoSidewalks.Select(s => s.Kept))), "the ginkgo walks and the kept sidewalk ends are the overlay's only added roads");
        Require(operations.OfType<HideOp>().Select(op => op.Target.Value).SequenceEqual(GinkgoSidewalks.Select(s => s.Hidden)), "the two ginkgo sidewalks are the overlay's only hides");
    }

    [TestCase]
    public void GinkgoSidewalksGiveWayToTheWalks()
    {
        // 0h follow-up (Opus 16:00 ① and 19:40): each OSM sidewalk running within 2.5 m of a ginkgo walk is hidden whole, from the
        // features and from the road network; its ends off the walk come back as a kept piece in the original point order, width
        // and type, joined to the walk's end. Nothing of the old sidewalk is lost: every stretch is under the walk or the kept piece.
        var world = RealWorld();
        var path = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe", "network.json");
        var original = JsonSerializer.Deserialize<WorldNetwork>(File.ReadAllBytes(path), ContractJson.Options)!;
        foreach (var (hidden, walk, kept) in GinkgoSidewalks)
        {
            Require(world.Network.Roads.All(r => r.Id.Value != hidden) && !world.Features.ContainsKey(new(hidden)), $"{hidden} is gone from the roads and the features");
            var old = original.Roads.Single(r => r.Id.Value == hidden);
            var walkLine = world.Network.Roads.Single(r => r.Id.Value == walk).Line.Points;
            var piece = world.Network.Roads.Single(r => r.Id.Value == kept);
            Require(piece.Width == old.Width && Prop(piece.Props, "roadType")?.GetString() == Prop(old.Props, "highway")?.GetString()
                && Prop(piece.Props, "footway")?.GetString() == Prop(old.Props, "footway")?.GetString(), $"{kept}: the old width and type");
            var points = piece.Line.Points;
            var joined = points[0] == walkLine[^1] ? points.Skip(1).ToArray() : points[^1] == walkLine[^1] ? points.Take(points.Count - 1).ToArray() : null;
            Require(joined is { Length: >= 2 }, $"{kept}: one end is the walk's end");
            var at = old.Line.Points.ToList().IndexOf(joined![0]);
            Require(at >= 0 && joined.Select((p, i) => at + i < old.Line.Points.Count && old.Line.Points[at + i] == p).All(same => same)
                && (at == 0 || at + joined.Length == old.Line.Points.Count), $"{kept}: the old points in the old order, out to the old end");
            var line = old.Line.Points;
            for (var s = 0f; s <= Length(line); s += 0.5f)
            {
                var p = At(line, s);
                // The kept piece leaves the old line only where it bends onto the walk's end (1.26 m at most).
                Require(PolylineDistance(p, walkLine) <= 2.5f || PolylineDistance(p, points) <= 1.3f,
                    $"{hidden}: ({p.X:0.0}, {p.Y:0.0}) at s {s:0.0} is under neither {walk} nor {kept}");
            }
        }
    }

    [TestCase]
    public void GinkgoWalksKeepOffWallsAndWaterAndJoinExistingWalks()
    {
        // max-108 0h-a: the walks between the 7 m and 14 m ginkgo rows keep their paved edge 3 m or more off every building and
        // water outline and waterway, and each end lies on the paving of an existing walk, so no end is a stub.
        const float half = 1.25f;
        var world = RealWorld();
        var avenue = world.Network.Roads.Single(r => r.Id.Value == "qsh:source:way:544688571").Line.Points;
        // Any other walk of the network counts, design or OSM: the kept sidewalk ends at the bridges are design roads.
        var walks = world.Network.Roads.Where(r => !GinkgoWalks.Contains(r.Id.Value) &&
            (Prop(r.Props, "roadType") ?? Prop(r.Props, "highway"))?.GetString() is "footway" or "path" or "pedestrian").ToArray();
        foreach (var id in GinkgoWalks)
        {
            var feature = world.Features[new(id)];
            Require(feature.Kind.Value == "road" && feature.Zone?.Value == "core", $"{id}: a core road feature");
            Require(Prop(feature.Props, "roadType")?.GetString() == "footway" && Prop(feature.Props, "width")?.GetSingle() == 2 * half, $"{id}: a 2.5 m footway");
            var line = ((PolylineGeometry)feature.Geometry).Line.Points;
            var min = line.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(20f);
            var max = line.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(20f);
            var outlines = Outlines(world, "building").Concat(Outlines(world, "water"))
                .Where(r => r.Outer.Any(q => q.X >= min.X && q.X <= max.X && q.Y >= min.Y && q.Y <= max.Y)).ToArray();
            for (var s = 0f; s <= Length(line); s += 0.5f)
            {
                var p = At(line, s);
                var wall = outlines.Min(r => PlanGeometry.Contains(r, p) ? 0f
                    : r.Holes.Aggregate(PlanGeometry.DistanceToRing(p, r.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h))));
                Require(wall - half >= 3f, $"{id}: paving {wall - half:0.00} m off a wall or water edge at ({p.X:0.0}, {p.Y:0.0})");
                var river = world.Network.Waterways.Min(w => PolylineDistance(p, w.Line.Points) - w.Width * 0.5f);
                Require(river - half >= 3f, $"{id}: paving {river - half:0.00} m off a waterway at ({p.X:0.0}, {p.Y:0.0})");
                var offset = PolylineDistance(p, avenue);
                Require(MathF.Abs(offset - 10.5f) < 0.15f, $"{id}: {offset:0.00} m off the avenue's centre line at s {s:0.0}");
            }
            foreach (var end in new[] { line[0], line[^1] })
                Require(walks.Any(w => PolylineDistance(end, w.Line.Points) <= half + MathF.Max(w.Width, 2f) * 0.5f),
                    $"{id}: the end ({end.X:0.0}, {end.Y:0.0}) lies on an existing walk's paving");
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly string[] GinkgoWalks = ["design:core-ginkgo-walk-west", "design:core-ginkgo-walk-east"];

    /// <summary>The OSM sidewalks the ginkgo walks replace, each with its walk and the kept piece off the walk.</summary>
    private static readonly (string Hidden, string Walk, string Kept)[] GinkgoSidewalks =
    [
        ("qsh:source:way:544161548", "design:core-ginkgo-walk-west", "design:core-ginkgo-sidewalk-west-bridge"),
        ("qsh:source:way:544161558", "design:core-ginkgo-walk-east", "design:core-ginkgo-sidewalk-east-bridge"),
    ];

    private static JsonElement? Prop(JsonElement? props, string key) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(key, out var value) ? value : null;

    /// <summary>Polygons of one kind, part by part where a feature has parts (a part's own kind wins, as the world model reads it).</summary>
    private static IEnumerable<PolygonRings> Outlines(IWorldModel world, string kind)
    {
        static IEnumerable<PolygonRings> Of(OverlayGeometry g) => g switch
        {
            PolygonGeometry polygon => [polygon.Rings],
            MultiPolygonGeometry multi => multi.Polygons,
            _ => [],
        };
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == kind) foreach (var rings in Of(feature.Geometry)) yield return rings;
                continue;
            }
            foreach (var part in feature.Parts)
                if ((Prop(part.Props, "kind")?.GetString() ?? feature.Kind.Value) == kind)
                    foreach (var rings in Of(part.Geometry)) yield return rings;
        }
    }

    private static float Length(IReadOnlyList<Vector2> line)
    {
        var length = 0f;
        for (var i = 1; i < line.Count; i++) length += Vector2.Distance(line[i - 1], line[i]);
        return length;
    }

    private static Vector2 At(IReadOnlyList<Vector2> line, float s)
    {
        for (var i = 1; i < line.Count; i++)
        {
            var span = Vector2.Distance(line[i - 1], line[i]);
            if (s <= span || i == line.Count - 1) return Vector2.Lerp(line[i - 1], line[i], span > 0f ? MathF.Min(1f, s / span) : 0f);
            s -= span;
        }
        return line[^1];
    }

    private static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line[i - 1], line[i]));
        return MathF.Sqrt(best);
    }

    private static string Describe(Claim claim) => claim switch
    {
        FeatureClaim f => f.Feature.Value + "/" + (int)f.Aspects,
        AreaClaim a => a.Zone.Value + "/" + (int)a.Aspects,
        _ => throw new ArgumentException("Unknown claim."),
    };

    private static void CheckBundles(IReadOnlyList<GeometryBundle> bundles)
    {
        foreach (var b in bundles)
        {
            b.ValidateVertexData();
            for (var t = 0; t < b.Index.Length; t += 3)
            {
                var a = P(b, b.Index[t]);
                var g = Vector3.Cross(P(b, b.Index[t + 1]) - a, P(b, b.Index[t + 2]) - a);
                var n = N(b, b.Index[t]) + N(b, b.Index[t + 1]) + N(b, b.Index[t + 2]);
                // Godot front faces are clockwise: the geometric normal opposes the shading normal.
                Require(Vector3.Dot(g, n) <= 1e-6f, "front-face winding");
            }
        }
    }

    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException("Expected: " + what);
    }

    private static bool Raises<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return true; }
        return false;
    }

    private static BuildContext Context(IWorldModel world, PaletteCatalog? palette = null) => new()
    {
        World = world,
        Map = null!,
        WorldSeed = 1u,
        Catalog = palette ?? Campus.Contracts.Tests.PaletteFixture.Catalog,
        PartLods = PartLodFixture.Catalog,
    };

    private static IWorldModel RealWorld()
    {
        var dir = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var name in new[] { "campus.json", "map.json", "network.json", "interiors.json" })
            files[name] = File.ReadAllBytes(Path.Combine(dir, name));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(dir, "overlay"), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetRelativePath(Path.Combine(dir, "overlay"), file).Replace('\\', '/')] = File.ReadAllBytes(file);
        return WorldModelLoader.Load(new MapId("qingshuihe"), files).World;
    }

    private static PaletteCatalog Palette()
    {
        var dir = Path.Combine(FindRepository(), "game", "style", "palette");
        var entries = new List<PaletteEntry>();
        foreach (var file in new[] { Path.Combine(dir, "core.json") }.Concat(Directory.EnumerateFiles(Path.Combine(dir, "ext"), "WP-*.json").Order(StringComparer.Ordinal)))
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return new PaletteCatalog(new PaletteRegistry(entries), []);
    }

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

    /// <summary>The real world with a replaced terrain (a stand-in for WP-08's evaluator in tests).</summary>
    private sealed class TerrainWorld(IWorldModel inner, ITerrain terrain) : IWorldModel
    {
        public ITerrain Terrain { get; } = terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => inner.Features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    /// <summary>A sunken disc at <paramref name="floor"/> out to <paramref name="radius"/>, rising at <paramref name="grade"/> back to 0.</summary>
    private sealed class BowlTerrain(Vector2 centre, float radius, float floor, float grade) : ITerrain
    {
        public bool IsFlat(TileKey tile) => false;
        public float HeightAt(float x, float z) => MathF.Min(0f, floor + grade * MathF.Max(0f, Vector2.Distance(new(x, z), centre) - radius));
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity);
    }
}
