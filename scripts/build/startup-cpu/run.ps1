# 启动测量批次：复用私有桌面，默认无逐帧日志与 EventPipe；诊断需显式选择。
# 用法：powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录> [-Modes immediate] [-Measurement experience|diagnostic] [-PlayingSample]。
# 产出：output/start_game/TOOL-215-profile/；诊断解析器的 bin/obj 也留在该目录。
param([Parameter(Mandatory)][string]$Output,
    [ValidateSet('title','immediate','dwell')][string[]]$Modes = @('title','immediate','dwell'),
    [ValidateSet('experience','diagnostic')][string]$Measurement = 'experience',
    [ValidateSet('valid','missing','unknown')][string]$GeometryCache = 'unknown',
    [ValidateSet('warm','first-run','unknown')][string]$PipelineCache = 'unknown',
    [ValidateSet('warm','reboot-first-run','unknown')][string]$SystemFileCache = 'unknown',
    [ValidateSet('yes','no','unknown')][string]$JustPrebaked = 'unknown',
    [string]$CacheEvidence = 'NOT_MEASURED', [string]$Device = 'unknown',
    [string]$BuiltRevision = 'unknown', [switch]$PlayingSample)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$root\scripts\lib\common.ps1"
. "$root\scripts\lib\start-game.ps1"
$Output = Assert-UESTCChildPath $Output (Join-Path $root 'output\start_game\TOOL-215-profile')
if (Test-Path -LiteralPath $Output) { throw 'Choose a fresh output directory.' }
if ($PipelineCache -eq 'warm') { throw 'Each capture uses fresh native user directories; warm pipeline-cache measurements are not available.' }
if ($SystemFileCache -eq 'reboot-first-run' -and $JustPrebaked -eq 'yes') { throw 'A just-prebaked run cannot be declared OS-cold.' }
if ($PlayingSample -and ($Measurement -ne 'diagnostic' -or $Modes.Count -ne 1 -or $Modes[0] -ne 'immediate')) {
    throw '-PlayingSample requires exactly -Modes immediate -Measurement diagnostic.'
}
New-Item -ItemType Directory -Path $Output | Out-Null
$testEnvironment = New-UESTCTestEnvironment -Work $Output
$preparation = [ordered]@{ status='PREPARING'; startedUtc=[DateTime]::UtcNow.ToString('O');
    completedUtc=$null; elapsedSeconds=$null; buildAndImportSeconds=$null;
    scope='Batch setup outside every capture script-entry interval';
    launcher=(New-UESTCLaunchTiming) }
$preparationClock = [Diagnostics.Stopwatch]::StartNew()
$lease = $null
try {
    $preparation.launcher['lockRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $lease = Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks
    $preparation.launcher['lockAcquiredTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $preparation.launcher['lockWaitStatus'] = 'MEASURED_IN_PREPARATION'
    Update-UESTCGameBuild -Root $root -Godot (Get-UESTCGodot) -BuildLease $lease -Timing $preparation.launcher -Environment $testEnvironment
    $preparation.buildAndImportSeconds = ($preparation.launcher.preparationCompletedTimestamp -
        $preparation.launcher.preparationStartedTimestamp) / [double][Diagnostics.Stopwatch]::Frequency
    $preparation.status = 'PREPARED'
} catch {
    $preparation.status = 'FAILED'
    throw
} finally {
    Exit-UESTCBuildLock $lease
    $preparationClock.Stop()
    $preparation.completedUtc = [DateTime]::UtcNow.ToString('O')
    $preparation.elapsedSeconds = $preparationClock.Elapsed.TotalSeconds
    $preparation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Output 'preparation.json') -Encoding UTF8
}
$parser = $null
if ($Measurement -eq 'diagnostic') {
    $lease = $null
    try {
        $lease = Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks
        # Diagnostic-tool build is setup, outside each product launch interval.
        & dotnet build "$PSScriptRoot\CpuParse.csproj" -v:q
        if ($LASTEXITCODE -ne 0) { throw 'CpuParse build failed.' }
        $parser = (& dotnet msbuild "$PSScriptRoot\CpuParse.csproj" -nologo -getProperty:TargetPath).Trim()
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $parser -PathType Leaf)) { throw 'CpuParse target path is missing.' }
    } finally { Exit-UESTCBuildLock $lease }
}
$index = 0
foreach ($mode in $Modes) {
    $round = Join-Path $Output $mode
    $condition = [ordered]@{ measurement=$Measurement; geometryCache=$GeometryCache;
        pipelineCache=$PipelineCache; systemFileCache=$SystemFileCache; justPrebaked=$JustPrebaked;
        cacheEvidence=$CacheEvidence; cacheStates='Each capture uses fresh APPDATA/LOCALAPPDATA; warm pipeline cache is unavailable. Other cache conditions require explicit evidence; unknown remains NOT_MEASURED.';
        preparation=$preparation;
        device=$Device; preset=if($PlayingSample){'fine'}else{'production-default'}; builtRevisionDeclaredByRoot=$BuiltRevision;
        playingSample=[bool]$PlayingSample;
        startupScenario=if($PlayingSample){'playing'}else{$mode};
        presentation='private desktop; CPU/GPU work diagnostic only, presented FPS not measured';
        playingDisplay=if($PlayingSample){'--quality=fine --quality-set=display_mode=0; preset v_sync=1; no fixed-fps'}else{$null} }
    # Every capture gets a fresh native profile, but only the first process can
    # retain an operator-established reboot-first-run system-file condition.
    if ($index -gt 0) {
        if ($SystemFileCache -eq 'reboot-first-run') { $condition.systemFileCache = 'unknown' }
        $condition.cacheStates += '; earlier round ran; OS-cold condition not reused'
    }
    $conditionPath = Join-Path $Output ($mode + '-condition.json')
    $condition | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $conditionPath -Encoding UTF8
    & python -I "$PSScriptRoot\isolated-launch.py" $round $mode $conditionPath
    if ($LASTEXITCODE -ne 0) { throw "$mode capture failed; do not reuse it as a successful measurement." }
    if ($Measurement -eq 'diagnostic') {
        & dotnet $parser $round
        if ($LASTEXITCODE -ne 0) { throw "$mode CPU parse failed." }
    }
    $index++
}
