---
id: ENG-ARCH
title: 工程架构：模块边界、接口契约与并行开发流程
category: engineering
summary: 目录与归属、程序集边界、全部 C# 契约、帧顺序、资源管线、桩与工作树流程
keywords: [工程架构, 目录归属, ownership.json, 程序集边界, 禁用写法, BannedApiAnalyzers, ArchUnitNET, 确定性, DetMath, StableHash, 自动发现, 特性注册, ContentModule, KernelNode, KernelService, Autoload, 契约, 契约冻结, PublicApiAnalyzers, ContractJson, 桩, 占位实现, 资源管线, 本机烘焙, .tbin, 缓存键, 工作树, git 流程, 合并队列, net10.0]
owners: [WP-00]
contributors: [WP-01, WP-02, WP-04, WP-08, WP-27]
depends: [CORE-INDEX, ENG-GATES, ENG-WP, ENG-PERF]
related: [ART-STYLE, ART-FIDELITY, SYS-MOTION, SYS-VIEW, ENG-BUDGET, SYS-TIME, UI-NAV, SYS-WALK, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP, WLD-TERRAIN]
contracts: [FeatureId, PartId, ModuleId, ZoneId, WpId, ChannelId, PaletteToken, Box3, LodLevel, TileKey, MapId,
  WaypointRef, StableHash, Rng, DetMath, Aspects, Claim, FeatureClaim, AreaClaim, ContentModuleAttribute,
  IContentModule, BuildContext, ContentOutput, MeshLod, GeometryBundle, PartAttribute, ParamAttribute,
  IPartDef, PlacedPart, ToonFx, ToonNormal, LineClass, ShadeSteps, ToonSpec, ToonRef, Season, ViewMode, Body,
  Flow, WorldTransition, WeatherKind, WeatherMode, WeatherSel, WeatherVec, WeatherSpan, WorldPreset,
  IWorldState, ClockSetMode, IWorldControl, WeatherQueueItem, WeatherBoot, WeatherTick, WeatherProcessState,
  IWeatherProcess, FramePriority, ChannelKind, ChannelRate, ChannelDecl, SeasonMask, NightRule, VariantTag,
  WindSample, IWindField, OverlayFile, OverlayLevel, OverlayOp, ReshapeOp, HeightOp, StyleOp, HideOp, AddOp,
  FeatureSource, IWorldModel, ISpatialIndex, Pose, ICameraRig, EntryPath, TransitPreference, TransitOptions,
  TransitKind, TransitResult, ITransitHandle, PoseMath, MediumKind, CameraMedium, MediumPresenterAttribute,
  IMediumPresenter, IMediumState, InteriorDetail, ZoneKind, PortalKind, StoreyInfo, ZoneVolume, PortalDecl,
  EntranceDecl, ElevatorDecl, DoorLeafKind, DoorLeafDecl, InteriorLayout, InteriorProviderAttribute,
  IInteriorProvider, InteriorIndexEntry, IInteriorIndex, DoorPhase, DoorEvent, IInteriorDoors, LocalizedName,
  InteriorOverlayFile, InteriorOverlayOp, SealKind, SealOp, InteriorConfigOp, InputLockReason,
  WaypointGroup, WaypointStand, WaypointRecommend, Waypoint, WalkClass, SurfaceTag, Collider, WallCollider,
  RampCollider, PostCollider, BoxCollider, SlabCollider, HeightFieldCollider, SlopedWallCollider, SweepHit,
  SupportHit, ICollisionWorld, ModeSwitchVia, ModeSwitchResult, IViewControl, QualityPreset, DeviceTier,
  ApplyClass, KnobKind, QualityKnob, KnobSpec, QualityProfile, QualityView, ApplyOrigin, QualityChange,
  KnobDelta, ApplyPlan, ApplyStatus, ApplyResult, IQualityService, IQualityAware, QualityResponsiveAttribute,
  IMotionSettings, IPaletteProbe, VeilReason, VeilContent, IVeilState, IVeilService, LightningFlash,
  ILightningFeed, IChannelReader, MapKind, MapTimeRule, GeoFrame, MapRect, RoamLimits, MapSpawn, MapWorldRules,
  MapEnvironment, MapTitle, MapPortal, MapDescriptor, MapScopeAttribute, IMapContext, MapTravelCause,
  MapTravelRequest, MapStage, MapTravelResult, IMapTransition, IMapService, IMapAware, MapVisit, MarkerId,
  MarkerKind, AnchorForm, UnlockState, TravelStyle, TravelRefusal, PinShape, TeleportSpec, AtlasMarker,
  AtlasRegion, AtlasLayer, CustomPin, IUnlockPolicy, IAtlasService, AtlasSave, AppState, EnterKind,
  CinematicMode, EnterPath, EnterResult, EnterRequest, IAppFlow, SlotId, SlotKind, SaveScope, SaveReason,
  SlotHealth, SaveMeta, SlotInfo, SaveSectionAttribute, RestorePhase, LoadScope, CaptureContext,
  RestoreContext, NewGameContext, ISaveSection, SaveStatus, SaveResult, LoadStatus, LoadResult, ReadOnlyReason,
  SaveState, ISaveService,
  RulesetId, WorldControlMask, InteractionRule, RulesetDescriptor, IRuleset, IObserverState, OutskirtsBand, OutskirtsKind, OutskirtsBlock, IOutskirtsModel, NavNodeId, NavSpace, NavEdgeKind, NavFlags,
  DoorKind, DoorState, NavNode, NavEdge, NavPortal, NavFloorGraph, NavQuery, NavPath, INavGraph, AudioBuses,
  AudioGroup, BackgroundBehavior, SuddenSoundMode, AudioSettings, UiCue, MusicCue, DuckReason, AudioSnapshot,
  IAudioService, SoundSetId, AmbienceProfileId, EmitterShape, DayBand, WeatherMask, TraceKind,
  AmbientEmitterDecl, AmbientEmitterAttribute, IAmbientEmitterSource, SoundSocketDecl, AmbienceCue,
  AmbienceSnapshot, IAmbienceService, AcousticClass, AbsorbClass, PathKind, AcousticPortalKind, AcousticEnv,
  SendTap, ListenerRouting, OcclusionResult, AcousticPortal, IAcousticEnv, AcousticRoomDecl,
  AcousticPortalDecl, AcousticsDecl, FootMat, StairSpec, FootPatch, GroundCover, IGroundCover, FootSide,
  FootfallInfo, ContactInfo, IWalkTelemetry, FoleyKind, GroundSample, FoleyEvent, FoleyLoop, FoleyStats,
  IFoleyService, KernelNodeAttribute, KernelServiceAttribute, ShaderGlobals, ContractJson, PartCatalog,
  ModuleBudget, PartBudget, PartGeometry, LightEmitter, Footprint2D, InspectionProbe, WorldFeature,
  WorldNetwork, PolygonRings, Polyline, OverlayGeometry, BuildingFamily, FacadePreset, DesignKind,
  PostOverride, IRuntimeContext, CinematicDef, CameraSeg, WorldCue, MaskSeg, CaptionCue, ExtraCue, AudioCue,
  CinematicEnd, IBakeService, BakeKey, BakePriority, BakeState, IBakeHandle, BakeProgress, FidelityZone, FacadeSegment,
  RoofZone, IFidelityModel, FidelityOverlayFile, FidelityOverlayOp, FidelityK1Op, FidelityRaiseOp,
  ITerrain, TerrainFacts, TerrainClass, TerrainOverlayFile, TerrainOp]
data: ["ownership.json", "third_party.json", "tools/godot-version.txt", "version.txt", "game/contracts/schemas/", "game/data/rulesets/", ".gitattributes", ".gitignore"]
gates: [G1, G2, G3, G4, G5, G6]
---
# ENG-ARCH · 工程架构：模块边界、接口契约与并行开发流程

> **范围**：本文规定 Godot 工程的目录结构与归属、程序集边界与禁用写法、确定性规则、自动发现、全部 C# 契约（§3 是契约的权威来源）、帧顺序、资源管线、桩、git 工作树流程和主要风险。所有子代理都按这里的约定写代码，让 20–40 个子代理同时工作也不互相覆盖。
> **不在本文**：各系统的行为细则以各自的文档为准（例如门的参数 WLD-INTERIORS §4.1、天气 SYS-WEATHER）；全局参数登记表见 ENG-PERF §1.5；预算见 ENG-BUDGET；门禁判据见 ENG-GATES；工作包分工见 ENG-WP；以后的扩展路线见 ENG-ROADMAP。
> **查阅**：先读 §0；目录归属、程序集边界、禁用写法与自动发现看 §2；契约签名看 §3；资源管线与本机烘焙看 §4；桩看 §5；工作树的技术细节看 §6；git 与合并流程看 WF-MERGE §5；风险与待验证项看 §7、§9；RPG 扩展点看 §8
> **读者与技术栈**：写代码的子代理先看 §0 的接入要点，再按"查阅"一行查要用的那一节；主代理另查 §5–§7 的桩、合并队列与风险。引擎是 Godot 4.7.2-stable .NET 版，目标框架 net10.0（D-177），只做 PC（Windows），渲染器 Forward+；代码全部用 C#，着色器用 Godot 着色语言（`.gdshader`、`.gdshaderinc`），计算着色器用 GLSL（`.glsl`）；§3 的 Godot 侧写法以 [引擎 C# API 参考](../../godot-api/README.md) 的签名为准。
> **关联**：[CORE-INDEX 总索引](../README.md)、[ENG-GATES 验收门禁](ENG-GATES-验收与门禁.md)、[ENG-BUDGET 性能预算](ENG-BUDGET-性能预算与硬规则.md)、[ENG-WP 工作包](ENG-WP-工作包派工单.md)、[ENG-PERF 性能工程](ENG-PERF-性能工程.md)、[ART-STYLE 风格基础](../art/ART-STYLE-风格基础.md)、[SYS-MOTION 动态与事件](../systems/SYS-MOTION-动态与事件.md)、[SYS-VIEW 观察模式与介质](../systems/SYS-VIEW-观察模式介质与开场.md)、[SYS-TIME 时间季节](../systems/SYS-TIME-时间与季节.md)、[UI-NAV 界面与跳转](../ui/UI-NAV-界面与操作说明.md)、[SYS-WALK 行走](../systems/SYS-WALK-行走模式.md)、[SYS-WEATHER 天气](../systems/SYS-WEATHER-天气.md)、[SYS-GRAPHICS 画面设置](../systems/SYS-GRAPHICS-画面设置.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL-界面视觉语言.md)、[SYS-ATLAS 平面地图与传送](../systems/SYS-ATLAS-平面地图与传送.md)、[SYS-TITLE 开始页与过场](../systems/SYS-TITLE-开始页与进入过场.md)、[SYS-SAVE 存档](../systems/SYS-SAVE-存档与持久化.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS-多地图.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS-边界与外围.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS-室内.md)、[SYS-AUDIO 音频](../systems/SYS-AUDIO-音频.md)、[ENG-ROADMAP 扩展路线](ENG-ROADMAP-扩展路线.md)、[WLD-TERRAIN 地形](../world/WLD-TERRAIN-地形.md)

## 0 结论

1. **目录即归属**：Godot 项目在 `game/`（`res://`）；每条路径归一个包，记在 `ownership.json`，越界的改动由 G3 拒绝；`project.godot`、csproj、契约、`ownership.json` 等共享文件的改法见 WF-BUILD §1（§2）。
2. **程序集分层**：契约 `Campus.Contracts` 和生成器 `Campus.Gen.*` 是纯 .NET，不引用 GodotSharp；Godot 侧代码都在主程序集 `Campus`；内容程序集互不依赖，其余限制由 ArchUnitNET 规则检查（§2"程序集边界"）。
3. **特性接入**：新模块、节点、服务、存档段打上对应特性就自动注册，不改共享文件；Autoload 只有 `Boot` 和 `Kernel`（§2"自动发现"）。
4. **确定性**：随机、哈希和 gen 里的超越函数一律用契约的 `Rng`、`StableHash`、`DetMath`；禁用写法由 BannedApiAnalyzers 和 G4 正则挡住；同一 ABI 下本机烘焙与 CI 参考逐字节一致（§2）。
5. **契约以源码为准**：§3只维护源码索引；改设计按WF-RULE §10同批迁完使用方与公开API记录、schema和文档，不保留旧接口。
6. **几何在本机烘焙**：几何一律由 gen 的 `Build()` 生成（经批准的英雄资产除外）；L0–L3 格包等在玩家机器上第一次需要时烘焙进本机缓存，用自定义 `.tbin`，不用 Godot 资源格式；L4 和全图常驻小数据随启动集发；贴图是 PNG 源图加 Godot 导入（§4、D-165）。
7. **生产入口唯一**：未交付能力可用显式桩；真实实现接入时，同批移除被替代的生产桩和旧执行入口。测试替身不带运行时注册，不能代替正式核心验收（§2、§5）。
8. **一个代理一个工作树**：多个代理同时写代码时，各用一个工作树和本地分支 `wp/<xx>-<slug>`，不推送；合并由主会话批准、值班执行者排队执行（WF-MERGE）。
9. **目标框架 net10.0**：引擎锁定 Godot 4.7.2-stable .NET 版，契约语言版本 C# 14；D0 跑通构建到导出版启动的全链路，不通时报作者（§3、§7、D-177）。

### 0.1 接入要点

