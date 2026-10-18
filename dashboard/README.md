# 作者观察台

作者了解项目的唯一入口：一个本地网页，像后台控制面板一样给作者看高层信息——项目结构、各种状态、文档地图、正在运行的模型与脚本、在做的工作、健康检查、需要作者审的东西、给作者的通知。双击 `打开观察台.cmd`（或直接用浏览器打开 `index.html`）即可，不需要服务器。

**只放高层信息**。作者注意力有限：每块先给一句状态和要紧的数字，细节放在点开或展开里；一条信息不长（约 120 字以内）就直接全文，真长的才给"一句话摘要 + 展开全文"，不靠省略号硬截；能直接读出的数写确切值，只有测出来的大量值才报数量级。谁更新、更新什么见 WF-COMMS（作者观察台一节），本文件只讲架构与扩展。

## 1 目录

```
dashboard/
  index.html            外壳：页眉、导航、页面容器；按 app/manifest.js 加载面板与数据
  打开观察台.cmd         启动实时采集（没在跑时）、跑一次项目采集、用默认浏览器打开 index.html
  app/
    manifest.js         页面、面板、数据源与刷新间隔的唯一清单；加页面或数据源只改这里
    core.js             Dash 运行时：数据注册与定时重载、页面路由、面板注册与渲染
    ui.js               通用格式化与小组件（数量级、时刻、h()、卡片、行、树、已看标记、待看汇总）；在 core.js 之后加载
    ui-live.js          实时状态的共用渲染：谁在干活 Dash.who（总览与模型页共用）、后台脚本 Dash.scripts、临时工作树 Dash.worktrees、钩子卡 Dash.hooks、长文字 Dash.longText、空状态说明 Dash.liveGap；在 ui.js 之后加载
    style.css           颜色与字体令牌、通用组件样式（含 ui-live.js 的组件）
    panels/<id>.js      一个面板一个文件（可带同名 .css）；只经 Dash 读数据
  data/                 本地状态，不进 git（.gitignore）
    auto/<名字>.js      采集脚本生成，任何人不手改
    curated/<名字>.js   只经 tools/post.py 改
    demo/<名字>.js      演示数据（?demo）
  tools/
    datafile.py         数据文件读写（唯一入口）
    collect.py          项目采集：读权威来源，写 data/auto 下的项目数据；live.py --loop 每 10 分钟跑一次
    live.py             实时采集：只组装监控库 view/ 的结果，写 live.js 与 catalog.js；--loop 每 15 s 写一次 live.js、每 60 s 写一次 catalog.js
    post.py             给模型用的发布工具：发通知、挂审计项、结项、删过期
```

**数据从哪来**：观察台的健康、在做、谁在干活、后台脚本、临时工作树、钩子、脚本目录这几项都由监控库 `.collab/tools/monitor/`（本机 `.collab` 工具，与巡逻共用，只读；分层与各文件职责见 `.collab/tools/README.md` 的 monitor/ 表）读出，观察台不另写一份读法或检查：`collect.py` 经 `monitor.check.run_all` 取健康检查、经 `monitor.read.collab` 读队列与来信与调研；`live.py` 经 `monitor.view` 取 `people`（谁在干活）、`work`（在做的工作）、`running`（后台脚本）、`trees`（临时工作树）、`hooks`（钩子卡）、`catalog`（脚本目录，读 `monitor.read.scriptfiles`），临时工作树里"最近一次门禁"的一句话取自 `monitor.check.build.gate_brief`（门禁结果只有那一份实现）。这是仓库代码引用 `.collab` 的唯一例外（本机协作数据不进仓库）。模型写的只有 `post.py` 的通知与审计项；其余都是程序从权威来源读出，模型把这些结构化位置写对即可：施工队列表与状态头部的"当前阶段""当前优先级"、子代理名开头的队列行号、任务单开头的行号、来信第一行的标记（WF-COMMS §6）。

## 2 数据文件

