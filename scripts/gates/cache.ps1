function Get-UESTCStringHash {
    param([string]$Text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-UESTCInputHash {
    param([string]$Root, [string]$Wp, [string]$BaseRef, [string]$Configuration, [string]$Godot, [string[]]$Changes)
    $parts = New-Object 'Collections.Generic.List[string]'
    $parts.Add("gate-cache-v1|$Wp|$BaseRef|$Configuration|$Godot")
    $sdk = Invoke-UESTCProcess dotnet @('--version') $Root (Join-Path $Root 'output/build/gates/cache-sdk.log')
    $parts.Add($sdk.Output.Trim())
    if (Test-Path -LiteralPath $Godot) { $engine = Get-Item -LiteralPath $Godot; $parts.Add("$($engine.Length)|$($engine.LastWriteTimeUtc.Ticks)") }
    $parts.Add(($Changes -join "`n"))
    $parts.Add((Invoke-UESTCGit $Root @('status', '--porcelain=v1', '--untracked-files=all')).StdOut)
    foreach ($path in (Get-UESTCFiles $Root | Where-Object { $_ -notmatch '^docs/|\.md$' })) {
        $absolute = Join-Path $Root $path
        if (Test-Path -LiteralPath $absolute -PathType Leaf) { $parts.Add($path + '=' + (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash) }
        else { $parts.Add($path + '=deleted') }
    }
    return Get-UESTCStringHash ($parts -join "`n")
}

function Get-UESTCBinaryHash {
    param([string]$Root)
    $directories = @((Join-Path $Root 'output/build'), (Join-Path $Root 'game/.godot/mono'), (Join-Path $Root 'game/bin'))
    $binaries = @($directories | Where-Object { Test-Path -LiteralPath $_ -PathType Container } | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Filter 'Campus*.dll' -Recurse -File
    } | Sort-Object FullName -Unique)
    if (-not @($binaries | Where-Object { $_.Name -eq 'Campus.dll' }).Count) { return '' }
    $parts = @($binaries | ForEach-Object { $_.FullName + '=' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    return Get-UESTCStringHash ($parts -join "`n")
}
