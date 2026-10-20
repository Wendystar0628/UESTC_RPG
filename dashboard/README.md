# 作者观察台

作者了解项目的唯一入口：一个本地网页，像后台控制面板一样回答七件事——要不要作者动手、每件活走到哪一步、两个指挥说了什么、谁在用什么模型、钩子脚本文档检查怎么相互作用、哪里不健康、资料在哪。双击 `打开观察台.cmd`（或直接用浏览器打开 `index.html`）即可；"知道了"这类写回操作要求页面由本地服务打开（见 §3.3 `Dash.ack`）。

**只放高层信息**。作者注意力有限：每块先给一句状态和要紧的数字，细节放在点开或展开里；一条信息不长（约 120 字以内）就直接全文，真长的才给"一句话摘要 + 展开全文"，不靠省略号硬截；测得到的数一律写确切值，不报数量级。业务判断都在采集侧，浏览器只画。谁更新、更新什么见 WF-COMMS（作者观察台一节），本文件只讲架构与扩展。

## 1 目录

```
dashboard/
  index.html            外壳：页眉、导航、页面容器、右下角提示条；按序加载 vendor/echarts、manifest、core、ui、palette、charts、charts-flow、charts-layered、charts-stats、ui-live
  打开观察台.cmd         启动实时采集（没在跑时）、跑一次项目采集、用默认浏览器打开 index.html
  vendor/
    echarts.min.js      Apache ECharts 单文件；许可文本在同目录 echarts-LICENSE.txt、echarts-NOTICE.txt
  app/
    manifest.js         页面（含资料页子标签）与数据源的唯一清单；加页面、子标签、数据源只改这里
    core.js             Dash 运行时：数据按需加载与版本索引、路由、面板注册与渲染、页内选中状态、页眉与导航角标
    ui.js               格式化与小组件：确切个数与时刻、h()、签条、状态条、折叠、行、表格、树、状态灯、分段开关
    palette.js          色表：语义（组、键）→ CSS 变量与中文名；签条、图例
    charts.js           共用图组件的底座（基于 ECharts）：主题色、尺寸自适应、空数据、加载占位、释放；建 Dash.charts，并经 Dash.internal.chartKit 把共用函数交给下面三份
    charts-flow.js      swimlane 泳道矩阵、dag 依赖图、timeline 时间线
    charts-layered.js   layered 分层关系图
    charts-stats.js     heatmap 热力矩阵、treemap 矩形树图、bars 条形图
    ui-live.js          实时数据的共用渲染：时长、长文字、空状态原因、子代理签条、后台脚本、临时工作树
    style.css           令牌（颜色、字号、间距、字体，浅色与深色各一份）与通用组件样式
    pages/<页>.js|css   一页一个文件（可带同名 .css）；资料页每个子标签一个：library-<子标签>.js|css；只经 Dash 读数据
  data/                 本地状态，不进 git（.gitignore）
    auto/<名字>.js      采集脚本生成，任何人不手改
    curated/<名字>.js   只经 tools/post.py 改
    demo/<名字>.js      演示数据（?demo），与 manifest 的数据源一一对应
  tools/
    datafile.py         数据文件读写（唯一入口）
    collect.py          项目采集：读权威来源，写 data/auto 下的项目数据；live.py --loop 每 10 分钟跑一次
    live.py             实时采集：只组装监控库 view/ 的结果，写 live.js 与 catalog.js；--loop 每 15 s 写一次 live.js、每 60 s 写一次 catalog.js
    post.py             给模型用的发布工具：发通知、挂审计项、结项、删过期
```

**数据从哪来**：观察台的健康、在做、谁在干活、后台脚本、临时工作树、钩子、脚本目录这几项都由监控库 `.collab/tools/monitor/`（本机 `.collab` 工具，与巡逻共用，只读；分层与各文件职责见 `.collab/tools/README.md` 的 monitor/ 表）读出，观察台不另写一份读法或检查：`collect.py` 经 `monitor.check.run_all` 取健康检查、经 `monitor.read.collab` 读队列与来信与调研；`live.py` 经 `monitor.view` 取 `people`（谁在干活）、`work`（在做的工作）、`running`（后台脚本）、`trees`（临时工作树）、`hooks`（钩子卡）、`catalog`（脚本目录，读 `monitor.read.scriptfiles`），临时工作树里"最近一次门禁"的一句话取自 `monitor.check.build.gate_brief`（门禁结果只有那一份实现）。这是仓库代码引用 `.collab` 的唯一例外（本机协作数据不进仓库）。模型写的只有 `post.py` 的通知与审计项；其余都是程序从权威来源读出，模型把这些结构化位置写对即可：施工队列表与状态头部的"当前阶段""当前优先级"、子代理名开头的队列行号、任务单开头的行号、来信第一行的标记（WF-COMMS §6）。

