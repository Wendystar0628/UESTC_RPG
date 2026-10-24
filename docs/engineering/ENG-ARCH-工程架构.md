---
id: ENG-ARCH
title: 工程架构
category: engineering
parent: ENG-INDEX
summary: 当前分层、依赖方向与唯一生产链，边界、确定性、契约、烘焙与桩的架构准则与后续导向；实现细节以代码为准
keywords: [工程架构, 分层, 依赖方向, 命名空间边界, 程序集, 唯一生产链, KernelStage, 组合根, 数据流, 自动发现, 特性注册, Autoload, 原生资源创建方, 确定性, 同机同输入同输出, StableHash, Rng, BannedApiAnalyzers, 生命周期, 取消与完成, 就绪与等待, 停滞判据, 契约, 契约申请, 契约分批冻结, 枚举穷举, 本机烘焙, .tbin, 英雄资产, 桩, 架构导向, net10.0, 技术栈]
depends: [CORE-INDEX, STD-CODE]
related: [ENG-GATES, ENG-PERF, ENG-BUDGET, ENG-ROADMAP, STD-CONVENTIONS, SYS-MAPS, SYS-SAVE, SYS-MOTION, SYS-AUDIO, ENG-LICENSE]
contracts: []
data: ["scripts/gates/shader-whitelist.json", "third_party.json", "tools/godot-version.txt"]
gates: [G1, G2, G3, G4, G5]
paths: ["game/**/*.cs", "game/kernel/bake/**", "game/testing/**", "game/gen/maps/qingshuihe/Campus.Gen.Maps.Qingshuihe.csproj", "game/gen/sites/**", "game/gen/Campus.Gen.csproj", "game/kernel/map/data/**", "game/kernel/renderer/geometry/**", "game/kernel/renderer/render-state/**", "game/kernel/instances/**", "game/gen/geometry/**", "scripts/gates/arch/Campus.Arch.Tests.csproj", "scripts/godot-api/ApiDump/ApiDump.csproj", "game/contracts/Runtime/Campus.Contracts.Runtime.csproj", "game/contracts/Campus.Contracts.csproj", "game/Campus.Gen.Tests.csproj", "game/Campus.Tests.csproj", "game/contracts/.gdignore", "game/contracts/*.csproj", "game/kernel/registry/**", "scripts/gates/arch/**", "**/Directory.*.targets", "game/Campus.csproj", "game/gen/.gdignore", "game/project.godot", "Directory.*.targets", "game/kernel/stubs/**", "**/Directory.*.props", "Directory.*.props", "game/contracts/**", "game/gen/bakeinput/assembly/**", "game/addons/**", "global.json", "**/*.csproj", "tools/**", "game/Campus.sln"]
---
# ENG-ARCH · 工程架构

> **范围**：本文说明当前的工程架构（技术栈、分层、生产链与数据流），架构层面的准则（边界、确定性、契约、烘焙、桩），以及后续设计的导向。只写结构、规则和方向。
> **不在本文**：类型签名、目录明细、文件格式、参数与清单以代码和数据为准；写代码的结构规矩见 STD-CODE；命名与目录约定见 STD-CONVENTIONS §3、§5；性能与预算见 ENG-PERF、ENG-BUDGET；门禁见 ENG-GATES；RPG 路线见 ENG-ROADMAP。
> **查阅**：先读 §0；当前架构看 §1；边界与确定性看 §2；契约看 §3；资源与烘焙看 §4；桩看 §5；架构导向看 §6；RPG 扩展方式看 §7

## 0 结论

