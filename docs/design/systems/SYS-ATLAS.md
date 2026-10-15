---
id: SYS-ATLAS
title: "平面地图与传送：底图、小地图、大地图、标记、锚点、传送与解锁"
category: systems
summary: "规定平面地图：底图烘焙与上色、小地图、大地图、标记、锚点、雾幕传送、解锁、契约与 G18 验收"
owners: [WP-48]
contributors: [WP-01, WP-04, WP-06, WP-13, WP-27, WP-35, WP-36, WP-39, WP-44, WP-51, WP-53a]
depends: [CORE-INDEX, ENG-ARCH, UI-NAV, SYS-WALK, UI-VISUAL, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS]
related: [SYS-VIEW, ENG-GATES, MAP-QSH-ADDITIONS, ENG-BUDGET, ENG-PERF, SYS-SAVE, SYS-AUDIO, ENG-ROADMAP]
contracts: [IAtlasService, IUnlockPolicy, MarkerId, MarkerKind, AnchorForm, UnlockState, TravelStyle, TravelRefusal, PinShape, TeleportSpec, AtlasMarker, AtlasRegion, AtlasLayer, CustomPin, AtlasSave, TransitPreference]
data: ["game/data/maps/qingshuihe/atlas/", "game/data/maps/qingshuihe/overlay/atlas.overlay.json", "game/assets/maps/qingshuihe/thumbs/"]
gates: [G2, G13, G14, G15, G16, G18]
---

# SYS-ATLAS · 平面地图与传送：底图、小地图、大地图、标记、锚点、传送与解锁

> **范围**：本文规定平面地图的全部内容：底图的烘焙与上色、小地图、大地图、标记类型、锚点实体、传送的流程与落点、解锁策略、按图数据、实现与性能、契约、存档段与设置项，以及 G18 地图一致性门禁。
> **不在本文**：地图加载与切换 `IMapService`（SYS-MAPS）；点位与"飞过去"的跳转过渡（UI-NAV §7）；界面令牌、`map.*` 色值与画面布局（UI-VISUAL）；开放室内的清单与入口（WLD-INTERIORS）；外围与截面线（WLD-OUTSKIRTS）；声音（SYS-AUDIO）；存档容器与保存时机（SYS-SAVE）。
> **关联**：[CORE-INDEX 总索引](../README.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH.md)、[UI-NAV 界面与跳转](../ui/UI-NAV.md)、[SYS-WALK 行走](SYS-WALK.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL.md)、[SYS-MAPS 多地图](SYS-MAPS.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS.md)；[SYS-VIEW 观察模式与介质](SYS-VIEW.md)、[ENG-GATES 验收门禁](../engineering/ENG-GATES.md)、[MAP-QSH-ADDITIONS 设计增补](../maps/qingshuihe/MAP-QSH-ADDITIONS.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF.md)、[SYS-SAVE 存档](SYS-SAVE.md)、[SYS-AUDIO 音频](SYS-AUDIO.md)、[ENG-ROADMAP 扩展路线](../engineering/ENG-ROADMAP.md)

**分工**：WP-48 平面地图、标记与传送，分三个子包（各包交付见 §13）：
- 48a 底图烘焙与地标贴图（`pipeline/map`）；
- 48b 小地图与大地图界面（`game/hud/map`）；
- 48c 标记、锚点、传送与解锁服务（`game/systems/atlas`）。

WP-13、WP-27、WP-35、WP-36、WP-39、WP-44、WP-01、WP-04、WP-06、WP-51、WP-53a 各有增量（§13）。

## 0 结论

1. **平面地图只活在界面层**：
   - 小地图、大地图和标记都是界面；
   - 3D 画面里仍然不叠加地点标签、选中描边、世界空间标记、光柱或追踪标记（INV-3）。
   - 世界里唯一与地图对应的东西是锚点实体，它是校园里本来就有的导览设施（§6），不发光、不做特效。
2. **底图做成"版图式校园平面图"**：
   - 从世界模型烘焙精确矢量距离场（SDF），在玩家机器上本机烘焙（D-165），运行时由白名单着色器 `map_base.gdshader` 上色（§2）；
   - 世界模型包括 OSM 加全部覆盖层的增补与删除，不直接用原始 OSM，所以艺术化处理后的环境和地图始终一致；
   - 配色按 UI-VISUAL登记的 `map.*` 色名，晶白、深屏各一套：大地图随主题；小地图是常驻件，默认固定用深屏一套，白天也是深底亮线；季节只轻微改绿地底色，天气不改地图；
   - 外围街区画成淡线，不可传送；地图页范围是 `atlasRect`（清水河 ±1536 m），包住整个箱庭外缘：`map_rect`（±1280 m）以内全分辨率，外圈半分辨率（§2.3）。
3. **小地图**：左上角常驻，北朝上，是漫游态唯一常驻的元素；外框 244×272（UI-VISUAL的画面布局）；按 U、`--ui=0`、过场、大地图或设置卡打开时隐藏（§3）。
4. **大地图**：
   - 按 M 打开，按 G 直接打开它的"目录"页，点位目录（UI-NAV §4.5）就是这一页；
   - 有图层筛选、图例、传送卡、自定义标记（上限 50）和追踪（§4）。
5. **可传送的标记只有四类**：锚点、观景点、可进入室内的门口、路线起点。地标、设施和自定义标记只能追踪（§5）。
   - "可进入的室内"图层由 WLD-INTERIORS的开放清单驱动（经它的室内索引读入口），以后清单里加一栋楼，地图上自动出现它的门口标记和轮廓高亮，不用改地图数据。
   - 观景点是 P00–P23，其中 P16–P23 是 WLD-INTERIORS新增的室内与高处点位。
6. **锚点 13 个**：大节点 6 个"导览亭"（MAP-QSH-ADDITIONS §2.13），牌面就是烘焙出的底图；补空 7 个"导览桩"，是低矮的金属立桩配一盏暖色小灯。不用银杏，不用蓝色全息（§6）。
7. **传送卡有两个按钮**（§7）：
   - 【传送】是雾幕瞬移，为默认：雾幕盖满 → 移位 → 等目的地就绪 → 雾幕散开，全程约 1.1–2.4 s；
   - 【飞过去】走 UI-NAV §7 的飞行。
   - 两者共用 `ICameraRig.Transit`、雾幕层、预取和到达保证。数字键和 `[` `]` 仍然是飞过去。
8. **解锁**：策略取自规则集的 `UnlockPolicy`（ENG-ROADMAP §6 第 1 条）；观光规则集取 `all-unlocked`，全部可用；`proximity`（靠近解锁）只留接口给以后的 RPG（§8）。
9. **服务名是 `IAtlasService`**（§11），管标记、解锁、追踪和传送。它与 SYS-MAPS的 `IMapService`（地图加载与切换）是两回事，不得混名。
10. **不是网页版地图**：
    - 没有文字搜索，目录只是分组列表；
    - 没有路径规划，只显示预先设计的推荐路线；
    - 没有在 3D 里拾取；
    - 传送卡只有名称、缩略图、推荐状态和按钮，不是网页地图那种地点卡片（INV-1）。

## 1 允许什么、禁止什么

| 允许（只在界面层） | 禁止 |
| --- | --- |
| 小地图：底图、锚点、门口、自定义标记、追踪目标、玩家箭头与视锥、比例尺、底栏区域名 | 3D 画面里的地点标签、选中描边、拾取、世界空间标记、光柱、路线线条、追踪标记，以及罗盘条 |
| 大地图：全部标记、区域名、地名、路名、比例尺、光标坐标与"距此多少米"、OSM 署名 | 文字搜索框、路径规划（任意两点之间算路）、营业时间和电话这类信息卡 |
| 传送卡、目录页、图层与图例 | 传送时的 3D 粒子、光柱、锚点发光或"解锁动画" |
| 推荐路线的折线（预先设计的 R 系列路线，SYS-WALK §11） | 右键地图任意一点"飞往此处" |

- 大地图本身是一张图，所以比例尺、坐标和距离读数允许出现，但只在大地图和小地图里；漫游态的其余位置仍然没有这些读数。
- 地图属于"再现场景"，地图上的字可以有英文名（中英切换跟 UI-NAV §8）。场景里的字仍然只用简体中文（INV-4）。导览亭、导览桩上的字也一样。
- 地图的界面、图标和锚点一律不用银杏意象。

## 2 底图（WP-48a 烘焙，WP-48b 上色）

### 2.1 来源：世界模型，不是原始 OSM

- **输入**：
  - SYS-MAPS的按图数据：`campus.json`，加上 `overlay/` 下的全部覆盖层，即 `reshape`、`height`、`hide`、`add`（ENG-ARCH §3）；
  - 外围数据 `outskirts.json`（WLD-OUTSKIRTS）：O0、O1 的街区轮廓、道路边线、河道，以及截面线（即箱庭外缘，进 `map_sdf_out.A`）；
  - 地图页范围取 `atlas/markers.json` 的 `atlasRect`（清水河 ±1536 m，§2.3）；全分辨率 SDF 的范围取 `map.json` 的 `dataRect`（SYS-MAPS），也就是全局参数 `map_rect` 的值（±1280 m）。
- 各区为了画面增删的元素（新增的广场、栈道、桥，删掉的临时围挡、杂乱的小建筑等）都按覆盖层后的结果进图，原始 OSM 里被 `hide` 的要素不画。
- **画进底图的要素**：水体、绿地、道路、广场与铺装、运动场、建筑、桥、栈道、校界、箱庭外缘，以及外围的街区、道路、河道。
- **不画进底图的**：家具、树（不画树点，绿地只用点阵表示）、灯具、生活痕迹。
- **烘焙器**：
  - 纯 .NET 的烘焙代码，引用 `Campus.Contracts` 和 `Campus.Gen.World`（只读），由它构造世界模型，保证与运行时看到的是同一份。代码在随包程序集里，发布版由 `IBakeService` 在玩家机器上调用（本机烘焙，D-165）；`pipeline/map/` 是同一份代码的控制台入口，给开发和 CI 的 `--bake=all` 用；
  - 对每个纹素求到矢量要素边界的精确有符号距离，用 32 m 空间桶加速，按行块多线程。
  - **何时烘**：首次运行（或缓存清空、版本更新以后）在扉页期间烘，先烘起始视角周围的格，再烘底图（SYS-TITLE §1）；纹理创建与 `map_base` 的预热等底图烘完。以后的启动直接读缓存。
  - 全图烘焙耗时的初值：8 线程 ≤ 20 s（§16）。
- **道路**：
  - 存"到中心线的距离减半宽"，宽度取 WP-15 的路宽；
  - 窄于 1 个纹素的小路不会被侵蚀，着色器里给它保底画 1 物理像素。
- **编码**：
  - 距离夹在 ±8 m，映射到 0–255，128 是边界，每级约 6.3 cm；
  - 着色器用 `fwidth` 做边缘抗锯齿，任意缩放都锐利。

**纹理**（数据纹理，按 ENG-BUDGET §7 第 5 条：不压缩的 RGBA8，运行时用 `Image` 创建 `ImageTexture`，带 mipmap）：

