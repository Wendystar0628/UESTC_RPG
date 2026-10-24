# 门禁总入口：按改动选出要跑的门禁，依次执行并汇报通过、失败或未完成。
# 产出：output/build/gates/、output/build/test-record.tsv
# 用法：powershell scripts\gate.ps1 [-Only G1,G2,... [-Full] | -Smoke | -Full] [-Plan] [-Paths <路径...>] [-BaseRef <基线>] [-Map qingshuihe] [-Frames 300]
#   默认：G1、受影响模块的 G2 和 G4，再加受影响的启动、行走、存档核心专项（核心专项驱动正式生产舞台）
#   -Plan 只读，只列出会跑什么；-Full 请求保留的 11 道门，没做完的门返回 NotImplemented
#   -Full 可与 -Only 组合限定门禁；选中 G2 时跑全量（含 Slow）及 NativeRender
#   退出码：0 请求范围内已实现的都通过；1 有失败；2 范围未完成
[CmdletBinding()]
param([string[]]$Only = @(), [switch]$Smoke, [switch]$Full,
    [switch]$Plan, [string[]]$Paths = @(), [string]$BaseRef = '',
    [string]$Map = 'qingshuihe', [int]$Frames = 300,
    [ValidateSet('Debug')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'lib/common.ps1')
. (Join-Path $PSScriptRoot 'lib/start-game.ps1')
. (Join-Path $PSScriptRoot 'gates/check-source.ps1')
. (Join-Path $PSScriptRoot 'gates/check-naming.ps1')
# Test budget (DECISION-246): quick-check tests over this many seconds are listed; the record keeps the recent runs only.
$slowTestSeconds = 2
$testRecordMaxRuns = 30
$testRecordMaxDays = 14
$testCutCandidateCount = 20
# Shared layer (contracts, planar geometry, public site tools, shared test fixtures): a change here selects every quick test.
$publicLayerPattern = '^game/(contracts|gen/bakeinput|gen/geometry|gen/sites|testing)/'
function Get-UESTCMainStage {
    param([string]$Root)
    $project = [IO.File]::ReadAllText((Join-Path $Root 'game/project.godot'))
    $application = [regex]::Match($project, '(?ms)^\[application\]\s*(.*?)(?=^\[|\z)').Groups[1].Value
    $sceneMatch = [regex]::Match($application, '(?m)^\s*run/main_scene\s*=\s*"res://([^"\r\n]+)"')
    if (-not $sceneMatch.Success) { return 'Unknown' }
    $scenePath = Assert-UESTCChildPath (Join-Path $Root ('game/' + $sceneMatch.Groups[1].Value)) (Join-Path $Root 'game')
    if (-not (Test-Path -LiteralPath $scenePath -PathType Leaf)) { return 'Unknown' }
    $scene = [IO.File]::ReadAllText($scenePath)
    $rootNode = [regex]::Match($scene, '(?ms)^\[node\b[^\r\n]*\]\s*(.*?)(?=^\[node\b|\z)').Groups[1].Value
    $scriptId = [regex]::Match($rootNode, '(?m)^\s*script\s*=\s*ExtResource\("([^"\r\n]+)"\)').Groups[1].Value
    if (-not $scriptId) { return 'Unknown' }
    foreach ($resource in [regex]::Matches($scene, '(?m)^\[ext_resource\s+[^\r\n]*\]')) {
        $id = [regex]::Match($resource.Value, '\bid="([^"]+)"').Groups[1].Value
        $path = [regex]::Match($resource.Value, '\bpath="([^"]+)"').Groups[1].Value
        if ($id -ne $scriptId) { continue }
        if ($path -match '^res://.+/KernelStage\.cs$') { return 'KernelStage' }
        return 'Unknown'
    }
    return 'Unknown'
}

function Get-UESTCAttributedChanges {
    param([string]$Root, [string]$BaseRef, [string[]]$Changes, [string]$Attribute)
    $code = @($Changes | Where-Object { $_ -match '^game/.*\.cs$' -and $_ -notmatch '^game/addons/|/(tests?|obj|bin|\.godot)/' })
    if (-not $code.Count) { return }
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    $basePaths = @(Get-UESTCGitPaths $Root @('ls-tree', '-z', '-r', '--name-only', $base))
    $pattern = '(?:\[|,)\s*(?:[A-Za-z_][\w.]*\.)?' + $Attribute + '(?:Attribute)?\b'
    foreach ($path in $code) {
        $absolute = Join-Path $Root $path
        $current = if (Test-Path -LiteralPath $absolute -PathType Leaf) { [IO.File]::ReadAllText($absolute) } else { '' }
        $old = if ($path -in $basePaths) { (Invoke-UESTCGit $Root @('show', "${base}:$path")).StdOut } else { '' }
        if ($current -match $pattern -or $old -match $pattern) { $path }
    }
}

function Get-UESTCG2CompileScope {
    param([string]$Root, [string]$Project, [string]$Configuration)
    try {
        # No target or restore: evaluate the same configuration used by dotnet test.
        $result = Invoke-UESTCProcess dotnet @('msbuild',(Join-Path $Root $Project),
            '-getItem:Compile',"-property:Configuration=$Configuration",'-nologo','-verbosity:quiet') $Root `
            -TextEncoding ([Text.Encoding]::UTF8)
        $evaluated = $result.StdOut | ConvertFrom-Json
        if (-not $evaluated -or -not $evaluated.PSObject.Properties['Items'] -or
            -not $evaluated.Items -or -not $evaluated.Items.PSObject.Properties['Compile']) {
            throw 'MSBuild output does not contain Items.Compile.'
        }
        $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
        $files = New-Object 'Collections.Generic.List[object]'
        $seen = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($item in @($evaluated.Items.Compile)) {
            if (-not $item -or -not $item.PSObject.Properties['FullPath'] -or
                [string]::IsNullOrWhiteSpace($item.FullPath) -or -not [IO.Path]::IsPathRooted($item.FullPath)) {
                throw 'MSBuild Compile item has no absolute FullPath.'
            }
            # FullPath is the physical source, including for linked/imported items.
            $fullPath = [IO.Path]::GetFullPath($item.FullPath)
            if (-not $seen.Add($fullPath)) { continue }
            $path = if ($fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                $fullPath.Substring($prefix.Length)
            } else { $fullPath }
            $files.Add([pscustomobject]@{ Path=$path.Replace('\','/'); FullPath=$fullPath })
        }
        return [pscustomobject]@{ CompileFiles=$files.ToArray() }
    } catch { throw "Cannot evaluate G2 Compile items for '${Project}': $($_.Exception.Message)" }
}

function Get-UESTCG2ProjectSelection {
    param([string]$Root, [string[]]$Changes, [bool]$ForceAll=$false, [string]$Configuration='Debug')
    $normalizedChanges = @($Changes | ForEach-Object { $_.Replace('\','/') })
    # Contract, build and shared data changes can affect both test hosts.
    $shared = @($normalizedChanges | Where-Object { $_ -match '^game/contracts/(?!Runtime/|tests/)|\.(props|targets|sln|slnx)$|^global\.json$|^game/(data/|style/palette/)' })
    foreach ($project in @('game/Campus.Gen.Tests.csproj','game/Campus.Tests.csproj')) {
        $scope = Get-UESTCG2CompileScope $Root $project $Configuration
        $reasons = New-Object 'Collections.Generic.List[string]'
        if ($ForceAll) { $reasons.Add('full G2 selection') }
        foreach ($path in $normalizedChanges) {
            if ($path -eq $project -or
                ($project -eq 'game/Campus.Tests.csproj' -and $path -eq 'game/Campus.csproj')) { $reasons.Add("Test host changed: $path") }
        }
        foreach ($path in $shared) { $reasons.Add("Shared dependency/project changed: $path") }
        $publicChanges = @($normalizedChanges | Where-Object { $_ -match $publicLayerPattern })
        if ($publicChanges.Count) { $reasons.Add("Shared layer changed: $($publicChanges[0]) ($($publicChanges.Count) files)") }
        if ($project -eq 'game/Campus.Tests.csproj') {
            foreach ($path in @($normalizedChanges | Where-Object { $_ -match '^game/contracts/Runtime/' -and $_ -notmatch '/tests/' })) {
                $reasons.Add("Runtime contract changed: $path")
            }
        }
        $compileFiles = @($scope.CompileFiles)
        foreach ($file in $compileFiles) {
            if ($file.Path -in $normalizedChanges -and
                [IO.File]::ReadAllText($file.FullPath) -notmatch '\[TestSuite(?:\(|\])') {
                # Shared support can be consumed across module boundaries or linked into both hosts.
                $reasons.Add("Shared test support changed: $($file.Path)")
            }
        }
        $allTests = $reasons.Count -gt 0
        $classes = New-Object 'Collections.Generic.List[string]'
        foreach ($file in $compileFiles) {
            # The aggregate gen/**/tests glob is not a module boundary. Each
            # test's own tests directory identifies the production module.
            $module = if ($file.Path -match '^(?<module>.+)/tests/') { $Matches.module } else { '' }
            $affected = $module -and @($normalizedChanges | Where-Object {
                $_ -ieq $module -or $_.StartsWith($module+'/', [StringComparison]::OrdinalIgnoreCase)
            }).Count -gt 0
            if (-not ($allTests -or $affected)) { continue }
            $source = [IO.File]::ReadAllText($file.FullPath)
            $namespace = [regex]::Match($source, '(?m)^\s*namespace\s+([\w.]+)\s*[;{]')
            $suites = [regex]::Matches($source, '(?s)\[TestSuite(?:\([^\]]*\))?\](?:\s*\[[^\]]*\])*\s*(?:(?:public|internal|sealed|partial|abstract)\s+)*class\s+(\w+)')
            foreach ($suite in $suites) {
                $class = $namespace.Groups[1].Value + '.' + $suite.Groups[1].Value
                if ($namespace.Success -and -not $classes.Contains($class)) { $classes.Add($class) }
            }
            if ($affected) { $reasons.Add("Changed module '$module'") }
        }
        $filter = 'TestCategory!=NativeRender&TestCategory!=G15Core'
        if (-not $ForceAll) { $filter += '&TestCategory!=Slow' }
        if (-not $allTests -and $classes.Count) {
            $filter += '&(' + (@($classes | Sort-Object | ForEach-Object { 'FullyQualifiedName~'+$_+'.' }) -join '|') + ')'
        }
        [pscustomobject]@{ Name=[IO.Path]::GetFileNameWithoutExtension($project); Project=$project;
            Selected=($allTests -or $classes.Count -gt 0); Reasons=@($reasons | Sort-Object -Unique); Filter=$filter;
            CompileFileCount=$compileFiles.Count;
            HasCompileFiles=($compileFiles.Count -gt 0) }
    }
}

function Get-UESTCTrxTests {
    param([string[]]$TrxPaths)
    foreach ($trxPath in $TrxPaths) {
        try { [xml]$trx = Get-Content -Raw -LiteralPath $trxPath }
        catch {
            # Result validation reports Incomplete; the optional timing record
            # must not replace that exception with another XML parse failure.
            Write-Warning "Cannot record test durations from invalid TRX: $trxPath"
            continue
        }
        foreach ($item in $trx.SelectNodes('//*[local-name()="UnitTestResult"]')) {
            $duration = $item.GetAttribute('duration')
            $seconds = if ($duration) { [TimeSpan]::Parse($duration, [Globalization.CultureInfo]::InvariantCulture).TotalSeconds } else { 0 }
            [pscustomobject]@{ Name=$item.GetAttribute('testName').Replace("`t",' '); Seconds=$seconds; Outcome=$item.GetAttribute('outcome') }
        }
    }
}

