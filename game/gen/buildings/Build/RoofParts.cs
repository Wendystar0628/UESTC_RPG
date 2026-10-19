using Campus.Gen.Parts.Core;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Parts.Roof;

namespace Campus.Gen.Buildings.Build;

/// <summary>
/// The WP-12 roof parts of one building at one level, placed by PartCatalog ID (MAP-QSH-ADDITIONS): coping and
/// lightning strip swept along the parapet centreline, and the plant enclosure, a closed equipment-fence ring around a
/// row of heat pumps and a water tank, plus a mast on some tall roofs. Density follows the roof's precision zone
/// (RoofRules): full roofs (K1, K2) at the building's level, the rest with fewer units one part level coarser near.
/// A unit's +X runs along the building axis; everything stands on the roof slab and is visual only (roofs cannot be
/// reached, MAP-QSH-ADDITIONS).
/// </summary>
public sealed class RoofParts(BuildingPlan plan, LodLevel lod, MeshSink sink)
{
    private static readonly WaterTankPart Tank = new();
    private static readonly HeatPumpPart HeatPump = new();
    private static readonly AntennaPart Antenna = new();
    private static readonly EquipmentFencePart Fence = new();

    // Looked up, never enumerated: a building repeats a few unit shapes.
    private readonly Dictionary<(string Part, object Params, LodLevel Level), PartGeometry> _parts = [];

    /// <summary>The roof carries the full set (K1, K2).</summary>
    public bool Full => plan.RoofZone <= RoofRules.FullEquipmentUpTo;

    /// <summary>
    /// Coping on every parapet ring, from the parapet's outer face <paramref name="sOuter"/> to its inner face
    /// <paramref name="sInner"/> (skin offsets) plus a lip each side, between <paramref name="y0"/> and <paramref name="top"/>;
    /// coloured as <paramref name="trim"/>. Returns false, having emitted nothing, when a ring cannot carry the part's
    /// section (an edge too short for its mitres); the caller then keeps its own coping profile.
    /// </summary>
    public bool Coping(float sOuter, float sInner, float y0, float top, ToonSpec trim)
    {
        var p = Defaults<TrimParams>(PartCatalog.ParapetCap) with
        {
            Height = top - y0,
            Depth = sOuter - sInner + 2f * RoofRules.CopingLip,
            Material = trim,
        };
        var level = lod < LodLevel.L3 ? lod : LodLevel.L3;
        var pieces = new List<PartGeometry>();
        foreach (var path in Centrelines((sOuter + sInner) * 0.5f))
        {
            if (path is null) return false;
            try { pieces.Add(ParapetCapPart.Along(path, true, p, level)); }
            catch (ArgumentException) { return false; }
        }
        var at = Matrix4x4.CreateTranslation(0f, y0, 0f);
        foreach (var piece in pieces)
            foreach (var mesh in piece.Meshes) sink.Append(mesh, at);
        return true;
    }

    /// <summary>
    /// One continuous galvanised rod on the coping (<paramref name="top"/>) along the parapet centreline; supports every
    /// metre only on K1 roofs, simple roofs take the part's far level (no supports) and stop after L0.
    /// </summary>
    public void LightningStrip(float sOuter, float sInner, float top)
    {
        if (lod > (Full ? RoofRules.StripUpToFull : RoofRules.StripUpToSimple)) return;
        var p = Defaults<TrimParams>(PartCatalog.LightningStrip) with { Variation = plan.RoofZone <= RoofRules.StripSupportsUpTo ? 1 : 0 };
        var level = Full ? lod : LodLevel.L1;
        var at = Matrix4x4.CreateTranslation(0f, top, 0f);
        foreach (var path in Centrelines((sOuter + sInner) * 0.5f))
        {
            if (path is null) continue;
            PartGeometry strip;
            try { strip = LightningStripPart.Along(path, true, p, level); }
            catch (ArgumentException) { continue; }
            foreach (var mesh in strip.Meshes) sink.Append(mesh, at);
        }
    }

