using Campus.Contracts.AppFlow;

namespace Campus.Contracts;

/// <summary>One fully covering loading screen for application entry, restore and return to title.</summary>
public interface IAppLoadingPresentation
{
    bool Active { get; }
    bool IsCovered { get; }
    float Progress { get; }
    /// <summary>Null returns to title; completion means the opaque screen has been presented.</summary>
    Task Show(EnterRequest? request = null);
    /// <summary>Reports completed real preparation items; the caller reports full only after all work is ready.</summary>
    void Report(int completed, int total);
    /// <summary>Waits for the configured loading-screen fade after full readiness.</summary>
    Task Hide();
}