# Appends this run to the test record (time, name, seconds, outcome per row) and drops runs beyond the caps.
function Add-UESTCTestRecord {
    param([string]$Path, [object[]]$Tests)
    $encoding = New-Object Text.UTF8Encoding $false
    $stamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $cutoff = [DateTime]::UtcNow.AddDays(-$testRecordMaxDays).ToString('yyyy-MM-ddTHH:mm:ssZ')
    $old = if (Test-Path -LiteralPath $Path -PathType Leaf) { @([IO.File]::ReadAllLines($Path, $encoding)) } else { @() }
    $new = @($Tests | ForEach-Object { $stamp + "`t" + $_.Name + "`t" + $_.Seconds.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture) + "`t" + $_.Outcome })
    $rows = @(@($old) + $new | Where-Object { $_ -match "`t" })
    $runs = @($rows | ForEach-Object { $_.Substring(0, $_.IndexOf("`t")) } | Sort-Object -Unique -Descending | Select-Object -First $testRecordMaxRuns)
    $kept = New-Object 'Collections.Generic.HashSet[string]' (, [string[]]@($runs | Where-Object { $_ -ge $cutoff }))
    $rows = @($rows | Where-Object { $kept.Contains($_.Substring(0, $_.IndexOf("`t"))) })
    [IO.File]::WriteAllText($Path, (($rows -join "`n") + "`n"), $encoding)
}

