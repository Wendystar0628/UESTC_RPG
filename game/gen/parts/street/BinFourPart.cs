using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Four-stream sorting bin (四联分类垃圾箱, WLD-LIFE): a dark grey steel cabinet on a recessed toe, a light grey lid band
/// round the top (the colour band round the mouth, ART-MODELING), and four coloured doors on the front (−Z) — seen from the
/// front, left to right: grey (other), blue (recyclable), red (hazardous), green (kitchen). A hood over each door shades its
/// throw-in slot. No lettering or icons on the mainline; doors are separate materials, so the material line draws their edges.
/// Collision: one box, Metal; its 1 m top is reachable by a jump (WLD-LIFE).
/// </summary>
[Part(PartCatalog.BinFour, Owner = "WP-13")]
public sealed class BinFourPart : IPartDef<FurnitureParams>
{
    public BinFourPart() => Lods = Levels.Of<FurnitureParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<FurnitureParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(240, 80);

    public Box3 Bounds(FurnitureParams p)
    {
        Validate(p);
        var o = S.BinTopOverhang;
        var front = p.Depth * 0.5f + S.BinDoorProud + S.BinHoodProud;
        return Levels.Padded(new(-p.Width * 0.5f - o, 0, -Math.Max(front, p.Depth * 0.5f + o)), new(p.Width * 0.5f + o, p.Height, p.Depth * 0.5f + o));
    }

    public IReadOnlyList<Collider> Collision(FurnitureParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(FurnitureParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var body = S.Casing(S.DarkSteel);
        var lidY = p.Height - S.BinTop;
        if (near) b.AddBox(new(-hx + 0.03f, 0, -hz + 0.03f), new(hx - 0.03f, S.BinPlinth + S.Joint, hz - 0.03f), 0, body, VertexFlag.NoShadow);
        var bodyFrom = near ? S.BinPlinth : 0;
        b.AddBox(new(-hx, bodyFrom, -hz), new(hx, lidY + S.Joint, hz), near ? S.BinBevel : 0, body);
        var o = S.BinTopOverhang;
        b.AddBox(new(-hx - o, lidY, -hz - o), new(hx + o, p.Height, hz + o), near ? 0.015f : 0, S.Casing(S.LightSteel));
        // Doors, from the viewer's left (+X when facing the −Z front) to right.
        var inner = p.Width - 2 * S.BinFrame - 3 * S.BinMullion;
        var door = inner / 4;
        var y0 = S.BinPlinth + 0.06f; var y1 = lidY - 0.06f;
        var face = -hz;
        for (var i = 0; i < 4; i++)
        {
            var colour = S.Board(S.BinDoors[i]);
            var x1 = hx - S.BinFrame - i * (door + S.BinMullion); var x0 = x1 - door;
            b.AddBox(new(x0, y0, face - S.BinDoorProud), new(x1, y1, face + S.Joint), 0, colour);
            if (!near) continue;
            // Hood over the throw-in slot: a proud lip that casts a dark line under it.
            var hoodTop = y1 - 0.1f;
            b.AddBox(new(x0 + 0.03f, hoodTop - S.BinHoodHeight, face - S.BinDoorProud - S.BinHoodProud), new(x1 - 0.03f, hoodTop, face - S.BinDoorProud + S.Joint), 0, S.Casing(S.BinDoors[i]));
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(FurnitureParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 1.2f || p.Height < 0.6f || p.Depth < 0.3f) throw new ArgumentOutOfRangeException(nameof(p), "A four-stream bin is at least 1.2 × 0.6 × 0.3 m.");
    }
}
