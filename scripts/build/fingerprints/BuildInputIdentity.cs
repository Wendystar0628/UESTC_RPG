// Collects the current evaluated build graph and owns the existing launch success receipt.
// 用法：由 Program 的 inputs/identity/check/begin/begin-rebuild/complete 命令调用；只读分支不执行 MSBuild target。
using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using Campus.Contracts;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using NuGet.Configuration;
using NuGet.ProjectModel;

namespace Campus.Build.Fingerprints;

// MSBuild's compile cache hashes item names and uses timestamps; content and successful-build identity need this owner.
internal sealed class BuildInputIdentity
{
    private const int ReceiptFormat = 2;
    private readonly string _project;
    private readonly string _toolProject;
    private readonly string _manifest;
    private readonly string _repository;
    private readonly Dictionary<string, string> _properties;
    private readonly string _receipt;
    private readonly string _targetPath;

    internal BuildInputIdentity(string project, string toolProject, string manifest,
        IReadOnlyDictionary<string, string> properties, string? targetPath)
    {
        _project = project;
        _toolProject = toolProject;
        _manifest = manifest;
        _repository = Directory.GetParent(Path.GetDirectoryName(project)!)!.FullName;
        _properties = new(properties, StringComparer.OrdinalIgnoreCase);
        _receipt = Path.Combine(_repository, "output", "start_game", "build.succeeded");
        _targetPath = targetPath ?? FormalTargetPath;
    }

    internal sealed record Snapshot(string Identity, string[] Inputs, string[] Outputs);
    private sealed record Receipt(int Format, string Status, Dictionary<string, string> BuildProperties, string Properties, string TargetPath, string Identity,
        string[] Inputs, string[] Outputs, string ManifestIdentity, bool Rebuild, bool Verified, string Operation);

    // ProjectGraph evaluates imports, project references and globs afresh; it never submits a build request.
    internal Snapshot Capture()
    {
        var inputs = new SortedSet<string>(StringComparer.Ordinal);
        var outputs = new SortedSet<string>(StringComparer.Ordinal);
        var values = new SortedSet<string>(StringComparer.Ordinal) { "campus-build-input-v2" };
        var globals = new Dictionary<string, string>(_properties, StringComparer.OrdinalIgnoreCase);
        globals.Remove("TargetPath"); // Output metadata must never replace a referenced project's TargetPath.
        using var collection = new ProjectCollection();
        var entries = new[]
        {
            new ProjectGraphEntryPoint(_project, globals),
            new ProjectGraphEntryPoint(_toolProject, ToolProperties(globals))
        };
        var graph = new ProjectGraph(entries, collection,
            (path, properties, context) => context.LoadProject(path, properties, toolsVersion: null).CreateProjectInstance());
        foreach (var node in graph.ProjectNodes.OrderBy(item => item.ProjectInstance.FullPath, StringComparer.Ordinal))
        {
            var instance = node.ProjectInstance;
            var project = collection.LoadProject(instance.FullPath, instance.GlobalProperties, toolsVersion: null);
            CollectProject(project, inputs, values);
            foreach (var reference in node.ProjectReferences)
                values.Add("edge:" + instance.FullPath + ":" + reference.ProjectInstance.FullPath);
            var target = project.GetPropertyValue("TargetPath");
            if (target.Length != 0) outputs.Add(FullPath(project, target));
            outputs.Add(FullPath(project, Path.Combine(project.GetPropertyValue("IntermediateOutputPath"),
                project.GetPropertyValue("MSBuildProjectFile") + ".FileListAbsolute.txt")));
        }
        foreach (var input in inputs) values.Add("file:" + input + ":" + TargetSourceFingerprint.HashFile(input));
        return new(TargetSourceFingerprint.Hash(values), inputs.ToArray(), outputs.ToArray());
    }

