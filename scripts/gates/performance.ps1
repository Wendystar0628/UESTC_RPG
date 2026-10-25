# G10 只读分析器最新已登记轮与相邻轮比较，缺测、过时与确认不足保留原因。
# 用法：由 scripts/gate.ps1 点引用后调用 Invoke-UESTCPerformanceGate；采集和轮登记仅由 scripts/perf 负责。
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
    try { return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop }
    catch { $Reasons.Add("Cannot read '$Path': $($_.Exception.Message)"); return $null }
}

function Get-UESTCPerfCostChecks {
    param($Group, [Collections.Generic.List[string]]$Reasons)
    $scene = Get-UESTCPerfField $Group 'comparisonKey.scene'
    $checks = @(Get-UESTCPerfField $Group 'distanceTable' | Where-Object { $null -ne $_ })
    foreach ($check in $checks) {
        $metric = Get-UESTCPerfField $check 'metric'
        $tier = Get-UESTCPerfField $check 'tier'
        [pscustomobject]@{ Scene=$scene; Metric=$metric; Value=(Get-UESTCPerfField $check 'value'); Budget=(Get-UESTCPerfField $check 'upper');
            BudgetSource=(Get-UESTCPerfField $check 'source'); Tier=$tier; Exceeded=(Get-UESTCPerfField $check 'exceeded') }
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
    $index = Read-UESTCPerfJson (Join-Path $perf 'index.json') $reasons
    $batches = @(@(Get-UESTCPerfField $index 'batches') | Where-Object { (Get-UESTCPerfField $_ 'id') -eq $Batch })
    $batchResult = if ($batches.Count -eq 1) { $batches[0] } else { $null }
    $sourceRoots = @()
    foreach ($sourceRoot in @((Get-UESTCPerfField $batchResult 'sourceRoot')) + @(Get-UESTCPerfField $batchResult 'confirmationRoots' | Where-Object { $null -ne $_ })) {
        if (-not $sourceRoot -or -not [IO.Path]::IsPathRooted($sourceRoot)) {
            $reasons.Add('Analyzer round registration has no absolute source root.')
            continue
        }
        $sourceRoots += [IO.Path]::GetFullPath($sourceRoot).TrimEnd('\','/')
    }
    foreach ($sourceRoot in $sourceRoots) {
        if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { $reasons.Add("Round source '$sourceRoot' is missing."); continue }
        foreach ($file in @(Get-ChildItem -LiteralPath $sourceRoot -Filter attempt.json -File -Recurse)) {
            $attempt = Read-UESTCPerfJson $file.FullName $reasons
            if (-not $attempt) { continue }
            $attempts.Add([pscustomobject]@{ Scene=$attempt.scene; Attempt=$file.Directory.Name;
                Status=$attempt.status; Reasons=@(Get-UESTCPerfField $attempt 'reasons');
                SessionId=(Get-UESTCPerfField $attempt 'observed.sessionId');
                Save=(Get-UESTCPerfField $attempt 'observed.save'); Close=(Get-UESTCPerfField $attempt 'close') })
        }
    }
    foreach ($errorRecord in @(Get-UESTCPerfField $index 'errors')) {
        $errorSource = Get-UESTCPerfField $errorRecord 'source'
        if ($errorSource -and @($sourceRoots | Where-Object {
            [IO.Path]::GetFullPath($errorSource) -eq $_ -or [IO.Path]::GetFullPath($errorSource).StartsWith($_ + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
        }).Count) {
            $analysisErrors.Add($errorRecord)
        }
    }
    if ((Get-UESTCPerfField $batchResult 'status') -ne 'current' -or
        (Get-UESTCPerfField $batchResult 'gateStatus') -notin @('Passed','Failed')) {
        $reasons.Add('Analyzer has no current complete round with its required confirmation for this G10 request.')
        foreach ($reason in @(Get-UESTCPerfField $batchResult 'statusReasons')) { if ($reason) { $reasons.Add([string]$reason) } }
        foreach ($errorRecord in $analysisErrors) { $reasons.Add("Analyzer source '$($errorRecord.source)': $($errorRecord.reason)") }
    }
    $groups = @(Get-UESTCPerfField $batchResult 'groups')
    $actualScenes = @($groups | ForEach-Object { Get-UESTCPerfField $_ 'comparisonKey.scene' } | Sort-Object)
    if (($actualScenes -join ',') -ne (($Scenes | Sort-Object) -join ',')) { $reasons.Add('Analyzer fixed-scene groups are missing, duplicated or unexpected.') }
    foreach ($group in $groups) {
        if ($null -eq $group) { continue }
        Read-UESTCPerfGroup -Perf $perf -SourceRoots $sourceRoots -Group $group -Index $index -SaveScenes $SaveScenes -Reasons $reasons -Sessions $sessions -Timings $timings
        foreach ($cost in @(Get-UESTCPerfCostChecks $group $reasons)) { $costs.Add($cost) }
    }
    $uniqueReasons = @($reasons | Select-Object -Unique)
    $exceeded = @($costs | Where-Object { $_.Exceeded })
    $status = if ($uniqueReasons.Count) { 'Incomplete' } else { Get-UESTCPerfField $batchResult 'gateStatus' }
    return [pscustomobject]@{ Status=$status; Batch=$Batch; SessionIds=@($sessions | Select-Object -Unique); Attempts=$attempts.ToArray();
        IncompleteReasons=$uniqueReasons; CostChecks=$costs.ToArray(); Exceeded=$exceeded; AnalysisErrors=$analysisErrors.ToArray();
        SaveAndClose=$timings.ToArray(); Comparison=@(Get-UESTCPerfField $batchResult 'comparison');
        TimingPolicy='Save and close durations are reported without thresholds.' }
}

function Read-UESTCPerfGroup {
    param([string]$Perf, [string[]]$SourceRoots, $Group, $Index,
        [string[]]$SaveScenes,
        [Collections.Generic.List[string]]$Reasons, [Collections.Generic.List[string]]$Sessions,
        [Collections.Generic.List[object]]$Timings)
    $ids = @(Get-UESTCPerfField $Group 'sessionIds')
    $scene = Get-UESTCPerfField $Group 'comparisonKey.scene'
    foreach ($sessionId in $ids) {
        if (-not $sessionId -or [IO.Path]::GetFileName($sessionId) -ne $sessionId) { $Reasons.Add("${scene}: invalid session ID."); continue }
        $Sessions.Add([string]$sessionId)
        $summaryPath = Assert-UESTCChildPath (Join-Path $Perf ("derived/$sessionId/summary.json")) (Join-Path $Perf 'derived')
        $summary = Read-UESTCPerfJson $summaryPath $Reasons
        if (-not $summary) { continue }
        $source = Get-UESTCPerfField $summary 'source'
        if (-not $source -or -not @($SourceRoots | Where-Object {
            [IO.Path]::GetFullPath($source) -eq $_ -or [IO.Path]::GetFullPath($source).StartsWith($_ + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
        }).Count) {
            $Reasons.Add("${sessionId}: source is outside this registered round."); continue
        }
        $run = Read-UESTCPerfJson (Join-Path $source 'run.json') $Reasons
        if ($summary.id -ne $sessionId -or $summary.calc -ne (Get-UESTCPerfField $Index 'calc') -or $Group.calc -ne $summary.calc -or
            $summary.origin -ne 'ai' -or $summary.scope -ne 'cost' -or $summary.baseStatus -ne 'candidate' -or $summary.status -notin @('current','superseded') -or
            (Get-UESTCPerfField $run 'status') -ne 'valid' -or (Get-UESTCPerfField $run 'purpose') -ne 'baseline' -or
            (Get-UESTCPerfField $run 'exitCode') -ne 0 -or (Get-UESTCPerfField $run 'timedOut') -ne $false -or
            (Get-UESTCPerfField $run 'scene') -ne $scene) {
            $Reasons.Add("${sessionId}: session is not current, valid formal cost evidence for this scene.")
        }
        foreach ($errorRecord in @(@(Get-UESTCPerfField $Index 'errors') | Where-Object { (Get-UESTCPerfField $_ 'source') -eq $source })) {
            $Reasons.Add("${sessionId}: $($errorRecord.reason)")
        }
        foreach ($name in @('sampleBegin','sampleEnd','gpu','sample')) {
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
    }
}

function Invoke-UESTCPerformanceGate {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Map)
    $plan = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'scripts/perf/scenarios.json') | ConvertFrom-Json
    if ($plan.map -ne $Map) {
        return [pscustomobject]@{ Status='Incomplete'; IncompleteReasons=@("Fixed collection map '$($plan.map)' differs from requested map '$Map'.") }
    }
    $reasons = New-Object 'Collections.Generic.List[string]'
    $index = Read-UESTCPerfJson (Join-Path $Root 'output/perf/index.json') $reasons
    $latest = @(Get-UESTCPerfField $index 'batches' | Sort-Object number -Descending | Select-Object -First 1)
    $dictionary = Read-UESTCPerfJson (Join-Path $Root 'scripts/perf/derived-fields.json') $reasons
    if (-not $latest.Count -or -not $latest[0] -or (Get-UESTCPerfField $index 'calc') -ne (Get-UESTCPerfField $dictionary 'calc')) {
        return [pscustomobject]@{ Status='Incomplete'; IncompleteReasons=@($reasons.ToArray()) + @('No latest registered round analyzed with the current calc.'); Comparison=@() }
    }
    $authored = @($plan.fixed) + @($plan.route) + @($plan.travel)
    $saveScenes = @($authored | Where-Object { $_.PSObject.Properties['saveAfterSample'] -and $_.saveAfterSample -and $_.id -in $latest[0].scenes } | ForEach-Object { $_.id })
    return Read-UESTCPerformanceResult -Root $Root -Batch $latest[0].id -Scenes @($latest[0].scenes) -SaveScenes $saveScenes
}
