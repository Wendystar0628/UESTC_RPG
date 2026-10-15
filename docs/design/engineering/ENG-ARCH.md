---
id: ENG-ARCH
title: 工程架构：模块边界、接口契约与并行开发流程
category: engineering
summary: 目录与归属、程序集边界、全部 C# 契约、帧顺序、资源管线、桩与工作树流程
owners: [WP-00]
contributors: [WP-01, WP-02, WP-04, WP-08, WP-27]
depends: [CORE-INDEX, ENG-GATES, ENG-WP, ENG-PERF]
related: [ART-STYLE, SYS-MOTION, SYS-VIEW, ENG-BUDGET, SYS-TIME, UI-NAV, SYS-WALK, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP]
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
  RestoreContext, NewGameContext, ISaveSection, SaveStatus, SaveResult, LoadStatus, LoadResult, ISaveService,
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
  CinematicEnd, IBakeService, BakeKey, BakePriority, BakeState, IBakeHandle, BakeProgress]
data: ["ownership.json", "third_party.json", "tools/godot-version.txt", "game/contracts/schemas/", "game/data/rulesets/", ".gitattributes", ".gitignore"]
gates: [G1, G2, G3, G4, G5, G6]
---
# ENG-ARCH · 工程架构：模块边界、接口契约与并行开发流程

> **范围**：本文规定 Godot 工程的目录结构与归属、程序集边界与禁用写法、确定性规则、自动发现、全部 C# 契约（§3 是契约的权威来源）、帧顺序、资源管线、桩、git 工作树流程和主要风险。所有子代理都按这里的约定写代码，让 20–40 个子代理同时工作也不互相覆盖。
> **不在本文**：各系统的行为细则以各自的文档为准（例如门的参数 WLD-INTERIORS §4.1、天气 SYS-WEATHER）；全局参数登记表见 ENG-PERF §1.5；预算见 ENG-BUDGET；门禁判据见 ENG-GATES；工作包分工见 ENG-WP；以后的扩展路线见 ENG-ROADMAP。
> **读者与技术栈**：所有子代理都读 §1–§4，主代理另读 §5–§7。引擎是 Godot 4.7.2-stable .NET 版，只做 PC（Windows），渲染器 Forward+；代码全部用 C#，着色器用 Godot 着色语言（`.gdshader`、`.gdshaderinc`），计算着色器用 GLSL（`.glsl`）；§3 的 Godot 侧写法以 [引擎 C# API 参考](../../godot-api/README.md) 的签名为准。
> **关联**：[CORE-INDEX 总索引](../README.md)、[ENG-GATES 验收门禁](ENG-GATES.md)、[ENG-BUDGET 性能预算](ENG-BUDGET.md)、[ENG-WP 工作包](ENG-WP.md)、[ENG-PERF 性能工程](ENG-PERF.md)、[ART-STYLE 风格基础](../art/ART-STYLE.md)、[SYS-MOTION 动态与事件](../systems/SYS-MOTION.md)、[SYS-VIEW 观察模式与介质](../systems/SYS-VIEW.md)、[SYS-TIME 时间季节](../systems/SYS-TIME.md)、[UI-NAV 界面与跳转](../ui/UI-NAV.md)、[SYS-WALK 行走](../systems/SYS-WALK.md)、[SYS-WEATHER 天气](../systems/SYS-WEATHER.md)、[SYS-GRAPHICS 画面设置](../systems/SYS-GRAPHICS.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL.md)、[SYS-ATLAS 平面地图与传送](../systems/SYS-ATLAS.md)、[SYS-TITLE 开始页与过场](../systems/SYS-TITLE.md)、[SYS-SAVE 存档](../systems/SYS-SAVE.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS.md)、[SYS-AUDIO 音频](../systems/SYS-AUDIO.md)、[ENG-ROADMAP 扩展路线](ENG-ROADMAP.md)

## 1 旧网页栈怎么处理

**原则**：旧网页代码只留在 git 基线里，主分支上整体删除，不保留 legacy 目录。

1. **基线**（已建）：清理前的整棵树已提交为基线（`node_modules/`、`dist/`、`.venv/`、`output/`、`test-results/`、`.playwright-cli/` 和照片贴图目录不在库里），标签 `baseline-map`；远端是 GitHub 上的 `Wendystar0628/UESTC_RPG`（§6）。之后的步骤见 §6 第 1 步。
2. **主分支**：网页栈、照片贴图和网页工具链已删除（清单见本节末尾）；按 §2 搭 Godot 项目。能复用的数据和算法按下面三张表处理；待迁移的数据和脚本暂留原位（`data/`、`public/data/`、`scripts/data/`、`scripts/assets/prepare_building_profiles.py`、`requirements.txt`），由 WP-08 迁到 `pipeline/`。
3. **读旧代码**：各包需要参考时用 `git show baseline-map:src/scene/architecture.ts` 读出来，不要把旧文件拷回工作树。

**数据：迁到新位置**

| 旧位置 | 新位置 | 归属 | 说明 |
| --- | --- | --- | --- |
| `scripts/data/build_dataset.py` | `pipeline/` | WP-08 | Python OSM 管线整体迁移；输出写到 `game/data/maps/<地图>/`，脚本加 `--map`（SYS-MAPS §2.1） |
| `data/raw/osm-campus.json` | `pipeline/raw/qingshuihe/` | WP-08 | OSM 快照；原始数据按图放 `pipeline/raw/<地图>/` |
| `public/data/qingshuihe/campus.json` | `game/data/maps/qingshuihe/campus.json` | WP-08 | 格式留用，只增不改。实体 ID 的前缀 `qsh:` 是数据，不是地图 ID，保持不动（SYS-MAPS §2.1） |
| `data/categories.json`、`data/place-enrichments.json` | `pipeline/` | WP-08 | 管线输入 |
| `data/building-profiles.json`、`scripts/assets/prepare_building_profiles.py` | `pipeline/` | WP-08 | 留家族划分和照片引用，去掉材质键。家族划分由管线写进 `campus.json` 的建筑要素（新增字段，只增不改）；照片引用只供人工参考，不进导出包 |
| `data/` 下的照片参考清单（`campus-photo-references.json`、`campus-life-references.json`） | `pipeline/` | WP-08 | 只作人工参考 |
| `requirements.txt` | `pipeline/requirements.txt` | WP-08 | `.venv/` 不进库；WP-04、WP-37、WP-44 的 Python 脚本共用这个环境，加依赖向 WP-08 申请 |

**算法：改写成 C#**（只移植算法，不移植 three 的数据结构）

| 旧文件 | 留什么 | 改写到 | 归属 |
| --- | --- | --- | --- |
| `src/scene/geometry.ts` | 轮廓工具：点在环内、点在足迹内、环的整理 | `game/gen/world/` | WP-08 |
| 同上，`THREE.Shape` 的三角剖分 | 不留，在 C# 里移植 earcut 代替（ISC 许可，登记 `third_party.json`） | `game/gen/parts/core/` | WP-09 |
| `src/scene/architecture.ts` | 边、斜接、扫描线和开间分层算法 | `game/gen/buildings/` | WP-14。斜接和扫描线需要共享时，提升到 `parts/core` |
| `src/scene/landmarkRoofs.ts` | 曲面算法（主成分分析求主轴） | `game/gen/zones/south/` | WP-18 |
| `src/scene/libraryRoof.ts` | 八角采光顶 | `game/gen/zones/core/` | WP-16 |
| `src/scene/landscape.ts` | 落点算法：固定种子、排除区网格、间距网格、聚簇噪声 | `game/gen/vegetation/` | WP-23 |
| `src/scene/SportsMarkings.tsx` 的 `createSportsMarkings` | 球场标线拟合 | `game/gen/ground/` | WP-15 |
| `tests/architecture.test.ts`、`landmarkRoofs.test.ts`、`landscape.test.ts`、`sportsMarkings.test.ts` | 断言：几何有限、有索引、朝向正确、面数上限、确定性 | 对应包的 `tests/` | 同上 |
| `tests/map.test.ts` 的 "data and space" 一组 | 数据不变量：校区范围、足迹的洞、东北轴向、竖直高度 | `game/gen/world/tests/` | WP-08 |

改写规则：

- `THREE.BufferGeometry` 换成契约的 `GeometryBundle`（§3），`THREE.Vector3` 换成 `System.Numerics.Vector3`。
- `landscape.ts` 里的线性同余随机数换成契约里的带种子生成器。不要求复现旧的落点，只要求新实现自身确定。
- `Math.imul` 写的整数哈希改用 `unchecked` 的 `uint` 乘法，或直接用契约里的 `StableHash`。
- 严格高度模式的分支全部删掉。
- JS 按双精度计算。C# 内部可以用 `double`，输出 `float`；旧测试的期望值按 1e-4 m 容差比对。

**重写**（不移植实现，可以看旧代码了解意图）：`batches.ts`、`materials.ts`、`appearance.ts`、`CampusScene.tsx`、`SceneRuntime.tsx`、`CameraController.tsx`、`CampusLandscape.tsx`（树冠）、`MapGeometry.tsx`、`quality.ts`。分别由 WP-02、WP-04、WP-01、WP-06、WP-23、WP-32 按 Godot 重做。

**删除**（只在标签 `baseline-map` 里保留）：

- 地图功能：`src/app/`、`src/ui/`、`src/scene/annotations/`、`src/data/search.ts`、`buildingEnvelope.ts`、`cameraState.ts`、`MapGeometry.tsx` 的拾取和高亮、严格高度模式、`styles.css`、`lucide-react`（SYS-VIEW §1）。界面里的平面地图（SYS-ATLAS）从世界模型重新烘焙，不移植旧网页的搜索、地点卡片、路径规划和拾取（INV-1）。
- 网页工具链：`package.json`、`package-lock.json`、`node_modules/`、`tsconfig.json`、`tsconfig.tsbuildinfo`、`vite.config.ts`、`playwright.config.ts`、`index.html`、`public/`、`dist/`、`tests/`（上表迁移的断言除外）、`test-results/`、`.playwright-cli/`。
- 照片纹理：`public/assets/materials-v1/`、`data/raw/materials/`、`data/appearances.json`、`data/materials.json`、`data/material-sources.json`，以及 `scripts/assets/prepare_materials.py`、`prepare_architecture_materials.py` 两个旧贴图脚本（ART-STYLE §6.1）。

**旧版的现状数据**（网页版实测，只作数量级参考）：总览视角下，建筑与地面约 29.1 万三角形，植被约 40.3 万，球场标线约 1 万，合计约 70 万；绘制调用 105 次，阴影帧 213 次。

**OSM 快照里有、但 `campus.json` 丢掉的信息**：77 座桥、15 处涵洞、146 棵单独的树、13 条行道树线、67 处人行横道，以及道路和河渠的中心线。WP-08 要把它们导出成新的 `game/data/maps/qingshuihe/network.json`。

## 2 目录结构与归属

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
│  │  ├─ maps/qingshuihe/    map.json；campus.json、network.json、zones.json；overlay/（<区>.overlay.json、walkable、outskirts、atlas，
│  │  │                      以及 overlay/interior/<区>.interior.json）；outskirts.json；interiors.json；waypoints.json；lighting-schedule.json；
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
└─ global.json、ownership.json、budgets.json、third_party.json、.gitattributes、.gitignore
```

不进 git 的位置：仓库外的工作树 `..\UESTC_RPG.wt\<WP-xx>\`、GPU 锁 `..\UESTC_RPG.wt\.locks\`、开发和 CI 的烘焙缓存 `..\UESTC_RPG.cache\bake\<地图>\`（§4，按图分目录；玩家机器上的本机缓存在 `user://cache/bake/`，不在仓库里）；仓库里的导出产物 `build/`、日志与截图 `output/`。音频的母带与素材（`*.wav`、`*.ogg`）进 Git LFS；仓库公开时，只许随游戏发布、不许公开分发的素材（如 Sonniss）放私有素材库（SYS-AUDIO §14.3）。

**归属明细**（`ownership.json` 按这张表写；路径相对仓库根）：

```
game/project.godot、*.sln、*.csproj、Directory.*.props、packages.lock.json、
  export_presets.cfg、.editorconfig、addons/                      主代理
game/contracts/                       WP-00  契约（含 Quality、Maps、Atlas、Flow、Cinematic、Save、Audio 各子目录与 schemas/）、部件编目、
                                      带种子随机数与稳定哈希、特性定义
game/kernel/registry/                 WP-00  特性扫描与注册（含 [SaveSection]、[AmbientEmitter] 与按图筛选）、Kernel 服务定位器（Autoload）、
                                      `IRuleset` 服务（规则集，§3）
game/gen/parts/_stubs/                WP-00  方块桩，由各部件包替换
game/kernel/boot|renderer|frame|inspect   WP-01  Boot（Autoload，命令行参数）、渲染设置与全局参数名字表、帧顺序、检查接口、
                                      雾幕服务与启动序列（SYS-TITLE §1）
game/stage/                           WP-01  main.tscn、窗口启动与开场、署名
game/kernel/bake|lod|stream           WP-02  本机烘焙服务 `IBakeService`、本机缓存与 `cache.index`（`user://cache/bake/` 归 WP-02 独占）、启动集 `boot_<地图>.pck` 与参考清单 `bake.manifest.json`、`--bake*` 参数、开发和 CI 的无界面烘焙器（D-165，§4）；格子分级、流式选级与请求（Stream）、挂载（StreamMount）、整图卸载
game/kernel/lighting                  WP-03  主光与阴影、灯光通道、灯光表
game/kernel/post                      WP-05
game/kernel/quality                   WP-32  设备分档、画面设置（IQualityService、预设、应用、持久化）、自动调节画质、帧节奏（SYS-GRAPHICS）；
                                      32a、32b 的子目录见 ENG-WP §3
game/kernel/occlusion                 WP-33  遮挡体生成与可见性调优
game/kernel/world                     WP-34  世界状态、美术日月轨迹、流逝与过渡（SYS-TIME）
game/kernel/save                      WP-50  存档与持久化（SYS-SAVE）：ISaveService、[SaveSection] 的注册与恢复、容器与迁移、金档、保存时机；
                                      含 format/ 与 tests/golden/
game/kernel/map                       WP-46  地图服务与多地图（SYS-MAPS）：IMapService、切换状态机、IMapAware 分发、PCK 挂载、写 map_rect
game/kernel/collision                 WP-43  碰撞级 C 的烘焙、流式加载与查询（SYS-WALK §13）；开放室内的碰撞包、运行时门体、声学 Area3D 的代建
game/style/shaders|palette|textures   WP-04  卡通着色器与材质库、调色板纹理、贴图加载（palette/keys、palette/ext 除外）
game/style/shaders/<系统>_*           对应系统包（文件名以 ENG-PERF §1.4 为准）：medium_* WP-07、diorama_* WP-22、veg_*（veg_lotus 除外）WP-23、
                                      water* 与 reflect_flat WP-24、motion_* WP-26、ui_* WP-35（ui_panel、ui_screen，UI-VISUAL §11.5）、
                                      fx_fall 与 veg_lotus WP-38、fx_glow WP-39、sky* WP-40、
                                      precip* WP-45（precip.gdshader：雨丝、溅落、檐滴共用，SYS-WEATHER §0）、
                                      map_* WP-48b（map_base.gdshader，SYS-ATLAS §2.2）、cine_* WP-49b（cine_cloud.gdshader，SYS-TITLE §9）；
                                      toon_stub.gdshader WP-00（桩，§5）；bake_impostor.gdshader WP-23（只在替身烘焙器里用）；
                                      globals.gdshaderinc WP-01；其余 toon_*（含 toon_cut）与 fog.gdshaderinc 只归 WP-04（每个文件一个负责方）
game/style/palette/keys               WP-37  日相与季节关键帧、调色板合成器（SYS-TIME §3）
game/style/palette/ext/<包>.json      各包自己的调色板扩展，一包一个文件（界面令牌 ui.json 归 WP-35，UI-VISUAL §3.8）
game/style/outline                    WP-05  描线 CompositorEffect 与计算着色器
game/camera/                          WP-06  旁观相机与输入分发（transit、walk、cinematic 除外）；可达空间与推回、写 edge_fog（WLD-OUTSKIRTS §8）
game/camera/transit                   WP-36  点位跳转与传送的过渡（UI-NAV §7、SYS-ATLAS §7）
game/camera/walk                      WP-42  行走控制器与模式切换（SYS-WALK）
game/camera/cinematic                 WP-49a  CinematicPlayer（SYS-TITLE §8.3）
game/flow/                            WP-49a  IAppFlow 与开场流程（SYS-TITLE §7）
game/medium/                          WP-07  介质呈现与室内运行时（medium/interior/：入口预取、按层显示、软停信号、ENG-PERF §5 第 6 条、
                                      会动的门，WLD-INTERIORS §6）
game/hud/、game/assets/hud/           WP-35  界面（UI-NAV、UI-VISUAL）；字体、图标（含窗口图标源文件 app_icon.svg，UI-VISUAL §6.3）；hud/map 除外
game/hud/map/                         WP-48b  小地图与大地图（SYS-ATLAS §3、§4）
game/systems/atlas/                   WP-48c  IAtlasService、标记、锚点、解锁、传送调用（SYS-ATLAS §11）
game/audio/core|ambience|acoustics|foley   WP-53a|53b|53c|53d  音频内核与混音、环境声、空间声学、观者声（SYS-AUDIO）
game/assets/style/                    WP-04  PNG 源图、图集、数组纹理与导入设置（style/backdrop/<地图>/ 是 WP-22 申请的子目录，归 WP-22）
game/assets/maps/<地图>/thumbs/       WP-27  标记缩略图（SYS-ATLAS §10；点位缩略图也放这里，不放 game/assets/hud）
game/assets/title/<地图>/             WP-49b  扉页变体与云卡贴图（SYS-TITLE §1.3）
game/assets/audio/                    WP-53e  音频素材与入库清单（SYS-AUDIO §14）
game/assets/hero/<文件>               经主代理批准的英雄资产，逐文件归属
game/gen/world/                       WP-08  世界模型、覆盖层、外围数据与室内索引的解析、空间索引
game/gen/parts/core                   WP-09  部件库内核
game/gen/parts/openings               WP-10  门窗与幕墙（含室内门与会动的门扇）
game/gen/parts/circulation            WP-11  栏杆、楼梯、阳台
game/gen/parts/roof                   WP-12  屋顶与设备（室内设备在 interior/mep 子目录）
game/gen/parts/street、game/gen/furniture、game/systems/furniture   WP-13  街道家具：部件、沿路网摆放、运行时；导览亭、导览桩、
                                      开放入口部件
game/gen/buildings                    WP-14  通用建筑生成器
game/gen/ground                       WP-15  地表、道路、桥涵
game/gen/diorama                      WP-22  截面与远景（含昼夜层、季节层）
game/gen/walkable                     WP-44  可行走范围、边界收口、安全点（SYS-WALK §6–§9）
game/gen/collision                    WP-43  碰撞图元：楼体墙段自动生成、各模块碰撞体的汇总
game/gen/interior/core                WP-51a  平面内核 Campus.Gen.Interior（WLD-INTERIORS §7.1）
game/gen/interior/shell               WP-51b  内容模块 interior.shell：简化室内、I0 兜底层、室内索引、窗卡与门厅卡、室内遮挡体
game/gen/interior-props               WP-52  室内家具与生活痕迹（WLD-INTERIORS §7.5、§9）
game/gen/outskirts                    WP-47  外围环境带 O0–O2（WLD-OUTSKIRTS）
game/gen/acoustics                    WP-53c  声学网格的烘焙（SYS-AUDIO §10.3）
game/gen/ambience                     WP-53b  默认的 [AmbientEmitter] 来源与声景场（SYS-AUDIO §4.2）
game/gen/zones/core                   WP-16
game/gen/zones/library-interior       WP-17  图书馆与博物馆室内（博物馆在 museum/ 子目录，WLD-INTERIORS §2.2）
game/gen/zones/south                  WP-18
game/gen/zones/west                   WP-19
game/gen/zones/north                  WP-20
game/gen/zones/center-east            WP-21
game/gen/zones/design-*               WP-28 至 WP-30，设计增补（见 ENG-WP）
game/gen/zones/showcase-interiors/south|west-east|north   WP-31a|31b|31c  精选室内（WLD-INTERIORS §18）
game/gen/vegetation、game/systems/vegetation   WP-23  布点与母模型；运行时近池、远池（seasons 子目录除外）
game/systems/vegetation/seasons       WP-38  物候与季节植被（SYS-TIME §4）
game/systems/seasonfx                 WP-38  季节氛围：飘落池（樱瓣、落叶、枝头雪团）、萤火虫、荷花实例；雪花归降水池（WP-45b，game/systems/weather）
game/systems/sky                      WP-40  全日天空
game/gen/water、game/systems/water    WP-24
game/gen/fauna|traces、game/systems/fauna|traces   WP-25
game/systems/motion                   WP-26
game/systems/nightlight               WP-39  夜景灯光、时刻表、光晕（SYS-TIME §6、MAP-QSH-ADDITIONS §8）
game/systems/weather                  WP-45  天气（SYS-WEATHER）：前向过程、剧本、过渡、预报读数（45a）；降水池与湿润（45b，precip 子目录，ENG-WP §3）
game/gates/                           WP-27  （matrix 子目录除外）
game/gates/matrix                     WP-41
game/data/maps/index.json、game/data/maps/*/map.json   WP-46  地图清单与 MapDescriptor 的手写部分（SYS-MAPS §3；字段增改经主代理）
game/data/maps/qingshuihe/campus.json、network.json、zones.json   WP-08
game/data/maps/qingshuihe/overlay/<zone>.overlay.json   每个区一个文件，由该区工作包独占
game/data/maps/qingshuihe/overlay/walkable.overlay.json   WP-44  可行走区域，ID 以 walk: 开头
game/data/maps/qingshuihe/overlay/outskirts.overlay.json、outskirts.json   WP-47  外围覆盖层与外围数据（WLD-OUTSKIRTS §6）
game/data/maps/qingshuihe/overlay/atlas.overlay.json、atlas/markers.json   WP-48c  锚点实体与标记（SYS-ATLAS §9）
game/data/maps/qingshuihe/atlas/labels.json   WP-48a  标注（初稿由烘焙器生成，人工校订）
game/data/maps/qingshuihe/overlay/interior/<区>.interior.json   各区精修室内的包独占本区一份（WLD-INTERIORS §14）
game/data/maps/qingshuihe/interiors.json   主代理  开放室内清单（清单的增删由主代理审批；格式与 schema 由 WP-51 定，WLD-INTERIORS §14）
game/data/maps/qingshuihe/waypoints.json   WP-36  点位清单（UI-NAV §6）
game/data/maps/qingshuihe/lighting-schedule.json   WP-39  灯光时刻表
game/data/maps/qingshuihe/backdrop.json   WP-22  截面、雾海、远景环、雪山的参数
game/data/maps/qingshuihe/sky.json    WP-40  与地理有关的天空参数
game/data/maps/qingshuihe/review.json   WP-27  本图的审查视角与路线（ENG-GATES §1）
game/data/maps/qingshuihe/cinematics/   WP-49a  过场数据（SYS-TITLE §8.1）
game/data/maps/qingshuihe/audio/      WP-53b、WP-53c 按文件分（SYS-AUDIO §21.1）
game/data/maps/qingshuihe/nav/        WP-44  路线与观景点的手写数据（WLD-INTERIORS §13）
game/data/maps/qingshuihe/incidents.json   WP-26  偶发事件清单：事件 ID、路线实体、选树与选灯的规则引用、各版本开关；schema 同包（SYS-MOTION §10）
game/data/weather/<剧本>/             WP-45  片段模板、钟点权重、时长分布、转移边（SYS-WEATHER §6.3；45a）
game/data/audio/                      WP-53  按文件分给 53a–53e（SYS-AUDIO §21.1）
game/data/vegetation/                 WP-23  树种表 species.json（WLD-VEGETATION §2）
game/data/rulesets/                   WP-00  规则集文件（sightseeing.json）与 schema（§3）
game/data/quality-presets.json        WP-32  四个画面预设的单项取值（SYS-GRAPHICS §1；32a）
game/entities/、game/contracts/Entities/、game/data/text/、game/data/maps/*/entities/
                                      WP-00  RPG 预留（ENG-ROADMAP §6 第 6 条），观光期不写入
pipeline/                             WP-08  （walk、nav、assets、colorscript、outskirts、map、audio 子目录除外）；含 raw/<地图>/
pipeline/walk、pipeline/nav           WP-44  可行走遮罩、分类栅格、安全点网格的构建；导航图烘焙（开发和 CI 出，随启动集发，WLD-INTERIORS §13）
pipeline/assets                       WP-04
pipeline/colorscript                  WP-37  关键帧推导脚本与色板联表
pipeline/outskirts                    WP-47  外围数据管线（Overpass 快照 → outskirts.json，WLD-OUTSKIRTS §6.1）
pipeline/map                          WP-48a  底图烘焙器（.NET 类库，随包发，由 IBakeService 调用，D-165）、标注初稿（SYS-ATLAS §2.1）
pipeline/audio                        WP-53e  音频素材管线与许可清单（SYS-AUDIO §14.2）
scripts/gate.ps1、scripts/gates/      WP-27  （matrix、arch 子目录除外；WP-00 先放骨架）
scripts/gates/matrix                  WP-41  时季验收矩阵，含天气维与预设因子（SYS-TIME §8、SYS-WEATHER §9、SYS-GRAPHICS §10）
scripts/gates/arch                    WP-00  ArchUnitNET 规则（增删规则要主代理批准）
scripts/wt/                           WP-00
scripts/devices/                      WP-32
scripts/godot-api/、tools/、global.json、ownership.json、budgets.json、third_party.json、
  .gitattributes、.gitignore、docs/   主代理
