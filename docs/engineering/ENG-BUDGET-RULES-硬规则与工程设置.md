---
id: ENG-BUDGET-RULES
title: 硬规则与工程设置
category: engineering
parent: ENG-BUDGET
summary: 违反即不能推送的 23 条硬规则，及 project.godot、导出、渲染器等工程设置
keywords: [硬规则, 性能规则, 白名单着色器, 不得新增光源, 线程边界, 全局着色器参数登记, 风格底线不可关, 存档只走一条路, 本机烘焙同机确定, 工程设置, 渲染器, Shader Baker, 版本锁定]
depends: [CORE-INDEX, ENG-BUDGET, ENG-ARCH, ENG-PERF, SYS-GRAPHICS, SYS-SAVE, SYS-AUDIO, WLD-INTERIORS, ART-STYLE]
related: [ENG-GATES, WF-BUILD]
contracts: []
data: []
gates: [G4, G5, G7, G20, G21]
paths: ["Directory.Build.props", "game/**/*.gdshader*", "game/kernel/**/*.cs", "game/style/**", "game/project.godot", "game/export_presets.cfg", "game/kernel/map/data/**", "game/kernel/renderer/geometry/**", "game/kernel/renderer/render-state/**", "game/kernel/instances/**", "game/gen/geometry/**", "game/kernel/registry/**", "scripts/gates/arch/**", "**/Directory.*.props", "Directory.*.props", "game/gen/bakeinput/assembly/**", "budgets.json", "game/addons/**"]
---
# ENG-BUDGET-RULES · 硬规则与工程设置

> **范围**：违反即不能推送的 23 条硬规则（§1，覆盖渲染、材质、时间、烘焙、线程、画面设置、存档、室内清单、音频、确定性，条号不变，引用写"第 n 条"）与工程设置的规则（§2，含渲染器选择）。所有包交付前必须满足。
> **不在本文**：份额与设备档（ENG-BUDGET）；细节级别与速度律（ENG-BUDGET-LOD）；各条规则背后的策略（ENG-PERF）；已在 `game/project.godot` 里的值，以该文件为准。
> **查阅**：先读 §0；第 1–10 条（渲染、材质与时间）看 §1.1；第 11–23 条（烘焙、线程、画面设置、存档、室内清单、音频与确定性）看 §1.2；工程设置看 §2

## 0 结论

1. **硬规则 23 条，违反任何一条都不能推送**：第 1–10 条管渲染、材质与时间，第 11–23 条管烘焙、线程、画面设置、存档、室内清单、音频与确定性（§1）。
2. **门禁按这些条查**：一票否决含全部硬规则（ENG-GATES-VETO §1 第 10 条）；G4、G5、G7、G20、G21 各验其中几条。
3. **工程设置由主会话维护**：`game/project.godot`、`game/export_presets.cfg`、`Directory.Build.props` 的改动要申请，固定项四预设相同、运行时不动（§2）。

### 0.1 接入要点

- **写材质或着色器**：只经 `ToonSpec` 和调色板色名，只用白名单着色器，不逐物体建材质；着色器不用 `TIME`、`LIGHT_COLOR`，按距离判断用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`（§1 第 1、4、15 条）。
- **想加光源、SubViewport、CompositorEffect、全屏通道或 Environment**：不得新增，先申请（§1 第 2 条）。
- **运行时改渲染设置**：阴影开关、软影滤波、MSAA、TAA、FSR2、深度预通道、Environment 的各项开关会触发管线重编译，启动时定，运行时不改（§1 第 9 条）。
- **交付几何或碰撞**：经登记处按格交付；`Build()` 在工作线程上跑，不读世界状态和画面设置，所以时刻、季节、天气不让任何烘焙产物失效（§1 第 3、11、12、18 条）。
- **写逐帧代码**：零托管分配，不用 `System.Random`、`DateTime.Now`、`Stopwatch`（§1 第 4 条）。
- **要全局着色器参数**：先登记，运行时不增不读回（§1 第 13 条）。
- **读写 `user://`**：存档只经 `ISaveService`，不用 `ResourceLoader`、`ResourceSaver`（§1 第 19、23 条）。
- **要改 `project.godot`、`export_presets.cfg`、`Directory.Build.props`**：由主会话维护，申请（§2）。

## 1 硬规则（违反任何一条都不能推送）

