using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Metre frame of one paved area: <see cref="U"/> runs along the entrance façade, <see cref="V"/> along the entrance
/// axis (outward). The origin lies on the axis where it enters the plaza, so the joint at U = 0 is the axis centre line
/// and the first row starts at the plaza edge. Direction and phase live only in the UVs, never in material identity.
/// </summary>
internal sealed record PavingFrame(byte Profile, Vector2 Origin, Vector2 U, Vector2 V)
{
    public Vector2 Uv(Vector2 p)
    {
        var d = p - Origin;
        return new(Vector2.Dot(d, U), Vector2.Dot(d, V));
    }
}

/// <summary>
/// Surface patterns of the ground (GAME-015, ART-STYLE). Plaza family: the forecourt of a library's open entrance is
/// laid in 600×600 mm granite, stack bond, square to that entrance's façade, one joint on the entrance axis.
/// Walk family: every footway, path and bridge footprint is laid in 300×150 mm pavers, running bond, courses along
/// the walk's centre line (<see cref="WalkFrameAt"/>).
/// <para>
/// Selection reads data characteristics only (DECISION-168 mainline, no entity or map IDs in code): buildings whose category is
/// a library; their open entrances from the interiors index; the first plaza source met by each entrance's outward axis
/// within <see cref="Reach"/>. Several entrances reaching the same plaza: the axis with the longest run across it is the
/// plaza's main axis. The binding belongs to the source feature, so every shape read from it (its area and its network
/// line) shares one frame. Curbs, vertical faces and plaza borders never take a pattern. A point where a bound source
/// overlaps an unbound plaza (the time plaza laid inside the forecourt) keeps the forecourt grid: one continuous granite
/// field, no jagged material seam along the triangulation.
/// </para>
/// Precision zone: read from the exported fidelity data (<see cref="IFidelityModel.GroundAt"/>); K4 and K5 switch the
/// pattern off. The published world always carries its build-time classification.
/// </summary>
internal sealed class GroundPaving
{
    /// <summary>Profile 0: no pattern.</summary>
    public const byte None = 0;
    public const byte GraniteProfile = 1;
    public const byte PaverProfile = 2;

    public const string GraniteMaterialId = "ground.plaza.granite";
    public const string PaverMaterialId = "ground.walk.paver";

    /// <summary>Building categories whose entrance forecourt is laid in granite (data vocabulary, not identities).</summary>
    private const string LibraryCategory = "library";
    /// <summary>Furthest the forecourt edge may be from the door along the axis (stairs and landings lie between).</summary>
    public const float Reach = 40f;
    private const float Step = 0.25f;
    private const float MaxRun = 600f;

    private readonly Dictionary<FeatureId, PavingFrame> _frames;
    /// <summary>Walk shapes (by reference: one feature can give several) and their per-segment frames.</summary>
    private readonly Dictionary<GroundShape, PavingFrame[]> _walks;
    private readonly IFidelityModel? _fidelity;

    public static GroundPaving Empty { get; } = new([], new(ReferenceEqualityComparer.Instance), null);

    private GroundPaving(Dictionary<FeatureId, PavingFrame> frames, Dictionary<GroundShape, PavingFrame[]> walks,
        IFidelityModel? fidelity)
    {
        _frames = frames;
        _walks = walks;
        _fidelity = fidelity;
    }

    public bool HasPlazas => _frames.Count > 0;
    public bool HasWalks => _walks.Count > 0;

    public static string MaterialId(byte profile) => profile switch
    {
        GraniteProfile => GraniteMaterialId,
        PaverProfile => PaverMaterialId,
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown ground paving profile.")
    };

    public PavingFrame? FrameOf(GroundShape shape) => _frames.GetValueOrDefault(shape.Id);

    /// <summary>
    /// Paver frame of a walk at a point. A centre-line walk has one frame per segment: U is the arc length from the
    /// line's start, V the signed offset from the line, so course joints run parallel to the walk and stay continuous
    /// round a bend. The segment is chosen like a mitre (nearest segment; outside a bend, the bisector), so only the
    /// cross joints change there. An area walk has one frame on its longest outer edge.
    /// </summary>
    public PavingFrame? WalkFrameAt(GroundShape shape, Vector2 p)
    {
        if (!_walks.TryGetValue(shape, out var frames)) return null;
        if (frames.Length == 1) return frames[0];
        var line = shape.DirectionPath!;
        var best = 0;
        float bestDistance = float.MaxValue, bestExcess = float.MaxValue;
        for (var i = 0; i < frames.Length; i++)
        {
            var a = line[i];
            var d = frames[i].U;
            var length = Vector2.Distance(a, line[i + 1]);
            var t = Vector2.Dot(p - a, d);
            var distance = Vector2.Distance(p, a + d * Math.Clamp(t, 0f, length));
            var excess = MathF.Max(MathF.Max(-t, t - length), 0f);
            // Outside a bend both segments are nearest at the shared corner: the smaller overshoot is the mitre side.
            if (distance < bestDistance - 1e-3f || (distance <= bestDistance + 1e-3f && excess < bestExcess))
            {
                best = i;
                bestDistance = MathF.Min(bestDistance, distance);
                bestExcess = excess;
            }
        }
        return frames[best];
    }

