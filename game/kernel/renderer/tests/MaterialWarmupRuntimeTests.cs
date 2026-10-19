using Campus.Tests;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Instances;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Kernel.Post;
using Campus.Style.Materials;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed partial class MaterialWarmupRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ActualDrawUnlocksPoolsOnlyAfterTheirOwnCoveredWarmBatch()
    {
        await using var fixture = new Fixture();
        var first = fixture.Mount("first", fixture.Spec);
        AssertThat(!first.Visible && !fixture.Library.IsReady(fixture.Spec)).OverrideFailureMessage("A pending pool must start hidden.").IsTrue();
        await fixture.Until(() => fixture.Provider.Accepted > 0);
        AssertThat(fixture.Veil.Opacity == 1f && !fixture.ReadyForReveal).OverrideFailureMessage("Accepting warm evidence must not skip sample removal and its postdraw barrier.").IsTrue();
        await fixture.Until(() => fixture.ReadyForReveal);
        await fixture.Draw();
        AssertThat(first.Visible && fixture.Warmup.SamplesReleased).OverrideFailureMessage("The real provider did not unlock the pool after warm draws.").IsTrue();
        AssertThat(fixture.Provider.FirstDrawCompleteFrame > fixture.Provider.FirstCapturedFrame).OverrideFailureMessage("Evidence must follow actual rendered frames.").IsTrue();
        await fixture.Boot.Reveal(0f);
        await fixture.Draw();
        var secondSpec = fixture.Spec with { Line = LineClass.Fine };
        using var lateCover = fixture.Veil.Cover(VeilReason.Boot, 0f);
        await lateCover.Covered;
        var late = fixture.Mount("late", secondSpec);
        AssertThat(!late.Visible && !fixture.ReadyForReveal).OverrideFailureMessage("A late material inherited old readiness.").IsTrue();
        await fixture.Until(() => fixture.Veil.Opacity == 1f);
        AssertThat(!late.Visible).OverrideFailureMessage("The late pool escaped before its material was drawn.").IsTrue();
        await fixture.Until(() => fixture.ReadyForReveal && late.Visible);
        AssertThat(fixture.Provider.Accepted >= 2 && fixture.Provider.Rejected == 0).OverrideFailureMessage("The late pool must complete its own real batch.").IsTrue();
        await lateCover.Reveal(0f);
        await fixture.Until(() => fixture.Veil.Opacity == 0f);
        await fixture.Draw();
        using var externalCover = fixture.Veil.Cover(VeilReason.Transit, 0f);
        var externallyCovered = fixture.Mount("external-cover", fixture.Spec with { Line = LineClass.Tree });
        await fixture.Until(() => fixture.ReadyForReveal && externallyCovered.Visible);
        await externalCover.Covered;
        AssertThat(fixture.Veil.Opacity == 1f && fixture.Veil.Reason == VeilReason.Transit).OverrideFailureMessage("Warmup revealed an external Cover that started from the previously visible viewport.").IsTrue();
        await externalCover.Reveal(0f);
        await fixture.Draw();
        using var warmCover = fixture.Veil.Cover(VeilReason.Boot, 0f);
        await warmCover.Covered;
        var coveredDuringWarm = fixture.Mount("cover-during-warm", fixture.Spec with { Line = LineClass.None });
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        using var teleportCover = fixture.Veil.Cover(VeilReason.Teleport, 0f);
        await teleportCover.Covered;
        await fixture.Until(() => fixture.ReadyForReveal && coveredDuringWarm.Visible);
        for (var i = 0; i < 5; i++) await fixture.Draw();
        AssertThat(fixture.Veil.Opacity == 1f && fixture.Veil.Reason == VeilReason.Teleport).OverrideFailureMessage("An explicit Cover during warmup was replaced by an older automatic reveal intent.").IsTrue();
        GD.Print($"MATERIAL_WARMUP_EVIDENCE actualDraw=True firstVisible={first.Visible} lateVisible={late.Visible} " +
            $"accepted={fixture.Provider.Accepted} rejected={fixture.Provider.Rejected} samplesReleased={fixture.Warmup.SamplesReleased} externalCoverRetained=True");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task UncoveredViewportCannotProduceWarmEvidenceOrReleaseTheBootCover()
    {
        await using var fixture = new Fixture();
        fixture.Cover.Visible = false;
        var pool = fixture.Mount("uncovered", fixture.Spec);
        for (var i = 0; i < MaterialWarmup.StableFrames + 5; i++) await fixture.Draw();
        AssertThat(!fixture.Cover.IsCovered && !fixture.ReadyForReveal && fixture.Warmup.SamplesReleased &&
            fixture.Provider.Accepted == 0 && !pool.Visible && fixture.Boot.IsCurrent).OverrideFailureMessage("An uncovered viewport fabricated a warm or visibility result or lost its boot cover.").IsTrue();
        fixture.Cover.Visible = true;
        await fixture.Until(() => fixture.ReadyForReveal && pool.Visible);
        await fixture.Draw();
        await fixture.Boot.Reveal(0f);
        GD.Print("MATERIAL_WARMUP_EVIDENCE uncoveredAccepted=0 bootCoverRetained=True recovered=True");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ChangedLayoutRejectsInflightEvidenceAndResizeRequiresFreshCoveredDraws()
    {
        await using var fixture = new Fixture();
        var pool = fixture.Mount("revision", fixture.Spec);
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        var batch = fixture.Library.CapturePendingWarm();
        var material = batch.Materials.Single().Material;
        fixture.Library.TrackLayout(material, batch.Materials.Single().Layouts.Single() with { Instanced = false });
        AssertThat(!fixture.Library.IsReady(fixture.Spec) && fixture.Library.CapturePendingWarm().Revision > batch.Revision).OverrideFailureMessage("Adding a real layout must invalidate the current batch.").IsTrue();
        await fixture.Until(() => fixture.Provider.Rejected > 0);
        AssertThat(!fixture.Library.IsReady(fixture.Spec) && !pool.Visible).OverrideFailureMessage("A stale batch partially unlocked the pool.").IsTrue();
        var window = fixture.Parent.GetTree().Root;
        var oldSize = window.Size;
        var resizeInvalidated = false;
        void CheckResizedCoverage() => resizeInvalidated |= !fixture.Cover.IsCovered;
        window.SizeChanged += CheckResizedCoverage;
        try
        {
            await fixture.Until(() => !fixture.Warmup.SamplesReleased);
            window.Size = new Vector2I(oldSize.X + 48, oldSize.Y + 32);
            await fixture.Until(() => fixture.ReadyForReveal && pool.Visible);
            AssertThat(resizeInvalidated).OverrideFailureMessage("A resized viewport inherited the previous coverage certificate.").IsTrue();
            AssertThat(fixture.Provider.Accepted == 1 && fixture.Provider.Rejected == 1 && fixture.Warmup.SamplesReleased).OverrideFailureMessage("Fresh layout/viewport evidence did not replace the stale batch exactly once.").IsTrue();
            GD.Print("MATERIAL_WARMUP_EVIDENCE staleRejected=True resizeRecertified=True poolVisible=True");
        }
        finally { window.SizeChanged -= CheckResizedCoverage; window.Size = oldSize; }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task BankOneSamplePreservesTheFullPaletteIndexAndUnlocksItsPool()
    {
        await using var fixture = new Fixture();
        var spec = fixture.Spec with { Lit = new("test.high.lit"), Shade = new("test.high.shade") };
        var pool = fixture.Mount("bank-one", spec);
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        var samples = fixture.Camera.GetChildren().OfType<Node3D>().Single(node => node.Name == "MaterialWarmSamples");
        var mesh = (ArrayMesh)samples.GetChild<MultiMeshInstance3D>(0).Multimesh.Mesh;
        using (var arrays = mesh.SurfaceGetArrays(0))
        {
            var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
            AssertThat(colors.Length == 4 && colors.All(color => PaletteEncoding.DecodeColor(
                (byte)MathF.Round(color.R * 255f), (byte)MathF.Round(color.B * 255f)).Value == 510)).OverrideFailureMessage("A warm sample truncated the bank-1 colour or lost its bank flag.").IsTrue();
        }
        await fixture.Until(() => fixture.ReadyForReveal && pool.Visible);
        AssertThat(pool.Visible).OverrideFailureMessage("Bank-1 warmup failed to unlock the pool.").IsTrue();
        GD.Print("MATERIAL_WARMUP_EVIDENCE paletteIndex=510 nativeBankFlag=True poolVisible=True");
    }

    private sealed partial class Fixture : IAsyncDisposable
    {
        public Node3D Parent { get; } = new();
        public GlobalDataTextureRegistry Textures { get; } = new();
        public ToonLibrary Library { get; }
        public RecordingProvider Provider { get; }
        public VeilTimeline Veil { get; } = new();
        public IVeilLease Boot { get; }
        public VeilLayer Cover { get; } = new();
        public MaterialWarmup Warmup { get; } = new();
        public Camera3D Camera { get; } = new() { Current = true, Near = .05f, Far = 100f };
        public bool ReadyForReveal => Warmup.IsReady && Warmup.SamplesReleased && !Warmup.HasPendingWarm;
        private bool _warming;
        public ToonSpec Spec { get; } = new(new("test.lit"), new("test.shade"));
        private readonly InstancePoolHost _host;

        public Fixture()
        {
            Boot = Veil.Cover(VeilReason.Boot, 0f);
            var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot runtime required.");
            var palettes = new Palettes();
            var source = new ShaderMaterialSource();
            Library = new ToonLibrary(palettes, source, Textures);
            Provider = new(Library);
            var colors = new PresentationColors();
            Cover.Configure(colors, new Campus.Kernel.World.WorldStateService(new TestWeatherProcess()), colors);
            tree.Root.AddChild(Parent);
            Parent.AddChild(Camera);
            Parent.AddChild(Cover);
            _host = new InstancePoolHost(Provider, source, palettes.Catalog);
            Parent.AddChild(_host);
            Warmup.Configure(Provider, Veil, Cover, Camera, palettes.Catalog, Loading);
            Parent.AddChild(Warmup);
            Cover.Present(new VeilFrame(Veil.Opacity, Veil.Content, null));
        }

        public MultiMeshInstance3D Mount(string id, ToonSpec spec)
        {
            var surface = GeometryBuffer.Freeze(new GeometryBundle(new(spec),
                [-.5f, -.5f, 0f, .5f, -.5f, 0f, 0f, .5f, 0f],
                [0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f], [0, 1, 2, 0, 2, 1])
            { ColorIndices = spec.Lit.Value == "test.high.lit" ? [510, 510, 510] : [64, 64, 64] });
            var prototype = new GeometryPrototype(id, [surface], new Box3(new(-.5f, -.5f, 0f), new(.5f, .5f, 0f)));
            var pool = _host.Mount(new GeometryInstancePoolSpec(id, "WP-00", prototype, 1,
                new Aabb(new(-10f, -10f, -10f), new(20f, 20f, 20f)), 1));
            var buffer = new float[16];
            InstanceStreamLayout.Write(buffer, N.Matrix4x4.CreateTranslation(0f, 0f, -3f), N.Vector4.Zero);
            pool.WriteBuffer(buffer);
            pool.VisibleCount = 1;
            return _host.GetChildren().OfType<MultiMeshInstance3D>().Single(node => node.GetMeta("instance_pool_id").AsString() == id);
        }

        public async Task Draw()
        {
            Veil.Advance(1d / 60);
            Cover.Present(new VeilFrame(Veil.Opacity, Veil.Content, null));
            await Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (_runtime is not null) { _runtime._Process(0); return; }
            // Test composition explicitly starts each production warm batch after a real covered draw.
            if (!_warming && Veil.Opacity == 1f && Cover.IsCovered && Warmup.HasPendingWarm)
            { Warmup.Begin(); _warming = true; }
            if (_warming && ReadyForReveal) { Warmup.End(); _warming = false; }
        }
        public async Task Until(Func<bool> ready)
        {
            for (var i = 0; i < 1200; i++)
            {
                if (Warmup.Error is { } error) throw new InvalidOperationException(error);
                if (ready()) return;
                await Draw();
            }
            throw new TimeoutException("Warmup fixture stalled: " + Warmup.DescribeReadiness());
        }
        public async ValueTask DisposeAsync()
        {
            if (_arrivalDraw is not null) RenderingServer.Singleton.FramePostDraw -= _arrivalDraw;
            _runtime?.StopNewWork();
            _runtime?.Free();
            Boot.Dispose();
            Parent.Free();
            Library.Dispose();
            await Textures.ShutdownAsync();
        }
    }

    private sealed class PresentationColors : IColorScriptFrame, IPaletteProbe
    {
        public ColorScriptSnapshot Current => default(ColorScriptSnapshot) with { FogSun = new(.4f), FogAnti = new(.2f) };
        public void SampleHorizon(Season season, WeatherVec weather, Span<N.Vector3> srgb24) => srgb24.Fill(new(.3f));
    }

    private sealed class Palettes : IPaletteCatalogSource
    {
        public PaletteCatalog Catalog { get; } = new(new PaletteRegistry([
            new(new("test.lit"), new(64), N.Vector3.One, "test", false, PaletteRole.Lit),
            new(new("test.shade"), new(65), N.Vector3.Zero, "test", false, PaletteRole.Shade)]), [
            new(new("test.high.lit"), new(510), N.Vector3.One, "test", false, PaletteRole.Lit),
            new(new("test.high.shade"), new(511), N.Vector3.Zero, "test", false, PaletteRole.Shade)]);
    }

    private sealed class RecordingProvider(ToonLibrary library) : IToonMaterialProvider
    {
        public event Action? WarmChanged { add => library.WarmChanged += value; remove => library.WarmChanged -= value; }
        public int Accepted { get; private set; }
        public int Rejected { get; private set; }
        public int Captures { get; private set; }
        public ulong FirstCapturedFrame { get; private set; }
        public ulong FirstDrawCompleteFrame { get; private set; }
        public Material Get(ToonSpec spec) => library.Get(spec);
        public bool IsReady(ToonSpec spec) => library.IsReady(spec);
        public void Track(ShaderId shader, Material material) => library.Track(shader, material);
        public void TrackLayout(Material material, WarmDrawLayout layout) => library.TrackLayout(material, layout);
        public bool IsReady(ShaderId shader, Material material) => library.IsReady(shader, material);
        public ToonWarmBatch CapturePendingWarm()
        {
            Captures++;
            var batch = library.CapturePendingWarm();
            if (batch.Materials.Count > 0 && FirstCapturedFrame == 0) FirstCapturedFrame = Engine.GetProcessFrames();
            return batch;
        }
        public bool CompleteWarm(ToonWarmBatch batch)
        {
            var accepted = library.CompleteWarm(batch);
            if (accepted) { Accepted++; if (FirstDrawCompleteFrame == 0) FirstDrawCompleteFrame = Engine.GetProcessFrames(); }
            else Rejected++;
            return accepted;
        }
    }
}
