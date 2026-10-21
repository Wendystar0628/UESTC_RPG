---
id: ENG-ROADMAP
title: 扩展路线
category: engineering
parent: ENG-INDEX
summary: 观光到 RPG 的扩展原则、现在不能写死的地方、写法纪律和 6 条最小预留
keywords: [扩展路线, 从观光到 RPG, 扩展原则, 规则集, sightseeing, 观光规则集, 观者状态, 输入占用按原因计, 枚举的穷举纪律, 偶发事件数据化, 名字占位, RPG 预留目录, 实体 ID 前缀, 存档预留段名, 最小预留, 写法纪律]
depends: [CORE-INDEX, ENG-ARCH, SYS-SAVE, SYS-MAPS]
related: [ENG-BUDGET, ENG-PERF, SYS-MOTION, SYS-TIME, SYS-WALK, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO]
contracts: []
data: []
gates: [G4, G5, G20]
paths: ["scripts/requirements.*", "game/testing/**", "game/data/build-code-fingerprints.json", "game/gen/maps/qingshuihe/Campus.Gen.Maps.Qingshuihe.csproj", "game/gen/sites/**", ".collab/tools/**", "dashboard/**", "game/gen/Campus.Gen.csproj", "game/kernel/map/data/**", "game/kernel/renderer/geometry/**", "game/kernel/renderer/render-state/**", "game/kernel/instances/**", "game/gen/geometry/**", "scripts/gates/arch/Campus.Arch.Tests.csproj", "scripts/godot-api/ApiDump/ApiDump.csproj", "game/contracts/Runtime/Campus.Contracts.Runtime.csproj", "game/contracts/Campus.Contracts.csproj", "game/data/maps/*/interiors.json", "game/Campus.Gen.Tests.csproj", "game/Campus.Tests.csproj", "game/contracts/.gdignore", "game/export_presets.cfg", "game/contracts/*.csproj", "game/kernel/registry/**", "game/data/rulesets/**", "scripts/gates/arch/**", "**/Directory.*.targets", "game/Campus.csproj", "game/gen/.gdignore", "game/project.godot", "Directory.*.targets", "game/kernel/stubs/**", "scripts/godot-api/**", "**/Directory.*.props", "scripts/gate.ps1", "third_party.json", "Directory.*.props", "docs/godot-api/**", "game/contracts/**", ".gitattributes", "scripts/build/**", "scripts/setup/**", "game/gen/bakeinput/assembly/**", "scripts/docs/**", "budgets.json", "game/addons/**", "global.json", "version.txt", "scripts/lib/**", ".gitignore", ".ignore", "AGENTS.md", "CLAUDE.md", "README.md", "启动游戏.cmd", "**/*.csproj", "LICENSE", "tools/**", "**/*.sln", "game/.editorconfig", "scripts/git-hooks/**"]
---
# ENG-ROADMAP · 扩展路线

> **范围**：本文只规定架构上的扩展原则、现在不能写死什么、写法纪律，以及 6 条最小预留。不设计 RPG 玩法。
> **不在本文**：RPG 子系统挂点、预算余量与开工待办（RPG 开工时按当时的代码重写）；契约签名与冻结（ENG-ARCH §3）；各系统细则（各系统文档）；预算数值（ENG-BUDGET）。契约名、数据路径以 `game/contracts/` 源码与各文档为准。
> **查阅**：先读 §0；扩展原则看 §1；写死的规定与写法纪律看 §2；最小预留看 §3

## 0 结论

1. 观光是 RPG 的子集：同一套世界、渲染、地图服务、存档和音频；观光是关掉一部分功能的规则集 `sightseeing`，RPG 是另一个规则集，不另做一套引擎（§1）。
2. 新功能只用四种方式进来：加服务、加数据、加存档段、在枚举末尾追加值；不改既有契约的语义，不加 Autoload（§1）。
3. 观光的禁令是配置，不是代码；规则集化只改规则写在哪，观光规则一条不放松（§1、§2）。
4. 写法纪律现在就照做，权威是 STD-CONVENTIONS §3.6（§2）。
5. 只做 6 条最小预留（DECISION-160）：规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位；判据是以后再补会改变既有契约的语义、存档格式或目录归属（§3）。
6. 除这 6 条外不为 RPG 预留任何东西，其余以后只增即可（§3.2）。

### 0.1 接入要点

