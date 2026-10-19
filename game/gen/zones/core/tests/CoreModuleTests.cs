using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Axis;
using Campus.Gen.Zones.Core.Framework;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Zones.Core.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class CoreModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WholeSourceTracesRestoreProductionGeometryAndCollisions()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var context = Context(world);
        var module = new CoreModule();
        var pieces = CoreSite.Pieces(world);
        var buildings = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Roof) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L3 })
        {
            var traces = module.TraceSources(context, lod).ToArray();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage("source IDs are unique").IsTrue();
            AssertThat(traces.Select(t => t.SourceId).SequenceEqual(pieces.Select(p => $"core/{p.Id}"))).OverrideFailureMessage("whole pieces retain production order and stable IDs across levels").IsTrue();
            AssertThat(traces.Where(t => t.Feature is { } feature && buildings.Contains(feature)).Select(t => t.Feature!.Value).ToHashSet().SetEquals(buildings)).OverrideFailureMessage("every claimed building has its own actual source").IsTrue();
            foreach (var trace in traces.Where(t => t.Feature is { } feature && buildings.Contains(feature)))
                AssertThat(trace.Output.Meshes.Count > 0).OverrideFailureMessage($"{trace.SourceId}: actual generated building geometry").IsTrue();
            foreach (var tile in pieces.Select(p => TileGrid.At(p.Anchor, lod)).Distinct())
            {
                var sources = traces.Zip(pieces).Where(pair => TileGrid.At(pair.Second.Anchor, lod) == tile).Select(pair => pair.First).ToArray();
                var output = module.Build(context, tile, lod);
                var expected = Triangles([output]);
                var actual = Triangles(sources.Select(t => t.Output));
                AssertThat(expected.Count == actual.Count && expected.All(t => actual.TryGetValue(t.Key, out var count) && count == t.Value)).OverrideFailureMessage($"{lod} {tile}: source triangles restore the batched visible geometry without clipping").IsTrue();
                AssertThat(JsonSerializer.Serialize(output.Parts, ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Output.Parts).ToArray(), ContractJson.Options)).OverrideFailureMessage($"{lod} {tile}: placed parts and transforms survive tracing").IsTrue();
                AssertThat(JsonSerializer.Serialize(output.Emitters ?? [], ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Output.Emitters ?? []).ToArray(), ContractJson.Options)).OverrideFailureMessage($"{lod} {tile}: emitters retain production order").IsTrue();
            }
            foreach (var tile in module.Tiles(context))
            {
                var sources = traces.Zip(pieces).Where(pair => TileGrid.At(pair.Second.Anchor, LodLevel.L0) == tile).Select(pair => pair.First);
                AssertThat(JsonSerializer.Serialize(module.Collide(context, tile), ContractJson.Options) == JsonSerializer.Serialize(sources.SelectMany(t => t.Colliders).ToArray(), ContractJson.Options)).OverrideFailureMessage($"{tile}: whole sources restore production collisions").IsTrue();
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

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void RealMapLoadsTheAuthoredWorldAndBothAxisPieces()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var pieces = CoreSite.Pieces(world);
        AssertThat(pieces.Count > 0 && pieces[0] is TimePlaza && pieces.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() == pieces.Count).OverrideFailureMessage("the plaza leads the pieces, whose IDs are unique").IsTrue();
        foreach (var claim in CoreSite.Claims.OfType<FeatureClaim>())
            AssertThat(world.Features.ContainsKey(claim.Feature)).OverrideFailureMessage($"claimed feature {claim.Feature.Value} exists").IsTrue();
        var declared = CoreSite.Claims.Select(Describe).Order(StringComparer.Ordinal).ToArray();
        var fromPieces = pieces.SelectMany(p => p.Claims).Select(Describe).Order(StringComparer.Ordinal).ToArray();
        AssertThat(declared.SequenceEqual(fromPieces)).OverrideFailureMessage("module claims are exactly the pieces' claims").IsTrue();
        var plaza = (TimePlaza)pieces[0];
        AssertThat(plaza.FloorTop == world.Terrain.HeightAt(plaza.Centre.X, plaza.Centre.Y) + GroundLevels.Paving).OverrideFailureMessage("plaza floor follows the production terrain").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void FlatPlazaHasInlaysFlagsLampsAndBenches()
    {
        var world = new FakeWorld(Campus.Tests.RealMapFixture.Load().World) { Terrain = new FormulaTerrain((_, _) => 0f) };
        var module = new CoreModule();
        var ctx = Context(world);
        var plaza = (TimePlaza)CoreSite.Pieces(world)[0];
        var output = module.Build(ctx, TileGrid.At(plaza.Anchor, LodLevel.L0), LodLevel.L0);
        AssertThat(output.Parts.Count(p => p.Part.Value == PartCatalog.StreetLamp) == CoreStyle.RimLamps).OverrideFailureMessage("sixteen rim lamps").IsTrue();
        AssertThat(output.Parts.Count(p => p.Part.Value == PartCatalog.Bench) == world.Features.Keys.Count(id => id.Value.StartsWith(CoreIds.PlazaBenchPrefix, StringComparison.Ordinal))).OverrideFailureMessage("benches follow their world records").IsTrue();
        AssertThat(output.Emitters!.Count(e => e.Channel == CoreStyle.RimLampChannel) == CoreStyle.RimLamps).OverrideFailureMessage("one first-light emitter per lamp").IsTrue();
        AssertThat(output.Emitters!.Count(e => e.Channel == CoreStyle.FlagLightChannel) > 0).OverrideFailureMessage("flag uplights").IsTrue();
        AssertThat(module.DescribeLights(ctx).SequenceEqual(output.Emitters!)).OverrideFailureMessage("light metadata matches the plaza's L0 output").IsTrue();
        AssertThat(output.Meshes.Any(m => m.Flags!.All(f => (f & VertexFlags.Decal) != 0))).OverrideFailureMessage("inlays are decals").IsTrue();
        foreach (var lamp in plaza.LampPositions())
        {
            var off = Vector2.Distance(lamp, plaza.Centre);
            AssertThat(MathF.Abs(off - plaza.Radius - CoreStyle.RimLampOffset) < TestTolerance.Position).OverrideFailureMessage("lamps on the rim ring").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void SunkenTerrainGivesSixRisersTwoRampsAndStairColliders()
    {
        var flat = Campus.Tests.RealMapFixture.Load().World;
        var plazaFlat = (TimePlaza)CoreSite.Pieces(flat)[0];
        var world = new FakeWorld(flat) { Terrain = BowlTerrain(plazaFlat.Centre, plazaFlat.Radius - 2.1f + 0.3f, -0.9f, 0.5f) };
        var plaza = (TimePlaza)CoreSite.Pieces(world)[0];
        AssertThat(plaza.Sunken && plaza.Risers == 6).OverrideFailureMessage($"six risers for a 0.9 m drop (got {plaza.Risers})").IsTrue();
        AssertThat(MathF.Abs(plaza.Riser - 0.9f / plaza.Risers) < TestTolerance.Position && MathF.Abs(plaza.Tread - Campus.Gen.Parts.Circulation.TerrainDrop.TreadFor(plaza.Riser)) < TestTolerance.Position).OverrideFailureMessage("risers share the drop, treads follow the riser").IsTrue();
        AssertThat(MathF.Abs(plaza.FootRadius - (plaza.Radius - 2.1f)) < TestTolerance.Position).OverrideFailureMessage("the step band is 2.1 m wide").IsTrue();
        var colliders = plaza.Collide();
        AssertThat(colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 6 })).OverrideFailureMessage("stair ramps carry the six-step spec").IsTrue();
        AssertThat(colliders.OfType<WallCollider>().Any()).OverrideFailureMessage("ramp cheek walls collide").IsTrue();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every collider has a foot material").IsTrue();
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog);
        plaza.Build(build, LodLevel.L0);
        var bundles = build.Mesh.Finish();
        AssertThat(build.Mesh.Triangles > 0).OverrideFailureMessage($"steps and ramps add geometry ({build.Mesh.Triangles} triangles)").IsTrue();
        CheckBundles(bundles);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValidAndWound()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var module = new CoreModule();
        var ctx = Context(world, palette);
        foreach (var lod in Levels)
            foreach (var tile in CoreSite.Pieces(world).Select(p => TileGrid.At(p.Anchor, lod)).Distinct())
            {
                var output = module.Build(ctx, tile, lod);
                CheckBundles(output.Meshes);
                foreach (var m in output.Meshes)
                {
                    AssertThat(m.ColorIndices is not null).OverrideFailureMessage("palette IDs per vertex").IsTrue();
                    palette.Validate(m.Material.Spec);
                }
            }
        var first = module.Build(ctx, TileGrid.At(CoreSite.Pieces(world)[0].Anchor, LodLevel.L0), LodLevel.L0);
        var second = new CoreModule().Build(Context(Campus.Tests.RealMapFixture.Load().World, palette), TileGrid.At(CoreSite.Pieces(world)[0].Anchor, LodLevel.L0), LodLevel.L0);
        AssertThat(first.Meshes.Count == second.Meshes.Count && first.Meshes.Zip(second.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position))).OverrideFailureMessage("deterministic").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void L2CellsCarryNoPartThatOnlyBuildsAtL0AndL1()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var module = new CoreModule();
        var ctx = Context(world, Campus.Contracts.Tests.PaletteFixture.Catalog);
        // The oracle is the part definitions themselves; the context carries the catalogue captured from them (contract-36).
        var bench = new Campus.Gen.Parts.Street.BenchPart().Lods.Keys.ToArray();
        AssertThat(bench.SequenceEqual([LodLevel.L0, LodLevel.L1])).OverrideFailureMessage("the bench builds at L0 and L1 only").IsTrue();
        var catalog = ctx.PartLods ?? throw new InvalidOperationException("the fixture context carries the part catalogue");
        AssertThat(catalog.SupportedLods(new PartId(PartCatalog.Bench)).SequenceEqual(bench)).OverrideFailureMessage("the catalogue reads the bench's levels").IsTrue();
        AssertThat(catalog.Supports(new PartId(PartCatalog.StreetLamp), LodLevel.L2)).OverrideFailureMessage("the street lamp builds at L2").IsTrue();

        var plaza = CoreSite.Pieces(world)[0];
        var near = module.Build(ctx, TileGrid.At(plaza.Anchor, LodLevel.L0), LodLevel.L0);
        AssertThat(near.Parts.Any(p => p.Part.Value == PartCatalog.Bench)).OverrideFailureMessage("the plaza cell still seats its benches at L0").IsTrue();
        var l2Tiles = CoreSite.Pieces(world).Select(p => TileGrid.At(p.Anchor, LodLevel.L2)).Distinct().ToArray();
        AssertThat(l2Tiles.Length > 0).OverrideFailureMessage("the zone has L2 cells").IsTrue();
        foreach (var lod in new[] { LodLevel.L2, LodLevel.L3, LodLevel.L4 })
            foreach (var tile in CoreSite.Pieces(world).Select(p => TileGrid.At(p.Anchor, lod)).Distinct())
                foreach (var part in module.Build(ctx, tile, lod).Parts)
                {
                    AssertThat(catalog.Supports(part.Part, lod)).OverrideFailureMessage($"{lod} {tile}: {part.Part.Value} has no {lod} builder").IsTrue();
                    AssertThat(part.Part.Value != PartCatalog.Bench).OverrideFailureMessage($"{lod} {tile}: no bench").IsTrue();
                }
        var far = module.Build(ctx, TileGrid.At(plaza.Anchor, LodLevel.L2), LodLevel.L2);
        AssertThat(far.Parts.Count(p => p.Part.Value == PartCatalog.StreetLamp) == CoreStyle.RimLamps).OverrideFailureMessage("the rim lamps (L0 to L2) stay at L2").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PlacedPartsAreCheckedAgainstTheContextCatalogueOnly()
    {
        // contract-36: a missing catalogue is "not supplied" and an unknown part is an error; neither passes the part through.
        var world = Campus.Tests.RealMapFixture.Load().World;
        var plaza = CoreSite.Pieces(world)[0];
        var tile = TileGrid.At(plaza.Anchor, LodLevel.L0);
        var module = new CoreModule();
        _ = module.Build(Context(world), tile, LodLevel.L0);
        var bare = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
        InvalidOperationException? missingLods = null;
        try { module.Build(bare, tile, LodLevel.L0); }
        catch (InvalidOperationException ex) { missingLods = ex; }
        AssertThat(missingLods is InvalidOperationException).OverrideFailureMessage("placed parts without BuildContext.PartLods are refused").IsTrue();
        var empty = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u, PartLods = new PartLodCatalog([]) };
        KeyNotFoundException? unknownPart = null;
        try { module.Build(empty, tile, LodLevel.L0); }
        catch (KeyNotFoundException ex) { unknownPart = ex; }
        AssertThat(unknownPart is KeyNotFoundException).OverrideFailureMessage("a part the catalogue does not know is refused").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GnomonPointsTrueNorthAtTheLatitude()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var sundial = CoreSite.Pieces(world).OfType<Sundial>().Single();
        var g = sundial.Gnomon;
        AssertThat(MathF.Abs(g.X) < TestTolerance.Position && g.Z < 0).OverrideFailureMessage("gnomon in the north meridian plane, pointing north (−z)").IsTrue();
        var elevation = MathF.Asin(g.Y) * 180f / MathF.PI;
        AssertThat(MathF.Abs(elevation - world.Features[CoreIds.Sundial].Number("gnomonElevationDeg")) < TestTolerance.AngleDegrees).OverrideFailureMessage($"elevation {elevation}° equals the latitude").IsTrue();
        AssertThat(g.Y < 0.55f).OverrideFailureMessage("dial face normal y below the snow threshold (MAP-QSH-CORE)").IsTrue();
        AssertThat(sundial.Collide().All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every collider has a foot material").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TerrainOverlayIsWellFormed()
    {
        var path = Path.Combine(Campus.Tests.RealMapFixture.DataDirectory(), "world", "core.terrain.json");
        // The contract parser checks field names, discriminators and numeric bounds.
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(path));
        AssertThat(file.Zone.Value == "core" && file.Owner.Value == "WP-16").OverrideFailureMessage("zone and owner").IsTrue();
        var world = Campus.Tests.RealMapFixture.Load().World;
        var roads = world.Network.Roads.Select(r => r.Id).ToHashSet();
        bool Exists(FeatureId id) => world.Features.ContainsKey(id) || roads.Contains(id);
        foreach (var op in file.Ops)
        {
            AssertThat(op.Id.StartsWith("design:terrain:core-", StringComparison.Ordinal)).OverrideFailureMessage($"core terrain ID {op.Id}").IsTrue();
            AssertThat(op.Reason.Length > 10).OverrideFailureMessage($"{op.Id} has a reason").IsTrue();
            IEnumerable<FeatureId> references = op switch
            {
                TerrainPadOp pad => [pad.Feature],
                TerrainCorridorOp corridor => [corridor.Road, .. corridor.Steps.Select(s => s.Structure)],
                TerrainChannelOp channel => channel.Crossings.Select(c => c.Road),
                _ => [],
            };
            foreach (var id in references) AssertThat(Exists(id)).OverrideFailureMessage($"{op.Id} references the real entity {id.Value}").IsTrue();
        }
    }


    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GinkgoSidewalksGiveWayToTheWalks()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        foreach (var (removed, walk, kept) in CoreIds.GinkgoSidewalks)
        {
            AssertThat(world.Network.Roads.All(road => road.Id.Value != removed) && !world.Features.ContainsKey(new(removed)))
                .OverrideFailureMessage("The removed duplicate sidewalk remains in the world.").IsTrue();
            var walkLine = world.Network.Roads.Single(road => road.Id.Value == walk).Line.Points;
            var piece = world.Network.Roads.Single(road => road.Id.Value == kept);
            var record = world.Features[piece.Id];
            AssertThat(record.Geometry is PolylineGeometry authored && authored.Line.Points.SequenceEqual(piece.Line.Points) &&
                WorldProps.TryNumber(record.Props, "width") == piece.Width).OverrideFailureMessage("The road differs from its authored record.").IsTrue();
            AssertThat(Vector2.Distance(piece.Line.Points[0], walkLine[^1]) <= TestTolerance.Position ||
                Vector2.Distance(piece.Line.Points[^1], walkLine[^1]) <= TestTolerance.Position).OverrideFailureMessage("The kept sidewalk must join its walk.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GinkgoWalksKeepOffWallsAndWaterAndJoinExistingWalks()
    {
        // max-108 0h-a: the walks between the 7 m and 14 m ginkgo rows keep their paved edge 3 m or more off every building and
        // water outline and waterway, and each end lies on the paving of an existing walk, so no end is a stub.
        const float half = 1.25f;
        var world = Campus.Tests.RealMapFixture.Load().World;
        var avenue = world.Network.Roads.Single(r => r.Id.Value == CoreIds.GinkgoAvenue).Line.Points;
        // Any other walk of the network counts, design or OSM: the kept sidewalk ends at the bridges are design roads.
        var walks = world.Network.Roads.Where(r => !CoreIds.GinkgoWalks.Contains(r.Id.Value) &&
            WorldProps.TryText(r.Props, "highway") is "footway" or "path" or "pedestrian").ToArray();
        foreach (var id in CoreIds.GinkgoWalks)
        {
            var feature = world.Features[new(id)];
            AssertThat(feature.Kind.Value == "road" && feature.Zone?.Value == "core").OverrideFailureMessage($"{id}: a core road feature").IsTrue();
            AssertThat(WorldProps.TryText(feature.Props, "highway") == "footway" && WorldProps.TryNumber(feature.Props, "width") == 2 * half).OverrideFailureMessage($"{id}: a 2.5 m footway").IsTrue();
            var line = ((PolylineGeometry)feature.Geometry).Line.Points;
            var min = line.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(20f);
            var max = line.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(20f);
            var outlines = Outlines(world, "building").Concat(Outlines(world, "water"))
                .Where(r => r.Outer.Any(q => q.X >= min.X && q.X <= max.X && q.Y >= min.Y && q.Y <= max.Y)).ToArray();
            for (var s = 0f; s <= PlanLines.Length(line); s += 0.5f)
            {
                var p = PlanLines.Sample(line, s).Point;
                var wall = outlines.Min(r => PlanGeometry.Contains(r, p) ? 0f
                    : r.Holes.Aggregate(PlanGeometry.DistanceToRing(p, r.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h))));
                AssertThat(wall - half >= 3f).OverrideFailureMessage($"{id}: paving {wall - half:0.00} m off a wall or water edge at ({p.X:0.0}, {p.Y:0.0})").IsTrue();
                var river = world.Network.Waterways.Min(w => MathF.Sqrt(PlanLines.PolylineDistanceSquared(w.Line.Points, p)) - w.Width * 0.5f);
                AssertThat(river - half >= 3f).OverrideFailureMessage($"{id}: paving {river - half:0.00} m off a waterway at ({p.X:0.0}, {p.Y:0.0})").IsTrue();
                var offset = MathF.Sqrt(PlanLines.PolylineDistanceSquared(avenue, p));
                AssertThat(MathF.Abs(offset - 10.5f) < 3 * TestTolerance.Layout).OverrideFailureMessage($"{id}: {offset:0.00} m off the avenue's centre line at s {s:0.0}").IsTrue();
            }
            foreach (var end in new[] { line[0], line[^1] })
                AssertThat(walks.Any(w => MathF.Sqrt(PlanLines.PolylineDistanceSquared(w.Line.Points, end)) <= half + MathF.Max(w.Width, 2f) * 0.5f)).OverrideFailureMessage($"{id}: the end ({end.X:0.0}, {end.Y:0.0}) lies on an existing walk's paving").IsTrue();
        }
    }

    // ---------------------------------------------------------------- fixtures

    /// <summary>Polygons of one kind, part by part where a feature has parts (a part's own kind wins, as the world model reads it).</summary>
    private static IEnumerable<PolygonRings> Outlines(IWorldModel world, string kind)
    {
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == kind) foreach (var rings in WorldFeatureGeometry.PolygonsOf(feature.Geometry)) yield return rings;
                continue;
            }
            foreach (var part in feature.Parts)
                if ((WorldProps.TryText(part.Props, "kind") ?? feature.Kind.Value) == kind)
                    foreach (var rings in WorldFeatureGeometry.PolygonsOf(part.Geometry)) yield return rings;
        }
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
                AssertThat(Vector3.Dot(g, n) <= 1e-6f).OverrideFailureMessage("front-face winding").IsTrue();
            }
        }
    }

    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

    private static BuildContext Context(IWorldModel world, PaletteCatalog? palette = null) => new()
    {
        World = world,
        Map = null!,
        WorldSeed = 1u,
        Catalog = palette ?? Campus.Contracts.Tests.PaletteFixture.Catalog,
        PartLods = PartLodFixture.Catalog,
    };

    /// <summary>A sunken disc at <paramref name="floor"/> out to <paramref name="radius"/>, rising at <paramref name="grade"/> back to 0.</summary>
    private static FormulaTerrain BowlTerrain(Vector2 centre, float radius, float floor, float grade) =>
        new((x, z) => MathF.Min(0f, floor + grade * MathF.Max(0f, Vector2.Distance(new(x, z), centre) - radius)));
}
