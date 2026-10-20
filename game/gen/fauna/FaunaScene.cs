using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Contracts.Fauna;

namespace Campus.Gen.Fauna;

/// <summary>
/// Turns one set of a site into world-space geometry for the kernel's render host (ENG-PERF: animals are scene nodes the kernel
/// creates through IRenderHost). Cats share one soft-goods bundle, birds one curved-prop bundle; colours go into ColorIndices as the
/// lit palette IDs. Models are built once per key and shared. Pure: no IO, no engine types.
/// </summary>
public sealed class FaunaScene(PaletteCatalog catalog)
{
    private readonly Dictionary<FaunaModelKey, AnimalMesh> _models = [];
    private readonly PaletteCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

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
            meshes.AddRange(Bundles(spec, group));
        }
        return new ContentOutput(meshes, []);
    }

    public int Triangles(IReadOnlyList<FaunaInstance> animals) => animals.Sum(a => Model(a.Model).Triangles);

    private IReadOnlyList<GeometryBundle> Bundles(ToonSpec spec, IReadOnlyList<FaunaInstance> animals)
    {
        var sink = new GeometrySink(_catalog);
        foreach (var animal in animals)
        {
            var model = Model(animal.Model);
            var rotation = Matrix4x4.CreateRotationY(animal.Yaw);
            var world = Matrix4x4.CreateScale(animal.Scale) * rotation * Matrix4x4.CreateTranslation(animal.Position);
            var vertices = new GeometryVertex[model.Positions.Length];
            for (var i = 0; i < vertices.Length; i++)
                vertices[i] = new(Vector3.Transform(model.Positions[i], world),
                    Vector3.Normalize(Vector3.TransformNormal(model.Normals[i], rotation)),
                    ColorIndex: _catalog.Resolve(model.Colors[i]).Value);
            sink.Polygon(spec, vertices, model.Index);
        }
        return sink.Finish();
    }
}
