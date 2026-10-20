# 作者观察台

作者了解项目的唯一入口：现在、施工、团队、健康、体系、动态、文档七页，回答当前推进、流水线、协作、运行状况、开发结构、最近变化与文档内容。双击 `打开观察台.cmd`，启动唯一实时采集与回环 HTTP 服务。作者过目不挡推进（D-274），工程、架构与排期由两位指挥判断。

统计口径、时间窗口、排序与未知判断统一在 `monitor/view/`；页面展示现成结果，保留按方向筛抽屉、按 parent 展开文档树、拼编号和用 `Dash.since` 显示经过时长等展示行为。历史不全照实显示已有事实，缺项保留“未确认”；不公开原始日志或完整会话。数据契约见 §1–§2，页面与交互见 §3–§5。

## 1 采集与写入

- `live.py --loop` 是唯一采集调度，使用一个 watchdog `WindowsApiObserver`，与历史 Recorder 共用原生文件通知。输入变化只标脏，快慢工作分开；采集期间再次变化会接着处理最新输入，无变化不重采整套数据。
- `collect.py [数据名…]` 按现有生产表只算点名数据，按 `@` 依赖顺序回写同轮 `context.values`，完成一份即发布一份。两位指挥、子代理与运行灯共用 people 样本；team 同轮依赖 progress。时间变化复用现有 `next_expiry` 到期调度，不做周期全扫、不因 SSE 心跳重读来源。
- `.collab/tools/monitor/read/` 持有来源 IO 与解析，`view/` 计算业务。巡逻周期唯一经 `read.collab.patrol_interval_seconds()` 读取 `.collab/tools/README.md` 的明确登记；缺失或不明确为 null。health 在 view 计算 late 与私有到期点，调度只按该点重算 health，并监听登记变化。
- `attention.py` 独占作者输入事务：输入变更、过目重建、revision 校验、确认、attention/feedback 发布。`post.py`、采集与 HTTP 入口只调用它；先持输入事务锁，再经窄发布函数取得短锁。
- `datafile.write()` 是唯一文件发布入口：原子替换 `Dash.put` 后提交内容签名与确认时刻；签名不含 updated，相同内容只推进确认。失败可发布 errors，不能推进确认。内部 `dashboard-publication.json` 只是当前发布快照，checked 是私有 name→ISO 表，不是历史或公开 SSE 载荷。
- `datafile.AUTO_NAMES` 是唯一有序 name→确认间隔秒表；现有间隔从原前端真实值迁入，页面与 manifest 不再保存阈值。`/api/data-names.js` 同次响应给 `window.DashDataNames` 字符串数组和 `window.DashDataIntervals` 对象，均从此表生成；前端依名单生成数据项，只补展示元数据，不复制名单。
- `serve.py` 只绑定 `127.0.0.1:8765`，提供静态文件、同源写入、SSE 与文档只读接口。服务 Observer 只观察发布元数据，不采集、不持发布锁等待客户端。唯一 `health.freshness(checked, intervals, now)` 纯函数算 stale/ratio；HTTP 只包装结果，业务指标不在服务计算。

`dashboard/data/auto/` 是程序结果；`curated/notices.js` 与 `curated/audit.js` 是私有输入，只经 post.py 改，通知汇入公开 feed。作者信全文只从真实 inbox 与已送达档读取，截图只来自已登记检出的两种固定机位总览。index、app、vendor 内容签名发布为 versions.app。

## 2 数据契约 v3

数据文件用 `Dash.put(名字, 数据)` 交付，附 `updated`、固定生产方 `source`、`errors`。时刻为 ISO，时长为分钟；`rate`、`ratio` 为 0–1 口径的小数，freshness.ratio 是经过时长/应有间隔，可大于 1。比例显示用 `Dash.pct(rate, 1)`。公开名字与顺序以 AUTO_NAMES 为准：live、attention、feedback、progress、flow、collab、team、health、system、hookfeed、feed、docs、now；未生成显示缺数据。index、stats、records 停产，通知仅作输入。

内容改变才换版本，updated 是该版内容发布时间。真实读取、解析或依赖失败发布 errors；缺事实、覆盖不足、零分母或无法确认的判断保留 null，不写成零、空闲或正常。历史带 `coverage: {from}`，不补造记账起点以前的事实；后端给出各窗口与列表顺序，前端不再排序、切窗或重算业务。

