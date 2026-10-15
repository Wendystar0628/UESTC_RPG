using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Adaptive;
using Campus.Kernel.Quality.Settings;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class QualityApplicationTests
{
    [TestCase]
    public async Task QualityTimeoutRestoresStateReleasesOwnVeilAndDoesNotSave() =>
        await VerifyFailedApplication("timeout", failDispatch: false, takeover: false, failRollback: false);

    [TestCase]
    public async Task QualityDispatchFailureRestoresStateAndReleasesOwnVeil() =>
        await VerifyFailedApplication("dispatch", failDispatch: true, takeover: false, failRollback: false);

    [TestCase]
    public async Task QualityFailureNeverRevealsAnotherFlowsVeil() =>
        await VerifyFailedApplication("takeover", failDispatch: false, takeover: true, failRollback: false);

    [TestCase]
    public async Task RollbackConsumerFailureStillReleasesOwnVeilAndReportsBothErrors() =>
        await VerifyFailedApplication("rollback", failDispatch: true, takeover: false, failRollback: true);

    private static async Task VerifyFailedApplication(string name, bool failDispatch, bool takeover, bool failRollback)
    {
        var runtime = new FakeRuntime();
        var baseline = new QualityProfile(QualityPreset.Fine,
            new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 1000 }, true);
        var store = new FileStoreSpy(name, new(baseline, DeviceTier.MinSpec, runtime.Device.CacheKey(1920, 1080), true));
        var service = new QualityService(Catalog(), runtime, store);
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        runtime.Booting = false;
        var before = service.Effective;
        var beforeProfile = service.Requested;
        var beforeHash = store.Hash;
        runtime.FailDispatch = failDispatch;
        runtime.FailRollback = failRollback;
        runtime.FailReadiness = !failDispatch;
        runtime.Takeover = takeover;
        var changes = new List<QualityChange>();
        service.Changed += changes.Add;
        var overrides = beforeProfile.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        overrides[QualityKnob.AntiAliasing] = 1; // UnderVeil, deliberately not a 15-second-confirmation setting.
        overrides[QualityKnob.LensFlare] = 0;
        overrides[QualityKnob.GraphicsApi] = 1;
        var pending = service.Apply(new(beforeProfile.Preset, overrides, beforeProfile.Adaptive), ApplyOrigin.User);
        service.Tick(.25);
        var failed = false;
        try { await pending; }
        catch (Exception error) when (error is TimeoutException or InvalidOperationException or AggregateException) { failed = true; }
        service.Tick(0); // Observe the fault held by the scheduler as well as the caller's task.
        if (!failed || runtime.Errors.Count == 0) throw new InvalidOperationException("Failed application was not observable.");
        foreach (var knob in QualityCatalog.All)
        {
            if (service.Effective[knob] != before[knob] || service.Effective.Requested(knob) != before.Requested(knob) ||
                service.Effective.Locked(knob) != before.Locked(knob) || runtime.EngineView![knob] != before[knob])
                throw new InvalidOperationException($"Failed quality application was not rolled back: {knob}");
        }
        if (service.Requested.Preset != beforeProfile.Preset || service.Requested.Adaptive != beforeProfile.Adaptive ||
            service.Requested.Overrides.Count != beforeProfile.Overrides.Count || !changes.Any(change => change.Origin == ApplyOrigin.Revert))
            throw new InvalidOperationException("Requested profile or rollback notification was lost.");
        if (store.SaveCount != 0 || store.Hash != beforeHash) throw new InvalidOperationException("Failed application reached persistent storage.");
        if (takeover)
        {
            if (runtime.RevealCount != 0 || runtime.Reason != VeilReason.MapTravel)
                throw new InvalidOperationException("Recovery revealed another flow's veil.");
        }
        else if (runtime.RevealCount != 1 || runtime.OwnsQualityVeil)
            throw new InvalidOperationException("Recovery left its quality veil covering the player.");
    }

    [TestCase]
    public async Task ForcedTierAndCliProfileStayTemporaryWhileUserDeltaIsSaved()
    {
        var (service, runtime, store) = await CliSession("cli-user");
        var beforeHash = store.Hash;
        if (store.SaveCount != 0) throw new InvalidOperationException("CLI startup wrote settings.");
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.LensFlare] = 0;
        await ApplyUser(service, new(service.Requested.Preset, next, service.Requested.Adaptive));
        var saved = store.Read()!;
        var values = Catalog().Resolve(saved.Profile, saved.Profile.Preset);
        if (store.SaveCount != 1 || store.Hash == beforeHash || saved.Tier != DeviceTier.MinSpec ||
            saved.DeviceKey != runtime.Device.CacheKey(1920, 1080) || saved.Profile.Preset != QualityPreset.Balanced ||
            values[(int)QualityKnob.ShadowDistance] != 1000 || values[(int)QualityKnob.LensFlare] != 0 || !saved.Profile.Adaptive)
            throw new InvalidOperationException("User delta was dropped or a CLI override leaked into the saved profile/cache.");
        if (service.Tier != DeviceTier.DgpuHigh || service.Requested.Preset != QualityPreset.Ultra ||
            service.Effective[QualityKnob.ShadowDistance] != 600 || service.Requested.Adaptive)
            throw new InvalidOperationException("Saving a user edit removed the live CLI overlay.");
    }

    [TestCase]
    public async Task ConfirmDisplayPersistsOnlyTheUsersDisplayDelta()
    {
        var (service, _, store) = await CliSession("cli-confirm");
        var beforeHash = store.Hash;
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.RenderScale] = .5f;
        var applied = await ApplyUser(service, new(service.Requested.Preset, next, service.Requested.Adaptive));
        if (!applied.AwaitingConfirm || store.SaveCount != 0 || store.Hash != beforeHash)
            throw new InvalidOperationException("Unconfirmed display change was saved.");
        service.ConfirmDisplay();
        await service.Initialize(BootPhase.BeforeReveal, Context()); // Drain the existing pending-write task.
        var saved = store.Read()!;
        var values = Catalog().Resolve(saved.Profile, saved.Profile.Preset);
        if (store.SaveCount != 1 || store.Hash == beforeHash || saved.Profile.Preset != QualityPreset.Balanced ||
            saved.Tier != DeviceTier.MinSpec || values[(int)QualityKnob.RenderScale] != .5f ||
            values[(int)QualityKnob.ShadowDistance] != 1000 || !saved.Profile.Adaptive)
            throw new InvalidOperationException("Display confirmation persisted unrelated CLI settings.");
    }

    [TestCase]
    public async Task DisplayTimeoutRestoresCliPreviewWithoutPersistingIt()
    {
        var (service, _, store) = await CliSession("cli-revert");
        var beforeHash = store.Hash;
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.RenderScale] = .5f;
        await ApplyUser(service, new(service.Requested.Preset, next, service.Requested.Adaptive));
        service.Tick(16);
        service.Tick(0);
        if (service.Effective[QualityKnob.RenderScale] != .67f || store.SaveCount != 0 || store.Hash != beforeHash)
            throw new InvalidOperationException("Timeout leaked the CLI display baseline or failed to restore it.");
    }

    [TestCase]
    public async Task CleanPresetSelectionPersistsTheChoiceWithoutForcedDeviceOrAdaptive()
    {
        var (service, _, store) = await CliSession("cli-preset");
        await ApplyUser(service, QualityCatalog.Profile(QualityPreset.Ultra, adaptive: false));
        service.ConfirmDisplay();
        await service.Initialize(BootPhase.BeforeReveal, Context());
        var saved = store.Read()!;
        if (store.SaveCount == 0 || saved.Profile.Preset != QualityPreset.Ultra || saved.Tier != DeviceTier.MinSpec || !saved.Profile.Adaptive)
            throw new InvalidOperationException("Explicit preset selection was dropped or carried unrelated CLI metadata.");
    }

    [TestCase]
    public async Task ForcedTierWithoutRealCacheCannotSeedAValidDetectionCache()
    {
        var runtime = new FakeRuntime();
        var store = new FileStoreSpy("cli-no-cache", null);
        var service = new QualityService(Catalog(), runtime, store);
        await service.Initialize(BootPhase.AfterMapWarm, Context(forcedTier: true));
        runtime.Booting = false;
        if (store.SaveCount != 0) throw new InvalidOperationException("Forced tier created a hardware cache.");
        await ApplyUser(service, new(QualityPreset.Ultra, new Dictionary<QualityKnob, float> { [QualityKnob.LensFlare] = 0 }, true));
        var saved = store.Read()!;
        if (store.SaveCount != 1 || saved.DeviceKey.Length != 0 || saved.Tier == DeviceTier.DgpuHigh)
            throw new InvalidOperationException("A forced tier was stored as a real detection result.");
    }

    private static async Task<(QualityService, FakeRuntime, FileStoreSpy)> CliSession(string name)
    {
        var runtime = new FakeRuntime();
        var stored = new StoredGraphics(QualityCatalog.Profile(QualityPreset.Balanced), DeviceTier.MinSpec,
            runtime.Device.CacheKey(1920, 1080), true);
        var store = new FileStoreSpy(name, stored);
        var service = new QualityService(Catalog(), runtime, store);
        var originalHash = store.Hash;
        await service.Initialize(BootPhase.AfterMapWarm, Context(forcedTier: true));
        await service.Apply(new(QualityPreset.Ultra, new Dictionary<QualityKnob, float>
        { [QualityKnob.ShadowDistance] = 600, [QualityKnob.RenderScale] = .67f }, false), ApplyOrigin.CommandLine);
        if (store.Hash != originalHash || store.SaveCount != 0) throw new InvalidOperationException("CLI application touched the file.");
        runtime.Booting = false;
        return (service, runtime, store);
    }

    private static async Task<ApplyResult> ApplyUser(QualityService service, QualityProfile profile)
    {
        service.Tick(0);
        var pending = service.Apply(profile, ApplyOrigin.User);
        service.Tick(.25);
        var result = await pending;
        service.Tick(0);
        return result;
    }

    private static BootContext Context(bool forcedTier = false) => new(
        forcedTier ? new Dictionary<string, string> { ["tier"] = "dgpu-high" } : new Dictionary<string, string>(),
        forcedTier ? new HashSet<string>(["tier"], StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal),
        Path.Combine(RepositoryRoot(), "output", "tests", "quality"), "quality-tests", false);

    private sealed class FakeRuntime : IQualityRuntime
    {
        public GraphicsDevice Device { get; } = new("test-uma", "test-vendor", "test-driver", 256UL << 20, 4UL << 30, true, true);
        public bool Booting { get; set; } = true;
        public bool Transitioning => false;
        public bool OwnsQualityVeil => Reason == VeilReason.Quality;
        public VeilReason? Reason { get; private set; }
        public float RefreshRate => 60;
        public Godot.Vector2I OutputSize => new(1920, 1080);
        public Godot.Vector2I ScreenSize => OutputSize;
        public bool FailDispatch { get; set; }
        public bool FailRollback { get; set; }
        public bool FailReadiness { get; set; }
        public bool Takeover { get; set; }
        public int RevealCount { get; private set; }
        public QualityView? EngineView { get; private set; }
        public List<string> Errors { get; } = [];
        public Task<double> NextFrame() => Task.FromResult(.1);
        public GraphicsDevice ReadDevice() => Device;
        public Task<DeviceTier> Sample(GraphicsDevice device) => Task.FromResult(DeviceTier.MinSpec);
        public Task Cover() { Reason = VeilReason.Quality; return Task.CompletedTask; }
        public Task Reveal() { RevealCount++; Reason = null; return Task.CompletedTask; }
        public Task WaitForDestination()
        {
            if (Takeover) Reason = VeilReason.MapTravel;
            return FailReadiness ? Task.FromException(new TimeoutException("Fake destination exceeded 1.2 seconds.")) : Task.CompletedTask;
        }
        public void Dispatch(QualityView view, in QualityChange change)
        {
            EngineView = view; // Model a consumer that mutates before it fails.
            if (change.Origin == ApplyOrigin.Revert && FailRollback) throw new InvalidOperationException("Fake rollback consumer failed.");
            if (FailDispatch) { FailDispatch = false; throw new InvalidOperationException("Fake apply consumer failed."); }
        }
        public void ReportError(string message) => Errors.Add(message);
        public void ReportInfo(string message) { }
    }

    private sealed class FileStoreSpy : IGraphicsSettingsStore
    {
        private static int _nextId;
        private readonly string _path;
        public int SaveCount { get; private set; }
        public string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_path)));
        public FileStoreSpy(string scenario, StoredGraphics? baseline)
        {
            var directory = Path.Combine(RepositoryRoot(), "output", "tests", "quality", $"{scenario}-{Environment.ProcessId}-{Interlocked.Increment(ref _nextId)}");
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "graphics-spy.json");
            File.WriteAllBytes(_path, baseline is null ? [] : JsonSerializer.SerializeToUtf8Bytes(baseline));
        }
        public StoredGraphics? Read()
        {
            var bytes = File.ReadAllBytes(_path);
            return bytes.Length == 0 ? null : JsonSerializer.Deserialize<StoredGraphics>(bytes);
        }
        public Task Write(QualityProfile profile, GraphicsDevice device, DeviceTier tier, string key, bool userSelected)
        {
            File.WriteAllBytes(_path, JsonSerializer.SerializeToUtf8Bytes(new StoredGraphics(profile, tier, key, userSelected)));
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private static QualityCatalog Catalog() => new(File.ReadAllText(Path.Combine(RepositoryRoot(), "game", "data", "quality-presets.json")));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "game", "data", "quality-presets.json");
            if (File.Exists(path)) return directory.FullName;
        }
        throw new FileNotFoundException("Cannot locate the production quality preset table.");
    }
}
