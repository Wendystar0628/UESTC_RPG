---
id: WF-ONBOARD
title: 子代理开工手册
category: workflow
summary: 子代理开工时通读的手册：项目与阶段、开工环境、怎么查资料与按要做的事查哪里、完成怎么自检与回报
keywords: [开工手册, 子代理, 开工环境, 工作树, 引擎路径, 查资料, SECTIONS.md, 接入要点, 路由表, 按任务查, Godot API, signatures.txt, 文档冲突, 完成与回报, gate.ps1, 核心专项, 回报格式, 截图, render-review, 交回就结束]
owners: [WP-00]
contributors: []
depends: [WF-INDEX, WF-RULE, WF-BUILD, CORE-INDEX]
related: [WF-DOC, WF-FLOW, WF-MODEL, ENG-WP, ENG-GATES]
contracts: []
data: []
gates: []
---

# WF-ONBOARD · 子代理开工手册

> **范围**：本文是施工子代理开工时通读的手册（仓库根的 `CLAUDE.md` 与 `AGENTS.md` 引入）：项目与阶段、开工环境、怎么查资料、做完怎么自检与回报。
> **不在本文**：每个包做什么：ENG-WP §2、§2.1；硬规则：WF-RULE（守则）、WF-BUILD（边界与禁用写法）；怎么被派工与交接：WF-FLOW、WF-MODEL §8；各系统的设计细则：各自的文档。
> **查阅**：先读 §0；项目与阶段看 §1；环境看 §2；怎么找资料、按要做的事查哪里看 §3；做完怎么交、回报格式看 §4。
> **关联**：[WF-INDEX 协作总纲](WF-INDEX-协作总纲.md)、[WF-RULE 全员守则](rules/WF-RULE-全员守则.md)、[WF-BUILD 施工边界与内容底线](rules/WF-BUILD-施工边界与内容底线.md)、[WF-DOC 文档写法](rules/WF-DOC-文档写法.md)、[WF-FLOW 开发流程](process/WF-FLOW-开发流程.md)、[WF-MODEL 模型选择与子代理](collaboration/WF-MODEL-模型选择与子代理.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[ENG-GATES 验收门禁](../engineering/ENG-GATES-验收与门禁.md)、[CORE-INDEX 总索引](../README.md)

## 0 结论

1. **开工只读四样**：本文、WF-RULE、WF-BUILD（三份都由 `CLAUDE.md` 引入）、ENG-WP §2 主表与 ENG-WP §2.1 里本包的行，加派工简报和任务单；其余需要时再查（D-178）。
2. **主线优先**：主线闸口通过之前只交主线版，能接进主线、跑得通、不崩、不挡路（§1，WF-RULE §8）。
3. **怎么查资料**（§3）：用 `docs/design/SECTIONS.md` 定位小节，按行号只读需要的那一节；碰到别的系统先看那份文档 §0 末尾的"接入要点"；按要做的事查，见 §3.2。
4. **Godot API 不凭记忆写**：先在 `docs/godot-api/signatures.txt` 查到签名再用（§3.3，D-145）。
5. **拿不准就问**：会改契约、数据格式或目录归属的，停下问；其余按最保守的理解做，回报里写明假设（WF-RULE §11）。
6. **做完以后**：跑 `scripts\gate.ps1 -Wp <WP-xx>`，截几张图，按 §4 回报，交回就结束（§4）。

### 0.1 接入要点

本文只约束子代理的工作方式，没有对其他包的接口要求。新增或改名的规范文档，要在 §3.2 对应行补上，才能被查到。

## 1 项目与当前阶段

- **作品**：《电光年华》，校园题材的独立 RPG，首张地图是成电清水河校区，画风是三渲二的日式动画背景。
- **技术栈**：Godot 4.7.2-stable .NET 版，只做 PC（Windows）；代码全部用 C#（net10.0，C# 14，D-177），着色器用 Godot 着色语言，不写 GDScript。
- **当前阶段**：世界与观光模式，范围见 CORE-INDEX §1、§2。
- **主线优先**（D-168）：主线闸口之前每个包只交主线版，简化几何、默认参数、占位贴图和声音都可以，但不能改动或绕开契约、数据格式和目录归属，也不留临时接口；本包的主线版要看到什么、接通谁，见 ENG-WP §2.1 你那一行。

## 2 开工环境

- **路径**：项目在 `E:\Project\UESTC_RPG`，你的工作树在 `..\UESTC_RPG.wt\<WP-xx>`；没有包号的任务是 `SN-<nnn>`。简报会给出绝对路径，只在那里读写。
- **git**：分支是本地临时分支 `wp/<xx>-<slug>`，小步提交，不推送、不合并、不碰 `main`，规则见 WF-RULE §7；`main` 有新合入时先 `git rebase main`（WF-RULE §10）。只有一个代理写代码时，直接在主检出的 `main` 上做，同样先过与改动相关的检查。
- **引擎**：命令行版路径读环境变量 `UESTC_GODOT`，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`；新工作树第一次需要自己 `dotnet build game/Campus.sln` 和 `--headless --path game --import`。
- **工作树配置**：工作树里的 `game\override.cfg` 已把 `user://` 指到 `UESTC_<WP-xx>`，不要改，也不要提交（工作树的技术细节见 ENG-ARCH §6）。
- **当前施工状态**：看施工状态文件 `E:\Project\UESTC_RPG.wt\.collab\status\施工状态.md`（WF-STATUS）。
- **向谁回报**：向派你的一方回报（WF-INDEX §1）；主会话开的子代理向主会话回报。
- **交接目录** `E:\Project\UESTC_RPG.wt\.collab\`：开始下一步之前看一眼 `logs\merged.log` 末尾，`main` 变了就 rebase；各类文件放哪见 WF-COMMS §1。

## 3 查资料

### 3.1 文档在哪、怎么找

- **总入口**：文档都在 `docs/design/`，总索引是 CORE-INDEX（`docs/design/README.md`）；文档地图每份一行，写着它规定什么、由哪个包主责。
- **小节目录 `docs/design/SECTIONS.md`**：每份文档的每一节一行，格式 `编码 §号 标题 · L行号 · 字数`。先 grep 关键词或编码，再用行号只读原文那一段；不要整份读大文档（ENG-ARCH、ENG-PERF、SYS-AUDIO 都在 10 万字以上）；行号可能落后几行，以标题为准。
- **每份规范文档的结构一样**：文件头的 `summary`、`keywords`、`contracts`、`data`；范围块的"查阅"行；§0 结论；§0 末尾的"接入要点"（别的包碰到这个系统必须守的规则）。判断一份文档和你有没有关系，看 `summary` 和接入要点就够了。
- **可以直接 grep 的编码**：文档编码、`§`、`INV-n`、`D-nnn`、`G1`–`G21`、`B1`–`B13`、`K1`–`K5`、`WP-xx`、契约名与类型名。跨文档引用写作"SYS-WEATHER §6.4"。
- **专门的查询文档**：术语查 CORE-GLOSSARY；某条规则为什么这样、是否仍然生效，按 D 编号查 CORE-DECISIONS；坐标、单位、命名、目录、帧顺序查 CORE-CONVENTIONS。
- **契约**：以 `game/contracts/` 的源码为准，各契约组与冻结状态的地图见 ENG-ARCH §3。
- **ENG-WP 的"起读"栏**分三档：必读小节、只读接入要点（文档 §0.1）、其余按需。

### 3.2 按要做的事查哪里

表只是起点；每份文档范围块的"查阅"行会再往下指，规则与表冲突时以文档原文为准。表里没有的，用 `SECTIONS.md` 按关键词搜。

| 要做或要查的事 | 先看 |
| --- | --- |
| 本包做什么、交什么、依赖谁、主线版做到哪 | ENG-WP §2、ENG-WP §2.1 本包的行 |
| 某个包归谁、先开哪些包 | ENG-WP §1.1、§2 |
| 契约签名，或申请改契约 | `game/contracts/`、ENG-ARCH §3；申请见 WF-FLOW §5 |
| 路径归谁、能引用哪个程序集、模块怎么注册、禁用写法的完整清单 | ENG-ARCH §2；WF-BUILD |
| 依赖的包还没交付时用什么桩 | ENG-ARCH §5 |
| 工作树的技术细节（override.cfg、导入种子、计数锁、烘焙预选） | ENG-ARCH §6 |
| 工作树、rebase、合并的流程 | WF-MERGE §5 |
| 加贴图、模型、字体、音频；本机烘焙与缓存键 | ENG-ARCH §4 |
| 本包的额度、超额怎么办；合入前核对性能硬规则 | ENG-BUDGET §0.1、§8、§7 |
| 该跑哪档测试、`gate.ps1` 怎么用 | ENG-GATES §2 |
| 截图默认串、命令行参数、审查视角的位姿 | ENG-GATES §1 |
| 回报格式、截图放哪 | 本文 §4 |
| 别的包做到哪一步 | 施工状态文件（WF-STATUS §1） |
| 开发时预览、按实体 ID 定位镜头 | TOOL-PREVIEW §1、§2 |
| 版本号、用户目录、日志与导出诊断；第三方素材与许可 | OPS-RELEASE §1、§2、§6、§7 |
| 为 RPG 预留的接口（规则集、观者状态、输入占用、枚举纪律） | ENG-ROADMAP §6 |
| 谁做什么、向谁回报 | WF-INDEX §1 |
| 一个包从派工到合入怎么走、交接文件怎么写 | WF-FLOW §1、WF-COMMS §3 |
| 守则、改哪里、写文档 | WF-RULE、WF-BUILD、WF-DOC |
| 读钟点、季节、世界实例 | SYS-TIME §0.1、§1 |
| 材质随时刻和季节变色、新增色名 | SYS-TIME §3；ART-STYLE §0.1 |
| 做季节内容（落叶、开花、覆雪） | SYS-TIME §4；WLD-VEGETATION §9 |
| 让灯、窗、屏幕随时刻亮灭 | SYS-TIME §6；ENG-PERF §10.3 |
| 让表现随天气变化（湿面、积水、降水） | SYS-WEATHER §0.1、§7、§8 |
| 响应画面设置、新增画面单项 | SYS-GRAPHICS §0.1、§3、§8 |
| 让东西动起来：风、飘落物、偶发事件 | SYS-MOTION §0.1、§3、§4.1、§10 |
| 有要跨读档保留的状态 | SYS-SAVE §0.1、§2、§10 |
| 读按图的数据，或代码里要区分地图 | SYS-MAPS §0.1、§7 |
| 要出声；部件的声音插座；碰撞体的脚步材质 | SYS-AUDIO §0.1、§4.2、§9.3 |
| 交碰撞体、做挡路物、登记可行走范围 | SYS-WALK §0.1、§6、§7、§13 |
| 读地面高度、贴地、楼的垫层、水位与坡度；写地形覆盖层 | WLD-TERRAIN §0.1、§3、§4 |
| 该读观者位置还是镜头位姿 | SYS-VIEW §0.1；ENG-ARCH §3 |
| 地图上的锚点、观景点、传送 | SYS-ATLAS §0.1、§5、§6、§7 |
| 只在过场里出现的东西、`IAppFlow` 的各状态 | SYS-TITLE §0.1、§3.3、§7 |
| 界面取色、Theme、界面着色器 | UI-VISUAL §0.1、§3.8、§11.5 |
| 读键盘鼠标、加快捷键、占用观者输入 | UI-NAV §0.1、§5 |
| 界面文字、地名、临时提示 | UI-NAV §4.9、§8 |
| 色名、影子、描线 | ART-STYLE §0.1、§3、§4 |
| 表面属于哪个精度区、底线怎么自查 | ART-FIDELITY §0.4、§0.5、§2.0 |
| 最低合格构件、倒角、法线 | ART-MODELING §7.2、§7.6、§7.8 |
| 天空、主光、雾 | ART-SKYLIGHT §0.1、§3、§6.5 |
| 日式物件能不能用、放在哪 | ART-AMBIENCE §0.1、§1 |
| 翻新老楼 | ART-RENOVATION §0.1；MAP-QSH-RENOVATION |
| 着色器白名单、全局参数、LOD 与淡化 | ENG-PERF §0.1、§1.4、§1.5、§2 |
| 生成几何、流式加载、本机烘焙 | ENG-PERF §5、§6 |
| 某栋楼能不能进、门怎么做、开放新的室内 | WLD-INTERIORS §0.1、§2、§4.1、§14 |
| 关着的楼的窗卡与简化室内 | WLD-INTERIORS §5 |
| 校界、外围的精度、行走与飞行阻挡 | WLD-OUTSKIRTS §0.1、§4.1、§7、§8 |
| 雾幕、截面、远景 | WLD-DIORAMA §0.1、§4.2、§5 |
| 树种、季相、植被 | WLD-VEGETATION §2、§9 |
| 水面、倒影、积水 | WLD-WATER §7 |
| 动物、生活痕迹、街道物件 | WLD-LIFE §0.1、§1、§4 |
| 设计增补、登记覆盖层 | MAP-QSH-ADDITIONS §0.1、§2、§7 |
| 本区的点位、锚点、入口与边界 | 本区 MAP-QSH 文档 §0 末尾的接入要点 |

### 3.3 Godot API

- 写任何 Godot 代码之前，先读 `docs/godot-api/README.md`。
- 用到的每个类、方法、属性，都先在 `docs/godot-api/signatures.txt` 里查到签名再写（D-145）。模型记忆里混着 Godot 3 和 4 的写法，这是最常见的错误来源。
- 着色语言的内置变量和函数，查 `docs/godot-api/shaders/README.md`。

### 3.4 文档不清楚或冲突时

- **冲突怎么判**：按 WF-DOC §6 的层级；生效的决策（CORE-DECISIONS）优先于各文档正文。
- **不清楚或缺内容**：按 WF-RULE §11：会改契约、数据格式或目录归属的停下问，其余按最保守的理解做并写明假设。
- **发现文档与 CORE-DECISIONS 不一致**：报告主会话，不要自己改。

## 4 完成与回报

### 4.1 做完怎么交

1. **跑检查**：运行 `scripts\gate.ps1 -Wp <WP-xx>`，它按改动自动跑快检。改动碰到下面三类时，加跑核心专项并确保全部通过（ENG-GATES §2，D-167）：启动与进出（G5 的启动与进出核心范围）、行走碰撞（G15 的前四项和入树检查）、存档（G20 的往返、损坏、退出时保存）。其余门禁留到集中测试，由主会话安排。截图类门禁的运行方式见 WF-RULE §13。
2. **截图**：用 `--pose` 截几张能说明本包效果的图，存到 `output/render-review/`；全套巡检截图在集中测试时做。结果看得见或听得见的，可调参数集中放一处并在回报里列出（WF-FLOW §4）。
3. **回报**：照 §4.2 的格式，中文，交给派你的一方；要提的申请（契约申请见 WF-FLOW §5）、做过的假设、留到后面的细节都写进回报。
4. **交候选时写"接续"**：在任务单"状态"里照 WF-MODEL §8 写一段"接续"，下一张子单由新的子代理照它接着做。
5. **交回以后就结束**（WF-MODEL §7，D-191）：只有交回后几分钟内的小返工会发回给你；接到时先看 `logs/merged.log`、`git rebase main`，再核对这次涉及的契约、数据和相关包的新提交，记得的旧版本不当现行依据。

### 4.2 回报格式

```
工作包：WP-18 南区
改动文件：game/gen/zones/south/**、game/data/maps/qingshuihe/overlay/south.d/
已完成条目：MAP-QSH-SOUTH §1.*、§2.1–§2.9、§4.*、§5.*、§6.*、§7.*；MAP-QSH-ADDITIONS §7 中南区的增补共 11 项
未完成条目及原因：MAP-QSH-SOUTH §2.6 主楼喷泉（可选），未做
覆盖层：reshape 2 处（均为轻微调整），add 23 处（design:east-lake-*），详见差异报告
需要其他包配合：WP-23 种植主楼两侧的银杏林，范围 x −470…−150、z 430…620
预算：三角形 7.4% / 8%，绘制调用 5.1% / 6%；关掉本包前后的帧时差 1.9 ms
门禁：scripts\gate.ps1 -Wp WP-18；快检通过；核心专项 G15 前四项通过（本包提供碰撞体）；其余门禁留到集中测试
天气与画质：本区"天气要点"已落实（避雨处 4 处、积水点 6 处）；天气增量（SYS-WEATHER §10）不涉及；负责的画面单项（SYS-GRAPHICS §7）无
存档：不涉及
声音：本区登记的发声器 9 个、插座 3 个（SYS-AUDIO §4.2）；Foot 已填；G21 不涉及；音频主线程与声部不涉及
环境：引擎 4.7.2.stable.mono.official.ed1daf0bf；截图显卡 RTX 4080（G7 断言通过）
截图：output/render-review/WP-18-*.png
契约修改申请：无
```

- 截图统一放在 `output/render-review/`（不进 git），命名为"包-视角.png"或"包-Qn.png"（Qn 是巡检点编号 Q1–Q8，与点位 P00–P23 区分）。非默认状态的截图在后面加季节和钟点，例如 `WP-18-V6-winter-2100.png`；天气不是晴、预设不是精致时再依次加天气和预设，例如 `WP-18-V6-winter-2100-snow.png`、`WP-18-V1-autumn-1730-clear-smooth.png`（名字与命令行取值相同）。
- "门禁"一行先写运行的命令，再写结果。
- "时季矩阵"一行写 G12 抽样的结果、G13 的 ΔE 最大值，以及本包涉及的英雄帧得分；不涉及时写"不涉及"。
- "天气与画质"一行写本包的天气增量（SYS-WEATHER §10）和负责的画面单项（SYS-GRAPHICS §7）的落实情况，以及 G17 的结果；不涉及时写"不涉及"。
- "存档"一行（SYS-SAVE §2）：写本包的存档段是否已登记、G20 是否通过，以及段的版本号和有没有迁移函数；没有存档段时写"不涉及"。
- "声音"一行（SYS-AUDIO §22）：写本包登记的发声器与插座、`Foot` 的填写情况、G21 相关子项的结果、音频主线程与声部；不涉及时写"不涉及"。
- 报告里引用条目时只写编号，不复述内容；合并请求与契约申请的六行短头部见 WF-COMMS §3。
