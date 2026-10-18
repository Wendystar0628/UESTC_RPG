using Campus.Kernel.Save;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class SaveReadinessObservationTests
{
    [TestCase]
    public void CancellationBeforeMainFrameAttachImmediatelyStopsTheLateToken()
    {
        var holder = new SaveReadinessObservation();
        var token = new CountingToken();
        Task.Run(holder.Dispose).GetAwaiter().GetResult(); // Worker continuation has already left its finally.
        holder.Attach(token); // The previously-running main-frame action publishes late.
        holder.Dispose();
        AssertThat(token.Disposals == 1).OverrideFailureMessage("Late main-frame attachment leaked or disposed more than once.").IsTrue();
    }

    [TestCase]
    public void CancellationAfterAttachAndRepeatedDisposeReleaseOnlyTheAttachedToken()
    {
        var holder = new SaveReadinessObservation();
        var token = new CountingToken(); var otherOwner = new CountingToken();
        holder.Attach(token);
        Task.Run(holder.Dispose).GetAwaiter().GetResult();
        holder.Dispose(); holder.Dispose();
        AssertThat(token.Disposals == 1 && otherOwner.Disposals == 0).OverrideFailureMessage("Disposal duplicated or crossed owner boundaries.").IsTrue();
    }

    [TestCase]
    public void CancellationBetweenTokenCreationAndAttachIsDeterministic()
    {
        var holder = new SaveReadinessObservation();
        var token = new CountingToken();
        // This hook models cancellation after the main-frame API returned but before Attach takes its lock.
        holder.Dispose();
        holder.Attach(token);
        AssertThat(token.Disposals == 1).OverrideFailureMessage("A token created before cancellation escaped its terminated holder.").IsTrue();
    }

    private sealed class CountingToken : IDisposable
    {
        public int Disposals;
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }
}
