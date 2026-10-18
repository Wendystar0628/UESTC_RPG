using System.Globalization;
using Campus.Contracts.Startup;
using Campus.Hud.Motion;

namespace Campus.Hud;

/// <summary>
/// 界面读的启动参数（只含显式给出的），在 BeforeMap 阶段由 <see cref="From"/> 从 BootContext 取出；
/// 取出之前（含单元测试）是 <see cref="None"/>，即一个参数也没给。值的合法性由内核解析时保证。
/// </summary>
public sealed record HudArguments(string? Ui, string? UiGallery, float? UiScale, string? ExportTheme, MotionLevel? Motion, UiThemeMode? UiTheme)
{
    public static readonly HudArguments None = new(null, null, null, null, null, null);

    public bool UiHidden => Ui == "0";

    public static HudArguments From(BootContext context)
    {
        string? Get(string key) => context.ExplicitArguments.Contains(key) ? context.Arguments[key] : null;
        return new HudArguments(
            Get("ui"),
            Get("ui-gallery"),
            Get("ui-scale") is { } scale ? float.Parse(scale, NumberStyles.Float, CultureInfo.InvariantCulture) : null,
            Get("ui-export-theme") switch
            {
                null => null,
                "1" => "res://hud/ui_theme.tres",
                var path => path,
            },
            Get("motion") switch
            {
                null => null,
                "full" => MotionLevel.Full,
                "reduced" => MotionLevel.Reduced,
                "instant" => MotionLevel.Instant,
                var other => throw new ArgumentException($"Unknown --motion value: {other}"),
            },
            Get("ui-theme") switch
            {
                null => null,
                "auto" => UiThemeMode.Auto,
                "light" => UiThemeMode.Light,
                "dark" => UiThemeMode.Dark,
                var other => throw new ArgumentException($"Unknown --ui-theme value: {other}"),
            });
    }
}
