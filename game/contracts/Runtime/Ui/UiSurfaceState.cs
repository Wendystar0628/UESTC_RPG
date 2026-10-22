using System;

namespace Campus.Contracts;

[Flags]
public enum UiSurface : ushort
{
    None = 0, Minimap = 1, Atlas = 2, AtlasIndex = 4, SettingsCard = 8, SavePage = 16
}

/// <summary>Surfaces that are really open and interactive; changes are published on the main thread only when values change.</summary>
public interface IUiSurfaceState
{
    UiSurface Open { get; }
    /// <summary>The settings card's selected page once its rows are built and the card is interactive; null otherwise.</summary>
    string? SettingsPage { get; }
    long Rev { get; }
    event Action? Changed;
}

/// <summary>Owners report only surfaces that are really open and interactive, and withdraw them when they stop being so.</summary>
public interface IUiSurfaceSink
{
    /// <summary>Reports exactly one of Minimap, Atlas, AtlasIndex or SavePage.</summary>
    void Report(UiSurface surface, bool open);

    /// <summary>Non-null: the settings card's selected page (its existing page identity) is built and interactive; null: card closed or page not ready.</summary>
    void ReportSettings(string? page);
}
