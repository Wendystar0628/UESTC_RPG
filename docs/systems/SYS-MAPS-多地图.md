---
id: SYS-MAPS
title: 多地图
category: systems
parent: SYS-INDEX
summary: "按图数据与全局的划分、map.json 约定、归属、map_rect、单图规模；先卸后装的切图运行时在主线之后"
keywords: [多地图, 换图, 切图, 地图服务, 按图数据, 先卸后装, 同图重载, 天气按图, 单图半径, 本机烘焙缓存, IMapService, IMapAware, MapScope, MapDescriptor, map.json, map_rect]
owners: [WP-46]
contributors: [WP-00, WP-01, WP-02, WP-08, WP-15, WP-22, WP-23, WP-24, WP-26, WP-27, WP-34, WP-35, WP-36, WP-39, WP-40, WP-43, WP-44, WP-45, WP-47, WP-48, WP-49, WP-50, WP-51, WP-53]
depends: [SYS-VIEW, ENG-ARCH, ENG-PERF, SYS-TIME, SYS-WEATHER, SYS-SAVE, WLD-OUTSKIRTS]
related: [CORE-INDEX, WLD-OUTSKIRTS-DIORAMA, ENG-GATES, ENG-BUDGET, PLAN-WP, UI-NAV, SYS-WALK, SYS-GRAPHICS, SYS-ATLAS, SYS-TITLE, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP]
contracts: [MapId, WaypointRef, MapKind, MapTimeRule, GeoFrame, MapRect, RoamLimits, MapTitle, MapSpawn, MapWorldRules, MapEnvironment, MapPortal, MapDescriptor, MapScopeAttribute, IMapContext, MapTravelCause, MapTravelRequest, MapStage, MapTravelResult, IMapTransition, IMapService, IMapAware, MapVisit]
data: ["game/data/maps/index.json", "game/data/maps/qingshuihe/map.json", "game/data/maps/qingshuihe/"]
gates: [G2, G4, G5, G6, G7]
paths: ["game/data/maps/**"]
---
# SYS-MAPS · 多地图

> **范围**：本文规定多地图的模型：哪些数据按图存放、哪些全局共用，`map.json` 的约定，一个 World3D 内先卸后装的切图，时间与季节全局、天气按图，切图时的相机、行走与安全点，区代码的程序集归属 `MapScope`，全图数据纹理的范围 `map_rect`，单图规模上限。现在只有一张图 `qingshuihe`，数据与采样的约定（地图编号、数据根目录、`map_rect`）一律按多张图写；**切图的运行时、请求排队、失败回退和同图重载不泄漏在主线之后**，主线只做启动时加载当前图（§3）。
> **不在本文**：`map.json` 的字段（`game/contracts/Maps/MapContracts.cs`、`MapFiles.cs` 与各图的 `map.json`）；契约签名（`game/contracts/Maps/`、`game/contracts/Runtime/Maps/`）；帧顺序（契约 `FramePriority`）；命令行与同图重载门禁的做法（ENG-GATES-SHOTS §1、ENG-GATES §1）；平面地图的标记与传送（SYS-ATLAS）；活动范围与外围（WLD-OUTSKIRTS）；存档格式（SYS-SAVE）；开始页与进入游戏（SYS-TITLE）；换图时的声音（SYS-AUDIO）。
> **查阅**：先读 §0；按图与全局的划分、烘焙缓存看 §1；`map.json` 的约定看 §2；切图看 §3（运行时在主线之后）；时间与天气看 §4；相机、行走与安全点看 §5；归属看 §6；`map_rect` 看 §7；规模上限看 §8；以后的方向看 §9

## 0 结论

1. **现在只有一张图，但按多张图写**：不存在"全校"这个隐含前提，范围、原点、出生点、预算都取自当前图（§1、§2）。
2. **按图的数据放 `game/data/maps/<地图>/`，按图的区代码放 `game/gen/maps/<地图>/`（每图一个程序集）**。区代码靠所在程序集的 `MapScope` 归属，通用模块每张图都跑、只读当前图的数据（§6）。
3. **切图在数据层完成，不换场景**：本作的静态内容不在场景树里，按图记账，卸一张图就是按账逐项释放（§3.1）。
4. **一个 World3D，先卸后装**：加载界面盖起（SYS-TITLE §3.1）→ 卸旧图 → 装新图 → 出生 → 加载界面淡出；内存峰值是一张图加全局常驻，不做无缝切换（§3、§8）。切图运行时在主线之后。
5. **时间与季节全局，天气按图求值**：世界时间切图时照常向前；自动天气是（种子、地图、世界时间）的纯函数，不存按图的状态，切图不需要补算（§4）。
6. **同图重载不泄漏**（`Travel` 到当前图 3 次，旧模型能回收）在主线之后，到第二张图立项时再做（§3.5）。
7. **全图数据纹理一律按 `map_rect` 采样**，不按"全校"隐式换算（§7）。
8. **单图半径不超过 8 km**（§8）。

