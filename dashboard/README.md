# 作者观察台

作者通过五页了解项目：首页、进展、团队、体系、资料。只给高层信息、统计和可回应的审美取向；编号对象显示“编号 + 一句话标题”，完成项只计数。作者过目不挡推进（D-274），工程、架构与排期由两个指挥判断。

双击 `打开观察台.cmd`，入口启动 `live.py --loop` 与 `serve.py`，用回环 HTTP 打开页面。纯文件预览无法执行留言或“知道了”。业务判断集中在采集侧，浏览器只画。

## 1 采集与写入

- `collect.py` 每次按真实来源生成 v2 汇总；队列表只读正式 `title`，不把“做什么”长描述当标题。文档域沿用 CORE-INDEX，记录沿用 REG-DECISIONS。
- `live.py` 每 15 秒写两个指挥是否运行及界面版本；输入变化即以新进程运行 collect，无变化每两分钟刷新。修改采集入口或 monitor 模块后重启循环与服务。
- `.collab/tools/monitor/view/` 的 attention、progress、team、system、hookfeed、stats 分别负责过目/留言、四段进展、团队汇总、体系摘要、注入摘要、统计；people 继续提供真实会话读法。底层读取与巡逻共用，不保留 v1 页面视图。
- `datafile.py` 是 `Dash.put` 文件的唯一读写入口；原子替换文件，版本索引只登记 v2。`post.py` 只维护通知及审美过目输入，写入有锁。
- `serve.py` 只绑定 `127.0.0.1:8765`，提供静态文件与两个同源写入 API。

`dashboard/data/auto/` 是程序结果，`curated/notices.js` 是通知，`curated/audit.js` 只是私有审美输入，`data/demo/` 由前端维护。源码目录 `app/`、`vendor/`、`index.html` 的版本集合决定 `live.appStamp`。

## 2 数据契约 v2

每份数据由 `Dash.put(名字, 数据)` 交付，附 `updated`、`source`；时刻是本地 ISO 字符串。来源失败写 `errors`，不会把旧值当作新结果；缺乏真实事实的字段用 `null`。只有 `notices` 在 curated，其余在 auto。

```text
index: {versions: {名字: 版本串}}
live: {leads: [{who: Claude|Codex, running: true|false|null}], appStamp}
attention: {items: [{id, kind: audit|shots|poster|review, title, detail, link, at, acked}]}
feedback: {items: [{at, text, delivered}]}
progress: {counts: {queued, doing, finishing, paused}, done: {today, week},
           rows: [{id, title, who: Claude|Codex|null, stage: queued|doing|finishing|paused, status}]}
team: {leads: [{who, model, effort, row, rowTitle, agents}],
       usageToday: [{model, count, minutes}], rules: [{model, use}]}
system: {hooks: [{name, side, purpose, ok: true|false|null}], scripts: [{group, count, purpose}],
         docs: [{domain, count, summary}], health: {level: ok|warn|bad, summary,
         counts: {ok: 数值|null, warn: 数值|null, bad: 数值|null}, problems: [一句话]}}
hookfeed: {lanes: [{role: 主会话|Claude 子代理|Codex 主线|Codex 子代理,
                   items: [{at, event: 中文事件, blocks: [中文块名], chars}]}]}
stats: {cards: [{key, label, value: 数值|短文字|null, unit, note,
        trend?: [连续七天日值], parts?: [{label, value, tone}], level?: ok|warn|bad}]}
records: {decisions: [{id, at, text: 一句结论约40字内}]}
docs: {domains: [{name, docs: [{code, title, summary}]}]}
notices: {items: [{id, at, from, level, text, link, expires, pinned}]}
```

`index` 每 15 秒重读并保持心跳；其他汇总随真实变化发版本。`live.leads` 是指挥运行灯的唯一字段；详细会话、进程、用量、依赖与日志都不发布。Codex 不读取或输出 token。`hookfeed` 的事件直接给中文，浏览器不另建对照表；不认识的事件写“未知事件”。决策只给一句短结论，不发布提交哈希、路径与原始长文。

