# G10 复用固定采集入口与分析器的批次中位，只判成本预算，缺测与协议不足保留原因。
# 用法：由 scripts/gate.ps1 点引用后调用 Invoke-UESTCPerformanceGate；Read-UESTCPerformanceResult 只读已有批次。
function Get-UESTCPerfField {
    param($Value, [string]$Path)
    foreach ($part in $Path.Split('.')) {
        if ($null -eq $Value) { return $null }
        if ($Value -is [Collections.IDictionary]) { $Value = $Value[$part] }
        elseif ($Value.PSObject.Properties[$part]) { $Value = $Value.$part }
        else { return $null }
    }
    return $Value
}

function Test-UESTCPerfNumber {
    param($Value)
    return ($Value -is [ValueType] -and $Value -isnot [bool] -and
        $Value -isnot [char] -and -not [double]::IsNaN([double]$Value) -and
        -not [double]::IsInfinity([double]$Value))
}

function Read-UESTCPerfJson {
    param([string]$Path, [Collections.Generic.List[string]]$Reasons)
    try { return Get-Content -Raw -LiteralPath $Path -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop }
    catch { $Reasons.Add("Cannot read '$Path': $($_.Exception.Message)"); return $null }
}

function Get-UESTCPerfCostChecks {
    param($Group, [object[]]$Summaries, [Collections.Generic.List[string]]$Reasons)
    $scene = Get-UESTCPerfField $Group 'comparisonKey.scene'
    # The analyzer explicitly marks frameSetupCpuMs as a borrowed floor, never a budget.
    foreach ($metric in @('mainViewportGpuMs','mainViewportCpuMs')) {
        $checks = @($Summaries | ForEach-Object { @(Get-UESTCPerfField $_ 'budgets') | Where-Object { (Get-UESTCPerfField $_ 'metric') -eq $metric } })
        $value = Get-UESTCPerfField $Group "phases.a1.steady.cost.$metric.p95"
        if ($checks.Count -ne 3 -or @($checks | Where-Object {
            (Get-UESTCPerfField $_ 'applicable') -ne $true -or
            -not (Test-UESTCPerfNumber (Get-UESTCPerfField $_ 'value')) -or
            -not (Test-UESTCPerfNumber (Get-UESTCPerfField $_ 'budget')) -or
            (Get-UESTCPerfField $_ 'budget') -le 0 -or
            (Get-UESTCPerfField $_ 'exceeded') -isnot [bool]
        }).Count -or -not (Test-UESTCPerfNumber $value)) {
            $Reasons.Add("${scene}/${metric}: budget conditions or measured cost are unavailable. " +
                ((@($checks | ForEach-Object { Get-UESTCPerfField $_ 'reason' } | Where-Object { $_ } | Select-Object -Unique)) -join '; '))
            continue
        }
        $budgets = @($checks | ForEach-Object { $_.budget } | Select-Object -Unique)
        $sources = @($checks | ForEach-Object { $_.budgetSource } | Select-Object -Unique)
        if ($budgets.Count -ne 1 -or $sources.Count -ne 1 -or -not $sources[0]) {
            $Reasons.Add("${scene}/${metric}: analyzer budget differs within the three repetitions.")
            continue
        }
        [pscustomobject]@{ Scene=$scene; Metric=$metric; Value=$value; Budget=$budgets[0];
            BudgetSource=$sources[0]; Exceeded=($value -gt $budgets[0]) }
    }
}

