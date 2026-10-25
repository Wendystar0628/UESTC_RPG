# 私有桌面启动体验/诊断：本轮从脚本进入计，包含等锁和正式输出检查，批次准备另记，诊断显式选择。
# 用法：由 isolated-launch.py 传入 -Output、-Mode、-ConditionPath。
# 产出：-Output 指定目录（采样日志、跟踪、JSON 记录与隔离存档；父目录由调用方传入）。
param([Parameter(Mandatory)][string]$Output, [ValidateSet('title','immediate','dwell')][string]$Mode,
    [Parameter(Mandatory)][string]$ConditionPath)
$scriptEnteredTimestamp = [Diagnostics.Stopwatch]::GetTimestamp()
$scriptEnteredUtc = [DateTime]::UtcNow
$ErrorActionPreference = 'Stop'
$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$toolRoot\scripts\lib\common.ps1"
. "$toolRoot\scripts\lib\start-game.ps1"
$condition = Get-Content -LiteralPath $ConditionPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($condition.measurement -notin @('experience','diagnostic')) { throw 'Unknown measurement condition.' }
if ($condition.pipelineCache -notin @('first-run','unknown')) { throw 'Unknown pipeline-cache condition; fresh native user directories support only first-run or unknown.' }
$diagnostic = $condition.measurement -eq 'diagnostic'
$playingSample = $condition.playingSample -eq $true
if ($playingSample -and (-not $diagnostic -or $Mode -ne 'immediate')) { throw 'Playing sample requires diagnostic immediate.' }
$timing = New-UESTCLaunchTiming -ScriptEnteredUtc $scriptEnteredUtc -ScriptEnteredTimestamp $scriptEnteredTimestamp
$lease = $null
$trace = $null
$eventPipe = $null
$collectorRecord = [ordered]@{}
$gameProcess = $null
$gameTree = $null
$failure = $null
$cleanupFailures = [Collections.Generic.List[object]]::new()
$result = $null
$shutdown = [ordered]@{ status='NOT_REQUESTED'; windowHandle=$null; closeRequestedUtc=$null;
    closePostedTimestamp=$null; exitObservedUtc=$null; exitCode=$null }
