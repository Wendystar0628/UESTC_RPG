using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Instances;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stubs.AudioUi;
using Campus.Kernel.Stubs.Visual;
using Campus.Style.Materials;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class D0MaterialWarmupRuntimeTests
{
    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ActualDrawUnlocksPoolAndLateMaterialReentersTheSameVeilBarrier()
    {
        await using var fixture = new Fixture();
        var first = fixture.Mount("first", fixture.Spec);
        Check(!first.Visible && !fixture.Library.IsReady(fixture.Spec), "A pending pool must start hidden.");
        await fixture.Until(() => fixture.Provider.Accepted > 0);
        Check(fixture.Veil.Opacity == 1f && !fixture.Driver.ReadyForReveal,
            "Accepting warm evidence must not skip sample removal and its postdraw barrier.");
        await fixture.Until(() => fixture.Driver.ReadyForReveal);
        await fixture.InitialReady;
        await fixture.Draw();
        Check(first.Visible && fixture.Warmup.SamplesReleased, "The real provider did not unlock the pool after warm draws.");
        Check(fixture.Provider.FirstDrawCompleteFrame > fixture.Provider.FirstCapturedFrame,
            "Evidence must follow actual rendered frames.");
        await fixture.Veil.Reveal(VeilReason.Boot, 0f);
        await fixture.Draw();
        fixture.Capture("WP-00-material-first-visible");
        var secondSpec = fixture.Spec with { Line = LineClass.Fine };
        var late = fixture.Mount("late", secondSpec);
        Check(!late.Visible && !fixture.Driver.ReadyForReveal, "A late material inherited old readiness.");
        await fixture.Until(() => fixture.Veil.Opacity == 1f);
        Check(!late.Visible, "The late pool escaped before its material was drawn.");
        fixture.Capture("WP-00-material-late-cover");
        await fixture.Until(() => fixture.Driver.ReadyForReveal && late.Visible);
        Check(fixture.Provider.Accepted >= 2 && fixture.Provider.Rejected == 0,
            "The late pool must complete its own real batch.");
        await fixture.Until(() => fixture.Veil.Opacity == 0f);
        await fixture.Draw();
        fixture.Capture("WP-00-material-late-visible");
        var externalCover = fixture.Veil.Cover(VeilReason.Transit, .1f);
        var externallyCovered = fixture.Mount("external-cover", fixture.Spec with { Line = LineClass.Tree });
        await fixture.Until(() => fixture.Driver.ReadyForReveal && externallyCovered.Visible);
        await externalCover;
        Check(fixture.Veil.Opacity == 1f && !fixture.Veil.WantsRevealed,
            "Warmup revealed an external Cover that started from the previously visible viewport.");
        await fixture.Veil.Reveal(VeilReason.Transit, 0f);
        await fixture.Draw();
        var coveredDuringWarm = fixture.Mount("cover-during-warm", fixture.Spec with { Line = LineClass.None });
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        await fixture.Veil.Cover(VeilReason.Teleport, 0f);
        await fixture.Until(() => fixture.Driver.ReadyForReveal && coveredDuringWarm.Visible);
        for (var i = 0; i < 5; i++) await fixture.Draw();
        Check(fixture.Veil.Opacity == 1f && !fixture.Veil.WantsRevealed,
            "An explicit Cover during warmup was replaced by an older automatic reveal intent.");
        GD.Print($"MATERIAL_WARMUP_EVIDENCE actualDraw=True firstVisible={first.Visible} lateVisible={late.Visible} " +
            $"accepted={fixture.Provider.Accepted} rejected={fixture.Provider.Rejected} samplesReleased={fixture.Warmup.SamplesReleased} externalCoverRetained=True");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task UncoveredViewportAndZeroSecondRevealCannotProduceWarmEvidence()
    {
        await using var fixture = new Fixture();
        fixture.Cover.Visible = false;
        var pool = fixture.Mount("uncovered", fixture.Spec);
        var reveal = fixture.Veil.Reveal(VeilReason.Boot, 0f);
        fixture.Veil.HoldUntil(VeilReason.Boot, () => false, 0f);
        for (var i = 0; i < MaterialWarmup.StableFrames + 5; i++) await fixture.Draw();
        Check(!fixture.Cover.IsCovered && !fixture.Driver.ReadyForReveal && fixture.Warmup.SamplesReleased &&
            fixture.Provider.Accepted == 0 && !pool.Visible && !reveal.IsCompleted,
            "Uncovered/zero-second reveal fabricated a warm or visibility result.");
        fixture.Cover.Visible = true;
        await fixture.Until(() => fixture.Driver.ReadyForReveal && pool.Visible);
        await reveal;
        GD.Print("MATERIAL_WARMUP_EVIDENCE uncoveredAccepted=0 zeroRevealBlocked=True recovered=True");
    }

    [TestCase]
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
        Check(!fixture.Library.IsReady(fixture.Spec) && fixture.Library.CapturePendingWarm().Revision > batch.Revision,
            "Adding a real layout must invalidate the current batch.");
        await fixture.Until(() => fixture.Provider.Rejected > 0);
        Check(!fixture.Library.IsReady(fixture.Spec) && !pool.Visible,
            "A stale batch partially unlocked the pool.");
        var window = fixture.Parent.GetTree().Root;
        var oldSize = window.Size;
        var resizeInvalidated = false;
        void CheckResizedCoverage() => resizeInvalidated |= !fixture.Cover.IsCovered;
        window.SizeChanged += CheckResizedCoverage;
        try
        {
            await fixture.Until(() => !fixture.Warmup.SamplesReleased);
            window.Size = new Vector2I(oldSize.X + 48, oldSize.Y + 32);
            await fixture.Until(() => fixture.Driver.ReadyForReveal && pool.Visible);
            Check(resizeInvalidated, "A resized viewport inherited the previous coverage certificate.");
            Check(fixture.Provider.Accepted == 1 && fixture.Provider.Rejected == 1 && fixture.Warmup.SamplesReleased,
                "Fresh layout/viewport evidence did not replace the stale batch exactly once.");
            GD.Print("MATERIAL_WARMUP_EVIDENCE staleRejected=True resizeRecertified=True poolVisible=True");
        }
        finally { window.SizeChanged -= CheckResizedCoverage; window.Size = oldSize; }
    }

    [TestCase]
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
            Check(colors.Length == 4 && colors.All(color => PaletteEncoding.DecodeColor(
                (byte)MathF.Round(color.R * 255f), (byte)MathF.Round(color.B * 255f)).Value == 510),
                "A warm sample truncated the bank-1 colour or lost its bank flag.");
        }
        await fixture.Until(() => fixture.Driver.ReadyForReveal && pool.Visible);
        Check(pool.Visible, "Bank-1 warmup failed to unlock the pool.");
        GD.Print("MATERIAL_WARMUP_EVIDENCE paletteIndex=510 nativeBankFlag=True poolVisible=True");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Node3D Parent { get; } = new();
        public GlobalDataTextureRegistry Textures { get; } = new();
        public ToonLibrary Library { get; }
        public RecordingProvider Provider { get; }
        public VeilServiceStub Veil { get; } = new();
        public D0VeilPresentation Cover { get; } = new(Colors.CornflowerBlue);
        public MaterialWarmup Warmup { get; } = new();
        public Camera3D Camera { get; } = new() { Current = true, Near = .05f, Far = 100f };
        public D0MaterialWarmup Driver { get; }
        public Task InitialReady { get; }
        public ToonSpec Spec { get; } = new(new("test.lit"), new("test.shade"));
        private readonly InstancePoolHost _host;

        public Fixture()
        {
            var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot runtime required.");
            var palettes = new Palettes();
            var source = new ShaderMaterialSource();
            Library = new ToonLibrary(palettes, source, Textures);
            Provider = new(Library);
            tree.Root.AddChild(Parent);
            Parent.AddChild(Camera);
            Parent.AddChild(Cover);
            _host = new InstancePoolHost(Provider, source, palettes.Catalog);
            Parent.AddChild(_host);
            Warmup.Configure(Provider, Veil, Cover, Camera, palettes.Catalog);
            Parent.AddChild(Warmup);
            Driver = new(Warmup, Veil, Provider, Cover);
            Parent.AddChild(Driver);
            InitialReady = Driver.WaitUntilReady();
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

        public async Task Draw() => await Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        public void Capture(string name)
        {
            if (System.Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is not { Length: > 0 } root) return;
            var destination = Path.Combine(root, "output", "render-review");
            Directory.CreateDirectory(destination);
            using var image = Parent.GetViewport().GetTexture().GetImage();
            Check(image.SavePng(Path.Combine(destination, name + ".png")) == Godot.Error.Ok, "Could not save warmup evidence.");
        }
        public async Task Until(Func<bool> ready)
        {
            for (var i = 0; i < 1200; i++)
            {
                if (Driver.Error is { } error) throw new InvalidOperationException(error);
                if (ready()) return;
                await Draw();
            }
            throw new TimeoutException("Warmup fixture stalled: " + Warmup.DescribeReadiness());
        }
        public async ValueTask DisposeAsync()
        {
            Parent.Free();
            Library.Dispose();
            await Textures.ShutdownAsync();
        }
    }

    private sealed class Palettes : IPaletteCatalogSource
    {
        public PaletteCatalog Catalog { get; } = new(new PaletteRegistry([
            new(new("test.lit"), 64, N.Vector3.One, "test", false, PaletteRole.Lit),
            new(new("test.shade"), 65, N.Vector3.Zero, "test", false, PaletteRole.Shade)]), [
            new(new("test.high.lit"), new(510), N.Vector3.One, "test", false, PaletteRole.Lit),
            new(new("test.high.shade"), new(511), N.Vector3.Zero, "test", false, PaletteRole.Shade)]);
    }

    private sealed class RecordingProvider(ToonLibrary library) : IToonMaterialProvider
    {
        public int Accepted { get; private set; }
        public int Rejected { get; private set; }
        public ulong FirstCapturedFrame { get; private set; }
        public ulong FirstDrawCompleteFrame { get; private set; }
        public Material Get(ToonSpec spec) => library.Get(spec);
        public bool IsReady(ToonSpec spec) => library.IsReady(spec);
        public void Track(ShaderId shader, Material material) => library.Track(shader, material);
        public void TrackLayout(Material material, WarmDrawLayout layout) => library.TrackLayout(material, layout);
        public bool IsReady(ShaderId shader, Material material) => library.IsReady(shader, material);
        public ToonWarmBatch CapturePendingWarm()
        {
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
