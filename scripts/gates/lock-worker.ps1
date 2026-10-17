param([string]$Common,[string]$Locks,[string]$Name,[string]$Output,[int]$TimeoutSeconds=20,[string]$Mode='Hold',[string]$Repository='')
$ErrorActionPreference='Stop'
. $Common
if ($Repository) { $Locks = (Get-UESTCLayout $Repository).Locks }
$self=[Diagnostics.Process]::GetCurrentProcess()
$utf8=New-Object Text.UTF8Encoding $false
function Save-WorkerJson { param([string]$Suffix,$Value) [IO.File]::WriteAllText((Join-Path $Output ($Name+$Suffix)),($Value | ConvertTo-Json -Depth 5 -Compress),$utf8) }
Save-WorkerJson '.started.json' @{processId=$PID;birthTicks=$self.StartTime.ToUniversalTime().Ticks;locks=$Locks}
$lease=$null; $code=0
try {
    $lease=Enter-UESTCBuildLock $Locks -TimeoutSeconds $TimeoutSeconds
    Save-WorkerJson '.acquired.json' @{processId=$PID;locks=$Locks}
    if($Mode -eq 'ThrowAfterAcquire'){throw 'IntentionalAfterAcquireFailure'}
    $hold=[Diagnostics.Stopwatch]::StartNew()
    while(-not (Test-Path -LiteralPath (Join-Path $Output ($Name+'.release')))) {
        if($hold.ElapsedMilliseconds -gt 60000){throw 'WorkerHoldSafetyDeadline'}
        Start-Sleep -Milliseconds 20
    }
} catch { $code=1; Save-WorkerJson '.error.json' @{error=$_.Exception.Message;processId=$PID} }
finally { Exit-UESTCBuildLock $lease; Save-WorkerJson '.exited.json' @{code=$code;processId=$PID};$self.Dispose() }
exit $code
