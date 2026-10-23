using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

/// <summary>Owns one save's optional thumbnail, staging lifetime and failure result.</summary>
internal sealed class SaveWriteOperation
{
    private readonly List<Exception> _cleanupFailures = [];
    private int _snapshotDetached;
    public bool SnapshotDetached => Volatile.Read(ref _snapshotDetached) != 0;
    public Exception? Failure { get; private set; }

    // Call only after all live-world/map queries have produced the immutable document for Commit.
    public void MarkSnapshotDetached() => Volatile.Write(ref _snapshotDetached, 1);

    public async Task<SaveResult> WaitForFlush(Task<SaveResult> writing, SaveReason reason,
        SlotId? slot, CancellationToken cancellation)
    {
        try { return await writing.WaitAsync(cancellation).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            // A completed failure is never hidden by the title's bounded wait.
            if (writing.IsCompleted) return await writing.ConfigureAwait(false);
            if (reason == SaveReason.ToTitle && SnapshotDetached)
                return new(SaveStatus.Deferred, slot, 0);
            throw;
        }
    }

    public async Task<byte[]?> CaptureThumbnail(IDiagnosticLog diagnostics, Func<Task<byte[]?>> capture, byte[]? previous)
    {
        try { return await capture().ConfigureAwait(false) ?? previous; }
        catch (OperationCanceledException) { return previous; }
        catch (Exception error) { return DiscardThumbnail(diagnostics, error, previous); }
    }

    private byte[]? DiscardThumbnail(IDiagnosticLog diagnostics, [FailureHandler("SYS-SAVE-FORMAT")] Exception error, byte[]? previous)
    {
        // SYS-SAVE-FORMAT permits an absent thumbnail; keep the existing image and continue this state save.
        diagnostics.Warning("Save thumbnail unavailable; saving state with the previous optional image.", error);
        return previous;
    }

    public SlotSnapshot Commit(SaveRepository repository, SaveDocument document,
        Action<FileStream, byte[]>? writeFault = null, bool replaceUnusable = false)
    {
        if (!repository.Enabled) throw new InvalidOperationException("This process does not own the save lock.");
        var slot = document.Meta.Slot;
        if (!SaveContainer.ValidSlot(slot)) throw new ArgumentException("Invalid slot.");
        var current = repository.Scan(slot);
        var replacesWorld = replaceUnusable && document.Meta.Reason == SaveReason.NewGame && document.Meta.Line == document.Meta.Gen;
        if (current.Info.Health == SlotHealth.Newer && !replacesWorld)
            throw new InvalidOperationException("A slot from a newer version is read-only.");
        if (!replaceUnusable && current.Info.Health == SlotHealth.Corrupt)
            throw new InvalidOperationException("An unusable slot requires an explicit new game before writing.");
        if (document.Meta.Gen <= current.MaxGeneration) throw new InvalidOperationException("Save generation must increase.");
        var directory = repository.SlotDirectory(slot);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "g" + document.Meta.Gen.ToString("D6", CultureInfo.InvariantCulture) + ".sav");
        var staging = repository.StagingPath(slot);
        var created = false;
        SaveDocument checkedDocument;
        try
        {
            var bytes = SaveContainer.Encode(document);
            var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            created = true;
            try
            {
                if (writeFault is null) stream.Write(bytes); else writeFault(stream, bytes);
                stream.Flush(flushToDisk: true);
            }
            catch
            {
                CloseFailedWrite(stream);
                throw;
            }
            stream.Dispose();
            checkedDocument = repository.Read(staging, document.Meta.Gen, slot);
            repository.Prepare(checkedDocument);
            // SYS-SAVE-FORMAT: only a fully verified file may become a visible generation; never replace a published name.
            File.Move(staging, path);
        }
        catch (Exception error)
        {
            AbortIncomplete(error, created ? staging : null);
            if (!ReferenceEquals(Failure, error)) throw Failure!;
            throw;
        }
        // Rename is the commit boundary. Maintenance owns every later deletion/enumeration failure.
        repository.Maintain(checkedDocument, replacesWorld);
        return repository.Scan(slot);
    }

    private void CloseFailedWrite(FileStream stream)
    {
        try { stream.Dispose(); }
        catch (Exception cleanupError) { RetainCleanupFailure(cleanupError); }
    }

    private void AbortIncomplete([FailureHandler("SYS-SAVE-FLOW")] Exception error, string? path)
    {
        Failure = error;
        if (path is not null)
        {
            try { File.Delete(path); }
            catch (Exception cleanupError) { RetainCleanupFailure(cleanupError); }
        }
        if (_cleanupFailures.Count != 0)
            Failure = new AggregateException(new[] { error }.Concat(_cleanupFailures));
    }

    private void RetainCleanupFailure([FailureHandler("SYS-SAVE-FLOW")] Exception cleanupError) =>
        _cleanupFailures.Add(cleanupError);

    public SaveResult Fail(IDiagnosticLog diagnostics, [FailureHandler("SYS-SAVE-FLOW")] Exception error, SlotId? slot, ulong generation)
    {
        var primary = _cleanupFailures.Count != 0 && error is AggregateException aggregate ? aggregate.InnerExceptions[0] : error;
        Failure = error;
        var key = primary switch
        {
            UnauthorizedAccessException => "save.error.permission",
            IOException io when (io.HResult & 0xffff) is 39 or 112 => "save.error.disk_full",
            IOException => "save.error.io",
            SaveFormatException => "save.error.verification",
            _ => "save.error.snapshot"
        };
        var result = new SaveResult(SaveStatus.Failed, slot, generation, key);
        diagnostics.Error("Save operation failed.", Failure);
        return result;
    }
}