- **新建或修改文件**：只改 `ownership.json` 划给本包的路径；共享文件（`project.godot`、`*.csproj`、`Directory.*.props`、契约、`ownership.json`、`docs/design/` 等）向 Astra 申请（WF-BUILD §1、§2）
- **写纯 .NET 代码（契约、gen、烘焙子任务）**：不引用 GodotSharp，不碰文件路径，数据由内核读成字节交来；gen 不引用其他内容程序集，不依赖运行时接口（清单见 §2"ArchUnitNET 规则"）
- **内核、风格、音频以外的 Godot 侧包要显示、用材质或出声**：显示经 `IRenderHost`，材质经 `ToonSpec`，声音经 `IAudioService` 等音频服务；不自己创建 `MeshInstance3D`、`ShaderMaterial`、`AudioStreamPlayer`（§2）
- **接入新模块、节点、服务或存档段**：打 `[ContentModule]`、`[Part]`、`[InteriorProvider]`、`[KernelNode]`、`[KernelService]`、`[QualityResponsive]`、`[SaveSection]` 等特性，由注册器自动发现；不加 Autoload，不改 `main.tscn`（§2"自动发现"）
- **写随机、计时、哈希、三角函数或色值**：禁用清单以 §2 的 BannedApiAnalyzers 和 G4 正则为准；随机用 `Rng`，种子用 `StableHash`，gen 与烘焙子任务的超越函数用 `DetMath`，遍历字典先排序；不写十六进制色值、着色器 `TIME`、`.gd`、手写 `uid://`（§2）
- **读写 `user://`**：每个文件只有一个读写方；任何代码都不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`（§2）
- **用到契约**：签名查 §3 对应的块；会追加值的枚举写穷举 `switch`；JSON 一律用 `ContractJson.Options`（§3）
- **要改契约，或加全局着色器参数、着色器文件、程序集引用、NuGet 包**：向主代理申请，由主代理改（§3、D-134）
- **依赖的包还没交付**：照桩开发；本区特有的部件建在自己目录里，ID 加区名前缀，证明能复用再申请提升（§5）
- **加贴图、模型、字体或音频**：区域包不自带大于 512² 的贴图，向 WP-04 申请图集槽位或数组层；外部模型先经主代理批准，放 `game/assets/hero/`；每个资源登记许可、脚本、哈希和体积（§4）
- **多个代理并行写代码时**：用 `scripts\wt\new.ps1` 开工作树，在本地分支小步提交，不推送、不碰 `main`；`main` 有新合入后 `git rebase main`（WF-MERGE §4）；只走命令行，不在编辑器里重存场景（§6）

## 1 资源管线迁移约定

资源管线以 `pipeline/` 为现行实现与数据位置；旧网页栈迁移记录不再在此维护。管线输入和输出遵守以下约定（WP-08；WP-47、WP-04、WP-37、WP-44 共用）：

- 同一份已提交输入在任何工作树重建，输出逐字节相同。
- 快照日期取自随快照提交的元数据，不读文件修改时间。
- 默认离线生成；刷新快照须显式执行，并与元数据一并提交。
- Python 依赖按 `pipeline/requirements.lock` 锁定；实体 ID 遵守 CORE-CONVENTIONS §3 的稳定规则。

## 2 目录结构与归属

### 架构边界与唯一生产链

本节规定模块如何协作；接口签名、格式与参数以源码和数据为准。整治遵循WF-RULE §10，视觉与生成结果保持，行为修正单列评审；RPG扩展限ENG-ROADMAP已经设计的边界。

| 层 | 责任与依赖 |
| --- | --- |
| 契约与纯值 | 描述数据、命令、只读状态及确定性工具；不持有Godot对象、文件I/O或全局运行状态。 |
| 世界与公共几何 | 解释冻结的地图输入，提供权威查询和共享几何操作；同级内容包不互相引用实现。全局输入与按图输入保留各自语义。 |
| 内容生成与生产管线 | 通过显式上下文生成几何、碰撞和结构化诊断；相同输入的生成顺序和数值运算顺序确定。共用算法放公共层，区域仅保留内容选择与组合。 |
| 领域服务与编排 | 每类世界状态只有一个写入方；启动、换图、读档与退出由正式编排入口驱动。注册器负责装配，业务逻辑依赖明确接口，不自行发现另一套服务或重读输入。 |
| 引擎与表现适配 | 主线程挂载、界面与音频适配消费领域状态和生成产物；原生资源创建、租约与释放集中在所属内核服务，表现层不成为地图或状态的第二权威。 |

- **一次读取与发布**：地图加载发布同一版本的上下文，消费者显式接收冻结输入和目录；界面读取服务发布的展示数据，不另读同一份地图文件。缓存身份沿生产机制失效，不保留退役格式的读取旁路。
- **可写与只读边界**：构建阶段可写，进入共享缓存或跨线程发布时冻结；发布方不把缓存持有的可写数组交给调用者。几何合并、变换与打包保留全部受支持通道及原顺序。
- **线程交接**：后台任务只持纯数据；主线程接收时确认地图与任务代际。渲染帧值及其资源租约作为同一次交接发布和接收，替换、消费、取消均有唯一释放方。
- **生命周期**：应用、地图、节点与一次操作分别拥有其资源、订阅和异步工作；卸图或退树终止等待、解除订阅并阻止迟到结果发布。服务可分解为明确职责的内部组件，不另建平行状态机或全局事件通道。
- **工具与验证**：构建、导入、截图和测试共用资源调度及进程生命周期入口；配置与真实执行目标一致。测试按本包语义及独占夹具验证，不锁其他内容包可合法改变的生产全局集合；固定原始快照的本包回归不因此删除。
- **替换即清旧**：切换生产实现时，全部调用者、测试、脚本和相关说明同批迁移；删除旧入口、重复读取与失效断言。只有WF-RULE §10规定的短暂过渡可例外，并在同一任务结案前删除。

### 目录树

Godot 项目放在子目录 `game/`（即 `res://`），Godot 只扫描这个目录，不碰 docs、Python 和原始数据。

```
UESTC_RPG/
├─ game/                     Godot 项目根（res://）：project.godot、Campus.sln、Campus.csproj（主程序集，Godot 侧代码）、
│                            Campus.Tests.csproj、Campus.Gen.Tests.csproj、export_presets.cfg、.editorconfig、
│                            Directory.Build.props、Directory.Packages.props、packages.lock.json（主代理独占）；
│                            override.cfg（每个工作树生成，不进 git）
│  ├─ contracts/             WP-00  Campus.Contracts：纯 .NET 契约程序集（接口、record，用 System.Numerics，不引用 GodotSharp），带 PublicAPI.*.txt；
│  │                         子目录 Quality/、Maps/、Atlas/、Flow/、Cinematic/、Save/、Audio/、Bake/、Parts/、schemas/；Entities/ 是 RPG 预留，观光期不写入
│  ├─ gen/                   纯 .NET 的生成器程序集（Campus.Gen.*）：部件库、建筑、地面、各区、设计增补、植被布点、碰撞图元、可行走数据、
│  │                         室内平面与家具、外围、声学与声景
│  │  ├─ world/              WP-08  世界模型（读 campus.json、覆盖层、外围数据、室内索引、空间索引）
│  │  ├─ parts/{core,openings,circulation,roof,street}/   WP-09–13；parts/_stubs 归 WP-00
│  │  ├─ buildings/  ground/  diorama/                    WP-14、WP-15、WP-22
│  │  ├─ zones/{core,library-interior,south,west,north,center-east,design-*,showcase-interiors}/   WP-16–21、WP-28–31
│  │  │                      library-interior 下有 museum/（WP-17）；showcase-interiors 下分 south/、west-east/、north/（WP-31a、31b、31c）
│  │  ├─ interior/{core,shell}/  interior-props/          WP-51（51a 平面内核、51b 壳体）、WP-52（室内家具与生活痕迹，WLD-INTERIORS）
│  │  ├─ outskirts/          WP-47  外围环境带（WLD-OUTSKIRTS）
│  │  ├─ acoustics/  ambience/                            WP-53c（声学网格烘焙）、WP-53b（发声器来源与声景场，SYS-AUDIO）
│  │  ├─ vegetation/  walkable/  collision/               WP-23（布点与母模型）、WP-44、WP-43（图元）
│  │  ├─ water/  fauna/  traces/  furniture/              WP-24、WP-25、WP-25、WP-13（摆放）
│  │  └─ <各包>/tests/       测试跟着目录归属走
│  ├─ kernel/                Godot 侧内核（C#）：boot、renderer、frame、inspect（WP-01）；bake（本机烘焙服务与缓存，D-165）、lod、stream（WP-02）；
│  │                         lighting（WP-03）；post（WP-05）；quality（WP-32）；occlusion（WP-33）；world（WP-34）；collision（WP-43）；registry（WP-00）；
│  │                         map（WP-46，SYS-MAPS）；save（WP-50，SYS-SAVE）
│  ├─ flow/                  WP-49a  IAppFlow 与开场流程（SYS-TITLE）
│  ├─ entities/              WP-00  RPG 预留（角色与实体层，ENG-ROADMAP §4），观光期不写入
│  ├─ audio/                 WP-53  core（53a）、ambience（53b）、acoustics（53c）、foley（53d），命名空间 Campus.Audio.*（SYS-AUDIO）
│  ├─ style/                 WP-04  shaders/（toon.gdshaderinc、白名单着色器、C# 材质库）、palette/（运行时调色板）、palette/keys（WP-37）、
│  │                         palette/ext（调色板扩展，逐文件归属）、textures/；style/outline（WP-05：CompositorEffect 与 .glsl）
│  ├─ camera/                WP-06；camera/transit（WP-36）；camera/walk（WP-42）；camera/cinematic（WP-49a，CinematicPlayer）
│  ├─ medium/                WP-07；medium/interior/：室内运行时（WLD-INTERIORS §6）
│  ├─ systems/               sky（WP-40）、vegetation（WP-23 运行时）、vegetation/seasons 与 seasonfx（WP-38）、water（WP-24）、
│  │                         fauna|traces（WP-25）、furniture（WP-13 运行时）、motion（WP-26）、nightlight（WP-39）、weather（WP-45）、atlas（WP-48c，SYS-ATLAS）
│  ├─ hud/                   WP-35（style、motion、widgets、pages、gallery、i18n，UI-VISUAL §11.2）；hud/map 归 WP-48b（SYS-ATLAS）
│  ├─ stage/                 WP-01  main.tscn（唯一的共享场景，只放内核根节点）
│  ├─ gates/                 WP-27  截图机、路线回放、巡检的引擎内驱动，只在带门禁参数时启用；gates/matrix 归 WP-41
│  ├─ data/                  运行时 JSON（按图的数据都在 maps/ 下，SYS-MAPS §2.1）：
│  │  ├─ maps/index.json     WP-46  地图清单、默认地图
│  │  ├─ maps/qingshuihe/    map.json；campus.json、network.json、zones.json；overlay/（<组>.d/ 分片目录、walkable、outskirts、fidelity，
│  │  │                      以及 overlay/interior/<区>.interior.json、overlay/terrain/<区>.terrain.json）；outskirts.json；fidelity.json；interiors.json；waypoints.json；lighting-schedule.json；
│  │  │                      backdrop.json；sky.json；review.json；cinematics/<id>.json；atlas/（markers.json、labels.json）；
│  │  │                      audio/（声景与声学的本图数据）；nav/（routes.json、views.json）；incidents.json（偶发事件清单）；
│  │  │                      entities/（RPG 预留，观光期不写入）
│  │  ├─ weather/<剧本>/     WP-45a  天气剧本库，清水河用 chengdu
│  │  ├─ audio/              WP-53  全局音频数据（声音集、配方、观者声样本组、界面音、音乐、许可清单）
│  │  ├─ vegetation/         WP-23  树种表 species.json（全局，各图通用，WLD-VEGETATION §2）
│  │  ├─ rulesets/           WP-00  规则集（sightseeing.json；schema 在 contracts/schemas/，§3）
│  │  ├─ text/               WP-00  RPG 预留（文本库），观光期不写入
│  │  └─ quality-presets.json   WP-32  画面预设（全局）
│  ├─ assets/                style/（贴图、图集，WP-04；按图的远景卡片在 style/backdrop/<地图>/）、hud/（字体、图标，WP-35）、
│  │                         maps/<地图>/thumbs/（标记缩略图，WP-27）、title/<地图>/（扉页与云卡贴图，WP-49b）、audio/（WP-53e）、
│  │                         hero/（经批准的英雄资产，逐文件归属）
│  └─ addons/                第三方插件（gdUnit4 等），主代理维护，登记在 third_party.json
├─ pipeline/                 原 scripts/data（Python OSM 管线，WP-08；raw/<地图>/ 归 WP-08；walk/、nav/ 子目录 WP-44）、assets/（贴图生成脚本，WP-04）、
│                            colorscript/（WP-37）、outskirts/（WP-47）、map/（底图烘焙器，WP-48a）、audio/（素材管线，WP-53e）
├─ scripts/                  gate.ps1（统一入口）与 gates/（门禁编排，WP-27；matrix 子目录 WP-41）、wt/（工作树脚本，WP-00）、
│                            devices/（核显实测与设备登记，WP-32）、godot-api/（API 参考生成，主代理）、docs/（文档总索引生成与校验，主代理）
├─ tools/godot-version.txt   锁定的版本串 4.7.2.stable.mono.official.ed1daf0bf
├─ docs/                     design/、godot-api/
└─ version.txt（版本号，§4 构建信息）、global.json、ownership.json、budgets.json、third_party.json、.gitattributes、.gitignore
```

### 生成物与资源位置

不进 git 的位置：仓库外的工作树 `..\UESTC_RPG.wt\<WP-xx>\`、GPU 锁 `..\UESTC_RPG.wt\.locks\`、开发和 CI 的烘焙缓存 `..\UESTC_RPG.cache\bake\<地图>\`（§4，按图分目录；玩家机器上的本机缓存在 `user://cache/bake/`，不在仓库里）；仓库里的导出产物 `build/`、日志与截图 `output/`。音频的母带与素材（`*.wav`、`*.ogg`）进 Git LFS；仓库公开时，只许随游戏发布、不许公开分发的素材（如 Sonniss）放私有素材库（SYS-AUDIO §14.3）。

### 归属依据

**目录归属**以仓库根目录的 `ownership.json` 为准；本节目录树只说明结构，不重复逐路径归属表。

### 程序集边界

**程序集边界**（编译器能挡的交给编译器，剩下的由 G4 的 ArchUnitNET 规则和 BannedApiAnalyzers 检查）

