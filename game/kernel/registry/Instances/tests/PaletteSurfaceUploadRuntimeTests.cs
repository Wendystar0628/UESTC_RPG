using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Instances.Tests;

[TestSuite]
public sealed class PaletteSurfaceUploadRuntimeTests
{
    [TestCase]
    public void FallbackRejectsPatternsBeforeTokenAndVertexColorShortcuts()
    {
        var fallback = new FallbackToonMaterialProvider(new CatalogSource(Catalog()));
        var profile = Spec(64) with { SurfacePattern = new(7, new(2, 1), new(.01f, .01f)) };
        Throws<NotSupportedException>(() => fallback.Get(profile));
        Throws<NotSupportedException>(() => fallback.VertexColor(profile, new PaletteIndex(64)));
        Throws<NotSupportedException>(() => fallback.Get(profile with { Lit = new("d0.ground"), Shade = new("d0.ground.shade") }));
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void SurfacePatternUploadPreservesMetresAndRejectsProfileChangingOverrides()
    {
        var provider = new Provider();
        var spec = Spec(64) with { SurfacePattern = new(7, new(2, 1), new(.01f, .01f)) };
        var source = Geometry() with { Material = new(spec), Uv = [-128.125f, .25f, -127.125f, .25f, -128.125f, 1.25f], Uv2 = [1, 3, 1, 3, 1, 3] };
        using var mesh = new ArrayMesh();
        GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(), materialSpec: spec);
        GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(source), provider, Catalog());
        for (var slot = 0; slot < 2; slot++)
        {
            using var arrays = mesh.SurfaceGetArrays(slot);
            var uv = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            var gate = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            for (var vertex = 0; vertex < 3; vertex++)
                Check(uv[vertex] == new Vector2(source.Uv![vertex * 2], source.Uv[vertex * 2 + 1]) &&
                    gate[vertex] == new Vector2(1, 3), "Native upload lost metre UV or discrete UV2.");
        }
        Throws<ArgumentException>(() => GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(), materialSpec: Spec(64)));
        Throws<ArgumentException>(() => GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(),
            materialSpec: spec with { SurfacePattern = spec.SurfacePattern.Value with { Layer = 8 } }));
        Throws<ArgumentException>(() => GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(),
            materialSpec: spec with { Kind = ToonKind.Glass }));
        Throws<ArgumentException>(() => GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(), compressAttributes: true));
        Throws<NotSupportedException>(() => GeometrySurfaceUpload.Append(mesh, source, provider, Catalog(), useD0Rgb: true));
        Check(mesh.GetSurfaceCount() == 2, "Rejected overrides published partial native surfaces.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void ActualNativeUploadRetainsAllLegacyColorBytesAndIndependentDecalBanks()
    {
        var catalog = Catalog();
        var provider = new Provider();
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
            GeometrySurfaceUpload.Append(oldMesh, GeometryBuffer.Freeze(legacy), provider, catalog);
            GeometrySurfaceUpload.Append(catalogMesh, legacy, provider, catalog: catalog);
            using var oldArrays = oldMesh.SurfaceGetArrays(0);
            using var nextArrays = catalogMesh.SurfaceGetArrays(0);
            var oldColors = oldArrays[(int)Mesh.ArrayType.Color].AsColorArray();
            var nextColors = nextArrays[(int)Mesh.ArrayType.Color].AsColorArray();
            for (var i = 0; i < 256; i++)
            {
                Check(oldColors[i] == nextColors[i], "Catalog selection changed legacy native COLOR values.");
                Check(Byte(nextColors[i].R) == i && Byte(nextColors[i].G) == (byte)(legacy.Ao![i] * 255f) &&
                    Byte(nextColors[i].B) == (i & 15) && Byte(nextColors[i].A) == 255 - i,
                    "A real native COLOR byte differs from the frozen v1 meaning.");
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
                GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(source), provider, catalog: catalog);
                using var arrays = mesh.SurfaceGetArrays(0);
                var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
                var uv2 = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
                for (var i = 0; i < 3; i++)
                {
                    var flags = (byte)Byte(colors[i].B);
                    Check(PaletteEncoding.DecodeColor((byte)Byte(colors[i].R), flags).Value == foreground &&
                        PaletteEncoding.DecodeDecalBase((byte)Byte(colors[i].A), flags).Value == background &&
                        (flags & 15) == source.Flags![i] && Byte(colors[i].G) == (byte)(source.Ao![i] * 255f), "Native bank decode lost a palette/semantic/AO bit.");
                    Check(BitConverter.SingleToInt32Bits(uv2[i].X) == BitConverter.SingleToInt32Bits(source.Uv2![i * 2]) &&
                        BitConverter.SingleToInt32Bits(uv2[i].Y) == BitConverter.SingleToInt32Bits(source.Uv2![i * 2 + 1]), "Native UV2 bits changed.");
                }
                Check(arrays[(int)Mesh.ArrayType.Custom0].AsFloat32Array().SequenceEqual(source.Custom0!), "Native CUSTOM0 changed.");
                Check(arrays[(int)Mesh.ArrayType.Custom1].AsFloat32Array().SequenceEqual(source.Custom1!), "Native CUSTOM1 changed.");
                Check(arrays[(int)Mesh.ArrayType.Custom2].AsFloat32Array().SequenceEqual(source.Custom2!), "Native CUSTOM2 changed.");
            }
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void HighFallbackAndTintUseTheSharedCatalog()
    {
        var catalog = Catalog();
        var provider = new Provider();
        using var mesh = new ArrayMesh();
        var high = Geometry() with { Material = new(Spec(510)) };
        GeometrySurfaceUpload.Append(mesh, high, provider, catalog: catalog);
        GeometrySurfaceUpload.Append(mesh, Geometry() with { ColorIndices = [64, 66, 68] }, provider, tint: Token(510), catalog: catalog);
        for (var surface = 0; surface < 2; surface++)
        {
            using var arrays = mesh.SurfaceGetArrays(surface);
            Check(arrays[(int)Mesh.ArrayType.Color].AsColorArray().All(color => Byte(color.R) == 254 && Byte(color.B) == 16),
                "High token fallback/tint called the byte provider or lost bank 1.");
        }
        GeometrySurfaceUpload.Append(mesh, GeometryBuffer.Freeze(high), provider, catalog);
        Throws<ArgumentException>(() => GeometrySurfaceUpload.Append(mesh,
            Geometry() with { ColorIndices = [512, 256, 256] }, provider, catalog));
        Throws<NotSupportedException>(() => GeometrySurfaceUpload.Append(mesh,
            Geometry() with { ColorIndices = [256, 256, 256], Flags = [4, 4, 4], DecalBaseColorIndices = [510, 510, 510] },
            new FallbackToonMaterialProvider(new CatalogSource(catalog)), catalog, useD0Rgb: true));
        Check(mesh.GetSurfaceCount() == 3, "Rejected streams published partial native surfaces.");
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
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private sealed class Provider : IToonMaterialProvider
    {
        public Material Get(ToonSpec spec) => throw new NotSupportedException();
        public bool IsReady(ToonSpec spec) => false;
        public void Track(ShaderId shader, Material material) => throw new NotSupportedException();
        public void TrackLayout(Material material, WarmDrawLayout layout) => throw new NotSupportedException();
        public bool IsReady(ShaderId shader, Material material) => false;
        public ToonWarmBatch CapturePendingWarm() => throw new NotSupportedException();
        public bool CompleteWarm(ToonWarmBatch batch) => throw new NotSupportedException();
    }

    private sealed class CatalogSource(PaletteCatalog catalog) : IPaletteCatalogSource
    {
        public PaletteCatalog Catalog { get; } = catalog;
    }
}
