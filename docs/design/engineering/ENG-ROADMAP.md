---
id: ENG-ROADMAP
title: 扩展路线：从观光到 RPG
category: engineering
summary: 规定观光版给以后 RPG 留的扩展点、现在不能写死的地方和 v1 冻结前的最小预留
owners: [WP-00]
contributors: [WP-06, WP-07, WP-26, WP-27, WP-35, WP-39, WP-42, WP-46, WP-48b, WP-48c, WP-49a, WP-50]
depends: [CORE-INDEX, ENG-ARCH, ENG-WP, SYS-SAVE, SYS-MAPS]
related: [SYS-MOTION, ENG-BUDGET, ENG-PERF, SYS-TIME, UI-NAV, SYS-WALK, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO]
contracts: []
data: []
gates: [G4, G5, G20]
---

# ENG-ROADMAP · 扩展路线：从观光到 RPG

> **范围**：本文只规定架构上的扩展点和"现在不能写死什么"：观光版已经留了哪些口子，哪些观光规则要做成规则集的配置，v1 冻结前必须加的最小预留。不设计 RPG 玩法。
> **不在本文**：契约签名与冻结（ENG-ARCH §3，以及 ENG-ARCH 末尾的"扩展点（RPG）"一节）；各系统细则（各系统文档）；预算数值（ENG-BUDGET、ENG-PERF）；工作包划分（ENG-WP）。本文列出的契约名、数据路径以 ENG-ARCH §3、SYS-MAPS §2.1 为准，所以 front matter 的 `contracts`、`data` 留空。
> **关联**：[CORE-INDEX 总览](../README.md)、[ENG-ARCH 工程架构](ENG-ARCH.md)、[ENG-WP 工作包](ENG-WP.md)、[SYS-SAVE 存档](../systems/SYS-SAVE.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS.md)、[SYS-MOTION 动态与事件](../systems/SYS-MOTION.md)、[ENG-BUDGET 性能预算](ENG-BUDGET.md)、[ENG-PERF 性能工程](ENG-PERF.md)、[SYS-TIME 时间季节](../systems/SYS-TIME.md)、[UI-NAV 界面与跳转](../ui/UI-NAV.md)、[SYS-WALK 行走](../systems/SYS-WALK.md)、[SYS-GRAPHICS 画面设置](../systems/SYS-GRAPHICS.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL.md)、[SYS-ATLAS 平面地图与传送](../systems/SYS-ATLAS.md)、[SYS-TITLE 开始页与过场](../systems/SYS-TITLE.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS.md)、[SYS-AUDIO 音频](../systems/SYS-AUDIO.md)

## 1 扩展原则

以后的方向是 RPG，主线是"入学到毕业"。观光版不做任何 RPG 内容，只保证 RPG 进来时不用推翻现有架构。

**规则集**：决定"这一局按什么规则运行"的一组配置。观光是规则集 `sightseeing`，存档 meta 的 `Ruleset` 字段已经记它（SYS-SAVE §10）；RPG 是另一个规则集。

| # | 原则 | 做法 | 依据 |
| --- | --- | --- | --- |
| 1 | 观光是 RPG 的子集 | 同一套世界、渲染、地图服务、存档、音频；观光只是关掉了一部分功能的规则集，不另做一套"RPG 版引擎" | SYS-SAVE §10 的 `Ruleset`，SYS-ATLAS §11 的 `PolicyId` |
| 2 | 新功能只用四种方式进来 | 加服务（`[KernelService]`、`[KernelNode]`）、加数据（按图或全局目录）、加存档段（`[SaveSection]`）、加枚举值（末尾追加）。不改既有契约的语义，不加 Autoload | ENG-ARCH §2、ENG-ARCH §3、SYS-SAVE §3.6 |
| 3 | 观光的禁令是配置，不是代码 | 无人物、无点选、一个偶发事件、两种观察模式、过场只在开始页前后，都写进 `sightseeing` 规则集；运行时代码读规则集，门禁按规则集取判据 | §3、§6 第 1 条 |
| 4 | 观光规则一条不放松 | 规则集化只改"规则写在哪"，不改规则本身。CORE-INDEX §3 的不变量和 ENG-GATES §3 的一票否决在观光规则集下照旧执行。扩展点只留口子，不提前做 RPG 内容（例如 SYS-SAVE §2 的 `character` 段只占名字） | INV-1–INV-12、ENG-GATES §3、SYS-SAVE §2 |
| 5 | 确定性和单调时间照旧 | 剧情、NPC、事件的随机一律走 `StableHash` 与 `Rng`；世界时间在一个世界实例内单调；读档换世界实例 | ENG-ARCH §2、ENG-ARCH §3、SYS-SAVE §5.2 |
| 6 | 预算按账本申请 | RPG 子系统是新的工作包，进 `budgets.json` 的账本，不挤占观光各包的额度 | ENG-BUDGET §8 |
| 7 | 预留越少越好 | 只有"以后再补会改变既有契约的语义、存档格式或目录归属"的，才在 v1 冻结前预留（§6）；其余以后只增即可 | ENG-ARCH §3 的只增不改 |

## 2 现有扩展点清单

