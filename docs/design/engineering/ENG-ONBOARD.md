---
id: ENG-ONBOARD
title: 子代理开工手册
category: engineering
summary: 子代理开工时唯一要通读的文档：开工环境、硬规则、查资料的方法与路由、完成与回报
keywords: [开工手册, 子代理, 必读, 查阅, 按需查, 信息获取, 小节目录, SECTIONS.md, 接入要点, 硬规则, 禁用写法, 工作树, 回报, CLAUDE.md, AGENTS.md]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX, CORE-CONVENTIONS, ENG-ARCH, ENG-WP]
related: [ENG-GATES, ENG-STATUS, ENG-BUDGET, CORE-DECISIONS, CORE-GLOSSARY]
contracts: []
data: []
gates: []
---

# ENG-ONBOARD · 子代理开工手册

> **范围**：本文是每个子代理开工时唯一要从头读到尾的文档，内容包括：开工环境、必须遵守的硬规则、怎么查其余文档，以及做完以后怎么自检和回报。仓库根的 `CLAUDE.md` 会引入本文，所以多数情况下它已经自动在你的上下文里。
> **不在本文**：
> - 每个包具体做什么：ENG-WP §2、§2.1；
> - 各系统的设计细则：各自的文档；
> - 规则的权威原文：禁用写法以 ENG-ARCH §2 为准，测试与回报以 ENG-GATES 为准，文件与文档约定以 CORE-CONVENTIONS 为准。本文只做汇总，跟原文不一致时以原文为准，并报告主代理。
> **查阅**：先读 §0；环境与 git 看 §2；能改什么、不能写什么看 §3；找资料看 §4；做完怎么交看 §5。
> **关联**：[CORE-INDEX 总索引](../README.md)、[CORE-CONVENTIONS 约定](../core/CORE-CONVENTIONS.md)、[ENG-ARCH 工程架构](ENG-ARCH.md)、[ENG-WP 工作包](ENG-WP.md)、[ENG-GATES 验收门禁](ENG-GATES.md)、[ENG-STATUS 施工状态](ENG-STATUS.md)

## 0 结论

1. **开工只读三样**：本文、ENG-WP §2 主表与 ENG-WP §2.1 里本包的两行，以及派工简报。其余文档需要时再查，不要求开工前通读（D-178）。
2. **硬规则必须遵守**（§3）：
   - 只改本包的独占目录；
   - 共享文件和契约要改，就提申请；
   - 模块用特性接入；
   - 禁用写法由门禁 G4 拦截。
   - 贴近主线：开工先看最近的合入，只往自己的文件里加，测试只锁本包输出，改了别人依赖的东西默认向后兼容，小步交付（§3.6）。
   - 不过度工程化：不写证据溯源、哈希校验，不自己加门禁和检查（§3.7）。
3. **主线优先**：主线闸口通过之前，只交主线版，也就是能接进主线、跑得通、不崩、不挡路（§1，D-168）。
4. **怎么查资料**（§4）：
   - 用小节目录 `docs/design/SECTIONS.md` 定位到具体小节，按行号只读需要的那一节；
   - 碰到别的系统，先看那份文档 §0 末尾的"接入要点"；
   - §4.2 列出了"要做什么事，先去哪里查"。
5. **Godot API 不凭记忆写**：先在 `docs/godot-api/signatures.txt` 查到签名再用（§4.3，D-145）。
6. **拿不准就问**：会改动契约、数据格式或目录归属的，停下来问主代理；不影响这些的，先按最保守的理解做，并在回报里写明你的假设（§4.4）。
7. **做完以后**：跑 `scripts\gate.ps1 -Wp <WP-xx>`，截几张图，再按 ENG-GATES §5 回报（§5）。

### 0.1 接入要点

本文只约束子代理的工作方式，没有对其他包的接口要求。新的规范文档要按 §6 写，才能被 §4 的查法找到。

## 1 项目与当前阶段

