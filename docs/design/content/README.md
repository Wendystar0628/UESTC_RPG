# content/ · CONT

内容生产：建模与贴图规范、素材来源与许可、导入设置、命名与目录、资产审核流程。

- **编码**：文件名即编码：`CONT-<主题>.md`，主题用大写英文单词，多个词用连字符，例如 `CONT-<主题>-<子主题>.md`。
- **约定**：小节编号从 0 开始、不带文档前缀；引用写成"编码 §小节"，见 CORE-CONVENTIONS。
- **当前为空**：按需新建文档，新建后在总索引 README.md 里登记一行。

## 文件头模板

```markdown
---
id: CONT-<主题>
title: <中文标题>
category: content
summary: <一句话，≤ 60 字>
owners: []
contributors: []
depends: []
related: []
contracts: []
data: []
gates: []
---

# CONT-<主题> · <中文标题>

> **范围**：……
> **不在本文**：……
> **关联**：……

## 0 结论
```
