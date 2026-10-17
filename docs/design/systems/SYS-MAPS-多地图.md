---
id: SYS-MAPS
title: "多地图：按图数据、`IMapService` 与切换"
category: systems
summary: "按图数据与 map.json、IMapService 先卸后装切图、时间全局天气按图、map_rect、同图重载门禁"
keywords: [多地图, 换图, 切图, 地图服务, 按图数据, 按图与全局, 先卸后装, 切换 8 步, 同图重载, 天气按图, 进图补算, 地图归属, 本图默认状态, 单图半径, 启动集 PCK, IMapService, IMapContext, IMapAware, MapScope, MapDescriptor, MapId, MapVisit, MapTravelRequest, IMapTransition, map.json, index.json, map_rect, "boot_<地图>.pck", FramePriority.Map, "--gate=maptravel"]
owners: [WP-46]
contributors: [WP-00, WP-01, WP-02, WP-08, WP-15, WP-22, WP-23, WP-24, WP-26, WP-27, WP-34, WP-35, WP-36, WP-39, WP-40, WP-43, WP-44, WP-45, WP-47, WP-48, WP-49, WP-50, WP-51, WP-53]
depends: [SYS-VIEW, ENG-ARCH, ENG-PERF, SYS-TIME, SYS-WEATHER, SYS-SAVE, WLD-OUTSKIRTS]
related: [CORE-INDEX, WLD-DIORAMA, ENG-GATES, ENG-BUDGET, ENG-WP, UI-NAV, SYS-WALK, SYS-GRAPHICS, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP]
contracts: [MapId, WaypointRef, MapKind, MapTimeRule, GeoFrame, MapRect, RoamLimits, MapTitle, MapSpawn, MapWorldRules, MapEnvironment, MapPortal, MapDescriptor, MapScopeAttribute, IMapContext, MapTravelCause, MapTravelRequest, MapStage, MapTravelResult, IMapTransition, IMapService, IMapAware, MapVisit]
data: ["game/data/maps/index.json", "game/data/maps/qingshuihe/map.json", "game/data/maps/qingshuihe/", "game/contracts/schemas/map.schema.json"]
gates: [G2, G4, G5, G6, G7, G8, G13, G16]
---
# SYS-MAPS · 多地图：按图数据、`IMapService` 与切换

> **范围**：本文规定多地图架构：按图存放的数据与目录，`map.json`（`MapDescriptor`），`IMapService` 的加载与切换（一个 World3D，先卸后装，8 步），时间与季节全局、天气按图，全局参数 `map_rect`，以及同图重载门禁。现在只有一张图 `qingshuihe`，架构从第一天起按多张图写。
> **不在本文**：平面地图的标记、解锁与传送（SYS-ATLAS的 `IAtlasService`）；活动范围的公式与外围数据（WLD-OUTSKIRTS）；开放室内的清单与加载（WLD-INTERIORS）；存档的段格式（SYS-SAVE）；开始页与过场（SYS-TITLE）；音频换图的细节（SYS-AUDIO §2.7）。
> **查阅**：先读 §0；目录、按图与全局的划分、本机烘焙缓存与启动集看 §2；`map.json` 的字段看 §3；切换 8 步、规则与各入口看 §4；时间、天气在切图时怎样看 §5；模块归属与 `MapScope` 看 §7；契约签名看 §8；`map_rect` 看 §10
> **关联**：[SYS-VIEW 观察模式与介质](SYS-VIEW-观察模式介质与开场.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH-工程架构.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF-性能工程.md)、[SYS-TIME 时间与季节](SYS-TIME-时间与季节.md)、[SYS-WEATHER 天气](SYS-WEATHER-天气.md)、[SYS-SAVE 存档](SYS-SAVE-存档与持久化.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS-边界与外围.md)；[CORE-INDEX 总览](../README.md)、[WLD-DIORAMA 箱庭与远景](../world/WLD-DIORAMA-箱庭边界截面与远景.md)、[ENG-GATES 验收](../engineering/ENG-GATES-验收与门禁.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET-性能预算与硬规则.md)、[ENG-WP 工作包](../engineering/ENG-WP-工作包派工单.md)、[UI-NAV 界面与跳转](../ui/UI-NAV-界面与操作说明.md)、[SYS-WALK 行走模式](SYS-WALK-行走模式.md)、[SYS-GRAPHICS 画面设置](SYS-GRAPHICS-画面设置.md)、[SYS-ATLAS 平面地图与传送](SYS-ATLAS-平面地图与传送.md)、[SYS-TITLE 开始页与过场](SYS-TITLE-开始页与进入过场.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS-室内.md)、[SYS-AUDIO 音频](SYS-AUDIO-音频.md)、[ENG-ROADMAP 扩展路线](../engineering/ENG-ROADMAP-扩展路线.md)

### 分工与约定

- **WP-46 地图服务与多地图**独占 `game/kernel/map`、`game/data/maps/index.json` 和各图的 `map.json`；其他包的增量见 §16。
- `IMapService`（WP-46）只管地图的加载与切换；平面地图的标记、解锁、传送是 `IAtlasService`（WP-48，SYS-ATLAS），两者不混名。
- 地图 ID 一律写全称 `MapId("qingshuihe")`。

## 0 结论

1. **现在只有一张图 `qingshuihe`，但从第一天起按"多张图"写**。`campus.json` 的 `meta` 里存着原点、投影和范围（§3 的 `geo` 从它派生）。
2. **按图的数据放在 `game/data/maps/qingshuihe/`，代码目录不按图拆分**。各区的 gen 程序集由 WP-00 在 D0 写一行程序集级特性 `[assembly: MapScope("qingshuihe")]`（写在 csproj 里），各区包自己什么都不用写。通用包（建筑、地面、植被、水、家具、碰撞、外围、室内生成器等）不标归属，每张图都跑，只读当前图的数据（§7）。
3. **新增 Kernel 服务 `IMapService`**（WP-46）。切图在数据层完成，不用 `ChangeSceneTo*`：它会立即删掉当前场景，而本作的静态内容本来就不在场景树里（经 RenderingServer 挂载，ENG-PERF §1.2）。
4. **只用一个 World3D，先卸后装**：雾幕 → 卸旧图 → 装新图 → 出生 → 雾幕散，共 8 步（§4）。内存峰值等于"一张图 + 全局常驻"。两张图同时驻留太占内存。每图一个 World3D 的写法留到以后要做无缝切换时（§14）。
5. **世界状态是混合方案**：`WorldHours`、季节、`AnimTime` 全局共享，切图时世界时间照常向前；天气按图各自一份前向过程，进图时把队列补算到当前世界时间，补算是微秒级（SYS-WEATHER §6.5），不在后台模拟没去的图（§5）。
6. **地图服务这一版就交付**：只有一张图时，`Travel` 到当前图就是"同图重载"。用它跑泄漏门禁（§13），把"以为只有一张图"的隐藏写法提前逼出来。
7. **几个新登记**：帧顺序 `FramePriority.Map = −1300`，与 ENG-ARCH §3 的 17 步表统一（§9）；全局参数 `map_rect`，清水河取全图数据纹理的覆盖范围 ±1280 m（§10）；命令行 `--map`、`--spawn`、`--gate=maptravel`（§11）；单图半径不超过 8 km（§12）。

### 0.1 接入要点

- **读本图的数据文件**：路径一律由 `IMapContext.DataRoot` 拼出，启动集产物用 `BakeRoot`；内核、系统、界面和通用程序集里不写地图 ID、`res://data/maps/…` 路径和完整实体 ID 的字面量（G4，§7、§8）
- **要给每张图加数据或资源**：数据放 `game/data/maps/<地图>/` 下本包自己的文件或子目录，资源放各资源目录的 `<地图>/` 子目录，归属不变；`map.json` 归 WP-46，字段增改须经主代理批准（§2.1、§3）
- **写内容模块、室内提供者或区内部件**：区代码靠所在程序集的 `MapScope` 归属（WP-00 写进 csproj），各区包自己不写；通用模块只读 `BuildContext.Map` 和本图数据，不认领具体实体，出现 `FeatureClaim` 由 G5 报错（§7）
- **全局常驻的 `[KernelNode]` 用到按图的数据**：实现 `IMapAware`，在 `OnMapUnloading` 释放本图的实例、纹理内容和缓存，在 `OnMapLoaded` 按新图重建；不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容，G16 的同图重载用弱引用查这一条（§4.3、§8、§13）
- **节点只属于某张图**：`[KernelNode]` 写 `Map`，Mount 时实例化、Unload 时释放（§7）
- **写全图数据纹理或采样它**：一律按全局参数 `map_rect` 换算（着色器用 `map_uv()`、`in_map()`，范围外取"无数据"的默认值），不按"全校"隐式换算；C# 侧栅格从 `MapDescriptor.DataRect` 换算；纹理在 `OnMapLoaded` 里重建（§10）
- **设置罩住整张图的 `CustomAabb`**：切图时重设，罩箱庭内容的用 `MapDescriptor.Bounds`，跟相机走的用 `Center` ± `Roam.MaxRadius`（§3、§10）
- **在切图途中被调用**：`IMapService.Context` 在 Unload 到 Mount 之间为 null，要判空；`Current`、`Context` 在 `FramePriority.Map`（−1300）那一步换，同一帧里各系统看到同一张图（§8、§9）
- **同图内移动**（点位跳转、传送、回位）：交给 `ICameraRig.Transit` 和 `IAtlasService`，不调 `IMapService.Travel`；只有目标在别的图时才 `Travel`（§4.3）
- **依赖时间或天气**：切图时 `WorldHours`、季节照常向前，`IWorldState.Instance` 不变，不要把切图当成换实例；天气按图各一份，进图时补算，不在后台模拟没去的图（§5）

