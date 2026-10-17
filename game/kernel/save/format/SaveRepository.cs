using System.Globalization;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format;

public sealed record SlotSnapshot(SlotInfo Info, PreparedSave? Save, ulong MaxGeneration);

/// <summary>One exclusive lock and immutable generation files. No rename is used to commit a save.</summary>
public sealed class SaveRepository : IDisposable
{
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
        var maximum = MaximumGeneration(slot, paths.Select(p => p.Generation).DefaultIfEmpty().Max());
        if (paths.Length == 0) return new(new(slot, Kind(slot), SlotHealth.Empty, null, 0), null, maximum);
        var rejected = new List<string>();
        ulong? line = null;
        SaveMeta? lastMeta = null;
        foreach (var (path, generation) in paths)
        {
            SaveDocument? document = null;
            try
            {
                document = Read(path, generation, slot);
                if (line is { } expectedLine && document.Meta.Line != expectedLine) break;
                line ??= document.Meta.Line;
                lastMeta ??= document.Meta;
                var save = _sections.Prepare(document);
                // No file moves unless a complete, same-line recovery succeeded. All-corrupt slots stay intact.
                if (Enabled)
                    foreach (var bad in rejected) Quarantine(slot, bad, newer: false);
                return new(new(slot, Kind(slot), rejected.Count == 0 ? SlotHealth.Ok : SlotHealth.RecoveredOlderGen,
                    document.Meta, paths.Count(p => p.Generation >= document.Meta.Line) - rejected.Count), save, maximum);
            }
            catch (SaveFormatException error) when (error.NewerBuild)
            { return new(new(slot, Kind(slot), SlotHealth.NewerBuild, document?.Meta ?? lastMeta, paths.Length), null, maximum); }
            catch (Exception error) when (error is SaveFormatException or IOException or UnauthorizedAccessException)
            { rejected.Add(path); }
        }
        return new(new(slot, Kind(slot), SlotHealth.Corrupt, lastMeta, paths.Length), null, maximum);
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
        if (!replaceUnusable && current.Info.Health is SlotHealth.Corrupt or SlotHealth.NewerBuild)
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
            var keep = Kind(slot) == SlotKind.Manual ? 1 : 3;
            var retained = 0;
            foreach (var old in Generations(slot).OrderByDescending(g => g.Generation))
            {
                if (old.Generation >= document.Meta.Line && retained++ < keep) continue;
                try { File.Delete(old.Path); }
                catch (IOException) { /* A scanner can hold an old generation briefly. Retry cleanup after the next commit. */ }
                catch (UnauthorizedAccessException) { /* A successful commit must not be reported as failed because cleanup was blocked. */ }
            }
            return new(new(slot, Kind(slot), SlotHealth.Ok, checkedDocument.Meta, Math.Min(retained, keep)),
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

    public void ArchiveUnusableSlot(SlotSnapshot snapshot)
    {
        if (!Enabled) return;
        if (snapshot.Info.Health is not (SlotHealth.Corrupt or SlotHealth.NewerBuild)) return;
        foreach (var path in Generations(snapshot.Info.Slot).OrderBy(p => p.Generation))
            Quarantine(snapshot.Info.Slot, path.Path, snapshot.Info.Health == SlotHealth.NewerBuild);
    }

    private void Quarantine(SlotId slot, string path, bool newer)
    {
        var directory = Path.Combine(_root, "corrupt");
        Directory.CreateDirectory(directory);
        var basename = (newer ? "newer-" : "") + slot.Value + "-" + Path.GetFileName(path);
        for (var suffix = 0; suffix < 10000; suffix++)
        {
            var target = Path.Combine(directory, basename + (suffix == 0 ? "" : "." + suffix.ToString(CultureInfo.InvariantCulture)));
            if (File.Exists(target)) continue;
            try { File.Move(path, target); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return;
        }
    }

    private ulong MaximumGeneration(SlotId slot, ulong value)
    {
        var corrupt = Path.Combine(_root, "corrupt");
        if (!Directory.Exists(corrupt)) return value;
        foreach (var file in Directory.EnumerateFiles(corrupt))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("newer-", StringComparison.Ordinal)) name = name[6..];
            var prefix = slot.Value + "-g";
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var end = name.IndexOf(".sav", StringComparison.Ordinal);
            if (end > prefix.Length && ulong.TryParse(name[prefix.Length..end], NumberStyles.None, CultureInfo.InvariantCulture, out var generation))
                value = Math.Max(value, generation);
        }
        return value;
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
    private static SlotKind Kind(SlotId slot) => slot.Value.StartsWith('s') ? SlotKind.Auto : SlotKind.Manual;
    public void Dispose() { _lock?.Dispose(); _lock = null; }
}
