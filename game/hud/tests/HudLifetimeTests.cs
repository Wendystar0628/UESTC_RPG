using System.Reflection;
using Campus.Contracts;
using Campus.Audio.Core;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Audio;
using Campus.Contracts.Settings;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
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
        settings.Set(HudSettingDefinitions.Theme, (int)UiThemeMode.Light);
        settings.Set(HudSettingDefinitions.ReduceMotion, (int)ReduceMotionMode.On);
        var world = new WorldStateService(new TestWeatherProcess());
        var motion = new MotionPolicy(settings);
        var baseline = Subscribers(settings, "Changed");
        var worldBaseline = Subscribers(world, "InstanceChanged");
        var resources = new RequiredResources();
        var clock = new UiThemeClock(world, settings, motion, resources);
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        try
        {
            for (var cycle = 0; cycle < 3; cycle++)
            {
                parent.AddChild(clock);
                AssertThat(!clock.Dark && Subscribers(settings, "Changed") == baseline + 1).OverrideFailureMessage("Clock did not bind exactly once on entry.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline + 1).OverrideFailureMessage("Clock did not bind world notification exactly once on entry.").IsTrue();
                settings.Set(HudSettingDefinitions.Theme, (int)UiThemeMode.Dark);
                AssertThat(clock.Dark).OverrideFailureMessage("Attached clock stopped responding to theme settings.").IsTrue();
                parent.RemoveChild(clock);
                var front = clock.Front;
                settings.Set(HudSettingDefinitions.Theme, (int)UiThemeMode.Light);
                AssertThat(clock.Dark && clock.Front == front && Subscribers(settings, "Changed") == baseline).OverrideFailureMessage("Detached clock still observed settings or retained a service subscription.").IsTrue();
                AssertThat(Subscribers(world, "InstanceChanged") == worldBaseline).OverrideFailureMessage("Detached clock retained a world observer.").IsTrue();
            }
        }
        finally { clock.Free(); }
    }

    // 界面建的主题、字体等原生资源要等包装终结才归还；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task HudRootReentryRebuildsResidentsAndOwnsOnlyItsInputLayers()
    {
        var ruleset = TestRuleset.ForFlow();
        var flow = new TestAppFlow();
        var router = new TestInputRouter();
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
        world.CompleteRestore();
        AssertThat(model.Mode == camera.Mode && model.Clock == world.Clock && model.Season == world.SeasonTarget && model.Speed == camera.Speed)
            .OverrideFailureMessage("HUD readouts required a refresh before exposing the current source values.").IsTrue();
        camera.Mode = ViewMode.Fly;
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        // AudioRuntime is a Node outside the tree: disposing the wrapper does not free it, so the fixture frees it explicitly.
        var audio = new AudioRuntime();
        HudRoot? hud = null;
        try
        {
            hud = new HudRoot();
            hud.Bind(Dependencies(settings, model, motion, world, view, flow, router, audio));
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
                // 宿主仲裁每帧跑一次：ProcessFrame 信号先于节点的 _Process，等两次才保证 HUD 至少处理过一帧。
                await FrameAwaiter.ProcessAsync(parent);
                await FrameAwaiter.ProcessAsync(parent);
                AssertThat(hud.PageHost.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Disabled)
                    .OverrideFailureMessage("A visible dialog did not disable the page host.").IsTrue();
                AssertThat(router.Cursors == 1).OverrideFailureMessage("A visible dialog did not hold exactly one cursor lease.").IsTrue();
                parent.RemoveChild(hud);
                AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0).OverrideFailureMessage("Tree exit retained input layers or service observers.").IsTrue();
                AssertThat(router.Cursors == 0).OverrideFailureMessage("Tree exit retained the HUD cursor lease.").IsTrue();
                AssertThat(Subscribers(settings, "Changed") == settingsBaseline && Subscribers(world, "InstanceChanged") == worldBaseline).OverrideFailureMessage("Tree exit retained settings/world observers.").IsTrue();
                AssertThat(Residents(corners) == 0).OverrideFailureMessage("Queued residents remained parented and could reenter before deletion.").IsTrue();
                // 退树后释放上层内容，仲裁必须已停；重入时按现存子件重新仲裁。
                hud.DialogHost.RemoveChild(dialog);
                dialog.Free();
                router.PublishCursor(true);
                flow.SetState(AppState.Boot);
                flow.SetState(AppState.Playing);
                parent.AddChild(hud);
                AssertThat(ReferenceEquals(pages, hud.PageHost) && Residents(corners) == residentCount).OverrideFailureMessage("Reentry duplicated the hosts or failed to rebuild residents.").IsTrue();
                AssertThat(hud.PageHost.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Inherited)
                    .OverrideFailureMessage("Reentry retained the detached dialog's focus block.").IsTrue();
            }
            // 进游戏不展开面板；T 展开的时间卡持光标租约，Esc 先收起它而不是打开设置卡，收起后归还租约（UI-NAV）。
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(!TimeCardShown(corners) && router.Cursors == 0).OverrideFailureMessage("Entering play expanded a panel or held the cursor.").IsTrue();
            AssertThat(router.Press("time_panel")).OverrideFailureMessage("The HUD did not consume the time card shortcut.").IsTrue();
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(TimeCardShown(corners) && router.Cursors == 1).OverrideFailureMessage("The expanded time card did not hold the cursor.").IsTrue();
            AssertThat(router.Press("menu") && !TimeCardShown(corners) && hud.PageHost.GetChildren().OfType<Control>().All(page => !page.Visible))
                .OverrideFailureMessage("Esc did not close the top panel before opening any page.").IsTrue();
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(router.Cursors == 0).OverrideFailureMessage("Closing the last panel did not return the cursor lease.").IsTrue();
            // 直接带可见弹层释放根，覆盖退出时子节点信号与宿主销毁的顺序。
            hud.DialogHost.AddChild(new Control());
        }
        finally
        {
            try
            {
                hud?.Free();
                AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0 && router.Cursors == 0).OverrideFailureMessage("Free retained HUD registrations.").IsTrue();
            }
            finally
            {
                audio.Free();
                // 保留同帧重入的检查；失败时也让退树 QueueFree 的四角走完实际释放。
                await FrameAwaiter.ProcessAsync(parent);
                await FrameAwaiter.ProcessAsync(parent);
            }
        }
    }

    // 大页（设置卡、存档页）与对话框同样经宿主仲裁持光标租约：页宿主里有可见内容持 1 份，隐藏后归还（UI-NAV、DECISION-308）。
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task PageHostContentHoldsTheCursorLeaseUntilHidden()
    {
        var ruleset = TestRuleset.ForFlow();
        var flow = new TestAppFlow();
        var router = new TestInputRouter();
        var settings = new HudSettings(new SettingsStore());
        var world = new WorldStateService(new TestWeatherProcess());
        var view = new TestWorldState(new TestWeatherProcess(), ruleset);
        var model = new HudModel(world, new TestCameraRig(view), ruleset);
        world.Freeze(13, Season.Summer, 0);
        world.CompleteRestore();
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        var audio = new AudioRuntime();
        var hud = new HudRoot();
        try
        {
            hud.Bind(Dependencies(settings, model, new MotionPolicy(settings), world, view, flow, router, audio));
            parent.AddChild(hud);
            // 宿主仲裁每帧跑一次：ProcessFrame 信号先于节点的 _Process，等两次才保证 HUD 至少处理过一帧。
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(router.Cursors == 0).OverrideFailureMessage("Playing without an open page held the cursor.").IsTrue();
            var page = new Control();
            hud.PageHost.AddChild(page);
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(router.Cursors == 1).OverrideFailureMessage("A visible page did not hold exactly one cursor lease.").IsTrue();
            page.Visible = false;
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(router.Cursors == 0).OverrideFailureMessage("Hiding the page did not return the cursor lease.").IsTrue();
        }
        finally
        {
            // 页面随界面根一起释放；退树 QueueFree 的四角等两帧走完实际释放。
            hud.Free();
            audio.Free();
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
        }
    }

    // The fixture supplies only explicit dependencies; it never constructs the production boot graph.
    private static HudRootDependencies Dependencies(HudSettings settings, HudModel model, MotionPolicy motion, WorldStateService world,
        TestWorldState view, TestAppFlow flow, TestInputRouter router, AudioRuntime audio)
    {
        var resources = new RequiredResources();
        return new HudRootDependencies(
            Settings: settings,
            Model: model,
            Motion: motion,
            WorldControl: world,
            Flow: flow,
            Exit: new TestAppExit(allowQuit: false),
            Failure: new RecordingFailure(),
            Audio: audio,
            PaletteCatalog: new PaletteRegistrySource(resources),
            ShaderMaterials: new ShaderMaterialSource(resources),
            ThemeClock: new UiThemeClock(world, settings, motion, resources),
            InputRouter: router,
            ViewControl: view,
            Capture: new UnusedCapture(),
            Maps: new TestMapService(),
            WorldState: world,
            Saves: new TestSaveService(),
            PaletteProbe: new Probe(),
            Diagnostics: new DiagnosticLog(),
            Resources: resources);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task HudTaskReportsOriginalFailureAndEndsQuietlyOnScopeCancel()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var failure = new RecordingFailure();
        var operation = new HudTask(failure);
        var pending = new TaskCompletionSource();
        AssertThat(operation.Start(token => pending.Task.WaitAsync(token))).IsTrue();
        AssertThat(operation.Start(_ => Task.CompletedTask)).OverrideFailureMessage("A running UI operation accepted a second start.").IsFalse();
        // 作用域取消只结束界面一侧的等待，不算失败。
        operation.Cancel();
        await FrameAwaiter.ProcessAsync(root);
        await FrameAwaiter.ProcessAsync(root);
        AssertThat(!operation.Running && failure.Errors.Count == 0).OverrideFailureMessage("Scope cancellation was reported as a failure or kept the operation running.").IsTrue();
        // 以前被丢弃的故障任务吞掉失败：现在原异常交给失败接收方。
        var error = new InvalidOperationException();
        AssertThat(operation.Start(_ => Task.FromException(error))).IsTrue();
        await FrameAwaiter.ProcessAsync(root);
        AssertThat(failure.Errors.Count == 1 && ReferenceEquals(failure.Errors[0], error) && !operation.Running)
            .OverrideFailureMessage("A failed UI operation did not hand its original exception to the failure receiver.").IsTrue();
    }

    private static int Subscribers(object source, string name) =>
        ((Delegate?)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source))?.GetInvocationList().Length ?? 0;

    private static int Residents(Node root) => root.GetChildren().Sum(child =>
        (child is SpeedBar or TimeCard or TimeStrip or WeatherStrip ? 1 : 0) + Residents(child));

    private static bool TimeCardShown(Node root) => root.GetChildren().Any(child =>
        child is TimeCard card ? card.IsVisibleInTree() : TimeCardShown(child));

    private sealed class Probe : IPaletteProbe
    {
        public void SampleHorizon(Season season, WeatherVec weather, Span<System.Numerics.Vector3> srgb24) =>
            srgb24.Fill(System.Numerics.Vector3.Zero);
    }

    private sealed class RecordingFailure : IAppFailure
    {
        public List<Exception> Errors { get; } = [];
        public void Report(Exception error) => Errors.Add(error);
    }

    private sealed class UnusedCapture : IGameCapture
    {
        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
