---
id: STD-CONVENTIONS
title: 坐标单位与命名约定
category: standards
parent: STD-INDEX
summary: 跨文档共同约定：坐标与单位、方位角、时间写法，编号体系与命名（地图与实体 ID、色名、枚举纪律、持久化 ID、文件与编号格式）、确定性原则、数据路径
keywords: [约定, 坐标系, 单位, 角度, 方位角, 时间写法, 编号体系, 命名, 地图 ID, 实体 ID, 色名, 枚举纪律, 持久化 ID, 文件命名, 提交说明格式, 确定性, 数据路径]
depends: [CORE-INDEX]
related: [ENG-ARCH, ENG-GATES, SYS-TIME, SYS-SAVE, SYS-MAPS, REG-DECISIONS, REG-GLOSSARY]
contracts: []
data: []
gates: [G1, G5]
paths: ["scripts/requirements.*", "game/testing/**", "game/data/build-code-fingerprints.json", "game/gen/maps/qingshuihe/Campus.Gen.Maps.Qingshuihe.csproj", "game/gen/sites/**", ".collab/tools/**", "dashboard/**", "game/gen/Campus.Gen.csproj", "game/kernel/map/data/**", "game/kernel/renderer/geometry/**", "game/kernel/renderer/render-state/**", "game/kernel/instances/**", "game/gen/geometry/**", "scripts/gates/arch/Campus.Arch.Tests.csproj", "scripts/godot-api/ApiDump/ApiDump.csproj", "game/contracts/Runtime/Campus.Contracts.Runtime.csproj", "game/contracts/Campus.Contracts.csproj", "game/data/maps/*/interiors.json", "game/Campus.Gen.Tests.csproj", "game/Campus.Tests.csproj", "game/contracts/.gdignore", "game/export_presets.cfg", "game/contracts/*.csproj", "game/kernel/registry/**", "game/data/rulesets/**", "scripts/gates/arch/**", "**/Directory.*.targets", "game/Campus.csproj", "game/gen/.gdignore", "game/project.godot", "Directory.*.targets", "game/kernel/stubs/**", "scripts/godot-api/**", "**/Directory.*.props", "scripts/gate.ps1", "third_party.json", "Directory.*.props", "docs/godot-api/**", "game/contracts/**", ".gitattributes", "scripts/build/**", "scripts/setup/**", "game/gen/bakeinput/assembly/**", "scripts/docs/**", "budgets.json", "game/addons/**", "global.json", "version.txt", "scripts/lib/**", ".gitignore", ".ignore", "AGENTS.md", "CLAUDE.md", "README.md", "启动游戏.cmd", "**/*.csproj", "LICENSE", "tools/**", "**/*.sln", "game/.editorconfig", "scripts/git-hooks/**"]
---

# STD-CONVENTIONS · 坐标单位与命名约定

> **范围**：本文规定所有文档和代码共同遵守的写法：坐标与单位、方位角、时间的单位与写法，编号体系与命名（地图与实体 ID、同号不同义的读法、命名原则与代码里的名字、色名、枚举与扩展纪律、持久化 ID 的稳定、文件与编号的命名格式），确定性原则，数据路径。
> **不在本文**：契约与帧顺序（`game/contracts/` 源码，说明见 ENG-ARCH §3）；命令行与门禁（ENG-GATES）；预算（ENG-BUDGET）；目录归属（各文档文件头的 paths，STD-DOC §2）；按图文件归属（SYS-MAPS、MAP-QSH-*）；决策（REG-DECISIONS）；术语（REG-GLOSSARY）；文档写法（STD-DOC）。
> **查阅**：先读 §0；坐标、角度、单位看 §1；时间写法看 §2；ID、编号体系、命名、色名、枚举纪律、持久化 ID、文件与编号格式看 §3；确定性看 §4；路径看 §5

## 0 结论

