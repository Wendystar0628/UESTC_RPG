# Interprocess tests use isolated output lock roots and owned workers.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../lib/common.ps1')
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$common=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../lib/common.ps1'))
$work=Join-Path $root ('output/build/gates/lock-self-test/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fffffff'))
$null=[IO.Directory]::CreateDirectory($work)
$workers=New-Object 'Collections.Generic.List[object]'
$cases=New-Object 'Collections.Generic.List[string]'
$held=$null
$utf8=New-Object Text.UTF8Encoding $false
function Assert-Lock {param([bool]$Condition,[string]$Message) if(-not $Condition){throw $Message}}
function Wait-Lock {param([scriptblock]$Condition,[string]$Message) $clock=[Diagnostics.Stopwatch]::StartNew();while(-not (&$Condition)){if($clock.Elapsed.TotalSeconds -gt 12){throw $Message};Start-Sleep -Milliseconds 25}}
function Worker-Path {param($Worker,[string]$Suffix) return Join-Path $work ($Worker.Name+$Suffix)}
function Has-Acquired {param($Worker) return Test-Path -LiteralPath (Worker-Path $Worker '.acquired.json')}
function Start-LockWorker {
    param([string]$Name,[string]$Locks,[int]$Timeout=20,[string]$Mode='Hold',[string]$Repository='')
    $exe=Join-Path $PSHOME 'powershell.exe'
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$exe
    $info.Arguments=(@('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'lock-worker.ps1'),$common,$Locks,$Name,$work,[string]$Timeout,$Mode,$Repository) | ForEach-Object {ConvertTo-UESTCProcessArgument $_}) -join ' '
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.WorkingDirectory=$root
    $info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $p=New-Object Diagnostics.Process;$p.StartInfo=$info
    if(-not $p.Start()){throw "Cannot start $Name"}
    $worker=[pscustomobject]@{Name=$Name;Process=$p;BirthTicks=$p.StartTime.ToUniversalTime().Ticks;Exe=$exe;Stdout=$p.StandardOutput.ReadToEndAsync();Stderr=$p.StandardError.ReadToEndAsync()}
    $workers.Add($worker)
    Wait-Lock {Test-Path -LiteralPath (Worker-Path $worker '.started.json')} "Worker $Name did not start"
    return $worker
}
function Release-LockWorker {param($Worker) [IO.File]::WriteAllText((Worker-Path $Worker '.release'),'release',$utf8)}
function Wait-LockExit {param($Worker,[int]$Expected=0) Assert-Lock ($Worker.Process.WaitForExit(5000)) "Worker exit timeout: $($Worker.Name)";Assert-Lock ($Worker.Process.ExitCode -eq $Expected) "Unexpected exit: $($Worker.Name); $($Worker.Stderr.GetAwaiter().GetResult())"}
function Stop-OwnedLockWorker {
    param($Worker)
    if($Worker.Process.HasExited){return}
    Assert-Lock ($Worker.Process.StartTime.ToUniversalTime().Ticks -eq $Worker.BirthTicks -and [string]::Equals($Worker.Process.MainModule.FileName,$Worker.Exe,[StringComparison]::OrdinalIgnoreCase)) 'Refusing to terminate a non-owned/reused worker PID.'
    $Worker.Process.Kill();Assert-Lock ($Worker.Process.WaitForExit(5000)) 'Owned worker did not terminate.'
}
try {
    $locks=Join-Path $work 'locks'
    $first=Start-LockWorker 'first' $locks
    Wait-Lock {Has-Acquired $first} 'First worker did not acquire'
    $second=Start-LockWorker 'second' $locks 1
    Wait-LockExit $second 1
    Assert-Lock (-not (Has-Acquired $second)) 'Two processes held the build lock'
    $errorRecord=Get-Content -Raw -LiteralPath (Worker-Path $second '.error.json') | ConvertFrom-Json
    Assert-Lock ($errorRecord.error -like 'Timed out waiting for build lock*') 'Lock contention did not time out'
    Release-LockWorker $first;Wait-LockExit $first
    $probe=Enter-UESTCBuildLock $locks -TimeoutSeconds 2;Exit-UESTCBuildLock $probe
    $cases.Add('capacity-one interprocess exclusion, timeout and normal release')

    $failed=Start-LockWorker 'exception' $locks 2 ThrowAfterAcquire
    Wait-LockExit $failed 1
    Assert-Lock (Has-Acquired $failed) 'Exception worker never held the lock'
    $probe=Enter-UESTCBuildLock $locks -TimeoutSeconds 2;Exit-UESTCBuildLock $probe
    $crashed=Start-LockWorker 'terminated' $locks
    Wait-Lock {Has-Acquired $crashed} 'Terminated worker never held the lock'
    Stop-OwnedLockWorker $crashed
    $probe=Enter-UESTCBuildLock $locks -TimeoutSeconds 2;Exit-UESTCBuildLock $probe
    $cases.Add('exception and process-termination release, persistent empty file reacquisition')

    # The retained visual checkout discovers Primary through git-common-dir.
    $vis=@(Get-UESTCGitLines $root @('worktree','list','--porcelain') | Where-Object { $_ -like 'worktree */VIS-check' -or $_ -like 'worktree *\VIS-check' })
    Assert-Lock ($vis.Count -eq 1) 'Expected the retained VIS-check checkout'
    $visRoot=$vis[0].Substring(9)
    $layout=Get-UESTCLayout $root
    $visLayout=Get-UESTCLayout $visRoot
    Assert-Lock ($layout.Locks -eq $visLayout.Locks -and $layout.Locks -eq (Join-Path $root 'output/.locks')) 'VIS and primary have different build locks'
    $held=Enter-UESTCBuildLock $layout.Locks
    $visual=Start-LockWorker 'VIS-check' '' 1 Hold $visRoot
    Wait-LockExit $visual 1
    Assert-Lock (-not (Has-Acquired $visual)) 'VIS entered while primary held the build lock'
    Exit-UESTCBuildLock $held;$held=$null
    $visual=Start-LockWorker 'VIS-released' '' 2 Hold $visRoot
    Wait-Lock {Has-Acquired $visual} 'VIS did not acquire the released primary lock'
    Release-LockWorker $visual;Wait-LockExit $visual
    $cases.Add('primary and VIS-check share the same interprocess lock')
    $report=Join-Path $work 'result.json'
    [IO.File]::WriteAllText($report,(@{status='Passed';cases=$cases.ToArray()} | ConvertTo-Json -Depth 5),$utf8)
    Write-Output "Passed: $($cases -join '; '). Report: $report"
} finally {
    Exit-UESTCBuildLock $held
    foreach($worker in $workers){Stop-OwnedLockWorker $worker;$worker.Process.Dispose()}
}