| 名称 | 尺寸 | 范围 | 每纹素 | 通道 | 显存（含 mip） |
| --- | --- | --- | --- | --- | --- |
| `map_sdf_a` | 2048² | 校界外扩 64 m 的方框，约 ±1056 m | 约 1.03 m | R 水，G 绿地，B 道路，A 建筑 | 约 21 MB |
| `map_sdf_b` | 1024² | 同上 | 约 2.06 m | R 区域与地点编号（最近邻，向外膨胀 2 纹素），G 要素类别（普通建筑、地标、广场、运动场、桥与栈道，最近邻，膨胀），B 运动场与广场的 SDF，A 校界 SDF | 约 5.3 MB |
| `map_sdf_out` | 1024² | `map_rect` 以内，清水河 ±1280 m | 2.5 m | R 外围街区，G 外围道路，B 外围水体，A 箱庭外缘 | 约 5.3 MB |
| `map_sdf_ring` | 896² | 整个地图页 `atlasRect`，清水河 ±1536 m；着色器只在 `map_rect` 以外取用 | 约 3.4 m（纹素密度约为 `map_sdf_out` 的一半） | 同 `map_sdf_out` | 约 4.3 MB |
| `map_glyphs` | 1024² | — | — | 地标线描图集，MSDF 格式（§2.5） | 约 5.3 MB |

- 合计约 41 MB，计入 ENG-BUDGET §2 的纹理显存 384 MB（§10）。地图纹理的显存以本表为准，WLD-OUTSKIRTS引用本表。
- 小地图和大地图共用这五张纹理，常驻；它们不随时间、季节、天气和画面预设变化。
- 这些是平面地图自己的数据纹理，`map_base` 不读全局参数 `map_rect`。全分辨率部分在烘焙时取与 `map_rect` 同一个矩形，所以与 `ground_mask`、`precip_occ` 等全图纹理的覆盖范围一致（SYS-MAPS）；外圈纹理只给平面地图用，不是全图数据纹理，`map_rect` 不因它扩大。
- **接缝**：`map_base` 在 ±1280 m 边线内侧 24 m 里把 `map_sdf_out` 与 `map_sdf_ring` 按距离混合；两张都从同一份矢量求精确距离，零值线一致，看不出接缝。外圈里只有外围淡线和箱庭外缘，半分辨率足够。

**烘焙产物与哈希**：

- 产物是本机烘焙的（D-165），按 ENG-ARCH §4 进本机缓存（`user://cache/bake/qingshuihe/<ABI>/map/<哈希>/`），不随安装包发（按图分目录，见 SYS-MAPS §2.4）。
- 同一目录写一份 `map.manifest.json`：世界模型哈希（campus.json、全部覆盖层、outskirts.json 的内容哈希）、烘焙代码哈希、页面范围、纹素尺寸、区域表版本。
- G18 用它核对底图与当前世界模型一致（§14）；任何覆盖层改了就要重烘，烘焙器的缓存键包含这些哈希。
- 底图没就绪（缓存里还没有对应哈希的产物，本机烘焙正在烘）时，底图只画校内用地一种颜色，不阻塞运行。这只是短暂状态：烘完就换成完整底图，不报"底图过期"。

**地标贴图**（48a）：

1. **地标线描图集 `map_glyphs`**：
   - 12 个地标各一枚屋顶平面线稿，例如图书馆的八瓣采光顶、宣邦楼钟楼、体育馆的放射屋面、训练馆的筒拱肋。
   - 先由 WP-27 的截图机按正交俯视出参考图，48a 照着画 SVG 线稿（每枚不超过 40 条线），再烘成 MSDF。
   - 线稿外框与建筑轮廓的对位误差不超过 0.5 m（G18）。
2. **导览亭牌面与导览桩铭牌**（§6）：
   - 由同一份 SDF 离线渲染成贴图，配色取**场景调色板**，不用界面令牌；
   - 交给 WP-04，进"不重复内容"的图集（ENG-PERF §1.5）。

### 2.2 上色：`map_base.gdshader`

- **白名单着色器**：
  - `game/style/shaders/map_base.gdshader`，`shader_type canvas_item`，归 WP-48b（ENG-PERF §1.4 登记）；
  - ShaderMaterial 按 ENG-ARCH §2 由材质库（`Campus.Style`）创建。
  - 一个着色器两种用法，由材质 uniform `mode` 区分（不用 `#define`，ENG-BUDGET §7 第 1 条）：
    - `mode = 0` 画底图；
    - `mode = 1` 画地标线描图集里的一枚（MSDF）。
- **uniform**：
  - 视图：中心 (x, z)、每米像素数、旋转角，由小地图逐帧写（§10）；
  - 主题：取值 0 晶白、1 深屏，加切换扫描参数，跟随 UI-VISUAL的主题切换，与其余大页同一次扫过。小地图是常驻件，按 UI-VISUAL的约定写 `pin_night = 1`，固定取深屏一套；只有设置"常驻件：随主题"时才跟随切换；
  - 揭示掩码：`uvec2`，64 位，每位一个区域，给解锁用（§8），观光模式全为 1；
  - 选中高亮：一个区域或要素编号。
- **全局参数**：只读 `season`（ENG-PERF §1.5），用来轻微改绿地底色（§2.4）。不读写其他全局参数。

**画层与色名**（由下到上）。色名是 UI-VISUAL令牌表里的 `map.*` 一组，晶白、深屏各一个值，已登记在 `game/style/palette/ext/ui.json` 的 `map` 段；色值以 UI-VISUAL为准，本文件不另写色值：

| 层 | 色名 | 画法 |
| --- | --- | --- |
| 地图页底 | `map.base` | 平涂；只在大地图上，于校外空白区画 100 m 间距的十字测试点，不画连续的网格线 |
| 外围街区 | `map.outskirts` | 只有轮廓淡线和极淡的填色（§2.3） |
| 箱庭外缘 | `map.edge` | 1 物理像素发丝线 |
| 校内用地 | `map.land` | 平涂 |
| 绿地 | `map.green` | 平涂加 8 px 点阵；底色随季节轻微变化 |
| 水体 | `map.water` | 平涂加 45° 影线（版图工具的图层填充语言），岸线 1 物理像素 |
| 广场、铺装、栈道、桥 | `map.plaza` | 平涂，比用地略深一档 |
| 道路 | `map.road` | 填色加发丝描边；道路是"上下文"，不靠它传达关键信息 |
| 运动场 | `map.sport` | 只描边，跑道画一圈内线；不用暖色（UI-VISUAL规定暖色点只表示"太阳 / 此刻"） |
| 建筑 | `map.building` | 填色加 1 物理像素描边 |
| 地标建筑 | `map.landmark` | 填色和描边各深一档，叠线描（`mode = 1`） |
| 校界 | `map.bound` | 5/3 虚线 |
| 可进入的室内 | `map.interior` | 运行时由标记层画轮廓高亮（§5），不烘进底图 |
| 标注 | `map.label`、`map.label.halo` | 文字加 2 px 晕（§2.5） |
| 玩家 | `map.player` | 主色箭头加视锥（§3） |

- 主色（清蓝）在地图上只用于玩家、选中、追踪和可传送标记的"通电"状态；大面积的底图是中性冷灰，与 UI-VISUAL"主色只做小面积点亮"一致。
- 夜图（深屏）不在楼块上点暖色窗灯点：界面里唯一的暖色点只表示"太阳 / 此刻"（UI-VISUAL）。
- 任何线都不细于 1 物理像素；界面缩放大于等于 150% 时整体乘缩放（UI-VISUAL的线宽规则）。

### 2.3 外围、校界与箱庭外缘

- **外围街区**（WLD-OUTSKIRTS的 O0、O1 带，即校界到截面线之间，约 250–500 m，位置以 WLD-OUTSKIRTS为准）：
  - 画街区轮廓、主要道路边线和河道，线色 `map.outskirts`，比校内建筑描边淡一档，填色几乎看不出；
  - 只标 4 个路名作定位：西源大道、天润路、科新南路、绕城高速；外围保留的身份地标（WLD-OUTSKIRTS）只写名字，不画线描，也没有锚点。
  - 不放任何可传送标记，鼠标悬停时光标读数后面加"校外 · 不可前往"。自定义标记可以放在外围。
- **校界**：虚线 `map.bound`。行走的边界就在这里（WLD-OUTSKIRTS）。
- **箱庭外缘**：发丝线 `map.edge`，就是截面线，飞行能到的地方以它为界（WLD-OUTSKIRTS）。线外是地图页底 `map.base`，不画雾海纹样（UI-VISUAL不画装饰纹样）；Z0、Z1 两档在外缘外侧写一行淡字标注"雾海"（`atlas/labels.json` 的水体类），回应 WLD-OUTSKIRTS"截面线以外是雾海"的画法。
- **地图页范围**：另设 `atlasRect`，清水河为 x、z 都是 −1536…1536 m，包住整个箱庭截面线（截面线包围盒的初值按 WLD-OUTSKIRTS的估计，约为 x −1250…1260、z −1400…1440，南缘的 1440 m 也在页内；§16）。`atlasRect` 写在本图的 `atlas/markers.json`（§9），只管平面地图，与 SYS-MAPS `map.json` 的 `dataRect`（= `map_rect`，±1280 m）分开：±1280 m 以内用全分辨率 SDF（`map_sdf_a`、`map_sdf_b`、`map_sdf_out`），1280–1536 m 的外圈用半分辨率的外圈纹理 `map_sdf_ring`（§2.1）。校园主轴斜转约 33°，北朝上时整片校园呈斜方形，页边正好留出标题、比例尺和署名的位置。
  - **页外的东西**：P00 (−641, 928, 1283) 在页内；P15 在约 (−1640, 775, 262)（WLD-OUTSKIRTS），在页外（西缘以外约 100 m）。规则是：页外的标记（观景点、自定义标记）贴在页边，画一枚指向它的小箭头加距离，与小地图的追踪指示同一画法（§3），照常能选中、能传送；玩家飞出页外时，大地图的玩家箭头也贴边显示。
  - **截面线定稿后**：如果截面线超出 ±1536 m，只把 `atlasRect` 按 64 m 对齐外扩，外圈纹理的纹素随之变粗；`dataRect` 和 `map_rect` 都不动。G18 报告箱庭外缘被页边裁掉的长度（§14.1），正常应为 0。

### 2.4 季节与昼夜：只做轻微变化

- **昼夜**：
  - **大地图**跟随 UI-VISUAL的晶白、深屏两套主题，离散切换，带回差；与其余大页在同一次扫描前沿里换掉，"减少动态效果"开启时直接换。不做连续混色。
  - **小地图**是常驻件，默认固定用深屏一套（深底亮线，读作"芯片版图"），白天也一样，交班时不变；设置"常驻件：随主题"时才跟随切换（UI-VISUAL）。
- **季节**：
  - 只改 `map.green` 的底色，四季各一个值，按全局参数 `season` 的连续值插值，换季转场时随之慢慢过去；
  - 冬季接近白，表示积雪；水面不结冰，不改色。
- **天气**：不改地图。
- **不做**：植被季相点（樱、荷、银杏金点之类）、雪地纹理、雨天湿润色。

### 2.5 文字与标注