### 0.1 接入要点

- **改地图数据**：只改项目的世界文件 `game/data/maps/<图>/world/`，增删改写明理由；不从 OSM 或其他在线地图取数据，也不照着改；OSM 只剩署名与随版本发的地图数据包（D-252、ENG-RELEASE §5）
- **读本图的数据文件**：路径由 `IMapContext.DataRoot` 拼出，烘焙产物用 `BakeRoot`；内核、系统、界面和通用程序集里不写地图 ID、`res://data/maps/…` 路径和完整实体 ID 的字面量（G4，§6）
- **加数据或资源**：放 `game/data/maps/<地图>/` 下本包自己的文件，资源放各资源目录的 `<地图>/` 子目录；`map.json` 归 WP-46（地图服务与多地图），字段增改须经主会话批准（§1、§2）
- **写区内容**：区代码靠程序集的 `MapScope`；通用模块只读 `BuildContext.Map` 和本图数据，不认领具体实体，出现 `FeatureClaim` 由 G5 报错（§6）
- **全局常驻的 `[KernelNode]` 用到按图的数据**：实现 `IMapAware`，在 `OnMapLoaded` 重建；`OnMapUnloading` 释放本图的实例、纹理内容和缓存，随切图运行时在主线之后；不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容（§3.3）
- **写或采样全图数据纹理**：按 `map_rect` 换算，在 `OnMapLoaded` 里重建；罩住整张图的 `CustomAabb` 切图时重设（§7）
- **在切图途中被调用**（主线之后）：`IMapService.Context` 在 Unload 到 Mount 之间为 null，要判空（§3）
- **同图内移动**（跳转、传送、回位）：交给 `ICameraRig.Transit` 和 `IAtlasService`，不调 `Travel`（§3.3、§4）

## 1 数据归属

### 1.1 按图与全局

| 按图（每张图一份） | 全局（所有图共用） |
| --- | --- |
| 原点与投影、校界、活动范围、`map_rect` | 着色器白名单、全局参数名、材质库与色名、调色板关键帧 |
| 世界文件与世界模型、外围数据 | 树种表、部件编目、`ToonSpec`、室内生成器与类型库、规则集 |
| 开放室内清单、偶发事件清单 | 画面、按键、界面、声音四类设置 |
| 点位、出生点、审查视角与路线 | 界面主题与组件；音频库、混音总线与音量 |
| 可行走数据、碰撞格、导航图、声景数据 | `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime` |
| TileBundle、L4、遮挡体；灯光时刻表、灯表、窗覆盖 | 天气剧本库、天气基础种子（每个存档一个） |
| 远景、天空档案、倒影湖 | 存档容器与槽位；`IAppFlow` 状态机 |
| 平面地图的底图与标记 | 进程内存与显存的总上限 |

- 目录与文件清单以 `game/data/maps/` 和 `ownership.json` 为准；按图的资源放在各资源目录的 `<地图>/` 子目录。
- 实体 ID 只在本图内唯一；跨图引用写成"地图 + ID"。`qsh:` 前缀属于数据，不是地图 ID。以后的图用自己的前缀，G5 检查前缀不冲突。
- 世界文件 `world/<区>.json`：每区一份，记录就是 `WorldFeature`，格式见 STD-CONVENTIONS §3.1，加载以 `game/gen/world/WorldModelLoader.cs` 为准；原点与投影在 `world/geo.json`。
- 精选室内属于所在的图，不是单独的图，随本图的流式加载进出（WLD-INTERIORS）。

### 1.2 烘焙缓存

- 烘焙什么、随包、查找顺序与判鲜归 ENG-ARCH §4（D-165）。
- 缓存按图分开；本机缓存与画面设置、世界状态、存档无关，跨存档、跨画质共用，它不是存档（STD-CONVENTIONS §4）。不另做几何启动集；主 PCK 只留全局资源和各图的 JSON。

## 2 地图描述 `map.json`

字段与取值以 `game/contracts/Maps/MapContracts.cs`、`MapFiles.cs` 和各图的 `map.json` 为准，G5 检查 `defaultSpawn` 能解析、`dataRect` 盖住校界外接框、倒影湖实体存在。约定如下：

