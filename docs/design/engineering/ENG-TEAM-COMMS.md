---
id: ENG-TEAM-COMMS
title: 协作分册：交接与通信
category: engineering
summary: .collab 交接目录与文件，命名与写法，任务单、请求文件与批准行的格式，消息与流水，唤醒脚本与值班执行者，上下文重置后的接续
keywords: [.collab, archive, logs, 结案即挪走, 谁读什么, .ignore, 交接目录, 合并请求, 评审请求, 契约申请文件, 任务单, max-000, max-backlog, sol-001, 旁路单, 调研单, logs/merged.log, 流水, notes, 收件箱, 一条一个文件, 只追加, 不预判时间, 撞号, 批准行, 已合入行, 消息频率, 确认框, 转问作者, 等待不短轮询, 唤醒脚本, sol_duty_watch, sol_side_runner, 值班执行者, Grok 4.7, 上下文重置, HANDOVER.md]
owners: [WP-00]
contributors: []
depends: [ENG-TEAM, ENG-TEAM-ROLES, ENG-TEAM-FLOW]
related: [ENG-TEAM-DUTY, ENG-TEAM-QUOTA, ENG-STATUS]
contracts: []
data: []
gates: []
---

# ENG-TEAM-COMMS · 协作分册：交接与通信

> **范围**：本分册规定会话之间经哪些文件交接、文件怎么命名和写、任务单与请求文件及批准行的格式、什么时候发消息、唤醒脚本怎么叫醒值班与旁路的执行者，以及上下文重置后怎么接着做。
> **不在本文**：一个包从派工到合入的各步：ENG-TEAM-FLOW；值班与旁路会话做什么：ENG-TEAM-DUTY；额度：ENG-TEAM-QUOTA。
> **查阅**：先读 §0；`.collab` 下有哪些文件、谁写看 §1；命名、只追加与不预判时间看 §2；任务单、请求文件与批准行的格式看 §3；消息、流水、等待与确认看 §4；唤醒脚本与值班执行者看 §5；上下文重置后先读什么看 §6。
> **关联**：[ENG-TEAM 协作规程总纲](ENG-TEAM.md)、[ENG-TEAM-ROLES 参与者与分工](ENG-TEAM-ROLES.md)、[ENG-TEAM-FLOW 包的归属与开发流程](ENG-TEAM-FLOW.md)、[ENG-TEAM-DUTY 值班与旁路](ENG-TEAM-DUTY.md)、[ENG-TEAM-QUOTA 账号与额度](ENG-TEAM-QUOTA.md)、[ENG-STATUS 施工状态](ENG-STATUS.md)

## 0 结论

