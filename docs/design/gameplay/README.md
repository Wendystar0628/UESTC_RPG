# gameplay/ · GAME

RPG 玩法规则：交互、任务、背包、成长、经济、日程与日历。观光模式的规则集也在这里登记扩展。

- **编码**：文件名即编码：`GAME-<主题>.md`，主题用大写英文单词，多个词用连字符，例如 `GAME-<主题>-<子主题>.md`。
- **约定**：小节编号从 0 开始、不带文档前缀；引用写成"编码 §小节"，见 CORE-CONVENTIONS。
- **当前为空**：按需新建文档，新建后在总索引 README.md 里登记一行。

## 文件头模板

```markdown
---
id: GAME-<主题>
title: <中文标题>
category: gameplay
summary: <一句话，≤ 60 字>
owners: []
contributors: []
depends: []
related: []
contracts: []
data: []
gates: []
---

# GAME-<主题> · <中文标题>

> **范围**：……
> **不在本文**：……
> **关联**：……

## 0 结论
```
