namespace Campus.Contracts;

public readonly record struct RenderHandle(ulong Value);

/// <summary>Godot-free boundary for runtime geometry. Only the kernel owns engine instances.</summary>
public interface IRenderHost
{
    RenderHandle Mount(ContentOutput content, WpId owner);
    /// <summary>True only for a completed live mount whose submitted draws have prepared materials.</summary>
    bool IsReady(RenderHandle handle);
    void Unmount(RenderHandle handle);
}