1. **四层，依赖只朝下**：契约 → 世界与公共几何 → 内容生成 → 运行时；同层的内容模块互不引用；程序集只有契约两个、通用生成一个、每张图一个、运行时一个，层间与内容模块间的边界由命名空间规则（G4）守（§1、§2，DECISION-232）。
2. **生成是纯 .NET 的纯函数**：生成侧契约 `Campus.Contracts` 与生成层不引用 Godot，只给运行时用的 `Campus.Contracts.Runtime` 可以用 Godot 类型；`Build()` 只读冻结的地图输入，不读运行时状态、画面设置和时间（§2）。
3. **一条生产链**：正式运行由 `KernelStage` 装配；每类状态一个写入方；原生资源只由所属的内核服务创建（§1、§2）。
4. **打特性即接入**：模块、部件、服务、节点、存档段靠特性自动发现，不改共享文件；Autoload 只有 `Boot`，`Kernel` 是静态入口（§2）。
5. **确定性**：随机和哈希只用契约的 `Rng`、`StableHash`；同一台机器上同一输入烘出同样的字节，不要求本机与别的机器逐字节一致（§2，DECISION-232）。
6. **契约以源码为准**：签名只在 `game/contracts/`；改动先申请，与全部使用方同批迁移（§3）。
7. **几何构建期烘焙**：几何由代码生成，每批收口全图预烘，启动只读现成缓存；缓存查找、判鲜与补烘只在 §4。
8. **往哪走**：运行时状态只有一个写入方，按职责拆；其余落地写法在 STD-CODE（§6）。
9. **寿命与等待**：作用域拥有并排空自己的工作，取消是请求、清理完才算完成；就绪由持有者按实际结果发布，等待按连续无进展判停滞（§2「生命周期与发布」「就绪与等待」）。

### 0.1 接入要点

- **新建或修改文件**：只改派工简报允许的路径；`project.godot`、csproj、`Directory.*.props`、契约、`scripts/gates/shader-whitelist.json`、`docs/`，以及新的着色器全局参数、着色器文件、项目引用、NuGet 包，向 Codex 主线申请（WF-BUILD §1、§3）。
- **写 `Campus.Contracts`、生成层或烘焙子任务**：不引用 GodotSharp，不读文件路径和运行时状态，不引用其他内容模块的命名空间（§2）。
- **要显示、用材质或出声**：显示经 `IRenderHost`，材质经 `ToonSpec`，声音经音频服务；不自己创建网格实例、`ShaderMaterial` 和音频播放器（§2）。
- **接入新模块、节点、服务或存档段**：打对应的特性，不加 Autoload，不改 `main.tscn`（§2）。
- **写随机、计时或哈希**：用契约的 `Rng`、`StableHash`，遍历字典先排序；禁用写法以各程序集的 `BannedSymbols.txt` 和 G4 为准（§2）。
- **读写 `user://`**：每个文件只有一个读写方；不用 `ResourceLoader`、`ResourceSaver`（§2）。
- **用到或改契约**：签名看 `game/contracts/` 源码；命名空间只用 `check-naming.ps1` 登记的；会追加值的枚举写穷举 `switch`，JSON 用 `ContractJson.Options`；改动与全部使用方同批迁移（§3）。
- **写全局着色器参数**：只由 `ShaderGlobals` 登记的写入方写，换写入方同批改登记（G4）。
- **碰启动路径、缓存或全图级系统**：启动只读不建，不编译、不导入、不生成；核对缓存只比输入标识；全图级系统随格挂载（§4，DECISION-256）。
- **开后台任务、订阅或原生资源，或写等待与就绪**：挂到所属作用域，退出走停接新工作、取消、排空、逆序释放；等待写明在等谁、有失败出口，停滞判据只引用 §2「就绪与等待」（§2）。
- **依赖的能力还没交付**：照显式桩开发，真实实现接入时同批删桩（§5）。

## 1 当前架构

### 技术栈

引擎锁定 Godot 4.7.2-stable .NET 版，目标框架 net10.0（C# 14，DECISION-177），以 `tools/godot-version.txt`、`global.json`、csproj 为准，三处一致，门禁比对引擎 `--version`；升级按 ENG-BUDGET-RULES §1「版本锁定」，升级后按 `docs/godot-api/README.md` 重新生成 API 参考；只做 PC（Windows），渲染器 Forward+，默认 D3D12。代码全部用 C#，着色器用 Godot 着色语言，计算着色器用 GLSL；Godot API 以本地签名库 `docs/godot-api/` 为准。

### 分层