### 2.1 统计边界

进展把可派/等前置合成“排着”，进行中是“在做”，待编译/已推待收口等合成“收尾”，暂停单列；只发布当前队列未完成行，不复活已删行。推送只认本地 origin/main reflog 中明确的 push 与完整前后节点，缺源不猜。

完成数取 origin/main 中带队列行号的历史，且该行当前已从队列删除；排除明确回滚，按行号去重，不另存完成账。日期只取本地 reflog 的真实推送范围，不用提交时间代替推送日期。尚未观测到可靠推送时间的历史不补计。今天按本地零点、本周按本地周一零点。

模型今日个数是今天启动的真实子代理，时长是其今天运行区间的累计分钟数；开始或终止事实不全用 null。多个同时在做的行没有明确当前行证据时，指挥卡的 row/rowTitle 为 null。第三方无记账的钩子 ok 为 null。

`system.health.counts` 与 level 来自整轮巡逻；巡逻把真实记录的 ok/warn/bad 统计写进现有 `patrol.txt`，观察台只读该行，不另跑检查或钩子自测。旧报告未提供分级时三项都为 null，不把“要看”猜成 warn 或 bad。problems 只收确切检查标题标识的数据停止、钩子故障、留言送达与构建运行异常等作者事项；维护清理、审计待派等仍只在巡逻。summary 区分全巡逻提醒数与影响查看或推进的问题数；巡逻超过 20 分钟则明示结论待刷新。连续门禁失败与主线卡住暂无可靠语义来源，不据单次失败或模型合规告警推断。

卡片 note 不超过 12 字；parts 可给环形占比，level 可给健康状态，健康占比使用整轮巡逻 counts。完成行数、推送次数与文档份数三卡的 trend 给最近七天，最早在前；任一天无法确认就省略整条 trend。完成与推送从 Git 真实来源逐日重算，文档只取 `.collab/tools/state/dashboard-stats-daily.json` 中逐日真实快照，缺少的历史不补零；具体来源与完成判定由 progress/stats 统一处理。

### 2.2 作者过目与留言

过目项只收明确 `scope=aesthetic` 且带当前判断的 post 输入，以及固定机位截图总览。detail 必须说明“已按什么在做”；指挥来信不进入作者事项。`POST /api/attention/ack` 收 `{id, revision?}`，按实际内容版本确认；内容变了重新过目。

`POST /api/feedback` 收 `{text}`，只写 Claude inbox 的 `<YYYYMMDD-HHMMSS>-author-留言-<随机后缀>.md`，第一行 `【作者】`，返回 `{ok: true, at}`。同秒留言各自独立，排他创建不会覆盖。回显最近十条，只从真实 inbox 与 inbox_wait/delivered 读取；归档时 delivered 才为 true。没有第二份全文权威。C249 的监视与主会话钩子负责即时送达。

模型发布示例：

通知的显示文字（text 与 from）拦提交哈希、文件名和路径；正文只写高层结论，合法的仓库产物仍通过 `--link` 关联。

```powershell
python -I -X utf8 dashboard/tools/post.py notice "一句高层通知" --from "Codex 主线"
python -I -X utf8 dashboard/tools/post.py audit "海报取向供你过目" --why "请看主视觉观感" --doing "主会话选定的画面方向" --kind poster --link output/poster.jpg --from "主会话"
```

v1 数据与页面明细停产；collect 删除明确退役文件，版本索引与服务均只公开 v2 数据。旧前端章节以下暂保留到 C248 同批交付，以本节 v2 数据契约为准。

## 3 页面运行时

### 3.1 数据与加载

`manifest.js` 的 `data` 登记 19 份数据：`name`、`label`（页眉弹层与"等数据"提示里的名字）、`path`（相对 `dashboard/`）、`every`（秒，定时拉取间隔）、`stale`（秒，超过算"数据旧了"，`null` 不判断），以及两个开关：

