using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;

namespace Campus.Systems.Fauna;

internal sealed class FaunaLatch
{
    public bool Applied;
    public FaunaSet? Set;
    public FaunaSet? Desired;
    public bool Requested;
    public long Epoch;
    public long AppliedEpoch = -1;
    public long Version;
    public bool AllowVisible;
    public readonly List<RenderHandle> Handles = [];
    public readonly List<RenderHandle> RetiringHandles = [];
}

internal sealed class FaunaTile(FaunaTileContent content, BakePriority priority, long revision, long generation)
{
    public readonly FaunaTileContent Content = content;
    public readonly BakePriority Priority = priority;
    public readonly long Revision = revision;
    public readonly long Generation = generation;
    public readonly FaunaLatch[] Latches = content.Content.Sites.Select(_ => new FaunaLatch()).ToArray();
    public FaunaTileMountWork? Work;
    public bool Retiring;
    public int ReleaseLatch;
    public bool Mounted;
}
