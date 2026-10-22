# 启动游戏：正式 Debug 程序集、已发布业务清单和原生导入缓存都存在即启动，不判新旧；缺失才构建并执行原生增量导入。
# 用法：双击根目录 启动游戏.cmd，或 启动游戏.cmd [游戏参数…]；游戏参数原样传递。
# 产出：output/start_game/；output/.locks/build.lock（工作区半成品不触发编译）。
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs = @())
$scriptEnteredTimestamp = [Diagnostics.Stopwatch]::GetTimestamp()
$scriptEnteredUtc = [DateTime]::UtcNow
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
. (Join-Path $repo 'scripts\lib\start-game.ps1')
$null = Invoke-UESTCStartGame -Root $repo -Godot (Get-UESTCGodot) -GameArgs $GameArgs -ScriptEnteredUtc $scriptEnteredUtc -ScriptEnteredTimestamp $scriptEnteredTimestamp
Write-Host '游戏已启动。'