- `poll: true`：不看版本索引，每 `every` 秒拉一次，永远要（`live`、`index`）。
- `keep: true`：页眉与导航要用，不管在哪一页都保持最新（`attention`、`health`、`queue`）。
- 其余数据只在当前页有面板 `needs` 它、或面板的 `wants()` 此刻点名它时才拉。

**版本索引**：`index` 是 `{ versions: { 名字: 版本串 } }`，每 15 s 拉一次；某份数据在索引里的版本变了才重拉；版本只在这次请求的文件真的执行了 `Dash.put` 之后才记下，下载失败或文件执行出错不推进版本、隔 15 s 再试，期间这份数据按读取失败处理（`Dash.get` 返回 null，面板写"读取失败"），旧缓存不冒充新数据。索引没到、没有 `versions`、超过 120 s 没更新，或索引里没列这份数据时，这份数据退回按自己的 `every` 定时拉，右下角提示条写"数据索引未就绪：各份数据按定时拉取"（旧了写"数据索引旧了（HH:MM）：…"）。第一次读完索引之前不拉非 `poll` 数据。

**整页刷新**：`live` 带 `appStamp`（界面代码最后改动时刻），变了就 `location.reload()`，开着的页面不跑旧代码。数据文件格式与 `Dash.put` 见 §2。

**`?demo`**：地址带 `?demo` 时所有数据改读 `data/demo/<名字>.js`（同一格式），不判旧，页眉写"演示数据"，`Dash.ack` 直接失败；用来在采集没跑时开发和预览。演示数据只放 `data/demo/`，不进 `auto/`、`curated/`。

**页眉**：一句总述（进行中的行数、在跑的会话数、阶段）；右侧"实时 HH:MM"按钮，点开列每份数据的更新时刻、旧不旧、拉取方式（定时 / 按版本 / 定时（无索引））。导航角标：现在页是 `attention` 里没点"知道了"的条数（有 `urgency` ≥ 3 的转红），健康页是 `health` 里不是 `ok` 的检查数。

### 3.2 面板

```
Dash.panel({ id, page, tab?, title?, note?, span?, order?, needs?, uses?, wants?, partial?, watch?, render(el, data) })
```

| 字段 | 含义 |
| --- | --- |
| `id` | 面板 id，全局唯一；也是 `#页/块` 路由的块名 |
| `page`、`tab` | 挂在哪一页（manifest 的页 id）；资料页的面板必须写 `tab`，其余页不写 |
| `title`、`note` | 卡片标题与标题下一行小字；不写 `title` 就没有卡片头 |
| `span` | 占 12 栏里的几栏：12（默认）、7、5（`style.css` 只定义了这几种，1180 px 以下一律整行；要别的宽度先在那里加） |
| `order` | 页内排序，小的在上（默认 100） |
| `needs` | 数据名数组：进到本页就拉，全部到齐才画（`partial: true` 时到一份就画，没到的是 `null`）；任一份变了重绘 |
| `uses` | 不主动拉，只在这份数据变了时重绘（配合 `wants` 按需拉的数据） |
| `wants` | 函数，返回此刻要按需拉的数据名（取自 `uses`，如"选中了一行才拉它的历史"）；面板在当前页时每次同步都点名，切页回来仍按版本更新；`Dash.select` 之后立即重新点名 |
| `watch` | `Dash.select` 的键数组，选中变了就重绘 |
| `render(el, data)` | 往 `el` 里画；`data` 是 `{ 名字: 数据 }`；抛异常时这块显示"这块出错了"，不影响别的面板 |