### 1.1 渲染、材质与时间（第 1–10 条）

1. **材质只用白名单着色器**：只经 `ToonSpec` 和调色板色名获得；白名单以 `scripts/gates/shader-whitelist.json` 的根数组为准，新增文件要主会话批准；禁止逐物体建材质，逐物体变化放顶点属性、`INSTANCE_CUSTOM` 或 instance uniform（ENG-ARCH §2）。
2. **不得新增**光源、SubViewport、CompositorEffect、全屏通道、WorldEnvironment、Environment；只有渲染内核与舞台（ENG-ARCH）、主光与阴影（`game/kernel/lighting/`）、描线与后期（ART-STYLE-LINES）可以，先申请；水体系统（WLD-WATER）倒影视口已登记。
3. **几何经登记处按格交付**，烘焙器按级别给一个典型误差，不逐件上报；全部级别烘焙成 TileBundle，`Build()` 在工作线程上跑，主线程不生成几何；只有内核创建渲染实例（`MeshInstance3D`、`MultiMeshInstance3D`、`OccluderInstance3D`，ENG-ARCH §2），静态几何经 RenderingServer 挂载。
4. **计时与逐帧路径**：计时只由 动态与偶发事件（SYS-MOTION）的通道驱动（世界时间例外）；钟点、季节、天气只在 WorldState 那一步推进；风、水、一拍二、雨丝只读 `anim_time`；`Engine.TimeScale` 固定 1，着色器不用 `TIME`；禁用写法见 ENG-ARCH §2（G4）；逐帧零托管分配，不在 `_Process` 里 `GetNode`；界面每秒刷新 ≤ 10 次，动效只改 `Modulate`、非容器的 `Position` / `Rotation`、instance uniform 和 `ui_night`，小地图与大地图是登记例外。
5. **纹理**：导入为 VRAM Compressed，数据纹理运行时创建；各区不得自带大于 512² 的贴图。
6. **容器**按 ENG-PERF §1.1 选；MultiMesh 不用实例色、成批整块写；GPUParticles3D 只作备选，用要批准。
7. **顶点格式**：内核或烘焙器打包成压缩 20 B/顶点，每个表面 ≤ 65 536 个顶点，只用 ENG-PERF §2 登记的格式。
8. **discard**：不透明着色器里的 `discard` 与 `ALPHA_SCISSOR_THRESHOLD` 只用于白名单里的镂空类和抖动淡化。
9. **运行时不得改动会触发管线重编译的设置**：阴影开关、`LightAngularDistance` 与 `ShadowBlur` 的有无、软影滤波档位、阴影深度 16 位、MSAA、TAA、FSR2、CompositorEffect 的 `Needs*`、深度预通道、Environment 的各项 `*Enabled` 启动时定，四预设相同；画面设置按即时、加载、重启三类生效（SYS-GRAPHICS §3），会清空管线的设置不做成选项；启动时在扉页下按组合预热一批（ENG-PERF §2）；时间、季节、天气、介质只经全局参数与全局纹理；每次加载界面淡出之后 SURFACE、DRAW、Canvas 编译计数不增，新 MSDF 字形为 0。
10. **版本锁定**：Godot 4.7.2-stable .NET；升级经主会话评审，升级后跑一次快检和固定机位截图。

### 1.2 烘焙、线程、存档、音频与确定性（第 11–23 条）

