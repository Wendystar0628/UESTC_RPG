using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>
/// Collects closed kernel pieces, moves them rigidly and merges surfaces that share a ToonSpec into one bundle.
/// Kernel AO is visibility (1 = unoccluded, opening reveals below 1) and passes straight through, scaled by the piece's
/// own factor; a bundle that ends up fully unoccluded stores no AO (contract: missing AO = 1).
/// Groups keep first-use order, so output is independent of hash ordering. Pieces of one material that touch must overlap
/// by a few millimetres instead of sharing an edge, so every merged surface stays a set of closed solids.
/// </summary>
public sealed class PartBuilder
{
    private readonly List<Group> _groups = [];

    public PartBuilder Add(PartGeometry piece, Matrix4x4 at, byte flags = 0, float ao = 1f)
    {
        ArgumentNullException.ThrowIfNull(piece);
        foreach (var mesh in piece.Meshes)
        {
            var group = Find(mesh.Material.Spec);
            var first = group.Count;
            var count = mesh.Position.Length / 3;
            for (var i = 0; i < count; i++)
            {
                var p = Xf.Point(at, new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]));
                var n = Xf.Direction(at, new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]));
                group.Position.Add(p.X); group.Position.Add(p.Y); group.Position.Add(p.Z);
                group.Normal.Add(n.X); group.Normal.Add(n.Y); group.Normal.Add(n.Z);
                group.Uv.Add(mesh.Uv?[2 * i] ?? 0f); group.Uv.Add(mesh.Uv?[2 * i + 1] ?? 0f);
                group.Uv2.Add(mesh.Uv2?[2 * i] ?? 0f); group.Uv2.Add(mesh.Uv2?[2 * i + 1] ?? 0f);
                group.DecalBase.Add(mesh.DecalBaseColorIndices?[i] ?? 0);
                var visibility = (mesh.Ao?[i] ?? 1f) * ao;
                var combinedFlags = (byte)(flags | (mesh.Flags?[i] ?? 0));
                group.Flags.Add(combinedFlags); group.Ao.Add(visibility);
                group.HasFlags |= combinedFlags != 0;
                group.HasAo |= visibility < 1f;
            }
            foreach (var index in mesh.Index) group.Index.Add(first + index);
            group.HasUv2 |= mesh.Uv2 is not null;
            group.HasDecalBase |= mesh.DecalBaseColorIndices is not null;
            if (group.Count > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
        }
        return this;
    }

    public PartBuilder Add(PartGeometry piece, byte flags = 0) => Add(piece, Matrix4x4.Identity, flags);

    /// <summary>Exact extents of everything added so far (for parts whose bounds follow tilted, curved pieces).</summary>
    public (Vector3 Min, Vector3 Max) Measure()
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var group in _groups)
            for (var i = 0; i < group.Position.Count; i += 3)
            {
                var p = new Vector3(group.Position[i], group.Position[i + 1], group.Position[i + 2]);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
        return (min, max);
    }

    public PartGeometry Finish(Box3 bounds)
    {
        if (_groups.Count == 0) throw new InvalidOperationException("A part level must contain geometry.");
        var meshes = new GeometryBundle[_groups.Count];
        for (var g = 0; g < _groups.Count; g++)
        {
            var group = _groups[g];
            var mesh = new GeometryBundle(new ToonRef(group.Spec), [.. group.Position], [.. group.Normal], [.. group.Index],
                group.HasAo ? [.. group.Ao] : null, [.. group.Uv], group.HasUv2 ? [.. group.Uv2] : null)
            {
                Flags = group.HasFlags ? [.. group.Flags] : null,
                DecalBaseColorIndices = group.HasDecalBase ? [.. group.DecalBase] : null,
            };
            mesh.ValidateVertexData();
            meshes[g] = mesh;
        }
        return new(Array.AsReadOnly(meshes), bounds);
    }

    private Group Find(ToonSpec spec)
    {
        foreach (var group in _groups)
            if (group.Spec == spec) return group;
        var created = new Group(spec);
        _groups.Add(created);
        return created;
    }

    private sealed class Group(ToonSpec spec)
    {
        public ToonSpec Spec { get; } = spec;
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<float> Uv { get; } = [];
        public List<float> Uv2 { get; } = [];
        public List<ushort> DecalBase { get; } = [];
        public List<int> Index { get; } = [];
        public List<byte> Flags { get; } = [];
        public List<float> Ao { get; } = [];
        public bool HasFlags { get; set; }
        public bool HasAo { get; set; }
        public bool HasUv2 { get; set; }
        public bool HasDecalBase { get; set; }
        public int Count => Position.Count / 3;
    }
}

/// <summary>Vertex flag bits of the VF-A COLOR byte (ENG-PERF).</summary>
public static class VertexFlag
{
    /// <summary>Interior surface: shade from palette row 2, lit from row 3 times the light-patch gain (WLD-INTERIORS).</summary>
    public const byte Interior = 1;
    /// <summary>Pieces under 0.25 m (and thin inserts) stay out of the shadow pass (ART-MODELING).</summary>
    public const byte NoShadow = 8;
}
