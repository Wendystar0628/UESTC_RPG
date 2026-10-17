---
id: ENG-ROADMAP
title: 扩展路线：从观光到 RPG
category: engineering
summary: 规定观光版给以后 RPG 留的扩展点、现在不能写死的地方和 v1 冻结前的最小预留
keywords: [扩展路线, 从观光到 RPG, 扩展点, 扩展原则, 规则集, sightseeing, 观光规则集, 观者状态, 输入占用按原因计, 枚举的穷举纪律, 偶发事件数据化, 名字占位, RPG 预留目录, 实体 ID 前缀, 存档预留段名, v1 冻结前的预留, 写法纪律, RPG 子系统挂点, VRM, 必需段组]
owners: [WP-00]
contributors: [WP-06, WP-07, WP-26, WP-27, WP-35, WP-39, WP-42, WP-46, WP-48b, WP-48c, WP-49a, WP-50]
depends: [CORE-INDEX, ENG-ARCH, SYS-SAVE, SYS-MAPS]
related: [ENG-WP, ENG-BUDGET, ENG-PERF, SYS-MOTION, SYS-TIME, SYS-WALK, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO]
contracts: []
data: []
gates: [G4, G5, G20]
---
# ENG-ROADMAP · 扩展路线：从观光到 RPG

> **范围**：本文只规定架构上的扩展点和"现在不能写死什么"：观光版已经留了哪些口子，哪些观光规则要做成规则集的配置，v1 冻结前必须加的最小预留。不设计 RPG 玩法。
> **不在本文**：契约签名与冻结（ENG-ARCH §3）；各系统细则（各系统文档）；预算数值（ENG-BUDGET）；工作包划分（ENG-WP）。契约名、数据路径以 `game/contracts/` 源码与各文档为准。
> **查阅**：先读 §0；扩展原则看 §1；已留的扩展点看 §2；现在写死的规定与写法纪律看 §3；RPG 子系统挂点看 §4；预算与风险看 §5；v1 冻结前的预留看 §6；RPG 开工前待办看 §7

## 0 结论

1. 观光是 RPG 的子集：同一套世界、渲染、地图服务、存档和音频；观光是关掉一部分功能的规则集 `sightseeing`，RPG 是另一个规则集，不另做一套引擎（§1）。
2. 新功能只用四种方式进来：加服务、加数据、加存档段、在枚举末尾追加值；不改既有契约的语义，不加 Autoload（§1）。
3. 观光的禁令是配置，不是代码：无人物、无点选、一个偶发事件、两种观察模式、过场只在开始页前后，都写进规则集；规则集化只改规则写在哪，观光规则一条不放松（§1、§3）。
4. 观光版已留好的扩展点列在 §2；现在写死的规定在观光版全部保留，看作规则集的内容，需要最小预留的见 §3 末列。
5. 写法纪律现在就照做：不写观光专属常量，分清观者与镜头，枚举不做二值判断，换世界实例时直接落到新值（§3）。
6. RPG 子系统只定挂点，服务名和帧顺序值是草案；人物走 VRM 离线转换，运行时不用 VRM 插件和 MToon（§4）。
7. 观光版没给角色与 NPC 留预算；RPG 规则集在 `budgets.json` 里单列一套账，不挤占观光各包的额度（§5）。
8. v1 冻结前只做 6 条预留（D-160）：规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位；判据是以后再补会改变既有契约的语义、存档格式或目录归属（§6）。

### 0.1 接入要点

- **观光规则**（解锁策略名、偶发事件 ID、允许的观察模式）：读规则集，不在代码里写常量（§3、§6.1 第 1 条）
- **要知道观者在哪**：读 `IObserverState`；"画面从哪看"（视锥、听者、流式选级）读 `ICameraRig`（§6.1 第 2 条）
- **要临时接管输入**：经 `ICameraRig.LockInput(InputLockReason)` 占用，不设布尔开关；没有占用时才接受观者输入（§6.1 第 3 条）
- **消费会追加值的枚举**：写穷举 `switch`，`default` 拒绝或报错，不写二值判断（§6.1 第 4 条）
- **做平滑、带回差或保留历史的系统**：读档、新游戏换世界实例时直接落到新值（§3）
- **加新功能**：只用加服务、加数据、加存档段、枚举末尾追加；不加 Autoload（§1）
- **碰到 RPG 预留的名字**：`game/entities/` 等预留目录观光期不写入；`npc:`、`item:`、`quest:` 前缀、存档预留段名、E、I 两个键和 `interact`、`inventory` 动作名不给观光功能用（§6.1 第 6 条）
- **加 `FramePriority` 常量**：契约源码与引用它的文档表同步改（§4）