| 方向 | 现状（观光版） | 已预留 | 以后怎么接 | 依据 |
| --- | --- | --- | --- | --- |
| 多地图 | 只有 `qingshuihe` 一张图；按图数据都在 `game/data/maps/<id>/`；切图走雾幕、先卸后装 | `IMapService`、`MapDescriptor`、`IMapAware`、程序集级 `MapScope`；`MapPortal` 与 `MapTravelCause.Portal`；同图重载门禁逼出单图假设 | 剧情需要的新场景按 SYS-MAPS §14 的八步加图，不改契约、不改流式架构；邻图入口启用 `MapPortal`，边界提示语同步改（WLD-OUTSKIRTS §10） | SYS-MAPS §0、SYS-MAPS §8、SYS-MAPS §14 |
| `MapKind`、`MapTimeRule.Fixed` | 只有 `Outdoor`、`Shared`；写了 `Fixed` 的图 `Travel` 返回 `Rejected`，G5 也拒绝 | `Interior`、`Instance` 两种图；`Fixed` 进图钉住钟点、季节、天气，界面控件变灰，全局时间照走 | 副本式的小场景用 `Instance` + `Fixed`；楼层图用 `Interior` 或 SYS-ATLAS 的 `AtlasLayer` | SYS-MAPS §14、ENG-ARCH §3 |
| 存档段与世界实例 | 8 个段，3 个恢复阶段；读档、新游戏都换世界实例 | 未知段原样保留，缺段用 `CreateDefault` 补；每段独立版本与迁移链，金档；预留段名 `quest`、`inventory`、`character`、`flags`、`map:<id>/objects`；`SlotKind.Manual`（`m1`–`m9`）、`SaveReason.Checkpoint`、meta 的 `Ruleset`；容器版本 1 在 P0 定死 | RPG 只加段（`Content` 阶段，按 `Order` 恢复），不改容器；观光档在 RPG 版里读出来仍是观光规则集，缺的段取默认值 | SYS-SAVE §2、SYS-SAVE §3.3、SYS-SAVE §3.6、SYS-SAVE §10、SYS-SAVE §5.2 |
| 解锁策略 `proximity` | 只启用 `all-unlocked` | `IUnlockPolicy`、`UnlockState`（Hidden、Known、Unlocked）、`AtlasSave.Unlock`、`IAtlasService.PolicyId`；`Proximity` 的判定半径、揭示规则和 64 位揭示掩码已写好；解锁提示已登记 | RPG 规则集选 `proximity` | SYS-ATLAS §8、SYS-ATLAS §11、UI-NAV §4.9 |
| 标记与图层 | 7 类标记 | `MarkerKind.Quest`、`AtlasLayer`、`TravelRefusal.Locked`；走线母题可连到任务列表 | 任务服务产出 Quest 标记，经 `IAtlasService` 追踪 | SYS-ATLAS §5、SYS-ATLAS §9、UI-VISUAL §5.5 |
| 开放室内清单与生成器 | 15 处室内加 3 处高处 | 加楼只改 `interiors.json` 并注册提供者（配置型或手工型），不改契约和流式架构；会动的门是不进烘焙碰撞的运行时门体；室内覆盖层 `InteriorOverlayFile` 的记录类型只有 `SealOp`（封口）和 `InteriorConfigOp`（楼的配置），按 `op` 字段区分，以后只增（ENG-ARCH §3） | 剧情场所按 WLD-INTERIORS §14 的六步开放；剧情锁门在运行时门体上加锁状态；开放状态随剧情变时，G19c 的一致性检查（开放清单 = 注册的提供者 = 门开着的楼）按运行时的开放状态分别核对 | WLD-INTERIORS §14、WLD-INTERIORS §4.1、WLD-INTERIORS §2.0、WLD-INTERIORS §19、ENG-ARCH §3 |
| 导航图 | 烘焙的分层图（开发和 CI 的烘焙器出，随启动集发，D-165），供地图路线、G19、落点和存档的所在空间 | `INavGraph`、`NavQuery`、`NavFlags` 的 `NpcOnly`、`PlayerOnly`、`Locked`，楼级超图；NPC 局部寻路用 NavigationServer3D 的代理参数已给出 | NPC 宏观寻路与任务导引用 `INavGraph`，局部用 NavigationServer3D | WLD-INTERIORS §13、ENG-ARCH §3、SYS-WALK §6、SYS-WALK §11 |
| 偶发事件框架 | 一个事件（银杏大道），夜间 / 白天两版，各带四种天气版本 | `IncidentDirector`：触发条件（ω、转场冷却、只在 Playing、世界实例变化时清零）、全局参数 `incident` 与 `incident_route`、给音频的批号提示与整批取消；事件清单 `incidents.json`，导演只跑规则集 `Incidents` 里登记的事件（§6 第 5 条） | 泛化为事件导演：多个事件登记在数据里，观光规则集只登记这一个 | SYS-MOTION §10.1、SYS-MOTION §10.2、SYS-MOTION §10.7、SYS-MOTION §12 |
| 音频总线 | 总线树由代码建，约 21–25 条；五组音量 | `AudioBuses` 常量只增，枚举末尾追加；`FoleyKind.AutoDoorOpen`、`AutoDoorClose` 预留；动态发声器 `AddDynamic` / `MoveDynamic`（动物已在用） | 对白另加总线、音量组和压低原因；NPC 的声音按动态发声器接入；人声规则按规则集取判据 | SYS-AUDIO §3.1、SYS-AUDIO §19、SYS-AUDIO §8、WLD-LIFE §6.3 |
| 界面预留画面 | 漫游态只常驻小地图 | 对话或旁白条、任务列表、物品卡的视觉已定，母题不新增；解锁提示 | RPG 页面放 `game/hud/pages/`，用 UI-VISUAL 的组件拼 | UI-VISUAL §8.12、UI-NAV §4.9、UI-VISUAL §11.2 |
| 过场数据格式 | 开始页前后的过场；没有角色轨 | `format` 只增，遇到不认识的轨报错；每图的 `arrive` 过场预留 | 加角色轨、对白轨时升 `format` | SYS-TITLE §8.2、SYS-TITLE §12 |
| 画面设置与预算 | 27 个单项，四档预设 | `QualityKnob` 末尾追加、按位存（最多 64 项）；`KnobSpec` 自动生成界面；`budgets.json` 按包、按图记账 | 人群密度、角色细节作为新单项追加；RPG 包按账本申请额度 | SYS-GRAPHICS §8、SYS-GRAPHICS §3、ENG-BUDGET §8 |
| 自动发现与服务 | Autoload 只有两个；模块打特性即接入 | `[KernelService]`、`[KernelNode]`、`[SaveSection]` 等；按图启用模块 | RPG 子系统一律以 Kernel 服务和节点接入，不加 Autoload，不改共享文件 | ENG-ARCH §2 |
| 世界时间 | 一条全局时间线，单调向前 | `WorldHours`（double）、`Instance`、`FlowTo` | 日历由 `WorldHours` 推出，不另起时钟 | ENG-ARCH §3、SYS-TIME §1、SYS-MAPS §5 |
| 剧情显隐 | 物体显隐只看季节、时段、天气的变体掩码；生活痕迹的状态也只有这三种位 | `VariantTag` 可加 init 属性，`ChannelDecl.Source` 可加前缀（ENG-ARCH §8） | 掩码加剧情标志位，内核按 `IStoryState` 显隐，不重建格子 | WLD-LIFE §6.2、WLD-INTERIORS §9 第 4、5 条、ENG-ARCH §8 |
| 开局 | 新游戏一律从本图默认状态（落金初灯）开局，起点、钟点、季节都取默认状态 | `ISaveService.NewGame` 的 `start` 参数、`map.json` 的 `newGameStart`（ENG-ARCH §8） | RPG 规则集给出起始图、出生点、开局时刻与季节；新游戏先定规则集，再调各段的 `CreateDefault` | SYS-TIME §0、SYS-TITLE §6、SYS-TITLE §12、SYS-MAPS §4.4 |
| 工作包结构 | 54 个包，独占目录，子包共用原包的程序集 | `ownership.json`、ENG-WP §3 的拆法、通用简报 | RPG 包从 WP-54 起编号，每个子系统一个包、独占一个目录 | ENG-WP §2、ENG-WP §3、ENG-WP §4 |

