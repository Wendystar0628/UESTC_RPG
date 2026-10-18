namespace Campus.Contracts.Settings;

/// <summary>UI settings also consumed before the HUD is constructed.</summary>
public static class UiSettingDefinitions
{
    public static readonly SettingDefinition<float> Scale =
        new("ui_scale", 1f, "settings.ui_scale", new(0.75, 1.5, 0.05));
    public static readonly SettingDefinition<float> FlyFov =
        new("fly_fov", 60f, "settings.fly_fov", new(50, 80, 1));
    public static readonly SettingDefinition<float> WalkFov =
        new("walk_fov", 70f, "settings.walk_fov", new(50, 90, 1));
    public static readonly SettingDefinition<int> ViewBobbing =
        new("view_bobbing", 50, "settings.view_bobbing", Options:
            [new(0, "ui.common.off"), new(50, "settings.view_bobbing.half"), new(100, "settings.view_bobbing.full")]);
    public static readonly SettingDefinition<bool> AutoJump =
        new("auto_jump", false, "settings.auto_jump");
}
