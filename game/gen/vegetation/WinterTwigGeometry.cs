using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.MotherModels;
using static Campus.Gen.Vegetation.CrownCards;

namespace Campus.Gen.Vegetation;

/// <summary>Builds the winter twig shell and its deterministic crown channels.</summary>
internal static class WinterTwigGeometry
{
    private const int TwigMassRings = 9, TwigMassSides = 8;

    /// <summary>The twig-mass mesh of a mother model (veg_twig_mass); null for species without one. Tag 1 marks the far stand-in's copy.</summary>
    internal static VegetationPrototypeGeometry? TwigMass(TreePlan plan, PaletteCatalog palette, float tag)
    {
        if (!HasTwigMass(plan.Species)) return null;
        var w = new VegetationPrototypeGeometry(withUv: false);
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
        // CUSTOM0.x = height in main-branch tiers (whole numbers at the tiers, veg_twig_mass draws its layers there).
        float Tier(float y) => plan.TierPitch > 0f ? (y - plan.TierOrigin) / plan.TierPitch : 0f;
        void Vertex(Vector3 p, Vector3 n) =>
            w.Add(p, n, 1f, color, Gradient(plan, p, n), new Vector4(Tier(p.Y), 0f, 0f, hash), Vector4.Zero,
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
