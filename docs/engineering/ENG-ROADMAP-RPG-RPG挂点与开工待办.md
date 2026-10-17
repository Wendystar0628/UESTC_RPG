---
id: ENG-ROADMAP-RPG
title: RPG 挂点与开工待办
category: engineering
parent: ENG-ROADMAP
summary: 观光版已留的扩展点、RPG 可能新增的子系统与挂点、预算余量的对策、RPG 开工前的待办
keywords: [现有扩展点, RPG 子系统挂点, 草案帧步, 角色资产, VRM, 骨骼网格, 必需段组, NPC 预算, 规则集预算, RPG 开工待办]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX, ENG-ROADMAP, ENG-ARCH, SYS-SAVE, SYS-MAPS]
related: [ENG-WP, ENG-BUDGET, ENG-PERF, SYS-MOTION, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO, UI-VISUAL]
contracts: []
data: []
gates: []
---
# ENG-ROADMAP-RPG · RPG 挂点与开工待办

> **范围**：观光版已留的扩展点（§1）、RPG 可能新增的子系统与挂点（§2）、预算与性能余量（§3）、RPG 开工前待办（§4）。只写挂在哪里，不写玩法。
> **不在本文**：扩展原则、写死的规定、写法纪律、v1 冻结前的预留（ENG-ROADMAP）；契约签名（ENG-ARCH §3）；预算数值（ENG-BUDGET）。契约名、数据路径以 `game/contracts/` 源码与各文档为准。
> **查阅**：先读 §0；已留的扩展点看 §1；子系统挂点看 §2；预算余量看 §3；开工前待办看 §4

## 0 结论

1. **扩展点已留**：多地图、存档段、解锁策略、开放室内、导航图、偶发事件、音频、界面、过场、画面设置、服务、工作包都有现成的接口，RPG 只加不改（§1）。
2. **RPG 子系统只定挂点**：服务名与帧顺序值是草案；人物走 VRM 离线转换，运行时不用 VRM 插件和 MToon（§2）。
3. **预算单列**：观光版没给角色与 NPC 留预算，RPG 规则集在 `budgets.json` 里单列一套账，不挤占观光各包额度（§3）。
4. **开工前待办 7 项**，不阻塞观光版（§4）。

### 0.1 接入要点

- **加 `FramePriority` 常量**：契约源码与引用它的文档表同步改（§2）

## 1 现有扩展点

契约与类型名以 `game/contracts/` 为准。

| 方向 | 现状与以后怎么接 | 依据 |
| --- | --- | --- |
| 多地图 | 只有 `qingshuihe`；按图数据在 `game/data/maps/<id>/`。新场景按 SYS-MAPS 的加图步骤加，不改契约和流式架构；邻图入口启用 `MapPortal` | SYS-MAPS §9 |
| 图的种类与时间规则 | 只有 `Outdoor`、`Shared`；`Interior`、`Instance` 与 `MapTimeRule.Fixed` 已预留（`Fixed` 现在被拒绝）；副本式小场景用 `Instance` + `Fixed` | SYS-MAPS §9 |
| 存档段与世界实例 | 未知段原样保留，缺段补默认值，每段独立版本与迁移链；预留段名 `quest`、`inventory`、`character`、`flags`、`map:<id>/objects`、手动槽、`SaveReason.Checkpoint`。RPG 只加段，不改容器 | SYS-SAVE §2、SYS-SAVE-FORMAT §1.4 |
| 解锁策略 | 现在只启用 `all-unlocked`；`proximity` 与揭示规则已写好，RPG 规则集选它 | SYS-ATLAS-TRAVEL §4、SYS-ATLAS-UI §4 |
| 标记与图层 | `MarkerKind.Quest`、`AtlasLayer`、`TravelRefusal.Locked` 已预留，任务服务产出 Quest 标记 | SYS-ATLAS-TRAVEL §1、SYS-ATLAS §2 |
| 开放室内与生成器 | 加楼只改 `interiors.json` 并注册提供者；会动的门是运行时门体，剧情锁门在上面加锁状态；开放状态随剧情变时，G19c 按运行时状态核对 | WLD-INTERIORS-ACCESS §4、WLD-INTERIORS-ACCESS §2.1 |
| 导航图 | 烘焙的分层图，`NavFlags` 有 `NpcOnly`、`PlayerOnly`、`Locked`；NPC 宏观寻路用 `INavGraph`，局部用 NavigationServer3D | WLD-INTERIORS-ACCESS §4 |
| 偶发事件 | `IncidentDirector` 只跑规则集 `Incidents` 里登记的事件；泛化为事件导演后多个事件登记在数据里 | SYS-MOTION-LIFE §3 |
| 音频 | `AudioBuses` 常量与相关枚举只增；动态发声器供 NPC；对白另加总线、音量组与压低原因 | SYS-AUDIO §2、ENG-ARCH §3 |
| 界面预留 | 对话条、任务列表、物品卡的视觉已定；RPG 页面放 `game/hud/pages/`，用 UI-VISUAL 的组件拼 | UI-VISUAL-COMPONENTS §5 |
| 过场数据 | `format` 只增，遇到不认识的轨报错；加角色轨、对白轨时升 `format` | SYS-TITLE-CINEMATIC §5 |
| 画面设置与预算 | `QualityKnob` 末尾追加、按位存；人群密度、角色细节作为新单项；RPG 包按账本申请额度 | ENG-ARCH §3、ENG-BUDGET §3 |
| 服务与世界时间 | 模块打特性即接入，Autoload 只有两个；日历由 `WorldHours` 推出，不另起时钟 | ENG-ARCH §2、SYS-TIME §1 |
| 剧情显隐 | 物体显隐只看季节、时段、天气掩码；以后掩码加剧情标志位，内核按 `IStoryState` 显隐 | ENG-ARCH §7 |
| 开局 | 新游戏取本图默认状态；RPG 规则集给起始图、出生点、开局时刻与季节，先定规则集再调各段 `CreateDefault` | SYS-TITLE-CINEMATIC §4、SYS-MAPS §3.4 |
| 工作包 | RPG 包从 WP-60 起编号，WP-54 至 WP-59 留给本阶段新增的包；每个子系统一个包、独占一个目录 | ENG-WP |

