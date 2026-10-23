using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class DerivedMemoContractTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void EachWorldEvaluatesItsDerivedFactoryOnce()
    {
        var world = new FakeWorld();
        var key = new object();
        var calls = 0;
        var values = new int[2];
        Parallel.For(0, values.Length, i => values[i] = DerivedMemo.ForWorld(world, key, () => Interlocked.Increment(ref calls)));
        AssertThat(calls == 1 && values.All(value => value == calls)).OverrideFailureMessage("one world evaluates a derived factory once").IsTrue();
        var other = DerivedMemo.ForWorld(new FakeWorld(world), key, () => Interlocked.Increment(ref calls));
        AssertThat(calls == 2 && other == calls).OverrideFailureMessage("another world evaluates its own derived value").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ContextsUsingTheSameWorldKeepSeparateDerivedInputs()
    {
        var world = new FakeWorld();
        var firstContext = Context(world);
        var secondContext = Context(world);
        var key = new object();
        var calls = 0;
        var values = new int[2];
        Parallel.For(0, values.Length, i => values[i] = firstContext.Memo(key, () => Interlocked.Increment(ref calls)));
        AssertThat(calls == 1 && values.All(value => value == calls)).OverrideFailureMessage("one context evaluates its derived factory once").IsTrue();
        var separate = secondContext.Memo(key, () => Interlocked.Increment(ref calls));
        AssertThat(calls == 2 && separate == calls).OverrideFailureMessage("contexts using the same world retain separate derived inputs").IsTrue();
    }

    private static BuildContext Context(IWorldModel world) => new()
    {
        World = world,
        Map = null!,
        WorldSeed = 1,
        WindowNumbering = new(new MapId("fixture"), new string('0', 64), []),
        Catalog = PaletteFixture.Catalog,
        CodeHash = "fixture",
    };
}
