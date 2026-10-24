using Campus.Contracts.Atlas;
using Campus.Hud.Map;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

// 大地图目的地预取的触发与释放（SYS-ATLAS-TRAVEL）：只看请求了哪个标记、哪份句柄已释放。
[TestSuite]
public sealed class DestinationPrefetchTests
{
    private static readonly MarkerId A = new("test-a"), B = new("test-b");

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void HoverRequestsOnlyAfterItsDwell()
    {
        var service = new Requests();
        using var prefetch = new DestinationPrefetch(service.Request);
        prefetch.Hover(A, DestinationPrefetch.MarkerHoverSeconds);
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds * 0.5f);
        prefetch.Hover(null, DestinationPrefetch.MarkerHoverSeconds);
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds);
        AssertThat(service.Issued.Count).OverrideFailureMessage("A hover that left before its dwell requested a destination.").IsEqual(0);
        // 同一目标的重复悬停事件（鼠标在标记上移动）不重置计时。
        prefetch.Hover(A, DestinationPrefetch.MarkerHoverSeconds);
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds * 0.5f);
        prefetch.Hover(A, DestinationPrefetch.MarkerHoverSeconds);
        AssertThat(service.Issued.Count).OverrideFailureMessage("A hover requested before its dwell.").IsEqual(0);
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds * 0.5f);
        AssertThat(service.Issued.Count == 1 && service.Issued[0].Id == A)
            .OverrideFailureMessage("A marker hover held for its dwell did not request that marker.").IsTrue();
        prefetch.Hover(B, DestinationPrefetch.IndexHoverSeconds);
        AssertThat(service.Issued[0].Released).OverrideFailureMessage("Moving away did not release the hover destination.").IsTrue();
        prefetch.Tick(DestinationPrefetch.IndexHoverSeconds);
        AssertThat(service.Issued.Count == 2 && service.Issued[1].Id == B && !service.Issued[1].Released)
            .OverrideFailureMessage("An index hover held for its dwell did not request that entry.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OpenCardRequestsAtOnceAndANewDestinationReleasesTheOld()
    {
        var service = new Requests();
        using var prefetch = new DestinationPrefetch(service.Request);
        prefetch.ShowCard(A, TravelStyle.Teleport);
        AssertThat(service.Issued.Count == 1 && service.Issued[0].Id == A)
            .OverrideFailureMessage("Opening the travel card did not request its destination at once.").IsTrue();
        prefetch.Hover(B, DestinationPrefetch.MarkerHoverSeconds);
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds);
        var hovered = service.Issued[^1];
        AssertThat(hovered.Id == B && !hovered.Released && service.Issued[0].Released)
            .OverrideFailureMessage("A newer destination did not replace the card's and release the old handle.").IsTrue();
        // 悬停移开后回到卡片的目的地，同一时刻只持一份。
        prefetch.Hover(null, DestinationPrefetch.MarkerHoverSeconds);
        var card = service.Issued[^1];
        AssertThat(card.Id == A && !card.Released && hovered.Released)
            .OverrideFailureMessage("Leaving the hover did not release it and return to the open card's destination.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ClosingTheCardOrTheMapReleasesTheDestination()
    {
        var service = new Requests();
        using var prefetch = new DestinationPrefetch(service.Request);
        prefetch.ShowCard(A, TravelStyle.Teleport);
        prefetch.HideCard();
        AssertThat(service.Issued.Count == 1 && service.Issued[0].Released)
            .OverrideFailureMessage("Closing the travel card did not release its destination.").IsTrue();
        prefetch.ShowCard(A, TravelStyle.Teleport);
        prefetch.Hover(B, DestinationPrefetch.MarkerHoverSeconds);
        prefetch.Clear();
        var requested = service.Issued.Count;
        prefetch.Tick(DestinationPrefetch.MarkerHoverSeconds);
        AssertThat(service.Issued.Count == requested && service.Issued.All(lease => lease.Released))
            .OverrideFailureMessage("Closing the map left a destination held or let a pending hover request one later.").IsTrue();
    }

    private sealed class Requests
    {
        public List<TestAtlasPrefetch> Issued { get; } = [];

        public IAtlasPrefetch Request(MarkerId id, TravelStyle style)
        {
            var lease = new TestAtlasPrefetch(id);
            Issued.Add(lease);
            return lease;
        }
    }
}
