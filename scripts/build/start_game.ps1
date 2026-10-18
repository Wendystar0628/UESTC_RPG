# 启动游戏：先编译与导入（经构建锁，WF-BUILD §4），再用窗口版引擎打开正式入口（开始页）。
# 用法：双击根目录 启动游戏.cmd；命令行可写 启动游戏.cmd [-NoBuild] [游戏参数…]，参数原样转到这里
#   -NoBuild：代码没改过时跳过编译与导入，直接启动
#   游戏参数照 BootArguments 原样传给游戏，例如 --title=0（跳过开始页直接进漫游）
# 编译与导入日志在 output\start_game\；游戏在后台运行，脚本启动后即返回。
param([switch]$NoBuild, [Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs = @())
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
$layout = Get-UESTCLayout $repo
$godot = Get-UESTCGodot
$out = Join-Path $repo 'output\start_game'

if (-not $NoBuild) {
    Write-Host '编译与导入中……'
    $lease = Enter-UESTCBuildLock $layout.Locks
    try {
        Invoke-UESTCProcess 'dotnet' @('build', "$repo\game\Campus.csproj", '-v', 'q') $repo (Join-Path $out 'build.log') | Out-Null
        Invoke-UESTCProcess $godot @('--headless', '--path', "$repo\game", '--import') $repo (Join-Path $out 'import.log') -TimeoutSeconds 600 | Out-Null
    } finally { Exit-UESTCBuildLock $lease }
}

# 玩用窗口版引擎（不带控制台窗口）；没有就用配置里的那个
$window = $godot -replace '_console\.exe$', '.exe'
if (-not (Test-Path -LiteralPath $window)) { $window = $godot }
$arguments = @('--path', "`"$repo\game`"")
if ($GameArgs.Count) { $arguments += @('--') + $GameArgs }
Start-Process -FilePath $window -ArgumentList $arguments -WorkingDirectory $repo
Write-Host '游戏已启动。'
