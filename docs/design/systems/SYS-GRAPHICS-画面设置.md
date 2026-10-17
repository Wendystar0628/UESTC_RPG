---
id: SYS-GRAPHICS
title: 画面设置：预设、单项、风格底线、自动推荐与自适应
category: systems
summary: 四档画面预设与单项、风格底线、应用流程、首次分档与自动调节、持久化与命令行、契约与验收
keywords: [画面设置, 画质, 画面预设, 设备档, 流畅, 均衡, 精致, 极致, 单项, 风格底线, 风格默认, 自动调节画质, 自适应, 首次分档, 渲染比例, 超采样, 雾幕类设置, 画面页, 显存估算, IQualityService, IQualityAware, QualityResponsive, QualityKnob, QualityView, KnobSpec, quality-presets.json, graphics.cfg, engine_override.cfg, reset-graphics.cmd, 预设轮换]
owners: [WP-32]
contributors: [WP-01, WP-02, WP-03, WP-04, WP-05, WP-07, WP-23, WP-24, WP-27, WP-35, WP-38, WP-41, WP-45, WP-47]
depends: [ART-STYLE, ENG-ARCH, ENG-BUDGET, ENG-PERF]
related: [CORE-INDEX, ENG-GATES, ART-FIDELITY, SYS-TIME, UI-NAV, UI-VISUAL, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO, OPS-RELEASE]
contracts: [QualityPreset, DeviceTier, ApplyClass, KnobKind, QualityKnob, KnobSpec, QualityProfile, QualityView, ApplyOrigin, QualityChange, KnobDelta, ApplyPlan, ApplyStatus, ApplyResult, IQualityService, IQualityAware, QualityResponsiveAttribute]
data: ["game/data/quality-presets.json", "user://graphics.cfg", "user://engine_override.cfg"]
gates: [G2, G5, G7, G8, G9, G10, G11, G12, G13, G14, G16, G17]
---

# SYS-GRAPHICS · 画面设置：预设、单项、风格底线、自动推荐与自适应

> **范围**：本文规定画面设置：设备档与四档预设、各单项的取值与生效方式、不随画质关掉的风格底线、应用流程、首次分档与自动调节、`graphics.cfg` 与命令行、契约、画面页的内容与验收。
> **不在本文**：画面页的视觉（UI-VISUAL §8.5）；声音设置（SYS-AUDIO §13）；设备档的预算与帧率目标（ENG-BUDGET §2）；自动调节的控制律（ENG-PERF §11.3）；开放室内的介质呈现（WLD-INTERIORS）。
> **查阅**：先读 §0；预设取值看 §1；风格底线看 §2；单项、生效方式与负责方看 §3、§7；分档与自动调节看 §5；`graphics.cfg` 与命令行看 §6；契约与 `[QualityResponsive]` 看 §8；验收看 §10
> **分工**：WP-32 主责（32a 画面设置、32b 分档与自适应），WP-35 做画面页；各单项的负责方见 §3、§7。
> **关联**：[ART-STYLE 风格基础](../art/ART-STYLE-风格基础.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH-工程架构.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET-性能预算与硬规则.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF-性能工程.md)；另见 [CORE-INDEX](../README.md)、[ENG-GATES](../engineering/ENG-GATES-验收与门禁.md)、[ART-FIDELITY](../art/ART-FIDELITY-视觉精度标准.md)、[SYS-TIME](SYS-TIME-时间与季节.md)、[UI-NAV](../ui/UI-NAV-界面与操作说明.md)、[UI-VISUAL](../ui/UI-VISUAL-界面视觉语言.md)、[SYS-TITLE](SYS-TITLE-开始页与进入过场.md)、[SYS-SAVE](SYS-SAVE-存档与持久化.md)、[SYS-MAPS](SYS-MAPS-多地图.md)、[WLD-OUTSKIRTS](../world/WLD-OUTSKIRTS-边界与外围.md)、[WLD-INTERIORS](../world/WLD-INTERIORS-室内.md)、[SYS-AUDIO](SYS-AUDIO-音频.md)、[OPS-RELEASE](../ops/OPS-RELEASE-发布与交付.md)

## 0 结论

1. **两层概念**：
   - **设备档**是硬件分类（独显高、独显中、最低配置，ENG-BUDGET §2），由首次分档得出，只决定推荐哪个预设、帧率目标和自适应的预算。最低配置是近两年的核显轻薄本：酷睿 Ultra 第一代（Arc 核显）或 Radeon 780M 同级及以上、16 GB 双通道内存、SSD；更旧更弱的核显（UHD 6xx、Vega 8、Iris Xe、680M 等）不做适配，不另设档（D-169）；
   - **画面预设**是一组单项取值，玩家能选、能改。极致 ↔ 独显高，精致 ↔ 独显中，均衡 ↔ 最低配置；流畅不对应设备档，是重场景和自动调节时的兜底预设，玩家也可以手动选。改动任何单项，预设显示为"自定义"。
2. **风格底线不做开关**（§2）：描线、分阶明暗与按材质的影色、柔光（高光晕开）、平涂贴图、实时阴影、西湖倒影等在任何画质下都保留，低画质只降渲染比例、阴影图、距离和密度，不降画风和近景建模。
3. **单项按生效代价分四类**（契约 `ApplyClass`，§8）：即时（只改数值）、分帧（即时生效，分多帧重写完成，每帧不超过 0.3 ms，如细节距离，§3）、雾幕（要重建渲染缓冲，点"应用"后在 0.3 s 雾幕里批量生效）、重启。**会清空管线的设置不做成选项**，四档统一取值，所以切换设置永远不触发管线编译。
4. **画面设置只改运行时的选级和引擎参数**：不改烘焙产物、碰撞、世界状态和种子，也不按预设各烘一套，所以本机烘焙缓存跨画质共用（SYS-MAPS §2.4，D-165）。生成器（`game/gen`）完全不感知画面设置。
5. **自动调节画质默认开**：只往下调，永远不超过玩家所选；玩家手动改过的项被锁定，自动调节跳过它。
6. **画面设置不影响声音**：声音的设置在 UI-NAV §4.6 的"声音"页（SYS-AUDIO §13），存 `user://audio.cfg`，与本文件互不相干。

