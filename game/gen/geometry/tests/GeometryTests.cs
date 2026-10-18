using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.Geometry.Tests;

[TestSuite]
public sealed class GeometryTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void FrozenPublicationOwnsEveryWritableChannelAndLodIndex()
    {
        var source = Surface() with
        {
            Uv2 = [65537, 64, 65537, 64, 65537, 64],
            ColorIndices = [1, 2, 3],
            Flags = [0, 0, 0],
            Reserved = [1, 2, 3],
            Custom0 = Enumerable.Repeat(10f, 12).ToArray(),
            Custom1 = Enumerable.Repeat(20f, 12).ToArray(),
            Custom2 = Enumerable.Repeat(30f, 12).ToArray(),
            Lods = [new(.5f, [2, 1, 0])],
            Reflect = true,
            IgnoreOcclusionCulling = true,
        };
        var frozen = GeometryBuffer.Freeze(source);
        var published = GeometryPublication.Publish(frozen, true);
        foreach (var channel in new[] { published.Position, published.Normal, published.Ao!, published.Uv!, published.Uv2!, published.Custom0!, published.Custom1!, published.Custom2! })
            Array.Fill(channel, 99f);
        Array.Fill(published.Index, 99);
        Array.Fill(published.ColorIndices!, (ushort)99);
        Array.Fill(published.Flags!, (byte)8);
        Array.Fill(published.Reserved!, (byte)99);
        Array.Fill(published.Lods![0].Index, 99);
        var again = GeometryPublication.Publish(frozen, true);
        AssertThat(again.Position.SequenceEqual(source.Position) && again.Normal.SequenceEqual(source.Normal) && again.Index.SequenceEqual(source.Index)).OverrideFailureMessage("position, normal and index publication aliases the cache").IsTrue();
        AssertThat(again.Ao!.SequenceEqual(source.Ao!) && again.Uv!.SequenceEqual(source.Uv!) && again.Uv2!.SequenceEqual(source.Uv2!)).OverrideFailureMessage("AO or UV publication aliases the cache").IsTrue();
        AssertThat(again.ColorIndices!.SequenceEqual(source.ColorIndices!) && again.Flags!.SequenceEqual(source.Flags!) && again.Reserved!.SequenceEqual(source.Reserved!)).OverrideFailureMessage("window and colour publication aliases the cache").IsTrue();
        AssertThat(again.Custom0!.SequenceEqual(source.Custom0!) && again.Custom1!.SequenceEqual(source.Custom1!) && again.Custom2!.SequenceEqual(source.Custom2!)).OverrideFailureMessage("custom channel publication aliases the cache").IsTrue();
        AssertThat(again.Lods![0].Index.SequenceEqual(new[] { 2, 1, 0 }) && again.Lods[0].Error == .5f && again.Reflect && again.IgnoreOcclusionCulling).OverrideFailureMessage("LOD or surface metadata changed").IsTrue();

        var decal = Surface() with { Flags = [4, 4, 4], DecalBaseColorIndices = [3, 4, 5] };
        var frozenDecal = GeometryBuffer.Freeze(decal);
        GeometryPublication.Publish(frozenDecal).DecalBaseColorIndices![0] = 99;
        AssertThat(GeometryPublication.Publish(frozenDecal).DecalBaseColorIndices![0] == 3).OverrideFailureMessage("decal background indices alias the cache").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SingletonAndPassthroughPreserveAllChannelsWithoutAliasing()
    {
        var source = Surface();
        var passthrough = source with { Custom0 = new float[12], Lods = [new(.5f, [2, 1, 0])] };
        foreach (var policy in Enum.GetValues<GeometryMergePolicy>())
            foreach (var input in new[] { source, passthrough })
            {
                var result = GeometryBatch.Merge([input], policy).Single();
                result.Position[0] = 99;
                result.Index[0] = 99;
                if (result.Custom0 is not null) result.Custom0[0] = 99;
                if (result.Lods is not null) result.Lods[0].Index[0] = 99;
                AssertThat(input.Position[0] == 0 && input.Index[0] == 0 && (input.Custom0 is null || input.Custom0[0] == 0) &&
                    (input.Lods is null || input.Lods[0].Index[0] == 2)).OverrideFailureMessage($"{policy} singleton or passthrough kept mutable input storage").IsTrue();
            }
        var builder = new SiteBuilder(new BuildContext { World = null!, Map = null!, Catalog = null!, WorldSeed = 1 }, LodLevel.L0, SiteDetail.Full);
        builder.Add(source with { Lods = [] }, Matrix4x4.Identity);
        var item = builder.Items.Single();
        var restored = GeometryPublication.Publish(item.Mesh!, item.HasLodChannel);
        AssertThat(restored.Lods is { Count: 0 }).OverrideFailureMessage("an explicitly empty LOD channel became null").IsTrue();
        AssertThat(GeometryBatch.Merge([restored, source], GeometryMergePolicy.Plain).Count == 2).OverrideFailureMessage("an explicitly empty LOD channel changed merge eligibility").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MergePoliciesRetainBucketOrderAndNumberedWindowIndices()
    {
        var a = Surface();
        var b = Surface() with { Material = new(new ToonSpec(new("other.lit"), new("other.shade"))) };
        var special = a with { Custom0 = new float[12] };
        var plain = GeometryBatch.Merge([a, b, special, a], GeometryMergePolicy.Plain);
        AssertThat(plain.Count == 3 && plain[0].Custom0 is not null && plain[1].Index.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }) && plain[2].Material == b.Material).OverrideFailureMessage("streaming passthrough or first-use bucket order changed").IsTrue();
        var grouped = GeometryBatch.Merge([a, b, special, a], GeometryMergePolicy.MaterialGroups);
        AssertThat(grouped.Count == 4 && grouped[0].Custom0 is null && grouped[1].Custom0 is not null && grouped[3].Material == b.Material).OverrideFailureMessage("public-space grouping must passthrough the entire special group and retain group order").IsTrue();
        var window = a with { Uv2 = [65537, 32, 65537, 32, 65537, 32], ColorIndices = [1, 2, 3], Flags = [0, 0, 0], Reserved = [1, 2, 3] };
        var merged = GeometryBatch.Merge([window, window], GeometryMergePolicy.WindowChannels).Single();
        AssertThat(merged.Index.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }) && merged.Uv2!.SequenceEqual(window.Uv2!.Concat(window.Uv2)) &&
            merged.ColorIndices!.SequenceEqual(new ushort[] { 1, 2, 3, 1, 2, 3 }) && merged.Reserved!.SequenceEqual(new byte[] { 1, 2, 3, 1, 2, 3 })).OverrideFailureMessage("numbered window channels or index offsets changed").IsTrue();
        AssertThat(GeometryBatch.Merge([window, window], GeometryMergePolicy.Plain).Count == 2).OverrideFailureMessage("plain regions changed their window merge policy").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlacementKeepsScalarRowOrderNormalsAndColliderMetadata()
    {
        var at = new Matrix4x4(0, 0, 2, 0, 0, 2, 0, 0, -2, 0, 0, 0, 11, 7, 13, 1);
        var (anchor, mesh) = GeometryPlacement.Place(Surface(), at);
        AssertThat(mesh.Position.SequenceEqual(new float[] { 11, 7, 13, 11, 7, 15, 11, 9, 13 }) && anchor == new Vector2(11, 14)).OverrideFailureMessage("placed vertex/anchor order changed").IsTrue();
        AssertThat(mesh.Normal.SequenceEqual(new float[] { -1, 0, 0, -1, 0, 0, -1, 0, 0 })).OverrideFailureMessage("placed normal rule changed").IsTrue();
        var wall = new WallCollider(new(1, 2), new(3, 4), .2f, 1, 2, SurfaceTag.Boundary) { Foot = Contracts.Audio.FootMat.Metal };
        var placed = (WallCollider)GeometryPlacement.Colliders([wall], at).Single();
        AssertThat(placed.A == new Vector2(7, 15) && placed.B == new Vector2(3, 19) && placed.Y0 == 8 && placed.Y1 == 9 && placed.Foot == wall.Foot && placed.Tag == wall.Tag).OverrideFailureMessage("collider plan placement or semantic metadata changed").IsTrue();
    }

    private static GeometryBundle Surface() => new(new(new ToonSpec(new("fixture.lit"), new("fixture.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2], [.25f, .5f, 1f], [0, 0, 1, 0, 0, 1]);

}
