namespace Campus.Tests;

/// <summary>Repository paths for both test hosts. An explicit runner root must identify a real checkout.</summary>
internal static class RepositoryFixture
{
    public static string Root { get; } = FindRoot();

    public static string FilePath(params string[] segments) => Path.Combine([Root, .. segments]);

    private static string FindRoot()
    {
        if (Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } configured)
        {
            var root = Path.GetFullPath(configured);
            if (!IsRepository(root)) throw new DirectoryNotFoundException("UESTC_REPOSITORY must identify the tested checkout.");
            return root;
        }

        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() }.Distinct(StringComparer.OrdinalIgnoreCase))
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (IsRepository(directory.FullName)) return directory.FullName;
        throw new DirectoryNotFoundException("The test repository could not be found. Set UESTC_REPOSITORY to the tested checkout.");
    }

    private static bool IsRepository(string root) =>
        File.Exists(Path.Combine(root, "game", "Campus.csproj")) &&
        File.Exists(Path.Combine(root, "game", "project.godot"));
}
