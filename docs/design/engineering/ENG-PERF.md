---
id: ENG-PERF
title: 性能工程：高质量实现设计（Godot 4.7.2 .NET）
category: engineering
summary: 性能的实现方法：合批、着色器白名单、全局参数登记、预热、剔除、流式、主线程、内存、阴影后期、自适应与测量
keywords: [着色器白名单, 全局着色器参数, 全局数据纹理, ShaderGlobals, globals.gdshaderinc, 合批, 自动实例化, MultiMesh, 绘制调用口径, 管线预热, 管线编译计数, 顶点格式, 抖动淡化, 遮挡剔除, 渲染图层, TileBundle, 流式加载, 卡帧, 本机烘焙缓存, .tbin, 零分配, 显存账, PSSM 阴影, 方向量化, 灯光通道, 后期链, 描线, 设备档, 自动调节画质, 性能基准]
owners: [WP-01, WP-02]
contributors: [WP-03, WP-04, WP-05, WP-07, WP-23, WP-24, WP-27, WP-32, WP-33, WP-34, WP-37, WP-38, WP-39, WP-40, WP-42, WP-43, WP-45, WP-46, WP-47, WP-48, WP-49, WP-50, WP-51, WP-53]
depends: [ENG-ARCH, ENG-BUDGET, SYS-TIME, UI-NAV, SYS-WALK, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO]
related: [ART-STYLE, ART-SKYLIGHT, WLD-VEGETATION, WLD-WATER, SYS-MOTION, SYS-VIEW, ENG-GATES, ART-FIDELITY, ENG-WP]
contracts: [ShaderGlobals]
data: ["game/style/shaders/", "game/style/outline/", "output/perf/"]
gates: [G2, G4, G5, G6, G8, G10, G11, G13, G16, G17, G19, G20, G21]
---
# ENG-PERF · 性能工程：高质量实现设计（Godot 4.7.2 .NET）

> **范围**：本文规定性能的实现方法：绘制管线与合批、着色器白名单、全局参数与数据纹理登记表、编译与预热、填充率、剔除、流式加载、主线程与 C#、内存与显存、安装包与本机烘焙缓存、植被、阴影光照与后期、设备档与自适应、测量与门禁。引擎锁定 Godot 4.7.2-stable .NET（C#、Forward+、Windows、默认 D3D12），只做 PC，当前只做观光模式；目标是视觉精度的底线处处成立、细节按精度区分级（ART-FIDELITY §0）、绝不卡帧。
> **不在本文**：预算数值与硬规则（ENG-BUDGET）、契约（ENG-ARCH §3）、画面设置的单项与预设数值（SYS-GRAPHICS）、门禁的登记与完整判据（ENG-GATES）、美术与内容设计（ART-STYLE、ART-SKYLIGHT、WLD-VEGETATION、WLD-WATER 等）。ENG-BUDGET给出预算和硬规则，本文给出实现方法；两者冲突时，预算数值和硬规则以 ENG-BUDGET为准，实现方法与全局参数登记以本文为准（D-133）。
> **查阅**：先读 §0；着色器白名单与全局参数登记看 §1.4、§1.5；预热与编译看 §2；图层与剔除看 §4；流式与本机烘焙看 §5；阴影、灯光与后期看 §10；画面设置与自动调节看 §11；交付自检看 §14
> **读者**：按需查阅。其他包接入时先看 §0.1；给场景加内容时查 §1.1（容器选型）、§1.4（着色器）和 §4（图层）；写着色器或要读世界状态时查 §1.5、§2；内核与流式（WP-01、02、43、51）读 §1、§5–§8 里与自己相关的小节；材质与后期（WP-04、05）读 §2、§3、§10.2；阴影、灯光与倒影（WP-03、24、39）读 §10.1、§10.3、§10.4；植被、飘落与降水（WP-23、38、45）读 §9；剔除（WP-33）读 §4；界面与平面地图（WP-35、48）查 §1.4 的画布着色器和 §2 的二维预热；画面设置与自动调节（WP-32）读 §11；测量与门禁（WP-27）读 §12；任何包交付前对照 §14 自检。
> **API 参考**：[docs/godot-api/README.md](../../godot-api/README.md)，写代码时以它的签名为准。
> **关联**：[ENG-ARCH 工程架构](ENG-ARCH.md)、[ENG-BUDGET 性能预算](ENG-BUDGET.md)、[ENG-GATES 验收门禁](ENG-GATES.md)、[ENG-WP 工作包](ENG-WP.md)、[SYS-TIME 时间季节](../systems/SYS-TIME.md)、[UI-NAV 界面与跳转](../ui/UI-NAV.md)、[SYS-WALK 行走](../systems/SYS-WALK.md)、[SYS-WEATHER 天气](../systems/SYS-WEATHER.md)、[SYS-GRAPHICS 画面设置](../systems/SYS-GRAPHICS.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL.md)、[SYS-ATLAS 平面地图与传送](../systems/SYS-ATLAS.md)、[SYS-TITLE 开始页与过场](../systems/SYS-TITLE.md)、[SYS-SAVE 存档](../systems/SYS-SAVE.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS.md)、[SYS-AUDIO 音频](../systems/SYS-AUDIO.md)、[ART-STYLE 风格基础](../art/ART-STYLE.md)、[ART-SKYLIGHT 天空光照大气](../art/ART-SKYLIGHT.md)、[WLD-VEGETATION 植被](../world/WLD-VEGETATION.md)、[WLD-WATER 水](../world/WLD-WATER.md)、[SYS-MOTION 动态与事件](../systems/SYS-MOTION.md)、[SYS-VIEW 观察模式与介质](../systems/SYS-VIEW.md)、[ART-FIDELITY 视觉精度](../art/ART-FIDELITY.md)

## 0 十条总原则

1. **引擎锁定为 Godot 4.7.2-stable .NET**，渲染器 Forward+，Windows 默认 D3D12，Vulkan 只用于调试和兜底（§13）。代码全部用 C#，着色器用 Godot 着色语言，计算着色器用 GLSL。升级引擎要主代理评审：表面的字节格式在 4.2 改过一次，升级时必须重跑 §12.5 的比对门禁。
2. **场景只用白名单里的着色器**（§1.4），共用 `toon.gdshaderinc`。材质特性靠"调色板 + 顶点标志位 + 全局参数"在着色器内分支实现，不在运行时新建 Shader、改 `render_mode` 或 `#define`。雾幕散去（有开始页时是标题页转为实时，SYS-TITLE §1.1）之后，管线编译计数（SURFACE、DRAW、Canvas）不得再增长（G16）。
3. **PC 的瓶颈在两头**：CPU 一侧是 Forward+ 每次绘制的提交和剔除开销；GPU 一侧是"同一份几何要走很多遍"（深度预通道、颜色通道、4 级阴影）和后期的填充。所以合批靠离线合并、自动实例化和 MultiMesh（§1），阴影只能靠减少投影物省钱（§10.1）。
4. **阴影用引擎原生的 4 级 PSSM**，自写的只剩方向量化、阴影方向高度钳、主光强度、退光线和日月交接（§10.1）。描线、边缘光、移轴、光斑、倒影、窗灯和路灯光斑自己实现（§10.2–§10.4）。不用 SSAO、SSIL、SSR、SDFGI、体积雾、反射探针和 Environment 雾。
5. **不透明通道里的 discard 只用于镂空类和抖动淡化**（ENG-BUDGET §7 第 8 条）。透明物只有近景透明窗（同屏不超过 32 块）、水的透明变体（可选）、粒子、光晕、光柱、细尘和介质覆盖层；贴面贴花留在不透明通道（ART-MODELING §7.5），透明的 `toon_decal` 只作备选（§1.4），启用时彼此不重叠、按 `render_priority` 排在最前（§1.6）。
6. **主线程只挂载，不生成。** 几何全部烘焙成 TileBundle：发布版在玩家机器上本机烘焙（D-165），第一次需要时由工作线程调同一个 `Build()` 生成并写进本机缓存，之后从缓存读（§5）；本机缓存是自定义的 `.tbin`，由读缓存的工作线程任务读入、校验，并在同一工作线程上创建 GPU 资源，不经 `ResourceLoader`（读取路径以 ENG-ARCH §4 为准）；`ResourceLoader.LoadThreadedRequest` 只读 `res://` 里的启动集等资源；主线程只调少量 RenderingServer 接口，每帧最多激活 1 格，不超过 1.0 ms（§5）。
7. **保证绝不卡帧、不穿地的六条不变量**，详见 §5：L4 常驻；父格要等 4 个子格都"加载、挂载、管线就绪"才隐藏；按字节 LRU 淘汰；看门狗；碰撞数据没有就绪就冻结物理（行走模式）；镜头在楼体里至少显示 I0、进开放室内前未就绪就在门槛前 1 m 软停（第 6 条，WLD-INTERIORS §6.4）。另有两条全局约束：雾幕之后管线编译计数不增（§2）；主线程从不生成几何（第 6 条）。
8. **预算按"场景"核算，不按"格"平均。** 银杏大道这类热点地段按 E1 视角核算。
9. **测量分两类**：吞吐量（`ViewportGetMeasuredRenderTimeGpu/Cpu` 的毫秒，用来判断回归）和体验（帧间隔、掉帧和卡顿，用来判断是否达标）。PC 上 GPU 计时一律可用。
10. **任何调参都不能触发管线重编译。** 会改特化常量或管线键的设置（阴影开关、PCSS 的有无、软影滤波档位、16 位深度、MSAA、TAA、Compositor 的 `Needs*` 缓冲、深度预通道、Environment 的 `*Enabled`）只在启动时定，四个画面预设相同，不做成选项；画面设置按即时、雾幕、重启三类生效（§11.7、SYS-GRAPHICS §3），启动预热覆盖所有可达的组合；改时间、换季、换天气、切换观察模式和介质只改全局参数和全局纹理（§1.5）。

### 0.1 接入要点

- **要给内容上材质**：只经 `ToonSpec` 和调色板色名取卡通材质（ENG-ARCH §3）；不建 StandardMaterial3D，不自己新建 Shader 或 ShaderMaterial；逐物体的变化放顶点色号、`INSTANCE_CUSTOM` 或 instance uniform，不逐物体建材质、不用材质覆盖；运行时换材质只许换成预热过的变体（§1.1、§1.6、§2）
- **要用或新增着色器、顶点格式**：只用 §1.4 白名单里的文件，共用 `toon.gdshaderinc`；新增着色器文件或顶点格式（登记的只有 VF-A 至 VF-D）要主代理批准；运行时只改参数值，不新建 Shader，不改 `render_mode`、`#define` 或特化开关；可开关、按状态出现的节点启动时就放进树里，让预热编到（§1.4、§2）
- **着色器要读时间、季节、天气、介质或画面设置**：只读 §1.5 登记表里的全局参数和全局数据纹理，每项只有一个写入方；要新增时先登记进 §1.5 的表（它是唯一的登记来源：主代理据此改 `project.godot`，WP-01 据此生成 `globals.gdshaderinc` 和 C# 名字常量）；C# 侧用 `static readonly StringName` 常量写入，不用 `GlobalShaderParameterGet` 读回（§1.5、§6）
- **要在场景里显示东西**：经内核的 `IRenderHost` 申请，不自己建 RenderingServer 实例或 MeshInstance3D（ENG-ARCH §2）；按 §1.1 选容器：同种部件共用 Mesh RID 与材质 RID，每格同种超过约 64 个时用 MultiMesh（统一格式、不用实例色、`MultimeshSetBuffer` 整块写入）；只有动态物体走场景节点（§1.1、§1.2）
- **要生成几何或碰撞**：写成确定性的 `Build()`（`Campus.Gen.*`，不引用 GodotSharp，超越函数用 `DetMath`，不用 §5 列出的禁用 API，每个表面不超过 65 536 个顶点），由内核在工作线程上调用；主线程只挂载、不生成几何；`Build()` 不读世界状态和画面设置；工作线程上不调场景树、`RenderingServer.Instance*` 和 PhysicsServer3D 的 body 接口，gen 里不用 `Parallel`、`Task.Run`（§5、§6、§14）
- **要分级或淡入淡出**：几何的淡入淡出在 `toon.gdshaderinc` 里按 4×4 Bayer 抖动做（instance uniform `fade_t0`，0.25 s；MultiMesh 不能逐实例淡化，改在 vertex() 里缩到 0）；visibility range 的 `fadeMode` 一律 Disabled，不用它自带的淡化；同级网格 LOD 只换索引、不换顶点，跨级持续存在的面保持同一法线、色号和材质 ID，剪影与投影构件留到不足 1 px，小件的线先于几何淡出（§1.2、§2）
- **写逐帧代码或着色器**：逐帧路径零托管分配（不用 LINQ、闭包、装箱、字符串拼接，`GetNode` 只在 `_Ready` 里调）；调用引擎 API 一律批量；计时只用 `Campus.Kernel.Frame` 的计时作用域；着色器不用 `TIME` 和 `LIGHT_COLOR`，按距离的判断用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`（§6、§14）
- **内容要投影或接收阴影**：投影物放在相机可见的图层上，按 §4 的图层登记选层，要新层向主代理申请；草、插片、粒子、光晕不投影，植被和远处体量用 `ShadowsOnly` 代理（第 5 层）；场景里只有 WP-03 的 KeyLight 一盏方向光，阴影的特化开关运行时一律不动，light() 以外要主光方向时读 `key_dir`（§4、§10.1）
- **要让东西随时刻亮灯**：接进 WP-39 在按图的 `lighting-schedule.json` 里登记的 32 个灯光通道，亮度由着色器读 `light_channels` 和按哈希的阈值算出，CPU 不逐盏写；路灯光斑走 `lamp_grid`、`lamp_table`，只有重点灯用 OmniLight3D：全校不超过 32 盏，不投影，`LightCullMask` 排除第 2 层（§10.3）
- **本包有受画面设置影响的内容**：在自己的 `[KernelNode]` 上用 `[QualityResponsive]` 声明负责或只读的单项，只经 `IQualityService` 接收，不读 `graphics.cfg`；按即时、雾幕、重启三类落到引擎上，有视觉变化的项淡化完成、不硬切，密度类用 `VisibleInstanceCount` 前缀抽稀；本包的风格底线在每个预设的最差值配置下仍在（§11.7、§11.3）
- **要释放资源或经历切图**：只释放自己的实例和 TileBundle（`FreeRid` 加 `Dispose()`），不释放共享的材质、图集和数据纹理；全图纹理与包围盒从 `map_rect`、`MapDescriptor.Bounds` 换算，不写死"全校"；跨图常驻的节点只经 `IMapAware` 换数据（§7、§14）
- **交付前**：逐条对照 §14 的性能自检清单，并通过 G8 静态预算（§12.5）

## 1 绘制管线与合批（WP-02，WP-01 配合）

### 1.1 容器选型规则

| 内容 | 容器 | 说明 |
| --- | --- | --- |
| 静态、独有的块几何（墙板、屋面、地面、铺装） | 离线按"材质 × 不超过 16–24 m 的块"合并成 ArrayMesh，经 RenderingServer 挂载 | 每块 1–8k 三角形，每块每种材质一个表面。块要小，遮挡剔除才有效（§4） |
| 在本格只出现 1–3 次、不超过 2k 三角形的部件 | 烘焙时直接并进所在的块 | 不值得单独实例化 |
| 每格同种不超过约 64 个的重复部件（雨水管、外机、灯杆、长椅） | 独立实例：同一个 Mesh RID、同一个材质 | Forward+ 的自动实例化把它们合成一次绘制，每个实例单独剔除 |
| 每格同种超过约 64 个的重复部件（窗模块、窗帘、栏杆段、单车） | MultiMesh，范围取"格 ∩ 楼"（一栋楼的同种部件一个） | MultiMesh 整体剔除、共用一级 LOD；按楼拆开，才有机会被遮挡剔除 |
| 植被、草、地面落叶、飘落池、降水池、檐滴、荷、萤火虫、光晕、光锥 | MultiMesh（§9、§10） | 每实例 48–80 B |
| 动态物体（动物、水禽、钟面指针等） | 场景节点（`MeshInstance3D`，由内核经 `IRenderHost` 创建） | 只有它们走节点 |
| 远景剪影 L4、本图遮挡体、外围 O2 雾中楼顶（WLD-OUTSKIRTS §4.2） | 常驻的合并网格 | 切图的 Mount 那一步同步加载（冷启动时在扉页后面，SYS-MAPS §4.2） |

**自动实例化的条件**：

- 不透明列表里相邻两个元素的排序键全部相同（LOD、深度层、表面、几何、材质、着色器、优先级），而且镜像状态一致，就并成一次绘制；
- 只在 Forward+ 下有效；不适用于 MultiMesh、蒙皮或混合形状网格、透明材质；
- 一段最多合并 2²⁰ − 1 个元素。

所以：同一种部件必须共用 Mesh RID 和材质 RID，逐实例的变化放 instance uniform（它是逐实例数据，不进排序键）或顶点属性，不用材质覆盖。排序键里有深度层，长段会被距离切开，实际命中率由 P0 实测（§12.2）。

### 1.2 MultiMesh 与 RenderingServer 挂载的用法

**MultiMesh**：

- 格式统一：`TransformFormat = Transform3D`，`UseCustomData = true`（`INSTANCE_CUSTOM`，4 个 float），`UseColors = false`。不用实例色：它在着色器代码执行之前就已经和顶点色相乘了，会把色号改坏。每实例 48 B 变换 + 16 B 自定义数据 = 64 B。格式统一以后，MultiMesh 相关的特化常量只有一种组合，预热简单（§2）。
- 静态的 MultiMesh 随 TileBundle 烘焙存盘；动态的（植被近池、草、落叶层）用 C# 侧预分配的 `float[]` 拼好，一次 `RenderingServer.MultimeshSetBuffer(rid, ReadOnlySpan<float>)` 写入，不逐实例调 `MultimeshInstanceSetTransform`。只有每帧改几个实例的可以逐实例写：飘落池每阵风改 3–5 个槽位，少量动物的实例变换（WLD-LIFE）。
- `CustomAabb` 用烘焙统计里的包围盒；顶点着色器里会移动的（风、飘落、雪），包围盒要罩住全部位移。罩住箱庭内容的（飘落池）用地图包围盒 `MapDescriptor.Bounds`；跟相机走的降水池不用 `Bounds`，而用可达范围"中心 ± 3 km"（`MapDescriptor.Center` ± `roam.maxRadius`，y 取 `floorY`…`ceilY`），镜头在箱庭外朝外看时整池才不会被剔除；两者都在切图时重设（SYS-MAPS §3）。
- 画面设置的植被与草密度、降水与粒子密度，以及自动调节，都用 `VisibleInstanceCount`（§11.3、SYS-GRAPHICS §3）：实例按稳定种子的哈希排好序，前缀就是均匀抽稀后的集合。改它不重编译，也不改缓冲大小。
- 间接绘制（`MultimeshAllocateData(..., useIndirect: true)` 加 `MultimeshGetCommandBufferRdRid`）可以做计算着色器剔除，只在近池重建实测太慢时才考虑。

**挂载**（主线程，WP-02 的流式管理器）：

1. `RenderingServer.InstanceCreate2(meshRid, scenarioRid)`；
2. `InstanceSetTransform`、`InstanceSetLayerMask`（图层见 §4）、`InstanceGeometrySetCastShadowsSetting`、`InstanceGeometrySetVisibilityRange`（`fadeMode` 为 Disabled），HLOD 用 `InstanceSetVisibilityParent`；
3. 需要时写 instance uniform：`InstanceGeometrySetShaderParameter`（§1.5）；
4. 子格就绪前保持 `InstanceSetVisible(false)`，切换时同一帧批量改可见性。

每格约 50–150 次调用。场景节点只留给动态物体。淘汰时先 `FreeRid` 掉本格的实例，再对 TileBundle 调 `Dispose()`，让 Mesh RID 立即释放，不等 GC（P0 用 `RenderVideoMemUsed` 回到基线来验证，§16）。

**网格 LOD**：只换索引不换顶点。每一级是"误差 + 这一级自己的索引数组"（契约 `MeshLod`，ENG-ARCH §3），与本表面共用同一份顶点，写进表面的 `lods`（`AddSurfaceFromArrays` 的 `lods` 字典，键是误差、值是该级的索引数组；或表面字典里的 `lods` 键）。引擎允许每级各有一份索引数组，不要求是前缀。

- **连续表面**（墙面、屋面、倒角盒、树冠壳）的同级简化必须生成新的索引数组（边折叠一类的简化，只引用原有顶点），不能截掉一段三角形，否则会留下洞。
- **前缀**只是快捷写法（`MeshLod.Prefix`）：三角形按件和重要度排好序，低一级截掉排在后面的整件，只用于独立的叶片、小件和合并块里的整栋楼。
- **建筑的主线版**只用 L0+–L4 语义级别（ENG-BUDGET §3），不给同级网格 LOD；同级内部的简化后做。

网格 LOD 的默认阈值 1 px，运行时可调 `Viewport.MeshLodThreshold`。同级简化在 gen 里按语义自己写（本节"跨级不跳线、不跳明暗"第 2 条），结果确定，能过 `--bake-verify`。`ImporterMesh.GenerateLods`、`SurfaceTool.GenerateLod` 属于 GodotSharp，纯 .NET 的 gen 调不到，不用；部件库和树冠云确实要二次误差简化时，经主代理批准引入 meshoptimizer 的 .NET 绑定，产物同样要逐字节一致（G6）。建筑一律不用自动简化，以免破坏轮廓和倒角高光。阴影通道用主相机选出的同一级 LOD。

**跨级不跳线、不跳明暗**（ART-MODELING §7.7；跳变检测见 ART-FIDELITY §8 第 4 条）：语义级别 L0+–L4 之间有 0.25 s 的抖动过渡（§2），同级网格 LOD 却是一帧内换索引、没有淡化；三渲二里线和两阶明暗一跳就很显眼。所以：

1. **同一表面跨级不变**：跨级持续存在的表面保持同一法线约定、色号和材质 ID。同级网格 LOD 共用一份顶点，法线和色号本来就不能变；要不同的法线，就在顶点缓冲末尾追加粗级专用顶点。例如倒角盒追加 24 个尖角顶点（8 个尖角，每个按相交的三个面拆开、带面法线），细级不引用，粗级只引用它们，只换索引就把倒角收成直角；切换放在倒角宽不足 1 px 以后。没被引用的顶点在带索引的绘制里不进顶点着色。
2. **同级网格 LOD 只做不足 τ 的变化**：整件拿掉独立小件（`MeshLod.Prefix`）；平面内合并三角形（为 AO 边环、贴面混色加的细分，粗级只引用原有角点）；嵌套降段（圆柱、圆管按 32 / 16 / 8 段生成，端盖的扇形也嵌套，粗级只连偶数号顶点，留下的顶点法线是精确的径向法线；配合解析法线则明暗完全不动，§2）。
3. **剪影与投影构件留到看不见**：檐口、女儿墙、屋顶设备、雨篷这类剪影和投长影的构件留到不足 1 px。每级的几何误差按"被删构件的最大可见尺寸"记（ENG-BUDGET §7 第 3 条），投影构件再乘低太阳下的影长系数（太阳高 4°–5.5° 时影长是高度的 10–14 倍）；或者可见几何先走，投影留在 `ShadowsOnly` 代理里（第 5 层，代理的可见上限 = 可见上限 + 最长影长，同植被，§9）。
4. **线先于几何消失**：小件的线类淡出距离（`mat_table` 行 0）取它剔除距离的 0.6–0.7 倍。MultiMesh 共用一份 instance uniform，不能逐实例淡化，所以 MultiMesh 里的小件在剔除距离前 10%–20% 于 vertex() 里按主相机距离（`MAIN_CAM_INV_VIEW_MATRIX[3].xyz`）缩到 0，阴影里同步缩，线随件变细、消失。
5. **抖动期间两级的基面共面**：过渡期两级同时画，4×4 Bayer 让两级的深度和法线逐像素交错，差异处会被描成一片噪线，斑点去除也滤不掉。所以两级的基面用同一个平面方程（量化差 1–2 mm，远低于 1.5% 的深度判据），两级的差异在切换距离上不超过 τ。
6. **L2 窗带不把明暗烘死**：窗带在着色器里按立面的 N·L 和主光方位画一条窗洞影边（上沿和背光一侧），与 L1 的平均明度对齐，差不超过 3%（ENG-BUDGET §3）。
7. **HLOD**：visibility parent 在 `Disabled` 模式下父子硬切换，只用在父子差异不足 τ 的地方；有可见差异的交给语义级别的抖动。

默认状态（秋 17:30，太阳高度 5.5°）的推镜头录像同时看低太阳下的影子跳变（ART-FIDELITY §8 第 4 条）。

### 1.3 调用口径

Godot 里只有一个"绘制调用"口径，分主通道和阴影两类。

| 统计 | 读法 | 口径 |
| --- | --- | --- |
| 主通道绘制调用 | `Viewport.GetRenderInfo(Viewport.RenderInfoType.Visible, Viewport.RenderInfo.DrawCallsInFrame)` | 不透明、运动、透明三张列表里，每个自动实例化连续段记 1 次；深度预通道不重复计数，所以实际提交给 GPU 的约为"不透明 × 2 + 透明" |
| 阴影绘制调用 | `Viewport.GetRenderInfo`，类型取 `RenderInfoType.Shadow`，项取 `RenderInfo.DrawCallsInFrame` | 各级阴影合计 |
| 三角形 | `RenderInfo.PrimitivesInFrame`，或 `Performance.Monitor.RenderTotalPrimitivesInFrame` | 带 LOD 的表面按所选那级的索引数计，**不乘 MultiMesh 的实例数**；不带 LOD 的表面按顶点数 × 实例数；间接绘制不计 |
| 界面 | `RenderInfoType.Canvas` | 不计入主通道 |

因为引擎的三角形计数在 MultiMesh 上不可靠，三角形以 G8 的预测和内核自记的账为准，引擎计数只作对照，偏差超过 15% 报警。各档的数值见 ENG-BUDGET §2，按包的额度见 ENG-BUDGET §8，都写进 `budgets.json`。

### 1.4 着色器白名单

白名单就是 `game/style/shaders/` 和 `game/style/outline/` 里的文件清单（ENG-ARCH §2），新增文件要主代理批准。下表是初始清单；文件名以系统名作前缀。

**公共 include**（不计入数量）：

| 文件 | 归属 | 内容 |
| --- | --- | --- |
| `globals.gdshaderinc` | WP-01 | 全部 `global uniform` 的声明，由 §1.5 的登记表生成；G5 比对它、`project.godot` 和 C# 名字常量三者一致 |
| `toon.gdshaderinc` | WP-04 | 调色板取色、明暗分阶的合成与 light() 主体（ART-STYLE §2）、材质 ID 写进 ROUGHNESS、抖动淡化、退光线；天气分支：天光方向（§10.1）、湿润变暗与积水（SYS-WEATHER §7，挂点归 WP-04，公共函数归 WP-45b）、闪电的 EMISSION 增量（§10.2）。明暗档按材质读 `mat_table`（建筑两阶、曲面道具与软物两阶半、植被三阶，ART-STYLE §2.1）；影色读调色板行 0，室内顶点读行 2（室内影色）、行 3（室内受光色）并乘光斑增益 `interior_light.x`（ART-STYLE §3）；影里随法线 y 的天光渐变与朝下面的地面暖反光（ART-STYLE §3）；透光写 `BACKLIGHT`，在 light() 方向光分支里按阈值化阴影加进 `DIFFUSE_LIGHT`；受光侧暖边、湿面闪点（透光、暖边、闪点都见 ART-STYLE §5）；`map_rect` 以外的片元改读顶点里的季节覆盖潜势（WLD-OUTSKIRTS §4.5）；倒角带按法线曲率提亮一阶、球与柱类道具的解析法线、贴面贴花的底色混合（§2）。**不做接触影**，遮蔽只推阈值、不加一阶 |
| `fog.gdshaderinc` | WP-04（天空色带的部分由 WP-40 以需求提交） | 共用雾函数：雾程按高度积分（标高 200 m，高空俯瞰不会整片陷在雾里），地平线天色、200 m 降饱和、800 m 剪影、贴地雾与湖雾、介质混合；天气只修正它的输入（`weather_fog`），签名不变；旁观边缘雾按 `edge_fog` 只在朝外一侧加浓：密度 ×(1 + 0.6 × `edge_fog` × w)（WLD-OUTSKIRTS §8.3）；另有一份逐行对应的 GLSL 版 `fog.glslinc` 给计算着色器 |
| `motion_wind.gdshaderinc` | WP-26 | 风场相位与三层摆动（§9），与 C# 的 `IWindField.Sample` 同一公式 |
| `precip_wet.gdshaderinc` | WP-45b | 湿润与积水的公共函数：暴露度、积水遮罩、积水倒影色、湿面掠射天光带、雨点环；toon、water 与外围都 include 它（toon 的挂点 `toon_wet_exposure`、`toon_wet`、`toon_puddle` 归 WP-04，函数体调本文件） |

**空间着色器与其他**（共 25 个：空间与天空着色器 22 个、画布着色器 3 个；常驻 22 个，另有 1 个桩（`toon_stub`）、2 个可选或备选（`water_clear`、`toon_decal`）；主代理可再合并）：

| 文件 | 归属 | 用途 | 通道 |
| --- | --- | --- | --- |
| `toon_static.gdshader` | WP-04 | 建筑、地面、道具的块几何、独立实例和重复部件的 MultiMesh；外围 O0、O1、O2 也用它（WLD-OUTSKIRTS） | 不透明 |
| `toon_cut.gdshader` | WP-04（WP-07 以需求提交） | 剖切变体：`cull_disabled`，背面输出剖切色、法线朝向相机、ROUGHNESS 写剖切 ID（SYS-VIEW §5）；开放楼内当前层与 ±1 层的室内壳（WLD-INTERIORS §6.3） | 不透明 |
| `toon_glass.gdshader` | WP-04 | 窗玻璃：室内映射、斜条高光、窗灯；关着的门的门厅卡图层、窗卡号查层号表（WLD-INTERIORS §5） | 不透明 |
| `toon_glass_near.gdshader` | WP-04 | 开放楼室内一侧的朝里玻璃与开放入口的门玻璃（同屏不超过 32 块，WLD-INTERIORS §4、WLD-INTERIORS §8.3） | 透明 |
| `toon_decal.gdshader` | WP-04 | 透明的预投影网格贴花，`depth_draw_never`，不投影；只作备选：事件贴花、地面标线和落叶堆一律做成不透明的贴面几何（`toon_static` 的贴花分支，ART-FIDELITY §6），P0 后用不上就从白名单删去（§16） | 透明（备选） |
| `toon_stub.gdshader` | WP-00 | 桩（ENG-ARCH §5），WP-04 交付后删除 | 不透明 |
| `veg_trunk.gdshader` | WP-23 | 树干、团块、绿篱；同一着色器也给 `ShadowsOnly` 代理用（instance uniform 打开噪声挖洞） | 不透明 |
| `veg_card.gdshader` | WP-23 | 叶片插片：alpha scissor、`cull_disabled`、风、季相、开花 | 不透明 |
| `veg_impostor.gdshader` | WP-23 | L2 替身（半八面体替身或团块低模，P0 二选一） | 不透明 |
| `veg_grass.gdshader` | WP-23 | 草和 20 m 内的地面实例叶 | 不透明 |
| `veg_lotus.gdshader` | WP-38 | 荷：叶、柄、花、莲蓬合成一个网格，尺寸随季节 | 不透明 |
| `water.gdshader` | WP-24 | 默认不透明水面（不读深度和屏幕纹理）；雨点环、阴天水色是它的天气分支；按到岸距离三道硬边色带、倒影碎片与 5 级色块化、四芒星闪点（WLD-WATER） | 不透明 |
| `water_clear.gdshader` | WP-24 | 可选：需要真实交界泡沫时的透明变体 | 透明（可选） |
| `reflect_flat.gdshader` | WP-24 | 倒影世界里的平涂 REFLECT 代理，丢弃水面以下的片元 | 不透明（倒影世界） |
| `fx_fall.gdshader` | WP-38 | 飘落池：落叶、花瓣、雪团，位置由 `anim_time` 解析算出（雪花由 `precip` 画） | 不透明（alpha scissor，双面） |
| `precip.gdshader` | WP-45 | 降水池：雨丝、雪花、溅落、檐滴共用（§9）。`blend_premul_alpha`、`unshaded`、`depth_draw_never`，写 `FOG`；含太阳雨的受光着色（逆光雨丝染暖金，SYS-WEATHER §3） | 透明 |
| `fx_glow.gdshader` | WP-39 | 加法、不受光：窗灯光晕、路灯光晕与光锥、萤火虫；光柱分三个分支：**室外软边光柱**（开口圆柱或扁片，读 `hint_depth_texture` 做深度软化）、**室内棱柱光柱**（解析求视线在柱内的路径长）、**细尘**（MultiMesh 小方片，位置在 vertex() 里由 `anim_time` 解析算出，不在光路里的塌缩成 0），见 §10.2 | 透明 |
| `diorama_section.gdshader` | WP-22 | 箱庭截面地层与水柱 | 不透明 |
| `diorama_far.gdshader` | WP-22 | 360° 远景环形卡片与雾海面片；流畅预设下外围 O2 的卡片环用它的卡片分支，清水河银带是雾海分支的顶点属性（WLD-OUTSKIRTS §11.4） | 不透明 |
| `sky.gdshader` | WP-40 | 天空（`shader_type sky`）：五段渐变、日月盘、星空、银河、积雨云卡片、城市天光；天气：半分辨率云层、阴天云盖、远处雨幕前锋、闪电照亮的云团、彩虹 | 天空 |
| `medium_overlay.gdshader` | WP-07 | 相机上的全屏四边形：介质染色、焦散、地层线（`unshaded`、`depth_test_disabled`） | 透明 |
| `cine_cloud.gdshader` | WP-49b | 开始页与过场的云卡和镜头云幕：alpha scissor、`cull_disabled`、不投影、不进倒影世界，按太阳或月亮方位分受光与云腹两阶，走共用雾函数写 `FOG`，材质 ID 取"不描"；只在 Title、Entering、Leaving 可见，节点启动时就在树里（SYS-TITLE §9） | 不透明 |
| `ui_panel.gdshader` | WP-35 | 界面：面板、线、图标、文字（`kind` 0–3）；切角 SDF、物理像素发丝线、流光、焦点衍射边、量化出入场、悬停、晶白环境影、深屏解析辉光、交班扫描前沿；只读 `ui_night`、`anim_time`、`step_rate`（UI-VISUAL §11.5） | 画布 |
| `ui_screen.gdshader` | WP-35 | 界面：读屏幕纹理的效果，只在低频场合挂：`CanvasGroup` 整页揭示（扫描、网格、量化）与大页磨砂（配 `BackBufferCopy(Rect)`）（UI-VISUAL §11.5） | 画布 |
| `map_base.gdshader` | WP-48b | 平面地图：底图 SDF 上色、地标线描 MSDF（`mode` 为材质 uniform，不用 `#define`）；只读 `season`（SYS-ATLAS §2.2） | 画布 |

**计算着色器**（GLSL，经 RenderingDevice，共 6 个）：

