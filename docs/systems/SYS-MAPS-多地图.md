---
id: SYS-MAPS
title: "多地图：按图数据、`IMapService` 与切换"
category: systems
summary: "按图数据与全局的划分、map_rect、单图半径等多图约定；先卸后装的切图运行时在主线之后"
keywords: [多地图, 换图, 切图, 地图服务, 按图数据, 先卸后装, 同图重载, 天气按图, 单图半径, 本机烘焙缓存, IMapService, IMapAware, MapScope, MapDescriptor, map.json, map_rect]
owners: [WP-46]
contributors: [WP-00, WP-01, WP-02, WP-08, WP-15, WP-22, WP-23, WP-24, WP-26, WP-27, WP-34, WP-35, WP-36, WP-39, WP-40, WP-43, WP-44, WP-45, WP-47, WP-48, WP-49, WP-50, WP-51, WP-53]
depends: [SYS-VIEW, ENG-ARCH, ENG-PERF, SYS-TIME, SYS-WEATHER, SYS-SAVE, WLD-OUTSKIRTS]
related: [CORE-INDEX, WLD-DIORAMA, ENG-GATES, ENG-BUDGET, ENG-WP, UI-NAV, SYS-WALK, SYS-GRAPHICS, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP]
contracts: [MapId, WaypointRef, MapKind, MapTimeRule, GeoFrame, MapRect, RoamLimits, MapTitle, MapSpawn, MapWorldRules, MapEnvironment, MapPortal, MapDescriptor, MapScopeAttribute, IMapContext, MapTravelCause, MapTravelRequest, MapStage, MapTravelResult, IMapTransition, IMapService, IMapAware, MapVisit]
data: ["game/data/maps/index.json", "game/data/maps/qingshuihe/map.json", "game/data/maps/qingshuihe/", "game/contracts/schemas/map.schema.json"]
gates: [G2, G4, G5, G6, G7]
---
# SYS-MAPS · 多地图：按图数据、`IMapService` 与切换

> **范围**：本文规定多地图的模型：哪些数据按图存放、哪些全局共用，一个 World3D 内先卸后装的切图，时间与季节全局、天气按图，全图数据纹理的范围 `map_rect`，单图规模上限。现在只有一张图 `qingshuihe`，数据与采样的约定（地图编号、数据根目录、`map_rect`）从第一天起按多张图写；**切图的运行时、借用遮挡、请求排队、失败回退和同图重载不泄漏在主线之后**，主线只做启动时加载当前图（§3）。
> **不在本文**：`map.json` 的字段（`game/contracts/schemas/map.schema.json` 与各图的 `map.json`）；契约签名（`game/contracts/Maps/`）；帧顺序（ENG-ARCH §3 的 `FramePriority`）；命令行与同图重载门禁的做法（ENG-GATES-SHOTS §1、ENG-GATES §1）；平面地图的标记与传送（SYS-ATLAS）；活动范围与外围（WLD-OUTSKIRTS）；存档格式（SYS-SAVE）；开始页与过场（SYS-TITLE）；换图时的声音（SYS-AUDIO）。
> **查阅**：先读 §0；按图与全局的划分、烘焙缓存与启动集看 §1；`map.json` 的约定看 §2；切图看 §3（运行时在主线之后）；时间与天气看 §4；归属看 §6；`map_rect` 看 §7；规模上限看 §8

## 0 结论

1. **现在只有一张图，但按多张图写**：不存在"全校"这个隐含前提，范围、原点、出生点、预算都取自当前图（§1、§2）。
2. **按图的数据放 `game/data/maps/<地图>/`，代码目录不按图拆**。区代码靠所在程序集的 `MapScope` 归属，通用模块每张图都跑、只读当前图的数据（§6）。
3. **切图在数据层完成，不换场景**：本作的静态内容不在场景树里，按图记账，卸一张图就是按账逐项释放（§3.1）。
4. **一个 World3D，先卸后装**：雾幕盖起 → 卸旧图 → 装新图 → 出生 → 雾幕散；内存峰值是一张图加全局常驻，不做无缝切换（§3、§8）。切图运行时在主线之后。
5. **时间与季节全局，天气按图求值**：世界时间切图时照常向前；自动天气是（种子、地图、世界时间）的纯函数，不存按图的状态，切图不需要补算（§4）。
6. **同图重载不泄漏**（`Travel` 到当前图 3 次，旧模型能回收）在主线之后，到第二张图立项时再做（§3.5）。
7. **全图数据纹理一律按 `map_rect` 采样**，不按"全校"隐式换算（§7）。
8. **单图半径不超过 8 km**（§8）。