### 0.1 接入要点

- **系统要响应画面设置**：在实现 `IQualityAware` 的 `[KernelNode]` 上标 `[QualityResponsive]`，`Owns` 列自己负责落到引擎上的单项（每项恰有一个负责方，G5 核对），`Reads` 列只读的项；设置经 `IQualityAware.ApplyQuality()` 送达（§7、§8）
- **要读画面设置**：只经 `IQualityService` 和 `IQualityAware.ApplyQuality()`，任何包都不读 `user://graphics.cfg`；着色器要的细节系数、植被密度、草半径读全局参数 `quality`（vec4，WP-32 写）（§8）
- **做会画进画面的东西**：风格底线（描线、分阶明暗与按材质的影色、柔光、平涂贴图、雾、天空、调色板与 LUT、窗灯与路灯光斑、季节内容、一拍二、剖切与介质呈现）不做开关，任何预设、手动值、自适应级别都不能关；实时阴影、西湖倒影只能降到最差值（§2）
- **要给低画质省开销**：只降渲染比例、阴影图、距离和密度，不降画风和近景建模；季节内容只降密度、不缺席（§0、§2）
- **要新增画面单项**：在 `QualityKnob` 末尾追加（契约只增不改），写好 `KnobSpec`（负责方、生效类、种类、最差值）和预设表 `game/data/quality-presets.json` 里的取值（§7、§8）
- **要新增引擎设置、后期效果或着色器变体**：会清空管线的设置不做成选项；切换任何设置都不得触发管线编译，新的可达组合要进启动预热的并集（§3）
- **负责一个会被自动调节的单项**：每级降档在 0.5–1 s 内淡化完成，不硬切（G13）；分帧生效的每帧不超过 0.3 ms；玩家手动改过的项被锁定，自动调节跳过它（§3、§5）
- **负责雾幕类单项**：在雾幕全黑时按负责方顺序执行（WP-32 → WP-03 → WP-05 → WP-04 → WP-02），不编译管线（§4）
- **界面或其他包要改画面设置**：只调 `IQualityService.Plan()`、`Apply()`；跳转、传送、换季、切图、读档、过场等遮挡期间返回 `Deferred`，结束后再执行（§4）
- **生成器与烘焙**：`game/gen` 完全不感知画面设置，不按预设各烘一套；画面设置不改烘焙产物、碰撞、世界状态和种子（§0）
- **要临时改画面相关的引擎参数（如开放室内的阴影第一级范围）**：作为介质覆盖自己记，不登记 `Owns`，也不进 `QualityView`（§3"介质覆盖"、§7）
- **门禁、截图、基准要指定画质**：用命令行 `--quality`、`--quality-set`、`--adaptive`、`--tier`，不读 `user://`；视觉基准是 `--quality=fine --adaptive=0`（§6、§10）

## 1 预设

数值是初值，P0 在开发机和一台最低配置的笔记本上实测校准（§12）。带 * 的项属于"风格默认"，玩家可以关（§2）。

| 项 | 流畅 | 均衡 | 精致 | 极致 |
| --- | --- | --- | --- | --- |
| 对应设备档 | 不对应（兜底） | 最低配置 | 独显中 | 独显高 |
| 帧率目标 | 60 | 60 | 60 | 跟随刷新率 |
| 内部像素上限 | 0.75 MP | 1.0 MP | 2.1 MP | 3.7 MP（4K 输出时 4.9 MP） |
| 渲染比例（1080p / 1440p / 4K 输出） | FSR1 0.59 / — / — | FSR1 0.67 / 0.5 / — | 原生 / FSR1 0.67 / 0.5 | 原生 / 原生 / FSR1 0.77；1080p 下可选超采样 1.25×（默认不开） |
| 抗锯齿 | FXAA | FXAA | SMAA 1x | SMAA 1x |
| 各向异性过滤 | 4× | 8× | 16× | 16× |
| 阴影图 / 级联 / 距离 | 2048 / 2 级 / 600 m | 2048 / 2 级 / 1000 m | 4096 / 4 级 / 1600 m | 8192 / 4 级 / 1600 m |
| 细小物件投影 | 关 | 开 | 开 | 开 |
| 柔光质量 | 标准 | 标准 | 高 | 高 |
| 移轴 * | 1/4 分辨率 | 1/4 | 1/2 | 1/2 |
| 镜头光斑 * / 光柱 * | 开 / 开 | 开 / 开 | 开 / 开 | 开 / 开 |
| 暗角 *（ART-STYLE §7.8） | 开 | 开 | 开 | 开 |
| 细节距离系数 | 0.7 | 0.8 | 0.9 | 1.0 |
| 模型 LOD 精度 | 2 px | 1.5 px | 1 px | 1 px |
| 近景几何叶 | 关 | 关 | 开 | 开 |
| 植被与草密度 / 草地范围 | 75% / 12 m | 100% / 20 m | 100% / 20 m | 100% / 20 m |
| 西湖倒影刷新（分辨率固定 1/4） | 每 4 帧 | 每 3 帧 | 每 2 帧 | 每 2 帧 |
| 降水与粒子密度 | 50% | 75% | 100% | 100% |

