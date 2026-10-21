---
id: REG-DECISIONS-SYSTEMS
title: 系统与工程决策
category: registry
parent: REG-DECISIONS
summary: 界面、存档、音频、性能与画面设置、工程五个领域的已定决策
keywords: [决策, 系统, 工程, 界面, 存档, 音频, 性能, 画面设置, 预算, 契约]
depends: [REG-DECISIONS]
related: []
contracts: []
data: []
gates: []
---

# REG-DECISIONS-SYSTEMS · 系统与工程决策

> **范围**：本文登记系统、界面、工程三个域的决策（界面、存档、音频、性能与画面设置、工程、地图与行走、时间季节天气），每条一句结论，附适用范围、细则所在的小节和状态。
> **不在本文**：用法、编号规则、状态含义和归卷规则在 REG-DECISIONS §0；数值与规则的细则以"细则所在"指向的文档为准；其他领域的决策在 REG-DECISIONS §0 列出的别的卷。
> **查阅**：先读 §0；按决策号或关键词 grep；界面看 §1，存档看 §2，音频看 §3，性能与画面设置看 §4，工程看 §5，地图与行走看 §6，时间季节天气看 §7

## 0 结论

本文装系统与工程类决策：细则所在第一份文档属系统、界面、工程的归这里（归卷规则见 REG-DECISIONS §0）。

### 0.1 接入要点

本文只约束登记，对其他施工方没有要求；用法见 REG-DECISIONS §0。