### 0.1 接入要点

- **读本图的数据文件**：路径由 `IMapContext.DataRoot` 拼出，烘焙产物用 `BakeRoot`；内核、系统、界面和通用程序集里不写地图 ID、`res://data/maps/…` 路径和完整实体 ID 的字面量（G4，§6）
- **加数据或资源**：放 `game/data/maps/<地图>/` 下本包自己的文件，资源放各资源目录的 `<地图>/` 子目录；`map.json` 归 WP-46，字段增改须经主代理批准（§1、§2）
- **写区内容**：区代码靠程序集的 `MapScope`；通用模块只读 `BuildContext.Map` 和本图数据，不认领具体实体，出现 `FeatureClaim` 由 G5 报错（§6）
- **全局常驻的 `[KernelNode]` 用到按图的数据**：实现 `IMapAware`，在 `OnMapLoaded` 重建；`OnMapUnloading` 释放本图的实例、纹理内容和缓存，随切图运行时在主线之后；不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容（§3.3）
- **写或采样全图数据纹理**：按 `map_rect` 换算，在 `OnMapLoaded` 里重建；罩住整张图的 `CustomAabb` 切图时重设（§7）
- **在切图途中被调用**（主线之后）：`IMapService.Context` 在 Unload 到 Mount 之间为 null，要判空（§3）
- **同图内移动**（跳转、传送、回位）：交给 `ICameraRig.Transit` 和 `IAtlasService`，不调 `Travel`；切图不换 `IWorldState.Instance`（§3.3、§4）

## 1 数据归属

### 1.1 按图与全局

| 按图（每张图一份） | 全局（所有图共用） |
| --- | --- |
| 原点与投影、校界、活动范围、`map_rect` | 着色器白名单、全局参数名、材质库与色名、调色板关键帧 |
| 世界模型、覆盖层、外围数据 | 树种表、部件编目、`ToonSpec`、室内生成器与类型库、规则集 |
| 开放室内清单、偶发事件清单 | 画面、按键、界面、声音四类设置 |
| 点位、出生点、审查视角与路线 | 界面主题与组件；音频库、混音总线与音量 |
| 可行走数据、碰撞格、导航图、声景数据 | `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` |
| TileBundle、L4、遮挡体；灯光时刻表、灯表、窗覆盖 | 天气剧本库、天气基础种子（每个存档一个） |
| 远景、天空档案、倒影湖 | 存档容器与槽位；`IAppFlow` 状态机 |
| 平面地图的底图与标记；过场 | 进程内存与显存的总上限 |

- 目录与文件清单以 `game/data/maps/` 和 `ownership.json` 为准；按图的资源放在各资源目录的 `<地图>/` 子目录。
- 实体 ID 只在本图内唯一；跨图引用写成"地图 + ID"。`qsh:` 前缀是 OSM 管线的既有写法，属于数据，不是地图 ID。以后的图用自己的前缀，G5 检查前缀不冲突。
- 覆盖层的规则以 `game/gen/world/WorldModelLoader.cs` 为准。
- 精选室内属于所在的图，不是单独的图，随本图的流式加载进出（WLD-INTERIORS）。

### 1.2 本机烘焙缓存与启动数据

- **本机烘焙**（D-165）：L0–L3 的 TileBundle（含外围 O0、O1）、碰撞级 C、室内包与碰撞包、平面地图底图不随安装包发，在玩家机器上由与开发烘焙器同一个 `Build()` 生成，写进本机缓存。查找顺序：本机缓存 → 本机烘焙。做法见 ENG-PERF-STREAM §1、ENG-ARCH §3（`IBakeService`）。
- **缓存按图分开**，各图互不覆盖；只有影响 `Build()` 的输入改了才重烘，改出生点、默认状态、过场、点位不重烘。判鲜规则见 ENG-ARCH §4、ENG-PERF-STREAM §1.1。
- **本机缓存与画面设置、世界状态、存档无关**，跨存档、跨画质共用（CORE-CONVENTIONS §4）；它不是存档。
- **没有几何启动集**：每张图的几何都在本机烘焙（ENG-ARCH §4）。
- 主 PCK 只留全局资源和各图的 JSON。

## 2 地图描述 `map.json`

字段与取值以 `game/contracts/schemas/map.schema.json` 和各图的 `map.json` 为准，G2 校验，G5 检查 `defaultSpawn` 能解析、`dataRect` 盖住校界外接框、倒影湖实体存在。约定如下：

