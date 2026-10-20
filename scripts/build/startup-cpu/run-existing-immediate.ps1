# Root only, after screenshot AND prebake release. Uses existing game/parser binaries verbatim.
# 用法：powershell -File scripts/build/startup-cpu/run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交> [-ProjectRoot <现成游戏检出根目录>]
param([Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$BuiltRevision,
    [string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$hasProjectRoot = [bool]$ProjectRoot
$ProjectRoot = if ($hasProjectRoot) { (Resolve-Path -LiteralPath $ProjectRoot).Path } else { $toolRoot }
. "$toolRoot\scripts\lib\common.ps1"
$Output = Assert-UESTCChildPath $Output (Join-Path $toolRoot 'output\start_game\C215-profile')
if (Test-Path -LiteralPath $Output) { throw 'Choose a fresh output directory.' }
$parser = Join-Path $toolRoot 'output\start_game\C215-profile\bin\CpuParse\Debug\net10.0\CpuParse.dll'
$assembly = Join-Path $ProjectRoot 'game\.godot\mono\temp\bin\Debug\Campus.dll'
$lease = $null
try {
    $lease = Enter-UESTCBuildLock (Get-UESTCLayout $toolRoot).Locks
    foreach ($file in @($parser, $assembly, (Join-Path $toolRoot 'output\tools\dotnet-diagnostics\dotnet-trace.exe'))) {
        if (-not [IO.File]::Exists($file)) { throw "Existing binary missing; no automatic build is permitted: $file" }
    }
    $binary = Get-Item -LiteralPath $assembly
    $binaryStamp = [ordered]@{ path=$binary.FullName; length=$binary.Length; lastWriteUtc=$binary.LastWriteTimeUtc.ToString('O') }
    # Deliberately skip Initialize-UESTCLaunch, dotnet build/msbuild, import and prebake.
    # The original private-desktop capture launches normal Godot without --editor/--build-solutions.
    $launchArguments = @('-I', "$PSScriptRoot\isolated-launch.py", $Output, 'immediate', '10')
    if ($hasProjectRoot) { $launchArguments += $ProjectRoot }
    & python @launchArguments
    if ($LASTEXITCODE -ne 0) { throw 'Immediate capture failed; do not use it as a successful measurement.' }
    $resultPath = Join-Path $Output 'result.json'
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    $result.baseline = $BuiltRevision
    $result | Add-Member -NotePropertyName existingBuild -NotePropertyValue ([ordered]@{
        builtRevisionDeclaredByRoot=$BuiltRevision; checkoutHead=(git -C $ProjectRoot rev-parse HEAD);
        compileImportPrebakeInvoked=$false; gameAssembly=$binaryStamp
    })
    $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resultPath -Encoding UTF8
    & dotnet $parser $Output
    if ($LASTEXITCODE -ne 0) { throw 'Existing CpuParse parser failed.' }
} finally { Exit-UESTCBuildLock $lease }
