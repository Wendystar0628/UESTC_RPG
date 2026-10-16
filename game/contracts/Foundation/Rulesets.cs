using System.Numerics;
using Campus.Contracts.AppFlow;

namespace Campus.Contracts;

public readonly record struct RulesetId(string Value);
[Flags]
public enum WorldControlMask : byte { None = 0, Clock = 1, Season = 2, Weather = 4, Flow = 8, All = 15 }
public enum InteractionRule : byte { None }
public sealed record RulesetDescriptor(
    RulesetId Id,
    IReadOnlyList<ViewMode> ViewModes,
    WorldControlMask WorldControls,
    string UnlockPolicy,
    IReadOnlyList<string> Incidents,
    IReadOnlyList<AppState> CinematicStates)
{
    public bool Characters { get; init; }
    /// <summary>Permits the player presentation independently of NPCs; runtime release/body gates still apply.</summary>
    public bool PlayerBody { get; init; }
    public InteractionRule Interaction { get; init; }
    public bool ClearVoices { get; init; }
}
public interface IRuleset
{
    RulesetDescriptor Current { get; }
    long Rev { get; }
}
/// <summary>Restricted activation entry point for new-game and restore orchestration; rule data remains kernel-owned.</summary>
public interface IRulesetActivation
{
    void Select(RulesetId id);
}
public interface IObserverState
{
    Vector3 Feet { get; }
    Vector3 Eye { get; }
    float YawDeg { get; }
    NavSpace? Space { get; }
    ViewMode Mode { get; }
}
