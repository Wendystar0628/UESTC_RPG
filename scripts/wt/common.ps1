# Shared path, process and handle-lock helpers. Compatible with Windows PowerShell 5.1.
Set-StrictMode -Version Latest

function Get-UESTCLayout {
    param([Parameter(Mandatory)][string]$Repository)
    $root = [IO.Path]::GetFullPath($Repository).TrimEnd('\', '/')
    $common = (& git -C $root rev-parse --git-common-dir 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0) { throw "Not a git worktree: $root" }
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
        [Parameter(Mandatory)][string]$WorkingDirectory, [Parameter(Mandatory)][string]$Log,
        [int]$TimeoutSeconds = 1800, [switch]$AllowFailure, [switch]$Visible)
    $null = [IO.Directory]::CreateDirectory((Split-Path $Log -Parent))
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $Executable
    $info.Arguments = (($Arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = -not $Visible
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
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
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText($Log, $output, (New-Object Text.UTF8Encoding $false))
        $result = [pscustomobject]@{ ExitCode = $process.ExitCode; Output = $output; Log = $Log }
        if (-not $AllowFailure -and $result.ExitCode -ne 0) { throw "Exit $($result.ExitCode): $Executable. See $Log" }
        return $result
    } finally {
        if ($started -and -not $process.HasExited) {
            $process.Kill()
            $process.WaitForExit()
            [IO.File]::WriteAllText($Log, ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()), (New-Object Text.UTF8Encoding $false))
        }
        $process.Dispose()
    }
}

function Get-UESTCGodot {
    if ($env:UESTC_GODOT) { return [IO.Path]::GetFullPath($env:UESTC_GODOT) }
    return 'E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
}
