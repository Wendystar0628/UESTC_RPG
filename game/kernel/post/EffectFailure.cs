using Campus.Contracts;

namespace Campus.Kernel.Post;

/// <summary>Retains render-thread failures for the effects' existing map-readiness publication (ENG-PERF-STREAM).</summary>
internal static class EffectFailure
{
    internal static void Record(ref Exception? failure, [FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        while (true)
        {
            var observed = Volatile.Read(ref failure);
            var combined = Append(observed, error);
            if (ReferenceEquals(combined, observed)) return;
            if (ReferenceEquals(Interlocked.CompareExchange(ref failure, combined, observed), observed)) return;
        }
    }

    private static Exception Append(Exception? retained, Exception error)
    {
        if (ReferenceEquals(retained, error)) return error;
        // Empty aggregates are actual failures; only nonempty containers may be expanded.
        if (error is AggregateException { InnerExceptions.Count: > 0 } aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions) retained = Append(retained, inner);
            return retained!;
        }
        if (retained is null) return error;
        if (Contains(retained, error)) return retained;
        return new AggregateException("Post effect failed.", retained, error);
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
