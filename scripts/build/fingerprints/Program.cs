// Generates the packaged CPU-code manifest from production Compile items and registration attributes.
// 用法：dotnet Campus.CodeFingerprints.dll --project game/Campus.csproj --tool-project scripts/build/fingerprints/Campus.CodeFingerprints.csproj --output game/data/build-code-fingerprints.json --property Configuration=Debug
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Campus.Build.Fingerprints;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            MSBuildLocator.RegisterDefaults();
            await Generate(options);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Code fingerprints: {error}");
            return 1;
        }
    }

    private static async Task Generate(Options options)
    {
        using var workspace = MSBuildWorkspace.Create(options.Properties);
        workspace.LoadMetadataForReferencedProjects = false;
        var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var registration = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure) failures.Enqueue(args.Diagnostic.Message);
        });
        var production = await workspace.OpenProjectAsync(options.Project);
        var tool = await workspace.OpenProjectAsync(options.ToolProject);
        if (!failures.IsEmpty) throw new InvalidDataException(string.Join(Environment.NewLine, failures));
        var manifest = await TargetSourceFingerprint.Create(production, tool, options.Properties);
        if (!failures.IsEmpty) throw new InvalidDataException(string.Join(Environment.NewLine, failures));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            manifest, ContractJson.Options);
        var output = Path.GetFullPath(options.Output);
        if (File.Exists(output) && File.ReadAllBytes(output).AsSpan().SequenceEqual(bytes)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var temporary = output + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes);
            File.Move(temporary, output, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        Console.WriteLine($"Code fingerprints: wrote {manifest.Roots.Count} roots and {manifest.Registrations.Count} registration owners to {output}.");
    }

    private sealed record Options(string Project, string ToolProject, string Output, Dictionary<string, string> Properties)
    {
        public static Options Parse(string[] args)
        {
            string? project = null, toolProject = null, output = null;
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 == args.Length) throw new ArgumentException($"Missing value for {args[i]}.");
                var value = args[++i];
                switch (args[i - 1])
                {
                    case "--project": project = Path.GetFullPath(value); break;
                    case "--tool-project": toolProject = Path.GetFullPath(value); break;
                    case "--output": output = Path.GetFullPath(value); break;
                    case "--property":
                        var equals = value.IndexOf('=');
                        if (equals <= 0) throw new ArgumentException($"Invalid MSBuild property {value}.");
                        if (!properties.TryAdd(value[..equals], value[(equals + 1)..]))
                            throw new ArgumentException($"Duplicate MSBuild property {value[..equals]}.");
                        break;
                    default: throw new ArgumentException($"Unknown argument {args[i - 1]}.");
                }
            }
            if (project is null || toolProject is null || output is null || !File.Exists(project) || !File.Exists(toolProject))
                throw new ArgumentException("--project and --tool-project must name existing projects; --output is required.");
            if (!properties.ContainsKey("Configuration"))
                throw new ArgumentException("Pass the build's effective Configuration via --property.");
            return new(project, toolProject, output, properties);
        }
    }
}
