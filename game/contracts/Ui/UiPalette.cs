using System.Globalization;
using System.Numerics;

namespace Campus.Contracts;

/// <summary>
/// Pure .NET resolver for ui.json. Results are normalized sRGB RGBA, without premultiplication.
/// Names resolve through special, then ramps (split at the first dot), then PaletteCatalog.Albedo.
/// The scene fallback is the registered intrinsic/constant color, not a GPU or current-frame palette read.
/// The reserved name none means do not draw and always resolves to fully transparent black.
/// </summary>
public sealed class UiPalette
{
    private readonly UiPaletteDocument _document;
    private readonly PaletteCatalog _registry;

    public UiPalette(UiPaletteDocument document, PaletteCatalog registry)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(registry);
        _document = document;
        _registry = registry;
    }

    /// <summary>Resolves a name or a serialized color expression, including an optional @alpha multiplier.</summary>
    public Vector4 Resolve(string reference) => ResolveCore(reference, []);

    public Vector4 ResolveTheme(string theme, string token)
    {
        var tokens = Theme(theme);
        if (!tokens.TryGetValue(token, out var reference)) throw Failure($"Unknown UI theme token 'themes.{theme}.{token}'.");
        return ResolveCore(reference, [$"themes.{theme}.{token}"]);
    }

    /// <summary>Components select object members such as fill, line, or spring; scalar map tokens need none.</summary>
    public Vector4 ResolveMap(string theme, string token, params string[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var tokens = Map(theme);
        if (!tokens.TryGetValue(token, out var value)) throw Failure($"Unknown UI map token 'map.{theme}.{token}'.");
        var path = $"map.{theme}.{token}";
        foreach (var component in components)
        {
            path += "." + component;
            if (value.Components is null || !value.Components.TryGetValue(component, out value))
                throw Failure($"Unknown UI map color component '{path}'.");
        }
        if (value.Color is null) throw Failure($"UI token '{path}' is composite; select a color component.");
        return ResolveCore(value.Color, [path]);
    }

    /// <summary>G5 entry point: checks matching theme/map key sets and resolves every declared color leaf.</summary>
    public static void Validate(UiPaletteDocument document, PaletteCatalog registry) => new UiPalette(document, registry).Validate();

    public void Validate()
    {
        if (_document.Ramps is null || _document.Special is null || _document.Themes is null || _document.Map is null)
            throw Failure("UI palette requires ramps, special, themes and map.");
        MatchKeys(_document.Themes.Light, _document.Themes.Dark, "themes");
        MatchKeys(_document.Map.Light, _document.Map.Dark, "map");

        foreach (var group in _document.Ramps.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (group.Value is null) throw Failure($"Missing UI ramp 'ramps.{group.Key}'.");
            foreach (var token in group.Value.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                _ = ResolveCore(token.Value, [$"ramps.{group.Key}.{token.Key}"]);
        }
        foreach (var token in _document.Special.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            ValidateToken(token.Value, $"special.{token.Key}");
        foreach (var theme in new[] { "light", "dark" })
        {
            foreach (var token in Theme(theme).OrderBy(pair => pair.Key, StringComparer.Ordinal))
                _ = ResolveCore(token.Value, [$"themes.{theme}.{token.Key}"]);
            foreach (var token in Map(theme).OrderBy(pair => pair.Key, StringComparer.Ordinal))
                ValidateToken(token.Value, $"map.{theme}.{token.Key}");
        }
    }

    private void ValidateToken(UiColorToken token, string path)
    {
        if (token is null) throw Failure($"Null UI color token at '{path}'.");
        if (token.Color is { } color)
        {
            if (token.Components is not null || token.Scene.HasValue) throw Failure($"UI scalar token '{path}' also has components.");
            _ = ResolveCore(color, [path]);
            return;
        }
        if (token.Components is null || token.Components.Count == 0) throw Failure($"UI token '{path}' has no color components.");
        foreach (var component in token.Components.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            ValidateToken(component.Value, path + "." + component.Key);
    }

    private Vector4 ResolveCore(string reference, List<string> chain)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw Failure($"Empty UI color reference at {Chain(chain)}.");
        var name = reference;
        var alpha = 1f;
        var alphaIndex = reference.LastIndexOf('@');
        if (alphaIndex >= 0)
        {
            name = reference[..alphaIndex];
            if (!float.TryParse(reference.AsSpan(alphaIndex + 1), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out alpha)
                || !float.IsFinite(alpha) || alpha is < 0f or > 1f)
                throw Failure($"Invalid UI alpha multiplier in '{reference}' at {Chain(chain)}.");
        }
        if (name.StartsWith("ui.", StringComparison.Ordinal)) name = name[3..];

        Vector4 color;
        if (name == "none") return Vector4.Zero;
        if (name.StartsWith('#')) color = ParseHex(name, chain);
        else if (_document.Special.TryGetValue(name, out var special))
        {
            if (special.Color is null) throw Failure($"UI special color '{name}' is composite; a scalar reference is required at {Chain(chain)}.");
            color = Follow("special." + name, special.Color, chain);
        }
        else
        {
            var split = name.IndexOf('.');
            if (split > 0 && _document.Ramps.TryGetValue(name[..split], out var ramp) && ramp.TryGetValue(name[(split + 1)..], out var rampValue))
                color = Follow("ramps." + name, rampValue, chain);
            else if (_registry.TryGet(new PaletteToken(name), out var entry) && entry is not null)
                color = new Vector4(entry.Albedo, 1f);
            else throw Failure($"Unresolved UI color '{name}' at {Chain(chain)} (searched special, ramps, PaletteCatalog).");
        }
        color.W *= alpha;
        return color;
    }

    private Vector4 Follow(string key, string reference, List<string> chain)
    {
        if (chain.Contains(key, StringComparer.Ordinal)) throw Failure($"Cyclic UI color reference: {Chain(chain)} -> {key}.");
        chain.Add(key);
        try { return ResolveCore(reference, chain); }
        finally { chain.RemoveAt(chain.Count - 1); }
    }

    private static Vector4 ParseHex(string color, List<string> chain)
    {
        if (color.Length is not (7 or 9)) throw Failure($"Invalid UI color literal '{color}' at {Chain(chain)}.");
        Span<byte> bytes = stackalloc byte[4];
        bytes[3] = byte.MaxValue;
        for (var index = 0; index < (color.Length - 1) / 2; index++)
            if (!byte.TryParse(color.AsSpan(1 + index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[index]))
                throw Failure($"Invalid UI color literal '{color}' at {Chain(chain)}.");
        return new Vector4(bytes[0] / 255f, bytes[1] / 255f, bytes[2] / 255f, bytes[3] / 255f);
    }

    private IReadOnlyDictionary<string, string> Theme(string theme) => theme switch
    {
        "light" => _document.Themes.Light,
        "dark" => _document.Themes.Dark,
        _ => throw Failure($"Unknown UI theme '{theme}'; expected light or dark.")
    };

    private IReadOnlyDictionary<string, UiColorToken> Map(string theme) => theme switch
    {
        "light" => _document.Map.Light,
        "dark" => _document.Map.Dark,
        _ => throw Failure($"Unknown UI map theme '{theme}'; expected light or dark.")
    };

    private static void MatchKeys<T>(IReadOnlyDictionary<string, T> light, IReadOnlyDictionary<string, T> dark, string path)
    {
        if (light is null || dark is null) throw Failure($"UI {path} needs both light and dark dictionaries.");
        var onlyLight = light.Keys.Except(dark.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var onlyDark = dark.Keys.Except(light.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (onlyLight.Length != 0 || onlyDark.Length != 0)
            throw Failure($"UI {path}.light/dark key mismatch; light only: [{string.Join(", ", onlyLight)}]; dark only: [{string.Join(", ", onlyDark)}].");
    }

    private static string Chain(List<string> chain) => chain.Count == 0 ? "<requested color>" : string.Join(" -> ", chain);
    private static FormatException Failure(string message) => new(message);
}
