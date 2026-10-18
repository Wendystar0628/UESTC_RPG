---
id: WF-BUILD
title: 施工边界
category: workflow
summary: 施工时能改哪里、共享文件、契约申请与冻结、构建与测试排队、新模块怎么接入、Godot API 先查签名，以及禁用写法门禁拦不住的两条约定
keywords: [独占目录, 共享文件, 契约申请, 契约冻结, 构建锁, 构建与测试排队, .uid, 特性接入, 穷举 switch, 几何不随时季天气变, GDScript, uid, signatures.txt, Godot API]
owners: [WP-00]
contributors: []
depends: [WF-RULE, CORE-INDEX, ENG-ARCH]
related: [WF-ENTRY, WF-FLOW, WF-STATUS, ENG-WP, CORE-CONVENTIONS, ENG-GATES]
contracts: []
data: ["ownership.json"]
gates: [G3, G4]
paths: ["game/**/*.cs", "game/**/*.gdshader*"]
---

# WF-BUILD · 施工边界

> **范围**：本文规定施工子代理写代码和做内容时的硬边界：能改哪些文件，哪些要提申请，契约怎么申请与冻结，构建与测试怎么排队，新模块怎么接入。
> **不在本文**：代码边界与禁用写法：原则见 ENG-ARCH §2，清单以各程序集的 `BannedSymbols.txt` 和 G4 为准；契约的内容：`game/contracts/` 源码与 ENG-ARCH §3；冻结状态：WF-STATUS §1；目录与归属明细：`ownership.json`；共享文件清单：本文 §1；内容与画面的底线：CORE-INDEX §0；全员守则：WF-RULE。
> **查阅**：先读 §0；改哪里与申请看 §1；G4 拦不住的约定看 §2；契约申请与冻结看 §3；构建与测试排队看 §4。

## 0 结论

1. **只改本包的独占目录**（§1）：独占目录见 ENG-WP-LIST §1 与 `ownership.json`，越界由门禁 G3 拒绝。
2. **共享文件不自己改**（§1）：工程共享件由 Codex 主线改，设计文档由主会话改；要改契约、全局着色器参数、着色器文件、程序集引用或 NuGet 包，写契约申请。
3. **新模块用特性接入**（§1）：不改注册表、不改主场景、不加 Autoload。
4. **代码边界和禁用写法的权威是 ENG-ARCH §2**，由门禁 G4 拦截；G4 拦不住的只有 §2 的两条。
5. **契约申请要早、冻结分批**（§3，D-134）：写 `requests/contract-<队列行号>-<主题>.md` 交 Codex 主线；已冻结的组只增不改。
6. **构建、导入、烘焙、门禁、截图一次只跑一个**（§4，D-211）：经构建锁排队，不绕过。

### 0.1 接入要点

- **新增契约、全局着色器参数、着色器文件、程序集引用、NuGet 包**：不自己加，写 `requests/contract-<队列行号>-<主题>.md` 交 Codex 主线（§3）；要早提。
- **要改的路径不在本包独占目录里**：不改，写进回报或契约申请。
- **写 gen 或烘焙子任务**：随机只用契约的 `Rng` 与 `StableHash`，同机同输入同输出即可（ENG-ARCH §2，D-232）。
- **跑构建、导入、烘焙、门禁或截图**：经构建锁排队，不绕过；只走命令行，不在编辑器里重存场景（§4）。
- **写 Godot 代码前**：先读 `docs/godot-api/README.md`，用到的类、方法、属性都先在 `docs/godot-api/signatures.txt` 查到签名再写（D-145），着色语言查 `docs/godot-api/shaders/README.md`。

## 1 改哪里

- **只改本包的独占目录**：见 ENG-WP-LIST §1 的"独占目录"一列和 `ownership.json`；子包独占原包目录下的一个子目录（ENG-WP-LIST §2）。这是"同一批文件同一时间只有一个写者"的分工依据（WF-FLOW §2.1）。
- **共享文件不自己改**：工程共享件（契约、csproj、`Directory.*.props`、`project.godot`、`ownership.json`、`gate.ps1`、`scripts/` 下的工程脚本等）由 Codex 主线直接提交推送；`docs/` 由主会话改。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请交 Codex 主线（§3）。
- **新模块用特性接入**：接入特性的清单见 ENG-ARCH §2；不改注册表、不改主场景、不加 Autoload。
- **场景与路径**：不在编辑器里打开或重存场景，场景尽量由代码生成；代码里只用 `res://` 路径，不手写 `uid://`；`.uid` 的处理见 §4；不写 GDScript。

## 2 G4 拦不住的约定

- **几何不随时季和天气变**：只走调色板、灯光通道、变体掩码和天气向量（SYS-TIME §0.1、SYS-WEATHER §0.1）。
- **规则集与枚举**：观光专属常量读 `IRuleset`；会追加值的枚举写穷举 `switch`，`default` 报错（ENG-ROADMAP §3）。

## 3 契约申请与冻结

- **申请**：任何代理要改契约或共享工程文件，写 `requests/contract-<队列行号>-<主题>.md`（短头部见 WF-COMMS §3.3）。Codex 主线处理后在文件末尾写结论和对应的提交。
- **提交**：Codex 主线在主检出直接提交推送；改共享件前告知邻居，改完同批迁完使用方（WF-RULE §10）。
- **冻结**：契约分批冻结：某组契约的真实生产方与使用方在主线上接通、相关核心专项通过后，由主会话宣布冻结这一组，之后只增不改；没冻结的组按申请修改；冻结状态见 WF-STATUS（D-134）。
- **要早**：上游包开工后头几个小时内就把契约申请提上来，免得下游写得太深再返工。

## 4 构建与测试排队

所有代理共用主检出 `E:\Project\UESTC_RPG`，`bin/`、`obj/`、`.godot/` 和烘焙缓存也共用。git 与回滚见 WF-RULE §7，防打架见 WF-FLOW §2，共享文件谁能改见 §1。引擎路径从环境变量 `UESTC_GODOT` 读命令行版 exe，门禁比对 `--version` 与 `tools/godot-version.txt`。

1. **一次只跑一个**：`dotnet build`、`--headless --import`、烘焙、`gate.ps1`、截图和基准同一时刻只许一个在跑，用 `scripts\` 里那把锁（容量 1）排队。拿不到就等，不绕过：不另起进程抢跑，不删锁文件，不为赶时间并行。施工子代理默认不编译，构建由指挥在一批交回后统一跑（WF-FLOW §3.1）。
2. **只走命令行**：不在编辑器里打开或重存场景，场景由代码生成；导入日志里不得有 `ERROR` 或 `SCRIPT ERROR`（G1）。截图类和性能类门禁要在交互式会话的窗口模式下跑。
3. **先选检查，再决定烘不烘**：`gate.ps1` 先按改动选出要跑的检查（测试节奏以 ENG-GATES §1 为准），再按所选检查的需要补烘共享缓存；快检不烘焙。
4. **归属与 `.uid`**：改动路径必须在 `ownership.json` 划给本包的范围内（G3）；`.uid` 跟着源文件走、一起提交，复制文件时删掉复制来的 `.uid`。仓库的换行、二进制与忽略规则以 `.gitattributes`、`.gitignore` 为准；`game/override.cfg` 让开发态用独立的用户目录，不进 git。
