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
        Check(Types().That().ResideInAssemblyMatching(@"^Campus\.Gen(\.|,|$)")
            .Should().NotDependOnAny(forbidden).Because("ENG-ARCH 2: generators are pure and independent of runtime state")
            .WithoutRequiringPositiveResults());
        Check(Types().That().ResideInAssemblyMatching(@"^Campus\.Contracts(?:,|$)")
            .Should().NotDependOnAny(forbidden.Or().ResideInAssemblyMatching(@"^Campus(?:,|\.)(?!Contracts(?:,|$))"))
            .WithoutRequiringPositiveResults());
    }

    [TestCase]
    [TestCategory("Contract")]
    public void RuntimeContractsCannotDependOnImplementations()
    {
        Check(Types().That().ResideInAssemblyMatching(@"^Campus\.Contracts\.Runtime(?:,|$)")
            .Should().NotDependOnAny(Types().That().ResideInAssemblyMatching(@"^Campus(?:,|$)|^Campus\.Gen(?:\.|,|$)"))
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
            // DECISION-244: a zone reads another zone's identity catalog, never its implementation.
            var allowedCatalogs = MapNamespace(origin) is { } map
                ? "^" + Regex.Escape(map) + @"\.[^.]+\.[^.+]+(?:Feature)?Ids(?:\+[^.]+)?$" : "(?!)";
            if (forbidden.Length > 0)
                Check(Types().That().ResideInNamespaceMatching(NamespacePattern(origin)).Should()
                    .NotDependOnAny(Types().That().ResideInNamespaceMatching(string.Join('|', forbidden))
                        .And().DoNotHaveFullNameMatching(allowedCatalogs))
                    .WithoutRequiringPositiveResults());
        }
        // The native graph retains project dependencies even when the compiler elides their IL use.
        foreach (var project in SourceModel.Projects)
            foreach (var reference in project.ProjectReferences)
            {
                var target = project.Solution.GetProject(reference.ProjectId)
                    ?? throw new InvalidOperationException($"G4 could not resolve a project reference from {project.FilePath}.");
                AssertAssemblyReference(project.AssemblyName!, target.AssemblyName!);
            }

    }

    [TestCase]
    [TestCategory("Contract")]
    public void KernelNamespacesOnlyDependDownward()
    {
        var owners = Model.Value.Types.Select(type => KernelOwner(type.Namespace.FullName))
            .Where(owner => owner is not null).Select(owner => owner!).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var owner in owners)
        {
            var level = KernelLevel(owner);
            var forbidden = owners.Where(target => KernelLevel(target) > level)
                .Select(target => "^Campus\\.Kernel\\." + Regex.Escape(target) + @"(?:\.|$)").ToArray();
            if (forbidden.Length == 0) continue;
            Check(Types().That().ResideInNamespaceMatching("^Campus\\.Kernel\\." + Regex.Escape(owner) + @"(?:\.|$)")
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(string.Join('|', forbidden)))
                .Because("ENG-ARCH: kernel responsibilities only depend on lower responsibilities")
                .WithoutRequiringPositiveResults());
        }
    }

    private static string? KernelOwner(string space) => space.StartsWith("Campus.Kernel.", StringComparison.Ordinal)
        ? space.Split('.')[2] : null;

    // These are concrete responsibility homes, including order within each broad architecture layer.
    private static int KernelLevel(string owner) => owner switch
    {
        "Diagnostics" => 0, "Frame" => 0, "Registry" => 1,
        "Materials" => 2, "Lod" => 3, "Renderer" => 4, "Instances" => 5,
        "Settings" => 6, "Stubs" => 7, "Collision" => 8, "Maps" => 9,
        "Quality" => 10, "Lighting" => 11, "Post" => 12, "World" => 13,
        "Bake" => 14, "Stream" => 15,
        "Boot" => 16, "Save" => 17, "Inspect" => 18,
        _ => throw new InvalidOperationException($"Kernel namespace has no declared layer: {owner}.")
    };

    private static string? MapScope(ReflectionAssembly assembly) => assembly.GetCustomAttributesData()
        .SingleOrDefault(attribute => attribute.AttributeType.FullName == "Campus.Contracts.MapScopeAttribute")
        ?.ConstructorArguments[0].Value as string;

    private static string? NamespaceOwner(string name)
    {
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
        name is not "Campus.Gen.Geometry" and not "Campus.Gen.World" and not "Campus.Gen.Interior" and not "Campus.Gen.BakeInput" &&
        !name.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal);

    private static void AssertAssemblyReference(string origin, string target)
    {
        if (!origin.StartsWith("Campus", StringComparison.Ordinal) || origin.EndsWith("Tests", StringComparison.Ordinal)) return;
        bool allowed;
        if (origin == "Campus") return;
        if (target.StartsWith("GodotSharp", StringComparison.Ordinal)) allowed = origin == "Campus.Contracts.Runtime";
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
        if (origin == "Campus.Gen.BakeInput") return target is "Campus.Gen.World" or "Campus.Gen.Geometry";
        return IsContent(origin) && (target is "Campus.Gen.Geometry" or "Campus.Gen.World" or "Campus.Gen.Interior" ||
            target.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal));
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
                .And().DoNotHaveFullName("Campus.Kernel.Registry.Kernel"))
            .WithoutRequiringPositiveResults());
        var systemNames = Model.Value.Types.Select(type => type.Namespace.FullName)
            .Where(name => name.StartsWith("Campus.Systems.", StringComparison.Ordinal))
            .Select(name => string.Join('.', name.Split('.').Take(3))).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var system in systemNames)
        {
            // Vegetation.Seasons is the one permitted intra-system shader-hook extension.
            var pattern = "^" + Regex.Escape(system) + @"(\.|$)";
            Check(Types().That().ResideInNamespaceMatching(pattern).Should()
                .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Campus\.Systems\.")
                    .And().DoNotResideInNamespaceMatching(pattern)).WithoutRequiringPositiveResults());
        }
    }

    internal static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "game", "project.godot")) &&
                File.Exists(Path.Combine(current.FullName, "docs", "README.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("UESTC_REPOSITORY or a repository ancestor is required.");
    }
}
