# 固定提交临时树的性能采集入口：每场景采一遍，打印指标与上一次差值；诊断单独输出。
# 用法：powershell -File scripts/perf/run.ps1 -Batch <新来源名> [-Suite fixed|route|travel] [-Scene <场景>]；诊断用 -Scene <场景> -DiagnosticTool gpu-profile|EventPipe|Nsight。
[CmdletBinding(DefaultParameterSetName='Collection')]
param([Parameter(Mandatory,ParameterSetName='Collection')][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9-]+$')][string]$Batch,
    [ValidateSet('fixed','route','travel')][string[]]$Suite=@('fixed','route','travel'), [string[]]$Scene=@(),
    [switch]$PrepareBake, [IO.FileStream]$BuildLease=$null,
    [Parameter(Mandatory,ParameterSetName='Diagnostic')][ValidateSet('gpu-profile','EventPipe','Nsight')][string]$DiagnosticTool)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $root 'scripts\lib\common.ps1')
. (Join-Path $root 'scripts\lib\start-game.ps1')
. (Join-Path $PSScriptRoot 'collection-plan.ps1')
# Main remains free to commit; the caller supplies an existing detached worktree at the chosen commit.
if (-not (Test-Path -LiteralPath (Join-Path $root '.git') -PathType Leaf)) {
    throw 'Run collection or diagnostics from a temporary Git worktree at a fixed commit.'
}
$null=& git -C $root symbolic-ref -q HEAD
if ($LASTEXITCODE -eq 0) { throw 'Detach the temporary worktree at the chosen commit before collecting.' }
if ($LASTEXITCODE -ne 1) { throw 'Cannot read the worktree revision.' }
# Git already knows the primary checkout; raw captures and derived metrics stay together across temporary trees.
$common=(& git -C $root rev-parse --path-format=absolute --git-common-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot find the primary checkout.' }
$perfRoot=Join-Path (Split-Path $common -Parent) 'output\perf'
if ($PSCmdlet.ParameterSetName -eq 'Diagnostic') {
    . (Join-Path $PSScriptRoot 'diagnostic-run.ps1')
    Invoke-PerfDiagnostic -Root $root -Scene $Scene -Tool $DiagnosticTool -BuildLease $BuildLease -OutputRoot $perfRoot
    return
}
$plan=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'scenarios.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$selected=@(Get-CollectionScenes $plan $Suite $Scene)
$directory=Assert-UESTCChildPath (Join-Path $perfRoot ('runs\' + $Batch)) (Join-Path $perfRoot 'runs')
if (Test-Path -LiteralPath $directory) { throw 'Choose a fresh source name; prior data is retained.' }
$null=[IO.Directory]::CreateDirectory($directory)
$lease=$null
$failed=$false
$cache=$env:UESTC_BAKE_CACHE
Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue
try {
    if ($BuildLease) { $heldLease=$BuildLease }
    else { $lease=Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks; $heldLease=$lease }
    $godot=Get-UESTCGodot
    $environment=New-UESTCTestEnvironment -Work $directory
    $environment['UESTC_BAKE_CACHE']=$null
    Update-UESTCGameBuild -Root $root -Godot $godot -BuildLease $heldLease -Environment $environment
    if ($PrepareBake) {
        $bake=Invoke-UESTCProcess $godot @('--headless','--path',(Join-Path $root 'game'),'--','--bake=all',('--map=' + $plan.map)) `
            $root (Join-Path $directory 'preparation-bake.log') -TimeoutSeconds 14400 -Environment $environment
        Assert-UESTCCleanGodotLog $bake
    }
    foreach ($item in $selected) {
        $sceneDirectory=Join-Path $directory $item.scene.id
        $null=[IO.Directory]::CreateDirectory($sceneDirectory)
        $request=New-CollectionRequest $item
        $requestPath=Join-Path $sceneDirectory 'collection.json'
        Write-CollectionJson $requestPath $request
        $run=[ordered]@{ scene=$item.scene.id; sampleSegmentId=$request.sampleSegmentId; conditions=$request;
            exitCode=$null; observed=$null; close=@{ durationMs=$null };
            launcher=@{ scriptEnteredUtc=[DateTime]::UtcNow.ToString('O'); processExitUtc=$null } }
        try {
            $environment=New-UESTCTestEnvironment -Work $sceneDirectory
            $environment['UESTC_BAKE_CACHE']=$null
            $arguments=@(Get-CollectionArguments $root $sceneDirectory $plan $item $requestPath)
            $result=Invoke-UESTCProcess -Executable $godot -Arguments $arguments -WorkingDirectory $root `
                -Log (Join-Path $sceneDirectory 'game.log') -TimeoutSeconds 600 -AllowFailure `
                -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
            $run.exitCode=$result.ExitCode
            if ($null -ne $result.ProcessExitUtc) { $run.launcher.processExitUtc=$result.ProcessExitUtc.ToString('O') }
            Assert-UESTCCleanGodotLog $result
            if ($result.ExitCode -ne 0) { throw "Game exited with code $($result.ExitCode)." }
            $marks=[regex]::Matches($result.StdOut, '(?m)^PERF_COLLECTION_RESULT (.+)\r?$')
            if ($marks.Count -eq 1) {
                $observed=$marks[0].Groups[1].Value | ConvertFrom-Json
                $save=$observed.PSObject.Properties['save']
                $run.observed=@{ save=$(if ($null -ne $save) { $save.Value } else { $null }); travelSegments=@($observed.travelSegments) }
            }
            $begin=[regex]::Match($result.StdOut, '(?m)^PERF_COLLECTION_CLOSE_BEGIN ([0-9.Ee+-]+)\r?$')
            $end=[regex]::Match($result.StdOut, '(?m)^PERF_COLLECTION_CLOSE_END ([0-9.Ee+-]+)\r?$')
            if ($begin.Success -and $end.Success) {
                $culture=[Globalization.CultureInfo]::InvariantCulture
                $run.close.durationMs=[double]::Parse($end.Groups[1].Value,$culture)-[double]::Parse($begin.Groups[1].Value,$culture)
            }
        } catch { $failed=$true; $run['error']=$_.Exception.ToString(); Write-Warning $run.error }
        finally {
            $recordings=@(Publish-CollectionRun $sceneDirectory $run)
            if (-not $recordings.Count) { $failed=$true; Write-Warning 'Game produced no closed performance recording.' }
        }
    }
    $analysis=Invoke-UESTCProcess python @('-I','-X','utf8',(Join-Path $PSScriptRoot 'analyze.py'),'--root',$root,'--output',$perfRoot) $root `
        -SuccessExitCodes @(0,1) -TextEncoding ([Text.Encoding]::UTF8)
    $analysisResult=$analysis.StdOut | ConvertFrom-Json
    if ($analysisResult.skipped -contains '分析锁被占用') { throw 'Performance analysis lock is occupied.' }
    if (@($analysisResult.errors | Where-Object { $_.source.StartsWith($directory + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) }).Count) {
        $failed=$true
    }
    & python -I -X utf8 (Join-Path $PSScriptRoot 'perf.py') --root $root --output $perfRoot latest --origin ai
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read performance metrics.' }
} finally {
    Exit-UESTCBuildLock $lease
    if ($null -eq $cache) { Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue }
    else { $env:UESTC_BAKE_CACHE=$cache }
}
if ($failed) { throw 'One or more game processes failed; original data and errors are retained.' }
