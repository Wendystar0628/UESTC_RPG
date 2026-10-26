# 单场景单工具诊断：沿用采集场景计划、构建准备与私桌面，产物写 output/perf/diagnostics/<时刻>-<场景>-<工具>/ 并打印摘要；不登记、不进分析。
# 用法：由 run.ps1 -Scene <场景> -DiagnosticTool gpu-profile|EventPipe|Nsight 点引用调用。
function Find-PerfNsight {
    $installed=Join-Path ${env:ProgramFiles} 'NVIDIA Corporation'
    $path=@(Get-ChildItem -LiteralPath $installed -Directory -Filter 'Nsight Systems *' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'target-windows-x64\nsys.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
    if (-not $path.Count) { throw 'Nsight Systems is not installed.' }
    return $path[0]
}

function Write-PerfGpuSteps($Session) {
    $rows=@(Import-Csv -LiteralPath (Join-Path $Session 'gpusteps.csv') -Encoding UTF8 | Where-Object { $_.valid -eq 'true' -and $_.index -ne '0' })
    if (-not $rows.Count) { throw 'gpusteps.csv has no valid GPU marker intervals.' }
    $culture=[Globalization.CultureInfo]::InvariantCulture
    $table=foreach ($group in $rows | Group-Object name) {
        [pscustomobject]@{ step=$group.Name; frames=$group.Count
            gpuMeanMs=[Math]::Round(($group.Group | ForEach-Object { [double]::Parse($_.gpuSincePreviousMarkerMs,$culture) } | Measure-Object -Average).Average,3)
            cpuMeanMs=[Math]::Round(($group.Group | ForEach-Object { [double]::Parse($_.cpuSincePreviousMarkerMs,$culture) } | Measure-Object -Average).Average,3) }
    }
    Write-Host ($table | Sort-Object gpuMeanMs -Descending | Format-Table -AutoSize | Out-String)
}

function Invoke-PerfDiagnostic($Root, [string[]]$Scene, [string]$Tool, [IO.FileStream]$BuildLease, [string]$OutputRoot) {
    if ($Scene.Count -ne 1) { throw 'Diagnostics take exactly one -Scene.' }
    $plan=Get-Content -LiteralPath (Join-Path $Root 'scripts\perf\scenarios.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $item=@(Get-CollectionScenes $plan @('fixed','route','travel') $Scene)[0]
    $parent=Join-Path $OutputRoot 'diagnostics'
    $directory=Assert-UESTCChildPath (Join-Path $parent ([DateTime]::Now.ToString('yyyyMMddTHHmmss') + '-' + $item.scene.id + '-' + $Tool)) $parent
    $null=[IO.Directory]::CreateDirectory($directory)
    $lease=$null
    # DECISION-346: the inherited override is removed, and every child uses this checkout's own default cache.
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
        $traceTool=if ($Tool -eq 'EventPipe') { Get-UESTCDotnetTrace -Root $Root } else { $null }
        $parser=if ($traceTool) { Build-PerfCpuParse $Root $directory $traceTool } else { $null }
        $request=New-CollectionRequest $item
        # EventPipe stops its collector while the target is alive, so the game waits for the owner's window close.
        $request['externalCloseAfterSample']=($Tool -eq 'EventPipe')
        $requestPath=Join-Path $directory 'collection.json'
        Write-CollectionJson $requestPath $request
        $arguments=@(Get-CollectionArguments $Root $directory $plan $item $requestPath)
        if ($Tool -eq 'gpu-profile') { $arguments=@($arguments[0..1]) + @('--gpu-profile') + @($arguments[2..($arguments.Count-1)]) }
        $engine=Get-UESTCGodotHost -Godot $godot
        $log=Join-Path $directory 'game.log'
        switch ($Tool) {
            'gpu-profile' {
                $result=Invoke-UESTCProcess $godot $arguments $Root $log -TimeoutSeconds 600 -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
            }
            'Nsight' {
                # Windows CPU sampling/context-switch collection needs elevation. API tracing does not.
                $nsight=@('profile','--trace=dx12,vulkan,nvtx','--sample=none','--cpuctxsw=none','--wait=all','--kill=false',
                    ('--output=' + (Join-Path $directory 'graphics')),$engine) + $arguments
                $result=Invoke-UESTCProcess (Find-PerfNsight) $nsight $Root $log -TimeoutSeconds 600 -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
            }
            'EventPipe' {
                Write-CollectionJson (Join-Path $directory 'launch.json') ([ordered]@{ engine=$engine; arguments=$arguments; traceTool=$traceTool })
                $result=Invoke-UESTCProcess powershell @('-NoProfile','-File',(Join-Path $Root 'scripts\perf\diagnostic-capture.ps1'),'-Work',$directory) `
                    $Root (Join-Path $directory 'capture.log') -TimeoutSeconds 720 -IsolatedDesktop -Environment $environment -TextEncoding ([Text.Encoding]::UTF8)
                $result=[pscustomobject]@{ Output=[IO.File]::ReadAllText($log); Log=$log }
            }
        }
        Assert-UESTCCleanGodotLog $result
        $sessions=@(Get-ChildItem -LiteralPath $directory -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') })
        if ($sessions.Count -ne 1) { throw "Expected one closed recording in $directory." }
        Write-Host "Diagnostic output: $directory"
        switch ($Tool) {
            'gpu-profile' { Write-PerfGpuSteps $sessions[0].FullName }
            'Nsight' { Write-Host ('Nsight report: ' + (@(Get-ChildItem -LiteralPath $directory -File -Filter '*.nsys-rep').FullName -join ', ')) }
            'EventPipe' {
                $parsed=Invoke-UESTCProcess dotnet @($parser,'--perf-session',$directory,$sessions[0].FullName) $Root (Join-Path $directory 'cpu-parser.log') -TimeoutSeconds 600
                Write-Host $parsed.StdOut
            }
        }
    } finally {
        Exit-UESTCBuildLock $lease
        if ($null -eq $cache) { Remove-Item Env:UESTC_BAKE_CACHE -ErrorAction SilentlyContinue } else { $env:UESTC_BAKE_CACHE=$cache }
    }
}
