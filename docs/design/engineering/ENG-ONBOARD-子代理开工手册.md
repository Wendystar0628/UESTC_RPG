---
id: ENG-ONBOARD
title: 子代理开工手册
category: engineering
summary: 子代理开工时唯一要通读的文档：开工环境、硬规则、查资料的方法与路由、完成与回报
keywords: [开工手册, 子代理, 必读, 查阅, 按需查, 小节目录, SECTIONS.md, 接入要点, 硬规则, 禁用写法, 工作树, 回报, CLAUDE.md]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX, CORE-CONVENTIONS, ENG-ARCH, ENG-WP]
related: [ENG-GATES, ENG-STATUS, ENG-BUDGET, CORE-DECISIONS, CORE-GLOSSARY, ENG-ONBOARD-LOOKUP]
contracts: []
data: []
gates: []
---

# ENG-ONBOARD · 子代理开工手册

> **范围**：本文是每个子代理开工时唯一要从头读到尾的文档：开工环境、硬规则、怎么查其余文档、做完怎么自检和回报。仓库根的 `CLAUDE.md` 会引入本文，多数情况下它已经在你的上下文里。
> **不在本文**：每个包做什么（ENG-WP §2、§2.1）；各系统的设计细则（各自的文档）。规则原文以 ENG-ARCH §2（禁用写法）、ENG-GATES（测试与回报）、CORE-CONVENTIONS（文件与文档约定）为准，本文只汇总，不一致时以原文为准并报告主代理。
> **查阅**：先读 §0；环境与 git 看 §2；能改什么、不能写什么看 §3；找资料看 §4；做完怎么交看 §5。
> **关联**：[CORE-INDEX 总索引](../README.md)、[CORE-CONVENTIONS 约定](../core/CORE-CONVENTIONS-约定.md)、[ENG-ARCH 工程架构](ENG-ARCH-工程架构.md)、[ENG-WP 工作包](ENG-WP-工作包派工单.md)、[ENG-GATES 验收门禁](ENG-GATES-验收与门禁.md)、[ENG-STATUS 施工状态](ENG-STATUS-施工状态.md)

## 0 结论

1. **开工只读三样**：本文、ENG-WP §2 主表与 ENG-WP §2.1 里本包的两行、派工简报；其余需要时再查（D-178）。
2. **硬规则必须遵守**（§3）：只改本包的独占目录；共享文件和契约要改就提申请；模块用特性接入；禁用写法由门禁 G4 拦截；贴近主线、小步交付（§3.6）；不过度工程化（§3.7）。
3. **主线优先**：主线闸口通过之前只交主线版，能接进主线、跑得通、不崩、不挡路（§1，D-168）。
4. **怎么查资料**（§4）：用 `docs/design/SECTIONS.md` 定位小节，按行号只读需要的那一节；碰到别的系统先看那份文档 §0 末尾的"接入要点"；按要做的事查，见 ENG-ONBOARD-LOOKUP §1。
5. **Godot API 不凭记忆写**：先在 `docs/godot-api/signatures.txt` 查到签名再用（§4.3，D-145）。
6. **拿不准就问**：会改动契约、数据格式或目录归属的，停下来问主代理；不影响这些的，按最保守的理解做，并在回报里写明假设（§4.4）。
7. **做完以后**：跑 `scripts\gate.ps1 -Wp <WP-xx>`，截几张图，按 ENG-GATES §5 回报（§5）。

### 0.1 接入要点

本文只约束子代理的工作方式，没有对其他包的接口要求。新的规范文档要按 §6 写，才能被 §4 的查法找到。

## 1 项目与当前阶段

