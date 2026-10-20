using Campus.Contracts;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>Shared material classes, paint factories and tessellation policies for the core zone.</summary>
public static class CoreStyle
{
    /// <summary>Solids continue this far below the surface they stand on, so draping never opens a gap.</summary>
    public const float Sink = 0.1f;

    // ---- Tessellation per level ----
    public static int CircleSides(LodLevel lod) => lod switch
    {
        LodLevel.L0 => 96,
        LodLevel.L1 => 64,
        LodLevel.L2 => 40,
        LodLevel.L3 or LodLevel.L4 => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };

    public static int SmallSides(LodLevel lod) => lod switch
    {
        LodLevel.L0 => 16,
        LodLevel.L1 => 12,
        LodLevel.L2 => 8,
        LodLevel.L3 or LodLevel.L4 => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };

    // ---- Colours (palette families) ----
    /// <summary>Flag plinth, sundial tiers and pad: warm granite.</summary>
    public const string Granite = "stone.beige";
    /// <summary>
    /// Grey stone of the three bands: a pattern in the paving, not a line on it. Borrowed: of all registered colours its
    /// derived lit colour sits nearest the plaza granite (ground.curb) at about 0.06–0.07 lower OKLab L with chroma 0.015
    /// at 17:30 and noon; the building-class warm greys (wall.creamCoffee) derive to an apricot band at dusk.
    /// </summary>
    public const string BandStone = "equip.grey";

    /// <summary>Polished stainless steel: poles, sundial ring and gnomon.</summary>
    public const string Steel = "roof.silverMetal";

    public const string FlagWhite = "wall.greyWhite";

    public static string DefaultFamily(CoreMaterial material) => material switch
    {
        CoreMaterial.Stone => Granite,
        CoreMaterial.Inlay => BandStone,
        CoreMaterial.Metal => Steel,
        CoreMaterial.Cloth => FlagWhite,
        CoreMaterial.Wall or CoreMaterial.Trim or CoreMaterial.Roof or CoreMaterial.Sheet => BuildingFamily(material),
        CoreMaterial.Lawn => EntranceStyle.TerraceLawn,
        CoreMaterial.Foliage => EntranceStyle.BoxHedge,
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown core material."),
    };

    /// <summary>ToonSpec of a material class with a colour family; the non-colour fields define the class.</summary>
    public static ToonSpec Spec(CoreMaterial material, string family)
    {
        var spec = Campus.Gen.Sites.ZoneLooks.S(family);
        return material switch
        {
            CoreMaterial.Stone => spec with { Fx = ToonFx.BevelLift | ToonFx.Mottle, Wet = 0.6f },
            // No outline: inlays are part of the paving, so their edges are colour changes only.
            CoreMaterial.Inlay => spec with { Fx = ToonFx.DetailTiling, Line = LineClass.None, Wet = 0.6f },
            CoreMaterial.Metal => spec with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f },
            CoreMaterial.Cloth => spec with { Translucency = 0.5f, Line = LineClass.Fine, Wet = 0.3f },
            CoreMaterial.Wall or CoreMaterial.Trim or CoreMaterial.Roof or CoreMaterial.Sheet => BuildingSpec(material, spec),
            CoreMaterial.Lawn => spec with { Line = LineClass.None, Wet = 0.4f },
            CoreMaterial.Foliage => spec with { Normal = ToonNormal.Smooth, Steps = ShadeSteps.Soft, Line = LineClass.Tree, Wet = 0.3f },
            _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown core material."),
        };
    }

    public static Paint Stone(string family) => new(CoreMaterial.Stone, family);
    public static Paint Inlay(string family) => new(CoreMaterial.Inlay, family, (byte)(VertexFlag.Decal | VertexFlag.NoShadow));
    public static Paint Metal(string family) => new(CoreMaterial.Metal, family);
    public static Paint Cloth(string family) => new(CoreMaterial.Cloth, family);

    /// <summary>Parameters of a catalogue part with a material of this zone (stubs draw their bounding box until the owner delivers).</summary>
    public static ToonSpec PartMaterial(string family) => Spec(CoreMaterial.Metal, family);

    private static string BuildingFamily(CoreMaterial material) => material switch
    {
        CoreMaterial.Wall => LibraryStyle.LibraryWall,
        CoreMaterial.Trim => LibraryStyle.LibraryTrim,
        CoreMaterial.Roof => LibraryStyle.LibraryRoof,
        CoreMaterial.Sheet => LibraryStyle.LibraryVault,
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Not a building material."),
    };

    private static ToonSpec BuildingSpec(CoreMaterial material, ToonSpec spec) => material switch
    {
        CoreMaterial.Wall => spec with { Fx = ToonFx.BevelLift },
        CoreMaterial.Trim => spec with { Fx = ToonFx.BevelLift, Line = LineClass.Fine },
        CoreMaterial.Roof => spec with { Fx = ToonFx.Mottle, Wet = 0.85f },
        CoreMaterial.Sheet => spec with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Wet = 0.5f },
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Not a building material."),
    };

    public static Paint Wall(string family) => new(CoreMaterial.Wall, family);

    public static Paint Trim(string family) => new(CoreMaterial.Trim, family);

    public static Paint Roof(string family) => new(CoreMaterial.Roof, family);

    public static Paint Sheet(string family) => new(CoreMaterial.Sheet, family);

    /// <summary>Window glass (toon_glass with window cards); casts shadow, it closes the hollow shell.</summary>
    public static ToonSpec WindowGlass => Campus.Gen.Sites.ZoneLooks.Glass;

    /// <summary>See-through glass of the skylight and the front tower slot (paths P1 and P2, toon_glass clear mode).</summary>
    public static ToonSpec ClearGlass { get; } = WindowGlass with { Kind = ToonKind.GlassNear };
}
