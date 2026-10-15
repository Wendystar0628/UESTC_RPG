using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Stubs;

/// <summary>Deliberately plain D0 boxes. Content owners replace these with compliant bevelled parts.</summary>
public abstract class BoxPartStub<TParams> : IPartDef<TParams> where TParams : BoxPartParams
{
    protected BoxPartStub()
    {
        var levels = new SortedDictionary<LodLevel, Func<TParams, PartGeometry>>();
        foreach (var level in Enum.GetValues<LodLevel>()) levels.Add(level, BuildBox);
        Lods = new ReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>>(levels);
    }

    public IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(12, 12);
    public Box3 Bounds(TParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Validate(p.Width, nameof(p.Width));
        Validate(p.Height, nameof(p.Height));
        Validate(p.Depth, nameof(p.Depth));
        return new(new(-p.Width * 0.5f, 0f, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(TParams p)
    {
        var bounds = Bounds(p);
        return [new BoxCollider(Matrix4x4.CreateTranslation(bounds.Center), bounds.Size * 0.5f, SurfaceTag.Prop) { Foot = p.Foot }];
    }

    private PartGeometry BuildBox(TParams p)
    {
        var bounds = Bounds(p);
        var min = bounds.Min;
        var max = bounds.Max;
        Vector3[] corners =
        [
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z),
            new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z),
            new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)
        ];
        // Godot front faces are clockwise when viewed from outside. Split vertices give flat face normals.
        int[][] faces = [[0, 1, 2, 3], [5, 4, 7, 6], [4, 0, 3, 7], [1, 5, 6, 2], [3, 2, 6, 7], [4, 5, 1, 0]];
        Vector3[] normals = [-Vector3.UnitZ, Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY];
        var position = new float[72];
        var normal = new float[72];
        var uv = new float[48];
        var index = new int[36];
        for (var face = 0; face < faces.Length; face++)
        {
            for (var corner = 0; corner < 4; corner++)
            {
                var vertex = face * 4 + corner;
                var point = corners[faces[face][corner]];
                position[vertex * 3] = point.X;
                position[vertex * 3 + 1] = point.Y;
                position[vertex * 3 + 2] = point.Z;
                normal[vertex * 3] = normals[face].X;
                normal[vertex * 3 + 1] = normals[face].Y;
                normal[vertex * 3 + 2] = normals[face].Z;
                uv[vertex * 2] = corner is 1 or 2 ? 1f : 0f;
                uv[vertex * 2 + 1] = corner >= 2 ? 1f : 0f;
            }
            var first = face * 4;
            var offset = face * 6;
            index[offset] = first;
            index[offset + 1] = first + 1;
            index[offset + 2] = first + 2;
            index[offset + 3] = first;
            index[offset + 4] = first + 2;
            index[offset + 5] = first + 3;
        }
        return new([new GeometryBundle(new(p.Material), position, normal, index, Uv: uv)], bounds);
    }

    private static void Validate(float value, string parameter)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameter, "Part dimensions must be finite and positive.");
    }
}