| 程序集 | 目录 | 可以引用 | 不得引用 |
| --- | --- | --- | --- |
| `Campus.Contracts` | `game/contracts` | 只有基础类库（`System.Numerics`、`System.Text.Json`） | GodotSharp，以及本仓库的任何其他程序集 |
| `Campus.Gen.World` | `gen/world` | 契约 | GodotSharp、其他 gen 程序集 |
| `Campus.Gen.Parts.Core` | `gen/parts/core` | 契约 | 世界模型、GodotSharp |
| `Campus.Gen.Parts.<类>` | `gen/parts/<类>` | 契约、部件内核 | 世界模型、GodotSharp、其他部件包（跨包复用走部件 ID） |
| `Campus.Gen.Interior`（室内共享层） | `gen/interior/core` | 契约、部件内核 | 世界模型、GodotSharp |
| `Campus.Gen.InteriorProps`（室内共享层） | `gen/interior-props` | 契约、部件内核、`Campus.Gen.Interior` | 世界模型、GodotSharp |
| `Campus.Gen.<内容>`（buildings、ground、diorama、zones.*、vegetation、water、fauna、traces、furniture、walkable、collision、outskirts、interior.shell、acoustics、ambience） | `gen/<内容>`（interior.shell 在 `gen/interior/shell`） | 契约、部件、室内共享层、世界模型（只读） | GodotSharp、其他内容程序集（区与区不互相依赖） |
| `Campus.Pipeline.Map`（WP-48a） | `pipeline/map` | 契约、`Campus.Gen.World`（只读） | GodotSharp |
| `Campus`（主程序集） | kernel、style、camera、medium、systems、hud、stage、gates、flow、audio | GodotSharp、契约、全部 gen 程序集、`Campus.Pipeline.Map` | — （内部按命名空间受下面的规则约束） |
| `Campus.Gen.Tests`、`Campus.Tests` | 各包的 `tests/` | 被测程序集、gdUnit4Net | — |

- 每个 gen 目录一个 csproj，D0 由 WP-00 全部建好。`Campus.csproj` 用通配引用 `gen/**/*.csproj`，另引用 `pipeline/map` 的 `Campus.Pipeline.Map`，并用 `Compile Remove` 去掉 `contracts/`、`gen/`、`**/tests/`。新增文件不改任何共享文件；需要新增程序集引用时，向主代理申请。ENG-WP §3 拆出的子包共用原包的程序集，不新建 csproj。例外：WP-51 有两个程序集（`gen/interior/core`、`gen/interior/shell`），WP-53 有两个（`gen/acoustics`、`gen/ambience`），都由 WP-00 在 D0 建好；`showcase-interiors` 下的 south、west-east、north 三个子目录共用一个程序集。
- `Campus.Pipeline.Map` 是纯 .NET 类库，随包程序集一起发：本机烘焙要在发布版里调用它（D-165），主程序集引用它，它也受 §2"BannedApiAnalyzers"里的确定性禁用写法约束。`pipeline/outskirts`、`pipeline/audio` 是 Python 脚本，不进导出包，共用 `pipeline/requirements.txt` 的环境（§1）。
- 测试统一用 gdUnit4Net，经 `dotnet test` 运行。纯 .NET 的测试默认不启动引擎；需要引擎的测试标 `[RequireGodotRuntime]`，由 `gate.ps1` 把 `UESTC_GODOT` 指向的引擎交给测试适配器。各包的测试源码放在自己目录的 `tests/` 里，分别编进 `Campus.Gen.Tests`（gen 和契约的测试）和 `Campus.Tests`（Godot 侧的测试），用测试所在目录区分归属。存档容器、校验与迁移链写成不碰 GodotSharp 的纯 C# 类（`game/kernel/save/format/`），也用 `dotnet test` 直接跑（SYS-SAVE §14）。
- `build()` 拿不到世界状态：`BuildContext`（§3）里没有 `IWorldState`，gen 程序集也引用不到内核。随时间、季节和天气变化的东西，只能通过调色板、灯光通道、变体掩码和天气类全局参数表达（ENG-BUDGET §7）。`build()` 也拿不到画面设置：烘焙产物不随预设变（SYS-GRAPHICS §0 第 4 条）。`build()` 能拿到的地图信息只有 `BuildContext.Map`（`MapDescriptor`，纯数据）。

### 命名空间依赖规则

**ArchUnitNET 规则**（只靠命名空间才能判断的，写在 `scripts/gates/arch/`，归 WP-00；增删规则要主代理批准）：

- gen 程序集不得依赖契约里的运行时接口：`IWorldState`、`IWorldControl`、`IWeatherProcess`、`ICameraRig`、`IViewControl`、`ICollisionWorld`，以及 `Campus.Contracts.Quality` 整个命名空间（它们和内容契约在同一个程序集里，编译器挡不住）；同样不得依赖：`IMapService`、`IMapContext`、`IMapTransition`、`IMapAware`；`IAtlasService`；`IAppFlow`；`IBakeService`（本机烘焙服务只由内核调用）；`ISaveService` 与 `Campus.Contracts.Save` 整个命名空间；`IVeilState`、`IVeilService`、`IMotionSettings`、`IPaletteProbe`、`IInteriorDoors`；`IRuleset`、`IObserverState`（烘焙产物不随规则集变）；音频的运行时接口 `IAudioService`、`IAmbienceService`、`IAcousticEnv`、`IFoleyService`、`IGroundCover`、`IWalkTelemetry`、`IMediumState`、`ILightningFeed`、`IChannelReader`。gen 可以引用声明类的类型：`MapDescriptor`、`AmbientEmitterDecl`、`SoundSocketDecl`、`AcousticRoomDecl`、`AcousticPortalDecl`、`AcousticsDecl`、`SoundSetId`、`AmbienceProfileId`、`FootMat`、`StairSpec`、`FootPatch`、`DoorLeafDecl` 和导航图的记录（SYS-AUDIO §16、WLD-INTERIORS §15）。
- 内容程序集之间互不依赖，csproj 的引用已经挡住，这里再查一遍。区域包要用通用建筑的立面规则，就向 WP-14 申请把它提升到部件内核。室内共享层不算内容程序集，可以被内容程序集引用；共享层之间只许 `InteriorProps → Interior` 一个方向（WLD-INTERIORS §18）。
- **多地图**（SYS-MAPS §7）：归属不同图的程序集互不引用；通用程序集不得引用任何带 `MapScope` 的程序集（csproj 已经挡住，再查一遍）。
- 只有 `Campus.Kernel.*` 能创建 `MeshInstance3D`、`MultiMeshInstance3D`、`OccluderInstance3D`，或调用 `RenderingServer.InstanceCreate*`、`RenderingServer.MeshCreate*`。其余 Godot 侧包要显示东西，一律经内核的 `IRenderHost` 申请，内核按包记账（ENG-PERF §12.2 的归因）。
- 只有 `Campus.Style.*` 能创建 `ShaderMaterial`，而且只能用白名单里的着色器。其余代码只能经 `ToonSpec` 取材质（§3）；界面的 `ui_panel`、`ui_screen` 和地图的 `map_base` 也由界面、地图向 `Campus.Style` 的材质库申请（UI-VISUAL §11.5、SYS-ATLAS §13）。
- `Campus.Hud` 除 GodotSharp 外只能依赖契约（读 `IWorldState`，写 `IWorldControl`、`IViewControl`、`ICameraRig.Transit`；画面页只经 `IQualityService`）和 `Kernel.Get<T>()`；不能依赖内核和其他包的内部类型。`HudModel` 是 WP-35 自己注册的 Kernel 服务。此外：
  - 读 `IPaletteProbe`；实现并注册 `IMotionSettings`；可以经 `RenderingServer.GlobalShaderParameterSet` 写全局参数 `ui_night`，只此一项（UI-VISUAL §11.6）；
  - `IAppFlow`：读状态，调 `Enter`、`ReturnToTitle`、`Quit`、`Skip`（SYS-TITLE §7.4）；放映层接口由 WP-35d 实现、`CinematicPlayer` 调用；
  - `ISaveService`：开始页、存档页只读存档位列表和缩略图；写入只经 `IAppFlow`（SYS-SAVE §10）；
  - `IMapService`：只读，取当前图的名字和"回到 ××"的按钮名（SYS-MAPS §5）；
  - `IAudioService`：界面音和"声音"页（SYS-AUDIO §16）；
  - `IBakeService`：设置卡的"存储"行读 `Progress`、`CacheBytes`，"清除缓存"调 `Clear()`（D-165）；失败页读 `IBakeHandle.Error`，"重试"调 `ClearFailures()`（ENG-PERF §5）；
  - `IRuleset`：只读，世界控件按 `WorldControls` 显示可用或禁用态（§3 规则集块）。
- `Campus.Hud.Map`（`game/hud/map`，WP-48b）除 GodotSharp 外只依赖契约、`Kernel.Get<T>()`、`IAtlasService`、`IWorldState`（读）、`IObserverState`（读：玩家箭头与所在区域）和 `ICameraRig.Pose`（读：视锥）；传送一律经 `IAtlasService.Travel`，不直接调 `ICameraRig.Transit`。`Campus.Systems.Atlas` 可以调 `ICameraRig.Transit`、`ICameraRig.LockInput`、`IViewControl`、读室内索引（`IWorldModel.Interiors`）和 `IObserverState`（`Proximity` 判定），只在目标在别的图时调 `IMapService.Travel`（SYS-ATLAS §11）。
- 计时只能在 `Campus.Kernel.Frame`、`Campus.Kernel.Quality`、`Campus.Kernel.Inspect`、`Campus.Kernel.Save`（保存耗时与 G10 计数）和 `Campus.Gates` 里出现（`Time.GetTicks*`、`Stopwatch`）。界面的 0.1 s 采样由 `HudModel` 按帧间隔累加实现，不用 `Timer` 节点；音频也不自己计时，监视项登记在 `Campus.Kernel.Inspect`（SYS-AUDIO §18.1）。
- **存档**（SYS-SAVE §9）：只有 `Campus.Kernel.Save` 能读写 `user://saves`；任何代码都不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`，没有例外。本机烘焙缓存 `user://cache/bake/` 只有 `IBakeService`（`Campus.Kernel.Bake`）读写，用自定义的 `.tbin` 格式和 `FileAccess`（或 .NET 流），不经 Resource 机制：`user://` 下的 `.res`、`.tres` 能嵌脚本、读入即执行（D-165，§4）。
- **音频**（SYS-AUDIO §16）：只有 `Campus.Audio.*` 能创建 `AudioStreamPlayer`、`AudioStreamPlayer2D`、`AudioStreamPlayer3D`，能调用 `AudioServer.Set*`、`AddBus*`、`RemoveBus*`，其中 `SetBusMute`、`SetBusVolumeDb` 只许 `Campus.Audio.Core`；`AudioListener3D` 只由 WP-06 的相机创建；声学用的 `Area3D` 只有 `Campus.Kernel.Collision` 能建。其他包要出声，一律经 `IAudioService`、`IAmbienceService`、`IFoleyService`。
- 系统包之间只经契约和 Kernel 服务交互。例外：`Campus.Systems.Vegetation.Seasons` 可以引用 `Campus.Systems.Vegetation` 公开的着色器挂点。

### 禁用写法与确定性

**BannedApiAnalyzers**（全程序集一律禁止的写法，编译期报错；清单写在各程序集的 `BannedSymbols.txt`，由 WP-00 维护）：

- 随机数：`System.Random`、`GD.Randf`、`GD.Randi`、`GD.RandRange`、`Godot.RandomNumberGenerator`。一律用契约里的带种子生成器。唯一登记的例外（G4）：新游戏时由 `ISaveService.NewGame` 取一次熵作本存档的天气种子，写进日志和存档；`--weather-seed` 给出时不取；读档时种子来自存档（SYS-WEATHER §6.4、SYS-SAVE §5.4）。
- 时间：`DateTime.Now`、`DateTime.UtcNow`、`Environment.TickCount`、`System.Threading.Timer`、`Time.GetUnixTimeFromSystem`、`Time.GetDatetime*FromSystem`；gen 里另禁 `Stopwatch`、`Parallel`、`Task.Run`（并行只在格与格之间，由内核调度）。登记的例外：`Campus.Kernel.Save` 读一次系统时钟，只写进 meta 的 `savedAtUnix`，只用于显示（SYS-SAVE §6）。
- 不确定的哈希：显式调用 `string.GetHashCode()`（.NET 每个进程的字符串哈希种子不同）、`Guid.NewGuid`。需要新 ID 的地方用按存档递增的整数，例如平面地图的自定义标记（SYS-ATLAS §11、本文 §3）。
- **确定性（gen 与烘焙子任务，D-165）**：`Math`、`MathF` 的超越函数（`Sin`、`Cos`、`Tan`、`Asin`、`Acos`、`Atan`、`Atan2`、`Exp`、`Log`、`Log10`、`Pow`、`Cbrt` 等；它们走系统数学库，结果可能因 Windows 版本而异）一律改用契约里的 `DetMath`（WP-00 提供，纯 C# 实现，有单元测试的误差界）；`System.Numerics.Vector<T>` 和 `System.Runtime.Intrinsics`（向量宽度随 CPU 变）、`Math.FusedMultiplyAdd`、`TensorPrimitives` 一律禁用。`System.Numerics` 里内部走三角函数的构造也一律禁用：`Matrix4x4.CreateRotationX/Y/Z`、`CreateFromAxisAngle`、`CreateFromYawPitchRoll`，`Quaternion.CreateFromAxisAngle`、`CreateFromYawPitchRoll`、`Quaternion.Slerp`，以及其他涉及三角函数的方法（`Vector2`、`Vector3` 的 `Lerp` 不在此列），改用契约里基于 `DetMath` 的同名工具（`DetMath.RotationY(…)` 等，WP-00）。`Math.Sqrt`、加减乘除和 `Math.Round` 是 IEEE 精确运算，可以用。范围是 `Campus.Gen.*`、`Campus.Pipeline.Map`，以及其他在发布版里由 `IBakeService` 调用的烘焙子任务代码。
- 材质与着色器：`StandardMaterial3D`、`ORMMaterial3D`、`new Shader()`；运行时改 `render_mode` 或 `#define` 的写法。
- `RenderingServer.GlobalShaderParameterGet`（禁止从脚本读回全局参数）；给 `Engine.TimeScale` 赋值（固定为 1）。
- **存档与设置**（SYS-SAVE §9）：在 `Campus.Kernel.Save` 和实现 `ISaveSection` 的类里，禁用 `ConfigFile`、`GD.StrToVar`、`GD.BytesToVar`、`GD.BytesToVarWithObjects`、`FileAccess.GetVar`、Godot 的 `Json`，以及不带 `JsonTypeInfo` 的 `JsonSerializer` 重载。存读档一律用 System.Text.Json 源生成；Godot Resource（`.tres`、`.res`）不得用来存档（Resource 里嵌入的 GDScript 会被执行）。
- **音频**（SYS-AUDIO §16）：`AudioStreamRandomizer`（整个类型）；`AudioStreamPlaylist.Shuffle` 的 setter 与 `SetShuffle`；`AudioServer.PlaybackSpeedScale` 的 setter 与 `SetPlaybackSpeedScale`。

