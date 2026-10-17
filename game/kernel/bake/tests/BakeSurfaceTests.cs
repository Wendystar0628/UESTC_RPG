using System.Buffers.Binary;
using Campus.Contracts;
using Campus.Contracts.Bake;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeSurfaceTests
{
    [TestCase]
    public void AbsentOptionalChannelsStayAbsentAndEmptyArraysRemainInvalid()
    {
        var absent = Geometry() with
        {
            Ao = null,
            Uv = null,
            Uv2 = null,
            ColorIndices = null,
            Flags = null,
            Reserved = null,
            Custom0 = null,
            Custom1 = null,
            Custom2 = null
        };
        var packed = BakeSurfacePacker.Pack(absent, Bounds(), Palette());
        var frozen = BakeSurfacePacker.Pack(GeometryBuffer.Freeze(absent), Bounds(), Palette());
        if ((packed.Format & (ulong)RenderingServer.ArrayFormat.FlagCompressAttributes) == 0 ||
            (packed.Format & (ulong)RenderingServer.ArrayFormat.FormatTexUV2) != 0 ||
            packed.VertexData.Length != 36 || packed.AttributeData.Length != 24 || packed.Format != frozen.Format ||
            !packed.VertexData.Span.SequenceEqual(frozen.VertexData.Span) || !packed.AttributeData.Span.SequenceEqual(frozen.AttributeData.Span))
            throw new InvalidOperationException("Absent channels became present empty memory or changed the frozen-surface ABI.");
        for (var vertex = 0; vertex < 3; vertex++)
        {
            var data = packed.AttributeData.Span.Slice(vertex * 8, 8);
            if (data[0] != 64 || data[1] != 255 || data[2..].ToArray().Any(value => value != 0))
                throw new InvalidOperationException("Absent channels failed to select their defined defaults.");
        }
        // Empty is supplied-but-invalid for a nonempty mesh. Never normalize Length=0 to missing.
        foreach (var malformed in new[]
        {
            absent with { Ao = [] }, absent with { Uv = [] }, absent with { Uv2 = [] }, absent with { ColorIndices = [] },
            absent with { Flags = [] }, absent with { Reserved = [] }, absent with { Custom0 = [] },
            absent with { Custom1 = [] }, absent with { Custom2 = [] }
        })
        {
            try { _ = BakeSurfacePacker.Pack(malformed, Bounds(), Palette()); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("An invalid empty optional array was silently treated as absent.");
        }
    }

    [TestCase]
    public void PlacedTintOverridesExplicitColorIndicesWithoutChangingOtherVertexBytes()
    {
        var palette = new PaletteCatalog(new PaletteRegistry(new PaletteEntry[]
        {
            new(new("fixture.lit"), 64, N.Vector3.One, "fixture", false, PaletteRole.Lit),
            new(new("fixture.shade"), 65, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade),
            new(new("fixture.tint"), 66, N.Vector3.One, "fixture", false, PaletteRole.Lit),
            new(new("fixture.tint.shade"), 67, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade),
            new(new("fixture.accent"), 68, N.Vector3.One, "fixture", false, PaletteRole.Lit),
            new(new("fixture.accent.shade"), 69, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade)
        }), []);
        foreach (var useUv2 in new[] { false, true })
        {
            ushort[] originalIds = [64, 68, 64];
            var original = Geometry() with
            {
                ColorIndices = originalIds,
                Ao = [.2f, .7f, 1f],
                Flags = [1, 2, 8],
                Reserved = [3, 4, 5],
                Uv2 = useUv2 ? [65535, 17, 65536, 33, 16777216, -1] : null
            };
            var placed = new PlacedPart(new("fixture.tint"), new object(), N.Matrix4x4.Identity, new PaletteToken("fixture.tint"));
            var noTint = BakeSurfacePacker.ApplyTint(original, placed with { Tint = null }, palette);
            var tinted = BakeSurfacePacker.ApplyTint(original, placed, palette);
            var before = BakeSurfacePacker.Pack(noTint, Bounds(), palette);
            var after = BakeSurfacePacker.Pack(tinted, Bounds(), palette);
            if (!originalIds.SequenceEqual(new ushort[] { 64, 68, 64 }) || !ReferenceEquals(noTint, original) ||
                tinted.Material.Spec.Lit.Value != "fixture.tint" || tinted.Material.Spec.Shade.Value != "fixture.tint.shade")
                throw new InvalidOperationException("Tint mutated source data or did not select the registered Lit/Shade pair.");
            if ((tinted.Material.Spec with { Lit = original.Material.Spec.Lit, Shade = original.Material.Spec.Shade }) != original.Material.Spec)
                throw new InvalidOperationException("Tint changed non-color material/shadow parameters.");
            var stride = after.AttributeData.Length / 3;
            for (var i = 0; i < 3; i++)
            {
                if (before.AttributeData.Span[i * stride] != originalIds[i] || after.AttributeData.Span[i * stride] != 66)
                    throw new InvalidOperationException("PlacedPart.Tint must override every explicit ColorIndices entry like the immediate render path.");
                if (!before.AttributeData.Span.Slice(i * stride + 1, stride - 1).SequenceEqual(after.AttributeData.Span.Slice(i * stride + 1, stride - 1)))
                    throw new InvalidOperationException("Tint changed AO, flags, reserved bytes or UV data.");
            }
            if (!before.VertexData.Span.SequenceEqual(after.VertexData.Span))
                throw new InvalidOperationException("Tint changed geometry/normal bytes.");
            var key = BakeRequestKeys.Tile(new("fixture"), "fixture.tint", new(0, 0), LodLevel.L0);
            var bundle = new TileBundle(key, new(0, 0), LodLevel.L0, Bounds(), [after]);
            var restored = ((TileBundle)TileBundleCodec.Decode("fixture/tint", key, TileBundleCodec.Encode("fixture/tint", bundle))).Surfaces.Single();
            if (!restored.AttributeData.Span.SequenceEqual(after.AttributeData.Span) || restored.Material != after.Material)
                throw new InvalidOperationException("Tint semantics changed during cache round trip.");
        }
    }

    [TestCase]
    public void Uv2AndSurfaceMetadataSurviveExactByteRoundTrip()
    {
        // 2^24 probes fp32 storage precision; it is not a valid campus-window allocation bound.
        float[] uv2 = [65535, 961, 65536, 1025, 16777216, -1];
        var input = Geometry() with { Uv2 = uv2, Shader = ShaderId.Water, Reflect = true };
        var packed = BakeSurfacePacker.Pack(input, Bounds(), Palette());
        if ((packed.Format & (ulong)RenderingServer.ArrayFormat.FlagCompressAttributes) != 0 ||
            packed.VertexData.Length + packed.AttributeData.Length != 3 * 40)
            throw new InvalidOperationException("UV2 surface did not use the 40-byte fp32 ABI.");
        CheckUvBits(packed, uv2);
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.surface", new(0, 0), LodLevel.L0);
        var bundle = new TileBundle(key, new(0, 0), LodLevel.L0, Bounds(), [packed]);
        var bytes = TileBundleCodec.Encode("fixture/surface", bundle);
        var restored = ((TileBundle)TileBundleCodec.Decode("fixture/surface", key, bytes)).Surfaces.Single();
        CheckUvBits(restored, uv2);
        if (restored.Shader != ShaderId.Water || !restored.Reflect)
            throw new InvalidOperationException("Shader/reflection metadata was lost.");
        if (!bytes.SequenceEqual(TileBundleCodec.Encode("fixture/surface", new TileBundle(key, new(0, 0), LodLevel.L0, Bounds(), [restored]))))
            throw new InvalidOperationException("Surface cache bytes are not stable.");
    }

    [TestCase]
    [RequireGodotRuntime]
    [TestCategory("NativeRender")]
    public void NativeSurfaceDecoderPreservesUv2AndNormalDirections()
    {
        foreach (var useUv2 in new[] { false, true })
        {
            var input = Geometry() with { Uv2 = useUv2 ? [65535, 17, 65536, 33, 16777216, -1] : null };
            var packed = BakeSurfacePacker.Pack(input, Bounds(), Palette());
            using var surface = Dictionary(packed);
            var surfaces = new Godot.Collections.Array<Godot.Collections.Dictionary> { surface };
            using var ownedSurfaces = (Godot.Collections.Array)surfaces;
            var rid = RenderingServer.MeshCreateFromSurfaces(surfaces);
            try
            {
                using var arrays = RenderingServer.MeshSurfaceGetArrays(rid, 0);
                var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                for (var i = 0; i < 3; i++)
                {
                    var expected = new Vector3(input.Normal[i * 3], input.Normal[i * 3 + 1], input.Normal[i * 3 + 2]);
                    if ((normals[i] - expected).Length() > .001f) throw new InvalidOperationException("Native normal direction differs from source.");
                }
                if (!useUv2) continue;
                var decoded = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
                for (var i = 0; i < 3; i++)
                    if (BitConverter.SingleToInt32Bits(decoded[i].X) != BitConverter.SingleToInt32Bits(input.Uv2![i * 2]) ||
                        BitConverter.SingleToInt32Bits(decoded[i].Y) != BitConverter.SingleToInt32Bits(input.Uv2[i * 2 + 1]))
                        throw new InvalidOperationException("Native renderer changed fp32 UV2 bits.");
            }
            finally { RenderingServer.FreeRid(rid); }
        }
    }

    [TestCase]
    public void CompressedBaseAndCustomStridesAreExplicitAndDeterministic()
    {
        var input = Geometry();
        var packed = BakeSurfacePacker.Pack(input, Bounds(), Palette());
        if (packed.ByteCount - packed.IndexData.Length != 3 * 20 ||
            (packed.Format & (ulong)RenderingServer.ArrayFormat.FlagFormatVersion2) == 0)
            throw new InvalidOperationException("VF-A is not the pinned 20-byte format.");
        input = input with { Custom0 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12] };
        var first = BakeSurfacePacker.Pack(input, Bounds(), Palette());
        var second = BakeSurfacePacker.Pack(input, Bounds(), Palette());
        if (first.AttributeData.Length != 3 * 24 || !first.VertexData.Span.SequenceEqual(second.VertexData.Span) ||
            !first.AttributeData.Span.SequenceEqual(second.AttributeData.Span))
            throw new InvalidOperationException("Custom fp32 channel changed stride or output bytes.");
    }

    private static void CheckUvBits(BakedSurface surface, float[] expected)
    {
        for (var i = 0; i < 3; i++)
            for (var component = 0; component < 2; component++)
                if (BinaryPrimitives.ReadInt32LittleEndian(surface.AttributeData.Span[(i * 20 + 12 + component * 4)..]) !=
                    BitConverter.SingleToInt32Bits(expected[i * 2 + component]))
                    throw new InvalidOperationException("UV2 packed bytes changed an integer/sentinel.");
    }
    private static GeometryBundle Geometry() => new(new(new(new("fixture.lit"), new("fixture.shade"))),
        [0, 0, 0, 64, 0, 0, 0, 0, 64], [0, 1, 0, 1, 0, 0, 0, 0, -1], [0, 2, 1], Uv: [0, 0, 1, 0, 0, 1]);
    private static Box3 Bounds() => new(N.Vector3.Zero, new(64, 64, 64));
    private static PaletteCatalog Palette() => new(new PaletteRegistry([
        new(new("fixture.lit"), 64, N.Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("fixture.shade"), 65, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade)]), []);
    private static Godot.Collections.Dictionary Dictionary(BakedSurface surface) => new()
    {
        ["format"] = surface.Format,
        ["primitive"] = (int)RenderingServer.PrimitiveType.Triangles,
        ["vertex_count"] = surface.VertexCount,
        ["index_count"] = surface.IndexCount,
        ["vertex_data"] = surface.VertexData.ToArray(),
        ["attribute_data"] = surface.AttributeData.ToArray(),
        ["index_data"] = surface.IndexData.ToArray(),
        ["aabb"] = new Aabb(new(surface.Bounds.Min.X, surface.Bounds.Min.Y, surface.Bounds.Min.Z),
            new(surface.Bounds.Size.X, surface.Bounds.Size.Y, surface.Bounds.Size.Z)),
        ["uv_scale"] = new Vector4(surface.UvScale.X, surface.UvScale.Y, surface.UvScale.Z, surface.UvScale.W)
    };
}
