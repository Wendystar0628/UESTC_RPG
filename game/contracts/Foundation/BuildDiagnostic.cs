using System.Text.Json.Serialization;

namespace Campus.Contracts;

public enum DiagnosticLevel : byte { Info = 0, Warning = 1, Error = 2 }

/// <summary>A deterministic producer diagnostic; module and owner identity are attached only by the collector.</summary>
/// <remarks>
/// Code is a stable, non-empty identifier; Message is a short, non-empty description derived only from build inputs.
/// Do not include wall-clock time, random identifiers or stack traces. Producers return diagnostics in a stable order.
/// The constructor validates ordinary creation. Value-type default construction and record with expressions can
/// bypass it, so collectors must call Validate on every record before accepting or serializing a report.
/// </remarks>
[method: JsonConstructor]
public readonly record struct BuildDiagnostic(
    DiagnosticLevel Level,
    string Code,
    string Message,
    FeatureId? Feature = null)
{
    public DiagnosticLevel Level { get; init; } = CheckLevel(Level);
    public string Code { get; init; } = CheckText(Code, nameof(Code));
    public string Message { get; init; } = CheckText(Message, nameof(Message));

    /// <summary>Rejects unknown severity and empty text, including invalid values created by default/with.</summary>
    public void Validate()
    {
        _ = CheckLevel(Level);
        _ = CheckText(Code, nameof(Code));
        _ = CheckText(Message, nameof(Message));
        if (Feature is { } feature && string.IsNullOrWhiteSpace(feature.Value))
            throw new ArgumentException("A supplied diagnostic feature ID must be non-empty.", nameof(Feature));
    }

    private static DiagnosticLevel CheckLevel(DiagnosticLevel level) => level switch
    {
        DiagnosticLevel.Info or DiagnosticLevel.Warning or DiagnosticLevel.Error => level,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown build diagnostic level.")
    };

    private static string CheckText(string text, string parameter)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Build diagnostic text must be non-empty.", parameter);
        return text;
    }
}
