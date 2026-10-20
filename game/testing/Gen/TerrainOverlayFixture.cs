using System.Text;
using System.Text.Json.Nodes;

namespace Campus.Contracts.Tests;

internal static class TerrainOverlayFixture
{
    internal static JsonObject DryFixture()
    {
        var root = JsonNode.Parse(Fixture)!.AsObject();
        var channel = root["ops"]![5]!;
        channel["dry"] = true; channel["water"] = null; channel["waterY"] = null;
        channel["weirs"] = new JsonArray();
        channel["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4","outer":"1:6"}""");
        return root;
    }

    internal static byte[] InvalidCandidate(Action<JsonObject> mutate, bool dry = false)
    {
        var root = dry ? DryFixture() : JsonNode.Parse(Fixture)!.AsObject();
        // Establish that each negative case starts from a valid document. Otherwise an unrelated omitted
        // collection failure could make every rejection assertion pass without testing the intended mutation.
        _ = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        mutate(root);
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    // Synthetic wire fixtures only: they are not campus layout data and do not pass entity/topology validation.
    internal const string TwoStepFixture = """
        {
          "zone": "fixture",
          "ops": [
            {"op":"channel","id":"design:terrain:fixture-a","zone":"fixture","reason":"Synthetic upper constant-level water segment","water":"design:water:fixture-a","waterY":-0.5,"depth":1.2,"bank":{"kind":"hard","slope":"1:4"},"line":{"points":[[-4,0],[0,0]]},"width":2,"weirs":[]},
            {"op":"channel","id":"design:terrain:fixture-b","zone":"fixture","reason":"Synthetic independent middle water segment and two real-structure references","water":"design:water:fixture-b","waterY":-0.85,"depth":1.2,"bank":{"kind":"hard","slope":"1:4"},"line":{"points":[[0,0],[1.5,0]]},"width":2,"weirs":[{"s":0,"drop":0.35,"kind":"weir","structure":"design:weir:fixture-ab","upstream":"design:water:fixture-a","downstream":"design:water:fixture-b"},{"s":1.5,"drop":0.35,"kind":"weir","structure":"design:weir:fixture-bc","upstream":"design:water:fixture-b","downstream":"design:water:fixture-c"}]},
            {"op":"channel","id":"design:terrain:fixture-c","zone":"fixture","reason":"Synthetic lower constant-level water segment","water":"design:water:fixture-c","waterY":-1.2,"depth":1.2,"bank":{"kind":"hard","slope":"1:4"},"line":{"points":[[1.5,0],[6,0]]},"width":2,"weirs":[]}
          ]
        }
        """;

    internal const string Fixture = """
        {
          "zone": "fixture",
          "ops": [
            {"op":"platform","id":"design:terrain:fixture-platform","zone":"fixture","reason":"Synthetic DTO boundary fixture","rings":{"outer":[[0,0],[10,0],[10,10],[0,10]],"holes":[[[2,2],[3,2],[2,3]]]},"y":0.8,"side":{"fill":"1:3","cut":"1:3"},"fillet":2},
            {"op":"mound","id":"design:terrain:fixture-mound","zone":"fixture","reason":"Synthetic DTO boundary fixture","shape":{"shape":"circle","center":[0,0],"radius":9},"height":1},
            {"op":"basin","id":"design:terrain:fixture-basin","zone":"fixture","reason":"Synthetic DTO boundary fixture","shape":{"shape":"ellipse","center":[0,0],"radii":[9,12],"yawDeg":90},"height":1},
            {"op":"slope","id":"design:terrain:fixture-slope","zone":"fixture","reason":"Synthetic DTO boundary fixture","geometry":{"type":"polygon","rings":{"outer":[[0,0],[10,0],[10,4],[0,4]],"holes":[]}},"y0":0,"y1":0.5,"crossFall":0.01,"side":{"fill":"1:3","cut":"1:3"},"fillet":2,"from":[0,0],"to":[10,0]},
            {"op":"terrace","id":"design:terrain:fixture-terrace","zone":"fixture","reason":"Synthetic DTO boundary fixture","front":{"points":[[0,0],[10,0]]},"inward":[0,1],"frontY":0,"tiers":2,"riser":0.5,"tread":2,"stairAt":[]},
            {"op":"channel","id":"design:terrain:fixture-channel","zone":"fixture","reason":"Synthetic DTO boundary fixture","water":"design:water:upstream","depth":1.2,"bank":{"kind":"soft","slope":"1:5"},"waterY":-0.5,"weirs":[{"s":4,"drop":0.4,"kind":"weir","structure":"design:weir:fixture","upstream":"design:water:upstream","downstream":"design:water:downstream"}],"line":{"points":[[0,0],[10,0]]},"width":2},
            {"op":"lake","id":"design:terrain:fixture-lake","zone":"fixture","reason":"Synthetic DTO boundary fixture","water":"design:water:downstream","waterY":-0.9,"bank":{"kind":"soft","slope":"1:5"},"bed":{"maxDepth":1.4},"wade":[]},
            {"op":"pad","id":"design:terrain:fixture-pad","zone":"fixture","reason":"Synthetic DTO boundary fixture","feature":"design:building:fixture","grade":4,"edge":{"structureId":"design:wall:fixture","kind":"edgeBand","riser":0.5,"tread":2,"tiers":1}},
            {"op":"corridor","id":"design:terrain:fixture-corridor","zone":"fixture","reason":"Synthetic DTO boundary fixture","road":"design:road:fixture","anchors":[{"s":0,"y":0},{"s":10,"y":0.2}],"crossFall":0.015,"shoulder":0.75}
          ]
        }
        """;
}