**另外由 G4 用正则检查的**：

- 直接写的十六进制色值（`#RRGGBB`、`Color.FromHtml`、`new Color("…")`），只有调色板数据文件可以有（界面色值只在 `ext/ui.json`，UI-VISUAL §11.3）。
- 着色器里出现 `TIME`（一律用全局参数 `anim_time`）。
- 着色器的 `vertex()` 里用 `CAMERA_POSITION_WORLD` 做距离判断。阴影通道里它是灯的位置，一律改用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`。
- GDScript 文件（`.gd`），`addons/` 里第三方插件自带的除外。
- `stage/main.tscn` 以外的共享 `.tscn`、`.tres`；代码里手写的 `uid://`。例外：`game/hud/ui_theme.tres` 是 `UiThemeBuilder` 的导出产物，只作预览与差异审查，运行时不读，G5 比对它与生成器一致（UI-VISUAL §11.3）。
- 存档、多地图与音频：
  - 字面量 `user://saves` 只能出现在 `Campus.Kernel.Save`；`ResourceLoader.Load(`、`ResourceSaver.Save(` 的参数以 `user://` 开头时一律拒绝（SYS-SAVE §9），没有例外；字面量 `user://cache/bake` 只能出现在 `Campus.Kernel.Bake`（D-165）；
  - 通用程序集、内核、系统、界面代码里不得出现完整的实体 ID 字面量（形如 `"<前缀>:<类型>:<编号>"`、`"design:…"`、`"walk:…"`）、地图 ID 字面量和 `res://data/maps/…` 路径字面量；例外只有 `kernel/map`（读 `index.json`）和门禁的夹具（SYS-MAPS §7）；
  - 音频代码里不出现 `Timer`、`Time.GetTicks*`（SYS-AUDIO §16）。

**确定性**（G2 的"同一输入哈希一致"靠这几条保证）：

- 种子 = `StableHash.Hash32(世界种子, featureId, partId, 用途)`，与生成顺序无关（ENG-PERF §5）。世界种子取 `BuildContext.Map.Seed`（SYS-MAPS §3）。凡是"放不放、选哪种"的判断，一律用整数哈希；三角函数只用来算坐标，且一律用 `DetMath`（§2"BannedApiAnalyzers"）。
- 生成器里不依赖 `Dictionary`、`HashSet` 的遍历顺序；需要遍历时先按 ID 排序。
- 一个 `build()` 内部不做并行的浮点归约。
- **跨机器逐字节一致**（D-165）：同一 ABI 下，本机烘焙的产物与 CI 参考烘焙逐字节一致，按每个文件的内容哈希比。靠本节的种子规则、`DetMath` 和 BannedApiAnalyzers 的禁用写法保证；G6 在集中测试时用 `--bake-verify` 抽样比对，并全图逐字节比对（ENG-GATES §2）。
- 天气过程的每次抽取用计数器子流 `StableHash(种子, mapId, epoch, 片段号, 尝试号, 用途)`（种子每个存档一个，SYS-MAPS §5），与补队列的时机和帧率无关（SYS-WEATHER §6.4）。运行时的确定性是：同一钟点、同一哈希的东西结果相同；同一构建 + 同一种子 + 同一串输入得到同样的画面。时间不能逆转，所以不要求"倒回去复现"。
- **读档**：读档是换一个世界实例（`IWorldState.Instance` 加 1），不算倒回去。续跑等价：同一构建下，在安全时刻拍的存档读回后给相同输入，世界和天气与没退出时逐帧一致（SYS-SAVE §5.3）；过渡和偶发事件不续播，声景不进存档（SYS-AUDIO）。
- **音频**（SYS-AUDIO §17）：音频的随机一律用契约的 `Rng` 与 `StableHash`：音频种子 = StableHash(`--seed`, "audio")，每个发声器、每个排程槽一条子流；Movie Maker 下同一组参数两次录音逐字节相同。

### 自动发现与模块接入

**自动发现**：内核启动时用反射扫描下列特性，按 ID 排序后注册；ID 重复由 G5 报错。新模块打上特性就能接入，不需要改任何共享文件。

| 特性 | 标在 | 所在程序集 | 在哪里运行 |
| --- | --- | --- | --- |
| `[ContentModule("zone.south", Owner = "WP-18")]`；例：`[ContentModule("interior.shell", Owner = "WP-51b")]` | 实现 `IContentModule` 的类，文件名 `*Module.cs`；可带 `Map`，没写就继承程序集的 `MapScope` | gen | 线程池或烘焙器（纯函数） |
| `[Part("opening.window.casement", Owner = "WP-10")]` | 实现 `IPartDef<TParams>` 的类 | `gen/parts` | 线程池或烘焙器（纯函数） |
| `[InteriorProvider("interior.library", Owner = "WP-17")]` | 实现 `IInteriorProvider` 的类（按层，WLD-INTERIORS §15）；可带 `Map` | gen | 线程池或烘焙器（纯函数） |
| `[AmbientEmitter("ambience.water", Owner = "WP-53b")]` | 实现 `IAmbientEmitterSource` 的类；输出进本图的 `ambience.bake`。部件的声音插座走 `IPartDef.Sounds()`，室内的声学走 `IInteriorProvider.Acoustics()`，不另设特性；动态发声器在运行时注册（SYS-AUDIO §19） | gen | 烘焙器（纯函数） |
| `[MediumPresenter("medium.interior", Owner = "WP-07")]` | 实现 `IMediumPresenter` 的类 | 主程序集 | 主线程 |
| `[KernelNode("systems.water", Owner = "WP-24")]` | Godot 侧包的根节点（`Node` 子类）。内核按 ID 顺序实例化，挂到 `main.tscn` 的内核根节点下；动态物体由它负责。可带 `Map`：带 `Map` 的在该图 Mount 时实例化、Unload 时释放；不带的跨图常驻，需要换数据的实现 `IMapAware`（SYS-MAPS §7） | 主程序集 | 主线程 |
| `[KernelService(typeof(IWorldState), Owner = "WP-34")]`；例：`[KernelService(typeof(IAtlasService), Owner = "WP-48c")]` | 服务的实现类。注册成 Kernel 的服务，用 `Kernel.Get<IWorldState>()` 取 | 主程序集 | 主线程 |
| `[QualityResponsive(Owns = [...], Reads = [...])]` | 实现 `IQualityAware` 的 `[KernelNode]` 类（SYS-GRAPHICS §8）。G5 核对每个 `QualityKnob` 恰有一个 `Owns`，且与 `KnobSpec.OwnerWp` 一致 | 主程序集 | 主线程，`FramePriority.Quality` 那一步 |
| `[SaveSection("world", Owner = "WP-34", Version = 1, Phase = RestorePhase.World, Order = 10)]` | 实现 `ISaveSection<T>` 的 `[KernelService]` 或 `[KernelNode]` 类；`Scope = SaveScope.PerMap` 的段在文件里展开为 `map:<id>/<段>`。G5 查 `Phase` 与 SYS-SAVE §2 的表一致、`Order` 不重复、`Owner` 与 `ownership.json` 一致（SYS-SAVE §10） | 主程序集 | 主线程：`Capture` 在 `FramePriority.Save` 那一步，`Restore` 在读档那一帧的同一步；迁移函数在后台线程 |
| `[assembly: MapScope("qingshuihe")]` | 各区程序集，由 WP-00 写进 csproj（见 §2 的 `MapScope` 条） | gen | 注册时求模块归属 |
| `res://style/palette/ext/*.json` | 调色板扩展，一包一个文件 | — | 启动时用 `ResourceLoader.ListDirectory` 扫描 |
| `res://data/maps/<地图>/overlay/<组>.d/*.json`、`overlay/{walkable,outskirts,fidelity}.overlay.json`、`overlay/interior/*.interior.json`、`overlay/terrain/*.terrain.json` | 覆盖层，通用的按包分片、专属格式一区一个文件（按图分目录） | — | WP-08 只扫描当前图的目录 |

- **Autoload 只有两个**，D0 冻结：`Boot`（WP-01，解析命令行参数）和 `Kernel`（WP-00，服务定位器）。需要新的全局对象时，用 `[KernelService]` 注册成 Kernel 的服务，不往 `project.godot` 里加条目。`IMapService`、`IAtlasService`、`IAppFlow`、`ISaveService`、`IAudioService` 等一律如此。
- **build 与 mount 分开**：生成在纯 .NET 里，可以在线程池或烘焙器里跑（发布版的本机烘焙就在工作线程上调 `Build()`，D-165）；Godot 侧的挂载和动态物体只在主线程。gen 里的内容包没有挂载：随时间动的东西一律通过通道（`ChannelDecl`、`PlacedPart.Emitter`）和着色器（风场、`anim_time`）表达，需要节点的动态物体由 Godot 侧包的 `[KernelNode]` 负责（SYS-MOTION）。开放室内里会动的门扇同样如此：gen 只声明门扇（`DoorLeafDecl`），转动与运行时门体由 WP-07、WP-43 负责（§3）。
- 注册器每次加载一张图，只启用"通用 + 当前图"的 `[ContentModule]`、`[InteriorProvider]`、`[AmbientEmitter]`；`[KernelService]`、`[MediumPresenter]`、`[Part]`、`[QualityResponsive]`、`[SaveSection]` 是全局的（SYS-MAPS §7）。`--gate=registry --map=*` 对每张图各查一遍。
- 反射只扫 `Campus` 和 `Campus.Gen.*` 程序集。编译器会丢掉没用到的程序集引用，所以注册器按构建时生成的程序集清单逐个显式加载，不依赖"已加载的程序集"。导出版里的反射行为由 P0 验证（§7）。

## 3 关键接口与契约源码

契约放在 `game/contracts/`（`Campus.Contracts` 程序集），分批定死（D-134，ENG-WP §5 第 3 条）：

- **候选**：WP-00 先交能编译的契约 v1 候选和全部桩（§5），各包照它开工。还没冻结的组仍可修改，改动一律向主代理申请，由主代理改。
- **按组冻结**：契约按下表分组。某一组的真实生产方与使用方在主线上接通（不再经桩）、相关核心专项通过（ENG-GATES §2）后，由主代理宣布冻结这一组，记进 WF-STATUS §1。
- **契约 21–23 的增量**（玩家身体、游泳、反应动物）：签名以源码为准；本节保留尚未落地能力及其边界。

契约变更按WF-RULE §10同批迁移；冻结状态用于协调使用方，不作为长期保留旧设计的理由：

- C#公开API由PublicApiAnalyzers检查，公开记录随已批准的设计与全部使用方同批更新；接口签名只在源码和分析器所需记录中维护。
- JSON格式以`game/contracts/schemas/`为准；改格式时同批迁移生产输入和读取方，持久化数据的迁移或重建安排需在该任务里明确，不隐式保留旧解析器。
- **枚举的穷举纪律**（ENG-ROADMAP §6 第 4 条）：枚举只在末尾追加值，JSON 按字符串存。会追加值的枚举（`ViewMode`、`AppState`、`MapKind`、`MapTravelCause`、`MarkerKind`、`SaveReason`、`SlotKind`、`TransitKind`、`VeilReason`、`DuckReason`、`AudioGroup`、`FoleyKind`、`QualityKnob`、`InputLockReason`、`InteractionRule` 等），消费方一律写穷举的 `switch`，`default` 分支拒绝或报错；不写 `mode == ViewMode.Fly ? … : …` 这样的二值判断。读到不认识的枚举字符串，按 SYS-SAVE §3.5 当作不合法。G1 的 `-warnaserror` 把非穷举 switch 表达式的警告当错误，二值判断靠评审。

契约签名与源码注释以 `game/contracts/` 为准。当前尚无冻结组；冻结状态以 WF-STATUS §1 为准。下表列出契约组的源码位置与尚未落地的源码类型。

### 契约组与源码

| 契约组 | 源码目录 | 冻结状态 | 源码未有类型 |
| --- | --- | --- | --- |
| 基础类型与确定性工具 | `game/contracts/Foundation/`、`game/contracts/Determinism/` | 候选 | — |
| 内容模块、部件与风格 | `game/contracts/Foundation/`、`game/contracts/Parts/`、`game/contracts/RuntimeBridge/` | 候选 | — |
| 世界状态与覆盖层 | `game/contracts/World/`、`game/contracts/WorldModel/`、`game/contracts/schemas/` | 候选 | — |
| 相机、介质、观察模式与碰撞 | `game/contracts/Spatial/` | 候选 | — |
| 室内与点位 | `game/contracts/Interiors/`、`game/contracts/Waypoints/` | 候选 | — |
| 画面设置与规则集 | `game/contracts/Quality/`、`game/contracts/Foundation/Rulesets.cs` | 候选 | — |
| 界面、雾幕与只读状态 | `game/contracts/Ui/`、`game/contracts/World/` | 候选 | — |
| 多地图与平面地图 | `game/contracts/Maps/`、`game/contracts/Atlas/` | 候选 | — |
| 开始页与过场 | `game/contracts/AppFlow/`、`game/contracts/Cinematic/` | 候选 | — |
| 存档与持久化 | `game/contracts/Save/`、`game/contracts/Serialization/` | 候选 | — |
| 外围与导航图 | `game/contracts/Spatial/`、`game/contracts/WorldModel/` | 候选 | — |
| 音频 | `game/contracts/Audio/` | 候选 | — |
| 本机烘焙 | `game/contracts/Bake/` | 候选 | — |
| 精度区 | `game/contracts/WorldModel/Fidelity.cs` | 候选 | — |

系统行为以各自规范为准：世界时间与天气见 SYS-TIME §1、SYS-WEATHER §6、SYS-SAVE §5、SYS-MAPS §5；全局参数与数据纹理见 ENG-PERF §1.5；网格、JSON 和资源管线见 ENG-PERF §1、WF-DOC 与本节 §4；窗灯见 SYS-TIME §6、SYS-WEATHER §7、WLD-INTERIORS §4–§6.3；精度区见 ART-FIDELITY §0。

### 覆盖层约束

- 覆盖层的格式、分片与地形操作分别见 SYS-MAPS §2.1、WLD-TERRAIN §3 和 `game/contracts/schemas/`。加载校验还拒绝空理由、来源不唯一、重复删除和删除后仍被路网引用。
- 加载后只发布一份有效 `WorldNetwork`；新增步行线进入 `Roads`，删改原路只接受无歧义的纯 road，替换片段用精确 `hide` 加 `add` 并迁移地形引用，不自动裁切、吸附或重定向旧 ID（SYS-MAPS §2.1）。
- 墙面、门窗、柱与对应碰撞共用同一来源立面布局；`Parts` 的逐来源生成规则以 `game/contracts/WorldModel/WorldModel.cs` 的源码注释为准。

