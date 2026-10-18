# 命名检查函数库（Test-UESTCSourceNaming）：检查规范文档、数据叶文件名及 C# 文件名与命名空间。
# 用法：由 scripts\gate.ps1 点引用（. scripts\gates\check-naming.ps1）后调用 Test-UESTCSourceNaming -Root <仓库根>，不单独运行
function Test-UESTCNamespaceRole {
    param([string]$Path, [string]$Namespace, [string]$ProjectNamespace = '')
    if ($Namespace -cnotmatch '^Campus(?:\.[A-Z][A-Za-z0-9]*)+$') { return $false }
    if ($Path -match '/tests/|^scripts/gates/arch/') { return $true }
    if ($Path -match '^game/contracts/') {
        return $Namespace -cmatch '^Campus\.Contracts(?:\.(?:Quality|Maps|Atlas|AppFlow|Cinematic|Save|Audio|Bake|Parts|Entities|Startup|Settings|Content|Vegetation|Water|Fauna)(?:\.[A-Z][A-Za-z0-9]*)*)?$'
    }
    if ($Path -match '^pipeline/map/') {
        # These sources are compiled into Campus.Gen, retaining their pipeline responsibility.
        return $Namespace -ceq 'Campus.Pipeline.Map' -or $Namespace.StartsWith('Campus.Pipeline.Map.', [StringComparison]::Ordinal)
    }
    if ($Path -match '^game/gen/') {
        # A merged project may contain both generic and map-specific responsibilities.
        $owners = @{
            geometry='Geometry'; world='World'; buildings='Buildings'; ground='Ground'; vegetation='Vegetation';
            water='Water'; fauna='Fauna'; furniture='Furniture'; collision='Collision'; diorama='Diorama';
            'interior/core'='Interior'; 'interior/shell'='Interior.Shell';
            'parts/core'='Parts.Core'; 'parts/openings'='Parts.Openings'; 'parts/circulation'='Parts.Circulation';
            'parts/roof'='Parts.Roof'; 'parts/street'='Parts.Street'; 'parts/_stubs'='Parts.Stubs';
            'zones/common'='Zones.Common'; 'zones/core'='Zones.Core'; 'zones/north'='Zones.North'; 'zones/south'='Zones.South';
            'zones/west'='Zones.West'; 'zones/center-east'='Zones.CenterEast';
            'zones/design-boundary'='Zones.DesignBoundary'; 'zones/design-public'='Zones.DesignPublic';
            'zones/design-waterfront'='Zones.DesignWaterfront'
        }
        foreach ($owner in $owners.Keys) {
            if ($Path.StartsWith('game/gen/' + $owner + '/', [StringComparison]::Ordinal)) {
                $expected = 'Campus.Gen.' + $owners[$owner]
                return $Namespace -ceq $expected -or $Namespace.StartsWith($expected + '.', [StringComparison]::Ordinal)
            }
        }
        return $ProjectNamespace -and ($Namespace -ceq $ProjectNamespace -or $Namespace.StartsWith($ProjectNamespace + '.', [StringComparison]::Ordinal))
    }
    $roles = @{
        kernel='Kernel'; style='Style'; camera='Camera'; medium='Medium'; systems='Systems';
        hud='Hud'; stage='Stage'; gates='Gates'; flow='Flow'; audio='Audio'; entities='Entities'
    }
    if ($Path -match '^game/kernel/stubs/') {
        # D0 stubs live together physically, but semantic API ownership still uses
        # the real responsibility namespace (and is checked by ArchUnitNET).
        return $Namespace -cmatch '^Campus\.(?:Kernel|Style|Camera|Medium|Systems|Hud|Stage|Gates|Flow|Audio)(?:\.[A-Z][A-Za-z0-9]*)*$'
    }
    $parts = $Path -split '/'
    if ($parts.Count -gt 2 -and $parts[0] -eq 'game' -and $roles.ContainsKey($parts[1])) {
        $expected = 'Campus.' + $roles[$parts[1]]
        return $Namespace -ceq $expected -or $Namespace.StartsWith($expected + '.', [StringComparison]::Ordinal)
    }
    return $false
}

function Test-UESTCSourceNaming {
    param([string]$Root)
    $failures = New-Object 'Collections.Generic.List[string]'
    $checked = 0
    foreach ($path in (Get-UESTCFiles $Root)) {
        $absolute = Join-Path $Root $path
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        $fileName = [IO.Path]::GetFileName($path)
        if ($path -match '^docs/.*\.md$' -and $path -notmatch '^docs/godot-api/' -and $fileName -cnotin @('README.md', 'SECTIONS.md')) {
            if ($fileName -cnotmatch '^[A-Z]+(?:-[A-Z]+)+-[^-]+\.md$' -or $fileName -notmatch '[\p{IsCJKUnifiedIdeographs}]') {
                $failures.Add("${path}: docs file names require an uppercase hyphenated code and a Chinese title (CORE-CONVENTIONS)")
            }
            $checked++
        }
        if ($path -match '^game/data/') {
            if ($fileName -match 'v2|^WP-') {
                $failures.Add("${path}: data leaf file names must not contain v2 or start with WP- (CORE-CONVENTIONS)")
            }
            $checked++
        }
        if ($path -notmatch '^(game/|pipeline/|scripts/gates/arch/).*\.cs$' -or $path -match '^game/addons/|/(obj|bin)/') { continue }
        if ([IO.Path]::GetFileNameWithoutExtension($path) -cnotmatch '^[A-Z][A-Za-z0-9]*(?:\.[A-Z][A-Za-z0-9]*)*$') {
            $failures.Add("${path}: C# authored file names use PascalCase segments")
        }
        $text = [IO.File]::ReadAllText($absolute)
        # Comments and literals cannot declare namespaces. Generated obj/bin files
        # and the vendored gdUnit plugin are excluded before this source check.
        $text = [regex]::Replace($text, '@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*''|/\*[\s\S]*?\*/|//[^\r\n]*', ' ')
        $namespaces = [regex]::Matches($text, '(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]')
        if (-not $namespaces.Count -and $text -match '\b(class|record|struct|interface|enum)\s+[A-Za-z_]') {
            $failures.Add("${path}: production and test types require an explicit Campus namespace")
        }
        $projectNamespace = ''
        if ($path -match '^game/gen/') {
            $directory = Split-Path $absolute -Parent
            while ($directory.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
                $projects = @(Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File)
                if ($projects.Count) {
                    if ($projects.Count -ne 1) { $failures.Add("${path}: ambiguous owning project"); break }
                    [xml]$project = [IO.File]::ReadAllText($projects[0].FullName)
                    $declared = $project.SelectSingleNode('/Project/PropertyGroup/RootNamespace')
                    $projectNamespace = if ($declared) { $declared.InnerText } else { $projects[0].BaseName }
                    break
                }
                $directory = Split-Path $directory -Parent
            }
        }
        foreach ($match in $namespaces) {
            $name = $match.Groups[1].Value
            if (-not (Test-UESTCNamespaceRole $path $name $projectNamespace)) {
                $failures.Add("${path}: namespace '$name' violates PascalCase or its assembly responsibility")
            }
        }
        $checked++
    }
    if ($failures.Count) { throw ($failures -join "`n") }
    [pscustomobject]@{ CheckedFiles=$checked; Rule='CORE-CONVENTIONS: docs/data file names, PascalCase and assembly responsibility; no literal directory-case coupling' }
}
