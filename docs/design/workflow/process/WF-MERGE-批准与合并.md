---
id: WF-MERGE
title: 批准与合并
category: workflow
summary: 批准行与滚动文档分支，合并执行的步骤，成批合入与省项，合入以后的状态与 rebase，git 分支与工作树的流程
keywords: [批准, 批准行, 滚动文档分支, doc/rolling, 合并执行, squash, 成批合入, 省项, SkipFormat, merged.log, 已合入, rebase 通知, 工作树, 本地分支, new.ps1, cleanup_worktrees, git 流程, 远端只有 main, D-166, D-204, D-206]
owners: [WP-00]
contributors: []
depends: [WF-INDEX, WF-FLOW, WF-COMMS]
related: [WF-DUTY, WF-STATUS, WF-RULE, ENG-GATES, ENG-ARCH, ENG-WP]
contracts: []
data: []
gates: [G1, G2, G3, G4, G5]
---

# WF-MERGE · 批准与合并

> **范围**：本文规定候选怎么被批准、值班执行者怎么合并、成批合入与可省的检查、合入以后的状态与 rebase，以及 git 分支与工作树的流程。
> **不在本文**：评审与批准前的流程：WF-FLOW；批准行格式：WF-COMMS §3；值班怎么被叫醒：WF-DUTY §1；门禁的判据与命令：ENG-GATES §2；工作树的技术细节（`override.cfg`、导入种子、计数锁）：ENG-ARCH §6；内核包的合并顺序：ENG-WP §1.1。
> **查阅**：先读 §0；批准与滚动文档分支看 §1；合并步骤看 §2；成批与省项看 §3；合入以后看 §4；git 与工作树看 §5。
> **关联**：[WF-INDEX 协作总纲](../WF-INDEX-协作总纲.md)、[WF-FLOW 开发流程](WF-FLOW-开发流程.md)、[WF-COMMS 交接与通信](../collaboration/WF-COMMS-交接与通信.md)、[WF-DUTY 值班与旁路](../collaboration/WF-DUTY-值班与旁路.md)、[WF-STATUS 施工状态与队列](../management/WF-STATUS-施工状态与队列.md)、[WF-RULE 全员守则](../rules/WF-RULE-全员守则.md)、[ENG-GATES 验收门禁](../../engineering/ENG-GATES-验收与门禁.md)、[ENG-ARCH 工程架构](../../engineering/ENG-ARCH-工程架构.md)、[ENG-WP 工作包](../../engineering/ENG-WP-工作包派工单.md)

## 0 结论

1. **只合有主会话批准行的候选**（§1）；批准即合，成批合入，不攒时间窗（D-204）。
2. **合并由值班执行者在主检出上执行**（§2）：squash、门禁、推送 `main`、记 `merged.log`、删工作树。
3. **成批合入，整批公共门禁只跑一次**（§3）；候选都已 rebase 并过快检、squash 无冲突时，省 G1 格式与 G4（D-206）。
4. **合入以后更新施工状态并通知 rebase**（§4）。
5. **远端只有 `main` 和标签**；分支只在本地隔离并行施工，从不推送，合入后即删（§5，D-166）。

### 0.1 接入要点

- **交候选之前**：`git rebase main`，跑 `scripts\gate.ps1 -Wp <WP-xx>`，用 `git merge-tree` 对最新 `main` 预演；候选写进合并请求（WF-COMMS §3）。
- **想让文档改动尽快进 main**：零碎的提交到 `doc/rolling`，急的单独开候选并写批准行（§1）。
- **`main` 有新合入**：`logs/merged.log` 新增一行，里面写明谁要 rebase；各方在自己的工作树里 `git rebase main`（§4）。

## 1 批准与滚动文档分支

**批准**：评审通过后，主会话在合并请求或任务单末尾写批准行（格式见 WF-COMMS §3）。批准行可以附"门禁：…"（例如去掉 G3、只改文档只跑 `check_docs.py`）或补充条件（例如"merge-WP-00-4 已合入 `main` 之后再合"）；条件没满足就不合，由值班在巡检时复查。

