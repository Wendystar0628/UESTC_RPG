using Campus.Contracts.Quality;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 设置页底部的说明栏（UI-NAV、UI-VISUAL-COMPONENTS）：当前行的名称与生效条件、一句说明、附加读数、禁用原因，
/// 以及 GPU、显存、CPU 各一组三格负载 LED（小、中、大依次点亮一到三格）。写什么由页面按目录给出，本件只排版。
/// 栏的最小高度留够"名称＋两行说明＋一行读数＋LED"，悬停换行时不推动上方的设置行。
/// </summary>
internal sealed class SettingNoteBar
{
    /// <summary>一条说明：Detail、Warning 为空即不显示那一行；Load 为空时不画 LED（ui 节的行没有负载）。</summary>
    public readonly record struct Note(string Title, string Condition, string Hint,
        string? Detail = null, string? Warning = null, KnobLoad? Load = null);

    private const int Cells = 3;
    private readonly UiKit _kit;
    private readonly Label _title, _condition, _hint, _detail, _warning;
    private readonly HBoxContainer _leds;
    private readonly Panel[][] _cells = new Panel[3][];

    public PanelContainer Root { get; }

    private SettingNoteBar(UiKit kit, Control parent)
    {
        _kit = kit;
        Root = kit.Panel(parent, new UiFx(Line: "line.control"));
        var margin = kit.Layout.Add(Root, new MarginContainer(), UiThemeBuilder.SmallMargin);
        var stack = kit.Layout.Stack(margin, groups: false);
        var head = kit.Layout.Row(stack);
        _title = kit.Text(head, "", "body.strong", "text.1", wrap: false);
        _condition = kit.Text(head, "", "data", "text.2", wrap: false, align: HorizontalAlignment.Right);
        _condition.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        _condition.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        _hint = kit.Text(stack, "", "small", "text.2");
        _detail = kit.Text(stack, "", "data", "text.1");
        _warning = kit.Text(stack, "", "small", "warning.text");
        _leds = kit.Layout.Row(stack);
        // LED 格沿用速度 LED 的格子尺寸与格距，同一套"LED 格"视觉（UI-VISUAL-COMPONENTS）。
        var size = kit.Tokens.V2("metrics", "speedLed", "size");
        var gap = kit.Tokens.F("metrics", "speedLed", "gap");
        string[] names = ["quality.load.gpu", "quality.load.vram", "quality.load.cpu"];
        for (var group = 0; group < names.Length; group++)
        {
            var label = kit.Text(_leds, UiKit.T(names[group]), "data", "text.2", wrap: false);
            label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            // 三格是自绘件：格位在件内按格宽与格距排，件的最小尺寸交给容器。
            var cells = kit.Layout.Add(_leds, new Control
            {
                CustomMinimumSize = new Vector2(Cells * size.X + (Cells - 1) * gap, size.Y),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            });
            _cells[group] = new Panel[Cells];
            for (var i = 0; i < Cells; i++)
            {
                var cell = kit.Box(cells, new UiFx(Line: "line.control"));
                cell.Position = new Vector2(i * (size.X + gap), 0);
                cell.Size = size;
                _cells[group][i] = cell;
            }
        }
        float Height(string role) => MathF.Ceiling(kit.Theme.Font(role, out var fontSize, out _).GetHeight(fontSize));
        var separation = stack.GetThemeConstant("separation");
        stack.CustomMinimumSize = new Vector2(0,
            Height("body.strong") + 2 * Height("small") + Height("data") + size.Y + 4 * separation);
    }

    public static SettingNoteBar Create(UiKit kit, Control parent) => new(kit, parent);

    public void Show(Note note)
    {
        _title.Text = note.Title;
        _condition.Text = note.Condition;
        _hint.Text = note.Hint;
        _detail.Text = note.Detail ?? "";
        _detail.Visible = !string.IsNullOrEmpty(note.Detail);
        _warning.Text = note.Warning ?? "";
        _warning.Visible = !string.IsNullOrEmpty(note.Warning);
        _leds.Visible = note.Load is not null;
        if (note.Load is not { } load) return;
        Light(0, load.Gpu);
        Light(1, load.Vram);
        Light(2, load.Cpu);
    }

    // 负载 0–2 依次点亮 1–3 格：小、中、大。
    private void Light(int group, byte level)
    {
        for (var i = 0; i < Cells; i++)
            _kit.Apply(_cells[group][i], i <= level ? new UiFx(Fill: "led.on") : new UiFx(Line: "line.control"));
    }
}
