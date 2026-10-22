---
id: WF-BUILD
title: 施工边界
category: workflow
parent: WF-INDEX
summary: 施工时能改哪里、共享文件、契约申请与冻结、构建与测试排队、新模块怎么接入
keywords: [独占目录, 共享文件, 契约申请, 契约冻结, 构建锁, 构建与测试排队, .uid, 特性接入, paths, G3]
depends: [WF-RULE, CORE-INDEX, ENG-ARCH]
related: [WF-ENTRY, WF-FLOW, WF-STATUS, STD-CONVENTIONS, ENG-GATES]
contracts: []
data: []
gates: [G3, G4]
paths: ["game/gen/maps/qingshuihe/Campus.Gen.Maps.Qingshuihe.csproj", "game/gen/Campus.Gen.csproj", "scripts/gates/arch/Campus.Arch.Tests.csproj", "scripts/godot-api/ApiDump/ApiDump.csproj", "game/contracts/Runtime/Campus.Contracts.Runtime.csproj", "game/contracts/Campus.Contracts.csproj", "game/Campus.Gen.Tests.csproj", "game/Campus.Tests.csproj", "game/contracts/.gdignore", "game/export_presets.cfg", "game/contracts/*.csproj", "**/Directory.*.targets", "game/Campus.csproj", "game/project.godot", "Directory.*.targets", "**/Directory.*.props", "scripts/gate.ps1", "third_party.json", "Directory.*.props", "game/contracts/**", "scripts/build/**", "game/addons/**", "global.json", "**/*.csproj"]
---

# WF-BUILD · 施工边界

> **范围**：本文规定施工子代理写代码和做内容时的硬边界：能改哪些文件，哪些要提申请，契约怎么申请与冻结，构建与测试怎么排队，新模块怎么接入。
> **不在本文**：代码边界与禁用写法：原则见 ENG-ARCH §2，清单以各程序集的 `BannedSymbols.txt` 和 G4 为准；契约的内容：`game/contracts/` 源码与 ENG-ARCH §3；冻结状态：WF-STATUS §1；目录与归属明细：各文档文件头的 `paths`；共享文件清单：本文 §1；Godot 代码的写法（查签名、GDScript、场景与路径）：STD-CODE §0.1；内容与画面的底线：CORE-INDEX §0；全员守则：WF-RULE。
> **查阅**：先读 §0；改哪里与申请看 §1；G4 拦不住的约定在哪看 §2；契约申请与冻结看 §3；构建与测试排队看 §4。

## 0 结论

1. **只改派工简报允许的路径**（§1）：写入边界以简报为准，目录来自施工队列的"目录"列；门禁 G3 按文档文件头的 `paths` 核对改动文件的归属。
2. **共享文件不自己改**（§1）：工程共享件由 Codex 主线改，设计文档由主会话改；要改契约、全局着色器参数、着色器文件、程序集引用或 NuGet 包，写契约申请。
3. **新模块用特性接入**（§1）：不改注册表、不改主场景、不加 Autoload。
4. **代码边界和禁用写法的权威是 ENG-ARCH §2**，由门禁 G4 拦截；G4 拦不住的约定各有权威，见 §2。
5. **契约申请要早、冻结分批**（§3，DECISION-134）：写 `requests/contract-<队列行号>-<主题>.md` 交 Codex 主线；已冻结的组只增不改。
6. **构建、导入、烘焙、门禁、截图一次只跑一个**（§4，DECISION-211）：经构建锁排队，不绕过。

### 0.1 接入要点

- **新增契约、全局着色器参数、着色器文件、程序集引用、NuGet 包**：不自己加，写 `requests/contract-<队列行号>-<主题>.md` 交 Codex 主线（§3）；要早提。
- **要改的路径不在简报允许的范围里**：不改，写进回报或契约申请。
- **写 gen 或烘焙子任务**：随机只用契约的 `Rng` 与 `StableHash`，同机同输入同输出即可（ENG-ARCH §2，DECISION-232）。
- **跑构建、导入、烘焙、门禁或截图**：经构建锁排队，不绕过；只走命令行，不在编辑器里重存场景（§4）。
- **写 Godot 代码前**：先查 API 签名，写法见 STD-CODE §0.1。
- **写会追加值的枚举或观光规则、做随时刻季节天气变化的东西**：权威见 §2 指向的 INV-8 与 STD-CONVENTIONS §3.6。

