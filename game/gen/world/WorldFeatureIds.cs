using System.Globalization;

namespace Campus.Gen.World;

/// <summary>The three map identities derived from one OSM member. Zone catalogues supply the map prefix and number once.</summary>
public readonly record struct WorldFeatureIds(string Entity, string Part, string Source)
{
    public static WorldFeatureIds Way(string prefix, long number) => Create(prefix, "way", number);
    public static WorldFeatureIds Relation(string prefix, long number) => Create(prefix, "relation", number);
    public static WorldFeatureIds Node(string prefix, long number) => Create(prefix, "node", number);

    /// <summary>Retain the loader's authored source suffix verbatim; it may be a nonnumeric source token.</summary>
    public static WorldFeatureIds FromPart(string prefix, string part)
    {
        var suffix = part[part.IndexOf(':')..];
        return new(prefix + suffix, part, prefix + ":source" + suffix);
    }

    public static bool TryFromSource(string prefix, string source, out WorldFeatureIds ids) =>
        TryParse(prefix, source, prefix + ":source:", out ids);

    public static bool TryWayFromSource(string prefix, string source, out WorldFeatureIds ids)
    {
        if (source.StartsWith(prefix + ":source:way:", StringComparison.Ordinal)) return TryFromSource(prefix, source, out ids);
        ids = default;
        return false;
    }

    private static bool TryParse(string prefix, string value, string start, out WorldFeatureIds ids)
    {
        ids = default;
        if (string.IsNullOrWhiteSpace(prefix) || prefix != prefix.Trim() || prefix.Contains(':') || !value.StartsWith(start, StringComparison.Ordinal)) return false;
        var suffix = value[start.Length..].Split(':');
        if (suffix.Length != 2 || suffix[0] is not ("way" or "relation" or "node")
            || !long.TryParse(suffix[1], NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < 0 || number.ToString(CultureInfo.InvariantCulture) != suffix[1]) return false;
        ids = Create(prefix, suffix[0], number);
        return true;
    }

    private static WorldFeatureIds Create(string prefix, string kind, long number)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix != prefix.Trim() || prefix.Contains(':') || number < 0)
            throw new ArgumentException("An OSM identity needs a map prefix and a non-negative source number.");
        var suffix = kind + ':' + number.ToString(CultureInfo.InvariantCulture);
        return new(prefix + ':' + suffix, "osm:" + suffix, prefix + ":source:" + suffix);
    }
}
