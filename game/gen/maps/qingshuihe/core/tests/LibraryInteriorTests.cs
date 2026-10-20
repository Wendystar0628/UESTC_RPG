using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class LibraryInteriorTests
{
    /// <summary>
    /// The bake refuses an empty open-interior product and walking needs footed collision: every catalogued feature and open
    /// storey of the library entry builds non-empty IF and IS meshes, and the library's storeys carry footed colliders,
    /// with a stair from 1F to 2F.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryOpenStoreyBuildsAProductAndFootedCollision()
    {
        var world = RealMapFixture.Load().World;
        using var catalog = JsonDocument.Parse(RealMapFixture.ReadFiles()["interiors.json"]);
        var id = typeof(LibraryInteriorProvider).GetCustomAttribute<InteriorProviderAttribute>()!.Id;
        var entry = catalog.RootElement.GetProperty("interiors").EnumerateArray()
            .Single(item => item.GetProperty("providerId").GetString() == id);
        var features = entry.GetProperty("featureIds").EnumerateArray().Select(e => new FeatureId(e.GetString()!)).ToArray();
        var parts = InteriorEntranceJson.ParseOpenParts(entry);
        var floors = parts.SelectMany(part => part.Floors).Distinct().Order().ToArray();
        var provider = new LibraryInteriorProvider();
        provider.Configure(new(features, parts, new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>(), null, []));
        var ctx = new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
        foreach (var feature in features)
        {
            var layout = provider.Layout(ctx, feature);
            var featureFloors = parts.Where(part => part.FeatureId == feature).SelectMany(part => part.Floors).Distinct().Order().ToArray();
            AssertThat(layout.OpenStoreys.Order().SequenceEqual(featureFloors) && featureFloors.All(f => layout.Storeys.Any(s => s.Index == f)))
                .OverrideFailureMessage($"{feature.Value} layout covers its open storeys").IsTrue();
            foreach (var storey in featureFloors)
                foreach (var detail in new[] { InteriorDetail.Full, InteriorDetail.Shell })
                {
                    var meshes = provider.BuildStorey(ctx, feature, storey, detail).Meshes;
                    AssertThat(meshes.Count > 0 && meshes.All(m => m.Index.Length > 0 && m.IgnoreOcclusionCulling))
                        .OverrideFailureMessage($"{feature.Value}/{storey}/{detail} is a non-empty interior product").IsTrue();
                }
        }
        foreach (var storey in floors)
        {
            var colliders = provider.CollisionStorey(ctx, CoreIds.Library, storey);
            AssertThat(colliders.Count > 0 && colliders.All(c => c.Foot != FootMat.Auto))
                .OverrideFailureMessage($"library storey {storey} has footed collision").IsTrue();
        }
        foreach (var feature in features.Where(candidate => candidate != CoreIds.Library))
        {
            var actual = provider.Layout(ctx, feature);
            var source = world.Features[feature];
            var count = source.Storeys ?? source.Parts.Max(part => part.Storeys)!.Value;
            AssertThat(actual.Storeys.Select(level => level.Index).Distinct().Order().SequenceEqual(Enumerable.Range(1, count)))
                .OverrideFailureMessage("Neighbour metadata keeps real closed upper floors without inventing higher floors.").IsTrue();
            foreach (var level in actual.Storeys.Where(candidate => !actual.OpenStoreys.Contains(candidate.Index)))
                AssertThat(provider.BuildStorey(ctx, feature, level.Index, InteriorDetail.Full).Meshes.Count == 0)
                    .OverrideFailureMessage("Real upper-floor metadata does not open closed neighbour floors.").IsTrue();
        }
        if (floors.Contains(1) && floors.Contains(2))
            AssertThat(provider.CollisionStorey(ctx, CoreIds.Library, 1).Any(c => c is RampCollider { Tag: SurfaceTag.Stair }))
                .OverrideFailureMessage("1F reaches 2F by stair").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void NeighbourWithoutAuthoredHeightFailsForOpenAndClosedFloors()
    {
        var polygon = new PolygonGeometry(new PolygonRings([new(0, 0), new(8, 0), new(8, 8), new(0, 8)]));
        foreach (var open in new[] { false, true })
        foreach (float? height in new float?[] { null, 0f })
        {
            var feature = new WorldFeature(CoreIds.NorthEastConnector, new("building"), polygon) { Storeys = 2, Height = height };
            var world = new Campus.Gen.Tests.Support.FakeWorld
            {
                Features = new Dictionary<FeatureId, WorldFeature> { [feature.Id] = feature }
            };
            var scopes = new List<InteriorOpenPart> { new(CoreIds.Library, "", [1]) };
            if (open) scopes.Add(new(feature.Id, "", [1]));
            var provider = new LibraryInteriorProvider();
            provider.Configure(new([CoreIds.Library, feature.Id], scopes,
                new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>(), null, []));
            var context = new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []),
                Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
            AssertThrown(() => provider.Layout(context, feature.Id)).IsInstanceOf<InvalidDataException>();
        }
    }
}