- 四档共同：描线、分阶明暗与按材质的影色、柔光始终开；软影滤波固定一档、阴影深度固定 16 位（§3）。
- "—"表示该输出分辨率下内部像素已低于 FSR1 0.5 的下限，界面建议改用 1080p 窗口。预设按"内部像素上限"换算到最接近的 FSR1 比例，不写死比例。
- **超采样 1.25×**：默认不做，任何预设都不默认取它；作为极致的可选项，放进"渲染比例"的取值里，只在设备档为独显高时列出，选了它预设就显示为"自定义"：`Viewport.Scaling3DScale = 1.25`、`Scaling3DMode = Bilinear`（文档写明大于 1 只对 Bilinear 有效，属超采样、代价高）。只在 1080p 输出下列出：内部约 3.2 MP，仍在极致的 3.7 MP 上限以内；1440p 下是 5.8 MP，超出上限，不提供。属雾幕类（§3）。描线线宽照旧按"内部高度 / 1080"换算（§2），P0 在渲染比例 0.5 / 0.59 / 0.67 / 1.0 / 1.25 五档截图比较描线，并实测开销后再定是否保留（§12）。参考场景里的 1.6 倍超采样 G-buffer 在 Godot 里等于整场景再画一遍，不做（ART-STYLE）。
- 不做的：FSR2、TAA、MSAA（拖影或破坏描线用的材质 ID，ENG-PERF §10.2）；DLSS、XeSS（Godot 4.7 没有）；超过 1.25× 的超采样；HDR 输出（与 LUT + CDL 调色冲突，首版不做）；纹理质量（总量不超过 1 GB，降一半就碰到 ART-FIDELITY §7 的贴脸纹素底线，而且 Godot 没有运行时跳 mip 的设置）。

## 2 风格底线与风格默认

| 类别 | 内容 | 规则 |
| --- | --- | --- |
| **风格底线**（没有开关） | 描线；分阶明暗与按材质的影色（含透光元素的逆光透射）；柔光（高光晕开：白天只晕高光，黄昏以后才叠一层全帧柔光，ART-STYLE §7.1）；高光肩部（ART-STYLE §7.2）；平涂贴图；雾、天空、调色板与 LUT（LUT 里烘有分调，ART-STYLE §7.8）；窗灯、路灯光斑与亮窗室内；室内地面与墙上的窗光斑（WLD-INTERIORS）；季节内容（只降密度，不缺席）；一拍二；剖切与介质呈现 | 任何预设、任何手动值、任何自适应级别都不能关 |
| **风格底线**（有最差值） | 实时阴影：不低于 2048 阴影图、2 级级联；西湖倒影：不低于 1/4 分辨率、每 4 帧刷新 | 只能降到最差值 |
| **风格默认**（可关） | 移轴；镜头光斑；光柱（室外软边光柱与室内窗格光柱、细尘一起开关，WLD-INTERIORS）；暗角（ART-STYLE §7.8；分调烘在 LUT 里，随 LUT 属风格底线，不跟着关） | 四档默认都开；玩家可以单独关（也满足无障碍"动态效果要能关"的要求）；关掉之后的画面不进入验收 |
| **性能项** | §1 表里其余各项 | 按预设取值，玩家可调 |

风格底线按 ART-STYLE 的"清透干净"：影子以明度为主、按材质推导，不做接触影；白天只让高光晕开（黄昏以后才有一层轻柔的光扩散）；贴图以平涂为主，斑驳只留在近处。这些是画风，不随画质关掉。

**描线在低渲染比例下不能变糊**：Compositor 的描线通道在放大之前、按内部分辨率执行。线宽按"内部高度 / 1080"计算，内部不足 1 px 的线按覆盖率画，并设最小宽度，保证放大后上屏的线仍是清楚的 1 px 左右（ENG-PERF §10.2）。P0 在 0.5、0.59、0.67 三档比例下截图比较描线（§12）。

## 3 单项清单与生效方式

生效：**即** = 即时；**雾** = 点"应用"后在 0.3 s 雾幕里生效（重建渲染缓冲，4K 下约 11–25 ms，不编译管线）；**启** = 下次启动生效。

