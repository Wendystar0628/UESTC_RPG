using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>存档页的一张槽卡：表头状态标签、缩略图、槽摘要、接点条与命中层（UI-VISUAL-COMPONENTS）；选中态与写入中只由存档页切换。</summary>
internal sealed class SaveSlotCard
{
    private readonly UiKit _kit;
    private readonly PanelContainer _panel;
    private readonly List<Panel> _contacts;
    private readonly bool _empty;
    private readonly Label _writing;

    public TechButton Hit { get; }

    private SaveSlotCard(UiKit kit, PanelContainer panel, List<Panel> contacts, bool empty, Label writing, TechButton hit)
    {
        _kit = kit;
        _panel = panel;
        _contacts = contacts;
        _empty = empty;
        _writing = writing;
        Hit = hit;
    }

    /// <summary>槽号的两位显示形式，卡头、提示与确认框共用。</summary>
    public static string Number(SlotId slot) => slot.Value.TrimStart('s').PadLeft(2, '0');

    /// <param name="latest">最近保存的可读存档位（继续进入的那个）。</param>
    /// <param name="current">本局正在自动保存的存档位，只在游戏内打开时标出。</param>
    public static SaveSlotCard Build(UiKit kit, Control parent, SlotInfo slot, bool latest, bool current, ISaveService saves, IMapService maps)
    {
        // 空只看健康状态：坏档、新版本档读不出元信息时也不是空槽（SYS-SAVE-FORMAT）。
        var empty = slot.Health == SlotHealth.Empty;
        var panel = kit.Panel(parent, CardFx(kit, empty, selected: false));
        var margin = kit.Layout.Margin(panel);
        var content = kit.Layout.Stack(margin, groups: false);
        var header = kit.Layout.Row(content);
        kit.Text(header, $"SLOT {Number(slot.Slot)}", "mono-caps", "text.2");
        if (latest)
        {
            var lamp = kit.Box(header, new UiFx(Fill: "led.on", Flags: UiFlags.Circle));
            var diameter = kit.Tokens.F("metrics", "via", "core");
            lamp.CustomMinimumSize = new Vector2(diameter, diameter);
            lamp.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            Tag(kit, header, "saves.latest", "text.accent");
        }
        if (current) Tag(kit, header, "saves.current", "text.1");
        var writing = Tag(kit, header, "saves.writing", "text.2");
        writing.Visible = false;
        // 只读用中性色，不借危险色（UI-VISUAL-COMPONENTS）。
        if (slot.Health == SlotHealth.Newer) Tag(kit, header, "saves.read_only", "text.2");
        Summary(kit, content, margin, slot, saves, maps);
        var pins = kit.Layout.Row(content);
        var contacts = new List<Panel>();
        for (var i = 0; i < 16; i++)
        {
            var pin = kit.Box(pins, new UiFx(Line: "line.hair"));
            pin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            pin.CustomMinimumSize = new Vector2(0, kit.Layout.Metric("keycap") / 2f);
            contacts.Add(pin);
        }
        // PanelContainer 把内容和命中层叠放；空文字按钮保留原生焦点、悬停和鼠标命中。
        var hit = kit.Button(panel, ButtonStyle.Ghost, "");
        hit.SizeFlagsHorizontal = hit.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        hit.TooltipText = $"SLOT {Number(slot.Slot)}";
        return new SaveSlotCard(kit, panel, contacts, empty, writing, hit);
    }

    /// <summary>
    /// 卡身：左 16:9 缩略图、右槽摘要；存档卡与危险确认的缩略卡共用。
    /// <paramref name="margin"/> 是外层留边容器，首次排版按它的左右留边估缩略图高度。
    /// </summary>
    public static void Summary(UiKit kit, Control parent, MarginContainer margin, SlotInfo slot, ISaveService saves, IMapService maps)
    {
        var empty = slot.Health == SlotHealth.Empty;
        var corrupt = slot.Health == SlotHealth.Corrupt;
        var newer = slot.Health == SlotHealth.Newer;
        // 所有槽共用同一内容区；空槽也保留缩略图占位和槽状态，不缩成只有接点的细条。
        var body = kit.Layout.Row(parent);
        var aspect = kit.Layout.Add(body, new AspectRatioContainer
        {
            Ratio = 16f / 9f,
            StretchMode = AspectRatioContainer.StretchModeEnum.WidthControlsHeight,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
        });
        // AspectRatioContainer 只调整孩子的比例，不把宽度推导的高度交给外层 HBox。
        // 首次排版按卡宽令牌估出内容最小高度，随后只从实际分配宽度更新，不写位置。
        var padding = margin.GetThemeConstant("margin_left") + margin.GetThemeConstant("margin_right");
        var previewWidth = Math.Max(0, (kit.Layout.Metric("cardMinWidth") - padding - body.GetThemeConstant("separation")) / 2f);
        aspect.CustomMinimumSize = new Vector2(0, previewWidth / aspect.Ratio);
        void FitPreviewHeight()
        {
            if (aspect.Size.X > 0) aspect.CustomMinimumSize = new Vector2(0, aspect.Size.X / aspect.Ratio);
        }
        aspect.Resized += FitPreviewHeight;
        var thumbnail = kit.Panel(aspect, new UiFx(Fill: corrupt ? "danger.fill" : "surface.sunk", Line: "line.hair"));
        if (slot.Health == SlotHealth.Ok) TryThumbnail(thumbnail, saves.Thumbnail(slot.Slot));
        var data = kit.Layout.Stack(body, groups: false);
        if (empty)
        {
            data.Alignment = BoxContainer.AlignmentMode.Center;
            kit.Text(data, UiKit.T("saves.empty"), "body", "text.2", align: HorizontalAlignment.Center);
            return;
        }
        var meta = slot.Meta;
        var place = corrupt ? UiKit.T("saves.corrupt") : newer ? UiKit.T("saves.newer") : SaveMetaText.Place(meta!, maps);
        kit.Text(data, place, "h3", corrupt ? "danger.text" : newer ? "text.2" : "text.1");
        if (meta is null) return;
        kit.Text(data, SaveMetaText.Moment(meta), "small", "text.2");
        kit.Text(data, SaveMetaText.PlayTime(meta.PlaySeconds), "data", "text.2");
        kit.Text(data, SaveMetaText.SavedAt(meta.SavedAtUnix), "data", "text.2");
    }

    /// <summary>后台写盘进行中的静态标签；写入动效不在这里做。</summary>
    public void SetWriting(bool writing) => _writing.Visible = writing;

    public void SetSelected(bool selected)
    {
        _kit.Apply(_panel, CardFx(_kit, _empty, selected));
        Hit.Selected = selected;
        foreach (var pin in _contacts)
            _kit.Apply(pin, new UiFx(Fill: selected ? "primary.fill" : null, Line: selected ? "primary.line" : "line.hair"));
    }

    private static UiFx CardFx(UiKit kit, bool empty, bool selected) => new(Fill: empty ? null : "surface.solid",
        Line: selected ? "primary.line" : empty ? "line.control" : "line.panel",
        Chamfer: kit.Tokens.Chamfer("card"), Flags: (empty ? UiFlags.Dashed : 0) | (selected ? UiFlags.SelectedIris : 0));

    private static Label Tag(UiKit kit, Control header, string key, string role)
    {
        var tag = kit.Text(header, UiKit.T(key), "small", role, wrap: false);
        tag.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        return tag;
    }

    private static void TryThumbnail(Control parent, byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return;
        var image = new Image();
        if (image.LoadWebpFromBuffer(bytes) != Error.Ok) return;
        parent.AddChild(new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }

}