- 单位米；x 向东、y 向上、z 向南；数据坐标直接当 Godot 世界坐标用；正北是 −z（§1.1）。
- 方位角从正北顺时针，单位度；Godot 节点的 `Rotation.Y = −A`（弧度）（§1.2）。
- 时间只往前走；钟点正文写 HH:MM，命令行与 JSON 写小数小时（§2）。
- 每张图一个坐标原点和投影；实体 ID 只在本图内唯一，按 ID 定位对象，不按名字（§1.1、§3.1）。
- 编号前缀各有固定含义，同号不同义时按 §3.3 的写法区分（§3.2、§3.3）。
- 色名是唯一的取色途径；枚举只追加不重排；已进 `main` 的持久化 ID 不复用（§3.5、§3.6、§3.7）。
- 同一构建加同一种子加同一串输入，得到同样的结果；随机只用契约的 `Rng` 与 `StableHash`（§4）。

### 0.1 接入要点

- **用坐标、角度、单位**：x 东、y 上、z 南，单位米；方位角从正北顺时针；地面高度读 `IWorldModel.Terrain`，不写死 y = 0（§1）。
- **写时间**：正文 HH:MM，命令行与 JSON 小数小时；世界时间只增不减（§2）。
- **起 ID 和名字**：前缀、大小写、命名空间、命令行取值按 §3；代码里不写地图 ID、实体 ID 的字面量（§3.1）。
- **加枚举值**：只在末尾追加；消费方写穷举 `switch`，不做二值判断（§3.6）。
- **删或改被引用的对象**：ID 不复用，改名不改身份，确需换 ID 给旧新映射（§3.7）。
- **生成或随机的结果要能复现**：只用契约的随机来源，细则见 ENG-ARCH §2（§4）。
- **写或改文档**：按 STD-DOC；**施工**：入口见 WF-ENTRY，顶层规则见 CORE-INDEX。

## 1 坐标与单位

### 1.1 坐标系

- 单位米；x 向东、y 向上、z 向南。与 Godot 一样是右手系、y 向上，数据坐标直接当 Godot 世界坐标用，不做换算；正北就是 Godot 的前方（−z），偏航 0° 的相机朝北（DECISION-144）。
- **每张图一个原点和投影**，写在该图 `world/geo.json` 里，`map.json` 不重复；校界、箱庭包围盒、`map_rect` 这类按图的范围数也不在本文，见 SYS-MAPS §2、§7。
- 地图之间的坐标互不相通；跨图引用一律写成"地图 + ID"。单图半径（离中心最远的可达点）不超过 8 km（SYS-MAPS §8）。
- **高度**：地面不是平的，全图约 −5…+10 m，地形写在地形覆盖层；地面高度、垫层、水位一律读 `IWorldModel.Terrain`，不写死 y = 0（WLD-TERRAIN）。
- **两套数学类型**：契约程序集只用 `System.Numerics`，Godot 侧用 `Transform3D`；两者之间只用内核提供的扩展方法转换（ENG-ARCH §2）。

### 1.2 角度

- **方位角**一律用罗盘方位：从正北开始顺时针，单位度。方位 A 的水平单位向量是 (x, z) = (sin A, −cos A)。
- **Godot 节点**：罗盘方位 A 对应 `Rotation.Y = −A`（弧度）。因为 Godot 绕 y 轴的正向旋转是从上往下看逆时针，与罗盘方向相反。偏航、日月方位、点位朝向、入口朝向一律按这条换算。
- **俯仰**：度，负值表示朝下看（UI-NAV-WAYPOINTS §1）。
- 契约里以 `Deg` 结尾的字段都是度；只有 Godot 节点的属性用弧度。
- **"目标点、方位、仰角、距离"的写法**（审查视角）：方位是从目标点看向相机的罗盘方位，相机朝向目标点时偏航 = 方位 − 180°（ENG-GATES-SHOTS §1）。

### 1.3 其他单位