| 组 | 单项 | 取值 | 生效 | Godot 实现 | 负责 |
| --- | --- | --- | --- | --- | --- |
| 显示 | 显示模式 | 窗口 / 无边框全屏（默认）/ 独占全屏 | 雾 | `Window.Mode` | WP-32 |
| 显示 | 窗口尺寸 | 只在窗口模式下 | 雾 | `DisplayServer.WindowSetSize` | WP-32 |
| 显示 | 垂直同步 | 开 / 关 | 即 | `DisplayServer.WindowSetVsyncMode` | WP-32 |
| 显示 | 帧率上限 | 30 / 60 / 半刷新率 / 刷新率 / 不限 | 即 | `Engine.MaxFps` | WP-32 |
| 显示 | 可变刷新率 | 开 / 关 | 即 | 开时帧率上限取刷新率 − 3 | WP-32 |
| 清晰度 | 渲染比例 | 超采样 1.25×（只在独显高、1080p 输出下列出，§1）；原生；FSR1 0.77 / 0.67 / 0.59 / 0.5 | 雾 | `Viewport.Scaling3DMode`、`Scaling3DScale`；超采样时 `Scaling3DMode = Bilinear`、`Scaling3DScale = 1.25` | WP-32 |
| 清晰度 | FSR 锐度 | 0–2 | 雾 | `Viewport.FsrSharpness` | WP-32 |
| 清晰度 | 抗锯齿 | SMAA 1x / FXAA | 雾 | `Viewport.ScreenSpaceAA` | WP-05 |
| 清晰度 | 各向异性过滤 | 2× / 4× / 8× / 16× | 雾 | `Viewport.AnisotropicFilteringLevel`；着色器采样器要写 `filter_linear_mipmap_anisotropic` | WP-04 |
| 光影 | 阴影质量 | 2048·2 级 / 4096·4 级 / 8192·4 级 | 即 | `RenderingServer.DirectionalShadowAtlasSetSize`、`DirectionalShadowMode` | WP-03 |
| 光影 | 阴影距离 | 600 / 1000 / 1600 m | 即 | `DirectionalShadowMaxDistance`、split | WP-03 |
| 光影 | 细小物件投影 | 开 / 关 | 雾 | 投影物图层（ENG-PERF §4）。只在雾幕里切，避免小件影子在一帧内消失 | WP-03 |
| 后期 | 柔光质量 | 高 / 标准 | 即 | glow 级数、双三次放大 | WP-05 |
| 后期 | 移轴 | 1/2 / 1/4 / 关 | 即 | CompositorEffect 的分辨率变体与 `Enabled`；关时 0.5 s 淡出 | WP-05 |
| 后期 | 镜头光斑 | 开 / 关 | 即 | CompositorEffect `Enabled`；0.5 s 淡出 | WP-05 |
| 后期 | 光柱 | 开 / 关 | 即 | 面片节点 `Visible`（启动时就在树里，已预编译）；0.5 s 淡出 | WP-05 |
| 后期 | 暗角 | 开 / 关 | 即 | 暗角并在移轴合成那次 dispatch 里（ENG-PERF §10.2），关时 C# 把 push constant 里的暗角强度 0.5 s 内降到 0，不切 `Enabled`、不改管线；移轴关掉时合成 dispatch 照跑暗角与高光肩部 | WP-05 |
| 场景 | 细节距离 | 0.7 / 0.8 / 0.9 / 1.0 | 即（分帧） | 选级 τ 与 `InstanceGeometrySetVisibilityRange`，已挂载的格子分帧重写，每帧 ≤ 0.3 ms。外围 O1、O2 同样随之；取 0.7 时 O2 换成一圈卡片，0.5–1 s 抖动淡化（WLD-OUTSKIRTS §11.4） | WP-02（WP-47 只读） |
| 场景 | 模型 LOD 精度 | 2 / 1.5 / 1 px | 即 | `Viewport.MeshLodThreshold` | WP-02 |
| 植被 | 近景几何叶 | 开 / 关 | 即 | 近池重建（≤10 Hz）时生效，抖动淡化 | WP-23 |
| 植被 | 植被与草密度 | 75% / 100% | 即 | `MultiMesh.VisibleInstanceCount`；远池用全局参数调抖动阈值 | WP-23 |
| 植被 | 草地范围 | 12 / 20 m | 即 | 草的循环范围 | WP-23 |
| 水 | 西湖倒影刷新 | 每 4 / 3 / 2 帧 | 即 | `SubViewport.RenderTargetUpdateMode` | WP-24 |
| 粒子 | 降水与粒子密度 | 50% / 75% / 100% | 即 | 降水池、飘落池的 `VisibleInstanceCount`（萤火虫不降，它只占 1 次绘制） | WP-45b（WP-38 只读，按同一密度设飘落池） |
| 帧率 | 自动调节画质 | 开 / 关 | 即 | §5 | WP-32 |
| 高级 | 图形接口 | D3D12（默认）/ Vulkan | 启 | 写 `user://engine_override.cfg`，经项目设置 `application/config/project_settings_override` 读取；`OS.SetRestartOnExit` 后退出 | WP-32 |
| 高级 | 显卡选择 | 双显卡笔记本上选独显 / 核显；主线阶段不可选，显示"引擎默认" | 启 | Godot 只能用启动参数 `--gpu-index` 选显卡，没有对应的工程设置，所以不写进 `engine_override.cfg`；以后由 WP-01 的启动器在引擎启动前传参 | WP-32 |

**固定不做选项的引擎设置**（四档统一，保证切换不编译管线）：

- 软影滤波档位：改一次会清空**全部**已编译管线，所以四档固定为同一档。三渲二对阴影做了阈值化（ART-STYLE §2.5），滤波档位对画面影响很小。
- 阴影深度：固定 16 位。项目设置 `rendering/lights_and_shadows/positional_shadow/atlas_16_bits` 设为 false，让引擎预编译两种格式（本项目没有点光阴影）。
- MSAA、SSAO、SSR、体积雾：不用（ENG-PERF §10.2）。
- 遮挡剔除：没有画面差，由程序按视角自动开关（ENG-PERF §4）。

**介质覆盖**（不是玩家单项，WLD-INTERIORS）：镜头在开放室内时，介质服务（WP-07）把方向光阴影的第一级范围缩到 8–12 m，0.5 s 内过渡，出楼恢复；它叠在"阴影质量""阴影距离"两项之上，属即时类，不编译管线。自动调节和玩家的设置都不动它；`QualityView` 报告的是玩家与自适应的取值，覆盖量由介质服务自己记。进入室内时把阴影方向的最低高度抬到约 14°（P0 原型核实项，34、§12）也属于介质呈现，同样不是画面单项。

**启动预热**：扉页后面的预热（SYS-TITLE §1.1；`--title=0` 时在开场雾幕里）覆盖"四个预设和所有单项取值能到达的组合"的并集（SMAA 和 FXAA 两套后期、移轴两种分辨率、超采样的 Bilinear 缩放等），所以玩家怎么切都不再编译管线（门禁 G16，基线从标题页转为实时那一刻算起）。

## 4 应用流程

1. 界面只调用 `IQualityService.Plan()` 和 `Apply()`（§8）。
2. **即时类**：拖动就预览，0.2 s 去抖。
3. **雾幕类**：标成"待应用"，点"应用"或收起设置卡时一次提交：
   1. 雾幕淡入 0.3 s；
   2. 按负责方依次执行：WP-32（显示与渲染比例）→ WP-03（阴影）→ WP-05（后期）→ WP-04（各向异性）→ WP-02（选级）；
   3. 描线的 CompositorEffect 检查自己的纹理（缓冲重建后会被释放），缺了就重建；
   4. 等流式各环就绪（最多 1.2 s，同跳转的到达保证）；
   5. 雾幕淡出 0.3 s；G16 的计数基线在这一刻重置。
