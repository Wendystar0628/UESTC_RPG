# 按既有 deepen 条目重放三种诊断，保留原条件与附件，不加入常规协议。
# 用法：由 run.ps1 -DeepenSession <id> [-DiagnosticTool gpu-profile,EventPipe,Nsight] [-DeepenEntry 0] 调用。
function Find-PerfDiagnosticTool([string]$Root, [string]$Name) {
    if ($Name -eq 'EventPipe') {
        $path=Join-Path $Root 'output\tools\dotnet-diagnostics\dotnet-trace.exe'
        if (-not (Test-Path -LiteralPath $path)) {
            $common=(& git -C $Root rev-parse --path-format=absolute --git-common-dir).Trim()
            $path=Join-Path (Split-Path $common -Parent) 'output\tools\dotnet-diagnostics\dotnet-trace.exe'
        }
    } else {
        $installed=Join-Path ${env:ProgramFiles} 'NVIDIA Corporation'
        $path=@(Get-ChildItem -LiteralPath $installed -Directory -Filter 'Nsight Systems *' |
            Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'target-windows-x64\nsys.exe' } |
            Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)[0]
    }
    if (-not $path -or -not (Test-Path -LiteralPath $path)) { throw "$Name is not installed in the configured local tool store." }
    return $path
}

function Invoke-PerfDiagnostic($Root, $SessionId, [string[]]$Tools, [int]$EntryIndex, [IO.FileStream]$BuildLease) {
    $summaryPath=Join-Path $Root ('output\perf\derived\' + $SessionId + '\summary.json')
    $summary=Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
    $entries=@($summary.deepen)
    if ($EntryIndex -ge $entries.Count) { throw 'The requested session has no such deepen entry; analyze it first.' }
    $entry=$entries[$EntryIndex]
    $source=[IO.Path]::GetFullPath($summary.source)
    $manifest=Get-Content -LiteralPath (Join-Path $source 'manifest.json') -Raw | ConvertFrom-Json
    $sourceRun=Get-Content -LiteralPath (Join-Path $source 'run.json') -Raw | ConvertFrom-Json
    if (-not $manifest.complete -or $manifest.status -ne 'closed') { throw 'Diagnostic replay requires a closed source session.' }
    if (-not $sourceRun.conditions -or -not $sourceRun.sampleSegmentId) { throw 'Source has no frozen collection request; recollect it through run.ps1.' }
    if ($entry.scene -ne $sourceRun.scene) { throw 'Deepen scene differs from the frozen source run.' }
    $directory=Join-Path $Root ('output\perf\runs\diagnostic-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
    $null=[IO.Directory]::CreateDirectory($directory)
    $lease=$null
    $diagnosticFailed=$false
    $diagnosticSources=[Collections.Generic.List[string]]::new()
    $cache=$env:UESTC_BAKE_CACHE
    Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue
    try {
        if ($BuildLease) {
            if (-not $BuildLease.CanRead -or -not $BuildLease.CanWrite) { throw 'The supplied BuildLease is closed.' }
            $heldLease=$BuildLease
        } else { $lease=Enter-UESTCBuildLock (Get-UESTCLayout $Root).Locks; $heldLease=$lease }
        $godot=Get-UESTCGodot
        $environment=New-UESTCTestEnvironment -Work $directory
        $environment['UESTC_BAKE_CACHE']=$null
        Update-UESTCGameBuild -Root $Root -Godot $godot -BuildLease $heldLease -Environment $environment
        $parserDll=$null
        if ('EventPipe' -in $Tools) {
            $traceTool=Find-PerfDiagnosticTool $Root 'EventPipe'
            $traceLibrary=@(Get-ChildItem -LiteralPath (Join-Path (Split-Path $traceTool -Parent) '.store\dotnet-trace') -Recurse -File -Filter 'Microsoft.Diagnostics.Tracing.TraceEvent.dll')
            if ($traceLibrary.Count -ne 1) { throw 'The pinned dotnet-trace store must identify exactly one TraceEvent assembly.' }
            $parseBuild=Invoke-UESTCProcess dotnet @('build',(Join-Path $Root 'scripts\build\startup-cpu\CpuParse.csproj'),
                ('-p:TraceToolDir=' + $traceLibrary[0].DirectoryName),'--nologo') $Root (Join-Path $directory 'cpu-parser-build.log')
            $parserDll=Join-Path $Root 'output\start_game\TOOL-215-profile\bin\CpuParse\Debug\net10.0\CpuParse.dll'
            if (-not (Test-Path -LiteralPath $parserDll)) { throw 'CpuParse build did not publish its existing CLI.' }
        }
        foreach ($tool in $Tools | Select-Object -Unique) {
            $work=Join-Path $directory $tool
            $null=[IO.Directory]::CreateDirectory($work)
            $request=$sourceRun.conditions | ConvertTo-Json -Depth 16 | ConvertFrom-Json
            $request.sampleSegmentId='diagnostic-' + [Guid]::NewGuid().ToString('N')
            $requestPath=Join-Path $work 'collection.json'
            Write-CollectionJson $requestPath $request
            $poseArguments=@($entry.poseArgs | Where-Object { $_ -notmatch '^--(?:perf-out|perf-collection|save-dir)=' })
            $gameArguments=@('--path',(Join-Path $Root 'game'))
            if ($tool -eq 'gpu-profile') { $gameArguments+='--gpu-profile' }
            $gameArguments+=@('--') + $poseArguments +
                @(('--save-dir=' + (Join-Path $work 'save')),('--perf-out=' + $work),('--perf-collection=' + $requestPath))
            $condition=[ordered]@{ purpose='diagnostic'; sourceSession=$SessionId; sourceDirectory=$source; entry=$entry;
                tool=$tool; scene=$entry.scene; collection=$request; sourceCollection=$sourceRun.conditions;
                engine=$godot; arguments=$gameArguments; desktop='isolated'; elevation='ordinary';
                toolPath=$(if($tool -ne 'gpu-profile'){Find-PerfDiagnosticTool $Root $tool}else{$godot});
                cpuProfile=$(if($tool -eq 'EventPipe'){'dotnet-sampled-thread-time'}else{$null});
                parserDll=$parserDll; nominalSampleRateHz=$(if($tool -eq 'EventPipe'){100}else{$null}); sampleRateHz=$null;
                sampleRateReason='Actual collector output and timestamp intervals are retained; no requested rate is asserted as measured.' }
            $conditionPath=Join-Path $work 'condition.json'
            Write-CollectionJson $conditionPath $condition
            $environment=New-UESTCTestEnvironment -Work $work
            $environment['UESTC_BAKE_CACHE']=$null
            $result=Invoke-UESTCProcess powershell @('-NoProfile','-File',(Join-Path $Root 'scripts\perf\diagnostic-capture.ps1'),'-ConditionPath',$conditionPath) `
                $Root (Join-Path $work 'capture.log') -TimeoutSeconds 720 -AllowFailure -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
            & python -I -X utf8 (Join-Path $Root 'scripts\perf\diagnostic-report.py') --root $Root --work $work --source $source --exit-code $result.ExitCode
            if ($LASTEXITCODE -eq 2) { $diagnosticFailed=$true }
            elseif ($LASTEXITCODE -ne 0) { throw "Diagnostic publication failed: $work" }
            foreach ($session in Get-ChildItem -LiteralPath $work -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') }) {
                $diagnosticSources.Add($session.FullName)
            }
        }
        Invoke-UESTCPerfAnalysis -Root $Root -SessionSources $diagnosticSources.ToArray()
        if ($diagnosticFailed) { throw 'One or more diagnostic captures are incomplete; original attachments and conditions are retained.' }
    } finally {
        Exit-UESTCBuildLock $lease
        if ($null -eq $cache) { Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue } else { $env:UESTC_BAKE_CACHE=$cache }
    }
}
