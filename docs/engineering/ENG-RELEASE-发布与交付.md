---
id: ENG-RELEASE
title: 发布与交付
category: engineering
parent: ENG-INDEX
summary: 版本号与构建信息、正式用户目录、导出与发布、安装更新卸载、地图数据包、故障诊断、许可、署名与图标
keywords: [发布, 交付, 版本号, version.txt, 构建信息, 导出, 导出入口, scripts/release, 引擎版本, 插件清单, UESTC-<版本>-win64.7z, 地图数据包, map-data-<版本>.zip, ODbL, 正式用户目录, '%APPDATA%\UESTC', 安装, 更新, 卸载, 诊断包, 导出诊断, 许可, MIT, third_party.json, 干净环境验证, OSM 署名, 许可页, 窗口标题, 窗口图标, 八角晶片]
depends: [ENG-ARCH, ENG-PERF, ENG-GATES]
related: [STD-CONVENTIONS, SYS-SAVE, SYS-TITLE, SYS-GRAPHICS, UI-NAV, UI-VISUAL, SYS-AUDIO, WLD-OUTSKIRTS]
contracts: []
data: ["LICENSES.txt"]
gates: [G6]
paths: ["scripts/requirements.*", "game/testing/**", "game/data/build-code-fingerprints.json", "game/gen/maps/qingshuihe/Campus.Gen.Maps.Qingshuihe.csproj", "game/gen/sites/**", ".collab/tools/**", "dashboard/**", "game/gen/Campus.Gen.csproj", "game/kernel/map/data/**", "game/kernel/renderer/geometry/**", "game/kernel/renderer/render-state/**", "game/kernel/instances/**", "game/gen/geometry/**", "scripts/gates/arch/Campus.Arch.Tests.csproj", "scripts/godot-api/ApiDump/ApiDump.csproj", "game/contracts/Runtime/Campus.Contracts.Runtime.csproj", "game/contracts/Campus.Contracts.csproj", "game/data/maps/*/interiors.json", "game/Campus.Gen.Tests.csproj", "game/Campus.Tests.csproj", "game/contracts/.gdignore", "game/export_presets.cfg", "game/contracts/*.csproj", "game/kernel/registry/**", "game/data/rulesets/**", "scripts/gates/arch/**", "**/Directory.*.targets", "game/Campus.csproj", "game/gen/.gdignore", "game/project.godot", "Directory.*.targets", "game/kernel/stubs/**", "scripts/godot-api/**", "**/Directory.*.props", "scripts/gate.ps1", "third_party.json", "Directory.*.props", "docs/godot-api/**", "game/contracts/**", ".gitattributes", "scripts/build/**", "scripts/setup/**", "game/gen/bakeinput/assembly/**", "scripts/docs/**", "budgets.json", "game/addons/**", "global.json", "version.txt", "scripts/lib/**", ".gitignore", ".ignore", "AGENTS.md", "CLAUDE.md", "README.md", "启动游戏.cmd", "**/*.csproj", "LICENSE", "tools/**", "**/*.sln", "game/.editorconfig", "scripts/git-hooks/**"]
---
# ENG-RELEASE · 发布与交付

> **范围**：本文规定对外交付：版本号、引擎与框架版本和构建信息从哪里来、正式版的用户目录、一次导出与发布怎么做、玩家怎样安装更新卸载、每个公开版本附带的地图数据包、试玩出问题时怎样交回诊断材料，以及各部分按什么许可公开、插件清单、画面右下角的署名、许可页、窗口标题与图标。
> **不在本文**：导出入口脚本的步骤（`scripts/release/`，主线之后建）；构建信息的生成（`scripts/build/build-info.ps1`）；安装包的组成与体积（ENG-PERF-STREAM §4，G6 见 ENG-GATES §1.2）；日志的写法（STD-CODE §0.1）；存档的格式与失败处理（SYS-SAVE）；许可页的界面（UI-NAV §3.5）；图标的画法（UI-VISUAL-COMPONENTS §3.3）。
> **查阅**：先读 §0；版本号、引擎与框架版本和构建信息看 §1；用户数据目录看 §2；导出与发布看 §3；安装、更新与卸载看 §4；地图数据包看 §5；故障与诊断看 §6；许可与插件清单看 §7；署名、许可页、窗口标题与图标看 §7.1