## 1 扩展原则

以后的方向是 RPG，主线是"入学到毕业"。观光版不做任何 RPG 内容，只保证 RPG 进来时不用推翻现有架构。**规则集**是决定"这一局按什么规则运行"的一组配置；观光是规则集 `sightseeing`，存档 meta 的 `Ruleset` 字段记它（SYS-SAVE）。

1. **观光是 RPG 的子集**：同一套世界、渲染、地图服务、存档、音频，不做"RPG 版引擎"。
2. **新功能只用四种方式进来**：加服务（`[KernelService]`、`[KernelNode]`）、加数据、加存档段（`[SaveSection]`）、加枚举值（末尾追加）。不改既有契约的语义，不加 Autoload（ENG-ARCH §2、§3）。
3. **观光的禁令是配置，不是代码**：运行时代码读规则集，门禁按规则集取判据。
4. **观光规则一条不放松**：INV-1–INV-12 与 ENG-GATES §3 的一票否决在观光规则集下照旧；扩展点只留口子，不提前做 RPG 内容。
5. **确定性和单调时间照旧**：剧情、NPC、事件的随机走 `StableHash` 与 `Rng`；世界时间在一个实例内单调，读档换实例。
6. **预算按账本申请**：RPG 子系统是新的工作包，进 `budgets.json`，不挤占观光各包额度。
7. **预留越少越好**：只有"以后再补会改变既有契约语义、存档格式或目录归属"的才在 v1 冻结前预留（§6）。

## 2 现有扩展点

契约与类型名以 `game/contracts/` 为准。

| 方向 | 现状与以后怎么接 | 依据 |
| --- | --- | --- |
| 多地图 | 只有 `qingshuihe`；按图数据在 `game/data/maps/<id>/`。新场景按 SYS-MAPS 的加图步骤加，不改契约和流式架构；邻图入口启用 `MapPortal` | SYS-MAPS §9 |
| 图的种类与时间规则 | 只有 `Outdoor`、`Shared`；`Interior`、`Instance` 与 `MapTimeRule.Fixed` 已预留（`Fixed` 现在被拒绝）；副本式小场景用 `Instance` + `Fixed` | SYS-MAPS §9 |
| 存档段与世界实例 | 未知段原样保留，缺段补默认值，每段独立版本与迁移链；预留段名 `quest`、`inventory`、`character`、`flags`、`map:<id>/objects`、手动槽、`SaveReason.Checkpoint`。RPG 只加段，不改容器 | SYS-SAVE §2、SYS-SAVE §3.4 |
| 解锁策略 | 现在只启用 `all-unlocked`；`proximity` 与揭示规则已写好，RPG 规则集选它 | SYS-ATLAS §8、§11 |
| 标记与图层 | `MarkerKind.Quest`、`AtlasLayer`、`TravelRefusal.Locked` 已预留，任务服务产出 Quest 标记 | SYS-ATLAS §5、§9 |
| 开放室内与生成器 | 加楼只改 `interiors.json` 并注册提供者；会动的门是运行时门体，剧情锁门在上面加锁状态；开放状态随剧情变时，G19c 按运行时状态核对 | WLD-INTERIORS §11、WLD-INTERIORS §3.1 |
| 导航图 | 烘焙的分层图，`NavFlags` 有 `NpcOnly`、`PlayerOnly`、`Locked`；NPC 宏观寻路用 `INavGraph`，局部用 NavigationServer3D | WLD-INTERIORS §11 |
| 偶发事件 | `IncidentDirector` 只跑规则集 `Incidents` 里登记的事件；泛化为事件导演后多个事件登记在数据里 | SYS-MOTION §10 |
| 音频 | `AudioBuses` 常量与相关枚举只增；动态发声器供 NPC；对白另加总线、音量组与压低原因 | SYS-AUDIO §2、ENG-ARCH §3 |
| 界面预留 | 对话条、任务列表、物品卡的视觉已定；RPG 页面放 `game/hud/pages/`，用 UI-VISUAL 的组件拼 | UI-VISUAL §8 |
| 过场数据 | `format` 只增，遇到不认识的轨报错；加角色轨、对白轨时升 `format` | SYS-TITLE §8 |
| 画面设置与预算 | `QualityKnob` 末尾追加、按位存；人群密度、角色细节作为新单项；RPG 包按账本申请额度 | ENG-ARCH §3、ENG-BUDGET §8 |
| 服务与世界时间 | 模块打特性即接入，Autoload 只有两个；日历由 `WorldHours` 推出，不另起时钟 | ENG-ARCH §2、SYS-TIME §1 |
| 剧情显隐 | 物体显隐只看季节、时段、天气掩码；以后掩码加剧情标志位，内核按 `IStoryState` 显隐 | ENG-ARCH §8 |
| 开局 | 新游戏取本图默认状态；RPG 规则集给起始图、出生点、开局时刻与季节，先定规则集再调各段 `CreateDefault` | SYS-TITLE §6、SYS-MAPS §3.4 |
| 工作包 | RPG 包从 WP-60 起编号，WP-54 至 WP-59 留给本阶段新增的包；每个子系统一个包、独占一个目录 | ENG-WP |