- **字体**：按 UI-VISUAL：中文思源黑体 SC，英文 Inter，编号、坐标、比例尺用 JetBrains Mono。大地图地图页的标注文字层用 MSDF 字形，这是 UI-VISUAL §11.8 登记的例外（连续缩放、缩放动画中整层 `Scale`，位图字会糊）；光标读数、比例尺数字、左栏和小地图底栏是普通界面文字，用位图字形。MSDF 字形导入时预渲染、在开场雾幕或扉页后预热（UI-VISUAL §11.8、§11.9）。
- **标注类别与数据**：标注写在本图的 `atlas/labels.json` 里（§9），每条记：
  - 中英文，或指向名字来源的 `ref`（本节末"地名来源"）；
  - 类别：区域、地点、地标、道路、水体、外围路名；
  - 锚点或折线；
  - 优先级。
  - 初稿由 48a 的烘焙器从世界模型和区域表生成，人工校订后入库。
- **缩放档与避让**：
  - 大地图按每米像素数分 5 档（§4.2）；
  - 烘焙器对每一档按优先级贪心放置、互不重叠，并给标记图标留出位置，结果写进烘焙产物 `labels.bake`；
  - 运行时只查表，不做避让计算。
- **区域与地点**：
  - 区域表分两级：大区（核心区、南区、西区、西北区、北部生活区、中部、东区，约 7 个），以及地点（如"核心区 · 图书馆""南区 · 东湖"，不超过 255 个）；
  - 地点按其登记点做 Voronoi 剖分，再裁到所属大区，编号烘进 `map_sdf_b.R`，CPU 侧另存一份 8 m 栅格给小地图查区域名（§3）。
- **地名来源**（UI-NAV §8 的文本约定）：区域与地点表是区域名、地点名（含楼名）的唯一来源。`labels.json` 里区域、地点、地标类的标注只写 `ref`（区域、地点或标记的 ID），文字从来源取，不另写中英文；只有道路、水体、外围路名这类别处没有来源的标注自己写中英文。点位名取 `waypoints.json`，标记名取 `markers.json`（§9）。
- **按语言排布局**（WP-48a）：`labels.bake` 每种界面语言各一份，每份含 5 个缩放档，排的是实际显示的文字：中文界面主名用中文，Z4 的副名用英文；英文界面主名用英文，Z4 的副名用中文。
  - 文字框按运行时同一套字体、字号、字距和晕宽量出（引擎侧用 `Font.GetStringSize` 量好，交给烘焙器），放置仍由纯 .NET 的贪心算法做，运行时只查表。
  - 布局用 1920×1080 基准的逻辑像素；界面缩放让地图页和文字一起放大，相对位置不变，所以不按缩放另排。
  - 两种语言在首次运行时都排好；切换语言时直接换表，不现排。
- **标注布局的缓存键**：在底图的键之外另含标注输入的哈希：`labels.json`（含区域与地点表）、`markers.json` 与 `waypoints.json` 里影响图标位置和名字的字段、字体文件、排版参数（字号、字距、晕宽、图标留白、各档 px/m）和语言。其中任何一项变了只重排标注，不重烘 SDF（失效规则见 SYS-MAPS §2.4）。

## 3 小地图（WP-48b）

**位置与外形**：

- 位置和尺寸照 UI-VISUAL的画面布局（漫游态、界面态两张图）：
  - 左上角，锚在 (32, 32)（1920×1080 基准，随界面缩放）；
  - 外框 244×272：上方是 226×226 的地图视口，下方是 28 px 的底栏。
- 按 UI-VISUAL的形状：外框只切左上一个 45° 角（面板档，12 px），切角旁一个过孔灯，发丝线边。视口顶边中央一个等宽的"N"小标签。
- 右缘 x = 276，界面态加上两个按钮到 x = 320，都在画面中央 60%（x ≥ 384）之外（G14）。
- **配色**：小地图是常驻件，外框用深屏面板（`screen` @90%），视口用 `map.*` 的深屏一套，白天也是深底亮线；设置"常驻件：随主题"时才随主题（UI-VISUAL）。

**朝向**：

- **北朝上**（默认）：底图不转，玩家箭头旋转。这与 SYS-VIEW §4"晷针指北"一致，也利于建立心智地图。
- **随镜头**（设置项）：底图按镜头偏航反向旋转，图标保持正立，"N"标签沿视口边沿移动，始终指向北。

**内容**：

- 显示：
  - 底图；
  - 锚点、可进入室内的门口、自定义标记、追踪目标；
  - 玩家：主色箭头加视锥。箭头的位置与朝向读 `IObserverState`（观者在哪）；视锥读 `ICameraRig`（画面从哪看），视锥角等于当前的水平视场角，长度为视口半宽的 0.3（ENG-ROADMAP §3 的写法纪律）；
  - 底栏左侧是区域名，右侧是比例尺。
- 不显示：地名标注、观景点、地标和设施图标、网格、坐标。
- 观者在可进入的室内时（`IObserverState` 的所在空间）：
  - 小地图仍画室外底图，所在楼的轮廓用 `map.interior` 高亮；
  - 底栏的区域名后面加楼层，例如"核心区 · 图书馆 · 2F"，楼层取 `IObserverState` 所在空间 `NavSpace` 的楼层（ENG-ARCH §3）。

**自动缩放**（W 是视口中心到边的距离，单位 m）：

| 状态 | W |
| --- | --- |
| 行走 | 固定 120 m，约 0.94 px/m，看得清门口和小路 |
| 旁观 | W = clamp(120 + 1.1·h + 2.5·v, 120, 1300)。h 是离地高度，取常驻的低精度地形，与 SYS-VIEW §2.1 的速度律同源；v 是当前水平速度（m/s）。例如离地 100 m、12 m/s 时 W ≈ 260 m；在 P00 的高度上 W 取到 1300 m，能看见整个校园 |

- W 用临界阻尼弹簧跟随（阻尼比 1，响应约 0.6 s），不跳变。
- 界面态下，鼠标在小地图上滚动滚轮，可以乘一个手动倍率 ×0.5–×2，只在本次运行中有效。

**显隐与透明度**：

| 情况 | 呈现 |
| --- | --- |
| 漫游态、界面态 | 显示。界面态里 4 s 没有操作、其余界面淡出时（UI-NAV §3），小地图**不淡出** |
| 设置为"移动时显示" | 镜头或角色移动时显示；静止 3 s 后在 0.6 s 内淡出 |
| 设置为"关"，或按 N 关掉 | 不显示；按 N 恢复到原来的模式，并给一条临时提示 |
| 镜头在室内、地下、水下 | 整体不透明度降到 0.6 |
| 传送、跳转的雾幕段，换图雾幕 | 跟着雾幕淡出，雾幕散开时淡回 |
| 纯净模式（U）、`--ui=0`、过场、开始页、返回标题途中（SYS-TITLE的 Title、Entering、Leaving 三个状态）、存档页、大地图打开、设置卡打开（UI-NAV §4.11） | 隐藏 |

- 面板本身按 UI-VISUAL的深屏约 90%（常驻件固定深屏；"随主题"时白天约 94%）；地图视口是不透明的底图，不透出 3D 画面。

**区域名**：

- 每秒 10 次用 `IAtlasService.RegionAt()` 按观者位置（`IObserverState`）查 8 m 区域栅格。
- 新区域要连续保持 1 s 才换字，避免在区域交界处来回跳。
- 中英文跟随界面语言。

**追踪指示**：

- 同一时间只追踪一个目标，可以是标记、自定义标记或路线（§4.6）。
- 目标在视口里：图标外加一圈主色环。
- 目标在视口外：在视口边沿贴一枚指向它的小箭头，旁边用等宽字写距离（10 m 取整，1 km 以上写"1.2 km"）。
- 追踪路线时，在小地图上用主色画出这条路线的折线（数据来自导航图，§5）。路线折线宽 2 px（1080p 基准，随界面缩放），色 `primary.line`，节点处不画圆点；大地图上选中或追踪的路线同样画法。这是 UI-VISUAL"2 px 只给选中指示条"之外唯一的 2 px 线。
- 到达后（行走 15 m 内，旁观 40 m 内），追踪自动结束，小地图上的图标亮一下就停，不做循环动画。
- 3D 画面里没有任何追踪标记。

**界面态的附加件**：

- 视口右侧贴两个 36 px 的图标按钮，上下排列：[大地图 M]、[目录 G]（位置见 UI-VISUAL的界面态布局）。漫游态时不显示这两个按钮。
- 单击小地图也打开大地图。

**读屏**：

- 小地图的可读名字是"小地图，核心区 · 图书馆"。
- 漫游态不可聚焦；界面态下，按 Tab 先经过左上角区域的两个按钮（UI-NAV §8）。

## 4 大地图（WP-48b）

### 4.1 布局与开合

```
┌────────────────────────────────────────────────────────────────────────────────┐
│ (376,30) 清水河校区  QINGSHUIHE CAMPUS · MAP 01/01  [秋·17:30·晴]   [× 收起 M] │
│(32,104)┌图层│目录 320┐(376,104)┌地图页 1512×900 ───────────────────────────────┐ │
│        │☑ 锚点   13 │         │┌光标 30.7508°N 103.9238°E · 距此 412 m┐   [+] │ │
│        │☑ 观景点 24 │         │     ┌传送卡 372┐                          [−] │ │
│        │☑ 可进入室内│         │     │缩略图    │──╲ 走线                  [⌖] │ │
│        │☑ 路线      │         │     │西湖·隔湖望馆│  ╲                    [▣] │ │
│        │☐ 地标      │         │     │[传送][飞过去]│  ⊕ 选中的锚点            │ │
│        │☐ 设施      │         │     └──────────┘       ▲ 玩家 + 视锥        │ │
│        │☑ 我的标记  │         │      外围淡线 · 校界虚线 · 箱庭外缘          │ │
│        │ 图例 …     │         │[滚轮 缩放][拖动 平移][右键 标记][Esc 返回] 100 m│ │
│        └────────────┘         └────────────────────────────── © OSM ────────┘ │
└────────────────────────────────────────────────────────────────────────────────┘
```

- **尺寸**：照 UI-VISUAL的大地图框架：左栏 320 宽，锚在 (32, 104)；地图页 1512×900，锚在 (376, 104)。地图页左上切角 14，面板材质按 UI-VISUAL；大页可选轻度磨砂，有"降低透明度"开关。
- **主题**：大地图是大页，随昼夜主题，用 `map.*` 对应的一套（UI-VISUAL）。
- **声音**：打开 `PageOpen`、关闭 `PageClose`，打开期间环境声压低 4 dB，关上后 0.4 s 恢复（SYS-AUDIO）。
- **背后**：3D 画面照常运行，压一层 UI-VISUAL的遮罩（scrim）。世界**不暂停**，时间照常流逝，与 Esc 不暂停世界一致（UI-NAV §5）。
- **标题行**：图名（中英）、等宽的"MAP 01/01"、当前季节·钟点·天气的小标签、[收起 M]。以后有多张图时，"MAP 01/01"变成地图切换标签（§9）。
- **右侧按钮**：[+]、[−]、[⌖ 回到玩家]、[▣ 全图]。
- **光标读数**：左上角，等宽，写经纬度和"距此多少米"，在外围时加"校外 · 不可前往"。
- **底部**：按键提示条、比例尺和 OSM 署名。署名必须在地图页上（ODbL），SYS-VIEW §6 的常驻署名照旧。
- **开合**：
  - 按 UI-VISUAL的"大地图开合"编排：压暗 220 ms，地图页从小地图所在的左上角斜向扫开（约 420 ms，贝塞尔 (.2,.7,.2,1)，不回弹）；关闭 220 ms，不再扫描。时长以 UI-VISUAL为准。
  - "减少动态效果"开启时，按 UI-VISUAL的替代：整页 200 ms 淡入，没有位移和扫描。