    /// <summary>Half size (along the axis, across it) of this roof's plant enclosure, or null when it has none.</summary>
    public Vector2? EnclosureHalf()
    {
        var (tank, pumps) = RoofRules.Plant(plan.Style, Full);
        if (pumps == 0 && !tank) return null;
        var units = Units(tank, pumps);
        var along = Row(units) + 2f * RoofRules.FenceClearance;
        var across = MathF.Max(RoofRules.MinEnclosureDepth, units.Max(u => u.Params.Depth) + 2f * RoofRules.FenceClearance);
        return new Vector2(along * 0.5f, across * 0.5f);
    }

    /// <summary>
    /// The plant enclosure on <paramref name="ring"/> (its fence line, counter-clockwise) centred at <paramref name="centre"/>:
    /// the fence and its units in a row along the building axis, standing at <paramref name="baseY"/>.
    /// </summary>
    public void Enclosure(PlanRing ring, Vector2 centre, float baseY)
    {
        if (Level(Fence) is { } fenceLevel)
        {
            var at = Matrix4x4.CreateTranslation(0f, baseY, 0f);
            var fence = Defaults<RailingParams>(PartCatalog.EquipmentFence);
            foreach (var mesh in EquipmentFencePart.Along(ring.Points, true, fence, fenceLevel).Meshes)
                sink.Append(FenceBoards(mesh), at);
            if (fenceLevel == LodLevel.L0) FenceDoor(ring, baseY, fence);
        }
        var (tank, pumps) = RoofRules.Plant(plan.Style, Full);
        var units = Units(tank, pumps);
        var axis = plan.Axis;
        var a = -Row(units) * 0.5f;
        foreach (var (isTank, p) in units)
        {
            var at = Frame(axis, centre + axis * (a + p.Width * 0.5f), baseY);
            a += p.Width + RoofRules.UnitGap;
            var geometry = isTank ? Part(Tank, PartCatalog.WaterTank, p) : Part(HeatPump, PartCatalog.HeatPump, p);
            if (geometry is not null) Place(geometry, at);
        }
    }

    private static readonly Face FrameFace = new(MaterialKind.Metal, Families.FrameAlu, VertexFlags.NoShadow);
    private static readonly Face LeafFace = new(MaterialKind.Trim, RoofRules.DoorLeaf);

    /// <summary>
    /// The access door on the longest run of the fence ring, on the bay the WP-12 fence part would give an open run (middle
    /// one): frame, leaf and handle stand proud of the boards between that bay's posts, so nothing crosses the fence itself.
    /// The posts and bays are the part's (round(length / PostSpacing) bays from the run's first corner); a bay too narrow
    /// for a door after its posts gets none.
    /// </summary>
    private void FenceDoor(PlanRing ring, float baseY, RailingParams fence)
    {
        var run = Array.IndexOf(ring.Length, ring.Length.Max());
        var count = Math.Max(1, (int)Math.Round(ring.Length[run] / Math.Max(fence.PostSpacing, 0.3f)));
        var bay = ring.Length[run] / count;
        var width = MathF.Min(RoofStyle.FenceDoorWidth, bay - RoofStyle.FencePost - 2f * (RoofRules.DoorJamb + RoofRules.DoorSideClear));
        if (width < RoofRules.DoorMinWidth) return;
        var f = EdgeFrame.Of(ring, run);
        var centre = bay * (count / 2 + 0.5f);
        float u0 = centre - width * 0.5f, u1 = centre + width * 0.5f, jamb = RoofRules.DoorJamb;
        var board = fence.Depth * 0.5f;
        float sFrame = board + RoofRules.DoorFrameProud, sLeaf = board + RoofRules.DoorLeafProud;
        var cap = MathF.Min(RoofStyle.FenceCap, fence.Height * 0.1f);
        var headTop = baseY + fence.Height - cap - RoofRules.DoorHeadGap;
        var headBottom = headTop - RoofRules.DoorHead;
        f.Box(sink, FrameFace, u0 - jamb, u0, 0f, sFrame, baseY, headTop, bottom: false);
        f.Box(sink, FrameFace, u1, u1 + jamb, 0f, sFrame, baseY, headTop, bottom: false);
        f.Box(sink, FrameFace, u0, u1, 0f, sFrame, headBottom, headTop);
        f.Box(sink, LeafFace, u0, u1, 0f, sLeaf, baseY + RoofRules.DoorSill, headBottom, bottom: false);
        var h0 = u1 - RoofRules.HandleFromJamb - RoofRules.HandleLength;
        var y0 = baseY + RoofRules.HandleY;
        f.Box(sink, FrameFace, h0, h0 + RoofRules.HandleLength, sLeaf - 0.004f, sLeaf + RoofRules.HandleProud, y0, y0 + RoofRules.HandleHeight);
    }

