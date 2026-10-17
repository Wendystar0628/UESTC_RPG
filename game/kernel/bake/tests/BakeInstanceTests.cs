using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeInstanceTests
{
    [TestCase]
    public void MultipleSurfacesAndSubBlocksKeepWorldRecordsAndCacheBytes()
    {
        var firstSource = Surface("fixture.first", "fixture.first.shade");
        var secondSource = Surface("fixture.second", "fixture.second.shade") with
        {
            Shader = ShaderId.Water,
            IgnoreOcclusionCulling = true,
            Uv = [2.5f, -1.25f, .5f, 3f, 2f, 4f],
            Uv2 = [65535, 17, 65536, 33, 16777216, -1],
            ColorIndices = [66, 68, 66],
            Ao = [.25f, .5f, .75f],
            Flags = [2, 2, 2],
            Reserved = [7, 8, 9],
            Custom0 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            Custom2 = [-4, 3, 2, 1, -8, 7, 6, 5, -12, 11, 10, 9]
        };
        var frozenFirst = GeometryBuffer.Freeze(firstSource);
        var frozenSecond = GeometryBuffer.Freeze(secondSource);
        var prototype = new GeometryPrototype("fixture.multi", [frozenFirst, frozenSecond], PrototypeBounds());
        var at0 = Matrix4x4.CreateScale(1.5f, 2f, .5f) * DetMath.RotationY(.7f) * Matrix4x4.CreateTranslation(-480, 20, 300);
        var at1 = Matrix4x4.CreateTranslation(-470, 22, 315);
        var at2 = Matrix4x4.CreateTranslation(-430, 30, 350);
        var negativeZero = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
        var records0 = Records((at0, new(.25f, .5f, .75f, negativeZero)), (at1, new(11, 12, 13, 14)));
        var records1 = Records((at2, new(21, 22, 23, 24)));
        var bounds0 = new Box3(new(-512, 0, 256), new(-448, 64, 320));
        var bounds1 = new Box3(new(-448, 0, 320), new(-384, 64, 384));
        var stream0 = new GeometryInstanceStream("block/-8/4", prototype, records0, bounds0);
        var stream1 = new GeometryInstanceStream("block/-7/5", prototype, records1, bounds1);
        records0[3] = 12345; firstSource.Position[0] = 54321; // Frozen inputs must not follow their old mutable owners.
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.instances", new(-2, 1), LodLevel.L2);
        var adapter = new BakeInstanceAdapter(Palette());
        var bundle = adapter.PackTile(key, new(-2, 1), LodLevel.L2, new(new(-512, 0, 256), new(-256, .001f, 512)), [], [stream0, stream1]);
        if (bundle.Bounds != new Box3(new(-512, 0, 256), new(-256, 64, 512)))
            throw new InvalidOperationException("Tile culling bounds did not include instance heights beyond the static geometry range.");
        if (bundle.MultiMeshes.Count != 4 || bundle.Surfaces.Count != 0 || bundle.Stats.Instances != 6 || bundle.Stats.Surfaces != 4)
            throw new InvalidOperationException("A two-surface prototype must produce two independent MultiMeshes per sub-block.");
        if (!ReferenceEquals(bundle.MultiMeshes[0].Surface, bundle.MultiMeshes[2].Surface) ||
            !ReferenceEquals(bundle.MultiMeshes[1].Surface, bundle.MultiMeshes[3].Surface))
            throw new InvalidOperationException("The same frozen prototype was packed again for another tile sub-block.");
        CheckStream(bundle.MultiMeshes[0], stream0, at0);
        CheckStream(bundle.MultiMeshes[1], stream0, at0);
        CheckStream(bundle.MultiMeshes[2], stream1, at2);
        CheckStream(bundle.MultiMeshes[3], stream1, at2);
        if (bundle.MultiMeshes[0].Surface.Material != frozenFirst.Material || bundle.MultiMeshes[1].Surface.Material != frozenSecond.Material ||
            bundle.MultiMeshes[1].Surface.Shader != ShaderId.Water || !bundle.MultiMeshes[1].Surface.IgnoreOcclusionCulling ||
            bundle.MultiMeshes[0].Surface.IgnoreOcclusionCulling)
            throw new InvalidOperationException("Surface material slots, shader or occlusion policy were lost.");
        CompareSurface(bundle.MultiMeshes[0].Surface, BakeSurfacePacker.Pack(frozenFirst, PrototypeBounds(), Palette()));
        CompareSurface(bundle.MultiMeshes[1].Surface, BakeSurfacePacker.Pack(frozenSecond, PrototypeBounds(), Palette()));
        var bytes = TileBundleCodec.Encode("fixture/instances", bundle);
        var restored = (TileBundle)TileBundleCodec.Decode("fixture/instances", key, bytes);
        if (!bytes.SequenceEqual(TileBundleCodec.Encode("fixture/instances", restored)))
            throw new InvalidOperationException("Multi-surface instance cache bytes changed in a round trip.");
        for (var i = 0; i < restored.MultiMeshes.Count; i++)
        {
            CompareSurface(restored.MultiMeshes[i].Surface, bundle.MultiMeshes[i].Surface);
            if (restored.MultiMeshes[i].Bounds != bundle.MultiMeshes[i].Bounds ||
                !restored.MultiMeshes[i].InstanceData.Span.SequenceEqual(bundle.MultiMeshes[i].InstanceData.Span))
                throw new InvalidOperationException("Cache decode changed a sub-block's world bounds or 16-float records.");
        }
        using var stats = JsonDocument.Parse(bundle.StatisticsJson);
        var root = stats.RootElement;
        var expectedBytes = bundle.MultiMeshes.Sum(multi => multi.Surface.ByteCount + multi.InstanceData.Length);
        if (bundle.Stats.Bytes != expectedBytes || root.GetProperty("bytes").GetInt64() != expectedBytes ||
            root.GetProperty("logicalInstances").GetInt64() != 3 || root.GetProperty("instances").GetInt64() != 6 ||
            root.GetProperty("instanceBufferBytes").GetInt64() != 6 * 64 || root.GetProperty("multiMeshes").GetInt32() != 4)
            throw new InvalidOperationException("Statistics hid duplicated per-surface buffers/instances.");
        var groups = root.GetProperty("instanceStreams");
        if (groups.GetArrayLength() != 2 || groups[0].GetProperty("id").GetString() != stream0.Id ||
            groups[1].GetProperty("id").GetString() != stream1.Id || groups[1].GetProperty("firstMultiMesh").GetInt32() != 2)
            throw new InvalidOperationException("Independent 64 m stream identities were merged or reordered.");
    }

    [TestCase]
    public void PlanarPrototypeBoundsRemainExact()
    {
        var bounds = new Box3(Vector3.Zero, new(1, 1, 0));
        var prototype = new GeometryPrototype("flat-card", [GeometryBuffer.Freeze(Surface("fixture.first", "fixture.first.shade"))], bounds);
        var worldBounds = new Box3(new(-80, 4, 126), new(-76, 8, 130));
        var stream = new GeometryInstanceStream("flat-card/block", prototype,
            Records((Matrix4x4.CreateTranslation(-78, 5, 128), Vector4.Zero)), worldBounds);
        var result = new BakeInstanceAdapter(Palette()).Pack([stream]).Single();
        if (result.Surface.Bounds != bounds || result.Bounds != worldBounds)
            throw new InvalidOperationException("A planar prototype or world sub-block bound was silently expanded.");
        for (var i = 0; i < 3; i++)
            if (BinaryPrimitives.ReadUInt16LittleEndian(result.Surface.VertexData.Span[(i * 8 + 4)..]) != 0)
                throw new InvalidOperationException("A constant prototype coordinate was not encoded deterministically.");
    }

    [TestCase]
    public void UnsupportedInstanceSurfaceStatesAreExplicitFailures()
    {
        var source = Surface("fixture.first", "fixture.first.shade");
        Reject(source with { Reflect = true });
        Reject(source with { Variant = new VariantTag() });
    }

    private static void Reject(GeometryBundle source)
    {
        var frozen = GeometryBuffer.Freeze(source);
        var stream = new GeometryInstanceStream("unsupported", new("unsupported", [frozen], PrototypeBounds()),
            Records((Matrix4x4.Identity, Vector4.Zero)), PrototypeBounds());
        ExpectUnsupported(() => new BakeInstanceAdapter(Palette()).Pack([stream]));
    }
    private static void ExpectUnsupported(Action action)
    {
        try { action(); }
        catch (NotSupportedException) { return; }
        throw new InvalidOperationException("An unsupported instance state was silently accepted.");
    }
    private static void CheckStream(BakedMultiMesh multi, GeometryInstanceStream stream, Matrix4x4 firstTransform)
    {
        if (multi.InstanceCount != stream.Count || multi.Bounds != stream.Bounds || multi.Surface.At != Matrix4x4.Identity)
            throw new InvalidOperationException("Instance count/bounds changed or a second prototype transform was added.");
        for (var i = 0; i < stream.Data.Length; i++)
            if (BinaryPrimitives.ReadInt32LittleEndian(multi.InstanceData.Span[(i * 4)..]) != BitConverter.SingleToInt32Bits(stream.Data.Span[i]))
                throw new InvalidOperationException("Instance ordering or a float/custom bit changed.");
        var point = new Vector3(.5f, 2, -1);
        var actual = new Vector3(Row(0), Row(4), Row(8));
        var expected = Vector3.Transform(point, firstTransform);
        if (Vector3.Distance(actual, expected) > .0001f)
            throw new InvalidOperationException("Prototype-local to world transform gained a tile origin or changed convention.");
        float Row(int offset) => Read(offset) * point.X + Read(offset + 1) * point.Y + Read(offset + 2) * point.Z + Read(offset + 3);
        float Read(int index) => BinaryPrimitives.ReadSingleLittleEndian(multi.InstanceData.Span[(index * 4)..]);
    }
    private static void CompareSurface(BakedSurface actual, BakedSurface expected)
    {
        if (actual.Material != expected.Material || actual.Shader != expected.Shader || actual.Format != expected.Format ||
            actual.VertexCount != expected.VertexCount || actual.IndexCount != expected.IndexCount || actual.Bounds != expected.Bounds ||
            actual.At != expected.At || actual.UvScale != expected.UvScale || actual.IgnoreOcclusionCulling != expected.IgnoreOcclusionCulling ||
            actual.Variant != expected.Variant || actual.Reflect != expected.Reflect ||
            !actual.VertexData.Span.SequenceEqual(expected.VertexData.Span) || !actual.AttributeData.Span.SequenceEqual(expected.AttributeData.Span) ||
            !actual.IndexData.Span.SequenceEqual(expected.IndexData.Span) || !actual.SkinData.Span.SequenceEqual(expected.SkinData.Span) || actual.Lods.Count != expected.Lods.Count)
            throw new InvalidOperationException("A packed prototype surface lost a material, vertex stream, bound or flag.");
        for (var i = 0; i < actual.Lods.Count; i++)
            if (actual.Lods[i].Error != expected.Lods[i].Error || !actual.Lods[i].IndexData.Span.SequenceEqual(expected.Lods[i].IndexData.Span))
                throw new InvalidOperationException("Prototype LOD data changed.");
    }
    private static float[] Records(params (Matrix4x4 At, Vector4 Custom)[] values)
    {
        var data = new float[values.Length * 16];
        for (var i = 0; i < values.Length; i++) InstanceStreamLayout.Write(data.AsSpan(i * 16, 16), values[i].At, values[i].Custom);
        return data;
    }
    private static Box3 PrototypeBounds() => new(new(-2, 0, -2), new(2, 8, 2));
    private static GeometryBundle Surface(string lit, string shade) => new(new(new(new(lit), new(shade))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2],
        Uv: [0, 0, 1, 0, 0, 1], Lods: [new MeshLod(.5f, [0, 2, 1])]);
    private static PaletteCatalog Palette() => new(new PaletteRegistry([
        new(new("fixture.first"), 64, Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.first.shade"), 65, new Vector3(.5f), "fixture", false, PaletteRole.Shade),
        new(new("fixture.second"), 66, Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.second.shade"), 67, new Vector3(.5f), "fixture", false, PaletteRole.Shade),
        new(new("fixture.accent"), 68, Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.accent.shade"), 69, new Vector3(.5f), "fixture", false, PaletteRole.Shade)]), []);
}