```text
live:      {leads: [{who: Claude|Codex, running: true|false|null, since}]}
attention: {open, items: [{id, kind: audit|shots|poster|review, title, detail, link, at, revision}]}
feedback:  {items: [{at, text, delivered}]}
hookfeed:  {lanes: [{role: 主会话|Claude 子代理|Codex 主线|Codex 子代理,
                    items: [{at, event: 中文事件, blocks: [中文块名], chars}]}]}
progress:  {counts: {queued, doing, finishing, paused},
            dwell: [{side: all|claude|codex, stage, n, med}],
            rows: [{id, title, kind: Q|C|S, who, stage, status, since, waiting, stageP80, long, jump,
                    deps: [id], waitedBy: [id], history: [{stage, from, to|null, min}]}]}
flow:      {coverage, leadTime: [{side: all|claude|codex, kind: all|Q|C, cur: 分布, prev: 分布|null}],
            wip: {days: [日], windows: {"14": 窗口, "30": 窗口}},
            stages: [{side, week: cur|prev, n, parts: {queued, doing, finishing, paused}|null,
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
                     link: {doc, find}|{path}|null}]}
docs:      {count, domains: [{key, name, index, docs: [{code, title, summary, keywords, parent|null, updated}]}]}
now:       {phase: {step: P0|P1|P2|P3|null, text, priority},
            kpis: [{key, label, value|null, unit, note, trend: better|worse|same|null, href}]}
```

- attention 仅公开当前 revision 未过目的 items，按 at 降序，未知在末尾；open 等于其条数。
- progress.dwell 为三侧×四阶段固定 12 项，当前已知起点的样本数为 n，med 为相同 min 的中位数。未结束 history 段 to=null；stageP80 只用同阶段真实结束段。since 是进入当前阶段的真实时刻。waiting 表示排着且前置未完，或暂停。
- flow.wip.days 固定含今天的 30 个自然日，升序；windows 的 days 分别给最近 14/30 日，done 从相同 days 汇总，任一天未知则总数 null。当前队列只有技术目录，没有可靠中文领域，所有 byDomain 保留 null，不按目录猜领域；有明确来源才使用所列数组形状。前 7 天覆盖不足时 leadTime.prev=null，stages 对应条目保留且 parts=null。
- blocked.rate=waiting/open；jump.rate=jumped/added，others=added-jumped，jump 覆盖不足保留 rate=null。链每一级含 stage。rework.pushes 是滚动近 30 天全部真实推送，时间升序；summary 同源，revert 优先于 audit，rate=(revert+audit)/n，完整历史或分母不足时为 null。none 只表示没有显式返工证据，不表示已审计。
- team 行字段复制同轮 progress，队列无对应行则为 null。agents 按 stalled、running、unknown、done 排，未交回按开始升序，交回按结束降序；runMin 只用真实开始和在跑时刻或真实交回。leads.agents 与 sides 同源，等于 running+stalled；unknown 单列，doneN 为近一小时真实交回，覆盖不足为 null。停止、失败不算交回。
- letters.open 是唯一未回信平铺列表，minutes 降序，未知在末尾；cells 只给对应 openN，抽屉按 from/to/mark 筛同一列表。audit.missing 与 unaudited 同一次差集，已知时条数相等，已不在队列的 stage=null；rate=audited/rows，dispatch.rate=ok/n。覆盖或来源不足为 null。
- health 各组 counts 相加等于总 counts；groups.items 包含全部项，按 bad、warn、unknown、ok 排，正常标题同源。rounds 为最近 48 轮、时间升序；roundsSummary.last 只取同一末轮。钩子自测只在 hooks.selftest，其 level 与巡逻结论同源；无结构化计数则 ok/total=null。late 只由真实巡逻时刻和登记间隔决定，未知不判正常。
- system 的图四层分别为时机、钩子、注入类别、文档域；id 前缀 t:/h:/k:/d:，边与 in/out 引用同一节点 id。次数有未知不把已知部分冒充完整值。collide.total24 与 items 同源，scripts.total 与 groups 同一次扫描；domains 的 key/index/docs 取同轮 docs，不另建目录来源。
- feed.days 补满含今天的 30 个自然日并升序，每日三个真实 side×六类共 18 格；不增加 side=all 重复统计。完整日覆盖不足或存在对应无日期源项时 n=null，已确认无事件才为 0；items 为同一事件源的近 30 天及无日期项。决策由既有解析器全量取，不按最近 15 条推算。已从私有通知快照删除的通知不伪造历史。