## 3 现在写死、以后会挡路的地方

左列的规定在观光版里全部保留，看作 `sightseeing` 规则集的内容。"预留"一列写"无"的，以后只增即可。

| # | 现在的规定 | 以后怎么扩 | 预留 |
| --- | --- | --- | --- |
| 1 | 不出现 NPC 与他人；旁观观者没有身体；行走有玩家角色本体（D-182）；展陈无人物肖像；过场无人；无听得清的人声（INV-6，ENG-GATES §3 第 2、24 条） | RPG 规则集打开角色，由实体层负责；对白走单独总线 | 规则集内容规则 `Characters`、`ClearVoices`；实体层占位（§6 第 1、6 条） |
| 2 | 没有可点选的东西，没有准星；3D 不叠加世界空间标记；会动的门走近自动推开（INV-1、INV-3） | 近距离交互：交互键加界面提示，不恢复鼠标拾取 | `Interaction = None`；E、I 不分配（§6 第 6 条） |
| 3 | 全片只有一个偶发事件（INV-10） | 事件导演登记多个事件 | 偶发事件数据化（§6 第 1、5 条） |
| 4 | 只有旁观和行走两种观察模式（INV-2） | `ViewMode` 末尾追加 | 规则集 `ViewModes`；穷举纪律（§6 第 4 条） |
| 5 | 镜头不自己动；过场只在 Title、Entering、Leaving（INV-2） | Playing 里的剧情过场与对话镜头；谁占用输入与镜头按原因计 | 输入占用按原因计；规则集 `CinematicStates`（§6 第 3 条） |
| 6 | 第一人称：观者与镜头重合 | 第三人称下"观者在哪"和"画面从哪看"分开 | `IObserverState`（§6 第 2 条） |
| 7 | 时间、季节、天气、流逝随意调；季节不自动推进（INV-8） | RPG 用日历推进季节，部分世界控件变灰 | 规则集 `WorldControls`；预留段名 `calendar`（§6 第 1、6 条） |
| 8 | 解锁策略是 `all-unlocked` | RPG 选 `proximity` | 取自规则集 `UnlockPolicy` |

以下写死的规定（各自的规则在对应文档）以后只增即可，不需要预留：

- 过场没有角色轨和配音，升 `format` 加轨。
- 碰撞与可走性不随状态变；剧情锁门只走运行时门体的锁状态，不改烘焙碰撞。
- 静态内容全部烘焙进格子，生活痕迹是不可拿取的几何；可拿取物是运行时节点（`[KernelNode]`），场景物体变化记在 `map:<id>/objects` 差分。
- 动态物体不参与碰撞、不推角色，动物自己让路（允许不超过 0.3 s 的穿插）；NPC 沿用同一份感知快照与让路规则。
- 只有一个观者，观者声与 `IWalkTelemetry` 都指玩家自己；NPC 脚步走 3D 发声器。
- 文字只有中英两种，内联在 `LocalizedName`；雾幕后不许新生成 MSDF 字形；以后文本键与文本库，对白字形集构建时收齐。
- 全场一盏方向光，OmniLight3D ≤ 32 盏且不投影，内容包不得新增光源、SubViewport、全屏通道；以后手持光源等作为新类别，额度按规则集单列。
- 不画车流和行驶的车，电车静止；以后动态交通层作为运行时层加入。
- 外围能看不能进，没有碰撞，不能传送，行走坐标按不超过 1.02 km 定单精度方案；以后分段开放外围（WLD-OUTSKIRTS §8.2），超出范围另定精度方案。
- 界面只有登记的件，没有任务提示、教学、成就；以后 RPG 规则集扩展白名单，新常驻件按 UI-VISUAL 的组件与深屏规则加入。
- 音乐只在开始页与进场过场，`MusicCue` 只有 5 项；漫游音乐由规则集打开，末尾追加。
- 观者没有坠落伤害和生命值，动作声只有脚步、落地、碰撞、涉水和风声；RPG 规则集定伤害与回位，`FoleyKind` 末尾追加。