### 仍未落地项

- contract-22 B–E：栖地/分层环境查询、稳定 AnimalId 动态实例与动画更新、刺激与动物动作事件、行为调度交接，尚无对应公共契约。现有 IRenderHost 只有 Mount/Unmount；IInstancePool 是内核侧固定容量 MultiMesh 接口，不补写成上述契约。
- contract-23 C3/C4：toon_figure 已登记但文件缺失，ToonLibrary.MaterialFor 未处理 Figure；fig_ctx 有常量和语义但没有写入方；VF-E 骨索引/权重布局、上传、角色宿主和真实蒙皮预热尚未实现。WarmDrawLayout.Skin4 仅为请求声明。
- contract-21/23 已有的物理、水域、身体签名仍含桩；本节不将公共签名等同于 WP-08/42/54a 的生产能力完成。蒙皮请求类型 `WarmDeformation` 属于 Kernel.Renderer（`game/kernel/registry/Presentation/RuntimePresentation.cs`），其上传与预热消费者尚未接通。

## 4 资源管线

资源用"PNG 源图 + Godot 导入"，几何是烘焙产物，在玩家机器上本机烘焙（D-165）；不用 KTX2、Basis、glTF 的 meshopt 压缩，也没有首屏网络和 CDN。

- **几何**：
  - 一律由代码生成（gen 里的纯 C# 生成器）。不用 GDScript 生成。
  - 确实需要外部模型时（例如雕像），先向主代理提交申请，批准后才能用 Blender 做少量手工英雄资产。导出 glTF 2.0（`.glb`），放在 `game/assets/hero/`，经 Godot 导入；导入设置里不生成材质（材质由内核按 `ToonSpec` 替换），开阴影网格和 LOD 生成。
- **烘焙**（WP-02；本机烘焙，D-165）：几何类烘焙产物不随安装包发，由玩家机器上的 `IBakeService`（§3）调用与开发烘焙器同一个 `Build()` 生成：第一次需要时生成，写进本机缓存，之后照常从缓存读。"烘焙""TileBundle""缓存键"的含义不变，只是烘焙改在本机进行。
  - **开发和 CI 的无界面烘焙器**照旧：`& $env:UESTC_GODOT --headless --path game -- --bake=all --map=qingshuihe`（`--map=*` 依次烘全部图），用来出参考哈希（`bake.manifest.json`）、按所选门禁的需要给共享缓存补烘（`scripts\gate.ps1`，§6 第 3 步、ENG-GATES §1）。开发态 `-- --gen=live` 实时生成同一份 `Build()`，不读也不写缓存。
  - **产物格式**（安全规则，D-165）：本机缓存和开发共享缓存都不用 Godot 资源格式：`user://` 下的 `.res`、`.tres` 能嵌脚本、读入即执行（SYS-SAVE §8 的威胁模型），所以 `ResourceLoader`、`ResourceSaver` 读写 `user://` 的全局禁令（§2）不设例外。
    - 用自定义的格包二进制格式 `.tbin`，即 `TileBundle`：头（魔数、格式版本、缓存键、长度）+ 各表面的顶点与索引缓冲（与引擎表面字典同一打包，ENG-PERF §5）+ 实例变换 + MultiMesh 缓冲 + 碰撞形状参数 + 统计 JSON（顶点、三角形、表面数、字节），整体 zstd（`FileAccess.OpenCompressed` 或 .NET 的 zstd，P0 定）。解析器逐段做边界检查，长度超限就拒收。
    - 读：工作线程上读文件 → 解压 → 用 `System.IO.Hashing.XxHash128` 算哈希，与启动集里 `res://bake/<地图>/boot/bake.manifest.json` 中这个缓存键的哈希比对，不符就删掉重烘（缓存键不在 manifest 里的文件按 ENG-PERF §5 处理） → 在同一工作线程上用 `RenderingServer.MeshCreateFromSurfaces` 建网格（与 `--gen=live` 同一条路径），碰撞形状由参数在加载线程或主线程上创建（ENG-PERF §5）→ 主线程照常只挂载。
    - 写：先写 tmp，`Flush` 后改名；每图一份 `cache.index`（键、字节、最后使用时间），只用于 LRU 和统计，不作信任依据，信任依据是启动集里的 manifest。
    - 开发共享缓存（`UESTC_BAKE_CACHE`）和 `--bake=all` 也写同一种 `.tbin`；启动集在 `res://` 里，仍用 `.res`。
  - 遮挡体每图一份，随 L4 放在启动集 `boot_<地图>.pck` 里，在切图的 Mount 那一步加载，不进 TileBundle（WP-33，ENG-PERF §4、SYS-MAPS §4.2）。
  - 碰撞级 C 每格单独一个 `.tbin`（同一种格式，只装碰撞形状参数和统计），与 TileBundle 分开，只在行走模式下加载（WP-43，SYS-WALK §13）。
  - 路灯灯光表这类派生数据纹理也由烘焙器的子任务产出，是全图常驻的小数据：开发和 CI 的烘焙器出，随启动集发，不在本机烘焙。
  - 需要窗口和 GPU 的烘焙（例如 L2 的半八面体替身，如果 P0 选了它）不能在无界面模式下跑，要排进 GPU 锁（ENG-PERF §12.5）；这类产物只在开发期出，随启动集发布，不在玩家机器上烘。
  - **缓存键**（SYS-MAPS §2.4、ENG-PERF §5）：`ABI/地图/数据哈希/模块@代码哈希/级别/tileId`；室内包、碰撞级 C、底图也各有自己的键（§4"其他烘焙子任务"）。数据哈希只算本图里影响 `Build()` 的输入：`campus.json`、`network.json`、`zones.json`、全部覆盖层、`interiors.json`、`outskirts.json`、`fidelity.json`、`backdrop.json`，以及 `map.json` 的 `seed`、`dataRect`；改出生点、默认状态、过场、点位不重烘。
    - **代码哈希**是模块的生成代码指纹，包含它的全部共享生成依赖：模块所在的程序集，加上它传递引用的本仓库程序集（契约、部件内核与各部件包、室内共享层、世界模型；底图另含 `Campus.Pipeline.Map`），取这些程序集的源文件和项目文件，再加 `global.json`、`Directory.Build.props`、`Directory.Packages.props`。构建时按相对路径排序算出，写进构建信息（本节末尾），与工作树路径无关。共享部件或世界模型的代码一改，所有引用它的模块都换键重烘。
    - 内核里的表面打包和 `.tbin` 读写代码不算模块代码：改了它们的输出字节，就提高 ABI 里的格式版本。
  - **缓存位置**：
    - 开发和 CI：仓库外的共享缓存目录，路径从环境变量 `UESTC_BAKE_CACHE` 读，默认 `E:\Project\UESTC_RPG.cache\bake\`，按图分子目录 `<UESTC_BAKE_CACHE>\<地图>\`，各图互不覆盖。各工作树共用，按区单独烘焙；产物不进 git。
    - 玩家：`user://cache/bake/<地图>/<ABI>/…`，即导出版的 `%APPDATA%` 下，文件布局与缓存键与开发共享缓存相同。只有 `IBakeService` 读写这里，用 `.tbin`，不经 `ResourceLoader`、`ResourceSaver`（§2）。
    - `--bake-cache=<绝对路径>` 换缓存目录。默认是 `user://cache/bake/`；开发态默认指向 `UESTC_BAKE_CACHE`，所以开发与玩家走同一套逻辑（ENG-GATES §1）。
  - **缓存文件与校验**：写盘先写 tmp，`Flush` 后改名。读到长度超限、解析越界或哈希与 manifest 不符的文件时，删掉它，自动重建一次；新产物就绪前同样按 manifest 校验，仍不符就以 `BakeState.Failed` 结束，不再循环重烘（ENG-PERF §5"损坏、校验失败与终态"）；`cache.index` 只用于 LRU，不作信任依据。
  - **查找顺序**：启动集（`res://bake/<地图>/boot/`）→ 本机缓存 → 本机烘焙（生成后写进缓存）。
  - **失效**：缓存键里已含代码哈希和数据哈希，游戏更新或开发中改了输入以后自然不命中。失效粒度就是现行键的实际行为：
    - 数据哈希每图一份：缓存键里列出的任何一项数据输入变了（哪怕只改了一条覆盖层），本图的全部格子、室内包、碰撞级 C 都换键重烘；
    - 代码哈希按模块：只改了一个内容模块自己的代码，只有这个模块的产物重烘；改了共享生成依赖，所有用到它的模块一起重烘；
    - 先保证这条简单而正确的规则。只有全图失效确实拖慢了制作，才把数据输入细分到模块，细分时由 G2 证明依赖没有漏算；不建逐实体的增量构建。
    - 启动时删掉 `user://cache/bake/<地图>/` 下不是当前 ABI 的目录；同一 ABI 下旧键的文件不专门清理，按"最久未用"淘汰。
  - **大小**：一张图烘满约 0.8–1.4 GB。默认上限 2.5 GB，超出时按"最久未用"淘汰：先淘汰别的图，再淘汰本图的 L0 和室内包；L1–L4 和全图常驻数据不淘汰。磁盘剩余空间不足 1 GB 时不写缓存，只在内存里生成、用完即弃，并给一次几秒内淡出的临时提示（INV-3 允许）。
  - **与画面设置、世界状态、存档无关**：`Build()` 的输入里没有这些（§2），所以缓存跨存档、跨画质共用；`--seed` 覆盖时种子进数据哈希，各自一份缓存。
  - **什么时候烘**（预算和队列细则见 ENG-PERF §5）：
    - 首次运行（或缓存清空、版本更新以后）：扉页期间，先烘起始视角（T0 或命令行位姿）周围按 ENG-PERF §5 的环要用的格（L2–L3 与外围），再烘平面地图底图；扉页下方显示一行进度，例如"首次运行 · 正在本机生成校园 37%"，用数据字体，烘完就淡出（WP-49，SYS-TITLE §1）。目标初值（开发机）：首次运行到转为实时 ≤ 25 s，底图 8 线程 ≤ 20 s；以后的启动仍是冷启动 ≤ 9 s、热启动 ≤ 3.5 s。
    - 后台预烘：进开始页起，按与起始点的距离由近到远烘满全图，顺序是 L3 → L2 → L1 → L0 → 碰撞 C → 开放室内 → 简化室内。线程数：扉页和开始页期间取 max(1, 逻辑核数 − 2)，进 Playing 后降到 max(1, 逻辑核数 / 4)；线程优先级低于正常，排在按需请求之后。初值：开发机在开始页烘满全图 ≤ 4 min。`--bake-bg=0` 关闭后台预烘，门禁和基准都关。
    - 全图常驻的小数据随启动集同步挂上，不在本机烘焙（见 §4"导出与随包内容"）。
    - 按需：流式请求的文件不在缓存里时，生成任务进与加载同样的四个队列（碰撞 → 已知目的地 → 室内入口预取 → 常规）；在工作线程上调 `Build()`、打包表面、写缓存，再把同一个 TileBundle 直接交给挂载，不再从盘上读一遍。同时在途的生成任务不超过 max(2, 逻辑核数 − 2)，与加载请求的"在途 ≤ 3"分开计。
    - 不出空洞、不穿地靠现有的流式不变量：父格等子格（缺 L0 时显示最近的已就绪祖先）、碰撞未就绪冻结物理、室内包没就绪时按进楼衔接原有的未就绪规则（ENG-PERF §5 第 2、5、6 条）。
    - 切图：`Travel` 的 LoadData 挂上 `boot_<地图>.pck`；起始点周围在缓存里没有时，在雾幕下烘。Movie Maker、读档、过场都是流式屏障，本机烘焙同样算在屏障里。
    - 单件生成耗时的初值（开发机单线程，P0 实测）：L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms。
  - **线程与 GC**：生成只在工作线程上做，主线程只挂载，预算仍是每帧 ≤ 1.0 ms。工作线程原来只在开发态跑 `Build()`，现在发布版也跑；gen 里仍禁用 `Parallel` 和 `Task.Run`，并行由内核调度（§2）。生成代码用 `ArrayPool` 和预分配缓冲；GC 门禁仍在缓存已烘满时测，后台烘焙期间第 2 代回收的暂停另列 P0 项（§9）。
  - **导出与随包内容**：WP-02 的打包步骤只打启动集。安装包合计约 0.45–0.75 GB：

    | 内容 | 大小 |
    | --- | --- |
    | Godot 运行时与 .NET 运行时 | 约 100–150 MB |
    | C# 程序集（含 gen 与烘焙子任务的代码，都要随包发） | ≤ 25 MB |
    | 着色器：只发 D3D12 一份 shader baker 产物；Vulkan 只是回落，回落时在扉页后面现场编译 | 约 10–40 MB |
    | 贴图（BC 压缩；BC1/BC3 优先，BC7 只在需要时用） | ≤ 250 MB |
    | 界面字体 | 约 30–50 MB |
    | 扉页静帧 | 约 4–8 MB |
    | 世界数据 JSON | 约 10–15 MB |
    | 英雄资产 `.glb`（如有） | 计入主 PCK |
    | 启动集 `boot_<地图>.pck`（含全图常驻的小数据和参考哈希清单 `bake.manifest.json`） | 每图 ≤ 60 MB（其中全图常驻小数据约 10–15 MB） |
    | 音频 | ≤ 165 MB |

    - 启动集每图一个 `boot_<地图>.pck`，包内路径一律 `res://bake/<地图>/boot/`：L4、遮挡体、外围 O2 常驻块与卡片版，以及需要 GPU 才能出的产物（L2 半八面体替身图集，如果 P0 选了它）；另含全图常驻的小数据：派生数据纹理（灯表、`precip_occ` 等）、2 m 分类栅格与 8 m 安全点网格、声学网格、声景场与发声器表（`ambience.bake`）、导航图；以及参考哈希清单 `res://bake/<地图>/boot/bake.manifest.json`（< 1 MB；读本机缓存时按它校验，也是 `--bake-verify` 的比对依据）。小数据和清单由开发和 CI 的烘焙器出，打包步骤从共享缓存取来打进启动集，同步挂上，不在本机烘焙。主 PCK 只留全局资源、各图的 JSON 和英雄资产。某张图第一次进入时用 `ProjectSettings.LoadResourcePack(path, replaceFiles: false)` 挂上，之后不卸载；G6 检查各 PCK 的路径集合两两不相交（SYS-MAPS §2.4）。
    - 不再随包发、改为本机烘焙的只有四类：L0–L3 的 TileBundle（含外围 O0、O1 和 I0 兜底层）、碰撞级 C、开放室内与简化室内的室内包和碰撞包、平面地图底图（SDF、MSDF 图集、`labels.bake`、区域栅格）。全图常驻的小数据体积小，又是启动和第一次切到行走时就要的，本机现烘会拖慢首次运行，所以随启动集发。
    - G6 的上限：安装目录 ≤ 0.9 GB，分发压缩包（7-Zip，LZMA2，`-mx=9`，文件名 `UESTC-<版本>-win64.7z`）≤ 0.7 GB，分别记在 `budgets.json` 的 `export.install` 和 `export.archive`（ENG-GATES §2、ENG-BUDGET §8）。
