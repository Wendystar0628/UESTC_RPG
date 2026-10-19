---
id: SYS-INDEX
title: 系统总纲
category: systems
summary: 系统域的总纲：运行时各系统怎么工作：观察模式、行走、时间、天气、动态、存档、多地图、平面地图与传送、开始页、画面设置、音频；本域通用原则与文档树（主题文档与子文档）
keywords: [系统, 系统总纲, 文档树, SYS]
owners: []
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# SYS-INDEX · 系统总纲

> **范围**：本文是系统域的入口：本域管运行时各系统怎么工作：观察模式、行走、时间、天气、动态、存档、多地图、平面地图与传送、开始页、画面设置、音频；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的核心准则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **写行为与边界，不写实现**：数值以代码和数据为准，接口签名以 `game/contracts/` 为准。
2. **世界状态只有时刻、季节、天气**，时间只向前（INV-8）；只有旁观与行走两种模式（INV-2）。

### 0.1 接入要点

- **改一个系统的行为**：先读它的主文档 §0，再读改到的子文档。
- **在本域新增文档**：主题文档在 §1 加一行、文件头写 `parent: SYS-INDEX`；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- SYS-ATLAS 平面地图与传送
  - SYS-ATLAS-TRAVEL 标记、锚点与传送
  - SYS-ATLAS-UI 地图界面
- SYS-AUDIO 音频
  - SYS-AUDIO-AMBIENCE 环境声景与天气事件声
  - SYS-AUDIO-FOLEY 观者声与空间声学
- SYS-GRAPHICS 画面设置
- SYS-MAPS 多地图
- SYS-MOTION 动态与事件
  - SYS-MOTION-ENV 风、飘落、远景与季节氛围
  - SYS-MOTION-LIFE 生命体、室内微动与偶发事件
- SYS-SAVE 存档与持久化
  - SYS-SAVE-FLOW 保存时机与读档流程
  - SYS-SAVE-FORMAT 格式、损坏处理与安全
- SYS-TIME 时间与季节系统
- SYS-TITLE 开始页与启动
- SYS-VIEW 观察模式与旁观漫游
  - SYS-VIEW-MEDIUM 介质呈现
- SYS-WALK 行走模式
  - SYS-WALK-BIKE 骑行
  - SYS-WALK-BOUNDS 可行走范围、边界与碰撞
- SYS-WEATHER 天气
  - SYS-WEATHER-PROCESS 自动天气与存档语义
