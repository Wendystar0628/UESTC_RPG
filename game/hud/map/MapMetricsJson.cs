using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Hud.Map;

// ui_metrics.json 里地图界面读的那几段。文件还有别的读取方（UiTokens）和大量与地图无关的段，
// 所以这里不开“不许未知字段”；缺键、类型不对由必填检查拦下，错误信息带键路径。
internal sealed record UiMetricsDoc(MetricsDoc Metrics, ThemeSwitchDoc ThemeSwitch, MotionDoc Motion);

internal sealed record MetricsDoc(MapGeometryDoc Map, ShapeDoc Shape, LineDoc Line, GlowDoc Glow, TypeDoc Type);

internal sealed record MapGeometryDoc(
    [property: JsonPropertyName("map.bound")] BoundDoc Bound,
    [property: JsonPropertyName("map.label.halo")] HaloDoc LabelHalo,
    [property: JsonPropertyName("map.player")] PlayerDoc Player);

internal sealed record BoundDoc(float[] Dash);
internal sealed record HaloDoc(float Width);
internal sealed record PlayerDoc(float LineWidth, PlayerGlowDoc Glow);
internal sealed record PlayerGlowDoc(float Dark);

internal sealed record ThemeSwitchDoc(float FrontWidth);
internal sealed record ShapeDoc(ChamferDoc Chamfer, float SignatureVia);
internal sealed record ChamferDoc(float Page, float Panel, float Card);
internal sealed record LineDoc(float IconStroke, float Indicator);
internal sealed record GlowDoc(float Max);

internal sealed record MotionDoc(DurationsDoc Durations, ReducedDoc Reduced);
internal sealed record DurationsDoc(float Scrim, PageDoc Page);
internal sealed record PageDoc(float[] In, float[] Out);
internal sealed record ReducedDoc(float FadeMax);

internal sealed record TypeDoc(
    Dictionary<string, FamilyDoc> Families, Dictionary<string, string[]> Chains, Dictionary<string, RoleDoc> Roles);
internal sealed record FamilyDoc(string Name, Dictionary<string, string> Files);
internal sealed record RoleDoc(string Chain, string Weight, int Size, float Tracking, bool Upper = false);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UiMetricsDoc))]
internal partial class MapMetricsJson : JsonSerializerContext
{
    private static readonly MapMetricsJson Strict = new(new JsonSerializerOptions
    {
        // 传了运行时选项，特性上的命名策略不再生效，这里要再写一次
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    });

    /// <summary>读并校验：System.Text.Json 管缺键与类型，数组长度与跨段引用在这里补。</summary>
    internal static UiMetricsDoc Parse(ReadOnlySpan<byte> utf8)
    {
        var doc = JsonSerializer.Deserialize(utf8, Strict.UiMetricsDoc) ?? throw new JsonException("ui_metrics.json is empty.");
        Need(doc.Metrics.Map.Bound.Dash.Length >= 2, "metrics.map.map.bound.dash", "needs [dash, gap]");
        Need(doc.Motion.Durations.Page.In.Length >= 2, "motion.durations.page.in", "needs [min, max]");
        Need(doc.Motion.Durations.Page.Out.Length >= 1, "motion.durations.page.out", "needs at least one value");
        var type = doc.Metrics.Type;
        foreach (var (name, role) in type.Roles)
        {
            var path = "metrics.type.roles." + name;
            Need(type.Chains.TryGetValue(role.Chain, out var chain) && chain.Length > 0, path + ".chain", $"chain '{role.Chain}' is missing or empty");
            foreach (var family in chain!)
            {
                Need(type.Families.TryGetValue(family, out var f), "metrics.type.chains." + role.Chain, $"family '{family}' is not defined");
                Need(f!.Files.ContainsKey(role.Weight) || f.Files.ContainsKey("Regular"), "metrics.type.families." + family + ".files",
                    $"no file for weight '{role.Weight}' and no Regular fallback");
            }
        }
        return doc;
    }

    private static void Need(bool ok, string path, string message)
    {
        if (!ok) throw new JsonException($"ui_metrics.json: {message}. Path: $.{path}");
    }
}