- **作品**：《电光年华》，校园题材的独立 RPG。首张地图是成电清水河校区，画风是三渲二的日式动画背景。
- **技术栈**：Godot 4.7.2-stable .NET 版，只做 PC（Windows）。代码全部用 C#（net10.0，C# 14，D-177），着色器用 Godot 着色语言，不写 GDScript。
- **当前阶段**：世界与观光模式，具体范围见 CORE-INDEX §1、§2。
- **主线优先**（CORE-CONVENTIONS §8.6，D-168）：
  - 主线闸口之前，每个包只交主线版；
  - 主线版可以用简化几何、默认参数、占位贴图和占位声音，边角情况可以跳过；
  - 但不能改动或绕开契约、数据格式和目录归属，也不能留临时接口；
  - 本包的主线版要看到什么、必须接通谁，见 ENG-WP §2.1 你那一行。
- **作品定位**：这不是地图复刻。
  - 主要建筑、道路、设施的大体位置和形状要对，其余可以风格化，也要主动做设计增补（MAP-QSH-ADDITIONS）。
  - 照片和 OSM 只用来定结构：体量、层数、平面、入口、交通核、道路、水体。
  - 材质、色彩、陈设和标识按氛围做艺术化处理。现实里没有的元素可以加，难看的元素可以删；增删都写进覆盖层，并写明理由（ENG-ARCH §3）。

## 2 开工环境

- **路径**：项目在 `E:\Project\UESTC_RPG`，你的工作树在 `..\UESTC_RPG.wt\<WP-xx>`。
- **git**（ENG-ARCH §6，D-166）：
  - 分支是本地临时分支 `wp/<xx>-<slug>`：只在上面小步提交，不推送、不合并、不碰 `main`；
  - 主代理通知 `main` 有新合入时，执行 `git rebase main`；
  - 远端只有 `main` 和标签；
  - 只有一个代理写代码时，直接在主检出的 `main` 上做。
- **引擎**：命令行版引擎的路径从环境变量 `UESTC_GODOT` 读，默认是 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。
- **工作树配置**：工作树里的 `game\override.cfg` 已经把 `user://` 指到 `UESTC_<WP-xx>`。不要改它，也不要提交它。
- **当前施工状态**：看 ENG-STATUS，里面有哪些包在做、各自做到哪一步。
- **多模型协作**（ENG-TEAM，D-179）：
  - 主会话的 Opus 是唯一的主代理，管画面、风格、内容与判断；Astra 是工程负责人，管架构、契约与共享工程文件；二号会话开子代理承担 Opus 侧包的施工；值班执行者（只改文档的合并交 luna，其余与巡检交 Grok，没回复退回 Sol，D-198）做合并执行、施工状态与巡检，Sol 旁路会话做调研（ENG-TEAM-ROLES §1、ENG-TEAM-COMMS §5）。
  - 你向派你的一方回报：二号会话派的子代理找二号会话，Astra 派的找 Astra 主线程，主会话派的找主会话（ENG-TEAM §0.1）。
  - 单 Claude 账户模式下（ENG-TEAM-QUOTA §4），Claude 侧的子代理都由主会话派，向主会话回报。
  - 合并请求、契约申请、任务单和合并日志都在 `E:\Project\UESTC_RPG.wt\.collab\`（ENG-TEAM-COMMS §1）。开始下一步之前看一眼 `merged.log`，`main` 变了就 rebase。

## 3 硬规则

### 3.1 改哪里

- **只改本包的独占目录**：见 ENG-WP §2 的"独占目录"一列和 `ownership.json`。越界会被门禁 G3 拒绝。
- **共享文件不自己改**：包括 `project.godot`、全部 csproj、`Directory.*.props`、契约等，清单见 CORE-CONVENTIONS §5.1。它们由 Astra 改、经主代理合入（ENG-TEAM-FLOW §2.7）。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请（`.collab/requests/contract-*.md`），由 Astra 处理（ENG-TEAM-FLOW §2.7）。契约分批冻结：已冻结的组只增不改（ENG-ARCH §3，D-134）。
- **新模块用特性接入**：`[ContentModule]`、`[Part]`、`[KernelNode]`、`[KernelService]`、`[SaveSection]`、`[AmbientEmitter]`、`[InteriorProvider]`、`[MediumPresenter]`、`[QualityResponsive]`。不改注册表，不改主场景，不加 Autoload（ENG-ARCH §2）。
- **场景与路径**：
  - 不在编辑器里打开或重存场景，场景尽量由代码生成；
  - 代码里只用 `res://` 路径，不手写 `uid://`；
  - 新文件的 `.uid` 由 `--headless --import` 生成，跟源文件一起提交。

