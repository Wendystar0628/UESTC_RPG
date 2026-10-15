using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Registry;

public partial class Kernel : Node
{
    public static ServiceRegistry Services { get; private set; } = new();
    public static T Get<T>() where T : class => Services.Get<T>();
    public override void _Ready()
    {
        Services = new ServiceRegistry();
        var bytes = Godot.FileAccess.GetFileAsBytes(ProjectSettings.GetSetting("campus/rulesets/default").AsString());
        var descriptor = JsonSerializer.Deserialize<RulesetDescriptor>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException("Ruleset data is missing.");
        if (descriptor.Characters || descriptor.ClearVoices || descriptor.Interaction != InteractionRule.None)
            throw new InvalidDataException("Sightseeing content rules are violated.");
        Services.Add<IRuleset>(new RulesetService(descriptor));
        var assemblies = new List<Assembly> { typeof(Kernel).Assembly };
        foreach (var name in BuildInfo.ModuleAssemblies) assemblies.Add(Assembly.Load(new AssemblyName(name)));
        Services.Discover(assemblies);
        GD.Print($"D0 registry: {Services.Entries.Count} declarations; build {BuildInfo.Build}");
    }
    private sealed class RulesetService(RulesetDescriptor current) : IRuleset
    {
        public RulesetDescriptor Current { get; } = current;
        public long Rev => 1;
    }
}
