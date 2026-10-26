namespace Campus.Contracts;

public readonly record struct RenderHandle(ulong Value);

/// <summary>Godot-free boundary for runtime geometry. Only the kernel owns engine instances.</summary>
public interface IRenderHost
{
    RenderHandle Mount(ContentOutput content);
    /// <summary>True only for a completed live mount whose submitted draws have prepared materials.</summary>
    bool IsReady(RenderHandle handle);
    /// <summary>Revokes readiness immediately and retires the actual native owner on the shared mount queue.</summary>
    void Unmount(RenderHandle handle);
    /// <summary>True when no active, preparing or retiring owner exists. Zero and never-issued handles own nothing and return true.</summary>
    bool IsReleased(RenderHandle handle);
}
