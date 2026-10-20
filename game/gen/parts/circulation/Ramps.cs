using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// One ramp run (WLD-TERRAIN): Width across X, Height the rise, Depth the horizontal run, climbing toward +Z.
/// Grade at most 1:5 here; the 1:12 limit and 0.75 m segments are planned by <see cref="TerrainDrop"/>.
/// Variation: 0 solid deck with edge curbs and handrails on both sides, 1 deck and curbs only, 2 curbs and handrails
/// only, for ramps whose deck is the terrain height field. Collision: one walkable RampCollider over the deck
/// (none for variation 2, where the height field carries the walker).
/// </summary>
[Part(PartCatalog.Ramp)]
public sealed class RampPart() : CirculationPart<RampParams>(new(900, 400))
{
    private const float StubRun = 0.15f;

    private protected override Box3 BoundsOf(RampParams p) => Centered(p.Width, -S.Sink - S.RampCurbRise - 0.2f, p.Height + S.HandrailHeight + S.HandrailKnob + 0.01f, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(RampParams p)
    {
        Check(p);
        if (p.Variation == 2) return [];
        float w = p.Width * 0.5f, d = p.Depth * 0.5f;
        return
        [
            new RampCollider(new(-w, -0.02f, -d), new(w, -0.02f, -d), new(w, p.Height, d), new(-w, p.Height, d), S.StepColliderThickness, SurfaceTag.Ground)
            { Foot = Foot(p.Foot, S.RampFoot) },
        ];
    }

    private static void Check(RampParams p)
    {
        Range(p.Height / p.Depth, 0.005f, 0.2f, "ramp grade");
        Range(p.Width, 2 * S.RampCurbWidth + 0.6f, 30f, nameof(p.Width));
        Range(p.Depth, 2 * StubRun + 0.3f, 60f, nameof(p.Depth));
    }

    private protected override void Compose(MeshBuilder mesh, RampParams p, Detail detail)
    {
        Check(p);
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, g = p.Height / p.Depth;
        float Deck(float z) => (z + d) * g;
        if (p.Variation != 2)
            mesh.Extrude(Pick(p.Material, S.Stone), [new(-d, -S.Sink), new(-d, 0), new(d, p.Height), new(d, -S.Sink)], null, -w, w, Frame.Identity);
        if (detail == Detail.Mass && p.Variation != 2) return;
        // Curb top line, with short level stubs so both ends are cut vertically.
        var c = S.RampCurbRise;
        List<Vector2> curb = [new(-d, Deck(-d + StubRun) + c), new(-d + StubRun, Deck(-d + StubRun) + c), new(d - StubRun, Deck(d - StubRun) + c), new(d, Deck(d - StubRun) + c)];
        var bevel = detail == Detail.Full ? S.RailBevel : 0;
        var section = Profiles.Rect(0, c + 0.2f, 0, S.RampCurbWidth - S.FaceInset, bevel);
        // Curbs stand 0.1 m proud: under 0.25 m nothing casts a shadow (ART-MODELING), which also spares the
        // deck a saw-toothed grazing shadow at low sun.
        mesh.VerticalSweep(S.Trim, section, curb, -w + S.FaceInset, Frame.Identity, VertexFlag.NoShadow);
        mesh.VerticalSweep(S.Trim, section, curb, w - S.RampCurbWidth, Frame.Identity, VertexFlag.NoShadow);
        if (p.Variation == 1 || detail == Detail.Mass) return;
        List<Vector2> rail = [new(-d + 0.08f, Deck(-d + 0.08f) + S.HandrailHeight), new(d - 0.08f, Deck(d - 0.08f) + S.HandrailHeight)];
        foreach (var side in new[] { -1f, 1f })
            RailKit.Handrail(mesh, rail, side * (w - S.RampCurbWidth * 0.5f), z => RailKit.HeightAt(curb, z), detail, Frame.Identity);
    }
}

/// <summary>
/// Raised platform or landing block: Width × Depth, top at Height, sunk 0.1 m. L0 chamfers its edges.
/// Collision: a box whose top is walkable.
/// </summary>
[Part(PartCatalog.Platform)]
public sealed class PlatformPart() : CirculationPart<PanelParams>(new(44, 12))
{
    private protected override Box3 BoundsOf(PanelParams p) => Centered(p.Width, -S.Sink, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(PanelParams p)
    {
        var at = Matrix4x4.Identity; at.M42 = (p.Height - S.Sink) * 0.5f;
        return [new BoxCollider(at, new(p.Width * 0.5f, (p.Height + S.Sink) * 0.5f, p.Depth * 0.5f), SurfaceTag.Ground) { Foot = Foot(p.Foot, S.OutdoorStepFoot) }];
    }

    private protected override void Compose(MeshBuilder mesh, PanelParams p, Detail detail) =>
        mesh.Box(Pick(p.Material, S.Stone), new(-p.Width * 0.5f, -S.Sink, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f),
            detail == Detail.Full ? S.SlabBevel : 0, Frame.Identity);
}