| 文件 | 归属 | 用途 |
| --- | --- | --- |
| `post_outline.glsl` | WP-05 | 描线、凸边亮线与屏幕空间边缘光（POST_SKY）；远雨幕带、闪光帧加深并进同一次 dispatch（§10.2）。同一文件里分两次 dispatch（线遮罩 → 斑点去除、帐篷抗锯齿与合成），算一个文件 |
| `post_tiltshift_blur.glsl`、`post_tiltshift_comp.glsl` | WP-05 | 移轴：1/2 或 1/4 分辨率可分离模糊（两种分辨率的管线启动时都建好）、按屏幕 y 与深度合成；合成那次 dispatch 并进高光肩部和暗角（ART-STYLE §7） |
| `post_flare.glsl` | WP-05 | 主光可见度（32 个深度点写进存储缓冲）和光斑 |
| `post_lut.glsl` | WP-05 | 把 CDL ∘ 32³ 基表写进运行时 LUT |
| `post_fxaa.glsl` | WP-05 | 可选：跳过描线像素的自写 FXAA，只在 SMAA 侵蚀细线时启用（§10.2） |
| `fog.glslinc` | WP-05 | 计算侧的雾函数，与 WP-04 的 `fog.gdshaderinc` 逐行对应，逐像素差不超过 1/255；两边改动同步提交（ART-SKYLIGHT §6.5） |

另有烘焙专用的 `bake_impostor.gdshader`（归 WP-23，与替身烘焙器同包，§9；只在替身烘焙器的 SubViewport 里用，不进游戏场景）。

- MultiMesh 和独立实例共用同一个着色器：引擎按特化常量区分，不需要另写一份。
- 以下内容都并进上表，不另加着色器：星空、银河、城市天光、积雨云并进 `sky`；萤火虫、光晕、光柱并进 `fx_glow`；花瓣并进 `fx_fall`；季相、覆雪、月影都是现有着色器里的全局参数分支。行走模式不画任何东西，也不新增着色器。
- **天气**只有 `precip.gdshader` 一个专用着色器（SYS-WEATHER §0 第 5 条），雪花由它画；云、雨幕前锋、闪电、彩虹并进 `sky`，远雨幕带和闪光帧并进 `post_outline`，湿面、积水和天光方向并进 `toon.gdshaderinc`，雨点环并进 `water`，天气雾并进 `fog.gdshaderinc`。画面设置不新增着色器：移轴的两种分辨率、描线的变体都是计算管线，启动时建好（§11.7）。
- 各功能落在哪个着色器：
  - **界面**：`ui_panel` 和 `ui_screen` 两个文件。读屏幕纹理的效果必须单独成文件：着色器里只要出现 `hint_screen_texture`，引擎就会在它之前做一次背景拷贝，与分支是否执行无关（UI-VISUAL §11.5）。`rendering/viewport/hdr_2d` 固定为关，界面画在后期之后，不吃暗角、分调、高光肩部和 LUT（UI-VISUAL §11.1）。
  - **平面地图**只有 `map_base.gdshader` 一个；小地图和大地图共用，两种用法由材质 uniform 区分；它在开场雾幕或扉页期间按两种 `mode` 和两套主题各画一次（SYS-ATLAS §10）。
  - **开始页与过场**只有 `cine_cloud.gdshader`（SYS-TITLE §9）。不复用 `diorama_far`：云卡要做全遮擦入擦出和受光两阶。
  - **外围**（WLD-OUTSKIRTS）不新增着色器：O0、O1 用 `toon_static`、`toon_glass`、`veg_*`；O2 和流畅档的卡片环用 `toon_static` 与 `diorama_far` 的已有分支；清水河银带是 `diorama_far` 雾海分支的顶点属性。
  - **室内**（WLD-INTERIORS）不新增着色器：门厅卡和窗卡是 `toon_glass` 的图层，室内壳的剖切用 `toon_cut`，门口光池走灯表着色。
  - **风格**不新增着色器：都在 `toon.gdshaderinc`（明暗档、室内行、天光渐变与地面反光、透光、受光侧暖边、湿面闪点，不做接触影）、`fx_glow`（三种光柱分支）、`precip`（太阳雨）、`water`（色带、倒影碎片、闪点）、`post_outline`（两次 dispatch）和 `post_tiltshift_comp`（高光肩部、暗角）里。计算着色器仍是 6 个。

### 1.5 全局参数与数据纹理表

**做法**：世界状态的连续量走全局着色器参数（`RenderingServer.GlobalShaderParameterSet`），数组一律做成数据纹理（全局参数支持 bool、int、float 及其向量、color、mat2–4、transform，以及 sampler2D、2DArray、3D、Cube，**不支持数组**）。

- 全部参数必须先在 `project.godot` 的 Shader Globals 里登记，否则着色器编译失败。**下面两张表是唯一的登记来源**：主代理据此改 `project.godot`，WP-01 据此生成 `globals.gdshaderinc` 和 C# 名字常量（`game/kernel/renderer/`，`static readonly StringName`），G5 比对三者。
- 每个参数只有一个写入方。每帧写入几乎不花代价；**禁止从脚本读回**（`GlobalShaderParameterGet` 会同步等待，ENG-BUDGET §7 第 13 条）。
- 只改全局参数的值，不碰任何材质的着色器、`render_mode` 和 `#define`（ENG-BUDGET §7 第 9 条）。
- 名字一律小写下划线、不带前缀；其他文档里出现的 `g_` 前缀或驼峰写法（如 `animTime`）指的就是表里的同名量，以本表为准。
- 计算着色器读不到全局参数：Compositor 的 C# 代码每帧把它要的几项（雾、介质、主光方向、CDL，以及天气的远雨幕、闪光、雨丝倾角和内部分辨率的线宽系数，`edge_fog`、`map_rect` 的中心、描线加深 `line_darken` 与两个染色色 `line.warm`、`line.cool`、暗角强度与色调）写进 push constant 或 uniform buffer，数值取自与全局参数同一份的 C# 状态（天气向量、`QualityView`），不读回全局参数。

**全局参数**（37 个）：

| 名称 | 类型 | 内容 | 写入方 | 更新 |
| --- | --- | --- | --- | --- |
| `clock` | float | 世界钟点 0–24 h | WP-34 | 每帧 |
| `anim_time` | float | 动画时间：每帧 delta 累加，mod 480 s；暂停（P）时照常走（SYS-TIME §1），`--anim=` 可冻结 | WP-34 | 每帧 |
| `fade_clock` | float | 内核过渡用的时钟：delta 累加 mod 480 s，不受 `--anim=` 冻结；只给抖动淡化用 | WP-01 | 每帧 |
| `step_rate` | float | 一拍二的步频：帧率上限为 30 时（含 60 Hz 屏的半刷新率）取 15，其余取 12；截图和门禁一律 12（§11.4） | WP-32 | 帧率上限变化时 |
| `season` | vec4 | 连续季节 s（0–4）、转场起点季、目标季、转场进度 | WP-34 | 每帧 |
| `sun_dir`、`moon_dir` | vec3 | 美术太阳、月亮的真实方向（天空和日月盘用，不量化、不钳高度） | WP-34 | 每帧 |
| `moon_lit` | vec4 | xyz 月盘球面明暗的受光方向，w 月相（默认 1 = 满月） | WP-34 | 每帧 |
| `key_dir` | vec3 | 主光当前（量化后）的方向，与 DirectionalLight3D 一致；给 light() 以外的代码用（水面碎金带、草的逆光） | WP-03 | 转灯时 |
| `key_strength` | float | 主光强度 smoothstep(−2°, 4°, h) | WP-03 | 每帧 |
| `shadow_strength` | float | 阴影强度：无影蓝调里 0，月影最强 0.45，白天 1；再乘天气乘子 (1 − 0.8 × 阴度)，白天不低于 0.2，雨夜无月影（§10.1） | WP-03 | 每帧 |
| `retreat_y` | float | 退光线的高度（§10.1） | WP-03 | 每帧 |
| `wind` | vec4 | (风向 x, 风向 z, 幅度系数, 湍流)：幅度系数 = 四季 × 昼夜 × 天气 × 转场扬起，由 WP-26 每帧乘成一个数（SYS-MOTION §11.4、SYS-MOTION §12.2）；湍流是风暴度大于 0 时叠加的细碎阵风，周期取 480/k（SYS-MOTION §11.4、WLD-VEGETATION §11.6） | WP-26 | 每帧 |
| `snow` | float | 覆雪量 = min(1, max(季节基线, 天气累积))（WLD-VEGETATION §9.6、SYS-WEATHER §2） | WP-38 | 每帧 |
| `ground_amt` | vec4 | 地面遮罩 R、G、B 三层的季节量（B = max(季节基线, 天气累积)，可到 2，WLD-VEGETATION §9.5），w 为花瓣标志 | WP-38 | 每帧 |
| `veil` | float | 雾幕强度（换季转场、远距离跳转、画面设置的雾幕类应用） | WP-01 | 每帧 |
| `stars` | float | 星空可见度 | WP-40 | 每帧 |
| `fog` | vec4 | 雾密度、霾带高度（地平线以上的度数）、起雾距离、剪影距离（来自调色板关键帧的标量）；天气的修正走 `weather_fog`，在雾函数里叠加 | WP-37 | 每帧 |
| `fog_low` | vec4 | 贴地雾的高度上限（m）、贴地雾密度、湖面雾强度、预留（来自调色板关键帧；清晨的贴地雾与日出湖雾） | WP-37 | 每帧 |
| `medium`、`medium_color` | vec4、color | 介质的起点、目标、混合度、能见度；介质色（SYS-VIEW §5） | WP-07 | 每帧 |
| `ui_night` | float | 界面主题的扫描前沿位置：0 全屏晶白，1 全屏深屏；按真实太阳高度带回差锁存（下穿 −0.5° 切深屏、上穿 +0.5° 切晶白），切换时 0.6 s 走完，减少动态效果时一帧走完（UI-VISUAL §3.6、UI-VISUAL §11.6）。只有界面着色器读它；常驻小部件默认固定深屏，不随它切换（UI-VISUAL §3.6） | WP-35（`UiThemeClock`，`FramePriority.Hud`） | 只在切换的 0.6 s 里每帧写 |
| `reflect_vp` | mat4 | 倒影相机渲染那一帧的 VP 矩阵 | WP-24 | 倒影更新时 |
| `reflect_lake` | int | 当前做倒影的湖号；水面材质的湖号与它相同时才采样 `reflect_tex`，否则走解析天空（WLD-WATER） | WP-24 | 换湖时 |
| `incident`、`incident_route` | vec4、vec4 | 偶发事件的进度、版本、淡出系数，版本含天气版本（SYS-MOTION §10.8）；路线折线端点（SYS-MOTION） | WP-26 | 事件期间每帧 |
| `weather_a` | vec4 | 云量、阴度、降水量、降水类型（0 雨 → 1 雪） | WP-45b，在 `FramePriority.Motion` 从 `IWorldState.WeatherNow` 换算写入；45a 不碰全局参数 | 每帧（WorldState 求出天气向量之后，SYS-WEATHER §8） |
| `weather_b` | vec4 | 湿润、积水、风暴度、彩虹 | WP-45b（同上） | 每帧 |
| `weather_fog` | vec4 | 雾密度倍率、贴地雾顶高、贴地雾密度、远雨幕 | WP-45b（同上） | 每帧 |
| `cloud` | vec4 | 漂移 xy（= f(世界时间) + g(`anim_time`)，C# 累加）、云型、积雨云量 | WP-40 | 每帧 |
| `lightning` | vec4 | 闪光、方位、仰角、种子（节奏与闪光安全见 SYS-WEATHER §5） | WP-40 | 每帧（动态那一步） |
| `snow_fx` | vec4 | 新雪亮度、冠雪、融雪湿、保留 | WP-38 | 每帧 |
| `overcast_dir` | vec3 | 天光方向（世界坐标）：主光方位、仰角 55°，与 KeyLight 同一套量化（§10.1） | WP-03 | 转灯时 |
| `quality` | vec4 | 着色器要读的画面设置：细节距离系数、植被与草密度、草地范围（m）、保留（SYS-GRAPHICS §8） | WP-32 | 设置或自动调节变化时 |
| `map_rect` | vec4 | 全图数据纹理覆盖的 xz 范围：(x0, z0, 1/宽, 1/深)，uv = (p.xz − xy) × zw。清水河取 x、z 都是 −1280…1280 m，即 (−1280, −1280, 1/2560, 1/2560)（SYS-MAPS §10；取全图数据纹理的覆盖范围，不等于箱庭包围盒 `MapDescriptor.Bounds`） | WP-46 | 切图的 Mount 那一步（`FramePriority.Map`） |
| `edge_fog` | float | 旁观边缘雾 0–1：水平类约束的接近量，0.5 s 平滑；行走、室内、地下、水下、过场和截图门禁时为 0（WLD-OUTSKIRTS §8.3） | WP-06 | 每帧（相机那一步） |
| `shaft` | vec4 | 光柱：整体强度、近镜淡入起点（1.2 m）、终点（5.5 m）、顺轴衰减（0.4）（ART-STYLE §7.6） | WP-39 | 每帧 |
| `interior_light` | vec4 | 室内光：x 光斑增益（默认格 1.35，范围 1.2–1.6）、y 室内光柱整体系数（镜头在楼内时 0.55）、z 阴影方向抬高进度 0–1（WLD-INTERIORS §6.3）、w 保留 | WP-07（介质那一步统一写；z 的进度由介质切换求出，WP-03 在主光那一步从同一份 C# 状态读，不读回全局参数） | 每帧 |

**全局数据纹理**（`global uniform sampler2D`，ImageTexture 由写入方运行时创建；只更新内容、不换 RID，材质不会失效；切图除外，见表后）：

| 名称 | 尺寸与格式 | 内容 | 写入方 | 更新 |
| --- | --- | --- | --- | --- |
| `palette` | 256×64 RGBA8，`source_color` | 调色板（行布局见下） | WP-37 创建、每帧写入并绑定全局参数（`mat_table` 归 WP-04） | 每帧整张 `Update()`，64 KB |
| `light_channels` | 32×1 RGF，最近邻 | 32 个灯光通道的值 L 和过渡宽度 Δ（§10.3） | WP-03（数值来自 WP-39 的时刻表） | 每帧 |
| `window_override` | 256×256 RGBA8，最近邻（256 KB） | 6.5 万扇窗的覆盖值：强制亮灭、冷暖、窗帘（全校立面约 5.8 万个开间，WLD-INTERIORS §12）；按图 | WP-39 | 变化时，≤ 10 Hz |
| `lamp_grid` | 8 m 网格，范围 = `map_rect`（清水河 2560 m，320×320），RGBA32F，最近邻 | 每格最多 4 个灯号；按图 | WP-39（烘焙） | 静态（切图时重建） |
| `lamp_table` | N×2 RGBA32F | 每盏灯的位置、半径、灯照色号、所属通道、开灯阈值、事件序号；开放室内的每个入口登记一盏门口灯（WLD-INTERIORS §4）；按图 | WP-39（烘焙） | 静态（切图时重建） |
| `tree_species` | 32×8 RGBA32F | 树种表：叶量 A、开花 B 的四季锚点，摆幅，枝条笔触的数组层号，色行号，是否落叶 | WP-38 | 启动时 |
| `mat_table` | 128×4 RGBA8，最近邻 | 按材质 ID 查：行 0 = 线表（线色染色强度、线宽系数、擦除位、只描外轮廓、内部软线、不描、小件淡出距离）；行 1 = 明暗档、`half_level`、`band_soft`、湿润响应；行 2 = 透光系数 k_t、受光侧暖边系数、闪点系数；行 3 = 解析法线的方式（0 不用、1 球、2 柱）、倒角提亮的受光幅度、影里幅度（0 表示这个材质不提亮，也不画凸边亮线）、保留（ART-STYLE §2.5、ART-STYLE §2.6、ART-STYLE §4.5）。线表放在这张全局纹理里、不另建 RD 纹理：toon 着色器要读湿润响应，RD 纹理它读不到；计算着色器用 `RenderingServer.TextureGetRdTexture()` 读这一张 | WP-04 持有，WP-05 共读 | 启动时写入，静态 |
| `ground_mask` | 全图（范围 = `map_rect`）RGBA8，不压缩 | R 银杏落叶潜势、G 其他落叶与花瓣、B 积雪潜势、A 保留（§9） | WP-15（R、G 由 WP-23 生成） | 静态（切图时重建） |
| `terrain_height` | 全图（范围 = `map_rect`）RF | 低精度地形高度（飘落池落地） | WP-15 | 静态（切图时重建） |
| `reflect_tex` | 倒影 SubViewport 的 ViewportTexture | 倒影 | WP-24 | 按预设的刷新间隔（每 2–4 帧，SYS-GRAPHICS §1） |
| `cloud_noise` | 512² RGBA8，可平铺 | 云层和地面云影共用的噪声（SYS-WEATHER §7） | WP-40 | 静态 |
| `precip_occ` | 全图（范围 = `map_rect`）`Image.Format.Rh`，2 m/px（清水河 1280²，3.3 MB） | 俯视遮蔽顶面高度：雨丝截在顶面、积水倒影步进（§9） | WP-45（开发期烘焙，随启动集发，D-165；与 WP-02、WP-15 配合） | 静态（切图时重建） |
| `refl_lamps` | 16×2 RGBAF | 共用灯条表：16 盏关键灯，每盏灯一列两个纹素：(x, z, y, 灯光通道号)、(颜色行号, 开灯阈值, 条宽, 灯光表索引)；亮度不由 CPU 写，着色器按灯光通道算（排布以 WLD-WATER §7.3 为准）；水面、积水、湿路面和倒影世界共用（§10.3） | WP-24 | 4 Hz |
| `ground_mask2` | 全图（范围 = `map_rect`）RGBA8，不压缩，与 `ground_mask` 同尺寸 | 地面遮罩 2：R 积水潜势、G 建筑遮蔽（檐下、雨篷、连廊为 0）、B 树冠遮蔽（运行时乘叶量）、A 保留（SYS-WEATHER §7） | WP-15 | 静态（切图时重建） |

- 不设全局纹理 `contact_spheres`（不做接触影，§10.1）。
- **全图纹理共用一个 `map_rect`**（SYS-MAPS §10）：`ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid`，以及以后任何"全图"纹理都按它换算，各自的分辨率可以不同；不隐式按"全校"换算。
- **范围外**：`globals.gdshaderinc` 提供 `map_uv()` 和 `in_map()`（WP-01 生成）；范围外一律取"无数据"的默认值：遮罩为 0、`precip_occ` 视为没有顶面、地形高度取 0。外围 O1、O2 和截面上缘不论是否落在 `map_rect` 内（截面线东西两侧约 ±1250 m 在范围内，南北两侧在范围外），一律不读这几张纹理：季节覆盖的潜势按面类别写进顶点，配程序噪声做阈值生长，由外围的顶点标志选择数据来源，`map_rect` 的边线穿过 O1 也不出接缝（WLD-OUTSKIRTS §3.5、WLD-OUTSKIRTS §4.5）。
- **切图**：各写入方在 `OnMapLoaded` 里重建纹理；尺寸不变用 `ImageTexture.Update()`，尺寸变了用 `SetImage()`（会重新分配显存）。RID 是否保持、会不会让材质失效或触发编译，P0 验证（SYS-MAPS §15）。
- 全局纹理类型里没有 `usampler`，所以灯号这类整数用浮点格式存；RGBA16F 只能精确表示 2048 以内的整数，灯数可能超过，`lamp_grid` 用 RGBA32F（清水河 320×320，约 1.6 MB）。
- 全局 sampler 上的 `source_color`、`filter_*`、`repeat_*` 提示，源码显示与普通纹理走同一条路径，P0 实测确认；不生效时，季相行改用 `texelFetch` 手工插值，并在行末多存一列处理冬 → 春的衔接。`cloud_noise` 同样依赖 repeat 和 mipmap 提示，不生效时在着色器里对坐标取 `fract` 并手工选 mip。

**调色板的行布局**（色彩设计见 SYS-TIME §3.1）：

| 行 | 内容 | 读法 |
| --- | --- | --- |
| 0 | 当前色：256 个色号在当前时刻和季节下的颜色 | `texelFetch`，按顶点 COLOR 里的色号取列 |
| 1 | 灯照色：同一色号在路灯光斑里的颜色。光斑里用它替换影色，画面仍然只有两阶 | `texelFetch` |
| 2 | 室内影色：同一色号在室内的暖影，按 ART-STYLE §3.4 推导 | `texelFetch`，顶点 COLOR 的"室内"位为 1 时用 |
| 3 | 室内受光色（按 ART-STYLE §3.4 推导）：窗光斑里的受光色，再乘 `interior_light.x`（WLD-INTERIORS §8.2） | `texelFetch`，顶点 COLOR 的"室内"位为 1 时用 |
| 4–63 | 季相行：每行一个植被色号，横向是 256 个季节样本 | `texture()` 沿季节轴线性过滤，横坐标 = s_i / 4（s_i 是逐株错相后的局部季节，§9）；`repeat_enable` 让冬 → 春自然衔接 |

- **CPU 求值**：WP-37 的关键帧表按"太阳高度 × 晨昏 × 四季"组织（19 键 × 4 季）。C# 每帧用双精度在 OKLab 里插值，插值规则照 SYS-TIME §3.1，再叠加天气修正量（SYS-WEATHER §0 第 2 条），合计不超过 0.3 ms。结果写进复用的 `byte[]`，经同一个 `Image.SetData` 后 `ImageTexture.Update(image)`，不每帧 new。
- **为什么不在着色器里查二维表**：关键帧在高度轴上不等距；sRGB 纹理的硬件过滤发生在解码之后的线性空间，绿和金之间会插出泥色；每个色号要采样 4 次。
- 调色板整张每帧上传：ImageTexture 只能整张更新，64 KB 每帧上传在 PC 上可以忽略。

**instance uniform**（每个着色器最多 16 个，不能是纹理、数组或矩阵；在 MultiMesh 上是整个节点共用一份）：

| 名称 | 内容 | 写入方 |
| --- | --- | --- |
| `fade_t0` | 抖动淡化的起始时刻（`fade_clock` 计），正负号区分淡入、淡出；0 表示不在淡化中（§2） | WP-02 |
| `retreat_bias` | 退光线偏置，只有宣邦楼穹顶的实例设为 6 m（§10.1） | WP-03，按实体 ID 设 |
| `shadow_proxy` | 植被投影代理打开噪声挖洞（§9） | WP-23 |
| `fog_exempt` | 地标在浓雾里的雾豁免系数，0–1，默认 0；只有八角穹顶与宣邦楼钟楼的实例设值（ART-SKYLIGHT §6.6） | WP-40，按实体 ID 设 |
| `precip_d` | 降水池：xyz 循环盒位移（C# 按 T12 累加、对 2h 取模），w 半边长 h（雨 18 m、雪 14 m） | WP-45b |
| `precip_v` | 降水池：xyz 雨丝方向（单位向量，含风与倾角），w 丝长（m） | WP-45b |
| `precip_k` | 降水池：x 降水类型（0 雨 → 1 雪），y 强度系数，z 角色（0 降水池、1 檐滴），w 保留 | WP-45b |

**逐顶点数据**（ENG-BUDGET §7 第 7 条）：COLOR RGBA8 = 色号、AO、标志、1 字节备用；UV2 或 CUSTOM0 = 窗号基址与窗格行列（§10.3）。建筑按块合并以后，楼与楼的区别只能放在顶点上，不能放 instance uniform。各字节的用法：
- "标志"字节含"室内"位（取调色板行 2、行 3），外围窗另有"跳过覆盖查表"位（§10.3），贴面贴花另有"贴花"位（§2）；各位的分配由 WP-04 登记；
- 备用字节按着色器区分用途：建筑是墙脚渐变的离楼基高度（0–2.55 m，1 cm 一级；墙脚渐变默认关，可留空，ART-STYLE §2.4），植被是冠内明度梯度的 t0，室内是灯照权重（WLD-INTERIORS §8.2），贴面贴花的面片是底面的色号（标志字节另记"贴花"位，§2）；
- AO 字节只给覆雪和窄凹处的阈值偏移用（ART-STYLE §2），不生成接触影；
- 外围 O1、O2 和截面上缘不论是否落在 `map_rect` 内，季节覆盖潜势都按面类别写进顶点（草 1、步道 0.5、车道 0.2、屋面按法线，WLD-OUTSKIRTS §4.5），由外围顶点标志选来源（与外围窗"跳过覆盖查表"同一位）；放在 VF-A 现有的哪个字节由 WP-47 与 WP-04 定，不新增顶点格式（§16）。

**需要重复铺贴的贴图**（修边件、斑驳、细节平铺、叶卡、枝条笔触）：Texture2DArray，每层一种。**不重复的内容**（题字、店招、替身）：2048² 图集。都经 Godot 导入，VRAM Compressed（ENG-BUDGET §7 第 5 条）。

### 1.6 排序与绘制顺序

- 不透明物体的绘制顺序交给引擎：有深度预通道，颜色通道的过度绘制接近 1，按材质排序比按距离排序更省。不用手工排大顺序。
- 全场 ShaderMaterial 实例尽量少，目标不超过 48 个（§16）：材质 RID 在排序键里，每多一种材质就多切开一段自动实例化。逐物体的颜色变化走顶点上的色号，不建新材质。
- 透明物只有：近景透明窗（同屏不超过 32 块）、水的透明变体（可选）、粒子、光晕、光柱、细尘和介质覆盖层。贴面贴花留在不透明通道（ART-MODELING §7.5，做法见 §2）；透明的 `toon_decal` 只作备选（§1.4），启用时彼此不重叠，按 `render_priority` 排在最前；加法混合的面片之间不讲顺序。
- 天空由引擎在不透明之后画，只填补没被遮挡的像素。

## 2 着色器与编译（WP-04、WP-05、WP-01）

**编译单位**：一个管线 = 着色器 × 内部版本（深度预通道、带 normal_roughness 的预通道、阴影、颜色通道）× 管线键：顶点格式、帧缓冲格式、剔除模式、图元类型，以及特化常量——MultiMesh 的格式与有无、方向光和点光的软影开关与采样数、深度雾、区域光等。

**引擎已经做的**：

- 4.4 起有 ubershader：第一次遇到某个组合时先用 ubershader 画，特化管线在后台线程编译，编完再换上（`PipelineCompilationsSpecialization` 记这一类，不卡帧）。DRAW 计数只在连 ubershader 都没有时才增长。
- 4.5 起导出可以开 shader baker，导出时预编译中间格式，D3D12 上加载约快 20 倍。它要用渲染设备，`--headless` 导出时不能烘焙，所以 G6 的导出在窗口模式下跑（ENG-GATES §2）。导出只发 D3D12 一份产物（§8）。
- 管线缓存（`rendering_device/pipeline_cache`）和着色器缓存存盘，第二次启动更快。但 D3D12 下引擎不建管线缓存（`pipeline_cache_create` 直接返回 false，实测 `user://vulkan` 为空），首次启动的编译耗时取决于显卡驱动自己的缓存；只有 Vulkan 能用上引擎的管线缓存。

**仍会卡顿的情况**：运行中第一次出现的"着色器 × 网格"组合；材质覆盖这类修饰；运行中途开启 MSAA、normal_roughness 缓冲、运动向量、分离高光；改动特化常量对应的灯光设置（软影开关、滤波档位）；打开 Environment 的深度雾；加入区域光。另外：改软影滤波档位会经 `set_default_specialization` 清空**全部**已编译管线（连 ubershader 的也清），之后在绘制时同步重编；FSR2 第一次开启冷启动卡 1.7 s；运行时新建 Shader 卡 175–331 ms（D3D12 的着色器格式转换）。所以这几项都不做成画面选项（§11.7）。

**我们的做法**：

1. **顶点格式只用登记的 4 种**，新增要主代理批准：
   - VF-A 块几何：压缩 20 B（位置、八面体法线与切线、UV、COLOR）；
   - VF-B 窗格：VF-A + UV2（窗号基址）；
   - VF-C 植被插片：VF-A + CUSTOM0–2（§9）；
   - VF-D 面片与粒子：VF-A 的子集（无切线时补默认切线，保持同一压缩格式）。
2. **MultiMesh 格式统一**（§1.2）；剔除模式只有 `cull_back`（闭合网格）和 `cull_disabled`（插片、水、剖切变体）两种，写在白名单着色器里，不在运行时改。
3. **扉页后面预热**（WP-01 的 `PipelineWarmup`；有开始页时在扉页插画后面，按 SYS-TITLE §1.1 第 3–4 步；`--title=0` 时在雾幕里）：
   - **三维**：从烘焙统计里汇总全部"着色器 × 顶点格式 × MultiMesh 与否 × 剔除模式 × 投影与否"组合，生成预热表；在相机前 2 m 摆一个预热阵列，每个组合一件小网格，放在第 1 级阴影范围内，让颜色、预通道和阴影三类管线都编到。倒影世界里另摆一组（它没有投影光，特化常量不同）。剖切变体、Compositor 的计算管线、天空一起预热。另有：`cine_cloud` 的云卡与镜头云幕（SYS-TITLE §9）、外围 O0–O2 的组合（WLD-OUTSKIRTS）、开放室内的组合（室内壳的 `toon_cut`、门厅卡、`toon_glass_near`，WLD-INTERIORS）、`fx_glow` 的三种光柱分支与细尘。
   - **二维**（UI-VISUAL §11.9、SYS-TITLE §1.1 第 4 步）：在扉页之下的预热层（layer 4）以正常不透明度画一张"预热样片"：`ui_panel`、`ui_screen` × {StyleBox 四边形、`Line2D`、`TextureRect`、位图文字、MSDF 文字、`CanvasGroup`、`BackBufferCopy`} 各画一次（画布着色器按绘制类型分特化）；`map_base` 的两种 `mode` × 两套主题各画一次（SYS-ATLAS §10）；开始页与放映层的全部组件各画一帧。界面着色器第一次出现那一帧会卡 18.4 ms（`PipelineCompilationsCanvas` +5，UI-VISUAL §11.9）。
   - **MSDF 字形**：导入时预渲染全部会用到的字（翻译表、点位名、地图标注、片名与标题卡文案自动收集），运行中不生成；现场生成约 8 ms/字（UI-VISUAL §11.8）。
   - **大页控件预建**：设置卡（含画面页约 120 个控件）、存档页、开始页菜单、制作人员页、大地图的卡片与左栏在扉页后面**预建一次并隐藏**（UI-VISUAL §11.9、SYS-ATLAS §10）。按需创建 159 个控件入树要 22–40 ms，所以不在打开时建（SYS-GRAPHICS §9 同此）。地图纹理的 `ImageTexture` 也在这时创建。
   - **覆盖所有可达的画面设置**（SYS-GRAPHICS §3）：预热表取"四个预设和所有单项取值能到达的组合"的并集——SMAA 和 FXAA 两套后期、移轴 1/2 和 1/4 两种分辨率的计算管线、描线的全部变体、光斑、2 级和 4 级 PSSM、FSR1 放大与原生两种缓冲配置。光柱与细尘、降水池、檐滴、云卡这类可开关、按天气或按状态出现的节点启动时就在树里（隐藏的节点也会被预编译），一起编到。雾幕类设置只重建缓冲、不换帧缓冲格式，所以不需要重新预热（§11.7）。
   - **何时散开**：等 SURFACE 和 MESH 计数连续 10 帧不增、Specialization 计数连续 30 帧不增、`PipelineCompilationsCanvas` 连续 10 帧不增，再撤掉阵列和预热层，转为实时（有开始页时，扉页以 1.2 s 交叉淡化成实时的 T0，SYS-TITLE §1.1 第 8 步；`--title=0` 时散开雾幕）；最长等 5 s，超时记警告并照常继续。扉页阶段停满 4 s 仍未转为实时才亮等待灯（SYS-TITLE §1.1）。
4. **TileBundle 的统计 JSON 里列出本格用到的组合**。流式管理器挂载前查预热表，查不到就是烘焙错误（G5、G8 报出），不在运行时临时编译。所以 §5 第 2 条里的"管线就绪"是静态可判定的。
5. **运行时只改值**：材质参数（`SetShaderParameter`）、instance uniform、全局参数。换材质只允许换成预热过的（剖切变体；可选的淡化变体，见本节"discard 与抖动淡化"）。
6. **门禁**（G16）：每次雾幕淡出之后（开场、换季、远距离跳转、传送、切图、读档、画面设置的雾幕）SURFACE 和 DRAW 计数不增，界面的 `PipelineCompilationsCanvas` 也不增，运行中新生成的 MSDF 字形为 0；"前推 24 小时""四季轮换""天气巡回""预设轮换""过场体验"跑完 DRAW 为 0（§12.4）；Specialization 只记录不判。MESH 计数由加载线程产生（加载网格时编译），不卡主线程，只记录。**计数基线**：`--title=1` 时从"标题页转为实时"那一刻算起；`--title=0` 时在开场雾幕淡出时（SYS-TITLE §11.2）。切图只预热新图新出现的组合（SYS-MAPS §4.2 的 Warm 步），之后同样不增。
7. **无界面模式不编译着色器**，着色器错误只有窗口模式才会暴露，所以 G5 要加一次短时间的窗口冒烟运行（ENG-GATES §2）。

**精度**：PC 上着色器按 32 位浮点计算。需要注意的只有：世界坐标在 ±1.02 km 内，单精度步长约 0.1 mm；`anim_time` 取 mod 480 后精度约 30 µs；`clock` 在 0–24 内足够；不用内置 `TIME`（默认 3600 s 回绕一次）。

**分支**：基于全局参数、instance uniform 或 flat varying 的分支几乎不花代价；逐像素分叉的分支里不做纹理采样，非采不可时用 `textureGrad`。阴影通道里 `IN_SHADOW_PASS` 是全局标志，分支同样几乎免费。

**light() 的开销**：light() 对每个像素的每个光源各执行一次，顺序是方向光 → 点光 → 聚光。大部分像素只有一盏主光；重点 OmniLight3D 不超过 32 盏，light() 的点光分支只做"灯照色 − 影色"的差值，不采样纹理。颜色一律不用 `LIGHT_COLOR`（它含能量 × π）。保持 `force_vertex_shading = false`，否则 light() 不执行。

**discard 与抖动淡化**：

- 淡入淡出一律在 `toon.gdshaderinc` 里做：片元按 4×4 Bayer 阈值和淡化进度比较，低于阈值就 discard。进度 = clamp((`fade_clock` − |`fade_t0`|) / 0.25 s)，主线程只在开始淡化时写一次 `fade_t0`，不逐帧写。父子格用互补的阈值（父用 1 − 进度），过渡期两者都画，结束后父格 `InstanceSetVisible(false)`。同时淡化的格超过 50 个时直接切换，不淡化。
- 不用 visibility range 自带的淡化：Self 和 Dependencies 两种模式都会强制走透明管线，normal_roughness 缓冲里就没有这些像素，描线会断。
- 代价：含 discard 的着色器在深度预通道里不能提前写深度。我们的预通道本来就要跑片元代码写 normal_roughness，所以只多一次 Bayer 比较；P0 对比"单个着色器带淡化分支"和"淡化变体（预热过，只给淡化中的实例换材质）"两种写法，后者会切开自动实例化，前者是默认（§16）。
- 其余允许 discard 或 alpha scissor 的只有：植被插片、替身、投影代理的噪声挖洞、冬季浅滩挖洞、倒影代理丢弃水面以下的片元（ENG-BUDGET §7 第 8 条）。

**法线与倒角的着色**（`toon.gdshaderinc`，WP-04；风格见 ART-STYLE §2.6，法线怎么生成见 ART-MODELING §7.2、§7.6）：

