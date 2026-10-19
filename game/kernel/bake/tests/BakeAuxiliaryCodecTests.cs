using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;
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
            [new("fixture.mother", new("WP-23"), record, [prototype, prototype, prototype, prototype], [bounds])]);
        var bytes = BakeProductCodec.Encode("fixture", trees);
        var decoded = (VegetationTileContent)BakeProductCodec.Decode("fixture", key, bytes);
        var mother = decoded.Mothers.Single();
        if (!mother.Records.Span.SequenceEqual(record) || mother.Owner != trees.Mothers[0].Owner || mother.Bounds.Single() != bounds)
            throw new InvalidOperationException("Vegetation lost frozen mother data.");
        Verify(mother.Prototypes[3].Surfaces.Single());
        if (!bytes.SequenceEqual(BakeProductCodec.Encode("fixture", decoded)))
            throw new InvalidOperationException("Vegetation round trip changed canonical bytes.");

        key = key with { Module = BakeModuleIds.FaunaRuntime };
        var output = new ContentOutput([surface], [])
        {
            Instances = [new("fixture.instance", prototype, record, bounds)]
        };
        var fauna = new FaunaTileContent(key, default, new(
            [new("fixture.site", FaunaKind.Cat, bounds, [output, ContentOutput.Empty, output])], ["fixture log"]));
        bytes = BakeProductCodec.Encode("fixture", fauna);
        var animals = (FaunaTileContent)BakeProductCodec.Decode("fixture", key, bytes);
        var site = animals.Content.Sites.Single();
        if (site.Bounds != bounds || site.Sets.Count != 3 || site.Sets[1].Meshes.Count != 0 ||
            !site.Sets[2].Instances.Single().Data.Span.SequenceEqual(record))
            throw new InvalidOperationException("Fauna lost its three-state order or instance records.");
        Verify(site.Sets[0].Instances.Single().Prototype.Surfaces.Single());
        if (!bytes.SequenceEqual(BakeProductCodec.Encode("fixture", animals)))
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
        var bytes = BakeProductCodec.Encode("fixture", lamps);
        var decoded = (NightlightTileContent)BakeProductCodec.Decode("fixture", key, bytes);
        if (decoded.LampIds.Single() != 321 || decoded.Glow.Buffer[12] != 321 || decoded.ReflectionWindowIndices.Count != 0)
            throw new InvalidOperationException("Lamp IDs were renumbered by the tile codec.");
        var windows = new NightlightTileContent(key with { Module = BakeModuleIds.NightlightWindows }, default, [],
            lamps.Glow with { Kind = GlowKind.Window }, [0]);
        bytes = BakeProductCodec.Encode("fixture", windows);
        var restored = (NightlightTileContent)BakeProductCodec.Decode("fixture", windows.Key, bytes);
        if (restored.ReflectionWindowIndices.Single() != 0 || restored.LampIds.Count != 0)
            throw new InvalidOperationException("Window reflections lost their local buffer index.");
        Reject(() => BakeProductCodec.Encode("fixture", windows with { ReflectionWindowIndices = [1] }));
        Reject(() => BakeProductCodec.Encode("fixture", lamps with { LampIds = [320] }));
        Reject(() => BakeProductCodec.Decode("fixture", windows.Key, bytes[..^1]));
        Reject(() => BakeProductCodec.Decode("fixture", windows.Key, bytes.Concat(new byte[] { 0 }).ToArray()));
        foreach (var module in new[] { BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass, BakeModuleIds.FaunaRuntime,
            BakeModuleIds.NightlightLamps, BakeModuleIds.NightlightWindows })
            if (BakeCacheStore.MaximumPayload(key with { Module = module }) != BakeCacheStore.MaximumProductBytes)
                throw new InvalidOperationException("An auxiliary CPU product inherited the render container limit.");
        var count = BakeCacheStore.MaximumPayloadBytes / (GlowSet.Stride * sizeof(float)) + 1;
        var large = windows with { Glow = windows.Glow with { Count = count, Buffer = new float[count * GlowSet.Stride] }, ReflectionWindowIndices = [] };
        var largeBytes = BakeProductCodec.Encode("fixture/large", large);
        if (largeBytes.Length <= BakeCacheStore.MaximumPayloadBytes ||
            ((NightlightTileContent)BakeProductCodec.Decode("fixture/large", windows.Key, largeBytes)).Glow.Count != count)
            throw new InvalidOperationException("CPU channels or containers still use the single-render-upload boundary.");
        Reject(() => BakeProductCodec.Decode("fixture/large", key with { Module = "fixture.render" }, largeBytes));
        Reject(() => BakeProductCodec.Decode("fixture/large", key with { Module = "collision.world", Detail = "C/0/0" }, largeBytes));
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