```

- C# 命名空间与目录一致：`game/gen/zones/south/` 对应 `Campus.Gen.Zones.South`，`game/systems/water/` 对应 `Campus.Systems.Water`（`.editorconfig` 开 IDE0130 检查）。
- **RPG 名字占位**（ENG-ROADMAP §6 第 6 条，都不写代码）：上表记为 RPG 预留的目录观光期不写入；实体 ID 前缀 `npc:`、`item:`、`quest:` 保留，G5 的前缀检查拒绝观光数据使用；存档预留段名见 SYS-SAVE §2（含 `calendar`、`agents`）；输入动作 `interact`（E）、`inventory`（I）的名字和键位不分配给观光功能（UI-NAV §5）。
- `game/contracts/`、`game/gen/` 和每个 `tests/` 目录里各放一个空的 `.gdignore`。Godot 不扫描这些纯 .NET 代码，它们不产生 `.uid`，也不进导出包。
- 纯 .NET 代码不碰文件路径：数据由内核用 `FileAccess.GetFileAsBytes()` 读成字节，交给 `Campus.Gen.World` 解析。烘焙（开发烘焙器和本机烘焙）也走这条路。路径一律由 `IMapContext.DataRoot` 拼出（`res://data/maps/<地图>/…`，SYS-MAPS §7），代码里不写数据路径、地图 ID 和实体 ID 的字面量。这一条约束 gen 程序集和随包的烘焙子任务（含 `Campus.Pipeline.Map`）：它们在发布版里由 `IBakeService` 调用，只收字节和世界模型，产物字节交给 `IBakeService` 写盘，自己不碰路径；开发期的 `pipeline/*` 脚本和命令行入口可以读写仓库里的文件。
- Godot 侧包的着色器都放在 `game/style/shaders/`（计算着色器另可放 `game/style/outline/`），文件名以系统名作前缀，逐文件登记归属。这个目录里的文件清单就是**着色器白名单**（清单与数目以 ENG-PERF §1.4 为准；天气用 `precip.gdshader`，界面用 `ui_panel`、`ui_screen`，过场用 `cine_cloud`，平面地图用 `map_base`；三渲二风格、外围、室内、音频不新增着色器）。其他目录里出现 `.gdshader`、`.gdshaderinc`、`.glsl`，G4 一律拒绝。新增着色器文件要主代理改 `ownership.json`，这就等于批准。
- **`kernel/quality`**（WP-32）：画面设置的唯一入口是 Kernel 服务 `IQualityService`（§3），预设表在 `game/data/quality-presets.json`。画面设置只改运行时的选级和引擎参数，不改烘焙产物、碰撞、世界状态和种子，`game/gen` 完全不感知它（SYS-GRAPHICS §0）。每个单项由一个负责包落到引擎上（SYS-GRAPHICS §3、SYS-GRAPHICS §7），负责包在自己的 `[KernelNode]` 上用 `[QualityResponsive]` 声明，经 `IQualityAware.ApplyQuality()` 接收；其他包不读设置文件。画面设置不影响声音（SYS-AUDIO §13）。
- **`user://` 下的文件各有一个读写方**：

  | 文件 | 读写方 | 说明 |
  | --- | --- | --- |
  | `graphics.cfg`、`engine_override.cfg` | WP-32 | SYS-GRAPHICS §6、SYS-GRAPHICS §3 |
  | `settings.cfg` | WP-35 | 界面设置，含界面主题、大页磨砂、降低透明度、界面缩放（UI-VISUAL §11.10）、开场过场（SYS-TITLE §5.3），以及平面地图的四项设置和图层筛选（SYS-ATLAS §12，WP-48b 经 WP-35 的设置接口读写，不另开文件） |
  | `input.cfg` | WP-06 | 只存物理键码（int）和鼠标键号，不存 `InputEvent` 对象（SYS-SAVE §8） |
  | `audio.cfg` | WP-53a | 五组音量与声音的各项开关（SYS-AUDIO §13）；开机时音频要比界面更早拿到音量，所以独立成文件 |
  | `cache/bake/<地图>/<ABI>/…`、`cache/bake/<地图>/cache.index` | WP-02 独占（`IBakeService`） | 本机烘焙的缓存（§4，D-165）：L0–L3 的 TileBundle、碰撞级 C、室内包和碰撞包、平面地图底图，都是自定义的 `.tbin`，不用 Godot 资源格式。不是存档，删掉只会让下次重新烘焙；只有 `IBakeService` 读写这里，不经 `ResourceLoader`、`ResourceSaver`；读出后按启动集 `bake.manifest.json` 的哈希校验，`cache.index` 只用于 LRU，不作信任依据 |
  | `saves/<slot>/g<gen>.sav`、`saves/.lock`、`saves/corrupt/` | WP-50 独占 | 每代一个新文件，保留 3 代，不覆盖、不改名；损坏的只挪进 `corrupt/`，从不删除（SYS-SAVE §3、SYS-SAVE §7） |

  - **设置类文件读值只接受基本类型**（`settings.cfg`、`input.cfg`、`graphics.cfg`、`audio.cfg`）：ConfigFile 的解析器在返回之前就会按 `Object(…)` 写法创建对象，所以先把文件读成文本，含 `Object(`、`Resource(`、`SubResource(`、`ExtResource(` 就整份退回 `.bak` 或默认值；再 `Parse`，每个键只接受 bool、int、float、String；写盘走 tmp + `FileStream.Flush(true)` + `File.Replace`（SYS-SAVE §8）。`engine_override.cfg` 由引擎先读，只写登记过的键。
  - 自定义地图标记存进存档（SYS-ATLAS §12）；存档以外只把地图的筛选偏好写进 `settings.cfg`。
  - 门禁和基准照旧不读任何 `user://` 文件（SYS-GRAPHICS §6）；本机烘焙缓存也一样，门禁和基准带 `--bake-cache` 指向共享缓存（ENG-GATES §1）；`--save=off` 下 `user://saves` 不被创建、不被修改（SYS-SAVE §12）。
- **`game/systems/weather`**（WP-45，SYS-WEATHER）：天气的前向过程和剧本在这里，参数在 `game/data/weather/<剧本>/`。其他包只读天气向量（`IWorldState.WeatherNow`）和天气类全局参数（ENG-PERF §1.5），不自己判断天气。天气按图各一份前向过程，进图时补算到当前时间（SYS-MAPS §5）。
- **`MapScope`**（SYS-MAPS §7）：各区程序集 `Campus.Gen.Zones.*`（core、library-interior、south、west、north、center-east、design-waterfront、design-public、design-boundary、showcase-interiors）带程序集级 `[assembly: MapScope("qingshuihe")]`，由 WP-00 在 D0 写进这些 csproj 的 `<AssemblyAttribute>`，各区包不写任何东西。其余 gen 程序集（buildings、ground、diorama、vegetation、water、fauna、traces、furniture、walkable、collision、outskirts、interior、interior-props、acoustics、ambience 和全部部件）是通用模块，只读 `BuildContext.Map` 和本图数据，不认识任何具体的图。模块的归属：类上的 `Map` → 程序集的 `MapScope` → 都没有就是通用；每次加载一张图只启用"通用 + 当前图"的模块（G5 查通用模块里不得有 `FeatureClaim`）。以后的图各区代码另建程序集，标自己的 `MapScope`。

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

- 每个 gen 目录一个 csproj，D0 由 WP-00 全部建好（§6 第 1 步）。`Campus.csproj` 用通配引用 `gen/**/*.csproj`，另引用 `pipeline/map` 的 `Campus.Pipeline.Map`，并用 `Compile Remove` 去掉 `contracts/`、`gen/`、`**/tests/`。新增文件不改任何共享文件；需要新增程序集引用时，向主代理申请。ENG-WP §3 拆出的子包共用原包的程序集，不新建 csproj。例外：WP-51 有两个程序集（`gen/interior/core`、`gen/interior/shell`），WP-53 有两个（`gen/acoustics`、`gen/ambience`），都由 WP-00 在 D0 建好；`showcase-interiors` 下的 south、west-east、north 三个子目录共用一个程序集。
- `Campus.Pipeline.Map` 是纯 .NET 类库，随包程序集一起发：本机烘焙要在发布版里调用它（D-165），主程序集引用它，它也受下文的确定性禁用写法约束。`pipeline/outskirts`、`pipeline/audio` 是 Python 脚本，不进导出包，共用 `pipeline/requirements.txt` 的环境（§1）。
- 测试统一用 gdUnit4Net，经 `dotnet test` 运行。纯 .NET 的测试默认不启动引擎；需要引擎的测试标 `[RequireGodotRuntime]`，由 `gate.ps1` 把 `UESTC_GODOT` 指向的引擎交给测试适配器。各包的测试源码放在自己目录的 `tests/` 里，分别编进 `Campus.Gen.Tests`（gen 和契约的测试）和 `Campus.Tests`（Godot 侧的测试），用测试所在目录区分归属。存档容器、校验与迁移链写成不碰 GodotSharp 的纯 C# 类（`game/kernel/save/format/`），也用 `dotnet test` 直接跑（SYS-SAVE §14）。
- `build()` 拿不到世界状态：`BuildContext`（§3）里没有 `IWorldState`，gen 程序集也引用不到内核。随时间、季节和天气变化的东西，只能通过调色板、灯光通道、变体掩码和天气类全局参数表达（ENG-BUDGET §7）。`build()` 也拿不到画面设置：烘焙产物不随预设变（SYS-GRAPHICS §0 第 4 条）。`build()` 能拿到的地图信息只有 `BuildContext.Map`（`MapDescriptor`，纯数据）。

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
  - `IBakeService`：设置卡的"存储"行读 `Progress`、`CacheBytes`，"清除缓存"调 `Clear()`（D-165）；
  - `IRuleset`：只读，世界控件按 `WorldControls` 显示可用或禁用态（§3 规则集块）。
- `Campus.Hud.Map`（`game/hud/map`，WP-48b）除 GodotSharp 外只依赖契约、`Kernel.Get<T>()`、`IAtlasService`、`IWorldState`（读）、`IObserverState`（读：玩家箭头与所在区域）和 `ICameraRig.Pose`（读：视锥）；传送一律经 `IAtlasService.Travel`，不直接调 `ICameraRig.Transit`。`Campus.Systems.Atlas` 可以调 `ICameraRig.Transit`、`ICameraRig.LockInput`、`IViewControl`、读室内索引（`IWorldModel.Interiors`）和 `IObserverState`（`Proximity` 判定），只在目标在别的图时调 `IMapService.Travel`（SYS-ATLAS §11）。
- 计时只能在 `Campus.Kernel.Frame`、`Campus.Kernel.Quality`、`Campus.Kernel.Inspect`、`Campus.Kernel.Save`（保存耗时与 G10 计数）和 `Campus.Gates` 里出现（`Time.GetTicks*`、`Stopwatch`）。界面的 0.1 s 采样由 `HudModel` 按帧间隔累加实现，不用 `Timer` 节点；音频也不自己计时，监视项登记在 `Campus.Kernel.Inspect`（SYS-AUDIO §18.1）。
- **存档**（SYS-SAVE §9）：只有 `Campus.Kernel.Save` 能读写 `user://saves`；任何代码都不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`，没有例外。本机烘焙缓存 `user://cache/bake/` 只有 `IBakeService`（`Campus.Kernel.Bake`）读写，用自定义的 `.tbin` 格式和 `FileAccess`（或 .NET 流），不经 Resource 机制：`user://` 下的 `.res`、`.tres` 能嵌脚本、读入即执行（D-165，§4）。
- **音频**（SYS-AUDIO §16）：只有 `Campus.Audio.*` 能创建 `AudioStreamPlayer`、`AudioStreamPlayer2D`、`AudioStreamPlayer3D`，能调用 `AudioServer.Set*`、`AddBus*`、`RemoveBus*`，其中 `SetBusMute`、`SetBusVolumeDb` 只许 `Campus.Audio.Core`；`AudioListener3D` 只由 WP-06 的相机创建；声学用的 `Area3D` 只有 `Campus.Kernel.Collision` 能建。其他包要出声，一律经 `IAudioService`、`IAmbienceService`、`IFoleyService`。
- 系统包之间只经契约和 Kernel 服务交互。例外：`Campus.Systems.Vegetation.Seasons` 可以引用 `Campus.Systems.Vegetation` 公开的着色器挂点。

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

- 种子 = `StableHash.Hash32(世界种子, featureId, partId, 用途)`，与生成顺序无关（ENG-PERF §5）。世界种子取 `BuildContext.Map.Seed`（SYS-MAPS §3）。凡是"放不放、选哪种"的判断，一律用整数哈希；三角函数只用来算坐标，且一律用 `DetMath`（上文 BannedApiAnalyzers）。
- 生成器里不依赖 `Dictionary`、`HashSet` 的遍历顺序；需要遍历时先按 ID 排序。
- 一个 `build()` 内部不做并行的浮点归约。
- **跨机器逐字节一致**（D-165）：同一 ABI 下，本机烘焙的产物与 CI 参考烘焙逐字节一致，按每个文件的内容哈希比。靠上面的种子规则、`DetMath` 和 BannedApiAnalyzers 的禁用写法保证；G6 在集中测试时用 `--bake-verify` 抽样比对，并全图逐字节比对（ENG-GATES §2）。
- 天气过程的每次抽取用计数器子流 `StableHash(种子, mapId, epoch, 片段号, 尝试号, 用途)`（种子每个存档一个，SYS-MAPS §5），与补队列的时机和帧率无关（SYS-WEATHER §6.4）。运行时的确定性是：同一钟点、同一哈希的东西结果相同；同一构建 + 同一种子 + 同一串输入得到同样的画面。时间不能逆转，所以不要求"倒回去复现"。
- **读档**：读档是换一个世界实例（`IWorldState.Instance` 加 1），不算倒回去。续跑等价：同一构建下，在安全时刻拍的存档读回后给相同输入，世界和天气与没退出时逐帧一致（SYS-SAVE §5.3）；过渡和偶发事件不续播，声景不进存档（SYS-AUDIO）。
- **音频**（SYS-AUDIO §17）：音频的随机一律用契约的 `Rng` 与 `StableHash`：音频种子 = StableHash(`--seed`, "audio")，每个发声器、每个排程槽一条子流；Movie Maker 下同一组参数两次录音逐字节相同。

**自动发现**：内核启动时用反射扫描下列特性，按 ID 排序后注册；ID 重复由 G5 报错。新模块打上特性就能接入，不需要改任何共享文件。

| 特性 | 标在 | 所在程序集 | 在哪里运行 |
| --- | --- | --- | --- |
| `[ContentModule("zone.south", Owner = "WP-18")]`；例：`[ContentModule("interior.shell", Owner = "WP-51b")]` | 实现 `IContentModule` 的类，文件名 `*Module.cs`；可带 `Map`，没写就继承程序集的 `MapScope` | gen | 线程池或烘焙器（纯函数） |
| `[Part("opening.window.casement", Owner = "WP-10")]` | 实现 `IPartDef<TParams>` 的类 | `gen/parts` | 同上 |
| `[InteriorProvider("interior.library", Owner = "WP-17")]` | 实现 `IInteriorProvider` 的类（按层，WLD-INTERIORS §15）；可带 `Map` | gen | 同上 |
| `[AmbientEmitter("ambience.water", Owner = "WP-53b")]` | 实现 `IAmbientEmitterSource` 的类；输出进本图的 `ambience.bake`。部件的声音插座走 `IPartDef.Sounds()`，室内的声学走 `IInteriorProvider.Acoustics()`，不另设特性；动态发声器在运行时注册（SYS-AUDIO §19） | gen | 烘焙器（纯函数） |
| `[MediumPresenter("medium.interior", Owner = "WP-07")]` | 实现 `IMediumPresenter` 的类 | 主程序集 | 主线程 |
| `[KernelNode("systems.water", Owner = "WP-24")]` | Godot 侧包的根节点（`Node` 子类）。内核按 ID 顺序实例化，挂到 `main.tscn` 的内核根节点下；动态物体由它负责。可带 `Map`：带 `Map` 的在该图 Mount 时实例化、Unload 时释放；不带的跨图常驻，需要换数据的实现 `IMapAware`（SYS-MAPS §7） | 主程序集 | 主线程 |
| `[KernelService(typeof(IWorldState), Owner = "WP-34")]`；例：`[KernelService(typeof(IAtlasService), Owner = "WP-48c")]` | 服务的实现类。注册成 Kernel 的服务，用 `Kernel.Get<IWorldState>()` 取 | 主程序集 | 主线程 |
| `[QualityResponsive(Owns = [...], Reads = [...])]` | 实现 `IQualityAware` 的 `[KernelNode]` 类（SYS-GRAPHICS §8）。G5 核对每个 `QualityKnob` 恰有一个 `Owns`，且与 `KnobSpec.OwnerWp` 一致 | 主程序集 | 主线程，`FramePriority.Quality` 那一步 |
| `[SaveSection("world", Owner = "WP-34", Version = 1, Phase = RestorePhase.World, Order = 10)]` | 实现 `ISaveSection<T>` 的 `[KernelService]` 或 `[KernelNode]` 类；`Scope = SaveScope.PerMap` 的段在文件里展开为 `map:<id>/<段>`。G5 查 `Phase` 与 SYS-SAVE §2 的表一致、`Order` 不重复、`Owner` 与 `ownership.json` 一致（SYS-SAVE §10） | 主程序集 | 主线程：`Capture` 在 `FramePriority.Save` 那一步，`Restore` 在读档那一帧的同一步；迁移函数在后台线程 |
| `[assembly: MapScope("qingshuihe")]` | 各区程序集，由 WP-00 写进 csproj（见上文 `MapScope`） | gen | 注册时求模块归属 |
| `res://style/palette/ext/*.json` | 调色板扩展，一包一个文件 | — | 启动时用 `ResourceLoader.ListDirectory` 扫描 |
| `res://data/maps/<地图>/overlay/*.overlay.json`、`overlay/interior/*.interior.json` | 覆盖层，一区一个文件（按图分目录） | — | WP-08 只扫描当前图的目录 |

- **Autoload 只有两个**，D0 冻结：`Boot`（WP-01，解析命令行参数）和 `Kernel`（WP-00，服务定位器）。需要新的全局对象时，用 `[KernelService]` 注册成 Kernel 的服务，不往 `project.godot` 里加条目。`IMapService`、`IAtlasService`、`IAppFlow`、`ISaveService`、`IAudioService` 等一律如此。
- **build 与 mount 分开**：生成在纯 .NET 里，可以在线程池或烘焙器里跑（发布版的本机烘焙就在工作线程上调 `Build()`，D-165）；Godot 侧的挂载和动态物体只在主线程。gen 里的内容包没有挂载：随时间动的东西一律通过通道（`ChannelDecl`、`PlacedPart.Emitter`）和着色器（风场、`anim_time`）表达，需要节点的动态物体由 Godot 侧包的 `[KernelNode]` 负责（SYS-MOTION）。开放室内里会动的门扇同样如此：gen 只声明门扇（`DoorLeafDecl`），转动与运行时门体由 WP-07、WP-43 负责（§3）。
- 注册器每次加载一张图，只启用"通用 + 当前图"的 `[ContentModule]`、`[InteriorProvider]`、`[AmbientEmitter]`；`[KernelService]`、`[MediumPresenter]`、`[Part]`、`[QualityResponsive]`、`[SaveSection]` 是全局的（SYS-MAPS §7）。`--gate=registry --map=*` 对每张图各查一遍。
- 反射只扫 `Campus` 和 `Campus.Gen.*` 程序集。编译器会丢掉没用到的程序集引用，所以注册器按构建时生成的程序集清单逐个显式加载，不依赖"已加载的程序集"。导出版里的反射行为由 P0 验证（§7）。

## 3 关键接口（契约草案）

契约由 WP-00 在 `game/contracts/`（`Campus.Contracts` 程序集）冻结为 v1。D1 结束时（开工后 24 小时内）收集一轮修改申请，发布 v1.1（ENG-WP §5 第 3 条）。之后**只增不改**：

- C# 公开 API 由 Microsoft.CodeAnalysis.PublicApiAnalyzers 检查。v1.1 时冻结 `PublicAPI.Shipped.txt`，之后删除或改动公开 API 就编译失败。这个分析器跟踪整个程序集，所以契约必须单独成一个程序集。
- JSON 格式（覆盖层、点位、灯光时刻表、调色板扩展等）写成 schema 文件，放在 `game/contracts/schemas/`，由脚本检查只增不删。
- **枚举的穷举纪律**（ENG-ROADMAP §6 第 4 条）：枚举只在末尾追加值，JSON 按字符串存。会追加值的枚举（`ViewMode`、`AppState`、`MapKind`、`MapTravelCause`、`MarkerKind`、`SaveReason`、`SlotKind`、`TransitKind`、`VeilReason`、`DuckReason`、`AudioGroup`、`FoleyKind`、`QualityKnob`、`InputLockReason`、`InteractionRule` 等），消费方一律写穷举的 `switch`，`default` 分支拒绝或报错；不写 `mode == ViewMode.Fly ? … : …` 这样的二值判断。读到不认识的枚举字符串，按 SYS-SAVE §3.5 当作不合法。G1 的 `-warnaserror` 把非穷举 switch 表达式的警告当错误，二值判断靠评审。

世界状态、点位跳转、观察模式与碰撞（SYS-TIME、UI-NAV、SYS-WALK），天气与画面设置（SYS-WEATHER §8、SYS-GRAPHICS §8），以及界面（UI-VISUAL §11.11）、平面地图（SYS-ATLAS §11）、开始页（SYS-TITLE §7.4）、存档（SYS-SAVE §10）、多地图（SYS-MAPS §8）、外围（WLD-OUTSKIRTS §12.3）、室内（WLD-INTERIORS §15）、音频（SYS-AUDIO §19）、规则集与观者状态（ENG-ROADMAP §6）的契约都在开工前定下，直接写进 v1 草案。各文档里的契约片段以本节为准：门的契约名以本节为准，门的参数以 WLD-INTERIORS §4.1 为准；`IWeatherProcess.Capture`、`Restore` 是公开方法，供 WP-45a 自己的两个存档段调用，也让 G20 的续跑等价测试能经契约比对天气状态。

契约程序集只用 `System.Numerics` 的 `Vector2`、`Vector3`、`Quaternion`、`Matrix4x4`，Godot 侧在边界处转换成 Godot 类型（§7"两套数学类型"）。坐标约定照 CORE-INDEX：x 向东、y 向上、z 向南。这和 Godot 一样是右手系、y 向上，−Z 方向就是北，数值不用换算。

以下是草案：方法体省略为 `{ … }`；没有展开的类型（`ModuleBudget`、`PartBudget`、`PartGeometry`、`LightEmitter`、`Footprint2D`、`InspectionProbe`、`WorldFeature`、`WorldNetwork`、`PolygonRings`、`Polyline`、`OverlayGeometry`、`BuildingFamily`、`FacadePreset`、`DesignKind`、`PostOverride`、`IRuntimeContext`）由 WP-00 在 v1 里定义。契约程序集的语言版本是 C# 12（net8.0 的默认值）。

```csharp
// 基础类型：标识符都是字符串包装，JSON 里就是字符串
public readonly record struct FeatureId(string Value);   // campus.json 的 entities[].id，或 design:…、walk:…
public readonly record struct PartId(string Value);      // 如 "opening.window.casement"
public readonly record struct ModuleId(string Value);
public readonly record struct ZoneId(string Value);
public readonly record struct WpId(string Value);        // 如 "WP-18"
public readonly record struct ChannelId(string Value);
public readonly record struct PaletteToken(string Value); // 色名，如 "wall.lit"
public readonly record struct Box3(Vector3 Min, Vector3 Max);
public enum LodLevel : byte { L0 = 0, L1 = 1, L2 = 2, L3 = 3, L4 = 4 }   // 见 ENG-BUDGET §3
public readonly record struct TileKey(int X, int Z);     // 64 m 格；整数 tileId 见 ENG-PERF §5
// 多地图的基础类型（SYS-MAPS §8）：SYS-ATLAS、SYS-SAVE引用同一个定义
public readonly record struct MapId(string Value);       // 小写字母、数字、连字符，≤ 32 字符；清水河是 "qingshuihe"
public readonly record struct WaypointRef(MapId Map, string Id);   // 跨图引用点位或出生点（平面地图、传送门、存档）

// 确定性工具：gen 与烘焙子任务里唯一允许的随机来源和超越函数来源
public static class StableHash { public static uint Hash32(uint seed, params string[] parts) { … } }   // 与进程无关
public struct Rng(uint seed) { public float NextFloat() { … } public int NextInt(int max) { … } }
// 超越函数：纯 C# 实现，不走系统数学库，单元测试给出误差界（WP-00）；float、double 各一套重载。gen 与烘焙子任务里禁用 Math、MathF 的同名函数（§2 BannedApiAnalyzers）
public static class DetMath
{
    public static float Sin(float x) { … }    public static float Cos(float x) { … }    public static float Tan(float x) { … }
    public static float Asin(float x) { … }   public static float Acos(float x) { … }   public static float Atan(float x) { … }   public static float Atan2(float y, float x) { … }
    public static float Exp(float x) { … }    public static float Log(float x) { … }    public static float Log10(float x) { … }
    public static float Pow(float x, float y) { … }    public static float Cbrt(float x) { … }       // 清单随 gen 实际用到的补全
    // 基于 DetMath 的 System.Numerics 构造，同名工具（gen 里禁用 Matrix4x4.CreateRotation*、CreateFromAxisAngle、CreateFromYawPitchRoll，Quaternion 的对应方法和 Slerp，§2）
    public static Matrix4x4 RotationX(float radians) { … }   public static Matrix4x4 RotationY(float radians) { … }   public static Matrix4x4 RotationZ(float radians) { … }
    public static Matrix4x4 FromAxisAngle(Vector3 axis, float radians) { … }   public static Matrix4x4 FromYawPitchRoll(float yaw, float pitch, float roll) { … }
    public static Quaternion QuatFromAxisAngle(Vector3 axis, float radians) { … }   public static Quaternion QuatFromYawPitchRoll(float yaw, float pitch, float roll) { … }
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { … }
}
```

