# 显式准备后测一次 immediate 体验；与常规批次共用正式输出检查、私桌面与计时，不跳过启动器等待。
# 用法：powershell -File scripts/build/startup-cpu/run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交>。
# 产出：output/start_game/TOOL-215-profile/；output/.locks/build.lock。
param([Parameter(Mandatory)][string]$Output, [Parameter(Mandatory)][string]$BuiltRevision,
    [ValidateSet('valid','missing','unknown')][string]$GeometryCache = 'unknown',
    [ValidateSet('warm','first-run','unknown')][string]$PipelineCache = 'unknown',
    [ValidateSet('warm','reboot-first-run','unknown')][string]$SystemFileCache = 'unknown',
    [ValidateSet('yes','no','unknown')][string]$JustPrebaked = 'unknown',
    [string]$CacheEvidence = 'NOT_MEASURED', [string]$Device = 'unknown')
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\run.ps1" -Output $Output -Modes immediate -Measurement experience -BuiltRevision $BuiltRevision `
    -GeometryCache $GeometryCache -PipelineCache $PipelineCache -SystemFileCache $SystemFileCache `
    -JustPrebaked $JustPrebaked -CacheEvidence $CacheEvidence -Device $Device
