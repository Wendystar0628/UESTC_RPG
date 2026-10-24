using Campus.Contracts.Atlas;

namespace Campus.Tests;

/// <summary>Shared IAtlasPrefetch fake: remembers the requested marker and whether it was released. Ready never completes.</summary>
internal sealed class TestAtlasPrefetch(MarkerId id) : IAtlasPrefetch
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public MarkerId Id { get; } = id;
    public bool Released { get; private set; }
    public Task Ready => _ready.Task;
    public void Dispose() => Released = true;
}
