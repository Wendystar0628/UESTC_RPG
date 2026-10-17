using Campus.Kernel.Save;
using GdUnit4;

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
        Check(token.Disposals == 1, "Late main-frame attachment leaked or disposed more than once.");
    }

    [TestCase]
    public void CancellationAfterAttachAndRepeatedDisposeReleaseOnlyTheAttachedToken()
    {
        var holder = new SaveReadinessObservation();
        var token = new CountingToken(); var otherOwner = new CountingToken();
        holder.Attach(token);
        Task.Run(holder.Dispose).GetAwaiter().GetResult();
        holder.Dispose(); holder.Dispose();
        Check(token.Disposals == 1 && otherOwner.Disposals == 0, "Disposal duplicated or crossed owner boundaries.");
    }

    [TestCase]
    public void CancellationBetweenTokenCreationAndAttachIsDeterministic()
    {
        var holder = new SaveReadinessObservation();
        var token = new CountingToken();
        // This hook models cancellation after the main-frame API returned but before Attach takes its lock.
        holder.Dispose();
        holder.Attach(token);
        Check(token.Disposals == 1, "A token created before cancellation escaped its terminated holder.");
    }

    private sealed class CountingToken : IDisposable
    {
        public int Disposals;
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
