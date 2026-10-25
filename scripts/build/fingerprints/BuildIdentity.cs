// Publishes the successful native build's Git identity beside its actual assembly, without changing compiler inputs.
// 用法：由 Campus.csproj 调用既有工具 --build-identity <assembly> <repository>。
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;

namespace Campus.Build.Fingerprints;

internal static class BuildIdentity
{
    public static async Task Write(string assemblyPath, string repository)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var assembly = new PEReader(stream);
        var metadata = assembly.GetMetadataReader();
        var identity = new JsonObject
        {
            ["moduleVersionId"] = metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString("D")
        };
        try
        {
            identity["commit"] = await Git(repository, "rev-parse", "HEAD");
            identity["dirty"] = (await Git(repository, "status", "--porcelain", "--untracked-files=normal")).Length != 0;
        }
        catch (Exception error)
        {
            Missing(identity, error);
        }
        var bytes = JsonSerializer.SerializeToUtf8Bytes(identity, ContractJson.Options);
        string output = assemblyPath + ".identity.json";
        if (File.Exists(output) && File.ReadAllBytes(output).AsSpan().SequenceEqual(bytes)) return;
        await File.WriteAllBytesAsync(output, bytes);
    }

    // An unavailable source identity leaves this build explicitly incomplete; the original failure is retained.
    private static void Missing(JsonObject identity, [FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        identity.Remove("commit");
        identity.Remove("dirty");
        identity["identityMissingReason"] = error.ToString();
    }

    private static async Task<string> Git(string repository, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = repository, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Git could not be started for build identity.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        string result = await output, failure = await error;
        if (process.ExitCode != 0) throw new IOException($"Git build identity failed ({process.ExitCode}): {failure}");
        return result.Trim();
    }
}
