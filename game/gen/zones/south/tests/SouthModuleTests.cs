using Campus.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using Campus.Gen.Zones.Common.Tests;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Zones.South.Tests.SouthTestSupport;

[TestSuite]
public sealed class SouthModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    public void BuildingOneKeepsOnlyItsOneStoreySourcePartOnTheRealMap()
    {
        var one = new FeatureId(SouthIds.Building1.Entity);
        var rawFiles = RealMapFixture.ReadTextFiles().Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var raw = RealMapFixture.LoadPrepared(rawFiles, options: new WorldLoadOptions { ClaimedBy = new Dictionary<FeatureId, WpId>() }).World;
        AssertThat(raw.Features[one].Parts.Any(part => part.SourceId == SouthIds.Building1HiddenWing.Part)).OverrideFailureMessage("the raw map carries two source parts for building 1").IsTrue();
        // The map as the kernel loads it: every authored overlay.
        var world = RealMapFixture.Load().World;
        AssertThat(world.Features.ContainsKey(one)).OverrideFailureMessage("hidePart keeps the entity itself").IsTrue();
        var building = world.Features[one];
        AssertThat(building.Parts.Select(part => part.SourceId).SequenceEqual(raw.Features[one].Parts.Where(part => part.SourceId != SouthIds.Building1HiddenWing.Part).Select(part => part.SourceId))).OverrideFailureMessage("only the 1-storey source part is left").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ClaimsNameExistingFeatures()
    {
        var world = RealMapFixture.Load().World;
        var module = new SouthModule();
        AssertThat(module.Claims.Count == SouthSites.All.Sum(site => site.Claims.Count)).OverrideFailureMessage("gate, front steps, Xuanbang, Liren's connector, the gym, the natatorium, the sports platform and the stand are claimed").IsTrue();
        foreach (var claim in module.Claims.OfType<FeatureClaim>())
        {
            AssertThat(world.Features.ContainsKey(claim.Feature)).OverrideFailureMessage($"claimed feature {claim.Feature.Value} exists").IsTrue();
            AssertThat((claim.Aspects & Aspects.Massing) != 0).OverrideFailureMessage("claims cover massing so WP-14 skips the extrusion").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WholeSourcesReproduceProductionAndKeepClaimedPlans()
    {
        var ctx = Context();
        var module = new SouthModule();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            var traces = module.TraceSources(ctx, lod).ToArray();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage("source IDs are unique").IsTrue();
            foreach (var claim in module.Claims.OfType<FeatureClaim>())
                AssertThat(traces.Any(t => t.Feature == claim.Feature && t.Output.Meshes.Count > 0 && t.Colliders.Count > 0 && t.OccupiedContours.Count > 0)).OverrideFailureMessage($"{lod}: claimed source {claim.Feature} has actual geometry, collision and material contours").IsTrue();
            var cells = module.ItemsFor(ctx, lod).Select(i => TileGrid.At(i.Anchor, lod)).Distinct();
            var built = cells.Select(t => module.Build(ctx, t, lod)).ToArray();
            AssertThat(Triangles(traces.SelectMany(t => t.Output.Meshes)).SequenceEqual(Triangles(built.SelectMany(o => o.Meshes)))).OverrideFailureMessage($"{lod}: complete source geometry reproduces the tiled production geometry").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .SequenceEqual(built.SelectMany(o => o.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal))).OverrideFailureMessage($"{lod}: placed parts remain in their actual source").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.SelectMany(o => o.Instances))).OverrideFailureMessage($"{lod}: instance streams remain in their actual source").IsTrue();
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            string Dump(Collider c) => System.Text.Json.JsonSerializer.Serialize<Collider>(c, options);
            AssertThat(traces.SelectMany(t => t.Colliders).Select(Dump).Order(StringComparer.Ordinal)
                .SequenceEqual(module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Select(Dump).Order(StringComparer.Ordinal))).OverrideFailureMessage($"{lod}: complete source colliders reproduce production collision").IsTrue();
            AssertThat(traces.Count(t => t.SourceId.StartsWith("18a.main-building/", StringComparison.Ordinal)) == 3 &&
                traces.Where(t => t.Feature == new FeatureId(MainBuilding.MainBuildingSite.FrontSteps)).All(t => t.SourceId.EndsWith("/front", StringComparison.Ordinal))).OverrideFailureMessage("the roof gallery and rear porch are separate B2 sources").IsTrue();
            var gallery = traces.Single(t => t.SourceId.EndsWith("/roof-gallery", StringComparison.Ordinal));
            AssertThat(gallery.OccupiedContours.Single().Holes.Count == 1).OverrideFailureMessage("the gallery keeps its open centre").IsTrue();
            var stand = traces.Single(t => t.Feature == new FeatureId(Sports.GrandstandSite.Feature));
            var centre = Sports.GrandstandSite.PlanOf(ctx.World).Centre;
            AssertThat(stand.OccupiedContours.All(r => !PlanGeometry.InsideByCrossings(r.Outer, centre))).OverrideFailureMessage("stand segments leave the track and the C opening unfilled").IsTrue();
            AssertThat(traces.Single(t => t.Feature == new FeatureId(Sports.GrandstandSite.LowSeats)).SourceId.EndsWith("/east-seats", StringComparison.Ordinal)).OverrideFailureMessage("east seats use their own real feature").IsTrue();
            var connector = traces.Single(t => t.Feature == new FeatureId(Liren.LirenSite.Connector));
            var connectorPlan = Liren.LirenSite.Rings(ctx.World);
            AssertThat(connector.OccupiedContours.Count == Liren.LirenSite.Blocks(connectorPlan).Count() + Liren.LirenSite.Columns(connectorPlan).Count()).OverrideFailureMessage("connector contours contain only its generated blocks and columns").IsTrue();
        }

        static IEnumerable<string> Triangles(IEnumerable<GeometryBundle> meshes)
        {
            var result = new List<string>();
            foreach (var mesh in meshes)
                for (var i = 0; i < mesh.Index.Length; i += 3)
                {
                    string Vertex(int v) => string.Join(",", mesh.Position.Skip(v * 3).Take(3).Concat(mesh.Normal.Skip(v * 3).Take(3)).Select(BitConverter.SingleToInt32Bits));
                    result.Add($"{mesh.Material}/{Vertex(mesh.Index[i])}/{Vertex(mesh.Index[i + 1])}/{Vertex(mesh.Index[i + 2])}");
                }
            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new SouthModule();
        var tiles = module.Tiles(ctx).ToArray();
        AssertThat(tiles.Length > 0).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var cells = items.Select(i => (TileGrid.At(i.Anchor, lod).X, TileGrid.At(i.Anchor, lod).Z)).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("bundle vertex limit").IsTrue();
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            AssertThat(meshes > 0).OverrideFailureMessage($"{lod} emits meshes").IsTrue();
            AssertThat(items.Count(i => i.Part is not null) == parts).OverrideFailureMessage($"{lod}: every placed part is emitted exactly once").IsTrue();
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void BuildIsDeterministic()
    {
        var ctx = Context();
        var cells = ZoneTestCells.Heaviest(new SouthModule(SouthSites.All.ToArray()), ctx);
        var a = Bytes(ctx, cells);
        var b = Bytes(Context(), cells);
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("two worlds give the same bytes").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void WritableBuildOutputCannotModifySharedSiteGeometry()
    {
        var ctx = Context();
        var module = new SouthModule();
        var cached = module.ItemsFor(ctx, LodLevel.L0);
        var anchor = cached.First(i => i.Mesh is not null).Anchor;
        var tile = TileGrid.At(anchor);
        var output = module.Build(ctx, tile, LodLevel.L0);
        var positions = output.Meshes.Select(m => m.Position.ToArray()).ToArray();
        var indices = output.Meshes.Select(m => m.Index.ToArray()).ToArray();
        foreach (var mesh in output.Meshes)
        {
            Array.Fill(mesh.Position, 99f);
            Array.Fill(mesh.Index, 99);
        }
        var again = module.Build(ctx, tile, LodLevel.L0);
        AssertThat(ReferenceEquals(cached, module.ItemsFor(ctx, LodLevel.L0))).OverrideFailureMessage("the regression must exercise the same shared cache").IsTrue();
        AssertThat(again.Meshes.Count == positions.Length && again.Meshes.Select((m, i) => m.Position.SequenceEqual(positions[i]) && m.Index.SequenceEqual(indices[i])).All(same => same)).OverrideFailureMessage("a caller's writable output changed the next build from the cached site").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EntranceKitsAreLeftToTheStreetFurniture()
    {
        // WLD-INTERIORS: WP-13 places the mat, open sign and door lamp on every open entrance anchor; the zone builds the doorway only.
        string[] kit = [PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp];
        var placed = new SouthModule().ItemsFor(Context(), LodLevel.L0).Where(i => i.Part is not null).Select(i => i.Part!.Part.Value).ToArray();
        AssertThat(!placed.Any(kit.Contains)).OverrideFailureMessage($"the zone places no entrance kit ({string.Join(", ", placed.Where(kit.Contains))})").IsTrue();
        AssertThat(!new SouthModule().Claims.OfType<FeatureClaim>().Any(c => (c.Aspects & Aspects.Props) != 0)).OverrideFailureMessage("no Props claim keeps WP-13 off the zone's entrances").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SitesStayInsideTheSouthZone()
    {
        var ctx = Context();
        var module = new SouthModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            AssertThat(item.Anchor.X is > -1000 and < 1000 && item.Anchor.Y is > 90 and < 1000).OverrideFailureMessage($"item at {item.Anchor} is in the south zone").IsTrue();
    }

    private static byte[] Bytes(BuildContext ctx, IEnumerable<TileKey> cells)
    {
        var module = new SouthModule(SouthSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in cells)
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var v in mesh.Position) writer.Write(v);
                foreach (var v in mesh.Normal) writer.Write(v);
                foreach (var v in mesh.Index) writer.Write(v);
            }
            foreach (var part in output.Parts) writer.Write(part.Part.Value + part.At.ToString());
        }
        return stream.ToArray();
    }
}
