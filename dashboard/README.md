# 作者观察台

作者了解项目的唯一入口：现在、施工、团队、健康、体系、动态、文档七页，回答当前推进、流水线、协作、运行状况、开发结构、最近变化与文档内容。双击 `打开观察台.cmd`，启动唯一实时采集与回环 HTTP 服务。作者过目不挡推进（D-274），工程、架构与排期由两位指挥判断。

统计口径、时间窗口、排序与未知判断统一在 `monitor/view/`；页面展示现成结果，保留按方向筛抽屉、按 parent 展开文档树、拼编号和用 `Dash.since` 显示经过时长等展示行为。历史不全照实显示已有事实，缺项保留“未确认”；不公开原始日志或完整会话。数据契约见 §1–§2，页面与交互见 §3–§5。

## 1 采集与写入

- `live.py --loop` 是唯一采集调度，使用一个 watchdog `WindowsApiObserver`，与历史 Recorder 共用原生文件通知。输入变化只标脏，快慢工作分开；采集期间再次变化会接着处理最新输入，无变化不重采整套数据。
- `collect.py [数据名…]` 按现有生产表只算点名数据，按 `@` 依赖顺序回写同轮 `context.values`，完成一份即发布一份。两位指挥、子代理与运行灯共用 people 样本；team 同轮依赖 progress。时间变化复用现有 `next_expiry` 到期调度，不做周期全扫、不因 SSE 心跳重读来源。 有真实到期点的 view 显式返回 `(payload, expiry)`；采集器保存到期，调度只重算对应份，不经 context 隐式回写。
- `.collab/tools/monitor/read/` 持有来源 IO 与解析，`view/` 计算业务。巡逻周期唯一经 `read.collab.patrol_interval_seconds()` 读取 `.collab/tools/README.md` 的明确登记；缺失或不明确为 null。health 在 view 计算 late 与私有到期点，调度只按该点重算 health，并监听登记变化。
- `attention.py` 独占作者输入事务：输入变更、过目重建、revision 校验、确认、attention/feedback 发布。`post.py`、采集与 HTTP 入口只调用它；先持输入事务锁，再经窄发布函数取得短锁。
- `datafile.write()` 是唯一文件发布入口：原子替换 `Dash.put` 后提交内容签名与确认时刻；签名不含 updated，相同内容只推进确认。失败可发布 errors，不能推进确认。内部 `dashboard-publication.json` 只是当前发布快照，checked 是私有 name→ISO 表，不是历史或公开 SSE 载荷。
- `catalog.DATASETS` 是各份公开与内部结果的唯一登记，集中输入、确认间隔、通道、合批、行与样本需求、周期和到期属性。`datafile.AUTO_NAMES` 由公开项派生；collect 的 PRODUCERS 是唯一实际生产函数绑定，启动核对其输出名字与登记名集，live 按同表调度。`/api/data-names.js` 同次输出 `window.DashDataNames`（公开数据名）与 `window.DashQueueStates`（施工状态有序数组）；状态 key、中文名与顺序只由 `.collab/tools/monitor/queue-states.json` 定义，Python、Node 与前端共用。
- 登记的 kind 区分公开结果、共享原始事实与界面发布；collect 仅核对实际生产输出与登记的名字集合，不再保存或比较第二份函数绑定。作者事务输出由 attention.BUILDERS 导出；策展输入名单统一在 catalog.CURATED_NAMES。判鲜仅对从未确认的来源返回空，已确认来源的坏时刻或坏间隔直接报错。
- `serve.py` 只绑定 `127.0.0.1:8765`，提供静态文件、同源写入、SSE 与文档只读接口。服务 Observer 只观察发布元数据，不采集、不持发布锁等待客户端。唯一 `health.freshness(checked, intervals, now)` 纯函数算 stale/ratio；HTTP 只包装结果，业务指标不在服务计算。

`dashboard/data/auto/` 是程序结果；`curated/notices.js` 与 `curated/audit.js` 是私有输入，只经 post.py 改，通知汇入公开 feed。作者信全文只从真实 inbox 与已送达档读取，截图只来自已登记检出的两种固定机位总览。index、app、vendor 内容签名发布为 versions.app。

## 2 数据契约 v3

数据文件用 `Dash.put(名字, 数据)` 交付，附 `updated`、固定生产方 `source`、`errors`。时刻为 ISO，时长为分钟；`rate`、`ratio` 为 0–1 口径的小数，freshness.ratio 是经过时长/应有间隔，可大于 1。比例显示用 `Dash.pct(rate, 1)`。公开名字、顺序与间隔由 catalog.DATASETS 派生，未生成显示缺数据。index、stats、records 停产，通知仅作输入。

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
            rows: [{id, title, kind: Q|C|S, who, stage, status, since, waiting, stageP80, long, jump,
                    deps: [id], waitOn: [还没结的前置 id], waitedBy: [id], history: [{stage, from, to|null, min}]}]}