**写法纪律**（现在就照做，不花额外代码）：不在代码里写观光专属常量，一律读规则集；"观者在哪"读 `IObserverState`，"画面从哪看"读 `ICameraRig`；会追加值的枚举不做二值判断；读档、新游戏换世界实例时，平滑、回差、历史类系统直接落到新值，以后的 NPC、剧情状态同样遵守。

## 4 RPG 可能新增的子系统与挂点

只写挂在哪里，不写玩法。服务名与帧顺序值都是草案，以 ENG-ARCH 与契约 `FramePriority` 为准；新加常量时，引用它的文档表同步改。

**草案帧步**（放在现有步的空档里）：`Story`（−950，WorldState 之后、KeyLight 之前，剧情改世界状态当帧生效）；`Agents`（−300，Stream 之后、Systems 之前）；`Animate`（20，Systems 之后、Audio 之前，动画驱动的脚步声当帧进仲裁）。

| 子系统 | 服务（草案） | 目录 | 存档段 |
| --- | --- | --- | --- |
| 角色与动画 | `ICharacterService`；玩家位置仍经 `IObserverState` | `game/entities/character/`、`pipeline/character/` | `character` |
| 交互 | `IInteractionService` | `game/systems/interaction/` | 无，结果写进目标自己的段或 `flags` |
| 任务与剧情 | `IQuestService`、`IStoryState` | `game/systems/quest/`、`game/data/quests/` | `quest`、`flags` |
| 对话 | `IDialogueService` | `game/systems/dialogue/` | `flags` |
| 背包与物品 | `IInventoryService`（可拿取物是运行时节点，不进烘焙格子） | `game/systems/inventory/` | `inventory`、`map:<id>/objects` |
| NPC 与日程 | `IAgentService`（只在已挂载的格里实例化） | `game/entities/agents/`、`game/data/npcs/` | `agents`，只存偏离日程的状态 |
| 事件与触发器 | `IEventDirector`（由 `IncidentDirector` 泛化） | `game/systems/events/` | `flags`，偶发事件照旧不存 |
| 本地化与文本 | `ITextService` | `game/data/text/<语言>/` | 无 |
| 日历 | `ICalendar`（由 `WorldHours` 推出，可驱动季节） | `game/kernel/world/` | `calendar` |

**角色资产**：人物用 VRoid Studio 制作、导出 VRM，由 `pipeline/character/` 离线转成本项目的网格、骨架与动画，材质换成白名单卡通着色器；运行时不用 VRM 插件和 MToon（G4 拒绝白名单目录以外的着色器，材质只能经 `ToonSpec`，材质 ID 全场 128 个）。VRM 许可逐文件登记，做法同音频素材。蒙皮网格要补登记顶点格式、剔除图层、预热组合，远处 NPC 用 MultiMesh 加顶点动画或替身（ENG-PERF）。

## 5 预算与性能余量

主线程余量、总账余量、3D 声源、材质 ID、包体上限的现状以 ENG-BUDGET §8 与 `budgets.json` 为准，当前都所剩不多。角色与 NPC 的开销观光版没有留预算，对策：

- 动画按一拍二步进，远处降频，屏外不求值；AI、日程、寻路在线程池里做纯 .NET 计算。
- RPG 规则集在 `budgets.json` 单列一套账（`rulesets.<id>`，做法同 `maps.<id>`），原型实测后重订帧时表。
- 一个角色合到 1–2 个材质；远处 NPC 用 MultiMesh 加顶点动画或替身；角色只在近处投影；人数随画面设置的新单项缩放。
- NPC 声音进同一个仲裁池，不另开上限；角色共用白名单卡通着色器和少量材质 ID；VRM 原始文件不进导出包。
- 以上都不够：重订 RPG 规则集下的帧时目标，由 WP-00 报给用户决定。

## 6 v1 冻结前的预留

### 6.1 清单

以下 6 条已采纳（D-160），由 WP-00 写进契约 v1 候选；契约以 `game/contracts/` 为准。判据：以后再补会改变既有契约的语义、存档格式或目录归属的，才列在这里。