- `map.json` 只写手写部分；原点与投影、中心、箱庭包围盒、格范围由加载器从世界模型和烘焙统计派生，不手写。
- **`dataRect`** 是全图数据纹理的覆盖范围，边长是 64 m 的整数倍，必须盖住校界外接框和外围街道带（§7）。平面地图的页面范围 `atlasRect` 不在这里（SYS-ATLAS）。
- **`roam`** 的公式与取值归 WLD-OUTSKIRTS；没有外围数据的图用"校界外扩 + 高度系数"的兜底。
- **世界规则**：时间规则只有 `Shared` 一种；本图默认状态是新游戏建实例时的状态，也是"回到本图默认状态"的目标。默认状态的数值只在 SYS-TIME §1.1；清水河上按钮名是"回到落金初灯"，新游戏直接建在这个状态（SYS-TITLE §3.1）。
- **倒影湖**写实体 ID，列表顺序即湖号；清水河有西湖和东湖两个。
- `map.json` 不写预算（预算不记在图里，ENG-BUDGET §3），也不写允许的季节（季节全局共享、任选，SYS-MOTION §3）；以后要钉住季节的图用 `fixed`。
- 开始页镜头的字段以 SYS-TITLE 为准。

## 3 切换

**主线范围**：只有一张图，启动时在扉页后面加载当前图（§3.2 的装载、预热、出生），新游戏与读档只摆位姿、不切图。遮挡、卸载与图和图之间的切换，以及 §3.3 的规则和 §3.5 的门禁，在主线之后。

### 3.1 一个 World3D，先卸后装

场景树里始终只有内核根节点。静态内容经 RenderingServer 挂载，碰撞 body 由 `kernel/collision` 入树，都按图记账。主 World3D 和倒影的独立 World3D 里属于本图的东西，Unload 时一并清掉。

### 3.2 切图的步骤（切图部分主线之后）

在 `FramePriority.Map` 那一步推进。**换图经加载界面**（SYS-TITLE §3.1），它下面世界照常运行，全局节点不重建，整体几秒内完成，不分步卡时限。顺序：

1. **遮挡**：加载界面盖满；占用观者输入；取消进行中的跳转与预取；小地图淡出；存档暂停拍快照；World 声音组压低。
2. **记录与卸载**：记下旧图的 `MapVisit`（位姿、模式）；流式停发请求；按账释放本图的实例、碰撞 body、L4、遮挡体；各 `IMapAware` 收到 `OnMapUnloading`；地图内容、室内提供者与生产者由注册器的地图作用域持有，消费者卸载和数据参与者 Unload 后统一释放该作用域，失败路径也清理；`Context` 置空；`GC.Collect()` 一次。
3. **装载**（`MapStage` 的 LoadData、Mount）：建世界模型，按"通用 + 本图"筛选模块；同步加载 L4 与遮挡体，写 `map_rect`，重建全图数据纹理，实例化本图节点，各 `IMapAware` 收到 `OnMapLoaded`，预取出生点周围。
4. **预热与出生**（Warm、Spawn）：只预热本图新出现的"着色器 × 顶点格式 × 渲染状态"组合，一批交给引擎预编译（同图重载为 0，ENG-PERF §2，D-261）；设位姿并合法化，行走模式等碰撞就绪，清相机插值缓冲，重判介质，`Current` 换成新图。
5. **撤遮挡**：加载界面淡出，环境声淡入；跨图到达显示一次到达标题；释放输入占用，返回 `Arrived`。

- **热切图目标**：整体几秒内（按出生点周围已在缓存里计）；冷启动目标见 SYS-TITLE §1。缓存里没有出生点周围的格时现场本机烘焙，遮挡保持到就绪，不限时；预取清单有键进入 `BakeState.Failed` 或连续 60 s 没有进展，按 Failed 处理（§3.3）。
- **Boot**：窗口启动时就是扉页，跳过卸载，从装载起在扉页后面完成（SYS-TITLE）。
- 生产本图数据的实例（烘焙服务等）实现 `IMapDataParticipant`：装时全部 `Prepare` 完成才分发 `OnMapLoaded`；卸时先等在途的 `Prepare` 结束，消费者全部释放后才 `Unload`。已挂着一张图、正在卸载或 `Prepare` 未完成时再 Mount 一律报错。

### 3.3 规则（主线之后）

