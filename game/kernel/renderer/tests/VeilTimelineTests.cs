using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class VeilTimelineTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LatestHolderPresentsAndOnlyTheFinalHolderReveals()
    {
        var veil = new VeilTimeline();
        using var faulted = veil.Cover(VeilReason.MapTravel, 0, VeilContent.Frontispiece);
        using var next = veil.Cover(VeilReason.MapTravel, 0);
        if (faulted.IsCurrent || !next.IsCurrent || veil.Content != VeilContent.Fog)
            throw new InvalidOperationException("The latest lease did not own presentation.");
        var released = next.Reveal(1);
        veil.Advance(2);
        if (!released.IsCompletedSuccessfully || veil.Opacity != 1 || !faulted.IsCurrent || veil.Content != VeilContent.Frontispiece)
            throw new InvalidOperationException("Releasing another lease exposed the failed holder's world.");
        next.Dispose();
        var reveal = faulted.Reveal(1);
        faulted.Dispose();
        veil.Advance(.5);
        if (veil.Opacity is <= 0 or >= 1 || reveal.IsCompleted)
            throw new InvalidOperationException("Final reveal lost its authored fade.");
        veil.Advance(.5);
        if (veil.Opacity != 0 || !reveal.IsCompletedSuccessfully)
            throw new InvalidOperationException("Final reveal did not finish.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NewCoverDoesNotCancelAnOlderCoverAwaiter()
    {
        var veil = new VeilTimeline();
        veil.Cover(VeilReason.Boot, 0).Dispose();
        using var old = veil.Cover(VeilReason.Teleport, 1);
        veil.Advance(.25);
        using var next = veil.Cover(VeilReason.Quality, 1);
        if (old.Covered.IsCompleted || next.Covered.IsCompleted)
            throw new InvalidOperationException("A cover completed before opacity reached one.");
        veil.Advance(1);
        if (!old.Covered.IsCompletedSuccessfully || !next.Covered.IsCompletedSuccessfully || veil.Opacity != 1)
            throw new InvalidOperationException("A replacement cover stranded an active holder.");
        old.Dispose();
        old.Dispose();
        if (veil.Opacity != 1 || !next.IsCurrent)
            throw new InvalidOperationException("A stale disposal released the current cover.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DisposalBeforeCoveredCancelsOnlyItsOwnWaiter()
    {
        var veil = new VeilTimeline();
        veil.Cover(VeilReason.Boot, 0).Dispose();
        var old = veil.Cover(VeilReason.Transit, 1);
        using var next = veil.Cover(VeilReason.Transit, 1);
        old.Dispose();
        if (!old.Covered.IsCanceled || next.Covered.IsCompleted || !next.IsCurrent)
            throw new InvalidOperationException("Canceled cover retained its waiter or affected another owner.");
        veil.Advance(1);
        if (!next.Covered.IsCompletedSuccessfully || veil.Opacity != 1)
            throw new InvalidOperationException("Disposal interrupted the surviving cover.");
        next.Dispose();
        if (veil.Opacity != 0)
            throw new InvalidOperationException("The final disposed lease leaked its cover.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoverDuringRevealCancelsTheFadeWithoutLosingTheNewOwner()
    {
        var veil = new VeilTimeline();
        var old = veil.Cover(VeilReason.Cinematic, 0);
        var reveal = old.Reveal(1);
        veil.Advance(.5);
        using var next = veil.Cover(VeilReason.Respawn, 1);
        old.Dispose();
        if (!reveal.IsCanceled || !next.IsCurrent || next.Covered.IsCompleted)
            throw new InvalidOperationException("A new cover did not replace the outgoing fade.");
        veil.Advance(1);
        if (!next.Covered.IsCompletedSuccessfully || veil.Opacity != 1)
            throw new InvalidOperationException("An interrupted reveal exposed the new holder.");
    }
}