| 层 | 程序集 | 责任 |
| --- | --- | --- |
| 契约 | `Campus.Contracts`；`Campus.Contracts.Runtime`（运行时边界契约，生成层不引用） | 数据、命令、只读状态与确定性工具；`Campus.Contracts` 只引用基础类库；`Campus.Contracts.Runtime` 可暴露引擎类型并引用 GodotSharp，但不引用 Kernel、系统层或生成实现；跨模块运行时能力统一经它发布 |
| 世界与公共几何 | `Campus.Gen`（世界、几何、部件内核等通用生成，以及 `Campus.Gen.BakeInput` 的冻结输入装配与底图烘焙） | 解释冻结的地图输入（世界文件、地形、室内索引；路网由世界文件派生），提供权威查询和共享几何 |
| 内容生成 | 通用内容模块在 `Campus.Gen`（建筑、地面、植被、水、碰撞、可行走、外围、声学等，及跨图共用的公共站点 `Campus.Gen.Sites`）；每张图一个 `Campus.Gen.Maps.<图>`，在 `game/gen/maps/<图>/`，各区是它下面的子目录与命名空间 `Campus.Gen.Maps.<图>.<区>` | 按格生成几何、碰撞与声明数据；纯函数，命名空间互不引用 |
| 运行时 | 主程序集 `Campus`（kernel、stage、flow、camera、medium、systems、style、audio、hud、gates） | 装配、烘焙与流式、世界状态、表现与界面 |

- 程序集与目录的对应以仓库本身为准，路径归属以各文档文件头的 `paths` 为准。程序集只按"契约 / 通用生成 / 每图一个 / 运行时"划分，模块与模块、层与层之间的边界靠命名空间规则（§2）；加一张图只新增 `game/gen/maps/<图>/`，不改共享清单（SYS-MAPS §6）。

### 生产链

1. `Boot` 解析命令行并注册只读 `BootRunOptions`；`KernelStage` 是唯一的组合根，装配已注册的服务与节点。下层只消费已解析输入，不回读 Boot 实现。
2. 地图服务加载当前图的冻结输入，发布一份世界模型。
3. 烘焙服务按格调用内容模块的 `Build()`，产物进缓存（§4）；流式与 LOD 把格包经 `IRenderHost` 挂上。植被、动物、夜灯与碰撞经同一参与者协议交给流式导演，统一负责请求、完成发布、就绪和退休；挂载工作共用生命周期骨架，各所有者只实现本类内容的有界挂载与资源释放。
4. 运行时系统（时间、天气、植被、水、灯、动物）读世界状态驱动表现；行走读碰撞与可行走烘焙；界面只读服务发布的展示数据。
5. 启动、换图、读档、退出都由正式编排入口（应用流程、地图服务、存档服务）驱动，走同一条链。
6. 拍摄由 `IGameCapture` 的游戏内服务统一持有界面可见性、已绘制帧屏障、视口读回与图像导出；固定机位、原生录像编排、F12 与存档缩略图共用，调用方保留自身就绪与失败语义。

### 数据流

`pipeline/` 离线生成按图数据 → `game/data/maps/<图>/` 的 JSON 与世界文件 → 世界模型 → 生成 → 缓存 → 挂载（§4）。

## 2 边界与确定性

### 依赖方向

