param([Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)][string]$Output,[string]$Configuration='Debug')
$ErrorActionPreference='Stop'
. (Join-Path $Root 'scripts/wt/common.ps1')
$rootPath=[IO.Path]::GetFullPath($Root)
$version=(Get-Content -LiteralPath (Join-Path $rootPath 'version.txt') -Raw -Encoding UTF8).Trim()
$commit=(Invoke-UESTCGit $rootPath @('rev-parse','--short=12','HEAD')).StdOut.Trim()
if(@(Get-UESTCGitLines $rootPath @('status','--porcelain')).Count -gt 0){$commit+="-dirty"}
$kind=if($Configuration -eq 'Debug'){'dev'}else{'export'}
# Hash authored input closures, not build/test products. Paths in the digest are repository-relative.
function Get-RelativeInput([string]$path) {
  $path=[IO.Path]::GetFullPath($path)
  if(-not $path.StartsWith($rootPath.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw "Input outside repository: $path" }
  $path.Substring($rootPath.TrimEnd('\','/').Length+1).Replace('\','/')
}
function Test-AuthoredInput([string]$path) {
  $relative=Get-RelativeInput $path
  $relative -notmatch '(?i)(^|/)(obj|bin|tests|\.godot|\.git|gdunit4_testadapter[^/]*)(/|$)' -and
    $relative -notmatch '(?i)(^|/)[^/]*Tests[^/]*\.(csproj|packages\.lock\.json)$'
}
$authored=@(Get-UESTCGitLines $rootPath @('ls-files','--cached','--others','--exclude-standard') | ForEach-Object {
  $p=Join-Path $rootPath $_
  if((Test-Path -LiteralPath $p -PathType Leaf) -and (Test-AuthoredInput $p)) { [IO.Path]::GetFullPath($p) }
})
function Resolve-InputPattern([string]$folder,[string]$pattern,[bool]$required) {
  $pattern=$pattern.Replace('$(MSBuildProjectDirectory)',$folder).Replace('$(MSBuildThisFileDirectory)',$folder+[IO.Path]::DirectorySeparatorChar)
  if($pattern.Contains('$(')) { throw "Unsupported authored input expression '$pattern' in $folder" }
  $safe=$pattern.Replace('*','__WILDCARD_STAR__').Replace('?','__WILDCARD_QUESTION__')
  $joined=if([IO.Path]::IsPathRooted($safe)){$safe}else{Join-Path $folder $safe}
  $absolute=[IO.Path]::GetFullPath($joined).Replace('__WILDCARD_STAR__','*').Replace('__WILDCARD_QUESTION__','?')
  if(-not $absolute.StartsWith($rootPath.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw "Input pattern outside repository: $pattern"}
  $relative=$absolute.Substring($rootPath.TrimEnd('\','/').Length+1).Replace('\','/')
  if($relative -match '[*?]') {
    $rx=[regex]::Escape($relative).Replace('\*\*/','(?:.*/)?').Replace('\*\*','.*').Replace('\*','[^/]*').Replace('\?','[^/]')
    $matches=@($authored | Where-Object {(Get-RelativeInput $_) -cmatch ('^'+$rx+'$')})
  } else { $matches=@(if(Test-Path -LiteralPath $absolute -PathType Leaf){$absolute}) }
  if($required -and -not $matches.Count) { throw "Missing referenced input '$pattern' in $folder" }
  $matches
}
function Get-BuildControls([string]$folder) {
  while($folder.Length -ge $rootPath.TrimEnd('\','/').Length) {
    Get-ChildItem -LiteralPath $folder -File | Where-Object {
      $_.Name -match '^Directory\..*\.(props|targets)$|^global\.json$|^NuGet\.Config$|^nuget\.config$|^\.editorconfig$'
    } | ForEach-Object FullName
    if($folder.TrimEnd('\','/') -eq $rootPath.TrimEnd('\','/')) { break }
    $folder=Split-Path $folder -Parent
  }
}
$projectInputCache=@{}
function Get-LocalProjectInputs([string]$project) {
  $project=[IO.Path]::GetFullPath($project)
  if(-not(Test-Path -LiteralPath $project -PathType Leaf)) { throw "Missing project: $project" }
  if($projectInputCache.ContainsKey($project)){return $projectInputCache[$project]}
  $references=[Collections.Generic.List[string]]::new()
  $files=@(& {
  $folder=Split-Path $project
  $project
  # Conservative superset of authored sources/resources/MSBuild inputs, including locks.
  $prefix=(Get-RelativeInput $project).Substring(0,(Get-RelativeInput $project).LastIndexOf('/')+1)
  $authored | Where-Object {(Get-RelativeInput $_).StartsWith($prefix,[StringComparison]::Ordinal) -and $_ -match '\.(cs|csproj|props|targets|json|resx|config|editorconfig|ruleset|txt|ps1)$'}
  Get-ChildItem -LiteralPath $folder -File -Filter '*lock.json' | Where-Object {Test-AuthoredInput $_.FullName} | ForEach-Object FullName
  $controls=@(Get-BuildControls $folder)
  $controls
  $pending=[Collections.Generic.Queue[string]]::new()
  foreach($file in @($project)+@($controls | Where-Object {$_ -match '\.(props|targets)$'})) {$pending.Enqueue($file)}
  $read=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  while($pending.Count) {
    $input=$pending.Dequeue()
    if(-not $read.Add($input)){continue}
    [xml]$xml=Get-Content -LiteralPath $input -Raw -Encoding UTF8
    foreach($reference in $xml.SelectNodes('//ProjectReference')) {
      foreach($part in $reference.Include.Split(';')) {
        if($part.Trim()) { foreach($candidate in @(Resolve-InputPattern (Split-Path $input) $part $true)) { [void]$references.Add($candidate) } }
      }
    }
    foreach($item in $xml.SelectNodes('//Compile[@Include]|//AdditionalFiles[@Include]|//EmbeddedResource[@Include]|//Content[@Include]|//None[@Include]|//Import[@Project]')) {
      $pattern=if($item.LocalName -eq 'Import'){$item.Project}else{$item.Include}
      # Generated Compile items (e.g. BuildInfo.g.cs) are explicitly not authored inputs.
      if($pattern -match '\$\(IntermediateOutputPath\)|\$\(BaseIntermediateOutputPath\)') { continue }
      foreach($part in $pattern.Split(';')) {
        if(-not $part.Trim()) { continue }
        $required= -not $item.HasAttribute('Condition')
        foreach($candidate in @(Resolve-InputPattern (Split-Path $input) $part $required)) {
          if(Test-AuthoredInput $candidate) { $candidate }
          if($item.LocalName -eq 'Import') { $pending.Enqueue($candidate) }
        }
      }
    }
  }
  })
  $model=[pscustomobject]@{Files=$files;References=$references.ToArray()}
  $projectInputCache[$project]=$model
  $model
}
function Get-ProjectInputs([string]$project,[Collections.Generic.HashSet[string]]$seen) {
  $project=[IO.Path]::GetFullPath($project)
  if(-not $seen.Add($project)){return}
  $model=Get-LocalProjectInputs $project
  $model.Files
  foreach($reference in $model.References){Get-ProjectInputs $reference $seen}
}
$moduleProjects=@(Get-ChildItem -LiteralPath (Join-Path $rootPath 'game/gen') -Recurse -Filter *.csproj | Where-Object {Test-AuthoredInput $_.FullName})
$moduleProjects+=Get-Item -LiteralPath (Join-Path $rootPath 'pipeline/map/Campus.Pipeline.Map.csproj')
$moduleNames=[string[]]@($moduleProjects | ForEach-Object BaseName)
[Array]::Sort($moduleNames,[StringComparer]::Ordinal)
$names=@($moduleNames | ForEach-Object {'"'+$_+'"'}) -join ','
$projects=@($moduleProjects)+@(Get-Item -LiteralPath (Join-Path $rootPath 'game/Campus.csproj'))
$hashLines=[Collections.Generic.List[string]]::new()
$byName=@{}
foreach($project in $projects) {
  if($byName.ContainsKey($project.BaseName)) { throw "Duplicate module assembly: $($project.BaseName)" }
  $byName[$project.BaseName]=$project
}
$hashNames=[string[]]@($byName.Keys);[Array]::Sort($hashNames,[StringComparer]::Ordinal)
foreach($name in $hashNames) {
  $project=$byName[$name]
  $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $inputs=@(Get-ProjectInputs $project.FullName $seen)
  $inputs+=@('scripts/build/build-info.ps1','scripts/wt/common.ps1','version.txt') | ForEach-Object {Join-Path $rootPath $_}
  $relativeInputs=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($file in $inputs) { if(Test-AuthoredInput $file){[void]$relativeInputs.Add((Get-RelativeInput $file))} }
  $ordered=[string[]]@($relativeInputs);[Array]::Sort($ordered,[StringComparer]::Ordinal)
  $builder=[Text.StringBuilder]::new()
  foreach($relative in $ordered) {
    [void]$builder.Append($relative).Append(':').Append((Get-FileHash -LiteralPath (Join-Path $rootPath $relative) -Algorithm SHA256).Hash).Append("`n")
  }
  $sha=[Security.Cryptography.SHA256]::Create()
  try{$hash=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($builder.ToString())))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
  $hashLines.Add('["'+$name+'"]="'+$hash+'"')
}
$source=@"
// <auto-generated />
namespace Campus.Kernel.Registry;
internal static class BuildInfo {
 public const string Version = "$version";
 public const string Commit = "$commit";
 public const string Build = "$version+$commit";
 public const string Kind = "$kind";
 public static readonly string[] ModuleAssemblies = [$names];
 public static readonly System.Collections.Generic.IReadOnlyDictionary<string,string> ModuleCodeHashes = new System.Collections.Generic.Dictionary<string,string> { $($hashLines -join ',') };
}
"@
$destination=[IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
if(-not(Test-Path -LiteralPath $destination) -or [IO.File]::ReadAllText($destination) -ne $source){[IO.File]::WriteAllText($destination,$source.Replace("`r`n","`n"),[Text.UTF8Encoding]::new($false))}
