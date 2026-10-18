using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// The smooth outer shell of one crown (max-011 review, 2026-10-16): every crown mass and every leaf card takes its normal
/// from this one surface, so the whole crown shades as a single lit side and one clean shadow band instead of a pile of
/// separately shaded lumps. Card and mixed crowns use the plan's crown envelope (an ellipsoid blended towards a cone by
/// the form's taper) fitted to the masses; cloud crowns use the smooth union of their balls, pulled a little towards that
/// envelope so the large clouds still read as one crown. The shell is decided once per mother model and shared by all
/// levels and the shadow proxy, so changing level never changes the shading.
/// </summary>
internal sealed class CrownShell
{
    /// <summary>Smooth-union blend width of cloud balls, as a multiple of the mean ball radius.</summary>
    public const float CloudSmooth = 1.0f;

    /// <summary>How far cloud-crown normals lean from the ball union towards the crown ellipsoid (0 union … 1 ellipsoid).</summary>
    public const float CloudEllipsoid = 0.3f;

    private readonly Vector3 _center;
    private readonly Vector3 _radii;
    private readonly float[] _profile;
    private readonly (Vector3 Center, float Radius)[] _balls;
    private readonly float _smooth;
    private readonly float _step;

    public CrownShell(TreePlan plan)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var b in plan.Blobs)
        {
            min = Vector3.Min(min, b.Center - new Vector3(b.Radius));
            max = Vector3.Max(max, b.Center + new Vector3(b.Radius));
        }
        _center = (min + max) * 0.5f;
        var half = Vector3.Max((max - min) * 0.5f, new Vector3(0.1f));
        // The horizontal profile follows the plan's crown envelope (ellipse blended towards a cone by the form's taper),
        // as a ratio to the plain ellipse, scaled so its widest point meets the masses' extent.
        _profile = new float[ProfileSteps + 1];
        var widest = 0f;
        for (var i = 0; i <= ProfileSteps; i++)
        {
            var t = -1f + 2f * i / ProfileSteps;
            var e = plan.Envelope(t) / MathF.Max(1e-3f, plan.CrownRx);
            widest = MathF.Max(widest, e);
            _profile[i] = Math.Clamp(e / MathF.Max(0.05f, MathF.Sqrt(MathF.Max(0f, 1f - t * t))), 0.2f, 3f);
        }
        _radii = new Vector3(half.X / MathF.Max(0.2f, widest), half.Y, half.Z / MathF.Max(0.2f, widest));
        Diameter = 2f * MathF.Max(half.X, half.Z);
        _step = MathF.Max(0.02f, Diameter * 0.01f);
        if (plan.Species.Crown == CrownType.Cloud && plan.Form.Id != "palm")
        {
            _balls = plan.Blobs.Select(b => (b.Center, b.Radius)).ToArray();
            _smooth = CloudSmooth * plan.Blobs.Average(b => b.Radius);
        }
        else _balls = [];
    }

    /// <summary>Crown diameter (horizontal), the yardstick of the 5% card rule.</summary>
    public float Diameter { get; }

    public Vector3 Center => _center;

    private const int ProfileSteps = 40;

    /// <summary>Horizontal scale of the envelope at relative height t ∈ [−1, 1], relative to the plain ellipse.</summary>
    private float Profile(float t)
    {
        var x = (Math.Clamp(t, -1f, 1f) + 1f) * 0.5f * ProfileSteps;
        var i = Math.Min((int)x, ProfileSteps - 1);
        return ScalarMath.Lerp(_profile[i], _profile[i + 1], x - i);
    }

    /// <summary>Envelope field: &lt; 0 inside, 0 on the shell (not a true distance; only its gradient is used).</summary>
    private float Ellipsoid(Vector3 p)
    {
        var d = p - _center;
        var s = Profile(d.Y / _radii.Y);
        var q = new Vector3(d.X / (_radii.X * s), d.Y / _radii.Y, d.Z / (_radii.Z * s));
        return (q.Length() - 1f) * MathF.Min(_radii.X, MathF.Min(_radii.Y, _radii.Z));
    }

    /// <summary>Smooth union of the cloud balls (polynomial smooth minimum, balls in plan order).</summary>
    private float Union(Vector3 p)
    {
        var f = float.MaxValue;
        foreach (var (c, r) in _balls)
        {
            var d = Vector3.Distance(p, c) - r;
            if (f == float.MaxValue) { f = d; continue; }
            var h = MathF.Max(_smooth - MathF.Abs(f - d), 0f) / _smooth;
            f = MathF.Min(f, d) - h * h * _smooth * 0.25f;
        }
        return f;
    }

    /// <summary>Signed depth below the shell (positive inside), in metres for cloud crowns and roughly so for ellipsoids.</summary>
    public float Depth(Vector3 p) => -(_balls.Length > 0 ? Union(p) : Ellipsoid(p));

    /// <summary>Depth below the fitted ellipsoid only (a conservative distance to it), used to size hole fillers.</summary>
    public float EllipsoidDepth(Vector3 p) => -Ellipsoid(p);

    /// <summary>Shell normal at p: the normalised gradient by central differences.</summary>
    public Vector3 Normal(Vector3 p)
    {
        var e = Gradient(Ellipsoid, p);
        if (_balls.Length == 0) return e;
        var u = Gradient(Union, p);
        return Vector3.Normalize(Vector3.Lerp(u, e, CloudEllipsoid) + new Vector3(0f, 1e-5f, 0f));
    }

    private Vector3 Gradient(Func<Vector3, float> f, Vector3 p)
    {
        var h = _step;
        var g = new Vector3(
            f(p + new Vector3(h, 0f, 0f)) - f(p - new Vector3(h, 0f, 0f)),
            f(p + new Vector3(0f, h, 0f)) - f(p - new Vector3(0f, h, 0f)),
            f(p + new Vector3(0f, 0f, h)) - f(p - new Vector3(0f, 0f, h)));
        if (g.LengthSquared() > 1e-12f) return Vector3.Normalize(g);
        var r = p - _center;
        return r.LengthSquared() > 1e-8f ? Vector3.Normalize(r) : Vector3.UnitY;
    }

    /// <summary>Point on the fitted envelope for unit direction d (d.y is the relative height), used to aim probes and card rays.</summary>
    public Vector3 EllipsoidPoint(Vector3 d)
    {
        var s = Profile(d.Y);
        return _center + new Vector3(d.X * _radii.X * s, d.Y * _radii.Y, d.Z * _radii.Z * s);
    }
}