只画当前页（和子标签）的面板，别的页记着脏，切过去再画。重绘时面板里 `<details>` 的开合状态在个数不变时保持。卡片右上角自动写"更新 HH:MM"（`needs` 里最旧的一份），数据旧了变灰并写"数据旧了 · HH:MM"。数据还没读到时画骨架，读过一次仍没有写"等数据"并列出缺哪份、期望路径；`needs`、`uses` 里有读取失败的数据时，面板顶部写"读取失败"（`needs` 没到齐则整块写）。

### 3.3 运行时接口

```
Dash.put(name, data)            数据文件调用（§2）
Dash.get(name)                  取当前数据，没有或最近一次读取失败返回 null
Dash.dataState(name)            "wait" 读取中 / "missing" 读过但没有这份文件 / "error" 读取失败 / "ok"
Dash.select(key, value)         页内选中状态；值相同再选一次取消；watch 里有这个键的面板重绘
Dash.selected(key)              当前选中值，没有是 null
Dash.redraw(id) / redrawAll()   重画面板（本地交互改了显示方式）；主题切换时 core 与图组件自己调 redrawAll
Dash.go(path)                   跳路由
Dash.ack(id, revision?)         "知道了"写回：POST /api/attention/ack，成功后重读 attention 与 index；
                                页面不是由本地服务打开（file://）、演示模式、服务没在跑时 Promise 失败，面板把原因写出来；不存浏览器
Dash.isStale(name)              按 manifest 的 stale 判断；没有数据、演示、stale 为 null 都是 false
Dash.stale(data, seconds)       按数据自带的 updated 判断
Dash.demo                       是否 ?demo
```

路由是地址里的 `#页` / `#页/子标签` / `#页/块` / `#页/子标签/块`（块 id 就是面板 id，如 `#work/work-flow`、`#library/docs/lib-docs-refs`）；资料页缺子标签取第一个，页名不认识取第一页。

### 3.4 格式化与小组件（ui.js、ui-live.js）

数字口径：能直接读出的数写确切值——份数与个数、代码行、字数、引用次数用 `Dash.num`，时刻用 `Dash.at`，不报数量级。

```
Dash.h(tag.cls, props?, ...kids)  建节点；props 有 cls、text、on:{事件}、dataset，其余当属性；kids 可为节点、文字、数组
Dash.esc(s)                       转义 HTML
Dash.num(n)                       千位加逗号（12,345）；没有数据写 —
Dash.at(iso)                      今天 "HH:MM"，不是今天 "M-D HH:MM"；解析不了写 —
Dash.elapsed(iso)                 到现在的整分钟数："不到 1 分钟""12 分钟""1 小时 5 分钟"
Dash.parseTime(iso)               解析为毫秒数
Dash.chip(text, tone)             状态小签；tone：ok warn risk claude codex author auto muted
Dash.status(text, tone)           面板第一行的一句状态；statusLine(tone) 返回 <p>，内容往 .body 追加
Dash.a(path, text?, cls?)         指向仓库文件的链接（新标签页）；path 相对仓库根，换算成相对 dashboard/ 的链接
Dash.empty(title, sub?, compact?) 空状态
Dash.fold(summary, content, open?)  折叠，默认合上
Dash.row({ lead, title, meta, act })  列表行：前导、标题、右侧小字、操作节点数组
Dash.table(cols, rows, opts)      cols [{ key, label, num?, width?, cell?(row) }]；opts: onRow、selected(row)、empty
Dash.lights(items)                一排状态灯：[{ label, tone: [组, 键], sub?, href? }]
Dash.segment(options, value, onPick)  页内分段开关：options [{ id, label }]
Dash.tree({ key, nodes, label(n, depth), kids?, id?, openDepth? })
                                  可展开收起的树；key 是展开状态的名字（只记在本页内存）；openDepth 默认 1；键盘见 ui.js 注释
Dash.sideTone(from, side?)        按来源挑色：返回 codex / claude / author / muted
Dash.expired(item)                item.expires 已过
Dash.modelName(m)                 claude-opus-5-5 → Opus 5.5；别的照原样
Dash.gist(text) → { text, long }  不长（约 120 字内）是全文，真长的取第一句
Dash.longText(text, cls?)         不长直接全文，真长的给摘要 + 展开全文
Dash.liveGap(live, key, labels, emptyText)
                                  live 某一项没有内容时说清原因（没数据 / 采集是旧版本 / 读源失败原文 / 确实为空）；有内容返回 null
Dash.agentLabel(a)                子代理签条文字
Dash.scripts(live)                构建锁 + 逐进程后台脚本 + 巡逻
Dash.worktrees(live)              临时工作树，每个一行
```

