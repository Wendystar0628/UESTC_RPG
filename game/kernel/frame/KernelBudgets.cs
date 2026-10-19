namespace Campus.Kernel.Frame;

/// <summary>
/// The single home of the runtime ENG-BUDGET numbers the kernel enforces (budgets.json only keeps the offline
/// gate limits and sits outside res://, so the runtime cannot read it). ENG-BUDGET holds the rules, not the numbers.
/// Modules read these by name and never repeat the literals.
/// </summary>
public static class KernelBudgets
{
    // ENG-BUDGET: internal render pixels per preset (ultra also has a 4K figure)
    public const double BalancedInternalPixels = 1_000_000;
    public const double FineInternalPixels = 2_100_000;
    public const double UltraInternalPixels = 3_700_000;
    public const double UltraInternalPixelsAt4k = 4_900_000;

    // ENG-BUDGET: the shared per-frame stream mount budget
    public const double StreamMountMilliseconds = 1;

    // ENG-BUDGET: the collision share of that mount budget
    public const double CollisionMountMilliseconds = .3;
}
