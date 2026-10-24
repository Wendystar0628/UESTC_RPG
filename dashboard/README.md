# 作者观察台

作者了解项目的唯一入口：现在、施工、团队、健康、性能、体系、动态、文档八页，回答当前推进、流水线、协作、运行状况、游戏性能、开发结构、最近变化与文档内容。双击 `打开观察台.cmd`，启动唯一实时采集与回环 HTTP 服务。作者过目不挡推进（DECISION-274），工程、架构与排期由两位指挥判断。

统计口径、时间窗口、排序与未知判断统一在 `monitor/view/`，失败登记的提交窗口由 writer、list 与 view 共用 `monitor/read/failures.window_records()` 纯函数；页面展示现成结果，保留按方向筛抽屉、按 parent 展开文档树、拼编号和用 `Dash.since` 显示经过时长等展示行为。历史不全照实显示已有事实，缺项保留“未确认”；不公开原始日志或完整会话。数据契约见 §1–§2，页面与交互见 §3–§5。

## 1 采集与写入

- `live.py --loop` 是唯一采集调度，使用一个 watchdog `WindowsApiObserver`，与历史 Recorder 共用原生文件通知。输入变化只标脏，快慢工作分开；采集期间再次变化会接着处理最新输入，无变化不重采整套数据。
- `collect.py [数据名…]` 按现有生产表只算点名数据，按 `@` 依赖顺序回写同轮 `context.values`，完成一份即发布一份。两位指挥、子代理与运行灯共用 people 样本；team 同轮依赖 progress。时间变化复用现有 `next_expiry` 到期调度，不做周期全扫、不因 SSE 心跳重读来源。 有真实到期点的 view 显式返回 `(payload, expiry)`；采集器保存到期，调度只重算对应份，不经 context 隐式回写。
- `.collab/tools/monitor/read/` 持有来源 IO 与解析，`view/` 计算业务。巡逻周期唯一经 `read.collab.patrol_interval_seconds()` 读取 `.collab/tools/README.md` 的明确登记；缺失或不明确为 null。health 在 view 计算 late 与私有到期点，调度只按该点重算 health，并监听登记变化。
- `attention.py` 独占作者输入事务：输入变更、过目重建、revision 校验、确认、attention/feedback 发布。`post.py`、采集与 HTTP 入口只调用它；先持输入事务锁，再经窄发布函数取得短锁。
- `datafile.write()` 是唯一文件发布入口：原子替换 `Dash.put` 后提交内容签名与确认时刻；签名不含 updated，相同内容只推进确认。失败可发布 errors，不能推进确认。内部 `dashboard-publication.json` 只是当前发布快照，checked 是私有 name→ISO 表，不是历史或公开 SSE 载荷。
- `failure-types.json` 的精确文件事件与 `history:failures`、既有 Git 引用事件驱动 failures；不新增计时器或巡逻入口。
- `perf` 走慢道，10 秒合批、3600 秒判鲜；非递归精确监听作者性能根的 `latest.json`、`output/perf/index.json` 与根 `budgets.json`，主线 Git 移动同样触发。作者根复用分析器从 `game/project.godot` 自定义用户目录名推导；read 层只读关闭会话，发现未分析或输入失鲜时调用现有 `scripts/perf/analyze.py`。指标、有效性、好坏、异常与保留规则以该分析器的 index、derived、history 为唯一来源，不建立观察台性能历史。主线提交表由 `read/repo.mainline_commits()` 集中，failures 与 perf 共用。
- `perf.recording` 只从 `latest.json` 的 recording 状态、所指会话锁占用和 pending 清单的开始时刻取事实，绝不读取记录中的逐帧文件；锁文件存在不表示仍在录制。录制器原子发布 recording/closed 指针后触发采集，前端按 since 显示经过时间。
- `catalog.DATASETS` 是各份公开与内部结果的唯一登记，集中输入、确认间隔、通道、合批、行与样本需求、周期和到期属性。`datafile.AUTO_NAMES` 由公开项派生；collect 的 PRODUCERS 是唯一实际生产函数绑定，启动核对其输出名字与登记名集，live 按同表调度。`/api/data-names.js` 同次输出 `window.DashDataNames`（公开数据名）与 `window.DashQueueStates`（施工状态有序数组）；状态 key、中文名与顺序只由 `.collab/tools/monitor/queue-states.json` 定义，Python、Node 与前端共用。
- 登记的 kind 区分公开结果、共享原始事实与界面发布；collect 仅核对实际生产输出与登记的名字集合，不再保存或比较第二份函数绑定。作者事务输出由 attention.BUILDERS 导出；策展输入名单统一在 catalog.CURATED_NAMES。判鲜仅对从未确认的来源返回空，已确认来源的坏时刻或坏间隔直接报错。
- `serve.py` 只绑定 `127.0.0.1:8765`，提供静态文件、同源写入、SSE 与文档只读接口。服务 Observer 只观察发布元数据，不采集、不持发布锁等待客户端。唯一 `health.freshness(checked, intervals, now)` 纯函数算 stale/ratio；HTTP 只包装结果，业务指标不在服务计算。

`dashboard/data/auto/` 是程序结果；`curated/notices.js` 与 `curated/audit.js` 是私有输入，只经 post.py 改，通知汇入公开 feed。作者信全文只从真实 inbox 与已送达档读取，截图只来自已登记检出的两种固定机位总览。index、app、vendor 内容签名发布为 versions.app。

## 2 数据契约 v3

数据文件用 `Dash.put(名字, 数据)` 交付，附 `updated`、固定生产方 `source`、`errors`。时刻为 ISO，时长为分钟；`rate`、`ratio` 为 0–1 口径的小数，freshness.ratio 是经过时长/应有间隔，可大于 1。比例显示用 `Dash.pct(rate, 1)`；failures.rate 单独表示每 window.segment 次提交的登记次数。公开名字、顺序与间隔由 catalog.DATASETS 派生，未生成显示缺数据。index、stats、records 停产，通知仅作输入。

施工种类唯一由 `monitor.util.ROW_KINDS` 定义，`flow.kinds` 给出有序的 key 与中文 label；`leadTime` 为 all 和该清单各类输出分组，页面据清单展示。现行行号仅为大写类别加三位数与可选小写子行后缀；Git、外部会话与原始诊断日志的施工引用只在读源边界规范化，实际事件原文与计数保持原样。

内容改变才换版本，updated 是该版内容发布时间。真实读取、解析或依赖失败发布 errors；缺事实、覆盖不足、零分母或无法确认的判断保留 null，不写成零、空闲或正常。历史带 `coverage: {from}`，不补造记账起点以前的事实；后端给出各窗口与列表顺序，前端不再排序、切窗或重算业务。