4. **重启类**：写盘并提示"重启后生效"，提供"立即重启"。
5. **排队**：跳转、地图传送（SYS-ATLAS）、模式切换、换季转场、回位、切图（SYS-MAPS）、读档的遮挡期间（SYS-SAVE）、进场过场与返回标题（SYS-TITLE）进行中时，返回 `Deferred`，等它们结束再执行。切图途中提交的雾幕类改动并进切图的雾幕一起生效，即时类照常（SYS-MAPS §9）。
6. **15 秒确认**：改显示模式、窗口尺寸、渲染比例后，弹"保留 / 还原"倒计时，15 s 不确认就自动还原。
7. 设置生效的那一帧及其后 2 s 不计入自适应的统计。

## 5 自动推荐与自动调节

**首次分档**（扉页后面，≤2.5 s，SYS-TITLE §1.1 第 5 步；`--title=0` 时在开场雾幕内）：

冷启动先显示扉页静帧，预热和首次分档都在扉页后面做完（SYS-TITLE §1）。测速的"起始视角"是 V1（即 P00）；测完镜头移到开始页的 T0，V1 的各环留着给新游戏过场的终点复用。首次启动时开始页的菜单下方显示一行"已按本机推荐：极致 · RTX 4080 · 设置 › 画面"（SYS-TITLE §2.4），数据取 `Recommended` 和显卡名；只在首次启动或重新检测之后显示。


1. 读显卡名、厂商、驱动（`RenderingServer.GetVideoAdapterName/Vendor`、`OS.GetVideoAdapterDriverInfo`）。
2. 用 `RenderingServer.GetRenderingDevice().GetDriverResource(...)` 拿到 Godot 正在用的 DXGI 适配器，C# 手写 COM 调用 `IDXGIAdapter3.QueryVideoMemoryInfo` 读显存预算，`ID3D12Device.CheckFeatureSupport(ARCHITECTURE)` 读 UMA 标志，不引入依赖。
   - **不能用** `GetVideoAdapterType()` 判核显：D3D12 下核显也会报几十到几百 MB 的"专用显存"，被误判为独显。
   - WMI 的显存数封顶约 4 GB，不能用。
3. UMA 或专用显存小于 1 GB 判为核显，一律归最低配置（酷睿 Ultra 第一代的 Arc 核显、Radeon 780M 同级及以上；更旧更弱的核显不做适配，D-169）；独显按显存预算和名称判高或中；判不出来的进入测速。
4. 测速（限时采样，细则以 ENG-PERF §11.2 为准）：起始视角 V1 下按时间而不是按帧数采样，渲染比例 1.0 和 0.67 各采一段，整步不超过 2.5 s，到时就用已采到的样本；拟合"GPU 时间 = a × 像素数 + b"，取 p90 与该档预算的 80%（核显 75%）比较选档。样本不够或拟合不可信时采用保守结果（核显取最低配置，独显取独显中），设置页可"重新检测"。
5. 结果写进 `user://graphics.cfg` 的 `[device]` 段；推荐预设设为当前预设。

**之后**：玩家手动选过预设，下次启动不覆盖。只有显卡名、驱动版本或输出分辨率变了才重新检测，推荐变了用一条临时提示告知（UI-NAV §4.9），不自动改玩家的选择。

**自动调节画质**（默认开）：

- 实际生效值 = clamp(min(玩家所选, 自适应上限), 最差值)。只往下调，回升不超过玩家所选。
- 只动"即时"类的性能项：细节距离、模型 LOD 精度、植被与草密度、降水与粒子密度、倒影刷新、柔光质量、阴影距离与级联（4→2 级、缩距离都实测即时、无编译）。不动渲染比例、显示设置和风格底线。
- 玩家手动改过的项被锁定，自动调节跳过它。
- 每一级降档都在 0.5–1 s 内淡化完成，不得硬切（G13）。
- 界面上用一行小字写明当前自动降了哪些项，例如"已自动降低：植被密度、阴影距离"。
- 关闭时只保留帧节奏控制。
- 控制律、样本剔除、发热漂移沿用 ENG-PERF §11.3。
- 开始页、进场过场、返回标题（`IAppFlow` 的 Title、Entering、Leaving，SYS-TITLE §7.2）期间自适应冻结，不升也不降，这些帧全部剔除出统计；传送的雾幕帧、存档抓缩略图的那一帧同样剔除（SYS-ATLAS §7.4、SYS-SAVE §4.5）。

**一拍二的步频**：`step_rate` 随帧率上限变化：上限 30（含 60 Hz 屏的半刷新率）时取 15，每个姿态正好 2 帧；其余取 12（SYS-MOTION §2.2、ENG-PERF §11.4）。截图和门禁一律 12。

**画面测试**（设置页的"试跑"按钮）：

- 60 s 固定路线（ENG-GATES §1 的 F1），默认状态，雾幕进出，界面隐藏，按真实时间运行；
- 只在游戏里（Playing 状态）可用；从开始页打开设置卡时"试跑"禁用，读出原因"进入后可试跑"（SYS-TITLE §2.2）；
- 先等流式屏障就绪，并断言窗口有焦点（失焦时驱动会限帧）；
- 结果写在画面页注释栏的结果卡上（UI-VISUAL §8.5）：平均帧率、1% 低帧、超过 50 ms 的卡顿次数、GPU p95、能否达到当前预设的帧率目标、建议哪个预设；
- 同一套代码供 `--bench` 和 G10 使用。Movie Maker 是固定步长、非实时，只用来录像和截图，不能测帧率。

