# One diagnostic batch. Root invokes only after the screenshot AND prebake completion notices.
# 产出：output/start_game/TOOL-215-profile/；output/start_game/；output/.locks/build.lock。
param([Parameter(Mandatory)][string]$Output, [ValidateRange(1,120)][double]$TitleDwellSeconds = 10)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$root\scripts\lib\common.ps1"
. "$root\scripts\lib\start-game.ps1"
$Output = Assert-UESTCChildPath $Output (Join-Path $root 'output\start_game\TOOL-215-profile')
if (Test-Path -LiteralPath $Output) { throw 'Choose a fresh output directory.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$lease = $null
try {
    $lease = Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks
    Initialize-UESTCLaunch -Root $root -Godot (Get-UESTCGodot) -BuildLease $lease
    # Compile only this disposable parser, before the three conditions. No NuGet package references.
    & dotnet build "$PSScriptRoot\CpuParse.csproj" -v:q
    if ($LASTEXITCODE -ne 0) { throw 'CpuParse build failed.' }
    $parser = (& dotnet msbuild "$PSScriptRoot\CpuParse.csproj" -nologo -getProperty:TargetPath).Trim()
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $parser -PathType Leaf)) { throw 'CpuParse target path is missing.' }
    foreach ($mode in @('title','immediate','dwell')) {
        $round = Join-Path $Output $mode
        & python -I "$PSScriptRoot\isolated-launch.py" $round $mode $TitleDwellSeconds
        if ($LASTEXITCODE -ne 0) { throw "$mode capture failed; do not reuse it as a successful measurement." }
        & dotnet $parser $round
        if ($LASTEXITCODE -ne 0) { throw "$mode CPU parse failed." }
    }
} finally { Exit-UESTCBuildLock $lease }