### 3.5 颜色语义（palette.js）

颜色只按语义定一套：值写在 `style.css` 的 CSS 变量里（浅色与深色各一份），`Dash.palette` 登记"（组, 键）→ 变量 + 中文名"，签条、图例和所有图都从这里取；页面与图不写颜色字面量，图里的项只传 `tone: [组, 键]`（如 `['queue', '进行中']`、`['tag', '需定']`）。

| 组 | 键 |
| --- | --- |
| `status` | `done` `doing` `fail` `todo` `na`（流水阶段与行状态） |
| `queue` | `进行中` `待编译` `可派` `等前置` `暂停` `已推`（施工队列状态列，借用上面的色） |
| `side` | `claude` `codex` `author` `auto` |
| `tag` | `需定` `待审` `知会` `插队` `无`（来信标签） |
| `model` | `opus` `sonnet` `haiku` `gpt-6.1-sol` `gpt-6-luna` `gpt-6-astra` `other`（Claude 一族紫、Codex 一族青，按深浅分） |
| `level` | `ok` `warn` `bad`（健康检查等级） |
| `node` | `event` `hook` `doc` `script` `data` `check` `gate` `path`（体系关系图节点种类） |
| `edge` | `inject` `route` `govern` `readwrite` `generate` `call` `check` `gate`（边种类） |
| `layer` | 数组 6 项，键是下标（按层的顺序取，不按名字） |
| `scale` | 数组 2 项：单色渐变两端 |

```
Dash.color(组, 键)        当前主题下的色值（给 ECharts）；Dash.colorVar(组, 键) 是 CSS 变量名；Dash.cssVar(name) 读任一 CSS 变量
Dash.label(组, 键)        中文名
Dash.mark(组, 键, 字?)    语义签条（节点），字省略用中文名
Dash.legend(组, 键们?)    一行图例，键省略列整组
Dash.modelKey(型号)       型号 → 色表键：claude-opus-5-5 → opus；gpt-6.1-sol 原样；认不出是 other
Dash.stages               流水阶段的键与中文名，按先后：dispatch 派工、build 施工、pending 待编译、gate 快检、pushed 已推、audit 收口审计、shots 截图评审、prebake 预烘
```

### 3.6 图组件（charts*.js）

7 个组件，各返回一个节点，直接 append；节点进到可见页面且有了宽度才初始化，容器变宽变窄自动重排，面板重绘时 core 释放旧图，主题切换自动重画。第二个参数 `opts` 通用项：`height`（像素，省略按数据算）、`onClick(项, 事件)`、`legend: [[组, 键们?], ...]`（图上方图例）、`empty`（空数据时的话）、`key`（同一张图的稳定名，只有第一次画有动画）、`label`（无障碍名）。