**显存估算**（P2 交付）：

| 部分 | 公式 |
| --- | --- |
| 3D 渲染缓冲 | ≈ 44 B × 内部像素；描线另加 normal_roughness 4 B × 内部像素和自建中间纹理 |
| SMAA / FSR1 | ≈ 17 / 14 B × 输出像素 |
| 阴影图 | 边长² × 2 B（16 位）：8192 为 128 MB，2048 为 8 MB |
| 倒影 | 44 B × 1/16 屏幕像素 |
| 固定开销 | ≈ 70 MB |
| 纹理与几何 | G8 的烘焙统计 × 常驻集 |

- 设置页显示：当前 DXGI 用量 + 待改设置按公式算出的差值，对比 DXGI 给本进程的显存预算；超过 80% 标黄，超过 100% 标红，并点名最占显存的几项。误差：渲染目标约 ±5%，总量 ±10–15%。

## 6 持久化与命令行

- **文件**：`user://graphics.cfg`（ConfigFile 格式）。`[meta]` 存版本、显卡名、厂商、驱动；`[device]` 存分档缓存；`[profile]` 存预设、手动覆盖项（只存与预设不同的项，写成 `knob_snake_case = float`）、自动调节开关。版本迁移用逐版本的迁移函数链。
  - **读值只接受基本类型**（SYS-SAVE §8）：ConfigFile 的解析器支持 `Object(类名, …)` 写法，在 `Load` 或 `Parse` 返回之前就已经按文件内容创建了对象，所以读出来再查类型挡不住。先把文件读成文本，含 `Object(`、`Resource(`、`SubResource(`、`ExtResource(` 任一写法就整份不用，退回 `graphics.cfg.bak`，`.bak` 也不行就用默认值并写日志；再 `ConfigFile.Parse(text)`，每个键按登记表只接受 bool、int、float、String，类型不对或越界就用默认值，未知的键忽略。
  - **写盘**：`EncodeToText()` → 写 `graphics.cfg.tmp` → `FileStream.Flush(true)` → `File.Replace(tmp, graphics.cfg, graphics.cfg.bak)`；第一次写、目标还不存在时用 `File.Move`；目标被占用时重试 3 次、间隔 50 ms，仍失败就放弃这次写盘，内存里的设置照常生效。不用 `ConfigFile.Save`（只 `fflush`，不保证落盘），也不用 `DirAccess.Rename`（在 Windows 上先删后移，不是原子操作，SYS-SAVE §4.5）。
  - `graphics.cfg` 是档案级文件，不进存档，换存档位不变。
  - `user://engine_override.cfg` 同样这么写；引擎在任何 C# 代码运行之前就读它，没法先查文本，所以 WP-32 只写登记过的几个键（SYS-SAVE §8）。
  - 声音的设置文件 `user://audio.cfg` 归 WP-53a，与本文件同一做法：只经 `IAudioService` 读写，读值只接受基本类型（SYS-AUDIO §13）。画面设置不读它，它也不读 `graphics.cfg`。
- **命令行**（ENG-GATES §1）：
  - `--quality=smooth|balanced|fine|ultra`：选预设，只在内存里生效、不写盘；
  - `--quality-set=<单项>=<值>,…`：覆盖单项，名字是 `QualityKnob` 的 snake_case；名字未知或劣于最差值时以非零退出码结束；
  - `--adaptive=0|1`：在 `--gate`、`--shots`、`--bench`、`--frames` 模式下默认 0；`--save` 在同样这几种模式下默认 `off`（SYS-SAVE §12）；
  - `--tier=min-spec|dgpu-mid|dgpu-high`（最低配置、独显中、独显高）：只设设备档、跳过检测；只给 `--tier` 时预设取该档的推荐预设；
  - 优先级：命令行 > `graphics.cfg` > 自动推荐 > 默认值。门禁和基准只由命令行决定，不读 `user://`。