## 3 现在写死、以后会挡路的地方

左列的规定在观光版里全部保留，只是把它们看作 `sightseeing` 规则集的内容。"最小预留"一栏写"无"的，以后只增即可。

| # | 现在的规定（保留，作为观光规则集的配置） | 以后的扩展方式 | 现在需要的最小预留 |
| --- | --- | --- | --- |
| 1 | 不出现任何人物或角色；观者没有身体、影子、脚印；展陈不出现人物肖像；过场里不出现人；声音里没有听得清的人声；生活痕迹允许"人刚离开"，不许"人还在"（INV-6，ENG-GATES §3 第 2、24 条，SYS-WALK §1，SYS-TITLE §13，WLD-INTERIORS §11，SYS-AUDIO §8，SYS-AUDIO §22 的 G21h；痕迹判据见 WLD-LIFE §2，ART-FIDELITY §3.4，ART-FIDELITY §6，MAP-QSH-ADDITIONS §4 第 6 条，WLD-INTERIORS §9 第 8 条） | RPG 规则集打开角色：玩家角色与 NPC 由实体层负责；对白走单独的总线和规则 | 规则集的内容规则 `Characters`、`ClearVoices`（§6 第 1 条）；实体层的目录与 ID 前缀占位（§6 第 6 条） |
| 2 | 没有可以点选的东西，没有准星；不在 3D 里拾取，3D 画面不叠加世界空间标记；会动的门走近自动推开，不加交互键（INV-1、INV-3，UI-NAV §1，UI-NAV §4.4，SYS-ATLAS §1，WLD-INTERIORS §4.1） | 近距离交互：交互键加界面提示，不恢复鼠标拾取；3D 里能不能出提示由 RPG 规则集定 | 规则集 `Interaction = InteractionRule.None`；E、I 两个键不分配给观光功能（§6 第 6 条）。交互接口不必现在加 |
| 3 | 全片只有一个偶发事件（INV-10，SYS-MOTION §10，ENG-GATES §3 第 6 条） | 事件导演登记多个事件与触发器 | 偶发事件数据化，规则集列出允许的事件（§6 第 1、5 条） |
| 4 | 只有旁观和行走两种观察模式（INV-2，ENG-ARCH §3 的 `ViewMode`，SYS-WALK §1） | `ViewMode` 末尾追加（例如第三人称跟随） | 规则集 `ViewModes`；枚举的穷举纪律（§6 第 4 条） |
| 5 | 镜头不自己动；过场只在 Title、Entering、Leaving，进场过场与返回标题是唯一的例外；不做环绕、导览、自动运镜（INV-2，SYS-MOTION §1.4，SYS-VIEW §2.5，SYS-TITLE §7.2） | Playing 里的剧情过场与对话镜头，由过场播放器在 Playing 里播；谁占用输入与镜头按原因计 | 输入占用按原因计（§6 第 3 条）；规则集 `CinematicStates` |
| 6 | 第一人称：观者与镜头重合；`IObserverState` 旁观时取镜头，行走时取 WP-42 的物理体（ENG-ARCH §3） | 第三人称下"观者在哪"（角色）和"画面从哪看"（镜头）分开 | `IObserverState`（§6 第 2 条） |
| 7 | 时间、季节、天气、流逝都可以随意调；季节不自动推进（INV-8，SYS-TIME §1，UI-NAV §4） | RPG 用日历推进季节，部分世界控件变灰（同 SYS-MAPS §14 里 `Fixed` 的做法） | 规则集 `WorldControls`；存档预留段名 `calendar`（§6 第 1、6 条） |
| 8 | 观光规则集的解锁策略是 `all-unlocked`（SYS-ATLAS §8） | RPG 规则集选 `proximity` | 策略取自规则集的 `UnlockPolicy`（§6 第 1 条） |
| 9 | 过场没有角色轨，没有对话和旁白配音（SYS-TITLE §8.2、SYS-TITLE §13） | 升 `format` 加轨 | 无 |
| 10 | 碰撞与可走性不随状态变（ENG-BUDGET §7 第 WLD-WATER、WLD-LIFE、20 条，WLD-INTERIORS §10） | 剧情锁门只走运行时门体加锁状态，不改烘焙碰撞（WLD-INTERIORS §14） | 无；运行时门体是可走性随状态变的唯一通道（ENG-BUDGET §7 第 20 条） |
| 11 | 静态内容全部烘焙进格子；生活痕迹是不可拿取的几何（ENG-BUDGET §7 第 3 条，WLD-LIFE §2，WLD-INTERIORS §9） | 可拿取的物品是运行时节点，像动物一样由 `[KernelNode]` 负责；场景物体的变化记在 `map:<id>/objects` 的差分（SYS-SAVE §2） | 无 |
| 12 | 动态物体不参与碰撞，只有猫的软圆，由 WP-25 每帧交给 WP-42（SYS-WALK §12，WLD-LIFE §6.3） | 软障碍的来源泛化，NPC 也交软圆或胶囊 | 无 |
| 13 | 只有一个观者：观者声、`IWalkTelemetry`、`Near.Dry` 总线都指玩家自己（SYS-AUDIO §9） | NPC 的脚步走 3D 发声器，观者声仍只指玩家 | 无 |
| 14 | 文字只有中英两种，内联在 `LocalizedName(Zh, En)` 里（ENG-ARCH §3）；翻译表在界面包的 `game/hud/i18n/`（UI-NAV §8）；雾幕后不许新生成 MSDF 字形（ENG-BUDGET §7 第 9 条） | 文本键与文本库；对白的字形集在构建时收齐 | 目录占位 `game/data/text/`（§6 第 6 条） |
| 15 | 同材质的两件物体相接不描：7 位材质 ID 不分物体（ART-STYLE §4.1、ART-STYLE §4.5） | 角色、道具的独立描边或选中描边用物体标记通道（`mat_table` 行 3 或另开通道） | 无 |
| 16 | 全场只有一盏方向光（太阳或月亮）；真实 OmniLight3D 不超过 32 盏且不投影；内容包不得新增光源、SubViewport、全屏通道（ART-SKYLIGHT §3.1、ART-SKYLIGHT §3.5、ENG-BUDGET §7 第 2 条） | 手持光源、技能光、交互特效作为新的光源与特效类别，额度按规则集单列（§5） | 无 |
| 17 | 水面涟漪每个湖只有 8 个槽（`ripples[8]`），只接 WP-42 的落脚事件（WLD-WATER §1.8、WLD-WATER §7.3） | 通用的涟漪事件接口，NPC、投掷物也能发；槽位按需加大 | 无 |
| 18 | 不画车流和行驶的车；电子科大站的电车静止，不做行驶与进站声（WLD-DIORAMA §5，MAP-QSH-SOUTH §1.5，MAP-QSH-WEST §13，WLD-LIFE §3.1，WLD-OUTSKIRTS §4.6，WLD-OUTSKIRTS §5.3） | 动态交通层：车流、行驶的车、电车运行与进站声，作为运行时层加进街道与外围 | 无 |
| 19 | 外围能看不能进：行走被 `Boundary` 挡住，旁观被可达空间约束推回；外围没有碰撞（ENG-PERF §5）；外围不能传送（SYS-ATLAS §2）；行走坐标按不超过 1.02 km 定单精度方案（SYS-WALK §12） | 分段开放外围：按段解除 `Boundary` 与可达空间约束，补外围碰撞、锚点与传送（WLD-OUTSKIRTS §10）；超出 ±1.02 km 时另定精度方案 | 无 |
| 20 | 界面只有 UI-NAV、UI-VISUAL 登记的件：没有任务提示、教学、成就（UI-NAV §1）；主动引导与临时提示按白名单（UI-NAV §4.7、UI-NAV §4.9）；常驻件只有登记的几种，默认固定深屏（UI-VISUAL §3.6、UI-VISUAL §8.2、UI-VISUAL §13 第 9 条）；挡路提示以随时可切旁观为前提（MAP-QSH-WEST §12） | RPG 规则集扩展界面白名单（任务提示、教学、成就）；新的常驻件（任务追踪、状态条）按 UI-VISUAL 的组件与深屏规则加入；挡路提示另写不能切旁观时的版本 | 无 |
| 21 | 计时只能由 WP-26 通过通道驱动（世界时间例外），计时 API 只许内核用（ENG-BUDGET §7 第 4 条，ENG-ARCH §2） | 玩法计时（冷却、任务时限）走带种子、可存档的计时通道，按真实秒或世界时间计 | 无 |
| 22 | 存档预算按观光档定：峰值 < 1 MB，快照主线程 p99 ≤ 0.3 ms（ENG-BUDGET §8 的 WP-50 一行，SYS-SAVE §13） | RPG 存档的大小与快照预算随规则集重订，进 `budgets.json` 的规则集账本（§5） | 无 |
| 23 | 门禁的命令行只描述观光状态：`--mode` 只有 `fly`、`walk`，没有任务、背包、角色状态的参数位（ENG-GATES §1） | 命令行参数只增：加规则集、任务、背包、角色状态的参数与新的审查视角组 | 无 |
| 24 | 音乐只在开始页与进场过场，`MusicCue` 只有 5 项（SYS-AUDIO §12、SYS-AUDIO §19） | 漫游时的音乐由规则集打开，`MusicCue` 末尾追加 | 无 |
| 25 | 观者不会受伤：没有坠落伤害和生命值，不做游泳，落水回位（SYS-WALK §2、SYS-WALK §9）；动作声只有观者的脚步、落地、碰撞、涉水和飞行风声（SYS-AUDIO §9.1、SYS-AUDIO §9.2） | RPG 规则集定坠落伤害、游泳、生命值与回位；主角动作声、受伤声在 `FoleyKind` 末尾追加 | 无 |

