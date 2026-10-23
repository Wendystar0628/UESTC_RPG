# 启动体验与 CPU 诊断

沿用 `title`、`immediate`、`dwell` 与现有私有 Windows desktop 启动器。私桌面只隔离显示；测量无需切换桌面、获取 OS 焦点或操作作者键鼠。输出在 `output/start_game/TOOL-215-profile/<新目录>`。

普通体验：`powershell -File scripts/build/startup-cpu/run.ps1 -Output <新目录>`，默认三轮；单轮用 `-Modes immediate`。默认 `experience` 显式设 `UESTC_STARTUP_PROFILE=0`，不开 EventPipe、不枚举线程、不构建解析器。`-Measurement diagnostic` 才构建现有 CpuParse、启用逐帧日志和 EventPipe；诊断结果不证明体验目标通过。

Playing 诊断：`powershell -File scripts/build/startup-cpu/run.ps1 -Output output/start_game/TOOL-215-profile/GAME-151-<唯一目录> -Modes immediate -Measurement diagnostic -PlayingSample`。仅允许 diagnostic immediate；沿用 `--quality=fine --quality-set=display_mode=0` 的 Fine 窗口与预设 v_sync，不加 fixed-fps。Root 在采样前协调构建、预烘、截图等重活及录屏/叠加层。未知缓存条件保留 unknown。

输入走游戏内现有 `StageGateDriver` 与单职责 `StartupSampleDriver`：唯一参数 `--startup-scenario=title|immediate|dwell|playing` 选择场景，正常 `--title=1`、交互启动、画质、存档、时间与业务默认值保持正式路径。该参数不启用 CaptureMode，也不创建另一套服务或场景。

宿主在真实 `title-input-ready` 后调用 `StageGateDriver.OnTitleInputReady()`，立即返回，任务由驱动持有。Title 不注入任何输入。Immediate 此时通过 `Input.ParseInputEvent` 注入 Enter，下一真实 process-frame 后释放，让正式菜单按钮启动新游戏；Dwell 还等待同代真实 `IAppBootFlow.NewGamePreloadReady`，无固定停留时间。`new-game-enter` 保留正式流程的真实 `preloadReady` 字段；Dwell 受理时不为 true 则采集失败。

Playing 在上述 immediate 正式入场后，等待真实 Playing、宿主 realtime、Walk/碰撞/站立落定与加载遮挡释放，再测 10 s 静止、20 s 按住正式 `IInputRouter.PhysicalKey("move_forward")`，finally 释放；放键后等待后续真实帧、action released、速度归零与 Walk 落定。整个 20 s 不因首次位移而提前结束。输入均经过正式菜单/控制器路由，动作状态、脚部位姿、速度与碰撞如实记录，不改 action 状态或位姿，不按速度推算坐标。

新设计默认鼠标转视角；Alt 与面板租约控制光标。采集不使用旧界面模式或场景点击假设，不请求光标控制、不伪造焦点。正式 `CursorVisible/CursorHeld` 只作遥测，不能用光标可见性推断移动被锁。驱动任务使用 host Lifetime；宿主关闭时先取消、再 `await DrainStartupSample()`，最后释放输入与其他服务。正常取消仍执行 finally 放键，异常保留原对象进入现有失败通道；任务不自行关闭游戏或等待自己的关闭。

外层只按现有 20 ms 循环增量读取完整 `STARTUP_PHASE/STARTUP_FRAME` 日志、采集 CPU 与控制跟踪器。`title-enter-press/release` 与 `playing-idle-start/end`、`playing-forward-start/end`、`playing-release-observed` 由引擎内驱动记录实际单调毫秒和帧号；外层另记观察 UTC/Stopwatch，不把末次看到的帧伪装为输入时刻。失败仍保留 `playing-observations.json`、原始日志与 CPU 读数。

`title` 体验终点是静态菜单可输入；Title 没有实时世界。Title 诊断还等真实 `preload-ready`，`captureEndpoint` 明示包含后台准备。进入轮必须观测到真实 `new-game-enter`、Enter 释放、Playing 后首个 `realtime` 和首次 Walk `input-ready`；Playing 诊断再等完整动作释放落定。请求至 Walk 的时长直接用同一引擎时钟；启动脚本至菜单/实时仍是外层观测边界，包含日志刷新、名义 20 ms 轮询与调度延迟。不插值、不扣估算开销，不将输入释放代理称为首次步行响应。

`StageStartupTrace.TraceFrame` 的既有 profile 开关输出逐帧 renderer CPU/GPU、process-frame、flow、管线计数、GC pause、Walk 位姿/yaw、原生 action、速度/碰撞及光标状态。`cpu-hotspots.json` 按真实引擎边界分窗，分别给渲染 CPU/GPU 与连续 process 观察间隔的 nearest-rank p50/p95/p99/max、超过 33/100 ms 的数量。跳帧、非正间隔、缺失值单列，不补齐。隐藏桌面的帧率可能受呈现调度影响，过程间隔不能证明呈现 FPS 或体验卡顿；本轮判断渲染负载依据 CPU/GPU 工作时间，真实 FPS 需另约作者可见窗口测量。

