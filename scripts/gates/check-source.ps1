# 源码与归属检查函数库：列出改动文件，按文档 paths 核对归属与 UID，并检查着色器白名单。
# 产出：无（只读检查源码与归属并返回内存结果）。
# 用法：由 scripts\gate.ps1 点引用（. scripts\gates\check-source.ps1）后调用 Test-UESTCOwnership、Test-UESTCSourceBoundaries 等函数，不单独运行
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
    $powershell = @($files | Where-Object { $_ -match '^scripts/.*\.(ps1|psm1|psd1)$' })
    if ($powershell.Count) {
        # Parse saved bytes with the same Windows PowerShell used by build Exec tasks.
        $paths = @($powershell | ForEach-Object { "'" + (Join-Path $Root $_).Replace("'", "''") + "'" }) -join ",`n"
        $code = @'
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$failed = $false
foreach ($path in @(
__SCRIPT_PATHS__
)) {
    $tokens = $null
    $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$parseErrors)
    foreach ($parseError in $parseErrors) {
        [Console]::WriteLine(('{0}:{1}: {2}' -f $parseError.Extent.File, $parseError.Extent.StartLineNumber, $parseError.Message))
        $failed = $true
    }
}
if ($failed) { exit 1 }
exit 0
'@
        $code = $code.Replace('__SCRIPT_PATHS__', $paths)
        $hostPath = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
        $null = Invoke-UESTCProcess $hostPath @('-NoProfile', '-NonInteractive', '-EncodedCommand',
            [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($code))) $Root -TextEncoding ([Text.Encoding]::UTF8)
    }
    $python = @($files | Where-Object { $_ -match '^(dashboard/tools|pipeline|scripts)/.*\.py$' })
    if ($python.Count) {
        $code = 'import ast, pathlib, sys; [ast.parse(pathlib.Path(p).read_text(encoding="utf-8"), filename=p) for p in sys.argv[1:]]'
        $null = Invoke-UESTCProcess python (@('-I','-B','-X','utf8','-c',$code) + $python) $Root -TextEncoding ([Text.Encoding]::UTF8)
    }
    $javascript = @($files | Where-Object { $_ -match '^dashboard/app/.*\.(js|mjs|cjs)$' })
    foreach ($path in $javascript) {
        $null = Invoke-UESTCProcess node @('--check', (Join-Path $Root $path)) $Root -TextEncoding ([Text.Encoding]::UTF8)
    }
    [pscustomobject]@{ PowerShellFiles=$powershell.Count; PythonFiles=$python.Count; JavaScriptFiles=$javascript.Count }
}

function Get-UESTCDocumentPathRules {
    param([string]$Root)
    $text = [IO.File]::ReadAllText((Join-Path $Root 'docs/ROUTES.json'), [Text.UTF8Encoding]::new($false, $true))
    $routes = $text | ConvertFrom-Json -ErrorAction Stop
    if ($null -eq $routes -or $routes.docs -isnot [pscustomobject] -or $routes.paths -isnot [array]) {
        throw 'docs/ROUTES.json must contain document paths and a paths array'
    }
    foreach ($route in $routes.paths) {
        if ($route.glob -isnot [string] -or [string]::IsNullOrWhiteSpace($route.glob) -or
            $route.docs -isnot [array] -or -not $route.docs.Count) {
            throw 'docs/ROUTES.json contains an invalid document path rule'
        }
        foreach ($code in $route.docs) {
            if ($code -isnot [string] -or [string]::IsNullOrWhiteSpace($code) -or
                -not $routes.docs.PSObject.Properties[$code] -or
                $routes.docs.PSObject.Properties[$code].Value -isnot [string] -or
                [string]::IsNullOrWhiteSpace($routes.docs.PSObject.Properties[$code].Value)) {
                throw "docs/ROUTES.json contains an invalid document reference: $code"
            }
        }
        [pscustomobject]@{ Pattern=$route.glob; Docs=@($route.docs) }
    }
}

