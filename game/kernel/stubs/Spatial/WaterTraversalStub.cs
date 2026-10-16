using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

/// <summary>No water catalog is loaded. Unavailable is distinct from confirmed dry or a zero-height water surface.</summary>
[KernelService(typeof(IWaterTraversalQuery), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IWaterMotionEvents), Owner = "WP-00", IsStub = true)]
public sealed class WaterTraversalStub(IMapService maps) : IWaterTraversalQuery, IWaterMotionEvents
{
    public MapId Map => maps.Current;
    public long DataRevision => 0;
    public WaterQueryResult Sample(in WaterQuery query) =>
        new(WaterQueryStatus.NotReady, diagnosticCode: "water.catalog.stub");
    public WaterQueryResult Trace(Vector3 from, Vector3 to, NavSpace? space = null) =>
        new(WaterQueryStatus.NotReady, diagnosticCode: "water.catalog.stub");
    public WaterQueryStatus ExitsNear(in WaterQuery query, float radius, Span<WaterExitDecl> buffer, out int written)
    {
        written = 0;
        return WaterQueryStatus.NotReady;
    }
    public event Action<WaterMotionEvent>? Emitted { add { } remove { } }
}
