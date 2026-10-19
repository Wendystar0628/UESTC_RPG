---
id: WLD-INTERIORS
title: 室内：开放与制作
category: world
summary: 不分图的室内规则目录：开放（原则、门、关着的楼、新开一栋）与制作（加载、生成器、风格、碰撞）
keywords: [室内, 精选开放]
owners: [WP-51, WP-52, WP-17, WP-31a, WP-31b, WP-31c]
contributors: [WP-00, WP-02, WP-03, WP-04, WP-07, WP-08, WP-10, WP-11, WP-12, WP-13, WP-14, WP-16, WP-18, WP-19, WP-20, WP-21, WP-26, WP-27, WP-33, WP-35, WP-36, WP-37, WP-39, WP-42, WP-43, WP-44, WP-48, WP-53]
depends: [CORE-INDEX, ART-STYLE, MAP-QSH-LIBRARY, SYS-VIEW, ART-FIDELITY, ART-MODELING, ENG-ARCH, ENG-BUDGET, ENG-PERF, SYS-TIME, SYS-WALK, SYS-ATLAS, SYS-MAPS, SYS-AUDIO]
related: [WLD-LIFE, WLD-DIORAMA, WLD-OUTSKIRTS, SYS-MOTION, SYS-WEATHER, SYS-SAVE, ENG-GATES, ENG-WP, UI-NAV, ENG-ROADMAP, MAP-QSH-ADDITIONS, ART-SKYLIGHT, WLD-INTERIORS-ACCESS, WLD-INTERIORS-BUILD]
contracts: []
data: []
gates: []
---

# WLD-INTERIORS · 室内：开放与制作

> **范围**：本文是不分图室内规则的目录，规定室内规则分哪两块、各自什么时候读；规则本身在两份子文档。
> **不在本文**：清水河各楼的开放范围、陈设与观景时刻（MAP-QSH-LIBRARY，含开放楼总表）；开放的屋顶与高处、电梯（MAP-QSH-INTERIORS）；各楼外观（各区 MAP-QSH 文档）；行走控制器（SYS-WALK）；楼内介质（SYS-VIEW）；声音（SYS-AUDIO）；地图门口传送（SYS-ATLAS）；契约（`game/contracts/`）；室内预算（ENG-BUDGET）。
> **查阅**：先读 §0；开放、门、关着的楼、新开一栋看 WLD-INTERIORS-ACCESS；加载、生成器、风格、痕迹、楼梯与碰撞看 WLD-INTERIORS-BUILD

## 0 结论

室内是精选开放，不全量开放；规则分开放与制作两块，读者不同。

| 子文档 | 管什么 | 什么时候读 |
| --- | --- | --- |
| WLD-INTERIORS-ACCESS 室内开放 | 开放原则与共用约定、门（含自动门）、关着的楼、导航图与新开一栋楼 | 判断一栋楼能不能进、做门、画关着的楼、扩大开放范围 |
| WLD-INTERIORS-BUILD 室内制作 | 加载与进楼衔接、生成器、风格与光、生活痕迹、楼梯门洞与碰撞 | 做室内几何、加家具痕迹、写室内碰撞、调加载 |

### 0.1 接入要点

- 本文是目录，对外要求在子文档的接入要点。

