using System.Globalization;
using System.Text;
using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 性能信息条（UI-NAV）：右上角三个钮的下方一行，帧率、帧时与窗口内最慢一帧、CPU 与 GPU 渲染耗时、本进程 CPU 占用与物理内存、渲染显存、
/// 内部分辨率 → 输出分辨率与生效帧率上限，每 0.5 s 刷新一次。最慢一帧取窗口内内核发布的原始帧间隔（IFrameTiming）的最大值，引擎交来的步长会被截断平滑、漏报卡顿。分辨率与上限读引擎上实际生效的值（视口倍率、Engine.MaxFps、垂直同步），不另算一份。
/// 常驻件，固定深屏（pin）。只在显示时存在；视口渲染耗时测量由帧统计常开，本件只读不开关；刷新之间不分配托管内存（ENG-BUDGET）。
/// </summary>
public partial class PerfReadout : PanelContainer
{
    private const double Interval = 0.5;

    private static readonly int Cores = System.Environment.ProcessorCount;

    private readonly StringBuilder _text = new(96);
    private readonly System.Diagnostics.Process _process = System.Diagnostics.Process.GetCurrentProcess();
    private string _tFps = "", _tFrame = "", _tCpuRender = "", _tGpuRender = "", _tCpuUsage = "", _tMem = "", _tVram = "", _tWorst = "", _tInternal = "", _tCap = "";
    private UiKit _kit = null!;
    private IFrameTiming _timing = null!;
    private Label _label = null!;
    private Rid _viewport;
    private TimeSpan _lastCpuTime;
    private double _since, _worst;   // _worst 单位毫秒
    private int _frames;
    public static PerfReadout Create(UiKit kit, Control parent)
    {
        var bar = new PerfReadout
        {
            _kit = kit,
            _timing = Campus.Kernel.Registry.Kernel.Get<IFrameTiming>(),
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            MouseFilter = MouseFilterEnum.Ignore,
            _tFps = UiKit.T("hud.perf.fps"),
            _tFrame = UiKit.T("hud.perf.frame"),
            _tCpuRender = UiKit.T("hud.perf.cpu_render"),
            _tGpuRender = UiKit.T("hud.perf.gpu_render"),
            _tCpuUsage = UiKit.T("hud.perf.cpu_usage"),
            _tMem = UiKit.T("hud.perf.mem"),
            _tVram = UiKit.T("hud.perf.vram"),
            _tWorst = UiKit.T("hud.perf.worst"),
            _tInternal = UiKit.T("hud.perf.internal"),
            _tCap = UiKit.T("hud.perf.cap"),
        };
        parent.AddChild(bar);
        kit.Apply(bar, new UiFx(Fill: "surface.panel", Line: "line.panel", Pin: true));
        var margin = kit.Layout.Add(bar, new MarginContainer(), UiThemeBuilder.SmallMargin);
        bar._label = kit.Text(margin, "", "data", "text.1", true);
        UiLayout.BindResize(parent, bar, bar.Fit);

        bar._viewport = bar.GetViewport().GetViewportRid();
        bar._lastCpuTime = bar._process.TotalProcessorTime;
        bar.Write(Engine.GetFramesPerSecond(), 0, 0, 0, 0, 0);
        return bar;
    }

    public override void _ExitTree()
    {
        _process.Dispose();
    }

    public override void _Process(double delta)
    {
        // 界面不自己取时钟（G4）：刷新节拍与平均帧时用引擎步长，最慢一帧读内核的原始帧间隔，空值（刚就绪）跳过
        _since += delta;
        _frames++;
        if (_timing.PreviousFrameMilliseconds is { } ms) _worst = Math.Max(_worst, ms);
        if (_since < Interval) return;
        Refresh(_since, _frames, _worst);
        _since = 0;
        _worst = 0;
        _frames = 0;
    }

    private void Refresh(double seconds, int frames, double worstMs)
    {
        var cpuTime = _process.TotalProcessorTime;
        var cpuUsed = (cpuTime - _lastCpuTime).TotalSeconds;
        _lastCpuTime = cpuTime;

        var frameMs = frames > 0 ? seconds * 1000 / frames : 0;
        var usage = seconds > 0 ? cpuUsed / (seconds * Cores) * 100 : 0;
        Write(Engine.GetFramesPerSecond(), frameMs, worstMs,
            RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport),
            RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport), usage);
    }

    private void Write(double fps, double frameMs, double worstMs, double cpuRenderMs, double gpuRenderMs, double cpuUsagePct)
    {
        var c = CultureInfo.InvariantCulture;
        _text.Clear();
        _text.Append(_tFps).Append(' ').Append(c, $"{fps:0}")
            .Append(" · ").Append(_tFrame).Append(' ').Append(c, $"{frameMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tWorst).Append(' ').Append(c, $"{worstMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tCpuRender).Append(' ').Append(c, $"{cpuRenderMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tGpuRender).Append(' ').Append(c, $"{gpuRenderMs:0.0}").Append(" ms")
            .Append(" · ").Append(_tCpuUsage).Append(' ').Append(c, $"{cpuUsagePct:0}").Append('%')
            .Append(" · ").Append(_tMem).Append(' ').Append(c, $"{System.Environment.WorkingSet / (1024.0 * 1024):0}").Append(" MB")
            .Append(" · ").Append(_tVram).Append(' ').Append(c, $"{Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / (1024.0 * 1024):0}").Append(" MB");
        AppendResolution(c);
        _label.Text = _text.ToString();
        Fit();
    }

    // 内部分辨率按视口实际的 3D 倍率算；上限取 Engine.MaxFps，垂直同步开着时再与刷新率取小，0 表示不限
    private void AppendResolution(CultureInfo c)
    {
        var output = GetWindow().Size;
        var scale = GetViewport().Scaling3DScale;
        _text.Append(" · ").Append(_tInternal).Append(' ')
            .Append(c, $"{(int)Math.Round(output.X * scale)}×{(int)Math.Round(output.Y * scale)} → {output.X}×{output.Y}");

        var cap = Engine.MaxFps;
        if (DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled)
        {
            var refresh = (int)Math.Floor(DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen()));
            if (refresh > 0 && (cap <= 0 || refresh < cap)) cap = refresh;
        }
        _text.Append(" · ").Append(_tCap).Append(' ');
        if (cap > 0) _text.Append(cap);
        else _text.Append('∞');
    }

    private void Fit()
    {
        var font = _kit.Theme.Font("data", out var size, out _);
        var width = font.GetStringSize(_label.Text, HorizontalAlignment.Left, -1, size).X + 2 * _kit.Tokens.F("metrics", "grid", "half");
        var available = GetParentControl().CustomMaximumSize.X;
        CustomMinimumSize = new Vector2(available > 0 ? Math.Min(width, available) : width, 0);
    }
}