```text
live:      {leads: [{who: Claude|Codex, running: true|false|null, since}]}
attention: {open, items: [{id, kind: audit|shots|poster|review, title, detail, link, at, revision}]}
feedback:  {items: [{at, text, delivered}]}
hookfeed:  {lanes: [{role: 主会话|Claude 子代理|Codex 主线|Codex 子代理,
                    rules: {at: ISO|null, calls: 工具调用数|null, late: bool|null}|null,
                    items: [{at, event: 中文事件, blocks: [中文块名], chars}]}]}
progress:  {counts: {waiting, ready, active, integration, paused},
            dwell: [{side: all|claude|codex, stage, n, med}],
            rows: [{id, title, kind: GAME|TOOL|BATCH, who, stage, status, since, waiting, stageP80, long, jump,
                    deps: [id], waitOn: [还没结的前置 id], waitedBy: [id], history: [{stage, from, to|null, min}]}]}
flow:      {coverage, kinds: [{key: GAME|TOOL|BATCH, label}],
            leadTime: [{side: all|claude|codex, kind: all|GAME|TOOL|BATCH, cur: 分布, prev: 分布|null}],
            wip: {days: [日], windows: {"14": 窗口, "30": 窗口}},
            stages: [{side, week: cur|prev, n, parts: {waiting, ready, active, integration, paused}|null,
                      top: {段: [{id, title, minutes}]}|null}],
            blocked: {all: 阻塞, claude: 阻塞, codex: 阻塞},
            jump: {added, jumped, others, rate, paused, rows: [{id, title, at, paused: [{id, title}]}]},
            rework: {pushes: [{at, rows: [id], kind: none|revert|audit, why|null}],
                     items: [{id, title, kind, at}], summary: {n, revert, audit, rate}}}
分布 =      {n, q: [min, p25, p50, p80, max]|null}
日 =        {day, wip, done, byDomain: [{key, label, n, unassigned: bool}]|null}
窗口 =      {days: [日], done, byDomain: [{key, label, n, unassigned: bool}]|null}
阻塞 =      {open, waiting, rate, avgWait, longest: [{id, title, status, stage, minutes, on: [id]}],
             chains: [链]}
链 =        {id, title, stage, waitedBy: [链]}
collab:    {coverage,
            letters: {cells: [{from, to, mark: 需定|待审|知会|插队, n, p50|null,
                              items: [{at, first}], openN}], unpaired, openN,
                      open: [{at, first, minutes, from, to, mark}]},
            audit: {rows, audited, unaudited, rate, found, newRows, missing: [{id, title, at, stage|null}]|null},
            dispatch: {n, ok, rate, rules: [{key: R1..R5, label, bad}],
                       bad: [{name, side, model, effort, rules: [key]}],
                       hints: [{name, side, model, effort, text}], opus: [{name, row, at}]},
            idle: [{who, minutes, longest: {from, to, rows: [id]}|null}]}
team:      {coverage, leads: [{who, model, effort, running, since,
                              rows: [{id, title, stage, since, who, jump, deps, waitedBy}], agents}],
            agents: [{id, name, side: claude|codex, model, effort, row, rowTitle,
                      state: running|stalled|done|unknown, started, ended|null, quietMin|null, runMin|null,
                      stage, since, who, jump, deps, waitedBy}],
            sides: [{side, running, stalled, unknown, doneN}]}
health:    {coverage, at, level, summary, late: bool|null, counts: {ok, warn, bad, unknown},
            groups: [{key, label, level, counts: {ok, warn, bad, unknown}, okTitles: [标题],
                      items: [{id, level, title, summary, advice|null}]}],
            rounds: [{at, warn, bad, flagged: [{group, label, items: [{level, title}]}]|null}],
            roundsSummary: {n, flagged, last: {at, warn, bad}|null},
            gates: [{checkout, at, scope, gates: [{name: G1..G5, ok: true|false|null}], run, failed, failedNames}],
            tests: {days: [{day, count, lines, ratio, quickSec}]},
            hooks: {selftest: {ok, total, failed: [一句], level}, errors: [{hook, n, last}]}}
failures:  {coverage: {from: 记账起点|null},
            window: {size, segment, commits|null, from: 时刻|null, n|null, rate|null,
                     unclassified|null, fresh|null, trend: better|worse|same|null, last: {at, minutes}|null},
            majors: [{key, label, n|null, rate|null, lastSegment|null, recent: [记录id],
                      subs: [{key, label, note, n|null, rate|null, lastSegment|null,
                              added|null, fresh: bool|null, recent: [记录id]}]}],
            segments: [{from: 提交序号, to: 提交序号, commits, n|null, partial: bool,
                        byMajor: {大类key: 次数|null}, recent: [记录id]}]|null,
            records: {记录id: {at, major|null, sub, who: claude|codex|null, row|null, note}}, recent: [记录id]}
system:    {coverage, timings: [{key, label}], hooks: [{id, side, name, timings, purpose, ok}],
            kinds: [{key, label, group: 规则|接入要点|提醒, n24, last}],
            domains: [{key, label, index, docs: [{code, title, parent}]}],
            graph: {all: 图, claude: 图, codex: 图},
            collide: {total24, items: [{key, label, n24}]},
            scripts: {total, groups: [{key, label, note,
                                      items: [{name, purpose, usage|null, outputs|null, callers: [一句]}]}]}}
图 =        {nodes: [{id, layer: 0|1|2|3, key, label, n24, regs,
                     in: [{id, n24}], out: [{id, n24}]}],
             links: [{layer: 0|1|2, from: 节点id, to: 节点id, n24}]}
feed:      {coverage, days: [{day, cells: [{side: null|claude|codex, type, n}]}],
            items: [{id, at, type: decision|notice|milestone|done|revert|audit, side|null, title, detail|null,
                     day: 本地日期|null, windows: {24h: bool|null, 7d: bool|null, 30d: bool|null},
                     link: {doc, find}|{path}|null}], windows: {24h|7d|30d: {from: 日期, to: 日期}}}
docs:      {count, domains: [{key, name, index, docs: [{code, title, summary, keywords, parent|null, updated}]}]}
now:       {phase: {step: P0|P1|P2|P3|null, text, priority},
            kpis: [{key, label, value|null, unit, note, trend: better|worse|same|null, href}]}
perf:      {coverage: {from: 最早会话开始时刻|null}, caliber, units, labels: {levels, phases},
            recording: {since: ISO|null, minutes: 分钟|null}|null,
            retention: {authorSessions, authorBytes, aiBatches, historyPerGroup, diagnosticBytes},
            metrics: [{key, label: 中文, unit, scope: cost|experience}],
            series: [{key, label: 中文, unit, scope: cost|experience}],
            latest: {play: 会话id|null, ai: 会话id|null},
            sessions: [性能会话], trends: [性能趋势], details: {会话id: 性能曲线|null},
            displayCoverage: {sessions: 显示会话数, totalSessions: 来源会话数, note}}
保留项 =    {value: 现值|null, limit: 来源上限|null, unit}
性能会话 =  {id, origin: play|ai, originLabel: 作者|AI, scope: full|cost, scopeLabel, scopeReason,
            status, statusLabel, statusReasons, startedAt, endedAt, recordedSeconds, calc,
            build: {seq: 主线序号|null, subject: 首行|null, dirty: bool|null, configuration, behindMainline: 落后主线提交次数|null, reason},
            comparisonKey, comparisonLabels, comparisonMissing, coverage, validity,
            all: {values: {指标key: 数|null}, reasons: {缺数指标key: 中文原因}}, steady: 同all,
            targets: [{metric, label, value, budget, budgetSource, applicable, ratio, exceeded, reason, line: 适用预算|null}],
            frameLines: [{key: targetFrameMs|heavyStutterMs|severeStutterMs|freezeMs, label, value: 数|null, unit: ms, source: 中文来源, reason: 中文缺数原因|null}],
            intervals: {context: [{state, label, fromMs, toMs, firstFrame, lastFrame}], segments: [带label的分析器活动事件], gaps: [缺口事件]},
            anomalyCounts, anomalyTotal, anomalies: [分析器异常加typeLabel与levelLabel],
            retained: {raw, summary, anomalies, hotframes}, vramCapacityMiB: 容量|null}
性能趋势 =  {key, scope, origin, comparisonKey, calc, status: stale|unsupported|null, label: 各次运行摘要的中位,
            points: [{seq, subject, dirty: false, n: 运行数, metrics: {指标key: 中位数|null},
                      anomaliesPerMinute: 每分钟异常组数的运行摘要中位|null, anomalyRateRuns: 有效分母运行数, anomalyRateReason: 中文原因|null}]}
性能曲线 =  {bucketSeconds, columns: [列名], buckets: [[按columns排列的值]],
            hotframeColumns: [列名], hotframes: [[逐帧值]], hotframesTotal: 派生总行数,
            seriesReasons: {缺数曲线key: 中文原因}, reason}
```