1. **经文件交接，消息只在事件发生时发**（§1、§4）：交接目录是 `E:\Project\UESTC_RPG.wt\.collab\`。任务单和请求文件是交接的唯一依据，任何一方上下文重置，都照它们接着做（§6）。
2. **任务单与请求文件只追加、不改写，来信一条一个新文件**（§2、§4）：每条带时间，路径写绝对路径；不预判时间，只写做到哪一步。
3. **批准行有固定格式**（§3）：单独成行，行首就是"批准合入："，唤醒脚本按行首匹配。
4. **值班与旁路各由一个唤醒脚本叫醒**（§5）：值班唤醒脚本按批准行、新任务单和定时叫醒值班执行者；旁路唤醒脚本按 `side/cards/` 下的新任务单叫醒 Sol 旁路会话，一次一张。值班执行者按难度分：只改文档的合并交 Gemini 3.8 Flash High（不可用时 gpt-6-luna），其余合并、sol 任务单与定时巡检交 Grok，没有回复或出错就退回 Sol 值班会话（D-198）。
5. **下属会话与子代理不向作者出选项或确认框**（§4）：要确认的事写进任务单或请求文件并通知主会话，主会话能据项目信息判断的直接定，只有属于作者的决定才转问作者。
6. **在用的和结案的分开放，不用读的放进 `archive/`、`logs/`**（§1.1）：`tasks/<类>/` 与 `requests/` 下只放在用的和没合入的，搜索默认跳过 `archive/`、`logs/`；各角色读什么见 §1.1。
7. **来信按收件方分收件箱，一条一个文件**（§4，D-207）：写给谁就在 `notes/<收件方>/` 新建一个文件，写完不改；收件方处理完挪进 `archive/notes/<收件方>/`，收件箱里只剩没处理的。

### 0.1 接入要点

本文只约束协作各方的工作方式，没有对其他包的接口要求；按角色的接入要点集中在 ENG-TEAM §0.1。

## 1 交接目录与文件

各方经仓库外的交接目录 `E:\Project\UESTC_RPG.wt\.collab\` 交接；任务单和请求文件是唯一依据，消息只是提醒。值班执行者由唤醒脚本叫醒。

| 路径（相对 `.collab\`） | 谁写 | 写什么 |
| --- | --- | --- |
| `requests/merge-<包号>-<序号>.md` | 包的负责方：Astra 主线程、主会话的子代理、Sol 旁路会话（代码类旁路单） | 合并请求：包号、工作树、分支、候选提交、已跑的检查与结果、关联的契约申请、截图路径、要主会话看的东西。末尾依次是主会话的评审与批准行、值班会话的合入记录。只放还没合入的：值班写完"已合入"后把它挪进 `archive/requests/`（长期批准的 `merge-DOC-rolling.md` 例外） |
| `requests/contract-<序号>.md` | 任何代理 | 契约或共享工程文件的申请（ENG-TEAM-FLOW §2.7）。Astra 在末尾写结论和对应的提交 |
| `requests/review-<包号>-<序号>.md` | Astra 主线程 | 请主会话做审美评审：截图或录像路径、可调参数在哪、各参数现在的值。主会话在末尾写结论或新值 |
| `tasks/claude/max-<nnn>.md` | 主会话建单，写评审与批准行；施工子代理或主会话写一行现状 | Claude 侧施工单（ENG-TEAM-FLOW §2.2）：只放目标、范围、已定取值、交付标准、评审结论和一行现状；过程流水记到 `logs/tasks/<单号>.log.md`；批准行与它的"已合入"行留在单里。只放还在用的，结案后挪进 `archive/tasks/claude/` |
| `tasks/claude/max-brief-common.md` | 主会话 | Claude 侧施工子代理的通用简报，每张 max 单都附 |
| `tasks/claude/max-backlog.md` | 主会话维护 | 空缺活清单（待派队列）；流水记在 `logs/tasks/max-backlog.log.md` |
| `tasks/sol/sol-001.md` | 主会话建单；值班执行者写"状态" | 值班执行者的长期任务单：合并执行、施工状态、巡检、通知、不许做的事 |
| `tasks/sol/sol-<nnn>.md`（其余） | 主会话 | 交给值班执行者的其他任务，例如 `sol-002.md` 完成度调研；值班执行者在末尾写"状态"；结案后挪进 `archive/tasks/sol/` |
| `tasks/astra/astra-*.md` | Astra | Astra 的计划与子单；结案后挪进 `archive/tasks/astra/` |
| `tasks/misc/` | 主会话 | 其他任务单与简报（`sonnet-*.md`、`brief-*.md` 等）；结案后挪进 `archive/tasks/misc/` |
| `side/ROLE.md` | 主会话 | Sol 旁路会话的角色说明：怎么被唤醒、两类任务单、边界 |
| `side/cards/r<nnn>.md` | 值班执行者 | 调研单（ENG-TEAM-DUTY §2），模板是 `research/cards/r002.md` |
| `side/cards/s<nnn>.md` | 主会话 | 旁路单（ENG-TEAM-DUTY §3）：在哪做、交付什么、能改什么；旁路会话在末尾写"状态" |
| `research/INDEX.md` | 值班执行者追加；旁路会话做完标"待审"；主会话审计后改"Opus 审计"一列 | 调研索引：一单一行，单号、启动时间、阶段、方向、总览路径、Opus 审计状态（ENG-TEAM-DUTY §2） |
| `research/<日期>-r<nnn>/` | Sol 旁路会话 | 各方向的报告与 `00-总览.md`；主会话在总览末尾写"Opus 审计"。已审计且采纳项都已入队的挪进 `archive/research/`（INDEX 里的路径随之改） |
| `notes/<收件方>/<YYYYMMDD-HHMMSS>-<发件方>-<短词>.md` | 发件方 | 来信收件箱，一条一个文件（§4）：`notes/opus/` 发给主会话，`notes/astra/` 发给 Astra，双账号模式时 `notes/claude2/` 发给二号会话。收件方处理完挪进 `archive/notes/<收件方>/`；主会话的收件箱由事件等待脚本带回全文后挪走 |
| `notes/codex-app-done.md` | Codex 会话 | 脚本握手：做完一件活追加一行"[任务号] 一句话结果"，`tools/duty/codex_app_run.py` 看到这一行才算这一轮结束；只有脚本读 |
| `logs/merged.log` | 值班执行者 | 每合入一次追加一行：`<YYYY-MM-DD HH:MM:SS> main=<短号> <包号> 已合入：<一句话>。<需要谁 rebase>` |
| `logs/tasks/` | 各单的执行方与主会话 | 各任务单拆出的过程流水 `<单号>.log.md`（派出、交回、合入、值班状态行）；已结案大单的旧评审 `<单号>.history.md` |
| `logs/duty/`、`logs/side/` | 唤醒脚本 | 值班脚本日志 `watch.log`、每轮输入输出 `runs/`；旁路脚本日志与每张单的输入输出 |
| `archive/` | 挪入时 | 结案的任务单（`tasks/<类>/`）、已合入的请求（`requests/`）、已审计的调研（`research/`）、处理过的来信（`notes/<收件方>/`）、已交出的旁路单（`side/cards/`） |
| `.ignore`、`README.md` | 主会话 | `.ignore` 让搜索默认跳过 `archive/`、`logs/`；`README.md` 是目录说明与"谁读什么" |
| `tools/sol_duty_watch.sh` | 主会话 | 值班唤醒脚本（§5） |
| `tools/sol_duty/` | 值班唤醒脚本 | 值班唤醒脚本的状态：`seen.txt`、`cards.txt` 等 |
| `tools/sol_side_runner.sh` | 主会话 | 旁路唤醒脚本（§5） |
| `tools/sol_side/` | 旁路唤醒脚本 | 旁路唤醒脚本的状态：`done.txt` |
| `HANDOVER.md` | 主会话写，冻结与切换模式时追加 | 主会话交接单：进行中的事、模式切换记录（ENG-TEAM-QUOTA §4、§5） |

### 1.1 结案即挪走与谁读什么

- **在用的和结案的分开放**：`tasks/<类>/` 下只放还在用的任务单，结案的由主会话写"结案"后挪进 `archive/tasks/<类>/`；`requests/` 下只放还没合入的，值班合入并写完"已合入"后把请求文件挪进 `archive/requests/`。`archive/` 与 `logs/` 不用读，搜索默认跳过它们（`.collab/.ignore`），要查旧事点名路径搜。
- **各角色读什么**（"末尾几行"指只读文件最后几十行；同样的表在 `.collab/README.md`）：

| 角色 | 开工必读 | 每轮读 | 按需查 | 不用读 |
| --- | --- | --- | --- | --- |
| 主会话 | `HANDOVER.md`；`notes/opus/` 里还没处理的来信 | 等待脚本带回的来信；新请求；`logs/merged.log` 末尾 | `tasks/<类>/` 里某张单；`research/INDEX.md` | `archive/`、`logs/` 其余内容 |
| Claude 施工子代理 | 派工简报；自己的任务单与 `tasks/claude/max-brief-common.md`；ENG-ONBOARD | `logs/merged.log` 最近 24 小时（只看末尾，挑本包相关的） | ENG-WP 本包两行；相关契约申请 | 其他任务单、`notes/`、`archive/`、`logs/` 其余内容 |
| Astra | `CODEX-BOOT.md`；ENG-ONBOARD | 收件箱 `notes/astra/` 里还没处理的来信；`logs/merged.log` 末尾；自己相关的请求 | `tasks/astra/` 自己的计划；相关调研 | `archive/`、`logs/` 其余内容 |
| Astra 的 Sol 子代理 | Astra 的简报；ENG-ONBOARD | `logs/merged.log` 末尾 | 简报点名的任务单、请求、契约申请 | `notes/`、`archive/`、`logs/` 其余内容 |
| 值班执行者（Grok、Gemini、luna、Sol 值班） | `tools/duty/ROLE-<自己>.md`；`tasks/sol/sol-001.md` | 唤醒消息；消息点名的请求文件末尾；`logs/merged.log` 末尾 | `tasks/sol/sol-032.md` 等值班单 | `archive/`、`logs/` 旧内容、`research/` |
| Sol 旁路会话 | `side/ROLE.md` | 唤醒消息点名的 `side/cards/<号>.md` | `research/INDEX.md` 与已有调研 | `tasks/`、`requests/`、`archive/`、`logs/` 其余内容 |

## 2 命名与写法

- `.collab` 在仓库之外，不进 git。
- 任务单与请求文件只追加、不改写；来信一条一个新文件，写完不改（§4）；每条带时间；路径一律写绝对路径。唯一的例外是 `research/INDEX.md` 的"Opus 审计"一列：旁路会话做完一单时标"待审"，主会话审计后改写。
- **不预判时间**：模型凭推理感知时间误差很大，所以不估计"已经用了多久""还要多久"，回报和流水里不写预计完成时间，只写做到哪一步。要用时间时以真实读数为准：手边的命令输出、文件时间戳、额度读数；时长和额度消耗速度用两次真实读数相减。读时间不是硬性规定，不为写时间频繁读时钟；手边没有真实时间就只写日期，不凭推理填。任务单按 D-184 写的工作量用来定单子大小，不当作完成时间的预告。
- **请求文件**：序号取现有最大号加一，用不覆盖的写法创建（例如 bash 先 `set -C` 再重定向，或先确认文件不存在）；撞号时改用下一个号，不覆盖别人的文件。
- **任务单**：`max-<nnn>`、`sol-<nnn>`、调研单 `r<nnn>`、旁路单 `s<nnn>` 都是三位编号递增，新建前确认没被占用。

## 3 任务单与请求文件的格式

**max 任务单**：正文写 ENG-TEAM-FLOW §2.2 列的五项。末尾依次是：
- **"状态"小节**：只留一行现状和一行指向 `logs/tasks/<单号>.log.md` 的流水。过程流水一行一事、带时间，写进那份日志：派出（子代理名与模型）、受阻（原因、需要谁定）、待评审（候选提交、截图路径、要定的问题）、已按评审修改、每次合入。
- **"Opus 评审（时间）"小节**：主会话写，通过或逐条修改意见。
- **批准行**。

**sol 任务单**：正文写任务、做法和不许做的事；末尾"状态"小节由值班执行者写现状一行，过程流水一行一事、带时间，写进 `logs/tasks/<单号>.log.md`（`sol-001.md` 的是 `logs/tasks/sol-001.log.md`）。

**旁路单**：正文写在哪做、交付什么、能改什么、哪些不许做；末尾"状态"小节由旁路会话写过程与结果。调研单的写法见 ENG-TEAM-DUTY §2。

**请求文件**：正文按 §1 的"写什么"一列；后续各方只在末尾追加，每条带时间和署名。

**合并请求与契约申请的短头部**（D-201）：开头六行写完要主会话看的东西，正文不贴证据：
1. 候选：分支、头提交、基线；
2. 改了什么：3 行以内，说结果不说过程；
3. 要主会话定的事：逐条，没有就写"无"；
4. 门禁：一行结果，例如"G1–G5 过，G2 实跑"；
5. 依赖与合入条件：没有就写"无"；
6. 证据：截图、日志的路径，一行列完。
再往下只写主会话需要知道的例外。不写逐项证明、测试计数明细、来源链和哈希。

**批准行**：
- 格式：`批准合入：<候选提交或说明>，Opus，<时间>`，后面可以接括号里的门禁组合或条件。
- 写在合并请求或任务单的末尾，单独成行，行首就是"批准合入："，前面不加列表符号或空格。唤醒脚本按行首匹配，行首不对就叫不醒值班执行者。
- 批准行一出现，值班就会把它和当时其他能合的一起合掉（ENG-TEAM-FLOW §2.5，D-204）。
- 合入后，值班执行者在同一文件末尾追加"已合入：main=<短号>，<时间>，<执行者>"。

## 4 消息与流水

**消息只在事件发生时发**，内容一句话，细节都在任务单里：

| 方向 | 什么时候发 |
| --- | --- |
| 主会话 → 二号会话 | 新单、评审已写 |
| 二号会话 → 主会话 | 待评审、受阻需要决定、整单完成 |
| 两个 Sol 会话 → 主会话 | 合入完成、门禁失败、条件未满足、卡死进程、调研完成；写进主会话收件箱 `notes/opus/`，主会话一侧有监视 |
| 主会话 ↔ Astra | 其余通知，例如文档改了、契约冻结了、某个包阻塞了；写进对方的收件箱 `notes/astra/`、`notes/opus/` |

- **一条来信一个新文件**（D-207）：写给谁，就在谁的收件箱 `notes/<收件方>/` 新建一个文件，文件名 `<YYYYMMDD-HHMMSS>-<发件方>-<短词>.md`；时间取真实读数，发件方写 `opus`、`astra`、`duty`（值班执行者）、`gemini`、`side`（Sol 旁路会话）、`claude2`（二号会话）之一。写完不改，不往别的文件里追加；值班一轮的通知合成一个文件。收件方处理完把文件挪进 `archive/notes/<收件方>/`，收件箱里只剩没处理的：写的一方不用读旧信，收的一方只读新文件。主会话的收件箱由事件等待脚本带回全文后自动挪走；主会话发给 Astra 的信，内容同时用 `codex queue` 或引导信箱发进会话。
- 不发进度汇报，不互相轮询。几张单在 10 分钟内先后到待评审时，合成一条消息。
- **来信分三级**（D-201）：写给主会话的每条来信（`notes/opus/` 里的每个文件），第一行开头标三种之一：
  - 【需定】要主会话拍板：观感、跨两侧的取舍、作者级的事；
  - 【待审】有候选、申请或报告等主会话审；
  - 【知会】只是告诉一声：开工了、已核过、合入成功之类。
  主会话的事件等待只为【需定】【待审】和失败、冲突叫醒主会话；【知会】攒着，下次因别的事醒来时一并看。
- **一条只讲一件事，5 行以内**：要点写全，细节放在任务单或请求文件里给路径。事件等待把新来信全文带给主会话并把文件挪进归档，不再二次读文件。
- **纯工程判断自己定**：Astra 侧能按契约、架构和现有规则判断的工程事（接口细节、实现路径、测试写法、合并顺序），Astra 自己定，记一条【知会】；不写成"请主会话决定"。
- **等待不短轮询**：等门禁、编译、导入、截图或别的后台命令时，能前台一次跑完的就前台跑；放后台的，用一次长等待（工具自带的超时），或者查完成标记和退出码。必须看进度时，间隔不短于 2 分钟，不连续反复读同一份日志。每次被叫醒只读与这次事件有关的文件，不重读已完成的任务单；值班已经处理过的批准行和任务单记在 `tools/sol_duty/seen.txt`、`cards.txt` 里。
- **双账号模式下的二号会话**：消息走跨会话消息；要留文字的，主会话写进 `notes/claude2/`，二号会话写进 `notes/opus/`（发件方 `claude2`），同样一条一个文件。二号会话的额度记账写在 `logs/tasks/claude2-quota.log.md`，一行一事（ENG-TEAM-QUOTA §2）。

**确认与提问**：
- 所有下属会话与子代理（二号会话、Sol 值班与旁路会话、Astra，以及各自的子代理）都不向作者出选项或确认框。
- 需要确认时，写进任务单或请求文件，并通知主会话：二号会话发跨会话消息，Sol 与 Astra 在 `notes/opus/` 新建来信文件；子代理交给派它的一方，由它写进单子并转达。
- 主会话能据项目信息判断的直接定；只有属于作者的决定，才转问作者。

**Codex 侧会话都在 Codex 应用里**（D-203）：Astra、Sol 值班、Sol 旁路和 luna 都是作者在 Codex 应用里开着的会话，会话号见 ENG-TEAM-ROLES §1。主会话、唤醒脚本给它们派活，一律发进对应会话：`codex queue --thread <会话号> --message <内容>`，作者在应用里能看到全过程。脚本用 `tools/duty/codex_app_run.py` 发，它在消息末尾附一个任务号，要求会话做完在 `notes/codex-app-done.md` 追加"[任务号] 一句话结果"，看到这一行才算这一轮结束。模型、推理档和速度跟着会话走，在应用里设。要新的会话，请作者在应用里开好再登记会话号。不用 `codex exec` 另起或续跑后台线程。

**要当轮生效的消息走引导信箱**：会话正在跑时，`codex queue` 的消息要等这一轮结束才送到。要它当轮就看到，用 `python -I tools/duty/codex_app_steer.py <会话号> <消息文件> [等待秒数]`：会话在跑，就把信放进 `steer/<会话号>.md`，钩子 `tools/hooks/uestc-steer.cjs`（PostToolUse）在它下一次工具调用后把信送进当前这一轮，只送一次，送完挪进 `steer/delivered/`；等待超时没被取走就撤回、改为排队；会话空闲就直接排队，马上开新一轮。只在确实要马上改变对方手上的事时用，平常照常排队。

## 5 唤醒脚本

值班与旁路各由一个唤醒脚本叫醒，两个脚本都每 20 秒查一次。旁路唤醒脚本把任务单发进 Sol 旁路会话；值班唤醒脚本按下表把事交给值班执行者，退回时发进 Sol 值班会话。发进 Codex 会话都用 `tools/duty/codex_app_run.py`（§4）。

| 脚本 | 启动 | 叫醒谁 | 什么时候叫醒 |
| --- | --- | --- | --- |
| `tools/sol_duty_watch.sh` | `bash sol_duty_watch.sh <值班会话的 thread>` | 值班执行者（下表） | 新的批准行、新的 sol 任务单、每小时巡检（下表） |
| `tools/sol_side_runner.sh` | `bash sol_side_runner.sh <旁路会话的 thread>` | Sol 旁路会话 | `side/cards/` 下有还没交出的任务单 |

**值班唤醒脚本**按下面的顺序处理，一次只叫醒一轮：

| 发现 | 做什么 |
| --- | --- |
| `requests/*.md` 或 `tasks/<类>/*.md` 里新的行首"批准合入："行 | 立即开一轮合并：把当时所有已批未合、条件已满足的候选成批合掉（含 `doc/rolling`）；有代码待合交 Grok（high），只有文档交 Gemini 3.8 Flash High（不可用时 gpt-6-luna）。叫醒消息写明这一轮同不同步 ENG-STATUS（每小时最多一次） |
| 否则：`tasks/sol/` 下新的 `sol-*.md`（`sol-001.md` 除外） | 交 Grok（medium）读单执行 |
| 否则：距上次巡检满 60 分钟 | 交 Grok（medium）巡检；已批未合、条件已满足的顺手合入，到点就同步 ENG-STATUS |

**执行者与退回**（D-198）：值班执行者是照 `sol-001.md` 做合并、施工状态与巡检的一方，按难度分：只改文档的合并（批准行写了"只跑 check_docs.py"的）这类简单快速的交 Gemini 3.8 Flash High（发进作者开着的 Antigravity 会话，`tools/duty/gemini_run.py` 等它做完，D-202），Gemini 不可用时交 gpt-6-luna（发进 luna 会话，快速模式）；其余合并、任务单与定时巡检交 Grok 4.7（作者的 Grok 订阅，后台无界面调用，同一会话续用）。Grok 或 luna 没有回复、出错或报额度用尽，就把同一条消息退回 Sol 值班会话（gpt-6.1-sol，high，普通速度），由它先看 `git status`、`logs/merged.log` 和 `logs/tasks/sol-001.log.md` 末尾再接着做，已做完的不重做。三者都照 `sol-001.md` 与 ENG-TEAM-FLOW §2.5、ENG-TEAM-DUTY §5 做，通知都写进主会话收件箱 `notes/opus/`（发件方 `duty`，一轮一个文件），开头注明是谁；同一时刻只有一个执行者在主检出上做。Grok 与 GPT 一样不做审美工作（ENG-TEAM-ROLES §2.2）。

值班唤醒脚本不启动调研：调研单写进 `side/cards/`，由旁路唤醒脚本交出。

**旁路唤醒脚本**按文件名顺序，从 `side/cards/` 里取一张还没交出的单，叫醒旁路会话，消息里写明任务单路径，并要求照 `side/ROLE.md` 执行；这一轮结束后再取下一张。一次只交一张。

- **叫醒的命令**：发进 Codex 会话一律 `python -I tools/duty/codex_app_run.py <会话号> <提示文件> [超时秒数]`；Gemini 用 `python -I tools/duty/gemini_run.py <会话号> <提示文件> [超时秒数]`；Grok 用 `grok --prompt-file <提示文件> -r <会话> -m grok-4.7 --reasoning-effort <档>`。档位与速度在 Codex 应用里按会话设：Sol 值班 high、Sol 旁路 xhigh，都是普通速度；luna 快速模式。
- **每轮只做被叫醒的事**：值班执行者只处理消息里写的事和巡检；旁路会话只做交给它的那一张单，做完写状态和流水就结束本轮。
- **记录**：
  - `tools/sol_duty/`：`seen.txt`、`cards.txt` 记已经处理过的批准行和任务单；`logs/duty/watch.log` 记每次唤醒，每轮发给执行者的提示与结果在 `logs/duty/runs/`。
  - `tools/sol_side/done.txt` 记已经交出的单；`logs/side/`：`runner.log` 记每张单的唤醒与"本单结束"，`<单号>.prompt.md`、`<单号>.out.txt` 是每张单发出的提示和完成行。

## 6 上下文重置后怎么接着做

任务单和请求文件是交接的唯一依据。任何一方的会话上下文重置，都先读下表的文件，照里面的"状态"接着做；对不上时以 Git 为准（ENG-STATUS §1）。切换账号以后，接续的主会话先读交接单 `HANDOVER.md`（ENG-TEAM-QUOTA §5）。

| 谁 | 先读 |
| --- | --- |
| 主会话 | 交接单 `HANDOVER.md` 的"进行中的事"；ENG-STATUS §2–§4；`logs/merged.log`；`tasks/<类>/` 下各单的"状态"；收件箱 `notes/opus/` 里还没处理的来信 |
| 二号会话 | `archive/tasks/claude/max-000.md` 与各张 `max-*.md` 的"状态"；收件箱 `notes/claude2/` |
| Astra | `requests/` 下自己的合并请求与契约申请；收件箱 `notes/astra/` |
| 值班执行者（含 Sol 值班会话） | `tasks/sol/sol-001.md` 及其余 `sol-*.md` 的"状态" |
| Sol 旁路会话 | `side/ROLE.md`；`side/cards/` 下各单的"状态" |
| 施工子代理 | 自己的任务单或简报、工作树里的 `git log` |
