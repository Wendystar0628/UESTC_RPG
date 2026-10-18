using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(IAmbienceService), Owner = "WP-00", IsStub = true)]
public sealed class AmbienceServiceStub : IAmbienceService
{
    private int _nextHandle;
    public AmbienceSnapshot Now => default;
    public void Cue(in AmbienceCue cue) { }
    public void CancelCues(uint group) { }
    public int AddDynamic(in AmbientEmitterDecl decl) => checked(++_nextHandle);
    public void MoveDynamic(int handle, System.Numerics.Vector3 position) { }
    public void RemoveDynamic(int handle) { }
}

[KernelService(typeof(IFoleyService), Owner = "WP-00", IsStub = true)]
public sealed class FoleyServiceStub : IFoleyService
{
    public FoleyStats Stats => default;
    public IReadOnlyList<FoleyEvent> RecentLog => Array.Empty<FoleyEvent>();
    public void Play(in FoleyEvent e) { }
    public void SetLoop(FoleyLoop loop, float level01, float param01) { }
}

[KernelService(typeof(IGroundCover), Owner = "WP-00", IsStub = true)]
public sealed class GroundCoverStub : IGroundCover
{
    public GroundCover Sample(float x, float z) => default;
}