- **其他烘焙子任务**（底图、室内包、外围 O0/O1 是本机烘焙，由 `IBakeService` 在发布版里调用，所以代码随包发，放进随包程序集，不能只放在 `pipeline/`，受 §2 的确定性禁用写法约束，产物走同一套缓存键、`cache.index` 和查找顺序，不随包发；导航图、声学与声景是全图常驻的小数据，由开发和 CI 的烘焙器出，随启动集发）：
  - **平面地图底图**（WP-48a，SYS-ATLAS §2.1）：`Campus.Pipeline.Map`（`pipeline/map` 的 .NET 类库，引用 `Campus.Gen.World`，与运行时看到同一份世界模型）出精确矢量 SDF（`map_sdf_a`、`map_sdf_b`、`map_sdf_out`）、地标线描 MSDF 图集、`labels.bake`、8 m 区域栅格和清单 `map.manifest.json`；开发期可用 `--bake=map --map=<id>` 单独烘。缓存键含世界模型哈希（campus.json、全部覆盖层、outskirts.json）和烘焙代码哈希，放在缓存的 `<地图>/map/<哈希>/` 下。G18 用清单核对底图与世界模型一致；缓存里没有对应哈希时由 `IBakeService` 现烘（首次运行在扉页期间），烘出来之前底图只画一种颜色，`--bake-strict=1` 下报"底图过期"并以非零退出码结束。
  - **外围**（WP-47，WLD-OUTSKIRTS §6）：`pipeline/outskirts` 从提交进仓库的 Overpass 快照生成 `outskirts.json`，这一步仍在开发期离线做，构建时不联网，`outskirts.json` 随包发；`game/gen/outskirts` 的 `Build()` 按格烘焙 O0、O1（本机烘焙），O2 烘成常驻合并块（随启动集发）。外围数据按 ODbL 提供派生数据（WLD-OUTSKIRTS §6.4）。
  - **室内包**（WP-51、WP-17、WP-31a–c，WLD-INTERIORS §6.1）：每栋开放楼、每层、每个细节级（IS、IF）一个 `.tbin`，每层一个碰撞包；缓存键 `ABI/地图/数据哈希/interior.<提供者>@代码哈希/featureId/storey/detail`，碰撞的细节位记 C；简化室内与 I0 由 `interior.shell` 按格烘焙，I0 进 TileBundle 的 L0。室内包放在缓存的 `interior/` 下，不随包发，也不另设室内 PCK。
  - **导航图**（WP-44，WLD-INTERIORS §13）：`pipeline/nav` 烘焙室外步行网、开放室内的走廊与楼梯边，手写数据在 `game/data/maps/<地图>/nav/`，产物放共享缓存 `nav/<地图>/<哈希>/…`，不进 git；打包步骤把它打进启动集，包内在 `res://bake/<地图>/boot/` 下，不在本机烘焙。
  - **声学与声景**（WP-53c、53b，SYS-AUDIO §10.3）：声学网格（4 m，覆盖 `map_rect`，约 3.3 MB，烘好后全图常驻）和声景场与发声器表（`ambience.bake`），与 `precip_occ` 用同一个子任务挂点；产物放共享缓存的 `audio/` 下，打包步骤把它打进启动集，包内在 `res://bake/<地图>/boot/` 下，不在本机烘焙。
- **贴图**：
  - Python 脚本（`pipeline/assets/`，WP-04）生成低频斑驳、笔触和修边件，输出 PNG 到 `game/assets/style/`。斑驳要弱，明度幅度不超过 ±0.03 L，中远景读作干净的平涂；方向笔触只在 5 m 以内（ART-STYLE §6.2）。`.import` 文件进 git，导入产物（`.godot/imported/`）不进。
  - 导入设为 VRAM Compressed：普通质量是 BC1/BC3，勾选高质量是 BC7，法线用 RGTC。BC7 不能用 GPU 压缩，导入慢，只在需要时用。
  - 修边件、斑驳、细节平铺、叶卡用 Texture2DArray，每层一种；店招与题字、树木远景替身仍是 2048² 图集。窗卡与门厅卡的视差图层（约 84 层 256²，WLD-INTERIORS §5.1）进 WP-04 的视差数组；导览亭牌面、导览桩铭牌进"不重复内容"图集（SYS-ATLAS §2.1）。
  - 数据纹理（调色板、灯光通道、窗覆盖、地面遮罩、树种表、`mat_table`、平面地图的 SDF）用不压缩的 RGBA8（或浮点格式）ImageTexture，运行时创建，不走导入。
  - 区域包不能自带大于 512² 的贴图，只能向 WP-04 申请图集槽位或数组层。
  - 扉页插画（WP-49b，SYS-TITLE §1.3）：8 个变体由出图脚本从 T0 的三维静帧生成，放 `game/assets/title/<地图>/`，不烧片名；标记缩略图由 WP-27 的截图机按推荐状态出图，放 `game/assets/maps/<地图>/thumbs/`（SYS-ATLAS §10）。
- **题字与字体**：只用开源许可（OFL）字体，并只截取用到的字，生成 MSDF 字形图集（BC7 或不压缩）。校名书法、校训题字这类场景里的原作字迹，要先确认许可；未确认之前，用风格接近的开源书法字体代替。界面字体（思源黑体 SC、Inter、JetBrains Mono，均 OFL；不用思源宋体和书法字）整套打包，不改字体文件；标题字重的 MSDF 字形在导入时预渲染，字形清单从翻译表、点位名、地图标注、片名与标题卡文案自动收集，运行中新生成的 MSDF 字形为 0（UI-VISUAL §4、UI-VISUAL §11.8）。
- **音频素材**（WP-53e，SYS-AUDIO §14、SYS-AUDIO §15）：
  - 不做实地采录；用免费开源、可商用的素材，CC0 优先，其次是允许随游戏发布的免费包，需要时用 EQ、变调、分层、混响、剪辑处理；
  - 流程：WAV 母带 → `pipeline/audio` 入库体检与标定 → `.import`（短声 QOA，长床外部编码 Ogg，WAV 循环的 `loop_end` 写总帧数）；统一 48 kHz（`project.godot` 的 `mix_rate = 48000`，主代理改）；
  - `*.wav`、`*.ogg` 进 Git LFS；只许随游戏发布、不许公开分发的素材在仓库公开时放私有素材库；
  - 每个文件在 `game/data/audio/licenses.json` 登记来源与许可，署名页由它自动生成；音频在导出包里单列，不超过 165 MB。
- **资源清单**：每个资源都要登记许可证、生成脚本、哈希和体积：自产资源登记在所在目录的 `manifest.json`（`game/assets/style/` 归 WP-04，`game/assets/hud/` 归 WP-35，`game/assets/title/` 归 WP-49b，`game/assets/maps/` 归 WP-27），音频登记在 `licenses.json`（WP-53e），第三方登记在 `third_party.json`。门禁拒收任何以照片为来源的资源。界面资源清单里没有银杏、书签、纸纹意象（UI-VISUAL §13）。
- **导出过滤器**：`export_presets.cfg` 要显式包含 `data/*.json`、`data/maps/**/*.json`、`data/weather/**`、`data/audio/**` 等非资源文件（是否必需，由 P0 的导出验证确认，§7），排除 `gates/` 下的数据文件和全部 `tests/`，以及门禁夹具图（如果做，SYS-MAPS §15）。`gates/` 的 C# 代码仍编进主程序集，因为 G10 要在导出的 release 版里回放路线；不带门禁参数时它不启用。G6 的上限是安装目录 ≤ 0.9 GB、分发压缩包 ≤ 0.7 GB（§4"导出与随包内容"、ENG-PERF §8）。
- **构建信息**（WP-00 建生成步骤，WP-01 读出并写日志）：版本号只有一个来源，仓库根的 `version.txt`（一行 `主.次.修订`，主代理维护，发版时改）。`Campus.csproj` 的构建步骤读它和当前提交（`git rev-parse --short=12 HEAD`；工作区有未提交的改动时加 `-dirty`，拿不到 Git 时写 `nogit`），生成 `BuildInfo.g.cs`，内容是：
  - `Version`（版本号）、`Commit`（提交）、`Build`（`<版本>+<提交>`）；
  - `Kind`：按构建配置，`Debug` 为 `dev`，`ExportDebug`、`ExportRelease` 为 `export`；
  - 各模块的代码哈希（§4"缓存键"）。
  - **谁读它**：显示版本的界面、`SaveMeta.Build`、日志第一行与诊断摘要（ENG-PERF §12.6）、`output/perf/` 报告、分发压缩包名 `UESTC-<版本>-win64.7z` 一律读它，别处不另写版本号。对外发布的构建必须来自干净的提交（`Commit` 不带 `-dirty`）；导出、发布与更新的步骤见 OPS-RELEASE。

## 5 接口先定、桩先合入

WP-00 合入时自带一套能跑、但很简陋的桩（占位实现）。这样所有工作包第一天就能运行和截图：

| 桩 | 行为 |
| --- | --- |
| 卡通材质 | 材质库对任何 `ToonSpec` 都返回同一个最简 ShaderMaterial（`toon_stub.gdshader`，颜色取受光色，Lambert 漫反射），也在白名单里 |
| 部件 | 按包围盒画方块（`gen/parts/_stubs`） |
| 世界状态 | 固定在秋季 17:30（`WorldHours` = 17.5），`AnimTime` = 240 s（即旧网页版的 t = 0.5），观察模式为旁观；`Instance` 固定为 1；`FlowTo` 直接把钟点设到终点 |
| 天气 | `IWeatherProcess` 的桩：永远是晴的天气向量，`Ahead` 为空，`Select` 只改读数；天气类全局参数写晴的值；`Enter`、`Leave` 空实现，`Capture` 返回晴的状态，`Restore` 忽略 |
| 画质 | `IQualityService` 的桩：设备档固定独显中，预设固定精致（SYS-GRAPHICS §1），`Plan` 返回空差异，`Apply` 直接返回 `Applied`；不做自动调节；启动时对每个 `[QualityResponsive]` 节点调用一次 `ApplyQuality` |
| 地图 | `IMapService` 的桩读 `index.json`，只有 `qingshuihe`；启动时加载这张图、设好 `Context`、写 `map_rect`；`Travel` 到当前图只做 `SetPose` 并返回 `Arrived`，不卸载；真正的同图重载由 WP-46 交付（SYS-MAPS §16） |
| 平面地图 | `IAtlasService` 的桩：标记只取 `waypoints.json` 的点位，`Travel` 一律按飞过去调 `ICameraRig.Transit`；底图桩是一张单色的校内用地。这样 WP-48b 第一天就能做界面，WP-36 可以并行做 `TransitKind.Teleport` |
| 开始页与过场 | `IAppFlow` 的桩：直接走 `--title=0` 的路径，雾幕淡出后进入 Playing；`CinematicPlayer` 的桩：雾幕直达终点。WP-49 开工前其他包照旧能跑 |
| 存档 | `ISaveService` 的桩：`Enabled = false`，三个存档位都是 `Empty`；`Request`、`SaveNow` 返回 `Disabled`；`NewGame` 只把已登记的各段设为 `CreateDefault`，并让 `Instance` 加 1；`Load` 不论 `Full`、`Preview` 都返回 `Empty`，`HasPendingPose` 恒为 false，`CompleteResume` 立即完成；`FlushForQuit` 立即返回 `Disabled`，所以关窗口照常退出；`State` 的 `ReadOnly` 为 `SaveOff` |
| 规则集 | `IRuleset` 由 WP-00 随契约交付，读 `game/data/rulesets/sightseeing.json`，不另做桩 |
| 外围 | O1、O2 按楼块包围盒画方块；`IWorldModel.Outskirts` 可以为 null，这时前沿线退化为校界外扩 `FallbackMargin`（WLD-OUTSKIRTS §12.3） |
| 室内 | 开放清单里每栋楼一个方块桩提供者：每层一块楼板、开放入口处一个门洞、一部直跑楼梯，`Walkable`，让 WP-42、WP-07 第一天就能走进去；平面内核先给 0.5 m 栅格版的桩；导航图先用 WLD-INTERIORS §13 的桩图 |
| 门 | `IInteriorDoors` 的桩：所有会动的门一律 `Open`，没有运行时门体，不发 `Changed` |
| 音频 | `IAudioService` 建好总线树，`PlayUi` 空实现；`IAcousticEnv` 永远返回 Open（介质是 Interior 时 `Indoor` 为 1，否则为 0），`Routing` 只有干声，`Occlusion` 一律 Direct；`IFoleyService.Play` 空实现；`IGroundCover` 全 0；`IWalkTelemetry` 不发事件（SYS-AUDIO §21.1） |
| 界面与雾幕 | `IMotionSettings.ReduceMotion` 读系统设置，其余为 false；`IPaletteProbe` 返回一条固定的秋季天色；`IVeilService` 用 WP-05 之前的一块全屏 ColorRect |
| 太阳 | 一盏 DirectionalLight3D，固定在方位 241°、高度 5.5° |
| 调色板 | 只有"秋 · 黄金"一张关键帧 |
| 后期 | 没有 CompositorEffect；Environment 只设 Tonemap Linear |
| 细节分级 | 全部内容当作一个 L0 格子，开发态实时生成，不烘焙 |
| 相机 | 简易的 WASD 加鼠标捕获（Esc 释放）；`Transit()` 直接 `SetPose`；`LockInput` 照常按原因计数；`IObserverState` 的桩取镜头位置，`Space` 为 null；可达空间先按校界外扩 150 m（同 `RoamLimits.FallbackMargin`） |
| 碰撞 | HeightMapShape3D 地面 + 楼体轮廓拉伸成的薄 BoxShape3D 墙段，直接从世界模型生成（SYS-WALK §13），行走第一天就能走 |
| 界面 | 一个调试面板（Control）：时间前推按钮、季节按钮、天气格、模式切换 |
| 介质 | 永远返回"在空气中"；`IMediumState` 同此 |
| 门禁 | `scripts\gate.ps1 -Wp <WP-xx>` 的骨架，G1–G6 能跑 |
| 精度区 | `IWorldModel.Fidelity` 的桩：立面段、屋顶、地面一律 K3，`WalkDistanceAt` 返回 15 m；WP-44 交出 `fidelity.json` 之前，生成器照 K3 做 |
| 地形 | `IWorldModel.Terrain` 的平地桩：`HeightAt` 恒 0、`IsFlat` 恒真、`PadY` 恒 0、`FillTile` 全 0、`WaterY` 取 WP-24 现用的水面高；`ISpatialIndex.GroundY`、`SupportY` 经它取值。WP-08 交出评估器之前各包照桩接线，产物与平地相同（WLD-TERRAIN §10.1，contract-WP-08-地形求值与覆盖层） |

