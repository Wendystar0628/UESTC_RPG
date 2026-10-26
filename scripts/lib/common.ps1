# 公共函数库：仓库与锁目录布局、路径越界检查、构建锁、子进程与 git 调用、Godot 路径查找；兼容 Windows PowerShell 5.1。
# 同时持有 EventPipe 收集器的附着、Stop/rundown、排空与私有控制台生命周期。
# 用法：由其他脚本点引用（. scripts\lib\common.ps1），不单独运行
# 产出：output/.locks/build.lock（主检出构建锁）；调用方 -Log 指定的日志文件（父目录由调用方传入）。
Set-StrictMode -Version Latest

function Assert-UESTCCleanGodotLog {
    param($ProcessResult)
    $pattern = '(?im)(?:SCRIPT )?ERROR:|\[Error\][ \t]|Object was deleted while awaiting a callback|shader[^\r\n]*(?:compilation failed|compile error)|\bleaked\b|\bRIDs?\b[^\r\n]*\bleak\w*|\bleak\w*[^\r\n]*\bRIDs?\b|\bDetected\s+<?[1-9][0-9]*>?\s+orphan\s+nodes?\b'
    if ($ProcessResult.Output -match $pattern) { throw "Godot reported errors, orphan objects or resource leaks. See $($ProcessResult.Log)" }
}

# Each test/import/capture owns a fresh native user directory under its existing output directory.
function New-UESTCTestEnvironment {
    param([Parameter(Mandatory)][string]$Work)
    $profile = Assert-UESTCChildPath (Join-Path $Work ('profile-' + [Guid]::NewGuid().ToString('N'))) $Work
    $environment = @{ APPDATA=(Join-Path $profile 'roaming'); LOCALAPPDATA=(Join-Path $profile 'local') }
    foreach ($path in $environment.Values) { $null = [IO.Directory]::CreateDirectory($path) }
    return $environment
}

function Write-UESTCTestSettings {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Godot, [string]$Parameters='--headless',
        [Collections.IDictionary]$Environment=@{})
    $variables = @{ GODOT_BIN=$Godot; UESTC_REPOSITORY=$Root }
    foreach ($key in $Environment.Keys) { $variables[$key] = [string]$Environment[$key] }
    $nodes = foreach ($key in @($variables.Keys | Sort-Object)) {
        $name = [Xml.XmlConvert]::VerifyName([string]$key)
        '<' + $name + '>' + [Security.SecurityElement]::Escape($variables[$key]) + '</' + $name + '>'
    }
    $arguments = [Security.SecurityElement]::Escape($Parameters)
    $xml = '<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount><TreatNoTestsAsError>true</TreatNoTestsAsError><EnvironmentVariables>' +
        ($nodes -join '') + '</EnvironmentVariables></RunConfiguration><GdUnit4><Parameters>' + $arguments +
        '</Parameters><DisplayName>FullyQualifiedName</DisplayName><CaptureStdOut>true</CaptureStdOut></GdUnit4></RunSettings>'
    [IO.File]::WriteAllText($Path, $xml, (New-Object Text.UTF8Encoding $false))
    return $Path
}

