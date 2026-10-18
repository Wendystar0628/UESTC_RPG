using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// Stadium ground: an untagged stadium or sports-centre outline has no surface, a running track is a ring around its infield,
/// and a football pitch is natural turf with its white lines.
/// </summary>
[TestSuite]
public sealed class GroundStadiumTests
{
    private static readonly GroundSection Section = GroundSection.Default;

    [TestCase]
    public void UntaggedStadiumAndSportsCentreOutlinesHaveNoCourt()
    {
        var world = TestWorld.Of(
        [
            Entity("test:way:1", """{"leisure":"stadium"}""", """{"kind":"sport"}""", Rect(-100f, -100f, 100f, 100f)),
            Entity("test:way:2", """{"leisure":"sports_centre"}""", """{"kind":"sport"}""", Rect(300f, -50f, 380f, 50f)),
            Entity("test:way:3", """{"leisure":"pitch"}""", """{"kind":"sport"}""", Rect(-300f, -30f, -250f, 30f)),
            Entity("test:way:4", """{"leisure":"pitch","sport":"basketball"}""", """{"kind":"sport","sport":"basketball"}""", Rect(-200f, 150f, -170f, 180f)),
        ]);

        var (shapes, _) = GroundSources.Read(world);
        AssertThat(shapes.All(s => s.Id.Value is not ("test:way:1" or "test:way:2"))).OverrideFailureMessage("A stadium or sports centre without a sport tag only encloses other features: it makes no ground shape.").IsTrue();
        AssertThat(shapes.Count(s => s.Kind == SurfaceKind.Court) == 1 && shapes.Single(s => s.Kind == SurfaceKind.Court).Id.Value == "test:way:4").OverrideFailureMessage("Only the pitch that names a sport is a court.").IsTrue();
        AssertThat(shapes.Single(s => s.Id.Value == "test:way:3").Kind == SurfaceKind.Turf).OverrideFailureMessage("A pitch without a sport tag is open turf.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 80f, 80f))).OverrideFailureMessage("Inside the stadium outline the lawn shows, not a court.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 340f, 0f))).OverrideFailureMessage("Inside the sports centre outline the lawn shows, not a court.").IsTrue();
        AssertThat(SurfaceAt(world, -275f, 0f) == GroundSurface.Turf).OverrideFailureMessage("An untagged pitch is turf, not a court.").IsTrue();
        AssertThat(SurfaceAt(world, -185f, 165f) == GroundSurface.Court).OverrideFailureMessage("A tagged court is still a court.").IsTrue();
    }

    [TestCase]
    public void RunningTrackIsARingAroundItsInfield()
    {
        var oval = Oval(0f, 0f, 40f, 30f);   // 60 m wide, 140 m long: the line along the track's inner edge
        var world = TestWorld.Of([Entity("test:way:10", """{"leisure":"track","sport":"running"}""", """{"kind":"sport","sport":"running"}""", oval)]);

        var tracks = GroundSources.Read(world).Shapes.Where(s => s.Kind == SurfaceKind.Track).ToArray();
        AssertThat(tracks.Length == 1 && tracks[0].Rings is { Length: 2 }).OverrideFailureMessage("A running track polygon is one ring: an outer and an inner outline.").IsTrue();
        var layout = GroundLayout.Of(world);
        AssertThat(Math.Abs((double)(-SportGround.TrackWidth * 0.5) - (double)(layout.SignedDistance(SurfaceKind.Track, new(30f + SportGround.TrackWidth * 0.5f, 0f), 50f))) <= 0.05).OverrideFailureMessage($"the middle of the ring is half a track width from both edges: expected {-SportGround.TrackWidth * 0.5}, got {layout.SignedDistance(SurfaceKind.Track, new(30f + SportGround.TrackWidth * 0.5f, 0f), 50f)}.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Track, new(30f, 0f), 50f) is > -0.01f and < 0.01f).OverrideFailureMessage("The traced line is the inner edge of the ring.").IsTrue();

        // Along the straight (x = 30 is the inner edge): the ring lies outside it, the infield inside is lawn.
        AssertThat(SurfaceAt(world, 34f, 0f) == GroundSurface.Track).OverrideFailureMessage("4 m outside the inner edge is track.").IsTrue();
        AssertThat(SurfaceAt(world, 37f, 0f) == GroundSurface.Track).OverrideFailureMessage("7 m outside the inner edge is still track.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 26f, 0f))).OverrideFailureMessage("Just inside the inner edge is infield lawn.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 0f, 0f))).OverrideFailureMessage("The infield is lawn, not track.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 41f, 0f))).OverrideFailureMessage("Beyond the track width the lawn returns.").IsTrue();
        // Round the end (the semicircle is centred on z = 40 and reaches z = 70).
        AssertThat(SurfaceAt(world, 0f, 74f) == GroundSurface.Track).OverrideFailureMessage("The ring runs round the end of the oval.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 0f, 64f)) && IsLawn(SurfaceAt(world, 0f, 85f))).OverrideFailureMessage("Infield and outside of the end are lawn.").IsTrue();
    }

    [TestCase]
    public void TrackTaggedByLeisureAloneAndTrackLinesAreBands()
    {
        // leisure=track without any sport tag is a track too.
        var bare = TestWorld.Of([Entity("test:way:11", """{"leisure":"track"}""", """{"kind":"sport"}""", Oval(0f, 0f, 40f, 30f))]);
        AssertThat(GroundSources.Read(bare).Shapes.Count(s => s.Kind == SurfaceKind.Track) == 1).OverrideFailureMessage("leisure=track is a running track.").IsTrue();

        // A track mapped as a closed line is a band along it that closes on itself.
        var points = Oval(0f, 0f, 40f, 30f).Outer;
        var line = WorldFeatureFixture.Create("test:way:12", "sport",
            new PolylineGeometry(new Polyline([.. points, points[0]])), """{"kind":"sport","sport":"running"}""", FeatureSource.Osm);
        var shape = GroundSources.Read(TestWorld.Of([line])).Shapes.Single(s => s.Kind == SurfaceKind.Track);
        AssertThat(shape.Line is { } chain && chain.Length == points.Count + 1 && chain[0] == chain[^1]).OverrideFailureMessage("A closed track line stays closed.").IsTrue();
        AssertThat(Math.Abs((double)(SportGround.TrackWidth * 0.5) - (double)(shape.HalfWidth)) <= 0.01).OverrideFailureMessage($"track line half width: expected {SportGround.TrackWidth * 0.5}, got {shape.HalfWidth}.").IsTrue();

        // A concave outline cannot be offset outward: it falls back to a band centred on the outline, never a filled area.
        var dent = new PolygonRings([new(0f, 0f), new(60f, 0f), new(60f, 40f), new(40f, 40f), new(40f, 20f), new(20f, 20f), new(20f, 40f), new(0f, 40f)]);
        var concave = TestWorld.Of([Entity("test:way:13", """{"leisure":"track","sport":"running"}""", """{"kind":"sport","sport":"running"}""", dent)]);
        var fallback = GroundSources.Read(concave).Shapes.Single(s => s.Kind == SurfaceKind.Track);
        AssertThat(fallback.Line is not null && fallback.Rings is null).OverrideFailureMessage("A concave track polygon becomes a band along its outline.").IsTrue();
    }

    [TestCase]
    public void FootballPitchIsTurfWithWhiteLines()
    {
        var pitch = Pitch(0f, 0f, 108f, 70f, 8f);
        var world = TestWorld.Of([Entity("test:way:20", """{"leisure":"pitch","sport":"soccer"}""", """{"kind":"sport","sport":"soccer"}""", pitch.Rings)]);
        AssertThat(GroundSources.Read(world).Shapes.Single().Kind == SurfaceKind.Turf).OverrideFailureMessage("A soccer pitch is turf.").IsTrue();
        AssertThat(SurfaceAt(world, pitch.Centre.X + 20f * pitch.Across.X, pitch.Centre.Y + 20f * pitch.Across.Y) == GroundSurface.Turf).OverrideFailureMessage("The pitch is turf.").IsTrue();

        var layout = GroundLayout.Of(world);
        var quads = QuadsNear(layout, 0f, 0f, 120f);
        AssertThat(quads.Count > 100).OverrideFailureMessage($"A pitch carries over a hundred line strips (straight lines and circle chords), got {quads.Count}.").IsTrue();
        AssertThat(quads.All(q => q.Color == GroundColor.Marking && q.Level == Section.PavingY)).OverrideFailureMessage("Pitch lines are white and lie on the turf level.").IsTrue();
        // No line leaves the turf.
        foreach (var q in quads)
            foreach (var corner in new[] { q.A, q.B, q.C, q.D })
                AssertThat(layout.SignedDistance(SurfaceKind.Turf, corner, 5f) < 0f).OverrideFailureMessage($"A pitch line corner {corner} lies outside the turf.").IsTrue();

        bool Covered(float u, float v) => Covers(quads, pitch.Centre + pitch.Along * u + pitch.Across * v);
        // The lines are laid out on the rectangle inside the outer line, which lies inside the edging band of the boundary.
        var inset = Section.EdgingWidth + Section.Pitch.LineWidth * 0.5f + Section.Pitch.EdgeClear;
        var hl = pitch.Length * 0.5f - inset;
        var hw = pitch.Width * 0.5f - inset;
        var su = 2f * hl / 105f;
        var sv = 2f * hw / 68f;
        var radius = 9.15f * (su + sv) * 0.5f;
        AssertThat(Covered(0f, 10f) && Covered(0f, -25f)).OverrideFailureMessage("The halfway line runs across the pitch.").IsTrue();
        AssertThat(!Covered(5f, 10f) && !Covered(-5f, -10f)).OverrideFailureMessage("Nothing else crosses the middle.").IsTrue();
        AssertThat(Covered(radius, 0f) && Covered(-radius, 0f)).OverrideFailureMessage("The centre circle crosses the long axis at its radius.").IsTrue();
        AssertThat(Covered(0f, 0f)).OverrideFailureMessage("The kick-off spot.").IsTrue();
        AssertThat(Covered(hl, 0f) && Covered(-hl, 0f) && Covered(0f, hw) && Covered(0f, -hw)).OverrideFailureMessage("The outer line.").IsTrue();
        foreach (var end in new[] { -1f, 1f })
        {
            var goal = end * hl;
            AssertThat(Covered(goal - end * 16.5f * su, 0f)).OverrideFailureMessage("The edge of the penalty area, in front of goal.").IsTrue();
            AssertThat(Covered(goal - end * 5.5f * su, 0f)).OverrideFailureMessage("The edge of the goal area.").IsTrue();
            AssertThat(Covered(goal - end * 11f * su, 0f)).OverrideFailureMessage("The penalty spot.").IsTrue();
            AssertThat(!Covered(goal - end * 8.25f * su, 0f)).OverrideFailureMessage("Between the goal area and the penalty spot there is no line.").IsTrue();
            AssertThat(Covered(goal - end * 8.25f * su, 20.16f * sv) && Covered(goal - end * 8.25f * su, -20.16f * sv)).OverrideFailureMessage("The sides of the penalty area.").IsTrue();
            AssertThat(Covered(goal - end * 11f * su - end * radius, 0f)).OverrideFailureMessage("The penalty arc bows out past the area edge.").IsTrue();
        }

        // Pitch lines are real decal geometry in the tile that holds the pitch.
        var output = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), LodLevel.L0);
        var marking = output.Meshes.Where(m => m.Material.Spec.Lit.Value == "ground.marking.lit").ToArray();
        AssertThat(marking.Length > 0 && marking.All(m => m.Flags!.All(f => (f & GroundPalette.FlagDecal) != 0))).OverrideFailureMessage("Pitch lines are decals.").IsTrue();
        foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
        foreach (var mesh in marking)
            for (var v = 0; v < mesh.Position.Length / 3; v++)
                AssertThat(Math.Abs((double)(Section.PavingY + Section.DecalLift) - (double)(mesh.Position[3 * v + 1])) <= 0.001).OverrideFailureMessage($"pitch line height above flat ground: expected {Section.PavingY + Section.DecalLift}, got {mesh.Position[3 * v + 1]}.").IsTrue();
    }

    [TestCase]
    public void PitchesThatAreNotFootballRectanglesGetNoLines()
    {
        var tiny = Pitch(0f, 0f, 24f, 12f, 0f);                       // narrower than a pitch worth marking
        var square = Pitch(200f, 0f, 50f, 50f, 0f);                   // not a football proportion
        var trapezoid = new PolygonRings([new(0f, 200f), new(100f, 200f), new(80f, 260f), new(20f, 260f)]);
        var world = TestWorld.Of(
        [
            Entity("test:way:30", """{"sport":"soccer"}""", """{"kind":"sport","sport":"soccer"}""", tiny.Rings),
            Entity("test:way:31", """{"sport":"soccer"}""", """{"kind":"sport","sport":"soccer"}""", square.Rings),
            Entity("test:way:32", """{"sport":"soccer"}""", """{"kind":"sport","sport":"soccer"}""", trapezoid),
        ]);
        AssertThat(GroundSources.Read(world).Shapes.Count(s => s.Kind == SurfaceKind.Turf) == 3).OverrideFailureMessage("All three are still turf.").IsTrue();
        AssertThat(QuadsNear(GroundLayout.Of(world), 100f, 100f, 400f).Count == 0).OverrideFailureMessage("Only rectangles of football proportions are marked.").IsTrue();
    }

    [TestCase]
    public void TrackOutsidePitchAndLinesStayClear()
    {
        // A stadium as the campus has it: an outline with no sport tag, the track line, and a pitch inside the track.
        var oval = Oval(0f, 0f, 45f, 40f);
        var pitch = Pitch(0f, 0f, 100f, 62f, 0f);
        var world = TestWorld.Of(
        [
            Entity("test:way:40", """{"leisure":"stadium"}""", """{"kind":"sport"}""", Oval(0f, 0f, 70f, 70f)),
            Entity("test:way:41", """{"leisure":"track","sport":"running"}""", """{"kind":"sport","sport":"running"}""", oval),
            Entity("test:way:42", """{"leisure":"pitch","sport":"soccer"}""", """{"kind":"sport","sport":"soccer"}""", pitch.Rings),
        ]);
        var layout = GroundLayout.Of(world);
        var quads = QuadsNear(layout, 0f, 0f, 160f);
        AssertThat(quads.Count > 100).OverrideFailureMessage("The pitch inside the track is marked.").IsTrue();
        AssertThat(quads.All(q => layout.SignedDistance(SurfaceKind.Track, (q.A + q.C) * 0.5f, 5f) > 0f)).OverrideFailureMessage("No pitch line lies on the track ring.").IsTrue();
        AssertThat(SurfaceAt(world, 0f, 20f) == GroundSurface.Turf).OverrideFailureMessage("The pitch is turf.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 36f, 0f))).OverrideFailureMessage("Between the pitch and the track the infield is lawn.").IsTrue();
        AssertThat(SurfaceAt(world, 44f, 0f) == GroundSurface.Track).OverrideFailureMessage("The ring starts at the traced line, 40 m from the centre.").IsTrue();
        AssertThat(IsLawn(SurfaceAt(world, 60f, 60f))).OverrideFailureMessage("Inside the stadium outline, outside the track, the lawn shows.").IsTrue();
    }

    [TestCase]
    public void RealCampusStadiumIsARingAroundTurfOnLawn()
    {
        var world = RealMapFixture.Load().World;
        var shapes = GroundSources.Read(world).Shapes;
        var sportsSites = world.Features.Values.Where(feature => feature.Props is { } props &&
            props.TryGetProperty("sourceTags", out var tags) && tags.TryGetProperty("leisure", out var leisure) &&
            leisure.GetString() is "stadium" or "sports_centre").ToArray();
        // The leisure tag also describes buildings such as the gym and swimming hall; only sport-kind enclosures
        // are empty outlines. Buildings keep their footprints regardless of their leisure tag.
        var outlines = sportsSites.Where(feature => feature.Kind.Value == "sport").ToArray();
        AssertThat(outlines.Length > 0).IsTrue();
        AssertThat(outlines.All(outline => shapes.All(shape => shape.Id != outline.Id)))
            .OverrideFailureMessage("Stadium and sports-centre outlines make no ground of their own.").IsTrue();
        foreach (var building in sportsSites.Where(feature => feature.Kind.Value == "building"))
            AssertThat(shapes.Any(shape => shape.Id == building.Id && shape.Kind == SurfaceKind.Building))
                .OverrideFailureMessage($"The sports building {building.Id} keeps its footprint.").IsTrue();
        AssertThat(shapes.Any(shape => shape.Kind == SurfaceKind.Track && shape.Rings is { Length: > 1 }))
            .OverrideFailureMessage("The real running track surrounds an open infield.").IsTrue();
        var pitches = shapes.Where(shape => shape.Kind == SurfaceKind.Turf && shape.Marked).ToArray();
        AssertThat(pitches.Length > 0).IsTrue();
        var layout = GroundLayout.Of(world);
        var pitch = pitches.OrderByDescending(shape => (shape.Max.X - shape.Min.X) * (shape.Max.Y - shape.Min.Y)).First();
        var centre = (pitch.Min + pitch.Max) * 0.5f;
        var half = MathF.Max(pitch.Max.X - pitch.Min.X, pitch.Max.Y - pitch.Min.Y);
        AssertThat(QuadsNear(layout, centre.X, centre.Y, half).Count > 0)
            .OverrideFailureMessage($"The real main pitch {pitch.Id} has white lines.").IsTrue();
    }

    // ---------------------------------------------------------------- helpers

    private static bool IsLawn(GroundSurface surface) => GroundPalette.IsLawn(surface);

    /// <summary>Final ground surface at a point, classified on a 0.25 m lattice of the L0 tile that holds it.</summary>
    private static GroundSurface SurfaceAt(IWorldModel world, float x, float z)
    {
        var layout = GroundLayout.Of(world);
        var key = TileGrid.At(new Vector2(x, z));
        var tx = key.X;
        var tz = key.Z;
        var tile = new GroundTile(tx * TileGrid.BaseEdge, tz * TileGrid.BaseEdge, TileGrid.BaseEdge, TileGrid.BaseEdge / 256f);
        var grid = TerrainGrid.For(FlatTerrain.Default, new TileKey(tx, tz), LodLevel.L0, tile);
        var field = TileField.Build(layout, tile, Section, grid);
        var mesher = new GroundMesher(field, Section, LodLevel.L0, layout, grid, new MeshSink());
        return mesher.SurfaceAt((int)MathF.Round((x - tile.X0) / tile.Cell), (int)MathF.Round((z - tile.Z0) / tile.Cell));
    }

    /// <summary>All marking quads in the 64 m cells that touch the square of the given half size around the point.</summary>
    private static List<RoadMarkings.Quad> QuadsNear(GroundLayout layout, float cx, float cz, float half)
    {
        var markings = layout.Markings(Section);
        var quads = new List<RoadMarkings.Quad>();
        for (var z = (int)MathF.Floor((cz - half) / TileGrid.BaseEdge); z <= (int)MathF.Floor((cz + half) / TileGrid.BaseEdge); z++)
            for (var x = (int)MathF.Floor((cx - half) / TileGrid.BaseEdge); x <= (int)MathF.Floor((cx + half) / TileGrid.BaseEdge); x++)
                quads.AddRange(markings.In(x, z));
        return quads;
    }

    /// <summary>True when the point lies inside one of the quads (they are rectangles, so a projection test is exact).</summary>
    private static bool Covers(List<RoadMarkings.Quad> quads, Vector2 p)
    {
        foreach (var q in quads)
        {
            var along = q.B - q.A;
            var across = q.D - q.A;
            var rel = p - q.A;
            var s = Vector2.Dot(rel, along) / along.LengthSquared();
            var t = Vector2.Dot(rel, across) / across.LengthSquared();
            if (s >= -0.02f && s <= 1.02f && t >= -0.02f && t <= 1.02f) return true;
        }
        return false;
    }

    private sealed record PitchShape(PolygonRings Rings, Vector2 Centre, Vector2 Along, Vector2 Across, float Length, float Width);

    /// <summary>A pitch of the given size centred at (cx, cz), its long axis turned by the given angle from +z.</summary>
    private static PitchShape Pitch(float cx, float cz, float length, float width, float degrees)
    {
        var a = degrees * Angles.DegreesToRadians;
        var along = new Vector2(MathF.Sin(a), MathF.Cos(a));
        var across = new Vector2(along.Y, -along.X);
        var centre = new Vector2(cx, cz);
        Vector2 Corner(float u, float v) => centre + along * (u * length * 0.5f) + across * (v * width * 0.5f);
        return new(new PolygonRings([Corner(-1f, -1f), Corner(1f, -1f), Corner(1f, 1f), Corner(-1f, 1f)]), centre, along, across, length, width);
    }

    /// <summary>A stadium-shaped outline (straights of 2 x halfStraight along z, semicircular ends of the given radius).</summary>
    private static PolygonRings Oval(float cx, float cz, float halfStraight, float radius)
    {
        var points = new List<Vector2>();
        const int arc = 12;
        for (var k = 0; k <= arc; k++)
        {
            var a = MathF.PI * k / arc;
            points.Add(new(cx + radius * MathF.Cos(a), cz + halfStraight + radius * MathF.Sin(a)));
        }
        for (var k = 0; k <= arc; k++)
        {
            var a = MathF.PI + MathF.PI * k / arc;
            points.Add(new(cx + radius * MathF.Cos(a), cz - halfStraight + radius * MathF.Sin(a)));
        }
        return new PolygonRings(points);
    }

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>An entity as the world loader builds one: the entity record carries the OSM tags, its part only the source record.</summary>
    private static WorldFeature Entity(string id, string sourceTags, string partProps, PolygonRings rings)
    {
        var part = new WorldFeaturePart(id.Replace("test:", "osm:", StringComparison.Ordinal), new PolygonGeometry(rings), null, null) { Props = Json(partProps) };
        return WorldFeatureFixture.Create(id, "sport", new PolygonGeometry(rings),
            $$"""{"id":"{{id}}","kind":"sport","sourceTags":{{sourceTags}}}""", FeatureSource.Osm, parts: [part]);
    }
}