function Read-UESTCPerformanceResult {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Batch,
        [Parameter(Mandatory)][string[]]$Scenes, [string[]]$SaveScenes=@(), [string]$CollectionError='')
    $reasons = New-Object 'Collections.Generic.List[string]'
    $sessions = New-Object 'Collections.Generic.List[string]'
    $costs = New-Object 'Collections.Generic.List[object]'
    $timings = New-Object 'Collections.Generic.List[object]'
    $attempts = New-Object 'Collections.Generic.List[object]'
    $analysisErrors = New-Object 'Collections.Generic.List[object]'
    if ($CollectionError) { $reasons.Add($CollectionError) }
    if (-not $SaveScenes.Count -or @($SaveScenes | Where-Object { $_ -notin $Scenes }).Count) {
        $reasons.Add('The fixed G10 plan has no covered post-sample Manual save scene.')
    }
    $perf = Join-Path $Root 'output/perf'
    $phaseRoot = Assert-UESTCChildPath (Join-Path $perf ("runs/$Batch/a1")) (Join-Path $perf 'runs')
    $phase = Read-UESTCPerfJson (Join-Path $phaseRoot 'phase.json') $reasons
    if ((Get-UESTCPerfField $phase 'completed') -ne $true -or
        (Get-UESTCPerfField $phase 'status') -ne 'completed' -or
        (Get-UESTCPerfField $phase 'purpose') -ne 'baseline' -or
        (Get-UESTCPerfField $phase 'protocol') -ne 'repeat3' -or
        (Get-UESTCPerfField $phase 'phase') -ne 'a1' -or
        (@(Get-UESTCPerfField $phase 'scenes') -join ',') -ne ($Scenes -join ',')) {
        $reasons.Add('Formal repeat3/a1 phase is incomplete or its fixed-scene coverage differs.')
        if (Get-UESTCPerfField $phase 'error') { $reasons.Add([string]$phase.error) }
    }
    if (Test-Path -LiteralPath $phaseRoot -PathType Container) {
        foreach ($file in @(Get-ChildItem -LiteralPath $phaseRoot -Filter attempt.json -File -Recurse)) {
            $attempt = Read-UESTCPerfJson $file.FullName $reasons
            if (-not $attempt) { continue }
            $attempts.Add([pscustomobject]@{ Scene=$attempt.scene; Attempt=$file.Directory.Name;
                Status=$attempt.status; Reasons=@(Get-UESTCPerfField $attempt 'reasons');
                SessionId=(Get-UESTCPerfField $attempt 'observed.sessionId');
                Save=(Get-UESTCPerfField $attempt 'observed.save'); Close=(Get-UESTCPerfField $attempt 'close') })
            $observedSession = Get-UESTCPerfField $attempt 'observed.sessionId'
            if ($observedSession -and -not $sessions.Contains($observedSession)) { $sessions.Add($observedSession) }
        }
    }
    $index = Read-UESTCPerfJson (Join-Path $perf 'index.json') $reasons
    foreach ($errorRecord in @(Get-UESTCPerfField $index 'errors')) {
        $errorSource = Get-UESTCPerfField $errorRecord 'source'
        if ($errorSource -and [IO.Path]::GetFullPath($errorSource).StartsWith($phaseRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            $analysisErrors.Add($errorRecord)
        }
    }
    $batches = @(@(Get-UESTCPerfField $index 'batches') | Where-Object { (Get-UESTCPerfField $_ 'id') -eq $Batch })
    $batchResult = if ($batches.Count -eq 1) { $batches[0] } else { $null }
    if ((Get-UESTCPerfField $batchResult 'status') -ne 'current' -or
        (Get-UESTCPerfField $batchResult 'kind') -ne 'repeat3' -or
        (Get-UESTCPerfField $batchResult 'targetPhase') -ne 'a1') {
        $reasons.Add('Analyzer has no current, complete repeat3 batch for this G10 request.')
        foreach ($reason in @(Get-UESTCPerfField $batchResult 'statusReasons')) { if ($reason) { $reasons.Add([string]$reason) } }
        foreach ($errorRecord in $analysisErrors) { $reasons.Add("Analyzer source '$($errorRecord.source)': $($errorRecord.reason)") }
    }
    $groups = @(Get-UESTCPerfField $batchResult 'groups')
    $actualScenes = @($groups | ForEach-Object { Get-UESTCPerfField $_ 'comparisonKey.scene' } | Sort-Object)
    if (($actualScenes -join ',') -ne (($Scenes | Sort-Object) -join ',')) { $reasons.Add('Analyzer fixed-scene groups are missing, duplicated or unexpected.') }
    foreach ($group in $groups) {
        if ($null -eq $group) { continue }
        $summaries = @(Read-UESTCPerfGroup -Perf $perf -PhaseRoot $phaseRoot -Batch $Batch -Group $group -Index $index -SaveScenes $SaveScenes -Reasons $reasons -Sessions $sessions -Timings $timings)
        foreach ($cost in @(Get-UESTCPerfCostChecks $group $summaries $reasons)) { $costs.Add($cost) }
    }
    $uniqueReasons = @($reasons | Select-Object -Unique)
    $exceeded = @($costs | Where-Object { $_.Exceeded })
    $status = if ($uniqueReasons.Count) { 'Incomplete' } elseif ($exceeded.Count) { 'Failed' } else { 'Passed' }
    return [pscustomobject]@{ Status=$status; Batch=$Batch; SessionIds=@($sessions | Select-Object -Unique); Attempts=$attempts.ToArray();
        IncompleteReasons=$uniqueReasons; CostChecks=$costs.ToArray(); Exceeded=$exceeded; AnalysisErrors=$analysisErrors.ToArray();
        SaveAndClose=$timings.ToArray(); TimingPolicy='Save and close durations are reported without thresholds.' }
}