## 1 界面

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-066 | 色彩分工：场景暖、界面冷；清蓝只在界面层，不进 3D 材质，世界里不放蓝色全息；场景里的 `uestc.blue` 只给旗面、校车、单车 | 全项目 | UI-VISUAL §2.1；ART-STYLE §2.4 | 生效 |
| DECISION-067 | 界面主题是信息、数据、电子、芯片、光、计算、AI；专业克制，不做霓虹灯管、满屏辉光、数字雨这类效果；不用书页与银杏书签母题 | 界面 | UI-VISUAL §0；UI-VISUAL §1；UI-VISUAL §1 | 生效 |
| DECISION-068 | 第一主题色清蓝 #73C9F9（阶梯 400 档）；深端 800、900 两档是校色科技蓝 #1961A0、电子蓝 #003F8D，只作小面积；界面唯一的暖色点是 `window.warm` #FFE0A8，只表示"太阳 / 此刻" | 界面 | UI-VISUAL §3.1；UI-VISUAL §3.2 | 生效 |
| DECISION-069 | 两套主题：白天"晶白"、夜间"深屏"，按真实太阳高度离散切换（下穿 −0.5°、上穿 +0.5°），0.6 s 扫描前沿扫过；不做连续混色 | 界面 | UI-VISUAL §3.4 | 生效 |
| DECISION-070 | 常驻小部件（小地图、时间读数、临时提示、到达标题下的推荐小条等）默认固定深屏，可设为"随主题"；大页（大地图、设置卡、存档页）随昼夜；开始页不随主题 | 界面 | UI-VISUAL §3.4；SYS-TITLE §2.2 | 生效 |
| DECISION-071 | 面板默认不透明（晶白约 94%、深屏约 90%）；磨砂只给大页且可关，另有"降低透明度"开关 | 界面 | UI-VISUAL-COMPONENTS §2 | 生效 |
| DECISION-072 | 形状：每个容器只切左上一个 45° 角，边线一律 1 物理像素，对齐 8 px 网格但不画网格线，过孔灯表示状态，衍射光边只给键盘焦点和选中项 | 界面 | UI-VISUAL-COMPONENTS §2 | 生效 |
| DECISION-073 | 字体：中文思源黑体 SC，英文 Inter，编号、坐标、键帽用 JetBrains Mono，全部 OFL；不用思源宋体和书法字 | 界面 | UI-VISUAL-COMPONENTS §1 | 生效 |
| DECISION-074 | 动效：光是信号不是装饰，八个母题分三级；计算出来的东西按 `step_rate` 步进；辉光只在深色底上，半径 ≤ 4 px，同屏最多 3 处；每个母题都有"减少动态效果"下的替代 | 界面 | UI-VISUAL-MOTION §1；UI-VISUAL-MOTION §2.1 | 生效 |
| DECISION-075 | 时间组件是"24 h 天色光谱环"，钟点顺时针增加；加载界面以外的等待不做进度条，超过 1.5 s 才亮三颗过孔等待灯（扉页阈值 4 s） | 界面 | UI-VISUAL-COMPONENTS §4.3；UI-VISUAL-COMPONENTS §4.5；SYS-TITLE §1 | 生效 |
| DECISION-076 | 窗口图标是"八角晶片" | 窗口 | UI-VISUAL-COMPONENTS §3.3；ENG-RELEASE §7.1 | 生效 |
| DECISION-079 | 小地图在左上角常驻、北朝上（设置可改为随镜头、移动时显示或关），是漫游态唯一常驻的元素；按 U、`--ui=0`、大地图或设置卡打开时隐藏 | 界面 | SYS-ATLAS-UI §2；UI-NAV §2 | 生效 |
| DECISION-080 | 快捷键：M 大地图，G 大地图的目录页（含点位目录），N 小地图显隐，Y 切天气（按住 0.6 s 回到自动），`.` 前进 1 h；没有往回的键 | 界面 | UI-NAV §4.2 | 生效 |
| DECISION-081 | 底图是本机烘焙（DECISION-165）的精确矢量 SDF、运行时上色的"版图式校园平面图"，晶白与深屏各一套；外围街区画淡线，不可传送 | 平面地图 | SYS-ATLAS-UI §1 | 生效 |
| DECISION-082 | 传送默认经加载界面瞬移（DECISION-263），"飞过去"是第二选项；只有锚点、观景点、可进入室内的门口、路线起点可以传送 | 平面地图 | SYS-ATLAS-TRAVEL §1；SYS-ATLAS-TRAVEL §3.1 | 生效 |
| DECISION-083 | 锚点 13 个（6 个导览亭、7 个导览桩），做成校园里本来就有的导览设施；不用银杏、不用蓝色全息，在 3D 里不可点选 | 平面地图 | SYS-ATLAS-TRAVEL §2 | 生效 |
| DECISION-084 | 解锁：观光规则集的解锁策略是 `all-unlocked`，全部可用；`proximity` 只留给 RPG | 平面地图 | SYS-ATLAS-TRAVEL §4；ENG-ROADMAP §3 | 生效 |
| DECISION-085 | 冷启动依次是雾色、静态扉页；扉页就是开始页，背后不再交叉淡化成实时世界（DECISION-295）；菜单在扉页上就出、不等世界（DECISION-262），开始页上没有进度条 | 开始页 | SYS-TITLE §1；SYS-TITLE §2 | 生效 |
| DECISION-086 | 开始页菜单是继续 / 新游戏 / 存档 / 设置 / 制作人员与许可 / 退出；片名用细体宽字距加一行数据，不用书法 | 开始页 | SYS-TITLE §2.2；SYS-TITLE §2.3 | 生效 |
| DECISION-087 | 本阶段不做过场动画：新游戏、继续、返回标题都进一个带进度条的加载界面，进度条走满后淡出；新游戏直接建在默认状态（秋 17:30、晴、1×），以行走模式落到出生点 | 开始页、存档 | SYS-TITLE §3.1；SYS-SAVE-FLOW §2.5 | 生效 |
| DECISION-088 | 没有过场，也就不设跳过和"开场过场"设置项；加载界面的进度条只跟真实进展走，只增不减，不显示百分比和剩余时间 | 开始页 | SYS-TITLE §3.1；UI-VISUAL-COMPONENTS §4.5 | 生效 |
| DECISION-089 | 应用流程由 `IAppFlow` 状态机管理：Boot → Title → Entering → Playing → Leaving → Title，另有 Quitting；Leaving 是返回标题，Quitting 是退出程序 | 开始页 | SYS-TITLE §3；SYS-SAVE-FLOW §1.4 | 生效 |
| DECISION-090 | 界面缩放范围见 UI-NAV §3.5，不乘系统 DPI | 界面 | UI-VISUAL §4 | 生效 |
| DECISION-245 | 界面布局一律由锚点与容器推导：全屏根下设最宽 16:9 的安全框，四角件锚角，页面套标准骨架，位置不写坐标，尺寸来自内容与 ui_metrics 令牌并经 Theme 下发；界面缩放只用 ContentScaleFactor；绝对坐标只留在自绘件与地图画布内部 | 全部界面 | UI-VISUAL-COMPONENTS §5，UI-VISUAL §4，UI-NAV §5 | 生效 |
| DECISION-262 | 开始页的菜单不等世界：引擎、界面、音频和存档摘要就绪就在静态开始页上出菜单；地图基础、预热和继续终点与新游戏出生点的预载从双击起在后台并行；世界没好就选新游戏或继续，直接进加载界面接着已在跑的加载，不重来 | 开始页 | SYS-TITLE §1；SYS-TITLE §3.1 | 生效 |
| DECISION-295 | 开始页是独立的静态页：扉页是一张固定的图，存在资源里直接贴上，不由游戏渲染、不随存档与世界状态变，加片名块与菜单，背后不保留实时世界；进入游戏经加载界面直接在终点揭开，不做镜头运动；从双击起全力并行加载地图基础、管线预热与继续终点、新游戏出生点的预载（真实挂载保留），只按真实依赖排先后；世界实例在点击进入时才建。理由：同类开放世界 RPG 的主流做法，省下准备开始页实景的时间与带宽，全部用在玩家真正要去的地方；扉页用固定图，不必维护出图机位、云卡与变体 | 开始页、启动、存档 | SYS-TITLE §0、§1、§2.1、§3.1 | 生效 |
| DECISION-263 | 要遮住画面做加载或重建的转场一律用 DECISION-087 的加载界面：新游戏、继续、返回标题、传送、换图、旁观与行走互切、需要"应用"的画面设置、存档退出、截图与门禁换机位；删掉雾幕（全屏雾色遮挡层及其淡入、停留、散开流程）。设计好的画面内过渡照旧，不换成加载界面：换季"扬起 → 起雾 → 落定"、改时间的延时快进、天气连续过渡、点位跳转的镜头运动、"减少动态"下的 0.5 s 交叉淡化 | 全项目 | SYS-TITLE §3.1；SYS-MOTION §3；SYS-ATLAS-TRAVEL §3.2 | 生效 |
| DECISION-288 | 启动失败页的"重试"用引擎原生的退出后重启（`OS.SetRestartOnExit`，沿用原启动参数），当前进程走统一退出；不做进程内从失败步骤重入：终态错误在同一进程里多半修不好，冷启动最干净，同类主流游戏也是报错后重启或退出 | 开始页 | SYS-TITLE §1 | 生效 |

