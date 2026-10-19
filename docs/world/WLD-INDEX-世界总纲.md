---
id: WLD-INDEX
title: 世界总纲
category: world
summary: 不分图的世界内容规则的入口：地形、水体、植被、室内、外围、动物与街道家具、日式街景、老楼翻新
keywords: [世界, 世界总纲, 文档树, WLD]
owners: []
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# WLD-INDEX · 世界总纲

> **范围**：本文是世界域的入口：本域管不分图的世界内容规则：地形、水体、植被、室内、边界与外围、动物与街道家具、日式街景、老楼翻新；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；某张图的具体内容（各图总纲，清水河是 MAP-QSH-INDEX）；全项目的核心准则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **只写不分图的规则**：本域写对所有图都成立的做法，不写某张图的楼、地名、树种位置和数值清单；这些写进那张图的文档，本域文档只在需要处给一句指向。
2. **几何不随时刻、季节、天气变**：地形、碰撞、家具位置和门的静止状态在构建时定死；变化只走调色板、全局参数和可见性显隐（INV-8）。
3. **增补与改动写进世界文件或覆盖层**：新增物的 ID 以 `design:` 开头并写理由，删除就是删记录（INV-9）。
4. **世界里不画人**：生活只以干净的痕迹出现；外观全新、不做旧；日式元素只限日常街景（INV-6、INV-7、INV-11）。
5. **默认状态是秋 17:30**：本域描写的光色与季相是默认状态的样子，其余时刻和季节可调，一律视觉效果优先。
6. **"主线之后"**：标题或节末带这个词的内容现在不施工（STD-DOC §2）。

### 0.1 接入要点

- **要写某张图的具体内容**：写进那张图的文档，本域只留通用规则。
- **在本域新增文档**：主题文档写 `parent: WLD-INDEX` 并列进 §1；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和本总纲 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- WLD-AMBIENCE 日式日常街景
- WLD-INTERIORS 室内
  - WLD-INTERIORS-ACCESS 室内开放
  - WLD-INTERIORS-BUILD 室内制作
- WLD-LIFE 动物、生活痕迹与街道家具
  - WLD-LIFE-FAUNA 动物与生活痕迹
  - WLD-LIFE-STREET 停着的车与街道家具
- WLD-OUTSKIRTS 边界与外围
  - WLD-OUTSKIRTS-BAND 外围环境带
  - WLD-OUTSKIRTS-DIORAMA 箱庭截面与远景
- WLD-RENOVATION 老楼翻新
- WLD-TERRAIN 地形
  - WLD-TERRAIN-APPLY 地形落地
  - WLD-TERRAIN-SHAPE 地形设计
- WLD-VEGETATION 植被
- WLD-WATER 水体