```csharp
// 内容模块
[Flags] public enum Aspects { Massing = 1, Facade = 2, Roof = 4, Interior = 8, Ground = 16, Planting = 32, Props = 64 }
public abstract record Claim(Aspects Aspects);
public sealed record FeatureClaim(FeatureId Feature, Aspects Aspects) : Claim(Aspects);
public sealed record AreaClaim(ZoneId Zone, Aspects Aspects) : Claim(Aspects);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContentModuleAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";              // "WP-18"；G5 核对与 ownership.json 一致
    public string Map { get; set; } = "";                // SYS-MAPS §7：空 = 继承程序集的 MapScope；都没有 = 通用模块
}

public interface IContentModule
{
    IReadOnlyList<Claim> Claims { get; }
    ModuleBudget Budget { get; }
    IEnumerable<TileKey> Tiles(BuildContext ctx);
    ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod);   // 纯函数，在线程池或烘焙器里运行
    IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => [];   // SYS-WALK §13
    IReadOnlyList<ChannelDecl> Channels => [];
    IReadOnlyList<InspectionProbe> Probes => [];                         // 近景巡检点，见 ENG-GATES
}
// 没有 mount：gen 里的内容没有挂载，动态物体归 Godot 侧包的 [KernelNode]（§2）

public sealed class BuildContext           // 内核或烘焙器构造；完整定义由 WP-00 在 v1 冻结
{
    public required IWorldModel World { get; init; }   // 只读
    public required MapDescriptor Map { get; init; }   // 当前图的描述，纯数据（SYS-MAPS §8）
    public required uint WorldSeed { get; init; }      // 取 Map.Seed；--seed 可覆盖
    public Rng Seeded(FeatureId f, PartId part, string purpose) { … }   // 种子 = StableHash(世界种子, featureId, partId, 用途)
    public ToonRef Toon(ToonSpec spec) { … }           // 只登记规格，不创建引擎材质
    // 刻意不提供 IWorldState：build() 不得读取钟点、季节、观察模式（ENG-BUDGET §7）
}

public sealed record ContentOutput(
    IReadOnlyList<GeometryBundle> Meshes,
    IReadOnlyList<PlacedPart> Parts,
    IReadOnlyList<LightEmitter>? Emitters = null,
    IReadOnlyList<Footprint2D>? Occupancy = null);

public readonly record struct MeshLod(float Error, int IndexCount);   // 网格 LOD：误差（m）和索引前缀的长度

public sealed record GeometryBundle(
    ToonRef Material,
    float[] Position,                 // xyz 交错，世界坐标，单位 m
    float[] Normal,                   // xyz 交错，单位长
    int[] Index,                      // 三角形索引；打包时每表面不超过 65 536 个顶点，转成 16 位
    float[]? Ao = null,               // 每顶点 0–1，打包进 COLOR
    float[]? Uv = null,
    float[]? Uv2 = null,              // 窗号基址、窗格行列等逐顶点数据（ENG-PERF §10.3）
    IReadOnlyList<MeshLod>? Lods = null,   // 三角形已按重要度排序时，每级取索引前缀，由细到粗
    VariantTag? Variant = null);
```

```csharp
// 部件
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PartAttribute(string id) : Attribute { public string Id { get; } = id; public string Owner { get; set; } = ""; }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ParamAttribute : Attribute { public double Min { get; set; } public double Max { get; set; } }

public interface IPartDef<TParams> where TParams : notnull   // TParams 是 record，字段上标 [Param]
{
    IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }   // L0 必须有实体厚度和倒角
    Box3 Bounds(TParams p);
    IReadOnlyDictionary<string, Matrix4x4> Sockets => new Dictionary<string, Matrix4x4>();   // 挂点
    PartBudget Budget { get; }                                       // 三角形数：L0、L1
    IReadOnlyList<Collider> Collision(TParams p) => [];              // 由部件参数生成，与几何同源（SYS-WALK §13）
    IReadOnlyList<SoundSocketDecl> Sounds(TParams p) => [];          // 部件自带的声音插座（售货机、外机、喇叭等，SYS-AUDIO §19）
}

public sealed record PlacedPart(
    PartId Part, object Params, Matrix4x4 At,                        // Params 是该部件的参数 record
    PaletteToken? Tint = null, ChannelId? Emitter = null, VariantTag? Variant = null);
```

```csharp
// 风格
[Flags] public enum ToonFx { None = 0, GlassStreak = 1, Glare = 2, AnisoBand = 4, Rim = 8, Mottle = 16, DetailTiling = 32 }
public enum ToonNormal { Flat, Smooth, Spherical }
public enum LineClass { Building, Tree, Fine, None }
public enum ShadeSteps : byte { Two, TwoHalf, Soft, Three }   // 建筑两阶、曲面道具两阶半、软物两阶半宽带、植被三阶（ART-STYLE §2.1）

public sealed record ToonSpec(PaletteToken Lit, PaletteToken Shade)
{
    public PaletteToken? Deep { get; init; }             // 只作 WP-37 自检的校验值；植被深影由着色器从影色算出
                                                         // （L × 0.88、C × 0.95，ART-STYLE §2.1），着色器不读本字段
    public float Band { get; init; } = 0.02f;            // 明暗过渡带：建筑 0.02（阴雨天按天光方向时 0.06），植被团块 0.06（ART-STYLE §2.1）
    public ToonNormal Normal { get; init; } = ToonNormal.Flat;
    public LineClass Line { get; init; } = LineClass.Building;
    public ToonFx Fx { get; init; }
    public PaletteToken? Backface { get; init; }         // 剖切色：镜头穿进物体时看到的切面颜色
    public ChannelId? Emissive { get; init; }
    public ShadeSteps Steps { get; init; } = ShadeSteps.Two;   // 写进 mat_table 行 1（ENG-PERF §1.5）
    public float Translucency { get; init; }             // 透光系数 k_t，0 = 不透光；写进 mat_table 行 2（ART-STYLE §5.6）
}
public readonly record struct ToonRef(ToonSpec Spec);   // 值语义，按内容比较；内核按它查材质缓存
// Godot 侧：WP-04 的材质库（Campus.Style）把 ToonSpec 映射到白名单着色器的 ShaderMaterial，
// 并分配 7 位材质 ID（全场不超过 128 个，含剖切 ID），写进 ROUGHNESS；描线和 toon 都按 ID 查全局数据纹理 mat_table
// （描线的线表是 mat_table 的行 0，ART-STYLE §4、ENG-PERF §10.2）。影色由固有色按 ART-STYLE §3 推导，
// Shade 色名仍由调色板给出，只是数值改由 WP-37 按影色规则生成
```

```csharp
// 世界状态与帧顺序：内容模块不能自己写计时器
// 世界状态见 SYS-TIME §1；世界时间单调向前（WorldHours）；天气见 SYS-WEATHER §8
public enum Season { Spring, Summer, Autumn, Winter }
public enum ViewMode { Fly, Walk }
public readonly record struct Body(Vector3 Dir, float AltDeg, float AzDeg, float Intensity);
public readonly record struct Flow(int Rate);   // 世界秒 / 真实秒，Rate ∈ {0, 1, 10, 60, 360, 1440}，0 = 暂停；默认 1
[Flags] public enum WorldTransition : byte { None = 0, Time = 1, Season = 2, Weather = 4 }   // 几种过渡可以同时进行

// 天气（SYS-WEATHER §8）：界面的 8 格（SYS-WEATHER §1）对应下面 11 个内部状态
public enum WeatherKind : byte { Clear, FewClouds, Cloudy, Overcast, Fog, LightRain, Rain, Thunderstorm, AfterRain, Snow, AfterSnow }
public enum WeatherMode : byte { Auto, Manual }
public readonly record struct WeatherSel(WeatherMode Mode, WeatherKind Kind, float Intensity);
    // Auto 时 Kind 是前向过程当前的状态；Intensity 0–1，对应界面的轻 / 中 / 重三档
public readonly record struct WeatherVec(       // 天气向量：每帧插值；所有天气表现只读它和它写出的全局参数
    float Cloud, float CloudType, float Cb,     // 云量、云型、积雨云量
    float Overcast, float Precip, float PrecipKind,   // 阴度、降水量、降水类型（0 雨 → 1 雪）
    float Wet, float Puddle,                    // 湿润、积水
    float Fog, float GroundFog,                 // 雾密度倍率（晴为 1）、贴地雾
    float Storm, float SnowAdd,                 // 风暴度、新雪
    float Rainbow, float Lightning);            // 彩虹、闪光
public readonly record struct WeatherSpan(double FromHours, double ToHours, WeatherKind Kind, float Intensity);   // 按 WorldHours 计
public sealed record WorldPreset(double Clock, Season Season, WeatherKind? Weather = null, float? Intensity = null);

public interface IWorldState                // Kernel 的服务（WP-34），不单独做 Autoload
{
    long Rev { get; }
    long Instance { get; }                  // 世界实例号。启动后第一次得到世界状态为 1，此后每次 Load、LoadFile、NewGame 加 1；
                                            // 切图不变（SYS-SAVE §5.2、SYS-MAPS §5）。做平滑、带回差或保留历史的系统，看到它变了就直接落到新值
    double WorldHours { get; }              // 世界时间（h）：同一世界实例内单调递增，只增不减；暂停时不动
    double Clock { get; }                   // 美术钟点 0–24 h = (WorldHours + offset) mod 24（offset 见下文）
    double SeasonPos { get; }               // 旧网页版的 season：0–4，连续，循环；0 春 1 夏 2 秋 3 冬
    Season SeasonTarget { get; }
    Body Sun { get; }
    Body Moon { get; }
    float MoonPhase { get; }                // 默认 1 = 满月
    float Omega { get; }                    // 主光角速度 °/s；WP-03 据此决定量化转灯还是连续转动（ENG-PERF §10.1）
    Flow Flow { get; }
    bool Scrubbing { get; }                 // 正在拖动时间环（UI-VISUAL §7.5）
    WorldTransition Transition { get; }     // 位标志：快进、换季转场、换天气的过渡
    ViewMode ViewMode { get; }
    WeatherSel Weather { get; }             // 生效的天气选择（换季替换之后的值）
    WeatherVec WeatherNow { get; }          // 本帧的天气向量
    IReadOnlyList<WeatherSpan> WeatherAhead { get; }   // 前瞻 24 h 的天气队列，供快进和预报行（UI-NAV §4.10、UI-VISUAL §7.6）
    uint WeatherSeed { get; }               // 本存档的天气基础种子（SYS-WEATHER §6.4；每个存档一个，SYS-SAVE §5.4）
    bool IsNative(WeatherKind k);           // 该天气在目标季节里有没有；界面据此把该格画成禁用态（SYS-WEATHER §1、UI-VISUAL §7.6）
    double AnimTime { get; }                // 实时秒 mod 480，用 delta 累加，与 Clock 无关；旧网页版的 t = AnimTime / 480
    double Step12 { get; }                  // floor(AnimTime·StepRate)/StepRate，"一拍二"；StepRate 与全局参数 step_rate 同源
                                            // （QualityView.StepRate：帧率上限 30 时 15，其余 12；截图与门禁一律 12）
}

public enum ClockSetMode { Drag, Jump }
public interface IWorldControl              // 没有任何让世界时间倒退的方法；读档由 WP-34 自己的 world 存档段恢复，不经过 IWorldControl（SYS-SAVE §10）
{
    void SetClock(double hours, ClockSetMode mode = ClockSetMode.Jump);   // 跳到"下一次出现"的 hours（见下文）
    void Advance(double hours);                                // 前推 hours 小时（> 0，否则抛 ArgumentOutOfRangeException）：句点键 +1 h、脚本"前推 24 小时"
    void FlowTo(double clock, int rate);                       // SYS-TITLE §6、§7.4：以 rate 正向流逝，到下一次出现的 clock 正好停住（流逝变为 0）；
                                                               // clock 取 [0, 24)，"下一次出现"与 SetClock 同一语义；rate 必须是 Flow 的取值之一，否则抛 ArgumentOutOfRangeException；
                                                               // 停点由 WorldState 在推进那一步里夹住，不越过一帧；SetFlow 或再次 FlowTo 取消上一次未到点的 FlowTo。
                                                               // 新游戏过场用它（60×，简短 360×，关与跳过 1440×），终态与播放速度无关
    void SetSeason(Season s);                                  // 季节任选，一律正向转场（SYS-TIME §5）
    void SetFlow(int rate);                                    // rate 必须是 Flow 的取值之一，否则抛 ArgumentOutOfRangeException
    void SetWeather(WeatherKind kind, float? intensity = null);   // 进入手动；不合季节的格不生效（界面先用 IsNative 判断并提示）
    void SetWeatherIntensity(float intensity);
    void SetWeatherAuto();
    void Apply(WorldPreset p);                                 // 应用一组状态，例如点位推荐（见下文）
    void ResetDefault();                                       // "回到本图默认状态"：清水河上就是"回到落金初灯"（见下文，SYS-MAPS §5）
    void Freeze(double clock, Season season, double anim);     // 只给测试构建（--clock、--season、--anim）：启动时设初值，WorldHours 从 clock 起算
}

// 天气过程：WP-45 实现，注册为 Kernel 服务；只有 WorldState（WP-34）和 WP-45a 自己的存档段调用它
// （Reset 由新游戏调用，见 WeatherBoot），其他代码一律读 IWorldState 和天气类全局参数。有了这一层，WP-34 和 WP-45 可以并行开发
public readonly record struct WeatherQueueItem(WeatherKind Kind, double Minutes);
public sealed record WeatherBoot(            // Boot 解析 --weather* 后交给新游戏；新游戏在第一帧之前调 Reset（SYS-WEATHER §8、SYS-SAVE §5.5）
    uint Seed,                               // --weather-seed；没给时由 ISaveService.NewGame 取一次熵（§2 BannedApiAnalyzers 的唯一例外）。
                                             // 读档时不调 Reset，天气由 WP-45a 的 weather、map:<id>/weather 两个存档段恢复（SYS-SAVE §5.4）
    WeatherKind? Kind = null, float? Intensity = null, float? Wet = null,   // --weather（auto 为 null）、--weather-intensity、--wet
    double? RemainingMinutes = null,         // --weather-remaining
    IReadOnlyList<WeatherQueueItem>? Queue = null);   // --weather-queue
public readonly record struct WeatherTick(
    double WorldHours,
    double TargetHours,                      // 大于 WorldHours 表示正在快进（跳转、拨时间环、Advance、FlowTo、ResetDefault、Apply）
    double ClockOffset, Season Season, float SunAltDeg, int FlowRate, double RealDt);
public sealed record WeatherProcessState(    // 一张图的天气过程状态（落定值）；字段由 WP-45a 在 v1 里定，按 SYS-SAVE §3.5 只读、不含 Godot 类型，
    MapId Map, uint Epoch, int Segment,      // 至少包括：图、epoch、片段号、
    WeatherSel Selection,                    // 自动或手动的选择、
    IReadOnlyList<WeatherQueueItem> Queue, double QueueFromHours,   // 前瞻队列、
    double? PinFromHours, double? PinToHours, WeatherKind? PinKind, float? PinIntensity,   // 钉住窗口、
    double QuietUntilHours, float Wet, float Puddle,                // 保持晴的期限、湿润与积水余量、
    double LeftAtHours);                     // 离开这张图时的世界时间（切图时冻结，SYS-MAPS §5）
public interface IWeatherProcess
{
    void Reset(WeatherBoot b);               // 只在新游戏时调用
    void Step(in WeatherTick t);             // 每帧在钟点、季节推进之后调用：补前瞻队列，求本帧的天气向量
    WeatherSel Selection { get; }
    WeatherVec Now { get; }
    IReadOnlyList<WeatherSpan> Ahead { get; }
    uint Seed { get; }
    bool Transitioning { get; }              // 换天气的过渡进行中（WorldTransition.Weather）
    bool IsNative(WeatherKind k, Season s);
    void Select(WeatherSel sel);             // SetWeather、SetWeatherIntensity、SetWeatherAuto 转发到这里
    void Reseason(double worldHours, Season to, double clockOffset);   // 换季：当前相就地映射，之后的队列丢弃、前向重排（SYS-WEATHER §6.5）
    void Quiet(double worldHours, double untilHours);                  // 保持晴到 untilHours：新游戏与 ResetDefault（本图 quietUntilDusk 为真时）
    void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours);   // 钉住：Apply
    // 按图的过程与存档：
    void Enter(MapId map, string profile, double worldHours);          // 进图：恢复或新建本图的过程，补算到 worldHours；离开超过 72 h 则 epoch + 1 从现在重排（SYS-MAPS §5）
    void Leave(MapId map, double worldHours);                          // 离图：冻结在离开那一刻
    WeatherProcessState Capture(MapId map);                            // 存档：给出落定值，过渡中取终点（SYS-SAVE §4.2）；在 FramePriority.Save 那一步调用
    void Restore(MapId map, WeatherProcessState s);                    // 读档：在遮挡下直接落到存档的状态，不做过渡、不调 Reset
}

// 帧顺序用 Node.ProcessPriority（值越小越先执行，与节点在树里的位置无关；对应旧网页版的 FramePhase / onPhase）
// 共 17 步。任何两步都不得同值（同值时 Godot 按场景树顺序执行，就说不清先后，SYS-SAVE §11）。
// Stream（−500）只做选级与请求，StreamMount（900）做挂载与碰撞入树。
// ENG-PERF §5–§6 的帧内顺序、SYS-TIME §1 的每帧顺序表、SYS-MAPS §9 和 SYS-AUDIO §20 的表与本表逐行一致（同样的 17 个值、同样的工作包）
public static class FramePriority
{
    public const int Map = -1300;           // WP-46：推进切图状态机；在雾幕全黑的帧里分发 IMapAware；Mount 时写 map_rect；
                                            // Current、Context 在这一步换，同一帧里所有系统看到同一张图（SYS-MAPS §9）
    public const int Quality = -1200;       // WP-32：即时类设置与自动调节生效、分发 IQualityAware、写全局参数 quality；
                                            // 先于相机，同一帧里所有系统看到同一版本的 QualityView（SYS-GRAPHICS §8）
    public const int Camera = -1100;        // WP-06（含可达空间求值、写 edge_fog）、WP-36、WP-42；WP-49a 的 CinematicPlayer（SYS-TITLE §8.3）
    public const int WorldState = -1000;    // WP-34：推进世界时间、钟点和季节（含 FlowTo 的停点），再调 IWeatherProcess.Step 求天气向量（WP-45）
    public const int KeyLight = -900;       // WP-03：含按阴度改投影（SYS-WEATHER §7）、室内抬高阴影方向的最低高度（ENG-PERF §10.1）
    public const int Palette = -800;        // WP-37、WP-04：含天气修正量
    public const int Channels = -700;       // WP-03 通道求值、WP-39 时刻表：按有效太阳高度 h_eff 求值（SYS-WEATHER §7）
    public const int Motion = -650;         // WP-26 风场（含天气系数）、一拍二、偶发事件、钟面指针与钟室钟锤（SYS-MOTION §9.5）；WP-45 降水数量与闪电
    public const int SeasonFx = -640;       // WP-38 季节氛围
    public const int Medium = -600;         // WP-07：介质判定，写介质全局参数与 interior_light，更新 IMediumState；室内运行时（按层显示、软停信号，WLD-INTERIORS §6）；
                                            // 自动门的状态机：读本帧行走物理之后的观者位置，推进推开与闭门器，阶段变化时发 IInteriorDoors.Changed（WLD-INTERIORS §4.1）
    public const int Acoustics = -590;      // WP-53c：本帧听者处的声学与路由，含门户开度（自动门读 IInteriorDoors，SYS-AUDIO §20）
    public const int Stream = -500;         // WP-02 流式选级与请求、WP-43 碰撞格请求；切图期间（Unload 到 Mount）不发新请求
    public const int Systems = 0;           // 其余系统（含 WP-48c 的 IAtlasService：10 Hz 的区域查询与追踪到达检查）
    public const int Audio = 50;            // WP-53a、53b、53d：仲裁、权重、排程、写播放器音量（SYS-AUDIO §20）
    public const int Hud = 100;             // WP-35：HudModel、MotionHub、UiThemeClock（写 ui_night）；WP-48b 的小地图与大地图
    public const int StreamMount = 900;     // WP-02 挂载（每帧 ≤ 1.0 ms）、WP-43 碰撞入树（预算内分帧）；新挂上的室内房间与门户下一帧生效
    public const int Save = 1000;           // WP-50：每帧最后一步，拍快照；读档时在这一步恢复，下一帧所有系统同时看到新世界（SYS-SAVE §11）
}
// 行走的物理在 _PhysicsProcess 里按固定 120 Hz 子步推进（旧网页版的 'physics' 阶段，SYS-WALK §12）；观者的脚步等事件在那里当场发声，不等 Audio 那一步（SYS-AUDIO §20）。
// 自动门的开关门声在 Medium 那一步门的阶段变化时当场发出；门体碰撞的启停从下一个物理步起生效（WLD-INTERIORS §4.1）

public enum ChannelKind { Scalar, Vec2, PerInstance }
public enum ChannelRate { Continuous, Step12 }   // Step12：按 IWorldState.Step12 步进，即 floor(AnimTime·StepRate)/StepRate，StepRate 同 step_rate
public sealed record ChannelDecl(
    ChannelId Id, ChannelKind Kind, float Min, float Max, ChannelRate Rate,
    string? Source = null);                 // 由灯光时刻表或世界状态驱动，例如 "window.dorm"、"lamp.street"（SYS-TIME §6）

// 随季节、昼夜显隐的物体（雪人、校庆道旗、毕业行李箱等）：几何照常生成，由内核按掩码显隐，不重建格子
[Flags] public enum SeasonMask : byte { Spring = 1, Summer = 2, Autumn = 4, Winter = 8, All = 15 }
public enum NightRule { Any, Only, Never }
public readonly record struct VariantTag(SeasonMask Seasons = SeasonMask.All, NightRule Night = NightRule.Any);

public readonly record struct WindSample(float Gust, float Phase);
public interface IWindField                 // WP-26
{
    WindSample Sample(float x, float z, double animTime);   // CPU 侧：植被近池重建、粒子槽位
}
// 着色器侧是 res://style/shaders/motion_wind.gdshaderinc，与 Sample 同一公式；G2 比对两边的输出
```

**世界时间与天气的语义**（行为细节见 SYS-TIME §1、SYS-WEATHER §6、SYS-SAVE §5、SYS-MAPS §5）：

- **单调时间**：在同一世界实例内，`WorldHours` 只增不减。`Clock = (WorldHours + offset) mod 24`。换季时为保持太阳在弧线上的位置 u 不变而重算 offset，钟点数字可能变小，但太阳不倒走、`WorldHours` 不减。
- **流逝**：`Flow(Rate)` 只有暂停（0）和 1×、10×、60×、360×、1440×，默认 1×。季节不自动推进。
- **`SetClock(h, mode)` 跳到下一次出现的 h**：前进量 Δ = (h − 目标钟点) mod 24，0 ≤ Δ < 24。目标钟点取当前的追随目标，不取弹簧追随中的显示值，所以连续拖动不会算错圈。
  - `Jump`（点刻度、点预设）：延时摄影式快进，用时 0.6 + 0.18 × Δ 秒，上限 3 s。
  - `Drag`（拖动时间环，UI-VISUAL §7.5）：界面只在把手顺时针移动时调用，逆时针拖时把手停在原处、不调用；连续拖满一圈就是 +24 h。目标由临界阻尼弹簧追随；拖动时流逝暂停，松手 1 s 后继续。
- **`Advance(hours)`**：从当前目标再往前推 hours 小时，按 `Jump` 的快进做。`.` 键单击 +1 h，按住连续前进。界面没有 −1 h 的按钮，也没有往回的快捷键。
- **`ResetDefault()`**（"回到本图默认状态"，也是新游戏的初值，SYS-SAVE §5.5）：读当前图 `map.json` 的 `world.default` 与 `quietUntilDusk`（SYS-MAPS §5），执行写法不变。清水河上就是"回到落金初灯"：换到秋季（正向换季转场）+ 快进到下一个 17:30（换季后的钟点已在 17:22–17:40 之间就不动）+ 流逝 1× + 自动天气 + 天气保持晴到秋季黄昏带结束（约 19:36）。天气种子不变，不移动镜头（SYS-WEATHER §6.6）。关键动作落定后请求一次保存（`SaveReason.Settled`，SYS-SAVE §4.1）。
- **`FlowTo(clock, rate)`**（SYS-TITLE §6、SYS-TITLE §7.4）：以 `rate` 正向流逝，到下一次出现的 `clock` 时正好停住（流逝变为 0），由过场的世界轨在结束时 `SetFlow(1)`。新游戏的完整、简短、关和跳过都走同一条世界轨，只是倍率不同（60×、360×、1440×），所以终态逐位相同。
  - 取值：`clock` 是钟点，0 ≤ clock < 24（例如 17.5），"下一次出现"与 `SetClock` 同一语义，所以只会向前；`rate` 必须是 `Flow` 的取值之一（0、1、10、60、360、1440），否则抛 `ArgumentOutOfRangeException`。
  - 停点：由 WorldState 在 `FramePriority.WorldState`（−1000）那一步里夹住，不会越过一帧；途中天气照前向过程推进。
  - 取消：调用 `SetFlow` 或再次 `FlowTo`，就取消上一次还没到点的 `FlowTo`；过场被跳过时，世界轨改用 1440× 的 `FlowTo` 跑完（SYS-TITLE §5.2）。
- **`Apply(WorldPreset p)`**：季节不同就正向换季；前向跳到下一次出现的 `p.Clock`；把 `p.Weather`（为 null 时按晴）钉住，从推荐钟点前 0.5 h 到后 2 h，之后自然演化，天气模式为自动（SYS-WEATHER §6.6）。
- **换天气**：手动选择、换季替换（雨 ↔ 雪等，SYS-WEATHER §1）和自动过程的转移都只向前推进；快进途中过程完整推进，画面只做一次"起点天气 → 终点天气"的过渡，放在快进的 30–80% 段（SYS-WEATHER §6.5）。
- **确定性**：同一钟点、同一哈希的东西结果相同（例如窗灯按哈希亮灭）；同一构建 + 同一种子 + 同一串输入（命令行、Movie Maker 固定步长、脚本化的时间推进）得到同样的画面（SYS-WEATHER §6.4）。
- **读档与新游戏**（SYS-SAVE §5）：
  - 读档、新游戏是换一个世界实例，`Instance` 加 1，不算时间倒流；做平滑、带回差或保留历史的系统（主光量化、调色板追随、界面主题锁存、声音的平滑量等），看到 `Instance` 变了就直接落到新值，不做过渡；
  - 读档的顺序：先恢复世界段与天气段（全局时间、各图的天气状态），再 `IMapService.Travel(LoadSave)`，位姿和模式取视角段（SYS-MAPS §4.4）；
  - 关着游戏的那段现实时间、开始页停着的时间都不计入世界时间（SYS-TITLE §6、SYS-SAVE §6）；
  - 新游戏一律从默认图的默认状态开始，清水河上就是落金初灯；天气种子每个存档一个。
- **多地图**（SYS-MAPS §5）：`WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` 全局共享，只有一条时间线，切图时不变，`Instance` 也不变；天气按图各一份前向过程，离开时冻结，进图时在雾幕里补算到当前时间。

