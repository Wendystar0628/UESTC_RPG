using Campus.Kernel.Save.Format.Tests;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save.Tests;

internal sealed class SaveTestStorage : IDisposable
{
    private readonly ManualResetEventSlim _readRelease = new(true);
    private bool _blockReads, _stagedOnly;
    internal TempSaveDirectory Folder { get; } = new();
    internal TaskCompletionSource ReadEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Exception? ReadFailure { get; set; }
    internal string? GenerationFile => Files("g*.sav").SingleOrDefault();
    internal bool HasGeneration => Files("g*.sav").Any();
    internal string? StagingFile => Files(SaveRepository.StagingPattern).SingleOrDefault();
    private IEnumerable<string> Files(string pattern) => System.IO.Directory.Exists(Folder.SlotPath)
        ? System.IO.Directory.EnumerateFiles(Folder.SlotPath, pattern) : [];

    internal void BlockReads()
    {
        _stagedOnly = false; _readRelease.Reset(); _blockReads = true;
    }
    internal void BlockReadback()
    {
        _stagedOnly = true; _readRelease.Reset(); _blockReads = true;
    }
    internal void BeforeTypeInfo()
    {
        // Serialization precedes staging; the staged file identifies the real Commit readback.
        if (!_blockReads || (StagingFile is null && (_stagedOnly || !HasGeneration))) return;
        ReadEntered.TrySetResult(); _readRelease.Wait();
        if (ReadFailure is { } failure) throw failure;
    }
    internal void ReleaseReads() => _readRelease.Set();
    public void Dispose()
    {
        _readRelease.Dispose();
        Folder.Dispose();
    }
}