# A successful adapter process is insufficient: warnings may be Passed in TRX.
function Assert-UESTCTestResult {
    param([Parameter(Mandatory)]$ProcessResult, [Parameter(Mandatory)][string]$TrxPath)
    $trx = $null
    $executed = 0
    try {
        if ([IO.File]::Exists($TrxPath)) {
            [xml]$trx = [IO.File]::ReadAllText($TrxPath)
            $counters = $trx.SelectSingleNode('//*[local-name()="ResultSummary"]/*[local-name()="Counters"]')
            if ($counters) { $executed = [int]$counters.GetAttribute('executed') }
        }
    } catch {
        $failure = [InvalidOperationException]::new("Test result is incomplete: invalid TRX $TrxPath", $_.Exception)
        $failure.Data['Incomplete'] = $true
        throw $failure
    }
    if ($executed -le 0) {
        $failure = [InvalidOperationException]::new("Test result is incomplete: zero executed tests or missing TRX. See $TrxPath")
        $failure.Data['Incomplete'] = $true
        throw $failure
    }
    if ($ProcessResult.ExitCode -ne 0) { throw "Test process exited $($ProcessResult.ExitCode). See $($ProcessResult.Log)" }
    $summary = $trx.SelectSingleNode('//*[local-name()="ResultSummary"]')
    if ($summary.GetAttribute('outcome') -notin @('Passed','Completed')) { throw "TRX summary did not pass. See $TrxPath" }
    foreach ($name in @('failed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','passedButRunAborted')) {
        $value = $counters.GetAttribute($name)
        if ($value -and [int]$value -gt 0) { throw "TRX reports $name=$value. See $TrxPath" }
    }
    $testResults = $trx.SelectNodes('//*[local-name()="UnitTestResult"]')
    if ($testResults.Count -eq 0) {
        $failure = [InvalidOperationException]::new("Test result is incomplete: no per-test results. See $TrxPath")
        $failure.Data['Incomplete'] = $true
        throw $failure
    }
    foreach ($item in $testResults) {
        if ($item.GetAttribute('outcome') -ne 'Passed') {
            throw "TRX test '$($item.GetAttribute('testName'))' did not pass. See $TrxPath"
        }
    }
    foreach ($info in $trx.SelectNodes('//*[local-name()="RunInfo"]')) {
        if ($info.GetAttribute('outcome') -in @('Failed','Error','Aborted','Timeout')) {
            throw "TRX run diagnostic reports failure: $($info.InnerText). See $TrxPath"
        }
    }
    $messages = $trx.SelectNodes('//*[local-name()="ResultSummary"]/*[local-name()="Output"]')
    $runOutput = @($messages | ForEach-Object { $_.InnerText }) -join "`n"
    if ($runOutput -match '(?m)^\[GdUnit4\]') {
        # The adapter also logs editor rebuild/discovery. Only its fixed runner
        # events delimit execution; StringName counters are not node leak reports.
        $execution = New-Object 'Collections.Generic.List[string]'
        $running = $false
        $runners = 0
        foreach ($line in ($runOutput -split '\r?\n')) {
            if ($line -match '^\[GdUnit4\] \[[^\]\r\n]+\] Starting (GodotRuntimeTestRunner|DefaultTestRunner)\s*$') {
                $running = $true
                $runners++
            }
            if ($running) { $execution.Add($line) }
            if ($line -match '^\[GdUnit4\] \[[^\]\r\n]+\] (?:GodotRuntimeTestRunner ends with exit code:|Completed tests for assembly:)') {
                $running = $false
            }
        }
        if ($runners -eq 0 -or $running) {
            $failure = [InvalidOperationException]::new("Test execution log is incomplete: missing gdUnit runner boundary. See $TrxPath")
            $failure.Data['Incomplete'] = $true
            throw $failure
        }
        $runOutput = $execution -join "`n"
    } else {
        Assert-UESTCCleanGodotLog $ProcessResult
    }
    Assert-UESTCCleanGodotLog ([pscustomobject]@{ Output=$runOutput; Log=$TrxPath })
    # Per-case output contains gdUnit's Detected N orphan nodes diagnostics,
    # including warnings whose adapter outcome is still Passed.
    $caseMessages = $trx.SelectNodes('//*[local-name()="UnitTestResult"]/*[local-name()="Output"]')
    Assert-UESTCCleanGodotLog ([pscustomobject]@{ Output=(@($caseMessages | ForEach-Object { $_.InnerText }) -join "`n"); Log=$TrxPath })
    return $executed
}

function Get-UESTCLayout {
    param([Parameter(Mandatory)][string]$Repository)
    $root = [IO.Path]::GetFullPath($Repository).TrimEnd('\', '/')
    # Finish the native process before selecting the first line. No PowerShell
    # native-error stream is involved, even when a caller redirects all streams.
    $commonLines = @(Get-UESTCGitLines $root @('rev-parse', '--git-common-dir'))
    if ($commonLines.Count -eq 0) { throw "Not a git worktree: $root (git returned no common directory)" }
    $common = $commonLines[0]
    if (-not [IO.Path]::IsPathRooted($common)) { $common = Join-Path $root $common }
    $primary = Split-Path ([IO.Path]::GetFullPath($common)) -Parent
    [pscustomobject]@{ Root = $root; Primary = $primary; Locks = Join-Path $primary 'output/.locks' }
}

function Assert-UESTCChildPath {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Parent)
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $base = [IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
    if (-not $full.StartsWith($base + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes intended parent: $full (parent $base)"
    }
    # A junction in an existing ancestor can redirect a superficially safe path.
    $cursor = $full
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse point rejected: $cursor" }
        }
        $next = Split-Path $cursor -Parent
        if ($next -eq $cursor) { break }
        $cursor = $next
    }
    return $full
}

