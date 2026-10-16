using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.West.Framework;
using L = Campus.Gen.Zones.West.WestTune.Look;
using M = Campus.Gen.Zones.West.WestTune.Motto;

namespace Campus.Gen.Zones.West.Motto;

/// <summary>
/// 校训石 (MAP-QSH-WEST §2): one cream-white natural boulder lying on its side, about 2 m × 6.6 m, on a thin granite pad
/// that draws the line between stone foot and ground. Its dressed face turns toward P10 and is left blank until the scene text atlas carries the
/// motto (contract-26). The low lawn lamp beside it is WP-39's (lighting-schedule.json).
/// </summary>
public sealed class MottoStoneSite : WestSite
{
    internal static readonly BoulderShape Shape = new(M.Length, M.Depth, M.Height, FaceHeight: 1.0f, FaceOut: 0.42f, FaceTilt: 0.16f, Shear: 0.3f) { Rise = 0.12f };

    /// <summary>The stone's frame: origin at its plan centre, local +Z toward its face's bearing.</summary>
    internal static PlanFrame Frame => PlanMath.Facing(M.At, M.Facing);

    public override string Id => "19a.motto";

    public override void Build(SiteBuilder b)
    {
        var f = Frame;
        var y = b.Ground(M.At);
        var at = f.At(0, y - 0.06f, 0);
        float hx = M.Length * 0.5f + M.PadMargin, hz = M.Depth * 0.5f + M.PadMargin;
        GridFacade.Box(b, f, -hx, hx, y - 0.1f, y + 0.03f, -hz, hz, L.Granite, b.Detail == SiteDetail.Full ? 0.02f : 0f);
        // Few, broad facets: a rough block-faced stone (粗粝的块面石), not a turned pebble.
        var (segments, rings) = b.Detail switch { SiteDetail.Full => (16, 5), SiteDetail.Plain => (12, 4), _ => (9, 3) };
        b.Add(Shapes.Faceted(Solids.Boulder(Shape, segments, rings, L.CreamStone)), at);
        if (b.Detail == SiteDetail.Mass) return;
        // Text hook: the stone face stays blank until the scene text atlas (contract-26) arrives. Then add on the dressed face
        // (Shape.FaceAt, Look.Red lacquer) two lines "求实求真" / "大气大为" laid out by CharacterSize, CharacterPitch, LineGap.
    }

    /// <summary>One box with a top (SYS-WALK §13): the stone is about 2 m high, not climbable.</summary>
    public override void Collide(SiteColliders c)
    {
        var y = c.Ground(M.At.X, M.At.Y);
        c.Box(Frame.At(0, y + M.Height * 0.5f, 0), new(M.Length * 0.5f, M.Height * 0.5f, M.Depth * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }

    /// <summary>Q1 (walk, K1): 3 m in front of the face, eye height, looking at the characters and the stone's foot.</summary>
    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            var out1 = PlanMath.Heading(M.Facing);
            var eye = M.At + out1 * (M.Depth * 0.5f + 3f);
            return [Probe("Q1", M.Entity, new(eye.X, 1.62f, eye.Y), -out1, -8f, 3f)];
        }
    }
}