- 上层可以引用下层，反过来不行；同层的内容模块、归属不同地图的程序集互不引用；通用生成不引用任何 `Campus.Gen.Maps.*`，每图程序集只引用契约和 `Campus.Gen`（SYS-MAPS §6）。程序集并少之后，这些边界由 `scripts/gates/arch/` 的命名空间架构规则（G4）挡，不靠多建程序集；增删规则要 Codex 主线批准。
- **kernel 内部**：自底向上为 Diagnostics/Frame/Registry、Renderer/Materials/Lod/Instances、地图与烘焙流式及状态驱动、Boot/Save/Inspect（Diagnostics 是唯一的诊断输出入口，在最底层：正式运行由 Boot 启用后写引擎错误输出，由引擎落到 `user://logs/godot.log`（ENG-LICENSE §1），没有引擎宿主的纯 .NET 测试仍写标准错误；上层经契约 `IDiagnosticLog` 取用；Inspect 是最高层的拍摄与检视）；完整顺序以 G4 的 `KernelLevel` 声明为准，低层不依赖高层，不留双向依赖或豁免。
- **生成层看不到运行时**：`BuildContext` 里没有世界状态、画面设置和规则集。派生值只经 `DerivedMemo` 缓存：只读冻结世界的按世界复用（`ForWorld`），依赖种子或输入的随上下文（`BuildContext.Memo`），不另写私有缓存。随时间、季节和天气变化的东西，只能通过调色板、灯光通道、变体掩码和全局着色器参数表达（ENG-BUDGET-RULES §1）。
- **门禁与专项驱动在 `game/gates`**：`kernel/registry` 只提供服务注册与查找；`RulesetService`、`GlobalBuildInputs` 是注入的服务，不放在 registry。
- **几何只经 `gen/geometry/GeometrySink`**：普通表面按完整顶点共享，保留材质及可选通道的边界；Indexed 保持已给定的顶点、索引、法线和自定义通道顺序，Positions 装配尚未分格的点集；`VertexFlag`、`GeometryVertexLimits` 只在契约里定义，生成器不自带位置法线数组。
- **运行时内部**：各命名空间之间只经契约和 Kernel 服务交互；界面除 GodotSharp 外只依赖契约和 `Kernel.Get<T>()`，不用其他模块的内部类型。
- **原生资源各有唯一创建方**：网格与渲染实例只由内核创建，其他模块经 `IRenderHost` 申请并按挂载句柄释放；`ShaderMaterial` 只由 Style 按白名单创建，其余经 `ToonSpec` 取；音频播放器与总线只由 Audio 创建；计时只出现在内核的帧、画质、巡检、存档和门禁里。这样每类资源只有一处记账与释放；逐条硬规则见 ENG-BUDGET-RULES §1。
- **`user://` 每个文件一个读写方**：存档只由内核存档服务读写，本机烘焙缓存只由烘焙服务读写，性能记录在游戏内只由记录服务读写、仓库脚本只读；不用 `ResourceLoader`、`ResourceSaver`，因为 Resource 能嵌入脚本、读入即执行（ENG-BUDGET-RULES §1 第 19、23 条）。

### 确定性与禁用写法

禁用写法由 BannedApiAnalyzers（各程序集一份 `BannedSymbols.txt`）和 G4 的架构规则挡住，清单以它们为准。背后的原则：

- 随机只用契约的带种子 `Rng`；种子由 `StableHash` 把世界种子与 ID、用途组合起来，与生成顺序无关。
- 不读系统时钟，不用进程间不稳定的哈希。
- 只保证同一台机器上同一输入同一输出；不要求本机与别的机器逐字节一致，不设专门的数学库，数学照常用标准库（DECISION-232）。
- 遍历字典、集合前先排序；一个 `Build()` 内部不做并行浮点归约，并行只在格与格之间，由内核调度。
- 颜色只用色名，着色器用全局的 `anim_time` 而不用 `TIME`（ENG-BUDGET-RULES §1 第 1、4 条）；游戏代码不写 GDScript（编辑器与构建期工具外壳除外，DECISION-005），不手写 `uid://`，`main.tscn` 以外不放共享的 `.tscn`、`.tres`。

由此保证：同一台机器上同一输入烘出同样的字节，缓存判鲜才可靠；运行时同一钟点、同一种子、同一串输入得到同样的画面；读档是换一个世界实例，续跑等价（SYS-SAVE-FLOW §2.3）。例外只有登记过的几处，以 G4 的登记为准。

### 自动发现与模块接入

- 内核按构建时生成的程序集清单逐个加载，清单由 MSBuild 实际构建的项目引用推出、不手改，反射扫描特性，按 ID 排序注册，ID 重复报错。注册种类用 `RegistrationKind`；特性参数与默认寿命以契约源码的 `Registration` 为准，发现、桩替换、按图筛选和构造走同一实现。注册记录与反射发现能力统一放在纯契约的 `Campus.Contracts.RegistrationDiscovery`，它通过纯 .NET 元数据识别运行时声明，不引用生成层或运行时程序集。
- **注册 ID 是运行身份**：`Owner` 只用于诊断归因，由构建期复用生产项目的源类型索引、按特性声明的文件路径匹配文档文件头的 `paths`，得到归属文档编码，仅作诊断，写入同一份构建代码指纹资产，发布版只读该资产；不手写。诊断归属不参与运行顺序、缓存身份、实例池键或 claim 裁决，claim 指向内容模块的注册 ID。
- 加载一张图时只选择"通用 + 当前图"的声明；对象寿命由特性的 `Lifetime` 决定，`Map` 与 `MapScope` 只决定适用地图。内容模块、室内提供者、发声器来源与烘焙生产者默认按图，部件、服务、节点、介质呈现器和存档段默认全局（SYS-MAPS §6）。
- Autoload 只有 `Boot`；`Kernel` 是纯静态访问入口，新的全局对象一律注册成 Kernel 服务。
- 生成与挂载分开：`Build()` 在工作线程上跑；挂载和会动的物体只在主线程，由内核节点负责（SYS-MOTION）。

