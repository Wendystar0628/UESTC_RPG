using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Roof;

namespace Campus.Gen.Buildings.Build;

/// <summary>One unit-grille niche on a facade: its span in skin u, its bottom, its storey and the precision zone of its window.</summary>
public readonly record struct GrilleSlot(float U0, float U1, int Row, float Y0, FidelityZone Zone);

/// <summary>
/// The game/gen/parts/roof/ facade fittings of one building at one level, placed by PartCatalog ID: the unit grille (cage, with the
/// outdoor unit inside it where the window's zone is K1 or K2 at L0) and the rain downpipe or condensate drain.
/// A part's +X runs along the facade and its −Z out of the building (OpeningPlacement.Frame), the wall at its back.
/// Slots and drains are decided here once per facade, so the downpipes and the niches never meet (FittingRules).
/// </summary>
public sealed class FacadeFittings(BuildingPlan plan, LodLevel lod, GeometrySink sink)
{
    private static readonly EquipmentGrillePart Cage = new();
    private static readonly AirConditionerPart Unit = new();
    private static readonly DownpipePart Pipe = new();

    // Looked up, never enumerated: a building repeats a few niche and pipe shapes.
    private readonly Dictionary<(string Part, object Params, LodLevel Level), PartGeometry> _parts = [];
    private readonly Dictionary<int, GrilleSlot[]> _slots = [];

    /// <summary>The niches of facade <paramref name="fi"/>: right of each upper-storey window with room, no opening of its storey in the way.</summary>
    public IReadOnlyList<GrilleSlot> Slots(int fi, FacadeLayout layout, PlanRing skin)
    {
        if (!plan.Profile.AcGrilles) return [];
        if (_slots.TryGetValue(fi, out var cached)) return cached;
        var openings = plan.Openings[fi];
        var uEnd = skin.Length[layout.Edge] - FittingRules.GrilleEndMargin;
        var list = new List<GrilleSlot>();
        for (var k = 0; k < openings.Length; k++)
        {
            var o = openings[k];
            if (o.Kind != BayKind.Window || o.Row == 0) continue;
            var u0 = o.U1 + FittingRules.GrilleGap;
            var u1 = u0 + FittingRules.Grille.Width;
            if (u1 > uEnd || openings.Any(p => p.Row == o.Row && p.U0 < u1 + FittingRules.GrilleClear && p.U1 > u0)) continue;
            list.Add(new(u0, u1, o.Row, plan.FloorY(o.Row) + FittingRules.GrilleLift, plan.ZoneOf(fi, k)));
        }
        var slots = list.ToArray();
        _slots.Add(fi, slots);
        return slots;
    }

    /// <summary>
    /// Condensate drains of facade <paramref name="fi"/>: beside every strip of at least FittingRules.DrainFromNiches
    /// niches, as (axis u, top) — the top of the strip's highest niche — unless a window of any storey is in the way.
    /// </summary>
    public IEnumerable<(float U, float Top)> Drains(int fi, FacadeLayout layout, PlanRing skin)
    {
        var openings = plan.Openings[fi];
        var uEnd = skin.Length[layout.Edge] - FittingRules.GrilleEndMargin;
        foreach (var strip in Slots(fi, layout, skin).GroupBy(s => MathF.Round(s.U0 / FittingRules.StripTolerance)))
        {
            if (strip.Count() < FittingRules.DrainFromNiches) continue;
            var u = strip.Max(s => s.U1) + FittingRules.DrainBesideGrille;
            if (u + FittingRules.PipeHalf > uEnd || !Clear(openings, u)) continue;
            yield return (u, strip.Max(s => s.Y0) + FittingRules.Grille.Height);
        }
    }

    /// <summary>The pipe at <paramref name="u"/> (axis) keeps clear of every opening of the facade, sill ears included.</summary>
    public static bool Clear(Opening[] openings, float u) =>
        !openings.Any(o => o.U0 - FittingRules.PipeClear < u + FittingRules.PipeHalf && u - FittingRules.PipeHalf < o.U1 + FittingRules.PipeClear);

    /// <summary>One niche: the cage, and its outdoor unit where the window's zone and the building's level call for it.</summary>
    public void Grille(EdgeFrame f, GrilleSlot slot)
    {
        var size = FittingRules.Grille;
        var withUnit = lod == LodLevel.L0 && slot.Zone <= FittingRules.UnitUpTo;
        var at = OpeningPlacement.Frame(f.At((slot.U0 + slot.U1) * 0.5f, size.Depth * 0.5f, slot.Y0), f.Out);
        Place(Cage, PartCatalog.EquipmentGrille, size, withUnit ? LodLevel.L0 : LodLevel.L1, at);
        if (!withUnit) return;
        var unit = FittingRules.Unit;
        var back = size.Depth * 0.5f - FittingRules.UnitBackGap - unit.Depth * 0.5f;
        Place(Unit, PartCatalog.AirConditioner, unit, LodLevel.L0, Matrix4x4.CreateTranslation(0f, 0f, back) * at);
    }

    /// <summary>
    /// A downpipe with its axis at <paramref name="u"/>, from <paramref name="foot"/> up to <paramref name="top"/>: the rain
    /// pipe with its hopper in <paramref name="colour"/>, or (<paramref name="drain"/>) the hopperless condensate drain.
    /// </summary>
    public void Downpipe(EdgeFrame f, float u, float foot, float top, bool drain, ToonSpec colour)
    {
        var p = Defaults<ColumnParams>(PartCatalog.Downpipe) with { Height = top - foot, Material = colour, Variation = drain ? 1 : 0 };
        Place(Pipe, PartCatalog.Downpipe, p, lod, OpeningPlacement.Frame(f.At(u, p.Depth * 0.5f, foot), f.Out));
    }

    private void Place<T>(IPartDef<T> def, string id, T parameters, LodLevel level, in Matrix4x4 at) where T : BoxPartParams
    {
        if (!_parts.TryGetValue((id, parameters, level), out var geometry))
        {
            geometry = def.Lods[level](parameters);
            _parts.Add((id, parameters, level), geometry);
        }
        foreach (var mesh in geometry.Meshes) sink.Append(mesh, at);
    }

    private static T Defaults<T>(string part) where T : BoxPartParams =>
        PartCatalog.Find(new PartId(part))?.Defaults as T ?? throw new InvalidOperationException($"No {typeof(T).Name} defaults for {part}.");
}
