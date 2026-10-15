param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
. (Join-Path $rootPath 'scripts/wt/common.ps1')
$work = Join-Path $rootPath 'output/build/native-render'
$null = [IO.Directory]::CreateDirectory($work)
$godot = [Security.SecurityElement]::Escape((Get-UESTCGodot))
$escapedRoot = [Security.SecurityElement]::Escape($rootPath)
$settings = Join-Path $work 'window.runsettings'
$xml = @"
<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount><TreatNoTestsAsError>true</TreatNoTestsAsError><EnvironmentVariables><GODOT_BIN>$godot</GODOT_BIN><UESTC_REPOSITORY>$escapedRoot</UESTC_REPOSITORY></EnvironmentVariables></RunConfiguration><GdUnit4><Parameters>--rendering-method forward_plus --rendering-driver d3d12 --resolution 640x360</Parameters><DisplayName>FullyQualifiedName</DisplayName><TolerateExitCodeAfterCompletion>false</TolerateExitCodeAfterCompletion></GdUnit4></RunSettings>
"@
[IO.File]::WriteAllText($settings, $xml, (New-Object Text.UTF8Encoding $false))
$locks = Join-Path (Split-Path $rootPath -Parent) '.locks'
$lease = Enter-UESTCResourceLock $locks CpuGpuShared
try {
    & (Join-Path $rootPath 'scripts/setup/install-test-runner.ps1') -Root $rootPath
    $result = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),
        '--settings',$settings,'--filter','TestCategory=NativeRender','--logger','trx',
        '--results-directory',$work) $rootPath (Join-Path $work 'native-render.log') -TimeoutSeconds 180
    if ($result.Output -match '(?m)(?:^|Godot /err: )ERROR:') { throw "Native renderer reported ERROR. See $($result.Log)" }
    Write-Output "NativeRender passed: $($result.Log)"
} finally { Exit-UESTCResourceLock $lease }
