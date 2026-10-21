using Campus.Tests;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Style.Palette.Keys;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests.Style;

/// <summary>
/// lit.heroGuard of the game/style/palette/keys/ colour script: yellow-orange hero colours never turn past their hero-frame hue towards green and
/// keep a chroma floor in dim light; every other colour derives exactly as without the guard. Pure .NET: reads the key data.
/// </summary>
[TestSuite]
public sealed class HeroGuardTests
{
    private static readonly double[] Clocks = [21.0, 19.0, 6.5];

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void YellowHeroesNeverTurnGreenerThanTheHeroFrame()
    {
        var script = Script(false);
        var floor = DeriveNode()["lit"]!["heroGuard"]!["chromaFloor"]!.GetValue<double>();
        var ratio = DeriveNode()["lit"]!["chromaCap"]!["ratio"]!.GetValue<double>();
        foreach (var token in script.Vegetation.Entries.Values
            .Where(e => e.Hero[(int)Season.Autumn].Guard).Select(e => e.Token))
            foreach (var clock in Clocks)
            {
                var (m, hero) = Colours(script, token, Season.Autumn, clock);
                var lit = m.Lit;
                var cmin = Math.Min(floor * hero.CRef, m.Shade.Chroma / ratio);
                AssertThat(hero.Guard).OverrideFailureMessage($"{token}: autumn hero hue {hero.HRef:F1} should be guarded.").IsTrue();
                AssertThat(Oklab.HueDiff(hero.HRef, lit.Hue) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"{token} {clock}: lit hue {lit.Hue:F2} is greener than the hero hue {hero.HRef:F2}.").IsTrue();
                AssertThat(lit.Chroma >= cmin - TestTolerance.FloatRelative).OverrideFailureMessage($"{token} {clock}: lit C {lit.Chroma:F4} is below the floor {cmin:F4}.").IsTrue();
            }
        foreach (var e in script.Vegetation.Entries.Values)
            foreach (var season in Enum.GetValues<Season>())
            {
                var hero = e.Hero[(int)season];
                if (!hero.Guard) continue;
                for (var hour = 0; hour < 24; hour++)
                {
                    var lit = Lit(script, e.Token, season, hour).Lit;
                    AssertThat(Oklab.HueDiff(hero.HRef, lit.Hue) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"{e.Token} {season} {hour}:00: lit hue {lit.Hue:F2} past hero {hero.HRef:F2}.").IsTrue();
                }
            }
    }

    // The floor never passes the chromaCap at any hour: shade C ≥ lit C × ratio for every guarded colour with albedo C ≥ minAlbedoC.
    [TestCase]
    [TestCategory(TestReason.Contract)]
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
                    AssertThat(m.Lit.Chroma <= m.Shade.Chroma / ratio + TestTolerance.FloatRelative).OverrideFailureMessage($"{e.Token} {season} {hour}: lit C {m.Lit.Chroma:F4} passes the cap {m.Shade.Chroma / ratio:F4}.").IsTrue();
                    checkedCount++;
                }
            }
        AssertThat(checkedCount > 0).OverrideFailureMessage("No guarded colours were checked.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OtherColoursAreUnchanged()
    {
        var on = Script(false);
        var off = Script(true);
        var guarded = on.Vegetation.Entries.Values.Where(e => e.Hero.Any(h => h.Guard)).Select(e => e.Token).Order(StringComparer.Ordinal).ToArray();
        AssertThat(guarded.Length > 0).OverrideFailureMessage("No guarded colours were loaded.").IsTrue();
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
                    AssertThat(a == b).OverrideFailureMessage($"{e.Token} {season} {hour}: {Oklab.ToHex(a.Lit)} / {Oklab.ToHex(b.Lit)} changed by the guard.").IsTrue();
                    checkedCount++;
                }
            }
        AssertThat(checkedCount > 0).OverrideFailureMessage("No unguarded colours were checked.").IsTrue();
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
        var keys = Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette", "keys");
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
        JsonNode.Parse(File.ReadAllBytes(Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette", "keys", "derive.json")))!;
}
