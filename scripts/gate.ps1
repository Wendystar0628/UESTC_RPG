# Default: G1, affected-module G2 and G4. Specialist checks are explicit.
# -Plan is read-only. Core scopes drive the formal production stage.
# -Full requests G1-G21; unfinished gates return NotImplemented and exit 2.
# Exit codes: 0 requested implemented scope passed; 1 failed; 2 incomplete scope.
# -NoCache forces fresh quick checks. full/window/export always run freshly.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^WP-\d{2}[a-z]?$')][string]$Wp,
    [string[]]$Only = @(), [switch]$Smoke, [switch]$Full,
    [switch]$Plan, [switch]$NoCache, [string[]]$Paths = @(), [string]$BaseRef = '',
    [string]$Map = 'qingshuihe', [int]$Frames = 300,
    [ValidateSet('Debug')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'lib/common.ps1')
. (Join-Path $PSScriptRoot 'gates/check-source.ps1')
. (Join-Path $PSScriptRoot 'gates/check-naming.ps1')
. (Join-Path $PSScriptRoot 'gates/cache.ps1')
. (Join-Path $PSScriptRoot 'gates/select-service-window.ps1')
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
    $shared = @($normalizedChanges | Where-Object { $_ -match '^game/contracts/(?!tests/)|^game/contracts/tests/PaletteFixture\.cs$|\.(props|targets|sln|slnx)$|^global\.json$|^game/(data/|style/palette/)' })
    foreach ($project in @('game/Campus.Gen.Tests.csproj','game/Campus.Tests.csproj')) {
        $scope = Get-UESTCG2CompileScope $Root $project $Configuration
        $reasons = New-Object 'Collections.Generic.List[string]'
        if ($ForceAll) { $reasons.Add('full G2 selection') }
        foreach ($path in $normalizedChanges) {
            if ($path -eq $project -or
                ($project -eq 'game/Campus.Tests.csproj' -and $path -eq 'game/Campus.csproj')) { $reasons.Add("Test host changed: $path") }
        }
        foreach ($path in $shared) { $reasons.Add("Shared dependency/project changed: $path") }
        $compileFiles = @($scope.CompileFiles)
        $allTests = $reasons.Count -gt 0
        $classes = New-Object 'Collections.Generic.List[string]'
        foreach ($file in $compileFiles) {
            # The aggregate gen/**/tests glob is not a module boundary. Each
            # test's own tests directory identifies the production module.
            if ($file.Path -notmatch '^(?<module>.+)/tests/') { continue }
            $module = $Matches.module
            $affected = @($normalizedChanges | Where-Object {
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

$root = Split-Path $PSScriptRoot -Parent
$layout = Get-UESTCLayout $root
$game = Join-Path $root 'game'
$godot = Get-UESTCGodot
$work = Join-Path $root 'output/build/gates'
$cacheWork = Join-Path $root 'output/cache/gates'
$branch = (Invoke-UESTCGit $root @('branch', '--show-current')).StdOut.Trim()
if (-not $BaseRef) {
    $BaseRef = 'main'
    if ($branch -eq 'main') {
        $upstream = (Invoke-UESTCGit $root @('for-each-ref', '--format=%(upstream:short)', 'refs/heads/main')).StdOut.Trim()
        if (-not $upstream.Trim()) { throw 'main has no upstream; supply -BaseRef with the last published commit.' }
        $BaseRef = $upstream.Trim()
    }
}
$modeFlags = @(($Only.Count -gt 0), [bool]$Smoke, [bool]$Full)
if (@($modeFlags | Where-Object { $_ }).Count -gt 1) { throw 'Choose only one of -Only, -Smoke, -Full.' }
if ($Frames -lt 1) { throw 'Frames must be positive.' }
$changes = @(if ($PSBoundParameters.ContainsKey('Paths')) {
    @($Paths | ForEach-Object {
        $absolute = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }))
        if (-not $absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Path escapes repository: $_"
        }
        $absolute.Substring($root.Length + 1).Replace('\','/')
    } | Sort-Object -Unique)
} else { Select-UESTCChanges $root $Wp @(Get-UESTCChanges $root $BaseRef) $BaseRef })
$formatPaths = @($changes | Where-Object { $_ -match '\.cs$' -and $_ -notmatch '^game/addons/' -and (Test-Path -LiteralPath (Join-Path $root $_) -PathType Leaf) })
$runtimeChanges = @($changes | Where-Object { $_ -notmatch '^docs/|\.md$|^\.collab/' })
if (-not ($Full -or $Smoke -or $Only.Count)) {
    $existingBasePaths = @(Get-UESTCGitLines $root @('ls-tree', '-r', '--name-only', $BaseRef))
    $runtimeChanges = @($runtimeChanges | Where-Object {
        $_ -notin $existingBasePaths -or -not (Test-UESTCCommentOnlyChange $root $_ $BaseRef)
    })
}
$importRequired = $Full -or @($changes | Where-Object { $_ -match '^game/' -and $_ -notmatch '^game/(contracts|gen)/' -and $_ -notmatch '\.(md|txt|csproj|sln|props|lock\.json)$' }).Count -gt 0
$selected = New-Object 'Collections.Generic.List[string]'
if ($Only.Count) { foreach ($id in ($Only -join ',' -split ',')) { if ($id -notmatch '^G(?:[1-9]|1[0-9]|2[01])$' -and $id -notin @('G5Smoke','G15Core','G20Core')) { throw "Unknown gate: $id" }; $selected.Add($id) } }
elseif ($Smoke) { $selected.Add('G5Smoke') }
elseif ($Full) { 1..21 | ForEach-Object { $selected.Add("G$_") }; $selected.Add('G5Smoke') }
elseif ($runtimeChanges.Count) {
    foreach ($id in @('G1','G2','G4')) { $selected.Add($id) }
}
# Service replacement is a required G5 subcheck, including explicitly selected G5.
$mainStage = Get-UESTCMainStage $root
$serviceImpact = if ($selected.Contains('G5')) { Get-UESTCServiceWindowImpact $root $BaseRef $changes } else { [pscustomobject]@{ Required=$false; ChangedCode=@(); Reasons=@() } }
$serviceWindowPlan = Select-UESTCServiceWindow $selected.ToArray() $serviceImpact.Required
$selected.Clear()
foreach ($gateId in $serviceWindowPlan.Gates) { $selected.Add($gateId) }
$cacheIds = @('G1','G2','G3','G4')
if ($serviceWindowPlan.CacheG5) { $cacheIds += 'G5' }
$scope = if ($Full) { 'Full' } elseif ($Only.Count) { 'Selected' } elseif ($Smoke) { 'Core' } else { 'Quick' }
$planObject = [ordered]@{ Scope = $scope; WorkPackage = $Wp; BaseRef = $BaseRef; Branch = $branch; Gates = $selected.ToArray(); ImportRequired = $importRequired; ChangedPaths = $changes; Godot = $godot; Locks = $layout.Locks; BuildCapacity = 1; FormatPaths = $formatPaths; Bake = 'No pre-bake in quick/core; centralized bake gates not implemented' }
$g2Selection = if ($selected.Contains('G2')) { @(Get-UESTCG2ProjectSelection $root $runtimeChanges ([bool]$Full) $Configuration) } else { @() }
$planObject['G2Selection'] = $g2Selection
$planObject['Stage'] = $mainStage
$planObject['ServiceWindow'] = [ordered]@{ Impact=$serviceImpact; RequireFormalCore=$serviceWindowPlan.RequireFormalCore; CacheG5=$serviceWindowPlan.CacheG5 }
if ($Plan) { $planObject | ConvertTo-Json -Depth 8; return }
$gateLease = Enter-UESTCBuildLock $layout.Locks
try {
$null = [IO.Directory]::CreateDirectory($work)
$null = [IO.Directory]::CreateDirectory($cacheWork)
$results = New-Object 'Collections.Generic.List[object]'
$utf8 = New-Object Text.UTF8Encoding $false
$cacheable = $selected.Count -gt 0 -and -not ($Full -or $NoCache)
$cachePaths = if ($cacheable) { @(Get-UESTCCachePaths $root $Wp $changes) } else { @() }
$cacheOptions = "$Configuration|map=$Map|frames=$Frames|import=$importRequired|G2=$(@($g2Selection | Where-Object Selected | ForEach-Object { $_.Project+':'+$_.Filter }) -join ',')"
$inputHash = if ($cacheable) { Get-UESTCInputHash $root $Wp $BaseRef $cacheOptions $godot $changes $cachePaths } else { '' }
$ownershipHash = if ($cacheable -and $selected.Contains('G3')) {
    # Ownership is checked for every changed path, including documentation and deletions.
    Get-UESTCStringHash ($inputHash + ($changes -join "`n") + (Invoke-UESTCGit $root @('show',"${BaseRef}:ownership.json")).StdOut +
        (Get-UESTCOwnershipUidHash $root $changes))
} else { '' }
$cachePath = Join-Path $cacheWork "$Wp-cache.json"
$cacheEntries = @{}
if ($cacheable -and (Test-Path -LiteralPath $cachePath)) {
    try {
        $savedCache = Get-Content -Raw -Encoding UTF8 -LiteralPath $cachePath | ConvertFrom-Json
        foreach ($entry in @($savedCache | Where-Object { $_.PSObject.Properties['version'] -and $_.version -eq 5 } | ForEach-Object entries)) {
            if ($entry.Status -ne 'Passed') { continue }
            $cacheEntries[$entry.Gate] = $entry
        }
    } catch { Write-Warning 'Unreadable gate cache ignored.' }
}

function Invoke-GateProcess {
    param([string]$Name, [string]$Executable, [string[]]$Arguments, [switch]$Visible,
        [int]$TimeoutSeconds = 1800)
    try { Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name.log") -Visible:$Visible -TimeoutSeconds $TimeoutSeconds }
    catch {
        # A known native import failure is retried once; ordinary failures and ERROR logs are never masked.
        if ($Name -ne 'G1-import' -or $_.Exception.Data['ExitCode'] -ne -1073741819) { throw }
        Write-Warning "Godot import exited 0xC0000005; retrying once. Original log preserved: $(Join-Path $work "$Name.log")"
        Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name-retry.log") -Visible:$Visible -TimeoutSeconds $TimeoutSeconds
    }
}
function Assert-GodotVersion {
    if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Godot missing: $godot" }
    $version = Invoke-UESTCProcess $godot @('--version') $root (Join-Path $work 'godot-version.log')
    $expected = (Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'tools/godot-version.txt')).Trim()
    if ($version.Output.Trim() -ne $expected) { throw "Godot version mismatch: expected $expected; got $($version.Output.Trim())" }
}
function Assert-CleanGodotLog {
    param($ProcessResult)
    if ($ProcessResult.Output -match '(?im)(?:SCRIPT )?ERROR:|shader[^\r\n]*(?:compilation failed|compile error)') { throw "Godot reported errors: $($ProcessResult.Log)" }
}
function Write-TestSettings {
    $settings = Join-Path $work 'tests.runsettings'
    $escapedGodot = [Security.SecurityElement]::Escape($godot)
    $escapedRoot = [Security.SecurityElement]::Escape($root)
    $xml = @"
<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount><TreatNoTestsAsError>true</TreatNoTestsAsError><EnvironmentVariables><GODOT_BIN>$escapedGodot</GODOT_BIN><UESTC_REPOSITORY>$escapedRoot</UESTC_REPOSITORY></EnvironmentVariables></RunConfiguration><GdUnit4><Parameters>--headless</Parameters><DisplayName>FullyQualifiedName</DisplayName><TolerateExitCodeAfterCompletion>false</TolerateExitCodeAfterCompletion></GdUnit4></RunSettings>
"@
    [IO.File]::WriteAllText($settings, $xml, $utf8)
    return $settings
}

foreach ($id in $selected) {
    Write-Host "[$id] $scope"
    try {
        $gateHash = if ($id -eq 'G3') { $ownershipHash } else { $inputHash }
        if ($cacheable -and $id -in $cacheIds -and $cacheEntries.ContainsKey($id)) {
            $entry = $cacheEntries[$id]
            $binaryHash = if ($id -eq 'G3') { 'source-only' } else { Get-UESTCBinaryHash $root $Wp $cachePaths }
            if ($binaryHash -and $entry.InputHash -eq $gateHash -and $entry.BinaryHash -eq $binaryHash) {
                $results.Add([pscustomobject]@{ Gate=$id; Status='CachedPass'; Scope=$scope; Detail=$entry.Detail })
                Write-Host "[$id] CachedPass (source, dependency, tool and binary fingerprints unchanged)"
                continue
            }
        }
        $cacheEntries.Remove($id)
        $detail = ''
        $status = 'Passed'
        switch ($id) {
            'G1' {
                $naming = Test-UESTCSourceNaming $root
                Assert-GodotVersion
                $null = Invoke-GateProcess 'G1-build' dotnet @('build',(Join-Path $game 'Campus.sln'),'-c',$Configuration,'-warnaserror')
                if ($formatPaths.Count) {
                    $formatArgs = @('format',(Join-Path $game 'Campus.sln'),'--verify-no-changes','--no-restore','--include') + @($formatPaths | ForEach-Object { Join-Path $root $_ })
                    $null = Invoke-GateProcess 'G1-format' dotnet $formatArgs
                }
                if ($importRequired) {
                    $r = Invoke-GateProcess 'G1-import' $godot @('--headless','--path',$game,'--import')
                    Assert-CleanGodotLog $r
                }
                $formatDetail = "changed-source format verified ($($formatPaths.Count) files)"
                $detail = "Naming files=$($naming.CheckedFiles); build verified; $formatDetail; import=$importRequired"
            }
            'G2' {
                $settings = Write-TestSettings
                $runCount = 0
                $notes = @()
                foreach ($selection in @($g2Selection | Where-Object { $_.Selected })) {
                    $test = $selection.Name
                    $project = Join-Path $root $selection.Project
                    $notes += "${test}: " + ($selection.Reasons -join '; ')
                    if (-not $selection.HasCompileFiles) { $notes += "${test}: Skipped (MSBuild evaluated no Compile items)"; continue }
                    # Native rendering/resource-lifetime tests are run explicitly under the build lock.
                    # Ordinary quick checks remain headless checks (D-167).
                    $null = Invoke-GateProcess "G2-$test" dotnet @('test',$project,'-c',$Configuration,'--settings',$settings,'--filter',$selection.Filter,'--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $runCount++
                }
                if ($runCount -eq 0) { $status = 'Skipped' }
                $detail = "Test projects run=$runCount. " + ($notes -join '; ')
            }
            'G3' { $detail = Test-UESTCOwnership $root $Wp $changes -BaseRef $BaseRef | ConvertTo-Json -Depth 5 -Compress }
            'G4' {
                $null = Test-UESTCSourceBoundaries $root
                $settings = Write-TestSettings
                $null = Invoke-GateProcess 'G4-architecture' dotnet @('test',(Join-Path $root 'scripts/gates/arch/Campus.Arch.Tests.csproj'),'-c','Debug','--settings',$settings,'--logger','trx','--results-directory',(Join-Path $work 'arch-results'))
                $detail = 'Source rules and ArchUnitNET rules passed; BannedApiAnalyzers run in compilation.'
            }
            'G5' {
                Assert-GodotVersion
                $r = Invoke-GateProcess 'G5-registry' $godot @('--headless','--path',$game,'--','--gate=registry','--map=*','--save=off','--audio=off')
                Assert-CleanGodotLog $r
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
                    $detail = Invoke-UESTCMainlineSmoke -Root $root -Godot $godot -Locks $layout.Locks -Wp $Wp -Map $Map -BuildLease $gateLease
                    if ($detail.Status -notin @('Passed','Failed','NotImplemented')) { throw 'The core entry returned an invalid status.' }
                    $status = $detail.Status
                }
            }
            'G6' {
                Assert-GodotVersion
                $release = Join-Path $root 'build/windows'
                $null = [IO.Directory]::CreateDirectory($release)
                $r = Invoke-GateProcess 'G6-export' $godot @('--path',$game,'--export-release','Windows Desktop',(Join-Path $release 'UESTC.exe')) -Visible
                Assert-CleanGodotLog $r
                if (-not (Test-Path -LiteralPath (Join-Path $release 'UESTC.exe'))) { throw 'Export command did not produce UESTC.exe.' }
                $bytes = (Get-ChildItem -LiteralPath $release -File -Recurse | Measure-Object Length -Sum).Sum
                $detail = "Windows Desktop release export; directory bytes=$bytes. Archive/PCK/license/production-budget checks: NotImplemented."
                $status = 'NotImplemented'
            }
            'G15Core' {
                Assert-GodotVersion
                # The daily walking core has a three-minute session limit.
                [xml]$coreSettings = Get-Content -Raw -LiteralPath (Write-TestSettings)
                $sessionTimeout = $coreSettings.CreateElement('TestSessionTimeout')
                $sessionTimeout.InnerText = '180000'
                $null = $coreSettings.RunSettings.RunConfiguration.AppendChild($sessionTimeout)
                $settings = Join-Path $work 'G15Core.runsettings'
                $coreSettings.Save($settings)
                $previousMap = [Environment]::GetEnvironmentVariable('UESTC_G15_MAP', 'Process')
                try {
                    [Environment]::SetEnvironmentVariable('UESTC_G15_MAP', $Map, 'Process')
                    $coreArguments = @('test',(Join-Path $game 'Campus.Tests.csproj'),'-c',$Configuration,'--settings',$settings,'--filter','TestCategory=G15Core','--logger','console;verbosity=detailed','--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $null = Invoke-GateProcess 'G15Core' dotnet $coreArguments -TimeoutSeconds 210
                } catch {
                    throw ("G15Core nearby walking failed. " + $_.Exception.Message)
                } finally { [Environment]::SetEnvironmentVariable('UESTC_G15_MAP', $previousMap, 'Process') }
                $detail = 'P01/P08 authored landing and nearby walking passed; at most 3x3 collision tiles. Full routes and whole-map checks were not run.'
            }
            'G20Core' {
                $coreScript = Join-Path $PSScriptRoot 'gates/core-smoke.ps1'
                if ($mainStage -ne 'KernelStage' -or -not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
                    $status = 'NotImplemented'
                    $detail = 'G20 core requires the formal KernelStage production lifecycle.'
                } else {
                    . $coreScript
                    $saveSuitesPassed = @($results | Where-Object { $_.Gate -eq 'G2' -and $_.Status -in @('Passed','CachedPass') }).Count -gt 0 -and
                        @($g2Selection | Where-Object { $_.Selected -and $_.HasCompileFiles -and $_.Project -eq 'game/Campus.Tests.csproj' -and $_.Filter -notmatch 'FullyQualifiedName' }).Count -gt 0
                    $detail = Invoke-UESTCSaveCore -Root $root -Godot $godot -Locks $layout.Locks -Wp $Wp -Map $Map -SaveTestsAlreadyPassed:$saveSuitesPassed -BuildLease $gateLease
                    $status = $detail.Status
                }
            }
            default { $status = 'NotImplemented'; $detail = 'Centralized or core-specialist gate has no implementation yet; no pre-bake was started.' }
        }
        $results.Add([pscustomobject]@{ Gate=$id; Status=$status; Scope=$scope; Detail=$detail })
        if ($cacheable -and $status -eq 'Passed' -and $id -in $cacheIds) {
            $binaryHash = if ($id -eq 'G3') { 'source-only' } else { Get-UESTCBinaryHash $root $Wp $cachePaths }
            $cacheEntries[$id] = [pscustomobject]@{ Gate=$id; Status='Passed'; InputHash=$gateHash; BinaryHash=$binaryHash; Detail=$detail }
        }
        Write-Host "[$id] $status - $detail"
    } catch {
        $cacheEntries.Remove($id)
        $results.Add([pscustomobject]@{ Gate=$id; Status='Failed'; Scope=$scope; Detail=$_.Exception.Message })
        Write-Warning "[$id] $($_.Exception.Message)"
        # Dependent later stages must not test stale binaries after a build/import failure.
        break
    }
}
if ($cacheable) {
    # If import or restore changed an input during this run, do not publish a stale pass.
    $afterPaths = @(Get-UESTCCachePaths $root $Wp $changes)
    $afterHash = Get-UESTCInputHash $root $Wp $BaseRef $cacheOptions $godot $changes $afterPaths
    if ($afterHash -eq $inputHash) { [IO.File]::WriteAllText($cachePath, (@{ version=5; entries=@($cacheEntries.Values) } | ConvertTo-Json -Depth 8), $utf8) }
}
$report = [ordered]@{ Plan=$planObject; Results=$results.ToArray(); CachePolicy='Package inputs and project dependencies, shared tools/options and scoped binaries; only completed passes reused; full/window/export always fresh'; GeneratedAt=[DateTime]::UtcNow.ToString('o') }
$reportPath = Join-Path $work "$Wp-result.json"
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 10), $utf8)
Write-Host "Report: $reportPath"
if (@($results | Where-Object { $_.Status -eq 'Failed' }).Count) { exit 1 }
if (@($results | Where-Object { $_.Status -eq 'NotImplemented' }).Count) { exit 2 }
exit 0

} finally { Exit-UESTCBuildLock $gateLease }
