using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Banks;

/// <summary>One explicitly registered bank, cut from the compiled terrain shore in its water-left order.</summary>
internal sealed class BankPlan
{
    private readonly ITerrainBankSource _source;
    private readonly double _start;

    private BankPlan(WorldFeature feature, ITerrain terrain, ITerrainBankSource source, TerrainBankSpan span,
        double start, double end)
    {
        Id = feature.Id; Span = span; _source = source; _start = start;
        Shore = PlanLines.Subline(span.Shore.Points, start, end);
        Length = PlanLines.Length(Shore);
        WaterY = terrain.WaterY(new(feature.Text("water")))
            ?? throw new InvalidOperationException($"{Id.Value}: its registered water has no WaterY.");
        var exits = new List<BankExit>();
        if (feature.RequiredProps().TryGetProperty("treads", out var treads))
        {
            if (treads.ValueKind != JsonValueKind.Array) throw new InvalidOperationException($"{Id.Value}: treads must be an array.");
            foreach (var tread in treads.EnumerateArray())
            {
                var authoredCentre = tread.Pair("centre");
                var centre = PlanLines.Project(Shore, authoredCentre);
                var width = tread.Number("width");
                if (width <= 0 || centre.Distance > T.BankEndpointTolerance || centre.Station - width / 2 < 0 || centre.Station + width / 2 > Length)
                    throw new InvalidOperationException($"{Id.Value}: exit centre {authoredCentre} projects to {centre.Point} at {centre.Distance:F3} m from the shore; width {width:F2} m, station {centre.Station:F3}/{Length:F3} m. The exit must fit inside the selected shore.");
                var frame = new PlanFrame(centre.Point, centre.Tangent);
                var top = TopAt(centre.Station);
                var low = WaterY - T.BankSubmersion;
                // Keep both land and submerged tread exact; share the small residual over the ordinary risers.
                var intervals = Math.Max(1, (int)MathF.Round((top - low) / T.BankRiser));
                var riser = (top - low) / intervals;
                var steps = new StairParams(width, (intervals + 1) * riser, (intervals + 1) * T.BankTread, intervals + 1)
                    { Variation = 2, Material = T.BankWallSpec, Foot = T.StoneFoot };
                exits.Add(new(centre.Station, frame, low - riser, steps));
            }
        }
        Exits = exits.OrderBy(exit => exit.Station).ToArray();
    }

    public FeatureId Id { get; }
    public TerrainBankSpan Span { get; }
    public IReadOnlyList<Vector2> Shore { get; }
    public double Length { get; }
    public float WaterY { get; }
    public IReadOnlyList<BankExit> Exits { get; }
    public float TopAt(double station) => _source.BankTopY(Span.Id, _start + station);

    public static IReadOnlyList<BankPlan> All(IWorldModel world)
    {
        var registrations = world.Features.Values.Where(feature => feature.Kind.Value == T.BankKind && feature.OptionalText("form") == T.HardBankForm)
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        if (registrations.Length == 0) return [];
        var source = world.Terrain as ITerrainBankSource
            ?? throw new InvalidOperationException("Registered bank structures require the world's compiled ITerrainBankSource.");
        return registrations.Select(feature =>
        {
            var (a, b) = feature.PointPair();
            var water = new FeatureId(feature.Text("water"));
            var candidates = source.Banks.Where(span => span.Kind == TerrainBankKind.Hard && span.Waters.Contains(water))
                .Select(span => (Span: span, A: PlanLines.Project(span.Shore.Points, a), B: PlanLines.Project(span.Shore.Points, b)))
                .Where(candidate => candidate.A.Distance <= T.BankEndpointTolerance && candidate.B.Distance <= T.BankEndpointTolerance
                    && Math.Min(candidate.A.Station, candidate.B.Station) >= candidate.Span.CoreFrom - T.BankJoinTolerance
                    && Math.Max(candidate.A.Station, candidate.B.Station) <= candidate.Span.CoreTo + T.BankJoinTolerance).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException($"{feature.Id.Value}: both registered endpoints must select one compiled Hard bank core.");
            var selected = candidates[0];
            return new BankPlan(feature, world.Terrain, source, selected.Span,
                Math.Max(selected.Span.CoreFrom, Math.Min(selected.A.Station, selected.B.Station)),
                Math.Min(selected.Span.CoreTo, Math.Max(selected.A.Station, selected.B.Station)));
        }).ToArray();
    }

    /// <summary>Short terrain-following pieces; the exit cuts remove the wall and coping together.</summary>
    public IEnumerable<BankPiece> Pieces()
    {
        var cuts = new SortedSet<double> { 0, Length };
        var station = 0d;
        for (var i = 1; i < Shore.Count; i++)
        {
            var dx = (double)Shore[i].X - Shore[i - 1].X;
            var dz = (double)Shore[i].Y - Shore[i - 1].Y;
            var length = Math.Sqrt(dx * dx + dz * dz);
            var count = Math.Max(1, (int)Math.Ceiling(length / T.BankPieceLength));
            for (var part = 1; part <= count; part++) cuts.Add(station + length * part / count);
            station += length;
        }
        foreach (var exit in Exits)
        {
            cuts.Add(exit.Station - exit.Steps.Width / 2);
            cuts.Add(exit.Station + exit.Steps.Width / 2);
        }
        var ordered = cuts.ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            var from = ordered[i - 1]; var to = ordered[i];
            if (to - from < 0.001 || Exits.Any(exit => Math.Abs((from + to) / 2 - exit.Station) < exit.Steps.Width / 2)) continue;
            yield return new(PlanLines.Sample(Shore, from).Point, PlanLines.Sample(Shore, to).Point, TopAt(from), TopAt(to));
        }
    }
}

internal readonly record struct BankPiece(Vector2 A, Vector2 B, float TopA, float TopB);
internal sealed record BankExit(double Station, PlanFrame Frame, float BaseY, StairParams Steps)
{
    // Local +Z climbs toward land: opposite the source shore's mathematical left (the water side).
    public Matrix4x4 At => Frame.AtReversed(0, BaseY, Steps.Depth / 2);
}