## 2 RPG 可能新增的子系统与挂点

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

## 3 预算与性能余量

主线程余量、总账余量、3D 声源、材质 ID、包体上限的现状以 ENG-BUDGET §3 与 `budgets.json` 为准，当前都所剩不多。角色与 NPC 的开销观光版没有留预算，对策：

- 动画按一拍二步进，远处降频，屏外不求值；AI、日程、寻路在线程池里做纯 .NET 计算。
- RPG 规则集在 `budgets.json` 单列一套账（`rulesets.<id>`，做法同 `maps.<id>`），原型实测后重订帧时表。
- 一个角色合到 1–2 个材质；远处 NPC 用 MultiMesh 加顶点动画或替身；角色只在近处投影；人数随画面设置的新单项缩放。
- NPC 声音进同一个仲裁池，不另开上限；角色共用白名单卡通着色器和少量材质 ID；VRM 原始文件不进导出包。
- 以上都不够：重订 RPG 规则集下的帧时目标，由 WP-00 报给用户决定。

## 4 RPG 开工前待办

以下各项不阻塞观光版，RPG 开工前处理（编号与 ENG-ARCH §7 对应）。

- **10 骨骼网格**：只许内核建 `MeshInstance3D`，`IRenderHost` 没有骨骼实例入口；RPG 时加方法，按 ENG-PERF 归因。
- **11 角色着色与描线**：白名单不含 MToon；`LineClass`、`ShadeSteps` 没有角色档；走 §2 的离线转换，RPG 时白名单加蒙皮卡通着色器，两个枚举末尾追加角色档。
- **12 对白字形**：雾幕后新生成 MSDF 字形必须为 0（G16），字形集在构建时从全部文本表收齐。
- **13 镜头规则**：INV-2 与"镜头不自己动"会挡住第三人称跟随和 Playing 里的剧情过场；观光规则集下不变，RPG 规则集另列允许的镜头行为。
- **14 季节与日历**：时刻表没有"日类型"维；观光下不变，RPG 由日历推进季节，时刻表以后加维。
- **15 文本**：`LocalizedName(Zh, En)` 内联两种语言；不阻塞 v1，RPG 时加文本键和 `game/data/text/`。
- **16 必须一起恢复的存档段**：`quest`、`inventory`、`flags`、`map:<id>/objects` 互相依赖，单独重置会重复领奖或永久卡关。开工条件（WP-50 与首批任务包落实）：规则集声明"必需段组"及各段起始版本；组内任一段损坏时整组回退到同一世界线里最近一份完整可读的旧代，不单独换默认值；写档版本早于起始版本的缺段补默认值，否则按组内损坏处理；任务选择、奖励与世界变化在同一落定状态里完成后才算安全时刻，中途不拍快照。沿用现有容器、代际与回退机制（SYS-SAVE），不另做事务框架。