## 2 数据文件

每个数据文件只有一行：`Dash.put("<名字>", <JSON>);`。页面用 `<script>` 加载，所以 `file://` 下也能读；定时重载时换一个带时间戳的 `<script>`。读写一律经 `tools/datafile.py`（写入先写临时文件再改名）。每份数据都带 `updated`（ISO 时刻）和 `source`（哪个脚本或哪个会话写的），页面据此显示"更新 HH:MM"，过期变灰。**数据存全文**，不在采集里截断。

| 名字 | 种类 | 写的人 | 刷新 | 内容 |
| --- | --- | --- | --- | --- |
| `live` | auto | live.py | 15 s | 会话、子代理（型号与推理强度）、在做的工作、后台脚本、构建锁与引擎进程、临时工作树、钩子与生效记录 |
| `queue` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 施工队列各状态的行、当前阶段与优先级 |
| `structure` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 项目分层与主要目录，目录可嵌套子目录（文件数、代码行、测试代码行） |
| `docs` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 文档清单，结构与 AI 读的一致：顶层 CORE-INDEX，其下按 CORE-INDEX §1 的域与主文档顺序，子文档按文件头 `parent` 挂在主文档下（编码、标题、路径、一句话说明、体量、节目录）；另有互相引用关系 |
| `decisions` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 最近的决策（编号、一句话、状态）与总数 |
| `health` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 健康检查记录（测试与门禁、代码与文档、工作区与推送、协作流程、后台与钩子、会话与子代理） |
| `audit-auto` | auto | collect.py | 输入一变就采，不变时 2 min 兜底；页面 15 s 重读 | 自动发现的待审项：新截图、新海报与报告页、待审调研、【需定】来信 |
| `catalog` | auto | live.py | 60 s | 所有脚本与作用：按组、按子目录成树，每个脚本的作用、用法、开头说明、语言、行数、修改时刻、进不进 git、现在是否在跑 |
| `notices` | curated | post.py | 30 s | 给作者的通知 |
| `audit` | curated | post.py | 30 s | 模型挂给作者审的事 |

### 2.1 各数据的形状（字段可以加，不能改名或改义）

