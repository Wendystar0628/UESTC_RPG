namespace Campus.Kernel.Registry;

/// <summary>Composition access to the application's single registry; this is not an engine node.</summary>
public static class Kernel
{
    public static ServiceRegistry Services { get; } = new();
    public static T Get<T>() where T : class => Services.Get<T>();
}
