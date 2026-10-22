# 启动体验与 CPU 诊断

沿用 `title`、`immediate`、`dwell` 三个入口与成熟私有 Windows desktop 启动器，不切作者桌面、不用全局输入。输出在 `output/start_game/TOOL-215-profile/<新目录>`。批次先持共享构建锁，调用 `Update-UESTCGameBuild` 普通构建 `Campus.csproj` 并执行 Godot 原生增量导入；预烘仍由 Root 按采样条件安排。每轮再取锁，与默认启动共用 `Test-UESTCLaunchOutputs`，只检查正式程序集、已发布业务清单、原生导入缓存三类产物是否都存在，不判新旧；任一缺失直接报错，计时内不补构建或导入。

普通体验：`powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录>`。默认三轮；单轮可用 `-Modes immediate`。默认 `experience` 显式设 `UESTC_STARTUP_PROFILE=0`，不开 EventPipe、不枚举 CPU 线程、不构建解析器。有归因需求时加 `-Measurement diagnostic`，才构建现有 CpuParse、启用逐帧日志和 EventPipe；诊断不能证明体验目标通过。`run-existing-immediate.ps1 -Output <新目录> -BuiltRevision <构建提交>` 复用相同体验路径。

Playing 短诊断沿同一个入口：`powershell -File scripts/build/startup-cpu/run.ps1 -Output output/start_game/TOOL-215-profile/GAME-151-<唯一目录> -Modes immediate -Measurement diagnostic -PlayingSample`。Root 先确认邻居源码稳定、没有构建/预烘/截图等重活，并关闭录屏和叠加层。开关只允许 diagnostic immediate；用既有 `--quality=fine --quality-set=display_mode=0` 选择 Fine 窗口，Fine 的 v_sync=1，不传 fixed-fps；其余体验条件和缓存资格只按实证填写，未知保留 unknown。正常 experience 的输入与计时端点不变。

`StageStartupTrace.TraceFrame` 在 `KernelStage._Process` 中持续执行；既有 `UESTC_STARTUP_PROFILE=1` 才逐帧输出引擎单调时钟、process-frame、渲染 CPU/GPU。诊断追加 flow、管线计数、GC pause、Walk 脚部位姿/yaw、action pressed 与界面模式，首次 Playing 只记一次正式 move_forward 物理绑定。没有遥测就不造分布。采集器沿原有 20 ms 循环增量读完整日志行；正式 input-ready 与 Playing/Walk 后若仍是界面态，按“单击画面开始”的正常路径，只向本轮 PID/HWND 的实际客户端中央发送一次 WM_MOUSEMOVE + 左键 down/up。客户端大小经 GetClientRect 实读，记录尺寸、点击坐标、请求、释放与后续首个真实漫游态帧；不移动作者真实鼠标、不强设模式、不伪造焦点、不循环重试。界面态本身不代表移动输入被锁住；这里进入漫游态是本轮采样条件。漫游态下等连续 10 帧管线计数不变，记录 10 s 静止，再向本 PID 私窗口按住实际前进绑定 20 s，随后放开并等后续真实帧观测到 action released；不切桌面、不用全局输入、不写世界坐标或代入速度。当前 Windows 前进绑定转换仅支持正式绑定中的 ASCII 字母/数字；特殊键、键盘布局不匹配或未收到 action 的情况不猜替代键。鼠标/按键请求和释放单独保留宿主 UTC/Stopwatch，finally 对本轮左键与前进键分别负责释放；复用原启动截止时间，未进入漫游态或未开始采样时明确失败。

`result.json.playingSample` 与失败时仍保留的 `playing-observations.json` 记录实际状态、请求、边沿和名义每秒进程 CPU 累计读数；`roamInput` 保留场景点击请求/释放与后续真实模式变化，未请求或未观测到的值保持 null。边沿使用当时最后一个完整 STARTUP_FRAME 原始时间/帧号，并保留观察与命令时刻，绝不插值成游戏输入生效时间。`cpu-hotspots.json` 按静止、前进、释放分窗给连续真实帧间隔与渲染 CPU/GPU 的 nearest-rank p50/p95/p99/max、>33/>100 ms 数量；帧号跳跃、非正间隔单列，不平摊或补齐。位姿与 action 共同报告是否观察到正向位移、静止的按键帧，撞挡的零位移照录；没有按键接收/位移就不能称实际前进完成。

场景点击复用标题 Enter 的同一 15 s 输入受理时长：没有后续真实 Playing/Walk、interfaceMode=False 帧就明确失败并保留记录，不重试、不等到通用 300 s 截止；10 s 静止与20 s前进端点不变。

