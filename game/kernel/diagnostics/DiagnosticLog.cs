using System;
using Campus.Contracts;
using Campus.Contracts.Diagnostics;

namespace Campus.Kernel.Diagnostics;

/// <summary>The single diagnostic output; recovery and termination remain with the caller.</summary>
[KernelService(typeof(IDiagnosticLog))]
public sealed class DiagnosticLog : IDiagnosticLog
{
    public static void Error(string context, Exception error) => Error(context, error.ToString());
    public static void Error(string context, string message) => Write("Error", context, message);
    public static void Warning(string context, Exception error) => Warning(context, error.ToString());
    public static void Warning(string context, string message) => Write("Warning", context, message);

    void IDiagnosticLog.Error(string context, Exception error) => Error(context, error);
    void IDiagnosticLog.Error(string context, string message) => Error(context, message);
    void IDiagnosticLog.Warning(string context, Exception error) => Warning(context, error);
    void IDiagnosticLog.Warning(string context, string message) => Warning(context, message);

    private static void Write(string level, string context, string message) =>
        Console.Error.WriteLine($"[{level}] {context} {message}");
}
