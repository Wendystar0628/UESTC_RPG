# 收口预烘：显式准备正式游戏构建与按需导入，再后台全图烘进同一份本机 CPU 缓存，全程持构建锁。
# 产出：output/prebake_game/、本机CPU烘焙缓存
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build\prebake-game.ps1
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
. (Join-Path $repo 'scripts\lib\start-game.ps1')
$layout = Get-UESTCLayout $repo
$godot = Get-UESTCGodot
$out = Join-Path $repo 'output\prebake_game'
$timer = [Diagnostics.Stopwatch]::StartNew()
$lease = $null
$code = 1
try {
    $lease = Enter-UESTCBuildLock $layout.Locks
    Update-UESTCGameBuild -Root $repo -Godot $godot -BuildLease $lease
    $result = Invoke-UESTCProcess $godot @('--headless', '--path', "$repo\game", '--', '--bake=all', '--map=*') $repo (Join-Path $out 'bake.log') -TimeoutSeconds 14400 -AllowFailure
    $code = $result.ExitCode
    foreach ($line in ($result.StdOut -split '\r?\n')) {
        if ($line -match '^(BAKE_MAP_COMPLETE|PREBAKE_COMPLETE) ') { Write-Host $line }
    }
    if ($code -ne 0) { Write-Error "全图预烘失败（退出码 $code），见 $out\bake.log" -ErrorAction Continue }
} catch {
    if ($_.Exception.Data.Contains('ExitCode')) { $code = [int]$_.Exception.Data['ExitCode'] }
    if ($code -eq 0) { $code = 1 }
    Write-Error $_ -ErrorAction Continue
} finally {
    Exit-UESTCBuildLock $lease
    $timer.Stop()
    Write-Host ('预烘入口结束：退出码 {0}，总用时 {1:F3} 秒。日志：{2}' -f $code, $timer.Elapsed.TotalSeconds, $out)
}
exit $code
