# AI 固定采集唯一入口：冻结条件，分开本树准备与私桌面采样，保留失败并交既有分析器。
# 用法：powershell -File scripts/perf/run.ps1 -Batch TOOL-428-check [-Suite fixed|route|travel] [-Scene overview-default] [-PrepareBake]；正式协议加 -Mode formal -Protocol repeat3|aba -Phase a1|b1|a2；诊断用 -DeepenSession <id> [-DiagnosticTool gpu-profile,EventPipe,Nsight]。
# 产出：output/perf/runs/<Batch>/<Phase>/<场景>/<重复-尝试>/<会话>/run.json 与原始六件；functional 永不进入比较。
[CmdletBinding(DefaultParameterSetName='Collection')]
param([Parameter(Mandatory,ParameterSetName='Collection')][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9-]+$')][string]$Batch,
    [ValidateSet('fixed','route','travel')][string[]]$Suite=@('fixed','route','travel'), [string[]]$Scene=@(),
    [ValidateSet('functional-smoke','formal')][string]$Mode='functional-smoke',
    [ValidateSet('repeat3','aba')][string]$Protocol='repeat3', [ValidateSet('a1','b1','a2')][string]$Phase='a1',
    [switch]$PrepareBake, [IO.FileStream]$BuildLease=$null, [string]$BatchConditions='',
    [Parameter(Mandatory,ParameterSetName='Diagnostic')][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$DeepenSession,
    [Parameter(ParameterSetName='Diagnostic')][ValidateSet('gpu-profile','EventPipe','Nsight')][string[]]$DiagnosticTool=@(),
    [Parameter(ParameterSetName='Diagnostic')][ValidateRange(0,10000)][int]$DeepenEntry=0)
$ErrorActionPreference='Stop'
$collectionScriptEnteredUtc=[DateTime]::UtcNow.ToString('O')
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $root 'scripts\lib\common.ps1')
. (Join-Path $root 'scripts\lib\start-game.ps1')
. (Join-Path $PSScriptRoot 'collection-plan.ps1')
if ($PSCmdlet.ParameterSetName -eq 'Diagnostic') {
    . (Join-Path $PSScriptRoot 'diagnostic-run.ps1')
    Invoke-PerfDiagnostic -Root $root -SessionId $DeepenSession -Tools $DiagnosticTool -EntryIndex $DeepenEntry -BuildLease $BuildLease
    return
}
# DECISION-344: all thresholds are frozen here; normal small DWM load is below the GPU threshold.
$stableFrames=10
$realtimeSeconds=2
$repetitions=3
$sampleSeconds=30
$smokeSeconds=2
$maximumAttempts=3
$otherGpuFraction=0.20
$otherCpuFraction=0.25
$maximumFactAgeSeconds=3
$timeoutSeconds=600
$judgement=[ordered]@{ logicalPauseVerified=$false; mainThreadLongStopVerified=$false;
    noiseCalibrationBatchId=$null; tuningSelectedMetrics=@(); startupCache='unknown' }