**写法纪律**（现在就照做，不花额外代码）：

- 不在代码里写观光专属的常量（解锁策略名、事件 ID、允许的观察模式），一律读规则集。
- "观者在哪"读 `IObserverState`，"画面从哪看"（视锥、听者、流式选级）读 `ICameraRig`。
- 会追加值的枚举不做二值判断（§6 第 4 条）。
- 读档、新游戏换世界实例时，做平滑、回差、历史的系统直接落到新值（ENG-ARCH §3）；以后的 NPC、剧情状态同样遵守。

## 4 RPG 可能新增的子系统与挂点

只写挂在哪里，不写玩法。服务名、帧顺序的值都是草案，以 ENG-ARCH 为准。新加 `FramePriority` 常量时，ENG-PERF §6、SYS-TIME §1、SYS-MAPS §9、SYS-AUDIO §20 的表同步改。

**可能新增的帧顺序步**（放在现有 17 步的空档里，任何两步不同值）：

| 步（草案） | 值 | 为什么放在这里 |
| --- | --- | --- |
| `Story` | −950 | 在 WorldState（−1000）之后，读到本帧的时间与日历；在 KeyLight（−900）、Palette（−800）之前，剧情改了世界状态，同一帧生效 |
| `Agents` | −300 | 在 Stream（−500）之后，知道哪些格已挂载；在 Systems（0）之前，交互与界面读到本帧的 NPC 位置 |
| `Animate` | 20 | 在 Systems（0）之后、Audio（50）之前，动画驱动的脚步声当帧进仲裁 |

