---
id: WF-INDEX
title: 协作总纲
category: workflow
summary: 开发期多个模型与会话怎么协作的总纲：角色表与各角色入口、决定权与执行人、规则放哪份的判定表
keywords: [协作总纲, 多模型协作, 角色表, 角色入口, 作者, 主会话, 主会话的子代理, Astra, Astra 的子代理, Sol 会话, 决定权, 执行人, 判定表]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX]
related: [WF-ENTRY, WF-ROLE, WF-MODEL, WF-COMMS, WF-FLOW, WF-QUOTA, WF-STATUS, WF-RULE, WF-BUILD, WF-DOC, WF-ONBOARD, ENG-WP, CORE-DECISIONS]
contracts: []
data: []
gates: []
---

# WF-INDEX · 协作总纲

> **范围**：本文是 `docs/design/workflow/` 的总纲：协作有哪些角色、各自的入口在哪、谁决定谁执行、一条规则该放哪份文档。
> **不在本文**：各角色入口：WF-ENTRY；各项细则在各分册（文档地图见 DOCMAP）；包的划分、独占目录与交付：ENG-WP；此刻的施工状态：施工状态文件（WF-STATUS）。
> **查阅**：先读 §0；角色表与各角色入口看 §1；谁决定、谁执行看 §2；新规则放哪份看 §4。
> **关联**：[WF-ROLE 参与者与分工](collaboration/WF-ROLE-参与者与分工.md)、[WF-MODEL 模型选择与子代理](collaboration/WF-MODEL-模型选择与子代理.md)、[WF-COMMS 交接与通信](collaboration/WF-COMMS-交接与通信.md)、[WF-FLOW 开发流程](process/WF-FLOW-开发流程.md)、[WF-QUOTA 账号与额度](management/WF-QUOTA-账号与额度.md)、[WF-STATUS 施工状态与队列](management/WF-STATUS-施工状态与队列.md)、[WF-RULE 全员守则](rules/WF-RULE-全员守则.md)、[WF-BUILD 施工边界](rules/WF-BUILD-施工边界.md)、[WF-DOC 文档写法](rules/WF-DOC-文档写法.md)、[WF-ONBOARD 子代理开工手册](WF-ONBOARD-子代理开工手册.md)、[WF-ENTRY 角色入口卡](WF-ENTRY-角色入口卡.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[CORE-DECISIONS 决策登记](../core/CORE-DECISIONS-决策登记.md)

## 0 结论

1. **作者拍板，主会话总指挥**（§1、§2）：主会话是唯一的主代理，只做判断；施工者自测后自己提交推送。
2. **各角色的入口只在 WF-ENTRY**：§1 的表只记模型、运行方式、汇报给谁和入口在哪一节；会话怎么运行见 WF-ROLE §1。
3. **每条规则只有一个家**（§4）：全员守则在 WF-RULE，改规则只改它的家。

### 0.1 接入要点

- **新接手的任何角色**：读 WF-ENTRY 里自己那一节，其余按 DOCMAP 和 §4 的判定表查。
- **要新加或改一条协作规则**：按 §4 的判定表找到它的家，只改那一处，别处只给指向。
- **写任务单、简报、派工消息**：规则不抄，写"见 WF-RULE §n"；入口只指向 WF-ENTRY。

## 1 角色表

协作由一位作者和下表的会话组成。评审里的"Opus"指主会话；"Opus 侧"指主会话和它开的 Claude 侧施工。各角色做什么见 WF-ROLE。模型、会话名与调用方式的操作细节（含会话号）在 `E:\Project\UESTC_RPG.wt\.collab\tools\README.md`，不写进文档。

| 角色 | 模型与运行方式 | 汇报给 | 入口 |
| --- | --- | --- | --- |
| 作者 | — | — | — |
| 主会话 | Claude Opus 5.5，Claude Code 交互会话 | 作者 | WF-ENTRY 主会话 |
| 主会话的子代理 | Claude Opus 或 Sonnet 按任务单，Agent 工具后台开，按包取名 | 主会话 | WF-ENTRY 子代理 |
| Astra | gpt-6-astra，xhigh，Codex 应用里的会话 | 主会话 | WF-ENTRY Astra |
| Astra 的子代理 | 默认 gpt-6.1-sol，`spawn_agent` 开；gpt-6-astra 子代理只在 WF-ROLE §6 列的情形开 | Astra | WF-ENTRY 子代理 |
| Sol 会话 | gpt-6.1-sol，xhigh，普通速度，Codex 应用里的会话；调研、旁路、机械活（机械活可开 gpt-6-luna 子代理） | 主会话 | WF-ENTRY Sol 会话 |

- 读文件时的"末尾几行"指只读文件最后几十行；`archive/`、`logs/` 其余内容、别的角色的收件箱不用读（WF-COMMS §1）。

## 2 决定权与执行人

**主会话决定**：派工与分工；评审后留下还是回滚；施工状态的内容；`docs/design/` 改什么；与作者沟通。

**施工者自己提交推送**：自测通过就在主检出 `main` 上提交并推送，出问题用 `git revert` 回滚（WF-RULE §7）；`main` 上同一批文件同一时间只有一个写者（WF-FLOW §2.1）。设计文档的撰写可以派出去：需要 Opus 的交 Opus 子代理，Sol 只在任务单给出确切文字时按文打补丁。

**其他各方定什么**：Astra 定契约、共享工程文件和工程上的取舍（WF-FLOW §5）；画面与声音的取值、风格定案，以主会话的评审为准（WF-ROLE §3）。

## 4 一条规则放哪份

按"这条规则回答什么问题"放，不按谁来读（各分册管什么，见 DOCMAP 的摘要）：

| 回答的问题 | 放哪份 |
| --- | --- |
| 谁做什么、谁定 | WF-ROLE（角色表在本文 §1） |
| 选哪个模型、怎么开子代理 | WF-MODEL |
| 文件怎么命名、怎么写、放哪、来信怎么发 | WF-COMMS |
| 一件事怎么走（派工到评审）、防打架、调研与旁路 | WF-FLOW |
| git、推送、回滚 | WF-RULE §7 |
| 多少、何时（额度、并发、节奏） | WF-QUOTA |
| 此刻的状态、队列、交接 | WF-STATUS |
| 必须与禁止（全员） | WF-RULE |
| 必须与禁止（改哪里、代码与内容） | WF-BUILD |
| 文档怎么写、怎么引用 | WF-DOC |
| 开工环境、查资料、回报格式 | WF-ONBOARD |
| 各角色的入口与必守 | WF-ENTRY |
| 决策记录 | CORE-DECISIONS 只留一句加落点 |

改规则只改它的家；钩子文字是 WF-RULE §1 说明的唯一例外。
