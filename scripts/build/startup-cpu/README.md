# TOOL-215 启动 CPU 采样脚本

两个入口都把样本、日志、JSON 与 `.nettrace` 写入 `output/start_game/TOOL-215-profile`。`run.ps1` 在固定机位截图和预烘完成后使用：要求全新输出目录，取得共享构建锁并做现有启动判鲜，只构建独立 `CpuParse`，再串行采集 `title`、`immediate`、`dwell`。`-TitleDwellSeconds` 默认 10 秒、范围 1–120 秒。解析器的 `bin/obj` 留在 output。

`run-existing-immediate.ps1` 只用现有游戏与解析器二进制；缺少任一二进制就报错。它跳过启动初始化、编译、导入和预烘，结果记录 `compileImportPrebakeInvoked=false`。游戏、默认缓存、工具、解析器、输出及共享构建锁始终来自脚本所在仓库的当前主检出。用法：`powershell -File scripts/build/startup-cpu/run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交>`。子进程早期错误写入样本目录的 `worker.stderr.txt`。

三轮使用正常产品默认值和隔离 save/log，不带 gate、shots、frames 或 CaptureMode。`title` 到 realtime 结束；`immediate` 在观察到 title-ready 后等真实后续 process frame 发定向 Enter，再跨下一 process frame 释放；`dwell` 先停留指定秒数再执行同样输入。immediate 的 Enter 若 15 秒内未到 new-game-enter，该轮失败。dwell 时长不保证后台世界已就绪；title-ready、world-ready、realtime 与首次 Walk 是不同边界。日志按名义 20 ms 轮询，边界观测有延迟，不插值。

启动器使用私有 Windows desktop，不切换桌面、不用全局输入；只向目标 PID 窗口发送 Enter，并在私有控制台向自己的 `dotnet-trace` 写 Enter 以正常 Stop/rundown。采集以普通用户令牌运行，不用管理员令牌或提权。EventPipe 的 `dotnet-sampled-thread-time` 包含等待中的托管线程，**不是 on-CPU 归因**；函数 CPU 占比保持 `NOT_MEASURED`。进程/OS 线程 CPU 计数器另行记录，采集在进程创建后 attach，启动初段覆盖不全。启用逐帧日志会带来诊断开销；renderer CPU/GPU 是渲染测量，不等于 OS 线程 CPU。