- **截图默认串**追加 `--quality=fine --adaptive=0`；默认串末尾另有 `--map=qingshuihe --title=0 --save=off --audio=off`（ENG-GATES §1、SYS-AUDIO §17；截图和基准一律不出声，只有声音门禁显式给 `--audio=on`）。基准默认 `--quality=fine`，开发机另跑 `--quality=ultra`。
- **恢复画面设置**（游戏外入口，WP-32 随包提供）：引擎在任何 C# 代码运行之前就读 `engine_override.cfg`，图形接口写坏时游戏可能起不来，游戏里的画面页和启动失败页（SYS-TITLE §1.1）都打不开。所以安装目录根下随包放一个 `reset-graphics.cmd`（界面和说明里叫"恢复画面设置"），不依赖 Godot 和 .NET：
  - 把正式版 user 目录 `%APPDATA%\UESTC\`（ENG-ARCH §2）下的 `engine_override.cfg` 和 `graphics.cfg` 改名为 `<原名>.reset-<日期时间>`，不删除，留给诊断；
  - 只动这两个文件：存档、`settings.cfg`、`input.cfg`、`audio.cfg`、日志和本机烘焙缓存都不碰；
  - 下次启动按默认值走：D3D12、引擎默认的显卡，重新做首次分档并采用推荐预设（§5）；
  - 两个文件都不存在时什么也不改，提示"画面设置已是默认值"；脚本只用 ASCII 命令，结果用中英两行写在窗口里，按任意键关闭；
  - OPS-RELEASE §6 的故障处理引用这一入口；发版前在导出版上验证一次：写一份坏的 `engine_override.cfg`，运行脚本后游戏能正常启动。

## 7 各包的分工

| 包 | 交付 |
| --- | --- |
| WP-32a | `IQualityService`、`QualityCatalog`、预设表 `game/data/quality-presets.json`、应用状态机、持久化、命令行、与自适应的合并 |
| WP-32b | 首次分档（DXGI、UMA、测速；扉页后面做，SYS-TITLE §1.1）、自动调节（Title、Entering、Leaving 下冻结）、帧节奏、`step_rate`、画面测试、显存估算 |
| WP-35 | 设置卡的"画面"页（§9，视觉按 UI-VISUAL §8.5） |
| WP-01 | 解析新参数；预热覆盖所有可达设置的并集；雾幕服务开放"请求雾幕"钩子 |
| WP-02 | 细节距离、模型 LOD 精度；分帧重写 visibility range；按预设的显存上限定 LRU 容量 |
| WP-03 | 阴影质量、阴影距离、细小物件投影 |
| WP-04 | 各向异性（着色器采样器提示） |
| WP-05 | 抗锯齿、柔光质量、移轴、镜头光斑、光柱、暗角；描线线宽按内部分辨率换算 |
| WP-23 | 近景几何叶、植被与草密度、草地范围 |
| WP-24 | 倒影刷新 |
| WP-45 | 粒子与降水密度（单项的唯一负责方；WP-38 只读，按同一密度设飘落池） |
| WP-47 | 外围环境带：在 `[QualityResponsive]` 里登记"细节距离"为只读（负责方仍是 WP-02），O1、O2 随之选级，流畅档的 O2 换卡片环（WLD-OUTSKIRTS §11.4） |
| WP-07 | 介质覆盖：开放室内的阴影第一级范围（§3），不是单项，不登记 `Owns` |
| WP-27、WP-41 | 门禁按预设参数化、G17、"预设轮换"脚本 |

区域包、部件包和行走相关的包没有新交付：碰撞、行走半径与画面设置无关。室内包（WP-17、WP-31a–c、WP-51、WP-52）也不新增单项：WLD-INTERIORS 提出的可选单项"室内痕迹密度"（只降次痕迹和杂物，主痕迹不降）暂不做，P0 实测室内预算后如需要，再在 `QualityKnob` 末尾追加，负责方 WP-52（§12）。

## 8 契约

```csharp
// game/contracts/Quality/（纯 .NET，只增不改；Campus.Gen.* 不得引用）
namespace Campus.Contracts.Quality;

public enum QualityPreset : byte { Smooth, Balanced, Fine, Ultra, Custom }       // 流畅、均衡、精致、极致、自定义
public enum DeviceTier : byte { MinSpec, DgpuMid, DgpuHigh }                     // 最低配置、独显中、独显高（ENG-BUDGET §2、D-169）
public enum ApplyClass : byte { Immediate, Amortized, UnderVeil, Restart }
public enum KnobKind : byte { Floor, StyleDefault, Performance }                 // 底线 / 风格默认 / 性能

public enum QualityKnob : byte
{
    DisplayMode, WindowSize, VSync, FrameCap, Vrr,
    RenderScale, FsrSharpness, AntiAliasing, Anisotropy,
    ShadowQuality, ShadowDistance, SmallCasters,
    GlowQuality, TiltShift, LensFlare, LightShafts,
    DetailScale, MeshLodThreshold,
    GeometricLeaves, FoliageDensity, GrassRadius,
    ReflectionInterval, ParticleDensity,
    Adaptive, GraphicsApi, GpuIndex,
    Vignette,                                    // 暗角（风格默认，§2），追加在末尾
}

public sealed record KnobSpec(QualityKnob Knob, string OwnerWp, ApplyClass Apply, KnobKind Kind,
    float Min, float Max, float Step, float WorstAllowed, bool HigherIsBetter,
    bool InAdaptiveLadder, string LabelKey);                                     // 界面按它自动生成控件

public sealed record QualityProfile(QualityPreset Preset,
    IReadOnlyDictionary<QualityKnob, float> Overrides, bool Adaptive, int Schema = 1);

