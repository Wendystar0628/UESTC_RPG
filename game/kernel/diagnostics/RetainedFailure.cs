using System;
using Campus.Contracts;

namespace Campus.Kernel.Diagnostics;

/// <summary>Combines original producer failures by reference without replacing empty aggregate causes.</summary>
internal static class RetainedFailure
{
    internal static Exception Append(Exception? retained, [FailureHandler("ENG-PERF-STREAM")] Exception error,
        string? message = null)
    {
        if (ReferenceEquals(retained, error)) return error;
        // Empty aggregates are actual failures; only nonempty containers may be expanded.
        if (error is AggregateException { InnerExceptions.Count: > 0 } aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions) retained = Append(retained, inner, message);
            return retained!;
        }
        if (retained is null) return error;
        if (Contains(retained, error)) return retained;
        return message is null ? new AggregateException(retained, error) : new AggregateException(message, retained, error);
    }

    private static bool Contains(Exception retained, Exception error)
    {
        if (ReferenceEquals(retained, error)) return true;
        if (retained is AggregateException aggregate)
            foreach (var inner in aggregate.InnerExceptions)
                if (Contains(inner, error)) return true;
        return false;
    }
}
