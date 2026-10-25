# 固定采集入口的场景选择、正式参数与逐会话关闭发布；不计算指标或轮比较。
# 用法：仅由同目录 run.ps1 点引用。
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

function Publish-CollectionRound($Root, $Id, $Number, $SourceRoot, [string[]]$Scenes, $PreviousId,
    [string]$ConfirmationRoot='', [switch]$Preview) {
    $arguments=@('-I','-X','utf8',(Join-Path $Root 'scripts/perf/perf.py'),'--root',$Root,
        'record',$Id)
    if ($ConfirmationRoot) { $arguments += @('--confirmation-root',$ConfirmationRoot) }
    else {
        $arguments += @('--number',[string]$Number,'--source-root',$SourceRoot)
        if ($PreviousId) { $arguments += @('--previous-id',$PreviousId) }
        foreach ($scene in $Scenes) { $arguments += @('--scene',$scene) }
    }
    if ($Preview) { $arguments += '--preview' }
    $result = Invoke-UESTCProcess python $arguments $Root -TextEncoding ([Text.Encoding]::UTF8)
    if ($Preview) { return $result.StdOut | ConvertFrom-Json }
    Write-Host $result.StdOut
}

function Get-CollectionArguments($Root, $Directory, $Plan, $Item, $Seconds, $RequestPath) {
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
        (Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json')) -or
        (Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.pending.json'))
    })
    $accepted=$false
    if ($sessions.Count -ne 1) { $Run.status='partial'; $Run.reasons += 'Expected exactly one recording session for this attempt.' }
    foreach ($session in $sessions) {
        $manifestPath = Join-Path $session.FullName 'manifest.json'
        if (-not (Test-Path -LiteralPath $manifestPath)) {
            $Run.status='partial'; $Run.reasons += 'Recording did not publish a closed manifest.'
        } else {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if (-not $manifest.complete -or $manifest.status -ne 'closed') {
                $Run.status='partial'; $Run.reasons += 'Recording closed incompletely; raw data retained.'
            }
            if ($Run.observed -and $Run.observed.sessionId -ne $manifest.id) {
                $Run.status='partial'; $Run.reasons += 'Completion does not identify this recording session.'
            }
        }
        Write-CollectionJson (Join-Path $session.FullName 'run.json') $Run
        $accepted = $Run.status -eq 'valid'
    }
    if (-not $sessions.Count) { $Run.status='partial'; $Run.reasons += 'No recording session was created.' }
    Write-CollectionJson (Join-Path $Directory 'attempt.json') $Run
    return $accepted
}
