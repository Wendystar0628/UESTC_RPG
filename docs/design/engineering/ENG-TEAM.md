---
id: ENG-TEAM
title: 多模型协作规程：总纲与分册索引
category: engineering
summary: 多模型协作规程的总纲：五份分册各自的结论、按角色的接入要点与分册索引
keywords: [多模型协作, 协作规程, 总纲, 分册索引, 接入要点, 按角色读, 作者, 主会话, 二号会话, 二号会话开的子代理, Astra, Astra 派出的子代理, 值班执行者, Sol 值班会话, Sol 旁路会话, 下属会话, 施工子代理, 不向作者出选项]
owners: [WP-00]
contributors: []
depends: [ENG-WP, ENG-ARCH, ENG-ONBOARD]
related: [ENG-TEAM-ROLES, ENG-TEAM-FLOW, ENG-TEAM-COMMS, ENG-TEAM-QUOTA, ENG-TEAM-DUTY, ENG-STATUS, ENG-GATES, CORE-DECISIONS]
contracts: []
data: []
gates: []
---

# ENG-TEAM · 多模型协作规程：总纲与分册索引

> **范围**：本文是多模型协作规程的总纲。开发期多个模型、多个会话怎么协作，规则分在五份分册里；本文只放各分册的结论、按角色的接入要点和分册索引。
> **不在本文**：各项细则在分册：参与者与分工 ENG-TEAM-ROLES，包的归属与开发流程 ENG-TEAM-FLOW，交接与通信 ENG-TEAM-COMMS，账号与额度 ENG-TEAM-QUOTA，值班与旁路 ENG-TEAM-DUTY。包的划分、独占目录与交付见 ENG-WP；git 工作树与合并的命令细节见 ENG-ARCH §6；子代理的工作规则见 ENG-ONBOARD；测试与回报格式见 ENG-GATES；此刻的施工进度见 ENG-STATUS。
> **查阅**：先读 §0；再在 §0.1 找到自己那一行；要查哪类规则、去哪份分册的哪一节，看 §1。
> **关联**：[ENG-TEAM-ROLES 参与者与分工](ENG-TEAM-ROLES.md)、[ENG-TEAM-FLOW 包的归属与开发流程](ENG-TEAM-FLOW.md)、[ENG-TEAM-COMMS 交接与通信](ENG-TEAM-COMMS.md)、[ENG-TEAM-QUOTA 账号与额度](ENG-TEAM-QUOTA.md)、[ENG-TEAM-DUTY 值班与旁路](ENG-TEAM-DUTY.md)、[ENG-WP 工作包](ENG-WP.md)、[ENG-ARCH 工程架构](ENG-ARCH.md)、[ENG-ONBOARD 开工手册](ENG-ONBOARD.md)、[ENG-STATUS 施工状态](ENG-STATUS.md)、[ENG-GATES 验收门禁](ENG-GATES.md)、[CORE-DECISIONS 决策登记](../core/CORE-DECISIONS.md)

## 0 结论

1. **参与者与分工**（ENG-TEAM-ROLES）：作者提出要求、最终拍板；主会话是唯一的主代理和总指挥，只做判断；视觉风格只由 Opus 侧做，GPT 模型不做审美工作；施工模型按七问与难度选，子代理能恢复就不新开。
2. **包的归属与开发流程**（ENG-TEAM-FLOW）：包按擅长归属，主线分段开工；一个包按固定顺序从派工走到合入，只合有主会话批准行的候选；契约只由 Astra 改；调研走入队流水线；派工看整条流水线的队列平衡，并贴近主线、减少合并工作量。
3. **交接与通信**（ENG-TEAM-COMMS）：各方经 `.collab` 交接目录里的文件交接，任务单和请求文件是唯一依据，消息只在事件发生时发；唤醒脚本叫醒值班与旁路的执行者；下属会话与子代理不向作者出选项。
4. **账号与额度**（ENG-TEAM-QUOTA）：现在是单 Claude 账户模式，Claude 侧子代理都开在主会话下，并发按 Opus 当量的公式定；主账号剩余约 1% 时冻结；Codex 额度不监控；双账号模式的规则单列一节。
5. **值班与旁路**（ENG-TEAM-DUTY）：值班执行者（luna、Grok，没回复退回 Sol 值班会话）做合并执行、施工状态与巡检，并持续排完成度调研；Sol 旁路会话按任务单开子代理做调研与旁路活；各方怎么开子代理；Codex 侧不浪费在等待和重复上。
6. **按角色读**（§0.1）：每个角色先读自己那一行列出的分册小节，其余按 §1 的索引查。

### 0.1 接入要点