    /// <summary>UV2 of a patterned face: (gate, K). Gate 1 only in K1–K3; K4/K5 keep the averaged plain face.</summary>
    public (float Gate, float Zone) GateAt(Vector2 p)
    {
        if (_fidelity is null) throw new InvalidOperationException("Empty paving has no fidelity model.");
        var zone = _fidelity.GroundAt(p.X, p.Y);
        var k = (byte)zone;
        if (k is < 1 or > 5) throw new InvalidDataException($"Fidelity zone {k} at ({p.X}, {p.Y}) is outside K1–K5.");
        return (k <= 3 ? 1f : 0f, k);
    }

    public static GroundPaving Build(IWorldModel world, IReadOnlyList<GroundShape> shapes)
    {
        ArgumentNullException.ThrowIfNull(world);
        var plazas = shapes.Where(shape => shape.Kind == SurfaceKind.Plaza && shape.Rings is not null).ToArray();
        var best = new Dictionary<FeatureId, (float Run, PavingFrame Frame)>();
        if (plazas.Length > 0)
            foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            {
                if (feature.Kind.Value != "building" || WorldProps.TryText(feature.Props, "category") != LibraryCategory) continue;
                if (world.Interiors.Get(feature.Id) is not { } entry) continue;
                foreach (var entrance in entry.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal))
                {
                    if (Forecourt(plazas, entrance) is not { } hit) continue;
                    // Several entrances on one plaza: the longest axis run is the main axis; ties keep the first (ordinal) one.
                    if (!best.TryGetValue(hit.Shape.Id, out var current) || hit.Run > current.Run)
                        best[hit.Shape.Id] = (hit.Run, hit.Frame);
                }
            }
        var frames = new Dictionary<FeatureId, PavingFrame>();
        foreach (var pair in best) frames.Add(pair.Key, pair.Value.Frame);
        var walks = new Dictionary<GroundShape, PavingFrame[]>(ReferenceEqualityComparer.Instance);
        foreach (var shape in shapes)
            if (shape.Kind == SurfaceKind.Walk && WalkFrames(shape) is { } walk) walks.Add(shape, walk);
        return new(frames, walks, world.Fidelity);
    }

    private static PavingFrame[]? WalkFrames(GroundShape shape)
    {
        if (shape.DirectionPath is { Count: >= 2 } line)
        {
            var result = new PavingFrame[line.Count - 1];
            var s = 0f;
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i];
                var length = Vector2.Distance(a, line[i + 1]);
                if (!(length > 1e-4f)) return null;
                var d = (line[i + 1] - a) / length;
                result[i] = new PavingFrame(PaverProfile, a - d * s, d, new(-d.Y, d.X));
                s += length;
            }
            return result;
        }
        if (shape.Rings is not [var outer, ..] || outer.Length < 3) return null;
        var (start, dir, longest) = (outer[0], Vector2.UnitX, 0f);
        for (var i = 0; i < outer.Length; i++)
        {
            var edge = outer[(i + 1) % outer.Length] - outer[i];
            if (edge.Length() > longest) (start, dir, longest) = (outer[i], Vector2.Normalize(edge), edge.Length());
        }
        return longest > 1e-4f ? [new PavingFrame(PaverProfile, start, dir, new(-dir.Y, dir.X))] : null;
    }

    /// <summary>First plaza on the entrance's outward axis, the axis entry point and the run across that plaza.</summary>
    private static (GroundShape Shape, float Run, PavingFrame Frame)? Forecourt(GroundShape[] plazas, EntranceDecl entrance)
    {
        // Yaw 0 = north (−Z), 90 = east (+X) (WLD-INTERIORS).
        var axis = Angles.Heading(entrance.YawDeg);
        if (!VectorMath.Finite(axis) || axis.LengthSquared() < 0.5f) return null;
        axis = Vector2.Normalize(axis);
        var door = new Vector2(entrance.Center.X, entrance.Center.Z);
        for (var t = 0f; t <= Reach; t += Step)
        {
            var p = door + axis * t;
            foreach (var shape in plazas)
            {
                if (!Inside(shape, p)) continue;
                // Bisect the entry to a millimetre between the last outside sample and this one.
                float lo = MathF.Max(0f, t - Step), hi = t;
                if (Inside(shape, door + axis * lo)) hi = lo;
                else
                    for (var i = 0; i < 16; i++)
                    {
                        var mid = 0.5f * (lo + hi);
                        if (Inside(shape, door + axis * mid)) hi = mid; else lo = mid;
                    }
                var entry = door + axis * hi;
                var run = 0f;
                while (run < MaxRun && Inside(shape, entry + axis * (run + Step))) run += Step;
                var across = new Vector2(-axis.Y, axis.X);
                return (shape, run, new PavingFrame(GraniteProfile, entry, across, axis));
            }
        }
        return null;
    }

    private static bool Inside(GroundShape shape, Vector2 p) =>
        p.X >= shape.Min.X && p.X <= shape.Max.X && p.Y >= shape.Min.Y && p.Y <= shape.Max.Y && Sdf.Shape(shape, p, 1f) <= 0f;
}
