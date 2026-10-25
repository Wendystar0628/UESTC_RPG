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
public sealed class HudTitleReturnTests
{
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    // 回标题时保存转后台写盘（Deferred），开始页出现后才失败：提示一次，落定即退订，离开开始页也退订（GAME-203）。
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task LateLeaveSaveFailureWarnsOnceOnTitle()
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
        var map = TitleMap();
        var saves = new LateSaveService();
        var maps = new TestMapService { Default = map.Id, Maps = [map] };
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        var audio = new AudioRuntime();
        var hud = new HudRoot();
        try
        {
            hud.Bind(HudTestSupport.Dependencies(settings, model, new MotionPolicy(settings), world, view, flow, router, audio, saves, maps));
            parent.AddChild(hud);
            var toast = hud.HintHost.GetChildren().OfType<Toast>().Single();
            flow.SetState(AppState.Leaving);
            saves.Writing = true;
            flow.SetState(AppState.Title);
            // 触发条件：开始页已出现，这次保存仍在后台写盘、还没失败。
            AssertThat(flow.State == AppState.Title && saves.Busy && saves.State.Last != SaveStatus.Failed && !toast.Visible && saves.Observers == 1)
                .OverrideFailureMessage("The fixture did not reach the title with the leave save still writing in the background.").IsTrue();
            saves.Publish(SaveStatus.Failed);
            AssertThat(toast.Visible && Texts(toast).Contains(UiKit.T("title.leave_unsaved.no_time")) && saves.Observers == 0)
                .OverrideFailureMessage("A late leave-save failure was not reported on the title, or the watch outlived its result.").IsTrue();
            toast.Visible = false;
            saves.Publish(SaveStatus.Failed);
            AssertThat(toast.Visible).OverrideFailureMessage("The same leave reported its save failure twice.").IsFalse();
            // 上一次离开的失败还留在 State.Last 时再离开：这次的后台写盘照样要盯；还没落定就离开开始页，随作用域退订。
            flow.SetState(AppState.Playing);
            flow.SetState(AppState.Leaving);
            saves.Writing = true;
            flow.SetState(AppState.Title);
            AssertThat(saves.State.Last == SaveStatus.Failed && saves.Observers == 1 && !toast.Visible)
                .OverrideFailureMessage("A second background leave save was not watched, or the previous failure was reported as this one.").IsTrue();
            flow.SetState(AppState.Entering);
            AssertThat(saves.Observers == 0).OverrideFailureMessage("Leaving the title kept the leave-save watch.").IsTrue();
        }
        finally
        {
            hud.Free();
            audio.Free();
            await FrameAwaiter.ProcessAsync(parent);
            await FrameAwaiter.ProcessAsync(parent);
        }
    }

    private static IEnumerable<string> Texts(Node root) => root.GetChildren().SelectMany(child =>
        (child is Label label ? [label.Text] : Array.Empty<string>()).Concat(Texts(child)));

    // 开始页没有存档时读默认图的默认世界状态，夹具只给它需要的字段。
    private static MapDescriptor TitleMap() => new(new("test"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, new(-100, -100), new(100, 100)), System.Numerics.Vector2.Zero,
        new(new(-200, -28, -200), new(200, 2000, 200)), new(new(-200, -200), new(200, 200)),
        new(.9f, 30, 3000, -28, 2000, 30, 40, 150), default, default, "start", [],
        new(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), new("测试", "Test"), true, "test"),
        new("test", "test", []), []);

    // 后台写盘中的存档服务：重新实现接口以发布 SlotsChanged；Publish 即这次写盘落定，State 随之变、Busy 落下。
    private sealed class LateSaveService : TestSaveService, ISaveService
    {
        private SaveState _state = new(null, SaveStatus.Saved, null, null, ReadOnlyReason.None);
        public bool Writing { get; set; }
        public override bool Busy => Writing;
        public override SaveState State => _state;
        public new event Action? SlotsChanged;
        public int Observers => SlotsChanged?.GetInvocationList().Length ?? 0;
        public void Publish(SaveStatus status)
        {
            _state = _state with { Last = status, ErrorKey = status == SaveStatus.Failed ? "save.error.disk_full" : null };
            Writing = false;
            SlotsChanged?.Invoke();
        }
    }
}