- `perf` 单位独立：帧与渲染耗时 ms、显存 MiB、比例 0–1、功耗 W、频率 MHz、温度 °C；`recordedSeconds` 是秒，区间及桶边界是进程单调时钟 ms，只有 recording.minutes 是分钟。归一化负载、GPU 忙碌比及预算 ratio 可超过 1，指标目录分别标“倍”。未知 null，零行 coverage 保留分析器的 null，不伪造空闲、正常或零成本。
- 性能会话按完成时刻新到旧；latest 各来源独立取最新已关闭会话，缺作者或 AI 来源保留 null。all 与 steady 对应分析器全样本、明确稳态摘要，视图只投影已有值，不重新计算分位与 low。体验指标不适用时 reasons 给原因；cost 固定写“隔离桌面只判成本，不测帧时与帧率”，帧时、帧率与 GPU 占用均不画零。两列显存分别是 NVML 整卡已用与引擎记账；容量缺测不算比例。主视口 GPU 与视口 CPU 渲染允许近似 ±2 帧成本，只作分布、预算与趋势，不逐帧归因，也不从 GPU 占用猜瓶颈。目标线 line 仅适用时给值。
- `build.behindMainline` 用共享主线表的最新序号减构建序号，构建不在表中时为 null、原因沿用 build.reason；`all/steady.values.low01TailN` 与 `validFrameN` 原样投影分析器的尾样本数与有效帧数，不据 low 值反推。`frameLines` 的目标帧时复用分析器对记录清单限帧、垂直同步与刷新率的解释；重卡顿、严重卡顿和卡死阈值读取本轮分析器预算，成本口径、历史格式或来源不足给 null 与中文原因。
- 跨提交趋势按比较键、scope、origin、calc 分组，点按主线序号升序，对同提交各次运行的 steady 摘要取中位；身份缺失或 dirty 不为 false 的运行只保留会话证据，不归入主线趋势。成本指标为目录前五项；体验趋势只给作者：平均帧率、1% low、0.1% low、帧时 p99、GPU 占用均值。保留层由 retained 如实给出，历史摘要只剩 history 时仍显示 summary 层；旧 calc 的会话与趋势标 stale，不能当当前基线。历史趋势直接读取分析器已有 history，保留上限从 index 与记录器清单快照取，不抄第二套常量。
- 每提交 `anomaliesPerMinute` 先把每次运行分析器的异常组总数除以真实 `recordedSeconds` 再乘 60，最后取同提交运行摘要中位；不是原始发生次数，未测、无分析摘要或时长无效的运行不记零，参与数量与缺数原因分别给出。旧记录格式标 unsupported，只作历史证据；公开原因与说明由后端提供中文。
- 性能曲线只给每来源最新会话，唯一来源为分析器的 `derived/<id>/curves.json`；档位、有效值聚合与高耗时帧投影由 `scripts/perf/perf_curves.py` 产出，计算口径以 `scripts/perf/derived-fields.json` 为准。每桶一秒或更粗，最多每会话 900 桶（整份最多 1800）；columns 前三列为 fromMs、toMs、frames，其后按 series 顺序交替为 `keyMean`、`keyMax`。空桶不跨缺口补线，非稳态与活动区间由 intervals 覆盖。hotframes 按 hotframeColumns 解包，最多前 1000 行，hotframesTotal 给分析器选中总行数。
- 为守 1 MB，view 只选择已有曲线档位、裁掉更早的非最新会话与趋势点，并以 displayCoverage 公开显示范围；不读逐帧原始文件、不重新聚合或计算帧率与归一化负载。缺测保留 null 与分析器的 seriesReasons；`gpuUtilSampleAgeMs`、`gpuUtilSamplingWindowMs` 不以查询时刻或间隔顶替原生来源。旧 calc 或没有可用曲线的会话保留其状态与缺数原因。
- attention 仅公开当前 revision 未过目的 items，按 at 降序，未知在末尾；open 等于其条数。
- hookfeed 四列固定按上述角色顺序，每列 items 为最近 8 次真实有注入的事件，新的在前。rules 与事件列表共读同一次 guard journal：只匹配同轮 people 样本中的当前主会话身份；子代理只取 running=true 的集合，Claude 侧还须属于当前主会话，Codex 侧沿用 people 的主线后代集合。没有在场身份时 rules=null，显示空态；有身份但最新 journal 缺 rulesAt 或 rulesCalls（含字段启用前的历史）则 at/calls/late 全为 null，显示未知，不从旧记录或其他会话补值。两字段齐全时，at 由 journal.rulesAt（最后整套顶层 stdout 成功交付的 Unix 毫秒）转 ISO，calls 原样取 journal.rulesCalls（此后工具数，刚完整交付为 0），不按事件或块名重算。多位在跑子代理取 rulesAt 最早的一位的成对事实，任一位事实未知则整列未知。late 仅由 view 用同轮当前时刻判满 30 分钟；未来到期点交既有 next_expiry，前端仅格式化时间、呈现次数及提示色。
- progress.dwell 为三侧×五状态固定 15 项，当前已知起点的样本数为 n，med 为相同 min 的中位数。未结束 history 段 to=null；stageP80 只用同状态真实结束段。since 是进入当前状态的真实时刻。waiting 表示等前置或就绪且前置未完，或暂停。
- flow.wip.days 固定含今天的 30 个自然日，升序；windows 的 days 分别给最近 14/30 日，done 从相同 days 汇总，任一天未知则总数 null。当前队列只有技术目录，没有可靠中文领域，所有 byDomain 保留 null，不按目录猜领域；有明确来源才使用所列数组形状。前 7 天覆盖不足时 leadTime.prev=null，stages 对应条目保留且 parts=null。
- blocked.rate=waiting/open；jump.rate=jumped/added，others=added-jumped，jump 覆盖不足保留 rate=null。链每一级含 stage。rework.pushes 是滚动近 30 天全部真实推送，时间升序；summary 同源，revert 优先于 audit，rate=(revert+audit)/n，完整历史或分母不足时为 null。none 只表示没有显式返工证据，不表示已审计。
- team 行字段复制同轮 progress，队列无对应行则为 null。agents 按 stalled、running、unknown、done 排，未交回按开始升序，交回按结束降序；runMin 只用真实开始和在跑时刻或真实交回。leads.agents 与 sides 同源，等于 running+stalled；unknown 单列，doneN 为近一小时真实交回，覆盖不足为 null。停止、失败不算交回。
- letters.open 是唯一未回信平铺列表，minutes 降序，未知在末尾；cells 只给对应 openN，抽屉按 from/to/mark 筛同一列表。audit.missing 与 unaudited 同一次差集，已知时条数相等，已不在队列的 stage=null；rate=audited/rows，dispatch.rate=ok/n。覆盖或来源不足为 null。
- health 各组 counts 相加等于总 counts；groups.items 包含全部项，按 bad、warn、unknown、ok 排，正常标题同源。rounds 为最近 48 轮、时间升序；roundsSummary.last 只取同一末轮。钩子自测只在 hooks.selftest，其 level 与巡逻结论同源；at 为真实黑盒完成时刻，同源复用不改时刻，health.at 仍为巡逻确认时刻；无结构化计数则 ok/total=null。late 只由真实巡逻时刻和登记间隔决定，未知不判正常。
- failures 的窗口大小与段长只读 `monitor/failure-types.json`，读取共用 `read/failures.snapshot()`，不复述数值。窗口按登记时的 origin/main 提交序号取 `(current-size, current]`；起点所在提交的首次登记同样保留，分母为 `min(size, current-start.seq)`。rate=次数÷实际提交数×segment，不足一段为 null；majors 与 subs 的 lastSegment 表示最近一个 segment 的次数，段长同源取 window.segment，不输出旧字段。没开始记账时 coverage.from、窗口指标、类别计数与 fresh、segments 均为 null；原始读取或 Git 失败发布 errors。
- failures.segments 固定覆盖窗口、旧到新，from 不含、to 包含；整体在起点之前 n/byMajor 为 null，跨起点 partial=true，不把未记账写为零。trend 仅在最新段已完整覆盖时与此前完整段次数均值比较，没有前序完整段为 null。未收录子类计入 window.n/unclassified，不进大类。子类别名由原始 reader 唯一解析。fresh 只计真实 added 晚于记账起点、位于已知窗口内的子类，种子 added=null 不算；window.fresh 同源汇总。
- failures.records 为窗口内全量记录索引；recent、majors[].recent、subs[].recent、segments[].recent 都由 view 排成新到旧且最多 20 条。页面按 fmajor/fsub 选择现成索引，点提交段打开对应抽屉，不排序、切窗或重新统计；不公开 head、src、by 与原始日志路径。窗口内零条显示“窗口内没有登记”，登记数不等于实际失败数。
- system 的图四层分别为时机、钩子、注入类别、文档域；id 前缀 t:/h:/k:/d:，边与 in/out 引用同一节点 id。次数有未知不把已知部分冒充完整值。collide.total24 与 items 同源，scripts.total 与 groups 同一次扫描；domains 的 key/index/docs 取同轮 docs，不另建目录来源。
- feed.days 补满含今天的 30 个自然日并升序，每日三个真实 side×六类共 18 格；不增加 side=all 重复统计。完整日覆盖不足或存在对应无日期源项时 n=null，已确认无事件才为 0；items 为同一事件源的近 30 天及无日期项。决策由既有解析器全量取，不按最近 15 条推算。已从私有通知快照删除的通知不伪造历史。