```
live:      { sessions: [{ id, name, side: "claude"|"codex", model, effort, state: "running"|"idle", since, task }],
             agents:   [{ id, parent, name, side, model, effort, state: "running"|"done", started, last, task, row, stalled }],
             scripts:  [{ id, name, kind: "watch"|"cron"|"gate"|"engine"|"build"|"other", state: "running"|"stopped", since, where, cmd, note,
                          stage?, testing? }],
             build:    { lock: "free"|"held", holder, engines, builds, editors: [{ pid, since, cmd }] },   # editors：不是命令行任务的 Godot 进程，命令行读不到时 cmd 是空串
             work:     { items: [{ row, what, dirs, state, who, sheet, agents: [{ id, side, name, model, effort, state, started, last, task }],
                                   changes: { files, dirs: [{ path, files }] }, flags: [一句话],
                                   pending?: { by, files: { count, list: [路径] },
                                               tree: null | { path, name, state, dirty, ahead, behind, touched,
                                                              running: [{ name, kind, cmd, stage, testing, since }], gate, gateOk,
                                                              shots: { count, at } },
                                               waiting: { since, estimated, basis } } }],
                         loose:  [agent...],
                         mains:  [{ name, model, effort, state, since, task }] },
             worktrees: { root: 指定的工作树目录,
                          items: [{ path, name, short, branch, detached, ahead, behind, dirty, touched, idleMin, locked, prunable, inRoot,
                                    running: [{ kind, n, since }], gate, gateOk, state: "using"|"idle"|"invalid", flags: [一句话] }] },
             hooks:    [{ id, side: "claude"|"codex", name, desc, script, events, tracked,
                          last, h1, h24, lastInj, recent, flag, note }],
             errors:   [采集这一块失败的原文："块名：异常类型 原文"] }
queue:     { stage, priority, counts: { "进行中": n, "待编译": n, "等前置": n, "可派": n, "暂停": n },
             rows: [{ id, title, status, who, dirs, pre, fix }] }   # fix：可派、等前置该怎么改（monitor/check/flow.queue_fixes，与巡逻同一份），没有是 null
structure: { layers: [{ name, note, dirs: [{ path, files, loc, testLoc, note, children }] }] }
             # children 是子目录数组，形状同 dirs 的元素，递归；没有就是叶子；files 是该目录（含子目录）的文件数
docs:      { domains: [{ key, name, docs: [{ code, title, path, summary, chars, parent, sections }] }],
             edges: [{ from, to, n }] }            # from/to 是文档编码，n 是引用次数；path 相对仓库根
             # parent 是子文档的主文档编码（主文档为 null）；
             # sections: [{ id, title, chars, children }]，id 如 "§8"、"§8.1"，children 是下一级节，递归
decisions: { total, recent: [{ id, text, status, scope }] }
catalog:   { total, missing, groups: [{ label, note, roots, count, missing, dirs: [目录...], scripts: [脚本...] }] }
             # 组按 monitor/local.json 的 scriptRoots（脚本放在哪只写那一处；同名标签并成一组）；目录 { name, path, count, dirs, scripts } 递归；
             # 脚本 { path, name, lang, lines, mtime, tracked, purpose, usage, intro, running }：purpose 是作用（Python 取模块文档字符串、其余取开头注释块的第一段，
             # 没写是空串，计入 missing），usage 是“用法：”开头那一行及其缩进续行，intro 是开头说明全文，running 是 null 或 { n 个进程, since }
health:    { checks: [{ id, group, title, level: "ok"|"warn"|"bad", summary, details: [{ text, link?, meta? }], tip, link? }] }
audit-auto:{ items: [{ id, kind: "shots"|"poster"|"report"|"research"|"decision", title, path, since }] }
notices:   { items: [{ id, at, from, level: "info"|"attention"|"decision", text, link, expires, pinned }] }
audit:     { items: [{ id, at, from, title, why, link, status: "open"|"done" }] }
```

- `live.sessions`、`live.agents`：Claude 与 Codex 两侧；`agents[].row` 是子代理名开头对上的队列行号（Claude 看 `description`，Codex 看 `task_name`；没有行号的是调研、核查一类，`row` 为空）；`stalled` 是"记录没收尾却 20 分钟没写"。`since`：会话当前状态从何时起（Codex 取会话记录里最近一次开始或结束；Claude 在忙时取这一轮开始的那条消息，空闲时取记录最后写入）。Claude 子代理的 `effort` 派单时传的优先，没传取记录里实际用的推理强度，都没有写"默认"。`task`：Codex 主线最近一句话（全文）。
- `live.scripts`：逐个进程列出，同一脚本的外壳与子进程合并成一项；`where` 是"主检出"或临时工作树的路径（命令行路径对上 git 登记的工作树）；`cmd` 是命令摘要（`gate.ps1 -Wp WP-00`、`fixed-shots.ps1 -Only …`、`dotnet test Campus.Gen.Tests --filter …`、Godot 的 `--shots=P02 clear/autumn/17.5` 等）；门禁另有 `stage`（走到哪一门：G1 编译、G1 资源导入、G2 测试 <项目>、G4 架构规则……，读该检出 `output/build/gates` 下的日志）与 `testing`（正在跑的测试项目）。没在跑也要露面的来信监视、实时采集和巡逻（定时任务，进程里看不到，看 `state/patrol.last`）带 `state: "stopped"`。
- `live.work`：`items` 是施工队列里进行中、待编译的行（再加有子代理在跑的行），`agents` 是按行号对上的最近 30 分钟有动静的子代理，`changes` 是工作区里落在这行"目录"列的路径或工作包下的未提交文件数与目录（文件归哪个包按 `ownership.json` 最具体的一条，与门禁同一判法），`sheet` 是任务单路径或 null；`flags` 是程序算出的不一致（子代理在跑但行不是进行中；进行中却 3 小时没改动也没子代理；待编译但工作区里已没有改动）。`loose` 是在跑或刚交回、却对不上队列行的子代理；`mains` 是两个主会话现在在干什么。
  待编译的行另带 `pending`（没有的写明没有）：`by` 谁编（谁一列写 Claude 的是"主会话统一编译"，写 Codex 的是"Codex 主线编译"）；`files` 是主检出里落在这行"目录"列的路径或工作包下还没提交的文件，数量加全部清单；`tree` 是按行号对上的同名临时工作树（开在 `worktreeRoot/<队列行号>`，WF-RULE §13，`C-176` 与 `c176` 同一行），没有就是 `null`，对上了给路径、状态、未提交数、`running`（在里面跑的脚本，来自 `live.scripts`：门禁的 `stage` 走到哪一门与 `testing`，Godot 的 `cmd` 里带 `--shots=机位`）、`gate`/`gateOk`（最近一次门禁的一句话，来自 `check/build.gate_brief`，没跑过是 `null`）、`shots`（这个检出 `output/shots` 的截图张数与这一轮的时刻）；`waiting` 是等了多久：队列没有记"改成待编译"的时刻，文件修改时间也不能代表交回时间，所以 `since` 为 `null`、`estimated` 为假，`basis` 写明拿不到的原因。临时工作树与后台脚本由 `live.py` 取好传给 `monitor.view.work`，视图不另读进程表。