11. **内容几何不依赖时刻、季节、天气**：`Build()` 不读世界状态，差异只做成着色器参数，改时间、换季、换天气不让任何烘焙产物失效。
12. **碰撞数据**本机烘焙，与可见几何同参数生成，不拿可见网格当 ConcavePolygonShape3D；碰撞 body 只在主线程创建、删除、入树，入树前加完形状；开放室内每个 body ≤ 250 个形状，每帧入树一个，单帧不超过 1 ms；自动门门体运行时恢复，不进烘焙碰撞；外围没有碰撞。
13. **全局着色器参数必须先登记**（`game/contracts/Materials/ShaderGlobals.cs`，类型与默认值在 `game/project.godot` 的 `[shader_globals]`）；运行时不得 `GlobalShaderParameterAdd`、不得读回；每个参数只有一个写入方。
14. **阴影投影物**必须在相机可见层上；草、插片、落叶、粒子、光晕、光锥一律 `cast_shadow = Off`；远处与植被的投影用 `ShadowsOnly` 代理。
15. **着色器里按距离判断**用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`，不用 `CAMERA_POSITION_WORLD`（阴影通道里它是灯的位置）；不用 `LIGHT_COLOR`（G4）。
16. **线程边界**：工作线程只做纯 .NET 计算、读写本机缓存、`res://` 后台加载和 RenderingServer 资源创建；场景树、`RenderingServer.Instance*`、PhysicsServer3D 的 body 只在主线程；gen 里禁用 `Parallel`、`Task.Run`；在途生成任务不超过 max(2, 逻辑核数 − 2)。
17. **风格底线不可关**（清单见 ART-STYLE-LINES §2）：任何预设、手动值和自动调节级别都不能关；实时阴影不低于 2048²、2 级，西湖倒影不低于每轴 1/4、每 4 帧；只有移轴、镜头光斑、光柱、暗角可关。最差值写在 `KnobSpec.WorstAllowed`，集中测试时在最差值配置下截图看一眼（G7）。
18. **画面设置不改烘焙、碰撞和世界状态**：`Build()` 与 `Campus.Gen.*` 不感知画面设置；各模块只经 `IQualityService` 与 `[QualityResponsive]` 接收设置，不直接读设置文件；每个单项恰有一个负责方（G5）。
19. **存档只走一条路**：只经 `ISaveService` 与 `[SaveSection]`，禁用 Godot Resource、ConfigFile、`StrToVar`、`BytesToVar*`、`GetVar`；只有 `Campus.Kernel.Save` 写 `user://saves`；快照不造成可见卡顿（主线程单帧 ≲ 1 ms），其余在线程池（SYS-SAVE-FORMAT §4）。
20. **室内只在开放清单里**（WLD-INTERIORS-BUILD §5）：清单外的楼没有完整室内和楼内碰撞；可走性不随钟点、季节、天气变；会动的门只由观者位置触发，剧情锁门也只走运行时门体。
21. **音频硬上限**：同时在播的声源数以 `game/audio/core/AudioVoicePool.cs` 为准；不在主线程同步加载素材；只有 `Campus.Audio.*` 能建播放器和改总线（SYS-AUDIO §1、§9）。
22. **本机烘焙要确定**（DECISION-165、DECISION-232）：同一台机器上同一输入烘出同样的字节；gen 与烘焙子任务的随机、哈希和遍历顺序按 ENG-ARCH §2。
23. **`user://` 不用 Godot 资源格式**（DECISION-165）：`ResourceLoader`、`ResourceSaver` 不得读写 `user://`；本机缓存用自定义 `.tbin`（ENG-ARCH §4）。

## 2 工程设置

`game/project.godot`、`game/export_presets.cfg`、`Directory.Build.props` 由主会话维护（WF-BUILD §1），改动要申请；已在 `game/project.godot` 里的值以该文件为准。

- **固定项**四预设相同，运行时一律不动（§1 第 9 条）；随画面设置改的项按 SYS-GRAPHICS §3 的类别改。
- **渲染器**：Forward+，默认 D3D12，不考虑兼容渲染器（跑不了 Compositor）和 Mobile 渲染器；渲染线程模式 Safe；导出开 Shader Baker（只烘 D3D12，要窗口模式导出）。Vulkan 只用于着色器源码级调试、交叉验证驱动问题和 D3D12 不可用时回落；截图与基准一律 D3D12，两个驱动的结果不混用。改默认为 Vulkan 须经主会话批准，前提是出现 D3D12 特有、短期绕不开的驱动问题，且 Vulkan 下 G10 不明显比 D3D12 差。
- **不用的引擎特性**（全项目唯一的清单）：SSAO、SSIL、SSR、SDFGI、体积雾、Environment 雾、反射探针、引擎 Decal、带阴影的点光与聚光；这些效果由自写的着色器与代理实现（ENG-PERF-LIGHT）。
- **Environment**：背景用 Sky，环境光与反射源关，Tonemap Linear，Glow 常开，运行时不切。
- **固定的渲染项**：深度预通道开且不改（描线读 normal_roughness），`screen_space_roughness_limiter`、MSAA、TAA、`hdr_2d` 关；`force_vertex_shading` 关，否则 `light()` 不执行。
- **C#**：工作站并发 GC，加载界面淡出后设 `SustainedLowLatency`。
