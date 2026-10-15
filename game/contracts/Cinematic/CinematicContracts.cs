using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts.Save;

namespace Campus.Contracts.Cinematic;

public enum CinematicKind : byte { EnterNew, EnterNewShort, EnterContinue, Leave }
// Transit resolves exactly two anchor points and uses UI-NAV 7.1 arc construction.
// Its duration comes from T0/T1 (or ArriveAt), never from an interactive transit timer.
public enum CameraPath : byte { Hold, Bezier, Orbit, Transit }
public enum CameraLookMode : byte { Fixed, Ahead, Target }
public enum WorldCueKind : byte { NewTimeline, RestoreSlot, FlowTo, SetFlow }
public enum MaskKind : byte { Letterbox, CloudCover, Veil }
public enum MaskHoldUntil : byte { Path15s, DestReady, TitleReady }
public enum CaptionStyle : byte { Subtitle, Clock, TitleCard, Place }
public enum ExtraKind : byte { LampCue, Birds }
public enum AudioCueKind : byte
{
    [JsonStringEnumMemberName("title.start")] TitleStart,
    [JsonStringEnumMemberName("title.fadeIn")] TitleFadeIn,
    [JsonStringEnumMemberName("title.fadeOut")] TitleFadeOut,
    Duck,
    [JsonStringEnumMemberName("amb.altitude")] AmbAltitude,
}

// Camera may start after zero. Until its first T0, retain the playback-start
// $current pose, including its orientation and FOV. Subsequent segments remain adjacent.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicDef(
    int Format, string Id, MapId Map, CinematicKind Kind, double Duration,
    IReadOnlyList<CameraSeg> Camera, IReadOnlyList<WorldCue> World,
    IReadOnlyList<MaskSeg> Masks, IReadOnlyList<CaptionCue> Captions,
    IReadOnlyList<ExtraCue> Extras, IReadOnlyList<AudioCue> Audio, CinematicEnd End);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CameraPoint(
    string? Anchor = null, Vector3? P = null, Vector3? In = null, Vector3? Out = null);