function Read-UESTCPerfGroup {
    param([string]$Perf, [string]$PhaseRoot, [string]$Batch, $Group, $Index,
        [string[]]$SaveScenes,
        [Collections.Generic.List[string]]$Reasons, [Collections.Generic.List[string]]$Sessions,
        [Collections.Generic.List[object]]$Timings)
    $ids = @(Get-UESTCPerfField $Group 'phases.a1.sessionIds')
    $scene = Get-UESTCPerfField $Group 'comparisonKey.scene'
    if ($ids.Count -ne 3 -or @($ids | Select-Object -Unique).Count -ne 3) { $Reasons.Add("${scene}: analyzer did not publish exactly three distinct sessions.") }
    foreach ($sessionId in $ids) {
        if (-not $sessionId -or [IO.Path]::GetFileName($sessionId) -ne $sessionId) { $Reasons.Add("${scene}: invalid session ID."); continue }
        $Sessions.Add([string]$sessionId)
        $summaryPath = Assert-UESTCChildPath (Join-Path $Perf ("derived/$sessionId/summary.json")) (Join-Path $Perf 'derived')
        $summary = Read-UESTCPerfJson $summaryPath $Reasons
        if (-not $summary) { continue }
        $source = Get-UESTCPerfField $summary 'source'
        if (-not $source -or -not [IO.Path]::GetFullPath($source).StartsWith($PhaseRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            $Reasons.Add("${sessionId}: source is outside this fresh phase."); continue
        }
        $run = Read-UESTCPerfJson (Join-Path $source 'run.json') $Reasons
        if ($summary.id -ne $sessionId -or $summary.calc -ne (Get-UESTCPerfField $Index 'calc') -or $Group.calc -ne $summary.calc -or
            $summary.origin -ne 'ai' -or $summary.scope -ne 'cost' -or $summary.baseStatus -ne 'candidate' -or $summary.status -notin @('current','superseded') -or
            (Get-UESTCPerfField $run 'status') -ne 'valid' -or (Get-UESTCPerfField $run 'purpose') -ne 'baseline' -or
            (Get-UESTCPerfField $run 'protocol.batchId') -ne $Batch -or (Get-UESTCPerfField $run 'scene') -ne $scene) {
            $Reasons.Add("${sessionId}: session is not current, valid formal cost evidence for this scene.")
        }
        foreach ($errorRecord in @(@(Get-UESTCPerfField $Index 'errors') | Where-Object { (Get-UESTCPerfField $_ 'source') -eq $source })) {
            $Reasons.Add("${sessionId}: $($errorRecord.reason)")
        }
        foreach ($name in @('sampleBegin','sampleEnd','gpu','cpu','sample')) {
            $observed = Get-UESTCPerfField $run "coverage.$name.observed"
            $required = Get-UESTCPerfField $run "coverage.$name.required"
            if (-not (Test-UESTCPerfNumber $observed) -or -not (Test-UESTCPerfNumber $required) -or $required -le 0 -or $observed -lt $required) {
                $Reasons.Add("${sessionId}: $name coverage is insufficient.")
            }
        }
        # Save and close facts are supplied by the collection owner; never infer elapsed time from frame counts.
        $saveRequested = $scene -in $SaveScenes
        $save = Get-UESTCPerfField $run 'observed.save'
        $saveMs = Get-UESTCPerfField $save 'durationMs'
        if ((Get-UESTCPerfField $run 'conditions.saveAfterSample') -ne $saveRequested) {
            $Reasons.Add("${sessionId}: post-sample save request differs from the frozen fixed plan.")
        }
        if ($saveRequested -and ((Get-UESTCPerfField $save 'reason') -ne 'Manual' -or
            (Get-UESTCPerfField $save 'status') -ne 'Saved' -or -not (Test-UESTCPerfNumber $saveMs) -or $saveMs -lt 0)) {
            $Reasons.Add("${sessionId}: real Manual save completion or elapsed time is missing.")
        }
        $closeMs = Get-UESTCPerfField $run 'close.durationMs'
        if ((Get-UESTCPerfField $run 'close.completed') -ne $true -or
            (Get-UESTCPerfField $run 'close.source') -ne 'formal-close-task' -or
            -not (Test-UESTCPerfNumber $closeMs) -or $closeMs -lt 0) {
            $Reasons.Add("${sessionId}: formal close lacks measured start/end facts. " + (Get-UESTCPerfField $run 'close.reason'))
        }
        $Timings.Add([pscustomobject]@{ SessionId=$sessionId; SaveRequested=$saveRequested;
            Save=$save; Close=(Get-UESTCPerfField $run 'close') })
        $summary
    }
}

function Invoke-UESTCPerformanceGate {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Map,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease)
    $perfScript = Join-Path $Root 'scripts/perf/perf.py'
    $check = Invoke-UESTCProcess python @('-I','-X','utf8',$perfScript,'--root',$Root,'selfcheck') $Root
    Write-Host $check.StdOut
    if (-not $BuildLease.CanRead -or -not $BuildLease.CanWrite) { throw 'The supplied G10 BuildLease is closed.' }
    $plan = Get-Content -Raw -LiteralPath (Join-Path $Root 'scripts/perf/scenarios.json') | ConvertFrom-Json
    if ($plan.map -ne $Map) {
        return [pscustomobject]@{ Status='Incomplete'; IncompleteReasons=@("Fixed collection map '$($plan.map)' differs from requested map '$Map'.") }
    }
    $scenes = @($plan.fixed | ForEach-Object { $_.id })
    $saveScenes = @($plan.fixed | Where-Object { $_.PSObject.Properties['saveAfterSample'] -and $_.saveAfterSample } | ForEach-Object { $_.id })
    $batch = 'G10-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
    $collectionError = ''
    try {
        & (Join-Path $Root 'scripts/perf/run.ps1') -Batch $batch -Suite fixed -Scene $scenes -Mode formal -Protocol repeat3 -Phase a1 -BuildLease $BuildLease | Out-Host
    } catch { $collectionError = $_.Exception.ToString() }
    return Read-UESTCPerformanceResult -Root $Root -Batch $batch -Scenes $scenes -SaveScenes $saveScenes -CollectionError $collectionError
}
