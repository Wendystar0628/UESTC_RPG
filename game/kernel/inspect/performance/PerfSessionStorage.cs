using System.Globalization;
using System.Text.Json.Nodes;
using Campus.Contracts.Perf;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Inspect;

internal static class PerfSessionStorage
{
    public static (string Id, string Directory, FileStream Lock) Open(string root, DateTimeOffset time, PerfOrigin origin)
    {
        Directory.CreateDirectory(root);
        string stem = time.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" +
            (origin switch { PerfOrigin.Play => "play", PerfOrigin.Ai => "ai", _ => throw new ArgumentOutOfRangeException(nameof(origin)) });
        for (int suffix = 1; ; suffix++)
        {
            string id = suffix == 1 ? stem : stem + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            string directory = Path.Combine(root, id);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, ".lock");
            if (File.Exists(Path.Combine(directory, "manifest.json")) || File.Exists(Path.Combine(directory, "manifest.pending.json"))) continue;
            try { return (id, directory, new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)); }
            catch (IOException error) when (File.Exists(path)) { ExistingSession(error); }
        }
    }

    // CreateNew's collision is an expected session-name conflict, not a recording failure (ENG-PERF-DATA).
    private static void ExistingSession([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] IOException error)
    {
        if (error.HResult is not (unchecked((int)0x80070050) or unchecked((int)0x800700B7)))
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    public static void PruneAuthor(string root)
    {
        if (!Directory.Exists(root)) return;
        var closed = new List<(string Directory, string Id, long Bytes, DateTimeOffset StartedAt)>();
        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            string manifestPath = Path.Combine(directory, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            using var sessionLock = TryLock(directory);
            if (sessionLock is null) continue;
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject();
            if (manifest?["status"]?.GetValue<string>() != "closed" ||
                manifest["id"]?.GetValue<string>() != Path.GetFileName(directory)) continue;
            long bytes = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length);
            closed.Add((directory, Path.GetFileName(directory), bytes, manifest["startedAt"]!.GetValue<DateTimeOffset>()));
        }
        var ordered = closed.OrderByDescending(session => session.StartedAt).ThenByDescending(session => Suffix(session.Id)).ToList();
        long retainedBytes = 0;
        for (int index = 0; index < ordered.Count; index++)
        {
            var session = ordered[index];
            retainedBytes += session.Bytes;
            // Leave room for the new session; bytes are checked again at the next start.
            if (index < KernelBudgets.PerfAuthorRetainedSessions - 1 && retainedBytes <= KernelBudgets.PerfAuthorRetainedBytes) continue;
            using (var sessionLock = TryLock(session.Directory))
            {
                if (sessionLock is null) continue;
                foreach (string file in Directory.EnumerateFiles(session.Directory, "*", SearchOption.AllDirectories))
                    if (Path.GetFileName(file) != ".lock") File.Delete(file);
            }
            File.Delete(Path.Combine(session.Directory, ".lock"));
            Directory.Delete(session.Directory, true);
        }
    }

    private static int Suffix(string id) => int.TryParse(id.AsSpan(id.LastIndexOf('-') + 1),
        NumberStyles.None, CultureInfo.InvariantCulture, out int suffix) ? suffix : 1;

    private static FileStream? TryLock(string directory)
    {
        try { return new FileStream(Path.Combine(directory, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33) { LockedSession(error); return null; }
    }

    private static void LockedSession([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] IOException error) =>
        _ = error; // Another process owns this closed session; retention explicitly skips it (ENG-PERF-DATA).
}