# Only real contention pays for the native handle-owner query and process table.
function Write-UESTCBuildLockWait {
    param([Parameter(Mandatory)][string]$LockPath)
    Write-Host -NoNewline '正在等待构建锁；占锁任务：'
    try {
        if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTree.cs') }
        $users = @([Uestc.Tools.ProcessTree]::GetFileUsers($LockPath))
        if ($users.Count -eq 0) { Write-Host '未知（系统未返回持锁进程）'; return }
        $tasks = foreach ($user in $users) {
            $rows = @(Get-CimInstance Win32_Process -Filter "ProcessId = $($user.ProcessId)" -OperationTimeoutSec 2 -ErrorAction Stop)
            if ($rows.Count -eq 1 -and $rows[0].CreationDate -and $rows[0].CommandLine) {
                # CIM timestamps have microsecond precision; RM FILETIME has 100 ns precision.
                $difference = [Math]::Abs($rows[0].CreationDate.ToUniversalTime().ToFileTimeUtc() - $user.StartTimeFileTime)
                if ($difference -lt 10) {
                    "PID $($user.ProcessId) $($rows[0].Name)：$($rows[0].CommandLine -replace '[\r\n]+', ' ')"
                    continue
                }
            }
            "PID $($user.ProcessId)：未知（进程已退出、身份变化或命令不可读）"
        }
        Write-Host ($tasks -join '；')
    } catch {
        Write-Host ("未知（查询失败：$($_.Exception.Message -replace '[\r\n]+', ' ')）")
    }
}

# One OS handle serializes builds, imports, gates, baking and screenshots.
function Enter-UESTCBuildLock {
    param([Parameter(Mandatory)][string]$LockRoot, [int]$TimeoutSeconds = 1800)
    $null = [IO.Directory]::CreateDirectory($LockRoot)
    $lockPath = Join-Path $LockRoot 'build.lock'
    $waitShown = $false
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        do {
            try {
                return [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate,
                    [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            } catch [IO.IOException] {
                if (($_.Exception.HResult -band 0xffff) -notin @(32,33)) { throw }
                if (-not $waitShown) {
                    $waitShown = $true
                    Write-UESTCBuildLockWait -LockPath $lockPath
                }
            }
            Start-Sleep -Milliseconds 100
        } while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds)
        throw "Timed out waiting for build lock in $LockRoot"
    } finally { $timer.Stop() }
}

function Exit-UESTCBuildLock {
    param([IO.FileStream]$Lease)
    if ($Lease) { $Lease.Dispose() }
}