- 长度 m，速度 m/s；时间单位见 §2。
- **眼高**：行走 1.62 m（SYS-WALK）。
- **像素基准**：截图与界面都按 1920×1080；线宽按"内部高度 / 1080"换算（ENG-GATES-SHOTS §1、UI-VISUAL-COMPONENTS §5）。
- **颜色**：色彩计算在 OKLab / OKLCH 里做，明度写 L（OKLab）；十六进制色值只出现在调色板数据文件里（§3.5）。
- **音量**：dB；界面滑杆值 0–1（SYS-AUDIO）。

## 2 时间写法

时间各量的定义（`WorldHours`、`Clock`、`SeasonPos`、流逝倍率、`AnimTime`、换世界实例）以 SYS-TIME §1 为准；本节只定单位与写法。

- **单位**：`WorldHours` 与 `Clock` 用小时；`AnimTime` 用秒，与世界时间无关；天气时长在队列项与命令行里按分钟，`WeatherSpan` 按 `WorldHours`；真实秒是帧间隔累加，偶发事件、转场、界面动效按它。
- **钟点的写法**：正文写 HH:MM；命令行与 JSON 写小数小时（17:30 写 `17.5`，17:15 写 `17.25`）。
- **只能向前**：没有任何让 `WorldHours` 减小的方法；设定钟点是"跳到下一次出现"。读档、新游戏是换世界实例，不算倒流（SYS-TIME §1.2）。
- **系统时钟**只在存档元数据里读一次，只用于显示；其余代码不读，真实秒也不读它（ENG-ARCH §2）。着色器不用内置 `TIME`，一律用 `anim_time`（SYS-MOTION §1.2）。

## 3 标识与命名

### 3.1 地图、实体与世界文件

| 标识 | 格式 | 规则 |
| --- | --- | --- |
| 地图 ID（`MapId`） | 小写字母、数字、连字符，≤ 32 字符；与目录名相同，如 `qingshuihe` | 代码里不写地图 ID 字面量（地图内核和门禁夹具除外）；不用 "qsh" 指地图（DECISION-137） |
| 实体 ID（`FeatureId`） | `world/<区>.json` 里记录的 `id`；清水河前缀 `qsh:`，形如 `qsh:<类型>:<编号>`；记录内部件另有部件 ID | 只在本图内唯一；`qsh:` 是数据的既有写法，不是地图 ID；按 ID 定位对象，不按名字；通用代码里不写字面量；以后的图用自己的前缀；部件 ID 进楼宇随机种子，路线 ID 的顺序决定行道树编号，都不改 |
| 设计增补 | `design:` 开头；锚点写 `design:anchor:Axx` | 直接加在本区世界文件里，记录写 `note`（MAP-QSH-ADDITIONS） |
| 可行走区域 | `walk:` 开头 | 只在可行走覆盖层（SYS-WALK-BOUNDS §1） |
| 地形操作 | `design:terrain:` 开头，本图内唯一 | 只在地形覆盖层，每条写理由（WLD-TERRAIN-APPLY §1） |
| 环境发声器 | `amb:` 开头，本图内唯一 | 见 ENG-ARCH §3 |
| RPG 预留前缀 | `npc:`、`item:`、`quest:` | 观光数据不得使用（ENG-ROADMAP §3.1） |

- **世界文件**：地图数据归项目所有，每区一份 `world/<区>.json`，格式与增删改规则见 SYS-MAPS §1.1（DECISION-252、DECISION-253）；`design:` 记录写 `note` 说明设计意图（INV-9）。

### 3.2 编号体系