### 仓库脚本自成一体

`scripts/` 不引用 `.collab/`；本机协作工具可以调用仓库脚本，反过来不行。别人克隆仓库也要用的工具（构建、检查、清理、截图）放 `scripts/`。

### 生命周期与发布

- **一次读取、一次发布**：地图加载发布同一版本的上下文，消费者显式接收，不各自重读同一份文件。
- **可写与冻结**：构建阶段可写，进入共享缓存或跨线程发布时冻结；后台任务只持纯数据，主线程接收时核对地图与任务的代际。
- **谁开谁关，按作用域整体释放**：应用、地图、格、节点与一次操作各自拥有自己的资源、订阅和异步工作；原生资源创建时就登记到所属作用域，作用域结束时整体释放，不在别处单个释放；卸图或退树时终止等待、解除订阅，迟到的结果不再发布。
- **注册器的所有权**：注册器只拥有自己创建的实例；外部 Add 进来的实例是借用，不纳入拥有列表。按图释放只释放本图作用域；存档段的 `SaveScope` 只决定数据身份，与实例寿命无关。
- **关闭顺序**：退出分两段，都只在退出流程里调：先 `SealMapPreparationAsync` 封口——注册器从此拒绝新建、只放行已有绑定，地图准备与后台生产停下；等各生产方排空后再 `ShutdownAsync`：关图，让节点离树（离树时仍能查已有绑定），按创建的逆序释放自己拥有的对象，最后清场，异步释放完成后才退出引擎。封口没有解封，换图与回标题不调它。
- **应用失败的接收方**：无法恢复的失败只交 `IAppFailure`（舞台持有），它保留原异常并负责关机；上报方照常清理自己，启动工作与退出参与者都不等应用关闭；同一失败只走一条路，就绪通道只让等待方停下，不再上报。退出时注册器先让全部退出参与者停接新工作并取消，再逐个等它们排空（`DrainForExitAsync`），一个失败不挡其余，原异常都交到接收方。
- **取消与完成**：取消只是请求，实际工作清理完才算完成；按令牌或所有者状态识别取消，不看异常文字；取消不算失败，真实失败也不因取消抹掉。
- 不另建平行的状态机或全局事件通道；服务可以拆成职责明确的内部组件。

### 就绪与等待

- **就绪由持有者发布**：就绪按实际结果由状态持有者发布，不用代理标志、提前完成或探测注册表推断；等待方只等持有者给出的结果。
- **等待有对象、有出口**：每个等待写明在等哪个持有者、哪一代的哪个目标；失败与取消都有出口，失败按该目标的失败码上报。
- **按进展判停滞**：只要进度在走就一直等；同一步连续 60 s 没有进展才按停滞处理，不按总时长判。60 s 是初值，在最低配置的笔记本上首次运行实测后取实测的数倍以上。
- 就绪之前不撤遮挡、不放输入（SYS-TITLE §1）。

## 3 契约

