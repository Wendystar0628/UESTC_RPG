# Default: changed-package quick checks, plus explicitly reported core-specialist work.
# -Plan is read-only. Core scopes drive the formal production stage.
# -Full requests G1-G21; unfinished gates return NotImplemented and exit 2.
# Exit codes: 0 requested implemented scope passed; 1 failed; 2 incomplete scope.
# -NoCache forces fresh quick checks. full/window/export always run freshly.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^WP-\d{2}[a-z]?$')][string]$Wp,
    [string[]]$Only = @(), [switch]$Smoke, [switch]$Full,
    [switch]$Plan, [switch]$NoCache, [switch]$SkipFormat, [string]$BaseRef = '',
    [string]$Map = 'qingshuihe', [int]$Frames = 300,
    [ValidateSet('Debug')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'wt/common.ps1')
. (Join-Path $PSScriptRoot 'gates/check-source.ps1')
. (Join-Path $PSScriptRoot 'gates/check-naming.ps1')
. (Join-Path $PSScriptRoot 'gates/cache.ps1')
. (Join-Path $PSScriptRoot 'gates/select-service-window.ps1')
function ConvertTo-UESTCG2RelativePath {
    param([string]$Base, [string]$Path)
    if ([IO.Path]::IsPathRooted($Path)) { throw 'Absolute Compile/change paths require explicit repository mapping.' }
    $parts = New-Object 'Collections.Generic.List[string]'
    foreach ($part in (($Base.Replace('\','/') + '/' + $Path.Replace('\','/')) -split '/')) {
        if (-not $part -or $part -eq '.') { continue }
        if ($part -eq '..') {
            if (-not $parts.Count -or $parts[$parts.Count-1] -match '[*?]') { throw 'Compile path escapes the repository or traverses a glob.' }
            $parts.RemoveAt($parts.Count-1)
        } else {
            if ($part.Contains(':')) { throw 'Absolute Compile paths require MSBuild evaluation.' }
            $parts.Add($part)
        }
    }
    return ($parts -join '/')
}

function Get-UESTCG2CompileScope {
    param([string]$Root, [string]$Project)
    $globs = New-Object 'Collections.Generic.List[string]'
    $roots = New-Object 'Collections.Generic.List[string]'
    $fallback = New-Object 'Collections.Generic.List[string]'
    try {
        $xml = New-Object Xml.XmlDocument
        $xml.XmlResolver = $null
        $xml.Load((Join-Path $Root $Project))
        $defaults = @($xml.SelectNodes("//*[local-name()='EnableDefaultCompileItems']"))
        if ($defaults.Count -ne 1 -or $defaults[0].InnerText.Trim() -ine 'false' -or
            @($defaults[0].SelectNodes('ancestor-or-self::*[@Condition]')).Count) {
            $fallback.Add('EnableDefaultCompileItems is not unconditionally false')
        }
        if (@($xml.SelectNodes("//*[local-name()='Import' or local-name()='Choose']")).Count) {
            $fallback.Add('Explicit Import/Choose may change Compile items')
        }
        if (@($xml.SelectNodes("//*[local-name()='Compile']//@*|//*[local-name()='Compile']//text()") | Where-Object { $_.Value -match '[\$@%]\(' }).Count) {
            $fallback.Add('Unevaluated MSBuild expression in Compile items')
        }
        $items = @($xml.SelectNodes("//*[local-name()='Compile']"))
        if (-not $items.Count) { $fallback.Add('No explicit Compile Include items') }
        $base = ($Project.Replace('\','/') -replace '/[^/]+$', '')
        foreach ($item in $items) {
            if (-not $item.HasAttribute('Include') -or $item.HasAttribute('Remove') -or
                $item.HasAttribute('Update') -or $item.HasAttribute('Exclude') -or
                @($item.SelectNodes('ancestor-or-self::*[@Condition]')).Count) {
                $fallback.Add('Conditional/Remove/Update/Exclude Compile items require evaluation')
            }
            foreach ($include in ($item.GetAttribute('Include') -split ';')) {
                $include = $include.Trim()
                if (-not $include) { continue }
                try {
                    if ($include -match '[\$@%]\(|%[0-9a-fA-F]{2}|[\[\]{}]') { throw 'MSBuild expression or escaped Compile path' }
                    $glob = ConvertTo-UESTCG2RelativePath $base $include
                    $globs.Add($glob)
                    $segments = @($glob -split '/')
                    $limit = $segments.Count - 1
                    for ($i=0; $i -lt $segments.Count; $i++) {
                        if ($segments[$i] -ieq 'tests' -or $segments[$i] -match '[*?]') { $limit=$i; break }
                    }
                    $production = if ($limit -gt 0) { ($segments[0..($limit-1)] -join '/') } else { '' }
                    if (-not $roots.Contains($production)) { $roots.Add($production) }
                } catch { $fallback.Add("Compile Include '$include': $($_.Exception.Message)") }
            }
        }
    } catch { $fallback.Add("Cannot statically read project: $($_.Exception.Message)") }
    return [pscustomobject]@{ Project=$Project; CompileGlobs=$globs.ToArray(); ProductionRoots=$roots.ToArray();
        ConservativeReasons=$fallback.ToArray() }
}

function Get-UESTCG2ProjectSelection {
    param([string]$Root, [string[]]$Changes, [string[]]$Files, [bool]$ForceAll=$false)
    $normalizedChanges = @($Changes | ForEach-Object { ConvertTo-UESTCG2RelativePath '' $_ })
    $normalizedFiles = @($Files | ForEach-Object { ConvertTo-UESTCG2RelativePath '' $_ })
    # Shared dependency/project edits retain the existing conservative all-project policy.
    $shared = @($normalizedChanges | Where-Object { $_ -match '^game/contracts/|\.(props|targets|sln|slnx)$|^global\.json$|^game/(data/|style/palette/)' })
    foreach ($project in @('game/Campus.Gen.Tests.csproj','game/Campus.Tests.csproj')) {
        $scope = Get-UESTCG2CompileScope $Root $project
        $reasons = New-Object 'Collections.Generic.List[string]'
        if ($ForceAll) { $reasons.Add('full G2 selection') }
        foreach ($path in $normalizedChanges) {
            if ($path -eq $project -or $path -eq ($project -replace '\.csproj$', '.packages.lock.json') -or
                ($project -eq 'game/Campus.Tests.csproj' -and $path -eq 'game/Campus.csproj')) { $reasons.Add("Test host changed: $path") }
        }
        foreach ($path in $shared) { $reasons.Add("Shared dependency/project changed: $path") }
        foreach ($reason in $scope.ConservativeReasons) { $reasons.Add("Conservative selection: $reason") }
        foreach ($path in $normalizedChanges) {
            foreach ($production in $scope.ProductionRoots) {
                if (-not $production -or $path -ieq $production -or $path.StartsWith($production+'/', [StringComparison]::OrdinalIgnoreCase)) {
                    $reasons.Add("Compile-derived production root '$production': $path")
                    break
                }
            }
        }
        $patterns = @($scope.CompileGlobs | ForEach-Object {
            $pattern = [regex]::Escape($_).Replace('\*\*/','(?:.*/)?').Replace('\*\*','.*').Replace('\*','[^/]*').Replace('\?','[^/]')
            New-Object Text.RegularExpressions.Regex ('^'+$pattern+'$'), ([Text.RegularExpressions.RegexOptions]::IgnoreCase)
        })
        $compileFiles = @($normalizedFiles | Where-Object {
            $file = $_
            foreach ($pattern in $patterns) {
                if ($pattern.IsMatch($file) -and (Test-Path -LiteralPath (Join-Path $Root $file) -PathType Leaf)) { return $true }
            }
            return $false
        })
        [pscustomobject]@{ Name=[IO.Path]::GetFileNameWithoutExtension($project); Project=$project;
            Selected=($reasons.Count -gt 0); Reasons=$reasons.ToArray();
            CompileGlobs=$scope.CompileGlobs; ProductionRoots=$scope.ProductionRoots;
            CompileFileCount=$compileFiles.Count;
            HasCompileFiles=($compileFiles.Count -gt 0 -or $scope.ConservativeReasons.Count -gt 0) }
    }
}

$root = Split-Path $PSScriptRoot -Parent
$layout = Get-UESTCLayout $root
$game = Join-Path $root 'game'
$godot = Get-UESTCGodot
$work = Join-Path $root 'output/build/gates'
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
$changes = @(Get-UESTCChanges $root $BaseRef)
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
    1..4 | ForEach-Object { $selected.Add("G$_") }
    if (Test-UESTCG5Impact $root $BaseRef $runtimeChanges) { $selected.Add('G5') }
    if (@($changes | Where-Object { $_ -match '^game/(flow|kernel|camera|stage)/|^game/project\.godot$' }).Count) { $selected.Add('G5Smoke') }
    if (@($changes | Where-Object { $_ -match '^game/(camera/walk|kernel/collision|gen/(collision|walkable))/' }).Count) { $selected.Add('G15Core') }
    if (@($changes | Where-Object { $_ -match '^game/kernel/save/|^game/contracts/Save/' }).Count) { $selected.Add('G20Core') }
}
# Service replacement is a required G5 subcheck, including explicitly selected G5.
$mainStage = Get-UESTCMainStage $root
$serviceImpact = if ($selected.Contains('G5')) { Get-UESTCServiceWindowImpact $root $BaseRef $changes } else { [pscustomobject]@{ Required=$false; ChangedCode=@(); Reasons=@() } }
$serviceWindowPlan = Select-UESTCServiceWindow $selected.ToArray() $serviceImpact.Required
$selected.Clear()
foreach ($gateId in $serviceWindowPlan.Gates) { $selected.Add($gateId) }
$cacheIds = @('G1','G2','G3','G4')
if ($SkipFormat) { $cacheIds = @($cacheIds | Where-Object { $_ -ne 'G1' }) }
if ($serviceWindowPlan.CacheG5) { $cacheIds += 'G5' }
$scope = if ($Full) { 'Full' } elseif ($Only.Count) { 'Selected' } elseif ($Smoke) { 'Core' } else { 'Quick' }
$planObject = [ordered]@{ Scope = $scope; WorkPackage = $Wp; BaseRef = $BaseRef; Branch = $branch; Gates = $selected.ToArray(); ImportRequired = $importRequired; ChangedPaths = $changes; Godot = $godot; Locks = $layout.Locks; CpuLimit = [Math]::Max(2,[Math]::Floor([Environment]::ProcessorCount/8.0)); Bake = 'No pre-bake in quick/core; centralized bake gates not implemented' }
$g2Selection = if ($selected.Contains('G2')) { @(Get-UESTCG2ProjectSelection $root $runtimeChanges @(Get-UESTCFiles $root) ([bool]$Full)) } else { @() }
$planObject['G2Selection'] = $g2Selection
$planObject['SkipFormat'] = [bool]$SkipFormat
$planObject['Stage'] = $mainStage
$planObject['ServiceWindow'] = [ordered]@{ Impact=$serviceImpact; RequireFormalCore=$serviceWindowPlan.RequireFormalCore; CacheG5=$serviceWindowPlan.CacheG5 }
if ($Plan) { $planObject | ConvertTo-Json -Depth 8; return }
$null = [IO.Directory]::CreateDirectory($work)
$results = New-Object 'Collections.Generic.List[object]'
$utf8 = New-Object Text.UTF8Encoding $false
$cacheable = $selected.Count -gt 0 -and -not ($Full -or $NoCache)
$cachePaths = if ($cacheable) { @(Get-UESTCCachePaths $root $Wp) } else { @() }
$cacheOptions = "$Configuration|map=$Map|frames=$Frames|import=$importRequired|format=$(-not $SkipFormat)|G2=$(@($g2Selection | Where-Object Selected | ForEach-Object Project) -join ',')"
$inputHash = if ($cacheable) { Get-UESTCInputHash $root $Wp $BaseRef $cacheOptions $godot $changes $cachePaths } else { '' }
$ownershipHash = if ($cacheable -and $selected.Contains('G3')) {
    # Ownership is checked for every changed path, including documentation and deletions.
    Get-UESTCStringHash ($inputHash + ($changes -join "`n") + (Invoke-UESTCGit $root @('show',"${BaseRef}:ownership.json")).StdOut +
        (Get-UESTCOwnershipUidHash $root))
} else { '' }
$cachePath = Join-Path $work "$Wp-cache.json"
$cacheEntries = @{}
if ($cacheable -and (Test-Path -LiteralPath $cachePath)) {
    try {
        $savedCache = Get-Content -Raw -Encoding UTF8 -LiteralPath $cachePath | ConvertFrom-Json
        foreach ($entry in @($savedCache | Where-Object { $_.PSObject.Properties['version'] -and $_.version -eq 4 } | ForEach-Object entries)) {
            if ($entry.Status -ne 'Passed') { continue }
            if ($SkipFormat -and $entry.Gate -eq 'G1') { continue }
            $cacheEntries[$entry.Gate] = $entry
        }
    } catch { Write-Warning 'Unreadable gate cache ignored.' }
}

function Invoke-LockedProcess {
    param([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$Kind = 'Cpu', [switch]$Visible,
        [int]$TimeoutSeconds = 1800)
    $lease = Enter-UESTCResourceLock $layout.Locks $Kind
    try {
        try { Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name.log") -Visible:$Visible -TimeoutSeconds $TimeoutSeconds }
        catch {
            # A known native import failure is retried once; ordinary failures and ERROR logs are never masked.
            if ($Name -ne 'G1-import' -or $_.Exception.Data['ExitCode'] -ne -1073741819) { throw }
            Write-Warning "Godot import exited 0xC0000005; retrying once. Original log preserved: $(Join-Path $work "$Name.log")"
            Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name-retry.log") -Visible:$Visible -TimeoutSeconds $TimeoutSeconds
        }
    }
    finally { Exit-UESTCResourceLock $lease }
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
                $null = Invoke-LockedProcess 'G1-build' dotnet @('build',(Join-Path $game 'Campus.sln'),'-c',$Configuration,'-warnaserror')
                if (-not $SkipFormat) {
                    $null = Invoke-LockedProcess 'G1-format' dotnet @('format',(Join-Path $game 'Campus.sln'),'--verify-no-changes','--no-restore','--exclude',(Join-Path $game 'addons'))
                }
                if ($importRequired) {
                    $r = Invoke-LockedProcess 'G1-import' $godot @('--headless','--path',$game,'--import')
                    Assert-CleanGodotLog $r
                }
                $formatDetail = if ($SkipFormat) { 'format skipped' } else { 'first-party format verified' }
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
                    if (-not $selection.HasCompileFiles) { $notes += "${test}: Skipped (no files match explicit Compile Include)"; continue }
                    # Native rendering/resource-lifetime tests are run explicitly with a GPU lease.
                    # Ordinary merge quick checks remain headless CPU checks (D-167).
                    $null = Invoke-LockedProcess "G2-$test" dotnet @('test',$project,'-c',$Configuration,'--settings',$settings,'--filter','TestCategory!=NativeRender&TestCategory!=G15Core','--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $runCount++
                }
                if ($runCount -eq 0) { $status = 'Skipped' }
                $detail = "Test projects run=$runCount. " + ($notes -join '; ')
            }
            'G3' { $detail = Test-UESTCOwnership $root $Wp $changes -BaseRef $BaseRef | ConvertTo-Json -Depth 5 -Compress }
            'G4' {
                $null = Test-UESTCSourceBoundaries $root
                $settings = Write-TestSettings
                $null = Invoke-LockedProcess 'G4-architecture' dotnet @('test',(Join-Path $root 'scripts/gates/arch/Campus.Arch.Tests.csproj'),'-c','Debug','--settings',$settings,'--logger','trx','--results-directory',(Join-Path $work 'arch-results'))
                $detail = 'Source rules and ArchUnitNET rules passed; BannedApiAnalyzers run in compilation.'
            }
            'G5' {
                Assert-GodotVersion
                $r = Invoke-LockedProcess 'G5-registry' $godot @('--headless','--path',$game,'--','--gate=registry','--map=*','--save=off','--audio=off')
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
                    $detail = Invoke-UESTCMainlineSmoke -Root $root -Godot $godot -Locks $layout.Locks -Wp $Wp -Map $Map
                    if ($detail.Status -notin @('Passed','Failed','NotImplemented')) { throw 'The core entry returned an invalid status.' }
                    $status = $detail.Status
                }
            }
            'G6' {
                Assert-GodotVersion
                $release = Join-Path $root 'build/windows'
                $null = [IO.Directory]::CreateDirectory($release)
                $r = Invoke-LockedProcess 'G6-export' $godot @('--path',$game,'--export-release','Windows Desktop',(Join-Path $release 'UESTC.exe')) GpuExclusive -Visible
                Assert-CleanGodotLog $r
                if (-not (Test-Path -LiteralPath (Join-Path $release 'UESTC.exe'))) { throw 'Export command did not produce UESTC.exe.' }
                $bytes = (Get-ChildItem -LiteralPath $release -File -Recurse | Measure-Object Length -Sum).Sum
                $detail = "Windows Desktop release export; directory bytes=$bytes. Archive/bake-verify/PCK/license/production-budget checks: NotImplemented."
                $status = 'NotImplemented'
            }
            'G15Core' {
                Assert-GodotVersion
                # The complete-map core gets its own bounded session; ordinary G2 settings stay unchanged.
                [xml]$coreSettings = Get-Content -Raw -LiteralPath (Write-TestSettings)
                $sessionTimeout = $coreSettings.CreateElement('TestSessionTimeout')
                $sessionTimeout.InnerText = '1320000'
                $null = $coreSettings.RunSettings.RunConfiguration.AppendChild($sessionTimeout)
                $settings = Join-Path $work 'G15Core.runsettings'
                $coreSettings.Save($settings)
                $previousMap = [Environment]::GetEnvironmentVariable('UESTC_G15_MAP', 'Process')
                try {
                    [Environment]::SetEnvironmentVariable('UESTC_G15_MAP', $Map, 'Process')
                    $coreArguments = @('test',(Join-Path $game 'Campus.Tests.csproj'),'-c',$Configuration,'--settings',$settings,'--filter','TestCategory=G15Core','--logger','console;verbosity=detailed','--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $null = Invoke-LockedProcess 'G15Core' dotnet $coreArguments Cpu -TimeoutSeconds 1500
                } catch {
                    throw ("G15Core FirstFourPassed=false; whole-tile mount CPU and first actual Jolt-step timing are deferred. " + $_.Exception.Message)
                } finally { [Environment]::SetEnvironmentVariable('UESTC_G15_MAP', $previousMap, 'Process') }
                # This batch implements the first four checks; it does not sign off the remaining G15 scope.
                $status = 'NotImplemented'
                $detail = [pscustomobject]@{ FirstFourPassed=$true; Deferred=@('Whole-tile native construction/mount CPU budget','First actual Jolt-step spike') }
            }
            'G20Core' {
                $coreScript = Join-Path $PSScriptRoot 'gates/core-smoke.ps1'
                if ($mainStage -ne 'KernelStage' -or -not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
                    $status = 'NotImplemented'
                    $detail = 'G20 core requires the formal KernelStage production lifecycle.'
                } else {
                    . $coreScript
                    $saveSuitesPassed = @($results | Where-Object { $_.Gate -eq 'G2' -and $_.Status -in @('Passed','CachedPass') }).Count -gt 0 -and
                        @($g2Selection | Where-Object { $_.Selected -and $_.HasCompileFiles -and $_.Project -eq 'game/Campus.Tests.csproj' }).Count -gt 0
                    $detail = Invoke-UESTCSaveCore -Root $root -Godot $godot -Locks $layout.Locks -Wp $Wp -Map $Map -SaveTestsAlreadyPassed:$saveSuitesPassed
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
    $afterHash = Get-UESTCInputHash $root $Wp $BaseRef $cacheOptions $godot $changes
    if ($afterHash -eq $inputHash) { [IO.File]::WriteAllText($cachePath, (@{ version=4; entries=@($cacheEntries.Values) } | ConvertTo-Json -Depth 8), $utf8) }
}
$report = [ordered]@{ Plan=$planObject; Results=$results.ToArray(); CachePolicy='Package inputs and project dependencies, shared tools/options and scoped binaries; only completed passes reused; full/window/export always fresh'; GeneratedAt=[DateTime]::UtcNow.ToString('o') }
$reportPath = Join-Path $work "$Wp-result.json"
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 10), $utf8)
Write-Host "Report: $reportPath"
if (@($results | Where-Object { $_.Status -eq 'Failed' }).Count) { exit 1 }
if (@($results | Where-Object { $_.Status -eq 'NotImplemented' }).Count) { exit 2 }
exit 0
