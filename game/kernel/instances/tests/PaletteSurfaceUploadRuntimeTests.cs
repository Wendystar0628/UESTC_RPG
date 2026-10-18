using Campus.Tests;
using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Instances.Tests;

[TestSuite]
public sealed class PaletteSurfaceUploadRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void SurfacePatternUploadPreservesMetresAndRejectsProfileChangingOverrides()
    {
        var spec = Spec(64) with { SurfacePattern = new(7, new(2, 1), new(.01f, .01f)) };
        var source = Geometry() with { Material = new(spec), Uv = [-128.125f, .25f, -127.125f, .25f, -128.125f, 1.25f], Uv2 = [1, 3, 1, 3, 1, 3] };
        using var mesh = new ArrayMesh();
        GeometrySurfaceUpload.Append(mesh, source, Catalog(), materialSpec: spec);
        GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(source), Catalog());
        for (var slot = 0; slot < 2; slot++)
        {
            using var arrays = mesh.SurfaceGetArrays(slot);
            var uv = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            var gate = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            for (var vertex = 0; vertex < 3; vertex++)
                AssertThat(uv[vertex] == new Vector2(source.Uv![vertex * 2], source.Uv[vertex * 2 + 1]) &&
                    gate[vertex] == new Vector2(1, 3)).OverrideFailureMessage("Native upload lost metre UV or discrete UV2.").IsTrue();
        }
        {
            var thrown = false;
            try { GeometrySurfaceUpload.Append(mesh, source, Catalog(), materialSpec: Spec(64)); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { GeometrySurfaceUpload.Append(mesh, source, Catalog(),
            materialSpec: spec with { SurfacePattern = spec.SurfacePattern.Value with { Layer = 8 } }); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { GeometrySurfaceUpload.Append(mesh, source, Catalog(),
            materialSpec: spec with { Kind = ToonKind.Glass }); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { GeometrySurfaceUpload.Append(mesh, source, Catalog(), compressAttributes: true); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        AssertThat(mesh.GetSurfaceCount() == 2).OverrideFailureMessage("Rejected overrides published partial native surfaces.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void ActualNativeUploadRetainsAllLegacyColorBytesAndIndependentDecalBanks()
    {
        var catalog = Catalog();
        var legacy = Geometry(256) with
        {
            ColorIndices = Enumerable.Range(0, 256).Select(i => (ushort)i).ToArray(),
            Ao = Enumerable.Range(0, 256).Select(i => i / 255f).ToArray(),
            Flags = Enumerable.Range(0, 256).Select(i => (byte)(i & 15)).ToArray(),
            Reserved = Enumerable.Range(0, 256).Select(i => (byte)(255 - i)).ToArray()
        };
        using (var oldMesh = new ArrayMesh())
        using (var catalogMesh = new ArrayMesh())
        {
            GeometrySurfaceUpload.Append(oldMesh, GeometryBuffer.Freeze(legacy), catalog);
            GeometrySurfaceUpload.Append(catalogMesh, legacy, catalog: catalog);
            using var oldArrays = oldMesh.SurfaceGetArrays(0);
            using var nextArrays = catalogMesh.SurfaceGetArrays(0);
            var oldColors = oldArrays[(int)Mesh.ArrayType.Color].AsColorArray();
            var nextColors = nextArrays[(int)Mesh.ArrayType.Color].AsColorArray();
            for (var i = 0; i < 256; i++)
            {
                AssertThat(oldColors[i] == nextColors[i]).OverrideFailureMessage("Catalog selection changed legacy native COLOR values.").IsTrue();
                AssertThat(Byte(nextColors[i].R) == i && Byte(nextColors[i].G) == (byte)(legacy.Ao![i] * 255f) &&
                    Byte(nextColors[i].B) == (i & 15) && Byte(nextColors[i].A) == 255 - i).OverrideFailureMessage("A real native COLOR byte differs from the frozen v1 meaning.").IsTrue();
            }
        }
        foreach (var foreground in new ushort[] { 255, 256, 511 })
            foreach (var background in new ushort[] { 255, 511 })
            {
                var source = Geometry() with
                {
                    ColorIndices = [foreground, foreground, foreground],
                    DecalBaseColorIndices = [background, background, background],
                    Flags = [4, 7, 15],
                    Ao = [0f, .5f, 1f],
                    Uv2 = [65535, 17, 65536, 33, 16777216, -1],
                    Custom0 = [1, 2, 3, 4, -5, -6, -7, -8, 9, 10, 11, 12],
                    Custom1 = [13, 14, 15, 16, 17, 18, 19, 20, -21, -22, -23, -24],
                    Custom2 = [25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36]
                };
                using var mesh = new ArrayMesh();
                GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(source), catalog: catalog);
                using var arrays = mesh.SurfaceGetArrays(0);
                var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
                var uv2 = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
                for (var i = 0; i < 3; i++)
                {
                    var flags = (byte)Byte(colors[i].B);
                    AssertThat(PaletteEncoding.DecodeColor((byte)Byte(colors[i].R), flags).Value == foreground &&
                        PaletteEncoding.DecodeDecalBase((byte)Byte(colors[i].A), flags).Value == background &&
                        (flags & 15) == source.Flags![i] && Byte(colors[i].G) == (byte)(source.Ao![i] * 255f)).OverrideFailureMessage("Native bank decode lost a palette/semantic/AO bit.").IsTrue();
                    AssertThat(BitConverter.SingleToInt32Bits(uv2[i].X) == BitConverter.SingleToInt32Bits(source.Uv2![i * 2]) &&
                        BitConverter.SingleToInt32Bits(uv2[i].Y) == BitConverter.SingleToInt32Bits(source.Uv2![i * 2 + 1])).OverrideFailureMessage("Native UV2 bits changed.").IsTrue();
                }
                AssertThat(arrays[(int)Mesh.ArrayType.Custom0].AsFloat32Array().SequenceEqual(source.Custom0!)).OverrideFailureMessage("Native CUSTOM0 changed.").IsTrue();
                AssertThat(arrays[(int)Mesh.ArrayType.Custom1].AsFloat32Array().SequenceEqual(source.Custom1!)).OverrideFailureMessage("Native CUSTOM1 changed.").IsTrue();
                AssertThat(arrays[(int)Mesh.ArrayType.Custom2].AsFloat32Array().SequenceEqual(source.Custom2!)).OverrideFailureMessage("Native CUSTOM2 changed.").IsTrue();
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void HighFallbackAndTintUseTheSharedCatalog()
    {
        var catalog = Catalog();
        using var mesh = new ArrayMesh();
        var high = Geometry() with { Material = new(Spec(510)) };
        GeometrySurfaceUpload.Append(mesh, high, catalog: catalog);
        GeometrySurfaceUpload.Append(mesh, Geometry() with { ColorIndices = [64, 66, 68] }, tint: Token(510), catalog: catalog);
        for (var surface = 0; surface < 2; surface++)
        {
            using var arrays = mesh.SurfaceGetArrays(surface);
            AssertThat(arrays[(int)Mesh.ArrayType.Color].AsColorArray().All(color => Byte(color.R) == 254 && Byte(color.B) == 16)).OverrideFailureMessage("High token fallback/tint called the byte provider or lost bank 1.").IsTrue();
        }
        GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(high), catalog);
        {
            var thrown = false;
            try { GeometrySurfaceUpload.Append(mesh,
            Geometry() with { ColorIndices = [512, 256, 256] }, catalog); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        AssertThat(mesh.GetSurfaceCount() == 3).OverrideFailureMessage("Rejected streams published partial native surfaces.").IsTrue();
    }

    private static int Byte(float value) => (int)MathF.Round(value * 255f);
    private static PaletteToken Token(int index) => new("fixture.p" + index);
    private static ToonSpec Spec(int index) => new(Token(index), Token(index + 1));
    private static PaletteCatalog Catalog()
    {
        var legacy = new PaletteRegistry(Enumerable.Range(0, 256).Select(i => new PaletteEntry(Token(i), (byte)i,
            new N.Vector3(.5f), "fixture", false, i < 32 ? PaletteRole.Frame : i < 40 ? PaletteRole.Retired :
                i < 64 ? PaletteRole.Weather : (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade)));
        return new(legacy, Enumerable.Range(256, 256).Select(i => new PaletteEntryV2(Token(i), new(i), new(.5f),
            "fixture", false, (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade)));
    }
    private static GeometryBundle Geometry(int count = 3) => new(new(Spec(64)),
        Enumerable.Range(0, count).SelectMany(i => new[] { i % 3 == 1 ? 1f : 0f, 0f, i % 3 == 2 ? 1f : 0f }).ToArray(),
        Enumerable.Range(0, count).SelectMany(_ => new[] { 0f, 1f, 0f }).ToArray(), [0, 1, 2], Uv: new float[count * 2]);
}
