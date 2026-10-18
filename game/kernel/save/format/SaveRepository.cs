using System.Globalization;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format;

public sealed record SlotSnapshot(SlotInfo Info, PreparedSave? Save, ulong MaxGeneration);

/// <summary>One exclusive lock and immutable generation files, three per slot. No rename is used to commit a save.</summary>
public sealed class SaveRepository : IDisposable
{
    private const int Retained = 3;
    private readonly string _root;
    private readonly SaveSections _sections;
    private FileStream? _lock;
    public bool Enabled => _lock is not null;
    public ReadOnlyReason ReadOnly { get; }
    public string Root => _root;

    public SaveRepository(string absoluteRoot, bool enabled, SaveSections sections)
    {
        if (!Path.IsPathFullyQualified(absoluteRoot)) throw new ArgumentException("Save directory must be absolute.");
        _root = Path.GetFullPath(absoluteRoot);
        _sections = sections;
        if (!enabled) { ReadOnly = ReadOnlyReason.SaveOff; return; }
        Directory.CreateDirectory(_root);
        try { _lock = new FileStream(Path.Combine(_root, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException) { ReadOnly = ReadOnlyReason.OtherWindow; }
    }

    public SlotSnapshot Scan(SlotId slot)
    {
        var paths = Generations(slot).OrderByDescending(g => g.Generation).ToArray();
        if (paths.Length == 0) return new(new(slot, SlotKind.Auto, SlotHealth.Empty, null, 0), null, 0);
        var maximum = paths[0].Generation;
        var rejected = 0;
        ulong? line = null;
        SaveMeta? lastMeta = null;
        // Newest first; the first complete generation of the same world line wins. Unreadable files stay where they are.
        foreach (var (path, generation) in paths)
        {
            try
            {
                var document = Read(path, generation, slot);
                if (line is { } expectedLine && document.Meta.Line != expectedLine) break;
                line ??= document.Meta.Line;
                lastMeta ??= document.Meta;
                var save = _sections.Prepare(document);
                return new(new(slot, SlotKind.Auto, SlotHealth.Ok, document.Meta,
                    paths.Count(p => p.Generation >= document.Meta.Line) - rejected), save, maximum);
            }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            { rejected++; }
        }
        return new(new(slot, SlotKind.Auto, SlotHealth.Corrupt, lastMeta, paths.Length), null, maximum);
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

    public SlotSnapshot Write(SaveDocument document, Action<FileStream, byte[]>? writeFault = null, bool replaceUnusable = false)
    {
        if (!Enabled) throw new InvalidOperationException("This process does not own the save lock.");
        var slot = document.Meta.Slot;
        if (!SaveContainer.ValidSlot(slot)) throw new ArgumentException("Invalid slot.");
        var current = Scan(slot);
        if (!replaceUnusable && current.Info.Health == SlotHealth.Corrupt)
            throw new InvalidOperationException("An unusable slot requires an explicit new game before writing.");
        if (document.Meta.Gen <= current.MaxGeneration) throw new InvalidOperationException("Save generation must increase.");
        var directory = SlotDirectory(slot);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "g" + document.Meta.Gen.ToString("D6", CultureInfo.InvariantCulture) + ".sav");
        var created = false;
        var committed = false;
        try
        {
            var bytes = SaveContainer.Encode(document);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                if (writeFault is null) stream.Write(bytes); else writeFault(stream, bytes);
                stream.Flush(flushToDisk: true);
            }
            var checkedDocument = Read(path, document.Meta.Gen, slot);
            var prepared = _sections.Prepare(checkedDocument);
            committed = true;
            // Older worlds and generations are touched only after the new generation has been read back successfully.
            var retained = 0;
            foreach (var old in Generations(slot).OrderByDescending(g => g.Generation))
            {
                if (old.Generation >= document.Meta.Line && retained++ < Retained) continue;
                try { File.Delete(old.Path); }
                catch (IOException) { /* A scanner can hold an old generation briefly. Retry cleanup after the next commit. */ }
                catch (UnauthorizedAccessException) { /* A successful commit must not be reported as failed because cleanup was blocked. */ }
            }
            return new(new(slot, SlotKind.Auto, SlotHealth.Ok, checkedDocument.Meta, Math.Min(retained, Retained)),
                prepared, document.Meta.Gen);
        }
        catch
        {
            if (created && !committed)
            {
                try { File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            throw;
        }
    }

    private IEnumerable<(string Path, ulong Generation)> Generations(SlotId slot)
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

    private string SlotDirectory(SlotId slot)
    {
        if (!SaveContainer.ValidSlot(slot)) throw new ArgumentException("Invalid slot.");
        return Path.Combine(_root, slot.Value);
    }
    public void Dispose() { _lock?.Dispose(); _lock = null; }
}
