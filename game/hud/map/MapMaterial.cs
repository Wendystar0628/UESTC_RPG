using Campus.Contracts;
using Campus.Kernel.Materials;
using Godot;
using KernelRegistry = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud.Map;

/// <summary>
/// map_base 材质的唯一取用点。只有 Campus.Style 能建 ShaderMaterial（ENG-ARCH §2）：地图经
/// IShaderMaterialSource.Create(ShaderId.MapBase) 向材质库申请（contract-3）。取不到时返回 null，
/// 底图按"未就绪"的画法只画校内用地一种颜色（SYS-ATLAS §2.1），不挡主线。
/// </summary>
public static class MapMaterial
{
    private static readonly StringName PalDay = new("pal_day");
    private static readonly StringName PalNight = new("pal_night");
    private static readonly StringName ThemeFront = new("theme_front");
    private static readonly StringName FrontWidth = new("front_width");
    private static readonly StringName BoundDash = new("bound_dash");
    private static readonly StringName Mode = new("mode");

    public static ShaderMaterial? Acquire()
    {
        try { return KernelRegistry.Get<IShaderMaterialSource>().Create(ShaderId.MapBase); }
        catch (InvalidOperationException)
        {
            GD.PushWarning("WP-48b: IShaderMaterialSource is not registered; the base map draws the single-colour placeholder.");
            return null;
        }
    }

    /// <summary>色表与几何只写一次；主题前沿在交班时写（不逐帧）。</summary>
    public static void Configure(ShaderMaterial m, MapStyle style)
    {
        m.SetShaderParameter(Mode, 0);
        m.SetShaderParameter(PalDay, style.MapDay);
        m.SetShaderParameter(PalNight, style.MapNight);
        m.SetShaderParameter(FrontWidth, style.Metrics.FrontWidth);
        m.SetShaderParameter(BoundDash, new Vector2(style.Metrics.BoundDash, style.Metrics.BoundGap));
    }

    public static void SetThemeFront(ShaderMaterial m, float front) => m.SetShaderParameter(ThemeFront, front);
}
