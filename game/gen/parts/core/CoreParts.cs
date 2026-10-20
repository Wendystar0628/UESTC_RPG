using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Core;

/// <summary>Shared semantic LODs. Close geometry has chamfers; far geometry preserves the complete solid.</summary>
public abstract class CoreBoxPart<TParams> : IPartDef<TParams> where TParams : BoxPartParams
{
    protected CoreBoxPart()
    {
        Lods = new ReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>>(new SortedDictionary<LodLevel, Func<TParams, PartGeometry>>
        {
            [LodLevel.L0] = p => Build(p, true),
            [LodLevel.L1] = p => Build(p, false),
            [LodLevel.L2] = p => Build(p, false),
            [LodLevel.L3] = p => Build(p, false),
            [LodLevel.L4] = p => Build(p, false),
        });
    }
    public IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(44, 12);
    // IPartDef.Sockets has no parameter argument. Only parameter-independent anchors belong here.
    public IReadOnlyDictionary<string, Matrix4x4> Sockets { get; } = new ReadOnlyDictionary<string, Matrix4x4>(new SortedDictionary<string, Matrix4x4>(StringComparer.Ordinal) { ["origin"] = Matrix4x4.Identity });
    public Box3 Bounds(TParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        GeometryMath.Positive(p.Width, nameof(p.Width)); GeometryMath.Positive(p.Height, nameof(p.Height)); GeometryMath.Positive(p.Depth, nameof(p.Depth));
        return new(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }
    public IReadOnlyList<Collider> Collision(TParams p)
    {
        _ = Bounds(p);
        var at = Matrix4x4.Identity; at.M42 = p.Height * 0.5f;
        return [new BoxCollider(at, new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), CollisionTag) { Foot = p.Foot }];
    }
    protected virtual SurfaceTag CollisionTag => SurfaceTag.Prop;
    protected virtual PartGeometry Build(TParams p, bool close)
    {
        _ = Bounds(p);
        var material = close && p.Bevel > 0 ? p.Material with { Fx = p.Material.Fx | ToonFx.BevelLift } : p.Material;
        return PartGeometryKernel.BeveledBox(p.Width, p.Height, p.Depth, close ? p.Bevel : 0, material);
    }
}

[Part(PartCatalog.Box)]
public sealed class BoxPart : CoreBoxPart<BoxParams> { }

[Part(PartCatalog.Column)]
public sealed class ColumnPart : CoreBoxPart<ColumnParams> { }

[Part(PartCatalog.WallPanel)]
public sealed class WallPanelPart : CoreBoxPart<PanelParams>
{
    protected override SurfaceTag CollisionTag => SurfaceTag.Wall;
}

[Part(PartCatalog.Slab)]
public sealed class SlabPart : CoreBoxPart<PanelParams>
{
    protected override SurfaceTag CollisionTag => SurfaceTag.Ground;
}

[Part(PartCatalog.Trim)]
public sealed class TrimPart : CoreBoxPart<TrimParams>
{
    protected override PartGeometry Build(TrimParams p, bool close)
    {
        _ = Bounds(p);
        var material = close && p.Bevel > 0 ? p.Material with { Fx = p.Material.Fx | ToonFx.BevelLift } : p.Material;
        return PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(p.Depth, p.Height, close ? p.Bevel : 0),
            [new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0)], material);
    }
}