- 契约在 `game/contracts/`（`Campus.Contracts`，运行时边界契约 `Campus.Contracts.Runtime`）；签名、注释和 JSON 格式以源码为准，文档不抄签名。契约按领域归目录，不设杂物目录；`Runtime/Maps` 是地图运行时契约的唯一目录。
- **帧顺序以契约 `FramePriority` 为准**：每个登记步的优先级值唯一，同值登记节点按节点 ID 序执行；步内不声明前后关系，需要先后的节点使用不同登记值，使用方直接引用登记项（DECISION-135）。
- **`BuildContext`**：`CodeHash` 与 `WindowNumbering` 必须显式提供，无窗口地图和编号前置上下文提供空编号快照；`WithWorld` 只切换冻结世界并保留其余输入；`BuildingSet` 只接受 `BuildContext`。`CaptureContext` 必须携带同次保存唯一的 `WorldSaveMoment`，存档段不另取时刻。
- 改契约，或加着色器全局参数、着色器文件、程序集引用、NuGet 包，向 Codex 主线申请（WF-BUILD §1、WF-BUILD §3）；改动与全部使用方、数据和文档同批迁移，不保留旧接口（WF-RULE §10，DECISION-209、DECISION-214）。
- **分批冻结**（DECISION-134）：某组契约的真实生产方与使用方在主线上接通、核心专项通过后，由主会话宣布冻结，之后只增不改；冻结的组记进施工状态。
- **演进约束**（为 RPG 扩展留口，ENG-ROADMAP）：record 只加 init 属性，不动位置参数；接口加成员带默认实现，或另开新接口；不改已有签名或枚举序号；不让 `Build()` 读运行时状态；不为剧情改烘焙产物或烘焙碰撞，剧情带来的世界变化放在运行时层：显隐掩码、物体差分、运行时门体、导航覆盖。
- 画面相关的契约（`ToonSpec`、调色板、着色器全局参数、界面令牌）由主会话确认字段够用、命名与美术文档一致。
- **枚举纪律**：枚举只在末尾追加值，JSON 按字符串存；会追加值的枚举，消费方写穷举 `switch`，`default` 分支拒绝或报错，不写二值判断；读到不认识的字符串按不合法处理（SYS-SAVE-FORMAT §1.3）。
- JSON 一律用 `ContractJson.Options`；存读档用 System.Text.Json 源生成（SYS-SAVE-FORMAT §4）。
- 契约用 `System.Numerics`，Godot 侧用引擎的向量与变换，两边转换只用内核的一组扩展方法（STD-CONVENTIONS §1）。

## 4 资源与烘焙

- **几何由代码生成**：全部几何由生成层的 `Build()` 产出。确需外部模型（如雕像）时先经主会话批准，作为英雄资产放 `game/assets/hero/`。
- **构建期烘焙**（DECISION-165）：格包、碰撞、室内包、平面底图与远景、遮挡体、灯表、导航图、声学等派生数据，都由烘焙服务调用同一个 `Build()` 生成。每批收口用同一入口全图预烘，启动不现烘；缓存缺失或损坏时才本机补烘，开始页起在后台把全图补齐，碰到的格优先；不做几何启动集（DECISION-232）。
- **开放高处的许可源**：与可见布局、碰撞共用真实几何，只在冷烘捕获并随常驻元数据冻结；运行时只读冻结区域，不调用生成来源。
- **查找顺序**：本机缓存 → 本机补烘。缓存只有一份：`UESTC_BAKE_CACHE` 指向的目录，未设时在仓库的 `output/cache/bake/`（以 `BakeOptions` 源码为准），共享缓存只由收口预烘写入（DECISION-346）。缓存上限、淘汰与启动耗时目标以 `budgets.json` 的 `cache` 为准，补烘时机见 ENG-PERF-STREAM §1.1。
- **格式**：缓存用自定义的 `.tbin`，不用 Godot 资源格式；顶点、索引、实例变换、高度栅格这类大块数值一律走二进制通道，JSON 只放键、ID、包围盒这类小元数据；格包只存产物运行数据，不存诊断归属；格式变化升级格式版本和缓存 ABI，不留旧格式兼容；逻辑格（一个烘焙键的完整产物）超过单个物理分片上限时，由烘焙层统一拆成物理分片并写逻辑索引（带该格输入标识，核对只读索引不解载荷），索引最后发布、分片齐全才算就绪，对外只交付拼好的完整格包，消费方不感知分片；读坏了删掉重烘一次，仍不行报失败。
- **判鲜**（DECISION-229）：每个生产方（内容模块、室内提供者、烘焙生产方）声明实际用到的数据类别与地图文件或全局文件键。每格只看这些数据输入与该产物实际用到的生成代码改没改，改了只重烘这一格，没改的直接用；代码那一半由构建期生成代码指纹清单（`scripts/build/fingerprints`），在生产源码、构建输入或文档文件头的 paths 有变时更新，运行时只读这份清单；指纹的输入范围、分组与例外以 `scripts/build/fingerprints` 为准，室内按楼判鲜，改一栋只重烘这一栋。
- **窗号**：窗号地址由本图 `window-numbering.json` 持久登记，新网格与扩容追加地址，退役不复用；显式预烘成功后写回登记表，正常启动只读。
- **构建期做完，运行时只读**（DECISION-256）：能在构建期或收口预烘里算好的不放到运行时；固定种子的静态数据纹理（如云噪声）由构建导入插件生成原生资源，运行时只借用上传。启动路径只读不建：双击启动不编译、不导入，正式产物（程序集、指纹清单、导入缓存、构建期生成的静态资源）都在就直接起引擎、缺了才编与导入，产物新鲜由集成与收口预烘的原生增量构建与导入保证，不另设判鲜（DECISION-302）；启动到能走只起引擎与服务、核对缓存、读首视角与出生点要的格；核对只比输入标识，生成只在缓存缺失时发生；全图级的系统随格挂载，不在启动时整图挂。
- **管线输入**：`pipeline/` 离线生成按图数据。同一台机器上用同一份已提交输入重建，输出逐字节相同；世界文件不由管线生成（DECISION-252）；Python 依赖按 `scripts/requirements.lock`（工程脚本）与 `pipeline/audio/requirements.lock`（离线音频）锁定。
- **着色器单一来源**：共用雾函数以 `fog.gdshaderinc` 为唯一源；`scripts/build/generate-style-shaders.py` 读取调色板登记表与 C# 字面规则，生成 `fog.glslinc` 与 `style_consts.gdshaderinc`。两份 include 及其 UID 是本地构建产物，不入库；构建时生成，启动复用现成结果。
- **导入入口**：导入只走 `game/addons/build_import` 的 EditorPlugin，命令行一律传 `--build-import`；`project.godot` 只登记该插件。
- **贴图与字体**：PNG 源图加 Godot 导入，贴图规则见 ENG-BUDGET-RULES §1 第 5 条；数据纹理在运行时创建；字体见 UI-VISUAL-COMPONENTS §1，音频素材见 SYS-AUDIO §8。
- **许可登记**：资源的许可与来源登记见 ENG-LICENSE §2。

