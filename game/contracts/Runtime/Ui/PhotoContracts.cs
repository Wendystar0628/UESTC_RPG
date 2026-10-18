using System.Threading.Tasks;

namespace Campus.Contracts;

public enum PhotoExportStatus : byte { Saved, Busy, Unavailable, Failed }

// Path is the actual absolute output filename when one has been selected; otherwise null.
public readonly record struct PhotoExportResult(PhotoExportStatus Status, string? Path);

/// <summary>Exports the current rendered view. Call on the main thread; UI owns input and result messages.</summary>
public interface IPhotoExport
{
    Task<PhotoExportResult> ExportCurrentAsync();
}
