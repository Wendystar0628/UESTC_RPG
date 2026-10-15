---
id: SYS-VIEW
title: 观察模式、介质与开场
category: systems
summary: 观察模式总述与旁观漫游、镜头在楼里地下水下雾海的介质呈现、启动开场与署名、旧网页栈归档
owners: [WP-06, WP-07, WP-01]
contributors: [WP-00, WP-04, WP-08, WP-24, WP-35, WP-36, WP-42, WP-49, WP-51, WP-53]
depends: [ENG-ARCH, ENG-PERF, UI-NAV, SYS-WALK, SYS-TITLE, WLD-OUTSKIRTS, WLD-INTERIORS]
related: [WLD-DIORAMA, SYS-MOTION, ENG-GATES, ART-FIDELITY, ENG-BUDGET, SYS-TIME, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-SAVE, SYS-MAPS, SYS-AUDIO]
contracts: [ICameraRig, Pose, CameraMedium, MediumKind, IMediumPresenter, IMediumState]
data: ["LICENSES.txt"]
gates: [G1, G2, G7, G13, G16, G21]
---

# SYS-VIEW · 观察模式、介质与开场

> **范围**：本文规定两种观察模式的总述和旁观者漫游（操作、活动范围、初始位置、命令行参数、提示），`--title=0` 的启动流程与各项预热的去处，环境痕迹与平面地图的分工，镜头在楼里、地下、水下、雾海里时的介质呈现，署名、窗口标题与图标，以及旧网页栈的归档。
> **不在本文**：行走模式见 SYS-WALK；界面、点位跳转与操作说明见 UI-NAV、UI-VISUAL；平面地图与传送见 SYS-ATLAS；正常启动的扉页、开始页与进场过场见 SYS-TITLE；存档见 SYS-SAVE；多地图见 SYS-MAPS；活动范围的细则与数值见 WLD-OUTSKIRTS；室内的开放清单、加载与门见 WLD-INTERIORS；声音见 SYS-AUDIO。
> **关联**：[ENG-ARCH 工程架构](../engineering/ENG-ARCH.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF.md)、[UI-NAV 界面与跳转](../ui/UI-NAV.md)、[SYS-WALK 行走](SYS-WALK.md)、[SYS-TITLE 开始页与过场](SYS-TITLE.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS.md)；[WLD-DIORAMA 箱庭与远景](../world/WLD-DIORAMA.md)、[SYS-MOTION 动态与事件](SYS-MOTION.md)、[ENG-GATES 验收门禁](../engineering/ENG-GATES.md)、[ART-FIDELITY 视觉精度](../art/ART-FIDELITY.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET.md)、[SYS-TIME 时间季节](SYS-TIME.md)、[SYS-WEATHER 天气](SYS-WEATHER.md)、[SYS-GRAPHICS 画面设置](SYS-GRAPHICS.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL.md)、[SYS-ATLAS 平面地图与传送](SYS-ATLAS.md)、[SYS-SAVE 存档](SYS-SAVE.md)、[SYS-MAPS 多地图](SYS-MAPS.md)、[SYS-AUDIO 音频](SYS-AUDIO.md)

### 分工

- WP-00：旧网页栈的归档与拆除（§1）；
- WP-01：开场、署名、窗口标题与图标（§3、§6）；
- WP-06：相机与输入（§2）；
- WP-07：介质呈现（§5）。

行走模式见 SYS-WALK（WP-42–44），界面与点位跳转见 UI-NAV（WP-35、WP-36），界面视觉语言见 UI-VISUAL，平面地图与传送见 SYS-ATLAS（WP-48），开始页与进场过场见 SYS-TITLE（WP-49），存档见 SYS-SAVE（WP-50），多地图见 SYS-MAPS（WP-46），边界与外围见 WLD-OUTSKIRTS（WP-47），室内见 WLD-INTERIORS（WP-51、WP-52），声音见 SYS-AUDIO（WP-53）。

## 1 旧网页栈的归档（WP-00）

新工程不用旧网页栈，按下表归档。

| 内容 | 处理 |
| --- | --- |
| 整套 Vite + React + three.js 代码：`src/`、`public/`、`index.html`、`package.json` 与锁文件、`vite.config.ts`、`playwright.config.ts`、网页端测试、`dist/`、`node_modules/` | 在 git 基线（标签 `baseline-map`）里保留；主分支上整体删除，不留 legacy 目录 |
| 可移植的算法：轮廓工具、边与斜接、扫描线、开间分层、地标屋顶的主轴求法、八角采光顶、落点算法、球场标线拟合等 | 清单见 ENG-ARCH §1。各包用 `git show baseline-map:<原路径>` 读出原文，改写成 C#，放进自己的目录；旧栈的几何与数据测试一并改写成 `dotnet test` |
| Python 数据管线 `scripts/data/`、OSM 快照、`categories.json` 等管线输入 | 迁到 `pipeline/`（逐项见 ENG-ARCH §1）；输出写到 `game/data/` |
| `campus.json` | 迁到 `game/data/maps/qingshuihe/campus.json`，格式留用，只增不改 |
| 照片纹理：`public/assets/materials-v1/`、`data/raw/materials/` 和两个旧贴图脚本 | 不迁移，只留在归档里（ART-STYLE §6.1） |
| `lucide-react` 等前端依赖 | 随归档删除；新界面的图标自绘（UI-NAV §2、UI-VISUAL §6） |

**旧栈的网页地图功能（搜索、地点卡片、路径规划、拾取、在 3D 里点选），新工程一律不做**（INV-1）；界面里的平面地图按 SYS-ATLAS 重新设计，不按旧代码补回。下表留作清单，防止有人照着旧代码补回来（"手动选择画质的界面"一行要做，它不是地图功能）：

