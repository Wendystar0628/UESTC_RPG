using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Save;
using SaveDiagnostics = Campus.Kernel.Diagnostics.Diagnostics;

namespace Campus.Kernel.Save.Format;

public sealed record RejectedGeneration(ulong Generation, Exception Error)
{
    public SaveMeta? Metadata { get; init; }
}
public sealed record SlotSnapshot(SlotInfo Info, PreparedSave? Save, ulong MaxGeneration)
{
    public IReadOnlyList<RejectedGeneration> Rejected { get; init; } = [];
}

/// <summary>One exclusive lock and immutable generation files, three per slot. No rename is used to commit a save.</summary>
public sealed class SaveRepository : IDisposable
{
    private readonly SaveMaintenance _maintenance;
    private readonly string _root;
    private readonly SaveSections _sections;
    private FileStream? _lock;
    public bool Enabled => _lock is not null;
    public ReadOnlyReason ReadOnly { get; private set; }
    public string Root => _root;
    public IReadOnlyList<Exception> PendingMaintenanceErrors => _maintenance.Errors;
    public Exception? LockConflict { get; private set; }

    public SaveRepository(string absoluteRoot, bool enabled, SaveSections sections)
    {
        if (!Path.IsPathFullyQualified(absoluteRoot)) throw new ArgumentException("Save directory must be absolute.");
        _root = Path.GetFullPath(absoluteRoot);
        _sections = sections;
        _maintenance = new(this);
        if (!enabled) { ReadOnly = ReadOnlyReason.SaveOff; return; }
        Directory.CreateDirectory(_root);
        try { _lock = new FileStream(Path.Combine(_root, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33)
        { EnterReadOnly(error); }
    }

    public SlotSnapshot Scan(SlotId slot)
    {
        var paths = Generations(slot).OrderByDescending(g => g.Generation).ToArray();
        if (paths.Length == 0) return new(new(slot, SlotKind.Auto, SlotHealth.Empty, null, 0), null, 0);
        var maximum = paths[0].Generation;
        var rejected = new List<RejectedGeneration>();
        ulong? line = null;
        SaveMeta? lastMeta = null;
        // Newest first; the first complete generation of the same world line wins. Unreadable files stay where they are.
        foreach (var (path, generation) in paths)
        {
            SaveMeta? metadata = null;
            try
            {
                var document = Read(path, generation, slot);
                metadata = document.Meta;
                if (line is { } expectedLine && document.Meta.Line != expectedLine) break;
                line ??= document.Meta.Line;
                lastMeta ??= document.Meta;
                var save = _sections.Prepare(document);
                return new(new(slot, SlotKind.Auto, SlotHealth.Ok, document.Meta,
                    paths.Count(p => p.Generation >= document.Meta.Line) - rejected.Count), save, maximum) { Rejected = rejected.ToArray() };
            }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            {
                metadata ??= (error as SaveFormatException)?.Metadata;
                RejectGeneration(rejected, generation, metadata, error);
                // An unidentified generation may start a different world line; never infer its identity from an older file.
                if (metadata is null || line is { } expectedLine && metadata.Line != expectedLine) break;
                line ??= metadata.Line;
                lastMeta ??= metadata;
            }
        }
        return new(new(slot, SlotKind.Auto, SlotHealth.Corrupt, lastMeta, paths.Length), null, maximum) { Rejected = rejected.ToArray() };
    }

    public PreparedSave ReadExternal(string absolutePath)
    {
        if (!Path.IsPathFullyQualified(absolutePath)) throw new ArgumentException("Load file must be absolute.");
        return _sections.Prepare(Read(Path.GetFullPath(absolutePath)));
    }

    public SaveDocument Read(string absolutePath, ulong? generation = null, SlotId? slot = null)
    {
        using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > SaveContainer.MaxFile) throw new SaveFormatException("File exceeds 20 MiB.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return SaveContainer.Decode(bytes, generation, slot);
    }

    public SlotSnapshot Write(SaveDocument document, Action<FileStream, byte[]>? writeFault = null, bool replaceUnusable = false) =>
        new SaveWriteOperation().Commit(this, document, writeFault, replaceUnusable);

    internal PreparedSave Prepare(SaveDocument document) => _sections.Prepare(document);
    internal void Maintain(SaveDocument committed) => _maintenance.AfterCommit(committed.Meta);

    private void EnterReadOnly([FailureHandler("SYS-SAVE")] IOException error)
    {
        LockConflict = error;
        ReadOnly = ReadOnlyReason.OtherWindow;
        SaveDiagnostics.Warning("Save lock is owned by another window; this process is read-only.", error);
    }

    private static void RejectGeneration(List<RejectedGeneration> rejected, ulong generation, SaveMeta? metadata,
        [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        rejected.Add(new(generation, error) { Metadata = metadata });
        SaveDiagnostics.Warning("Save generation rejected; recovery requires a known matching world line.", error);
    }

    internal IEnumerable<(string Path, ulong Generation)> Generations(SlotId slot)
    {
        var directory = SlotDirectory(slot);
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory, "g*.sav"))
        {
            var name = Path.GetFileName(file);
            if (ulong.TryParse(name.AsSpan(1, name.Length - 5), NumberStyles.None, CultureInfo.InvariantCulture, out var generation) && generation != 0)
                yield return (file, generation);
        }
    }

    internal string SlotDirectory(SlotId slot)
    {
        if (!SaveContainer.ValidSlot(slot)) throw new ArgumentException("Invalid slot.");
        return Path.Combine(_root, slot.Value);
    }
    public void Dispose() { _lock?.Dispose(); _lock = null; }
}
