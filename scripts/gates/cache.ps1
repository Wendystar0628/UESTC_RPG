function Get-UESTCStringHash {
    param([string]$Text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-UESTCCachePaths {
    param([string]$Root, [string]$Wp)
    $files = @(Get-UESTCFiles $Root | Where-Object { $_ -notmatch '^docs/|\.md$|^\.collab/|^output/' })
    $ownership = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Root 'ownership.json') | ConvertFrom-Json
    $rules = @($ownership.rules | Sort-Object @{Expression={ ($_.pattern -replace '[*?]', '').Length };Descending=$true} | ForEach-Object {
        [pscustomobject]@{ Owner=[string]$_.owner; Pattern='^'+[regex]::Escape($_.pattern.Replace('\','/')).Replace('\*\*','__DOUBLE_STAR__').Replace('\*','[^/]*').Replace('__DOUBLE_STAR__','.*').Replace('\?','[^/]')+'$' }
    })
    $paths = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $projects = @($files | Where-Object { $_ -match '\.csproj$' })
    $pending = New-Object 'Collections.Generic.Queue[string]'
    foreach ($path in $files) {
        $owner = ''
        foreach ($rule in $rules) { if ($path -match $rule.Pattern) { $owner=$rule.Owner; break } }
        if ($owner -eq $Wp -or ($Wp -cmatch '^WP-[0-9]{2}$' -and $owner -cmatch ('^'+[regex]::Escape($Wp)+'[a-z]$'))) {
            $null = $paths.Add($path)
            # The nearest project owns the package's production compilation.
            $project = @(if ($path -in $projects) { $path } else { $projects | Where-Object {
                $_ -notmatch '\.Tests\.csproj$' -and $path.StartsWith(($_ -replace '/[^/]+$', '')+'/', [StringComparison]::OrdinalIgnoreCase)
            } | Sort-Object Length -Descending | Select-Object -First 1 })
            if ($project.Count) { $pending.Enqueue($project[0]) }
        }
    }
    $visited = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    while ($pending.Count) {
        $project = $pending.Dequeue()
        if (-not $visited.Add($project)) { continue }
        $directory = $project -replace '/[^/]+$', ''
        $aggregate = $directory -eq 'game' -or $project -match '(?:\.Tests|scripts/gates/arch/)'
        $null = $paths.Add($project)
        foreach ($path in $files) {
            if (-not $aggregate -and $path.StartsWith($directory+'/', [StringComparison]::OrdinalIgnoreCase)) {
                $null = $paths.Add($path)
            }
            if ($aggregate -and $path -in @("$directory/packages.lock.json", "$directory/$([IO.Path]::GetFileNameWithoutExtension($project)).packages.lock.json")) { $null = $paths.Add($path) }
        }
        if (-not (Test-Path -LiteralPath (Join-Path $Root $project))) { continue }
        [xml]$xml = [IO.File]::ReadAllText((Join-Path $Root $project))
        foreach ($reference in $xml.SelectNodes("//*[local-name()='ProjectReference']/@Include")) {
            foreach ($include in ($reference.Value -split ';')) {
                # Campus and the test hosts aggregate every generator. Their
                # wildcard is not an upstream dependency of each individual package.
                if ($aggregate -and $include -match '[*?]') { continue }
                $pattern = ConvertTo-UESTCG2RelativePath $directory $include
                foreach ($dependency in $projects) {
                    if (Test-UESTCGlob $dependency $pattern) { $pending.Enqueue($dependency) }
                }
            }
        }
    }
    # Shared gate/build inputs affect every package; unrelated content does not.
    foreach ($path in $files) {
        # Map snapshots, the full palette and global data assets are read outside
        # ProjectReference by generators and tests, so remain shared inputs.
        if ($path -match '^game/(data/|style/palette/|kernel/registry/)|^scripts/(gate\.ps1$|gates/|wt/|build/|setup/install-test-runner\.ps1$)|^(ownership\.json|global\.json|\.editorconfig|\.gitattributes|\.gitignore|NuGet\.Config)$|(^|/)Directory\.[^/]+\.(props|targets)$|^tools/godot-version\.txt$|^game/(project\.godot|Campus\.sln)$') {
            $null = $paths.Add($path)
        }
    }
    return @($paths | Sort-Object)
}

function Get-UESTCInputHash {
    param([string]$Root, [string]$Wp, [string]$BaseRef, [string]$Options, [string]$Godot, [string[]]$Changes, [string[]]$Paths=@())
    $parts = New-Object 'Collections.Generic.List[string]'
    $parts.Add("gate-cache-v4-package|$Wp|$Options|$Godot")
    $parts.Add("PowerShell=$($PSVersionTable.PSVersion)")
    $sdk = Invoke-UESTCProcess dotnet @('--version') $Root (Join-Path $Root 'output/build/gates/cache-sdk.log')
    $parts.Add($sdk.Output.Trim())
    if (Test-Path -LiteralPath $Godot) { $engine = Get-Item -LiteralPath $Godot; $parts.Add("$($engine.Length)|$($engine.LastWriteTimeUtc.Ticks)") }
    if (-not $Paths.Count) { $Paths = @(Get-UESTCCachePaths $Root $Wp) }
    foreach ($path in $Paths) {
        $absolute = Join-Path $Root $path
        if (Test-Path -LiteralPath $absolute -PathType Leaf) { $parts.Add($path + '=' + (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash) }
        else { $parts.Add($path + '=deleted') }
    }
    # Discovery consumes declarations from all modules, not their unrelated bodies.
    foreach ($path in (Get-UESTCFiles $Root | Where-Object { (Test-UESTCProductionCs $_) -or $_ -match '\.csproj$' })) {
        $absolute = Join-Path $Root $path
        if (Test-Path -LiteralPath $absolute -PathType Leaf) {
            $registration = Get-UESTCRegistrationSignature ([IO.File]::ReadAllText($absolute))
            if ($registration) { $parts.Add("registration:$path=$registration") }
        }
    }
    return Get-UESTCStringHash ($parts -join "`n")
}

function Get-UESTCOwnershipUidHash {
    param([string]$Root)
    $tracked = @(Get-UESTCGitLines $Root @('ls-files','--cached','--','*.uid'))
    $parts = @("tracked=$($tracked -join "`n")")
    foreach ($path in (Get-UESTCFiles $Root | Where-Object { $_ -match '^game/.*\.uid$' })) {
        $absolute = Join-Path $Root $path
        $value = if (Test-Path -LiteralPath $absolute -PathType Leaf) { (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash } else { 'deleted' }
        $parts += "$path=$value"
    }
    return Get-UESTCStringHash ($parts -join "`n")
}

function Get-UESTCBinaryHash {
    param([string]$Root, [string]$Wp, [string[]]$Paths=@())
    if (-not $Paths.Count) { $Paths = @(Get-UESTCCachePaths $Root $Wp) }
    $names = @($Paths | Where-Object { $_ -match '\.csproj$' -and $_ -notmatch '(?:^game/Campus(?:\.Gen)?(?:\.Tests)?\.csproj$|\.Arch\.Tests\.csproj$)' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_)+'.dll' })
    $directories = @((Join-Path $Root 'output/build'), (Join-Path $Root 'game/.godot/mono'), (Join-Path $Root 'game/bin'))
    $binaries = @($directories | Where-Object { Test-Path -LiteralPath $_ -PathType Container } | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Filter 'Campus*.dll' -Recurse -File | Where-Object { $_.Name -in $names -or $_.Name -eq 'Campus.dll' }
    } | Sort-Object FullName -Unique)
    if (-not @($binaries | Where-Object { $_.Name -eq 'Campus.dll' }).Count) { return '' }
    $parts = @($binaries | Where-Object { $_.Name -ne 'Campus.dll' } | ForEach-Object { $_.FullName + '=' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    return Get-UESTCStringHash ($parts -join "`n")
}
