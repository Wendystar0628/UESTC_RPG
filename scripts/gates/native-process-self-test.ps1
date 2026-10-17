# Windows PowerShell 5.1 redirection regression. Only this tree's output/ is mutated.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../wt/common.ps1')
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $root ('output/build/gates/native-process-self-test/' + [Guid]::NewGuid().ToString('N'))
$fixture = Join-Path $output 'fixture repo'
$null = [IO.Directory]::CreateDirectory($fixture)
$utf8 = New-Object Text.UTF8Encoding $false
$common = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../wt/common.ps1'))
$null = Invoke-UESTCGit $fixture @('-c', 'init.defaultBranch=main', 'init', '--quiet')
$null = Invoke-UESTCGit $fixture @('config', 'core.autocrlf', 'false')
$null = Invoke-UESTCGit $fixture @('config', 'core.safecrlf', 'warn')
[IO.File]::WriteAllText((Join-Path $fixture '.gitattributes'), "*.txt text eol=lf`n", $utf8)
$payload = Join-Path $output 'payload.ps1'
$payloadSource = @'
param([string]$Common,[string]$Fixture,[string]$Label,[string]$Result,[string]$Root)
$ErrorActionPreference='Stop'
. $Common
$candidate=Join-Path $Fixture ($Label+'.txt')
[IO.File]::WriteAllText($candidate,"alpha`r`nbeta`r`n",(New-Object Text.UTF8Encoding $false))
$warning=Invoke-UESTCGit $Fixture @('add','--',$candidate)
if($warning.ExitCode -ne 0 -or $warning.StdErr -notmatch 'CRLF|LF will be replaced') { throw 'Expected an actual successful Git CRLF warning.' }
if($warning.StdErr -isnot [string]) { throw 'Native stderr was converted to an ErrorRecord.' }
$fatalPreserved=$false
try { $null=Invoke-UESTCGit $Fixture @('rev-parse','--verify','missing-native-test-reference') }
catch { $fatalPreserved=$_.Exception.Data['ExitCode'] -eq 128 -and $_.Exception.Data['StdErr'] -match 'fatal:' }
if(-not $fatalPreserved) { throw 'A genuine Git fatal error was lost.' }
$missing=Invoke-UESTCGit $Fixture @('show-ref','--verify','--quiet','refs/heads/missing-native-test-reference') -SuccessExitCodes @(0,1)
if($missing.ExitCode -ne 1) { throw 'Expected missing-reference exit 1.' }
for($i=0;$i -lt 6;$i++) { $layout=Get-UESTCLayout $Root; if($layout.Root -ne $Root) { throw 'Layout output was truncated.' } }
$gate=Join-Path $Root 'scripts/gate.ps1'
$plan=& $gate -Wp WP-00 -Plan | ConvertFrom-Json
if($plan.WorkPackage -ne 'WP-00') { throw 'Gate plan failed.' }
$worktree=& (Join-Path $Root 'scripts/wt/new.ps1') -Wp WP-99 -Slug native-probe -Plan
if(-not $worktree.Target.EndsWith('WP-99')) { throw 'Worktree plan failed.' }
& (Join-Path $Root 'scripts/build/build-info.ps1') -Root $Root -Output ($Result+'.BuildInfo.cs')
if(-not (Test-Path -LiteralPath ($Result+'.BuildInfo.cs'))) { throw 'Build metadata generation failed.' }
$record=[ordered]@{mode=$Label;gitWarning=$warning.StdErr;warningExit=$warning.ExitCode;fatalExit128Preserved=$fatalPreserved;layoutRepeats=6;gatePlan=$true;worktreePlan=$true;buildInfo=$true}
[IO.File]::WriteAllText($Result,($record|ConvertTo-Json -Depth 5),(New-Object Text.UTF8Encoding $false))
Write-Output "PASS $Label"
'@
[IO.File]::WriteAllText($payload, $payloadSource, $utf8)
$wrapper = Join-Path $output 'redirect.ps1'
$wrapperSource = @'
param([string]$Mode,[string]$Payload,[string]$Common,[string]$Fixture,[string]$Result,[string]$Root,[string]$Capture)
$ErrorActionPreference='Stop'
if($Mode -eq 'merged-errors') { & $Payload -Common $Common -Fixture $Fixture -Label $Mode -Result $Result -Root $Root 2>&1 | Out-File -LiteralPath $Capture -Encoding utf8 }
elseif($Mode -eq 'all-streams') {
    # Intentionally exercise the native PowerShell boundary under the caller's Stop policy.
    & (Join-Path $PSHOME 'powershell.exe') -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $Payload -Common $Common -Fixture $Fixture -Label $Mode -Result $Result -Root $Root *> $Capture
    if($LASTEXITCODE -ne 0) { throw "Redirected powershell.exe exited $LASTEXITCODE" }
}
else { throw "Unknown test mode: $Mode" }
if(-not(Test-Path -LiteralPath $Result)) { throw 'Redirected payload did not complete.' }
'@
[IO.File]::WriteAllText($wrapper, $wrapperSource, $utf8)
$powershell = Join-Path $PSHOME 'powershell.exe'
$reports = New-Object 'Collections.Generic.List[object]'
foreach ($mode in @('merged-errors','all-streams')) {
    $resultFile = Join-Path $output ($mode + '.json')
    $arguments = @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$wrapper,
        '-Mode',$mode,'-Payload',$payload,'-Common',$common,'-Fixture',$fixture,'-Result',$resultFile,'-Root',$root,'-Capture',(Join-Path $output ($mode+'.capture.log')))
    $null = Invoke-UESTCProcess $powershell $arguments $root (Join-Path $output ($mode+'.host.log'))
    $reports.Add((Get-Content -Raw -Encoding UTF8 -LiteralPath $resultFile | ConvertFrom-Json))
}
# Exercise the project's Git Bash boundary, not the unrelated system WSL shim.
$gitPath = @(Get-Command git.exe -CommandType Application -ErrorAction Stop)[0].Source
$gitRoot = Split-Path (Split-Path $gitPath -Parent) -Parent
$bash = Join-Path $gitRoot 'bin/bash.exe'
$bashResult = Join-Path $output 'bash-entry.json'
$bashCommand = 'powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$1" -Common "$2" -Fixture "$3" -Label bash-entry -Result "$4" -Root "$5"'
$null = Invoke-UESTCProcess $bash @('--noprofile','--norc','-c',$bashCommand,'--',$payload,$common,$fixture,$bashResult,$root) $root (Join-Path $output 'bash-entry.host.log')
$reports.Add((Get-Content -Raw -Encoding UTF8 -LiteralPath $bashResult | ConvertFrom-Json))

$copySource = Join-Path $output 'copy source'
$copyTarget = Join-Path $output 'copy target'
$null = [IO.Directory]::CreateDirectory($copySource)
[IO.File]::WriteAllText((Join-Path $copySource 'seed.txt'), 'seed', $utf8)
$copy = Invoke-UESTCProcess robocopy @($copySource,$copyTarget,'/E','/R:0','/W:0','/NFL','/NDL','/NJH','/NJS') $root (Join-Path $output 'robocopy.log') -SuccessExitCodes (0..7)
if ($copy.ExitCode -lt 1 -or -not (Test-Path -LiteralPath (Join-Path $copyTarget 'seed.txt'))) { throw 'Robocopy success-with-copy was not accepted.' }
$summary = [ordered]@{ Results=$reports.ToArray(); RobocopyExit=$copy.ExitCode; Scope='Script validation only; no build/import/worktree creation/commit' }
[IO.File]::WriteAllText((Join-Path $output 'summary.json'), ($summary | ConvertTo-Json -Depth 8), $utf8)
Write-Output "Passed all three Windows PowerShell entry modes, actual CRLF stderr warning, fatal exit 128, repeated layout, read-only plans, build metadata and robocopy exit $($copy.ExitCode)."
Write-Output "Evidence: $output"
