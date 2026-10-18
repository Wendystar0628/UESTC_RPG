using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GdUnit4;
using static GdUnit4.Assertions;

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

        AssertThat(span.Waters.SequenceEqual(new FeatureId[] { new("water:a"), new("water:b") }) &&
            span.Roads.SequenceEqual(new FeatureId[] { new("road:a"), new("road:b") }) &&
            span.Bridges.SequenceEqual(new FeatureId[] { new("bridge:a") })).OverrideFailureMessage("Provenance must be a frozen ordinal set, including all roads.").IsTrue();
        AssertThat(span.Shore.Points.SequenceEqual(new Vector2[] { new(0, 1), new(0, 0), new(4, 0) }) && span.Length == 5).OverrideFailureMessage("Publishing a shore must preserve its supplied water-left direction and seam-crossing point order.").IsTrue();
        AssertThrown(() => ((IList<Vector2>)span.Shore.Points)[0] = Vector2.One).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => ((IList<FeatureId>)span.Roads)[0] = new("road:replacement")).IsInstanceOf<NotSupportedException>();
    }

    [TestCase]
    public void LengthAccumulatesFinalFloatPointsInDouble()
    {
        // Every coordinate is representable as float; float accumulation would round 30,000,003 to 30,000,004.
        var span = new TerrainBankSpan("bank:long", [new("water:a")], [], [],
            new Polyline([new(0, 0), new(30_000_000, 0), new(30_000_000, 3)]),
            2, 30_000_001d, 2, TerrainBankKind.Hard);
        AssertThat(span.Length == 30_000_003d && span.Length - span.CoreTo == 2).OverrideFailureMessage("Length and endpoint stations must use double accumulation on the final exported polyline.").IsTrue();
    }

    [TestCase]
    public void CoreTransitionsAndWholeHardRingHaveExplicitBoundaries()
    {
        var open = Open();
        AssertThat(open.CoreFrom == 2 && open.CoreTo == 12 && open.Length == 14).OverrideFailureMessage("Exact transition capacity must be accepted.").IsTrue();
        var noTransition = Open(0, 14, 0);
        AssertThat(noTransition.CoreFrom == 0 && noTransition.CoreTo == noTransition.Length && noTransition.TransitionLength == 0).OverrideFailureMessage("An authored or clipped open Hard segment must not invent a Soft transition.").IsTrue();
        foreach (var (from, to, transition) in new[]
        {
            (-1d, 12d, 2d), (12d, 12d, 2d), (2d, 15d, 2d),
            (1d, 12d, 2d), (2d, 13d, 2d),
            (2d, 12d, -1d), (double.NaN, 12d, 2d),
            (2d, double.PositiveInfinity, 2d), (2d, 12d, double.PositiveInfinity)
        }) AssertThrown(() => Open(from, to, transition)).IsInstanceOf<ArgumentException>();

        Polyline ring = new([new(0, 0), new(4, 0), new(4, 3), new(0, 3), new(0, 0)]);
        var whole = new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 0, 14, 0, TerrainBankKind.Hard);
        AssertThat(whole.Length == 14 && whole.Shore.Points[0] == whole.Shore.Points[^1]).OverrideFailureMessage("A complete Hard ring retains its closing endpoint.").IsTrue();
        AssertThrown(() => new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 0, 14, 0, TerrainBankKind.Soft)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new TerrainBankSpan("bank:ring", [new("water:a")], [], [], ring, 2, 12, 2, TerrainBankKind.Hard)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void InvalidIdentityKindAndShoreAreRejectedAtPublication()
    {
        foreach (var id in new[] { "", " ", " bank:a", "bank:a ", "bank:\0a" })
            AssertThrown(() => Open(id: id)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Open(kind: (TerrainBankKind)255)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Open(waters: [])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Open(waters: [default])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new TerrainBankSpan("bank:a", [new("water:a")], [new(" road:a")], [], OpenShore(), 2, 12, 2, TerrainBankKind.Hard)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new TerrainBankSpan("bank:a", [new("water:a")], [], [new("\tbridge:a")], OpenShore(), 2, 12, 2, TerrainBankKind.Hard)).IsInstanceOf<ArgumentException>();
        foreach (var shore in new[]
        {
            new Polyline([]), new Polyline([Vector2.Zero]),
            new Polyline([Vector2.Zero, Vector2.Zero, new(14, 0)]),
            new Polyline([Vector2.Zero, new(float.NaN, 0)]),
            new Polyline([Vector2.Zero, new(14, float.PositiveInfinity)]),
            new Polyline([Vector2.Zero, new(7, 0), Vector2.Zero])
        }) AssertThrown(() => new TerrainBankSpan("bank:a", [new("water:a")], [], [], shore, 2, 12, 2, TerrainBankKind.Hard)).IsInstanceOf<ArgumentException>();
    }

    private static Polyline OpenShore() => new([new(-10, 0), new(4, 0)]);
    private static TerrainBankSpan Open(double from = 2, double to = 12, double transition = 2,
        string id = "bank:a", TerrainBankKind kind = TerrainBankKind.Hard, IReadOnlyList<FeatureId>? waters = null) =>
        new(id, waters ?? [new("water:a")], [], [], OpenShore(), from, to, transition, kind);
}