| 数据 | 来源 | 更新 |
| --- | --- | --- |
| live、team | 同一 people 样本；team 取 agents 历史与同轮 progress | 会话、队列、相关历史或公开依赖完成；会话到期 |
| attention、feedback | attention 输入事务与 read 原源 | 输入、作者信归档、真实截图事件 |
| progress | 队列与 queue 历史 | 快通道，不等 Git 或全量文档 |
| hookfeed | 同一次 guard.jsonl 与同轮 people 样本 | 钩子、会话、队列或本机标识事件；view 给出的真实到期点 |
| flow | queue、pushes、audits、progress 与共享 Git 证据 | 慢通道，合并 10 秒内变化 |
| collab | letters、audits、pushes、agents、leads、queue 历史 | 慢通道，合并 10 秒内变化 |
| health | patrol 结构化历史、门禁、代码规模、钩子、巡逻周期登记 | 原报告事件、周期登记变化、view 给出的真实到期点；不重跑检查 |
| failures | 类别清单、失败账本及 origin/main 提交序号 | 登记、新增类别或 Git 引用事件；快通道合批，不进巡逻 |
| system | 钩子登记、日志、脚本与同轮 docs | 慢通道；24 小时计数每 10 分钟到期刷新 |
| docs | 文档头与 SECTIONS.md | 文档事件，慢通道 |
| feed | 真实 Git 完成、回滚、审计、决策、私有通知 | 原源事件 |
| now | 状态头与同轮 progress、flow、collab | 原源或公开依赖完成 |

历史唯一账位于 `.collab/tools/state/dashboard-events/`，通常保留 60 天；failures 按类别清单的提交窗口修剪，记账起点永久保留。envelope.at 是观察时刻，sourceAt 只认来源明确时刻，否则 null；不拿 mtime、删行、超时或停止补造完成。完成与每日完成唯一复用 progress.evidence.finished。钩子事件和块名直接给中文；未识别写“未知事件”。决策只给一句结论。测试 count 必须来自真实测试批次，文件数不算用例；代码行数比例只描述规模，缺关联批次的门禁执行数保持 null，门禁未跑不显示通过。now.phase.step 只认状态头“当前阶段”开头明确的 P0–P3。

动态条目的预设窗口归属由 view 用同一个 now 计算，滚动区间为 `(now-duration, now]`，无来源时刻的归属为 null。页面只选择窗口标记或 day 字段，不使用浏览器时间重切窗；自然日换日、未来事件进入与已知事件移出都返回真实下一到期点。文档读取与搜索均经 view/docs，检索打分、排序与截断只在 view。

施工状态 key 为 waiting、ready、active、integration、paused，未知为 null；中文名与顺序从同次 DashQueueStates 读取，色板只按 key 保存样式。progress.dwell 固定三侧×五状态 15 项；无法识别状态时 stage/waiting=null，涉及未知状态的 WIP/阻塞全量指标保留 null。

### 2.1 推送与确认时刻

