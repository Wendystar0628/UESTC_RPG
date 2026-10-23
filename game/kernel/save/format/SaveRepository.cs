using System.Globalization;
using System.Runtime.ExceptionServices;
using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save.Format;

public sealed record SlotSnapshot(SlotInfo Info, PreparedSave? Save, ulong MaxGeneration)
{
    public IReadOnlyDictionary<ulong, byte[]> Thumbnails { get; init; } = new Dictionary<ulong, byte[]>();
}

/// <summary>One exclusive lock; verified same-directory staging files become immutable entries retained in separate reason pools.</summary>
public sealed class SaveRepository : IDisposable
{
    internal const string StagingPattern = ".save-*.tmp";
    private readonly SaveMaintenance _maintenance;
    private readonly string _root;
    private readonly SaveSections _sections;
    private FileStream? _lock;
    public bool Enabled => _lock is not null;
    public ReadOnlyReason ReadOnly { get; private set; }
    public string Root => _root;
    public IReadOnlyList<Exception> PendingMaintenanceErrors => _maintenance.Errors;

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
        if (Enabled) _maintenance.Initialize();
    }

    public SlotSnapshot Scan(SlotId slot) => Scan(slot, null, out _, out _);

    private SlotSnapshot Scan(SlotId slot, ulong? requested, out PreparedSave? selected, out Exception? selectionFailure)
    {
        selected = null;
        selectionFailure = null;
        var paths = Generations(slot).OrderByDescending(g => g.Generation).ToArray();
        if (paths.Length == 0) return new(new(slot, SlotKind.Auto, SlotHealth.Empty, null, 0), null, 0);
        var entries = new List<SaveEntry>();
        var thumbnails = new Dictionary<ulong, byte[]>();
        ulong? line = null;
        SaveMeta? lastMeta = null;
        PreparedSave? latest = null;
        Exception? future = null;
        var visible = true;
        // Inspect every file for future versions; entry selection still stops at an unknown identity or world-line boundary.
        foreach (var (path, generation) in paths)
        {
            SaveMeta? metadata = null;
            PreparedSave? prepared = null;
            Exception? failure = null;
            var health = SlotHealth.Ok;
            try
            {
                var document = Read(path, generation, slot);
                metadata = document.Meta;
                prepared = _sections.Prepare(document);
            }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            {
                metadata ??= (error as SaveFormatException)?.Metadata;
                failure = error;
                health = error is SaveFormatException { IsNewer: true } ? SlotHealth.Newer : SlotHealth.Corrupt;
                if (health == SlotHealth.Newer) future ??= error;
                RejectGeneration(error);
            }
            if (!visible) continue;
            if (line is { } expectedLine && (generation < expectedLine || metadata is { } known && known.Line != expectedLine))
            { visible = false; continue; }
            entries.Add(new(generation, health, metadata));
            line ??= metadata?.Line;
            lastMeta ??= metadata;
            latest ??= prepared;
            if (prepared?.Document.Thumbnail is { } thumbnail) thumbnails.Add(generation, thumbnail);
            if (requested == generation) { selected = prepared; selectionFailure = failure; }
            // An unidentified generation may start a different world line; older files cannot establish its identity.
            if (metadata is null) visible = false;
        }
        if (future is not null) { selected = null; selectionFailure = future; }
        var slotHealth = future is not null ? SlotHealth.Newer : latest is not null ? SlotHealth.Ok : SlotHealth.Corrupt;
        var info = new SlotInfo(slot, SlotKind.Auto, slotHealth, latest?.Document.Meta ?? lastMeta, entries.Count)
        { Entries = entries.AsReadOnly() };
        return new(info, future is null ? latest : null, paths[0].Generation) { Thumbnails = thumbnails };
    }

    public PreparedSave ReadEntry(SlotId slot, ulong generation)
    {
        if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
        Scan(slot, generation, out var selected, out var failure);
        if (selected is not null) return selected;
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        throw new SaveFormatException("The requested save entry is missing or outside the current world line.");
    }

    public SlotSnapshot DeleteEntry(SlotId slot, ulong generation)
    {
        var current = RequireWritable(slot);
        var entry = current.Info.Entries.SingleOrDefault(e => e.Gen == generation);
        if (entry?.Meta?.Reason != SaveReason.Manual)
            throw new InvalidOperationException("Only an identified manual entry may be deleted individually.");
        var path = Generations(slot).Single(g => g.Generation == generation).Path;
        File.Delete(path);
        return Scan(slot);
    }

    public SlotSnapshot DeleteSlot(SlotId slot)
    {
        // The service accepts this destructive operation only after explicit Title confirmation.
        if (!Enabled) throw new InvalidOperationException("This process does not own the save lock.");
        var directory = SlotDirectory(slot);
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        _maintenance.Forget(slot);
        return Scan(slot);
    }

    private SlotSnapshot RequireWritable(SlotId slot)
    {
        if (!Enabled) throw new InvalidOperationException("This process does not own the save lock.");
        var current = Scan(slot);
        if (current.Info.Health == SlotHealth.Newer)
            throw new InvalidOperationException("A slot from a newer version is read-only.");
        return current;
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
    internal void Maintain(SaveDocument committed, bool replacesWorld) => _maintenance.AfterCommit(committed.Meta, replacesWorld);
    internal string StagingPath(SlotId slot) => Path.Combine(SlotDirectory(slot), ".save-" + Path.GetRandomFileName() + ".tmp");

    private void EnterReadOnly([FailureHandler("SYS-SAVE")] IOException error)
    {
        ReadOnly = ReadOnlyReason.OtherWindow;
        DiagnosticLog.Warning("Save lock is owned by another window; this process is read-only.", error);
    }

    private static void RejectGeneration([FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        DiagnosticLog.Warning("Save generation rejected; recovery requires a known matching world line.", error);
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
