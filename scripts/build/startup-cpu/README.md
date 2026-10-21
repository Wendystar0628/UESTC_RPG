# 启动体验与 CPU 诊断

沿用 `title`、`immediate`、`dwell` 三个入口与成熟私有 Windows desktop 启动器，不切作者桌面、不用全局输入。输出在 `output/start_game/TOOL-215-profile/<新目录>`。先由 Root 完成统一构建、导入与预烘；每轮取得共享构建锁并复用正式启动判鲜，旧构建或旧导入直接报错。

普通体验：`powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录>`。默认三轮；单轮可用 `-Modes immediate`。默认 `experience` 显式设 `UESTC_STARTUP_PROFILE=0`，不开 EventPipe、不枚举 CPU 线程、不构建解析器。有归因需求时加 `-Measurement diagnostic`，才构建现有 CpuParse、启用逐帧日志和 EventPipe；诊断不能证明体验目标通过。`run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交>` 复用相同体验路径。

每轮有 `launch.json`、`launcher-timing.json`、`result.json` 与隔离 save/log。计时包含脚本进入、原生绑定初始化、等锁、正式判鲜、起引擎与稀疏阶段日志观测；原生绑定是额外工具开销，保守计入。script-entry Stopwatch、进程 StartTime UTC、引擎 `STARTUP_PHASE` 毫秒保留各自原点。产品双击入口也保存 `output/start_game/launch.json`；使用外部已持有锁时明确外部等锁未测。

`title` 体验在稀疏 `title-input-ready` 结束，端点是静态菜单可输入，不等后台资源；Title 没有实时世界。`title` 诊断继续捕获，直到静态菜单可输入、出生点资源 `preload-ready` 两者均观测到；`captureEndpoint` 明确标为 `static-menu-input-ready-and-background-new-game-preload-ready`，这是包含后台准备的另一端点。两类标题轮均不等待或制造 `realtime`。

`immediate` 观测到 `title-input-ready` 就向目标 PID 发送 Enter。`dwell` 等 `title-input-ready` 和稀疏 `preload-ready preloadReady=True` 后发送，没有固定停留秒数。两种进入轮均以 `new-game-enter` 的真实 `preloadReady` 字段记录受理状态；停留轮若受理时不是 true，直接失败。`observedMenuInputReadyToPreloadReadyWaitSeconds` 记录菜单可输入后等待资源的观测秒数，资源先完成则为 0；`observedMenuInputReadyToRequestSeconds` 记录实际请求前停留。

Enter 延用 PID 定向、私桌面 100 ms 短按：按请求时刻起在既有轮询中释放，再核对 `new-game-enter` 受理，受理超 15 s 失败且不重试。目标时长见 `input.shortPressTargetMilliseconds`，观测时长见 `observedRequestToReleaseMilliseconds`；墙钟时长不证明经过某个游戏帧。进入轮到正式 Playing 后的首个 `realtime` 和首次 Walk `input-ready` 两者均观测到才结束。采集固定 `--title=1`，不把 `--title=0` 记录算菜单测量。

`result.json` 保留请求、释放和资源预载锚点。短按完整计入“定向请求→首次 Walk 输入释放”，不扣除；`movementResponseSeconds=null`，实际方向输入与位移由既有核心专项检查。3 s 只在 `new-game-enter preloadReady=True` 时比较。端点含名义 20 ms 轮询、日志刷新与调度延迟，不插值、不宣称首步时间已测。诊断解析分别报告进程、主 OS 线程 CPU 计数与 sampled-thread-time（含等待，非函数 CPU），如实保留 attach 前缺口。

缓存参数分别声明 `-GeometryCache valid|missing|unknown`、`-PipelineCache warm|first-run|unknown`、`-SystemFileCache warm|reboot-first-run|unknown`，附 `-CacheEvidence <依据>`、`-JustPrebaked yes|no|unknown` 与 `-Device <设备>`。默认全部 unknown，属于操作者声明。脚本不清缓存；预烘后不能宣称 OS 冷。一个批次只有首轮可沿用 reboot 或首次管线条件，后续轮自动记 unknown。

`budgetObservations` 只消费 `budgets.json.cache`：静态菜单热 5 s、冷 10 s；首次管线从脚本进入到正式 Playing 后首个实时画面 30 s，标题轮没有此端点，记未测；预载到 Walk 3 s、立即到 Walk 10 s。适用条件沿用有效几何、管线与 OS 状态声明，未知或条件不适用为 `NOT_MEASURED`；诊断为 `DIAGNOSTIC_ONLY`。体验只比较观测代理端点，超目标保留数值交 Root 判断，不增加门禁。
