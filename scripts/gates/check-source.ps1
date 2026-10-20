# 源码与归属检查函数库：列出改动文件，按 ownership.json 核对归属与 UID，并检查新增代码放置、命名及源码边界规则。
# 产出：无（只读检查源码与归属并返回内存结果）。
# 用法：由 scripts\gate.ps1 点引用（. scripts\gates\check-source.ps1）后调用 Test-UESTCOwnership、Test-UESTCNewCodeFiles、Test-UESTCSourceBoundaries 等函数，不单独运行
function Get-UESTCFiles {
    param([string]$Root)
    Get-UESTCGitPaths $Root @('ls-files', '-z', '--cached', '--others', '--exclude-standard') | Sort-Object -Unique
}

function Get-UESTCChanges {
    param([string]$Root, [string]$BaseRef)
    $files = @(Get-UESTCGitPaths $Root @('diff', '-z', '--no-renames', '--name-only', '--diff-filter=ACDMRTUXB', "$BaseRef...HEAD"))
    # Keep index and worktree changes separately: they can cancel against HEAD.
    $files += @(Get-UESTCGitPaths $Root @('diff', '-z', '--no-renames', '--name-only', '--cached'))
    $files += @(Get-UESTCGitPaths $Root @('diff', '-z', '--no-renames', '--name-only'))
    $files += @(Get-UESTCGitPaths $Root @('ls-files', '-z', '--others', '--exclude-standard'))
    @($files | Where-Object { $_ } | Sort-Object -Unique)
}


# Validate only changed authored text; deleted files have no current bytes to inspect.
function Test-UESTCChangedText {
    param([string]$Root, [string[]]$Changes, [string]$BaseRef)
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    $gbk = [Text.Encoding]::GetEncoding(936, [Text.EncoderExceptionFallback]::new(), [Text.DecoderExceptionFallback]::new())
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    $basePaths = @(Get-UESTCGitPaths $Root @('ls-tree', '-z', '-r', '--name-only', $base))
    $checked = 0
    $failures = [Collections.Generic.List[string]]::new()
    foreach ($path in $Changes) {
        if ($path -notmatch '\.(cs|gd|gdshader|gdshaderinc|glsl|glslinc|json|md|txt|ps1|psm1|psd1|py|js|mjs|cjs|ts|jsx|tsx|html|css|svg|xml|yml|yaml|toml|ini|cfg|godot|tscn|tres|uid|csproj|props|targets|sln|slnx|cmd|bat|sh|csv|tsv)$' -and
            [IO.Path]::GetFileName($path) -notin @('.editorconfig','.gitattributes','.gitignore','.ignore','LICENSE')) { continue }
        $absolute = Join-Path $Root $path
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        $checked++
        $bytes = [IO.File]::ReadAllBytes($absolute)
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
        try { $text = $utf8.GetString($bytes) }
        catch [Text.DecoderFallbackException] { $failures.Add("${path}: invalid UTF-8"); continue }
        # Strip exactly one actual UTF-8 BOM before classifying the body.
        if ($hasBom) { $text = $text.Substring(1) }
        $hasNonAscii = $text -match '[^\x00-\x7f]'
        $requiresBom = $path -match '\.(ps1|psm1|psd1)$' -and $hasNonAscii
        if ($requiresBom -and -not $hasBom) {
            $failures.Add("${path}: non-ASCII PowerShell text requires UTF-8 BOM")
        } elseif ($hasBom -and -not $requiresBom) {
            $failures.Add("${path}: UTF-8 BOM is not allowed")
        }
        if ($text.Contains([string][char]0xfffd)) { $failures.Add("${path}: literal Unicode replacement character") }
        if ($path -notin $basePaths -or -not $hasNonAscii) { continue }
        $before = (Invoke-UESTCGit $Root @('show', "${base}:$path")).StdOut
        # Existing Chinese in this same file supplies evidence; ordinary printable Chinese
        # and private-use icon characters are never classified from a character blacklist.
        foreach ($span in @([regex]::Matches($before, '\p{IsCJKUnifiedIdeographs}{2,}') | ForEach-Object Value | Sort-Object -Unique)) {
            try { $misread = $gbk.GetString($utf8.GetBytes($span)) }
            catch [Text.DecoderFallbackException] { continue }
            if ($misread -cne $span -and $text.Contains($misread) -and -not $before.Contains($misread)) {
                $failures.Add("${path}: newly introduced GBK decoding of baseline Chinese text: $misread")
            }
        }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ CheckedTextFiles=$checked }
}

