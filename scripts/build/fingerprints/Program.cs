// Generates CPU-code fingerprints and owns current build-input identity and launch receipts.
// 用法：dotnet Campus.CodeFingerprints.dll --project game/Campus.csproj --tool-project scripts/build/fingerprints/Campus.CodeFingerprints.csproj --output game/data/build-code-fingerprints.json --property Configuration=Debug --configuration-property Configuration=Debug；其他 mode 的构建调用见 Campus.csproj，启动只调 check。
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
        Options? options = null;
        try
        {
            options = Options.Parse(args);
            MSBuildLocator.RegisterDefaults();
            return await Run(options);
        }
        catch (FileNotFoundException error) when (options?.Mode is "check" or "complete")
        {
            return MissingInput(options!.Mode, error);
        }
        catch (DirectoryNotFoundException error) when (options?.Mode is "check" or "complete")
        {
            return MissingInput(options!.Mode, error);
        }
        catch (Exception error)
        {
            return CommandFailure(error);
        }
    }

    private static int MissingInput(string mode, [FailureHandler("ENG-ARCH")] Exception error)
    {
        BuildInputIdentity.Warn($"An input, output or receipt disappeared: {error.Message}");
        return mode == "check" ? 3 : 0;
    }

    private static async Task<int> Run(Options options)
    {
        var identity = new BuildInputIdentity(options.Project, options.ToolProject, options.Output, options.Properties, options.TargetPath);
        switch (options.Mode)
        {
            case "generate": await Generate(options); return 0;
            case "inputs":
                foreach (var input in identity.Capture().Inputs) Console.WriteLine(input);
                return 0;
            case "identity": Console.WriteLine(identity.Capture().Identity); return 0;
            case "check": return identity.Check() ? 0 : 3;
            case "begin": Console.WriteLine(identity.Begin(rebuild: false)); return 0;
            case "begin-rebuild": Console.WriteLine(identity.Begin(rebuild: true)); return 0;
            case "complete":
                identity.Complete(options.Operation ?? throw new ArgumentException("--operation is required for complete."));
                return 0;
            default: throw new ArgumentException($"Unknown mode {options.Mode}.");
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
        var tool = await workspace.OpenProjectAsync(options.ToolProject);
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

    private sealed record Options(string Mode, string Project, string ToolProject, string Output, Dictionary<string, string> Properties,
        Dictionary<string, string> Configuration, string? Operation, string? TargetPath)
    {
        public static Options Parse(string[] args)
        {
            string? project = null, toolProject = null, output = null, operation = null, targetPath = null;
            var mode = "generate";
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 == args.Length) throw new ArgumentException($"Missing value for {args[i]}.");
                var value = args[++i];
                switch (args[i - 1])
                {
                    case "--mode": mode = value; break;
                    case "--operation": operation = value; break;
                    case "--target-path": targetPath = value; break;
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
            return new(mode, project, toolProject, output, properties, configuration, operation, targetPath);
        }
    }
}
