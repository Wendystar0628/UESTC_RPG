using System.Text.RegularExpressions;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class HudTextKeyTests
{
    // 界面层里名为 T 的成员调用都是取文案（UiKit.T、Tr.T、MapHudContext.T）；第一个实参里的每段完整字面量都是键，
    // 三元与 switch 表达式的各个分支都查；与 "+" 相邻的字面量是拼接出的动态键，插值与逐字字符串也不当键（UI-NAV）。
    private static readonly Regex Call = new(@"\.T\(", RegexOptions.CultureInvariant);

    // 直接写进代码的文案键都要在翻译表的 keys 列里；查不到时界面只显示原始键（GAME-183）。
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LiteralTextKeysExistInTranslationTable()
    {
        var keys = TranslationKeys();
        var hud = RepositoryFixture.FilePath("game", "hud");
        var used = 0;
        var missing = new List<string>();
        foreach (var file in Directory.EnumerateFiles(hud, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Call.Matches(text))
                foreach (var (index, key) in FirstArgumentKeys(text, match.Index + match.Length))
                {
                    used++;
                    if (keys.Contains(key)) continue;
                    var line = text.AsSpan(0, index).Count('\n') + 1;
                    missing.Add($"{Path.GetRelativePath(RepositoryFixture.Root, file)}:{line} {key}");
                }
        }
        AssertThat(used > 0).OverrideFailureMessage("No literal text key was found under game/hud; the scan pattern no longer matches the call form.").IsTrue();
        AssertThat(missing.Count == 0).OverrideFailureMessage("Text keys missing from game/hud/i18n/zh.csv:\n" + string.Join('\n', missing)).IsTrue();
    }

    // 从 T 的左括号之后扫到第一个实参结束（同层的逗号或右括号），收集其中不与 "+" 相邻的普通字符串字面量。
    private static List<(int Index, string Key)> FirstArgumentKeys(string text, int start)
    {
        var keys = new List<(int, string)>();
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}':
                    if (depth == 0) return keys;
                    depth--;
                    break;
                case ',' when depth == 0:
                    return keys;
                case '\'':
                    i = QuotedEnd(text, i, '\'');
                    break;
                case '"':
                    var prefixed = text[i - 1] is '$' or '@';
                    var verbatim = text[i - 1] == '@' || text[i - 1] == '$' && text[i - 2] == '@';
                    var end = verbatim ? VerbatimEnd(text, i) : QuotedEnd(text, i, '"');
                    if (!prefixed && Neighbour(text, i - 1, -1) != '+' && Neighbour(text, end + 1, 1) != '+')
                        keys.Add((i, text[(i + 1)..end]));
                    i = end;
                    break;
            }
        }
        throw new InvalidDataException("Unterminated text call in a game/hud source file.");
    }

    private static int QuotedEnd(string text, int open, char quote)
    {
        for (var i = open + 1; i < text.Length; i++)
        {
            if (text[i] == '\\') i++;
            else if (text[i] == quote) return i;
        }
        throw new InvalidDataException("Unterminated literal in a game/hud source file.");
    }

    private static int VerbatimEnd(string text, int open)
    {
        for (var i = open + 1; i < text.Length; i++)
        {
            if (text[i] != '"') continue;
            if (i + 1 < text.Length && text[i + 1] == '"') i++;
            else return i;
        }
        throw new InvalidDataException("Unterminated verbatim literal in a game/hud source file.");
    }

    // 从 from 起沿 step 方向找第一个非空白字符，找不到时返回 '\0'。
    private static char Neighbour(string text, int from, int step)
    {
        for (var i = from; i >= 0 && i < text.Length; i += step)
            if (!char.IsWhiteSpace(text[i])) return text[i];
        return '\0';
    }

    // 翻译表首行 "keys,zh,en"，键在第一列且不含逗号与引号。
    private static HashSet<string> TranslationKeys()
    {
        var lines = File.ReadAllLines(RepositoryFixture.FilePath("game", "hud", "i18n", "zh.csv"));
        if (lines.Length == 0 || !lines[0].TrimStart('\uFEFF').StartsWith("keys,", StringComparison.Ordinal))
            throw new InvalidDataException("game/hud/i18n/zh.csv must start with the keys column.");
        return lines.Skip(1).Where(line => line.Length > 0).Select(line => line[..line.IndexOf(',')]).ToHashSet(StringComparer.Ordinal);
    }
}
