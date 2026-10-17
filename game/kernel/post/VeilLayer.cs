using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Post;

/// <summary>
/// Presents the veil (IVeilPresentation; CanvasLayer 5, under the UI): a full-screen horizontal gradient in the
/// horizon fog colour, plus the frontispiece still of the boot sequence (SYS-TITLE §1.1, §1.3). No shader.
/// WP-01's veil service owns timing and content and writes the `veil` global; this layer only draws the frames
/// it is handed through <see cref="Present(in VeilFrame)"/>, and draws nothing until the first one.
/// <para>The veil is a global strength of the one shared fog (ART-SKYLIGHT §6.4): with `veil` written by the veil
/// service the 3D fog closes in from far to near, and this layer only completes the cover over the last stretch,
/// alpha = smoothstep(0.6, 1, opacity). Its colour follows the fog: across the screen each column takes
/// mix(FogAnti, FogSun, side weight) by its view azimuth, with the side rule of fog.gdshaderinc (sky_side_weight).</para>
/// <para>Frontispiece: the still fades in over 0.3 s on top of the opaque veil (step 1); once it is fully in, the fog
/// gradient underneath drops out, so lowering the opacity cross-fades the still straight into the live picture
/// (step 8, the curve is the service's). The texture reference is dropped when the reveal completes.</para>
/// </summary>
[KernelNode("post.veil", Owner = "WP-05")]
[KernelService(typeof(IVeilPresentation), Owner = "WP-05")]
public partial class VeilLayer : CanvasLayer, IVeilPresentation
{
    private const float LayerStart = 0.6f;
    private const float StillFadeInSeconds = 0.3f;   // SYS-TITLE §1.1 step 1
    private const int Stops = 9;                     // gradient stops across the screen width
    // Side rule mirrored from fog.gdshaderinc (SKY_SIDE_SUN_DEG, SKY_SIDE_ANTI_DEG, SKY_NIGHT_SIDE_FROM / _TO).
    private const float SideSunDeg = 60f;
    private const float SideAntiDeg = 90f;
    private const float NightSideFromDeg = -6f;
    private const float NightSideToDeg = -13f;

