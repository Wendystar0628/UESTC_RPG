---
id: WF-BUILD
title: 施工边界与内容底线
category: workflow
summary: 施工时能改哪里、共享文件与契约申请、代码边界与接入方式、禁用写法，以及内容与画面的底线
keywords: [独占目录, 共享文件, 契约申请, 特性接入, ContentModule, Part, KernelNode, SaveSection, 代码边界, game/gen 纯 .NET, IRenderHost, ToonSpec, 禁用写法, System.Random, StableHash, DetMath, StandardMaterial3D, 色名, 视觉精度, 精度区, 内容底线, INV-4, INV-7, INV-11, GDScript, uid]
owners: [WP-00]
contributors: []
depends: [WF-INDEX, WF-RULE, CORE-INDEX, ENG-ARCH]
related: [WF-ONBOARD, WF-FLOW, ENG-WP, CORE-CONVENTIONS, ART-FIDELITY, ART-STYLE]
contracts: []
data: ["ownership.json"]
gates: [G3, G4]
---

# WF-BUILD · 施工边界与内容底线

> **范围**：本文规定施工子代理在写代码和做内容时的硬边界：能改哪些文件，哪些要提申请，代码怎么接入、不能怎么写，以及内容与画面的底线。每条只留一行结论，细则在 ENG-ARCH §2 和各系统文档的接入要点。
> **不在本文**：契约的内容与冻结状态：ENG-ARCH §3；目录与归属明细：`ownership.json`；共享文件清单：CORE-CONVENTIONS §5.1；全员守则：WF-RULE。
> **查阅**：先读 §0；改哪里与申请看 §1；代码边界看 §2；禁用写法看 §3；内容与画面底线看 §4。
> **关联**：[WF-INDEX 协作总纲](../WF-INDEX-协作总纲.md)、[WF-RULE 全员守则](WF-RULE-全员守则.md)、[WF-ONBOARD 子代理开工手册](../WF-ONBOARD-子代理开工手册.md)、[WF-FLOW 开发流程](../process/WF-FLOW-开发流程.md)、[CORE-INDEX 总索引](../../README.md)、[ENG-ARCH 工程架构](../../engineering/ENG-ARCH-工程架构.md)、[ENG-WP 工作包](../../engineering/ENG-WP-工作包派工单.md)、[CORE-CONVENTIONS 约定](../../core/CORE-CONVENTIONS-约定.md)、[ART-FIDELITY 视觉精度标准](../../art/ART-FIDELITY-视觉精度标准.md)、[ART-STYLE 风格基础](../../art/ART-STYLE-风格基础.md)

## 0 结论

1. **只改本包的独占目录**（§1）：独占目录见 ENG-WP §2 与 `ownership.json`，越界由门禁 G3 拒绝。
2. **共享文件不自己改**（§1）：工程共享件由 Astra 改，设计文档由主会话改；要改契约、全局着色器参数、着色器文件、程序集引用或 NuGet 包，写契约申请。
3. **新模块用特性接入**（§1）：不改注册表、不改主场景、不加 Autoload。
4. **代码边界**（§2）：gen 是纯 .NET，显示经 `IRenderHost`，声音、存档、画面设置、天气、地图数据、观者与镜头各走自己的接口。
5. **禁用写法由门禁 G4 拦截**（§3）：随机、哈希、计时、材质与着色器、十六进制色值、gen 里的超越函数。
6. **内容与画面有底线**（§4）：视觉精度底线处处满足，画风清透干净，任何时刻、季节、天气都要好看，全局不变量不破。

### 0.1 接入要点

- **新增契约、全局着色器参数、着色器文件、程序集引用、NuGet 包**：不自己加，写 `requests/contract-<包号>-<主题>.md` 交 Astra（WF-FLOW §5）。
- **要改的路径不在本包独占目录里**：不改，写进回报或契约申请（§1）。
- **新模块**：用 §1 列出的特性接入，不碰注册表（§1）。
- **写 gen 或烘焙子任务**：只用 `DetMath` 的超越函数，随机只用契约的 `Rng` 与 `StableHash`（§3）。

## 1 改哪里

- **只改本包的独占目录**：见 ENG-WP §2 的"独占目录"一列和 `ownership.json`；子包各自独占原包目录下的一个子目录（ENG-WP §3）。
- **共享文件不自己改**：清单见 CORE-CONVENTIONS §5.1。工程共享件（契约、全部 csproj、`Directory.*.props`、`project.godot`、`ownership.json`、`gate.ps1`、`scripts/wt/` 等）由 Astra 改、经主会话合入；`docs/design/` 由主会话改。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请（`.collab/requests/contract-*.md`）交 Astra。契约分批冻结，已冻结的组只增不改（ENG-ARCH §3，D-134）。
- **新模块用特性接入**：`[ContentModule]`、`[Part]`、`[KernelNode]`、`[KernelService]`、`[SaveSection]`、`[AmbientEmitter]`、`[InteriorProvider]`、`[MediumPresenter]`、`[QualityResponsive]`；不改注册表、不改主场景、不加 Autoload（ENG-ARCH §2）。
- **场景与路径**：不在编辑器里打开或重存场景，场景尽量由代码生成；代码里只用 `res://` 路径，不手写 `uid://`；新文件的 `.uid` 由 `--headless --import` 生成，跟源文件一起提交，复制文件时删掉复制来的 `.uid`；不写 GDScript。

