namespace Campus.Contracts.Startup;

public enum BootPhase : byte { BeforeMap, AfterMapWarm, BeforeReveal }

/// <summary>Read-only startup inputs. SaveDirectory is an absolute OS directory resolved by the kernel.</summary>
public sealed record BootContext(
    IReadOnlyDictionary<string, string> Arguments,
    IReadOnlySet<string> ExplicitArguments,
    string SaveDirectory,
    string Build,
    bool CaptureMode);

/// <summary>
/// Implement on an already registered service or node; do not register this interface as a singleton service.
/// The kernel deduplicates instances by reference, rejects duplicate Ids, and calls each phase serially
/// in Order then ordinal Id order. Each instance is called at most once per phase; irrelevant phases return
/// immediately. Exceptions abort startup and propagate to Boot, which keeps the veil fully opaque.
/// </summary>
public interface IBootParticipant
{
    string Id { get; }
    int Order { get; }
    Task Initialize(BootPhase phase, BootContext context);
}

// BeforeMap: map catalog, save metadata/lock and silent audio resources, before destination selection.
// AfterMapWarm: after Travel(Boot), resident data, viewport and 3D/2D warmup; quality detection/application.
// BeforeReveal: after IAppBootFlow.PrepareWorld and explicit command-line world overrides, final ready work.
// The kernel performs the last Reveal, then IAppBootFlow.CompleteBoot; no participant reveals or grants input.
