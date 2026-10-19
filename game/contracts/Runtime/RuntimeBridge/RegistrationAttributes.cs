namespace Campus.Contracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KernelServiceAttribute(Type service) : Attribute
{
    public Type Service { get; } = service;
    public string Owner { get; set; } = "";
    public bool IsStub { get; set; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelNodeAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
    public string Map { get; set; } = "";
    public bool IsStub { get; set; }
}

public readonly record struct RenderHandle(ulong Value);

/// <summary>Godot-free boundary for runtime geometry. Only the kernel owns engine instances.</summary>
public interface IRenderHost
{
    RenderHandle Mount(ContentOutput content, WpId owner);
    /// <summary>True only for a completed live mount whose submitted draws have prepared materials.</summary>
    bool IsReady(RenderHandle handle);
    void Unmount(RenderHandle handle);
}