**子系统与挂点**：

| 子系统 | Kernel 服务（草案） | 目录 | 存档段 | 帧顺序 | 会交互的现有系统 |
| --- | --- | --- | --- | --- | --- |
| 角色与动画 | `ICharacterService`（角色实例、外观、动画状态）；玩家位置仍经 `IObserverState` | `game/entities/character/`；素材 `game/assets/characters/`；离线转换 `pipeline/character/` | `character` | 移动在 `_PhysicsProcess`（WP-42 的控制器）；第三人称镜头在 Camera（−1100）；动画在 `Animate`，AnimationMixer 用手动推进，按一拍二步进（SYS-MOTION §2.2） | WP-42 控制器与 WP-43 碰撞；WP-06 镜头；WP-04 卡通着色器、WP-05 描线、WP-03 阴影；WP-53d 观者声（步相改由动画落脚给出，SYS-AUDIO §9.2）；SYS-GRAPHICS 的新单项；`IRenderHost`（ENG-ARCH §2） |
| 交互 | `IInteractionService`（附近可交互对象、当前焦点、执行） | `game/systems/interaction/` | 无：结果写进目标自己的段或 `flags` | 查询在 Systems（0），提示在 Hud（100） | `IObserverState`；输入动作 `interact`（UI-NAV §5）；`IInteriorDoors` 与运行时门体（WLD-INTERIORS §4.1）；界面提示组件（UI-VISUAL）；界面音；规则集的 `Interaction` |
| 任务与剧情 | `IQuestService`、`IStoryState` | `game/systems/quest/`；全局数据 `game/data/quests/`，按图的部分在 `game/data/maps/<id>/entities/` | `quest`、`flags` | `Story`（−950） | 日历与 `IWorldState`；`IAtlasService`（`MarkerKind.Quest`、追踪）；`IMapService.Travel`；`ISaveService`（`SaveReason.Checkpoint`、手动槽）；事件导演；对话；任务列表（UI-VISUAL §8.12） |
| 对话 | `IDialogueService` | `game/systems/dialogue/`；数据 `game/data/dialogue/`，文本进 `game/data/text/` | `flags`（读过的、选过的） | 逻辑在 Systems（0），对话条在 Hud（100） | 输入占用（§6 第 3 条）；Playing 里的剧情过场（SYS-TITLE §8.3 的播放器）；对话条（UI-VISUAL §8.12）；对白总线与压低（SYS-AUDIO §3）；文本服务；镜头 |
| 背包与物品 | `IInventoryService` | `game/systems/inventory/`；数据 `game/data/items/` | `inventory`；场景里被拿走或放下的物品记 `map:<id>/objects` | 事件驱动，不逐帧 | 交互；任务；物品卡（UI-VISUAL §8.12）；可拿取的东西是运行时节点，不进烘焙格子（ENG-BUDGET §7 第 3、11 条），生活痕迹照旧是静态几何（WLD-LIFE §2、WLD-INTERIORS §9） |
| NPC 与日程 | `IAgentService`（登记、日程、决策） | `game/entities/agents/`；全局数据 `game/data/npcs/`，按图的出生点与日程覆盖 `game/data/maps/<id>/entities/` | `agents`：只存偏离日程的状态，按日程推得出的不存 | 日程与决策在 `Agents`（−300）；寻路在线程池；移动在 `_PhysicsProcess` | `INavGraph`（`NpcOnly`）与 NavigationServer3D（WLD-INTERIORS §13）；软障碍（SYS-WALK §12 的泛化）；流式加载（只在已挂载的格里实例化，ENG-PERF §5）；开放室内（WLD-INTERIORS）；灯光时刻表（SYS-TIME §6，生活节律一致）；动态发声器（WLD-LIFE §6.3）与人迹声规则（SYS-AUDIO §8）；节奏规则（SYS-MOTION §2） |
| 事件与触发器 | `IEventDirector`（由 `IncidentDirector` 泛化） | `game/systems/events/`，或在 `game/systems/motion/` 里扩展；数据由 `game/data/maps/<id>/incidents.json`（§6 第 5 条）扩成事件表 | `flags`（一次性事件）；偶发事件照旧不存（SYS-SAVE §2） | 判定在 `Story`（−950）；画面表现照旧在 Motion（−650），经通道与全局参数 | SYS-MOTION §10 的触发条件与转场冷却（SYS-MOTION §12）；`MapPortal` 的触发盒（SYS-MAPS §8）；`IAppFlow`；任务；`IAmbienceService.Cue` 的批号（SYS-MOTION §10.7）；天气钉住 `Pin`（SYS-WEATHER §6.6）：剧情事件要求的天气与钉住的先后在 RPG 立项时定 |
| 本地化与文本 | `ITextService`（键 → 文本，缺键回退） | `game/data/text/<语言>/`；界面自己的翻译表照旧在 `game/hud/i18n/` | 无：语言在 `settings.cfg` | 不逐帧 | UI-NAV §8 的 `TranslationServer`；`LocalizedName`（以 init 属性加文本键）；字体与 MSDF（UI-VISUAL §11.8）：G16 不许雾幕后新生成字形，对白的字形集要离线收齐（ENG-BUDGET §7 第 9 条）；文本输入（自定义标记的备注、任务日志）：界面现在没有文本框（UI-NAV §5），`CustomPin.Note` 观光期不开放（SYS-ATLAS §4.6） |
| 存档扩展 | `ISaveService`（不变） | `game/kernel/save/`（不变） | 上面各段；手动槽 `m1`–`m9` | Save（1000） | 所有段；金档与迁移链（SYS-SAVE §3.6）；G20；`Content` 阶段内按 `Order` 恢复（SYS-SAVE §2） |
| 日历 | `ICalendar`（由 `WorldHours` 推出学年、学期、周、日期，可驱动季节） | `game/kernel/world/`（WP-34 的扩展） | `calendar`：日历原点与偏移，日期本身由 `WorldHours` 推出 | WorldState（−1000）里，与季节同一步 | SYS-TIME §1 的季节（RPG 规则集下由日历推进，换季仍是正向过渡）；SYS-TIME §6 的时刻表（加"日类型"维）；WLD-INTERIORS §9 第 10 条的时间痕迹；规则集的 `WorldControls` |

