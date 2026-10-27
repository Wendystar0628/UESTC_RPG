namespace Campus.Kernel.Stream;

/// <summary>Pure mount-step budget policy with local cooldown and recovery rules.</summary>
public sealed class MountWatchdog
{
    private readonly double _budgetMilliseconds;
    private int _overruns;
    private double _resumeAt;
    public MountWatchdog(double budgetMilliseconds)
    {
        _budgetMilliseconds = budgetMilliseconds;
        VisualBudgetMilliseconds = budgetMilliseconds;
    }
    public double VisualBudgetMilliseconds { get; private set; }
    public int Trips { get; private set; }
    public bool CanActivate(double nowMilliseconds) => nowMilliseconds >= _resumeAt;
    public void Observe(double elapsedMilliseconds, double nowMilliseconds)
    {
        _overruns = elapsedMilliseconds > _budgetMilliseconds * 2 ? _overruns + 1 : 0;
        if (_overruns < 3)
        {
            if (_overruns == 0 && CanActivate(nowMilliseconds))
                VisualBudgetMilliseconds = Math.Min(_budgetMilliseconds, VisualBudgetMilliseconds * 2);
            return;
        }
        VisualBudgetMilliseconds = Math.Max(.1, VisualBudgetMilliseconds * .5);
        _resumeAt = nowMilliseconds + 250;
        _overruns = 0; Trips++;
    }
}
