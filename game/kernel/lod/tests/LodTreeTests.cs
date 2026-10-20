using System.Numerics;
using Campus.Contracts;
using Campus.Kernel.Lod;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class LodTreeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NegativeCoordinatesKeepTheCorrectParent()
    {
        var child = new StreamTile(new(-1, -3), LodLevel.L0);
        if (child.Parent != new StreamTile(new(-1, -2), LodLevel.L1)) throw new InvalidOperationException("Negative tiles crossed the origin.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ParentRemainsUntilTheLastChildIsReady()
    {
        var tree = new LodTree(new(new(0, 0, 0), new(128, 10, 128)), new());
        var required = new HashSet<StreamTile>(); var refined = new HashSet<StreamTile>(); var visible = new HashSet<StreamTile>();
        tree.Select(new(64, 2, 64), 0, 1080, 60, LodLevel.L0, required, refined);
        var ready = required.Where(t => t != new StreamTile(new(1, 1), LodLevel.L0)).ToHashSet();
        tree.ResolveVisible(refined, ready.Contains, visible);
        if (!visible.SetEquals([new StreamTile(new(0, 0), LodLevel.L1)])) throw new InvalidOperationException("Parent left a missing child uncovered.");
        ready.UnionWith(required); tree.ResolveVisible(refined, ready.Contains, visible);
        if (visible.Count != 4 || visible.Any(t => t.Level != LodLevel.L0)) throw new InvalidOperationException("Ready siblings did not replace their parent atomically.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SpeedRecoveryRequiresContinuousDwellBelowThreshold()
    {
        var policy = new LodSpeedPolicy(); policy.Step(46, .1f, false); policy.Step(9, .2f, false);
        if (policy.Minimum != LodLevel.L2) throw new InvalidOperationException("LOD gate recovered too soon.");
        policy.Step(36, .1f, false); policy.Step(9, .2f, false);
        if (policy.Minimum != LodLevel.L2) throw new InvalidOperationException("Interrupted dwell was retained.");
        policy.Step(9, .11f, false);
        if (policy.Minimum != LodLevel.L0) throw new InvalidOperationException("LOD gate did not recover.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoarseRootRemainsValidForTheWholeMap()
    {
        var tree = new LodTree(new(new(-1500, -30, -1500), new(1500, 100, 1500)), new());
        var required = new HashSet<StreamTile>(); var refined = new HashSet<StreamTile>(); var visible = new HashSet<StreamTile>();
        tree.Select(Vector3.Zero, 0, 1080, 60, LodLevel.L0, required, refined);
        tree.ResolveVisible(refined, t => t == StreamTile.Root, visible);
        if (!visible.SetEquals([StreamTile.Root])) throw new InvalidOperationException("Missing fine tiles left a hole.");
    }
}