- `live.worktrees`：git 登记着的每个工作树（主检出除外）。`ahead` / `behind` 是领先、落后本地 main 的提交数，`dirty` 是未提交的文件数，`touched` 是最后动过的时刻（改动文件、检出目录里最新的），`inRoot` 是否在 `root`（`monitor/local.json` 的 `worktreeRoot`）下，`running` 是命令行里带这个路径的进程按类（门禁、截图、Godot、dotnet 测试、dotnet 编译、其他）分组，`gate` 是最近一次门禁的一句话（没有报告是 null）。`state`：`using`（里面有进程在跑）、`idle`、`invalid`（目录没了，登记失效）；`flags` 是要看的理由与处理办法（不在指定目录、失效、没进程且超过 90 分钟没动）。
- `live.hooks`：登记的每个钩子（读 `~/.claude/settings.json` 与 `~/.codex/hooks.json`）。有我们自己记账的（`tracked: true`）带 `last`（最后触发）、`h1`/`h24`（近 1 小时、24 小时的 `n` 触发、`inj` 成功注入、`err` 报错、`dropped` 被挤掉、`miss` 取不到）、`lastInj`（最近一次注入：时刻、哪几类、字数）、`recent`（最近 20 条：时刻、事件、工具、注入类、字数、报错、挤掉与取不到个数、降级 `cut`）、`flag`（要看的理由：登记了但一次都没触发、触发了但没有一次成功注入、只有报错）；第三方钩子 `tracked: false`，只有登记信息，写明无记账。
- `live.errors`：采集某一块失败时的原文（哪个文件读不到会写在里面）；页面据此区分三种空状态：数据里没有这个字段（采集脚本是旧版本）、读源失败（显示 errors 原文）、有字段但确实为空。
- `health.checks`：每项 `level` 为 `ok`、`warn`（要看）、`bad`（要作者动手）；`summary` 是一句话带确切数字，`details` 带全文，`tip` 是建议原文（多条用换行连接）。检查的实现与分组见 `.collab/tools/README.md` 的 monitor/ 表，观察台与巡逻共用。
- 时刻一律 ISO 字符串；`path`、`link` 相对仓库根，页面换算成相对 `dashboard/` 的链接（如 `../docs/...`），点开就是那份文件。
- 字段存全文。通知与审计项是模型写的：标题与通知一两句、120 字内，`why` 200 字内（`post.py` 会拒绝超长）。
- 不放会话号、本机绝对路径之外的隐私信息；会话号只在 `id` 里给页面去重用，不显示。

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
