# Interprocess tests use isolated output lock roots and birth-checked owned workers.
# Supply -LegacyCommon with a pre-upgrade common.ps1 snapshot to verify mixed-version migration.
[CmdletBinding()]
param([string]$LegacyCommon = '')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../wt/common.ps1')
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$common=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../wt/common.ps1'))
$work=Join-Path $root ('output/build/gates/lock-self-test/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fffffff'))
if ($LegacyCommon) { $LegacyCommon=(Resolve-Path -LiteralPath $LegacyCommon).Path }
$null=[IO.Directory]::CreateDirectory($work)
$workers=New-Object 'Collections.Generic.List[object]'
$cases=New-Object 'Collections.Generic.List[object]'
$holds=New-Object 'Collections.Generic.List[object]'
$cpuLimit=[int][Math]::Max(2,[Math]::Floor([Environment]::ProcessorCount/8.0))
$utf8=New-Object Text.UTF8Encoding $false
function Assert-Fair {param([bool]$Condition,[string]$Message) if(-not $Condition){throw $Message}}
function Wait-Fair {param([scriptblock]$Condition,[string]$Message,[int]$Seconds=12) $fairWaitClock=[Diagnostics.Stopwatch]::StartNew();while(-not (&$Condition)){if($fairWaitClock.Elapsed.TotalSeconds -gt $Seconds){throw $Message};Start-Sleep -Milliseconds 25}}
function Worker-Path {param($Worker,[string]$Suffix) return Join-Path $work ($Worker.Name+$Suffix)}
function Has-Acquired {param($Worker) return Test-Path -LiteralPath (Worker-Path $Worker '.acquired.json')}
function Start-FairWorker {
    param([string]$Name,[string]$Locks,[string]$Kind,[int]$Timeout=20,[string]$Mode='Hold',[switch]$Legacy,[string]$StartGate='')
    $workerCommon=if($Legacy){$LegacyCommon}else{$common}
    if (-not $workerCommon) { throw 'LegacyCommon is required for legacy workers.' }
    $exe=Join-Path $PSHOME 'powershell.exe'
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$exe;$info.Arguments=(@('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'lock-worker.ps1'),$workerCommon,$Locks,$Name,$work,$Kind,[string]$Timeout,$Mode,$StartGate) | ForEach-Object {ConvertTo-UESTCProcessArgument $_}) -join ' '
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.WorkingDirectory=$PSScriptRoot;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $p=New-Object Diagnostics.Process;$p.StartInfo=$info
    if(-not $p.Start()){throw "Cannot start $Name"}
    $worker=[pscustomobject]@{Name=$Name;Kind=$Kind;Process=$p;ProcessId=$p.Id;BirthTicks=$p.StartTime.ToUniversalTime().Ticks;Exe=$exe;Stdout=$p.StandardOutput.ReadToEndAsync();Stderr=$p.StandardError.ReadToEndAsync()}
    $workers.Add($worker)
    Wait-Fair {Test-Path -LiteralPath (Worker-Path $worker '.started.json')} "Worker $Name did not start"
    return $worker
}
function Wait-Registered {param($Worker) Wait-Fair {Test-Path -LiteralPath (Worker-Path $Worker '.registered.json')} "Worker $($Worker.Name) did not register"}
function Release-FairWorker {param($Worker) [IO.File]::WriteAllText((Worker-Path $Worker '.release'),'release',$utf8)}
function Wait-FairExit {param($Worker,[int]$Expected=0) Assert-Fair ($Worker.Process.WaitForExit(5000)) "Worker exit timeout: $($Worker.Name)";Assert-Fair ($Worker.Process.ExitCode -eq $Expected) "Unexpected exit $($Worker.Process.ExitCode): $($Worker.Name); $($Worker.Stderr.GetAwaiter().GetResult())"}
function Stop-OwnedFairWorker {
    param($Worker)
    if($Worker.Process.HasExited){return}
    Assert-Fair ($Worker.Process.Id -eq $Worker.ProcessId -and $Worker.Process.StartTime.ToUniversalTime().Ticks -eq $Worker.BirthTicks -and [string]::Equals($Worker.Process.MainModule.FileName,$Worker.Exe,[StringComparison]::OrdinalIgnoreCase)) 'Refusing to terminate a non-owned/reused worker PID.'
    $birth=Read-UESTCFairJson (Worker-Path $Worker '.started.json')
    Assert-Fair ($birth.processId -eq $Worker.ProcessId -and [long]$birth.birthTicks -eq $Worker.BirthTicks) 'Worker birth record mismatch.'
    $Worker.Process.Kill();Assert-Fair ($Worker.Process.WaitForExit(5000)) 'Owned worker did not terminate.'
}
function New-FairRoot {param([string]$Name) $p=Join-Path $work $Name;$null=[IO.Directory]::CreateDirectory($p);return $p}
function Hold-Slots {param([string]$Locks,[string]$Kind,[int]$Count) $list=New-Object 'Collections.Generic.List[object]';for($i=0;$i -lt $Count;$i++){ $h=Open-UESTCResourceSlot $Locks $Kind $Count;if(-not $h){throw 'Fake slot acquisition failed'};$list.Add($h);$holds.Add($h)};return ,$list}
function Drop-Slots {param($List) foreach($h in $List){$h.Dispose()}}
function Case-Passed {param([string]$Name,$Evidence) $cases.Add([pscustomobject]@{name=$Name;status='Passed';evidence=$Evidence})}

try {
    # Composite queues first while GPU is occupied. Later CPU work cannot steal the sole free CPU slot.
    $locks=New-FairRoot 'composite-fairness';$cpu=Hold-Slots $locks Cpu ($cpuLimit-1);$gpu=Hold-Slots $locks GpuShared 2
    $compound=Start-FairWorker 'compound-first' $locks CpuGpuShared;Wait-Registered $compound
    $later=@();for($i=0;$i -lt 4;$i++){ $w=Start-FairWorker ('cpu-later-'+$i) $locks Cpu;Wait-Registered $w;$later+=,$w }
    Start-Sleep -Milliseconds 200
    Assert-Fair (-not (Has-Acquired $compound) -and @($later | Where-Object {Has-Acquired $_}).Count -eq 0) 'A later CPU request bypassed the blocked composite.'
    $probe=Open-UESTCResourceSlot $locks Cpu $cpuLimit;Assert-Fair ($null -ne $probe) 'Composite retained a partial CPU slot while waiting for GPU';$probe.Dispose()
    Drop-Slots $gpu
    Wait-Fair {Has-Acquired $compound} 'Composite did not acquire the first available CPU/GPU resources'
    Assert-Fair (@($later | Where-Object {Has-Acquired $_}).Count -eq 0) 'Later CPU overtook the earliest composite'
    Release-FairWorker $compound;Wait-FairExit $compound;Drop-Slots $cpu
    foreach($w in $later){Wait-Fair {Has-Acquired $w} 'Later CPU never resumed';Release-FairWorker $w;Wait-FairExit $w}
    Case-Passed 'composite-precedes-continuous-later-CPU-with-no-partial-hold' @{laterCpuRequests=4;cpuLimit=$cpuLimit}

    # Pause between registration and admission: publication alone must reserve order.
    $locks=New-FairRoot 'registration-gap';$cpu=Hold-Slots $locks Cpu ($cpuLimit-1);$gpu=Hold-Slots $locks GpuShared 1
    $paused=Start-FairWorker 'paused-composite' $locks CpuGpuShared 20 PauseAfterRegister;Wait-Registered $paused
    $lateCpu=Start-FairWorker 'gap-late-cpu' $locks Cpu;Wait-Registered $lateCpu
    $lateGpu=Start-FairWorker 'gap-late-gpu' $locks GpuShared;Wait-Registered $lateGpu
    Start-Sleep -Milliseconds 250
    Assert-Fair (-not (Has-Acquired $lateCpu) -and -not (Has-Acquired $lateGpu)) 'Admission bypassed a published ticket before its caller reached admission.'
    [IO.File]::WriteAllText((Worker-Path $paused '.admit'),'admit',$utf8)
    Wait-Fair {Has-Acquired $paused} 'Paused composite did not acquire after resuming'
    Assert-Fair (-not (Has-Acquired $lateCpu) -and -not (Has-Acquired $lateGpu)) 'Later work stole reserved capacity'
    Release-FairWorker $paused;Wait-FairExit $paused
    foreach($w in @($lateCpu,$lateGpu)){Wait-Fair {Has-Acquired $w} 'Gap waiter did not resume';Release-FairWorker $w;Wait-FairExit $w}
    Drop-Slots $cpu;Drop-Slots $gpu
    Case-Passed 'published-ticket-blocks-before-caller-reaches-admission' $true

    $locks=New-FairRoot 'independent';$cpu=Hold-Slots $locks Cpu $cpuLimit
    $early=Start-FairWorker 'blocked-cpu' $locks Cpu;Wait-Registered $early
    $independent=Start-FairWorker 'independent-gpu' $locks GpuShared;Wait-Registered $independent
    Wait-Fair {Has-Acquired $independent} 'Nonintersecting GPU was blocked by CPU queue';Assert-Fair (-not (Has-Acquired $early)) 'Fake CPU slots did not block CPU'
    Release-FairWorker $independent;Wait-FairExit $independent;Drop-Slots $cpu;Wait-Fair {Has-Acquired $early} 'CPU did not resume';Release-FairWorker $early;Wait-FairExit $early
    Case-Passed 'nonintersecting-resource-can-pass' $true

    $locks=New-FairRoot 'exclusive-order';$held=Enter-UESTCResourceLock $locks CpuGpuShared;$holds.AddRange([object[]]$held.Handles)
    $one=Start-FairWorker 'exclusive-one' $locks GpuExclusive;Wait-Registered $one
    Wait-Fair {Test-UESTCFairWriterWaiting $locks} 'Eligible Exclusive did not publish compatible intent'
    $two=Start-FairWorker 'exclusive-two' $locks GpuExclusive;Wait-Registered $two
    $late=Start-FairWorker 'exclusive-late-cpu' $locks Cpu;Wait-Registered $late
    Assert-Fair (-not (Has-Acquired $one) -and -not (Has-Acquired $two) -and -not (Has-Acquired $late)) 'Exclusive failed to wait for existing work'
    Exit-UESTCResourceLock $held
    Wait-Fair {Has-Acquired $one} 'First Exclusive did not drain';Assert-Fair (-not (Has-Acquired $two) -and -not (Has-Acquired $late)) 'Exclusive ticket order violated'
    Release-FairWorker $one;Wait-FairExit $one;Wait-Fair {Has-Acquired $two} 'Second Exclusive did not acquire';Assert-Fair (-not (Has-Acquired $late)) 'Late CPU bypassed second Exclusive'
    Release-FairWorker $two;Wait-FairExit $two;Wait-Fair {Has-Acquired $late} 'CPU did not resume after Exclusive';Release-FairWorker $late;Wait-FairExit $late
    Case-Passed 'exclusive-drains-and-registration-remains-independent' $true

    $locks=New-FairRoot 'cancel';$held=Enter-UESTCResourceLock $locks GpuShared;$holds.AddRange([object[]]$held.Handles)
    $cancel=Start-FairWorker 'cancel-exclusive' $locks GpuExclusive 1;Wait-Registered $cancel;Wait-FairExit $cancel 1
    Assert-Fair (-not (Test-UESTCFairWriterWaiting $locks)) 'Timed-out Exclusive intent survived'
    $probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Exit-UESTCResourceLock $probe;Exit-UESTCResourceLock $held
    Case-Passed 'timeout-cancels-ticket-intent-and-admission' $true

    $locks=New-FairRoot 'crash';$held=Enter-UESTCResourceLock $locks GpuShared;$holds.AddRange([object[]]$held.Handles)
    $crash=Start-FairWorker 'crash-waiting' $locks GpuExclusive;Wait-Registered $crash;Stop-OwnedFairWorker $crash
    Wait-Fair {-not (Test-UESTCFairWriterWaiting $locks)} 'Crashed waiting intent survived'
    $probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Exit-UESTCResourceLock $probe;Exit-UESTCResourceLock $held
    $active=Start-FairWorker 'crash-active' $locks GpuExclusive;Wait-Fair {Has-Acquired $active} 'Active crash worker did not acquire';Stop-OwnedFairWorker $active
    $probe=Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2;Exit-UESTCResourceLock $probe
    Case-Passed 'waiting-and-active-crash-release-with-owned-birth-check' $true

    $locks=New-FairRoot 'exceptions'
    $fault=Start-FairWorker 'fault-second-slot' $locks CpuGpuShared 10 ThrowSecondSlot;Wait-FairExit $fault 1
    $probe=Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2;Exit-UESTCResourceLock $probe
    $fault=Start-FairWorker 'fault-after-acquire' $locks CpuGpuShared 10 ThrowAfterAcquire;Wait-FairExit $fault 1
    $probe=Enter-UESTCResourceLock $locks CpuGpuShared -TimeoutSeconds 2;Exit-UESTCResourceLock $probe
    Case-Passed 'second-slot-and-caller-exception-release-all-handles' $true

    $locks=New-FairRoot 'capacity';$many=@()
    for($i=0;$i -lt $cpuLimit+2;$i++){ $w=Start-FairWorker ('capacity-'+$i) $locks Cpu;Wait-Registered $w;$many+=,$w }
    Wait-Fair {@($many | Where-Object {Has-Acquired $_}).Count -eq $cpuLimit} 'CPU capacity was not filled'
    Assert-Fair (@($many | Where-Object {Has-Acquired $_}).Count -eq $cpuLimit) 'CPU capacity was exceeded'
    for($i=0;$i -lt $cpuLimit;$i++){Assert-Fair (Has-Acquired $many[$i]) 'Initial CPU ticket was bypassed'}
    foreach($w in $many){Wait-Fair {Has-Acquired $w} 'Capacity waiter starved';Release-FairWorker $w;Wait-FairExit $w}
    Case-Passed 'CPU-capacity-and-pending-ticket-order' $cpuLimit

    # All workers are alive before a common release, so registration actually contends.
    $locks=New-FairRoot 'concurrent-gpu';$gpu=Hold-Slots $locks GpuShared 2
    $startGate=Join-Path $work 'gpu-burst.start';$burst=@()
    for($i=0;$i -lt 6;$i++){$burst+=,(Start-FairWorker ('gpu-burst-'+$i) $locks GpuShared -StartGate $startGate)}
    [IO.File]::WriteAllText($startGate,'start',$utf8)
    foreach($w in $burst){Wait-Registered $w}
    $ordered=@($burst | Sort-Object { [UInt64](Read-UESTCFairJson (Worker-Path $_ '.registered.json')).ticket })
    $tickets=@($ordered | ForEach-Object { (Read-UESTCFairJson (Worker-Path $_ '.registered.json')).ticket })
    Assert-Fair (@($tickets | Select-Object -Unique).Count -eq 6) 'Concurrent registration issued duplicate tickets'
    Assert-Fair ($tickets[0] -ceq '00000000000000000001' -and $tickets[5] -ceq '00000000000000000006') 'Concurrent ticket sequence has a gap'
    Drop-Slots $gpu
    for($i=0;$i -lt 6;$i++){
        $expected=[Math]::Min(6,$i+2)
        Wait-Fair {@($ordered | Where-Object {Has-Acquired $_}).Count -ge $expected} 'Shared GPU pending ticket did not resume'
        Start-Sleep -Milliseconds 150
        Assert-Fair (@($ordered | Where-Object {Has-Acquired $_}).Count -eq $expected) 'GPU admission exceeded two live leases'
        for($j=0;$j -lt $expected;$j++){Assert-Fair (Has-Acquired $ordered[$j]) 'GPU acquisition skipped a ticket'}
        Release-FairWorker $ordered[$i];Wait-FairExit $ordered[$i]
    }
    Case-Passed 'concurrent-registration-unique-tickets-GPU-capacity-and-order' @{tickets=$tickets;workersInBurst=6;gpuLimit=2}

    if ($LegacyCommon) {
    $locks=New-FairRoot 'legacy';$held=Enter-UESTCResourceLock $locks GpuShared;$holds.AddRange([object[]]$held.Handles)
    $old=Start-FairWorker 'legacy-exclusive' $locks GpuExclusive 20 Hold -Legacy
    Wait-Fair {Test-UESTCFairWriterWaiting $locks} 'Legacy Exclusive did not publish intent'
    $new=Start-FairWorker 'new-after-legacy' $locks Cpu;Wait-Registered $new
    Assert-Fair (-not (Has-Acquired $new)) 'New CPU ignored legacy Exclusive intent'
    Exit-UESTCResourceLock $held;Wait-Fair {Has-Acquired $old} 'Legacy Exclusive did not drain';Assert-Fair (-not (Has-Acquired $new)) 'New CPU entered active legacy Exclusive'
    Release-FairWorker $old;Wait-FairExit $old;Wait-Fair {Has-Acquired $new} 'New CPU did not resume';Release-FairWorker $new;Wait-FairExit $new
    Case-Passed 'legacy-Exclusive-intent-is-respected' $true

    $locks=New-FairRoot 'legacy-readers'
    $old=Start-FairWorker 'legacy-active-shared' $locks CpuGpuShared -Legacy;Wait-Fair {Has-Acquired $old} 'Legacy shared did not acquire'
    $new=Start-FairWorker 'new-exclusive-drain' $locks GpuExclusive;Wait-Registered $new
    Wait-Fair {Test-UESTCFairWriterWaiting $locks} 'New Exclusive did not publish legacy-compatible intent'
    $lateCpu=Start-FairWorker 'legacy-late-cpu' $locks Cpu -Legacy
    $lateGpu=Start-FairWorker 'legacy-late-gpu' $locks GpuShared -Legacy
    Assert-Fair (-not (Has-Acquired $new) -and -not (Has-Acquired $lateCpu) -and -not (Has-Acquired $lateGpu)) 'Mixed-version exclusive overlapped existing work'
    Release-FairWorker $old;Wait-FairExit $old;Wait-Fair {Has-Acquired $new} 'New Exclusive did not drain legacy work'
    Start-Sleep -Milliseconds 150
    Assert-Fair (-not (Has-Acquired $lateCpu) -and -not (Has-Acquired $lateGpu)) 'Legacy work entered new Exclusive'
    Release-FairWorker $new;Wait-FairExit $new
    foreach($w in @($lateCpu,$lateGpu)){Wait-Fair {Has-Acquired $w} 'Legacy shared did not resume';Release-FairWorker $w;Wait-FairExit $w}
    Case-Passed 'new-Exclusive-drains-old-shared-and-blocks-old-CPU-and-GPU' $true

    # Legacy arrivals have no ticket. Preserve safety, but document their fairness boundary.
    $locks=New-FairRoot 'legacy-fairness-boundary';$gpu=Hold-Slots $locks GpuShared 2
    $new=Start-FairWorker 'new-queued-composite' $locks CpuGpuShared;Wait-Registered $new
    $old=Start-FairWorker 'legacy-bypasses-queue' $locks Cpu -Legacy
    Wait-Fair {Has-Acquired $old} 'Legacy bypass boundary changed; revisit migration expectations'
    Assert-Fair (-not (Has-Acquired $new)) 'Occupied GPU did not hold composite'
    Release-FairWorker $old;Wait-FairExit $old;Drop-Slots $gpu
    Wait-Fair {Has-Acquired $new} 'Composite did not resume after legacy boundary demonstration';Release-FairWorker $new;Wait-FairExit $new
    Case-Passed 'migration-boundary-legacy-arrivals-do-not-obey-new-ticket-queue' @{legacyCpuBypassedQueuedComposite=$true;fairnessRequiresAllNewArrivalsUpgraded=$true}

    }

    $locks=New-FairRoot 'counter-high';$counter=Join-Path $locks 'fair-resource-counter.json';Write-UESTCFairCounterLocked $counter ([UInt64]9007199254740992)
    $probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Assert-Fair ($probe.Ticket -ceq '00009007199254740993') 'Ticket arithmetic lost UInt64 precision';Exit-UESTCResourceLock $probe
    Assert-Fair ([IO.File]::Exists($counter)) 'Published counter was deleted on close'
    Case-Passed 'atomic-counter-persists-and-exceeds-double-precision' $true

    $locks=New-FairRoot 'counter-bad';$counter=Join-Path $locks 'fair-resource-counter.json';[IO.File]::WriteAllText($counter,'broken-counter',$utf8)
    $rejected=$false;try{$probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Exit-UESTCResourceLock $probe}catch{$rejected=$true}
    Assert-Fair ($rejected -and [IO.File]::ReadAllText($counter) -ceq 'broken-counter') 'Bad counter was silently reset'
    Case-Passed 'malformed-counter-fails-without-reset' $true

    $locks=New-FairRoot 'stale';[IO.File]::WriteAllText((Join-Path $locks 'gpu-writer-stale.lock'),'stale',$utf8)
    $probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Exit-UESTCResourceLock $probe
    [IO.File]::WriteAllText((Join-Path $locks 'fair-resource-00000000000000000000.request.json'),'unlocked stale metadata',$utf8)
    [IO.File]::WriteAllText((Join-Path $locks 'fair-resource-counter.ignored.tmp'),'abandoned counter temporary',$utf8)
    $probe=Enter-UESTCResourceLock $locks Cpu -TimeoutSeconds 2;Exit-UESTCResourceLock $probe
    Assert-Fair ([IO.File]::Exists((Join-Path $locks 'fair-resource-counter.ignored.tmp'))) 'Other temporary file was deleted'
    Case-Passed 'unlocked-stale-and-temporary-files-ignored-not-deleted' $true

    $result=[ordered]@{status='Passed';scope='Independent output lock roots and owned PowerShell workers only';legacyChecks=([bool]$LegacyCommon);cpuLimit=$cpuLimit;gpuLimit=2;cases=@($cases.ToArray());workers=@($workers | ForEach-Object { @{name=$_.Name;processId=$_.ProcessId;birthTicks=$_.BirthTicks;exited=$_.Process.HasExited} });output=$work}
    [IO.File]::WriteAllText((Join-Path $work 'result.json'),($result | ConvertTo-Json -Depth 8),$utf8)
    Write-Output ("Passed {0} fairness/process cases; evidence {1}" -f $cases.Count,$work)
} finally {
    foreach($h in $holds){$h.Dispose()}
    foreach($w in $workers){try{if(-not $w.Process.HasExited){Stop-OwnedFairWorker $w}}finally{$w.Process.Dispose()}}
}
