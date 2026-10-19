---
id: WLD-LIFE
title: 动物、生活痕迹与街道家具
category: world
summary: 动物、生活痕迹、停着的车与街道家具的目录：共同底线，动物痕迹与车家具分子文档
keywords: [动物与生活痕迹总则, 艺术化处理, 世界里不画人]
owners: [WP-25, WP-13]
contributors: [WP-12, WP-16, WP-20, WP-26, WP-39, WP-42, WP-43, WP-44, WP-48a, WP-48c, WP-49b, WP-52, WP-53, WP-55]
depends: [CORE-INDEX, ART-STYLE, ART-SKYLIGHT, ENG-ARCH, SYS-TIME, SYS-WALK, SYS-WEATHER]
related: [MAP-QSH-CORE, MAP-QSH-LIBRARY, MAP-QSH-SOUTH, MAP-QSH-NORTH, MAP-QSH-CENTER-EAST, WLD-VEGETATION, WLD-WATER, SYS-MOTION, ART-FIDELITY, MAP-QSH-ADDITIONS, ENG-BUDGET, ENG-WP, ENG-PERF, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO, WLD-LIFE-FAUNA, WLD-LIFE-STREET]
contracts: []
data: []
gates: []
---

# WLD-LIFE · 动物、生活痕迹与街道家具

> **范围**：本文规定全校动物、室外生活痕迹、停着的车和街道家具共同遵守的底线与艺术化处理，并说明分哪两块、各自什么时候读。
> **不在本文**：动物与布料的动作节奏（SYS-MOTION）；图书馆与博物馆室内的道具（MAP-QSH-LIBRARY）；其余开放室内的家具与痕迹（WLD-INTERIORS）；路灯亮度曲线（SYS-TIME-LOOK §3）；导览亭与导览桩的外观与放置（SYS-ATLAS-TRAVEL §2）；发声（SYS-AUDIO）。
> **查阅**：先读 §0；动物、生活痕迹、显隐看 WLD-LIFE-FAUNA；停着的车、街道家具、碰撞看 WLD-LIFE-STREET

## 0 结论

1. **不画人**：NPC 和他人只以痕迹和远处听不清的声音出现；玩家自己的角色是唯一例外，只在行走模式下出现（INV-6）。
2. **物件真实、状态全新**：生活感靠干净整齐的痕迹，不靠磨损、污渍和杂物；物件状态遵守 D-174（ART-FIDELITY-DETAIL §1.5）；车和家具上不画可辨认的品牌标志（同 MAP-QSH-NORTH §3.3 店招），`uestc.blue` 只给旗面、校车和单车（ART-STYLE §2.2）。
3. **艺术化处理**：照片和资料只定结构与分布，颜色、涂装、标识和疏密按画面需要处理；可以删现实里有但难看的东西（临时围挡、刺眼广告、贴满小广告的灯杆和电箱、过多的指示牌、成堆的废弃单车），可以加校园里没有但加强氛围的家具和痕迹，加进世界文件并写 `note`（MAP-QSH-ADDITIONS §1）。
4. **分两块**：动物与痕迹归 FAUNA，车与家具归 STREET；显隐做法在 FAUNA，两块都按它做。

| 子文档 | 管什么 | 什么时候读 |
| --- | --- | --- |
| WLD-LIFE-FAUNA 动物与生活痕迹 | 动物、反应动物、室外生活痕迹、显隐做法 | 放动物、做随时段季节天气变的物件、写痕迹 |
| WLD-LIFE-STREET 停着的车与街道家具 | 停着的车与共享单车、街道家具、行走碰撞 | 放车和家具、给部件做碰撞 |

### 0.1 接入要点

- 本文是目录，对外要求在子文档的接入要点。