## 2 存档

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-091 | 3 个存档位 s1–s3，各自持续自动保存，"继续"取最近保存的那个；观光期不设手动槽 | 存档 | SYS-SAVE §1 | 生效 |
| DECISION-092 | 每次保存写一个新文件，保留最近几代，不覆盖、不改名；文件含头、元数据、分段正文和缩略图，用校验和发现损坏 | 存档 | SYS-SAVE-FORMAT §1.1 | 生效 |
| DECISION-093 | 序列化用 System.Text.Json 源生成；禁止用 Godot Resource、ConfigFile、`StrToVar`、`BytesToVar*`、`GetVar` 存读档 | 存档 | SYS-SAVE-FORMAT §1.3；SYS-SAVE-FORMAT §4 | 生效 |
| DECISION-094 | 保存时机：退出、返回标题、新游戏交还控制时立即保存（退出写完再退），漫游态每 5 分钟定时保存；只在安全时刻拍快照 | 存档 | SYS-SAVE-FLOW §1.1；SYS-SAVE-FLOW §1.2 | 生效 |
| DECISION-095 | 读档后的状态等于存档那一刻的落定状态；进行中的过渡、偶发事件和打开的面板不续播；断电、强杀或关机最多丢 5 分钟 | 存档 | SYS-SAVE §0；SYS-SAVE-FLOW §2.3 | 生效 |
| DECISION-096 | 新游戏一律从落金初灯开始 | 存档 | SYS-SAVE-FLOW §2.5 | 生效 |
| DECISION-097 | 设置只存一份 `user://settings.json`（分 ui、input、graphics、audio 四节），读值只接受基本类型（DECISION-232） | 设置 | SYS-SAVE-FORMAT §3 | 生效 |
| DECISION-098 | 自定义标记存进存档，ID 用本图内递增的整数，不用 `Guid.NewGuid`；存档以外只存筛选偏好 | 平面地图 | SYS-ATLAS-UI §3.5；SYS-ATLAS §3 | 生效 |
| DECISION-099 | 门禁、`--shots`、`--bench` 下默认不写存档（`--save=off`） | 门禁 | ENG-GATES-SHOTS §1 | 生效 |
| DECISION-100 | 存档按以后的 RPG 可扩展：各系统用 `[SaveSection]` 声明自己的段，字段有增减就升版本并写迁移函数 | 有状态的包 | SYS-SAVE-FORMAT §1.4 | 生效 |
| DECISION-171 | 保存失败只记日志，不弹框也不拦退出，上一代存档仍有效 | 存档 | SYS-SAVE-FLOW §1；UI-NAV §3.6 | 生效 |
| DECISION-240 | 读档恢复存档时的模式（旁观或行走），行走位置落不了地才按修正表切旁观 | 存档 | SYS-SAVE-FLOW §2.3 | 生效 |
| DECISION-241 | 传送到达不单独存档，靠漫游定时存档与退出、返回标题时的存档；存档请求原因不为传送加项 | 存档、传送 | SYS-ATLAS-TRAVEL §3；SYS-SAVE-FLOW §1 | 生效 |
| DECISION-242 | 新游戏固定在本图出生点（`defaultSpawn`，须带行走落点）以行走模式交还控制；清水河出生点为 P01 日晷·望穹顶 | 新游戏、观察模式 | SYS-VIEW §2 | 生效 |

## 3 音频

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-101 | 做环境声、观者动作音、空间声学和界面音效；不做配乐，只在开始页放一段原创音乐 | 全项目 | SYS-AUDIO §0；SYS-AUDIO §6 | 生效 |
| DECISION-102 | 开场音乐委托或自制一段 1–2 分钟的原创钢琴或弦乐小品；做好之前不放，开始页只放高空环境床 | 开始页 | SYS-AUDIO §6 | 生效 |
| DECISION-103 | 宣邦楼钟楼 07:00–22:00 整点敲单音铜钟，按 12 小时制敲 1–12 下，钟楼不放旋律，不用威斯敏斯特钟声；钟室挂一口静止铜钟，整点钟锤轻敲（必做），钟机不出"咔"声 | 钟楼 | SYS-AUDIO-AMBIENCE §3；SYS-MOTION-LIFE §2；MAP-QSH-SOUTH §5 | 待原型验证 |
| DECISION-104 | 傍晚 17:40–18:10 放一段远处的校园广播：开头一段听得出曲调的原创旋律，之后的人声听不清；不做上下课铃 | 全校 | SYS-AUDIO-AMBIENCE §3 | 待原型验证 |
| DECISION-105 | 钟声和广播只在流逝 ≤ 10× 时出现，快进和拨表盘跳过的不补 | 全项目 | SYS-AUDIO-AMBIENCE §3；SYS-AUDIO-AMBIENCE §3；SYS-MOTION-LIFE §3 | 生效 |
| DECISION-106 | 素材不求真实：用免费开源、可商用的素材，CC0 优先，其次是可随游戏发布的免费包；不做实地采录，鸟种不求本地准确；需要时用 EQ、变调、分层、混响、剪辑调整 | 音频素材 | SYS-AUDIO §8；SYS-AUDIO-AMBIENCE §1.3 | 生效 |
| DECISION-107 | 素材许可：CC BY 可用但要署名；NC、ND、BBC、BY-SA、GPL 一律不用；只许随游戏发布的素材（如 Sonniss）不进公开仓库 | 音频素材 | SYS-AUDIO §8 | 生效 |
| DECISION-108 | 远雷要做：低沉、远、可以关，跟云内闪同步；偶发事件本身无声，它引起的抖落、积雨、旗动有声 | 全项目 | SYS-AUDIO-AMBIENCE §3；SYS-AUDIO-AMBIENCE §3；SYS-WEATHER §5 | 生效 |
| DECISION-109 | 音量分五组：总、环境、动作、界面、音乐；声音设置存在 `settings.json` 的 audio 节 | 设置 | SYS-AUDIO §7；SYS-SAVE-FORMAT §3 | 生效 |
| DECISION-110 | 脚步默认安静、可调；闸机和门不出电子提示音，单车不响铃；飞行风声实时合成 | 观者声 | SYS-AUDIO-FOLEY §1.1；SYS-AUDIO-FOLEY §1.5；SYS-AUDIO-FOLEY §1.6 | 生效 |
| DECISION-111 | 门的声音：开放楼入口门固定敞开，没有开关声；能推开的门推开时一声，停在开位和回关途中无声，关好落锁一声；电梯不出"叮" | 开放室内 | SYS-AUDIO-FOLEY §1.5；SYS-WALK-BOUNDS §3 | 生效 |
| DECISION-112 | 同一动作在不同地方听起来不同（窄处脚步更明显），用"4 m 声学网格 + 房间与门户 + 行走射线细化 + 声源侧混响区"实现，不上 Steam Audio；推迟到主线之后，主线先做按地面材质的脚步（DECISION-233） | 空间声学 | SYS-AUDIO-FOLEY §2.1；SYS-AUDIO-FOLEY §2.2 | 生效 |
| DECISION-113 | 不用 `AudioStreamRandomizer`，改用自研的洗牌选择器；不给 `AudioStreamPlaylist.Shuffle` 和 `AudioServer.PlaybackSpeedScale` 赋值 | 音频实现 | SYS-AUDIO-FOLEY §1.4；ENG-ARCH §2 | 生效 |
| DECISION-114 | 没有听得清的人声：人迹只用远处、听不清、来自看不见处的群声；界面悬停默认无声，不加语音提示 | 全项目 | SYS-AUDIO §4；SYS-AUDIO §5；INV-3 | 生效 |
| DECISION-115 | 一次性声一律按真实秒排程，任何流逝倍率下任意 1 s 不超过 3 个 | 全项目 | SYS-AUDIO §3；INV-10 | 生效 |
| DECISION-116 | 音频预算：主线程不到 1 ms（从帧时余量里划出），同时在播的声源有上限（以 `AudioVoicePool` 为准），常驻内存与导出包里的音频都在百兆级 | 性能 | SYS-AUDIO §9 | 生效 |
| DECISION-159 | 空间声学（SYS-AUDIO-FOLEY）的主线程占用含在音频合计里，不另列份额 | 音频 | SYS-AUDIO §9；SYS-AUDIO §10 | 生效 |

