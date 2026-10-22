# 核心冒烟：在隔离桌面后台跑正式场景从标题经新游戏、行走、地图传送、返回标题与继续，再关闭存档和重启恢复，拒绝引擎错误/泄漏，报告实例参数需求与容量，供 G5 与 G20 核心共用。
# 产出：output/build/gates/；output/render-review/（核心专项日志、测试结果、隔离存档与截图）。
# 用法：由 scripts\gate.ps1 点引用（. scripts\gates\core-smoke.ps1）后调用 Invoke-UESTCMainlineSmoke 与 Invoke-UESTCSaveCore，不单独运行
. (Join-Path $PSScriptRoot '../lib/start-game.ps1')
if (-not (Get-Variable UESTCCoreSmokeResults -Scope Script -ErrorAction SilentlyContinue)) {
    $script:UESTCCoreSmokeResults = @{}
}

function New-UESTCCoreCloseObserver {
    param([Parameter(Mandatory)][hashtable]$Observation)
    $callback = {
        param([string]$OutputText, $Process)
        if ($Observation.Sent) { return }
        $ready = [regex]::Match($OutputText,'(?m)^CORE_READY_CLOSE (\d+)\r?$')
        if (-not $ready.Success) { return }
        $Observation.Sent = $true
        $Observation.At = $Process.PostClose([IntPtr]([long]$ready.Groups[1].Value))
    }
    return $callback.GetNewClosure()
}

