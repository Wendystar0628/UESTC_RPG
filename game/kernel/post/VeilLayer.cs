using Campus.Contracts;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Post;

/// <summary>
/// Presents the veil (CanvasLayer 5, under the UI): one full-screen ColorRect in the horizon fog colour, plus the
/// frontispiece still of the boot sequence (SYS-TITLE §1.1, §1.3). No shader. WP-01's veil service drives the
/// timeline and the `veil` global; this layer only draws what it is handed.
/// <para>The veil is a global strength of the one shared fog (ART-SKYLIGHT §6.4): with `veil` written by the veil
/// service the 3D fog closes in from far to near, and this flat layer only completes the cover over the last
/// stretch, alpha = smoothstep(0.6, 1, opacity).</para>
/// <para>Frontispiece: the still fades in over 0.3 s on top of the opaque veil (step 1); once it is fully in, the fog
/// layer underneath drops out, so lowering the opacity cross-fades the still straight into the live picture
/// (step 8, the curve is the service's). The texture reference is dropped when the reveal completes.</para>
/// </summary>
[KernelNode("post.veil", Owner = "WP-05")]
public partial class VeilLayer : CanvasLayer
{
    private const float LayerStart = 0.6f;
    private const float StillFadeInSeconds = 0.3f;   // SYS-TITLE §1.1 step 1
    private const double ResampleSeconds = 2.0;      // the palette probe is sampled on change, not every frame

    private readonly N.Vector3[] _horizon = new N.Vector3[24];
    private readonly ColorRect _fog = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TextureRect _still = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,   // height-fixed composition, sides cropped
    };
    private IVeilState? _state;
    private IWorldState? _world;
    private IPaletteProbe? _probe;
    private Season _sampledSeason;
    private double _sinceSample = double.MaxValue;
    private bool _presented;
    private float _opacity;
    private VeilContent _content;
    private float _stillIn;

    public VeilLayer()
    {
        Name = "VeilLayer";
        Layer = 5;
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        _state = Services.Get<IVeilState>();
        _world = Services.Get<IWorldState>();
        _probe = Services.Get<IPaletteProbe>();
        AddChild(_fog);
        AddChild(_still);
        _fog.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _still.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _fog.Visible = false;
        _still.Visible = false;
    }

    /// <summary>
    /// One veil frame from the veil service: the fields of the approved <c>IVeilPresentation.Present(in VeilFrame)</c>
    /// bridge (VeilFrame(Opacity, Content, Frontispiece)), which this node implements once WP-01 lands it.
    /// </summary>
    public void Present(float opacity, VeilContent content, Texture2D? frontispiece)
    {
        if (!float.IsFinite(opacity)) throw new ArgumentOutOfRangeException(nameof(opacity));
        _presented = true;
        _opacity = Mathf.Clamp(opacity, 0f, 1f);
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
        if (_state is null || _world is null || _probe is null) return;
        // Until the veil service presents frames (D0 stage), follow the published veil state, fog only.
        var opacity = _presented ? _opacity : Mathf.Clamp(_state.Opacity, 0f, 1f);
        var hasStill = _presented && _content == VeilContent.Frontispiece && _still.Texture is not null;

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
            var c = HorizonColor(delta);
            _fog.Color = new Color(c.X, c.Y, c.Z, fogAlpha);
        }
        _still.Visible = hasStill;
        if (hasStill) _still.Modulate = new Color(1f, 1f, 1f, stillFull ? opacity : _stillIn);
    }

    // Horizon colour of the current hour from the colour script's probe (same palette as the fog), interpolated
    // over the clock; re-sampled when the season changes or every couple of seconds for weather drift.
    // Moves to the sun-side / anti-sun fog colours by view direction once ColorScriptSnapshot carries them (contract-7).
    private N.Vector3 HorizonColor(double delta)
    {
        _sinceSample += delta;
        if (_world!.SeasonTarget != _sampledSeason || _sinceSample >= ResampleSeconds)
        {
            _probe!.SampleHorizon(_world.SeasonTarget, _world.WeatherNow, _horizon);
            _sampledSeason = _world.SeasonTarget;
            _sinceSample = 0;
        }
        var clock = (float)(_world.Clock % 24.0);
        if (clock < 0f) clock += 24f;
        var hour = (int)clock;
        return N.Vector3.Lerp(_horizon[hour % 24], _horizon[(hour + 1) % 24], clock - hour);
    }
}
