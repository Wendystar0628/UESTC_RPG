using Campus.Tests;
using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class ContentDiagnosticTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DiagnosticSeverityAndValidationSurviveRoundTripAndRejectBypassedConstruction()
    {
        AssertThat((byte)DiagnosticLevel.Info == 0 && (byte)DiagnosticLevel.Warning == 1 && (byte)DiagnosticLevel.Error == 2).OverrideFailureMessage("Diagnostic severity values are part of the contract.").IsTrue();
        var diagnostics = new[]
        {
            new BuildDiagnostic(DiagnosticLevel.Info, "source.checked", "Source was checked."),
            new BuildDiagnostic(DiagnosticLevel.Warning, "anchor.invalid", "Placement was skipped.", new FeatureId("design:test-anchor")),
            new BuildDiagnostic(DiagnosticLevel.Error, "source.missing", "Required input is missing.")
        };
        var json = JsonSerializer.Serialize(diagnostics, ContractJson.Options);
        AssertThat(json == JsonSerializer.Serialize(diagnostics, ContractJson.Options)).OverrideFailureMessage("Same ordered records must produce the same report bytes.").IsTrue();
        var restored = JsonSerializer.Deserialize<BuildDiagnostic[]>(json, ContractJson.Options)
            ?? throw new JsonException("Null diagnostic fixture.");
        AssertThat(restored.SequenceEqual(diagnostics)).OverrideFailureMessage("Diagnostic order or fields changed during serialization.").IsTrue();
        foreach (var diagnostic in restored) diagnostic.Validate();
        using var document = JsonDocument.Parse(json);
        AssertThat(document.RootElement[0].GetProperty("level").GetString() == "info" &&
            document.RootElement[1].GetProperty("level").GetString() == "warning" &&
            document.RootElement[2].GetProperty("level").GetString() == "error").OverrideFailureMessage("Diagnostic severity must use the shared string-enum wire convention.").IsTrue();
        var fields = document.RootElement[1].EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal);
        AssertThat(fields.SequenceEqual(new[] { "code", "feature", "level", "message" })).OverrideFailureMessage("Producer diagnostics must not carry module/owner identity.").IsTrue();

        AssertThrown(() => _ = new BuildDiagnostic((DiagnosticLevel)3, "source.checked", "Source was checked.")).IsInstanceOf<ArgumentException>();
        AssertThrown(() => _ = new BuildDiagnostic(DiagnosticLevel.Info, " ", "Source was checked.")).IsInstanceOf<ArgumentException>();
        AssertThrown(() => _ = new BuildDiagnostic(DiagnosticLevel.Info, "source.checked", "\t")).IsInstanceOf<ArgumentException>();
        AssertThrown(() => default(BuildDiagnostic).Validate()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (diagnostics[0] with { Code = "" }).Validate()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (diagnostics[0] with { Level = (DiagnosticLevel)255 }).Validate()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (diagnostics[0] with { Feature = default(FeatureId) }).Validate()).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DiagnosticsAreAnOptionalSidecarAndDoNotChangeContentSerialization()
    {
        var legacy = new ContentOutput([], []);
        var (meshes, parts, emitters, occupancy) = legacy;
        AssertThat(meshes.Count == 0 && parts.Count == 0 && emitters is null && occupancy is null &&
            legacy.Diagnostics.Count == 0 && ContentOutput.Empty.Diagnostics.Count == 0).OverrideFailureMessage("Existing constructors, deconstruction and default diagnostics must remain compatible.").IsTrue();
        var enriched = legacy with
        {
            Diagnostics = [new(DiagnosticLevel.Warning, "anchor.invalid", "Placement was skipped.")]
        };
        AssertThat(enriched.Diagnostics.Count == 1 && legacy.Diagnostics.Count == 0).OverrideFailureMessage("Diagnostics must remain available to the in-memory collector.").IsTrue();
        var plainBytes = JsonSerializer.SerializeToUtf8Bytes(legacy, ContractJson.Options);
        var enrichedBytes = JsonSerializer.SerializeToUtf8Bytes(enriched, ContractJson.Options);
        AssertThat(plainBytes.AsSpan().SequenceEqual(enrichedBytes)).OverrideFailureMessage("Diagnostics must not enter ContentOutput serialization or its byte-derived hashes.").IsTrue();
        var restored = JsonSerializer.Deserialize<ContentOutput>("""{"meshes":[],"parts":[]}""", ContractJson.Options)
            ?? throw new JsonException("Null legacy content fixture.");
        AssertThat(restored.Diagnostics.Count == 0).OverrideFailureMessage("Legacy content without diagnostics must deserialize with an empty sidecar.").IsTrue();
    }
}