```text
GET /api/data-names.js   text/javascript; charset=utf-8
window.DashDataNames = [按 catalog.DATASETS 公开项顺序的字符串];


GET /api/events          text/event-stream; charset=utf-8
retry: 3000

event: versions
data: {at, full: true|false, versions: {数据名: 内容签名, app: 界面签名},
       checked: {数据名: {at: 最后真实确认时刻|null, stale: bool|null, ratio: number|null}}}

event: checked
data: {at: 心跳时刻,
       checked: {数据名: {at: 最后真实确认时刻|null, stale: bool|null, ratio: number|null}}}
```

首次与重连先发 full=true 的完整 versions，之后 full=false 只带变化签名；三种 SSE 出口的 checked 都包含 AUTO_NAMES 全部键，且形状一致。未知确认时刻给 at/stale/ratio=null，不保留 checked 字符串值或同级 stale/ratio 表。尚无 app 首次发布时保持流并发等待注释；元数据损坏明报断开，不取旧快照。没有 id 或历史回放，不另拉索引或轮询；app 签名变就整页刷新。

checked 心跳每 15 秒一次。事件 at 是发送时间，checked[name].at 才是最后真实确认；只在采集成功或来源无脏标记且不在算时推进。纯函数 health.freshness 唯一计算 ratio=经过秒数/应有间隔、stale=ratio>1；非法或缺失时刻、缺失或无效间隔均为 null。前端直接读取结果，不按 updated、连接在线或本地阈值判鲜。服务只读取私有 publication 时间戳表并包装字段。

### 2.2 作者过目与留言

过目项只收明确 scope=aesthetic 且带当前判断的 post 输入，以及固定机位截图总览；detail 说明“已按什么在做”，指挥来信不进作者事项。`POST /api/attention/ack` 收 `{id, revision}`，只确认实际看到的版本；缺/旧 revision 返回 409，源项消失返回 404，同版重复确认可用；操作后按当前源重建公开 attention。

`POST /api/feedback` 收 `{text}`，只写真实 Claude inbox 的独立 author 信，首行 `【作者】`，返回 `{ok: true, at}`。同秒留言不覆盖，回显最近十条，只从真实 inbox 与 inbox_wait/delivered 读；归档才 delivered=true。没有第二份全文权威，来信监视与主会话钩子负责即时送达。

通知显示文字 text/from 拦提交哈希、文件名和路径，只写高层结论；合法仓库产物经 --link 关联。

```powershell
python -I -X utf8 dashboard/tools/post.py notice "一句高层通知" --from "Codex 主线"
python -I -X utf8 dashboard/tools/post.py list
```

静态服务只放行派生名单中的公开数据、文档、已登记总览和观察台公共资源的真实文件；不开放目录列表、内部脚本、策展输入或任意 output/.collab 文件。serve 与 post 共用 read.repo.dashboard_file 的文件范围。

### 2.3 文档只读接口

```text
GET /api/doc?code=WF-RULE → {ok, code, title, domain, path, updated, md,
                            toc: [{level: 2|3, n, title, anchor}]}
GET /api/doc/search?q=词  → {ok, items: [{code, title, sec: {n, title, anchor}, snippet}]}
失败                      → {ok: false, error}；不存在返回 404
```

只认公开 docs 数据列出的编码，不接受路径；搜索最多 30 条，按命中排序。md 是原文，anchor 与阅读器真实标题一致，页面直接使用服务端 anchor。接口复用同一 Handler，与 SSE、revision 确认及截图白名单共同运行。

## 3 页面运行时

前端文件：

```
dashboard/
  index.html            外壳：页眉（两位指挥在不在跑、"N 份旧了"）、导航、页面容器、右下角提示条；按序加载 vendor/echarts、vendor/marked、/api/data-names.js（后端数据名单）、manifest、core、coverage、ui、palette、charts、charts-time、reader
  vendor/
    echarts.min.js      Apache ECharts；许可在同目录 echarts-LICENSE.txt、echarts-NOTICE.txt
    marked.min.js       marked（Markdown 渲染）；许可在同目录 marked-LICENSE.txt
  app/
    manifest.js         页面与筛选的清单，以及每份数据的展示信息；有哪些数据只由后端名单（§1 catalog.DATASETS）定
    core.js             运行时：推送订阅与按需加载、路由与筛选、面板注册与渲染（各种状态）、页眉与角标、写回
    coverage.js         卡底的记账起点：时间窗起点、哪块面板写（同一页每个起点一次）、卡底文字与刷新；依赖 core.js 的 Dash.internal，在其后加载
    ui.js               格式化与小组件、抽屉、悬停框
    palette.js          色表：语义（组, 键）→ CSS 变量与中文名；签条
    charts.js           统计图（ECharts；桑基、密度条自绘）；宿主与取色经 Dash.charts.internal 给 charts-time.js
    charts-time.js      时间轴图：trend、band 与它们共用的阈值线、区间色带、异常针；在 charts.js 之后加载
    reader.js           文档阅读器
    style.css           令牌（浅色、深色各一份）与通用组件样式
    pages/<页>.js|css   一页一对文件，只经 Dash 读数据
```

页面不经本地服务打开（file://）时整页只写"请双击「打开观察台」，经本地服务打开"。

### 3.1 推送与加载

有哪些数据只在后端一处：serve 的 `/api/data-names.js` 给出 `window.DashDataNames`（从 `catalog.DATASETS` 派生）；`manifest.js` 的 `data` 由这份名单生成，只给每份补展示信息 `label`（提示里的中文名）与 `keep`（为真时不管在哪页都保持最新：`live` 给页眉，`attention` 给角标），名单与展示信息对不上就直接报错。其余数据只在当前页有面板 `needs` 它时才拉。

- 页面订阅 `GET /api/events`（§2 推送）。`versions` 事件里某份数据的版本和已交付的不同、且此刻要它，就重拉那份文件，只重画 `needs` 它的面板；此刻不要的先丢掉缓存，切过去时显示骨架再拉，不先画旧的。全量（`full`）里没列的数据按"还没有数据"。
- 版本只在这次请求的文件真的执行了 `Dash.put` 后才记下；下载或执行失败的这份按"没读到"，下一次推送（版本或 15 s 心跳）再试。载荷里 `errors` 非空是来源采集失败：这一版照样记下，不当数据交给面板，按"没读到"写后端给的原因，等后端发下一版。
- `versions.app` 变了整页刷新。连接断开时右下角写"推送断开，正在重连"，3 s 后重连（服务返回错误时浏览器不会自己重连，由 core 重建）；连上后服务端先发全量，页面按它对一遍。
- 旧了：由后端判，推送的 `checked[名字]` 带 `stale` 与 `ratio`，页面只照着画，不写阈值；`stale` 为 null 不判断。页面没有按秒轮询，"旧了"随推送的 15 s 心跳更新。

**页眉**：两位指挥的状态签（`live.leads`：在跑实心、空闲空心、未确认灰）；右侧按钮写"数据都新"、"N 份旧了"，或有不判断的（`stale` 为 null）时写"N 份未确认"，点了到 `#health/health-fresh`。导航角标：现在页是 `attention` 里没点"知道了"的条数。

### 3.2 面板与各种状态

