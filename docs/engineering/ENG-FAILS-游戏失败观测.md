---
id: ENG-FAILS
title: 游戏失败观测
category: engineering
parent: ENG-INDEX
summary: 游戏本体开发的失败按根因类别登记：什么算一次、先登记再继续、判类与新增类别、提交窗口、同类反复时的整类排查；类别清单以 failure-types.json 为准，健康页看次数与频率
keywords: [游戏失败观测, 失败登记, 失败类别, fail.py, failure-types.json, 根因类别, 表现桶, 候选类别, 提交窗口, 逃逸的失败, 健康页, 整类排查, DECISION-285, DECISION-296]
depends: [CORE-INDEX, ENG-GATES, WF-RULE]
related: [ENG-ARCH, ENG-BUDGET-RULES]
contracts: []
data: [".collab/tools/monitor/failure-types.json"]
gates: []
paths: [".collab/tools/fail.py", ".collab/tools/monitor/failure-types.json"]
---
# ENG-FAILS · 游戏失败观测

> **范围**：游戏本体开发的失败怎么登记：什么算一次、先登记再继续、判类、没有合适类别时怎么新增、只看最近多少次提交（DECISION-285），同类反复时怎么整类排查（DECISION-296）。
> **不在本文**：类别清单、窗口大小与段长以 `.collab/tools/monitor/failure-types.json` 为准；命令参数以 `.collab/tools/fail.py` 开头的用法为准；健康页的数据契约在 `dashboard/README.md`；缺陷分级与修不收敛就停在 WF-RULE §14。
> **查阅**：先读 §0；动手登记前读 §0.1。

## 0 结论

1. **只计游戏本体**：运行时与流式、烘焙与缓存、几何与内容、渲染与材质、行走、性能、存档、构建测试与门禁这类游戏代码的失败；协作流程、队列、钩子、巡逻、观察台、文档与索引的故障不计。
2. **计逃逸的失败**：交回或集成之后被抓到的才算一次——集成编译、快检与门禁、启动与运行报错、预烘、截图评审看出的结构性缺陷（穿模、漏面、贴地、缺图）、性能超出一个数量级、存档读写、收口审计查实。施工中边写边编的红灯、事先知道会红的失败、审美意见、构建锁占用与磁盘满这类环境偶发都不计。同一根因按一次计，不按报错行数；同类别、同行号、同一 HEAD 再登记只加重试次数。
3. **先登记再继续**：遇到上一条算一次的失败，先用 `.collab/tools/fail.py add` 登记（用法见脚本开头），再接着修。登记不算修复，也不替代 WF-RULE §14 的分级。
4. **按根因所在的模块判类**：问"这次最终改在哪个模块"。根因还没查明时，只能暂归三个表现桶 `perf.frame`、`render.look`、`walk.control`，查明后用 `reclassify` 改类，不重计。测试或门禁报的失败归被测问题的根因类；只有测试自身错才归 `build.test`，门禁判据错才归 `build.gate`。
5. **没有合适的类别就当场新增**：不设"其他"。现有子类都套不上时，用 `--new` 新增候选并计入，写明判别与最近邻类的区别；先跑 `fail.py cats` 看清单。候选由主会话在一批收口时审：并进已有类（记成别名）、转正或删。
6. **只看最近的提交窗口**：只保留最近若干次提交内的记录（数值见 failure-types.json），不补历史。健康页按子类看次数与走势；巡逻提醒第 7 条，并查漏登记与统计是否生效。
7. **同类反复就整类排查**（DECISION-296）：同一子类自上次排查以来在最近一段提交内（段长见 failure-types.json）累计到 3 条，主会话停止逐条修，开一行整类排查并用 `fail.py open` 登记行号：查明为什么反复出现，在根上补上，扫出同类残留同批修。做完用 `fail.py sweep` 记一笔，提醒的计数从此重起（原记录保留）；结论是"门禁按设计拦下"也照记。

### 0.1 接入要点

- **遇到算一次的游戏失败**：先 `fail.py add` 登记类别再继续修；拿不准算不算，按 §0 第 2 条判。
- **主会话收口时**：巡逻报同类累计就按 §0 第 7 条开整类排查行，做完用 `fail.py sweep` 记一笔。
- **改 failure-types.json**：只增子类与别名，新增必写判别与边界；改编码按全局替换，不留旧码。