## 0 结论

1. **构建信息只有一个来源**：仓库根的 `version.txt` 加当前提交，生成 `<版本>+<提交>`；界面、存档、日志、诊断和压缩包名都读它。
2. **正式用户目录固定**：`%APPDATA%\UESTC\`，不带版本号，所有版本共用；开发态用 `UESTC_DEV`，不碰它。
3. **一个公开版本** = `main` 上的标签 `v<版本>` + 分发压缩包 `UESTC-<版本>-win64.7z` + 地图数据包 `map-data-<版本>.zip`，三者版本号相同。
4. **更新解压到新目录**，不覆盖旧目录；卸载程序和清理用户数据是两件事。
5. **试玩出问题交回诊断包**：默认只含日志和诊断摘要，存档由玩家另选。
6. **许可分四类**：代码与脚本 MIT；原创美术、音频、文案与设计文档保留所有权利；OSM 派生数据 ODbL 1.0；第三方素材按各自许可（§7）。
7. **署名**：画面右下角只有一行"© OpenStreetMap contributors"，`--ui=0` 时画面上只剩它（§7.1）。

### 0.1 接入要点

- **显示或记录版本**：只读构建信息，别处不另写版本号；`version.txt` 只由主会话在发版时改（§1）。
- **判断存档能不能读**：只看存档自己的各层版本号，构建信息只用于显示和定位问题（§1）。
- **读写用户数据**：正式版 `user://` 是 `%APPDATA%\UESTC\`，不改名；开发态用 `UESTC_DEV`（§2）。
- **改了导出配置、用户目录或存档格式**：再走一遍干净环境验证，在回报里点明改了哪一项（§3）。
- **打地图数据包**：与安装包里是同一份字节，详见本文 §5。
- **引入第三方素材、代码或插件**：只收白名单里的许可，登记后汇总进 `LICENSES.txt`；插件进 `third_party.json`（§7）。
- **改署名、窗口标题或图标，写许可页、`LICENSES.txt` 或数据包说明**：署名只有一行 OSM 文字，`--ui=0` 时也保留；各处对同一类内容写同一种许可，并指向本版本的 `map-data-<版本>.zip`（§5、§7、§7.1）。
- **场景或文案涉及学校标识**：本作不是校方作品，校徽、红十字换成原创图形（§7）。

## 1 版本号与构建信息

- **版本号**：`version.txt` 一行 `主.次.修订`，只由主会话在发版时改。
- **引擎与框架版本**：Godot 4.7.2 与 net10.0（C# 14）以 `tools/godot-version.txt`、`global.json`、csproj 为准，三处一致，门禁比对引擎 `--version`；升级单独评估，经主会话（DECISION-177，ENG-BUDGET-RULES §1 第 10 条），升级后按 `docs/godot-api/README.md` 重新生成 API 参考并通知各施工方。导出链的验证见 ENG-ARCH §8，导出模板的下载见 WF-RULE §6。
- **构建信息**写作 `<版本>+<提交>`；工作区有未提交改动时提交号带 `-dirty`，带 `-dirty` 的构建不对外发布。
- **谁读它**：开始页版本行、存档 meta 的 `build`、日志第一行与诊断摘要、两个压缩包的文件名（只取版本号）。
- **兼容性**只看存档自己的各层版本号（SYS-SAVE-FORMAT §1.4）。拿到诊断包或存档，按里面的提交号检出代码即可复现。

## 2 用户数据目录

- **正式版**：`user://` 是 `%APPDATA%\UESTC\`，里面有存档、设置、本机烘焙缓存、日志和诊断包（SYS-SAVE-FORMAT §3、ENG-PERF-STREAM §4）。
- **不带版本号，也不再改名**：新旧版本读写同一目录，更新后存档和设置照常沿用；目录名一旦发出，改名就等于让玩家丢存档。
- **开发态隔离**：开发态用 `UESTC_DEV`；门禁和基准不读 `user://`（ENG-GATES-SHOTS §1.1）。在开发机上试玩导出版，用的才是 `UESTC`。