## 2 数据模型与目录

### 2.1 目录

```
game/data/
├─ maps/
│  ├─ index.json                         WP-46   地图清单、默认地图
│  └─ qingshuihe/                        清水河（当前唯一的图）
│     ├─ map.json                        WP-46   MapDescriptor 的手写部分（§3）；字段增改须经主代理批准
│     ├─ campus.json  network.json  zones.json          WP-08
│     ├─ overlay/<组>.d/                 各区包与公共空间、滨水、校界等包，每包一个分片文件；含 design: 增补与 hide 删除，每条都写理由（ENG-ARCH §3 覆盖层的规则）；旧 <组>.overlay.json 只留空壳
│     ├─ overlay/walkable.overlay.json   WP-44
│     ├─ overlay/outskirts.overlay.json  WP-47（WLD-OUTSKIRTS）
│     ├─ overlay/atlas.d/                WP-48c（SYS-ATLAS）锚点实体
│     ├─ overlay/interior/<区>.interior.json   各区精修室内的包（WLD-INTERIORS）：配置型室内的楼型配置
│     ├─ overlay/terrain/<区>.terrain.json     地形覆盖层，一个文件只归一个包（WLD-TERRAIN §3.1；清水河的文件与包的对应见 MAP-QSH-TERRAIN）
│     ├─ outskirts.json                  WP-47（WLD-OUTSKIRTS）
│     ├─ interiors.json                  主代理  开放室内清单（WLD-INTERIORS）：schema 与格式归 WP-51，条目由负责包填写；以后加楼只在这里加一项，清单的增删由主代理审批
│     ├─ waypoints.json                  WP-36   点位 P00 起，室内新增 P16–P23（UI-NAV §6、WLD-INTERIORS）
│     ├─ lighting-schedule.json          WP-39
│     ├─ incidents.json                  WP-26   偶发事件清单（SYS-MOTION §10、ENG-ROADMAP §6 第 5 条）：事件 ID、路线实体、选树与选灯的规则引用、各版本开关；schema 同包；WP-39 的灯光表烘焙读它
│     ├─ backdrop.json                   WP-22   截面、雾海、远景环、雪山的参数（WLD-DIORAMA；生成器代码在 `game/gen/diorama`）
│     ├─ sky.json                        WP-40   与地理有关的天空参数（城市天光的方位分布、霾带基线等，字段由 WP-40 定）
│     ├─ review.json                     WP-27   本图的审查视角 V、E 组与路线 F、W 组（ENG-GATES §1）；P 组直接取 waypoints.json
│     ├─ cinematics/<id>.json            WP-49a（SYS-TITLE）
│     ├─ atlas/…                         WP-48（SYS-ATLAS）markers.json（48c）、labels.json（48a）等；不得用 map.json 这个文件名
│     ├─ audio/…                         WP-53b、53c（SYS-AUDIO）本图的手工发声器、配方覆盖、远景声方位组、声学覆盖层，按文件分
│     ├─ entities/                       WP-00   RPG 预留（ENG-ROADMAP §6 第 6 条）：观光期不写入
│     └─ nav/…                           WP-44（WLD-INTERIORS）路线 routes.json 与观景点 views.json 的手写数据
├─ weather/<剧本>/                       WP-45a  全局剧本库；清水河引用 "chengdu"
├─ audio/                                WP-53   全局音频库（SYS-AUDIO）：声音集、配方、观者声样本组、界面音、音乐、许可清单
├─ rulesets/                             WP-00   规则集（ENG-ROADMAP §6 第 1 条）：sightseeing.json；schema 在 game/contracts/schemas/
├─ text/                                 WP-00   RPG 预留（ENG-ROADMAP §6 第 6 条）：文本库，观光期不写入
└─ quality-presets.json                  WP-32   全局
```

- **按图的资源**放在各自资源目录下的 `<地图>/` 子目录，归属不变：远景卡片 `game/assets/style/backdrop/qingshuihe/`（WP-22 向 WP-04 申请的子目录），扉页插画 `game/assets/title/qingshuihe/`（SYS-TITLE），平面地图的贴图按 SYS-ATLAS的目录，同样分到 `qingshuihe/` 下。
- **覆盖层**：通用覆盖层按包分片放在 `overlay/<组>.d/`，旧的 `<组>.overlay.json` 只留空壳；分片的命名与加载规则、`hidePart`、覆盖层新增步道进有效路网的规则都在 ENG-ARCH §3 覆盖层的规则。覆盖层里删原有的路只能精确 `hide` 无歧义的纯 road；与原路重叠的片段，先 `hide` 整条再 `add` 保留的各段和新连接，并迁移地形覆盖层对它的引用，不用 `sourceRoad` 隐藏原车行道。
- **管线**：`pipeline/` 的脚本都接受 `--map=<id>`，原始数据放 `pipeline/raw/<地图>/`，输出写到 `game/data/maps/<地图>/`。
- **实体 ID**：`campus.json` 里实体 ID 的前缀 `qsh:` 是 OSM 管线的既有写法，属于数据，不是地图 ID，保持不动。实体 ID 只在本图内唯一；跨图引用一律写成"地图 + ID"。以后的图换用自己的前缀，G5 检查各图数据的 ID 前缀不冲突。
- **精选室内属于所在的图**，不是单独的图。WLD-INTERIORS的 15 处开放室内都在 `qingshuihe` 里，随本图的流式加载进出（离入口 25 m 加载，WLD-INTERIORS）。

`index.json` 的写法：

```json
{ "version": 1, "defaultMap": "qingshuihe",
  "maps": [ { "id": "qingshuihe", "dir": "res://data/maps/qingshuihe/" } ] }
```

### 2.2 按图与全局的划分

| 按图（每张图一份） | 全局（所有图共用） |
| --- | --- |
| 原点与投影、校界、活动范围、全图数据范围 `map_rect` | 着色器白名单、全局参数名、材质库与色名、调色板关键帧（美术方向） |
| 世界模型（`campus.json`、`network.json`、`zones.json`）、覆盖层（含 `design:` 增补与 `hide` 删除）、外围数据 | 树种表、部件编目、`ToonSpec` |
| 开放室内清单 `interiors.json` 与各室内提供者；偶发事件清单 `incidents.json`（SYS-MOTION §10） | 室内平面生成器与类型库（WLD-INTERIORS）；规则集 `game/data/rulesets/`（ENG-ROADMAP §6 第 1 条） |
| 点位、出生点、审查视角与路线 | 画面设置、按键、界面设置、声音设置（`user://` 的四个 cfg：`settings`、`input`、`graphics`、`audio`） |
| 可行走遮罩、2 m 分类栅格、8 m 安全点网格、碰撞格、导航图 | 界面主题与组件（UI-VISUAL）、音频库（SYS-AUDIO：声音集、观者声样本组、界面音、音乐、声学预设） |
| 本图的声景数据：手工发声器、配方覆盖、远景方位组、声学覆盖层（`audio/`），以及烘焙产物声景场 `ambience.bake` 与声学网格（SYS-AUDIO） | 混音总线与五组音量（SYS-AUDIO） |
| TileBundle（L0–L4）、L4、遮挡体 | `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` |
| 灯光时刻表、灯表、窗覆盖、地面遮罩 1 与 2、`precip_occ`、`terrain_height` | 天气剧本库 `game/data/weather/<剧本>/` |
| 远景（`backdrop.json`）、天空档案（`sky.json`）、做倒影的湖 | 天气基础种子（每个存档一个，SYS-SAVE） |
| 天气过程的状态、默认预设、本图种子 | 存档容器与槽位（SYS-SAVE） |
| 平面地图的底图、标记、文字（SYS-ATLAS）；过场数据（SYS-TITLE） | `IAppFlow` 状态机（SYS-TITLE） |
| 预算 `budgets.json` 的 `maps.<id>` 一节（§12） | 进程内存与显存的总上限（ENG-BUDGET §2） |

