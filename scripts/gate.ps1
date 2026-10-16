# Default: changed-package quick checks, plus explicitly reported core-specialist work.
# -Plan is read-only. -D0 is the WP-00 build/import/registry/window/export chain.
# -Full requests G1-G21; unfinished gates return NotImplemented and exit 2.
# Exit codes: 0 requested implemented scope passed; 1 failed; 2 incomplete scope.
# -NoCache forces fresh quick checks. D0/full/window/export always run freshly.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^WP-\d{2}[a-z]?$')][string]$Wp,
    [string[]]$Only = @(), [switch]$Smoke, [switch]$Full, [switch]$D0,
    [switch]$Plan, [switch]$NoCache, [string]$BaseRef = '',
    [string]$Map = 'qingshuihe', [int]$Frames = 300,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
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
        if (@($xml.SelectNodes('//@*|//text()') | Where-Object { $_.Value -match '[\$@%]\(' }).Count) {
            $fallback.Add('Unevaluated MSBuild expression in project metadata')
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
    $shared = @($normalizedChanges | Where-Object { $_ -match '^game/contracts/|\.(props|targets|csproj|sln|slnx)$|^global\.json$' })
    foreach ($project in @('game/Campus.Gen.Tests.csproj','game/Campus.Tests.csproj')) {
        $scope = Get-UESTCG2CompileScope $Root $project
        $reasons = New-Object 'Collections.Generic.List[string]'
        if ($ForceAll) { $reasons.Add('D0/full/explicit G2 selection') }
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
$modeFlags = @(($Only.Count -gt 0), [bool]$Smoke, [bool]$Full, [bool]$D0)
if (@($modeFlags | Where-Object { $_ }).Count -gt 1) { throw 'Choose only one of -Only, -Smoke, -Full, -D0.' }
if ($Frames -lt 1) { throw 'Frames must be positive.' }
if ($D0 -and $Wp -ne 'WP-00') { throw '-D0 belongs to WP-00.' }
$changes = @(Get-UESTCChanges $root $BaseRef)
$runtimeChanges = @($changes | Where-Object { $_ -notmatch '^docs/|\.md$|^\.collab/' })
if (-not ($D0 -or $Full -or $Smoke -or $Only.Count)) {
    $existingBasePaths = @(Get-UESTCGitLines $root @('ls-tree', '-r', '--name-only', $BaseRef))
    $runtimeChanges = @($runtimeChanges | Where-Object {
        $_ -notin $existingBasePaths -or -not (Test-UESTCCommentOnlyChange $root $_ $BaseRef)
    })
}
$importRequired = $D0 -or $Full -or @($changes | Where-Object { $_ -match '^game/' -and $_ -notmatch '^game/(contracts|gen)/' -and $_ -notmatch '\.(md|txt|csproj|sln|props|lock\.json)$' }).Count -gt 0
$selected = New-Object 'Collections.Generic.List[string]'
if ($Only.Count) { foreach ($id in ($Only -join ',' -split ',')) { if ($id -notmatch '^G(?:[1-9]|1[0-9]|2[01])$') { throw "Unknown gate: $id" }; $selected.Add($id) } }
elseif ($Smoke) { $selected.Add('G5Smoke') }
elseif ($Full) { 1..21 | ForEach-Object { $selected.Add("G$_") }; $selected.Add('G5Smoke') }
elseif ($D0) { 1..5 | ForEach-Object { $selected.Add("G$_") }; $selected.Add('G5Smoke'); $selected.Add('G6'); $selected.Add('D0ExportRuntime') }
elseif ($runtimeChanges.Count) {
    1..5 | ForEach-Object { $selected.Add("G$_") }
    if (@($changes | Where-Object { $_ -match '^game/(flow|kernel|camera|stage)/|^game/project\.godot$' }).Count) { $selected.Add('G5Smoke') }
    if (@($changes | Where-Object { $_ -match '^game/(camera/walk|kernel/collision|gen/(collision|walkable))/' }).Count) { $selected.Add('G15Core') }
    if (@($changes | Where-Object { $_ -match '^game/kernel/save/|^game/contracts/Save/' }).Count) { $selected.Add('G20Core') }
}
# Service replacement is a required G5 subcheck, including explicitly selected G5.
$mainStage = Get-UESTCMainStage $root
$serviceImpact = if ($selected.Contains('G5')) { Get-UESTCServiceWindowImpact $root $BaseRef $changes } else { [pscustomobject]@{ Required=$false; ChangedCode=@(); Reasons=@() } }
$serviceWindowPlan = Select-UESTCServiceWindow $selected.ToArray() $mainStage $serviceImpact.Required ([bool]$D0) ([bool]$Full) ([bool]$Smoke)
$selected.Clear()
foreach ($gateId in $serviceWindowPlan.Gates) { $selected.Add($gateId) }
$d0WindowEvidence = $null
$cacheIds = @('G1','G2','G3','G4')
if ($serviceWindowPlan.CacheG5) { $cacheIds += 'G5' }
$scope = if ($D0) { 'D0' } elseif ($Full) { 'Full' } elseif ($Only.Count) { 'Selected' } elseif ($Smoke) { 'Core' } else { 'Quick' }
$planObject = [ordered]@{ Scope = $scope; WorkPackage = $Wp; BaseRef = $BaseRef; Branch = $branch; Gates = $selected.ToArray(); ImportRequired = $importRequired; ChangedPaths = $changes; Godot = $godot; Locks = $layout.Locks; CpuLimit = [Math]::Max(2,[Math]::Floor([Environment]::ProcessorCount/8.0)); Bake = 'No pre-bake in D0/quick/core; centralized bake gates not implemented' }
$g2Selection = if ($selected.Contains('G2')) { @(Get-UESTCG2ProjectSelection $root $changes @(Get-UESTCFiles $root) ([bool]($D0 -or $Full -or $Only.Count))) } else { @() }
$planObject['G2Selection'] = $g2Selection
$planObject['Stage'] = $mainStage
$planObject['ServiceWindow'] = [ordered]@{ Impact=$serviceImpact; D0WindowInG5=$serviceWindowPlan.D0WindowInG5; RequireFormalCore=$serviceWindowPlan.RequireFormalCore; CacheG5=$serviceWindowPlan.CacheG5 }
if ($Plan) { $planObject | ConvertTo-Json -Depth 8; return }
$null = [IO.Directory]::CreateDirectory($work)
$results = New-Object 'Collections.Generic.List[object]'
$utf8 = New-Object Text.UTF8Encoding $false
$cacheable = $selected.Count -gt 0 -and -not ($D0 -or $Full -or $NoCache)
$inputHash = if ($cacheable) { Get-UESTCInputHash $root $Wp $BaseRef $Configuration $godot $changes } else { '' }
$cachePath = Join-Path $work "$Wp-cache.json"
$cacheEntries = @{}
if ($cacheable -and (Test-Path -LiteralPath $cachePath)) {
    try {
        $savedCache = Get-Content -Raw -Encoding UTF8 -LiteralPath $cachePath | ConvertFrom-Json
        foreach ($entry in $savedCache.entries) { $cacheEntries[$entry.Gate] = $entry }
    } catch { Write-Warning 'Unreadable gate cache ignored.' }
}

function Invoke-LockedProcess {
    param([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$Kind = 'Cpu', [switch]$Visible)
    $lease = Enter-UESTCResourceLock $layout.Locks $Kind
    try {
        try { Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name.log") -Visible:$Visible }
        catch {
            # A known native import failure is retried once; ordinary failures and ERROR logs are never masked.
            if ($Name -ne 'G1-import' -or $_.Exception.Data['ExitCode'] -ne -1073741819) { throw }
            Write-Warning "Godot import exited 0xC0000005; retrying once. Original log preserved: $(Join-Path $work "$Name.log")"
            Invoke-UESTCProcess $Executable $Arguments $root (Join-Path $work "$Name-retry.log") -Visible:$Visible
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
function Invoke-WindowProbe {
    param([string]$Name, [string]$Executable, [switch]$Exported)
    $shots = Join-Path $root ("output/render-review/{0}-{1}" -f $Wp, $Name)
    $null = [IO.Directory]::CreateDirectory($shots)
    $args = @('--path',$game)
    if ($Exported) { $args = @() }
    $args += @('--','--shots=V1',"--out=$shots","--frames=$Frames",'--pose=V1','--clock=17.5','--season=autumn','--anim=240','--flow=0','--mode=fly','--ui=0','--seed=1','--weather=clear','--weather-seed=1','--quality=fine','--adaptive=0',"--map=$Map",'--title=0','--save=off','--audio=off')
    $began = [DateTime]::UtcNow
    # Atomic admission avoids a CPU -> GPU wait cycle with a queued exclusive job.
    $result = Invoke-LockedProcess $Name $Executable $args CpuGpuShared -Visible
    Assert-CleanGodotLog $result
    $images = @(Get-ChildItem -LiteralPath $shots -Filter '*.png' -File | Where-Object { $_.Length -gt 0 -and $_.LastWriteTimeUtc -ge $began })
    if (-not $images.Count) { throw "Window probe produced no new PNG in $shots" }
    return "Window screenshot produced: $($images[0].FullName)"
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
        if ($cacheable -and $id -in $cacheIds -and $cacheEntries.ContainsKey($id)) {
            $entry = $cacheEntries[$id]
            $binaryHash = if ($id -eq 'G3') { 'source-only' } else { Get-UESTCBinaryHash $root }
            if ($binaryHash -and $entry.InputHash -eq $inputHash -and $entry.BinaryHash -eq $binaryHash) {
                $results.Add([pscustomobject]@{ Gate=$id; Status='CachedPass'; Scope=$scope; Detail=$entry.Detail })
                Write-Host "[$id] CachedPass (source, dependency, tool and binary fingerprints unchanged)"
                continue
            }
        }
        $detail = ''
        $status = 'Passed'
        switch ($id) {
            'G1' {
                $naming = Test-UESTCSourceNaming $root
                Assert-GodotVersion
                $null = Invoke-LockedProcess 'G1-build' dotnet @('build',(Join-Path $game 'Campus.sln'),'-c',$Configuration,'-warnaserror')
                $null = Invoke-LockedProcess 'G1-format' dotnet @('format',(Join-Path $game 'Campus.sln'),'--verify-no-changes','--no-restore','--exclude',(Join-Path $game 'addons'))
                if ($importRequired) {
                    $r = Invoke-LockedProcess 'G1-import' $godot @('--headless','--path',$game,'--import')
                    Assert-CleanGodotLog $r
                }
                $detail = "Naming files=$($naming.CheckedFiles); build and first-party format verified; import=$importRequired"
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
                    $null = Invoke-LockedProcess "G2-$test" dotnet @('test',$project,'-c',$Configuration,'--settings',$settings,'--filter','TestCategory!=NativeRender','--logger','trx','--results-directory',(Join-Path $work 'test-results'))
                    $runCount++
                }
                if ($runCount -eq 0) { $status = 'Skipped' }
                $detail = "Test projects run=$runCount. " + ($notes -join '; ')
            }
            'G3' { $detail = Test-UESTCOwnership $root $Wp $changes -D0:$D0 | ConvertTo-Json -Depth 5 -Compress }
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
                $detail = 'Headless registry checked for all maps.'
                $d0Report = [regex]::Match($r.Output, '(?m)^G5_D0_REGISTRY_RESULT[ \t]+(.+)$')
                if ($d0Report.Success) { $detail = ConvertFrom-Json -InputObject $d0Report.Groups[1].Value }
                if ($serviceWindowPlan.D0WindowInG5) {
                    $registryEvidence = $detail
                    $d0WindowEvidence = Invoke-WindowProbe 'd0-service-window' $godot
                    $detail = [pscustomobject]@{ Scope='D0 registry plus service-replacement window only'; Registry=$registryEvidence; Window=$d0WindowEvidence; FullMainline='NotImplemented'; Trigger=$serviceImpact.Reasons }
                }
            }
            'G5Smoke' {
                if ($mainStage -eq 'D0') {
                    Assert-GodotVersion
                    if (-not $d0WindowEvidence) { $d0WindowEvidence = Invoke-WindowProbe 'd0-service-window' $godot }
                    $status = 'NotImplemented'
                    $detail = [pscustomobject]@{ Scope='Full mainline requested, but current scene is D0'; D0Window=$d0WindowEvidence; Missing='WP-27 mainline/close/save/resume journey' }
                } elseif ($mainStage -eq 'KernelStage') {
                    $coreScript = Join-Path $PSScriptRoot 'gates/core-smoke.ps1'
                    if (Test-Path -LiteralPath $coreScript -PathType Leaf) {
                        . $coreScript
                        if (-not (Get-Command Invoke-UESTCMainlineSmoke -ErrorAction SilentlyContinue)) {
                            $status = 'NotImplemented'
                            $detail = 'WP-27 core smoke entry point is not implemented; D0 fallback is forbidden.'
                        } else {
                            Assert-GodotVersion
                            $detail = Invoke-UESTCMainlineSmoke -Root $root -Godot $godot -Locks $layout.Locks -Wp $Wp -Map $Map
                            if ($detail.Status -notin @('Passed','Failed','NotImplemented')) { throw 'WP-27 returned an invalid core status.' }
                            $status = $detail.Status
                        }
                    } else {
                        $status = 'NotImplemented'
                        $detail = 'Formal KernelStage requires WP-27 core-smoke.ps1; D0 screenshot fallback is forbidden.'
                    }
                } else {
                    $status = 'NotImplemented'
                    $detail = 'Main scene root script is not recognized; refusing to substitute a D0 window for core coverage.'
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
                if (-not $D0) { $status = 'NotImplemented' }
            }
            'D0ExportRuntime' {
                $exe = Join-Path $root 'build/windows/UESTC.exe'
                $r = Invoke-LockedProcess 'D0-export-registry' $exe @('--headless','--','--gate=registry','--map=*','--save=off','--audio=off')
                Assert-CleanGodotLog $r
                $detail = 'Export registry passed. ' + (Invoke-WindowProbe 'export-smoke' $exe -Exported)
            }
            default { $status = 'NotImplemented'; $detail = 'Centralized or core-specialist gate has no implementation yet; no pre-bake was started.' }
        }
        $results.Add([pscustomobject]@{ Gate=$id; Status=$status; Scope=$scope; Detail=$detail })
        if ($cacheable -and $status -eq 'Passed' -and $id -in $cacheIds) {
            $binaryHash = if ($id -eq 'G3') { 'source-only' } else { Get-UESTCBinaryHash $root }
            $cacheEntries[$id] = [pscustomobject]@{ Gate=$id; InputHash=$inputHash; BinaryHash=$binaryHash; Detail=$detail }
        }
        Write-Host "[$id] $status - $detail"
    } catch {
        $results.Add([pscustomobject]@{ Gate=$id; Status='Failed'; Scope=$scope; Detail=$_.Exception.Message })
        Write-Warning "[$id] $($_.Exception.Message)"
        # Dependent later stages must not test stale binaries after a build/import failure.
        break
    }
}
if ($cacheable) {
    # If import or restore changed an input during this run, do not publish a stale pass.
    $afterHash = Get-UESTCInputHash $root $Wp $BaseRef $Configuration $godot $changes
    if ($afterHash -eq $inputHash) { [IO.File]::WriteAllText($cachePath, (@{ entries=@($cacheEntries.Values) } | ConvertTo-Json -Depth 8), $utf8) }
}
$report = [ordered]@{ Plan=$planObject; Results=$results.ToArray(); CachePolicy='Conservative whole-repository runtime/dependency hash plus SDK, engine and production binary fingerprints; D0/full/window/export never cached'; GeneratedAt=[DateTime]::UtcNow.ToString('o') }
$reportPath = Join-Path $work "$Wp-result.json"
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 10), $utf8)
Write-Host "Report: $reportPath"
if (@($results | Where-Object { $_.Status -eq 'Failed' }).Count) { exit 1 }
if (@($results | Where-Object { $_.Status -eq 'NotImplemented' }).Count) { exit 2 }
exit 0
