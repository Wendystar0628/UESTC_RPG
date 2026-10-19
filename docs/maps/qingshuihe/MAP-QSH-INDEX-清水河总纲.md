---
id: MAP-QSH-INDEX
title: 清水河总纲
category: maps
summary: 清水河域的总纲：清水河校区的具体内容：地形骨架、设计增补、翻新清单、植被、水体、外围街区、室内，以及五个区；本域通用原则与文档树（主题文档与子文档）
keywords: [清水河, 清水河总纲, 文档树, MAP-QSH]
owners: []
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# MAP-QSH-INDEX · 清水河总纲

> **范围**：本文是清水河域的入口：本域管清水河校区的具体内容：地形骨架、设计增补、翻新清单、植被、水体、外围街区、室内，以及五个区；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的核心准则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **数据以世界文件为准**：楼、路、水体的位置与实体 ID 以 `game/data/maps/qingshuihe/world/` 为准；地图只改世界文件并写明理由，不从 OSM 等外部地图取数据（INV-9）。
2. **区文档三件套**：每个区一份主文档写外观与身份特征，SEASONS 子文档写四季昼夜与天气，WALK 子文档写行走与声音。
3. **跨区主题各一份**：地形、增补、翻新、植被、水体、外围、室内各有一份，区文档只写本区的特例。

### 0.1 接入要点

- **改某个区或某栋楼**：先读本区主文档，跨区规则看对应主题文档。
- **在本域新增文档**：主题文档在 §1 加一行、文件头写 `parent: MAP-QSH-INDEX`；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- MAP-QSH-ADDITIONS 设计增补
  - MAP-QSH-ADDITIONS-ATMOSPHERE 夜间灯光总图与雨天增补
  - MAP-QSH-ADDITIONS-VOCAB 设计增补词汇与楼宇规则
- MAP-QSH-CORE 核心区
  - MAP-QSH-CORE-SEASONS 核心区灯光、四季昼夜与天气
  - MAP-QSH-CORE-WALK 核心区行走与声音
- MAP-QSH-INTERIORS 其余开放楼室内与开放的高处
  - MAP-QSH-INTERIORS-LIBRARY 图书馆与博物馆室内
- MAP-QSH-NORTH 北部生活区
  - MAP-QSH-NORTH-SEASONS 北部生活区四季昼夜与天气
  - MAP-QSH-NORTH-WALK 北部生活区行走与声音
- MAP-QSH-OUTSKIRTS 清水河外围街区
- MAP-QSH-RENOVATION 清水河翻新清单
- MAP-QSH-SOUTH 南区
  - MAP-QSH-SOUTH-SEASONS 南区四季昼夜与天气
  - MAP-QSH-SOUTH-WALK 南区行走与声音
- MAP-QSH-TERRAIN 全图地形骨架
  - MAP-QSH-TERRAIN-CHECKS 地形验收镜头、冲突检查与点位标高
  - MAP-QSH-TERRAIN-DISTRICTS 十处分区地形
- MAP-QSH-VEGETATION 清水河植被
- MAP-QSH-WATER 清水河水体
- MAP-QSH-WEST 西区与西北区
  - MAP-QSH-WEST-SEASONS 西区与西北区四季昼夜与天气
  - MAP-QSH-WEST-WALK 西区与西北区行走与声音
- MAP-QSH-CENTER-EAST 中部与东区
  - MAP-QSH-CENTER-EAST-SEASONS 中部与东区四季昼夜与天气
  - MAP-QSH-CENTER-EAST-WALK 中部与东区行走与声音
