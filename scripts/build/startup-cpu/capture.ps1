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
$inputWindowHandle = $null
$inputReleasedUtc = $null
$snapshots = [Collections.Generic.List[object]]::new()
$playingCpu = [Collections.Generic.List[object]]::new()
$playingEdges = [Collections.Generic.List[object]]::new()
$forwardWindow = $null
$forwardScanCode = $null
$forwardKeyHeld = $false
$forwardPhysicalKey = $null
$sceneButtonHeld = $false
$roamRequest = $null
$roamRelease = $null
$roamObserved = $null
$playingState = 'waiting'
$playingIdleSeconds = 10
$playingForwardSeconds = 20
$lastFrame = $null
$pipelineStableFrames = 0
$pipelineSignature = $null
$sampleStarted = $null
$moveRequested = $null
$releaseRequested = $null
$logReader = $null
$pendingLog = ''
$nextCpuRead = $null
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
    [DllImport("user32.dll")] static extern uint MapVirtualKeyW(uint code, uint mapType);
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int left,top,right,bottom; }
    [DllImport("user32.dll", SetLastError=true)] static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    static void RequireOwner(int pid, long hwnd) {
        uint owner; GetWindowThreadProcessId((IntPtr)hwnd, out owner);
        if (owner != (uint)pid) throw new InvalidOperationException("Input target no longer belongs to this run's PID.");
    }
    public static int[] SceneCenter(int pid, long hwnd) {
        RequireOwner(pid, hwnd);
        Rect rect;
        if (!GetClientRect((IntPtr)hwnd, out rect)) throw new Win32Exception(Marshal.GetLastWin32Error());
        int width=rect.right-rect.left, height=rect.bottom-rect.top;
        if (width <= 0 || height <= 0 || width > 65535 || height > 65535)
            throw new InvalidOperationException("No usable client rectangle for this run's scene click.");
        return new[] { width, height, width/2, height/2 };
    }
    public static void SceneButton(int pid, long hwnd, int x, int y, bool pressed) {
        RequireOwner(pid, hwnd);
        var point = (IntPtr)((y << 16) | x);
        // Directed client messages leave the real pointer and desktop alone.
        if (pressed && !PostMessageW((IntPtr)hwnd, 0x200u, IntPtr.Zero, point))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (!PostMessageW((IntPtr)hwnd, pressed ? 0x201u : 0x202u, pressed ? (IntPtr)1 : IntPtr.Zero, point))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public static int ForwardScanCode(int physicalKey) {
        // Godot's printable physical key values use ASCII. Unsupported special keys stay unknown.
        if (!((physicalKey >= 65 && physicalKey <= 90) || (physicalKey >= 48 && physicalKey <= 57)))
            throw new InvalidOperationException("The recorded physical binding is not a supported printable Windows key; no key was guessed.");
        var scan = MapVirtualKeyW((uint)physicalKey, 0);
        if (scan == 0 || scan > 255) throw new InvalidOperationException("No unambiguous Windows scan code for the recorded binding.");
        return (int)scan;
    }
    public static void Key(int pid, long hwnd, int physicalKey, int scan, bool pressed) {
        RequireOwner(pid, hwnd);
        var flags = 1 | (scan << 16);
        if (!pressed) flags = unchecked(flags | (int)0xc0000000);
        if (!PostMessageW((IntPtr)hwnd, pressed ? 0x100u : 0x101u, (IntPtr)physicalKey, (IntPtr)flags))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
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
function Read-PlayingCpu {
    $started = [DateTime]::UtcNow
    $readStarted = [Diagnostics.Stopwatch]::GetTimestamp()
    $gameProcess.Refresh()
    $seconds = $gameProcess.TotalProcessorTime.TotalSeconds
    $playingCpu.Add([ordered]@{ readUtc=[DateTime]::UtcNow.ToString('O'); processCpuSeconds=$seconds;
        startedUtc=$started.ToString('O'); observerMilliseconds=([Diagnostics.Stopwatch]::GetTimestamp()-$readStarted)*1000.0/[Diagnostics.Stopwatch]::Frequency })
}
function Add-PlayingEdge([string]$Name) {
    # Engine timestamp/frame are a real last-observed frame, never an interpolated input instant.
    $edge = [ordered]@{ name=$Name; milliseconds=$lastFrame.Milliseconds;
        observedUtc=[DateTime]::UtcNow.ToString('O'); observedTimestamp=[Diagnostics.Stopwatch]::GetTimestamp();
        suffix=" frame=$($lastFrame.Frame)"; source='last complete STARTUP_FRAME before this observer edge; see directed input request timestamps' }
    $anchors.Add($edge)
    $playingEdges.Add($edge)
    Read-CpuSnapshot $Name
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
    $gameArgs = @("--save-dir=$save", '--title=1')
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
    $directedInputAcceptanceSeconds = 15
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
            foreach ($frameMatch in [regex]::Matches($text, '(?m)^STARTUP_FRAME ([0-9.]+) frame=(\d+) ([^\r\n]+)')) {
                $fields = @{}
                foreach ($field in [regex]::Matches($frameMatch.Groups[3].Value, '(\w+)=([^ ]+)')) { $fields[$field.Groups[1].Value] = $field.Groups[2].Value }
                $number = [long]$frameMatch.Groups[2].Value
                $signature = "$($fields.pipelineSurface)|$($fields.pipelineDraw)|$($fields.pipelineCanvas)"
                $contiguous = $lastFrame -and $number -eq $lastFrame.Frame + 1
                $pipelineStableFrames = if ($contiguous -and $signature -eq $pipelineSignature -and $fields.flow -eq 'Playing' -and $fields.mode -eq 'Walk' -and $fields.interfaceMode -eq 'False') { $pipelineStableFrames + 1 } else { 0 }
                $pipelineSignature = $signature
                $lastFrame = [pscustomobject]@{ Milliseconds=[double]::Parse($frameMatch.Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture); Frame=$number; Fields=$fields }
                if ($null -ne $roamRequest -and $null -eq $roamObserved -and $number -gt $roamRequest.beforeEngineFrame -and
                    $fields.flow -eq 'Playing' -and $fields.mode -eq 'Walk' -and $fields.interfaceMode -eq 'False') {
                    $roamObserved = [ordered]@{ observedUtc=[DateTime]::UtcNow.ToString('O'); observedTimestamp=[Diagnostics.Stopwatch]::GetTimestamp();
                        engineFrame=$number; engineMilliseconds=$lastFrame.Milliseconds; interfaceMode=$false;
                        source='first complete later STARTUP_FRAME reporting real roam mode after directed scene click request' }
                }
            }
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
            $clock.Elapsed.TotalSeconds -gt $inputRequestedClockSeconds + $directedInputAcceptanceSeconds) {
            throw "Directed Enter did not reach new-game-enter within $($directedInputAcceptanceSeconds)s; hwnd=$inputWindowHandle titleFrame=$titleReadyFrame; no valid immediate/dwell result."
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
        if ($completed -and $playingSample) {
            if ($playingState -ne 'waiting' -and $lastFrame.Fields.flow -ne 'Playing') { throw 'Playing diagnostic left Playing before its endpoint; raw log retained.' }
            if ($playingState -ne 'waiting' -and $lastFrame.Fields.mode -ne 'Walk') { throw 'Playing diagnostic is no longer Walk; raw log retained.' }
            # Normal entry leaves the mouse visible. Click the actual client center once; never
            # infer an input lock from InterfaceMode, force focus, or write the mode itself.
            if ($playingState -eq 'waiting' -and $lastFrame -and $lastFrame.Fields.flow -eq 'Playing' -and $lastFrame.Fields.mode -eq 'Walk' -and
                $lastFrame.Fields.interfaceMode -eq 'True' -and $null -eq $roamRequest) {
                $point = [C215Launch]::SceneCenter($gameProcess.Id, $inputWindowHandle)
                $roamRequest = [ordered]@{ utc=[DateTime]::UtcNow.ToString('O'); timestamp=[Diagnostics.Stopwatch]::GetTimestamp();
                    beforeEngineFrame=$lastFrame.Frame; beforeEngineMilliseconds=$lastFrame.Milliseconds; interfaceMode=$true;
                    targetWindowHandle=$inputWindowHandle; clientWidth=$point[0]; clientHeight=$point[1]; clientX=$point[2]; clientY=$point[3];
                    method='PID-directed WM_MOUSEMOVE then WM_LBUTTONDOWN/UP at actual client center' }
                $sceneButtonHeld = $true
                [C215Launch]::SceneButton($gameProcess.Id, $inputWindowHandle, $roamRequest.clientX, $roamRequest.clientY, $true)
            }
            if ($sceneButtonHeld -and ([Diagnostics.Stopwatch]::GetTimestamp()-$roamRequest.timestamp) * 1000.0 /
                [Diagnostics.Stopwatch]::Frequency -ge $shortPressMilliseconds) {
                [C215Launch]::SceneButton($gameProcess.Id, $inputWindowHandle, $roamRequest.clientX, $roamRequest.clientY, $false)
                $sceneButtonHeld = $false
                $roamRelease = [ordered]@{ utc=[DateTime]::UtcNow.ToString('O'); timestamp=[Diagnostics.Stopwatch]::GetTimestamp();
                    beforeEngineFrame=$lastFrame.Frame; beforeEngineMilliseconds=$lastFrame.Milliseconds; cleanup=$false }
            }
            if ($null -ne $roamRequest -and $null -eq $roamObserved -and
                ([Diagnostics.Stopwatch]::GetTimestamp()-$roamRequest.timestamp)/[double][Diagnostics.Stopwatch]::Frequency -gt $directedInputAcceptanceSeconds) {
                throw "Playing sample did not start: directed scene click did not reach a later real Playing/Walk roam-mode frame within $($directedInputAcceptanceSeconds)s; request/release and raw telemetry retained."
            }
            if ($playingState -eq 'waiting' -and $lastFrame -and $lastFrame.Fields.flow -eq 'Playing' -and $lastFrame.Fields.mode -eq 'Walk' -and $pipelineStableFrames -ge 10 -and
                $null -ne $forwardPhysicalKey -and $lastFrame.Fields.forwardPressed -eq 'False' -and $lastFrame.Fields.interfaceMode -eq 'False' -and
                -not $sceneButtonHeld -and ($null -eq $roamRequest -or $null -ne $roamObserved)) {
                if ($null -eq $lastFrame.Fields.pipelineSurface -or $null -eq $lastFrame.Fields.feetX) { throw 'Missing continuous Playing telemetry; no synthetic sample.' }
                $forwardScanCode = [C215Launch]::ForwardScanCode($forwardPhysicalKey)
                $forwardWindow = $inputWindowHandle
                Add-PlayingEdge 'playing-idle-start'
                $sampleStarted = $lastFrame.Milliseconds
                $nextCpuRead = $clock.Elapsed.TotalSeconds
                $playingState = 'idle'
            }
            if ($playingState -eq 'idle' -and $lastFrame.Milliseconds - $sampleStarted -ge $playingIdleSeconds * 1000) {
                Add-PlayingEdge 'playing-idle-end'
                Add-PlayingEdge 'playing-forward-start'
                $moveRequested = [ordered]@{ utc=[DateTime]::UtcNow.ToString('O'); timestamp=[Diagnostics.Stopwatch]::GetTimestamp(); beforeEngineFrame=$lastFrame.Frame; beforeEngineMilliseconds=$lastFrame.Milliseconds }
                # Set ownership first: even an uncertain PostMessage result must get a key-up in finally.
                $forwardKeyHeld = $true
                [C215Launch]::Key($gameProcess.Id, $forwardWindow, $forwardPhysicalKey, $forwardScanCode, $true)
                $playingState = 'forward'
            }
            if ($playingState -eq 'forward' -and
                ([Diagnostics.Stopwatch]::GetTimestamp()-$moveRequested.timestamp)/[double][Diagnostics.Stopwatch]::Frequency -ge $playingForwardSeconds) {
                $releaseRequested = [ordered]@{ utc=[DateTime]::UtcNow.ToString('O'); timestamp=[Diagnostics.Stopwatch]::GetTimestamp(); beforeEngineFrame=$lastFrame.Frame; beforeEngineMilliseconds=$lastFrame.Milliseconds }
                [C215Launch]::Key($gameProcess.Id, $forwardWindow, $forwardPhysicalKey, $forwardScanCode, $false)
                $forwardKeyHeld = $false
                Add-PlayingEdge 'playing-forward-end'
                $playingState = 'released'
            }
            if ($null -ne $nextCpuRead -and $clock.Elapsed.TotalSeconds -ge $nextCpuRead) {
                Read-PlayingCpu
                $nextCpuRead = $clock.Elapsed.TotalSeconds + 1
            }
            # Observe the actual action release in a later engine frame; no additional wait/poll loop.
            if ($playingState -eq 'released' -and $lastFrame.Frame -gt $releaseRequested.beforeEngineFrame -and $lastFrame.Fields.forwardPressed -eq 'False') {
                Add-PlayingEdge 'playing-release-observed'
                $playingState = 'complete'
            }
            $completed = $playingState -eq 'complete'
        }
        if ($completed) { break }
        Start-Sleep -Milliseconds 20
    }
    if (-not $completed) {
        if ($playingSample -and $playingState -eq 'waiting') {
            if ($null -ne $roamRequest -and $null -eq $roamObserved) {
                throw 'Playing sample did not start: no later real Playing/Walk frame entered roam mode after the single directed scene click; raw telemetry retained.'
            }
            throw "Playing sample did not start before the existing startup deadline: flow=$($lastFrame.Fields.flow) mode=$($lastFrame.Fields.mode) interfaceMode=$($lastFrame.Fields.interfaceMode) stableRoamFrames=$pipelineStableFrames physicalKey=$forwardPhysicalKey; raw telemetry retained."
        }
        throw 'Startup phase deadline exceeded.'
    }
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
    } elseif ($playingSample) { 'first-Walk-plus-diagnostic-10s-idle-20s-forward-and-observed-action-release' }
    else { 'playing-realtime-and-Walk-input-ready' }
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
        playingSample=[ordered]@{ enabled=$playingSample; state=$playingState; edges=$playingEdges.ToArray();
            roamInput=[ordered]@{ request=$roamRequest; release=$roamRelease; observed=$roamObserved };
            targetIdleSeconds=if($playingSample){$playingIdleSeconds}else{$null}; targetForwardHoldSeconds=if($playingSample){$playingForwardSeconds}else{$null};
            physicalKey=$forwardPhysicalKey; windowsScanCode=$forwardScanCode; targetWindowHandle=$forwardWindow;
            request=$moveRequested; release=$releaseRequested; processCpuReads=$playingCpu.ToArray();
            movementStatus=if($playingSample){'SEE_RAW_PLAYING_POSES_AND_ACTION; WM delivery alone is not movement proof'}else{'NOT_REQUESTED'};
            diagnosticOverhead='EventPipe, every-frame engine logging, incremental 20ms log polling, phase thread snapshots and nominal 1s process counters are enabled. Read durations are retained. No no-telemetry control, causal overhead subtraction or experience qualification.' };
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
    $sceneReleaseFailure = $null
    if ($sceneButtonHeld -and $gameProcess -and -not $gameProcess.HasExited) {
        try {
            [C215Launch]::SceneButton($gameProcess.Id, $inputWindowHandle, $roamRequest.clientX, $roamRequest.clientY, $false)
            $sceneButtonHeld = $false
            $roamRelease = [ordered]@{ utc=[DateTime]::UtcNow.ToString('O'); timestamp=[Diagnostics.Stopwatch]::GetTimestamp();
                beforeEngineFrame=if($lastFrame){$lastFrame.Frame}else{$null}; beforeEngineMilliseconds=if($lastFrame){$lastFrame.Milliseconds}else{$null}; cleanup=$true }
        } catch {
            $sceneReleaseFailure = $_
            $_ | Out-String | Set-Content -LiteralPath (Join-Path $Output 'scene-button-release-failure.txt') -Encoding UTF8
        }
    }
    $forwardReleaseFailure = $null
    if ($forwardKeyHeld -and $gameProcess -and -not $gameProcess.HasExited) {
        try {
            [C215Launch]::Key($gameProcess.Id, $forwardWindow, $forwardPhysicalKey, $forwardScanCode, $false)
            $forwardKeyHeld = $false
        } catch {
            $forwardReleaseFailure = $_
            $_ | Out-String | Set-Content -LiteralPath (Join-Path $Output 'forward-release-failure.txt') -Encoding UTF8
        }
    }
    if ($logReader) { $logReader.Dispose() }
    # A failure after keydown still releases only this run's target window before cleanup.
    if ($null -ne $inputWindowHandle -and $null -eq $inputReleasedUtc -and
        $gameProcess -and -not $gameProcess.HasExited) {
        [C215Launch]::ReleaseEnter($inputWindowHandle)
        $inputReleasedUtc = [DateTime]::UtcNow.ToString('O')
    }
    $timing | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launcher-timing.json') -Encoding UTF8
    if ($diagnostic) { $snapshots.ToArray() | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Output 'cpu-snapshots.json') -Encoding UTF8 }
    if ($playingSample) {
        [ordered]@{ state=$playingState; edges=$playingEdges.ToArray(); request=$moveRequested; release=$releaseRequested;
            roamInput=[ordered]@{ request=$roamRequest; release=$roamRelease; observed=$roamObserved };
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
    if ($sceneReleaseFailure) { throw $sceneReleaseFailure }
    if ($forwardReleaseFailure) { throw $forwardReleaseFailure }
}


