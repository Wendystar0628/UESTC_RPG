# 用真实窗口的 Godot（d3d12，640x360）跑标了 NativeRender 分类的原生渲染测试；一个测试都没执行算未完成。
# 产出：output/build/native-render/（运行设置、构建与测试日志、TRX 结果）。
# 用法：powershell scripts\gates\native-render.ps1 -Root <仓库根> [-BuildLease <已持有的构建锁>]；平时由 scripts\gate.ps1 的 G2 调用
param([Parameter(Mandatory)][string]$Root, [IO.FileStream]$BuildLease = $null)
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
. (Join-Path $rootPath 'scripts/lib/common.ps1')
. (Join-Path $rootPath 'scripts/lib/start-game.ps1')
$layout = Get-UESTCLayout $rootPath
$work = Join-Path $rootPath 'output/build/native-render'
$null = [IO.Directory]::CreateDirectory($work)
$testEnvironment = New-UESTCTestEnvironment -Work $work
$engine = Get-UESTCGodot
$settings = Join-Path $work 'window.runsettings'
$null = Write-UESTCTestSettings -Path $settings -Root $rootPath -Godot $engine -Environment $testEnvironment `
    -Parameters '--rendering-method forward_plus --rendering-driver d3d12 --resolution 640x360'
$lease = $null
try {
    if (-not $BuildLease) {
        $lease = Enter-UESTCBuildLock $layout.Locks
        Update-UESTCGameBuild -Root $rootPath -Godot $engine -BuildLease $lease -Environment $testEnvironment
    }
    # The project build incrementally prepares its native gdUnit runner bridge.
    $null = Invoke-UESTCProcess dotnet @('build',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','-warnaserror') `
        $rootPath (Join-Path $work 'native-render-build.log') -Environment $testEnvironment

    $trxName = 'native-render-' + [Guid]::NewGuid().ToString('N') + '.trx'
    $trxPath = Join-Path $work $trxName
    $result = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'-c','Debug','--no-build',
        '--settings',$settings,'--filter','TestCategory=NativeRender','--logger',"trx;LogFileName=$trxName",
        '--results-directory',$work) $rootPath (Join-Path $work 'native-render.log') -TimeoutSeconds 600 -AllowFailure -IsolatedDesktop -Environment $testEnvironment
    $executed = Assert-UESTCTestResult $result $trxPath
    [pscustomobject]@{ Status='Passed'; Tests=$executed; Log=$result.Log }
} finally { Exit-UESTCBuildLock $lease }