**全局着色器参数**：世界状态、天气、画质、地图和风格的三十多个量，以及几张数据纹理（调色板、灯光通道、窗覆盖、路灯灯光表、树种表、云噪声、降水遮蔽高度图、共用灯条表、地面遮罩 2、材质表 `mat_table`）走全局着色器参数（ENG-PERF §1.5；天气的 7 项和纹理见 SYS-WEATHER §8，画质的 `quality` 见 SYS-GRAPHICS §8）。全局参数不支持数组，数组一律做成数据纹理。全部参数在 `project.godot` 的 Shader Globals 里集中登记（主代理），每个参数只有一个写入方；C# 侧的名字常量由 WP-01 维护在 `game/kernel/renderer/`，G5 比对两边一致。任何代码都不得从脚本读回全局参数。

- 参数名的 C# 常量集中在 `Campus.Contracts` 的静态类 `ShaderGlobals`（例如 `ShaderGlobals.WeatherA = "weather_a"`、`ShaderGlobals.GroundMask2 = "ground_mask2"`），由 ENG-PERF §1.5 的登记表生成；各包只用常量，不写字符串字面量。G5 比对 `ShaderGlobals`、`project.godot` 的 Shader Globals 和 `globals.gdshaderinc` 三者一致（ENG-PERF §1.4）。
- **地图、边缘雾、光柱与室内光的全局参数**（都登记进 ENG-PERF §1.5）：

  | 名称 | `ShaderGlobals` 常量 | 类型 | 内容 | 写入方 | 更新 |
  | --- | --- | --- | --- | --- | --- |
  | `map_rect` | `MapRect` | vec4 | 全图数据纹理覆盖的 xz 范围 (x0, z0, 1/宽, 1/深)，uv = (p.xz − xy) × zw。清水河取 x、z 都是 −1280…1280，即 `map.json` 的 `dataRect`（SYS-MAPS §10）；不是箱庭的包围盒 | WP-46 | 切图的 Mount 那一步 |
  | `edge_fog` | `EdgeFog` | float | 旁观飞到可达空间边缘时的边缘雾，0–1；只在 Playing 状态作用（WLD-OUTSKIRTS §8.3、SYS-TITLE §7.2） | WP-06 | 每帧 |
  | `shaft` | `Shaft` | vec4 | 光柱强度、近镜淡入起点、终点、顺轴衰减（ART-STYLE §7.6） | WP-39 | 每帧 |
  | `interior_light` | `InteriorLight` | vec4 | 室内光斑增益、室内光柱整体系数、阴影方向抬高的进度、保留（ART-STYLE §3.4） | WP-07（介质那一步统一写，唯一写入方；z 的进度由介质切换求出，WP-03 在主光那一步从同一份 C# 状态读，不读回全局参数，ENG-PERF §1.5） | 每帧 |

  - `ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid` 以及以后任何"全图"纹理一律按 `map_rect` 采样；`globals.gdshaderinc` 提供 `map_uv()`、`in_map()`（WP-01 生成），范围外取"无数据"的默认值。
  - `ui_night` 的写入方是 WP-35：`UiThemeClock` 按太阳高度带回差锁存，切换时 0.6 s 扫描前沿走完（UI-VISUAL §11.6）；它是界面层唯一可写的全局参数。
  - 数据纹理 `mat_table`（128×4 RGBA8，最近邻，WP-04 持有、WP-05 共读，启动时写入）里含描线的线表，WP-05 不另持线表；`window_override` 为 256×256（全校立面约 5.8 万个开间，WLD-INTERIORS §12）；不做接触影，没有 `contact_spheres`。

**补充**：

- `Build` 写在 gen 里实现 `IContentModule` 的类中（`*Module.cs`）。Godot 侧包的根节点写在 `*Node.cs` 里，标 `[KernelNode]`。
- `GeometryBundle` 里的 float 数据由内核统一打包：开发态实时生成时由内核打包，平时由烘焙（开发烘焙器或本机烘焙）打包（§4）。格式是 `ARRAY_FLAG_COMPRESS_ATTRIBUTES`，每顶点 20 B：位置 4×u16（按格包围盒归一化，第 4 分量存切线角度）、八面体法线 2×u16、UV 2×u16、COLOR RGBA8（色号、AO、标志）。每个表面不超过 65 536 个顶点；相邻格写统一的格包围盒，避免量化接缝（ENG-BUDGET §7、ENG-PERF §1）。
- 生成器的内层循环用托管数组；跨引擎边界时，每个表面只拷贝一次。
- 部件仍按语义级别（L0、L1）分别给几何；每级几何内部可以另给索引前缀形式的网格 LOD（`GeometryBundle.Lods`），由内核写进 `AddSurfaceFromArrays` 的 `lods` 字典。
- 引擎锁定为 Godot 4.7.2-stable .NET 版（`tools/godot-version.txt`；csproj 写 `Sdk="Godot.NET.Sdk/4.7.2"`）。升级要经主代理评审（ENG-WP §5）。
- 容器怎么选（按块合并的 ArrayMesh、MultiMesh、独立实例），见 ENG-PERF §1。
- JSON 一律用契约程序集里的 `ContractJson.Options` 读写：字段 camelCase，枚举按字符串，向量写成数组 `[x, y, z]`。多态类型（覆盖层操作、碰撞体）的类型字段必须写在每个对象的第一个字段（.NET 8 的 System.Text.Json 要求）。存档 DTO 用各包自己的源生成 `JsonSerializerContext`，约定与 `ContractJson.Options` 一致，ID 类的 record struct 写成字符串；存读档不得用不带 `JsonTypeInfo` 的重载（SYS-SAVE §3.5）。`map.json`、`interiors.json`、`markers.json`、过场 JSON 的 schema 放在 `game/contracts/schemas/`。

**窗灯**：每扇窗有一个随机阈值，灯光通道的值超过阈值时这扇窗就亮。通道值由灯光时刻表按"建筑类别 × 灯种"驱动（SYS-TIME §6，共 32 个通道），由 WP-03 每帧写进 32×1 的全局数据纹理，不增加任何绘制调用；窗号基址放在顶点属性里（`GeometryBundle.Uv2`）。同一钟点、同一哈希的窗，亮灭结果相同；阴雨天通道按有效太阳高度求值，灯会提前亮（SYS-WEATHER §7）。窗覆盖纹理为 256×256；开放室内的房间灯按房间哈希读本楼的通道，不要求与窗灯逐间对应（WLD-INTERIORS §5.1、WLD-INTERIORS §6.3）；每个开放入口在灯表里登记一盏门口灯（WLD-INTERIORS §4）。

```csharp
// 风格化数据覆盖层：OSM 数据永不改写。"op" 写在每个对象的第一个字段
public sealed record OverlayFile(ZoneId Zone, WpId Owner, IReadOnlyList<OverlayOp> Ops)
{
    public MapId? Map { get; init; }      // 只用于自检；归属以文件所在目录（maps/<地图>/overlay/）为准（SYS-MAPS §8）
}
public enum OverlayLevel { Minor, Design }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(ReshapeOp), "reshape")] [JsonDerivedType(typeof(HeightOp), "height")]
[JsonDerivedType(typeof(StyleOp), "style")] [JsonDerivedType(typeof(HideOp), "hide")] [JsonDerivedType(typeof(AddOp), "add")]
public abstract record OverlayOp;
public sealed record ReshapeOp(FeatureId Target, PolygonRings Rings, OverlayLevel Level, string Reason) : OverlayOp;
public sealed record HeightOp(FeatureId Target, float? Height, int? Storeys, string Reason) : OverlayOp;
public sealed record StyleOp(FeatureId Target, BuildingFamily Family, FacadePreset? Facade) : OverlayOp;
public sealed record HideOp(FeatureId Target, string Reason) : OverlayOp;
public sealed record AddOp(FeatureId Id /* 必须以 design: 开头 */, DesignKind Kind,
    OverlayGeometry Geometry,                                      // 多边形环或折线
    float? Height, JsonElement? Props, string Reason) : OverlayOp;

// 室内覆盖层（WLD-INTERIORS §2.0、WLD-INTERIORS §14）：maps/<地图>/overlay/interior/<区>.interior.json，各区精修室内的包独占本区一份；
// 记封口与配置型楼的楼型配置。字段由 WP-51 定，schema 放 game/contracts/schemas/；"op" 写在每个对象的第一个字段
public sealed record InteriorOverlayFile(ZoneId Zone, WpId Owner, IReadOnlyList<InteriorOverlayOp> Ops)
{
    public MapId? Map { get; init; }      // 只用于自检；归属以文件所在目录为准
}
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(SealOp), "seal")] [JsonDerivedType(typeof(InteriorConfigOp), "config")]
public abstract record InteriorOverlayOp;
public enum SealKind : byte { IronGate, FireDoor, GlassPartition, FoldingGate, RollerShutter, ShelfWall }
    // 收口物：锁着的铁栅门、关着的防火门、玻璃隔断、伸缩门、卷帘门、书架墙端板（WLD-INTERIORS §2.0）
public sealed record SealOp(FeatureId Building, int Storey, Vector2 A, Vector2 B, SealKind Kind, string Reason) : InteriorOverlayOp;
    // 封口：楼层、位置（平面上的一段）、收口物类型。碰撞记 WallCollider、SurfaceTag.DoorClosed，顶 ≥ 1.5 m（WLD-INTERIORS §10）；
    // 平面记 PortalKind.Sealed；SYS-WALK §6 可行走覆盖层的 seal 区域引用同一份封口
public sealed record InteriorConfigOp(FeatureId Building, string Profile, JsonElement Config, string Reason) : InteriorOverlayOp;
    // 配置型楼给 GenericInteriorProvider 的楼型配置：楼型、各层房间配比、开门比例、主痕迹的风格倾向（WLD-INTERIORS §14）

public enum FeatureSource { Osm, OsmOverlay, Design }             // 每个要素标明来源
public interface IWorldModel
{
    IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; }
    WorldNetwork Network { get; }          // Roads、Waterways（中心线）、Bridges、Culverts、Trees
    ISpatialIndex Spatial { get; }
    IInteriorIndex Interiors { get; }      // 室内索引（WP-51 烘焙，WP-08 随空间索引加载，约 50 KB，WLD-INTERIORS §15）
    IOutskirtsModel? Outskirts { get; }    // 外围数据；没有外围数据的图为 null（WLD-OUTSKIRTS §12.3）
}
public interface ISpatialIndex
{
    IReadOnlyList<FeatureId> Query(Box3 box);
    float GroundY(float x, float z);       // 也是常驻的低精度地形高度：旁观速度律、落点搜索用（SYS-VIEW §2）
    CameraMedium MediumAt(Vector3 p);      // Interior 的楼与层读室内索引
    // 行走模式用（SYS-WALK §13）
    float? SupportY(float x, float z, float yHint);
    WalkClass WalkClassAt(float x, float z);
    Vector3? NearestSafe(Vector3 p, float maxDist);
    // 常驻栅格的原点与尺寸从 MapDescriptor.DataRect 换算，不写死（SYS-MAPS §10）
}
```

覆盖层的规则：

- 轮廓调整在 4 m 以内算轻微（`Minor`）。超过 4 m，或者新增设计物体，必须写明理由；门禁会生成差异报告，供主代理审阅。
- 两个覆盖层改同一个要素时报错，除非其中一个已经认领了该要素。
- 照片和 OSM 只定结构，细节按氛围做艺术化处理；为画面新增的元素（`add`）和删掉的难看元素（`hide`）都写进本图的覆盖层，在 `Reason` 里注明属于设计处理并写明理由（CORE-INDEX 的作品定位）。覆盖层按图存放，WP-08 只扫当前图的目录。

```csharp
// 相机（WP-06）与介质呈现（WP-07），详见 SYS-VIEW
public readonly record struct Pose(Vector3 Position, float YawDeg, float PitchDeg, float? FovDeg = null);

public interface ICameraRig
{
    Pose Pose { get; }
    float Speed { get; }
    ViewMode Mode { get; }                   // Fly 由 WP-06 驱动，Walk 由 WP-42 驱动
    void SetPose(Pose p);                    // 用于出生位置和测试构建，没有过渡动画；另用于 Title、Entering、Leaving 状态下
                                             // CinematicPlayer 的逐帧驱动，这三个状态里 WP-06 的相机不积分输入（SYS-TITLE §7.4）
    ITransitHandle Transit(Pose to, TransitOptions? o = null);   // 点位跳转与同图传送（WP-36，UI-NAV §7、SYS-ATLAS §7），可被输入打断
    IDisposable LockInput(InputLockReason reason);   // 输入占用按原因计数：没有任何占用时才接受观者输入；Dispose 解除这一次占用
    ITransitHandle? ActiveTransit { get; }   // 进行中的跳转或传送，没有时为 null（SYS-AUDIO §19）
    float EdgeFog { get; }                   // 与全局参数 edge_fog 同一个值（WLD-OUTSKIRTS §8.3），音频据此压低远处的声音
}
public enum InputLockReason : byte { Atlas, Cinematic, MapTravel }   // 大地图（SYS-ATLAS §4.1）、开始页与过场的交还控制（SYS-TITLE §7.3）、
                                                                     // 换图雾幕（SYS-MAPS §4.2）；只在末尾追加（以后加对话、剧情过场、菜单）
public enum EntryPath { Skylight, Door, WaterSurface }
public enum TransitPreference : byte { Auto, Teleport }   // Auto 按距离选滑移、弧线或雾幕弧线（UI-NAV §7.1）；Teleport 是地图传送（SYS-ATLAS §11）
public sealed record TransitOptions(EntryPath? Entry = null, Vector3? LandOn = null /* 行走落点 */)
{
    public TransitPreference Prefer { get; init; } = TransitPreference.Auto;
}
public enum TransitKind { Slide, Arc, Veil, Teleport }   // Teleport：地图传送的雾幕瞬移，雾幕盖满 → 移位 → 等目的地就绪 → 散开（SYS-ATLAS §7.2）
public enum TransitResult { Arrived, Cancelled }
public interface ITransitHandle
{
    TransitKind Kind { get; }
    float Progress { get; }
    void Cancel();
    Task<TransitResult> Done { get; }        // 在主线程完成
}

public static class PoseMath
{
    public static Pose PoseFacing(Vector3 target, float azDeg, float elDeg, float dist) { … }   // 换算辅助函数，不是观看模式
}

public enum MediumKind { Air, Interior, Underground, Underwater, FogSea }
public readonly record struct CameraMedium(  // 值类型，每帧查询不分配内存
    MediumKind Kind,
    FeatureId? Feature = null,               // Interior：所在楼；Underwater：水体
    int Storey = 0,                          // Interior
    float WallDist = 0,                      // Interior
    float Depth = 0);                        // Underground、Underwater、FogSea

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MediumPresenterAttribute(string id) : Attribute { public string Id { get; } = id; public string Owner { get; set; } = ""; }
public interface IMediumPresenter            // Godot 侧实现（主线程）
{
    IReadOnlyList<MediumKind> Kinds { get; }
    void Enter(IRuntimeContext rt, in CameraMedium m);
    PostOverride Update(IRuntimeContext rt, in CameraMedium m, float blend);
    void Exit(IRuntimeContext rt);
}
public interface IMediumState { CameraMedium Current { get; } MediumKind From { get; } MediumKind To { get; } float Blend { get; } }
    // WP-07 实现，与全局参数 medium 同源的只读值；音频、界面读（SYS-AUDIO §19）
```

```csharp
// 室内（照 WLD-INTERIORS §15：IInteriorProvider 按层提供；只开放 WLD-INTERIORS §2 的开放清单，
// 以后加一栋楼只在 interiors.json 加一项、再注册一个提供者或给通用提供者加一条楼型配置，不改契约，也不改流式架构，WLD-INTERIORS §14）
public enum InteriorDetail : byte { Proxy = 0, Shell = 1, Full = 2 }   // I0 兜底、IS 只留壳、IF 完整
public enum ZoneKind : byte { Room, Corridor, Stair, Atrium, Lobby, Wc, Service, Gallery }
public enum PortalKind : byte { Door, StairOpening, AtriumOpening, Window, Sealed, ElevatorStop }   // Sealed：封口
public sealed record StoreyInfo(int Index, float FloorY, float Height, IReadOnlyList<Vector2> Footprint);
public sealed record ZoneVolume(int Storey, int Id, ZoneKind Kind, IReadOnlyList<Vector2> Ring,
    ChannelId LightChannel, uint LightHash, bool Open);                     // Open：属于开放范围
public sealed record PortalDecl(int Storey, int ZoneA, int ZoneB, Vector3 A, Vector3 B, float Height, PortalKind Kind);
public sealed record EntranceDecl(string Id, Vector3 Outside, Vector3 Inside, float ClearWidth, bool Open)
{   // 开放楼的入口门固定敞开，作为"能进"的信号，不随观者开合（WLD-INTERIORS §4）；
    // Outside：门外落点（门外 2.5 m），供地图传送（SYS-ATLAS §7.3）；Inside：门内 3 m 的点
    public Vector3 Center { get; init; }       // 门中心（SYS-ATLAS 的室内门口标记、G18 的 ≤ 0.5 m 比对，WLD-INTERIORS §15）
    public float YawDeg { get; init; }         // 门外法线的方位角，0 = 北、90 = 东，与点位偏航同一约定（WLD-INTERIORS §2.17）
}
public sealed record ElevatorDecl(string Id, int StoreyA, int StoreyB, Box3 CabA, Box3 CabB);   // 关门换位两站（WLD-INTERIORS §3）

// 会动的门（参数以 WLD-INTERIORS §4.1 为准）：开放室内里能进的房门、开放路线上的楼梯间防火门和走廊玻璃门，
// 观者走近自动推开，走过去后由闭门器缓缓关上，带开门和关门声；不加交互键（UI-NAV 的"没有可以点选的东西"不变）。
// 推开 0.55 s 到 90°（一拍二步进）；门扇朝观者前进的方向开、离观者而去，两个方向都能开，方向在运行时定；
// 观者离开扫掠区 1.5 m 以外再过 0.8 s 回关：前 80° 用 2.5–3.5 s（防火门 3.5 s），最后 10° 快速落锁；回关途中观者再走近就立即重新推开。
// 门扇转动和停在开位时都没有碰撞，关好（开度 < 2°）之后才有碰撞。开放楼的入口外门不在此列（固定敞开），锁着的门（DoorClosed）也不在此列
public enum DoorLeafKind : byte { RoomDoor, FireDoor, GlassSwing }      // 木质房门、楼梯间防火门、玻璃平开门：决定门体的 Foot 和开关门声
public sealed record DoorLeafDecl(string Id, int Storey, int ZoneA, int ZoneB,
    Vector3 Hinge, float ClosedYawDeg, float OpenDeg, float Width, float Height, DoorLeafKind Kind,
    float TriggerM = 1.5f);                  // 静止时的触发半径；运动时放大为 max(TriggerM, v × 0.75 s)，保证观者到门洞前门已开过 75°（WLD-INTERIORS §4.1）；
                                             // OpenDeg 只给开到的角度，不给方向
public sealed record InteriorLayout(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<ZoneVolume> Zones, IReadOnlyList<PortalDecl> Portals, IReadOnlyList<EntranceDecl> Entrances,
    IReadOnlyList<ElevatorDecl> Elevators, IReadOnlyList<int> OpenStoreys, IReadOnlyList<int> AtriumVisibleStoreys,
    Box3 Bounds, uint PlanHash)
{
    public IReadOnlyList<DoorLeafDecl> Doors { get; init; } = [];          // 会动的门扇
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InteriorProviderAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
    public string Map { get; set; } = "";    // SYS-MAPS §7
}
public interface IInteriorProvider           // gen 里实现，纯函数；注册即开放（G19c 核对开放清单 = 注册的提供者 = 门开着的楼）
{
    IReadOnlyList<FeatureId> FeatureIds { get; }
    int Priority { get; }
    bool Walkable => true;
    InteriorLayout Layout(BuildContext ctx, FeatureId id);
    ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail);
    IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey);   // 门扇不进烘焙碰撞，见 IInteriorDoors
    NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey) => NavFloorGraph.Empty(id, storey);   // WLD-INTERIORS §13
    AcousticsDecl Acoustics(BuildContext ctx, FeatureId id) => AcousticsDecl.Empty;   // 房间与门户按层声明（SYS-AUDIO §19）；没写时退到声学网格
}
// ContentOutput 里室内层的实例一律带 IgnoreOcclusionCulling 的标记，室内遮挡体单独成表（WP-33 管理显隐，WLD-INTERIORS §15）

// 室内索引：WP-51 烘焙，WP-08 随空间索引加载
public sealed record InteriorIndexEntry(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<EntranceDecl> Entrances, bool Open, string? ProviderId);
public interface IInteriorIndex
{
    InteriorIndexEntry? Get(FeatureId id);
    IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r);       // 入口预取、地图图层（SYS-ATLAS 的"可进入的室内"）、G19
}

// 门的运行时（Godot 侧，WP-07 的 InteriorDirector 实现，FramePriority.Medium 那一步推进，WLD-INTERIORS §4.1）
public enum DoorPhase : byte { Closed, Opening, Open, Closing }
public readonly record struct DoorEvent(FeatureId Building, string DoorId, DoorPhase Phase, Vector3 Position, ulong Tick);
public interface IInteriorDoors             // Kernel 服务；只读
{
    DoorPhase PhaseOf(FeatureId building, string doorId);
    float OpenOf(FeatureId building, string doorId);   // 门扇开度 0–1；声学门户的开度读同一个值（AcousticPortalDecl.Door，SYS-AUDIO §10.4）
    event Action<DoorEvent>? Changed;        // 阶段变化时发：WP-53d 放开门、关门、落锁声（进入 Opening、Closed 时；停在开位和回关途中无声），
                                             // WP-43 在 Closed 时启用、离开 Closed 时停用这扇门的运行时门体（StaticBody3D，只切 CollisionLayer，不增删形状）
}
// 规则：门扇转动只由观者位置触发，与钟点、季节、天气无关，所以"可走性与时刻无关"（WLD-INTERIORS §10、ENG-BUDGET §7）不受影响；
// 运行时门体不进烘焙碰撞，G19e 的碰撞哈希不含它；门在 Closed 时也只是一块薄盒，观者走近就先推开，不会挡路；
// 读档、切图、换实例时所有门直接落到 Closed，不放声音；门的状态不进存档
```

```csharp
// 点位（照 UI-NAV §6 冻结，数据在 game/data/maps/<地图>/waypoints.json）
public readonly record struct LocalizedName(string Zh, string En);
public enum WaypointGroup { Overview, Core, South, West, North, East, Edge }
public sealed record WaypointStand(Vector3 Feet, float YawDeg);
public sealed record WaypointRecommend(double Clock, Season Season, WeatherKind? Weather = null);
    // Weather 为 null 时按晴钉住；应用时转成 WorldPreset 调 IWorldControl.Apply（UI-NAV §6、SYS-WEATHER §6.6）
public sealed record Waypoint(
    string Id,                               // "P00" 起（UI-NAV §6）；室内点位 P16–P23（WLD-INTERIORS §2.17），以后从 P24 往后编；
                                             // 只在本图内唯一，跨图引用用 WaypointRef
    LocalizedName Name, WaypointGroup Group,
    Pose Fly,                                // 旁观位姿
    WaypointStand? Stand,                    // 行走落点；没有就是"仅旁观"
    EntryPath? Entry,
    WaypointRecommend Recommend);
```

```csharp
// 观察模式与碰撞（照 SYS-WALK §13 冻结，只增不改）
public enum WalkClass { Walk, Interior, Wade, Building, Water, Forbidden, Outside, Steep }
public enum SurfaceTag { Ground, Stair, Barrier, Wall, Prop, Water, Invisible, Boundary, DoorClosed }
    // Invisible：可行走遮罩边界的隐形墙（SYS-WALK §6）
    // Boundary（WLD-OUTSKIRTS §7）：校界栏杆、关闭的校门与闸机、车道抬杆、河渠格栅、校门处的隐形墙；离它 0.6 m 内缓停，顶住 0.8 s 提示"校外暂未开放"
    // DoorClosed（WLD-INTERIORS §4）：关着的门所在的楼体墙段，以及开放楼里锁着的房门、封口的防火门；顶住 0.8 s 提示"此处暂不开放"
    // 自动门（DoorLeafDecl）关好时的运行时门体用 Wall，Foot 按门扇材质（WoodDoor、Metal、Glass），WLD-INTERIORS §4.1

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(WallCollider), "wall")] [JsonDerivedType(typeof(RampCollider), "ramp")]
[JsonDerivedType(typeof(PostCollider), "post")] [JsonDerivedType(typeof(BoxCollider), "box")]
[JsonDerivedType(typeof(SlabCollider), "slab")] [JsonDerivedType(typeof(HeightFieldCollider), "heightField")]
[JsonDerivedType(typeof(SlopedWallCollider), "slopedWall")]
public abstract record Collider(SurfaceTag Tag)
{
    public FootMat Foot { get; init; } = FootMat.Auto;    // 脚步与接触声的材质（SYS-AUDIO §9.3）；Auto 由表面类型和地面栅格推断
}
public sealed record WallCollider(Vector2 A, Vector2 B, float HalfT, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);
public sealed record RampCollider(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float Thick, SurfaceTag Tag) : Collider(Tag)  // 台阶坡面、桥面
{
    public StairSpec? Stair { get; init; }               // 楼梯的踏步规格，供脚步声（SYS-AUDIO §9.3）
}
public sealed record PostCollider(Vector2 C, float R, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);     // 树干、灯杆
public sealed record BoxCollider(Matrix4x4 At, Vector3 Half, SurfaceTag Tag) : Collider(Tag);                // 长椅、花池
public sealed record SlabCollider(IReadOnlyList<Vector2> Ring, float Y, float Thick, SurfaceTag Tag) : Collider(Tag);  // 室内楼板
public sealed record HeightFieldCollider(TileKey Tile, short[] HeightsCm, SurfaceTag Tag) : Collider(Tag)      // 地面：65×65，1 m 间距（WP-15）
{
    public byte[]? FootMatHalfMeter { get; init; }        // 128×128 的 FootMat 栅格（SYS-AUDIO §9.3）
    public IReadOnlyList<FootPatch>? Patches { get; init; }   // 井盖、箅子、排水沟这类点状特征
}
public sealed record SlopedWallCollider(Vector2 A, Vector2 B, float HalfT,
    float Y0a, float Y1a, float Y0b, float Y1b, SurfaceTag Tag) : Collider(Tag);
    // WLD-INTERIORS §10：斜顶薄墙。A 端底 Y0a、顶 Y1a，B 端底 Y0b、顶 Y1b；宽梯井（> 0.2 m）的栏杆，顶面取踏步前缘线以上 1.5 m
// 图元到 Godot 形状的映射归 WP-43（SYS-WALK §13）：Wall → 薄 BoxShape3D（楼体顶取地面以上约 300 m），
// Ramp → 斜放的 BoxShape3D 或 8 顶点 ConvexPolygonShape3D，Post → CylinderShape3D，Box → BoxShape3D，
// Slab → 凸分解，HeightField → HeightMapShape3D 65×65，SlopedWall → 8 顶点 ConvexPolygonShape3D
// 楼梯：斜面 26–34°、上限 40°，坡顶与上平台齐平、坡底埋进下层地面 ≥ 0.2 m，SYS-WALK §12 的登阶层必须开；梯井 ≤ 0.2 m 时整面做墙（WLD-INTERIORS §10）

public readonly record struct SweepHit(Vector3 Point, Vector3 Normal, float T, SurfaceTag Tag);
public readonly record struct SupportHit(float Y, SurfaceTag Tag);
public interface ICollisionWorld             // WP-43，Godot 侧实现
{
    bool Ready(Vector3 p, float r);          // ENG-PERF §5 第 5 条用它判断
    SweepHit? Sweep(Vector3 from, Vector3 to, float r, float h);
    SupportHit? SupportY(float x, float z, float yHint);
}

public enum ModeSwitchVia { Key, Ui, DoubleSpace }        // 没有触屏输入
public enum ModeSwitchResult { Ok, Relocated, Refused }
public interface IViewControl
{
    ViewMode Mode { get; }
    Task<ModeSwitchResult> SetMode(ViewMode m, ModeSwitchVia via);
}
// IContentModule.Collide、IPartDef.Collision 两个默认方法和 IInteriorProvider.CollisionStorey 见上文，照 SYS-WALK §13
```

