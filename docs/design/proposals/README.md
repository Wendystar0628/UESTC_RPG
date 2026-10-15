# proposals/ · RFC

开发中临时发现的设计提案。评审通过后把结果合进对应的规格文档，提案标为 merged；未采纳的标 rejected。规格文档不引用提案。

- **编码**：文件名即编码：`RFC-<领域>-<短名>.md`。领域取规格文档的前缀（CORE、ART、WLD、MAP、SYS、UI、ENG、GAME、NARR、CHAR、CONT、TOOL、QA、OPS、L10N），短名用大写英文单词和连字符，在同一领域内唯一。状态写在文件头的 `status`：draft | review | merged | rejected。
- **约定**：小节编号从 0 开始、不带文档前缀；引用写成"编码 §小节"，见 CORE-CONVENTIONS。
- **当前为空**：按需新建文档，新建后在总索引 README.md 里登记一行。

## 文件头模板

```markdown
---
id: RFC-<领域>-<短名>
title: <中文标题>
category: proposals
status: draft
domain: <CORE|ART|WLD|MAP|SYS|UI|ENG|GAME|NARR|CHAR|CONT|TOOL|QA|OPS|L10N>
affects: []          # 受影响的规格文档编码
owners: []
created: <YYYY-MM-DD>
---

# RFC-<领域>-<短名> · <中文标题>
```