## 2 代码边界与接入方式

细则见 ENG-ARCH §2 和各系统文档 §0 末尾的接入要点，这里每条只留一行：
- **`game/gen` 是纯 .NET**：只引用契约、部件和世界模型（只读），不引用 GodotSharp、不读文件、不读世界状态、不感知天气和画面设置；本机烘焙结果必须与参考烘焙逐字节一致（D-165）。
- **显示**：一律经内核的 `IRenderHost`，材质只经 `ToonSpec` 取得。
- **时间、季节、天气**：几何不随时季和天气变，只走调色板、灯光通道、变体掩码和天气向量；天气只读 `IWorldState.WeatherNow` 与天气类全局参数；`IWorldState.Instance` 一变，做平滑或留历史的直接落到新值（SYS-TIME §0.1、SYS-WEATHER §0.1）。
- **画面设置**：`[QualityResponsive]` 声明、`IQualityAware.ApplyQuality()` 接收，风格底线任何画质下都不能关（SYS-GRAPHICS §0.1）。
- **按图的数据**：从 `IMapContext` 取，不写死数据路径、实体 ID 和地图 ID（SYS-MAPS §0.1）。
- **观者与镜头**：观者在哪读 `IObserverState`，画面从哪看读 `ICameraRig`（SYS-VIEW §0.1）。
- **存档**：跨读档的状态用 `[SaveSection]`，字段增减要升版本并写迁移，不自己读写 `user://`；任何代码都不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`（SYS-SAVE §0.1）。
- **声音**：只经 `IAudioService`、`IAmbienceService`、`IFoleyService`，随机只用契约里的 `Rng`（SYS-AUDIO §0.1）。
- **室内**：只有 WLD-INTERIORS §2 开放清单里的楼能走进去（WLD-INTERIORS §0.1）。
- **规则集与枚举**：观光专属常量读 `IRuleset`；会追加值的枚举写穷举 `switch`，`default` 报错（ENG-ROADMAP §6）。

## 3 禁用写法

完整清单和少数例外见 ENG-ARCH §2，由门禁 G4 检查。最容易踩的几条：
- **随机与哈希**：不用 `System.Random`、`GD.Randf`、`RandomNumberGenerator`，一律用契约里的带种子生成器；不拿 `string.GetHashCode` 当种子，改用 `StableHash`；不依赖 `Dictionary`、`HashSet` 的遍历顺序。
- **计时**：不用 `DateTime.Now`、`Stopwatch`、`Timer`，只有内核能计时；着色器里不用 `TIME`，改用全局参数 `anim_time`。
- **材质与着色器**：不用 `StandardMaterial3D`，不自己 new `ShaderMaterial`；不写十六进制色值，一律用色名。
- **gen 与烘焙子任务里**：不用 `Math`、`MathF` 的超越函数，改用 `DetMath`。

## 4 内容与画面的底线

- **视觉精度分两层**（ART-FIDELITY §0，D-009、D-176）：底线是任何位置、任何距离看都不粗糙（比例对，有倒角和收边，没有光秃的拉伸体块）；细节密度按精度区 K1–K5 分级，表面属于哪个区读导出的精度区数据，不自己判断。去粗糙靠最低合格构件、自定义法线和描线（ART-MODELING §7），不靠堆细节；底线不拿来换预算，细节密度在本包额度内按区取舍（ENG-BUDGET §8）。
- **共同口号**："走进剧场版动画的校园，清透、敦实、干净、有生活。"建模时拿不准风格取舍，回到这句话（ART-MODELING §6）。造型与质感拿不准时，看 ART-STYLE §1 的参考场景怎么做（读代码，不只看图）。
- **画风**（ART-STYLE）：清透干净的三渲二，不做旧、不脏，影子以明度为主、按材质推导，细描线随底色染色。
- **任何时刻、季节、天气都要好看**：四季、昼夜和天气一律视觉效果优先，真实情况只作参考；时间只能往前走。
- **全局不变量**（CORE-INDEX §3）：画面与展陈里不出现人物或人物肖像；场景里的字一律简体中文（INV-4）；日式元素只放开日常街景物件（INV-7、ART-AMBIENCE）；关着的楼只做外壳、窗卡和简化室内（INV-11）。
- **作品定位**：不是地图复刻。主要建筑、道路、设施的大体位置和形状要对，其余可以风格化，也要主动做设计增补（MAP-QSH-ADDITIONS）；照片和 OSM 只用来定结构，材质、色彩、陈设和标识按氛围做艺术化处理；增删都写进覆盖层并写明理由（ENG-ARCH §3）。
