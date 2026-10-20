// Hashes directory-owned production source, using Roslyn only for the real project graph and registration types.
// 用法：由 Program 调用；分组规则只在 SourceGroup 定义，运行时只读生成的 manifest。
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Campus.Build.Fingerprints;

internal static class TargetSourceFingerprint
{
    private sealed record Source(string Path, string Hash, string Group);
    private sealed record Root(string Key, string Group);
    private static readonly IReadOnlyDictionary<string, string> Registrations = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [typeof(ContentModuleAttribute).FullName!] = typeof(IContentModule).FullName!,
        [typeof(InteriorProviderAttribute).FullName!] = typeof(IInteriorProvider).FullName!,
        [typeof(InteriorProviderFactoryAttribute).FullName!] = typeof(IInteriorProviderFactory).FullName!,
        [typeof(BakeProducerAttribute).FullName!] = typeof(IBakeProducer).FullName!
    };

    public static async Task<BakeCodeManifest> Create(Project production, Project tool,
        IReadOnlyDictionary<string, string> properties)
    {
        var repository = Directory.GetParent(Path.GetDirectoryName(production.FilePath!)!)!.FullName;
        var projects = await LoadGraph(production);
        var ownership = ReadOwnership(repository);
        var owners = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var toolGraph = await LoadGraph(tool);
        var salt = new SortedSet<string>(StringComparer.Ordinal) { "target-source-v2" };
        foreach (var property in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            salt.Add("property:" + property.Key + "=" + PortableValue(property.Value, repository));
        foreach (var assembly in new[] { typeof(Compilation).Assembly, typeof(CSharpCompilation).Assembly,
                     typeof(Workspace).Assembly, typeof(MSBuildWorkspace).Assembly, typeof(Microsoft.Build.Locator.MSBuildLocator).Assembly }.Distinct())
            salt.Add("compiler:" + assembly.GetName().FullName + ":" +
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        var sourceAssemblies = projects.Concat(toolGraph).Select(item => item.Compilation.Assembly.Identity).ToHashSet();
        foreach (var (project, compilation) in projects.Concat(toolGraph).DistinctBy(item => item.Project.Id))
        {
            var projectPath = Relative(repository, project.FilePath!);
            salt.Add("project:" + projectPath + ":" + Hash(File.ReadAllText(project.FilePath!)));
            salt.Add("options:" + projectPath + ":" + CompilationOptions(compilation, (CSharpParseOptions)project.ParseOptions!));
            foreach (var reference in project.ProjectReferences)
                salt.Add("project-reference:" + projectPath + ":" + Relative(repository,
                    project.Solution.GetProject(reference.ProjectId)!.FilePath!));
            foreach (var reference in compilation.References)
            {
                var symbol = compilation.GetAssemblyOrModuleSymbol(reference)
                    ?? throw new InvalidDataException($"Unresolved build reference in {projectPath}.");
                var identity = symbol.ToDisplayString();
                // A workspace source project remains a source input even if Roslyn uses its reference assembly.
                var isSource = symbol is IAssemblySymbol assembly && sourceAssemblies.Contains(assembly.Identity);
                var input = reference is PortableExecutableReference binary && !isSource
                    ? PortableValue(binary.FilePath!, repository) + ":" + HashFile(binary.FilePath!) : "source";
                salt.Add("reference:" + projectPath + ":" + identity + ":" + input +
                    ":" + reference.Properties.Kind + ":" + reference.Properties.EmbedInteropTypes + ":" +
                    string.Join(',', reference.Properties.Aliases.Order(StringComparer.Ordinal)));
            }
            // Generator assemblies and their real project inputs are compiler inputs; generated C# is an output.
            foreach (var reference in project.AnalyzerReferences)
            {
                var path = reference.FullPath
                    ?? throw new InvalidDataException($"Analyzer reference has no input path in {projectPath}.");
                salt.Add("analyzer:" + projectPath + ":" + PortableValue(path, repository) + ":" + HashFile(path));
                foreach (var generator in reference.GetGenerators(project.Language))
                {
                    var type = generator.GetGeneratorType();
                    salt.Add("generator:" + projectPath + ":" + type.Assembly.GetName().FullName + ":" + type.FullName);
                }
            }
            foreach (var document in project.AdditionalDocuments.Concat<TextDocument>(project.AnalyzerConfigDocuments))
                salt.Add("compiler-input:" + projectPath + ":" + PortableValue(document.FilePath!, repository) + ":" +
                    Hash((await document.GetTextAsync()).ToString()));
            // Compilation-wide directives come only from the evaluated Compile inputs.
            foreach (var document in project.Documents)
            {
                var unit = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync()
                    ?? throw new InvalidDataException($"Compile document has no syntax root: {document.FilePath}."));
                foreach (var directive in unit.Usings.Where(item => item.GlobalKeyword.RawKind != 0))
                    salt.Add("global-using:" + projectPath + ":" + directive.WithoutTrivia().ToFullString());
            }
        }
        foreach (var name in new[] { "global.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.Config" })
        {
            var path = Path.Combine(repository, name);
            if (File.Exists(path)) salt.Add("configuration:" + name + ":" + Hash(File.ReadAllText(path)));
        }
        // The algorithm's own Compile items are the salt, independent of its built assembly.
        foreach (var (project, _) in toolGraph.Where(item => item.Project.Id == tool.Id))
            foreach (var document in project.Documents)
                salt.Add("tool-source:" + Relative(repository, document.FilePath!) + ":" + Hash((await document.GetTextAsync()).ToString()));

        var sources = new List<Source>();
        var roots = new List<Root>();
        using var evaluation = new Microsoft.Build.Evaluation.ProjectCollection(new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase));
        foreach (var (project, compilation) in projects)
        {
            var projectPath = Relative(repository, project.FilePath!);
            var generated = GeneratedSources(repository, evaluation.LoadProject(project.FilePath!));
            var sourceGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var document in project.Documents)
            {
                var path = Relative(repository, document.FilePath!);
                if (path.Split('/').Contains("tests", StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Production Compile contains a test source: {path}.");
                var group = SourceGroup(path, projectPath, generated);
                sourceGroups.Add(document.FilePath!, group);
                if (group.Length != 0)
                {
                    sources.Add(new(path, Hash((await document.GetTextAsync()).ToString()), group));
                }
            }
            foreach (var type in Types(compilation.Assembly.GlobalNamespace))
            {
                if (!type.IsAbstract && !type.IsGenericType)
                {
                    var ownerDeclarations = type.GetAttributes().Where(attribute => IsRegistration(attribute.AttributeClass)).ToArray();
                    if (ownerDeclarations.Length != 0)
                    {
                        var declaredOwners = ownerDeclarations.Select(attribute => OwnerOf(repository, ownership,
                            attribute.ApplicationSyntaxReference?.SyntaxTree.FilePath
                                ?? throw new InvalidDataException($"Registration {TypeKey(type)} has no source declaration.")))
                            .Distinct(StringComparer.Ordinal).ToArray();
                        if (declaredOwners.Length != 1)
                            throw new InvalidDataException($"Registration {TypeKey(type)} spans different source owners.");
                        owners.Add(TypeKey(type), declaredOwners[0]);
                    }
                }
                var registrations = type.GetAttributes().Where(attribute =>
                    Registrations.ContainsKey(attribute.AttributeClass?.ToDisplayString() ?? "")).ToArray();
                if (registrations.Length == 0) continue;
                var key = TypeKey(type);
                if (registrations.Length != 1 || type.IsAbstract || type.IsGenericType ||
                    !type.AllInterfaces.Any(item => item.ToDisplayString() == Registrations[registrations[0].AttributeClass!.ToDisplayString()]))
                    throw new InvalidDataException($"CPU root {key}: registration must describe one concrete, closed CPU contract implementation.");
                var declarations = type.DeclaringSyntaxReferences
                    .Where(declaration => sourceGroups.ContainsKey(declaration.SyntaxTree.FilePath)).ToArray();
                var groups = declarations.Select(declaration => sourceGroups[declaration.SyntaxTree.FilePath])
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (groups.Length != 1 || groups[0].Length == 0)
                    throw new InvalidDataException($"CPU root {key}: source ownership is undefined or spans responsibility directories: " +
                        string.Join(", ", declarations.Select(item => Relative(repository, item.SyntaxTree.FilePath))));
                roots.Add(new(key, groups[0]));
            }
        }
        if (roots.Count == 0) throw new InvalidDataException("The production graph contains no registered CPU roots.");
        var common = Hash(salt.Concat(sources.Where(item => item.Group == "shared")
            .Select(item => "source:" + item.Path + ":" + item.Hash)));
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in roots.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            var selected = sources.Where(source => source.Group == root.Group ||
                (root.Group.StartsWith("map:", StringComparison.Ordinal) &&
                 (source.Group == MapCommon(root.Group) ||
                  (root.Group.EndsWith(":interiors", StringComparison.Ordinal) && source.Group.StartsWith(MapPrefix(root.Group), StringComparison.Ordinal)))));
            if (!hashes.TryAdd(root.Key, Hash(selected.Select(item => "source:" + item.Path + ":" + item.Hash).Append("common:" + common))))
                throw new InvalidDataException($"Duplicate CPU root {root.Key}.");
        }
        foreach (var field in typeof(BakeCodeChains).GetFields().Where(item => item.IsLiteral && item.FieldType == typeof(string)))
        {
            var chain = (string)field.GetRawConstantValue()!;
            var responsibility = chain switch
            {
                BakeCodeChains.Collision => "runtime:kernel/collision",
                BakeCodeChains.Nightlight => "runtime:systems/nightlight",
                BakeCodeChains.Shared => "shared",
                _ => throw new InvalidDataException($"No source responsibility for CPU chain {chain}.")
            };
            hashes.Add(BakeCodeManifest.Chain(chain), responsibility == "shared" ? common :
                Hash(sources.Where(item => item.Group == responsibility)
                    .Select(item => "source:" + item.Path + ":" + item.Hash).Append("common:" + common)));
        }
        return new(BakeCodeManifest.CurrentSchema, hashes, owners);
    }

    private static bool IsRegistration(INamedTypeSymbol? type)
    {
        for (; type is not null; type = type.BaseType)
            if (type.ToDisplayString() == typeof(RegistrationAttribute).FullName) return true;
        return false;
    }

    // Same most-specific-first policy and glob semantics as check-source.ps1.
    private static (Regex Pattern, string Owner)[] ReadOwnership(string repository)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, "ownership.json")));
        return document.RootElement.GetProperty("rules").EnumerateArray()
            .Select(rule => (Pattern: rule.GetProperty("pattern").GetString()!, Owner: rule.GetProperty("owner").GetString()!))
            .OrderByDescending(rule => rule.Pattern.Count(c => c is not ('*' or '?')))
            .Select(rule => (new Regex("^" + Regex.Escape(rule.Pattern.Replace('\\', '/'))
                .Replace("\\*\\*", "__DOUBLE_STAR__").Replace("\\*", "[^/]*")
                .Replace("__DOUBLE_STAR__", ".*").Replace("\\?", "[^/]") + "$", RegexOptions.CultureInvariant), rule.Owner))
            .ToArray();
    }

    private static string OwnerOf(string repository, (Regex Pattern, string Owner)[] rules, string source)
    {
        var path = Relative(repository, source);
        foreach (var rule in rules)
            if (rule.Pattern.IsMatch(path))
                return !string.IsNullOrWhiteSpace(rule.Owner) ? rule.Owner
                    : throw new InvalidDataException($"Source owner is empty: {path}.");
        throw new InvalidDataException($"Registered source has no ownership rule: {path}.");
    }

    // The CPU fingerprint responsibility policy. Membership always intersects the actual production Compile graph.
    private static string SourceGroup(string path, string project, IReadOnlySet<string> generated)
    {
        // SDK Compile items are recognized by their evaluated MSBuild paths.
        if (generated.Contains(path))
        {
            if (project is "game/contracts/Campus.Contracts.csproj" or "game/contracts/Runtime/Campus.Contracts.Runtime.csproj" or "game/gen/Campus.Gen.csproj") return "shared";
            if (project.StartsWith("game/gen/maps/", StringComparison.Ordinal))
                return "map:" + project["game/gen/maps/".Length..].Split('/')[0] + ":common";
            if (project == "game/Campus.csproj") return ""; // runtime assembly metadata and build information
            throw new InvalidDataException($"Generated Compile item belongs to an unknown production project: {project}: {path}.");
        }
        if (path.Split('/').Any(part => part is "obj" or "bin" or ".godot"))
            throw new InvalidDataException($"Unrecognized generated production Compile item: {project}: {path}.");
        if (path.StartsWith("game/contracts/", StringComparison.Ordinal)) return "shared";
        if (path.StartsWith("game/gen/", StringComparison.Ordinal) && !path.StartsWith("game/gen/maps/", StringComparison.Ordinal)) return "shared";
        if (path.StartsWith("game/kernel/bake/", StringComparison.Ordinal) ||
            path is "game/kernel/map/data/GlobalBuildInputs.cs" or
                "game/kernel/map/data/MapClaimCapture.cs" or
                "game/kernel/map/data/GenerationInputCapture.cs" or
                "game/kernel/map/data/MapDataSource.cs" or
                "game/kernel/map/data/MapWorldLoading.cs" or
                "game/kernel/map/data/MapContext.cs" ||
            path == "game/kernel/lod/ModuleTileCatalog.cs") return "shared";
        if (path.StartsWith("game/kernel/collision/", StringComparison.Ordinal)) return "runtime:kernel/collision";
        if (path.StartsWith("game/systems/nightlight/", StringComparison.Ordinal)) return "runtime:systems/nightlight";
        if (path.StartsWith("game/gen/maps/", StringComparison.Ordinal))
        {
            var parts = path["game/gen/maps/".Length..].Split('/');
            if (parts.Length < 2) return "shared"; // common map-layer Compile items
            return "map:" + parts[0] + ":" + (parts.Length == 2 ? "common" : parts[1]);
        }
        // Explicit non-CPU responsibilities. New homes require a decision here instead of a silent cache hole.
        foreach (var directory in new[]
        {
            "game/audio/core/", "game/camera/", "game/flow/", "game/gates/", "game/hud/", "game/stage/",
            "game/style/palette/", "game/style/shaders/", "game/style/text/",
            "game/kernel/boot/", "game/kernel/frame/", "game/kernel/inspect/", "game/kernel/instances/",
            "game/kernel/lighting/", "game/kernel/lod/", "game/kernel/map/", "game/kernel/materials/",
            "game/kernel/post/", "game/kernel/quality/", "game/kernel/renderer/", "game/kernel/save/",
            "game/kernel/settings/", "game/kernel/stream/", "game/kernel/world/",
            "game/kernel/registry/", "game/kernel/stubs/",
            "game/systems/atlas/", "game/systems/fauna/", "game/systems/sky/", "game/systems/vegetation/",
            "game/systems/water/", "game/systems/weather/"
        })
            if (path.StartsWith(directory, StringComparison.Ordinal)) return "";
        if (path is "game/addons/gdUnit4/src/dotnet/GdUnit4CSharpApi.cs" or
            "game/gdunit4_testadapter_v5/GdUnit4TestRunnerScene.cs") return "";
        throw new InvalidDataException($"Unassigned production Compile item: {project}: {path}. Assign a CPU source group or an explicit non-CPU responsibility.");
    }

    private static HashSet<string> GeneratedSources(string repository, Microsoft.Build.Evaluation.Project project)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in new[] { "GeneratedAssemblyInfoFile", "TargetFrameworkMonikerAssemblyAttributesPath", "GeneratedGlobalUsingsFile" })
        {
            var path = project.GetPropertyValue(property);
            if (path.Length != 0) result.Add(Relative(repository, Path.GetFullPath(path, project.DirectoryPath)));
        }
        if (Relative(repository, project.FullPath) == "game/Campus.csproj")
            result.Add(Relative(repository, Path.GetFullPath(Path.Combine(project.GetPropertyValue("IntermediateOutputPath"), "BuildInfo.g.cs"), project.DirectoryPath)));
        return result;
    }

    private static string MapPrefix(string group) => group[..(group.LastIndexOf(':') + 1)];
    private static string MapCommon(string group) => MapPrefix(group) + "common";

    private static async Task<List<(Project Project, Compilation Compilation)>> LoadGraph(Project root)
    {
        var result = new List<(Project, Compilation)>();
        var visited = new HashSet<ProjectId>();
        async Task Add(Project project)
        {
            if (!visited.Add(project.Id)) return;
            foreach (var reference in project.ProjectReferences)
                await Add(project.Solution.GetProject(reference.ProjectId)
                    ?? throw new InvalidDataException($"Missing source project reference {reference.ProjectId}."));
            var compilation = await project.GetCompilationAsync()
                ?? throw new InvalidDataException($"No production C# compilation for {project.FilePath}.");
            result.Add((project, compilation));
        }
        await Add(root);
        return result;
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol scope)
    {
        foreach (var type in scope.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in Types(type)) yield return nested;
        }
        if (scope is INamespaceSymbol ns)
            foreach (var child in ns.GetNamespaceMembers())
                foreach (var type in Types(child)) yield return type;
    }

    private static string TypeKey(INamedTypeSymbol type)
    {
        var names = new Stack<string>();
        for (var current = type; current is not null; current = current.ContainingType) names.Push(current.MetadataName);
        var ns = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + ".";
        return BakeCodeManifest.TypeKey(type.ContainingAssembly.Name, ns + string.Join('+', names));
    }

    private static string CompilationOptions(Compilation compilation, CSharpParseOptions parse)
    {
        var options = (CSharpCompilationOptions)compilation.Options;
        return string.Join("|", compilation.Assembly.Identity, options.OutputKind, options.Platform, options.OptimizationLevel,
            options.CheckOverflow, options.AllowUnsafe, options.NullableContextOptions, options.Deterministic,
            options.ConcurrentBuild, options.GeneralDiagnosticOption, options.WarningLevel, options.MetadataImportOptions,
            options.MainTypeName, options.ModuleName, options.ReportSuppressedDiagnostics, options.PublicSign,
            parse.LanguageVersion, parse.Kind, parse.DocumentationMode,
            string.Join(',', parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal)),
            string.Join(',', options.Usings.Order(StringComparer.Ordinal)),
            string.Join(',', options.SpecificDiagnosticOptions.OrderBy(item => item.Key, StringComparer.Ordinal)),
            string.Join(',', parse.Features.OrderBy(item => item.Key, StringComparer.Ordinal)));
    }

    private static string Relative(string repository, string path)
    {
        var relative = Path.GetRelativePath(repository, path).Replace('\\', '/');
        if (Path.IsPathRooted(relative) || relative.StartsWith("../", StringComparison.Ordinal))
            throw new InvalidDataException($"Production source/project lies outside the repository: {path}.");
        return relative;
    }

    private static string PortableValue(string value, string repository)
    {
        if (!Path.IsPathFullyQualified(value)) return value.Replace('\\', '/');
        var path = value.Replace('\\', '/');
        var relative = Path.GetRelativePath(repository, value).Replace('\\', '/');
        if (!relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative)) return relative;
        foreach (var marker in new[] { "/packages/", "/packs/", "/sdk/", "/shared/" })
        {
            var index = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) return path[(index + 1)..]; // includes the resolved package/SDK version, excludes machine roots
        }
        throw new InvalidDataException($"Build reference/property has no portable version identity: {value}.");
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string HashFile(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static string Hash(IEnumerable<string> values) => Hash(string.Join('\0', values.Order(StringComparer.Ordinal)));

}


