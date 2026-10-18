# 作者观察台

作者了解项目的唯一入口：一个本地网页，像后台控制面板一样给作者看高层信息——项目结构、各种状态、文档地图、正在运行的模型与脚本、需要作者审的东西、给作者的通知。双击 `打开观察台.cmd`（或直接用浏览器打开 `index.html`）即可，不需要服务器。

**只放高层信息**。作者注意力有限：每块先给一句状态，数量只报数量级，细节只给指向（点开文档或文件），不复述底层内容。谁更新、更新什么见 WF-COMMS（作者观察台一节），本文件只讲架构与扩展。

## 1 目录

```
dashboard/
  index.html            外壳：页眉、导航、页面容器；按 app/manifest.js 加载面板与数据
  打开观察台.cmd         启动实时采集（没在跑时）、跑一次项目采集、用默认浏览器打开 index.html
  app/
    manifest.js         页面、面板、数据源与刷新间隔的唯一清单；加页面或数据源只改这里
    core.js             Dash 运行时：数据注册与定时重载、页面路由、面板注册与渲染
    ui.js               通用格式化与小组件（数量级、时间、h()、卡片、行、已看标记、待看汇总）；在 core.js 之后加载
    style.css           颜色与字体令牌、通用组件样式
    panels/<id>.js      一个面板一个文件（可带同名 .css）；只经 Dash 读数据
  data/                 本地状态，不进 git（.gitignore）
    auto/<名字>.js      采集脚本生成，任何人不手改
    curated/<名字>.js   只经 tools/post.py 改
  tools/
    datafile.py         数据文件读写（唯一入口）
    collect.py          项目采集：读权威来源，写 data/auto 下的项目数据；巡逻每轮跑一次
    live.py             实时采集：模型、子代理、后台脚本、构建锁；--loop 每 15 s 写一次 live.js
    post.py             给模型用的发布工具：发通知、挂审计项、结项、删过期
```

## 2 数据文件

每个数据文件只有一行：`Dash.put("<名字>", <JSON>);`。页面用 `<script>` 加载，所以 `file://` 下也能读；定时重载时换一个带时间戳的 `<script>`。读写一律经 `tools/datafile.py`（写入先写临时文件再改名）。每份数据都带 `updated`（ISO 时刻）和 `source`（哪个脚本或哪个会话写的），页面据此显示"多久前更新"，过期变灰。

| 名字 | 种类 | 写的人 | 刷新 | 内容 |
| --- | --- | --- | --- | --- |
| `live` | auto | live.py | 15 s | 正在运行的会话、子代理（型号与推理强度）、后台脚本、构建锁与引擎进程 |
| `queue` | auto | collect.py | 10 min | 施工队列各状态的行、活跃工作包、当前阶段与优先级 |
| `structure` | auto | collect.py | 10 min | 项目分层与主要目录（文件数、代码行，页面只显示数量级） |
| `docs` | auto | collect.py | 10 min | 文档按域分组的清单（编码、标题、路径、一句话说明、体量）与互相引用关系 |
| `decisions` | auto | collect.py | 10 min | 最近的决策（编号、一句话、状态）与总数 |
| `health` | auto | collect.py | 10 min | 测试（规模、超时数、失败数）、最近门禁、提交与推送、清理、文档超限 |
| `audit-auto` | auto | collect.py | 10 min | 自动发现的待审项：新截图、新海报与报告页、待审调研、【需定】来信 |
| `notices` | curated | post.py | 30 s | 给作者的通知 |
| `audit` | curated | post.py | 30 s | 模型挂给作者审的事 |

### 2.1 各数据的形状（字段可以加，不能改名或改义）

```
live:      { sessions: [{ id, name, side: "claude"|"codex", model, effort, state: "running"|"idle", since, task }],
             agents:   [{ id, parent, name, model, effort, state: "running"|"done", started, task, row }],
             scripts:  [{ id, name, kind: "watch"|"cron"|"gate"|"engine"|"build"|"other", state: "running"|"stopped", since, note }],
             build:    { lock: "free"|"held", holder, engines, builds } }
queue:     { stage, priority, counts: { "进行中": n, "待编译": n, "等前置": n, "可派": n, "暂停": n },
             rows: [{ id, title, status, who, dirs, pre }], packages: [{ id, goal, status, block }] }
structure: { layers: [{ name, note, dirs: [{ path, files, loc, note }] }] }
docs:      { domains: [{ key, name, docs: [{ code, title, path, summary, chars, sections }] }],
             edges: [{ from, to, n }] }            # from/to 是文档编码，n 是引用次数；path 相对仓库根
decisions: { total, recent: [{ id, text, status, scope }] }
health:    { tests: { count, loc, ratio, slow, failed, lastRun }, gate: { at, scope, result },
             git: { head, unpushed, recent: [{ hash, subject, at }] }, cleanup: { stale }, docs: { over } }
audit-auto:{ items: [{ id, kind: "shots"|"poster"|"report"|"research"|"decision", title, path, since }] }
notices:   { items: [{ id, at, from, level: "info"|"attention"|"decision", text, link, expires, pinned }] }
audit:     { items: [{ id, at, from, title, why, link, status: "open"|"done" }] }
```

