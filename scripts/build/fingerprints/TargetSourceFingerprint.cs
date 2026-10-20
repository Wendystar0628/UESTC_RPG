// Hashes directory-owned production source, using Roslyn only for the real project graph and registration types.
// 用法：由 Program 调用；分组规则只在 SourceGroup 定义，运行时只读生成的 manifest。
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
        ["Campus.Contracts.ContentModuleAttribute"] = "Campus.Contracts.IContentModule",
        ["Campus.Contracts.InteriorProviderAttribute"] = "Campus.Contracts.IInteriorProvider",
        ["Campus.Contracts.Bake.BakeProducerAttribute"] = "Campus.Contracts.Bake.IBakeProducer"
    };

    public static async Task<IReadOnlyDictionary<string, string>> Create(Project production, Project tool,
        IReadOnlyDictionary<string, string> properties)
    {
        var repository = Directory.GetParent(Path.GetDirectoryName(production.FilePath!)!)!.FullName;
        var projects = await LoadGraph(production);
        var toolGraph = await LoadGraph(tool);
        var salt = new SortedSet<string>(StringComparer.Ordinal) { "target-source-v1" };
        foreach (var property in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            salt.Add("property:" + property.Key + "=" + PortableValue(property.Value, repository));
        foreach (var assembly in new[] { typeof(Compilation).Assembly, typeof(CSharpCompilation).Assembly,
                     typeof(Workspace).Assembly, typeof(MSBuildWorkspace).Assembly, typeof(Microsoft.Build.Locator.MSBuildLocator).Assembly }.Distinct())
            salt.Add("compiler:" + assembly.GetName().FullName + ":" +
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
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
                var identity = compilation.GetAssemblyOrModuleSymbol(reference)?.ToDisplayString()
                    ?? throw new InvalidDataException($"Unresolved build reference in {projectPath}.");
                salt.Add("reference:" + projectPath + ":" + identity + ":" +
                    (reference is PortableExecutableReference binary ? PortableValue(binary.FilePath!, repository) : "source") +
                    ":" + reference.Properties.Kind + ":" + reference.Properties.EmbedInteropTypes + ":" +
                    string.Join(',', reference.Properties.Aliases.Order(StringComparer.Ordinal)));
            }
            // These compilation-wide directives can affect a selected source file regardless of their source home.
            foreach (var tree in compilation.SyntaxTrees)
            {
                var unit = (CompilationUnitSyntax)await tree.GetRootAsync();
                foreach (var directive in unit.Usings.Where(item => item.GlobalKeyword.RawKind != 0))
                    salt.Add("global-using:" + projectPath + ":" + directive.WithoutTrivia().ToFullString());
            }
        }
        foreach (var name in new[] { "global.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.Config" })
        {
            var path = Path.Combine(repository, name);
            if (File.Exists(path)) salt.Add("configuration:" + name + ":" + Hash(File.ReadAllText(path)));
        }
        // The algorithm's own Compile items are the salt; binaries and MVIDs never enter the key.
        foreach (var (_, compilation) in toolGraph.Where(item => item.Project.Id == tool.Id))
            foreach (var tree in compilation.SyntaxTrees)
                salt.Add("tool-source:" + Relative(repository, tree.FilePath) + ":" + Hash((await tree.GetTextAsync()).ToString()));

        var sources = new List<Source>();
        var roots = new List<Root>();
        foreach (var (project, compilation) in projects)
        {
            var projectPath = Relative(repository, project.FilePath!);
            foreach (var tree in compilation.SyntaxTrees)
            {
                var path = Relative(repository, tree.FilePath);
                if (path.Split('/').Contains("tests", StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Production Compile contains a test source: {path}.");
                var group = SourceGroup(path, projectPath);
                if (group.Length != 0) sources.Add(new(path, Hash((await tree.GetTextAsync()).ToString()), group));
            }
            foreach (var type in Types(compilation.Assembly.GlobalNamespace))
            {
                var registrations = type.GetAttributes().Where(attribute =>
                    Registrations.ContainsKey(attribute.AttributeClass?.ToDisplayString() ?? "")).ToArray();
                if (registrations.Length == 0) continue;
                var key = TypeKey(type);
                if (registrations.Length != 1 || type.IsAbstract || type.IsGenericType ||
                    !type.AllInterfaces.Any(item => item.ToDisplayString() == Registrations[registrations[0].AttributeClass!.ToDisplayString()]))
                    throw new InvalidDataException($"CPU root {key}: registration must describe one concrete, closed CPU contract implementation.");
                var groups = type.DeclaringSyntaxReferences.Select(declaration =>
                    SourceGroup(Relative(repository, declaration.SyntaxTree.FilePath), projectPath)).Distinct(StringComparer.Ordinal).ToArray();
                if (groups.Length != 1 || groups[0].Length == 0)
                    throw new InvalidDataException($"CPU root {key}: source ownership is undefined or spans responsibility directories: " +
                        string.Join(", ", type.DeclaringSyntaxReferences.Select(item => Relative(repository, item.SyntaxTree.FilePath))));
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
                _ => "shared"
            };
            hashes.Add(BakeCodeManifest.Chain(chain), Hash(sources.Where(item => item.Group == responsibility)
                .Select(item => "source:" + item.Path + ":" + item.Hash).Append("common:" + common)));
        }
        return hashes;
    }

    // The only source-ownership policy. Membership always intersects the actual production Compile graph.
    private static string SourceGroup(string path, string project)
    {
        if (path.StartsWith("game/contracts/", StringComparison.Ordinal)) return "shared";
        if (path.StartsWith("game/gen/", StringComparison.Ordinal) && !path.StartsWith("game/gen/maps/", StringComparison.Ordinal)) return "shared";
        if (path.StartsWith("pipeline/map/", StringComparison.Ordinal) ||
            path.StartsWith("game/kernel/bake/", StringComparison.Ordinal) ||
            path.StartsWith("game/kernel/map/data/", StringComparison.Ordinal) ||
            path.StartsWith("game/kernel/registry/", StringComparison.Ordinal) ||
            path.StartsWith("game/kernel/stubs/", StringComparison.Ordinal)) return "shared";
        if (path.StartsWith("game/kernel/collision/", StringComparison.Ordinal)) return "runtime:kernel/collision";
        if (path.StartsWith("game/systems/nightlight/", StringComparison.Ordinal)) return "runtime:systems/nightlight";
        if (path.StartsWith("game/gen/maps/", StringComparison.Ordinal))
        {
            var parts = path["game/gen/maps/".Length..].Split('/');
            if (parts.Length < 2) return "shared"; // common map-layer Compile items
            return "map:" + parts[0] + ":" + (parts.Length == 2 || parts[1] is "obj" or "bin" ? "common" : parts[1]);
        }
        // Linked SDK-generated Compile items belong to their source project, never to the whole Campus DLL.
        if (path.Contains("/obj/", StringComparison.Ordinal))
        {
            if (project.StartsWith("game/contracts/", StringComparison.Ordinal) || project == "game/gen/Campus.Gen.csproj") return "shared";
            if (project.StartsWith("game/gen/maps/", StringComparison.Ordinal))
                return "map:" + project["game/gen/maps/".Length..].Split('/')[0] + ":common";
        }
        return "";
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
    private static string Hash(IEnumerable<string> values) => Hash(string.Join('\0', values.Order(StringComparer.Ordinal)));

}


