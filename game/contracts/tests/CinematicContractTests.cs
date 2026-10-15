using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Cinematic;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class CinematicContractTests
{
    [TestCase]
    public void LookTargetsKeepBothLegacyAndAnchorWireShapes()
    {
        var legacy = RoundTrip<CameraLook>("""{"mode":"target","target":[1,2,3]}""");
        if (legacy.Target != new Vector3(1, 2, 3) || legacy.TargetAnchor is not null)
            throw new InvalidOperationException("Legacy vector target changed.");
        var anchored = RoundTrip<CameraLook>("""{"mode":"target","target":"$dest","blendIn":0.4,"settle":{"anchor":"$dest","from":0.6}}""");
        if (anchored.Target is not null || anchored.TargetAnchor != "$dest" || anchored.BlendIn != 0.4f || anchored.Settle != new CameraLookSettle("$dest", 0.6f))
            throw new InvalidOperationException("Anchor look failed to round-trip.");
        ExpectJsonError(() => JsonSerializer.Deserialize<CameraLook>("""{"mode":"fixed","blendIn":1e999}""", ContractJson.Options));
        ExpectJsonError(() => JsonSerializer.Serialize(new CameraLook(CameraLookMode.Fixed) { BlendIn = float.PositiveInfinity }, ContractJson.Options));
    }

    [TestCase]
    public void MaskEnumAndFullCoverBoundariesRemainExplicit()
    {
        var mask = RoundTrip<MaskSeg>("""{"t0":0.2,"t1":2.6,"kind":"veil","fadeIn":0.8,"fadeOut":0.4,"hold":{"until":"titleReady","maxWait":2}}""");
        if (mask.Hold?.Until != MaskHoldUntil.TitleReady || new MaskHold("titleReady", 2) != mask.Hold)
            throw new InvalidOperationException("Hold enum or legacy constructor changed.");
        var timeline = Timeline(mask);
        if (CinematicTiming.IsFullyCoveredAt(timeline, 0.9) || !CinematicTiming.IsFullyCoveredAt(timeline, 1.0) ||
            CinematicTiming.IsFullyCoveredAt(timeline, 2.3) || CinematicTiming.IsFullyCoveredAt(timeline, 2.6))
            throw new InvalidOperationException("Fade ramps or the removed endpoint were treated as fully covered.");
        if (CinematicTiming.IsFullyCoveredAt(Timeline(new MaskSeg(0, 2, MaskKind.Letterbox)), 1) ||
            CinematicTiming.IsFullyCoveredAt(Timeline(new MaskSeg(0, 2, MaskKind.Veil)), 2))
            throw new InvalidOperationException("Letterbox or exclusive T1 granted a cut exemption.");
        foreach (var invalid in new[] { mask with { FadeIn = -1 }, mask with { FadeOut = double.NaN }, mask with { FadeIn = 3 } })
        {
            try { CinematicTiming.IsFullyCoveredAt(Timeline(invalid), 1); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("Invalid mask granted a cut exemption.");
        }
    }

    [TestCase]
    public void SupplementalRecordsRoundTripTheirDiscriminatorsAndVectors()
    {
        var extras = RoundTrip<CinematicExtrasFile>("""
            {"format":1,"map":"example","refs":{
              "lights":{"kind":"lampCue","ref":"lights","group":"firstLight","route":[[0,0],[1,2]],"band":30,"front":[[0,0],[1,2]],"release":2},
              "birds":{"kind":"birds","ref":"birds","asset":"egret","count":7,"start":[0,2,3],"heading":90,"speed":14,"climb":0.8,"spread":[14,4,10],"flapStep":"t12","fade":[0.4,0.4]}}}
            """);
        if (extras.Refs["lights"] is not LampCueDef { Route.Count: 2 } || extras.Refs["birds"] is not BirdFlockDef { Count: 7, FlapStep: BirdFlapStep.T12 })
            throw new InvalidOperationException("Extra kinds lost their typed payloads.");
        var clouds = RoundTrip<CinematicCloudLayout>("""
            {"format":1,"map":"example","name":"example","shader":"res://style/shaders/cine_cloud.gdshader",
             "camVeil":{"offset":[0,0,-0.5],"scale":[4,1.2]},"layers":[{"layer":0,"cards":[{
               "p":[1,2,3],"size":[4,5],"faceYaw":192,"axisX":[1,0,0],"axisZ":[0,0,1],"custom":[0.1,0.2,0.3,0.4]}]}]}
            """);
        if (clouds.Layers[0].Cards[0].Custom != new Vector4(.1f, .2f, .3f, .4f))
            throw new InvalidOperationException("Cloud card vectors changed wire shape.");
    }

    [TestCase]
    public void FullTimelineComposesLookEaseAndEndpointConverters()
    {
        const string anchoredJson = """
            {"format":1,"id":"test","map":"example","kind":"enterContinue","duration":3,
             "camera":[{"t0":0,"t1":3,"path":"transit","points":[{"anchor":"$current"},{"anchor":"$dest"}],
               "look":{"mode":"target","target":"$dest","settle":{"anchor":"$dest","from":0.6}},
               "fov":[{"t":0,"value":38},{"t":3,"anchor":"$dest"}],"ease":"smootherstep"}],
             "world":[],"masks":[],"captions":[],"extras":[],"audio":[],"end":{"pose":"$dest"}}
            """;
        var anchored = RoundTrip<CinematicDef>(anchoredJson);
        if (anchored.Camera[0].Look.TargetAnchor != "$dest" || anchored.Camera[0].Ease?.Name != "smootherstep" || anchored.End.Pose.Anchor != "$dest")
            throw new InvalidOperationException("Timeline converter composition lost anchor or easing values.");
        var resolvedJson = anchoredJson.Replace("\"ease\":\"smootherstep\"", "\"ease\":[0.4,0,0.6,1]", StringComparison.Ordinal)
            .Replace("\"pose\":\"$dest\"", "\"pose\":{\"position\":[1,2,3],\"yawDeg\":45,\"pitchDeg\":-10,\"fovDeg\":50}", StringComparison.Ordinal);
        var resolved = RoundTrip<CinematicDef>(resolvedJson);
        if (resolved.End.Pose.Value != new Pose(new Vector3(1, 2, 3), 45, -10, 50) || resolved.Camera[0].Ease?.Bezier != new Vector4(.4f, 0, .6f, 1))
            throw new InvalidOperationException("Resolved endpoint or Bezier easing failed to round-trip inside a timeline.");
        ExpectJsonError(() => JsonSerializer.Deserialize<CinematicDef>(resolvedJson.Replace("\"yawDeg\":45", "\"unexpected\":0,\"yawDeg\":45", StringComparison.Ordinal), ContractJson.Options));
        ExpectJsonError(() => JsonSerializer.Deserialize<CinematicDef>(anchoredJson.Replace("\"mode\":\"target\"", "\"mode\":\"target\",\"unexpected\":0", StringComparison.Ordinal), ContractJson.Options));
        ExpectJsonError(() => JsonSerializer.Deserialize<CinematicDef>(anchoredJson.Replace("\"ease\":\"smootherstep\"", "\"ease\":{\"unexpected\":0}", StringComparison.Ordinal), ContractJson.Options));
    }

    private static T RoundTrip<T>(string json)
    {
        var value = JsonSerializer.Deserialize<T>(json, ContractJson.Options) ?? throw new JsonException("Null fixture.");
        var written = JsonSerializer.Serialize(value, ContractJson.Options);
        return JsonSerializer.Deserialize<T>(written, ContractJson.Options) ?? throw new JsonException("Null round-trip.");
    }

    private static void ExpectJsonError(Action action)
    {
        try { action(); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Invalid look data was accepted.");
    }

    private static CinematicDef Timeline(MaskSeg mask) => new(1, "test", new MapId("example"), CinematicKind.Leave,
        3, [], [], [mask], [], [], [], new CinematicEnd(new CinematicPose(Anchor: "$current")));
}
