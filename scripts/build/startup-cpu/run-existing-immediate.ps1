# Root only, after screenshot AND prebake release. Uses existing game/parser binaries verbatim.
param([Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$BuiltRevision)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$root\scripts\lib\common.ps1"
$Output = Assert-UESTCChildPath $Output (Join-Path $root 'output\start_game\C215-profile')
if (Test-Path -LiteralPath $Output) { throw 'Choose a fresh output directory.' }
$parser = Join-Path $root 'output\start_game\C215-profile\bin\CpuParse\Debug\net10.0\CpuParse.dll'
$assembly = Join-Path $root 'game\.godot\mono\temp\bin\Debug\Campus.dll'
$lease = $null
try {
    $lease = Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks
    foreach ($file in @($parser, $assembly, (Join-Path $root 'output\tools\dotnet-diagnostics\dotnet-trace.exe'))) {
        if (-not [IO.File]::Exists($file)) { throw "Existing binary missing; no automatic build is permitted: $file" }
    }
    $binary = Get-Item -LiteralPath $assembly
    $binaryStamp = [ordered]@{ path=$binary.FullName; length=$binary.Length; lastWriteUtc=$binary.LastWriteTimeUtc.ToString('O') }
    # Deliberately skip Initialize-UESTCLaunch, dotnet build/msbuild, import and prebake.
    # The original private-desktop capture launches normal Godot without --editor/--build-solutions.
    & python -I "$PSScriptRoot\isolated-launch.py" $Output immediate 10
    if ($LASTEXITCODE -ne 0) { throw 'Immediate capture failed; do not use it as a successful measurement.' }
    $resultPath = Join-Path $Output 'result.json'
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    $result.baseline = $BuiltRevision
    $result | Add-Member -NotePropertyName existingBuild -NotePropertyValue ([ordered]@{
        builtRevisionDeclaredByRoot=$BuiltRevision; checkoutHead=(git -C $root rev-parse HEAD);
        compileImportPrebakeInvoked=$false; gameAssembly=$binaryStamp
    })
    $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resultPath -Encoding UTF8
    & dotnet $parser $Output
    if ($LASTEXITCODE -ne 0) { throw 'Existing CpuParse parser failed.' }
} finally { Exit-UESTCBuildLock $lease }
