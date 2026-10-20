using KernelServices = Campus.Kernel.Registry.Kernel;
using Campus.Contracts.Diagnostics;
using Godot;

namespace Campus.Hud.I18n;

/// <summary>
/// 界面文字一律写稳定键 &lt;域&gt;.&lt;对象&gt;.&lt;用途&gt;（UI-NAV），翻译表在 game/hud/i18n/&lt;语言&gt;.csv（引擎 CSV 翻译导入，project.godot 注册）：
/// 首行 "keys,zh,en"，一句话一条，变量写成 {name} 这样的具名占位，不拼句。中英文共用一张表；查不到的键原样返回并告警一次。
/// </summary>
public static class Tr
{
    private static readonly HashSet<string> Missing = new(StringComparer.Ordinal);

    public static string T(string key)
    {
        var text = TranslationServer.Translate(key).ToString();
        if (text != key) return text;
        if (Missing.Add(key)) KernelServices.Get<IDiagnosticLog>().Warning("UI translation", $"UI string key missing: {key}");
        return key;
    }

    public static string T(string key, params (string Name, string Value)[] args)
    {
        var text = T(key);
        foreach (var (name, value) in args) text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        return text;
    }

    public static bool Has(string key) => TranslationServer.Translate(key).ToString() != key;
}