- **作品**：《电光年华》，校园题材的独立 RPG，首张地图是成电清水河校区，画风是三渲二的日式动画背景。
- **技术栈**：Godot 4.7.2-stable .NET 版，只做 PC（Windows）；代码全部用 C#（net10.0，C# 14，D-177），着色器用 Godot 着色语言，不写 GDScript。
- **当前阶段**：世界与观光模式，范围见 CORE-INDEX §1、§2。
- **主线优先**（CORE-CONVENTIONS §8.6，D-168）：主线闸口之前每个包只交主线版，可以用简化几何、默认参数、占位贴图和占位声音，边角情况可以跳过；但不能改动或绕开契约、数据格式和目录归属，也不留临时接口。本包的主线版要看到什么、接通谁，见 ENG-WP §2.1 你那一行。
- **作品定位**：不是地图复刻。主要建筑、道路、设施的大体位置和形状要对，其余可以风格化，也要主动做设计增补（MAP-QSH-ADDITIONS）；照片和 OSM 只用来定结构，材质、色彩、陈设和标识按氛围做艺术化处理；增删都写进覆盖层并写明理由（ENG-ARCH §3）。

## 2 开工环境

- **路径**：项目在 `E:\Project\UESTC_RPG`，你的工作树在 `..\UESTC_RPG.wt\<WP-xx>`。
- **git**（ENG-ARCH §6，D-166）：分支是本地临时分支 `wp/<xx>-<slug>`，小步提交，不推送、不合并、不碰 `main`；主代理通知 `main` 有新合入时执行 `git rebase main`；只有一个代理写代码时直接在主检出的 `main` 上做。
- **引擎**：命令行版路径读环境变量 `UESTC_GODOT`，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。
- **工作树配置**：工作树里的 `game\override.cfg` 已把 `user://` 指到 `UESTC_<WP-xx>`，不要改，也不要提交。
- **当前施工状态**：看 ENG-STATUS。
- **向谁回报**（ENG-TEAM §0.1、ENG-TEAM-ROLES §1）：向派你的一方回报；单 Claude 账户模式下 Claude 侧子代理都向主会话回报（ENG-TEAM-QUOTA §4）。
- **交接目录** `E:\Project\UESTC_RPG.wt\.collab\`：目录说明与"谁读什么"见其中的 `README.md`（ENG-TEAM-COMMS §1.1）；开始下一步之前看一眼 `logs\merged.log` 末尾，`main` 变了就 rebase。

## 3 硬规则

### 3.1 改哪里

- **只改本包的独占目录**：见 ENG-WP §2 的"独占目录"一列和 `ownership.json`。越界会被门禁 G3 拒绝。
- **共享文件不自己改**：包括 `project.godot`、全部 csproj、`Directory.*.props`、契约等，清单见 CORE-CONVENTIONS §5.1。它们由 Astra 改、经主代理合入（ENG-TEAM-FLOW §2.7）。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请（`.collab/requests/contract-*.md`），由 Astra 处理（ENG-TEAM-FLOW §2.7）。契约分批冻结：已冻结的组只增不改（ENG-ARCH §3，D-134）。
- **新模块用特性接入**：`[ContentModule]`、`[Part]`、`[KernelNode]`、`[KernelService]`、`[SaveSection]`、`[AmbientEmitter]`、`[InteriorProvider]`、`[MediumPresenter]`、`[QualityResponsive]`。不改注册表，不改主场景，不加 Autoload（ENG-ARCH §2）。
- **场景与路径**：不在编辑器里打开或重存场景，场景尽量由代码生成；代码里只用 `res://` 路径，不手写 `uid://`；新文件的 `.uid` 由 `--headless --import` 生成，跟源文件一起提交。

### 3.2 代码边界与接入方式

细则见 ENG-ARCH §2 和各系统文档 §0 末尾的接入要点，这里每条只留一行：

