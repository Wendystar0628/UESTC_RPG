using System.Globalization;
using System.Text;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 性能信息条（UI-NAV §2）：右上角三个钮的下方一行，帧率、帧时、CPU 与 GPU 渲染耗时、本进程 CPU 占用，每 0.5 s 刷新一次。
/// 常驻件，固定深屏（pin）。只在显示时存在：建时打开视口渲染耗时测量，移出场景树时关闭；刷新之间不分配托管内存（ENG-BUDGET）。
/// </summary>
public partial class PerfReadout : PanelContainer
{
    private const double Interval = 0.5;

    private static readonly int Cores = System.Environment.ProcessorCount;

    private readonly StringBuilder _text = new(96);
    private readonly System.Diagnostics.Process _process = System.Diagnostics.Process.GetCurrentProcess();
    private string _tFps = "", _tFrame = "", _tCpuRender = "", _tGpuRender = "", _tCpuUsage = "";
    private UiKit _kit = null!;
    private Label _label = null!;
    private Rid _viewport;
    private TimeSpan _lastCpuTime;
    private double _since;
    private int _frames;
    public static PerfReadout Create(UiKit kit, Control parent)
    {
        var bar = new PerfReadout
        {
            _kit = kit,
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            MouseFilter = MouseFilterEnum.Ignore,
            _tFps = UiKit.T("hud.perf.fps"),
            _tFrame = UiKit.T("hud.perf.frame"),
            _tCpuRender = UiKit.T("hud.perf.cpu_render"),
            _tGpuRender = UiKit.T("hud.perf.gpu_render"),
            _tCpuUsage = UiKit.T("hud.perf.cpu_usage"),
        };
        parent.AddChild(bar);
        kit.Apply(bar, new UiFx(Fill: "surface.panel", Line: "line.panel", Pin: true));
        var margin = kit.Layout.Add(bar, new MarginContainer(), UiThemeBuilder.SmallMargin);
        bar._label = kit.Text(margin, "", "data", "text.1", true);
        UiLayout.BindResize(parent, bar, bar.Fit);

        bar._viewport = bar.GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(bar._viewport, true);
        bar._process.Refresh();
        bar._lastCpuTime = bar._process.TotalProcessorTime;
        bar.Write(Engine.GetFramesPerSecond(), 0, 0, 0, 0);
        return bar;
    }

    public override void _ExitTree()
    {
        RenderingServer.ViewportSetMeasureRenderTime(_viewport, false);
        _process.Dispose();
    }

    public override void _Process(double delta)
    {
        // 时间只用引擎交来的帧间隔累计：计时归内核，界面不自己取时钟（G4）
        _since += delta;
        _frames++;
        if (_since < Interval) return;
        Refresh(_since, _frames);
        _since = 0;
        _frames = 0;
    }

    private void Refresh(double seconds, int frames)
    {
        _process.Refresh();
        var cpuTime = _process.TotalProcessorTime;
        var cpuUsed = (cpuTime - _lastCpuTime).TotalSeconds;
        _lastCpuTime = cpuTime;

        var frameMs = frames > 0 ? seconds * 1000 / frames : 0;
        var usage = seconds > 0 ? cpuUsed / (seconds * Cores) * 100 : 0;
        Write(Engine.GetFramesPerSecond(), frameMs,
            RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport),
            RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport), usage);
    }

    private void Write(double fps, double frameMs, double cpuRenderMs, double gpuRenderMs, double cpuUsagePct)
    {
        var c = CultureInfo.InvariantCulture;
        _text.Clear();
        _text.Append(_tFps).Append(' ').Append(c, $"{fps:0}")
            .Append(" · ").Append(_tFrame).Append(' ').Append(c, $"{frameMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tCpuRender).Append(' ').Append(c, $"{cpuRenderMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tGpuRender).Append(' ').Append(c, $"{gpuRenderMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tCpuUsage).Append(' ').Append(c, $"{cpuUsagePct:0}").Append('%');
        _label.Text = _text.ToString();
        Fit();
    }

    private void Fit()
    {
        var font = _kit.Theme.Font("data", out var size, out _);
        var width = font.GetStringSize(_label.Text, HorizontalAlignment.Left, -1, size).X + 2 * _kit.Tokens.F("metrics", "grid", "half");
        var available = GetParentControl().CustomMaximumSize.X;
        CustomMinimumSize = new Vector2(available > 0 ? Math.Min(width, available) : width, 0);
    }
}
