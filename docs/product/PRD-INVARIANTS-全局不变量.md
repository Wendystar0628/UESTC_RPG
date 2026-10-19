---
id: PRD-INVARIANTS
title: 全局不变量
category: product
parent: PRD-INDEX
summary: 观光规则集的 12 条硬约束 INV-1–INV-12：平台与范围、观察模式、界面、场景文字、画风、不出现人物、日式边界、世界状态、格局与地图数据、动态与声音、精度底线、性能；每条只写长期不变的约束，具体设计与数值在"详见"的文档
keywords: [全局不变量, INV, 硬约束, 观光规则集, sightseeing, 不变量, 世界规则]
owners: []
contributors: []
depends: [CORE-INDEX]
related: [REG-DECISIONS, REG-GLOSSARY, ENG-ROADMAP, ENG-GATES]
contracts: []
data: []
gates: []
---

# PRD-INVARIANTS · 全局不变量

> **范围**：本文列出观光规则集 `sightseeing` 的全局不变量 INV-1–INV-12，每条是对所有工作包长期生效的硬约束。
> **不在本文**：具体设计、数值与清单（以每条"详见"的文档为准）；核心准则（CORE-INDEX §0「守硬约束，审美听作者」一条指向本文）；门禁 G（ENG-GATES §1.2）；RPG 规则集放宽哪些（ENG-ROADMAP）。
> **查阅**：先读 §0；按编号 grep `INV-n` 直接找到 §1 表里那一行，细则看它的"详见"。

## 0 结论

1. **硬约束**：INV 对所有工作包生效，主线版也要满足；只有 INV-11 的细节密度和 INV-12 的性能数值留到主线闸口之后验收（WF-RULE §8）。RPG 规则集放宽哪些由 ENG-ROADMAP 登记，登记前一条不放松。
2. **只写长期不变的约束**：会随开发改的设计、数值、键位、颜色和清单不进本文，写在"详见"的文档里；改那些不用动本文。
3. **编号**：不变量写 INV-n，门禁写 G，是两套编号；编号不改、不复用（D-132、D-156）。

### 0.1 接入要点

- **改一条不变量**：先在 REG-DECISIONS 登记，再同批改本文、CORE-INDEX §0 里对应的摘要和"详见"的文档。
- **想往本文加内容**：先问它会不会随开发改；会改的写进管它的文档，本文只写约束并指向。

## 1 不变量

| 编号 | 约束 | 详见 |
| --- | --- | --- |
| INV-1 | PC（Windows）上实时渲染的观光作品；不做地图应用那类功能（搜索、地点卡片、路径规划、在 3D 里拾取），允许界面里的平面地图 | ENG-ARCH、SYS-ATLAS |
| INV-2 | 只有两种观察模式：**旁观**（穿透一切）和**行走**（重力与碰撞）；不做环绕、俯视、导览、自动运镜；观者主动触发的跳转、传送、模式切换，以及开始页不算 | SYS-VIEW、SYS-WALK、SYS-ATLAS、SYS-TITLE |
| INV-3 | 允许的界面以 UI-NAV 和 UI-VISUAL 登记的为准；3D 画面里不叠加地点标签、选中描边、世界空间标记或光柱；OSM 署名常驻（数据许可的要求） | UI-NAV、UI-VISUAL、ENG-RELEASE |
| INV-4 | 场景里本来就有的字要保留（楼名题字、店招、路牌），一律用简体中文；受专门规定保护的标志（校徽、红十字等）不原样使用，换成风格相近的原创图形（D-065） | ART-STYLE |
| INV-5 | 三渲二日式动画背景，清透干净：分阶明暗，影子以明度为主、浅而透、不加接触暗影；随底色染色的细描线；平涂贴图。画面设置的风格底线不可关，阴影与水面倒影只能降质量 | ART-STYLE、ART-STYLE-LINES、SYS-GRAPHICS |
| INV-6 | 不出现 NPC 和他人，也不加人物剪影，人只以生活痕迹出现；玩家自己的角色是唯一例外（D-182）。展陈里不出现人物肖像；声音里没有听得清的人声 | WLD-LIFE、SYS-WALK、WLD-INTERIORS、SYS-AUDIO |
| INV-7 | 日本元素仅限日常街景；边界以 ART-STYLE 为准，物件与分区见 WLD-AMBIENCE；室内与声音同样适用 | ART-STYLE、WLD-AMBIENCE、WLD-INTERIORS、SYS-AUDIO |
| INV-8 | 世界状态只有时刻、季节、天气三个量。世界时间在一个实例内只向前，新游戏、读档是换实例，不算倒流；几何、碰撞和门的静止状态不随这三个量变；切换自然过渡，每种组合都要好看 | SYS-TIME、SYS-WEATHER、SYS-SAVE |
| INV-9 | 不是地图复刻：保留大体的平面格局（楼、路、水体的位置与形状），竖向地形主动设计（D-180），其余按氛围风格化；地图只改项目的世界文件并写明理由，不从 OSM 等外部地图取数据（D-252、D-253） | PRD-SCOPE §1、WLD-TERRAIN、SYS-MAPS、MAP-QSH-ADDITIONS |
| INV-10 | 所有动态和声音都轻、慢、持续；偶发事件极少，数量与例外以 SYS-MOTION-LIFE §3 为准 | SYS-MOTION、SYS-AUDIO |
| INV-11 | 看得见的东西在任何位置、任何距离都满足视觉精度的底线：比例对，有倒角和收边，没有光秃的拉伸体块，不穿帮、不跳变，外观全新不做旧，包括只有旁观飞行才到的地方；细节密度按精度区分级，越常被近看越密 | ART-FIDELITY、WLD-RENOVATION、WLD-OUTSKIRTS、WLD-INTERIORS |
| INV-12 | 满足性能预算与硬规则；安装包不设体积上限，按数量级看与内容是否相称（D-125） | ENG-BUDGET、ENG-BUDGET-RULES、ENG-PERF |
