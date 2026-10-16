using System.Reflection;
using Campus.Contracts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// The levels each catalogue part can build (the keys of its <c>IPartDef.Lods</c>), read from the part packages this zone
/// references. The baker refuses a placed part at a level its definition lacks ("Part X has no L2 builder"), and small
/// street furniture registers L0 and L1 only (ART-MODELING §7.8), so <see cref="CoreBuild.Finish"/> asks here instead of each
/// piece repeating the parts' LOD lists.
/// </summary>
public static class PartLods
{
    /// <summary>One type from each part package the zone references; the registry is these assemblies' <see cref="PartAttribute"/> types.</summary>
    private static readonly Type[] Packages =
        [typeof(PartGeometryKernel), typeof(OpeningPlacement), typeof(CirculationGallery), typeof(RoofGallery), typeof(StreetGallery)];

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlySet<LodLevel>>> Table = new(Scan);

    /// <summary>The levels the part builds, or null when no referenced package registers it.</summary>
    public static IReadOnlySet<LodLevel>? Of(PartId part) => Table.Value.GetValueOrDefault(part.Value);

    /// <summary>
    /// Whether the baker can build the part at the level. A part no referenced package registers counts as supported:
    /// the baker reports it by name, which is better than losing it here without a trace.
    /// </summary>
    public static bool Supports(PartId part, LodLevel lod) => Of(part) is not { } levels || levels.Contains(lod);

    private static IReadOnlyDictionary<string, IReadOnlySet<LodLevel>> Scan()
    {
        var table = new Dictionary<string, IReadOnlySet<LodLevel>>(StringComparer.Ordinal);
        foreach (var assembly in Packages.Select(t => t.Assembly).Distinct())
            foreach (var type in assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                if (type.IsAbstract || type.GetCustomAttribute<PartAttribute>() is not { } attribute) continue;
                var definition = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
                var lods = definition.GetProperty(nameof(IPartDef<object>.Lods))!;
                var instance = Activator.CreateInstance(type)!;
                var keys = (IEnumerable<LodLevel>)lods.PropertyType.GetProperty("Keys")!.GetValue(lods.GetValue(instance))!;
                table.TryAdd(attribute.Id, keys.ToHashSet());
            }
        return table;
    }
}