| 旧功能 | 新工程 |
| --- | --- |
| 顶部标题栏、"操作指南"、页面布局 | 不做；界面只有 UI-NAV、UI-VISUAL 登记的组件 |
| 搜索侧栏、分类筛选、地点列表、搜索与筛选逻辑 | 不做文字搜索；分组列表并入大地图的目录页（SYS-ATLAS §4.4），图层筛选是大地图的左栏（SYS-ATLAS §4.3） |
| 标题区、建筑标签开关、3D / 俯视切换、指北针、缩放按钮、回到总览、图层面板、比例尺、手势提示、"数据与精度"、数据日期、信息卡 | 3D 画面里一律不做（建筑标签、俯视切换、指北针、信息卡）；大地图里有缩放、回到玩家、全图、图层、比例尺（SYS-ATLAS §4），小地图北朝上（SYS-ATLAS §3）；署名按 §6 |
| 选中、筛选、图层、严格高度等业务状态；点选与选中描边；标注、聚合、避让 | 3D 里不做点选和选中描边；地图上的标注与避让烘焙（本机烘焙，D-165；SYS-ATLAS §2.5），选中只在界面层表现 |
| MapControls、回正北、俯视、全校总览、楼体避让 | 不做；相机由 WP-06 新写（§2） |
| 严格高度模式（只显示有来源楼高的建筑） | 不做 |
| 手动选择画质的界面 | 做：画面设置（SYS-GRAPHICS）。四档预设（流畅、均衡、精致、极致）加自定义单项，风格底线不可关，自动调节画质默认开；界面是设置卡的"画面"页（UI-NAV §4.6、SYS-GRAPHICS §9） |
| 贴图加载失败的提示和重试按钮 | 不做；资源随安装包导入，流式加载失败时静默重试并写日志 |

## 2 旁观者漫游（WP-06）

观察模式有两种：**旁观**（默认，本节）和**行走**（SYS-WALK），用 F 键、界面按钮或双击空格切换（SYS-WALK §5）；不做环绕、俯视、导览和自动运镜。旁观的观看方式类似《我的世界》的旁观者模式：WASD 和空格自由移动，可以穿透任何物体。行走模式的手感和键位见 SYS-WALK §2–§4；鼠标捕获、漫游态与界面态见 UI-NAV §5。

### 2.1 操作

| 输入 | 作用 |
| --- | --- |
| 单击画面 | 进入漫游态，鼠标被捕获（`Input.MouseMode = Captured`）。只有 GUI 没处理的左键按下才算（UI-NAV §5） |
| Esc | 漫游态 → 界面态：释放鼠标，界面淡入。世界不暂停（UI-NAV §5） |
| 鼠标移动 | 漫游态下转动视角：偏航不限，俯仰限制在 ±89°。界面态下不转视角 |
| W / S | 沿视线的水平分量前进 / 后退 |
| A / D | 向左 / 向右平移 |
| 空格 | 上升 |
| Shift | 下降 |
| 按住 Ctrl | 冲刺，速度 ×2，松开即停止。默认开启（UI-NAV §5） |
| 双击 W（0.35 秒内） | 同样是冲刺，前进键松开即停止 |
| F，或双击空格 | 切到行走模式（SYS-WALK §5） |
| 滚轮 | 调整速度上限，共 16 档，按对数分布在 0.5–200 m/s 之间；默认第 9 档，约 12 m/s。界面上的档位条与之对应（UI-NAV §4.3） |

- 移动方式与《我的世界》的飞行一致：W 永远水平前进，升降只靠空格和 Shift。界面态下移动键仍然有效。
- **实际速度** = min(vₙ, 6 + 0.5 × 离地高度) × 冲刺倍数（ENG-BUDGET §4）。第 n 档 vₙ = 0.5 × 400^((n−1)/15) m/s，相邻两档之比约 1.49，第 9 档 12.2 m/s。离地面越近速度越慢，方便贴近看细节。
  - 离地高度取常驻的低精度地形高度，不用物理射线：旁观模式不加载碰撞（SYS-WALK §13）。
- **加速和减速**：v ← v目标 + (v − v目标)·e^(−k·dt)，与帧率无关。加速 k = 20/s，约 0.15 秒达到目标速度的 95%；减速 k = 15/s，约 0.2 秒停下。镜头不晃动，没有头部摆动效果。
- **视场角**：垂直 60°。Godot 的 `Camera3D.Fov` 在 `KeepAspect = Height`（默认）下就是垂直视场角。设置里可以在 50–80° 之间调（UI-NAV §4.6）；切到行走模式时过渡到 70°。
- **碰撞**：旁观模式下完全没有。镜头可以穿过墙、地面、树和水面，穿过时的画面由 §5 处理。行走模式有碰撞，见 SYS-WALK §12–§13。

**实现**（WP-06）

- **节点**：一个 `Camera3D`，`TopLevel = true`，`PhysicsInterpolationMode = Off`。飞行在 `_Process` 里积分，不用物理体。相机在每帧顺序的相机一步（`ProcessPriority = −1100`，SYS-TIME §1），前面只有地图（−1300，SYS-MAPS §9）和画质（−1200）两步；进场过场的 `CinematicPlayer` 也在这一步驱动镜头（SYS-TITLE）。
- **鼠标**：
  - 启动时设 `Input.UseAccumulatedInput = false`，鼠标事件逐个到达，更精确。
  - 在 `_UnhandledInput` 里读 `InputEventMouseMotion.ScreenRelative`。它不受界面缩放影响，官方推荐用于捕获模式下的视角控制。Windows 下，捕获模式走 WM_INPUT 的原始计数，不经过系统的指针加速。
  - 旋转逐事件立即写入相机，不放进物理步，不做平滑。
  - 灵敏度：每个计数转 0.15° × 8f³，其中 f = 0.6s + 0.2，s = 界面百分比 ÷ 200。100% 时正好 0.15°，与《我的世界》是同一条曲线（SYS-WALK §3）。
