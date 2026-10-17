param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
$directory = Join-Path $rootPath 'game/gdunit4_testadapter_v5'
$null = [IO.Directory]::CreateDirectory($directory)
# gdUnit4.api 5.1.0-rc5 installs this bridge but invokes an ambiguous bare dotnet build
# when a Godot root contains several csproj files. Preinstall the identical bridge so
# our explicit solution build compiles it and the adapter skips that fallback build.
# The game and tests are separate assemblies: load the test assembly into Godot's own
# context before the runner resolves test types (the API assumes a single assembly).
$source = @'
namespace GdUnit4.TestRunner;

public partial class GdUnit4TestRunnerScene : GdUnit4.Core.Runners.GdUnit4TestRunnerSceneCore
{
    public GdUnit4TestRunnerScene()
    {
        var configuration = System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyConfigurationAttribute>(GetType().Assembly)?.Configuration
            ?? throw new System.InvalidOperationException("Test host build configuration is unavailable.");
        var path = Godot.ProjectSettings.GlobalizePath($"res://bin/Campus.Tests/{configuration}/net10.0/Campus.Tests.dll");
        var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(GetType().Assembly)
            ?? throw new System.InvalidOperationException("Godot assembly load context is unavailable.");
        context.LoadFromAssemblyPath(path);
    }
}
'@
$path = Join-Path $directory 'GdUnit4TestRunnerScene.cs'
$source = $source.Replace("`r`n", "`n") + "`n"
if (-not [IO.File]::Exists($path) -or [IO.File]::ReadAllText($path) -ne $source) {
    [IO.File]::WriteAllText($path, $source, (New-Object Text.UTF8Encoding $false))
}
