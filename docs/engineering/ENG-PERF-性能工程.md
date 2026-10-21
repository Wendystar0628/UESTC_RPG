---
id: ENG-PERF
title: 性能工程
category: engineering
parent: ENG-INDEX
summary: 性能总原则与绘制管线：容器与合批、着色器白名单与全局参数、预热、填充率、剔除与图层
keywords: [着色器白名单, 全局着色器参数, ShaderGlobals, 数据纹理, 合批, 自动实例化, MultiMesh, 容器选型, 管线预热, 抖动淡化, 级别过渡, 填充率, 遮挡剔除, 渲染图层, 顶点格式, D3D12]
depends: [ENG-ARCH, ENG-BUDGET, SYS-TIME, UI-NAV, SYS-WALK, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO]
related: [ART-STYLE, ART-SKYLIGHT, WLD-VEGETATION, WLD-WATER, SYS-MOTION, SYS-VIEW, ENG-GATES, ART-FIDELITY, WLD-TERRAIN]
contracts: [ShaderGlobals]
data: ["game/style/shaders/", "game/style/outline/"]
gates: [G5, G7, G20, G21]
paths: ["game/data/pipeline-combinations.json", "game/style/shaders/globals.gdshaderinc.uid", "game/style/shaders/globals.gdshaderinc", "game/kernel/renderer/**", "game/kernel/diagnostics/**", "game/kernel/inspect/**", "game/kernel/frame/**", "game/kernel/boot/**", "game/stage/**"]
---
# ENG-PERF · 性能工程

> **范围**：本文规定性能的总原则与绘制管线：容器与合批、着色器与全局参数、预热、填充率、剔除与渲染图层。Godot 4.7.2-stable .NET，Forward+，只做 PC；目标是视觉底线处处成立、细节按精度区分级、绝不卡帧。
> **不在本文**：预算数值与硬规则（ENG-BUDGET，唯一的数值家）；流式、烘焙与内存（ENG-PERF-STREAM）；植被、阴影与后期（ENG-PERF-LIGHT）；自适应与测量（ENG-PERF-ADAPT）；契约（ENG-ARCH §3）；画面设置的单项与预设值（SYS-GRAPHICS）；门禁与命令行（ENG-GATES）。
> **查阅**：先读 §0；容器与合批看 §1；预热看 §2；剔除与图层看 §4

## 0 结论

1. **引擎锁定 Godot 4.7.2-stable .NET**，Forward+，默认 D3D12，Vulkan 只用于调试与兜底（ENG-BUDGET-RULES §2）。
2. **场景只用白名单着色器**（§1.4）；材质特性靠调色板、顶点标志位、全局参数在着色器内分支，运行时不新建 Shader；加载界面淡出之后管线编译计数不增（预热见 §2）。
3. **瓶颈在两头**：CPU 的提交与剔除，GPU 的多遍（预通道、颜色、4 级阴影）与后期。合批靠离线合并、自动实例化、MultiMesh（§1）；阴影只能靠减少投影物省钱（ENG-PERF-LIGHT §2.1）。
4. **阴影用引擎原生 PSSM**；描线、边缘光、移轴、光斑、倒影、窗灯、路灯光斑自己实现；不用的引擎特性清单见 ENG-BUDGET-RULES §2。
5. **主线程只挂载，不生成**；绝不卡帧、不穿地靠六条不变量（ENG-PERF-STREAM §1.2）。
6. **预算按场景核算**，不按格平均；测量分吞吐（判回归）与体验（判达标）两类（ENG-PERF-ADAPT §2）。

子文档：

- **ENG-PERF-STREAM**（流式加载、本机烘焙与内存）：做流式、烘焙、碰撞挂载、内存时读。
- **ENG-PERF-LIGHT**（植被、阴影、后期与灯光）：做植被、阴影、描线、窗灯路灯时读。
- **ENG-PERF-ADAPT**（自适应与测量）：做自动调节、帧节奏、基准与剖析时读。

### 0.1 接入要点