### 2.4 本机烘焙缓存与启动集 PCK

- **本机烘焙**（D-165）：四类产物不随安装包发：L0–L3 的 TileBundle（含外围 O0、O1 和 I0 兜底层）、碰撞级 C、开放室内与简化室内的室内包和碰撞包、平面地图底图。它们在玩家机器上由与开发烘焙器同一个 `Build()` 生成，第一次需要时生成，写进本机缓存，之后照常从缓存读。查找顺序：启动集 → 本机缓存 → 本机烘焙（生成后写进缓存）。何时烘、后台预烘、按需生成与本机缓存的管理见 ENG-PERF §5、ENG-ARCH §3（`IBakeService`）。
- **缓存键**：`ABI/地图/数据哈希/模块@代码哈希/级别/tileId`（ENG-PERF §5 的键在 ABI 之后加地图）。本机缓存目录 `user://cache/bake/<地图>/<ABI>/…`（导出版在 `%APPDATA%` 下）；开发和门禁用 `--bake-cache=<绝对路径>` 指到共享缓存（默认 `UESTC_BAKE_CACHE`）。各图互不覆盖。
- **数据哈希只算本图里影响 `Build()` 的输入**：`campus.json`、`network.json`、`zones.json`、全部覆盖层、`interiors.json`、`outskirts.json`、`backdrop.json`，以及 `map.json` 的 `seed`、`dataRect`。改出生点、默认状态、过场、点位不重烘。
- **烘焙命令**：`--bake=all --map=<id>` 是开发和 CI 的无界面烘焙器，用来出参考哈希、给带 `--bake-strict=1` 的检查补共享缓存；`--map=*` 依次烘全部图。`gate.ps1` 先按改动选出本次要跑的检查，再决定要不要准备烘焙数据：快检不烘，不带 `--bake-strict=1` 的检查缺什么按玩家路径现烘，带它的检查只把用到的图按哈希增量补进共享缓存；`--map=*` 只在集中测试或所选检查确实要全部图时跑（ENG-ARCH §6）。发布版运行时的本机烘焙由 `IBakeService` 调用同一个 `Build()`。
- **启动集**：每张图一个 `boot_<地图>.pck`，随安装包发，包内路径 `res://bake/<地图>/boot/`。内容是 L4、遮挡体、外围 O2 常驻块与卡片版，以及需要 GPU 才能出的产物（L2 半八面体替身图集，如果 P0 选了它）；另有**全图常驻的小数据**：派生数据纹理（灯表、`precip_occ` 等）、2 m 分类栅格与 8 m 安全点网格、声学网格、声景场与发声器表 `ambience.bake`（SYS-AUDIO）、导航图（WLD-INTERIORS）；以及参考哈希清单 `bake.manifest.json`（`res://bake/<地图>/boot/bake.manifest.json`）。每图 ≤ 60 MB，其中全图常驻小数据约 10–15 MB。这些小数据体积小，又是启动和第一次切到行走时就要的，所以随包发，不在本机烘焙；它们同样由开发烘焙器出（离线烘焙），随启动集挂载，LoadData 之后就能读。
- **本机烘焙产物的位置**（按各自的键进缓存，同样在 ABI 之后加地图）：L0–L3 的 TileBundle，碰撞级 C，平面地图的底图与标注 `map/`（SYS-ATLAS），开放室内与简化室内的按层包和碰撞包 `interior/`（WLD-INTERIORS）。开放室内不另设 PCK。
- **挂载**：某张图第一次进入时，在 LoadData 那一步 `ProjectSettings.LoadResourcePack(path, replaceFiles: false)` 挂上 `boot_<地图>.pck`，之后不卸载（Godot 没有卸载接口）。挂载只读目录表，常驻开销按很小计（§19）。G6 检查各 PCK 的路径集合两两不相交，否则同名文件会互相覆盖。
- **本机缓存与画面设置、世界状态、存档无关**：`Build()` 的输入里没有这些（CORE-CONVENTIONS §6），所以缓存跨存档、跨画质共用；`--seed` 覆盖时种子进数据哈希，各自一份。它不是存档（SYS-SAVE）。
- 主 PCK 只留全局资源和各图的 JSON（`res://data/maps/*/`）。导出过滤器包含 `data/maps/**/*.json`。
- **失效粒度**（先求简单正确，细则见 ENG-ARCH §4"失效"）：
  - **数据变化整图失效**：数据哈希每图一个。上面任何一份输入变了（哪怕只改一条覆盖层），本图的全部本机烘焙产物一起换键，之后按需和后台预烘重新生成；不按模块细分数据输入。
  - **代码指纹含共享生成依赖**：`模块@代码哈希` 的代码哈希把这个模块传递引用的共享生成代码（契约、部件内核与各部件包、室内共享层、世界模型；底图另含 `Campus.Pipeline.Map`）一起算进去。改了共享代码，所有引用它的模块一起重烘；没有引用它的模块不动。
  - 只有实测发现整图失效明显拖慢制作时，才把数据输入细分到模块，并由 G2 证明依赖没有漏算；不做逐实体的增量构建。
  - 平面地图底图与标注也按这一条：底图的键含世界模型哈希（SYS-ATLAS §2.1），标注布局另含自己的输入（SYS-ATLAS §2.5）。

## 3 `map.json`：`MapDescriptor`

`map.json` 只写手写的部分；`MapDescriptor` 里另有几项由加载器派生，不手写（下表"来源"一列）。

| 字段 | 清水河的值 | 来源与说明 |
| --- | --- | --- |
| `id` | `qingshuihe` | 小写字母、数字、连字符，不超过 32 字符；与目录名相同 |
| `name` | 清水河校区 / Qingshuihe Campus | 到达标题、平面地图、存档卡用；作品名《电光年华》（窗口标题，SYS-VIEW §6）不变 |
| `kind` | `Outdoor` | `Interior`、`Instance` 预留 |
| `version` | 1 | 本图数据的大版本。存档记下它；读档时版本不同，位姿要过一遍落地检查（SYS-SAVE） |
| `seed` | 1 | `BuildContext.WorldSeed`；`--seed` 可以覆盖（截图默认 1） |
| `dataRect` | x −1280…1280、z −1280…1280 | 全图数据纹理覆盖的范围，即 `map_rect`（§10）；边长是 64 m 的整数倍，必须盖住校界外接框和 O0 街道带（WLD-OUTSKIRTS的前沿线以内）。平面地图的页面范围 `atlasRect`（清水河 ±1536 m）不在这里，写在本图的 `atlas/markers.json`（SYS-ATLAS §2.3） |
| `roam` | 见下 | 旁观的活动范围；公式与前沿线数据见 WLD-OUTSKIRTS |
| `defaultSpawn` | `P00` | 默认出生点；出生点 ID 先查 `spawns`，再查本图的点位 |
| `spawns` | 空 | 点位以外另加的出生点 |
| `world` | 见下 | 世界规则 |
| `env` | 天空 `sky.json`、远景 `backdrop.json`、外围 `outskirts.json`、倒影湖 [西湖, 东湖] | 倒影湖写实体 ID，列表顺序就是湖号（全局参数 `reflect_lake`，ENG-PERF §1.5）；清水河列西湖和东湖两个，东湖映着宣邦楼的塔楼和钟楼，只靠解析天空画不出来（WLD-WATER §7.2）；没有外围数据的图 `outskirts` 为 null（WLD-OUTSKIRTS） |
| `title` | T0 位姿、云卡布局名 | 开始页的 T0 镜头（SYS-TITLE）；没写时开始页用默认图的 T0 |
| `cinematics` | 角色 → 过场 ID | 角色名以 SYS-TITLE为准：`enterNew`、`enterNewShort`、`enterContinue`、`leave`，都可缺省（缺省时走雾幕直达） |
| `portals` | 空 | 传送门，预留（§14） |
| `geo`（派生） | 经度 103.9282919、纬度 30.75316145，EPSG:32648，校界外接框 x −991…989、z −1020…1021 | 从 `campus.json` 的 `meta` 读，只有一个来源 |
| `center`（派生） | 校界形心，约 (50, 77) | 活动范围"离中心不超过 3 km"按它算（WLD-OUTSKIRTS） |
| `bounds`（派生） | 箱庭包围盒：x、z 取截面线的包围盒，约 x −1250…1260、z −1400…1440（由 `outskirts.json` 求出，WLD-OUTSKIRTS）；没有外围数据的图取校界外接框外扩 `fallbackMargin`；y 取 `roam.floorY`…`roam.ceilY` | 四叉树覆盖、坐标精度检查、罩住箱庭内容的 `CustomAabb`（飘落池） |
| `tileMin`、`tileMax`（派生） | 由烘焙统计得出 | 64 m 格的范围；整数 tileId 要求 \|ix\|、\|iz\| < 512 |

