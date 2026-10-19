using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Drain (雨水箅 and 边沟, ART-FIDELITY-DETAIL, ART-AMBIENCE §6): a flat ground piece laid along X; Width is its length, Height its
/// thickness (the placer sinks it so the top is flush with the road), Depth its width. Variation 0: the rain grate, a concrete frame round
/// a dark trough crossed by steel bars (they fade into one dark-grey plate beyond the near level, where the bar pitch drops under
/// 4 px); it carries the trickle of water that is heard in the rain and after it. 1: a trench of concrete cover slabs (盖板沟) with two
/// hand notches in each. 2: a light concrete L-gutter in long slabs with a joint between them. The caller's Material colours the
/// concrete. Walkable: no collider; the footstep comes from the ground grid patch, not from this part (SYS-AUDIO).
/// </summary>
[Part(PartCatalog.Drain, Owner = "WP-13")]
public sealed class DrainPart : IPartDef<TrimParams>
{
    public DrainPart() => Lods = Levels.Of<TrimParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4);
    public IReadOnlyDictionary<LodLevel, Func<TrimParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(520, 80);

    public Box3 Bounds(TrimParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    /// <summary>The grate's water sound: only in rain and after it (WLD-LIFE-STREET, SYS-AUDIO-AMBIENCE); the set waits for the registry.</summary>
    public IReadOnlyList<SoundSocketDecl> Sounds(TrimParams p)
    {
        Validate(p);
        if (p.Variation != 0) return [];
        return [new SoundSocketDecl("flow", new(S.DrainSet), Xf.Move(0, p.Height * 0.5f, 0), S.DrainDb, S.DrainRadius,
            Weather: WeatherMask.LightRain | WeatherMask.Rain | WeatherMask.Thunderstorm | WeatherMask.AfterRain)];
    }

    private PartGeometry Build(TrimParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        if (p.Variation == 0) Grate(b, p, near);
        else Slabs(b, p, near);
        return b.Finish(Bounds(p));
    }

    private static void Grate(PartBuilder b, TrimParams p, bool near)
    {
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var border = Math.Min(S.DrainBorder, p.Depth * 0.18f);
        var concrete = S.Stone(S.BodyColour(p.Material, S.KerbConcrete), false);
        var top = p.Height - S.DrainRecess;
        b.AddBox(new(-hx, 0, -hz), new(hx, top, hz), near ? 0.008f : 0, concrete);
        // The dark trough, then the bars (a single plate beyond the near level).
        b.AddBox(new(-hx + border, top - S.Joint, -hz + border), new(hx - border, p.Height - S.DrainRecess * 0.5f, hz - border), 0, S.Board(S.HazardBlack), VertexFlag.NoShadow);
        if (!near)
        {
            b.AddBox(new(-hx + border, top - S.Joint, -hz + border), new(hx - border, p.Height, hz - border), 0, S.Board(S.DarkSteel), VertexFlag.NoShadow);
            return;
        }
        var span = p.Width - 2 * border;
        var count = Math.Max(2, (int)MathF.Floor(span / S.DrainBarPitch));
        var pitch = span / count;
        var steel = S.Metal(S.GreyMetal);
        for (var i = 0; i < count; i++)
        {
            var x = -hx + border + (i + 0.5f) * pitch;
            b.AddBox(new(x - S.DrainBar * 0.5f, p.Height - S.DrainRecess * 0.5f - S.Joint, -hz + border), new(x + S.DrainBar * 0.5f, p.Height, hz - border), 0, steel, VertexFlag.NoShadow);
        }
    }

    /// <summary>A dark trench with concrete slabs laid on it, a gap between neighbours that shows the trench (variations 1 and 2).</summary>
    private static void Slabs(PartBuilder b, TrimParams p, bool near)
    {
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var gutter = p.Variation == 2;
        var colour = S.BodyColour(p.Material, gutter ? S.StoneWhite : S.KerbConcrete);
        var concrete = S.Stone(colour, false);
        var seam = S.DrainSeam;
        var pitch = gutter ? S.DrainGutterSlab : S.DrainCoverSlab;
        var count = Math.Max(1, (int)MathF.Round(p.Width / pitch));
        var slab = (p.Width - (count - 1) * seam) / count;
        var bed = p.Height - S.DrainSlabRise;
        b.AddBox(new(-hx, 0, -hz), new(hx, bed, hz), 0, S.Board(S.HazardBlack), VertexFlag.NoShadow);
        var top = p.Height - (gutter ? 0f : S.DrainHandle);
        for (var i = 0; i < count; i++)
        {
            var x0 = -hx + i * (slab + seam);
            b.AddBox(new(x0, bed - S.DrainSlabThick, -hz), new(x0 + slab, top, hz), near ? 0.008f : 0, concrete);
            if (gutter || !near) continue;
            // Two hand notches near the slab's ends, dark and raised a little so they read as holes.
            var cx = x0 + slab * 0.5f;
            foreach (var dz in new[] { -hz * 0.55f, hz * 0.55f })
                b.AddBox(new(cx - 0.05f, top - S.Joint, dz - 0.015f), new(cx + 0.05f, p.Height, dz + 0.015f), 0, S.Board(S.HazardBlack), VertexFlag.NoShadow);
        }
    }

    private static void Validate(TrimParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown drain variation.");
        if (p.Width < 0.3f || p.Depth < 0.12f || p.Height < 0.04f || p.Height > 0.25f) throw new ArgumentOutOfRangeException(nameof(p), "A drain is at least 0.3 × 0.12 m and 4–25 cm thick.");
    }
}