- 时刻一律 ISO 字符串；`path`、`link` 相对仓库根，页面换算成相对 `dashboard/` 的链接（如 `../docs/...`），点开就是那份文件。
- 文字字段都是一句话：`task`、`text`、`title` 不超过约 40 个汉字，`why` 不超过约 60 个汉字。
- 不放会话号、本机绝对路径之外的隐私信息；会话号只在 `id` 里给页面去重用，不显示。

## 3 页面运行时（app/core.js 与 ui.js 提供）

```
Dash.put(name, data)          数据文件调用；存下并重绘依赖它的面板
Dash.get(name)                取当前数据（没有返回 null）
Dash.panel({ id, page, title, order, needs: [名字...], render(el, data) })
                              面板注册；data 是 { 名字: 数据 }；needs 里任一数据变了就重绘
Dash.mag(n)                   数量级：0 → "无"，1–9 照写，10–19 "十几"，20–99 "几十"，100–199 "上百"，200–999 "几百"，
                              1000–1999 "上千"，2000–9999 "几千"，10000+ "上万"
Dash.ago(iso)                 "刚刚" / "几分钟前" / "约 N 小时前" / "昨天" / "几天前"
Dash.stale(data, seconds)     超过期限没更新返回 true（面板据此变灰并标"数据旧了"）
Dash.link(path)               仓库根相对路径 → 本页可点的链接
Dash.chip(text, tone)         状态小签：tone 取 "ok" "warn" "risk" "claude" "codex" "author" "auto" "muted"
```

- 页面路由用地址里的 `#页名`；页面清单在 `app/manifest.js`。
- 面板只用 `Dash.*` 与 `style.css` 的通用类，面板自己的样式写在同名 `.css`，只用令牌，不写颜色字面量。
- 已看标记（作者点"知道了"）只存在浏览器本地（localStorage），不回写数据文件；要真正结项由模型用 `post.py done`。
- **演示数据**：地址带 `?demo` 时，所有数据改从 `data/demo/<名字>.js` 读（同一格式），用来在采集脚本没跑时开发和预览面板；演示数据只放这里，不写进 `auto/`、`curated/`。
- **本地预览**：`file://` 直接打开即可；要用浏览器工具截图时，在仓库根起 `python -m http.server <端口>`，打开 `http://localhost:<端口>/dashboard/`。

## 4 页面与面板

| 页 | 面板（文件） | 看什么 |
| --- | --- | --- |
| `#overview` 总览 | `overview.js` | 一行项目状态；"需要你看"（审计项与【需定】，最多 5 条）；健康签条；模型与脚本缩略；最新通知 3 条 |
| `#models` 模型 | `models.js` | 会话 → 子代理的树，每个节点标型号与推理强度；后台脚本与构建锁 |
| `#work` 施工 | `queue.js` | 队列按状态分栏，行只显示号与一句话；活跃工作包 |
| `#structure` 结构 | `structure.js` | 项目分层与主要目录，规模只显示数量级 |
| `#docs` 文档 | `docs.js` | 按域分组的文档地图，点开文档；引用关系图 |
| `#audit` 审计 | `audit.js` | 需要作者审的事（自动发现 + 模型挂的），可点开对应文件 |
| `#notices` 通知 | `notices.js` | 通知时间线，过期自动收起 |
| `#decisions` 决策 | `decisions.js` | 最近的决策 |

加一个页面或面板：在 `app/panels/` 新建文件，用 `Dash.panel` 注册，再把它加进 `app/manifest.js`。加一个数据源：采集脚本里加一个写出函数，并在 manifest 里登记名字与刷新间隔。

## 5 视觉

- **颜色有固定语义**，与协作方法海报一致：玫红＝作者，紫＝Claude 侧，青绿＝Codex 侧，琥珀＝自动机制（钩子、巡逻、脚本、构建锁），红＝问题与风险、要作者动手，绿＝通过与正常；结构、文字、分隔一律用中性蓝灰的深浅。不为装饰换色。
- **低注意力**：每块第一行是一句状态；红色只给要作者动手的事；默认只露最重要的几条，其余折叠；数量只报数量级；"多久前"代替具体时刻。
- 浅色为主，跟随系统深色；字体用系统中文无衬线，数字等宽对齐。

## 6 采集与维护

- `collect.py`：由 `live.py --loop` 每 10 分钟跑一次（也可手动 `python -I -X utf8 dashboard/tools/collect.py`），只读权威来源：`.collab/status/施工状态.md`、`docs/`（各文档头部与 `docs/SECTIONS.md`）、`docs/core/CORE-DECISIONS-*`、`output/build/test-record.tsv`、门禁结果、`git`、`output/shots`、`output/posters`、`output/reports`、`.collab/research/INDEX.md`、收件箱。顺手删掉过期的通知。
- `live.py --loop`：主会话开局与重启后在后台起（来信监视同一套做法），巡逻发现没在跑会提醒；`打开观察台.cmd` 也会在它没跑时起一个。
- `post.py`：所有会话与子代理改了高层信息时用它发布或结项，规则见 WF-COMMS。
