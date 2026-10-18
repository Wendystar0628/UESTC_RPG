# Select real startup checks when registered services change.
# Uses the already loaded common.ps1 and check-source.ps1 helpers.
function Get-UESTCRegistrationSignature {
    param([string]$Text)
    $pattern = '(?s)\[\s*(?:[A-Za-z_][\w.]*\.)?(?:ContentModule|Part|KernelNode|KernelService|SaveSection|AmbientEmitter|InteriorProvider|MediumPresenter|QualityResponsive)(?:Attribute)?\b[^\]]*\](?:\s*\[[^\]]*\])*\s*(?:(?:public|internal|sealed|abstract|partial|static)\s+)*(?:class|struct|record)\s+\w+[^{;]*|<AssemblyAttribute\b.*?</AssemblyAttribute>'
    $declarations = @([regex]::Matches($Text, $pattern) | ForEach-Object { $_.Value.Trim() })
    # Registration attributes can live in another partial file. Constructor
    # parameters still change discovery dependencies; executable bodies do not.
    foreach ($type in [regex]::Matches($Text, '\b(?:class|struct|record)\s+(?<name>\w+)(?:\s*<[^>{}]+>)?\s*(?<primary>\([^)]*\))?')) {
        if ($type.Groups['primary'].Success) { $declarations += $type.Value.Trim() }
        $constructor = '\bpublic\s+' + [regex]::Escape($type.Groups['name'].Value) + '\s*\([^)]*\)'
        $declarations += @([regex]::Matches($Text, $constructor) | ForEach-Object { $_.Value.Trim() })
    }
    if (-not $declarations.Count) { return '' }
    return ((@([regex]::Matches($Text, '(?m)^\s*namespace\s+[\w.]+') | ForEach-Object { $_.Value.Trim() }) + $declarations) -join "`n")
}

function Test-UESTCG5Impact {
    param([string]$Root, [string]$BaseRef, [string[]]$Changes)
    if (@($Changes | Where-Object { $_ -match '^game/(kernel/|contracts/|stage/|data/maps/|project\.godot$)' }).Count) { return $true }
    $code = @($Changes | Where-Object { (Test-UESTCProductionCs $_) -or $_ -match '\.csproj$' })
    if (-not $code.Count) { return $false }
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    $baseFiles = @(Get-UESTCGitLines $Root @('ls-tree','-r','--name-only',$base))
    foreach ($path in $code) {
        $absolute = Join-Path $Root $path
        $current = if (Test-Path -LiteralPath $absolute) { [IO.File]::ReadAllText($absolute) } else { '' }
        $old = if ($path -in $baseFiles) { (Invoke-UESTCGit $Root @('show',"${base}:$path")).StdOut } else { '' }
        $before = Get-UESTCRegistrationSignature $old
        $after = Get-UESTCRegistrationSignature $current
        if ($before -cne $after) { return $true }
    }
    return $false
}

function Test-UESTCProductionCs {
    param([string]$Path)
    return $Path -match '^(game|pipeline)/.*\.cs$' -and $Path -notmatch '^game/addons/|/(tests?|obj|bin|\.godot)/'
}

function Get-UESTCOwnerForWindow {
    param([string]$Path, [object[]]$Rules)
    foreach ($rule in $Rules) { if (Test-UESTCGlob $Path $rule.pattern) { return [string]$rule.owner } }
    return ''
}

