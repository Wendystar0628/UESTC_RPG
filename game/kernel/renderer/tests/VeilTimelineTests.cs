using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class VeilTimelineTests
{
    [TestCase]
    public void RevealWaitsForReadinessAndThenCompletes()
    {
        var veil = new VeilTimeline();
        var ready = false;
        veil.HoldUntil(VeilReason.Boot, () => ready, 10);
        var reveal = veil.Reveal(VeilReason.Boot, 1);
        veil.Advance(2);
        if (veil.Opacity != 1 || reveal.IsCompleted) throw new InvalidOperationException("Incomplete scene was revealed.");
        ready = true;
        veil.Advance(.5);
        if (veil.Opacity is <= 0 or >= 1 || reveal.IsCompleted) throw new InvalidOperationException("Reveal did not start after readiness.");
        veil.Advance(.5);
        if (veil.Opacity != 0 || !reveal.IsCompletedSuccessfully) throw new InvalidOperationException("Reveal did not finish.");
    }

    [TestCase]
    public void TimeoutRemainsOpaqueAndFaultsTheAwaiter()
    {
        var veil = new VeilTimeline();
        veil.HoldUntil(VeilReason.MapTravel, () => false, 1);
        var reveal = veil.Reveal(VeilReason.MapTravel, 1);
        veil.Advance(2);
        if (veil.Opacity != 1 || !reveal.IsFaulted || veil.Failure is not TimeoutException)
            throw new InvalidOperationException("Timeout exposed an incomplete map or left an awaiter hanging.");
        _ = reveal.Exception;
    }
}