- **按键**：动作全部定义在 `project.godot` 里，一律按 `physical_keycode` 绑定，换键盘布局不改变键位（UI-NAV §5）。
- **滚轮**：按 `InputEventMouseButton.Factor` 累计，满 1 进一档，兼容高精度滚轮和触控板（待验证，§7）。
- **朝向换算**：偏航是罗盘方位（CORE-INDEX 的坐标约定），Godot 相机默认朝 −z，也就是正北。所以 `Rotation.Y = −Mathf.DegToRad(偏航)`，`Rotation.X = Mathf.DegToRad(俯仰)`，旋转顺序用 Node3D 默认的 YXZ。

### 2.2 触屏操作

只做 PC，不做触屏操作。


### 2.3 活动范围

箱庭是"大箱庭"：扩到校外一圈街区，能看不能进；旁观飞到边缘时减速、雾变浓、再被推回。本节是 WLD-OUTSKIRTS §8 的摘要，细节和数值以 WLD-OUTSKIRTS 为准。参数读当前图 `map.json` 的 `roam`（SYS-MAPS §3）；没有外围数据的图用"校界外扩 150 m + 0.9 × 当前高度"（`fallbackMargin`）。

活动范围是软约束：接近边界时逐渐减速，然后被轻轻推回，不会突然撞停，也不锁输入。

- **三条线**（WLD-OUTSKIRTS §3.1）：校界；前沿线，即对街的路缘；截面线，即箱庭的外缘，截面以外是雾海和远景。
- **水平方向**：离前沿线的距离不超过 0.9 × max(0, h − 30 m)，并且离本图中心（`MapDescriptor.Center`）不超过 3 km（WLD-OUTSKIRTS §8.1）。所以 30 m 以下只能飞到前沿线；飞得越高，能离开得越远。
- **竖直方向**：
  - 校界以内：−28 m 到 2000 m。−28 m 接近地层截面的底部。
  - 校界到前沿线（O0 街道带）：不低于地面以上 0.3 m，外围不开放地下；上限 2000 m。
  - 前沿线以外：不低于软地板，即外围屋顶的净空高度场 + 40 m；上限 2000 m。
  - 截面线以外，水平公式本身就要求离地约 300 m 以上，所以正常飞行到不了雾海顶面（§5.4）。
- **缓冲带与推回**（WLD-OUTSKIRTS §8.2）：边界内侧 30 m 是缓冲带，速度的朝外分量乘 (1 − s)²（s 是进入缓冲带的比例），切向不变；越出的部分沿法线按临界阻尼推回，速率 1.5/s，最大 15 m/s。在校内地下朝外穿过校界时，校界是一道竖墙，镜头被水平推回校内。
- **边缘雾**（WLD-OUTSKIRTS §8.3）：WP-06 在相机那一步写全局参数 `edge_fog`（0–1），雾只在朝外的一侧变浓，最多 1.6 倍，回头看校园不受影响。行走、介质在室内、地下或水下、过场和截图门禁时为 0。
- **提示**：在缓冲带里朝外推 0.5 s，或发生了一次推回，出一次"前方暂未开放"（WLD-OUTSKIRTS §9、UI-NAV §4.9）。
- **合法化**：读档、换图出生、`--pose` 出生时，位姿不在可达空间里就就近投影进去，不播推回动画；`--pose` 给出的审查位姿不合法或在缓冲带里，以非零退出码结束（WLD-OUTSKIRTS §8.4）。
- **不受约束的镜头**：活动范围、推回和 `edge_fog` 只在 Playing 状态作用。开始页的 T0（离中心约 3.48 km）和进场过场的镜头由 `CinematicPlayer` 驱动，在活动范围之外，观者飞不到；过场的终点必须在可达空间内，而且不在缓冲带里（SYS-TITLE §2.1、SYS-TITLE §7.2、WLD-OUTSKIRTS §8.5）。点位跳转、传送和模式切换的降落途中不施加推回，由 WP-36 保证路径和目的地都在可达空间内（WLD-OUTSKIRTS §8.5）。
- **远景**：天空、远山、月亮、城市剪影都画在无穷远的天空层上，镜头永远飞不到（ENG-BUDGET §5）。
- **行走模式**：活动范围由可行走范围和边界收口决定（SYS-WALK §6–§7）。校界一圈的栏杆、关着的校门和闸机让人在 0.6 m 内缓缓停住，顶住 0.8 s 提示"校外暂未开放"（WLD-OUTSKIRTS §7、WLD-OUTSKIRTS §9）。

### 2.4 初始位置

初始位置分新游戏、继续和 `--title=0` 三种情况（开始页与进场过场见 SYS-TITLE，存档见 SYS-SAVE，多地图见 SYS-MAPS）；只有新游戏的开场一定是默认状态。

- **默认出生点**：当前图的 `defaultSpawn`（SYS-MAPS §3）。清水河上就是点位 P00（UI-NAV §6），位姿约 (−641, 928, 1283)，偏航 20°，俯仰 −35°。这相当于从图书馆出发，方位 200°、仰角 35°、距离 1600 m 处看回图书馆。
- **画面**：南门、主楼、银杏大道在前景，图书馆在画面中央；画面下沿是西源大道和成都合院一带的外围街区（WLD-OUTSKIRTS §13.1）。默认状态下，落日在左侧画外，月亮在右侧。P00 在前沿线外约 490 m，在可达空间内（§2.3）。
- **新游戏**：一律从落金初灯开始（SYS-TITLE §3、SYS-SAVE §5.5）。进场过场的终点是 P00；交还控制时是旁观模式和本图的默认状态 `world.default`（SYS-MAPS §5）：秋季 17:30、晴、自动天气、流逝 1×（SYS-TIME §1、SYS-WEATHER §6.6）。自动天气保持晴到秋季黄昏带结束（约 19:36），所以任何天气种子下，新游戏交还控制时的画面都相同。之后世界时间从 17:30 单调向前，黄昏自然流逝进蓝调和夜晚。
- **继续**：回到存档时的位姿、观察模式和世界状态（SYS-SAVE §5.1）；继续过场约 9 s，穿云落到存档位姿（SYS-TITLE §4.1）。位姿不在可达空间里时就近合法化（§2.3）。
- **`--title=0`**（门禁、截图、基准）：没有扉页、开始页和过场，位姿取 `--pose`（截图默认 V1，即 P00），世界状态取命令行（§2.5）；给了 `--slot` 或 `--load` 时，位姿和世界状态都取那份存档（SYS-SAVE §12）。