```csharp
// 画面设置（照 SYS-GRAPHICS §8 冻结，只增不改）
// 放在 game/contracts/Quality/；Campus.Gen.* 不得引用（§2 的 ArchUnitNET 规则）
namespace Campus.Contracts.Quality;

public enum QualityPreset : byte { Smooth, Balanced, Fine, Ultra, Custom }       // 流畅、均衡、精致、极致、自定义
public enum DeviceTier : byte { IgpuLow, IgpuHigh, DgpuMid, DgpuHigh }           // 核显低、核显高、独显中、独显高（ENG-BUDGET §2）
public enum ApplyClass : byte { Immediate, Amortized, UnderVeil, Restart }       // 即时、即时（分帧）、雾幕、重启（SYS-GRAPHICS §3），由轻到重
public enum KnobKind : byte { Floor, StyleDefault, Performance }                 // 风格底线（有最差值）、风格默认（可关）、性能项（SYS-GRAPHICS §2）

public enum QualityKnob : byte                      // 共 27 项，序号固定（QualityChange.Knobs 按序号置位）；--quality-set 用它的 snake_case
{
    DisplayMode, WindowSize, VSync, FrameCap, Vrr,
    RenderScale, FsrSharpness, AntiAliasing, Anisotropy,
    ShadowQuality, ShadowDistance, SmallCasters,
    GlowQuality, TiltShift, LensFlare, LightShafts,
    DetailScale, MeshLodThreshold,
    GeometricLeaves, FoliageDensity, GrassRadius,
    ReflectionInterval, ParticleDensity,
    Adaptive, GraphicsApi, GpuIndex,
    Vignette,                                    // 暗角（风格默认，SYS-GRAPHICS §2），追加在末尾
}

public sealed record KnobSpec(QualityKnob Knob, string OwnerWp, ApplyClass Apply, KnobKind Kind,
    float Min, float Max, float Step, float WorstAllowed, bool HigherIsBetter,
    bool InAdaptiveLadder, string LabelKey);        // 界面按它自动生成控件；枚举类的项按序号编码成 float

public sealed record QualityProfile(QualityPreset Preset,
    IReadOnlyDictionary<QualityKnob, float> Overrides, bool Adaptive, int Schema = 1);   // Overrides 只存与预设不同的项

public sealed class QualityView                     // 实际生效值的不可变快照：值变时换新实例，读取不分配
{
    public long Version { get; }                    // 每次生效 +1；同一帧里所有系统看到同一版本
    public QualityPreset Preset { get; }            // 界面显示的预设：改过任何单项即 Custom
    public DeviceTier Tier { get; }
    public bool Adaptive { get; }                   // 自动调节是否开
    public int StepRate { get; }                    // 一拍二步频：帧率上限 30 时 15，其余 12（SYS-GRAPHICS §5）
    public float this[QualityKnob k] { get { … } }  // 实际生效值 = clamp(min(玩家所选, 自适应上限), 最差值)（SYS-GRAPHICS §5）
    public float Requested(QualityKnob k) { … }     // 玩家所选（自适应之前）
    public bool Locked(QualityKnob k) { … }         // 玩家手动改过：自动调节跳过它
    public bool Reduced(QualityKnob k) { … }        // 当前被自动调节压低：界面据此写"已自动降低：……"
}

public enum ApplyOrigin : byte
{
    Startup,        // 启动：graphics.cfg > 自动推荐 > 默认值
    CommandLine,    // --quality、--quality-set、--adaptive、--tier：只在内存里生效、不写盘，优先于 graphics.cfg（SYS-GRAPHICS §6）
    User,           // 设置页：写盘；改过的项锁定
    Gate,           // 门禁、基准、画面测试与"预设轮换"脚本：不写盘、不锁定
    Revert,         // 15 s 内没有 ConfirmDisplay()，自动还原显示类设置
    Adaptive,       // 自动调节；只出现在 QualityChange 里，Apply 不接受
}

public readonly record struct QualityChange(long Version, ulong Knobs, ApplyOrigin Origin, bool Veiled)
{
    public bool Has(QualityKnob k) => (Knobs & (1UL << (int)k)) != 0;   // 按 QualityKnob 的序号置位，不分配
}   // Veiled：本次在雾幕全黑时生效；为 false 时负责方自己淡化，不得硬切

public readonly record struct KnobDelta(QualityKnob Knob, float From, float To, ApplyClass Apply);
public sealed record ApplyPlan(
    QualityProfile Next,
    IReadOnlyList<KnobDelta> Deltas,                // 只列有变化的项
    ApplyClass Heaviest,                            // 最重的一类：有 UnderVeil 时界面才亮"应用"，有 Restart 时显示"启"
    bool NeedsConfirm,                              // 含显示模式、窗口尺寸、渲染比例：生效后要在 15 s 内确认（SYS-GRAPHICS §4 第 6 步）
    long VramBytes, long VramBudgetBytes,           // 按 SYS-GRAPHICS §5 的公式估的显存与 DXGI 给的预算；P2 之前为 0
    IReadOnlyList<QualityKnob> Rejected);           // 劣于最差值、超出范围或不合法的项；不为空时 Apply 整体拒绝，什么也不改

public enum ApplyStatus : byte { Applied, Deferred, Rejected }
public sealed record ApplyResult(
    ApplyStatus Status,                             // Deferred：SYS-GRAPHICS §4 第 5 步列出的过渡进行中（跳转、地图传送、模式切换、换季转场、回位、
                                                    // 切图、读档的遮挡期间、进场过场与返回标题），请求排队，
                                                    // 结束后执行并发 Changed；排队期间再 Apply，新请求替换旧请求
    QualityView Effective,
    bool AwaitingConfirm,                           // 显示类改动已生效，等 ConfirmDisplay()
    bool RestartPending,                            // 有重启类改动：已写盘，下次启动生效（SYS-GRAPHICS §4 第 4 步）
    IReadOnlyList<QualityKnob> Rejected);

public interface IQualityService                    // Kernel 服务（WP-32）；界面与门禁的唯一入口
{
    DeviceTier Tier { get; }
    QualityPreset Recommended { get; }
    QualityProfile Requested { get; }
    QualityView Effective { get; }                  // 实际生效值，读取无分配
    IReadOnlyList<KnobSpec> Knobs { get; }          // 单项目录（QualityCatalog）：画面页按它生成控件
    float PresetValue(QualityPreset p, QualityKnob k);   // 预设表 game/data/quality-presets.json 的取值
    event Action<QualityChange>? Changed;           // 主线程，值变才发
    ApplyPlan Plan(QualityProfile next);            // 界面据此显示"雾幕""重启"生效标签与显存估计（UI-VISUAL §8.5）
    Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin);   // origin 为 Adaptive 时抛 ArgumentException
    void ConfirmDisplay();                          // 15 s 确认
}

public interface IQualityAware                      // 标了 [QualityResponsive] 的 [KernelNode] 实现
{
    // WP-32 在 FramePriority.Quality 那一步调用：启动时以全部项调用一次，之后只在 Owns 或 Reads 里的项变化时调用。
    // 即时类按节点 ID 顺序调用；雾幕类在雾幕全黑时按 SYS-GRAPHICS §4 第 3 步的负责方顺序调用；
    // 分帧类（细节距离）由负责方自己分帧完成，每帧不超过 0.3 ms；自动调节的每级降档在 0.5–1 s 内淡化完成（G13）
    void ApplyQuality(QualityView view, in QualityChange change);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class QualityResponsiveAttribute : Attribute   // 标在 Godot 侧包的 [KernelNode] 上
{
    public QualityKnob[] Owns { get; init; } = [];   // 我负责落到引擎上；G5 核对每项恰有一个负责方
    public QualityKnob[] Reads { get; init; } = [];  // 只读，或按它调整自己的对象、但不对这一项的效果负责
}
// ParticleDensity：WP-45 Owns（降水池），WP-38 Reads（飘落池按同一系数降；萤火虫不降，SYS-GRAPHICS §3）
// 着色器要的值（细节系数、植被密度、草半径）由 WP-32 合成全局参数 quality（vec4，ENG-PERF §1.5）
// 任何包都不读 user://graphics.cfg，只经 IQualityService 和 IQualityAware 接收设置（SYS-GRAPHICS §8）
```

**界面、多地图、平面地图、开始页、存档、规则集与观者状态、外围、导航图与音频的契约块**（下面各块都在 `Campus.Contracts` 程序集里，纯 .NET，向量用 `System.Numerics`，只增不改；冻结后枚举只能在末尾追加，JSON 按字符串存，消费方守上文的穷举纪律）：

```csharp
// 界面、雾幕与只读状态（UI-VISUAL §11.11、SYS-TITLE §7.4、SYS-AUDIO §19）；命名空间 Campus.Contracts

// 减少动态效果：WP-35 实现并注册为 Kernel 服务；WP-34、WP-36、WP-26、WP-45、WP-48、WP-49 只读
public interface IMotionSettings
{
    bool ReduceMotion { get; }
    event Action? Changed;
}

// 时间环的天色光谱：WP-37 实现；HudModel 在换季、换天气模式时取一次，不逐帧
public interface IPaletteProbe
{
    void SampleHorizon(Season season, WeatherVec weather, Span<Vector3> srgb24);   // 24 个整点的地平线天色，sRGB 0–1
}

// 雾幕：WP-01 实现（雾幕层由 WP-05 画），转场、跳转、传送、换季、画质、回位、过场、切图共用一层
public enum VeilReason : byte { Boot, Transit, Teleport, Season, Quality, Respawn, Cinematic, MapTravel }   // 在 Campus.Contracts，不在音频命名空间
public enum VeilContent : byte { Fog, Frontispiece }    // 雾色；扉页插画（SYS-TITLE §1.3）
public interface IVeilState { float Opacity { get; } VeilReason Reason { get; } }   // 只读；音频据此压低 World 组（SYS-AUDIO §7.2）
public interface IVeilService : IVeilState             // 草案：签名由 WP-01 在 v1 里补全
{
    Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog);   // 盖满时完成
    void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds);            // 保持不透明直到条件满足（SYS-TITLE §10 的 hold）
    Task Reveal(VeilReason reason, float seconds);
}

// 其他包补的只读状态（SYS-AUDIO §19）
public readonly record struct LightningFlash(double RealSeconds, float AzDeg, float ElDeg, float DistanceKm,
    int Pulses, float Peak, bool Visible);              // Visible = false：减少动态效果时只发事件、不画
public interface ILightningFeed { event Action<LightningFlash>? Flash; }     // WP-40：远雷的节拍（SYS-AUDIO §6.1）
public interface IChannelReader { float Value(ChannelId id); }               // WP-03 / WP-39：灯光通道的当前值
```

```csharp
// 多地图（照 SYS-MAPS §8，WP-46）；命名空间与平面地图的契约分开。MapId、WaypointRef 在基础类型里
namespace Campus.Contracts.Maps;

public enum MapKind : byte { Outdoor, Interior, Instance }       // 现在只有 Outdoor；精选室内属于所在的图（SYS-MAPS §2.1）
public enum MapTimeRule : byte { Shared, Fixed }                 // Fixed 预留，WP-34 现在只实现 Shared（SYS-MAPS §14）
public sealed record GeoFrame(double OriginLon, double OriginLat, int Epsg,
    Vector2 BoundaryMin, Vector2 BoundaryMax);                   // 从 campus.json 的 meta 读，不手写
public sealed record MapRect(Vector2 Min, Vector2 Max);          // 全图数据纹理的 xz 范围，边长为 64 m 的整数倍；清水河 ±1280 m（SYS-MAPS §10）
public sealed record RoamLimits(                                 // 旁观活动范围（SYS-VIEW §2.3）；公式与前沿线见 WLD-OUTSKIRTS §8
    float HeightFactor, float HeightStart, float MaxRadius,      // 0.9、30 m、3000 m：水平范围 = 前沿线 + 0.9 × max(0, h − 30 m)
    float FloorY, float CeilY,                                   // −28 m、2000 m
    float Buffer, float SoftFloorClearance,                      // 30 m 缓冲带；外围上空的软地板 = 屋顶 + 40 m
    float FallbackMargin)                                        // 没有外围数据的图：校界外扩 150 m
{
    public float PushRate { get; init; } = 1.5f;                 // WLD-OUTSKIRTS §12.3：缓冲带里沿法线推回的速率
    public float PushMax { get; init; } = 15f;                   //   推回上限
    public float EdgeFogMax { get; init; } = 1.6f;               //   边缘雾上限
}
public sealed record MapSpawn(string Id, Pose Fly, WaypointStand? Stand);   // 点位以外的出生点
public sealed record MapWorldRules(
    MapTimeRule Time,
    WorldPreset Default,                                         // 清水河：秋 17:30 晴
    LocalizedName DefaultName,                                   // "落金初灯"
    bool QuietUntilDusk,                                         // 默认状态保持晴到本季黄昏带结束（SYS-WEATHER §6.6）
    string WeatherProfile,                                       // game/data/weather/<剧本>/；清水河 "chengdu"
    string SunProfile = "default",                               // 美术日月轨迹（SYS-TIME §2）
    WorldPreset? Fixed = null)                                   // Time == Fixed 时进图钉住的状态（预留）
{
    public WorldPreset? NewGameStart { get; init; }              // SYS-TITLE §12：新游戏过场的起点，清水河秋 17:05 晴；null 时取 Default
}
public sealed record MapEnvironment(string Sky, string Backdrop, IReadOnlyList<FeatureId> ReflectLakes)   // 列表顺序即湖号
{
    public string? Outskirts { get; init; }                      // WLD-OUTSKIRTS §12.3："outskirts.json"；null 表示没有外围
}
public sealed record MapTitle(Pose T0, string CloudLayout);      // SYS-TITLE §12：开始页的 T0 镜头与云卡布局名
public sealed record MapPortal(string Id, Box3 Trigger, MapId Target, string TargetSpawn);              // 预留
public sealed record MapDescriptor(
    MapId Id, LocalizedName Name, MapKind Kind, int Version, uint Seed,
    GeoFrame Geo, Vector2 Center, Box3 Bounds, MapRect DataRect, RoamLimits Roam,
        // Center：校界形心，清水河约 (50, 77)，活动范围"离中心不超过 3 km"按它算；Bounds：箱庭包围盒；DataRect：全图数据纹理 = map_rect，清水河 ±1280 m（SYS-MAPS §3、§10）
    TileKey TileMin, TileKey TileMax,
    string DefaultSpawn, IReadOnlyList<MapSpawn> Spawns,
    MapWorldRules World, MapEnvironment Env,
    IReadOnlyDictionary<string, string> Cinematics,              // 角色 → 过场 ID：enterNew、enterNewShort、enterContinue、leave（SYS-TITLE §12）
    IReadOnlyList<MapPortal> Portals)
{
    public MapTitle? Title { get; init; }                        // 没写时开始页用默认图的 T0
}

// 归属：类上不写 Map 时继承程序集的 MapScope；两者都没有 = 通用模块
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MapScopeAttribute(string map) : Attribute { public string Map { get; } = map; }
// ContentModuleAttribute、InteriorProviderAttribute、KernelNodeAttribute 各增 public string Map { get; set; } = ""（见上文）

public interface IMapContext                 // 纯数据；场景 RID 和物理空间留在 Godot 侧的 kernel/map
{
    MapDescriptor Descriptor { get; }
    IWorldModel World { get; }               // 切图后失效，任何节点都不得跨图持有
    string DataRoot { get; }                 // "res://data/maps/qingshuihe/"
    string BakeRoot { get; }                 // "res://bake/qingshuihe/boot/"（启动集根目录）；本机缓存由 IBakeService 管（§4）
}
public enum MapTravelCause : byte { Boot, NewGame, LoadSave, Ui, Portal, Teleport, Test }
public sealed record MapTravelRequest(MapId Target, string? Spawn = null, Pose? Pose = null,
    ViewMode? Mode = null, MapTravelCause Cause = MapTravelCause.Ui);
public enum MapStage : byte { VeilIn, Snapshot, Unload, LoadData, Mount, Warm, Spawn, VeilOut, Done }
public enum MapTravelResult : byte { Arrived, Rejected, Failed }
public interface IMapTransition
{
    MapId From { get; }
    MapId To { get; }
    MapTravelCause Cause { get; }
    MapStage Stage { get; }
    float Progress { get; }                  // 0–1，只给界面的等待灯用，不画进度条（UI-VISUAL §7.10）
    Task<MapTravelResult> Done { get; }      // 主线程完成；不可取消
}
public interface IMapService                 // Kernel 服务（WP-46）
{
    long Rev { get; }                        // 每次 Spawn 完成 + 1
    MapId Default { get; }                   // index.json 的默认图
    IReadOnlyList<MapDescriptor> Maps { get; }
    MapId Current { get; }
    IMapContext? Context { get; }            // Unload 到 Mount 之间为 null
    IMapTransition? Transition { get; }
    IMapTransition Travel(MapTravelRequest r);   // 跨图切换走雾幕加载；同图内移动交给 ICameraRig.Transit 和 IAtlasService
}
public interface IMapAware                   // [KernelNode] 可选实现；雾幕全黑时按节点 ID 顺序调用
{
    void OnMapUnloading(MapId from);         // 释放本图的实例、纹理内容和缓存；之后不得再读旧的 IWorldModel
    void OnMapLoaded(IMapContext ctx);       // 读新图的数据，重建本图的状态
}
public sealed record MapVisit(MapId Map, Pose LastPose, ViewMode Mode, uint WeatherEpoch, double LeftAtHours);
// 存档只需记：MapId CurrentMap、各图的 MapVisit；段的格式由 SYS-SAVE定（按图的段名是 "map:<id>/…"）
// 其他契约里与多地图有关的部分（见上文）：IWeatherProcess.Enter/Leave；FramePriority.Map；ResetDefault 即"回到本图默认状态"；ShaderGlobals.MapRect
```

```csharp
// 平面地图（照 SYS-ATLAS §11，WP-48c；Campus.Gen.* 不得引用 IAtlasService）
// 自定义标记的 ID 是按存档递增的整数（Guid.NewGuid 被 G4 禁用）
namespace Campus.Contracts.Atlas;

public readonly record struct MarkerId(string Value);
    // 本图内唯一："A03"（锚点）、"P05"（观景点）、"door:<入口 ID>"（可进入的室内）、"route:R1"、
    // "lm:<FeatureId>"（地标）、"fac:<FeatureId>"（设施）、"pin:<十进制整数>"（自定义）
public enum MarkerKind : byte { Anchor, Viewpoint, Interior, Route, Landmark, Facility, Custom, Quest /* RPG 预留 */ }
public enum AnchorForm : byte { Kiosk, Post }                     // 导览亭、导览桩
public enum UnlockState : byte { Hidden, Known, Unlocked }
public enum TravelStyle : byte { Teleport, Fly }                  // 默认 Teleport（雾幕瞬移），Fly 是"飞过去"
public enum TravelRefusal : byte { None, NotTravelable, Locked, Here, Busy, Unavailable }
    // Unavailable：过场、开始页、换图雾幕期间
public enum PinShape : byte { Diamond, Circle, Square, Triangle, Star, Flag }

public sealed record TeleportSpec(Pose Fly, WaypointStand? Stand, EntryPath? Entry = null);   // Stand 为 null：仅旁观

public sealed record AtlasMarker(
    MarkerId Id, MarkerKind Kind, LocalizedName Name,
    Vector2 At,                         // 地图坐标 (x, z)，单位 m
    string Icon,                        // UI-VISUAL图标体系里的名字
    int Priority, byte MinZoomBand,     // 缩放档 0–4（SYS-ATLAS §4.2）
    string? Region,                     // 区域 ID（labels.json）
    TeleportSpec? Teleport = null,      // 只有 Anchor、Viewpoint、Interior、Route 可以非空
    FeatureId? Feature = null,          // 锚点：design:anchor:Axx；地标、设施：实体；室内：楼
    string? WaypointId = null,          // Viewpoint：引用 waypoints.json，不复制位姿
    string? RouteId = null,
    AnchorForm? Form = null);

public sealed record AtlasRegion(string Id, LocalizedName Name, string? Parent);   // 大区的 Parent 为 null
public sealed record AtlasLayer(string Id, LocalizedName Name, float YMin, float YMax);   // 预留：楼层图

public sealed record CustomPin(int Id, Vector2 At, PinShape Shape, string? Note = null);   // Id：本图内递增，不复用；Note 观光期不开放

public interface IUnlockPolicy
{
    string Id { get; }                                            // "all-unlocked"、"proximity"
    UnlockState Initial(AtlasMarker m);
    UnlockState OnVisit(AtlasMarker m, UnlockState now);          // 进入判定圈时调用
}

public interface IAtlasService                                    // Kernel 服务（WP-48c）；实现 IMapAware，切图时换底图和标记
{
    long Rev { get; }
    MapId Map { get; }
    IReadOnlyList<AtlasMarker> Markers { get; }                   // 含运行时由室内索引生成的门口标记与自定义标记
    IReadOnlyList<AtlasRegion> Regions { get; }
    event Action? Changed;                                        // 主线程；换图、标记增删、解锁或追踪变化时发
    AtlasMarker? Find(MarkerId id);
    UnlockState StateOf(MarkerId id);
    string? RegionAt(float x, float z);                           // 8 m 区域栅格；返回地点 ID
    bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why);
    void Prefetch(MarkerId id);                                   // 同一时间只保留一个目的地（UI-NAV §7.5）
    ITransitHandle? Travel(MarkerId id, TravelStyle style);       // 同图只调 ICameraRig.Transit；目标在别的图时才调 IMapService.Travel；拒绝时返回 null
    MarkerId? Tracked { get; }
    void Track(MarkerId? id);                                     // null 取消；同一时间只有一个
    IReadOnlyList<CustomPin> Pins { get; }                        // 当前图
    CustomPin? AddPin(Vector2 at, PinShape shape);                // 满 50 返回 null
    void MovePin(int id, Vector2 at);
    void SetPinShape(int id, PinShape shape);
    void RemovePin(int id);
    string PolicyId { get; }                                      // 取自规则集的 UnlockPolicy（观光为 "all-unlocked"）；"proximity" 留给 RPG；
                                                                  // 要按图覆盖时再给 map.json 加字段
}

// 存档段（SYS-SAVE的 ISaveSection；段 ID 按图展开为 "map:<id>/atlas"）
public sealed record AtlasSave(
    string Policy,                                                // "all-unlocked"
    IReadOnlyList<CustomPin> Pins,
    int NextPinId,                                                // 下一个自定义标记的 ID，只增不减
    string? Tracked,                                              // MarkerId.Value
    IReadOnlyDictionary<string, UnlockState> Unlock,             // 只存与策略初值不同的项；观光期为空
    IReadOnlyList<string> VisitedAnchors);
// [SaveSection("atlas", Owner = "WP-48c", Version = 1, Scope = SaveScope.PerMap, Phase = RestorePhase.Content, Order = 40)]（SYS-SAVE §2）
// 相机契约里与传送有关的部分（TransitPreference、TransitOptions.Prefer、TransitKind.Teleport）在上文的相机块
```

```csharp
// 开始页与过场（照 SYS-TITLE §7.4，WP-49a 实现）
namespace Campus.Contracts.Flow;

public enum AppState : byte { Boot, Title, Entering, Playing, Leaving, Quitting }
public enum EnterKind : byte { NewGame, Continue, LoadSlot }
public enum CinematicMode : byte { Full, Short, Off }            // 设置项"开场过场"（SYS-TITLE §5.3）
public enum EnterPath : byte { Cinematic, ShortCinematic, Veil } // 本次实际走的路径；减少动态效果时为 Veil
public enum EnterResult : byte { Entered, Rejected, Failed }     // Failed：回到 Title 并提示原因
public sealed record EnterRequest(EnterKind Kind, SlotId? Slot = null);   // SlotId 见存档块；NewGame 的 Slot 由开始页选定

public interface IAppFlow                     // Kernel 服务（WP-49a），主线程；本身不逐帧运行，不直接读写任何 user:// 文件
{
    AppState State { get; }                   // Boot → Title → Entering → Playing → Leaving → Title；任何状态都能走到 Quitting
    long Rev { get; }
    event Action<AppState, AppState>? Changed; // (旧, 新)，值变才发
    EnterPath? Path { get; }                   // 只在 Entering 时有值
    bool Skippable { get; }                    // Entering 且还没开始收尾
    Task<EnterResult> Enter(EnterRequest r);   // 只在 Title 接受
    Task ReturnToTitle();                      // 只在 Playing 接受；先保存（SYS-SAVE）
    void Skip();                               // 输入层在按住 Esc 0.8 s 后调用；跳过后的终态与完整播放一致（SYS-TITLE §5）
    Task Quit();                               // 任何状态（SYS-TITLE §7.1）
}

// 过场数据的记录：game/contracts/Cinematic/（命名空间 Campus.Contracts.Cinematic），WP-00 合入；
// CinematicDef、CameraSeg、WorldCue、MaskSeg、CaptionCue、ExtraCue、AudioCue、CinematicEnd 的字段与 SYS-TITLE §8.2 的 JSON 一一对应，
// 数据放在 game/data/maps/<地图>/cinematics/<id>.json，format 只增；遇到不认识的轨由 SYS-TITLE §8.4 的检查报错，不静默忽略。
// 现在没有"角色轨"
// CinematicPlayer 不是契约：它是 WP-49a 在 game/camera/cinematic 里的 [KernelNode]，ProcessPriority = FramePriority.Camera（−1100），
// 只在 Title、Entering、Leaving 状态里做事；镜头只经 ICameraRig.SetPose，世界只经 ISaveService 和 IWorldControl（FlowTo、SetFlow），
// 黑边、字幕、标题卡经 WP-35d 的放映层接口，雾幕经 IVeilService，音频经 IAudioService.Cine（SYS-TITLE §8.3）
```

