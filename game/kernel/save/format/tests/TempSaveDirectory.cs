using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format.Tests;

internal sealed class TempSaveDirectory : IDisposable
{
    private static long _id;
    internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
        "uestc-rpg-save-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _id));
    internal string SlotPath => System.IO.Path.Combine(Path, SaveSlots.Auto[0].Value);
    internal string Entry(ulong generation) => System.IO.Path.Combine(SlotPath,
        "g" + generation.ToString("D6", System.Globalization.CultureInfo.InvariantCulture) + ".sav");

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}
