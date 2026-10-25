# AI 固定采集唯一入口：冻结条件，分开本树准备与私桌面采样，保留失败并交既有分析器。
# 用法：powershell -File scripts/perf/run.ps1 -Batch TOOL-428-check [-Suite fixed|route|travel] [-Scene overview-default] [-PrepareBake]；正式轮加 -Mode formal -RoundNumber 0 [-PreviousRound id]；诊断用 -DeepenSession <id> [-DiagnosticTool gpu-profile,EventPipe,Nsight]。
# 产出：output/perf/runs/<Batch>/<Phase>/<场景>/<重复-尝试>/<会话>/run.json 与原始六件；functional 永不进入比较。
[CmdletBinding(DefaultParameterSetName='Collection')]
param([Parameter(Mandatory,ParameterSetName='Collection')][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9-]+$')][string]$Batch,
    [ValidateSet('fixed','route','travel')][string[]]$Suite=@('fixed','route','travel'), [string[]]$Scene=@(),
    [ValidateSet('functional-smoke','formal')][string]$Mode='functional-smoke',
    [ValidateRange(0,2147483647)][int]$RoundNumber=0, [string]$PreviousRound='',
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
$sampleSeconds=30
$smokeSeconds=2
$maximumAttempts=3
$otherGpuFraction=0.20
$maximumFactAgeSeconds=3
$timeoutSeconds=600
$judgement=[ordered]@{ logicalPauseVerified=$false; mainThreadLongStopVerified=$false;
    tuningSelectedMetrics=@() }
if ($BatchConditions) {
    $requested=Get-Content -LiteralPath $BatchConditions -Raw | ConvertFrom-Json
    foreach ($property in $requested.PSObject.Properties) {
        if (-not $judgement.Contains($property.Name)) { throw "Unknown batch condition: $($property.Name)" }
        $judgement[$property.Name]=$property.Value
    }
    foreach ($name in @('logicalPauseVerified','mainThreadLongStopVerified')) {
        if ($judgement[$name] -isnot [bool]) { throw "$name must be a recorded round-zero boolean." }
    }
}
$plan=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'scenarios.json') -Raw | ConvertFrom-Json
$selected=@(Get-CollectionScenes $plan $Suite $Scene)
if ($Mode -eq 'functional-smoke' -and -not $Scene.Count) {
    $selected=@($Suite | ForEach-Object { $kind=$_; $selected | Where-Object { $_.kind -eq $kind } | Select-Object -First 1 })
}
$sceneIds=@($selected | ForEach-Object { $_.scene.id })
$batchRoot=Assert-UESTCChildPath (Join-Path $root ('output\perf\runs\' + $Batch)) (Join-Path $root 'output\perf\runs')
$roundRoot=$batchRoot
if (Test-Path -LiteralPath $roundRoot) { throw 'Choose a fresh round; prior data is retained.' }
$null=[IO.Directory]::CreateDirectory($roundRoot)
$purpose=if ($Mode -eq 'formal') { 'baseline' } else { 'functional' }
$roundRecord=[ordered]@{ purpose=$purpose; number=$RoundNumber; previousId=$PreviousRound; scenes=$sceneIds; status='preparing';
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
    $roundRecord.preparation=$preparation
    Write-CollectionJson (Join-Path $roundRoot 'round.json') $roundRecord
    if ($Mode -eq 'formal') { Publish-CollectionRound $root $Batch $RoundNumber $roundRoot $sceneIds $PreviousRound }
    $godot=Get-UESTCGodot
    $environment=New-UESTCTestEnvironment -Work $roundRoot
    $environment['UESTC_BAKE_CACHE']=$null
    Update-UESTCGameBuild -Root $root -Godot $godot -BuildLease $heldLease -Timing $preparation -Environment $environment
    $preparation['bakeRequested']=[bool]$PrepareBake
    if ($PrepareBake) {
        $bake=Invoke-UESTCProcess $godot @('--headless','--path',(Join-Path $root 'game'),'--','--bake=all',('--map=' + $plan.map)) `
            $root (Join-Path $roundRoot 'preparation-bake.log') -TimeoutSeconds 14400 -Environment $environment
        Assert-UESTCCleanGodotLog $bake
        $preparation['bakeExitCode']=$bake.ExitCode
    }
    $roundRecord.status='collecting'
    Write-CollectionJson (Join-Path $roundRoot 'round.json') $roundRecord
    $count=if ($Mode -eq 'formal') { 2 } else { 1 }
    $attemptLimit=if ($Mode -eq 'formal') { $maximumAttempts } else { 1 }
    foreach ($item in $selected) {
        $confirmationOf=$null
        for ($sample=0; $sample -lt $count; $sample++) {
            $accepted=$false
            for ($attempt=1; $attempt -le $attemptLimit; $attempt++) {
                $entry=$item.scene
                $directory=Join-Path $roundRoot ($entry.id + '\' + $sample + '-' + $attempt)
                $null=[IO.Directory]::CreateDirectory($directory)
                $seconds=if ($Mode -ne 'formal') { $smokeSeconds } elseif ($item.kind -eq 'route') { $entry.seconds } else { $sampleSeconds }
                $segment='sample-' + [Guid]::NewGuid().ToString('N')
                $request=[ordered]@{ sceneId=$entry.id; sampleSegmentId=$segment; functionalSmoke=($Mode -eq 'functional-smoke'); stableFrames=$stableFrames;
                    realtimeSeconds=$realtimeSeconds; sampleSeconds=$seconds; otherGpuFraction=$otherGpuFraction;
                    maximumFactAgeSeconds=$maximumFactAgeSeconds;
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
                    roundId=$Batch; confirmationOf=$confirmationOf; conditions=$request;
                    collectionScriptEnteredUtc=$collectionScriptEnteredUtc;
                    launcher=@{ scriptEnteredUtc=$null; scriptEnteredTimestamp=$null; stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency;
                        processExitUtc=$null; preparation='Batch build/import and optional bake precede this attempt; not a double-click launcher measurement.' } }
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
            if (-not $accepted) { $failed=$true; break }
            if ($Mode -eq 'formal' -and $sample -eq 0) {
                Publish-CollectionRound $root $Batch $RoundNumber $roundRoot $sceneIds $PreviousRound
                $index=Get-Content -LiteralPath (Join-Path $root 'output/perf/index.json') -Raw | ConvertFrom-Json
                $round=@($index.batches | Where-Object { $_.id -eq $Batch })
                $group=@($round.groups | Where-Object { $_.comparisonKey.scene -eq $entry.id })
                if ($group.Count -ne 1) { throw 'Analyzer did not publish the collected scene.' }
                if (-not $group[0].confirmationPending) { break }
                $confirmationOf=$group[0].mainSessionId
            }
        }
    }
    $roundRecord.completed=-not $failed
    $roundRecord.status=if ($failed) { 'incomplete' } else { 'completed' }
} catch { $roundRecord.status='failed'; $roundRecord['error']=$_.Exception.ToString(); throw }
finally {
    try {
        Write-CollectionJson (Join-Path $roundRoot 'round.json') $roundRecord
        try { Invoke-UESTCPerfAnalysis -Root $root -SessionSources $acceptedSources }
        catch {
            $failed=$true
            $roundRecord['analysisError']=$_.Exception.ToString()
            Write-Warning $roundRecord.analysisError
        }
        if ($Mode -eq 'formal') {
            Publish-CollectionRound $root $Batch $RoundNumber $roundRoot $sceneIds $PreviousRound
        } else {
            & python -I -X utf8 (Join-Path $PSScriptRoot 'perf.py') --root $root latest --origin ai --scope cost
        }
        if ($LASTEXITCODE -ne 0) { $failed=$true }
        if ($failed -and $roundRecord.completed) {
            $roundRecord.completed=$false
            $roundRecord.status='incomplete'
            Write-CollectionJson (Join-Path $roundRoot 'round.json') $roundRecord
        }
    } finally {
        Exit-UESTCBuildLock $lease
        if ($null -eq $inheritedBakeCache) { Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue }
        else { $env:UESTC_BAKE_CACHE=$inheritedBakeCache }
    }
}
if ($failed) { throw 'Collection incomplete; all attempts and original data are retained.' }