```csharp
// 存档与持久化（照 SYS-SAVE §10，WP-50；Campus.Gen.* 不得引用）
namespace Campus.Contracts.Save;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

public readonly record struct SlotId(string Value);   // "s1"–"s3"：三个存档位；"m1"–"m9" 预留给手动槽。文件夹名就是 Value
// MapId、MapVisit 由多地图契约定义，存档只引用。现在只有 new MapId("qingshuihe")

public enum SlotKind : byte { Auto, Manual }           // Manual 预留
public enum SaveScope : byte { Global, PerMap }        // PerMap 的段在文件里的键是 "map:<MapId>/<段 ID>"
public enum SaveReason : byte
{
    Quit,          // 关窗口、Alt+F4、菜单"退出"：AutoAcceptQuit = false，后台写完再 Quit()
    ToTitle,       // 返回开始页（SYS-TITLE）
    Interval,      // 漫游态累计 5 分钟
    FocusLost,     // 窗口失焦
    Settled,       // 关键动作落定：跳转或传送到达、换季落定、Apply 落定、回到落金初灯落定
    MapChanged,    // 换图到达（SYS-MAPS）
    NewGame,       // 新游戏交还控制：写这条世界线的第一代
    Manual,        // 预留：手动槽
    Checkpoint,    // 预留：RPG 的剧情节点
    Test,          // 门禁（--save-at）
}
public enum SlotHealth : byte { Empty, Ok, RecoveredOlderGen, Corrupt, NewerBuild }

public sealed record SaveMeta(
    int Format,                     // 正文结构版本，现为 1（SYS-SAVE §3.3）
    string Build,                   // 构建号，只用于显示和日志；兼容性只看版本号
    SlotId Slot, SlotKind Kind,
    ulong Gen,                      // 代号：存档位内严格递增；与头和文件名 g<gen>.sav 一致
    ulong Line,                     // 世界线号 = 这条世界线第一代的代号；回退只在同一条线内（SYS-SAVE §1）
    double PlaySeconds,             // 游玩时长：只在漫游态累加
    MapId Map, double WorldHours, double Clock, Season Season, WeatherKind Weather, ViewMode Mode,
    Pose Pose,                      // 镜头位姿，与 view 段相同；"继续"过场按它定终点（SYS-SAVE §5.1，SYS-TITLE §4.1）
    NavSpace? Space,                // 所在空间（楼与楼层，WLD-INTERIORS §15）；室外为 null
    string? Region,                 // 区域 ID（SYS-ATLAS区域栅格）
    string? PlaceKey,               // 地点名的翻译键，存档卡摘要与继续过场的地点字幕用；查不到时退回图名（SYS-SAVE §3.2）
    long SavedAtUnix,               // 系统时钟，只用于显示（SYS-SAVE §6，G4 例外）
    SaveReason Reason,
    string Ruleset = "sightseeing", // RulesetId.Value（规则集块）；预留 "rpg"
    SlotId? Origin = null,          // 手动槽预留：读它之后自动保存写回这个存档位
    bool HasThumb = false);
public sealed record SlotInfo(SlotId Slot, SlotKind Kind, SlotHealth Health, SaveMeta? Meta, int Generations);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SaveSectionAttribute(string id) : Attribute
{   // 标在实现 ISaveSection<T> 的 [KernelService] 或 [KernelNode] 类上；注册器从 Kernel 取这个实例
    public string Id { get; } = id;               // 小写字母、数字、连字符，如 "world"；"map:<id>/" 前缀由 Scope 决定，不写进 Id
    public string Owner { get; set; } = "";       // "WP-34"；G5 核对与 ownership.json 一致
    public int Version { get; set; } = 1;         // 本段当前的 v；字段有任何增减都要加 1（SYS-SAVE §3.6）
    public SaveScope Scope { get; set; } = SaveScope.Global;
    public RestorePhase Phase { get; set; } = RestorePhase.Content;   // 恢复阶段（SYS-SAVE §2）；G5 查 Phase 与 SYS-SAVE §2 的表一致
    public int Order { get; set; }                // 阶段内的恢复顺序，小的先（SYS-SAVE §2）；G5 查不重复
}
public enum RestorePhase : byte
{
    World,     // 换图之前：maps、world、weather、map:<id>/weather；这一阶段结束时 Instance + 1
    Content,   // 换图之后：map:<id>/atlas、progress 等
    Pose,      // 落位：view、walk。Preview 读档时只解析、留作终点，CompleteResume 时恢复
}
public enum LoadScope : byte { Full, Preview }      // 读档范围：Preview 给开始页用，不恢复 Pose 阶段（SYS-SAVE §5.1，SYS-TITLE §1.1 第 6 步）

public readonly record struct CaptureContext(SaveReason Reason, MapId Map, bool Forced);
    // Forced：此刻不是安全时刻（退出，或顺延满 60 s），各段必须给出落定值（SYS-SAVE §4.2）
public sealed class RestoreContext
{
    public required MapId Map { get; init; }
    public required int FromVersion { get; init; }   // 迁移前的版本，只用于日志；迁移已在后台做完
    public required bool Missing { get; init; }      // 文件里没有这一段，传进来的是 CreateDefault 的值
    public void Repair(string what) { … }            // 记一条校验修复（SYS-SAVE §5.6）
    // 修复要用的只读查询（安全点、活动范围、点位与标记是否存在）由 WP-50 在 v1 里补全
}
public sealed record NewGameContext(uint Seed, MapId Map, WorldPreset Start);
    // Start 默认取本图 map.json 的 newGameStart（MapWorldRules.NewGameStart；清水河秋 17:05、晴，SYS-MAPS §3），没有时取本图默认状态；
    // 之后由 IWorldControl.FlowTo 正向推到落金初灯（SYS-SAVE §5.5、SYS-TITLE §6）

public interface ISaveSection                         // 注册器只认这个非泛型接口；各包实现下面的泛型版
{
    JsonTypeInfo TypeInfo { get; }
    bool IsSettled => true;                           // 本段此刻是否落定；有一段为 false，就顺延（SYS-SAVE §4.2）
    object Capture(in CaptureContext c);
    void Restore(object dto, RestoreContext c);
    object CreateDefault(NewGameContext c);
    JsonNode Migrate(int fromVersion, JsonNode data) => throw new NotSupportedException();
        // 从 fromVersion 迁到 fromVersion + 1；纯函数，在后台线程调用，不碰 Godot 和服务状态
}
public interface ISaveSection<T> : ISaveSection where T : class   // T：sealed record，只读，不含 Godot 类型（SYS-SAVE §3.5）
{
    new JsonTypeInfo<T> TypeInfo { get; }             // 来自本包的源生成 JsonSerializerContext
    new T Capture(in CaptureContext c);               // 主线程，FramePriority.Save 那一步；落定快照；全部段合计 p99 ≤ 0.3 ms
    void Restore(T dto, RestoreContext c);            // 主线程，在遮挡下按 Phase、Order 调用；立即生效，不做过渡
    new T CreateDefault(NewGameContext c);            // 新游戏，或文件里缺这一段
    JsonTypeInfo ISaveSection.TypeInfo => TypeInfo;
    object ISaveSection.Capture(in CaptureContext c) => Capture(in c);
    void ISaveSection.Restore(object dto, RestoreContext c) => Restore((T)dto, c);
    object ISaveSection.CreateDefault(NewGameContext c) => CreateDefault(c);
}

public enum SaveStatus : byte { Saved, Deferred, Disabled, Failed }
public sealed record SaveResult(SaveStatus Status, SlotId? Slot, ulong Gen, string? Error = null);
public enum LoadStatus : byte { Loaded, Recovered, Empty, Corrupt, NewerBuild, Failed }
public sealed record LoadResult(LoadStatus Status, SlotId? Slot, ulong Gen, IReadOnlyList<string> Repairs, string? Error = null);

public interface ISaveService                         // Kernel 服务（WP-50）；不加 Autoload
{
    bool Enabled { get; }                             // --save=off，或 .lock 被别的进程占着时为 false：不写、不挪任何文件
    IReadOnlyList<SlotInfo> Slots { get; }            // 扉页后读好；s1–s3 恒为三项
    SlotId? Continue { get; }                         // 最近保存的可读存档位（SYS-SAVE §1）；没有就是 null
    SlotId? Active { get; }                           // 当前存档位：自动保存写到这里
    bool Busy { get; }                                // 后台写盘进行中
    event Action? SlotsChanged;                       // 在主线程发出
    void Request(SaveReason r);                       // 合并、限频（两次之间至少 30 s）、等安全时刻（SYS-SAVE §4.1）；只在漫游态有效；Quit、ToTitle 不走这里
    Task<SaveResult> SaveNow(SaveReason r);           // 不等、不限频：新游戏交还控制、门禁、预留的手动槽
    Task<LoadResult> Load(SlotId s, LoadScope scope = LoadScope.Full);
        // 后台读取、校验、迁移，主线程按阶段恢复（SYS-SAVE §5.1）；Full 在恢复位姿处的格与碰撞就绪后才完成，Preview 在 Content 阶段恢复完就完成
    bool HasPendingPose { get; }                      // Preview 读档后为 true，Pose 阶段的段已解析、还没恢复
    Task CompleteResume();                            // 继续过场交还控制那一帧调用，在 FramePriority.Save 恢复 view、walk；Instance 不变
    Task<LoadResult> LoadFile(string path);           // --load：一律 Full；源文件只读；之后写到 --save 指定的存档位，没指定就不写
    Task<LoadResult> NewGame(SlotId s, uint? seed = null, WorldPreset? start = null);
        // seed 为 null 时取一次熵（G4 唯一的熵源例外）；start 为 null 时取本图的 newGameStart（SYS-SAVE §5.5）
    Task FlushForQuit(SaveReason r, TimeSpan timeout);   // r 只能是 Quit 或 ToTitle；超时就放弃这一代，旧代不受影响
    byte[]? Thumbnail(SlotId s);                      // 最新可读一代的 WebP 字节，界面自己解码（SYS-SAVE §3.4）
}
// 文件结构：魔数 → 64 字节的头 → 未压缩的 meta JSON → Brotli 压缩的分段 JSON → WebP 缩略图，外加 SHA-256（SYS-SAVE §3.1）。
// 序列化只用 System.Text.Json 源生成；各包的 DTO 用自己的 JsonSerializerContext，约定与 ContractJson.Options 一致，
// ID 类的 record struct 写成字符串（SYS-SAVE §3.5）。其他契约里与存档有关的部分（IWorldState.Instance、FramePriority.Save、
// WeatherSeed 与 WeatherBoot.Seed 的含义、IWeatherProcess.Capture/Restore）见上文
// 规则集的先后（SYS-SAVE §5.1、SYS-SAVE §5.5）：新游戏先定规则集，再调各段的 CreateDefault；
// 读档先按 meta 的 Ruleset 定规则集，再进 World 阶段
```

```csharp
// 规则集与观者状态（ENG-ROADMAP §6 第 1、2 条；字段由 WP-00 在 v1 里定稿）；命名空间 Campus.Contracts
// 规则集：决定"这一局按什么规则运行"的一组配置。观光是 sightseeing，数据 game/data/rulesets/<id>.json，schema 在 game/contracts/schemas/。
// 各包读规则集，不在代码里写观光专属的常量（解锁策略名、事件 ID、允许的观察模式）
public readonly record struct RulesetId(string Value);          // "sightseeing"；预留 "rpg"；与 SaveMeta.Ruleset 同值
[Flags] public enum WorldControlMask : byte { None = 0, Clock = 1, Season = 2, Weather = 4, Flow = 8, All = 15 }
public enum InteractionRule : byte { None }                      // 观光为 None；只在末尾追加
public sealed record RulesetDescriptor(
    RulesetId Id,
    IReadOnlyList<ViewMode> ViewModes,         // 观光：Fly、Walk；IViewControl 拒绝不在表里的模式（WP-06、WP-42）
    WorldControlMask WorldControls,            // 观光：All（时间、季节、天气、流逝全开）；界面按它画可用或禁用态（WP-35）
    string UnlockPolicy,                       // 观光："all-unlocked"；IAtlasService.PolicyId 取它（WP-48c）
    IReadOnlyList<string> Incidents,           // 允许的偶发事件 ID（incidents.json）；IncidentDirector 只跑这里登记的（WP-26）
    IReadOnlyList<AppState> CinematicStates)   // 允许播过场的状态，观光：Title、Entering、Leaving（WP-49a）
{
    // 内容规则：运行时不读，门禁按它取判据（WP-27；ENG-GATES §3、SYS-AUDIO §22 的 G21h）
    public bool Characters { get; init; }              // 观光 false：不出现人物（INV-6）
    public InteractionRule Interaction { get; init; }  // 观光 None：没有可以点选的东西（UI-NAV §4.4）
    public bool ClearVoices { get; init; }             // 观光 false：没有听得清的人声（SYS-AUDIO §8）
}
public interface IRuleset                      // Kernel 服务（WP-00，game/kernel/registry/），只读
{
    RulesetDescriptor Current { get; }
    long Rev { get; }                          // 只在新游戏和读档时变；切换顺序见存档块末尾
}

// 观者状态："观者在哪"读它，"画面从哪看"（视锥、听者、流式选级）仍读 ICameraRig
public interface IObserverState                // Kernel 服务；WP-06 实现，行走时取 WP-42 物理体的数据
{
    Vector3 Feet { get; }                      // 脚位；旁观时取镜头位置
    Vector3 Eye { get; }                       // 眼位；旁观时就是镜头位置
    float YawDeg { get; }                      // 朝向，与 Pose.YawDeg 同一约定
    NavSpace? Space { get; }                   // 所在空间（楼与楼层，导航图块）；室外为 null
    ViewMode Mode { get; }
}
// 读 IObserverState 的：小地图的玩家箭头与底栏区域名（WP-48b）、Proximity 判定（WP-48c）、行走进楼触发与门（WP-07）、
// 存档 meta 的空间与区域（WP-50）
```

```csharp
// 外围（照 WLD-OUTSKIRTS §12.3，WP-47 生成、WP-08 解析；命名空间 Campus.Contracts）
public enum OutskirtsBand : byte { O0, O1, O2 }        // O0 街道带、O1 外围街区、O2 雾中楼顶；O3 是现有的远景卡片和雪山（WP-22）
public enum OutskirtsKind : byte { SlabResidential, PointTower, Factory, ParkOffice, Mall, School, Landmark }
public sealed record OutskirtsBlock(string Id, OutskirtsKind Kind, OutskirtsBand Band,
    IReadOnlyList<Vector2> Footprint, float Height, int Floors,
    byte FacadeLevel, byte RoofLevel, uint Seed);           // 级别由管线按最近可达距离定（WLD-OUTSKIRTS §4.1）

public interface IOutskirtsModel                             // WP-08 解析 outskirts.json 与外围覆盖层，只读
{
    IReadOnlyList<Vector2> FrontLine { get; }               // 前沿线，闭合
    IReadOnlyList<Vector2> CutLine { get; }                 // 截面线，闭合，2 m 等距；WP-22 与 WP-47 共用这些点
    IReadOnlyList<OutskirtsBlock> Blocks { get; }
    float FrontDistance(float x, float z);                  // 到前沿线的距离，线内为 0（16 m 网格，双线性）
    float SoftFloor(float x, float z);                      // 屋顶净空 + 40 m（8 m 网格，外扩一格取最大）
    // 道路横断面、轨道与站台、水系、用地、树点、灯位、地标、水柱交点：字段由 WP-47 定，经主代理并入
}
// 与外围有关的 IWorldModel.Outskirts、RoamLimits 与 MapEnvironment 的字段、SurfaceTag.Boundary 见上文
```

```csharp
// 导航图（照 WLD-INTERIORS §13、§15，WP-44 烘焙，随启动集发；室内部分由开放提供者的 Nav() 给出；命名空间 Campus.Contracts）
public readonly record struct NavNodeId(uint Value);
public readonly record struct NavSpace(FeatureId? Building, short Floor);   // 室外 Building 为 null；存档记观者所在的空间（SYS-SAVE）
public enum NavEdgeKind : byte { Road, Sidewalk, Footway, Path, Plaza, Steps, Ramp, Bridge, Boardwalk, Wade,
                                 Corridor, Room, Stair, Elevator, Connector, Door, Gate, Jump, DesignPath }
[Flags] public enum NavFlags : ushort { None = 0, Main = 1, Covered = 2, Lit = 4, Indoor = 8, OneWay = 16, Locked = 32,
                                        Accessible = 64, Scenic = 128, NpcOnly = 256, PlayerOnly = 512 }
public enum DoorKind : byte { Swing, Sliding, Curtain, Turnstile, Gate, OpenArch }
public enum DoorState : byte { Open, AutoOpen, Locked }      // AutoOpen：会动的门（DoorLeafDecl）
public sealed record NavNode(NavNodeId Id, Vector3 P, NavSpace Space, string? Tag = null);
public sealed record NavEdge(NavNodeId A, NavNodeId B, NavEdgeKind Kind, float Length, float Width,
                             float Rise, NavFlags Flags, string Source);
public sealed record NavPortal(string Id, FeatureId Building, NavNodeId Outside, NavNodeId Inside,
                               Vector3 DoorCenter, float YawDeg, float ClearWidth, DoorKind Kind, DoorState State);
public sealed record NavFloorGraph(FeatureId Building, short Floor, IReadOnlyList<NavNode> Nodes, IReadOnlyList<NavEdge> Edges)
{ public static NavFloorGraph Empty(FeatureId b, int f) => new(b, (short)f, [], []); }
public sealed record NavQuery(NavFlags Avoid = NavFlags.Locked | NavFlags.NpcOnly, NavFlags Prefer = NavFlags.None,
                              bool AllowWade = false, bool AllowElevator = true, bool AllowJump = false);
public sealed record NavPath(IReadOnlyList<NavNodeId> Nodes, float Length, float Seconds);
public interface INavGraph                  // 小地图的路线预览（SYS-ATLAS）、G19、以后的 RPG；不用 NavigationServer3D 和 AStar3D 作主图
{
    NavNode Node(NavNodeId id);
    IReadOnlyList<NavEdge> Edges(NavNodeId id);
    NavNodeId? Nearest(Vector3 p, NavSpace? space = null, float maxDist = 20f);
    NavPath? FindPath(Vector3 from, Vector3 to, NavQuery q);         // A*，可在线程池里跑
    IReadOnlyList<NavPortal> PortalsNear(Vector3 p, float r);
}
```

