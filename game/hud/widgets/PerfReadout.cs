using System.Globalization;
using System.Text;
using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 性能信息条（UI-NAV）：右上角三个钮的下方一行，帧率、帧时与窗口内最慢一帧、CPU 与 GPU 渲染耗时、本进程 CPU 占用与物理内存、渲染显存、
/// 内部分辨率 → 输出分辨率与生效帧率上限。前几项只读内核帧统计的显示窗（IFrameStatistics，ENG-PERF-DATA），窗口序号变了才重写，缺值显示"—"；分辨率与上限读引擎上实际生效的值（视口倍率、Engine.MaxFps、垂直同步），不另算一份。
/// 常驻件，固定深屏（pin）。只在显示时存在；刷新之间不分配托管内存（ENG-BUDGET）。
/// </summary>
public partial class PerfReadout : PanelContainer
{
    private const double BytesPerMegabyte = 1024.0 * 1024;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private readonly StringBuilder _text = new(96);
    private string _tFps = "", _tFrame = "", _tCpuRender = "", _tGpuRender = "", _tCpuUsage = "", _tMem = "", _tVram = "", _tWorst = "", _tInternal = "", _tCap = "";
    private UiKit _kit = null!;
    private IFrameStatistics _stats = null!;
    private Label _label = null!;
    private ulong? _sequence;
    public static PerfReadout Create(UiKit kit, Control parent)
    {
        var bar = new PerfReadout
        {
            _kit = kit,
            _stats = Campus.Kernel.Registry.Kernel.Get<IFrameStatistics>(),
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

        bar._sequence = bar._stats.DisplayWindow?.Sequence;
        bar.Write(bar._stats.DisplayWindow);
        return bar;
    }

    public override void _Process(double delta)
    {
        // 显示窗首次发布前为空；序号没变就不重写
        if (_stats.DisplayWindow is not { } window || window.Sequence == _sequence) return;
        _sequence = window.Sequence;
        Write(window);
    }

    private void Write(FrameDisplayWindow? w)
    {
        _text.Clear();
        Item(_tFps, w?.FramesPerSecond, false, "");
        Item(_tFrame, w?.AverageFrameMilliseconds, true, " ms");
        Item(_tWorst, w?.SlowestFrameMilliseconds, true, " ms");
        Item(_tCpuRender, w?.MainViewportCpuMilliseconds, true, " ms");
        Item(_tGpuRender, w?.MainViewportGpuMilliseconds, true, " ms");
        Item(_tCpuUsage, w?.ProcessCpuFraction * 100, false, "%");
        Item(_tMem, w?.WorkingSetBytes / BytesPerMegabyte, false, " MB");
        Item(_tVram, w?.EngineVideoMemoryBytes / BytesPerMegabyte, false, " MB");
        AppendResolution();
        _label.Text = _text.ToString();
        Fit();
    }

    // 缺值显示"—"，不显示 0（UI-NAV）
    private void Item(string label, double? value, bool fine, string unit)
    {
        if (_text.Length > 0) _text.Append(" · ");
        _text.Append(label).Append(' ');
        if (value is not { } v)
        {
            _text.Append('—');
            return;
        }
        if (fine) _text.Append(Invariant, $"{v:0.0}");
        else _text.Append(Invariant, $"{v:0}");
        _text.Append(unit);
    }

    // 内部分辨率按视口实际的 3D 倍率算；上限取 Engine.MaxFps，垂直同步开着时再与刷新率取小，0 表示不限
    private void AppendResolution()
    {
        var output = GetWindow().Size;
        var scale = GetViewport().Scaling3DScale;
        _text.Append(" · ").Append(_tInternal).Append(' ')
            .Append(Invariant, $"{(int)Math.Round(output.X * scale)}×{(int)Math.Round(output.Y * scale)} → {output.X}×{output.Y}");

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
