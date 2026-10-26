// 编排性能会话的 EventPipe 诊断；索引与证据文档由报告入口持有至序列化完成。
// 用法：dotnet CpuParse.dll --perf-session <trace-directory> <session-directory>。
if (args.Length != 3 || args[0] != "--perf-session")
    throw new ArgumentException("Expected --perf-session <trace-directory> <session-directory>.");
PerfSessionCpuReport.Run(args[1], args[2]);
