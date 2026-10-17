---
id: WF-INDEX
title: 协作总纲
category: workflow
summary: 开发期多个模型与会话怎么协作的总纲：角色表与各角色先读什么、决定权与执行人、各分册索引、规则放哪份的判定表
keywords: [协作总纲, 多模型协作, 角色表, 先读什么, 作者, 主会话, 主会话的子代理, Astra, Astra 的子代理, 值班执行者, Sol 值班会话, Sol 旁路会话, Grok, Gemini, luna, 决定权, 执行人, 分册索引, 判定表, 单账户模式]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX]
related: [WF-ROLE, WF-MODEL, WF-COMMS, WF-DUTY, WF-FLOW, WF-MERGE, WF-QUOTA, WF-STATUS, WF-RULE, WF-BUILD, WF-DOC, WF-ONBOARD, ENG-WP, CORE-DECISIONS]
contracts: []
data: []
gates: []
---

# WF-INDEX · 协作总纲

> **范围**：本文是 `docs/design/workflow/` 的总纲：协作有哪些角色、各自先读哪几节、谁决定谁执行、规则分在哪份文档、一条规则该放哪份。
> **不在本文**：各项细则在分册（§3）；包的划分、独占目录与交付：ENG-WP；此刻的施工状态：施工状态文件（WF-STATUS）。
> **查阅**：先读 §0；找自己那一行与先读什么看 §1；谁决定、谁执行看 §2；要查哪类规则去哪份看 §3；新规则放哪份看 §4。
> **关联**：[WF-ROLE 参与者与分工](collaboration/WF-ROLE-参与者与分工.md)、[WF-MODEL 模型选择与子代理](collaboration/WF-MODEL-模型选择与子代理.md)、[WF-COMMS 交接与通信](collaboration/WF-COMMS-交接与通信.md)、[WF-DUTY 值班与旁路](collaboration/WF-DUTY-值班与旁路.md)、[WF-FLOW 开发流程](process/WF-FLOW-开发流程.md)、[WF-MERGE 批准与合并](process/WF-MERGE-批准与合并.md)、[WF-QUOTA 账号与额度](management/WF-QUOTA-账号与额度.md)、[WF-STATUS 施工状态与队列](management/WF-STATUS-施工状态与队列.md)、[WF-RULE 全员守则](rules/WF-RULE-全员守则.md)、[WF-BUILD 施工边界与内容底线](rules/WF-BUILD-施工边界与内容底线.md)、[WF-DOC 文档写法](rules/WF-DOC-文档写法.md)、[WF-ONBOARD 子代理开工手册](WF-ONBOARD-子代理开工手册.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[CORE-DECISIONS 决策登记](../core/CORE-DECISIONS-决策登记.md)

## 0 结论

1. **作者拍板，主会话总指挥**（§1、§2）：主会话是唯一的主代理，只做判断；合并执行、施工状态与巡检交值班执行者。
2. **Claude 侧只有一个账号**：Opus 与 Sonnet 子代理都由主会话用 Agent 工具开，向主会话回报；Astra 与 Sol 各会话在 Codex 应用里。
3. **各角色先读什么只写在 §1 的表里**，其他文档和协作目录的入口文件只写"见 WF-INDEX §1 里某角色那一行"。
4. **每条规则只有一个家**（§3、§4）：全员守则在 WF-RULE，改规则只改它的家。

### 0.1 接入要点

- **新接手的任何角色**：先在 §1 找到自己那一行，按"先读"一列读；其余按 §3 的索引查。
- **要新加或改一条协作规则**：按 §4 的判定表找到它的家，只改那一处，别处只给指向。
- **写任务单、简报、角色说明**：规则不抄，写"见 WF-RULE §n"；"先读哪几节"只指向 §1。

## 1 角色表

协作由一位作者和下表的会话组成。批准行、评审里的"Opus"都指主会话；"Opus 侧"指主会话和它开的 Claude 侧施工。模型、会话名与调用方式的操作细节（含会话号）在 `E:\Project\UESTC_RPG.wt\.collab\tools\README.md`，不写进文档。

| 角色 | 模型与运行方式 | 职责 | 汇报给 | 先读 |
| --- | --- | --- | --- | --- |
| 作者 | — | 提出要求，最终拍板 | — | — |
| 主会话 | Claude Opus 5.5，Claude Code 交互会话 | 总指挥、唯一的主代理：评审定值、设计定案、派工、批准合入、定文档改动、与作者沟通 | 作者 | `.collab/HANDOVER.md`；WF-ROLE §5；WF-FLOW；WF-MERGE §1；WF-STATUS；WF-COMMS §4 |
| 主会话的子代理 | Claude Opus 或 Sonnet 按任务单，Agent 工具后台开，按包取名 | Opus：Opus 侧包的施工、质感移植、需要 Opus 的文档撰写；Sonnet：评审已定怎么改的活和补 GPT 短板的活（WF-MODEL §2） | 主会话 | WF-ONBOARD、WF-RULE、WF-BUILD（`CLAUDE.md` 自动载入）；自己的任务单；ENG-WP 本包的行；WF-MODEL §8 |
| Astra | gpt-6-astra，xhigh，Codex 应用里的会话 | 工程指挥与代码工程架构师：制定并演进工程架构，契约与共享工程文件，Astra 侧包的拆单派工与架构审查、集成取舍、写合并请求；尽量不亲自施工（WF-ROLE §6） | 主会话 | `.collab/CODEX-BOOT.md`；WF-ROLE §3、§6；WF-FLOW §3、§5；WF-COMMS §3–§4；WF-RULE |
| Astra 的子代理 | 默认 gpt-6.1-sol，`spawn_agent` 开 | Astra 侧包的施工；gpt-6-astra 子代理只在 WF-ROLE §6 列的情形开 | Astra | Astra 的简报；WF-ONBOARD、WF-RULE、WF-BUILD（Codex 侧不会自动载入，简报写明路径） |
| Sol 值班兜底 | gpt-6.1-sol，high，每次叫醒新开一个 Codex CLI 会话（D-210） | 值班执行者没回复时接手合并执行、施工状态与巡检（WF-DUTY §1） | 主会话 | `.collab/tasks/sol/001-值班长期单.md`；WF-MERGE §2；WF-DUTY §1–§2 |
| Grok 值班执行者 | Grok 4.7，后台无界面调用 | 合并执行（首选）、值班单、定时巡检 | 主会话 | `.collab/tools/duty/ROLE-grok.md`；`001-值班长期单.md`；WF-MERGE §2 |
| Gemini 快速杂活 | Gemini 3.8 Flash High，作者的 Antigravity 会话 | 只改文档的合并、主会话直接派的简单快速小活 | 主会话 | `.collab/tools/duty/ROLE-gemini.md`；`001-值班长期单.md` |
| luna 批量苦力 | gpt-6-luna；值班合并每次新开一个 Codex CLI 会话（D-210），批量苦力的单发进 Codex 应用里的"Luna会话" | 量大的机械活；Gemini 不可用时接手只改文档的合并 | 主会话、Astra | `.collab/tools/duty/ROLE-luna.md`；`001-值班长期单.md` |
| Sol 旁路会话 | gpt-6.1-sol，xhigh，普通速度，Codex 应用里的会话 | 按任务单开子代理，做调研与旁路活：调研、补信息、数据核对、按确切文字打文档补丁、批量旁路工程活（WF-DUTY §3–§4） | 主会话 | `.collab/side/ROLE.md`；WF-DUTY §3–§4 |

- 上表每个角色都要守 WF-RULE；"开工必读"之外的文档按需查，不通读。
- 读文件时的"末尾几行"指只读文件最后几十行；`archive/`、`logs/` 其余内容、别的角色的收件箱不用读（WF-COMMS §1）。
- 每个角色开工前或被叫醒后看一眼 `.collab/logs/merged.log` 末尾，挑与自己相关的合入；`main` 变了就 rebase（WF-RULE §10）。

## 2 决定权与执行人

**主会话决定**：合并队列（哪个候选、什么时候合）；`main` 上的提交与推送；施工状态的内容；`docs/design/` 改什么；与作者沟通。

**执行交出去**：上面不需要判断的执行部分，即按批准合入、跑门禁、提交推送、写 `logs/merged.log`、维护施工状态、巡检，由值班执行者做（WF-DUTY §1）。`main` 同一时刻只有一个执行人，值班合并时主会话和其他会话不碰主检出的暂存区。设计文档的撰写可以派出去：需要 Opus 的交 Opus 子代理，Sol 只在任务单给出确切文字时按文打补丁；改动都在工作树里做，经批准合入。

**其他各方定什么**：Astra 定契约、共享工程文件和工程上的取舍（WF-FLOW §5）；画面与声音的取值、风格定案，以主会话的评审为准（WF-ROLE §3）。

## 3 分册索引

| 分册 | 管什么 |
| --- | --- |
| [WF-ROLE](collaboration/WF-ROLE-参与者与分工.md) 参与者与分工 | 会话与运行方式、各模型的长短板、GPT 不做审美、事归谁与交叉、总指挥只做什么、Astra 当指挥 |
| [WF-MODEL](collaboration/WF-MODEL-模型选择与子代理.md) 模型选择与子代理 | 按七问选施工模型、Opus 与 Sonnet 的分界、便宜两档、推理强度、怎么开子代理、子代理生命周期、简报模板与"接续" |
| [WF-COMMS](collaboration/WF-COMMS-交接与通信.md) 交接与通信 | `.collab` 目录与文件、命名与写法、任务单与请求文件与批准行的格式、来信与消息、Codex 会话与引导信箱、重置后先读什么 |
| [WF-DUTY](collaboration/WF-DUTY-值班与旁路.md) 值班与旁路 | 值班执行者的分派与唤醒、值班做什么与不许做、完成度调研、Sol 旁路会话 |
| [WF-FLOW](process/WF-FLOW-开发流程.md) 开发流程 | 一个包从派工到回报与评审、契约申请、调研入队流水线 |
| [WF-MERGE](process/WF-MERGE-批准与合并.md) 批准与合并 | 批准与滚动文档分支、合并步骤、成批合入、合入以后、git 与工作树 |
| [WF-QUOTA](management/WF-QUOTA-账号与额度.md) 账号与额度 | 额度口径、并发公式与记账、节奏档、Codex 账号、额度见底时冻结 |
| [WF-STATUS](management/WF-STATUS-施工状态与队列.md) 施工状态与队列 | 施工状态文件怎么填、待派工队列与队列平衡、交接单与上下文重置 |
| [WF-RULE](rules/WF-RULE-全员守则.md) 全员守则 | 所有角色都要守的规则 |
| [WF-BUILD](rules/WF-BUILD-施工边界与内容底线.md) 施工边界与内容底线 | 改哪里、代码边界、禁用写法、内容底线 |
| [WF-DOC](rules/WF-DOC-文档写法.md) 文档写法 | 文件头、编码与引用、冲突层级、新增文档、文档维护 |
| [WF-ONBOARD](WF-ONBOARD-子代理开工手册.md) 子代理开工手册 | 开工环境、怎么查资料与按要做的事查哪里、完成与回报 |

## 4 一条规则放哪份

按"这条规则回答什么问题"放，不按谁来读：

| 回答的问题 | 放哪份 |
| --- | --- |
| 谁做什么、谁定 | WF-ROLE（角色表在本文 §1） |
| 选哪个模型、怎么开子代理 | WF-MODEL |
| 文件怎么命名、怎么写、放哪、来信怎么发 | WF-COMMS |
| 值班怎么被叫醒、分派给谁、旁路做什么 | WF-DUTY |
| 一件事怎么走（派工到评审） | WF-FLOW |
| 批准、合并、工作树、git 流程 | WF-MERGE |
| 多少、何时（额度、并发、节奏） | WF-QUOTA |
| 此刻的状态、队列、交接 | WF-STATUS |
| 必须与禁止（全员） | WF-RULE |
| 必须与禁止（改哪里、代码与内容） | WF-BUILD |
| 文档怎么写、怎么引用 | WF-DOC |
| 开工环境、查资料、回报格式 | WF-ONBOARD |
| 决策记录 | CORE-DECISIONS 只留一句加落点 |

改规则只改它的家；钩子文字是 WF-RULE §1 说明的唯一例外。
