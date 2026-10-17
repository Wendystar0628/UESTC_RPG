---
id: WF-ONBOARD
title: 子代理开工手册
category: workflow
summary: 子代理的查询手册，不通读：开工路径与引擎命令、怎么查资料、完成怎么自检与回报样式
keywords: [开工手册, 查询手册, 子代理, 开工环境, 主检出, 引擎路径, 查资料, SECTIONS.md, 接入要点, Godot API, signatures.txt, 文档冲突, 完成与回报, gate.ps1, 核心专项, 回报格式, 截图, render-review, 交回就结束]
owners: [WP-00]
contributors: []
depends: [WF-INDEX, WF-RULE, WF-BUILD, CORE-INDEX]
related: [WF-ENTRY, WF-DOC, WF-FLOW, WF-MODEL, ENG-WP, ENG-GATES]
contracts: []
data: []
gates: []
---

# WF-ONBOARD · 子代理开工手册

> **范围**：本文是施工子代理的查询手册，不通读：开工路径与引擎命令、怎么查资料、做完怎么自检与回报样式。
> **不在本文**：子代理的入口与必守：WF-ENTRY 第 2 节；每个包做什么：ENG-WP §2、§2.1；硬规则：WF-RULE（守则）、WF-BUILD（改哪里）；怎么被派工与交接：WF-FLOW、WF-MODEL §8；按要做的事查哪里：CORE-INDEX §6；各系统的设计细则：各自的文档。
> **查阅**：先读 §0；路径与引擎命令看 §2；怎么找资料看 §3；做完怎么交、回报样式看 §4。
> **关联**：[WF-INDEX 协作总纲](WF-INDEX-协作总纲.md)、[WF-ENTRY 角色入口卡](WF-ENTRY-角色入口卡.md)、[WF-RULE 全员守则](rules/WF-RULE-全员守则.md)、[WF-BUILD 施工边界](rules/WF-BUILD-施工边界.md)、[WF-DOC 文档写法](rules/WF-DOC-文档写法.md)、[WF-FLOW 开发流程](process/WF-FLOW-开发流程.md)、[WF-MODEL 模型选择与子代理](collaboration/WF-MODEL-模型选择与子代理.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[ENG-GATES 验收门禁](../engineering/ENG-GATES-验收与门禁.md)、[CORE-INDEX 总索引](../README.md)

## 0 结论

1. **本文是查询手册，不通读**：入口与必守在 WF-ENTRY 子代理节，需要什么再查本文对应的节（D-178）。
2. **怎么查资料**（§3）：用 `docs/design/SECTIONS.md` 定位小节，按行号只读需要的那一节；碰到别的系统先看那份文档的接入要点；按要做的事查，见 CORE-INDEX §6。
3. **Godot API 不凭记忆写**：先在 `docs/godot-api/signatures.txt` 查到签名再用（§3.3，D-145）。
4. **做完以后**：跑 `scripts\gate.ps1 -Wp <WP-xx>`，截几张图，提交并推送，按 §4 回报，交回就结束（§4）。

### 0.1 接入要点

本文只约束子代理的工作方式，没有对其他包的接口要求。新增或改名的规范文档，要在 CORE-INDEX §6 对应行补上，才能被查到。

## 2 开工路径与引擎命令

- **路径**：项目在 `E:\Project\UESTC_RPG`，这就是主检出，直接在 `main` 上改，不另开目录和分支；没有包号的任务是 `SN-<nnn>`。简报会给出绝对路径和同时在干活的邻居，只在点名的路径读写；开工先 `git status` 看谁在改什么（WF-FLOW §2.1）。
- **引擎**：命令行版路径读环境变量 `UESTC_GODOT`，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。
- **构建与测试**：`dotnet build`、`--import`、烘焙、`gate.ps1` 和截图排队，一次只跑一个（ENG-ARCH §6）。
- **git**：只 `git add` 自己改的文件，带路径提交，测完推送，回滚只用 `git revert`（WF-RULE §7）。

## 3 查资料

### 3.1 文档在哪、怎么找

- **总入口**：文档都在 `docs/design/`，总索引是 CORE-INDEX（`docs/design/README.md`）；文档地图每份一行，写着它规定什么、由哪个包主责。按要做的事查哪里见 CORE-INDEX §6。
- **小节目录 `docs/design/SECTIONS.md`**：每份文档的每一节一行，格式 `编码 §号 标题 · L行号 · 字数`。先 grep 关键词或编码，再用行号只读原文那一段；不要整份读大文档（ENG-ARCH、ENG-PERF、SYS-AUDIO 都在 10 万字以上）；行号可能落后几行，以标题为准。
- **每份规范文档的结构一样**：文件头的 `summary`、`keywords`、`contracts`、`data`；范围块的"查阅"行；§0 结论；§0 末尾的"接入要点"（别的包碰到这个系统必须守的规则）。判断一份文档和你有没有关系，看 `summary` 和接入要点就够了。
- **可以直接 grep 的编码**：文档编码、`§`、`INV-n`、`D-nnn`、`G1`–`G21`、`B1`–`B13`、`K1`–`K5`、`WP-xx`、契约名与类型名。跨文档引用写作"SYS-WEATHER §6.4"。
- **专门的查询文档**：术语查 CORE-GLOSSARY；某条规则为什么这样、是否仍然生效，按 D 编号查 CORE-DECISIONS；坐标、单位、命名、目录、帧顺序查 CORE-CONVENTIONS。
- **契约**：以 `game/contracts/` 的源码为准（ENG-ARCH §3），冻结状态见状态文件（WF-STATUS）。
- **ENG-WP 的"起读"栏**分三档：必读、接入要点、其余按需。

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

1. **跑检查**：运行 `scripts\gate.ps1 -Wp <WP-xx>`，它按改动自动跑快检。改动碰到下面三类时，加跑核心专项并确保全部通过（ENG-GATES §2，D-167）：启动与进出（G5 的启动与进出核心范围）、行走碰撞（G15 的前四项和入树检查）、存档（G20 的往返、损坏、退出时保存）。其余门禁留到集中测试，由主会话安排。截图类门禁的运行方式见 ENG-GATES §1。
2. **截图**：用 `--pose` 截几张能说明本包效果的图，存到 `output/render-review/`；全套巡检截图在集中测试时做。结果看得见或听得见的，可调参数集中放一处并在回报里列出（WF-FLOW §4）。
3. **提交并推送**：自测通过后带路径提交并 `git push origin main`（WF-RULE §7）。
4. **回报**：照 §4.2 的样式，中文，交给派你的一方；要提的申请（契约申请见 WF-FLOW §5）、做过的假设、留到后面的细节都写进回报。
5. **交回时写"接续"**：在任务单"状态"里照 WF-MODEL §8 写一段"接续"，下一张子单由新的子代理照它接着做。

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
- 各行怎么填由对应的系统文档规定（门禁见 ENG-GATES §2，天气与画质见 SYS-WEATHER §10、SYS-GRAPHICS §7，存档见 SYS-SAVE §2，声音见 SYS-AUDIO §22）；不涉及的写"不涉及"。
- 报告里引用条目时只写编号，不复述内容；契约申请的六行短头部见 WF-COMMS §3.3。
