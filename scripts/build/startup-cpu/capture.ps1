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
$timing = New-UESTCLaunchTiming -ScriptEnteredUtc $scriptEnteredUtc -ScriptEnteredTimestamp $scriptEnteredTimestamp
$lease = $null
$trace = $null
$gameProcess = $null
$stopped = $false
$stopAttempted = $false
$inputWindowHandle = $null
$inputReleasedUtc = $null
$snapshots = [Collections.Generic.List[object]]::new()
# Native launch returns the PRIMARY OS thread ID. It inherits this worker's private desktop.
# This small launch binding runs before the game; it is not a CPU sampler.
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
    delegate bool WindowCallback(IntPtr hwnd, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(WindowCallback callback, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr hwnd, StringBuilder name, int count);
    [DllImport("user32.dll", SetLastError=true)] static extern bool PostMessageW(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
    public static long NewGameEnter(int pid) {
        IntPtr target = IntPtr.Zero;
        EnumWindows((hwnd, _) => {
            uint owner; GetWindowThreadProcessId(hwnd, out owner);
            var name = new StringBuilder(128);
            if (owner == (uint)pid && IsWindowVisible(hwnd) &&
                GetClassNameW(hwnd, name, name.Capacity) != 0 && name.ToString() == "Engine") {
                target=hwnd; return false;
            }
            return true;
        }, IntPtr.Zero);
        if (target == IntPtr.Zero) throw new InvalidOperationException("No visible Godot Engine window for this private target PID.");
        // The empty isolated save makes TitleMenu focus NewGame. Directed window messages
        // stay on this worker's private desktop; never use global SendInput or SwitchDesktop.
        if (!PostMessageW(target, 0x100, (IntPtr)13, (IntPtr)0x001c0001))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return target.ToInt64();
    }
    public static void ReleaseEnter(long hwnd) {
        if (!PostMessageW((IntPtr)hwnd, 0x101, (IntPtr)13, (IntPtr)unchecked((int)0xc01c0001)))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
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
    # Batch preparation is outside this script-entry clock. Require its output so
    # the measured default-launch path can only take the existing-output branch.
    if (-not [IO.File]::Exists((Get-UESTCGameAssemblyPath -Root $toolRoot))) {
        throw 'Prepared game output is missing; no build is allowed inside the measured launch interval.'
    }
    Initialize-UESTCLaunch -Root $toolRoot -Godot $godot -BuildLease $lease -Timing $timing
    $window = $godot -replace '_console\.exe$', '.exe'
    if (-not [IO.File]::Exists($window)) { $window = $godot }
    $log = Join-Path $Output 'godot.log'
    $save = Join-Path $Output 'save'
    $gameArgs = @("--save-dir=$save", '--title=1')
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
    $titleObservedAt = $null
    $titleReadyFrame = $null
    $titleInputReadyAt = $null
    $preloadObservedAt = $null
    $newGameEnterAnchor = $null
    $preloadReady = $null
    $inputRequestedUtc = $null
    $inputRequestedClockSeconds = $null
    $inputRequestedTimestamp = $null
    $inputReleasedTimestamp = $null
    $shortPressMilliseconds = 100
    while ($clock.Elapsed.TotalSeconds -lt 300) {
        if ($gameProcess.HasExited) { throw "Target exited before sampling boundary: $($gameProcess.ExitCode)" }
        if ($trace -and $trace.HasExited) { throw "Collector exited before boundary: $($trace.ExitCode)" }
        $text = ''
        if ([IO.File]::Exists($log)) {
            $stream = [IO.FileStream]::new($log, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
            $reader = [IO.StreamReader]::new($stream)
            try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
        }
        $observed = [DateTime]::UtcNow.ToString('O')
        foreach ($m in [regex]::Matches($text, '(?m)^STARTUP_PHASE ([^ ]+) ([0-9.]+)([^\r\n]*)')) {
            if ($m.Groups[1].Value -eq 'input-ready' -and $m.Groups[3].Value -notmatch '(?:^|\s)mode=Walk(?:\s|$)') { continue }
            if ($seen.Add($m.Groups[1].Value)) {
                $anchors.Add([ordered]@{name=$m.Groups[1].Value;
                    milliseconds=[double]::Parse($m.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture);
                    observedUtc=$observed; observedTimestamp=[Diagnostics.Stopwatch]::GetTimestamp(); suffix=$m.Groups[3].Value})
                if ($diagnostic) { Read-CpuSnapshot $m.Groups[1].Value }
                if ($m.Groups[1].Value -eq 'title-ready') {
                    $titleObservedAt = $clock.Elapsed.TotalSeconds
                    $titleReadyFrame = [long][regex]::Match($m.Groups[3].Value, 'frame=(\d+)').Groups[1].Value
                }
                if ($m.Groups[1].Value -eq 'title-input-ready') { $titleInputReadyAt = $clock.Elapsed.TotalSeconds }
                if ($m.Groups[1].Value -eq 'preload-ready') {
                    if ($m.Groups[3].Value -notmatch '(?:^|\s)preloadReady=True(?:\s|$)') {
                        throw 'preload-ready must report real new-game resource readiness.'
                    }
                    $preloadObservedAt = $clock.Elapsed.TotalSeconds
                }
                if ($m.Groups[1].Value -eq 'new-game-enter') { $newGameEnterAnchor = $anchors[$anchors.Count - 1] }
            }
        }
        if ($Mode -ne 'title' -and $null -ne $titleInputReadyAt -and $null -eq $inputRequestedUtc -and
            ($Mode -eq 'immediate' -or $null -ne $preloadObservedAt)) {
            # Menu input is independent of background resource loading. Dwell waits for real preload.
            $inputRequestedUtc = [DateTime]::UtcNow.ToString('O')
            $inputRequestedTimestamp = [Diagnostics.Stopwatch]::GetTimestamp()
            $inputRequestedClockSeconds = $clock.Elapsed.TotalSeconds
            $inputWindowHandle = [C215Launch]::NewGameEnter($gameProcess.Id)
        }
        if ($null -ne $inputWindowHandle -and $null -eq $inputReleasedUtc -and
            ([Diagnostics.Stopwatch]::GetTimestamp() - $inputRequestedTimestamp) * 1000.0 /
                [Diagnostics.Stopwatch]::Frequency -ge $shortPressMilliseconds) {
            # Normal short press: release on elapsed request time, then observe acceptance.
            # Wall time does not prove that the game processed any particular frame.
            [C215Launch]::ReleaseEnter($inputWindowHandle)
            $inputReleasedTimestamp = [Diagnostics.Stopwatch]::GetTimestamp()
            $inputReleasedUtc = [DateTime]::UtcNow.ToString('O')
        }
        if ($null -ne $inputRequestedClockSeconds -and -not $seen.Contains('new-game-enter') -and
            $clock.Elapsed.TotalSeconds -gt $inputRequestedClockSeconds + 15) {
            throw "Directed Enter did not reach new-game-enter within 15s; hwnd=$inputWindowHandle titleFrame=$titleReadyFrame; no valid immediate/dwell result."
        }
        if ($null -ne $inputReleasedTimestamp -and $null -ne $newGameEnterAnchor) {
            $preload = [regex]::Match($newGameEnterAnchor.suffix, '(?:^|\s)preloadReady=(true|false)(?:\s|$)', 'IgnoreCase')
            if (-not $preload.Success) { throw 'new-game-enter is missing its real preloadReady state.' }
            $preloadReady = [bool]::Parse($preload.Groups[1].Value)
            if ($Mode -eq 'dwell' -and -not $preloadReady) {
                throw 'Dwell entered without real new-game resources retained and ready; no valid preloaded result.'
            }
        }
        $completed = if ($Mode -eq 'title') {
            # The diagnostic endpoint additionally includes background resource preparation.
            $null -ne $titleInputReadyAt -and (-not $diagnostic -or $null -ne $preloadObservedAt)
        } else {
            $seen.Contains('realtime') -and $null -ne $inputReleasedTimestamp -and
                $null -ne $newGameEnterAnchor -and $seen.Contains('input-ready')
        }
        if ($completed) { break }
        Start-Sleep -Milliseconds 20
    }
    if (-not $completed) { throw 'Startup phase deadline exceeded.' }
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
    } else { 'playing-realtime-and-Walk-input-ready' }
    $realtimeSeconds = Observed-Seconds 'realtime' $scriptEnteredTimestamp
    $inputSeconds = if ($null -ne $inputRequestedTimestamp) { Observed-Seconds 'input-ready' $inputRequestedTimestamp } else { $null }
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
        (Budget-Observation 'preloadedNewGameToWalkSecondsMax' $inputSeconds ($validGeometry -and $warmPipelines -and $preloadReady -eq $true) 'directed request to first Walk input-release observation; movement checked by core smoke'),
        (Budget-Observation 'immediateNewGameToWalkSecondsMax' $inputSeconds ($validGeometry -and $warmPipelines -and $Mode -eq 'immediate') 'directed request to first Walk input-release observation; movement checked by core smoke')
    )
    [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId; engineStartUtc=$processStart.ToString('O');
        mainThreadIdentification='CreateProcessW primary OS thread ID; compare only OS IDs, never CLR ManagedThreadID';
        mode=$Mode; captureEndpoint=$captureEndpoint; commandLine=$line; measurement=$condition.measurement; condition=$condition; launcher=$timing;
        preparation=$condition.preparation;
        collector=if($diagnostic){'EventPipe dotnet-sampled-thread-time; NOT on-CPU'}else{'OFF'};
        attachRequestedUtc=$attachRequestedUtc; collectorPid=if($trace){$trace.Id}else{$null}; collectorStopRequestedUtc=$stopRequestedUtc;
        collectorStoppedWhileTargetAlive=if($diagnostic){$stopped}else{$null}; collectorFinishedUtc=if($diagnostic){[DateTime]::UtcNow.ToString('O')}else{$null};
        phaseObservations=$anchors.ToArray(); cpuSnapshots=$snapshots.ToArray(); phasePollingMilliseconds=20;
        baseline=(git -C $toolRoot rev-parse HEAD); captureMode=$false; cpuFunctionShare='NOT_MEASURED';
        frameTelemetryEnvironment=if($diagnostic){'UESTC_STARTUP_PROFILE=1'}else{'UESTC_STARTUP_PROFILE=0'};
        timing=[ordered]@{ origin='script-entry Stopwatch timestamp; phase milliseconds retain the separate engine origin';
            scriptToEngineLaunchRequestSeconds=(Elapsed-Seconds $scriptEnteredTimestamp $timing.engineLaunchRequestedTimestamp);
            lockWaitSeconds=(Elapsed-Seconds $timing.lockRequestedTimestamp $timing.lockAcquiredTimestamp);
            outputCheckSeconds=(Elapsed-Seconds $timing.outputCheckStartedTimestamp $timing.outputCheckCompletedTimestamp);
            scriptToMenuObservedSeconds=$titleSeconds; scriptToPlayingRealtimeObservedSeconds=$realtimeSeconds;
            scriptToNewGamePreloadReadyObservedSeconds=(Observed-Seconds 'preload-ready' $scriptEnteredTimestamp);
            directedRequestToWalkInputReleaseObservedSeconds=$inputSeconds;
            movementResponseSeconds=$null; movementStatus='NOT_MEASURED_IN_TIMING_ROUND; core smoke checks real directional displacement';
            limit='Observed log boundaries include flush/poll/scheduling delay, nominal poll 20ms; no guessed overhead subtraction. Worker native-binding setup is included before lock/output check, conservatively beyond the product launcher. Explicit preparation is recorded separately outside script-entry time. Process StartTime UTC and engine phase milliseconds have separate origins.' };
        budgetObservations=$checks;
        input=[ordered]@{ method='PID-directed private-desktop WM_KEYDOWN/UP Enter; normal TitleMenu NewGame focus';
            requestedUtc=$inputRequestedUtc; requestedTimestamp=$inputRequestedTimestamp; targetWindowHandle=$inputWindowHandle;
            titleReadyFrame=$titleReadyFrame; releasedUtc=$inputReleasedUtc; releasedTimestamp=$inputReleasedTimestamp;
            shortPressTargetMilliseconds=if($Mode -eq 'title'){$null}else{$shortPressMilliseconds};
            observedRequestToReleaseMilliseconds=if($null -eq $inputReleasedTimestamp){$null}else{
                (Elapsed-Seconds $inputRequestedTimestamp $inputReleasedTimestamp) * 1000};
            spawnPreloadedAtAcceptance=$preloadReady;
            waitCondition=if($Mode -eq 'dwell'){'real-new-game-preload-ready-and-menu-input-ready'}elseif($Mode -eq 'immediate'){'menu-input-ready'}else{$null};
            observedMenuInputReadyToPreloadReadyWaitSeconds=if($null -eq $preloadObservedAt -or $null -eq $titleInputReadyAt){$null}else{[Math]::Max(0, $preloadObservedAt-$titleInputReadyAt)};
            observedMenuInputReadyToRequestSeconds=if($null -eq $inputRequestedClockSeconds){$null}else{$inputRequestedClockSeconds-$titleInputReadyAt};
            observedTitleToRequestSeconds=if($null -eq $inputRequestedClockSeconds){$null}else{$inputRequestedClockSeconds-$titleObservedAt}};
        caveat='Normal --title=1 with isolated empty save/log, no CaptureMode. Sparse title-input-ready precedes directed Enter; elapsed wall time releases the short press, then new-game-enter proves acceptance. The full request-to-Walk interval includes the press; no game-frame crossing is inferred. Dwell waits for sparse preload-ready and requires preloadReady=true again at new-game-enter. Title experience ends at the static menu input marker; title diagnostic additionally waits for new-game resources, a different endpoint. Realtime is observed only after formal entry into Playing. Unknown cache states do not qualify budgets. Experience has no EventPipe/per-frame log/CPU snapshots; diagnostics never establish experience-target passes. First Walk input release is a proxy; real movement is checked separately by the existing core smoke.'
    } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'result.json') -Encoding UTF8

} catch {
    $timing['status'] = 'FAILED'
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $Output 'failure.txt') -Encoding UTF8
    throw
} finally {
    # A failure after keydown still releases only this run's target window before cleanup.
    if ($null -ne $inputWindowHandle -and $null -eq $inputReleasedUtc -and
        $gameProcess -and -not $gameProcess.HasExited) {
        [C215Launch]::ReleaseEnter($inputWindowHandle)
        $inputReleasedUtc = [DateTime]::UtcNow.ToString('O')
    }
    $timing | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launcher-timing.json') -Encoding UTF8
    if ($diagnostic) { $snapshots.ToArray() | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Output 'cpu-snapshots.json') -Encoding UTF8 }
    if ($trace -and -not $stopped -and -not $stopAttempted) { Stop-OwnTrace }
    # A failed collector Stop leaves this exact collector/target alive, identified in launch.json.
    if ($gameProcess -and ($stopped -or -not $trace) -and -not $gameProcess.HasExited) {
        $null = $gameProcess.CloseMainWindow()
        if (-not $gameProcess.WaitForExit(15000)) { $gameProcess.Kill(); $gameProcess.WaitForExit() }
    }
    if ($trace -and $trace.HasExited) { $trace.Dispose() }
    Exit-UESTCBuildLock $lease
}


