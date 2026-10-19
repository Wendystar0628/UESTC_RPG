---
id: UI-INDEX
title: 界面总纲
category: ui
summary: 界面域的总纲：界面有什么、怎么操作，以及长什么样；本域通用原则与文档树（主题文档与子文档）
keywords: [界面, 界面总纲, 文档树, UI]
owners: []
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# UI-INDEX · 界面总纲

> **范围**：本文是界面域的入口：本域管界面有什么、怎么操作，以及长什么样；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的核心准则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **内容与外观分开**：有什么、怎么操作看 UI-NAV，长什么样看 UI-VISUAL。
2. **3D 画面里不叠标签**：允许的界面以本域登记的为准（INV-3）。

### 0.1 接入要点

- **加一个界面件**：先在 UI-NAV 登记，再在 UI-VISUAL 定外观。
- **在本域新增文档**：主题文档在 §1 加一行、文件头写 `parent: UI-INDEX`；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- UI-NAV 界面与操作说明
  - UI-NAV-WAYPOINTS 点位、跳转与传送
- UI-VISUAL 界面视觉语言
  - UI-VISUAL-COMPONENTS 界面字体、形状、图标、组件外观与画面布局
  - UI-VISUAL-MOTION 界面动效与减少动态效果
