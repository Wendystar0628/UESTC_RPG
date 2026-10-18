namespace Campus.Contracts.Settings;

/// <summary>One setting's persisted key, default and presentation metadata.</summary>
public sealed record SettingDefinition<T>(
    string Key,
    T Default,
    string LabelKey,
    SettingRange? Range = null,
    IReadOnlyList<SettingOption<T>>? Options = null);

public sealed record SettingRange(double Min, double Max, double Step);
public sealed record SettingOption<T>(T Value, string LabelKey);

/// <summary>Reads use the same definition as the owning domain and its settings UI.</summary>
public static class SettingsSectionDefinitions
{
    public static bool Get(this ISettingsSection section, SettingDefinition<bool> definition) =>
        section.Bool(definition.Key, definition.Default);

    public static int Get(this ISettingsSection section, SettingDefinition<int> definition)
    {
        int value = section.Int(definition.Key, definition.Default,
            definition.Range is { } range ? (int)range.Min : int.MinValue,
            definition.Range is { } bounds ? (int)bounds.Max : int.MaxValue);
        return definition.Options is { } options && !options.Any(option => option.Value == value)
            ? definition.Default : value;
    }

    public static float Get(this ISettingsSection section, SettingDefinition<float> definition) =>
        section.Float(definition.Key, definition.Default,
            definition.Range is { } range ? (float)range.Min : float.MinValue,
            definition.Range is { } bounds ? (float)bounds.Max : float.MaxValue);

    public static string? Get(this ISettingsSection section, SettingDefinition<string?> definition) =>
        section.String(definition.Key) ?? definition.Default;
}
