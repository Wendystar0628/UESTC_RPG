using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainBankSpanTests
{
    [TestCase]
    public void SnapshotOwnsShoreAndProvenanceWithoutChangingWaterLeftOrder()
    {
        var waters = new List<FeatureId> { new("water:b"), new("water:a"), new("water:b") };
        var roads = new List<FeatureId> { new("road:b"), new("road:a") };
        var bridges = new List<FeatureId> { new("bridge:a") };
        // This cut crosses a source ring's seam without closing the exported interval.
        var points = new List<Vector2> { new(0, 1), new(0, 0), new(4, 0) };
        var span = new TerrainBankSpan("bank:a", waters, roads, bridges, new(points), 1, 4, 1, TerrainBankKind.Hard);
        waters.Clear(); roads[0] = new("road:replacement"); bridges.Clear(); points[0] = new(99, 99); points.Clear();

        Require(span.Waters.SequenceEqual(new FeatureId[] { new("water:a"), new("water:b") }) &&
            span.Roads.SequenceEqual(new FeatureId[] { new("road:a"), new("road:b") }) &&
            span.Bridges.SequenceEqual(new FeatureId[] { new("bridge:a") }), "Provenance must be a frozen ordinal set, including all roads.");
        Require(span.Shore.Points.SequenceEqual(new Vector2[] { new(0, 1), new(0, 0), new(4, 0) }) && span.Length == 5,
            "Publishing a shore must preserve its supplied water-left direction and seam-crossing point order.");
        Expect<NotSupportedException>(() => ((IList<Vector2>)span.Shore.Points)[0] = Vector2.One);
        Expect<NotSupportedException>(() => ((IList<FeatureId>)span.Roads)[0] = new("road:replacement"));
    }

    [TestCase]
    public void LengthAccumulatesFinalFloatPointsInDouble()
    {
        // Every coordinate is representable as float; float accumulation would round 30,000,003 to 30,000,004.
        var span = new TerrainBankSpan("bank:long", [new("water:a")], [], [],
            new Polyline([new(0, 0), new(30_000_000, 0), new(30_000_000, 3)]),
            2, 30_000_001d, 2, TerrainBankKind.Hard);
        Require(span.Length == 30_000_003d && span.Length - span.CoreTo == 2,
            "Length and endpoint stations must use double accumulation on the final exported polyline.");
    }

    [TestCase]
    public void CoreTransitionsAndWholeHardRingHaveExplicitBoundaries()
    {
        var open = Open();
        Require(open.CoreFrom == 2 && open.CoreTo == 12 && open.Length == 14, "Exact transition capacity must be accepted.");
        var noTransition = Open(0, 14, 0);
        Require(noTransition.CoreFrom == 0 && noTransition.CoreTo == noTransition.Length && noTransition.TransitionLength == 0,
            "An authored or clipped open Hard segment must not invent a Soft transition.");
        foreach (var (from, to, transition) in new[]
        {
            (-1d, 12d, 2d), (12d, 12d, 2d), (2d, 15d, 2d),
            (1d, 12d, 2d), (2d, 13d, 2d),
            (2d, 12d, -1d), (double.NaN, 12d, 2d),
            (2d, double.PositiveInfinity, 2d), (2d, 12d, double.PositiveInfinity)
        }) Expect<ArgumentException>(() => Open(from, to, transition));

        Polyline ring = new([new(0, 0), new(4, 0), new(4, 3), new(0, 3), new(0, 0)]);
        var whole = new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 0, 14, 0, TerrainBankKind.Hard);
        Require(whole.Length == 14 && whole.Shore.Points[0] == whole.Shore.Points[^1], "A complete Hard ring retains its closing endpoint.");
        Expect<ArgumentException>(() => new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 0, 14, 0, TerrainBankKind.Soft));
        Expect<ArgumentException>(() => new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 2, 12, 2, TerrainBankKind.Hard));
    }

    [TestCase]
    public void InvalidIdentityKindAndShoreAreRejectedAtPublication()
    {
        foreach (var id in new[] { "", " ", " bank:a", "bank:a ", "bank:\0a" })
            Expect<ArgumentException>(() => Open(id: id));
        Expect<ArgumentException>(() => Open(kind: (TerrainBankKind)255));
        Expect<ArgumentException>(() => Open(waters: []));
        Expect<ArgumentException>(() => Open(waters: [default]));
        Expect<ArgumentException>(() => new TerrainBankSpan("bank:a", [new("water:a")], [new(" road:a")], [], OpenShore(), 2, 12, 2, TerrainBankKind.Hard));
        Expect<ArgumentException>(() => new TerrainBankSpan("bank:a", [new("water:a")], [], [new("\tbridge:a")], OpenShore(), 2, 12, 2, TerrainBankKind.Hard));
        foreach (var shore in new[]
        {
            new Polyline([]), new Polyline([Vector2.Zero]),
            new Polyline([Vector2.Zero, Vector2.Zero, new(14, 0)]),
            new Polyline([Vector2.Zero, new(float.NaN, 0)]),
            new Polyline([Vector2.Zero, new(14, float.PositiveInfinity)]),
            new Polyline([Vector2.Zero, new(7, 0), Vector2.Zero])
        }) Expect<ArgumentException>(() => new TerrainBankSpan("bank:a", [new("water:a")], [], [], shore, 2, 12, 2, TerrainBankKind.Hard));
    }

    private static Polyline OpenShore() => new([new(-10, 0), new(4, 0)]);
    private static TerrainBankSpan Open(double from = 2, double to = 12, double transition = 2,
        string id = "bank:a", TerrainBankKind kind = TerrainBankKind.Hard, IReadOnlyList<FeatureId>? waters = null) =>
        new(id, waters ?? [new("water:a")], [], [], OpenShore(), from, to, transition, kind);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
