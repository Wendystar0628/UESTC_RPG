function Get-UESTCFiles {
    param([string]$Root)
    @(& git -C $Root -c core.quotepath=false ls-files --cached --others --exclude-standard) | Sort-Object -Unique
}

function Get-UESTCChanges {
    param([string]$Root, [string]$BaseRef)
    $files = @(& git -C $Root -c core.quotepath=false diff --name-only --diff-filter=ACDMRTUXB "$BaseRef...HEAD")
    if ($LASTEXITCODE -ne 0) { throw "Cannot compare $BaseRef...HEAD" }
    $files += @(& git -C $Root -c core.quotepath=false diff --name-only HEAD)
    $files += @(& git -C $Root -c core.quotepath=false ls-files --others --exclude-standard)
    @($files | Where-Object { $_ } | Sort-Object -Unique)
}

function Test-UESTCCommentOnlyChange {
    param([string]$Root, [string]$Path, [string]$BaseRef)
    if ($Path -notmatch '\.(cs|gdshader|gdshaderinc|glsl|glslinc)$') { return $false }
    $absolute = Join-Path $Root $Path
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { return $false }
    $base = (& git -C $Root merge-base $BaseRef HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { return $false }
    if (-not @(& git -C $Root ls-tree -r --name-only $base -- $Path).Count) { return $false }
    $before = (@(& git -C $Root show "${base}:$Path") -join "`n").Replace("`r`n", "`n")
    $after = [IO.File]::ReadAllText($absolute).Replace("`r`n", "`n")
    # Raw strings can contain quote/comment sequences; conservatively run the gates for them.
    if ($before.Contains('"""') -or $after.Contains('"""')) { return $false }
    $pattern = '(@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*'')|/\*[\s\S]*?\*/|//[^\r\n]*'
    $strip = { param($m) if ($m.Groups[1].Success) { $m.Value } else { ' ' } }
    return [regex]::Replace($before, $pattern, $strip).Trim() -ceq [regex]::Replace($after, $pattern, $strip).Trim()
}

function Test-UESTCGlob {
    param([string]$Path, [string]$Pattern)
    $patternRegex = [regex]::Escape($Pattern.Replace('\','/')).Replace('\*\*','__DOUBLE_STAR__').Replace('\*','[^/]*').Replace('__DOUBLE_STAR__','.*').Replace('\?','[^/]')
    return $Path -match ('^' + $patternRegex + '$')
}

function Test-UESTCOwnership {
    param([string]$Root, [string]$Wp, [string[]]$Changes, [switch]$D0)
    $ownership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $rules = @($ownership.rules | Sort-Object @{Expression={ ($_.pattern -replace '[*?]', '').Length };Descending=$true})
    $failures = New-Object 'Collections.Generic.List[string]'
    $exceptions = New-Object 'Collections.Generic.List[string]'
    $initialPaths = @()
    if ($D0 -and $Wp -eq 'WP-00') {
        $initialPaths = @(& git -C $Root ls-tree -r --name-only main)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect D0 base tree.' }
    }
    foreach ($path in $Changes) {
        $owner = $null
        foreach ($rule in $rules) { if (Test-UESTCGlob $path $rule.pattern) { $owner = $rule.owner; break } }
        if ($owner -eq $Wp -or ($Wp -eq 'WP-00' -and $owner -in @('shared','main'))) { continue }
        $initial = $false
        if ($D0 -and $Wp -eq 'WP-00' -and $owner -and $path -notmatch '^docs/' -and 'game/project.godot' -notin $initialPaths) {
            $initial = $path -notin $initialPaths
        }
        if ($initial) { $exceptions.Add("D0 initial file: $path (future owner: $owner)") }
        else { $failures.Add("Ownership: $path belongs to '$owner', requested $Wp") }
    }
    $files = @(Get-UESTCFiles $Root)
    $tracked = @(& git -C $Root -c core.quotepath=false ls-files --cached)
    $uids = @{}
    foreach ($path in $files) {
        $absolute = Join-Path $Root $path
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        if ($path -match '^game/.*\.uid$') {
            $value = (Get-Content -Raw -Encoding UTF8 -LiteralPath $absolute).Trim()
            if ($value -notmatch '^uid://[a-z0-9]+$') { $failures.Add("Invalid UID: $path") }
            elseif ($uids.ContainsKey($value)) { $failures.Add("Duplicate UID: $path and $($uids[$value])") }
            else { $uids[$value] = $path }
        }
        if ($path -notmatch '^game/.*\.(cs|gdshader|gdshaderinc)$' -or $path -match '^game/addons/') { continue }
        $ignored = $false
        $directory = Split-Path $absolute -Parent
        while ($directory.StartsWith((Join-Path $Root 'game'), [StringComparison]::OrdinalIgnoreCase)) {
            if (Test-Path -LiteralPath (Join-Path $directory '.gdignore')) { $ignored = $true; break }
            $directory = Split-Path $directory -Parent
        }
        if ($ignored) { continue }
        $uid = "$path.uid"
        if (-not (Test-Path -LiteralPath (Join-Path $Root $uid))) { $failures.Add("Missing UID: $uid; run G1 import") }
        elseif (-not $D0 -and $uid -notin $tracked) { $failures.Add("UID not staged/tracked: $uid") }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ CheckedPaths = $Changes.Count; InitialD0Exceptions = $exceptions.ToArray(); Uids = $uids.Count }
}

function Test-UESTCSourceBoundaries {
    param([string]$Root)
    $ownership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $failures = New-Object 'Collections.Generic.List[string]'
    $mapIds = @()
    $indexPath = Join-Path $Root 'game/data/maps/index.json'
    if (Test-Path -LiteralPath $indexPath) {
        $indexText = Get-Content -Raw -Encoding UTF8 -LiteralPath $indexPath
        $mapIds = @([regex]::Matches($indexText, '"id"\s*:\s*"([^"/]+)"') | ForEach-Object { $_.Groups[1].Value })
    }
    foreach ($path in (Get-UESTCFiles $Root)) {
        if ($path -notmatch '^(game|pipeline)/' -or $path -match '^game/addons/|/(obj|bin)/') { continue }
        $file = Join-Path $Root $path
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
        if ($path -match '\.gd$') { $failures.Add("GDScript outside addons: $path"); continue }
        if ($path -match '\.(tscn|tres)$' -and $path -notin @('game/stage/main.tscn','game/hud/ui_theme.tres')) { $failures.Add("Shared resource outside allowlist: $path") }
        if ($path -notmatch '\.(cs|gdshader|gdshaderinc|glsl|glslinc)$' -or $path -match '/tests/') { continue }
        $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file
        # Preserve strings while dropping comments, so URLs and quoted IDs remain inspectable.
        $text = [regex]::Replace($text, '(@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*'')|/\*[\s\S]*?\*/|//[^\r\n]*', { param($m) if ($m.Groups[1].Success) { $m.Value } else { '' } })
        $checks = @{
            'inline hexadecimal colour' = '#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?\b|Color\s*\.\s*FromHtml\s*\(|new\s+Color\s*\(\s*"';
            'handwritten UID' = 'uid://';
            'retired colour key' = '"(?:shadow\.contact|line\.building|line\.tree|shadow\.interior)"'
        }
        if ($path -match '\.(gdshader|gdshaderinc|glsl|glslinc)$') {
            if ($path -match '\.(gdshader|gdshaderinc|glsl|glslinc)$') {
                $allowed = @($ownership.shaderWhitelist | Where-Object { Test-UESTCGlob $path $_ })
                if (-not $allowed.Count) { $failures.Add("Shader not in whitelist: $path") }
            }
            $checks['shader built-in TIME'] = '\bTIME\b'
            $vertex = [regex]::Match($text, '\bvoid\s+vertex\s*\(\s*\)\s*\{')
            if ($vertex.Success) {
                $depth = 1
                $end = $vertex.Index + $vertex.Length
                while ($end -lt $text.Length -and $depth -gt 0) {
                    if ($text[$end] -eq '{') { $depth++ }
                    elseif ($text[$end] -eq '}') { $depth-- }
                    $end++
                }
                if ($text.Substring($vertex.Index, $end - $vertex.Index) -match '\bCAMERA_POSITION_WORLD\b') {
                    $failures.Add("${path}: vertex camera position is unsafe for shadow passes")
                }
            }
        } else {
            $checks['runtime shader mutation'] = '"[^"\r\n]*(?:render_mode|#define)'
            $checks['Resource user storage'] = '(?:ResourceLoader\s*\.\s*Load|ResourceSaver\s*\.\s*Save)\s*\([^;]*"user://'
            if ($path -notmatch '^game/kernel/save/') { $checks['save path outside save kernel'] = 'user://saves' }
            if ($path -notmatch '^game/kernel/bake/') { $checks['bake cache outside bake kernel'] = 'user://cache/bake' }
            if ($path -notmatch '^game/kernel/quality/') { $checks['quality settings outside quality kernel'] = 'user://graphics\.cfg' }
            if ($path -notmatch '^game/kernel/map/|^game/gates/|^game/gen/zones/') {
                $checks['map data path in generic code'] = 'res://data/maps/'
                $checks['entity ID in generic code'] = '"(?:[a-z][a-z0-9_-]*:[a-z][a-z0-9_-]*:[^"\s]+|(?:design|walk):[^"\s]+)"'
                foreach ($mapId in $mapIds) { $checks["map ID $mapId in generic code"] = '"' + [regex]::Escape($mapId) + '"' }
            }
            if ($path -match '^game/audio/') { $checks['audio self timing'] = '\bTimer\b|\bTime\s*\.\s*GetTicks' }
            if ($path -match '^game/hud/') {
                foreach ($call in [regex]::Matches($text, '\bRenderingServer\s*\.\s*GlobalShaderParameterSet\s*\(\s*([^,]+),')) {
                    if ($call.Groups[1].Value.Trim() -notmatch '^(?:"ui_night"|ShaderGlobals\.UiNight)$') {
                        $failures.Add("${path}: HUD may only set the ui_night global")
                    }
                }
            }
        }
        foreach ($rule in $checks.Keys) { if ($text -match $checks[$rule]) { $failures.Add("${path}: $rule") } }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ Status = 'Passed'; Scope = 'Source rules; semantic dependencies are checked separately by ArchUnitNET' }
}