- `map.json` 只写手写部分；原点与投影、中心、箱庭包围盒、格范围由加载器从世界模型和烘焙统计派生，不手写。
- **`dataRect`** 是全图数据纹理的覆盖范围，边长是 64 m 的整数倍，必须盖住校界外接框和外围街道带（§7）。平面地图的页面范围 `atlasRect` 不在这里（SYS-ATLAS）。
- **`roam`** 的公式与取值归 WLD-OUTSKIRTS；没有外围数据的图用"校界外扩 + 高度系数"的兜底。
- **世界规则**：时间规则只有 `Shared` 一种；本图默认状态（新游戏交还控制时的状态、"回到本图默认状态"的目标）与新游戏起点预设分开写。清水河的默认状态是秋日傍晚晴（落金初灯，按钮"回到落金初灯"），新游戏从略早的起点建实例，由 SYS-TITLE 的过场推到默认状态。
- **倒影湖**写实体 ID，列表顺序即湖号；清水河有西湖和东湖两个。
- `map.json` 不写预算（预算不记在图里，ENG-BUDGET §3），也不写允许的季节（季节全局共享、任选，SYS-MOTION §3）；以后要钉住季节的图用 `fixed`。
- 过场与开始页镜头的字段以 SYS-TITLE 为准；缺省时走雾幕直达。

## 3 切换

**主线范围**：只有一张图，启动时在扉页后面加载当前图（下表 LoadData 到 Spawn 五步），新游戏与读档只摆位姿、不切图。VeilIn、Snapshot、Unload、VeilOut 与图和图之间的切换，以及 §3.3 的规则和 §3.5 的门禁，在主线之后。

### 3.1 一个 World3D，先卸后装

场景树里始终只有内核根节点。静态内容经 RenderingServer 挂载，碰撞 body 由 `kernel/collision` 入树，都按图记账。主 World3D 和倒影的独立 World3D 里属于本图的东西，Unload 时一并清掉。

### 3.2 八步（切图部分主线之后）

在 `FramePriority.Map` 那一步推进。雾幕下世界照常运行（天空、天气、风都在走），全局节点不重建。整体几秒内完成，不分步卡时限。

| 步 | 名称 | 做什么 |
| --- | --- | --- |
| 1 | VeilIn | 雾幕盖满；占用观者输入；取消进行中的跳转与预取；小地图淡出；存档暂停拍快照；World 声音组压低 |
| 2 | Snapshot | 记下旧图的 `MapVisit`（位姿、模式） |
| 3 | Unload | 流式停发请求；按账释放本图的实例、碰撞 body、L4、遮挡体；各 `IMapAware` 收到 `OnMapUnloading`；`Context` 置空；`GC.Collect()` 一次 |
| 4 | LoadData | 建世界模型；按"通用 + 本图"筛选模块 |
| 5 | Mount | 同步加载 L4 与遮挡体；写 `map_rect`；重建全图数据纹理；实例化本图节点；各 `IMapAware` 收到 `OnMapLoaded`；预取出生点周围 |
| 6 | Warm | 只预热本图新出现的组合，等管线编译计数稳定；同图重载为 0 |
| 7 | Spawn | 设位姿并合法化；行走模式等碰撞就绪；清相机插值缓冲；重判介质；`Current` 换成新图 |
| 8 | VeilOut | 雾幕散开，环境声淡入；跨图到达显示一次到达标题；释放输入占用，返回 `Arrived` |

- **热切图目标**：整体几秒内（按出生点周围已在缓存里计）；冷启动按 ENG-PERF-STREAM §4。
- **缓存里没有出生点周围的格**时，在雾幕下现场本机烘焙，雾幕保持不透明直到就绪，不限时；预取清单有键进入 `BakeState.Failed` 或连续 60 s 没有进展，按 Failed 处理（§3.3）。
- **Boot**：窗口启动时就是扉页，第 1–3 步跳过，从 LoadData 起在扉页后面完成（SYS-TITLE）。
- 生产本图数据的实例（烘焙服务等）实现 `IMapDataParticipant`：装时全部 `Prepare` 完成才分发 `OnMapLoaded`；卸时先等在途的 `Prepare` 结束，消费者全部释放后才 `Unload`。已挂着一张图、正在卸载或 `Prepare` 未完成时再 Mount 一律报错。

### 3.3 规则（主线之后）