每个数据文件只有一行：`Dash.put("<名字>", <JSON>);`。页面用 `<script>` 加载，所以 `file://` 下也能读；定时重载时换一个带时间戳的 `<script>`。读写一律经 `tools/datafile.py`（写入先写临时文件再改名）。每份数据都带 `updated`（ISO 时刻）和 `source`（哪个脚本或哪个会话写的），页面据此显示"更新于 HH:MM"，过期变灰。**数据存全文**，不在采集里截断。

| 名字 | 种类 | 写的人 | 刷新 | 内容 |
| --- | --- | --- | --- | --- |
| `live` | auto | live.py | 15 s | 会话、子代理（型号与推理强度）、在做的工作、后台脚本、构建锁与引擎进程、临时工作树、钩子与生效记录 |
| `queue` | auto | collect.py | 10 min | 施工队列各状态的行、当前阶段与优先级 |
| `structure` | auto | collect.py | 10 min | 项目分层与主要目录，目录可嵌套子目录（文件数、代码行、测试代码行） |
| `docs` | auto | collect.py | 10 min | 文档按域分组的清单（编码、标题、路径、一句话说明、体量、所属主文档、节目录）与互相引用关系 |
| `decisions` | auto | collect.py | 10 min | 最近的决策（编号、一句话、状态）与总数 |
| `health` | auto | collect.py | 10 min | 健康检查记录（测试与门禁、代码与文档、工作区与推送、协作流程、后台与钩子、会话与子代理） |
| `audit-auto` | auto | collect.py | 10 min | 自动发现的待审项：新截图、新海报与报告页、待审调研、【需定】来信 |
| `catalog` | auto | live.py | 60 s | 所有脚本与作用：按组、按子目录成树，每个脚本的作用、用法、开头说明、语言、行数、修改时刻、进不进 git、现在是否在跑 |
| `notices` | curated | post.py | 30 s | 给作者的通知 |
| `audit` | curated | post.py | 30 s | 模型挂给作者审的事 |

### 2.1 各数据的形状（字段可以加，不能改名或改义）

