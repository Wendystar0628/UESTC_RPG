param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
. (Join-Path $rootPath 'scripts/lib/common.ps1')
$layout = Get-UESTCLayout $rootPath
$work = Join-Path $rootPath 'output/build/native-render'
$null = [IO.Directory]::CreateDirectory($work)
$godot = [Security.SecurityElement]::Escape((Get-UESTCGodot))
$escapedRoot = [Security.SecurityElement]::Escape($rootPath)
$settings = Join-Path $work 'window.runsettings'
$xml = @"
<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount><TreatNoTestsAsError>true</TreatNoTestsAsError><EnvironmentVariables><GODOT_BIN>$godot</GODOT_BIN><UESTC_REPOSITORY>$escapedRoot</UESTC_REPOSITORY></EnvironmentVariables></RunConfiguration><GdUnit4><Parameters>--rendering-method forward_plus --rendering-driver d3d12 --resolution 640x360</Parameters><DisplayName>FullyQualifiedName</DisplayName><TolerateExitCodeAfterCompletion>false</TolerateExitCodeAfterCompletion></GdUnit4></RunSettings>
"@
[IO.File]::WriteAllText($settings, $xml, (New-Object Text.UTF8Encoding $false))
$lease = Enter-UESTCBuildLock $layout.Locks
try {
    & (Join-Path $rootPath 'scripts/setup/install-test-runner.ps1') -Root $rootPath
    $null = Invoke-UESTCProcess dotnet @('build',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','-warnaserror') `
        $rootPath (Join-Path $work 'native-render-build.log')

    $result = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','--no-build',
        '--settings',$settings,'--filter','TestCategory=NativeRender','--logger','trx',
        '--results-directory',$work) $rootPath (Join-Path $work 'native-render.log') -TimeoutSeconds 120
    if ($result.Output -match '(?m)(?:^|Godot /err: )ERROR:') { throw "Native renderer reported ERROR. See $($result.Log)" }
    Write-Output "NativeRender passed: $($result.Log)"
} finally { Exit-UESTCBuildLock $lease }