$snapshots = [Collections.Generic.List[object]]::new()
$playingCpu = [Collections.Generic.List[object]]::new()
$playingEdges = [Collections.Generic.List[object]]::new()
$forwardPhysicalKey = $null
$playingState = 'waiting'
$playingTargetSeconds = @{}
$moveRequested = $null
$releaseRequested = $null
$logReader = $null
$pendingLog = ''
$nextCpuRead = $null
# ProcessTree owns the native primary process/thread identity and each job.
if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path "$toolRoot\scripts\lib\ProcessTree.cs" }
function Read-CpuSnapshot([string]$Name) {
    $startedUtc = [DateTime]::UtcNow.ToString('O')
    $gameProcess.Refresh()
    $cpu = $gameProcess.TotalProcessorTime.TotalSeconds
    $cpuReadUtc = [DateTime]::UtcNow.ToString('O')
    $threads = [Collections.Generic.List[object]]::new()
    $unreadable = [Collections.Generic.List[object]]::new()
    foreach ($thread in $gameProcess.Threads) {
        $tid = $null
        try {
            $tid = $thread.Id
            $born = $thread.StartTime.ToUniversalTime().ToString('O')
            $seconds = $thread.TotalProcessorTime.TotalSeconds
            $threads.Add([ordered]@{ osThreadId=$tid; threadStartUtc=$born; cpuSeconds=$seconds;
                readUtc=[DateTime]::UtcNow.ToString('O') })
        } catch {
            # Threads can exit between enumeration and counter reads. Missing is never zero.
            $unreadable.Add([ordered]@{ osThreadId=$tid; error=$_.Exception.Message })
        } finally { $thread.Dispose() }
    }
    $snapshots.Add([ordered]@{ name=$Name; startedUtc=$startedUtc; processCpuReadUtc=$cpuReadUtc;
        processCpuSeconds=$cpu; threads=$threads.ToArray(); unreadableThreads=$unreadable.ToArray();
        finishedUtc=[DateTime]::UtcNow.ToString('O') })
}
function Read-PlayingCpu {
    $started = [DateTime]::UtcNow
    $readStarted = [Diagnostics.Stopwatch]::GetTimestamp()
    $gameProcess.Refresh()
    $seconds = $gameProcess.TotalProcessorTime.TotalSeconds
    $playingCpu.Add([ordered]@{ readUtc=[DateTime]::UtcNow.ToString('O'); processCpuSeconds=$seconds;
        startedUtc=$started.ToString('O'); observerMilliseconds=([Diagnostics.Stopwatch]::GetTimestamp()-$readStarted)*1000.0/[Diagnostics.Stopwatch]::Frequency })
}
try {
    $timing['lockRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $lease = Enter-UESTCBuildLock (Get-UESTCLayout $toolRoot).Locks
    $timing['lockAcquiredTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $timing['lockWaitStatus'] = 'MEASURED_IN_TOTAL'
    $godot = Get-UESTCGodot
    # Batch preparation is outside this script-entry clock. Check the same three
    # outputs as the product launcher; never prepare inside the measured interval.
    $timing['buildInvoked'] = $false
    $timing['importInvoked'] = $false
    $timing['outputCheckStartedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $outputsPresent = Test-UESTCLaunchOutputs -Root $toolRoot
    $timing['outputCheckCompletedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    if (-not $outputsPresent) {
        throw 'Prepared launch outputs are incomplete: the formal assembly, published business manifest and native import cache must all exist; no build is allowed inside the measured launch interval.'
    }
    $window = Get-UESTCGodotHost -Godot $godot
    $log = Join-Path $Output 'godot.log'
    $save = Join-Path $Output 'save'
    $scenario = if ($playingSample) { 'playing' } else { $Mode }
    $gameArgs = @("--save-dir=$save", '--title=1', "--startup-scenario=$scenario")
    if ($playingSample) { $gameArgs += @('--quality=fine', '--quality-set=display_mode=0') }
    if ($diagnostic) { $gameArgs += "--perf-out=$($condition.performanceOutputDirectory)" }
    # All three conditions use normal production defaults; no gate/shots/frames CaptureMode.
    $arguments = @('--path', "$toolRoot\game", '--log-file', $log, '--') + $gameArgs
    $line = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
    $timing['engineLaunchRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $gameTree = New-Object Uestc.Tools.ProcessTree($window, $line, $toolRoot, $true, [Text.Encoding]::UTF8, $false, $null)
    $gameProcess = [Diagnostics.Process]::GetProcessById($gameTree.ProcessId)
    $mainThreadId = $gameTree.MainThreadId
    $processStart = $gameProcess.StartTime.ToUniversalTime()
    $timing['engineStartUtc'] = $processStart.ToString('O')
    $timing['status'] = 'ENGINE_STARTED'
    $attachRequestedUtc = $null
    if ($diagnostic) {
        $eventPipe = @{ Target=$gameTree; ToolPath=(Get-UESTCDotnetTrace -Root $toolRoot);
            Root=$toolRoot; Output=$Output; Record=$collectorRecord; Deadline=[DateTime]::UtcNow.AddSeconds(300) }
        $timing['collector'] = $collectorRecord
        Invoke-UESTCEventPipeCapture -Action Start -Capture $eventPipe
        $trace = $eventPipe.Collector
        $attachRequestedUtc = $collectorRecord.attachRequestedUtc
        Read-CpuSnapshot 'attach-request'
    }
    [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId;
        engineStartUtc=$processStart.ToString('O'); launcher=$timing;
        collectorPid=if($trace){$trace.ProcessId}else{$null}; attachRequestedUtc=$attachRequestedUtc } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launch.json') -Encoding UTF8
    $anchors = [Collections.Generic.List[object]]::new()
    $seen = [Collections.Generic.HashSet[string]]::new()
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $completed = $false
    $titleInputReadyAt = $null
    $preloadObservedAt = $null
    $newGameEnterAnchor = $null
    $preloadReady = $null
    $inputRequest = $null
    $inputRelease = $null
    while ($clock.Elapsed.TotalSeconds -lt 300) {
        if ($gameTree.HasExited) { throw "Target exited before sampling boundary: $($gameTree.ExitCode)" }
        if ($trace -and $trace.HasExited) { throw "Collector exited before boundary: $($trace.ExitCode)" }
        $text = ''
        if ($null -eq $logReader -and [IO.File]::Exists($log)) {
            $stream = [IO.FileStream]::new($log, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
            $logReader = [IO.StreamReader]::new($stream)
        }
        if ($logReader) {
            $pendingLog += $logReader.ReadToEnd()
            $newline = $pendingLog.LastIndexOf("`n")
            if ($newline -ge 0) {
                $text = $pendingLog.Substring(0, $newline + 1)
                $pendingLog = $pendingLog.Substring($newline + 1)
            }
        }
        if ($playingSample) {
            $binding = [regex]::Match($text, '(?m)^STARTUP_INPUT action=move_forward physicalKey=(\d+)')
            if ($binding.Success) { $forwardPhysicalKey = [int]$binding.Groups[1].Value }
        }
        foreach ($environment in [regex]::Matches($text, '(?m)^STARTUP_ENVIRONMENT [^\r\n]*\bwindowHandle=(\d+)(?:\s|$)')) {
            $handle = [long]::Parse($environment.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
            if ($handle -le 0) { throw 'STARTUP_ENVIRONMENT did not publish a real game window handle.' }
            $shutdown.windowHandle = $handle
        }
        $observed = [DateTime]::UtcNow.ToString('O')
        foreach ($m in [regex]::Matches($text, '(?m)^STARTUP_PHASE ([^ ]+) ([0-9.]+)([^\r\n]*)')) {
            if ($m.Groups[1].Value -eq 'input-ready' -and $m.Groups[3].Value -notmatch '(?:^|\s)mode=Walk(?:\s|$)') { continue }
            if ($seen.Add($m.Groups[1].Value)) {
                $anchors.Add([ordered]@{name=$m.Groups[1].Value;
                    milliseconds=[double]::Parse($m.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture);
                    observedUtc=$observed; observedTimestamp=[Diagnostics.Stopwatch]::GetTimestamp(); suffix=$m.Groups[3].Value})
                if ($diagnostic) { Read-CpuSnapshot $m.Groups[1].Value }
                if ($m.Groups[1].Value -eq 'title-input-ready') { $titleInputReadyAt = $clock.Elapsed.TotalSeconds }
                if ($m.Groups[1].Value -eq 'preload-ready') {
                    if ($m.Groups[3].Value -notmatch '(?:^|\s)preloadReady=True(?:\s|$)') {
                        throw 'preload-ready must report real new-game resource readiness.'
                    }
                    $preloadObservedAt = $clock.Elapsed.TotalSeconds
                }
                if ($m.Groups[1].Value -eq 'new-game-enter') { $newGameEnterAnchor = $anchors[$anchors.Count - 1] }
                $edge = $anchors[$anchors.Count - 1]
                if ($edge.name -in @('playing-idle-start','playing-forward-start')) {
                    $target = [regex]::Match($edge.suffix, '(?:^|\s)targetMilliseconds=([0-9.]+)(?:\s|$)')
                    if (-not $target.Success) { throw "$($edge.name) did not publish its target duration." }
                    $playingTargetSeconds[$edge.name] = [double]::Parse($target.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture) / 1000
                }
                switch ($edge.name) {
                    'title-enter-press' { $inputRequest = $edge }
                    'title-enter-release' { $inputRelease = $edge }
                    'playing-idle-start' { $playingEdges.Add($edge); $playingState = 'idle'; $nextCpuRead = $clock.Elapsed.TotalSeconds }
                    'playing-idle-end' { $playingEdges.Add($edge) }
                    'playing-forward-start' { $playingEdges.Add($edge); $moveRequested = $edge; $playingState = 'forward' }
                    'playing-forward-end' { $playingEdges.Add($edge); $releaseRequested = $edge; $playingState = 'released' }
                    'playing-release-observed' { $playingEdges.Add($edge); $playingState = 'complete' }
                }
            }
        }
        if ($null -ne $inputRelease -and $null -ne $newGameEnterAnchor) {
            $preload = [regex]::Match($newGameEnterAnchor.suffix, '(?:^|\s)preloadReady=(true|false)(?:\s|$)', 'IgnoreCase')
            if (-not $preload.Success) { throw 'new-game-enter is missing its real preloadReady state.' }
            $preloadReady = [bool]::Parse($preload.Groups[1].Value)
            if ($Mode -eq 'dwell' -and -not $preloadReady) {
                throw 'Dwell entered without real new-game resources retained and ready.'
            }
        }
        $completed = if ($Mode -eq 'title') {
            # The diagnostic endpoint additionally includes background resource preparation.
            $null -ne $titleInputReadyAt -and (-not $diagnostic -or $null -ne $preloadObservedAt)
        } else {
            $seen.Contains('realtime') -and $null -ne $inputRelease -and
                $null -ne $newGameEnterAnchor -and $seen.Contains('input-ready')
        }
        if ($playingSample) {
            if ($null -ne $nextCpuRead -and $clock.Elapsed.TotalSeconds -ge $nextCpuRead) {
                Read-PlayingCpu
                $nextCpuRead = $clock.Elapsed.TotalSeconds + 1
            }
            $completed = $completed -and $playingState -eq 'complete'
        }
        if ($completed) { break }
        Start-Sleep -Milliseconds 20
    }
    if (-not $completed) { throw "Startup sampling boundary was not observed; playingState=$playingState. Raw telemetry retained." }
    if ($diagnostic) { Invoke-UESTCEventPipeCapture -Action Stop -Capture $eventPipe }
    if ($gameTree.HasExited) { throw 'Target exited before collector Stop/rundown completed.' }
    if ($null -eq $shutdown.windowHandle) { throw 'Sampling completed without the game-owned STARTUP_ENVIRONMENT windowHandle; raw telemetry retained.' }
    $shutdown.status = 'CLOSE_REQUESTED'
    $shutdown.closeRequestedUtc = [DateTime]::UtcNow.ToString('O')
    $shutdown.closePostedTimestamp = $gameTree.PostClose([IntPtr]([long]$shutdown.windowHandle))
    # WM_CLOSE enters the production async shutdown chain. Job cleanup is never a successful exit.
    if (-not $gameProcess.WaitForExit(30000)) {
        $shutdown.status = 'TIMED_OUT'
        throw 'Game did not finish its formal window-close shutdown within 30 seconds; raw telemetry retained.'
    }
    $shutdown.exitObservedUtc = [DateTime]::UtcNow.ToString('O')
    # GetProcessById is a counter observer; the native owner retains the handle after exit.
    $shutdown.exitCode = $gameTree.ExitCode
    if ($shutdown.exitCode -ne 0) {
        $shutdown.status = 'EXIT_FAILED'
        $exitFailure = [InvalidOperationException]::new("Game failed during formal shutdown: exit $($shutdown.exitCode). See $log")
        $exitFailure.Data['ExitCode'] = $shutdown.exitCode
        throw $exitFailure
    }
    $shutdown.status = 'EXITED'
    $gameTree.Finish()
    Assert-UESTCCleanGodotLog ([pscustomobject]@{ Output=([IO.File]::ReadAllText($log) + $gameTree.StdOut + $gameTree.StdErr); Log=$log })
    $shutdown.status = 'COMPLETED'
    $phaseByName = @{}
    foreach ($anchor in $anchors) { $phaseByName[$anchor.name] = $anchor }
    function Elapsed-Seconds([long]$From, [long]$To) {
        return ($To - $From) / [double][Diagnostics.Stopwatch]::Frequency
    }
    function Observed-Seconds([string]$Name, [long]$From) {
        if (-not $phaseByName.ContainsKey($Name)) { return $null }
        return Elapsed-Seconds $From $phaseByName[$Name].observedTimestamp
    }
    $titleSeconds = Observed-Seconds 'title-input-ready' $scriptEnteredTimestamp
    $captureEndpoint = if ($Mode -eq 'title') {
        if ($diagnostic) { 'static-menu-input-ready-and-background-new-game-preload-ready' }
        else { 'static-menu-input-ready' }
    } elseif ($playingSample) { 'first-Walk-plus-driver-timed-diagnostic-idle-forward-and-observed-action-release' }
    else { 'playing-realtime-and-Walk-input-ready' }
    $realtimeSeconds = Observed-Seconds 'realtime' $scriptEnteredTimestamp
    $inputSeconds = if ($inputRequest -and $phaseByName.ContainsKey('input-ready')) {
        ($phaseByName['input-ready'].milliseconds - $inputRequest.milliseconds) / 1000
    } else { $null }
    $budgets = (Get-Content -LiteralPath (Join-Path $toolRoot 'budgets.json') -Raw | ConvertFrom-Json).waits
    # ENG-PERF-DATA: AI startup observations cannot prove the full double-click upper bound.
    $checks = @([ordered]@{ key='firstRunLauncherToRealtimeSecondsMax';
        targetSeconds=$budgets.firstRunLauncherToRealtimeSecondsMax; observedSeconds=$realtimeSeconds;
        status='NOT_JUDGED_AI_STARTUP'; endpoint='script-entry to first Playing realtime observation after formal entry';
        reason='AI launch timing excludes the full double-click path; startup targets remain unjudged.' })
    $result = [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId; engineStartUtc=$processStart.ToString('O');
        mainThreadIdentification='CreateProcessW primary OS thread ID; compare only OS IDs, never CLR ManagedThreadID';
        mode=$Mode; captureEndpoint=$captureEndpoint; commandLine=$line; measurement=$condition.measurement; condition=$condition; launcher=$timing;
        preparation=$condition.preparation;
        collector=if($diagnostic){'EventPipe dotnet-sampled-thread-time + CLR GC allocation/type/stack (0x41080001:5); NOT on-CPU'}else{'OFF'};
        attachRequestedUtc=$attachRequestedUtc; collectorPid=if($trace){$trace.ProcessId}else{$null};
        collectorStopRequestedUtc=if($diagnostic){$collectorRecord.collectorStopRequestedUtc}else{$null};
        collectorStoppedWhileTargetAlive=if($diagnostic){$collectorRecord.collectorStoppedWhileTargetAlive}else{$null};
        collectorFinishedUtc=if($diagnostic){$collectorRecord.collectorFinishedUtc}else{$null};
        shutdown=$shutdown;
        phaseObservations=$anchors.ToArray(); cpuSnapshots=$snapshots.ToArray(); phasePollingMilliseconds=20;
        playingSample=[ordered]@{ enabled=$playingSample; state=$playingState; edges=$playingEdges.ToArray();
            targetIdleSeconds=if($playingSample){$playingTargetSeconds['playing-idle-start']}else{$null}; targetForwardHoldSeconds=if($playingSample){$playingTargetSeconds['playing-forward-start']}else{$null};
            physicalKey=$forwardPhysicalKey; request=$moveRequested; release=$releaseRequested; processCpuReads=$playingCpu.ToArray();
            method='Game-owned Input.ParseInputEvent; normal controller routing and actual driver phase observations';
            diagnosticOverhead='EventPipe, performance recording, incremental 20ms phase log polling, phase thread snapshots and nominal 1s process counters are enabled. Read durations are retained. No causal overhead subtraction or experience qualification.' };
        baseline=(git -C $toolRoot rev-parse HEAD); captureMode=$false; cpuFunctionShare='NOT_MEASURED';
        performance=if($diagnostic){[ordered]@{ recordingRoot=$condition.performanceOutputDirectory; derivedRoot=(Join-Path $toolRoot 'output\perf\derived') }}else{$null};
        timing=[ordered]@{ origin='script-entry Stopwatch timestamp; phase milliseconds retain the separate engine origin';
            scriptToEngineLaunchRequestSeconds=(Elapsed-Seconds $scriptEnteredTimestamp $timing.engineLaunchRequestedTimestamp);
            lockWaitSeconds=(Elapsed-Seconds $timing.lockRequestedTimestamp $timing.lockAcquiredTimestamp);
            outputCheckSeconds=(Elapsed-Seconds $timing.outputCheckStartedTimestamp $timing.outputCheckCompletedTimestamp);
            scriptToMenuObservedSeconds=$titleSeconds; scriptToPlayingRealtimeObservedSeconds=$realtimeSeconds;
            scriptToNewGamePreloadReadyObservedSeconds=(Observed-Seconds 'preload-ready' $scriptEnteredTimestamp);
            inProcessRequestToWalkInputReadyEngineSeconds=$inputSeconds;
            movementResponseSeconds=$null; movementStatus='NOT_MEASURED_IN_TIMING_ROUND; core smoke checks real directional displacement';
            limit='Observed log boundaries include flush/poll/scheduling delay, nominal poll 20ms; no guessed overhead subtraction. Worker native-binding setup is included before lock/output check, conservatively beyond the product launcher. Explicit preparation is recorded separately outside script-entry time. Process StartTime UTC and engine phase milliseconds have separate origins.' };
        budgetObservations=$checks;
        input=[ordered]@{ method='Game-owned Input.ParseInputEvent Enter press/release through the normal TitleMenu';
            request=$inputRequest; release=$inputRelease;
            engineRequestToReleaseMilliseconds=if($inputRequest -and $inputRelease){$inputRelease.milliseconds-$inputRequest.milliseconds}else{$null};
            spawnPreloadedAtAcceptance=$preloadReady;
            waitCondition=if($Mode -eq 'dwell'){'real-new-game-preload-ready-and-menu-input-ready'}elseif($Mode -eq 'immediate'){'menu-input-ready'}else{$null};
            observedMenuInputReadyToPreloadReadyWaitSeconds=if($null -eq $preloadObservedAt -or $null -eq $titleInputReadyAt){$null}else{[Math]::Max(0, $preloadObservedAt-$titleInputReadyAt)};
            engineMenuInputReadyToRequestSeconds=if($inputRequest){($inputRequest.milliseconds-$phaseByName['title-input-ready'].milliseconds)/1000}else{$null} };
        caveat='Normal interactive --title=1 with isolated empty save/log. Input scenarios retain production defaults and use Input.ParseInputEvent through the normal menu/controller. Game-owned phase markers retain actual engine time/frame; observer UTC includes log flush/poll/scheduling delay. Dwell requires actual preloadReady=true at acceptance. Title experience ends at static menu input readiness; title diagnostic additionally includes resource preload. Hidden-desktop render pacing is not presented FPS or evidence of experience stutter; renderer CPU/GPU work is measured separately. Experience has no EventPipe/performance recording/CPU snapshots; diagnostic rounds do not establish experience-target passes. Driver completion is not a displacement measurement; no movement is inferred from the injected action alone.'
    }

} catch {
    $failure = $_
    $timing['status'] = 'FAILED'
} finally {
    # Try every owned cleanup and evidence write; retain the primary error object when cleanup also fails.
    $cleanup = [ordered]@{
        Collector = { if ($eventPipe) { Invoke-UESTCEventPipeCapture -Action Dispose -Capture $eventPipe } }
        OwnedJob = { if ($gameTree) { $gameTree.Dispose() } }
        GameOutput = {
            if ($gameTree) {
                [IO.File]::WriteAllText((Join-Path $Output 'game.stdout.txt'), $gameTree.StdOut)
                [IO.File]::WriteAllText((Join-Path $Output 'game.stderr.txt'), $gameTree.StdErr)
            }
        }
        LogReader = { if ($logReader) { $logReader.Dispose() } }
        GameProcess = { if ($gameProcess) { $gameProcess.Dispose() } }
        CpuSnapshots = {
            if ($diagnostic) { $snapshots.ToArray() | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Output 'cpu-snapshots.json') -Encoding UTF8 }
        }
        PlayingObservations = {
            if ($playingSample) {
                [ordered]@{ state=$playingState; edges=$playingEdges.ToArray(); request=$moveRequested; release=$releaseRequested;
                    processCpuReads=$playingCpu.ToArray(); movementStatus='Raw telemetry required; partial failure is not a completed movement sample' } |
                    ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $Output 'playing-observations.json') -Encoding UTF8
            }
        }
        ShutdownRecord = { $shutdown | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'shutdown.json') -Encoding UTF8 }
        BuildLock = { Exit-UESTCBuildLock $lease }
    }
    foreach ($step in $cleanup.GetEnumerator()) {
        try { & $step.Value }
        catch {
            $cleanupFailures.Add([ordered]@{ step=$step.Key; error=$_.Exception.ToString() })
            if ($failure) { $failure.Exception.Data["StartupCapture.$($step.Key)"] = $_.Exception }
            else { $failure = $_ }
        }
    }
}
if ($failure) { $timing['status'] = 'FAILED' }
try { $timing | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launcher-timing.json') -Encoding UTF8 }
catch {
    if ($failure) { $failure.Exception.Data['StartupCapture.LauncherTiming'] = $_.Exception }
    else { $failure = $_ }
}
if ($failure) {
    try {
        ($failure | Out-String) + ($cleanupFailures.ToArray() | ConvertTo-Json -Depth 5) |
            Set-Content -LiteralPath (Join-Path $Output 'failure.txt') -Encoding UTF8
    } catch { $failure.Exception.Data['StartupCapture.FailureRecord'] = $_.Exception }
    throw $failure
}
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'result.json') -Encoding UTF8