```
live:      { sessions: [{ id, name, side: "claude"|"codex", model, effort, state: "running"|"idle", since, task }],
             agents:   [{ id, parent, name, side, model, effort, state: "running"|"done", started, last, task, row, stalled }],
             scripts:  [{ id, name, kind: "watch"|"cron"|"gate"|"engine"|"build"|"other", state: "running"|"stopped", since, where, cmd, note,
                          stage?, testing? }],
             build:    { lock: "free"|"held", holder, engines, builds, editors },
             work:     { items: [{ row, what, dirs, state, who, sheet, agents: [{ id, side, name, model, effort, state, started, last, task }],
                                   changes: { files, dirs: [{ path, files }] }, flags: [一句话] }],
                         loose:  [agent...],
                         mains:  [{ name, model, effort, state, since, task }] },
             worktrees: { root: 指定的工作树目录,
                          items: [{ path, name, short, branch, detached, ahead, behind, dirty, touched, idleMin, locked, prunable, inRoot,
                                    running: [{ kind, n, since }], gate, gateOk, state: "using"|"idle"|"invalid", flags: [一句话] }] },
             hooks:    [{ id, side: "claude"|"codex", name, desc, script, events, tracked,
                          last, h1, h24, lastInj, recent, flag, note }],
             errors:   [采集这一块失败的原文："块名：异常类型 原文"] }
queue:     { stage, priority, counts: { "进行中": n, "待编译": n, "等前置": n, "可派": n, "暂停": n },
             rows: [{ id, title, status, who, dirs, pre }] }
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

- `live.sessions`、`live.agents`：Claude 与 Codex 两侧；`agents[].row` 是子代理名开头对上的队列行号（Claude 看 `description`，Codex 看 `task_name`；没有行号的是调研、核查一类，`row` 为空）；`stalled` 是"记录没收尾却 20 分钟没写"。`since`：会话当前状态从何时起（Codex 取会话记录里最近一次开始或结束，Claude 取记录最后写入）。`task`：Codex 主线最近一句话（全文）。
- `live.scripts`：逐个进程列出，同一脚本的外壳与子进程合并成一项；`where` 是"主检出"或临时工作树的路径（命令行路径对上 git 登记的工作树）；`cmd` 是命令摘要（`gate.ps1 -Wp WP-00`、`fixed_shots.ps1 -Only …`、`dotnet test Campus.Gen.Tests --filter …`、Godot 的 `--shots=P02 clear/autumn/17.5` 等）；门禁另有 `stage`（走到哪一门：G1 编译、G1 资源导入、G2 测试 <项目>、G4 架构规则……，读该检出 `output/build/gates` 下的日志）与 `testing`（正在跑的测试项目）。没在跑也要露面的来信监视、实时采集和巡逻（定时任务，进程里看不到，看 `state/patrol.last`）带 `state: "stopped"`。
- `live.work`：`items` 是施工队列里进行中、待编译的行（再加有子代理在跑的行），`agents` 是按行号对上的最近 30 分钟有动静的子代理，`changes` 是工作区里落在这行"目录"列下的未提交文件数与目录，`sheet` 是任务单路径或 null；`flags` 是程序算出的不一致（子代理在跑但行不是进行中；进行中却 3 小时没改动也没子代理；待编译但工作区里已没有改动）。`loose` 是在跑或刚交回、却对不上队列行的子代理；`mains` 是两个主会话现在在干什么。
- `live.worktrees`：git 登记着的每个工作树（主检出除外）。`ahead` / `behind` 是领先、落后本地 main 的提交数，`dirty` 是未提交的文件数，`touched` 是最后动过的时刻（改动文件、检出目录里最新的），`inRoot` 是否在 `root`（`monitor/local.json` 的 `worktreeRoot`）下，`running` 是命令行里带这个路径的进程按类（门禁、截图、Godot、dotnet 测试、dotnet 编译、其他）分组，`gate` 是最近一次门禁的一句话（没有报告是 null）。`state`：`using`（里面有进程在跑）、`idle`、`invalid`（目录没了，登记失效）；`flags` 是要看的理由与处理办法（不在指定目录、失效、没进程且超过 90 分钟没动）。
- `live.hooks`：登记的每个钩子（读 `~/.claude/settings.json` 与 `~/.codex/hooks.json`）。有我们自己记账的（`tracked: true`）带 `last`（最后触发）、`h1`/`h24`（近 1 小时、24 小时的 `n` 触发、`inj` 成功注入、`err` 报错、`dropped` 被挤掉、`miss` 取不到）、`lastInj`（最近一次注入：时刻、哪几类、字数）、`recent`（最近 20 条：时刻、事件、工具、注入类、字数、报错、挤掉与取不到个数、降级 `cut`）、`flag`（要看的理由：登记了但一次都没触发、触发了但没有一次成功注入、只有报错）；第三方钩子 `tracked: false`，只有登记信息，写明无记账。
- `live.errors`：采集某一块失败时的原文（哪个文件读不到会写在里面）；页面据此区分三种空状态：数据里没有这个字段（采集脚本是旧版本）、读源失败（显示 errors 原文）、有字段但确实为空。
- `health.checks`：每项 `level` 为 `ok`、`warn`（要看）、`bad`（要作者动手）；`summary` 是一句话带确切数字，`details` 带全文，`tip` 是建议原文（多条用换行连接）。检查的实现与分组见 `.collab/tools/README.md` 的 monitor/ 表，观察台与巡逻共用。
- 时刻一律 ISO 字符串；`path`、`link` 相对仓库根，页面换算成相对 `dashboard/` 的链接（如 `../docs/...`），点开就是那份文件。
- 字段存全文。通知与审计项是模型写的：标题与通知一两句、120 字内，`why` 200 字内（`post.py` 会拒绝超长）。
- 不放会话号、本机绝对路径之外的隐私信息；会话号只在 `id` 里给页面去重用，不显示。

## 3 页面运行时（app/core.js、ui.js、ui-live.js 提供）

```
Dash.put(name, data)          数据文件调用；存下并重绘依赖它的面板
Dash.get(name)                取当前数据（没有返回 null）
Dash.panel({ id, page, title, order, needs: [名字...], render(el, data) })
                              面板注册；data 是 { 名字: 数据 }；needs 里任一数据变了就重绘