function Invoke-UESTCMainlineSmoke {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][string]$Locks,
        [Parameter(Mandatory)][string]$Map, [IO.FileStream]$BuildLease = $null)
    $rootPath = [IO.Path]::GetFullPath($Root)
    $key = $rootPath + '|' + $Godot + '|' + $Map
    if ($script:UESTCCoreSmokeResults.ContainsKey($key)) { return $script:UESTCCoreSmokeResults[$key] }
    $tag = [Guid]::NewGuid().ToString('N').Substring(0,8)
    $work = Join-Path $rootPath ("output/build/gates/core-{0}" -f $tag)
    $shots = Join-Path $rootPath ("output/render-review/core-{0}" -f $tag)
    $saves = Join-Path $work 'saves'
    $null = [IO.Directory]::CreateDirectory($work)
    $null = [IO.Directory]::CreateDirectory($shots)
    $testEnvironment = New-UESTCTestEnvironment -Work $work
    $lease = $null
    try {
        if (-not $BuildLease) {
            $lease = Enter-UESTCBuildLock $Locks
            Update-UESTCGameBuild -Root $rootPath -Godot $Godot -BuildLease $lease -Environment $testEnvironment
        }
        $engineArgs = @('--path',(Join-Path $rootPath 'game'),'--rendering-method','forward_plus',
            '--rendering-driver','d3d12','--resolution','1280x720','--fixed-fps','60','--')
        $captureArgs = @("--save-dir=$saves","--out=$shots",'--shots=V1','--ui=1','--audio=off','--quality=fine')
        $closeObservation = @{ At=$null; Sent=$false }
        $closeObserver = New-UESTCCoreCloseObserver $closeObservation
        $close = Invoke-UESTCProcess $Godot ($engineArgs + $captureArgs + @('--gate=save-close','--title=1',
            '--save=1',"--map=$Map",'--flow=0','--seed=1','--weather-seed=1')) $rootPath (Join-Path $work 'save-close.log') -Visible -IsolatedDesktop -TimeoutSeconds 600 -ObserveOutput $closeObserver -Environment $testEnvironment
        $closeEnded = [Diagnostics.Stopwatch]::GetTimestamp()
        Assert-UESTCCleanGodotLog $close
        foreach ($marker in @('CORE_TITLE_READY','CORE_INPUT_READY','CORE_SETTINGS_READY','CORE_TRANSIT_READY','CORE_RETURN_TITLE_READY','CORE_CONTINUE_READY','CORE_ROUNDTRIP_READY')) {
            if (-not $close.StdOut.Contains($marker)) { throw "The production close process missed $marker." }
        }
        # GAME-128 / RESEARCH-034: normal input release alone does not prove directional motion.
        $movement = [regex]::Match($close.StdOut, '(?m)^CORE_INPUT_READY distance=([0-9.Ee+-]+) forward=([0-9.Ee+-]+)\r?$')
        if (-not $movement.Success) { throw 'The core walk check did not report directional displacement.' }
        $distance = [double]::Parse($movement.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
        $forward = [double]::Parse($movement.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture)
        if ([double]::IsNaN($distance) -or [double]::IsInfinity($distance) -or $distance -le 0 -or
            [double]::IsNaN($forward) -or [double]::IsInfinity($forward) -or $forward -le 0) {
            throw 'Normal forward input did not produce finite positive displacement in its expected direction.'
        }
        $loadingSequence = '(?s)STARTUP_PHASE title-ready [^\r\n]+.*STARTUP_PHASE new-game-enter [^\r\n]+.*STARTUP_PHASE loading-complete [^\r\n]+.*STARTUP_PHASE input-ready [^\r\n]*mode=Walk.*CORE_INPUT_READY.*CORE_TRANSIT_READY.*STARTUP_PHASE loading-complete [^\r\n]+.*CORE_RETURN_TITLE_READY.*STARTUP_PHASE loading-complete [^\r\n]+.*CORE_CONTINUE_READY'
        if ($close.StdOut -notmatch $loadingSequence) {
            throw 'The title/new-game/return/continue sequence did not complete each real loading progress bar.'
        }
        if ($null -eq $closeObservation.At) { throw 'The close process did not receive WM_CLOSE.' }
        # Same machine monotonic clock; includes the runner's final observation interval.
        $closeMs = ($closeEnded - [long]$closeObservation.At) * 1000.0 / [Diagnostics.Stopwatch]::Frequency
        if ($closeMs -lt 0 -or $closeMs -gt 30000) { throw "WM_CLOSE appears stuck ($closeMs ms)." }
        $baseline = [regex]::Match($close.StdOut,'(?m)^CORE_GENERATION (\d+)\r?$')
        if (-not $baseline.Success) { throw 'The close process did not expose its pre-exit generation.' }
        $generations = @(Get-ChildItem -LiteralPath (Join-Path $saves 's1') -Filter 'g*.sav' -File |
            ForEach-Object { if ($_.BaseName -match '^g(\d+)$') { [UInt64]$Matches[1] } })
        $latest = ($generations | Measure-Object -Maximum).Maximum
        if (-not $latest -or [UInt64]$latest -le [UInt64]$baseline.Groups[1].Value) {
            throw 'WM_CLOSE did not write a new save generation.'
        }
        $resumeObservation = @{ At=$null; Sent=$false }
        $resumeObserver = New-UESTCCoreCloseObserver $resumeObservation
        $resume = Invoke-UESTCProcess $Godot ($engineArgs + $captureArgs + @('--gate=save-resume','--title=0',
            '--slot=1','--save=off')) $rootPath (Join-Path $work 'save-resume.log') -Visible -IsolatedDesktop -TimeoutSeconds 600 -ObserveOutput $resumeObserver -Environment $testEnvironment
        Assert-UESTCCleanGodotLog $resume
        $loaded = [regex]::Match($resume.StdOut,'(?m)^CORE_GENERATION (\d+)\r?$')
        if (-not $loaded.Success -or [UInt64]$loaded.Groups[1].Value -ne [UInt64]$latest) {
            throw 'The second process did not validate and restore the newly written generation.'
        }
        if (-not $resume.StdOut.Contains('CORE_RUNTIME_MATCH')) {
            throw 'The second process did not confirm its restored core runtime state.'
        }
        $firstImage = Join-Path $shots 'save-close.png'
        $secondImage = Join-Path $shots 'save-resume.png'
        if (-not (Test-Path -LiteralPath $firstImage -PathType Leaf) -or -not (Test-Path -LiteralPath $secondImage -PathType Leaf)) {
            throw 'The production lifecycle did not capture both screenshots.'
        }
        $warningRatio = (Get-Content -Raw -LiteralPath (Join-Path $rootPath 'budgets.json') | ConvertFrom-Json).instanceShaderParameters.warningRatio
        $parameterUsage = [ordered]@{ WarningRatio=$warningRatio }
        $parameterWarnings = @()
        foreach ($sample in @(@{ Name='Close'; Process=$close }, @{ Name='Resume'; Process=$resume })) {
            $parameterMarker = [regex]::Match($sample.Process.StdOut, '(?m)^CORE_INSTANCE_PARAMETERS[ \t]+(.+)\r?$')
            if ($parameterMarker.Success) {
                $usage = ConvertFrom-Json -InputObject $parameterMarker.Groups[1].Value
                $parameterUsage[$sample.Name] = $usage
                if ($null -ne $usage.TrackedDeclaredDemandRatio -and $usage.TrackedDeclaredDemandRatio -gt $warningRatio) {
                    $message = "$($sample.Name): tracked instance-parameter demand $($usage.TrackedDeclaredParameterSlots)/$($usage.CapacitySlots) slots exceeds warning ratio $warningRatio; native actual remains unavailable."
                    $parameterWarnings += $message
                    Write-Warning $message -WarningAction Continue
                }
            } else {
                $parameterUsage[$sample.Name] = [pscustomobject]@{ NativeActualSlots=$null; Status='Unavailable'; Coverage='CORE_INSTANCE_PARAMETERS snapshot missing' }
            }
        }
        $parameterUsage['Warnings'] = $parameterWarnings
        $result = [pscustomobject]@{ Status='Passed';
            Scope='G5 Title/NewGame/Walk/SettingsOpenClose/ReturnToTitle/Continue/window-close save and restart restore'; CloseMilliseconds=$closeMs;
            DirectionalMovement=[pscustomobject]@{ DistanceMeters=$distance; ForwardProjectionMeters=$forward };
            Generation=[UInt64]$latest; PersistedFields='RuntimeMatched (CoreSnapshotsMatch spatial position tolerance; other fields exact)';
            Screenshot='Both captured for Opus visual review; no G7 pixel comparison or tolerance claim';
            InstanceShaderParameters=[pscustomobject]$parameterUsage;
            Logs=$work; Screenshots=$shots }
    } catch {
        $result = [pscustomobject]@{ Status='Failed'; Scope='G5 title/entry/return/continue/exit core'; Error=$_.Exception.Message; Logs=$work; Screenshots=$shots }
    } finally { if ($lease) { Exit-UESTCBuildLock $lease } }
    $script:UESTCCoreSmokeResults[$key] = $result
    return $result
}