function Get-UESTCServiceImpactCore {
    param([string[]]$Changed, [string[]]$CurrentRegistrations, [string[]]$OldRegistrations,
        [object[]]$CurrentRules, [object[]]$OldRules)
    $code = @($Changed | Where-Object { Test-UESTCProductionCs $_ })
    $hits = New-Object 'Collections.Generic.List[string]'
    $owners = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($path in $code) {
        if ($path -match '^game/(kernel/(boot/|registry/|stubs/|map/data/|renderer/(geometry|presentation|render-state)/|materials/|instances/)|stage/)') { $hits.Add("shared-startup:$path") }
        $currentOwner = Get-UESTCOwnerForWindow $path $CurrentRules
        $oldOwner = Get-UESTCOwnerForWindow $path $OldRules
        if (-not $currentOwner -and -not $oldOwner) { $hits.Add("unknown-owner:$path") }
        if ($currentOwner) { $null = $owners.Add($currentOwner) }
        if ($oldOwner) { $null = $owners.Add($oldOwner) }
    }
    foreach ($snapshot in @(
        [pscustomobject]@{ Paths=$CurrentRegistrations; Rules=$CurrentRules; Label='current' },
        [pscustomobject]@{ Paths=$OldRegistrations; Rules=$OldRules; Label='base' })) {
        foreach ($path in $snapshot.Paths) {
            if (-not (Test-UESTCProductionCs $path)) { continue }
            $owner = Get-UESTCOwnerForWindow $path $snapshot.Rules
            if ($owner -and $owners.Contains($owner)) { $hits.Add("$($snapshot.Label)-registration:$owner`:$path") }
        }
    }
    [pscustomobject]@{ Required=$hits.Count -gt 0; ChangedCode=$code; Reasons=$hits.ToArray() }
}

function Get-UESTCServiceWindowImpact {
    param([string]$Root, [string]$BaseRef, [string[]]$Changes)
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    # Include both paths of a rename so moving a registration to another owner
    # while deleting its attribute cannot hide the old package's involvement.
    $allChanges = @($Changes) + @(Get-UESTCGitLines $Root @('diff','--name-only','--no-renames',$base,'--'))
    $code = @($allChanges | Sort-Object -Unique | Where-Object { (Test-UESTCProductionCs $_) -and -not (Test-UESTCCommentOnlyChange $Root $_ $BaseRef) })
    if (-not $code.Count) { return [pscustomobject]@{ Required=$false; ChangedCode=@(); Reasons=@() } }
    $currentOwnership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $baseOwnershipExists = @(Get-UESTCGitLines $Root @('ls-tree','--name-only',$base,'--','ownership.json')).Count -gt 0
    $oldOwnership = if ($baseOwnershipExists) { (Invoke-UESTCGit $Root @('show',"${base}:ownership.json")).StdOut | ConvertFrom-Json } else { $currentOwnership }
    $sort = @{Expression={ ($_.pattern -replace '[*?]', '').Length };Descending=$true}
    $currentRules = @($currentOwnership.rules | Sort-Object $sort)
    $oldRules = @($oldOwnership.rules | Sort-Object $sort)
    # Package-level selection intentionally accepts lexical false positives; it
    # does not require an attribute to appear in the changed partial file.
    $pattern = '\bKernel(?:Service|Node)(?:Attribute)?\b'
    $current = @(Get-UESTCFiles $Root | Where-Object {
        (Test-UESTCProductionCs $_) -and (Test-Path -LiteralPath (Join-Path $Root $_)) -and
        ([IO.File]::ReadAllText((Join-Path $Root $_)) -match $pattern)
    })
    # One complete git grep, not one git process per old source file. Exit 1 is
    # precisely "no matches"; genuine git failures still throw.
    $oldMatches = Invoke-UESTCGit $Root @('grep','-l','-I','-E','Kernel(Service|Node)(Attribute)?',$base,'--','*.cs') -SuccessExitCodes @(0,1)
    $old = @($oldMatches.StdOut -split '\r?\n' | Where-Object { $_ } | ForEach-Object { $_.Substring($base.Length + 1) })
    Get-UESTCServiceImpactCore $code $current $old $currentRules $oldRules
}

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

function Select-UESTCServiceWindow {
    param([string[]]$Gates, [bool]$Affected)
    $selected = New-Object 'Collections.Generic.List[string]'
    foreach ($gate in $Gates) { if (-not $selected.Contains($gate)) { $selected.Add($gate) } }
    $formal = $selected.Contains('G5') -and $Affected
    if ($formal -and -not $selected.Contains('G5Smoke')) { $selected.Add('G5Smoke') }
    [pscustomobject]@{ Gates=$selected.ToArray(); RequireFormalCore=$formal; CacheG5=-not $formal }
}
