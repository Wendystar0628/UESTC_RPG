# 正式启动的内部共用步骤：同一构建锁内只查已集成产物，存在即启动；首次缺失与显式准备共用普通构建及按需导入。
# 用法：点引用 common.ps1 与本文件，再 Invoke-UESTCStartGame -Root <仓库> -Godot <引擎> [-BuildLease <已持有的锁>]。
# 产出：output/start_game/；output/.locks/build.lock（默认玩上次集成版本，工作区半成品不触发编译）。
function Get-UESTCLaunchImportFiles {
    param([string]$Directory)
    foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($Directory)) {
        $name = [IO.Path]::GetFileName($entry)
        if ([IO.Directory]::Exists($entry)) {
            if ($name.StartsWith('.') -or $name -in @('obj', 'bin', 'tests', 'testing', 'bake')) { continue }
            if ([IO.File]::Exists((Join-Path $entry '.gdignore'))) { continue }
            Get-UESTCLaunchImportFiles $entry
        } else { [IO.FileInfo]::new($entry) }
    }
}

function Test-UESTCLaunchImport {
    param([string]$Root, [string]$Godot)
    $game = Join-Path $Root 'game'
    $cache = Join-Path $game '.godot\editor\filesystem_cache10'
    if (-not [IO.File]::Exists($cache) -or -not [IO.File]::Exists((Join-Path $game '.godot\uid_cache.bin'))) { return $false }
    if ([IO.File]::GetLastWriteTimeUtc((Join-Path $game 'project.godot')) -gt [IO.File]::GetLastWriteTimeUtc($cache) -or
        [IO.File]::GetLastWriteTimeUtc($Godot) -gt [IO.File]::GetLastWriteTimeUtc($cache)) { return $false }
    $records = @{}
    $directory = ''
    foreach ($line in [IO.File]::ReadLines($cache)) {
        $parts = $line -split '::'
        if ($line.StartsWith('::res://')) { $directory = $parts[1].Substring(6); continue }
        if ($parts.Count -ge 6) { $records[$directory + $parts[0]] = $parts }
    }
    foreach ($relative in $records.Keys) {
        if ($records[$relative][4] -ne '0' -and -not [IO.File]::Exists((Join-Path $game $relative))) { return $false }
    }
    # The filesystem cache records both source and .import mtimes, as integer
    # Unix seconds. Read that Godot metadata instead of hashing every asset.
    $importedAt = [IO.File]::GetLastWriteTimeUtc($cache)
    $epoch = [DateTime]::SpecifyKind([DateTime]'1970-01-01', [DateTimeKind]::Utc)
    foreach ($file in @(Get-UESTCLaunchImportFiles $game)) {
        if ($file.Extension -eq '.import') {
            if (-not [IO.File]::Exists($file.FullName.Substring(0, $file.FullName.Length - 7))) { return $false }
            continue
        }
        # Includes have no .import sidecar. The existing scan timestamp is only
        # a cheap trigger; dependency selection belongs to the editor import step.
        if ($file.Extension -in @('.glslinc', '.gdshaderinc') -and $file.LastWriteTimeUtc -gt $importedAt) { return $false }
        $relative = $file.FullName.Substring($game.Length + 1).Replace('\', '/')
        $sidecar = $file.FullName + '.import'
        if (-not [IO.File]::Exists($sidecar)) {
            # New resources are absent from the existing scan. Code, raw data
            # and directly loaded shaders/scenes do not require an import pass.
            if ($file.Extension -match '^\.(csv|svg|png|jpe?g|webp|bmp|tga|hdr|exr|ktx|dds|wav|ogg|mp3|ttf|otf|woff2?|glsl|gltf|glb|fbx|blend|obj)$') { return $false }
            continue
        }
        if (-not $records.ContainsKey($relative)) { return $false }
        $record = $records[$relative]
        if ($record[5] -ne '1' -or [long][Math]::Floor(($file.LastWriteTimeUtc - $epoch).TotalSeconds) -ne [long]$record[3] -or
            [long][Math]::Floor(([IO.File]::GetLastWriteTimeUtc($sidecar) - $epoch).TotalSeconds) -ne [long]$record[4]) { return $false }
        $text = [IO.File]::ReadAllText($sidecar)
        foreach ($match in [regex]::Matches($text, '"res://([^"\r\n]+)"')) {
            if (-not [IO.File]::Exists((Join-Path $game $match.Groups[1].Value))) { return $false }
        }
    }
    return $true
}

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

# 显式准备与首次缺产物走同一链；普通构建失败直接交出，不改成 Rebuild。
function Update-UESTCGameBuild {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease, [Collections.IDictionary]$Timing = $null)
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
    Invoke-UESTCProcess 'dotnet' @('build', (Join-Path $Root 'game\Campus.csproj'), '-c', 'Debug', '-warnaserror', '-v', 'q') $Root (Join-Path $out 'build.log') | Out-Null
    if (-not (Test-UESTCLaunchImport $Root $Godot)) {
        Write-Host '导入资源有变化，导入中……'
        if ($Timing) { $Timing['importInvoked'] = $true }
        $import = Invoke-UESTCProcess $Godot @('--headless', '--editor', '--path', (Join-Path $Root 'game'), '--', '--build-import') $Root (Join-Path $out 'import.log') -TimeoutSeconds 600
        Assert-UESTCCleanGodotLog $import
    }
    if ($Timing) { $Timing['preparationCompletedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp() }
}

# DECISION-302：默认启动只看正式程序集存在；不读取工作区输入、回执或导入元数据。
function Initialize-UESTCLaunch {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease, [Collections.IDictionary]$Timing = $null)
    if ($Timing) {
        $Timing['buildInvoked'] = $false
        $Timing['importInvoked'] = $false
        $Timing['outputCheckStartedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp()
    }
    $exists = [IO.File]::Exists((Get-UESTCGameAssemblyPath -Root $Root))
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
