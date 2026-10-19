# 固定机位截图：一个 Godot 进程按 poses.txt 拍完所选机位，地图只加载一次，再拼一张缩略总览。
# 用法：powershell -NoProfile -File fixed_shots.ps1 [-Only 名字,名字] [-Build]
#   -Build：先 dotnet build 与 --headless --import（代码改过、还没编译时用）
# 整轮持有构建锁（WF-BUILD），不开声音；出图在运行它的检出（主检出或工作树）的 output/shots/，覆盖上一轮，总览是 总览.jpg。
param([string[]]$Only = @(), [switch]$Build)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
$layout = Get-UESTCLayout $repo
$godot = Get-UESTCGodot
$out = Join-Path $repo 'output\shots'
$null = [IO.Directory]::CreateDirectory($out)
$poses = Join-Path $PSScriptRoot 'poses.txt'
$names = @(Get-Content -Encoding UTF8 $poses | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') } | ForEach-Object { ($_ -split '\s*\|\s*')[0].Trim() })
if ($Only.Count) {
    $Only = @($Only | ForEach-Object { $_ -split ',' })
    foreach ($name in $Only) { if ($name -notin $names) { throw "未知固定机位：$name" } }
    $names = @($names | Where-Object { $_ -in $Only })
}
$arguments = @('--path', "$repo\game", '--windowed', '--resolution', '1920x1080', '--rendering-driver', 'd3d12', '--fixed-fps', '60',
    '--', "--shots-file=$poses", "--out=$out", '--anim=240', '--flow=0', '--mode=fly', '--ui=0', '--seed=1', '--weather-seed=1',
    '--quality=fine', '--map=qingshuihe', '--title=0', '--save=off', '--audio=off')
if ($Only.Count) { $arguments += "--shots=$($names -join ',')" }
$lease = Enter-UESTCBuildLock $layout.Locks
try {
    # 拿到锁后清掉上一轮截图、日志和总览，整轮出图与拼图共用这把锁。
    Get-ChildItem -LiteralPath $out -File | Where-Object { $_.Extension -in '.png', '.log', '.jpg' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
    if ($Build) {
        Invoke-UESTCProcess 'dotnet' @('build', "$repo\game\Campus.sln", '-v', 'q') $repo (Join-Path $out 'build.log') | Out-Null
        Invoke-UESTCProcess $godot @('--headless', '--path', "$repo\game", '--import') $repo (Join-Path $out 'import.log') -TimeoutSeconds 600 | Out-Null
    }
    # 缓存失效时仍给首次烘焙留足时间；有缓存时按内容就绪抓取，不再先空跑 300 帧。
    try { $r = Invoke-UESTCProcess $godot $arguments $repo (Join-Path $out 'capture.log') -AllowFailure -Visible -TimeoutSeconds 900; $code = $r.ExitCode }
    catch { $code = '超时或启动失败' }
    foreach ($name in $names) {
        "{0}：{1}" -f $name, $(if (Test-Path -LiteralPath (Join-Path $out "$name.png")) { '出图' } else { "没出图（$code，日志 capture.log）" })
    }
    & python -I -X utf8 (Join-Path $PSScriptRoot 'contact_sheet.py') $out
} finally { Exit-UESTCBuildLock $lease }
