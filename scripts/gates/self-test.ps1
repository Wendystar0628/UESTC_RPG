# Script-only smoke tests: no build, import, worktree creation or global locks.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../wt/common.ps1')
. (Join-Path $PSScriptRoot 'check-source.ps1')
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $root 'output/build/gates/script-self-test'
$null = [IO.Directory]::CreateDirectory($output)
$locks = Join-Path $output 'locks'
$leases = New-Object 'Collections.Generic.List[object]'
function Assert-True { param([bool]$Value, [string]$Message) if (-not $Value) { throw $Message } }
function Assert-LockBlocked {
    param([string]$Kind)
    $unexpected = $null
    try { $unexpected = Enter-UESTCResourceLock $locks $Kind -TimeoutSeconds 1 }
    catch { if ($_.Exception.Message -notlike 'Timed out*') { throw }; return }
    finally { if ($unexpected) { Exit-UESTCResourceLock $unexpected } }
    throw "$Kind lock was incorrectly granted."
}
try {
    $layout = Get-UESTCLayout $root
    Assert-True ($layout.Root -eq [IO.Path]::GetFullPath($root)) 'Wrong repository root.'
    Assert-True ($layout.Locks -eq (Join-Path $layout.Worktrees '.locks')) 'Wrong lock root.'
    try { $null = Assert-UESTCChildPath (Join-Path $output '../escape') $output; throw 'Traversal accepted.' }
    catch { if ($_.Exception.Message -notlike 'Path escapes*') { throw } }
    Assert-True (Test-UESTCGlob 'game/contracts/A.cs' 'game/contracts/**') 'Directory glob failed.'
    Assert-True (-not (Test-UESTCGlob 'game/contracts/A.cs' 'game/*.cs')) 'Single star crossed a directory.'
    $a = Enter-UESTCResourceLock $locks GpuShared; $leases.Add($a)
    $b = Enter-UESTCResourceLock $locks GpuShared; $leases.Add($b)
    Assert-LockBlocked GpuShared
    Assert-LockBlocked GpuExclusive
    Exit-UESTCResourceLock $a; Exit-UESTCResourceLock $b; $leases.Clear()
    $cpu = Enter-UESTCResourceLock $locks Cpu; $leases.Add($cpu)
    Assert-LockBlocked GpuExclusive
    Exit-UESTCResourceLock $cpu; $leases.Clear()
    $cpuLimit = [Math]::Max(2, [Math]::Floor([Environment]::ProcessorCount / 8.0))
    for ($i = 0; $i -lt $cpuLimit; $i++) { $leases.Add((Enter-UESTCResourceLock $locks Cpu)) }
    Assert-LockBlocked Cpu
    foreach ($lease in $leases) { Exit-UESTCResourceLock $lease }; $leases.Clear()
    $exclusive = Enter-UESTCResourceLock $locks GpuExclusive; $leases.Add($exclusive)
    Assert-LockBlocked Cpu
    Assert-LockBlocked GpuShared
    Exit-UESTCResourceLock $exclusive; $leases.Clear()
    # Existing empty lock files are harmless: only the held OS handle represents ownership.
    $reopened = Enter-UESTCResourceLock $locks GpuExclusive
    Exit-UESTCResourceLock $reopened
    $version = Invoke-UESTCProcess dotnet @('--version') $root (Join-Path $output 'dotnet-version.log')
    Assert-True ($version.ExitCode -eq 0) 'Process wrapper failed.'
    $failure = Invoke-UESTCProcess (Join-Path $PSHOME 'powershell.exe') @('-NoProfile','-NonInteractive','-Command','exit 23') $root (Join-Path $output 'expected-failure.log') -AllowFailure
    Assert-True ($failure.ExitCode -eq 23) 'Native process failure was not preserved.'
    $echoScript = Join-Path $output 'echo-args.ps1'
    [IO.File]::WriteAllText($echoScript, '@($args) | ConvertTo-Json -Compress', (New-Object Text.UTF8Encoding $false))
    $arguments = @('a b', 'a"b', 'C:\space path\', 'literal$(value)', '')
    $echo = Invoke-UESTCProcess (Join-Path $PSHOME 'powershell.exe') (@('-NoProfile','-NonInteractive','-File',$echoScript) + $arguments) $root (Join-Path $output 'quote-check.log')
    $actual = ConvertFrom-Json -InputObject $echo.Output
    Assert-True (@($actual).Count -eq $arguments.Count) 'Native quoting dropped an argument.'
    for ($i = 0; $i -lt $arguments.Count; $i++) { Assert-True ($actual[$i] -ceq $arguments[$i]) "Native quoting changed argument $i." }
    Write-Output 'Passed: repository layout, containment, glob, CPU limit, GPU max=2, GPU exclusive vs CPU/shared, release/reacquisition, native failures and argument quoting.'
} finally { foreach ($lease in $leases) { Exit-UESTCResourceLock $lease } }
