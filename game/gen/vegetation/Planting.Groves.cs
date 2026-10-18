using Campus.Gen.Geometry;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>Planting, groves on the green parcels (§3, §5.3).</summary>
public static partial class Planting
{
    // ---------------------------------------------------------------------------------------------------------
    // Groves: each green parcel has its own area^0.84 quota, candidate stream and keys.
    // ---------------------------------------------------------------------------------------------------------

    private static void PlantGroves(Site site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var areas = rules.Groves.Areas.Select(a => (Area: a, Box: site.Envelope(a.Name))).Where(a => a.Box is not null).ToList();
        var wash = rules.Groves.WashNear.Select(a => (Area: a, Box: site.Envelope(a.Name))).Where(a => a.Box is not null).ToList();
        foreach (var parcel in site.Green.Where(p => p.Area > 35f))
        {
            var target = (int)MathF.Round(GroveDensityCoefficient * MathF.Pow(parcel.Area, GroveAreaExponent));
            var rng = new Rng(StableHash.Hash32(site.Seed, "veg", "groves", parcel.Id));
            var planted = 0;
            for (var attempt = 0; attempt < target * 90 && planted < target; attempt++)
            {
                var x = parcel.Min.X + rng.NextFloat() * (parcel.Max.X - parcel.Min.X);
                var z = parcel.Min.Y + rng.NextFloat() * (parcel.Max.Y - parcel.Min.Y);
                var draw = rng.NextFloat();
                var p = new Vector2(x, z);
                if (!PlanGeometry.Contains(parcel.Rings, p) || !PlanGeometry.Contains(site.World.Boundary, p)) continue;
                var edge = Edge(parcel.Rings, p);
                var cluster = (MathF.Sin(x / 53f + MathF.Cos(z / 47f)) + MathF.Cos(z / 59f - x / 139f) + 2f) / 4f;
                if (parcel.Area > BigLawn && edge > EdgeKeep && cluster < OpenNoise) continue;
                var mix = areas.FirstOrDefault(a => Near(a.Box!.Value, p, a.Area.Radius)).Area?.Mix ?? rules.Groves.Mix;
                var stand = Hash01(site.Seed, "stand", (int)MathF.Floor(x / StandCell), (int)MathF.Floor(z / StandCell));
                var info = species.Get(Pick(mix, StandWeight * stand + (1f - StandWeight) * draw));
                var key = "grove:" + parcel.Id + ":" + attempt.ToString(CultureInfo.InvariantCulture);
                var washed = wash.Any(a => Near(a.Box!.Value, p, a.Area.Radius));
                // Crowns include their envelope: keep the crown inside the parcel's edge band (landscape.ts: 0.7 r).
                var probe = Probe(site, info, key);
                if (edge < probe.CrownRadius * 0.7f) continue;
                if (Try(site, info, p, key, washed, PlantSource.Grove, trees, crownClear: true)) planted++;
            }
        }
    }

    private static string Pick(IReadOnlyList<(string Species, float Weight)> mix, float u)
    {
        var total = mix.Sum(m => m.Weight);
        var acc = 0f;
        foreach (var (id, weight) in mix)
        {
            acc += weight / total;
            if (u < acc) return id;
        }
        return mix[^1].Species;
    }

    private static bool Near((Vector2 Min, Vector2 Max) box, Vector2 p, float radius)
    {
        var dx = MathF.Max(0f, MathF.Max(box.Min.X - p.X, p.X - box.Max.X));
        var dz = MathF.Max(0f, MathF.Max(box.Min.Y - p.Y, p.Y - box.Max.Y));
        return dx * dx + dz * dz <= radius * radius;
    }

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));
}