- **写法纪律**（观光规则读规则集、枚举穷举、换实例直接落新值）：按 STD-CONVENTIONS §3.6（§2）
- **要知道观者在哪**：读 `IObserverState`；"画面从哪看"（视锥、听者、流式选级）读 `ICameraRig`（§3.1 第 2 条）
- **要临时接管输入**：经 `ICameraRig.LockInput(InputLockReason)` 占用，不设布尔开关（§3.1 第 3 条）
- **加新功能**：只用加服务、加数据、加存档段、枚举末尾追加；不加 Autoload（§1）
- **碰到 RPG 预留的名字**：`game/entities/` 等预留目录观光期不写入；`npc:`、`item:`、`quest:` 前缀、存档预留段名、E、I 两个键和 `interact`、`inventory` 动作名不给观光功能用（§3.1 第 6 条）

## 1 扩展原则

以后的方向是 RPG，主线是"入学到毕业"。观光版不做任何 RPG 内容，只保证 RPG 进来时不用推翻现有架构。**规则集**是决定"这一局按什么规则运行"的一组配置；观光是规则集 `sightseeing`，存档 meta 的 `Ruleset` 字段记它（SYS-SAVE）。

1. **观光是 RPG 的子集**：同一套世界、渲染、地图服务、存档、音频。
2. **新功能只用四种方式进来**：加服务（`[KernelService]`、`[KernelNode]`）、加数据、加存档段（`[SaveSection]`）、加枚举值（末尾追加）。不改既有契约的语义，不加 Autoload（ENG-ARCH §2、§3）。
3. **观光的禁令是配置，不是代码**：运行时代码读规则集，门禁按规则集取判据。
4. **观光规则一条不放松**：INV-1–INV-12 与 ENG-GATES-VETO §1 的一票否决在观光规则集下照旧；扩展点只留口子，不提前做 RPG 内容。
5. **确定性和单调时间照旧**：剧情、NPC、事件的随机走 `StableHash` 与 `Rng`；世界时间在一个实例内单调，读档换实例。
6. **预留越少越好**：只有"以后再补会改变既有契约语义、存档格式或目录归属"的才预留（§3）。

## 2 写死的规定与写法纪律

观光版现行的规定全部保留，看作规则集 `sightseeing` 的内容：不出现 NPC 与他人、没有可点选的东西、全片一个偶发事件、只有旁观和行走两种观察模式、镜头不自己动、时间季节天气随意调、解锁策略 `all-unlocked`（INV-1–INV-12，DECISION-182）。内容规则写成规则集字段 `Characters`、`Interaction`、`ClearVoices`（运行时不读，门禁按它取判据）。其余写死的规定（碰撞不随状态变、全场一盏方向光、不画车流、外围不能进、界面只有登记的件、音乐只在开始页等），以后只增即可，不需要预留，各自的规则在对应文档。

**写法纪律**只在 STD-CONVENTIONS §3.6，现在就照做，不花额外代码；以后的 NPC、剧情状态同样遵守。

## 3 最小预留

### 3.1 清单

以下 6 条已采纳（DECISION-160）；契约与数据以 `game/contracts/` 源码和 `game/data/` 为准，这里只写预留什么、为什么。

1. **规则集**：观光规则（允许的观察模式、世界控制、解锁策略、偶发事件与内容规则）写成规则集配置，运行时只读，只有新游戏和读档能切换。新游戏先定规则集再创建各存档段的默认值；读档先按存档元数据里的规则集再进世界。RPG 规则集可以另设解锁策略（靠近锚点解锁、导览桩 Known、底图揭示），细则那时再定；观光规则集 `all-unlocked`，地图全部可见，不解锁（SYS-ATLAS）。
2. **观者状态**：观者（脚位、眼位、朝向、所在空间、观察模式）与镜头分开读；旁观时是镜头位置，行走时是行走控制器的物理体。第一人称下观者与镜头重合，第三人称后分开。
3. **输入占用按原因计**：接管观者输入一律按原因占用，不设布尔开关，没有占用时才接受观者输入；原因有大地图、开始页、加载界面、菜单、改键捕获，以后末尾追加。
4. **枚举的穷举纪律**：见 STD-CONVENTIONS §3.6。
5. **偶发事件数据化**：事件 ID、路线实体、选树选灯规则、各版本开关记在数据里，导演只跑规则集允许的事件。
6. **名字占位**：RPG 预留的目录（归属登记在相关文档的 `paths`）、实体 ID 前缀 `npc:`、`item:`、`quest:`（G5 拒绝观光数据使用）、存档预留段名，以及 E、I 两个键和交互、背包两个动作名，观光期不写入、不分配给观光功能。

### 3.2 不需要现在预留的

以后只增即可：新观察模式（`ViewMode` 末尾追加）；新帧步（`FramePriority` 追加常量）；新服务与契约块（交互、任务、对话、背包、NPC、文本、日历）；新存档段；`MapKind.Instance`；规则集预算。
