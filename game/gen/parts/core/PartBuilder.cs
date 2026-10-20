using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>Places kernel pieces and publishes through the shared accumulator; fully open pieces omit AO, as before.</summary>
public sealed class PartBuilder
{
    private readonly GeometrySink _sink = new();
    private Vector3 _min = new(float.MaxValue), _max = new(float.MinValue);
    private bool _hasGeometry;

    public PartBuilder Add(PartGeometry piece, Matrix4x4 at, byte flags = 0, float ao = 1f)
    {
        ArgumentNullException.ThrowIfNull(piece);
        foreach (var mesh in piece.Meshes)
        {
            var count = mesh.Position.Length / 3;
            var visibility = ao != 1f || mesh.Ao?.Any(value => value != 1f) == true ? new float[count] : null;
            var combinedFlags = flags != 0 || mesh.Flags?.Any(value => value != 0) == true ? new byte[count] : null;
            for (var i = 0; i < count; i++)
            {
                if (visibility is not null) visibility[i] = (mesh.Ao?[i] ?? 1f) * ao;
                if (combinedFlags is not null) combinedFlags[i] = (byte)(flags | (mesh.Flags?[i] ?? 0));
                var p = Xf.Point(at, new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]));
                _min = Vector3.Min(_min, p); _max = Vector3.Max(_max, p);
            }
            _sink.Append(mesh with { Ao = visibility, Flags = combinedFlags }, at);
            _hasGeometry |= mesh.Index.Length > 0;
        }
        return this;
    }

    public PartBuilder Add(PartGeometry piece, byte flags = 0) => Add(piece, Matrix4x4.Identity, flags);

    public (Vector3 Min, Vector3 Max) Measure() => (_min, _max);

    public PartGeometry Finish(Box3 bounds)
    {
        if (!_hasGeometry) throw new InvalidOperationException("A part level must contain geometry.");
        return new(_sink.Finish(), bounds);
    }
}