- **不可取消**：请求一经接受就走完。
- **只留最后一次**：途中再来请求只保留最后一次，当前这次走到 Spawn 后不散雾幕，直接从下一次的 Snapshot 接着走；目标相同就合并。
- **Rejected**：未知的图、尚未实现的规则（`Fixed`）、`Kind` 不是 `Outdoor`。
- **Failed**：加载出错时留在雾幕里，退回上一张图；上一张也失败就进默认图；都失败就在雾幕上显示失败页（重试 / 复制错误信息 / 退出，SYS-TITLE §1），不无限重试。退出不写新的一代存档，换图之前的那一代仍有效（SYS-SAVE），也不出保存失败的三选一。`--title=0` 与门禁下不出页，写日志后以非零退出码结束。
- **全局节点不重建**：天空、植被、水、灯光、天气、界面、平面地图、音频等跨图常驻，只靠 `IMapAware` 换数据，不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容。
- **同图内的移动不走地图服务**：点位跳转、传送、回位交给 `ICameraRig.Transit` 和 `IAtlasService`；`IAtlasService` 只在目标标记属于别的图时才调 `Travel`。

### 3.4 入口

| 入口 | 怎么走 |
| --- | --- |
| 冷启动 | 起始图 = 命令行 `--map` > 最近存档的图 > 默认图；LoadData–Warm 在扉页后面完成 |
| 新游戏 | 起始图是默认图；当前已是这张图就不切图，只按 SYS-SAVE 重置世界，由 SYS-TITLE 的过场接上 |
| 继续 / 读档 | 先恢复世界段与天气段，再 `Travel`，最后恢复其余段（SYS-SAVE）；读档是换一个世界实例，切图本身不改 `Instance`，到达后也不请求保存 |
| 平面地图传送（主线之后） | 只有目标标记在别的图时才用（SYS-ATLAS） |

### 3.5 同图重载不泄漏（主线之后）

连续 `Travel` 到当前图 3 次：旧 `IWorldModel` 的弱引用必须能被回收，每个 `IMapAware` 各收到一次卸载与加载通知，显存、内核账、对象数、托管堆不持续增长，热重载几秒内。**一票否决**：同图重载后旧 `IWorldModel` 无法回收，或显存、对象数持续增长（泄漏）。

## 4 世界状态：时间与季节全局，天气按图求值

| 量 | 范围 | 切图时 |
| --- | --- | --- |
| `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` | 全局，一条时间线 | 照常向前；"世界时间单调向前"在切图时照样成立，存档只记一份时间 |
| `IWorldState.Instance` | 全局 | 不变；只有读档和新游戏才加 1 |
| 自动天气 | 按图求值：（种子、地图、世界时间）的纯函数 | 无状态，不需要冻结与补算 |
| 灯光时刻表、窗覆盖、灯表 | 按图 | 经 `IMapAware` 换成新图的 |
| 日月轨迹、调色板关键帧、`ui_night` | 全局 | 不动 |

**天气按图求值**：

- 自动天气由（种子、地图、世界时间）直接求出（SYS-WEATHER-PROCESS §1），存档里没有按图的天气段；手动与钉住是世界级设定，存在全局 `weather` 段（SYS-SAVE）。
- 新游戏的起始图从本图的新游戏起点建。
- 降水、湿润、积水的全局参数跟当前图的天气走，切图那一帧在雾幕里直接写新值（切图在主线之后）。

**默认状态与"回到落金初灯"**：`IWorldControl.ResetDefault()` 的语义是"回到本图默认状态"：读当前图的默认状态，正向换季、快进到下一次出现的钟点、流逝 1×、自动天气，默认状态的天气保持晴到本季黄昏带结束；不移动镜头（SYS-TIME §1、SYS-WEATHER-PROCESS §1.6）。按钮名取本图的默认状态名。

## 5 相机、行走与安全点（主线之后）

- **不变的**：速度档、视场角、灵敏度、改键、冲刺方式都是设置，切图不变。
- **位姿优先级**：请求里的位姿 > 请求里的出生点 > 本图的 `MapVisit.LastPose` > 默认出生点。
- **观察模式**：请求给了模式就用，否则保持；目标出生点没有行走落点时自动切到旁观并提示一次。
- **行走模式**：Spawn 时不开物理，等角色所在格和邻格的碰撞就绪（SYS-WALK），交接时清相机插值缓冲；切图的雾幕可以多停，停得过久写日志。
- **活动范围**读新图的 `roam`，推回状态清零。
- **安全点**环形缓冲在 Unload 时清空，不把旧图的点带进新图。切图途中回位不响应。
- **介质**在 Spawn 后重判，过渡在雾幕里直接落定（SYS-VIEW-MEDIUM §1）。
- 跳转：VeilIn 取消进行中的 `Transit`；`Transit` 不跨图。

