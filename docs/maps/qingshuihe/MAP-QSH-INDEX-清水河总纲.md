---
id: MAP-QSH-INDEX
title: 清水河总纲
category: maps
summary: 清水河校区总纲：本域通用原则与文档树
keywords: [清水河, 清水河总纲, 文档树, MAP-QSH, 默认状态, 区文档三件套, 主线之后, 跨区主题, 世界文件, 数据位置]
owners: []
paths: ["docs/maps/qingshuihe/**"]
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# MAP-QSH-INDEX · 清水河总纲

> **范围**：本文是清水河域的入口：本域管清水河校区的具体内容（地形、设计增补、夜间灯光与雨天、翻新、植被、水体、外围街区、开放楼室内，以及五个区），列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；不分图的世界与画面规则（WLD-INDEX、ART-INDEX）；全项目的顶层规则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **本域只放清水河的具体内容**：不分图的规则放世界域、画面域和系统域的对应文档，本域文档引用它们，不重抄。
2. **数据在哪**：楼、路、水体的位置与实体 ID 在 `game/data/maps/qingshuihe/world/`，点位在 `waypoints.json`，开放室内在 `interiors.json`，灯光调度在 `lighting-schedule.json`，平面地图在 `atlas/`；文档写设计，以这些数据为准；取数规则见 INV-9。
3. **默认状态是秋 17:30**：各文档外观部分写的是这个状态；区文档四季表的"秋"一列就是该区主题文档写的现状，"—"或"同秋"表示相同；四季昼夜表用作用代号（MAP-QSH-ADDITIONS §2、§8.2）。
4. **"主线之后"**：标题或节末带"（主线之后）"的内容现在不施工，含义见 STD-DOC §2；区文档的 SEASONS、WALK 大多属此。
5. **区文档三件套**：每个区一份主题文档写外观与身份特征，SEASONS 子文档写四季昼夜与天气，WALK 子文档写行走与声音；三者的共同口径在 MAP-QSH-ADDITIONS §8，区文档只写本区。
6. **增补与翻新的登记**：ID 以 `design:` 开头并写 `note`，写法统一见 MAP-QSH-ADDITIONS §1，本域其他文档只指向它。

### 0.1 接入要点

- **改某个区或某栋楼**：先读本区主题文档，跨区规则看对应的主题文档。
- **在本域新增文档**：主题文档文件头写 `parent: MAP-QSH-INDEX`，在 §1 加一行；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和本总纲 §1 那个主题下（STD-DOC §5.1、§7）。

## 1 文档树

跨区主题文档在前，按编码字母序；区文档在后，按核心区、南区、西区、北部、中部与东区的顺序（与 MAP-QSH-ADDITIONS §8 一致）。缩进的是子文档；只列编码与标题。

- MAP-QSH-ADDITIONS 设计增补
  - MAP-QSH-ADDITIONS-VOCAB 增补词汇与全校摆放
- MAP-QSH-ATMOSPHERE 夜间灯光与雨天
- MAP-QSH-INTERIORS 开放楼室内与开放的高处
  - MAP-QSH-INTERIORS-LIBRARY 图书馆与博物馆室内
- MAP-QSH-OUTSKIRTS 清水河外围街区
- MAP-QSH-RENOVATION 清水河翻新例外与样板
- MAP-QSH-TERRAIN 全图地形骨架
  - MAP-QSH-TERRAIN-CHECKS 地形验收与点位标高
  - MAP-QSH-TERRAIN-DISTRICTS 十处分区地形
- MAP-QSH-VEGETATION 清水河植被
- MAP-QSH-WATER 清水河水体
- MAP-QSH-CORE 核心区
  - MAP-QSH-CORE-SEASONS 核心区灯光、四季昼夜与天气
  - MAP-QSH-CORE-WALK 核心区行走与声音
- MAP-QSH-SOUTH 南区
  - MAP-QSH-SOUTH-SEASONS 南区四季昼夜与天气
  - MAP-QSH-SOUTH-WALK 南区行走与声音
- MAP-QSH-WEST 西区与西北区
  - MAP-QSH-WEST-SEASONS 西区与西北区四季昼夜与天气
  - MAP-QSH-WEST-WALK 西区与西北区行走与声音
- MAP-QSH-NORTH 北部生活区
  - MAP-QSH-NORTH-SEASONS 北部生活区四季昼夜与天气
  - MAP-QSH-NORTH-WALK 北部生活区行走与声音
- MAP-QSH-CENTER-EAST 中部与东区
  - MAP-QSH-CENTER-EAST-SEASONS 中部与东区四季昼夜与天气
  - MAP-QSH-CENTER-EAST-WALK 中部与东区行走与声音
