using System;

namespace Campus.Contracts;

[Flags]
public enum UiSurface : ushort
{
    None = 0, Minimap = 1, Atlas = 2, AtlasIndex = 4, SettingsCard = 8, SavePage = 16, TitleMenu = 32,
    Dialog = 64, TimeCard = 128, WeatherStrip = 256
}

/// <summary>Visible, interactive surfaces; changes are published on the main thread only when values change.</summary>
public interface IUiSurfaceState
{
    UiSurface Open { get; }
    bool InterfaceMode { get; }
    string? SettingsPage { get; }
    long Rev { get; }
    event Action? Changed;
}

public interface IUiSurfaceSink
{
    /// <summary>Reports exactly one of Minimap, Atlas or AtlasIndex.</summary>
    void Report(UiSurface surface, bool open);
}
