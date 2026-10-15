namespace Campus.Contracts;

/// <summary>Approved non-toon shader families. Append new members only; toon materials use ToonSpec.</summary>
public enum ShaderId : byte
{
    Sky = 0,
    Water = 1,
    WaterClear = 2,
    ReflectFlat = 3,
    FxGlow = 4,
    FxFall = 5,
    Precip = 6,
    VegTrunk = 7,
    VegCard = 8,
    VegImpostor = 9,
    VegGrass = 10,
    VegLotus = 11,
    DioramaSection = 12,
    DioramaFar = 13,
    MediumOverlay = 14,
    CineCloud = 15,
    UiPanel = 16,
    UiScreen = 17,
    MapBase = 18
}
