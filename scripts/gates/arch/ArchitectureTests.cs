using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        var names = Directory.GetFiles(AppContext.BaseDirectory, "Campus*.dll")
            .Where(path => !Path.GetFileNameWithoutExtension(path).EndsWith("Tests", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        if (!names.Any(path => Path.GetFileName(path) == "Campus.dll") ||
            !names.Any(path => Path.GetFileName(path) == "Campus.Contracts.dll") ||
            !names.Any(path => Path.GetFileName(path) == "Campus.Contracts.Runtime.dll"))
        {
            throw new InvalidOperationException("Production binaries missing; architecture checks cannot run on an empty model.");
        }
        return names.Select(ReflectionAssembly.LoadFrom).ToArray();
    }

    private static void Check(IArchRule rule)
    {
        if (!rule.HasNoViolations(Model.Value))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, rule.Evaluate(Model.Value)));
        }
    }

    [TestCase]
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
    public void AssemblyReferencesAndMapScopesRespectLayering()
    {
        var scopes = Production.Value.ToDictionary(assembly => assembly.GetName().Name!, MapScope, StringComparer.Ordinal);
        foreach (var assembly in Production.Value)
        {
            var name = assembly.GetName().Name!;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                AssertReference(name, reference.Name!);
                if (name == "Campus" || !scopes.TryGetValue(reference.Name!, out var target) || target is null)
                {
                    continue; // Campus is the composition root and must discover every map.
                }
                if (scopes[name] != target)
                {
                    throw new InvalidOperationException($"MapScope violation: {name} ({scopes[name] ?? "generic"}) -> {reference.Name} ({target})");
                }
            }
            // Check real IL dependencies as well as declared assembly references.
            if (IsContent(name))
            {
                var others = Production.Value.Where(other => other != assembly && IsContent(other.GetName().Name!)).ToArray();
                if (others.Length > 0)
                {
                    Check(Types().That().ResideInAssembly(assembly).Should()
                        .NotDependOnAny(Types().That().ResideInAssembly(others[0], others.Skip(1).ToArray()))
                        .WithoutRequiringPositiveResults());
                }
            }
        }
        // An empty project can lose unused references during compilation. Inspect its declaration too.
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? FindRepository();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "game", "gen"), "*.csproj", SearchOption.AllDirectories)
                     .Append(Path.Combine(root, "game", "contracts", "Campus.Contracts.csproj"))
                     .Append(Path.Combine(root, "game", "contracts", "Runtime", "Campus.Contracts.Runtime.csproj"))
                     .Append(Path.Combine(root, "pipeline", "map", "Campus.Pipeline.Map.csproj")))
        {
            var document = XDocument.Load(path);
            foreach (var item in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                var target = (string?)item.Attribute("Include") ?? throw new InvalidOperationException("ProjectReference missing Include");
                foreach (var reference in target.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    AssertReference(Path.GetFileNameWithoutExtension(path), Path.GetFileNameWithoutExtension(reference.Replace('\\', '/')));
                }
            }
        }
    }

    private static string? MapScope(ReflectionAssembly assembly) => assembly.GetCustomAttributesData()
        .SingleOrDefault(attribute => attribute.AttributeType.FullName == "Campus.Contracts.MapScopeAttribute")
        ?.ConstructorArguments[0].Value as string;

    private static bool IsContent(string name) => name.StartsWith("Campus.Gen.", StringComparison.Ordinal) &&
        name is not "Campus.Gen.Geometry" and not "Campus.Gen.World" and not "Campus.Gen.Interior" &&
        !name.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal);

    private static void AssertReference(string origin, string target)
    {
        if (!origin.StartsWith("Campus", StringComparison.Ordinal) || origin.EndsWith("Tests", StringComparison.Ordinal)) return;
        bool allowed;
        if (origin == "Campus") return;
        if (target.StartsWith("GodotSharp", StringComparison.Ordinal)) allowed = false;
        else if (!target.StartsWith("Campus", StringComparison.Ordinal)) return;
        else if (origin == "Campus.Contracts") allowed = false;
        else if (target == "Campus.Contracts") allowed = true;
        else if (origin == "Campus.Contracts.Runtime") allowed = false;
        else if (origin == "Campus.Gen.Geometry") allowed = false;
        else if (origin == "Campus.Gen.World") allowed = false;
        else if (origin == "Campus.Gen.Parts.Core") allowed = false;
        else if (origin.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal)) allowed = target == "Campus.Gen.Parts.Core";
        else if (origin == "Campus.Gen.Interior") allowed = target == "Campus.Gen.Parts.Core";
        else if (origin == "Campus.Pipeline.Map") allowed = target == "Campus.Gen.World";
        else allowed = target is "Campus.Gen.Geometry" or "Campus.Gen.World" or "Campus.Gen.Interior" || target.StartsWith("Campus.Gen.Parts.", StringComparison.Ordinal);
        if (!allowed) throw new InvalidOperationException($"Assembly boundary: {origin} -> {target}");
    }

    [TestCase]
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
    public void HudAndSystemsOnlyUsePublishedInterfaces()
    {
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

    private static string FindRepository()
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
