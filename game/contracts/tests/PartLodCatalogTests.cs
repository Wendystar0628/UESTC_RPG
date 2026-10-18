using System.Numerics;
using Campus.Contracts.Parts;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class PartLodCatalogTests
{
    [TestCase]
    public void ExistingGenericDefinitionsExposeSortedReadOnlyCapabilitiesWithoutNewMembers()
    {
        var legacy = new LegacyPart(LodLevel.L4, LodLevel.L0, LodLevel.L2);
        IPartDef<int> typed = legacy;
        IPartDef untyped = typed;
        var levels = untyped.SupportedLods;
        AssertThat(levels.SequenceEqual([LodLevel.L0, LodLevel.L2, LodLevel.L4])).OverrideFailureMessage("Default bridge lost numeric order or keys.").IsTrue();
        AssertThrown(() => ((IList<LodLevel>)levels)[0] = LodLevel.L1).IsInstanceOf<NotSupportedException>();
        legacy.Builders.Clear();
        legacy.Builders.Add(LodLevel.L1, _ => throw new InvalidOperationException("A capability query ran a builder."));
        AssertThat(levels.SequenceEqual([LodLevel.L0, LodLevel.L2, LodLevel.L4])).OverrideFailureMessage("A returned default-interface list aliases mutable Lods.Keys.").IsTrue();
        AssertThat(untyped.SupportedLods.SequenceEqual([LodLevel.L1])).OverrideFailureMessage("The generic bridge did not read this object's current builders.").IsTrue();
        legacy.Builders.Add((LodLevel)255, _ => throw new InvalidOperationException("A capability query ran a builder."));
        AssertThrown(() => { _ = untyped.SupportedLods; }).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    [TestCase]
    public void CatalogRejectsUnknownIdsDuplicatesAndInvalidDefinitions()
    {
        var valid = new LegacyPart(LodLevel.L0);
        var catalog = new PartLodCatalog([Pair("known", valid)]);
        AssertThat(catalog.Supports(new("known"), LodLevel.L0) && !catalog.Supports(new("known"), LodLevel.L2)).OverrideFailureMessage("Known supported/unsupported LOD query changed.").IsTrue();
        KeyNotFoundException? unknown = null;
        AssertThrown(() =>
        {
            try { catalog.Supports(new("missing"), LodLevel.L0); }
            catch (KeyNotFoundException caught) { unknown = caught; throw; }
        }).IsInstanceOf<KeyNotFoundException>();
        AssertThat(unknown!.Message.Contains("missing", StringComparison.Ordinal)).OverrideFailureMessage("Unknown ID diagnostic lost the ID.").IsTrue();
        AssertThrown(() => catalog.SupportedLods(new("missing"))).IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => catalog.Supports(new("known"), (LodLevel)255)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => catalog.SupportedLods(default)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog(null!)).IsInstanceOf<ArgumentNullException>();
        AssertThrown(() => new PartLodCatalog([new(default, valid)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair(" ", valid)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("same", valid), Pair("same", valid)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("null-definition", null!)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("empty", new RawDefinition([]))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("bad-enum", new RawDefinition([(LodLevel)255]))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("repeat-lod", new RawDefinition([LodLevel.L0, LodLevel.L0]))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([Pair("null-list", new RawDefinition(null!))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PartLodCatalog([]).Supports(new("absent"), LodLevel.L0)).IsInstanceOf<KeyNotFoundException>();
    }

    [TestCase]
    public void CatalogCapturesOnceAndIsIndependentOfInputOrderAndLaterMutation()
    {
        var legacy = new LegacyPart(LodLevel.L2, LodLevel.L0);
        var mutable = new[] { LodLevel.L4, LodLevel.L1 };
        var raw = new RawDefinition(mutable);
        var source = new List<KeyValuePair<PartId, IPartDef>> { Pair("z", raw), Pair("a", legacy) };
        var frozen = new PartLodCatalog(source);
        AssertThat(raw.Reads == 1).OverrideFailureMessage("Catalog queried the definition more than once during capture.").IsTrue();
        var reversed = new PartLodCatalog([Pair("a", new LegacyPart(LodLevel.L0, LodLevel.L2)),
            Pair("z", new RawDefinition([LodLevel.L1, LodLevel.L4]))]);
        foreach (var id in new[] { new PartId("a"), new PartId("z") })
            AssertThat(frozen.SupportedLods(id).SequenceEqual(reversed.SupportedLods(id))).OverrideFailureMessage("Insertion order changed the catalog.").IsTrue();
        source.Clear();
        legacy.Builders.Clear();
        mutable[0] = LodLevel.L0;
        raw.ThrowOnRead = true;
        AssertThat(frozen.SupportedLods(new("a")).SequenceEqual([LodLevel.L0, LodLevel.L2])).OverrideFailureMessage("Frozen catalog still reads a definition's Lods.").IsTrue();
        AssertThat(frozen.SupportedLods(new("z")).SequenceEqual([LodLevel.L1, LodLevel.L4])).OverrideFailureMessage("Frozen catalog aliases a provider-owned list.").IsTrue();
        AssertThat(frozen.Supports(new("z"), LodLevel.L4) && !frozen.Supports(new("z"), LodLevel.L0) && raw.Reads == 1).OverrideFailureMessage("A query re-read or reinterpreted mutable definitions.").IsTrue();
        AssertThrown(() => ((IList<LodLevel>)frozen.SupportedLods(new("z")))[0] = LodLevel.L0).IsInstanceOf<NotSupportedException>();
    }

    private static KeyValuePair<PartId, IPartDef> Pair(string id, IPartDef definition) => new(new(id), definition);

    // Implements only the existing generic contract's abstract members: no SupportedLods member.
    private sealed class LegacyPart(params LodLevel[] levels) : IPartDef<int>
    {
        public Dictionary<LodLevel, Func<int, PartGeometry>> Builders { get; } = levels.ToDictionary(level => level,
            level => (Func<int, PartGeometry>)(parameter => throw new InvalidOperationException("A capability query ran a builder.")));
        public IReadOnlyDictionary<LodLevel, Func<int, PartGeometry>> Lods => Builders;
        public Box3 Bounds(int p) => new(Vector3.Zero, Vector3.One);
        public PartBudget Budget { get; } = new(0, 0);
    }

    private sealed class RawDefinition(IReadOnlyList<LodLevel> levels) : IPartDef
    {
        public int Reads { get; private set; }
        public bool ThrowOnRead { get; set; }
        public IReadOnlyList<LodLevel> SupportedLods
        {
            get
            {
                Reads++;
                if (ThrowOnRead) throw new InvalidOperationException("A frozen catalog retained its definition.");
                return levels;
            }
        }
    }
}
