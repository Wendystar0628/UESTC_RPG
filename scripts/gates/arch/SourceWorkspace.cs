// Materializes the native project graph and semantic inputs once for the G4 source rules.
// 用法：由 SourceModel 加载；仅执行 MSBuildWorkspace 设计时项目加载。
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Campus.Arch.Tests;

internal sealed class SourceWorkspace(Solution solution, Dictionary<ProjectId, Compilation> compilations, SourceFile[] files)
{
    public Solution Solution { get; } = solution;
    public Dictionary<ProjectId, Compilation> Compilations { get; } = compilations;
    public SourceFile[] Files { get; } = files;

    public static SourceWorkspace Load(string root)
    {
        RegisterMSBuild(root);
        return LoadRegistered(root).GetAwaiter().GetResult();
    }

    private static void RegisterMSBuild(string root)
    {
        if (MSBuildLocator.IsRegistered) return;
        var instance = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions
        {
            WorkingDirectory = root,
            DiscoveryTypes = DiscoveryType.DotNetSdk
        }).OrderByDescending(instance => instance.Version).FirstOrDefault()
            ?? throw new InvalidOperationException($"No .NET SDK matching {Path.Combine(root, "global.json")} is available for G4.");
        MSBuildLocator.RegisterInstance(instance);
    }

    // Keep the workspace's MSBuild dependency out of the registration method's JIT load.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<SourceWorkspace> LoadRegistered(string root)
    {
        using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
        {
            ["Configuration"] = "Debug",
            ["DesignTimeBuild"] = "true",
            ["SkipCompilerExecution"] = "true",
            ["BuildProjectReferences"] = "false"
        });
        workspace.LoadMetadataForReferencedProjects = false;
        var diagnostics = new ConcurrentQueue<WorkspaceDiagnostic>();
        using var registration = workspace.RegisterWorkspaceFailedHandler(args => diagnostics.Enqueue(args.Diagnostic));
        try
        {
            var entries = Directory.EnumerateFiles(Path.Combine(root, "game"), "*.csproj")
                .Where(path => Path.GetFileName(path) == "Campus.csproj" || path.EndsWith(".Tests.csproj", StringComparison.Ordinal))
                .Append(Path.Combine(root, "scripts", "gates", "arch", "Campus.Arch.Tests.csproj"))
                .Order(StringComparer.Ordinal).ToArray();
            foreach (var path in entries)
            {
                if (workspace.CurrentSolution.Projects.Any(project => string.Equals(project.FilePath, path, StringComparison.OrdinalIgnoreCase))) continue;
                await workspace.OpenProjectAsync(path).ConfigureAwait(false);
                RequireNoFailures(workspace, diagnostics);
            }
            var loaded = workspace.CurrentSolution;
            var compilations = new Dictionary<ProjectId, Compilation>();
            var files = new List<SourceFile>();
            foreach (var project in loaded.Projects.OrderBy(project => project.FilePath, StringComparer.Ordinal))
            {
                var compilation = await project.GetCompilationAsync().ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"G4 could not obtain a compilation for {project.FilePath}.");
                RequireNoFailures(workspace, diagnostics);
                var errors = compilation.GetDiagnostics().Where(diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error && !diagnostic.IsWarningAsError).ToArray();
                if (errors.Length != 0) throw new InvalidOperationException($"G4 project {project.FilePath}:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
                compilations.Add(project.Id, compilation);
                foreach (var document in project.Documents)
                {
                    if (document.FilePath is not { } path) continue;
                    var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                    if (relative.Split('/').Any(part => part.StartsWith('.') || part is "obj" or "bin" or "addons" ||
                        part.StartsWith("gdunit4_testadapter", StringComparison.Ordinal))) continue;
                    var tree = await document.GetSyntaxTreeAsync().ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"G4 could not obtain a syntax tree for {project.FilePath}: {path}.");
                    files.Add(new SourceFile(relative, tree, compilation.GetSemanticModel(tree)));
                }
            }
            RequireNoFailures(workspace, diagnostics);
            if (files.Count == 0) throw new InvalidOperationException("G4 source model has no project sources.");
            return new(loaded, compilations, files.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray());
        }
        catch (Exception error)
        {
            var failures = Failures(workspace, diagnostics);
            if (failures.Length == 0) throw;
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures), error);
        }
    }

    private static string[] Failures(MSBuildWorkspace workspace, ConcurrentQueue<WorkspaceDiagnostic> diagnostics) =>
        workspace.Diagnostics.Concat(diagnostics).Where(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            .Select(diagnostic => diagnostic is ProjectDiagnostic project
                ? $"{workspace.CurrentSolution.GetProject(project.ProjectId)?.FilePath ?? project.ProjectId.ToString()}: {diagnostic.Message}"
                : diagnostic.Message).Distinct(StringComparer.Ordinal).ToArray();

    private static void RequireNoFailures(MSBuildWorkspace workspace, ConcurrentQueue<WorkspaceDiagnostic> diagnostics)
    {
        var failures = Failures(workspace, diagnostics);
        if (failures.Length != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }
}
