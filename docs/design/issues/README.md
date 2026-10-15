# issues/ · ISS

开发中发现的问题：现象、复现、影响范围、处理方式。修复后如果改变了设计，结果写进规格文档。

- **编码**：文件名即编码：`ISS-<领域>-<短名>.md`。领域取规格文档的前缀（CORE、ART、WLD、MAP、SYS、UI、ENG、GAME、NARR、CHAR、CONT、TOOL、QA、OPS、L10N），短名用大写英文单词和连字符，在同一领域内唯一。状态写在文件头的 `status`：open | fixing | closed | wontfix。
- **约定**：小节编号从 0 开始、不带文档前缀；引用写成"编码 §小节"，见 CORE-CONVENTIONS。
- **当前为空**：按需新建文档，新建后在总索引 README.md 里登记一行。

## 文件头模板

```markdown
---
id: ISS-<领域>-<短名>
title: <中文标题>
category: issues
status: open
domain: <CORE|ART|WLD|MAP|SYS|UI|ENG|GAME|NARR|CHAR|CONT|TOOL|QA|OPS|L10N>
affects: []          # 受影响的规格文档编码
owners: []
created: <YYYY-MM-DD>
---

# ISS-<领域>-<短名> · <中文标题>
```