### 2.5 镜头不会自己动

镜头只由观者控制，不做任何自动运镜（SYS-MOTION §1.4）。以下过渡都由观者主动触发，不算自动运镜：

- 点位跳转（UI-NAV §7），途中可以随时打断；
- 平面地图上的传送（SYS-ATLAS §7）：默认雾幕瞬移，或选"飞过去"；
- 模式切换时的降落和起飞（SYS-WALK §5）；
- 行走模式下的回位（SYS-WALK §9）；
- 主楼电梯的"关门换位"（WLD-INTERIORS §3）：镜头随角色平移，不另运镜。

**开始页与进场过场**是 INV-2 的例外（SYS-TITLE）：由观者在开始页主动选"新游戏"或"继续"触发，可以按住 Esc 0.8 s 跳过，路径来自数据文件，交还控制后镜头不再自己动；返回标题同理。开始页的 T0 是静止机位，不做呼吸式推拉。

**命令行参数**：截图和自动化脚本用写在 `--` 之后的用户参数，设定漫游相机的出生位置、世界状态和画面设置，例如 `-- --pose=V3`、`--pose=P05`，以及 `--clock=`、`--season=`、`--flow=`、`--mode=`、`--ui=0`、`--weather=`、`--weather-seed=`、`--quality=`、`--adaptive=`；另有：

- 地图：`--map=<id>`（默认 `qingshuihe`）、`--spawn=<id>`（SYS-MAPS §11）；
- 开始页与过场：`--title=0|1`（门禁与截图默认 0）、`--cine=<id>`（SYS-TITLE）；
- 存档：`--save=off|<n>`、`--slot=<n>`、`--load=<路径>`、`--save-dir=<路径>`、`--save-at=<帧号>`（SYS-SAVE §12）；
- 声音：`--audio=on|off`、`--audio-seed=`、`--audio-limiter=`、`--audio-log=`、`--audio-probe`、`--amb-*`（SYS-AUDIO §19）；
- 门禁：`--gate=maptravel`（SYS-MAPS §13）、`--gate=acoustics`（SYS-AUDIO §22）。

完整列表和截图默认值见 ENG-GATES §1，截图默认串是 `-- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off`。设定之后，镜头仍然是普通的漫游相机，这不算另一种观看模式。

- 参数由 Autoload `Boot` 用 `OS.GetCmdlineUserArgs()` 读取。未知参数直接以非零退出码结束，免得拼错的参数被静默忽略；不合季节的天气（例如冬季下雨）也一样（ENG-GATES §1）。`--slot` 或 `--load` 与 `--clock`、`--season`、`--pose`、`--map` 这类世界类参数同时给出时，同样以非零退出码结束，因为读档时世界状态只来自存档（SYS-SAVE §12）。
- 导出的正式版也接受这些参数，因为 G10 测的就是导出的 release 版（ENG-GATES §2）。观者直接启动时不带参数，相当于 `--title=1`：先是扉页和开始页（SYS-TITLE §1、SYS-TITLE §2），起始图是最近存档的图（SYS-MAPS §4.4）；天气种子跟着存档走，只在新游戏时取一次熵（SYS-WEATHER §6.4、SYS-SAVE §5.4）；画面设置读 `user://graphics.cfg`，没有时用自动推荐的预设（SYS-GRAPHICS §5–§6）。
- 门禁和基准只由命令行决定，不读 `user://`；命令行优先于 `graphics.cfg`（SYS-GRAPHICS §6）。`--save` 和 `--audio` 在 `--gate`、`--shots`、`--bench`、`--frames` 下默认 `off`；起始图不因"最近存档的图"而改变，不给 `--map` 就取默认图（SYS-SAVE §12、SYS-MAPS §11、SYS-AUDIO §17）。
- 在编辑器里运行时，参数写在本工作树 `override.cfg` 的 `editor/run/main_run_args` 里（`override.cfg` 不进 git）。

### 2.6 操作说明与提示

画面上允许以下操作说明与提示：

- 首次新游戏的过场交还控制之后，屏幕下方一行"单击画面开始 · Esc 打开菜单 · H 操作说明"，4 秒后淡出，以后不再出现（UI-NAV §4.7）；样式按 UI-VISUAL §7.4 的临时提示；
- 按 H 展开的操作说明卡片（UI-NAV §4.6）；
- 漫游态下 2 秒内淡出的临时提示，只限 UI-NAV §4.9 列出的几种，包括"校外暂未开放""前方暂未开放"（WLD-OUTSKIRTS §9）和"此处暂不开放"（WLD-INTERIORS §4）；
- 左上角的小地图（SYS-ATLAS §3）：漫游态唯一常驻的元素，按 N 切换显隐，按 U、`--ui=0` 和过场时隐藏。

### 2.7 行走模式

见 SYS-WALK。本文件的 §3–§6 对两种模式都适用。

## 3 启动与开场（WP-01；流程以 30.1 为准）

正常启动的开场是"扉页 → 开始页 → 进场过场"，流程以 SYS-TITLE §1 的启动序列为准（窗口雾色 → 约 1 s 内换成扉页 → 扉页后面做完地图加载、三维与二维预热、首次分档、读存档 → 1.2 s 交叉淡化成实时的开始页，全程没有进度条）。本节只规定四件事：引擎启动画面的设置、启动各项预热的去处、`--title=0` 的流程，以及失败处理。