## 4 性能与画面设置

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-117 | 主线画面预设三档：均衡、精致、极致，默认精致，另有自定义；设备档与兜底的流畅预设推迟到主线之后（DECISION-233） | 画面设置 | SYS-GRAPHICS §0；SYS-GRAPHICS §1 | 生效 |
| DECISION-118 | 风格底线不可关：描线、分阶明暗与按材质的影色、柔光（高光晕开）、高光肩部、平涂贴图、雾与调色板、窗灯光晕等；实时阴影和西湖倒影只能降到最差值 | 画面设置 | SYS-GRAPHICS §2；ENG-BUDGET-RULES §1 | 生效 |
| DECISION-119 | 风格默认可关：移轴、镜头光斑、光柱、暗角 | 画面设置 | SYS-GRAPHICS §2 | 生效 |
| DECISION-120 | 不做超采样 | 画面设置 | SYS-GRAPHICS §1 | 生效 |
| DECISION-121 | 自动调节画质推迟到主线之后（DECISION-233）；做的时候只往下调，不超过玩家所选，跳过玩家手动改过的项 | 画面设置 | SYS-GRAPHICS §0；SYS-GRAPHICS §5 | 生效 |
| DECISION-122 | 画面设置只改运行时选级和引擎参数，不改烘焙、碰撞、世界状态和种子，不影响声音；会清空管线的设置不做成选项 | 画面设置 | SYS-GRAPHICS §0 | 生效 |
| DECISION-123 | 不做静止降频：镜头静止时帧率不降（`low_processor_mode` 关） | 性能 | ENG-PERF-ADAPT §1.1；ENG-BUDGET-RULES §2 | 生效 |
| DECISION-124 | 视觉验收的基准是精致预设、1080p 原生 | 验收 | SYS-GRAPHICS §0.1；ART-FIDELITY §4 | 生效 |
| DECISION-125 | 安装包不预设体积上限：内容做到哪由作者定，体积随内容涨；导出时报安装目录与分发压缩包（7-Zip LZMA2）的体积，含随包的全图烘焙缓存，按数量级判断与当前设计和内容量是否相称，新增内容带来的增量同样按数量级看 | 构建 | ENG-GATES §1；ENG-BUDGET §3；ENG-PERF-STREAM §4 | 生效 |
| DECISION-127 | 总账各项统一留约 10% 作主代理余量，界面加音频的主线程合计不到 1 ms | 预算 | ENG-BUDGET §0 | 待原型验证 |
| DECISION-128 | 室内预算（楼内视角，精致）：≤ 80 万三角形、≤ 250 次绘制，大厅和中庭 ≤ 100 万三角形 | 开放室内 | ENG-BUDGET §2 | 生效 |
| DECISION-129 | 外围预算：三角形 6%、绘制调用 5%、阴影绘制调用 3%、GPU 3% | 外围 | ENG-BUDGET §1 | 生效 |
| DECISION-130 | 存档预算：快照在主线程帧末拍，不做 I/O、不造成可见卡顿，不占主线程余量；序列化与写盘在后台 | 存档 | SYS-SAVE-FLOW §1.2；INV-12 | 生效 |
| DECISION-165 | 烘焙产物随包发（同类主流游戏的构建期烘焙）：发布时用开发预烘的同一入口把全图烘好放进安装包，玩家首次启动只编译着色器，不现烘；缓存缺失或损坏时才在本机用同一个 `Build()` 补烘，开始页起后台把全图补齐，碰到的格优先；开发期照常本机烘焙、每批收口全图预烘；不发几何启动集（DECISION-232） | 构建、流式 | ENG-ARCH §2；ENG-ARCH §4；ENG-PERF-STREAM §1；ENG-PERF-STREAM §4；ENG-GATES §1 | 生效 |
| DECISION-169 | 最低配置按近两年的核显轻薄本：酷睿 Ultra 第一代（Arc 核显）或 Radeon 780M 同级及以上、16 GB 双通道内存、SSD；更旧更弱的核显（UHD 6xx、Vega 8、Iris Xe 等）不做适配 | 全项目 | ENG-BUDGET §2；SYS-GRAPHICS §5 | 生效 |
| DECISION-229 | 烘焙只重烘改到的格：每格只看它用到的输入改没改，用最省事的判断办法，说明文字不算；玩家侧游戏版本一变就清缓存；不叠多层哈希，不出整图行走产物 | 烘焙、本机缓存 | ENG-ARCH §4 | 生效 |
| DECISION-230 | 行走用常规做法：地面每格一个高度场，楼和物件用盒子，0.35 m 以下薄边不单独做碰撞，只加载身边；落点与回位运行时算；不预算整图可行走栅格、安全点网格和精确支持面；碰撞与画面差几厘米可以接受；G15 日常只跑固定路线加改到区域撒点 | 行走、碰撞、门禁 | SYS-WALK-BOUNDS §1、§4、§7，ENG-GATES | 生效 |
| DECISION-238 | 门禁按 demo 阶段收缩：保留项以 ENG-GATES §1.2 为准；删 G8 静态预算（G10 实测代替）、G9 近景巡检、G11 核显实测、G13 过渡逐帧判定、G16，G12 时季与 G17 预设并进 G7 的集中测试出图；编号不复用；gate.ps1 只跑保留项，不做门禁结果缓存 | 全部工程 | ENG-GATES §1 | 生效 |
| DECISION-239 | 按图数据只去重、不搬造型：实体的坐标、轮廓、层数、名称只在按图数据里，各区代码按 ID 从世界模型读，不抄副本；只供测试对照的数据副本删掉；本图专用的造型与组合及其尺寸留在每图程序集，各在本区 Tune 一处，不改成 JSON | 每图程序集 | ENG-ARCH §6 | 生效 |
| DECISION-243 | 取要素的一段用数据里的选段要素（两个端点）指明，代码按端点取轮廓上的那一段；不按顶点序号、最长、最靠某侧这类条件挑，端点离轮廓过远即报数据错误 | 每图程序集 | ENG-ARCH §6 | 生效 |
| DECISION-244 | 全局唯一权威：公共底座按使用范围分层，公共函数清单由 G4 守；每区 ID 目录；默认值只在数据；运行时状态只有一个写入方，改用租约与命令；文档不抄数值；门禁有改动却选 0 项算未完成 | 全部工程 | ENG-ARCH §6；STD-CODE §2、§3、§6；ENG-GATES §1.1 | 生效 |
| DECISION-247 | 文档已写、代码未做的行走与旁观机制纳入实现：校界缓停、安全点与游泳、模式切换落到最近安全点、视场角可调、视角晃动、台阶镜头平滑、自动跳跃、地图页压低环境声与安静模式；行走顶住边界的软墙提示取消 | 行走、旁观、音频、地形、街道家具 | SYS-WALK-BOUNDS；SYS-WALK；SYS-VIEW；SYS-AUDIO-AMBIENCE；WLD-LIFE-STREET | 生效 |
| DECISION-248 | 回安全点一律用最简单的规则：落点不可走、回位、脱困都直接硬切到最近的安全点，不滑行、不飞过去、不淡入淡出 | 行走 | SYS-WALK §4；SYS-WALK-BOUNDS §4 | 生效 |
| DECISION-294 | 旁观与行走互切一律经加载界面：不做降落、起飞、视场角与声音的连续过渡；切到行走直接放到离镜头最近的安全点，切回旁观在当前眼位原地换模式；落位只用回安全点同一条规则（DECISION-248），不按位置分别维护落地办法 | 行走、旁观 | SYS-WALK §4 | 生效 |
| DECISION-250 | 游泳只做极简版：只有西湖、东湖主水面能游，不下潜、不跳、不蹲、不疾跑，出水靠水变浅；入水、出水各一声；水面反馈、划水声、泳姿、泳态存档、游泳馆池下水与水禽让开留到主线之后 | 游泳、行走、水体 | SYS-WALK-BOUNDS §4；WLD-WATER §4 | 生效 |

