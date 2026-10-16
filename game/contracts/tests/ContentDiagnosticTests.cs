using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class ContentDiagnosticTests
{
    [TestCase]
    public void DiagnosticSeverityAndValidationSurviveRoundTripAndRejectBypassedConstruction()
    {
        Check((byte)DiagnosticLevel.Info == 0 && (byte)DiagnosticLevel.Warning == 1 && (byte)DiagnosticLevel.Error == 2,
            "Diagnostic severity values are part of the contract.");
        var diagnostics = new[]
        {
            new BuildDiagnostic(DiagnosticLevel.Info, "source.checked", "Source was checked."),
            new BuildDiagnostic(DiagnosticLevel.Warning, "anchor.invalid", "Placement was skipped.", new FeatureId("design:test-anchor")),
            new BuildDiagnostic(DiagnosticLevel.Error, "source.missing", "Required input is missing.")
        };
        var json = JsonSerializer.Serialize(diagnostics, ContractJson.Options);
        Check(json == JsonSerializer.Serialize(diagnostics, ContractJson.Options), "Same ordered records must produce the same report bytes.");
        var restored = JsonSerializer.Deserialize<BuildDiagnostic[]>(json, ContractJson.Options)
            ?? throw new JsonException("Null diagnostic fixture.");
        Check(restored.SequenceEqual(diagnostics), "Diagnostic order or fields changed during serialization.");
        foreach (var diagnostic in restored) diagnostic.Validate();
        using var document = JsonDocument.Parse(json);
        Check(document.RootElement[0].GetProperty("level").GetString() == "info" &&
            document.RootElement[1].GetProperty("level").GetString() == "warning" &&
            document.RootElement[2].GetProperty("level").GetString() == "error",
            "Diagnostic severity must use the shared string-enum wire convention.");
        var fields = document.RootElement[1].EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal);
        Check(fields.SequenceEqual(new[] { "code", "feature", "level", "message" }), "Producer diagnostics must not carry module/owner identity.");

        Expect<ArgumentException>(() => _ = new BuildDiagnostic((DiagnosticLevel)3, "source.checked", "Source was checked."));
        Expect<ArgumentException>(() => _ = new BuildDiagnostic(DiagnosticLevel.Info, " ", "Source was checked."));
        Expect<ArgumentException>(() => _ = new BuildDiagnostic(DiagnosticLevel.Info, "source.checked", "\t"));
        Expect<ArgumentException>(() => default(BuildDiagnostic).Validate());
        Expect<ArgumentException>(() => (diagnostics[0] with { Code = "" }).Validate());
        Expect<ArgumentException>(() => (diagnostics[0] with { Level = (DiagnosticLevel)255 }).Validate());
        Expect<ArgumentException>(() => (diagnostics[0] with { Feature = default(FeatureId) }).Validate());
    }

    [TestCase]
    public void DiagnosticsAreAnOptionalSidecarAndDoNotChangeContentSerialization()
    {
        var legacy = new ContentOutput([], []);
        var (meshes, parts, emitters, occupancy) = legacy;
        Check(meshes.Count == 0 && parts.Count == 0 && emitters is null && occupancy is null &&
            legacy.Diagnostics.Count == 0 && ContentOutput.Empty.Diagnostics.Count == 0,
            "Existing constructors, deconstruction and default diagnostics must remain compatible.");
        var enriched = legacy with
        {
            Diagnostics = [new(DiagnosticLevel.Warning, "anchor.invalid", "Placement was skipped.")]
        };
        Check(enriched.Diagnostics.Count == 1 && legacy.Diagnostics.Count == 0, "Diagnostics must remain available to the in-memory collector.");
        var plainBytes = JsonSerializer.SerializeToUtf8Bytes(legacy, ContractJson.Options);
        var enrichedBytes = JsonSerializer.SerializeToUtf8Bytes(enriched, ContractJson.Options);
        Check(plainBytes.AsSpan().SequenceEqual(enrichedBytes), "Diagnostics must not enter ContentOutput serialization or its byte-derived hashes.");
        var restored = JsonSerializer.Deserialize<ContentOutput>("""{"meshes":[],"parts":[]}""", ContractJson.Options)
            ?? throw new JsonException("Null legacy content fixture.");
        Check(restored.Diagnostics.Count == 0, "Legacy content without diagnostics must deserialize with an empty sidecar.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