function Invoke-UESTCSaveCore {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][string]$Locks,
        [Parameter(Mandatory)][string]$Map, [switch]$SaveTestsAlreadyPassed, [IO.FileStream]$BuildLease = $null)
    $rootPath = [IO.Path]::GetFullPath($Root)
    $work = Join-Path $rootPath 'output/build/gates/save-core'
    $testEnvironment = New-UESTCTestEnvironment -Work $work
    $lease = $null
    try {
        if (-not $BuildLease) {
            $lease = Enter-UESTCBuildLock $Locks
            $BuildLease = $lease
            Update-UESTCGameBuild -Root $rootPath -Godot $Godot -BuildLease $BuildLease -Environment $testEnvironment
        }
        if (-not $SaveTestsAlreadyPassed) {
            $settings = Join-Path $work 'save.runsettings'
            $null = Write-UESTCTestSettings -Path $settings -Root $rootPath -Godot $Godot -Environment $testEnvironment
            $trxName = 'save-core-' + [Guid]::NewGuid().ToString('N') + '.trx'
            $trxPath = Join-Path $work $trxName
            $saveTests = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug',
                '--settings',$settings,'--filter','FullyQualifiedName~Campus.Kernel.Save',
                '--logger',"trx;LogFileName=$trxName",'--results-directory',$work) $rootPath (Join-Path $work 'save-core.log') -TimeoutSeconds 600 -AllowFailure -Environment $testEnvironment
            $null = Assert-UESTCTestResult $saveTests $trxPath
        }
        $window = Invoke-UESTCMainlineSmoke -Root $rootPath -Godot $Godot -Locks $Locks -Map $Map -BuildLease $BuildLease
        return [pscustomobject]@{ Status=$window.Status; Scope='G20 roundtrip, migration, corruption and exit-save core';
            SaveTests=$(if ($SaveTestsAlreadyPassed) { 'Covered by this invocation G2' } else { 'Existing save suites passed' }); Window=$window }
    } finally { Exit-UESTCBuildLock $lease }
}
