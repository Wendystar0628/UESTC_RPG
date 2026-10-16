using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Parts.Openings;

namespace Campus.Gen.Buildings.Build;

/// <summary>
/// Fills the openings of one building at one level with WP-10 parts, placed by PartCatalog ID: window or
/// curtain-wall frames and entrance doors on OpeningPlacement frames, curtains from OpeningPlacement.Curtain, and
/// glass from OpeningGlass with the building's window numbers. Reveals and sills stay with the wall (BuildingMesher).
/// A part's +X runs along the facade (EdgeFrame.Along) and its −Z out of the building.
/// </summary>
public sealed class FacadeOpenings
{
    private readonly BuildingPlan _p;
    private readonly WindowBlock _block;
    private readonly LodLevel _lod;
    private readonly MeshSink _sink;
    private readonly ChannelId _channel;
    // Looked up, never enumerated: the openings of a building share a few part shapes.
    private readonly Dictionary<(string Part, object Params, LodLevel Level), PartGeometry> _parts = [];

    public FacadeOpenings(BuildingPlan plan, WindowBlock block, LodLevel lod, MeshSink sink)
    {
        _p = plan;
        _block = block;
        _lod = lod;
        _sink = sink;
        _channel = LightChannelCatalog.Id(plan.Profile.Channel);
    }

    /// <summary>Window number of an opening: BaseId + row × TotalColumns + building-grid column.</summary>
    public static int Number(BuildingPlan plan, WindowBlock block, FacadeLayout layout, Opening o) =>
        block.Base + o.Row * plan.TotalColumns + layout.ColumnStart + o.Bay;

    /// <summary>Card type of an opening, hashed from its window number (WLD-INTERIORS §5.1).</summary>
    public static WindowCard Card(BuildingPlan plan, Opening o, int number) =>
        plan.CardOf(o.Kind, StableHash.Hash32(plan.Seed, "card", number.ToString(CultureInfo.InvariantCulture)));

    /// <summary>Part placement of an opening: bottom centre of the clear opening, <paramref name="s"/> out from the wall plane.</summary>
    public static Matrix4x4 Frame(EdgeFrame f, Opening o, float s, float lift = 0f) =>
        OpeningPlacement.Frame(f.At((o.U0 + o.U1) * 0.5f, s, o.Y0 + lift), f.Out);

