using static GdUnit4.Assertions;
using Campus.Pipeline.Map;
using System.Text;
using System.Numerics;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class BuildInputAssemblyTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AssetBoundaryReadsAndFreezesOnceAcrossMapConsumers()
    {
        var raw = Encoding.UTF8.GetBytes("species-version-one");
        var reads = 0;
        var assets = new Lazy<BuildInputSnapshot>(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["vegetation/species.json"], key => { reads++; return raw; }, "fixture"));
        var firstMap = assets.Value;
        raw[0] = 0;
        var secondMap = assets.Value;
        AssertThat(reads == 1 && ReferenceEquals(firstMap, secondMap)).OverrideFailureMessage("Asset bytes were read or frozen again per map.").IsTrue();
        AssertThat(firstMap.Require("vegetation/species.json").Span[0] == (byte)'s').OverrideFailureMessage("Published bytes alias a caller-owned mutable array.").IsTrue();
        var empty = BuildInputAssembly.ReadRequiredGlobalInputs([], _ => throw new InvalidOperationException("Unexpected fixture IO."), "fixture");
        AssertThat(ReferenceEquals(empty, BuildInputSnapshot.Empty)).OverrideFailureMessage("A fixture with no requested global assets lost its empty default.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RequiredInputsFailBeforeAnyConsumerRuns()
    {
        AssertThrown(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["vegetation/species.json"], _ => ReadOnlyMemory<byte>.Empty, "fixture")).IsInstanceOf<InvalidDataException>();
        var read = false;
        AssertThrown(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["res://data/vegetation/species.json"], _ => { read = true; return new byte[] { 1 }; }, "fixture")).IsInstanceOf<ArgumentException>();
        AssertThat(!read).OverrideFailureMessage("A non-canonical key reached the IO boundary.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ContributorsAreReadOnlyAndChannelConflictsFailBeforePainting()
    {
        var first = new Contributor("z", GroundMaskChannels.CanopyShelter);
        var second = new Contributor("a", GroundMaskChannels.GinkgoLeaves);
        var source = new List<IGroundMaskContributor> { first, second };
        var snapshot = BuildInputAssembly.FreezeGroundMaskContributors(source);
        source.Clear();
        AssertThat(snapshot.Select(item => item.Id).SequenceEqual(["a", "z"])).OverrideFailureMessage("Contributor snapshot was mutable or unsorted.").IsTrue();
        AssertThrown(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [second, new Contributor("conflict", GroundMaskChannels.GinkgoLeaves)])).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [second, new Contributor("a", GroundMaskChannels.OtherLeaves)])).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [new Contributor("unknown", (GroundMaskChannels)16)])).IsInstanceOf<InvalidOperationException>();
        AssertThat(first.PaintCalls == 0 && second.PaintCalls == 0).OverrideFailureMessage("Composition painted buffers before ownership validation completed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CapturedPartCollisionKeepsRealStairAndPlatformSurfacesAndRejectsWrongParameters()
    {
        var definitions = new Dictionary<PartId, IPartDef>
        {
            [new(PartCatalog.Steps)] = new StepsPart(),
            [new(PartCatalog.Platform)] = new PlatformPart()
        };
        var collision = new PartCollisionBinding(definitions);
        definitions.Clear();
        var steps = collision.Collision(new(new(PartCatalog.Steps), new StairParams(2, .9f, 1.8f, 6), Matrix4x4.Identity));
        var platform = collision.Collision(new(new(PartCatalog.Platform), new PanelParams(2, .3f, 2), Matrix4x4.Identity));
        AssertThat(steps.Any(collider => collider is RampCollider { Tag: SurfaceTag.Stair }) &&
            platform.Any(collider => collider is BoxCollider { Tag: SurfaceTag.Ground })).OverrideFailureMessage("Captured definitions lost their authored support surfaces.").IsTrue();
        AssertThrown(() => collision.Collision(new(new(PartCatalog.Steps), new object(), Matrix4x4.Identity))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => collision.Collision(new(new("unregistered"), new object(), Matrix4x4.Identity))).IsInstanceOf<InvalidOperationException>();
    }

    // No registration attributes or test-assembly scanning: this exercises a detached capability list only.
    private sealed class Contributor(string id, GroundMaskChannels channels) : IContentModule, IGroundMaskContributor
    {
        public string Id => id;
        public BakeInputDependencies BakeInputs => BakeInputDependencies.World;
        public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;
        GroundMaskChannels IGroundMaskContributor.Channels => channels;
        public int PaintCalls { get; private set; }
        public IReadOnlyList<Claim> Claims => [];
        public IEnumerable<TileKey> Tiles(BuildContext context) => [];
        public ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod) => ContentOutput.Empty;
        public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2) => PaintCalls++;
    }
}
