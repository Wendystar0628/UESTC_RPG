namespace Campus.Contracts.AppFlow;

/// <summary>
/// Engine shutdown boundary called only by IAppFlow.Quit after saving has allowed departure.
/// The stage unloads maps, stops producers, drains native resources and finally closes the scene tree.
/// Implementations coalesce repeated calls and must never call IAppFlow.Quit recursively.
/// </summary>
public interface IAppExit
{
    Task FinishQuit();
}
