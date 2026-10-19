using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Lamp-pole broadcast horn (灯杆广播喇叭, ART-FIDELITY-DETAIL, ART-AMBIENCE): a grey collar that clamps round the lamp pole, with
/// reflex horns flaring out of it, each horn a pale body with a dark driver in its mouth. Variation 0: two horns back to back along Z
/// (up and down the road). 1: four horns, a cross. The part's origin is the pole's axis at the collar's foot, so a placer puts it at
/// the pole at 3.5–4.5 m. Width × Depth is the footprint the horns reach; Height is the horn's bell. It carries the evening broadcast
/// as a sound socket (WLD-LIFE-STREET: an original melody from 17:40, then voices too faint to follow, from the lamp-pole horns). Visual
/// only: it hangs out of reach.
/// </summary>
[Part(PartCatalog.StreetSpeaker, Owner = "WP-13")]
public sealed class StreetSpeakerPart : IPartDef<EquipmentParams>
{
    public StreetSpeakerPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1300, 420);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    /// <summary>The broadcast, heard from afar (WLD-LIFE-STREET); the set and its time window (17:40–18:10) wait for the sound-set registry.</summary>
    public IReadOnlyList<SoundSocketDecl> Sounds(EquipmentParams p)
    {
        Validate(p);
        return [new SoundSocketDecl("broadcast", new(S.BroadcastSet), Xf.Move(0, p.Height * 0.5f, 0), S.BroadcastDb, S.BroadcastRadius)];
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var segments = near ? S.RoundSegments : S.RoundSegmentsFar;
        var b = new PartBuilder();
        var cy = p.Height * 0.5f;
        var rc = S.SpeakerCollar;
        var grey = S.Pole(S.GreyMetal);
        // Collar: a short drum with chamfered rims, the lamp pole running through it.
        b.AddLathe(near
            ? PartShapes.Creased(new(0, 0.02f), new(rc - 0.015f, 0.02f), new(rc, 0.035f), new(rc, p.Height - 0.035f), new(rc - 0.015f, p.Height - 0.02f), new(0, p.Height - 0.02f))
            : PartShapes.Creased(new(0, 0.02f), new(rc, 0.02f), new(rc, p.Height - 0.02f), new(0, p.Height - 0.02f)), segments, grey, Matrix4x4.Identity);
        var bell = Math.Min(p.Height * 0.5f - 0.02f, S.SpeakerBell);
        // Each horn is built pointing up the lathe's Y; the matrix turns that onto −Z, +Z, −X or +X.
        (Matrix4x4 Turn, float Mouth)[] horns = p.Variation == 0
            ? [(Xf.RotX(-90f), p.Depth * 0.5f), (Xf.RotX(90f), p.Depth * 0.5f)]
            : [(Xf.RotX(-90f), p.Depth * 0.5f), (Xf.RotX(90f), p.Depth * 0.5f), (Xf.RotX(-90f).Then(Xf.RotY(90f)), p.Width * 0.5f), (Xf.RotX(-90f).Then(Xf.RotY(-90f)), p.Width * 0.5f)];
        foreach (var (turn, mouth) in horns)
            Horn(b, bell, rc - S.Joint, mouth - S.Joint, near, segments, turn.Then(Xf.Move(0, cy, 0)));
        return b.Finish(Bounds(p));
    }

    /// <summary>One horn along the lathe axis: a neck at <paramref name="y0"/> flaring to the bell at <paramref name="y1"/>, a dark driver in the mouth.</summary>
    private static void Horn(PartBuilder b, float bell, float y0, float y1, bool near, int segments, Matrix4x4 at)
    {
        var length = y1 - y0; var neck = S.SpeakerNeck;
        // A trumpet curve: slow at first, flaring fast at the mouth.
        float[] t = near ? [0f, 0.45f, 0.72f, 0.9f, 1f] : [0f, 0.7f, 1f];
        var pts = t.Select(u => new Vector2(neck + (bell - neck) * MathF.Pow(u, 2.2f), y0 + length * u)).ToArray();
        var bands = new List<LatheBand> { LatheBand.Flat(0, y0, pts[0].X, y0) };
        bands.AddRange(PartShapes.Smooth(pts));
        bands.Add(LatheBand.Flat(pts[^1].X, y1, 0, y1));
        b.AddLathe(bands, segments, S.Pole(S.PaintWhite), at);
        // The driver: a dark disc standing a hair proud of the mouth, a rim of the horn left round it.
        var driver = bell * 0.78f;
        b.AddLathe(PartShapes.Creased(new(0, y1 - S.Joint), new(driver, y1 - S.Joint), new(driver, y1 + S.SpeakerDriverProud), new(0, y1 + S.SpeakerDriverProud)), segments, S.Casing(S.DarkSteel), at, VertexFlag.NoShadow);
    }

    private static void Validate(EquipmentParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown street-speaker variation.");
        if (p.Width < 0.3f || p.Depth < 0.3f || p.Height < 0.2f) throw new ArgumentOutOfRangeException(nameof(p), "A broadcast horn is at least 0.3 × 0.2 × 0.3 m.");
    }
}