- **法线约定**：建筑平面一律硬边，同一平面上所有模块的法线逐位相同：由平面方程算，不由量化后的三角形叉积算；建筑模块不随机旋转。曲面一律平滑法线，段数只按剪影误差定（ART-FIDELITY §4）。两阶明暗会把几度的法线误差放大成碎影（低太阳下接近掠射的侧立面最明显）；下面的倒角带判定也要求大面三个顶点的法线完全相同。
- **倒角盒**：单段斜面加自定义法线：大面取面法线，倒角带两侧的顶点与相邻大面共用、各取那一面的法线，带内插值过渡；24 个顶点、44 个三角形。WP-09 的内核提供倒角盒和倒角扫掠的构造器，统一输出这种法线。`SurfaceTool.GenerateNormals` 属于 GodotSharp，gen 里不用。
- **倒角提亮**：两阶明暗下受光色与 N·L 的大小无关，两面都受光时倒角带和大面同色，阳角只剩一条描线。toon 在 fragment() 里按法线曲率判出倒角带，给它提亮一阶，不需要顶点数据：

  ```glsl
  // fragment()：视空间里算；κ 是法线随位置的变化率（1/m）。要在写 NORMAL 之前算
  float kappa = length(fwidth(NORMAL)) / max(length(fwidth(VERTEX)), 1e-5);
  float bevel = smoothstep(2.0, 4.0, kappa) * mat_bevel_on;   // κ 大于约 3/m 判为倒角带；mat_bevel_on 来自 mat_table 行 3
  ```

  大面三个顶点的法线相同，κ 正好是 0；30 mm 倒角带的 κ 约 37/m；半径 10 m 的圆楼只有 0.1/m。曲面道具也会被判成倒角带，所以只给 `mat_table` 行 3 开了提亮的材质（`ToonFx.BevelLift`：硬质建筑材料和道具的平直件）。提亮的幅度读同一行：受光 +0.05 L、影里 +0.02 L（初值，§16），影里也略提，免得两面都在影里时阳角消失。做法是在 fragment() 里改 EMISSION（影色）和传给 light() 的"受光 − 影"差值，light() 不变，`NORMAL` 不改。倒角窄于约 1.5 px 以后由描线的凸边亮线接替（§10.2）。
- **只改明暗、不改线**：倒角提亮、修边贴图和铺装的假倒角、地标曲面的手调交界都不写 `NORMAL`，建筑和道具也不用法线贴图（ART-STYLE §6.5）。铺装除 K1 的英雄机位和热点簇放几何铺装块以外，K2 到 K5 一律靠着色器的逐块明度和假倒角（ART-FIDELITY §0.3）。这类修正的量在 fragment() 里算好写进 varying，light() 的方向光分支读它改 N·L 或受光差值。varying 可以在 fragment() 里赋值、在 light() 里读，不能在 light() 里赋值。`NORMAL` 会写进描线读的 normal_roughness 缓冲，改了它就会多出线或让线和明暗对不上。
- **解析法线**：球、柱、胶囊类的独立实例和 MultiMesh 道具（灯头、球形灌木、护柱、灯杆）按中心或轴逐像素算法线，明暗交界与段数、LOD 都无关，降段只改剪影。vertex() 里取视空间的中心 `MODELVIEW_MATRIX[3].xyz` 和轴 `normalize(mat3(MODELVIEW_MATRIX) * vec3(0.0, 1.0, 0.0))`，经 `flat` varying 传给片元（MultiMesh 里 vertex() 的矩阵已含实例变换，片元里没有，§9）；片元里球取 N = normalize(VERTEX − c)，柱取 d = VERTEX − c、N = normalize(d − dot(d, a) a)。结果写回 `NORMAL`：它进描线缓冲，圆件因此没有内部折线，这正是要的。`ToonSpec.Normal` 取 `AnalyticSphere`、`AnalyticCylinder`（ENG-ARCH §3），方式写进 `mat_table` 行 3；合并块里的件没有自己的中心，不适用。
- **贴面贴花**（ART-FIDELITY §6，做法见 ART-MODELING §7.5）：贴面片沿表面法线外推 1–2 mm，按图样外轮廓裁成多边形（`BitMap.OpaqueToPolygons`，资源管线离线生成，gen 只读结果），并进所在块的网格，走 `toon_static`；顶点的标志字节记"贴花"位，备用字节带底面的色号（§1.5），片元按图样的 alpha 在底色和贴花色之间混合，底面的世界坐标斑驳用同一个函数算，边缘没有接缝。不用 discard（ENG-BUDGET §7 第 8 条），留在不透明通道、可以并块，不切开自动实例化；季节落叶堆、花瓣带也用这一套，颜色读季相行；线类记"不描"；不投影：在 vertex() 里按"贴花"位、在 `IN_SHADOW_PASS` 时把顶点塌到一点。不用引擎的 Decal 节点：它改的是 ALBEDO（本项目 ALBEDO 固定为 1，贴花色只会乘进受光项），带 ORM 贴图时会覆写存着材质 ID 的粗糙度，也不走调色板，还占分簇元素。

## 3 填充率与带宽（WP-05、WP-23）

### 3.1 渲染目标格式

Godot 的内部缓冲格式由引擎决定（Forward+ 颜色 RGBA16F、深度优先 D32_SFLOAT_S8_UINT、normal_roughness RGBA8）。我们能控制的只有"不打开额外的缓冲"：不开 MSAA、TAA（运动向量）、SSR、SSAO、SSIL、SDFGI、分离高光。物体与材质的区分编进 ROUGHNESS 的 7 位材质 ID（§10.2）。渲染目标的显存公式见 §7。

### 3.3 过度绘制

- **深度预通道全场开着**（Forward+ 默认，描线也离不开它）。颜色通道的过度绘制因此接近 1；代价转到预通道里被遮挡的像素，以及顶点着色要跑多遍（预通道、颜色通道、各级阴影）。
- **叶片卡**：按 alpha 轮廓裁成 6–8 个顶点（`BitMap.OpaqueToPolygons` 生成轮廓），省 30–50% 的像素。离相机 1.5 m 以内的卡在 vertex() 里塌缩，避免钻进树冠时糊满屏幕；距离用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`（ENG-BUDGET §7 第 15 条）。
- **大面积透明**：
  - 玻璃靠室内映射按不透明物体绘制。只有开放楼室内一侧的朝里玻璃（5 m 内）和开放入口的门玻璃才用 `toon_glass_near`（透明，同屏不超过 32 块，WLD-INTERIORS §4、WLD-INTERIORS §8.3）；关着的楼一律是窗卡和门厅卡（WLD-INTERIORS §5）。
  - 雾在材质里用共用雾函数算，写进 `FOG`；材质一写 `FOG`，引擎自带的雾和体积雾就整段跳过。
  - 水面默认不透明，只画一层。
  - 降水池的雨丝走透明管线，但像素覆盖率不超过 4%（雷雨 6%，SYS-WEATHER §3 第 3 条），线宽不超过 1.5 px，过度绘制可以忽略；雪花的屏幕覆盖由 P0 用 Quad Overdraw 看一次。
- **指标**：E1–E3 视角下，颜色通道平均每像素着色不超过 1.3 次；预通道平均不超过 2.5 次、峰值不超过 6 次（初值，§16）。
- **怎么测**：内置的 Overdraw 调试视图会忽略自定义着色器；不透明材质也不能在运行时改成加法混合来计数。所以用 RenderDoc 的 Quad Overdraw 叠加层，按通道看（Vulkan 抓帧，§12.3）。

### 3.4 动态分辨率

- Godot 没有子区域渲染的接口，改 `Scaling3DScale` 会重新分配缓冲，所以渲染比例不做运行时连续调节。
- **做法**：渲染比例随画面预设（SYS-GRAPHICS §1），按预设的"内部像素上限"换算到最接近的 FSR1 比例（0.77 / 0.67 / 0.59 / 0.5，AMD 推荐的比例）：例如极致在 1440p 原生、4K 用 0.77，精致在 1080p 原生，均衡 0.67，流畅 0.59。只在设置界面里改 `Viewport.Scaling3DMode` 和 `Scaling3DScale`，属雾幕类：重建 3D 渲染缓冲（4K 下 11–25 ms，不编译），点"应用"后在 0.3 s 雾幕里生效（§11.7、UI-NAV §4.6）。
- 不用 FSR2：它要运动向量和历史帧，会让描线和一拍二拖影；第一次开启还要冷编译 1.7 s。也不提供 Nearest。
- 运行时降负载只调"不重编译、不重新分配"的参数，见 §11.3 的阶梯；自动调节不动渲染比例。屏幕误差按内部渲染高度计算，所以低比例的预设自动选更粗的级别；描线线宽也按内部分辨率换算（§10.2）。

## 4 剔除（WP-02、WP-33、WP-07）

| 层级 | 做法 | 开销 |
| --- | --- | --- |
| 格 | 自写四叉树的视锥测试，决定每格挂哪一级（流式管理器）；四叉树覆盖当前图的包围盒 `MapDescriptor.Bounds`（清水河含外围，约 ±1.5 km，WLD-OUTSKIRTS §3.5），格数按包围盒计 | < 0.05 ms |
| 实例 | 引擎对每个实例做视锥剔除；实例数超过 1000 时多线程剔除 | 计入渲染 CPU |
| 块 | 每块 1–8k 三角形、不超过 16–24 m，引擎逐块做视锥和遮挡剔除 | — |
| MultiMesh | 整体剔除，用 `CustomAabb`；所以按"格 ∩ 楼"拆分，不跨格 | — |
| 距离 | visibility range：按到实例包围盒中心的距离；HLOD 用 visibility parent；`fadeMode` 为 Disabled，用边距做回差 | — |
| 屏幕尺寸 | 小物体的 `visibility_range_end` ≈ 620 × 包围球半径 × 细节系数，即投影半径约 1.5 px 处（按 ART-FIDELITY §1 的换算） | — |
| 介质 | 镜头在地下时，相机 `CullMask` 去掉地上各层（被隐藏的层，影子也随之消失）；在开放楼内时，开启室内遮挡体、隐藏本楼的外包遮挡体（WLD-INTERIORS §6.3） | — |
| 阴影 | 投影物控制：`ShadowsOnly` 代理、visibility range（对投影物同样生效）、主光的 `ShadowCasterMask`（§10.1） | — |
| 遮挡 | 引擎内置的 OccluderInstance3D（下文） | 计入渲染 CPU |

**图层登记**（`VisualInstance3D.Layers`，相机 `CullMask`、主光 `ShadowCasterMask`、点光 `LightCullMask` 都按它；归 WP-01，需要新层向主代理申请）：

| 层 | 内容 | 相机可见 | 投影 |
| --- | --- | --- | --- |
| 1 | 地上的建筑、道具的可见几何 | 是 | 是 |
| 2 | 地面、道路、铺装 | 是 | 否（地面不投影）；点光的 `LightCullMask` 排除这一层，避免和灯光表的光斑叠两遍 |
| 3 | 小件投影物（栏杆、灯杆、长椅、单车等，高 ≥ 0.25 m） | 是 | 是；画面设置"细小物件投影"关时从 `ShadowCasterMask` 里去掉这一层，只在雾幕里切（SYS-GRAPHICS §3）；自动调节不动它（§11.3） |
| 4 | 植被可见几何 | 是 | 否（投影走第 5 层的代理） |
| 5 | 投影代理（`ShadowsOnly`：植被代理、远处体量代理、外围 O1 的盒体投影代理） | 是（引擎要求投影物在相机可见层上；代理本身不画） | 是 |
| 6 | 草、地面落叶、粒子（含降水池和檐滴）、光晕、光锥、光柱 | 是 | 否 |
| 7 | 地下内容（管沟、树根、楼基） | 只在地下 | 否 |
| 8 | 室内内容（15 处开放室内、简化室内、I0 兜底层，WLD-INTERIORS） | 是 | 是（室内只有墙和楼板投影，家具、踢脚、窗框内侧、灯片不投影，WLD-INTERIORS §8.3） |
| 9 | 水面与水下 | 是 | 否 |
| 10 | 远景环、雾海、箱庭截面、外围 O2 雾中楼顶、流畅档的 O2 卡片环（WLD-OUTSKIRTS §4.2） | 是 | 否 |

**遮挡剔除**（WP-33，"遮挡体生成与可见性调优"）：

- 不用离线可见集：镜头能穿墙；几何经常在变；可见位集太大。
- **原理**：引擎在 CPU 上用 Embree 光栅化遮挡体，缓冲像素约为 512 条光线 × 逻辑核数，并夹在视口像素的 1/1024 到 1/4 之间（28 线程约 160×90，8 线程约 85×48）。被遮挡物的包围盒必须被完全挡住才会剔除；移动或增删遮挡体会重建 BVH；遮挡剔除不作用于阴影通道。
- **做法**：
  - 每栋楼的轮廓向内缩 0.5 m，拉伸到檐口高度，生成 ArrayOccluder3D（清水河约 7k 三角形）；遮挡体**每图一份**，随本图的 L4 在切图的 Mount 那一步加载、Unload 时释放（SYS-MAPS §4.2）；同一张图里仍常驻不动，避免流式增删引起 BVH 重建。可选：外围 O1 第一排楼生成遮挡体，约 1k 三角形，随 L4 常驻（WP-33，WLD-OUTSKIRTS §11.2）。
  - 离地 60 m 以上关闭 `Viewport.UseOcclusionCulling`（回差 55/65 m）。镜头在**开放楼**内时不关闭，而是显示室内遮挡体、隐藏本楼的外包遮挡体（楼内看出去的室外绘制由 400 次降到 51 次，WLD-INTERIORS §6.3）；室内遮挡体随室内包加载，隐藏比移动便宜，不触发 BVH 重建。镜头在**关着的楼**里（旁观穿墙）或离外墙不到 1 m 时仍关闭，否则这栋楼自己的遮挡体会把窗外的东西全挡掉。
  - 室内内容（第 8 层：开放室内、简化室内、I0）一律 `InstanceGeometrySetFlag(..., IgnoreOcclusionCulling, true)`：它们在外包遮挡体里面，从窗外看会被误剔。
  - 遮挡不作用于阴影通道：室内只有墙和楼板投影（WLD-INTERIORS §8.3）。
  - 块的粒度必须保持在 16–24 m 以内，64 m 的 MultiMesh 很难被整体挡住（§1.1）。
- **收益**：行走和街道高度的视角收益大，高空视角很小；GPU 方面的收益主要来自少画的绘制和顶点，因为预通道已经挡掉了大部分像素。CPU 开销与收益是否划算，P0 在本机和一台最低配置的笔记本上各测一次。

## 5 流式加载（WP-02）

**级别分工**：全部级别都是烘焙产物，**本机烘焙**（D-165）只有四类：L0–L3 的 TileBundle（含外围 O0、O1 和 I0 兜底层）、碰撞级 C、室内包与碰撞包、平面地图底图。它们不随包发，在玩家机器上由与开发烘焙器同一个 `Build()`（ENG-ARCH §3、§4）生成：第一次需要时生成，按"格 × 级"存成自定义的格包二进制格式 `.tbin`（整体 zstd），写进本机缓存，之后照常从缓存读（缓存、查找顺序和调度见"本机缓存"与"本机烘焙的时机"两段）。随包发的只有启动集 `boot_<地图>.pck`（L4、遮挡体、外围 O2 常驻块与卡片版、常驻低精度地形高度、需要 GPU 才能出的产物如 L2 半八面体替身图集，以及全图常驻的小数据：派生数据纹理、2 m 分类栅格与 8 m 安全点网格、声学网格、声景场与发声器表、导航图，§8）。开发和 CI 的无界面烘焙器 `--bake=all` 照旧，用来出参考哈希、给门禁预烘缓存；`--gen=live` 只生成、不读也不写缓存。启动集在 `res://` 里，仍用 `.res`。`Build()` 在工作线程上跑，内核在同一个工作线程上用 `RenderingServer.MeshCreateFromSurfaces` 建网格（Forward+ 下可以在调用线程上创建 GPU 资源），主线程照常只挂载。

**TileBundle**（自定义 Resource，WP-02 定义；启动集里存成 `.res`，本机缓存和开发共享缓存里存成 `.tbin`，见"本机缓存"）：

- 各块的 ArrayMesh 和它们的变换；
- 本格的 MultiMesh（静态部分）；
- 统计 JSON：顶点数、三角形数、表面数、字节数（含各级 LOD 索引）、各包归属、用到的管线组合（§2）、包围盒。
- 遮挡体不在 TileBundle 里，每图一份常驻、在启动集里（§4）；碰撞是独立的碰撞级 C（见本节"碰撞级 C"）。开放室内另成室内包（见本节"室内包"），关着的楼的 I0 兜底层放在 L0 的 TileBundle 里、默认隐藏（WLD-INTERIORS §5.3）。

**表面打包**：烘焙器按引擎的表面字典自己打包字节（`ARRAY_FLAG_COMPRESS_ATTRIBUTES`，20 B/顶点），`aabb` 一律写本格的统一格包围盒，让相邻格的量化网格对齐，避免接缝。64 m 格的位置量化步长约 1 mm。打包结果与引擎 `AddSurfaceFromArrays` 的输出由门禁比对（§12.5）。启动集的 `.res` 用哪个接口把打包好的表面存进 ArrayMesh，P0 原型定；`.tbin` 里存的就是这份打包字节，读入后经 `RenderingServer.MeshCreateFromSurfaces` 建网格。

**加载**：

- 接口：`res://` 里的资源（启动集、音频等）用 `ResourceLoader.LoadThreadedRequest(path, "", useSubThreads: false, ResourceLoader.CacheMode.Reuse)`，每帧用 `LoadThreadedGetStatus` 轮询，完成后 `LoadThreadedGet` 取回。`user://` 下的本机缓存不经 ResourceLoader（"本机缓存"一段），由工作线程任务读入。两类请求都**没有优先级，也没有取消**，所以：
  - 队列由主线程自己维护（预分配的二叉堆），`LoadThreadedRequest` 同时在途不超过 3 个；读本机缓存的工作线程任务与 `LoadThreadedRequest` 分开计在途，上限与生成任务相同，都是 max(2, 逻辑核数 − 2)；
  - "取消"就是不发出请求；在途的照常完成，回来已过期就按 LRU 处理或直接 `Dispose()`。
- 来源：文件在启动集里发 `LoadThreadedRequest`；在本机缓存里发读缓存任务；都没有时改发生成任务（见"本机烘焙的时机"）。生成完把同一个 TileBundle 直接交给挂载。读缓存任务和生成任务都不占 `LoadThreadedRequest` "在途不超过 3 个"的名额。
- GPU 上传：网格在加载线程上创建 GPU 资源，走 4.4 起的传输队列，主线程不承担上传。暂存缓冲上限 128 MB（`staging_buffer/max_size_mb`），超出时 GPU 会停下来等；单格不超过 8 MB；读缓存任务和生成任务的工作线程都会调 `MeshCreateFromSurfaces`，同时在途数受上面的上限约束，暂存缓冲是否顶满由 P0 实测（ENG-BUDGET §10）。
- 渲染线程模式保持默认的 Safe。Separate 模式官方标注为实验性，"不建议用于生产"。

**帧内两步**：

| 步 | `FramePriority` | 做什么 |
| --- | --- | --- |
| 选级与请求 | `Stream` = −500（WP-02、WP-43） | 在介质（−600）和声学（−590）之后、各系统（0）之前：四叉树选级、调度重排、发出加载请求、轮询 `LoadThreadedGetStatus`；切图期间（Unload 到 Mount）不发新请求（SYS-MAPS §9） |
| 挂载 | `StreamMount` = 900（WP-02、WP-43） | 排在 SYS-TIME §1 各步、默认优先级的系统（0）、音频（50）和界面（100）之后，存档（1000）之前：取回已完成的请求、挂载、碰撞格入树、可见性切换，在下面的预算内分帧 |

**挂载**（主线程，`FramePriority.StreamMount`）：

- 按 §1.2 的顺序调用 RenderingServer；每帧最多激活 1 格，主线程不超过 1.0 ms（含碰撞格入树、室内包的一层、音频素材加载完成后的建节点，SYS-AUDIO §15）。计时用 `Campus.Kernel.Frame` 提供的帧计时接口（ENG-ARCH §2 只允许这几个命名空间直接计时）。
- 新挂上的块以淡化进度 0 出现，0.25 s 淡入（§2）。
- 新挂上的室内房间与门户下一帧才在声学里生效（SYS-AUDIO §20）。

**调度**：

- 主线程每 100 ms 重排一次。排序依次按：粗级优先；需要时刻 t = (距离 − 切换半径) / max(接近速度, 1 m/s)，越早越优先；视锥加权只用于 L2 及以上（视锥放大 1.3 倍）。
- L0、L1 按 360° 环带加载，因为旁观者模式 0.3 s 就能转身。
- 四个队列，每次空出在途名额时按顺序取：碰撞队列 → 已知目的地队列 → 室内入口预取 → 常规队列（WLD-INTERIORS §6.2）。在途请求一律不打断。

**碰撞级 "C"**（SYS-WALK §13）：

- 与渲染的 L0–L4 无关。本机烘焙（D-165），与可见几何用同一套参数生成；每格一个碰撞 `.tbin`（只存形状参数），存在本机缓存里，不在 TileBundle 里。只在行走模式下加载；旁观模式下只保留全局常驻数据（2 m 分类栅格、8 m 安全点网格、低精度地形高度，约 0.9 MB 的 C# 数组；都在启动集里）。
- 图元映射按 SYS-WALK §13：地面 HeightMapShape3D 65×65；墙段、栏杆、绿篱、护岸用 BoxShape3D；台阶、坡道、桥面用斜 Box 或 8 顶点 ConvexPolygonShape3D；树干、灯杆用 CylinderShape3D；室内楼板凸分解。
- 读缓存的工作线程任务读入碰撞 `.tbin`，解压、校验哈希后由形状参数创建形状资源（在工作线程上建，形状 RID 表是否线程安全由 P0 验证（§16）；不安全时在主线程建形状）。
- 主线程入树（`kernel/collision`，WP-43）：每格按表面类型建几个 StaticBody3D，类型写进 `CollisionLayer` 和 meta；**入树之前**用 `CreateShapeOwner` 和 `ShapeOwnerAddShape` 加完全部形状，再 `AddChild`（Jolt 对已在空间里的 body 逐个加形状是 O(n²)）。每帧最多入树 1 格，不超过 0.3 ms，计入挂载的 1.0 ms。
- 加载半径 96 m，卸载半径 160 m，两者之差就是回差。角色所在格及 8 个邻格排在最前，其余按到角色的距离排；用行走速度外推（上限 12 m/s），不按视锥加权。
- 最坏延迟 = 一个在途的渲染请求加上碰撞请求本身；缓存里没有这一格时再加上它的生成（碰撞一格 ≤ 50 ms，初值，P0 实测，§16）；行走时不会离开已就绪的格太远，§5 第 5 条兜底。碰撞生成任务排在生成队列的最前（下文"本机烘焙的时机"）。

**室内包**（WLD-INTERIORS §6；WP-51 生成，WP-02 调度）：

- **整体加载、按层显示**：每栋开放楼、每层、每个细节级一个 `.tbin`（整体 zstd），另有每层一个碰撞包，都是本机烘焙产物（D-165），存在本机缓存里（`user://cache/bake/qingshuihe/<ABI>/…` 下按室内包的键排列，§8、SYS-MAPS §2.4），不随包发。缓存里没有时按需生成，室内一层 ≤ 100 ms（初值，P0 实测，§16）；没就绪时按第 6 条（进楼衔接）原有的未就绪规则处理。镜头所在层显示 IF（全精度），±1 层只显示 IS（墙、楼板、吊顶、楼梯、灯片、门）；在楼梯间或中庭里相邻两层都用 IF；其余开放层已加载但隐藏，切换只改可见性（WLD-INTERIORS §6.1）。
- **触发**：行走时离任一开放入口 ≤ 25 m，预取这栋楼开放部分的全部 IF、IS 和碰撞（入口用室内索引的 `EntrancesNear()` 查）；旁观时镜头沿当前速度 1 s 内会进入外包棱柱，预取预计入楼高度所在层 ±1，速度超过 15 m/s 不加载 IF，只显示 I0 加介质雾；"看进去例外"：图书馆 P1–P3 路径和品·悦吧大玻璃，镜头在 150 m 以内时加载 IS 层（WLD-INTERIORS §6.2）。
- **挂载**：一层 IF 约 270 个实例，主线程 0.31–0.45 ms，每帧挂一层，计入挂载的 1.0 ms；新挂上的层抖动淡入 0.25 s。
- **碰撞**：开放部分整体入树，按房间簇拆成每个 ≤ 250 个形状的 body，每帧入树 1 个、≤ 0.3 ms；一栋楼 ≤ 5 千个形状，约 0.3 s 入完。楼里的自动门（能进的房门、开放路线上的楼梯间防火门）另有运行时门体：每扇一个 `StaticBody3D`，不进烘焙碰撞，只切换 `CollisionLayer`、不增删形状，门扇转动期间没有碰撞、关好以后才有；门扇转动只改实例变换，不加绘制调用；每层 ≤ 40 扇、同时在动的 ≤ 4 扇，状态机主线程 ≤ 0.02 ms（WLD-INTERIORS §4.1）。
- **驻留与淘汰**：出门后保留 20 s 或直到离楼 40 m 以外，再按字节 LRU 淘汰；同时最多驻留 2 栋，显存 ≤ 60 MB（WLD-INTERIORS §6.1）。
- **关着的楼**：旁观镜头 1 s 内会进入时加载简化室内（镜头所在层 ±1），15 m/s 以上只显示 I0 加雾；行走模式下永远不加载（WLD-INTERIORS §5.3）。
- **加更多楼**：在开放清单里加一项、注册一个 `IInteriorProvider`（或给生成器加一条楼型配置），本节的调度、缓存键和不变量都不用改（WLD-INTERIORS §14）。

**外围**（WLD-OUTSKIRTS §4.2、WLD-OUTSKIRTS §11）：

- **O2 雾中楼顶**常驻，在启动集里（随包发）：按方位合并成不超过 16 块，随 L4 在 Mount 那一步同步加载，不参与淘汰（§5 第 1 条）；流畅预设下退化为一圈卡片，两者都烘好、都在启动集里，运行时只选用哪一个（WLD-OUTSKIRTS §11.4）。
- **O1 外围街区**流式：格子只有 L1–L4，只有临街店面那一层裙房有 L0；与校内共用每帧 1.0 ms 的挂载预算和同一套 LRU。L1–L3 和 L0 本机烘焙，L4 在启动集里。
- **O0 街道带**按校内的格子流式（L0–L1 两级），本机烘焙。
- 外围的 Overpass 快照到 `outskirts.json` 这一步仍在开发期离线做，`outskirts.json` 随包发。外围的生成代码放在随包程序集里，由 `IBakeService` 调用（WP-47）。
- 外围没有碰撞（行走到不了），已知目的地预取和碰撞级 C 不受影响。

**已知目的地预取**（UI-NAV §7.5）：路径已知的移动不靠速度外推猜去向，而是直接把目的地插到常规队列之前。

- **触发**：点击点位，或在点位上悬停 150 ms；旁观切行走时，落点一算出来就触发（SYS-WALK §5）。另有：
  - 平面地图传送：传送卡打开时，或在标记上悬停 300 ms、在目录项上停 150 ms 时（SYS-ATLAS §7.4）；
  - 读档：恢复的位姿（含行走的脚位）一算出来就触发（SYS-SAVE §5.1）；
  - 切图：Mount 时的出生点（SYS-MAPS §4.2）；
  - 标题页转为实时时：继续的终点各环（行走存档加碰撞格）、新游戏的整条路径（每 0.25 s 一个采样点，按该点的镜头速度放宽屏幕误差，按需要的时刻排序）、P00 各环（SYS-TITLE §10）。
- **内容**：目的地的 L0+、L0（40 m 环）、L1（150 m 环）、L2（400 m 环）、室内包与室内碰撞（目的地是室内点位、室内门口，或位姿在开放楼 25 m 以内时，WLD-INTERIORS §6.2）；行走模式下再加碰撞格（进碰撞队列）。
- **沿途**：按弧线采样，把途经的 L1 格排在目的地之后。
- **到达保证**：到达帧要求目的地各环就绪，否则雾幕多停一会儿，最多 1.2 s；行走模式下碰撞格未就绪时不开启物理（§5 第 5 条）。例外：
  - **读档**：就绪之前不撤雾幕、也不结束继续过场；等待超过 1.5 s 亮三颗等待灯（UI-VISUAL §7.10），不设硬超时；行走模式仍受 §5 第 5 条保护；
  - **切图**：不设 1.2 s 上限，超过 5 s 写日志（SYS-MAPS §4.2）；
  - **过场**（SYS-TITLE §10）：Title 状态下预取的格在 LRU 里钉住，Entering 结束后解除；新游戏开播前 15 s 的路径没就绪时停在云卡全遮里等，最长 2 s；播放中每帧检查前方 3 s 的格，没就绪就把播放率降到不低于 0.85，仍不够时显示最近的已就绪祖先（§5 第 2 条）并记一条"过场流式欠账"；终点沿用 1.2 s（行走 1.5 s）的到达保证。过场和跳转一样豁免 ENG-BUDGET §4 的速度律。
- **取消**：跳转被打断或改道时（UI-NAV §7.4），删掉旧目的地未发出的请求，在途的照常完成；同一时间只保留一个目的地。
- 跳转帧、传送帧不计入自适应画质的统计（§11.3）。

**音频素材**（SYS-AUDIO §15）：全部后台加载，主线程只做"加载完成后建节点"，计入挂载的 1.0 ms。环境床按"当前季节 + 目标季节"常驻，其余季节在换季雾幕前 5 s 用 `LoadThreadedRequest` 预载；开放室内的房间底噪跟着本节"室内包"的入口预取走。声学网格、声景场与发声器表（`ambience.bake`）是全图常驻数据，与 2 m 分类栅格同类：开发期烘焙，与 `precip_occ` 同一个烘焙挂点（WP-53c、53b 的算法），随启动集 `boot_<图>.pck` 发布（D-165），不在本机烘焙、不进 TileBundle，首帧之后空闲时加载，烘焙的缓存键只依赖几何。

**确定性**：

- 种子 = `StableHash.Hash32(世界种子, featureId, partId, 用途)`，与生成顺序无关（ENG-ARCH §2）。凡是"放不放、选哪种"的判断，一律用整数哈希；坐标计算里的超越函数一律用 `DetMath`（见下），结果量化后再用。
- 跨格的要素按锚点归属唯一的一格，格子包围盒相应外扩。
- `Build()` 的输入里没有世界状态（钟点、季节、天气、观察模式），也没有画面设置，所以改时间、换季、换天气、切换模式、改画质都不会让任何烘焙产物失效（ENG-BUDGET §7 第 11、18 条）；缓存因此跨存档、跨画质共用。
- **跨机器逐字节一致**（D-165）：同一 ABI 下，本机烘焙的产物与 CI 参考烘焙逐字节一致，按每个文件的内容哈希比，不按 C# 侧的几何内容比。存盘时子资源的 ID 可能随机生成，所以烘焙时给每个子资源写确定的 `ResourceSceneUniqueId`，让文件字节稳定。
- **gen 和烘焙子任务里禁用的 API**（G4 的 BannedApiAnalyzers 执行）：
  - `Math`、`MathF` 的超越函数：`Sin`、`Cos`、`Tan`、`Asin`、`Acos`、`Atan`、`Atan2`、`Exp`、`Log`、`Log10`、`Pow`、`Cbrt` 等。它们走系统数学库，结果可能因 Windows 版本而异，一律改用契约里的 `DetMath`（WP-00 提供的纯 C# 实现，有单元测试的误差界，ENG-ARCH §3）。`Math.Sqrt`、加减乘除和 `Math.Round` 是 IEEE 精确运算，可以用；
  - `System.Numerics.Vector<T>` 和 `System.Runtime.Intrinsics`（向量宽度随 CPU 变）、`Math.FusedMultiplyAdd`、`TensorPrimitives`。
- **门禁**：G6 在集中测试时跑 `--bake-verify`：用导出的 release 版在空的临时缓存目录里烘，把逐文件哈希与随包的 `bake.manifest.json` 比对，有一个不同就不过；同时做全图逐字节比对（ENG-GATES §2）。

**缓存键**：`ABI/地图/数据哈希/模块@代码哈希/级别/tileId`（数据哈希只算本图里影响 `Build()` 的输入，SYS-MAPS §2.4）。ABI 含引擎版本和顶点格式版本。tileId 是整数：`(lod<<20)|((ix+512)<<10)|(iz+512)`。碰撞级在缓存键里记为 "C"，在整数 tileId 里级别位取 7。室内包：`ABI/地图/数据哈希/interior.<提供者>@代码哈希/featureId/storey/detail`，碰撞的细节位记 "C"（WLD-INTERIORS §6.1）。碰撞级 C、平面地图底图也各按这套键。代码哈希包含模块的全部共享生成依赖（契约、部件内核与部件包、室内共享层、世界模型），算法以 ENG-ARCH §4 为准。开发态和 CI 的共享缓存放在仓库外的目录（`UESTC_BAKE_CACHE`，ENG-ARCH §4）。

**本机缓存**（D-165；服务是 `IBakeService`，`[KernelService]`，WP-02，契约见 ENG-ARCH §3）：

- **位置**：`user://cache/bake/<地图>/<ABI>/…`（导出版在 `%APPDATA%` 下），其下按本节"缓存键"一段的键排列。`--bake-cache=<绝对路径>` 覆盖；开发态默认指向 `UESTC_BAKE_CACHE`，所以开发与玩家走同一套逻辑。
- **格式**（安全规则）：本机缓存不用 Godot 资源格式。`user://` 下的 `.res`/`.tres` 能嵌脚本、读入即执行（SYS-SAVE §8 的威胁模型），所以 `ResourceLoader`、`ResourceSaver` 一律不得读写 `user://`，不设例外。缓存用自定义的格包二进制格式 `.tbin`：头（魔数、格式版本、缓存键、长度）+ 各表面的顶点与索引缓冲（与"表面打包"同一份打包字节）+ 实例变换 + MultiMesh 缓冲 + 碰撞形状参数 + 统计 JSON，整体 zstd（`FileAccess.OpenCompressed` 或 .NET 的 zstd）。解析器逐段做边界检查，长度超限就拒收。开发共享缓存（`UESTC_BAKE_CACHE`）和 `--bake=all` 也写同一种 `.tbin`。
- **读**：工作线程上读文件 → 解压 → 用 `System.IO.Hashing.XxHash128` 算哈希，与随包的 `res://bake/<地图>/boot/bake.manifest.json` 里这个缓存键的哈希比对，不符就删掉、自动重建一次（见下文"损坏、校验失败与终态"）。缓存键不在 manifest 里的文件（`--seed` 覆盖、开发构建）只在开发构建或带 `--seed` 时接受，按 `cache.index` 的哈希校验，其余情况一律重烘 → 在同一工作线程上用 `RenderingServer.MeshCreateFromSurfaces` 建网格（与 `--gen=live` 同一条路径），碰撞形状由参数创建 → 主线程照常只挂载。
- **写**：先写 tmp，`Flush` 后改名。每图一份 `cache.index`（键、字节、最后使用时间），只用于 LRU 和统计，不作信任依据；信任依据是随包的 `bake.manifest.json`。
- **查找顺序**：启动集（`res://bake/<地图>/boot/`）→ 本机缓存 → 本机烘焙（生成后写进缓存）。
- **失效**：缓存键里已含代码哈希和数据哈希，游戏更新后自然不命中。数据哈希每图一份，任何一项 `Build()` 输入变了（哪怕只改一条覆盖层），本图全部产物重烘；代码哈希按模块，只改一个模块自己的代码只重烘它，改了共享生成依赖则所有用到它的模块一起重烘（ENG-ARCH §4）。数据输入要不要细分到模块，等全图失效确实拖慢制作再定。启动时删掉 `user://cache/bake/<地图>/` 下不是当前 ABI 的目录。`--seed` 覆盖时种子进数据哈希，各自一份缓存。
- **大小与淘汰**：一张图烘满约 0.8–1.4 GB。默认上限 2.5 GB，超出按"最久未用"淘汰；剩余空间不足 1 GB 时不写缓存，只在内存里生成、用完即弃（ENG-BUDGET §8）。
- **接口**：`Ensure(key, priority)` 返回句柄，挂载侧用；`Progress`（总量、已完成、当前阶段）给扉页和设置用；`CacheBytes`；`Clear()` 只清缓存，已挂载的不受影响，之后按需重烘。设置卡的"存储"一行（WP-35）显示占用和进度（"已完成"或"进行中 63%"），并带"清除缓存"按钮。
- **损坏、校验失败与终态**（WP-02；失败页由 WP-01、WP-49、WP-35 提供，SYS-TITLE §1、SYS-MAPS §4）：
  - **只自动重建一次**：读到损坏或哈希不符的文件，删掉、重建一次；同一个键在本次运行里最多自动重建一次。
  - **就绪前校验**：新生成的产物在交给挂载、写进缓存之前，先对要写进 `.tbin` 的未压缩字节算 XxHash128（与"读"一条算的是同一份字节），与 manifest 比对（键不在 manifest 里的，照"读"一条的规则处理）。
  - **终态**：重建后仍不符、`Build()` 抛异常或解析越界时，句柄进入 `BakeState.Failed`，`Error` 写一行可复制的原因（缓存键、阶段、期望与实际哈希或异常类型），不写缓存，本次运行不再自动重试；失败页的"重试"调 `ClearFailures()` 后才再试一次（ENG-ARCH §3）。每个失败的键写一条错误日志（§12.6）。
  - **等待方怎么出来**：不阻塞画面的请求（游戏中的 L0–L3）失败时，照旧显示最近的已就绪祖先（第 2 条）。阻塞等待的一方（扉页的首次运行、切图与读档的雾幕、碰撞冻结、进楼软停）看到 `Failed` 就停止等待，转到不依赖三维世界的失败页：重试、复制错误信息、退出。
  - **不按短时限误判**：正常烘焙只要还有进展就一直等，不设固定超时。只有等待的那批请求连续 60 s 没有任何进展（完成件数和子步骤都不变；底图这类大件按子步骤报进度；初值，§16），才按失败处理，原因记"烘焙停滞"。
  - `--bake-strict=1` 下遇到 `Failed` 直接以非零退出码结束。

