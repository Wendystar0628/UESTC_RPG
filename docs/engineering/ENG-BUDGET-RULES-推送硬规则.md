---
id: ENG-BUDGET-RULES
title: 推送硬规则与工程设置
category: engineering
parent: ENG-BUDGET
summary: 23 条推送硬规则与 project.godot、导出、Environment 等工程设置的规则
keywords: [硬规则, 推送规则, 白名单着色器, 不得新增光源, 线程边界, 全局着色器参数登记, 风格底线不可关, 存档只走一条路, 本机烘焙同机确定, 工程设置, 渲染器, Shader Baker, 版本锁定]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX, ENG-BUDGET, ENG-ARCH, ENG-PERF, SYS-GRAPHICS, SYS-SAVE, SYS-AUDIO, WLD-INTERIORS, ART-STYLE]
related: [ENG-GATES, WF-BUILD]
contracts: []
data: []
gates: [G4, G5, G7, G20, G21]
paths: ["game/project.godot", "game/export_presets.cfg", "Directory.Build.props"]
---
# ENG-BUDGET-RULES · 推送硬规则与工程设置

> **范围**：推送硬规则 23 条（§1，条号不变，引用写"第 n 条"）与工程设置的规则（§2）。所有包交付前必须满足。
> **不在本文**：额度与设备档（ENG-BUDGET）；细节级别与速度律（ENG-BUDGET-LOD）；各条规则背后的策略（ENG-PERF）；已在 `game/project.godot` 里的值，以该文件为准。
> **查阅**：先读 §0；第 1–10 条（渲染、材质与时间）看 §1.1；第 11–23 条（烘焙、线程、存档与音频）看 §1.2；工程设置看 §2

## 0 结论

1. **硬规则 23 条，违反任何一条都不能推送**：第 1–10 条管渲染、材质与时间，第 11–23 条管烘焙、线程、存档与音频（§1）。
2. **门禁按这些条查**：一票否决含全部硬规则（ENG-GATES-VETO §1 第 10 条）；G4、G5、G7、G20、G21 各验其中几条。
3. **工程设置由主代理维护**：`game/project.godot`、`game/export_presets.cfg`、`Directory.Build.props` 的改动要申请，固定项四预设相同、运行时不动（§2）。

### 0.1 接入要点

- **写材质或着色器**：只经 `ToonSpec` 和调色板色名，只用白名单着色器（§1 第 1 条）
- **想加光源、SubViewport、CompositorEffect、全屏通道或 Environment**：不得新增，只有 WP-01、03、05 可以，先申请（§1 第 2 条）
- **交付几何或碰撞**：经登记处按格交付；`Build()` 在工作线程上跑，不读世界状态和画面设置（§1 第 3、11、12、18 条）
- **写逐帧代码**：零托管分配，不用 `System.Random`、`DateTime.Now`、`Stopwatch`（§1 第 4 条）；gen 里随机与哈希只用 `Rng`、`StableHash`（§1 第 22 条）
- **要全局着色器参数**：先登记，运行时不增不读回（§1 第 13 条）
- **读写 `user://`**：不用 `ResourceLoader`、`ResourceSaver`；存档只经 `ISaveService`（§1 第 19、23 条）
- **要改 `project.godot`、`game/export_presets.cfg`、`Directory.Build.props`**：由主代理维护，申请（§2）

## 1 硬规则（违反任何一条都不能推送）

### 1.1 渲染、材质与时间（第 1–10 条）