```csharp
// 音频（照 SYS-AUDIO §19，WP-53；放在 game/contracts/Audio/）
// VeilReason、IVeilState、IMediumState、ILightningFeed、IChannelReader 在上文各自的块里；
// 碰撞记录的 Foot、Stair、FootMatHalfMeter、Patches 在碰撞块里
namespace Campus.Contracts.Audio;
using System.Numerics;

// ===== 名字常量：G5 比对代码建出的总线树（同 ShaderGlobals 的做法） =====
public static class AudioBuses
{
    public const string Master = "Master", Music = "Music", Ui = "UI", World = "World", Out = "Out",
        AmbBed = "Amb.Bed", AmbWeather = "Amb.Weather", AmbFar = "Amb.Far", AmbTrace = "Amb.Trace", AmbEmit = "Amb.Emit",
        OutEcho = "Out.Echo", InBed = "In.Bed", InEmit = "In.Emit",
        NearDry = "Near.Dry", SlotAEr = "Slot.A.ER", SlotARev = "Slot.A.Rev", SlotBEr = "Slot.B.ER", SlotBRev = "Slot.B.Rev",
        Air = "Air";
    public const string ZonePrefix = "Zone.";       // Zone.<AcousticClass>.Rev，≤ 6 条（SYS-AUDIO §10.6）
}
public enum AudioGroup : byte { Master, Ambience, Foley, Ui, Music }      // 设置里的五组音量：总、环境、动作、界面、音乐

// ===== 设置：user://audio.cfg，只有 53a 读写，读取只收基本类型 =====
public enum BackgroundBehavior : byte { Keep, Lower, Mute }
public enum SuddenSoundMode : byte { Normal, Softer, Off }                 // 雷、钟、孔雀
public sealed record AudioSettings(
    float Master = 0.8f, float Ambience = 1.0f, float Foley = 0.8f, float Ui = 0.6f, float Music = 0.7f,   // 滑杆值 0–1；脚步默认安静
    bool UiSounds = true, bool HoverSounds = false, bool QuietMode = false, bool Mono = false,
    SuddenSoundMode Sudden = SuddenSoundMode.Normal, BackgroundBehavior Background = BackgroundBehavior.Keep,
    string? OutputDevice = null, int Schema = 1);

// ===== 界面音与音乐 =====
public enum UiCue : byte { Focus, Hover, Confirm, Back, Toggle, PageOpen, PageClose, SliderTick,
    TeleportConfirm, TeleportArrive, Saved, ShiftDayNight, Denied }
public enum MusicCue : byte { Silence, Title, EnterNew, EnterContinue, ReturnTitle }   // 开场音乐做好之前全部按 Silence

// ===== 音频服务：总入口（53a，Kernel 服务） =====
public enum DuckReason : byte { Boot, Veil, PageOpen, Cinematic, MapTravel }
public readonly record struct AudioSnapshot(long Frame, int RealVoices, int Nodes3D, int VirtualEmitters,
    int OneShotsLast5s, float MasterPeakDb, int ResidentKb);
public interface IAudioService
{
    AudioSettings Settings { get; }
    event Action<AudioSettings>? SettingsChanged;            // 主线程；值变了才发
    void ApplySettings(AudioSettings next);                  // 音量 80 ms 斜坡；0.5 s 去抖写盘
    void PlayUi(UiCue cue);                                  // 当场 PlayStream；界面音效关时静默丢弃；80 ms 去重
    void SetMusic(MusicCue cue, float fadeSeconds = 1.5f);
    void Cine(string cue, float seconds, float db = 0f);     // 过场的 audio 轨：title.start / title.fadeIn / title.fadeOut / duck / amb.altitude
    void Duck(DuckReason reason, float db, float attackSeconds, float releaseSeconds);   // 多个原因取最深
    void ReleaseDuck(DuckReason reason);
    AudioSnapshot Snapshot { get; }                          // 调试面板与门禁
}

// ===== 环境声（53b） =====
public readonly record struct SoundSetId(string Value);          // 如 "amb.bird.dusk.roost"，登记在 game/data/audio/sets/*.json
public readonly record struct AmbienceProfileId(string Value);   // 室内档案，如 "interior.library.atrium"
public enum EmitterShape : byte { Point, Line, Area, Scatter }
[Flags] public enum DayBand : ushort { PreDawn = 1, Dawn = 2, Morning = 4, Midday = 8, Afternoon = 16,
                                       Dusk = 32, Evening = 64, Night = 128, LateNight = 256, All = 511 }
[Flags] public enum WeatherMask : ushort { Clear = 1, FewClouds = 2, Cloudy = 4, Overcast = 8, Fog = 16, LightRain = 32,
                                           Rain = 64, Thunderstorm = 128, AfterRain = 256, Snow = 512, AfterSnow = 1024, All = 2047 }
public enum TraceKind : byte { None, HumanTrace, VisibleAnimal }  // 要过画外规则或动物同步规则（SYS-AUDIO §8、§4.4）

public sealed record AmbientEmitterDecl(
    string Id,                            // 以 "amb:" 开头，本图内唯一；G5 查重
    EmitterShape Shape, SoundSetId Set,
    IReadOnlyList<Vector3> Points,        // Point：1 个；Line：折线；Area：外环；Scatter：候选位点
    float RefDb, float AudibleRadius,
    SeasonMask Seasons = SeasonMask.All, DayBand Bands = DayBand.All, WeatherMask Weather = WeatherMask.All,
    TraceKind Trace = TraceKind.None, byte Priority = 4,
    float MinDistance = 0f,               // 远景声的距离下限（SYS-AUDIO §4.6）
    FeatureId? Feature = null,
    ChannelId? Activity = null,           // 人迹跟随的灯光通道（灯亮才有人迹）
    string Reason = "");                  // 手工点必须写理由

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AmbientEmitterAttribute(string id) : Attribute { public string Id { get; } = id; public string Owner { get; set; } = ""; }
public interface IAmbientEmitterSource { IEnumerable<AmbientEmitterDecl> Emit(BuildContext ctx); }   // gen 里，纯函数，在烘焙器里跑

// 部件自带的声音插座：IPartDef.Sounds() 默认方法见上文
public sealed record SoundSocketDecl(string Id, SoundSetId Set, Matrix4x4 Local, float RefDb, float AudibleRadius,
    SeasonMask Seasons = SeasonMask.All, NightRule Night = NightRule.Any, WeatherMask Weather = WeatherMask.All, byte Priority = 2);

public readonly record struct AmbienceCue(SoundSetId Set, Vector3 Position, float DelaySec = 0f, float GainDb = 0f,
    TraceKind Trace = TraceKind.None, byte Priority = 1, uint Group = 0);   // Group：同一批（如一次偶发事件），可以整批取消
public readonly record struct AmbienceSnapshot(long Frame, int ActiveVoices, int OneShotsLast5s, float BedWeightSum,
    float Birds, float Insects, float Frogs, float HumanTrace, float Rain, float Wind, float Water, float Far);
public interface IAmbienceService
{
    AmbienceSnapshot Now { get; }
    void Cue(in AmbienceCue cue);                          // 与画面同步的一次性声：偶发事件本身无声，它引起的抖落、积雨、旗动有声（SYS-AUDIO §6.4）
    void CancelCues(uint group);                           // 偶发事件被转场打断时
    int AddDynamic(in AmbientEmitterDecl decl);            // 会动的发声器（动物），返回句柄
    void MoveDynamic(int handle, Vector3 position);
    void RemoveDynamic(int handle);
}

// ===== 空间声学（53c）：声学网格 + 房间与门户 + 射线细化 + 混响区，不上 Steam Audio =====
public enum AcousticClass : byte
{
    Open, Waterside, Street, Courtyard, Canopy, Underpass, Arcade,                      // 室外与半室外
    Corridor, Stair, SmallRoom, Room, Hall, Atrium, Gym, Pool, Canteen, Auditorium, Exhibit,  // 室内
    Underwater, Underground, FogSea,                                                    // 介质（SYS-VIEW §5）
}
public enum AbsorbClass : byte { Hard, Mixed, Soft, Foliage, Water, Snow }
public enum PathKind : byte { Direct, Obstructed, Occluded, Portal, Transmission, None }
public enum AcousticPortalKind : byte { OpenDoor, SlidingGlass, Curtain, Window, Archway, ElevatorDoor, SwingDoor }
    // 与室内的 PortalKind（WLD-INTERIORS）区分，不同名；SwingDoor 给会动的门
public readonly record struct AcousticEnv(                // 值类型，查询不分配
    AcousticClass Class, float Indoor, float SkyOpen, float Enclosure,
    float WallDistM, float CeilingM, float MeanFreePathM, float VolumeM3,
    float Absorption, float HfRatio, float Rt60S, float ErDelayMs, float ErLevelDb, float FlutterMs,
    float NoiseFloorDb, ushort RoomId, FeatureId? Building);
public readonly record struct SendTap(int BusIndex, float GainDb);
public readonly record struct ListenerRouting(int DryBus, SendTap ErA, SendTap RevA, SendTap ErB, SendTap RevB,
    float Enclosure, long Rev);                            // 不交叉时 B 两路为 −80 dB；BusIndex 由 53a 启动时建表，不在发声时查字符串
public readonly record struct OcclusionResult(float DirectDb, float CutoffHz, float FilterDb, PathKind Path, Vector3 ApparentPos);
public readonly record struct AcousticPortal(ushort Id, ushort RoomA, ushort RoomB, Vector3 Center, Vector3 Normal,
    float Width, float Height, float Depth, AcousticPortalKind Kind, float Openness);
public interface IAcousticEnv                              // Kernel 服务，主线程，FramePriority.Acoustics 那一步
{
    AcousticEnv At(Vector3 p);                             // 网格 + 房间 + 覆盖层 + 介质 + 天气修正；不含射线细化
    AcousticEnv Listener { get; }                          // 本帧听者处：At() + 射线细化 + 平滑；每帧一次
    ListenerRouting Routing { get; }                       // 观者声每次发声取一份
    long Rev { get; }                                      // Listener 或 Routing 变化时 +1
    OcclusionResult Occlusion(Vector3 source, ushort sourceRoom);   // 相对当前听者
    ushort RoomAt(Vector3 p);                              // 0 = 室外
    AmbienceProfileId? ProfileOf(ushort room);
    ReadOnlySpan<AcousticPortal> PortalsOf(ushort room);
}
public sealed record AcousticRoomDecl(string LocalId, AcousticClass Class, IReadOnlyList<Box3> Volumes,
    AbsorbClass Surfaces, AmbienceProfileId? Profile = null, float? Rt60 = null, float? SendDb = null);
public sealed record AcousticPortalDecl(string RoomA, string? RoomB /* null = 室外 */, Vector3 Center, Vector3 Normal,
    float Width, float Height, float Depth, AcousticPortalKind Kind, ChannelId? Openness = null /* 电梯门的开度通道 */,
    string? Door = null /* 自动门的门 ID（DoorLeafDecl.Id），开度读 IInteriorDoors.OpenOf，关好时门户馈送降到 −80 dB（SYS-AUDIO §10.4） */);
public sealed record AcousticsDecl(IReadOnlyList<AcousticRoomDecl> Rooms, IReadOnlyList<AcousticPortalDecl> Portals)
{ public static readonly AcousticsDecl Empty = new([], []); }
// IInteriorProvider.Acoustics(ctx, id) 默认方法见上文；按层提供的室内照样实现：房间按层声明，Box3 带 y 区间

// ===== 观者声（53d）与它的输入 =====
public enum FootMat : byte
{
    Auto = 0, Asphalt, Concrete, StonePaving, BrickPaver, RubberTrack, Grass, Soil, Gravel,
    WoodDeck, MetalGrate, SteelPlate, MetalCover, RubberMat, Terrazzo, PolishedStone, Tile,
    Carpet, WoodFloor, Epoxy, SportWood, PoolDeck, Glass, Metal,
    Hedge, WoodDoor, Shutter,                              // 只作接触组
    Bark,                                                  // 树干的接触组（SYS-AUDIO §9.3），追加在末尾
}
public readonly record struct StairSpec(float RiserM, float TreadM, int Count);
public readonly record struct FootPatch(Vector2 Center, Vector2 HalfSize, float YawDeg, FootMat Mat, uint Flags);  // 井盖、箅子、排水沟

public readonly record struct GroundCover(float Wet, float Puddle, float Snow, float SnowFresh,
    float Leaves, float LeafWet, float Petals, float Shelter);
public interface IGroundCover { GroundCover Sample(float x, float z); }    // WP-38；与着色器同一套阈值函数，G2 比对两边

public enum FootSide : byte { Left, Right, Both }
public readonly record struct FootfallInfo(ulong Tick, FootSide Side, Vector3 Pos, float SpeedH, Vector3 Normal,
    ulong ColliderId, int ShapeIndex, bool OnStair, float Slope);
public readonly record struct ContactInfo(ulong Tick, ulong ColliderId, int ShapeIndex, Vector3 Pos, Vector3 Normal,
    float NormalSpeed, float TangentSpeed, bool Pushing, SurfaceTag Tag);
public interface IWalkTelemetry                            // WP-42；全部在 _PhysicsProcess 里发出
{
    event Action<FootfallInfo> Footfall;
    event Action<ulong, float> Jumped;                     // tick, 起跳速度
    event Action<ulong, float, Vector3> Landed;            // tick, 撞击速度, 位置（SYS-WALK §12 第 9 条的扩展）
    event Action<ulong, float> StepUp;                     // 高差
    event Action<ulong, float> StepDown;
    event Action<ulong, float> Skid;                       // Δv
    event Action<ContactInfo> WallContact;                 // 每个物理步里有墙接触就发
    event Action<ulong, float> WaterEnter;                 // 水深
    event Action<ulong> WaterExit;
    event Action<ulong> Sink;
    event Action<ulong, float> TurnInPlace;                // 角度
    event Action<ulong> ModeTouchdown;
    event Action<ulong> ModeTakeoff;
    event Action<ulong> Teleport;
    event Action<ulong, float> ElevatorDoor;               // 主楼电梯门的开度 0–1
    float StepPhase { get; }                               // 0–1，晃动和脚步共用
}

public enum FoleyKind : byte
{
    Footfall, Scuff, Jump, Land, StepUp, StepDown, Skid, Bump, Brush,
    WadeStep, WadeEnter, WadeExit, Sink, LeafKick,
    Curtain, LockedDoor, Gate, Railing, Hedge, BoundaryTouch,
    ElevatorDoorClose, ElevatorDoorOpen, ElevatorRun,
    Takeoff, Touchdown,
    DoorOpen, DoorClose, AutoDoorOpen, AutoDoorClose,      // DoorOpen、DoorClose 给会动的门（WLD-INTERIORS §4.1，IInteriorDoors.Changed：
                                                           // 进入 Opening 时 DoorOpen，进入 Closed 时 DoorClose，SYS-AUDIO §9.8）；Auto* 预留
}
public readonly record struct GroundSample(FootMat Mat, FootMat Mat2, float Mat2Weight, bool Stair,
    GroundCover Cover, float WaterDepth, uint LocationHash);
public readonly record struct FoleyEvent(
    FoleyKind Kind,
    Vector3 Pos,                 // 世界坐标；观者自身的事件取落脚点
    float Intensity,             // 0–1：速度、撞击速度、Δv 等归一后的力度
    GroundSample Ground,         // 地面事件用；交互事件可以为 default
    FootSide Side = FootSide.Both,
    FootMat Contact = FootMat.Auto,   // 碰撞类事件：碰到的东西的材质
    uint SourceId = 0,           // 交互对象的稳定 ID（冷却、复用）
    ulong Tick = 0);             // 物理步序号（事件日志与确定性）
public enum FoleyLoop : byte { WadeDrag, WallBrush, HedgeBrush, ElevatorCab, FlyAir }   // FlyAir：飞行风声，实时合成
public readonly record struct FoleyStats(int ActivePlayers, int Voices, int Dropped, long Events);
public interface IFoleyService                             // Kernel 服务（53d）
{
    void Play(in FoleyEvent e);                                  // 主线程，物理步或帧里都可以
    void SetLoop(FoleyLoop loop, float level01, float param01);  // 每帧调；level 为 0 时自动淡出停播
    FoleyStats Stats { get; }
    IReadOnlyList<FoleyEvent> RecentLog { get; }                 // 只在门禁构建里记录
}
// 闸机和门不出电子提示音，单车不响铃；3D 声源在播上限 32（核显档默认 24），不用 AudioStreamRandomizer，用自己的洗牌选择器（SYS-AUDIO）
```

```csharp
// 本机烘焙（WP-02；命名空间 Campus.Contracts.Bake；D-165，§4）：几何类烘焙产物在玩家机器上由同一个 Build() 生成并写进本机缓存
public readonly record struct BakeKey(MapId Map, string Module, string Detail);   // Detail 是 "级别/tileId" 或 "featureId/storey/detail"；ABI、数据哈希、代码哈希由服务补齐，拼成 §4 的缓存键
public enum BakePriority : byte { Collision, KnownDestination, InteriorPrefetch, Normal, Background }   // 前四个是流式的四个队列（ENG-PERF §5），Background 是后台预烘
public enum BakeState : byte { Pending, Running, Ready, Failed }
public interface IBakeHandle { BakeState State { get; } object? Product { get; } }   // Ready 时 Product 是引擎侧的产物（TileBundle 等），契约里不出现引擎类型；刚生成的产物直接交给挂载，不再读一遍盘
public readonly record struct BakeProgress(int Total, int Done, string Stage);       // 总量、已完成、当前阶段；扉页的首次运行进度行和设置卡的"存储"行读它

public interface IBakeService      // Kernel 服务（WP-02，[KernelService(typeof(IBakeService), Owner = "WP-02")] 标在实现类上）；gen 不得依赖（§2 ArchUnitNET 规则）
{
    IBakeHandle Ensure(BakeKey key, BakePriority priority);   // 挂载侧用：启动集、本机缓存里有就读，没有就排队生成（查找顺序见 §4）
    BakeProgress Progress { get; }
    long CacheBytes { get; }                                   // 本机缓存当前占用
    void Clear();                                              // 只清缓存；已挂载的不受影响，之后按需重烘
}
```

## 4 资源管线

资源用"PNG 源图 + Godot 导入"，几何是烘焙产物，在玩家机器上本机烘焙（D-165）；不用 KTX2、Basis、glTF 的 meshopt 压缩，也没有首屏网络和 CDN。

- **几何**：
  - 一律由代码生成（gen 里的纯 C# 生成器）。不用 GDScript 生成。
  - 确实需要外部模型时（例如雕像），先向主代理提交申请，批准后才能用 Blender 做少量手工英雄资产。导出 glTF 2.0（`.glb`），放在 `game/assets/hero/`，经 Godot 导入；导入设置里不生成材质（材质由内核按 `ToonSpec` 替换），开阴影网格和 LOD 生成。
- **烘焙**（WP-02；本机烘焙，D-165）：几何类烘焙产物不随安装包发，由玩家机器上的 `IBakeService`（§3）调用与开发烘焙器同一个 `Build()` 生成：第一次需要时生成，写进本机缓存，之后照常从缓存读。"烘焙""TileBundle""缓存键"的含义不变，只是烘焙改在本机进行。
  - **开发和 CI 的无界面烘焙器**照旧：`& $env:UESTC_GODOT --headless --path game -- --bake=all --map=qingshuihe`（`--map=*` 依次烘全部图），用来出参考哈希（`bake.manifest.json`）、给门禁预烘共享缓存（`scripts\gate.ps1`，ENG-GATES §1）。开发态 `-- --gen=live` 实时生成同一份 `Build()`，不读也不写缓存。
  - **产物格式**（安全规则，D-165）：本机缓存和开发共享缓存都不用 Godot 资源格式：`user://` 下的 `.res`、`.tres` 能嵌脚本、读入即执行（SYS-SAVE §8 的威胁模型），所以 `ResourceLoader`、`ResourceSaver` 读写 `user://` 的全局禁令（§2）不设例外。
    - 用自定义的格包二进制格式 `.tbin`，即 `TileBundle`：头（魔数、格式版本、缓存键、长度）+ 各表面的顶点与索引缓冲（与引擎表面字典同一打包，ENG-PERF §5）+ 实例变换 + MultiMesh 缓冲 + 碰撞形状参数 + 统计 JSON（顶点、三角形、表面数、字节），整体 zstd（`FileAccess.OpenCompressed` 或 .NET 的 zstd，P0 定）。解析器逐段做边界检查，长度超限就拒收。
    - 读：工作线程上读文件 → 解压 → 用 `System.IO.Hashing.XxHash128` 算哈希，与启动集里 `res://bake/<地图>/boot/bake.manifest.json` 中这个缓存键的哈希比对，不符就删掉重烘（缓存键不在 manifest 里的文件按 ENG-PERF §5 处理） → 在同一工作线程上用 `RenderingServer.MeshCreateFromSurfaces` 建网格（与 `--gen=live` 同一条路径），碰撞形状由参数在加载线程或主线程上创建（ENG-PERF §5）→ 主线程照常只挂载。
    - 写：先写 tmp，`Flush` 后改名；每图一份 `cache.index`（键、字节、最后使用时间），只用于 LRU 和统计，不作信任依据，信任依据是启动集里的 manifest。
    - 开发共享缓存（`UESTC_BAKE_CACHE`）和 `--bake=all` 也写同一种 `.tbin`；启动集在 `res://` 里，仍用 `.res`。
  - 遮挡体每图一份，随 L4 放在启动集 `boot_<地图>.pck` 里，在切图的 Mount 那一步加载，不进 TileBundle（WP-33，ENG-PERF §4、SYS-MAPS §4.2）。
  - 碰撞级 C 每格单独一个 `.tbin`（同一种格式，只装碰撞形状参数和统计），与 TileBundle 分开，只在行走模式下加载（WP-43，SYS-WALK §13）。
  - 岸线 SDF、路灯灯光表这类派生数据纹理也由烘焙器的子任务产出，是全图常驻的小数据：开发和 CI 的烘焙器出，随启动集发，不在本机烘焙。
  - 需要窗口和 GPU 的烘焙（例如 L2 的半八面体替身，如果 P0 选了它）不能在无界面模式下跑，要排进 GPU 锁（ENG-PERF §12.5）；这类产物只在开发期出，随启动集发布，不在玩家机器上烘。
  - **缓存键**（SYS-MAPS §2.4、ENG-PERF §5）：`ABI/地图/数据哈希/模块@代码哈希/级别/tileId`；室内包、碰撞级 C、底图也各按下文自己的键。数据哈希只算本图里影响 `Build()` 的输入：`campus.json`、`network.json`、`zones.json`、全部覆盖层、`interiors.json`、`outskirts.json`、`backdrop.json`，以及 `map.json` 的 `seed`、`dataRect`；改出生点、默认状态、过场、点位不重烘。
  - **缓存位置**：
    - 开发和 CI：仓库外的共享缓存目录，路径从环境变量 `UESTC_BAKE_CACHE` 读，默认 `E:\Project\UESTC_RPG.cache\bake\`，按图分子目录 `<UESTC_BAKE_CACHE>\<地图>\`，各图互不覆盖。各工作树共用，按区单独烘焙；产物不进 git。
    - 玩家：`user://cache/bake/<地图>/<ABI>/…`，即导出版的 `%APPDATA%` 下，文件布局与缓存键同上。只有 `IBakeService` 读写这里，用 `.tbin`，不经 `ResourceLoader`、`ResourceSaver`（§2）。
    - `--bake-cache=<绝对路径>` 换缓存目录。默认是 `user://cache/bake/`；开发态默认指向 `UESTC_BAKE_CACHE`，所以开发与玩家走同一套逻辑（ENG-GATES §1）。
  - **缓存文件与校验**：写盘先写 tmp，`Flush` 后改名。读到长度超限、解析越界或哈希与 manifest 不符的文件时，删掉它，重新烘焙；`cache.index` 只用于 LRU，不作信任依据。
  - **查找顺序**：启动集（`res://bake/<地图>/boot/`）→ 本机缓存 → 本机烘焙（生成后写进缓存）。
  - **失效**：缓存键里已含代码哈希和数据哈希，游戏更新后自然不命中，只有改了的模块重烘。启动时删掉 `user://cache/bake/<地图>/` 下不是当前 ABI 的目录。
  - **大小**：一张图烘满约 0.8–1.4 GB。默认上限 2.5 GB，超出时按"最久未用"淘汰：先淘汰别的图，再淘汰本图的 L0 和室内包；L1–L4 和全图常驻数据不淘汰。磁盘剩余空间不足 1 GB 时不写缓存，只在内存里生成、用完即弃，并给一次几秒内淡出的临时提示（INV-3 允许）。
  - **与画面设置、世界状态、存档无关**：`Build()` 的输入里没有这些（§2），所以缓存跨存档、跨画质共用；`--seed` 覆盖时种子进数据哈希，各自一份缓存。
  - **什么时候烘**（预算和队列细则见 ENG-PERF §5）：
    - 首次运行（或缓存清空、版本更新以后）：扉页期间，先烘起始视角（T0 或命令行位姿）周围按 ENG-PERF §5 的环要用的格（L2–L3 与外围），再烘平面地图底图；扉页下方显示一行进度，例如"首次运行 · 正在本机生成校园 37%"，用数据字体，烘完就淡出（WP-49，SYS-TITLE §1）。目标初值（开发机）：首次运行到转为实时 ≤ 25 s，底图 8 线程 ≤ 20 s；以后的启动仍是冷启动 ≤ 9 s、热启动 ≤ 3.5 s。
    - 后台预烘：进开始页起，按与起始点的距离由近到远烘满全图，顺序是 L3 → L2 → L1 → L0 → 碰撞 C → 开放室内 → 简化室内。线程数：扉页和开始页期间取 max(1, 逻辑核数 − 2)，进 Playing 后降到 max(1, 逻辑核数 / 4)；线程优先级低于正常，排在按需请求之后。初值：开发机在开始页烘满全图 ≤ 4 min。`--bake-bg=0` 关闭后台预烘，门禁和基准都关。
    - 全图常驻的小数据随启动集同步挂上，不在本机烘焙（见下文"导出与随包内容"）。
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

    - 启动集每图一个 `boot_<地图>.pck`，包内路径一律 `res://bake/<地图>/boot/`：L4、遮挡体、外围 O2 常驻块与卡片版、常驻低精度地形高度，以及需要 GPU 才能出的产物（L2 半八面体替身图集，如果 P0 选了它）；另含全图常驻的小数据：派生数据纹理（岸线 SDF、灯表、`precip_occ` 等）、2 m 分类栅格与 8 m 安全点网格、声学网格、声景场与发声器表（`ambience.bake`）、导航图；以及参考哈希清单 `res://bake/<地图>/boot/bake.manifest.json`（< 1 MB；读本机缓存时按它校验，也是 `--bake-verify` 的比对依据）。小数据和清单由开发和 CI 的烘焙器出，打包步骤从共享缓存取来打进启动集，同步挂上，不在本机烘焙。主 PCK 只留全局资源、各图的 JSON 和英雄资产。某张图第一次进入时用 `ProjectSettings.LoadResourcePack(path, replaceFiles: false)` 挂上，之后不卸载；G6 检查各 PCK 的路径集合两两不相交（SYS-MAPS §2.4）。
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
- **导出过滤器**：`export_presets.cfg` 要显式包含 `data/*.json`、`data/maps/**/*.json`、`data/weather/**`、`data/audio/**` 等非资源文件（是否必需，由 P0 的导出验证确认，§7），排除 `gates/` 下的数据文件和全部 `tests/`，以及门禁夹具图（如果做，SYS-MAPS §15）。`gates/` 的 C# 代码仍编进主程序集，因为 G10 要在导出的 release 版里回放路线；不带门禁参数时它不启用。G6 的上限是安装目录 ≤ 0.9 GB、分发压缩包 ≤ 0.7 GB（上文"导出与随包内容"，ENG-PERF §8）。

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
| 存档 | `ISaveService` 的桩：`Enabled = false`，三个存档位都是 `Empty`；`Request`、`SaveNow` 返回 `Disabled`；`NewGame` 只把已登记的各段设为 `CreateDefault`，并让 `Instance` 加 1；`Load` 不论 `Full`、`Preview` 都返回 `Empty`，`HasPendingPose` 恒为 false，`CompleteResume` 立即完成；`FlushForQuit` 立即完成，所以关窗口照常退出 |
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

部件按"编目先行"的方式管理：

- `game/contracts/Parts/` 冻结所有共享部件的 ID 常量和参数 record（`PartCatalog`），受 PublicApiAnalyzers 保护。
- 区域包直接按 ID 放置部件，先显示成方块；部件包完成后，方块自动换成真模型。
- 区域包需要特有部件时，在自己的目录里新建，ID 加上区名前缀。证明可以复用后，再申请提升为共享部件。

## 6 git 工作树流程

**远端与分支**（D-166）：

- 远端 `origin` 是 GitHub 上的 `Wendystar0628/UESTC_RPG`，只有 `main` 和里程碑标签（`baseline-map`、`contracts-v1` 等）。不开 PR，不推送任何别的分支，不对 `main` 强推。
- 本地分支只用来隔离并行施工：每个写代码的子代理一个工作树、一个本地临时分支 `wp/<xx>-<slug>`。分支在本地提交、从不推送，合进 `main` 后立即删除。
- **并发规模**：常态同时运行十几个 Opus 代理，可以分属两个账号，都在本机同一个仓库上工作。任何时刻只有一个主代理；合并队列、`main` 上的提交和推送只由它做。
- **什么时候开工作树**：同一时刻有两个以上代理要写代码、编译或导入时，每个写代码的代理各开一个工作树。
  - 只有一个代理写代码时（例如主代理自己做 WP-00、改共享文件或小修），直接在主检出（`E:\Project\UESTC_RPG`）的 `main` 上做：同样先过与改动相关的门禁，再提交、推送。
  - 只改文档、不编译的代理在主检出里改，由主代理分派（CORE-CONVENTIONS §7.6），两个代理不同时改同一个文件。

工作树之间靠"每个工作树一个 `override.cfg` + 复制 `.godot` 做种子"隔离。

引擎路径一律从环境变量 `UESTC_GODOT` 读命令行版 exe，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。门禁比对 `--version` 的输出与 `tools/godot-version.txt`。

1. **仓库现状与 D0**：
   - 仓库已建好：清理前的整棵树在标签 `baseline-map`；`main` 上是开发文档和待迁移的数据与脚本（§1）。
   - `.gitattributes` 已有 `* text=auto eol=lf` 和图片、`.res`、`.glb`、`.pck`、音频的 binary 标记；WP-00 补字体的 binary 标记，音频改走 Git LFS（`*.wav`、`*.ogg`，SYS-AUDIO §14.3）。
   - `.gitignore` 已有 `.godot/`、`override.cfg`、`bin/`、`obj/`、`.vs/`、`*.user`、`output/`、`.venv/`、`__pycache__/`；WP-00 按需补 `build/`。
   - WP-00 在主检出的 `main` 上搭好 ENG-WP §1 列出的 Godot 骨架（含全部 gen 目录的 csproj），在主检出跑一次 `--headless --import`，生成的 `.godot` 留作工作树的种子。完成后打标签 `contracts-v1` 并推送。
2. **开工作树**：由 `scripts\wt\new.ps1 -Wp WP-18 -Slug south` 完成，从当前 `main` 拉本地分支，展开就是：

   ```powershell
   $G  = $env:UESTC_GODOT
   $WT = '..\UESTC_RPG.wt\WP-18'
   git worktree add $WT -b wp/18-south main
   robocopy game\.godot "$WT\game\.godot" /MIR /XD shader_cache    # 用主检出的导入缓存做种子；退出码小于 8 都算成功
   @"
   [application]
   config/use_custom_user_dir=true
   config/custom_user_dir_name="UESTC_WP18"
   "@ | Set-Content -Encoding utf8 "$WT\game\override.cfg"          # 每个工作树一个 user://，日志和着色器缓存互不干扰
   dotnet build "$WT\game\Campus.sln" -warnaserror
   & $G --headless --path "$WT\game" --import --log-file "$WT\output\logs\import.log"
   ```

   - 不要用目录联接共享 `.godot`：导入产物的文件名是"文件名-路径 md5"，两个分支内容不同时会互相覆盖。复制种子后，引擎发现修改时间不同会再比对 md5，内容一致就不重新导入。
   - `--import` 的含义是"启动编辑器，等资源导入完成后退出"。导入日志里不得有 `ERROR` 或 `SCRIPT ERROR`（G1）。
