using System.Reflection;
using System.Text.RegularExpressions;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using GdUnit4;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class ArchitectureTests
{
    private static readonly Lazy<ReflectionAssembly[]> Production = new(LoadProduction);
    private static readonly Lazy<Architecture> Model = new(() => new ArchLoader()
        .LoadAssemblies(Production.Value.Append(ReflectionAssembly.Load("GodotSharp")).ToArray()).Build());

    private static ReflectionAssembly[] LoadProduction()
    {
        var composition = ReflectionAssembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Campus.dll"));
        var names = (string[])composition.GetType("Campus.Kernel.Registry.BuildInfo", throwOnError: true)!
            .GetField("ModuleAssemblies", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var paths = names.Select(name => Path.Combine(AppContext.BaseDirectory, name + ".dll")).ToArray();
        if (paths.Length == 0 || paths.Any(path => !File.Exists(path)))
        {
            throw new InvalidOperationException("Production binaries missing; architecture checks cannot run on an empty model.");
        }
        return paths.Select(ReflectionAssembly.LoadFrom).ToArray();
    }

    private static void Check(IArchRule rule)
    {
        if (!rule.HasNoViolations(Model.Value))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, rule.Evaluate(Model.Value)));
        }
    }

    [TestCase]
    [TestCategory("Contract")]
    public void GeneratorsCannotDependOnEngineOrRuntimeContracts()
    {
        var forbidden = Types().That().ResideInAssemblyMatching("^GodotSharp(?:,|$)")
            .Or().ResideInAssemblyMatching(@"^Campus(?:,|$)|^Campus\.Contracts\.Runtime(?:,|$)")
            .Or().HaveFullNameMatching(@"^System\.IO\.(File|Directory|FileInfo|DirectoryInfo|FileStream|FileSystemInfo|FileSystemWatcher|StreamReader|StreamWriter)$");
        Check(Types().That().ResideInAssemblyMatching(@"^Campus\.(Gen|Pipeline)(\.|,|$)")
            .Should().NotDependOnAny(forbidden).Because("ENG-ARCH 2: generators are pure and independent of runtime state")
            .WithoutRequiringPositiveResults());
        Check(Types().That().ResideInAssemblyMatching(@"^Campus\.Contracts(?:,|$)")
            .Should().NotDependOnAny(forbidden.Or().ResideInAssemblyMatching(@"^Campus(?:,|\.)(?!Contracts(?:,|$))"))
            .WithoutRequiringPositiveResults());
    }

    [TestCase]
    [TestCategory("Contract")]
    public void ArchitectureEngineDetectsAForbiddenDependency()
    {
        var probeModel = new ArchLoader().LoadAssembly(typeof(ArchitectureTests).Assembly).Build();
        var rule = Types().That().HaveFullName(typeof(ForbiddenDependencyProbe).FullName!)
            .Should().NotDependOnAny(typeof(Random));
        if (rule.HasNoViolations(probeModel))
        {
            throw new InvalidOperationException("Architecture engine did not detect the negative fixture.");
        }
    }

    private sealed class ForbiddenDependencyProbe
    {
        public Random Create() => new(1);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void AssemblyReferencesAndMapScopesRespectLayering()
    {
        var scopes = Production.Value.ToDictionary(assembly => assembly.GetName().Name!, MapScope, StringComparer.Ordinal);
        foreach (var assembly in Production.Value)
        {
            var name = assembly.GetName().Name!;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                AssertAssemblyReference(name, reference.Name!);
                if (name == "Campus" || !scopes.TryGetValue(reference.Name!, out var target) || target is null)
                {
                    continue; // Campus is the composition root and must discover every map.
                }
                if (scopes[name] != target)
                {
                    throw new InvalidOperationException($"MapScope violation: {name} ({scopes[name] ?? "generic"}) -> {reference.Name} ({target})");
                }
            }
            // Generic generation must never reach into a map, even when namespaces are shared.
            if (name == "Campus.Gen")
            {
                var others = Production.Value.Where(other => scopes[other.GetName().Name!] is not null).ToArray();
                if (others.Length > 0)
                {
                    Check(Types().That().ResideInAssembly(assembly).Should()
                        .NotDependOnAny(Types().That().ResideInAssembly(others[0], others.Skip(1).ToArray()))
                        .WithoutRequiringPositiveResults());
                }
            }
        }
        // Merged DLLs retain the former package dependency directions through namespaces.
        var packages = Model.Value.Types.Select(type => NamespaceOwner(type.Namespace.FullName))
            .Where(name => name is not null).Select(name => name!).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var origin in packages)
        {
            var forbidden = packages.Where(target => !AssertReference(origin, target)).Select(NamespacePattern).ToArray();
            // D-244: a zone reads another zone's identity catalog, never its implementation.
            var allowedCatalogs = MapNamespace(origin) is { } map
                ? "^" + Regex.Escape(map) + @"\.[^.]+\.[^.+]+(?:Feature)?Ids(?:\+[^.]+)?$" : "(?!)";
            if (forbidden.Length > 0)
                Check(Types().That().ResideInNamespaceMatching(NamespacePattern(origin)).Should()
                    .NotDependOnAny(Types().That().ResideInNamespaceMatching(string.Join('|', forbidden))
                        .And().DoNotHaveFullNameMatching(allowedCatalogs))
                    .WithoutRequiringPositiveResults());
        }
        // Evaluate wildcard references as MSBuild does, including references a compiler may elide.
        foreach (var project in SourceModel.ProjectItems())
            foreach (var reference in project.References)
                AssertAssemblyReference(Path.GetFileNameWithoutExtension(project.Path), Path.GetFileNameWithoutExtension(reference));

    }

    private static string? MapScope(ReflectionAssembly assembly) => assembly.GetCustomAttributesData()
        .SingleOrDefault(attribute => attribute.AttributeType.FullName == "Campus.Contracts.MapScopeAttribute")
        ?.ConstructorArguments[0].Value as string;

    private static string? NamespaceOwner(string name)
    {
        if (name == "Campus.Pipeline.Map" || name.StartsWith("Campus.Pipeline.Map.", StringComparison.Ordinal)) return "Campus.Pipeline.Map";
        if (!name.StartsWith("Campus.Gen.", StringComparison.Ordinal)) return null;
        var parts = name.Split('.');
        var length = MapNamespace(name) is not null ? 5 : parts[2] is "Parts" || name.StartsWith("Campus.Gen.Interior.Shell", StringComparison.Ordinal) ? 4 : 3;
        return string.Join('.', parts.Take(length));
    }

    private static string? MapNamespace(string name) => Production.Value
        .Where(assembly => MapScope(assembly) is not null)
        .Select(assembly => assembly.GetName().Name!)
        .SingleOrDefault(map => name == map || name.StartsWith(map + ".", StringComparison.Ordinal));

    private static string NamespacePattern(string owner) => owner == "Campus.Gen.Interior"
        ? @"^Campus\.Gen\.Interior(?!(?:\.Shell)(?:\.|$))(?:\.|$)"
        : "^" + Regex.Escape(owner) + @"(?:\.|$)";

    private static bool IsContent(string name) => name.StartsWith("Campus.Gen.", StringComparison.Ordinal) &&
        name is not "Campus.Gen.Geometry" and not "Campus.Gen.World" and not "Campus.Gen.Interior" &&
        !name.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal);

    private static void AssertAssemblyReference(string origin, string target)
    {
        if (!origin.StartsWith("Campus", StringComparison.Ordinal) || origin.EndsWith("Tests", StringComparison.Ordinal)) return;
        bool allowed;
        if (origin == "Campus") return;
        if (target.StartsWith("GodotSharp", StringComparison.Ordinal)) allowed = false;
        else if (!target.StartsWith("Campus", StringComparison.Ordinal)) return;
        else if (origin == "Campus.Contracts") allowed = false;
        else if (target == "Campus.Contracts") allowed = true;
        else if (origin == "Campus.Contracts.Runtime") allowed = false;
        else allowed = Production.Value.Any(assembly => assembly.GetName().Name == origin && MapScope(assembly) is not null) && target == "Campus.Gen";
        if (!allowed) throw new InvalidOperationException($"Assembly boundary: {origin} -> {target}");
    }

    private static bool AssertReference(string origin, string target)
    {
        // Map namespaces own registrations; generic sites are shared by every map.
        if (origin == target) return true;
        if (target == "Campus.Gen.Sites") return MapNamespace(origin) is not null;
        if (target == "Campus.Gen.Interior.Shell" && MapNamespace(origin) is not null) return true;
        if (MapNamespace(origin) is { } map && target == map) return true;
        if (origin == "Campus.Gen.Geometry") return false;
        if (origin is "Campus.Gen.World" or "Campus.Gen.Parts.Core") return target == "Campus.Gen.Geometry";
        if (origin.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal)) return target is "Campus.Gen.Parts.Core" or "Campus.Gen.Geometry";
        if (origin == "Campus.Gen.Interior") return target is "Campus.Gen.Parts.Core" or "Campus.Gen.Geometry";
        if (origin == "Campus.Pipeline.Map") return target == "Campus.Gen.World";
        return IsContent(origin) && (target is "Campus.Gen.Geometry" or "Campus.Gen.World" or "Campus.Gen.Interior" ||
            target.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal));
    }

    [TestCase]
    [TestCategory("Contract")]
    public void EngineCreationTimingAndAudioCallsStayWithOwners()
    {
        DenyCallsOutside(@"^Campus\.Kernel\.", method =>
            IsConstructor(method, "Godot.MeshInstance3D", "Godot.MultiMeshInstance3D", "Godot.OccluderInstance3D") ||
            IsMethod(method, "Godot.RenderingServer", "InstanceCreate", "MeshCreate"));
        DenyCallsOutside(@"^Campus\.Style\.", method => IsConstructor(method, "Godot.ShaderMaterial"));
        DenyCallsOutside(@"^Campus\.Kernel\.Collision(?:\.|$)", method =>
            IsConstructor(method, "Godot.StaticBody3D", "Godot.AnimatableBody3D", "Godot.Area3D") ||
            IsMethod(method, "Godot.CollisionObject3D", "ShapeOwnerAddShape"));
        DenyCallsOutside(@"^Campus\.Audio\.", method =>
            IsConstructor(method, "Godot.AudioStreamPlayer", "Godot.AudioStreamPlayer2D", "Godot.AudioStreamPlayer3D") ||
            IsMethod(method, "Godot.AudioServer", "Set", "set_", "AddBus", "RemoveBus"));
        DenyCallsOutside(@"^Campus\.Audio\.Core(?:\.|$)", method => IsMethod(method, "Godot.AudioServer", "SetBusMute", "SetBusVolumeDb"));
        DenyCallsOutside(@"^Campus\.Camera(?:\.|$)", method => IsConstructor(method, "Godot.AudioListener3D"));
        DenyCallsOutside(@"^Campus\.(?:Kernel\.(?:Frame|Quality|Inspect|Save)(?:\.|$)|Gates(?:\.|$))", method =>
            method.DeclaringType.FullName == "System.Diagnostics.Stopwatch" ||
            IsMethod(method, "Godot.Time", "GetTicks") || IsConstructor(method, "Godot.Timer", "System.Timers.Timer"));
        DenyCallsOutside(@"^Campus\.Kernel\.Save(?:\.|$)", method => IsMethod(method, "Godot.Time", "GetUnixTimeFromSystem"));
        DenyCallsOutside("(?!)", method =>
            IsMethod(method, "Godot.Engine", "SetTimeScale", "set_TimeScale") ||
            IsMethod(method, "Godot.RenderingServer", "GlobalShaderParameterGet") ||
            IsMethod(method, "Godot.AudioStreamPlaylist", "SetShuffle", "set_Shuffle") ||
            IsMethod(method, "Godot.AudioServer", "SetPlaybackSpeedScale", "set_PlaybackSpeedScale"));
    }

    [TestCase]
    [TestCategory("Contract")]
    public void HudAndSystemsOnlyUsePublishedInterfaces()
    {
        Check(Types().That().ResideInNamespaceMatching(@"^Campus\.Kernel(?:\.|$)").Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Campus\.Systems(?:\.|$)"))
            .WithoutRequiringPositiveResults());
        Check(Types().That().ResideInNamespaceMatching(@"^Campus\.(?:Systems|Style)(?:\.|$)").Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Campus\.(?:Gen|Pipeline)(?:\.|$)"))
            .WithoutRequiringPositiveResults());
        Check(Types().That().ResideInNamespaceMatching(@"^Campus\.Hud(?:\.|$)").Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Campus\.(Kernel|Style|Camera|Medium|Systems|Audio|Flow)(\.|$)")
                .And().DoNotHaveFullName("Campus.Kernel.Registry.Kernel")
                .And().DoNotHaveFullName("Campus.Kernel.Materials.IShaderMaterialSource"))
            .WithoutRequiringPositiveResults());
        var hudForbiddenCalls = Methods().Where(method =>
            method.DeclaringType.FullName == "Campus.Kernel.Registry.Kernel" && !Regex.IsMatch(method.Name, @"^Get(?:<|\(|$)") ||
            method.DeclaringType.FullName.EndsWith(".ISaveService", StringComparison.Ordinal) &&
                !Regex.IsMatch(method.Name, @"^(?:get_|add_|remove_|Thumbnail)") ||
            method.DeclaringType.FullName.EndsWith(".IMapService", StringComparison.Ordinal) && method.Name.StartsWith("Travel", StringComparison.Ordinal));
        Check(Types().That().ResideInNamespaceMatching(@"^Campus\.Hud(?:\.|$)").Should()
            .NotCallAny(hudForbiddenCalls).WithoutRequiringPositiveResults());
        var systemNames = Model.Value.Types.Select(type => type.Namespace.FullName)
            .Where(name => name.StartsWith("Campus.Systems.", StringComparison.Ordinal))
            .Select(name => string.Join('.', name.Split('.').Take(3))).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var system in systemNames)
        {
            // Vegetation.Seasons is the one permitted intra-system shader-hook extension.
            // The approved Kernel.Materials.IShaderMaterialSource is outside this
            // peer-system set; ShaderMaterial construction remains Style-only above.
            var pattern = "^" + Regex.Escape(system) + @"(\.|$)";
            Check(Types().That().ResideInNamespaceMatching(pattern).Should()
                .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Campus\.Systems\.")
                    .And().DoNotResideInNamespaceMatching(pattern)).WithoutRequiringPositiveResults());
        }
        var transit = Methods().Where(method => IsMethod(method, "Campus.Contracts.ICameraRig", "Transit") ||
            method.DeclaringType.FullName.EndsWith(".ICameraRig", StringComparison.Ordinal) && method.Name.StartsWith("Transit", StringComparison.Ordinal));
        Check(Types().That().ResideInNamespaceMatching(@"^Campus\.Hud\.Map(?:\.|$)")
            .Should().NotCallAny(transit).WithoutRequiringPositiveResults());
    }

    [TestCase]
    [TestCategory("Contract")]
    public void SaveSectionsOnlyUseSafeSerialization()
    {
        var sectionNames = Model.Value.Types.Where(type => type.ImplementedInterfaces.Any(contract => contract.FullName.Contains("ISaveSection", StringComparison.Ordinal)))
            .Select(type => Regex.Escape(type.FullName));
        var owners = Types().That().ResideInNamespaceMatching(@"^Campus\.Kernel\.Save(?:\.|$)")
            .Or().HaveFullNameMatching("^(?:" + string.Join('|', sectionNames.Append("(?!)")) + ")$");
        Check(Types().That().Are(owners).Should().NotDependOnAny(Types().That().HaveFullNameMatching(@"^Godot\.(ConfigFile|Json|ResourceLoader|ResourceSaver)$"))
            .WithoutRequiringPositiveResults());
        var unsafeCalls = Methods().Where(method =>
            IsMethod(method, "Godot.GD", "StrToVar", "BytesToVar") || IsMethod(method, "Godot.FileAccess", "GetVar") ||
            method.DeclaringType.FullName == "System.Text.Json.JsonSerializer" &&
            (method.Name.StartsWith("Serialize", StringComparison.Ordinal) || method.Name.StartsWith("Deserialize", StringComparison.Ordinal)) &&
            !method.Parameters.Any(parameter => parameter.FullName.StartsWith("System.Text.Json.Serialization.Metadata.JsonTypeInfo", StringComparison.Ordinal)));
        Check(Types().That().Are(owners).Should().NotCallAny(unsafeCalls).WithoutRequiringPositiveResults());
    }

    private static IEnumerable<MethodMember> Methods() => Model.Value.Types.Concat(Model.Value.ReferencedTypes)
        .SelectMany(type => type.Members).OfType<MethodMember>().Distinct();

    private static void DenyCallsOutside(string allowedNamespace, Func<MethodMember, bool> forbidden)
    {
        Check(Types().That().ResideInAssemblyMatching(@"^Campus(?:,|\.)")
            .And().DoNotResideInNamespaceMatching(allowedNamespace).Should().NotCallAny(Methods().Where(forbidden))
            .WithoutRequiringPositiveResults());
    }

    private static bool IsConstructor(MethodMember method, params string[] types) =>
        method.MethodForm == MethodForm.Constructor && types.Contains(method.DeclaringType.FullName, StringComparer.Ordinal);

    private static bool IsMethod(MethodMember method, string type, params string[] prefixes) =>
        method.DeclaringType.FullName == type && prefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal));

    internal static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("UESTC_REPOSITORY or a repository ancestor is required.");
    }
}
