using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save.Format;

/// <summary>Owns abandoned staging and post-commit deletion work. Failed sweeps remain queued until the next commit.</summary>
internal sealed class SaveMaintenance(SaveRepository repository)
{
    internal const int Retained = 3;
    private sealed class Sweep
    {
        public SaveMeta? Meta { get; set; }
        public Exception? Error { get; set; }
    }
    private readonly Dictionary<SlotId, Sweep> _pending = [];
    public IReadOnlyList<Exception> Errors => _pending.Values.Where(s => s.Error is not null).Select(s => s.Error!).ToArray();

    public void Initialize() => SweepPending();

    public void AfterCommit(SaveMeta committed)
    {
        if (_pending.TryGetValue(committed.Slot, out var existing)) existing.Meta = committed;
        else _pending.Add(committed.Slot, new() { Meta = committed });
        SweepPending();
    }

    private void SweepPending()
    {
        foreach (var slot in SaveSlots.Auto) _pending.TryAdd(slot, new());
        foreach (var slot in _pending.Keys.OrderBy(s => s.Value, StringComparer.Ordinal).ToArray())
        {
            var sweep = _pending[slot];
            try
            {
                DeleteStaging(slot);
                if (sweep.Meta is { } committed) DeleteOldGenerations(committed);
                _pending.Remove(slot);
            }
            catch (Exception error) { RetainSweep(sweep, error); }
        }
    }

    private void DeleteStaging(SlotId slot)
    {
        var directory = repository.SlotDirectory(slot);
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, SaveRepository.StagingPattern)) File.Delete(path);
    }

    private void DeleteOldGenerations(SaveMeta meta)
    {
        var generations = repository.Generations(meta.Slot).OrderByDescending(g => g.Generation).ToArray();
        var preserved = new HashSet<string>(StringComparer.Ordinal);
        var retained = 0;
        foreach (var generation in generations)
        {
            // The just-verified generation is protected even when an earlier sweep is retried.
            if (generation.Generation == meta.Gen)
            { preserved.Add(generation.Path); retained++; continue; }
            if (generation.Generation < meta.Line) continue;
            try { repository.Prepare(repository.Read(generation.Path, generation.Generation, meta.Slot)); }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            { PreserveUnreadable(preserved, generation.Path, error); continue; }
            if (retained++ < Retained) preserved.Add(generation.Path);
        }
        foreach (var generation in generations)
        {
            if (preserved.Contains(generation.Path)) continue;
            // Generations before Line belong to the world explicitly replaced by NewGame.
            File.Delete(generation.Path);
        }
    }

    private static void PreserveUnreadable(HashSet<string> preserved, string path,
        [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        preserved.Add(path);
        DiagnosticLog.Warning("Save maintenance preserved an unreadable generation in the current world line.", error);
    }

    private static void RetainSweep(Sweep sweep, [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        sweep.Error = error;
        DiagnosticLog.Warning("Save repository maintenance remains pending for the next commit.", error);
    }
}