function Write-UESTCSlowTests {
    param([object[]]$Tests)
    $slow = @($Tests | Where-Object { $_.Seconds -gt $slowTestSeconds } | Sort-Object Seconds -Descending)
    if (-not $slow.Count) { return }
    Write-Host "[G2] Tests over ${slowTestSeconds}s ($($slow.Count), notice only):"
    foreach ($test in $slow) { Write-Host ("  {0,7:N1}s  {1}" -f $test.Seconds, $test.Name) }
}

# Centralized run only: tests that never failed in the record, slowest on average first. Printed for the main session to decide; nothing is deleted.
function Write-UESTCCutCandidates {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $rows = @([IO.File]::ReadAllLines($Path, (New-Object Text.UTF8Encoding $false)) | Where-Object { $_ -match "`t" } | ForEach-Object {
        $cells = $_.Split("`t")
        [pscustomobject]@{ Run=$cells[0]; Name=$cells[1]; Seconds=[double]::Parse($cells[2], [Globalization.CultureInfo]::InvariantCulture); Outcome=$cells[3] }
    })
    $runCount = @($rows | Select-Object -ExpandProperty Run -Unique).Count
    $candidates = @($rows | Group-Object Name | Where-Object { -not @($_.Group | Where-Object { $_.Outcome -ne 'Passed' }).Count } |
        ForEach-Object { [pscustomobject]@{ Name=$_.Name; Average=($_.Group | Measure-Object Seconds -Average).Average; Runs=$_.Count } } |
        Sort-Object Average -Descending | Select-Object -First $testCutCandidateCount)
    Write-Host "[G2] Cut candidates: never failed in $runCount recorded runs, slowest $testCutCandidateCount on average (main session decides):"
    foreach ($candidate in $candidates) { Write-Host ("  {0,7:N2}s  x{1,-3} {2}" -f $candidate.Average, $candidate.Runs, $candidate.Name) }
}

