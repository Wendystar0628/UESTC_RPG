---
id: SYS-ATLAS
title: 平面地图与传送
category: systems
summary: 平面地图的总则：允许与禁止什么、多地图下的数据归属、存档与设置里存什么；界面与传送分在两份子文档
keywords: [平面地图, 地图只活在界面层, 3D 里不标注, IAtlasService, MarkerId, 自定义标记存档, 解锁状态存档, 前往方式, 图层筛选, AtlasSave, TransitPreference]
owners: [WP-48]
contributors: [WP-01, WP-35, WP-36, WP-51, WP-53a]
depends: [CORE-INDEX, ENG-ARCH, UI-NAV, SYS-WALK, UI-VISUAL, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS]
related: [SYS-VIEW, ENG-GATES, MAP-QSH-ADDITIONS, ENG-BUDGET, ENG-PERF, SYS-SAVE, SYS-AUDIO, ENG-ROADMAP]
contracts: [IAtlasService, MarkerId, AtlasSave, TransitPreference]
data: []
gates: [G2, G16]
---

# SYS-ATLAS · 平面地图与传送

> **范围**：本文规定平面地图的总则：界面层与 3D 画面的分界（允许什么、禁止什么）、多地图下的数据归属、存档与设置里存什么。底图、小地图、大地图见 SYS-ATLAS-UI，标记、锚点、传送与解锁见 SYS-ATLAS-TRAVEL。
> **不在本文**：地图加载与切换 `IMapService`（SYS-MAPS）；点位与"飞过去"的跳转过渡（UI-NAV-WAYPOINTS §2）；界面令牌与画面布局（UI-VISUAL）；开放室内的清单与入口（WLD-INTERIORS）；外围与截面线（WLD-OUTSKIRTS）；声音（SYS-AUDIO）；存档容器与保存时机（SYS-SAVE）；服务契约（`game/contracts/Atlas/`）、锚点与标记数据（`game/data/maps/qingshuihe/atlas/`、`overlay/atlas.d/`）、地图一致性验收（ENG-GATES G18）。
> **查阅**：先读 §0；允许与禁止看 §1；多地图下的数据看 §2；存档与设置看 §3

## 0 结论

1. **平面地图只活在界面层**：小地图、大地图和标记都是界面，3D 画面里不叠加地点标签、选中描边、世界空间标记、光柱或追踪标记（INV-3）。世界里唯一与地图对应的是锚点实体，即导览设施（SYS-ATLAS-TRAVEL §2）。
2. **不是网页版地图**：没有文字搜索、路径规划和 3D 拾取（INV-1）。
3. **存档与设置**：解锁策略、自定义标记、追踪目标进存档；小地图、朝向、前往方式、图层筛选进设置（§3）。

**子文档**

- SYS-ATLAS-UI 地图界面：烘焙底图、上色、做标注、小地图、大地图、传送卡的界面时读。
- SYS-ATLAS-TRAVEL 标记、锚点与传送：新增锚点、观景点、可传送标记，做传送流程、落点和解锁时读。

### 0.1 接入要点

- 想在 3D 画面里标出地点、路线、选中或追踪：不许，只画在小地图和大地图上（§1、SYS-ATLAS-TRAVEL §2）
- 要知道玩家在哪、在哪个区：观者位置读 `IObserverState`，区域名用 `IAtlasService.RegionAt()`；`ICameraRig.Pose` 不当观者位置（SYS-ATLAS-UI §2）
- 从界面发起传送或飞过去：一律调 `IAtlasService.Travel`；同图走 `ICameraRig.Transit`，目标在别的图上才走 `IMapService.Travel`（SYS-ATLAS-TRAVEL §3.1）

## 1 允许什么、禁止什么

| 允许（只在界面层） | 禁止 |
| --- | --- |
| 小地图：底图、锚点、门口、自定义标记、追踪目标、玩家箭头与视锥、比例尺、区域名 | 3D 画面里的地点标签、选中描边、拾取、世界空间标记、光柱、路线线条、追踪标记、罗盘条 |
| 大地图：全部标记、区域名、地名、路名、比例尺、光标坐标与"距此多少米"、OSM 署名 | 文字搜索框、路径规划（任意两点之间算路）、营业时间和电话这类信息卡 |
| 传送卡、目录页、图层与图例 | 传送时的 3D 粒子、光柱、锚点发光或"解锁动画" |
| 推荐路线的折线（SYS-WALK-BOUNDS §6） | 右键地图任意一点"飞往此处" |

- 比例尺、坐标和距离读数只在大地图和小地图里。
- 地图上的字可以有英文名（中英切换跟 UI-NAV §5）；场景里的字（含导览亭、导览桩）只用简体中文（INV-4）。

## 2 多地图下的数据

- 地图标识一律用 `MapId("qingshuihe")`；`MarkerId` 只在一张图内唯一，跨图引用写成"地图 + 标记"。服务名是 `IAtlasService`，与地图加载切换的 `IMapService` 不得混名。
- 平面地图的数据在本图 `atlas/`（`markers.json`、`labels.json`）、覆盖层 `overlay/atlas.d/` 和缩略图目录 `game/assets/maps/qingshuihe/thumbs/`；底图与标注布局是本机烘焙产物，不进 git、不随包发（SYS-MAPS §1.2）。点位表归 UI-NAV-WAYPOINTS §1，开放清单归 WLD-INTERIORS。
- 按图与全局的划分以 SYS-MAPS §1.1 为准：筛选偏好和小地图设置是全局的，底图、标记、标注、区域表、锚点实体、缩略图、自定义标记、解锁状态都按图。
- **换图**：卸载时释放本图的地图纹理和缩略图，加载时读新图数据并重建标记；换图在雾幕里完成，小地图跟着雾幕淡出淡回。多图时"MAP 01/01"变成切换标签；楼层图以后作为同一张图的"地图层"，现在不用。

## 3 存档里存什么、设置里存什么

**存进存档**（按图一段）：

- 解锁策略 ID；自定义标记（整数 ID、位置、形状，备注预留）和下一枚的 ID；正在追踪的目标；与策略初值不同的解锁状态（观光期为空）；到访过的锚点（观光期只用于统计）。
- 读档时：位置落在地图页之外的标记丢弃，超过 50 枚的截断并写日志，ID 重复的保留第一枚；追踪目标已不存在时清空；未知 ID 丢弃。新游戏时这些都为空，下一枚 ID 为 1。

**存进设置**：小地图（常驻 / 移动时显示 / 关，默认常驻）；小地图朝向（北朝上 / 随镜头，默认北朝上）；地图上的前往方式（传送 / 飞过去，决定传送卡的主按钮，默认传送）；图层筛选（默认设施、地标关，其余开）。这几项登记在 UI-NAV §3.5 的设置表。