public interface IQualityService                    // Kernel 服务（WP-32）；界面与门禁的唯一入口
{
    DeviceTier Tier { get; }
    QualityPreset Recommended { get; }
    QualityProfile Requested { get; }
    QualityView Effective { get; }                  // 实际生效值，读取无分配
    IReadOnlyList<KnobSpec> Knobs { get; }          // 单项目录（QualityCatalog）：画面页按它生成控件
    float PresetValue(QualityPreset p, QualityKnob k);   // 预设表 game/data/quality-presets.json 的取值
    event Action<QualityChange>? Changed;           // 主线程，值变才发
    ApplyPlan Plan(QualityProfile next);            // 界面据此显示"雾幕""重启"生效标签与显存估计
    Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin);
    void ConfirmDisplay();                          // 15 s 确认
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class QualityResponsiveAttribute : Attribute   // 标在 Godot 侧包的 [KernelNode] 上
{
    public QualityKnob[] Owns { get; init; } = [];   // 我负责落到引擎上；G5 核对每项恰有一个负责方
    public QualityKnob[] Reads { get; init; } = [];  // 只读
}
// FramePriority 增：Quality = −1200（先于相机，同一帧里所有系统看到同一版本，ENG-ARCH §3）
```

`QualityView`、`QualityChange`、`ApplyPlan`、`ApplyResult`、`ApplyOrigin` 的完整定义写进 ENG-ARCH §3。任何包都不读 `graphics.cfg`，只通过 `IQualityService` 和 `IQualityAware.ApplyQuality()` 接收设置。着色器需要的值（细节系数、植被密度、草半径）合成一个全局参数 `quality`（vec4），写入方 WP-32，登记进 ENG-PERF §1.5。

## 9 界面（WP-35，视觉见 UI-VISUAL §8.5）

- 设置卡的"画面"页分组列出 §3 的单项，"显示"一组也在这一页，不另设"显示"页。"界面缩放""大页磨砂""降低透明度""界面主题"不是画面单项，留在"设置"页（UI-NAV §4.6）；声音在"声音"页（SYS-AUDIO §13）。
- 页首是一组分段选择做预设：流畅、均衡、精致、极致、自定义；当前项为选中态（UI-VISUAL §7.3）。
- 下面一行："推荐：精致（RTX 3050，起始视角 GPU 9.8 ms）· 重新检测 · 试跑"，再下面一条细显存条（P2），画成 LED 格，80% 处一根警戒刻线。
- 每行是"名称｜控件｜生效标签"：没有标签 = 即时，"雾幕" = 雾幕下生效，"重启" = 需重启。标签是等宽字加发丝边，雾幕标签用 `primary.line` 边（UI-VISUAL §8.5）。
- 悬停或聚焦某一行时，底部注释栏显示两行说明、负载标签（GPU、显存、CPU 的大中小，画成三格 LED），注释栏用一根走线连到当前行（UI-VISUAL §5.5）；雾幕类配两张 192×108 的对比小图，由截图机在集中测试时按固定视角出（阴影用银杏大道、倒影用西湖、渲染比例和描线用图书馆近景、移轴用总览）。
- 即时类直接看身后实景：画面页只占左侧约 1/3，右侧场景实时变化，不压暗。
- **面板材质**：设置卡是大页，随昼夜主题切换（UI-NAV §2）；轻度磨砂随设备档：独显档默认开、最低配置档默认关，"降低透明度"打开后一律 100% 不透明（UI-VISUAL §5.7）。磨砂的 GPU 开销在设置卡打开期间另计，不进自适应统计（UI-VISUAL §12）。
- **从开始页打开**（SYS-TITLE §2.2）：右侧 2/3 是实时的 T0；雾幕类改动照常在雾幕里生效；"试跑"禁用，读出原因"进入后可试跑"（§5）。
- 页脚注："描线、分阶明暗与按材质的影色、柔光、平涂贴图、实时阴影与西湖倒影是本作画风，任何画质下都保留。"
- 有雾幕类改动待生效时"应用"按钮才亮，并亮一盏呼吸灯（UI-VISUAL §9.3 的 M8）。
- **按需创建**：画面页随设置卡打开时才建、关闭即释放，不预建隐藏（UI-VISUAL §11.9）：隐藏的页会占 3D 流式实例共用的实例参数池。全页约 120 个控件；口径是可见且在处理的 Control ≤ 400、总数 ≤ 700（G14）。画面页里还没应用的改动跨开关保留。

## 10 验收

- **视觉精度的验收基准**：精致预设、1920×1080 原生、自动调节关（`--quality=fine --adaptive=0`）。G7、G9 按它截图。
- **低预设的口径**分两层：
  - 与分辨率无关的结构性条款，任何预设都必须过：ART-FIDELITY §7 的黑洞、灰影、深度闪烁、季节残留、室内外亮灯一致、夜间 L* ≥ 12、影色自检通过（SYS-TIME §8），以及身份特征、底线清单（ART-FIDELITY §2.0）、精度区 K1 和 K2 的 L0+ 建筑细节；
  - 与像素相关的条款以精致为基准；低预设下距离阈值乘该预设与精致的细节系数之比（均衡 ≈ 0.89，流畅 ≈ 0.78；极致不放宽），像素阈值按渲染比例换算。
- **门禁 G17 预设底线**（ENG-GATES §2）：
  - 每个预设加"最差值配置"，在 V1、V4、E1、E3 和 V7 夜间截图，自动判：描线像素占比不低于精致的 70%（用 `--inspect=matid`，阈值见 §12）、投影存在、倒影存在、影色自检通过（SYS-TIME §8）、夜间 L* ≥ 12、无黑洞和穿帮、裸面检测（距离乘细节系数之比）；
  - "预设轮换"脚本：四个预设和若干自定义组合依次切换，不崩溃，显存回到基线 ±5%，每次雾幕淡出后管线编译计数不增长（G16）；
  - 何时跑：集中测试时全量跑（ENG-GATES §2）。
- **其他门禁**：G8 静态预算按四个预设各算一遍，并检查单调性（极致 ≥ 精致 ≥ 均衡 ≥ 流畅）；G10 在集中测试时跑各设备档的推荐预设（均衡、精致、极致），另加流畅，只判回归；G11 在集中测试时用一台最低配置的笔记本验证首次推荐是否正确，手边没有就跳过并在回报里说明；G12 把"预设"作为 L16 正交表的一个因子，不增加截图张数；英雄帧在集中测试时加一遍流畅预设，不低于 3 分。

## 12 待验证（P0）

| 待验证 | 验证方法 / 负责包 | 验证前的初值 |
| --- | --- | --- |
| §1 各预设的数值 | P0 在开发机和一台最低配置的笔记本上实测校准，WP-32 | §1 表中的值 |
| 超采样 1.25× 是否保留（§1） | P0 在渲染比例 0.5 / 0.59 / 0.67 / 1.0 / 1.25 五档截图比较描线，并实测开销，WP-32、WP-05 | 作为极致的可选项保留，默认不开 |
| 低渲染比例下描线是否仍清楚（§2） | P0 在 0.5、0.59、0.67 三档比例下截图比较，WP-05 | 线宽按"内部高度 / 1080"换算；内部不足 1 px 的线按覆盖率画，并设最小宽度 |
| 进入开放室内时阴影方向的最低高度（§3"介质覆盖"） | P0 原型核实，WP-03、WP-07（WLD-INTERIORS） | 约 14° |
| 可选单项"室内痕迹密度"做不做（§7） | P0 实测室内预算后定，WP-52 | 不做 |
| G17 的描线像素占比阈值（§10） | P0 标定，WP-27 | 不低于精致的 70% |
