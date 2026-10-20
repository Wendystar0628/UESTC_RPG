using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Read-only build result. Runtime queries never classify a surface or sample visibility.</summary>
public sealed class WorldFidelityModel : IFidelityModel
{
    private readonly FidelityFrozenData _data;
    private readonly IReadOnlyDictionary<FeatureId, IReadOnlyList<FacadeSegment>> _facades;
    private readonly IReadOnlyDictionary<FeatureId, RoofZone> _roofs;
    private readonly IReadOnlyDictionary<FeatureId, FidelityZone> _featureZones;

    public WorldFidelityModel(FidelityFrozenData data)
    {
        _data = data;
        if (data.Width <= 0 || data.Height <= 0 || data.Ground.Length != checked(data.Width * data.Height))
            throw new InvalidDataException("Invalid fidelity ground raster dimensions.");
        _facades = data.Facades.GroupBy(segment => segment.Building).ToDictionary(group => group.Key,
            group => (IReadOnlyList<FacadeSegment>)Array.AsReadOnly(group.OrderBy(segment => segment.Index).ToArray()));
        _roofs = data.Roofs.ToDictionary(roof => roof.Building);
        _featureZones = data.FeatureZones.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public string InputHash => _data.InputHash;
    public FidelityZone? Declared(FeatureId feature) => _featureZones.TryGetValue(feature, out var zone) ? zone : null;
    public IReadOnlyList<FacadeSegment> Facades(FeatureId building) => _facades.TryGetValue(building, out var segments)
        ? segments : throw new KeyNotFoundException($"No fidelity facades for building '{building.Value}'.");
    public RoofZone Roof(FeatureId building) => _roofs.TryGetValue(building, out var roof)
        ? roof : throw new KeyNotFoundException($"No fidelity roof for building '{building.Value}'.");

    public FidelityZone GroundAt(float x, float z)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(x));
        var column = (int)MathF.Floor((x - _data.Min.X) / WorldFidelityCompiler.GroundCell);
        var row = (int)MathF.Floor((z - _data.Min.Y) / WorldFidelityCompiler.GroundCell);
        return column < 0 || row < 0 || column >= _data.Width || row >= _data.Height
            ? FidelityZone.K5 : (FidelityZone)_data.Ground[row * _data.Width + column];
    }
}
