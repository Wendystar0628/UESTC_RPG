using Campus.Contracts.Startup;

namespace Campus.Kernel.Boot;

internal sealed record FixedShot(string Name, string Pose, BootRunOptions Options);

/// <summary>Uses the existing fixed-shot table; all selected rows share one boot and map mount.</summary>
internal static class FixedShotPlan
{
    internal static IReadOnlyList<FixedShot> Read(string[] args, bool development)
    {
        var files = args.Where(arg => arg.StartsWith("--shots-file=", StringComparison.Ordinal)).ToArray();
        if (files.Length == 0) return [];
        if (files.Length != 1 || !development) throw new ArgumentException("--shots-file requires one development capture table.");
        var path = files[0]["--shots-file=".Length..];
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("--shots-file must be absolute.");
        var selectors = args.Where(arg => arg.StartsWith("--shots=", StringComparison.Ordinal)).ToArray();
        if (selectors.Length > 1) throw new ArgumentException("Duplicate --shots selection.");
        var selected = selectors.Length == 0 ? [] : selectors[0]["--shots=".Length..].Split(',');
        if (selected.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Empty fixed-shot selection.");
        var common = args.Where(arg => !files.Contains(arg) && !selectors.Contains(arg)).ToArray();
        if (common.Any(arg => arg.StartsWith("--frames=", StringComparison.Ordinal)))
            throw new ArgumentException("--shots-file captures as soon as content is ready; do not set --frames.");
        var result = new List<FixedShot>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
                var columns = line.Split('|').Select(column => column.Trim()).ToArray();
                if (columns.Length != 3 || string.IsNullOrWhiteSpace(columns[0]) ||
                    columns[0].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !names.Add(columns[0]))
                    throw new ArgumentException("Invalid or duplicate fixed-shot name.");
                if (selected.Length > 0 && !selected.Contains(columns[0], StringComparer.Ordinal)) continue;
                var extra = columns[2].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                var options = BootArguments.ParseSingle(common.Concat(extra).Concat(["--shots=" + columns[1], "--pose=" + columns[1]]), development);
                if (options.HasLoad || options.Title || options.Capture.Gate is not null ||
                    options.World.ViewMode != Campus.Contracts.ViewMode.Fly || options.World.FlowRate != 0)
                    throw new ArgumentException("Fixed shots require a fresh, paused fly capture without title or lifecycle gates.");
                result.Add(new(columns[0], columns[1], options));
            }
        }
        catch (IOException error) { throw new ArgumentException("Fixed-shot table could not be read.", error); }
        if (result.Count == 0 || selected.Any(name => !names.Contains(name)))
            throw new ArgumentException("No fixed shots selected or an unknown fixed-shot name was requested.");
        return result;
    }
}
