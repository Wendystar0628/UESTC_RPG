using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Audio;
using Campus.Contracts.Settings;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Kernel.Materials;
using Campus.Kernel.Registry;
using Campus.Kernel.World;
using Campus.Style.Materials;
using Campus.Systems.Weather;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Tests;

[TestSuite]
public sealed class HudLifetimeTests
{
    [TestCase]
    [TestCategory("Regression")]
    [RequireGodotRuntime]
    public void ThemeClockStopsObservingSettingsUntilItReenters()
    {
        var settings = new HudSettings(Services.Get<ISettingsStore>());
        settings.SetTheme(UiThemeMode.Light);
        settings.SetReduceMotion(ReduceMotionMode.On);
        var world = new WorldStateService(new WeatherProcess(Services.Get<IWeatherProfileSource>()));
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
    [TestCategory("Regression")]
    [RequireGodotRuntime]
    public async Task HudRootReentryRebuildsResidentsAndOwnsOnlyItsInputLayers()
    {
        var previousRegistry = Services.Services;
        var servicesProperty = typeof(Services).GetProperty(nameof(Services.Services))!;
        var registry = new ServiceRegistry();
        var flow = new TestAppFlow();
        var router = new Router();
        var settings = new HudSettings(Services.Get<ISettingsStore>());
        var world = new WorldStateService(new TestWeatherProcess());
        var camera = new TestCameraRig();
        var model = new HudModel(world, camera, previousRegistry.Get<IRuleset>());
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
            registry.Add(settings);
            registry.Add(model);
            registry.Add<IMotionSettings>(motion);
            registry.Add(motion);
            registry.Add(new UiThemeClock(world, settings, motion));
            registry.Add<IWorldState>(world);
            registry.Add<IWorldControl>(world);
            registry.Add<ICameraRig>(camera);
            registry.Add<IPaletteCatalogSource>(previousRegistry.Get<IPaletteCatalogSource>());
            registry.Add<IShaderMaterialSource>(new ShaderMaterialSource());
            registry.Add<IPaletteProbe>(new Probe());
            registry.Add<IAudioService>(previousRegistry.Get<IAudioService>());
            registry.Add<IMapService>(previousRegistry.Get<IMapService>());
            registry.Add<IAppFlow>(flow);
            registry.Add<IInputRouter>(router);
            servicesProperty.SetValue(null, registry);
            hud = new HudRoot();
            parent.AddChild(hud);
            var root = hud.GetNode<Control>("Root");
            var corners = root.GetNode<Control>("Corners");
            var residentCount = corners.GetChildCount();
            AssertThat(residentCount > 0).OverrideFailureMessage("Playing HUD did not build its resident controls.").IsTrue();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                AssertThat(router.Layers == 3 && router.Observers == 1 && flow.Observers == 1).OverrideFailureMessage("HUD registered duplicate input layers or flow observers.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline + 2).OverrideFailureMessage("HUD root and theme clock did not bind world notifications exactly once.").IsTrue();
                parent.RemoveChild(hud);
                AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0).OverrideFailureMessage("Tree exit retained input layers or service observers.").IsTrue();
                AssertThat(Subscribers(settings, "Changed") == settingsBaseline && Subscribers(world, "InstanceChanged") == worldBaseline).OverrideFailureMessage("Tree exit retained settings/world observers.").IsTrue();
                AssertThat(corners.GetChildCount() == 0).OverrideFailureMessage("Queued residents remained parented and could reenter before deletion.").IsTrue();
                router.SetInterfaceMode(!router.InterfaceMode);
                flow.SetState(AppState.Boot);
                flow.SetState(AppState.Playing);
                parent.AddChild(hud);
                AssertThat(ReferenceEquals(root, hud.GetNode<Control>("Root")) && corners.GetChildCount() == residentCount).OverrideFailureMessage("Reentry duplicated the root or failed to rebuild residents.").IsTrue();
            }
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
                servicesProperty.SetValue(null, previousRegistry);
                // 保留同帧重入的检查；失败时也让退树 QueueFree 的四角走完实际释放。
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
    }

    private static int Subscribers(object source, string name) =>
        ((Delegate?)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source))?.GetInvocationList().Length ?? 0;

    private sealed class Probe : IPaletteProbe
    {
        public void SampleHorizon(Season season, WeatherVec weather, Span<System.Numerics.Vector3> srgb24) =>
            srgb24.Fill(System.Numerics.Vector3.Zero);
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
