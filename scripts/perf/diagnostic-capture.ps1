# 在调用方的私桌面启动并持有一次诊断工具进程树，保留实际命令与输出。
# 用法：powershell -NoProfile -File scripts/perf/diagnostic-capture.ps1 -ConditionPath <condition.json>。
param([Parameter(Mandatory)][string]$ConditionPath)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $root 'scripts\lib\common.ps1')
$condition=Get-Content -LiteralPath $ConditionPath -Raw -Encoding UTF8 | ConvertFrom-Json
$work=Split-Path $ConditionPath -Parent
$principal=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Diagnostic collectors require an ordinary, non-elevated token.' }
$executable=$condition.engine
$arguments=@($condition.arguments)
switch ($condition.tool) {
    'EventPipe' {
        $executable=$condition.toolPath
        $arguments=@()
    }
    'Nsight' {
        $executable=$condition.toolPath
        # Windows CPU sampling/context-switch collection needs elevation. API tracing does not.
        $arguments=@('profile','--trace=dx12,vulkan,nvtx','--sample=none','--cpuctxsw=none','--wait=all','--kill=false',
            ('--output=' + (Join-Path $work 'graphics')),$condition.engine) + $arguments
    }
}
$record=[ordered]@{ executable=$executable; arguments=$arguments; tool=$condition.tool; elevated=$false;
    startedUtc=[DateTime]::UtcNow.ToString('O'); exitCode=$null; status='partial'; error=$null }
