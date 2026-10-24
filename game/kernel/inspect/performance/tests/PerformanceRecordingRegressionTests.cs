using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Quality.Settings;
using Campus.Kernel.Settings;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class PerformanceRecordingRegressionTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public void FirstPublishedFrameAlreadyHasRoomForEveryRegisteredViewport()
    {
        var ring = new PerfFrameRing(capacity: 1);
        var viewports = new ViewportFrameSample[KernelBudgets.PerfMeasuredViewportLimit];
        var frame = new FrameSample { CollectionFrame = 1 };
        if (viewports.Length <= 1) throw new InvalidOperationException("The fixture no longer exercises growth beyond the former empty-layout slot.");
        if (!ring.TryWrite(in frame, viewports) || ring.DroppedFrames != 0 || ring.Peek()?.ViewportCount != viewports.Length)
            throw new InvalidOperationException("The first registered-viewport frame was dropped or truncated.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void UnlockedCrashedPendingSessionsParticipateInAuthorRetention()
    {
        string root = PerformanceRecordingContractTests.TemporaryDirectory();
        try
        {
            string? oldest = null;
            for (int index = 0; index < KernelBudgets.PerfAuthorRetainedSessions; index++)
            {
                string id = "crashed-" + index;
                string directory = Path.Combine(root, id);
                Directory.CreateDirectory(directory);
                using (File.Create(Path.Combine(directory, ".lock"))) { }
                PerfFieldDictionary.PublishJson(Path.Combine(directory, "manifest.pending.json"), new JsonObject
                {
                    ["id"] = id, ["status"] = "recording",
                    ["startedAt"] = DateTimeOffset.FromUnixTimeSeconds(index).ToString("O")
                });
                oldest ??= directory;
            }
            if (oldest is null || File.Exists(Path.Combine(oldest, "manifest.json")))
                throw new InvalidOperationException("The fixture did not create a pending-only crashed session.");
            using (new FileStream(Path.Combine(oldest, ".lock"), FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None)) { }
            PerfSessionStorage.PruneAuthor(root);
            if (Directory.Exists(oldest) || Directory.EnumerateDirectories(root).Count() >= KernelBudgets.PerfAuthorRetainedSessions)
                throw new InvalidOperationException("Pending-only sessions escaped the author retention limit.");
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ManifestUsesRealWindowScaleAndTheSharedEnumConverter()
    {
        string root = PerformanceRecordingContractTests.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            var window = DisplayServer.WindowGetSize();
            if (window.X <= 0 || window.Y <= 0) throw new InvalidOperationException("The native window-size trigger is unavailable.");
            var manifest = PerfManifest.Capture(new BootRunOptions(), new(true, true), quality);
            float scale = quality.Effective[QualityKnob.RenderScale];
            if (manifest["graphics"]!["internalWidthPx"]!.GetValue<int>() != (int)(window.X * scale) ||
                manifest["graphics"]!["internalHeightPx"]!.GetValue<int>() != (int)(window.Y * scale))
                throw new InvalidOperationException("The manifest lost the real internal resolution.");
            string? expectedPreset = JsonSerializer.SerializeToElement(quality.Effective.Preset, PerfFieldDictionary.JsonOptions).GetString();
            string? expectedVsync = JsonSerializer.SerializeToElement(DisplayServer.WindowGetVsyncMode(), PerfFieldDictionary.JsonOptions).GetString();
            if (manifest["graphics"]!["preset"]!.GetValue<string>() != expectedPreset ||
                manifest["pacing"]!["vsync"]!.GetValue<string>() != expectedVsync)
                throw new InvalidOperationException("Manifest enums bypassed the shared camel-case converter.");
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ThrowingStateSubscriberFailsItsRequestAndDrainsTheQueuedStop()
    {
        string root = PerformanceRecordingContractTests.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var resources = new RequiredResources();
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            using var frames = new FrameStatistics(((SceneTree)Engine.GetMainLoop()).Root);
            var diagnostics = new TestDiagnosticLog();
            var recorder = new PerformanceRecordingService(frames, settings, diagnostics, new TestInputRouter(), resources, quality);
            var original = new InvalidOperationException();
            Task<PerfRecordingSnapshot>? queuedStop = null;
            bool invoked = false;
            recorder.StateChanged += snapshot =>
            {
                if (snapshot.State != PerfRecordingState.Starting) return;
                invoked = true;
                queuedStop = recorder.SetRecordingAsync(false);
                throw original;
            };
            try
            {
                var opening = recorder.InitializeAsync(new BootRunOptions { PerfOutputDirectory = root });
                if (!invoked || queuedStop is null) throw new InvalidOperationException("The subscriber failure trigger was not reached.");
                if (!opening.IsCompleted) throw new InvalidOperationException("The throwing subscriber stranded its request completion.");
                try { await opening; throw new InvalidOperationException("The throwing subscriber request succeeded."); }
                catch (InvalidOperationException error) when (ReferenceEquals(error, original)) { }
                if ((await queuedStop).State != PerfRecordingState.Off ||
                    !ReferenceEquals(recorder.LastEndResult?.Error, original) || diagnostics.Errors.Count != 1 ||
                    !ReferenceEquals(diagnostics.Errors[0], original))
                    throw new InvalidOperationException("Request failure lost its original exception or blocked the queued stop.");
            }
            finally { await recorder.DrainForExitAsync(); }
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task FaultingSubscriberCannotStrandCleanupFromWriterOrProducer(bool producer)
    {
        string root = PerformanceRecordingContractTests.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            using var frames = new FrameStatistics(((SceneTree)Engine.GetMainLoop()).Root);
            var diagnostics = new TestDiagnosticLog();
            var recorder = new PerformanceRecordingService(frames, settings, diagnostics, new TestInputRouter(), new RequiredResources(), quality);
            var session = new PerfRecordingSession(root, PerfOrigin.Play, DateTimeOffset.UnixEpoch,
                new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json"))), new JsonObject());
            try
            {
                if (!await session.Ready) throw new InvalidOperationException("The injected live session did not open.");
                session.Begin(new(0, 0, 0));
                if (!await session.StartedWriting) throw new InvalidOperationException("The injected live session did not start writing.");
                // Inject an owned live session without booting unrelated map/render services.
                const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(PerformanceRecordingService).GetField("_session", fields)!.SetValue(recorder, session);
                typeof(PerformanceRecordingService).GetField("_state", fields)!.SetValue(recorder, PerfRecordingState.Recording);
                var original = new IOException();
                var observer = new InvalidOperationException();
                recorder.StateChanged += value => { if (value.State == PerfRecordingState.Faulted) throw observer; };
                if (producer)
                    typeof(PerformanceRecordingService).GetMethod("ProducerFailure", fields)!.Invoke(recorder, [original]);
                else
                {
                    session.AddFailure(PerfFaultCode.WriteFailed, original);
                    recorder.Poll();
                }
                var ended = await session.Completion.WaitAsync(TimeSpan.FromSeconds(5));
                if (!ended.Closed || ended.Complete || ended.Error is not AggregateException aggregate ||
                    !aggregate.Flatten().InnerExceptions.Contains(original) || !aggregate.Flatten().InnerExceptions.Contains(observer))
                    throw new InvalidOperationException("Fault cleanup did not close the real session with both original failures.");
                await recorder.DrainForExitAsync();
                using var released = new FileStream(Path.Combine(root, ended.SessionId, ".lock"),
                    FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
                if (diagnostics.Errors.Count != 1 || !ReferenceEquals(diagnostics.Errors[0], ended.Error) ||
                    !File.Exists(Path.Combine(root, ended.SessionId, "manifest.json")))
                    throw new InvalidOperationException("Fault cleanup did not publish and receive its end result exactly once.");
            }
            finally { await recorder.DrainForExitAsync(); }
        }
        finally { Directory.Delete(root, true); }
    }
}
