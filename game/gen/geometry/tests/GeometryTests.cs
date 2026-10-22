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
        AssertThat(mesh.Position.Length / 3 == 4 && mesh.Index.Length == 12)
            .OverrideFailureMessage("Repeated triangle corners must reuse their full vertex record across writer entry points.").IsTrue();

        var seams = new GeometrySink();
        seams.Tri(source.Material.Spec, a, b, c);
        seams.Tri(source.Material.Spec, a with { Ao = .5f }, b, c);
        seams.Tri(source.Material.Spec, a with { Uv = Vector2.One }, b, c);
        AssertThat(seams.Finish().Single().Position.Length / 3 == 5)
            .OverrideFailureMessage("Coincident vertices with different authored attributes must retain their seams.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void WriterKeepsMaterialClassesAndOriginalOrdinalOrder()
    {
        var spec = Surface().Material.Spec;
        ToonSpec[] classes = [spec, spec with { Kind = ToonKind.Glass }, spec with { Fx = ToonFx.Rim },
            spec with { Line = LineClass.None }, spec with { Steps = ShadeSteps.Three },
            spec with { Normal = ToonNormal.Smooth }, spec with { Band = .5f },
            spec with { Translucency = .5f }, spec with { Wet = 1f }, spec with { Deep = new("deep") },
            spec with { Deep = new("") }, spec with { Deep = new("-") },
            spec with { Backface = new("backface") }, spec with { Backface = new("") }, spec with { Backface = new("-") },
            spec with { Emissive = new("emissive") }, spec with { Emissive = new("") }, spec with { Emissive = new("-") },
            spec with { SurfacePattern = new(1, Vector2.One, Vector2.Zero) },
            spec with { Lit = new("other.lit") }, spec with { Shade = new("other.shade") }];
        var a = new GeometryVertex(Vector3.Zero, Vector3.UnitZ);
        var b = new GeometryVertex(Vector3.UnitX, Vector3.UnitZ);
        var c = new GeometryVertex(Vector3.UnitY, Vector3.UnitZ);
        var sink = new GeometrySink();
        foreach (var material in classes)
        {
            sink.Tri(material, a, b, c);
            sink.Tri(material with { }, a, b, c);
        }
        var output = sink.Finish();
        AssertThat(output.Count == classes.Length && classes.All(material =>
            output.Single(mesh => mesh.Material.Spec == material).Index.Length == 2 * Surface().Index.Length))
            .OverrideFailureMessage("Material attributes must keep separate classes, including equal specs from different instances.").IsTrue();

        var ordinal = new GeometrySink();
        var two = spec with { Band = 2f };
        var ten = spec with { Band = 10f };
        ordinal.Tri(two, a, b, c);
        ordinal.Tri(ten, a, b, c);
        AssertThat(ordinal.Finish().Select(mesh => mesh.Material.Spec).SequenceEqual(new[] { ten, two }))
            .OverrideFailureMessage("Material output must retain ordinal round-trip-string order rather than numeric order.").IsTrue();

        var colored = new GeometrySink();
        a = a with { ColorIndex = 2 }; b = b with { ColorIndex = 2 }; c = c with { ColorIndex = 2 };
        colored.Tri(spec, a, b, c);
        colored.Tri(spec with { Lit = new("other.lit"), Shade = new("other.shade") }, a, b, c);
        AssertThat(colored.Finish().Single().Index.Length == 2 * Surface().Index.Length)
            .OverrideFailureMessage("Vertex colors replace only the lit/shade material pair when grouping.").IsTrue();

        var distinct = new GeometrySink();
        var first = spec with { Lit = new("a|b"), Shade = new("c") };
        var second = spec with { Lit = new("a"), Shade = new("b|c") };
        distinct.Tri(first, a, b, c);
        distinct.Tri(second, a with { ColorIndex = null },
            b with { ColorIndex = null }, c with { ColorIndex = null });
        distinct.Tri(first, a with { ColorIndex = null },
            b with { ColorIndex = null }, c with { ColorIndex = null });
        var distinctOutput = distinct.Finish();
        AssertThat(distinctOutput.Count == 3 && distinctOutput.All(mesh => mesh.Index.Length == Surface().Index.Length) &&
            distinctOutput.Single(mesh => mesh.ColorIndices is not null).Material.Spec == first &&
            distinctOutput.Where(mesh => mesh.ColorIndices is null).Select(mesh => mesh.Material.Spec)
                .ToHashSet().SetEquals(new[] { first, second }))
            .OverrideFailureMessage("Distinct material identities must keep their own colours even when their sort strings collide.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void WriterPreservesAuthoredChannelsAndNormalizesOnlyStreamNormals()
    {
        var source = Surface() with
        {
            Normal = Surface().Normal.Select(value => 2 * value).ToArray(),
            Uv2 = [65537, 32, 65537, 32, 65537, 32],
            ColorIndices = [1, 2, 3], Flags = [0, 1, 2], Reserved = [3, 4, 5]
        };
        var sink = new GeometrySink();
        sink.Append(source, Matrix4x4.Identity);
        var output = sink.Finish().Single();
        AssertThat(output.Position.SequenceEqual(source.Position) && output.Normal.SequenceEqual(Surface().Normal) &&
            output.Index.SequenceEqual(new[] { source.Index[0], source.Index[2], source.Index[1] }) &&
            output.Ao!.SequenceEqual(source.Ao!) && output.Uv!.SequenceEqual(source.Uv!) && output.Uv2!.SequenceEqual(source.Uv2!))
            .OverrideFailureMessage("Stream packing must retain geometry, AO and both UV channels while normalizing normals and keeping clockwise winding.").IsTrue();
        AssertThat(output.ColorIndices!.SequenceEqual(source.ColorIndices!) && output.Flags!.SequenceEqual(source.Flags!) &&
            output.Reserved!.SequenceEqual(source.Reserved!))
            .OverrideFailureMessage("Stream packing must retain color, flag and reserved channels.").IsTrue();
        var decal = source with { Reserved = null, Flags = Enumerable.Repeat(VertexFlag.Decal, source.Index.Length).ToArray(),
            DecalBaseColorIndices = [7, 8, 9] };
        var decals = new GeometrySink();
        decals.Append(decal, Matrix4x4.Identity);
        var packedDecal = decals.Finish().Single();
        AssertThat(packedDecal.DecalBaseColorIndices!.SequenceEqual(decal.DecalBaseColorIndices!) && packedDecal.Reserved is null)
            .OverrideFailureMessage("Decal background colors must retain their separate channel without a reserved payload.").IsTrue();

        GeometryVertex[] authored = [new(Vector3.Zero, 2 * Vector3.UnitZ), new(Vector3.UnitX, 2 * Vector3.UnitZ),
            new(Vector3.UnitY, 2 * Vector3.UnitZ), new(Vector3.One, 2 * Vector3.UnitZ)];
        var custom = authored.Select(vertex => new Vector4(vertex.Position, 1f)).ToArray();
        var indexed = GeometrySink.Indexed(source.Material.Spec, authored, source.Index,
            GeometryChannels.Ao | GeometryChannels.Uv | GeometryChannels.Uv2 | GeometryChannels.Flags | GeometryChannels.Reserved,
            custom, custom, custom);
        AssertThat(indexed.Position.Length == authored.Length * 3 && indexed.Index.SequenceEqual(source.Index) &&
            indexed.Normal.Where((_, i) => i % 3 == 2).All(value => value == authored[0].Normal.Z) &&
            indexed.Ao!.All(value => value == 1f) && indexed.Uv!.All(value => value == 0f) &&
            indexed.Uv2!.All(value => value == 0f) && indexed.Flags!.All(value => value == 0) && indexed.Reserved!.All(value => value == 0))
            .OverrideFailureMessage("Authored indexed surfaces must retain unused vertices, normals, winding and explicit default channels.").IsTrue();
        AssertThat(indexed.Custom0!.SequenceEqual(indexed.Custom1!) && indexed.Custom0.SequenceEqual(indexed.Custom2!) &&
            custom.SelectMany(value => new[] { value.X, value.Y, value.Z, value.W }).SequenceEqual(indexed.Custom0!))
            .OverrideFailureMessage("Indexed custom channels must retain all four components in authored order.").IsTrue();
        var empty = GeometrySink.Indexed(source.Material.Spec, [], [], GeometryChannels.Ao | GeometryChannels.Uv, []);
        AssertThat(empty.Ao is { Length: 0 } && empty.Uv is { Length: 0 } && empty.Custom0 is { Length: 0 } && empty.Uv2 is null)
            .OverrideFailureMessage("Empty indexed surfaces must preserve explicitly present channels.").IsTrue();
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
