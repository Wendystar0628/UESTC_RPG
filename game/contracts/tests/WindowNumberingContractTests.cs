using Campus.Tests;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class WindowNumberingContractTests
{
    private static readonly string Fingerprint = new('a', 64);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExactCapacityKeepsLastIdAndRejectsOneMoreCell()
    {
        WindowAllocation first = new(new("fixture:a"), "grid", 0, 256, 255);
        WindowAllocation last = new(new("fixture:b"), "grid", first.Count, 256, 1);
        var snapshot = Create(first, last);
        AssertThat(snapshot.WindowCount == 65536).OverrideFailureMessage("Exact capacity must be representable.").IsTrue();
        AssertThat(snapshot.Get(last.Feature, last.Key).IdAt(255, 0) == 65535).OverrideFailureMessage("The last campus window ID changed.").IsTrue();
        AssertThrown(() => Create(first, last, new(new("fixture:c"), "grid", 65536, 1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 65537, 1))).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DimensionsAreValidatedBeforeIntMultiplicationCanWrap()
    {
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, int.MaxValue, int.MaxValue))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 0, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, 0))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, -1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, -1))).IsInstanceOf<ArgumentException>();
        var oversized = new WindowAllocation(new("fixture:a"), "grid", 0, int.MaxValue, 2);
        AssertThrown(() => { _ = oversized.Count; }).IsInstanceOf<OverflowException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SnapshotCopiesInputAndDoesNotExposeWritableStorage()
    {
        WindowAllocation original = new(new("fixture:a"), "grid", 0, 3, 2);
        WindowAllocation replacement = new(new("fixture:b"), "other", 0, 1, 1);
        var input = new[] { original };
        var snapshot = new WindowNumberingSnapshot(new("fixture"), Fingerprint, input);
        input[0] = replacement;
        AssertThat(snapshot.WindowCount == 6 && snapshot.Allocations[0] == original).OverrideFailureMessage("Mutating the input changed the snapshot.").IsTrue();
        AssertThat(snapshot.Get(original.Feature, original.Key) == original).OverrideFailureMessage("Lookup did not preserve the copied allocation.").IsTrue();
        AssertThrown(() => snapshot.Get(replacement.Feature, replacement.Key)).IsInstanceOf<KeyNotFoundException>();
        var list = (IList<WindowAllocation>)snapshot.Allocations;
        AssertThrown(() => list[0] = replacement).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => list.Add(replacement)).IsInstanceOf<NotSupportedException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void IdAtUsesRowMajorCoordinatesAndRejectsEveryEdgeOutsideTheGrid()
    {
        var snapshot = Create(new WindowAllocation(new("fixture:a"), "prefix", 0, 5, 1), new(new("fixture:b"), "grid", 5, 7, 3));
        var grid = snapshot.Get(new("fixture:b"), "grid");
        AssertThat(grid.IdAt(0, 0) == 5 && grid.IdAt(1, 0) == 6 && grid.IdAt(0, 1) == 12 && grid.IdAt(6, 2) == 25).OverrideFailureMessage("IdAt must be BaseId + row * Columns + column.").IsTrue();
        AssertThrown(() => grid.IdAt(-1, 0)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => grid.IdAt(7, 0)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => grid.IdAt(0, -1)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => grid.IdAt(0, 3)).IsInstanceOf<ArgumentOutOfRangeException>();
        var invalid = new WindowAllocation(new("fixture:a"), "grid", 0, 0, 1);
        AssertThrown(() => invalid.IdAt(0, 0)).IsInstanceOf<InvalidOperationException>();
        var overflow = new WindowAllocation(new("fixture:a"), "grid", int.MaxValue, 2, 1);
        AssertThrown(() => overflow.IdAt(1, 0)).IsInstanceOf<OverflowException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CompositeKeysAreUniqueAndInputOrderIsNeverSilentlyRepaired()
    {
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 1, 1), new(new("fixture:a"), "grid", 1, 1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:b"), "grid", 0, 1, 1), new(new("fixture:a"), "grid", 1, 1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "z", 0, 1, 1), new(new("fixture:a"), "a", 1, 1, 1))).IsInstanceOf<ArgumentException>();
        var snapshot = Create(new WindowAllocation(new("fixture:a"), "A", 0, 1, 1), new(new("fixture:a"), "a", 1, 1, 1), new(new("fixture:b"), "a", 2, 1, 1));
        AssertThat(snapshot.Get(new("fixture:a"), "A").BaseId == 0 && snapshot.Get(new("fixture:a"), "a").BaseId == 1 &&
            snapshot.Get(new("fixture:b"), "a").BaseId == 2).OverrideFailureMessage("Keys must remain ordinal, case-sensitive and scoped by feature.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RetiredGapsStayUnusedWhileOverlapsAndNegativeBasesFail()
    {
        var gap = new WindowNumberingSnapshot(new("fixture"), Fingerprint,
            [new(new("fixture:a"), "grid", 1, 1, 1)], 4);
        AssertThat(gap.NextId == 4 && gap.ContainsId(1) && !gap.ContainsId(0) && !gap.ContainsId(3)).IsTrue();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", -1, 1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), "grid", 0, 2, 1), new(new("fixture:b"), "grid", 1, 1, 1))).IsInstanceOf<ArgumentException>();
        var separated = Create(new WindowAllocation(new("fixture:a"), "grid", 0, 2, 1), new(new("fixture:b"), "grid", 3, 1, 1));
        AssertThat(separated.WindowCount == 3 && separated.NextId == 4 && !separated.ContainsId(2)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EmptySnapshotIsExplicitAndMissingIdentityRaisesAnError()
    {
        var empty = Create();
        AssertThat(empty.Map == new MapId("fixture") && empty.WindowCount == 0 && empty.Allocations.Count == 0).OverrideFailureMessage("An empty map needs an explicit empty snapshot.").IsTrue();
        AssertThrown(() => empty.Get(new("fixture:missing"), "grid")).IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => new WindowNumberingSnapshot(new(" "), Fingerprint, [])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WindowNumberingSnapshot(new("fixture"), new string('A', 64), [])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WindowNumberingSnapshot(new("fixture"), "short", [])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(default, "grid", 0, 1, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Create(new WindowAllocation(new("fixture:a"), " ", 0, 1, 1))).IsInstanceOf<ArgumentException>();
    }

    private static WindowNumberingSnapshot Create(params WindowAllocation[] allocations) => new(new("fixture"), Fingerprint, allocations);
}
