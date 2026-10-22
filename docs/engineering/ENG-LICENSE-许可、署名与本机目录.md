---
id: ENG-LICENSE
title: 许可、署名与本机目录
category: engineering
parent: ENG-INDEX
summary: 只从项目运行、不打包不设版本号，用户目录与日志，各部分许可与插件清单，OSM 署名、许可页、窗口标题与图标
keywords: [许可, 署名, 本机目录, 不打包, 不发布, 不设版本号, 用户目录, '%APPDATA%\UESTC', 日志, MIT, ODbL, third_party.json, 插件清单, 第三方许可白名单, OSM 署名, 许可页, 窗口标题, 窗口图标, 八角晶片]
depends: [ENG-ARCH]
related: [STD-CONVENTIONS, SYS-SAVE, SYS-TITLE, SYS-GRAPHICS, UI-NAV, UI-VISUAL, SYS-AUDIO, WLD-OUTSKIRTS]
contracts: []
data: ["third_party.json"]
gates: []
paths: ["game/project.godot", "third_party.json", "game/addons/**", "README.md", "LICENSE"]
---
# ENG-LICENSE · 许可、署名与本机目录

> **范围**：本文规定游戏怎样运行与交付、用户数据目录与日志、各部分按什么许可公开与第三方怎样登记，以及画面右下角的署名、许可页、窗口标题与图标。
> **不在本文**：存档与设置文件的格式（SYS-SAVE-FORMAT）；烘焙缓存的位置与查找（ENG-ARCH §4）；日志的写法（STD-CODE §0.1）；许可页的界面（UI-NAV §3.5）；图标的画法（UI-VISUAL-COMPONENTS §3.3）；音频素材的登记（SYS-AUDIO §8）。
> **查阅**：先读 §0；用户目录与日志看 §1；许可与插件清单看 §2；署名、许可页、窗口标题与图标看 §3。

## 0 结论

1. **只从项目运行**：个人实验作品，不导出、不打包、不发布，不设版本号（DECISION-181）；构建只有一种（DECISION-310）。
2. **用户目录只有一个**：`%APPDATA%\UESTC\`，不改名（§1）。
3. **许可分四类**：代码与脚本 MIT；原创美术、音频、文案与设计文档保留所有权利；OSM 派生数据 ODbL 1.0；第三方素材按各自许可（§2）。
4. **署名**：画面右下角只有一行"© OpenStreetMap contributors"，`--ui=0` 时画面上只剩它（§3）。

### 0.1 接入要点

- **想加版本号、导出或安装包**：不加（DECISION-181）；存档能不能读只看存档自己的各层版本号（SYS-SAVE-FORMAT §1.4）。
- **读写用户数据**：`user://` 是 `%APPDATA%\UESTC\`，不改名（§1）。
- **引入第三方素材、代码或插件**：只收白名单里的许可，插件登记进 `third_party.json`（§2）。
- **改署名、窗口标题或图标，写许可页**：署名只有一行 OSM 文字，`--ui=0` 时也保留；各处对同一类内容写同一种许可（§2、§3）。
- **场景或文案涉及学校标识**：本作不是校方作品，校徽、红十字换成原创图形（§2）。

## 1 用户目录与日志

- **位置**：`user://` 是 `%APPDATA%\UESTC\`，由 `project.godot` 的自定义用户目录名决定，不带版本号，也不再改名；里面有存档、设置和日志（SYS-SAVE-FORMAT §3）。
- **日志**：`user://logs/godot.log`，引擎原生文件日志，每次启动轮换、保留最近 5 次；诊断只经 `DiagnosticLog` 写入（ENG-ARCH §2）。设置卡"键位"页底的 [打开日志目录] 直接打开这里（UI-NAV §3.5）。
- **清理**：删掉 `%APPDATA%\UESTC\` 会删掉存档、设置和日志，不可恢复；烘焙缓存不在这里，在仓库的缓存目录（ENG-ARCH §4），由收口预烘维护。

## 2 许可

各部分许可如下（DECISION-170）；对外的权威文本是仓库根的 `LICENSE` 和 `README.md` 的"许可"一段，由主会话维护。

| 内容 | 许可 | 写在哪 |
| --- | --- | --- |
| 代码与脚本（C#、着色器、Python、PowerShell、构建配置） | MIT | `LICENSE` |
| 原创美术、音频、文案与设计文档（含 `docs/`） | 保留所有权利 | `README.md` 的"许可"一段 |
| 地图数据（© OpenStreetMap contributors）及其派生数据 | ODbL 1.0；派生数据库就是仓库里各图 `world/` 下的世界文件、地形文件和外围数据（WLD-OUTSKIRTS-BAND §6） | `README.md` |
| 第三方素材与代码（字体、音频、插件、移植的算法） | 按各自许可 | `third_party.json`、`game/data/audio/licenses.json` |

- **插件清单**：`game/addons/` 与 `third_party.json`（名称、版本、提交、许可证、哈希）；插件须声明支持当前小版本；NuGet 包在 `Directory.Packages.props` 集中锁版本。
- **第三方许可白名单**：插件与第三方代码只收 MIT、Apache、BSD、ISC、OFL。例外：只在开发机上运行的工具链依赖（如 pyproj 带进来的 certifi，MPL-2.0）可以用 MPL-2.0，条件是游戏运行时不加载、不把源码或库文件放进仓库，并在 `third_party.json` 里标为仅开发用；音频素材按 SYS-AUDIO §8 登记。
- **不是校方作品**：`README.md` 开头写明这是个人创作，与电子科技大学没有关联；场景里的校徽、红十字换成原创图形（DECISION-065、ART-STYLE §4）。
- **说法一致**：`README.md` 与许可页对同一类内容写同一种许可；改许可时先改 `LICENSE` 与 `README.md`，再同步许可页。

## 3 署名、许可页、窗口标题与图标

- **署名**：内容只有一行文字"© OpenStreetMap contributors"，放在右下角，字号 10–11 px（1920×1080 基准，随界面缩放），颜色取当前雾色的反色，不透明度约 35%。界面的时间卡展开时署名左移让位，存储指示在署名上方；署名层在界面层之上，`--ui=0` 时画面上只剩它。按住 Alt 点击署名用系统浏览器打开 OSM 版权页。数据采用 ODbL 协议，必须署名；外围街区的数据同样来自 OSM（WLD-OUTSKIRTS-BAND §6）。
- **许可页**：思源黑体、Inter、JetBrains Mono（SIL OFL 1.1）和 Godot 引擎（MIT）的许可文本，放进设置卡的"许可"页和开始页的"制作人员与许可"（同一个组件）。不使用思源宋体。
  - 开头先写本作自身的许可（§2 表中四类），同一处注明这是个人创作，不是学校的官方作品。
  - 写明世界文件与外围数据是 ODbL 1.0 下的派生数据库，附快照日期（取 `world/geo.json` 的署名）；音频素材的署名从 `licenses.json` 生成（SYS-AUDIO §8）。
- **窗口标题**为"电光年华"；描述为"电子科技大学清水河校区，三渲二漫游作品"。
- **图标**：八角晶片，画法见 UI-VISUAL-COMPONENTS §3.3；底色科技蓝 #1961A0，切角旁暖色过孔 #FFE0A8，16、24 px 只留切角方块和八边形外框。
