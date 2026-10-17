using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Flow;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace Campus.Camera.Cinematic;

/// <summary>Baked camera paths; all engine curve allocation happens before playback.</summary>
public sealed class CinematicTrack : IDisposable
{
    private readonly CinematicDef _definition;
    private readonly CinematicAnchorSet _anchors;
    private readonly Curve3D?[] _curves;
    private readonly float[] _lengths;
    private int _segment = -1;
    private Pose _segmentStart;
    public CinematicTrack(CinematicDef definition, CinematicAnchorSet anchors, ISpatialIndex spatial)
    {
        CinematicValidation.Validate(definition, definition.Map);
        _definition = definition; _anchors = anchors; _segmentStart = anchors.Current;
        _curves = new Curve3D?[definition.Camera.Count]; _lengths = new float[_curves.Length];
        for (var i = 0; i < _curves.Length; i++)
        {
            var segment = definition.Camera[i];
            switch (segment.Path)
            {
                case CameraPath.Hold: break;
                case CameraPath.Orbit: break;
                case CameraPath.Bezier:
                    var curve = new Curve3D { BakeInterval = FlowTuning.CurveBakeInterval };
                    foreach (var point in segment.Points) curve.AddPoint(ToGodot(Point(point)), ToGodot(point.In ?? Vector3.Zero), ToGodot(point.Out ?? Vector3.Zero));
                    _curves[i] = curve; _lengths[i] = curve.GetBakedLength();
                    break;
                case CameraPath.Transit:
                    var from = Point(segment.Points[0]); var to = Point(segment.Points[1]);
                    var roof = Math.Max(from.Y, to.Y);
                    for (var s = 0; s <= FlowTuning.TransitSamples; s++)
                    {
                        var p = Vector3.Lerp(from, to, (float)s / FlowTuning.TransitSamples);
                        roof = Math.Max(roof, (spatial.SupportY(p.X, p.Z, float.MaxValue) ?? spatial.GroundY(p.X, p.Z)) + FlowTuning.TransitClearance);
                    }
                    var apex = roof + Vector3.Distance(from, to) * FlowTuning.TransitHeightFactor;
                    var transit = new Curve3D { BakeInterval = FlowTuning.CurveBakeInterval };
                    // A single cubic whose control points lift the arc, with a vertical destination tangent.
                    var firstHandle = Vector3.Lerp(from, to, 0.35f) with { Y = apex };
                    var lastHandle = to with { Y = apex };
                    transit.AddPoint(ToGodot(from), null, ToGodot(firstHandle - from));
                    transit.AddPoint(ToGodot(to), ToGodot(lastHandle - to));
                    _curves[i] = transit; _lengths[i] = transit.GetBakedLength();
                    break;
                default: throw new InvalidDataException("Unknown camera path.");
            }
        }
    }
    public Pose Sample(double time, Pose previous, double delta)
    {
        var index = SegmentAt(time);
        if (index < 0) return _anchors.Current;
        var segment = _definition.Camera[index];
        if (_segment != index) { _segment = index; _segmentStart = previous; }
        var t = Progress(segment, time);
        var position = Position(index, time);
        var desired = segment.Look.Mode switch
        {
            CameraLookMode.Fixed => segment.Points.Count > 0 && segment.Points[0].Anchor is { } anchor
                ? _anchors.Resolve(anchor) with { Position = position } : _segmentStart with { Position = position },
            CameraLookMode.Ahead => CinematicMath.Face(position, Ahead(index, time, segment.Look.Lead ?? 50), previous),
            CameraLookMode.Target => CinematicMath.Face(position, segment.Look.Target ?? _anchors.Resolve(segment.Look.TargetAnchor!).Position, previous),
            _ => throw new InvalidDataException("Unknown look mode."),
        };
        desired = desired with { PitchDeg = desired.PitchDeg + (segment.Look.PitchBias ?? 0) };
        if (segment.Look.Settle is { } settle && t >= settle.From)
            desired = CinematicMath.BlendOrientation(desired, _anchors.Resolve(settle.Anchor) with { Position = position }, CinematicMath.Smooth((t - settle.From) / (1 - settle.From)));
        if (segment.Look.BlendIn is > 0)
            desired = CinematicMath.BlendOrientation(_segmentStart, desired, CinematicMath.Smooth((float)((time - segment.T0) / segment.Look.BlendIn.Value)));
        if (!CinematicMath.FullyCovered(_definition, time)) desired = desired with
        {
            YawDeg = previous.YawDeg + Math.Clamp(CinematicMath.AngleDelta(previous.YawDeg, desired.YawDeg), -FlowTuning.MaxYawRate * (float)delta, FlowTuning.MaxYawRate * (float)delta),
            PitchDeg = previous.PitchDeg + Math.Clamp(desired.PitchDeg - previous.PitchDeg, -FlowTuning.MaxPitchRate * (float)delta, FlowTuning.MaxPitchRate * (float)delta),
        };
        return desired with { Position = position, FovDeg = Fov(segment, time) };
    }
    public Vector3 PositionAt(double time) { var index = SegmentAt(time); return index < 0 ? _anchors.Current.Position : Position(index, time); }
    private int SegmentAt(double time)
    {
        for (var i = 0; i < _definition.Camera.Count; i++)
            if (time >= _definition.Camera[i].T0 && (time < _definition.Camera[i].T1 || i == _definition.Camera.Count - 1)) return i;
        return -1;
    }
    private static float Progress(CameraSeg segment, double time) => Math.Clamp((float)((time - segment.T0) / ((segment.ArriveAt ?? segment.T1) - segment.T0)), 0, 1);
    private Vector3 Position(int index, double time)
    {
        var segment = _definition.Camera[index]; var progress = CinematicMath.Ease(Progress(segment, time), segment.Ease);
        switch (segment.Path)
        {
            case CameraPath.Hold: return Point(segment.Points[0]);
            case CameraPath.Bezier:
            case CameraPath.Transit: return ToNumerics(_curves[index]!.SampleBaked(_lengths[index] * progress, cubic: true));
            case CameraPath.Orbit:
                var orbit = segment.Orbit!;
                var radius = orbit.Radius0 + (orbit.Radius1 - orbit.Radius0) * progress;
                var angle = (orbit.Azimuth0 + (orbit.Azimuth1 - orbit.Azimuth0) * progress) * float.Pi / 180;
                return orbit.Center + new Vector3(MathF.Sin(angle) * radius, orbit.Height0 + (orbit.Height1 - orbit.Height0) * progress, -MathF.Cos(angle) * radius);
            default: throw new InvalidDataException("Unknown camera path.");
        }
    }
    private Vector3 Ahead(int index, double time, float lead)
    {
        var segment = _definition.Camera[index];
        if (_curves[index] is { } curve)
        {
            var distance = _lengths[index] * CinematicMath.Ease(Progress(segment, time), segment.Ease);
            var target = ToNumerics(curve.SampleBaked(Math.Min(distance + lead, _lengths[index]), cubic: true));
            if (Vector3.DistanceSquared(target, Position(index, time)) > 0.0001f) return target;
            return target + target - ToNumerics(curve.SampleBaked(Math.Max(0, _lengths[index] - Math.Max(lead, 0.5f)), cubic: true));
        }
        var current = Position(index, time);
        var ahead = Position(index, Math.Min(segment.T1, time + 0.05));
        return Vector3.DistanceSquared(current, ahead) > 0.0001f ? ahead : current + current - Position(index, Math.Max(segment.T0, time - 0.05));
    }
    private float Fov(CameraSeg segment, double time)
    {
        var from = segment.Fov[0];
        for (var i = 0; i < segment.Fov.Count; i++)
        {
            var to = segment.Fov[i];
            if (to.T > time)
            {
                var blend = to.T > from.T ? CinematicMath.Smooth((float)((time - from.T) / (to.T - from.T))) : 0;
                return _anchors.Resolve(from) + (_anchors.Resolve(to) - _anchors.Resolve(from)) * blend;
            }
            from = to;
        }
        return _anchors.Resolve(from);
    }
    private Vector3 Point(CameraPoint point) => point.P ?? _anchors.Resolve(point.Anchor!).Position;
    private static Godot.Vector3 ToGodot(Vector3 p) => new(p.X, p.Y, p.Z);
    private static Vector3 ToNumerics(Godot.Vector3 p) => new(p.X, p.Y, p.Z);
    public void Dispose() { foreach (var curve in _curves) curve?.Dispose(); }
}
