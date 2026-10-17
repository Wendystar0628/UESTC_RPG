using Godot;

namespace Campus.Hud.I18n;

/// <summary>
/// 界面文字一律写稳定键 &lt;域&gt;.&lt;对象&gt;.&lt;用途&gt;（UI-NAV），翻译表在 game/hud/i18n/&lt;语言&gt;.csv：
/// 首行 "keys,&lt;语言&gt;"，一句话一条，变量写成 {name} 这样的具名占位，不拼句。主线版只有中文表；查不到的键原样返回并告警一次。
/// </summary>
public static class Tr
{
    private static readonly Dictionary<string, string> Table = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Missing = new(StringComparer.Ordinal);
    private static bool _loaded;

    public static string T(string key)
    {
        if (!_loaded) Load("zh");
        if (Table.TryGetValue(key, out var text)) return text;
        if (Missing.Add(key)) GD.PushWarning($"UI string key missing: {key}");
        return key;
    }

    public static string T(string key, params (string Name, string Value)[] args)
    {
        var text = T(key);
        foreach (var (name, value) in args) text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        return text;
    }

    public static bool Has(string key)
    {
        if (!_loaded) Load("zh");
        return Table.ContainsKey(key);
    }

    private static void Load(string language)
    {
        _loaded = true;
        var text = Godot.FileAccess.GetFileAsString($"res://hud/i18n/{language}.csv");
        var first = true;
        foreach (var row in Rows(text))
        {
            if (first) { first = false; continue; }
            if (row.Count < 2 || row[0].Length == 0) continue;
            Table[row[0]] = row[1].Replace("\\n", "\n", StringComparison.Ordinal);
        }
    }

    // 最小的 CSV 读法：逗号分列，双引号包住的字段里可以有逗号和成对的双引号。
    private static IEnumerable<List<string>> Rows(string text)
    {
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; break;
                case ',': row.Add(field.ToString()); field.Clear(); break;
                case '\r': break;
                case '\n':
                    row.Add(field.ToString()); field.Clear();
                    yield return row;
                    row = [];
                    break;
                default: field.Append(c); break;
            }
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row; }
    }
}