function Test-UESTCScriptSyntax {
    param([string]$Root)
    $files = @(Get-UESTCFiles $Root | Where-Object { Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf })
    $python = @($files | Where-Object { $_ -match '^(dashboard/tools|pipeline|scripts)/.*\.py$' })
    if ($python.Count) {
        $code = 'import ast, pathlib, sys; [ast.parse(pathlib.Path(p).read_text(encoding="utf-8"), filename=p) for p in sys.argv[1:]]'
        $null = Invoke-UESTCProcess python (@('-I','-B','-X','utf8','-c',$code) + $python) $Root -TextEncoding ([Text.Encoding]::UTF8)
    }
    $javascript = @($files | Where-Object { $_ -match '^dashboard/app/.*\.(js|mjs|cjs)$' })
    foreach ($path in $javascript) {
        $null = Invoke-UESTCProcess node @('--check', (Join-Path $Root $path)) $Root -TextEncoding ([Text.Encoding]::UTF8)
    }
    [pscustomobject]@{ PythonFiles=$python.Count; JavaScriptFiles=$javascript.Count }
}

function Test-UESTCOwnershipRules {
    param([string]$Root)
    $ownership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $files = @(Get-UESTCFiles $Root | Where-Object { Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf })
    $unmatched = [Collections.Generic.List[string]]::new()
    foreach ($rule in $ownership.rules) {
        $expression = Get-UESTCGlobExpression $rule.pattern
        if (@($files -match $expression).Count) { continue }
        # Ignored collaboration tools are real owned files, even though git omits them.
        $prefix = ($rule.pattern -split '[*?]', 2)[0]
        if ($prefix -eq $rule.pattern) {
            $matched = Test-Path -LiteralPath (Join-Path $Root $prefix) -PathType Leaf
        } else {
            $directory = if ($prefix.EndsWith('/')) { $prefix.TrimEnd('/') } else { Split-Path $prefix -Parent }
            $absolute = if ($directory) { Join-Path $Root $directory } else { $Root }
            $matched = $false
            if (Test-Path -LiteralPath $absolute -PathType Container) {
                $matched = @(Get-ChildItem -LiteralPath $absolute -File -Recurse -Force | Where-Object {
                    ($_.FullName.Substring($Root.Length + 1).Replace('\','/')) -match $expression
                } | Select-Object -First 1).Count -gt 0
            }
        }
        if (-not $matched) { $unmatched.Add($rule.pattern) }
    }
    if ($unmatched.Count) { throw ("Ownership rules match no existing files:`n" + ($unmatched -join "`n")) }
    [pscustomobject]@{ CheckedRules=@($ownership.rules).Count }
}

function Test-UESTCCommentOnlyChange {
    param([string]$Root, [string]$Path, [string]$BaseRef)
    if ($Path -notmatch '\.(cs|gdshader|gdshaderinc|glsl|glslinc)$') { return $false }
    $absolute = Join-Path $Root $Path
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { return $false }
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    if (-not @(Get-UESTCGitPaths $Root @('ls-tree', '-z', '-r', '--name-only', $base, '--', $Path)).Count) { return $false }
    if (-not @(Get-UESTCGitPaths $Root @('ls-files', '-z', '--cached', '--', $Path)).Count) { return $false }
    $before = (Invoke-UESTCGit $Root @('show', "${base}:$Path")).StdOut.Replace("`r`n", "`n")
    $indexed = (Invoke-UESTCGit $Root @('show', ":$Path")).StdOut.Replace("`r`n", "`n")
    $after = [IO.File]::ReadAllText($absolute).Replace("`r`n", "`n")
    # Raw strings can contain quote/comment sequences; conservatively run the gates for them.
    if ($before.Contains('"""') -or $indexed.Contains('"""') -or $after.Contains('"""')) { return $false }
    $pattern = '(@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*'')|/\*[\s\S]*?\*/|//[^\r\n]*'
    $strip = { param($m) if ($m.Groups[1].Success) { $m.Value } else { ' ' } }
    $beforeCode = [regex]::Replace($before, $pattern, $strip).Trim()
    return $beforeCode -ceq [regex]::Replace($indexed, $pattern, $strip).Trim() -and
        $beforeCode -ceq [regex]::Replace($after, $pattern, $strip).Trim()
}

function Test-UESTCGlob {
    param([string]$Path, [string]$Pattern)
    return $Path -match (Get-UESTCGlobExpression $Pattern)
}

function Get-UESTCGlobExpression {
    param([string]$Pattern)
    $patternRegex = [regex]::Escape($Pattern.Replace('\','/')).Replace('\*\*','__DOUBLE_STAR__').Replace('\*','[^/]*').Replace('__DOUBLE_STAR__','.*').Replace('\?','[^/]')
    return '^' + $patternRegex + '$'
}

# Resolve deletions against exactly the same baseline used by selection and G3.
function Get-UESTCPathOwnership {
    param([string]$Root, [string[]]$Paths, [string]$BaseRef)
    $ownership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $rules = @($ownership.rules | Sort-Object @{Expression={ ($_.pattern -replace '[*?]', '').Length };Descending=$true})
    $deleted = @($Paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf) })
    $basePaths = @()
    $baseRules = @()
    if ($deleted.Count) {
        $basePaths = @(Get-UESTCGitPaths $Root @('ls-tree', '-z', '-r', '--name-only', $BaseRef))
        if (@($deleted | Where-Object { $_ -in $basePaths }).Count) {
            $baseOwnership = (Invoke-UESTCGit $Root @('show', "${BaseRef}:ownership.json")).StdOut | ConvertFrom-Json
            $baseRules = @($baseOwnership.rules | Sort-Object @{Expression={ ($_.pattern -replace '[*?]', '').Length };Descending=$true})
        }
    }
    foreach ($path in $Paths) {
        $owner = ''
        $pathRules = if ($path -in $deleted -and $path -in $basePaths) { $baseRules } else { $rules }
        foreach ($rule in $pathRules) {
            if (Test-UESTCGlob $path $rule.pattern) {
                $owner = [string]$rule.owner
                break
            }
        }
        [pscustomobject]@{ Path=$path; Owner=$owner }
    }
}

# G4 applies placement/name rules to additions only; existing paths keep their baseline.
function Test-UESTCNewCodeFiles {
    param([string]$Root, [string[]]$Changes, [Parameter(Mandatory)][string]$BaseRef)
    $base = (Invoke-UESTCGit $Root @('merge-base', $BaseRef, 'HEAD')).StdOut.Trim()
    $basePaths = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($path in (Get-UESTCGitPaths $Root @('ls-tree', '-z', '-r', '--name-only', $base))) {
        $null = $basePaths.Add($path)
    }
    $added = @($Changes | Where-Object {
        $_ -match '^(game|pipeline|scripts|docs|dashboard|tools|\.collab/tools)/' -and
            $_ -match '\.(cs|gd|gdshader|gdshaderinc|glsl|glslinc|ps1|psm1|py|js|mjs|cjs|ts|jsx|tsx|cmd|bat|sh)$' -and
            -not $basePaths.Contains($_) -and (Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf)
    } | Sort-Object -Unique)
    $failures = New-Object 'Collections.Generic.List[string]'
    $patchNames = @('misc','util','utils','helper','helpers','temp','tmp','new','v2','old','backup','legacy','compat','patch','fix')
    foreach ($entry in (Get-UESTCPathOwnership $Root $added $BaseRef)) {
        if (-not $entry.Owner) { $failures.Add("New code file has no registered ownership: $($entry.Path)") }
        # Match whole path tokens, so ordinary responsibilities such as news/oldTown remain valid.
        $tokens = @($entry.Path -split '[^a-zA-Z0-9]+' | Where-Object { $_ -in $patchNames } | Sort-Object -Unique)
        if ($tokens.Count) { $failures.Add("New code path uses patch-style name ($($tokens -join ', ')): $($entry.Path)") }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ CheckedNewCodeFiles=$added.Count }
}

function Get-UESTCScopedUidPaths {
    param([string[]]$Paths, [string[]]$Files)
    $sources = [Collections.Generic.HashSet[string]]::new($Files, [StringComparer]::Ordinal)
    @($Paths | Where-Object { $sources.Contains(($_ -replace '\.uid$', '')) -and $_ -match '^game/(?:.*\.uid|.*\.(cs|gdshader|gdshaderinc))$' -and $_ -notmatch '^game/addons/' } |
        ForEach-Object { if ($_ -match '\.uid$') { $_ } else { "$_.uid" } } | Sort-Object -Unique)
}

# Foreign work in progress must not change this invocation's UID baseline.
function Get-UESTCUidInputs {
    param([string]$Root, [string[]]$Required)
    if (-not $required.Count) { return }
    $committed = @(Get-UESTCGitPaths $Root @('ls-tree','-z','-r','--name-only','HEAD','--','game') | Where-Object { $_ -match '\.uid$' })
    $dirty = @(Get-UESTCGitPaths $Root @('diff','-z','--no-renames','--name-only','HEAD','--','*.uid'))
    foreach ($path in @($committed + $required | Sort-Object -Unique)) {
        if ($path -notin $required -and $path -in $dirty) {
            $value = (Invoke-UESTCGit $Root @('show',"HEAD:$path")).StdOut.Trim()
        } else {
            $absolute = Join-Path $Root $path
            $value = if (Test-Path -LiteralPath $absolute -PathType Leaf) { [IO.File]::ReadAllText($absolute).Trim() } else { '' }
        }
        [pscustomobject]@{ Path=$path; Value=$value }
    }
}

function Test-UESTCOwnership {
    param([string]$Root, [string[]]$Changes, [Parameter(Mandatory)][string]$BaseRef)
    $failures = New-Object 'Collections.Generic.List[string]'
    foreach ($entry in (Get-UESTCPathOwnership $Root $Changes $BaseRef)) {
        if (-not $entry.Owner) { $failures.Add("Ownership: $($entry.Path) has no registered ownership rule") }
    }
    $tracked = @(Get-UESTCGitPaths $Root @('ls-files', '-z', '--cached'))
    $files = @(Get-UESTCFiles $Root)
    $requiredUids = @(Get-UESTCScopedUidPaths $Changes $files)
    $uids = @{}
    foreach ($input in (Get-UESTCUidInputs $Root $requiredUids)) {
        $path = $input.Path
        $value = $input.Value
        if ($path -in $requiredUids -and (Test-Path -LiteralPath (Join-Path $Root $path) -PathType Leaf) -and $value -notmatch '^uid://[a-z0-9]+$') { $failures.Add("Invalid UID: $path") }
        if ($value -match '^uid://[a-z0-9]+$') {
            if (-not $uids.ContainsKey($value)) { $uids[$value] = @() }
            $uids[$value] += $path
        }
    }
    foreach ($value in @($uids.Keys)) {
        if ($uids[$value].Count -gt 1 -and @($uids[$value] | Where-Object { $_ -in $requiredUids }).Count) {
            $failures.Add("Duplicate UID: $($uids[$value] -join ' and ')")
        }
    }
    foreach ($uid in $requiredUids) {
        $source = $uid -replace '\.uid$', ''
        $absolute = Join-Path $Root $source
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        $ignored = $false
        $directory = Split-Path $absolute -Parent
        while ($directory.StartsWith((Join-Path $Root 'game'), [StringComparison]::OrdinalIgnoreCase)) {
            if (Test-Path -LiteralPath (Join-Path $directory '.gdignore')) { $ignored = $true; break }
            $directory = Split-Path $directory -Parent
        }
        if ($ignored) { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $Root $uid))) { $failures.Add("Missing UID: $uid; run G1 import") }
        elseif ($uid -notin $tracked) { $failures.Add("UID not staged/tracked: $uid") }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ CheckedPaths=$Changes.Count; Uids=$requiredUids.Count }
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
    $files = @(Get-UESTCFiles $Root)
    $formalGlsl = @($files | Where-Object {
        $_ -match '^(game|pipeline)/.*\.glsl$' -and $_ -notmatch '(^|/)(addons|output|obj|bin|\.godot)/' -and
            (Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf)
    })
    if ($formalGlsl.Count -gt 1) {
        $failures.Add("Formal GLSL entry limit is one: $($formalGlsl -join ', ')")
    }
    foreach ($path in $formalGlsl) {
        if ($path -cne 'game/style/outline/post_outline.glsl') {
            $failures.Add("Unapproved formal GLSL entry: $path; only game/style/outline/post_outline.glsl is allowed")
        }
    }
    foreach ($path in $files) {
        if ($path -notmatch '^(game|pipeline)/' -or $path -match '^game/addons/|/(obj|bin)/') { continue }
        $file = Join-Path $Root $path
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
        if ($path -match '\.gd$') { $failures.Add("GDScript outside addons: $path"); continue }
        if ($path -match '\.(tscn|tres)$' -and $path -notin @('game/stage/main.tscn','game/hud/ui_theme.tres')) { $failures.Add("Shared resource outside allowlist: $path") }
        if ($path -notmatch '\.(cs|gdshader|gdshaderinc|glsl|glslinc)$' -or $path -match '/tests/|^game/testing/') { continue }
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
            if ($path -notmatch '^game/kernel/map/|^game/gates/|^game/gen/maps/') {
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
