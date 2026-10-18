using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts.Cinematic;

// Authoring data in cinematics/extras.json. Every dictionary key must equal Ref.
// These effects only exist during cinematic playback and never become save state.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicExtrasFile(int Format, MapId Map, IReadOnlyDictionary<string, CinematicExtraDef> Refs);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(LampCueDef), "lampCue")]
[JsonDerivedType(typeof(BirdFlockDef), "birds")]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public abstract record CinematicExtraDef(string Ref);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LampCueDef(
    string Ref, string Group, IReadOnlyList<Vector2> Route, float Band,
    IReadOnlyList<Vector2> Front, double Release) : CinematicExtraDef(Ref);
// Route: ordered (x,z) points in metres. Front: ordered (timeline seconds, route
// arc length in metres) pairs. Release removes the timing override, not final light values.

public enum BirdFlapStep : byte { T12 }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BirdFlockDef(
    string Ref, string Asset, int Count, Vector3 Start, float Heading, float Speed,
    float Climb, Vector3 Spread, BirdFlapStep FlapStep, Vector2 Fade) : CinematicExtraDef(Ref);
// Heading is degrees, Speed and Climb are metres/second; Fade is (in,out) seconds.

// Authoring data in cinematics/clouds/<layout>.json. Shader must be whitelisted.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicCloudLayout(
    int Format, MapId Map, string Name, string Shader, CinematicCameraVeil CamVeil,
    IReadOnlyList<CinematicCloudLayer> Layers);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicCameraVeil(Vector3 Offset, Vector2 Scale);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicCloudLayer(int Layer, IReadOnlyList<CinematicCloudCard> Cards);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicCloudCard(
    Vector3 P, Vector2 Size, float FaceYaw, Vector3 AxisX, Vector3 AxisZ, Vector4 Custom);
