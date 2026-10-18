namespace Campus.Contracts;

public enum UiThemeMode : byte { Auto, Light, Dark }
public enum MotionLevel : byte { Full, Reduced, Instant }

/// <summary>Shared UI day/night theme and the current switching front.</summary>
public interface IUiTheme
{
    bool Dark { get; }
    float Front { get; }
    event Action<bool>? Switched;
}