$root = Split-Path $PSScriptRoot -Parent
$layout = Get-UESTCLayout $root
$game = Join-Path $root 'game'
$godot = Get-UESTCGodot
$work = Join-Path $root 'output/build/gates'
$testRecordPath = Join-Path $root 'output/build/test-record.tsv'
$branch = (Invoke-UESTCGit $root @('branch', '--show-current')).StdOut.Trim()
if (-not $BaseRef) {
    $BaseRef = 'main'
    if ($branch -eq 'main') {
        $upstream = (Invoke-UESTCGit $root @('for-each-ref', '--format=%(upstream:short)', 'refs/heads/main')).StdOut.Trim()
        if (-not $upstream.Trim()) { throw 'main has no upstream; supply -BaseRef with the last published commit.' }
        $BaseRef = $upstream.Trim()
    }
}
if ($Smoke -and ($Only.Count -gt 0 -or $Full)) { throw '-Smoke cannot be combined with -Only or -Full.' }
if ($Frames -lt 1) { throw 'Frames must be positive.' }
$changes = @(if ($PSBoundParameters.ContainsKey('Paths')) {
    $Paths | ForEach-Object {
        $absolute = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }))
        if (-not $absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Path escapes repository: $_"
        }
        $absolute.Substring($root.Length + 1).Replace('\','/')
    } | Sort-Object -Unique
} else {
    Get-UESTCChanges $root $BaseRef
})
$formatPaths = @($changes | Where-Object { $_ -match '\.cs$' -and $_ -notmatch '^game/addons/' -and (Test-Path -LiteralPath (Join-Path $root $_) -PathType Leaf) })
$runtimeChanges = @($changes | Where-Object { $_ -notmatch '^docs/|\.md$|^\.collab/' })
if (-not ($Full -or $Smoke -or $Only.Count)) {
    $existingBasePaths = @(Get-UESTCGitPaths $root @('ls-tree', '-z', '-r', '--name-only', $BaseRef))
    $runtimeChanges = @($runtimeChanges | Where-Object {
        $_ -notin $existingBasePaths -or -not (Test-UESTCCommentOnlyChange $root $_ $BaseRef)
    })
}
$textOnlyQuick = -not ($Full -or $Smoke -or $Only.Count) -and $changes.Count -gt 0 -and $runtimeChanges.Count -eq 0
$selected = New-Object 'Collections.Generic.List[string]'
$gateIds = @('G1','G2','G3','G4','G5','G7','G10','G14','G15','G20','G21')
if ($Only.Count) { foreach ($id in ($Only -join ',' -split ',')) { if ($id -cnotin $gateIds -and $id -cnotin @('G5Smoke','G15Core','G20Core')) { throw "Unknown gate: $id" }; if (-not $selected.Contains($id)) { $selected.Add($id) } } }
elseif ($Smoke) { $selected.Add('G5Smoke') }
elseif ($Full) { foreach ($id in $gateIds) { $selected.Add($id) }; foreach ($id in @('G5Smoke','G15Core','G20Core')) { $selected.Add($id) } }
elseif ($runtimeChanges.Count) {
    foreach ($id in @('G1','G2','G4')) { $selected.Add($id) }
}
elseif ($textOnlyQuick) { foreach ($id in @('G1','G2','G4')) { $selected.Add($id) } }
# Only changed attributed source files trigger startup; old content covers removals.
$mainStage = Get-UESTCMainStage $root
$startupChanges = @()
$walkChanges = @()
$saveChanges = @()
if (-not ($Full -or $Smoke -or $Only.Count)) {
    $startupChanges = @(Get-UESTCAttributedChanges $root $BaseRef $runtimeChanges 'Kernel(?:Service|Node)')
    $walkChanges = @($runtimeChanges | Where-Object {
        $_ -match '^game/(camera/walk/|kernel/collision/|gen/(collision|ground)/|contracts/((?:Runtime/)?Spatial/(Walk|WaterTraversal|Collision|Stair)|Foundation/Terrain))|^game/data/maps/[^/]+/(world/|nav/|waypoints\.json$|overlay/walkable\.overlay\.json$)'
    })
    $saveChanges = @(@($runtimeChanges | Where-Object {
        $_ -match '^game/(kernel/save/|contracts/(?:Runtime/)?Save/)|^game/.*Save[^/]*\.cs$'
    }) + @(Get-UESTCAttributedChanges $root $BaseRef $runtimeChanges 'SaveSection') | Sort-Object -Unique)
    if ($startupChanges.Count) { $selected.Add('G5Smoke') }
    if ($walkChanges.Count) { $selected.Add('G15Core') }
    if ($saveChanges.Count) { $selected.Add('G20Core') }
}
$scope = if ($Full) { 'Full' } elseif ($Only.Count) { 'Selected' } elseif ($Smoke) { 'Core' } else { 'Quick' }
$planObject = [ordered]@{ Scope = $scope; BaseRef = $BaseRef; Branch = $branch; Gates = $selected.ToArray(); Preparation = 'Ordinary Debug build and native incremental import before game consumers; selecting G1 builds Campus.sln including test projects once, otherwise Campus.csproj'; ChangedPaths = $changes; Godot = $godot; Locks = $layout.Locks; BuildCapacity = 1; FormatPaths = $formatPaths; Bake = 'No pre-bake in quick/core; centralized bake gates not implemented' }
$g2Selection = if ($selected.Contains('G2')) { @(Get-UESTCG2ProjectSelection $root $runtimeChanges ([bool]$Full) $Configuration) } else { @() }
$planObject['G2Selection'] = $g2Selection
$selectionIncomplete = @(
    if ($runtimeChanges.Count -and $selected.Count -eq 0) { 'Runtime changes selected no gates.' }
    foreach ($selection in @($g2Selection | Where-Object { $_.Selected -and -not $_.HasCompileFiles })) {
        "G2 selected project has no evaluated Compile items: $($selection.Project)"
    }
)
$planObject['SelectionStatus'] = if ($selectionIncomplete.Count) { 'Incomplete' } else { 'Ready' }
$planObject['IncompleteReasons'] = $selectionIncomplete
$planObject['Stage'] = $mainStage
$planObject['CoreChanges'] = [ordered]@{ Startup=$startupChanges; Walking=$walkChanges; Save=$saveChanges }
if ($Plan) { $planObject | ConvertTo-Json -Depth 8; return }
$gateLease = Enter-UESTCBuildLock $layout.Locks
try {
$null = [IO.Directory]::CreateDirectory($work)
$testEnvironment = New-UESTCTestEnvironment -Work $work
$results = New-Object 'Collections.Generic.List[object]'
$utf8 = New-Object Text.UTF8Encoding $false
$g2TrxPaths = New-Object 'Collections.Generic.List[string]'
$sourcePreflight = $false
$gamePrepared = $false
$gamePreparation = New-UESTCLaunchTiming
if ($runtimeChanges.Count -and $selected.Count -eq 0) {
    $results.Add([pscustomobject]@{ Gate='Selection'; Status='Incomplete'; Scope=$scope; Detail='Runtime changes selected no gates.' })
}

function Invoke-GateProcess {
    param([string]$Name, [string]$Executable, [string[]]$Arguments, [switch]$Visible,
        [int]$TimeoutSeconds = 1800, [Collections.IDictionary]$Environment = $testEnvironment)
    $isTest = $Executable -eq 'dotnet' -and $Arguments.Count -gt 0 -and $Arguments[0] -eq 'test'
    if ($isTest) {
        $trxName = "$Name-$([Guid]::NewGuid().ToString('N')).trx"
        $directoryIndex = [Array]::IndexOf($Arguments, '--results-directory')
        if ($directoryIndex -lt 0) { throw "${Name}: test results directory is required." }
        $trxPath = Join-Path $Arguments[$directoryIndex + 1] $trxName
        for ($index = 0; $index -lt $Arguments.Count - 1; $index++) {
            if ($Arguments[$index] -eq '--logger' -and $Arguments[$index + 1] -match '^trx(?:;|$)') {
                $Arguments[$index + 1] = "trx;LogFileName=$trxName"
            }
        }
    }
    $result = Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name.log") -Visible:$Visible -AllowFailure:$isTest -TimeoutSeconds $TimeoutSeconds -Environment $Environment
    if ($isTest) {
        # Failed runs still leave a TRX; the test record needs them.
        if ($Name -like 'G2-*' -and (Test-Path -LiteralPath $trxPath -PathType Leaf)) { $g2TrxPaths.Add($trxPath) }
        $executed = Assert-UESTCTestResult $result $trxPath
        $result | Add-Member -NotePropertyName Tests -NotePropertyValue $executed
    }
    return $result
}
function Assert-GodotVersion {
    if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Godot missing: $godot" }
    $version = Invoke-UESTCProcess $godot @('--version') $root (Join-Path $work 'godot-version.log') -Environment $testEnvironment
    $expected = (Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'tools/godot-version.txt')).Trim()
    if ($version.Output.Trim() -ne $expected) { throw "Godot version mismatch: expected $expected; got $($version.Output.Trim())" }
}
function Write-TestSettings {
    $settings = Join-Path $work 'tests.runsettings'
    return Write-UESTCTestSettings -Path $settings -Root $root -Godot $godot -Environment $testEnvironment
}

