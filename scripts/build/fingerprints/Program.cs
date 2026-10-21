// Generates the business CPU-code fingerprint manifest from the real production and tool projects.
// 用法：由 Campus.csproj 在输入变化时调用；传入 --project、--tool-project、--output 和实际构建配置。
using System.Text.Json;
using System.Text;
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
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            var options = Options.Parse(args);
            MSBuildLocator.RegisterDefaults();
            await Generate(options);
            return 0;
        }
        catch (Exception error)
        {
            return CommandFailure(error);
        }
    }

    // The build command reports the complete failure and terminates unsuccessfully (ENG-ARCH, STD-CODE).
    private static int CommandFailure([FailureHandler("ENG-ARCH")] Exception error)
    {
        Console.Error.WriteLine($"Code fingerprints: {error}");
        return 1;
    }

    private static async Task Generate(Options options)
    {
        var properties = new Dictionary<string, string>(options.Properties, StringComparer.OrdinalIgnoreCase);
        properties.Remove("TargetPath");
        using var workspace = MSBuildWorkspace.Create(properties);
        workspace.LoadMetadataForReferencedProjects = false;
        var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var registration = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure) failures.Enqueue(args.Diagnostic.Message);
        });
        var production = await workspace.OpenProjectAsync(options.Project);
        var tool = workspace.CurrentSolution.Projects.Single(project =>
            string.Equals(Path.GetFullPath(project.FilePath!), options.ToolProject, StringComparison.OrdinalIgnoreCase));
        if (!failures.IsEmpty) throw new InvalidDataException(string.Join(Environment.NewLine, failures));
        var manifest = await TargetSourceFingerprint.Create(production, tool, properties, options.Configuration);
        if (!failures.IsEmpty) throw new InvalidDataException(string.Join(Environment.NewLine, failures));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            manifest, ContractJson.Options);
        var output = Path.GetFullPath(options.Output);
        if (File.Exists(output) && File.ReadAllBytes(output).AsSpan().SequenceEqual(bytes)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var stagingPath = output + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(stagingPath, bytes);
            File.Move(stagingPath, output, overwrite: true);
        }
        finally
        {
            if (File.Exists(stagingPath)) File.Delete(stagingPath);
        }
        Console.WriteLine($"Code fingerprints: wrote {manifest.Roots.Count} roots and {manifest.Registrations.Count} registration owners to {output}.");
    }

    private sealed record Options(string Project, string ToolProject, string Output, Dictionary<string, string> Properties,
        Dictionary<string, string> Configuration)
    {
        public static Options Parse(string[] args)
        {
            string? project = null, toolProject = null, output = null;
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 == args.Length) throw new ArgumentException($"Missing value for {args[i]}.");
                var value = args[++i];
                switch (args[i - 1])
                {
                    case "--project": project = Path.GetFullPath(value); break;
                    case "--tool-project": toolProject = Path.GetFullPath(value); break;
                    case "--output": output = Path.GetFullPath(value); break;
                    case "--encoded-property":
                        value = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                        goto case "--property";
                    case "--property":
                    case "--configuration-property":
                        var equals = value.IndexOf('=');
                        if (equals <= 0) throw new ArgumentException($"Invalid MSBuild property {value}.");
                        var destination = args[i - 1] == "--configuration-property" ? configuration : properties;
                        if (!destination.TryAdd(value[..equals], value[(equals + 1)..]))
                            throw new ArgumentException($"Duplicate MSBuild property {value[..equals]}.");
                        break;
                    default: throw new ArgumentException($"Unknown argument {args[i - 1]}.");
                }
            }
            if (project is null || toolProject is null || output is null)
                throw new ArgumentException("--project, --tool-project and --output are required.");
            if (!properties.ContainsKey("Configuration"))
                throw new ArgumentException("Pass the build's effective Configuration via --property.");
            return new(project, toolProject, output, properties, configuration);
        }
    }
}
