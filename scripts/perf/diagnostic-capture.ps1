﻿# EventPipe 诊断的私桌面工作进程：启动游戏，样段前附着收集器，样段结束后在游戏存活时停收集器，再正式关窗。
# 用法：由 diagnostic-run.ps1 以 powershell -NoProfile -File scripts/perf/diagnostic-capture.ps1 -Work <诊断目录> 调用，读该目录 launch.json。
param([Parameter(Mandatory)][string]$Work)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $root 'scripts\lib\common.ps1')
$launch=Get-Content -LiteralPath (Join-Path $Work 'launch.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not ('Uestc.Tools.ProcessTree' -as [type])) { Add-Type -Path (Join-Path $root 'scripts\lib\ProcessTree.cs') }
$deadline=[DateTime]::UtcNow.AddSeconds(600)
$line=(@($launch.arguments) | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
# The game and collector inherit this worker's private desktop and profile.
$game=New-Object Uestc.Tools.ProcessTree($launch.engine,$line,$root,$false,[Text.Encoding]::UTF8,$false,$null)
$eventPipe=$null
try {
    # Boot publishes this line from managed _EnterTree, before collection preparation.
    while ($game.StdOut -notmatch '(?m)^Storage profile: [^\r\n]+\r?\n') {
        if ($game.HasExited) { throw 'Godot exited before publishing managed boot readiness.' }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Godot did not publish managed boot readiness in time.' }
        Start-Sleep -Milliseconds 100
    }
    $eventPipe=@{ Target=$game; ToolPath=$launch.traceTool; Root=$root; Output=$Work; Record=[ordered]@{}; Deadline=$deadline }
    Invoke-UESTCEventPipeCapture -Action Start -Capture $eventPipe
    while ($game.StdOut -notmatch '(?m)^PERF_COLLECTION_RESULT [^\r\n]+\r?\n') {
        if ($game.HasExited) { throw "Godot exited before its sample End: $($game.ExitCode)." }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Godot did not finish its sample in time.' }
        Start-Sleep -Milliseconds 100
    }
    $windows=[regex]::Matches($game.StdOut,'(?m)^STARTUP_ENVIRONMENT [^\r\n]*\bwindowHandle=(\d+)(?:\s|$)')
    if (-not $windows.Count) { throw 'Godot did not publish its native window identity.' }
    Invoke-UESTCEventPipeCapture -Action Stop -Capture $eventPipe
    $null=$game.PostClose([IntPtr][long]::Parse($windows[$windows.Count-1].Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture))
    $closeDeadline=[DateTime]::UtcNow.AddSeconds(30)
    while (-not $game.HasExited) {
        if ([DateTime]::UtcNow -ge $closeDeadline) { throw 'Godot did not finish its window-close shutdown within 30 seconds.' }
        Start-Sleep -Milliseconds 100
    }
    if ($game.ExitCode -ne 0) { throw "Godot exited unsuccessfully: $($game.ExitCode)." }
} finally {
    try { if ($eventPipe) { Invoke-UESTCEventPipeCapture -Action Dispose -Capture $eventPipe } }
    finally {
        $game.Finish()
        [IO.File]::WriteAllText((Join-Path $Work 'game.log'),($game.StdOut + $game.StdErr),(New-Object Text.UTF8Encoding $false))
        $game.Dispose()
    }
}
