---
id: WLD-INTERIORS
title: 室内
category: world
parent: WLD-INDEX
summary: 不分图的室内规则目录：开放（原则、门、关着的楼、新开一栋）与制作（加载、生成器、风格、痕迹、碰撞）
keywords: [室内, 精选开放]
depends: [CORE-INDEX, ART-STYLE, MAP-QSH-INTERIORS-LIBRARY, SYS-VIEW, ART-FIDELITY, ART-MODELING, ENG-ARCH, ENG-BUDGET, ENG-PERF, SYS-TIME, SYS-WALK, SYS-ATLAS, SYS-MAPS, SYS-AUDIO]
related: [WLD-LIFE, WLD-OUTSKIRTS-DIORAMA, WLD-OUTSKIRTS, SYS-MOTION, SYS-WEATHER, SYS-SAVE, ENG-GATES, UI-NAV, ENG-ROADMAP, MAP-QSH-ADDITIONS, ART-SKYLIGHT, WLD-INTERIORS-ACCESS, WLD-INTERIORS-BUILD]
contracts: []
data: []
gates: []
paths: ["game/gen/maps/qingshuihe/core/LibraryInterior/**", "game/gen/maps/qingshuihe/core/tests/LibraryInteriorTests.cs", "game/gen/maps/qingshuihe/west/SixInterior/**", "game/style/palette/ext/openings.json", "game/style/palette/ext/roof_equipment.json", "game/gen/parts/circulation/**", "game/gen/parts/openings/**", "game/gen/parts/roof/**", "game/style/shaders/window_card.gdshaderinc", "game/style/shaders/window_card.gdshaderinc.uid", "game/gen/interior/shell/**", "game/gen/interior/core/**"]
---

# WLD-INTERIORS · 室内

> **范围**：本文是不分图室内规则的目录，规定室内规则分哪两块、各自什么时候读；规则本身在两份子文档。
> **不在本文**：清水河各楼的开放范围、陈设与观景时刻（MAP-QSH-INTERIORS-LIBRARY，含开放楼总表）；开放的屋顶与高处、电梯（MAP-QSH-INTERIORS）；各楼外观（各区 MAP-QSH 文档）；行走控制器（SYS-WALK）；楼内介质（SYS-VIEW）；声音（SYS-AUDIO）；地图门口传送（SYS-ATLAS）；契约（`game/contracts/`）；室内预算（ENG-BUDGET）。
> **查阅**：先读 §0；开放、门、关着的楼、新开一栋看 WLD-INTERIORS-ACCESS；加载、生成器、风格、痕迹、楼梯与碰撞看 WLD-INTERIORS-BUILD

## 0 结论

室内是精选开放，不全量开放；规则分开放与制作两块，读者不同。

| 子文档 | 简题 | 什么时候读 |
| --- | --- | --- |
| WLD-INTERIORS-ACCESS | 室内开放 | 判断一栋楼能不能进、做门、画关着的楼、扩大开放范围（含开放原则、门与自动门） |
| WLD-INTERIORS-BUILD | 室内制作 | 做室内几何、加家具痕迹、写室内碰撞、调加载（含生成器、风格与光） |

### 0.1 接入要点

- 本文是目录，对外要求在子文档的接入要点。

