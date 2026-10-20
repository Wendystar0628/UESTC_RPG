namespace Campus.Contracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KernelServiceAttribute(Type service)
    : RegistrationAttribute(RegistrationKind.Service, service.FullName ?? service.Name, RegistrationLifetime.Global)
{
    public Type Service { get; } = service;
    public override Type Contract => Service;
}

/// <summary>After saving and map shutdown, stop background native access before owned objects are released.</summary>
public interface IRegistryExitParticipant
{
    void PrepareForExit();
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelNodeAttribute(string id) : RegistrationAttribute(RegistrationKind.Node, id, RegistrationLifetime.Global)
{ }

public readonly record struct RenderHandle(ulong Value);

/// <summary>Godot-free boundary for runtime geometry. Only the kernel owns engine instances.</summary>
public interface IRenderHost
{
    RenderHandle Mount(ContentOutput content, WpId owner);
    /// <summary>True only for a completed live mount whose submitted draws have prepared materials.</summary>
    bool IsReady(RenderHandle handle);
    void Unmount(RenderHandle handle);
}
