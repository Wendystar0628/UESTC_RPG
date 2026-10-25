using Campus.Tests;
using static Campus.Kernel.Bake.Tests.BakeInputFixture;
using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using GdUnit4;
using Format = Godot.RenderingServer.ArrayFormat;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class PackedSurfaceBatchTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void OnlyWholeToonSurfacesWithoutShadowContributionDisableNativeCasting()
    {
        var cases = new (byte[]? Flags, bool NoShadow)[]
        {
            (null, false),
            ([0, 0, 0], false),
            ([VertexFlag.NoShadow, VertexFlag.NoShadow, 0], false),
            ([VertexFlag.Decal, 0, VertexFlag.NoShadow], false),
            ([VertexFlag.NoShadow, VertexFlag.NoShadow, VertexFlag.NoShadow], true),
            ([VertexFlag.Decal, VertexFlag.Decal, VertexFlag.Decal], true),
            ([VertexFlag.NoShadow, VertexFlag.Decal, VertexFlag.NoShadow | VertexFlag.Outskirts], true)
        };
        foreach (var kind in new[] { ToonKind.Static, ToonKind.VegTrunk, ToonKind.VegCard, ToonKind.VegTwigMass })
            foreach (var (flags, noShadow) in cases)
                foreach (var explicitShader in new[] { false, true })
                {
                    var source = ShadowGeometry() with
                    {
                        Material = new(ShadowGeometry().Material.Spec with { Kind = kind }),
                        Shader = explicitShader ? MaterialShaders.ShaderFor(kind) : null,
                        Flags = flags
                    };
                    var packed = BakeSurfacePacker.Pack(source, Bounds(), Palette());
                    var frozen = BakeSurfacePacker.Pack(GeometryBuffer.Freeze(source), Bounds(), Palette());
                    if (MaterialShaders.HasNoShadow(kind, source.Shader, flags) != noShadow ||
                        packed.NoShadow != noShadow || frozen.NoShadow != noShadow)
                        throw new InvalidOperationException("Whole-surface shadow semantics changed for flags, frozen input or an explicit toon shader.");
                    var key = BakeRequestKeys.Tile(new("fixture"), "fixture.shadow", default, LodLevel.L2);
                    var tile = new TileBundle(key, default, LodLevel.L2, Bounds(), [packed]);
                    var restored = (TileBundle)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/shadow", key,
                        TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/shadow", tile));
                    if (restored.Surfaces.Single().NoShadow != noShadow || BakePipelineCatalog.Combinations(restored)
                        .Any(pipeline => pipeline.Shadows != (noShadow ? PipelineShadowMode.Off : PipelineShadowMode.On)))
                        throw new InvalidOperationException("Cache or pipeline preparation lost the baked native shadow mode.");
                }
        var overrideSource = ShadowGeometry() with
        {
            Shader = ShaderId.DioramaFar,
            Flags = [VertexFlag.NoShadow, VertexFlag.NoShadow, VertexFlag.NoShadow]
        };
        if (BakeSurfacePacker.Pack(overrideSource, Bounds(), Palette()).NoShadow)
            throw new InvalidOperationException("A non-toon override inherited toon vertex shadow semantics.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void KnownLayoutsRetainEverySourceVertexAttributeAndTriangleByte()
    {
        foreach (var uv2 in new[] { false, true })
            for (var customMask = 0; customMask < 8; customMask++)
            {
                var sources = Enumerable.Range(0, 3).Select(index => Surface(index, uv2, customMask)).ToArray();
                var merged = PackedSurfaceBatcher.Merge(sources).Single();
                VerifySources(sources, merged, uv2 ? 12 : 8, uv2 ? 8 : 4);
                var key = BakeRequestKeys.Tile(new("fixture"), "batch", new(-1, 2), LodLevel.L0);
                var before = new TileBundle(key, new(-1, 2), LodLevel.L0, Bounds(), sources);
                var after = new TileBundle(key, new(-1, 2), LodLevel.L0, Bounds(), [merged]);
                if (after.Stats.Surfaces != 1 || before.Stats.Surfaces != 3 || before.Stats.Vertices != after.Stats.Vertices ||
                    before.Stats.Triangles != after.Stats.Triangles || before.Stats.Bytes != after.Stats.Bytes)
                    throw new InvalidOperationException("Batching changed vertex/triangle/raw-byte statistics or kept the old surface count.");
                var identity = BakeOptions.Abi + "/fixture/batch";
                var encoded = TileBundleCodec.Encode(BakePayloadClass.Tile, identity, after);
                var restored = (TileBundle)TileBundleCodec.Decode(BakePayloadClass.Tile, identity, key, encoded);
                VerifySources(sources, restored.Surfaces.Single(), uv2 ? 12 : 8, uv2 ? 8 : 4);
                if (!encoded.SequenceEqual(TileBundleCodec.Encode(BakePayloadClass.Tile, identity, restored)))
                    throw new InvalidOperationException("Batched cache round-trip bytes changed.");
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryMetadataDifferenceFencesBatchesWithoutReorderingOtherSurfaces()
    {
        var original = Surface(0, true, 0);
        var differences = new[]
        {
            Copy(original, material: new(original.Material.Spec with { Wet = .3f })),
            Copy(original, variant: new VariantTag()), Copy(original, reflect: true), Copy(original, ignore: true),
            Copy(original, uvScale: new(2, 1, 0, 0)), Copy(original, at: Matrix4x4.CreateTranslation(-4, 0, 2)),
            Copy(original, bounds: new(new(-128, -16, -128), new(128, 128, 128))), Surface(0, true, 1),
            Copy(original, uvScale: new(BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)), 0, 0, 0))
        };
        foreach (var different in differences)
        {
            var merged = PackedSurfaceBatcher.Merge([original, different, original]);
            if (merged.Count != 3 || !ReferenceEquals(merged[0], original) || !ReferenceEquals(merged[1], different) || !ReferenceEquals(merged[2], original))
                throw new InvalidOperationException("A metadata difference was discarded or nonadjacent surfaces were reordered/merged.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnsupportedShadersTopologySkinLodsAndLayoutsRemainOriginalReferences()
    {
        var original = Surface(0, true, 0);
        var unsupported = new[]
        {
            Copy(original, shader: ShaderId.Water), Copy(original, material: new(original.Material.Spec with { Kind = ToonKind.GlassNear })),
            Copy(original, primitive: BakedPrimitive.TriangleStrip), Copy(original, skin: new byte[] { 1 }),
            Copy(original, lods: [new BakedMeshLod(1f, original.IndexCount, original.IndexData)]),
            Copy(original, format: original.Format | (1UL << 60)),
            Copy(original, format: original.Format | (ulong)Format.FormatCustom3),
            Copy(original, format: original.Format | (ulong)Format.FlagCompressAttributes),
            Copy(original, attributes: original.AttributeData[..^1])
        };
        foreach (var surface in unsupported)
        {
            var retained = PackedSurfaceBatcher.Merge([surface, surface]);
            if (retained.Count != 2 || !ReferenceEquals(retained[0], surface) || !ReferenceEquals(retained[1], surface))
                throw new InvalidOperationException("An unsupported surface was reinterpreted, merged or replaced.");
        }
        var custom = Surface(0, true, 1);
        var differentCustomEncoding = Copy(custom, format: custom.Format ^ (1UL << (int)Format.FormatCustom0Shift));
        if (PackedSurfaceBatcher.Merge([differentCustomEncoding, differentCustomEncoding]).Count != 2)
            throw new InvalidOperationException("A non-RGBAFloat custom layout was accepted with a matching layout.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void BatchingKeepsWholeSurfaceShadowExclusionThroughCacheDecode()
    {
        var source = BakeSurfacePacker.Pack(ShadowGeometry() with { Flags = [VertexFlag.NoShadow, VertexFlag.NoShadow, VertexFlag.NoShadow] }, Bounds(), Palette());
        var merged = PackedSurfaceBatcher.Merge([source, source]).Single();
        if (!source.NoShadow || !merged.NoShadow)
            throw new InvalidOperationException("Batching lost an all-surface shadow exclusion.");
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.shadow.batch", default, LodLevel.L1);
        var tile = new TileBundle(key, default, LodLevel.L1, Bounds(), [merged]);
        var restored = (TileBundle)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/shadow/batch", key,
            TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/shadow/batch", tile));
        if (!restored.Surfaces.Single().NoShadow)
            throw new InvalidOperationException("The batched cache round trip lost native shadow exclusion.");
        var mixed = PackedSurfaceBatcher.Merge([source, BakeSurfacePacker.Pack(ShadowGeometry(), Bounds(), Palette())]);
        if (mixed.Count != 1 || mixed.Single().NoShadow)
            throw new InvalidOperationException("Shadow exclusion split a visible batch or disabled casting for its mixed source surfaces.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void The65536VertexBoundarySplitsStablyWithoutDroppingOrWeldingVertices()
    {
        var a = RawSurface(32768, 11); var b = RawSurface(32768, 29);
        var c = RawSurface(4, 47); var d = RawSurface(65536, 61); var e = RawSurface(3, 83);
        var merged = PackedSurfaceBatcher.Merge([a, b, c, d, e]);
        if (!merged.Select(surface => surface.VertexCount).SequenceEqual(new[] { 65536, 4, 65536, 3 }) ||
            !ReferenceEquals(merged[1], c) || !ReferenceEquals(merged[2], d) || !ReferenceEquals(merged[3], e))
            throw new InvalidOperationException("The 65536-vertex limit did not form stable source-order batches.");
        VerifySources([a, b], merged[0], 8, 4);
        if (BinaryPrimitives.ReadUInt16LittleEndian(merged[0].IndexData.Span[8..]) != ushort.MaxValue)
            throw new InvalidOperationException("The highest valid rebased u16 index was changed.");
    }



    private static GeometryBundle ShadowGeometry() => new(new(new(new("fixture.lit"), new("fixture.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 2, 1]);
    private static BakedSurface Surface(int id, bool uv2, int customMask)
    {
        float[] Custom(int channel) => Enumerable.Range(0, 12).Select(index => index == 0 ?
            BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)) : id * 100 + channel * 20 + index + .125f).ToArray();
        var geometry = new GeometryBundle(new(new(new("fixture.lit"), new("fixture.shade")) { Kind = uv2 ? ToonKind.Glass : ToonKind.Static }),
            [-20 + id, 2, 11, -17 + id, 3, 11, -18 + id, 6, 11], [0, 0, 1, 0, 0, 1, 0, 0, 1], [2, 0, 1],
            Ao: [.1f, .5f, .9f], Uv: [0, 0, 1, 0, 0, 1], Uv2: uv2 ? [65535, 961, 65536, 1025, 16777216, -1] : null)
        {
            ColorIndices = [64, 66, 64],
            Flags = [1, 2, 8],
            Reserved = [7, (byte)(7 | WindowCardEncoding.PhysicalCurtainFlag), 9],
            Custom0 = (customMask & 1) != 0 ? Custom(0) : null,
            Custom1 = (customMask & 2) != 0 ? Custom(1) : null,
            Custom2 = (customMask & 4) != 0 ? Custom(2) : null
        };
        return BakeSurfacePacker.Pack(geometry, Bounds(), Palette());
    }

    private static BakedSurface RawSurface(int count, byte marker)
    {
        var basis = Surface(0, false, 0);
        var vertices = Enumerable.Range(0, count * 12).Select(index => (byte)((index + marker) % 251)).ToArray();
        var attributes = Enumerable.Range(0, count * 8).Select(index => (byte)((index + marker * 2) % 251)).ToArray();
        var indices = new byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(indices.AsSpan(2), checked((ushort)(count - 1)));
        BinaryPrimitives.WriteUInt16LittleEndian(indices.AsSpan(4), 1);
        return new(basis.Material, basis.Format, BakedPrimitive.Triangles, count, 3, vertices, attributes, indices, Bounds(), uvScale: basis.UvScale);
    }

    private static BakedSurface Copy(BakedSurface source, ToonRef? material = null, ulong? format = null,
        BakedPrimitive? primitive = null, Matrix4x4? at = null, Box3? bounds = null, Vector4? uvScale = null,
        VariantTag? variant = null, bool reflect = false, bool ignore = false, ShaderId? shader = null,
        IReadOnlyList<BakedMeshLod>? lods = null, ReadOnlyMemory<byte> skin = default, ReadOnlyMemory<byte>? attributes = null) =>
        new(material ?? source.Material, format ?? source.Format, primitive ?? source.Primitive, source.VertexCount, source.IndexCount,
            source.VertexData, attributes ?? source.AttributeData, source.IndexData, bounds ?? source.Bounds,
            at: at ?? source.At, lods: lods, skinData: skin, uvScale: uvScale ?? source.UvScale, variant: variant, ignoreOcclusionCulling: ignore)
        { Shader = shader, Reflect = reflect, NoShadow = source.NoShadow };
    private static Box3 Bounds() => new(new(-64, -16, -64), new(64, 64, 64));
    private static PaletteCatalog Palette() => new(new PaletteRegistry([
        new(new("fixture.lit"), new(64), Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.shade"), new(65), new Vector3(.5f), "fixture", false, PaletteRole.Shade),
        new(new("fixture.accent"), new(66), Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.accent.shade"), new(67), new Vector3(.5f), "fixture", false, PaletteRole.Shade)]), []);
}
