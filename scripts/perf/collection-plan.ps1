# 固定采集与诊断入口共用的场景选择、采集请求、启动参数与逐会话发布；不计算指标。
# 用法：由同目录 run.ps1 点引用。
function Write-CollectionJson($Path, $Value) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 16), (New-Object Text.UTF8Encoding $false))
}

function Get-CollectionScenes($Plan, [string[]]$Suites, [string[]]$Scenes) {
    $selected = @()
    foreach ($kind in $Suites) {
        foreach ($entry in $Plan.$kind) {
            if ($Scenes.Count -and $entry.id -notin $Scenes) { continue }
            $selected += [pscustomobject]@{ kind=$kind; scene=$entry }
        }
    }
    if (-not $selected.Count) { throw 'No authored scenes selected.' }
    if (@($selected.scene.id | Select-Object -Unique).Count -ne $selected.Count) { throw 'Duplicate scenes selected.' }
    if ($Scenes.Count -and @($Scenes | Where-Object { $_ -notin $selected.scene.id }).Count) { throw 'Unknown selected scene.' }
    return $selected
}

# DECISION-363: preparation and sample markers belong to the game driver; no condition gate here.
function New-CollectionRequest($Item) {
    $entry=$Item.scene
    $seconds=if ($Item.kind -eq 'route') { $entry.seconds } else { 30 }
    return [ordered]@{ sceneId=$entry.id; sampleSegmentId=('sample-' + [Guid]::NewGuid().ToString('N'));
        sampleSeconds=$seconds; saveAfterSample=($entry.PSObject.Properties['saveAfterSample'] -and [bool]$entry.saveAfterSample);
        routePointIds=@($(if ($entry.PSObject.Properties['routePointIds']) { $entry.routePointIds }));
        travelPrefetch=($entry.PSObject.Properties['travelPrefetch'] -and [bool]$entry.travelPrefetch) }
}

function Get-CollectionArguments($Root, $Directory, $Plan, $Item, $RequestPath) {
    $entry=$Item.scene
    $saveArgument = if ($entry.PSObject.Properties['saveAfterSample'] -and $entry.saveAfterSample) { '--save=1' } else { '--save=off' }
    $arguments = @('--path',(Join-Path $Root 'game'),'--fullscreen','--resolution','3840x2160','--',('--map=' + $Plan.map),
        '--quality=fine','--quality-set=display_mode=1','--audio=off','--flow=0',$saveArgument,
        ('--save-dir=' + (Join-Path $Directory 'save')),('--perf-out=' + $Directory),('--perf-collection=' + $RequestPath))
    switch ($Item.kind) {
        'fixed' {
            if ($entry.start -eq 'new-game') { $arguments += @('--title=1','--startup-scenario=immediate') }
            else { $arguments += @('--title=0',('--pose=' + $entry.waypoint),('--mode=' + $entry.mode)) }
        }
        'route' {
            $arguments += @('--title=1','--startup-scenario=route',('--route-start=' + $entry.waypoint),
                ('--route-mode=' + $entry.mode),('--route-seconds=' + $entry.seconds))
        }
        'travel' { $arguments += @('--title=0','--mode=walk',('--travel-sequence=' + ($entry.sequence -join ','))) }
    }
    if ($entry.world -eq 'summer-thunderstorm') {
        $arguments += @('--season=summer','--clock=22','--weather=thunderstorm','--weather-intensity=1','--wet=1')
    }
    return $arguments
}

function Publish-CollectionRun($Directory, $Run) {
    $sessions = @(Get-ChildItem -LiteralPath $Directory -Directory | Where-Object {
        Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json')
    })
    foreach ($session in $sessions) { Write-CollectionJson (Join-Path $session.FullName 'run.json') $Run }
    Write-CollectionJson (Join-Path $Directory 'run.json') $Run
    return @($sessions | ForEach-Object { $_.FullName })
}
