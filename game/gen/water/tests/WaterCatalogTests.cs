using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Water.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class WaterCatalogTests
{
    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static MapDescriptor Map(params FeatureId[] lakes) => new(
        new MapId("test"), new LocalizedName("", ""), MapKind.Outdoor, 1, 1,
        new GeoFrame(0, 0, 0, Vector2.Zero, Vector2.Zero), Vector2.Zero, new Box3(),
        new MapRect(new(-1280, -1280), new(1280, 1280)), new RoamLimits(0.9f, 30, 3000, -28, 2000, 30, 40, 150),
        new TileKey(-20, -20), new TileKey(20, 20), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("", ""), false, "test"),
        new MapEnvironment("", "", lakes), new Dictionary<string, string>(), []);

    [TestCase, TestCategory(TestReason.Contract)]
    public void EveryBodyHasItsOwnLevelAndPartsAreBuiltSeparately()
    {
        var lake = new FeatureId("lake");
        var river = new FeatureId("river");
        var features = new Dictionary<FeatureId, WorldFeature>
        {
            [lake] = WorldFeatureFixture.Create(lake.Value, "water", new PolygonGeometry(Rect(0, 0, 80, 60)), source: FeatureSource.Osm),
            // Two parts: the aggregate geometry must not be built in addition.
            [river] = WorldFeatureFixture.Create(river.Value, "water", new PolygonGeometry(Rect(-500, -500, 500, 500)), source: FeatureSource.Osm) with
            {
                Parts =
                [
                    new("a", new PolygonGeometry(Rect(200, 0, 208, 300)), null, null),
                    new("b", new MultiPolygonGeometry([Rect(220, 0, 228, 300), Rect(240, 0, 248, 300)]), null, null),
                ],
            },
        };
        var bodies = WaterCatalog.Build(new FakeWorld { Features = features, Spatial = new FlatSpatial((_, _) => 2f) }, Map(lake));
        var p = WaterParams.Default;
        AssertThat(bodies.Count != 4).OverrideFailureMessage($"expected 4 bodies, got {bodies.Count}").IsFalse();
        var l = bodies.Single(b => b.Id == lake);
        AssertThat(l.Kind != WaterKind.Lake || l.ReflectSlot != 0 || MathF.Abs(l.SurfaceY - (2f - p.LakeDrop + p.LakeLift)) > TestTolerance.Position).OverrideFailureMessage("lake level or slot wrong").IsFalse();
        foreach (var r in bodies.Where(b => b.Id == river))
            AssertThat(r.Kind != WaterKind.Channel || r.ReflectSlot != -1 || MathF.Abs(r.SurfaceY - (2f - p.ChannelDrop)) > TestTolerance.Position).OverrideFailureMessage("channel level, kind or slot wrong").IsFalse();
    }
}
