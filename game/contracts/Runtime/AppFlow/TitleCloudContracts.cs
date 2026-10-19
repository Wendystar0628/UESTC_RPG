using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts.AppFlow;

// Authoring data in title-clouds.json. Shader must be whitelisted.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TitleCloudLayout(
    int Format, MapId Map, string Name, string Shader,
    IReadOnlyList<TitleCloudLayer> Layers);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TitleCloudLayer(int Layer, IReadOnlyList<TitleCloudCard> Cards);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TitleCloudCard(
    Vector3 P, Vector2 Size, float FaceYaw, Vector3 AxisX, Vector3 AxisZ, Vector4 Custom);
