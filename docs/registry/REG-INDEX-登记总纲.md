---
id: REG-INDEX
title: 登记总纲
category: registry
summary: 登记域总纲：按编号或按词查的登记：决策与术语
keywords: [登记, 登记总纲, 文档树, REG, 决策, 术语表, D 编号]
owners: []
paths: ["docs/registry/**"]
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# REG-INDEX · 登记总纲

> **范围**：本文是登记域的入口：本域管按编号或按词查的登记：决策与术语；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的顶层规则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **决策按 D 编号查**（REG-DECISIONS），术语按词查（REG-GLOSSARY）；规则本身以各条指向的文档为准。
2. **每条只写一行**，不受单份体量上限约束。
3. **登记只记现行结果**：决策有变时改写原行，不写来由与过程；术语写统一的解释，规则细节在它指向的文档。

### 0.1 接入要点

- **登记新决策**：先登记再改相关文档（STD-DOC §8）。
- **在本域新增文档**：主题文档在 §1 加一行、文件头写 `parent: REG-INDEX`；子文档写 `parent: <主题文档编码>`，列进主题文档的 §0 和本总纲的 §1 对应主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- REG-DECISIONS 决策登记
  - REG-DECISIONS-PROCESS 协作与流程决策
  - REG-DECISIONS-SYSTEMS 系统与工程决策
  - REG-DECISIONS-WORLD 世界与画面决策
- REG-GLOSSARY 术语表