## 5 工程

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-005 | 引擎用 Godot 4.7.2-stable .NET，代码全部用 C#、不写 GDScript，渲染器 Forward+，默认 D3D12 | 全项目 | ENG-ARCH §1；PRD-SCOPE §1；ENG-BUDGET-RULES §2 | 生效 |
| DECISION-010 | RPG（入学到毕业）方向暂缓，只留架构接口（存档段与迁移、地图服务、解锁策略 `proximity`、多地图、门的锁状态），重新提出之前不展开设计 | 全项目 | ENG-ROADMAP；ENG-ARCH §7 | 生效 |
| DECISION-132 | 门禁编号以 ENG-GATES §1.2 的门禁表为准；全局不变量改名为 INV-1 至 INV-12，与门禁区分（引用写法见 DECISION-156） | 全项目 | ENG-GATES §1；PRD-INVARIANTS §1；STD-CONVENTIONS §3.3 | 生效 |
| DECISION-133 | 权威文档分工：契约与帧顺序以 `game/contracts/` 源码为准（说明见 ENG-ARCH §3），全局参数以 ENG-PERF §1.5 为准，预算与硬规则以 ENG-BUDGET 为准，门禁以 ENG-GATES 为准，某系统的细则以该系统自己的文档为准 | 全项目 | ENG-ARCH §3；ENG-PERF §1.5；ENG-BUDGET §1；ENG-BUDGET-RULES §1；ENG-GATES §1 | 生效 |
| DECISION-135 | 帧顺序的每一步有唯一的优先级值，用节点 `ProcessPriority` 固定，任何两步不同值；权威是契约里的 `FramePriority` | 全项目 | `game/contracts/` 源码；ENG-ARCH §3；SYS-TIME §1 | 生效 |
| DECISION-136 | 流式拆成两步：`Stream`（−500）只做选级与请求，`StreamMount`（900）做挂载与碰撞入树 | 内核 | ENG-PERF-STREAM §1；ENG-ARCH §3 | 生效 |
| DECISION-137 | 地图 ID 一律写 `MapId("qingshuihe")`，不用 "qsh"；实体 ID 的 `qsh:` 前缀是 OSM 数据的写法，不是地图 ID | 全项目 | SYS-MAPS §1.1；SYS-ATLAS §2；STD-CONVENTIONS §3.1 | 生效 |
| DECISION-138 | 服务命名：`IMapService` 管地图加载与切换（SYS-MAPS），`IAtlasService` 管平面地图的标记、解锁、追踪与传送（SYS-ATLAS），两者不得混名 | 全项目 | ENG-ARCH §3 | 生效 |
| DECISION-139 | 按图的数据一律从 `IMapContext` 取，通用代码里不写死数据路径、实体 ID 和地图 ID | 全部包 | SYS-MAPS §6 | 生效 |
| DECISION-141 | 开放室内的室内包与碰撞包也是本机烘焙，进本图的本机缓存，不另设 PCK | 打包 | SYS-MAPS §1.2；ENG-PERF-STREAM §4；WLD-INTERIORS-BUILD §1.1 | 生效 |
| DECISION-142 | 白名单着色器含 `ui_panel`、`ui_screen`、`map_base`、`precip`；外围不新增着色器 | 渲染 | ENG-PERF §1.4 | 生效 |
| DECISION-143 | 全局参数（含 `map_rect`、`edge_fog`、`interior_light` 等）一律登记在 ENG-PERF §1.5 | 渲染 | ENG-PERF §1.5；WF-BUILD §3 | 生效 |
| DECISION-146 | 截图与门禁的默认参数串固定：默认状态、流逝暂停、界面隐藏、精致预设、自动调节关、清水河、不写存档、不开声音 | 门禁 | ENG-GATES-SHOTS §1 | 生效 |
| DECISION-160 | 最小预留做 ENG-ROADMAP §3 的六条预留：规则集、观者状态、按原因计数的输入占用、枚举穷举纪律、偶发事件数据化、RPG 名字占位；观光规则本身不变，写进 `sightseeing` 规则集 | 全项目 | ENG-ROADMAP §3；ENG-ARCH §2；ENG-ARCH §3；STD-CONVENTIONS §3.6 | 生效 |
| DECISION-167 | 测试按最小必要：推送前只跑快检，核心体验改动涉及时跑核心专项，其余集中做一次 | 全部包 | ENG-GATES §1；WF-RULE §9 | 生效 |
| DECISION-170 | 许可：代码与脚本用 MIT；原创美术、音频、文案与设计文档保留所有权利；OSM 派生数据按 ODbL 1.0，每个公开版本附地图数据包；第三方素材按各自许可；公开说明这是个人创作、不是校方作品 | 全项目 | ENG-RELEASE；根目录 LICENSE 与 README | 生效 |
| DECISION-172 | 网格的细节级别契约改为"误差加该级自己的索引数组"，不再只靠截取同一索引的前缀；前缀只用于整件移除独立小件 | 全部建模包 | ENG-ARCH §3；ENG-PERF §1.2 | 生效 |
| DECISION-181 | 本项目是作者的个人实验性作品：不上 Steam 或其他商业平台，不做商店页、宣传素材、成就、云存档和平台集成；对外只按 ENG-RELEASE 的 GitHub 整包方式发布；调研与施工都不再考虑商业发行与上架要求 | 全项目 | ENG-RELEASE | 生效 |
| DECISION-214 | 契约程序集不再用 PublicApiAnalyzers 与 `PublicAPI.*.txt` 清单；契约改动照旧先申请、与全部使用方同批改，签名只在源码里 | `game/contracts/` | ENG-ARCH §2、§3；WF-BUILD §1 | 生效 |
| DECISION-254 | 删去从未实现的 G18 地图一致性与 G19 连通（含 G19a–c），门禁收到 12 项；连通与可达由 G15 行走路线兜底，标记一致性靠集中测试的截图与界面检查；编号不复用 | 全部工程 | ENG-GATES §1.2 | 生效 |
| DECISION-255 | 加载链路按根因整改：开发、游玩与测量的构建一律开编译优化；缓存判新旧只哈希源数据与代码戳且先于任何生成；流送目录由站点声明的范围得出；地形烘成 1 m 高度栅格供地面、碰撞与贴地查询 | 构建、流式、地形 | ENG-ARCH §4；ENG-PERF-STREAM §1.1 | 生效 |
| DECISION-256 | 启动路径只读不建：双击启动不编译、不导入，正式产物在就直接起引擎（DECISION-302）；启动到能走只起引擎与服务、核对缓存、读首视角与出生点要的格，只准备玩家真正要去的一个终点，不为不会展示的画面等待；生成只在缓存缺失时发生。目标从双击（启动脚本进入）计：可操作开始页热启动 ≤5 s、冷启动 ≤10 s；首次运行（驱动首次编译管线）从双击到进入游戏后的第一个画面 ≤30 s；新游戏到首次能走，出生点已预载 ≤3 s，开始页一出现就点 ≤10 s。冷热写明几何、管线与系统文件缓存的状态；体验测量不开逐帧日志与采样，诊断测量另记（RESEARCH-034） | 构建、启动、流式 | ENG-ARCH §4；`budgets.json` 的 `cache` | 生效 |
| DECISION-260 | 画面被扉页或加载界面整屏盖住时，流式挂载不受每帧 1 格、1.0 ms 的限制，多格连续挂载，只给盖住的界面留出刷新余量；每帧限额只为画面可见时不卡帧而设，盖住时照样节流会把等待拉长成"格数 × 帧时" | 全项目 | ENG-PERF-STREAM §1.2；ENG-BUDGET-LOD §1 | 生效 |
| DECISION-261 | 管线预热按"着色器 × 顶点格式 × 渲染状态"组合做，全集在构建期由白名单着色器与烘焙统计定出；启动时在扉页下把每个组合隐藏实例化一次，交给引擎的管线预编译与 ubershader（Godot 4.4 起 Forward+ 的官方做法），一批做完；不画样本、不按帧数等稳定、不逐材质实例取证（同一着色器换参数值不需要新管线）；格的挂载、LOD 切换与撤盖不等预热；导出开 Shader Baker；验收沿用"撤盖后三项编译计数不增" | 全项目 | ENG-PERF §2；ENG-BUDGET-RULES §1 | 生效 |
| DECISION-267 | 只给运行时用的契约 `Campus.Contracts.Runtime` 可以引用 Godot 类型；生成侧契约 `Campus.Contracts` 与生成层仍是纯 .NET | 全项目 | ENG-ARCH §0、§1 | 生效 |
| DECISION-289 | 为看不出的误差换算法是过度工程化：为消灭看不见的浮点缝改用大数、有理数精确运算的几何构造、追交会身份一类方案不立项，已有候选与记录删净；生成几何用现行稳定构造，见到真实可见缝只修那一处 | 建筑与部件几何 | WF-RULE §1、§15 | 生效 |
| DECISION-297 | 先用 Godot 原生，不重复造轮子：写功能前按次序判断 Godot 原生、.NET 自带、已引用的成熟库、新引入的库、公共层能不能做，原生做得到且更简单就用原生，都不行才自写并在代码处写明理由；改到已有的自写实现时同样先判能否换成原生，量小当批换、量大开行；收口审计查这一项 | 全项目代码 | CORE-INDEX §5；STD-CODE §3；WF-FLOW §3.1 | 生效 |
| DECISION-298 | 顶层代码规范补运行时可靠性（RESEARCH-035）：CORE-INDEX §5 重写为八条写法规范，新增 §6 运行时与可靠性七条（唯一编排链、谁开谁关先停再排空后释放、失败带原异常交到底、异步有主、就绪看真实状态、构建期做完、存档只存落定状态）；细则落 STD-CODE §4、§7 与 ENG-ARCH §2「生命周期与发布」「就绪与等待」，停滞判据只在后者写一次；全仓顶层节号引用同批更新 | 全项目代码与钩子注入 | CORE-INDEX §5、§6；STD-CODE §4、§7；ENG-ARCH §2 | 生效 |
| DECISION-302 | 启动构建不另设判鲜：双击启动只看正式输出产物在不在，在就直接起引擎，缺了才跑一次原生构建与导入（首次运行）；产物新鲜由统一集成与收口预烘的原生增量构建保证，测量、截图、预烘入口开跑前自己跑原生增量构建；删开发回执与全输入身份层，烘焙指纹清单是唯一构建身份，生成步骤挂原生 Inputs/Outputs、真变才跑。起因是自写判鲜每次启动 5.2 s、失鲜后 Rebuild 加导入 54 s，且 374–382 反复逃逸（TOOL-383） | 构建、启动 | ENG-ARCH §4 | 生效 |

