namespace Campus.Contracts.Diagnostics;

/// <summary>Publishes diagnostics after the owner handles failure; this port does not recover or terminate operations.</summary>
public interface IDiagnosticLog
{
    void Error(string context, Exception error);
    void Error(string context, string message);
    void Warning(string context, Exception error);
    void Warning(string context, string message);
}