## 6 归属与 `MapScope`

- **求法**：注册器照常扫描全部程序集，给每个模块求所属的图：类上的 `Map` → 程序集的 `MapScope` → 都没有就是通用模块。每次 LoadData 只启用"通用 + 当前图"的模块。
- **区代码**（core、south、west 等各区程序集）由 WP-00 在 csproj 里标 `MapScope`，各区包自己不写。**通用程序集**（建筑、地面、植被、水、家具、碰撞、外围、室内生成器等）不标归属，每张图都跑，只读 `BuildContext.Map` 和本图数据，不认识任何具体的图。
- `[KernelNode]` 带 `Map` 的属于本图，Mount 时实例化、Unload 时释放；不带的全局常驻。覆盖层按图，只扫描当前图的目录。
- **规则**：归属不同图的程序集互不引用；通用程序集不得引用带 `MapScope` 的程序集。**通用模块里出现 `FeatureClaim`（认领具体实体）直接报错**，因为它说明该模块属于某张图（G5）。
- **G4**：通用程序集、内核、系统、界面代码里不得出现完整实体 ID、地图 ID 和 `res://data/maps/…` 的字面量，数据路径一律由 `IMapContext.DataRoot` 拼出；例外只有 `kernel/map` 和门禁夹具。

## 7 全局参数 `map_rect`

`map_rect`（vec4：xz 起点与宽深的倒数）是全图数据纹理的覆盖范围，取 `map.json` 的 `dataRect`，由 WP-46 在切图的 Mount 一步写入，登记在 ENG-PERF §1.5。

- **用它的纹理**：`ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid`，以及以后任何全图纹理；它们共用同一个 `map_rect`，分辨率可以不同。不按"全校"隐式换算，否则换一张尺寸不同的图就会错位，这是最典型的"以为只有一张图"。
- **范围外**取"无数据"的默认值：遮罩为 0、`precip_occ` 视为没有顶面、地形高度取 0（着色器用 `map_uv()`、`in_map()`）。
- `map_rect` 不随外围扩大，也不等于箱庭包围盒；它盖住外围街道带，所以街道带与校内一样读这几张纹理；更外的外围读顶点里写的季节潜势，在边上不出接缝（WLD-OUTSKIRTS）。平面地图另有自己的页面范围（SYS-ATLAS）。
- **C# 侧**栅格同样从 `MapDescriptor.DataRect` 换算；各写入方在 `OnMapLoaded` 里重建纹理。
- **`CustomAabb`**：罩住箱庭内容的（飘落池）切图时重设为 `Bounds`；跟相机走的（降水池）用可达范围 `Center` ± `Roam.MaxRadius`，否则镜头在箱庭外朝外看时整池被剔除，雨雪凭空消失。

## 8 内存、包体与单图半径

- **进程上限与图的数量无关**：峰值是最大的一张图加全局常驻，不是两张图之和；上限按预设（ENG-BUDGET §2、ENG-PERF-STREAM §3）。
- **每张图单独核算**：性能按图实测核算（ENG-BUDGET §4）。安装包体按全部图的原始数据加全局资源计，本机缓存不计在内（ENG-PERF-STREAM §1、ENG-BUDGET §3）。
- **单图半径不超过 8 km**（离中心最远的可达点）：单精度在 8 km 内步长不超过约 0.5 mm；整数 tileId 的格号范围远大于 8 km；远裁剪面、雾海外半径、远景环半径按图从 `backdrop.json` 读。更大的区域拆成几张图。

## 9 以后的方向

**加第二张图**不改契约、不改流式架构：建 `game/data/maps/<id>/`，`index.json` 加一行；管线 `--map=<id>` 出世界数据；区代码新建程序集标 `MapScope`；补齐本图的远景、天空、室内、事件、点位、审查视角、导航、音频、预算、平面地图底图、过场与新游戏起点；G5、G7 对新图各跑一遍。

## 10 待验证（P0）

| 待验证 | 初值 |
| --- | --- |
| 热切图整体是否在几秒内（§3.2，主线之后，WP-46 用同图重载看） | 几秒内 |
| 给全局 sampler 换一张尺寸不同的 `ImageTexture` 后，RID 是否保持、材质是否失效、是否触发管线编译（§7） | 按可行处理；不行就各图一律同尺寸、范围外留空 |
| 雾幕里清材质缓存、按本图重新分配材质 ID 是否触发编译（全场 128 个，ENG-ARCH §3） | 不行就把全部图的材质并集控制在 128 以内 |
