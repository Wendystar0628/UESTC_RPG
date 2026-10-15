using System;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;

namespace Campus.Contracts;

/// <summary>Opening cinematic setting, written by WP-35 and read by WP-49a.</summary>
public interface ICinematicSettings
{
    CinematicMode Mode { get; }
    event Action? Changed;
}

/// <summary>WP-35d presentation layer; called on the main thread by the cinematic player.</summary>
public interface ICinemaPresenter
{
    void Begin(CinematicDef def, string? placeKey);
    void Present(double timelineSeconds, float skipProgress, bool skipHintVisible, double waitingSeconds);
    void End(CinematicEnd end, bool skipped);
    bool Active { get; }
}

/// <summary>Read-only skip state. WP-06 alone measures input; WP-49a consumes it without another timer.</summary>
public interface ISkipInputState
{
    float Progress { get; }
    bool HintVisible { get; }
}
