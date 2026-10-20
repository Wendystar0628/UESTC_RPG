using System.Threading;
using System.Threading.Tasks;

namespace Campus.Contracts;

public enum CaptureStatus : byte { Saved, Busy, Unavailable, Failed }

// A null path selects the user's photos directory and a collision-safe name; explicit paths are replaced.
public readonly record struct CaptureRequest(string? OutputPath = null, bool HideUi = true)
{
    public CaptureRequest() : this(null, true) { }
}

// Path is the actual absolute output filename when one has been selected; otherwise null.
public readonly record struct CaptureResult(CaptureStatus Status, string? Path);

/// <summary>Captures the rendered root viewport. Begin on the main thread; callers own input and messages.</summary>
public interface IGameCapture
{
    Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default);
    // The save consumer owns thumbnail validation and the decision to continue without an image.
    Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default);
}
