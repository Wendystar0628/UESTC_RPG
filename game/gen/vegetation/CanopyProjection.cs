using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// The fallen-leaf and canopy channels WP-23 owns in the two ground masks (WLD-VEGETATION §6, §9.5, §11.7), painted
/// from one crown projection of the planting layout:
/// ground_mask R (ginkgo leaves), G (other leaves and petals) and ground_mask2 B (canopy shelter). Both masks are RGBA8,
/// row-major from the map rectangle's minimum corner with rows along +z, the layout WP-15's GroundMaskSet uploads.
/// Potentials only ever grow (max), so the projection can be painted over WP-15's defaults. Ground shading and the
/// thresholds stay WP-15's (coverage = step(noise, mask × season amount)).
/// </summary>
public static class CanopyProjection
{
    /// <summary>Leaves lie a little beyond the crown edge; potential falls from the trunk (densest) to this many crown radii.</summary>
    public const float LeafReach = 1.15f;

    /// <summary>Canopy shelter reaches the crown edge: 1 at the trunk, 0 at the rim (§9.5).</summary>
    public const float ShelterReach = 1f;

    /// <summary>The ginkgo avenue's carriageway is covered wall to wall (R 1), except two lighter tracks (§3, §6).</summary>
    public const float TrackOffset = 1.1f;
    public const float TrackWidth = 0.9f;
    public const byte TrackPotential = 150;

    /// <summary>The avenue surface always carries half a G potential, so spring leaves a thin petal layer on it (§9.5).</summary>
    public const byte AvenuePetals = 128;

    /// <summary>Byte offsets of the channels within one RGBA8 texel.</summary>
    private const int R = 0, G = 1, B = 2;

    public static void Paint(PlantingLayout layout, PlantingRules rules, IWorldModel world, MapRect rect, float metersPerPixel,
        Span<byte> mask1, Span<byte> mask2)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(rect);
        var width = (int)MathF.Round((rect.Max.X - rect.Min.X) / metersPerPixel);
        var height = (int)MathF.Round((rect.Max.Y - rect.Min.Y) / metersPerPixel);
        if (mask1.Length != width * height * 4 || mask2.Length != mask1.Length)
            throw new ArgumentException("Masks must be RGBA8 over the map rectangle at the given resolution.", nameof(mask1));
        var grid = new Grid(rect, metersPerPixel, width, height);

        foreach (var tree in layout.Trees)
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            var r = tree.CrownRadius;
            // Ginkgo leaves go to R; the other autumn-coloured deciduous trees (and spring petals) to G; evergreens drop none.
            if (tree.Species.Id == "ginkgo") grid.Cone(mask1, R, p, r * LeafReach, 255);
            else if (tree.Species.Deciduous) grid.Cone(mask1, G, p, r * LeafReach, 255);
            grid.Cone(mask2, B, p, r * ShelterReach, 255);
        }

        // The avenue: the rule whose rows are ginkgo; its carriageway carries the full leaf carpet with two worn tracks.
        foreach (var rule in rules.Roads.Where(r => r.Species.Contains("ginkgo")))
            foreach (var road in world.Network.Roads.Where(l => Name(l) == rule.Name).OrderBy(l => l.Id.Value, StringComparer.Ordinal))
                grid.Corridor(mask1, road.Line.Points, road.Width * 0.5f);
    }

    private static string? Name(NetworkLine line)
    {
        if (line.Props is not { ValueKind: JsonValueKind.Object } props) return null;
        foreach (var name in new[] { "name:zh", "name" })
            if (props.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    private readonly record struct Grid(MapRect Rect, float Step, int Width, int Height)
    {
        private Vector2 Centre(int px, int pz) => new(Rect.Min.X + (px + 0.5f) * Step, Rect.Min.Y + (pz + 0.5f) * Step);

        private (int X, int Z) Pixel(Vector2 p) =>
            ((int)MathF.Floor((p.X - Rect.Min.X) / Step), (int)MathF.Floor((p.Y - Rect.Min.Y) / Step));

        private void Max(Span<byte> mask, int channel, int px, int pz, byte value)
        {
            var i = (pz * Width + px) * 4 + channel;
            if (mask[i] < value) mask[i] = value;
        }

        /// <summary>A disc fading linearly from <paramref name="centre"/> to 0 at the rim, kept by maximum.</summary>
        public void Cone(Span<byte> mask, int channel, Vector2 at, float radius, byte centre)
        {
            if (!(radius > 0f)) return;
            var (x0, z0) = Pixel(at - new Vector2(radius));
            var (x1, z1) = Pixel(at + new Vector2(radius));
            for (var pz = Math.Max(z0, 0); pz <= Math.Min(z1, Height - 1); pz++)
                for (var px = Math.Max(x0, 0); px <= Math.Min(x1, Width - 1); px++)
                {
                    var d = Vector2.Distance(Centre(px, pz), at);
                    if (d < radius) Max(mask, channel, px, pz, (byte)MathF.Round(centre * (1f - d / radius)));
                }
        }

        /// <summary>The carriageway of one road: R full, the two worn tracks lighter; G at least the petal floor.</summary>
        public void Corridor(Span<byte> mask, IReadOnlyList<Vector2> line, float half)
        {
            for (var i = 1; i < line.Count; i++)
            {
                var a = line[i - 1];
                var b = line[i];
                var (x0, z0) = Pixel(Vector2.Min(a, b) - new Vector2(half));
                var (x1, z1) = Pixel(Vector2.Max(a, b) + new Vector2(half));
                for (var pz = Math.Max(z0, 0); pz <= Math.Min(z1, Height - 1); pz++)
                    for (var px = Math.Max(x0, 0); px <= Math.Min(x1, Width - 1); px++)
                    {
                        var d = MathF.Sqrt(PlanGeometry.SegmentDistanceSquared(Centre(px, pz), a, b));
                        if (d >= half) continue;
                        var track = MathF.Abs(d - TrackOffset) < TrackWidth * 0.5f;
                        var index = (pz * Width + px) * 4;
                        // The worn tracks overwrite the canopy cones: they are footfall, not missing leaves.
                        mask[index + R] = track ? TrackPotential : (byte)255;
                        Max(mask, G, px, pz, AvenuePetals);
                    }
            }
        }
    }
}