**本机烘焙的时机**（D-165）：

- **首次运行**（缓存为空，或缓存清空、版本更新以后）：扉页期间，先烘起始视角（T0 或命令行位姿）周围按本节"预取"的环要用的格（L2–L3 与外围），再烘平面地图底图。扉页下方显示一行进度，例如"首次运行 · 正在本机生成校园 37%"，用数据字体，烘完就淡出（WP-49）。目标见 §8。
- **后台预烘**：进开始页起，按与起始点的距离由近到远烘满全图，顺序是 L3 → L2 → L1 → L0 → 碰撞 C → 开放室内 → 简化室内。线程数：扉页和开始页期间取 max(1, 逻辑核数 − 2)，进 Playing 后降到 max(1, 逻辑核数 / 4)；线程优先级低于正常，并且排在按需请求之后。开发机在开始页烘满全图 ≤ 4 min（初值，§16）。`--bake-bg=0` 关闭后台预烘，门禁和基准都关（§12.4）。
- **按需**：流式请求的文件不在缓存里时，生成任务进与加载同样的四个队列：碰撞 → 已知目的地 → 室内入口预取 → 常规。生成任务在工作线程上调用 `Build()`、打包表面、写缓存，再把同一个 TileBundle 直接交给挂载，不再从盘上读一遍。同时在途的生成任务不超过 max(2, 逻辑核数 − 2)，与加载请求的"在途不超过 3 个"分开计。
- **单件生成耗时的初值**（开发机单线程，P0 实测，§16）：L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms。
- **不出空洞、不穿地**：靠下面的六条不变量。缺 L0 时显示最近的已就绪祖先（第 2 条）；碰撞未就绪冻结物理（第 5 条）；室内包没就绪时按第 6 条（进楼衔接）原有的未就绪规则。
- **切图**：`Travel` 的 LoadData 挂上 `boot_<地图>.pck`；起始点周围在缓存里没有时，在雾幕下烘。

**整图卸载与同图重载**（SYS-MAPS §4.2 第 3、5 步）：

- **卸载**（Unload）：流式队列不再发新请求，在途的加载请求和生成任务照常完成后直接 `Dispose()`（生成的文件照常写进缓存）；本图全部实例 `FreeRid`、碰撞 body 出树，释放 L4、遮挡体、外围 O2 常驻块、室内包和倒影代理；全图数据纹理交还各写入方。LRU 的淘汰规则此时不适用（全卸）。最后 `GC.Collect()` 一次（雾幕里允许，期间 GC 延迟模式临时改回 Interactive，§6）。
- **装载**（Mount）：同步加载新图的 L4、遮挡体和 O2 常驻块（§5 第 1 条），写 `map_rect`，各写入方重建全图纹理；按已知目的地预取出生点周围的 L0+–L2 各环和室内包，行走模式另加碰撞格。
- **先卸后装**：显存峰值是"最大的一张图 + 全局常驻"，不是两张图之和（§7）。
- **同图重载**：`--gate=maptravel` 对同一张图卸载再装载 3 次，判泄漏、计数和弱引用（SYS-MAPS §13，并进 G16）。全局节点（天空、植被系统、水、灯光、天气、界面、平面地图）跨图常驻，只经 `IMapAware` 换数据，不得跨图持有格子句柄或纹理内容。

**预取**：用平滑后的速度（时间常数 0.3 s）外推 +0.5、+1.5、+3 s 的位置，并夹在活动范围之内。路径已知时改用本节的"已知目的地预取"。

**速度降级**：

- 屏幕误差阈值 τ = 1.5 px × (1 + v / 30 m/s)。
- 另保留 15 m/s 和 45 m/s 两道硬门槛，带回差：速度降到 10 m/s 和 35 m/s 以下、并持续 0.3 s 后才恢复。
- 屏幕误差按内部渲染高度计算，渲染比例低的档会自动选更粗的级别。
- 跳转和旁观切行走的降落豁免速度律（ENG-BUDGET §4）。跳转的最后 25% 路程仍不超过速度律；降落则在落点未就绪时悬停等待（SYS-WALK §5）。行走模式的水平速度上限 12 m/s，始终低于 15 m/s 门槛。

**绝不卡帧、不穿地的六条不变量**（ENG-BUDGET §1 第 4 条引用的就是这六条）：

1. **当前图的 L4 与外围 O2 常驻块常驻**：切图时在 Mount 那一步用同步的 `ResourceLoader.Load` 从启动集加载（冷启动时在扉页或雾幕后面），不参与淘汰。
2. **父格等子格**：一个格的 4 个子格都"已加载、已挂载、管线就绪"（组合都在预热表里，§2）之后，才在同一帧切换父子格的可见性。缺 L0 时显示最近的已就绪祖先。
3. **淘汰**：按 TileBundle 统计里的字节数做 LRU，最细的级别优先淘汰；正在显示的祖先不淘汰；每次最多淘汰 5%。淘汰 = `FreeRid` 实例 + `Dispose()` 资源。字节上限取当前预设的几何显存上限（ENG-BUDGET §2），DXGI 报告显存紧张时再收紧（§11.7）。
4. **看门狗**：连续 3 帧挂载超出预算的 2 倍，就把激活预算减半，并暂停激活 250 ms。
5. **碰撞未就绪冻结物理**（SYS-WALK §14）：行走模式下，角色所在格及 8 个邻格的碰撞数据就绪之前，冻结物理，角色停在原地，不受重力。这一条与渲染当前显示的是哪一级无关，判断用 `ICollisionWorld.Ready()`。它保证的是"不穿地、不掉出世界"，所以宁可停住，也不用粗数据先走。读档恢复行走状态时同样适用：恢复的脚位所在格及 8 个邻格的碰撞就绪之前，冻结物理（SYS-SAVE §5.1）。
6. **进楼衔接**（WLD-INTERIORS §6.4）：
   - 镜头在任何楼体里时，至少显示这栋楼的 I0 兜底层（楼板、外墙内皮、交通核体块），永远不显示空壳；
   - 行走模式下，角色要跨进的开放室内楼层，IF 与碰撞没有就绪就在**门槛前 1 m 软停**：水平速度按距离线性降到 0，就绪后恢复，不撞墙、不出提示。开放楼的入口门固定敞开，所以屏障不能是"门不开"。G19f 统计触发次数，目标为 0（WLD-INTERIORS §19）；
   - 从旁观切行走时，要等当前层的楼板碰撞就绪才落地（SYS-WALK §5）。

两条全局约束：雾幕之后 SURFACE、DRAW 管线编译计数不增（§2）；主线程不生成几何（本机烘焙和开发态实时生成都在工作线程上）。

**Movie Maker 下的流式屏障**（G13 录像，ENG-GATES §1）：后台加载按真实时间进行，而 Movie Maker 按帧号推进时间，所以录像模式下流式管理器改为同步：本帧需要的格全部同步取回（`res://` 里的资源 `LoadThreadedGet` 阻塞，本机缓存的 `.tbin` 同步读入）并挂载完，再推进路线和时间，结果才能复现；缓存里没有的格同步生成，本机烘焙同样算在屏障里。过场同样适用（`Engine.GetWriteMoviePath()` 非空时，SYS-TITLE §10）；读档和切图也是屏障，就绪（含本机烘焙）之前不推进下一帧。

**借鉴的成熟做法**：3DTilesRendererJS 的 250 ms 淡入淡出、同时淡出超过 50 格就直接切换、下载与解析分开；Cesium 的"移动时剔除请求、视野中心优先、停下 0.2 s 后再补边缘"。

## 6 主线程与 C#（WP-01）

主线程的组织：节点的 `ProcessPriority` 定顺序；流式内容不走场景加载；界面经 `HudModel` 采样；`*Module.cs`（gen 里，纯函数）与 `*Node.cs`（主程序集，主线程）分开（ENG-ARCH §2）；跨线程数据用 C# 内存池加一次跨边界拷贝。

**每帧主线程预算**（P0 实测校准，§16；渲染线程模式为 Safe，剔除和绘制提交都在主线程上）：

| 项目 | 开发机 | 最低配置档 |
| --- | --- | --- |
| 渲染 CPU：剔除、绘制列表、提交（`ViewportGetMeasuredRenderTimeCpu`） | 3.0 ms | 4.5 ms |
| 各帧步骤（SYS-TIME §1 每帧顺序表里的各步与各系统的 `_Process`） | 0.8 ms | 1.2 ms |
| 物理（行走模式，120 Hz，Jolt 加自写层） | 原型重定（预计 0.05–0.3 ms） | 同左 |
| 流式挂载（含碰撞格入树、室内包的一层） | 1.0 ms | 1.5 ms |
| 界面脚本与排版 | 平均 0.2 ms | 0.3 ms |
| 音频（WP-53：内核、环境声、声学、观者声，含引擎更新在播的 3D 声源，SYS-AUDIO §18.1） | 0.35 ms | 0.35 ms（最低配置档靠降级守住：在播 3D 声源上限默认降到 24，次要的床退成 2 个锚点） |
| 余量 | ≥ 0.15 ms | ≥ 0.15 ms |

合计约 5.5 ms，与 ENG-BUDGET §2 的"主线程 CPU p95 ≤ 6 ms"对应。
- 界面的画布剔除与合批计在第一行"渲染 CPU"里：开发机 ≤ 0.3 ms、最低配置档 ≤ 0.5 ms（UI-VISUAL §12；376 个控件约 0.12 ms）。小地图主线程平均 ≤ 0.02 ms（"随镜头"模式 ≤ 0.05 ms），计在界面一行里（SYS-ATLAS §10）。
- 音频 0.35 ms 从余量里划出，开发机余量 ≥ 0.15 ms（ENG-BUDGET §8）。
- 其余各项都在已有行里：`CinematicPlayer` ≤ 0.05 ms（各帧步骤，SYS-TITLE §11.1）；存档快照 p99 ≤ 0.3 ms，平时几乎不做事（各帧步骤，SYS-SAVE §13）；缩略图那一帧主线程 1080p ≤ 5 ms、1440p ≤ 8 ms，只在退出、返回标题、失焦、新游戏时出现，不进自适应统计（§11.3）；外围的可达空间求值与校界查询各 ≤ 0.02 ms（相机那一步，WLD-OUTSKIRTS §11.3）；开放楼的自动门状态机 ≤ 0.02 ms（介质那一步，WLD-INTERIORS §4.1）。

官方基准里 1 万个同网格的 MeshInstance3D，渲染 CPU 约 4.6 ms，所以主通道 2000 次、阴影 2500 次绘制的提交开销要在 P0 用本机和一台最低配置的笔记本各测一次。

**帧内顺序**（与 ENG-ARCH §3 的 `FramePriority` 表、SYS-TIME §1 的每帧顺序表一致；任何两步不得同值，同值时 Godot 按场景树顺序执行，说不清谁先谁后）：

地图 −1300（WP-46：切换状态机、`IMapAware` 分发、Mount 时写 `map_rect`，SYS-MAPS §9）→ 画质 −1200（`FramePriority.Quality`，只在设置或自动调节变化的那一帧做事，同一帧里所有系统看到同一版本，SYS-GRAPHICS §8）→ 相机 −1100（含 `CinematicPlayer`，SYS-TITLE §8.3；写 `edge_fog`）→ WorldState −1000（推进钟点、季节后求天气向量）→ 主光 −900 → 调色板 −800 → 通道 −700 → 动态 −650（季节氛围 −640）→ 介质 −600 → 声学 −590（SYS-AUDIO §20）→ 流式选级与请求 −500 → 默认优先级的系统 0（植被近池重建、`IAtlasService` 等）→ 音频 50 → 界面 100（HudModel、`UiThemeClock` 写 `ui_night`）→ 流式挂载 900 → 存档 1000（平时只计时，有请求时在安全时刻拍快照；读档时在这一步恢复，SYS-SAVE §11）。

行走模式的 `_PhysicsProcess` 由引擎在每帧的 `_Process` 之前按 120 Hz 补步；观者的脚步等声音在 `_PhysicsProcess` 里当场发声，不等音频那一步（SYS-AUDIO §20）；相机在 −1100 那一步按 `Engine.GetPhysicsInterpolationFraction()` 插值（SYS-WALK）。

**零分配循环**：

- 逐帧路径不 `new` 引用类型，不用 LINQ、闭包、`params object[]`，不装箱，不拼接字符串；集合预分配，临时缓冲用 `ArrayPool<T>` 或自有池，小数组用 `Span<T>` 和 `stackalloc`。
- **Godot 特有的隐形分配和开销**：
  - `string` 隐式转成 `StringName` 每次都要分配并算哈希：全局参数名、着色器参数名、信号名一律用 `static readonly StringName` 常量（§1.5）；
  - `Godot.Collections.Array`、`Dictionary` 每次跨边界都要封送：热路径不用（`MeshCreateFromSurfaces` 的表面字典只在加载线程上用）；
  - `GetNode<T>("路径")` 只在 `_Ready` 里调用并缓存；不每帧 `GetChildren()`；
  - 不在 C# 里遍历场景树找东西。
- **TileKey** 作为字典键时用整数 tileId（§5），不用 record 的默认哈希。

**跨边界开销**：每次调用引擎 API 都有一次封送，所以一律批量：

- MultiMesh 用 `MultimeshSetBuffer` 一次写整块（§1.2）；
- 网格用表面字典一次建成；
- 数据纹理整张 `Image.SetData` 后 `ImageTexture.Update`，Image 和 `byte[]` 都复用；
- 全局参数每帧二十多次 `GlobalShaderParameterSet`，每次约 1 µs 量级，可以接受。

**GC**：

- 工作站 GC、并发 GC；雾幕结束后设 `GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency`（ENG-BUDGET §10）。
- 监视：`GC.GetTotalPauseDuration()` 和 `GC.CollectionCount(0/1/2)` 做成 `Performance.AddCustomMonitor` 的自定义项。
- 门禁（初值，§16）：F1 一圈里第 2 代回收 0 次，单次暂停不超过 1 ms；GC 门禁仍在缓存已烘满时测（`--bake-strict=1`、`--bake-bg=0`，§12.4）；违反时用 dotnet-counters 和 dotnet-trace 查分配来源（§12.3）。
- 本机烘焙（D-165）在发布版的工作线程上跑 `Build()`：生成代码用 `ArrayPool` 和预分配缓冲，少分配。另加一条 P0：后台烘焙期间第 2 代回收的暂停（§16）。
- JIT：雾幕或扉页期间各系统至少空跑 30 帧，让热路径先编译；导出版能否用 ReadyToRun 进一步减少首帧 JIT，P0 评估。另外：存档的"序列化 + 压缩 + 哈希"空跑一次，写进内存流（第一次序列化实测 29 ms，全是 JIT，SYS-SAVE §13）；`CinematicPlayer` 把 `enter_new` 前 2 s 在内存里空算一遍（SYS-TITLE §1.1 第 7 步）；界面的建页、开合、滚动数字、弹簧、交班扫过的代码路径空跑一遍（UI-VISUAL §11.9）。

**线程边界**（ENG-BUDGET §7 第 16 条）：工作线程只做纯 .NET 计算（`Build()` 与各烘焙子任务，开发态和发布版都在这里跑，发布版即本机烘焙，D-165）、读写本机缓存的 `.tbin`（自己的解析器，`user://` 下不经 ResourceLoader 和 ResourceSaver）、`ResourceLoader` 对 `res://` 的后台加载和 RenderingServer 的资源创建（`MeshCreateFromSurfaces`、纹理创建）；`RenderingServer.Instance*`、场景树和 PhysicsServer3D 的 body 只在主线程调用（见官方文档"线程安全的 API"）。gen 程序集里仍禁用 `Parallel` 和 `Task.Run`，格与格之间的并行由内核调度（ENG-ARCH §2）；生成任务的并发上限见 §5。主线程不变：生成只在工作线程上做，主线程只挂载，预算仍是每帧 ≤ 1.0 ms。

## 7 内存与显存（WP-01、WP-02）

Godot 在 D3D12/Vulkan 上没有"上下文丢失"这一类事件；设备移除（驱动重置）时引擎直接报错退出，不做恢复。

**显存预算**（ENG-BUDGET §2，按画面预设）：

| 预设 | 纹理 | 几何 | 渲染目标与阴影图（不含约 70 MB 固定开销） | 合计 |
| --- | --- | --- | --- | --- |
| 极致 | ≤ 384 MB（四档同一套纹理） | ≤ 1 GB | ≤ 450 MB（1440p）；≤ 700 MB（4K） | ≤ 3 GB |
| 精致 | ≤ 384 MB | ≤ 600 MB | ≤ 300 MB；≤ 450 MB（4K） | ≤ 1.8 GB |
| 均衡、流畅 | ≤ 384 MB | ≤ 400 MB | ≤ 200 MB | ≤ 1.2 GB（核显占系统内存） |

纹理没有质量选项，四档相同，上限 384 MB；美术需要更多纹理时由主代理按 ENG-BUDGET §2 重定（§16）。进程内存（不含显存）：精致、极致不超过 4 GB，均衡、流畅不超过 3 GB；核显的显存从系统内存里划，两者合计不超过约 4.2 GB。

切图先卸后装（§5），峰值是"最大的一张图 + 全局常驻"，LRU 的字节上限按预设，与哪张图无关（SYS-MAPS §12）。

**字节账**：`Performance` 只给总量（`RenderVideoMemUsed`、`RenderTextureMemUsed`、`RenderBufferMemUsed`），所以内核按格自己记账：

- 几何 = Σ顶点数 × 20 B（植被 VF-C 按 44 B）+ Σ各级 LOD 的索引数 × 2 B + MultiMesh 实例数 × 64 B。引擎把每一级 LOD 的索引单独存一份，前缀并不共享存储，所以 LOD 的索引要全部计入。
- 纹理 = GPU 格式字节 × 1.33（含 mip）：BC1 0.5 B/px，BC3、BC7 1 B/px。
- 渲染目标按 §11.7 的公式记：3D 渲染缓冲约 44 B × 内部像素，描线另加 normal_roughness 4 B × 内部像素和自建的中间纹理；SMAA 约 17 B、FSR1 约 14 B × 输出像素；固定开销约 70 MB。精致 1080p 原生约 135 MB，极致 1440p 原生约 240 MB，极致 4K（内部 4.9 MP）约 490 MB（都不含阴影图、倒影和固定开销）。
- 阴影图按分配尺寸记：8192² × 2 B = 128 MB，4096² 为 32 MB，2048² 为 8 MB；点光阴影图设为 0（ENG-BUDGET §10）。
- 倒影 SubViewport：44 B × 1/16 输出像素，1440p 约 10 MB，4K 约 23 MB。
- 天气：`cloud_noise` 1 MB、`precip_occ` 约 3.3 MB、`refl_lamps` 可忽略，另加地面遮罩 2（与 `ground_mask` 同尺寸）；降水池 8192 × 64 B = 0.5 MB。
- 碰撞数据在 C# 堆和 Jolt 里，也按字节记进账（加载环内的碰撞 `.tbin` 不超过 1.3 MB；Jolt 内部结构 P0 实测）。
- 外围、室内、平面地图、音频等各项（都计入上表已有的额度，主代理按 ENG-BUDGET §2 一并核对）：
  - **几何**：外围 ≤ 80 MB（O1 流式、在 LRU 里，O2 常驻 ≤ 12 MB，O0 流式 ≤ 20 MB，WLD-OUTSKIRTS §11.3）；开放室内一栋整体驻留 ≤ 30 MB，最多 2 栋 ≤ 60 MB（WLD-INTERIORS §12）。
  - **纹理**（384 MB 额度内）：平面地图约 41 MB（底图 SDF 三张、外圈纹理和地标线描图集）和常驻缩略图约 1.6 MB（SYS-ATLAS §10）；外围新增贴图 ≤ 48 MB（WLD-OUTSKIRTS §11.3）；窗卡与门厅卡的视差数组约 5.4 MB（WLD-INTERIORS §5.1）；`window_override` 256 KB；按 `map_rect` 覆盖的全图纹理（`precip_occ` 3.3 MB、`lamp_grid` 320² × 16 B 约 1.6 MB）；扉页静帧只在 Boot 状态加载一张，约 20 MB，转为实时后释放（SYS-TITLE §1.3）。
  - **进程内存**（不占显存）：音频常驻 ≤ 96 MB（53a 登记 `audio/resident_kb`，SYS-AUDIO §18.2）；室内碰撞每栋 ≤ 2 MB、室内索引约 50 KB、导航图约 2 MB（WLD-INTERIORS §12）；外围前沿线距离场约 0.3 MB、软地板网格约 0.25 MB（WLD-OUTSKIRTS §11.3）；存档峰值 < 1 MB（SYS-SAVE §13）。
- 每帧用 `Performance` 的总量复核内核的账，偏差超过 15% 报警。

**显存容量查询**：Godot 没有查询显存总量的 API，`GetDeviceTotalMemory()` 在 D3D12 下返回 0。WP-32 拿到 Godot 正在用的 DXGI 适配器，用手写的 COM 调用读 `IDXGIAdapter3.QueryVideoMemoryInfo` 的显存预算和当前用量（做法见 §11.7），用于首次分档、设置页的显存估算和运行时的显存压力判断：用量逼近预算时先收紧 LRU 的字节上限，再按 §11.3 降级。`Performance.RenderVideoMemUsed`（D3D12MA 的分配字节）始终比 DXGI 的当前用量少约 70 MB，比对时要扣掉。

**释放的归属**：格子只释放自己的实例和 TileBundle（§1.2）。共享的卡通材质、图集、数据纹理归 WP-04 和各写入方所有，格子不释放它们。全图数据纹理归各写入方，切图时由它们经 `IMapAware` 自己重建（SYS-MAPS §10）；室内包的实例和碰撞归流式管理器，随这栋楼一起淘汰。

**泄漏门禁**：F1 路线跑两圈，第二圈结束时：

- `RenderVideoMemUsed`、`RenderBufferMemUsed` 和内核账里的实例数、字节数与第一圈相差在 ±5% 以内；
- `Performance.Monitor.ObjectCount`、`ObjectResourceCount` 稳定，`ObjectOrphanNodeCount` 不增长；
- 管线编译计数不变；
- `GC.Collect()` 之后托管堆增长不超过 10 MB。

另有同图重载 3 次的切图泄漏门禁（`--gate=maptravel`，SYS-MAPS §13，并进 G16）。

## 8 安装包与磁盘布局（WP-01、WP-02、WP-04、WP-27）

**安装包的组成**（D-165 本机烘焙；合计约 0.45–0.75 GB，与 ENG-BUDGET §8 一致，P0 导出实测，§16）：

| 内容 | 大小 |
| --- | --- |
| Godot 运行时与 .NET 运行时（导出模板自带） | 约 100–150 MB |
| C# 程序集（主程序集、契约、各 gen 程序集；gen 与各烘焙子任务的代码都要随包发，本机烘焙用） | ≤ 25 MB |
| 着色器（shader baker 只发 D3D12 一份产物；Vulkan 只是回落，回落时在扉页后面现场编译，§13） | 约 10–40 MB |
| 贴图（BC 压缩，BC1/BC3 优先，BC7 只在需要时用：图集、Texture2DArray、替身；含外围新增贴图） | ≤ 250 MB |
| 界面字体（思源黑体 SC 三个字重、Inter、JetBrains Mono，整包，不改文件，UI-VISUAL §4.1） | 约 30–50 MB |
| 扉页静帧（每张图 8 张，WebP，每张 0.5–1 MB，SYS-TITLE §1.3） | 约 4–8 MB |
| 世界数据 JSON（全局 data 与按图的 `data/maps/<地图>/`：`campus.json`、覆盖层、点位、时刻表、`outskirts.json` ≤ 5 MB、`interiors.json`、过场） | 约 10–15 MB |
| 英雄资产 `.glb`（如有） | 计入主 PCK |
| 启动集 `boot_<地图>.pck`（每图：L4、遮挡体、外围 O2 常驻块与卡片版、常驻低精度地形高度、需要 GPU 才能出的产物如 L2 半八面体替身图集；全图常驻的小数据：派生数据纹理（岸线 SDF、灯表、`precip_occ` 等）、2 m 分类栅格与 8 m 安全点网格、声学网格、声景场与发声器表（`ambience.bake`）、导航图；参考哈希清单 `bake.manifest.json`，包内 `res://bake/<地图>/boot/bake.manifest.json`，G6 的 `--bake-verify` 用；zstd） | 每图 ≤ 60 MB（其中全图常驻小数据约 10–15 MB） |
| 音频（床 Ogg、短声 QOA、界面 PCM、开场音乐，SYS-AUDIO §18.2） | 约 125–165 MB，G6 里单列 ≤ 165 MB |

合计约 0.45–0.75 GB。几何类烘焙产物不随包发，在玩家机器上本机烘焙并缓存（D-165，§5）。**不随包发、本机烘焙**的只有四类：L0–L3 的 TileBundle（含外围 O0、O1 和 I0 兜底层）；碰撞级 C；开放室内与简化室内的室内包和碰撞包；平面地图底图（SDF、MSDF 图集、`labels.bake`、区域栅格）。

G6 的导出体积预算写进 `budgets.json`，两个上限分开记：安装目录 ≤ **0.9 GB**（`export.install`），分发压缩包（7-Zip，LZMA2，`-mx=9`，文件名 `UESTC-<版本>-win64.7z`）≤ **0.7 GB**（`export.archive`）；音频单列，外围的 O2 与卡片版在启动集里、`outskirts.json` 在世界数据里（ENG-BUDGET §8）。各图的 `boot_<地图>.pck` 体积另在 `maps.<id>` 里核算（SYS-MAPS §12）。全图 L0 的三角形总数决定本机缓存的大小和本机烘焙的时间，不进包体：每多 1000 万个三角形，缓存约多 150–200 MB。

**磁盘布局**（P0 用导出验证）：

```
UESTC/                              安装目录（G6 导出，≤ 0.9 GB）
├─ UESTC.exe                        导出模板
├─ UESTC.pck                        主资源：着色器（D3D12）、贴图、字体、全局音频、assets/title/（扉页）、全局 data 与 data/maps/*/
├─ boot_qingshuihe.pck              清水河的启动集：L4、遮挡体、外围 O2 常驻块与卡片版、常驻低精度地形高度、需要 GPU 才能出的产物（如 L2 半八面体替身图集）、全图常驻的小数据（派生数据纹理、2 m 分类栅格与 8 m 安全点网格、ambience.bake 与声学网格、导航图 nav/）、bake.manifest.json；包内路径 res://bake/qingshuihe/boot/
└─ data_<程序集名>_windows_x86_64/   .NET 程序集与运行时（Godot .NET 导出的固定布局）

user://（导出版是 %APPDATA%\UESTC\，不带版本号，ENG-ARCH §2）
   settings.cfg、input.cfg、graphics.cfg（画面设置与分档缓存，SYS-GRAPHICS §6）、engine_override.cfg（重启类画面设置）、audio.cfg（SYS-AUDIO）、saves/<slot>/g*.sav、saves/.lock、saves/corrupt/（SYS-SAVE）、管线缓存（只有 Vulkan 用）、着色器缓存、cache/bake/<地图>/<ABI>/…（本机烘焙缓存，含每图一份 cache.index，§5，预算见 ENG-BUDGET §8）、logs/（日志，§12.6）、diagnostics/（导出的诊断包，§12.6）
```

- **启动集按图分开**：WP-02 的打包步骤用 `PckPacker` 只把本图的启动集写进 `boot_<地图>.pck`（ENG-ARCH §4 的"按清单打进导出包"），首次进图时 `ProjectSettings.LoadResourcePack` 挂上、不卸载，`replaceFiles: false`；包内路径一律 `res://bake/<地图>/boot/`，G6 查各 PCK 的路径两两不相交（SYS-MAPS §2.4）。其余烘焙产物不进任何 PCK，在本机缓存里（§5）。改了代码或数据，缓存键变化，玩家机器上按 §5"失效"一条的粒度重烘：数据变了整图重烘，只改了某个模块的代码就只重烘它；启动集内容变了才重打 `boot_<地图>.pck`。参考哈希清单 `bake.manifest.json`（每图一份）在启动集里，包内路径 `res://bake/<地图>/boot/bake.manifest.json`。
- `.res` 本身已是 zstd 压缩，PCK 不再二次压缩；贴图本身是 BC 格式。
- 设置类文件（`settings.cfg`、`input.cfg`、`graphics.cfg`、`engine_override.cfg`、`audio.cfg`）读值时只接受基本类型，先查文本、含 `Object(` 之类的写法就整份退回；写盘走 tmp + `Flush(true)` + `File.Replace`（SYS-SAVE §8）。
- 只考虑 SSD；机械硬盘上的加载时间不作为验收条件。

**启动（扉页后面）要做的事**（顺序以 SYS-TITLE §1.1 为准，地图部分按 SYS-MAPS §4.2 的 `Travel(Boot)`）：

1. 引擎与 .NET 启动、程序集加载；读画面设置（命令行 > `graphics.cfg` > 自动推荐 > 默认，SYS-GRAPHICS §6）。首次运行时先用默认值，第 6 步分档后再换成推荐预设，仍在扉页后面完成；预热已覆盖所有可达的组合，换预设不再编译；
2. 主场景第一帧在 0.3 s 内换上扉页（`--title=0` 时是雾色），读各存档位的 meta，选出最近存档位；读 `index.json`，定起始图（`--map` > 最近存档的图 > 默认图）；
3. `IMapService.Travel(Boot)` 的 LoadData 与 Mount：挂上起始图的 `boot_<地图>.pck`；解析 `campus.json`（WP-08，`System.Text.Json`）和 `outskirts.json`；同步加载 L4、本图遮挡体、外围 O2 常驻块、常驻的低精度地形高度和全图常驻的小数据（都在启动集里，不现烘）；写 `map_rect`，创建全局数据纹理（调色板、树种表、`mat_table`、地面遮罩与地面遮罩 2、灯表、天气的 `cloud_noise` 和 `precip_occ`，灯表、`precip_occ` 等派生数据纹理从启动集读）；音频内核建总线树，后台加载常驻声音集；
4. 三维预热（§2）；
5. 二维预热：界面着色器、`map_base`、开始页与放映层组件，大页预建并隐藏（§2）；
6. 首次分档的测量（§11.2）；
7. 恢复世界与 T0：有存档时恢复最近存档位的地图、世界段和天气段，视角段留作继续的终点；没有存档时建预览世界（新游戏的终态）；`IAppFlow` 冻结流逝，镜头放到 T0，按 §5 的环加载 T0 周围（只要 L2–L4 和外围；缓存里没有的现烘）；`--title=0` 时改为按命令行的世界状态和位姿加载起始视角周围的 L0–L2（缓存里没有的现烘）；
8. JIT 预热（§6）；
9. 转为实时：扉页以 1.2 s 交叉淡化成实时 T0（`--title=0` 时雾幕 2–3 s 淡出，直接进入 Playing）。"音频就绪"也是这一步的条件之一（SYS-AUDIO）。

**首次运行**（本机缓存为空，或缓存清空、版本更新以后，D-165）：缓存里没有的内容在扉页期间现烘：先烘起始视角周围的格（L2–L3 与外围，第 7 步），再烘平面地图底图；第 5 步里依赖底图的地图纹理和大页预建排在底图烘完之后。扉页下方显示进度行（WP-49，见 §5），烘完淡出；进度来自 `IBakeService.Progress`。

**目标**（初值，SYS-TITLE §1.2、本文 §16）：开发机上，缓存已烘满时，窗口出现到扉页热启动 ≤ 1.0 s、冷启动 ≤ 1.5 s；到转为实时热启动（驱动的着色器缓存已热；D3D12 下引擎不存管线缓存，§2）≤ 3.5 s，冷启动（驱动的着色器缓存冷，含首次分档）≤ 9 s。首次运行到转为实时 ≤ 25 s，其中平面地图底图 8 线程 ≤ 20 s；开始页上后台预烘满全图 ≤ 4 min（§5）。

**首帧之后再加载的内容**：点位缩略图；全局常驻的碰撞数据（2 m 分类栅格、8 m 安全点网格、约 0.9 MB）和声学网格、声景场，都在启动集里，在首帧之后空闲时后台加载，第一次切到行走之前完成。C# 程序集随启动一起加载，不分包。

**兜底**：D3D12 不可用时引擎自动回落 Vulkan（`fallback_to_vulkan`）；回落时包里没有 Vulkan 的着色器产物，在扉页后面现场编译（§13）。两者都不可用时由引擎报错退出。不回落到兼容渲染器（它跑不了 Compositor 描线），也不做降级画面。

## 9 植被（WP-23）

**容器**：

- **近池**（150 m 以内，L0+、L0、L1）：每个"母模型 × 级"一个动态 MultiMesh（一个树种可有几个几何变体母模型，WLD-VEGETATION §11.2、WLD-VEGETATION §11.4；MultiMesh 只能有一个网格、共用一级 LOD，所以三级分开）。CPU 以不超过 10 Hz 的频率按距离把每株分进对应的级（回差 10%），用 `MultimeshSetBuffer` 整块写入。
- **远池**（150–400 m，L2）：64 m 一块，静态，随格子烘焙。节点用 visibility range，`fadeMode` 为 Disabled，范围按块的半对角线放宽；由着色器按每株的距离做 4×4 Bayer 抖动，决定这株显示替身还是交给 L3。
- **L3、L4**：树冠云，并进格子几何。
- 草、地面落叶层、飘落池、降水池、荷、萤火虫：各自的 MultiMesh（下文）。
- 近池重建的耗时（银杏大道 1600 株）在 P0 实测；C# 不够快时，再考虑计算着色器剔除加间接绘制（§1.2）。

**分级**：植被按实例单独选级，不跟随格子的级别；回差 10%。下表以 12 m 高的银杏为基准，其他树的切换距离乘以（树高 / 12）× 细节系数。

