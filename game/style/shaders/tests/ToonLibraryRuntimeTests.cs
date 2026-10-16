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

        library.Dispose();
        library.Dispose();
        Require(!GodotObject.IsInstanceValid(owned), "Library-created Toon material must be released deterministically.");
        Require(!GodotObject.IsInstanceValid(image), "The producer-owned Image must be released deterministically.");
        Require(GodotObject.IsInstanceValid(borrowed) && GodotObject.IsInstanceValid(shader), "Borrowed material and its shader must stay owned by the caller.");
        Require(GodotObject.IsInstanceValid(texture), "A live reader lease must protect the registry-owned texture from library disposal.");
        Require(!fixture.Textures.TryAcquire(ShaderGlobals.MatTable, out _), "Dispose must unregister mat_table before returning.");
        Require(fixture.Quality.SubscriberCount == 0, "Dispose must unsubscribe from quality changes.");
        fixture.Quality.ResetPipelines();
        Require(!library.CompleteWarm(inflight), "Dispose invalidates all inflight evidence.");
        Require(!library.IsReady(fixture.Spec) && !library.IsReady(ShaderId.UiPanel, borrowed), "Disposed libraries must never report ready.");
        Require(library.CapturePendingWarm().Materials.Count == 0, "Disposed libraries must not retain warm candidates.");
        RequireNoTrackedMaterials(library);

        retained.Dispose();
        await fixture.Textures.ShutdownAsync();
        Require(!GodotObject.IsInstanceValid(texture), "The registry must release the texture after the final lease ends.");
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
