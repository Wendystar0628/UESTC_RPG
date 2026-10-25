using System.Runtime.InteropServices;

namespace Campus.Kernel.Inspect;

internal sealed record CpuCounterRead
{
    public ulong? Kernel100ns { get; init; }
    public ulong? User100ns { get; init; }
    public ulong? ProcessKernel100ns { get; init; }
    public ulong? ProcessUser100ns { get; init; }
    public double SystemQueryStartMs { get; init; }
    public double SystemQueryEndMs { get; init; }
    public double ProcessQueryStartMs { get; init; }
    public double ProcessQueryEndMs { get; init; }
    public int? SystemError { get; init; }
    public int? ProcessError { get; init; }
    public ushort ProcessorGroups { get; init; }
    public uint LogicalProcessors { get; init; }
}
internal sealed record CpuSamplingRead(double? ProcessFraction, string? Reason,
    CpuCounterRead Current, CpuCounterRead? Previous);

/// <summary>Godot exposes no process CPU fraction. One hardware worker reads public Windows counters;
/// GetSystemTimes supplies only the normalization denominator. No processor-group extrapolation.</summary>
internal sealed class WindowsCpuSampler
{
    internal const string Source = "Windows.GetSystemTimes-GetProcessTimes";
    private CpuCounterRead? _previous;

    public CpuSamplingRead Read(Func<double> clock)
    {
        double systemStart = clock();
        bool systemOk = GetSystemTimes(out _, out ulong kernel, out ulong user);
        int? systemError = systemOk ? null : Marshal.GetLastWin32Error();
        double systemEnd = clock();
        double processStart = clock();
        bool processOk = GetProcessTimes(GetCurrentProcess(), out _, out _, out ulong processKernel, out ulong processUser);
        int? processError = processOk ? null : Marshal.GetLastWin32Error();
        double processEnd = clock();
        var current = new CpuCounterRead
        {
            Kernel100ns = systemOk ? kernel : null, User100ns = systemOk ? user : null,
            ProcessKernel100ns = processOk ? processKernel : null, ProcessUser100ns = processOk ? processUser : null,
            SystemQueryStartMs = systemStart, SystemQueryEndMs = systemEnd, ProcessQueryStartMs = processStart,
            ProcessQueryEndMs = processEnd, SystemError = systemError, ProcessError = processError,
            ProcessorGroups = GetActiveProcessorGroupCount(), LogicalProcessors = GetActiveProcessorCount(ushort.MaxValue)
        };
        var result = Calculate(current, _previous);
        // A failed read breaks continuity; never bridge an unread interval silently.
        _previous = systemOk && processOk ? current : null;
        return result;
    }

    internal static CpuSamplingRead Calculate(CpuCounterRead current, CpuCounterRead? previous)
    {
        string? reason = null;
        double? process = null;
        if (current.ProcessorGroups != 1 || current.LogicalProcessors is 0 or > 64)
            reason = "GetSystemTimes cannot establish complete all-group coverage";
        else if (current.SystemError.HasValue || current.ProcessError.HasValue)
            reason = "Windows CPU counter read failed";
        else if (previous is null) reason = "Initial sample has no CPU counter interval";
        else if (current.Kernel100ns < previous.Kernel100ns || current.User100ns < previous.User100ns ||
            current.ProcessKernel100ns < previous.ProcessKernel100ns ||
            current.ProcessUser100ns < previous.ProcessUser100ns)
            reason = "CPU cumulative counter moved backwards";
        else
        {
            double? total = (current.Kernel100ns - previous.Kernel100ns) + (double?)(current.User100ns - previous.User100ns);
            double? processTime = (current.ProcessKernel100ns - previous.ProcessKernel100ns) +
                (double?)(current.ProcessUser100ns - previous.ProcessUser100ns);
            if (total is not > 0 || current.SystemQueryEndMs <= previous.SystemQueryEndMs ||
                current.ProcessQueryEndMs <= previous.ProcessQueryEndMs)
                reason = "CPU counter interval has no positive denominator";
            else
            {
                process = processTime / total;
                if (process is not >= 0 or > 1)
                { process = null; reason = "Process CPU counter interval does not produce a valid fraction"; }
            }
        }
        return new(process, reason, current, previous);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(IntPtr process, out ulong creation, out ulong exit, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")]
    private static extern ushort GetActiveProcessorGroupCount();
    [DllImport("kernel32.dll")]
    private static extern uint GetActiveProcessorCount(ushort group);
}
