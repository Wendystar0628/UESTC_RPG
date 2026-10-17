# Shared path, process and handle-lock helpers. Compatible with Windows PowerShell 5.1.
Set-StrictMode -Version Latest

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

# One OS handle serializes builds, imports, gates, baking and screenshots.
function Enter-UESTCBuildLock {
    param([Parameter(Mandatory)][string]$LockRoot, [int]$TimeoutSeconds = 1800)
    $null = [IO.Directory]::CreateDirectory($LockRoot)
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        do {
            try {
                return [IO.File]::Open((Join-Path $LockRoot 'build.lock'), [IO.FileMode]::OpenOrCreate,
                    [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            } catch [IO.IOException] {
                if (($_.Exception.HResult -band 0xffff) -notin @(32,33)) { throw }
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
        [scriptblock]$ObserveOutput = $null)
    if ($Log) { $null = [IO.Directory]::CreateDirectory((Split-Path $Log -Parent)) }
    if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTree.cs') }
    $path = @(Get-Command $Executable -CommandType Application -ErrorAction Stop)[0].Source
    $commandLine = (($Arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    if (-not $TextEncoding) { $TextEncoding = [Console]::OutputEncoding }
    $process = $null
    try {
        $process = New-Object Uestc.Tools.ProcessTree($path, $commandLine, $WorkingDirectory, [bool]$Visible, $TextEncoding)
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

function Get-UESTCGodot {
    if ($env:UESTC_GODOT) { return [IO.Path]::GetFullPath($env:UESTC_GODOT) }
    return 'E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
}