### 3.2 代码边界与接入方式

细则以 ENG-ARCH §2 和各系统文档的接入要点为准。

- **`game/gen` 是纯 .NET**：
  - 只引用契约、部件和世界模型（只读）；
  - 不引用 GodotSharp，不读文件，不读世界状态，不感知天气和画面设置；
  - 它和烘焙子任务的代码在玩家机器上由 `IBakeService` 本机烘焙，结果必须与参考烘焙逐字节一致（D-165）。
- **显示**：一律经内核的 `IRenderHost`；材质只经 `ToonSpec` 取得。
- **时间、季节、天气**：
  - 几何不随时季和天气变化，变化只走调色板、灯光通道、变体掩码和天气向量（SYS-TIME、SYS-WEATHER）；
  - 天气只读 `IWorldState.WeatherNow` 和天气类全局参数（SYS-WEATHER §8），不自己判断天气，不写计时器；
  - 做平滑、带回差或保留历史的，一旦 `IWorldState.Instance` 变了，直接落到新值。
- **画面设置**：在 `[KernelNode]` 上用 `[QualityResponsive]` 声明负责和只读的单项，经 `IQualityAware.ApplyQuality()` 接收（SYS-GRAPHICS §7），不读任何设置文件。风格底线在任何画质下都不能关（SYS-GRAPHICS §2）。
- **按图的数据**：一律从 `IMapContext` 取，用 `DataRoot` 拼路径。不写死数据路径、实体 ID 和地图 ID；全图纹理按 `map_rect` 采样；换图时要换数据的，实现 `IMapAware`（SYS-MAPS §7）。
- **"观者在哪"和"画面从哪看"要分开读**：
  - 观者在哪（小地图箭头、所在区域、解锁判定、进楼触发）读 `IObserverState`；
  - 画面从哪看（视锥、听者、流式选级）读 `ICameraRig`。
- **存档**：要跨读档保留的状态，用 `[SaveSection]` 声明（SYS-SAVE §2、§10）。
  - 字段有任何增减，都要升版本并写迁移函数；
  - 不自己读写 `user://`，不用 `Resource` 或 `ConfigFile` 存状态。
- **声音**：
  - 出声只经 `IAudioService`、`IAmbienceService` 或 `IFoleyService`，不自己建 `AudioStreamPlayer*`；
  - 碰撞体要填 `Foot` 材质，有声的部件实现 `Sounds()`（SYS-AUDIO §4.2、§9.3）；
  - 声音的随机只用契约里的 `Rng`。
- **室内**：只有 WLD-INTERIORS §2 的开放清单能走进去。门的规则见 WLD-INTERIORS §4.1；要新开一栋楼，按 WLD-INTERIORS §14 做，不改契约。
- **规则集与枚举**：
  - 观光专属的常量（解锁策略名、偶发事件 ID、允许的观察模式）不写进代码，一律读规则集 `IRuleset`（ENG-ROADMAP §6）；
  - 会追加值的枚举（`ViewMode`、`AppState`、`TransitKind`、`VeilReason` 等）一律写穷举的 `switch`，`default` 分支报错，不写二值判断。

### 3.3 禁用写法

由门禁 G4 检查。完整清单和少数例外（例如材质库可以建 `ShaderMaterial`，内核可以计时和建实例）以 ENG-ARCH §2 为准。

- **随机与哈希**：
  - 不用 `System.Random`、`GD.Randf`、`GD.Randi`、`RandomNumberGenerator`，随机数一律用契约里的带种子生成器；
  - 不拿 `string.GetHashCode` 当种子，改用 `StableHash`；
  - 不依赖 `Dictionary`、`HashSet` 的遍历顺序；
  - 不用 `Guid.NewGuid`，新 ID 用递增整数。
- **计时**：
  - 不用 `DateTime.Now`、`Stopwatch`、`Timer`、`Time.GetTicks*`、`Time.GetUnixTimeFromSystem`，只有内核能计时；
  - 不给 `Engine.TimeScale` 赋值；
  - 着色器里不用 `TIME`，改用全局参数 `anim_time`。
