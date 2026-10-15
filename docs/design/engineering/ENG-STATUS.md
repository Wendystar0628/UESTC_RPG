---
id: ENG-STATUS
title: 施工状态
category: engineering
summary: 主代理维护的当前施工状态：main、最近可运行入口和活跃工作包的进度
keywords: [施工状态, 施工进度, 当前 main, 最近可运行入口, 活跃工作包, 已冻结的契约组, 契约冻结记录, 候选提交, 待合入, 已集成, 交接, 接手, 换会话, 换账号, 阻塞, 下一步]
owners: [WP-00]
contributors: []
depends: [ENG-WP, ENG-ARCH]
related: [ENG-GATES, CORE-CONVENTIONS]
contracts: []
data: []
gates: []
---

# ENG-STATUS · 施工状态

> **范围**：本文记录此刻的施工状态：当前 `main`、最近可运行的入口、每个活跃工作包做到哪一步。作者暂停、主代理换会话或换账号以后，接手的人读这一页就能接上。
> **不在本文**：工作包的划分与交付（ENG-WP）；git 流程与合并队列（ENG-ARCH §6）；完成回报的格式（ENG-GATES §5）；已经完成的历史（看 `git log`）。
> **查阅**：先读 §0；何时更新、每列写什么、接手时怎么核对看 §1；当前 `main`、可运行入口和已冻结的契约组看 §2；活跃工作包的进度看 §3
> **关联**：[ENG-WP 工作包](ENG-WP.md)、[ENG-ARCH 工程架构](ENG-ARCH.md)、[ENG-GATES 验收门禁](ENG-GATES.md)、[CORE-CONVENTIONS 约定](../core/CORE-CONVENTIONS.md)

## 0 结论

1. 内容由主会话决定，由 Sol 值班会话在每次合入后维护（ENG-TEAM §8.1，CORE-CONVENTIONS §8.5）；其余子代理只读。
2. 只记"现在"：活跃的包一行一个，合进 `main` 并做完的包从表里删掉，历史交给 Git。
3. "代理回报完成"不等于"已集成"：只有合进 `main` 并推送以后，才算集成（ENG-ARCH §6 第 4 步）。

### 0.1 接入要点

- **想知道自己的包在施工表里的状态**：只读本文 §3，不改；进度、阻塞和完成都经回报交主代理（§0 第 1 条，ENG-GATES §5）
- **要判断某组契约能不能改**：看 §2 的"已冻结的契约组"；列在那里的组只增不改，其余按申请修改（§2，ENG-WP §5 第 3 条）
- **要知道别的包的东西是否已集成**：以合进 `main` 并推送为准，回报完成但还没合入的不算（§0 第 3 条）

## 1 填写规则

- **何时更新**：派工、合入、交接、冻结契约这四个时刻各更新一次，其余时候不必改。
  - 派工：加一行，状态写"已派工"；
  - 合入：`main` 推送成功后改 §2 的两行，删掉做完的包；还要继续做的包把分支和候选提交换成新的（ENG-ARCH §6 第 4 步第 5 小步），状态改回"施工中"；
  - 交接：把每一行的状态、阻塞和下一步改成此刻的实情；
  - 冻结契约：主代理宣布冻结一组契约时（ENG-ARCH §3、D-134），在 §2 的"已冻结的契约组"里加上这一组和冻结时的提交。
- **§2 当前 main、可运行入口与已冻结契约**：写当前 `main` 的短 SHA 和提交首行；最近可运行入口写能启动并走通主线的那条命令和它对应的提交，还没有就写"尚无"；已冻结的契约组逐组列出，还没有就写"尚无"。
- **§3 活跃工作包**，每列写什么：

