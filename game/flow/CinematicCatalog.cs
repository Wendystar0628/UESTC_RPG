using System.Text.Json;
using Campus.Camera.Cinematic;
using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;

namespace Campus.Flow;

public sealed class CinematicCatalog(IMapContext context)
{
    public CinematicDef? Find(string role)
    {
        if (!context.Descriptor.Cinematics.TryGetValue(role, out var id)) return null;
        return Read(id);
    }
    public CinematicDef Read(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.AsSpan().IndexOfAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-".AsSpan()) >= 0)
            throw new InvalidDataException("Invalid cinematic identifier.");
        if (!context.SourceInputs.Entries.TryGetValue("cinematics/" + id + ".json", out var bytes))
            throw new InvalidDataException($"Cinematic data is missing: {id}");
        var definition = JsonSerializer.Deserialize<CinematicDef>(bytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException($"Empty cinematic: {id}");
        CinematicValidation.Validate(definition, context.Descriptor.Id);
        ValidateNewGameEnd(definition, context.Descriptor.World);
        if (definition.Id != id) throw new InvalidDataException("Cinematic filename and identifier differ.");
        return definition;
    }
    private static void ValidateNewGameEnd(CinematicDef definition, MapWorldRules world)
    {
        switch (definition.Kind)
        {
            case CinematicKind.EnterContinue: case CinematicKind.Leave: return;
            case CinematicKind.EnterNew: case CinematicKind.EnterNewShort: break;
            default: throw new InvalidDataException("Unknown cinematic kind.");
        }
        var creation = definition.World.Count(c => c.Cue is WorldCueKind.NewTimeline);
        var target = definition.World.LastOrDefault(c => c.Cue is WorldCueKind.FlowTo)?.Clock;
        if (creation != 1 || target is null || Math.Abs(target.Value - world.Default.Clock) > FlowTuning.ClockTolerance || definition.End.Flow != 1)
            throw new InvalidDataException("New-game timeline does not reach the map default state.");
        if (world.NewGameStart is { } start && (start.Season != world.Default.Season ||
            start.Weather.HasValue && world.Default.Weather.HasValue && start.Weather != world.Default.Weather))
            throw new InvalidDataException("New-game world cues cannot change season or weather to reach this default.");
    }
}