| 数据 | 来源 | 更新 |
| --- | --- | --- |
| live、team | 同一 people 样本；team 取 agents 历史与同轮 progress | 会话、队列、相关历史或公开依赖完成；会话到期 |
| attention、feedback | attention 输入事务与 read 原源 | 输入、作者信归档、真实截图事件 |
| progress | 队列与 queue 历史 | 快通道，不等 Git 或全量文档 |
| hookfeed | guard.jsonl | 钩子记账事件 |
| flow | queue、pushes、audits、progress 与共享 Git 证据 | 慢通道，合并 10 秒内变化 |
| collab | letters、audits、pushes、agents、leads、queue 历史 | 慢通道，合并 10 秒内变化 |
| health | patrol 结构化历史、门禁、代码规模、钩子、巡逻周期登记 | 原报告事件、周期登记变化、view 给出的真实到期点；不重跑检查 |
| system | 钩子登记、日志、脚本与同轮 docs | 慢通道；24 小时计数每 10 分钟到期刷新 |
| docs | 文档头与 SECTIONS.md | 文档事件，慢通道 |
| feed | 真实 Git 完成、回滚、审计、决策、私有通知 | 原源事件 |
| now | 状态头与同轮 progress、flow、collab | 原源或公开依赖完成 |

历史唯一账位于 `.collab/tools/state/dashboard-events/`，保留 60 天。envelope.at 是观察时刻，sourceAt 只认来源明确时刻，否则 null；不拿 mtime、删行、超时或停止补造完成。完成与每日完成唯一复用 progress.evidence.finished。钩子事件和块名直接给中文；未识别写“未知事件”。决策只给一句结论。测试 count 必须来自真实测试批次，文件数不算用例；代码行数比例只描述规模，缺关联批次的门禁执行数保持 null，门禁未跑不显示通过。now.phase.step 只认状态头“当前阶段”开头明确的 P0–P3。

### 2.1 推送与确认时刻

```text
GET /api/data-names.js   text/javascript; charset=utf-8
window.DashDataNames = [按 AUTO_NAMES 键顺序的字符串];
window.DashDataIntervals = {数据名: 应有确认间隔秒};

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
python -I -X utf8 dashboard/tools/post.py audit "海报取向供你过目" --why "请看主视觉观感" --doing "主会话选定的画面方向" --kind poster --link output/poster.jpg --from "主会话"
```

静态服务按 AUTO_NAMES 放行公开数据与已登记检出的固定机位总览，审美私有输入不公开。

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
  index.html            外壳：页眉（一句总述、两位指挥在不在跑、数据时刻）、导航、页面容器、右下角提示条；按序加载 vendor/echarts、manifest、core、ui、palette、charts
  打开观察台.cmd         启动实时采集（没在跑时）、跑一次项目采集、用默认浏览器打开 index.html
  vendor/
    echarts.min.js      Apache ECharts 单文件；许可文本在同目录 echarts-LICENSE.txt、echarts-NOTICE.txt
  app/
    manifest.js         页面与数据源的唯一清单；加页面、数据源只改这里
    core.js             Dash 运行时：数据按需加载与版本索引、路由、面板注册与渲染、页眉与导航角标、写回（知道了、留言）
    ui.js               格式化与小组件：h()、确切的数与时刻、状态句、空状态、折叠、编号对象一行、流水线比例条、指挥与模型的中文名
    palette.js          色表：语义（组、键）→ CSS 变量与中文名；签条
    charts.js           统计小图（基于 ECharts）：趋势小柱、环形占比、横向条形、面积图
    style.css           令牌（颜色、字号、间距、字体，浅色与深色各一份）与通用组件样式
    pages/<页>.js|css   一页一对文件：home、progress、team、system、library；只经 Dash 读数据
  data/                 本地状态，不进 git（.gitignore）
    auto/<名字>.js      采集脚本生成，任何人不手改
    curated/<名字>.js   只经 tools/post.py 改
