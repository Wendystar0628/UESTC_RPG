# 作者观察台

作者了解项目的唯一入口：一个本地网页，五页回答五件事——要不要我动手、项目推进得怎样（首页）；没做完的活各在哪一段（进展）；两边各在忙什么、用了哪些模型（团队）；这套开发机器由什么组成、是否正常（体系）；查文档、决策和通知（资料）。双击 `打开观察台.cmd`，入口启动 `live.py --loop` 与 `serve.py`，用回环 HTTP 打开页面；"知道了"与留言要求页面由本地服务打开（见 §3.3）。作者过目不挡推进（D-274），工程、架构与排期由两个指挥判断。

**给人看，只放高层信息与统计**：不给原始数据、明细表、日志与牵连关系（关系图、依赖图都不要）；首页一屏放下最重要的几块，下一层每页回答一件事，点开最多再展开一句话；编号对象（队列行、工作包、决策）只写"编号 + 一句话"加作者要知道的状态；快检、截图评审、已推、预烘、收口审计这些内部环节合成"收尾"，完成的只给个数；能用图表达的统计做成小图（趋势小柱、环形占比、条形、面积），图只画统计不画原始数据。数写确切值，时刻写 HH:MM；页面上不出现英文字段名、路径、哈希与原始 JSON。业务判断都在采集侧，浏览器只画。谁更新、更新什么见 WF-COMMS（作者观察台一节），本文件只讲架构与扩展。

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

collect 删除明确退役的数据文件，版本索引与服务只公开本节的数据。

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
    demo/<名字>.js      演示数据（?demo），与 manifest 的数据源一一对应
```

### 3.1 数据与加载

`manifest.js` 的 `data` 登记契约 v2 的数据：`name`、`label`（页眉弹层与"还没有数据"提示里的中文名）、`path`（相对 `dashboard/`）、`every`（秒，定时拉取间隔）、`stale`（秒，超过算"数据旧了"，`null` 不判断），以及两个开关：

- `poll: true`：不看版本索引，每 `every` 秒拉一次，永远要（`live`、`index`）。
- `keep: true`：页眉与导航要用，不管在哪一页都保持最新（`attention`、`progress`）。
- 其余数据只在当前页有面板 `needs` 它时才拉。

**版本索引**：`index` 是 `{ versions: { 名字: 版本串 } }`，每 15 s 拉一次；某份数据在索引里的版本变了才重拉；版本只在这次请求的文件真的执行了 `Dash.put` 之后才记下，下载失败或文件执行出错不推进版本、隔 15 s 再试，期间这份数据按没读到处理（面板写"没读到"），旧缓存不冒充新数据。索引没到、没有 `versions`、旧了，或索引里没列这份数据时，这份数据按自己的 `every` 定时拉，右下角提示条写明。第一次读完索引之前不拉非 `poll` 数据。

**整页刷新**：`live` 带 `appStamp`（界面代码最后改动时刻），变了就 `location.reload()`，开着的页面不跑旧代码。

**`?demo`**：地址带 `?demo` 时所有数据改读 `data/demo/<名字>.js`（同一格式），不判旧，页眉写"演示数据"，"知道了"与留言直接失败并写明是演示数据；用来在采集没跑时开发和预览。

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
Dash.ack(id)                    "知道了"：POST /api/attention/ack {id} → {ok: true}；成功后重读 attention 与 index
Dash.feedback(text)             留言：POST /api/feedback {text} → {ok: true, at}；成功后重读 feedback 与 index
Dash.isStale(name)              按 manifest 的 stale 判断；没有数据、演示、stale 为 null 都是 false
Dash.stale(data, seconds)       按数据自带的 updated 判断
Dash.demo                       是否 ?demo
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
Dash.minutes(m)                   "45 分钟""2 小时 5 分钟"
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
| `#team` 团队 | 两边各在忙什么、用了哪些模型 | `team-leads` 两位指挥（头像卡带在跑灯：模型与推理档、正在做的行、在跑子代理数，`team`、`live`）；`team-usage` 今天各模型做了多少（条长是累计时长，条尾写个数与时长，`team`，7 栏）；`team-rules` 模型分工（模型 → 做什么，`team`，5 栏） |
| `#system` 体系 | 这套开发机器由什么组成、是否正常 | `system-health` 健康（正常 / 要看 / 有问题的环形占比、一句结论、要看的问题，`system`）；`system-hooks` 钩子（按负责方分组色块，每个一盏灯、一句作用，`system`，5 栏）；`system-scripts` 脚本（按个数分段的色带、分组色块：组名、个数、一句作用，`system`，7 栏）；`system-hookfeed` 最近注入了什么（主会话、Claude 子代理、Codex 主线、Codex 子代理四道，每道最近 10 次：时刻、事件、注入了哪几块、字数，不展示正文，`hookfeed`）；`system-docs` 文档（按域的面积图与各域一句话，`system`） |
| `#library` 资料 | 查文档、决策和通知 | `library-docs` 文档目录（一域一行，点开看编码、简题、一句话摘要，`docs`）；`library-decisions` 最近决策（最近 15 条的竖时间线，D 编号加一句结论，`records`，7 栏）；`library-notices` 通知（竖时间线，`notices`，5 栏） |

**加页面、面板、数据源**：
- 加页面：在 `manifest.js` 的 `pages` 加一项（`id`、`title`、`ask`、`file`），建 `pages/<页>.js` 与同名 `.css`，用 `Dash.panel` 注册。
- 加面板：在所属页文件里再写一个 `Dash.panel`，写清 `title` 与 `note`。
- 加数据源：先按 §2 在契约里定形状并让采集写出、版本索引列出，再在 `manifest.js` 的 `data` 登记，并在 `data/demo/` 放一份同格式演示数据。
- 面板只用 `Dash.*` 与 `style.css` 的通用类，自己的样式写进同名 `.css`，只用令牌，不写颜色字面量。

## 5 视觉

- **颜色有固定语义**（§3.5），与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制，红＝有问题，橙＝要看，绿＝正常；流水线四段各一色；结构、文字、分隔一律用中性蓝灰。浅蓝主色只给当前页、今天与主按钮，不为装饰换色。
- **一眼读懂**：字号偏大（正文 15 px、块标题 17 px、关键数 26–36 px），留白够；每块先给一个数或一句结论，再给小图；红色只给有问题的事；时刻写具体时间。
- 浅色为主，跟随系统深色（`prefers-color-scheme`，`style.css` 令牌各一份）；字体用系统中文无衬线，数字等宽；统计图统一用 ECharts，流水线比例条、色带、时间线这类简单图形用 HTML 与 CSS。窄屏（390 px）单列、不横向溢出。
- **本地预览**：`python -I -m http.server <端口> --bind 127.0.0.1 --directory dashboard`，打开 `http://127.0.0.1:<端口>/?demo`；写回要用 `tools/serve.py` 起的服务。

## 6 采集与维护

- `collect.py`：由 `live.py --loop` 每 10 分钟跑一次（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），读权威来源：`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/registry/REG-DECISIONS-*`、`output/shots`、`output/posters`、`output/reports`，以及经监控库读的本机协作数据与健康检查（见第 1 节）。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。改了 `live.py` 或监控库以后要重启它。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
