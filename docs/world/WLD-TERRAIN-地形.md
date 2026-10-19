---
id: WLD-TERRAIN
title: 地形
category: world
parent: WLD-INDEX
summary: 不分图的地形规则目录：设计（原则、分层、竖直落差、水位岸型）与落地（合成、贴地、可走性、着色）
keywords: [地形, 地形规则]
owners: [WP-08, WP-15]
contributors: [WP-00, WP-02, WP-04, WP-05, WP-11, WP-13, WP-14, WP-16, WP-18, WP-19, WP-20, WP-21, WP-22, WP-23, WP-24, WP-25, WP-28, WP-30, WP-36, WP-38, WP-42, WP-43, WP-44, WP-47, WP-49]
depends: [CORE-INDEX, ENG-ARCH, SYS-WALK, ART-STYLE, ART-MODELING, ENG-PERF, ENG-BUDGET]
related: [WLD-WATER, WLD-OUTSKIRTS, WLD-OUTSKIRTS-DIORAMA, WLD-VEGETATION, WLD-INTERIORS, MAP-QSH-TERRAIN, MAP-QSH-ADDITIONS, ART-FIDELITY, ART-SKYLIGHT, SYS-VIEW, SYS-MAPS, ENG-GATES, ENG-PREVIEW, WLD-TERRAIN-SHAPE, WLD-TERRAIN-APPLY]
contracts: []
data: []
gates: []
---

# WLD-TERRAIN · 地形

> **范围**：本文是不分图地形规则的目录，规定地形分哪两块、各自什么时候读；规则本身在两份子文档。
> **不在本文**：清水河各层的分布、骨架、地标丘、各水体的常水位与各区的地形操作（MAP-QSH-TERRAIN）；水面的画法（WLD-WATER）；驳岸、桥、亲水台阶等构筑物的外观（各区 MAP-QSH 文档）；控制器参数（SYS-WALK）；地形接口与操作字段（`game/contracts/`）；性能份额（ENG-BUDGET）。
> **查阅**：先读 §0；设计原则、分层、竖直落差、水位与岸型看 WLD-TERRAIN-SHAPE；表达与合成、贴地、可走性、着色看 WLD-TERRAIN-APPLY

## 0 结论

1. **做法**：地形是"平缓底板 + 少量强垂直 + 遮挡与揭示"，写成按区的矢量操作，合成为确定性纯函数 H(x, z)，所有包读同一个地形接口（D-180）。
2. **分两块**：先定设计，再落到画面和碰撞；两块读者不同，规则都在子文档。

| 子文档 | 简题 | 什么时候读 |
| --- | --- | --- |
| WLD-TERRAIN-SHAPE | 地形设计 | 设计一处地形、做台阶坡道台缘带、画水面或做岸、核校界（含设计原则、分层、手感刻度、坡度类、水位与岸型） |
| WLD-TERRAIN-APPLY | 地形落地 | 读地面高度、贴地、放楼放树、写碰撞、改地形文件（含矢量操作与合成、可走性、着色与描线） |

### 0.1 接入要点

- 本文是目录，对外要求在子文档的接入要点。

