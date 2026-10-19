using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Zones.Common.Tests;

[TestSuite]
public sealed class FeatureContourTests
{
    private const string Selection = "design:test-contour";
    private static readonly Vector2[] Square = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];

    [TestCase, TestCategory(TestReason.Contract)]
    public void OrderedEndpointsKeepTheExplicitCcwArcThroughRingRotationAndReversal()
    {
        var selection = Segment(new(4.1f, 0.1f), new(-0.1f, 0.1f));
        Vector2[] expected = [Square[1], Square[2], Square[3], Square[0]];
        for (var shift = 0; shift < Square.Length; shift++)
            foreach (var reversed in new[] { false, true })
            {
                var ring = Enumerable.Range(0, Square.Length).Select(i => Square[(i + shift) % Square.Length]).ToArray();
                if (reversed) Array.Reverse(ring);
                var world = World(new(new("parent"), new("water"), new PolygonGeometry(new(ring))), selection);
                AssertThat(world.ContourSegment(Selection).SequenceEqual(expected))
                    .OverrideFailureMessage("The explicit CCW arc changed with the ring's start/direction or was shortened.").IsTrue();
                var frame = world.ContourFrame(Selection, originAtEnd: false, forward: false);
                AssertThat(frame.Origin == expected[0] && frame.U == Vector2.UnitX)
                    .OverrideFailureMessage("Frame origin and direction must be independent of the selected arc.").IsTrue();
                var endFrame = world.ContourFrame(Selection, originAtEnd: true);
                AssertThat(endFrame.Origin == expected[^1] && endFrame.U == -Vector2.UnitX).IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EndpointMismatchReportsTheSelectionIdInsteadOfChoosingAnotherSegment()
    {
        foreach (var endpoints in new[] { (new Vector2(4.6f, 0), Vector2.Zero), (Square[1], Square[1]) })
        {
            var world = World(new(new("parent"), new("water"), new PolygonGeometry(new(Square))), Segment(endpoints.Item1, endpoints.Item2));
            WorldDataException? failure = null;
            try { world.ContourSegment(Selection); }
            catch (WorldDataException error) { failure = error; }
            AssertThat(failure is not null && failure.Entry == Selection)
                .OverrideFailureMessage("Bad selection endpoints must report a data error with the selection identity.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SourcePartAndFacadeAreExplicitReferences()
    {
        var facade = Square.Select(p => p + new Vector2(10, 0)).ToArray();
        var parent = new WorldFeature(new("parent"), new("building"), new PolygonGeometry(new(Square)))
        {
            Parts = [new("source-a", new PolygonGeometry(new(Square)), null, null)
                { FacadeGeometry = new PolygonGeometry(new(facade)) },
                new("source-b", new PolygonGeometry(new(Square)), null, null)]
        };
        var selection = Segment(facade[1], facade[0]) with { Props = JsonSerializer.SerializeToElement(new { parent = "parent", sourceId = "source-a", contour = "facade" }) };
        var world = World(parent, selection);
        AssertThat(world.ContourSegment(Selection).SequenceEqual(new[] { facade[1], facade[2], facade[3], facade[0] }))
            .OverrideFailureMessage("Selection lost its exact source part or current facade geometry.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void WorldSelectionLoadsWithoutPublishingARoad()
    {
        var world = WorldModelLoader.Load(new("test"), new Dictionary<string, string>
        {
            ["world/geo.json"] = """
                {"origin":[103,30],"crs":"EPSG:32648","bounds":[-20,-20,20,20],"entityPrefix":"fixture",
                 "boundary":{"outer":[[-20,-20],[20,-20],[20,20],[-20,20]],"holes":[]}}
                """,
            ["world/test.json"] = """
                {"features":[
                  {"id":"fixture:source:way:1","kind":"water","geometry":{"type":"polygon","rings":{"outer":[[0,0],[4,0],[4,4],[0,4]],"holes":[]}}},
                  {"id":"design:test-contour","kind":"contourSegment","geometry":{"type":"polyline","line":{"points":[[4,0],[0,0]]}},
                   "props":{"parent":"fixture:source:way:1"},"note":"Test contour reference, without rendered geometry."}
                ],"trees":[]}
                """
        }).World;
        AssertThat(world.ContourSegment(Selection).SequenceEqual(new[] { Square[1], Square[2], Square[3], Square[0] })).IsTrue();
        AssertThat(world.Network.Roads.Count == 0 && world.Network.Waterways.Count == 0 && world.Network.Bridges.Count == 0)
            .OverrideFailureMessage("A contour reference must not enter the rendered network.").IsTrue();
    }

    private static WorldFeature Segment(Vector2 a, Vector2 b) => new(new(Selection), new("contourSegment"), new PolylineGeometry(new([a, b])))
        { Props = JsonSerializer.SerializeToElement(new { parent = "parent" }) };

    private static FakeWorld World(WorldFeature parent, WorldFeature selection) => new()
        { Features = new Dictionary<FeatureId, WorldFeature> { [parent.Id] = parent, [selection.Id] = selection } };
}
