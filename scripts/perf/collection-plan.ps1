# 固定采集入口的场景选择、正式参数与逐会话关闭发布；不计算指标或批次中位。
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

function Assert-CollectionPhase($BatchRoot, $Protocol, $Phase, [string[]]$Scenes, $JudgementConditions) {
    if ($Phase -eq 'a1') { return }
    $previous = if ($Phase -eq 'b1') { 'a1' } else { 'b1' }
    $path = Join-Path $BatchRoot ($previous + '\phase.json')
    if (-not (Test-Path -LiteralPath $path)) { throw 'A/B/A phases must be collected in order.' }
    $record = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if (($record.judgementConditions | ConvertTo-Json -Compress -Depth 8) -ne ($JudgementConditions | ConvertTo-Json -Compress -Depth 8)) {
        throw 'Prior phase judgement conditions differ; batch conditions are frozen.'
    }
    if (-not $record.completed -or $record.protocol -ne $Protocol -or ($record.scenes -join ',') -ne ($Scenes -join ',')) {
        throw 'Prior phase is incomplete or its frozen protocol/scenes differ.'
    }
}

function Get-CollectionArguments($Root, $Directory, $Plan, $Item, $Seconds, $RequestPath) {
    $entry=$Item.scene
    $saveArgument = if ($entry.PSObject.Properties['saveAfterSample'] -and $entry.saveAfterSample) { '--save=1' } else { '--save=off' }
    $arguments = @('--path',(Join-Path $Root 'game'),'--windowed','--resolution','3840x2160','--',('--map=' + $Plan.map),
        '--quality=fine','--quality-set=display_mode=0','--audio=off','--flow=0',$saveArgument,
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
    $requestedBatchId=$Run.protocol.batchId
    if ($sessions.Count -ne 1) { $Run.status='partial'; $Run.reasons += 'Expected exactly one recording session for this attempt.' }
    foreach ($session in $sessions) {
        $manifestPath = Join-Path $session.FullName 'manifest.json'
        if (-not (Test-Path -LiteralPath $manifestPath)) {
            $Run.status='partial'; $Run.reasons += 'Recording did not publish a closed manifest.'
        } else {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            if (-not $manifest.complete -or $manifest.status -ne 'closed') {
                $Run.status='partial'; $Run.reasons += 'Recording closed incompletely; raw data retained.'
            }
            if ($Run.observed -and $Run.observed.sessionId -ne $manifest.id) {
                $Run.status='partial'; $Run.reasons += 'Completion does not identify this recording session.'
            }
        }
        if ($Run.status -ne 'valid') {
            $Run['requestedBatchId']=$requestedBatchId
            $Run.protocol.batchId = $requestedBatchId + '-rejected-' + $Run.protocol.phase + '-' + $Run.scene + '-' + $Run.protocol.repetition + '-' + $Run.attempt
            if ($Run.purpose -eq 'baseline') { $Run.purpose='diagnostic' }
        }
        Write-CollectionJson (Join-Path $session.FullName 'run.json') $Run
        $accepted = $Run.status -eq 'valid'
    }
    if (-not $sessions.Count) { $Run.status='partial'; $Run.reasons += 'No recording session was created.' }
    Write-CollectionJson (Join-Path $Directory 'attempt.json') $Run
    return $accepted
}
