[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^WP-\d{2}[a-z]?$')][string]$Wp,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string]$Slug,
    [switch]$Plan, [switch]$SkipInitialize)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
$layout = Get-UESTCLayout (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
$target = Assert-UESTCChildPath (Join-Path $layout.Worktrees $Wp) $layout.Worktrees
$branch = 'wp/' + $Wp.Substring(3).ToLowerInvariant() + '-' + $Slug
if (Test-Path -LiteralPath $target) { throw "Refusing to touch an existing tree: $target" }
$branchProbe = Invoke-UESTCGit $layout.Root @('show-ref', '--verify', '--quiet', ('refs/heads/' + $branch)) -SuccessExitCodes @(0, 1)
if ($branchProbe.ExitCode -eq 0) { throw "Local branch already exists: $branch" }
$null = Invoke-UESTCGit $layout.Root @('rev-parse', '--verify', 'main')
$seed = Join-Path $layout.Primary 'game\.godot'
$godot = Get-UESTCGodot
if ($Plan) {
    [pscustomobject]@{ Target = $target; Branch = $branch; Base = 'main'; Seed = $seed; Locks = $layout.Locks; Initialize = -not $SkipInitialize }
    return
}
if (-not $SkipInitialize -and -not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Godot not found: $godot" }
$null = [IO.Directory]::CreateDirectory($layout.Worktrees)
$null = Invoke-UESTCGit $layout.Root @('worktree', 'add', $target, '-b', $branch, 'main')
# Only a newly created and validated destination is seeded; /E never deletes files.
$destination = Assert-UESTCChildPath (Join-Path $target 'game\.godot') $target
if (Test-Path -LiteralPath $seed -PathType Container) {
    $lease = Enter-UESTCResourceLock $layout.Locks Cpu
    try {
        $null = Invoke-UESTCProcess robocopy @($seed, $destination, '/E', '/XD', 'shader_cache', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS') `
            $target (Join-Path $target 'output/build/worktree-seed.log') -SuccessExitCodes (0..7)
    } finally { Exit-UESTCResourceLock $lease }
} else { Write-Warning "Import seed absent: $seed. First import will generate it locally." }
$override = Join-Path $target 'game\override.cfg'
$name = 'UESTC_' + ($Wp -replace '-', '')
[IO.File]::WriteAllText($override, "[application]`nconfig/use_custom_user_dir=true`nconfig/custom_user_dir_name=`"$name`"`n", (New-Object Text.UTF8Encoding $false))
if (-not $SkipInitialize) {
    $lease = Enter-UESTCResourceLock $layout.Locks Cpu
    try {
        $null = Invoke-UESTCProcess dotnet @('build', (Join-Path $target 'game\Campus.sln'), '-warnaserror') $target (Join-Path $target 'output\build\worktree-build.log')
        $log = Join-Path $target 'output\build\worktree-import.log'
        $result = Invoke-UESTCProcess $godot @('--headless','--path',(Join-Path $target 'game'),'--import','--log-file',$log) $target ($log + '.console.log')
        if ($result.Output -match '(?m)(?:SCRIPT )?ERROR:') { throw "Import errors; see $log" }
    } finally { Exit-UESTCResourceLock $lease }
}
Write-Output "Created $target on $branch; user directory $name. No push or merge performed."
