using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Boot;

/// <summary>Loads and activates the current validated ruleset through the service registry.</summary>
[KernelService(typeof(IRuleset))]
[KernelService(typeof(IRulesetActivation))]
public sealed class RulesetService : IRuleset, IRulesetActivation
{
    private readonly IRequiredResources _resources;

    public RulesetService(IRequiredResources resources)
    {
        _resources = resources;
        Current = Read(ProjectSettings.GetSetting("campus/rulesets/default").AsString());
    }

    public RulesetDescriptor Current { get; private set; }
    public long Rev { get; private set; } = 1;

    public void Select(RulesetId id)
    {
        if (id == Current.Id) return;
        if (string.IsNullOrEmpty(id.Value) || id.Value.Length > 64 ||
            id.Value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
            throw new ArgumentException("Invalid ruleset identifier.", nameof(id));
        var path = "res://data/rulesets/" + id.Value + ".json";
        var next = Read(path);
        if (next.Id != id) throw new InvalidDataException("Ruleset identity is invalid.");
        Current = next;
        Rev++;
    }

    private RulesetDescriptor Read(string path)
    {
        var descriptor = JsonSerializer.Deserialize<RulesetDescriptor>(_resources.ReadBytes(path), ContractJson.Options)
            ?? throw new InvalidDataException("Ruleset data is missing.");
        if (descriptor.Characters || descriptor.ClearVoices || descriptor.Interaction != InteractionRule.None)
            throw new InvalidDataException("Sightseeing content rules are violated.");
        if (!Enum.IsDefined(descriptor.DefaultViewMode) || descriptor.ViewModes is null ||
            !descriptor.ViewModes.Contains(descriptor.DefaultViewMode))
            throw new InvalidDataException("The default view mode must be allowed by the ruleset.");
        return descriptor;
    }
}
