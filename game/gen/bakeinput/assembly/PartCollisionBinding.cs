using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Gen.BakeInput;

/// <summary>Frozen registered part definitions, shared by walking preparation and collision tile production.</summary>

public sealed class PartCollisionBinding(IEnumerable<KeyValuePair<PartId, IPartDef>> definitions)
{
    private readonly IReadOnlyDictionary<string, IPartDef> _parts = definitions.ToDictionary(
        pair => pair.Key.Value, pair => pair.Value, StringComparer.Ordinal);

    public IReadOnlyList<Collider> Collision(PlacedPart part)
    {
        if (!_parts.TryGetValue(part.Part.Value, out var definition)) throw new InvalidOperationException($"Unregistered collision part: {part.Part.Value}.");
        var contract = definition.GetType().GetInterfaces().Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPartDef<>));
        if (!contract.GenericTypeArguments[0].IsInstanceOfType(part.Params)) throw new ArgumentException($"Wrong parameters for part {part.Part.Value}.");
        var method = contract.GetMethod("Collision") ?? throw new MissingMethodException(contract.FullName, "Collision");
        try { return (IReadOnlyList<Collider>)(method.Invoke(definition, [part.Params]) ?? throw new InvalidOperationException("Part collision returned null.")); }
        catch (TargetInvocationException error) { throw new InvalidOperationException($"Collision generation failed for {part.Part.Value}.", error.InnerException ?? error); }
    }
}