1. **引擎启动画面**：从窗口出现的第一帧起就是全屏雾色 #B8B6D6，不显示任何文字、进度条、图标，也不显示引擎的启动标志。
   - `application/boot_splash/show_image = false`，`application/boot_splash/bg_color = #B8B6D6`；`rendering/environment/defaults/default_clear_color` 也设成同色（`project.godot`，WP-00 维护）。
   - 主场景的第一帧起，由雾幕接管：雾幕层（layer 5）里的全屏 `ColorRect`，不透明（UI-NAV §9、SYS-TITLE §1.1）。启动画面和雾幕同色，交接时看不出来。正常启动时雾幕层在 0.3 s 内换上扉页静帧（SYS-TITLE §1.3）。
2. **启动各项预热的去处**：都在扉页（`--title=0` 时是雾幕）后面做：

   | 项 | 做法 |
   | --- | --- |
   | 加载常驻的 L4，加载并挂载初始位置附近的格子 | 开场就是一次 `IMapService.Travel(Boot)`：按 SYS-MAPS §4.2 的 LoadData、Mount、Warm，直到"加载、挂载、管线就绪"（ENG-PERF §5 第 2 条）；起始图 = `--map` > 最近存档的图 > 默认图（SYS-MAPS §4.4）。SYS-TITLE §1.1 第 2 步 |
   | 预热管线 | 三维：白名单里的每个着色器变体各画一次，包括 §5.1 的剖切变体、降水用的 `precip.gdshader`（SYS-WEATHER §7）、云卡 `cine_cloud` 和外围环境（WLD-OUTSKIRTS），覆盖四个预设和所有单项取值能到达的组合的并集（SYS-GRAPHICS §3），直到 `PipelineCompilationsSurface`、`PipelineCompilationsDraw` 不再增长（SYS-TITLE §1.1 第 3 步）。二维：`ui_panel`、`ui_screen` 两个着色器 × 各绘制类型各画一次，大页（设置卡与画面页、存档页、开始页菜单、大地图的卡片）预建并隐藏，界面代码路径空跑一遍，之后 `PipelineCompilationsCanvas` 不再增长（UI-VISUAL §11.9，SYS-TITLE §1.1 第 4 步） |
   | 首次分档与推荐预设 | 按 SYS-GRAPHICS §5 在 2.5 s 内完成，结果写进 `user://graphics.cfg`；观者手动选过预设的，下次启动不覆盖。SYS-TITLE §1.1 第 5 步 |
   | 天气种子 | 有可继续的存档时，种子和已生成的队列来自存档；新游戏时由 `ISaveService.NewGame` 取一次熵（`--weather-seed` 覆盖），并保持晴到秋季黄昏带结束（SYS-WEATHER §6.4、SYS-WEATHER §6.6，SYS-SAVE §5.4） |
   | 读取 `settings.cfg`、`input.cfg`、`graphics.cfg` | 在 Boot 里读（UI-NAV §4.6、SYS-GRAPHICS §6），另加 `user://audio.cfg`（SYS-AUDIO）。读值时只接受基本类型，读之前先查文本里有没有 `Object(` 这类对象构造（SYS-SAVE §8） |
   | 存档 | 拿 `user://saves/.lock`（`--save=off` 时不拿锁）；读三个存档位的 meta，挑出"继续"的那个（SYS-TITLE §1.1 第 1 步），再完整恢复成开始页 T0 要显示的世界（SYS-SAVE §5.1，SYS-TITLE §1.1 第 6 步）；空跑一次"序列化 + 压缩 + 哈希"，消掉第一次约 29 ms 的 JIT（SYS-SAVE §4.5，SYS-TITLE §1.1 第 7 步） |
   | 声音 | 扉页或雾幕期间 Master 组在 −∞；"音频就绪"是扉页转为实时、雾幕淡出的条件之一（SYS-AUDIO §2.7） |

3. **`--title=0`**（门禁、截图、基准的默认，ENG-GATES §1）：没有扉页和开始页。
   - 窗口雾色 → 雾幕后面做完上表各项，世界状态和位姿取命令行；给了 `--slot` 或 `--load` 时取那份存档（SYS-SAVE §12）→ 全部就绪后，雾色在 2–3 秒内淡出，直接进入 Playing（SYS-TITLE §1.1、SYS-TITLE §7.1）。
   - 透明度由 Tween 驱动；雾幕颜色每帧取当前地平线雾色，与跳转雾幕是同一种语言（UI-NAV §7.1）。
   - 雾幕淡出时环境声从 −∞ 用 3 s 淡入（SYS-AUDIO §7.2）。
4. **失败**：正常启动时停在扉页上，`--title=0` 时停在雾色上，后台静默重试，错误写进日志（SYS-TITLE §1.1）。
5. **之后**：
   - 正常启动时，扉页转为实时的那一刻进入开始页（Title）；开始页的视觉见 UI-VISUAL §8.6，菜单与 T0 见 SYS-TITLE §2。首次进入的一行引导在新游戏交还控制之后出现（§2.6）。
   - G16 的计数基线：正常启动时是"标题页转为实时"那一刻（SYS-TITLE §1.1 第 8 步）；`--title=0` 时仍是雾幕淡出那一刻。此后三项管线编译计数都不得再增长（ENG-GATES §2）。

## 4 环境痕迹与平面地图的分工

界面里有平面地图（SYS-ATLAS），3D 画面不叠加任何标注。地点的名字、选中和追踪只出现在界面层的小地图和大地图上；3D 画面里靠环境本身回答"这是哪里、往哪走"。