Dash.mag(n)                   只给测出来的大量值（代码行、文件体积、测试代码行、引用次数）用；数量级：0 → "无"，1–9 照写，10–19 "十几"，20–99 "几十"，100–199 "上百"，200–999 "几百"，
                              1000–1999 "上千"，2000–9999 "几千"，10000+ "上万"
Dash.at(iso)                  确切时刻：今天 "HH:MM"，不是今天 "M-D HH:MM"；解析不了写 "—"
Dash.atAgo(iso)               确切时刻后面带一个灰色的"多久前"（节点）
Dash.ago(iso)                 "多久前"：只作 Dash.at 后面的灰色补充
Dash.elapsed(iso)             到现在的整分钟数，写成 "12 分钟""1 小时 5 分钟"（确切值）
Dash.stale(data, seconds)     超过期限没更新返回 true（面板据此变灰并标"数据旧了"）
Dash.link(path)               仓库根相对路径 → 本页可点的链接
Dash.chip(text, tone)         状态小签：tone 取 "ok" "warn" "risk" "claude" "codex" "author" "auto" "muted"
Dash.tree({ key, nodes, label(n, depth), kids?(n), id?(n), openDepth? })
                              可展开、收起的树（结构页、文档页共用），键盘与展开状态见 ui.js 注释
Dash.who(live)                谁在干活：每个会话一块，名下在跑的子代理逐个列出（行号或名字、型号、推理档、开始多久），已交回的折叠（总览与模型页共用）
Dash.scripts(live)            构建锁 + 逐个进程列出的后台脚本 + 巡逻（总览与模型页共用）
Dash.worktrees(live)          临时工作树：每个登记的工作树一行（名字、路径、HEAD 与离 main 的提交数、未提交文件数、在跑什么或闲置多久、最近一次门禁、状态签条：在用 / 闲置 / 不在指定目录 / 失效）（施工页与总览共用）
Dash.worktreeFold(live)       总览用：一行"临时工作树 N 个"，点开是 Dash.worktrees
Dash.hooks(live)              钩子卡：每个登记的钩子一行，点开看最近 20 条
Dash.gist(text)               { text 摘要（不长就是全文）, long 是否真长 }，longText 与脚本页共用
Dash.longText(text)           不长直接全文，真长的先给一句话摘要、点开看全文
Dash.liveGap(live, key, 块名们, 空时的话)
                              实时数据某一项没有内容时说清真实原因（没有数据 / 没有这个字段 / 读源失败原文 / 确实为空）；有内容返回 null
```

**数字口径**：能直接读出的数写确切值——时刻（`Dash.at`）、份数与个数（文档、队列行、子代理、待审、测试个数、提交号等）；只有测出来的大量值才报数量级（`Dash.mag`）：代码行、文件体积、测试代码行、引用次数。

- 页面路由用地址里的 `#页名`，`#页名/块` 滚到那一页的某一块（块 id 就是面板 id，如 `#work/work-doing`）；页面清单在 `app/manifest.js`。
- 面板只用 `Dash.*` 与 `style.css` 的通用类，面板自己的样式写在同名 `.css`，只用令牌，不写颜色字面量。
- 已看标记（作者点"知道了"）只存在浏览器本地（localStorage），不回写数据文件；要真正结项由模型用 `post.py done`。
- **演示数据**：地址带 `?demo` 时，所有数据改从 `data/demo/<名字>.js` 读（同一格式），用来在采集脚本没跑时开发和预览面板；演示数据只放这里，不写进 `auto/`、`curated/`。
- **本地预览**：`file://` 直接打开即可；要用浏览器工具截图时，在仓库根起 `python -m http.server <端口>`，打开 `http://localhost:<端口>/dashboard/`。

## 4 页面与面板

