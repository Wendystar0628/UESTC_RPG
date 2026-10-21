namespace Campus.Contracts.AppFlow;

/// <summary>
/// Engine shutdown boundary owned by the production stage. Interactive departure calls it only
/// after IAppFlow.Quit has allowed departure; startup commands supply their process exit code.
/// The stage unloads maps, stops producers, drains native resources and finally closes the scene tree.
/// Implementations coalesce repeated calls and must never call IAppFlow.Quit recursively.
/// </summary>
public interface IAppExit
{
    Task FinishQuit(int exitCode = 0);
}
