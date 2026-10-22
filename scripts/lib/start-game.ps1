# 正式启动的内部共用步骤：同一构建锁内只查已集成产物，存在即启动；首次缺失与显式准备共用普通构建及原生增量导入。
# 用法：点引用 common.ps1 与本文件，再 Invoke-UESTCStartGame -Root <仓库> -Godot <引擎> [-BuildLease <已持有的锁>]。
# 产出：output/start_game/；output/.locks/build.lock（默认玩上次集成版本，工作区半成品不触发编译）。
# DECISION-256: keep the script-entry and engine clocks separate; external lock wait is never fabricated.
function New-UESTCLaunchTiming {
    param([DateTime]$ScriptEnteredUtc = [DateTime]::UtcNow,
        [long]$ScriptEnteredTimestamp = [Diagnostics.Stopwatch]::GetTimestamp())
    return [ordered]@{ scriptEnteredUtc=$ScriptEnteredUtc.ToString('O'); scriptEnteredTimestamp=$ScriptEnteredTimestamp;
        stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency; lockWaitStatus='NOT_MEASURED';
        lockRequestedTimestamp=$null; lockAcquiredTimestamp=$null;
        outputCheckStartedTimestamp=$null; outputCheckCompletedTimestamp=$null;
        preparationStartedTimestamp=$null; preparationCompletedTimestamp=$null;
        buildInvoked=$null; importInvoked=$null; engineLaunchRequestedTimestamp=$null;
        engineStartUtc=$null; status='INITIALIZING' }
}

# DECISION-302：Godot 正式 Debug 输出约定仅在这里定义，启动不查询 MSBuild。
function Get-UESTCGameAssemblyPath {
    param([Parameter(Mandatory)][string]$Root)
    return Join-Path $Root 'game\.godot\mono\temp\bin\Debug\Campus.dll'
}

# 默认启动只判三类正式产物存在；不读取内容、时间戳或工作区输入。
function Test-UESTCLaunchOutputs {
    param([Parameter(Mandatory)][string]$Root)
    return ([IO.File]::Exists((Get-UESTCGameAssemblyPath -Root $Root)) -and
        [IO.File]::Exists((Join-Path $Root 'game\data\build-code-fingerprints.json')) -and
        [IO.Directory]::Exists((Join-Path $Root 'game\.godot\imported')) -and
        [IO.File]::Exists((Join-Path $Root 'game\.godot\editor\filesystem_cache10')) -and
        [IO.File]::Exists((Join-Path $Root 'game\.godot\uid_cache.bin')))
}

# 显式准备与首次缺产物走同一链；普通构建失败直接交出，不改成 Rebuild。
function Update-UESTCGameBuild {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease, [Collections.IDictionary]$Timing = $null,
        [switch]$Solution)
    if ($Timing) {
        $Timing['preparationStartedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
        $Timing['preparationCompletedTimestamp'] = $null
        $Timing['buildInvoked'] = $false
        $Timing['importInvoked'] = $false
    }
    $out = Join-Path $Root 'output\start_game'
    $null = [IO.Directory]::CreateDirectory($out)
    Write-Host '准备游戏，增量编译中……'
    if ($Timing) { $Timing['buildInvoked'] = $true }
    $target = if ($Solution) { 'game\Campus.sln' } else { 'game\Campus.csproj' }
    Invoke-UESTCProcess 'dotnet' @('build', (Join-Path $Root $target), '-c', 'Debug', '-warnaserror', '-v', 'q') $Root (Join-Path $out 'build.log') | Out-Null
    Write-Host '准备资源，原生增量导入中……'
    if ($Timing) { $Timing['importInvoked'] = $true }
    try {
        $import = Invoke-UESTCProcess $Godot @('--headless', '--editor', '--path', (Join-Path $Root 'game'), '--', '--build-import') $Root (Join-Path $out 'import.log') -TimeoutSeconds 600
        Assert-UESTCCleanGodotLog $import
    } catch {
        $importError = $_
        try {
            # 失效默认启动会检查的原生缓存，让下次启动重走已有准备链。
            [IO.File]::Delete((Join-Path $Root 'game\.godot\editor\filesystem_cache10'))
        } catch {
            throw [AggregateException]::new('资源导入与缓存失效均失败。', [Exception[]]@($importError.Exception, $_.Exception))
        }
        throw
    }
    if ($Timing) { $Timing['preparationCompletedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp() }
}

# DECISION-302：默认启动只看正式产物存在；不读取工作区输入、回执或导入元数据内容。
function Initialize-UESTCLaunch {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease, [Collections.IDictionary]$Timing = $null)
    if ($Timing) {
        $Timing['buildInvoked'] = $false
        $Timing['importInvoked'] = $false
        $Timing['outputCheckStartedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    }
    $exists = Test-UESTCLaunchOutputs -Root $Root
    if ($Timing) { $Timing['outputCheckCompletedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp() }
    if ($exists) { return }
    Update-UESTCGameBuild -Root $Root -Godot $Godot -BuildLease $BuildLease -Timing $Timing
}

function Invoke-UESTCStartGame {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [string[]]$GameArgs = @(), [IO.FileStream]$BuildLease = $null,
        [DateTime]$ScriptEnteredUtc = [DateTime]::UtcNow,
        [long]$ScriptEnteredTimestamp = [Diagnostics.Stopwatch]::GetTimestamp())
    $Root = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $timing = New-UESTCLaunchTiming -ScriptEnteredUtc $ScriptEnteredUtc -ScriptEnteredTimestamp $ScriptEnteredTimestamp
    $lease = $null
    try {
        if (-not $BuildLease) {
            $timing['lockRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
            $lease = Enter-UESTCBuildLock (Get-UESTCLayout $Root).Locks
            $timing['lockAcquiredTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
            $timing['lockWaitStatus'] = 'MEASURED_IN_TOTAL'
        } else { $timing['lockWaitStatus'] = 'ALREADY_HELD; external wait NOT_MEASURED' }
        $heldLease = if ($BuildLease) { $BuildLease } else { $lease }
        Initialize-UESTCLaunch -Root $Root -Godot $Godot -BuildLease $heldLease -Timing $timing
        $window = $Godot -replace '_console\.exe$', '.exe'
        if (-not [IO.File]::Exists($window)) { $window = $Godot }
        $arguments = @('--path', "$Root\game")
        if ($GameArgs.Count) { $arguments += @('--') + $GameArgs }
        $commandLine = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
        $timing['engineLaunchRequestedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
        $process = Start-Process -FilePath $window -ArgumentList $commandLine -WorkingDirectory $Root -WindowStyle Normal -PassThru
        $timing['engineStartUtc'] = $process.StartTime.ToUniversalTime().ToString('O')
        $timing['gamePid'] = $process.Id
        $timing['menuRequested'] = -not ($GameArgs -contains '--title=0')
        $timing['status'] = 'ENGINE_STARTED'
        return $process
    } catch { $timing['status'] = 'FAILED'; throw }
    finally {
        try {
            $out = Join-Path $Root 'output\start_game'
            $null = [IO.Directory]::CreateDirectory($out)
            $timing | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'launch.json') -Encoding UTF8
        } finally { Exit-UESTCBuildLock $lease }
    }
}
