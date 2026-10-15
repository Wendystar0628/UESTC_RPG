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
    $worktrees = Join-Path (Split-Path $primary -Parent) ((Split-Path $primary -Leaf) + '.wt')
    [pscustomobject]@{ Root = $root; Primary = $primary; Worktrees = $worktrees; Locks = Join-Path $worktrees '.locks' }
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

function Test-UESTCWriterWaiting {
    param([string]$LockRoot)
    foreach ($file in (Get-ChildItem -LiteralPath $LockRoot -Filter 'gpu-writer-*.lock' -File)) {
        $probe = $null
        try {
            $probe = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch [IO.IOException] {
            # A held handle is live intent; a concurrent close merely costs one retry.
            return $true
        } finally { if ($probe) { $probe.Dispose() } }
    }
    return $false
}

function Open-UESTCResourceSlot {
    param([string]$LockRoot, [string]$Kind, [int]$Limit)
    for ($i = 0; $i -lt $Limit; $i++) {
        try {
            return [IO.File]::Open((Join-Path $LockRoot ("{0}-{1}.lock" -f $Kind.ToLowerInvariant(), $i)), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch [IO.IOException] { }
    }
    return $null
}

function Enter-UESTCResourceLock {
    param([Parameter(Mandatory)][string]$LockRoot,
        [Parameter(Mandatory)][ValidateSet('Cpu','GpuShared','CpuGpuShared','GpuExclusive')][string]$Kind,
        [int]$TimeoutSeconds = 1800)
    if ($TimeoutSeconds -lt 1) { throw 'Lock timeout must be positive.' }
    $null = [IO.Directory]::CreateDirectory($LockRoot)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $cpuLimit = [Math]::Max(2, [Math]::Floor([Environment]::ProcessorCount / 8.0))
    $intent = $null
    $turnstile = $null
    $barrier = $null
    try {
        if ($Kind -eq 'GpuExclusive') {
            # Register before competing for admission: even a second waiting writer
            # prevents newly arriving readers from winning the turnstile repeatedly.
            $intentPath = Join-Path $LockRoot ("gpu-writer-{0}-{1}.lock" -f $PID, [Guid]::NewGuid().ToString('N'))
            $intent = New-Object IO.FileStream($intentPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 4096, [IO.FileOptions]::DeleteOnClose)
            $identity = [Text.Encoding]::UTF8.GetBytes("pid=$PID`nkind=$Kind`n")
            $intent.Write($identity, 0, $identity.Length)
            $intent.Flush()
            while ([DateTime]::UtcNow -lt $deadline) {
                try {
                    if (-not $turnstile) {
                        $turnstile = [IO.File]::Open((Join-Path $LockRoot 'admission.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                    }
                    # Hold admission closed while already admitted work drains.
                    $barrier = [IO.File]::Open((Join-Path $LockRoot 'gpu-exclusive.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                    $lease = [pscustomobject]@{ Kind = $Kind; Handles = @($barrier, $turnstile, $intent) }
                    $barrier = $null; $turnstile = $null; $intent = $null
                    return $lease
                } catch [IO.IOException] { }
                Start-Sleep -Milliseconds 100
            }
        } else {
            # Never nest Cpu then GpuShared: acquire their slots together so a
            # waiting writer cannot form a cycle with a partly admitted job.
            $kinds = switch ($Kind) {
                'CpuGpuShared' { @('Cpu', 'GpuShared') }
                default { @($Kind) }
            }
            while ([DateTime]::UtcNow -lt $deadline) {
                $slots = New-Object 'Collections.Generic.List[object]'
                try {
                    $turnstile = [IO.File]::Open((Join-Path $LockRoot 'admission.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                    if (-not (Test-UESTCWriterWaiting $LockRoot)) {
                        $barrier = [IO.File]::Open((Join-Path $LockRoot 'gpu-exclusive.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite)
                        foreach ($slotKind in $kinds) {
                            $limit = if ($slotKind -eq 'Cpu') { $cpuLimit } else { 2 }
                            $slot = Open-UESTCResourceSlot $LockRoot $slotKind $limit
                            if (-not $slot) { break }
                            $slots.Add($slot)
                        }
                        if ($slots.Count -eq @($kinds).Count) {
                            $slots.Add($barrier)
                            $lease = [pscustomobject]@{ Kind = $Kind; Handles = $slots.ToArray() }
                            $barrier = $null
                            $slots.Clear()
                            return $lease
                        }
                    }
                } catch [IO.IOException] { }
                finally {
                    foreach ($slot in $slots) { $slot.Dispose() }
                    if ($barrier) { $barrier.Dispose(); $barrier = $null }
                    if ($turnstile) { $turnstile.Dispose(); $turnstile = $null }
                }
                Start-Sleep -Milliseconds 100
            }
        }
        throw "Timed out waiting for $Kind lock in $LockRoot"
    } finally {
        if ($barrier) { $barrier.Dispose() }
        if ($turnstile) { $turnstile.Dispose() }
        if ($intent) { $intent.Dispose() }
    }
}

function Exit-UESTCResourceLock {
    param($Lease)
    if ($Lease) { foreach ($handle in $Lease.Handles) { $handle.Dispose() } }
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
        [int[]]$SuccessExitCodes = @(0), [Text.Encoding]$TextEncoding = $null)
    if ($Log) { $null = [IO.Directory]::CreateDirectory((Split-Path $Log -Parent)) }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $Executable
    $info.Arguments = (($Arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = -not $Visible
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    if ($TextEncoding) {
        $info.StandardOutputEncoding = $TextEncoding
        $info.StandardErrorEncoding = $TextEncoding
    }
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    $started = $false
    try {
        $started = $process.Start()
        if (-not $started) { throw "Could not start $Executable" }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (-not $process.WaitForExit(500)) {
            if ([DateTime]::UtcNow -ge $deadline) { throw "Process timeout: $Executable" }
        }
        $stdoutText = $stdout.GetAwaiter().GetResult()
        $stderrText = $stderr.GetAwaiter().GetResult()
        $output = $stdoutText + $stderrText
        if ($Log) { [IO.File]::WriteAllText($Log, $output, (New-Object Text.UTF8Encoding $false)) }
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
        if ($started -and -not $process.HasExited) {
            $process.Kill()
            $process.WaitForExit()
            if ($Log) { [IO.File]::WriteAllText($Log, ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()), (New-Object Text.UTF8Encoding $false)) }
        }
        $process.Dispose()
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