| `Dash.charts.` | 数据 | 用途与专有参数 |
| --- | --- | --- |
| `swimlane` | `{ columns:[{id,label}], lanes:[{id,label,sub?,side?,head?}], cells:[{lane,col,status,at?,note?}] }` | 泳道矩阵：一行一道（`head` 为真是分组标题行），一列一阶段，格子按状态着色写时刻；`opts.selected` 高亮一道，`opts.labelWidth` 标签宽 |
| `dag` | `{ nodes:[{id,label,tone,side?,title?,mark?,markNote?}], edges:[{from,to,strong?,critical?}], critical? }` | 有向依赖图：按前置分层从左到右，`tone` 着色，按 `side` 分形状（Claude 胶囊、Codex 方块、其他六边形）；`mark` 加红色虚线外圈（`opts.markText` 是图例名），`opts.selected` 加主色外圈，`critical` 边主色加粗；没有前后置的节点在图下排成签条 |
| `layered` | `{ columns:[{kind,label}], nodes:[{id,label,kind,group?,title?}], edges:[{from,to,kind,title?}] }` | 分层关系图：按列从左到右、列内按 `group` 分段；节点色取 `node` 组、边色取 `edge` 组；图上方按边种开关；点节点高亮上下游；`opts.fold`（可折叠的节点种类）、`foldOver`（节点多于它默认折起）、`through`（高亮时继续往下走的种类）、`state`（调用方保存的交互状态，重画后保留）；返回的节点带 `pick(id)` |
| `heatmap` | `{ x:[名], y:[名], cells:[{x,y,value?,tone?,title?}], mode:'count' 或 'tone' }` | 热力矩阵：`count` 按数值深浅（`scale` 组），`tone` 每格按语义色；`x`、`y` 是下标 |
| `treemap` | `{ nodes:[{name,value,tone?,children?,title?}] }` | 矩形树图：面积为 `value`，顶层按顺序取 `layer` 组色（或 `tone`）；`opts.unit` 数值单位；点格子下钻 |
| `timeline` | `{ lanes:[{id,label}], items:[{lane,at,end?,tone,label,title?}], from?, to? }` | 时间线：横轴时刻，一道一行；有 `end` 画成一段否则是点；竖线标现在 |
| `bars` | `{ categories:[名], series:[{name,tone,values:[数]}], stack?, horizontal? }` | 条形图（可堆叠） |

另有 `Dash.charts.loading(height)`（加载占位）与 `Dash.charts.release(root)`（释放 root 里的图，core 在重绘时调）。

## 4 页面与面板

导航 7 页，每页顶部一句"这页回答什么"（`manifest.js` 的 `ask`）；共 32 个面板。

| 页 | 回答什么 | 面板（id，用到的数据） |
| --- | --- | --- |
| `#now` 现在 | 要不要我动手、此刻在发生什么 | `now-pulse` 系统脉搏（巡逻、来信监视、采集、钩子、构建锁的灯，`live`）；`now-attention` 需要你（需定来信、作者级审计、待看截图、待审调研，一条一行按紧急排序，"知道了"经 `Dash.ack`，`attention`，7 栏）；`now-recent` 最近（最近 5 次推送、3 条通知，`history`、`notices`，5 栏）；`now-lanes` 两条指挥泳道（各自在做的行、所处阶段、挂的子代理，`live`、`pipeline`）；`now-flow` 本批流水（未收口行 × 阶段的小矩阵，`pipeline`） |
| `#work` 施工 | 每件活走到哪一步、谁挡着谁 | `work-flow` 流水线（行 × 阶段泳道，同一次推送收口的行归一批，`pipeline`）；`work-history` 行的历史（选中行的时间线，`pipeline`、`queue`，按需 `rowhistory`）；`work-deps` 依赖图（关键路径与"前置已完成却没解锁"，`queue`）；`work-board` 看板（可派、等前置、暂停，`queue`）；`work-trees` 临时工作树（`live`） |
| `#collab` 协作 | 两边在说什么、审了什么 | `collab-letters` 来信（两道时间线，按标签着色，点开看全文，未送达标出，`letters`）；`collab-audits` 收口审计（谁派、哪个模型、交回没有、查出几处补丁、转成哪些行，`audits`）；`collab-files` 任务单、契约申请、调研与交接材料（`letters`） |
| `#models` 模型 | 谁在用什么模型干什么 | `models-table` 会话与子代理（`models`）；`models-usage` 近 24 小时各型号的派工个数与时长（`models`，7 栏）；`models-rules` 分工对照（WF-MODEL 的分工与实际使用，偏离标出，`models`，5 栏）；`models-tree` 派工树（`models`） |
| `#system` 体系 | 钩子、脚本、文档、检查怎么相互作用 | `system-graph` 关系图（点节点高亮上下游，可按边种过滤，`relations`）；`system-hooks` 钩子明细（事件、时机、近 24 小时各文档注入次数、降级与挤掉，`live`、`relations`、`history`）；`system-scripts` 脚本明细（作用、用法、谁调用、读写什么、最近运行，`catalog`、`history`、`relations`，按需 `catalogdetail`） |
| `#health` 健康 | 系统哪里不对 | `health-matrix` 检查矩阵（6 组 × 各项方块，下方 24 小时历史条带，`health`、`history`）；`health-detail` 细节与建议（`health`、`history`）；`health-checks` 全部检查（每组要看的在前，点开看明细、建议、链接，`health`）；`health-procs` 后台进程与构建锁（`live`） |
| `#library` 资料 | 文档、结构、决策、通知和脚本都在哪 | 子标签 5 个：`docs` 文档（`lib-docs-tree` 文档树：顶层 → 域 → 主文档 → 子文档 → 节，点开打开文件，`docs`；`lib-docs-refs` 引用矩阵：域 × 域热力，点格展开到文档，`docs`、`docrefs`）；`structure` 结构（`lib-structure-map` 代码分布矩形树图、`lib-structure-tests` 测试占比、`lib-structure-tree` 目录树，`structure`）；`decisions` 决策（`lib-decisions` 按 D 编号一格一条，颜色是状态，`decisions`）；`notices` 通知（`lib-notices` 按发布方分道，颜色是等级，`notices`）；`scripts` 脚本目录（`lib-scripts` 按根目录分组，每个一句作用，`catalog`；详情在体系页） |

