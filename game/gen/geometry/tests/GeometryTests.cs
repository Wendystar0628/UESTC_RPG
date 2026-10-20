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
        foreach (var input in new[] { source, passthrough })
        {
            var result = GeometryBatch.Merge([input]).Single();
            result.Position[0] = 99;
            result.Index[0] = 99;
            if (result.Custom0 is not null) result.Custom0[0] = 99;
            if (result.Lods is not null) result.Lods[0].Index[0] = 99;
            AssertThat(input.Position[0] == 0 && input.Index[0] == 0 && (input.Custom0 is null || input.Custom0[0] == 0) &&
                (input.Lods is null || input.Lods[0].Index[0] == 2)).OverrideFailureMessage("singleton or passthrough kept mutable input storage").IsTrue();
        }
        var builder = new SiteBuilder(new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), World = null!, Map = null!, Catalog = null!, WorldSeed = 1 }, LodLevel.L0, SiteDetail.Full);
        builder.Add(source with { Lods = [] }, Matrix4x4.Identity);
        var item = builder.Items.Single();
        var restored = GeometryPublication.Publish(item.Mesh!, item.HasLodChannel);
        AssertThat(restored.Lods is { Count: 0 }).OverrideFailureMessage("an explicitly empty LOD channel became null").IsTrue();
        AssertThat(GeometryBatch.Merge([restored, source]).Count == 2).OverrideFailureMessage("an explicitly empty LOD channel changed merge eligibility").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SharedMergePreservesNumberedWindowsAndCustomSurfaces()
    {
        var surface = Surface();
        var special = surface with { Custom0 = new float[12] };
        var window = surface with { Uv2 = [65537, 32, 65537, 32, 65537, 32],
            ColorIndices = [1, 1, 1], Flags = [0, 0, 0], Reserved = [1, 2, 3] };
        var output = GeometryBatch.Merge([window, special, window]);
        var merged = output.Single(mesh => mesh.Custom0 is null);
        AssertThat(output.Count == 2 && output.Single(mesh => mesh.Custom0 is not null).Custom0!.SequenceEqual(special.Custom0!))
            .OverrideFailureMessage("Custom vertex formats must pass through unchanged.").IsTrue();
        var expected = new[] { 0, 2, 1, 0, 2, 1 };
        AssertThat(merged.Index.Length == expected.Length && Enumerable.Range(0, expected.Length).All(i =>
            merged.Uv2![2 * merged.Index[i]] == window.Uv2![2 * expected[i]] &&
            merged.Uv2[2 * merged.Index[i] + 1] == window.Uv2[2 * expected[i] + 1] &&
            merged.ColorIndices![merged.Index[i]] == window.ColorIndices![expected[i]] &&
            merged.Reserved![merged.Index[i]] == window.Reserved![expected[i]] &&
            Enumerable.Range(0, 3).All(axis => merged.Position[3 * merged.Index[i] + axis] == window.Position[3 * expected[i] + axis])))
            .OverrideFailureMessage("Shared merging must keep window channels and clockwise winding.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void SharedWriterReusesCompleteVerticesAcrossTrianglesQuadsAndAppends()
    {
        var source = Surface();
        var sink = new GeometrySink();
        var a = new GeometryVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero, Ao: .25f);
        var b = new GeometryVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX, Ao: .5f);
        var c = new GeometryVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY, Ao: 1f);
        var d = new GeometryVertex(new(1, 1, 0), Vector3.UnitZ, Vector2.One, Ao: .75f);
        sink.Tri(source.Material.Spec, a, b, c);
        sink.Quad(source.Material.Spec, a, b, d, c);
        sink.Append(source, Matrix4x4.Identity);
        var mesh = sink.Finish().Single();
        AssertThat(mesh.Position.Length / 3 == 4 && mesh.Index.Length == 12 && sink.Triangles == 4)
            .OverrideFailureMessage("Repeated triangle corners must reuse their full vertex record across writer entry points.").IsTrue();

        var seams = new GeometrySink();
        seams.Tri(source.Material.Spec, a, b, c);
        seams.Tri(source.Material.Spec, a with { Ao = .5f }, b, c);
        seams.Tri(source.Material.Spec, a with { Uv = Vector2.One }, b, c);
        AssertThat(seams.Finish().Single().Position.Length / 3 == 5)
            .OverrideFailureMessage("Coincident vertices with different authored attributes must retain their seams.").IsTrue();
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