    /// <summary>A communication mast (WP-12 antenna, mast variant) standing at <paramref name="spot"/>.</summary>
    public void Mast(Vector2 spot, float baseY)
    {
        if (lod == LodLevel.L4) return;
        var p = Defaults<EquipmentParams>(PartCatalog.Antenna) with
        {
            Width = RoofRules.MastBase,
            Height = RoofRules.MastHeight,
            Depth = RoofRules.MastBase,
            Variation = 1,
        };
        if (Part(Antenna, PartCatalog.Antenna, p) is { } mast) Place(mast, Frame(plan.Axis, spot, baseY));
    }

    /// <summary>The fence's board surface in RoofRules.FenceBoard instead of the part's fence steel; posts and cap rail unchanged.</summary>
    private static GeometryBundle FenceBoards(GeometryBundle mesh) =>
        mesh.Material.Spec.Lit == PartMaterials.Lit(RoofStyle.FenceSteel)
            ? mesh with { Material = new ToonRef(mesh.Material.Spec with { Lit = PartMaterials.Lit(RoofRules.FenceBoard), Shade = PartMaterials.Shade(RoofRules.FenceBoard) }) }
            : mesh;

    /// <summary>The units of an enclosure, tank first (its ladder faces the heat pumps), at their PartCatalog sizes.</summary>
    private static List<(bool Tank, EquipmentParams Params)> Units(bool tank, int pumps)
    {
        var list = new List<(bool, EquipmentParams)>();
        if (tank) list.Add((true, Defaults<EquipmentParams>(PartCatalog.WaterTank)));
        var h = Defaults<EquipmentParams>(PartCatalog.HeatPump);
        for (var i = 0; i < pumps; i++) list.Add((false, h));
        return list;
    }

    /// <summary>Length of a row of units along the axis, gaps included.</summary>
    private static float Row(List<(bool Tank, EquipmentParams Params)> units) =>
        units.Sum(u => u.Params.Width) + RoofRules.UnitGap * (units.Count - 1);

    /// <summary>Each skin ring offset to the parapet centreline <paramref name="s"/>, in ring order (null when it flips).</summary>
    private IEnumerable<Vector2[]?> Centrelines(float s)
    {
        foreach (var ring in plan.SkinRings) yield return ring.Offset(s);
    }

    /// <summary>Row-vector placement: part +X along <paramref name="axis"/>, +Y up, origin at <paramref name="at"/> on <paramref name="y"/>.</summary>
    private static Matrix4x4 Frame(Vector2 axis, Vector2 at, float y) =>
        new(axis.X, 0f, axis.Y, 0f, 0f, 1f, 0f, 0f, -axis.Y, 0f, axis.X, 0f, at.X, y, at.Y, 1f);

    /// <summary>The part level of a unit: the building's, one coarser near on simple roofs; null when the part has none.</summary>
    private LodLevel? Level<T>(IPartDef<T> def) where T : BoxPartParams
    {
        if (lod == LodLevel.L4) return null;
        var level = !Full && lod == LodLevel.L0 ? LodLevel.L1 : lod;
        return def.Lods.ContainsKey(level) ? level : null;
    }

    private PartGeometry? Part<T>(IPartDef<T> def, string id, T parameters) where T : BoxPartParams
    {
        if (Level(def) is not { } level) return null;
        if (!_parts.TryGetValue((id, parameters, level), out var geometry))
        {
            geometry = def.Lods[level](parameters);
            _parts.Add((id, parameters, level), geometry);
        }
        return geometry;
    }

    private void Place(PartGeometry geometry, in Matrix4x4 at)
    {
        foreach (var mesh in geometry.Meshes) sink.Append(mesh, at);
    }

    private static T Defaults<T>(string part) where T : BoxPartParams =>
        PartCatalog.Find(new PartId(part))?.Defaults as T ?? throw new InvalidOperationException($"No {typeof(T).Name} defaults for {part}.");
}
