using Godot;

namespace Campus.Kernel.Registry;

public partial class Kernel : Node
{
    public static ServiceRegistry Services { get; private set; } = new();
    public static T Get<T>() where T : class => Services.Get<T>();
}
