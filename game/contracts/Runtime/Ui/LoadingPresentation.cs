using Campus.Contracts.AppFlow;

namespace Campus.Contracts;

/// <summary>The single loading screen, shared by independent application operations.</summary>
public interface IAppLoadingPresentation
{
    bool Active { get; }
    bool IsCovered { get; }
    float Progress { get; }
    /// <summary>Acquires ownership synchronously. A null request denotes a general loading operation.</summary>
    IAppLoadingLease Show(EnterRequest? request = null);
}

/// <summary>Releasing one operation never reveals a screen still owned by another operation.</summary>
public interface IAppLoadingLease : IAsyncDisposable
{
    /// <summary>Completes after the opaque loading screen has actually been presented.</summary>
    Task Covered { get; }
    void Report(int completed, int total);
    /// <summary>Idempotently releases this lease; only the final release awaits the screen fade.</summary>
    Task Hide();
}

/// <summary>Actual fully opaque coverage by the frontispiece or loading screen; false before any fade-out.</summary>
public interface IAppScreenCoverage
{
    bool IsCovered { get; }
}

public interface IAppFrontispiecePresentation
{
    Task Show();
    Task Hide(float seconds);
}

/// <summary>Crossfades a captured previous scene into the current scene without covering it with a solid layer.</summary>
public interface IWorldTransitionPresentation
{
    void CapturePreviousFrame();
    Task Crossfade(float seconds);
}
