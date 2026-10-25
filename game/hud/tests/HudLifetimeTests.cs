using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Audio.Core;
using Campus.Contracts.AppFlow;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Settings;
using Campus.Kernel.World;
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
        var clock = CycleThemeClock(settings, world, motion);
        // 设置与世界在这之后仍活着：钟释放后还能被回收，才说明离树时两处订阅都已退掉。
        AssertThat(Collected(clock)).OverrideFailureMessage("Freed clock is still held by a settings or world subscription.").IsTrue();
        GC.KeepAlive(settings);
        GC.KeepAlive(world);
    }

    // 单独成帧，调试构建下栈上不留钟的引用；返回弱引用给调用方判回收。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CycleThemeClock(HudSettings settings, WorldStateService world, MotionPolicy motion)
    {
        var clock = new UiThemeClock(world, settings, motion, new RequiredResources());
        var switches = 0;
        clock.Switched += _ => switches++;
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        try
        {
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                parent.AddChild(clock);
                AssertThat(clock.Dark).OverrideFailureMessage("Clock did not take the configured light theme on entry.").IsFalse();
                settings.Set(HudSettingDefinitions.Theme, (int)UiThemeMode.Dark);
                AssertThat(clock.Dark && switches == cycle).OverrideFailureMessage("Attached clock did not switch once on a theme setting change.").IsTrue();
                parent.RemoveChild(clock);
                var front = clock.Front;
                settings.Set(HudSettingDefinitions.Theme, (int)UiThemeMode.Light);
                // 离树期间换一次世界实例：发给已退订的钟不该出错。
                world.EndInstance();
                world.Freeze(13, Season.Summer, 0);
                world.CompleteRestore();
                AssertThat(clock.Dark && clock.Front == front && switches == cycle).OverrideFailureMessage("Detached clock still responded to settings.").IsTrue();
            }
            return new WeakReference(clock);
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
            hud.Bind(HudTestSupport.Dependencies(settings, model, motion, world, view, flow, router, audio));
            parent.AddChild(hud);
            var pages = hud.PageHost;
            var corners = hud.CornerHost;
            var residentCount = Residents(corners);
            AssertThat(residentCount > 0).OverrideFailureMessage("Playing HUD did not build its resident controls.").IsTrue();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                AssertThat(router.Layers == 3 && router.Observers == 1 && flow.Observers == 1).OverrideFailureMessage("HUD registered duplicate input layers or flow observers.").IsTrue();
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
                AssertThat(Residents(corners) == 0).OverrideFailureMessage("Queued residents remained parented and could reenter before deletion.").IsTrue();
                // 退树后释放上层内容，仲裁必须已停；重入时按现存子件重新仲裁。
                hud.DialogHost.RemoveChild(dialog);
                dialog.Free();
                // 离树期间设置与世界照常变化：发给已退订的界面不该出错，重入时按现值重建。
                var follows = settings.Get(HudSettingDefinitions.Resident) == (int)ResidentTheme.Follow;
                settings.Set(HudSettingDefinitions.Resident, (int)(follows ? ResidentTheme.Dark : ResidentTheme.Follow));
                world.EndInstance();
                world.Freeze(13, Season.Summer, 0);
                world.CompleteRestore();
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
            var released = new WeakReference(hud);
            hud.Free();
            hud = null;
            AssertThat(router.Layers == 0 && router.Observers == 0 && flow.Observers == 0 && router.Cursors == 0).OverrideFailureMessage("Free retained HUD registrations.").IsTrue();
            // 隔一帧再判，调试构建下本段栈上的临时引用已不在；设置、世界、流程与输入仍活着，界面还能被回收才说明订阅都已退掉。
            await FrameAwaiter.ProcessAsync(parent);
            AssertThat(Collected(released)).OverrideFailureMessage("Freed HUD is still held by a settings, world or service subscription.").IsTrue();
        }
        finally
        {
            hud?.Free();
            audio.Free();
            // 保留同帧重入的检查；失败时也让退树 QueueFree 的四角走完实际释放。
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
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
            hud.Bind(HudTestSupport.Dependencies(settings, model, new MotionPolicy(settings), world, view, flow, router, audio));
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

    // 退订的外部可见结果：释放以后，活着的服务不再经订阅持有界面，界面对象可以被回收。
    private static bool Collected(WeakReference target)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return !target.IsAlive;
    }

    private static int Residents(Node root) => root.GetChildren().Sum(child =>
        (child is SpeedBar or TimeCard or TimeStrip or WeatherStrip ? 1 : 0) + Residents(child));

    private static bool TimeCardShown(Node root) => root.GetChildren().Any(child =>
        child is TimeCard card ? card.IsVisibleInTree() : TimeCardShown(child));
}