**什么时候合**（D-204）：唤醒脚本一发现新的批准行，就叫醒值班执行者，把当时所有已批未合、条件已满足的候选成批合掉，不攒、不等时间窗；有前后依赖的先合基点，合完再看依赖它的。执行者正在合的时候新来的批准行，这一轮结束后马上开下一轮。

**滚动文档分支**：主会话的零碎文档改动不各开候选，一律提交到 `doc/rolling`（工作树 `E:\Project\UESTC_RPG.wt\DOC-rolling`），请求文件 `requests/merge-DOC-rolling.md` 里的"滚动批准"行长期有效。每一轮合并里它比 `main` 多出提交，就记下头提交 T，整段 squash 合入（只跑 `check_docs.py`），提交首行写"文档：滚动合入"、正文逐条列这些提交的标题；合完若分支头仍是 T 就从新 `main` 重建分支，期间又有新提交就 `git rebase --onto main T`，工作树不干净就不动它、写一行状态。急的文档改动也可以单独开候选、写批准行。

## 2 合并步骤

值班执行者照 `tasks/sol/001-值班长期单.md`，在主检出 `E:\Project\UESTC_RPG` 上做，所有 git 用 `git -C E:\Project\UESTC_RPG`：
1. `git status --short` 必须为空；不空就停下，写进文件末尾并通知主会话，不动别人的改动。
2. `git merge --squash <候选提交>`；有冲突就 `git reset --merge` 撤回，停下报告，由施工方 rebase 后重交。
3. 跑门禁：默认 `scripts\gate.ps1 -Wp <包号> -Only G1,G2,G3,G4,G5`，批准行另写了组合就照写的跑；只改文档的跑 `scripts\docs\check_docs.py`，要求 problems 0。资源有改动时 G1 会在主检出跑 `--headless --import`，同时更新工作树的导入种子。失败：`git reset --merge` 撤回，把失败项、关键报错行、日志路径写到文件末尾，通知主会话，停下，不修代码。等 GPU 锁超过 15 分钟：查卡死的进程并报告，不绕过锁。G3 报 `docs/design/` 归属错时，主检出有未推送的文档提交，先 `git push origin main` 再重跑。
4. 提交：首行 `WP-xx: <一句话>` 或 `文档：<一句话>`；正文几条要点，只从请求文件或回报里取，末尾写"候选 <提交>。门禁 … 通过。"；结尾 `Co-Authored-By` 写实际执行的模型和 Claude Opus 5.5 两行。
5. `git push origin main`：只推 `main`，不推分支，不强推。
6. 往 `logs/merged.log` 追加一行（格式见 WF-COMMS §1）。
7. 用完的工作树当场删（D-166）：运行 `bash E:/Project/UESTC_RPG.wt/.collab/tools/duty/cleanup_worktrees.sh <工作树目录名>`，删掉刚合入的这个工作树和它的本地分支。脚本只删同时满足这几条的：不在长期留用名单 `tools/duty/keep_worktrees.txt`、没有未提交改动和新文件、内容已全在 `main`、没有别的在途请求提到它；不满足就留着，结果写进流水。长期留用的 `DOC-rolling` 不删，照 §1 重建。同包的下一张单由新的子代理用 `scripts\wt\new.ps1` 开新的工作树。
8. 在请求文件末尾追加"已合入：main=<短号>，<时间>，<执行者>"并通知主会话；然后把这份请求文件挪进 `archive/requests/`（目标已有同名文件就停下报告；`merge-DOC-rolling.md` 例外，一直留在 `requests/`；批准行写在任务单里的不挪，任务单由主会话写"结案"后挪走）。

## 3 成批合入与省项

