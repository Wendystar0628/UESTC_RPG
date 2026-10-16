using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainOverlayContractTests
{
    [TestCase]
    public void AllNineOperationsRoundTripWithCanonicalDefaultsAndMetadataFirst()
    {
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(Fixture));
        if (((TerrainChannelOp)parsed.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainCorridorOp)parsed.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Omitted optional collections must retain their empty defaults.");
        var expected = new[] { 300, 100, 100, 200, 300, 50, 50, 50, 50 };
        if (parsed.Ops.Count != expected.Length || !parsed.Ops.Select(op => op.Priority).SequenceEqual(expected))
            throw new InvalidOperationException("Concrete priority defaults changed.");
        var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
        using var document = JsonDocument.Parse(encoded);
        foreach (var op in document.RootElement.GetProperty("ops").EnumerateArray())
            if (op.EnumerateObject().First().Name != "op") throw new InvalidOperationException("Operation metadata must be first.");
        var roundTrip = TerrainOverlayJson.Parse(encoded);
        if (((TerrainChannelOp)roundTrip.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainCorridorOp)roundTrip.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Empty optional collections failed to round-trip.");
        if (roundTrip.Ops[0] is not TerrainPlatformOp { Rings.Holes.Count: 1 } ||
            roundTrip.Ops[6] is not TerrainLakeOp { Bed.Shelf: "1:7" } ||
            roundTrip.Ops[7] is not TerrainPadOp { Apron: 0.8, PadY: null })
            throw new InvalidOperationException("Polygon holes or explicit normative defaults were lost.");
        var channel = (TerrainChannelOp)roundTrip.Ops[5];
        if (channel.Water != channel.Weirs[0].Upstream || channel.Weirs[0].Upstream == channel.Weirs[0].Downstream ||
            channel.Weirs[0].Structure != new FeatureId("design:weir:fixture"))
            throw new InvalidOperationException("A channel lost its real structure/water-segment references.");
    }

    [TestCase]
    public void ExplicitPriorityAndPolylineSlopeRemainAuthoredInputs()
    {
        var root = JsonNode.Parse(Fixture)!.AsObject();
        var ops = root["ops"]!.AsArray();
        ops[0]!["priority"] = 1; // Overrides the type default; no hidden platform-after-mound sort is applied here.
        var slope = ops[3]!.AsObject();
        slope["geometry"] = JsonNode.Parse("""{"type":"polyline","line":{"points":[[0,0],[10,0]]}}""");
        slope.Remove("from"); slope.Remove("to"); slope["width"] = 2;
        ops[1]!["priority"] = int.MaxValue;
        ops[5]!["crossings"] = new JsonArray();
        ops[8]!["steps"] = new JsonArray();
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        if (parsed.Ops[0].Priority != 1 || parsed.Ops[1].Priority != int.MaxValue ||
            parsed.Ops[3] is not TerrainSlopeOp { Geometry: PolylineGeometry, Width: 2, From: null, To: null })
            throw new InvalidOperationException("Authored priority or valid polyline slope changed.");
        if (((TerrainChannelOp)parsed.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainCorridorOp)parsed.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Explicit empty arrays are valid and must not become null.");
    }

    [TestCase]
    public void DryChannelsAreExplicitAndStepsRetainTheirRealStructureReference()
    {
        var root = DryFixture();
        var channel = root["ops"]![5]!;
        channel["crossings"] = JsonNode.Parse("""[{"road":"design:road:underpass","at":[4,0]},{"road":"design:road:stop","endBefore":true}]""");
        root["ops"]![8]!["steps"] = JsonNode.Parse("""[{"s0":3,"s1":5,"structure":"design:stairs:fixture"}]""");
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
        var roundTrip = TerrainOverlayJson.Parse(encoded);
        if (roundTrip.Ops[5] is not TerrainChannelOp { Dry: true, Water: null, WaterY: null, Line: not null, Width: 2 })
            throw new InvalidOperationException("A dry channel acquired water or lost its explicit line.");
        var dryChannel = (TerrainChannelOp)roundTrip.Ops[5];
        if (dryChannel.Bank is not { Kind: TerrainBankKind.Swale, Slope: null, Inner: "1:4", Outer: "1:6" } ||
            dryChannel.Crossings is not [TerrainUnderpassCrossing { At.X: 4, At.Y: 0 }, TerrainEndBeforeCrossing])
            throw new InvalidOperationException("Symmetric swale or crossing alternatives were conflated.");
        var steps = ((TerrainCorridorOp)roundTrip.Ops[8]).Steps;
        if (steps.Count != 1 || steps[0].Structure != new FeatureId("design:stairs:fixture"))
            throw new InvalidOperationException("A steps interval lost its real stair reference.");
        using var wire = JsonDocument.Parse(encoded);
        var dry = wire.RootElement.GetProperty("ops")[5];
        if (dry.GetProperty("water").ValueKind != JsonValueKind.Null || dry.GetProperty("waterY").ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException("Required null fields must round-trip explicitly despite global null omission.");
        var crossings = dry.GetProperty("crossings");
        if (crossings[0].TryGetProperty("endBefore", out _) || crossings[1].TryGetProperty("at", out _) ||
            !crossings[1].GetProperty("endBefore").GetBoolean())
            throw new InvalidOperationException("Crossing JSON must preserve exactly one locator form.");
    }

    [TestCase]
    public void ZeroOrOmittedSwaleOuterCanonicalizesWithoutCreatingAnOuterSlope()
    {
        foreach (var omit in new[] { false, true })
        {
            var root = DryFixture();
            var bank = root["ops"]![5]!["bank"]!.AsObject();
            if (omit) bank.Remove("outer"); else bank["outer"] = 0;
            var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
            var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
            var roundTrip = TerrainOverlayJson.Parse(encoded);
            if (((TerrainChannelOp)roundTrip.Ops[5]).Bank.Outer is not null)
                throw new InvalidOperationException("An absent outer segment acquired a slope.");
            using var wire = JsonDocument.Parse(encoded);
            if (wire.RootElement.GetProperty("ops")[5].GetProperty("bank").TryGetProperty("outer", out _))
                throw new InvalidOperationException("Canonical no-outer form must omit the optional field.");
        }
    }

    [TestCase]
    public void UnknownOrIncompleteContractsFailRatherThanCreatePlausibleTerrain()
    {
        Reject(root => root["unapproved"] = 1);
        Reject(root => root["ops"]![1]!["op"] = "noise");
        Reject(root => root["ops"]![0]!.AsObject().Remove("op"));
        Reject(root => root["ops"]![1]!["shape"]!.AsObject().Remove("shape"));
        Reject(root => root["ops"]![1]!["unapproved"] = 1);
        Reject(root => root["ops"]![0]!["id"] = "design:terrain:probe\n");
        Reject(root => root["ops"]![0]!["side"]!["fill"] = "1:3\n");
        Reject(root => root["ops"]![1]!["shape"]!["shape"] = "polygon");
        Reject(root => root["ops"]![1]!["profile"] = "noise");
        Reject(root => root["ops"]![1]!["profile"] = "hermite,hermite");
        Reject(root => root["ops"]![6]!["bank"]!["kind"] = "soft,steps");
        Reject(root => root["ops"]![5]!["weirs"]![0]!["kind"] = "weir,gate");
        Reject(root => root["ops"]![7]!["edge"]!["kind"] = "edgeBand,terrace");
        Reject(root => root["ops"]![0]!.AsObject().Remove("fillet"));
        Reject(root => root["ops"]![0]!["priority"] = -1);
        Reject(root => root["ops"]![0]!["priority"] = 2147483648L);
        Reject(root => root["ops"]![5]!.AsObject().Remove("water"));
        Reject(root => root["ops"]![5]!["waterY"] = null);
        Reject(root => root["ops"]![5]!["dry"] = true);
        Reject(root => root["ops"]![5]!["crossings"] = null);
        Reject(root => root["ops"]![5]!["crossings"] = null, dry: true);
        Reject(root => root["ops"]![8]!["steps"] = null);
        Reject(root => root["ops"]![5]!["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4"}"""));
        Reject(root => root["ops"]![6]!["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4"}"""));
        Reject(root => root["ops"]![5]!["crossings"] = JsonNode.Parse("""[{"road":"design:road:fixture","at":[4,0]}]"""));
        Reject(root => root["ops"]![5]!["bank"] = JsonNode.Parse("""{"kind":"soft","slope":"1:4"}"""), dry: true);
        Reject(root => root["ops"]![5]!["bank"]!["outer"] = 1, dry: true);
        Reject(root => root["ops"]![5]!["bank"]!["outer"] = null, dry: true);
        Reject(root => root["ops"]![5]!["bank"]!["outer"] = "0", dry: true);
        Reject(root => root["ops"]![5]!["bank"]!["left"] = "1:4", dry: true);
        Reject(root => root["ops"]![5]!["bank"]!["slope"] = "1:4", dry: true);
        Reject(root => root["ops"]![5]!["bank"]!.AsObject().Remove("inner"), dry: true);
        foreach (var invalidCrossing in new[]
        {
            """{"road":"design:road:fixture","at":[4,0],"endBefore":true}""",
            """{"road":"design:road:fixture","endBefore":false}""",
            """{"road":"design:road:fixture"}""",
            """{"at":[4,0]}""",
            """{"road":"design:road:fixture","at":null}""",
            """{"road":"design:road:fixture","at":[4,0],"roadWidth":2}"""
        })
            Reject(root => root["ops"]![5]!["crossings"] = new JsonArray(JsonNode.Parse(invalidCrossing)), dry: true);
        Reject(root => root["ops"]![8]!["steps"] = JsonNode.Parse("""[{"s0":3,"s1":5}]"""));
        Reject(root => root["ops"]![5]!["weirs"]![0]!["downstream"] = "design:water:upstream");
        Reject(root => root["ops"]![8]!["anchors"]!.AsArray().RemoveAt(1));
        Reject(root =>
        {
            root["ops"]![3]!["geometry"] = JsonNode.Parse("""{"type":"point","position":[0,0,0]}""");
        });
        Reject(root =>
        {
            var op = root["ops"]![0]!.AsObject();
            op.Remove("op"); op["op"] = "platform"; // Metadata moved after ordinary fields.
        });
    }

    private static JsonObject DryFixture()
    {
        var root = JsonNode.Parse(Fixture)!.AsObject();
        var channel = root["ops"]![5]!;
        channel["dry"] = true; channel["water"] = null; channel["waterY"] = null;
        channel["weirs"] = new JsonArray();
        channel["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4","outer":"1:6"}""");
        return root;
    }

    private static void Reject(Action<JsonObject> mutate, bool dry = false)
    {
        var root = dry ? DryFixture() : JsonNode.Parse(Fixture)!.AsObject();
        // Establish that each negative case starts from a valid document. Otherwise an unrelated omitted
        // collection failure could make every rejection assertion pass without testing the intended mutation.
        _ = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        mutate(root);
        try { TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString())); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Invalid terrain candidate was accepted.");
    }

    // Synthetic wire fixtures only: they are not campus layout data and do not pass entity/topology validation.
    private const string Fixture = """
        {
          "zone": "fixture",
          "owner": "WP-00",
          "ops": [
            {
              "op": "platform",
              "id": "design:terrain:fixture-platform",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "rings": {
                "outer": [
                  [
                    0,
                    0
                  ],
                  [
                    10,
                    0
                  ],
                  [
                    10,
                    10
                  ],
                  [
                    0,
                    10
                  ]
                ],
                "holes": [
                  [
                    [
                      2,
                      2
                    ],
                    [
                      3,
                      2
                    ],
                    [
                      2,
                      3
                    ]
                  ]
                ]
              },
              "y": 0.8,
              "side": {
                "fill": "1:3",
                "cut": "1:3"
              },
              "fillet": 2
            },
            {
              "op": "mound",
              "id": "design:terrain:fixture-mound",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "shape": {
                "shape": "circle",
                "center": [
                  0,
                  0
                ],
                "radius": 9
              },
              "height": 1
            },
            {
              "op": "basin",
              "id": "design:terrain:fixture-basin",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "shape": {
                "shape": "ellipse",
                "center": [
                  0,
                  0
                ],
                "radii": [
                  9,
                  12
                ],
                "yawDeg": 90
              },
              "height": 1
            },
            {
              "op": "slope",
              "id": "design:terrain:fixture-slope",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "geometry": {
                "type": "polygon",
                "rings": {
                  "outer": [
                    [
                      0,
                      0
                    ],
                    [
                      10,
                      0
                    ],
                    [
                      10,
                      4
                    ],
                    [
                      0,
                      4
                    ]
                  ],
                  "holes": []
                }
              },
              "y0": 0,
              "y1": 0.5,
              "crossFall": 0.01,
              "side": {
                "fill": "1:3",
                "cut": "1:3"
              },
              "fillet": 2,
              "from": [
                0,
                0
              ],
              "to": [
                10,
                0
              ]
            },
            {
              "op": "terrace",
              "id": "design:terrain:fixture-terrace",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "front": {
                "points": [
                  [
                    0,
                    0
                  ],
                  [
                    10,
                    0
                  ]
                ]
              },
              "inward": [
                0,
                1
              ],
              "frontY": 0,
              "tiers": 2,
              "riser": 0.5,
              "tread": 2,
              "stairAt": []
            },
            {
              "op": "channel",
              "id": "design:terrain:fixture-channel",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "water": "design:water:upstream",
              "depth": 1.2,
              "bank": {
                "kind": "soft",
                "slope": "1:5"
              },
              "waterY": -0.5,
              "weirs": [
                {
                  "s": 4,
                  "drop": 0.4,
                  "kind": "weir",
                  "structure": "design:weir:fixture",
                  "upstream": "design:water:upstream",
                  "downstream": "design:water:downstream"
                }
              ],
              "line": {
                "points": [
                  [
                    0,
                    0
                  ],
                  [
                    10,
                    0
                  ]
                ]
              },
              "width": 2
            },
            {
              "op": "lake",
              "id": "design:terrain:fixture-lake",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "water": "design:water:downstream",
              "waterY": -0.9,
              "bank": {
                "kind": "soft",
                "slope": "1:5"
              },
              "bed": {
                "maxDepth": 1.4
              },
              "wade": []
            },
            {
              "op": "pad",
              "id": "design:terrain:fixture-pad",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "feature": "design:building:fixture",
              "grade": 4,
              "edge": {
                "structureId": "design:wall:fixture",
                "kind": "edgeBand",
                "riser": 0.5,
                "tread": 2,
                "tiers": 1
              }
            },
            {
              "op": "corridor",
              "id": "design:terrain:fixture-corridor",
              "zone": "fixture",
              "reason": "Synthetic DTO boundary fixture",
              "road": "design:road:fixture",
              "anchors": [
                {
                  "s": 0,
                  "y": 0
                },
                {
                  "s": 10,
                  "y": 0.2
                }
              ],
              "crossFall": 0.015,
              "shoulder": 0.75
            }
          ]
        }
        """;
}