- **大地图打开时**：
  - 小地图和其余界面隐藏；
  - 取得输入占用 `ICameraRig.LockInput(InputLockReason.Atlas)`（ENG-ARCH §3），关闭大地图时释放；占用期间不接受观者输入，行走模式下角色站住不动；
  - WASD 和方向键改作平移地图。
  - 单键快捷键只认 M、G、N、Esc 和本页内的键（§4.7），其余的（T、H、Y、J、P、`.`、F、1–9、`[` `]`、U）在地图打开期间不响应。
- **控件预建**：大地图的控件在开场雾幕或扉页之后预建、隐藏（UI-VISUAL的预热规则），第一次按 M 不建控件、不编译着色器（G16）。

### 4.2 缩放与平移

- **缩放**：0.4–3.0 px/m 连续缩放。滚轮一格 ×1.25，以光标为中心，220 ms 缓动。
- **缩放档**（决定标注和图标的取舍）：

| 档 | px/m | 显示 |
| --- | --- | --- |
| Z0 | < 0.6 | 大区名、锚点、外围路名 |
| Z1 | 0.6–0.9 | 再加地标名、观景点、路线起点 |
| Z2 | 0.9–1.4 | 再加可进入室内的门口、地点名、水体名 |
| Z3 | 1.4–2.1 | 再加主要道路名、设施（图层开着时） |
| Z4 | ≥ 2.1 | 再加次要道路名、英文副名 |

- **打开时的初始视图**：按 M 打开，以玩家为中心、0.8 px/m；按 G 打开，"全图"适配地图页。
- **平移**：左键拖动；WASD 或方向键每秒 600 px；Home 回到玩家；0 键全图。
- 缩放动画进行中，文字层按比例缩放；动画结束后，按新的缩放档重画一次（§10）。

### 4.3 图层、筛选与图例

- 左栏"图层"页是一组开关：锚点、观景点、可进入的室内、路线、地标、设施、我的标记，另加"外围街区"。
- 默认：设施和地标关，其余开。每行写当前图里的数量。
- 筛选偏好存 `user://settings.cfg`（§12），不进存档。
- 图层下面是图例，写明各类标记的图标和状态：可前往、此处、追踪中、未解锁（RPG 预留，观光模式下这一行灰显）。

### 4.4 目录页（G）

- **目录**是左栏的第二页，也就是 UI-NAV §4.5 的点位目录。
- **分组**：按区分组（总览、核心、南、西、北、东、边界，沿用 `WaypointGroup`）。组内依次是锚点、观景点、可进入的室内、路线，各带类型图标。
- **列表项**：96×54 缩略图（§10）、中英双行名称、编号。点位标"仅旁观"的，加旁观图标。
- **"此处"**：沿用 UI-NAV §4.5 的规则——镜头在 40 m 以内、朝向偏差在 60° 以内，就标"此处"，用 UI-VISUAL的过孔灯表示。
- **联动**：在列表里移动焦点时，地图页平移到该项并高亮它，不改缩放档。
- 列表用 `ItemList`，一个列表只算 1 个 Control（§10）。
- 没有搜索框，也没有文字输入。

### 4.5 传送卡

- **打开**：点任意标记，或在目录里按 Enter。
  - 卡片宽约 372，用 UI-VISUAL的实底面板，左上切角；
  - 一根 45° 走线从标记连到卡片。这是 UI-VISUAL的"走线"母题，只表示连接。
  - 卡片放在不遮住该标记的一侧。
- **内容**：
  - 缩略图 336×189；
  - 名称 h3，以及英文名；
  - 等宽元信息，例如"P05 · 西区 · 30.7516°N 103.9229°E"；
  - 标签行：可达模式（旁观 / 行走 / 仅旁观）、推荐状态（例如"推荐 夏 15:30 · 晴"）；
  - 可进入的室内另写开放范围（取自 WLD-INTERIORS的开放清单）。
- **按钮**：

| 标记 | 按钮 |
| --- | --- |
| 锚点、观景点、可进入的室内、路线 | 【传送】（主按钮）、【飞过去】、【追踪】 |
| 地标、设施 | 【追踪】 |
| 自定义标记 | 【追踪】、形状切换、【删除】 |
| 当前就在这里（20 m 以内） | 【传送】【飞过去】灰显，写"此处" |

- **主按钮**：默认是【传送】。设置项"地图上的前往方式"可以改为以【飞过去】为主按钮（§12）。
- **推荐状态**：卡片不提供"应用推荐"。到达后，到达标题下的小签照旧提供 [应用]（UI-NAV §4.8）。
- **预取**：卡片一打开就预取目的地；鼠标在标记上悬停 300 ms、在目录项上停 150 ms 也预取。同一时间只预取一个目的地（UI-NAV §7.5）。

### 4.6 自定义标记与追踪

- **放置**：在地图页空白处右键，放一枚自定义标记，同时弹出一个小浮层，可以换形状、追踪或删除。
- **ID**：在本存档的这张图内递增的整数，从 1 开始，删掉的号不复用；不用 `Guid.NewGuid`（G4 禁用，SYS-SAVE）。
- **形状**：菱形、圆、方、三角、星、旗，共 6 种，单色线框，按 UI-VISUAL的图标规格；靠形状区分，不靠颜色。不用任何植物图案。
- **上限**：每张图 50 枚。满了时提示"标记已满（50）"。
- **名称**：自动编为"标记 01"……
  - 观光期不做文字备注：那需要文本框和输入法，现在的界面里没有文本框（UI-NAV §5）。
  - 契约里留了 `Note` 字段，以后再开。
- **能做的事**：可以拖动、追踪、删除；**不能传送**。
- **追踪**：
  - 卡片上的【追踪】，或在标记上右键选"追踪"；
  - 同一时间只追踪一个；再点一次，或追踪别的目标，就取消原来的。
  - 追踪状态进存档（§12）。

### 4.7 大地图内的按键

| 键 | 作用 |
| --- | --- |
| M | 关闭大地图，回到打开前的状态（从漫游态打开的，回到漫游态） |
| G | 在"图层"和"目录"两页之间切换；从漫游态按 G，直接打开目录页 |
| Esc | 先关传送卡或浮层；没有卡片时关闭大地图，留在界面态（UI-NAV §5） |
| 滚轮、+、− | 缩放 |
| 拖动、WASD、方向键 | 平移 |
| Home、0 | 回到玩家、全图 |
| Tab | 在左栏、地图页上的传送卡之间移动焦点 |
| ↑ ↓、Enter（目录里） | 选择；Enter 打开传送卡 |
| Enter、空格（卡片上） | 主按钮（默认【传送】） |
| F（卡片上） | 【飞过去】。大地图打开时不切换观察模式 |
| T（卡片上） | 【追踪】 |
| Delete（自定义标记上） | 删除 |
| 右键 | 放置自定义标记，或打开标记的小菜单 |

- 全程只用键盘就能完成一次传送：G → ↑↓ → Enter → Enter（G14）。

## 5 标记类型

| 类型 | 数量（清水河） | 图标（UI-VISUAL图标体系） | 可传送 | 落点 | 数据来源 | 最低显示档 | 小地图 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 锚点 | 13（6 亭 + 7 桩） | 光学基准点：环加中心点加四根刻线；导览亭外加一圈细方框 | 是 | 旁观 `Fly`；行走 `Stand`（§6） | `atlas/markers.json`；实体在 `overlay/atlas.overlay.json`（§9） | Z0 | 显示 |
| 观景点 | P00–P23，共 24 个，其中 P16–P23 是 WLD-INTERIORS新增的室内与高处点位（UI-NAV §6） | 取景框；仅旁观的加旁观图标 | 是 | 照点位的 `Fly`、`Stand`、`Entry` | 引用 `waypoints.json`，不复制位姿 | Z1 | 不显示 |
| 可进入的室内 | 每个开放入口一枚（第一批 15 处，约 28 枚，含学知一组团的闸机；以 WLD-INTERIORS §2.16、WLD-INTERIORS §2.17 的入口为准） | 门加箭头；所在楼的轮廓用 `map.interior` 高亮 | 是，到门口 | WLD-INTERIORS给出的门外落点 `Stand`；`Fly` 在门外后上方（§7.3） | WLD-INTERIORS的开放清单与室内索引，运行时生成，不写进 `atlas/markers.json` | Z2 | 显示 |
| 路线 | R1–R5，加 WLD-INTERIORS新增的 R6–R14（室内串联路线） | 走线起点：一枚过孔接一段 45° 折线（UI-VISUAL）；选中或追踪时画出整条折线 | 是，到起点 | 路线第一个节点 | SYS-WALK §11 的路线表与 WLD-INTERIORS的新路线；折线取导航图（WP-44） | Z1 | 只在追踪时画折线 |
| 地标 | 约 12 | 地标线描加名字 | 否，只能追踪 | — | `atlas/markers.json` 列实体 ID | Z1 | 不显示 |
| 设施 | 约 40 | 按用途：餐饮、医疗、直饮水、休息点、快递、超市、咖啡、书店 | 否，只能追踪 | — | 世界模型的类别，加 MAP-QSH-ADDITIONS §2.13 摆放的直饮水台和休息点；`atlas/markers.json` 可增删 | Z3 | 不显示 |
| 自定义 | 每图 ≤ 50 | 6 种形状 | 否，只能追踪 | — | 存档（§12） | Z0 | 显示 |
| 任务（预留） | — | — | — | — | 以后的 RPG | — | — |

- **状态**（UI-VISUAL的"通电"隐喻）：
  - 可前往：线框；
  - 此处：实心；
  - 选中：衍射光边，只给键盘焦点和选中项；
  - 追踪中：外加主色环；
  - 未解锁（RPG 预留）：虚线环，名字灰显。
- 只有锚点、观景点、可进入室内的门口、路线起点可以传送。不提供"右键任意一点传送"。
- **可进入室内的标记由开放清单驱动**（数据以 WLD-INTERIORS为准）：
  - 地图加载时，`IAtlasService` 读 WLD-INTERIORS的开放清单（`game/data/maps/qingshuihe/interiors.json`）和室内索引（`IWorldModel.Interiors`），为每个 `Open` 的入口生成一枚 `Interior` 标记，ID 写作 `door:<入口 ID>`；
  - WLD-INTERIORS提供每个入口的位置与门外落点：入口声明里的门外点 `Outside`（门中心沿朝向外推 2.5 m，WLD-INTERIORS §2.17）就是行走落点，门中心与朝向取 `EntranceDecl.Center`、`EntranceDecl.YawDeg`（WLD-INTERIORS §15）；楼名取世界模型里这栋楼的名字，开放范围的一句话说明取开放清单（传送卡上显示）；
  - 再把清单里的楼在大地图上画出轮廓高亮。
  - 清单里加一栋楼，地图上就自动多出它的门口和高亮，`atlas/markers.json` 和底图都不用改。
  - G18 照样核对这些标记与门的位置（§14）；门对位、门外节点接路网归 WLD-INTERIORS的 G19。
