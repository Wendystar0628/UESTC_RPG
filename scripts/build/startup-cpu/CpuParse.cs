// 编排 EventPipe/OS CPU 诊断并流式写入报告；持有证据文档与索引直到序列化完成。
// 用法：dotnet CpuParse.dll <diagnostic-round-directory> [report-file-name]；性能会话用 --perf-session <trace-directory> <session-directory>。
using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;

if (args.Length == 3 && args[0] == "--perf-session")
{
    PerfSessionCpuReport.Run(args[1], args[2]);
    return;
}
if (args.Length is < 1 or > 2) throw new ArgumentException("Expected diagnostic directory and optional report file name.");
var directory = Path.GetFullPath(args[0]);
var reportName = args.Length == 2 ? args[1] : "cpu-hotspots.json";
if (string.IsNullOrWhiteSpace(reportName) || Path.GetFileName(reportName) != reportName ||
    !reportName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Report must be a JSON file name within the diagnostic directory.");
using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "result.json")));
var phases = new CpuPhaseReport(result.RootElement);
var traceIndexPath = Path.Combine(directory, "threads.etlx");
try
{
    // TraceEvent associates captured managed stacks and CLR rundown names; no custom stack unwinder.
    Console.WriteLine("CpuParse: converting EventPipe to TraceLog index");
    TraceLog.CreateFromEventPipeDataFile(Path.Combine(directory, "threads.nettrace"), traceIndexPath);
    // Both owners remain alive while lazy allocation stacks and report sequences are serialized.
    using var trace = new TraceLog(traceIndexPath);
    var analysis = new CpuTraceAnalysis(trace, phases.Pid, phases.Windows);
    analysis.ReadEvents();
    var report = phases.CreateReport(analysis);
    Console.WriteLine("CpuParse: writing JSON report (allocation stack IDs reference gcAllocationSampling.allocationCallStacks)");
    using (var output = File.Create(Path.Combine(directory, reportName)))
        JsonSerializer.Serialize(output, report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine("CpuParse: report complete");
}
finally { if (File.Exists(traceIndexPath)) File.Delete(traceIndexPath); }