flow:      {coverage, leadTime: [{side: all|claude|codex, kind: all|Q|C, cur: 分布, prev: 分布|null}],
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
            gates: [{checkout, at, pkg, gates: [{name: G1..G5, ok: true|false|null}], run, failed, failedNames}],
            tests: {days: [{day, count, lines, ratio, quickSec}]},
            hooks: {selftest: {ok, total, failed: [一句], level}, errors: [{hook, n, last}]}}
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
```

- attention 仅公开当前 revision 未过目的 items，按 at 降序，未知在末尾；open 等于其条数。
- hookfeed 四列固定按上述角色顺序，每列 items 为最近 8 次真实有注入的事件，新的在前。rules 与事件列表共读同一次 guard journal：只匹配同轮 people 样本中的当前主会话身份；子代理只取 running=true 的集合，Claude 侧还须属于当前主会话，Codex 侧沿用 people 的主线后代集合。没有在场身份时 rules=null，显示空态；有身份但最新 journal 缺 rulesAt 或 rulesCalls（含字段启用前的历史）则 at/calls/late 全为 null，显示未知，不从旧记录或其他会话补值。两字段齐全时，at 由 journal.rulesAt（最后整套顶层 stdout 成功交付的 Unix 毫秒）转 ISO，calls 原样取 journal.rulesCalls（此后工具数，刚完整交付为 0），不按事件或块名重算。多位在跑子代理取 rulesAt 最早的一位的成对事实，任一位事实未知则整列未知。late 仅由 view 用同轮当前时刻判满 30 分钟；未来到期点交既有 next_expiry，前端仅格式化时间、呈现次数及提示色。
- progress.dwell 为三侧×五状态固定 15 项，当前已知起点的样本数为 n，med 为相同 min 的中位数。未结束 history 段 to=null；stageP80 只用同状态真实结束段。since 是进入当前状态的真实时刻。waiting 表示等前置或就绪且前置未完，或暂停。
- flow.wip.days 固定含今天的 30 个自然日，升序；windows 的 days 分别给最近 14/30 日，done 从相同 days 汇总，任一天未知则总数 null。当前队列只有技术目录，没有可靠中文领域，所有 byDomain 保留 null，不按目录猜领域；有明确来源才使用所列数组形状。前 7 天覆盖不足时 leadTime.prev=null，stages 对应条目保留且 parts=null。
- blocked.rate=waiting/open；jump.rate=jumped/added，others=added-jumped，jump 覆盖不足保留 rate=null。链每一级含 stage。rework.pushes 是滚动近 30 天全部真实推送，时间升序；summary 同源，revert 优先于 audit，rate=(revert+audit)/n，完整历史或分母不足时为 null。none 只表示没有显式返工证据，不表示已审计。
- team 行字段复制同轮 progress，队列无对应行则为 null。agents 按 stalled、running、unknown、done 排，未交回按开始升序，交回按结束降序；runMin 只用真实开始和在跑时刻或真实交回。leads.agents 与 sides 同源，等于 running+stalled；unknown 单列，doneN 为近一小时真实交回，覆盖不足为 null。停止、失败不算交回。
- letters.open 是唯一未回信平铺列表，minutes 降序，未知在末尾；cells 只给对应 openN，抽屉按 from/to/mark 筛同一列表。audit.missing 与 unaudited 同一次差集，已知时条数相等，已不在队列的 stage=null；rate=audited/rows，dispatch.rate=ok/n。覆盖或来源不足为 null。
- health 各组 counts 相加等于总 counts；groups.items 包含全部项，按 bad、warn、unknown、ok 排，正常标题同源。rounds 为最近 48 轮、时间升序；roundsSummary.last 只取同一末轮。钩子自测只在 hooks.selftest，其 level 与巡逻结论同源；at 为真实黑盒完成时刻，同源复用不改时刻，health.at 仍为巡逻确认时刻；无结构化计数则 ok/total=null。late 只由真实巡逻时刻和登记间隔决定，未知不判正常。
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
| system | 钩子登记、日志、脚本与同轮 docs | 慢通道；24 小时计数每 10 分钟到期刷新 |
| docs | 文档头与 SECTIONS.md | 文档事件，慢通道 |
| feed | 真实 Git 完成、回滚、审计、决策、私有通知 | 原源事件 |
| now | 状态头与同轮 progress、flow、collab | 原源或公开依赖完成 |

历史唯一账位于 `.collab/tools/state/dashboard-events/`，保留 60 天。envelope.at 是观察时刻，sourceAt 只认来源明确时刻，否则 null；不拿 mtime、删行、超时或停止补造完成。完成与每日完成唯一复用 progress.evidence.finished。钩子事件和块名直接给中文；未识别写“未知事件”。决策只给一句结论。测试 count 必须来自真实测试批次，文件数不算用例；代码行数比例只描述规模，缺关联批次的门禁执行数保持 null，门禁未跑不显示通过。now.phase.step 只认状态头“当前阶段”开头明确的 P0–P3。

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
  index.html            外壳：页眉（两位指挥在不在跑、"N 份旧了"）、导航、页面容器、右下角提示条；按序加载 vendor/echarts、vendor/marked、/api/data-names.js（后端数据名单）、manifest、core、ui、palette、charts、reader
  vendor/
    echarts.min.js      Apache ECharts；许可在同目录 echarts-LICENSE.txt、echarts-NOTICE.txt
    marked.min.js       marked（Markdown 渲染）；许可在同目录 marked-LICENSE.txt
  app/
    manifest.js         页面与筛选的清单，以及每份数据的展示信息；有哪些数据只由后端名单（§1 catalog.DATASETS）定
    core.js             运行时：推送订阅与按需加载、路由与筛选、面板注册与渲染（各种状态）、页眉与角标、写回
    ui.js               格式化与小组件、抽屉、悬停框
    palette.js          色表：语义（组, 键）→ CSS 变量与中文名；签条
    charts.js           统计图（ECharts；桑基、密度条自绘）
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
             window?: '24h'|'7d'|'14d'|'30d'|'range', uses?: ['side'|'range'|'type'|'params'], fixed?(el), render(el, data, ctx) })
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
| 记账不够一个时间窗 | 卡底一行"记账从 M 月 D 日 HH:MM 起，之前的不全"；窗口取面板的 `window`（不写按 `'range'`：筛选条的 range，地址里拖选了 `from` 就从那天起）；"前 7 天"对比由面板按 `ctx.coverage` 自己隐藏 |
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
Dash.kpi({label, value, unit, note, trend, href})   指标卡；trend better 绿"↑ 变好"、worse 橙"↓ 变坏"、same"→ 持平"、null 不画
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
| `trend(xs, series, opts)` | `series [{key?, label, tone, values, axis: 0|1}]` | 折线，axis 1 用右轴；`opts.units` 两轴单位；`opts.notes` 每点一句 |
| `sankey(nodes, links, opts)` | `nodes [{id, label, tone, depth, value, note?}]`（id 全图唯一，value 是后端给的节点数，null 写未确认）；`links [{from, to, value}]` | 自绘 SVG；值为 0 或 null 的连线画细灰虚线；悬停节点高亮相连的线并写 note；`p.key` 是节点 id，`p.series` 是 node 或 link |
| `strip(days, opts)` | `[{day, value}]` | 密度条，一天一格，深浅按数；`opts.coverage` 之前的格画虚线框；拖选 → `opts.onBrush({from, to})`，点一格 → `onClick`；`opts.selected {from, to}` 高亮 |

## 4 页面与面板

导航七页，每页顶部一句"这页回答什么"（manifest 的 `ask`），右侧是这页的筛选条；每块一个标题、一句"这块回答什么"（`note`）与 ⓘ 口径（`caliber`）。各页的块 id 列在 `pages/<页>.js` 的头注释里，每块回答什么、口径是什么写在它的 `note` 与 `caliber` 里。

| 页 | 回答什么 | 筛选 | 用到的数据 |
| --- | --- | --- | --- |
| `#now` 现在 | 要不要我过目、推进到哪、机器正不正常 | — | attention、feedback、now、progress、health、team、feed |
| `#work` 施工 | 活在哪一段、流得顺不顺 | side | progress、flow |
| `#team` 团队 | 谁在做哪行、两边配合得怎样 | — | team、collab、progress |
| `#health` 健康 | 这套开发机器正不正常 | — | health；新鲜度用 `Dash.freshness()` |
| `#system` 体系 | 钩子与脚本怎么搭起来、在送什么 | side | system、hookfeed、docs |
| `#feed` 动态 | 最近发生了什么 | range、side、type | feed |
| `#docs` 文档 | 查文档、读文档 | — | docs、`/api/doc`、`/api/doc/search` |

现在页六块：`now-attention` 等你过目（没点"知道了"的前 3 条，其余折叠；打开、知道了，7 栏）、`now-feedback` 给主会话留言（5 栏）、`now-progress` 推进到哪（P0–P3 四格阶段条、当前阶段与当前优先级两句、流水线比例条，点段到 `#work/work-board?stage=`；阶段号为 null 不画格，8 栏）、`now-pulse` 机器与人（健康灯与要看、有问题的个数 → `#health`；两位指挥的灯与在跑子代理数 → `#team`，4 栏）、`now-kpis` 六张指标卡（点卡到 `kpis[].href`）、`now-feed` 最近 5 条动态（点了到 `#feed`）。1440×900 一屏放下前三排。

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
