# Script-only smoke tests: no build, import or global locks.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../lib/common.ps1')
. (Join-Path $PSScriptRoot 'check-source.ps1')
. (Join-Path $PSScriptRoot 'cache.ps1')
. (Join-Path $PSScriptRoot 'select-service-window.ps1')
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $root 'output/build/gates/script-self-test'
$null = [IO.Directory]::CreateDirectory($output)
$locks = Join-Path $output 'locks'
$leases = New-Object 'Collections.Generic.List[object]'
function Assert-True { param([bool]$Value, [string]$Message) if (-not $Value) { throw $Message } }
# Load the existing selectors without executing gate.ps1.
$tokens=$null; $parseErrors=$null
$gateAst = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot '../gate.ps1'), [ref]$tokens, [ref]$parseErrors)
foreach ($definition in $gateAst.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -match 'UESTCG2' }, $true)) {
    Invoke-Expression $definition.Extent.Text
}
$files = @(Get-UESTCFiles $root)
$gen = @(Get-UESTCG2ProjectSelection $root @('game/gen/vegetation/VegetationModule.cs') $files)
Assert-True ($gen[0].Selected -and -not $gen[1].Selected) 'Generator edit selected unrelated runtime tests.'
$runtime = @(Get-UESTCG2ProjectSelection $root @('game/flow/Flow.cs') $files)
Assert-True (-not $runtime[0].Selected -and $runtime[1].Selected) 'Runtime edit selected unrelated generator tests.'
$hostEdit = @(Get-UESTCG2ProjectSelection $root @('game/Campus.Gen.Tests.csproj') $files)
Assert-True ($hostEdit[0].Selected -and -not $hostEdit[1].Selected) 'A test-host edit forced all projects.'
Assert-True (@(Get-UESTCG2ProjectSelection $root @('game/data/maps/index.json') $files | Where-Object Selected).Count -eq 2) 'Shared map snapshot did not select both consumers.'
Assert-True (Test-UESTCG5Impact $root HEAD @('game/kernel/boot/Boot.cs')) 'Kernel edit did not select G5.'
Assert-True (Test-UESTCG5Impact $root HEAD @('game/data/maps/index.json')) 'Map edit did not select G5.'
Assert-True (-not (Test-UESTCG5Impact $root HEAD @('game/gen/vegetation/VegetationModule.cs','game/gen/zones/south/SouthModule.cs','game/gen/furniture/FurnitureModule.cs'))) 'Unchanged content registrations selected G5.'
$registration = '[ContentModule("a")] public class A { public int Value => 1; }'
Assert-True ((Get-UESTCRegistrationSignature $registration) -ceq (Get-UESTCRegistrationSignature ($registration.Replace('=> 1','=> 2')))) 'Content body changed registration signature.'
Assert-True ((Get-UESTCRegistrationSignature $registration) -cne (Get-UESTCRegistrationSignature ($registration.Replace('"a"','"b"')))) 'Registration edit was missed.'
$constructor = '[KernelService(typeof(IService))] public partial class Service { public Service(IFirst dependency) { Run(1); } public void Run(int value) {} }'
Assert-True ((Get-UESTCRegistrationSignature $constructor) -cne (Get-UESTCRegistrationSignature ($constructor.Replace('IFirst','ISecond')))) 'Public constructor dependency edit was missed.'
Assert-True ((Get-UESTCRegistrationSignature $constructor) -ceq (Get-UESTCRegistrationSignature ($constructor.Replace('Run(1)','Run(2)')))) 'Constructor body changed registration signature.'
$partialConstructor = 'public partial class Service(IFirst dependency) { public int Value => 1; }'
Assert-True ((Get-UESTCRegistrationSignature $partialConstructor) -cne (Get-UESTCRegistrationSignature ($partialConstructor.Replace('IFirst','ISecond')))) 'Primary constructor in an attribute-free partial file was missed.'
Assert-True ((Get-UESTCRegistrationSignature $partialConstructor) -ceq (Get-UESTCRegistrationSignature ($partialConstructor.Replace('=> 1','=> 2')))) 'Partial class body changed registration signature.'
& {
    # A small input fixture reuses the production collector/hash, without an engine or build.
    $fixture = Join-Path $output 'cache-inputs'
    $fixtureFiles = @{
        'ownership.json'='{"rules":[{"pattern":"game/gen/own/**","owner":"WP-18"},{"pattern":"game/gen/other/**","owner":"WP-19"}]}'
        'game/gen/own/Own.csproj'='<Project><ItemGroup><ProjectReference Include="../shared/Shared.csproj;../../Campus.csproj" /></ItemGroup></Project>'
        'game/gen/own/Own.cs'='class Own {}'
        'game/gen/own/Second.cs'='class Second {}'
        'game/gen/shared/Shared.csproj'='<Project />'
        'game/gen/shared/Shared.cs'='class Shared {}'
        'game/gen/other/Other.cs'='class Other {}'
        'game/gen/other/Other.cs.uid'='uid://fixture'
        'game/flow/Service.cs'='public partial class Service(IFirst dependency) {}'
        'game/Campus.csproj'='<Project />'
        'game/packages.lock.json'='{}'
        'game/data/maps/index.json'='{}'
        'game/data/vegetation/species.json'='{}'
        'game/style/palette/base.json'='{}'
        'scripts/gate.ps1'='# tool'
        'scripts/build/build-info.ps1'='# build'
        'scripts/setup/install-test-runner.ps1'='# setup'
        'docs/notes.md'='notes'
        'output/shot.png'='screenshot'
    }
    foreach ($path in $fixtureFiles.Keys) {
        $absolute = Join-Path $fixture $path
        $null = [IO.Directory]::CreateDirectory((Split-Path $absolute -Parent))
        [IO.File]::WriteAllText($absolute, $fixtureFiles[$path])
    }
    function Get-UESTCFiles { param([string]$Root) $fixtureFiles.Keys | Sort-Object }
    function Invoke-UESTCProcess { [pscustomobject]@{ Output='fixture-sdk' } }
    $trackedUids = @('game/gen/other/Other.cs.uid')
    function Get-UESTCGitLines { param($Root,$Arguments) if ($Arguments[0] -eq 'ls-tree') { 'game/gen/other/Other.cs.uid' } elseif ($Arguments[0] -ne 'diff') { $trackedUids } }
    $baseline = Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Own.cs')
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 main Debug 'missing-engine' @('game/gen/own/Own.cs'))) 'Equivalent inputs missed cache after baseline/docs changes.'
    Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Second.cs'))) 'Different selected sources in the same project shared a format cache key.'
    foreach ($path in @('game/gen/other/Other.cs','docs/notes.md','output/shot.png')) {
        [IO.File]::AppendAllText((Join-Path $fixture $path), 'changed')
    }
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Own.cs'))) 'Unrelated content/docs/screenshot invalidated cache.'
    foreach ($path in @('game/gen/own/Own.cs','game/gen/shared/Shared.cs','game/data/maps/index.json','game/data/vegetation/species.json','game/style/palette/base.json','scripts/gate.ps1','scripts/build/build-info.ps1','scripts/setup/install-test-runner.ps1','game/packages.lock.json')) {
        [IO.File]::AppendAllText((Join-Path $fixture $path), 'changed')
        Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Own.cs'))) "Related input did not invalidate cache: $path"
        [IO.File]::WriteAllText((Join-Path $fixture $path), $fixtureFiles[$path])
    }
    Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD 'Debug|changed-option' 'missing-engine' @('game/gen/own/Own.cs'))) 'Option change did not invalidate cache.'
    [IO.File]::WriteAllText((Join-Path $fixture 'game/flow/Service.cs'), $fixtureFiles['game/flow/Service.cs'].Replace('IFirst','ISecond'))
    Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Own.cs'))) 'Partial constructor dependency did not invalidate registration cache.'
    & {
        function Invoke-UESTCGit { param($Root, $Arguments) [pscustomobject]@{ StdOut= $(if ($Arguments[0] -eq 'show') { $fixtureFiles['game/flow/Service.cs'] } else { 'fixture-base' }) } }
        function Get-UESTCGitLines { 'game/flow/Service.cs' }
        Assert-True (Test-UESTCG5Impact $fixture HEAD @('game/flow/Service.cs')) 'Partial constructor dependency did not select G5.'
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'game/flow/Service.cs'), $fixtureFiles['game/flow/Service.cs'])
    $uidBaseline = Get-UESTCOwnershipUidHash $fixture @('game/gen/other/Other.cs')
    $uidPath = Join-Path $fixture 'game/gen/other/Other.cs.uid'
    [IO.File]::WriteAllText($uidPath, 'uid://changed')
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture @('game/gen/other/Other.cs'))) 'Other-package UID content did not invalidate G3.'
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @('game/gen/own/Own.cs'))) 'Other-package UID content invalidated unrelated gates.'
    [IO.File]::WriteAllText($uidPath, $fixtureFiles['game/gen/other/Other.cs.uid'])
    $trackedUids = @()
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture @('game/gen/other/Other.cs'))) 'UID tracked-state change did not invalidate G3.'
    $trackedUids = @('game/gen/other/Other.cs.uid')
    [IO.File]::Delete($uidPath)
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture @('game/gen/other/Other.cs'))) 'UID deletion did not invalidate G3.'
    $binaryDirectory = Join-Path $fixture 'output/build'
    $null = [IO.Directory]::CreateDirectory($binaryDirectory)
    [IO.File]::WriteAllText((Join-Path $binaryDirectory 'Campus.Contracts.dll'), 'contracts')
    Assert-True ((Get-UESTCBinaryHash $fixture WP-18 @('game/contracts/Campus.Contracts.csproj')) -eq '') 'Dependency DLL alone allowed cached production checks.'
    $hostPath = Join-Path $binaryDirectory 'Campus.dll'
    [IO.File]::WriteAllText($hostPath, 'host')
    $binaryBaseline = Get-UESTCBinaryHash $fixture WP-18 @('game/contracts/Campus.Contracts.csproj')
    Assert-True ($binaryBaseline -ne '') 'Existing production host did not enable binary caching.'
    [IO.File]::AppendAllText($hostPath, 'unrelated rebuild')
    Assert-True ($binaryBaseline -eq (Get-UESTCBinaryHash $fixture WP-18 @('game/contracts/Campus.Contracts.csproj'))) 'Unrelated host rebuild invalidated package binary cache.'
    [IO.File]::Delete($hostPath)
    Assert-True ((Get-UESTCBinaryHash $fixture WP-18 @('game/contracts/Campus.Contracts.csproj')) -eq '') 'Removed production host still allowed binary caching.'
}
& {
    $fixture = Join-Path $output ('ownership-scope-' + [Guid]::NewGuid().ToString('N'))
    $fixtureFiles = @{
        'ownership.json'='{"rules":[{"pattern":"scripts/lib/**","owner":"WP-00"},{"pattern":"scripts/gates/**","owner":"WP-27","maintainers":["WP-00"]},{"pattern":"docs/**","owner":"shared"},{"pattern":"game/own/**","owner":"WP-18a"},{"pattern":"game/other/**","owner":"WP-19"},{"pattern":"game/deleted/**","owner":"WP-19"}]}'
        'scripts/lib/common.ps1'='# common'
        'scripts/gates/cache.ps1'='# cache'
        'docs/neighbour.md'='unfinished neighbour document'
        'game/own/Own.cs'='class Own {}'
        'game/own/Own.cs.uid'='uid://own'
        'game/other/Missing.cs'='class Missing {}'
        'game/other/Invalid.cs.uid'='unfinished'
        'game/other/First.cs.uid'='uid://neighbourduplicate'
        'game/other/Second.cs.uid'='uid://neighbourduplicate'
    }
    foreach ($path in $fixtureFiles.Keys) {
        $absolute = Join-Path $fixture $path
        $null = [IO.Directory]::CreateDirectory((Split-Path $absolute -Parent))
        [IO.File]::WriteAllText($absolute,$fixtureFiles[$path])
    }
    $baseOwnership='{"rules":[{"pattern":"game/deleted/**","owner":"WP-18"}]}'
    $tracked=@('game/own/Own.cs','game/own/Own.cs.uid')
    function Get-UESTCFiles { param($Root) $fixtureFiles.Keys }
    $committedUids=@('game/own/Own.cs.uid','game/other/First.cs.uid','game/other/Second.cs.uid')
    $dirtyUids=@()
    function Get-UESTCGitLines {
        param($Root,$Arguments)
        if ($Arguments[0] -eq 'ls-tree') {
            if ($Arguments[3] -eq 'HEAD') { $committedUids } else { 'game/deleted/Old.cs' }
        } elseif ($Arguments[0] -eq 'diff') { $dirtyUids } else { $tracked }
    }
    function Invoke-UESTCGit {
        param($Root,$Arguments)
        if ($Arguments[1] -like 'HEAD:*.uid') { [pscustomobject]@{StdOut=$fixtureFiles[$Arguments[1].Substring(5)]} }
        else { Assert-True ($Arguments[1] -ceq 'scope-base:ownership.json') 'Deletion used another baseline'; [pscustomobject]@{StdOut=$baseOwnership} }
    }
    $wp00=@(Select-UESTCChanges $fixture WP-00 @('scripts/lib/common.ps1','scripts/gates/cache.ps1','docs/neighbour.md','game/other/Missing.cs') scope-base)
    Assert-True ($wp00.Count -eq 2 -and 'docs/neighbour.md' -notin $wp00) 'WP00 claimed neighbour shared/docs changes.'
    $own=@(Select-UESTCChanges $fixture WP-18 @('game/own/Own.cs','game/other/Missing.cs','game/deleted/Old.cs') scope-base)
    Assert-True ($own.Count -eq 2 -and 'game/deleted/Old.cs' -in $own) 'Selection lost subpackage/deleted baseline ownership.'
    $null=Test-UESTCOwnership $fixture WP-18 $own -BaseRef scope-base
    $rejected=$false
    try { $null=Test-UESTCOwnership $fixture WP-18 @('game/other/Missing.cs') -BaseRef scope-base }
    catch { $rejected=$_.Exception.Message -like '*Ownership: game/other/Missing.cs*' }
    Assert-True $rejected 'An explicit foreign path was silently filtered from G3.'
    $tracked=@('game/own/Own.cs')
    $rejected=$false
    try { $null=Test-UESTCOwnership $fixture WP-18 @('game/own/Own.cs') -BaseRef scope-base }
    catch { $rejected=$_.Exception.Message -like '*UID not staged/tracked*' }
    Assert-True $rejected 'Own untracked UID was not rejected.'
    $tracked=@('game/own/Own.cs','game/own/Own.cs.uid')
    $uidBaseline=Get-UESTCOwnershipUidHash $fixture @('game/own/Own.cs')
    [IO.File]::WriteAllText((Join-Path $fixture 'game/other/First.cs.uid'),'uid://own')
    $dirtyUids=@('game/other/First.cs.uid')
    $null=Test-UESTCOwnership $fixture WP-18 @('game/own/Own.cs') -BaseRef scope-base
    Assert-True ($uidBaseline -eq (Get-UESTCOwnershipUidHash $fixture @('game/own/Own.cs'))) 'A dirty foreign UID invalidated this scope cache.'
    [IO.File]::Delete((Join-Path $fixture 'game/other/First.cs.uid'))
    $null=Test-UESTCOwnership $fixture WP-18 @('game/own/Own.cs') -BaseRef scope-base
    Assert-True ($uidBaseline -eq (Get-UESTCOwnershipUidHash $fixture @('game/own/Own.cs'))) 'A deleted foreign UID invalidated this scope cache.'
    [IO.File]::WriteAllText((Join-Path $fixture 'game/other/First.cs.uid'),'uid://own')
    $committedUids=@('game/own/Own.cs.uid','game/other/Second.cs.uid')
    $tracked+=@('game/other/First.cs.uid')
    $null=Test-UESTCOwnership $fixture WP-18 @('game/own/Own.cs') -BaseRef scope-base
    $committedUids+=@('game/other/First.cs.uid')
    $dirtyUids=@()
    $rejected=$false
    try { $null=Test-UESTCOwnership $fixture WP-18 @('game/own/Own.cs') -BaseRef scope-base }
    catch { $rejected=$_.Exception.Message -like '*Duplicate UID:*' }
    Assert-True $rejected 'Own UID collision with a neighbour was missed.'
    Write-Output 'Passed: default owner/maintainer scope, WP00 neighbour docs exclusion, explicit foreign G3 failure, same-baseline deletion, scoped UID requirements, neighbour work-in-progress isolation and committed cross-scope collision.'
}
Write-Output 'Passed: package cache hit/invalidation, shared snapshots/palette/global assets/tools/options, G2 affected projects and G5 declaration selection.'
try {
    $layout = Get-UESTCLayout $root
    Assert-True ($layout.Root -eq [IO.Path]::GetFullPath($root)) 'Wrong repository root.'
    Assert-True ($layout.Locks -eq (Join-Path $layout.Primary 'output/.locks')) 'Wrong lock root.'
    Assert-True (@($layout.PSObject.Properties).Count -eq 3) 'Unexpected layout properties.'
    try { $null = Assert-UESTCChildPath (Join-Path $output '../escape') $output; throw 'Traversal accepted.' }
    catch { if ($_.Exception.Message -notlike 'Path escapes*') { throw } }
    Assert-True (Test-UESTCGlob 'game/contracts/A.cs' 'game/contracts/**') 'Directory glob failed.'
    Assert-True (-not (Test-UESTCGlob 'game/contracts/A.cs' 'game/*.cs')) 'Single star crossed a directory.'
    $lease = Enter-UESTCBuildLock $locks; $leases.Add($lease)
    $blocked = $false
    try { $unexpected = Enter-UESTCBuildLock $locks -TimeoutSeconds 1; Exit-UESTCBuildLock $unexpected }
    catch { if ($_.Exception.Message -notlike 'Timed out*') { throw }; $blocked = $true }
    Assert-True $blocked 'A second build lock was incorrectly granted.'
    Exit-UESTCBuildLock $lease; $leases.Clear()
    $reopened = Enter-UESTCBuildLock $locks
    Exit-UESTCBuildLock $reopened
    $version = Invoke-UESTCProcess dotnet @('--version') $root (Join-Path $output 'dotnet-version.log')
    Assert-True ($version.ExitCode -eq 0) 'Process wrapper failed.'
    $failure = Invoke-UESTCProcess (Join-Path $PSHOME 'powershell.exe') @('-NoProfile','-NonInteractive','-Command','exit 23') $root (Join-Path $output 'expected-failure.log') -AllowFailure
    Assert-True ($failure.ExitCode -eq 23) 'Native process failure was not preserved.'
    $echoScript = Join-Path $output 'echo-args.ps1'
    [IO.File]::WriteAllText($echoScript, '@($args) | ConvertTo-Json -Compress', (New-Object Text.UTF8Encoding $false))
    $arguments = @('a b', 'a"b', 'C:\space path\', 'literal$(value)', '')
    $echo = Invoke-UESTCProcess (Join-Path $PSHOME 'powershell.exe') (@('-NoProfile','-NonInteractive','-File',$echoScript) + $arguments) $root (Join-Path $output 'quote-check.log')
    $actual = ConvertFrom-Json -InputObject $echo.Output
    Assert-True (@($actual).Count -eq $arguments.Count) 'Native quoting dropped an argument.'
    for ($i = 0; $i -lt $arguments.Count; $i++) { Assert-True ($actual[$i] -ceq $arguments[$i]) "Native quoting changed argument $i." }
    Write-Output 'Passed: repository layout, containment, glob, single build lock, release/reacquisition, native failures and argument quoting.'
} finally { foreach ($lease in $leases) { Exit-UESTCBuildLock $lease } }
