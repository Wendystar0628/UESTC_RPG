using Campus.Kernel.Diagnostics;
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
            var combined = RetainedFailure.Append(observed, error, "Post effect failed.");
            if (ReferenceEquals(combined, observed)) return;
            if (ReferenceEquals(Interlocked.CompareExchange(ref failure, combined, observed), observed)) return;
        }
    }
}
