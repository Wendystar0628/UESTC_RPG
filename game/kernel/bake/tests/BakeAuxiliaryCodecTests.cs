using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;
using Campus.Contracts.Parts;
using Campus.Contracts.Maps;
using Campus.Tests;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeAuxiliaryCodecTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void VegetationAndFaunaPreserveFrozenPrototypesAndAllSurfaceChannels()
    {
        var surface = Surface();
        var prototype = new GeometryPrototype("fixture.prototype", [GeometryBuffer.Freeze(surface)], new(Vector3.Zero, Vector3.One));
        var record = new float[InstanceStreamLayout.FloatsPerInstance];
        InstanceStreamLayout.Write(record, Matrix4x4.CreateTranslation(12, 3, 14), new(1, 2, 3, 4));
        var bounds = new Box3(new(10, 1, 10), new(15, 8, 16));
        var key = BakeRequestKeys.Tile(new("fixture"), BakeModuleIds.VegetationTrees, default, LodLevel.L0);
        var trees = new VegetationTileContent(key, default,
            [new("fixture.mother", record, [prototype, prototype, prototype, prototype], [bounds])]);
        var bytes = BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", trees);
        var decoded = (VegetationTileContent)BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", key, bytes);
        var mother = decoded.Mothers.Single();
        if (!mother.Records.Span.SequenceEqual(record) || mother.Bounds.Single() != bounds)
            throw new InvalidOperationException("Vegetation lost frozen mother data.");
        Verify(mother.Prototypes[3].Surfaces.Single());
        if (!bytes.SequenceEqual(BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", decoded)))
            throw new InvalidOperationException("Vegetation round trip changed canonical bytes.");

        key = key with { Module = BakeModuleIds.FaunaRuntime };
        var text = new SceneTextRun(3.25f, [new(new(1, 2, 3, 4), new(.1f, .2f, .3f, .4f), 2)]);
        var part = new PlacedPart(new(PartCatalog.Signpost), new SignParams { PrintedText = text }, Matrix4x4.CreateTranslation(4, 5, 6))
            { Tint = new("fixture.tint"), IgnoreOcclusionCulling = true };
        var ring = new PolygonRings([new(1, 2), new(3, 4), new(5, 6)], [[new(2, 3), new(3, 3), new(2, 4)]]);
        var output = new ContentOutput([surface], [part], [], [new(new("fixture.occupancy"), ring, -1, 8)])
        {
            Instances = [new("fixture.instance", prototype, record, bounds)],
            Windows = [new(0, 2, new("window.general"), 0, 2, 1, Vector3.One, Vector3.UnitX, Vector3.UnitY, 2, 1, 1, [.75f]) { Skip = [1] }]
        };
        var fauna = new FaunaTileContent(key, default, new(
            [new("fixture.site", FaunaKind.Cat, bounds, [output, ContentOutput.Empty, output])], ["fixture log"]));
        bytes = BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", fauna);
        var animals = (FaunaTileContent)BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", key, bytes);
        var site = animals.Content.Sites.Single();
        if (site.Bounds != bounds || site.Sets.Count != 3 || site.Sets[1].Meshes.Count != 0 ||
            !site.Sets[2].Instances.Single().Data.Span.SequenceEqual(record))
            throw new InvalidOperationException("Fauna lost its three-state order or instance records.");
        Verify(site.Sets[0].Instances.Single().Prototype.Surfaces.Single());
        var restoredPart = site.Sets[0].Parts.Single();
        if (restoredPart.At != part.At || restoredPart.Tint != part.Tint || !restoredPart.IgnoreOcclusionCulling ||
            restoredPart.Params is not SignParams parameters || parameters.PrintedText != text ||
            !site.Sets[0].Occupancy![0].Rings.Holes[0].SequenceEqual(ring.Holes[0]) ||
            !site.Sets[0].Windows[0].SillY.SequenceEqual(output.Windows[0].SillY) || !site.Sets[0].Windows[0].Skip.SequenceEqual(output.Windows[0].Skip))
            throw new InvalidOperationException("Content output lost typed part parameters, placement or nested numeric channels.");
        if (!bytes.SequenceEqual(BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", animals)))
            throw new InvalidOperationException("Fauna round trip changed canonical bytes.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NightlightSeparatesCatalogIdsFromLocalReflectionIndicesAndRejectsCorruption()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), BakeModuleIds.NightlightLamps, default, LodLevel.L0);
        var data = new float[GlowSet.Stride];
        InstanceStreamLayout.Write(data, Matrix4x4.Identity, new(321, .2f, 0, 0));
        var lamps = new NightlightTileContent(key, default, [321], new(GlowKind.Lamp, 1, data, new(Vector3.Zero, Vector3.One)), []);
        var bytes = BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", lamps);
        var decoded = (NightlightTileContent)BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", key, bytes);
        if (decoded.LampIds.Single() != 321 || decoded.Glow.Buffer[12] != 321 || decoded.ReflectionWindowIndices.Count != 0)
            throw new InvalidOperationException("Lamp IDs were renumbered by the tile codec.");
        var windows = new NightlightTileContent(key with { Module = BakeModuleIds.NightlightWindows }, default, [],
            lamps.Glow with { Kind = GlowKind.Window }, [0]);
        bytes = BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", windows);
        var restored = (NightlightTileContent)BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", windows.Key, bytes);
        if (restored.ReflectionWindowIndices.Single() != 0 || restored.LampIds.Count != 0)
            throw new InvalidOperationException("Window reflections lost their local buffer index.");
        Reject(() => BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", windows with { ReflectionWindowIndices = [1] }));
        Reject(() => BakeProductCodec.Encode(BakePayloadClass.Product, "fixture", lamps with { LampIds = [320] }));
        Reject(() => BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", windows.Key, bytes[..^1]));
        Reject(() => BakeProductCodec.Decode(BakePayloadClass.Product, "fixture", windows.Key, bytes.Concat(new byte[] { 0 }).ToArray()));
        var count = BakeCacheStore.MaximumPayloadBytes / (GlowSet.Stride * sizeof(float)) + 1;
        var large = windows with { Glow = windows.Glow with { Count = count, Buffer = new float[count * GlowSet.Stride] }, ReflectionWindowIndices = [] };
        var largeBytes = BakeProductCodec.Encode(BakePayloadClass.Product, "fixture/large", large);
        if (largeBytes.Length <= BakeCacheStore.MaximumPayloadBytes ||
            ((NightlightTileContent)BakeProductCodec.Decode(BakePayloadClass.Product, "fixture/large", windows.Key, largeBytes)).Glow.Count != count)
            throw new InvalidOperationException("CPU channels or containers still use the single-render-upload boundary.");
        Reject(() => BakeProductCodec.Encode(BakePayloadClass.Tile, "fixture/large", large));
        Reject(() => BakeProductCodec.Decode(BakePayloadClass.Tile, "fixture/large", windows.Key, largeBytes));
        Reject(() => BakeProductCodec.Decode(BakePayloadClass.Product, "fixture/large", key with { Module = "fixture.render" }, largeBytes));
        Reject(() => BakeProductCodec.Decode(BakePayloadClass.Product, "fixture/large", key with { Module = "collision.world", Detail = "C/0/0" }, largeBytes));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void TerrainAndResidentCachesPreserveLargeChannelsAndNestedDirectories()
    {
        var map = new MapId("fixture"); var key = new BakeKey(map, "terrain", "meta");
        var ring = new PolygonRings([Vector2.Zero, Vector2.UnitX, Vector2.One], []);
        var span = new TerrainBankSpan("fixture.bank", [new("fixture.water")], [], [],
            new([Vector2.Zero, new(10, 0)]), 2, 8, 2, TerrainBankKind.Soft);
        var stations = new[] { 0d, Math.BitIncrement(2d), 10d };
        var terrain = new TerrainFrozenData(new(Vector2.Zero, new(64)), new("common", [new(default, "cell")]),
            [new(new("fixture.pad"), .125f)], [new(new("fixture.water"), .375f)], [new(new("fixture.water"), [ring], [ring])],
            [new(span, stations, [short.MinValue, 1, short.MaxValue])], [new(Vector2.Zero, Vector2.One)],
            Enumerable.Range(0, 600_000).Select(i => (short)(i % short.MaxValue)).ToArray(), ["fixture diagnostic"]);
        var bytes = TileBundleCodec.EncodeCache(BakePayloadClass.Product, key, terrain);
        var restored = TileBundleCodec.DecodeCache<TerrainFrozenData>(BakePayloadClass.Product, bytes, key);
        if (!restored.DistantHeights.SequenceEqual(terrain.DistantHeights) || !restored.Banks[0].Stations.SequenceEqual(stations) ||
            !restored.Banks[0].Heights.SequenceEqual(terrain.Banks[0].Heights) || !restored.Pads.SequenceEqual(terrain.Pads) ||
            !restored.WaterLevels.SequenceEqual(terrain.WaterLevels) || !restored.Supports.SequenceEqual(terrain.Supports) ||
            !restored.Waters[0].WadeAreas[0].Outer.SequenceEqual(ring.Outer) || !restored.Diagnostics.SequenceEqual(terrain.Diagnostics))
            throw new InvalidOperationException("Terrain cache lost frozen numeric data.");
        Reject(() => TileBundleCodec.DecodeCache<TerrainFrozenData>(BakePayloadClass.Product, bytes[..^1], key));
        Reject(() => TileBundleCodec.DecodeCache<TerrainFrozenData>(BakePayloadClass.Product, [.. bytes, 0], key));

        key = key with { Module = "resident" };
        var facade = new WindowFacadeDecl(0, 2, new("window.general"), 0, 2, 1, Vector3.One, Vector3.UnitX, Vector3.UnitY,
            2, 1, 1, [.75f]) { Skip = [1] };
        var windows = new WindowMetadata([facade], []);
        var catalog = new NightlightCatalog([new(Vector3.One, 2, 3, 4, .5f, 6, 7, .25f, 9, 10)],
            Enumerable.Range(0, 300_000).Select(i => i * .25f).ToArray(), new float[] { -.5f, .25f }, 2, 1);
        var lighting = new LightInventoryDocument(map, [], windows.Facades, windows.Windows, catalog)
        { WindowNumbering = new(map, new string('a', 64), [new(new("fixture.building"), "fixture.grid", 0, 2, 1)]) };
        var layout = new InteriorLayout(new("fixture.building"), [new(0, .125f, 3, ring.Outer)],
            [new(0, 1, ZoneKind.Room, ring.Outer, new("window.general"), 123, true)], [], [], [], [0], [0], new(Vector3.Zero, Vector3.One), 321);
        var resident = new BakeResidentData(new(lighting, new(map, [])), windows, [], [layout],
            new(new Dictionary<string, IReadOnlyList<TileKey>> { ["fixture.module"] = [new(1, 2), new(-3, 4)] },
                [new(new(map, "fixture.module", "L0/1/2"), layout.Bounds)]), [new(1, -2)]);
        bytes = TileBundleCodec.EncodeCache(BakePayloadClass.Product, key, resident);
        var saved = TileBundleCodec.DecodeCache<BakeResidentData>(BakePayloadClass.Product, bytes, key);
        if (!saved.Metadata.Lighting.Nightlight.LampTable.Span.SequenceEqual(catalog.LampTable.Span) ||
            !saved.Metadata.Lighting.Nightlight.LampGrid.Span.SequenceEqual(catalog.LampGrid.Span) ||
            !saved.Metadata.Lighting.Nightlight.Lamps.SequenceEqual(catalog.Lamps) ||
            !saved.Metadata.Lighting.WindowNumbering!.Allocations.SequenceEqual(lighting.WindowNumbering!.Allocations) ||
            !saved.Windows.Facades[0].SillY.SequenceEqual(facade.SillY) || !saved.Windows.Facades[0].Skip.SequenceEqual(facade.Skip) ||
            !saved.Layouts[0].Storeys[0].Footprint.SequenceEqual(ring.Outer) || !saved.Layouts[0].Zones[0].Ring.SequenceEqual(ring.Outer) ||
            !saved.Directory.Modules["fixture.module"].SequenceEqual(resident.Directory.Modules["fixture.module"]) ||
            !saved.Directory.Auxiliary.SequenceEqual(resident.Directory.Auxiliary) || !saved.TerrainPoints.SequenceEqual(resident.TerrainPoints))
            throw new InvalidOperationException("Resident cache lost lamp, window, interior or directory data.");
        Reject(() => TileBundleCodec.DecodeCache<BakeResidentData>(BakePayloadClass.Product, bytes, key with { Detail = "source-index" }));
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed auxiliary data was accepted.");
    }

    private static GeometryBundle Surface() => new(new(new(new("fixture.lit"), new("fixture.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2],
        [.2f, .5f, 1], [0, 0, 1, 0, 0, 1], [65536, 31, 65536, 31, 65536, 31], [new(.25f, [0, 1, 2])], new VariantTag())
    {
        Shader = ShaderId.Water, Reflect = true, IgnoreOcclusionCulling = true,
        ColorIndices = [256, 257, 258], Flags = [2, 2, 2], Reserved = [1, 2, 3],
        Custom0 = [1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4],
        Custom1 = [4, 3, 2, 1, 4, 3, 2, 1, 4, 3, 2, 1],
        Custom2 = [0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1]
    };

    private static void Verify(GeometryBuffer surface)
    {
        var expected = GeometryBuffer.Freeze(Surface());
        if (surface.Material != expected.Material || surface.Shader != expected.Shader || surface.Variant != expected.Variant ||
            surface.Reflect != expected.Reflect || surface.IgnoreOcclusionCulling != expected.IgnoreOcclusionCulling ||
            !surface.Position.Span.SequenceEqual(expected.Position.Span) || !surface.Normal.Span.SequenceEqual(expected.Normal.Span) ||
            !surface.Index.Span.SequenceEqual(expected.Index.Span) || !Equal(surface.Ao, expected.Ao) ||
            !Equal(surface.Uv, expected.Uv) || !Equal(surface.Uv2, expected.Uv2) ||
            !Equal(surface.ColorIndices, expected.ColorIndices) || !Equal(surface.Flags, expected.Flags) || !Equal(surface.Reserved, expected.Reserved) ||
            !Equal(surface.Custom0, expected.Custom0) || !Equal(surface.Custom1, expected.Custom1) || !Equal(surface.Custom2, expected.Custom2) ||
            surface.Lods.Single().Error != expected.Lods.Single().Error || !surface.Lods.Single().Index.Span.SequenceEqual(expected.Lods.Single().Index.Span))
            throw new InvalidOperationException("Frozen prototype surface lost a geometry channel or metadata field.");
    }

    private static bool Equal<T>(ReadOnlyMemory<T>? a, ReadOnlyMemory<T>? b) where T : IEquatable<T> =>
        a.HasValue == b.HasValue && (!a.HasValue || a.Value.Span.SequenceEqual(b!.Value.Span));
}