1. **材质只用白名单着色器**：只经 `ToonSpec` 和调色板色名获得；白名单以 `ownership.json` 的 `shaderWhitelist` 为准，新增文件要主代理批准；禁止逐物体建材质，逐物体变化放顶点属性、`INSTANCE_CUSTOM` 或 instance uniform（ENG-ARCH §2）。
2. **不得新增**光源、SubViewport、CompositorEffect、全屏通道、WorldEnvironment、Environment；只有 WP-01、03、05 可以，先申请；WP-24 倒影视口已登记。
3. **几何经登记处按格交付**，烘焙器按级别给一个典型误差，不逐件上报；全部级别烘焙成 TileBundle，`Build()` 在工作线程上跑，主线程不生成几何；只有内核创建渲染实例（`MeshInstance3D`、`MultiMeshInstance3D`、`OccluderInstance3D`，ENG-ARCH §2），静态几何经 RenderingServer 挂载。
4. **计时与逐帧路径**：计时只由 WP-26 的通道驱动（世界时间例外）；钟点、季节、天气只在 WorldState 那一步推进；风、水、一拍二、雨丝只读 `anim_time`；`Engine.TimeScale` 固定 1，着色器不用 `TIME`；随机与计时的禁用写法见 ENG-ARCH §2（G4），例外是新游戏时取一次的天气种子；逐帧零托管分配，不在 `_Process` 里 `GetNode`；界面每秒刷新 ≤ 10 次，动效只改 `Modulate`、非容器的 `Position` / `Rotation`、instance uniform 和 `ui_night`，小地图与大地图是登记例外。
5. **纹理**：导入为 VRAM Compressed，数据纹理运行时创建；区域包不得自带大于 512² 的贴图。
6. **容器**按 ENG-PERF §1.1 选；MultiMesh 不用实例色、成批整块写；GPUParticles3D 只作备选，用要批准。
7. **顶点格式**：内核或烘焙器打包成压缩 20 B/顶点，每个表面 ≤ 65 536 个顶点，只用 ENG-PERF §2 登记的格式。
8. **discard**：不透明着色器里的 `discard` 与 `ALPHA_SCISSOR_THRESHOLD` 只用于白名单里的镂空类和抖动淡化。
9. **运行时不得改动会触发管线重编译的设置**：阴影开关、`LightAngularDistance` 与 `ShadowBlur` 的有无、软影滤波档位、阴影深度 16 位、MSAA、TAA、FSR2、CompositorEffect 的 `Needs*`、深度预通道、Environment 的各项 `*Enabled` 启动时定，四预设相同；画面设置按即时、雾幕、重启三类生效（SYS-GRAPHICS §3），会清空管线的设置不做成选项；启动预热覆盖所有可达组合；时间、季节、天气、介质只经全局参数与全局纹理；每次雾幕淡出之后 SURFACE、DRAW、Canvas 编译计数不增，新 MSDF 字形为 0。
10. **版本锁定**：Godot 4.7.2-stable .NET；升级经主代理评审，并重跑表面字节格式比对门禁。

### 1.2 烘焙、线程、存档与音频（第 11–23 条）

