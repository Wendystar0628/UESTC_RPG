using Campus.Gen.Tests.Support;
using Campus.Tests;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Kernel.Registry;
using Campus.Gen.Interior.Shell.Box;

namespace Campus.Kernel.Bake.Tests;

internal static class BakeInputFixture
{
    internal static TerrainCacheService CreateTerrainCache(BakeOptions options) => new(options,
        new BuildCodeIndex(new BakeCodeManifest(BakeCodeManifest.CurrentSchema,
            new[] { BakeCodeChains.Shared, BakeCodeChains.Collision, BakeCodeChains.Nightlight }
                .ToDictionary(BakeCodeManifest.Chain, chain => "fixture-code/" + chain, StringComparer.Ordinal), new Dictionary<string, string>())));

    internal static WorldFeature Feature(string id, float x, string name, string note, string text) =>
        WorldFeatureFixture.Create(id, "prop", new PointGeometry(new(x, 0, 10))) with
        {
            Bounds = new(new(x, 0, 10), new(x + 1, 2, 11)), Name = name,
            Props = JsonSerializer.SerializeToElement(new { note, text }, ContractJson.Options)
        };
    internal static BuildContext Context(IWorldModel world, string species = "one", WindowNumberingSnapshot? numbering = null, string? planting = null,
        BuildInputSnapshot? global = null, string? interiors = null) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, WorldSeed = 1, CodeHash = "module-code", WindowNumbering = numbering ?? new(new MapId("fixture"), new string('0', 64), []),
        Map = new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
            new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), []),
        GlobalInputs = global ?? BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
            { ["vegetation/species.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { species })) }, "fixture"),
        SourceInputs = new[] { ("vegetation/planting.json", planting), ("interiors.json", interiors) }
            .Where(input => input.Item2 is not null).ToDictionary(input => input.Item1,
                input => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(input.Item2!), StringComparer.Ordinal),
        PartLods = new PartLodCatalog([new(new PartId("fixture.part"), new BakeFixturePart())])
    };

    internal static void VerifySources(IReadOnlyList<BakedSurface> sources, BakedSurface merged, int positionStride, int normalStride)
    {
        var vertexOffset = 0; var indexOffset = 0;
        foreach (var source in sources)
        {
            var attributeStride = source.AttributeData.Length / source.VertexCount;
            for (var vertex = 0; vertex < source.VertexCount; vertex++)
            {
                if (!source.VertexData.Span.Slice(vertex * positionStride, positionStride).SequenceEqual(
                        merged.VertexData.Span.Slice((vertexOffset + vertex) * positionStride, positionStride)) ||
                    !source.VertexData.Span.Slice(source.VertexCount * positionStride + vertex * normalStride, normalStride).SequenceEqual(
                        merged.VertexData.Span.Slice(merged.VertexCount * positionStride + (vertexOffset + vertex) * normalStride, normalStride)) ||
                    !source.AttributeData.Span.Slice(vertex * attributeStride, attributeStride).SequenceEqual(
                        merged.AttributeData.Span.Slice((vertexOffset + vertex) * attributeStride, attributeStride)))
                    throw new InvalidOperationException("A position, normal/tangent, UV/UV2/custom/color/flag byte changed during concatenation.");
            }
            for (var triangle = 0; triangle < source.IndexCount; triangle += 3)
                for (var corner = 0; corner < 3; corner++)
                {
                    var before = BinaryPrimitives.ReadUInt16LittleEndian(source.IndexData.Span[((triangle + corner) * 2)..]);
                    var after = BinaryPrimitives.ReadUInt16LittleEndian(merged.IndexData.Span[((indexOffset + triangle + corner) * 2)..]);
                    if (after - vertexOffset != before) throw new InvalidOperationException("A source triangle or its winding/order changed.");
                }
            vertexOffset += source.VertexCount; indexOffset += source.IndexCount;
        }
        if (vertexOffset != merged.VertexCount || indexOffset != merged.IndexCount)
            throw new InvalidOperationException("A source vertex or index was lost or duplicated.");
    }

    internal sealed class CountingFingerprintField(float height) : ITerrain
    {
        public TerrainInputSummary SourceInputs { get; } = new("fixture-height/" + height.ToString(System.Globalization.CultureInfo.InvariantCulture), []);
        public ConcurrentDictionary<(string Kind, int X, int Z), int> PointReads { get; } = new();
        public ConcurrentDictionary<(TileKey Tile, int Level), int> TileReads { get; } = new();
        private void Read(string kind, float x, float z) => PointReads.AddOrUpdate(
            (kind, BitConverter.SingleToInt32Bits(x), BitConverter.SingleToInt32Bits(z)), 1, (_, count) => count + 1);
        public bool IsFlat(TileKey tile) => height == 0;
        public float HeightAt(float x, float z) { Read("height", x, z); return height; }
        public (float Height, TerrainFacts Facts) SampleAt(float x, float z)
        {
            Read("sample", x, z);
            return (height, new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity));
        }
        public TerrainFacts FactsAt(float x, float z) { Read("facts", x, z); return new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity); }
        public void FillTile(TileKey tile, int level, Span<short> cm)
        {
            TileReads.AddOrUpdate((tile, level), 1, (_, count) => count + 1);
            cm.Fill(checked((short)(height / TerrainSampling.Centimetre)));
        }
        public float PadY(FeatureId feature) { Read("pad", 0, 0); throw new KeyNotFoundException(); }
        public bool TryGetPadY(FeatureId feature, out float y) { Read("pad", 0, 0); y = 0; return false; }
        public float? WaterY(FeatureId water) { Read("water", 0, 0); return null; }

    }
}
internal sealed class BakeFixturePart : IPartDef
{
    public IReadOnlyList<LodLevel> SupportedLods => [LodLevel.L0, LodLevel.L2];
}

[InteriorProvider("fixture.box")]
internal sealed class FixtureBoxInterior : BoxInteriorProvider
{
    public FixtureBoxInterior() { }
}
