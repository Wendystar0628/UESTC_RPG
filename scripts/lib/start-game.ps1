# 正式启动的内部共用步骤：按已有构建与导入记录判鲜，在同一构建锁内更新并创建游戏进程。
# 用法：点引用 common.ps1 与本文件，再 Invoke-UESTCStartGame -Root <仓库> -Godot <引擎> [-BuildLease <已持有的锁>]。
# 产出：output/start_game/；output/.locks/build.lock（按需构建与导入后启动游戏）。
function Get-UESTCLaunchFiles {
    param([string]$Directory, [switch]$Import)
    foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($Directory)) {
        $name = [IO.Path]::GetFileName($entry)
        if ([IO.Directory]::Exists($entry)) {
            if ($name.StartsWith('.') -or $name -in @('obj', 'bin', 'bake', 'tests', 'testing')) { continue }
            if ($Import -and [IO.File]::Exists((Join-Path $entry '.gdignore'))) { continue }
            Get-UESTCLaunchFiles $entry -Import:$Import
        } else { [IO.FileInfo]::new($entry) }
    }
}

function Test-UESTCLaunchBuild {
    param([string]$Root, [IO.FileInfo[]]$Files)
    $game = Join-Path $Root 'game'
    $output = Join-Path $game '.godot\mono\temp\bin\Debug'
    $assembly = Join-Path $output 'Campus.dll'
    $graphPath = Join-Path $game 'obj\Campus\Campus.csproj.nuget.dgspec.json'
    if (-not [IO.File]::Exists($assembly) -or -not [IO.File]::Exists($graphPath)) { return $false }
    $fingerprints = Join-Path $game 'data\build-code-fingerprints.json'
    $fingerprintsStamp = Join-Path $game '.godot\mono\temp\obj\Debug\CodeFingerprints.succeeded'
    if (-not [IO.File]::Exists($fingerprints) -or -not [IO.File]::Exists($fingerprintsStamp)) { return $false }
    if ([IO.File]::GetLastWriteTimeUtc($assembly) -gt [IO.File]::GetLastWriteTimeUtc($fingerprintsStamp)) { return $false }
    $builtAt = [IO.File]::GetLastWriteTimeUtc($assembly)
    # A successful fingerprint build can leave both the game DLL and JSON unchanged.
    if ([IO.File]::GetLastWriteTimeUtc($fingerprintsStamp) -gt $builtAt) { $builtAt = [IO.File]::GetLastWriteTimeUtc($fingerprintsStamp) }
    if ([IO.File]::GetLastWriteTimeUtc($fingerprints) -gt [IO.File]::GetLastWriteTimeUtc($fingerprintsStamp)) { return $false }
    # A successful no-op build can leave the DLL timestamp unchanged. One empty
    # local stamp records that success; it contains no source hashes or gate results.
    $stamp = Join-Path $Root 'output\start_game\build.succeeded'
    if ([IO.File]::Exists($stamp) -and [IO.File]::GetLastWriteTimeUtc($stamp) -gt $builtAt) { $builtAt = [IO.File]::GetLastWriteTimeUtc($stamp) }
    $graph = Get-Content -LiteralPath $graphPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $inputs = [Collections.Generic.List[IO.FileInfo]]::new()
    $packages = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($project in $graph.projects.PSObject.Properties) {
        $path = $project.Name
        if (-not [IO.File]::Exists($path)) { return $false }
        $inputs.Add((Get-Item -LiteralPath $path))
        $restore = $project.Value.restore
        $assetsPath = Join-Path $restore.outputPath 'project.assets.json'
        if (-not [IO.File]::Exists($assetsPath)) { return $false }
        $inputs.Add((Get-Item -LiteralPath $assetsPath))
        foreach ($config in $restore.configFilePaths) {
            if (-not [IO.File]::Exists($config)) { return $false }
            $inputs.Add((Get-Item -LiteralPath $config))
        }
        $name = [IO.Path]::GetFileNameWithoutExtension($path)
        $projectAssembly = Join-Path $output "$name.dll"
        if (-not [IO.File]::Exists($projectAssembly) -or [IO.File]::GetLastWriteTimeUtc($projectAssembly) -gt [IO.File]::GetLastWriteTimeUtc($fingerprintsStamp)) { return $false }
        $intermediate = if ($name -eq 'Campus') { Join-Path $game '.godot\mono\temp\obj\Debug' }
            else { Join-Path $restore.outputPath 'Debug\net10.0' }
        $list = Join-Path $intermediate "$name.csproj.FileListAbsolute.txt"
        if (-not [IO.File]::Exists($list)) { return $false }
        # MSBuild owns the output list. Old checkout entries can remain in it;
        # only outputs belonging to this checkout participate in this launch.
        foreach ($written in [IO.File]::ReadLines($list)) {
            if ($written.StartsWith($Root + '\', [StringComparison]::OrdinalIgnoreCase) -and -not [IO.File]::Exists($written)) { return $false }
        }
        $assets = Get-Content -LiteralPath $assetsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            $found = $false
            foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
                $package = Join-Path $folder $library.Value.path
                if (-not [IO.Directory]::Exists($package)) { continue }
                $found = $true
                if ($packages.Add($package)) {
                    foreach ($file in $library.Value.files) {
                        if ($file -notmatch '\.(dll|props|targets)$') { continue }
                        $dependency = Join-Path $package $file
                        if (-not [IO.File]::Exists($dependency) -or [IO.File]::GetLastWriteTimeUtc($dependency) -gt $builtAt) { return $false }
                    }
                }
                break
            }
            if (-not $found) { return $false }
        }
    }
    foreach ($name in @('global.json', 'scripts\gates\shader-whitelist.json', 'docs\ROUTES.json', 'version.txt', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'scripts\build\build-info.ps1', 'scripts\build\generate-style-shaders.py', 'game\style\palette\core.json', 'game\addons\build_import\plugin.cfg', 'game\addons\build_import\plugin.gd', 'scripts\build\fingerprints\Campus.CodeFingerprints.csproj', 'scripts\setup\install-test-runner.ps1')) {
        $inputs.Add((Get-Item -LiteralPath (Join-Path $Root $name)))
    }
    foreach ($name in @('NuGet.Config', 'nuget.config', '.editorconfig')) {
        $path = Join-Path $Root $name
        if ([IO.File]::Exists($path)) { $inputs.Add([IO.FileInfo]::new($path)) }
    }
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Root 'scripts\gates\arch') -Filter 'BannedSymbols.*.txt' -File) { $inputs.Add($file) }
    $sourceDirectories = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $Files) {
        if ($file.Extension -in @('.cs', '.props', '.targets', '.ruleset', '.glsl', '.glslinc', '.gdshaderinc') -or $file.Name -match '^(\.editorconfig|BannedSymbols.*\.txt|packages\.lock\.json|nuget\.config)$') {
            $inputs.Add($file)
            if ($file.Extension -in @('.cs', '.glsl', '.glslinc', '.gdshaderinc')) {
                $directory = $file.DirectoryName
                while ($directory.Length -gt $Root.Length -and $sourceDirectories.Add($directory)) { $directory = [IO.Path]::GetDirectoryName($directory) }
            }
        }
    }
    $fingerprintTool = Join-Path $Root 'scripts\build\fingerprints'
    foreach ($file in @(Get-UESTCLaunchFiles $fingerprintTool)) {
        if ($file.Extension -in @('.cs', '.csproj', '.props', '.targets')) {
            $inputs.Add($file)
            $directory = $file.DirectoryName
            while ($directory.Length -ge $fingerprintTool.Length -and $sourceDirectories.Add($directory)) { $directory = [IO.Path]::GetDirectoryName($directory) }
        }
    }
    foreach ($buildInput in $inputs) { if ($buildInput.LastWriteTimeUtc -gt $builtAt) { return $false } }
    # Directory write times also detect deleted/renamed sources, which a scan of
    # the surviving build inputs alone would miss. Unrelated resource trees are excluded.
    foreach ($directory in $sourceDirectories) { if ([IO.Directory]::GetLastWriteTimeUtc($directory) -gt $builtAt) { return $false } }
    return $true
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