## 3 导出与发布

1. **准备**：主会话在主检出的 `main` 上改 `version.txt`，提交并推送；工作区必须干净。
2. **导出**（主线之后）：运行 `scripts/release/` 的导出入口，一条命令从编译做到压缩包，中间用收口预烘的同一入口烘好全图缓存放进安装包（DECISION-165）：构建信息与 `LICENSES.txt`、7-Zip 打包 `UESTC-<版本>-win64.7z`、附上地图数据包（§5）；产物放仓库里不进 git 的 `build/`。
3. **检查**：G6 通过（体积与许可，ENG-GATES §1），两个包的文件名版本号与 `version.txt` 一致。
4. **发布**：在这个提交上打标签 `v<版本>` 并推送，在 GitHub 发布页上传两个包，说明里写版本号、构建信息和 §4 的更新方法。
- **导出设置**：导出预设开 Shader Baker（Direct3D 12，ENG-PERF §2、ENG-BUDGET-RULES §2）。
- **导出会改 `project.godot`**（引擎规范化，会删掉等于默认值的项）：导出之后要把它恢复到提交的版本，不提交导出改过的文件。
- **首次外发前走一遍干净环境**：在没装 .NET SDK、Godot、没有 `UESTC_*` 环境变量的普通 Windows 账户里解压、启动（首次运行读随包缓存，只编译着色器）、走一段、保存、退出，再把下一版解压到另一个目录启动，"继续"读到刚才的存档。以后改了导出配置、用户目录或存档格式，再走一遍。外发前复用集中测试结果，再加一轮少量首访定性观察；核对 `third_party.json`、`licenses.json`、`LICENSES.txt` 三者双向一致；发版说明写明删除 `settings.json` 即恢复画面设置（SYS-GRAPHICS §6）。

## 4 安装、更新与卸载

整包分发，没有安装器。仓库 README 和发布说明照下表写给玩家：

| 操作 | 做法 | 用户数据 |
| --- | --- | --- |
| 安装 | 解压到任意有写权限的目录，运行 `UESTC.exe`；直接读随包缓存，只编译着色器，缓存缺失或损坏时才本机补烘（ENG-ARCH §4） | 首次运行时在 `%APPDATA%\UESTC\` 建立 |
| 更新 | 新版本解压到新目录运行，不要覆盖旧目录，否则新旧程序集混在一起；新版能继续后再删旧目录 | 存档、设置自动沿用；游戏版本变了自动清缓存重烘，不用手动清（ENG-ARCH §4） |
| 回到旧版 | 运行旧目录里的程序 | 新版写过的存档在旧版里显示为"来自更新的版本"，只读不被写坏（SYS-SAVE-FORMAT §2） |
| 卸载程序 | 删掉安装目录 | 不受影响 |
| 清理用户数据 | 删掉 `%APPDATA%\UESTC\`；只想腾空间用设置卡"存储"行的"清除缓存"（UI-NAV §3.5） | 删目录会删掉存档、设置、缓存和日志，不可恢复；"清除缓存"不动存档和设置 |

## 5 地图数据包（ODbL）

地图数据按 ODbL 1.0 提供（WLD-OUTSKIRTS-BAND §6）。每个公开版本附一份 `map-data-<版本>.zip`，与分发压缩包放在同一个发布页。

- **内容**：本版本实际使用的派生数据库（各图 `world/` 下全部世界文件、地形文件和外围数据（WLD-OUTSKIRTS-BAND §6），与安装包里同一份字节）；`README.txt`（来源 © OpenStreetMap contributors、快照日期（取 `world/geo.json` 的署名）、ODbL 声明与全文地址、对应标签 `v<版本>`）。
- **指向同一份**：仓库 README 的许可一段、许可页和 `LICENSES.txt` 都写明本版本的数据包和它所在的发布页。

## 6 故障处理与诊断

- **日志**：`user://logs/godot.log`，每次启动轮换、保留最近 5 次；每行带归属文档编码（按文档文件头 paths 得到，仅作诊断）、当前图和流程阶段，第一行是构建信息；本机路径写成 `user://…`，不写出 Windows 用户名。
- **导出诊断**：操作说明页的"导出诊断"（UI-NAV §3.5）把日志和诊断摘要打成 `UESTC-diag-<版本>-<序号>.zip`，写进 `user://diagnostics/`并打开该目录；默认只含日志和摘要，存档由玩家另勾选；游戏不联网，诊断包由试玩者自己发回。
- **试玩者交回**：诊断包加一句话（在哪、做了什么、看到了什么）。代理按提交号检出代码复现，结论写进回报（WF-COMMS §3.4）。

