# 用真实窗口的 Godot（d3d12，640x360）跑标了 NativeRender 分类的原生渲染测试；一个测试都没执行算未完成。
# 用法：powershell scripts\gates\native-render.ps1 -Root <仓库根> [-BuildLease <已持有的构建锁>]；平时由 scripts\gate.ps1 的 G2 调用
param([Parameter(Mandatory)][string]$Root, [IO.FileStream]$BuildLease = $null)
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
$lease = $null
try {
    if (-not $BuildLease) {
        $lease = Enter-UESTCBuildLock $layout.Locks
        & (Join-Path $rootPath 'scripts/setup/install-test-runner.ps1') -Root $rootPath
        $null = Invoke-UESTCProcess dotnet @('build',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','-warnaserror') `
            $rootPath (Join-Path $work 'native-render-build.log')
    }

    $trxName = 'native-render-' + [Guid]::NewGuid().ToString('N') + '.trx'
    $trxPath = Join-Path $work $trxName
    $result = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','--no-build',
        '--settings',$settings,'--filter','TestCategory=NativeRender','--logger',"trx;LogFileName=$trxName",
        '--results-directory',$work) $rootPath (Join-Path $work 'native-render.log') -TimeoutSeconds 600 -AllowFailure
    $executed = 0
    if (Test-Path -LiteralPath $trxPath -PathType Leaf) {
        [xml]$trx = Get-Content -Raw -LiteralPath $trxPath
        $counters = $trx.SelectSingleNode('//*[local-name()="ResultSummary"]/*[local-name()="Counters"]')
        if ($counters) { $executed = [int]$counters.GetAttribute('executed') }
    }
    if ($executed -eq 0) {
        $failure = New-Object InvalidOperationException('NativeRender is incomplete: zero executed tests or missing TRX.')
        $failure.Data['Incomplete'] = $true
        throw $failure
    }
    if ($result.ExitCode -ne 0) { throw "NativeRender exited $($result.ExitCode). See $($result.Log)" }
    if ($result.Output -match '(?m)(?:^|Godot /err: )ERROR:') { throw "Native renderer reported ERROR. See $($result.Log)" }
    [pscustomobject]@{ Status='Passed'; Tests=$executed; Log=$result.Log }
} finally { Exit-UESTCBuildLock $lease }
