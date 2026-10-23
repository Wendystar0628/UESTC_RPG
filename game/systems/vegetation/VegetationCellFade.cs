using Campus.Contracts;
using Campus.Contracts.Quality;

namespace Campus.Systems.Vegetation;

/// <summary>Ready owner-cell handoff; reverse requests wait for the current bounded segment to finish.</summary>
internal sealed class VegetationCellFade
{
    private enum Phase : byte { Inactive, Entering, Ready, Exiting }
    private Phase _phase;
    private float _started;
    public bool Active => _phase != Phase.Inactive;
    public bool Transitioning => _phase is Phase.Entering or Phase.Exiting;
    public float Code => _phase switch
    {
        Phase.Entering => VegetationNearReadyEncoding.EncodeStart(_started, true),
        Phase.Exiting => VegetationNearReadyEncoding.EncodeStart(_started, false),
        _ => 0f
    };

    public void Update(float distance, float clock)
    {
        if (Transitioning)
        {
            if (VegetationBuckets.Elapsed(clock, _started) < RenderFade.Seconds) return;
            _phase = _phase == Phase.Entering ? Phase.Ready : Phase.Inactive;
        }
        var enter = VegFade.NearFadeStart * QualityRules.DetailDistanceScale;
        var exit = VegetationBuckets.NearReach * VegetationBuckets.Hysteresis;
        if (_phase == Phase.Inactive && distance <= enter)
        {
            _phase = Phase.Entering;
            _started = clock;
        }
        else if (_phase == Phase.Ready && distance > exit)
        {
            _phase = Phase.Exiting;
            _started = clock;
        }
    }

    public void Reset() => _phase = Phase.Inactive;
}
