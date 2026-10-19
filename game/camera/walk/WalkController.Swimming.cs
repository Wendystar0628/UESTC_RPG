using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private IWaterTraversalQuery? _water;
    private bool _swimming;

    private bool ReadWater(N.Vector3 point, out WaterQueryResult result)
    {
        result = _water!.Sample(new(point.X, point.Z));
        switch (result.Status)
        {
            case WaterQueryStatus.Ready:
            case WaterQueryStatus.NoWater:
                return true;
            case WaterQueryStatus.NotReady:
                CollisionReady = false;
                return false;
            case WaterQueryStatus.Invalid:
                GD.PushError($"[WP-42] invalid water query: {result.DiagnosticCode}");
                RequestExit();
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(result));
        }
    }

    private void FreezeWaterMotion()
    {
        _previousFeet = _feet;
        Velocity = Vector3.Zero;
        ResetComfort();
    }

    private void UpdateSwimming(WaterQueryResult result)
    {
        bool swimming = result.Sample is { } sample && WalkMath.SwimmingAt(sample, _feet.Y, _swimming);
        bool changed = swimming != _swimming;
        if (changed)
        {
            _swimming = swimming;
            _crouching = _restoredCrouch = false;
            _coyote = _jumpBuffer = 0;
            _jumpQueued = false;
            Grounded = false;
            MotionMode = swimming ? MotionModeEnum.Floating : MotionModeEnum.Grounded;
            ApplyHeight();
            ResetComfort();
        }
        if (_swimming)
        {
            var water = result.Sample!.Value;
            _feet.Y = water.SurfaceY - WaterMotionRules.FeetBelowSurface;
            GlobalPosition = _feet.ToGodot();
            Velocity = new(Velocity.X, 0, Velocity.Z);
            Grounded = false;
            // The new eye height takes effect this tick; do not interpolate from submerged walking feet.
            if (changed)
            {
                _previousFeet.Y = _feet.Y;
                _waterEnter?.Invoke(_telemetryTick, water.SurfaceY - water.BottomY);
            }
        }
        else if (changed) _waterExit?.Invoke(_telemetryTick);
    }

    private void ResetSwimming()
    {
        _swimming = false;
        MotionMode = MotionModeEnum.Grounded;
        ApplyHeight();
    }
}
