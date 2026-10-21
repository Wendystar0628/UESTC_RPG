using System.Reflection;
using Campus.Contracts;
using Campus.Audio.Core;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Audio;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Settings;
using Campus.Kernel.World;
using Campus.Style.Materials;
using Campus.Style.Palette;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class HudLifetimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void ThemeClockStopsObservingSettingsUntilItReenters()
    {
        var settings = new HudSettings(new SettingsStore());
        settings.SetTheme(UiThemeMode.Light);
        settings.SetReduceMotion(ReduceMotionMode.On);
        var world = new WorldStateService(new TestWeatherProcess());
        var motion = new MotionPolicy(settings);
        var baseline = Subscribers(settings, "Changed");
        var worldBaseline = Subscribers(world, "InstanceChanged");
        var clock = new UiThemeClock(world, settings, motion);
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        try
        {
            for (var cycle = 0; cycle < 3; cycle++)
            {
                parent.AddChild(clock);
                AssertThat(!clock.Dark && Subscribers(settings, "Changed") == baseline + 1).OverrideFailureMessage("Clock did not bind exactly once on entry.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline + 1).OverrideFailureMessage("Clock did not bind world notification exactly once on entry.").IsTrue();
                settings.SetTheme(UiThemeMode.Dark);
                AssertThat(clock.Dark).OverrideFailureMessage("Attached clock stopped responding to theme settings.").IsTrue();
                parent.RemoveChild(clock);
                var front = clock.Front;
                settings.SetTheme(UiThemeMode.Light);
                AssertThat(clock.Dark && clock.Front == front && Subscribers(settings, "Changed") == baseline).OverrideFailureMessage("Detached clock still observed settings or retained a service subscription.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline).OverrideFailureMessage("Detached clock retained a world observer.").IsTrue();
            }
        }
        finally { clock.Free(); }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task HudRootReentryRebuildsResidentsAndOwnsOnlyItsInputLayers()
    {
        var ruleset = TestRuleset.ForFlow();
        using var audio = new AudioRuntime();
        var flow = new TestAppFlow();
        var router = new Router();
        var settings = new HudSettings(new SettingsStore());
        var world = new WorldStateService(new TestWeatherProcess());
        var view = new TestWorldState(new TestWeatherProcess(), ruleset);
        var camera = new TestCameraRig(view);
        var model = new HudModel(world, camera, ruleset);
        var motion = new MotionPolicy(settings);
        var settingsBaseline = Subscribers(settings, "Changed");
        var worldBaseline = Subscribers(world, "InstanceChanged");
        camera.Mode = ViewMode.Walk;
        world.Freeze(13, Season.Summer, 0);
        AssertThat(model.Mode == camera.Mode && model.Clock == world.Clock && model.Season == world.SeasonTarget && model.Speed == camera.Speed)
            .OverrideFailureMessage("HUD readouts required a refresh before exposing the current source values.").IsTrue();
        camera.Mode = ViewMode.Fly;
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        HudRoot? hud = null;
        try
        {
            // The fixture supplies only explicit dependencies; it never constructs the production boot graph.
            var dependencies = new HudRootDependencies(
                Settings: settings,
                Model: model,
                Motion: motion,
                WorldControl: world,
                Flow: flow,
                Exit: new UnusedExit(),
                Audio: audio,
                PaletteCatalog: new PaletteRegistrySource(),
                ShaderMaterials: new ShaderMaterialSource(),
                ThemeClock: new UiThemeClock(world, settings, motion),
                InputRouter: router,
                ViewControl: view,
                Capture: new UnusedCapture(),
                Maps: new TestMapService(),
                WorldState: world,
                Saves: new UnusedSaveService(),
                PaletteProbe: new Probe(),
                Diagnostics: new DiagnosticLog());
            hud = new HudRoot();
            hud.Bind(dependencies);
            parent.AddChild(hud);
            var pages = hud.PageHost;
            var corners = hud.CornerHost;
            var residentCount = Residents(corners);
            AssertThat(residentCount > 0).OverrideFailureMessage("Playing HUD did not build its resident controls.").IsTrue();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                AssertThat(router.Layers == 3 && router.Observers == 1 && flow.Observers == 1).OverrideFailureMessage("HUD registered duplicate input layers or flow observers.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline + 2).OverrideFailureMessage("HUD root and theme clock did not bind world notifications exactly once.").IsTrue();
                var dialog = new Control();
                hud.DialogHost.AddChild(dialog);
                AssertThat(hud.PageHost.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Disabled)
                    .OverrideFailureMessage("A visible dialog did not disable the page host.").IsTrue();
                parent.RemoveChild(hud);
                AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0).OverrideFailureMessage("Tree exit retained input layers or service observers.").IsTrue();
                AssertThat(Subscribers(settings, "Changed") == settingsBaseline && Subscribers(world, "InstanceChanged") == worldBaseline).OverrideFailureMessage("Tree exit retained settings/world observers.").IsTrue();
                AssertThat(Residents(corners) == 0).OverrideFailureMessage("Queued residents remained parented and could reenter before deletion.").IsTrue();
                // 退树后释放上层内容，宿主观察必须已解除；重入时重新观察现存子件。
                hud.DialogHost.RemoveChild(dialog);
                dialog.Free();
                router.SetInterfaceMode(!router.InterfaceMode);
                flow.SetState(AppState.Boot);
                flow.SetState(AppState.Playing);
                parent.AddChild(hud);
                AssertThat(ReferenceEquals(pages, hud.PageHost) && Residents(corners) == residentCount).OverrideFailureMessage("Reentry duplicated the hosts or failed to rebuild residents.").IsTrue();
                AssertThat(hud.PageHost.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Inherited)
                    .OverrideFailureMessage("Reentry retained the detached dialog's focus block.").IsTrue();
            }
            // 直接带可见弹层释放根，覆盖退出时子节点信号与宿主销毁的顺序。
            hud.DialogHost.AddChild(new Control());
        }
        finally
        {
            try
            {
                hud?.Free();
                AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0).OverrideFailureMessage("Free retained HUD registrations.").IsTrue();
            }
            finally
            {
                // 保留同帧重入的检查；失败时也让退树 QueueFree 的四角走完实际释放。
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
    }

    private static int Subscribers(object source, string name) =>
        ((Delegate?)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source))?.GetInvocationList().Length ?? 0;

    private static int Residents(Node root) => root.GetChildren().Sum(child =>
        (child is SpeedBar or TimeCard or TimeStrip or WeatherStrip ? 1 : 0) + Residents(child));

    private sealed class Probe : IPaletteProbe
    {
        public void SampleHorizon(Season season, WeatherVec weather, Span<System.Numerics.Vector3> srgb24) =>
            srgb24.Fill(System.Numerics.Vector3.Zero);
    }

    private sealed class UnusedExit : IAppExit
    {
        public Task FinishQuit(int exitCode = 0) => throw new NotSupportedException();
    }

    private sealed class UnusedCapture : IGameCapture
    {
        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnusedSaveService : ISaveService
    {
        public bool Enabled => throw new NotSupportedException();
        public IReadOnlyList<SlotInfo> Slots => throw new NotSupportedException();
        public SlotId? Continue => throw new NotSupportedException();
        public SlotId? Active => throw new NotSupportedException();
        public bool Busy => throw new NotSupportedException();
        public SaveState State => throw new NotSupportedException();
        public bool HasPendingPose => throw new NotSupportedException();
        public event Action? SlotsChanged
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }
        public void Request(SaveReason reason) => throw new NotSupportedException();
        public Task<SaveResult> SaveNow(SaveReason reason) => throw new NotSupportedException();
        public Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full) => throw new NotSupportedException();
        public Task CompleteResume() => throw new NotSupportedException();
        public Task<LoadResult> LoadFile(string path) => throw new NotSupportedException();
        public Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) => throw new NotSupportedException();
        public Task<LoadResult> RestoreTitlePreview() => throw new NotSupportedException();
        public Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => throw new NotSupportedException();
        public byte[]? Thumbnail(SlotId slot) => throw new NotSupportedException();
    }

    private sealed class Router : IInputRouter
    {
        public int Layers { get; private set; }
        public bool InterfaceMode { get; private set; }
        public event Action<bool>? InterfaceModeChanged;
        public int Observers => InterfaceModeChanged?.GetInvocationList().Length ?? 0;
        public IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler)
        {
            Layers++;
            return new Layer(this);
        }
        public void SetInterfaceMode(bool enabled)
        {
            InterfaceMode = enabled;
            InterfaceModeChanged?.Invoke(enabled);
        }
        public bool TryRebind(string action, int physicalKey, out string? conflict) { conflict = null; return true; }
        public int PhysicalKey(string action) => 0;
        public void SaveBindings() { }

        private sealed class Layer(Router router) : IDisposable
        {
            private bool _disposed;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                router.Layers--;
            }
        }
    }
}
