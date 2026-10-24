using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Quality.Settings;
using Campus.Kernel.Renderer;
using Campus.Kernel.Settings;
using GdUnit4;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Tests;

[TestSuite]
public sealed class GameCaptureLifetimeTests
{
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task PendingCaptureFailureBelongsToCallerWhileExitWaitsForUiCleanup()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var resource = new Node();
        var loading = new ResourceLoadingPresentation(resource);
        var hud = new CanvasLayer();
        var capture = new GameCaptureNode();
        using var frames = new FrameStatistics(tree.Root);
        await using var settings = new SettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        var catalog = QualityApplicationTestFixture.Catalog();
        await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
        var recorder = new PerformanceRecordingService(frames, settings, new TestDiagnosticLog(),
            new TestInputRouter(), new RequiredResources(), quality);
        KernelServices.Services.Add<IAppLoadingPresentation>(loading);
        KernelServices.Services.Add(recorder);
        KernelServices.Services.Discover([typeof(GameCaptureNode)], _ => "fixture");
        tree.Root.AddChild(hud);
        CapturePresentation.Bind(hud);
        tree.Root.AddChild(capture);
        Task<byte[]?>? caller = null;
        Task? draining = null;
        try
        {
            caller = capture.CaptureThumbnailAsync();
            draining = capture.DrainForExitAsync();
            // Establish the original race before causing any resource failure or viewport readback.
            if (caller.IsCompleted || draining.IsCompleted || hud.Visible)
                throw new InvalidOperationException("Capture and exit did not retain the pending UI-owning operation.");
            resource.Free();
            Exception? received = null;
            try { await caller; }
            catch (ObjectDisposedException error) { received = error; }
            await draining;
            if (loading.Failure is null || !ReferenceEquals(received, loading.Failure) || !hud.Visible)
                throw new InvalidOperationException("Caller lost the native failure or exit finished before UI cleanup.");
        }
        finally
        {
            capture.PrepareForExit();
            if (caller is not null)
                await ((Task)caller).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing |
                    ConfigureAwaitOptions.ContinueOnCapturedContext);
            if (draining is not null)
                await draining.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing |
                    ConfigureAwaitOptions.ContinueOnCapturedContext);
            await recorder.DrainForExitAsync();
            capture.Free();
            hud.Free();
            if (GodotObject.IsInstanceValid(resource)) resource.Free();
        }
    }

    // The dependency reads a real native resource; disposal fails before any image is read or saved.
    private sealed class ResourceLoadingPresentation(Node resource) : IAppLoadingPresentation
    {
        public Exception? Failure { get; private set; }
        public bool Active
        {
            get
            {
                try { return resource.IsInsideTree(); }
                catch (ObjectDisposedException error) { Failure = error; throw; }
            }
        }
        public bool IsCovered => Active;
        public float Progress => 0;
        public IAppLoadingLease Show(EnterRequest? request = null) => throw new NotSupportedException();
    }
}
