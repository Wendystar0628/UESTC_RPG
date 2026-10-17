using System.Numerics;
using System.Text;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Kernel.Maps;
using Campus.Kernel.Stubs;
using Campus.Kernel.Stubs.Foundation;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeInputTests
{
    [TestCase]
    public void CompleteCatalogHashesAllMetadataInCanonicalOrder()
    {
        var context = Context(Inputs("campus", "planting"));
        var globals = Globals("species");
        var legacy = new PaletteRegistry([
            new(new("fixture.weather"), 40, new Vector3(.5f), "fixture", false, PaletteRole.Weather),
            new(new("fixture.lit"), 64, new Vector3(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.shade"), 65, new Vector3(.5f), "fixture", false, PaletteRole.Shade)]);
        PaletteEntryV2[] entries = [
            new(new("fixture.high"), new(256), new(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.high.shade"), new(257), new(.5f), "fixture", false, PaletteRole.Shade)];
        var baseline = Full(new(legacy, entries));
        if (baseline != Full(new(legacy, entries.Reverse()))) throw new InvalidOperationException("Catalog insertion order changed its identity.");
        var baseOnly = Full(new PaletteCatalog(legacy, []));
        if (baseline == baseOnly) throw new InvalidOperationException("Extension metadata did not affect the input identity.");
        foreach (var changed in new[]
        {
            new PaletteCatalog(legacy, entries.Select(e => e with { Token = new(e.Token.Value + ".renamed") })),
            new PaletteCatalog(legacy, entries.Select(e => e with { Index = new(e.Index.Value + 2) })),
            new PaletteCatalog(legacy, entries.Select(e => e with { MaterialClass = "other" })),
            new PaletteCatalog(legacy, entries.Select(e => e with { Albedo = new Vector3(.25f) })),
            new PaletteCatalog(new PaletteRegistry(legacy.Entries.Select(e => e.Id == 40 ? e with { Role = PaletteRole.Emissive } : e)), entries)
        }) if (baseline == Full(changed)) throw new InvalidOperationException("A catalog token/index/material/albedo/role edit hit old bytes.");
        var key = BakeRequestKeys.Tile(context.Descriptor.Id, "fixture.content", new(0, 0), LodLevel.L0);
        if (BakeInputFingerprint.Identity(key, baseline, "code") == BakeInputFingerprint.Identity(key, baseOnly, "code"))
            throw new InvalidOperationException("Catalog domain did not isolate cache lookup.");
        string Full(PaletteCatalog value) => BakeInputFingerprint.Compute(context, 1,
            BakeInputFingerprint.ReadInputs(context), value, globals);
    }

    [TestCase]
    public void InputReaderBorrowsPublishedVersionAndNeverFallsBackToDataRoot()
    {
        var raw = Encoding.UTF8.GetBytes("old-campus");
        var old = Context(BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = raw }));
        var oldInputs = BakeInputFingerprint.ReadInputs(old);
        var before = Hash(old, Globals("species-a"));
        raw[0] = (byte)'X';
        var next = Context(Inputs("new-campus", "planting-a"));
        if (!ReferenceEquals(oldInputs, old.SourceInputs.Entries) || ReferenceEquals(oldInputs, BakeInputFingerprint.ReadInputs(next)) ||
            Encoding.UTF8.GetString(oldInputs["campus.json"].Span) != "old-campus" || Hash(old, Globals("species-a")) != before)
            throw new InvalidOperationException("An old map job changed input versions or refroze its bytes.");
        Reject(() => BakeInputFingerprint.ReadInputs(Context(BuildInputSnapshot.Empty)));
        Reject(() => BakeInputFingerprint.ReadInputs(Context(BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        { ["network.json"] = Encoding.UTF8.GetBytes("{}") }))));
        Reject(() => BakeInputFingerprint.ReadInputs(Context(BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        { ["campus.json"] = ReadOnlyMemory<byte>.Empty }))));
    }

    [TestCase]
    public void SpeciesInvalidatesBothMapsWhilePlantingInvalidatesOnlyItsOwnMapAndIdentity()
    {
        var a = Context(Inputs("campus-a", "planting-a"), "fixture-a");
        var b = Context(Inputs("campus-b", "planting-b"), "fixture-b");
        var first = Globals("species-a");
        var changed = Globals("species-b");
        var aHash = Hash(a, first); var bHash = Hash(b, first);
        if (aHash == Hash(a, changed) || bHash == Hash(b, changed))
            throw new InvalidOperationException("A global species edit failed to invalidate a consuming map.");
        var edited = Context(Inputs("campus-a", "planting-a "), "fixture-a");
        if (Hash(edited, first) == aHash || Hash(b, first) != bHash)
            throw new InvalidOperationException("A raw planting edit changed the wrong map's identity.");
        var key = BakeRequestKeys.Tile(a.Descriptor.Id, "fixture.content", new(-1, 2), LodLevel.L0);
        if (BakeInputFingerprint.Identity(key, aHash, "code") == BakeInputFingerprint.Identity(key, Hash(edited, first), "code"))
            throw new InvalidOperationException("The changed production hash did not enter the cache key.");
    }

    [TestCase]
    public void ProductionHasherSeparatesScopesIgnoresInsertionOrderAndExcludesPresentationBytes()
    {
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["campus.json"] = Bytes("campus"),
            ["vegetation/planting.json"] = Bytes("planting"),
            ["overlay/terrain/fixture.terrain.json"] = Bytes("terrain"),
            ["atlas/labels.json"] = Bytes("labels-a"),
            ["map.json"] = Bytes("camera/time/title-a"),
            ["sky.json"] = Bytes("weather-a")
        };
        var a = Context(BuildInputSnapshot.Freeze(files));
        var reversed = Context(BuildInputSnapshot.Freeze(files.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)));
        var globals = Globals("species");
        var hash = Hash(a, globals);
        if (hash != Hash(reversed, globals)) throw new InvalidOperationException("Input insertion order changed the hash.");
        files["map.json"] = Bytes("camera/time/title-b"); files["sky.json"] = Bytes("weather-b"); files["atlas/labels.json"] = Bytes("labels-b");
        var presentationOnly = Context(BuildInputSnapshot.Freeze(files));
        if (hash != Hash(presentationOnly, globals) || Hash(a, globals, atlas: true) == Hash(presentationOnly, globals, atlas: true))
            throw new InvalidOperationException("Presentation/atlas dependencies leaked across bake variants.");
        var globalOnly = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["vegetation/planting.json"] = Bytes("same") });
        var mapOnly = Context(BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = Bytes("campus"), ["vegetation/planting.json"] = Bytes("same") }));
        var bare = Context(BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = Bytes("campus") }));
        if (Hash(bare, globalOnly) == Hash(mapOnly, BuildInputSnapshot.Empty))
            throw new InvalidOperationException("The same key/bytes in global and map scopes collided.");
    }

    [TestCase]
    public void AuthoredTerrainDefaultsInvalidateGenerationWithoutHashingAllMapSettings()
    {
        var context = Context(Inputs("campus", "planting"));
        var globals = Globals("species");
        var baseline = Hash(context, globals);
        var defaults = context.Descriptor.TerrainDefaults!;
        foreach (var edited in new[]
        {
            defaults with { LakeMaxDepth = defaults.LakeMaxDepth + .5f }, defaults with { LakeBankSlope = "1:6" },
            defaults with { ChannelDepth = defaults.ChannelDepth + .5f }, defaults with { ChannelBankSlope = "1:7" },
            defaults with { CorridorShoulder = defaults.CorridorShoulder + .5f }
        })
            if (Hash(context with { Descriptor = context.Descriptor with { TerrainDefaults = edited } }, globals) == baseline)
                throw new InvalidOperationException("Changed terrain generation defaults hit old geometry.");
        var presentation = context with { Descriptor = context.Descriptor with { DefaultSpawn = "other-camera", Title = null } };
        if (Hash(presentation, globals) != baseline) throw new InvalidOperationException("A camera-only descriptor edit invalidated geometry.");
    }

    [TestCase]
    public void AuthoredBackdropOutskirtsAndOverlayShardPathsInvalidateTheirCapturedBytes()
    {
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["campus.json"] = Bytes("campus"),
            ["scenery/far.asset"] = Bytes("far-a"),
            ["edge/ring.json"] = Bytes("edge-a"),
            ["overlay/props.d/0010-fixture.json"] = Bytes("shard-a"),
            ["backdrop.json"] = Bytes("unused-old-path")
        };
        var initial = Context(BuildInputSnapshot.Freeze(files));
        var map = initial with
        {
            Descriptor = initial.Descriptor with
            { Env = initial.Descriptor.Env with { Backdrop = "scenery/far.asset", Outskirts = "edge/ring.json" } }
        };
        var globals = Globals("species");
        var baseline = Hash(map, globals);
        foreach (var path in new[] { "scenery/far.asset", "edge/ring.json", "overlay/props.d/0010-fixture.json" })
        {
            var changed = new Dictionary<string, ReadOnlyMemory<byte>>(files, StringComparer.Ordinal) { [path] = Bytes("edited") };
            if (Hash(map with { SourceInputs = BuildInputSnapshot.Freeze(changed) }, globals) == baseline)
                throw new InvalidOperationException("An authored input path was omitted from the bake identity: " + path);
        }
        files["backdrop.json"] = Bytes("irrelevant-retired-path");
        if (Hash(map with { SourceInputs = BuildInputSnapshot.Freeze(files) }, globals) != baseline)
            throw new InvalidOperationException("A retired fixed backdrop path still invalidates this map.");
    }

    [TestCase]
    public void WindowContextForwardingRetainsFrozenGlobalMapAndContributorReferences()
    {
        var map = Context(Inputs("campus", "planting"));
        var globals = Globals("species");
        var contributors = BuildInputAssembly.FreezeGroundMaskContributors([new Contributor()]);
        var partLods = new PartLodCatalog([new(new PartId("fixture.part"), new BakeFixturePart())]);
        var catalog = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var original = new BuildContext
        {
            World = map.World,
            Map = map.Descriptor,
            WorldSeed = 1,
            Catalog = catalog,
            SourceInputs = BakeInputFingerprint.ReadInputs(map),
            GlobalInputs = globals,
            GroundMaskContributors = contributors,
            PartLods = partLods
        };
        var forwarded = BakeWindowPreparation.Context(original, "hash", "module-code", null);
        if (!ReferenceEquals(original.SourceInputs, forwarded.SourceInputs) || !ReferenceEquals(globals, forwarded.GlobalInputs) ||
            !ReferenceEquals(contributors, forwarded.GroundMaskContributors) || !ReferenceEquals(partLods, forwarded.PartLods) ||
            !ReferenceEquals(catalog, forwarded.Catalog) ||
            !forwarded.PartLods!.Supports(new("fixture.part"), LodLevel.L2) || forwarded.PartLods.Supports(new("fixture.part"), LodLevel.L4))
            throw new InvalidOperationException("Window prepass copied or dropped a frozen input/capability version.");
    }

    private static string Hash(TestContext context, BuildInputSnapshot globals, bool atlas = false) =>
        BakeInputFingerprint.Compute(context, 1, BakeInputFingerprint.ReadInputs(context), Campus.Contracts.Tests.PaletteFixture.Catalog, globals, atlas);
    private static BuildInputSnapshot Globals(string text) => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    { ["vegetation/species.json"] = Bytes(text) });
    private static BuildInputSnapshot Inputs(string campus, string planting) => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    { ["campus.json"] = Bytes(campus), ["vegetation/planting.json"] = Bytes(planting) });
    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
    private static TestContext Context(BuildInputSnapshot inputs, string id = "fixture")
    {
        var descriptor = new MapDescriptor(new(id), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
            new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), [])
        { TerrainDefaults = new("1:5", 1.5f, "1:4", 1.2f, .75f) };
        return new(descriptor, new StubWorldModel(new StubSpatialIndex(), new InteriorIndexStub(), new StubFidelityModel()), inputs);
    }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException error) when (error.Message.Contains("MapLoadInput.SourceInputs", StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("A missing loading-boundary snapshot was accepted.");
    }
    private sealed record TestContext(MapDescriptor Descriptor, IWorldModel World, BuildInputSnapshot SourceInputs) : IMapContext
    {
        public string DataRoot => "must-not-read-fixture-data";
        public string BakeRoot => "unused-fixture-bake";
    }
    private sealed class Contributor : IGroundMaskContributor
    {
        public string Id => "fixture.leaves";
        public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves;
        public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2) =>
            throw new InvalidOperationException("Input preparation must not paint geometry masks.");
    }
}

internal sealed class BakeFixturePart : IPartDef
{
    public IReadOnlyList<LodLevel> SupportedLods => [LodLevel.L0, LodLevel.L2];
}