- **跟相机走的 MultiMesh**（降水池）：`CustomAabb` 不用 `bounds`，用可达范围 `center` ± `roam.maxRadius`（y 取 `floorY`…`ceilY`），由 `Center` 和 `Roam` 算出，不另设字段。镜头最远能飞到离中心 3 km，比箱庭包围盒大；用箱庭包围盒的话，镜头在箱庭外朝外看时整池会被剔除，雨雪凭空消失。

**`roam`**（数值取 WLD-OUTSKIRTS的大箱庭方案，本节只定字段）：高度系数 0.9、起算高度 30 m（水平范围 = 前沿线 + 0.9 × max(0, h − 30 m)）、离中心最远 3000 m、最低 −28 m、最高 2000 m、缓冲带 30 m、外围上空软地板净空 40 m；推回速率 1.5/s、推回上限 15 m/s、边缘雾上限 1.6 倍（WLD-OUTSKIRTS）；没有外围数据的图用"校界外扩 150 m + 0.9 × 高度"（`fallbackMargin = 150`）。WLD-OUTSKIRTS写的 HeightOffset、RoofClearance、Margin 就是这里的 `heightStart`、`softFloorClearance`、`fallbackMargin`。

**`world`**：

| 字段 | 清水河 | 说明 |
| --- | --- | --- |
| `time` | `Shared` | 跟随全局时间线；`Fixed` 预留 |
| `default` | 秋 17:30，晴 | 本图默认状态（`WorldPreset`）：新游戏交还控制时的状态（从 `newGameStart` 推到这里，SYS-TITLE）、"回到本图默认状态"的目标 |
| `defaultName` | 落金初灯 | 界面上"回到 ××"的名字。清水河上按钮叫"回到落金初灯" |
| `quietUntilDusk` | true | 默认状态的天气是"自动 + 保持晴到本季黄昏带结束"（SYS-WEATHER §6.6） |
| `weatherProfile` | `chengdu` | 天气剧本，读 `game/data/weather/chengdu/` |
| `sunProfile` | `default` | 美术日月轨迹（SYS-TIME §2） |
| `fixed` | 无 | `time = Fixed` 时进图钉住的状态（预留） |
| `newGameStart` | 秋 17:05、晴 | 新游戏的起点预设（`WorldPreset`，SYS-TITLE）：新游戏从这里建实例，再正向推到 `default`（SYS-SAVE）；没写时直接建在 `default` |

```json
{
  "id": "qingshuihe", "name": { "zh": "清水河校区", "en": "Qingshuihe Campus" },
  "kind": "outdoor", "version": 1, "seed": 1,
  "dataRect": { "min": [-1280, -1280], "max": [1280, 1280] },
  "roam": { "heightFactor": 0.9, "heightStart": 30, "maxRadius": 3000, "floorY": -28, "ceilY": 2000,
            "buffer": 30, "softFloorClearance": 40, "fallbackMargin": 150,
            "pushRate": 1.5, "pushMax": 15, "edgeFogMax": 1.6 },
  "defaultSpawn": "P00", "spawns": [],
  "world": { "time": "shared", "default": { "clock": 17.5, "season": "autumn", "weather": "clear" },
             "defaultName": { "zh": "落金初灯", "en": "（英文名由 WP-35 定）" },
             "quietUntilDusk": true, "weatherProfile": "chengdu", "sunProfile": "default",
             "newGameStart": { "clock": 17.083333, "season": "autumn", "weather": "clear" } },
  "env": { "sky": "sky.json", "backdrop": "backdrop.json", "outskirts": "outskirts.json",
           "reflectLakes": ["qsh:relation:7849759", "qsh:relation:7849760"] },
  "title": { "t0": "（T0 位姿由 WP-49b 定，约 (−1240, 495, 2928)，SYS-TITLE）", "cloudLayout": "qingshuihe" },
  "cinematics": { "enterNew": "enter_new", "enterNewShort": "enter_new_short",
                  "enterContinue": "enter_continue", "leave": "leave" },
  "portals": []
}
```

（`title`、`cinematics` 的写法与过场 ID 以 SYS-TITLE为准，这里只示意字段。）

- `map.json` 不写本图预算，也不写允许的季节：预算只记在 `budgets.json`（ENG-BUDGET §8 是唯一的账本，§12）；季节全局共享、任选（SYS-TIME §5），按图限制季节会和这一条冲突；以后要钉住季节的图用 `fixed`。
- G2 用 `game/contracts/schemas/map.schema.json` 校验每份 `map.json`；G5 检查 `defaultSpawn` 能解析、`dataRect` 盖住校界外接框、倒影湖的实体存在。

## 4 运行时与切换

### 4.1 一个 World3D，先卸后装

- 场景树里始终只有 `main.tscn` 的内核根节点（ENG-ARCH §2）。静态内容由内核经 RenderingServer 挂载，碰撞 body 由 `kernel/collision` 入树，都按图记账，所以"卸掉一张图"就是按账逐项释放，不需要换场景。
- 第一版只有一个主 World3D，加上倒影的独立 World3D（ENG-PERF §10.4）。两者里属于本图的东西都在 Unload 时清掉。
- 内存峰值 = 一张图 + 全局常驻（§12）。

### 4.2 切换的 8 步

在 `FramePriority.Map`（−1300）那一步推进（§9）。时长是开发机热启动的初值，P0 实测（§19）。

| 步 | 名称 | 时长 | 做什么 |
| --- | --- | --- | --- |
| 1 | VeilIn | 0.5 s | 雾幕盖满，沿用开场和跳转的那一层（SYS-TITLE §1、UI-NAV §9），原因写 `VeilReason.MapTravel`（SYS-AUDIO）。取得输入占用 `ICameraRig.LockInput(InputLockReason.MapTravel)`（ENG-ARCH §3）；取消正在进行的跳转和已知目的地预取（UI-NAV §7.5）；小地图淡出（SYS-ATLAS）；存档服务暂停拍快照（不是安全时刻，SYS-SAVE）；声音的 World 组 0.5 s 压到 −40 dB（SYS-AUDIO） |
| 2 | Snapshot | 1 帧 | 给存档的挂点：记下旧图的 `MapVisit`（最后位姿、观察模式、天气 epoch、离开时的世界时间）；`IWeatherProcess.Leave(旧图)` |
| 3 | Unload | ≤ 0.5 s | 流式（`Stream`）不再发新请求，在途的照常完成后 `Dispose()`，挂载（`StreamMount`）不再挂本图的格子；本图所有实例 `FreeRid`，碰撞 body 出树，释放 L4、遮挡体和倒影代理；全图数据纹理交还各写入方；各 `IMapAware` 节点按 ID 顺序收到 `OnMapUnloading`（音频节点在这里丢掉本图的发声器、声景场、声学网格和房间，SYS-AUDIO）；带 `Map` 的 `[KernelNode]` 释放；`Context` 置空；最后 `GC.Collect()` 一次（雾幕里允许，期间 GC 延迟模式临时改回 Interactive，ENG-PERF §6） |
| 4 | LoadData | ≤ 1 s | 首次进图时挂上 `boot_<地图>.pck`（启动集，§2.4）；内核读字节、工作线程解析新图数据、建世界模型（ENG-ARCH §2"纯 .NET 代码不碰文件路径"）；读本图的常驻数据（2 m 分类栅格、8 m 安全点网格，都在启动集里，§2.4）；世界模型建好地形评估器，地形高度此后按 `ITerrain.HeightAt` 求值（WLD-TERRAIN §9.4）；注册器按"通用 + 本图"重新筛选模块（§7）；`IWeatherProcess.Enter(新图)`，补算队列（§5） |
| 5 | Mount | ≤ 1.5 s | 同步加载 L4 和遮挡体（ENG-PERF §5 第 1 条）；写全局参数 `map_rect`（§10）；各写入方重建全图数据纹理并重新设全局参数；实例化本图的 `[KernelNode]`；各 `IMapAware` 节点按 ID 顺序收到 `OnMapLoaded`（`IAtlasService` 换底图与标记，SYS-ATLAS；音频节点读本图的 `audio/` 和烘焙产物，SYS-AUDIO）；材质库按需重新分配材质 ID（风险 5）；按已知目的地预取出生点周围的 L0+–L2 各环和室内提供者，行走模式另加碰撞格 |
| 6 | Warm | 同图 0；新图 ≤ 2 s | 只预热本图新出现的组合，等 SURFACE、DRAW、Canvas 三项管线编译计数连续 30 帧不再增长（ENG-PERF §2）；本图节点空跑 30 帧做 JIT 预热。G16 对每张图都成立 |
| 7 | Spawn | ≤ 1.5 s | 设位姿（§6），按 WLD-OUTSKIRTS的可达空间合法化；行走模式等 `ICollisionWorld.Ready()`（SYS-WALK §14 第 5 条），出生点在开放室内时等这一层就绪（ENG-PERF §5 第 6 条，WLD-INTERIORS §6.4）；清相机插值缓冲，重判介质；`Current` 换成新图，`Rev` + 1 |
| 8 | VeilOut | 2–3 s | 雾幕散开，同 SYS-TITLE §1 第 3 步；环境声 2–3 s 淡入（SYS-AUDIO）；跨图到达时显示一次到达标题，写图名（UI-NAV §4.8）。之后释放第 1 步取得的输入占用，`Done` 返回 `Arrived`，请求一次存档（`MapChanged`，SYS-SAVE）；`Cause` 为 `Boot`、`LoadSave` 时不请求 |