    private static Dictionary<string, string> ToolProperties(Dictionary<string, string> properties)
    {
        var result = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { "TargetFramework", "RuntimeIdentifier", "SelfContained" }) result.Remove(name);
        return result;
    }

    private void CollectProject(Project project, SortedSet<string> inputs, SortedSet<string> values)
    {
        inputs.Add(project.FullPath);
        foreach (var import in project.Imports)
        {
            // NuGet's generated imports select restored inputs; the selected package files below are the sources.
            if (!IsIntermediate(project, import.ImportedProject.FullPath)) inputs.Add(import.ImportedProject.FullPath);
            // The imported SDK analyzer targets add their checked-in config files during CoreCompile.
            var configs = Path.Combine(Path.GetDirectoryName(import.ImportedProject.FullPath)!, "config");
            if (Directory.Exists(configs))
                foreach (var path in Directory.EnumerateFiles(configs, "*.globalconfig")) inputs.Add(path);
        }
        CollectCompilerOptions(project, inputs, values);
        var generated = TargetSourceFingerprint.GeneratedSources(_repository, project);
        foreach (var item in project.Items)
        {
            // The SDK's EditorConfigFiles contains existing discovered configs; GlobalAnalyzerConfigFiles is only a candidate list.
            if (item.ItemType is "Compile" or "AdditionalFiles" or "Analyzer" or "EditorConfigFiles" or
                "EmbeddedResource" or "Resource" or "CampusBuildInput" ||
                ((item.ItemType is "None" or "Content") && item.GetMetadataValue("CopyToOutputDirectory") is not ("" or "Never")))
            {
                var path = item.GetMetadataValue("FullPath");
                if (item.ItemType == "Compile" && generated.Contains(TargetSourceFingerprint.Relative(_repository, path))) continue;
                if (IsIntermediate(project, path))
                    throw new InvalidDataException($"Undeclared generated compiler input in {project.FullPath}: {path}.");
                inputs.Add(path);
                AddMetadata(project, item, values);
                if (item.ItemType == "Analyzer")
                    foreach (var dependency in Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.dll")) inputs.Add(dependency);
            }
            else if (item.ItemType is "Reference" or "ProjectReference" or "PackageReference" or "FrameworkReference" or
                     "Using" or "AssemblyAttribute" or "AssemblyMetadata" or "InternalsVisibleTo" or "CompilerVisibleProperty" or "CompilerVisibleItemMetadata" or "SourceRoot" or
                     "_NETCoreAppVersionsForDefines" or "SupportedNETFrameworkTargetFramework" or "SupportedNETStandardTargetFramework" or "SdkSupportedTargetPlatformVersion")
            {
                AddMetadata(project, item, values);
                if (item.ItemType == "CompilerVisibleProperty")
                    values.Add("generator-property:" + project.FullPath + ":" + item.EvaluatedInclude + "=" + project.GetPropertyValue(item.EvaluatedInclude));
                if (item.ItemType == "CompilerVisibleItemMetadata")
                    foreach (var source in project.GetItems(item.EvaluatedInclude))
                        values.Add("generator-metadata:" + project.FullPath + ":" + item.EvaluatedInclude + ":" + source.EvaluatedInclude + ":" +
                            item.GetMetadataValue("MetadataName") + "=" + source.GetMetadataValue(item.GetMetadataValue("MetadataName")));
                if (item.ItemType != "Reference") continue;
                var hint = item.GetMetadataValue("HintPath");
                if (hint.Length != 0) inputs.Add(FullPath(project, hint));
                else if (Path.IsPathFullyQualified(item.EvaluatedInclude)) inputs.Add(item.EvaluatedInclude);
                else throw new InvalidDataException($"Reference needs an explicit resolved input in {project.FullPath}: {item.EvaluatedInclude}.");
            }
        }
        // CoreCompile.Inputs omits response files and names target-resolved signing/resource properties.
        // Keep the raw file parameters as sources; their resolved intermediates are build outputs.
        foreach (var name in new[] { "CodeAnalysisRuleSet", "CompilerResponseFile", "ApplicationIcon", "ApplicationManifest", "Win32Resource",
                     "AssemblyOriginatorKeyFile", "KeyOriginatorFile", "SourceLink" })
        {
            var path = project.GetPropertyValue(name);
            if (path.Length != 0) inputs.Add(FullPath(project, path));
        }
        CollectRestore(project, inputs, values);
        CollectFrameworks(project, inputs);
        var roslyn = project.GetPropertyValue("RoslynTargetsPath");
        if (roslyn.Length != 0)
            foreach (var path in Directory.EnumerateFiles(Path.Combine(roslyn, "bincore"), "*", SearchOption.TopDirectoryOnly))
                inputs.Add(path);
        var sdkTasks = Path.Combine(project.GetPropertyValue("MSBuildSDKsPath"), "Microsoft.NET.Sdk", "tools", "net10.0");
        foreach (var path in Directory.EnumerateFiles(sdkTasks, "*.dll", SearchOption.TopDirectoryOnly)) inputs.Add(path);
        foreach (var name in new[] { "Microsoft.Build.Tasks.Core.dll", "Microsoft.Build.Utilities.Core.dll" })
            inputs.Add(Path.Combine(project.GetPropertyValue("MSBuildToolsPath"), name));
    }

    // Read the selected SDK's Csc parameter bindings; there is no second hand-written compiler-option table.
    private static void CollectCompilerOptions(Project project, SortedSet<string> inputs, SortedSet<string> values)
    {
        var compilerTask = Path.Combine(project.GetPropertyValue("RoslynTargetsPath"), "Microsoft.Build.Tasks.CodeAnalysis.dll");
        inputs.Add(compilerTask);
        var type = Assembly.LoadFrom(compilerTask).GetType("Microsoft.CodeAnalysis.BuildTasks.Csc", throwOnError: true)!;
        var outputs = type.GetProperties().Where(property => property.IsDefined(typeof(OutputAttribute), inherit: true))
            .Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var task in project.Targets["CoreCompile"].Tasks.Where(task => task.Name.Split('.').Last() == "Csc"))
            foreach (var parameter in task.Parameters.Where(parameter => !outputs.Contains(parameter.Key) && parameter.Key != "Sources"))
                values.Add("compiler-option:" + project.FullPath + ":" + parameter.Key + "=" + project.ExpandString(parameter.Value));
        CollectGeneratorInputs(project, values);
        values.Add("configuration:" + project.FullPath + ":" + project.GetPropertyValue("Configuration"));
    }

    private static void CollectGeneratorInputs(Project project, SortedSet<string> values)
    {
        // SDK targets contain batching expressions such as $(%(CompilerVisibleProperty.Identity)).
        // Read their named property inputs from the evaluated project, never expand or execute target bodies.
        // The target/import bytes and real item metadata are collected separately by CollectProject.
        var generators = new HashSet<string>(StringComparer.Ordinal)
        {
            "GenerateAssemblyInfo", "GetAssemblyAttributes", "GetAssemblyVersion", "AddSourceRevisionToInformationalVersion",
            "_GetUserSecretsAssemblyAttribute", "GenerateTargetFrameworkMonikerAttribute", "_SetTargetFrameworkMonikerAttribute",
            "GenerateGlobalUsings", "GenerateMSBuildEditorConfigFileShouldRun", "AddImplicitDefineConstants",
            "GenerateTargetFrameworkDefineConstants", "GenerateTargetPlatformDefineConstants", "GenerateNETCompatibleDefineConstants",
            "GeneratePlatformCompatibleDefineConstants", "_DisableDiagnosticTracing", "InitializeSourceRootMappedPaths", "_SetPathMapFromSourceRoots"
        };
        var properties = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in project.Imports.Select(import => import.ImportedProject).Append(project.Xml))
        {
            foreach (var target in root.Targets.Where(target => generators.Contains(target.Name) ||
                         target.Name.StartsWith("AddGlobalAnalyzerConfigForPackage_", StringComparison.Ordinal)))
            {
                // Also finds the receiver of property functions, e.g. $(Version.Trim()).
                // This only selects property names; MSBuild owns every property value and all item evaluation.
                foreach (Match reference in Regex.Matches(target.OuterElement, @"\$\(([A-Za-z_][A-Za-z_0-9]*)(?:\)|\.)"))
                    properties.Add(reference.Groups[1].Value);
                // SDK analyzer targets can expose more properties immediately before editorconfig generation.
                foreach (var item in target.Children.OfType<ProjectItemGroupElement>().SelectMany(group => group.Items)
                             .Where(item => item.ItemType == "CompilerVisibleProperty"))
                    foreach (var name in project.ExpandString(item.Include).Split(';', StringSplitOptions.RemoveEmptyEntries))
                        properties.Add(name);
            }
        }
        foreach (var name in properties)
            values.Add("generator-property:" + project.FullPath + ":" + name + "=" + project.GetPropertyValue(name));
    }

    private static void AddMetadata(Project project, ProjectItem item, SortedSet<string> values)
    {
        var key = "item:" + project.FullPath + ":" + item.ItemType + ":" + item.EvaluatedInclude;
        values.Add(key);
        foreach (var metadata in item.Metadata)
            values.Add(key + ":" + metadata.Name + "=" + metadata.EvaluatedValue);
    }

    private static bool IsIntermediate(Project project, string path)
    {
        var relative = Path.GetRelativePath(project.DirectoryPath, path).Replace('\\', '/');
        return relative.Split('/').Any(part => part is "obj" or "bin" or ".godot");
    }

    private static string FullPath(Project project, string path) => Path.GetFullPath(path, project.DirectoryPath);

    // NuGet's SDK reader owns the restore format; no targets, restore, package resolution or workspace compilation run here.
    private static void CollectRestore(Project project, SortedSet<string> inputs, SortedSet<string> values)
    {
        var assets = project.GetPropertyValue("ProjectAssetsFile");
        if (assets.Length == 0) throw new InvalidDataException($"Project has no restored assets: {project.FullPath}.");
        var restored = new LockFileFormat().Read(FullPath(project, assets))
            ?? throw new InvalidDataException($"Restore data cannot be read: {project.FullPath}.");
        var metadata = restored.PackageSpec?.RestoreMetadata
            ?? throw new InvalidDataException($"Restore metadata is unavailable: {project.FullPath}.");
        foreach (var config in metadata.ConfigFilePaths) inputs.Add(config);
        // DefaultSettings may create a user config. Discover only existing local configs, using NuGet's own names.
        for (DirectoryInfo? current = new(project.DirectoryPath); current is not null; current = current.Parent)
            foreach (var name in Settings.OrderedSettingsFileNames)
            {
                var config = Path.Combine(current.FullName, name);
                if (File.Exists(config)) inputs.Add(config);
            }
        var lockFile = project.GetPropertyValue("NuGetLockFilePath");
        if (lockFile.Length == 0) lockFile = "packages.lock.json";
        if (File.Exists(FullPath(project, lockFile))) inputs.Add(FullPath(project, lockFile));
        var target = restored.GetTarget(project.GetPropertyValue("TargetFramework"), project.GetPropertyValue("RuntimeIdentifier"))
            ?? throw new InvalidDataException($"Restore target is unavailable: {project.FullPath}.");
        values.Add("restore:" + project.FullPath + ":" + target.Name);
        foreach (var library in target.Libraries)
        {
            values.Add("resolved:" + project.FullPath + ":" + library.Name + ":" + library.Version + ":" + library.Type);
            if (library.Type != "package") continue;
            var package = restored.GetLibrary(library.Name, library.Version)
                ?? throw new InvalidDataException($"Restored package entry is missing: {library.Name}/{library.Version}.");
            var root = restored.PackageFolders.Select(folder => Path.Combine(folder.Path, package.Path))
                .FirstOrDefault(Directory.Exists)
                ?? throw new DirectoryNotFoundException($"Restored package is missing: {library.Name}/{library.Version}.");
            foreach (var file in package.Files.Where(path => Path.GetExtension(path) is ".dll" or ".props" or ".targets"))
                inputs.Add(Path.GetFullPath(Path.Combine(root, file)));
            foreach (var file in library.CompileTimeAssemblies.Concat(library.Build).Concat(library.BuildMultiTargeting)
                         .Concat(library.ContentFiles).Where(item => !item.Path.EndsWith("/_._", StringComparison.Ordinal)))
            {
                inputs.Add(Path.GetFullPath(Path.Combine(root, file.Path)));
                foreach (var property in file.Properties)
                    values.Add("resolved-item:" + project.FullPath + ":" + library.Name + ":" + file.Path + ":" + property.Key + "=" + property.Value);
            }
        }
    }

    // The current graphs use SDK KnownFrameworkReference targeting packs; their ref/analyzer inputs are read in place.
    private static void CollectFrameworks(Project project, SortedSet<string> inputs)
    {
        foreach (var reference in project.GetItems("FrameworkReference"))
        {
            var known = project.GetItems("KnownFrameworkReference").Single(item =>
                item.EvaluatedInclude == reference.EvaluatedInclude &&
                item.GetMetadataValue("TargetFramework") == project.GetPropertyValue("TargetFramework"));
            var name = known.GetMetadataValue("TargetingPackName");
            var version = reference.GetMetadataValue("TargetingPackVersion");
            if (version.Length == 0) version = known.GetMetadataValue("TargetingPackVersion");
            var pack = Path.Combine(project.GetPropertyValue("NetCoreTargetingPackRoot"), name, version);
            if (!Directory.Exists(pack))
                throw new DirectoryNotFoundException($"The selected SDK targeting pack is missing: {pack}.");
            foreach (var path in Directory.EnumerateFiles(pack, "*", SearchOption.AllDirectories)
                         .Where(path => Path.GetExtension(path) is ".dll" or ".json" or ".props" or ".targets" or ".globalconfig" ||
                             Path.GetRelativePath(pack, path).Split(Path.DirectorySeparatorChar)[0] == "data"))
                inputs.Add(path);
        }
    }

    private string PropertyIdentity => TargetSourceFingerprint.Hash(_properties.Select(item => item.Key + "=" + item.Value));

    private string FormalTargetPath => Path.Combine(_repository, "game", ".godot", "mono", "temp", "bin", "Debug", "Campus.dll");

    private bool IsFormal()
    {
        return _properties.GetValueOrDefault("Configuration") == "Debug" &&
            string.IsNullOrEmpty(_properties.GetValueOrDefault("RuntimeIdentifier")) &&
            !string.Equals(_properties.GetValueOrDefault("SelfContained"), "true", StringComparison.OrdinalIgnoreCase) &&
            !_properties.ContainsKey("TargetPath") &&
            string.Equals(_targetPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), FormalTargetPath,
                StringComparison.OrdinalIgnoreCase);
    }

    private Receipt? ReadReceipt()
    {
        if (!File.Exists(_receipt)) return null;
        var text = File.ReadAllText(_receipt);
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var json = JsonDocument.Parse(text);
        if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.Number ||
            !format.TryGetInt32(out var number) || number != ReceiptFormat) return null;
        var receipt = JsonSerializer.Deserialize<Receipt>(text, ContractJson.Options);
        return receipt is not null && receipt.Inputs is not null && receipt.Outputs is not null &&
            receipt.BuildProperties is not null && receipt.BuildProperties.All(item => item.Value is not null) &&
            receipt.Status is not null && receipt.Properties is not null && receipt.TargetPath is not null && receipt.Identity is not null &&
            receipt.ManifestIdentity is not null && receipt.Operation is not null ? receipt : null;
    }

    internal bool Check()
    {
        if (!IsFormal()) return false;
        var before = ReadReceipt();
        if (before is not { Format: ReceiptFormat, Status: "succeeded" } || before.TargetPath != _targetPath ||
            before.Inputs.Any(path => !File.Exists(path)) || before.Outputs.Any(path => !File.Exists(path))) return false;
        if (TargetSourceFingerprint.HashFile(_manifest) != before.ManifestIdentity) return false;
        return Capture().Identity == before.Identity && SameReceipt(ReadReceipt(), before);
    }

    internal string? Begin(bool rebuild)
    {
        if (!IsFormal()) return null;
        var before = ReadReceipt();
        File.Delete(_receipt); // A failed capture or build cannot leave the previous success in service.
        var snapshot = Capture();
        var verified = before is { Format: ReceiptFormat, Status: "succeeded" } && before.TargetPath == _targetPath &&
            before.Identity == snapshot.Identity && File.Exists(_manifest) &&
            TargetSourceFingerprint.HashFile(_manifest) == before.ManifestIdentity;
        var operation = Guid.NewGuid().ToString("N");
        WriteReceipt(new(ReceiptFormat, "building", new(_properties, StringComparer.OrdinalIgnoreCase), PropertyIdentity, _targetPath, snapshot.Identity, snapshot.Inputs,
            snapshot.Outputs, "", rebuild, verified, operation), replace: false);
        return operation;
    }

    internal void Complete(string operation)
    {
        if (!IsFormal()) return;
        var before = ReadReceipt();
        if (before is not { Format: ReceiptFormat, Status: "building" } || before.Properties != PropertyIdentity ||
            before.TargetPath != _targetPath || before.Operation != operation ||
            before.Inputs.Any(path => !File.Exists(path)))
        {
            Warn("Build receipt or its captured inputs changed; no success receipt was written.");
            return;
        }
        var snapshot = Capture();
        if (snapshot.Identity != before.Identity || !SameReceipt(ReadReceipt(), before))
        {
            Warn("Build inputs or receipt changed during the build; rebuild before launching.");
            return;
        }
        if (!before.Rebuild && !before.Verified)
        {
            Warn("Build inputs have no matching verified receipt; use dotnet build -t:Rebuild before launching.");
            return;
        }
        var outputs = CompletedOutputs(snapshot.Outputs);
        var manifestIdentity = TargetSourceFingerprint.HashFile(_manifest);
        WriteReceipt(before with { Status = "succeeded", Outputs = outputs, ManifestIdentity = manifestIdentity }, replace: true);
    }

    private static bool SameReceipt(Receipt? current, Receipt before) => current is not null &&
        JsonSerializer.Serialize(current, ContractJson.Options) == JsonSerializer.Serialize(before, ContractJson.Options);

    private string[] CompletedOutputs(string[] paths)
    {
        var outputs = new SortedSet<string>(paths, StringComparer.Ordinal);
        foreach (var path in paths)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Build output is missing.", path);
            if (!path.EndsWith(".FileListAbsolute.txt", StringComparison.Ordinal)) continue;
            foreach (var output in File.ReadLines(path))
                if (output.StartsWith(_repository + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(output)) throw new FileNotFoundException("Build file-list output is missing.", output);
                    outputs.Add(output);
                }
        }
        var gameOutput = Path.Combine(_repository, "game", ".godot", "mono", "temp", "bin", "Debug");
        foreach (var assembly in paths.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
        {
            if (assembly.StartsWith(Path.GetDirectoryName(_toolProject)!, StringComparison.OrdinalIgnoreCase)) continue;
            var copy = Path.Combine(gameOutput, Path.GetFileName(assembly));
            if (!File.Exists(copy)) throw new FileNotFoundException("Game project-reference output is missing.", copy);
            outputs.Add(copy);
        }
        return outputs.ToArray();
    }

    private void WriteReceipt(Receipt receipt, bool replace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_receipt)!);
        var staging = _receipt + ".tmp";
        try
        {
            File.WriteAllBytes(staging, JsonSerializer.SerializeToUtf8Bytes(receipt, ContractJson.Options));
            if (replace) File.Replace(staging, _receipt, destinationBackupFileName: null);
            else File.Move(staging, _receipt, overwrite: true);
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    internal static void Warn(string message) => Console.Error.WriteLine("warning : Build freshness: " + message);
}