```

### 3.1 数据与加载

`manifest.js` 的 `data` 登记契约 v2 的数据：`name`、`label`（页眉弹层与"还没有数据"提示里的中文名）、`path`（相对 `dashboard/`）、`every`（秒，定时拉取间隔）、`stale`（秒，超过算"数据旧了"，`null` 不判断），以及两个开关：

- `poll: true`：不看版本索引，每 `every` 秒拉一次，永远要（`live`、`index`）。
- `keep: true`：页眉与导航要用，不管在哪一页都保持最新（`attention`、`progress`）。
- 其余数据只在当前页有面板 `needs` 它时才拉。

**版本索引**：`index` 是 `{ versions: { 名字: 版本串 } }`，每 15 s 拉一次；某份数据在索引里的版本变了才重拉；版本只在这次请求的文件真的执行了 `Dash.put` 之后才记下，下载失败或文件执行出错不推进版本、隔 15 s 再试，期间这份数据按没读到处理（面板写"没读到"），旧缓存不冒充新数据。索引没到、没有 `versions`、旧了，或索引里没列这份数据时，这份数据按自己的 `every` 定时拉，右下角提示条写明。第一次读完索引之前不拉非 `poll` 数据。

**整页刷新**：`live` 带 `appStamp`（界面代码最后改动时刻），变了就 `location.reload()`，开着的页面不跑旧代码。

**页眉**：一句总述（在做、收尾几行，今天完成几行，取 `progress`）；两位指挥的状态签（`live.leads`：在跑实心灯、空闲空心灯）；右侧数据时刻按钮，点开列每份数据的更新时刻与旧不旧。导航角标：首页是 `attention` 里没点"知道了"的条数。

### 3.2 面板

```
Dash.panel({ id, page, title?, note?, span?, order?, needs?, partial?, fixed?(el), render(el, data) })
```

| 字段 | 含义 |
| --- | --- |
| `id` | 面板 id，全局唯一；也是 `#页/块` 路由的块名 |
| `page` | 挂在哪一页（manifest 的页 id） |
| `title`、`note` | 卡片标题与"这块回答什么"那一句；不写 `title` 就没有卡片头 |
| `span` | 占 12 栏里的几栏：12（默认）、8、7、6、5、4；1100 px 以下一律整行 |
| `order` | 页内排序，小的在上（默认 100） |
| `needs` | 数据名数组：进到本页就拉，全部到齐才画（`partial: true` 时到一份就画，没到的是 `null`）；任一份变了重绘 |
| `fixed(el)` | 只在挂上时画一次、之后不随数据重画的部分（留言输入框），画在数据内容上面 |
| `render(el, data)` | 往 `el` 里画；`data` 是 `{ 名字: 数据 }`；抛异常时这块显示"这块出错了"，不影响别的面板 |

只画当前页的面板，别的页记着脏，切过去再画。重绘时 `<details>` 的开合状态在个数不变时保持。卡片右上角写"更新 HH:MM"（`needs` 里最旧的一份），数据旧了变灰。数据还没读到时画骨架，读过一次仍没有写"还没有数据"并列出缺哪份（中文名）；有读取失败的数据时写"没读到"。

### 3.3 运行时接口与写回

```
Dash.put(name, data)            数据文件调用（§2）
Dash.dataState(name)            "wait" 读取中 / "missing" 读过但没有 / "error" 没读到 / "ok"
Dash.redraw(id) / redrawAll()   重画一个面板（本地交互改了显示）/ 全部（主题切换时图按新色重建）
Dash.ack(id, revision)                    "知道了"：POST /api/attention/ack {id, revision} → {ok: true}；成功后重读 attention 与 index
Dash.feedback(text)             留言：POST /api/feedback {text} → {ok: true, at}；成功后重读 feedback 与 index
Dash.isStale(name)              按 manifest 的 stale 判断；没有数据、stale 为 null 都是 false
Dash.stale(data, seconds)       按数据自带的 updated 判断
```

写回只经本地服务 `tools/serve.py`，不存浏览器。失败时 Promise 带一句人话原因，面板照写：页面是直接打开的文件（没经本地服务）、本地服务没在运行、本地服务不认这个操作（旧版服务，返回非 JSON）、服务给的 `error`。留言框：空文本发不出，发送中按钮置灰，成功后清空并写"已送到主会话 HH:MM"，失败保留原文并写原因；Ctrl + Enter 也能发。

路由是 `#页` 或 `#页/块`（块 id 就是面板 id，如 `#library/library-notices`）；页名不认识取首页。

### 3.4 格式化与小组件（ui.js）