- **热切图目标**：VeilIn 开始到 VeilOut 开始不超过 5 s（§19）；冷启动按 ENG-PERF §8 的目标。上表和这条目标按出生点周围已在本机缓存里计。
- **起始点周围不在缓存里时**（D-165）：第 4 步挂上 `boot_<地图>.pck` 之后，第 5 步预取清单里没有缓存的格（出生点周围的 L0+–L2 各环、室内提供者，行走模式另加碰撞格），在雾幕下本机烘焙（ENG-PERF §5）；雾幕保持不透明，烘完、Spawn 等到就绪才散，不受上表时长限制。只要烘焙还有进展就一直等；预取清单里有键进入 `BakeState.Failed`，或连续 60 s 没有进展（ENG-PERF §5"损坏、校验失败与终态"），这次切换按 §4.3 的 Failed 处理。
- **Boot**：窗口启动时就是雾色（SYS-TITLE §1），开始页时是扉页插画（SYS-TITLE），所以第 1–3 步跳过，从 LoadData 起；LoadData–Warm 都在扉页后面完成（SYS-TITLE的启动序列；首次运行时包括起始点周围的本机烘焙，进度行见 SYS-TITLE §1），VeilOut 由 `IAppFlow` 的转为实时代替。
- **雾幕契约的两项增量**（SYS-TITLE的协调项，WP-01 的雾幕服务）：雾幕内容除了雾色，还可以是 SYS-TITLE的"扉页"；雾幕可以"保持不透明直到条件满足"，切图用它等 Warm、Spawn 完成，冷启动用它等开始页就绪。
- **雾幕之下世界照常运行**：天空、天气、风都在走，只是看不见；全局节点不重建。

### 4.3 规则

- **不可取消**：请求一经接受就走完；`IMapTransition` 不提供取消。
- **只留最后一次**：途中再来请求，只保留最后一次，当前这次走到 Spawn 后不散雾幕，直接从下一次的 Snapshot 接着走。目标与正在进行的相同就合并。
- **Rejected**：未知的图；`time = Fixed` 等尚未实现的规则（§14）；`Kind` 不是 `Outdoor`。
- **Failed**：加载出错（含 §4.2 的本机烘焙失败或停滞）时留在雾幕里，退回上一张图；上一张也失败就进默认图；都失败时停止等待，在雾幕之上显示 SYS-TITLE §1.1 的失败页（重试 / 复制错误信息 / 退出），不再无限重试：
  - 重试：调 `IBakeService.ClearFailures()`，重新走一次原请求；
  - 退出：经 `IAppFlow.Quit()` 退出，不写新的一代存档，换图之前的那一代仍有效（SYS-SAVE §4.4 第 2 步），也不出保存失败的三选一；
  - `Cause` 为 `Boot` 时，这一页就是启动失败页本身；`--title=0` 与门禁下不出页，写日志后以非零退出码结束。
- **全局节点不重建**：天空、植被系统、水、灯光、天气、界面、平面地图（`IAtlasService` 实现 `IMapAware`，切图时换底图和标记，SYS-ATLAS）、音频（SYS-AUDIO）等 `[KernelNode]` 跨图常驻，只靠 `IMapAware` 换数据。它们不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容；G16 的同图重载用弱引用查这一条（§13）。
- **同图内的移动不走地图服务**：点位跳转、传送（SYS-ATLAS 的雾幕瞬移）、回位都只在当前图里，交给 `ICameraRig.Transit` 和 `IAtlasService`。`IAtlasService` 只在目标标记属于别的图时才调 `IMapService.Travel`。

### 4.4 各入口

| 入口 | 原因 | 怎么走 |
| --- | --- | --- |
| 冷启动 | `Boot` | 起始图 = 命令行 `--map` > 最近存档的图 > `index.json` 的默认图。在扉页插画后面完成 LoadData–Warm（SYS-TITLE）；开始页的 T0 镜头就在这张图上 |
| 新游戏 | `NewGame` | 起始图是默认图。当前已经是这张图就不切图，只按 SYS-SAVE重置世界（`IWorldState.Instance` + 1，从本图的 `newGameStart` 建实例），由 SYS-TITLE的过场接上 |
| 继续 / 读档 | `LoadSave` | SYS-SAVE的读档顺序：先恢复世界段和天气段（全局时间、各图的天气状态），再 `Travel(LoadSave)`，位姿和模式与存档的视角段相同（取这张图的 `MapVisit`，保存时两者一致），最后恢复其余段；目标图与当前相同就不切图。开始页的预览读档不给位姿，镜头留在 T0（SYS-TITLE）。读档是换一个世界实例，切图本身不改 `Instance`，到达后也不请求保存 |
| 平面地图传送 | `Teleport` | 只有目标标记在别的图时才用（SYS-ATLAS）；同图传送不经本服务 |
| 传送门 | `Portal` | 预留 |
| 界面 | `Ui` | 预留给以后的选图页 |
| 门禁 | `Test` | `--gate=maptravel`（§13） |

## 5 世界状态：时间与季节全局，天气按图

| 量 | 范围 | 切图时 |
| --- | --- | --- |
| `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` | 全局，一条时间线 | 不动，照常向前。雾幕几秒钟里世界时间也在走（1× 下只差几秒）；"世界时间单调向前"在切图时照样成立，存档只记一份时间 |
| `IWorldState.Instance`（SYS-SAVE） | 全局 | 不变。只有读档和新游戏才 + 1 |
| 天气 | 按图各一份前向过程 | 离开时冻结在离开那一刻；进图时补算到当前世界时间 |
| 灯光时刻表、窗覆盖、灯表 | 按图 | WP-39、WP-03 经 `IMapAware` 换成新图的 |
| 日月轨迹、调色板关键帧、`ui_night` | 全局 | 不动 |

**天气按图**（WP-45a）：

- 每张去过的图各存一份天气状态：种子派生的子流、epoch、片段号、队列、自动或手动、钉住窗口、湿润与积水余量。存档里按图各存一份（SYS-SAVE）。
- 子流是 `StableHash(种子, mapId, epoch, 片段号, 尝试号, 用途)`；种子每个存档一个（SYS-SAVE、SYS-WEATHER §6.4）。
- **进图补算**：从离开时的世界时间前向生成到现在，规则同 SYS-WEATHER §6.5 的快进，微秒级；离开超过 72 h 时，epoch + 1、从现在起重排，不去生成很长的历史。季节在离开期间变了，进图时先按 SYS-WEATHER §6.5 的换季映射 `Reseason`，再补算。补算和映射都在雾幕里完成，画面直接落定，不做过渡。
- **第一次进某张图**：自动天气、晴、epoch 0，从现在起生成。新游戏的起始图从本图的 `newGameStart` 建（`quietUntilDusk` 为真时同样保持晴到黄昏带结束）。
- 手动选的天气属于当前图的过程；去别的图再回来，手动选择还在。
- 降水池、湿润、积水的全局参数跟着当前图的天气向量走，切图那一帧在雾幕里直接写新值。

**默认状态与"回到落金初灯"**：

- `IWorldControl.ResetDefault()` 的语义是"回到本图默认状态"：读当前图的 `world.default`，执行正向换季、快进到下一次出现的钟点、流逝 1×、自动天气；`quietUntilDusk` 为真时保持晴到本季黄昏带结束；不移动镜头（SYS-TIME §1、SYS-WEATHER §6.6）。
- 界面上的按钮名取 `world.defaultName`：清水河上是"回到落金初灯"，其他图上是"回到本图默认状态"或该图自己的名字。
- 新游戏从起始图的 `newGameStart` 建实例（清水河：秋 17:05、晴），由 SYS-TITLE的过场以 `FlowTo` 正向推到默认状态，交还时就是落金初灯（SYS-SAVE）。

## 6 相机、行走与安全点

