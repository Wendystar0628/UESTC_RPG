namespace Campus.Contracts.AppFlow;

/// <summary>Stable startup diagnostics. The UI owns all visible and copied wording.</summary>
public sealed record AppStartupFailure(string Code, string Phase, string Build, string LogDirectory);

public static class AppStartupFailureCodes
{
    public const string Installation = "STARTUP_INSTALLATION";
    public const string DataInvalid = "STARTUP_DATA_INVALID";
    public const string Stalled = "STARTUP_STALLED";
    public const string MapPreparation = "STARTUP_MAP_PREPARATION";
    public const string BakeFailed = "STARTUP_BAKE_FAILED";
    public const string Unexpected = "STARTUP_UNEXPECTED";
}

/// <summary>A terminal preparation source supplies its code and retains its original diagnostic cause.</summary>
public sealed class AppStartupException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}

/// <summary>The production stage owns this one failure session. Commands run on its main thread and coalesce.</summary>
public interface IAppStartupFailureSession
{
    AppStartupFailure Failure { get; }
    /// <summary>Only accepted after presentation acknowledgement; requests native restart, then unified shutdown.</summary>
    Task Retry();
    /// <summary>Unified shutdown with exit code 2; no application quit/save prompt.</summary>
    Task Quit();
}

/// <summary>Register on the existing global UI node, not a second UI or backend service.</summary>
public interface IAppStartupFailurePresentation
{
    /// <summary>
    /// Returns true only after this session's card has actually been drawn on the live tree and its
    /// retry/quit keyboard and pointer input has taken ownership. Service existence or scheduling
    /// display is not acknowledgement. Return false immediately if uninitialized/unavailable;
    /// propagate display exceptions. The caller then records the failure and closes uniformly.
    /// Supersede frontispiece/loading and normal title input; keep ownership until tree exit.
    /// Losing the acknowledged page before exit must call session.Quit(), never leave an empty window.
    /// </summary>
    Task<bool> Show(IAppStartupFailureSession session);
}