[JsonConverter(typeof(CameraLookConverter))]
public sealed record CameraLook(
    CameraLookMode Mode, float? Lead = null, float? PitchBias = null, Vector3? Target = null)
{
    // JSON "target" is either the existing xyz array or a pose anchor string.
    // Keeping Target as Vector3? preserves existing C# callers; exactly one is set.
    [JsonIgnore] public string? TargetAnchor { get; init; }
    public CameraLookSettle? Settle { get; init; }
    // Seconds, measured from the segment start. Blend from the preceding orientation
    // with smootherstep, with the cinematic angular-rate limits still applying.
    public float? BlendIn { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CameraLookSettle(string Anchor, float From = 0.6f);
// From is normalized progress in [T0, ArriveAt ?? T1]. From=0.6 blends the final
// 40% with smootherstep to the anchor's orientation (not its position), without roll.

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FovKey(double T, float? Value = null, string? Anchor = null);
// FOV anchors: $userFov reads the current setting; $dest reads the saved destination
// FOV (falling back to the setting only when the destination Pose.FovDeg is null).

// SYS-TITLE specifies orbit semantics but leaves its JSON keys to the v1 candidate.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CameraOrbit(Vector3 Center, float Radius0, float Radius1,
    float Azimuth0, float Azimuth1, float Height0, float Height1);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CameraSeg(
    double T0, double T1, CameraPath Path, IReadOnlyList<CameraPoint> Points,
    CameraLook Look, IReadOnlyList<FovKey> Fov,
    CinematicEase? Ease = null, double? ArriveAt = null, CameraOrbit? Orbit = null);

[JsonConverter(typeof(CinematicEaseConverter))]
public sealed record CinematicEase(string? Name = null, Vector4? Bezier = null);
public sealed class CinematicEaseConverter : JsonConverter<CinematicEase>
{
    public override CinematicEase Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var name = reader.GetString();
            if (name != "smootherstep") throw new JsonException("Unknown cinematic easing.");
            return new(Name: name);
        }
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected easing name or four control values.");
        Span<float> v = stackalloc float[4];
        for (var i = 0; i < v.Length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number) throw new JsonException("Easing requires four values.");
            v[i] = reader.GetSingle();
        }
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Easing requires exactly four values.");
        return new(Bezier: new Vector4(v[0], v[1], v[2], v[3]));
    }
    public override void Write(Utf8JsonWriter writer, CinematicEase value, JsonSerializerOptions options)
    {
        if (value.Name == "smootherstep" && value.Bezier is null) { writer.WriteStringValue(value.Name); return; }
        if (value.Name is not null || value.Bezier is not { } b) throw new JsonException("Invalid cinematic easing.");
        writer.WriteStartArray();
        writer.WriteNumberValue(b.X); writer.WriteNumberValue(b.Y);
        writer.WriteNumberValue(b.Z); writer.WriteNumberValue(b.W);
        writer.WriteEndArray();
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record WorldCue(double T, WorldCueKind Cue,
    double? Clock = null, int? Rate = null, SlotId? Slot = null);
[method: JsonConstructor]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MaskHold(MaskHoldUntil Until, double MaxWait)
{
    // Retain the original constructor for callers authored against the D0 draft.
    public MaskHold(string until, double maxWait) : this(until switch
    {
        "path15s" => MaskHoldUntil.Path15s,
        "destReady" => MaskHoldUntil.DestReady,
        "titleReady" => MaskHoldUntil.TitleReady,
        _ => throw new ArgumentOutOfRangeException(nameof(until)),
    }, maxWait)
    { }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MaskSeg(double T0, double T1, MaskKind Kind, int? Layer = null, MaskHold? Hold = null)
{
    // [T0,T1) bounds the whole envelope; omitted fades preserve the original hard edges.
    // Full opacity is [T0 + FadeIn, T1 - FadeOut] intersected with [T0,T1).
    // FadeIn + FadeOut <= T1 - T0; all times/fades are finite, fades are nonnegative.
    public double FadeIn { get; init; }
    public double FadeOut { get; init; }
    // Hold pauses timeline time at full cover, for at most MaxWait real seconds.
    // Only cloudCover and veil can hold; letterbox never covers the camera view.
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaptionCue(double T0, double T1, CaptionStyle Style, string Key);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ExtraCue(double T0, double T1, ExtraKind Kind, string Ref, bool Optional = false);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AudioCue(double T, AudioCueKind Cue, float Seconds, float? Db = null, float? Weight = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CinematicEnd(
    CinematicPose Pose, ViewMode? Mode = null, int? Flow = null, bool TitleCard = false, bool FirstGuide = false);

// Anchors retain their literal JSON string form; resolved endpoints retain a Pose object.
[JsonConverter(typeof(CinematicPoseConverter))]
public sealed record CinematicPose(string? Anchor = null, Pose? Value = null);
public sealed class CinematicPoseConverter : JsonConverter<CinematicPose>
{
    public override CinematicPose Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var anchor = reader.GetString();
            if (!CinematicAnchors.IsPoseAnchor(anchor)) throw new JsonException("Unknown cinematic pose anchor.");
            return new(Anchor: anchor);
        }
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected pose anchor or pose object.");
        using var document = JsonDocument.ParseValue(ref reader);
        var comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);
        var allowed = new HashSet<string>(["position", "yawDeg", "pitchDeg", "fovDeg"], comparer);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!allowed.Contains(property.Name)) throw new JsonException($"Unknown pose property: {property.Name}");
            if (!seen.Add(property.Name)) throw new JsonException($"Duplicate pose property: {property.Name}");
        }
        if (!seen.Contains("position") || !seen.Contains("yawDeg") || !seen.Contains("pitchDeg"))
            throw new JsonException("A pose requires position, yawDeg and pitchDeg.");
        return new(Value: document.RootElement.Deserialize<Pose>(options));
    }
    public override void Write(Utf8JsonWriter writer, CinematicPose value, JsonSerializerOptions options)
    {
        if (value.Anchor is { } anchor && value.Value is null)
        {
            if (!CinematicAnchors.IsPoseAnchor(anchor)) throw new JsonException("Unknown cinematic pose anchor.");
            writer.WriteStringValue(anchor);
            return;
        }
        if (value.Anchor is null && value.Value is { } pose) { JsonSerializer.Serialize(writer, pose, options); return; }
        throw new JsonException("A cinematic pose must contain exactly one anchor or pose.");
    }
}