| 前缀 | 是什么 | 定义在 |
| --- | --- | --- |
| INV-n | 全局不变量，INV-1–INV-12 | PRD-INVARIANTS §1 |
| P | 点位，两位数（P00）；新增接在最大号之后 | UI-NAV-WAYPOINTS §1 |
| A | 锚点：导览亭与导览桩 | SYS-ATLAS-TRAVEL §2 |
| R | 推荐行走路线 | SYS-WALK-BOUNDS §6 |
| V、E | 审查视角：V 全景与中景，E 人眼高度与低空 | ENG-GATES-SHOTS §1 |
| F | 飞行路线脚本 | ENG-GATES-SHOTS §1 |
| G | 门禁 | ENG-GATES §1 |
| DECISION- | 决策，DECISION-001 起，三位数，全文连续，只增不复用 | REG-DECISIONS §0 |
| GAME-、TOOL-、BATCH- | 施工队列的行：GAME 游戏本体，TOOL 协作与工具，BATCH 批量机械活；三位数，拆出的子行加小写字母（GAME-034a） | WF-STATUS §2 |
| RESEARCH- | 调研单，RESEARCH-001 起，三位数 | WF-STATUS §4 |
| O0–O3 | 外围分层 | WLD-OUTSKIRTS-BAND §2.2 |
| L0+–L4 | 建筑与地块的细节级别；室内另有 I0、IS、IF 三级，碰撞级 C | ENG-BUDGET-LOD §1 |
| K1–K5 | 视觉精度区 | ART-FIDELITY §0 |
| B1–B13 | 视觉精度的底线条目 | ART-FIDELITY §2.0 |
| M1–M8 | 界面动效的八个母题 | UI-VISUAL-MOTION §1.2 |
| W-xxx | 窗卡类型 | WLD-INTERIORS-ACCESS §3 |
| s | 存档位 `s1`–`s3` | SYS-SAVE §1 |

### 3.3 同号不同义的读法

| 写法 | 可能指 | 读法与写法 |
| --- | --- | --- |
| G | 只指 ENG-GATES 的门禁 | 顶层规则的条目写它的标题，不写 G 与条号 |
| E、O | E1–E7 是审查视角；O0–O3 是外围分层 | E 只指审查视角，写"视角 E1"或 `--pose=E1`；外围写"外围 O1"（DECISION-157） |
| P0、P1… | 点位 P00–P23（两位数）、图书馆的三条"看进去"路径 P1–P3、开发阶段 P0–P3 | 点位一律两位数；路径写"MAP-QSH-INTERIORS-LIBRARY 的路径 P1"；阶段写"P0 阶段" |
| L0、L2… | 细节级别 L0+–L4，或声景七层 L0–L6 | 声景层写"声景 L2" |
| I0、I1… | 室内兜底层 I0，或室内推进批次 I1–I3 | 批次写"室内批次 I1" |
| 第 N 条 | 各文档编号列表里的一条（流式不变量、一票否决、硬规则等） | 带文档与小节写，如"ENG-BUDGET-RULES §1 第 9 条"，不写"不变量 5" |
| B1、B2… | 视觉精度底线 B1–B13，或主楼的分区 B1–B3 区 | 底线写"底线 B4"；分区写"主楼 B2 区" |
| GAME、TOOL、BATCH、C | 队列行 TOOL-170、BATCH-007；碰撞级 C；存档位 s1 | 队列行带连字符写三位数；碰撞级写"碰撞级 C"；存档位小写；任务单里的步骤写"第 n 步"，不另起字母编号 |

### 3.4 命名写法

**命名原则**（全项目，DECISION-251）：
1. **编号-主题**：带编号的名字一律"编号-中文主题"，编号只做索引，意思靠主题读出。
2. **一物一名**：同一个角色或东西只有一种叫法——主会话、Codex 主线、Claude 侧、Codex 侧；Opus、Sonnet、Sol、Astra 只指模型；"指挥"只指主会话或 Codex 主线（WF-FLOW §3.1）。
3. **名字写内容，不写来源**：不用会话名、日期、版本号当名字。
4. **不造缩写**：字母前缀与缩写都在 §3.2 登记，不再新增单字母前缀。
5. **同目录同写法**：语言与引擎有惯例的跟惯例（C# PascalCase，Godot 资源与着色器 snake_case，Python 下划线，PowerShell 与 cmd 连字符）。
6. **编号只增、不复用、不改义**（§3.7）。
7. **规则与实物同源**：文档写的格式、钩子提示与巡逻检查一起改，不留旧写法（DECISION-200）。
8. **可读优先于简短**。