| 级 | 距离 | 内容 | 三角形 |
| --- | --- | --- | --- |
| L0+ | 0–15 m | 树干 32 段，带根部隆起；8–12 个团块；1500–2000 张插片；300–500 片几何叶 | 9–10k |
| L0 | 15–40 m | 树干和主枝 16 段；600–800 张插片 | 3–4k |
| L1 | 40–150 m | 树干 8 段；团块加 100–150 张轮廓插片 | 0.6–0.9k |
| L2 | 150–400 m | 替身：半八面体替身（6–8 个顶点的裁边多边形）或团块低模（150–300 三角），P0 二选一 | 4–6 或 150–300 |
| L3/L4 | > 400 m | 树冠云、剪影，并入所在格 | — |

**云团型树冠**（樱花、桂花这类团块型树冠做成厚实的云团，WLD-VEGETATION §1.1；三角形 P0 核实，§16）：

| 级 | 云团型内容 | 三角形 |
| --- | --- | --- |
| L0+ | 18–30 个小球，二十面体 2 级细分（每球 320），毛边插片 300–600 张 | ≤ 1 万 |
| L0 | 同样的小球，1 级细分（每球 80），毛边插片 ≤ 150 张 | 1.5–2.5 千 |
| L1 | 6–10 个合并团块 | 0.6–0.9 千 |
| L2 | 团块低模（与插片型共用方案 A） | 150–300 |

投影代理不变：云团型的 L1 团块挖洞同样形成林荫光斑。

- **近处三级不做抖动淡化**：插片按重要度排序，低一级是高一级的前缀；过渡带里高一级多出来的插片在 vertex() 里按距离缩到 0（距离用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`）。抖动淡化只用在"L1 → 替身"和"替身 → 树冠云"两处。
- **团块数**（与 WLD-VEGETATION、ART-MODELING一致）：3–7 个主团块，每个主团块分 2–3 个子团块。

**替身**（P0 在 E1、银杏大道、西湖岸的 150 m 切换距离上对比两种方案，四季各验一次）：

- **团块低模**：150–300 三角，不透明，用世界坐标噪声 discard 控制覆盖率；冬季用 `tree.bare`，覆盖率不低于 0.35。PC 上实现简单得多。
- **半八面体替身**：8×8 帧。银杏每帧 128 px，其余树种 64 px，帧间留 2–4 px 间隙，mip 最多 4 级，共 16 个变体。图集 A 存色调标量、AO、透光厚度和覆盖率；图集 B 存球形化法线（八面体编码）和深度。各 2 页 2048²，BC7 约 21 MB。存色调，不存色号，因为色号不能在三帧之间插值。
  - 烘焙器是 C# `[Tool]` 脚本，用 SubViewport 加正交相机渲帧，需要窗口和 GPU，**不能在无界面模式下跑**，要排进 GPU 锁（§12.5）。图集在开发期烘出，随启动集 `boot_<地图>.pck` 发布（需要 GPU 才能出，不在玩家机器上烘，§8）。现成插件不合用：godot-imposter 只能在编辑器里逐个节点手动烘、只烘标准 PBR 通道；wojtekpil 的插件只支持 Godot 3。
  - 运行时走同一个 `toon.gdshaderinc`：用深度重建世界坐标；阴影由引擎在 light() 的 `ATTENUATION` 里给出；ROUGHNESS 写树的材质 ID。不写 `DEPTH`，保留提前深度测试。
- 验收：在切换距离上，L1 与替身的平均明度差不超过 3%，覆盖率差不超过 5%。
- **四季共用这一套替身**：色调读季相行；叶量用世界坐标噪声做阈值 discard。冬季落叶树用 `tree.bare`，覆盖率不低于 0.35。不另烘裸枝替身，也不烘四季各一套（ENG-BUDGET §3）。

**树冠云**：每个 512 m 格离线生成：

1. 用 2 m 体素把树冠做平滑并集；
2. 用 marching cubes 抽面，再简化到约 0.3 三角形/m²（经主代理批准引入 meshoptimizer 的 .NET 绑定，§1.2）；
3. 顶点色存色号，指向该树种的季相行，颜色随季节变化；法线朝簇中心球形化（系数 0.8）；
4. 作为不透明网格，与格子的几何合批。冬季沿法线最多收缩 1.5 m（按标志位只对树冠云顶点生效）。

**树叶**：

- **alpha scissor**：插片着色器同时写 `ALPHA` 和 `ALPHA_SCISSOR_THRESHOLD = 0.5`，材质就留在不透明通道。放大时锐化：`a = (a − 0.5) / fwidth(a) + 0.5`。
- **mip 与覆盖率**：Godot 导入 PNG 时生成的是普通 mip，远处树冠会变稀。两种办法，P0 选一：`pipeline/assets` 直接输出带保覆盖率 mip（Castaño）的 DDS（BC7），Godot 能直接加载 DDS（数组纹理是否支持，P0 核实）；或者在 fragment 里按 `textureQueryLod` 提高 alpha：`a *= 1 + max(0, lod) × 0.25`（`textureQueryLod` 只能在 fragment 里用）。
- **不用 alpha-to-coverage**：用了它的材质会被归到透明通道，而且不开 MSAA 时只有 1 个采样点，等于硬阈值。**不用 alpha hash**：没有 TAA 时是满屏噪点。
- 插片：每张 0.4–0.7 m，贴在团块外 0–0.25 m，朝向 = mix(基础法线, 视线, 0.5)，视线按主相机算；当 |N·V| < 0.25 时缩小，避免侧对镜头时露出一条线。叶卡用 Texture2DArray，每层一种叶、花或枝条笔触，避免图集串色。
- 不需要先画团块再由近到远画插片：深度预通道已经让被团块挡住的插片在颜色通道里不着色。

**明暗分阶与树冠法线**：

- 合成方式与全场一致（ART-STYLE §2）：`ALBEDO = 1`，`EMISSION` 写影色，受光色 − 影色经 varying 传给 light()。植被三阶（深影 / 影 / 受光，阈值 −0.25、0.05）：fragment() 里先按 N·L 在深影与影之间选好 EMISSION，light() 只管"影 → 受光"一档（ART-STYLE §2.5）；深影 = 影的 L × 0.88、C × 0.95，不另登记色名。
- **插片型**的球形法线在 vertex() 里算：`NORMAL = normalize(mix(NORMAL, normalize(VERTEX − 团心), 0.8))`，团心放在 CUSTOM2；插片用所属团块的球形法线，整团的明暗才成片。这一步也进 normal_roughness，树冠内部不出折线。**云团型**的法线向所属小球的球心软化 0.72，生成时烘进网格（ART-STYLE §2.3）；冠内明度梯度 ±8% L，t0 放在 COLOR 的备用字节（§1.5）。
- **透光**走 `BACKLIGHT`：在 light() 方向光分支里乘阈值化后的阴影，被别的物体挡住的叶子不透光；透光色随季节（秋金、春新绿、夏浓绿），冬季裸枝为 0（ART-STYLE §5.6）。
- 云团树冠内部的描线按"内部软线"×0.22（§10.2）。

**每株与每插片的数据**：

- instance uniform 是按节点设的，一个 MultiMesh 共用一份，只适合存"这个节点是哪个树种"。
- 每株参数放 `INSTANCE_CUSTOM`（Forward+ 里是 32 位浮点）：稳定种子（来自实体 ID，不用 `INSTANCE_ID`）、变体、风向上的位置、树高。
- 每张插片的数据放 CUSTOM0–2：插片中心与 h2、枝梢锚点与 h3、团心。这是登记的 VF-C 格式（§2）：压缩 20 B 加 3 × RGBA_HALF，约 44 B/顶点，豁免 ENG-BUDGET §7 第 7 条的 20 B。
- 在 MultiMesh 里，vertex() 的 `MODEL_MATRIX` 已含该实例的变换，fragment 里却只是节点的变换；fragment 需要实例数据时，用 flat varying 传过去。

**风**：

- 分三层（Crysis 方法）：
  - 主干：权重 (y/H)^1.5，最大 2°，弯曲时保持长度不变；
  - 团块：3–6 cm，0.6–1.2 Hz；
  - 叶片：1–2 cm，3–6 Hz。
- 着色器只要用了 vertex()、discard 或 alpha clip，阴影通道和深度预通道就编译同一段 vertex() 代码，所以风、叶量缩放、团块收缩都**自动**跟到影子上，影子和树永远对得上。代价是阴影通道里的距离判断必须用主相机（ENG-BUDGET §7 第 15 条，G4 检查）。
- 相位场：s = t − dot(p.xz, dir) / v，t 是全局参数 `anim_time`，不是世界钟点；第 k 阵风的中心 c = k×15 + hash(k mod 32)×6 − 3。480 s 正好 32 阵，首尾衔接。
- 风量的季节和昼夜系数（春 1.2×、夏 0.8×、秋 1.0×、冬 0.6×，夜间再 ×0.7，SYS-TIME §4）、天气系数（× (1 + 1.2 × 风暴度)，SYS-WEATHER §7）和换季转场时的阵风 ×2.5（SYS-TIME §5）都只乘幅度，不改相位场，所以快进、换季、换天气都不会让阵风错拍。它们由 WP-26 合进全局参数 `wind`。风暴时另加的湍流也用周期 480/k 的正弦叠加，`anim_time` 回绕时首尾衔接。
- **风前沿速度 v 取 10 m/s**，相邻的树依次摆动，间隔约 0.35 s（SYS-MOTION §3.1）。C# 侧的 `IWindField.Sample` 与 `motion_wind.gdshaderinc` 用同一个公式，G2 用同一组输入比对两边的输出（ENG-ARCH §3）。
- "一拍二"在着色器里解析计算：T = floor(`anim_time` × `step_rate`) / `step_rate`，直接算出位置和旋转，CPU 端不做任何事。树冠和草的摆动保持连续，不步进。

**落叶**（按季节换成落叶、花瓣或雪团）：

- **飘落池**：一个 MultiMesh，容量 160，1 次绘制，双面，不投影，`CustomAabb` 罩住全图（`MapDescriptor.Bounds`，切图时重设，SYS-MAPS §3）。每个实例存：原点 = 发射点；自定义数据 = (生成时刻 t0, 类型, 种子, 落地高度)。类型有银杏叶、其他落叶、花瓣、雪团（雪后枝头的雪团，SYS-WEATHER §2）。雪花由降水池画（下文）。
  - 着色器里：age = mod(T − t0, 480)，下落、翻转、横漂都是解析式；读全局纹理 `terrain_height`，到地面就塌缩。没有状态，截图和录像都能复现。没有反向播放：时间不能逆转，换季一律正向（SYS-TIME §5）。
  - CPU 每阵风只写 3–5 个槽位。平时的活跃数随季节和风量变化；换季扬起时用满 160 片（SYS-TIME §5）。行走疾跑时脚边翻起的两三片也从这个池里取（SYS-WALK §1）。画面设置的"降水与粒子密度"按比例缩小可用槽位数（`VisibleInstanceCount`，环形分配只用前 N 个，SYS-GRAPHICS §3）。
  - GPUParticles3D 只作备选：它有状态，截图复现要固定种子并预跑。
- 地面的落叶分三层：
  1. 地面材质读地面遮罩（`ground_mask`），画成平涂的金色、花瓣色或雪（WP-15）；
  2. 20 m 以内的实例叶：和草一样，以 4×4 m 的地块为 MultiMesh 实例，每块一个含数百片叶的网格，在 vertex() 里按距离和季节缩放；近处 80 片/m²，到 15 m 降到 20 片/m²；
  3. 路缘和树池里的堆积，做成不透明的贴面几何（ART-FIDELITY §6、§2"贴面贴花"），颜色读季相行，有无按地面遮罩的季节量做阈值。不用引擎的 Decal 节点（§2）。

**草**：纯顶点着色器生成，以 4×4 m 的地块为 MultiMesh 实例，地块位置随相机按 4 m 吸附、不超过 10 Hz 重写，分三个环：

| 环 | 距离 | 每块根数 | 每根顶点数 |
| --- | --- | --- | --- |
| R0 | 0–5 m | 4096 | 7 |
| R1 | 5–12 m | 1536 | 5 |
| R2 | 12–20 m | 512 | 3 |

- 共 3 次绘制。密度 250 × (5/d)^1.5 根/m²。
- 草叶用地面的法线着色：根部取影色，叶尖取受光色。噪声与草坪贴图共用一套。
- 15–20 m 之间草叶逐渐缩到 0。草不投影（第 6 层），但接收阴影。
- 草地范围是画面设置（20 / 12 m，SYS-GRAPHICS §3）：取 12 m 时不画 R2（少 1 次绘制），草叶在 9–12 m 之间缩到 0；着色器读全局参数 `quality.z`，所以改它不重建实例。
- 草色走季相行，局部发黄用噪声阈值控制；地面遮罩的 B 通道判为盖雪的地方，草叶缩到 0，不许从雪里穿出来。

**阴影**：

- 树干、团块，以及高于 1 m 的绿篱投影；草、插片、落叶不投影（ENG-BUDGET §7 第 14 条）。
- 本体 `cast_shadow = Off`，另建 `ShadowsOnly` 的代理 MultiMesh（第 5 层）：近池用 L1 团块网格，打开 instance uniform `shadow_proxy` 后按噪声挖出 0.4–0.8 m 的洞，形成林荫光斑；远池用团块低模。代理和本体用同一个 `veg_trunk` 着色器，叶量缩放、团块收缩和风自动一致。
- 投影物的选择依据是"它的影子能否落进前三级"，不按"离相机多远"：代理的 visibility range 上限 = 可见上限 + 最长影长（树高 / tan 4°）。远处的树影子很长，会投进近处。
- 树冠云随格子投影。

**季相**（设计见 SYS-TIME §4、SYS-TIME §5；参数和季相色由 WP-38、WP-37 提供，着色器实现归 WP-23）：

原则：不换模型，不重建格子，不重编译。所有季节变化都是连续参数加"阈值生长"，每个元素只在两个好看的端点之间做阈值切换。

- **逐株错相**：φ = 0.6 × hash + 0.4 × 风向上的位置；局部季节 s_i = 环形插值(起点季, 目标季, clamp((进度 − 0.3φ) / 0.7, 0, 1)) + (hash2 − 0.5) × 0.12。插值一律走正向（换季只做正向转场，SYS-TIME §5）：目标季在起点季之前时，CPU 先把它加 4 展开，着色器里再对 4 取模。起止季节和转场进度都从全局参数 `season` 读，不逐帧上传实例数据。
- **叶量 A**（各树种四季的取值见 SYS-TIME §4 和 WLD-VEGETATION §2 的树种表，存在 `tree_species` 纹理里）：A 在两个锚点之间用 smootherstep 插值。
  - 插片：k = smoothstep(h2, h2 + 0.08, A)。落叶树的尺寸 mix(0.6, 1, k)，k < 0.5 时换成叶卡数组里该树种的枝条笔触层（层号在树种表里，经 flat varying 传给 fragment）；常绿树的尺寸就是 k。位置 = mix(枝梢锚点, 插片中心, k)，保证枝梢不悬空。这个缩放与 LOD 前缀的缩放相乘。
  - 团块按 smoothstep(0.15, 0.6, A) 向团心收缩。
  - 季相不增加三角形。冬季插片缩到 0.6，像素反而更少。
- **开花**：h3 < B 的插片读花色行。樱花、美人梅、蓝花楹、玉兰、蜡梅都这样做，不加花朵几何。
- **整株错开变色**：团块的季节值加上团块哈希和高度的小偏移，树顶和向阳面先变。
- **地面遮罩**（RGBA8，导入用无压缩，免得 BC 压缩把阈值推偏）：

  | 通道 | 内容 |
  | --- | --- |
  | R | 银杏落叶潜势 |
  | G | 其他落叶和花瓣 |
  | B | 积雪潜势：草坪 1、步道 0.5、车道 0.2、檐下 0 |
  | A | 保留 |

  写法：v = 遮罩 × 季节量 − 噪声，覆盖 = clamp(v / fwidth(v) + 0.5, 0, 1)。叠放顺序：雪 > R > G > 地面。季节推进时，色块从树根处长出来。B 通道的季节量（`ground_amt.z`）= max(季节基线, 天气累积)，可到 2，所以下雪时步道也会白（WLD-VEGETATION §9.5、SYS-WEATHER §2）；按法线覆雪的 `snow` = min(1, max(季节基线, 天气累积))（WLD-VEGETATION §9.6）。

- **地面遮罩 2**（`ground_mask2`，WP-15）：RGBA8、不压缩，与地面遮罩同尺寸。R 积水潜势（路缘内侧、入口、广场低洼，烘焙生成）、G 建筑遮蔽（檐下、雨篷、连廊为 0）、B 树冠遮蔽（运行时乘叶量）、A 保留。湿润变暗、积水按 R 通道阈值长出和退去，写法与上面相同；着色归 `toon.gdshaderinc` 的天气分支（SYS-WEATHER §7）。地面着色器因此多采一张纹理，不加绘制。
- **按法线覆雪**：fragment 里的 `NORMAL` 是视空间的，先转世界：n = mat3(INV_VIEW_MATRIX) × NORMAL。雪量 = 用同样的 fwidth 阈值，把 sat((n.y − 0.55) / 0.35) × AO × `snow` 和噪声比较。AO 必须烘进顶点，着色器读不到 SSAO。常绿树冠顶积雪就是这样来的。
- **荷花**：东湖、西湖各一个 MultiMesh（约 400 + 200 个实例），主世界 2 次绘制；倒影世界里的两份代理另计，合计 ≤ 4 次。叶、柄、花、莲蓬合成一个网格，部件号放在 CUSTOM0.w；荷叶的尺寸、高度、下垂角，以及花和莲蓬的尺寸，都是 s_i 的函数；冬季只剩少量残梗。荷也进倒影世界（§10.4）。
- **萤火虫**：夏夜，西湖岸、湿地、东湖荷角三个区，只在镜头 60 m 以内出现，50–60 m 淡出。64 只，1 次绘制，走 `fx_glow`（不受光、加法、`depth_draw_never`）。位置按 T 步进，亮度用不步进的 `anim_time`。降水时数量按降水量减少（SYS-MOTION）；画面设置的密度不降它（只占 1 次绘制，SYS-GRAPHICS §3）。
- **雪花**：由降水池画（下文），循环盒半边长 14 m。读路灯网格（§10.3），进入光斑时提亮，不新增纹理。

**降水池**（WP-45b，画面规则见 SYS-WEATHER §3、SYS-WEATHER §7）：

| 项 | 规定 |
| --- | --- |
| 容器 | 1 个 MultiMeshInstance3D，8192 个实例（其中 512 个槽位做溅落），`precip.gdshader`（透明、`blend_premul_alpha`、`unshaded`、`depth_draw_never`，写 `FOG`），第 6 层，不投影；`CustomAabb` 取可达范围"中心 ± 3 km"（`MapDescriptor.Center` ± `roam.maxRadius`，y 取 `floorY`…`ceilY`，切图时重设，SYS-MAPS §3），不用箱庭包围盒 `Bounds`：池子跟着相机走，镜头最远飞到离中心 3 km，用 `Bounds` 时在箱庭外朝外看整池会被剔除。启动时就入树，在雾幕或扉页后面预热（§2）；运行时只改 `VisibleInstanceCount` 和 instance uniform，不改缓冲大小 |
| 循环盒 | p = c + mod(p0 + d − c + h, 2h) − h，c 取 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`（ENG-BUDGET §7 第 15 条）；半边长 h：雨 18 m，雪 14 m。位移 d 不用"速度 × `anim_time`"直接算：C# 每帧按当前风和下落速度累加位移、对 2h 取模，在 T12 跨步时才更新，经 instance uniform 传入。这样改风速、换强度时雨丝不跳，`anim_time` 回绕时也不跳 |
| 数量 | 小雨 1500，大雨 6000–8192，雷雨取上限，大雪 4000；再乘画面设置的"降水与粒子密度"（流畅 50%、均衡 75%、精致和极致 100%，SYS-GRAPHICS §1）。实例按哈希排序，前缀抽稀 |
| 雨丝 | 下落 7 m/s，一拍二每步 0.58 m，丝长取一步长，步进看上去是连续的线；线宽不超过 1.5 px（按输出像素换算到内部分辨率，§10.2）；倾角小雨 5°、大雨 15°、雷雨 25° 并加阵风，全部雨丝同一倾角。透明度按逆光、灯锥、暗背景求，上限 0.35（SYS-WEATHER §3 第 1 条），灯锥读灯光表（§10.3） |
| 遮挡 | `vertex()` 读 `precip_occ`，雨丝低于顶面就缩到 0：檐下、连廊下、屋里无雨 |
| 溅落 | 512 个槽位按哈希落在相机周围 30 m 的网格上，20 m 以内显形，y 取 `precip_occ` 的顶面；水面不画溅落，改画雨点环（`water`） |
| 檐滴 | L0 格的 TileBundle 带离线烘出的滴水点，每格一个小 MultiMesh（同网格、同材质 `precip`），随格子挂载、淘汰；大雨时连成水帘，雨停后再滴 60–120 s |
| 雪花 | 同一个池，下落 0.6–1.0 m/s，横漂读公共风函数；进路灯光斑时提亮 |
| 开销 | 主线程只写几个节点级的量；GPU 增量按 SYS-WEATHER §9（小雨约 +0.3 ms，大雨和雷雨 +0.6–0.8 ms，大雪约 +0.3 ms，独显高 1440p，估计值），记在 ENG-BUDGET §8 的天气一行 |

**预算**：

- E1 视角（站在银杏大道上）约 65 万三角形，在精致预设的植被额度（30% × 250 万 = 75 万）之内；均衡、流畅的额度只有 36 万、21 万，靠关近景几何叶、细节距离 0.8 / 0.7 和密度压下来，G8 按预设核实。季相不改变这个数：夏季满叶与秋季相同，冬季插片只是缩小。
- 绘制调用按 ENG-BUDGET §8 的植被额度（精致主通道约 210 次、阴影约 330 次；极致 280 次、550 次）；预计实际主通道约 120 次、阴影约 200 次。
- 植被阴影的三角形按 ENG-BUDGET §8 的比例（精致约 36 万，极致约 60 万）。
- 均衡、流畅预设关闭近景几何叶；流畅再把草地范围缩到 12 m，植被与草密度取 75%（`VisibleInstanceCount`，SYS-GRAPHICS §1）。近景几何叶的开关在近池下一次重建（≤ 10 Hz）时生效，走抖动淡化；密度变化让被去掉的实例 0.5 s 抖动淡出。
- 过度绘制按 §3.3 测。

## 10 阴影、光照与后期（WP-03、WP-05、WP-24）

### 10.1 阴影（WP-03，引擎原生 PSSM）

SYS-TIME §2 的主光交接、阴影方向钳制、方向量化，SYS-TIME §8 的阴影检查，都以本节为准。

**引擎行为**：引擎在相机移动时已经做了稳定化（每级用包围球拟合，再按纹素取整）；方向光阴影**每帧把所有级全部重渲**，RenderingServer 只提供阴影图尺寸、滤波质量、分级模式、混合、caster mask 这几个接口，**没有降低远级更新频率的办法**。所以阴影开销只能靠减少投影物来省。

**配置**（场景里只有一盏 DirectionalLight3D，名为 KeyLight，由 WP-03 创建；太阳和月亮轮流担任）：

| 设置 | 值 | 说明 |
| --- | --- | --- |
| `DirectionalShadowMode` | `Parallel4Splits`（精致、极致）；`Parallel2Splits`（均衡、流畅） | PSSM 2 时每级只拿到半高的矩形，纹素不等宽。改级数即时、不编译 |
| `DirectionalShadowSplit1/2/3` | 0.03 / 0.12 / 0.40 | 对应 48 / 192 / 640 m |
| `DirectionalShadowMaxDistance` | 1600 m（精致、极致） | 默认格 800 m 以外只剩剪影，远处没有影子看不出来 |
| 均衡、流畅的 2 级 | `MaxDistance` 1000 / 600 m（SYS-GRAPHICS §1），`Split1` 0.15 | 第 1 级约覆盖 0–150 m / 0–90 m |
| `DirectionalShadowBlendSplits` | 开 | 级与级之间混合，不出接缝 |
| `DirectionalShadowFadeStart` | 0.85 | — |
| `DirectionalShadowPancakeSize` | 默认，P0 按低太阳调 | — |
| `LightAngularDistance` | 约 0.45° | 开启 PCSS：770 m 长的影子尾端半影约 6 m；近处影边仍然清楚。P0 比较 0.45° 与 0.3°，取一个固定值（运行时不改），要的是干脆的近处影边（ART-SKYLIGHT §3.1） |
| `ShadowBias` / `ShadowNormalBias` | 0.05 / 4.0 | 引擎已把法线偏移按每级纹素缩放；0.1 / 2.5 在树冠小球交叠处会漏出细亮缝 |
| `ShadowReverseCullFace` | 开 | 闭合网格只往阴影图里渲背面，减少痤疮；双面材质不受影响 |
| `ShadowBlur` | 1.0，固定 | — |
| `ShadowOpacity` | 1；只在无影蓝调、地下和水下，以及天气让阴影强度低于 0.01 时设 0（见下） | 不在特化常量里，改它不重编译 |
| `ShadowCasterMask` | 去掉第 2、4、6、7、9、10 层（§4）；"细小物件投影"关时再去掉第 3 层（雾幕里切）；天气让阴影强度低于 0.01 时整个置 0（见下） | 草、粒子、光晕等不投影；caster mask 在剔除条件里，改它不编译 |
| `SkyMode` | `LightOnly` | 天空不画这盏灯；日月盘由天空着色器按 `sun_dir`、`moon_dir` 画 |
| `LightEnergy`、`LightColor` | 1、白，固定 | 主光的颜色和强度经调色板和 `key_strength` 进卡通材质，不用 `LIGHT_COLOR`；能量保持 1，避免引擎把这盏灯当成无效灯跳过 |
| 项目设置 | `directional_shadow/size` 按预设 8192 / 4096 / 2048，`16_bits` 开，`soft_shadow_filter_quality` 为 Soft Medium，`positional_shadow/atlas_16_bits` 关（ENG-BUDGET §10） | 尺寸用 `RenderingServer.DirectionalShadowAtlasSetSize` 改，只换纹理、不编译，即时生效；16 位和滤波档位四个预设固定，运行时不动 |

**运行时一律不动的开关**：`ShadowEnabled`、`LightAngularDistance` 的有无、`ShadowBlur` 的有无、软影滤波档位、16 位深度。前三项决定是否启用软影的特化常量；软影滤波档位改一次会清空全部已编译管线；位深改了要新管线。阴影是风格底线（ENG-BUDGET §7 第 17 条），所以不做开关。

**随画面设置改的**（SYS-GRAPHICS §3，都是即时类，不编译）：阴影图尺寸、PSSM 2 / 4 级、`MaxDistance` 与 split。最差值 2048²、2 级。自动调节只动阴影距离和级数（§11.3）；"细小物件投影"（第 3 层）只在雾幕里切，避免小件的影子在一帧内消失。

**纹素估算**：按引擎的包围球公式，垂直视场 60°、16:9，8192 的阴影图每级 4096²：

| 级 | 范围 | 纹素（8192） | 纹素（4096） |
| --- | --- | --- | --- |
| 1 | 0–48 m | 约 3.0 cm | 约 6 cm |
| 2 | 48–192 m | 约 12 cm | 约 23 cm |
| 3 | 192–640 m | 约 38 cm | 约 77 cm |
| 4 | 640–1600 m | 约 0.95 m | 约 1.9 m |

行走模式的视场 70°，纹素约大 20%。栏杆、灯杆在地上拉出的条纹长影（ART-FIDELITY §6）由第 1 级给出。

**方向量化**（KeyLightDirector，WP-03，`ProcessPriority` −900）：

1. 输入：WorldState 给的美术太阳或月亮方向（SYS-TIME §2）和主光角速度 ω（`IWorldState.Omega`）。
2. **阴影方向高度钳**：阴影方向的高度不低于 4°。日盘可以继续下落，两者最多差 4°，看不出来。天空着色器里的日月盘用不钳制的 `sun_dir`、`moon_dir`。介质为 Interior 时目标抬到 14°（12–16°，P0 看图定，§16），0.5 s 过渡、不量化，进度读 `interior_light.z` 的同一份 C# 状态；出楼还原。这样 17:30 普通窗的光斑也能落到地板上，代价是从窗里看出去的室外影子变短（ART-SKYLIGHT §3.1、WLD-INTERIORS §6.3）。
3. **量化**：在方位和高度上分别量化，目标方向跨过一步才转灯，并带 0.25 步的回差。步长取"该级最长的影子移动 0.5 个纹素"对应的角度：
   - 方位步长 δ = 0.01°：第 1 级 48 m 内的影子点最多移 0.84 cm，不到 0.3 个纹素。
   - 高度步长 δ_h = δ × max(sin 2α′, 0.1)，α′ 是钳制后的阴影高度。影长 L = H / tan α′ 对高度的导数放大了 1/(sin α′ cos α′) 倍，α′ = 5.5° 时约 10.5 倍；按 δ_h 走，影子上离投影物 L 处的点每步移动约 2Lδ，影长不超过该级范围时约 0.5 个纹素。
   - 例外：从远处投进近级的长影（例如背后 300 m 外的高楼投到镜头前），每步会移动 2–4 个第 1 级纹素（2 × 300 m × 0.01° ≈ 10 cm）。这部分落在 PCSS 的软尾里，再经卡通阈值的 smoothstep 收边，P0 的 A/B 专门看这种情形。
   - 估算：1× 流逝时太阳每分钟约走 0.2°，方位约每 3 s 跨一步（SYS-MOTION §2）；黄昏低太阳时高度步更密，约每 0.2–0.5 s 一步，每步不到半个纹素。
4. **快速变化**：ω > 1°/s（拨表盘、点刻度或预设的前向快进、换季转场、360× 和 1440× 流逝）时不量化，每帧连续转动；ω 降到 0.7°/s 以下并保持 0.5 s，才回到量化，回去时吸附到最近的一步（跳动不到半步）。
5. 只在转灯时写 KeyLight 的 `GlobalTransform` 和全局参数 `key_dir`。明暗交界线和影子在 light() 里用的是同一个内置 `LIGHT` 方向，天然同步。
6. **P0 做 A/B**：量化转灯和"始终连续转动"哪种更不显眼（后者没有跳步，但影边会随纹素网格一起爬动），在秋 17:30 的银杏大道和宿舍区各看一次，定下 δ。

**太阳贴地**：

- **主光强度** = smoothstep(−2°, 4°, h)，h 是太阳的真实高度，写进 `key_strength`。light() 的方向光分支乘它，不改 `LightEnergy`。
- **退光线**：太阳高度从 2.5° 降到 0.3° 时，`retreat_y` 从 −10 m 升到 80 m。vertex() 里算世界高度 `v_wy = (MODEL_MATRIX × vec4(VERTEX, 1)).y + retreat_bias`；light() 的方向光分支把受光乘以 smoothstep(R − 1.5, R + 1.5, v_wy)，3 m 软边。这样复现 ART-SKYLIGHT §3.4 的退场顺序：地面 → 低层 → 高层 → 穹顶。日出时反向，穹顶最先受光。
- **穹顶偏置**：instance uniform `retreat_bias` 只在宣邦楼穹顶的实例上设为 6 m，所以**穹顶必须是单独的实例，不能并进块几何**（WP-18 交付时注意）。
- **掠射面**：light() 里再乘 smoothstep(0, 0.06, N·L)，把容易出痤疮的掠射面整体判进影里；引擎的偏移不能逐片元改，所以由这一条和 `ShadowNormalBias` 共同防痤疮。

**无影蓝调里的日月交接**（SYS-TIME §2）：

1. 太阳高度从 −2° 降到 −7° 是"无影蓝调"：`shadow_strength` = 0，同时 KeyLight 的 `ShadowOpacity` 设 0。opacity 为 0 时着色器直接跳过阴影采样（源码门限 > 0.001）。
2. 在这段里任选一帧，把主光转到月亮方向（同样钳在 4° 以上），颜色和强度取调色板的值。引擎这一帧就把全部级联重渲完。
3. 太阳从 −7° 降到 −12° 时，先把 `ShadowOpacity` 恢复为 1，再让月影经 `shadow_strength` 淡入到 0.45；月亮高度低于 15° 时月影为 0，15°–20° 之间再乘一个淡入。黎明按相反顺序执行。
4. 月影的 0.45 **不能**用 `ShadowOpacity` 实现：opacity 是在阈值化之前和 1 混合的，0.45 会让影子里的值变成 0.55，越过卡通阈值 0.5，影子被当成受光。所以在 light() 里写 mix(1, 阈值化后的阴影, `shadow_strength`)。
5. 快进时如果一帧就跨过整段蓝调，就在这一帧转向，并按 `shadow_strength` = 0 处理。
6. 月光方向同样走量化。
7. 这样不会出现"影子在某个钟点突然从另一个方向冒出来"。

**天气**（SYS-WEATHER §7；WP-03 在主光那一步 −900 执行）：

1. **投影乘子**：`shadow_strength` 再乘 (1 − 0.8 × 阴度)，阴度读天气向量（全局参数里是 `weather_a.y`）；满阴时乘子正好是 0.2，所以白天不低于 0.2，保证两阶不丢（SYS-WEATHER §3 第 4 条）。雨、雷雨的夜里没有月影（SYS-WEATHER §2），月影直接为 0。
2. **空跑**：最终的阴影强度低于 0.01 时，设 `ShadowOpacity = 0`（着色器跳过阴影采样，源码门限 > 0.001）并把 `ShadowCasterMask` 置 0（剔除时去掉全部投影物），阴影通道空跑，天气越重越省。这两项都不是特化开关；P0 核实切换前后 SURFACE、DRAW 计数不增（G16）。
3. **恢复**：转晴、月影回来时，先在强度仍为 0 的那一帧恢复 mask 和 opacity（引擎当帧就重渲全部级联），下一帧起再让 `shadow_strength` 淡入，所以影子不会缺一帧。
4. **哪些时候空跑**：白天的乘子不低于 0.2，白天的阴、雨到不了 0.01，阴影通道照常跑、按满额核算；空跑只发生在无影蓝调、雨夜（无月影）、地下和水下。冬夜下雪时月影变淡但仍在，不空跑。
5. **天光方向**：阴、雨、雪时，toon 的 light() 用 `normalize(mix(LIGHT, mat3(VIEW_MATRIX) × overcast_dir, 阴度))` 求 N·L（light() 里可读 `VIEW_MATRIX`），过渡带宽从 0.02 放到 0.06，仍是两阶。`overcast_dir` 由 WP-03 在转灯时写，取量化后的主光方位、仰角 55°，所以明暗交界线和影子仍然同步。主光方向本身不因天气改变。
6. **云影**不是阴影图：地面片元沿主光方向求与 1.5 km 云平面的交点、采 `cloud_noise`，乘进退光线乘子；太阳低于 10° 淡出（SYS-WEATHER §7）。不占阴影通道，不加绘制。

**降开销**（只能减少投影物）：

- **代理**：远处体量和植被用 `ShadowsOnly` 的代理（第 5 层），本体 `cast_shadow = Off`。代理比本体简化，并向内收缩，收缩量不小于法线偏移。导入的英雄资产开 shadow mesh；`shadow_mesh` 只能提供"焊接过的同一组顶点"，不能拿来当简化代理。
- **按大小限投影距离**（visibility range 对投影物同样生效）：高 ≥ 0.25 m、宽 ≥ 3 cm 的小件（第 3 层）只投到 200 m；高 ≥ 1.5 m 的投到 640 m；只有体量和树冠团投到 1600 m。按物体的距离自然落进对应的级，不需要按级挑投影物。流畅预设关掉细小物件投影（SYS-GRAPHICS §1）；均衡、流畅的阴影距离只有 1000 / 600 m，远处的投影物自然被裁掉。
- `ShadowCasterMask` 排除草、粒子和光晕；投影物必须在相机能看到的图层上：裁剪条件是"相机可见层 & 物体层 & caster mask"。
- 阴影通道用主相机选出的 LOD。
- 植被的投影见 §9。投影物的绘制调用和三角形按 ENG-BUDGET §2 和 ENG-BUDGET §8。
- **可选的远影图**：只在 P0 证明第 4 级不够用时才做。用一个 SubViewport 加沿光方向的正交相机渲染深度代理，只在量化步时设 `UpdateMode.Once`，在 max_distance 以外由 `toon.gdshaderinc` 采样。