- **`game/gen` 是纯 .NET**：只引用契约、部件和世界模型（只读），不引用 GodotSharp、不读文件、不读世界状态；本机烘焙结果必须与参考烘焙逐字节一致（D-165）。
- **显示**：一律经内核的 `IRenderHost`，材质只经 `ToonSpec` 取得。
- **时间、季节、天气**：几何不随时季和天气变，只走调色板、灯光通道、变体掩码和天气向量；天气只读 `IWorldState.WeatherNow` 与天气类全局参数；`IWorldState.Instance` 一变，做平滑或留历史的直接落到新值（SYS-TIME §0.1、SYS-WEATHER §0.1）。
- **画面设置**：`[QualityResponsive]` 声明、`IQualityAware.ApplyQuality()` 接收，风格底线任何画质下都不能关（SYS-GRAPHICS §0.1）。
- **按图的数据**：从 `IMapContext` 取，不写死数据路径、实体 ID 和地图 ID（SYS-MAPS §0.1）。
- **观者与镜头**：观者在哪读 `IObserverState`，画面从哪看读 `ICameraRig`（SYS-VIEW §0.1）。
- **存档**：跨读档的状态用 `[SaveSection]`，字段增减要升版本并写迁移，不自己读写 `user://`（SYS-SAVE §0.1）。
- **声音**：只经 `IAudioService`、`IAmbienceService`、`IFoleyService`，随机只用契约里的 `Rng`（SYS-AUDIO §0.1）。
- **室内**：只有 WLD-INTERIORS §2 开放清单里的楼能走进去（WLD-INTERIORS §0.1）。
- **规则集与枚举**：观光专属常量读 `IRuleset`；会追加值的枚举写穷举 `switch`，`default` 报错（ENG-ROADMAP §6）。

### 3.3 禁用写法

完整清单和少数例外见 ENG-ARCH §2，门禁 G4 检查。最容易踩的几条：

- **随机与哈希**：不用 `System.Random`、`GD.Randf`、`RandomNumberGenerator`，一律用契约里的带种子生成器；不拿 `string.GetHashCode` 当种子，改用 `StableHash`；不依赖 `Dictionary`、`HashSet` 的遍历顺序。
- **计时**：不用 `DateTime.Now`、`Stopwatch`、`Timer`，只有内核能计时；着色器里不用 `TIME`，改用全局参数 `anim_time`。
- **材质与着色器**：不用 `StandardMaterial3D`，不自己 new `ShaderMaterial`；不写十六进制色值，一律用色名。
- **gen 与烘焙子任务里**：不用 `Math`、`MathF` 的超越函数，改用 `DetMath`。

### 3.4 内容与画面的底线

- **视觉精度分两层**（ART-FIDELITY §0，D-009、D-176）：底线是任何位置、任何距离看都不粗糙（比例对，有倒角和收边，没有光秃的拉伸体块）；细节密度按精度区 K1–K5 分级，表面属于哪个区读导出的精度区数据，不自己判断。去粗糙靠最低合格构件、自定义法线和描线（ART-MODELING §7），不靠堆细节；底线不拿来换预算，细节密度在本包额度内按区取舍（ENG-BUDGET §8）。
- **共同口号**："走进剧场版动画的校园，清透、敦实、干净、有生活。"建模时拿不准风格取舍，回到这句话（ART-MODELING §6）。
- **画风**（ART-STYLE）：清透干净的三渲二，影子以明度为主、按材质推导，细描线随底色染色。
- **任何时刻、季节、天气都要好看**：四季、昼夜和天气一律视觉效果优先，真实情况只作参考；时间只能往前走。
- **全局不变量**（CORE-INDEX §3）：画面与展陈里不出现人物或人物肖像；场景里的字一律简体中文（INV-4）；日式元素只放开日常街景物件（INV-7、ART-AMBIENCE）；关着的楼只做外壳、窗卡和简化室内（INV-11）。

### 3.5 文件操作

细则见 CORE-CONVENTIONS §8.4。