**角色资产的倾向**：人物用 VRoid Studio 制作、导出 VRM，由 `pipeline/character/` 离线转成本项目的网格、骨架与动画资源，材质换成白名单里的卡通着色器（按 ART-STYLE 的分阶明暗与描线）。运行时不用 VRM 插件，也不用 MToon。理由：

- G4 拒绝白名单目录以外的着色器文件（ENG-ARCH §2）；
- 材质只能经 `ToonSpec` 获得（ENG-BUDGET §7 第 1 条）；
- 全场材质 ID 只有 128 个（SYS-MAPS §15 风险 5）。

VRM 自带的许可信息逐文件登记，做法同音频素材（SYS-AUDIO §14）。转换路线和一个角色的帧开销由 RPG 立项时的原型验证。

**蒙皮网格接入现有管线时要补的登记**：顶点格式（ENG-PERF §2 只登记了 VF-A–VF-D）、剔除图层（ENG-PERF §4 没有角色层）、预热组合（ENG-PERF §2）；自动实例化不适用于蒙皮网格（ENG-PERF §1.1），远处 NPC 按 §5 用 MultiMesh 加顶点动画或替身。

## 5 预算与性能余量

**现状**：

| 项 | 现状 | 出处 |
| --- | --- | --- |
| 主线程余量 | 每帧主线程合计约 5.5 ms；音频的 0.35 ms 从余量里划出后，开发机只剩约 0.15 ms；最低配置档靠音频降级守住 | ENG-BUDGET §8、ENG-PERF §6 |
| 主线程上限 | CPU p95 ≤ 6 ms | ENG-BUDGET §2 |
| 总账余量（三角形 / 主通道绘制 / 阴影绘制 / GPU 帧时） | 4% / 11% / 10% / 2% | ENG-BUDGET §8 |
| 3D 声源 | 在播 ≤ 32（最低配置档默认 24） | ENG-BUDGET §7 第 21 条、SYS-AUDIO §18.1 |
| 材质 ID | 全场 128 个 | SYS-MAPS §15 |
| 安装目录与分发压缩包 | 安装目录 ≤ 0.9 GB，分发压缩包 ≤ 0.7 GB；几何类产物本机烘焙、不随包发（D-165） | ENG-GATES §2 的 G6、ENG-BUDGET §8 |

**风险与对策**（角色与 NPC 要的预算，观光版没有留）：

| 风险 | 对策 | 指向 |
| --- | --- | --- |
| 角色、NPC 的主线程开销（动画求值、骨骼姿态、AI、交互查询）没有余量 | ① 动画按一拍二步进，远处降频，屏外不求值；② AI、日程、寻路在线程池里做纯 .NET 计算（ENG-BUDGET §7 第 16 条）；③ RPG 规则集单列一套主线程账（`budgets.json` 加 `rulesets.<id>` 一节，做法同 `maps.<id>`），原型实测后重订 ENG-PERF §6 的表 | ENG-BUDGET §8、ENG-PERF §6 |
| 绘制调用与阴影绘制 | 转换时把一个角色合到 1–2 个材质；远处 NPC 用 MultiMesh 加顶点动画或替身（ENG-PERF §1.1 的容器规则）；角色只在近处投影 | ENG-PERF §1.1、ENG-PERF §10.1 |
| GPU 帧时余量只有 2% | 人数随画面设置的新单项（末尾追加的 `QualityKnob`）缩放；最低配置档默认少放人 | SYS-GRAPHICS §8、ENG-BUDGET §2 |
| 材质 ID 上限 | 角色共用白名单卡通着色器和少量材质 ID | SYS-MAPS §15 |
| 3D 声源上限 | NPC 的声音进同一个仲裁池，不另开上限 | SYS-AUDIO §2.5 |
| 显存与包体 | 角色贴图按 ENG-PERF §7 记账；VRM 原始文件不进导出包 | ENG-PERF §7、ENG-PERF §8 |
| 以上都不够 | 重订 RPG 规则集下的帧时目标（ENG-BUDGET §2），由 WP-00 报给用户决定 | ENG-BUDGET §2 |

## 6 v1 冻结前的预留

### 6.1 清单

以下 6 条已采纳（D-160），由 WP-00 写进契约 v1 候选、相关包在所在那组契约冻结前交付（ENG-ARCH §3），落实在 ENG-ARCH、SYS-MOTION、SYS-SAVE、SYS-MAPS、ENG-WP、CORE-CONVENTIONS（具体小节见末列）；契约以 ENG-ARCH §3 为准。判据：以后再补，会改变既有契约的语义、存档格式或目录归属的，才列在这里。

