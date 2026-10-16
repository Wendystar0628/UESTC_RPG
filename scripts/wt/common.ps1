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

# Tickets order publication under fair-register.lock, independently of admission.lock.
# Earlier pending requests block later requests only when their resource masks intersect.
# Legacy leases share the same barriers/slots; legacy arrivals do not honor ticket fairness.
function Test-UESTCFairSharingViolation {
    param([IO.IOException]$IoError)
    return ($IoError.HResult -band 0xffff) -in @(32,33)
}

function Get-UESTCFairResourceMask {
    param([string]$Kind)
    switch ($Kind) {
        'Cpu' { return 1 }
        'GpuShared' { return 2 }
        'CpuGpuShared' { return 3 }
        'GpuExclusive' { return 3 }
        default { throw "Invalid resource request kind: $Kind" }
    }
}

function Open-UESTCFairFileLock {
    param([string]$Path,[IO.FileShare]$Share=[IO.FileShare]::None)
    try { return [IO.File]::Open($Path,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,$Share) }
    catch [IO.IOException] { if (-not (Test-UESTCFairSharingViolation $_.Exception)) { throw }; return $null }
}

function Open-UESTCFairRegistration {
    param([string]$LockRoot,[Diagnostics.Stopwatch]$Timer,[double]$TimeoutMilliseconds)
    while ($Timer.Elapsed.TotalMilliseconds -lt $TimeoutMilliseconds) {
        $handle=Open-UESTCFairFileLock (Join-Path $LockRoot 'fair-register.lock')
        if ($handle) { return $handle }
        Start-Sleep -Milliseconds 10
    }
    throw "Timed out waiting for resource registration in $LockRoot"
}

function Read-UESTCFairJson {
    param([string]$Path)
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader=$null
    try { $reader=New-Object IO.StreamReader($stream,[Text.Encoding]::UTF8,$true); return ConvertFrom-Json -InputObject $reader.ReadToEnd() }
    finally { if ($reader) { $reader.Dispose() }; $stream.Dispose() }
}

