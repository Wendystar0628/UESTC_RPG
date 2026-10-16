using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Style.Palette.Keys;
using GdUnit4;

namespace Campus.Tests.Style;

/// <summary>
/// lit.heroGuard of the WP-37 colour script: yellow-orange hero colours never turn past their hero-frame hue towards green and
/// keep a chroma floor in dim light; every other colour derives exactly as without the guard. Pure .NET: reads the key data.
/// </summary>
[TestSuite]
public sealed class HeroGuardTests
{
    private static readonly double[] Clocks = [21.0, 19.0, 6.5];

    [TestCase]
    public void YellowHeroesNeverTurnGreenerThanTheHeroFrame()
    {
        var script = Script(false);
        var floor = DeriveNode()["lit"]!["heroGuard"]!["chromaFloor"]!.GetValue<double>();
        var ratio = DeriveNode()["lit"]!["chromaCap"]!["ratio"]!.GetValue<double>();
        foreach (var token in new[] { "ginkgo", "deciduous" })
            foreach (var clock in Clocks)
            {
                var (m, hero) = Colours(script, token, Season.Autumn, clock);
                var lit = m.Lit;
                var cmin = Math.Min(floor * hero.CRef, m.Shade.Chroma / ratio);
                Check(hero.Guard, $"{token}: autumn hero hue {hero.HRef:F1} should be guarded.");
                Check(Oklab.HueDiff(hero.HRef, lit.Hue) <= 1e-9, $"{token} {clock}: lit hue {lit.Hue:F2} is greener than the hero hue {hero.HRef:F2}.");
                Check(lit.Chroma >= cmin - 1e-9, $"{token} {clock}: lit C {lit.Chroma:F4} is below the floor {cmin:F4}.");
            }
        foreach (var e in script.Vegetation.Entries.Values)
            foreach (var season in Enum.GetValues<Season>())
            {
                var hero = e.Hero[(int)season];
                if (!hero.Guard) continue;
                for (var hour = 0; hour < 24; hour++)
                {
                    var lit = Lit(script, e.Token, season, hour).Lit;
                    Check(Oklab.HueDiff(hero.HRef, lit.Hue) <= 1e-9, $"{e.Token} {season} {hour}:00: lit hue {lit.Hue:F2} past hero {hero.HRef:F2}.");
                }
            }
    }

    // Same colours as the Python reference (pipeline/colorscript); at 21:00 the floor is held by the chromaCap, next to the reviewed #937F4F.
    [TestCase]
    public void GinkgoMatchesTheReference()
    {
        var script = Script(false);
        foreach (var (clock, hex) in new[] { (21.0, "#937F50"), (19.0, "#A4905F"), (6.5, "#A08C5B") })
        {
            var actual = Oklab.ToHex(Lit(script, "ginkgo", Season.Autumn, clock).Lit);
            Check(actual == hex, $"ginkgo autumn {clock}: {actual}, expected {hex}.");
        }
    }

    // The floor never passes the chromaCap at any hour: shade C ≥ lit C × ratio for every guarded colour with albedo C ≥ minAlbedoC.
    [TestCase]
    public void FloorNeverPassesTheChromaCap()
    {
        var script = Script(false);
        var cap = DeriveNode()["lit"]!["chromaCap"]!;
        var ratio = cap["ratio"]!.GetValue<double>();
        var minC = cap["minAlbedoC"]!.GetValue<double>();
        var checkedCount = 0;
        foreach (var e in script.Vegetation.Entries.Values)
            foreach (var season in Enum.GetValues<Season>())
            {
                if (!e.Hero[(int)season].Guard || e.Albedo[(int)season].Chroma < minC) continue;
                for (var hour = 0.0; hour < 24.0; hour += 0.5)
                {
                    var m = Colours(script, e.Token, season, hour).Colors;
                    Check(m.Lit.Chroma <= m.Shade.Chroma / ratio + 1e-9, $"{e.Token} {season} {hour}: lit C {m.Lit.Chroma:F4} passes the cap {m.Shade.Chroma / ratio:F4}.");
                    checkedCount++;
                }
            }
        Check(checkedCount > 500, $"only {checkedCount} cases checked.");
    }

    [TestCase]
    public void OtherColoursAreUnchanged()
    {
        var on = Script(false);
        var off = Script(true);
        string[] expected = ["bloom.yellow", "camphor.red", "cherry", "crop", "deciduous", "ginkgo", "lotus", "metaseq", "reed"];
        var guarded = on.Vegetation.Entries.Values.Where(e => e.Hero.Any(h => h.Guard)).Select(e => e.Token).Order(StringComparer.Ordinal).ToArray();
        Check(guarded.SequenceEqual(expected), $"guarded tokens: {string.Join(", ", guarded)}.");
        var checkedCount = 0;
        foreach (var e in on.Vegetation.Entries.Values)
            foreach (var season in Enum.GetValues<Season>())
            {
                if (e.Hero[(int)season].Guard) continue;
                for (var hour = 0.0; hour < 24.0; hour += 0.5)
                {
                    var w = on.EvaluateWorld(hour, (double)season, null, default(WeatherVec));
                    var a = on.Deriver.Material(e.Albedo[(int)season], e.Class, w.Frame, w.X, e.Hero[(int)season]);
                    var b = off.Deriver.Material(off.Vegetation.Entries[e.Token].Albedo[(int)season], e.Class, w.Frame, w.X, off.Vegetation.Entries[e.Token].Hero[(int)season]);
                    Check(a == b, $"{e.Token} {season} {hour}: {Oklab.ToHex(a.Lit)} / {Oklab.ToHex(b.Lit)} changed by the guard.");
                    checkedCount++;
                }
            }
        Check(checkedCount > 1000, $"only {checkedCount} cases checked.");
    }

    private static (Lab Lit, HeroRef Hero) Lit(ColorScript script, string token, Season season, double clock)
    {
        var (m, hero) = Colours(script, token, season, clock);
        return (m.Lit, hero);
    }

    private static (MaterialColors Colors, HeroRef Hero) Colours(ColorScript script, string token, Season season, double clock)
    {
        var e = script.Vegetation.Entries[token];
        var w = script.EvaluateWorld(clock, (double)season, null, default(WeatherVec));
        return (script.Deriver.Material(e.Albedo[(int)season], e.Class, w.Frame, w.X, e.Hero[(int)season]), e.Hero[(int)season]);
    }

    private static ColorScript Script(bool guardOff)
    {
        var keys = Path.Combine(RepositoryRoot(), "game", "style", "palette", "keys");
        byte[] Read(string name) => File.ReadAllBytes(Path.Combine(keys, name));
        var derive = Read("derive.json");
        if (guardOff)
        {
            var node = DeriveNode();
            node["lit"]!["heroGuard"]!["minHeroC"] = 10.0;
            derive = System.Text.Encoding.UTF8.GetBytes(node.ToJsonString());
        }
        return new ColorScript(ColorScriptData.Load(Read("autumn.keys.json"), derive, Read("seasons.json"), Read("vegetation.json"), Read("weather.json")));
    }

    private static JsonNode DeriveNode() =>
        JsonNode.Parse(File.ReadAllBytes(Path.Combine(RepositoryRoot(), "game", "style", "palette", "keys", "derive.json")))!;

    private static string RepositoryRoot()
    {
        if (Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } root) return root;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "game", "style", "palette", "core.json"))) return directory.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