foreach ($id in $selected) {
    Write-Host "[$id] $scope"
    try {
        $detail = ''
        $status = 'Passed'
        if ($id -in @('G1','G3','G4') -and -not $sourcePreflight) {
            $null = Test-UESTCChangedText $root $changes $BaseRef
            $null = Test-UESTCScriptSyntax $root
            $null = Test-UESTCOwnershipRules $root
            $sourcePreflight = $true
        }
        if (-not $gamePrepared -and $id -in @('G1','G2','G5','G5Smoke','G15Core','G20Core')) {
            Assert-GodotVersion
            Update-UESTCGameBuild -Root $root -Godot $godot -BuildLease $gateLease -Timing $gamePreparation -Solution:($selected.Contains('G1')) -Environment $testEnvironment
            $gamePrepared = $true
        }
        switch ($id) {
            'G1' {
                $naming = Test-UESTCSourceNaming $root
                if ($formatPaths.Count) {
                    # G1 preparation has restored and compiled the whole solution, including test sources.
                    $formatArgs = @('format',(Join-Path $game 'Campus.sln'),'--verify-no-changes','--no-restore','--include') + @($formatPaths | ForEach-Object { Join-Path $root $_ })
                    $null = Invoke-GateProcess 'G1-format' dotnet $formatArgs
                }
                $formatDetail = "changed-source format verified ($($formatPaths.Count) files)"
                $detail = "Naming files=$($naming.CheckedFiles); Campus.sln build verified (including test projects); $formatDetail; import=$($gamePreparation.importInvoked)"
            }
            'G2' {
                $settings = Write-TestSettings
                $runCount = 0
                $testCount = 0
                $notes = @()
                try {
                foreach ($selection in @($g2Selection | Where-Object { $_.Selected })) {
                    $test = $selection.Name
                    $project = Join-Path $root $selection.Project
                    $notes += "${test}: " + ($selection.Reasons -join '; ')
                    if (-not $selection.HasCompileFiles) { $status = 'Incomplete'; $notes += "${test}: no evaluated Compile items"; continue }
                    # Native rendering/resource-lifetime tests are run explicitly under the build lock.
                    # Ordinary quick checks remain headless checks (DECISION-167).
                    $testRun = Invoke-GateProcess "G2-$test" dotnet @('test',$project,'-c',$Configuration,'--settings',$settings,'--filter',$selection.Filter,'--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $runCount++
                    $testCount += $testRun.Tests
                }
                } finally {
                    $g2Tests = @(Get-UESTCTrxTests $g2TrxPaths.ToArray())
                    if ($g2Tests.Count) { Add-UESTCTestRecord $testRecordPath $g2Tests }
                    Write-UESTCSlowTests $g2Tests
                }
                if (-not @($g2Selection | Where-Object { $_.Selected }).Count) {
                    $status = 'NotRun'
                    $notes += '无适用测试、未运行'
                } elseif ($runCount -eq 0) { $status = 'Incomplete' }
                if ($Full) {
                    Write-UESTCCutCandidates $testRecordPath
                    $native = & (Join-Path $PSScriptRoot 'gates/native-render.ps1') -Root $root -BuildLease $gateLease
                    $testCount += $native.Tests
                    $notes += "NativeRender: $($native.Tests) tests; $($native.Log)"
                }
                $detail = "Test projects run=$runCount; tests executed=$testCount. " + ($notes -join '; ')
            }
            'G3' { $detail = Test-UESTCOwnership $root $changes | ConvertTo-Json -Depth 5 -Compress }
            'G4' {
                $newFiles = Test-UESTCNewCodeFiles $root $changes -BaseRef $BaseRef
                $null = Test-UESTCSourceBoundaries $root
                $settings = Write-TestSettings
                $null = Invoke-GateProcess 'G4-architecture' dotnet @('test',(Join-Path $root 'scripts/gates/arch/Campus.Arch.Tests.csproj'),'-c','Debug','--settings',$settings,'--logger','trx','--results-directory',(Join-Path $work 'arch-results'))
                $detail = "New code placement/name checked ($($newFiles.CheckedNewCodeFiles) files); source and ArchUnitNET rules passed; BannedApiAnalyzers run in compilation."
            }
            'G5' {
                Assert-GodotVersion
                $r = Invoke-GateProcess 'G5-registry' $godot @('--headless','--path',$game,'--','--gate=registry','--map=*','--save=off','--audio=off')
                Assert-UESTCCleanGodotLog $r
                $registryReport = [regex]::Match($r.Output, '(?m)^G5_REGISTRY_RESULT[ \t]+(.+)$')
                if (-not $registryReport.Success) { throw 'The formal registry check did not return its result.' }
                $detail = ConvertFrom-Json -InputObject $registryReport.Groups[1].Value
            }
            'G5Smoke' {
                $coreScript = Join-Path $PSScriptRoot 'gates/core-smoke.ps1'
                if ($mainStage -ne 'KernelStage' -or -not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
                    $status = 'NotImplemented'
                    $detail = 'The startup/exit core requires the formal KernelStage and core-smoke entry.'
                } else {
                    . $coreScript
                    Assert-GodotVersion
                    $detail = Invoke-UESTCMainlineSmoke -Root $root -Godot $godot -Locks $layout.Locks -Map $Map -BuildLease $gateLease
                    if ($detail.Status -notin @('Passed','Failed','NotImplemented')) { throw 'The core entry returned an invalid status.' }
                    $status = $detail.Status
                }
            }
            'G7' {
                $status = 'NotImplemented'
                $detail = 'Fixed-pose overview, seasonal shots and three quality-preset overviews are centralized; no gate implementation yet.'
            }
            'G15Core' {
                Assert-GodotVersion
                # Session limits detect hangs, including when the workstation is busy.
                [xml]$coreSettings = Get-Content -Raw -LiteralPath (Write-TestSettings)
                $sessionTimeout = $coreSettings.CreateElement('TestSessionTimeout')
                $sessionTimeout.InnerText = '600000'
                $null = $coreSettings.RunSettings.RunConfiguration.AppendChild($sessionTimeout)
                $settings = Join-Path $work 'G15Core.runsettings'
                $coreSettings.Save($settings)
                $walkEnvironment = $testEnvironment.Clone()
                $walkEnvironment['UESTC_G15_MAP'] = $Map
                try {
                    $coreArguments = @('test',(Join-Path $game 'Campus.Tests.csproj'),'-c',$Configuration,'--settings',$settings,'--filter','TestCategory=G15Core','--logger','console;verbosity=detailed','--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $null = Invoke-GateProcess 'G15Core' dotnet $coreArguments -TimeoutSeconds 630 -Environment $walkEnvironment
                } catch {
                    if ($_.Exception.Data['Incomplete']) { throw }
                    throw ("G15Core nearby walking failed. " + $_.Exception.Message)
                }
                $detail = 'P01/P08 authored landing and nearby walking passed; at most 3x3 collision tiles. Full routes and whole-map checks were not run.'
            }
            'G20Core' {
                $coreScript = Join-Path $PSScriptRoot 'gates/core-smoke.ps1'
                if ($mainStage -ne 'KernelStage' -or -not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
                    $status = 'NotImplemented'
                    $detail = 'G20 core requires the formal KernelStage production lifecycle.'
                } else {
                    . $coreScript
                    $saveSuitesPassed = @($results | Where-Object { $_.Gate -eq 'G2' -and $_.Status -eq 'Passed' }).Count -gt 0 -and
                        @($g2Selection | Where-Object { $_.Selected -and $_.HasCompileFiles -and $_.Project -eq 'game/Campus.Tests.csproj' -and $_.Filter -notmatch 'FullyQualifiedName' }).Count -gt 0
                    $detail = Invoke-UESTCSaveCore -Root $root -Godot $godot -Locks $layout.Locks -Map $Map -SaveTestsAlreadyPassed:$saveSuitesPassed -BuildLease $gateLease
                    $status = $detail.Status
                }
            }
            'G10' {
                . (Join-Path $PSScriptRoot 'gates/performance.ps1')
                $detail = Invoke-UESTCPerformanceGate -Root $root -Map $Map -BuildLease $gateLease
                $status = $detail.Status
            }
            default { $status = 'NotImplemented'; $detail = 'Centralized or core-specialist gate has no implementation yet; no pre-bake was started.' }
        }
        $results.Add([pscustomobject]@{ Gate=$id; Status=$status; Scope=$scope; Detail=$detail })
        Write-Host "[$id] $status - $detail"
    } catch {
        $failureStatus = if ($_.Exception.Data['Incomplete']) { 'Incomplete' } else { 'Failed' }
        $results.Add([pscustomobject]@{ Gate=$id; Status=$failureStatus; Scope=$scope; Detail=$_.Exception.Message })
        Write-Warning "[$id] $($_.Exception.Message)"
        # Dependent later stages must not test stale binaries after a build/import failure.
        break
    }
}
$report = [ordered]@{ Plan=$planObject; Preparation=$gamePreparation; Results=$results.ToArray(); GeneratedAt=[DateTime]::UtcNow.ToString('o') }
$reportPath = Join-Path $work 'gate-result.json'
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 10), $utf8)
Write-Host "Report: $reportPath"
if (@($results | Where-Object { $_.Status -eq 'Failed' }).Count) { exit 1 }
if (@($results | Where-Object { $_.Status -in @('NotImplemented','Incomplete') }).Count) { exit 2 }
exit 0

} finally { Exit-UESTCBuildLock $gateLease }
