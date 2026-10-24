using System.Runtime.InteropServices;

namespace Campus.Kernel.Frame;

/// <summary>Godot and .NET have no per-frame thread CPU counter. ENG-PERF-DATA keeps raw Windows cycles only.</summary>
internal static class ThreadCycles
{
    // Current-thread pseudo handle needs no allocation or CloseHandle.
    internal static ulong? Read() => OperatingSystem.IsWindows() && QueryThreadCycleTime(new IntPtr(-2), out var cycles)
        ? cycles : null;

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryThreadCycleTime(IntPtr threadHandle, out ulong cycleTime);
}