- **下载**：需要的东西直接下载，只用官方或可信来源，下完说明下了什么、多大、放在哪。
- **第三方许可**：只收 MIT、Apache、BSD、ISC、OFL 许可，第三方代码登记进 `third_party.json`，音频素材登记进 `licenses.json`。
- **临时文件**：放会话临时目录或不进 git 的 `output/`，不放进源码和文档目录。
- **删除**：能不删就不删；要删只用绝对路径、只删具体文件，不用通配符或相对路径，不写 `cd X; … rm …`。
- **路径与格式**：一律用绝对路径（命令行参数里的也一样）；新建文本文件用 UTF-8 和 LF 换行。只改分派给自己的文件。

### 3.6 贴近主线，少给合并添活

施工分支离 `main` 越远、碰的公共文件越多，合并时的冲突和返工就越多（ENG-TEAM-FLOW §2.10，D-199）：

- **开工和被恢复时先看主线变了什么**：读 `E:\Project\UESTC_RPG.wt\.collab\logs\merged.log` 最近 24 小时的合入，挑出和本包相关的，先 `git rebase main` 再动手。
- **只往自己的文件里加**：覆盖层、种植表这类多包共用的数据只加本包的条目；通用覆盖层已按包分片，新条目写进本包自己的 `overlay/<组>.d/<四位序号>-wp-<包号>-<短名>.json`，不往旧的 `<组>.overlay.json` 里追加。改动尽量落在本包的独占目录。
- **生成文件不手改**：CORE-INDEX §7–§9 与 `docs/design/SECTIONS.md` 由 `scripts/docs/gen_index.py` 生成，冲突时取任一边后重跑脚本。决策编号（D-nnn）由主会话统一分配，子代理需要就在回报里提。
- **测试只锁本包自己的输出**：可以锁本包的产物和"原有条目的字节与顺序不变"，不锁别的包会合法增减的全局集合（全校设计树整套指纹、全校楼总数等）。
- **改了别人依赖的东西，默认向后兼容**：数据字段、接口语义、几何口径一旦有下游在用，只增不改（加新字段或新入口，旧行为不变）；交候选前跑全仓 G2，回报里列出受影响的下游包和测试。
- **小步交付**：做到一块能独立合入的程度就先交一个候选；叠在没合入的候选上最多一层；交候选前 `git rebase main`，再用 `git merge-tree` 对最新 `main` 预演一次，有冲突先自己解掉。
- **评审结论以规范文档为准**：评审里定下的通用规则会写回规范文档，开工时按文档做。

### 3.7 不过度工程化

本项目是个人实验作品，不是商业交付级项目（ENG-TEAM-ROLES §2.2a，D-201）：
- 不写证据溯源、来源链、哈希或校验和、proof/verification 一类的脚本与报告，不建 handoff 证据目录。
- 不自己加门禁、检查脚本、测试脚手架或全量回归；只跑任务单要求的检查，默认是 `gate.ps1` 的快检。
- 回报照 ENG-GATES §5，证据只给路径；写给主会话的来信开头标【需定】【待审】【知会】，一条一件事、5 行以内（ENG-TEAM-COMMS §4）。

## 4 查资料

### 4.1 文档在哪、怎么找

- **总入口**：文档都在 `docs/design/`，总索引是 CORE-INDEX（`docs/design/README.md`），CORE-INDEX §7 的文档地图每份一行，写着它规定什么、由哪个包主责。
- **小节目录 `docs/design/SECTIONS.md`**：每份文档的每一节一行，格式 `编码 §号 标题 · L行号 · 字数`。先 grep 关键词或编码，再用行号只读原文那一段；不要整份读大文档（ENG-ARCH、ENG-PERF、SYS-AUDIO 都在 10 万字以上）；行号可能落后几行，以标题为准。
- **每份规范文档的结构一样**：文件头的 `summary`、`keywords`、`contracts`、`data`；范围块的"查阅"行；§0 结论；§0 末尾的"接入要点"（别的包碰到这个系统必须守的规则）。判断一份文档和你有没有关系，看 `summary` 和接入要点就够了。
- **可以直接 grep 的编码**：文档编码、`§`、`INV-n`、`D-nnn`、`G1`–`G21`、`B1`–`B13`、`K1`–`K5`、`WP-xx`、契约名与类型名。跨文档引用写作"SYS-WEATHER §6.4"。
- **专门的查询文档**：术语查 CORE-GLOSSARY；某条规则为什么这样、是否仍然生效，按 D 编号查 CORE-DECISIONS；坐标、单位、命名、目录、帧顺序查 CORE-CONVENTIONS。
- **契约**：D0 之后以 `game/contracts/` 的源码为准，设计说明在 ENG-ARCH §3。