- **观景点**：室内与高处的新观景点就是 WLD-INTERIORS的 P16–P23，清单写在 UI-NAV §6；本文件不另编号。目录页的分组不变。

## 6 锚点实体：导览亭与导览桩（WP-13 做部件，WP-48c 登记，WP-44 配合选点）

锚点是场景里的物件，字一律用简体中文（INV-4），按 MAP-QSH-ADDITIONS §0 的 R1–R7 写默认状态、夜间和四季。

- **不用银杏，不用蓝色全息投影，不发光、不旋转、不做"解锁特效"**。界面主色清蓝不进 3D 材质（UI-VISUAL）。
- 它们本来就是校园里该有的导览设施，回答"该往哪走"，与 MAP-QSH-ADDITIONS §2.13 的步行服务设施是同一个家族。
- 不能做成可以点选的东西：在 3D 里点它不会有任何反应。

**两种形态**：

| 增补物 | 外观 | 尺度 | 默认状态 | 夜间 · 四季 | 依据 |
| --- | --- | --- | --- | --- | --- |
| **导览亭**（大节点，MAP-QSH-ADDITIONS §2.13） | 深灰钢框、两根侧柱，顶上一块薄平顶棚，挑出 0.6 m。<br>牌面印的就是本作的平面地图：由 §2 的同一份 SDF 离线渲染，配色取场景调色板，暖米白楼块、浅灰路、水面取西湖的水色，不用界面令牌。<br>牌面按"面向牌子时的前方朝上"旋转，这是现实导览图的常见做法。<br>红点加"您在此处"四个字；下沿一行"电子科技大学清水河校区"，右下角小字是锚点编号，例如"A02"。<br>牌背是本区区名和简化的区内平面。<br>不加木檐（MAP-QSH-ADDITIONS §5 第 13 条） | 牌面 1.6×1.0 m，下沿离地 0.9 m；总高 2.6 m，宽 2.0 m，顶棚深 0.8 m | 受光；凑近能认出图书馆的八角和两湖的形状 | 灯下：顶棚底一盏向下照的小灯照亮牌面，三级照度，暖色，属于初灯组，整夜亮（MAP-QSH-ADDITIONS §8.4）。<br>四季：顶棚和牌顶接季（秋落叶、春花瓣、冬积雪） | 相当于原神七天神像、鸣潮共鸣枢纽那一级的大节点，但做成真实校园的导览设施 |
| **导览桩**（补空） | 深灰钢方柱，截面 0.2×0.2 m，顶面朝观者 30° 斜切。<br>斜面嵌一块 0.18×0.14 m 的蚀刻铝牌：编号（例如"A08"）、地名，以及以本桩为中心约 200 m 的线刻平面（同一份 SDF 导出，单色）。<br>桩身朝路一侧离地 0.85 m 嵌一盏暖色小灯，乳白灯罩，直径 4 cm。<br>下面一块 0.3 m 见方的深灰石材底座 | 高 1.05 m | 剪影；斜面受光，铝牌映出一点天光 | 灯源：小灯三级、暖色，地面留一块直径约 1.5 m 的小光斑；初灯组，整夜亮。<br>四季：斜面接季，冬季斜面积一层雪，铝牌露出半截 | 作用相当于星穹铁道的时空锚点、鸣潮的共鸣信标，但只是一根低矮的指示桩 |

- **碰撞**（`walk` 列，MAP-QSH-ADDITIONS §7）：
  - 导览亭：阻挡，侧柱用 `CylinderShape3D`，牌面用薄 `BoxShape3D`；
  - 导览桩：阻挡，用 `CylinderShape3D`。
  - 都由 WP-13 用部件参数生成（SYS-WALK §13）。
- **放置**：
  - 在主网的大拍节点或节点旁（SYS-WALK §10），离路缘 1.5 m 以上，不挡主人流；
  - 牌面或斜面朝向广场或主要来人方向；
  - 相邻锚点之间约 250–450 m。
- **夜里认得出**：导览桩的暖色小灯在远处是一个低亮度的点，与 MAP-QSH-ADDITIONS §8"初灯"的语言一致；白天靠它所在的大拍节点和地标来认（SYS-WALK §6 的 weenie 原则）。
- **登记**：
  - 每个锚点在 `atlas.overlay.json` 里有一条 `add` 操作，ID 是 `design:anchor:A01`……，`Props` 写形态（亭或桩）、朝向和编号，并写明理由（ENG-ARCH §3）；
  - `atlas/markers.json` 的锚点条目引用这个 ID，G18 核对两者相差不超过 0.5 m。

**锚点表**（坐标是按各区文件的 OSM 锚点推出的初值，y 取地面；最终由 WP-48c 与 WP-44 按截图和 SYS-WALK §15 的落地检查定，§16）：

| # | 名称 | 形态 | 位置 (x, z) | 朝向 | 所在节点 | 说明 |
| --- | --- | --- | --- | --- | --- | --- |
| A01 | 南门 | 亭 | (−495, 690) | 35° | 南门内广场 | 朝主轴，牌面与校名墙同框 |
| A02 | 时间广场 | 亭 | (−100, 168) | 307° | 时间广场偏南，兼顾图书馆前 | 朝图书馆正门，日晷在侧 |
| A03 | 东湖 | 亭 | (−15, 395) | 102° | 东湖西岸，草阶看台旁（东湖跑道的里程 0 点在吴立人像旁 (200, 292)，MAP-QSH-SOUTH §4.7，不随 A03 挪动） | 朝宣邦楼；不挡草阶看台和荷角（P09 的前景） |
| A04 | 品学楼 | 亭 | 约 (120, −65)（初值，§16） | 约 90°（朝门框，初值） | 品学楼门框前广场：叠水喷泉（约 (140, −65)，MAP-QSH-CENTER-EAST §2.3）西侧约 20 m | 朝门框横梁 |
| A05 | 北门 · 商业街 | 亭 | (−90, −765) | 165° | 北门内，兼顾商业街北口 | 朝校内 |
| A06 | 体育中心 | 亭 | (−165, 705) | 100° | 体育馆、游泳馆之间的大平台；y 取平台顶面（约 5.0 m，WLD-INTERIORS §2.17），不取地面 | 朝体育场 |
| A07 | 西湖 · 博物馆 | 桩 | (−330, 95) | 250° | 博物馆前木栈道口 | 朝湖 |
| A08 | 校训石 | 桩 | (−535, 255) | 290° | 六号科研楼前 | 朝校训石与雪山方向 |
| A09 | 研究院 | 桩 | (−470, −130) | 20° | 经管楼与研究院之间 | 补西北空档 |
| A10 | 湿地 | 桩 | (−205, −330) | 250° | 湿地栈道南口 | 朝芦苇荡 |
| A11 | 银杏苑 | 桩 | (70, −470) | 310° | 银杏苑与学知苑之间 | 补北区西半 |
| A12 | 思学苑 | 桩 | (330, −300) | 15° | 思学苑门前 | 补北区东半 |
| A13 | 东区 | 桩 | (450, 110) | 60° | 文科楼与综合训练馆之间 | 朝训练馆筒拱 |

- **落点规则**（§7.3）：
  - `Stand` 在锚点侧前方约 2 m，面向它朝向的广场；
  - `Fly` 默认在锚点后方 8 m、上方 6 m，俯仰 −12°。
  - 两者都写进 `atlas/markers.json`，可以逐个调整。
- **不另设导览牌**：导览设施只有这 13 个锚点；图书馆前由 A02、商业街北口由 A05、博物馆前由导览桩 A07 承担。

## 7 传送流程（WP-48c 调用，WP-36 实现过渡）

### 7.1 两种方式

| 方式 | 入口 | 过渡 | 用途 |
| --- | --- | --- | --- |
| **传送**（默认） | 传送卡的主按钮、目录里按 Enter | 雾幕瞬移（§7.2）。新增 `TransitKind.Teleport`，由 `TransitOptions.Prefer = TransitPreference.Teleport` 触发（§11） | 原神式的"直接到"；省时间 |
| **飞过去** | 传送卡的【飞过去】、F；数字键 1–9 / 0 和 `[` `]` | UI-NAV §7.1 的三种方式：滑移、弧线、雾幕弧线，按距离自动选 | 保留地理感，看沿途。连续飞行时声景随位置连续变化，飞行风按速度变（SYS-AUDIO） |

- 两者共用同一套机制：`ICameraRig.Transit`、雾幕层（WP-01 的 CanvasLayer 5，UI-NAV §9）、预取与到达保证（UI-NAV §7.5）、到达标题（UI-NAV §4.8）。
- 传送不是"淡出、加载、淡入"那种加载画面：雾幕的颜色取当前地平线的雾色，与开场、跳转是同一种语言（SYS-VIEW §3、UI-NAV §7.1）；没有进度条，也没有等待文字。
- 跨地图的传送（以后有多张图时）走 SYS-MAPS的雾幕加载，不在本节范围内：同图传送一律走 `ICameraRig.Transit`；只有目标标记在别的图上时，`IAtlasService` 才调 `IMapService.Travel`（`MapTravelCause.Teleport`）。

### 7.2 传送的时序

1. 按下【传送】：`PlayUi(TeleportConfirm)`（SYS-AUDIO）；大地图收拢 0.25 s，与下一步同时开始；大地图取得的输入占用（`InputLockReason.Atlas`，§4.1）不随收拢释放，保持到第 6 步还给控制时才释放。
2. **雾幕盖满** 0.3 s，曲线 (.2,.7,.2,1)；雾幕原因写 `VeilReason.Teleport`。雾幕盖起时环境声压低 8 dB、加 1.5 kHz 低通，全遮时声景直接换到目的地（SYS-AUDIO）。
3. **移位**：一帧内把相机放到目的地位姿；清掉相机的插值缓冲（SYS-WALK §5）。
4. **等就绪**：
   - 目的地的 L0+、L0、L1、L2 各环就绪（UI-NAV §7.5 第 5 条）；
   - 行走模式还要等落点周围的碰撞格（`ICollisionWorld.Ready()`，SYS-WALK §14 第 5 条）；
   - 室内目的地还要等这一处开放室内的当前层就绪（WLD-INTERIORS）。
   - 至少停 2 帧，让管线和倒影稳定；最多等 1.2 s，行走模式 1.5 s。
5. **雾幕散开** 0.6 s；散到 30% 时淡入到达标题（UI-NAV §4.8），同时 `PlayUi(TeleportArrive)`；环境声在 0.6–0.8 s 里恢复（SYS-AUDIO）。读屏的 live region 写入"已到达：……"（UI-NAV §8）。
6. 还给控制：旁观模式静止悬停；行走模式落地，最后 0.3 m 交给物理（UI-NAV §7.3）。

- **总时长**约 1.1–2.4 s，取决于第 4 步。
- **"减少动态效果"**开启时，流程不变：雾幕本来就是 UI-NAV §7.6 的替代做法。只有大地图的收拢改为透明度变化。
- 到达后按 SYS-SAVE的"关键动作落定后"请求一次存档（`Request(SaveReason.Settled)`，30 s 合并；快照在 `FramePriority.Save` 那一步拍）。

