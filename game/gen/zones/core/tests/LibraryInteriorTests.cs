using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Zones.Core.LibraryInterior;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Zones.Core.Tests;

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
    public void EveryOpenStoreyBuildsAProductAndFootedCollision()
    {
        var world = RealMapFixture.Load().World;
        using var catalog = JsonDocument.Parse(RealMapFixture.ReadFiles()["interiors.json"]);
        var id = typeof(LibraryInteriorProvider).GetCustomAttribute<InteriorProviderAttribute>()!.Id;
        var entry = catalog.RootElement.GetProperty("interiors").EnumerateArray()
            .Single(item => item.GetProperty("providerId").GetString() == id);
        var features = entry.GetProperty("featureIds").EnumerateArray().Select(e => new FeatureId(e.GetString()!)).ToArray();
        var floors = entry.GetProperty("openFloors").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        var provider = new LibraryInteriorProvider();
        provider.Configure(features, floors, new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>());
        var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
        foreach (var feature in features)
        {
            var layout = provider.Layout(ctx, feature);
            AssertThat(layout.OpenStoreys.Order().SequenceEqual(floors.Order()) && floors.All(f => layout.Storeys.Any(s => s.Index == f)))
                .OverrideFailureMessage($"{feature.Value} layout covers its open storeys").IsTrue();
            foreach (var storey in floors)
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
        if (floors.Contains(1) && floors.Contains(2))
            AssertThat(provider.CollisionStorey(ctx, CoreIds.Library, 1).Any(c => c is RampCollider { Tag: SurfaceTag.Stair }))
                .OverrideFailureMessage("1F reaches 2F by stair").IsTrue();
    }
}