```
Dash.h(tag.cls, props?, ...kids)  建节点；props 有 cls、text、on:{事件}、dataset，其余当属性；kids 可为节点、文字、数组
Dash.esc(s)                       转义 HTML
Dash.num(n)                       千位加逗号，小数留一位（12,345、19.9）；没有数据写 —
Dash.at(iso)                      今天 "HH:MM"，不是今天 "M-D HH:MM"；解析不了写 —
Dash.day(iso)                     "M 月 D 日"
Dash.minutes(m)                   "45 分钟""2 小时 5 分钟"；缺事实写 —
Dash.parseTime(iso)               解析为毫秒数
Dash.status(text, tone)           一句状态；tone：ok warn risk claude codex author auto muted
Dash.open(path, text?, cls?)      打开仓库里的产物（新标签页），只写动作不露路径；path 相对仓库根
Dash.empty(title, sub?, compact?) 空状态
Dash.fold(summary, content, open?)  折叠，默认合上
Dash.item(id, title, side?)       编号对象一行：编号 + 一句话 + 右侧签条
Dash.flowbar(counts, link?)       任务流水线比例条：排着、在做、收尾、暂停四段，长度按个数；link(段) 给跳转地址
Dash.stageKeys                    四段的键，按先后：queued doing finishing paused
Dash.sideKey(who)                 指挥或负责方 → claude / codex / author / muted
Dash.modelName(m)                 claude-opus-5-5 → Opus 5.5；别的照原样
Dash.effortName(e)                推理档的中文名：low 低、medium 中、high 高、xhigh 很高、max 最高
```

### 3.5 颜色语义（palette.js）

颜色只按语义定一套：值写在 `style.css` 的 CSS 变量里（浅色与深色各一份），`Dash.palette` 登记"（组, 键）→ 变量 + 中文名"，签条、色块和所有图都从这里取；页面与图不写颜色字面量，图里的项只传 `tone: [组, 键]`。

| 组 | 键 |
| --- | --- |
| `stage` | `queued` 排着、`doing` 在做、`finishing` 收尾、`paused` 暂停 |
| `side` | `claude` `codex` `author` `muted` |
| `model` | `opus` `sonnet` `haiku` `gpt-6.1-sol` `gpt-6-luna` `gpt-6-astra` `other`（Claude 一族紫、Codex 一族青，按深浅分） |
| `level` | `ok` 正常、`warn` 要看、`bad` 有问题 |
| `kind` | `audit` 观感、`shots` 截图、`poster` 海报、`review` 评审（等你过目的种类） |
| `seq` | 数组 6 项，一组蓝，键是下标（脚本分组、文档域按顺序取） |

```
Dash.colorVar(组, 键)     CSS 变量名；Dash.color(组, 键) 当前主题下的色值（给 ECharts）；Dash.cssVar(name) 读任一 CSS 变量
Dash.label(组, 键)        中文名
Dash.mark(组, 键, 字?)    语义签条（节点），字省略用中文名
Dash.modelKey(型号)       型号 → 色表键：claude-opus-5-5 → opus；gpt-6.1-sol 原样；认不出是 other
```

### 3.6 统计小图（charts.js）

只画统计，不画原始数据与牵连关系。各返回一个节点，直接 append；节点进到可见页面且有了宽度才初始化，容器变宽变窄自动重排，面板重绘时 core 释放旧图，主题切换自动重画。`opts` 通用项：`height`（像素）、`label`（无障碍名）。

| `Dash.charts.` | 数据 | 用途 |
| --- | --- | --- |
| `spark(values, opts)` | 数组，最右是今天 | 近 N 天的小柱，今天用主色；统计卡上的趋势；`opts.unit` |
| `ring(parts, opts)` | `[{ label, value, tone }]` | 环形占比；`opts.center` 环心的字、`opts.unit` |
| `bars(items, opts)` | `[{ label, value, text?, tone }]` | 横向条形，条尾写 `text`（省略写数值） |
| `treemap(items, opts)` | `[{ label, value, note?, tone }]` | 面积图，悬停写 `note`；`opts.unit` |

## 4 页面与面板

导航 5 页，每页顶部一句"这页回答什么"（`manifest.js` 的 `ask`）；每块一个标题加一句"这块回答什么"（面板的 `note`）。

