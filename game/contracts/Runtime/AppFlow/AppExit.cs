namespace Campus.Contracts.AppFlow;

/// <summary>
/// Engine shutdown boundary owned by the production stage. Interactive departure calls it only
/// after IAppFlow.Quit has allowed departure; startup commands supply their process exit code.
/// The stage unloads maps, stops producers, drains native resources and finally closes the scene tree.
/// Implementations coalesce repeated calls and must never call IAppFlow.Quit recursively.
/// </summary>
public interface IAppExit
{
    /// <summary>Returns the shared shutdown task. Interactive departure awaits it after the flow's decision and save.</summary>
    Task FinishQuit(int exitCode = 0);

    /// <summary>
    /// Synchronously requests the same stage-owned shutdown without waiting for completion.
    /// Startup participants use this boundary so shutdown can drain their initialization tasks.
    /// </summary>
    void RequestQuit(int exitCode = 0);
}
