# 隐藏等待本次游戏进程退出，然后对已关闭的性能会话运行一次分析。
# 用法：由 start-game.ps1 启动 powershell -File scripts/perf/wait-analyze.ps1 -Root <仓库> -GameProcessId <进程> -StartedUtcTicks <启动时刻ticks> -Python <解释器>。
# 输入：游戏进程身份与性能六件；产出：output/perf/ 派生、索引与历史；错误写启动入口指定日志，分析后进程自行结束。
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][int]$GameProcessId,
    [Parameter(Mandatory)][long]$StartedUtcTicks,
    [Parameter(Mandatory)][string]$Python
)
$ErrorActionPreference = 'Stop'
try {
    $game = $null
    try { $game = [Diagnostics.Process]::GetProcessById($GameProcessId) }
    catch [ArgumentException] { } # 游戏已先退出，仍需分析其最终清单。
    if ($null -ne $game) {
        try {
            # PID 被系统复用时不等待另一个进程。
            if ($game.StartTime.ToUniversalTime().Ticks -eq $StartedUtcTicks) { $game.WaitForExit() }
        } catch [InvalidOperationException] {
            # 获取句柄期间已退出，与正常等到退出相同。
        } finally { $game.Dispose() }
    }
    & $Python -I -X utf8 (Join-Path $Root 'scripts\perf\analyze.py') --root $Root
    exit $LASTEXITCODE
} catch {
    [Console]::Error.WriteLine($_.Exception.ToString())
    exit 1
}