| 地图元素 | 3D 里用什么代替 | 由谁实现 |
| --- | --- | --- |
| 地点标签和图标 | 3D 里靠灯和店招认：有名字的楼宇和设施，入口亮一盏暖色门厅灯，窗口按比例亮起，商店亮店招；开放室内的门口另有暖光池、地垫和绿色开放牌（WLD-INTERIORS §4）。名字只出现在小地图底栏的区域名和大地图上（SYS-ATLAS §2.5） | WP-08 从数据里挑出名单（有名字，类别属于教学、科研、图书、行政、餐饮、服务、医疗、体育）；WP-03 管理灯光通道；各区放置灯具；地图标注归 WP-48a |
| 指北针 | 3D 里是日晷的晷针指北；界面上小地图默认北朝上（SYS-ATLAS §3），二者一致 | WP-16、WP-48b |
| 搜索和地点列表 | 大地图的目录页：按区分组的列表，带缩略图和"此处"标记，没有搜索框（SYS-ATLAS §4.4） | WP-48b、WP-36 |
| 选中高亮 | 3D 里不留任何痕迹；地图上的选中、追踪只画在界面层（SYS-ATLAS §1） | WP-48b |
| 场景里的锚点实体 | 导览亭与导览桩：真实校园里的导览设施，不发光、不做特效；导览亭的牌面就是同一张平面地图的场景配色版（MAP-QSH-ADDITIONS §2.13、SYS-ATLAS §6） | WP-13、WP-48c |
| 人（地图的使用者） | 推开的椅子、车筐里的落叶、晾着的衣服、冒热气的杯子 | WP-17、WP-25 |

## 5 介质呈现：镜头在楼里、地下、水下、雾海里时（WP-07）

镜头处在哪种介质里，由 WP-08 的空间索引 `Spatial.MediumAt()`（ENG-ARCH §3）判断，每帧一次，耗时低于 0.05 ms。行走模式下，镜头只会处在空气或 15 处开放室内（WLD-INTERIORS §2）；只有涉水区外沿的"沉水"过程会短暂进入水下（SYS-WALK §9）。地下和雾海只在旁观模式出现。切换介质时有 0.25 秒的过渡，用来遮住近裁剪面切开几何的那一瞬间。

**实现**（WP-07）

- **MediumService**：Kernel 的一个服务，每帧在相机之后运行（`ProcessPriority = −600`，SYS-TIME §1）。它判断介质，写两个全局着色器参数：
  - `medium`：vec4，依次是起点介质、目标介质、混合度（0–1）、能见度（m）；
  - `medium_color`：当前介质的雾色。
  - 两个参数都要在项目设置的 Shader Globals 里登记（登记表见 ENG-PERF §1.5，`project.godot` 由 WP-00 维护）。
- **0.25 秒过渡**：共用雾函数（每个材质在 fragment 里写 `FOG`）和天空着色器都读这两个参数做混合。不切换 Environment，因为切了没法渐变；不用 FogVolume 和体积雾，它们范围有限，有拖影和色带。
- **全屏四边形**：相机下挂一个全屏四边形（`unshaded`、`depth_test_disabled`），用 WP-07 的 `game/style/shaders/medium_overlay.gdshader` 画染色、焦散和地层线。
  - 它在透明通道里画，晚于描线（POST_SKY），早于移轴和光斑（POST_TRANSPARENT）（ENG-PERF）。
  - 这个着色器在着色器白名单里（ENG-PERF §1.4）。
  - 按 G4，四边形实例由内核创建、材质由材质库创建：WP-07 经内核的 `IRenderHost` 申请（ENG-ARCH §2），之后只写参数。
- **阴影**：介质切换时只改全局参数 `shadow_strength` 和主光的 `ShadowOpacity`，不碰会改特化常量的开关（shadow_enabled 等，ENG-PERF）。
- **天气**：天气雾、雨丝和雪只作用于室外。镜头进入室内、地下、水下时，`medium` 的混合度把天气雾换成该介质的值；降水被 `precip_occ` 挡在屋檐、连廊和室内之外（SYS-WEATHER §7）。商业街步行街、立人楼外廊、平台下街廊这类半室外空间按室外介质处理，降水由 `precip_occ` 挡在顶下，走进房间或店里才切到室内介质（WLD-INTERIORS §2.0）。
- **声音**（SYS-AUDIO §10.9）：WP-07 另提供只读服务 `IMediumState`（当前介质、起点、目标、混合度，与全局参数 `medium` 同源），音频据此滤波，不读回全局参数。声音的介质过渡用 0.6–1.0 s，比画面的 0.25 s 慢；旁观穿墙时听者空间槽与画面同步 0.25 s，飞行风做一次低通扫频、压低 6 dB，不做撞击声。各介质的声音写在下面各小节末尾。

### 5.1 楼里

- **剖切面**：
  - 外壳平时只画正面（`cull_back`）。相机进入外壳 2 m 以内时，只给这个实例换成预编译好的剖切变体（`cull_disabled`）：背面（`!FRONT_FACING`）输出剖切色，法线设为朝向相机，ROUGHNESS 写剖切 ID（ENG-PERF）。离开 2 m 后换回。
  - 换法：WP-07 经 `IRenderHost` 提出请求，内核给该实例设材质覆盖（`RenderingServer.InstanceGeometrySetMaterialOverride`），变体材质由材质库提供。WP-07 不自己创建材质（G4）。
  - 剖切变体在扉页或雾幕后面预热（§3、SYS-TITLE §1.1 第 3 步）。第一次穿墙时不能触发管线编译（G16，ENG-GATES §2）。
  - 剖切色由 WP-04 登记进调色板，用偏暖的灰紫色。
  - 外壳不做整体双面绘制：那样图元和阴影的工作量都会翻倍。
  - 阴影用引擎原生（ENG-PERF）；不做剖切封口程序，阴影通道里也不让闭合网格只渲背面。
  - 剖切面的边缘描一圈细线：剖切 ID 和墙面的材质 ID 不同，描线后处理按 ID 的变化自动出线（ART-STYLE、ENG-PERF）。所以穿墙的那一刻，画面就像一张剖面图。
  - 镜头在开放楼内时，当前层与 ±1 层的室内壳一律用剖切变体，旁观切进隔墙不会透视看到隔壁（WLD-INTERIORS §6.3）。
  - 画面里不能透出天空，也不能出现黑洞。
