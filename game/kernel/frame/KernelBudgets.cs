namespace Campus.Kernel.Frame;

/// <summary>
/// The single code-side home of the ENG-BUDGET numbers the kernel enforces. Values mirror budgets.json
/// (that file sits outside res:// and is not packed, so the runtime cannot read it); change both together.
/// Modules read these by name and never repeat the literals.
/// </summary>
public static class KernelBudgets
{
    // budgets.json presets.{balanced,fine,ultra}.internalPixels and presets.ultra.internalPixelsAt4k
    public const double BalancedInternalPixels = 1_000_000;
    public const double FineInternalPixels = 2_100_000;
    public const double UltraInternalPixels = 3_700_000;
    public const double UltraInternalPixelsAt4k = 4_900_000;

    // budgets.json runtime[WP-02a/WP-02b].streamMountMsMax: the shared per-frame mount budget
    public const double StreamMountMilliseconds = 1;

    // budgets.json runtime[WP-42/WP-43].collisionMountMsMax: the collision share of that budget
    public const double CollisionMountMilliseconds = .3;
}
