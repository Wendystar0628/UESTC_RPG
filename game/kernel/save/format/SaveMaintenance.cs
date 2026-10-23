using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save.Format;

/// <summary>Owns abandoned staging and post-commit deletion work. Failed sweeps remain queued until the next commit.</summary>
internal sealed class SaveMaintenance(SaveRepository repository)
{
    private sealed class Sweep
    {
        public SaveMeta? Meta { get; set; }
        public bool ReplacesWorld { get; set; }
        public Exception? Error { get; set; }
    }
    private readonly Dictionary<SlotId, Sweep> _pending = [];
    public IReadOnlyList<Exception> Errors => _pending.Values.Where(s => s.Error is not null).Select(s => s.Error!).ToArray();

    public void Initialize() => SweepPending();
    public void Forget(SlotId slot) => _pending.Remove(slot);

    public void AfterCommit(SaveMeta committed, bool replacesWorld)
    {
        if (_pending.TryGetValue(committed.Slot, out var existing))
        { existing.Meta = committed; existing.ReplacesWorld = replacesWorld; }
        else _pending.Add(committed.Slot, new() { Meta = committed, ReplacesWorld = replacesWorld });
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
                var snapshot = repository.Scan(slot);
                if (snapshot.Info.Health == SlotHealth.Newer &&
                    (!sweep.ReplacesWorld || snapshot.Info.Entries.Any(e => e.Health == SlotHealth.Newer))) continue;
                if (sweep.ReplacesWorld && sweep.Meta is { } replacement)
                    foreach (var generation in repository.Generations(slot).Where(g => g.Generation < replacement.Line))
                        File.Delete(generation.Path);
                if (sweep.Meta is { } committed && !DeleteOldGenerations(committed)) continue;
                DeleteStaging(slot);
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

    private bool DeleteOldGenerations(SaveMeta meta)
    {
        var generations = repository.Generations(meta.Slot).OrderByDescending(g => g.Generation).ToArray();
        var preserved = new HashSet<string>(StringComparer.Ordinal);
        var automatic = 0;
        var manual = 0;
        foreach (var generation in generations)
        {
            // The just-verified generation is protected even when an earlier sweep is retried.
            if (generation.Generation == meta.Gen)
            { preserved.Add(generation.Path); Retain(meta.Reason); continue; }
            SaveDocument document;
            try
            {
                document = repository.Read(generation.Path, generation.Generation, meta.Slot);
                repository.Prepare(document);
            }
            catch (SaveFormatException error) when (error.IsNewer)
            { RejectGeneration(preserved, generation.Path, true, error); return false; }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            {
                RejectGeneration(preserved, generation.Path, generation.Generation >= meta.Line, error);
                continue;
            }
            if (generation.Generation < meta.Line) continue;
            if (Retain(document.Meta.Reason)) preserved.Add(generation.Path);
        }
        foreach (var generation in generations)
        {
            if (preserved.Contains(generation.Path)) continue;
            // Generations before Line belong to the world explicitly replaced by NewGame.
            File.Delete(generation.Path);
        }
        return true;

        bool Retain(SaveReason reason) => reason switch
        {
            SaveReason.Manual => manual++ < SaveSlots.ManualLimit,
            SaveReason.Quit or SaveReason.ToTitle or SaveReason.Interval or SaveReason.NewGame or SaveReason.Test =>
                automatic++ < SaveSlots.AutoLimit,
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };
    }

    private static void RejectGeneration(HashSet<string> preserved, string path, bool preserve,
        [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        if (preserve) preserved.Add(path);
        DiagnosticLog.Warning("Save maintenance rejected a generation; only explicitly replaced world lines may be deleted.", error);
    }

    private static void RetainSweep(Sweep sweep, [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        sweep.Error = error;
        DiagnosticLog.Warning("Save repository maintenance remains pending for the next commit.", error);
    }
}