```
Dash.panel({ id, page, title, note, caliber?, span: 12|8|7|6|5|4, order, needs: [数据名], partial?,
             window?: '24h'|'7d'|'14d'|'30d'|'range'|'commits', uses?: ['side'|'range'|'type'|'params'], fixed?(el), render(el, data, ctx) })
ctx = { filters: {side, range, type: [键]}, params: {路由参数}, stale: bool, coverage: 时刻|null }
```

- `needs` 里任一份推来新版本就重画；`uses` 里的筛选或路由参数与这块上次画出时不同也重画（换页回来同样对一遍）；`needs: []` 的面板照常画。面板不自己订阅、不自己拉数据；面板内部状态变了（块内开关、搜索词）调 `Dash.redraw(id)`。
- render 只在数据到齐时调用；`partial` 时到一份就画，没到的那份是 null。render 抛异常时这块写"这块出错了"，细节进浏览器控制台，不影响别的面板。
- `fixed(el)` 只在面板建立时画一次，重画只换正文，不清掉它（输入框、搜索框放这里）。
- 重绘时 `<details>` 的开合在个数不变时保持。只画当前页的面板，别的页记着脏，切过去再画。
- `ctx.coverage` 取 needs 里各份 `coverage.from` 最晚的一个。
- `span` 在 1100 px 以下一律整行。

| 情形 | 外壳的表现 |
| --- | --- |
| 读取中 | 骨架 |
| 读过但没有这份 | "还没有数据" + 缺哪份（中文名） |
| 读取失败 | "没读到" + 一句原因（下载失败、文件有误，或来源发布了 `errors`），不显示旧值 |
| 旧了 | 卡头"更新 HH:MM · 旧了"橙字，正文 60% 不透明，照样能交互 |
| 记账不够一个时间窗 | 卡底一行"记账从 M 月 D 日 HH:MM 起，之前的不全"；由页面统一算：这一页上数据齐了、记账起点晚于自己时间窗的面板里，同一个起点只由 order 最小的那块写一次，任一数据加载、刷新或失败后整页重算卡底（只改卡底，不重绘正文），结果与加载先后无关，读取失败的来源不贡献起点。时间窗取面板的 `window`（不写按 `'range'`：筛选条的 range，地址里拖选了 `from` 就从那天起）；`'commits'` 是按提交次数算的窗口，不参与这套按天的判断，起点与"窗口未满"由面板按后端给的 `coverage.from` 与 `window` 自己写（`Dash.coverageText(from)` 给统一的句子）；"前 7 天"对比由面板按 `ctx.coverage` 自己隐藏 |
| 口径 | 有 `caliber` 的卡头带 ⓘ，悬停显示；图的悬停框末行也带这句（图的 `opts.caliber`） |

### 3.3 路由、筛选与写回

```
Dash.go(page, params?, block?)          地址 #page/block?k=v；同页换参数时保留地址里已有的 side、range、type（params 里写了的以它为准），换页不带
Dash.openDoc(code, {sec?, find?})       等于 Dash.go('docs', {code, sec, find})
Dash.setFilter(key, value)              改一个筛选，保留其余筛选与路由参数；默认值（side=all、range=7d、type 全选）不写进地址；改 range 时去掉 from、to
Dash.drawer({title, sub?, body}) / Dash.drawer.close()     右侧抽屉，Esc 或点外面关掉，路由不变，换页时关掉
Dash.redraw(id) / Dash.redrawAll()
Dash.dataState(name)                    wait / missing / error / ok
Dash.checkedAt(name)
Dash.ack(id, revision)                  POST /api/attention/ack {id, revision}；内容已变（409）时换成新内容，Promise 带"内容刚变过，已换成新内容，请重新过目"
Dash.feedback(text)                     POST /api/feedback {text} → {ok, at}
```

- `side`、`range`、`type` 是筛选（`ctx.filters`），其余查询键是路由参数（`ctx.params`）。`type` 写成逗号分隔，空数组即全选。页头的筛选条按 manifest `pages[].filters` 画：side 是 全部 / Claude / Codex，range 是 近 24 小时 / 近 7 天 / 近 30 天，type 是动态六类的多选签。
- 写回只经本地服务，不存浏览器；失败时 Promise 带一句人话原因（服务没在跑、服务不认这个操作、服务给的 `error`），面板照写。成功后马上重拉那份数据，推送随后到达时再对一遍。

### 3.4 格式化与小组件（ui.js）

```
Dash.h(tag.cls, props?, ...kids)  建节点；props 有 cls、text、on:{事件}、dataset，其余当属性
Dash.esc(s)  Dash.parseTime(iso)
Dash.num(n)                       千位逗号、小数一位；没有写 —
Dash.at(iso)                      今天 "HH:MM"，不是今天 "M-D HH:MM"
Dash.day(iso)                     "M 月 D 日"
Dash.minutes(m)                   "45 分钟""2 小时 5 分钟""3 天 4 小时"
Dash.ago(iso) / Dash.since(iso) / Dash.sinceMin(iso)   "12 分钟前" / "已 2 小时 5 分钟" / 距此刻的分钟数（解析不了为 null）
Dash.pct(a, b)                    "37%"；b 是 0 或 null 写 —
Dash.status(text, tone)  Dash.empty(title, sub?, compact?)  Dash.fold(summary, content, open?)
Dash.open(path, text?, cls?)      新标签页打开仓库里的产物，只写动作不露路径
Dash.item(id, title, side?)       编号 + 一句话 + 右侧签
Dash.flowbar(counts, link?)       流水线五种状态比例条；link(段) 给跳转地址；Dash.stageKeys 五种状态的键
Dash.sideKey(who)  Dash.modelName(m)  Dash.effortName(e)
Dash.lamp(level)                  ok|warn|bad 的灯，null 空心"未确认"
Dash.freshLevel(level, late)      带过期标记的结论画灯用的等级：ok 只在 late 为 false 时照画，过期按 warn，不知道过没过期按 null
Dash.kpi({label, value, unit, note, trend, vs, href})   指标卡；trend better 绿"↑ 变好"、worse 橙"↓ 变坏"、same"→ 持平"、null 不画；vs 是趋势的比较口径（如"比前 7 天"），悬停显示，不传不写
Dash.timeline(items, {compact?, onClick?})   items [{at, tone: [组, 键], tag, side?, title, sub?, body?}]；按天分组（compact 不分组）；side 画侧签；有 body 的点开展开；onClick(条目, 下标)
Dash.tree(nodes, {onClick?, filter?})        nodes [{key, label, sub?, tone?, count?, kids?, open?, current?}]；onClick(节点)；
                                             返回的节点有 .filter(文字)：按 opts.filter(节点, 文字) → bool（缺省比 label、sub、key）过滤并展开命中路径；current 高亮
Dash.dots(cells, {onHover?})      cells [{tone, tip}] 点阵，悬停显示 tip
Dash.chips(options, value, onChange, {multi?})   options [{key, label, tone?}]；单选 onChange(键)，多选 onChange(键数组)
Dash.freshness()                  [{name, label, checked, stale: bool|null, ratio}]；null 是后端不判断，不算新；调用它的面板随推送心跳重画
Dash.tip.show(行数组, x, y) / Dash.tip.hide()    悬停框；带 data-tip 的元素自动用它（多行用换行分隔）
Dash.reader(el, {code, sec?, find?, docs})   文档阅读器（reader.js）；docs 是同轮 `docs` 数据，阅读面板 needs 它
```