function ConvertTo-UESTCProcessArgument {
    param([AllowEmptyString()][string]$Value)
    # Windows CommandLineToArgvW quoting; arguments never pass through a shell.
    if ($Value -notmatch '[\s"]' -and $Value.Length -gt 0) { return $Value }
    return '"' + ([regex]::Replace([regex]::Replace($Value, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1')) + '"'
}

function Invoke-UESTCProcess {
    param([Parameter(Mandatory)][string]$Executable, [string[]]$Arguments = @(),
        [Parameter(Mandatory)][string]$WorkingDirectory, [string]$Log = '',
        [int]$TimeoutSeconds = 1800, [switch]$AllowFailure, [switch]$Visible,
        [int[]]$SuccessExitCodes = @(0), [Text.Encoding]$TextEncoding = $null,
        [scriptblock]$ObserveOutput = $null, [switch]$IsolatedDesktop,
        [Collections.IDictionary]$Environment = $null)
    if ($Log) { $null = [IO.Directory]::CreateDirectory((Split-Path $Log -Parent)) }
    if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTree.cs') }
    $path = @(Get-Command $Executable -CommandType Application -ErrorAction Stop)[0].Source
    $commandLine = (($Arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    if (-not $TextEncoding) { $TextEncoding = [Console]::OutputEncoding }
    $process = $null
    try {
        $process = New-Object Uestc.Tools.ProcessTree($path, $commandLine, $WorkingDirectory, [bool]$Visible, $TextEncoding, [bool]$IsolatedDesktop, $Environment)
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (-not $process.HasExited) {
            if ([DateTime]::UtcNow -ge $deadline) { throw "Process timeout: $Executable" }
            if ($ObserveOutput) { $null = & $ObserveOutput $process.StdOut $process }
            Start-Sleep -Milliseconds 100
        }
        $process.Finish()
        $stdoutText = $process.StdOut
        $stderrText = $process.StdErr
        $output = $stdoutText + $stderrText
        $result = [pscustomobject]@{ ExitCode = $process.ExitCode; ProcessExitUtc = $process.ProcessExitUtc;
            StdOut = $stdoutText; StdErr = $stderrText; Output = $output; Log = $Log }
        if (-not $AllowFailure -and $result.ExitCode -notin $SuccessExitCodes) {
            $location = if ($Log) { " See $Log." } else { '' }
            $diagnostic = if ($stderrText.Trim()) { $stderrText.Trim() } else { $stdoutText.Trim() }
            $failure = New-Object InvalidOperationException("Exit $($result.ExitCode): $Executable.$location`n$diagnostic")
            $failure.Data['ExitCode'] = $result.ExitCode
            $failure.Data['StdOut'] = $stdoutText
            $failure.Data['StdErr'] = $stderrText
            throw $failure
        }
        return $result
    } finally {
        if ($process) {
            try { $process.Finish() }
            finally {
                if ($Log) { [IO.File]::WriteAllText($Log, ($process.StdOut + $process.StdErr), (New-Object Text.UTF8Encoding $false)) }
                $process.Dispose()
            }
        }
    }
}

# Own one real-PID EventPipe collector from attachment through Stop/rundown and output drain.
# The caller owns Target and calls Stop at its observed game boundary, then Dispose in finally.
function Invoke-UESTCEventPipeCapture {
    param([Parameter(Mandatory)][ValidateSet('Start','Stop','Dispose')][string]$Action,
        [Parameter(Mandatory)][Collections.IDictionary]$Capture)
    $record = $Capture.Record
    if ($Action -eq 'Start') {
        $Capture['Collector'] = $null
        $Capture['OwnConsole'] = $false
        $Capture['StopAttempted'] = $false
        $Capture['Stopped'] = $false
        foreach ($key in @('toolPid','attachRequestedUtc','collectorReadyObservedUtc','collectorStopRequestedUtc',
                          'toolExitUtc','collectorFinishedUtc','collectorStoppedWhileTargetAlive','exitCode')) { $record[$key] = $null }
        if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTree.cs') }
        if (-not ('Uestc.Tools.EventPipeConsole' -as [type])) {
            Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace Uestc.Tools {
    public static class EventPipeConsole {
        [StructLayout(LayoutKind.Explicit, Size=20, CharSet=CharSet.Unicode)] struct Input {
            [FieldOffset(0)] public ushort type;
            [FieldOffset(4)] public int down;
            [FieldOffset(8)] public ushort repeat;
            [FieldOffset(10)] public ushort key;
            [FieldOffset(14)] public char character;
        }
        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr GetStdHandle(int name);
        [DllImport("kernel32.dll", SetLastError=true)] static extern bool SetStdHandle(int name, IntPtr handle);
        [DllImport("kernel32.dll", SetLastError=true)] static extern uint GetFileType(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError=true)] static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError=true)] static extern bool FreeConsole();
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool WriteConsoleInputW(SafeFileHandle handle, Input[] records, uint count, out uint written);
        public static bool Prepare() {
            // A window handle does not establish console attachment. Detach only this
            // disposable worker; preserve redirected output when AllocConsole resets std handles.
            var output = GetStdHandle(-11); var error = GetStdHandle(-12);
            bool outputRedirected = GetFileType(output) == 3 || GetFileType(output) == 1;
            bool errorRedirected = GetFileType(error) == 3 || GetFileType(error) == 1;
            if (!FreeConsole()) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!AllocConsole()) throw new Win32Exception(Marshal.GetLastWin32Error());
            try {
                if (outputRedirected && !SetStdHandle(-11, output)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (errorRedirected && !SetStdHandle(-12, error)) throw new Win32Exception(Marshal.GetLastWin32Error());
                ShowWindow(GetConsoleWindow(), 0);
                return true;
            } catch (Exception failure) {
                if (!FreeConsole()) failure.Data["EventPipeConsole.FreeConsole"] = new Win32Exception(Marshal.GetLastWin32Error());
                throw;
            }
        }
        public static void RequestStop() {
            // Open the worker's real private console, even when its standard input is NUL.
            using (var input = CreateFile("CONIN$", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero)) {
                if (input.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var down = new Input { type=1, down=1, repeat=1, key=13, character='\r' };
                var up = down; up.down=0; uint written;
                if (!WriteConsoleInputW(input, new[] {down,up}, 2, out written) || written != 2)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        public static void Release() {
            if (!FreeConsole()) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
"@
        }
        $Capture.OwnConsole = [Uestc.Tools.EventPipeConsole]::Prepare()
        if ($Capture.Target.HasExited) { throw 'EventPipe target exited before attachment.' }
        $arguments = @('collect','--process-id',[string]$Capture.Target.ProcessId,'--profile','dotnet-sampled-thread-time',
            '--providers','Microsoft-Windows-DotNETRuntime:0x41080001:5','--output',(Join-Path $Capture.Output 'threads.nettrace'))
        $record['arguments'] = $arguments
        $record.attachRequestedUtc = [DateTime]::UtcNow.ToString('O')
        $line = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
        $Capture.Collector = New-Object Uestc.Tools.ProcessTree($Capture.ToolPath,$line,$Capture.Root,$true,[Text.Encoding]::UTF8,$false,$null,$true)
        $record.toolPid = $Capture.Collector.ProcessId
        while ($Capture.Collector.StdOut -notmatch '(?m)^Output File\s*:\s*[^\r\n]+\r?\n') {
            if ($Capture.Target.HasExited) { throw 'EventPipe target exited before successful attachment.' }
            if ($Capture.Collector.HasExited) { throw "EventPipe collector exited before successful attachment: $($Capture.Collector.ExitCode)" }
            if ([DateTime]::UtcNow -ge $Capture.Deadline) { throw 'EventPipe attachment did not complete within the capture lifetime.' }
            Start-Sleep -Milliseconds 100
        }
        # Output File is published only after StartEventPipeSession succeeds.
        $record.collectorReadyObservedUtc = [DateTime]::UtcNow.ToString('O')
        return
    }
    if ($Action -eq 'Stop') {
        if ($Capture.Stopped) { return }
        $Capture.StopAttempted = $true
        if ($Capture.Target.HasExited) { throw 'EventPipe target exited before collector Stop/rundown.' }
        if ($Capture.Collector.HasExited) { throw "Collector exited before requested Stop: $($Capture.Collector.ExitCode)" }
        $record.collectorStopRequestedUtc = [DateTime]::UtcNow.ToString('O')
        [Uestc.Tools.EventPipeConsole]::RequestStop()
        $stopClock = [Diagnostics.Stopwatch]::StartNew()
        while (-not $Capture.Collector.HasExited) {
            if ($Capture.Target.HasExited) { throw 'EventPipe target exited during collector Stop/rundown.' }
            if ($stopClock.Elapsed.TotalSeconds -ge 120) { throw 'Collector did not finish Stop/rundown; its owned job will be reclaimed as a failed capture.' }
            Start-Sleep -Milliseconds 100
        }
        $record.exitCode = $Capture.Collector.ExitCode
        $record.toolExitUtc = $Capture.Collector.ProcessExitUtc.ToString('O')
        $Capture.Collector.Finish()
        $record.collectorFinishedUtc = [DateTime]::UtcNow.ToString('O')
        $record.collectorStoppedWhileTargetAlive = -not $Capture.Target.HasExited
        if (-not $record.collectorStoppedWhileTargetAlive) { throw 'Target exited before collector Stop/rundown completed.' }
        if ($record.exitCode -ne 0) { throw "Collector failed: $($record.exitCode)" }
        if (-not [IO.File]::Exists((Join-Path $Capture.Output 'threads.nettrace'))) { throw 'Missing nettrace output.' }
        $Capture.Stopped = $true
        return
    }
    $failure = $null
    $cleanup = [ordered]@{
        Stop = { if ($Capture.Collector -and -not $Capture.Stopped -and -not $Capture.StopAttempted) {
            Invoke-UESTCEventPipeCapture -Action Stop -Capture $Capture
        } }
        Exit = { if ($Capture.Collector -and $Capture.Collector.HasExited) {
            $record.exitCode = $Capture.Collector.ExitCode
            $record.toolExitUtc = $Capture.Collector.ProcessExitUtc.ToString('O')
        } }
        Drain = { if ($Capture.Collector) { $Capture.Collector.Finish() } }
        StdOut = { if ($Capture.Collector) { [IO.File]::WriteAllText((Join-Path $Capture.Output 'trace.stdout.txt'),$Capture.Collector.StdOut) } }
        StdErr = { if ($Capture.Collector) { [IO.File]::WriteAllText((Join-Path $Capture.Output 'trace.stderr.txt'),$Capture.Collector.StdErr) } }
        ExitRecord = { if ($null -ne $record.exitCode) { [IO.File]::WriteAllText((Join-Path $Capture.Output 'trace.exit.txt'),[string]$record.exitCode) } }
        Collector = { if ($Capture.Collector) { $Capture.Collector.Dispose() } }
        Console = { if ($Capture.OwnConsole) { [Uestc.Tools.EventPipeConsole]::Release(); $Capture.OwnConsole = $false } }
    }
    foreach ($step in $cleanup.GetEnumerator()) {
        try { & $step.Value }
        catch {
            if (-not $record.Contains('cleanupErrors')) { $record['cleanupErrors'] = @() }
            $record.cleanupErrors += @([ordered]@{ step=$step.Key; error=$_.Exception.ToString() })
            if ($failure) { $failure.Exception.Data["EventPipe.$($step.Key)"] = $_.Exception }
            else { $failure = $_ }
        }
    }
    if ($failure) { throw $failure }
}

function Invoke-UESTCGit {
    param([Parameter(Mandatory)][string]$Repository, [string[]]$Arguments = @(),
        [int[]]$SuccessExitCodes = @(0), [string]$Log = '')
    $root = [IO.Path]::GetFullPath($Repository)
    $result = Invoke-UESTCProcess git (@('-C', $root, '-c', 'core.quotepath=false') + $Arguments) $root $Log `
        -SuccessExitCodes $SuccessExitCodes -TextEncoding ([Text.Encoding]::UTF8)
    if ($result.StdErr.Trim()) {
        # Git often sends ordinary progress and CRLF notices to stderr with exit 0.
        # Preserve that diagnostic as a warning, never as a native ErrorRecord.
        Write-Warning -Message ("git diagnostic (exit $($result.ExitCode)): " + $result.StdErr.Trim()) -WarningAction Continue
    }
    return $result
}

function Get-UESTCGitLines {
    param([Parameter(Mandatory)][string]$Repository, [string[]]$Arguments = @())
    $result = Invoke-UESTCGit $Repository $Arguments
    $result.StdOut -split '\r?\n' | Where-Object { $_.Length -gt 0 }
}

# Path-list commands use Git's -z output, without quoting or line splitting.
function Get-UESTCGitPaths {
    param([Parameter(Mandatory)][string]$Repository, [string[]]$Arguments = @())
    $result = Invoke-UESTCGit $Repository $Arguments
    $result.StdOut -split "`0" | Where-Object { $_.Length -gt 0 }
}

function Get-UESTCGodot {
    if ($env:UESTC_GODOT) { return [IO.Path]::GetFullPath($env:UESTC_GODOT) }
    return 'E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
}


# The console launcher is retained for build/import; production processes own the real host PID.
function Get-UESTCGodotHost {
    param([string]$Godot = (Get-UESTCGodot))
    $hostPath = [IO.Path]::GetFullPath(($Godot -replace '_console\.exe$', '.exe'))
    if (-not [IO.File]::Exists($hostPath)) { throw "Actual Godot host executable is missing: $hostPath" }
    return $hostPath
}

function Get-UESTCDotnetTrace {
    param([Parameter(Mandatory)][string]$Root)
    $relative = 'output/tools/dotnet-diagnostics/dotnet-trace.exe'
    foreach ($repository in @($Root, (Get-UESTCLayout $Root).Primary) | Select-Object -Unique) {
        $path = [IO.Path]::GetFullPath((Join-Path $repository $relative))
        if ([IO.File]::Exists($path)) { return $path }
    }
    throw 'dotnet-trace is not installed in the configured local tool store.'
}

function Build-PerfCpuParse($Root, $Directory, $TraceTool) {
    $library=@(Get-ChildItem -LiteralPath (Join-Path (Split-Path $TraceTool -Parent) '.store\dotnet-trace') -Recurse -File -Filter 'Microsoft.Diagnostics.Tracing.TraceEvent.dll')
    if ($library.Count -ne 1) { throw 'The pinned dotnet-trace store must identify exactly one TraceEvent assembly.' }
    $project=Join-Path $Root 'scripts\perf\cpu\CpuParse.csproj'
    $property='-p:TraceToolDir=' + $library[0].DirectoryName
    $null=Invoke-UESTCProcess dotnet @('build',$project,$property,'--nologo','-v:q') $Root (Join-Path $Directory 'cpu-parser-build.log')
    $target=(Invoke-UESTCProcess dotnet @('msbuild',$project,$property,'-nologo','-getProperty:TargetPath') $Root).StdOut.Trim()
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw 'CpuParse target path is missing.' }
    return $target
}