部件按"编目先行"的方式管理：

- `game/contracts/Parts/` 放所有共享部件的 ID 常量和参数 record（`PartCatalog`），随契约 v1 候选先交；部件包与区域包在主线上接通后按组冻结（§3），之后受 PublicApiAnalyzers 保护。
- 区域包直接按 ID 放置部件，先显示成方块；部件包完成后，方块自动换成真模型。
- 区域包需要特有部件时，在自己的目录里新建，ID 加上区名前缀。证明可以复用后，再申请提升为共享部件。

## 6 工作树的技术细节

git 分支、工作树的建法、合并与 rebase 的流程见 WF-MERGE §5；谁能改哪些共享文件见 CORE-CONVENTIONS §5.1。本节只写工作树里的技术细节。

引擎路径一律从环境变量 `UESTC_GODOT` 读命令行版 exe，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。门禁比对 `--version` 的输出与 `tools/godot-version.txt`。

工作树之间靠"每个工作树一个 `override.cfg` + 复制 `.godot` 做种子"隔离。

1. **仓库配置**：
   - `.gitattributes` 是 `* text=auto eol=lf`，图片、`.res`、`.glb`、`.pck`、字体的 binary 标记，音频走 Git LFS（`*.wav`、`*.ogg`，SYS-AUDIO §14.3）；
   - `.gitignore` 含 `.godot/`、`override.cfg`、`bin/`、`obj/`、`.vs/`、`*.user`、`output/`、`.venv/`、`__pycache__/`、`build/`；
   - 主检出有 `game/override.cfg`（`config/use_custom_user_dir=true`、`config/custom_user_dir_name="UESTC_DEV"`），让开发态不碰正式版的 `%APPDATA%\UESTC\`（§2）。
2. **新工作树**由 `scripts\wt\new.ps1` 完成，以脚本为准：从当前 `main` 拉本地分支；把主检出的 `game\.godot`（不含 `shader_cache`）复制过去做导入种子；写 `game\override.cfg`，`custom_user_dir_name` 设成 `UESTC_<WP-xx>`，让每个工作树有自己的 `user://`；`dotnet build -warnaserror`；`--headless --import`。
   - 不要用目录联接共享 `.godot`：导入产物的文件名是"文件名-路径 md5"，两个分支内容不同时会互相覆盖。复制种子后，引擎发现修改时间不同会再比对 md5，内容一致就不重新导入。
   - `--import` 的含义是"启动编辑器，等资源导入完成后退出"。导入日志里不得有 `ERROR` 或 `SCRIPT ERROR`（G1）。
3. **日常开发**：
   - 代理只走命令行，不在编辑器里打开或重存场景（编辑器存盘会给每个节点写 `unique_id`，产生无关差异）；场景尽量由代码生成。
   - rebase 后如果资源有变化，再跑一次 `--headless --import`（`gate.ps1` 会自动做）。
   - 截图类门禁（G7、G9）必须在交互式会话的窗口模式下跑，窗口不能最小化；无界面模式用的是空渲染器，不能截图，也不编译着色器。截图取共享 GPU 锁，同时最多 2 个工作包在跑；性能门禁（G10）取独占锁（ENG-PERF §12.5）。
   - **先选检查，再决定要不要烘焙**：`gate.ps1` 先按改动选出本次要跑的检查（快检；碰到核心体验时加核心专项；集中测试时全量；测试节奏只以 ENG-GATES §2 为准），再看所选的检查要不要烘焙产物：
     - 快检（编译、单元测试、归属、无界面注册检查）不烘焙；
     - 不带 `--bake-strict=1` 的检查（如核心专项的窗口冒烟）缺什么就按玩家路径现烘，不预烘；
     - 带 `--bake-strict=1` 的检查（截图类门禁、基准、G10）先把它们用到的图（`--bake=all --map=<id>`）按哈希增量补进共享缓存（`UESTC_BAKE_CACHE`），运行时带 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`，计时不受本机烘焙干扰（追加方式见 ENG-GATES §1）；
     - 全部图的 `--map=*` 预烘只在集中测试，或所选检查确实要全部图时跑。

     各工作树的 `override.cfg` 把 `user://` 指向 `UESTC_WPxx`，所以玩家路径的本机缓存各工作树各一份；开发态 `--bake-cache` 默认指向共享缓存，一般用不到它。
   - `dotnet build`、`--import` 和烘焙（含本机烘焙）都是重 CPU 任务。十几个工作树并行时，同时运行的重 CPU 任务不超过 max(2, 逻辑核数 / 8) 个，用 `..\UESTC_RPG.wt\.locks\` 下的计数锁排队（初值，P0 按本机实测调整，§9）；持有独占 GPU 锁时，其他工作树一律不得运行它们。
   - **公平票据**：请求资源（CPU、GPU 共享、GPU 独占）先在独立的登记锁下取一个单调递增的票号，再竞争准入；较早的等待请求会阻挡后到、资源集合有交集的请求，资源不相交的可以继续。CPU 加 GPU 的组合请求拿不到全套资源时释放已占的局部槽位，不占着 CPU 等 GPU；GPU 独占先排空在跑的作业并阻止新的 CPU 与 GPU 请求。容量保持 CPU 为 max(2, 逻辑核数 / 8)、GPU 共享 2。超时、取消和进程退出会释放请求句柄，失效的文件不当活队列，计数器损坏时拒绝而不是悄悄归零。旧脚本发起的 CPU 请求不认识票据，可以绕过新的排队（新旧互斥仍安全），所以脚本更新后各工作树要 rebase、用新进程发起作业，才有统一的公平保证；在跑的旧作业正常结束，不杀进程、不删锁文件。
4. **归属与 `.uid`**：
   - 改动路径必须在 `ownership.json` 给本包划定的范围内，否则门禁 G3 拒绝。
   - `.uid` 跟着源文件走，和源文件在同一个目录，所以目录独占规则自然覆盖它。新文件的 `.uid` 由 `--import` 生成后一起提交。复制文件时删掉复制来的 `.uid`，否则会出现重复的 UID（G3 检查"每个脚本都有 `.uid`、全仓 UID 不重复"）。
   - `.import`、`.uid`、`export_presets.cfg` 进 git；`.godot/`、`override.cfg`、烘焙产物不进。

## 7 主要风险与对策

| 风险 | 对策 |
| --- | --- |
| 部件库成为瓶颈 | 编目和参数类型随候选契约先交，方块桩先合入；区域可以先建私有部件；部件库拆成 5 个包 |
| 风格不统一 | 场景里只允许经 `ToonSpec` 取得的卡通材质，着色器只用白名单；颜色只能用色名；先做金样（见 ART-MODELING §6），再放开并行；设一名风格负责人横向比对 |
| 预算超支 | 每个部件必须声明各级细节的面数；按模块统计预算；单个格子先卡上限；G8 用烘焙统计预测；详见 ENG-BUDGET |
| 合并冲突 | 目录独占加特性自动发现；共享文件只有一个写入者；契约只增不改（PublicApiAnalyzers）；`main` 有新合入时 rebase（WF-MERGE §4）；代理不在编辑器里重存场景（编辑器存盘会给每个节点写 `unique_id`，产生无关差异） |
| 自由漫游暴露死角 | 由介质呈现兜底楼内、地下、水下和雾海；关着的楼旁观飞进去看到简化室内，镜头在任何楼体里至少显示 I0（WLD-INTERIORS §5.3、WLD-INTERIORS §6.4）；活动范围用软约束；集中测试时在全校随机巡检；行走模式另有行走走廊的加密标准（ART-FIDELITY §9） |
| 近处深度精度不够 | 反向 Z 由引擎提供（4.3 起），近裁剪面 0.05 m 够用；不做反向 Z 的回退方案（ENG-BUDGET §5） |
| 20 多个工作树抢一块 GPU | 视觉门禁排队；截图要求交互式会话、窗口可见；用 `--gpu-index` 固定 RTX 4080 并断言显卡名；开发阶段可以用半分辨率快速截图 |
| 截图无法复现 | 用 `-- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off` 冻结世界状态、天气和画面设置（末尾四项是地图、跳过开始页、不写存档、无声，ENG-GATES §1、SYS-AUDIO §17；`--shots`、`--gate`、`--bench`、`--frames` 下音频本来就默认关，串里照样显式写上）；固定随机种子；截图时禁用输入；等流式队列空、管线编译计数不再增长后才存图；固定 D3D12；比对时允许像素容差。G13 的录像用 Movie Maker 加"流式屏障" |
| 时间 × 季节 × 天气的组合太多，难保每格都好看 | 关键帧按太阳高度索引，只手调秋季 12 帧加四季增量，天气是再叠一层的修正量；几何不随时季和天气变化；按 SYS-TIME §8 的矩阵抽样，集中测试时人工打分；天气另验静态格与动态格（SYS-WEATHER §9） |
| 自动天气随机拼出难看的画面 | 只走验证过的转移边，晨昏带内不自动转移，降水有最短时长和最短间隔（SYS-WEATHER §6.2）；新游戏交还控制时固定是晴（SYS-WEATHER §9 的 G2）；G2 的 lint 与统计测试，一票否决"未验证的转移边"（SYS-WEATHER §9） |
| 画面设置的切换触发管线编译，或低画质丢了画风 | 会清空管线的引擎设置不做成选项，四档统一取值；启动预热覆盖所有可达设置的并集；风格底线不做开关（SYS-GRAPHICS §2、SYS-GRAPHICS §3）；G16 查雾幕后的编译计数，G17 查各预设的最差值配置（SYS-GRAPHICS §10） |
| 行走时穿模、卡死、掉出世界 | `CharacterBody3D` + Jolt + 自写登阶层（SYS-WALK §12）；碰撞与可见几何同参数生成；碰撞未就绪就冻结物理（ENG-PERF §5 第 5 条）；集中测试时 200 个随机点脚本行走（SYS-WALK §15） |
| 界面破坏画面 | 漫游态唯一常驻的是左上角的小地图，可以关，按 U、`--ui=0`、过场时隐藏；界面态的组件贴四角，不进画面中央 60%；3D 画面里不叠加地点标签、选中描边、世界空间标记或光柱；常驻小部件固定用深屏，免得浅色面板在高调画面上消失；`--ui=0` 截图门禁（UI-NAV、UI-VISUAL §13、SYS-ATLAS §14.2） |
| 字体与题字的许可 | 只用 OFL 字体（界面是思源黑体 SC、Inter、JetBrains Mono），界面字体不改文件；原作字迹须先确认许可 |
| `.uid` 缺失或重复 | `.uid` 与源文件同目录、一起提交；复制文件时删掉复制来的 `.uid`；代码只用 `res://` 路径（UID 失效时引擎退回按路径解析，只打印警告）；G3 检查缺失和重复 |
| 工作树共用 `user://` | 所有工作树的项目设置相同，会共用同一个 `user://`，日志和运行时着色器缓存互相干扰。每个工作树生成 `override.cfg`，把 `custom_user_dir_name` 设成 `UESTC_WPxx`；主检出设成 `UESTC_DEV`，不碰正式版的 `UESTC`（§2、§6） |
| Godot 4.7.2 在 net10.0 下的全链路 | 项目目标定为 net10.0（D-177）；Godot 4.7.2 的 C# 模板默认仍是 net8.0。D0 在 net10.0 下跑通 `dotnet build` → `--headless --import` → `--export-release` → 导出版启动；不通时报作者决定，不自行退回 net8.0；`global.json` 锁 SDK 10.0.402 |
| 无界面模式不编译着色器 | 着色器错误只有在窗口模式下才会暴露。G5 加一次短时间的窗口冒烟运行；G7 也在窗口模式下跑 |
| 导出版里的反射与程序集加载 | 编译器会丢掉没用到的程序集引用，导出后反射可能扫不到模块。注册器按构建时生成的程序集清单显式加载；P0 跑通 `dotnet build` → `--import` → `--export-release` 全链路，确认导出版注册结果与编辑器一致 |
| 导出模板未下载 | .NET 导出模板约 1.2 GB，第一次做 G6 导出前从官方来源直接下载，下完说明（WF-RULE §6） |
| JSON 的导出行为不确定 | 源码里 JSON 被识别为资源，文档却把它列为非资源文件。P0 验证导出包里能否读到 `data/*.json`，必要时在导出过滤器里显式包含 |
| 生成结果不确定 | .NET 每个进程的字符串哈希种子不同，`HashSet`、`Dictionary` 的遍历顺序也不可靠。种子一律用 `StableHash`；遍历前先排序；`build()` 内部不做并行浮点归约（§2） |
| 两套数学类型 | 契约用 `System.Numerics`（`Matrix4x4` 是行向量约定），Godot 用 `Transform3D`（列向量约定），两边的 `Vector3` 同名。转换只用内核提供的一组扩展方法（WP-00），G2 有往返测试；同一文件里两套都用时，用 `using N = System.Numerics;` 别名 |
| C# 垃圾回收造成卡顿 | 逐帧循环不分配内存（`CameraMedium` 等每帧查询的类型都是值类型）；用 `GC.GetTotalPauseDuration` 加自定义监视项监测（ENG-PERF §12.2）；生成代码用 `ArrayPool` 和预分配缓冲，后台烘焙期间第 2 代回收的暂停另测（§9） |
| 40 个工作树同时导入、编译 | 每个工作树估计 0.1–0.3 GB，首次导入在分钟级以内（待验证，见 §9）；重 CPU 任务按 §6 的计数锁排队 |
| 测试框架尚在 RC | gdUnit4Net 支持 4.7.2 的 5.1 版还是 RC；需要引擎的测试（`[RequireGodotRuntime]`）怎么托管，P0 验证；转正式版后由主代理换版本（ENG-WP §5） |
| 引擎升级 | 锁定 4.7.2。升级可能改变表面的字节格式（4.2 有先例），将来 C# 也可能改到 GDExtension 上。升级前单独评估，加一道门禁比对我们的打包结果与 `AddSurfaceFromArrays` 的输出；升级后重新生成 API 参考（docs/godot-api/README.md） |
| 本机烘焙：首次运行久、预烘占 CPU、缓存占盘（D-165） | 首次运行只烘起始视角周围和底图，其余后台预烘，线程数按核数、优先级低于正常、排在按需请求之后；缓存上限 2.5 GB、按最久未用淘汰，磁盘剩余不足 1 GB 时只在内存里生成；缺格时靠祖先格兜底，碰撞未就绪冻结物理（ENG-PERF §5）；G10 的冷缓存子项把关首次运行时间和碰撞冻结累计 |
| 本机烘焙的产物与 CI 参考不一致（系统数学库、变宽 SIMD） | gen 与烘焙子任务禁用系统超越函数、内部走三角函数的 `System.Numerics` 构造、`Vector<T>`、`Intrinsics`、`FusedMultiplyAdd`，一律用 `DetMath` 及其同名工具（G4 的 BannedApiAnalyzers）；G6 在集中测试时用 `--bake-verify` 抽样比对参考清单，并全图逐字节比对（§2、ENG-GATES §2） |
| 缓存文件写到一半、损坏、被篡改或过期 | 缓存用自定义的 `.tbin`，不用 Godot 资源格式（`user://` 下的 `.res`、`.tres` 能嵌脚本，§2）；解析器逐段做边界检查、长度超限就拒收；读出后用 XxHash128 与启动集的 `bake.manifest.json` 比对，不符就删掉重烘，`cache.index` 只用于 LRU；先写 tmp、`Flush` 后改名；缓存键含代码哈希和数据哈希，更新后自然不命中；启动时清掉非当前 ABI 的目录（§4） |
| 安装包与分发压缩包体积 | 几何不随包发；贴图 BC1/BC3 优先；G6 查安装目录 ≤ 0.9 GB、分发压缩包 ≤ 0.7 GB（7-Zip），音频单列不超过 165 MB（§4、ENG-BUDGET §8） |
| **多地图、平面地图、过场、存档、外围、室内与音频的风险** | |
| 隐藏的单图假设：写死的全校纹理坐标、`CustomAabb`、通用代码里的实体 ID 和数据路径 | `map_rect` 统一全图纹理的采样（SYS-MAPS §10）；G4 正则禁止实体 ID、地图 ID 和数据路径字面量；集中测试时同图重载（SYS-MAPS §13、ENG-GATES §2 的 G16） |
| 切图的卸载泄漏：RID、Jolt body、ImageTexture、跨图持有的世界模型 | G16 的同图重载判据（显存、对象数 ±5%，旧 `IWorldModel` 的弱引用被回收）；P0 实测同图重载 3 次 |
| PCK 无法卸载、全局纹理换尺寸、材质 ID 只有 128 个 | 包内路径按图分开，G6 查路径不相交；P0 各验一次换尺寸后 RID 与编译计数、雾幕里重新分配材质 ID（SYS-MAPS §15） |
| 以后每图一个 World3D | 第一版不做；要做时先原型验证节点登记、物理空间和倒影视口（SYS-MAPS §14） |
| 底图与世界模型不一致：覆盖层改了没重烘 | G18 核对 `map.manifest.json` 的哈希；缓存里没有对应哈希时本机烘焙，烘出来之前画单色占位，`--bake-strict=1` 下报"底图过期"并非零退出（§4、SYS-ATLAS §2.1） |
| 主题切换时地图和面板不同步 | `map_base` 读与界面同一个 `ui_night` 扫描前沿（SYS-ATLAS §2.4、UI-VISUAL §11.6） |
| 小地图逐帧更新违反"界面 10 Hz" | 在 UI-NAV §9、ENG-BUDGET §7 登记例外：只许逐帧写着色器参数和变换，不改文字、尺寸、主题；G14 用写入计数检查（SYS-ATLAS §10） |
| 过场卡顿或穿模 | 路径整条预取并在 Title 状态钉住；前瞻检查与最低 0.85 倍降速；路径每 0.25 s 采样做穿模检查；`FlowTo` 让世界终态与播放速度无关（SYS-TITLE §8.4、SYS-TITLE §10） |
| 断电、强杀、关机时丢档 | 每 5 分钟定时保存加窗口失焦时保存；每代一个新文件，保留 3 代；Godot 收不到关机消息，最多丢 5 分钟（SYS-SAVE §4） |
| 存档被注入代码或对象 | 禁用 Resource、ConfigFile、`StrToVar`、`BytesToVar*`、`GetVar`，只用源生成的 System.Text.Json，加资源上限；设置文件先查文本再解析（SYS-SAVE §8、SYS-SAVE §9） |
| 存档格式定死太晚，RPG 开工时要改容器 | P0 定死容器版本 1，从 P0 起累积金档；段的字段一改就升版本、写迁移（SYS-SAVE §3.6） |
| 外围楼块数据缺口大，程序化楼型观感不稳 | P0 截图定楼型；截面线与 O1 地面共点，两个包的采样用 G2 比对；ODbL 派生数据的提供义务按 WLD-OUTSKIRTS §6.4 |
| 进楼卡顿、走进空楼、楼梯卡死 | 入口 25 m 整体预取、门口 1 m 软停、ENG-PERF §5 第 6 条（镜头在楼体里至少显示 I0）；楼梯斜面齐平与登阶层必需；G19（WLD-INTERIORS §6、WLD-INTERIORS §10） |
| 会动的门打破"碰撞不随状态变" | 门扇只由观者位置触发，与钟点、季节、天气无关；运行时门体不进烘焙碰撞，转动期间没有碰撞；读档、切图时一律落到关好（§3） |
| 音频把主线程余量吃光 | 音频主线程 ≤ 0.35 ms 从余量里划出，开发机余量只剩约 0.15 ms；最低配置档默认把 3D 声源上限降到 24、次要的床退成 2 个锚点；P0 在开发机和一台最低配置的笔记本上实测（SYS-AUDIO §18.1） |
| 免费素材的许可不清，或不能随仓库公开 | 来源白名单、`licenses.json` 逐文件登记、G21a 入库检查；不许公开分发的素材放私有素材库（SYS-AUDIO §14） |

