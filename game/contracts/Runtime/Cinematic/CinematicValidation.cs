namespace Campus.Contracts.Cinematic;

/// <summary>Runtime-independent format and timeline invariants. Geometry checks use the loaded world separately.</summary>
public static class CinematicValidation
{
    public static void Validate(CinematicDef definition, MapId map)
    {
        Require(definition.Format == 1, "Unsupported cinematic format.");
        Require(definition.Map == map, "Cinematic belongs to a different map.");
        Require(double.IsFinite(definition.Duration) && definition.Duration > 0, "Invalid duration.");
        Require(definition.Camera.Count > 0, "Camera track is empty.");
        double previous = definition.Camera[0].T0;
        foreach (var segment in definition.Camera)
        {
            Range(segment.T0, segment.T1, definition.Duration);
            Require(Math.Abs(segment.T0 - previous) < 0.000001, "Camera segments must be adjacent.");
            Require(segment.ArriveAt is null || segment.ArriveAt > segment.T0 && segment.ArriveAt <= segment.T1, "Invalid arriveAt.");
            switch (segment.Path)
            {
                case CameraPath.Hold: Require(segment.Points.Count == 1, "Hold needs one point."); break;
                case CameraPath.Bezier: Require(segment.Points.Count >= 2, "Bezier needs two points."); break;
                case CameraPath.Transit:
                    Require(segment.Points.Count == 2 && segment.Points.All(p => p.Anchor is not null && p.In is null && p.Out is null), "Transit needs two anchor points without handles.");
                    break;
                case CameraPath.Orbit: Require(segment.Orbit is not null, "Orbit parameters are absent."); break;
                default: throw new InvalidDataException("Unknown camera path.");
            }
            foreach (var point in segment.Points)
            {
                Require((point.Anchor is null) != (point.P is null), "Camera point needs exactly one position source.");
                if (point.Anchor is not null) Require(CinematicAnchors.IsPoseAnchor(point.Anchor), "Invalid point anchor.");
                if (point.P is { } p) Require(Finite(p), "Non-finite camera point.");
                if (point.In is { } incoming) Require(Finite(incoming), "Non-finite incoming handle.");
                if (point.Out is { } outgoing) Require(Finite(outgoing), "Non-finite outgoing handle.");
            }
            Require(segment.Fov.Count > 0, "FOV track is empty.");
            double keyTime = -1;
            foreach (var key in segment.Fov)
            {
                Require(double.IsFinite(key.T) && key.T >= segment.T0 && key.T <= segment.T1 && key.T > keyTime, "FOV keys must be ordered within their segment.");
                Require((key.Value is null) != (key.Anchor is null), "FOV needs exactly one value source.");
                if (key.Value is { } value) Require(float.IsFinite(value) && value > 0 && value < 180, "Invalid FOV.");
                else Require(key.Anchor is "$dest" or "$userFov", "Invalid FOV anchor.");
                keyTime = key.T;
            }
            _ = segment.Look.Mode switch
            {
                CameraLookMode.Fixed or CameraLookMode.Ahead or CameraLookMode.Target => 0,
                _ => throw new InvalidDataException("Unknown look mode."),
            };
            if (segment.Look.Mode == CameraLookMode.Target) Require((segment.Look.Target is null) != (segment.Look.TargetAnchor is null), "Target look needs exactly one target.");
            if (segment.Look.BlendIn is { } blend) Require(float.IsFinite(blend) && blend >= 0, "Invalid look blend.");
            if (segment.Look.Settle is { } settle) Require(float.IsFinite(settle.From) && settle.From >= 0 && settle.From < 1 && CinematicAnchors.IsPoseAnchor(settle.Anchor), "Invalid settle.");
            if (segment.Ease?.Bezier is { } easing) Require(float.IsFinite(easing.X) && float.IsFinite(easing.Y) && float.IsFinite(easing.Z) && float.IsFinite(easing.W) && easing.X >= 0 && easing.X <= 1 && easing.Z >= 0 && easing.Z <= 1, "Invalid easing control points.");
            previous = segment.T1;
        }
        Require(Math.Abs(previous - definition.Duration) < 0.000001, "Camera track does not end at duration.");
        foreach (var mask in definition.Masks)
        {
            Range(mask.T0, mask.T1, definition.Duration);
            Require(mask.FadeIn >= 0 && mask.FadeOut >= 0 && double.IsFinite(mask.FadeIn) && double.IsFinite(mask.FadeOut) && mask.FadeIn + mask.FadeOut <= mask.T1 - mask.T0, "Invalid mask fades.");
            switch (mask.Kind)
            {
                case MaskKind.Letterbox: Require(mask.Hold is null, "Letterbox cannot hold."); break;
                case MaskKind.CloudCover: Require(mask.T1 - mask.T0 - mask.FadeIn - mask.FadeOut <= 0.300001, "Cloud full cover exceeds 0.3 seconds."); break;
                case MaskKind.Veil: break;
                default: throw new InvalidDataException("Unknown mask.");
            }
            if (mask.Hold is { } hold)
            {
                Require(double.IsFinite(hold.MaxWait) && hold.MaxWait >= 0, "Invalid hold timeout.");
                _ = hold.Until switch { MaskHoldUntil.Path15s or MaskHoldUntil.DestReady or MaskHoldUntil.TitleReady => 0, _ => throw new InvalidDataException("Unknown hold condition.") };
            }
        }
        double cueTime = -1;
        foreach (var cue in definition.World)
        {
            Require(double.IsFinite(cue.T) && cue.T >= 0 && cue.T <= definition.Duration && cue.T >= cueTime, "World cues must be ordered.");
            switch (cue.Cue)
            {
                case WorldCueKind.NewTimeline:
                case WorldCueKind.RestoreSlot: Require(CinematicTiming.IsFullyCoveredAt(definition, cue.T), "World replacement must be fully covered."); break;
                case WorldCueKind.FlowTo: Require(cue.Clock is >= 0 and < 24, "Invalid flow destination."); ValidRate(cue.Rate); break;
                case WorldCueKind.SetFlow: ValidRate(cue.Rate); break;
                default: throw new InvalidDataException("Unknown world cue.");
            }
            cueTime = cue.T;
        }
        foreach (var caption in definition.Captions) { Range(caption.T0, caption.T1, definition.Duration); Require(!string.IsNullOrWhiteSpace(caption.Key), "Caption key is empty."); }
        foreach (var extra in definition.Extras) Range(extra.T0, extra.T1, definition.Duration);
        foreach (var cue in definition.Audio)
        {
            Require(double.IsFinite(cue.T) && cue.T >= 0 && cue.T <= definition.Duration && float.IsFinite(cue.Seconds) && cue.Seconds >= 0, "Invalid audio cue timing.");
            _ = cue.Cue switch
            {
                AudioCueKind.TitleStart or AudioCueKind.TitleFadeIn or AudioCueKind.TitleFadeOut or AudioCueKind.AmbAltitude => 0,
                AudioCueKind.Duck when cue.Db.HasValue => 0,
                _ => throw new InvalidDataException("Unknown or incomplete audio cue."),
            };
        }
    }
    private static void ValidRate(int? rate) => Require(rate is 0 or 1 or 10 or 60 or 360 or 1440, "Invalid world flow rate.");
    private static bool Finite(System.Numerics.Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
    private static void Range(double from, double to, double duration) => Require(double.IsFinite(from) && double.IsFinite(to) && from >= 0 && to > from && to <= duration, "Invalid track interval.");
    private static void Require(bool value, string reason) { if (!value) throw new InvalidDataException(reason); }
}