### 7.3 落点

| 目的地 | 旁观模式 | 行走模式 |
| --- | --- | --- |
| 锚点 | `Fly`：锚点后方 8 m、上方 6 m，俯仰 −12°（可逐个调整） | `Stand`：锚点侧前方约 2 m，面向广场 |
| 观景点 | 点位的 `Fly` | 点位的 `Stand`；"仅旁观"的点位自动切到旁观，并提示一次（UI-NAV §6） |
| 可进入的室内 | 门外后上方，看着敞开的门：从门外落点沿朝外方向后退 8 m、抬高 6 m，俯仰 −12° | WLD-INTERIORS入口声明里的门外点 `Outside`，面向门：门中心沿朝向（门外法线的方位角）外推 2.5 m（WLD-INTERIORS §2.17）。不直接落进室内，让观者自己走进那道暖光（WLD-INTERIORS的"门口暖光池"） |
| 室内与高处观景点（WLD-INTERIORS的 P16–P23） | 点位的 `Fly` | 点位的 `Stand`，在室内或屋顶；传送时直接落在 `Stand`，不走 `Entry` 进场路径（雾幕已经遮住）；飞过去时照 UI-NAV §6 走进场路径（这些点位都是 `Entry = Door`，WLD-INTERIORS） |
| 路线起点 | 起点上方 30 m，朝路线前进方向，俯仰 −20° | 起点，朝路线前进方向 |

- 每个可传送标记的 `Stand` 都要过 SYS-WALK §15 的落地检查（G15）；`Fly` 位姿的介质必须是空气，不在楼体里（G18）。

### 7.4 预取与到达保证

- 沿用 UI-NAV §7.5：
  - 卡片打开时，或在标记上悬停 300 ms、在目录项上停 150 ms 时，把目的地各环、碰撞格和室内提供者插到加载队列最前面；
  - 同一时间只保留一个目的地。
- 到达帧要求目的地各环就绪，否则雾幕继续停，上限见 §7.2 第 4 步。
- 传送帧和雾幕期间的帧不计入自适应画质的统计（ENG-PERF §11.3）。

### 7.5 打断与边界情况

- 雾幕开始升起之前（按下后约 1 帧内），按 Esc 取消；雾幕一旦开始升起，就不能取消，先完成移位再交还控制，与 UI-NAV §7.4"在雾幕阶段打断"一致。
- 传送进行中再点别的标记：忽略，到达后才能再传。
- 目的地就在 20 m 以内：两个按钮灰显，写"此处"。
- 换季转场、换天气过渡、快进时间进行中，也可以传送：雾幕和这些过渡叠在一起，不额外处理。
- 换图雾幕、过场、开始页期间，地图打不开，也不能传送。

## 8 解锁策略（WP-48c）

- **三种状态**：
  - `Hidden`：不画；
  - `Known`：画虚线环，名字灰显，不能传送；
  - `Unlocked`：正常。
- **观光规则集取 `all-unlocked`**：所有标记一开始就是 `Unlocked`，底图全图揭示。这是现在唯一启用的策略。
- **`proximity`**（只留接口，给以后的 RPG）：
  - 距离按 `IObserverState` 的观者位置算：行走时进入锚点 8 m 以内，或旁观时在锚点 25 m 以内、离地 25 m 以下，该锚点变为 `Unlocked`；
  - 到访导览亭后，所在大区的导览桩变为 `Known`，该区底图揭示；
  - 未揭示的区域在 `map_base` 里只画建筑和水岸的发丝线，不填色，像"还没曝光的版图"，不用黑雾；揭示状态用 64 位掩码 uniform 传给着色器（§2.2）。
  - 进出判定用不同的半径防抖（例如进 8 m、出 12 m）。
- **3D 里没有任何解锁表现**：导览桩的灯按 MAP-QSH-ADDITIONS §8 的时刻表亮灭，与解锁无关。解锁的反馈只在界面上：小地图上的图标通电一次，再加一条临时提示（UI-NAV §4.9）。
- **策略的选择**：由 `IAtlasService.PolicyId` 给出。新游戏时取规则集的 `UnlockPolicy` 写进存档（§12），需要按图覆盖时再给 `map.json` 加字段；读档时照存档恢复。观光规则集的存档里恒为 `all-unlocked`。

## 9 多地图下的数据（WP-48c，与 SYS-MAPS一致）

- **地图标识**：一律用 `MapId("qingshuihe")`，不用 "qsh"。`MarkerId` 只在一张图内唯一，跨图引用时写成"地图 + 标记"。
- **目录**：平面地图自己的数据一律放在本图的 `atlas/` 子目录（SYS-MAPS）；不用 `map.json` 这个文件名，它已被 `MapDescriptor` 占用。

| 路径 | 内容 | 谁写 | 进不进 git |
| --- | --- | --- | --- |
| `game/data/maps/qingshuihe/atlas/markers.json` | 缩放区间与缩放档；地图页范围 `atlasRect`（清水河 ±1536 m，§2.3；全分辨率部分的范围取 `map.json` 的 `dataRect`，不在这里另写）；锚点（ID、名称、形态、位置、`Fly`、`Stand`、引用的覆盖层 ID）；地标、设施的增删和优先级；隐藏清单 | WP-48c | 进 |
| `game/data/maps/qingshuihe/atlas/labels.json` | 标注：文字（中英）或名字来源的 `ref`、类别、锚点或折线、优先级；区域与地点表（区域名、地点名的唯一来源，§2.5） | WP-48a 生成初稿，人工校订 | 进 |
| `game/data/maps/qingshuihe/overlay/atlas.overlay.json` | 锚点实体的 `add` 操作，`design:anchor:A01`…（§6）；覆盖层照 SYS-MAPS的规则放在本图的 `overlay/` 下 | WP-48c | 进 |
| `game/data/maps/qingshuihe/waypoints.json` | 点位 P00–P23，观景点标记引用这里 | WP-36（UI-NAV §6） | 进 |
| `game/data/maps/qingshuihe/interiors.json` 与室内索引 | 开放清单与入口（WLD-INTERIORS），运行时生成可进入室内的门口标记 | 清单：主代理（格式与 schema 归 WP-51，条目由负责包填写，SYS-MAPS §2.1）；室内索引：WP-51 | 进 |
| 本机缓存 `user://cache/bake/qingshuihe/<ABI>/map/<哈希>/` | `map_sdf_a`、`map_sdf_b`、`map_sdf_out`、`map_sdf_ring`、`map_glyphs`、`labels.bake`（每种界面语言一份，§2.5）、8 m 区域栅格、`map.manifest.json` | WP-48a 的烘焙器（本机烘焙） | 不进，也不随包发（ENG-ARCH §4、SYS-MAPS §2.4） |
| `game/assets/maps/qingshuihe/thumbs/` | 标记缩略图（§10） | WP-27 的截图机 | 进 |

- **按图的**：底图与烘焙产物、标记、标注、区域表、锚点实体、缩略图、自定义标记、解锁状态。
- **全局的**：`map_base.gdshader`、`map.*` 色名（UI-VISUAL的 `ui.json`）、图标、筛选偏好和小地图设置（`settings.cfg`）。
- **换图**：
  - `IAtlasService` 实现 SYS-MAPS的 `IMapAware`；
  - 卸载回调里释放本图的五张地图纹理和缩略图；加载回调里读新图的数据，重建标记，发 `Changed`；
  - 界面收到 `Changed` 后换底图和标记。换图在雾幕里完成，小地图跟着雾幕淡出、淡回。
- **预留**：
  - 大地图标题行的"MAP 01/01"在多图时变成切换标签（3 张以内用分段，更多用下拉）；
  - 以后的楼层图（图书馆各层）作为同一张图的"地图层"，契约里预留 `AtlasLayer`，现在不用。

## 10 实现与性能（WP-48b、WP-48c）

**节点与挂载**：

- 地图界面都在 WP-35 的 `HudRoot`（CanvasLayer 10，UI-NAV §9）里：
  - WP-35 在左上角留一个槽位放小地图，再留一个全屏页的槽位放大地图（槽位的名字由 WP-35 定）；
  - WP-48b 的 `MapHud` 挂进这两个槽位。
- `IAtlasService` 由 WP-48c 用 `[KernelService(typeof(IAtlasService), Owner = "WP-48c")]` 注册（ENG-ARCH §2），放在 `game/systems/atlas`。

**小地图**：

- 控件：
  - 一个外框 Control（UI-VISUAL的 StyleBox）；
  - 一个 226² 的视口 Control，配 `map_base` 材质，`ClipContents = true`；
  - 一个标记层 Control，在地图坐标里用 `_Draw` 画图标；
  - 一个玩家箭头 `TextureRect`；
  - 底栏两个 Label。
  - 共约 8 个 Control。
- **每帧只做三件事**：
  - 用 `SetInstanceShaderParameter()` 写视图（中心、每米像素数、旋转），把 uniform 声明为 instance uniform；
  - 改标记层的 `Position`；
  - 改箭头的 `Rotation`。
- **标记层重画**：缩放变化超过 3%、标记集合变化或追踪状态变化时才重画（≤ 10 Hz）。
  - "随镜头"模式下，图标要逐个反向旋转保持正立，标记层每帧重画；可见图标 ≤ 40 个，用 `DrawTextureRectRegion` 从一张图集里取，计入下面的预算。
- **区域名**：10 Hz 读 `IAtlasService.RegionAt()`，变了才改 `Text`。
- **这是 UI-NAV §9 和 ENG-BUDGET §7 第 4 条的登记例外**：小地图和大地图可以逐帧写着色器参数和变换（`Position`、`Rotation`、`Scale`），以及"随镜头"模式下标记层的重画；仍然不逐帧改文字、尺寸和主题。原因是小地图要跟着镜头平滑移动，10 Hz 会一顿一顿的。

**大地图**：

- 地图页一个 `map_base` 视口；标记层、文字层各一个 `_Draw` 控件，文字用 `DrawString` 画；左栏两页；传送卡；按钮。
- 拖动平移时只改两个层的 `Position`。
- 缩放档变化时重画（≤ 10 Hz）；缩放动画进行中，层按比例 `Scale`，结束后重画一次。
- 列表用 `ItemList`，每个列表只算 1 个。全部 ≤ 120 个 Control，开场雾幕或扉页之后预建、隐藏（§4.1）。

**缩略图**：

- 每个可传送标记一张，由 WP-27 的截图机按推荐状态渲染：
  - 锚点用默认状态；
  - 室内门口用门外 3 m 朝门的视角。
- 目录用 96×54（按 2 倍即 192×108 导入），常驻，约 80 张（锚点 13、观景点 24、室内门口约 28、路线起点 14），约 1.6 MB。
- 传送卡用 336×189（按 2 倍导入），打开卡片时用 `ResourceLoader.LoadThreadedRequest()` 后台读；大地图关闭 30 s 后释放。读取期间卡片先显示列表小图放大后的样子，不显示等待提示。

**预热**（UI-VISUAL、SYS-VIEW §3）：

