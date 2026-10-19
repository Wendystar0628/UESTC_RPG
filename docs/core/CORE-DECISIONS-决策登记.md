---
id: CORE-DECISIONS
title: 决策登记
category: core
summary: 全项目已定决策登记的用法与归卷规则；决策本体分世界与画面、系统与工程、协作与流程三卷
keywords: [决策, D 编号, 决策登记, 生效, 待原型验证, 细则所在, 推翻决策, 归卷]
owners: []
contributors: []
depends: [CORE-INDEX]
related: [CORE-CONVENTIONS, CORE-GLOSSARY, ENG-WP, ENG-GATES, ENG-ARCH, ENG-BUDGET, ENG-PERF, ENG-ROADMAP]
contracts: []
data: []
gates: []
---

# CORE-DECISIONS · 决策登记

> **范围**：本文是全项目已定决策登记的主文档，规定编号、状态、"细则所在"写法和归卷规则；决策本体分三卷，每条一句结论，附适用范围、细则所在的小节和状态。需要确认一件事定了没有、定成什么样、细则去哪里看时，按 D 编号或关键词查三卷，不必开工前通读。
> **不在本文**：各条决策的内容在三卷；数值、规则、契约与验收的细则，以"细则所在"指向的文档为准；决策的来由与过程不写进文档；术语见 CORE-GLOSSARY，约定见 CORE-CONVENTIONS，RPG 扩展路线见 ENG-ROADMAP。
> **新增决策**：由主会话登记进本文，再改相关文档；子代理只读主文档与各卷，发现细则文档与本文不一致时报主会话，不自行改动。
> **查阅**：先读 §0；按 D 编号 grep 三卷；按领域找卷：世界、地图、时间、天气、美术看 CORE-DECISIONS-WORLD，界面、存档、音频、性能、工程看 CORE-DECISIONS-SYSTEMS，协作与流程看 CORE-DECISIONS-PROCESS，卷目录看 §1

## 0 用法

- **编号**：D-001 起全文连续，编号不改、不复用。决策有变时改写原行，只写现行决定。
- **状态**：
  - 生效：按细则文档施工；
  - 待原型验证：方向已定，数值或做法以原型、看图、听评或实测为准；验证前按细则文档里的初值施工；验证完立即改成生效。
- **细则所在**：写"编码 §x.y"，例如"SYS-WEATHER-PROCESS §1.4"；不变量写"INV-6"。决策与细则文档有出入时以决策为准；发现者报主会话，由主会话同批改细则文档。
- **归卷**：按"细则所在"第一份文档的领域归卷，跨域取主要影响的一边：世界、地图、时间、天气、美术、植被归 CORE-DECISIONS-WORLD；界面、存档、音频、画质、性能、预算、工程、契约归 CORE-DECISIONS-SYSTEMS；WF-*、WF-DOC、门禁流程、文档规则归 CORE-DECISIONS-PROCESS。新决策追加到所属卷表末尾；查最大号用 `grep -ohE "^\| D-[0-9]+" docs/core/CORE-DECISIONS*.md | sort | tail -1`。

### 0.1 接入要点

- **想知道一件事定了没有**：按 D 编号或关键词 grep 本文；数值与做法以"细则所在"指向的文档为准，二者有出入以本文为准（§0）。
- **发现细则文档与本文不一致**：以本文为准，报主会话，不自行改动（§0）。
- **需要一个新决定**：在回报里提出，由主会话登记进本文，再改相关文档（WF-DOC）。

## 1 卷目录

| 编码 | 简题 | 装什么决策 |
| --- | --- | --- |
| CORE-DECISIONS-WORLD | 世界与画面决策 | 作品定位与范围、世界与地图、时间季节天气、美术风格 |
| CORE-DECISIONS-SYSTEMS | 系统与工程决策 | 界面、存档、音频、性能与画面设置、工程 |
| CORE-DECISIONS-PROCESS | 协作与流程决策 | 协作、流程、文档规则、门禁流程 |
