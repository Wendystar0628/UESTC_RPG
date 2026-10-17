---
id: ENG-ROADMAP
title: 扩展路线：从观光到 RPG
category: engineering
summary: 规定观光版给以后 RPG 留的扩展原则、现在不能写死的地方和 v1 冻结前的最小预留（6 条）
keywords: [扩展路线, 从观光到 RPG, 扩展原则, 规则集, sightseeing, 观光规则集, 观者状态, 输入占用按原因计, 枚举的穷举纪律, 偶发事件数据化, 名字占位, RPG 预留目录, 实体 ID 前缀, 存档预留段名, v1 冻结前的预留, 写法纪律]
owners: [WP-00]
contributors: [WP-06, WP-07, WP-26, WP-27, WP-35, WP-39, WP-42, WP-46, WP-48b, WP-48c, WP-49a, WP-50]
depends: [CORE-INDEX, ENG-ARCH, SYS-SAVE, SYS-MAPS]
related: [ENG-WP, ENG-BUDGET, ENG-PERF, SYS-MOTION, SYS-TIME, SYS-WALK, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO]
contracts: []
data: []
gates: [G4, G5, G20]
---
# ENG-ROADMAP · 扩展路线：从观光到 RPG

> **范围**：本文只规定架构上的扩展原则、现在不能写死什么、写法纪律，以及 v1 冻结前的 6 条最小预留。不设计 RPG 玩法。
> **不在本文**：RPG 子系统挂点、预算余量与开工待办（RPG 开工时按当时的代码重写）；契约签名与冻结（ENG-ARCH §3）；各系统细则（各系统文档）；预算数值（ENG-BUDGET）；工作包划分（ENG-WP）。契约名、数据路径以 `game/contracts/` 源码与各文档为准。
> **查阅**：先读 §0；扩展原则看 §1；写死的规定与写法纪律看 §2；v1 冻结前的预留看 §3

## 0 结论

1. 观光是 RPG 的子集：同一套世界、渲染、地图服务、存档和音频；观光是关掉一部分功能的规则集 `sightseeing`，RPG 是另一个规则集，不另做一套引擎（§1）。
2. 新功能只用四种方式进来：加服务、加数据、加存档段、在枚举末尾追加值；不改既有契约的语义，不加 Autoload（§1）。
3. 观光的禁令是配置，不是代码；规则集化只改规则写在哪，观光规则一条不放松（§1、§2）。
4. 写法纪律现在就照做：不写观光专属常量，分清观者与镜头，枚举不做二值判断，换世界实例时直接落到新值（§2）。
5. v1 冻结前只做 6 条预留（D-160）：规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位；判据是以后再补会改变既有契约的语义、存档格式或目录归属（§3）。
6. 除这 6 条外不为 RPG 预留任何东西，其余以后只增即可（§3.2）。

### 0.1 接入要点

- **观光规则**（解锁策略名、偶发事件 ID、允许的观察模式）：读规则集，不在代码里写常量（§2、§3.1 第 1 条）
- **要知道观者在哪**：读 `IObserverState`；"画面从哪看"（视锥、听者、流式选级）读 `ICameraRig`（§3.1 第 2 条）
- **要临时接管输入**：经 `ICameraRig.LockInput(InputLockReason)` 占用，不设布尔开关（§3.1 第 3 条）
- **消费会追加值的枚举**：写穷举 `switch`，`default` 拒绝或报错，不写二值判断（§3.1 第 4 条）
- **做平滑、带回差或保留历史的系统**：读档、新游戏换世界实例时直接落到新值（§2）
- **加新功能**：只用加服务、加数据、加存档段、枚举末尾追加；不加 Autoload（§1）
- **碰到 RPG 预留的名字**：`game/entities/` 等预留目录观光期不写入；`npc:`、`item:`、`quest:` 前缀、存档预留段名、E、I 两个键和 `interact`、`inventory` 动作名不给观光功能用（§3.1 第 6 条）

## 1 扩展原则

以后的方向是 RPG，主线是"入学到毕业"。观光版不做任何 RPG 内容，只保证 RPG 进来时不用推翻现有架构。**规则集**是决定"这一局按什么规则运行"的一组配置；观光是规则集 `sightseeing`，存档 meta 的 `Ruleset` 字段记它（SYS-SAVE）。

