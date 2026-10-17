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
/// One immutable publication. Taking it transfers both leases to the outline callback, which releases them in finally.
/// </summary>
internal readonly record struct PostFrameBatch(PostFrame Frame, GlobalDataTextureLease? Palette,
    GlobalDataTextureLease? MatTable) : IDisposable
{
    public void Dispose()
    {
        Palette?.Dispose();
        MatTable?.Dispose();
    }
}

/// <summary>
/// Single-slot main-to-render mailbox for the active camera's PostSky -> PostTransparent sequence.
/// PostSky takes the pending frame and leases together and latches only its frame for the next PostTransparent;
/// Publish cannot change that latch. Without a new publication, another PostSky reuses the scalars with fallback
/// textures. Replaced or cancelled publications release only leases the render thread has not taken.
/// </summary>
internal sealed class PostFrameBox
{
    private readonly object _gate = new();
    private PostFrameBatch _latest;
    private bool _valid, _closed;
    private PostFrame _finishFrame;
    private bool _finishValid;

    /// <summary>Main thread: transfers ownership of this frame and its leases, even after Close.</summary>
    public void Publish(in PostFrame frame, GlobalDataTextureLease? palette, GlobalDataTextureLease? matTable)
    {
        var batch = new PostFrameBatch(frame, palette, matTable);
        PostFrameBatch replaced;
        lock (_gate)
        {
            replaced = _closed ? batch : _latest;
            if (!_closed)
            {
                _latest = batch;
                _valid = true;
            }
        }
        replaced.Dispose();
    }

    /// <summary>PostSky: takes the publication and selects the frame shared by both passes.</summary>
    public bool TryTake(out PostFrameBatch batch)
    {
        lock (_gate)
        {
            batch = _latest;
            // Preserve the existing repeated-draw behavior: keep scalars, but never retake owned leases.
            // Without a new publication Outline uses the frame's fallback textures as before.
            _latest = new PostFrameBatch(batch.Frame, null, null);
            _finishValid = _valid;
            if (!_finishValid) return false;
            _finishFrame = batch.Frame;
            return true;
        }
    }

    /// <summary>PostTransparent: consumes only the frame selected by this draw's PostSky, never the pending frame.</summary>
    public bool TryTakeFinish(out PostFrame frame)
    {
        lock (_gate)
        {
            frame = _finishFrame;
            var valid = _finishValid;
            _finishValid = false;
            return valid;
        }
    }

    /// <summary>Stops publication and cancels unclaimed input; taken batches remain owned by their callbacks.</summary>
    public void Close()
    {
        PostFrameBatch cancelled;
        lock (_gate)
        {
            _closed = true;
            cancelled = _latest;
            _latest = default;
            _valid = _finishValid = false;
        }
        cancelled.Dispose();
    }
}