**文档阅读器**：调 `GET /api/doc?code=`（§2），用 marked 渲染原文（文件头元数据不显示，原始 HTML 当文字）。左边小节目录（`toc` 的 2、3 级，可折叠，当前节随滚动高亮），右边正文；头部写编码、标题、所在域、更新时刻。小节标题的 id 用服务端给的 `anchor`；`sec` 认 `anchor`，也认小节号 `n`。正文里文档目录里有的"编码 §n"、紧跟其后的单独"§n"变成 `#docs?code=&sec=` 链接；文档里的相对链接按文档位置解析、新标签页打开，页内锚点只滚动。`find` 先到 `sec` 那一节（没给就全文）找第一处，高亮并滚过去。读不到时写"这份文档没读到：原因"。同一份文档按 `docs` 里的更新时刻缓存。

### 3.5 颜色语义（palette.js）

颜色只按语义定一套：值写在 `style.css` 的 CSS 变量里（浅色与深色各一份），`Dash.palette` 登记"（组, 键）→ 变量 + 中文名"，签条、色块和所有图都从这里取；页面与图不写颜色字面量，只传 `tone: [组, 键]`。

| 组 | 键 |
| --- | --- |
| `stage` | `waiting` 等前置、`ready` 就绪、`active` 进行中、`integration` 待集成、`paused` 暂停；名称与顺序由 DashQueueStates 提供 |
| `side` | `claude` `codex` `author` `muted` |
| `model` | `opus` `sonnet` `haiku` `gpt-6.1-sol` `gpt-6-luna` `gpt-6-astra` `other` |
| `level` | `ok` 正常、`warn` 要看、`bad` 有问题 |
| `kind` | `audit` 观感、`shots` 截图、`poster` 海报、`review` 评审 |
| `agent` | `running` 在跑（蓝）、`stalled` 可能卡住（橙）、`done` 已交回（灰）、`unknown` |
| `mark` | `需定`（橙）、`待审`（主色）、`知会`（灰）、`插队`（琥珀） |
| `feed` | `decision` 决策、`notice` 通知、`milestone` 里程碑、`done` 完成、`revert` 回滚、`audit` 审计 |
| `inject` | `规则`、`接入要点`、`提醒`（键用后端给的类别名） |
| `gate` | `pass` 过、`fail` 没过、`skip` 没跑 |
| `seq`、`timing` | 一组蓝，键是下标 |
| `cat` | 十二色分类色，键是下标；系列多、要彼此分开时用（如健康页失败大类） |
| `perf` | 异常四级 `info` 提示、`light` 轻、`heavy` 重、`severe` 严重（由灰经橙到红）；曲线 `frame` `fps` `gpu` `measured` `cpu` `setup` `util` `load` `power` `vram` `vramEngine` `capacity`；五段 `physMs` `procMs` `lateMs` `drawMs` `tailMs`；标注 `target` 目标线、`idle` 非稳态、`unknown` 上下文未知、`activity` 在做什么；来源 `ai`（作者用 `side.author`） |

```
Dash.colorVar(组, 键)  Dash.color(组, 键)  Dash.cssVar(name)  Dash.label(组, 键)  Dash.mark(组, 键, 字?)  Dash.modelKey(型号)
```

### 3.6 统计图（charts.js）

每个函数返回一个节点；节点进到可见页面、有了宽度才画，变宽变窄自动重排，面板重绘与抽屉关闭时释放，主题切换全部重画。通用 `opts`：`height`、`label`（无障碍名）、`caliber`（悬停框末行）、`unit`、`onClick(p)`，`p = {index 类别下标, key, label, value, series 系列的 key}`。

| `Dash.charts.` | 数据 | 说明 |
| --- | --- | --- |
| `spark(values, opts)` | 数组，最右是今天 | 近 N 天小柱 |
| `ring(parts, opts)` | `[{label, value, tone, key?}]` | 环形占比；`opts.center` 环心字（占比由调用方带后端给的数，图不算）；任一 value 为 null 画未确认 |
| `bars(items, opts)` | `[{label, value, text?, note?, tone, key?}]` | 横向条形；条尾写 text；悬停写 label、值与单位、note；`opts.max` 定满格；标签过长折行（`opts.labelWidth`） |
| `treemap(items, opts)` | `[{label, value, note?, tone, key?}]` | 面积图 |
| `box(groups, opts)` | `[{key, label, tone, cur: 分布, prev: 分布|null}]`，分布 `{n, q: [min, p25, p50, p80, max]|null}` | 箱线，值是分钟；本 7 天实色、前 7 天浅色虚线；样本少于 3 的组只画点并标"样本少"；`p.series` 是 cur 或 prev |
| `stack(cats, series, opts)` | `series [{key, label, tone, values}]` | 堆叠条；`opts.horizontal` 横向 |
| `combo(days, bars, line, opts)` | `bars [{key, label, tone, values}]`（多条堆叠）、`line {key?, label, tone, values}` | 柱线组合；`opts.tips` 每天一段补充，悬停附上 |
| `heat(rows, cols, cells, opts)` | rows、cols 是标签数组；`cells [{r, c, value, text?}]` | 热力；text 是格内小字；`p.index` 是 cells 下标 |
| `trend(xs, series, opts)` | `series [{key?, label, tone, values, axis: 0|1, dashed?}]`，或散点 `{label, tone, points: [{index, value, hollow?, ring?}]}` | 折线，axis 1 用右轴；散点同一横轴位置可有多点，hollow 空心、ring 加粗圈；`opts.units` 两轴单位；`opts.notes` 每点一句；`opts.titles` 每个横轴位置的悬停标题；标注见下（charts-time.js） |
| `band(xs, series, opts)` | `series [{key?, label, tone, mean, max?, axis: 0|1, dashed?}]` | 均值线 + 最大值带：有 max 的在线上方画到最大值的浅色带；null 处断开不补线；`opts.zoom {from, to}` 横轴下标，缩到这一段并可拖动平移；`opts.titles`、标注同 trend（charts-time.js） |
| `sankey(nodes, links, opts)` | `nodes [{id, label, tone, depth, value, note?}]`（id 全图唯一，value 是后端给的节点数，null 写未确认）；`links [{from, to, value}]` | 自绘 SVG；值为 0 或 null 的连线画细灰虚线；悬停节点高亮相连的线并写 note；`p.key` 是节点 id，`p.series` 是 node 或 link |
| `strip(days, opts)` | `[{day, value}]` | 密度条，一天一格，深浅按数；`opts.coverage` 之前的格画虚线框；拖选 → `opts.onBrush({from, to})`，点一格 → `onClick`；`opts.selected {from, to}` 高亮 |

trend 与 band 的标注：`opts.lines [{value, label, tone, axis?}]` 横向阈值线，只画调用方给的值；`opts.areas [{from, to, label?, tone, alpha?}]` 横轴下标区间的色带；`opts.pins [{index, value, label, tone}]` 异常针。