| # | 预留 | 内容 | 负责 | 为什么在冻结前做 | 落实在 |
| --- | --- | --- | --- | --- | --- |
| 1 | 规则集 | 契约 `RulesetId`、`WorldControlMask`、`InteractionRule`、`RulesetDescriptor`、`IRuleset`（Kernel 服务，只读；只有新游戏和读档能切换）；数据 `game/data/rulesets/sightseeing.json`。v1 字段：`Id`；`ViewModes`（Fly、Walk）；`WorldControls`（`WorldControlMask`：`Clock`、`Season`、`Weather`、`Flow`，观光为 `All`）；`UnlockPolicy`（`all-unlocked`）；`Incidents`（允许的偶发事件 ID）；`CinematicStates`（Title、Entering、Leaving）；内容规则 `Characters = false`、`Interaction = InteractionRule.None`、`ClearVoices = false`（运行时不读，门禁按它取判据）。顺序：新游戏先定规则集，再调各段的 `CreateDefault`；读档先按 meta 的 `Ruleset` 定规则集，再进 `World` 阶段 | WP-00：契约、服务（放 `game/kernel/registry/`）、schema 和规则集文件。读它的包：WP-48c（解锁策略）、WP-06 与 WP-42（`IViewControl` 拒绝不在表里的模式）、WP-35（世界控件）、WP-26（事件清单）、WP-49a（过场状态）、WP-50（读档恢复）、WP-27（门禁判据） | meta 的 `Ruleset` 字符串要有定义；不定的话，观光禁令会散成各包代码里的常量和判断，以后每一处都要改语义 | ENG-ARCH §3（规则集契约）、ENG-ARCH §2（`game/data/rulesets/`、`game/kernel/registry/`）、ENG-WP §2（WP-00 行与读规则集的各行）、SYS-ATLAS §8、SYS-TITLE §7.1、SYS-TITLE §7.4、SYS-SAVE §5.1、SYS-SAVE §5.5、ENG-GATES §3 |
| 2 | 观者状态 | 契约 `IObserverState`：观者的脚位、眼位、朝向、所在空间 `NavSpace?`、观察模式。旁观时就是镜头位置，行走时是 WP-42 的物理体。v1 读它的：小地图的玩家箭头（视锥仍读镜头）、底栏区域名、`Proximity` 的判定、行走进楼触发与门、存档 meta 的空间与区域 | WP-00（契约）；WP-06 实现（行走时取 WP-42 的数据）；读它的包：WP-48b、WP-48c、WP-07、WP-50 | 第一人称下观者与镜头重合；第三人称后两者分开，各包若直接读 `ICameraRig.Pose`，就要逐包排查、改语义。v1 只是换一个读取来源 | ENG-ARCH §3（`IObserverState`）、ENG-ARCH §2（`Campus.Hud.Map` 的依赖规则）、ENG-WP §2（WP-06 行）、SYS-ATLAS §3、SYS-ATLAS §8、SYS-ATLAS §11、SYS-SAVE §3.2、WLD-INTERIORS §4.1、WLD-INTERIORS §6.2 |
| 3 | 输入占用按原因计 | `ICameraRig` 用 `IDisposable LockInput(InputLockReason r)` 占用输入，没有任何占用时才接受观者输入；不设布尔开关。v1 的原因 `InputLockReason { Atlas, Cinematic, MapTravel, Menu, Rebind }`：大地图、开始页与过场、换图雾幕、菜单（设置卡、存档页、数据手册、确认对话框）、改键捕获；以后在末尾追加对话、剧情过场 | WP-00（契约）；WP-06 实现；调用方 WP-48c、WP-49a、WP-46、WP-35 | v1 已有五种占用：大地图（SYS-ATLAS §4.1）、开始页与过场（SYS-TITLE §7.3）、换图第 1、8 步（SYS-MAPS §4.2）、菜单与改键捕获（UI-NAV §5）。一个布尔量被几处开关会互相覆盖，RPG 还要再加几处；冻结后再改成计数，会改变已有调用的语义 | ENG-ARCH §3（相机块）、ENG-WP §2（WP-06 行）、SYS-ATLAS §4.1、SYS-ATLAS §11、SYS-TITLE §7.1、SYS-TITLE §7.3、SYS-MAPS §4.2、UI-NAV §5 |
| 4 | 枚举的穷举纪律 | 会追加值的枚举（`ViewMode`、`AppState`、`MapKind`、`MapTravelCause`、`MarkerKind`、`SaveReason`、`SlotKind`、`TransitKind`、`VeilReason`、`DuckReason`、`AudioGroup`、`FoleyKind`、`QualityKnob` 等），消费方一律写穷举的 `switch`，`default` 分支拒绝或报错；不写 `mode == ViewMode.Fly ? … : …` 这样的二值判断。读到不认识的枚举字符串，按 SYS-SAVE §3.5 当作不合法 | WP-00：写进简报（ENG-WP §4）和约定（CORE-CONVENTIONS）。G1 的 `-warnaserror` 已把非穷举 switch 表达式的警告当错误，二值判断靠评审 | "末尾追加"只有在消费方不假设闭集时才安全；二值判断会把新值静默当成旧值，编译器查不出 | CORE-CONVENTIONS §3.8、ENG-WP §4、ENG-ARCH §8（扩展方式第 1 条） |
| 5 | 偶发事件数据化 | `game/data/maps/qingshuihe/incidents.json`：事件 ID、路线实体（银杏大道南段，加转向时间广场的一段）、选树与选灯的规则引用、各版本的开关。`IncidentDirector` 只跑规则集 `Incidents` 里登记的事件；灯光表里的事件序号由同一份文件烘出 | WP-26（文件与 schema）；WP-46 在按图目录里登记；WP-39 的灯光表烘焙读它 | G4 不许系统代码出现实体 ID 字面量，路线实体要有数据文件和按图目录里的位置；数据化以后，"只有一个偶发事件"就是清单长度，加图也不改代码 | SYS-MOTION §10.2、SYS-MOTION §10.7、ENG-ARCH §2、SYS-MAPS §2.1、ENG-WP §2（WP-26 行）、CORE-CONVENTIONS §5.2 |
| 6 | 名字占位 | 目录：`game/entities/`、`game/contracts/Entities/`、`game/data/text/`、`game/data/maps/<id>/entities/` 在 `ownership.json` 里记为 WP-00 的 RPG 预留，观光期不写入。实体 ID 前缀 `npc:`、`item:`、`quest:` 保留，G5 的前缀检查拒绝观光数据使用。存档预留段名含 `calendar`、`agents`。输入动作 `interact`（E）、`inventory`（I）的名字和键位不分配给观光功能 | WP-00；SYS-SAVE的预留表；UI-NAV快捷键表不占 E、I | 名字一旦被观光的包占用，以后要改目录、改 ID、改存档段名，代价都比现在留一行大；这几项都不写代码 | ENG-ARCH §2、SYS-SAVE §2、SYS-MAPS §2.1、UI-NAV §5、ENG-GATES §2（G5）、CORE-CONVENTIONS §3.1、CORE-CONVENTIONS §5.1、CORE-CONVENTIONS §5.2 |

