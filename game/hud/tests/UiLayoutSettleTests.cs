using Campus.Contracts;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Settings;
using Campus.Kernel.World;
using Campus.Style.Materials;
using Campus.Style.Palette;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class UiLayoutSettleTests
{
    // 界面建的主题、字体等原生资源要等包装终结才归还；整组跑完在这里收齐（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    // GAME-175：换行文字放进两列表格、横排与流式容器时，最大宽度绑成父宽会按整行宽报尺寸，把行撑宽再被压回，逐帧反复。
    // 判据与界面轮的大页画廊共用 UiLayoutGallery.AssertSettledAsync。
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task WrappingTextInMultiColumnContainersSettlesWithinItsColumn()
    {
        var settings = new HudSettings(new SettingsStore());
        var motion = new MotionPolicy(settings);
        var resources = new RequiredResources();
        var clock = new UiThemeClock(new WorldStateService(new TestWeatherProcess()), settings, motion, resources);
        var tokens = UiTokens.Load(resources);
        var colors = UiColors.Load(resources, new PaletteRegistrySource(resources).Catalog);
        var kit = new UiKit(tokens, colors, new UiMaterials(tokens, colors, new ShaderMaterialSource(resources)),
            new UiThemeBuilder(tokens, colors, resources), motion, clock);
        var parent = ((SceneTree)Engine.GetMainLoop()).Root;
        var host = new Control { Name = "SettleHost", Theme = kit.Theme.Theme, MouseFilter = Control.MouseFilterEnum.Ignore };
        try
        {
            parent.AddChild(host);
            host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var frame = kit.Layout.Page(host, UiKit.T("settings.title"));
            var text = string.Join(" ", Enumerable.Repeat(UiKit.T("settings.title"), 120));
            var grid = kit.Layout.Add(frame.Body, new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            var labels = new List<Label> { kit.Text(grid, text), kit.Text(grid, text) };
            var row = kit.Layout.Row(frame.Body);
            labels.Add(kit.Text(row, text));
            kit.Button(row, ButtonStyle.Secondary, UiKit.T("ui.common.close"));
            labels.Add(kit.Text(kit.Layout.Flow(frame.Body), text));

            // 触发条件：每段文字不换行时都比整个视口宽，多列容器里只能靠换行收进自己那一列（页宽会被自撑带宽，不拿来比）。
            await FrameAwaiter.ProcessAsync(host);
            foreach (var label in labels)
            {
                var single = label.GetThemeFont("font").GetStringSize(label.Text, HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size")).X;
                AssertThat(single > host.Size.X).OverrideFailureMessage("The fixture text does not overflow the viewport, so it cannot trigger self-stretching.").IsTrue();
            }

            Exception? unsettled = null;
            try { await UiLayoutGallery.AssertSettledAsync(host, CancellationToken.None); }
            catch (Exception error) when (error is InvalidOperationException or TimeoutException) { unsettled = error; }
            AssertThat(unsettled is null).OverrideFailureMessage("Wrapping text kept resizing its containers: " + unsettled?.Message).IsTrue();
            AssertThat(grid.Size.X <= frame.Shell.Size.X && row.Size.X <= frame.Shell.Size.X)
                .OverrideFailureMessage("A multi-column container grew wider than its page.").IsTrue();
            AssertThat(labels.All(label => label.GetLineCount() > 1)).OverrideFailureMessage("Overflowing text did not wrap inside its column.").IsTrue();
        }
        finally
        {
            host.Free();
            clock.Free();
        }
    }
}