    /// <summary>The light-inventory entries of a building (ContentOutput.LooseWindows): one per glazed opening, doors included.</summary>
    public static IReadOnlyList<WindowDecl> Declarations(BuildingPlan plan, WindowBlock block)
    {
        var list = new List<WindowDecl>();
        var channel = LightChannelCatalog.Id(plan.Profile.Channel);
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var f = EdgeFrame.Of(skin, e);
                var layout = plan.Facades[fi];
                foreach (var o in plan.Openings[fi])
                    list.Add(OpeningGlass.Declare(Frame(f, o, -plan.Reveal), o.U1 - o.U0, o.Y1 - o.Y0, block.Base,
                        layout.ColumnStart + o.Bay, o.Row, plan.TotalColumns, channel));
            }
        }
        return list;
    }

    /// <summary>
    /// L0/L1: the opening's frame and glass on the glass plane. In K1 and K2 (<paramref name="zone"/>, from the fidelity
    /// data) the full WP-10 window part and its curtain; in K3 and beyond the simple window (OpeningRules). The entrance
    /// door is the WP-10 door in every zone.
    /// </summary>
    public void Fill(EdgeFrame f, FacadeLayout layout, Opening o, FidelityZone zone)
    {
        var sG = -_p.Reveal;
        float w = o.U1 - o.U0, h = o.Y1 - o.Y0;
        var number = Number(_p, _block, layout, o);
        var card = Card(_p, o, number);
        if (o.Kind == BayKind.Door)
        {
            // The door closes the opening itself; its glass panes take the door bay's window cell.
            var door = Defaults<DoorParams>(PartCatalog.Door) with { Width = w, Height = h, Leaves = w >= OpeningRules.TwoLeavesFrom ? 2 : 1 };
            Place(PartCatalog.Door, door, Frame(f, o, sG + OpeningRules.DoorForward),
                new PartGlass(new(layout.ColumnStart + o.Bay, o.Row), Uv2()));
            return;
        }
        var part = _p.Profile.Window;
        var (columns, rows) = OpeningRules.Grid(_p.Profile, part, w, h);
        if (zone > OpeningRules.FullPartsUpTo)
        {
            SimpleWindow(f, o, columns, rows, sG);
            Glass(f, layout, o, sG, card);
            return;
        }
        Place(part, Defaults<WindowParams>(part) with { Width = w, Height = h, Columns = columns, Rows = rows }, Frame(f, o, sG),
            gaskets: OpeningRules.Gaskets, behind: OpeningRules.CullBehindGlass ? OpeningRules.BehindGlass : null);
        if (_p.Profile.Curtains && _lod <= OpeningRules.CurtainsUpTo && part != PartCatalog.CurtainWall && OpeningRules.Curtained(card) &&
            OpeningPlacement.Curtain(_p.Seed, number, w - 2f * OpeningRules.CurtainSideInset, h - OpeningRules.CurtainHeadInset) is { } curtain)
            Place(PartCatalog.Curtain, curtain,
                Frame(f, o, sG + curtain.Depth * 0.5f + OpeningRules.CurtainGlassGap, (h - curtain.Height) * 0.5f),
                level: _lod > OpeningRules.CurtainDetail ? _lod : OpeningRules.CurtainDetail);
        Glass(f, layout, o, sG, card);
    }

    /// <summary>
    /// Simple window of K3 and beyond: a frame ring pressed into the reveal, with continuous mullions and a transom
    /// between them (the WP-10 casement light layout). Each member is a front face, a single 45° chamfer on its inner
    /// edges and returns back to the glass: at L0 24 triangles for the ring and 10 for each bar; at L1 the ring alone
    /// without chamfer (16). Nothing behind the glass. Metal of the WP-10 frames, no shadow.
    /// </summary>
    private void SimpleWindow(EdgeFrame f, Opening o, int columns, int rows, float sG)
    {
        var c = _lod == LodLevel.L0 ? OpeningRules.SimpleChamfer : 0f;
        var e = OpeningRules.SimplePressIn;
        var face = OpeningRules.SimpleFrameFace;
        var bar = OpeningRules.SimpleBarFace;
        var sF = sG + OpeningRules.SimpleFrameProud;
        float iu0 = o.U0 + face, iu1 = o.U1 - face, iy0 = o.Y0 + face, iy1 = o.Y1 - face;
        // Ring: corner loops for the outer edge (front), the chamfer's outer edge (front) and the clear light (back).
        (float U, float Y)[] Loop(float u0, float y0, float u1, float y1) => [(u0, y0), (u1, y0), (u1, y1), (u0, y1)];
        var outer = Loop(o.U0 - e, o.Y0 - e, o.U1 + e, o.Y1 + e);
        var bevel = Loop(iu0 - c, iy0 - c, iu1 + c, iy1 + c);
        var light = Loop(iu0, iy0, iu1, iy1);
        var a = f.Along;
        var up = Vector3.UnitY;
        Vector3[] inward = [up, -a, -up, a];   // bottom, right, top, left: towards the light
        for (var k = 0; k < 4; k++)
        {
            var n = (k + 1) % 4;
            Band(outer[k], outer[n], sF, bevel[n], bevel[k], sF, f.Out);
            if (c > 0f) Band(bevel[k], bevel[n], sF, light[n], light[k], sF - c, Vector3.Normalize(f.Out + inward[k]));
            Band(light[k], light[n], sF - c, light[n], light[k], sG, inward[k]);
        }
        // Mullions run through between the ring's chamfers; transoms sit a step behind and span the whole light.
        if (_lod > OpeningRules.SimpleBarsUpTo) return;
        var lightWidth = (iu1 - iu0 - (columns - 1) * bar) / columns;
        for (var k = 1; k < columns; k++)
        {
            var m0 = iu0 + k * lightWidth + (k - 1) * bar;
            Bar(m0, m0 + bar, iy0 - c, iy1 + c, sF, vertical: true);
        }
        if (rows < 2) return;
        var top = Math.Min(Math.Clamp((o.Y1 - o.Y0) * OpeningRules.TransomShare, OpeningRules.TransomMin, OpeningRules.TransomMax), (iy1 - iy0) * 0.5f);
        Bar(iu0 - c, iu1 + c, iy1 - top - bar, iy1 - top, sF - OpeningRules.SimpleTransomStep, vertical: false);

        void Band((float U, float Y) p0, (float U, float Y) p1, float s01, (float U, float Y) p2, (float U, float Y) p3, float s23, Vector3 normal) =>
            _sink.Quad(SimpleMetal,
                new V(f.At(p0.U, s01, p0.Y), normal, new(p0.U, p0.Y)), new V(f.At(p1.U, s01, p1.Y), normal, new(p1.U, p1.Y)),
                new V(f.At(p2.U, s23, p2.Y), normal, new(p2.U, p2.Y)), new V(f.At(p3.U, s23, p3.Y), normal, new(p3.U, p3.Y)));

        // A bar from (u0, y0) to (u1, y1) with its front at sFront: front face, chamfers and returns along its length.
        void Bar(float u0, float u1, float y0, float y1, float sFront, bool vertical)
        {
            if (vertical)
            {
                Band((u0 + c, y0), (u1 - c, y0), sFront, (u1 - c, y1), (u0 + c, y1), sFront, f.Out);
                if (c > 0f)
                {
                    Band((u0, y0), (u0, y1), sFront - c, (u0 + c, y1), (u0 + c, y0), sFront, Vector3.Normalize(f.Out - a));
                    Band((u1 - c, y0), (u1 - c, y1), sFront, (u1, y1), (u1, y0), sFront - c, Vector3.Normalize(f.Out + a));
                }
                Band((u0, y0), (u0, y1), sFront - c, (u0, y1), (u0, y0), sG, -a);
                Band((u1, y0), (u1, y1), sFront - c, (u1, y1), (u1, y0), sG, a);
            }
            else
            {
                Band((u0, y0 + c), (u1, y0 + c), sFront, (u1, y1 - c), (u0, y1 - c), sFront, f.Out);
                if (c > 0f)
                {
                    Band((u0, y0), (u1, y0), sFront - c, (u1, y0 + c), (u0, y0 + c), sFront, Vector3.Normalize(f.Out - up));
                    Band((u0, y1 - c), (u1, y1 - c), sFront, (u1, y1), (u0, y1), sFront - c, Vector3.Normalize(f.Out + up));
                }
                Band((u0, y0), (u1, y0), sFront - c, (u1, y0), (u0, y0), sG, -up);
                Band((u0, y1), (u1, y1), sFront - c, (u1, y1), (u0, y1), sG, up);
            }
        }
    }

    private static readonly Face SimpleMetal = new(MaterialKind.Metal, Families.FrameAlu, VertexFlags.NoShadow);

    /// <summary>L2/L3: the glass alone, just proud of the wall plane (same numbers and cards as near).</summary>
    public void FlatGlass(EdgeFrame f, FacadeLayout layout, Opening o, float s) =>
        Glass(f, layout, o, s, Card(_p, o, Number(_p, _block, layout, o)));

    /// <summary>One window cell over the whole opening: it closes the hollow shell, so it casts shadow.</summary>
    private void Glass(EdgeFrame f, FacadeLayout layout, Opening o, float s, WindowCard card) =>
        _sink.Append(OpeningGlass.Opening(o.U1 - o.U0, o.Y1 - o.Y0, _block.Base, layout.ColumnStart + o.Bay, o.Row, _p.TotalColumns,
            _channel, (byte)card), Frame(f, o, s));

    private Vector2 Uv2() => new(_block.Base, LightChannelCatalog.Slot(_channel) + 32f * _p.TotalColumns);

    /// <summary>
    /// Places a part's surfaces at the building's level (or <paramref name="level"/>); <paramref name="gaskets"/> false
    /// leaves out its gasket surface (OpeningRules.Gaskets), <paramref name="behind"/> the triangles behind the glass.
    /// </summary>
    private void Place(string part, object parameters, in Matrix4x4 at, PartGlass? glass = null, bool gaskets = true,
        LodLevel? level = null, float? behind = null)
    {
        var lod = level ?? _lod;
        if (!_parts.TryGetValue((part, parameters, lod), out var geometry))
        {
            geometry = OpeningGallery.Build(new PartId(part), parameters, lod);
            _parts.Add((part, parameters, lod), geometry);
        }
        foreach (var mesh in geometry.Meshes)
            if (gaskets || mesh.Material.Spec != OpeningStyle.Gasket) _sink.Append(mesh, at, glass, behind);
    }

    private static T Defaults<T>(string part) where T : BoxPartParams =>
        PartCatalog.Find(new PartId(part))?.Defaults as T ?? throw new InvalidOperationException($"No {typeof(T).Name} defaults for {part}.");
}