- **全局不变的**：16 档速度、视场角、灵敏度、改键、冲刺方式都是设置，切图不变。
- **位姿**：请求里的 `Pose` > 请求里的出生点 > 本图的 `MapVisit.LastPose`（本次运行记下的或存档里的）> `defaultSpawn`。`--pose` 给出时优先于 `--spawn`。
- **观察模式**：请求给了模式就用它，否则保持当前模式。目标出生点没有行走落点 `Stand` 时自动切到旁观，提示一次，同 UI-NAV §6 的"仅旁观"点位。
- **行走模式**：Spawn 时不开物理，等角色所在格和 8 个邻格的碰撞就绪（SYS-WALK §14 第 5 条），交接时清相机插值缓冲（SYS-WALK §12）。切图的雾幕可以多停，不设 1.5 s 的上限；超过 5 s 写日志。
- **活动范围**：读新图的 `roam`；软约束的推回状态在 Spawn 时清零。
- **安全点**：16 槽环形缓冲在 Unload 时清空，不把旧图的点带进新图；8 m 安全点网格和 2 m 分类栅格按图，在 LoadData 时换成新图的（SYS-WALK §9）。切图途中回位（长按 R）不响应。
- **介质**：Spawn 后重判，介质过渡在雾幕里直接落定（SYS-VIEW §5）。
- **跳转**：VeilIn 取消进行中的 `Transit`；`Transit` 不跨图。

## 7 自动发现与 `MapScope`

**归属的求法**：注册器启动时照常扫描全部程序集（ENG-ARCH §2），给每个模块求出所属的图：类上的 `Map` → 程序集的 `MapScope` → 都没有就是通用模块。每次 LoadData 只启用"通用 + 当前图"的模块。

| 特性 / 数据 | 归属 | 切图时 |
| --- | --- | --- |
| `[ContentModule]`、`[InteriorProvider]` | 按上面的求法 | 只有通用和本图的参与 `Tiles()`、`Build()`、`Collide()` |
| `[KernelNode]` 带 `Map` | 本图 | Mount 时实例化，Unload 时释放 |
| `[KernelNode]` 不带 `Map` | 全局 | 常驻；需要换数据的实现 `IMapAware` |
| `[KernelService]`、`[MediumPresenter]`、`[Part]`、`[QualityResponsive]` | 全局 | 不变（区内私有部件随所在程序集归本图） |
| `res://data/maps/<地图>/overlay/<组>.d/*.json`、`overlay/*.overlay.json` | 本图 | WP-08 只扫描当前图的目录 |
| `res://style/palette/ext/*.json` | 全局 | 不变 |

- **换图数据的生命周期**：生产本图数据的实例（烘焙服务等）实现 `IMapDataParticipant`，与 `IMapAware` 按下面的顺序配合，消费方只管 `IMapAware`。
  - **装**：Mount 先实例化本图的 `[KernelNode]` 并全部入树；再按顺序 `await` 每个 `IMapDataParticipant.Prepare(ctx)`；全部完成后，才给各 `IMapAware` 分发 `OnMapLoaded`。分发前 `Prepare` 没有等完，消费方就读不到完整数据，不允许跳过。
  - **卸**：先等在途的 `Prepare` 结束，免得迟到的 CPU 结果把已卸的图重新发布；再给各 `IMapAware` 分发 `OnMapUnloading`；全部消费者释放后才调 `IMapDataParticipant.Unload(from)`；最后释放本图的 `[KernelNode]`。
  - **先卸后装**：已挂着一张图、正在卸载或 `Prepare` 未完成时再 Mount 一律报错；同一个 `MapId` 换一个上下文也要先卸载。正在卸载的旧图留下的句柄保持 `Failed`，不当成空成功。
  - 烘焙产物的 `Ready` 只代表该产物通过了生产解码；挂载与目的地是否就绪由启动流程判定，后台全图预烘的进度不作为进图的前置。
- **带 `MapScope("qingshuihe")` 的程序集**：`Campus.Gen.Zones.*`，即 core、library-interior、south、west、north、center-east、design-waterfront、design-public、design-boundary、showcase-interiors。WP-00 在 D0 写进这些 csproj 的 `<AssemblyAttribute>`，各区包不写任何东西。
- **通用程序集**：buildings、ground、diorama、vegetation、water、fauna、traces、furniture、medium、walkable、collision、外围（WP-47）、室内生成器（WP-51）、室内家具（WP-52）和全部部件。它们只读 `BuildContext.Map` 和本图数据，不认识任何具体的图。
- **以后的图**：各区代码新建程序集，标上自己的 `MapScope`；目录怎么排（例如 `game/gen/zones/<图>-<区>/`）加第二张图时再定（§19），现在不动。
- **G5**：`--gate=registry --map=*` 对每张图各查一遍：ID 不重复、认领不冲突、`MapScope` 指向 `index.json` 里存在的图。**通用模块里出现 `FeatureClaim`（认领具体实体）直接报错**：认领了具体实体就说明它属于某张图。
- **ArchUnitNET**（`scripts/gates/arch/`，WP-00）：
  - 归属不同图的程序集互不引用；通用程序集不得引用任何带 `MapScope` 的程序集（csproj 已经挡住，再查一遍）。
  - **G4 正则**：通用程序集、内核、系统、界面代码里不得出现完整的实体 ID 字面量（形如 `"<前缀>:<类型>:<编号>"` 或 `"design:…"`、`"walk:…"`），也不得出现地图 ID 字面量和 `res://data/maps/…` 路径字面量。数据路径一律由 `IMapContext.DataRoot` 拼出。例外只有 `kernel/map`（读 `index.json`）和门禁的夹具。

## 8 C# 契约

照 ENG-ARCH §3 的写法，只增不改。`MapId` 放在基础类型那一组（与 `FeatureId` 同处），SYS-ATLAS、SYS-SAVE引用同一个定义，不各自另定。

> 契约类型与签名以 `game/contracts/Maps/`为准。

## 9 帧顺序：`FramePriority.Map = −1300`

与 ENG-ARCH §3 的 `FramePriority` 17 步表一致。任何两步都不得同值。

| `ProcessPriority` | 常量 | 工作包 | 职责；切图期间怎样 |
| --- | --- | --- | --- |
| **−1300** | **`Map`** | **WP-46** | 推进切换状态机；在雾幕全黑的帧里分发 `IMapAware`；Mount 时写 `map_rect`；`Current`、`Context` 在这一步换，同一帧里所有系统看到同一张图 |
| −1200 | `Quality` | WP-32 | 自适应画质（SYS-GRAPHICS §8）；切图进行中 `Apply` 返回 `Deferred` |
| −1100 | `Camera` | WP-06（含可达空间求值、写 `edge_fog`，WLD-OUTSKIRTS）、WP-36、WP-42；SYS-TITLE的 `CinematicPlayer`（WP-49a） | 切图期间观者输入被 `InputLockReason.MapTravel` 占用（§4.2） |
| −1000 | `WorldState` | WP-34 | 世界时间照常向前（§5） |
| −900 | `KeyLight` | WP-03 | 照常 |
| −800 | `Palette` | WP-37、WP-04 | 照常 |
| −700 | `Channels` | WP-03、WP-39 | 照常 |
| −650 | `Motion` | WP-26、WP-45 | 照常 |
| −640 | `SeasonFx` | WP-38 | 照常 |
| −600 | `Medium` | WP-07 | Spawn 后重判介质 |
| −590 | `Acoustics` | WP-53c（SYS-AUDIO） | 本帧听者处的声学与路由；新图的声学网格在 `OnMapLoaded` 之后才生效 |
| −500 | `Stream` | WP-02、WP-43 | 只做选级与请求；切图期间（Unload 到 Mount）不发新请求 |
| 0 | `Systems` | 各包（含 WP-48c 的 `IAtlasService`） | 照常 |
| 50 | `Audio` | WP-53a、53b、53d（SYS-AUDIO） | 雾幕盖起时 World 组压低（§4.2） |
| 100 | `Hud` | WP-35；WP-48b 的小地图与大地图 | 切图期间小地图淡出 |
| 900 | `StreamMount` | WP-02、WP-43 | 挂载与碰撞入树，预算内分帧；Unload 之后不再挂旧图的格子 |
| 1000 | `Save` | WP-50（SYS-SAVE） | 每帧最后一步；切图期间不拍快照 |

- 排在画质之前：切图途中，画面设置的雾幕类改动并进切图的雾幕一起生效，即时类照常；`IQualityService.Apply` 在切图进行中返回 `Deferred`（同 SYS-GRAPHICS §4 对跳转、换季的处理）。
- 平时这一步只读一个状态，几乎不花时间。

## 10 全局参数 `map_rect`

登记进 ENG-PERF §1.5：

| 名称 | 类型 | 内容 | 写入方 | 更新 |
| --- | --- | --- | --- | --- |
| `map_rect` | vec4 | 全图数据纹理覆盖的 xz 范围：(x0, z0, 1/宽, 1/深)，uv = (p.xz − xy) × zw | WP-46 | 切图的 Mount 那一步 |