- **材质与着色器**：
  - 不用 `StandardMaterial3D`，不自己 new `ShaderMaterial` 或 `Shader`，不在运行时改 `render_mode` 或 `#define`；
  - 不用白名单以外的着色器文件；
  - 不写十六进制色值，一律用色名；
  - `vertex()` 里不用 `CAMERA_POSITION_WORLD` 做距离判断，改用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`。
- **实例与几何**：不在内核以外创建 `MeshInstance3D`、`MultiMeshInstance3D` 或 RenderingServer 实例；不在主线程生成几何；不从脚本读回全局着色器参数。
- **gen 与烘焙子任务里**：
  - 不用 `Math`、`MathF` 的超越函数，一律改用 `DetMath`；
  - 不用内部走三角函数的 System.Numerics 构造，改用 `DetMath` 里的同名工具；
  - 不用 `Vector<T>`、`System.Runtime.Intrinsics`、`Math.FusedMultiplyAdd`、`TensorPrimitives`。
- **文件与存档**：
  - 不用 `ResourceLoader`、`ResourceSaver` 读写 `user://`，没有例外；
  - 存档代码里不用 `ConfigFile`、`GD.StrToVar`、`GD.BytesToVar*`、`FileAccess.GetVar`。
- **字面量**：通用代码里不写实体 ID、地图 ID 或 `res://data/maps/` 路径的字面量。
- **音频**：不用 `AudioStreamRandomizer`；不给 `AudioStreamPlaylist.Shuffle` 或 `AudioServer.PlaybackSpeedScale` 赋值。

### 3.4 内容与画面的底线

- **视觉精度分两层**（ART-FIDELITY §0，D-009、D-176）：
  - 底线：任何位置、任何距离看都不粗糙。比例对，有倒角和收边，没有光秃的拉伸体块；
  - 细节密度：按精度区 K1–K5 分级。每个表面属于哪个区，读导出的精度区数据，不自己判断；
  - 去粗糙靠最低合格构件、自定义法线和描线（ART-MODELING §7），不靠堆细节；
  - 底线不拿来换预算；细节密度在本包额度内按区取舍。本包额度见 ENG-BUDGET §8。
- **一句共同口号**："走进剧场版动画的校园，清透、敦实、干净、有生活。"建模时拿不准风格取舍，回到这句话判断（ART-MODELING §6）。
- **画风**（ART-STYLE）：清透干净的三渲二。影子以明度为主、按材质推导；细描线随底色染色。
- **任何时刻、季节、天气都要好看**：四季、昼夜和天气一律视觉效果优先，真实情况只作参考；时间只能往前走。
- **全局不变量**（CORE-INDEX §3）：
  - 画面里不出现人物，展陈里也不出现人物肖像；
  - 场景里的字一律用简体中文（INV-4）；
  - 日式元素只放开日常街景物件（INV-7、ART-AMBIENCE）；
  - 关着的楼只做外壳、窗卡和简化室内（INV-11）。

### 3.5 文件操作

细则见 CORE-CONVENTIONS §8.4。

- **下载**：需要的东西直接下载，只用官方或可信来源，下完说明下了什么、多大、放在哪。
- **第三方许可**：第三方代码只收 MIT、Apache、BSD、ISC、OFL 许可，并登记进 `third_party.json`；音频素材登记进 `licenses.json`。
- **临时文件**：放会话临时目录，或仓库里不进 git 的 `output/`，不放进源码和文档目录。
- **删除**：能不删就不删。要删时只用绝对路径、只删具体文件，不用通配符或相对路径，不写 `cd X; … rm …` 这种组合。
- **路径与格式**：一律用绝对路径，命令行参数里的路径也一样；新建的文本文件用 UTF-8 和 LF 换行。
- **范围**：只改分派给自己的文件。

### 3.6 贴近主线，少给合并添活

施工分支离 `main` 越远、碰的公共文件越多，合并时的冲突和返工就越多。下面几条每个包都要守（ENG-TEAM-FLOW §2.10，D-199）：