| 列 | 写什么 |
| --- | --- |
| 包 | 编号，子包写到子包（`WP-35b`） |
| 目标 | 本轮要交的东西，一句话，引用 ENG-WP 的条目，不复述 |
| 工作树 | `..\UESTC_RPG.wt\<WP-xx>\`；直接在主检出上做的写"主检出" |
| 分支 | `wp/<xx>-<slug>` |
| 候选提交 | 待合入的那个提交的短 SHA；还没有写"—" |
| 状态 | 已派工、施工中、待合入（代理已回报，等主会话批准、值班会话合并）、合入中、阻塞，五选一 |
| 阻塞 | 卡在什么上、等谁；没有写"—" |
| 下一步 | 下一个动作和由谁做 |

- **接手时**：先读本文，再用 `git worktree list`、`git branch`、`git log --oneline -1 main` 核对；对不上以 Git 为准，并改本文。代理在工作树里有、但没写进本文的提交，接手人先问清楚再处理，不删。
- **不写**：日报、周报、讨论过程和评审意见；回报原文留在会话里，本文只写结论（CORE-CONVENTIONS §7.4）。

## 2 当前 main、可运行入口与已冻结契约

- 当前 `main`：`f371344` WP-05 雾幕与后期链接入第 4 批状态接口。
- 已合入的主线版：WP-00（D0 骨架 `9c7e28a`，共享修补 `9d481f3`，第一波接线契约 `4568685`，第 4 批共享契约 `d4f46a5`）、WP-49b `182f291`、WP-03 `de88536`、WP-37 `0b5be3c`、WP-35 `5ee112b`、WP-09 `3af35c3`、WP-39 `9348404`、WP-51a `8fd87a5`、WP-05（主线版 `bb92dc2`，第 4 批状态接线 `f371344`）、WP-04 `2fb42da`、WP-14（主线版 `cb7a8b0`，部件来源修正 `63645b3`）、WP-48b `bd03484`、WP-35 第二版 `d0e22dc`、WP-15 `89dc0be`、WP-24 `c4d1205`、WP-48a `e5a3947`、WP-08 `5864341`、WP-53a `d7bb772`、WP-34 `60d7fbe`、WP-45a `6a1d7aa`。
- 已合入的地形规范：DOC-terrain-spec `874f89f`（WLD-TERRAIN 与相关文档接入）；DOC-terrain-map `e37a34c`（清水河分区地形方案）。
- 最近可运行入口：`d4f46a5`，`$env:UESTC_GODOT --path game -- --title=0`（D0 桩场景；90 帧窗口冒烟、Release 导出与导出版运行时通过，不代表完整主线旅程）。
- 已冻结的契约组：尚无（v1 候选）。
- 当前阶段：第一波（ENG-TEAM §4.2）。第二波前提里 WP-09、WP-14、WP-08 已合入，还差 WP-02。交接文件在 `E:\Project\UESTC_RPG.wt\.collab\`。
- 高优先级插入项：地形分层 D-180 已登记；contract-15 阶段 0（`ITerrain`、统一采样布局与平地桩）已合入 `d4f46a5`；完整非平地 DTO/schema/评估器待下一批；规范 WLD-TERRAIN 已合入 `874f89f`；分区方案 MAP-QSH-TERRAIN 已合入 `e37a34c`。
- 共享工程后续：contract-16 UI 底图桥、显式 world 加载上下文、存档恢复故障协议、BuildInfo 生产者 hash 留待 core5。
- 已知问题：截图进程卡住会一直占 GPU 锁，截图脚本一律给每个 Godot 进程加 120 s 超时。

## 3 活跃工作包

| 包 | 目标 | 工作树 | 分支 | 候选提交 | 状态 | 阻塞 | 下一步 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| WP-04 | 接 `lamp.gdshaderinc`、`IToonMaterialProvider`、D0RenderHost 接材质库 | `..\UESTC_RPG.wt\WP-04` | `wp/04-toon` | — | 已派工 | — | WP-00-4 共享接口已合入，按交接改接真实接口、快检后交候选 |
| WP-24 | 主线版已合入；接 contract-17 的白名单着色器与倒影代理 | `..\UESTC_RPG.wt\WP-24` | `wp/24-water` | — | 已派工 | — | WP-00-4 共享接口已合入，按交接改接真实接口、快检后交候选 |
| WP-39 | 主线版已合入；接 contract-12 的灯具清单、窗格声明与光晕挂载 | `..\UESTC_RPG.wt\WP-39` | `wp/39-nightlight` | — | 已派工 | — | 按 WP-00-4 接灯具与窗格声明，复用 InstancePoolHost 挂光晕；快检后交候选 |
| WP-40 | 全日天空主线版；修 17:30 云边锯齿 | `..\UESTC_RPG.wt\WP-40` | `wp/40-sky` | — | 施工中 | — | Opus 的子代理交候选，Opus 合入 |
| WP-45b | 降水与湿润的主线版（评审已过） | `..\UESTC_RPG.wt\WP-45b` | `wp/45b-precip` | — | 已派工 | — | WP-00-4 共享接口已合入，按交接改接真实接口、快检后交候选 |
| WP-01 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-01` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-02a | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-02a` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-02b | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-02b` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-06 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-06` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-08 | 主线版已合入；继续 D-180 地形阶段 1 | `..\UESTC_RPG.wt\WP-08` | `wp/08-world` | — | 已派工 | — | WP-00-4 已合入，由 Astra 安排阶段 1 |
| WP-27 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-27` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-32 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-32` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-34 | 主线版已合入；正式启动与存档联调 | `..\UESTC_RPG.wt\WP-34` | `wp/34-time` | — | 阻塞 | 正式启动与 G20 待 WP-01/50 联合验证 | 联调世界/天气目标一致性；恢复失败回滚由 WP-50 处理 |
| WP-42 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-42` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-43 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-43` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-44 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-44` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-45a | 主线版已合入；天气存档真实容器联调 | `..\UESTC_RPG.wt\WP-45a` | `wp/45a-weather` | — | 阻塞 | G20 真实容器联调待 WP-50 | WP-50 接通后验证全局与按图天气恢复；GPU 仍归 WP-45b |
| WP-46 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-46` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-48c | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-48c` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-49a | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-49a` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-50 | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-50` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-51a | 主线版（ENG-WP §2.1）；Astra 负责 | `..\UESTC_RPG.wt\WP-51a` | 由 Astra 建 | — | 已派工 | — | Astra 的子代理施工，Astra 审后写合并请求 |
| WP-53a | 主线版已合入；正式 Boot 音频联调 | `..\UESTC_RPG.wt\WP-53a` | `wp/53a-audio-core` | — | 阻塞 | 实际初始化、Ready 与换图运行待 WP-01 正式 Boot | WP-01 接通后联调；素材由 53e 后续接入 |