function ConvertTo-UESTCFairTicket {
    param($Value)
    [UInt64]$number=0
    if ($Value -isnot [string] -or $Value -notmatch '^[0-9]{1,20}$' -or -not [UInt64]::TryParse($Value,[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)) {
        throw 'Invalid resource ticket counter/metadata; no state was reset.'
    }
    return $number
}

function Test-UESTCFairFileLive {
    param([string]$Path)
    $probe=$null
    try {
        # A new write handle is denied by a live request's ShareRead|Delete handle.
        # No contents are written; unlocked stale files are ignored, never deleted.
        $probe=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        return $false
    } catch [IO.FileNotFoundException] { return $false }
    catch [IO.IOException] { if (Test-UESTCFairSharingViolation $_.Exception) { return $true }; throw }
    finally { if ($probe) { $probe.Dispose() } }
}

function Get-UESTCFairPendingLocked {
    param([string]$LockRoot)
    foreach ($file in @(Get-ChildItem -LiteralPath $LockRoot -Filter 'fair-resource-*.request.json' -File)) {
        if (-not (Test-UESTCFairFileLive $file.FullName)) { continue }
        try { $data=Read-UESTCFairJson $file.FullName } catch [IO.FileNotFoundException] { continue }
        $ticket=ConvertTo-UESTCFairTicket $data.ticket
        $expected='fair-resource-'+$ticket.ToString('D20',[Globalization.CultureInfo]::InvariantCulture)+'.request.json'
        if ($data.schema -cne 'uestc-resource-request-v1' -or $ticket -eq 0 -or $file.Name -cne $expected -or
            $data.mask -ne (Get-UESTCFairResourceMask $data.kind)) { throw "Invalid live resource request metadata: $($file.FullName)" }
        [pscustomobject]@{ Ticket=$ticket; Kind=$data.kind; Mask=[int]$data.mask; Path=$file.FullName }
    }
}

function Write-UESTCFairCounterLocked {
    param([string]$Path,[UInt64]$LastIssued)
    # Randomness only identifies an unpublished scratch file, never queue order.
    # Close it BEFORE atomic publication: DeleteOnClose here would delete the renamed counter.
    $temporary=$Path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    $stream=New-Object IO.FileStream($temporary,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try {
        $json=[ordered]@{ version=1; lastIssued=$LastIssued.ToString('D20',[Globalization.CultureInfo]::InvariantCulture) } | ConvertTo-Json -Compress
        $bytes=[Text.Encoding]::UTF8.GetBytes($json+"`n"); $stream.Write($bytes,0,$bytes.Length); $stream.Flush($true)
    } finally { $stream.Dispose() }
    if ([IO.File]::Exists($Path)) { [IO.File]::Replace($temporary,$Path,[System.Management.Automation.Language.NullString]::Value) }
    else { [IO.File]::Move($temporary,$Path) }
    # A crash can leave a scratch file. It is neither a request nor a lock and is never scanned/deleted.
}

function New-UESTCFairRequest {
    param([string]$LockRoot,[string]$Kind,[Diagnostics.Stopwatch]$Timer,[double]$TimeoutMilliseconds)
    $registration=$null; $requestHandle=$null
    try {
        $registration=Open-UESTCFairRegistration $LockRoot $Timer $TimeoutMilliseconds
        $counterPath=Join-Path $LockRoot 'fair-resource-counter.json'
        $pending=@(Get-UESTCFairPendingLocked $LockRoot)
        if ([IO.File]::Exists($counterPath)) {
            $counter=Read-UESTCFairJson $counterPath
            if ($counter.version -ne 1) { throw 'Invalid resource ticket counter version; no state was reset.' }
            [UInt64]$last=ConvertTo-UESTCFairTicket $counter.lastIssued
        } else {
            if (@(Get-ChildItem -LiteralPath $LockRoot -Filter 'fair-resource-*.request.json' -File).Count) { throw 'Resource counter is missing while request artifacts exist; no state was reset.' }
            [UInt64]$last=0
        }
        if (@($pending | Where-Object { $_.Ticket -gt $last }).Count) { throw 'Resource counter precedes a live ticket; no state was reset.' }
        if ($last -eq [UInt64]::MaxValue) { throw 'Resource ticket counter exhausted; no state was reset.' }
        [UInt64]$ticket=([decimal]$last+[decimal]1)
        Write-UESTCFairCounterLocked $counterPath $ticket
        $text=$ticket.ToString('D20',[Globalization.CultureInfo]::InvariantCulture)
        $path=Join-Path $LockRoot ('fair-resource-'+$text+'.request.json')
        $mask=Get-UESTCFairResourceMask $Kind
        $requestHandle=New-Object IO.FileStream($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,([IO.FileShare]::Read -bor [IO.FileShare]::Delete),4096,[IO.FileOptions]::DeleteOnClose)
        $bytes=[Text.Encoding]::UTF8.GetBytes(([ordered]@{schema='uestc-resource-request-v1';ticket=$text;kind=$Kind;mask=$mask;diagnosticPid=$PID} | ConvertTo-Json -Compress)+"`n")
        $requestHandle.Write($bytes,0,$bytes.Length); $requestHandle.Flush($true)
        $request=[pscustomobject]@{ Ticket=$ticket; TicketText=$text; Kind=$Kind; Mask=$mask; Path=$path; Handle=$requestHandle }
        $requestHandle=$null
        return $request
    } finally { if ($requestHandle) { $requestHandle.Dispose() }; if ($registration) { $registration.Dispose() } }
}

function Test-UESTCFairBlocked {
    param([string]$LockRoot,$Request,[Diagnostics.Stopwatch]$Timer,[double]$TimeoutMilliseconds)
    # Check while holding admission; registration cannot publish a partial request.
    $registration=Open-UESTCFairRegistration $LockRoot $Timer $TimeoutMilliseconds
    try { return @((Get-UESTCFairPendingLocked $LockRoot) | Where-Object { $_.Ticket -lt $Request.Ticket -and ($_.Mask -band $Request.Mask) -ne 0 }).Count -gt 0 }
    finally { $registration.Dispose() }
}

function Test-UESTCFairWriterWaiting {
    param([string]$LockRoot)
    foreach ($file in @(Get-ChildItem -LiteralPath $LockRoot -Filter 'gpu-writer-*.lock' -File)) {
        if (Test-UESTCFairFileLive $file.FullName) { return $true }
    }
    return $false
}

function Enter-UESTCResourceLock {
    param([Parameter(Mandatory)][string]$LockRoot,
        [Parameter(Mandatory)][ValidateSet('Cpu','GpuShared','CpuGpuShared','GpuExclusive')][string]$Kind,
        [int]$TimeoutSeconds=1800)
    if ($TimeoutSeconds -lt 1) { throw 'Lock timeout must be positive.' }
    $timer=[Diagnostics.Stopwatch]::StartNew(); $timeout=[double]$TimeoutSeconds*1000
    $null=[IO.Directory]::CreateDirectory($LockRoot)
    $cpuLimit=[Math]::Max(2,[Math]::Floor([Environment]::ProcessorCount/8.0))
    $request=$null; $turnstile=$null; $barrier=$null; $intent=$null
    try {
        $request=New-UESTCFairRequest $LockRoot $Kind $timer $timeout
        if ($Kind -eq 'GpuExclusive') {
            while ($timer.Elapsed.TotalMilliseconds -lt $timeout) {
                if (-not $turnstile) {
                    $turnstile=Open-UESTCFairFileLock (Join-Path $LockRoot 'admission.lock')
                    if (-not $turnstile) { Start-Sleep -Milliseconds 100; continue }
                    if ((Test-UESTCFairBlocked $LockRoot $request $timer $timeout) -or (Test-UESTCFairWriterWaiting $LockRoot)) {
                        $turnstile.Dispose(); $turnstile=$null; Start-Sleep -Milliseconds 100; continue
                    }
                    # Only the eligible exclusive publishes legacy intent. Earlier queued requests cannot be blocked by it.
                    $intentPath=Join-Path $LockRoot ('gpu-writer-fair-'+$request.TicketText+'.lock')
                    $intent=New-Object IO.FileStream($intentPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None,4096,[IO.FileOptions]::DeleteOnClose)
                }
                # No registration handle is held while existing work drains. Do not yield to later legacy intents here.
                $barrier=Open-UESTCFairFileLock (Join-Path $LockRoot 'gpu-exclusive.lock')
                if ($barrier) {
                    $lease=[pscustomobject]@{ Kind=$Kind; Ticket=$request.TicketText; Handles=@($barrier,$turnstile,$intent) }
                    $request.Handle.Dispose(); $request=$null
                    $barrier=$null; $turnstile=$null; $intent=$null
                    return $lease
                }
                Start-Sleep -Milliseconds 100
            }
        } else {
            $kinds=switch ($Kind) { 'CpuGpuShared' { @('Cpu','GpuShared') } default { @($Kind) } }
            while ($timer.Elapsed.TotalMilliseconds -lt $timeout) {
                $slots=New-Object 'Collections.Generic.List[object]'
                try {
                    $turnstile=Open-UESTCFairFileLock (Join-Path $LockRoot 'admission.lock')
                    if ($turnstile -and -not (Test-UESTCFairBlocked $LockRoot $request $timer $timeout) -and -not (Test-UESTCFairWriterWaiting $LockRoot)) {
                        $barrier=Open-UESTCFairFileLock (Join-Path $LockRoot 'gpu-exclusive.lock') ([IO.FileShare]::ReadWrite)
                        if ($barrier) {
                            foreach ($slotKind in $kinds) {
                                $limit=if ($slotKind -eq 'Cpu') { $cpuLimit } else { 2 }
                                $slot=Open-UESTCResourceSlot $LockRoot $slotKind $limit
                                if (-not $slot) { break }
                                $slots.Add($slot)
                            }
                            if ($slots.Count -eq @($kinds).Count) {
                                $slots.Add($barrier)
                                $lease=[pscustomobject]@{ Kind=$Kind; Ticket=$request.TicketText; Handles=$slots.ToArray() }
                                $request.Handle.Dispose(); $request=$null
                                $barrier=$null; $slots.Clear()
                                return $lease
                            }
                        }
                    }
                } finally {
                    # A composite never retains a CPU slot while waiting for a GPU slot.
                    foreach ($slot in $slots) { $slot.Dispose() }
                    if ($barrier) { $barrier.Dispose(); $barrier=$null }
                    if ($turnstile) { $turnstile.Dispose(); $turnstile=$null }
                }
                Start-Sleep -Milliseconds 100
            }
        }
        throw "Timed out waiting for $Kind lock in $LockRoot"
    } finally {
        if ($barrier) { $barrier.Dispose() }; if ($turnstile) { $turnstile.Dispose() }; if ($intent) { $intent.Dispose() }
        if ($request) { $request.Handle.Dispose() }
        $timer.Stop()
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
