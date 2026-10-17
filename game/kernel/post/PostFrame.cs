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
/// One immutable publication. The mailbox and active callbacks each retain a reference; the last return releases
/// both producer leases, so repeated draws never need a second texture source.
/// </summary>
internal sealed class PostFrameBatch(PostFrame frame, GlobalDataTextureLease palette,
    GlobalDataTextureLease matTable) : IDisposable
{
    private int _references = 1;
    public PostFrame Frame { get; } = frame;
    public GlobalDataTextureLease Palette { get; } = palette;
    public GlobalDataTextureLease MatTable { get; } = matTable;

    // Called only under the mailbox lock while its own reference is still held.
    public void Retain() => Interlocked.Increment(ref _references);

    public void Dispose()
    {
        if (Interlocked.Decrement(ref _references) != 0) return;
        Palette.Dispose();
        MatTable.Dispose();
    }
}

/// <summary>
/// Single-slot main-to-render mailbox for the active camera's PostSky -> PostTransparent sequence.
/// PostSky promotes pending input to current and borrows it for each draw, latching its frame for PostTransparent;
/// Publish cannot change that latch. Current leases survive repeated draws until replacement or cancellation,
/// and active callbacks keep their own references across either operation.
/// </summary>
internal sealed class PostFrameBox
{
    private readonly object _gate = new();
    private PostFrameBatch? _pending, _current;
    private bool _closed;
    private PostFrame _finishFrame;
    private bool _finishValid;

    /// <summary>Main thread: transfers leases even after Close; missing producers cancel pending input.</summary>
    public void Publish(in PostFrame frame, GlobalDataTextureLease? palette, GlobalDataTextureLease? matTable)
    {
        PostFrameBatch? batch = null;
        if (palette is not null && matTable is not null) batch = new PostFrameBatch(frame, palette, matTable);
        else
        {
            palette?.Dispose();
            matTable?.Dispose();
        }
        PostFrameBatch? replaced, cancelled = null;
        lock (_gate)
        {
            replaced = _closed ? batch : _pending;
            if (!_closed)
            {
                _pending = batch;
                if (batch is null)
                {
                    cancelled = _current;
                    _current = null;
                }
            }
        }
        replaced?.Dispose();
        cancelled?.Dispose();
    }

    /// <summary>PostSky: takes the publication and selects the frame shared by both passes.</summary>
    public bool TryTake(out PostFrameBatch batch)
    {
        PostFrameBatch? replaced = null;
        lock (_gate)
        {
            if (_pending is not null)
            {
                replaced = _current;
                _current = _pending;
                _pending = null;
            }
            if (_current is not { } current)
            {
                batch = null!;
                _finishValid = false;
                return false;
            }
            batch = current;
            _finishValid = true;
            batch.Retain();
            _finishFrame = batch.Frame;
        }
        replaced?.Dispose();
        return true;
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

    /// <summary>Stops publication and returns pending/current ownership; active callbacks keep their references.</summary>
    public void Close()
    {
        PostFrameBatch? pending, current;
        lock (_gate)
        {
            _closed = true;
            pending = _pending;
            current = _current;
            _pending = _current = null;
            _finishValid = false;
        }
        pending?.Dispose();
        current?.Dispose();
    }
}
