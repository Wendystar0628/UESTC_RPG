using Campus.Gen.Tests.Support;
using Campus.Tests;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;

namespace Campus.Kernel.Bake.Tests;

internal static class BakeInputFixture
{
    internal static TerrainCacheService CreateTerrainCache(BakeOptions options) => new(options,
        new BakeCodeFingerprints(new(BakeCodeManifest.CurrentSchema,
            new[] { BakeCodeChains.Geometry, BakeCodeChains.Collision, BakeCodeChains.Map, BakeCodeChains.Vegetation,
                BakeCodeChains.Fauna, BakeCodeChains.Nightlight, BakeCodeChains.Terrain, BakeCodeChains.InputIndex, BakeCodeChains.Resident }
                .ToDictionary(BakeCodeManifest.Chain, chain => "fixture-code/" + chain, StringComparer.Ordinal))));

    internal static WorldFeature Feature(string id, float x, string name, string note, string text) =>
        WorldFeatureFixture.Create(id, "prop", new PointGeometry(new(x, 0, 10))) with
        {
            Bounds = new(new(x, 0, 10), new(x + 1, 2, 11)), Name = name,
            Props = JsonSerializer.SerializeToElement(new { note, text }, ContractJson.Options)
        };
    internal static BuildContext Context(IWorldModel world, string species = "one", WindowNumberingSnapshot? numbering = null, string? planting = null,
        BuildInputSnapshot? global = null, string? interiors = null) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, WorldSeed = 1, CodeHash = "module-code", WindowNumbering = numbering,
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

    internal sealed class CountingFingerprintField(float height) : ITerrain, IFidelityModel
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
        public IReadOnlyList<FacadeSegment> Facades(FeatureId building) => [];
        public RoofZone Roof(FeatureId building) => new(building, FidelityZone.K3);
        public FidelityZone GroundAt(float x, float z) { Read("ground", x, z); return FidelityZone.K3; }
        public float WalkDistanceAt(float x, float z) { Read("walk", x, z); return 0; }
        public string InputHash => "fixture";
    }
}
internal sealed class BakeFixturePart : IPartDef
{
    public IReadOnlyList<LodLevel> SupportedLods => [LodLevel.L0, LodLevel.L2];
}
