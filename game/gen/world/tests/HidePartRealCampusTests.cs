using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

/// <summary>Authored source removals applied in isolation to the captured production baseline.</summary>
[TestSuite]
public sealed class HidePartRealCampusTests
{
    [TestCase]
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
        var files = captured.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var before = RealMapFixture.LoadPrepared(files, options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() }).World;
        files["overlay/hide-part-review.d/0000-fixture.json"] = JsonSerializer.Serialize(
            new OverlayFile(new("review"), new("WP-14"), removals), ContractJson.Options);
        var after = RealMapFixture.LoadPrepared(files, options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() }).World;
        AssertThat(after.Features.Count == before.Features.Count).OverrideFailureMessage("Source-only removal deleted an entity identity.").IsTrue();
        AssertThat(after.Features.Values.Sum(feature => feature.Parts.Count) ==
            before.Features.Values.Sum(feature => feature.Parts.Count) - removals.Length).OverrideFailureMessage("Unexpected source parts were removed.").IsTrue();
        var targets = removals.Select(op => op.Target).ToHashSet();
        foreach (var id in targets)
        {
            var old = before.Features[id]; var current = after.Features[id];
            AssertThat(current.Name == old.Name && current.Zone == old.Zone && current.Family == old.Family &&
                current.Facade == old.Facade && current.Props?.GetRawText() == old.Props?.GetRawText()).OverrideFailureMessage("Parent properties changed.").IsTrue();
            var removed = removals.Where(op => op.Target == id).Select(op => op.SourceId).ToHashSet(StringComparer.Ordinal);
            var survivors = old.Parts.Where(part => !removed.Contains(part.SourceId)).ToArray();
            AssertThat(current.Parts.Select(part => part.SourceId).SequenceEqual(survivors.Select(part => part.SourceId), StringComparer.Ordinal) &&
                current.Height == survivors.Max(part => part.Height) && current.Storeys == survivors.Max(part => part.Storeys)).OverrideFailureMessage("The remaining real source membership or aggregate height changed.").IsTrue();
            foreach (var part in current.Parts)
                AssertThat(Canonical(part) == Canonical(old.Parts.Single(previous => previous.SourceId == part.SourceId))).OverrideFailureMessage("A surviving real source part changed.").IsTrue();
        }
        foreach (var id in before.Features.Keys.Where(id => !targets.Contains(id)))
            AssertThat(Canonical(after.Features[id]) == Canonical(before.Features[id])).OverrideFailureMessage("An independent A/B entity or one of its parts changed.").IsTrue();
    }

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
}