- **上材质**：只经 `ToonSpec` 与调色板色名；不自建 Shader；逐物体变化放顶点色号、`INSTANCE_CUSTOM` 或 instance uniform；运行时只许用预热过的着色器与顶点格式组合（§1.1、§2）
- **新增着色器或顶点格式**：要主会话批准；新组合进预热全集，可开关的节点启动时就放进树里（§1.4、§2）
- **着色器读时间、季节、天气、介质、画面设置**：只读已登记的全局参数，每项一个写入方；新增先登记，不读回（§1.5）
- **显示东西**：经 `IRenderHost` 申请，按 §1.1 选容器
- **投影**：放在相机可见层上，按 §4 选层
- **调参数、改设置**：不能触发管线重编译；会改特化常量的设置启动时定，画面设置按即时、加载、重启三类生效，时间、季节、天气、介质只改全局参数与纹理（§2、ENG-PERF-ADAPT §1.2）

## 1 绘制管线与合批

### 1.1 容器选型

- 静态独有的块几何：离线按"材质 × 不超过 16–24 m 的块"合并，经 RenderingServer 挂载；块小，遮挡剔除才有效。
- 每格同种不超过约 64 个的重复部件：独立实例，共用 Mesh 与材质，靠 Forward+ 自动实例化合成一次绘制；超过约 64 个用 MultiMesh，范围取"格 ∩ 楼"。
- 植被、草、落叶、降水、荷、萤火虫、光晕：MultiMesh。动态物体：场景节点。L4、遮挡体、外围 O2：常驻合并网格。
- 自动实例化要求相邻元素的排序键全同：同种部件共用 Mesh 与材质，逐实例变化放 instance uniform 或顶点属性，不用材质覆盖；全场 ShaderMaterial 目标 ≤ 48 个。

### 1.2 MultiMesh、网格 LOD 与级别过渡

- MultiMesh 格式统一（3D 变换加自定义数据，不用实例色，它会改坏色号），成批整块写；`CustomAabb` 罩住全部位移，跟相机走的降水池用可达范围；密度与自动调节用 `VisibleInstanceCount`，不重编译。
- 网格 LOD 只换索引；级别过渡一律在 toon 着色器里按 Bayer 抖动淡化（0.25 s），visibility range 的 `fadeMode` 一律 Disabled（引擎自带淡化强制走透明管线，描线依赖的缓冲会缺）。
- **跨级不跳线、不跳明暗**：同一表面跨级保持法线、色号、材质 ID；同级 LOD 只做不足 τ 的变化；剪影与投影构件留到看不见；线先于几何消失；抖动期间两级基面共面；跨级明度差口径见 ENG-BUDGET-LOD §1。

### 1.3 调用口径

绘制调用读 `Viewport.GetRenderInfo`（主通道 Visible、阴影 Shadow，界面 Canvas 另计）；三角形以内核自记为准，引擎计数只作对照，报警口径见 ENG-BUDGET §4。

### 1.4 着色器白名单

白名单以 `scripts/gates/shader-whitelist.json` 的根数组为准，新增要主会话批准。原则：

- **功能并进现有着色器的分支，不新增文件**：天空类（星空、云、雨幕前锋、闪电、彩虹）并进 `sky`，光晕并进 `fx_glow`（当前只有窗灯光晕和灯头光晕两个分支；光柱、光锥、细尘与萤火虫没有现成分支，实际交付时按消费者接入），湿面与天光方向并进 `toon.gdshaderinc`，雨点环并进 `water`；外围与室内用现有着色器的分支与图层；季相、覆雪、月影是全局参数分支。
- 读屏幕纹理的界面效果必须单独成文件（`ui_screen`）：着色器里只要出现 `hint_screen_texture`，引擎就在它之前做背景拷贝，与分支是否执行无关。
- 透明物只有近景透明窗（同屏 ≤ 32 块）、水的透明变体（可选）、粒子、光晕、光柱、细尘、介质覆盖层；贴面贴花留在不透明通道。
- 玩家角色着色器 `toon_figure` 与蒙皮格式 VF-E 尚未实现，不能当作已有能力。

### 1.5 全局参数与数据纹理

世界状态的连续量走全局着色器参数，数组一律做成数据纹理（全局参数不支持数组）。

- **登记以代码为准**：名字见 `game/contracts/Materials/ShaderGlobals.cs`，类型与默认值见 `game/project.godot` 的 `[shader_globals]`，G5 比对；未登记着色器编译失败，新增先申请。
- 写入方与读回的规则见 ENG-BUDGET-RULES §1 第 13 条。计算着色器读不到全局参数，Compositor 的 C# 每帧把需要的项写进 push constant。
- 数据纹理只更新内容、不换 RID；全图纹理共用 `map_rect`，范围外取"无数据"；外围不读全图纹理，季节覆盖潜势写进顶点；切图时写入方在 `OnMapLoaded` 重建；释放权归内核全局纹理登记处。色号编解码只在契约 `PaletteEncoding` 里一份。
- 数据纹理同样以代码登记为准；调色板行布局见 ART-COLORSCRIPT §1.1。