- **完整室内**：开放清单里的 15 处（WLD-INTERIORS §2），每处一个按层提供的 `IInteriorProvider`（WLD-INTERIORS §15）。加载按 WLD-INTERIORS §6：离开放入口 25 m 整体预取这栋楼的开放部分，按层显示（当前层全精度，上下层只留墙、楼板、楼梯和灯）；行走时门槛前 1 m 软停兜底；镜头在楼体里至少显示 I0（ENG-PERF §5 第 6 条，WLD-INTERIORS §6.4）。
- **简化室内**（关着的楼）：由 WP-51 的平面内核按楼型骨架生成，内容是楼板、隔墙（带门洞）、室内雾和灯（灯片），加楼梯与电梯井的体块；家具每间最多 2–3 件平面剪影，可省；不做碰撞，不做生活痕迹（WLD-INTERIORS §5.3）。
  - 触发条件：镜头沿当前速度 1 s 内会进入这栋楼的外包棱柱时，加载镜头所在层 ±1；速度超过 15 m/s 不加载，只显示 I0 加雾。
  - 生成用固定的随机种子，同一栋楼每次进去看到的都一样。生成在线程池里跑，挂载在主线程（ENG-ARCH §2 的 build 与 mount 分开）。
  - 内容和底线按 ART-FIDELITY §5 的第 2 条。
  - 灯片亮灭按本楼的灯光通道和哈希，不要求与外面看到的窗灯逐间对应。
  - 行走模式下关着的楼永远进不去，也永远不加载简化室内。
- **I0 兜底层**：每栋楼的楼板、外墙内皮、交通核体块，默认隐藏；镜头进入任何楼体时立即显示，所以永远看不到空壳（ENG-PERF §5 第 6 条，WLD-INTERIORS §5.3、WLD-INTERIORS §6.4）。
- **细节级别**：镜头在开放楼里时，开启随室内包加载的室内遮挡体、隐藏本楼的外包遮挡体，楼外经窗洞看不到的部分由遮挡剔除去掉；主光阴影第一级缩到 8–12 m，0.5 s 内过渡，出楼恢复（WLD-INTERIORS §6.3）。
- **声音**（SYS-AUDIO §10.8、SYS-AUDIO §10.9）：室外声经透射变闷变小（低通 600–2000 Hz，−6 到 −18 dB），室内档案的房间底噪淡入；飞行气流 −12 dB；简化室内用通用房间声明（Room、Corridor）。

### 5.2 地下

- 画面按镜头所在地层的颜色上雾（WLD-DIORAMA §2），能见度 8–15 m。
- 看得到桩基、管沟、涵洞、树根团和地下水线。
- **地上的图层全部剔除**：
  - 相机的 `CullMask` 去掉地上各层，只留地面、地下内容和截面。渲染层的分配表由内核登记（WP-01）。
  - 被剔除的层，影子随之消失；同时把 `shadow_strength` 和主光的 `ShadowOpacity` 设为 0。
  - 西湖的倒影视口设为 `SubViewport.UpdateMode.Disabled`。
- 从地下往上看，地表背面显示表土的切面色：相机所在格和邻格的地面换成剖切变体，背面输出表土色（做法同 §5.1；待验证，§7）。
- **声音**（SYS-AUDIO §10.9）：World 低通约 200 Hz、−20 dB，土层的低沉闷响，没有生命声。

### 5.3 水下

- 画面染成青绿色，雾的能见度 5–10 m。染色由全屏四边形画，能见度由 `medium` 参数交给共用雾函数。
- 看得到沉水植物、锦鲤剪影和湖底卵石；在西湖，还能看到天鹅的脚蹼在水面下划动。
- 水下的着色参数由 WP-24 提供。
- **声音**（SYS-AUDIO §10.9）：World 低通从 20 kHz 对数渐变到 300–500 Hz，随 1.5 s 的下沉逐步进入；岸上声 −10 dB；水下床与偶尔的气泡；不发脚步。

### 5.4 雾海

截面线以外的最低合法高度约 300 m（§2.3），正常飞行到不了雾海顶面以下。雾海分支只作兜底：推回滞后时，或过场镜头穿过时（WLD-OUTSKIRTS §8.1）。

- 镜头沉到雾海顶面以下时，画面是浓雾（`medium` 的雾海分支）。顶面高度读当前值不变：雾天雾海会抬升，最高到地面以下约 1 m（WLD-DIORAMA §4.2）。
- 活动范围照常把镜头推回（§2.3）。
- **声音**（SYS-AUDIO §10.9）：World 低通 2–3 kHz、−6 到 −10 dB，只有一层很轻的白噪和风，远景声消失。

## 6 署名、窗口标题与图标（WP-01）

- **署名**：
  - 内容只有一行文字："© OpenStreetMap contributors"。
  - 放在右下角，字号 10–11 px（按 1920×1080 基准，随界面缩放，UI-NAV §9），颜色取当前雾色的反色，不透明度约 35%。颜色由 WP-01 每 0.1 s 按调色板的 CPU 求值结果（当前地平线雾色）算一次，写进 `Modulate`，不逐帧写。
  - 界面的时间卡（含时间环，UI-VISUAL §7.5）展开时，署名左移让位（UI-NAV §3）。存储指示（UI-VISUAL §7.10）在署名上方。
  - 放映层（过场、开始页）出现黑边时，署名改用固定的浅色，保证在黑边上读得出（SYS-TITLE §8.5）。
  - 实现：一个 `LinkButton`（`UnderlineMode = Never`，不做悬停样式），放在 WP-01 的署名层（CanvasLayer，layer 11，在界面层之上，UI-NAV §9）。`--ui=0` 时画面上只剩它。
  - 界面态下点击署名，用系统浏览器打开 `https://www.openstreetmap.org/copyright`（`OS.ShellOpen`）。漫游态下鼠标被捕获，点不到。
  - 理由：数据采用 ODbL 协议，必须署名。外围街区的数据同样来自 OSM，署名不变（WLD-OUTSKIRTS §6.4）。