位姿与 action 共同报告实际前向位移以及静止的按键帧。没有真实位移就不能称实际前进完成；撞挡零位移照录。每个较长过程间隔保留原始诊断字段、附近事件/异常/缓存行及 sampled-thread-time 栈候选；同现只说明候选，不证明函数 CPU 份额或因果。未知缓存命中、动态句柄与静态驻留分别保留原口径。

进程/线程 CPU 计数用 BCL 读取，约 1 s 累计差和阶段计数并非逐帧 CPU。启动复用 `ProcessTree` 持有本轮 job，primary OS TID 取其 `CreateProcessW` 返回值，不能用线程枚举顺序或 CLR ManagedThreadID 猜测。私有控制台的 Enter 仅停止现有 dotnet-trace 收集器并完成 rundown，不是玩家操作。EventPipe 是含等待的 sampled-thread-time，不能当 on-CPU 栈。所有诊断工具开销留在观测值内。

采样边界到达后，诊断先停收集器并完成 rundown，目标保持存活；随后读取游戏既有 `STARTUP_ENVIRONMENT` 发布的十进制 `windowHandle`，经本轮 `ProcessTree.PostClose(HWND)` 发送 WM_CLOSE，等待正式异步关闭完成。采样端点和启动计时保持原口径，关闭时间另记在 `shutdown.json` 与成功 `result.json.shutdown`。缺失句柄、发送失败、30 s 内未退出、非零退出或现有日志检查发现错误/泄漏，整轮均失败；超时后只回收本轮 owned job，不能算作正常关闭。不使用 `--quit-after`、`CloseMainWindow` 或 Kill 代替正式退出。失败保留原始日志、trace、CPU 与动作观察，并将 cleanup 失败挂到原异常后传出；成功结果只在清理完成后写出。

诊断同时采集 CLR AllocationTick，`gcAllocationSampling` 按阶段、OS 线程与类型汇总分配量子及缺失；这不是逐对象账目，也不能把分配栈归为某次 GC 暂停的原因。`topAllocationStacks.stackId` 在根级 `gcAllocationSampling.allocationCallStacks` 查完整栈，ID 只在该份报告内有效。没有事件、类型或栈时保留 `NOT_MEASURED`。已有 trace 可用 `dotnet CpuParse.dll <轮目录> <新报告名>.json` 重解析，无需重新采集。

解析器职责：`CpuParse.cs` 管参数、资源寿命与编排；`CpuPhaseReport.cs` 读取轮次证据、构造时窗并生成实际 OS CPU、阶段与总报告；`CpuTraceAnalysis.cs` 读取 TraceLog、在线聚合 AllocationTick 并共享原生栈索引、CodeAddress 描述缓存与完整分配栈表；`PlayingObservations.cs` 解析日志、帧间隔、尖峰候选和实际位移。入口持有 `JsonDocument` 与 `TraceLog` 直至流式 JSON 序列化完成，随后关闭索引并清理 `threads.etlx`；不保存原始分配 tick，不逐事件展开分配栈。

批次先持共享构建锁，通过 `Update-UESTCGameBuild` 普通构建与 Godot 增量导入，预烘由 Root 按条件安排。`preparation.json` 留真实准备时间和调用，单独于每轮 capture 脚本计时。每轮再取锁，用产品同一 `Test-UESTCLaunchOutputs` 检查正式程序集、已发布业务清单、构建期静态资源与原生导入缓存存在，不判断新旧；缺失直接失败，正式计时内不补构建。每轮保留 `launch.json`、`launcher-timing.json`、`result.json` 和隔离 save/log；原生启动绑定、等锁与输出检查开销计入脚本进入时长。

缓存分别用 `-GeometryCache valid|missing|unknown`、`-PipelineCache first-run|unknown`、`-SystemFileCache warm|reboot-first-run|unknown` 声明，附 `-CacheEvidence`、`-JustPrebaked yes|no|unknown` 与 `-Device`。声明针对显式准备后的状态，脚本不清缓存，预烘后不能宣称 OS 冷。每轮使用新的 APPDATA/LOCALAPPDATA，管线条件只能是 first-run 或 unknown，当前入口不提供热管线测量；系统文件缓存的 warm 独立声明。同批只有首轮可沿用 reboot-first-run 系统文件条件，后续自动记 unknown。

`budgetObservations` 只消费 `budgets.json.cache` 的首次管线到正式 Playing 端点与适用条件；依赖热管线的菜单热/冷、预载/立即请求到 Walk 预算不由当前入口测量。缓存证据缺失、条件未知或端点缺失为 `NOT_MEASURED`，条件齐备的诊断为 `DIAGNOSTIC_ONLY`。保留超目标原值交 Root 判断，不增加门禁。此入口不是 G5/G10，也不替代既有核心生命周期真实方向位移检查。
