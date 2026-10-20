using Campus.Gen.Geometry;
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
/// glass from OpeningGlass with the building's window numbers. Reveals and sills stay with the wall (BuildingFacadeMesher).
/// A part's +X runs along the facade (EdgeFrame.Along) and its −Z out of the building.
/// </summary>
public sealed class FacadeOpenings
{
    private readonly BuildingPlan _p;
    private readonly WindowBlock _block;
    private readonly LodLevel _lod;
    private readonly GeometrySink _sink;
    private readonly ChannelId _channel;
    // Looked up, never enumerated: the openings of a building share a few part shapes.
    private readonly Dictionary<(string Part, object Params, LodLevel Level), PartGeometry> _parts = [];

    public FacadeOpenings(BuildingPlan plan, WindowBlock block, LodLevel lod, GeometrySink sink)
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

    /// <summary>Card type of an opening, hashed from its window number (WLD-INTERIORS).</summary>
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
            new SimpleWindowMesher(_sink, f, _lod, sG).Build(o, columns, rows);
            Glass(f, layout, o, sG, card);
            return;
        }
        Place(part, Defaults<WindowParams>(part) with { Width = w, Height = h, Columns = columns, Rows = rows }, Frame(f, o, sG),
            gaskets: OpeningRules.Gaskets, behind: OpeningRules.CullBehindGlass ? OpeningRules.BehindGlass : null);
        var physicalCurtain = false;
        if (_p.Profile.Curtains && _lod <= OpeningRules.CurtainsUpTo && part != PartCatalog.CurtainWall && OpeningRules.Curtained(card) &&
            OpeningPlacement.Curtain(_p.Seed, number, w - 2f * OpeningRules.CurtainSideInset, h - OpeningRules.CurtainHeadInset) is { } curtain)
        {
            Place(PartCatalog.Curtain, curtain,
                Frame(f, o, sG + curtain.Depth * 0.5f + OpeningRules.CurtainGlassGap, (h - curtain.Height) * 0.5f),
                level: _lod > OpeningRules.CurtainDetail ? _lod : OpeningRules.CurtainDetail);
            physicalCurtain = true;
        }
        Glass(f, layout, o, sG, card, physicalCurtain);
    }

    /// <summary>L2/L3: the glass alone, just proud of the wall plane (same numbers and cards as near).</summary>
    public void FlatGlass(EdgeFrame f, FacadeLayout layout, Opening o, float s) =>
        Glass(f, layout, o, s, Card(_p, o, Number(_p, _block, layout, o)));

    /// <summary>One window cell over the whole opening: it closes the hollow shell, so it casts shadow.</summary>
    private void Glass(EdgeFrame f, FacadeLayout layout, Opening o, float s, WindowCard card, bool physicalCurtain = false)
    {
        // WP-10 takes the logical card type. This fresh bundle is locally owned; encode this LOD's
        // actual curtain presence only after creation, without changing the part's input contract.
        var mesh = OpeningGlass.Opening(o.U1 - o.U0, o.Y1 - o.Y0, _block.Base, layout.ColumnStart + o.Bay, o.Row, _p.TotalColumns,
            _channel, (byte)card);
        if (physicalCurtain)
            for (var i = 0; i < mesh.Reserved!.Length; i++) mesh.Reserved[i] |= WindowCardEncoding.PhysicalCurtainFlag;
        _sink.Append(Tinted(mesh), Frame(f, o, s));
    }

    /// <summary>A glass surface in the building's glass colour (FacadeProfile.Glass, a preset's 玻璃色); other surfaces as they are.</summary>
    private GeometryBundle Tinted(GeometryBundle mesh)
    {
        var spec = mesh.Material.Spec;
        var lit = Materials.Lit(_p.Profile.Glass);
        return spec.Kind != ToonKind.Glass || spec.Lit == lit ? mesh
            : mesh with { Material = new ToonRef(spec with { Lit = lit, Shade = Materials.Shade(_p.Profile.Glass) }) };
    }

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
            geometry = OpeningPartFactory.Build(new PartId(part), parameters, lod);
            _parts.Add((part, parameters, lod), geometry);
        }
        foreach (var mesh in geometry.Meshes)
            if (gaskets || mesh.Material.Spec != OpeningStyle.Gasket) _sink.Append(Tinted(mesh), at, glass, behind);
    }

    private static T Defaults<T>(string part) where T : BoxPartParams =>
        PartCatalog.Find(new PartId(part))?.Defaults as T ?? throw new InvalidOperationException($"No {typeof(T).Name} defaults for {part}.");
}
