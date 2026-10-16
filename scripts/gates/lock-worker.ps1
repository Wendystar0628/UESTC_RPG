param([string]$Common,[string]$Locks,[string]$Name,[string]$Output,[string]$Kind,[int]$TimeoutSeconds=20,[string]$Mode='Hold',[string]$StartGate='')
$ErrorActionPreference='Stop'
. $Common
$self=[Diagnostics.Process]::GetCurrentProcess()
$utf8=New-Object Text.UTF8Encoding $false
function Save-WorkerJson { param([string]$Suffix,$Value) [IO.File]::WriteAllText((Join-Path $Output ($Name+$Suffix)),($Value | ConvertTo-Json -Depth 5 -Compress),$utf8) }
Save-WorkerJson '.started.json' @{processId=$PID;birthTicks=$self.StartTime.ToUniversalTime().Ticks;kind=$Kind}
$registerCommand=Get-Command New-UESTCFairRequest -ErrorAction SilentlyContinue
$script:originalRegister=if($registerCommand){$registerCommand.ScriptBlock}else{$null}
if ($script:originalRegister) {
    function New-UESTCFairRequest {
        param([string]$LockRoot,[string]$Kind,[Diagnostics.Stopwatch]$Timer,[double]$TimeoutMilliseconds)
        $r=& $script:originalRegister $LockRoot $Kind $Timer $TimeoutMilliseconds
        try { Save-WorkerJson '.registered.json' @{ticket=$r.TicketText;processId=$PID;path=$r.Path;kind=$Kind} }
        catch { $r.Handle.Dispose(); throw }
        if ($Mode -eq 'PauseAfterRegister') {
            try {
                $pauseClock=[Diagnostics.Stopwatch]::StartNew()
                while (-not [IO.File]::Exists((Join-Path $Output ($Name+'.admit')))) {
                    if ($pauseClock.Elapsed.TotalSeconds -gt 20) { throw 'RegistrationPauseDeadline' }
                    Start-Sleep -Milliseconds 10
                }
            } catch { $r.Handle.Dispose(); throw }
        }
        return $r
    }
}
if ($Mode -eq 'ThrowSecondSlot') {
    $script:originalSlot=(Get-Command Open-UESTCResourceSlot).ScriptBlock
    function Open-UESTCResourceSlot { param([string]$LockRoot,[string]$Kind,[int]$Limit) if($Kind -eq 'GpuShared') {throw 'IntentionalSecondSlotFailure'}; return & $script:originalSlot $LockRoot $Kind $Limit }
}
$lease=$null; $code=0
try {
    if ($StartGate) {
        $startClock=[Diagnostics.Stopwatch]::StartNew()
        while (-not [IO.File]::Exists($StartGate)) {
            if ($startClock.Elapsed.TotalSeconds -gt 20) { throw 'WorkerStartGateDeadline' }
            Start-Sleep -Milliseconds 10
        }
    }
    $lease=Enter-UESTCResourceLock $Locks $Kind -TimeoutSeconds $TimeoutSeconds
    $ticket=if($lease.PSObject.Properties['Ticket']){$lease.Ticket}else{'legacy'}
    Save-WorkerJson '.acquired.json' @{ticket=$ticket;processId=$PID;birthTicks=$self.StartTime.ToUniversalTime().Ticks;kind=$Kind}
    if($Mode -eq 'ThrowAfterAcquire'){throw 'IntentionalAfterAcquireFailure'}
    $hold=[Diagnostics.Stopwatch]::StartNew()
    while(-not (Test-Path -LiteralPath (Join-Path $Output ($Name+'.release')))) {if($hold.ElapsedMilliseconds -gt 60000){throw 'WorkerHoldSafetyDeadline'};Start-Sleep -Milliseconds 20}
} catch { $code=1; Save-WorkerJson '.error.json' @{error=$_.Exception.Message;kind=$Kind;processId=$PID} }
finally { Exit-UESTCResourceLock $lease; Save-WorkerJson '.exited.json' @{code=$code;processId=$PID};$self.Dispose() }
exit $code
