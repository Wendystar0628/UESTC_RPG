namespace Campus.Contracts;

/// <summary>Allowed world-seconds per real-second multipliers, in UI order; zero pauses.</summary>
public static class FlowRates
{
    public static ReadOnlySpan<int> All => [0, 1, 10, 60, 360, 1440];
    public static bool IsValid(int rate) => All.Contains(rate);
    public static void Validate(int rate)
    {
        if (!IsValid(rate)) throw new ArgumentOutOfRangeException(nameof(rate));
    }
}
