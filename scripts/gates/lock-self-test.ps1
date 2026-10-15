# Interprocess lock checks only: no Godot, build, import or production lock files.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../wt/common.ps1')
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $root ('output/build/gates/lock-self-test/' + [Guid]::NewGuid().ToString('N'))
$locks = Join-Path $output 'locks'
$null = [IO.Directory]::CreateDirectory($locks)
$common = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../wt/common.ps1'))
$workerScript = Join-Path $output 'worker.ps1'
$utf8 = New-Object Text.UTF8Encoding $false
$workerCode = @'
param([string]$Common,[string]$Locks,[string]$Kind,[string]$Ready,[string]$Release)
$ErrorActionPreference='Stop'
. $Common
$lease=$null
try {
    $lease=Enter-UESTCResourceLock $Locks $Kind -TimeoutSeconds 20
    [IO.File]::WriteAllText($Ready, [string]$PID)
    while (-not (Test-Path -LiteralPath $Release)) { Start-Sleep -Milliseconds 20 }
} catch {
    [IO.File]::WriteAllText(($Ready+'.error'), $_.Exception.ToString())
    exit 1
} finally { Exit-UESTCResourceLock $lease }
'@
[IO.File]::WriteAllText($workerScript, $workerCode, $utf8)
$workers = New-Object 'Collections.Generic.List[object]'
$held = $null
function Start-Worker {
    param([string]$Name,[string]$Kind)
    $ready = Join-Path $output ($Name + '.ready')
    $release = Join-Path $output ($Name + '.release')
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $PSHOME 'powershell.exe'
    $arguments = @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$workerScript,$common,$locks,$Kind,$ready,$release)
    $info.Arguments = (($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' ')
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WorkingDirectory = $root
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    if (-not $process.Start()) { throw "Cannot start worker $Name" }
    $worker = [pscustomobject]@{ Name=$Name; Kind=$Kind; Process=$process; Ready=$ready; Release=$release }
    $workers.Add($worker)
    return $worker
}
function Wait-Condition {
    param([scriptblock]$Condition,[string]$Message)
    $deadline = [DateTime]::UtcNow.AddSeconds(8)
    while (-not (& $Condition)) {
        foreach ($worker in $workers) {
            if (Test-Path -LiteralPath ($worker.Ready + '.error')) { throw ([IO.File]::ReadAllText($worker.Ready + '.error')) }
        }
        if ([DateTime]::UtcNow -ge $deadline) { throw $Message }
        Start-Sleep -Milliseconds 30
    }
}
function Release-Worker { param($Worker) [IO.File]::WriteAllText($Worker.Release, 'release') }
function Assert-NoReader { param($Reader) if (Test-Path -LiteralPath $Reader.Ready) { throw 'A new shared reader overtook a waiting writer.' } }
try {
    $held = Enter-UESTCResourceLock $locks CpuGpuShared
    $writerOne = Start-Worker 'writer-one' GpuExclusive
    Wait-Condition { @(Get-ChildItem -LiteralPath $locks -Filter 'gpu-writer-*.lock' -File).Count -ge 1 } 'Writer did not register its intent.'
    $writerTwo = Start-Worker 'writer-two' GpuExclusive
    Wait-Condition { @(Get-ChildItem -LiteralPath $locks -Filter 'gpu-writer-*.lock' -File).Count -ge 2 } 'Second writer did not register its intent.'
    $reader = Start-Worker 'late-reader' CpuGpuShared
    Start-Sleep -Milliseconds 150
    Assert-NoReader $reader
    Exit-UESTCResourceLock $held; $held = $null
    Wait-Condition { (Test-Path -LiteralPath $writerOne.Ready) -or (Test-Path -LiteralPath $writerTwo.Ready) } 'An exclusive writer failed to acquire after readers drained.'
    Assert-NoReader $reader
    $first = if (Test-Path -LiteralPath $writerOne.Ready) { $writerOne } else { $writerTwo }
    $second = if ($first.Name -eq $writerOne.Name) { $writerTwo } else { $writerOne }
    Release-Worker $first
    Wait-Condition { Test-Path -LiteralPath $second.Ready } 'Second writer did not run.'
    Assert-NoReader $reader
    Release-Worker $second
    Wait-Condition { Test-Path -LiteralPath $reader.Ready } 'Shared work did not resume after writers.'
    Release-Worker $reader
    foreach ($worker in @($writerOne,$writerTwo,$reader)) {
        if (-not $worker.Process.WaitForExit(3000) -or $worker.Process.ExitCode -ne 0) { throw "Worker failed: $($worker.Name)" }
    }

    $held = Enter-UESTCResourceLock $locks GpuShared
    $waitingCrash = Start-Worker 'waiting-crash' GpuExclusive
    Wait-Condition { Test-UESTCWriterWaiting $locks } 'Crash-test writer did not queue.'
    $waitingCrash.Process.Kill(); $waitingCrash.Process.WaitForExit()
    Wait-Condition { -not (Test-UESTCWriterWaiting $locks) } 'Terminated waiter left a live intent.'
    $probe = Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2
    Exit-UESTCResourceLock $probe
    Exit-UESTCResourceLock $held; $held = $null

    $activeCrash = Start-Worker 'active-crash' GpuExclusive
    Wait-Condition { Test-Path -LiteralPath $activeCrash.Ready } 'Crash-test writer did not acquire.'
    $activeCrash.Process.Kill(); $activeCrash.Process.WaitForExit()
    $probe = Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2
    Exit-UESTCResourceLock $probe
    if (@(Get-ChildItem -LiteralPath $locks -Filter 'gpu-writer-*.lock' -File).Count) { throw 'DeleteOnClose writer intent survived process termination.' }
    [IO.File]::WriteAllText((Join-Path $locks 'gpu-writer-stale.lock'), 'old unlocked diagnostic file')
    $probe = Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2
    Exit-UESTCResourceLock $probe
    Write-Output 'Passed: two queued writers precede late shared work; atomic CPU/GPU admission; killed waiting/active writers release all handles; stale unlocked files do not block.'
} finally {
    Exit-UESTCResourceLock $held
    foreach ($worker in $workers) {
        if (-not $worker.Process.HasExited) { $worker.Process.Kill(); $worker.Process.WaitForExit() }
        $worker.Process.Dispose()
    }
}