## 5 未交付能力的占位边界

正式运行统一经 `KernelStage` 装配已交付的服务与节点。尚未交付的能力可以用显式桩，桩的注册、行为与默认值以 `game/kernel/stubs/` 源码为准。

- 依赖方按正式契约接线；桩只提供声明过的占位行为，桩的就绪、成功返回或截图不算该能力通过核心验收。
- 真实实现接入时，同批撤掉对应的生产桩、旧执行入口和旧断言，不留两套生产路径（WF-RULE §10）；测试替身不带运行时注册。
- 能力未实现或正式检查缺失时如实标为未交付，不以占位结果代签。

## 6 架构导向

新代码直接按这里写；边界、整图流程、公共层、程序集、测试的落地写法见 STD-CODE §1–§3、§5、§6。

- **数据驱动内容**：实体的坐标、轮廓、层数、名称只在按图数据里，代码按 ID 从世界模型读，不抄副本。要用某个要素的一段（一条边、一段岸线），由数据里的选段要素写两个端点指明，代码按端点取，不按顶点序号、最长、最靠某侧去挑，端点对不上轮廓就报数据错误（DECISION-243）；本图专用的造型与组合留在每图程序集，尺寸各在本区一处（DECISION-239）。
- **运行时状态只有一个写入方**（DECISION-244）：模式、位姿、流速、加载界面、就绪、主题这类状态各有唯一持有者；别处要改，向它申请租约（释放即恢复）或发一个命令（如到达），不各自复位标志；查询不改状态；模块之间经契约或注入接上，不按节点名、字符串键去找；全局着色器参数各有登记的唯一写入方。
- **按职责拆**：类型按职责和状态归属拆成独立对象，不用 partial 按行数切开；Godot 宿主只留生命周期、服务装配与实际编排，相机、行走、HUD、设置、流程、存档、烘焙、流式和内容生成各归自己的对象；只有原门面注册原有服务与参与者，内部对象不另设全局入口。

## 7 扩展点（RPG）

现在只做观光模式；扩展方式、规则集与挂点见 ENG-ROADMAP §1、§3，契约的演进约束见 §3。

