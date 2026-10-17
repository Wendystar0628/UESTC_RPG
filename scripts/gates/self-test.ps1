# Script-only smoke tests: no build, import, worktree creation or global locks.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../wt/common.ps1')
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
    function Get-UESTCGitLines { $trackedUids }
    $baseline = Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @()
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 main Debug 'missing-engine' @('docs/notes.md'))) 'Equivalent inputs missed cache after baseline/docs changes.'
    foreach ($path in @('game/gen/other/Other.cs','docs/notes.md','output/shot.png')) {
        [IO.File]::AppendAllText((Join-Path $fixture $path), 'changed')
    }
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @())) 'Unrelated content/docs/screenshot invalidated cache.'
    foreach ($path in @('game/gen/own/Own.cs','game/gen/shared/Shared.cs','game/data/maps/index.json','game/data/vegetation/species.json','game/style/palette/base.json','scripts/gate.ps1','scripts/build/build-info.ps1','scripts/setup/install-test-runner.ps1','game/packages.lock.json')) {
        [IO.File]::AppendAllText((Join-Path $fixture $path), 'changed')
        Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @())) "Related input did not invalidate cache: $path"
        [IO.File]::WriteAllText((Join-Path $fixture $path), $fixtureFiles[$path])
    }
    Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD 'Debug|changed-option' 'missing-engine' @())) 'Option change did not invalidate cache.'
    [IO.File]::WriteAllText((Join-Path $fixture 'game/flow/Service.cs'), $fixtureFiles['game/flow/Service.cs'].Replace('IFirst','ISecond'))
    Assert-True ($baseline -ne (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @())) 'Partial constructor dependency did not invalidate registration cache.'
    & {
        function Invoke-UESTCGit { param($Root, $Arguments) [pscustomobject]@{ StdOut= $(if ($Arguments[0] -eq 'show') { $fixtureFiles['game/flow/Service.cs'] } else { 'fixture-base' }) } }
        function Get-UESTCGitLines { 'game/flow/Service.cs' }
        Assert-True (Test-UESTCG5Impact $fixture HEAD @('game/flow/Service.cs')) 'Partial constructor dependency did not select G5.'
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'game/flow/Service.cs'), $fixtureFiles['game/flow/Service.cs'])
    $uidBaseline = Get-UESTCOwnershipUidHash $fixture
    $uidPath = Join-Path $fixture 'game/gen/other/Other.cs.uid'
    [IO.File]::WriteAllText($uidPath, 'uid://changed')
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture)) 'Other-package UID content did not invalidate G3.'
    Assert-True ($baseline -eq (Get-UESTCInputHash $fixture WP-18 HEAD Debug 'missing-engine' @())) 'Other-package UID content invalidated unrelated gates.'
    [IO.File]::WriteAllText($uidPath, $fixtureFiles['game/gen/other/Other.cs.uid'])
    $trackedUids = @()
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture)) 'UID tracked-state change did not invalidate G3.'
    $trackedUids = @('game/gen/other/Other.cs.uid')
    [IO.File]::Delete($uidPath)
    Assert-True ($uidBaseline -ne (Get-UESTCOwnershipUidHash $fixture)) 'UID deletion did not invalidate G3.'
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
Write-Output 'Passed: package cache hit/invalidation, shared snapshots/palette/global assets/tools/options, G2 affected projects and G5 declaration selection.'
function Assert-LockBlocked {
    param([string]$Kind)
    $unexpected = $null
    try { $unexpected = Enter-UESTCResourceLock $locks $Kind -TimeoutSeconds 1 }
    catch { if ($_.Exception.Message -notlike 'Timed out*') { throw }; return }
    finally { if ($unexpected) { Exit-UESTCResourceLock $unexpected } }
    throw "$Kind lock was incorrectly granted."
}
try {
    $layout = Get-UESTCLayout $root
    Assert-True ($layout.Root -eq [IO.Path]::GetFullPath($root)) 'Wrong repository root.'
    Assert-True ($layout.Locks -eq (Join-Path $layout.Worktrees '.locks')) 'Wrong lock root.'
    try { $null = Assert-UESTCChildPath (Join-Path $output '../escape') $output; throw 'Traversal accepted.' }
    catch { if ($_.Exception.Message -notlike 'Path escapes*') { throw } }
    Assert-True (Test-UESTCGlob 'game/contracts/A.cs' 'game/contracts/**') 'Directory glob failed.'
    Assert-True (-not (Test-UESTCGlob 'game/contracts/A.cs' 'game/*.cs')) 'Single star crossed a directory.'
    $a = Enter-UESTCResourceLock $locks GpuShared; $leases.Add($a)
    $b = Enter-UESTCResourceLock $locks GpuShared; $leases.Add($b)
    Assert-LockBlocked GpuShared
    Assert-LockBlocked GpuExclusive
    Exit-UESTCResourceLock $a; Exit-UESTCResourceLock $b; $leases.Clear()
    $cpu = Enter-UESTCResourceLock $locks Cpu; $leases.Add($cpu)
    Assert-LockBlocked GpuExclusive
    Exit-UESTCResourceLock $cpu; $leases.Clear()
    $cpuLimit = [Math]::Max(2, [Math]::Floor([Environment]::ProcessorCount / 8.0))
    for ($i = 0; $i -lt $cpuLimit; $i++) { $leases.Add((Enter-UESTCResourceLock $locks Cpu)) }
    Assert-LockBlocked Cpu
    foreach ($lease in $leases) { Exit-UESTCResourceLock $lease }; $leases.Clear()
    $exclusive = Enter-UESTCResourceLock $locks GpuExclusive; $leases.Add($exclusive)
    Assert-LockBlocked Cpu
    Assert-LockBlocked GpuShared
    Exit-UESTCResourceLock $exclusive; $leases.Clear()
    # Existing empty lock files are harmless: only the held OS handle represents ownership.
    $reopened = Enter-UESTCResourceLock $locks GpuExclusive
    Exit-UESTCResourceLock $reopened
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
    Write-Output 'Passed: repository layout, containment, glob, CPU limit, GPU max=2, GPU exclusive vs CPU/shared, release/reacquisition, native failures and argument quoting.'
} finally { foreach ($lease in $leases) { Exit-UESTCResourceLock $lease } }
