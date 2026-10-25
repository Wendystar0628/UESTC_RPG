using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Post;

/// <summary>
/// Everything the render-thread passes need for one frame. Gathered once per frame on the main thread
/// (<see cref="PostChain"/>, FramePriority.Systems) from published C# state — never by reading shader globals back
/// (ENG-PERF) — and handed to the CompositorEffects through <see cref="PostFrameBox"/>.
/// </summary>
internal struct PostFrame
{
    public long RenderGeneration;
    // Outline (post_outline.glsl)
    public float LineDarken;      // IColorScriptFrame.LineDarkness
    public float Flash;           // lightning flash 0..1: line_darken x 0.7 at full flash (SYS-WEATHER)
    public Vector3 KeyDir;        // world direction toward the key light (sun by day, moon by night)
    public float KeyStrength;
    // Copies of the global shader parameters read by fog.glslinc (same values as the globals)
    public Vector4 Fog;
    public Vector4 FogLow;
    public float SeasonAtmosphere;
    public Vector4 WeatherFog;
    public Vector4 MapRect;
    public Vector4 Medium;
    public Color MediumColor;     // linear
    public float EdgeFog;
    public Vector3 SunDir;        // sun_dir: real sun direction (sky side of the fog colour)
    public Vector3 MoonDir;       // moon_dir
    // Finish pass and LUT (versions finish and lut of post_outline.glsl)
    public float VignetteK;       // after the quality fade
    public Vector3 VignetteTint;
    public Vector3 CdlSlope;
    public Vector3 CdlOffset;
    public Vector3 CdlPower;
    public float CdlSaturation;
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
    private long _generation;
    private RenderDimensions? _dimensions;
    private bool _dimensionsActive;

    public RenderDimensions? Dimensions { get { lock (_gate) return _dimensions; } }

    public void PublishDimensions(long generation, in RenderDimensions dimensions)
    {
        lock (_gate)
            if (!_closed && _dimensionsActive && generation == _generation) _dimensions = dimensions;
    }

    // Observation validity is independent of existing post inputs and texture leases.
    public void InvalidateDimensions(bool? active = null)
    {
        lock (_gate)
        {
            _generation++;
            _dimensions = null;
            if (active is { } enabled) _dimensionsActive = enabled;
        }
    }

    /// <summary>Main thread: transfers leases even after Close; missing producers cancel pending input.</summary>
    public void Publish(in PostFrame frame, GlobalDataTextureLease? palette, GlobalDataTextureLease? matTable)
    {
        PostFrameBatch? batch = null;
        if (palette is null || matTable is null)
        {
            palette?.Dispose();
            matTable?.Dispose();
        }
        PostFrameBatch? replaced, cancelled = null;
        lock (_gate)
        {
            if (palette is not null && matTable is not null)
            {
                var published = frame;
                published.RenderGeneration = _generation;
                batch = new PostFrameBatch(published, palette, matTable);
            }
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
            _generation++;
            _dimensions = null;
            pending = _pending;
            current = _current;
            _pending = _current = null;
            _finishValid = false;
        }
        pending?.Dispose();
        current?.Dispose();
    }
}