**不做接触阴影**：不做 SSAO、屏幕空间接触影、地面轮廓距离场色带、道具接触贴片和动物接触球；接地感由描线的物体分界线（ART-STYLE §4.1）和真实投影给出。地面遮罩的 A 通道保留不用，不设全局纹理 `contact_spheres`，WP-25 不写接触球（§1.5）。顶点 AO 只用于覆雪和窄凹处的阈值偏移（窗洞内壁、檐底 0.5 m 内、门洞、栏杆缝，最多推 0.3，颜色仍是同一个影色）。

**楼内、地下和水下**：

- 楼内：阴影本来就每帧重渲，不需要"停止更新"；竖缝窗的光条由第 1 级给出。镜头在开放室内时（WLD-INTERIORS §6.3、WLD-INTERIORS §8.3），介质服务把第一级阴影范围缩到 8–12 m，0.5 s 内过渡，出楼恢复（改级联分割和阴影距离都是即时类、不编译，SYS-GRAPHICS §3 登记为"介质覆盖"），窗格光斑才不糊；同时把阴影方向的最低高度抬到 14°（§10.1"方向量化"第 2 条）。室内只有墙和楼板投影，家具、踢脚、窗框内侧、灯片不投影；玻璃不投影，所以窗格光斑是真实阴影的结果。
- 地下和水下：相机 `CullMask` 去掉地上各层，被隐藏的层的影子随之消失；同时 `shadow_strength` 和 `ShadowOpacity` 设 0，倒影视口设 `UpdateMode.Disabled`，关闭光斑和移轴。回到地面时借 0.25 s 的介质过渡恢复，不用补渲。

**验收**（配合 SYS-TIME §8 的无硬切检测和门禁 G13）：

- 量化转灯：每跨一步，各级阴影边缘的位移不超过 0.5 个纹素；不闪；明暗交界线和影子同步。
- 连续转动（快速变化）：影子可以连续扫过，不查单帧位移；回到量化时没有一帧明显的跳变。
- 跨越日落、日出：主光方向只能在 `shadow_strength` = 0 的帧里换成另一个天体，任何一帧都不得出现影子方向的突变。
- E1 视角下，楼脚背阴面的漏光缝小于 1 px；低太阳下平整墙面上没有碎影和条纹（ART-FIDELITY §7 第 10 条）。
- 室内阴影方向抬高的 A/B 截图（12°、14°、16°，秋 17:30 的教室和走廊，P0）。

**显存**：8192² × 16 位 = 128 MB（极致）；4096² 为 32 MB（精致）；2048² 为 8 MB（均衡、流畅）。没有前一版副本。

**开销**：夏正午满叶（晴）时 4 级全量重渲的 GPU 时间，是 P0 的头号测量项；份额按 ENG-BUDGET §2 的 22%，在精致预设下核算。超出时依次收紧：小件投影距离、植被代理密度、第 4 级的 max_distance。白天阴雨不省阴影（§10.1"天气"第 4 条）。

### 10.2 后期链（WP-05）

**顺序**（ART-STYLE §7）：

| 序 | 通道 | 实现 | 开销（1440p，估计值） |
| --- | --- | --- | --- |
| 1 | 深度预通道：深度 + normal_roughness（ROUGHNESS 里是 7 位材质 ID） | 引擎 | 算在不透明里 |
| 2 | 不透明物体：卡通材质，雾在材质里算 | 引擎 | — |
| 3 | 天空：贵的云放进 half-res 通道 | 引擎 + `sky.gdshader` | — |
| 4 | Compositor `PostSky`：描线（两次 dispatch：线遮罩 → 斑点去除、帐篷抗锯齿与合成）、屏幕空间边缘光，雨天的远雨幕带、雷雨的闪光帧加深 | `post_outline.glsl` | 0.3–0.5 ms |
| 5 | 透明物体：近景透明窗、水的透明变体（可选）、粒子（含降水池）、光晕、室外软边光柱、室内棱柱光柱、细尘、介质覆盖层；贴面贴花在第 2 步的不透明物体里，备选的 `toon_decal` 启用时排在本步最前（§1.6） | 引擎 | — |
| 6 | Compositor `PostTransparent`：移轴（1/2 或 1/4 分辨率可分离模糊，按屏幕 y 与深度合成，混合上限 0.25）、高光肩部（调色板登记的受光色在肩部前后 ΔE ≤ 2，只压柔光与高光的溢出部分）、暗角（并进移轴合成那次 dispatch，+0.02 ms 量级）、主光可见度与光斑 | `post_tiltshift_*.glsl`、`post_flare.glsl` | 0.3–0.5 ms |
| 7 | 内置 Glow：白天只晕高光，黄昏以后叠全帧柔光（两路取值见下"柔光"；质量高 / 标准） | 引擎 | 0.3–0.5 ms |
| 8 | Tonemap Linear | 引擎 | — |
| 9 | 调整 + LUT：`AdjustmentColorCorrection` = 运行时 LUT（基表烘入分调与饱和，见下） | 引擎 + `post_lut.glsl` | < 0.05 ms |
| 10 | 去色带抖动（`Viewport.UseDebanding = true`）、抗锯齿（精致、极致 SMAA 1x，均衡、流畅 FXAA）、FSR1 放大（按渲染比例） | 引擎 | 0.2–0.5 ms |

合计约 1.3–2.4 ms，按 ENG-BUDGET §2 的后期份额 15% 核算，P0 实测。界面画在这条链之后（`hdr_2d` 关），不吃暗角、分调、高光肩部和 LUT。

- **Compositor 的接入**：`CompositorEffect` 的 `EffectCallbackType` 设为 `PostSky`，`NeedsNormalRoughness = true`（只在 Forward+ 有效），在 `_RenderCallback` 里用 `RenderData.GetRenderSceneBuffers()` 取 `RenderSceneBuffersRD`，再用 `GetTexture("forward_clustered", "normal_roughness")`、`GetDepthLayer`、`GetColorLayer` 取缓冲。调色板在计算着色器里用 `RenderingServer.TextureGetRdTexture` 拿到 RD 纹理。RID 要手动释放；渲染线程上访问共享数据要加锁。
- **Tonemap 用 Linear**：它不改动颜色数据，调色板颜色能原样上屏。
- **柔光**：4.6 起 glow 在 tonemap 之前混合，默认 Screen 模式。两路共用一个 Glow：`GlowBloom` 管全帧柔光，`GlowHdrThreshold` 和 `GlowHdrScale` 管高光晕开。每帧按太阳高度 h 和天气只改属性值、不重编译（ART-STYLE §7.1）：

  | 情况 | `GlowBloom` | `GlowHdrThreshold` | `GlowHdrScale` | `GlowIntensity` | 级 |
  | --- | --- | --- | --- | --- | --- |
  | 白天（h > 6°，晴、少云），含默认格 17:30（h 5.5°） | 0.12 × smoothstep(6°, −7°, h)；默认格留 0–0.04 的余量，默认 0.02 | 0.85 | 0.3 | 0.6 | 2–4 |
  | 日落到蓝调（6° → −7°） | 0 → 0.12 | 0.85 | 0.3 | 0.6 → 0.35 | 3–5 |
  | 夜间 | 0.12–0.15 | 0.85 | 0.3 | 0.30 | 3–5 |
  | 雾、雨（叠加） | +0.05–0.08 | — | — | +0.05 | — |
  | 雷雨闪光帧（SYS-WEATHER §5）、换季雾幕 | +0.1 | — | — | — | — |

  - 阈值取 1.0：只晕开大于 1 的真高光；高光肩部（序 6）只压大于 1.0 的溢出，调色板登记的受光色前后 ΔE ≤ 2（ART-STYLE §7.2）。春季 +30% 乘在 `GlowIntensity` 上。
  - 换级用 `SetGlowLevel` 在 0.5 s 内渐变，不硬切。观感 P0 看图确认（§16）。

  Glow 本身常开（风格底线）；画面设置的"柔光质量"只改 glow 级数和双三次放大（`RenderingServer.EnvironmentGlowSetUseBicubicUpscale`），即时、不编译。要去掉的那一级用 `SetGlowLevel` 在 0.5 s 内把强度渐变到 0，不硬切；双三次放大的开关差异很小，放在渐变结束的那一帧。
- **CDL 与 LUT**：内置没有 CDL。32³ 基表保持静态；CDL 四个参数按关键帧插值（SYS-TIME §3.1），每帧（或变化超过阈值时）用一次 32³ dispatch 把"CDL ∘ 基表"写进运行时 LUT，赋给 `AdjustmentColorCorrection`（它接受 Texture3D，Texture3DRD 能否直接用由 P0 确认）。LUT 在 linear_to_srgb 之后执行，而且只有 Adjustment 开着时才执行，所以 Adjustment 开、亮度对比度饱和度都为 1。备选：在 `PostTransparent` 里直接算 CDL。基表烘入分调（暗部 ×(0.97, 1.0, 1.04)、亮部 ×(1.03, 1.0, 0.97)）和饱和 1.02；CDL 默认值按 ART-STYLE §7.2（白天 slope 0.96、offset +0.03，黄金 slope 0.97、offset +0.02、sat 1.03），offset 就是"抬黑"，让影和暗部清透。
- **高光肩部与暗角**（ART-STYLE §7.2、ART-STYLE §7.8）：Godot 4.7 的 `Environment.ToneMapper` 只有 `Linear`、`Reinhardt`、`Filmic`、`Aces`、`Agx`，没有 Khronos PBR Neutral，所以只借它的高光压缩段，在移轴合成那次 dispatch 里自写：峰值 ≤ 1.0 的调色板颜色原样上屏，> 1.0 的溢出（柔光、高光、灯）按 Neutral 肩部压回、不爆白，暗部不偏移。暗角是风格默认、可关：强度白天 0.10、夜间 0.12，色调白天 (0.90, 0.91, 0.95)、黄金 (0.92, 0.90, 0.93)，强度与色调由 C# 经 push constant 写。
- **主光可见度与光斑**：第一次 dispatch 在主光屏幕位置周围采 32 个深度点（反向 Z 下天空深度为 0），结果写进存储缓冲；第二次 dispatch 画 1 条横条和 5 个光圈，不透明度不超过 25% × 可见度。全程不需要 CPU 回读。光斑只跟太阳；月亮只在天空着色器里画光晕（ART-STYLE §7.3）。雾天日盘隔雾可见但不出光斑（SYS-WEATHER §2）：可见度随天气的雾密度倍率淡到 0。
- **随时段和天气变化的后期参数**只改 Environment 的属性值和 Compositor 的 push constant，不切 `*Enabled`，不重编译。
- **移轴、光斑、光柱、暗角可关**（风格默认，SYS-GRAPHICS §2）：移轴的 1/2 和 1/4 分辨率两套计算管线、光斑的两次 dispatch 都在启动时建好；玩家关掉时，先在 0.5 s 内把强度（移轴的模糊半径、光斑的不透明度、光柱与细尘的亮度、暗角强度）淡到 0，再关掉对应的部分：光斑把 `CompositorEffect.Enabled` 设 false，光柱节点 `Visible = false`（节点启动时就在树里，已预编译）。打开时反过来。自动调节不关它们（§11.3）。
  - **移轴合成那次 dispatch 不随移轴关掉**：高光肩部（风格底线）和暗角都并在它里面，所以移轴关时只跳过两次模糊 dispatch、混合权重取 0，合成 dispatch 照跑；暗角关时只把 push constant 里的强度降到 0。两者都不切 `Enabled`、不改管线。

**描线**（CompositorEffect 里的计算着色器）：

- **材质 ID**：ROUGHNESS 写 ID / 127。normal_roughness 的 a 通道 = roughness × 127/255，动态物体取 1 − a；解码时 a > 0.5 就取 1 − a，ID = round(a × 255)。数值模拟里 ID 0–127 在静态、动态两种情况下都无损往返，fp16 输入也无损。
- **三个前提**：关闭 `screen_space_roughness_limiter`（它默认开，会按法线导数改写 roughness，把 ID 改坏）；不开 MSAA；深度预通道保持开启（ENG-BUDGET §10）。
- **ID 的用法**：ID 查全局纹理 `mat_table` 的行 0（§1.5；计算着色器用 `RenderingServer.TextureGetRdTexture()` 读），取线宽系数、染色强度、擦除位、"只描外轮廓""内部软线""不描"标志和小件淡出距离。两侧 ID 不同时，取深度更近一侧的 ID 定色。物体轮廓靠深度判断。
- **两次 dispatch**（ART-STYLE §4.5）：第一次只算线遮罩（覆盖率、线宽、取色那一侧的材质 ID），写进一张 RG8 中间纹理（`RenderSceneBuffersRD.CreateTexture`，缓冲重建时一起建，§11.7）；第二次做孤立斑点去除（3×3 内线像素少于 3 个就丢）、沿边缘法向 ±0.5 px 的三点帐篷滤波作覆盖率，再按下面的线色混色。约 +0.08–0.15 ms（1440p，估计值）。
- **线画在前景一侧**：外轮廓和分界线一律画在前景物体（离镜头近的一侧）里面，不跨在边界上居中；这样取色不会读到另一侧的颜色，也不出锯齿。抗锯齿的帐篷滤波默认关（开了线会变浅变糊），只在 P0 看图时对比。
- **提前退出**：天空、距离超过 400 m 的像素直接跳过。不按移轴的模糊圈提前退出：移轴混合上限 0.25（ART-STYLE §7.4），按模糊圈退出会让上下边带里的线无故消失。
- **采样**：取 4 个对角点，偏移 max(0.5, 线宽/2) px，总共读约 10 次纹理。
- **深度判据**：用法线外推平面，相对差超过 1.5% 才算边，避免把贴地斜看的地面误描成线。**法线判据**：软阈值，折角线的覆盖率 = 1 − smoothstep(0.62, 0.78, dot(n_a, n_b))，即夹角约 39° 以下不出线、52° 以上全出线，中间按覆盖率淡入；线随距离和倒角宽度平滑淡出、不闪（ART-STYLE §4.6）。建模避开 40°–50° 的硬折角，例如八棱柱、45° 坡屋面与墙的交角。单段 45° 倒角把 90° 阳角拆成两个 45° 的折，正好落在过渡带里，只出半条线，所以近处的阳角主要靠倒角提亮和明暗交界读出（§2），远处由凸边亮线接替（下文）。**ID 判据**：ID 不同就描材质交界线，也就是物体分界线（道具与地面、墙与地面，接地感由它和真实投影给出，ART-STYLE §4.1）；同材质的两件物体相接不描。
- **线色**：本色加深 + 冷暖染色，线性空间里算（ART-STYLE §4.2）：底色先向自身亮度混 0.35 再乘 `line_darken`（默认 0.15，按关键帧，SYS-TIME §3.2），然后按底色在 OKLab b 轴上的冷暖混 0.22 的 `line.warm`（暖棕）或 `line.cool`（青灰），最后过雾。受光面上的线 L 约 0.49（浅线）。`line_darken` 和两个染色色由 C# 每帧写进 push constant（染色色从调色板取）。夜间仍比影暗 0.06–0.10。
- **线宽按内部分辨率换算**（SYS-GRAPHICS §2）：描线在 `PostSky` 里、FSR1 放大之前按内部分辨率执行，所以线宽按上屏效果定义、换算到内部像素。外轮廓名义半径按线性深度插值：30 m 以内 0.625 px（约 1.25 px 宽），60 m 0.55 px，150 m 0.375 px，400 m 以外不画；主折角 ×0.8，物体分界与材质交界 ×0.6（ART-STYLE §4.3，以 1080p 为基准）；贴脸上限 2 px。内部半径 = 名义半径 × 内部高度 / 1080。内部不足 1 px 的线按覆盖率画。**最小宽度**：名义宽度 ≥ 1 px 的线（约 90 m 以内的外轮廓）内部宽度不小于 1 个内部像素、全覆盖；更细的线照常按覆盖率淡出。采样偏移、边缘光的 1.5 px 偏移同样乘"内部高度 / 1080"。P0 在 0.5、0.59、0.67、1.0 和超采样 1.25 五档比例下截图比较描线，最小宽度以截图为准（§16）；G17 判描线像素占比（§12.5）。
- **轻重变化**（ART-STYLE §4.4）：强度 0.85–1.0、线宽 ×0.9–1.1，按世界坐标 0.4 m 格的哈希（由深度重建世界坐标，加材质 ID 做种子），锚在几何上，镜头移动时不游、不闪。
- **内部软线**：两侧都是"内部软线"类（云团树冠）时 ×0.22；外轮廓不受影响。**小件淡出**：大量重复的小件按 `mat_table` 里的淡出距离淡掉线。**不描类**（水面、透明玻璃、贴面痕迹、粒子、花瓣、草叶、落叶、雨丝、云卡）：邻点是"不描"时按同深度处理，免得花瓣贴在墙上勾出轮廓。
- **超采样**：默认不做（Godot 拿不到参考场景那种 1.6 倍 G-buffer）。极致预设可选 `Viewport.Scaling3DScale` = 1.25、`Scaling3DMode = Bilinear`（大于 1 只对 Bilinear 有效，属超采样、代价高）；P0 测开销，要加时在 SYS-GRAPHICS 登记，属雾幕类设置（§11.7）。
- **线色随距离过渡到雾色**：用 `fog.glslinc`（与材质的雾函数逐行对应）；雾和介质参数由 C# 每帧写进 push constant。
- **边缘光**（屏幕空间版，并进描线这次 dispatch）：沿屏幕上的主光方向偏移 1.5 px 取深度，邻点是远景或天空、且该像素背光时，叠上 rim 色；宽度 1–1.5 px，与线宽相配。不用内置的 `RIM`（它随粗糙度变）。雨后初晴时边缘光 ×1.3（SYS-WEATHER §2），只改 push constant。受光侧的剪影暖边在物体空间里算（`toon.gdshaderinc`，ART-STYLE §5.3），不在这里。
- **远雨幕带**（雨天，SYS-WEATHER §3 第 2 条；并进描线这次 dispatch，不另加通道）：中景 25–400 m 没有雨丝，只在 25、45、100 m 三个层距上做竖条纹调制：像素的线性深度超过层距才叠这一层斜条纹（倾角与近景雨丝相同；雪是点），明度取背景 ±0.12，不用白色，按 T12 下移；强度读 push constant 里的远雨幕量（`weather_fog.w` 的同一份数值）。天空像素和 400 m 以外的像素随描线一起提前退出。远处云下的雨幕前锋画在天空着色器里（WP-40）。开销记在 ENG-BUDGET §8 的天气一行。
- **闪光帧**（雷雨，SYS-WEATHER §5）：toon 在 fragment 里加 `EMISSION += 闪光 × (受光 − 影) × (0.5 + 0.5·n.y)`，全场一瞬间翻到受光色、没有投影；描线在闪光帧加深 30%（即 `line_darken` × 0.7），`GlowBloom` 加 0.1。闪光量由 WP-40 写进 `lightning.x`，Compositor 从同一份数值填 push constant。主光不参与（改能量无效，转向会让影子扫一遍），阴影图不重渲。节奏、"每秒不超过 3 次闪光"、全画面平均 ΔE ≤ 2 和"减少动态效果"时关闭，都按 SYS-WEATHER §5。
- **不用的替代方案**：stencil（4.5 起支持但标为实验性，Compositor 拿不到主渲染的 stencil）；SubViewport 渲 ID（相当于整场景再画一遍）；next_pass 外扩壳。
- **凸边亮线**（ART-STYLE §4.7；倒角在远处的替身，ART-FIDELITY §3.5）：倒角窄于约 1.5 px 以后，着色器的倒角提亮（§2）读不出来，改由描线这次 dispatch 接替，一直画到 150 m（120–150 m 淡出）。倒角宽的时候法线渐变，采样跨不出折角，既不出线也不出亮线；窄到采样能跨过时才判出折角，两者自然衔接。
  - 第一次 dispatch 判出折角线时，用两侧采样点由深度重建的视空间位置 P 和法线 N 判凸凹：(P_b − P_a)·N_a < 0 为凸；再用 push constant 里的主光方向判哪一侧朝光（N·L > 0）。只对 `mat_table` 行 3 开了倒角提亮的材质判。"凸、且有一侧朝光"记一位，放在中间纹理里材质 ID 那个通道的最高位（材质 ID 只用 7 位）。
  - 第二次 dispatch 在朝光一侧紧贴暗线画 1 px 亮线，颜色取该侧像素的现有颜色提亮（初值 +0.05 L，与倒角提亮同幅），受光时就是受光色提亮一阶。计算着色器读不到阴影衰减，分不出朝光面是否落在投影里：落在投影里时取到的是影色，只略提一点，不会亮出受光色。ART-STYLE §4.7 要的"影里不画"在这里近似成"影里很淡"，够不够由 P0 看图定（§16）。暗线照常画，亮线并排；是否改成只画亮线，同样 P0 定。
  - 它满足 ART-FIDELITY §7 第 1 条的"边缘高光"，所以小件的几何倒角只放在 L0+，L0 由它代替（ART-MODELING §7.2）。
  - 两侧的深度和法线第一次 dispatch 本来就读，亮线不加通道、不加纹理，开销并进描线的两次 dispatch。

**抗锯齿**：

- 精致、极致用 SMAA 1x（4.5 起内置），均衡、流畅用 FXAA（SYS-GRAPHICS §1），设置里可换；属雾幕类，只重建缓冲、不编译（SMAA 冷启动第一次约 40 ms，在启动预热里吃掉）。FXAA 会自动给 mip 偏移 −0.25。
- 不用 TAA：一拍二步进、风动顶点、细描线都会拖影。不用 MSAA：植被开销大；运行中切换会重编管线；ID 和法线没法 resolve。
- 内置的 FXAA 和 SMAA 都不能跳过描线像素，所以描线自带覆盖率抗锯齿。P0 检查 SMAA 会不会侵蚀 1 px 的线；会的话，关掉内置抗锯齿，启用 `post_fxaa.glsl`（在 `PostTransparent` 里跳过描线像素）。切换只在启动或雾幕时做。

**窗灯光晕**：不用屏幕空间模糊。用 MultiMesh 面片（`fx_glow`），离立面 2 cm，边长为窗宽的 1.5 倍，加法混合，250 m 外淡出，满足 ART-STYLE §7.5 的"半径不超过窗宽的一半"。`INSTANCE_CUSTOM` 存窗号、通道、阈值；亮度由着色器读 `light_channels` 和 `window_override` 算出，没亮的窗在 vertex() 里塌缩成 0 面积，不占光栅化。每盏灯每帧零 CPU 开销。夜间亮窗比例最高到 0.85（SYS-TIME §6），GPU 帧时计入 ENG-BUDGET §8 的"天空与特效"。归 WP-39。

**光柱**（ART-STYLE §7.6、ART-SKYLIGHT §7 第 5、9 项）：都走 `fx_glow` 的分支，玩家可以关（风格默认，见本节"移轴、光斑、光柱、暗角可关"）。

- **室外软边光柱**：沿 −`key_dir` 的开口圆柱或扁片（底半径 0.8–3 m），`blend_add`、`unshaded`、`depth_draw_never`、`fog_disabled`；软边 `pow(abs(dot(N, V)), 1.6)`；读 `hint_depth_texture` 在 1.4 m 内软化交界；沿轴呼吸 ±18%；显示空间峰值 0.15–0.22；颜色近窗处偏白 `shaft.near`，往远处变成主光的高彩色 `shaft.far`。**近镜 1.2 → 5.5 m 淡入**、**视线与光柱轴夹角 < 30° 时 × mix(1, 0.4, fwd²)** 是必需项（17:30 光柱接近水平、正在人眼高度），参数读全局参数 `shaft`。放置点由 WP-23 / WP-39 的生成器按"树冠缝、楼缝朝太阳"挑选，每屏不超过 8 根；按太阳高度 2°–30° 淡入。
- **室内棱柱光柱**：每块受光窗格一根棱柱（WLD-INTERIORS §8.3）：片元只做解析求交和 2 次噪声，不采阴影图（空间着色器读不到方向光阴影图），家具遮挡忽略，由生成器按楼板和梁切开棱柱；每间房的棱柱合成 1 次绘制，同屏最多 3 组，屏幕覆盖率 ≤ 0.6 屏（P0 实测）；镜头在楼内时整体 × `interior_light.y`（0.55），计入室内主通道的绘制额度（WLD-INTERIORS §12）。
- **细尘**：MultiMesh 小方片，每屏 ≤ 600 粒、单间 ≤ 300 粒，按受光玻璃面积给，亚像素时按覆盖率淡出，闪烁按一拍二步进；1 次绘制。
- **天气**：雾天 ×1.5，阴天只出现在云缝里（SYS-WEATHER §2）；雨中的白天不画，**太阳雨**例外：太阳可见时照画，光柱里的雨丝亮成金色（ART-SKYLIGHT §7 第 12 项）。

不用体积雾：写实感重、开销约 1–3 ms，而且写了 `FOG` 的材质会整段跳过体积雾。

### 10.3 窗灯与路灯（WP-03 负责通道与着色，WP-39 负责时刻表）

- **窗编号**：玻璃 UV 按窗格排布。窗号 = 楼基址 + 行 × 列数 + 列。建筑按块合并以后，楼基址只能放在顶点属性里（UV2 或 CUSTOM0，即 ENG-ARCH §3 的 `GeometryBundle.Uv2`），不能用 instance uniform。
- **亮灯时刻**：每扇窗的阈值 θ_i = hash(窗号)，与通道值 L 比较。默认状态的黄昏（17:22–17:40 前后，太阳约 6.9° → 3.7°）里，全校窗灯的 L 从约 20% 走到约 70%。L2 的窗带用同一个 hash，所以远近切换时窗灯不跳变。
- **灯光通道**：32 个，按"建筑类别 × 灯种"划分（宿舍、教学楼、图书馆、科研楼、食堂等 × 窗灯、泛光、路灯、景观灯、球场灯、店招），由 WP-39 在按图的 `game/data/maps/<地图>/lighting-schedule.json` 里登记；时刻表、灯表、窗覆盖按图，切图时 WP-39、WP-03 经 `IMapAware` 换成新图的（SYS-MAPS §5）。外围和开放室内都接进这些通道，不新增通道（WLD-OUTSKIRTS §4.4、WLD-INTERIORS §8.2）。每个通道存两个量：值 L 和过渡宽度 Δ。写进全局纹理 `light_channels`（32×1 RGF，每帧 `ImageTexture.Update()`，256 B）；全局参数没有数组类型，所以用数据纹理。
- **有效太阳高度**（SYS-WEATHER §7、SYS-TIME §6）：h_eff = h − 6° × 阴度 − 4° × 降水量，阴雨天灯提前亮。下面所有按太阳高度判断的曲线（窗灯的 dark、路灯两组的阈值）一律用 h_eff。C# 侧（WP-39）用天气向量算；着色器里（路灯光斑、光晕和光锥按灯表阈值求亮度时）由 `sun_dir.y` 和 `weather_a` 按同一公式算出，不另加全局参数；G2 用同一组输入比对两边。
- **时刻表曲线**（以 SYS-TIME §6 为准，这里只列计算形式）：WP-39 在 CPU 上按钟点、h_eff 和季节每帧算出各通道的 L。

  | 通道 | 曲线 |
  | --- | --- |
  | 窗灯总体 | L = clamp(dark × occ, 0, 0.85)，dark = smoothstep(9°, 2°, h_eff)，occ 是该类建筑按钟点的使用率（17:22–17:40 间全校约 20% → 70%，SYS-TIME §6）；白天再取天气下限：阴天 0.25、雨天 0.3（SYS-WEATHER §2） |
  | 宿舍 | 19–23 时 0.85；23:30 成片熄到 0.35；2–6 时 0.05 |
  | 教学楼 | 19:30–21:55 整齐亮起；22 时以后 0.05 |
  | 图书馆 | 天蒙蒙亮时全校第一栋亮灯；8:00–22:30 为 0.9；22:30 一层层熄灭，只留品·悦吧（24 h）和中庭的台灯。冬季考试周通宵亮灯（SYS-TIME §4） |
  | 科研楼 | 通宵 0.3 |
  | 食堂 | 06:30 先亮一排暖灯 |
  | 路灯 | 分两组，阈值在组内按哈希分布：初灯组在 h_eff 从 8° 降到 4° 之间逐盏亮起（秋季晴天约 17:16–17:38，17:30 约亮六成），其余路灯和景观灯在 3° 到 −1° 之间；1:00–5:30 景观灯熄一半（SYS-TIME §6） |
  | 球场灯 | 19:00–22:00 |
  | 店招 | 营业时段 |

- **窗灯的亮法**：每扇窗按 smoothstep(θ_i, θ_i + Δ, L) 亮起。Δ 随通道的变化速率自适应：Δ = clamp(|dL/dt| × 0.3 s, Δmin, Δmax)，初值取 Δmin = 0.005、Δmax = 0.15（§16），CPU 每帧按通道算好。1× 流逝时单扇窗在 0.3 s 内亮起；快进时 Δ 变宽，窗灯成片渐变，不闪。
- **可复现**：亮灭完全由哈希和通道值决定：同一钟点、同一季节和天气、同一哈希，结果相同，与怎样到达这个钟点无关；同一种子加同一串输入得到同样的画面（SYS-WEATHER §6.4）。
- **窗覆盖纹理**：`window_override`，256×256 RGBA8（256 KB），可容纳 6.5 万扇窗（全校立面约 5.8 万个开间，WLD-INTERIORS §12），只存覆盖值（强制亮灭、冷暖、窗帘）。只能整张 `Update()`，所以只在变化时更新，不超过 10 Hz。
- **外围窗**（WLD-OUTSKIRTS §4.4）：窗号基址取一个外围专用段，不进 `window_override`，着色器按顶点标志跳过覆盖查表（动态分支，不是特化开关）；暖白与冷白 2:1，不用彩色。
- **室内灯**（WLD-INTERIORS §8.2）：开放室内的房间灯不用点光，按房间哈希读本楼的通道，灯照权重烘进顶点（COLOR 的备用字节），通道号和哈希阈值放 UV2，亮时把影色换成灯照色乘权重，零绘制调用；不要求与外面的窗灯逐间对应（窗外看室内时精度不重要）。可选（P2）：进房间时只把玩家所在房间的那几扇窗写进 `window_override`，与房间灯读同一个哈希，出楼后保留到镜头离开 150 m 再清掉，写入经 WP-39 的接口（WLD-INTERIORS §6.3）。
- **路灯光斑**：离线生成一张 8 m 网格（范围 = `map_rect`，清水河 320×320，每格存最多 4 个灯号，`lamp_grid`），再加一张灯表（`lamp_table`），都是全局纹理，由 C# `[Tool]` 脚本从 Lamp 标记烘出。地面和 4 m 以下的立面，在着色器里读 1 次网格、最多 4 次灯表，画出平涂光斑。
  - **开放入口的门口灯**（WLD-INTERIORS §4）：每个开放入口在灯表里登记一盏门口灯，挂这栋楼的室内通道，地面光斑由同一套灯表着色画到台阶和地垫上；黄昏以后的光晕是 `fx_glow` 里多一个实例。不加绘制调用。
  - **外围的路灯**（WLD-OUTSKIRTS §4.4）：O0 的灯和校内一样进 `lamp_grid`、`lamp_table`；O1、O2 的路灯只画灯头光晕（光晕 MultiMesh 的实例增加不超过 1500）和路面灯带（沿灯位烘进路面顶点的一条亮度，读路灯通道），不进 `lamp_grid`。
  - 光斑里把影色换成调色板行 1 的灯照色（§1.5），画面仍然只有两阶。冬季光斑 ×1.5（SYS-TIME §3.3）；雪花进入光斑时提亮（§9）。
  - 灯表里每盏灯另存两项：所属通道、开灯阈值（太阳高度，按哈希生成，着色器里与 h_eff 比较）。着色器按这两项算亮度，不需要逐帧更新灯表。
  - 夜雾里路灯光晕半径 ×2、光锥可见（SYS-WEATHER §2）：只改 `fx_glow` 读的天气量，不加实例。
  - 开销固定，不受 Forward+ 分簇上限的约束。
- **共用灯条表 `refl_lamps`**（SYS-WEATHER §7；WP-24 写）：水面、积水、湿路面和倒影世界里的竖向灯条都读这一张表。
  - 内容：16×2 RGBAF，16 盏关键灯，每盏一列两个纹素：(x, z, y, 灯光通道号)、(颜色行号, 开灯阈值, 条宽, 灯光表索引；窗灯、钟面这类非路灯源记 −1)；排布以 WLD-WATER §7.3 为准。
  - 选灯：WP-24 每 0.25 s（4 Hz）从路灯、亮窗、钟面这些灯源里按亮度和到相机的距离挑 16 盏，相机移动超过 5 m 时立即重选（WLD-WATER §7.3）；换进换出的那盏在 0.25 s 内淡入淡出，不跳。
  - 亮度：不由 CPU 写，着色器按该灯所属通道的值和开灯阈值算，再乘偶发事件的提亮（读 `light_channels`、`incident`），所以偶发事件里依次变亮的路灯，在积水和湿路面上同步变亮（SYS-WEATHER §5）。
  - 湿路面的灯条只在湿润 > 0 时画，强度乘 `weather_b` 的湿润和积水；画法复用水面的反光带函数（WLD-WATER）。
  - 开销：每个湿面片元最多读 16 盏灯的 32 个纹素，P0 实测；超了就按屏幕距离只取最近的 8 盏（§16）。
- **重点灯用 OmniLight3D**（全校不超过 32 盏：图书馆门厅、开放室内的重点灯；当前所在的开放楼最多借 4–6 盏，给门厅和主痕迹旁的台灯，WLD-INTERIORS §8.2）：
  - Forward+ 的分簇默认同屏最多 512 个元素（点光、聚光、贴花、反射探针合计），超出时灯会忽隐忽现；每个屏幕块用位掩码记录元素，没有每簇上限。灯的增减不改特化常量，不重编译（区域光除外，本项目不用）。
  - 但全校几百盏路灯不能都用真实点光：时刻表要求逐盏改能量，C# 每帧要改几百个节点；而且自定义 light() 对每盏灯、每个像素都执行一次。所以只给重点灯。
  - 设置：`ShadowEnabled = false`，`DistanceFadeEnabled = true`，`LightSpecular = 0`，`LightCullMask` 排除地面层（第 2 层），避免和灯光表的光斑叠两遍。能量由 WP-03 按该灯所属通道的值写（C# 侧已有这个值，不读回全局参数）。
  - light() 的点光分支按调色板行 1 的灯照色加差值，不用 `LIGHT_COLOR`（ART-STYLE §2）。
- **光晕和光锥**：全校每类一个 MultiMesh（`fx_glow`），`INSTANCE_CUSTOM` 存灯号、通道、阈值；加法混合、不受光；亮度由着色器读通道算出，每盏灯每帧零 CPU 开销。

### 10.4 倒影（WP-24）