### 4.2 按要做的事查哪里

按要做的事查起点（工程与流程、接入各系统、画面与内容三张表）见 [ENG-ONBOARD-LOOKUP](ENG-ONBOARD-LOOKUP-开工手册查阅表.md) §1；每份文档范围块的"查阅"行和 §0 的接入要点会再往下指。

### 4.3 Godot API

- 写任何 Godot 代码之前，先读 `docs/godot-api/README.md`。
- 用到的每个类、方法、属性，都先在 `docs/godot-api/signatures.txt` 里查到签名再写（D-145）。模型记忆里混着 Godot 3 和 4 的写法，这是最常见的错误来源。
- 着色语言的内置变量和函数，查 `docs/godot-api/shaders/README.md`。

### 4.4 文档不清楚或冲突时

- **冲突怎么判**：按 CORE-CONVENTIONS §7.6 的层级；生效的决策（CORE-DECISIONS）优先于各文档正文。
- **不清楚或缺内容**：会改动契约、数据格式或目录归属的，停下来问主代理；不影响这些的，按最保守的理解做，在回报里写明假设。
- **发现文档与 CORE-DECISIONS 不一致**：报告主代理，不要自己改。

## 5 完成与回报

1. **跑检查**：运行 `scripts\gate.ps1 -Wp <WP-xx>`，它按改动自动跑快检。改动碰到下面三类时，加跑核心专项并确保全部通过（ENG-GATES §2，D-167）：启动与进出（G5 的启动与进出核心范围，完整 G5 留到主线最终闸口）；行走碰撞（G15 的前四项和入树检查）；存档（G20 的往返、损坏、退出时保存）。其余门禁留到集中测试，由主代理安排。截图类门禁在窗口模式下跑，窗口不能最小化；脚本会自己排 GPU 锁，不要绕过；截图和基准一律不开声音。
2. **截图**：用 `--pose` 截几张能说明本包效果的图，存到 `output/render-review/`；全套巡检截图在集中测试时做。
3. **回报**：按 ENG-GATES §5 的格式写；要提的申请（§3.1）、做过的假设（§4.4）、留到后面的细节都写进回报。
4. **交回以后就结束**（ENG-TEAM-ROLES §3.5，D-191）：同包的下一张子单由新的子代理照任务单的"接续"段接着做，所以回报里写清已完成的部分、已定的取值与约定和下一步。只有交回后几分钟内的小返工会发回给你；接到时先看 `logs/merged.log`、`git rebase main`，再核对这次涉及的契约、数据和相关包的新提交，记得的旧版本不当现行依据。

## 6 改文档时

- **只写现行规则**：不写修订说明、用户原话和调研证据（D-152）。格式与引用按 CORE-CONVENTIONS §7。
- **新的规范文档**必须有（D-178）：文件头的 `keywords`、范围块的"查阅"行、§0 结论，以及 §0 末尾的"接入要点"。
- **共享文档**（共识层、ENG-WP、ENG-ARCH 等）由主代理改。子代理需要改时，在回报里提。
- **改完以后**：主代理重跑 `scripts/docs/gen_index.py`，重新生成 CORE-INDEX §7–§9 和 `docs/design/SECTIONS.md`，再跑 `scripts/docs/check_docs.py`。