文件与编号的具体格式见 §3.8；以下是代码与数据里的名字。

- **命名空间**：PascalCase，按 ENG-ARCH §1 的分层与职责取，不跟目录的字面大小写（`game/gen/maps/qingshuihe/south/` 对应 `Campus.Gen.Maps.Qingshuihe.South`，`game/gen/sites/` 对应 `Campus.Gen.Sites`）。程序集与目录的对应见 ENG-ARCH §2。
- **特性的 ID**：小写、点分（`[ContentModule("zone.south")]`）；区域私有的部件 ID 加区名前缀（ENG-ARCH §2、§5）。
- **存档段 ID**：小写字母、数字、连字符；按图的段在文件里展开为 `map:<id>/<段>`（SYS-SAVE §2）。
- **全局着色器参数**：小写下划线、不带前缀（`weather_a`）；C# 侧只用 `ShaderGlobals` 的常量，总线名只用 `AudioBuses` 的常量，不写字符串字面量（ENG-PERF §1.5）。着色器文件名以系统名作前缀，白名单见 ENG-PERF §1.4。
- **JSON**：字段 camelCase，枚举按字符串，向量写成数组；多态类型的类型字段写在每个对象的第一个字段（ENG-ARCH §3）。
- **命令行取值**：天气写连字符小写（`few-clouds`），季节写英文小写，预设、设备档和 `--quality-set` 的单项名见 ENG-GATES-SHOTS §1。

### 3.5 色名

- 色名是唯一的取色途径：色名 → 色号（固定，只增不改）→ 调色板纹理的一列（ART-STYLE §2.5）。内容代码只写色名，代码和着色器里不写十六进制色值（G4）。
- **写法**：小写，点分层，`<对象>.<状态或用途>`，如 `wall.lit`、`window.warm`；一段内多个词用驼峰（`water.poolGlow`）。后缀 `.lit`、`.shade`、`.deep` 分别是受光、影、深影（ART-STYLE §2.1）。
- **界面色**：前缀 `ui.`，阶梯写 `ui.blue.400`；不进调色板纹理（UI-VISUAL §3）。
- **登记与退役**：谁登记、写在哪、重名与退役色名的处理见 ART-STYLE §2.5；色名重复或色号冲突由 G5 报。

### 3.6 枚举与扩展纪律

以下写法现在就照做，以后加观察模式、规则集、RPG 子系统时才不用改既有语义（预留见 ENG-ROADMAP §3）：

- **枚举只追加、不重排、不删值**：所在的契约组冻结后，枚举只能在末尾追加；JSON 与存档里按字符串存（DECISION-134）。读到不认识的枚举字符串，按"这一段不合法"处理，不猜（SYS-SAVE-FORMAT §1.3）。
- **穷举 `switch`，`default` 报告未知值**：会追加值的枚举，消费方一律写穷举的 `switch`，`default` 拒绝或报错，不把未知值静默当成某个已知值。
- **不做二值判断**：不写 `mode == ViewMode.Fly ? … : …`；它会把以后追加的值静默归进某一边，编译器查不出，靠评审。
- **不写观光专属的常量**：解锁策略名、事件 ID、允许的观察模式这类观光规则，一律读规则集（ENG-ROADMAP §2）。
- **"观者在哪"与"画面从哪看"分开读**：前者读 `IObserverState`，后者读 `ICameraRig`。
- **换世界实例直接落到新值**：做平滑、回差、保留历史的系统看到世界实例变了，就把状态直接设成新值（SYS-SAVE-FLOW §2.2）。

### 3.7 持久化 ID 的稳定

存档、世界文件、标记和其他数据文件按 ID 引用对象，ID 就是对象的身份。地图 ID、实体 ID（含 `design:` 增补）、点位 P、锚点 A、路线 R、室内入口 ID、区域 ID 与地点翻译键、存档段 ID 都按本节执行：

