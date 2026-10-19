using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

public static partial class MotherModels
{
    // ---------------------------------------------------------------------------------------------------------
    // Winter twig mass (Q-057): the usual middle-distance stand-in for a bare deciduous crown, a see-through shell on
    // the crown outline that veg_twig_mass alpha-blends in the twig colour and cross-fades against the near twig strokes.
    // Deciduous needle crowns (metasequoia) only: a narrowed cone around the crown axis, widest low and pointed at the
    // top, so neighbouring trees in a row keep a gap. Drawn at every level and by the far stand-in; it casts no shadow.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Species with a winter twig mass: deciduous needle crowns. Also decides the species-table winter colour.</summary>
    public static bool HasTwigMass(SpeciesInfo species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return species.Deciduous && species.Card == CardShape.Needle;
    }

    /// <summary>Twig-mass radius as a fraction of the crown envelope at the same height (narrower than the leafy crown).</summary>
    public const float TwigMassWidth = 0.68f;

    /// <summary>Relative crown height (0 bottom … 1 top) where the twig mass is widest; below it the base rounds in.</summary>
    public const float TwigMassWidest = 0.16f;

    /// <summary>Radial wobble of the shell, ±, so the cone is not a lathe-turned solid.</summary>
    public const float TwigMassWobble = 0.07f;

    private const int TwigMassRings = 9, TwigMassSides = 10;

    /// <summary>Warm red-brown of the twig mass (ext/tree_bark.json); also packed into the species table.</summary>
    public static readonly PaletteToken WinterTwig = new("tree.winterTwig.lit");
    public static readonly PaletteToken WinterTwigShade = new("tree.winterTwig.shade");

    public static ToonSpec TwigMassSpec() => new(WinterTwig, WinterTwigShade)
    {
        Kind = ToonKind.VegTwigMass,
        Steps = ShadeSteps.Soft,
        Normal = ToonNormal.Smooth,
        Line = LineClass.None,
        Translucency = 0f,
        Wet = 0f
    };

    /// <summary>The twig-mass mesh of a mother model (veg_twig_mass); null for species without one. Tag 1 marks the far stand-in's copy.</summary>
    private static MeshWriter? TwigMass(TreePlan plan, PaletteCatalog palette, float tag)
    {
        if (!HasTwigMass(plan.Species)) return null;
        var w = new MeshWriter(withUv: false);
        var color = palette.Resolve(WinterTwig).Value;
        var rng = new Rng(StableHash.Hash32(0x23A5u, "twigmass", plan.Species.Id, plan.Variant.ToString(CultureInfo.InvariantCulture)));
        var bottom = plan.CrownCenter.Y - plan.CrownRy;
        var axis = plan.CrownCenter with { Y = 0f };
        var hash = rng.NextFloat();
        // Radius at relative height u: the crown envelope narrowed, its base rounded in below the widest ring.
        float Radius(float u)
        {
            var e = plan.Envelope(MathF.Max(-1f + 2f * u, -1f + 2f * TwigMassWidest)) * TwigMassWidth;
            return u >= TwigMassWidest ? e : e * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(1f - u / TwigMassWidest, 2f) * 0.5f));
        }
        var wobble = new float[TwigMassRings * TwigMassSides];
        for (var i = 0; i < wobble.Length; i++) wobble[i] = 1f + (rng.NextFloat() * 2f - 1f) * TwigMassWobble;
        void Vertex(Vector3 p, Vector3 n) =>
            w.Add(p, n, 1f, color, Gradient(plan, p, n), new Vector4(axis with { Y = p.Y }, hash), Vector4.Zero,
                new Vector4(axis with { Y = p.Y }, tag), Vector2.Zero);
        // Bottom pole, rings, top pole (the leader tip).
        Vertex(axis + Vector3.UnitY * bottom, -Vector3.UnitY);
        for (var k = 0; k < TwigMassRings; k++)
        {
            var u = (k + 0.5f) / TwigMassRings;
            var y = bottom + u * 2f * plan.CrownRy;
            var r = Radius(u);
            var slope = (Radius(u + 0.02f) - Radius(u - 0.02f)) / (0.08f * plan.CrownRy);
            for (var j = 0; j < TwigMassSides; j++)
            {
                var phi = (j + (k & 1) * 0.5f) * 6.2831853f / TwigMassSides;
                var d = new Vector3(MathF.Cos(phi), 0f, MathF.Sin(phi));
                Vertex(axis + d * (r * wobble[k * TwigMassSides + j]) + Vector3.UnitY * y, Vector3.Normalize(d - Vector3.UnitY * slope));
            }
        }
        Vertex(axis + Vector3.UnitY * (bottom + 2f * plan.CrownRy), Vector3.UnitY);
        int Ring(int k, int j) => 1 + k * TwigMassSides + ((j % TwigMassSides) + TwigMassSides) % TwigMassSides;
        var top = 1 + TwigMassRings * TwigMassSides;
        // Clockwise seen from outside, the front face as in WriteBlob (veg_twig_mass culls back faces: one layer per tree).
        for (var j = 0; j < TwigMassSides; j++) w.Tri(0, Ring(0, j + 1), Ring(0, j));
        for (var k = 0; k + 1 < TwigMassRings; k++)
        {
            // Odd rings are turned half a side, so each band is a strip of alternating triangles.
            var shift = k & 1;
            for (var j = 0; j < TwigMassSides; j++)
            {
                w.Tri(Ring(k, j), Ring(k + 1, j + shift), Ring(k + 1, j - 1 + shift));
                w.Tri(Ring(k, j), Ring(k, j + 1), Ring(k + 1, j + shift));
            }
        }
        for (var j = 0; j < TwigMassSides; j++) w.Tri(top, Ring(TwigMassRings - 1, j), Ring(TwigMassRings - 1, j + 1));
        return w;
    }
}