- **视口**：一个 SubViewport，尺寸为窗口的每轴 1/4（风格底线，不随预设降）。按画面设置的"西湖倒影刷新"（极致、精致每 2 帧，均衡每 3 帧，流畅每 4 帧；最差每 4 帧，SYS-GRAPHICS §1、SYS-GRAPHICS §2）把 `RenderTargetUpdateMode` 重设为 `Once`，实现每 N 帧更新（N = 2 / 3 / 4，随画面设置）；改间隔即时生效。`Msaa3D` 关，`ScreenSpaceAA` 关，`PositionalShadowAtlasSize` 设 0，`UseOcclusionCulling` 关，`OwnWorld3D` 开；不挂 Compositor。
- **为什么要独立世界**：没有哪个视口属性能单独关掉方向光阴影，而每个视口渲染时都会把方向光阴影重渲一遍。独立世界里没有投影的光，也就没有这份开销。
- **独立世界的内容**（REFLECT 内容，总绘制不超过 40 次）：
  - 天空：WorldEnvironment 共用同一个 Sky 资源，环境光和反射源都关掉，Tonemap 与主视口一致；
  - 远景环（L3/L4 的体量）、不超过 10 个地标（用 L2/L3）、岸边树冠的替身、荷；
  - 都是平涂材质 `reflect_flat`：读全局调色板（全局参数对所有 World 都生效），夜间画亮窗（与窗灯同一个 hash 和通道，§10.3）和关键灯的竖条倒影（读共用灯条表 `refl_lamps`，§10.3），倒影里的亮灭与岸上一致。
  - 代理实例由 WP-24 在倒影世界的 scenario 里用 RenderingServer 创建，复用格子的 Mesh RID，用材质覆盖换成 `reflect_flat`（预热过，§2）；随格子一起挂载和淘汰。
- **镜像相机**：位置 y′ = 2h − y，朝向用镜像后的前向和上向量 `LookingAt` 出来。
- **裁剪**：4.7 的 Camera3D 只有透视、正交、frustum 三种投影，没有自定义或斜裁剪。所以只放水面以上的代理，并在 `reflect_flat` 里丢弃 y < h 的片元。
- **水面采样**：水着色器用全局参数 `reflect_vp`（倒影**渲染那一帧**的反射相机 VP 矩阵）把世界坐标投到 `reflect_tex` 上采样；每 N 帧更新带来的不超过 N 帧的延迟因此对得上。按波纹线遮罩把采样偏移 1–3 px（取整），整体压暗一阶。
- **每帧只给一个湖**：取屏幕上面积最大的那个湖（带回差），换湖时改 `reflect_lake`，并把该湖的水面高度 h 写给 `reflect_flat` 的材质参数。
- **随时段与季节**：代理读调色板行 0，岸边替身读季相行，所以倒影自动跟随时段和季节，不需要额外更新。天空是解析计算的，本来就跟得上。主光反光带、月光银带的画法归 WLD-WATER，这里只保证它们用的主光方向与 `key_dir` 一致。
- **河渠**：不做平面反射。用反射向量查询共用的解析天空函数，再叠上 `refl_lamps` 里的竖向光条。
- **天气**（SYS-WEATHER §7）：雨天湖面的波纹线换成雨点环，在 `water` 里按 0.8 m 网格的哈希解析画出（硬边同心环，一拍二），不占涟漪槽位；阴雨天关主光反光带（WLD-WATER）。积水的倒影不用这个视口：反射视线沿 `precip_occ` 步进 8–12 次，碰到顶面画剪影色，否则画解析天空，夜间叠 `refl_lamps` 的灯条；不用 SSR，也不加平面反射（每个视口都会把方向光阴影重渲一遍）。
- **水面本身**（WLD-WATER）：默认不透明，不读深度和屏幕纹理（读了就会被强制归到透明通道、而且不投影），能接收阴影，`cull_disabled` 让水下能看到底面；泡沫线和"伪深度"读烘焙的岸线 SDF（开发期烘焙，随启动集发，D-165；约 0.25 m/px）；需要真实交界泡沫时才用 `water_clear`。
- **不用 SSR**：只作用于不透明物体，到画面边缘会断。**不用反射探针**：设成每帧更新时要渲 6 个面。
- **开销**：每次更新约 0.3–0.6 ms（估计值），平均到每帧按 ENG-BUDGET §2 的 5% 记账。地下和水下时设 `UpdateMode.Disabled`。

## 11 设备档、画面预设与自适应（WP-32，WP-01 配合）

只做 PC。**设备档**是硬件分类，只决定推荐哪个预设、帧率目标和自动调节的预算；玩家选的是**画面预设**（流畅、均衡、精致、极致，加自定义，SYS-GRAPHICS §1）。画面设置的实现约束写在 §11.7。

### 11.1 设备档 → 推荐预设

| 设备档 | 典型设备 | 推荐预设 | 帧率目标 | 自动调节的预算 |
| --- | --- | --- | --- | --- |
| 最低配置 | 酷睿 Ultra 第一代（Arc 核显）或 Radeon 780M 同级及以上，16 GB 双通道内存，SSD | 均衡 | 60 | 16.7 ms |
| 独显中 | GTX 1660、RTX 3050、RX 6600 | 精致 | 60 | 16.7 ms |
| 独显高（显存预算 ≥ 8 GB） | RTX 4070 及以上（开发机 RTX 4080） | 极致 | 跟随刷新率 | 刷新周期（或 §11.4 选定的整数分之一） |

- 更旧更弱的核显（UHD 6xx、Vega 8、Iris Xe、680M 等）不做适配（D-169）。流畅不对应任何设备档，是重场景和自动调节时的兜底预设，帧率目标 60（ENG-BUDGET §2）。
- 预设的具体取值（渲染比例、阴影、细节距离、倒影、植被、降水密度等）见 SYS-GRAPHICS §1；各预设的资源上限和各设备档的指标见 ENG-BUDGET §2。
- **自动调节的预算**取上表一列与玩家帧率上限对应的帧周期中较大的那个：玩家把帧率上限设成 30 时，预算就是 33.3 ms，不会为了追 60 fps 去降画质。
- **风格底线**（描线、明暗分阶、按材质的影色、柔光（高光晕开）等）在任何预设、手动值和自动调节级别下都保留；移轴、镜头光斑、光柱、暗角是风格默认，四档都开，只有玩家能关（SYS-GRAPHICS §2、ENG-BUDGET §7 第 17 条）。
- 玩家可以选高于推荐的预设；这时不保证 ENG-BUDGET §2 的帧率，自动调节只往下调（§11.3）。

### 11.2 首次分档（扉页后面完成，不超过 2.5 s）

有开始页时在扉页插画后面做，排在二维预热之后、恢复世界与 T0 之前（SYS-TITLE §1.1 第 5 步）；`--title=0` 时在雾幕里。只在首次启动，或显卡、驱动、输出分辨率变了时做。

1. **读显卡信息**：`RenderingServer.GetVideoAdapterName()`、`GetVideoAdapterVendor()`、`OS.GetVideoAdapterDriverInfo()`。
2. **查 DXGI 和 UMA**（做法与坑见 §11.7）：拿到 Godot 正在用的适配器，读显存预算和 UMA 标志。**不用** `GetVideoAdapterType()` 判核显：D3D12 下它以"专用显存 > 0"判独显，而核显也会报 32–256 MB 的专用显存。
3. **判档**：UMA 或专用显存小于 1 GB 判为核显，一律归最低配置（更旧更弱的核显不做适配，D-169）；独显按显存预算和名称判高或中。判不出来的进入测速。
4. **测速（限时采样）**：把镜头放到 V1（即 P00），管线预热完、V1 的各环挂上之后才开始计时；首次运行时等这些格子本机烘焙的时间算在首次运行的目标里（§8），不占这 2.5 s。按时间采样，不按帧数，垂直同步照常开（GPU 时间读 `ViewportGetMeasuredRenderTimeGpu`，不含等垂直同步的时间）：
   - 0–0.4 s 预热，把 GPU 拉到负载频率，样本不用；
   - 0.4–1.4 s 用渲染比例 1.0，1.4–2.4 s 用 0.67；换比例后的前 3 帧不用（GPU 计时滞后 2–3 帧，缓冲重建的那一帧也在其中）；
   - 到 2.4 s 停止采样、出结果，整步不超过 2.5 s。60 Hz 下每档约 55 个样本，30 fps 的机器每档约 27 个。

   两档的有效样本都不少于 20 个时，拟合"GPU 时间 = a × 像素数 + b"，按各档推荐预设的内部像素预测 p90，与该档预算的 80%（核显 75%）比较选档。**到时样本不够**（某档不足 20 个，即单帧约 50 ms 以上），或拟合结果不可信（a ≤ 0）时，采用保守结果：第 3 步判为核显的取最低配置，判为独显的取独显中；照常写缓存，记一条日志，玩家可在设置页"重新检测"（第 6 条）。测完镜头移到 T0；V1 的各环留着，给新游戏过场的终点复用（SYS-TITLE §1.1 第 5 步）。GPU 降频会让测得的时间偏大；笔记本刚开机时又可能偏快。
5. **写缓存**：结果写进 `user://graphics.cfg` 的 `[device]` 段，键是"显卡名 + 驱动版本 + 输出分辨率"；首次运行时推荐预设就是当前预设（SYS-GRAPHICS §5）。读写按 SYS-SAVE §8：只存基本类型，写盘走 tmp + `Flush(true)` + `File.Replace`。
6. **之后**：玩家手动选过预设，下次启动不覆盖。只有显卡名、驱动版本或输出分辨率变了才重新检测；推荐变了用一条临时提示告知（UI-NAV §4.9），不自动改玩家的选择。设置页有"重新检测"。
7. **双显卡笔记本**：名字是核显、而 DXGI 能枚举到独显时，在设置页提示，并提供"显卡选择"（重启类，SYS-GRAPHICS §3）。

### 11.3 运行时自适应（自动调节画质）

**规则**（SYS-GRAPHICS §5，默认开）：

- 实际生效值 = clamp(min(玩家所选, 自适应上限), 最差值)。只往下调，回升不超过玩家所选。最差值取 `KnobSpec.WorstAllowed`：阴影和倒影取风格底线的最差值（ENG-BUDGET §7 第 17 条），其余性能项取流畅预设的值。
- 玩家手动改过的项被锁定，阶梯跳过它，直接进下一级；当前值已经等于或低于该级目标的项也跳过。
- 只动即时类的性能项。**不动**渲染比例、阴影图尺寸、细小物件投影、显示设置，也不动风格底线和风格默认（移轴、光斑、光柱、暗角）。
- 每一级降档都在 0.5–1 s 内淡化完成，不得硬切（G13）；升级同样淡化。
- 界面用一行小字写明当前自动降了哪些项，例如"已自动降低：植被密度、阴影距离"，读 `IQualityService.Effective` 与 `Requested` 的差。
- 关闭时只保留帧节奏控制（§11.4）。

**降级阶梯**（按顺序，每一级约省 10–20%，估计值）：

1. 植被、草和地面落叶层的密度降到 75%，降水与粒子密度降一档（`VisibleInstanceCount` 前缀抽稀，被去掉的实例 0.5 s 抖动淡出）；
2. 细节距离系数 ×0.85（选级的屏幕误差放宽，粗一级更早出现；已挂载格子的 visibility range 分帧重写，每帧 ≤ 0.3 ms，切换走 0.25 s 抖动淡化）；
3. 模型 LOD 精度从 1 px 调到 2 px（`Viewport.MeshLodThreshold` 在 1 s 内逐步调大，切换分散到多帧）；
4. 柔光质量从高降到标准（要去掉的 glow 级在 0.5 s 内渐变到 0，§10.2）；
5. 倒影的刷新间隔加一档（每 2 → 3 → 4 帧，不低于每 4 帧）；
6. 阴影：阴影距离收短一档（1600 → 1000 → 600 m，在 1 s 内连续收短，远端影子随 `DirectionalShadowFadeStart` 的淡出区一起退）；级联 4 → 2 是离散变化，只在阴影淡的时候做（`shadow_strength` ≤ 0.3，如蓝调、夜里、阴雨），或者等下一次雾幕，不在晴天白天硬切（P0 录像判 ΔE，§16）。最差 2048²、2 级。

阶梯用完仍不达标时，停在最后一级：不改渲染比例和阴影图尺寸，不主动拉雾幕，也不自动换预设。

**控制律**：

- **降级**：1 s 内有 3 帧以上的间隔超过目标的 1.5 倍，或 GPU p90 超过预算（§11.1）的 0.85 倍，就降 1 级；1 s 内超标帧达到 10 个，降 2 级。两次降级之间至少隔 1 s。
- **升级**：GPU p90 低于预算的 0.6 倍并持续 10 s。如果升级后 10 s 内又降回原级，下次升级的等待时间翻倍，从 20 s 依次到 40 s，最长 300 s。
- **剔除样本**：发生挂载、编译、可见性切换、尺寸变化、介质切换的帧，不计入统计，并清空统计窗口。以下几类帧也剔除：
  - 时间快进（点刻度、预设、拨表盘的前向快进）、换季转场的帧（SYS-TIME §1、SYS-TIME §5）；
  - 点位跳转帧（UI-NAV §7.5）和观察模式切换的过渡帧（SYS-WALK §5）；
  - 画面设置生效的那一帧及其后 2 s（SYS-GRAPHICS §4）；
  - 窗口失焦期间（驱动会限帧，实验里掉到 144 或 30 fps）；
  - 另有：
    - Title、Entering、Leaving 三个状态的全部帧（开始页、进场过场、返回标题）：不计入统计，自适应冻结，不升也不降（SYS-TITLE §7.2）；
    - 传送帧和传送雾幕期间的帧（SYS-ATLAS §7.4）；切图的雾幕期间（SYS-MAPS §4.2）；
    - 读档、继续、新游戏的遮挡期间，以及之后 2 s（SYS-SAVE §5.1）；
    - 存档缩略图抓帧的那一帧（主线程 3.8–7 ms，SYS-SAVE §13）；
    - 大页打开、3D 被压暗的期间（UI-VISUAL §12）。

  这些帧只是不触发降级。它们本身掉不掉帧，由 §12.4 的体验脚本和 F3 跳转路线检查，过场由"过场体验"脚本检查（SYS-TITLE §11.3）。换天气的过渡是慢变化，不剔除。
- **不可调的项**：特化常量和管线相关的开关（ENG-BUDGET §7 第 9 条）、渲染比例、阴影图尺寸、细小物件投影、显示设置、风格底线与风格默认，只由玩家在设置里改（风格底线连玩家也不能关）。

**发热漂移**（核显笔记本）：Windows 上没有方便的温度接口，所以按帧代价判断：在同一预设内，对帧代价做时间常数 60 s 的指数平均，与第 1–2 分钟的基线比较。连续两个 30 s 窗口超过基线的 1.15 倍，就把自适应上限再压低 1 级，并在 5 分钟内不升级；超过 1.3 倍，再降 1 级。

**不做静止降频**：镜头静止时，帧率不降低，保持与运动时相同。`low_processor_mode` 关（ENG-BUDGET §10）。

### 11.4 帧节奏与输入

- **垂直同步默认开**，交换链 3 张、帧队列 2；设置里可关（即时）。刷新率用 `DisplayServer.ScreenGetRefreshRate()` 读。
- **帧率上限**（画面设置，SYS-GRAPHICS §3）：30 / 60 / 半刷新率 / 刷新率 / 不限，用 `Engine.MaxFps`，即时生效。默认取预设的帧率目标：流畅、均衡、精致 60，极致跟随刷新率。
- **可变刷新率显示器**：开时 `Engine.MaxFps` = 刷新率 − 3，留在 VRR 范围里。设置里有"可变刷新率"开关，默认在刷新率 ≥ 120 Hz 时打开（§16）。
- **固定刷新率、又跟不上刷新率时**：帧率目标为"跟随刷新率"（极致）时，如果首次分档测得 GPU p90 超过刷新周期的 0.85，就把 `Engine.MaxFps` 设为刷新率的整数分之一（144 Hz 屏取 72 fps，160 Hz 屏取 80 fps）。**不要硬限 60**，否则帧间隔会抖。
- **动画步长**：`_Process` 的 delta 开着 `delta_smoothing`（ENG-BUDGET §10）；`anim_time` 和 `fade_clock` 都用它累加，单帧上限 100 ms。
- **行走物理**：固定 120 Hz（`physics_ticks_per_second`），不开全局物理插值，相机按 `Engine.GetPhysicsInterpolationFraction()` 手动插值，`physics_jitter_fix` 为 0（SYS-WALK §12）。`max_physics_steps_per_frame` 取 12，对应 100 ms 的步长上限。物理的每帧额度由 P0 原型重定。
- **一拍二**：默认 12 Hz 步进，60 fps 时每个姿态保持 5 帧，144 fps 时 12 帧。**`step_rate` 随帧率上限变化**（SYS-GRAPHICS §5）：上限 30（含 60 Hz 屏的半刷新率）时取 15，每个姿态正好 2 帧；其余取 12。WP-32 在帧率上限变化时写全局参数 `step_rate`，着色器统一用 floor(`anim_time` × `step_rate`) / `step_rate`（SYS-MOTION §2.2）。截图和门禁一律 12。
- **输入**（UI-NAV §5、SYS-VIEW §2）：`Input.UseAccumulatedInput = false`；在 `_UnhandledInput` 里读 `InputEventMouseMotion.ScreenRelative`（Windows 捕获模式下就是原始输入）；相机在 −1100 那一步积分，本帧的输入影响本帧的画面。

### 11.5 Windows 平台的已知问题

- **双显卡笔记本**：Windows 的图形首选项可能让程序跑在核显上；启动时校验显卡名，设置里可以选显卡（重启生效，§11.2、SYS-GRAPHICS §3）。
- **第一次运行**：驱动的着色器缓存是空的，编译最多；D3D12 下引擎不存管线缓存（§2），靠 shader baker 加雾幕或扉页后面的预热兜住（§2、§8）。驱动升级后同样会冷一次。
- **叠加层和录屏**：Steam、Discord、显卡驱动的叠加层和录屏会改变帧时序；基准测试时必须关掉（§12.4）。
- **窗口失焦**：驱动会限帧；画面测试断言窗口有焦点（SYS-GRAPHICS §5），自动调节在失焦期间不计统计（§11.3）。
- **全屏模式**：默认无边框全屏；独占全屏、窗口模式和窗口尺寸是雾幕类画面设置，改后 15 s 内不确认就自动还原（SYS-GRAPHICS §4）。全屏不改变显示器分辨率，全屏下的"分辨率"就是渲染比例。

### 11.6 集成显卡

- **带宽是第一瓶颈**：双通道 DDR4-3200 为 51.2 GB/s，还与 CPU 共享；对比 RTX 4060 为 272 GB/s。所以均衡、流畅预设优先降低渲染比例（FSR1），阴影用 2048、2 级，移轴降到 1/4 分辨率，植被减密度，抗锯齿用 FXAA（SYS-GRAPHICS §1）。
- **判别**：用 DXGI 的 UMA 标志，不用 `GetVideoAdapterType()`（§11.2）。AMD APU 上的 UMA 判定要在真机上验证（G11）。
- **显存就是系统内存**：核显的 1.2 GB 显存预算和 3 GB 进程内存一起占用系统内存（§7）；DXGI 给 UMA 报的显存预算就是本进程能用的那一份，设置页的显存条以它为准（§11.7）。
- **CPU 也弱**：笔记本 CPU 核数少，遮挡剔除的缓冲随之变小（§4），剔除效果变差；绘制调用的预算相应压低（ENG-BUDGET §2）。
- **计时**：D3D12 设备上 GPU 计时都可用，自适应直接用 GPU 时间。

### 11.7 画面设置（WP-32a、32b，WP-01、WP-35 配合）

设计、单项清单和预设数值在 SYS-GRAPHICS，这里只写性能侧的实现约束。

**三类生效**（SYS-GRAPHICS §3）：

| 类别 | 单项 | 引擎侧代价 | 怎么生效 |
| --- | --- | --- | --- |
| 即时 | 阴影质量（尺寸、级联）、阴影距离、柔光质量、移轴、镜头光斑、光柱、暗角、细节距离、模型 LOD 精度、近景几何叶、植被与草密度、草地范围、西湖倒影刷新、降水与粒子密度、垂直同步、帧率上限、可变刷新率、自动调节 | 只改数值，不编译、不重建缓冲；细节距离要分帧重写已挂载实例的 visibility range（每帧 ≤ 0.3 ms） | 拖动即预览，0.2 s 去抖；在下一帧 `FramePriority.Quality`（−1200）统一分发，同一帧里所有系统看到同一版本；有视觉变化的项按 §11.3、§10.2 的写法淡化 |
| 雾幕 | 显示模式、窗口尺寸、渲染比例、FSR 锐度、抗锯齿、各向异性、细小物件投影 | 重建 3D 渲染缓冲（`RenderSceneBuffersRD.configure()` 先清理再分配），4K 下 setter 4–10 ms、下一帧再 7–15 ms；不编译 | 标为"待应用"，点"应用"或收起设置卡时一次提交，在 0.3 s 雾幕里按 SYS-GRAPHICS §4 的顺序执行；描线的 CompositorEffect 每帧用 `RenderSceneBuffersRD.HasTexture` 查自建的中间纹理，缓冲重建后缺了就重建；G16 的计数基线在雾幕淡出时重置 |
| 重启 | 图形接口（D3D12 / Vulkan）、显卡选择 | — | 写 `user://engine_override.cfg`（项目设置 `application/config/project_settings_override` 指向它，发布模板默认支持；按 SYS-SAVE §8 写：只存基本类型，tmp + `Flush(true)` + `File.Replace`），`OS.SetRestartOnExit(true, args)` 后退出 |

- 细小物件投影本身只改 `ShadowCasterMask`，归雾幕类是为了遮住小件影子在一帧内消失。
- 跳转、模式切换、换季转场、回位进行中时，`Apply` 返回 `Deferred`，等它们结束再执行（SYS-GRAPHICS §4）。传送、切图、读档、过场进行中同样返回 `Deferred`；切图途中的雾幕类改动并进切图的雾幕一起生效（SYS-MAPS §9）。
- 超采样（极致可选的 `Scaling3DScale` 1.25，§10.2）若加进 27，属雾幕类：只重建缓冲，不编译。
- 介质覆盖（镜头在开放室内时阴影第一级缩到 8–12 m、阴影方向抬到 14°）属即时类，不编译，在 SYS-GRAPHICS §3 登记为"介质覆盖"，出楼恢复（WLD-INTERIORS §6.3、本文 §10.1）。

**不做成选项的**（四个预设统一取值，保证切换不编译）：软影滤波档位（改一次清空全部管线，§2）；阴影深度（固定 16 位，`positional_shadow/atlas_16_bits = false` 让两种格式的管线启动时都编好，ENG-BUDGET §10）；MSAA、TAA、FSR2；SSAO、SSR、体积雾；纹理质量（Godot 没有运行时跳 mip，`TextureMipmapBias` 不省显存）；HDR 输出（与 LUT + CDL 调色冲突）。

**启动预热覆盖所有可达组合**（§2 第 3 条）：WP-01 的预热表取"四个预设和所有单项取值能到达的组合"的并集，所以玩家怎么切都不再编译管线（G16）。雾幕类只重建缓冲，不需要重新预热。P0 实测预热总时长，计入 §8 的启动目标。门禁："预设轮换"脚本跑完，每次雾幕淡出后编译计数不增，显存回到基线 ±5%（§12.4）。

**DXGI 与 UMA 检测**（WP-32b；零 NuGet 依赖）：

1. `RenderingServer.GetRenderingDevice().GetDriverResource(RenderingDevice.DriverResource.PhysicalDevice, default, 0)` 拿到 Godot 正在用的 `IDXGIAdapter`；`QueryInterface` 到 `IDXGIAdapter3`，调 `GetDesc1`（名称、专用显存）和 `QueryVideoMemoryInfo`（LOCAL 段的显存预算与当前用量）。
2. `DriverResource.LogicalDevice` 拿到 `ID3D12Device`，调 `CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE)` 读 UMA 标志（本机 4080 为 0，软件渲染器为 1）。
3. 约 100 行手写 COM 调用（按虚表取函数指针），放在 `game/kernel/quality`；备选 Vortice.DXGI 3.8.3（MIT），不用 SharpDX（2018 年后没有更新）。
4. 坑：WMI 的 `AdapterRAM` 封顶约 4 GB，不能用；本机 DXGI 把同一张 4080 列了 3 次、LUID 各不相同（虚拟显示器造成），只能按名称匹配；`GetDeviceTotalMemory()` 在 D3D12 下返回 0；`Performance.RenderVideoMemUsed` 比 DXGI 当前用量少约 70 MB，DXGI 的数值按堆的粒度跳变（加 8192 阴影图时前者 +96 MB、后者 +127.5 MB）。
5. 用 Vulkan 时 `GetDriverResource` 返回的是 `VkPhysicalDevice`，退回到按名称在 DXGI 枚举里匹配。
6. 运行时每秒查一次当前用量，用于显存压力判断（§7）。

**显存估算**（P2 交付；与 G8 共用同一个函数，§12.5）：

| 部分 | 公式 |
| --- | --- |
| 3D 渲染缓冲 | ≈ 44 B × 内部像素（纹理 33 B + 簇缓冲 11 B）；描线另加 normal_roughness 4 B × 内部像素和自建的中间纹理 |
| SMAA / FSR1 | ≈ 17 / 14 B × 输出像素 |
| 阴影图 | 边长² × 2 B（16 位）：8192 为 128 MB，4096 为 32 MB，2048 为 8 MB |
| 倒影 | 44 B × 1/16 输出像素 |
| 固定开销 | ≈ 70 MB（首帧前 DXGI 已有 68 MB） |
| 纹理与几何 | G8 的烘焙统计 × 常驻集 |

按公式算出的"渲染目标与阴影图"（不含固定开销和描线的中间纹理）：

| 预设 | 1080p 输出 | 1440p 输出 | 4K 输出 |
| --- | --- | --- | --- |
| 流畅 | 约 80 MB | — | — |
| 均衡 | 约 90 MB | 约 115 MB | — |
| 精致 | 约 175 MB | 约 240 MB | 约 410 MB |
| 极致 | 约 275 MB | 约 385 MB | 约 650 MB |

- "—"表示该输出下内部像素已低于 FSR1 0.5 的下限（SYS-GRAPHICS §1）。4K 输出时 SMAA 和 FSR1 按输出像素计，占了一半以上，所以 ENG-BUDGET §2 给 4K 单列了上限。
- 设置页显示：当前 DXGI 用量 + 待改设置按公式算出的差值，对比 DXGI 给本进程的显存预算；超过 80% 标黄，超过 100% 标红，并点名最占显存的几项（SYS-GRAPHICS §5）。误差：渲染目标约 ±5%，总量 ±10–15%。

**契约与分发**（SYS-GRAPHICS §8、ENG-ARCH §3）：界面和门禁只经 `IQualityService`；各包在自己的 `[KernelNode]` 上用 `[QualityResponsive]` 声明负责（Owns）和只读（Reads）的单项，G5 核对每个单项恰有一个负责方；着色器要读的值合成全局参数 `quality`（§1.5）。任何包都不读 `graphics.cfg`。命令行与持久化见 SYS-GRAPHICS §6：门禁和基准只由命令行决定，不读 `user://`。

## 12 测量、剖析与门禁（WP-27，WP-01 提供接口）

### 12.1 测量机器

吞吐、回归和额度核算都在开发机（本机，RTX 4080 台式机）上进行，不设中档参考机。本机有多个适配器，一律用 `--gpu-index` 选 RTX 4080，并断言 `RenderingServer.GetVideoAdapterName()` 与机器登记表一致，不一致就判"环境失败"、重新排队，不算性能失败。预设由命令行指定、自动调节关：额度核算用精致 1080p（ENG-BUDGET §8），开发机的达标线用极致 1440p（ENG-BUDGET §2）。独显中和最低配置能否达标，在手头实际有的设备上测（G11）。

### 12.2 运行时测量

- **GPU 与 CPU 计时**：`RenderingServer.ViewportSetMeasureRenderTime(viewportRid, true)` 打开后，逐帧读 `ViewportGetMeasuredRenderTimeGpu` 和 `ViewportGetMeasuredRenderTimeCpu`（毫秒）。GPU 降频会让测得的时间偏大，所以测量时锁频（§12.4）。
- **分段 GPU 时间**：WP-05 在 Compositor 的回调里用 `RenderingDevice.CaptureTimestamp(名字)` 打点，2–3 帧后用 `GetCapturedTimestampsFrame`、`GetCapturedTimestampsCount`、`GetCapturedTimestampGpuTime` 读出。只到通道级。
- **调用与三角形**：`Viewport.GetRenderInfo` 分 Visible、Shadow、Canvas 三类；`Performance` 的 `RenderTotalDrawCallsInFrame`、`RenderTotalPrimitivesInFrame`、`RenderTotalObjectsInFrame`；口径见 §1.3。
- **显存与对象**：`RenderVideoMemUsed`、`RenderTextureMemUsed`、`RenderBufferMemUsed`；`ObjectCount`、`ObjectResourceCount`、`ObjectOrphanNodeCount`。
- **管线编译**：`PipelineCompilationsCanvas`、`...Mesh`、`...Surface`、`...Draw`、`...Specialization`（G16）。
- **自定义监视项**：`Performance.AddCustomMonitor` 加流式队列长度、在途请求数、在途生成任务数、碰撞冻结累计时长、祖先格兜底时长、挂载耗时、字节账、自动实例化命中率（内核按"同网格同材质的可见实例数 / 主通道绘制调用"估算）、GC 次数和暂停；另有音频的 `audio/main_us`、`audio/real_voices`、`audio/nodes3d`、`audio/virtual`、`audio/resident_kb`、`audio/mix_core_pct`（53a 登记，SYS-AUDIO §18.1）；"过场流式欠账"计数（SYS-TITLE §10）；室内门口软停的触发次数（G19f）；本帧创建的控件数（帧时超过 20 ms 时附带，UI-VISUAL §11.9）。
- **归因**：每个 RenderingServer 实例都标有所属工作包（只有内核能建实例，经 `IRenderHost`，ENG-ARCH §2），G8 按包预测。GPU 时间只精确到通道级；需要按包看时，用 `-- --without=WP-xx` 关掉一个包做增量测试，仅用于诊断；`--without=WP-53` 可以关掉音频比较主线程差值（音频不占 GPU）。CPU 时间由各系统在 `_Process` 里调用 `Campus.Kernel.Frame` 提供的计时作用域上报（系统自己不计时，ENG-ARCH §2），内核按包汇总。
- **分布统计**：帧间隔、GPU 毫秒、CPU 毫秒各算 p50、p95、p99；p99 用 5 次运行合并后的约 3000 帧计算。
  - 掉帧：间隔超过目标的 1.5 倍。
  - 卡顿：超过 50 ms；长卡顿：超过 100 ms。两者都按每分钟计数。
  - 抖动：相邻帧间隔之差的 p95，开发机不超过 2 ms。

### 12.3 剖析工具

| 工具 | 用来定位 |
| --- | --- |
| 编辑器的 Profiler 与 Visual Profiler | 脚本和引擎各阶段的 CPU、各通道的 GPU。代理一般不开编辑器，只在人工诊断时用 |
| `--gpu-profile` 启动参数 | 一帧里最耗时的 GPU 任务 |
| RenderDoc（Vulkan，加 `--generate-spirv-debug-info`） | 一帧内的命令序列、渲染目标、Quad Overdraw 叠加层（§3.3）、着色器源码级调试 |
| PIX（D3D12） | GPU 时间线、占用率、D3D12 状态、CPU 时间线 |
| dotnet-counters、dotnet-trace | GC 次数与暂停、分配率、JIT |
| Tracy | 要自己编译引擎（`profiler=tracy`），不用 |

### 12.4 自动化基准

- **测导出的 release 版**，不测编辑器。窗口模式（无界面模式是空渲染器，不渲染），窗口不能最小化（最小化时不绘制），在已登录的交互式会话里跑。
- **命令行**（`--` 之后是用户参数，用 `OS.GetCmdlineUserArgs()` 读，未知参数以非零退出码结束；默认值与 ENG-GATES §1 的截图默认值一致）：

  ```powershell
  UESTC.exe --rendering-driver d3d12 --gpu-index <n> --resolution 1920x1080 --disable-vsync --fixed-fps 60 `
    -- --bench=1 --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 `
       --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off `
       --audio=off --frames=600 --out=<绝对路径>
  ```

  默认串里的 `--map=qingshuihe --title=0 --save=off`：没有开始页和扉页，不读写存档（SYS-MAPS §11）；截图和基准一律 `--audio=off`（`--bench` 下本来就是默认，SYS-AUDIO §17），只有声音门禁显式给 `--audio=on`。门禁和基准只由命令行决定起始图，不因"最近存档的图"而改变。

  基准默认精致 1080p；开发机另跑一遍 `--resolution 2560x1440 … --quality=ultra`，对照 ENG-BUDGET §2 的 8.0 ms（SYS-GRAPHICS §6）。`--tier=` 只设设备档（`min-spec`、`dgpu-mid`、`dgpu-high`），决定推荐预设、帧率目标和自动调节的预算，单项以 `--quality` 为准。

  本机烘焙的参数（D-165）：`--bake-cache=<绝对路径>` 指定本机缓存目录，`--bake-bg=0|1` 开关后台预烘（默认 1），`--bake-strict=1` 时缓存不命中直接报错退出、不现场生成（默认 0）。`scripts\gate.ps1` 先选出本次要跑的检查，再决定要不要预烘（ENG-ARCH §6 第 3 步）：截图类门禁、基准和 G10 运行前，先把它们用到的图按哈希增量补进共享缓存，运行时一律追加 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`，计时不受本机烘焙干扰；只跑快检时不烘焙；门禁和基准的截图默认串不变，这几个参数由 gate.ps1 追加（ENG-GATES §1、§2）。

- **两种模式**：
  - 吞吐模式：`--disable-vsync --fixed-fps 60`。`--fixed-fps` 关掉实时同步、按固定步长推进，用来测吞吐。
  - 体验模式：去掉这两个参数，开垂直同步，60 Hz 显示器，窗口不被遮挡、有焦点。
- **固定条件**：预设和单项只由命令行决定（`--quality`、`--quality-set`），`--adaptive=0`，不读 `user://`（SYS-GRAPHICS §6）。`--flow=0` 冻结世界状态；`--weather=clear` 加固定的 `--weather-seed` 冻结天气；动画时间和 F1 都按帧号推进。
- **世界状态基准**：除默认状态外，加三个固定组合，对应 ENG-BUDGET §2 的另外三种最坏组合：

  | 名称 | 参数 | 看什么 |
  | --- | --- | --- |
  | V1 夜间 | `--pose=V1 --clock=21 --season=winter --weather=snow --weather-intensity=1` | 夜间光晕、亮窗、路灯光斑、降水池雪花、积雪、月影 |
  | E1 夏季 | `--pose=E1 --clock=12.83 --season=summer --weather=clear` | 满叶、正午阴影、积雨云 |
  | E1 夏夜雷雨 | `--pose=E1 --clock=21.5 --season=summer --weather=thunderstorm --weather-intensity=1` | 降水池满额、溅落、远雨幕、湿路面灯条与积水倒影、闪电帧、满叶、灯全亮；阴影通道空跑 |

  这三个也进 G10 的视角（§12.5）。天气基准用手动天气，开测前等天气向量稳定（没有正在进行的过渡）；闪电帧在日志里打标记，统计照常计入。
