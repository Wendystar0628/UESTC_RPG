using Godot;

namespace Campus.Tests;

/// <summary>
/// 用例释放节点以后，C# 新建或持有包装的 RefCounted（主题、字体变体、字体、样式盒、材质、纹理）要等包装终结才归还原生引用，
/// 而且逐层归还：主题删掉后字体变体才只剩包装持有，再下一轮才轮到字体和它的字形缓存纹理。GodotSharp 退出时只释放仍可达的包装，
/// 已被回收、还没终结的那批被跳过，就漏到引擎关闭（RID 与 ObjectDB 泄漏）。建这类资源的测试组在 [After] 里调用：
/// 那时各用例都已结束，局部变量不再可达。
/// </summary>
internal static class DroppedNatives
{
    /// <summary>回收并终结到某一轮不再有对象归还为止；按无进展收尾，不设轮数。</summary>
    public static void Release()
    {
        double before;
        do
        {
            before = Performance.GetMonitor(Performance.Monitor.ObjectCount);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        while (Performance.GetMonitor(Performance.Monitor.ObjectCount) < before);
    }
}
