using System.Reflection;
using System.Text.RegularExpressions;
using Campus.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    [TestCase]
    [TestCategory("Contract")]
    public void KernelNamespacesOnlyDependDownward()
    {
        // Imports and constant references can disappear from IL; check their bound source symbols too.
        SourceModel.RequireNoViolations(KernelSourceViolations(SourceModel.Production));
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

    private static IEnumerable<string> KernelSourceViolations(IEnumerable<SourceFile> sources)
    {
        var files = sources.ToArray();
        var allOwners = files.SelectMany(file => KernelSourceOwners(file, file.Root)).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var file in files)
        foreach (var name in file.Root.DescendantNodes().OfType<NameSyntax>())
        {
            // A namespace declaration establishes ownership; it is not a dependency on its parent.
            if (name.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any(space => space.Name.Span.Contains(name.Span))) continue;
            var symbol = file.Model.GetSymbolInfo(name).Symbol;
            if (symbol is IAliasSymbol alias) symbol = alias.Target;
            var spaceName = symbol is INamespaceSymbol spaceSymbol ? spaceSymbol.ToDisplayString() : symbol?.ContainingNamespace?.ToDisplayString();
            if (KernelOwner(spaceName ?? "") is not { } target) continue;
            var import = name.Ancestors().OfType<UsingDirectiveSyntax>().FirstOrDefault();
            var origins = import is null
                ? new[] { KernelOwner(file.Model.GetEnclosingSymbol(name.SpanStart)?.ContainingNamespace?.ToDisplayString() ?? "") }.OfType<string>()
                : import.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) ? allOwners
                : KernelSourceOwners(file, (SyntaxNode?)import.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault() ?? file.Root);
            foreach (var origin in origins)
                if (KernelLevel(target) > KernelLevel(origin))
                    yield return file.At(name, $"Kernel source boundary: {origin} -> {target}; kernel responsibilities only depend on the same or lower responsibilities (ENG-ARCH).");
        }
    }

    private static IEnumerable<string> KernelSourceOwners(SourceFile file, SyntaxNode scope) =>
        scope.DescendantNodesAndSelf().OfType<BaseNamespaceDeclarationSyntax>()
            .Select(space => KernelOwner(file.Model.GetDeclaredSymbol(space)?.ToDisplayString() ?? ""))
            .OfType<string>().Distinct(StringComparer.Ordinal);

    [TestCase]
    [TestCategory("Contract")]
    public void KernelSourceRuleDetectsImportsAndInlinedConstants()
    {
        var declarations = CSharpSyntaxTree.ParseText("""
            namespace Campus.Kernel.Boot { public static class BootTuning { public const double StalledSeconds = 1; } }
            namespace Campus.Kernel.Renderer { public static class Limits { public const double Seconds = 1; } }
            namespace Campus.Kernel.Frame { public static class Limits { public const double Seconds = 1; } }
            namespace Campus.Kernel.Diagnostics { public static class Limits { public const double Seconds = 1; } }
            """);
        (string Source, bool Rejected)[] cases =
        [
            ("using Campus.Kernel.Boot; namespace Campus.Kernel.Renderer { class Probe { } }", true),
            ("using Upper = Campus.Kernel.Boot.BootTuning; namespace Campus.Kernel.Renderer { class Probe { } }", true),
            ("using static Campus.Kernel.Boot.BootTuning; namespace Campus.Kernel.Renderer { class Probe { const double Seconds = StalledSeconds; } }", true),
            ("using Upper = Campus.Kernel.Boot; namespace Campus.Kernel.Renderer { class Probe { const double Seconds = Upper.BootTuning.StalledSeconds; } }", true),
            ("namespace Campus.Kernel.Renderer { class Probe { const double Seconds = global::Campus.Kernel.Boot.BootTuning.StalledSeconds; } }", true),
            ("namespace Campus.Kernel.Renderer { using Campus.Kernel.Boot; class Probe { const double Seconds = BootTuning.StalledSeconds; } }", true),
            ("using Campus.Kernel.Frame; namespace Campus.Kernel.Renderer { class Probe { const double Seconds = Campus.Kernel.Frame.Limits.Seconds; } }", false),
            ("namespace Campus.Kernel.Frame { class Probe { const double Seconds = Campus.Kernel.Diagnostics.Limits.Seconds; } }", false),
            ("namespace Campus.Kernel.Renderer { class Probe { const double Seconds = Limits.Seconds; } }", false),
            ("namespace Campus.Kernel.Boot { using Campus.Kernel.Renderer; class Probe { } } namespace Campus.Kernel.Frame { class Probe { } }", false),
            ("global using Campus.Kernel.Boot; namespace Campus.Kernel.Renderer { class Probe { } }", true)
        ];
        foreach (var (source, rejected) in cases)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("KernelSourceProbe", [declarations, tree],
                [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(item => item.ToString())));
            var violations = KernelSourceViolations([new("KernelSourceProbe.cs", tree, compilation.GetSemanticModel(tree))]).ToArray();
            if ((violations.Length > 0) != rejected)
                throw new InvalidOperationException($"Kernel source rule did not preserve the dependency direction for: {source}");
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

    [TestCase]
    [TestCategory("Contract")]
    public void FramePrioritiesUseTheRegisteredStepsWithoutOffsets()
    {
        var fields = typeof(FramePriority).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(int)).ToArray();
        if (fields.Length == 0 || fields.Select(field => (int)field.GetRawConstantValue()!).Distinct().Count() != fields.Length)
            throw new InvalidOperationException("FramePriority must declare distinct integer steps.");
        var values = fields.OrderBy(field => field.MetadataToken).Select(field => (int)field.GetRawConstantValue()!).ToArray();
        if (!values.SequenceEqual(values.Order()))
            throw new InvalidOperationException("FramePriority declarations must follow execution order.");
        var registered = fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        foreach (var assignment in file.Root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (file.Model.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol property ||
                property.Name is not ("ProcessPriority" or "ProcessPhysicsPriority") ||
                !SourceModel.Inherits(property.ContainingType, "Godot.Node")) continue;
            if (file.Model.GetSymbolInfo(assignment.Right).Symbol is not IFieldSymbol field ||
                SourceModel.Name(field.ContainingType) != "Campus.Contracts.FramePriority" ||
                !registered.Contains(field.Name))
                errors.Add(file.At(assignment, "Use a FramePriority step directly; register a new value when order is required."));
        }
        foreach (var file in SourceModel.Production)
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var method = SourceModel.Method(file, call);
            if (method?.Name is not ("SetProcessPriority" or "SetPhysicsProcessPriority") ||
                !SourceModel.Inherits(method.ContainingType, "Godot.Node")) continue;
            if (call.ArgumentList.Arguments.Count != 1 ||
                file.Model.GetSymbolInfo(call.ArgumentList.Arguments[0].Expression).Symbol is not IFieldSymbol field ||
                SourceModel.Name(field.ContainingType) != "Campus.Contracts.FramePriority" ||
                !registered.Contains(field.Name))
                errors.Add(file.At(call, "Use a FramePriority step directly; register a new value when order is required."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void ProductionDiagnosticsUseThePublishedOutput()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var method = SourceModel.Method(file, call);
            if (method?.Name is not ("PushError" or "PushWarning") ||
                SourceModel.Name(method.ContainingType) != "Godot.GD") continue;
            errors.Add(file.At(call, "Diagnostics use IDiagnosticLog or the kernel DiagnosticLog output."));
        }
        SourceModel.RequireNoViolations(errors);
    }

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
                .And().DoNotHaveFullName("Campus.Kernel.Registry.Kernel"))
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
            if (File.Exists(Path.Combine(current.FullName, "game", "project.godot")) &&
                File.Exists(Path.Combine(current.FullName, "docs", "README.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("UESTC_REPOSITORY or a repository ancestor is required.");
    }
}
