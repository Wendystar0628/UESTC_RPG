# 固定机位截图：在主检出按 poses.txt 一个机位一个进程出图，拼一张缩略总览，看一眼就行，不做像素比对。
# 用法：powershell -NoProfile -File fixed_shots.ps1 [-Only 名字,名字] [-Build]
#   -Build：先 dotnet build 与 --headless --import（代码改过、还没编译时用）
# 每个机位单独拿放构建锁（WF-BUILD §4），120 秒超时，不开声音。出图固定在主检出 output/shots/，每次运行覆盖上一轮，总览是 总览.jpg。
param([string[]]$Only = @(), [switch]$Build)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
$layout = Get-UESTCLayout $repo
$godot = Get-UESTCGodot
$out = Join-Path $repo 'output\shots'
$null = [IO.Directory]::CreateDirectory($out)
# 每次覆盖上一轮：先清掉本目录里上一轮的截图、日志和总览，免得新旧画面混在一起
Get-ChildItem -LiteralPath $out -File | Where-Object { $_.Extension -in '.png', '.log', '.jpg' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

if ($Build) {
    $lease = Enter-UESTCBuildLock $layout.Locks
    try {
        Invoke-UESTCProcess 'dotnet' @('build', "$repo\game\Campus.sln", '-v', 'q') $repo (Join-Path $out 'build.log') | Out-Null
        Invoke-UESTCProcess $godot @('--headless', '--path', "$repo\game", '--import') $repo (Join-Path $out 'import.log') -TimeoutSeconds 600 | Out-Null
    } finally { Exit-UESTCBuildLock $lease }
}

$common = '--anim=240', '--flow=0', '--mode=fly', '--ui=0', '--seed=1', '--weather-seed=1', '--quality=fine',
    '--map=qingshuihe', '--title=0', '--save=off', '--audio=off'
# 第一个机位可能要重新烘焙（地图数据或代码改过后缓存失效），给 15 分钟；之后复用缓存，120 秒
$timeout = 900
foreach ($line in Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'poses.txt')) {
    if ($line.StartsWith('#') -or -not $line.Trim()) { continue }
    $p = $line -split '\s*\|\s*'; $name = $p[0].Trim(); $pose = $p[1].Trim(); $extra = $p[2].Trim() -split '\s+'
    if ($Only.Count -and $name -notin $Only) { continue }
    $tmp = Join-Path $out "_$name"; $null = [IO.Directory]::CreateDirectory($tmp)
    $args = @('--path', "$repo\game", '--windowed', '--resolution', '1920x1080', '--rendering-driver', 'd3d12', '--fixed-fps', '60',
        '--', "--shots=$pose", "--out=$tmp", '--frames=300') + $extra + $common
    $lease = Enter-UESTCBuildLock $layout.Locks
    try { $r = Invoke-UESTCProcess $godot $args $repo (Join-Path $out "$name.log") -AllowFailure -Visible -TimeoutSeconds $timeout; $code = $r.ExitCode }
    catch { $code = '超时' }
    finally { Exit-UESTCBuildLock $lease }
    $timeout = 120
    $src = Join-Path $tmp "$pose.png"
    if (Test-Path $src) { Move-Item -Force -LiteralPath $src -Destination (Join-Path $out "$name.png") }
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    "{0}：{1}" -f $name, $(if (Test-Path (Join-Path $out "$name.png")) { '出图' } else { "没出图（$code，日志 $name.log）" })
}
& python -I -X utf8 (Join-Path $PSScriptRoot 'contact_sheet.py') $out
