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
$diagnostic = $condition.measurement -eq 'diagnostic'
$playingSample = $condition.playingSample -eq $true
if ($playingSample -and (-not $diagnostic -or $Mode -ne 'immediate')) { throw 'Playing sample requires diagnostic immediate.' }
$timing = New-UESTCLaunchTiming -ScriptEnteredUtc $scriptEnteredUtc -ScriptEnteredTimestamp $scriptEnteredTimestamp
$lease = $null
$trace = $null
$gameProcess = $null
$stopped = $false
$stopAttempted = $false
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
# Native launch returns the PRIMARY OS thread ID. It inherits this worker's private desktop.
# Process.Start does not expose the primary OS TID; keep only this thin launch binding for that measured identity.
# The private-console Stop below controls dotnet-trace, never player input.
Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
public static class C215Launch {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct SI {
        public int cb; public string reserved, desktop, title;
        public uint x,y,width,height,xChars,yChars,fill,flags;
        public ushort show,reservedBytes; public IntPtr reservedPtr,input,output,error;
    }
    [StructLayout(LayoutKind.Sequential)] struct PI { public IntPtr process,thread; public uint pid,tid; }
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool CreateProcessW(string app, StringBuilder command, IntPtr pa, IntPtr ta,
        bool inherit, uint flags, IntPtr environment, string cwd, ref SI si, out PI pi);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [StructLayout(LayoutKind.Explicit, Size=20, CharSet=CharSet.Unicode)] struct Input {
        [FieldOffset(0)] public ushort type;
        [FieldOffset(4)] public int down;
        [FieldOffset(8)] public ushort repeat;
        [FieldOffset(10)] public ushort key;
        [FieldOffset(14)] public char character;
    }
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr GetStdHandle(int number);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool WriteConsoleInputW(IntPtr handle, Input[] records, uint count, out uint written);
    public static void RequestStop() {
        // dotnet-trace ignores redirected stdin. Its inherited private console is real stdin.
        var down = new Input { type=1, down=1, repeat=1, key=13, character='\r' };
        var up = down; up.down=0; uint written;
        if (!WriteConsoleInputW(GetStdHandle(-10), new[] {down,up}, 2, out written) || written!=2)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public static int[] Start(string exe, string arguments, string cwd) {
        var si = new SI(); si.cb = Marshal.SizeOf(typeof(SI)); PI pi;
        if (!CreateProcessW(exe, new StringBuilder("\""+exe+"\" "+arguments), IntPtr.Zero,
            IntPtr.Zero, false, 0, IntPtr.Zero, cwd, ref si, out pi))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        CloseHandle(pi.thread); CloseHandle(pi.process);
        return new[] { (int)pi.pid, (int)pi.tid };
    }
}
"@
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
function Stop-OwnTrace {
    $script:stopAttempted = $true
    if ($trace.HasExited) { throw "Collector exited before requested Stop: $($trace.ExitCode)" }
    $script:stopRequestedUtc = [DateTime]::UtcNow.ToString('O')
    [C215Launch]::RequestStop()
    # Complete EventPipe Stop and CLR rundown while the target remains alive.
    if (-not $trace.WaitForExit(120000)) { throw 'Collector did not finish Stop/rundown; target left alive for root cleanup.' }
    [IO.File]::WriteAllText((Join-Path $Output 'trace.stdout.txt'), $traceStdout.Result)
    [IO.File]::WriteAllText((Join-Path $Output 'trace.stderr.txt'), $traceStderr.Result)
    [IO.File]::WriteAllText((Join-Path $Output 'trace.exit.txt'), [string]$trace.ExitCode)
    if ($trace.ExitCode -ne 0) { throw "Collector failed: $($trace.ExitCode)" }
    if (-not [IO.File]::Exists((Join-Path $Output 'threads.nettrace'))) { throw 'Missing nettrace output.' }
    $script:stopped = $true
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
    $window = $godot -replace '_console\.exe$', '.exe'
    if (-not [IO.File]::Exists($window)) { $window = $godot }
    $log = Join-Path $Output 'godot.log'
    $save = Join-Path $Output 'save'
    $scenario = if ($playingSample) { 'playing' } else { $Mode }
    $gameArgs = @("--save-dir=$save", '--title=1', "--startup-scenario=$scenario")
    if ($playingSample) { $gameArgs += @('--quality=fine', '--quality-set=display_mode=0') }
    # All three conditions use normal production defaults; no gate/shots/frames CaptureMode.
    $arguments = @('--path', "$toolRoot\game", '--log-file', $log, '--') + $gameArgs
    $line = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
    # Explicitly override inherited telemetry: experience must never enable per-frame logging.
    $env:UESTC_STARTUP_PROFILE = if ($diagnostic) { '1' } else { '0' }
    $timing['engineLaunchRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    $launched = [C215Launch]::Start($window, $line, $toolRoot)
    $gameProcess = [Diagnostics.Process]::GetProcessById($launched[0])
    $mainThreadId = $launched[1]
    $processStart = $gameProcess.StartTime.ToUniversalTime()
    $timing['engineStartUtc'] = $processStart.ToString('O')
    $timing['status'] = 'ENGINE_STARTED'
    $attachRequestedUtc = $null
    $stopRequestedUtc = $null
    if ($diagnostic) {
        $traceInfo = [Diagnostics.ProcessStartInfo]::new()
        $traceInfo.FileName = "$toolRoot\output\tools\dotnet-diagnostics\dotnet-trace.exe"
        $traceInfo.Arguments = 'collect --process-id ' + $gameProcess.Id +
            ' --profile dotnet-sampled-thread-time --output ' +
            (ConvertTo-UESTCProcessArgument (Join-Path $Output 'threads.nettrace'))
        $traceInfo.UseShellExecute = $false; $traceInfo.CreateNoWindow = $false
        $traceInfo.RedirectStandardInput = $false # inherit private real console for Stop
        $traceInfo.RedirectStandardOutput = $true; $traceInfo.RedirectStandardError = $true
        $attachRequestedUtc = [DateTime]::UtcNow.ToString('O')
        $trace = [Diagnostics.Process]::Start($traceInfo)
        $traceStdout = $trace.StandardOutput.ReadToEndAsync()
        $traceStderr = $trace.StandardError.ReadToEndAsync()
        Read-CpuSnapshot 'attach-request'
    }
    [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId;
        engineStartUtc=$processStart.ToString('O'); launcher=$timing;
        collectorPid=if($trace){$trace.Id}else{$null}; attachRequestedUtc=$attachRequestedUtc } |
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
        if ($gameProcess.HasExited) { throw "Target exited before sampling boundary: $($gameProcess.ExitCode)" }
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
    if ($diagnostic) { Stop-OwnTrace }
    if ($gameProcess.HasExited) { throw 'Target exited before collector Stop/rundown completed.' }
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
    $budgets = (Get-Content -LiteralPath (Join-Path $toolRoot 'budgets.json') -Raw | ConvertFrom-Json).cache
    $validGeometry = $condition.geometryCache -eq 'valid'
    $warmPipelines = $condition.pipelineCache -eq 'warm'
    function Budget-Observation([string]$Key, $Actual, [bool]$Applicable, [string]$Endpoint) {
        $limit = $budgets.$Key
        $status = if ($diagnostic) { 'DIAGNOSTIC_ONLY' }
            elseif (-not $Applicable -or $null -eq $Actual) { 'NOT_MEASURED' }
            elseif ($Actual -le $limit) { 'MEETS_OBSERVED_PROXY_TARGET' } else { 'EXCEEDS_OBSERVED_PROXY_TARGET' }
        return [ordered]@{ key=$Key; targetSeconds=$limit; observedSeconds=$Actual; status=$status; endpoint=$Endpoint }
    }
    $checks = @(
        (Budget-Observation 'warmLauncherToTitleSecondsMax' $titleSeconds ($validGeometry -and $warmPipelines -and $condition.systemFileCache -eq 'warm') 'script-entry to static-menu-input-ready observation'),
        (Budget-Observation 'coldLauncherToTitleSecondsMax' $titleSeconds ($validGeometry -and $warmPipelines -and $condition.systemFileCache -eq 'reboot-first-run') 'script-entry to static-menu-input-ready observation'),
        (Budget-Observation 'firstRunLauncherToRealtimeSecondsMax' $realtimeSeconds ($validGeometry -and $condition.pipelineCache -eq 'first-run') 'script-entry to first Playing realtime observation after formal entry'),
        (Budget-Observation 'preloadedNewGameToWalkSecondsMax' $inputSeconds ($validGeometry -and $warmPipelines -and $preloadReady -eq $true) 'in-process Enter request to first Walk input-ready engine boundary; movement checked by core smoke'),
        (Budget-Observation 'immediateNewGameToWalkSecondsMax' $inputSeconds ($validGeometry -and $warmPipelines -and $Mode -eq 'immediate') 'in-process Enter request to first Walk input-ready engine boundary; movement checked by core smoke')
    )
    [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId; engineStartUtc=$processStart.ToString('O');
        mainThreadIdentification='CreateProcessW primary OS thread ID; compare only OS IDs, never CLR ManagedThreadID';
        mode=$Mode; captureEndpoint=$captureEndpoint; commandLine=$line; measurement=$condition.measurement; condition=$condition; launcher=$timing;
        preparation=$condition.preparation;
        collector=if($diagnostic){'EventPipe dotnet-sampled-thread-time; NOT on-CPU'}else{'OFF'};
        attachRequestedUtc=$attachRequestedUtc; collectorPid=if($trace){$trace.Id}else{$null}; collectorStopRequestedUtc=$stopRequestedUtc;
        collectorStoppedWhileTargetAlive=if($diagnostic){$stopped}else{$null}; collectorFinishedUtc=if($diagnostic){[DateTime]::UtcNow.ToString('O')}else{$null};
        phaseObservations=$anchors.ToArray(); cpuSnapshots=$snapshots.ToArray(); phasePollingMilliseconds=20;
        playingSample=[ordered]@{ enabled=$playingSample; state=$playingState; edges=$playingEdges.ToArray();
            targetIdleSeconds=if($playingSample){$playingTargetSeconds['playing-idle-start']}else{$null}; targetForwardHoldSeconds=if($playingSample){$playingTargetSeconds['playing-forward-start']}else{$null};
            physicalKey=$forwardPhysicalKey; request=$moveRequested; release=$releaseRequested; processCpuReads=$playingCpu.ToArray();
            method='Game-owned Input.ParseInputEvent; normal controller routing, real pose/velocity observations';
            diagnosticOverhead='EventPipe, every-frame engine logging, incremental 20ms log polling, phase thread snapshots and nominal 1s process counters are enabled. Read durations are retained. No causal overhead subtraction or experience qualification.' };
        baseline=(git -C $toolRoot rev-parse HEAD); captureMode=$false; cpuFunctionShare='NOT_MEASURED';
        frameTelemetryEnvironment=if($diagnostic){'UESTC_STARTUP_PROFILE=1'}else{'UESTC_STARTUP_PROFILE=0'};
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
        caveat='Normal interactive --title=1 with isolated empty save/log. Input scenarios retain production defaults and use Input.ParseInputEvent through the normal menu/controller. Game-owned phase markers retain actual engine time/frame; observer UTC includes log flush/poll/scheduling delay. Dwell requires actual preloadReady=true at acceptance. Title experience ends at static menu input readiness; title diagnostic additionally includes resource preload. Hidden-desktop render pacing is not presented FPS or evidence of experience stutter; renderer CPU/GPU work is measured separately. Experience has no EventPipe/per-frame logging/CPU snapshots; diagnostic rounds do not establish experience-target passes. Actual movement comes from real poses/velocity, never the injected action alone.'
    } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'result.json') -Encoding UTF8

} catch {
    $timing['status'] = 'FAILED'
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $Output 'failure.txt') -Encoding UTF8
    throw
} finally {
    if ($logReader) { $logReader.Dispose() }
    $timing | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launcher-timing.json') -Encoding UTF8
    if ($diagnostic) { $snapshots.ToArray() | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Output 'cpu-snapshots.json') -Encoding UTF8 }
    if ($playingSample) {
        [ordered]@{ state=$playingState; edges=$playingEdges.ToArray(); request=$moveRequested; release=$releaseRequested;
            processCpuReads=$playingCpu.ToArray(); movementStatus='Raw telemetry required; partial failure is not a completed movement sample' } |
            ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $Output 'playing-observations.json') -Encoding UTF8
    }
    if ($trace -and -not $stopped -and -not $stopAttempted) { Stop-OwnTrace }
    # A failed collector Stop leaves this exact collector/target alive, identified in launch.json.
    if ($gameProcess -and ($stopped -or -not $trace) -and -not $gameProcess.HasExited) {
        $null = $gameProcess.CloseMainWindow()
        if (-not $gameProcess.WaitForExit(15000)) { $gameProcess.Kill(); $gameProcess.WaitForExit() }
    }
    if ($trace -and $trace.HasExited) { $trace.Dispose() }
    Exit-UESTCBuildLock $lease
}


