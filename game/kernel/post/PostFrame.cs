using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Post;

/// <summary>
/// Everything the render-thread passes need for one frame. Gathered once per frame on the main thread
/// (<see cref="PostChain"/>, FramePriority.Systems) from published C# state — never by reading shader globals back
/// (ENG-PERF §1.5) — and handed to the CompositorEffects through <see cref="PostFrameBox"/>.
/// </summary>
internal struct PostFrame
{
    // Outline (post_outline.glsl)
    public float LineDarken;      // IColorScriptFrame.LineDarkness
    public float TentMix;         // 0 by default (ENG-PERF §10.2: lines on the foreground side, tent filter off)
    public float Flash;           // lightning flash 0..1: line_darken x 0.7 at full flash (SYS-WEATHER §5)
    public float FarRain;         // far rain band amount (reserved until the rain band lands)
    public Vector3 KeyDir;        // world direction toward the key light (sun by day, moon by night)
    public float KeyStrength;
    public Color RimColor;        // linear
    public float RimStrength;     // 0 = screen-space rim off (mainline)
    public int DebugView;         // 0 = off; 1 mask, 2 ID, 3 normal, 4 world, 5 fog amount
    // Copies of the global shader parameters read by fog.glslinc (same values as the globals)
    public Vector4 Fog;
    public Vector4 FogLow;
    public Vector4 WeatherFog;
    public Vector4 MapRect;
    public Vector4 Medium;
    public Color MediumColor;     // linear
    public float EdgeFog;
    public float Veil;
    public Vector3 SunDir;        // sun_dir: real sun direction (sky side of the fog colour)
    public Vector3 MoonDir;       // moon_dir
    // Stand-in data textures (RenderingServer RIDs), used only while the registry has no producer for a name
    public Rid FallbackPalette;
    public Rid FallbackMatTable;
    // Finish pass and LUT (versions tiltshift_comp and lut of post_outline.glsl)
    public float TiltAmount;      // 0 until the tilt-shift blur dispatches land
    public float VignetteK;       // after the quality fade
    public Vector3 VignetteTint;
    public Vector3 CdlSlope;
    public Vector3 CdlOffset;
    public Vector3 CdlPower;
    public float CdlSaturation;
    // Diagnostics (PostDiagSink): filled by the main thread only while the switch is on, otherwise 0.
    public long DiagWorldInstance;   // IWorldState.Instance when the frame was gathered
    public ulong DiagMainFrame;      // Engine.GetProcessFrames() when the frame was gathered
}

/// <summary>
/// Single-slot mailbox between the main thread and the render thread; struct copies, no allocation. It also carries
/// the per-frame read leases of the global data textures (IGlobalDataTextureRegistry): the main thread acquires them
/// before the frame is queued, the outline callback takes them over, uses them and disposes them after issuing its
/// commands. Leases the render thread never took are disposed by the main thread when the next pair replaces them.
/// </summary>
internal sealed class PostFrameBox
{
    private readonly object _gate = new();
    private PostFrame _frame;
    private bool _valid;
    private GlobalDataTextureLease? _palette;
    private GlobalDataTextureLease? _matTable;

    public void Write(in PostFrame frame)
    {
        lock (_gate)
        {
            _frame = frame;
            _valid = true;
        }
    }

    public bool TryRead(out PostFrame frame)
    {
        lock (_gate)
        {
            frame = _frame;
            return _valid;
        }
    }

    /// <summary>Main thread: hands over this frame's leases (either may be null) and releases unclaimed older ones.</summary>
    public void OfferLeases(GlobalDataTextureLease? palette, GlobalDataTextureLease? matTable)
    {
        GlobalDataTextureLease? oldPalette, oldMatTable;
        lock (_gate)
        {
            oldPalette = _palette;
            oldMatTable = _matTable;
            _palette = palette;
            _matTable = matTable;
        }
        oldPalette?.Dispose();
        oldMatTable?.Dispose();
    }

    /// <summary>Render thread: takes ownership of the current leases; the caller disposes them.</summary>
    public void TakeLeases(out GlobalDataTextureLease? palette, out GlobalDataTextureLease? matTable)
    {
        lock (_gate)
        {
            palette = _palette;
            matTable = _matTable;
            _palette = null;
            _matTable = null;
        }
    }
}
