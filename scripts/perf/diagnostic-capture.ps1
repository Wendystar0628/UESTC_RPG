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
$toolTree=$null
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
        $record['toolPid']=$null
        $record['collectorReadyObservedUtc']=$null
        $record['toolExitUtc']=$null
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
        $arguments=@('collect','--process-id',[string]$gameTree.ProcessId,'--profile','dotnet-sampled-thread-time',
            '--providers','Microsoft-Windows-DotNETRuntime:0x41080001:5','--output',(Join-Path $work 'threads.nettrace'))
        $record.arguments=$arguments
        $toolArguments=($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
        $toolTree=New-Object Uestc.Tools.ProcessTree($executable,$toolArguments,$root,$false,[Text.Encoding]::UTF8,$false,$null)
        $record.toolPid=$toolTree.ProcessId
        while (-not $gameTree.HasExited -or -not $toolTree.HasExited) {
            if (-not $record.collectorReadyObservedUtc -and $toolTree.StdOut -match '(?m)^Output File\s*:\s*[^\r\n]+\r?\n') {
                # The collector prints this only after StartEventPipeSession succeeds.
                $record.collectorReadyObservedUtc=[DateTime]::UtcNow.ToString('O')
            }
            if ($gameTree.HasExited -and $gameTree.ExitCode -ne 0) { throw "Godot exited with code $($gameTree.ExitCode)." }
            if ($toolTree.HasExited -and $toolTree.ExitCode -ne 0) { throw "EventPipe collector exited with code $($toolTree.ExitCode)." }
            if ([DateTime]::UtcNow -ge $deadline) { throw 'EventPipe/game did not complete within the capture lifetime.' }
            Start-Sleep -Milliseconds 100
        }
        $gameTree.Finish()
        $toolTree.Finish()
        $record.exitCode=$toolTree.ExitCode
        $record.gameExitCode=$gameTree.ExitCode
        $record.toolExitUtc=$toolTree.ProcessExitUtc.ToString('O')
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
    }
    if ($record.status -eq 'completed' -and $condition.tool -eq 'EventPipe') {
        $sessions=@(Get-ChildItem -LiteralPath $work -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') })
        if ($sessions.Count -ne 1) { throw 'EventPipe target did not publish exactly one closed performance session.' }
        $parsed=Invoke-UESTCProcess dotnet @($condition.parserDll,'--perf-session',$work,$sessions[0].FullName) $root `
            (Join-Path $work 'cpu-parser.log') -TimeoutSeconds 120
        $record['cpuParserExitCode']=$parsed.ExitCode
    }
} catch { $record.status='failed'; $record.error=$_.Exception.ToString() }
finally {
    foreach ($owned in @(@{tree=$toolTree;name='tool'},@{tree=$gameTree;name='game'})) {
        if (-not $owned.tree) { continue }
        try {
            # Read natural exit status before cleanup can terminate an unfinished tree.
            if ($owned.tree.HasExited) {
                if ($owned.name -eq 'tool') {
                    $record.exitCode=$owned.tree.ExitCode
                    $record.toolExitUtc=$owned.tree.ProcessExitUtc.ToString('O')
                } else { $record.gameExitCode=$owned.tree.ExitCode }
            }
            $owned.tree.Finish()
        } catch {
            $record.status='failed'
            if (-not $record.Contains('cleanupErrors')) { $record['cleanupErrors']=@() }
            $record.cleanupErrors+=@($_.Exception.ToString())
        } finally {
            try {
                [IO.File]::WriteAllText((Join-Path $work ($owned.name+'.stdout.txt')),$owned.tree.StdOut)
                [IO.File]::WriteAllText((Join-Path $work ($owned.name+'.stderr.txt')),$owned.tree.StdErr)
                [IO.File]::WriteAllText((Join-Path $work ($owned.name+'.log')),($owned.tree.StdOut+$owned.tree.StdErr))
            } catch {
                $record.status='failed'
                if (-not $record.Contains('cleanupErrors')) { $record['cleanupErrors']=@() }
                $record.cleanupErrors+=@($_.Exception.ToString())
            } finally {
                try { $owned.tree.Dispose() } catch {
                    $record.status='failed'
                    if (-not $record.Contains('cleanupErrors')) { $record['cleanupErrors']=@() }
                    $record.cleanupErrors+=@($_.Exception.ToString())
                }
            }
        }
    }
    $record['endedUtc']=[DateTime]::UtcNow.ToString('O')
    [IO.File]::WriteAllText((Join-Path $work 'capture-result.json'),($record | ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding $false))
}
if($record.status -ne 'completed'){exit 1}
