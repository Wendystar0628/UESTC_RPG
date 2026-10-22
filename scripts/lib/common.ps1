# 公共函数库：仓库与锁目录布局、路径越界检查、构建锁、子进程与 git 调用、Godot 路径查找；兼容 Windows PowerShell 5.1。
# 用法：由其他脚本点引用（. scripts\lib\common.ps1），不单独运行
# 产出：output/.locks/build.lock（主检出构建锁）；调用方 -Log 指定的日志文件（父目录由调用方传入）。
Set-StrictMode -Version Latest

function Assert-UESTCCleanGodotLog {
    param($ProcessResult)
    if ($ProcessResult.Output -match '(?im)(?:SCRIPT )?ERROR:|shader[^\r\n]*(?:compilation failed|compile error)') { throw "Godot reported errors: $($ProcessResult.Log)" }
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
        [scriptblock]$ObserveOutput = $null, [switch]$IsolatedDesktop)
    if ($Log) { $null = [IO.Directory]::CreateDirectory((Split-Path $Log -Parent)) }
    if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTree.cs') }
    $path = @(Get-Command $Executable -CommandType Application -ErrorAction Stop)[0].Source
    $commandLine = (($Arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    if (-not $TextEncoding) { $TextEncoding = [Console]::OutputEncoding }
    $process = $null
    try {
        $process = New-Object Uestc.Tools.ProcessTree($path, $commandLine, $WorkingDirectory, [bool]$Visible, $TextEncoding, [bool]$IsolatedDesktop)
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (-not $process.HasExited) {
            if ([DateTime]::UtcNow -ge $deadline) { throw "Process timeout: $Executable" }
            if ($ObserveOutput) { $null = & $ObserveOutput $process.StdOut }
            Start-Sleep -Milliseconds 100
        }
        $process.Finish()
        $stdoutText = $process.StdOut
        $stderrText = $process.StdErr
        $output = $stdoutText + $stderrText
        $result = [pscustomobject]@{ ExitCode = $process.ExitCode; StdOut = $stdoutText; StdErr = $stderrText; Output = $output; Log = $Log }
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