- **不复用**：ID 进了 `main` 以后，对象删掉了，ID 也不再分给别的对象；新对象取新号。
- **改名不改身份**：改显示名、译名、所在文件或分区，ID 不变；轮廓、高度、位置的调整直接改记录，ID 也不变。
- **确需换 ID 时给旧新映射**：例如对象被拆分或合并。负责数据的包随数据提交旧 ID → 新 ID 的映射；引用这些 ID 的存档段升版本，由存档迁移按映射改写（SYS-SAVE-FORMAT §1.4）。
- **真正删除不给映射**：读档时按 SYS-SAVE-FLOW §2.6 的校验修复处理。

### 3.8 文件与编号的命名格式

各文档讲到这些名字时只指向本节，不另写格式（DECISION-251）。

| 类 | 格式 | 例子 |
| --- | --- | --- |
| 规范文档 | `编码-中文简题.md`，简题 ≤14 字；编码规则见 STD-DOC §3；`docs/godot-api/` 下生成的 API 参考不在此列 | `SYS-WEATHER-天气.md` |
| 任务单 | `tasks/<claude 或 codex>/<队列行号>-<主题>.md`；一张单管几行时用主行的号 | `GAME-036-广场图样稳定性采集.md` |
| 旁路单 | `side/cards/BATCH-<nnn>-<主题>.md` | `BATCH-007-清点外围楼名.md` |
| 调研 | `research/RESEARCH-<nnn>-<主题>/`，日期只记在 INDEX | `RESEARCH-025-命名规范/` |
| 来信 | `notes/<claude 或 codex>/<年月日-时分秒>-<发件方>-<短词>.md`，发件方写 `claude`、`codex`，作者经观察台留言写 `author`（只进 `notes/claude/`） | `20261019-024457-claude-编号统一.md` |
| 契约申请 | `requests/contract-<队列行号>-<主题>.md` | `contract-GAME-003-地形求值.md` |
| 世界文件 | 区文件 `world/<区>.json`；原点、投影与校界 `world/geo.json` | `world/core.json` |
| 脚本 | `scripts/` 的 Python、PowerShell 与命令脚本文件名用小写连字符；C# 工具类型文件遵守 C# 命名。开头一句话作用加"用法："（STD-CODE） | `check-naming.ps1`、`clean-artifacts.py` |
| 固定机位 | `<昼、夜或雨>-<点位>-<地点>` | `夜-P00-全景` |
| 临时截图 | `output/render-review/<点位>-<模式>-<年月日>.png` | `P16-旁观-20261019.png` |
| 临时工作树 | `E:\Project\UESTC_RPG.wt\<队列行号或用途>`（WF-RULE §13） | `GAME-015b` |
| 提交说明 | `<队列行号> <领域词>：一句话`；一个提交只写一个行号（WF-RULE §7）；没有队列行的写 `文档：`、`工具：`、`数据：` | `GAME-034a 街道家具：主路口横排白色石球` |
| 子代理名 | Claude 侧的 description 写队列行号加中文主题，不混英文，没有队列行的以"调研""核查"开头；Codex 侧的 task_name 只能是标识符，写小写行号加英文短词 | `GAME-031 草地积水`、`GAME-031_031_grass_puddles` |

## 4 确定性

同一构建加同一种子加同一串输入，得到同样的结果；时间不能逆转，所以不要求"倒回去复现"。随机来源、种子推导、禁用函数与遍历顺序以 ENG-ARCH §2 为准。

- **烘焙产物不依赖世界状态和画面设置**：改时间、换季、换天气、改画质不会让烘焙产物失效（ENG-PERF-STREAM §1）。
- **门禁与截图**只由命令行决定，不读 `user://`（ENG-GATES-SHOTS §1）。

## 5 路径

- 按图的数据都在 `game/data/maps/<地图>/` 下，一律由 `IMapContext.DataRoot` 拼出，代码里不写数据路径（SYS-MAPS §1、§6，DECISION-139）；各文件的归属见 SYS-MAPS 与相关文档文件头的 `paths`。
- `user://` 只有存档服务能读写（ENG-ARCH §2）。
