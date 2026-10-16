using System.Numerics;
using Campus.Contracts.Parts;
using GdUnit4;

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
        Require(levels.SequenceEqual([LodLevel.L0, LodLevel.L2, LodLevel.L4]), "Default bridge lost numeric order or keys.");
        Expect<NotSupportedException>(() => ((IList<LodLevel>)levels)[0] = LodLevel.L1);
        legacy.Builders.Clear();
        legacy.Builders.Add(LodLevel.L1, _ => throw new InvalidOperationException("A capability query ran a builder."));
        Require(levels.SequenceEqual([LodLevel.L0, LodLevel.L2, LodLevel.L4]), "A returned default-interface list aliases mutable Lods.Keys.");
        Require(untyped.SupportedLods.SequenceEqual([LodLevel.L1]), "The generic bridge did not read this object's current builders.");
        legacy.Builders.Add((LodLevel)255, _ => throw new InvalidOperationException("A capability query ran a builder."));
        Expect<ArgumentOutOfRangeException>(() => { _ = untyped.SupportedLods; });
    }

    [TestCase]
    public void CatalogRejectsUnknownIdsDuplicatesAndInvalidDefinitions()
    {
        var valid = new LegacyPart(LodLevel.L0);
        var catalog = new PartLodCatalog([Pair("known", valid)]);
        Require(catalog.Supports(new("known"), LodLevel.L0) && !catalog.Supports(new("known"), LodLevel.L2), "Known supported/unsupported LOD query changed.");
        var unknown = Expect<KeyNotFoundException>(() => catalog.Supports(new("missing"), LodLevel.L0));
        Require(unknown.Message.Contains("missing", StringComparison.Ordinal), "Unknown ID diagnostic lost the ID.");
        Expect<KeyNotFoundException>(() => catalog.SupportedLods(new("missing")));
        Expect<ArgumentOutOfRangeException>(() => catalog.Supports(new("known"), (LodLevel)255));
        Expect<ArgumentException>(() => catalog.SupportedLods(default));
        Expect<ArgumentNullException>(() => new PartLodCatalog(null!));
        Expect<ArgumentException>(() => new PartLodCatalog([new(default, valid)]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair(" ", valid)]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("same", valid), Pair("same", valid)]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("null-definition", null!)]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("empty", new RawDefinition([]))]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("bad-enum", new RawDefinition([(LodLevel)255]))]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("repeat-lod", new RawDefinition([LodLevel.L0, LodLevel.L0]))]));
        Expect<ArgumentException>(() => new PartLodCatalog([Pair("null-list", new RawDefinition(null!))]));
        Expect<KeyNotFoundException>(() => new PartLodCatalog([]).Supports(new("absent"), LodLevel.L0));
    }

    [TestCase]
    public void CatalogCapturesOnceAndIsIndependentOfInputOrderAndLaterMutation()
    {
        var legacy = new LegacyPart(LodLevel.L2, LodLevel.L0);
        var mutable = new[] { LodLevel.L4, LodLevel.L1 };
        var raw = new RawDefinition(mutable);
        var source = new List<KeyValuePair<PartId, IPartDef>> { Pair("z", raw), Pair("a", legacy) };
        var frozen = new PartLodCatalog(source);
        Require(raw.Reads == 1, "Catalog queried the definition more than once during capture.");
        var reversed = new PartLodCatalog([Pair("a", new LegacyPart(LodLevel.L0, LodLevel.L2)),
            Pair("z", new RawDefinition([LodLevel.L1, LodLevel.L4]))]);
        foreach (var id in new[] { new PartId("a"), new PartId("z") })
            Require(frozen.SupportedLods(id).SequenceEqual(reversed.SupportedLods(id)), "Insertion order changed the catalog.");
        source.Clear();
        legacy.Builders.Clear();
        mutable[0] = LodLevel.L0;
        raw.ThrowOnRead = true;
        Require(frozen.SupportedLods(new("a")).SequenceEqual([LodLevel.L0, LodLevel.L2]), "Frozen catalog still reads a definition's Lods.");
        Require(frozen.SupportedLods(new("z")).SequenceEqual([LodLevel.L1, LodLevel.L4]), "Frozen catalog aliases a provider-owned list.");
        Require(frozen.Supports(new("z"), LodLevel.L4) && !frozen.Supports(new("z"), LodLevel.L0) && raw.Reads == 1,
            "A query re-read or reinterpreted mutable definitions.");
        Expect<NotSupportedException>(() => ((IList<LodLevel>)frozen.SupportedLods(new("z")))[0] = LodLevel.L0);
    }

    private static KeyValuePair<PartId, IPartDef> Pair(string id, IPartDef definition) => new(new(id), definition);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static T Expect<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

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
