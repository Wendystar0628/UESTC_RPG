# 在调用方的私桌面启动并持有一次诊断工具进程树，保留实际命令与输出。
# 用法：powershell -NoProfile -File scripts/perf/diagnostic-capture.ps1 -ConditionPath <condition.json>。
param([Parameter(Mandatory)][string]$ConditionPath)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $root 'scripts\lib\common.ps1')
$condition=Get-Content -LiteralPath $ConditionPath -Raw | ConvertFrom-Json
$work=Split-Path $ConditionPath -Parent
$principal=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Diagnostic collectors require an ordinary, non-elevated token.' }
$executable=$condition.engine
$arguments=@($condition.arguments)
switch ($condition.tool) {
    'EventPipe' {
        $executable=$condition.toolPath
        $arguments=@('collect','--profile','dotnet-sampled-thread-time','--providers','Microsoft-Windows-DotNETRuntime:0x41080001:5',
            '--output',(Join-Path $work 'threads.nettrace'),'--show-child-io','--',$condition.engine) + $arguments
    }
    'Nsight' {
        $executable=$condition.toolPath
        # Windows CPU sampling/context-switch collection needs elevation. API tracing does not.
        $arguments=@('profile','--trace=dx12,vulkan,nvtx','--sample=none','--cpuctxsw=none','--wait=all','--kill=false',
            ('--output=' + (Join-Path $work 'graphics')),$condition.engine) + $arguments
    }
}
$record=[ordered]@{ executable=$executable; arguments=$arguments; tool=$condition.tool; elevated=$false;
    startedUtc=[DateTime]::UtcNow.ToString('O'); exitCode=$null; status='partial'; error=$null }
try {
    $version=Invoke-UESTCProcess $executable @('--version') $root (Join-Path $work 'tool-version.log') -TimeoutSeconds 30
    $record['versionOutput']=$version.Output.Trim()
    if ($condition.tool -eq 'EventPipe') {
        $profiles=Invoke-UESTCProcess $executable @('list-profiles') $root (Join-Path $work 'tool-profiles.log') -TimeoutSeconds 30
        $record['profileDescription']=$profiles.Output.Trim()
    }
    $result=Invoke-UESTCProcess $executable $arguments $root (Join-Path $work 'tool.log') -TimeoutSeconds 600 -AllowFailure -TextEncoding ([Text.Encoding]::UTF8)
    $record.exitCode=$result.ExitCode
    $record.status=if($result.ExitCode -eq 0){'completed'}else{'failed'}
    [IO.File]::WriteAllText((Join-Path $work 'tool.stdout.txt'),$result.StdOut)
    [IO.File]::WriteAllText((Join-Path $work 'tool.stderr.txt'),$result.StdErr)
    Assert-UESTCCleanGodotLog $result
    if ($record.status -eq 'completed' -and $condition.tool -eq 'EventPipe') {
        $sessions=@(Get-ChildItem -LiteralPath $work -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') })
        if ($sessions.Count -ne 1) { throw 'EventPipe target did not publish exactly one closed performance session.' }
        $parsed=Invoke-UESTCProcess dotnet @($condition.parserDll,'--perf-session',$work,$sessions[0].FullName) $root `
            (Join-Path $work 'cpu-parser.log') -TimeoutSeconds 120
        $record['cpuParserExitCode']=$parsed.ExitCode
    }
} catch { $record.status='failed'; $record.error=$_.Exception.ToString() }
finally {
    $record['endedUtc']=[DateTime]::UtcNow.ToString('O')
    [IO.File]::WriteAllText((Join-Path $work 'capture-result.json'),($record | ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding $false))
}
if($record.status -ne 'completed'){exit 1}
