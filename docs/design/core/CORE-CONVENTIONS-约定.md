---
id: CORE-CONVENTIONS
title: 约定
category: core
summary: 跨文档共同约定速查：坐标单位、时间、编号命名、帧顺序、目录归属、确定性
keywords: [约定, 坐标系, 单位, 角度, 编号体系, 命名, 程序集, 命名空间, 色名, 帧顺序, FramePriority, 目录归属, ownership.json, user://, 确定性]
owners: [WP-00]
contributors: [WP-27]
depends: [CORE-INDEX]
related: [ENG-GATES, ENG-ARCH, ENG-BUDGET, ENG-WP, ENG-PERF, SYS-TIME, SYS-SAVE, SYS-MAPS, CORE-DECISIONS, CORE-GLOSSARY, ENG-ROADMAP, WF-STATUS, OPS-RELEASE]
contracts: []
data: []
gates: [G1, G2, G3, G4, G5]
---

# CORE-CONVENTIONS · 约定

> **范围**：本文汇总所有子代理共同遵守的跨文档约定：坐标与单位、时间单位、标识与命名、帧顺序、目录与归属、确定性，查一处就够。有权威表的地方只给速查和指针。
> **不在本文**：契约全文（ENG-ARCH §3）、全局参数登记表（ENG-PERF §1.5）、命令行参数表（ENG-GATES §1）、门禁（ENG-GATES §2）、预算（ENG-BUDGET）、工作包（ENG-WP）、决策（CORE-DECISIONS）、术语解释（CORE-GLOSSARY）、RPG 扩展点（ENG-ROADMAP）；文档写法（WF-DOC）、施工规则（WF-RULE、WF-BUILD）、开工手册（WF-ONBOARD）。
> **查阅**：先读 §0；坐标与单位看 §1；时间单位看 §2；编号与命名看 §3；帧顺序看 §4；目录与 `user://` 看 §5；确定性看 §6；与权威位置的差异看 §7
> **关联**：[CORE-INDEX 总览](../README.md)、[ENG-GATES 验收](../engineering/ENG-GATES-验收与门禁.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH-工程架构.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET-性能预算与硬规则.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF-性能工程.md)、[SYS-TIME 时间季节](../systems/SYS-TIME-时间与季节.md)、[SYS-SAVE 存档](../systems/SYS-SAVE-存档与持久化.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS-多地图.md)、[CORE-DECISIONS 决策登记](CORE-DECISIONS-决策登记.md)、[CORE-GLOSSARY 术语表](CORE-GLOSSARY-术语表.md)、[ENG-ROADMAP 扩展路线](../engineering/ENG-ROADMAP-扩展路线.md)、[OPS-RELEASE 发布与交付](../ops/OPS-RELEASE-发布与交付.md)

## 0 原则与权威表

- 本文只做速查。数值、签名、表格以权威位置为准；本文与权威位置不一致时，以权威位置为准，差异记进 §7。
- 权威位置的分工见 CORE-DECISIONS 的 D-133。本文引用的速查与权威位置对应如下：

| 内容 | 权威位置 | 本文 |
| --- | --- | --- |
| 契约（接口、record、枚举、特性） | ENG-ARCH §3 | §3 只列命名规则 |
| 帧顺序 `FramePriority` | ENG-ARCH §3 | §4 抄一份速查表 |
| 全局着色器参数与数据纹理 | ENG-PERF §1.5 | §1.3 只列 `map_rect` |
| 着色器白名单 | ENG-PERF §1.4 | §3.6 |
| 命令行参数 | ENG-GATES §1 | §6 只抄截图默认串 |
| 门禁 G1–G21 与测试节奏 | ENG-GATES §2 | 不摘录 |
| 预算与额度 | 20（总账 ENG-BUDGET §8） | 不摘录 |
| 工作包、独占目录、阶段 | ENG-WP §1、ENG-WP §2、ENG-WP §3 | 不摘录 |
| 目录与归属 | ENG-ARCH §2、`ownership.json` | §5 |
| 按图数据 | SYS-MAPS §2 | §5.2 |
| 已定决策 | CORE-DECISIONS | WF-DOC §6、§8 |
| 术语 | CORE-GLOSSARY | — |
| RPG 扩展点 | ENG-ROADMAP | §3.8 |
| 子代理开工与查阅 | WF-ONBOARD | 不摘录 |

### 0.1 接入要点

- **用到坐标、角度和单位**：x 向东、y 向上、z 向南，单位米，方位角从正北顺时针（§1）。
- **用到时间单位**：钟点、流逝倍率和动画时间的写法按 §2。
- **起名**：ID、程序集、命名空间、文件、色名和着色器文件按 §3；会被持久化的 ID 按 §3.9 保持稳定。
- **节点要在某一帧阶段执行**：按 §4 的帧顺序选 `FramePriority`。
- **放文件**：仓库目录按 §5.1，按图的数据按 §5.2，`user://` 下的文件按 §5.3。
- **生成或随机的结果要能复现**：按 §6 的确定性规则。
- **写或改文档**：按 WF-DOC；**施工**：入口见 WF-ENTRY，内容准则见 CORE-CONTENT。

## 1 坐标与单位

### 1.1 坐标系

- 单位米；x 向东、y 向上、z 向南。与 Godot 一样是右手系、y 向上，数据坐标直接当 Godot 世界坐标用，不做换算；正北就是 Godot 的前方（−z），偏航 0° 的相机朝北（CORE-INDEX §4，D-144）。
- **每张图一个原点和投影**，写在该图 `campus.json` 的 `meta` 里，`MapDescriptor.Geo` 从这里读，`map.json` 不重复（SYS-MAPS §3）。清水河：经度 103.9282919、纬度 30.75316145，EPSG:32648。
- 地图之间的坐标互不相通；跨图引用一律写成"地图 + ID"（`WaypointRef(MapId, Id)`，ENG-ARCH §3）。单图半径（离中心最远的可达点）不超过 8 km（SYS-MAPS §12、ENG-BUDGET §5）。
- **两套数学类型**：契约程序集只用 `System.Numerics`（`Vector2`、`Vector3`、`Quaternion`、`Matrix4x4`；`Matrix4x4` 是行向量约定），Godot 用 `Transform3D`（列向量约定）。只用内核提供的扩展方法转换；同一文件里两套都用时写 `using N = System.Numerics;`（ENG-ARCH §7）。

### 1.2 角度

- **方位角**一律用罗盘方位：从正北开始顺时针，单位度。方位 A 的水平单位向量是 (x, z) = (sin A, −cos A)（CORE-INDEX §4）。
- **Godot 节点**：罗盘方位 A 对应 `Rotation.Y = −A`（弧度）。偏航、日月方位、点位朝向、入口朝向一律按这条换算。
- **俯仰**：度，负值表示朝下看（UI-NAV §6）。
- 契约里以 `Deg` 结尾的字段都是度（`YawDeg`、`PitchDeg`、`FovDeg`、`AltDeg`、`AzDeg`、`ClosedYawDeg`、`OpenDeg`）；只有 Godot 节点的属性用弧度。主光角速度 `Omega` 是 °/s（ENG-ARCH §3）。
- **"目标点、方位、仰角、距离"的写法**（V 组审查视角、T0，由 `PoseMath.PoseFacing()` 换算，ENG-GATES §1）：方位是从目标点看向相机的罗盘方位，相机朝向目标点时偏航 = 方位 − 180°。按 V1 与 P00、V9 与 P15 的数值核对一致；T0 另给了偏航 12°，不正对目标点（SYS-TITLE §2.1）。

### 1.3 范围数（清水河）

| 名称 | 值 | 用途 | 出处 |
| --- | --- | --- | --- |
| 校界外接框 | x −991…989、z −1020…1021 | 行走边界；`MapDescriptor.Geo` 的范围 | WLD-DIORAMA §1.1、SYS-MAPS §3 |
| 校界形心（`MapDescriptor.Center`） | 约 (50, 77) | 活动范围"离中心不超过 3 km"的中心 | SYS-MAPS §3、WLD-OUTSKIRTS §3.5 |
| 箱庭包围盒（`MapDescriptor.Bounds`） | 截面线的包围盒，约 x −1250…1260、z −1400…1440 | 四叉树覆盖、坐标精度检查、罩住箱庭内容的 `CustomAabb` | SYS-MAPS §3、WLD-OUTSKIRTS §3.5 |
| `map_rect`（即 `map.json` 的 `dataRect`） | x、z 都是 −1280…1280；全局参数值 (−1280, −1280, 1/2560, 1/2560)，uv = (p.xz − xy) × zw | 全图数据纹理（`ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid`）、声学网格、声景场 | ENG-PERF §1.5、SYS-MAPS §10 |
| `atlasRect` | x、z 都是 −1536…1536，写在本图 `atlas/markers.json` | 平面地图的页面范围；±1280 m 以内全分辨率 SDF，外圈半分辨率 | SYS-ATLAS §2.3 |
| 旁观活动范围 | 水平 = 前沿线 + 0.9 × max(0, h − 30 m)，离中心 ≤ 3 km；竖直 −28…2000 m；缓冲带 30 m | 可达空间（参数在 `map.json` 的 `roam`） | WLD-OUTSKIRTS §8.1、SYS-MAPS §3 |
| 格 | 64 m 网格的四叉树，每张图一棵；格号 \|ix\|、\|iz\| < 512 | 烘焙与流式加载 | ENG-BUDGET §3、ENG-PERF §5 |

- `map_rect` 只管全图数据纹理，不等于箱庭包围盒，也不随 `atlasRect` 扩大。各张全图纹理的分辨率可以不同，一律按 `map_rect` 换算；范围外取"无数据"的默认值（`globals.gdshaderinc` 的 `map_uv()`、`in_map()`）。外围 O1、O2 和截面上缘不论是否落在 `map_rect` 内，都不读这几张纹理（ENG-PERF §1.5、WLD-OUTSKIRTS §4.5）。
- `map_rect` 的中心是 (0, 0)，与校界形心相差不到 0.1 km；`edge_fog` 的"朝外方向"直接按 `map_rect` 的中心求（WLD-OUTSKIRTS §8.3）。

### 1.4 其他单位

- 长度 m，速度 m/s；时间单位见 §2。
- **眼高**：行走 1.62 m；E 组审查视角 1.7 m（为了与旧截图对比，ENG-GATES §1）。
- **裁剪面**：近 0.05 m；远裁剪面按图从 `backdrop.json` 读，清水河 12 km（ENG-BUDGET §5）。
- **像素基准**：截图与界面都按 1920×1080；描线线宽按"内部高度 / 1080"换算（ENG-GATES §1、SYS-GRAPHICS §2、UI-VISUAL §8）。
- **颜色**：色彩计算在 OKLab / OKLCH 里做，明度写 L（OKLab）；十六进制色值只出现在调色板数据文件里（§3.5）。
- **音量**：dB；界面滑杆值 0–1（SYS-AUDIO §13）。

## 2 时间单位

| 量 | 单位与取值 | 谁推进 | 要点 | 出处 |
| --- | --- | --- | --- | --- |
| `WorldHours` | 小时，double | WP-34 | 同一世界实例内单调递增，只增不减；暂停时不动；跨实例比较没有意义 | SYS-TIME §1、SYS-SAVE §5.2 |
| `Clock`（全局参数 `clock`） | 小时，[0, 24) | 由 `WorldHours` 换算 | `Clock = (WorldHours + offset) mod 24`；offset 只在换季时重算，钟点数字可能变小，但太阳不倒走 | SYS-TIME §1 |
| 钟点的写法 | 正文写 HH:MM；命令行与 JSON 写小数小时 | — | 17:30 写 `17.5`；17:05 写 `17.083333`（`map.json` 的 `newGameStart`） | ENG-GATES §1、SYS-MAPS §3 |
| `SeasonPos`（全局参数 `season` 的 x） | [0, 4)，连续、循环；0 春、1 夏、2 秋、3 冬 | WP-34 | 季节不自动推进 | SYS-TIME §0 |
| `Flow.Rate` | 世界秒 / 真实秒，取 {0, 1, 10, 60, 360, 1440}；0 = 暂停，默认 1 | `IWorldControl.SetFlow`、`FlowTo` | 其他值抛 `ArgumentOutOfRangeException` | ENG-ARCH §3 |
| `AnimTime`（全局参数 `anim_time`） | 秒，[0, 480)；每帧 delta 累加后 mod 480 | WP-34 | 与世界时间无关，暂停、快进、倍速都不影响；`--anim` 可冻结；周期量取 480/k，保证回绕无缝 | SYS-MOTION §1.6、ENG-PERF §1.5 |
| `fade_clock` | 秒，mod 480 | WP-01 | 只给抖动淡化用；不受 `--anim` 冻结 | ENG-PERF §1.5 |
| `step_rate` | Hz：一般 12；帧率上限为 30 时 15；截图与门禁一律 12 | WP-32 | 着色器里 T12 = floor(anim_time·step_rate)/step_rate；界面里"被计算出来的"动效同拍 | SYS-MOTION §2.2、ENG-PERF §11.4、UI-VISUAL §9.2 |
| 真实秒 | 秒；帧间隔累加 | 各包自己累加 | 不读系统时钟；偶发事件、转场、一次性声的排程、界面动效都按它 | SYS-MOTION §1.1、SYS-AUDIO §7.1 |
| 天气时长 | 分钟（`WeatherQueueItem.Minutes`、`--weather-remaining`、`--weather-queue`）；`WeatherSpan` 按 `WorldHours` | WP-45a | — | ENG-ARCH §3、ENG-GATES §1 |
| 物理步 | 固定 120 Hz（`_PhysicsProcess`） | WP-42 | 观者的脚步等声音在物理步里当场发出 | SYS-WALK §12、SYS-AUDIO §20 |
| 系统时钟 | 只有 `Campus.Kernel.Save` 读一次，写进 meta 的 `savedAtUnix`，只用于显示 | WP-50 | 其余代码禁用系统时钟 | ENG-ARCH §2 |

- **只能向前**：没有任何让 `WorldHours` 减小的方法。`SetClock(h)` 跳到 h 的下一次出现，前进量 Δ = (h − 目标钟点) mod 24，0 ≤ Δ < 24；`Advance(hours)` 要求 hours > 0；`FlowTo(clock, rate)` 正向流逝到下一次出现的 clock 停住（ENG-ARCH §3、SYS-TIME §1）。
- **世界实例**：读档、新游戏是换一个世界实例（`IWorldState.Instance` 加 1），不算倒流；"回到落金初灯"和切图不换实例（SYS-SAVE §5.2）。
- **不计入世界时间**：关着游戏的那段现实时间、开始页停着的时间（SYS-SAVE §6、SYS-TITLE §6）。单帧 delta 上限 0.1 s；系统睡眠恢复后的第一帧 dt 夹到 100 ms（SYS-MOTION §1.6、SYS-TIME §1）。
- **引擎时间**：`Engine.TimeScale` 固定为 1，`AudioServer.PlaybackSpeedScale` 固定为 1；着色器不得用内置 `TIME`，一律用 `anim_time`（SYS-MOTION §1.6、ENG-ARCH §2）。
- **计时 API**（`Time.GetTicks*`、`Stopwatch`）只许出现在 `Campus.Kernel.Frame`、`Campus.Kernel.Quality`、`Campus.Kernel.Inspect`、`Campus.Kernel.Save` 和 `Campus.Gates` 里；界面和音频也不自己计时（ENG-ARCH §2）。

## 3 标识与命名

### 3.1 地图、实体与覆盖层

| 标识 | 格式 | 例 | 规则 | 出处 |
| --- | --- | --- | --- | --- |
| 地图 ID（`MapId`） | 小写字母、数字、连字符，≤ 32 字符；与目录名相同 | `MapId("qingshuihe")` | 代码里不写地图 ID 字面量（`kernel/map` 和门禁夹具除外）；不用 "qsh" 指地图 | SYS-MAPS §3、ENG-ARCH §2、D-137 |
| 实体 ID（`FeatureId`） | `campus.json` 的 `entities[].id`；清水河的前缀是 `qsh:`，形如 `qsh:<类型>:<编号>` | `qsh:way:544688571`、`qsh:relation:8407525` | 只在本图内唯一；`qsh:` 是数据，不是地图 ID；按 ID 定位对象，不按名字；通用代码里不写字面量；以后的图换用自己的前缀，G5 查前缀不冲突 | CORE-INDEX §4、SYS-MAPS §2.1 |
| 设计增补 | `design:` 开头；锚点实体写 `design:anchor:Axx` | `design:anchor:A02` | 只经覆盖层的 `add` 新增，写明理由 | ENG-ARCH §3、MAP-QSH-ADDITIONS §0、SYS-ATLAS §6 |
| 可行走区域 | `walk:` 开头 | — | 只在 `overlay/walkable.overlay.json` | SYS-WALK §6 |
| 地形操作 | `design:terrain:` 开头，本图内唯一 | `design:terrain:lakeside-lawn-tier1` | 只在 `overlay/terrain/<区>.terrain.json`；每条写理由 | WLD-TERRAIN §3.2 |
| 环境发声器 | `amb:` 开头，本图内唯一 | — | G5 查重 | ENG-ARCH §3（音频块） |
| RPG 预留前缀 | `npc:`、`item:`、`quest:` | — | 观光数据不得使用 | ENG-ROADMAP §6.1 第 6 条 |

- **覆盖层**：OSM 数据永不改写。风格化改动、设计增补和删除都写进本图的覆盖层：`game/data/maps/<地图>/overlay/<组>.d/`，通用覆盖层按包分片，每个包只写自己的 `<四位序号>-wp-<包号>-<短名>.json`，序号接在该目录现有最大值之后，不改别包的分片，`0000-wp-<包号>-base.json` 底稿尽量不改（加载规则见 ENG-ARCH §3 覆盖层的规则）；通用单文件入口已退役，地形、室内等专属格式不在此列。操作有 `reshape`、`height`、`style`、`hide`、`add`，`"op"` 写在每个对象的第一个字段；轮廓调整 4 m 以内算轻微（`Minor`），超过 4 m 或新增设计物体必须写理由；两个覆盖层改同一个要素时报错，除非其中一个已经认领了它（ENG-ARCH §3）。地形另有地形覆盖层 `overlay/terrain/<区>.terrain.json`，只写地形操作，一个文件只归一个包（WLD-TERRAIN §3.1）。

### 3.2 编号体系

| 前缀 | 是什么 | 清水河的范围 | 定义在 | 用法 |
| --- | --- | --- | --- | --- |
| P | 点位 | P00–P23，两位数；新增从 P24 往后编 | UI-NAV §6，`waypoints.json` | `--pose=P05`；P00 即 V1 |
| A | 锚点：导览亭 A01–A06、导览桩 A07–A13 | A01–A13 | SYS-ATLAS §6、UI-NAV §6 | 标记 ID `"A03"` |
| R | 推荐行走路线 | R1–R14 | SYS-WALK §11，`nav/routes.json` | 标记 ID `"route:R1"` |
| V | 审查视角：全景与中景 | V1–V11 | ENG-GATES §1，`review.json` | `--pose=V1` |
| E | 审查视角：人眼高度与低空 | E1–E7 | ENG-GATES §1 | `--pose=E1` |
| F | 飞行路线脚本 | F1–F4 | ENG-GATES §1 | — |
| W | 行走路线的录像脚本，W*n* 对应 R*n* | W1–W14 | ENG-GATES §1、SYS-WALK §11 | — |
| T | 开始页镜头 | T0 | SYS-TITLE §2.1 | 只在 `--title=1` 下有效 |
| Q | 各内容包的近景巡检点 | 每包 Q1–Q8 | ART-FIDELITY §8 | 截图名 `WP-18-Q3.png` |
| G | 门禁 | G1–G21；子项写 G19a–G19h、G21a–G21j | ENG-GATES §2 | — |
| INV | 全局不变量 | INV-1–INV-12 | CORE-INDEX §3 | "INV-6" |
| WP | 工作包 | WP-00–WP-53；子包写 WP-31a、45b；RPG 包从 WP-54 起 | ENG-WP §2、ENG-WP §3、ENG-ROADMAP §2 | `WpId("WP-18")` |
| D | 决策 | D-001 起，三位数，全文连续，只增不复用 | CORE-DECISIONS §0 | "36 的 D-133" |
| O0–O3 | 外围分层 | O0 街道带、O1 外围街区、O2 雾中楼顶、O3 远景 | WLD-OUTSKIRTS §4.2 | — |
| L0+–L4 | 建筑与地块的细节级别；另有室内 I0、IS、IF 三级，碰撞级 C | — | ENG-BUDGET §3 | `LodLevel` |
| 英雄帧 | 必须单独手调的 12 张 | 1–12 | SYS-TIME §3.5 | "英雄帧 7" |
| VF-A–VF-D | 登记的 4 种顶点格式 | — | ENG-PERF §2 | — |
| W-xxx | 窗卡类型，15 种 | `W-CLS`、`W-LAB` 等 | WLD-INTERIORS §5.1 | — |
| s、m | 存档位 `s1`–`s3`；手动槽 `m1`–`m9` 预留 | — | SYS-SAVE §1 | `SlotId("s1")` |
| M1–M8 | 界面动效的八个母题 | — | UI-VISUAL §9.3 | — |
| K1–K5 | 视觉精度区：主轴与地标、行走可达的街道与室内、普通立面与一般地面、屋顶背立面与只有飞行才到的位置、外围与远景 | — | ART-FIDELITY §0 | "精度区 K3" |
| B1–B13 | 视觉精度的底线条目 | — | ART-FIDELITY §2.0 | "底线 B4" |

### 3.3 同号不同义的读法

| 写法 | 可能指 | 读法与写法 |
| --- | --- | --- |
| G、INV | G1–G21 是 ENG-GATES 的门禁；INV-1–INV-12 是 CORE-INDEX 的全局不变量 | G 一律指门禁；不变量写"INV-6"（D-156） |
| E、O | E1–E7 是审查视角；O0–O3 是外围分层 | E 只指审查视角，写"视角 E1"或 `--pose=E1`；外围写"外围 O1"或"O1 外围街区"（D-157） |
| P0、P1… | 点位 P00–P23（两位数）、MAP-QSH-LIBRARY 图书馆的三条"看进去"路径 P1–P3、开发阶段 P0–P3 | 点位一律两位数；路径写"MAP-QSH-LIBRARY 的路径 P1"；阶段写"P0 阶段""P0 原型"（阶段的定义见 ENG-WP §1） |
| L0、L2… | 细节级别 L0+–L4，或 SYS-AUDIO声景七层 L0–L6 | 声景层写"声景 L2"或"L2 季节层" |
| I0、I1… | 室内兜底层 I0（IS、IF），或 ENG-WP §1 的室内推进批次 I1–I3 | 批次写"室内批次 I1"；全局不变量写 INV-n，不写 I# |
| 第 N 条 | ENG-PERF §5 的流式不变量 1–6；SYS-WALK §14 的第 5、6 条就是其中的第 5、6 条 | 写"ENG-PERF §5 第 5 条"或"SYS-WALK §14 第 5 条"，不写"不变量 5"；CORE-INDEX §3 的全局不变量写 INV-n |
| 第 N 条 | 编号列表里的一条（ENG-GATES §3 一票否决、ART-FIDELITY §7、ENG-BUDGET §7 硬规则等） | 写"ENG-BUDGET §7 第 9 条" |
| B1、B2… | 视觉精度的底线条目 B1–B13，或主楼的分区 B1–B3 区 | 底线写"底线 B4"；主楼分区写"主楼 B2 区" |

### 3.4 C# 程序集、命名空间与文件

- **命名空间**：命名空间用 PascalCase，按 ENG-ARCH §2 的程序集与职责取，不跟目录的字面大小写：`game/gen/zones/south/` 对应 `Campus.Gen.Zones.South`，`game/systems/water/` 对应 `Campus.Systems.Water`；契约程序集的分组目录（`Foundation/`、`Ui/` 等）共用根命名空间 `Campus.Contracts`，只有 ENG-ARCH §3 列出的子命名空间（如 `Campus.Contracts.AppFlow`）例外。IDE0130 按目录字面大小写判，不适用本约定，`.editorconfig` 里关掉；改由 G1 的源码门禁按本条检查（ENG-ARCH §2）。
- **程序集**（边界与可引用关系以 ENG-ARCH §2 的表为准）：

| 程序集 | 目录 | 说明 |
| --- | --- | --- |
| `Campus.Contracts` | `game/contracts` | 纯 .NET 契约，只引用基础类库；子目录 `Quality/`、`Maps/`、`Atlas/`、`AppFlow/`、`Cinematic/`、`Save/`、`Audio/`、`Parts/`、`schemas/` |
| `Campus.Gen.World` | `game/gen/world` | 世界模型 |
| `Campus.Gen.Parts.Core`、`Campus.Gen.Parts.<类>` | `game/gen/parts/…` | 部件内核与各部件包 |
| `Campus.Gen.Interior`、`Campus.Gen.InteriorProps` | `game/gen/interior/core`、`game/gen/interior-props` | 室内共享层，只许 `InteriorProps → Interior` 一个方向 |
| `Campus.Gen.<内容>` | `game/gen/<内容>` | buildings、ground、diorama、zones.*、vegetation、water、fauna、traces、furniture、walkable、collision、outskirts、interior.shell、acoustics、ambience；内容程序集之间互不依赖 |
| `Campus.Pipeline.Map` | `pipeline/map` | 底图烘焙器，离线工具 |
| `Campus` | kernel、style、camera、medium、systems、hud、stage、gates、flow、audio | 主程序集（Godot 侧） |
| `Campus.Gen.Tests`、`Campus.Tests` | 各包的 `tests/` | 测试按目录归属 |

- 各区程序集带 `[assembly: MapScope("qingshuihe")]`，由 WP-00 写进 csproj；其余 gen 程序集是通用模块（ENG-ARCH §2、SYS-MAPS §7）。
- **文件名**：实现 `IContentModule` 的类写在 `*Module.cs`；Godot 侧包的根节点写在 `*Node.cs`，标 `[KernelNode]`（ENG-ARCH §3）。
- **特性的 ID**：小写、点分：`[ContentModule("zone.south")]`、`[ContentModule("interior.shell")]`、`[Part("opening.window.casement")]`、`[KernelNode("systems.water")]`、`[InteriorProvider("interior.library")]`、`[AmbientEmitter("ambience.water")]`、`[MediumPresenter("medium.interior")]`；`Owner` 写 `"WP-xx"`，G5 核对与 `ownership.json` 一致。区域私有的部件 ID 加区名前缀（ENG-ARCH §2、ENG-ARCH §5）。
- **存档段 ID**：小写字母、数字、连字符（如 `"world"`）；按图的段在文件里展开为 `map:<id>/<段>`（ENG-ARCH §3）。
- **全局着色器参数**：名字一律小写下划线、不带前缀（`weather_a`、`map_rect`）；C# 侧只用 `ShaderGlobals` 的常量（`ShaderGlobals.WeatherA`），不写字符串字面量（ENG-PERF §1.5、ENG-ARCH §3）。总线名同样只用 `AudioBuses` 的常量。
- **JSON**：一律用 `ContractJson.Options`：字段 camelCase，枚举按字符串，向量写成数组 `[x, y, z]`；多态类型的类型字段写在每个对象的第一个字段（覆盖层是 `"op"`，碰撞体是 `"kind"`）；存档 DTO 用各包自己的源生成 `JsonSerializerContext`，约定相同，ID 类的 record struct 写成字符串（ENG-ARCH §3、SYS-SAVE §3.5）。
- **命令行取值**：天气写连字符小写（`few-clouds`、`after-rain`）；季节写英文小写；预设 `smooth`、`balanced`、`fine`、`ultra`；设备档 `dgpu-high` 等；`--quality-set` 的单项名是 `QualityKnob` 的 snake_case（ENG-GATES §1）。
- **过场**：过场 ID 用 snake_case（`enter_new`），`map.json` 里的角色名用 camelCase（`enterNew`）（SYS-MAPS §3）。

### 3.5 色名

- 色名是唯一的取色途径：色名 → 色号（0–511，固定，只增不改；0–255 是 bank 0，256–511 是 bank 1）→ 调色板纹理的一列（ART-STYLE §8.5）。内容代码只写色名（`ToonSpec` 等的 `PaletteToken`），代码和着色器里不写十六进制色值（G4 正则）。
- **写法**：小写，用点分层，`<对象>.<状态或用途>`，例如 `wall.lit`、`wall.shade`、`ginkgo.deep`、`window.warm`、`sign.green`；一段内多个词用驼峰（`water.poolGlow`）。后缀 `.lit`、`.shade`、`.deep` 分别是受光、影、深影（ART-STYLE §8.1、ART-STYLE §2.1）。
- **登记**：WP-04 维护核心表（`game/style/palette/`）；其他包的色名写 `game/style/palette/ext/<包>.json`（色号 0–255，schema 1）；色号 256–511 的新色名写 `game/style/palette/ext-v2/<包>.json`（schema 2），两个目录都是一包一个文件、各包只写自己的那份（`ownership.json` 按包归属），两种格式不混读；色名重复或色号冲突由 G5 报（ART-STYLE §8.5）。
- **界面色**：前缀 `ui.`，阶梯写 `ui.blue.400`、`ui.neutral.25`；只登记在 `ext/ui.json`，不分色号、不进调色板纹理；地图色在 `ui.json` 的 `map` 段（UI-VISUAL §3、UI-VISUAL §3.8）。
- **室内色名**约 22 个，登记在 `ext/interior.json`，与室外分开计数（WLD-INTERIORS §8.4）。
- **退役色名**（`shadow.contact`、`line.building`、`line.tree`、`shadow.interior`；室内影色在调色板第 2 行）色号保留、不分配给别的色名，新代码不得引用（ART-STYLE §8.1，G5）。

### 3.6 着色器文件

- 着色器都放在 `game/style/shaders/`，计算着色器另可放 `game/style/outline/`；这两个目录的文件清单就是白名单，其他目录出现 `.gdshader`、`.gdshaderinc`、`.glsl`，G4 一律拒绝。新增文件要在 `ownership.json` 里登记，逐文件归属（ENG-ARCH §2、ENG-PERF §1.4）。
- 文件名以系统名作前缀。现行白名单（ENG-PERF §1.4）的前缀与归属：

| 文件 | 归属 |
| --- | --- |
| `globals.gdshaderinc` | WP-01 |
| `toon.gdshaderinc`、`fog.gdshaderinc`、`toon_static`、`toon_cut`、`toon_glass`、`toon_glass_near`、`toon_decal` | WP-04（`toon_cut` 由 WP-07、`fog` 的天空色带部分由 WP-40 以需求提交） |
| `toon_stub` | WP-00（桩，WP-04 交付后删除） |
| `veg_trunk`、`veg_card`、`veg_impostor`、`veg_grass` | WP-23 |
| `bake_impostor`（只在替身烘焙器的 SubViewport 里用，不进游戏场景） | WP-23 |
| `veg_lotus`、`fx_fall` | WP-38 |
| `water`、`water_clear`、`reflect_flat` | WP-24 |
| `motion_wind.gdshaderinc` | WP-26 |
| `precip` | WP-45 |
| `fx_glow` | WP-39 |
| `diorama_section`、`diorama_far` | WP-22 |
| `sky` | WP-40 |
| `medium_overlay` | WP-07 |
| `cine_cloud` | WP-49b |
| `ui_panel`、`ui_screen` | WP-35 |
| `map_base` | WP-48b |
| `post_*.glsl`（outline、tiltshift_blur、tiltshift_comp、flare、lut、fxaa） | WP-05 |

- 着色器里不用 `TIME`；`vertex()` 里做距离判断用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`，不用 `CAMERA_POSITION_WORLD`（ENG-ARCH §2）。

### 3.7 其他命名

- **主检出**：`E:\Project\UESTC_RPG`，始终在 `main` 上，所有代理在这里直接改；`user://` 的目录名是 `UESTC_DEV`（ENG-ARCH §6）。
- **截图**：放 `output/render-review/`，命名"包-视角.png"或"包-Qn.png"；非默认状态依次加季节、钟点、天气、预设，名字与命令行取值相同，例如 `WP-18-V6-winter-2100-snow.png`（WF-ONBOARD §4.2）。
- **存档文件**：`saves/<slot>/g<6 位代号>.sav`，如 `s1/g000041.sav`（SYS-SAVE §1）。
- **启动集**：每张图一个 `boot_<地图>.pck`，包内路径 `res://bake/<地图>/boot/`；其余烘焙产物是本机缓存 `user://cache/bake/<地图>/` 里的 `.tbin`（D-165，ENG-ARCH §4）。

### 3.8 枚举与扩展纪律

以下写法现在就照做，以后加观察模式、规则集、RPG 子系统时才不用改既有语义（细则以 ENG-ROADMAP §3 的写法纪律和 ENG-ROADMAP §6.1 第 4 条为准）：

- **枚举只追加、不重排、不删值**：所在的契约组冻结后（D-134），枚举只能在末尾追加；JSON 里按字符串存（ENG-ARCH §3）。`QualityKnob` 按序号置位存进 `QualityChange.Knobs`，重排会直接改变含义。
- **穷举 `switch`，`default` 报告未知值**：会追加值的枚举（`ViewMode`、`AppState`、`MapKind`、`MapTravelCause`、`MarkerKind`、`SaveReason`、`SlotKind`、`TransitKind`、`VeilReason`、`DuckReason`、`AudioGroup`、`FoleyKind`、`QualityKnob` 等），消费方一律写穷举的 `switch`，`default` 分支拒绝或报错，不把未知值静默当成某个已知值。
- **不做二值判断**：不写 `mode == ViewMode.Fly ? … : …` 这类写法；它会把以后追加的值静默归进某一边，编译器查不出。G1 的 `-warnaserror` 能挡住非穷举的 `switch` 表达式，二值判断靠评审（ENG-ROADMAP §6.1）。
- **存档里的枚举按字符串存**：存档 DTO 的枚举按字符串写（SYS-SAVE §3.5）；读到不认识的枚举字符串，按"这一段不合法"处理，不猜。
- **不写观光专属的常量**：解锁策略名、事件 ID、允许的观察模式这类观光规则，一律读规则集，不写成代码里的常量或判断（ENG-ROADMAP §3）。
- **"观者在哪"与"画面从哪看"分开读**：前者读 `IObserverState`，后者（视锥、听者、流式选级）读 `ICameraRig`（ENG-ROADMAP §3；契约以 ENG-ARCH §3 定稿为准）。
- **换世界实例直接落到新值**：做平滑、回差、保留历史的系统看到 `IWorldState.Instance` 变了，就把状态直接设成新值；以后的 NPC、剧情状态同样遵守（SYS-SAVE §5.2）。

### 3.9 持久化 ID 的稳定

存档、覆盖层、标记和其他数据文件按 ID 引用对象，ID 就是对象的身份。下列 ID 都按本节执行：地图 ID、实体 ID（含 `design:` 增补）、点位 P、锚点 A、路线 R、室内入口 ID（标记写 `door:<入口 ID>`）、区域 ID 与地点翻译键、存档段 ID。

- **不复用**：ID 进了 `main` 以后，对象删掉了，ID 也不再分给别的对象；新对象一律取新号（例如点位从 P24 往后编，§3.2）。
- **改名不改身份**：改显示名、译名、所在文件或分区，ID 不变；轮廓、高度、位置的调整走覆盖层（§3.1），ID 也不变。
- **确需换 ID 时给旧新映射**：例如刷新 OSM 快照后，同一个真实对象的 OSM ID 变了。负责该数据的包随数据提交一份旧 ID → 新 ID 的映射；引用这些 ID 的存档段升段版本，由 WP-50 在迁移函数里按映射改写（SYS-SAVE §3.6）。
- **真正删除不给映射**：读档时按 SYS-SAVE §5.6 的校验修复处理（从已访问、解锁、追踪的列表里去掉，`map:<id>/objects` 丢掉那一条）。

## 4 帧顺序

权威表在 ENG-ARCH §3 的 `FramePriority`（D-135）；SYS-TIME §1、ENG-PERF §6、SYS-MAPS §9、SYS-AUDIO §20 的表与它逐行一致。下表是速查，两者不一致时以 ENG-ARCH §3 为准。

| 值 | 常量 | 工作包 | 做什么 |
| --- | --- | --- | --- |
| −1300 | `Map` | WP-46 | 推进切图状态机；雾幕全黑时分发 `IMapAware`；Mount 时写 `map_rect`；`Current`、`Context` 在这一步换 |
| −1200 | `Quality` | WP-32 | 即时类设置与自动调节生效，分发 `IQualityAware`，写全局参数 `quality` |
| −1100 | `Camera` | WP-06、WP-36、WP-42、WP-49a | 移动镜头（含可达空间求值、写 `edge_fog`）；Title、Entering、Leaving 下由 `CinematicPlayer` 驱动镜头 |
| −1000 | `WorldState` | WP-34；WP-45a | 推进世界时间、钟点、季节（含 `FlowTo` 的停点），算日月；再调 `IWeatherProcess.Step` 求天气向量 |
| −900 | `KeyLight` | WP-03 | 主光与阴影：高度钳、方向量化、日月交接，按阴度改投影，写天光方向 |
| −800 | `Palette` | WP-37、WP-04 | 调色板插值，叠天气修正量，写调色板纹理 |
| −700 | `Channels` | WP-03、WP-39 | 按有效太阳高度 h_eff 求 32 个灯光通道 |
| −650 | `Motion` | WP-26；WP-45 | 风场、一拍二节拍、偶发事件、钟面指针与钟锤；降水数量与闪电 |
| −640 | `SeasonFx` | WP-38 | 季节氛围 |
| −600 | `Medium` | WP-07 | 介质判定，写 `medium`、`interior_light`；室内运行时（按层显示、软停信号）；自动门的状态机 |
| −590 | `Acoustics` | WP-53c | 本帧听者处的声学与路由，含门户开度 |
| −500 | `Stream` | WP-02、WP-43 | 流式选级与请求、碰撞格请求；切图期间不发新请求 |
| 0 | `Systems` | 其余各包 | 默认优先级的节点（含 `IAtlasService` 的 10 Hz 查询） |
| 50 | `Audio` | WP-53a、53b、53d | 仲裁、权重、排程、写播放器音量 |
| 100 | `Hud` | WP-35、WP-48b | `HudModel`、`MotionHub`、`UiThemeClock`（写 `ui_night`）；小地图与大地图 |
| 900 | `StreamMount` | WP-02、WP-43 | 挂载与碰撞入树，每帧 ≤ 1.0 ms，分帧 |
| 1000 | `Save` | WP-50 | 每帧最后一步：在安全时刻拍快照；读档时在这一步恢复 |

- 用节点的 `ProcessPriority` 固定，值越小越先执行，与场景树里的顺序无关；任何两步不得同值（同值时按场景树顺序，说不清先后）。
- 行走物理在 `_PhysicsProcess` 里按固定 120 Hz 子步推进，不在上表里；脚步等观者声在那里当场发出。自动门的开关门声在 `Medium` 那一步门的阶段变化时当场发出（ENG-ARCH §3）。
- 新增一步：在现有空档里加常量，属于契约变更（走 WP-00），ENG-ARCH §3、SYS-TIME §1、ENG-PERF §6、SYS-MAPS §9、SYS-AUDIO §20 五张表同步改（ENG-ROADMAP §6.2）。
- **画布层号**（从下到上）：3D 画面 → CanvasLayer 4 预热层（只在扉页阶段，WP-01）→ 5 雾幕与扉页（WP-01）→ 9 放映层（WP-35d）→ 10 界面 `HudRoot`（WP-35）→ 11 署名（WP-01）。OSM 署名始终在最上层（UI-VISUAL §11.1、SYS-TITLE §8.5）。

## 5 目录与归属

### 5.1 仓库

```
UESTC_RPG/
├─ game/            Godot 项目根（res://）
│  ├─ contracts/    WP-00  Campus.Contracts：纯 .NET 契约与 schemas/
│  ├─ gen/          纯 .NET 生成器（Campus.Gen.*）：world、parts、buildings、ground、diorama、zones、
│  │                interior、interior-props、outskirts、acoustics、ambience、vegetation、walkable、collision、
│  │                water、fauna、traces、furniture
│  ├─ kernel/       Godot 侧内核：boot、renderer、frame、inspect、registry、bake、lod、stream、lighting、post、
│  │                quality、occlusion、world、collision、map、save
│  ├─ flow/  audio/  style/  camera/  medium/  systems/  hud/  stage/  gates/
│  ├─ data/         运行时 JSON：maps/（按图）、weather/<剧本>/、audio/、vegetation/、rulesets/、quality-presets.json；text/（RPG 预留）
│  ├─ assets/       style/、hud/、maps/<地图>/thumbs/、title/<地图>/、audio/、hero/
│  └─ addons/       第三方插件
├─ pipeline/        离线管线：OSM 管线、raw/<地图>/、walk、nav、assets、colorscript、outskirts、map、audio
├─ scripts/         gate.ps1 与 gates/（含 matrix、arch）、wt/、devices/、godot-api/
├─ tools/           godot-version.txt
├─ docs/            design/、godot-api/
└─ global.json、ownership.json、budgets.json、third_party.json、.gitattributes、.gitignore
```

- 目录树全文和逐目录的归属以 ENG-ARCH §2 为准；`ownership.json` 照它写。改动的路径越出本包范围，G3 拒绝。
- **RPG 预留目录**：`game/entities/`、`game/contracts/Entities/`、`game/data/text/`、`game/data/maps/<地图>/entities/` 在 `ownership.json` 里记为 WP-00 的预留，观光期不写入（ENG-ROADMAP §6 第 6 条）。
- **共享文件**（工程共享件由 Astra 改并直接提交推送，`docs/design/` 由主会话改，规则见 WF-BUILD §1）：`game/project.godot`（含 Shader Globals、动作表、Autoload）、`export_presets.cfg`、`Campus.sln`、全部 `*.csproj`、`Directory.Build.props`、`Directory.Packages.props`、`packages.lock.json`、`global.json`、`.editorconfig`、`.gitattributes`、`.gitignore`、`game/addons/`、契约、`ownership.json`、`budgets.json`、`third_party.json`、`tools/`、`docs/design/`、`docs/godot-api/`、各图的 `interiors.json`；`map.json` 归 WP-46，但字段增改经主代理（ENG-ARCH §6）。
- **不进 git**：构建锁（位置以 `scripts\` 为准）、烘焙缓存（环境变量 `UESTC_BAKE_CACHE`，默认 `E:\Project\UESTC_RPG.cache\bake\<地图>\`）；仓库里的 `build/`、`output/`、`.godot/`、`override.cfg`（ENG-ARCH §2、ENG-ARCH §4、ENG-ARCH §6）。`*.wav`、`*.ogg` 进 Git LFS。
- `game/contracts/`、`game/gen/` 和每个 `tests/` 目录各放一个空的 `.gdignore`，Godot 不扫描纯 .NET 代码（ENG-ARCH §2）。

### 5.2 按图的数据

- 按图的数据都在 `game/data/maps/<地图>/` 下；地图清单是 `game/data/maps/index.json`（WP-46）。清水河目录的主要文件与归属（全表见 SYS-MAPS §2.1）：

| 文件 | 归属 |
| --- | --- |
| `map.json` | WP-46（字段增改经主代理） |
| `campus.json`、`network.json`、`zones.json` | WP-08 |
| `overlay/<组>.d/`（各区、`public`、`waterfront`、`boundary`） | 各包只写自己的分片；`0000-wp-<包号>-base.json` 是历史底稿，归对应区包 |
| `overlay/walkable.overlay.json` | WP-44 |
| `overlay/outskirts.overlay.json`、`outskirts.json` | WP-47 |
| `overlay/atlas.d/`、`atlas/markers.json` | WP-48c；`atlas/labels.json` 归 WP-48a |
| `overlay/interior/<区>.interior.json` | 各区精修室内的包 |
| `overlay/terrain/<区>.terrain.json` | 一个文件只归一个包（文件与包的对应见 MAP-QSH-TERRAIN，写法见 WLD-TERRAIN §3） |
| `interiors.json` | 开放室内清单：主代理（schema 与格式归 WP-51，条目由负责包经回报提交，增删由主代理审批，D-164） |
| `incidents.json` | WP-26（偶发事件清单，SYS-MOTION §10） |
| `waypoints.json` | WP-36 |
| `lighting-schedule.json` | WP-39 |
| `backdrop.json` | WP-22 |
| `sky.json` | WP-40 |
| `review.json` | WP-27 |
| `cinematics/<id>.json` | WP-49a |
| `audio/…` | WP-53b、WP-53c 按文件分 |
| `nav/…` | WP-44 |
| `entities/` | WP-00 预留（RPG，观光期不写入） |

- **全局数据**：`game/data/weather/<剧本>/`（WP-45a，清水河用 `chengdu`）、`game/data/audio/`（WP-53）、`game/data/quality-presets.json`（WP-32）、`game/data/vegetation/`（树种表，WP-23）、`game/data/rulesets/`（规则集，WP-00）。按图与全局的划分见 SYS-MAPS §2.2。
- **路径**一律由 `IMapContext.DataRoot` 拼出（`res://data/maps/<地图>/…`），代码里不写数据路径、地图 ID 和实体 ID 的字面量（SYS-MAPS §7、D-139）。`game/gen` 的纯 .NET 代码不碰文件路径：数据由内核用 `FileAccess.GetFileAsBytes()` 读成字节，交给 `Campus.Gen.World` 解析（ENG-ARCH §2）。
- 按图的资源放在各资源目录下的 `<地图>/` 子目录；`pipeline/` 的脚本都接受 `--map=<id>`，原始数据放 `pipeline/raw/<地图>/`（SYS-MAPS §2.1）。

### 5.3 `user://` 下的文件

| 文件 | 读写方 | 内容 |
| --- | --- | --- |
| `graphics.cfg`、`engine_override.cfg` | WP-32 | 画面设置；引擎先读的覆盖项（SYS-GRAPHICS §6、SYS-GRAPHICS §3） |
| `settings.cfg` | WP-35 | 界面设置（含平面地图的设置，WP-48b 经 WP-35 的接口读写） |
| `input.cfg` | WP-06 | 改键：只存物理键码和鼠标键号 |
| `audio.cfg` | WP-53a | 五组音量与声音开关 |
| `saves/<slot>/g<gen>.sav`、`saves/.lock`、`saves/corrupt/` | WP-50 独占 | 存档：每代一个新文件，保留 3 代；损坏的只挪进 `corrupt/` |
| `cache/bake/<地图>/<ABI>/…`、`cache/bake/<地图>/cache.index` | WP-02 独占（`IBakeService`） | 本机烘焙的缓存（D-165）：L0–L3 的 TileBundle、碰撞级 C、室内包与碰撞包、平面地图底图，一律是自定义二进制 `.tbin`（不是 Godot 资源）。不是存档，删掉只会让下次重新烘焙 |

- 设置类文件读值只接受 bool、int、float、String；先按文本检查，含 `Object(`、`Resource(` 等就整份退回；写盘走 tmp + `FileStream.Flush(true)` + `File.Replace`（SYS-SAVE §8）。
- 只有 `Campus.Kernel.Save` 能读写 `user://saves`；任何代码都不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`，因为 `user://` 里的 Godot 资源能嵌脚本，读入即执行（ENG-ARCH §2、SYS-SAVE §8）。本机缓存因此用自定义二进制 `.tbin`，由 `IBakeService` 用 `FileAccess` 读写，读入后先按随包的 `bake.manifest.json` 校验哈希再用（ENG-PERF §5）。
- 门禁和基准不读任何 `user://` 文件，本机缓存由 `--bake-cache` 指到共享的烘焙缓存（ENG-GATES §1）；`--save=off` 下 `user://saves` 不被创建、不被修改（SYS-GRAPHICS §6、SYS-SAVE §12）。开发态经 `override.cfg` 把 `user://` 指到 `UESTC_DEV`（ENG-ARCH §6）。
- **目录名**：正式版的 `user://` 是 `%APPDATA%\UESTC\`（`custom_user_dir_name = "UESTC"`，ENG-ARCH §2）。目录名不带版本号，所有版本共用，解压新版本后存档、设置和日志照常沿用（OPS-RELEASE §2）。开发态另行隔离：用 `UESTC_DEV`（§3.7），不读写正式目录。

### 5.4 文档目录

- `docs/design/`：开发文档，开发以它为准（D-152）。`README.md` 是总索引（CORE-INDEX）；文件夹与编码前缀见 WF-DOC §3 的表。
- `reference/REF-PROMPT-完整提示词备份.md` 是作品描述的备份，不是施工文档；与拆分文档冲突时以拆分文档为准（D-153）。
- `docs/godot-api/`：Godot 4.7.2 C# API 参考。写 Godot 代码前先在 `docs/godot-api/signatures.txt` 查到签名，不凭记忆写 API（D-145）。

## 6 确定性

- **种子**：种子 = `StableHash.Hash32(世界种子, featureId, partId, 用途)`，与生成顺序无关；世界种子取 `BuildContext.Map.Seed`（`map.json` 的 `seed`，`--seed` 可覆盖）。`BuildContext.Seeded(…)` 直接给出带种子的 `Rng`（ENG-ARCH §2、ENG-ARCH §3）。
- **唯一的随机来源**是契约的 `Rng` 与 `StableHash`。禁用 `System.Random`、`GD.Randf`、`GD.Randi`、`GD.RandRange`、`Godot.RandomNumberGenerator`；不得显式调用 `string.GetHashCode()` 当种子；不用 `Guid.NewGuid`，新 ID 用按存档递增的整数（例如平面地图的自定义标记）（ENG-ARCH §2）。
- **唯一的熵源例外**：新游戏时由 `ISaveService.NewGame` 取一次熵，作本存档的天气种子，写进日志和存档；`--weather-seed` 给出时不取（ENG-ARCH §2、SYS-WEATHER §6.4）。
- **生成器写法**：
  - "放不放、选哪种"一律用整数哈希；三角函数只用来算坐标，结果量化后再用；
  - 超越函数一律用契约的 `DetMath`，禁用 `Math`、`MathF` 的 `Sin`、`Cos`、`Atan2`、`Exp`、`Log`、`Pow` 等，因为它们走系统数学库，不同机器上结果可能不同；`Math.Sqrt` 和四则运算可以用。也禁用 `Vector<T>`、`System.Runtime.Intrinsics`、`FusedMultiplyAdd`，以及内部走三角函数的 `System.Numerics` 构造（`Matrix4x4.CreateRotation*`、`CreateFromAxisAngle`、`CreateFromYawPitchRoll`、`Quaternion.Slerp` 等），改用 `DetMath` 的同名工具。这样本机烘焙的产物与 CI 参考逐字节一致（D-165，ENG-ARCH §2）；
  - 不依赖 `Dictionary`、`HashSet` 的遍历顺序，遍历前先按 ID 排序；
  - 一个 `build()` 内部不做并行的浮点归约；gen 里另禁 `Stopwatch`、`Parallel`、`Task.Run`；
  - 跨格的要素按锚点归属唯一的一格，格子包围盒相应外扩；
  - `build()` 的输入里没有世界状态和画面设置，所以改时间、换季、换天气、改画质不会让烘焙产物失效（ENG-ARCH §2、ENG-PERF §5）。
- **天气**：每次抽取用计数器子流 `StableHash(种子, mapId, epoch, 片段号, 尝试号, 用途)`，与补队列的时机和帧率无关（SYS-WEATHER §6.4）。
- **音频**：音频种子 = `StableHash(--seed, "audio")`，每个发声器、每个排程槽一条子流；不用 `AudioStreamRandomizer`（ENG-ARCH §2、SYS-AUDIO §17）。
- **运行时的确定性**：同一钟点、同一哈希的东西结果相同（例如窗灯按哈希亮灭）；同一构建 + 同一种子 + 同一串输入（命令行、Movie Maker 固定步长、脚本化的时间推进）得到同样的画面。时间不能逆转，所以不要求"倒回去复现"。读档后的续跑等价见 SYS-SAVE §5.3（ENG-ARCH §3）。
- **Movie Maker**：`--write-movie <目录>\f.png --fixed-fps 60 --quit-after <帧数>`，delta 恒定，时间按帧号推进；流式管理器切换成同步的"流式屏障"，本帧要用的格全部就绪再推进；过场、读档、切图同样是屏障。PNG 序列旁边写一个 WAV，同一组参数两次录音逐字节相同（ENG-GATES §1、ENG-PERF §5、SYS-AUDIO §17）。
- **截图默认串**（权威在 ENG-GATES §1，D-146）：

  ```
  -- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off
  ```

  出别的视角只换 `--pose`；门禁和基准只由命令行决定，不读 `user://`；截图在窗口模式下、等流式队列空和管线编译计数稳定以后再存图（ENG-GATES §1）。

## 7 待对齐

本节登记本文与权威位置之间的差异；由主代理判定后改相关文档。

（暂无）