- **成批**：每轮从已批未合的候选里挑互不冲突的 3–5 条（文件不重叠、G3 owner 不交叉、前置条件已满足），各自 squash 成一个提交，按批准顺序叠起来，整批只跑一次 G1、G2、G4、G5，G3 按 owner 分跑；全过就整批推送，`logs/merged.log` 每条一行。整批失败时定位到具体候选，只撤出那一条并报告，其余重跑一次后合入。内核包仍按 ENG-WP §1.1 的顺序合。
- **省掉重复的两项**（D-206）：一批里每条候选都在自己的工作树里 rebase 到交候选时的最新 `main` 并跑过快检、结果是通过（请求或回报的门禁行写明），而且 squash 时没有解冲突，就跳过 G1 的格式检查（`-SkipFormat`）和 G4 的依赖规则检查（`-Only G1,G2,G3,G5`）；编译、G2、G3、G5 照跑。有一条不满足，整批照常全跑；提交正文的门禁行写明省了哪两项。
- **不重复复核**：同一组合的门禁只跑一次，没有新输入不再复核（WF-RULE §4）。
- 交候选之前，施工方先 rebase 到最新的 `main`（WF-RULE §10）。叠在别的候选上的，基点合入后照批准行写的基点 `rebase --onto main`。

## 4 合入以后：施工状态与 rebase

- **施工状态**：每小时最多同步一次；唤醒脚本在叫醒消息里写明这一轮同不同步。同步的那一轮整批合完以后，值班执行者更新施工状态文件（WF-STATUS §1）：当前 `main`、删掉已合入的行或改状态，并把依赖已满足的队列条目改成"可派"。状态文件在 git 之外，不需要提交，也不跑 `gen_index.py`。实时的合入记录看 `logs/merged.log`。
- **rebase 通知**：值班执行者在 `merged.log` 那一行里写明需要谁 rebase；主会话通知名下已在施工的子代理，Astra 通知自己的子代理。任何施工方开始下一步之前都看一眼 `merged.log`，`main` 变了就在自己的工作树里 `git rebase main`；资源有变化时再跑一次 `--headless --import`（`gate.ps1` 会自动做）。

## 5 git 与工作树

- **远端与分支**（D-166）：远端 `origin` 是 GitHub 上的 `Wendystar0628/UESTC_RPG`，只有 `main` 和里程碑标签；不开 PR，不推送别的分支，不对 `main` 强推。本地分支只用来隔离并行施工：每个写代码的子代理一个工作树、一个本地临时分支 `wp/<xx>-<slug>`，在本地提交、从不推送，合入后随工作树一起删（§2 第 7 步）。
- **什么时候开工作树**：同一时刻有两个以上代理要写代码、编译或导入时，每个写代码的代理各开一个。只有一个代理写代码时（例如主会话改文档或小修），直接在主检出 `E:\Project\UESTC_RPG` 的 `main` 上做：同样先过与改动相关的检查，再提交、推送。只改文档、不编译的代理在主检出里改，由主会话分派，两个代理不同时改同一个文件。
- **谁建、怎么建**：谁派的子代理谁建。每个包一个工作树 `E:\Project\UESTC_RPG.wt\<WP-xx>`：`powershell.exe -NoProfile -ExecutionPolicy Bypass -File E:\Project\UESTC_RPG\scripts\wt\new.ps1 -Wp <WP-xx> -Slug <短名>`。没有包号的任务用 `SN-<nnn>`，分支 `sn/<nnn>-<短名>`：`git -C E:\Project\UESTC_RPG worktree add E:\Project\UESTC_RPG.wt\SN-<nnn> -b sn/<nnn>-<短名> main`。新工作树的 `override.cfg`、导入种子和计数锁的技术细节见 ENG-ARCH §6。
- **任何时刻只有一个主代理**：合并队列、`main` 上的提交和推送由主会话决定，值班执行者执行；`main` 同一时刻只有一个执行人（WF-INDEX §2）。并发规模按账号额度定（WF-QUOTA）。