1. **规则集**：契约 `RulesetId`、`WorldControlMask`、`InteractionRule`、`RulesetDescriptor`、`IRuleset`（Kernel 服务，只读，只有新游戏和读档能切换）；数据 `game/data/rulesets/sightseeing.json`，字段含 `ViewModes`、`WorldControls`、`UnlockPolicy`、`Incidents`、`CinematicStates` 与内容规则 `Characters`、`Interaction`、`ClearVoices`（运行时不读，门禁按它取判据）。新游戏先定规则集再调各段 `CreateDefault`；读档先按 meta 的 `Ruleset` 定规则集再进 `World` 阶段。不定的话观光禁令会散成各包的常量，以后每一处都要改语义。读它的包：WP-48c、WP-06、WP-42、WP-35、WP-26、WP-49a、WP-50、WP-27。
2. **观者状态**：契约 `IObserverState`（脚位、眼位、朝向、所在空间、观察模式）；旁观时是镜头位置，行走时是 WP-42 的物理体；WP-06 实现。第一人称下观者与镜头重合，第三人称后分开，各包若直接读 `ICameraRig.Pose` 就要逐包排查。
3. **输入占用按原因计**：`ICameraRig.LockInput(InputLockReason)` 返回 `IDisposable`，没有占用时才接受观者输入，不设布尔开关；原因有大地图、开始页与过场、换图雾幕、菜单、改键捕获，以后末尾追加。布尔量被几处开关会互相覆盖，冻结后改成计数会改变已有调用的语义。
4. **枚举的穷举纪律**：会追加值的枚举，消费方一律写穷举 `switch`，`default` 拒绝或报错，不写二值判断；读到不认识的枚举字符串当作不合法。写进 CORE-CONVENTIONS §3.6 与 ENG-ARCH §0.1；G1 的 `-warnaserror` 已把非穷举 switch 表达式当错误，二值判断靠评审。
5. **偶发事件数据化**：`game/data/maps/qingshuihe/incidents.json` 记事件 ID、路线实体、选树选灯规则、各版本开关；`IncidentDirector` 只跑规则集 `Incidents` 里的事件；WP-26 负责文件与 schema，WP-39 的灯光表烘焙读它。数据化以后"只有一个偶发事件"就是清单长度。
6. **名字占位**：目录 `game/entities/`、`game/contracts/Entities/`、`game/data/text/`、`game/data/maps/<id>/entities/` 在 `ownership.json` 记为 RPG 预留，观光期不写入；实体 ID 前缀 `npc:`、`item:`、`quest:` 保留，G5 拒绝观光数据使用；存档预留段名含 `calendar`、`agents`；`interact`（E）、`inventory`（I）的名字和键位不分配给观光功能。名字一旦被观光占用，以后改目录、ID、段名的代价都比现在留一行大。

### 6.2 不需要现在预留的

以后只增即可：新观察模式（`ViewMode` 末尾追加）；对白总线、音量组、压低原因；人群密度与角色细节（`QualityKnob` 末尾追加）；新帧步；交互、任务、对话、背包、NPC、事件、文本、日历服务（新 Kernel 服务与契约块）；新存档段；过场的角色轨、对白轨；文本键；`MapKind.Instance`、`MapTimeRule.Fixed`；规则集预算；骨骼网格挂载（`IRenderHost` 加方法）；剧情锁门（运行时门体加锁状态）。

## 7 RPG 开工前待办

以下各项不阻塞观光版，RPG 开工前处理（编号与 ENG-ARCH §8 对应）。

- **10 骨骼网格**：只许内核建 `MeshInstance3D`，`IRenderHost` 没有骨骼实例入口；RPG 时加方法，按 ENG-PERF 归因。
- **11 角色着色与描线**：白名单不含 MToon；`LineClass`、`ShadeSteps` 没有角色档；走 §4 的离线转换，RPG 时白名单加蒙皮卡通着色器，两个枚举末尾追加角色档。
- **12 对白字形**：雾幕后新生成 MSDF 字形必须为 0（G16），字形集在构建时从全部文本表收齐。
- **13 镜头规则**：INV-2 与"镜头不自己动"会挡住第三人称跟随和 Playing 里的剧情过场；观光规则集下不变，RPG 规则集另列允许的镜头行为。
- **14 季节与日历**：时刻表没有"日类型"维；观光下不变，RPG 由日历推进季节，时刻表以后加维。
- **15 文本**：`LocalizedName(Zh, En)` 内联两种语言；不阻塞 v1，RPG 时加文本键和 `game/data/text/`。
- **16 必须一起恢复的存档段**：`quest`、`inventory`、`flags`、`map:<id>/objects` 互相依赖，单独重置会重复领奖或永久卡关。开工条件（WP-50 与首批任务包落实）：规则集声明"必需段组"及各段起始版本；组内任一段损坏时整组回退到同一世界线里最近一份完整可读的旧代，不单独换默认值；写档版本早于起始版本的缺段补默认值，否则按组内损坏处理；任务选择、奖励与世界变化在同一落定状态里完成后才算安全时刻，中途不拍快照。沿用现有容器、代际与回退机制（SYS-SAVE），不另做事务框架。