- **开工和被恢复时先看主线变了什么**：读 `E:\Project\UESTC_RPG.wt\.collab\merged.log` 最近 24 小时的合入，挑出和本包相关的（同一个数据文件、你调用的接口、你叠在上面的候选），先 `git rebase main` 再动手。
- **只往自己的文件里加**：覆盖层、种植表这类多包共用的数据，只加本包的条目，不改别包的条目；覆盖层拆成按包分文件以后，只写本包那一份。改动尽量落在本包的独占目录。
- **生成文件不手改**：CORE-INDEX §7–§9（`docs/design/README.md`）与 `docs/design/SECTIONS.md` 由 `scripts/docs/gen_index.py` 生成；冲突时取任一边后重跑脚本，不手工合并。决策编号（D-nnn）由主会话统一分配，子代理不往 CORE-DECISIONS 里加行，需要就在回报里提。
- **测试只锁本包自己的输出**：可以锁本包的产物和"原有条目的字节与顺序不变"，不锁别的包会合法增减的全局集合（例如全校设计树的整套指纹、全校楼的总数）。别的包按规矩加了东西，你的测试不该因此失败。
- **改了别人依赖的东西，默认向后兼容**：数据字段、接口语义、几何口径一旦有下游在用，就只增不改：加新字段或新入口，旧行为保持不变，下游按自己的节奏迁移（共享边界的"立面轮廓"就是这样）。交候选前跑全仓 G2，回报里列出受影响的下游包和测试。
- **小步交付**：一张单里做到一块能独立合入的程度，就先交一个候选，不攒到最后一起交。叠在没合入的候选上最多一层；交候选前 `git rebase main`，再用 `git merge-tree` 对最新 `main` 预演一次，有冲突先自己解掉。
- **评审结论以规范文档为准**：评审里定下的通用规则会写回规范文档，开工时按文档做，不只看自己这张单的评审。

### 3.7 不过度工程化

本项目是个人实验作品，不是商业交付级项目（ENG-TEAM-ROLES §2.2a，D-201）：
- 不写证据溯源、来源链、哈希或校验和、proof/verification 一类的脚本与报告，不建 handoff 证据目录。
- 不自己加门禁、检查脚本、测试脚手架或全量回归；只跑任务单要求的检查，默认是 `gate.ps1` 的快检。
- 回报照 ENG-GATES §5，证据只给路径；写给主会话的来信开头标【需定】【待审】【知会】，一条一件事、5 行以内（ENG-TEAM-COMMS §4）。

## 4 查资料

### 4.1 文档在哪、怎么找

- **总入口**：文档都在 `docs/design/`。总索引是 CORE-INDEX（`docs/design/README.md`），其中 CORE-INDEX §7 是文档地图，每份文档一行，写着它规定什么、由哪个包主责。
- **小节目录 `docs/design/SECTIONS.md`**：每份文档的每一节占一行，格式是 `编码 §号 标题 · L行号 · 字数`。
  - 查法：先在它里面 grep 关键词或编码，再用行号只读原文的那一段（Read 的 offset、limit）；
  - 不要整份读大文档：ENG-ARCH、ENG-PERF、SYS-AUDIO 都在 10 万字以上；
  - 它由脚本生成，行号可能比原文落后几行，以标题文字为准。
- **每份规范文档的结构都一样**：
  - 文件头的 `summary`、`keywords`、`contracts`、`data`；
  - 范围块里的"查阅"行，告诉你什么内容在哪一节；
  - §0 结论；
  - §0 最后一个小节"接入要点"，写的是别的包碰到这个系统时必须守的规则。
  - 判断一份文档和你有没有关系，看 `summary` 和接入要点就够了。
- **可以直接 grep 的编码**：文档编码、`§`、`INV-n`、`D-nnn`、`G1`–`G21`、`B1`–`B13`、`K1`–`K5`、`WP-xx`、契约名与类型名。跨文档引用的写法是"SYS-WEATHER §6.4"。
- **专门的查询文档**：
  - 术语不懂，查 CORE-GLOSSARY；
  - 想知道一条规则为什么这样、现在是否还生效，按 D 编号查 CORE-DECISIONS；
  - 坐标、单位、命名、目录、帧顺序，查 CORE-CONVENTIONS。
- **契约**：D0 之后以 `game/contracts/` 的源码为准，设计说明在 ENG-ARCH §3。

### 4.2 按要做的事查哪里

下表只列最常见的起点；每份文档范围块的"查阅"行和 §0 的接入要点会再往下指。

**工程与流程**

