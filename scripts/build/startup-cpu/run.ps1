# 启动测量批次：复用私有桌面，默认无逐帧日志与 EventPipe；诊断需显式选择。
# 用法：powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录> [-Modes immediate] [-Measurement experience|diagnostic]。
# 产出：output/start_game/TOOL-215-profile/；诊断解析器的 bin/obj 也留在该目录。
param([Parameter(Mandatory)][string]$Output,
    [ValidateSet('title','immediate','dwell')][string[]]$Modes = @('title','immediate','dwell'),
    [ValidateSet('experience','diagnostic')][string]$Measurement = 'experience',
    [ValidateSet('valid','missing','unknown')][string]$GeometryCache = 'unknown',
    [ValidateSet('warm','first-run','unknown')][string]$PipelineCache = 'unknown',
    [ValidateSet('warm','reboot-first-run','unknown')][string]$SystemFileCache = 'unknown',
    [ValidateSet('yes','no','unknown')][string]$JustPrebaked = 'unknown',
    [string]$CacheEvidence = 'NOT_MEASURED', [string]$Device = 'unknown',
    [string]$BuiltRevision = 'unknown')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$root\scripts\lib\common.ps1"
$Output = Assert-UESTCChildPath $Output (Join-Path $root 'output\start_game\TOOL-215-profile')
if (Test-Path -LiteralPath $Output) { throw 'Choose a fresh output directory.' }
if ($SystemFileCache -eq 'reboot-first-run' -and $JustPrebaked -eq 'yes') { throw 'A just-prebaked run cannot be declared OS-cold.' }
New-Item -ItemType Directory -Path $Output | Out-Null
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
        cacheEvidence=$CacheEvidence; cacheStates='Operator-declared; no caches cleared or inferred from process restart';
        device=$Device; preset='production-default'; builtRevisionDeclaredByRoot=$BuiltRevision }
    # Only a separately controlled first process can claim reboot/first-pipeline conditions.
    if ($index -gt 0) {
        if ($PipelineCache -eq 'first-run') { $condition.pipelineCache = 'unknown' }
        if ($SystemFileCache -eq 'reboot-first-run') { $condition.systemFileCache = 'unknown' }
        $condition.cacheEvidence += '; earlier round ran; cold/first conditions not reused'
    }
    $conditionPath = Join-Path $Output ($mode + '-condition.json')
    $condition | ConvertTo-Json | Set-Content -LiteralPath $conditionPath -Encoding UTF8
    & python -I "$PSScriptRoot\isolated-launch.py" $round $mode $conditionPath
    if ($LASTEXITCODE -ne 0) { throw "$mode capture failed; do not reuse it as a successful measurement." }
    if ($Measurement -eq 'diagnostic') {
        & dotnet $parser $round
        if ($LASTEXITCODE -ne 0) { throw "$mode CPU parse failed." }
    }
    $index++
}
