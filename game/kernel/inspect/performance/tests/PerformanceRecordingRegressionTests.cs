using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Quality.Settings;
using Campus.Kernel.Settings;
using GdUnit4;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Tests;

[TestSuite]
public sealed class PerformanceRecordingRegressionTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task PreCanceledAdapterCaptureCompletesWithoutAccessingTheRenderer()
    {
        using var exit = new CancellationTokenSource();
        exit.Cancel();
        var capture = RenderAdapterIdentity.CaptureAsync(exit.Token);
        if (!capture.IsCanceled)
            throw new InvalidOperationException("An already stopped owner queued rendering work.");
        try { await capture; }
        catch (OperationCanceledException error) when (error.CancellationToken == exit.Token) { return; }
        throw new InvalidOperationException("The adapter capture did not retain its owner's cancellation.");
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ActualAdapterCaptureAndOwnerExitBothComplete()
    {
        using var exit = new CancellationTokenSource();
        var actual = await RenderAdapterIdentity.CaptureAsync(exit.Token);
        if (actual.Status.Capability != "supported" || actual.Luid is null)
            throw new InvalidOperationException("The fixture did not capture the actual D3D12 rendering adapter.");
        var stopping = RenderAdapterIdentity.CaptureAsync(exit.Token);
        exit.Cancel();
        try
        {
            var completed = await stopping;
            if (completed.Luid != actual.Luid || completed.Status.Error is not null)
                throw new InvalidOperationException("The started native capture lost its detached identity or failed while draining.");
        }
        catch (OperationCanceledException error) when (error.CancellationToken == exit.Token) { }
    }

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

    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task MainThreadSamplingAndRecordingQueueHaveNoManagedAllocation()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            var tree = (SceneTree)Engine.GetMainLoop();
            using var frames = new FrameStatistics(tree.Root);
            frames.Enable();
            var diagnostics = new TestDiagnosticLog();
            var recorder = new PerformanceRecordingService(frames, settings, diagnostics, new TestInputRouter(), new RequiredResources(), quality);
            var camera = new Campus.Camera.ObserverCamera();
            var walk = new Campus.Camera.Walk.WalkController();
            var stream = new Campus.Kernel.Stream.StreamDirector();
            BindRecordingOwners(quality, camera, walk, stream);
            recorder.AttachViewport(tree.Root, new TestRenderDimensions());
            try
            {
                var opened = await recorder.InitializeAsync(new BootRunOptions { PerfOutputDirectory = root });
                if (opened.State != PerfRecordingState.Recording || opened.Fault != PerfFaultCode.None)
                    throw new InvalidOperationException("The actual recording chain did not start.");
                var context = new FrameContextSample(AppState.Playing, default, false, true, false);
                var elapsed = new long[TestSamples.PerProperty];
                FrameSample gcSample = default;
                long allocated = 0;
                ulong lastFrame = 0;
                int measured = 0;
                // Await and fixture work stay outside the call being measured. The real event subscriber enqueues it.
                for (int index = 0; index < TestSamples.PerProperty * 2; index++)
                {
                    await tree.Root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    if (OS.GetThreadCallerId() != OS.GetMainThreadId())
                        throw new InvalidOperationException("Sampling overhead must be measured on the engine main thread.");
                    ulong frame = Engine.GetProcessFrames();
                    lastFrame = frame;
                    frames.Begin(frame, FrameClock.MonotonicMilliseconds);
                    if (index == TestSamples.PerProperty) GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    double now = FrameClock.MonotonicMilliseconds;
                    long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                    long before = System.Diagnostics.Stopwatch.GetTimestamp();
                    frames.Sample(frame, now, in context, 0, 0, null);
                    long after = System.Diagnostics.Stopwatch.GetTimestamp();
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
                    if (index == TestSamples.PerProperty) gcSample = frames.Latest!.Value;
                    frames.PreDraw(frame, FrameClock.MonotonicMilliseconds);
                    frames.PostDraw(frame, FrameClock.MonotonicMilliseconds);
                    if (index < TestSamples.PerProperty) continue;
                    allocated += bytes;
                    elapsed[measured++] = after - before;
                }
                await recorder.SetRecordingAsync(false);
                var ended = recorder.LastEndResult;
                if (measured != elapsed.Length || frames.Latest?.CollectionFrame != lastFrame ||
                    ended is not { Closed: true, Complete: true, Fault: PerfFaultCode.None, DroppedFrames: 0 } ||
                    ended.WrittenFrames != TestSamples.PerProperty * 2 || diagnostics.Errors.Count != 0)
                    throw new InvalidOperationException("The measured real sampler did not publish and write every queued frame.");
                AssertRecordedGcDetail(root, ended!, in gcSample);
                Array.Sort(elapsed);
                double microseconds = 1_000_000d / System.Diagnostics.Stopwatch.Frequency;
                Console.WriteLine($"PERF_RECORDING_OVERHEAD actual-main-thread sampling+enqueue N={measured} allocatedBytes={allocated} " +
                    $"p50Us={elapsed[(int)Math.Ceiling(measured * .50) - 1] * microseconds:G4} " +
                    $"p95Us={elapsed[(int)Math.Ceiling(measured * .95) - 1] * microseconds:G4} " +
                    $"p99Us={elapsed[(int)Math.Ceiling(measured * .99) - 1] * microseconds:G4} maxUs={elapsed[^1] * microseconds:G4}; elapsed is reported without an invented threshold.");
                if (allocated != 0)
                    throw new InvalidOperationException("ENG-PERF-DATA sampling and enqueue allocated managed bytes on the main thread: " + allocated);
            }
            finally
            {
                try { await recorder.DrainForExitAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing); }
                finally { camera.Free(); walk.Free(); stream.Free(); }
            }
        }
        finally { Directory.Delete(root, true); }
    }

    private static void AssertRecordedGcDetail(string root, PerfSessionEndResult ended, in FrameSample sample)
    {
        if (sample.Gc.Gen0Collections == 0)
            throw new InvalidOperationException("The measured sampler did not observe the forced collection.");
        ulong frame = sample.CollectionFrame;
        var captured = File.ReadLines(Path.Combine(root, ended.SessionId, "events.jsonl")).Select(line => JsonNode.Parse(line)!)
            .SingleOrDefault(item => item["event"]!.GetValue<string>() == "gc.detail" && item["collectionFrame"]!.GetValue<ulong>() == frame);
        var payload = captured?["payload"];
        if (captured is null || payload is null || captured["source"]!.GetValue<string>() != "main" ||
            captured["monotonicMs"]!.GetValue<double>() != sample.MonotonicMilliseconds || payload["pauseMs"]!.GetValue<double>() != sample.Gc.PauseMilliseconds ||
            payload["gen0"]!.GetValue<int>() != sample.Gc.Gen0Collections || payload["gen1"]!.GetValue<int>() != sample.Gc.Gen1Collections ||
            payload["gen2"]!.GetValue<int>() != sample.Gc.Gen2Collections || payload["allocatedBytes"]!.GetValue<long>() != sample.Gc.AllocatedBytes ||
            payload["managedHeapMiB"]!.GetValue<double>() != sample.Gc.ManagedHeapBytes / 1048576d)
            throw new InvalidOperationException("The writer lost the measured collection's original frame facts.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void UnlockedCrashedPendingSessionsParticipateInAuthorRetention()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
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

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ThrowingObserversKeepTheRealRecordingAndQueuedStopSuccessful()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var resources = new RequiredResources();
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            using var frames = new FrameStatistics(((SceneTree)Engine.GetMainLoop()).Root);
            var diagnostics = new TestDiagnosticLog();
            var recorder = new PerformanceRecordingService(frames, settings, diagnostics, new TestInputRouter(), resources, quality);
            var camera = new Campus.Camera.ObserverCamera();
            var walk = new Campus.Camera.Walk.WalkController();
            var stream = new Campus.Kernel.Stream.StreamDirector();
            BindRecordingOwners(quality, camera, walk, stream);
            recorder.AttachViewport(((SceneTree)Engine.GetMainLoop()).Root, new TestRenderDimensions());
            var failures = new Dictionary<PerfRecordingState, Exception>
            {
                [PerfRecordingState.Starting] = new InvalidOperationException(),
                [PerfRecordingState.Recording] = new InvalidOperationException(),
                [PerfRecordingState.Stopping] = new InvalidOperationException(),
                [PerfRecordingState.Off] = new InvalidOperationException()
            };
            var endFailure = new IOException();
            Task<PerfRecordingSnapshot>? queuedStop = null;
            var observed = new List<PerfRecordingState>();
            PerfSessionEndResult? observedEnd = null;
            recorder.StateChanged += snapshot =>
            {
                if (snapshot.State == PerfRecordingState.Recording) queuedStop = recorder.SetRecordingAsync(false);
                if (failures.TryGetValue(snapshot.State, out var error)) throw error;
            };
            recorder.StateChanged += snapshot => observed.Add(snapshot.State);
            recorder.SessionEnded += _ => throw endFailure;
            recorder.SessionEnded += value => observedEnd = value;
            try
            {
                var opened = await recorder.InitializeAsync(new BootRunOptions { PerfOutputDirectory = root });
                if (opened.State != PerfRecordingState.Recording || opened.Fault != PerfFaultCode.None || queuedStop is null)
                    throw new InvalidOperationException("The real recording and observer-triggered stop were not reached.");
                if ((await queuedStop).State != PerfRecordingState.Off ||
                    recorder.LastEndResult is not { Closed: true, Complete: true, Fault: PerfFaultCode.None, Reason: PerfEndReason.Toggle } ended ||
                    failures.Values.Any(error => Contains(ended.Error, error)) || Contains(ended.Error, endFailure) ||
                    failures.Keys.Any(state => !observed.Contains(state)) || observed.Contains(PerfRecordingState.Faulted) ||
                    !ReferenceEquals(observedEnd, ended))
                    throw new InvalidOperationException("Observation failure changed the recording result, skipped a later observer or blocked the queued stop. Result=" +
                        recorder.LastEndResult + "; assemblyLocation=" + typeof(PerfManifest).Assembly.Location + "; missingReasons=" +
                        (recorder.LastEndResult is { Closed: true } rejected
                            ? JsonNode.Parse(File.ReadAllText(Path.Combine(root, rejected.SessionId, "manifest.json")))?["missingReasons"]?.ToJsonString()
                            : null));
                using (var released = new FileStream(Path.Combine(root, ended.SessionId, ".lock"),
                    FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None)) { }
                var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ended.SessionId, "manifest.json")))!;
                if (!manifest["complete"]!.GetValue<bool>() || manifest["recordedSeconds"]!.GetValue<double>() != ended.RecordedSeconds ||
                    recorder.Snapshot.RecordedSeconds != ended.RecordedSeconds ||
                    diagnostics.Errors.Any(error => failures.Values.Any(failure => Contains(error, failure)) || Contains(error, endFailure)))
                    throw new InvalidOperationException("Observation failure changed file completeness, duration or recording diagnostics.");
                Exception? received = null;
                try { await recorder.DrainForExitAsync(); }
                catch (AggregateException error) { received = error; }
                var receivedFailures = (received as AggregateException)?.Flatten().InnerExceptions;
                if (receivedFailures is null || receivedFailures.Count != failures.Count + 1 ||
                    failures.Values.Any(error => !receivedFailures.Contains(error)) || !receivedFailures.Contains(endFailure) ||
                    !ReferenceEquals(recorder.LastEndResult, ended))
                    throw new InvalidOperationException("Exit lost the original observer failure or changed the recording result.");
                await recorder.DrainForExitAsync();
            }
            finally
            {
                try { await recorder.DrainForExitAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing); }
                finally { camera.Free(); walk.Free(); stream.Free(); }
            }
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ProducerStartupFailureDrainsThroughTheRealRecorderWithFaultingSubscriber()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            using var frames = new FrameStatistics(((SceneTree)Engine.GetMainLoop()).Root);
            var diagnostics = new TestDiagnosticLog();
            var recorder = new PerformanceRecordingService(frames, settings, diagnostics, new TestInputRouter(), new RequiredResources(), quality);
            var observer = new InvalidOperationException();
            var endObserver = new IOException();
            bool faultObserved = false;
            PerfSessionEndResult? observedEnd = null;
            recorder.StateChanged += value => { if (value.State == PerfRecordingState.Faulted) throw observer; };
            recorder.StateChanged += value => { if (value.State == PerfRecordingState.Faulted) faultObserved = true; };
            recorder.SessionEnded += _ => throw endObserver;
            recorder.SessionEnded += value => observedEnd = value;
            try
            {
                // The real startup owns an open writer before it rejects the missing viewport.
                var opened = await recorder.InitializeAsync(new BootRunOptions { PerfOutputDirectory = root });
                if (opened.Fault != PerfFaultCode.ProducerFailed)
                    throw new InvalidOperationException("The real missing-viewport producer failure was not reached.");
                Exception? received = null;
                try { await recorder.DrainForExitAsync(); }
                catch (AggregateException error) { received = error; }
                var ended = recorder.LastEndResult ?? throw new InvalidOperationException("The failed producer did not reach its recorder owner.");
                var failures = (received as AggregateException)?.Flatten().InnerExceptions;
                if (!ended.Closed || ended.Complete || ended.Fault != PerfFaultCode.ProducerFailed ||
                    ended.Error is not InvalidOperationException || Contains(ended.Error, observer) || Contains(ended.Error, endObserver) ||
                    failures is null || failures.Count != 2 || !failures.Contains(observer) || !failures.Contains(endObserver) ||
                    !faultObserved || !ReferenceEquals(observedEnd, ended))
                    throw new InvalidOperationException("Producer and observation failures were mixed or a later observer was skipped.");
                await recorder.DrainForExitAsync();
                using var released = new FileStream(Path.Combine(root, ended.SessionId, ".lock"),
                    FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
                if (!File.Exists(Path.Combine(root, ended.SessionId, "manifest.json")) || diagnostics.Errors.Count != 1 ||
                    !ReferenceEquals(diagnostics.Errors[0], ended.Error) || !ReferenceEquals(recorder.LastEndResult, ended))
                    throw new InvalidOperationException("The real failed session did not close and report exactly once.");
            }
            finally { await recorder.DrainForExitAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing); }
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task WriterSerializationFailureRetainsItsCauseAndClosesTheRealSession()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
        var original = new IOException();
        var dictionary = new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
        var session = new PerfRecordingSession(root, PerfOrigin.Ai, DateTimeOffset.UnixEpoch, dictionary, PerformanceTestSupport.SessionManifest());
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The real writer did not open.");
            session.Begin(new(0, 0, 0));
            if (!await session.StartedWriting) throw new InvalidOperationException("The real writer did not start.");
            if (!session.TryEvent(new("failure.fixture", null, 1, new UnserializablePayload(original))))
                throw new InvalidOperationException("The failure payload was not accepted by the writer.");
            session.Stop(PerfEndReason.Failure, 2);
            var ended = await session.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            if (!ended.Closed || ended.Complete || ended.Fault != PerfFaultCode.WriteFailed || !Contains(ended.Error, original))
                throw new InvalidOperationException("Writer serialization lost its original cause or failed to close.");
            using var released = new FileStream(Path.Combine(root, ended.SessionId, ".lock"), FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
        }
        finally { session.Stop(PerfEndReason.Exit, 2); await session.Completion; Directory.Delete(root, true); }
    }

    private sealed class UnserializablePayload(Exception failure)
    {
        public string Value => throw failure;
    }

    private static void BindRecordingOwners(IQualityService quality, Campus.Camera.ObserverCamera camera,
        Campus.Camera.Walk.WalkController walk, IStreamStatistics stream)
    {
        KernelServices.Services.Add<IRuleset>(new TestRuleset());
        KernelServices.Services.Add<IAppFlow>(new TestAppFlow());
        KernelServices.Services.Add<IMapService>(new TestMapService());
        KernelServices.Services.Add<IMapRuntime>(new FlowRuntimeTestStub(() => false));
        KernelServices.Services.Add<IWorldState>(new TestWorldState(new TestWeatherProcess(), new TestRuleset()));
        KernelServices.Services.Add<IObserverState>(camera);
        KernelServices.Services.Add<ICameraRig>(camera);
        KernelServices.Services.Add<IWalkController>(walk);
        KernelServices.Services.Add<IMediumState>(new Campus.Kernel.Stubs.MediumStateStub());
        KernelServices.Services.Add<IUiSurfaceState>(new Campus.Hud.HudSurfaceState());
        KernelServices.Services.Add<IQualityService>(quality);
        KernelServices.Services.Add<ISaveService>(new TestSaveService());
        KernelServices.Services.Add<IAppLoadingPresentation>(new TestAppLoadingPresentation());
        KernelServices.Services.Add<IStreamStatistics>(stream);
        KernelServices.Services.Add<IBakeService>(new StreamLifecycleTestSupport.StreamBake(default));
    }

    private static bool Contains(Exception? retained, Exception original) => ReferenceEquals(retained, original) ||
        retained is AggregateException aggregate && aggregate.InnerExceptions.Any(error => Contains(error, original)) ||
        retained?.InnerException is { } inner && Contains(inner, original);
}
