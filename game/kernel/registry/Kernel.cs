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
        var rulesets = new RulesetService(descriptor);
        Services.Add<IRuleset>(rulesets);
        Services.Add<IRulesetActivation>(rulesets);
        var assemblies = new List<Assembly> { typeof(Kernel).Assembly };
        foreach (var name in BuildInfo.ModuleAssemblies) assemblies.Add(Assembly.Load(new AssemblyName(name)));
        Services.Discover(assemblies);
        GD.Print($"D0 registry: {Services.Entries.Count} declarations; build {BuildInfo.Build}");
    }
    private sealed class RulesetService(RulesetDescriptor current) : IRuleset, IRulesetActivation
    {
        public RulesetDescriptor Current { get; private set; } = current;
        public long Rev { get; private set; } = 1;
        public void Select(RulesetId id)
        {
            if (id == Current.Id) return;
            if (string.IsNullOrEmpty(id.Value) || id.Value.Length > 64 ||
                id.Value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
                throw new ArgumentException("Invalid ruleset identifier.", nameof(id));
            var path = "res://data/rulesets/" + id.Value + ".json";
            if (!Godot.FileAccess.FileExists(path)) throw new FileNotFoundException("Ruleset not found: " + id.Value);
            var next = JsonSerializer.Deserialize<RulesetDescriptor>(Godot.FileAccess.GetFileAsBytes(path), ContractJson.Options)
                ?? throw new InvalidDataException("Ruleset data is null.");
            if (next.Id != id || next.Characters || next.ClearVoices || next.Interaction != InteractionRule.None)
                throw new InvalidDataException("Ruleset identity or current sightseeing content rules are invalid.");
            Current = next;
            Rev++;
        }
    }
}