## 1 改哪里

- **只改派工简报给的路径**：以简报的"允许改"为准，目录来自施工队列行的"目录"列。这是"同一批文件同一时间只有一个写者"的分工依据（WF-FLOW §2.1）。
- **共享文件不自己改**：工程共享件（契约、csproj、`Directory.*.props`、`project.godot`、`scripts/gates/shader-whitelist.json`、`gate.ps1`、`scripts/` 下的工程脚本等）由 Codex 主线直接提交推送；`docs/` 由主会话改。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请交 Codex 主线（§3）。
- **新模块用特性接入**：接入特性的清单见 ENG-ARCH §2；不改注册表、不改主场景、不加 Autoload。
- **场景、路径与 `.uid`**：Godot 代码的写法（场景由代码生成、`res://` 路径、不手写 `uid://`）见 STD-CODE §0.1；`.uid` 的处理见 §4。

## 2 G4 拦不住的约定在哪

几何不随时刻、季节、天气变见 INV-8（细则 SYS-TIME §0.1、SYS-WEATHER §0.1）；观光专属常量读 `IRuleset`、会追加值的枚举写穷举 `switch` 见 STD-CONVENTIONS §3.6。本文不重复。

## 3 契约申请与冻结

- **申请**：任何代理要改契约或共享工程文件，写 `requests/contract-<队列行号>-<主题>.md`（短头部见 WF-COMMS §3.3）。Codex 主线处理后在文件末尾写结论和对应的提交。
- **提交**：Codex 主线在主检出直接提交推送；改共享件前告知邻居，改完同批迁完使用方（WF-RULE §10）。
- **冻结**：契约分批冻结：某组契约的真实生产方与使用方在主线上接通、相关核心专项通过后，由主会话宣布冻结这一组，之后只增不改；没冻结的组按申请修改；冻结状态见 WF-STATUS（DECISION-134）。
- **要早**：上游包开工后头几个小时内就把契约申请提上来，免得下游写得太深再返工。

## 4 构建与测试排队

Codex 侧的代理共用主检出 `E:\Project\UESTC_RPG`，`bin/`、`obj/`、`.godot/` 和烘焙缓存也共用；Claude 侧的施工在临时工作树里（WF-FLOW §3.1）。git 与推送见 WF-RULE §7，防打架见 WF-FLOW §2，共享文件谁能改见 §1。引擎路径从环境变量 `UESTC_GODOT` 读命令行版 exe，门禁比对 `--version` 与 `tools/godot-version.txt`。

1. **一次只跑一个**：`dotnet build`、导入（`--build-import`）、烘焙、`gate.ps1`、截图和基准同一时刻只许一个在跑，用 `scripts\` 里那把锁（容量 1）排队。拿不到就等，不绕过：不另起进程抢跑，不删锁文件，不为赶时间并行。施工子代理默认不编译，构建由指挥在一批交回后统一跑（WF-FLOW §3.1）。
2. **只走命令行**：不在编辑器里打开或重存场景，场景由代码生成；导入日志里不得有 `ERROR` 或 `SCRIPT ERROR`（G1）。截图类和性能类门禁要在交互式会话的窗口模式下跑。测量、验证一律后台跑：用命令行启动、读日志里的时间戳和结果，不用 computer use 去点击、截屏或操作作者的桌面。
3. **先选检查，再决定烘不烘**：`gate.ps1` 先按改动选出要跑的检查（测试节奏以 ENG-GATES §1 为准），再按所选检查的需要补烘共享缓存；快检不烘焙。
4. **归属与 `.uid`**：改动文件必须在文档文件头的 `paths` 中有归属（G3），施工只改简报允许的路径；`.uid` 跟着源文件走、一起提交，复制文件时删掉复制来的 `.uid`。仓库的换行、二进制与忽略规则以 `.gitattributes`、`.gitignore` 为准。