## 7 许可

各部分许可如下（DECISION-170）；对外的权威文本是仓库根的 `LICENSE` 和 `README.md` 的"许可"一段，由主会话维护。

| 内容 | 许可 | 写在哪 |
| --- | --- | --- |
| 代码与脚本（C#、着色器、Python、PowerShell、构建配置） | MIT | `LICENSE` |
| 原创美术、音频、文案与设计文档（含 `docs/`） | 保留所有权利 | `README.md` 的"许可"一段 |
| 地图数据（© OpenStreetMap contributors）及其派生数据 | ODbL 1.0；每个公开版本附地图数据包（§5） | `README.md`；数据包的 `README.txt` |
| 第三方素材与代码（字体、音频、插件、移植的算法） | 按各自许可 | `third_party.json`、`game/data/audio/licenses.json`；汇总进 `LICENSES.txt`（§7.1） |

- **插件清单**：`game/addons/` 与 `third_party.json`（名称、版本、提交、许可证、哈希）；插件须声明支持当前小版本；NuGet 包在 `Directory.Packages.props` 集中锁版本。
- **第三方许可白名单**：插件与第三方代码只收 MIT、Apache、BSD、ISC、OFL。例外：只在开发机上运行的工具链依赖（如 pyproj 带进来的 certifi，MPL-2.0）可以用 MPL-2.0，条件是不进游戏导出、不把源码或库文件放进仓库，并在 `third_party.json` 里标为仅开发用；音频素材按 SYS-AUDIO §8 登记。
- **不是校方作品**：`README.md` 开头和每个版本的发布说明都写明这是个人创作，与电子科技大学没有关联；场景里的校徽、红十字换成原创图形（DECISION-065、ART-STYLE §4）。
- **说法一致**：`README.md`、许可页、`LICENSES.txt`、地图数据包的 `README.txt` 对同一类内容写同一种许可；改许可时先改 `LICENSE` 与 `README.md`，再同步其余几处。

### 7.1 署名、许可页、窗口标题与图标

- **署名**：内容只有一行文字"© OpenStreetMap contributors"，放在右下角，字号 10–11 px（1920×1080 基准，随界面缩放），颜色取当前雾色的反色，不透明度约 35%。界面的时间卡展开时署名左移让位，存储指示在署名上方；署名层在界面层之上，`--ui=0` 时画面上只剩它。界面态下点击署名用系统浏览器打开 OSM 版权页。数据采用 ODbL 协议，必须署名；外围街区的数据同样来自 OSM（WLD-OUTSKIRTS-BAND §6）。
- **许可页与 `LICENSES.txt`**：思源黑体、Inter、JetBrains Mono（SIL OFL 1.1）和 Godot 引擎（MIT）的许可文本，放进设置卡的"许可"页和开始页的"制作人员与许可"（同一个组件），安装包里另附 `LICENSES.txt`。不使用思源宋体。
  - 开头先写本作自身的许可（上表四类），同一处注明这是个人创作，不是学校的官方作品。
  - 写明世界文件与外围数据是 ODbL 1.0 下的派生数据库，附快照日期，并指向本版本的地图数据包（§5）；音频素材的署名从 `licenses.json` 生成（SYS-AUDIO §8）。
- **窗口标题**为"电光年华"；描述为"电子科技大学清水河校区，三渲二漫游作品"。
- **图标**：八角晶片，画法见 UI-VISUAL-COMPONENTS §3.3；导出规格：底色科技蓝 #1961A0，切角旁暖色过孔 #FFE0A8，16、24 px 只留切角方块和八边形外框。