## 2 着色器与编译

- **会卡顿的**：运行中首次出现的"着色器 × 网格"组合、材质覆盖、中途改特化常量对应的设置（软影滤波档位会清空全部管线）、首次开 FSR2、运行时新建 Shader；所以都不做成画面选项。
- 顶点格式只用登记的 4 种（块几何、窗格、植被插片、面片与粒子），新增要批准；剔除模式只有 `cull_back`、`cull_disabled`。
- **预热（D-261）**：按"着色器 × 顶点格式 × 渲染状态"组合做；全集在构建期由白名单着色器与烘焙统计定出。启动时在扉页下把每个组合隐藏实例化一次，交给引擎的管线预编译与 ubershader（Godot 4.4 起 Forward+ 的做法），一批做完。同一着色器换参数值不需要新管线，所以不逐材质实例预热；不画样本、不按帧数等稳定。格的挂载、LOD 切换与撤盖不等预热。
- 二维画布着色器 × 绘制类型各在扉页下实例化一次（引擎对 2D 没有预编译）；MSDF 字形导入时预渲染；可开关的节点启动时就在树里；大页控件不预建隐藏（挂上界面材质就占全局实例参数池的槽）。
- TileBundle 统计列出本格用到的组合，查不到是烘焙错误，不在运行时编译。无界面模式不编译着色器，G5 要加窗口冒烟运行。
- **验收**：加载界面淡出后 SURFACE、DRAW、Canvas 三项编译计数不增（ENG-BUDGET-RULES §1 第 9 条）。
- **写法**：基于全局参数、instance uniform 的分支几乎免费，逐像素分叉里不采样；`light()` 对每光源每像素执行，点光分支只做差值；`NORMAL` 会进描线缓冲，倒角提亮、假倒角、贴花都不写它；贴面贴花并进所在块，不用 `discard`、不用引擎 Decal。

## 3 填充率与带宽

- 不开额外缓冲（MSAA、TAA 与屏幕空间特效，ENG-BUDGET-RULES §2）；材质区分编进 ROUGHNESS 的 7 位材质 ID。
- 预通道全场开（描线离不开它），颜色通道过度绘制接近 1；叶片卡按 alpha 轮廓裁小；玻璃靠室内映射按不透明画；雾在材质里算并写 `FOG`；水面默认不透明。
- 渲染比例随预设、用 FSR1，只在设置界面改（加载类）；不做连续动态分辨率。

## 4 剔除

- 格由自写四叉树做视锥测试，实例与块由引擎剔除，距离用 visibility range；镜头在地下时相机 `CullMask` 去掉地上各层。
- **图层**（相机、主光 `ShadowCasterMask`、点光 `LightCullMask` 都按它；新层须向主会话申请）：1 建筑与道具；2 地面铺装（不投影，点光排除）；3 小件投影物（"细小物件投影"关时去掉，只在加载界面下切）；4 植被可见几何（不投影）；5 `ShadowsOnly` 投影代理；6 草、落叶、粒子、光晕、光柱（不投影）；7 地下；8 室内（仅墙与楼板投影）；9 水面与水下；10 远景环、雾海、截面、外围 O2。
- **遮挡剔除**用引擎 `OccluderInstance3D`（不作用于阴影通道），不用离线可见集（镜头能穿墙）。每栋楼内缩 0.5 m 拉伸到檐口，每图一份随 L4 常驻，避免流式增删重建 BVH；块粒度保持在 16–24 m。离地 60 m 以上关闭；开放楼内显示室内遮挡体、隐藏本楼外包遮挡体；关着的楼里或离外墙不足 1 m 时关闭；室内内容忽略遮挡剔除。

## 5 待验证（P0）

按初值施工，验证后改回对应小节并删去本条。

- 全局数据纹理只更新内容时 RID 不变；全局 sampler 的 `source_color`、`filter_*`、`repeat_*` 提示是否生效。
- 自动实例化在长段上的命中率；抖动淡化用单着色器分支还是淡化变体；遮挡剔除的 CPU 收益。
