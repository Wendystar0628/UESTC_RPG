namespace Campus.Contracts.Settings;

/// <summary>UI settings also consumed before the HUD is constructed.</summary>
public static class UiSettingDefinitions
{
    public static readonly SettingDefinition<float> Scale =
        new("ui_scale", 1f, "settings.ui_scale", new(0.75, 1.5, 0.05));
}