| 要做或要查的事 | 先看 |
| --- | --- |
| 本包做什么、交什么、依赖谁、主线版做到哪 | ENG-WP §2、§2.1 本包的两行 |
| 契约签名，或申请改契约 | ENG-ARCH §3；D0 之后看 `game/contracts/` |
| 路径归谁、能引用哪个程序集、模块怎么被注册、禁用写法的完整清单 | ENG-ARCH §2 |
| 依赖的包还没交付时用什么桩 | ENG-ARCH §5 |
| 工作树、rebase、合并 | ENG-ARCH §6 |
| 加贴图、模型、字体、音频；本机烘焙与缓存键 | ENG-ARCH §4 |
| 本包的额度、超额怎么办 | ENG-BUDGET §0.1、§8 |
| 合入前核对性能硬规则 | ENG-BUDGET §7 |
| 该跑哪档测试、`gate.ps1` 怎么用 | ENG-GATES §2 |
| 回报格式、截图放哪 | ENG-GATES §5 |
| 截图默认串、命令行参数、审查视角的位姿 | ENG-GATES §1 |
| 别的包做到哪一步、某组契约是否冻结 | ENG-STATUS §2、§3 |
| 开发时预览、按实体 ID 定位镜头 | TOOL-PREVIEW §1、§2 |
| 版本号、用户目录、日志与导出诊断 | OPS-RELEASE §1、§2、§6 |
| 引入第三方素材或代码、查许可 | OPS-RELEASE §7 |
| 为 RPG 预留的接口（规则集、观者状态、输入占用、枚举纪律） | ENG-ROADMAP §6 |
| 谁做什么、向谁回报 | ENG-TEAM §0.1、ENG-TEAM-ROLES §1 |
| 某个包归谁、先开哪些包 | ENG-TEAM-FLOW §1 |
| 一个包从派工到合入怎么走、交接文件怎么写 | ENG-TEAM-FLOW §2、ENG-TEAM-COMMS |

**接入各系统**

| 要做或要查的事 | 先看 |
| --- | --- |
| 读钟点、季节、世界实例 | SYS-TIME §0.1、§1 |
| 材质随时刻和季节变色、新增色名 | SYS-TIME §3；ART-STYLE §0.1 |
| 做季节内容（落叶、开花、覆雪） | SYS-TIME §4；WLD-VEGETATION §9 |
| 让灯、窗、屏幕随时刻亮灭 | SYS-TIME §6；ENG-PERF §10.3 |
| 让表现随天气变化（湿面、积水、降水） | SYS-WEATHER §0.1、§7、§8 |
| 响应画面设置、新增画面单项 | SYS-GRAPHICS §0.1、§3、§8 |
| 让东西动起来：风、飘落物、偶发事件 | SYS-MOTION §0.1、§3、§4.1、§10 |
| 有要跨读档保留的状态 | SYS-SAVE §0.1、§2、§10 |
| 读按图的数据，或代码里要区分地图 | SYS-MAPS §0.1、§7 |
| 要出声；部件的声音插座；碰撞体的脚步材质 | SYS-AUDIO §0.1、§4.2、§9.3 |
| 交碰撞体、做挡路物、登记可行走范围 | SYS-WALK §0.1、§6、§7、§13 |
| 读地面高度、贴地、楼的垫层、水位与坡度；写地形覆盖层 | WLD-TERRAIN §0.1、§3、§4 |
| 该读观者位置还是镜头位姿 | SYS-VIEW §0.1；ENG-ARCH §3 |
| 地图上的锚点、观景点、传送 | SYS-ATLAS §0.1、§5、§6、§7 |
| 只在过场里出现的东西、`IAppFlow` 的各状态 | SYS-TITLE §0.1、§3.3、§7 |
| 界面取色、Theme、界面着色器 | UI-VISUAL §0.1、§3.8、§11.5 |
| 读键盘鼠标、加快捷键、占用观者输入 | UI-NAV §0.1、§5 |
| 界面文字、地名、临时提示 | UI-NAV §4.9、§8 |

**画面与内容**