- **预热**：等雾幕散开、流式队列为空、`PipelineCompilationsSurface`/`Draw`/`Specialization` 连续 60 帧不增，且相邻两个 60 帧窗口的中位数相差不到 2%；最少 2 s，最多 15 s。然后采样 8 s。
- **体验脚本**（只在体验模式下跑，判据是 ENG-BUDGET §2 的掉帧率和卡顿；每条跑完都要求 DRAW 管线编译计数为 0，G16）：
  - "前推 24 小时"（SYS-TIME §8）：从默认状态起、自动天气、`--weather-seed=1`，用两种方式各前推 24 小时：① 流逝，1440× 跑满 24 h（真实 60 s），10×、60×、360× 各跑 60 s 真实时间；② 拨表盘，脚本顺时针拨满一圈（每圈 +24 h，延时摄影式快进）。再点一次刻度，从 17:30 前跳到 05:30（前进 12 h，约 2.8 s 的快进）。途中天气按前向过程推进；
  - "四季轮换"：在 V1、E1 各做一遍春 → 夏 → 秋 → 冬 → 春的换季转场，再从秋直接选夏一次（一律正向转场，SYS-TIME §5）；
  - "天气巡回"（SYS-WEATHER §9）：用 `--weather-queue` 依次走遍各季验证过的转移边，再在界面上手动切换天气格和强度各一轮，含"减少动态效果"开启时；
  - "预设轮换"（SYS-GRAPHICS §10）：四个预设和若干自定义组合依次经 `IQualityService.Apply` 切换，雾幕类走雾幕；不崩溃，显存回到基线 ±5%，每次雾幕淡出后编译计数不增；
  - F3 跳转路线（ENG-GATES §1，判据见 UI-NAV §10）；F4 传送压力路线（SYS-ATLAS §14.3）；
  - W1–W5 行走路线（ENG-GATES §1，即 SYS-WALK §11 的 R1–R5）；开放室内的进楼路线（每处开放入口从 30 m 外按 1× 步行和 2× 疾跑各进一次门，G19f，WLD-INTERIORS §19）；
  - **"过场体验"**（SYS-TITLE §11.3）：用 `--title=1`，新游戏完整版和继续各播 5 次（继续按旁观、行走两种存档轮换），再各跳过一次。判据：没有超过 50 ms 的帧；掉帧率符合 ENG-BUDGET §2 当前预设的指标；"过场流式欠账"为 0；跑完 SURFACE、DRAW、Canvas 三项编译计数都为 0（G16）。
- **画面测试**（设置页的"试跑"，SYS-GRAPHICS §5）：与 `--bench` 共用本节的代码。60 s 的 F1、默认状态、雾幕进出、界面隐藏、按真实时间运行；先等流式屏障就绪，并断言窗口有焦点（失焦时驱动会限帧）。结果写在画面页注释栏的结果卡上（UI-VISUAL §8.5）：平均帧率、1% 低帧、超过 50 ms 的卡顿次数、GPU p95、能否达到当前预设的帧率目标、建议哪个预设。
- **录像**（G13、跳变检测）：Movie Maker，`--write-movie <目录>\f.png --fixed-fps 60 --quit-after <帧数>`，默认就是固定 60 fps、逐帧输出 PNG；加 §5 的流式屏障。
- **噪声控制**：
  - 每次运行用新进程；A/B 按 ABBA 顺序各跑 5 次。
  - 锁定 GPU 频率（`nvidia-smi -lgc`，需管理员权限），测完恢复。
  - 关闭各种叠加层、录屏和虚拟显示器。开测前要求 GPU 占用连续 3 s 低于 5%。
  - A/A 校准：在集中测试跑 G10 之前做，机器、驱动、引擎和渲染驱动都没变时沿用上次结果；主分支对主分支跑 20 轮，误报不超过 1 轮，且中位数的变异系数不超过 1.5%；达不到就把每组次数加到 10。
- **报告**：
  - 每次输出 `output/perf/<sha>.json`：机器、驱动、Godot 版本、设备档、预设、覆盖项、自动调节级别、世界状态（季节、钟点、天气）、地图，每个视角、每个通道的分位数，掉帧、卡顿，三角形、绘制调用、管线编译计数（含 Canvas），DXGI 显存峰值，GC 次数与暂停；音频主线程（`audio/main_us`）、在播 3D 声源数、混音线程占用（只在 `--audio=on` 的运行里有值）。
  - 每次集中测试的 G10 另在 `perf-history.jsonl` 追加一条，并生成静态 HTML 趋势图：中位数加 95% 区间，标注两次集中测试之间合入的工作包。

### 12.5 门禁（写入 ENG-GATES §2）

- **G8 静态预算**（纯 .NET 测试，不占 GPU，任何工作树都可以跑）：
  - 用与运行时同一套 C# 选级代码，读烘焙产物的统计 JSON，对 V1–V10、E1–E5、P00–P15，全校 64 m 网格上的贴地位姿（离地 1.7 m，8 个朝向），以及行走贴地位姿（W1–W5 上每 20 m 一点、眼高 1.62 m、8 个朝向），预测各包的三角形（主通道 / 阴影）、绘制调用（主通道 / 阴影）和显存。任何一项超过 `budgets.json` 就拒绝。另加的位姿：
    - 开放室内：每处开放入口的门内 3 m 朝外（最坏视角，室内 IF 加门外广场）、门厅、主空间、走廊、楼梯各 1 个，按 WLD-INTERIORS §12 的室内额度判（G19h，WLD-INTERIORS §19）；室内点位 P16–P23（WLD-INTERIORS §2.17）；SYS-WALK §8 的"可进入室内的门内 3 m"并入这一条；
    - 外围：V11、E6、E7 等外围视角，外围子项按 WLD-OUTSKIRTS §11.2 的分层上限核对；
    - 开始页与过场：T0（四种世界状态各算一次）和新游戏路径上每 0.25 s 的采样点（秋黄昏一种，SYS-TITLE §11.4）；
    - 按图跑：位姿清单从本图的 `review.json`、`waypoints.json` 读，`budgets.json` 的 `maps.<id>` 另核本图的 L4、遮挡体、全图纹理、`boot_<地图>.pck` 体积和本机缓存烘满后的体积（SYS-MAPS §12）。
  - 每个位姿按 ENG-BUDGET §2 的四种世界状态组合各预测一次，取最大；天气组合按降水池的实例数、檐滴和天气分支的绘制计入。
  - 四个预设各算一遍（选级代码以 `QualityView` 为输入，烘焙统计按内容类别记数，gen 不认识画面设置），检查单调性（极致 ≥ 精致 ≥ 均衡 ≥ 流畅），另算每个预设的最差值配置的显存（SYS-GRAPHICS §10）。
  - 绘制调用的预测：独立实例按"同网格同材质"合并成一次（乐观估计），MultiMesh 按表面数，阴影按投影物落进的级；用 G10 的实测得出的系数校正（深度层会切开自动实例化）。
  - 显存 = Σ TileBundle 字节 + 纹理表 + 渲染目标公式（§11.7，与设置页的显存估算是同一个函数）。
  - 用 G10 的实测校准，偏差超过 15% 报警；运行时再用 `RenderVideoMemUsed` 复核显存，偏差超过 15% 报警。
  - 碰撞数据的检查（碰撞地面与 L0 地面之差、墙段覆盖、外扩上限）归 G15（SYS-WALK §15）。
  - 安装目录 ≤ 0.9 GB 和分发压缩包 ≤ 0.7 GB 由 G6 管（§8）；本机烘焙产物与 CI 参考哈希的逐文件比对（`--bake-verify`）也在 G6（§5、ENG-GATES §2）。
- **G10 性能**：
  - 在集中测试时由主代理运行（ENG-GATES §2），持有独占的 GPU 锁；测导出的 release 版。
  - 视角：命中本包格子的视角，加上 V1、E1、F1，以及 §12.4 的"V1 夜间""E1 夏季""E1 夏夜雷雨"，另加 T0（默认状态，`--title=1`，SYS-TITLE §11.4）。
  - 预设（SYS-GRAPHICS §10）：精致（1080p）、极致（1440p）和流畅都跑，都只判回归；帧时账只记精致（ENG-BUDGET §8）。
  - 判定：候选版本（当前 `main`）对上一次集中测试的版本，按 ABBA 各跑 5 次。任一视角的 GPU 或 CPU 中位数同时满足以下两条，即判为疑似回归：
    - 单侧 Mann-Whitney U ≤ 2（5 对 5 时 p ≈ 0.016）；
    - Hodges-Lehmann 位移 ≥ max(3%, 0.25 ms)。
  - 疑似回归时，再跑 10 对 10 复核，p < 0.01 才确认拒绝。
  - 位移估计按 `main` 上各包的提交定位到包（ENG-GATES §2），记入该包的帧时账（ENG-BUDGET §8 的帧时占比列），累计超额就拒绝。
  - 集中测试时主分支另与一个冻结的参考版本比较，漂移超过 5% 时报警。
  - **冷缓存子项**（D-165）：空缓存目录、`--bake-bg=1`（不带 `--bake-strict=1`），测两项（初值，§16）：首次运行到转为实时 ≤ 25 s；沿 F1 路线行走一圈，碰撞冻结累计 ≤ 2 s，祖先格兜底时长另作记录。上面的各项一律带 `--bake-bg=0 --bake-strict=1`（§12.4）。
- **G16 管线编译与切图**（ENG-GATES §2 已列）：
  - 基线：`--title=1` 时从"标题页转为实时"那一刻算起；`--title=0` 时在开场雾幕淡出时；
  - 每次雾幕淡出之后（含画面设置的雾幕、传送、切图、读档）SURFACE、DRAW、Canvas 三项计数不增，运行中新生成的 MSDF 字形为 0；"前推 24 小时""四季轮换""天气巡回""预设轮换""过场体验"跑完 DRAW 为 0（§12.4）；
  - 检查路线加开始页停 60 s，依次打开存档页、设置卡的每一页、数据手册，播 `enter_new`、`enter_continue`、`leave` 和一次跳过，进入游戏后打开大地图和设置卡；第一次按 M、第一次换主题，计数都不增（SYS-TITLE §11.2、SYS-ATLAS §10）；
  - 同图重载：`--gate=maptravel` 连做 3 次同图 `Travel`，判泄漏、计数和跨图持有（SYS-MAPS §13）。
- **G17 预设底线**（SYS-GRAPHICS §10，写入 ENG-GATES §2）：
  - 每个预设加一套"最差值配置"：取该预设的值，再把自动调节阶梯能动的项都降到 `KnobSpec.WorstAllowed`，也就是这个预设在自动调节下能到达的最差画面；移轴、光斑、光柱、暗角照常开（关掉之后的画面不验收，SYS-GRAPHICS §2）。在 V1、V4、E1、E3 和 V7 夜间截图，自动判：描线像素占比不低于精致的 70%（初值，§16；用 `--inspect=matid`）、投影存在、倒影存在、影色自检通过（SYS-TIME §8 的四条）、夜间 L* ≥ 12、无黑洞和穿帮、裸面检测（距离乘该预设的细节系数）；
  - 跑 §12.4 的"预设轮换"：不崩溃，显存回到基线 ±5%，每次雾幕淡出后编译计数不增；
  - 在集中测试时全量跑（ENG-GATES §2）。截图部分取共享 GPU 锁，"预设轮换"取独占锁。
- **其他门禁里与性能有关的部分**（门禁本身在各自的文档里定义，ENG-GATES §2 登记）：
  - **G19f 进楼零等待**：软停触发 0 次、一层加载 ≤ 100 ms、"空壳帧"为 0、§5 第 6 条每帧成立（WLD-INTERIORS §19）；
  - **G20 存档**：保存与读档耗时、缩略图那一帧的主线程（SYS-SAVE §13、SYS-SAVE §14）；
  - **G21 声音**：音频主线程平均 ≤ 0.35 ms、p95 ≤ 0.5 ms，混音线程 ≤ 一个核的 10%（SYS-AUDIO §18.1、SYS-AUDIO §22）；
  - **G13** 加传送和过场的录像（SYS-ATLAS、SYS-TITLE §11.2）。
- **表面字节格式比对**（并进 G2，在集中测试时跑；升级引擎时必跑）：同一组输入，比对烘焙器自己打包的表面字节与无界面 Godot 里 `AddSurfaceFromArrays`（压缩标志相同）的输出，逐字节一致才通过。4.2 改过表面格式，有先例。
- **着色器静态检查**（并进 G4，ENG-GATES §2 已列）：不用 `TIME`；vertex() 里按距离的判断不用 `CAMERA_POSITION_WORLD`；不用 `LIGHT_COLOR`；全局参数的声明只出现在 `globals.gdshaderinc` 里。
- **G11 核显实测**（集中测试时跑，可选）：
  - 设备：一台最低配置的笔记本（酷睿 Ultra 第一代的 Arc 核显或 Radeon 780M 同级，16 GB 双通道内存，SSD，ENG-BUDGET §2）；手边没有就跳过这一项，并在集中测试的回报里说明。
  - 准备：接电源，关闭省电模式，窗口全屏，叠加层关闭。
  - 测试：V1、V2、E1 各 30 s × 3 次，然后 F1 循环 10 分钟。先用"自动推荐 + 自动调节开"跑一遍，再固定推荐预设、`--adaptive=0` 跑一遍（SYS-GRAPHICS §10）。
  - 判定：第 9–10 分钟的 p95 除以第 1–2 分钟的 p95 不超过 1.10，且期间自动调节最多多降 1 级；首次分档把这台机器判为最低配置、推荐均衡（这台是 AMD APU 时，UMA 判定也在它上面验）。
- **GPU 锁的分级**：
  1. 静态检查不需要锁（G1–G4、G5 的注册检查、G6、G8）。
  2. 截图（G7、G9、G12、G17 的截图）、G5 的窗口冒烟、替身烘焙取共享锁，最多 2 个同时运行。
  3. 性能测试（G10、G13 的体验脚本、G17 的"预设轮换"）取独占锁，并且写优先：有性能测试在等时，新的截图不得进入。

  锁文件放在 `..\UESTC_RPG.wt\.locks\`，带 PID 和心跳，心跳 60 s 无更新即视为失效。持有独占 GPU 锁时，还要同时持有"重 CPU"锁，阻止其他工作树运行 `dotnet build`、`--import` 和烘焙。

### 12.6 日志与诊断（WP-01；WP-02、WP-32、WP-50 提供读数，WP-35 提供入口）

目的是把试玩时的"卡住了""闪退了"变成能定位的材料。发行版与开发态用同一套，不做在线遥测。

- **一套日志**：用引擎自带的文件日志（`debug/file_logging/enable_file_logging = true`，导出版也开），路径 `user://logs/godot.log`，每次启动轮换，保留最近 5 次（`debug/file_logging/max_log_files = 5`）。各包只经 WP-01 的日志接口（`Campus.Kernel.Inspect`）写日志；接口给每行加前缀：启动后的毫秒与帧号、级别、所属工作包、当前图、流程阶段（`AppState`，切图途中另记 `MapStage`）；错误另带异常类型和堆栈。
- **大小上限**：单次运行经接口写出的日志超过 16 MB 后，只再记警告和错误；同一条消息每分钟最多记 10 次，多出的只计数，在下一条里写"另有 N 条相同"。日志目录因此在约 80 MB 以内（初值，§16）。
- **启动第一段**：构建信息（版本、提交、构建类别，ENG-ARCH §4）、命令行、Windows 版本、CPU 型号与逻辑核数、内存、显卡名与驱动版本、DXGI 显存预算、图形接口、设备档与当前预设。
- **诊断摘要**（`diagnostics.json`，导出时现场生成，只读现有接口，不另存状态）：上面这段启动信息，加上画面预设与手动改过的项、自动调节的当前级别（`IQualityService`）；当前图、位姿、观察模式、流程阶段；世界状态（季节、钟点、天气与天气种子）；本机缓存占用、烘焙进度与失败的键（`IBakeService`）；最近一次成功保存的时间与存档位、最近一次保存失败的原因（`ISaveService`）；最近 20 条警告和错误。
- **导出诊断**：把 `user://logs/` 下的日志和摘要打成一个 zip（`UESTC-diag-<版本>-<序号>.zip`），写进 `user://diagnostics/`（只留最近 3 份），再在文件管理器里打开这个目录（`OS.ShellShowInFileManager`）。默认只含日志和摘要，存档要玩家另外勾选。入口在操作说明页的"打开日志目录""导出诊断"（UI-NAV），故障处理的步骤见 OPS-RELEASE。
- **隐私**：日志和摘要里的本机路径一律写成 `user://…`、`res://…`，不写出 Windows 用户名和安装位置。
- **崩溃**：不强行保存（SYS-SAVE §4）；错误级别的日志立即落盘，闪退后下次启动仍能从轮换保留的文件里导出上一次运行的日志。

## 13 D3D12 与 Vulkan 的选择

兼容渲染器（OpenGL）和 Mobile 渲染器不考虑：前者跑不了 Compositor 和 normal_roughness，后者不按大场景设计。

**默认 D3D12**：

- 4.6 起 Windows 新工程默认 D3D12；
- shader baker 在 D3D12 上加载约快 20 倍；
- PIX 能看 D3D12 的 GPU 时间线。

**Vulkan 的用途**：

- 着色器源码级调试：`--rendering-driver vulkan --generate-spirv-debug-info` 配合 RenderDoc；
- 交叉验证驱动问题：同一现象在两个驱动上是否都出现；
- 兜底：D3D12 不可用时自动回落（`fallback_to_vulkan`）。导出只烘 D3D12 一份着色器，回落时在扉页后面现场编译（§8）。

**截图和基准一律用 D3D12**，两个驱动的结果不混用：像素会有细微差别，G7、G9、G12 的容差按 D3D12 标定（ENG-GATES §1）。

**把默认改成 Vulkan 的条件**（全部满足，并经主代理批准）：

1. 出现 D3D12 特有、在开发机或某一档目标设备上能复现、短期内绕不开的驱动问题（卡顿、错误画面或崩溃）；
2. Vulkan 下 G10 的 GPU 和 CPU 中位数都不比 D3D12 差 3% 以上；
3. Vulkan 下首次运行的管线编译加预热能在雾幕时间内完成（§8 的冷启动目标）；
4. 两个驱动在 G7 视角上的截图差异在容差之内；
5. 切换后重新标定截图容差，并重跑一次 A/A 校准（§12.4）；导出的 shader baker 改发 Vulkan 的一份。

## 14 每个工作包的性能自检清单

1. 只用白名单里的着色器和调色板色名；没有新建 Shader、StandardMaterial3D，没有在运行时改 `render_mode` 或 `#define`；没有新增顶点格式（§1.4、§2）。
2. 容器选型符合 §1.1；同种部件共用网格和材质；MultiMesh 是统一格式（3D 变换加自定义数据，无实例色），成批写入用 `MultimeshSetBuffer`。
3. 逐帧路径零托管分配：`StringName` 预先缓存；没有 LINQ、闭包、装箱、字符串拼接；`_Process` 里没有 `GetNode`；没有计时器；动态全部通过通道接入。风、水、一拍二只读 `anim_time`，着色器不用 `TIME`；界面每秒刷新不超过 10 次（§6）。
4. 不透明着色器里的 discard 和 alpha scissor 只用于镂空类和抖动淡化（§2）。
5. 着色器里按距离的判断用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`，不用 `CAMERA_POSITION_WORLD`；颜色不用 `LIGHT_COLOR`。
6. 静态预算（G8）通过；关掉本包前后的帧时差在帧时占比额度之内。
7. 生成函数是确定性的（同一输入、同一 ABI，产物逐字节一致），写在 `Campus.Gen.*` 里、不引用 GodotSharp，能在工作线程（本机烘焙）和烘焙器里运行；不用 §5 列出的禁用 API（超越函数一律用 `DetMath`），用 `ArrayPool` 和预分配缓冲。每个表面不超过 65 536 个顶点。
8. 释放了自己的实例和 TileBundle（`FreeRid` 加 `Dispose()`）；没有释放共享的材质、图集和数据纹理。
9. `Build()` 不读世界状态（钟点、季节、天气、观察模式），也不读画面设置。时间、季节和天气只经全局参数和全局纹理进入着色器；运行时不改特化开关；每次雾幕淡出之后 SURFACE、DRAW 编译计数不增（ENG-BUDGET §7 第 9、11、18 条）。
10. 碰撞数据本机烘焙（D-165）、与几何同参数；碰撞 body 只在主线程入树，入树前加完全部形状（ENG-BUDGET §7 第 12 条）。
11. 用到的全局参数都在 §1.5 的登记表里，有唯一的写入方；没有读回全局参数（ENG-BUDGET §7 第 13 条）。
12. 投影物在相机可见的图层上；草、插片、粒子、光晕不投影；远处用 `ShadowsOnly` 代理（ENG-BUDGET §7 第 14 条）。
13. 工作线程上没有调用场景树、`RenderingServer.Instance*` 和 PhysicsServer3D 的 body 接口（ENG-BUDGET §7 第 16 条）。
14. 画面设置只经 `IQualityService` 和 `[QualityResponsive]` 接收，不读 `graphics.cfg`；本包负责的单项按 §11.7 的生效类别落到引擎上，有视觉变化的淡化完成、不硬切；本包的风格底线内容在每个预设的最差值配置下仍然在（ENG-BUDGET §7 第 17、18 条，G17）。
15. 天气只读天气向量和天气的全局参数、纹理（§1.5），不自己维护天气状态；用到的节点启动时就在树里，运行时只改数值（ENG-BUDGET §7 第 9 条）。
16. 全图的纹理和包围盒一律从 `map_rect`、`MapDescriptor.Bounds`（跟相机走的降水池用 `Center` ± `roam.maxRadius`）换算，不写死"全校"的原点和尺寸；跨图常驻的节点只经 `IMapAware` 换数据，不跨图持有格子句柄、`FeatureId` 或纹理内容（SYS-MAPS §4.3、SYS-MAPS §10）。
17. 出声的包：在播 3D 声源走 WP-53 的池；没有用 `AudioStreamRandomizer`，也没有改 `PlaybackSpeedScale`（SYS-AUDIO §16）。
18. 界面：只用 `ui_panel`、`ui_screen`、`map_base` 三个画布着色器，组件在扉页后面预热或预建；运行中不新生成 MSDF 字形，不在打开时创建大页控件（UI-VISUAL §11.9）。
19. 法线与换级：倒角盒、倒角扫掠用 WP-09 内核的构造器（大面平法线、倒角带过渡法线），建筑平面的法线由平面方程算；圆件用平滑法线或解析法线；只想改明暗的效果不写 `NORMAL`（§2）。跨级持续存在的面用同一法线、色号和材质 ID，剪影和投长影的构件留到不足 1 px，小件的线先于几何淡出（§1.2）。

## 16 待验证（P0）

下列做法与数值在 P0 原型或实测之前按"验证前的初值"施工，验证后改回对应小节。

| 待验证 | 验证前的初值 | 验证方法与负责 | 位置 |
| --- | --- | --- | --- |
| 淘汰时 `FreeRid` 加 `Dispose()` 让 Mesh RID 立即释放，不等 GC | 按立即释放写 | P0 看 `RenderVideoMemUsed` 能否回到基线，WP-02 | §1.2 |
| 自动实例化在长段上的实际命中率（深度层会切开） | 同种部件共用 Mesh RID 与材质 RID | P0 实测（§12.2 的自定义监视项），WP-01 | §1.1 |
| 全局数据纹理只更新内容时 RID 不变、材质不失效；切图时 `SetImage()` 后 RID 是否保持、会不会触发编译 | 按不失效、不触发编译写 | P0 验证（SYS-MAPS §15），各写入方、WP-01 | §1.5 |
| 全局 sampler 上的 `source_color`、`filter_*`、`repeat_*` 提示是否生效 | 按生效写；不生效时季相行改 `texelFetch` 手工插值，`cloud_noise` 手工取 `fract` 并选 mip | P0 实测，WP-04、WP-40 | §1.5 |
| 外围季节覆盖潜势放在 VF-A 的哪个字节 | 不新增顶点格式 | WP-47 与 WP-04 定 | §1.5 |
| 全场 ShaderMaterial 实例数 | ≤ 48 | G10 实测，WP-04 | §1.6 |
| 抖动淡化的写法：单个着色器带淡化分支，或预热过的淡化变体 | 单个着色器带淡化分支 | P0 对比，WP-04 | §2 |
| 过度绘制指标 | 颜色通道平均 ≤ 1.3 次；预通道平均 ≤ 2.5 次、峰值 ≤ 6 次 | P0 用 RenderDoc 的 Quad Overdraw 实测，WP-05、WP-23 | §3.3 |
| 雪花的屏幕覆盖 | 与雨丝同一套上限 | P0 用 Quad Overdraw 看一次，WP-45b | §3.3 |
| 遮挡剔除的 CPU 开销与收益 | 开启（离地 60 m 以上关） | P0 在本机和一台最低配置的笔记本上各测一次，WP-33 | §4 |
| 打包好的表面存进 ArrayMesh 用哪个接口 | 烘焙器自己按表面字典打包 | P0 原型定，WP-02 | §5 |
| 碰撞形状在工作线程上由参数创建是否线程安全 | 按线程安全写；不安全时在主线程建形状（`.tbin` 本就只存形状参数） | P0 验证，WP-43 | §5 |
| 主线程每帧预算各项 | §6 表中的值（合计约 5.5 ms，开发机余量 ≥ 0.15 ms） | P0 在本机和一台最低配置的笔记本上实测校准，WP-01；主代理核账写回 ENG-BUDGET §8 | §6 |
| 主通道 2000 次、阴影 2500 次绘制的提交开销 | 计在"渲染 CPU"一行 | P0 在本机和一台最低配置的笔记本上各测一次，WP-01 | §6 |
| GC 门禁 | F1 一圈里第 2 代回收 0 次，单次暂停 ≤ 1 ms（缓存已烘满时测） | P0 实测后定，WP-01、WP-27 | §6 |
| 后台烘焙期间的 GC 暂停 | 先记录第 2 代回收的次数与暂停，判据 P0 后定 | P0 在后台预烘跑满时实测，WP-01、WP-02 | §6 |
| 导出版用 ReadyToRun 减少首帧 JIT | 不用，靠雾幕或扉页期间空跑 | P0 评估，WP-01 | §6 |
| 纹理显存上限 | 384 MB，四档相同 | 美术需要更多纹理时由主代理按 ENG-BUDGET §2 重定（同时放宽流畅、均衡的显存合计） | §7 |
| Jolt 内部结构的内存 | 另计 | P0 实测，WP-43 | §7 |
| 安装目录与分发压缩包的实测体积 | 合计约 0.45–0.75 GB；G6 上限：安装目录 ≤ 0.9 GB、分发压缩包（7-Zip）≤ 0.7 GB | P0 导出实测安装目录，并用 7-Zip 打包实测压缩包，后重估，主代理、WP-01 | §8 |
| 磁盘布局 | §8 的目录树（`boot_<地图>.pck`、`user://cache/bake/`） | P0 用导出验证，WP-01、WP-02 | §8 |
| 启动时间目标 | 扉页热启动 ≤ 1.0 s、冷启动 ≤ 1.5 s；转为实时热启动 ≤ 3.5 s、冷启动 ≤ 9 s（缓存已烘满） | P0 实测（含预热总时长，§11.7），WP-01（SYS-TITLE §1.2） | §8 |
| 首次运行到转为实时的时间 | ≤ 25 s（开发机，含本机烘焙）；平面地图底图 8 线程 ≤ 20 s | P0 在空缓存目录上实测，WP-02、WP-48a、WP-49 | §5、§8 |
| 各类产物的单件生成耗时与全图预烘总时长 | L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms（开发机单线程）；开始页烘满全图 ≤ 4 min | P0 实测，WP-02 与各烘焙子任务的负责包（WP-48a、WP-51、WP-47） | §5 |
| `.tbin` 的读入与建网格耗时 | L0 一格读文件、解压、哈希校验到 `MeshCreateFromSurfaces` 建完合计 ≤ 30 ms（ENG-BUDGET §3 分块表的读入上限） | P0 实测，WP-02 | §5 |
| `DetMath` 的精度与速度 | WP-00 提供的纯 C# 实现，误差界由其单元测试给出 | P0 实测，WP-00 | §5 |
| 本机缓存烘满后的实际体积 | 一张图约 0.8–1.4 GB；默认上限 2.5 GB | P0 实测，WP-02（预算见 ENG-BUDGET §8） | §5 |
| 植被近池重建耗时（银杏大道 1600 株） | C# 不超过 10 Hz 重建 | P0 实测；不够快时改计算着色器剔除加间接绘制，WP-23 | §9 |
| L2 替身方案 | 团块低模与半八面体替身二选一 | P0 在 E1、银杏大道、西湖岸的 150 m 切换距离上对比，四季各验一次，WP-23 | §9 |
| 云团型树冠的三角形 | §9 云团型表中的值（L0+ ≤ 1 万） | P0 核实，WP-23 | §9 |
| 叶卡保覆盖率 mip 的做法 | 带保覆盖率 mip 的 DDS（BC7），或 fragment 里按 `textureQueryLod` 提高 alpha | P0 选一（数组纹理能否用 DDS 一并核实），WP-23 | §9 |
| 降水的 GPU 增量 | 小雨约 +0.3 ms，大雨和雷雨 +0.6–0.8 ms，大雪约 +0.3 ms（独显高 1440p） | P0 按"E1 夏夜雷雨"基准实测，WP-45b；超出时由主代理决定收紧数量还是放宽上限（ENG-BUDGET §8） | §9 |
| 均衡、流畅 2 级阴影的 `Split1` | 0.15 | P0 看图，WP-03 | §10.1 |
| 低太阳下长影边的锯齿 | 约 100 m 处 20 px | P0 看图处理，WP-03 | §10.1 |
| `DirectionalShadowPancakeSize` | 默认 | P0 按低太阳调，WP-03 | §10.1 |
| 室内阴影方向的最低高度 | 14° | P0 原型看图（12°、14°、16° 的 A/B，秋 17:30 的教室和走廊），WP-03 | §10.1 |
| 方向量化与始终连续转动 | 量化，δ = 0.01° | P0 A/B（秋 17:30 的银杏大道和宿舍区）后定 δ，WP-03 | §10.1 |
| 阴影空跑切换前后的编译计数 | 不增 | P0 核实 SURFACE、DRAW 计数（G16），WP-03 | §10.1 |
| 夏正午满叶（晴）4 级全量重渲的 GPU 时间 | 按 ENG-BUDGET §2 的 22% 份额核算 | P0 头号测量项；超出时依次收紧小件投影距离、植被代理密度、第 4 级的 max_distance，WP-03 | §10.1 |
| 远影图 | 不做 | 只在 P0 证明第 4 级不够用时才做，WP-03 | §10.1 |
| 后期链开销 | 合计约 1.3–2.4 ms（1440p） | P0 实测，WP-05 | §10.2 |
| 柔光换级的观感 | `SetGlowLevel` 0.5 s 渐变 | P0 看图确认，WP-05 | §10.2 |
| Texture3DRD 能否直接给 `AdjustmentColorCorrection` | 能 | P0 确认；不能时在 `PostTransparent` 里直接算 CDL，WP-05 | §10.2 |
| 描线两次 dispatch 的开销 | 沙盒实测 0.32–0.42 ms（1440p，RTX 4080），目标 ≤ 0.15 ms | P0 优化后再测，WP-05 | §10.2 |
| 描线的最小宽度 | 名义宽度 ≥ 1 px 的线，内部宽度不小于 1 个内部像素 | P0 在 0.5、0.59、0.67、1.0 和超采样 1.25 五档比例下截图比较，WP-05 | §10.2 |
| SMAA 是否侵蚀 1 px 的线 | 用内置 SMAA | P0 检查；会侵蚀时关内置抗锯齿、启用 `post_fxaa.glsl`，WP-05 | §10.2 |
| 极致预设的超采样 1.25× | 默认不做 | P0 测开销，要加时在 SYS-GRAPHICS 登记，WP-05、WP-32 | §10.2 |
| 室内棱柱光柱的屏幕覆盖率 | ≤ 0.6 屏 | P0 实测，WP-39 | §10.2 |
| 窗灯的过渡宽度 Δ | Δmin = 0.005、Δmax = 0.15 | P0 看图，WP-39 | §10.3 |
| `refl_lamps` 每个湿面片元读 16 盏灯的开销 | 16 盏 | P0 实测；超了按屏幕距离只取最近的 8 盏，WP-24 | §10.3 |
| 倒影每次更新的开销 | 0.3–0.6 ms | P0 实测，WP-24 | §10.4 |
| 级联 4 → 2 的时机 | 只在阴影淡时（`shadow_strength` ≤ 0.3）或下一次雾幕做 | P0 录像判 ΔE，WP-32 | §11.3 |
| 可变刷新率的默认 | 刷新率 ≥ 120 Hz 时开 | P0 实机确认，WP-32 | §11.4 |
| 行走物理的每帧额度 | 预计 0.05–0.3 ms | P0 原型重定，WP-42 | §11.4 |
| AMD APU 上的 UMA 判定 | 按 DXGI 的 UMA 标志 | G11 真机验证，WP-32b | §11.6 |
| G17 描线像素占比的下限 | 不低于精致的 70% | P0 截图标定，WP-27 | §12.5 |
| 网格 LOD 的"误差 + 每级索引数组" | 连续表面给新的索引数组；前缀只用于整件拿掉独立小件 | P0 用墙窗、倒角盒、树冠三个代表件各出两级，看有无破洞、轮廓与倒角高光是否完好，`lods` 写入与 `.tbin` 往返一致；WP-09、WP-02 | §1.2 |
| 首次分档限时采样的样本数 | 2.5 s 内两档各 ≥ 20 个有效样本，不够就取保守结果 | P0 在一台最低配置的笔记本上看样本数与选档结果，WP-32 | §11.2 |
| 烘焙停滞的判定 | 等待的那批请求 60 s 没有进展 | P0 在一台最低配置的笔记本上看最慢的单件和底图子步骤的间隔，WP-02 | §5 |
| 日志上限 | 单次运行 16 MB，保留最近 5 次，同一消息每分钟 10 次 | P0 看首次运行和 F1 循环 10 分钟的日志量，WP-01 | §12.6 |
| 法线描线软阈值的观感；近处是否保留居中的折角线 | 覆盖率 = 1 − smoothstep(0.62, 0.78, dot)；近处让给倒角提亮和明暗交界 | P0 在石材楼金样的 1.7、5、30 m 机位做 A/B；要保留时在 `mat_table` 行 3 的保留通道登记倒角半径，采样偏移加宽到跨过倒角带（上限 4–6 px）并加非极大抑制，WP-05 | §10.2 |
| 倒角提亮的幅度与曲率阈值 | 受光 +0.05 L、影里 +0.02 L；κ 在 2–4/m 之间过渡 | P0 金样截图（默认状态与夏正午），WP-04 | §2 |
| 凸边亮线与暗线的关系、亮线幅度；投影里的近似 | 暗线照常，亮线并排画在朝光一侧，取现有颜色 +0.05 L；120–150 m 淡出；投影里只剩影色略提 | P0 看图，WP-05；投影里那条淡线不能接受时，由主代理决定是否给 toon 加一个"受光"标志的输出（要占缓冲，属新增全屏数据） | §10.2 |
| 解析法线的观感 | 视空间的中心与轴经 `flat` varying 传给片元 | P0 用灯杆、球形灌木在 0.5–5 m 截图，看明暗交界是否圆润、降段时交界不动，WP-04、WP-13 | §2 |
| 抖动过渡期间的描线噪声；低太阳下投影构件换级的影子跳变 | 两级基面共面；投影构件的误差乘影长系数 | P0 用跳变检测专看 L0 → L1，用默认状态（秋 17:30）的推镜头录像，WP-02、WP-05 | §1.2 |
| L2 窗带影边与 L1 的明度差 | ≤ 3% | P0 在秋 17:30 与夏正午各测一次，WP-14、WP-04 | §1.2 |
| `toon_decal` 是否还有用途 | 保留为透明贴花的备选 | P0 贴面几何做完以后看有没有它做不了的贴花；没有就从白名单删去，WP-04 | §1.4 |