11. **内容几何不依赖时刻、季节、天气**：`Build()` 不读世界状态，差异只做成着色器参数，改时间、换季、换天气不让任何烘焙产物失效。
12. **碰撞数据**本机烘焙，与可见几何同参数生成，不拿可见网格当 ConcavePolygonShape3D；碰撞 body 只在主线程创建、删除、入树，入树前加完形状；开放室内每个 body ≤ 250 个形状，每帧入树一个，单帧不超过 1 ms；自动门门体运行时恢复，不进烘焙碰撞；外围没有碰撞。
13. **全局着色器参数必须先登记**（`game/contracts/ShaderGlobals.cs`，类型与默认值在 `game/project.godot` 的 `[shader_globals]`）；运行时不得 `GlobalShaderParameterAdd`、不得读回；每个参数只有一个写入方。
14. **阴影投影物**必须在相机可见层上；草、插片、落叶、粒子、光晕、光锥一律 `cast_shadow = Off`；远处与植被的投影用 `ShadowsOnly` 代理。
15. **着色器里按距离判断**用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`，不用 `CAMERA_POSITION_WORLD`（阴影通道里它是灯的位置）；不用 `LIGHT_COLOR`（G4）。
16. **线程边界**：工作线程只做纯 .NET 计算、读写本机缓存、`res://` 后台加载和 RenderingServer 资源创建；场景树、`RenderingServer.Instance*`、PhysicsServer3D 的 body 只在主线程；gen 里禁用 `Parallel`、`Task.Run`；在途生成任务不超过 max(2, 逻辑核数 − 2)。
17. **风格底线不可关**（清单见 ART-STYLE-LINES §2）：任何预设、手动值和自动调节级别都不能关；实时阴影不低于 2048²、2 级，西湖倒影不低于每轴 1/4、每 4 帧；只有移轴、镜头光斑、光柱、暗角可关。最差值写在 `KnobSpec.WorstAllowed`，集中测试时在最差值配置下截图看一眼（G7）。
18. **画面设置不改烘焙、碰撞和世界状态**：`Build()` 与 `Campus.Gen.*` 不感知画面设置；各包只经 `IQualityService` 与 `[QualityResponsive]` 接收设置，不直接读设置文件；每个单项恰有一个负责方（G5）。
19. **存档只走一条路**：只经 `ISaveService` 与 `[SaveSection]`，禁用 Godot Resource、ConfigFile、`StrToVar`、`BytesToVar*`、`GetVar`；只有 `Campus.Kernel.Save` 写 `user://saves`；快照不造成可见卡顿（主线程单帧 ≲ 1 ms），其余在线程池（SYS-SAVE-FORMAT §3）。
20. **室内只在开放清单里**（WLD-INTERIORS-BUILD §5）：清单外的楼没有完整室内和楼内碰撞；可走性不随钟点、季节、天气变；会动的门只由观者位置触发，剧情锁门也只走运行时门体。
21. **音频硬上限**：同时在播的声源数以 `game/audio/core/AudioVoicePool.cs` 为准；不在主线程同步加载素材；只有 `Campus.Audio.*` 能建播放器和改总线（SYS-AUDIO §1、§9）。
22. **本机烘焙要确定**（D-165、D-232）：同一台机器上同一输入烘出同样的字节，不要求本机与别的机器逐字节一致；gen 与烘焙子任务的随机和哈希只用契约的 `Rng`、`StableHash`，不用 `System.Random`、系统时钟、`string.GetHashCode`、`Guid.NewGuid`，遍历字典、集合先排序。
23. **`user://` 不用 Godot 资源格式**（D-165）：`ResourceLoader`、`ResourceSaver` 不得读写 `user://`；本机缓存用自定义 `.tbin`，每格按它用到的输入判鲜，不符就重烘（ENG-ARCH §4、ENG-PERF-STREAM §1）。

## 2 工程设置

`game/project.godot`、`game/export_presets.cfg`、`Directory.Build.props` 由主代理维护（WF-BUILD §1），改动要申请；已在 `game/project.godot` 里的值以该文件为准。运行时的规则：

- **固定项**四预设相同，运行时一律不动（§1 第 9 条）；随画面设置改的项按 SYS-GRAPHICS §3 的类别改。
- **渲染器**：Forward+；D3D12 不可用时回落 Vulkan，不回落兼容渲染器；渲染线程模式 Safe；导出开 Shader Baker（只烘 D3D12，要窗口模式导出）。
- **深度与抗锯齿**：深度预通道开且不改（描线读 normal_roughness）；`screen_space_roughness_limiter` 关（会改坏材质 ID）；MSAA、TAA 关；渲染比例用 FSR1；`hdr_2d` 固定关。
- **阴影与灯光**：不用带阴影的点光与聚光、引擎 Decal、反射探针；`force_vertex_shading` 关，否则 `light()` 不执行。
- **剔除与帧节奏**：内置遮挡剔除开，离地 60 m 以上运行时关；垂直同步默认开；`low_processor_mode` 关（不做静止降频）。
- **Environment**：背景用 Sky，环境光与反射源关，Tonemap Linear，Glow 常开，雾、体积雾、SSAO、SSIL、SSR、SDFGI 关，运行时不切。
- **C#**：工作站并发 GC，雾幕结束后设 `SustainedLowLatency`。