1. **观光是 RPG 的子集**：同一套世界、渲染、地图服务、存档、音频。
2. **新功能只用四种方式进来**：加服务（`[KernelService]`、`[KernelNode]`）、加数据、加存档段（`[SaveSection]`）、加枚举值（末尾追加）。不改既有契约的语义，不加 Autoload（ENG-ARCH §2、§3）。
3. **观光的禁令是配置，不是代码**：运行时代码读规则集，门禁按规则集取判据。
4. **观光规则一条不放松**：INV-1–INV-12 与 ENG-GATES-VETO §1 的一票否决在观光规则集下照旧；扩展点只留口子，不提前做 RPG 内容。
5. **确定性和单调时间照旧**：剧情、NPC、事件的随机走 `StableHash` 与 `Rng`；世界时间在一个实例内单调，读档换实例。
6. **预留越少越好**：只有"以后再补会改变既有契约语义、存档格式或目录归属"的才在 v1 冻结前预留（§3）。

## 2 写死的规定与写法纪律

观光版现行的规定全部保留，看作规则集 `sightseeing` 的内容：不出现 NPC 与他人、没有可点选的东西、全片一个偶发事件、只有旁观和行走两种观察模式、镜头不自己动、过场只在 Title、Entering、Leaving、时间季节天气随意调、解锁策略 `all-unlocked`（INV-1–INV-12，D-182）。内容规则写成规则集字段 `Characters`、`Interaction`、`ClearVoices`（运行时不读，门禁按它取判据）。其余写死的规定（过场无角色轨、碰撞不随状态变、全场一盏方向光、不画车流、外围不能进、界面只有登记的件、音乐只在开始页与进场过场等），以后只增即可，不需要预留，各自的规则在对应文档。

**写法纪律**（现在就照做，不花额外代码）：不在代码里写观光专属常量，一律读规则集；"观者在哪"读 `IObserverState`，"画面从哪看"读 `ICameraRig`；会追加值的枚举不做二值判断；读档、新游戏换世界实例时，平滑、回差、历史类系统直接落到新值，以后的 NPC、剧情状态同样遵守。

## 3 v1 冻结前的预留

### 3.1 清单

以下 6 条已采纳（D-160），由 WP-00 写进契约 v1 候选；契约以 `game/contracts/` 为准。

1. **规则集**：契约 `RulesetId`、`WorldControlMask`、`InteractionRule`、`RulesetDescriptor`、`IRuleset`（Kernel 服务，只读，只有新游戏和读档能切换）；数据 `game/data/rulesets/sightseeing.json`，字段含 `ViewModes`、`WorldControls`、`UnlockPolicy`、`Incidents`、`CinematicStates` 与内容规则。新游戏先定规则集再调各段 `CreateDefault`；读档先按 meta 的 `Ruleset` 定规则集再进 `World` 阶段。
2. **观者状态**：契约 `IObserverState`（脚位、眼位、朝向、所在空间、观察模式）；旁观时是镜头位置，行走时是行走控制器的物理体。第一人称下观者与镜头重合，第三人称后分开。
3. **输入占用按原因计**：`ICameraRig.LockInput(InputLockReason)` 返回 `IDisposable`，没有占用时才接受观者输入，不设布尔开关；原因有大地图、开始页与过场、换图雾幕、菜单、改键捕获，以后末尾追加。
4. **枚举的穷举纪律**：会追加值的枚举，消费方一律写穷举 `switch`，`default` 拒绝或报错；读到不认识的枚举字符串当作不合法（CORE-CONVENTIONS §3.6、ENG-ARCH §3）。
5. **偶发事件数据化**：`game/data/maps/qingshuihe/incidents.json` 记事件 ID、路线实体、选树选灯规则、各版本开关；`IncidentDirector` 只跑规则集 `Incidents` 里的事件。
6. **名字占位**：目录 `game/entities/`、`game/contracts/Entities/`、`game/data/text/`、`game/data/maps/<id>/entities/` 在 `ownership.json` 记为 RPG 预留，观光期不写入；实体 ID 前缀 `npc:`、`item:`、`quest:` 保留，G5 拒绝观光数据使用；存档预留段名含 `calendar`、`agents`；`interact`（E）、`inventory`（I）的名字和键位不分配给观光功能。

### 3.2 不需要现在预留的

以后只增即可：新观察模式（`ViewMode` 末尾追加）；新帧步（`FramePriority` 追加常量）；新服务与契约块（交互、任务、对话、背包、NPC、文本、日历）；新存档段；过场的角色轨；`MapKind.Instance`；规则集预算。