$gameTree=$null
$eventPipe=$null
$failure=$null
try {
    $version=Invoke-UESTCProcess $executable @('--version') $root (Join-Path $work 'tool-version.log') -TimeoutSeconds 30
    $record['versionOutput']=$version.Output.Trim()
    if ($condition.tool -eq 'EventPipe') {
        $profiles=Invoke-UESTCProcess $executable @('list-profiles') $root (Join-Path $work 'tool-profiles.log') -TimeoutSeconds 30
        $record['profileDescription']=$profiles.Output.Trim()
    }
    if ($condition.tool -eq 'EventPipe') {
        $deadline=[DateTime]::UtcNow.AddSeconds(600)
        $gameArguments=(@($condition.arguments) | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
        $record['gameExecutable']=$condition.engine
        $record['gameArguments']=@($condition.arguments)
        $record['gamePid']=$null
        $record['gameExitCode']=$null
        $record['gameExitUtc']=$null
        $record['sampleEndObservedUtc']=$null
        $record['windowHandle']=$null
        $record['closeRequestedUtc']=$null
        if ($condition.collection.externalCloseAfterSample -ne $true) {
            throw 'EventPipe requires the explicit collection owner to keep the target alive for collector Stop/rundown.'
        }
        # Both children inherit this worker's existing private desktop and profile.
        $gameTree=New-Object Uestc.Tools.ProcessTree($condition.engine,$gameArguments,$root,$false,[Text.Encoding]::UTF8,$false,$null)
        $record.gamePid=$gameTree.ProcessId
        # Boot publishes this line from managed _EnterTree, before collection preparation.
        while ($gameTree.StdOut -notmatch '(?m)^Storage profile: [^\r\n]+\r?\n') {
            if ($gameTree.HasExited) { throw 'Godot exited before publishing managed boot readiness.' }
            if ([DateTime]::UtcNow -ge $deadline) { throw 'Godot did not publish managed boot readiness within the capture lifetime.' }
            Start-Sleep -Milliseconds 100
        }
        $record['gameManagedReadyObservedUtc']=[DateTime]::UtcNow.ToString('O')
        $eventPipe=@{ Target=$gameTree; ToolPath=$executable; Root=$root; Output=$work; Record=$record; Deadline=$deadline }
        Invoke-UESTCEventPipeCapture -Action Start -Capture $eventPipe
        while ($true) {
            if ($gameTree.HasExited) { throw "Godot exited before the observed diagnostic sample End: $($gameTree.ExitCode)." }
            if ($eventPipe.Collector.HasExited) { throw "EventPipe collector exited before the diagnostic sample End: $($eventPipe.Collector.ExitCode)." }
            $results=[regex]::Matches($gameTree.StdOut,'(?m)^PERF_COLLECTION_RESULT ([^\r\n]+)\r?\n')
            if ($results.Count -gt 1) { throw 'Godot published duplicate collection results.' }
            if ($results.Count -eq 1) {
                $completion=$results[0].Groups[1].Value | ConvertFrom-Json
                if ($completion.sampleSegmentId -ne $condition.collection.sampleSegmentId -or $completion.scene -ne $condition.scene -or
                    $completion.coverage.sampleEnd.observed -ne 1) {
                    throw 'Godot did not publish the requested real sample End; original result retained.'
                }
                $record.sampleEndObservedUtc=[DateTime]::UtcNow.ToString('O')
                break
            }
            if ([DateTime]::UtcNow -ge $deadline) { throw 'Godot did not publish its real sample End within the capture lifetime.' }
            Start-Sleep -Milliseconds 100
        }
        $windows=[regex]::Matches($gameTree.StdOut,'(?m)^STARTUP_ENVIRONMENT [^\r\n]*\bwindowHandle=(\d+)(?:\s|$)')
        if (-not $windows.Count) { throw 'Godot did not publish its existing native window identity.' }
        $record.windowHandle=[long]::Parse($windows[$windows.Count-1].Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
        if ($record.windowHandle -le 0) { throw 'Godot published an invalid native window identity.' }
        Invoke-UESTCEventPipeCapture -Action Stop -Capture $eventPipe
        if ($gameTree.HasExited) { throw 'Godot exited before its formal window-close request.' }
        $record.closeRequestedUtc=[DateTime]::UtcNow.ToString('O')
        $record['closePostedTimestamp']=$gameTree.PostClose([IntPtr]$record.windowHandle)
        $closeDeadline=[DateTime]::UtcNow.AddSeconds(30)
        while (-not $gameTree.HasExited) {
            if ([DateTime]::UtcNow -ge $closeDeadline) { throw 'Godot did not finish its formal window-close shutdown within 30 seconds.' }
            Start-Sleep -Milliseconds 100
        }
        $record.gameExitCode=$gameTree.ExitCode
        $record.gameExitUtc=$gameTree.ProcessExitUtc.ToString('O')
        $gameTree.Finish()
        if ($record.exitCode -ne 0 -or $record.gameExitCode -ne 0) { throw 'EventPipe collector or Godot exited unsuccessfully.' }
        $gameResult=[pscustomobject]@{ Output=$gameTree.StdOut+$gameTree.StdErr; Log=(Join-Path $work 'game.log') }
        Assert-UESTCCleanGodotLog $gameResult
        if (-not $record.collectorReadyObservedUtc) { throw 'Collector did not publish a successful EventPipe session.' }
        $record.status='completed'
    } else {
        $result=Invoke-UESTCProcess $executable $arguments $root (Join-Path $work 'tool.log') -TimeoutSeconds 600 -AllowFailure -TextEncoding ([Text.Encoding]::UTF8)
        $record.exitCode=$result.ExitCode
        $record.status=if($result.ExitCode -eq 0){'completed'}else{'failed'}
        [IO.File]::WriteAllText((Join-Path $work 'tool.stdout.txt'),$result.StdOut)
        [IO.File]::WriteAllText((Join-Path $work 'tool.stderr.txt'),$result.StdErr)
        Assert-UESTCCleanGodotLog $result
        if ($record.status -eq 'completed' -and $condition.tool -eq 'Nsight') {
            $reports=@(Get-ChildItem -LiteralPath $work -File -Filter '*.nsys-rep')
            if ($reports.Count -ne 1) { throw 'Nsight did not publish exactly one native report.' }
            $sessions=@(Get-ChildItem -LiteralPath $work -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') })
            if ($sessions.Count -ne 1) { throw 'Nsight target did not publish exactly one closed recording.' }
            $manifest=Get-Content -LiteralPath (Join-Path $sessions[0].FullName 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
            $gpuReport=switch ($manifest.machine.api) {
                'd3d12' { 'dx12_gpu_marker_sum' }
                'vulkan' { 'vulkan_gpu_marker_sum' }
                default { throw "No native Nsight GPU range report is selected for the captured graphics API: $($manifest.machine.api)" }
            }
            # The installed 2025.6.3 native reports confirm measured GPU ranges, not API calls alone.
            $statsArguments=@('stats','--report',$gpuReport,'--format','csv',
                '--timeunit','nanoseconds','--force-export=true','--output',(Join-Path $work 'nsight-gpu'),$reports[0].FullName)
            $record['nsightStatsArguments']=$statsArguments
            $stats=Invoke-UESTCProcess $executable $statsArguments $root (Join-Path $work 'nsight-stats.log') `
                -TimeoutSeconds 120 -AllowFailure -TextEncoding ([Text.Encoding]::UTF8)
            $record['nsightStatsExitCode']=$stats.ExitCode
            $record['nsightGpuReports']=@(('nsight-gpu_' + $gpuReport + '.csv') |
                Where-Object { Test-Path -LiteralPath (Join-Path $work $_) -PathType Leaf })
            [IO.File]::WriteAllText((Join-Path $work 'nsight-stats.stdout.txt'),$stats.StdOut)
            [IO.File]::WriteAllText((Join-Path $work 'nsight-stats.stderr.txt'),$stats.StdErr)
            if ($stats.ExitCode -ne 0) { throw "Native Nsight stats failed with exit $($stats.ExitCode). See nsight-stats.log." }
        }
    }
    if ($record.status -eq 'completed' -and $condition.tool -eq 'EventPipe') {
        $sessions=@(Get-ChildItem -LiteralPath $work -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') })
        if ($sessions.Count -ne 1) { throw 'EventPipe target did not publish exactly one closed performance session.' }
        $parsed=Invoke-UESTCProcess dotnet @($condition.parserDll,'--perf-session',$work,$sessions[0].FullName) $root `
            (Join-Path $work 'cpu-parser.log') -TimeoutSeconds 120
        $record['cpuParserExitCode']=$parsed.ExitCode
    }
} catch {
    $failure=$_
    $record.status='failed'
    $record.error=$_.Exception.ToString()
}
finally {
    # Try every owned cleanup/evidence step; preserve the primary exception object.
    $cleanup=[ordered]@{
        Collector={ if ($eventPipe) { Invoke-UESTCEventPipeCapture -Action Dispose -Capture $eventPipe } }
        GameExit={ if ($gameTree -and $gameTree.HasExited) {
            $record['gameExitCode']=$gameTree.ExitCode
            $record['gameExitUtc']=$gameTree.ProcessExitUtc.ToString('O')
        } }
        GameDrain={ if ($gameTree) { $gameTree.Finish() } }
        GameStdOut={ if ($gameTree) { [IO.File]::WriteAllText((Join-Path $work 'game.stdout.txt'),$gameTree.StdOut) } }
        GameStdErr={ if ($gameTree) { [IO.File]::WriteAllText((Join-Path $work 'game.stderr.txt'),$gameTree.StdErr) } }
        GameLog={ if ($gameTree) { [IO.File]::WriteAllText((Join-Path $work 'game.log'),($gameTree.StdOut+$gameTree.StdErr)) } }
        CollectorStdOut={ if ($eventPipe -and $eventPipe.Collector) { [IO.File]::WriteAllText((Join-Path $work 'tool.stdout.txt'),$eventPipe.Collector.StdOut) } }
        CollectorStdErr={ if ($eventPipe -and $eventPipe.Collector) { [IO.File]::WriteAllText((Join-Path $work 'tool.stderr.txt'),$eventPipe.Collector.StdErr) } }
        CollectorLog={ if ($eventPipe -and $eventPipe.Collector) { [IO.File]::WriteAllText((Join-Path $work 'tool.log'),($eventPipe.Collector.StdOut+$eventPipe.Collector.StdErr)) } }
        GameOwner={ if ($gameTree) { $gameTree.Dispose() } }
    }
    foreach ($step in $cleanup.GetEnumerator()) {
        try { & $step.Value }
        catch {
            $record.status='failed'
            if (-not $record.Contains('cleanupErrors')) { $record['cleanupErrors']=@() }
            $record.cleanupErrors+=@([ordered]@{ step=$step.Key; error=$_.Exception.ToString() })
            if ($failure) { $failure.Exception.Data["DiagnosticCapture.$($step.Key)"]=$_.Exception }
            else { $failure=$_ }
        }
    }
    $record['endedUtc']=[DateTime]::UtcNow.ToString('O')
    try { [IO.File]::WriteAllText((Join-Path $work 'capture-result.json'),($record | ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding $false)) }
    catch {
        if ($failure) { $failure.Exception.Data['DiagnosticCapture.ResultWrite']=$_.Exception }
        else { $failure=$_ }
    }
}
if ($failure) { throw $failure }
if($record.status -ne 'completed'){exit 1}
