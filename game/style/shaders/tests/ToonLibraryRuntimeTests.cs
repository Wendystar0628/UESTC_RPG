using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Style.Materials;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ToonLibraryRuntimeTests
{
    private static readonly WarmDrawLayout Layout = new(1, false);

    [TestCase]
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
        Require(key.Equals(Key(spec with { Lit = new("other.lit"), Shade = new("other.shade") })), "Colour-only changes must still share a material class.");
        foreach (var pattern in new[] { spec.SurfacePattern.Value with { Layer = 10 },
            spec.SurfacePattern.Value with { PeriodMeters = new(3, 1) },
            spec.SurfacePattern.Value with { JointMeters = new(.01f, .03f) } })
            Require(!key.Equals(Key(spec with { SurfacePattern = pattern })), "Material cache identity dropped a surface-profile field.");
        Require(!key.Equals(Key(spec with { SurfacePattern = null })), "Pattern and legacy classes collapsed.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task PendingSurfacePatternsCannotReuseAnAlreadyCachedPlainMaterial()
    {
        await using var fixture = new Fixture();
        var plain = fixture.Library.Get(fixture.Spec);
        var count = ReadPrivate<System.Collections.IDictionary>(fixture.Library, "_byClass").Count;
        var refused = false;
        try { fixture.Library.Get(fixture.Spec with { SurfacePattern = new(7, new(2, 1), new(.01f, .01f)) }); }
        catch (NotSupportedException) { refused = true; }
        Require(refused && ReferenceEquals(fixture.Library.Get(fixture.Spec), plain) &&
            ReadPrivate<System.Collections.IDictionary>(fixture.Library, "_byClass").Count == count,
            "Unconnected surface patterns must fail before cache lookup/allocation, leaving plain materials intact.");
    }

    [TestCase(Timeout = 20000)]
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
                { ColorIds = [64, 64, 64, 64], Reserved = [spare, spare, spare, spare] };
                GeometrySurfaceUpload.Append(mesh, source, fixture.Library);
            }
            parent.AddChild(new MeshInstance3D { Mesh = mesh });
            foreach (var state in new[] { "registered", "unbound", "undersized" })
            {
                if (state == "unbound") RenderingServer.GlobalShaderParameterSet(new StringName(ShaderGlobals.WindowCards), default(Variant));
                if (state == "undersized") RenderingServer.GlobalShaderParameterSet(new StringName(ShaderGlobals.WindowCards), tiny);
                for (var frame = 0; frame < 8; frame++)
                    await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (System.Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } root)
                {
                    var directory = Path.Combine(root, "output", "render-review");
                    Directory.CreateDirectory(directory);
                    using var image = parent.GetViewport().GetTexture().GetImage();
                    Require(image.SavePng(Path.Combine(directory, $"WP-00-window-cards-native-{state}.png")) == Error.Ok, "Save real glass smoke evidence.");
                }
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
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task DisposedBorrowedMaterialIsEvictedBeforeTheNextMapBatch()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var previousMap = fixture.Borrow("canvas_item");
        library.Track(ShaderId.UiPanel, previousMap);
        Require(library.CompleteWarm(library.CapturePendingWarm()), "The first map's canvas draw must be accepted.");
        Require(library.IsReady(ShaderId.UiPanel, previousMap), "The live canvas must be ready after its draw.");

        previousMap.Dispose();
        var empty = library.CapturePendingWarm();
        Require(empty.Materials.Count == 0, "An unloaded map's disposed material must not re-enter a batch.");
        Require(!library.IsReady(ShaderId.UiPanel, previousMap), "A disposed material must lose readiness.");
        RequireNoTrackedMaterials(library);

        var nextMap = fixture.Borrow("canvas_item");
        library.Track(ShaderId.UiPanel, nextMap);
        Require(!library.IsReady(ShaderId.UiPanel, nextMap), "The next map must not inherit the previous instance's evidence.");
        var pending = library.CapturePendingWarm();
        RequireOnly(pending, nextMap);
        Require(library.CompleteWarm(pending), "The replacement canvas must accept its own evidence.");
        Require(library.IsReady(ShaderId.UiPanel, nextMap), "The replacement must be ready only after completion.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ExtensionSpecsValidateAgainstTheCatalogButNeverThroughTheLegacyByte()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var extension = new ToonSpec(new PaletteToken("ext.lit"), new PaletteToken("ext.shade")) { Fx = ToonFx.BevelLift };
        Require(library.Get(extension) is ShaderMaterial, "A bank-1 Lit/Shade pair must pass the full-catalog ToonSpec check.");
        Require(library.PaletteId(new PaletteToken("test.lit")) == 64, "The legacy byte entry must keep the old colour ID.");
        var refused = false;
        try { library.PaletteId(new PaletteToken("ext.lit")); }
        catch (InvalidOperationException) { refused = true; }
        Require(refused, "An extension token must fail explicitly through the legacy byte PaletteId, never truncate.");
        var mismatched = false;
        try { library.Get(new ToonSpec(new PaletteToken("test.lit"), new PaletteToken("ext.shade")) { Fx = ToonFx.Rim }); }
        catch (ArgumentException) { mismatched = true; }
        Require(mismatched, "A Lit/Shade pair across banks must be rejected.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task QualityResetPrunesDisposedMaterialsAndRewarmsOnlyLiveOnes()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var unloaded = fixture.Borrow("spatial");
        var survivor = fixture.Borrow("spatial");
        library.Track(ShaderId.Water, unloaded);
        library.TrackLayout(unloaded, Layout);
        library.Track(ShaderId.Water, survivor);
        library.TrackLayout(survivor, Layout);
        Require(library.CompleteWarm(library.CapturePendingWarm()), "Both live layouts must warm initially.");

        unloaded.Dispose();
        fixture.Quality.ResetPipelines();
        Require(!library.IsReady(ShaderId.Water, unloaded), "Quality reset must not revive a disposed instance.");
        Require(!library.IsReady(ShaderId.Water, survivor), "Pipeline quality changes invalidate live evidence.");
        var pending = library.CapturePendingWarm();
        RequireOnly(pending, survivor);
        Require(library.CompleteWarm(pending), "The survivor's new quality evidence must be accepted.");
        Require(library.IsReady(ShaderId.Water, survivor), "Only the survivor becomes ready again.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task DisposalInvalidatesTheWholeInflightBatchWithoutPartiallyWarmingSurvivors()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var unloaded = fixture.Borrow("canvas_item");
        var survivor = fixture.Borrow("canvas_item");
        library.Track(ShaderId.UiPanel, unloaded);
        library.Track(ShaderId.UiPanel, survivor);
        var inflight = library.CapturePendingWarm();
        Require(inflight.Materials.Count == 2, "This batch must include both instances before disposal.");

        unloaded.Dispose();
        Require(!library.CompleteWarm(inflight), "A batch containing a disposed material must be rejected as a whole.");
        Require(!library.IsReady(ShaderId.UiPanel, survivor), "A rejected batch must not partially warm the survivor.");
        var replacement = library.CapturePendingWarm();
        Require(replacement.Revision > inflight.Revision, "Eviction must advance the revision.");
        RequireOnly(replacement, survivor);
        Require(!library.CompleteWarm(inflight), "Old evidence stays invalid after recapture.");
        Require(library.CompleteWarm(replacement), "Fresh survivor-only evidence must complete.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task BothReadinessOverloadsEvictDisposedInstances()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var borrowed = fixture.Borrow("canvas_item");
        library.Track(ShaderId.UiPanel, borrowed);
        Require(library.CompleteWarm(library.CapturePendingWarm()), "The borrowed instance must warm.");
        borrowed.Dispose();
        Require(!library.IsReady(ShaderId.UiPanel, borrowed), "The shader overload must prune before reading evidence.");
        RequireNoTrackedMaterials(library);

        var owned = library.Get(fixture.Spec);
        library.TrackLayout(owned, Layout);
        Require(library.CompleteWarm(library.CapturePendingWarm()), "The owned instance's layout must warm.");
        Require(library.IsReady(fixture.Spec), "The spec overload must report the live instance ready.");
        owned.Dispose();
        Require(!library.IsReady(fixture.Spec), "The spec overload must tolerate and evict an already released instance.");
        RequireNoTrackedMaterials(library);
        var replacement = library.Get(fixture.Spec);
        Require(GodotObject.IsInstanceValid(replacement) && !ReferenceEquals(owned, replacement), "Get must not return the disposed cache entry.");
        Require(!library.IsReady(fixture.Spec), "A recreated material has no previous layout evidence.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task SpatialAndCanvasEvidenceRemainDistinctAndNewLayoutsInvalidateBatches()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var spatial = fixture.Borrow("spatial");
        var canvas = fixture.Borrow("canvas_item");
        library.Track(ShaderId.Water, spatial);
        Require(library.CapturePendingWarm().Materials.Count == 0, "Spatial without an actual requested layout must stay out of batches.");
        Require(!library.IsReady(ShaderId.Water, spatial), "Spatial without a layout must not be ready.");
        library.TrackLayout(spatial, Layout);
        library.Track(ShaderId.UiPanel, canvas);
        var pending = library.CapturePendingWarm();
        Require(pending.Materials.Count == 2, "A spatial layout and a canvas draw must both be requested.");
        Require(pending.Materials.Single(item => ReferenceEquals(item.Material, spatial)).Layouts.Count == 1, "Spatial evidence must name its layout.");
        Require(pending.Materials.Single(item => ReferenceEquals(item.Material, canvas)).Layouts.Count == 0, "Canvas evidence must not invent vertex layouts.");
        Require(!library.CompleteWarm(new ToonWarmBatch(pending.Revision, pending.Materials)), "A copied batch is not provider-issued evidence.");
        Require(library.CompleteWarm(pending), "The actual issued batch must complete.");
        Require(library.IsReady(ShaderId.Water, spatial) && library.IsReady(ShaderId.UiPanel, canvas), "Both supported modes must be ready after their respective evidence.");

        library.TrackLayout(spatial, new WarmDrawLayout(1, true));
        Require(!library.IsReady(ShaderId.Water, spatial), "A new instanced layout must require new evidence.");
        Require(library.IsReady(ShaderId.UiPanel, canvas), "Adding a spatial layout must not clear the canvas's completed evidence.");
        RequireOnly(library.CapturePendingWarm(), spatial);
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task UnsupportedModesCannotBecomeReadyByCompletingAnEmptyBatch()
    {
        await using var fixture = new Fixture();
        foreach (var mode in new[] { "sky", "particles", "fog" })
        {
            var material = fixture.Borrow(mode);
            fixture.Library.Track(ShaderId.Sky, material);
            fixture.Library.TrackLayout(material, Layout);
            var pending = fixture.Library.CapturePendingWarm();
            Require(pending.Materials.Count == 0, $"No real prewarm path exists for {mode}.");
            Require(fixture.Library.CompleteWarm(pending), "An empty issued batch may complete without granting draw evidence.");
            Require(!fixture.Library.IsReady(ShaderId.Sky, material), $"{mode} must never inherit CanvasItem readiness.");
        }
        using var builtInParticles = new ParticleProcessMaterial();
        fixture.Library.Track(ShaderId.Precip, builtInParticles);
        fixture.Library.TrackLayout(builtInParticles, Layout);
        var builtInBatch = fixture.Library.CapturePendingWarm();
        Require(builtInBatch.Materials.Count == 0, "A non-ShaderMaterial must not be assumed to be Spatial.");
        Require(fixture.Library.CompleteWarm(builtInBatch), "An empty batch must not invent native particle draw evidence.");
        Require(!fixture.Library.IsReady(ShaderId.Precip, builtInParticles), "Built-in particle materials have no supported prewarm path.");
    }

    [TestCase(Timeout = 10000)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task DisposeReleasesOwnedMaterialsAndImageButPreservesBorrowedResourcesAndTextureLeases()
    {
        await using var fixture = new Fixture();
        var library = fixture.Library;
        var owned = library.Get(fixture.Spec);
        var borrowed = fixture.Borrow("canvas_item");
        var shader = borrowed.Shader;
        library.Track(ShaderId.UiPanel, borrowed);
        var inflight = library.CapturePendingWarm();
        var image = ReadPrivate<Image>(library, "_matImage");
        var texture = ReadPrivate<ImageTexture>(library, "_matTexture");
        Require(fixture.Textures.TryAcquire(ShaderGlobals.MatTable, out var lease), "The registered mat_table must be leaseable.");
        using var retained = lease ?? throw new InvalidOperationException("A successful acquire must return a lease.");
        Require(fixture.Textures.TryAcquire(ShaderGlobals.WindowCards, out var cardLease), "The static window_cards table must be registered once by the library.");
        using var retainedCards = cardLease ?? throw new InvalidOperationException("A successful acquire must return a card lease.");
        var cardRid = retainedCards.TextureRid;

        library.Dispose();
        library.Dispose();
        Require(!GodotObject.IsInstanceValid(owned), "Library-created Toon material must be released deterministically.");
        Require(!GodotObject.IsInstanceValid(image), "The producer-owned Image must be released deterministically.");
        Require(GodotObject.IsInstanceValid(borrowed) && GodotObject.IsInstanceValid(shader), "Borrowed material and its shader must stay owned by the caller.");
        Require(GodotObject.IsInstanceValid(texture), "A live reader lease must protect the registry-owned texture from library disposal.");
        Require(!fixture.Textures.TryAcquire(ShaderGlobals.MatTable, out _), "Dispose must unregister mat_table before returning.");
        Require(!fixture.Textures.TryAcquire(ShaderGlobals.WindowCards, out _), "Dispose must unregister window_cards before returning.");
        Require(retainedCards.TextureRid == cardRid, "An existing window-card lease remains valid through producer disposal.");
        Require(fixture.Quality.SubscriberCount == 0, "Dispose must unsubscribe from quality changes.");
        fixture.Quality.ResetPipelines();
        Require(!library.CompleteWarm(inflight), "Dispose invalidates all inflight evidence.");
        Require(!library.IsReady(fixture.Spec) && !library.IsReady(ShaderId.UiPanel, borrowed), "Disposed libraries must never report ready.");
        Require(library.CapturePendingWarm().Materials.Count == 0, "Disposed libraries must not retain warm candidates.");
        RequireNoTrackedMaterials(library);

        retained.Dispose();
        retainedCards.Dispose();
        await fixture.Textures.ShutdownAsync();
        Require(!GodotObject.IsInstanceValid(texture), "The registry must release the texture after the final lease ends.");
    }

    [TestCase(Timeout = 10000)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ConstructorFailureRetiresRegisteredTexturesAndDisposesUntransferredTexture()
    {
        using var registry = new GlobalDataTextureRegistry();
        var rejecting = new RejectCards(registry);
        var rejected = false;
        try { using var unexpected = new ToonLibrary(new Palettes(), new ShaderMaterialSource(), rejecting); }
        catch (InvalidOperationException error) when (error.Message == "reject window_cards") { rejected = true; }
        Require(rejected, "Exercise failure after mat_table registration and card texture creation.");
        Require(!registry.TryAcquire(ShaderGlobals.MatTable, out _), "A partially constructed library must unregister mat_table.");
        Require(rejecting.Cards is not null && !GodotObject.IsInstanceValid(rejecting.Cards), "Rejected texture ownership stays local and must be disposed.");
        await registry.ShutdownAsync();
        Require(rejecting.Mat is not null && !GodotObject.IsInstanceValid(rejecting.Mat), "Previously transferred texture must be released by the registry.");
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

    private static void RequireOnly(ToonWarmBatch batch, Material material)
    {
        Require(batch.Materials.Count == 1 && ReferenceEquals(batch.Materials[0].Material, material), "The batch must contain exactly the live expected instance.");
        Require(batch.Materials.All(item => GodotObject.IsInstanceValid(item.Material)), "A batch must never contain disposed native objects.");
    }

    private static void RequireNoTrackedMaterials(ToonLibrary library)
    {
        Require(ReadPrivate<System.Collections.IDictionary>(library, "_states").Count == 0, "Eviction must release the dictionary's strong references.");
        Require(ReadPrivate<System.Collections.ICollection>(library, "_order").Count == 0, "Eviction must release the ordered list's strong references.");
    }

    private static T ReadPrivate<T>(ToonLibrary library, string name) =>
        (T)(typeof(ToonLibrary).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(library)
            ?? throw new InvalidOperationException($"Missing lifecycle test field: {name}"));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly List<ShaderMaterial> _borrowed = [];
        private readonly List<Shader> _shaders = [];
        public GlobalDataTextureRegistry Textures { get; } = new();
        public QualityEvents Quality { get; } = new();
        public ToonSpec Spec { get; } = new(new PaletteToken("test.lit"), new PaletteToken("test.shade"));
        public ToonLibrary Library { get; }

        public Fixture()
        {
            Require(OS.GetThreadCallerId() == OS.GetMainThreadId(), "Native material lifetime tests must run on Godot's main thread.");
            Library = new ToonLibrary(new Palettes(), new ShaderMaterialSource(), Textures, Quality);
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
            new(new PaletteToken("test.lit"), 64, System.Numerics.Vector3.One, "test", false, PaletteRole.Lit),
            new(new PaletteToken("test.shade"), 65, System.Numerics.Vector3.Zero, "test", false, PaletteRole.Shade)
        ]), [
            new(new PaletteToken("ext.lit"), new PaletteIndex(256), System.Numerics.Vector3.One, "test", false, PaletteRole.Lit),
            new(new PaletteToken("ext.shade"), new PaletteIndex(257), System.Numerics.Vector3.Zero, "test", false, PaletteRole.Shade)
        ]);
    }

    private sealed class QualityEvents : IQualityService
    {
        public DeviceTier Tier => throw new NotSupportedException();
        public QualityPreset Recommended => throw new NotSupportedException();
        public QualityProfile Requested => throw new NotSupportedException();
        public QualityView Effective => throw new NotSupportedException();
        public IReadOnlyList<KnobSpec> Knobs => [];
        public event Action<QualityChange>? Changed;
        public int SubscriberCount => Changed?.GetInvocationList().Length ?? 0;
        public void ResetPipelines() => Changed?.Invoke(new QualityChange(1, 1UL << (int)QualityKnob.ShadowQuality, default, false));
        public float PresetValue(QualityPreset preset, QualityKnob knob) => throw new NotSupportedException();
        public ApplyPlan Plan(QualityProfile next) => throw new NotSupportedException();
        public Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin) => throw new NotSupportedException();
        public void ConfirmDisplay() => throw new NotSupportedException();
    }
}
