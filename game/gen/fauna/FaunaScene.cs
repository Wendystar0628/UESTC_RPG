using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Fauna;

/// <summary>
/// Turns one set of a site into world-space geometry for the kernel's render host (ENG-PERF: animals are scene nodes the kernel
/// creates through IRenderHost). Cats share one soft-goods bundle, birds one curved-prop bundle; colours go into ColorIds as the
/// lit palette IDs. Models are built once per key and shared. Pure: no IO, no engine types.
/// </summary>
public sealed class FaunaScene(Func<PaletteToken, byte> paletteId)
{
    private readonly Dictionary<FaunaModelKey, AnimalMesh> _models = [];
    private readonly Func<PaletteToken, byte> _paletteId = paletteId ?? throw new ArgumentNullException(nameof(paletteId));

    public AnimalMesh Model(FaunaModelKey key)
    {
        if (!_models.TryGetValue(key, out var mesh)) _models[key] = mesh = key.Build();
        return mesh;
    }

    public ContentOutput Build(IReadOnlyList<FaunaInstance> animals)
    {
        ArgumentNullException.ThrowIfNull(animals);
        if (animals.Count == 0) return ContentOutput.Empty;
        var meshes = new List<GeometryBundle>(2);
        foreach (var (spec, cats) in new[] { (FaunaStyle.Fur, true), (FaunaStyle.Feather, false) })
        {
            var group = animals.Where(a => (a.Model.Kind == FaunaKind.Cat) == cats).ToArray();
            if (group.Length == 0) continue;
            meshes.Add(Bundle(spec, group));
        }
        return new ContentOutput(meshes, []);
    }

    public int Triangles(IReadOnlyList<FaunaInstance> animals) => animals.Sum(a => Model(a.Model).Triangles);

    private GeometryBundle Bundle(ToonSpec spec, IReadOnlyList<FaunaInstance> animals)
    {
        var vertexCount = animals.Sum(a => Model(a.Model).Positions.Length);
        var position = new float[vertexCount * 3];
        var normal = new float[vertexCount * 3];
        var ids = new byte[vertexCount];
        var index = new List<int>(animals.Sum(a => Model(a.Model).Index.Length));
        var at = 0;
        foreach (var animal in animals)
        {
            var model = Model(animal.Model);
            var rotation = DetMath.RotationY(animal.Yaw);
            var world = Matrix4x4.CreateScale(animal.Scale) * rotation * Matrix4x4.CreateTranslation(animal.Position);
            for (var i = 0; i < model.Positions.Length; i++)
            {
                var p = Vector3.Transform(model.Positions[i], world);
                var n = Vector3.Normalize(Vector3.TransformNormal(model.Normals[i], rotation));
                position[(at + i) * 3] = p.X; position[(at + i) * 3 + 1] = p.Y; position[(at + i) * 3 + 2] = p.Z;
                normal[(at + i) * 3] = n.X; normal[(at + i) * 3 + 1] = n.Y; normal[(at + i) * 3 + 2] = n.Z;
                ids[at + i] = _paletteId(model.Colors[i]);
            }
            foreach (var k in model.Index) index.Add(at + k);
            at += model.Positions.Length;
        }
        var bundle = new GeometryBundle(new ToonRef(spec), position, normal, [.. index]) { ColorIds = ids };
        bundle.ValidateVertexData();
        return bundle;
    }
}