- **不可取消**：请求一经接受就走完。
- **只留最后一次**：途中再来请求只保留最后一次，当前这次走到出生后不撤遮挡，直接接着走下一次；目标相同就合并。
- **Rejected**：未知的图、尚未实现的规则（`Fixed`）、`Kind` 不是 `Outdoor`。
- **Failed**：加载出错时留在加载界面下，退回上一张图；上一张也失败就进默认图；都失败就在加载界面上显示失败页（重试 / 复制错误信息 / 退出，SYS-TITLE §1），不无限重试。退出不写新的一代存档，换图之前的那一代仍有效（SYS-SAVE），也不出保存失败的三选一。`--title=0` 与门禁下不出页，写日志后以非零退出码结束。
- **全局节点不重建**：天空、植被、水、灯光、天气、界面、平面地图、音频等跨图常驻，只靠 `IMapAware` 换数据，不得跨图持有 `IWorldModel`、`FeatureId`、格子句柄或纹理内容。
- **同图内的移动不走地图服务**：点位跳转、回位交给 `ICameraRig.Transit`，传送交给 `IAtlasService`（同图传送也经加载界面，SYS-ATLAS-TRAVEL §3.2）；`IAtlasService` 只在目标标记属于别的图时才调 `Travel`。

### 3.4 入口

| 入口 | 怎么走 |
| --- | --- |
| 冷启动 | 起始图 = 命令行 `--map` > 最近存档的图 > 默认图；LoadData–Warm 在扉页后面完成 |
| 新游戏 | 起始图是默认图；当前已是这张图就不切图，只按 SYS-SAVE 重置世界，经加载界面进入（SYS-TITLE §3.1） |
| 继续 / 读档 | 先恢复世界段与天气段，再 `Travel`，最后恢复其余段（SYS-SAVE）；读档是换一个世界实例，切图本身不改 `Instance`，到达后也不请求保存 |
| 平面地图传送（主线之后） | 只有目标标记在别的图时才用（SYS-ATLAS） |

### 3.5 同图重载不泄漏（主线之后）

连续 `Travel` 到当前图 3 次：旧 `IWorldModel` 的弱引用必须能被回收，每个 `IMapAware` 各收到一次卸载与加载通知，显存、内核账、对象数、托管堆不持续增长，热重载几秒内。**一票否决**：同图重载后旧 `IWorldModel` 无法回收，或显存、对象数持续增长（泄漏）。

**关闭顺序**：先关图并停止后台工作，再让节点离树（离树时仍能查已有绑定）；随后注册器按创建的逆序释放自己拥有的对象，最后清场（ENG-ARCH §2）。关闭期间禁止新建，借用对象不纳入拥有列表。

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
- 新游戏的起始图从本图的默认状态建。
- 降水、湿润、积水的全局参数跟当前图的天气走，切图的遮挡下直接写新值（切图在主线之后）。

**默认状态与"回到落金初灯"**：`IWorldControl.ResetDefault()` 的语义是"回到本图默认状态"，规则见 SYS-TIME §1.1；按钮名取本图的默认状态名。

## 5 相机、行走与安全点（主线之后）

- **不变的**：速度档、视场角、灵敏度、改键、冲刺方式都是设置，切图不变。
- **位姿优先级**：请求里的位姿 > 请求里的出生点 > 本图的 `MapVisit.LastPose` > 默认出生点。
- **观察模式**：请求给了模式就用，否则保持；目标出生点没有行走落点时自动切到旁观并提示一次。
- **行走模式**：Spawn 时不开物理，等角色所在格和邻格的碰撞就绪（SYS-WALK），交接时清相机插值缓冲；切图的遮挡可以多停，停得过久写日志。
- **活动范围**读新图的 `roam`，推回状态清零。
- **安全点**环形缓冲在 Unload 时清空，不把旧图的点带进新图。切图途中回位不响应。
- **介质**在出生后重判，过渡在遮挡下直接落定（SYS-VIEW-MEDIUM §1）。
- 跳转：遮挡开始时取消进行中的 `Transit`；`Transit` 不跨图。

## 6 归属与 `MapScope`