if ($BatchConditions) {
    $requested=Get-Content -LiteralPath $BatchConditions -Raw | ConvertFrom-Json
    foreach ($property in $requested.PSObject.Properties) {
        if (-not $judgement.Contains($property.Name)) { throw "Unknown batch condition: $($property.Name)" }
        $judgement[$property.Name]=$property.Value
    }
    foreach ($name in @('logicalPauseVerified','mainThreadLongStopVerified')) {
        if ($judgement[$name] -isnot [bool]) { throw "$name must be a recorded round-zero boolean." }
    }
    if ($judgement.startupCache -notin @('warm','cold','first-run','unknown')) { throw 'Unknown startup-cache condition.' }
}
if ($Protocol -eq 'repeat3' -and $Phase -ne 'a1') { throw 'repeat3 only has a1.' }
$plan=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'scenarios.json') -Raw | ConvertFrom-Json
$selected=@(Get-CollectionScenes $plan $Suite $Scene)
if ($Mode -eq 'functional-smoke' -and -not $Scene.Count) {
    $selected=@($Suite | ForEach-Object { $kind=$_; $selected | Where-Object { $_.kind -eq $kind } | Select-Object -First 1 })
}
$sceneIds=@($selected | ForEach-Object { $_.scene.id })
$batchRoot=Assert-UESTCChildPath (Join-Path $root ('output\perf\runs\' + $Batch)) (Join-Path $root 'output\perf\runs')
$phaseRoot=Join-Path $batchRoot $Phase
if (Test-Path -LiteralPath $phaseRoot) { throw 'Choose a fresh batch/phase; prior data is retained.' }
Assert-CollectionPhase $batchRoot $Protocol $Phase $sceneIds $judgement
$null=[IO.Directory]::CreateDirectory($phaseRoot)
$purpose=if ($Mode -eq 'formal') { 'baseline' } else { 'functional' }
$phaseRecord=[ordered]@{ purpose=$purpose; protocol=$Protocol; phase=$Phase; scenes=$sceneIds; status='preparing';
    revision=(& git -C $root rev-parse HEAD).Trim(); preparation=$null; completed=$false;
    judgementConditions=$judgement; collectionScriptEnteredUtc=$collectionScriptEnteredUtc }
$lease=$null
$failed=$false
$acceptedSources=@()
# DECISION-346: the inherited override is removed, and every child uses this checkout's own default cache.
$inheritedBakeCache=$env:UESTC_BAKE_CACHE
Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue
try {
    if ($BuildLease) {
        if (-not $BuildLease.CanRead -or -not $BuildLease.CanWrite) { throw 'The supplied BuildLease is closed.' }
        $heldLease=$BuildLease
    } else { $lease=Enter-UESTCBuildLock (Get-UESTCLayout $root).Locks; $heldLease=$lease }
    $preparation=New-UESTCLaunchTiming
    $preparation['lockWaitStatus']=if ($BuildLease) { 'ALREADY_HELD' } else { 'ACQUIRED_BY_COLLECTION' }
    $phaseRecord.preparation=$preparation
    Write-CollectionJson (Join-Path $phaseRoot 'phase.json') $phaseRecord
    $godot=Get-UESTCGodot
    $environment=New-UESTCTestEnvironment -Work $phaseRoot
    $environment['UESTC_BAKE_CACHE']=$null
    Update-UESTCGameBuild -Root $root -Godot $godot -BuildLease $heldLease -Timing $preparation -Environment $environment
    $preparation['bakeRequested']=[bool]$PrepareBake
    if ($PrepareBake) {
        $bake=Invoke-UESTCProcess $godot @('--headless','--path',(Join-Path $root 'game'),'--','--bake=all',('--map=' + $plan.map)) `
            $root (Join-Path $phaseRoot 'preparation-bake.log') -TimeoutSeconds 14400 -Environment $environment
        Assert-UESTCCleanGodotLog $bake
        $preparation['bakeExitCode']=$bake.ExitCode
    }
    $phaseRecord.status='collecting'
    Write-CollectionJson (Join-Path $phaseRoot 'phase.json') $phaseRecord
    $count=if ($Mode -eq 'formal') { $repetitions } else { 1 }
    $attemptLimit=if ($Mode -eq 'formal') { $maximumAttempts } else { 1 }
    foreach ($item in $selected) {
        for ($repeat=1; $repeat -le $count; $repeat++) {
            $accepted=$false
            for ($attempt=1; $attempt -le $attemptLimit; $attempt++) {
                $entry=$item.scene
                $directory=Join-Path $phaseRoot ($entry.id + '\' + $repeat + '-' + $attempt)
                $null=[IO.Directory]::CreateDirectory($directory)
                $seconds=if ($Mode -ne 'formal') { $smokeSeconds } elseif ($item.kind -eq 'route') { $entry.seconds } else { $sampleSeconds }
                $segment='sample-' + [Guid]::NewGuid().ToString('N')
                $request=[ordered]@{ sceneId=$entry.id; sampleSegmentId=$segment; functionalSmoke=($Mode -eq 'functional-smoke'); stableFrames=$stableFrames;
                    realtimeSeconds=$realtimeSeconds; sampleSeconds=$seconds; otherGpuFraction=$otherGpuFraction;
                    otherCpuFraction=$otherCpuFraction; maximumFactAgeSeconds=$maximumFactAgeSeconds;
                    saveAfterSample=($entry.PSObject.Properties['saveAfterSample'] -and [bool]$entry.saveAfterSample) }
                $request['suite']=$item.kind
                $request['routePointIds']=@()
                if ($entry.PSObject.Properties['routePointIds']) { $request['routePointIds']=@($entry.routePointIds) }
                $request['travelPrefetch']=if ($entry.PSObject.Properties['travelPrefetch']) { [bool]$entry.travelPrefetch } else { $false }
                foreach ($name in $judgement.Keys) { $request[$name]=$judgement[$name] }
                $requestPath=Join-Path $directory 'collection.json'
                Write-CollectionJson $requestPath $request
                $run=[ordered]@{ scene=$entry.id; status='partial'; reasons=@('Process has not completed.'); exitCode=$null;
                    timedOut=$false; coverage=@{}; purpose=$purpose; sampleSegmentId=$segment; attempt=$attempt; observed=$null;
                    polluted=$null; pollution=@();
                    close=@{ completed=$false; durationMs=$null; source='formal-close-task'; reason='Formal close start/end not observed.' };
                    protocol=@{ kind=$Protocol; batchId=$Batch; scenes=$sceneIds; phase=$Phase; repetition=$repeat }; conditions=$request;
                    collectionScriptEnteredUtc=$collectionScriptEnteredUtc;
                    launcher=@{ scriptEnteredUtc=$null; scriptEnteredTimestamp=$null; stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency;
                        processExitUtc=$null; basis='prepared-attempt'; preparation='Batch build/import and optional bake precede this attempt; not a double-click launcher measurement.' } }
                $arguments=@(Get-CollectionArguments $root $directory $plan $item $seconds $requestPath)
                try {
                    $run.launcher.scriptEnteredUtc=[DateTime]::UtcNow.ToString('O')
                    $run.launcher.scriptEnteredTimestamp=[Diagnostics.Stopwatch]::GetTimestamp()
                    $environment=New-UESTCTestEnvironment -Work $directory
                    $environment['UESTC_BAKE_CACHE']=$null
                    $result=Invoke-UESTCProcess -Executable $godot -Arguments $arguments -WorkingDirectory $root `
                        -Log (Join-Path $directory 'game.log') -TimeoutSeconds $timeoutSeconds -AllowFailure `
                        -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
                    $run.exitCode=$result.ExitCode
                    if ($null -ne $result.ProcessExitUtc) { $run.launcher.processExitUtc=$result.ProcessExitUtc.ToString('O') }
                    if ($result.ExitCode -eq 0) {
                        $closeMarks=[regex]::Matches($result.StdOut, '(?m)^PERF_COLLECTION_CLOSE_(BEGIN|END) ([0-9.Ee+-]+)\r?$')
                        $closeBegin=@($closeMarks | Where-Object { $_.Groups[1].Value -eq 'BEGIN' })
                        $closeEnd=@($closeMarks | Where-Object { $_.Groups[1].Value -eq 'END' })
                        $beginMs=0.0; $endMs=0.0
                        if ($closeBegin.Count -eq 1 -and $closeEnd.Count -eq 1 -and
                            [double]::TryParse($closeBegin[0].Groups[2].Value,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$beginMs) -and
                            [double]::TryParse($closeEnd[0].Groups[2].Value,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$endMs) -and
                            -not [double]::IsNaN($beginMs) -and -not [double]::IsInfinity($beginMs) -and
                            -not [double]::IsNaN($endMs) -and -not [double]::IsInfinity($endMs) -and $endMs -ge $beginMs) {
                            $durationMs=$endMs-$beginMs
                            if (-not [double]::IsInfinity($durationMs)) {
                                $run.close.completed=$true; $run.close.durationMs=$durationMs; $run.close.reason=$null
                            } else { $run.close.reason='Formal close duration is non-finite.' }
                        } else { $run.close.reason='Formal close timestamps are missing, duplicated, non-finite or out of order.' }
                    } else { $run.close.reason='Nonzero process exit; formal close is incomplete.' }
                    Assert-UESTCCleanGodotLog $result
                    $matches=[regex]::Matches($result.StdOut, '(?m)^PERF_COLLECTION_RESULT (.+)\r?$')
                    if ($result.ExitCode -ne 0) { $run.reasons=@('Nonzero exit; original streams retained.') }
                    elseif ($matches.Count -ne 1) { $run.reasons=@('Missing or ambiguous game-owned completion.') }
                    else {
                        $observed=$matches[0].Groups[1].Value | ConvertFrom-Json
                        $run.observed=$observed
                        $run.polluted=$observed.polluted
                        $run.pollution=@($observed.pollution)
                        $run.coverage=$observed.coverage
                        $run.status=$observed.status
                        $run.reasons=@($observed.reasons)
                        if ($observed.scene -ne $entry.id -or $observed.sampleSegmentId -ne $segment -or
                            $observed.functionalSmoke -ne $request.functionalSmoke) {
                            $run.status='invalid'; $run.reasons+='Completion does not match its frozen request.'
                        }
                    }
                } catch {
                    $run.status='partial'; $run.reasons=@($_.Exception.ToString())
                    $run.timedOut=$_.Exception.Message -like 'Process timeout:*'
                }
                $accepted=Publish-CollectionRun $directory $run
                if ($accepted) {
                    $acceptedSources += @(Get-ChildItem -LiteralPath $directory -Directory | Where-Object {
                        Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json')
                    } | ForEach-Object { $_.FullName })
                }
                if ($accepted -or $run.status -eq 'partial') { break }
            }
            if (-not $accepted) { $failed=$true }
        }
    }
    $phaseRecord.completed=-not $failed
    $phaseRecord.status=if ($failed) { 'incomplete' } else { 'completed' }
} catch { $phaseRecord.status='failed'; $phaseRecord['error']=$_.Exception.ToString(); throw }
finally {
    try {
        Write-CollectionJson (Join-Path $phaseRoot 'phase.json') $phaseRecord
        try { Invoke-UESTCPerfAnalysis -Root $root -SessionSources $acceptedSources }
        catch {
            $failed=$true
            $phaseRecord['analysisError']=$_.Exception.ToString()
            Write-Warning $phaseRecord.analysisError
        }
        if ($Mode -eq 'formal') {
            & python -I -X utf8 (Join-Path $PSScriptRoot 'perf.py') --root $root batch $Batch
        } else {
            & python -I -X utf8 (Join-Path $PSScriptRoot 'perf.py') --root $root latest --origin ai --scope cost
        }
        if ($LASTEXITCODE -ne 0) { $failed=$true }
        if ($failed -and $phaseRecord.completed) {
            $phaseRecord.completed=$false
            $phaseRecord.status='incomplete'
            Write-CollectionJson (Join-Path $phaseRoot 'phase.json') $phaseRecord
        }
    } finally {
        Exit-UESTCBuildLock $lease
        if ($null -eq $inheritedBakeCache) { Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue }
        else { $env:UESTC_BAKE_CACHE=$inheritedBakeCache }
    }
}
if ($failed) { throw 'Collection incomplete; all attempts and original data are retained.' }
