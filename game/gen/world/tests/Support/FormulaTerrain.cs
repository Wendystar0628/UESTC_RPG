using Campus.Contracts;

namespace Campus.Gen.Tests.Support;

/// <summary>Writes the centimetre samples of one terrain tile.</summary>
internal delegate void TileFill(TileKey tile, int level, Span<short> cm);

/// <summary>
/// The one fake <see cref="ITerrain"/>: ground whose height is <paramref name="height"/>(x, z), with no pads and no water.
/// Facts default to level ground; a tile is never flat and is sampled from the height unless <paramref name="isFlat"/> or
/// <paramref name="fill"/> say otherwise. Pads are at 0 unless <paramref name="padY"/> answers per feature.
/// </summary>
internal sealed class FormulaTerrain(
    Func<float, float, float> height,
    Func<float, float, TerrainFacts>? facts = null,
    Func<TileKey, bool>? isFlat = null,
    TileFill? fill = null,
    Func<FeatureId, float>? padY = null) : ITerrain
{
    private static readonly TerrainFacts LevelFacts = new(0f, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity);

    public bool IsFlat(TileKey tile) => isFlat?.Invoke(tile) ?? false;
    public float HeightAt(float x, float z) => height(x, z);
    public float PadY(FeatureId feature) => padY?.Invoke(feature) ?? 0f;
    public float? WaterY(FeatureId water) => null;
    public TerrainFacts FactsAt(float x, float z) => facts?.Invoke(x, z) ?? LevelFacts;

    public void FillTile(TileKey tile, int level, Span<short> cm)
    {
        if (fill is not null)
        {
            fill(tile, level, cm);
            return;
        }
        var count = level == 4 ? 81 : 65;
        var size = level == 4 ? 2560f : 64f * (1 << level);
        var x0 = level == 4 ? -1280f : tile.X * size;
        var z0 = level == 4 ? -1280f : tile.Z * size;
        var step = size / (count - 1);
        for (var j = 0; j < count; j++)
            for (var i = 0; i < count; i++)
                cm[j * count + i] = (short)MathF.Round(HeightAt(x0 + i * step, z0 + j * step) * 100f);
    }
}
