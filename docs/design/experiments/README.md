# experiments/ · EXP

原型与实测报告，主要回答各规格文档"待验证（P0）"一节的问题。结论确定后，把定下的数值写回规格文档。

- **编码**：文件名即编码：`EXP-<领域>-<短名>.md`。领域取规格文档的前缀（CORE、ART、WLD、MAP、SYS、UI、ENG、GAME、NARR、CHAR、CONT、TOOL、QA、OPS、L10N），短名用大写英文单词和连字符，在同一领域内唯一。状态写在文件头的 `status`：planned | running | concluded。
- **约定**：小节编号从 0 开始、不带文档前缀；引用写成"编码 §小节"，见 CORE-CONVENTIONS。
- **当前为空**：按需新建文档，新建后在总索引 README.md 里登记一行。

## 文件头模板

```markdown
---
id: EXP-<领域>-<短名>
title: <中文标题>
category: experiments
status: planned
domain: <CORE|ART|WLD|MAP|SYS|UI|ENG|GAME|NARR|CHAR|CONT|TOOL|QA|OPS|L10N>
affects: []          # 受影响的规格文档编码
owners: []
created: <YYYY-MM-DD>
---

# EXP-<领域>-<短名> · <中文标题>
```