## 4 页面与面板

导航八页，每页顶部一句"这页回答什么"（manifest 的 `ask`），右侧是这页的筛选条；每块一个标题、一句"这块回答什么"（`note`）与 ⓘ 口径（`caliber`）。各页的块 id 列在 `pages/<页>.js` 的头注释里，每块回答什么、口径是什么写在它的 `note` 与 `caliber` 里。

| 页 | 回答什么 | 筛选 | 用到的数据 |
| --- | --- | --- | --- |
| `#now` 现在 | 要不要我过目、推进到哪、机器正不正常 | — | attention、feedback、now、progress、health、team、feed |
| `#work` 施工 | 活在哪一段、流得顺不顺 | side | progress、flow |
| `#team` 团队 | 谁在做哪行、两边配合得怎样 | — | team、collab、progress |
| `#health` 健康 | 这套开发机器正不正常，游戏本体最近在哪类失败 | — | health、failures；新鲜度用 `Dash.freshness()` |
| `#perf` 性能 | 游戏跑得顺不顺、成本花在哪、越改越快还是越慢 | — | perf |
| `#system` 体系 | 钩子与脚本怎么搭起来、在送什么 | side | system、hookfeed、docs |
| `#feed` 动态 | 最近发生了什么 | range、side、type | feed |
| `#docs` 文档 | 查文档、读文档 | — | docs、`/api/doc`、`/api/doc/search` |

现在页六块：`now-attention` 等你过目（没点"知道了"的前 3 条，其余折叠；打开、知道了，7 栏）、`now-feedback` 给主会话留言（5 栏）、`now-progress` 推进到哪（P0–P3 四格阶段条、当前阶段与当前优先级两句、流水线比例条，点段到 `#work/work-board?stage=`；阶段号为 null 不画格，8 栏）、`now-pulse` 机器与人（健康灯与要看、有问题的个数 → `#health`；两位指挥的灯与在跑子代理数 → `#team`，4 栏）、`now-kpis` 六张指标卡（点卡到 `kpis[].href`）、`now-feed` 最近 5 条动态（点了到 `#feed`）。1440×900 一屏放下前三排。

健康页在“总体”之后、“各组检查”之前依次增加 `health-fail` 指标卡（12 栏）、`health-fail-types` 大类到子类（5 栏）、`health-fail-trend` 提交段堆叠（7 栏）、`health-fail-recent` 最近登记（12 栏）；fmajor/fsub 保存在路由，筛选可清除。频率未确认显示 —；类别新增签与未收录类别提示共用既有组件与令牌。

性能页八块，路由参数 src 选来源（缺省取完成时刻更晚的那份）、z 缩到本次异常的第几条、tg 与 tm 选趋势的比较组与指标：`perf-head` 页顶口径句“这页看趋势；调优以最新原始记录为准”，正在记录时加一行“正在记录（自 HH:MM，已 N 分钟）”，按 recording.since 走时；`perf-latest` 最新数据（来源签、完成时刻、时长、构建写主线序号与首行和落后主线次数、含未提交改动另标、设置签、口径与状态签；成本卡五张，显存峰值写 NVML 整卡已用 / 容量，引擎显存记账单列；体验卡五张，0.1% low 带尾样本数，cost 会话整行写后端原因）；`perf-frames` 帧时与帧率（逐桶帧时均值线与最大值带、右轴帧率列，frameLines 给值的目标帧时与卡顿阈值线，非稳态灰纹、在做什么色带、异常色带与针；cost 会话只写原因）；`perf-render` GPU 与 CPU 渲染（主视口 GPU、已确认受测视口 GPU 合计、视口 CPU 渲染、帧准备 CPU 四条带，缺的写 seriesReasons；目标线只在 targets[].line 给出时画）；`perf-gpu` GPU 占用与显存（占用、归一化负载（提示）与功耗，占用样本龄与采样窗口写值或 seriesReasons；NVML 整卡与引擎记账两条显存线和容量线；cost 会话占用一行写原因）；`perf-anomalies` 本次异常（时刻、级别、类别、关联阶段、持续、值 / 阈值、在做什么，悬停看候选原因；点一条把三张曲线缩到前后 10 秒，重级以上改画 hotframes 逐帧五段，cost 会话挂在渲染块；只链健康页，不写失败账本）；`perf-trend` 跨提交趋势（比较组 × 指标签，横轴主线序号，线是各次运行摘要的中位、细点单次运行、空心含未提交改动、圈最新，体验指标只给作者组；下方每分钟异常数）；`perf-sessions` 会话列表（每会话一行与留到哪层，卡底保留统计现值 / 上限）。后端的原因与说明原文照写。各块都按 window 'commits' 自写卡底，不用外壳的按天记账起点。

**加页面、面板、数据源**：
- 加页面：manifest 的 `pages` 加一项（`id`、`title`、`ask`、`file`、`filters?`），建 `pages/<页>.js` 与同名 `.css`，用 `Dash.panel` 注册。
- 加面板：所属页文件里再写一个 `Dash.panel`，写清 `title`、`note`，有统计口径的写 `caliber`。
- 加数据源：先按 §2 定形状，在 `catalog.DATASETS` 登记名字与调度属性，并在 collect.PRODUCERS 的实际导出中接入唯一生产函数；公开名单和调度属性自动派生。manifest 只补展示信息，启动核对接合。
- 面板只用 `Dash.*` 与 `style.css` 的通用类（`.card .chip .mark .item .status .empty .fold .btn .id .muted .kpi .lamp .tl .tree .dots .drawer .seg .note-line .cols .col`），自己的样式写进同名 `.css`，只用令牌，不写颜色字面量、阴影与渐变。

## 5 视觉

- **颜色有固定语义**（§3.5），与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制，红＝有问题，橙＝要看，绿＝正常；流水线五种状态各一色；结构、文字、分隔一律用中性蓝灰。浅蓝主色只给当前页、当前项、今天与主按钮，不为装饰换色。
- **一眼读懂**：字号偏大（正文 15 px、块标题 17 px、关键数 26–36 px），留白够；每块先给一个数或一句结论，再给图；红色只给有问题的事；时刻写具体时间。页面上不出现英文字段名、路径、哈希与原始 JSON。
- 浅色为主，跟随系统深色（`prefers-color-scheme`，令牌各一份）；字体用系统中文无衬线，数字等宽；统计图统一经 charts.js，比例条、时间线、点阵这类简单图形用 HTML 与 CSS。窄屏（390 px）单列、不横向溢出。
- **本地预览**：`python -I -X utf8 dashboard/tools/serve.py`，打开 `http://127.0.0.1:8765/`；已在运行时直接打开该地址。

## 6 采集与维护

- `collect.py`：按 §1 的唯一原生通知与到期调度运行（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），读权威来源：`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/registry/REG-DECISIONS-*`、固定机位总览，以及经监控库读的本机协作数据与巡逻摘要（见第 1 节）。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。改了 `live.py` 或监控库以后要重启它。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