## 6 地图与行走

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-016 | 活动范围的"中心"取校界形心约 (50, 77)，即 `MapDescriptor.Center` | 清水河 | SYS-MAPS §2 | 生效 |
| DECISION-017 | 全图数据纹理范围 `map_rect` 取 ±1280 m，不随外围扩大；平面地图页范围另设 `atlasRect` ±1536 m | 清水河 | SYS-MAPS §7；SYS-ATLAS-UI §1.3 | 生效 |
| DECISION-018 | 多地图只做可扩展架构，当前只有清水河一张图；按图的数据放 `game/data/maps/qingshuihe/`，代码目录不动 | 全项目 | SYS-MAPS §0；SYS-MAPS §1.1 | 生效 |
| DECISION-019 | 世界时间、季节、流逝倍率和 `AnimTime` 全局共享一条时间线；天气按图各一份前向过程，进图时补算到当前时间 | 多地图 | SYS-MAPS §4；SYS-WEATHER-PROCESS §1.5 | 生效 |
| DECISION-020 | 跨图切换经加载界面（DECISION-263），一个 World3D，先卸后装；`MapTimeRule.Fixed` 预留不用 | 多地图 | SYS-MAPS §2；SYS-MAPS §3 | 生效 |
| DECISION-031 | 室外的道路、广场、桥、台阶、球场、看台、草坪全部可走；室内只有开放部分可走 | 行走模式 | SYS-WALK §0；SYS-WALK-BOUNDS §1 | 生效 |
| DECISION-186 | 共享单车：载具只做匿名共享单车，不用真实品牌；骑行是行走模式里的运动分支，与步行、游泳互斥，不新增 ViewMode，用运动学解算、不做刚体平衡；只在许可区骑，固定停放点借还，切旁观、传送、换图时自动回收，读档一律恢复成步行；归 SYS-WALK-BIKE（共享单车运行与取还），按 DECISION-183 排在建模活之后 | 行走模式 | SYS-WALK-BIKE §1；SYS-WALK §3.2；SYS-SAVE §2；UI-NAV §4；WLD-LIFE-STREET §1 | 待原型验证 |
## 7 时间、季节、天气

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| DECISION-034 | 世界状态有三个量：钟点、季节、天气 | 全项目 | SYS-TIME §0；INV-8 | 生效 |
| DECISION-035 | 时间不能逆转：同一世界实例内 `WorldHours` 只增不减；时间环只能顺时针拨，`SetClock` 跳到下一次出现的钟点（最多前进 24 h），没有 −1 h 的按钮和快捷键 | 全项目 | SYS-TIME §1；ENG-ARCH §3；UI-NAV §3.1 | 生效 |
| DECISION-036 | 风、水、一拍二用的 `AnimTime` 是实时秒 mod 480，与世界时间无关；不做落金循环 | 全项目 | SYS-MOTION §1；SYS-TIME §0 | 生效 |
| DECISION-037 | 默认状态是秋 17:30、晴、自动天气、流逝 1×，天气保持晴到秋季黄昏带结束（约 19:36）；它是新游戏的终态和新存档的初值 | 清水河 | SYS-TIME §0；SYS-WEATHER-PROCESS §1.6；SYS-SAVE-FLOW §2.5 | 生效 |
| DECISION-038 | 流逝档位为暂停 / 1× / 10× / 60× / 360× / 1440×，默认 1×；季节不自动推进 | 全项目 | SYS-TIME §1；ENG-ARCH §3 | 生效 |
| DECISION-039 | "回到落金初灯"是正向换季到秋、快进到下一个 17:30（已在 17:22–17:40 之间则不动）、流逝 1×、自动天气、保持晴到黄昏带结束，不移动镜头；其他地图泛化为"回到本图默认状态" | 界面 | SYS-TIME §1；SYS-WEATHER-PROCESS §1.6；SYS-MAPS §4 | 生效 |
| DECISION-040 | 季节可以任意选，一律用正向转场（扬起 → 起雾 → 落定），不做倒放 | 全项目 | SYS-MOTION §3 | 生效 |
| DECISION-041 | 新游戏和读档都是换世界实例（`IWorldState.Instance` 加 1），不算时间倒流；开始页停着的时间、关着游戏的现实时间都不计入世界时间 | 全项目 | SYS-TITLE §3.1；SYS-SAVE-FLOW §2.2；SYS-SAVE-FLOW §3 | 生效 |
| DECISION-042 | 天气界面固定 8 格加"自动"；降水随季节变形（春夏秋是雨、冬季是雪），雷雨只在春夏；不做雾霾、冰雹、雨夹雪、落地闪电、镜头水滴 | 全项目 | SYS-WEATHER §1；UI-NAV §3.7 | 生效 |
| DECISION-043 | 自动天气是前向随机过程：随机时段、随机时长，只走验证过的组合，晴约占四分之三；自动时显示一行预报（预报行） | 全项目 | SYS-WEATHER-PROCESS §1；UI-NAV §3.7；UI-VISUAL-COMPONENTS §4.4 | 生效 |
| DECISION-044 | 天气种子每个存档一个，种子和已生成的天气队列都存进存档 | 存档 | SYS-WEATHER-PROCESS §1.4；SYS-SAVE-FLOW §2.4 | 生效 |
| DECISION-045 | 闪电只做远处积雨云的云内闪，不画落地闪电、不全屏闪白；闪电和彩虹都不算偶发事件 | 全项目 | SYS-WEATHER §5；INV-10 | 生效 |
| DECISION-046 | 太阳雨不新增天气格，是"雨 → 雨后初晴"这条转移边开头的 30–90 s；英雄帧第 12 张是"秋 17:30 太阳雨" | 天气 | SYS-WEATHER §2；SYS-WEATHER-PROCESS §1.3；ART-COLORSCRIPT §1.5 | 生效 |
| DECISION-047 | 彩虹只在雨后初晴出现，主虹半径随太阳升高在 30–42° 之间放大（不按真实的固定 42°），太阳在 34–40° 之间时淡出 | 天气 | SYS-WEATHER §5；ART-SKYLIGHT-ATMOSPHERE §1 | 生效 |
| DECISION-049 | 下雪由天气过程决定（冬季夜雪多在 19–23 时开始），不按固定时段 | 冬季 | SYS-WEATHER-PROCESS §1.3；ART-COLORSCRIPT §2 | 生效 |
| DECISION-050 | 阴雨天两阶不丢：投影强度乘 (1 − 0.8 × 阴度)，白天不低于 0.2 | 天气 | SYS-WEATHER §6；ENG-PERF-LIGHT §2.1 | 生效 |
| DECISION-158 | 一拍二的 CPU 侧节拍（`IWorldState.Step12`、飘落池的发射时刻）与着色器同读 `step_rate`（SYS-GRAPHICS 写：一般 12 Hz，帧率上限 30 时 15 Hz），两侧在同一帧换姿态 | 全项目 | SYS-MOTION §2.2；ENG-ARCH §3；ENG-PERF §1.5 | 生效 |
| DECISION-162 | 秋季黄昏带（太阳 +8° 到 −7°）约 17:16–19:36；初灯组开始亮灯的时刻、SYS-AUDIO声景的"黄昏"段和默认状态"保持晴"的终点都与它一致 | 清水河 | SYS-WEATHER-PROCESS §1.2；ART-COLORSCRIPT §3；SYS-AUDIO-AMBIENCE §1.1 | 生效 |