function Initialize-UESTCLaunch {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][IO.FileStream]$BuildLease)
    $out = Join-Path $Root 'output\start_game'
    $files = @(Get-UESTCLaunchFiles (Join-Path $Root 'game'))
    $built = -not (Test-UESTCLaunchBuild $Root $files)
    if ($built) {
        Write-Host '构建有变化，编译中……'
        Invoke-UESTCProcess 'dotnet' @('build', "$Root\game\Campus.csproj", '-c', 'Debug', '-v', 'q') $Root (Join-Path $out 'build.log') | Out-Null
    }
    # A build can change generated includes without changing an RD main source.
    if ($built -or -not (Test-UESTCLaunchImport $Root $Godot)) {
        Write-Host '导入资源有变化，导入中……'
        $import = Invoke-UESTCProcess $Godot @('--headless', '--editor', '--path', "$Root\game", '--', '--build-import') $Root (Join-Path $out 'import.log') -TimeoutSeconds 600
        Assert-UESTCCleanGodotLog $import
    }
    # Import can create .uid/.import files beside sources. Record success after
    # that step too, so generated sidecars do not cause another no-op build.
    if ($built) { [IO.File]::WriteAllText((Join-Path $out 'build.succeeded'), '') }
}

function Invoke-UESTCStartGame {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [string[]]$GameArgs = @(), [IO.FileStream]$BuildLease = $null)
    $Root = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $lease = $null
    try {
        if (-not $BuildLease) { $lease = Enter-UESTCBuildLock (Get-UESTCLayout $Root).Locks }
        $heldLease = if ($BuildLease) { $BuildLease } else { $lease }
        Initialize-UESTCLaunch -Root $Root -Godot $Godot -BuildLease $heldLease
        $window = $Godot -replace '_console\.exe$', '.exe'
        if (-not [IO.File]::Exists($window)) { $window = $Godot }
        $arguments = @('--path', "$Root\game")
        if ($GameArgs.Count) { $arguments += @('--') + $GameArgs }
        $commandLine = ($arguments | ForEach-Object { ConvertTo-UESTCProcessArgument $_ }) -join ' '
        return Start-Process -FilePath $window -ArgumentList $commandLine -WorkingDirectory $Root -WindowStyle Normal -PassThru
    } finally { Exit-UESTCBuildLock $lease }
}
