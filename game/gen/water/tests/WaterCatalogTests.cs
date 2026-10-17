using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Gen.Water.Tests;

[TestSuite]
public sealed class WaterCatalogTests
{
    private sealed class World(IReadOnlyDictionary<FeatureId, WorldFeature> features, float bank) : IWorldModel, ISpatialIndex
    {
        public PolygonRings Boundary { get; } = Rect(-1000, -1000, 1000, 1000);
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = features;
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial => this;
        public IInteriorIndex Interiors => null!;
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity => null;
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => bank;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    }

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static MapDescriptor Map(params FeatureId[] lakes) => new(
        new MapId("test"), new LocalizedName("", ""), MapKind.Outdoor, 1, 1,
        new GeoFrame(0, 0, 0, Vector2.Zero, Vector2.Zero), Vector2.Zero, new Box3(),
        new MapRect(new(-1280, -1280), new(1280, 1280)), new RoamLimits(0.9f, 30, 3000, -28, 2000, 30, 40, 150),
        new TileKey(-20, -20), new TileKey(20, 20), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("", ""), false, "test"),
        new MapEnvironment("", "", lakes), new Dictionary<string, string>(), []);

    [TestCase]
    public void EveryBodyHasItsOwnLevelAndPartsAreBuiltSeparately()
    {
        var lake = new FeatureId("lake");
        var river = new FeatureId("river");
        var water = new DesignKind("water");
        var features = new Dictionary<FeatureId, WorldFeature>
        {
            [lake] = new(lake, FeatureSource.Osm, water, new PolygonGeometry(Rect(0, 0, 80, 60))),
            // Two parts: the aggregate geometry must not be built in addition.
            [river] = new(river, FeatureSource.Osm, water, new PolygonGeometry(Rect(-500, -500, 500, 500)))
            {
                Parts =
                [
                    new("a", new PolygonGeometry(Rect(200, 0, 208, 300)), null, null),
                    new("b", new MultiPolygonGeometry([Rect(220, 0, 228, 300), Rect(240, 0, 248, 300)]), null, null),
                ],
            },
        };
        var bodies = WaterCatalog.Build(new World(features, 2f), Map(lake));
        var p = WaterParams.Default;
        if (bodies.Count != 4) throw new InvalidOperationException($"expected 4 bodies, got {bodies.Count}");
        var l = bodies.Single(b => b.Id == lake);
        if (l.Kind != WaterKind.Lake || l.ReflectSlot != 0 || MathF.Abs(l.SurfaceY - (2f - p.LakeDrop + p.LakeLift)) > 1e-5f)
            throw new InvalidOperationException("lake level or slot wrong");
        foreach (var r in bodies.Where(b => b.Id == river))
            if (r.Kind != WaterKind.Channel || r.ReflectSlot != -1 || MathF.Abs(r.SurfaceY - (2f - p.ChannelDrop)) > 1e-5f)
                throw new InvalidOperationException("channel level, kind or slot wrong");
    }
}
