using System.Text.RegularExpressions;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class HudTextKeyTests
{
    // 界面层里名为 T 的成员调用都是取文案（UiKit.T、Tr.T、MapHudContext.T）；只认第一个实参是整段字面量的，
    // 后面紧跟 "+" 或插值的动态键不在此列（UI-NAV）。
    private static readonly Regex LiteralKey = new(@"\.T\(\s*""(?<key>[^""\\]*)""\s*[,)]", RegexOptions.CultureInvariant);

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
            foreach (Match match in LiteralKey.Matches(text))
            {
                used++;
                var key = match.Groups["key"].Value;
                if (keys.Contains(key)) continue;
                var line = text.AsSpan(0, match.Index).Count('\n') + 1;
                missing.Add($"{Path.GetRelativePath(RepositoryFixture.Root, file)}:{line} {key}");
            }
        }
        AssertThat(used > 0).OverrideFailureMessage("No literal text key was found under game/hud; the scan pattern no longer matches the call form.").IsTrue();
        AssertThat(missing.Count == 0).OverrideFailureMessage("Text keys missing from game/hud/i18n/zh.csv:\n" + string.Join('\n', missing)).IsTrue();
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
