namespace Campus.Contracts.Startup;

public enum BootPhase : byte { BeforeMap, AfterMapWarm, BeforeReveal }

/// <summary>Read-only startup inputs. SaveDirectory is an absolute OS directory resolved by the kernel.</summary>
public sealed record BootContext(
    BootRunOptions Run,
    string SaveDirectory,
    string Build)
{
    public bool CaptureMode => Run.CaptureMode;
}

/// <summary>
/// Implement on an already registered service or node; do not register this interface as a singleton service.
/// The kernel deduplicates instances by reference, rejects duplicate Ids, and calls each phase serially
/// in Order then ordinal Id order. Each instance is called at most once per phase; irrelevant phases return
/// immediately. Exceptions abort startup and propagate to Boot, which keeps the application loading screen fully opaque.
/// </summary>
public interface IBootParticipant
{
    string Id { get; }
    int Order { get; }
    Task Initialize(BootPhase phase, BootContext context);
}

/// <summary>
/// Implement on an existing visible-content node. Readiness means its asynchronous work has been
/// uploaded/mounted and applied for the supplied current capture pose, not merely scheduled.
/// No-content configurations may be ready; a failed producer reports Failure until its next load.
/// </summary>
public interface IViewContentReadiness
{
    bool IsReady(Pose pose);
    Exception? Failure { get; }
}

/// <summary>Optional visible-content capability: discard view history before preparing a requested still.</summary>
public interface IViewCapturePreparation
{
    void PrepareCapture(Pose pose);
}

// BeforeMap: map catalog, save metadata/lock and silent audio resources, before destination selection.
// AfterMapWarm: after Travel(Boot), resident data and viewport preparation; quality detection/application.
// BeforeReveal: after IAppBootFlow.PrepareWorld and explicit command-line world overrides, final ready work.
// The kernel completes world readiness, then releases its presentation; no participant grants input.
