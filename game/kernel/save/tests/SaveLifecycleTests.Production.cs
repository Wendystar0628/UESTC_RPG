using System.Globalization;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.World;
using Campus.Systems.Weather;
using GdUnit4;

namespace Campus.Kernel.Save.Tests;

public sealed partial class SaveLifecycleTests
{
    [TestCase]
    public async Task ProductionFlowSeasonAndWeatherTransitionsShareOneSavedEndpointAndResumeBitwise()
    {
        using var original = new ProductionFixture();
        await original.Start(246);
        var liveHours = original.World.WorldHours;
        original.Moments.BeforeCapture = () =>
        {
            // Emulate HUD controls after World.Tick in the exact frame in which Save captures.
            original.World.SetSeason(Season.Winter);
            original.World.FlowTo(21, 60);
            original.World.SetWeather(WeatherKind.Snow, .7f);
            Check(!original.World.IsSettled && original.Weather.Transitioning, "The test did not enter real concurrent transitions.");
        };
        var result = await original.Pump(original.Save.SaveNow(SaveReason.Test));
        Check(result.Status == SaveStatus.Saved, "Production forced capture failed: " + result.Error);
        Check(original.Moments.Captures == 1 && original.Moments.PurityChecked,
            "The production capture did not use one immutable moment without live mutation.");
        Check(original.World.WorldHours == liveHours, "Taking the forced snapshot advanced the live world.");

        var saved = original.ReadSaved();
        var expectedWorld = original.WorldDto(saved);
        var expectedWeather = original.WeatherDto(saved);
        Check(expectedWorld.WorldHours == expectedWeather.LeftAtHours && expectedWorld.ClockOffset == expectedWeather.ClockOffset &&
            expectedWorld.SeasonTarget == expectedWeather.Season && expectedWorld.FlowRate == 0,
            "Production world/weather were not captured at the same FlowTo endpoint.");
        Check(expectedWorld.SeasonTarget == Season.Winter && expectedWeather.Selection.Kind == WeatherKind.Snow &&
            expectedWeather.Selection.Mode == WeatherMode.Manual && expectedWeather.Selection.Intensity == .7f,
            "The season or manual weather transition endpoint was lost.");
        Check(Math.Abs(saved.Meta.Clock - 21) < 1e-12 && saved.Meta.WorldHours == expectedWorld.WorldHours,
            "The metadata describes a different endpoint from the production sections.");

        var loaded = await original.Pump(original.Save.Load(new("s1")));
        Check(loaded.Status == LoadStatus.Loaded, "Production round trip failed: " + loaded.Error);
        Check(original.World.Capture(default) == expectedWorld, "World fields changed in a production round trip.");
        original.EqualWeather(original.Weather.Capture(original.Maps.Current), expectedWeather);
        Check(original.World.IsSettled && !original.Weather.Transitioning, "A forced endpoint resumed its discarded transition.");

        using var independent = new ProductionFixture();
        await independent.Start(999);
        var external = await independent.Pump(independent.Save.LoadFile(original.LatestPath));
        Check(external.Status == LoadStatus.Loaded, "Independent production restore failed: " + external.Error);
        original.World.SetFlow(60); independent.World.SetFlow(60);
        for (var i = 0; i < 180; i++)
        {
            var dt = (i % 3) switch { 0 => .016, 1 => .033, _ => .1 };
            original.World.Tick(dt); independent.World.Tick(dt);
            Check(original.World.Capture(default) == independent.World.Capture(default), "Production world continuation diverged.");
            Check(original.Weather.Selection == independent.Weather.Selection && original.Weather.Now == independent.Weather.Now &&
                original.Weather.Ahead.SequenceEqual(independent.Weather.Ahead), "Production weather continuation diverged.");
            original.EqualWeather(original.Weather.Capture(original.Maps.Current), independent.Weather.Capture(independent.Maps.Current));
        }
    }

