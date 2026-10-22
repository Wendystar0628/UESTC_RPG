namespace Campus.Contracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KernelServiceAttribute(Type service)
    : RegistrationAttribute(RegistrationKind.Service, service.FullName ?? service.Name, RegistrationLifetime.Global)
{
    public Type Service { get; } = service;
    public override Type Contract => Service;
}

/// <summary>
/// After saving and map shutdown, stop every participant first, then await every participant's
/// owned work before any node leaves the tree or any registered object is released.
/// </summary>
public interface IRegistryExitParticipant
{
    void PrepareForExit();
    Task DrainForExitAsync();
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelNodeAttribute(string id) : RegistrationAttribute(RegistrationKind.Node, id, RegistrationLifetime.Global)
{ }