**加页面、子标签、面板、数据源**：
- 加页面或子标签：在 `manifest.js` 的 `pages` 加一项（`id`、`title`、`ask`、`file`、`css`；有子标签的页写 `tabs`，每个子标签自带 `title`、`file`、`css`），建对应的 `pages/*.js` 并用 `Dash.panel` 注册；`css: true` 时同名 `.css` 一并加载。
- 加面板：在所属页文件里再写一个 `Dash.panel`。
- 加数据源：采集脚本加一个写出函数，在 `manifest.js` 的 `data` 登记名字、路径、`every`、`stale`，让版本索引列出它的版本，并在 `data/demo/` 放一份同格式演示数据。
- 面板只用 `Dash.*` 与 `style.css` 的通用类，自己的样式写进同名 `.css`，只用令牌，不写颜色字面量。

## 5 视觉

- **颜色有固定语义**（§3.5），与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制（钩子、巡逻、脚本、构建锁）与要看的小字，红＝问题与风险、要作者动手，绿＝通过与正常；结构、文字、分隔一律用中性蓝灰的深浅。不为装饰换色。
- **低注意力**：每块第一行是一句状态；红色只给要作者动手的事；默认只露最重要的几条，其余折叠；数字一律写确切值；时刻写具体时间，"多久前"只作灰色补充。
- 浅色为主，跟随系统深色（`prefers-color-scheme`，`style.css` 令牌各一份）；字体用系统中文无衬线，数字等宽对齐；图统一用 ECharts，不手写 SVG。
- **本地预览**：`file://` 直接打开即可（写回操作除外）；要用浏览器工具截图时，在仓库根起 `python -m http.server <端口>`，打开 `http://localhost:<端口>/dashboard/`，演示数据加 `?demo`。

## 6 采集与维护

- `collect.py`：由 `live.py --loop` 每 10 分钟跑一次（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），读权威来源：`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/registry/REG-DECISIONS-*`、`output/shots`、`output/posters`、`output/reports`，以及经监控库读的本机协作数据与健康检查（见第 1 节）。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。改了 `live.py` 或监控库以后要重启它。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
