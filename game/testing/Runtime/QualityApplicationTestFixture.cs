using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Settings;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

internal static class QualityApplicationTestFixture
{
    internal static async Task<(QualityService, FakeRuntime, FileStoreSpy)> CliSession(string name)
    {
        var runtime = new FakeRuntime();
        var store = new FileStoreSpy(name, QualityCatalog.Profile(QualityPreset.Balanced));
        var service = new QualityService(Catalog(), store);
        service.Bind(runtime);
        var originalHash = store.Hash;
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        await service.Apply(new(QualityPreset.Ultra, new Dictionary<QualityKnob, float>
        { [QualityKnob.ShadowDistance] = 600, [QualityKnob.RenderScale] = .67f }), ApplyOrigin.CommandLine);
        if (store.Hash != originalHash || store.SaveCount != 0) throw new InvalidOperationException("CLI application touched the file.");
        runtime.Booting = false;
        return (service, runtime, store);
    }

    internal static async Task<ApplyResult> ApplyUser(QualityService service, QualityProfile profile)
    {
        service.Tick(0);
        var pending = service.Apply(profile, ApplyOrigin.User);
        service.Tick(.25);
        var result = await pending;
        service.Tick(0);
        return result;
    }

    internal static BootContext Context() => new(new BootRunOptions(), Path.GetTempPath(), "quality-tests");

    internal sealed class FileStoreSpy : IGraphicsSettingsStore, IDisposable
    {
        private static int _nextId;
        private readonly string _path;
        private readonly string _directory;
        public int SaveCount { get; private set; }
        public TaskCompletionSource? HeldWrite { get; set; }
        public string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_path)));
        public FileStoreSpy(string scenario, QualityProfile? baseline)
        {
            _directory = Path.Combine(Path.GetTempPath(), $"uestc-rpg-quality-{scenario}-{Environment.ProcessId}-{Interlocked.Increment(ref _nextId)}");
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "graphics-spy.json");
            File.WriteAllBytes(_path, baseline is null ? [] : JsonSerializer.SerializeToUtf8Bytes(baseline));
        }
        public QualityProfile? Read()
        {
            var bytes = File.ReadAllBytes(_path);
            return bytes.Length == 0 ? null : JsonSerializer.Deserialize<QualityProfile>(bytes);
        }
        public Task Write(QualityProfile profile)
        {
            File.WriteAllBytes(_path, JsonSerializer.SerializeToUtf8Bytes(profile));
            SaveCount++;
            return HeldWrite?.Task ?? Task.CompletedTask;
        }
        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }

    internal static QualityCatalog Catalog() => new(File.ReadAllText(Path.Combine(RepositoryFixture.Root, "game", "data", "quality-presets.json")));

    internal sealed class FakeRuntime : IQualityRuntime
    {
        public bool Booting { get; set; } = true;
        public bool Transitioning { get; set; }
        public bool QualityLoadingActive { get; private set; }
        public bool OtherLoadingActive { get; private set; }
        public float RefreshRate => 60;
        public Godot.Vector2I OutputSize => new(1920, 1080);
        public Godot.Vector2I ScreenSize => OutputSize;
        public bool FailDispatch { get; set; }
        public bool FailRollback { get; set; }
        public bool FailReadiness { get; set; }
        public bool Takeover { get; set; }
        public int HideCount { get; private set; }
        public QualityView? EngineView { get; private set; }
        public List<string> Errors { get; } = [];
        public string? HoldAt { get; set; }
        public TaskCompletionSource<double> Held { get; } = new();
        public int HeldCalls { get; private set; }
        public int DispatchCount { get; private set; }
        // Deliberately ignore cancellation: the service must reject a boundary's late result itself.
        private Task Wait(string boundary)
        {
            if (HoldAt != boundary) return Task.CompletedTask;
            HeldCalls++;
            return Held.Task;
        }
        public Task<double> NextFrame(CancellationToken cancellationToken)
        {
            if (HoldAt != "frame") return Task.FromResult(.1);
            HeldCalls++;
            return Held.Task;
        }
        public IAppLoadingLease ShowLoading(CancellationToken cancellationToken)
        {
            QualityLoadingActive = true;
            return new QualityLease(this, Wait("loading"));
        }
        private sealed class QualityLease(FakeRuntime runtime, Task covered) : IAppLoadingLease
        {
            private bool _released;
            public Task Covered => covered;
            public void Report(int completed, int total) { }
            public Task Hide()
            {
                if (_released) return Task.CompletedTask;
                runtime.HideCount++;
                Release();
                return runtime.Wait("hide");
            }
            public ValueTask DisposeAsync() { Release(); return ValueTask.CompletedTask; }
            private void Release()
            {
                if (_released) return;
                _released = true;
                runtime.QualityLoadingActive = false;
            }
        }
        public Task WaitForDestination(CancellationToken cancellationToken)
        {
            if (Takeover) OtherLoadingActive = true;
            return FailReadiness ? Task.FromException(new TimeoutException("Fake destination exceeded 1.2 seconds.")) : Wait("destination");
        }
        public void Dispatch(QualityView view, in QualityChange change)
        {
            DispatchCount++;
            EngineView = view; // Model a consumer that mutates before it fails.
            if (FailDispatch) { FailDispatch = false; throw new InvalidOperationException("Fake apply consumer failed."); }
            if (FailRollback) throw new InvalidOperationException("Fake rollback consumer failed.");
        }
        public void ReportError(string message) => Errors.Add(message);
        public void ReportInfo(string message) { }
    }
}
