using System;
using System.Collections.Generic;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class WindowNumberingContractTests
{
    private static readonly string Fingerprint = new('a', 64);

    [TestCase]
    public void ExactCapacityKeepsLastIdAndRejectsOneMoreCell()
    {
        WindowAllocation first = new(new("fixture:a"), "grid", 0, 256, 255);
        WindowAllocation last = new(new("fixture:b"), "grid", first.Count, 256, 1);
        var snapshot = Create(first, last);
        Require(snapshot.WindowCount == 65536, "Exact capacity must be representable.");
        Require(snapshot.Get(last.Feature, last.Key).IdAt(255, 0) == 65535, "The last campus window ID changed.");
        Expect<ArgumentException>(() => Create(first, last, new(new("fixture:c"), "grid", 65536, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 65537, 1)));
    }

    [TestCase]
    public void DimensionsAreValidatedBeforeIntMultiplicationCanWrap()
    {
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, int.MaxValue, int.MaxValue)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 0, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, 0)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, -1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, -1)));
        var oversized = new WindowAllocation(new("fixture:a"), "grid", 0, int.MaxValue, 2);
        Expect<OverflowException>(() => { _ = oversized.Count; });
    }

    [TestCase]
    public void SnapshotCopiesInputAndDoesNotExposeWritableStorage()
    {
        WindowAllocation original = new(new("fixture:a"), "grid", 0, 3, 2);
        WindowAllocation replacement = new(new("fixture:b"), "other", 0, 1, 1);
        var input = new[] { original };
        var snapshot = new WindowNumberingSnapshot(new("fixture"), Fingerprint, input);
        input[0] = replacement;
        Require(snapshot.WindowCount == 6 && snapshot.Allocations[0] == original, "Mutating the input changed the snapshot.");
        Require(snapshot.Get(original.Feature, original.Key) == original, "Lookup did not preserve the copied allocation.");
        Expect<KeyNotFoundException>(() => snapshot.Get(replacement.Feature, replacement.Key));
        var list = (IList<WindowAllocation>)snapshot.Allocations;
        Expect<NotSupportedException>(() => list[0] = replacement);
        Expect<NotSupportedException>(() => list.Add(replacement));
    }

    [TestCase]
    public void IdAtUsesRowMajorCoordinatesAndRejectsEveryEdgeOutsideTheGrid()
    {
        var snapshot = Create(new WindowAllocation(new("fixture:a"), "prefix", 0, 5, 1), new(new("fixture:b"), "grid", 5, 7, 3));
        var grid = snapshot.Get(new("fixture:b"), "grid");
        Require(grid.IdAt(0, 0) == 5 && grid.IdAt(1, 0) == 6 && grid.IdAt(0, 1) == 12 && grid.IdAt(6, 2) == 25,
            "IdAt must be BaseId + row * Columns + column.");
        Expect<ArgumentOutOfRangeException>(() => grid.IdAt(-1, 0));
        Expect<ArgumentOutOfRangeException>(() => grid.IdAt(7, 0));
        Expect<ArgumentOutOfRangeException>(() => grid.IdAt(0, -1));
        Expect<ArgumentOutOfRangeException>(() => grid.IdAt(0, 3));
        var invalid = new WindowAllocation(new("fixture:a"), "grid", 0, 0, 1);
        Expect<InvalidOperationException>(() => invalid.IdAt(0, 0));
        var overflow = new WindowAllocation(new("fixture:a"), "grid", int.MaxValue, 2, 1);
        Expect<OverflowException>(() => overflow.IdAt(1, 0));
    }

    [TestCase]
    public void CompositeKeysAreUniqueAndInputOrderIsNeverSilentlyRepaired()
    {
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, 1), new(new("fixture:a"), "grid", 1, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:b"), "grid", 0, 1, 1), new(new("fixture:a"), "grid", 1, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "z", 0, 1, 1), new(new("fixture:a"), "a", 1, 1, 1)));
        var snapshot = Create(new WindowAllocation(new("fixture:a"), "A", 0, 1, 1), new(new("fixture:a"), "a", 1, 1, 1), new(new("fixture:b"), "a", 2, 1, 1));
        Require(snapshot.Get(new("fixture:a"), "A").BaseId == 0 && snapshot.Get(new("fixture:a"), "a").BaseId == 1 &&
            snapshot.Get(new("fixture:b"), "a").BaseId == 2, "Keys must remain ordinal, case-sensitive and scoped by feature.");
    }

    [TestCase]
    public void GapsOverlapsAndNegativeBasesFailRatherThanRenumber()
    {
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 1, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", -1, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 2, 1), new(new("fixture:b"), "grid", 1, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 2, 1), new(new("fixture:b"), "grid", 3, 1, 1)));
    }

    [TestCase]
    public void EmptySnapshotIsExplicitAndMissingIdentityDoesNotProduceFallbacks()
    {
        var empty = Create();
        Require(empty.Map == new MapId("fixture") && empty.WindowCount == 0 && empty.Allocations.Count == 0, "An empty map needs an explicit empty snapshot.");
        Expect<KeyNotFoundException>(() => empty.Get(new("fixture:missing"), "grid"));
        Expect<ArgumentException>(() => new WindowNumberingSnapshot(new(" "), Fingerprint, []));
        Expect<ArgumentException>(() => new WindowNumberingSnapshot(new("fixture"), new string('A', 64), []));
        Expect<ArgumentException>(() => new WindowNumberingSnapshot(new("fixture"), "short", []));
        Expect<ArgumentException>(() => Create(new WindowAllocation(default, "grid", 0, 1, 1)));
        Expect<ArgumentException>(() => Create(new WindowAllocation(new("fixture:a"), " ", 0, 1, 1)));
    }

    private static WindowNumberingSnapshot Create(params WindowAllocation[] allocations) => new(new("fixture"), Fingerprint, allocations);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