| 要做或要查的事 | 先看 |
| --- | --- |
| 色名、影子、描线 | ART-STYLE §0.1、§3、§4 |
| 表面属于哪个精度区、底线怎么自查 | ART-FIDELITY §0.4、§0.5、§2.0 |
| 最低合格构件、倒角、法线 | ART-MODELING §7.2、§7.6、§7.8 |
| 天空、主光、雾 | ART-SKYLIGHT §0.1、§3、§6.5 |
| 日式物件能不能用、放在哪 | ART-AMBIENCE §0.1、§1 |
| 翻新老楼 | ART-RENOVATION §0.1；MAP-QSH-RENOVATION |
| 着色器白名单、全局参数、LOD 与淡化 | ENG-PERF §0.1、§1.4、§1.5、§2 |
| 生成几何、流式加载、本机烘焙 | ENG-PERF §5、§6 |
| 某栋楼能不能进、门怎么做、开放新的室内 | WLD-INTERIORS §0.1、§2、§4.1、§14 |
| 关着的楼的窗卡与简化室内 | WLD-INTERIORS §5 |
| 校界、外围的精度、行走与飞行阻挡 | WLD-OUTSKIRTS §0.1、§4.1、§7、§8 |
| 雾幕、截面、远景 | WLD-DIORAMA §0.1、§4.2、§5 |
| 树种、季相、植被 | WLD-VEGETATION §2、§9 |
| 水面、倒影、积水 | WLD-WATER §7 |
| 动物、生活痕迹、街道物件 | WLD-LIFE §0.1、§1、§4 |
| 设计增补、登记覆盖层 | MAP-QSH-ADDITIONS §0.1、§2、§7 |
| 本区的点位、锚点、入口与边界 | 本区 MAP-QSH 文档 §0 末尾的接入要点 |

### 4.3 Godot API

- 写任何 Godot 代码之前，先读 `docs/godot-api/README.md`。
- 用到的每个类、方法、属性，都先在 `docs/godot-api/signatures.txt` 里查到签名再写（D-145）。模型记忆里混着 Godot 3 和 4 的写法，这是最常见的错误来源。
- 着色语言的内置变量和函数，查 `docs/godot-api/shaders/README.md`。

### 4.4 文档不清楚或冲突时

- **冲突怎么判**：按 CORE-CONVENTIONS §7.6 的层级。生效的决策（CORE-DECISIONS）优先于各文档的正文。
- **不清楚或缺内容时**：
  - 会改动契约、数据格式或目录归属的，停下来问主代理；
  - 不影响这些的，先按最保守的理解做，在回报里写明你做了什么假设。
- **发现文档与 CORE-DECISIONS 不一致**：报告主代理，不要自己去改。

## 5 完成与回报

1. **跑检查**：运行 `scripts\gate.ps1 -Wp <WP-xx>`，它会按改动自动跑快检。改动碰到下面三类时，加跑对应的核心专项（ENG-GATES §2，D-167），并确保全部通过：
   - 启动与进出：G5 的启动与进出核心范围（ENG-GATES §2），完整 G5 留到主线最终闸口；
   - 行走碰撞：G15 的前四项和入树检查；
   - 存档：G20 的往返、损坏、退出时保存。

   其余门禁留到集中测试，由主代理安排。截图类门禁在窗口模式下跑，窗口不能最小化；脚本会自己排 GPU 锁，不要绕过；截图和基准一律不开声音。
2. **截图**：用 `--pose` 截几张能说明本包效果的图，存到 `output/render-review/`。巡检点和审查视角的全套截图在集中测试时做。
3. **回报**：按 ENG-GATES §5 的格式写。要提的申请（契约、着色器、引用等，§3.1）、做过的假设（§4.4）、留到后面的细节，都写进回报。
4. **交回以后可能被恢复**（ENG-TEAM-ROLES §3.5，D-191）：返工、解冲突和同包的下一阶段，派你的一方会把新任务发回给你。接到新任务先对齐现状：看 `merged.log`，`git rebase main`，再核对这次涉及的契约、数据和相关包的新提交；你记得的旧版本不当现行依据。

## 6 改文档时

- **只写现行规则**：不写修订说明、用户原话和调研证据（D-152）。格式与引用按 CORE-CONVENTIONS §7。
- **新的规范文档**必须有（D-178）：文件头的 `keywords`、范围块的"查阅"行、§0 结论，以及 §0 末尾的"接入要点"。
- **共享文档**（共识层、ENG-WP、ENG-ARCH 等）由主代理改。子代理需要改时，在回报里提。
- **改完以后**：主代理重跑 `scripts/docs/gen_index.py`，重新生成 CORE-INDEX §7–§9 和 `docs/design/SECTIONS.md`，再跑 `scripts/docs/check_docs.py`。
