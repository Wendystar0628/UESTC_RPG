# 正式启动的内部共用步骤：只读 fingerprints host 的当前构建身份，在同一构建锁内更新并创建游戏进程。
# 用法：点引用 common.ps1 与本文件，再 Invoke-UESTCStartGame -Root <仓库> -Godot <引擎> [-BuildLease <已持有的锁>]。
# 产出：output/start_game/；output/.locks/build.lock（按需构建与导入后启动游戏）。
function Get-UESTCLaunchFiles {
    param([string]$Directory, [switch]$Import)
    foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($Directory)) {
        $name = [IO.Path]::GetFileName($entry)
        if ([IO.Directory]::Exists($entry)) {
            if ($name.StartsWith('.') -or $name -in @('obj', 'bin', 'tests', 'testing', 'bake')) { continue }
            if ($Import -and [IO.File]::Exists((Join-Path $entry '.gdignore'))) { continue }
            Get-UESTCLaunchFiles $entry -Import:$Import
        } else { [IO.FileInfo]::new($entry) }
    }
}

function Test-UESTCLaunchBuild {
    param([string]$Root)
    $Root = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $hostPath = Join-Path $Root 'scripts\build\fingerprints\bin\Campus.CodeFingerprints\Debug\net10.0\Campus.CodeFingerprints.dll'
    $receiptPath = Join-Path $Root 'output\start_game\build.succeeded'
    if (-not [IO.File]::Exists($hostPath) -or -not [IO.File]::Exists($receiptPath)) { return $false }
    try {
        $text = [IO.File]::ReadAllText($receiptPath)
        if ([string]::IsNullOrWhiteSpace($text)) { return $false }
        $receipt = $text | ConvertFrom-Json
        # A host and a success record in the current protocol are both required before a read-only check.
        if ($receipt.format -ne 2 -or $receipt.status -ne 'succeeded') { return $false }
        $arguments = @($hostPath, '--mode', 'check', '--project', (Join-Path $Root 'game\Campus.csproj'),
            '--tool-project', (Join-Path $Root 'scripts\build\fingerprints\Campus.CodeFingerprints.csproj'),
            '--output', (Join-Path $Root 'game\data\build-code-fingerprints.json'), '--property', 'Configuration=Debug')
        $result = Invoke-UESTCProcess 'dotnet' $arguments $Root -SuccessExitCodes @(0, 3)
        if ($result.StdErr.Trim()) { Write-Warning $result.StdErr.Trim() }
        return $result.ExitCode -eq 0
    } catch [IO.FileNotFoundException] { return $false }
    catch [IO.DirectoryNotFoundException] { return $false }
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
    foreach ($file in @(Get-UESTCLaunchFiles $game -Import)) {
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
        freshnessStartedTimestamp=$null; freshnessCompletedTimestamp=$null;
        buildInvoked=$null; importInvoked=$null; engineLaunchRequestedTimestamp=$null;
        engineStartUtc=$null; status='INITIALIZING' }
}

function Initialize-UESTCLaunch {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease, [Collections.IDictionary]$Timing = $null, [switch]$RequireFresh)
    if ($Timing) { $Timing['freshnessStartedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp() }
    $out = Join-Path $Root 'output\start_game'
    $null = [IO.Directory]::CreateDirectory($out)
    $built = -not (Test-UESTCLaunchBuild $Root)
    if ($Timing) { $Timing['buildInvoked'] = $false; $Timing['importInvoked'] = $false }
    if ($built -and $RequireFresh) { throw 'Existing build is stale; prepare it before the measurement.' }
    if ($built) {
        if ($Timing) { $Timing['buildInvoked'] = $true }
        Write-Host '构建有变化，编译中……'
        Invoke-UESTCProcess 'dotnet' @('build', "$Root\game\Campus.csproj", '-t:Rebuild', '-c', 'Debug', '-v', 'q') $Root (Join-Path $out 'build.log') | Out-Null
    }
    # A build can change generated includes without changing an RD main source.
    $imported = $built -or -not (Test-UESTCLaunchImport $Root $Godot)
    if ($imported -and $RequireFresh) { throw 'Existing import is stale; prepare it before the measurement.' }
    if ($imported) {
        if ($Timing) { $Timing['importInvoked'] = $true }
        Write-Host '导入资源有变化，导入中……'
        $import = Invoke-UESTCProcess $Godot @('--headless', '--editor', '--path', "$Root\game", '--', '--build-import') $Root (Join-Path $out 'import.log') -TimeoutSeconds 600
        Assert-UESTCCleanGodotLog $import
    }
    if ($Timing) { $Timing['freshnessCompletedTimestamp'] = [Diagnostics.Stopwatch]::GetTimestamp() }
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