- `map_base` 的两种 `mode` 和两套主题，在开场雾幕或扉页期间各画一次（SYS-TITLE启动序列的二维预热）。
- 地图纹理的 `ImageTexture` 在雾幕里创建。
- 大地图控件在扉页之后预建。
- 第一次按 M、第一次换主题，Canvas 和管线的编译计数都不增长（G16）。

**预算**：

| 项 | 预算 | 说明 |
| --- | --- | --- |
| 小地图主线程 | 平均 ≤ 0.02 ms/帧；"随镜头"模式 ≤ 0.05 ms | 一次着色器参数、一个 `Position`、一个 `Rotation`（初值，§16） |
| 大地图打开时主线程 | 平均 ≤ 0.15 ms/帧 | 此时其余界面隐藏，界面合计仍 ≤ 0.2 ms（UI-NAV §9） |
| `IAtlasService` | ≤ 0.01 ms/帧；10 Hz 的区域查询和追踪到达检查 | 在 `FramePriority.Systems` 那一步；不新增 `FramePriority` 值。地图加载与切换的 `FramePriority.Map = −1300` 属于 SYS-MAPS |
| GPU | 小地图可忽略（226² 像素、3 次采样）；大地图全屏在最低配置上 ≤ 0.3 ms，磨砂另按 UI-VISUAL | Canvas 通道单独统计（ENG-BUDGET §8） |
| Control | 小地图 ≤ 12，大地图 ≤ 120；`HudRoot` 合计仍 ≤ 400（UI-NAV §9） | — |
| 显存 | 地图纹理约 41 MB；常驻的缩略图约 1.6 MB；卡片缩略图按需 | 计入 ENG-BUDGET §2 的纹理 384 MB |
| 内存 | 标记、标注表、8 m 区域栅格（约 352²，0.12 MB） | — |

- 漫游态的界面开销只有小地图，与 ENG-BUDGET的界面一行一致。

## 11 C# 契约（照 ENG-ARCH §3 的写法；纯 .NET，只增不改）

```csharp
// game/contracts/Atlas/（Campus.Contracts 程序集；Campus.Gen.* 不得引用 IAtlasService，ENG-ARCH §2 的 ArchUnitNET 规则）
// MapId 见 SYS-MAPS；Pose、EntryPath、WaypointStand、LocalizedName、FeatureId、ITransitHandle 见 ENG-ARCH §3
namespace Campus.Contracts.Atlas;

public readonly record struct MarkerId(string Value);
    // 本图内唯一："A03"（锚点）、"P05"（观景点）、"door:<入口 ID>"（可进入的室内）、"route:R1"、
    // "lm:<FeatureId>"（地标）、"fac:<FeatureId>"（设施）、"pin:<整数>"（自定义，例如 "pin:7"）
public enum MarkerKind : byte { Anchor, Viewpoint, Interior, Route, Landmark, Facility, Custom, Quest /* RPG 预留 */ }
public enum AnchorForm : byte { Kiosk, Post }                     // 导览亭、导览桩
public enum UnlockState : byte { Hidden, Known, Unlocked }
public enum TravelStyle : byte { Teleport, Fly }
public enum TravelRefusal : byte { None, NotTravelable, Locked, Here, Busy, Unavailable }
    // Unavailable：过场、开始页、换图雾幕期间
public enum PinShape : byte { Diamond, Circle, Square, Triangle, Star, Flag }

public sealed record TeleportSpec(Pose Fly, WaypointStand? Stand, EntryPath? Entry = null);   // Stand 为 null：仅旁观

public sealed record AtlasMarker(
    MarkerId Id, MarkerKind Kind, LocalizedName Name,
    Vector2 At,                         // 地图坐标 (x, z)，单位 m
    string Icon,                        // UI-VISUAL图标体系里的名字
    int Priority, byte MinZoomBand,     // 缩放档 0–4（§4.2）
    string? Region,                     // 区域 ID（atlas/labels.json）
    TeleportSpec? Teleport = null,      // 只有 Anchor、Viewpoint、Interior、Route 可以非空
    FeatureId? Feature = null,          // 锚点：design:anchor:Axx；地标、设施：实体；室内：楼
    string? WaypointId = null,          // Viewpoint：引用 waypoints.json，不复制位姿
    string? RouteId = null,
    AnchorForm? Form = null);

public sealed record AtlasRegion(string Id, LocalizedName Name, string? Parent);   // 大区的 Parent 为 null
public sealed record AtlasLayer(string Id, LocalizedName Name, float YMin, float YMax);   // 预留：楼层图

public sealed record CustomPin(int Id, Vector2 At, PinShape Shape, string? Note = null);
    // Id：本存档这张图内递增的整数，从 1 开始，不复用；不用 Guid.NewGuid（G4 禁用，SYS-SAVE）。Note 观光期不开放

public interface IUnlockPolicy
{
    string Id { get; }                                            // "all-unlocked"、"proximity"
    UnlockState Initial(AtlasMarker m);
    UnlockState OnVisit(AtlasMarker m, UnlockState now);          // 进入判定圈时调用
}

public interface IAtlasService                                    // Kernel 服务（WP-48c）
{
    long Rev { get; }
    MapId Map { get; }
    IReadOnlyList<AtlasMarker> Markers { get; }                   // 含运行时生成的室内门口与自定义标记
    IReadOnlyList<AtlasRegion> Regions { get; }
    event Action? Changed;                                        // 主线程；换图、标记增删、解锁或追踪变化时发
    AtlasMarker? Find(MarkerId id);
    UnlockState StateOf(MarkerId id);
    string? RegionAt(float x, float z);                           // 8 m 区域栅格；返回地点 ID
    bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why);
    void Prefetch(MarkerId id);                                   // 同一时间只保留一个目的地（UI-NAV §7.5）
    ITransitHandle? Travel(MarkerId id, TravelStyle style);       // 同图只调 ICameraRig.Transit；目标在别的图时才调 IMapService.Travel（§7.1）；拒绝时返回 null
    MarkerId? Tracked { get; }
    void Track(MarkerId? id);                                     // null 取消；同一时间只有一个
    IReadOnlyList<CustomPin> Pins { get; }                        // 当前图
    CustomPin? AddPin(Vector2 at, PinShape shape);                // 满 50 返回 null；新 ID = NextPinId，随后 NextPinId 加 1
    void MovePin(int id, Vector2 at);
    void SetPinShape(int id, PinShape shape);
    void RemovePin(int id);
    string PolicyId { get; }
}

// ENG-ARCH §3 的相机契约增量（只增不改）：
public enum TransitPreference : byte { Auto, Teleport }          // Auto：按距离选滑移、弧线或雾幕弧线（UI-NAV §7.1）
// TransitOptions 增加：public TransitPreference Prefer { get; init; } = TransitPreference.Auto;
// TransitKind 增加：Teleport

// 存档段（SYS-SAVE的 ISaveSection；段 ID 按图展开为 "map:<id>/atlas"）
public sealed record AtlasSave(
    string Policy,                                                // "all-unlocked"
    IReadOnlyList<CustomPin> Pins,
    int NextPinId,                                                // 下一枚自定义标记的 ID；新游戏为 1
    string? Tracked,                                              // MarkerId.Value
    IReadOnlyDictionary<string, UnlockState> Unlock,             // 只存与策略初值不同的项；观光期为空
    IReadOnlyList<string> VisitedAnchors);
// [SaveSection("atlas", Owner = "WP-48c", Version = 1, Scope = SaveScope.PerMap, Order = 40)]，特性与接口以 SYS-SAVE为准
```

- **依赖规则**（ENG-ARCH §2）：
  - `Campus.Hud.Map`（`game/hud/map`）除 GodotSharp 外，只依赖契约、`Kernel.Get<T>()`、`IAtlasService`、`IWorldState`（读）、`IObserverState`（读：玩家箭头与区域名）和 `ICameraRig.Pose`（读：视锥等"画面从哪看"的量，ENG-ROADMAP §3）；
  - 它不直接调 `ICameraRig.Transit`，传送一律经 `IAtlasService.Travel`。
  - `Campus.Systems.Atlas` 可以调 `ICameraRig.Transit`、`ICameraRig.LockInput`、`IViewControl`，读 `IObserverState`（`proximity` 的判定）、WLD-INTERIORS的室内索引（`IWorldModel.Interiors`）和导航图，以及调 `IAudioService.PlayUi`（传送的两声，SYS-AUDIO）。
  - 帧顺序：`IAtlasService` 的 10 Hz 查询在 `FramePriority.Systems`（0）那一步；按 ENG-ARCH的统一表，它在 `Map`（−1300）之后，所以换图那一帧看到的已是新图；存档快照在 `Save`（1000）那一步拍。
- **JSON**：一律用 `ContractJson.Options`（ENG-ARCH §3）。`atlas/markers.json`、`atlas/labels.json` 的 schema 放在 `game/contracts/schemas/`。存档段 `AtlasSave` 走 SYS-SAVE的 System.Text.Json 源生成上下文。
- **不叫 `IMapService`**：那个名字属于 SYS-MAPS的地图加载与切换（WP-46）。

## 12 存档里存什么、设置里存什么

**存进存档**（按图一段 `map:<id>/atlas`，SYS-SAVE）：

| 字段 | 内容 | 读档时 |
| --- | --- | --- |
| `Policy` | 解锁策略 ID：新游戏时取规则集的 `UnlockPolicy`；观光规则集恒为 `all-unlocked` | 照存档恢复 |
| `Pins` | 自定义标记：整数 ID、位置、形状（备注预留） | 位置落在地图页之外的丢弃；超过 50 枚的截断并写日志；ID 重复的保留第一枚 |
| `NextPinId` | 下一枚自定义标记的 ID | 小于等于现有最大 ID 时改为最大 ID + 1 |
| `Tracked` | 正在追踪的目标 | 目标已不存在（标记被删、门口不在开放清单里）时清空 |
| `Unlock` | 与策略初值不同的解锁状态；观光期为空 | 未知 ID 丢弃 |
| `VisitedAnchors` | 到访过的锚点；观光期只用于统计，以后的 RPG 用来解锁 | 未知 ID 丢弃 |

- 新游戏：`Pins`、`Tracked`、`Unlock`、`VisitedAnchors` 都为空，`NextPinId` 为 1，策略取规则集的 `UnlockPolicy`（§8）。
- 这一段只有几 KB，符合 SYS-SAVE的保存耗时要求。保存时机照 SYS-SAVE；本服务在传送到达后请求一次（`SaveReason.Settled`）。
- 自定义标记只存在存档里，不另写 `user://` 文件。

**存进 `user://settings.cfg`**（WP-35 的文件，WP-48b 经 WP-35 的设置接口读写；读值只接受基本类型，SYS-SAVE）：

| 设置 | 取值 | 默认 |
| --- | --- | --- |
| 小地图 | 常驻 / 移动时显示 / 关 | 常驻 |
| 小地图朝向 | 北朝上 / 随镜头 | 北朝上 |
| 地图上的前往方式 | 传送 / 飞过去（决定传送卡的主按钮） | 传送 |
| 图层筛选 | 8 个开关，存成一个位掩码 | 设施、地标关，其余开 |

- 这几项登记在 UI-NAV §4.6 的设置表。

## 13 各包的分工

