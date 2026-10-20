using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save;

/// <summary>Owns one save's optional thumbnail, new-file lifetime and failure result.</summary>
internal sealed class SaveWriteOperation
{
    private readonly List<Exception> _cleanupFailures = [];
    public Exception? Failure { get; private set; }

    public async Task<byte[]?> CaptureThumbnail(Func<Task<byte[]?>> capture, byte[]? previous)
    {
        try { return await capture().ConfigureAwait(false) ?? previous; }
        catch (OperationCanceledException) { return previous; }
        catch (Exception error) { return DiscardThumbnail(error, previous); }
    }

    private byte[]? DiscardThumbnail([FailureHandler("SYS-SAVE-FORMAT")] Exception error, byte[]? previous)
    {
        // SYS-SAVE-FORMAT permits an absent thumbnail; keep the existing image and continue this state save.
        DiagnosticLog.Warning("Save thumbnail unavailable; saving state with the previous optional image.", error);
        return previous;
    }

    public SlotSnapshot Commit(SaveRepository repository, SaveDocument document,
        Action<FileStream, byte[]>? writeFault = null, bool replaceUnusable = false)
    {
        if (!repository.Enabled) throw new InvalidOperationException("This process does not own the save lock.");
        var slot = document.Meta.Slot;
        if (!SaveContainer.ValidSlot(slot)) throw new ArgumentException("Invalid slot.");
        var current = repository.Scan(slot);
        if (!replaceUnusable && current.Info.Health == SlotHealth.Corrupt)
            throw new InvalidOperationException("An unusable slot requires an explicit new game before writing.");
        if (document.Meta.Gen <= current.MaxGeneration) throw new InvalidOperationException("Save generation must increase.");
        var directory = repository.SlotDirectory(slot);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "g" + document.Meta.Gen.ToString("D6", CultureInfo.InvariantCulture) + ".sav");
        var created = false;
        SaveDocument checkedDocument;
        PreparedSave prepared;
        try
        {
            var bytes = SaveContainer.Encode(document);
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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
            checkedDocument = repository.Read(path, document.Meta.Gen, slot);
            prepared = repository.Prepare(checkedDocument);
        }
        catch (Exception error)
        {
            AbortIncomplete(error, created ? path : null);
            if (!ReferenceEquals(Failure, error)) throw Failure!;
            throw;
        }
        // Readback is the commit boundary. Maintenance owns every later deletion/enumeration failure.
        repository.Maintain(checkedDocument);
        var generations = document.Meta.Line == current.Info.Meta?.Line ? current.Info.Generations + 1 : 1;
        return new(new(slot, SlotKind.Auto, SlotHealth.Ok, checkedDocument.Meta, Math.Min(generations, SaveMaintenance.Retained)),
            prepared, document.Meta.Gen);
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

    public SaveResult Fail([FailureHandler("SYS-SAVE-FLOW")] Exception error, SlotId? slot, ulong generation)
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
        DiagnosticLog.Error("Save operation failed.", Failure);
        return result;
    }
}