- **用它的纹理**：`ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid`，以及以后任何"全图"纹理。这些纹理共用同一个 `map_rect`，各自的分辨率可以不同。不按"全校"隐式换算：那样换一张尺寸不同的图就会错位，这是最典型的"以为只有一张图"。
- **范围外**：`globals.gdshaderinc` 里提供 `map_uv()` 和 `in_map()` 两个函数（WP-01 生成）；在范围外一律取"无数据"的默认值：遮罩为 0、`precip_occ` 视为没有顶面、地形高度取 0。
- **清水河**：x、z 都是 −1280…1280，即 (−1280, −1280, 1/2560, 1/2560)。它盖住校界外接框，四周至少留 259 m，正好是 `precip_occ` 在 2 m/px 下的约 1280²（ENG-PERF §1.5）。
  - `map_rect` 就是全图数据纹理的覆盖范围，取 `map.json` 的 `dataRect` ±1280 m，**不随外围扩大**，也不等于箱庭包围盒 `bounds`（§3）。
  - 它盖住 O0 街道带（前沿线以内，约 ±1150 m），所以 O0 和校内一样读这几张纹理；外围 O1、O2 和截面上缘不论是否落在 `map_rect` 里，一律读顶点里写的季节潜势，由顶点标志选择来源（WLD-OUTSKIRTS），在 `map_rect` 的边上不会出现接缝。
  - 平面地图的页面范围不用 `map_rect`，由 SYS-ATLAS另设 `atlasRect`，清水河 ±1536 m，包住箱庭包围盒（SYS-ATLAS §2.3）：±1280 m 以内用全分辨率 SDF，外圈用半分辨率的外圈纹理；`atlasRect` 只给平面地图用，`map_rect` 不因它扩大。
- **C# 侧**：`ISpatialIndex` 的常驻栅格同样从 `MapDescriptor.DataRect` 换算，不写死原点和尺寸。
- **换纹理**：各写入方在 `OnMapLoaded` 里重建纹理；尺寸不变用 `ImageTexture.Update()`，尺寸变了用 `SetImage()`（会重新分配显存）。RID 是否保持、会不会让材质失效或触发编译，P0 验证（风险 4）。
- **`CustomAabb`**：ENG-PERF §9 里罩住整张图的那些，切图时重设：飘落池这类罩住箱庭内容的用地图包围盒 `MapDescriptor.Bounds`；降水池这类跟相机走的用可达范围 `Center` ± `Roam.MaxRadius`（§3）。

## 11 命令行

登记进 ENG-GATES §1、SYS-VIEW §2.5（WP-01 解析，WP-46 生效）：

| 参数 | 截图默认值 | 含义 |
| --- | --- | --- |
| `--map=<id>` | `qingshuihe` | 起始图；不给时取 `index.json` 的默认图（正常启动时是最近存档的图，§4.4）。未知 id 以非零退出码结束 |
| `--spawn=<id>` | — | 在起始图的出生点里选一个：先查 `spawns`，再查本图的点位；未知 id 以非零退出码结束。`--pose` 给出时以 `--pose` 为准 |
| `--pose=<编号>` | `V1` | 在当前图里解析：P 组查本图的 `waypoints.json`，V、E、F、W 组查本图的 `review.json` |
| `--bake=all --map=<id>` | — | 开发和 CI 的无界面烘焙器，只烘一张图；`--map=*` 依次烘全部。本机缓存的参数 `--bake-cache`、`--bake-bg`、`--bake-strict` 见 ENG-GATES |
| `--gate=maptravel` | — | 同图重载门禁（§13），用退出码报告结果 |

- 截图默认串在末尾加 `--map=qingshuihe --title=0 --save=off`（后两项见 SYS-TITLE、SYS-SAVE），与 ENG-GATES §1 合并时再加 SYS-AUDIO的 `--audio=off`，即 `-- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off`。
- 门禁和基准只由命令行决定（SYS-GRAPHICS §6），不因"最近存档的图"而改变起始图：带 `--gate`、`--shots`、`--bench` 时不给 `--map` 就取默认图。

## 12 内存、包体与单图半径

- **进程上限与图的数量无关**：精致、极致不超过 4 GB，均衡、流畅不超过 3 GB；显存按预设（ENG-BUDGET §2、ENG-PERF §7）。先卸后装，所以峰值是"最大的一张图 + 全局常驻"，不是两张图之和。LRU 的字节上限按预设，与哪张图无关。
- **每张图单独核算**：`budgets.json` 加 `maps.<id>` 一节，G8 按图检查。字段：L4 三角形（清水河约 26 万：校内 23 万加外围 O1 的 L4，ENG-BUDGET §3、WLD-OUTSKIRTS）、遮挡体三角形（约 7k，外围第一排楼另可登记约 1k，ENG-PERF §4、WLD-OUTSKIRTS）、全图数据纹理字节、常驻 CPU 数据（约 0.9 MB，加外围约 0.6 MB，SYS-WALK §13、WLD-OUTSKIRTS）、启动集 `boot_<地图>.pck` 体积（每图 ≤ 60 MB，含全图常驻的小数据约 10–15 MB，计入安装包）、本机缓存的估算（清水河烘满约 0.8–1.4 GB，含外围、碰撞级 C 和开放室内；只估算，不计入安装包，D-165）、热切图耗时（≤ 5 s）。G6 的上限是安装目录 ≤ 0.9 GB、分发压缩包 ≤ 0.7 GB（`budgets.json` 的 `export.install`、`export.archive`），按全部图的启动集加全局资源计，本机缓存不计在内，加图时重定（ENG-BUDGET §8、ENG-GATES）。本机缓存的上限默认 2.5 GB，多张图合计超出时按最久未用淘汰，所以图多了也不会无限增长（ENG-PERF §5）。
- **单图半径不超过 8 km**（离中心最远的可达点）：
  - 单精度在 8 km 以内的步长不超过约 0.5 mm，过了 8.19 km 变为约 1 mm；清水河离中心最远 3 km，步长约 0.24 mm。顶点按格包围盒量化（ENG-PERF §5），与世界坐标的大小无关。
  - 整数 tileId 的 10 位格号能覆盖 ±32 km（|ix|、|iz| < 512），8 km 远在其内。
  - 远裁剪面、雾海外半径、远景环半径按图从 `backdrop.json` 读；单图越大，这几项跟着放大。
  - 更大的区域拆成几张图。

## 13 同图重载门禁

属于 **G16 管线编译与切图**（要窗口和显卡，与管线编译检查同排 GPU 锁）。不需要引擎的部分属于 G2。WP-46 在 `game/kernel/map` 里实现引擎内的驱动（只在 `--gate=maptravel` 时启用），WP-27 把它接进 `scripts\gate.ps1`。

**做法**：

1. 截图默认串加 `--gate=maptravel` 启动，到 V1 就绪（ENG-GATES §1 的"截图方式"第 4 条）后记基线 B0：`RenderVideoMemUsed`、`RenderBufferMemUsed`、内核账里的实例数和字节、`ObjectCount`、`ObjectResourceCount`、`ObjectNodeCount`、`ObjectOrphanNodeCount`、物理 body 数、`GC.Collect()` 之后的托管堆、SURFACE、DRAW、Canvas 三项管线编译计数，再截一张图 S0。
2. 连做 3 次 `Travel(qingshuihe, Cause = Test)`，每次到 V1 就绪后记 Bi、截 Si。

