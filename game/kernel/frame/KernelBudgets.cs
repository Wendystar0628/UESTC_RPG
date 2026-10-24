namespace Campus.Kernel.Frame;

/// <summary>
/// The single home of the runtime ENG-BUDGET numbers the kernel enforces (budgets.json only keeps the offline
/// gate limits and sits outside res://, so the runtime cannot read it). ENG-BUDGET holds the rules, not the numbers.
/// Modules read these by name and never repeat the literals.
/// </summary>
public static class KernelBudgets
{
    // ENG-PERF-DATA: recording watchdog and author-session retention.
    public const double PerfHangWatchdogSeconds = 5;
    public const int PerfAuthorRetainedSessions = 20;
    public const long PerfAuthorRetainedBytes = 2L * 1024 * 1024 * 1024;

    // ENG-BUDGET: the shared per-frame stream mount budget
    public const double StreamMountMilliseconds = 1;

    // ENG-BUDGET: the collision share of that mount budget
    public const double CollisionMountMilliseconds = .3;

    // While an opaque screen is actually presented, yield often enough to keep its UI responsive.
    public const double StreamCoveredMountMilliseconds = 8;
}
