using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using Campus.Tests;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Pinxue;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Research;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;
using static GdUnit4.Assertions;

[TestSuite]
public sealed partial class CenterEastModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WholeSourcesReproduceProductionAndKeepClaimedPlans()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            var traces = module.TraceSources(ctx, lod).ToArray();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage("source IDs are unique").IsTrue();
            foreach (var claim in module.Claims.OfType<FeatureClaim>())
                AssertThat(traces.Any(t => t.Feature == claim.Feature && t.Output.Meshes.Count > 0 && t.Colliders.Count > 0 && t.OccupiedContours.Count > 0)).OverrideFailureMessage($"{lod}: claimed source {claim.Feature} carries production output, collision and solid contours").IsTrue();
            var cells = ZoneTestCells.Items(module, ctx, lod).Select(i => TileGrid.At(i.Anchor, lod)).Distinct();
            var built = cells.Select(t => module.Build(ctx, t, lod)).ToArray();
            AssertThat(Triangles(traces.SelectMany(t => t.Output.Meshes)).SequenceEqual(Triangles(built.SelectMany(o => o.Meshes)))).OverrideFailureMessage($"{lod}: source triangles reproduce tiled production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .SequenceEqual(built.SelectMany(o => o.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal))).OverrideFailureMessage("source parts reproduce production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.SelectMany(o => o.Instances))).OverrideFailureMessage("source instances reproduce production").IsTrue();
            string Dump(Collider collider) => System.Text.Json.JsonSerializer.Serialize<Collider>(collider, ContractJson.Options);
            AssertThat(traces.SelectMany(t => t.Colliders).Select(Dump).Order(StringComparer.Ordinal)
                .SequenceEqual(module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Select(Dump).Order(StringComparer.Ordinal))).OverrideFailureMessage("source colliders reproduce tiled production").IsTrue();
            foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
            {
                var own = site.Footprints(ctx.World);
                var contours = traces.Single(t => t.Feature == new FeatureId(site.Entity)).OccupiedContours;
                AssertThat(contours.Zip(site.GridBlocks(own).Select(b => b.Ring).Append(own.Hall)).All(pair => pair.First.Outer.SequenceEqual(pair.Second)) &&
                    contours.Count == site.GridBlocks(own).Count() + 1).OverrideFailureMessage("research sources use their generated blocks and hall, excluding neighbours").IsTrue();
            }
            var wenke = traces.Single(t => t.Feature == new FeatureId(CenterEastIds.Wenke.Entity));
            AssertThat(wenke.OccupiedContours.Select(r => r.Outer).SequenceEqual(WenkeSite.Footprints(ctx).Blocks.SelectMany(b => b.Outlines))).OverrideFailureMessage("Wenke keeps the actual clipped and rounded storeys").IsTrue();
            var frame = traces.Single(t => t.Feature == new FeatureId(CenterEastIds.Pinxue.Complex));
            foreach (var box in PinxueFrameSite.Bridges(ctx.World))
                AssertThat(frame.OccupiedContours.Any(r => r.Outer.SequenceEqual(box.Frame.Rect(box.X0, box.Z0, box.X1, box.Z1)))).OverrideFailureMessage("the frame uses its actual bridge plans").IsTrue();
        }

        static IEnumerable<string> Triangles(IEnumerable<GeometryBundle> meshes)
        {
            var triangles = new List<string>();
            foreach (var mesh in meshes)
                for (var i = 0; i < mesh.Index.Length; i += 3)
                {
                    string Vertex(int v) => string.Join(",", mesh.Position.Skip(v * 3).Take(3).Concat(mesh.Normal.Skip(v * 3).Take(3)).Select(BitConverter.SingleToInt32Bits));
                    triangles.Add($"{mesh.Material}/{Vertex(mesh.Index[i])}/{Vertex(mesh.Index[i + 1])}/{Vertex(mesh.Index[i + 2])}");
                }
            return triangles.Order(StringComparer.Ordinal);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ResearchRegistrationsMatchWhatIsBuilt()
    {
        var world = World();
        IReadOnlyList<Vector2> Outline(string id) => ((PolygonGeometry)world.Features[new(id)].Geometry).Rings.Outer;
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs((float)PlanRings.SignedArea(ring));
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var built = Area(site.Footprints(world).Hall);
            AssertThat(MathF.Abs(Area(Outline(site.HallId)) - built) < TestTolerance.Proportion * built).OverrideFailureMessage($"{site.HallId} outline matches the built hall").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ResearchBlocksStandFiveStoreysWithOneShutEntranceEach()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var top = ResearchSite.Style.Top;
        AssertThat(MathF.Abs(top - (R.GroundStorey + (R.Storeys - 1) * R.Storey + 1.3f)) < TestTolerance.Position).OverrideFailureMessage($"five storeys to the coping, got {top}").IsTrue();
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var plan = site.Footprints(ctx.World);
            var walls = colliders.OfType<WallCollider>().Where(w => plan.Blocks.Any(r => NearRing(r, (w.A + w.B) * 0.5f))).ToArray();
            AssertThat(walls.Any(w => w.Tag == SurfaceTag.Wall && MathF.Abs(w.Y1 - site.BaseY(ctx) - top) < TestTolerance.Position)).OverrideFailureMessage($"{site.Id} walls reach the coping above their terrain pad").IsTrue();
            var door = plan.Door ?? site.HallApex(plan).Apex;
            AssertThat(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, door) < 3f) is >= 1 and <= 2).OverrideFailureMessage($"{site.Id} has its shut entrance at the door").IsTrue();
        }

        static bool NearRing(IReadOnlyList<Vector2> ring, Vector2 p) => PlanRings.DistanceToRing(p, ring) < 1f;
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        AssertThat(module.Tiles(ctx).Any()).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = ZoneTestCells.Items(module, ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var cells = items.Select(i => TileGrid.At(i.Anchor, lod)).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var tile in lod == LodLevel.L4 ? [new TileKey(0, 0)] : cells)
            {
                var output = module.Build(ctx, tile, lod);
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
            // The entrance kit (mat, open sign, door lamp) is placed by WP-13 at every open entrance, never by the zone.
            string[] kit = [Contracts.Parts.PartCatalog.EntranceMat, Contracts.Parts.PartCatalog.OpenSign, Contracts.Parts.PartCatalog.EntranceLamp];
            AssertThat(!items.Any(i => i.Part is { } p && kit.Contains(p.Part.Value))).OverrideFailureMessage($"{lod}: no entrance kit placed by the zone").IsTrue();
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BuildIsDeterministic()
    {
        var ctx = Context();
        var cells = ZoneTestCells.Heaviest(new CenterEastModule(CenterEastSites.All.ToArray()), ctx);
        var a = Bytes(ctx, cells);
        var b = Bytes(Context(), cells);
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("two worlds give the same bytes").IsTrue();
    }

    private static byte[] Bytes(BuildContext ctx, IEnumerable<TileKey> cells)
    {
        var module = new CenterEastModule(CenterEastSites.All.ToArray());
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

    private static BuildContext Context()
    {
        // The center-east sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { CodeHash = "fixture", WindowNumbering = new((Descriptor()).Id, new string('0', 64), []), GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = Descriptor(), WorldSeed = 1 };
    }

    private static BuildInputSnapshot TextInputs() => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    {
        [SceneTextInputs.AtlasKey] = File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
    }, "fixture");

    private static MapDescriptor Descriptor() => RealMapFixture.Entry().Descriptor;

    // Geometry, pads and pool water levels all come from the same registered production world.
    private static IWorldModel World() => RealMapFixture.Load().World;
}