`scripts\gate.ps1` 先按改动选出本次要跑的检查，再决定要不要准备烘焙数据（ENG-ARCH §6）：选中这个门禁时，先把它用到的图按哈希增量补进共享缓存（`--bake=all --map=<id>`），再给它追加 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`，所以下面的耗时判据不含本机烘焙（D-165）。

**判据**：

- Bi 与 B0 相差在 ±5% 以内（显存、内核账、对象数）；`ObjectOrphanNodeCount` 不增长；托管堆增长不超过 10 MB（同 ENG-PERF §7 的泄漏门禁）。
- Unload 完成的那一帧，内核账里本图的 RenderingServer 实例和碰撞 body 都归零，只剩全局常驻。
- 旧 `IWorldModel` 的弱引用在 `GC.Collect()` 之后已被回收，证明没有节点跨图持有它。
- 三项管线编译计数在重载全程都不增长（同一张图，Warm 也不该编译）。
- Si 与 S0 的像素差在 G7 的容差内（`--flow=0`，世界状态相同）。
- 每个 `IMapAware` 节点每次各收到一次 `OnMapUnloading` 和 `OnMapLoaded`，顺序按节点 ID。
- 热重载（VeilIn 开始到 VeilOut 开始）不超过 5 s，P0 标定（§19）。

**G2 里的部分**（纯 .NET）：`index.json` 与每份 `map.json` 过 schema；切换状态机用假的服务测"不可取消、只留最后一次、未知图 Rejected、失败退回上一张图"；天气的进图补算与节奏无关（补一次和分几次补，队列相同）；`--map`、`--spawn` 的解析与非零退出。

**何时跑**：集中测试时全量跑，另加行走模式一遍、`--weather=rain` 一遍（降水池、湿润纹理、天气过程）。如果决定做门禁夹具图（风险表第 8 条、§19），集中测试时加跑"清水河 → 夹具图 → 清水河"（ENG-GATES §2）。

## 14 以后的方向

**每张地图一只箱庭，切换时飞越雾海**（大箱庭方案的延伸，WLD-OUTSKIRTS）：

- 每张图都是浮在雾海上的一只箱庭：截面以外是雾海和远景（WLD-DIORAMA、WLD-OUTSKIRTS）。
- 切图的演出：镜头从当前箱庭升到雾海上空 → 朝目标方向飞出 → 穿过云卡的全遮段（SYS-TITLE的云卡着色器）→ 从云里降到新箱庭。第 3–7 步藏在全遮段里；全遮段要盖住热切图的时长，没就绪就停在云里等，与 ENG-PERF §5 的到达保证同一个思路。整段由 SYS-TITLE的过场播放器按数据播，数据放各图的 `cinematics/`。
- 远景环里可以画出邻图箱庭的剪影。飞到那一侧的边缘时，提示语从"前方暂未开放"改为"前方：××"（WLD-OUTSKIRTS），这时 `MapPortal` 才启用。
- 真正无缝（不全遮）要每图一个 World3D、两图短暂同时驻留，内存峰值接近翻倍（风险 6）。第一版不做。

**`MapTimeRule.Fixed`**（预留，现在不用）：给秘境式的小图。进图时把显示的钟点、季节、天气钉在 `world.fixed`，全局时间线在后台照走；界面的时间、季节、天气控件变灰；离开时在雾幕里回到全局的"此刻"。`WorldHours` 照样单调。WP-34 现在只实现 `Shared`；`map.json` 写了 `Fixed` 的图，`Travel` 返回 `Rejected`，G5 也拒绝，直到实现为止。

**`MapKind.Interior`、`Instance`**：给以后的楼层图或副本。WLD-INTERIORS的精选室内不用它，仍属于所在的图。

**加第二张图要做的事**（不改契约、不改流式架构）：

1. 新建 `game/data/maps/<id>/`，写 `map.json`，`index.json` 加一行；
2. `pipeline/` 用 `--map=<id>` 跑出 `campus.json` 等数据；
3. 各区代码新建程序集，标 `MapScope("<id>")`，`ownership.json` 加归属；
4. 写本图的 `backdrop.json`、`sky.json`、`interiors.json`、`incidents.json`（SYS-MOTION §10）、`waypoints.json`、`review.json`、`nav/`、`audio/`，有外围时再写 `outskirts.json`（WLD-OUTSKIRTS），选或新写天气剧本；
5. `budgets.json` 加 `maps.<id>`；
6. `--bake=all --map=<id>`，出启动集和参考哈希清单 `bake.manifest.json`，打 `boot_<id>.pck`；
7. 平面地图底图（SYS-ATLAS）、过场与 `title`、`newGameStart`（SYS-TITLE）各出一份；
8. G5、G7、G8、G16 对新图各跑一遍。

## 15 风险与 P0 验证

| # | 风险 | 对策与 P0 验证 |
| --- | --- | --- |
| 1 | 隐藏的单图假设：写死的全校纹理坐标、`CustomAabb`、通用代码里的实体 ID 和数据路径 | `map_rect`（§10）、G4 正则（§7）、同图重载（§13） |
| 2 | 卸载泄漏：RID、Jolt body、ImageTexture、ResourceLoader 缓存里还被引用的资源、托管堆里跨图持有的世界模型 | P0 实测同图重载 3 次的泄漏和耗时（热启动目标 ≤ 5 s） |
| 3 | PCK 无法卸载 | 包内路径按图分开（§2.4）；P0 验证两张图的启动集 PCK 同时挂载、`replaceFiles: false` 的行为，以及导出过滤器带上 `data/maps/**` |
| 4 | 全局纹理换尺寸 | P0 确认给全局 sampler 换一张尺寸不同的 ImageTexture 后 RID 保持、材质不失效、不触发管线编译；不行就改为各图一律用同一尺寸、范围外留空 |
| 5 | 材质 ID 上限：全场 128 个（ENG-ARCH §3） | P0 验证"在雾幕里清材质缓存、按本图重新分配"不触发编译；不行就把全部图的材质并集控制在 128 以内，由 G5 检查 |
| 6 | 以后换成每图一个 World3D | 给主视口换 World3D 时，节点重新登记、物理空间、倒影视口的行为要原型验证；第一版不做 |
| 7 | 一次移除几千个碰撞 body 的耗时 | 在雾幕里允许；P0 测 Jolt 整格出树的耗时，超过 0.5 s 就分帧 |
| 8 | 只有一张图，跨图路径（不同尺寸的 `map_rect`、两个 PCK、不同的 `MapScope`）永远测不到 | 可选做一张门禁夹具图 `gate-mini`：只有一格平地和一个出生点，只在门禁构建里出现、不进导出包，不算内容。做不做待定（§19） |
| 9 | 帧顺序：流式的选级、挂载与存档快照的先后 | `Stream = −500`（选级与请求）和 `StreamMount = 900`（挂载与碰撞入树）分两步，`Save = 1000` 在最后，任何两步不同值（§9、ENG-ARCH §3） |
| 10 | 读档与切图的先后 | 世界段和天气段先恢复，再 `Travel(LoadSave)`，最后恢复其余段；SYS-SAVE按这一条分三个恢复阶段 |

## 16 工作包与各包增量

包的独占目录与交付以 [ENG-WP](../engineering/ENG-WP-工作包派工单.md) 为准。

## 17 验收

- [ ] `game/data/maps/qingshuihe/` 的目录与归属照 §2.1；`index.json` 和 `map.json` 过 schema；任何代码里都没有写死的、不按图的数据路径（如 `res://data/campus.json`，G4）。
- [ ] 带 `MapScope` 的只有各区程序集；通用模块里没有 `FeatureClaim`；`--gate=registry --map=*` 通过（G5）。
- [ ] 本机缓存目录、缓存键、`boot_qingshuihe.pck` 与包内路径都按图分开；导出版能读到 `data/maps/qingshuihe/` 的全部 JSON（G6）。
- [ ] 全图纹理一律按 `map_rect` 采样；把 `dataRect` 临时改成别的范围再烘一遍，地面落叶、积雪、积水、雨的遮蔽、路灯光斑都不错位（P0 一次性检查）。
- [ ] 同图重载 3 次满足 §13 的全部判据（G16）。
- [ ] 切图前后 `WorldHours` 不减、`Instance` 不变；"回到落金初灯"在清水河上的行为与 SYS-TIME §1 完全相同（G2、G13）。
- [ ] 行走模式下同图重载：碰撞就绪之前不开物理，安全点环形缓冲已清空，不掉出世界（G16 的行走模式一遍）。
- [ ] 截图默认串带 `--map=qingshuihe` 时画面与不带时完全相同（G7）。

## 19 待验证（P0）

| 待验证 | 验证方法或负责包 | 验证前的初值 |
| --- | --- | --- |
| 切换 8 步各步的时长（§4.2 的表） | WP-46，开发机热启动下实测 | 表里的值 |
| 热切图：VeilIn 开始到 VeilOut 开始（§4.2、§13、风险 2） | WP-46，用同图重载（G16）标定 | ≤ 5 s |
| 启动集 PCK 挂载后的常驻开销（§2.4、风险 3） | WP-02、WP-46，两张图的启动集 PCK 同时挂载时量常驻内存 | 按很小计 |
| 空缓存下切图：出生点周围现场烘焙的耗时（§4.2） | WP-02、WP-46，清空本机缓存后 `Travel` | 单件初值（开发机单线程）：L0 一格 ≤ 150 ms、碰撞一格 ≤ 50 ms、室内一层 ≤ 100 ms |
| 启动集体积与本机缓存烘满后的实际体积（§12） | WP-02，导出版实测 | 启动集每图 ≤ 60 MB；缓存烘满约 0.8–1.4 GB |
| 给全局 sampler 换一张尺寸不同的 `ImageTexture` 后，RID 是否保持、材质是否失效、是否触发管线编译（§10、风险 4） | WP-46 与各全图纹理的写入方，P0 原型 | 按可行处理；不行就改为各图一律用同一尺寸、范围外留空 |
| 门禁夹具图 `gate-mini` 做不做（风险 8） | 待定；WP-46、WP-27 | 不做；§13 的"清水河 → 夹具图 → 清水河"不跑 |
| 以后各图的区代码目录怎么排（§7） | 加第二张图时定 | 现在不动 |
