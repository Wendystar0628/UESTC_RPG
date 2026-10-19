using System.Text.Json;
using System.Text;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

/// <summary>Authored source removals applied in isolation to the captured production baseline.</summary>
[TestSuite]
public sealed class HidePartRealCampusTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void AuthoredSourceRemovalsKeepParentIdentitiesAndIndependentEntities()
    {
        var captured = RealMapFixture.ReadTextFiles();
        var removals = captured.Where(pair => pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .SelectMany(pair =>
            {
                using var document = JsonDocument.Parse(pair.Value);
                return document.RootElement.TryGetProperty("ops", out var ops)
                    ? ops.EnumerateArray().Where(op => op.GetProperty("op").GetString() == "hidePart")
                        .Select(op => (HidePartOp)op.Deserialize<OverlayOp>(ContractJson.Options)!).ToArray()
                    : Array.Empty<HidePartOp>();
            }).ToArray();
        AssertThat(removals.Length > 0).OverrideFailureMessage("The authored campus contains no source-removal scenario.").IsTrue();
        // Isolate the authored source-removal operations: terrain is outside this feature-membership contract.
        var files = captured.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(pair.Value), StringComparer.Ordinal);
        var map = RealMapFixture.Entry().Descriptor.Id;
        var before = WorldModelLoader.LoadTopology(map, files);
        files["overlay/hide-part-review.d/0000-fixture.json"] = JsonSerializer.SerializeToUtf8Bytes(
            new OverlayFile(new("review"), new("WP-14"), removals), ContractJson.Options);
        var after = WorldModelLoader.LoadTopology(map, files);
        AssertThat(after.Count == before.Count).OverrideFailureMessage("Source-only removal deleted an entity identity.").IsTrue();
        AssertThat(after.Values.Sum(feature => feature.Parts.Count) ==
            before.Values.Sum(feature => feature.Parts.Count) - removals.Length).OverrideFailureMessage("Unexpected source parts were removed.").IsTrue();
        var targets = removals.Select(op => op.Target).ToHashSet();
        foreach (var id in targets)
        {
            var old = before[id]; var current = after[id];
            AssertThat(current.Name == old.Name && current.Zone == old.Zone && current.Family == old.Family &&
                current.Facade == old.Facade && current.Props?.GetRawText() == old.Props?.GetRawText()).OverrideFailureMessage("Parent properties changed.").IsTrue();
            var removed = removals.Where(op => op.Target == id).Select(op => op.SourceId).ToHashSet(StringComparer.Ordinal);
            var survivors = old.Parts.Where(part => !removed.Contains(part.SourceId)).ToArray();
            AssertThat(current.Parts.Select(part => part.SourceId).SequenceEqual(survivors.Select(part => part.SourceId), StringComparer.Ordinal) &&
                current.Height == survivors.Max(part => part.Height) && current.Storeys == survivors.Max(part => part.Storeys)).OverrideFailureMessage("The remaining real source membership or aggregate height changed.").IsTrue();
            foreach (var part in current.Parts)
                AssertThat(Canonical(part) == Canonical(old.Parts.Single(previous => previous.SourceId == part.SourceId))).OverrideFailureMessage("A surviving real source part changed.").IsTrue();
        }
        foreach (var id in before.Keys.Where(id => !targets.Contains(id)))
            AssertThat(Canonical(after[id]) == Canonical(before[id])).OverrideFailureMessage("An independent A/B entity or one of its parts changed.").IsTrue();
    }

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
}
