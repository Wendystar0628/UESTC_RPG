---
id: PLAN-INDEX
title: 计划总纲
category: plan
summary: 计划域的总纲：工作包、阶段与主线闸口；本域通用原则与文档树（主题文档与子文档）
keywords: [计划, 计划总纲, 文档树, PLAN]
owners: []
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# PLAN-INDEX · 计划总纲

> **范围**：本文是计划域的入口：本域管工作包、阶段与主线闸口；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的核心准则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **这里只写划分与阶段**：每个包做什么、依赖谁、主线版交什么；施工状态与队列在 WF-STATUS 和 `.collab/`。

### 0.1 接入要点

- **加或拆工作包**：同批改 PLAN-WP-LIST 和 `ownership.json`。
- **在本域新增文档**：主题文档在 §1 加一行、文件头写 `parent: PLAN-INDEX`；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- PLAN-WP 工作包派工单
  - PLAN-WP-LIST 工作包一览与子包拆法
