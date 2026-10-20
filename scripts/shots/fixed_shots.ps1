# 固定机位截图或 Movie Maker 录像：截图沿既有队列，录像每机位独立进程，去掉加载前导帧后从零编号。
# 用法：powershell -NoProfile -File fixed_shots.ps1 [-Only 名字,名字] [-Build] [-Poses 机位文件] [-MovieFrames 600] [-MovieEvents '60:season=winter;90:season=spring']
#   -Build：按共享 Directory.Build.props 编译优化 Debug 并导入（代码改过、还没编译时用）
#   -Poses：换一份机位表（格式同 poses.txt），临时加拍的机位放仓库外，不改 poses.txt
# 事件也可写在机位表第三列 --movie-events=...；帧号零起始，60 fps，第 60 帧即第 1 秒。
# 整轮持有构建锁（WF-BUILD），不开声音；截图 output/shots/、总览 总览.jpg；录像 output/movies/机位名/frame00000000.png。
param([string[]]$Only = @(), [switch]$Build, [string]$Poses = '',
    [ValidateRange(0, 2147483647)][int]$MovieFrames = 0, [string]$MovieEvents = '')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $repo 'scripts\lib\common.ps1')
$layout = Get-UESTCLayout $repo
$godot = Get-UESTCGodot
if ($MovieEvents -and -not $MovieFrames) { throw '-MovieEvents 需要 -MovieFrames。' }
$out = Join-Path $repo $(if ($MovieFrames) { 'output\movies' } else { 'output\shots' })
$null = [IO.Directory]::CreateDirectory($out)
$poses = if ($Poses) { (Resolve-Path $Poses).Path } else { Join-Path $PSScriptRoot 'poses.txt' }
$names = @(Get-Content -Encoding UTF8 $poses | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') } | ForEach-Object { ($_ -split '\s*\|\s*')[0].Trim() })
if ($Only.Count) {
    $Only = @($Only | ForEach-Object { $_ -split ',' })
    foreach ($name in $Only) { if ($name -notin $names) { throw "未知固定机位：$name" } }
    $names = @($names | Where-Object { $_ -in $Only })
}
$arguments = @('--path', "$repo\game", '--windowed', '--resolution', '1280x720', '--rendering-driver', 'd3d12', '--fixed-fps', '60',
    '--', "--shots-file=$poses", "--out=$out", '--anim=240', '--flow=0', '--mode=fly', '--ui=0', '--seed=1', '--weather-seed=1',
    '--quality=fine', '--map=qingshuihe', '--title=0', '--save=off', '--audio=off')
if (-not $MovieFrames -and $Only.Count) { $arguments += "--shots=$($names -join ',')" }
$lease = Enter-UESTCBuildLock $layout.Locks
try {
    # 拿到锁后清掉上一轮截图、日志和总览，整轮出图与拼图共用这把锁。
    Get-ChildItem -LiteralPath $out -File | Where-Object { $_.Extension -in '.png', '.log', '.jpg' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
    if ($Build) {
        Invoke-UESTCProcess 'dotnet' @('build', "$repo\game\Campus.sln", '-c', 'Debug', '-v', 'q') $repo (Join-Path $out 'build.log') | Out-Null
        $import = Invoke-UESTCProcess $godot @('--headless', '--editor', '--path', "$repo\game", '--', '--build-import') $repo (Join-Path $out 'import.log') -TimeoutSeconds 600
        Assert-UESTCCleanGodotLog $import
    }
    if ($MovieFrames) {
        foreach ($name in $names) {
            # 机位表与截图共用；原生 Movie Maker 每次启动只有一条片段，世界与渲染历史互不串用。
            if ($name -in '.', '..' -or $name.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0) { throw "非法机位名：$name" }
            $movieOut = Join-Path $out $name
            $null = [IO.Directory]::CreateDirectory($movieOut)
            Get-ChildItem -LiteralPath $movieOut -File | Where-Object { $_.Extension -in '.png', '.wav', '.log', '.json' } |
                ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
            $nativePath = Join-Path $movieOut 'movie.png'
            $movieArguments = @('--path', "$repo\game", '--windowed', '--resolution', '1280x720', '--rendering-driver', 'd3d12',
                '--fixed-fps', '60', '--write-movie', $nativePath,
                '--', "--shots-file=$poses", "--shots=$name", "--out=$movieOut", "--movie-frames=$MovieFrames",
                '--anim=240', '--flow=0', '--mode=fly', '--ui=0', '--seed=1', '--weather-seed=1',
                '--quality=fine', '--map=qingshuihe', '--title=0', '--save=off', '--audio=off')
            if ($MovieEvents) { $movieArguments += "--movie-events=$MovieEvents" }
            $log = Join-Path $movieOut 'capture.log'
            $result = Invoke-UESTCProcess $godot $movieArguments $repo $log -Visible -TimeoutSeconds 900
            $start = [regex]::Match($result.StdOut, '\[phase=movie-start\] firstMovieFrame=(\d+) frames=(\d+)')
            $finish = [regex]::Match($result.StdOut, '\[phase=movie-complete\] firstMovieFrame=(\d+) frames=(\d+)')
            if (-not $start.Success -or -not $finish.Success -or $start.Groups[1].Value -ne $finish.Groups[1].Value -or
                [int]$start.Groups[2].Value -ne $MovieFrames -or [int]$finish.Groups[2].Value -ne $MovieFrames) {
                throw "录像没有完整结束：$name，见 $log"
            }
            $first = [long]$start.Groups[1].Value
            $nativeFrames = @{}
            foreach ($file in Get-ChildItem -LiteralPath $movieOut -Filter 'movie*.png' -File) {
                if ($file.Name -match '^movie(\d+)\.png$') { $nativeFrames[[long]$Matches[1]] = $file.FullName }
            }
            for ($frame = 0; $frame -lt $MovieFrames; $frame++) {
                if (-not $nativeFrames.ContainsKey($first + $frame)) { throw "缺少 Movie Maker 帧 $($first + $frame)：$name" }
            }
            for ($frame = 0; $frame -lt $MovieFrames; $frame++) {
                Move-Item -LiteralPath $nativeFrames[$first + $frame] -Destination (Join-Path $movieOut ('frame{0:D8}.png' -f $frame))
            }
            # 仅删除已知原生前导/收尾帧与无声 WAV；最终图片仍是 Godot 原生写出的字节。
            foreach ($path in $nativeFrames.Values) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force } }
            $wave = Join-Path $movieOut 'movie.wav'
            if (Test-Path -LiteralPath $wave) { Remove-Item -LiteralPath $wave -Force }
            [ordered]@{ Fps = 60; Frames = $MovieFrames; MovieMakerFirstFrame = $first; Pattern = 'frame%08d.png' } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $movieOut 'frames.json') -Encoding UTF8
            "{0}：录像 {1} 帧，60 fps；frame00000000.png 对应原生第 {2} 帧" -f $name, $MovieFrames, $first
        }
        return
    }
    # 首次加载和逐机位截图均复用正式的目的地就绪与呈帧屏障。
    try { $r = Invoke-UESTCProcess $godot $arguments $repo (Join-Path $out 'capture.log') -AllowFailure -Visible -TimeoutSeconds 900; $code = $r.ExitCode }
    catch { $code = '超时或启动失败' }
    foreach ($name in $names) {
        "{0}：{1}" -f $name, $(if (Test-Path -LiteralPath (Join-Path $out "$name.png")) { '出图' } else { "没出图（$code，日志 capture.log）" })
    }
    & python -I -X utf8 (Join-Path $PSScriptRoot 'contact_sheet.py') $out
} finally { Exit-UESTCBuildLock $lease }
