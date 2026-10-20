# 启动游戏：自判现有 Debug 构建与 Godot 导入，必要时更新，再后台打开正式开始页。
# 用法：双击根目录 启动游戏.cmd，或 启动游戏.cmd [游戏参数…]；游戏参数原样传递。
# 产出：output/start_game/；output/.locks/build.lock（按需构建与导入后启动游戏）。
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs = @())
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
. (Join-Path $repo 'scripts\lib\start-game.ps1')
$null = Invoke-UESTCStartGame -Root $repo -Godot (Get-UESTCGodot) -GameArgs $GameArgs
Write-Host '游戏已启动。'