| 页 | 回答什么 | 面板（id，用到的数据） |
| --- | --- | --- |
| `#home` 首页 | 要不要我动手、项目推进得怎样 | `home-attention` 等你过目（没点"知道了"的前 3 条，其余折叠；种类签、一句标题、一句"已按 X 在做"、时刻、"打开""知道了"，`attention`，7 栏）；`home-feedback` 给主会话留言（输入框、发送、送达结果，下面最近 3 条与主会话读没读，`feedback`，5 栏）；`home-progress` 进展一览（流水线比例条、今天与本周完成数、在做的前 5 行，`progress`，7 栏）；`home-milestones` 最近里程碑（最近 3 条通知的竖时间线，`notices`，5 栏）；`home-stats` 关键统计（约 6 张卡，一个数一句说明，带趋势小柱或环形占比，`stats`）。1440×900 一屏放得下这 5 块 |
| `#progress` 进展 | 还没做完的活各在哪一段 | `progress-board` 任务流水线（比例条加今天、本周完成数；四列卡片：排着 → 在做 → 收尾 → 暂停，列头写行数，卡片是编号 + 标题、谁、一句状态，`progress`） |
| `#team` 团队 | 两边各在忙什么、用了哪些模型 | `team-leads` 两位指挥（头像卡带在跑灯：模型与推理档、在做行数与单行编号标题、在跑子代理数，`team`、`live`）；`team-usage` 今天各模型做了多少（条长是累计时长，条尾写个数与时长，`team`，7 栏）；`team-rules` 模型分工（模型 → 做什么，`team`，5 栏） |
| `#system` 体系 | 这套开发机器由什么组成、是否正常 | `system-health` 健康（正常 / 要看 / 有问题的环形占比、一句结论、要看的问题，`system`）；`system-hooks` 钩子（按负责方分组色块，每个一盏灯、一句作用，`system`，5 栏）；`system-scripts` 脚本（按个数分段的色带、分组色块：组名、个数、一句作用，`system`，7 栏）；`system-hookfeed` 最近注入了什么（主会话、Claude 子代理、Codex 主线、Codex 子代理四道，每道最近 10 次：时刻、事件、注入了哪几块、字数，不展示正文，`hookfeed`）；`system-docs` 文档（按域的面积图与各域一句话，`system`） |
| `#library` 资料 | 查文档、决策和通知 | `library-docs` 文档目录（一域一行，点开看编码、简题、一句话摘要，`docs`）；`library-decisions` 最近决策（最近 15 条的竖时间线，D 编号加一句结论，`records`，7 栏）；`library-notices` 通知（竖时间线，`notices`，5 栏） |

**加页面、面板、数据源**：
- 加页面：在 `manifest.js` 的 `pages` 加一项（`id`、`title`、`ask`、`file`），建 `pages/<页>.js` 与同名 `.css`，用 `Dash.panel` 注册。
- 加面板：在所属页文件里再写一个 `Dash.panel`，写清 `title` 与 `note`。
- 加数据源：先按 §2 在契约里定形状并让采集写出、版本索引列出，再在 `manifest.js` 的 `data` 登记。
- 面板只用 `Dash.*` 与 `style.css` 的通用类，自己的样式写进同名 `.css`，只用令牌，不写颜色字面量。

## 5 视觉

- **颜色有固定语义**（§3.5），与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制，红＝有问题，橙＝要看，绿＝正常；流水线四段各一色；结构、文字、分隔一律用中性蓝灰。浅蓝主色只给当前页、今天与主按钮，不为装饰换色。
- **一眼读懂**：字号偏大（正文 15 px、块标题 17 px、关键数 26–36 px），留白够；每块先给一个数或一句结论，再给小图；红色只给有问题的事；时刻写具体时间。
- 浅色为主，跟随系统深色（`prefers-color-scheme`，`style.css` 令牌各一份）；字体用系统中文无衬线，数字等宽；统计图统一用 ECharts，流水线比例条、色带、时间线这类简单图形用 HTML 与 CSS。窄屏（390 px）单列、不横向溢出。
- **本地预览**：`python -I -X utf8 dashboard/tools/serve.py`，打开 `http://127.0.0.1:8765/`，读取采集的真实数据；已在运行时直接打开该地址。

## 6 采集与维护

- `collect.py`：按 §1 的唯一原生通知与到期调度运行（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），读权威来源：`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/registry/REG-DECISIONS-*`、固定机位总览，以及经监控库读的本机协作数据与巡逻摘要（见第 1 节）。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。改了 `live.py` 或监控库以后要重启它。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
