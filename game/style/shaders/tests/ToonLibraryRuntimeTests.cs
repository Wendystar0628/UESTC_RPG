using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stream;
using Campus.Style.Materials;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ToonLibraryRuntimeTests
{

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SurfacePatternMaterialKeyRetainsEveryProfileField()
    {
        var spec = new ToonSpec(new("test.lit"), new("test.shade"))
        {
            SurfacePattern = new(9, new(2, 1), new(.01f, .02f))
        };
        var keyType = typeof(ToonLibrary).GetNestedType("MaterialClass", BindingFlags.NonPublic)!;
        var method = keyType.GetMethod("Of", BindingFlags.Public | BindingFlags.Static)!;
        object Key(ToonSpec value) => method.Invoke(null, [value])!;
        var key = Key(spec);
        AssertThat(key.Equals(Key(spec with { Lit = new("other.lit"), Shade = new("other.shade") }))).OverrideFailureMessage("Colour-only changes must still share a material class.").IsTrue();
        foreach (var pattern in new[] { spec.SurfacePattern.Value with { Layer = 10 },
            spec.SurfacePattern.Value with { PeriodMeters = new(3, 1) },
            spec.SurfacePattern.Value with { JointMeters = new(.01f, .03f) } })
            AssertThat(!key.Equals(Key(spec with { SurfacePattern = pattern }))).OverrideFailureMessage("Material cache identity dropped a surface-profile field.").IsTrue();
        AssertThat(!key.Equals(Key(spec with { SurfacePattern = null }))).OverrideFailureMessage("Pattern and legacy classes collapsed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task UnregisteredSurfacePatternsCannotReuseAnAlreadyCachedPlainMaterial()
    {
        await using var fixture = new Fixture();
        var plain = fixture.Library.Get(fixture.Spec);
        var count = ReadPrivate<System.Collections.IDictionary>(fixture.Library, "_byClass").Count;
        var refused = false;
        try { fixture.Library.Get(fixture.Spec with { SurfacePattern = new(7, new(2, 1), new(.01f, .01f)) }); }
        catch (NotSupportedException) { refused = true; }
        AssertThat(refused && ReferenceEquals(fixture.Library.Get(fixture.Spec), plain) &&
            ReadPrivate<System.Collections.IDictionary>(fixture.Library, "_byClass").Count == count).OverrideFailureMessage("Unregistered surface layers must fail before cache lookup/allocation, leaving plain materials intact.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task BakedSurfacePatternUploadsMetresGatesAndBindsItsToonStaticMaterial()
    {
        await using var fixture = new Fixture();
        var plain = fixture.Library.Get(fixture.Spec);
        var spec = fixture.Spec with
        {
            SurfacePattern = new(SurfacePatternLayers.StackBond, new(.6f, .6f), new(.006f, .006f))
        };
        var patterned = (ShaderMaterial)fixture.Library.Get(spec);
        AssertThat(!ReferenceEquals(plain, patterned) && ReferenceEquals(patterned, fixture.Library.Get(spec)) &&
            fixture.Library.MaterialId(spec) != fixture.Library.MaterialId(fixture.Spec)).OverrideFailureMessage("A registered profile is its own cached material class with its own material ID.").IsTrue();
        AssertThat(patterned.Shader.ResourcePath == "res://style/shaders/toon_static.gdshader").OverrideFailureMessage("Patterns are drawn by toon_static only.").IsTrue();
        AssertThat((int)patterned.GetShaderParameter("pattern_layer") == SurfacePatternLayers.StackBond &&
            ((Vector2)patterned.GetShaderParameter("pattern_period")).IsEqualApprox(new(.6f, .6f)) &&
            ((Vector2)patterned.GetShaderParameter("pattern_joint")).IsEqualApprox(new(.006f, .006f))).OverrideFailureMessage("Layer, period and joint reach the material uniforms.").IsTrue();
        var source = new GeometryBundle(new(spec),
            [0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0, 1],
            [0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2, 3, 4, 5],
            Uv: [-128.125f, 64.25f, -127.125f, 64.25f, -128.125f, 65.25f, 20, 8, 21, 8, 20, 9],
            Uv2: [1, 2, 1, 2, 1, 2, 0, 4, 0, 4, 0, 4])
        { ColorIndices = [64, 64, 64, 64, 64, 64] };
        var bounds = new Box3(System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.pattern", new(0, 0), LodLevel.L0);
        var bundle = new TileBundle(key, new(0, 0), LodLevel.L0, bounds,
            [BakeSurfacePacker.Pack(source, bounds, new Palettes().Catalog)]);
        var restored = (TileBundle)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/pattern", key, TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/pattern", bundle));
        var scenario = RenderingServer.ScenarioCreate();
        try
        {
            using var prepared = PreparedTile.Create(restored);
            using var materials = new StreamMaterials(fixture.Library, new ShaderMaterialSource());
            var draw = prepared.Draws.Single();
            var material = materials.Resolve(draw);
            prepared.MountOne(0, scenario, material.GetRid());
            AssertThat(ReferenceEquals(material, patterned) && draw.Base == draw.Mesh &&
                RenderingServer.MeshSurfaceGetMaterial(draw.Mesh, 0) == patterned.GetRid()).OverrideFailureMessage("The formal packed-tile upload must bind the complete profile through ToonLibrary.").IsTrue();
            using var arrays = RenderingServer.MeshSurfaceGetArrays(draw.Mesh, 0);
            var uv = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            var gate = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
            for (var vertex = 0; vertex < 6; vertex++)
                AssertThat(uv[vertex] == new Vector2(source.Uv![vertex * 2], source.Uv[vertex * 2 + 1]) &&
                    gate[vertex] == new Vector2(source.Uv2![vertex * 2], source.Uv2[vertex * 2 + 1]) &&
                    colors[vertex].R8 == 64).OverrideFailureMessage("Native static upload changed metre coordinates, per-face gate/K or logical palette colour.").IsTrue();
        }
        finally { RenderingServer.FreeRid(scenario); }
        var refused = false;
        try { fixture.Library.Get(spec with { Kind = ToonKind.Glass }); }
        catch (ArgumentException) { refused = true; }
        AssertThat(refused).OverrideFailureMessage("Only the ordinary Static kind can carry a surface pattern.").IsTrue();
    }

    [TestCase(Timeout = 20000)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task RealGlassDrawsCardMaskAndUnboundOrUndersizedTableFallback()
    {
        await using var fixture = new Fixture();
        var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot runtime required.");
        var parent = new Node3D();
        tree.Root.AddChild(parent);
        using var mesh = new ArrayMesh();
        using var tinyImage = Image.CreateFromData(1, 1, false, Image.Format.Rgbaf, new byte[16]);
        using var tiny = ImageTexture.CreateFromImage(tinyImage);
        try
        {
            parent.AddChild(new Camera3D { Current = true, Near = .05f, Far = 100f });
            // Four real VF-B surfaces: office, physical curtain flag, invalid row, and existing cols=0 glass.
            for (var i = 0; i < 4; i++)
            {
                var left = -1.5f + .8f * i;
                var spare = i == 1 ? (byte)(5 | WindowCardEncoding.PhysicalCurtainFlag) : i == 2 ? (byte)31 : (byte)5;
                var spec = fixture.Spec with { Kind = ToonKind.Glass };
                var source = new GeometryBundle(new(spec),
                    [left, -.5f, -3f, left + .7f, -.5f, -3f, left + .7f, .5f, -3f, left, .5f, -3f],
                    [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2],
                    Uv: [.01f, .01f, .99f, .01f, .99f, .99f, .01f, .99f],
                    Uv2: [0, i == 3 ? 0 : 32, 0, i == 3 ? 0 : 32, 0, i == 3 ? 0 : 32, 0, i == 3 ? 0 : 32])
                { ColorIndices = [64, 64, 64, 64], Reserved = [spare, spare, spare, spare] };
                GeometrySurfaceUpload.Append(mesh, source, new Palettes().Catalog);
            }
            parent.AddChild(new MeshInstance3D { Mesh = mesh });
            foreach (var state in new[] { "registered", "unbound", "undersized" })
            {
                if (state == "unbound") RenderingServer.GlobalShaderParameterSet(new StringName(ShaderGlobals.WindowCards), default(Variant));
                if (state == "undersized") RenderingServer.GlobalShaderParameterSet(new StringName(ShaderGlobals.WindowCards), tiny);
                for (var frame = 0; frame < 8; frame++)
                    await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
            GD.Print("WINDOW_CARDS_NATIVE exercised=real_toon_glass,type_mask,invalid_row,unbound,undersized_1x1,cols0 pixel_assertions=False");
        }
        finally
        {
            RenderingServer.GlobalShaderParameterSet(new StringName(ShaderGlobals.WindowCards), default(Variant));
            parent.Free();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ExtensionSpecsValidateAgainstTheCatalogButNeverThroughTheLegacyByte()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var extension = new ToonSpec(new PaletteToken("ext.lit"), new PaletteToken("ext.shade")) { Fx = ToonFx.BevelLift };
        AssertThat(library.Get(extension) is ShaderMaterial).OverrideFailureMessage("A bank-1 Lit/Shade pair must pass the full-catalog ToonSpec check.").IsTrue();
        var mismatched = false;
        try { library.Get(new ToonSpec(new PaletteToken("test.lit"), new PaletteToken("ext.shade")) { Fx = ToonFx.Rim }); }
        catch (ArgumentException) { mismatched = true; }
        AssertThat(mismatched).OverrideFailureMessage("A Lit/Shade pair across banks must be rejected.").IsTrue();
    }





    [TestCase(Timeout = 10000)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task DisposeReleasesOwnedMaterialsAndImageButPreservesBorrowedResourcesAndTextureLeases()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var owned = library.Get(fixture.Spec);
        var borrowed = fixture.Borrow("canvas_item");
        var shader = borrowed.Shader;
        var image = ReadPrivate<Image>(library, "_matImage");
        var texture = ReadPrivate<ImageTexture>(library, "_matTexture");
        AssertThat(fixture.Textures.TryAcquire(ShaderGlobals.MatTable, out var lease)).OverrideFailureMessage("The registered mat_table must be leaseable.").IsTrue();
        using var retained = lease ?? throw new InvalidOperationException("A successful acquire must return a lease.");
        AssertThat(fixture.Textures.TryAcquire(ShaderGlobals.WindowCards, out var cardLease)).OverrideFailureMessage("The static window_cards table must be registered once by the library.").IsTrue();
        using var retainedCards = cardLease ?? throw new InvalidOperationException("A successful acquire must return a card lease.");
        var cardRid = retainedCards.TextureRid;

        library.Dispose();
        library.Dispose();
        AssertThat(!GodotObject.IsInstanceValid(owned)).OverrideFailureMessage("Library-created Toon material must be released deterministically.").IsTrue();
        AssertThat(!GodotObject.IsInstanceValid(image)).OverrideFailureMessage("The producer-owned Image must be released deterministically.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(borrowed) && GodotObject.IsInstanceValid(shader)).OverrideFailureMessage("Borrowed material and its shader must stay owned by the caller.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(texture)).OverrideFailureMessage("A live reader lease must protect the registry-owned texture from library disposal.").IsTrue();
        AssertThat(!fixture.Textures.TryAcquire(ShaderGlobals.MatTable, out _)).OverrideFailureMessage("Dispose must unregister mat_table before returning.").IsTrue();
        AssertThat(!fixture.Textures.TryAcquire(ShaderGlobals.WindowCards, out _)).OverrideFailureMessage("Dispose must unregister window_cards before returning.").IsTrue();
        AssertThat(retainedCards.TextureRid == cardRid).OverrideFailureMessage("An existing window-card lease remains valid through producer disposal.").IsTrue();
        retained.Dispose();
        retainedCards.Dispose();
        await fixture.Textures.ShutdownAsync();
        AssertThat(!GodotObject.IsInstanceValid(texture)).OverrideFailureMessage("The registry must release the texture after the final lease ends.").IsTrue();
    }

    [TestCase(Timeout = 10000)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ConstructorFailureRetiresRegisteredTexturesAndDisposesUntransferredTexture()
    {
        using var registry = new GlobalDataTextureRegistry();
        var rejecting = new RejectCards(registry);
        var rejected = false;
        try { using var unexpected = new ToonLibrary(new Palettes(), new ShaderMaterialSource(), rejecting); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected).OverrideFailureMessage("Exercise failure after mat_table registration and card texture creation.").IsTrue();
        AssertThat(!registry.TryAcquire(ShaderGlobals.MatTable, out _)).OverrideFailureMessage("A partially constructed library must unregister mat_table.").IsTrue();
        AssertThat(rejecting.Cards is not null && !GodotObject.IsInstanceValid(rejecting.Cards)).OverrideFailureMessage("Rejected texture ownership stays local and must be disposed.").IsTrue();
        await registry.ShutdownAsync();
        AssertThat(rejecting.Mat is not null && !GodotObject.IsInstanceValid(rejecting.Mat)).OverrideFailureMessage("Previously transferred texture must be released by the registry.").IsTrue();
    }

    private sealed class RejectCards(GlobalDataTextureRegistry inner) : IGlobalDataTextureRegistry
    {
        public Texture2D? Mat { get; private set; }
        public Texture2D? Cards { get; private set; }
        public IDisposable RegisterOwned(string name, Texture2D texture, Rid rid)
        {
            if (name == ShaderGlobals.WindowCards)
            {
                Cards = texture;
                throw new InvalidOperationException("reject window_cards");
            }
            Mat = texture;
            return inner.RegisterOwned(name, texture, rid);
        }
        public bool TryAcquire(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GlobalDataTextureLease? lease) => inner.TryAcquire(name, out lease);
        public Task ShutdownAsync() => inner.ShutdownAsync();
        public void Dispose() => inner.Dispose();
    }

    private static T ReadPrivate<T>(ToonLibrary library, string name) =>
        (T)(typeof(ToonLibrary).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(library)
            ?? throw new InvalidOperationException($"Missing lifecycle test field: {name}"));

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly List<ShaderMaterial> _borrowed = [];
        private readonly List<Shader> _shaders = [];
        public GlobalDataTextureRegistry Textures { get; } = new();
        public ToonSpec Spec { get; } = new(new PaletteToken("test.lit"), new PaletteToken("test.shade"));
        public ToonLibrary Library { get; }

        public Fixture()
        {
            AssertThat(OS.GetThreadCallerId() == OS.GetMainThreadId()).OverrideFailureMessage("Native material lifetime tests must run on Godot's main thread.").IsTrue();
            Library = new ToonLibrary(new Palettes(), new ShaderMaterialSource(), Textures);
        }

        public ShaderMaterial Borrow(string mode)
        {
            var shader = new Shader { Code = $"shader_type {mode};" };
            var material = new ShaderMaterial { Shader = shader };
            _shaders.Add(shader);
            _borrowed.Add(material);
            return material;
        }

        public async ValueTask DisposeAsync()
        {
            Library.Dispose();
            foreach (var material in _borrowed)
                if (GodotObject.IsInstanceValid(material)) material.Dispose();
            foreach (var shader in _shaders)
                if (GodotObject.IsInstanceValid(shader)) shader.Dispose();
            await Textures.ShutdownAsync();
        }
    }

    private sealed class Palettes : IPaletteCatalogSource
    {
        public PaletteCatalog Catalog { get; } = new(new PaletteRegistry([
            new(new PaletteToken("test.lit"), new(64), System.Numerics.Vector3.One, "test", false, PaletteRole.Lit),
            new(new PaletteToken("test.shade"), new(65), System.Numerics.Vector3.Zero, "test", false, PaletteRole.Shade)
        ]), [
            new(new PaletteToken("ext.lit"), new PaletteIndex(256), System.Numerics.Vector3.One, "test", false, PaletteRole.Lit),
            new(new PaletteToken("ext.shade"), new PaletteIndex(257), System.Numerics.Vector3.Zero, "test", false, PaletteRole.Shade)
        ]);
    }

}