## 8 扩展点（RPG）

现在只做观光模式（CORE-INDEX）。本节在契约层面列出给以后的 RPG 留下的扩展点，以及现在写死、以后会挡路的地方；路线、子系统的挂点和预算见 ENG-ROADMAP（扩展路线）。本节不改任何现行规则：INV-6"不出现人物"、UI-NAV §4.4"没有可以点选的东西"、SYS-MOTION §10"偶发事件全片唯一"在观光模式下照旧成立。下表里的服务名、类型名都是草案，写进 §3 时以 §3 为准。

**扩展的方式**（都守 §3 的"只增不改"）：

1. 枚举只在末尾追加值，JSON 一律按字符串存，旧数据和旧存档照读。会追加值的枚举，消费方写穷举的 `switch`、`default` 分支拒绝或报错，不写二值判断（§3 的穷举纪律，ENG-ROADMAP §6 第 4 条）。
2. record 只加 init 属性，不动位置参数。`VariantTag`、`PlacedPart`、`ContentOutput`、`AudioSettings`、`NavQuery`、`LocalizedName` 都是位置参数的 record，加字段只能这样加。
3. 接口加成员一律带默认实现（C# 的默认接口方法），或者另开新接口；已有的实现不用改。
4. 新系统一律是新的 Kernel 服务（`[KernelService]`）、新的存档段（`[SaveSection]`）和新的契约块，不加 Autoload（§2）。
5. 需要新的帧步骤时只追加 `FramePriority` 常量，值不与现有 17 步相同（SYS-SAVE §11）；ENG-PERF §6、SYS-TIME §1、SYS-MAPS §9、SYS-AUDIO §20 的表同步。ENG-ROADMAP §4 的草案是 `Story`（−950）、`Agents`（−300）、`Animate`（ENG-BUDGET）。
6. 观光和 RPG 是两个规则集（§3 规则集与观者状态块）；存档 meta 用 `SaveMeta.Ruleset` 记下（`"sightseeing"`，预留 `"rpg"`）。观光构建的行为和门禁不变。
7. 不许做的：改已有签名或枚举序号；让 `build()` 读运行时状态（ENG-BUDGET §7）；为剧情改烘焙产物或烘焙碰撞。世界的剧情变化一律放在运行时层：显隐掩码、物体差分、运行时门体、导航覆盖。

扩展方向、挂点与预算以 ENG-ROADMAP 为准，本节不重复清单。

## 9 待验证（P0）

正文原位只写验证前用的初值；下表列出要验证的内容、方法和负责方。

| 待验证 | 验证方法与负责 | 验证前用的初值 |
| --- | --- | --- |
| 导出版里的反射与程序集加载：注册结果是否与编辑器一致（§2、§7） | P0 跑通 `dotnet build` → `--import` → `--export-release` 全链路，比对两边的注册结果；WP-00 | 注册器按构建时生成的程序集清单逐个显式加载 |
| JSON 的导出行为：导出包里能否读到 `data/*.json`（§4、§7） | P0 的导出验证；WP-00、WP-02 | 导出过滤器显式包含 `data/*.json`、`data/maps/**/*.json`、`data/weather/**`、`data/audio/**` |
| Godot 4.7.2 在 net10.0 下的全链路是否可用（§7） | D0 跑通 §7 所列四步（`dotnet build` → `--headless --import` → `--export-release` → 导出版启动）；主代理 | net10.0，`global.json` 锁 SDK 10.0.402 |
| gdUnit4Net 5.1（RC）下需要引擎的测试怎么托管（§2、§7） | P0 验证 `[RequireGodotRuntime]` 经 `gate.ps1` 把 `UESTC_GODOT` 交给测试适配器的做法；WP-00、WP-27 | 按 §2 的写法；转正式版后由主代理换版本（ENG-WP §5） |
| 40 个工作树同时导入、编译的磁盘占用与耗时（§7） | D0 在主检出和一个工作树上实测；主代理 | 每个工作树 0.1–0.3 GB，首次导入在分钟级以内；重 CPU 任务按 §6 的计数锁排队 |
| L2 是否用半八面体替身（§4） | P0 选型（ENG-PERF）；WP-02 | 选用时这类烘焙要窗口和 GPU，排进 GPU 锁（ENG-PERF §12.5）；只在开发期出，随启动集发布，不在玩家机器上烘 |
| 切图的卸载泄漏（§7） | P0 实测同图重载 3 次，按 G16 的判据（显存、对象数 ±5%，旧 `IWorldModel` 的弱引用被回收）；WP-46 | 集中测试时同图重载（SYS-MAPS §13） |
| 全局纹理换尺寸、雾幕里重新分配材质 ID（§7） | P0 各验一次换尺寸后的 RID 与编译计数、雾幕里重新分配材质 ID（SYS-MAPS §15）；WP-46、WP-04 | 包内路径按图分开，G6 查各 PCK 的路径不相交 |
| 外围的程序化楼型观感（§7） | P0 截图定楼型；WP-47 | WLD-OUTSKIRTS的楼型 |
| 音频的主线程开销（§7） | P0 在开发机和一台最低配置的笔记本上实测（SYS-AUDIO §18.1）；WP-53a | 音频主线程 ≤ 0.35 ms；3D 声源上限 32，最低配置档默认 24；开发机主线程余量约 0.15 ms |
| 安装目录与分发压缩包的实测体积（§4） | P0 实测后统一核账（ENG-BUDGET §8、ENG-PERF §8）；主代理 | G6：安装目录 ≤ 0.9 GB，分发压缩包 ≤ 0.7 GB（全部图之和），音频单列不超过 165 MB |
| 各类产物的单件生成耗时，以及全图预烘总时长（§4） | P0 在开发机单线程实测；WP-02 | L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms；开始页烘满全图 ≤ 4 min |
| `.tbin` 在工作线程上的读写（zstd 解压、XxHash128、`MeshCreateFromSurfaces`）是否安全、耗时多少；zstd 用 `FileAccess.OpenCompressed` 还是 .NET 的实现（§4） | P0 验证；WP-02 | 工作线程上读、解压、校验、建网格，主线程只挂载；zstd 先用 `FileAccess.OpenCompressed` |
| `DetMath` 的精度与速度（§2、§3） | WP-00 的单元测试给出各函数的误差界和每次调用耗时 | 纯 C# 实现；误差界在 P0 标定 |
| 后台烘焙期间的 GC 暂停（§4、§7） | P0 测第 2 代回收的暂停；WP-02 | 生成代码用 `ArrayPool` 和预分配缓冲；暂停上限在 P0 标定 |
| 首次运行到转为实时的时间（§4） | P0 在空缓存目录上实测；WP-02、WP-49 | ≤ 25 s（开发机），底图 8 线程 ≤ 20 s |
| 本机缓存烘满后的实际体积（§4） | P0 全图烘满后量；WP-02 | 一张图约 0.8–1.4 GB；默认上限 2.5 GB |
| 十几个工作树并行时重 CPU 任务的并发上限（§6） | P0 在本机同时开 12 个工作树编译、导入、烘焙，看总耗时与内存；WP-00 | max(2, 逻辑核数 / 8) 个 |
| 精度区划分的可见性判据与转角过渡长度（§3"精度区数据的规则"） | WP-44 划分后抽 50 个 K4 立面段人工复核，看有没有常被看到的面被判成 K4；WP-44、WP-27 | 60 m 内、视线与立面法线夹角 < 60°、不被遮挡记 K3；转角切出 6 m；点位落点 30 m 以内记 K1 |