- **求法**：注册器照常扫描全部程序集，给每个模块求所属的图：类上的 `Map` → 程序集的 `MapScope` → 都没有就是通用模块。每次 LoadData 只启用"通用 + 当前图"的模块。
- **区代码**（core、south、west 等各区）每张图一个程序集 `Campus.Gen.Maps.<图>`，在 `game/gen/maps/<图>/`，各区是它的子目录与命名空间 `Campus.Gen.Maps.<图>.<区>`；`MapScope` 由该程序集的 csproj 标一次，各区包自己不写。跨图共用的区站点基类与几何在通用程序集 `Campus.Gen` 的 `Campus.Gen.Sites`，不属于任何图。程序集清单和模块名单由 MSBuild 实际构建的项目引用推出，不手改。**通用程序集**（建筑、地面、植被、水、家具、碰撞、外围、室内生成器等）不标归属，每张图都跑，只读 `BuildContext.Map` 和本图数据，不认识任何具体的图。
- 节点寿命由 `Lifetime` 声明：`Map` 在 Mount 创建、Unload 由注册器逆序释放，`Global` 常驻；`Map` 和程序集 `MapScope` 只筛选适用地图。地图专属节点显式声明 `Lifetime = RegistrationLifetime.Map`。世界文件按图，只读当前图的目录。
- **规则**：归属不同图的程序集互不引用；通用程序集不得引用带 `MapScope` 的程序集。**通用模块里出现 `FeatureClaim`（认领具体实体）直接报错**，因为它说明该模块属于某张图（G5）。
- **G4**：通用程序集、内核、系统、界面代码里不得出现完整实体 ID、地图 ID 和 `res://data/maps/…` 的字面量，数据路径一律由 `IMapContext.DataRoot` 拼出；例外只有 `kernel/map` 和门禁夹具。

## 7 全局参数 `map_rect`

`map_rect`（vec4：xz 起点与宽深的倒数）是全图数据纹理的覆盖范围，取 `map.json` 的 `dataRect`，由 WP-46（地图服务与多地图）在切图的 Mount 一步写入，登记在 ENG-PERF §1.5。

- **用它的纹理**：`ground_mask`、`ground_mask2`、`terrain_height`、`precip_occ`、`lamp_grid`，以及以后任何全图纹理；它们共用同一个 `map_rect`，分辨率可以不同。不按"全校"隐式换算，否则换一张尺寸不同的图就会错位，这是最典型的"以为只有一张图"。
- **范围外**取"无数据"的默认值：遮罩为 0、`precip_occ` 视为没有顶面、地形高度取 0（着色器用 `map_uv()`、`in_map()`）。
- `map_rect` 不随外围扩大，也不等于箱庭包围盒；它盖住外围街道带，所以街道带与校内一样读这几张纹理；更外的外围读顶点里写的季节潜势，在边上不出接缝（WLD-OUTSKIRTS）。平面地图另有自己的页面范围（SYS-ATLAS）。
- **C# 侧**栅格同样从 `MapDescriptor.DataRect` 换算；各写入方在 `OnMapLoaded` 里重建纹理。
- **`CustomAabb`**：罩住箱庭内容的（飘落池）切图时重设为 `Bounds`；跟相机走的（降水池）用可达范围 `Center` ± `Roam.MaxRadius`，否则镜头在箱庭外朝外看时整池被剔除，雨雪凭空消失。

## 8 内存、包体与单图半径

- **进程上限与图的数量无关**：峰值是最大的一张图加全局常驻，不是两张图之和；上限按预设（ENG-BUDGET §2、ENG-PERF-STREAM §3）。
- **每张图单独核算**：性能按图实测核算（ENG-BUDGET §4）。安装包体按全部图的原始数据加全局资源计，本机缓存不计在内（ENG-PERF-STREAM §1、ENG-BUDGET §3）。
- **单图半径不超过 8 km**（离中心最远的可达点）：单精度在 8 km 内够用；远裁剪面、雾海外半径、远景环半径按图从 `backdrop.json` 读。更大的区域拆成几张图。

## 9 以后的方向

**加第二张图**不改契约、不改流式架构：建 `game/data/maps/<id>/`，`index.json` 加一行；写这张图的世界文件（D-252）；新建 `game/gen/maps/<id>/` 程序集（csproj 标 `MapScope`，主程序集与测试程序集的 `gen/maps/*/*.csproj` 通配自动带上，程序集与模块名单不手改）；补齐本图的远景、天空、室内、事件、点位、审查视角、导航、音频、预算、平面地图底图；G5、G7 对新图各跑一遍。

## 10 待验证（P0）

| 待验证 | 初值 |
| --- | --- |
| 热切图整体是否在几秒内（§3.2，主线之后，WP-46（地图服务与多地图）用同图重载看） | 几秒内 |
| 给全局 sampler 换一张尺寸不同的 `ImageTexture` 后，RID 是否保持、材质是否失效、是否触发管线编译（§7） | 按可行处理；不行就各图一律同尺寸、范围外留空 |
| 加载界面下清材质缓存、按本图重新分配材质 ID 是否触发编译（全场 128 个，ENG-PERF §3） | 不行就把全部图的材质并集控制在 128 以内 |
