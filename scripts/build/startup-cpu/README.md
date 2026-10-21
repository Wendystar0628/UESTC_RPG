# 启动体验与 CPU 诊断

沿用 `title`、`immediate`、`dwell` 三个入口与成熟私有 Windows desktop 启动器，不切作者桌面、不用全局输入。输出仍在 `output/start_game/TOOL-215-profile/<新目录>`。先由 Root 完成统一构建/导入/预烘；采集在每轮脚本进入后取得共享构建锁并复用正式启动判鲜，旧构建或旧导入直接报错，不在样本里暗中更新。

普通体验：`powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录>`。默认三轮；单轮可用 `-Modes immediate`。默认 `experience` 显式设 `UESTC_STARTUP_PROFILE=0`，不开 EventPipe、不枚举 CPU 线程、不构建解析器。仅在确有归因需求时加 `-Measurement diagnostic`，它才构建现有 CpuParse、启用逐帧日志和 EventPipe，结果明确标为诊断，不能证明体验目标通过。`run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交>` 复用相同体验路径，不再跳过判鲜与等锁。

每轮有 `launch.json`、`launcher-timing.json`、`result.json` 与隔离 save/log。计时包含脚本进入、私桌面工作脚本的原生绑定初始化、等锁、正式判鲜、起引擎与稀疏阶段日志观测；原生绑定是额外工具开销，保守计入、不猜测扣除。script-entry Stopwatch、进程 StartTime UTC、引擎 `STARTUP_PHASE` 毫秒保留各自原点。产品双击入口也保存 `output/start_game/launch.json`，脚本进入时间在加载公共库前记录；使用外部已持有锁时明确外部等锁未测，不能填零。

`title` 到首个 `realtime`；`immediate` 在稀疏 `title-input-ready` 后向目标 PID 发送 Enter，`dwell` 先从观测的 `title-ready` 起停留 `-TitleDwellSeconds`（默认 10，范围 1–120 s）。Enter 按请求时刻起的正常短按时长在既有轮询中释放，再以 `new-game-enter` 验证受理；受理超 15 s 失败，不重试。短按目标时长见 `result.json.input.shortPressTargetMilliseconds`，实际请求到释放的观测时长见 `observedRequestToReleaseMilliseconds`；该墙钟时长不证明经过实际游戏帧。稀疏 `title-input-ready` 在 title-ready 后下一正常 process frame，输入路径无需逐帧日志。采集固定 `--title=1`；`--title=0` 的其他启动记录不算菜单测量。

`result.json` 保留请求与释放时刻，短按时间完整计入请求到 Walk，不扣除；使用“定向请求→首次 Walk 输入释放”而非“受理→释放”消费预算；`movementResponseSeconds=null`，实际方向输入与位移由既有核心专项顺带检查。停留固定秒数不代表出生点预载；3 s 只在 `new-game-enter preloadReady=true` 时比较。端点是日志观测，含名义 20 ms 轮询、刷新与调度延迟；不插值、不宣称首步时间已测。诊断 CPU 解析仍分别报告进程/主 OS 线程计数与 sampled-thread-time（含等待，非函数 CPU）；启动 attach 前缺口继续如实记录。

缓存参数分别声明 `-GeometryCache valid|missing|unknown`、`-PipelineCache warm|first-run|unknown`、`-SystemFileCache warm|reboot-first-run|unknown`，附 `-CacheEvidence <依据>`、`-JustPrebaked yes|no|unknown` 与 `-Device <设备>`。默认全部 unknown；它们是操作者声明，不是脚本已控制或自动验证。脚本不清任何缓存；重启后首次运行仍需注明 OS 页缓存未精确控制，预烘后不能宣称 OS 冷。一个批次只有首轮可沿用 reboot/首次管线条件，后续轮自动记 unknown，不能重复标冷/首次。

`budgetObservations` 只消费 `budgets.json.cache`：菜单热 5 s/冷 10 s（有效几何、热管线且相应 OS 条件已声明）、首次管线到实时 30 s（有效几何）、预载到 Walk 3 s/立即到 Walk 10 s（有效几何和热管线）。未知或条件不适用为 `NOT_MEASURED`；诊断为 `DIAGNOSTIC_ONLY`；体验只比较观测代理端点，不把它当作首步延迟实测。超目标保留数值交 Root 按工程预算规则判断，不额外创建门禁。