| 页 | 面板（文件） | 看什么 |
| --- | --- | --- |
| `#overview` 总览 | `overview.js` | 第一行：一句项目状态、阶段与优先级、数据新旧；第二行三栏：需要你看（审计项与【需定】，最多 5 条）、在做（队列里进行中与待编译的行，最多 8 条，谁的签条）、谁在干活（每个主会话一块，名下在跑的子代理逐个列，已交回的折叠）；第三行两栏：后台脚本（构建锁、逐个进程、巡逻，末尾一行"临时工作树 N 个"可展开）、健康摘要（只列不正常的项，最多 5 条，另有 N 项正常）；钩子与生效记录；最新通知 3 条。宽屏（≥1280 px）按此排，窄屏同顺序叠成一列 |
| `#work` 施工 | `work.js` | 自上而下四块：① 两个主会话现在在干什么；② 正在做（`work-doing`）：进行中与待编译的行，一行一张详细卡（行、状态、谁、按行号对上的子代理的签条、改动的目录与文件数、任务单链接、程序算出的不一致），末尾是没挂队列行的子代理；③ 排着的（`work-queued`）：可派、等前置、暂停三栏的紧凑卡片，暂停默认收起；④ 临时工作树（`work-trees`）一张卡 |
| `#health` 健康 | `health.js` | 全部检查按分组列出，每项一行：状态点、标题、一句话；点开看细节、建议、链接；分组标题写几项正常、几项要看 |
| `#models` 模型 | `models.js` | 会话 → 子代理（与总览同一个 `Dash.who`）；后台脚本与构建锁（同一个 `Dash.scripts`） |
| `#structure` 结构 | `structure.js` | 层 → 目录 → 子目录的树，默认展开第一层；每行文件数（确切）、代码行（数量级）、一句说明 |
| `#docs` 文档 | `docs.js` | 域 → 主文档 → 子文档 → 节的树，默认只展开到域，点文档或节打开文件；树下是引用关系图 |
| `#scripts` 脚本 | `scripts.js` | 所有脚本与作用，全部由程序从脚本自己读出：顶部一行“共几个脚本、几个没写开头说明”；按组（仓库工程脚本、观察台入口、数据管线、本机协作工具、双击入口）各一张卡，组内按子目录成可展开的树；每个脚本一行：文件名、作用（长的给摘要）、签条（语言、进 git 或仅本机、在跑、缺说明）；点开看用法、开头说明全文、路径、行数、修改时刻。写法见 ENG-CODE 接入要点“写或改脚本时”，改了脚本页面自动跟着变 |
| `#audit` 审计 | `audit.js` | 需要作者审的事（自动发现 + 模型挂的），可点开对应文件 |
| `#notices` 通知 | `notices.js` | 通知时间线，过期自动收起 |
| `#decisions` 决策 | `decisions.js` | 最近的决策 |

加一个页面或面板：在 `app/panels/` 新建文件，用 `Dash.panel` 注册，再把它加进 `app/manifest.js`。加一个数据源：采集脚本里加一个写出函数，并在 manifest 里登记名字与刷新间隔。

## 5 视觉

- **颜色有固定语义**，与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制（钩子、巡逻、脚本、构建锁）与要看的小字，红＝问题与风险、要作者动手，绿＝通过与正常；结构、文字、分隔一律用中性蓝灰的深浅。不为装饰换色。
- **低注意力**：每块第一行是一句状态；红色只给要作者动手的事；默认只露最重要的几条，其余折叠；数字能读出就写确切值，测出来的大量值才报数量级；时刻写具体时间，"多久前"只作灰色补充。
- 浅色为主，跟随系统深色；字体用系统中文无衬线，数字等宽对齐。

## 6 采集与维护

- `collect.py`：由 `live.py --loop` 每 10 分钟跑一次（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），读权威来源：`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/core/CORE-DECISIONS-*`、`output/shots`、`output/posters`、`output/reports`，以及经监控库读的本机协作数据与健康检查（见第 1 节）。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。改了 `live.py` 或监控库以后要重启它。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
