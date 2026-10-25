using Godot;

namespace Campus.Contracts;

public readonly record struct RenderDimensions(
    ulong DrawFrame, Vector2I TargetSizePx, Vector2I InternalSizePx);

public interface IRenderDimensions
{
    // Main-thread read of one coherent render-thread observation; absent before a live draw.
    // DrawFrame identifies the source draw, not the process or collection frame.
    RenderDimensions? Latest { get; }
}