### 6.2 不需要现在预留的（以后只增即可）

| 项 | 以后怎么加 |
| --- | --- |
| 新观察模式（第三人称跟随） | `ViewMode` 末尾追加；规则集 `ViewModes` 加一项 |
| 对白总线、音量组、压低原因 | `AudioBuses` 加常量，`AudioGroup`、`DuckReason` 末尾追加；`AudioSettings` 以 init 属性加字段，`audio.cfg` 升 schema |
| 人群密度、角色细节 | `QualityKnob` 末尾追加（按位存，还有 37 个空位） |
| 新的帧顺序步 | 在现有空档里加常量（§4），五张表同步 |
| 交互、任务、对话、背包、NPC、事件、文本、日历服务 | 新 Kernel 服务与新契约块 |
| 新存档段 | `[SaveSection]` 登记；旧档缺段时取默认值 |
| 过场的角色轨、对白轨 | 升 `format` |
| 文本键 | `LocalizedName` 以 init 属性加 `Key`，JSON schema 只增 |
| `MapKind.Instance`、`MapTimeRule.Fixed` | WP-34、WP-46 实现，G5 放行 |
| 规则集预算 | `budgets.json` 加 `rulesets.<id>` 一节 |
| 骨骼网格的挂载 | `IRenderHost` 加方法（§7 第 10 条） |
| 剧情锁门 | 运行时门体加锁状态（WLD-INTERIORS §14） |

## 7 RPG 开工前待办

以下各项不阻塞观光版，RPG 开工前处理（编号与 ENG-ARCH §8、本文 §6.2 的引用对应）。

| # | 项 | 涉及 | 处理方向 |
| --- | --- | --- | --- |
| 10 | 角色要骨骼网格：ENG-ARCH §2 只许内核建 `MeshInstance3D`，`IRenderHost` 没有骨骼网格与动画的入口 | ENG-ARCH、ENG-PERF | RPG 时给 `IRenderHost` 加骨骼实例的方法，按 ENG-PERF §12.2 归因 |
| 11 | 角色的着色与描线：ENG-BUDGET §7 第 1 条与 ENG-PERF §1.4 的白名单；G4 拒绝白名单目录以外的着色器，VRM 插件自带的 MToon 进不了 `game/`；`LineClass`、`ShadeSteps` 没有角色档；材质 ID 上限 128 个 | ART-STYLE、ENG-ARCH、ENG-BUDGET、ENG-PERF、SYS-MAPS | 走离线转换（§4"角色资产的倾向"）；RPG 时白名单加一个蒙皮卡通着色器，`LineClass`、`ShadeSteps` 末尾追加角色档 |
| 12 | 对白字形：ENG-BUDGET §7 第 9 条要求雾幕后新生成的 MSDF 字形为 0（G16） | ENG-GATES、ENG-BUDGET、UI-VISUAL | 字形集在构建时从全部文本表收齐 |
| 13 | 镜头规则：INV-2 的"不实现环绕、俯视、导览、自动运镜"，SYS-MOTION §1.4、SYS-VIEW §2.5 的"镜头不自己动"，会挡住第三人称跟随镜头和 Playing 里的剧情过场 | CORE-INDEX、SYS-MOTION、SYS-VIEW | 观光规则集下不变；RPG 规则集另列允许的镜头行为 |
| 14 | 季节与日历：SYS-TIME §1"季节不自动推进"；SYS-TIME §6 的时刻表没有"日类型"维 | SYS-TIME | 观光规则集下不变；RPG 规则集由日历推进季节，时刻表以后加维 |
| 15 | 文本：ENG-ARCH §3 的 `LocalizedName(Zh, En)` 内联两种语言；翻译表在 WP-35 的 `game/hud/i18n/`（UI-NAV §8） | ENG-ARCH、UI-NAV、UI-VISUAL | 不阻塞 v1；RPG 时加文本键和 `game/data/text/` |
| 16 | 必须一起恢复的存档段：SYS-SAVE §7 只把 `maps`、`world`、`weather`、`map:*/weather` 当核心段，其余段不合法时各自换成默认值。`quest`、`inventory`、`flags`、`map:<id>/objects` 互相依赖，单独重置会重复领奖（任务重置、物品还在）或永久卡关（物品重置、任务还在） | SYS-SAVE、ENG-ARCH | 开工条件，由 WP-50 与首批任务包落实：① 规则集声明"必需段组"，并写明组内每段从哪个版本起必需（按存档里哪一层版本号判断由 WP-50 定，SYS-SAVE §3.6）；组内任一段损坏或不合法时，整组回退到同一世界线里最近一份完整可读的旧代，不单独换默认值；② 写档的版本早于该段起始版本的，缺段属于旧档，补默认值；不早于的，缺段是异常，按组内损坏处理；③ 任务选择、奖励发放和世界变化在同一个落定状态里完成之后才算安全时刻（SYS-SAVE §4.2），中途不拍快照。沿用现有容器、代际与回退机制（SYS-SAVE §3、SYS-SAVE §7），不另做事务框架 |
