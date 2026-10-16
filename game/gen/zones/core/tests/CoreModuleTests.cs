using System.Numerics;
using System.Text.Json;
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
    public void RealMapLoadsTheOverlayAndBothAxisPieces()
    {
        var world = RealWorld();
        var pieces = CoreSite.Pieces(world);
        Require(pieces.Select(p => p.Id).SequenceEqual(["time-plaza", "sundial", "library-shell"]), "plaza, sundial and library pieces in order");
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
        var build = new CoreBuild(null);
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
                    Require(m.ColorIds is not null, "palette IDs per vertex");
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

    // ---------------------------------------------------------------- fixtures

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

    private static BuildContext Context(IWorldModel world, PaletteRegistry? palette = null) => new()
    {
        World = world,
        Map = null!,
        WorldSeed = 1u,
        Palette = palette,
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

    private static PaletteRegistry Palette()
    {
        var dir = Path.Combine(FindRepository(), "game", "style", "palette");
        var entries = new List<PaletteEntry>();
        foreach (var file in new[] { Path.Combine(dir, "core.json") }.Concat(Directory.EnumerateFiles(Path.Combine(dir, "ext"), "WP-*.json").Order(StringComparer.Ordinal)))
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return new PaletteRegistry(entries);
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