- **其他许可**：思源黑体、Inter、JetBrains Mono（均 SIL OFL 1.1）和 Godot 引擎（MIT）的许可文本，放进设置卡的"许可"页和开始页的"制作人员与许可"（同一个组件，UI-VISUAL §8.11），安装包里另附一份 `LICENSES.txt`。不使用思源宋体。引擎部分用 `Engine.GetLicenseText()` 和 `Engine.GetCopyrightInfo()` 生成。
  - `LICENSES.txt` 写明 `campus.json`、`outskirts.json` 是 ODbL 1.0 下的派生数据库，附快照日期和 Overpass 查询原文（WLD-OUTSKIRTS §6.4）。
  - 音频素材的署名从 `licenses.json` 生成，进"许可"页和 `LICENSES.txt`（SYS-AUDIO §14.4）。
- **窗口标题**：`application/config/name` 设为"电光年华"，窗口标题与之相同。
- **描述**：`application/config/description` 设为"电子科技大学清水河校区，三渲二漫游作品"；导出时同一句写进 exe 的文件说明（`export_presets.cfg`，WP-00 维护）。
- **图标**：八角晶片（UI-VISUAL §6.3）：一块左上切角的方形芯片，四边各 3 根短引脚，芯片面上是图书馆八角穹顶的平面八瓣线（`ui.blue.200`），底色科技蓝 #1961A0，切角旁一枚暖色过孔 #FFE0A8；16、24 px 只留切角方块和八边形外框。源文件 `game/assets/hud/icons/app_icon.svg`（WP-35）。
  - `application/config/icon` 用 PNG；`application/config/windows_native_icon` 用 `.ico`，内含 16–256 px 各尺寸；
  - 导出预设里同一个 `.ico` 写进 exe（WP-00 维护）。

## 交付验收

- **WP-00**：
  - 主分支只剩 Godot 工程骨架（`game/`、`pipeline/`、`scripts/`、`tools/`、`docs/`）；旧网页栈在标签 `baseline-map` 里能完整取回；
  - `dotnet build -warnaserror` 和 `--headless --import` 通过；
  - `--title=0` 时窗口启动后只有雾幕、画面和署名（新界面由 WP-35 接入）。
- **WP-06**：
  - §2.1 的操作全部可用，包括按住 Ctrl 和双击 W 两种冲刺；
  - Esc、单击画面、窗口失焦的状态切换符合 UI-NAV §5；
  - 向 WP-42 和 WP-36 提供输入分发和相机接口（ENG-ARCH §3 的 `ICameraRig.Mode`、`Transit()`）；
  - 实现 `IObserverState`（旁观时取镜头位姿，行走时取 WP-42 的物理体）和按原因计数的 `ICameraRig.LockInput(InputLockReason)`，没有任何占用时才接受观者输入（ENG-ROADMAP §6 第 2、3 条）；
  - 活动范围是软约束，按 WLD-OUTSKIRTS §8：缓冲带减速、沿法线推回、`edge_fog`、"前方暂未开放"；出生和读档位姿合法化；`--pose` 不合法时非零退出；
  - 镜头穿过任何物体都不会卡住；
  - 用命令行参数可以复现所有审查视角。
- **WP-07**：
  - 在楼里、地下、水下、雾海四种情况下截图，没有黑洞、天空或闪烁；旁观穿进关着的楼看到简化室内，不空、不黑、不透天；任何楼体里都至少显示 I0（ENG-PERF §5 第 6 条）；
  - 开放楼内的房间灯按房间哈希读本楼通道，不要求与外面的窗灯逐间对应（WLD-INTERIORS §6.3）；
  - 穿墙、入地、入水的过程中，管线编译计数不增长（G16）；
  - `IMediumState` 与 `medium` 同源，介质切换时声音没有硬切（G21d）。
- **WP-01**：
  - 引擎启动画面、`--title=0` 的雾幕、署名、窗口标题、描述和图标都符合本节要求；正常启动的扉页与开始页按 SYS-TITLE §1 验收：扉页 1 s 内出现，转为实时之后三项管线编译计数不增（SYS-TITLE §1.2）；
  - 新游戏交还控制时，在任何天气种子、任何画面预设下都是秋 17:30、晴（§2.4）；
  - 支持 ENG-GATES §1 的全部命令行参数（含天气、画质、地图、开始页、存档、声音参数），未知参数、不合季节的天气、读档参数与世界类参数同时给出，都以非零退出码结束；
  - 扉页或雾幕后面的预热覆盖所有可达画面设置的并集，之后改画面设置不触发管线编译（SYS-GRAPHICS §3、G16）；
  - `LICENSES.txt` 含字体、引擎、ODbL 派生数据库和音频素材的许可（§6）。
- **行走模式和界面**：验收见 SYS-WALK §15 和 UI-NAV §10。

## 7 待验证（P0）

- **滚轮的累计方式**（§2.1）：按 `InputEventMouseButton.Factor` 累计、满 1 进一档，能否同时兼容高精度滚轮和触控板。验证：WP-06 用高精度滚轮和触控板实测。初值：按 `Factor` 累计，满 1 进一档。
- **地下仰视的表土切面**（§5.2）：相机所在格和邻格的地面换成剖切变体、背面输出表土色。验证：WP-07 原型看图。初值：按 §5.1 的剖切变体做法。
