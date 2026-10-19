using Campus.Contracts;

namespace Campus.Gen.Sites;

/// <summary>Identical material construction shared by the zone tunes; each zone still chooses its own families.</summary>
public static class ZoneLooks
{
    public static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
        new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

    public static ToonSpec Glass { get; } = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
}
