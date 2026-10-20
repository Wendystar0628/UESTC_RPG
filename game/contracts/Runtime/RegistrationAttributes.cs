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