每个 >33 ms 间隔带 RenderHost 的本帧/前帧事件、异常/缓存日志的原始行号与帧号夹界、附近主线程 sampled-thread-time 栈候选及原始诊断字段。日志没记录 hit/miss 时补烘仍是未知；RenderHost 动态句柄也不是静态 TileBundle 驻留计数。CPU 累计差是约 1 s 整进程 CPU，阶段线程差也保留；都不等于逐帧 CPU。EventPipe 含等待，栈与事件同现只提示候选，不能推出函数 CPU 份额或因果。原始 godot.log、threads.nettrace 与读数均保留，呈现帧间隔没有测量。

诊断开销在 playingSample 单列：逐帧日志、EventPipe、增量日志轮询、阶段线程枚举和约 1 s 进程 CPU 读数；后两者保留实际读时间/耗时。不扣估算开销，没有无遥测对照就不宣称体验或冷/热缓存目标通过。此入口不是 G5/G10 或新门禁。

批次 `preparation.json` 记录准备发生时间、完整墙钟耗时（含等锁）与真实 build/import 调用；同一记录进入每轮 condition/result 和诊断报告。准备结束后才进入每轮 capture 脚本，准备耗时独立于正式热启动计时。每轮有 `launch.json`、`launcher-timing.json`、`result.json` 与隔离 save/log。正式计时包含脚本进入、原生绑定初始化、等锁、正式输出存在检查、起引擎与稀疏阶段日志观测；`outputCheckSeconds` 直接读取真实检查时间戳，原生绑定的额外工具开销保守计入。script-entry Stopwatch、进程 StartTime UTC、引擎 `STARTUP_PHASE` 毫秒保留各自原点。产品双击入口也保存 `output/start_game/launch.json`；使用外部已持有锁时明确外部等锁未测。

`title` 体验在稀疏 `title-input-ready` 结束，端点是静态菜单可输入，不等后台资源；Title 没有实时世界。`title` 诊断继续捕获，直到静态菜单可输入、出生点资源 `preload-ready` 两者均观测到；`captureEndpoint` 明确标为 `static-menu-input-ready-and-background-new-game-preload-ready`，这是包含后台准备的另一端点。两类标题轮均不等待或制造 `realtime`。

`immediate` 观测到 `title-input-ready` 就向目标 PID 发送 Enter。`dwell` 等 `title-input-ready` 和稀疏 `preload-ready preloadReady=True` 后发送，没有固定停留秒数。两种进入轮均以 `new-game-enter` 的真实 `preloadReady` 字段记录受理状态；停留轮若受理时不是 true，直接失败。`observedMenuInputReadyToPreloadReadyWaitSeconds` 记录菜单可输入后等待资源的观测秒数，资源先完成则为 0；`observedMenuInputReadyToRequestSeconds` 记录实际请求前停留。

Enter 延用 PID 定向、私桌面 100 ms 短按：按请求时刻起在既有轮询中释放，再核对 `new-game-enter` 受理，受理超 15 s 失败且不重试。目标时长见 `input.shortPressTargetMilliseconds`，观测时长见 `observedRequestToReleaseMilliseconds`；墙钟时长不证明经过某个游戏帧。进入轮到正式 Playing 后的首个 `realtime` 和首次 Walk `input-ready` 两者均观测到才结束。采集固定 `--title=1`，不把 `--title=0` 记录算菜单测量。

`result.json` 保留请求、释放和资源预载锚点。短按完整计入“定向请求→首次 Walk 输入释放”，不扣除；`movementResponseSeconds=null`，实际方向输入与位移由既有核心专项检查。3 s 只在 `new-game-enter preloadReady=True` 时比较。端点含名义 20 ms 轮询、日志刷新与调度延迟，不插值、不宣称首步时间已测。诊断解析分别报告进程、主 OS 线程 CPU 计数与 sampled-thread-time（含等待，非函数 CPU），如实保留 attach 前缺口。

缓存参数分别声明 `-GeometryCache valid|missing|unknown`、`-PipelineCache warm|first-run|unknown`、`-SystemFileCache warm|reboot-first-run|unknown`，附 `-CacheEvidence <依据>`、`-JustPrebaked yes|no|unknown` 与 `-Device <设备>`。默认全部 unknown，属于操作者对显式准备后状态的声明；准备可能访问系统文件或执行导入，不能把准备前的冷缓存声明直接当作测量条件。脚本不清缓存；预烘后不能宣称 OS 冷。一个批次只有首轮可沿用 reboot 或首次管线条件，后续轮自动记 unknown。

`budgetObservations` 只消费 `budgets.json.cache`：静态菜单热 5 s、冷 10 s；首次管线从脚本进入到正式 Playing 后首个实时画面 30 s，标题轮没有此端点，记未测；预载到 Walk 3 s、立即到 Walk 10 s。适用条件沿用有效几何、管线与 OS 状态声明，未知或条件不适用为 `NOT_MEASURED`；诊断为 `DIAGNOSTIC_ONLY`。体验只比较观测代理端点，超目标保留数值交 Root 判断，不增加门禁。