- **你是主会话**：先读 ENG-TEAM-ROLES §1.1、§3.2、ENG-TEAM-FLOW §2；只做判断，不开新的子代理，要 Opus 子代理做的事一律写成任务单交二号会话，杂活交 Sol；下属报来要确认的事，能据项目信息判断的直接定，只有属于作者的决定才转问作者（ENG-TEAM-COMMS §4）；调研审计采纳后写进 ENG-WP 并加入 ENG-STATUS §4 待派工队列，从"可派"条目里按优先级与二号会话的并发余量派工（ENG-TEAM-FLOW §2.8）；评审写在任务单或请求文件末尾，批准行照 ENG-TEAM-COMMS §3 的格式写；派单按 ENG-TEAM-QUOTA §6.2 分批。单 Claude 账户模式下，Claude 侧子代理由你直接开，二号会话的职责由你自己做（ENG-TEAM-QUOTA §4）。
- **你是二号会话**：先读 ENG-TEAM-FLOW §2.2–§2.6、ENG-TEAM-COMMS、ENG-TEAM-QUOTA §2、§6；收任务单，按并发公式开子代理施工，做初步核对，原样转达评审意见；画面与风格以主会话的评审为准；不合并、不推送、不改契约与共享工程文件，不自己大块施工。
- **你是二号会话开的子代理**：向二号会话回报，不直接找主会话；照任务单和通用简报施工（ENG-TEAM-FLOW §2.3）；评审意见回来，在原上下文里接着改（ENG-TEAM-FLOW §2.4）。
- **你是 Astra**：先读 ENG-TEAM-ROLES §2、§3.3、ENG-TEAM-FLOW §2、ENG-TEAM-DUTY §4；你是工程指挥，施工默认开 gpt-6.1-sol 子代理去做，你负责拆单、写简报、审查与集成（ENG-TEAM-ROLES §3.3）；你的包只交机制和集中的可调参数，不做画面观感的决定与调整，观感问题写评审请求（ENG-TEAM-ROLES §2.2、ENG-TEAM-FLOW §2.4）；契约与共享工程文件只由你改，在 WP-00 工作树提交并写合并请求（ENG-TEAM-FLOW §2.7）；看 `merged.log`，通知你的子代理 rebase（ENG-TEAM-FLOW §2.6）。
- **你是 Astra 派出的子代理**：向 Astra 主线程回报，不直接找主会话；做完由 Astra 写合并请求（ENG-TEAM-FLOW §2.3）；只在简报给的工作树里读写，git 一律 `git -C <工作树>`（ENG-TEAM-DUTY §4）。
- **你是值班执行者**（Grok、luna，或接手的 Sol 值班会话）：按 `tasks/sol-001.md` 执行；只合有批准行的候选（ENG-TEAM-FLOW §2.5）；合入后同步 ENG-STATUS，并顺带把 ENG-STATUS §4 待派工队列里依赖已满足的条目改成"可派"（ENG-TEAM-FLOW §2.6、§2.8）；巡检与排调研按 ENG-TEAM-DUTY §1、§2；不做任何画面与风格判断，不改代码。
- **你是 Sol 旁路会话**：按 `side/ROLE.md` 执行唤醒脚本交给你的那一张单（调研单 `r<nnn>` 或旁路单 `s<nnn>`），做完写状态和流水就结束本轮（ENG-TEAM-DUTY §3）；按单开 gpt-6.1-sol 子代理，最多 8 个并发；调研涉及观感的只收集事实和出处，不调研、不建议 RPG 内容（ENG-TEAM-DUTY §2）；不做审美判断，不改契约和共享工程文件，设计文档只按任务单给的确切文字打补丁，不合并、不推送，不碰别的工作树与主检出。
- **你是下属会话或子代理**（二号会话、Sol 值班与旁路会话、Astra，以及各自的子代理）：不向作者出选项或确认框；需要确认时写进任务单或请求文件，并通知主会话，子代理交给派你的一方转达（ENG-TEAM-COMMS §4）。
- **你是任何施工子代理**：
  - 向派你的一方回报（ENG-TEAM-ROLES §1 表的"汇报给"一列），做完写合并请求或回报，不自己合 `main`、不推送（ENG-TEAM-FLOW §2.3、§2.5）；
  - 开始下一步之前看 `merged.log`，`main` 变了就在自己的工作树里 `git rebase main`（ENG-TEAM-FLOW §2.6）；
  - 要改契约或共享工程文件，在 `.collab/requests/` 写契约申请，由 Astra 处理（ENG-TEAM-FLOW §2.7）；
  - 要改设计文档，在回报里提出，由主会话定（ENG-TEAM-ROLES §1.1）；
  - 结果看得见或听得见的，把可调参数集中放一处，并在回报里列出，供主会话评审定值（ENG-TEAM-FLOW §2.4）。

## 1 分册索引

| 分册 | 管什么 | 小节 |
| --- | --- | --- |
| [ENG-TEAM-ROLES](ENG-TEAM-ROLES.md) 参与者与分工 | 参与者、账号与会话，决定权与执行人；各模型的长处与短板，GPT 不做审美，事归谁；按七问与难度选施工模型，总指挥只做什么，Astra 当指挥，包内实现路径，子代理恢复还是新开 | ENG-TEAM-ROLES §1–§3 |
| [ENG-TEAM-FLOW](ENG-TEAM-FLOW.md) 包的归属与开发流程 | 包归 Astra 侧还是 Opus 侧，开工分段与合并顺序；派工与拆单，施工与回报，评审，批准与合并，合入以后，契约申请，调研入队，队列平衡与节奏档，贴近主线 | ENG-TEAM-FLOW §1–§2 |
| [ENG-TEAM-COMMS](ENG-TEAM-COMMS.md) 交接与通信 | `.collab` 的目录与文件，命名与写法，任务单、请求文件与批准行的格式，消息与流水，唤醒脚本与值班执行者，上下文重置后怎么接着做 | ENG-TEAM-COMMS §1–§6 |
| [ENG-TEAM-QUOTA](ENG-TEAM-QUOTA.md) 账号与额度 | 额度排程口径，Claude 侧的并发公式与记账，Codex 账号与速度，单 Claude 账户模式，额度见底时冻结，仅双账号模式 | ENG-TEAM-QUOTA §1–§6 |
| [ENG-TEAM-DUTY](ENG-TEAM-DUTY.md) 值班与旁路 | 值班执行者与 Sol 值班会话，完成度调研，Sol 旁路会话，各方怎么开子代理，Codex 侧怎么省，待验证的初值 | ENG-TEAM-DUTY §1–§6 |
