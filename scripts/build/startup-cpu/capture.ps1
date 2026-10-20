# Only via isolated-launch.py from a run entry point, which owns the shared build lease.
# 用法：由 isolated-launch.py 在私有桌面传入 -Output、-Mode、-TitleDwellSeconds。
# 产出：-Output 指定目录（采样日志、跟踪、JSON 记录与隔离存档；父目录由调用方传入）。
param([Parameter(Mandatory)][string]$Output, [ValidateSet('title','immediate','dwell')][string]$Mode,
    [ValidateRange(1,120)][double]$TitleDwellSeconds = 10)
$ErrorActionPreference = 'Stop'
$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
. "$toolRoot\scripts\lib\common.ps1"
$trace = $null
$gameProcess = $null
$stopped = $false
$stopAttempted = $false
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
    $godot = Get-UESTCGodot
    $window = $godot -replace '_console\.exe$', '.exe'
    if (-not [IO.File]::Exists($window)) { $window = $godot }
    $log = Join-Path $Output 'godot.log'
    $save = Join-Path $Output 'save'
    $gameArgs = @("--save-dir=$save", '--title=1')
    # All three conditions use normal production defaults; no gate/shots/frames CaptureMode.
    $arguments = @('--path', "$toolRoot\game", '--log-file', $log, '--') + $gameArgs
    $line = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
    $env:UESTC_STARTUP_PROFILE = '1' # root-approved opt-in, only this private worker/game process tree
    $launched = [C215Launch]::Start($window, $line, $toolRoot)
    $gameProcess = [Diagnostics.Process]::GetProcessById($launched[0])
    $mainThreadId = $launched[1]
    # Attach immediately to this new PID; OS/process/log metadata is recorded afterwards.
    $traceInfo = [Diagnostics.ProcessStartInfo]::new()
    $traceInfo.FileName = "$toolRoot\output\tools\dotnet-diagnostics\dotnet-trace.exe"
    $traceInfo.Arguments = 'collect --process-id ' + $gameProcess.Id +
        ' --profile dotnet-sampled-thread-time --output ' +
        (ConvertTo-UESTCProcessArgument (Join-Path $Output 'threads.nettrace'))
    $traceInfo.UseShellExecute = $false; $traceInfo.CreateNoWindow = $false
    $traceInfo.RedirectStandardInput = $false # inherit private real console; never redirect stdin
    $traceInfo.RedirectStandardOutput = $true; $traceInfo.RedirectStandardError = $true
    $attachRequestedUtc = [DateTime]::UtcNow.ToString('O')
    $trace = [Diagnostics.Process]::Start($traceInfo)
    $traceStdout = $trace.StandardOutput.ReadToEndAsync()
    $traceStderr = $trace.StandardError.ReadToEndAsync()
    $processStart = $gameProcess.StartTime.ToUniversalTime()
    [ordered]@{gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId;
        engineStartUtc=$processStart.ToString('O'); collectorPid=$trace.Id; attachRequestedUtc=$attachRequestedUtc} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'launch.json') -Encoding UTF8
    Read-CpuSnapshot 'attach-request'
    $anchors = [Collections.Generic.List[object]]::new()
    $seen = [Collections.Generic.HashSet[string]]::new()
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $completed = $false
    $titleObservedAt = $null
    $titleReadyFrame = $null
    $latestProcessFrame = -1L
    $inputRequestedUtc = $null
    $inputRequestedClockSeconds = $null
    $inputWindowHandle = $null
    $inputDownProcessFrame = $null
    $inputReleasedUtc = $null
    $inputUpProcessFrame = $null
    while ($clock.Elapsed.TotalSeconds -lt 300) {
        if ($gameProcess.HasExited) { throw "Target exited before sampling boundary: $($gameProcess.ExitCode)" }
        if ($trace.HasExited) { throw "Collector exited before boundary: $($trace.ExitCode)" }
        $text = ''
        if ([IO.File]::Exists($log)) {
            $stream = [IO.FileStream]::new($log, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
            $reader = [IO.StreamReader]::new($stream)
            try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
        }
        $observed = [DateTime]::UtcNow.ToString('O')
        foreach ($m in [regex]::Matches($text, '(?m)^STARTUP_PHASE ([^ ]+) ([0-9.]+)([^\r\n]*)')) {
            if ($seen.Add($m.Groups[1].Value)) {
                $anchors.Add([ordered]@{name=$m.Groups[1].Value;
                    milliseconds=[double]::Parse($m.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture);
                    observedUtc=$observed; suffix=$m.Groups[3].Value})
                Read-CpuSnapshot $m.Groups[1].Value
                if ($m.Groups[1].Value -eq 'title-ready') {
                    $titleObservedAt = $clock.Elapsed.TotalSeconds
                    $titleReadyFrame = [long][regex]::Match($m.Groups[3].Value, 'frame=(\d+)').Groups[1].Value
                }
            }
        }
        foreach ($m in [regex]::Matches($text, '(?m)^STARTUP_FRAME [0-9.]+ frame=(\d+) ')) {
            $latestProcessFrame = [Math]::Max($latestProcessFrame, [long]$m.Groups[1].Value)
        }
        $delay = if ($Mode -eq 'dwell') { $TitleDwellSeconds } else { 0 }
        if ($Mode -ne 'title' -and $null -ne $titleObservedAt -and $null -eq $inputRequestedUtc -and
            $clock.Elapsed.TotalSeconds -ge $titleObservedAt + $delay -and $latestProcessFrame -gt $titleReadyFrame) {
            # title-ready is synchronous inside a frame, before Travel can perform work which
            # pumps and drops OS input. Wait for a real subsequent process frame, then hold Enter through
            # another process frame so down/up cross the normal Godot input-processing boundary.
            $inputRequestedUtc = [DateTime]::UtcNow.ToString('O')
            $inputRequestedClockSeconds = $clock.Elapsed.TotalSeconds
            $inputDownProcessFrame = $latestProcessFrame
            $inputWindowHandle = [C215Launch]::NewGameEnter($gameProcess.Id)
        }
        if ($null -ne $inputWindowHandle -and $null -eq $inputReleasedUtc -and $latestProcessFrame -gt $inputDownProcessFrame) {
            [C215Launch]::ReleaseEnter($inputWindowHandle)
            $inputReleasedUtc = [DateTime]::UtcNow.ToString('O')
            $inputUpProcessFrame = $latestProcessFrame
        }
        if ($null -ne $inputRequestedClockSeconds -and -not $seen.Contains('new-game-enter') -and
            $clock.Elapsed.TotalSeconds -gt $inputRequestedClockSeconds + 15) {
            throw "Directed Enter did not reach new-game-enter within 15s; hwnd=$inputWindowHandle titleFrame=$titleReadyFrame downAfterProcess=$inputDownProcessFrame upAfterProcess=$inputUpProcessFrame; no valid immediate/dwell result."
        }
        $completed = $seen.Contains('realtime') -and
            ($Mode -eq 'title' -or ($text -match '(?m)^STARTUP_PHASE input-ready [^\r\n]*mode=Walk'))
        if ($completed) { break }
        Start-Sleep -Milliseconds 20
    }
    if (-not $completed) { throw 'Startup phase deadline exceeded.' }
    Stop-OwnTrace
    if ($gameProcess.HasExited) { throw 'Target exited before collector Stop/rundown completed.' }
    [ordered]@{ gamePid=$gameProcess.Id; mainOsThreadId=$mainThreadId; engineStartUtc=$processStart.ToString('O');
        mainThreadIdentification='CreateProcessW primary OS thread ID; compare only OS IDs, never CLR ManagedThreadID';
        mode=$Mode; commandLine=$line; collector='dotnet-trace 10 EventPipe dotnet-sampled-thread-time (all managed thread stacks; NOT on-CPU)';
        attachRequestedUtc=$attachRequestedUtc; collectorPid=$trace.Id; collectorStopRequestedUtc=$stopRequestedUtc;
        collectorStoppedWhileTargetAlive=$true; collectorFinishedUtc=[DateTime]::UtcNow.ToString('O');
        phaseObservations=$anchors.ToArray(); cpuSnapshots=$snapshots.ToArray(); phasePollingMilliseconds=20;
        baseline=(git -C $toolRoot rev-parse HEAD); captureMode=$false; cpuFunctionShare='NOT_MEASURED';
        frameTelemetryEnvironment='UESTC_STARTUP_PROFILE=1';
        input=[ordered]@{method='PID-directed private-desktop WM_KEYDOWN/UP Enter; normal TitleMenu NewGame focus';
            requestedUtc=$inputRequestedUtc; targetWindowHandle=$inputWindowHandle; titleReadyFrame=$titleReadyFrame;
            downAfterProcessFrame=$inputDownProcessFrame; releasedUtc=$inputReleasedUtc; upAfterProcessFrame=$inputUpProcessFrame;
            requestedDwellSeconds=if($Mode -eq 'title'){$null}else{$delay};
            observedTitleToRequestSeconds=if($null -eq $inputRequestedClockSeconds){$null}else{$inputRequestedClockSeconds-$titleObservedAt}};
        caveat='Normal Title, isolated empty save/log only; no CaptureMode. Menu-ready, world-ready and first Walk are separate boundaries. Immediate means first log-poll-directed Enter after an actual post-title process frame, released after another process frame; not zero-delay human input. Per-frame renderer telemetry, when enabled, adds diagnostic overhead; renderer CPU is viewport render time, not OS thread CPU.'
    } | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $Output 'result.json') -Encoding UTF8
} catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $Output 'failure.txt') -Encoding UTF8
    throw
} finally {
    $snapshots.ToArray() | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Output 'cpu-snapshots.json') -Encoding UTF8
    if ($trace -and -not $stopped -and -not $stopAttempted) { Stop-OwnTrace }
    # A failed collector Stop leaves this exact collector/target alive, identified in launch.json.
    if ($gameProcess -and ($stopped -or -not $trace) -and -not $gameProcess.HasExited) {
        $null = $gameProcess.CloseMainWindow()
        if (-not $gameProcess.WaitForExit(15000)) { $gameProcess.Kill(); $gameProcess.WaitForExit() }
    }
    if ($trace -and $trace.HasExited) { $trace.Dispose() }
}


