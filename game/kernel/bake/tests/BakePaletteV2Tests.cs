using Campus.Tests;
using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakePaletteV2Tests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OrdinaryBuildOutputUsesItsCapturedCatalogAndRejectsAReplacedSnapshot()
    {
        var catalog = Catalog();
        var source = Geometry() with { Material = new(Spec(510)), ColorIndices = [255, 256, 511] };
        var descriptor = new Campus.Contracts.Maps.MapDescriptor(
            Id: new("fixture"), Name: default!, Kind: Campus.Contracts.Maps.MapKind.Outdoor,
            Version: 1, Seed: 1, Geo: default!, Center: default, Bounds: Bounds(), DataRect: default!,
            Roam: default!, TileMin: default, TileMax: default, DefaultSpawn: "fixture", Spawns: [],
            World: default!, Env: default!, Portals: []);
        var context = new BuildContext { World = null!, Map = descriptor, WorldSeed = 1, Catalog = catalog };
        var key = BakeRequestKeys.Tile(descriptor.Id, "fixture.palette", new(0, 0), LodLevel.L0);
        var builder = new BakeGeometryBuilder(catalog, new ServiceRegistry());
        var diagnostics = new List<BuildDiagnostic>();
        var warning = new BuildDiagnostic(DiagnosticLevel.Warning, "fixture.warning", "fixture placement", new FeatureId("fixture.feature"));
        var result = builder.BuildOutput(key, context, new([source], []) { Diagnostics = [warning] }, new(0, 0), LodLevel.L0, diagnostics.AddRange);
        AssertThat(diagnostics.SequenceEqual([warning])).OverrideFailureMessage("BuildOutput dropped or changed producer diagnostics.").IsTrue();
        var quiet = builder.BuildOutput(key, context, new([source], []), new(0, 0), LodLevel.L0, diagnostics.AddRange);
        AssertThat(TileBundleCodec.Encode("fixture", result).SequenceEqual(TileBundleCodec.Encode("fixture", quiet))).OverrideFailureMessage("Diagnostics changed serialized geometry.").IsTrue();
        AssertThat(result.Surfaces.Single().AttributeData.Span[10] == 16).OverrideFailureMessage("The ordinary geometry path rejected or narrowed bank 1 before the packer.").IsTrue();
        var other = new BuildContext { World = null!, Map = descriptor, WorldSeed = 1, Catalog = Catalog() };
        AssertThrown(() => builder.BuildOutput(key, other, new([source], []), new(0, 0), LodLevel.L0, _ => { })).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BankZeroBytesRemainIdenticalThroughMutableAndFrozenGeometry()
    {
        var catalog = Catalog();
        foreach (var uv2 in new[] { false, true })
        {
            const int count = 256;
            var source = Geometry(count) with
            {
                ColorIndices = Enumerable.Range(0, count).Select(i => (ushort)i).ToArray(),
                Ao = Enumerable.Range(0, count).Select(i => i / 255f).ToArray(),
                Flags = Enumerable.Range(0, count).Select(i => (byte)(i & 15)).ToArray(),
                Reserved = Enumerable.Range(0, count).Select(i => (byte)(255 - i)).ToArray(),
                Uv2 = uv2 ? Enumerable.Range(0, count).SelectMany(_ => new[] { 16777216f, -1f }).ToArray() : null,
                Custom0 = Enumerable.Range(0, count * 4).Select(i => (float)i).ToArray(),
                Custom1 = Enumerable.Range(0, count * 4).Select(i => (float)-i).ToArray(),
                Custom2 = Enumerable.Range(0, count * 4).Select(i => i / 4f).ToArray()
            };
            var old = BakeSurfacePacker.Pack(source, Bounds(), catalog);
            var stride = old.AttributeData.Length / count;
            for (var i = 0; i < count; i++)
            {
                var rgba = old.AttributeData.Span.Slice(i * stride, 4);
                AssertThat(rgba[0] == i && rgba[1] == (byte)(source.Ao![i] * 255f) && rgba[2] == (i & 15) && rgba[3] == 255 - i).OverrideFailureMessage("The bank-zero surface no longer copies the frozen R/A/flags bytes or truncates AO as before.").IsTrue();
            }
            foreach (var next in new[]
            {
                BakeSurfacePacker.Pack(source, Bounds(), catalog),
                BakeSurfacePacker.Pack(GeometryBuffer.Freeze(source), Bounds(), catalog)
            })
            {
                AssertThat(old.Format == next.Format && old.VertexData.Span.SequenceEqual(next.VertexData.Span) &&
                    old.AttributeData.Span.SequenceEqual(next.AttributeData.Span) && old.IndexData.Span.SequenceEqual(next.IndexData.Span)).OverrideFailureMessage("Catalog support changed an existing vertex, attribute, index or format byte.").IsTrue();
                var key = BakeRequestKeys.Tile(new("fixture"), "fixture.palette", new(0, 0), LodLevel.L0);
                var before = new TileBundle(key, new(0, 0), LodLevel.L0, Bounds(), [old]);
                var after = new TileBundle(key, new(0, 0), LodLevel.L0, Bounds(), [next]);
                AssertThat(TileBundleCodec.Encode("same-fixture-identity", before).SequenceEqual(TileBundleCodec.Encode("same-fixture-identity", after))).OverrideFailureMessage("An equal surface no longer has equal TBIN payload bytes at the same identity.").IsTrue();
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BoundaryIndicesUseBankBitsAndPreserveNonDecalReservedAndCustomBits()
    {
        var source = Geometry() with
        {
            ColorIndices = [255, 256, 511],
            Ao = [0f, .5f, 1f],
            Flags = [1, 2, 8],
            Reserved = [1, 8, 15],
            Uv2 = [65535, 17, 65536, 33, 16777216, -1],
            Custom0 = [1, 2, 3, 4, -5, -6, -7, -8, 9, 10, 11, 12],
            Custom1 = [13, 14, 15, 16, 17, 18, 19, 20, -21, -22, -23, -24],
            Custom2 = [25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36]
        };
        var packed = BakeSurfacePacker.Pack(source, Bounds(), Catalog());
        byte[][] expected = [[255, 0, 1, 1], [0, 127, 18, 8], [255, 255, 24, 15]];
        var stride = packed.AttributeData.Length / 3;
        for (var i = 0; i < 3; i++)
        {
            var data = packed.AttributeData.Span.Slice(i * stride, stride);
            AssertThat(data[..4].SequenceEqual(expected[i])).OverrideFailureMessage("255, 256 or 511 aliased, or AO/flags/reserved changed.").IsTrue();
            AssertThat(PaletteEncoding.DecodeColor(data[0], data[2]).Value == source.ColorIndices![i]).OverrideFailureMessage("Logical color failed a bank round trip.").IsTrue();
            for (var component = 0; component < 2; component++)
                AssertThat(BinaryPrimitives.ReadInt32LittleEndian(data[(12 + component * 4)..]) == BitConverter.SingleToInt32Bits(source.Uv2![i * 2 + component])).OverrideFailureMessage("Encoding 2 changed fp32 UV2 bits.").IsTrue();
            var offset = 20;
            foreach (var custom in new[] { source.Custom0, source.Custom1, source.Custom2 })
            {
                for (var component = 0; component < 4; component++)
                    AssertThat(BinaryPrimitives.ReadInt32LittleEndian(data[(offset + component * 4)..]) == BitConverter.SingleToInt32Bits(custom![i * 4 + component])).OverrideFailureMessage("Encoding 2 repurposed or changed a custom-channel bit.").IsTrue();
                offset += 16;
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AllFourDecalBankCombinationsKeepIndependentForegroundAndBaseIndices()
    {
        foreach (var foreground in new ushort[] { 255, 511 })
            foreach (var background in new ushort[] { 255, 511 })
            {
                var source = Geometry() with
                {
                    ColorIndices = [foreground, foreground, foreground],
                    DecalBaseColorIndices = [background, background, background],
                    Flags = [15, 15, 15],
                    Ao = [.5f, .5f, .5f]
                };
                var packed = BakeSurfacePacker.Pack(GeometryBuffer.Freeze(source), Bounds(), Catalog());
                var expectedFlags = 15 + (foreground > 255 ? 16 : 0) + (background > 255 ? 32 : 0);
                for (var i = 0; i < 3; i++)
                {
                    var data = packed.AttributeData.Span.Slice(i * 8, 4);
                    AssertThat(data[0] == 255 && data[1] == 127 && data[2] == expectedFlags && data[3] == 255).OverrideFailureMessage("Decal banks collided with semantic flags or AO.").IsTrue();
                    AssertThat(PaletteEncoding.DecodeColor(data[0], data[2]).Value == foreground &&
                        PaletteEncoding.DecodeDecalBase(data[3], data[2]).Value == background).OverrideFailureMessage("Decal foreground/base banks were conflated.").IsTrue();
                }
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void HighMaterialDefaultsAndPlacedTintUseTheCompleteCatalog()
    {
        var catalog = Catalog();
        var high = Geometry() with { Material = new(Spec(510)) };
        var packed = BakeSurfacePacker.Pack(high, Bounds(), catalog);
        AssertThat(packed.AttributeData.Span[0] == 254 && packed.AttributeData.Span[2] == 16).OverrideFailureMessage("A material-only extension token was narrowed to a byte.").IsTrue();
        AssertThat(BakeSurfacePacker.Pack(GeometryBuffer.Freeze(high), Bounds(), catalog).AttributeData.Span.SequenceEqual(packed.AttributeData.Span)).OverrideFailureMessage("Freezing changed the high material fallback.").IsTrue();
        var source = Geometry() with { ColorIndices = [64, 66, 68], Flags = [4, 4, 4], DecalBaseColorIndices = [255, 256, 511] };
        var part = new PlacedPart(new("fixture.part"), new object(), Matrix4x4.Identity, Token(510));
        var tinted = BakeSurfacePacker.ApplyTint(source, part, catalog);
        AssertThat(tinted.ColorIndices!.All(i => i == 510) && tinted.Material.Spec == Spec(510)).OverrideFailureMessage("Tint dropped its high bit or retained conflicting color streams.").IsTrue();
        AssertThat(ReferenceEquals(tinted.DecalBaseColorIndices, source.DecalBaseColorIndices) && ReferenceEquals(tinted.Flags, source.Flags)).OverrideFailureMessage("Tint changed decal bases or semantic flags.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InvalidOrCompetingStreamsAreRejectedBeforePacking()
    {
        var catalog = Catalog();
        foreach (var invalid in new[]
        {
            Geometry() with { ColorIndices = [512, 256, 256] }, Geometry() with { ColorIndices = [] },
            Geometry() with { ColorIndices = [256, 256, 256], Flags = [16, 0, 0] },
            Geometry() with { DecalBaseColorIndices = [256, 256, 256], Flags = [4, 0, 4] },
            Geometry() with { DecalBaseColorIndices = [256, 256, 256], Flags = [4, 4, 4], Reserved = [0, 0, 0] }
        }) AssertThrown(() => BakeSurfacePacker.Pack(invalid, Bounds(), catalog)).IsInstanceOf<ArgumentException>();
        GeometryBuffer.Freeze(Geometry() with { ColorIndices = [255, 255, 255] });
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RepeatedPrototypeStreamsRetainOrderAndCachePerReferenceWithoutDiscardingInstances()
    {
        var catalog = Catalog();
        var frozen = GeometryBuffer.Freeze(Geometry() with { ColorIndices = [255, 256, 511] });
        var prototype = new GeometryPrototype("fixture.shared", [frozen], Bounds());
        var record = new float[16];
        InstanceStreamLayout.Write(record, Matrix4x4.Identity, new(1, 2, 3, 4));
        var stream = new GeometryInstanceStream("fixture.same-id", prototype, record, Bounds());
        var result = new BakeInstanceAdapter(catalog).Pack([stream, stream]);
        AssertThat(result.Count == 2 && ReferenceEquals(result[0].Surface, result[1].Surface) &&
            result[0].InstanceData.Span.SequenceEqual(result[1].InstanceData.Span)).OverrideFailureMessage("Repeated streams were deduplicated or prototype bytes were repacked.").IsTrue();
        AssertThat(result[0].Surface.AttributeData.Span[10] == 16).OverrideFailureMessage("Prototype packing reinterpreted its logical colour or lost bank 1.").IsTrue();
        AssertThrown(() => new BakeInstanceAdapter(catalog).Pack([null!])).IsInstanceOf<ArgumentNullException>();
    }

    private static PaletteToken Token(int index) => new("fixture.p" + index);
    private static ToonSpec Spec(int index) => new(Token(index), Token(index + 1));
    private static PaletteCatalog Catalog()
    {
        var legacy = new PaletteRegistry(Enumerable.Range(0, 256).Select(i => new PaletteEntry(Token(i), new(i),
            new Vector3(.5f), "fixture", false, i < 32 ? PaletteRole.Frame : i < 40 ? PaletteRole.Retired :
                i < 64 ? PaletteRole.Weather : (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade)));
        return new(legacy, Enumerable.Range(256, 256).Select(i => new PaletteEntry(Token(i), new(i), new(.5f),
            "fixture", false, (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade)));
    }
    private static Box3 Bounds() => new(Vector3.Zero, Vector3.One);
    private static GeometryBundle Geometry(int count = 3) => new(new(Spec(64)),
        Enumerable.Range(0, count).SelectMany(i => new[] { i % 3 == 1 ? 1f : 0f, 0f, i % 3 == 2 ? 1f : 0f }).ToArray(),
        Enumerable.Range(0, count).SelectMany(_ => new[] { 0f, 1f, 0f }).ToArray(), [0, 1, 2], Uv: new float[count * 2]);
}
