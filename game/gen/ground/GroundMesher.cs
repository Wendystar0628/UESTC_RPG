using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>Builds ground geometry and exposes the same surface interpretation to collision and masks.</summary>
internal sealed class GroundMesher
{
    private readonly GroundSurfaceProfile _surfaces;
    private readonly GroundTilePartition _partition;
    private readonly GroundContourMesher _contours;
    private readonly TileField _field;

    public GroundMesher(TileField field, GroundSection section, LodLevel lod, GroundLayout layout,
        TerrainGrid grid, GroundSurfaceSink sink)
    {
        _field = field;
        _surfaces = new(field, section, lod);
        _partition = new(field, lod, grid, _surfaces);
        var emitter = new GroundGeometryEmitter(field, section, layout, grid, sink, _surfaces, _partition);
        _contours = new(field, _surfaces, _partition, emitter);
    }

    public void Run() => _contours.Run();

    public GroundSurface Classify(int code) => _surfaces.Classify(code);

    public GroundSurface SurfaceAt(int i, int j) => _surfaces.Classify(_partition.Codes[j * _field.S + i]);

    public float HeightAt(int i, int j) => _surfaces.LatticeHeight(SurfaceAt(i, j), i, j);
}