    [TestCase]
    public async Task ProductionCoreCorruptionRecoversOlderGenerationAndSemanticRestoreFailureRemainsIsolated()
    {
        using var fixture = new ProductionFixture();
        await fixture.Start(801);
        Check((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved, "Could not save initial production state.");
        var initial = fixture.ReadSaved();
        fixture.Moments.BeforeCapture = () => fixture.World.FlowTo(20, 60);
        Check((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Saved, "Could not save transitioned production state.");

        var damaged = fixture.ReadSaved();
        var corruptWeather = fixture.WeatherDto(damaged) with { LeftAtHours = damaged.Meta.WorldHours - .125 };
        var sections = damaged.Sections.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        using (var envelope = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(envelope))
            {
                writer.WriteStartObject(); writer.WriteNumber("v", 1); writer.WritePropertyName("data");
                JsonSerializer.Serialize(writer, corruptWeather, fixture.WeatherSection.TypeInfo);
                writer.WriteEndObject();
            }
            sections["map:" + fixture.Maps.Current.Value + "/weather"] = new(1, envelope.ToArray());
        }
        // The container hash is deliberately valid. Only the production core-state coherence check can reject it.
        File.WriteAllBytes(fixture.LatestPath, SaveContainer.Encode(damaged with { Sections = sections }));
        var recovered = await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(recovered.Status == LoadStatus.Recovered && recovered.Gen == initial.Meta.Gen, "Semantic core corruption did not fall back within its line.");
        Check(fixture.World.Capture(default) == fixture.WorldDto(initial), "Recovery did not restore the old production world.");
        fixture.EqualWeather(fixture.Weather.Capture(fixture.Maps.Current), fixture.WeatherDto(initial));
        Check(!fixture.Restore.RestoreFaulted, "Pre-mutation corruption incorrectly left the valid fallback faulted.");

        fixture.Profiles.FailLoads = true;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(failed.Status == LoadStatus.Failed && fixture.Restore.RestoreFaulted && fixture.Camera.LockCount == 1,
            "A real weather-section restore failure exposed the partially restored world.");
        Check(fixture.Save.State.ErrorKey == "save.error.restore_world", "The actual restore failure has no UI-visible error.");
        var faultHours = fixture.World.WorldHours;
        var faultAnim = fixture.World.AnimTime;
        var faultInstance = fixture.World.Instance;
        for (var i = 0; i < 5; i++) fixture.World.Tick(.1);
        Check(fixture.World.WorldHours == faultHours && fixture.World.AnimTime == faultAnim && fixture.World.Instance == faultInstance,
            "The faulted production world advanced or changed instance.");
        var controlRejected = false;
        try { fixture.World.Advance(1); } catch (InvalidOperationException) { controlRejected = true; }
        Check(controlRejected, "Production world controls can mutate the failed transaction.");
        Check((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Failed,
            "The partially restored production state was saved.");
        Check(Directory.GetFiles(Path.Combine(fixture.Directory, "s1"), "g*.sav").Length == 1,
            "A generation was written while the production world was faulted.");

        fixture.Profiles.FailLoads = false;
        Check((await fixture.Pump(fixture.Save.Load(new("s1")))).Status == LoadStatus.Loaded &&
            !fixture.Restore.RestoreFaulted && fixture.Camera.LockCount == 0 && fixture.Save.State.ErrorKey is null,
            "A complete production Load did not recover and release the fault.");
        fixture.Profiles.FailLoads = true;
        Check((await fixture.Pump(fixture.Save.Load(new("s1")))).Status == LoadStatus.Failed, "The second injected provider failure was not reached.");
        fixture.Profiles.FailLoads = false;
        Check((await fixture.Pump(fixture.Save.NewGame(new("s1"), 802, new WorldPreset(6.25, Season.Winter, WeatherKind.Snow, .4f)))).Status == LoadStatus.Loaded &&
            !fixture.Restore.RestoreFaulted && fixture.Weather.Seed == 802 && fixture.Camera.LockCount == 0 &&
            fixture.World.SeasonTarget == Season.Winter && fixture.Weather.Selection.Kind == WeatherKind.Snow,
            "A complete production NewGame did not rebuild the failed world.");
        var newLine = await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        Check(newLine.Status == SaveStatus.Saved && newLine.Gen == 3 && fixture.Save.Slots[0].Meta?.Line == 3,
            "Recovered new-game save lost the quarantined generation high-water mark.");
    }

    private sealed class ProductionFixture : IDisposable
    {
        private static long _sequence;
        private bool _inSaveFrame;
        private readonly SaveSections _sections;
        private readonly FakeRules _rules = new();
        public string Directory { get; }
        public SaveService Save { get; } = new();
        public WorldStateService World { get; }
        public WeatherProcess Weather { get; }
        public IWorldRestore Restore => World;
        public ProductionMaps Maps { get; }
        public FakeCamera Camera { get; }
        public ProductionProfiles Profiles { get; }
        public WeatherMapSection WeatherSection { get; }
        public ProductionMomentProbe Moments { get; }
        public string LatestPath => Path.Combine(Directory, "s1", "g" + Save.Slots[0].Meta!.Gen.ToString("D6", CultureInfo.InvariantCulture) + ".sav");

        public ProductionFixture()
        {
            var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? throw new InvalidOperationException("UESTC_REPOSITORY required.");
            Directory = Path.Combine(root, "output", "save-tests", "production-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _sequence));
            Camera = new(() => _inSaveFrame, []);
            Maps = new(Camera);
            Profiles = new(WeatherProfile.Parse(File.ReadAllText(Path.Combine(root, "game", "data", "weather", "chengdu", "profile.json"))),
                Maps.Maps[0].World.WeatherProfile);
            Weather = new(Profiles);
            World = new(Weather);
            World.OnMapLoaded(Maps.Context);
            Maps.Arrival = () => { World.OnMapUnloading(Maps.Current); World.OnMapLoaded(Maps.Context); };
            WeatherSection = new(Weather, Maps, Profiles);
            Moments = new(World, ReadLiveState);
            _sections = new([
                new(new("world") { Owner = "WP-34", Phase = RestorePhase.World, Order = 10 }, World),
                new(new("weather") { Owner = "WP-45a", Phase = RestorePhase.World, Order = 20 }, new WeatherSeedSection(Weather)),
                new(new("weather") { Owner = "WP-45a", Scope = SaveScope.PerMap, Phase = RestorePhase.World, Order = 21 }, WeatherSection),
                new(new("view") { Owner = "WP-50", Phase = RestorePhase.Pose, Order = 60 }, Camera)]);
        }

        public async Task Start(uint seed)
        {
            var bindings = new SaveRuntimeBindings(World, Maps, Camera, Camera, _rules, new FakeAtlas(Maps), new FakeVeil(),
                () => AppState.Playing, _ => { }, Moments, (_, _, _) => Task.CompletedTask,
                () => Task.FromResult<byte[]?>(null), () => { Moments.AssertPureCapture(); return 1792082400; });
            await Pump(Save.Initialize(new(Directory, "production-integration", true, null, null, seed), bindings, _sections));
            var result = await Pump(Save.NewGame(new("s1"), seed));
            Check(result.Status == LoadStatus.Loaded, "Production fixture could not create a world: " + result.Error);
        }

        private LiveProductionState ReadLiveState() => new(World.WorldHours, World.AnimTime, World.SeasonPos, World.ClockOffset,
            World.Flow, Weather.Selection, Weather.Now, JsonSerializer.Serialize(Weather.Capture(Maps.Current), WeatherSection.TypeInfo), Weather.Ahead.ToArray());
        public SaveDocument ReadSaved()
        {
            using var reader = new SaveRepository(Directory, false, _sections);
            return reader.Read(LatestPath);
        }
        public WorldSaveV1 WorldDto(SaveDocument document)
        {
            using var json = SaveContainer.ParseJson(document.Sections["world"].Envelope);
            return JsonSerializer.Deserialize(json.RootElement.GetProperty("data"), World.TypeInfo)!;
        }
        public WeatherProcessState WeatherDto(SaveDocument document)
        {
            using var json = SaveContainer.ParseJson(document.Sections["map:" + Maps.Current.Value + "/weather"].Envelope);
            return JsonSerializer.Deserialize(json.RootElement.GetProperty("data"), WeatherSection.TypeInfo)!;
        }
        public void EqualWeather(WeatherProcessState a, WeatherProcessState b) => Check(
            JsonSerializer.Serialize(a, WeatherSection.TypeInfo) == JsonSerializer.Serialize(b, WeatherSection.TypeInfo),
            "Production weather DTO fields or queue counters differ after restore.");
        public async Task Pump(Task task)
        {
            for (var i = 0; i < 5000 && !task.IsCompleted; i++)
            {
                // Zero delta preserves the exact endpoint while still exercising the real pending/fault Tick guards.
                World.Tick(0);
                _inSaveFrame = true;
                try { Save.Tick(.01); } finally { _inSaveFrame = false; }
                await Task.Delay(1);
            }
            Check(task.IsCompleted, "Production save lifecycle did not finish."); await task;
        }
        public async Task<T> Pump<T>(Task<T> task) { await Pump((Task)task); return await task; }
        public void Dispose() => Save.Dispose();
    }

    private sealed record LiveProductionState(double Hours, double Anim, double SeasonPosition, double Offset, Campus.Contracts.Flow Flow,
        WeatherSel Selection, WeatherVec Vector, string WeatherDto, IReadOnlyList<WeatherSpan> Ahead);

    /// <summary>Instrumentation only: all moments and transaction behavior delegate to the actual world service.</summary>
    private sealed class ProductionMomentProbe(IWorldRestore actual, Func<LiveProductionState> readLive) : IWorldRestore
    {
        private LiveProductionState? _before;
        public Action? BeforeCapture { get; set; }
        public int Captures { get; private set; }
        public bool PurityChecked { get; private set; }
        public bool RestoreFaulted => actual.RestoreFaulted;
        public WorldSaveMoment GetSaveMoment()
        {
            var controls = BeforeCapture; BeforeCapture = null; controls?.Invoke();
            _before = readLive(); Captures++;
            return actual.GetSaveMoment();
        }
        public void AbortRestore() => actual.AbortRestore();
        public void CompleteRestore() => actual.CompleteRestore();
        public void AssertPureCapture()
        {
            if (_before is not { } before) return;
            var after = readLive(); _before = null;
            Check(before.Hours == after.Hours && before.Anim == after.Anim && before.SeasonPosition == after.SeasonPosition &&
                before.Offset == after.Offset && before.Flow == after.Flow && before.Selection == after.Selection && before.Vector == after.Vector &&
                before.WeatherDto == after.WeatherDto && before.Ahead.SequenceEqual(after.Ahead),
                "A production save capture mutated the live world/weather state or generated live queue entries.");
            PurityChecked = true;
        }
    }

    private sealed class ProductionProfiles(WeatherProfile profile, string name) : IWeatherProfileSource
    {
        public bool FailLoads { get; set; }
        public WeatherProfile Load(string requested)
        {
            if (FailLoads) throw new InvalidDataException("Injected weather profile failure inside the real WeatherMapSection.Restore.");
            if (requested != name) throw new InvalidDataException("Unknown fixture weather profile.");
            return profile;
        }
    }

    private sealed class ProductionMaps(FakeCamera camera) : IMapService
    {
        private readonly FakeMaps _metadata = new([]);
        private readonly BuildInputSnapshot _sourceInputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>());
        public Action? Arrival { get; set; }
        public long Rev => _metadata.Rev;
        public MapId Default => _metadata.Default;
        public MapId Current => _metadata.Current;
        public IReadOnlyList<MapDescriptor> Maps => _metadata.Maps;
        public IReadOnlyList<MapVisit> Visits => _metadata.Visits;
        public IMapContext Context => new ProductionMapContext(Maps.Single(m => m.Id == Current), _sourceInputs);
        public IMapTransition? Transition => null;
        public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
        public IMapTransition Travel(MapTravelRequest request)
        {
            Check(request.Target == Current, "Integration fixture only contains its synthetic map.");
            if (request.Pose is { } pose) camera.SetPose(pose);
            Arrival?.Invoke();
            return new CompletedMap(request, Task.FromResult(MapTravelResult.Arrived));
        }
    }
    private sealed record ProductionMapContext(MapDescriptor Descriptor, BuildInputSnapshot SourceInputs) : IMapContext
    {
        public IWorldModel World => throw new NotSupportedException("World-model geometry is not consumed by these production time/weather services.");
        public string DataRoot => "res://test/";
    }
}
