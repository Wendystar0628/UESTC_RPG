using System.Numerics;
using Campus.Audio.Core.Stubs;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(IAcousticEnv), Owner = "WP-00", IsStub = true)]
public sealed class AcousticEnvStub(IMediumState medium) : IAcousticEnv
{
    private bool _wasIndoor;
    private long _rev;

    private float Indoor
    {
        get
        {
            var indoor = medium.Current.Kind switch
            {
                MediumKind.Air => false,
                MediumKind.Interior => true,
                MediumKind.Underground => false,
                MediumKind.Underwater => false,
                MediumKind.FogSea => false,
                _ => throw new ArgumentOutOfRangeException(nameof(medium), "Unknown camera medium.")
            };
            if (indoor != _wasIndoor)
            {
                _wasIndoor = indoor;
                _rev++;
            }
            return indoor ? 1f : 0f;
        }
    }

    public AcousticEnv At(Vector3 p) => new(AcousticClass.Open, Indoor, 1f, 0f,
        0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -80f, 0f, -80f, 0, null);
    public AcousticEnv Listener => At(default);
    public ListenerRouting Routing
    {
        get
        {
            var rev = Rev;
            var silent = new SendTap(D0AudioNode.DryBusIndex, -80f);
            return new(D0AudioNode.DryBusIndex, silent, silent, silent, silent, 0f, rev);
        }
    }
    public long Rev { get { _ = Indoor; return _rev; } }
    public OcclusionResult Occlusion(Vector3 source, ushort sourceRoom) =>
        new(0f, 20000f, 0f, PathKind.Direct, source);
    public ushort RoomAt(Vector3 p) => 0;
    public AmbienceProfileId? ProfileOf(ushort room) => null;
    public ReadOnlySpan<AcousticPortal> PortalsOf(ushort room) => ReadOnlySpan<AcousticPortal>.Empty;
}