| 包 | 交付 |
| --- | --- |
| **WP-48a** 底图烘焙与地标贴图 | 底图烘焙器（引用 `Campus.Gen.World`；代码放进随包程序集，由 `IBakeService` 本机调用，D-165；`pipeline/map/` 只留控制台入口）：精确矢量 SDF、外围纹理、区域与地点栅格、`labels.bake`、`map.manifest.json`；`atlas/labels.json` 初稿；地标线描 SVG 与 MSDF 图集；导览亭牌面、导览桩铭牌的贴图（场景调色板，交 WP-04 进图集）；`--bake=map` 接进 ENG-ARCH §4 的烘焙流程 |
| **WP-48b** 小地图与大地图界面 | `game/hud/map/`：小地图、大地图（图层、目录、传送卡、自定义标记、追踪、按键）；`game/style/shaders/map_base.gdshader`；地图相关的设置项；读屏名字；预建与预热 |
| **WP-48c** 标记、锚点、传送与解锁服务 | `game/systems/atlas/`：`IAtlasService`、`all-unlocked` 与 `proximity` 两个策略的实现、开放清单驱动的室内标记、存档段（自定义标记用整数 ID）；`atlas/markers.json`、`overlay/atlas.overlay.json`；传送时的 `PlayUi` 两声；G18 检查器（与 WP-27 合作） |
| WP-13 | 导览亭、导览桩两个部件及其碰撞（§6），按 `atlas.overlay.json` 摆放；不另做单独的导览牌部件 |
| WP-36 | `TransitKind.Teleport` 与 `TransitPreference`：传送时序（§7.2）、到达保证、落点规则 |
| WP-35 | 左上角槽位与全屏页槽位；不做单独的点位目录抽屉（由 48b 的目录页承担）；设置表新增 §12 的四项；临时提示新增"小地图"一条 |
| WP-06 | 输入动作 `map`（M）、`atlas_index`（G）、`minimap`（N），按 `physical_keycode` 绑定，可改键；实现按原因计数的 `ICameraRig.LockInput`，大地图打开期间由 WP-48c 以 `InputLockReason.Atlas` 占用观者输入 |
| WP-27 | 标记缩略图（锚点、点位、室内门口、路线起点）；地标的正交俯视参考图；G18 的跑法；G14 的地图部分；F4 传送压力路线（§14） |
| WP-44 | 锚点和门口落点的选点，以及 SYS-WALK §15 的落地检查；导航图给出推荐路线的折线 |
| WP-39 | 导览亭顶棚灯、导览桩灯接进灯光时刻表（MAP-QSH-ADDITIONS §8.4） |
| WP-04 | 导览亭牌面、导览桩铭牌进"不重复内容"图集；材质库为 `map_base` 建 ShaderMaterial；`map.*` 色名按 UI-VISUAL登记 |
| WP-01 | 雾幕或扉页期间预热 `map_base`、创建地图纹理；`--bake=map`、`--gate=atlas` 的命令行解析；雾幕原因 `VeilReason.Teleport` |
| WP-51 | 开放清单 `interiors.json` 与室内索引里每个入口的门外点、门内点（WLD-INTERIORS），以及开放范围的一句话说明（中英），供门口标记和传送卡使用 |
| WP-53a | 传送的 `TeleportConfirm`、`TeleportArrive`，大地图开合的 `PageOpen`、`PageClose` 与压低；传送雾幕里声景换场（SYS-AUDIO） |

- 规模估计：48a 约 0.9k 行；48b 约 1.6k 行；48c 约 0.8k 行。

## 14 验收

### 14.1 G18 地图一致性（ENG-GATES §2）

G18 地图一致性判据的核心是"标记与覆盖层实体相差不超过 0.5 m，底图哈希与世界模型版本一致"。室内的连通与门对位是 G19（WLD-INTERIORS），存档是 G20（SYS-SAVE），声音是 G21（SYS-AUDIO）。

用无界面 Godot `-- --gate=atlas --map=qingshuihe` 运行，只检查、不渲染，用退出码报告。每张图各跑一遍。

| 子项 | 判据 |
| --- | --- |
| **底图哈希** | `map.manifest.json` 里的世界模型哈希和烘焙代码哈希，与当前的 campus.json、全部覆盖层、outskirts.json 和烘焙器代码一致；不一致就失败，并提示重烘 |
| **标记与实体** | 每个锚点标记与 `atlas.overlay.json` 里的 `design:anchor:*` 一一对应，位置相差 ≤ 0.5 m；地标、设施标记与所引用实体的登记点相差 ≤ 0.5 m；室内门口标记与 WLD-INTERIORS室内索引里同一入口的门中心相差 ≤ 0.5 m，开放清单里每个开放入口都恰有一枚门口标记 |
| **观景点** | 观景点标记引用的 ID 在 `waypoints.json` 里都存在，P00–P23 一个不缺；标记位置等于点位 `Fly` 的 (x, z)（引用，不复制，所以应当完全相等） |
| **落点** | 每个可传送标记：`Fly` 位姿处 `MediumAt()` 是空气；`Stand` 落在可行走遮罩上（SYS-WALK §6）；锚点和门口的 `Fly` 在 WLD-OUTSKIRTS的可达空间内、不在缓冲带里。真实落地由 G15 检查 |
| **页面范围** | 地图页等于 `atlas/markers.json` 的 `atlasRect`，并且包住 `map.json` 的 `dataRect`；箱庭外缘（截面线）被页边裁掉的长度写进报告，大于 0 时报警（只报警，§2.3）；`map_sdf_out` 与 `map_sdf_ring` 在 ±1280 m 边线上的零值线相差 ≤ 0.5 m |
| **底图与几何** | 在全部建筑轮廓上抽样 2000 个点：SDF 的零值线与世界模型轮廓相差 ≤ 0.5 m；地标线描的外框与轮廓相差 ≤ 0.5 m |
| **区域** | 可行走遮罩里的每一格都能查到区域名；`atlas/labels.json` 的每条标注都有中英文 |
| **标注** | 每个缩放档的 `labels.bake` 没有互相重叠的标注，也不压在可传送标记上 |
| **数量** | 锚点 12–14 个，相邻锚点不超过 500 m（只报警）；自定义标记的上限常量是 50 |

- 何时跑：集中测试时全量跑（ENG-GATES §2）。

### 14.2 G14 界面门禁的地图部分（G14 的总则见 UI-VISUAL与 ENG-GATES §2，这里只列地图部分）

- **键盘**：只用键盘完成一次传送：G → ↑↓ → Enter → Enter。脚本用 `Input.ParseInputEvent()` 注入，核对 `IAtlasService.Travel` 被调用、到达帧就绪。另跑一遍"飞过去"（卡片上按 F）。
- **遮挡**：在 1280×720 和 2560×1080 下，小地图（含界面态的两个按钮）不进画面中央 60% 的区域。大地图是全屏页，与设置页一样单独登记，不判中央 60%。
- **对比度**：两套主题下，地图上的文字对其底色 ≥ 4.5:1，可传送标记、玩家、校界和水岸 ≥ 3:1，按 UI-VISUAL登记的 `map.*` 色值自动检测；道路属于上下文，不判。小地图在白天也要按深屏一套测一遍。
- **隐藏**：`--ui=0` 时没有小地图；U、过场、开始页、大地图打开、设置卡打开时小地图隐藏；"关"和"移动时显示"两种设置按 §3 工作。
- **3D 无叠加**：在 `--ui=0` 下，追踪一个标记前后，以及选中、悬停标记前后，各截一张图，像素差在 ENG-GATES §1 的截图容差以内，证明 3D 画面里没有追踪或选中标记。
- **逐帧例外**：调试构建里统计每帧对地图控件 `Text`、`Size`、主题的写入次数，必须为 0；只允许着色器参数、`Position`、`Rotation`、`Scale` 的写入和登记过的标记层重画（§10）。
- **性能**：小地图主线程 ≤ 0.02 ms；大地图打开时界面合计 ≤ 0.2 ms；`HudRoot` 下的 Control 在大地图打开、设置页也打开时仍 ≤ 400；最低配置上大地图 GPU ≤ 0.3 ms（§10）。
- **主题**：越过主题回差时，大地图和其余大页在同一次扫描前沿里切换，没有一帧是一半新一半旧；小地图默认固定深屏，交班前后逐像素不变（底图本身随季节的微小变化除外）；设成"常驻件：随主题"时，小地图与大页在同一次前沿里切换。
- **读屏**：小地图、目录项、卡片按钮都有可读的名字；到达时 live region 写入"已到达：……"。

### 14.3 其他门禁的增量

- **G2**：
  - 解锁策略的单元测试：`all-unlocked` 全部 `Unlocked`；`proximity` 的进出半径防抖；
  - 自定义标记的上限与存档往返；ID 是整数、递增、不复用，`NextPinId` 在往返后不变；
  - `MarkerId` 在一张图内不重复；
  - 开放清单增加一项后，标记自动出现。
- **G13**：
  - 传送录像：旁观、行走各一条；室外到室内门口一条；室内观景点一条；跨 600 m 以上一条；"减少动态效果"开启时一条。
  - 判据：雾幕遮罩区间以外，相邻帧 ΔE 照常判；到达帧目的地各环就绪；镜头不跳变。
  - 另录：雾幕升起前按 Esc 取消，以及大地图的开合。
- **G15**：行走模式下传送到全部锚点、全部室内门口、全部路线起点的 `Stand`，都能正确落地（SYS-WALK §15 的"跳到全部点位的 `Stand`"扩展到全部可传送标记）。
- **G16**：
  - 第一次按 M、第一次打开传送卡、第一次主题切换之后，管线和 Canvas 的编译计数不增长；
  - 新增 F4 跑完，计数为 0。
- **F4 传送压力**（ENG-GATES §1）：
  - 在全部可传送标记之间随机传送 300 次，旁观、行走各半；
  - 每次到达帧目的地就绪；任何一帧卡顿不超过 100 ms；单次总时长不超过 2.4 s（行走 2.6 s）。

## 16 待验证（P0）

| 待验证 | 验证前用的初值 | 验证方法 · 负责 |
| --- | --- | --- |
| 底图本机烘焙的全图耗时（§2.1） | 8 线程 ≤ 20 s | P0 在开发机上实测；WP-48a |
| 截面线是否全在地图页内（§2.3） | 截面线包围盒约 x −1250…1260、z −1400…1440（WLD-OUTSKIRTS的估计）；`atlasRect` 取 ±1536 m | 截面线定稿后，G18 报告被页边裁掉的长度；超出时 `atlasRect` 按 64 m 对齐外扩（§2.3）；WP-48a |
| 锚点 A01–A13 的坐标、朝向与落点（§6） | §6 锚点表；`Stand`、`Fly` 按 §6 的落点规则 | 按截图和 SYS-WALK §15 的落地检查定稿；WP-48c、WP-44 |
| 锚点 A04 的位置与朝向（§6） | 品学楼门框前广场，叠水喷泉西侧约 20 m，约 (120, −65)，朝门框约 90° | WP-48c 与 WP-44 在门框前广场重新取点，结果同步 MAP-QSH-CENTER-EAST；WP-48c、WP-44 |
| 小地图的主线程开销（§10） | 平均 ≤ 0.02 ms/帧；"随镜头"模式 ≤ 0.05 ms | P0 实测；WP-48b |