    private readonly Gradient _gradient = new();
    private readonly TextureRect _fog = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
    };
    private readonly TextureRect _still = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,   // height-fixed composition, sides cropped
    };
    private readonly N.Vector3[] _horizon = new N.Vector3[24];
    private IColorScriptFrame? _colorScript;
    private IWorldState? _world;
    private IPaletteProbe? _probe;
    private Season _probedSeason;
    private double _sinceProbe = double.MaxValue;
    private bool _presented;
    private float _opacity;
    private VeilContent _content;
    private float _stillIn;
    private bool _covered;
    private bool _drawnOpaque;
    private Vector2 _drawnSize;

    public VeilLayer()
    {
        Name = "VeilLayer";
        Layer = 5;
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = FramePriority.Hud;
        var offsets = new float[Stops];
        var colors = new Color[Stops];
        for (var i = 0; i < Stops; i++)
        {
            offsets[i] = i / (Stops - 1f);
            colors[i] = Colors.White;
        }
        _gradient.Offsets = offsets;
        _gradient.Colors = colors;
        _fog.Texture = new GradientTexture2D
        {
            Gradient = _gradient,
            Width = 128,
            Height = 1,
            FillFrom = Vector2.Zero,
            FillTo = new Vector2(1f, 0f),
        };
    }

    /// <summary>True only while what this layer drew in the last frame covers the whole viewport fully opaque.</summary>
    public bool IsCovered => _drawnOpaque && CoversViewport() && _drawnSize == GetViewport().GetVisibleRect().Size;

    internal void Configure(IColorScriptFrame colors, IWorldState world, IPaletteProbe probe)
    {
        if (IsInsideTree()) throw new InvalidOperationException("Configure veil presentation before mounting it.");
        _colorScript = colors;
        _world = world;
        _probe = probe;
    }

    public override void _Ready()
    {
        _colorScript ??= Services.Get<IColorScriptFrame>();
        _world ??= Services.Get<IWorldState>();
        _probe ??= Services.Get<IPaletteProbe>();
        AddChild(_fog);
        AddChild(_still);
        _fog.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _still.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _fog.Visible = false;
        _still.Visible = false;
        RenderingServer.Singleton.FramePostDraw += OnFrameDrawn;
    }

    public void Present(in VeilFrame frame) => Present(frame.Opacity, frame.Content, frame.Frontispiece);

    private void Present(float opacity, VeilContent content, Texture2D? frontispiece)
    {
        if (!float.IsFinite(opacity)) throw new ArgumentOutOfRangeException(nameof(opacity));
        _presented = true;
        _opacity = Mathf.Clamp(opacity, 0f, 1f);
        if (_opacity < 1f) _drawnOpaque = false;
        _content = content;
        switch (content)
        {
            case VeilContent.Fog:
                DropStill();
                break;
            case VeilContent.Frontispiece:
                if (frontispiece is null) DropStill();
                else if (_still.Texture != frontispiece)
                {
                    _still.Texture = frontispiece;
                    _stillIn = 0f;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(content), content, "Unknown veil content");
        }
    }

    private void DropStill()
    {
        _still.Texture = null;
        _still.Visible = false;
        _stillIn = 0f;
    }

    public override void _Process(double delta)
    {
        if (_colorScript is null || _world is null || _probe is null || !_presented)
        {
            _covered = false;
            return;
        }
        var opacity = _opacity;
        var hasStill = _content == VeilContent.Frontispiece && _still.Texture is not null;
        if (hasStill)
        {
            _stillIn = Mathf.MoveToward(_stillIn, 1f, (float)delta / StillFadeInSeconds);
            if (opacity <= 0f && _stillIn >= 1f)
            {
                DropStill();
                hasStill = false;
            }
        }
        var stillFull = hasStill && _stillIn >= 1f;
        var fogAlpha = stillFull ? 0f : Mathf.SmoothStep(LayerStart, 1f, opacity);
        _fog.Visible = fogAlpha > 0f;
        if (_fog.Visible)
        {
            UpdateGradient(delta);
            _fog.Modulate = new Color(1f, 1f, 1f, fogAlpha);
        }
        var stillAlpha = stillFull ? opacity : _stillIn;
        _still.Visible = hasStill;
        if (hasStill) _still.Modulate = new Color(1f, 1f, 1f, stillAlpha);
        _covered = fogAlpha >= 1f || (hasStill && stillAlpha >= 1f);
    }

    private bool CoversViewport()
    {
        if (!IsInsideTree() || !Visible || !_covered) return false;
        var viewport = GetViewport().GetVisibleRect();
        return viewport.Size.X > 0f && viewport.Size.Y > 0f && (Covers(_fog) || Covers(_still));

        bool Covers(TextureRect layer)
        {
            if (!layer.IsVisibleInTree() || layer.Texture is null || layer.Modulate.A < 1f || layer.SelfModulate.A < 1f)
                return false;
            var actual = layer.GetGlobalRect();
            return actual.Position.X <= viewport.Position.X && actual.Position.Y <= viewport.Position.Y &&
                actual.End.X >= viewport.End.X && actual.End.Y >= viewport.End.Y;
        }
    }

    private void OnFrameDrawn()
    {
        _drawnOpaque = CoversViewport();
        _drawnSize = GetViewport().GetVisibleRect().Size;
    }

    public override void _ExitTree() => RenderingServer.Singleton.FramePostDraw -= OnFrameDrawn;

    private void UpdateGradient(double delta)
    {
        var snapshot = _colorScript!.Current;
        var sun = new Vector3(snapshot.FogSun.X, snapshot.FogSun.Y, snapshot.FogSun.Z);
        var anti = new Vector3(snapshot.FogAnti.X, snapshot.FogAnti.Y, snapshot.FogAnti.Z);
        if (sun == Vector3.Zero && anti == Vector3.Zero)
        {
            // Fog colours not published yet by the colour script: one horizon colour from the probe.
            var flat = ProbeHorizon(delta);
            for (var i = 0; i < Stops; i++) _gradient.SetColor(i, flat);
            return;
        }
        var camera = GetViewport().GetCamera3D();
        var sunDir = _world!.Sun.Dir;
        var moonDir = _world.Moon.Dir;
        var nightK = 1f - Mathf.SmoothStep(NightSideToDeg, NightSideFromDeg, Mathf.RadToDeg(Mathf.Asin(Mathf.Clamp(sunDir.Y, -1f, 1f))));
        Vector3 forward = Vector3.Forward, right = Vector3.Right;
        var tanHalf = 1f;
        if (camera is not null)
        {
            var basis = camera.GlobalTransform.Basis;
            forward = -basis.Z;
            right = basis.X;
            var size = GetViewport().GetVisibleRect().Size;
            tanHalf = Mathf.Tan(Mathf.DegToRad(camera.Fov) * 0.5f) * (size.Y > 0f ? size.X / size.Y : 1f);
        }
        for (var i = 0; i < Stops; i++)
        {
            var x = i / (Stops - 1f) * 2f - 1f;
            var dir = forward + right * (x * tanHalf);
            var w = Mathf.Lerp(SideOf(dir, sunDir.X, sunDir.Z), SideOf(dir, moonDir.X, moonDir.Z), nightK);
            var c = anti.Lerp(sun, w);
            _gradient.SetColor(i, new Color(c.X, c.Y, c.Z).LinearToSrgb());
        }
    }

    // sRGB horizon colour of the current hour from IPaletteProbe, interpolated over the clock; re-sampled when the
    // season changes or every couple of seconds for weather drift.
    private Color ProbeHorizon(double delta)
    {
        _sinceProbe += delta;
        if (_world!.SeasonTarget != _probedSeason || _sinceProbe >= 2.0)
        {
            _probe!.SampleHorizon(_world.SeasonTarget, _world.WeatherNow, _horizon);
            _probedSeason = _world.SeasonTarget;
            _sinceProbe = 0;
        }
        var clock = (float)(_world.Clock % 24.0);
        if (clock < 0f) clock += 24f;
        var hour = (int)clock;
        var c = N.Vector3.Lerp(_horizon[hour % 24], _horizon[(hour + 1) % 24], clock - hour);
        return new Color(c.X, c.Y, c.Z);
    }

    // Weight of the body's side for a view direction by horizontal azimuth difference (fog.gdshaderinc sky_side_of).
    private static float SideOf(Vector3 dir, float bodyX, float bodyZ)
    {
        var v = new Vector2(dir.X, dir.Z);
        var b = new Vector2(bodyX, bodyZ);
        if (v.Length() < 1e-4f || b.Length() < 1e-4f) return 0.5f;
        var angle = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(v.Normalized().Dot(b.Normalized()), -1f, 1f)));
        return 1f - Mathf.SmoothStep(SideSunDeg, SideAntiDeg, angle);
    }
}