3. **日常开发**：
   - 在自己的分支上小步提交，不推送，不碰 `main`。主代理通知 `main` 有新的合入时，在工作树里 `git rebase main` 跟上；rebase 后如果资源有变化，再跑一次 `--headless --import`（`gate.ps1` 会自动做）。
   - 代理只走命令行，不在编辑器里打开或重存场景；场景尽量由代码生成。
   - 截图类门禁（G7、G9）必须在交互式会话的窗口模式下跑，窗口不能最小化；无界面模式用的是空渲染器，不能截图，也不编译着色器。截图取共享 GPU 锁，同时最多 2 个工作包在跑；性能门禁（G10）取独占锁（ENG-PERF §12.5）。
   - `gate.ps1` 先跑 `--bake=all` 把共享缓存（`UESTC_BAKE_CACHE`）按哈希增量补齐；截图类门禁、基准和 G10 再带 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`，计时不受本机烘焙干扰（追加方式见 ENG-GATES §1）。各工作树的 `override.cfg` 把 `user://` 指向 `UESTC_WPxx`，所以玩家路径的本机缓存各工作树各一份；开发态 `--bake-cache` 默认指向共享缓存，一般用不到它。
   - `dotnet build`、`--import` 和烘焙（含本机烘焙）都是重 CPU 任务。十几个工作树并行时，同时运行的重 CPU 任务不超过 max(2, 逻辑核数 / 8) 个，用 `..\UESTC_RPG.wt\.locks\` 下的计数锁排队（初值，P0 按本机实测调整，§9）；持有独占 GPU 锁时，其他工作树一律不得运行它们。
4. **合并**（主代理的合并队列，一次合一个）：
   1. 子代理回报完成后，主代理在它的工作树里 `git rebase main`；
   2. 跑 `scripts\gate.ps1 -Wp <WP-xx>`（按改动跑快检，碰到核心专项的输入时加跑核心专项，ENG-GATES §2），全部通过（CORE-CONVENTIONS §8.3）；
   3. 回到主检出，`git merge --squash wp/<xx>-<slug>`，提交为一个提交，首行写 `WP-xx: <一句话说明>`；
   4. `git push origin main`；
   5. 删本地分支：工作包做完了就 `git worktree remove` 再 `git branch -D`；还要继续做，就在工作树里 `git switch -C wp/<xx>-<slug> main`，从新的 `main` 重建分支，保留 `.godot` 和编译缓存；
   6. 在主检出跑一次 `--headless --import` 更新工作树种子，并通知其他工作树 rebase。
   - 内核包按这个顺序合入：WP-08 → WP-34 → WP-01 → WP-50 → WP-04 → WP-02 → WP-46 → WP-03 → WP-05 → WP-09 → WP-06 → WP-07 → WP-43 → WP-27。WP-50（存档，与 WP-34 同为 P0，容器版本 1 和第一份金档在 P0 交付）排在 WP-01 之后；WP-46（地图服务，要 WP-02 的整图卸载接口）排在 WP-02 之后、WP-43 之前（SYS-SAVE §15、SYS-MAPS §16）。
   - 其余包靠桩就能运行，门禁通过就可以合入，不必等内核。
5. **冲突规则**：
   - 改动路径必须在 `ownership.json` 给本包划定的范围内，否则门禁 G3 拒绝。
   - 以下文件只有主代理能改：`game/project.godot`（含 Shader Globals 登记、动作表、Autoload）、`export_presets.cfg`、`Campus.sln`、全部 `*.csproj`、`Directory.Build.props`、`Directory.Packages.props`、`packages.lock.json`、`global.json`、`.editorconfig`、`.gitattributes`、`.gitignore`、`game/addons/`、契约、`ownership.json`、`budgets.json`、`third_party.json`、`tools/`、`docs/design/`、`docs/godot-api/`，以及开放室内清单 `game/data/maps/*/interiors.json`（清单的增删由主代理审批，WLD-INTERIORS §14）。`map.json` 归 WP-46，但字段增改经主代理（SYS-MAPS §3）。
   - `.uid` 跟着源文件走，和源文件在同一个目录，所以目录独占规则自然覆盖它。新文件的 `.uid` 由 `--import` 生成后一起提交。复制文件时删掉复制来的 `.uid`，否则会出现重复的 UID（G3 检查"每个脚本都有 `.uid`、全仓 UID 不重复"）。
   - 代码里只用 `res://` 路径，不手写 `uid://`。
   - `.import`、`.uid`、`export_presets.cfg` 进 git；`.godot/`、`override.cfg`、烘焙产物不进。

## 7 主要风险与对策

| 风险 | 对策 |
| --- | --- |
| 部件库成为瓶颈 | 编目和参数类型先冻结，方块桩先合入；区域可以先建私有部件；部件库拆成 5 个包 |
| 风格不统一 | 场景里只允许经 `ToonSpec` 取得的卡通材质，着色器只用白名单；颜色只能用色名；先做金样（见 ART-MODELING §6），再放开并行；设一名风格负责人横向比对 |
| 预算超支 | 每个部件必须声明各级细节的面数；按模块统计预算；单个格子先卡上限；G8 用烘焙统计预测；详见 ENG-BUDGET |
| 合并冲突 | 目录独占加特性自动发现；共享文件只有一个写入者；契约只增不改（PublicApiAnalyzers）；`main` 有新合入时 rebase（§6）；代理不在编辑器里重存场景（编辑器存盘会给每个节点写 `unique_id`，产生无关差异） |
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
| 工作树共用 `user://` | 所有工作树的项目名相同，会共用 `%APPDATA%\Godot\app_userdata\<名>`，日志和运行时着色器缓存互相干扰。每个工作树生成 `override.cfg`，把 `custom_user_dir_name` 设成 `UESTC_WPxx`（§6） |
| .NET 8 于 2026-11-10 停止支持 | Godot 4.7.2 默认目标是 net8.0。P0 原型验证能否改用 net10.0（需要另装 SDK 时从官方来源直接下载，下完说明，CORE-CONVENTIONS §8.4）；`global.json` 锁 SDK 版本 |
| 无界面模式不编译着色器 | 着色器错误只有在窗口模式下才会暴露。G5 加一次短时间的窗口冒烟运行；G7 也在窗口模式下跑 |
| 导出版里的反射与程序集加载 | 编译器会丢掉没用到的程序集引用，导出后反射可能扫不到模块。注册器按构建时生成的程序集清单显式加载；P0 跑通 `dotnet build` → `--import` → `--export-release` 全链路，确认导出版注册结果与编辑器一致 |
| 导出模板未下载 | .NET 导出模板约 1.2 GB，第一次做 G6 导出前从官方来源直接下载，下完说明（CORE-CONVENTIONS §8.4） |
| JSON 的导出行为不确定 | 源码里 JSON 被识别为资源，文档却把它列为非资源文件。P0 验证导出包里能否读到 `data/*.json`，必要时在导出过滤器里显式包含 |
| 生成结果不确定 | .NET 每个进程的字符串哈希种子不同，`HashSet`、`Dictionary` 的遍历顺序也不可靠。种子一律用 `StableHash`；遍历前先排序；`build()` 内部不做并行浮点归约（§2） |
| 两套数学类型 | 契约用 `System.Numerics`（`Matrix4x4` 是行向量约定），Godot 用 `Transform3D`（列向量约定），两边的 `Vector3` 同名。转换只用内核提供的一组扩展方法（WP-00），G2 有往返测试；同一文件里两套都用时，用 `using N = System.Numerics;` 别名 |
| C# 垃圾回收造成卡顿 | 逐帧循环不分配内存（`CameraMedium` 等每帧查询的类型都是值类型）；用 `GC.GetTotalPauseDuration` 加自定义监视项监测（ENG-PERF §12.2）；生成代码用 `ArrayPool` 和预分配缓冲，后台烘焙期间第 2 代回收的暂停另测（§9） |
| 40 个工作树同时导入、编译 | 每个工作树估计 0.1–0.3 GB，首次导入在分钟级以内（待验证，见 §9）；重 CPU 任务按 §6 第 3 步排队 |
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
| 音频把主线程余量吃光 | 音频主线程 ≤ 0.35 ms 从余量里划出，开发机余量只剩约 0.15 ms；核显档默认把 3D 声源上限降到 24、次要的床退成 2 个锚点；P0 在开发机和核显笔记本上实测（SYS-AUDIO §18.1） |
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

**扩展点清单**（"现在预留"一栏写"已写进 §3"的，契约以 §3 为准；表里其余的服务名、类型名都是草案）

| 方向 | 位置（§、类型） | 现状 | 扩展方式 | 现在预留接口？ |
| --- | --- | --- | --- | --- |
| 规则集 | §3 规则集与观者状态块 `RulesetId`、`RulesetDescriptor`、`IRuleset`；存档块 `SaveMeta.Ruleset` | 观光规则集 `sightseeing`（`game/data/rulesets/sightseeing.json`）：旁观与行走两种观察模式、世界控件全开、`all-unlocked`、一个偶发事件、过场只在 Title、Entering、Leaving；内容规则（无人物、无点选、无听得清的人声）只供门禁取判据；只在新游戏和读档时切换；各包读规则集，不在代码里写观光专属的常量 | RPG 另加一个规则集文件 `game/data/rulesets/<id>.json`；`RulesetDescriptor` 以 init 属性加字段，`InteractionRule` 末尾追加；门禁按规则集取判据 | 已写进 §3（ENG-ROADMAP §6 第 1 条） |
| 观者位置 | §3 规则集与观者状态块 `IObserverState`；相机块 `ICameraRig.Pose`；§2 ArchUnitNET 规则（`Campus.Hud.Map`） | 第一人称下观者位置和镜头重合：`IObserverState`（脚位、眼位、朝向、所在空间 `NavSpace?`、观察模式）旁观时取镜头，行走时取 WP-42 的物理体；小地图的玩家箭头、`Proximity` 判定、进楼触发、存档 meta 的空间读它，"画面从哪看"（视锥、听者、流式选级）读 `ICameraRig` | 第三人称时 `IObserverState` 改由角色给出，读它的包不改 | 已写进 §3（ENG-ROADMAP §6 第 2 条） |
| 输入占用 | §3 相机块 `ICameraRig.LockInput`、`InputLockReason` | 按原因计数的 `IDisposable LockInput(InputLockReason r)`，没有任何占用时才接受观者输入；现有原因：大地图、交还控制、换图（SYS-ATLAS §4.1、SYS-TITLE §7.3、SYS-MAPS §4.2） | `InputLockReason` 末尾追加对话、剧情过场、菜单 | 已写进 §3（ENG-ROADMAP §6 第 3 条） |
| 角色与实体 | §2 自动发现与程序集边界；`[KernelNode]`、`IRenderHost` | 没有角色与实体层，画面不出现人物（INV-6）；动态物体（动物、粒子）由各系统的 `[KernelNode]` 自管，经 `IRenderHost` 申请显示；`IRenderHost` 没有骨骼网格与动画的入口 | 新的 Kernel 服务 `ICharacterService`（角色）、`IAgentService`（NPC 与日程），目录 `game/entities/`，契约 `game/contracts/Entities/`；`IRenderHost` 加骨骼实例的方法，按包记账（ENG-PERF §12.2）；角色的声音用 `IAmbienceService.AddDynamic`、`MoveDynamic`；预算按账本另立（ENG-ROADMAP §5） | 只占名字：目录与实体 ID 前缀 `npc:`、`item:`、`quest:`（§2；ENG-ROADMAP §6 第 6 条）；接口不预留 |
| 观察模式与镜头 | §3 世界状态块 `ViewMode { Fly, Walk }`；相机块 `ICameraRig`、`IViewControl`、`ModeSwitchVia` | 只有旁观和行走；观者没有可见的身体；镜头不自己动（INV-2） | `ViewMode` 末尾加第三人称跟随；`ICameraRig` 加带默认实现的跟随目标成员；行走的物理（SYS-WALK §12）作为角色的移动层复用 | 否 |
| 材质与描线 | §3 风格块：7 位材质 ID，全场不超过 128 个（含剖切 ID）；`LineClass`、`ShadeSteps` | 场景材质已占去大部分 ID；`LineClass`、`ShadeSteps` 没有角色档 | 角色共用白名单里新增的一个蒙皮卡通着色器和少量材质 ID；`LineClass`、`ShadeSteps` 末尾追加角色档（ENG-ROADMAP §7 第 11 条） | 否 |
| 过场 | §3 开始页块：过场数据 `CinematicDef` | 没有角色轨；不认识的轨由检查报错（SYS-TITLE §8.4）；过场只在 Title、Entering、Leaving | 提高 `format`，加角色轨、对话轨；Playing 里的剧情过场由同一个播放器播，输入占用按原因计 | 否（`format` 只增，已经够用） |
| 交互 | §3 室内块"会动的门"；UI-NAV §4.4 | 除自动门（观者走近触发）以外没有可以点选的东西：没有交互键、准星、选中描边；3D 里不叠加世界空间标记（UI-NAV、UI-VISUAL、SYS-ATLAS） | 新的 Kernel 服务 `IInteractionService`（附近的可交互对象、当前焦点、执行）；gen 侧用纯数据声明可交互物，以 init 属性挂在 `ContentOutput`、`InteriorLayout` 上；输入动作 `interact` 由主代理加进 `project.godot` 的动作表；提示只在界面层，3D 里出不出提示由 RPG 规则集定 | 部分：`FoleyEvent.SourceId`（交互对象的稳定 ID）已有；E 键不分配给观光功能（ENG-ROADMAP §6 第 6 条）；接口不预留 |
| 门锁 | §3 室内块 `IInteriorDoors`（只读）、`DoorLeafDecl`；导航图块 `DoorState.Locked`；碰撞块 `SurfaceTag.DoorClosed` | 门锁在烘焙时定死；读档、切图时所有门落到 Closed，门的状态不进存档；碰撞不随世界状态变（ENG-BUDGET §7 第 11 条） | 在运行时门体上加锁状态（WLD-INTERIORS §14）：新增可写的门控制接口（例如 `SetLocked(building, doorId, locked)`），锁状态进 `map:<id>/objects` 段；导航图的门状态允许运行时覆盖；ENG-BUDGET §7 写明运行时门体是可走性随状态变的唯一通道 | 否 |
| 任务、背包、对话 | SYS-SAVE §2 的预留段 `quest`、`inventory`、`character`、`flags`、`map:<id>/objects`；§3 存档块 `SaveScope`、`[SaveSection]` | 只占名字，不登记 | 每个系统一个 Kernel 服务（`IQuestService`、`IStoryState`、`IDialogueService`、`IInventoryService`，ENG-ROADMAP §4）加一个 `[SaveSection]`（`Phase = Content`，`Order` 由 SYS-SAVE分配）；DTO 用本包的源生成 `JsonSerializerContext` | 段名已占，含 `calendar`、`agents`（SYS-SAVE §2；ENG-ROADMAP §6 第 6 条）；容器版本 1 只加段、不改容器（SYS-SAVE §3） |
| 剧情显隐 | §3 世界状态块 `VariantTag`、`ChannelDecl.Source` | 物体显隐只看季节与昼夜掩码；通道只由灯光时刻表和世界状态驱动 | `VariantTag` 加 init 属性（剧情标志名），内核按 `IStoryState` 显隐，不重建格子；`ChannelDecl.Source` 加 `flag:` 前缀 | 否 |
| 任务标记 | §3 平面地图块 `MarkerKind.Quest`、`IAtlasService.Markers`、`TravelRefusal.Locked` | `Quest` 已占位；标记只由数据和室内索引生成，`Markers` 只读 | 任务服务经 `IAtlasService` 新增的、带默认实现的成员登记运行时标记，追踪沿用 `Track` | `Quest` 已预留；登记接口没有留 |
| 界面 | UI-VISUAL §8.12；§2 ArchUnitNET 规则（`Campus.Hud` 的依赖）；§3 开始页块 `AppState` | 对话或旁白条、任务列表、物品卡只定了视觉；`Campus.Hud` 只许依赖登记过的契约 | 新页面放 `game/hud/pages/`，视觉按 UI-VISUAL §8.12；ArchUnit 规则给 `Campus.Hud` 加对新服务的许可；对话和菜单是 `Playing` 里的界面态，`AppState` 不加值 | 视觉已预留；契约不预留 |
| NPC 寻路 | §3 导航图块 `INavGraph`、`NavQuery`、`NavFlags.NpcOnly`、`PlayerOnly`、`Locked`（WLD-INTERIORS §13） | 烘焙的全图导航图（随启动集发）只给路线预览、G19 和落点用；`NavQuery` 默认避开 `NpcOnly`；不用 NavigationServer3D | `IAgentService` 的宏观寻路直接用 `INavGraph.FindPath`（可在线程池里跑）；局部避障用 NavigationServer3D，按碰撞格和楼层烘焙 `NavigationMesh`（参数见 WLD-INTERIORS §13）；`NavQuery` 加 init 属性（体型、速度）；锁门、封路这类运行时变化另加一层导航覆盖 | 已预留：`NpcOnly`、`PlayerOnly` 和楼级超图 |
| 存档的预留值 | §3 存档块 `SaveMeta.Ruleset`、`SaveReason.Checkpoint`、`SaveReason.Manual`、`SlotKind.Manual`、`SlotId` 的 `m1`–`m9`、`SaveMeta.Origin` | 都已预留，现在不用 | RPG 的存档写 `Ruleset = "rpg"`；剧情节点调 `SaveNow(SaveReason.Checkpoint)`；手动槽用 `m1`–`m9` | 已预留 |
| 物体差分 | SYS-SAVE §2 的 `map:<id>/objects`；§3 部件块 `PlacedPart` | 只占名字；读档只恢复位姿和所在空间；生活痕迹是不可拿取的静态几何 | 一个按图的 Kernel 服务记"相对烘焙基线的差分"，挂载后套用（隐藏、换部件、开锁）；可拿取的物品是运行时节点，由 `[KernelNode]` 负责；差分的键是 `FeatureId` 加局部键，所以 `PlacedPart` 加 init 属性（局部键），只给可变的物体填 | 否 |
| 开局 | §3 存档块 `NewGameContext`、`ISaveService.NewGame`；多地图块 `MapWorldRules.NewGameStart` | 新游戏一律从默认图的默认状态开始，清水河上就是落金初灯 | RPG 的开局由 `NewGame(…, start)` 的 `start` 和本图的 `newGameStart` 给出；新游戏先定规则集，再调各段的 `CreateDefault`（§3 存档块末尾） | 参数已有 |
| 解锁 | §3 平面地图块 `IUnlockPolicy`、`IAtlasService.PolicyId`、`UnlockState`、`AtlasSave.Unlock`、`AtlasSave.VisitedAnchors` | 观光规则集的 `UnlockPolicy` 恒为 `"all-unlocked"`；`"proximity"` 只有接口（SYS-ATLAS §8）；解锁表为空 | 实现并注册 `proximity` 策略（注册方式由 WP-48c 定）；策略取自规则集，新游戏时写进 `AtlasSave.Policy` | 已预留 |
| 多地图 | §3 多地图块 `MapKind`、`MapTimeRule.Fixed`、`MapPortal`、`MapDescriptor.Portals`、`MapTravelCause.Portal`（SYS-MAPS §14） | 只实现 `Outdoor` 加 `Shared`；`Fixed` 和非 `Outdoor` 的图 `Travel` 一律 `Rejected`；传送门为空；只用一个 World3D | 副本、秘境用 `Instance` 加 `Fixed`；楼层图用 `Interior`；传送门按 `MapPortal` 的触发盒调 `IMapService.Travel`（`Cause = Portal`）；新的图类只在 `MapKind` 末尾追加 | 已预留 |
| 数据引用 | §2 G4 正则：实体 ID、地图 ID 和数据路径不得写成字面量 | 代码里不认识任何具体的图和实体 | 任务、对话、事件里的地点引用一律写在数据里（`WaypointRef`，或 `MapId` 加 `FeatureId`）；按图的部分放 `game/data/maps/<地图>/` 下 | 不需要 |
| 事件系统 | §3 `FramePriority.Motion`（偶发事件在 WP-26 那一步）；音频块 `AmbienceCue.Group`、`IAmbienceService.CancelCues`；SYS-MOTION §10 | 偶发事件全片唯一，由 WP-26 的 `IncidentDirector` 按 `incidents.json` 运行，只跑规则集 `Incidents` 里登记的事件，不是契约；触发条件（ω、转场后 20 s、180–300 s）写在 SYS-MOTION；"转场"的定义分散在 SYS-MOTION §12、SYS-TITLE、SYS-SAVE、SYS-MAPS；事件不进存档，读档重新计时 | 泛化为 Kernel 服务 `IEventDirector`（由 SYS-MOTION的事件导演泛化）：事件登记在数据里（触发条件、冷却、互斥组、所需标志），现有的偶发事件成为其中一条；"转场"收成一个只读量（例如 `IWorldState` 加带默认实现的"上次转场时刻"）；声音整批取消沿用 `AmbienceCue.Group`；一次性的剧情事件记进 `flags` 段 | 数据化：`game/data/maps/<地图>/incidents.json`（WP-26，§2、SYS-MAPS §2.1；ENG-ROADMAP §6 第 5 条）；契约不预留 |
| 音频 | §3 音频块 `AudioBuses`、`AudioGroup`、`AudioSettings`、`DuckReason`；SYS-AUDIO §3.1、SYS-AUDIO §8 | 没有语音总线；音量只有五组；不许出现听得清的人声（SYS-AUDIO §8、G21） | `AudioBuses` 加 `Voice`（Master 的直接子总线，排在 World 之前，不进声学链）；`AudioGroup` 末尾加 `Voice`；`AudioSettings` 加 init 属性 `Voice`（`audio.cfg` 加键，`Schema` 加 1）；`DuckReason` 末尾加 `Dialogue`；`IAudioService` 加带默认实现的播放对白的成员；G21 的人声判据按规则集取 | 否 |
| 文本 | §3 点位块 `LocalizedName(Zh, En)` | 中英两种文字内联在数据里；雾幕后不许新生成 MSDF 字形（ENG-BUDGET §7 第 9 条） | `LocalizedName` 以 init 属性加文本键；新增 `ITextService` 与 `game/data/text/`；对白的字形集在构建时收齐 | 只占目录 `game/data/text/`（§2；ENG-ROADMAP §6 第 6 条） |
| 日历 | §3 世界状态块 `IWorldState.SeasonTarget`、`IWorldControl.SetSeason` | 季节不自动推进，时间、季节、天气、流逝都能随意调（SYS-TIME §1、UI-NAV §4） | `ICalendar` 由 `WorldHours` 推出学年、学期和日期，在 `FramePriority.WorldState` 那一步推进季节（换季仍是正向过渡）；世界控件按规则集变灰，做法同 `MapTimeRule.Fixed` | 只占段名 `calendar` |
| 通用约束 | §2"`build()` 拿不到世界状态"；ENG-BUDGET §7 | 几何和烘焙碰撞不随时季、天气、剧情变化 | RPG 的世界变化只走运行时层（显隐掩码、物体差分、运行时门体、导航覆盖），不进 `build()`，烘焙缓存键不变 | 不需要（长期约束） |

## 9 待验证（P0）

正文原位只写验证前用的初值；下表列出要验证的内容、方法和负责方。

| 待验证 | 验证方法与负责 | 验证前用的初值 |
| --- | --- | --- |
| 导出版里的反射与程序集加载：注册结果是否与编辑器一致（§2、§7） | P0 跑通 `dotnet build` → `--import` → `--export-release` 全链路，比对两边的注册结果；WP-00 | 注册器按构建时生成的程序集清单逐个显式加载 |
| JSON 的导出行为：导出包里能否读到 `data/*.json`（§4、§7） | P0 的导出验证；WP-00、WP-02 | 导出过滤器显式包含 `data/*.json`、`data/maps/**/*.json`、`data/weather/**`、`data/audio/**` |
| 能否改用 net10.0（.NET 8 于 2026-11-10 停止支持，§7） | P0 原型验证；需要另装 SDK 时从官方来源直接下载，下完说明（CORE-CONVENTIONS §8.4）；主代理 | net8.0，`global.json` 锁 SDK 版本 |
| gdUnit4Net 5.1（RC）下需要引擎的测试怎么托管（§2、§7） | P0 验证 `[RequireGodotRuntime]` 经 `gate.ps1` 把 `UESTC_GODOT` 交给测试适配器的做法；WP-00、WP-27 | 按 §2 的写法；转正式版后由主代理换版本（ENG-WP §5） |
| 40 个工作树同时导入、编译的磁盘占用与耗时（§7） | D0 在主检出和一个工作树上实测；主代理 | 每个工作树 0.1–0.3 GB，首次导入在分钟级以内；重 CPU 任务按 §6 第 3 步排队 |
| L2 是否用半八面体替身（§4） | P0 选型（ENG-PERF）；WP-02 | 选用时这类烘焙要窗口和 GPU，排进 GPU 锁（ENG-PERF §12.5）；只在开发期出，随启动集发布，不在玩家机器上烘 |
| 切图的卸载泄漏（§7） | P0 实测同图重载 3 次，按 G16 的判据（显存、对象数 ±5%，旧 `IWorldModel` 的弱引用被回收）；WP-46 | 集中测试时同图重载（SYS-MAPS §13） |
| 全局纹理换尺寸、雾幕里重新分配材质 ID（§7） | P0 各验一次换尺寸后的 RID 与编译计数、雾幕里重新分配材质 ID（SYS-MAPS §15）；WP-46、WP-04 | 包内路径按图分开，G6 查各 PCK 的路径不相交 |
| 外围的程序化楼型观感（§7） | P0 截图定楼型；WP-47 | WLD-OUTSKIRTS的楼型 |
| 音频的主线程开销（§7） | P0 在开发机和核显笔记本上实测（SYS-AUDIO §18.1）；WP-53a | 音频主线程 ≤ 0.35 ms；3D 声源上限 32，核显档默认 24；开发机主线程余量约 0.15 ms |
| 安装目录与分发压缩包的实测体积（§4） | P0 实测后统一核账（ENG-BUDGET §8、ENG-PERF §8）；主代理 | G6：安装目录 ≤ 0.9 GB，分发压缩包 ≤ 0.7 GB（全部图之和），音频单列不超过 165 MB |
| 各类产物的单件生成耗时，以及全图预烘总时长（§4） | P0 在开发机单线程实测；WP-02 | L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms；开始页烘满全图 ≤ 4 min |
| `.tbin` 在工作线程上的读写（zstd 解压、XxHash128、`MeshCreateFromSurfaces`）是否安全、耗时多少；zstd 用 `FileAccess.OpenCompressed` 还是 .NET 的实现（§4） | P0 验证；WP-02 | 工作线程上读、解压、校验、建网格，主线程只挂载；zstd 先用 `FileAccess.OpenCompressed` |
| `DetMath` 的精度与速度（§2、§3） | WP-00 的单元测试给出各函数的误差界和每次调用耗时 | 纯 C# 实现；误差界在 P0 标定 |
| 后台烘焙期间的 GC 暂停（§4、§7） | P0 测第 2 代回收的暂停；WP-02 | 生成代码用 `ArrayPool` 和预分配缓冲；暂停上限在 P0 标定 |
| 首次运行到转为实时的时间（§4） | P0 在空缓存目录上实测；WP-02、WP-49 | ≤ 25 s（开发机），底图 8 线程 ≤ 20 s |
| 本机缓存烘满后的实际体积（§4） | P0 全图烘满后量；WP-02 | 一张图约 0.8–1.4 GB；默认上限 2.5 GB |
| 十几个工作树并行时重 CPU 任务的并发上限（§6） | P0 在本机同时开 12 个工作树编译、导入、烘焙，看总耗时与内存；WP-00 | max(2, 逻辑核数 / 8) 个 |
