# G10 只读最新场景指标、预算线与上次差值；不采集、不登记、不作性能合格判定。
# 用法：由 scripts/gate.ps1 点引用后调用 Invoke-UESTCPerformanceGate。
function Invoke-UESTCPerformanceGate {
    param([Parameter(Mandatory)][string]$Root)
    $index = Get-Content -LiteralPath (Join-Path $Root 'output/perf/index.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $dictionary = Get-Content -LiteralPath (Join-Path $Root 'scripts/perf/derived-fields.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($index.calc -ne $dictionary.calc) { throw 'Run scripts/perf/analyze.py to update the metrics.' }
    foreach ($scene in $index.scenes) {
        Write-Host ("{0}: {1}; previous: {2}" -f $scene.scene, $scene.id, $scene.previousId)
        Write-Host ($scene.delta | Format-Table metric,previous,value,delta,changeFraction -AutoSize | Out-String)
        Write-Host ($scene.budgets | Format-Table metric,value,budget,deltaToBudget -AutoSize | Out-String)
    }
    return [pscustomobject]@{ Status='NotRun'; Scenes=@($index.scenes); AnalysisErrors=@($index.errors);
        Note='Metrics and differences only; no performance acceptance judgement.' }
}