function Test-UESTCOwnershipRules {
    param([string]$Root)
    $rules = @(Get-UESTCDocumentPathRules $Root)
    $files = @(Get-UESTCFiles $Root | Where-Object { Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf })
    $unmatched = [Collections.Generic.List[string]]::new()
    foreach ($rule in $rules) {
        $expression = Get-UESTCGlobExpression $rule.pattern
        if (@($files -match $expression).Count) { continue }
        # Ignored collaboration tools are real owned files, even though git omits them.
        $prefix = ($rule.pattern -split '[*?]', 2)[0]
        if ($prefix -eq $rule.pattern) {
            $ignored = Invoke-UESTCGit $Root @('check-ignore', '--quiet', '--', $prefix.Replace('\','/')) -SuccessExitCodes @(0, 1)
            if ($ignored.ExitCode -eq 0) { continue }
            $matched = Test-Path -LiteralPath (Join-Path $Root $prefix) -PathType Leaf
        } else {
            $directory = if ($prefix.EndsWith('/')) { $prefix.TrimEnd('/') } else { Split-Path $prefix -Parent }
            # An ignored directory covers the entire glob, including an absent local scope.
            # Query the literal containing directory, never a sample matching ignored file.
            if ($directory) {
                $ignored = Invoke-UESTCGit $Root @('check-ignore', '--quiet', '--', ($directory.Replace('\','/').TrimEnd('/') + '/')) -SuccessExitCodes @(0, 1)
                if ($ignored.ExitCode -eq 0) { continue }
            }
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
    if ($unmatched.Count) { throw ("Document path rules match no existing files:`n" + ($unmatched -join "`n")) }
    [pscustomobject]@{ CheckedRules=$rules.Count }
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

# Deleted paths have no current document ownership to validate.
function Get-UESTCPathOwnership {
    param([string]$Root, [string[]]$Paths)
    $rules = @(Get-UESTCDocumentPathRules $Root)
    foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $path) -PathType Leaf)) { continue }
        $documents = @($rules | Where-Object { Test-UESTCGlob $path $_.Pattern } |
            ForEach-Object { $_.Docs } | Sort-Object -Unique)
        [pscustomobject]@{ Path=$path; Owner=$documents }
    }
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
    param([string]$Root, [string[]]$Changes)
    $failures = New-Object 'Collections.Generic.List[string]'
    foreach ($entry in (Get-UESTCPathOwnership $Root $Changes)) {
        if (-not $entry.Owner) { $failures.Add("Ownership: $($entry.Path) has no document path ownership") }
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
    $whitelistText = [IO.File]::ReadAllText((Join-Path $Root 'scripts/gates/shader-whitelist.json'), [Text.UTF8Encoding]::new($false, $true))
    if (-not $whitelistText.TrimStart().StartsWith('[')) { throw 'Shader whitelist must be an array' }
    $shaderWhitelist = $whitelistText | ConvertFrom-Json -ErrorAction Stop
    if (@($shaderWhitelist | Where-Object { $_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_) }).Count) {
        throw 'Shader whitelist paths must be non-empty strings'
    }
    $failures = New-Object 'Collections.Generic.List[string]'
    foreach ($path in (Get-UESTCFiles $Root)) {
        if ($path -notmatch '^(game|pipeline)/.*\.(gdshader|gdshaderinc|glsl|glslinc)$' -or
            $path -match '^game/addons/|/(obj|bin)/' -or
            -not (Test-Path -LiteralPath (Join-Path $Root $path) -PathType Leaf)) { continue }
        $allowed = @($shaderWhitelist | Where-Object { Test-UESTCGlob $path $_ })
        if (-not $allowed.Count) { $failures.Add("Shader not in whitelist: $path") }
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ Status = 'Passed'; Scope = 'Shader whitelist' }
}
