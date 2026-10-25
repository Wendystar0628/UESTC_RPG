# 小节目录

> 由 `scripts/docs/gen-index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（CORE-INDEX §1）。行号以生成时为准，原文改过以后可能差几行，以标题文字为准。

## CORE-INDEX · 《电光年华》开发总纲

`docs/README.md` · 8.8k 字 · 全项目唯一的顶层，只放每次开发都要掌握的全局：项目与约束、信息地图（信息在哪、仓库目录、只列标题的文档地图），以及全员共守的工作原则、协作与分工、文档规范与代码规范；具体内容一律在下层按需读
关键词：总纲、顶层规则、项目与约束、硬约束、优先级、信息地图、文档地图、仓库目录、信息在哪、怎么查文档、按需读、钩子注入、电光年华、工作原则、协作与分工、文档规范、代码规范、实现优先、最小实现、最小测试、根因修复、同批清旧、维护体系、按完整改动提交、单一权威、写给大模型、文档精简、失败即报错

- CORE-INDEX §0 项目与约束 · L20 · 640
  - ★ CORE-INDEX §0.1 接入要点 · L27 · 277
- CORE-INDEX §1 信息地图 · L34 · 1.7k
- CORE-INDEX §2 工作原则 · L84 · 1.8k
- CORE-INDEX §3 协作与分工 · L97 · 925
- CORE-INDEX §4 文档规范 · L105 · 930
- CORE-INDEX §5 代码规范 · L115 · 1.1k
- CORE-INDEX §6 运行时与可靠性 · L126 · 950

## PRD-INDEX · 作品总纲

`docs/product/PRD-INDEX-作品总纲.md` · 1.0k 字 · 作品域总纲：作品是什么、做什么不做什么、观光规则集的硬约束；本域通用原则与文档树
关键词：作品、作品总纲、文档树、PRD

- PRD-INDEX §0 结论 · L21 · 543
  - ★ PRD-INDEX §0.1 接入要点 · L28 · 188
- PRD-INDEX §1 文档树 · L33 · 79

## PRD-INVARIANTS · 全局不变量

`docs/product/PRD-INVARIANTS-全局不变量.md` · 2.5k 字 · 观光规则集的 12 条硬约束 INV-1–INV-12；每条只写长期不变的约束，具体设计与数值在"详见"的文档
关键词：全局不变量、INV、硬约束、观光规则集、sightseeing、不变量、世界规则

- PRD-INVARIANTS §0 结论 · L21 · 400
  - ★ PRD-INVARIANTS §0.1 接入要点 · L27 · 133
- PRD-INVARIANTS §1 不变量 · L32 · 1.6k

## PRD-SCOPE · 作品与范围

`docs/product/PRD-SCOPE-作品与范围.md` · 2.2k 字 · 作品是什么、画面与地图的总体取舍、当前范围做什么不做什么、主线的定义；每项一两句加指向
关键词：作品定位、作品与范围、当前范围、不做什么、主线、主线版、观光规则集、sightseeing、落金初灯、视觉优先、不是地图复刻、电光年华

- PRD-SCOPE §0 结论 · L21 · 261
  - ★ PRD-SCOPE §0.1 接入要点 · L27 · 94
- PRD-SCOPE §1 作品与取舍 · L32 · 715
- PRD-SCOPE §2 当前范围 · L42 · 481
- PRD-SCOPE §3 主线（DECISION-168） · L54 · 187

## ART-INDEX · 画面总纲

`docs/art/ART-INDEX-画面总纲.md` · 1.4k 字 · 画面域总纲：风格、明暗与描线、视觉精度、造型、天空与光照、色彩脚本的视觉语言；本域通用原则与文档树
关键词：画面、画面总纲、文档树、ART

- ART-INDEX §0 结论 · L21 · 613
  - ★ ART-INDEX §0.1 接入要点 · L30 · 174
- ART-INDEX §1 文档树 · L35 · 325

## ART-COLORSCRIPT · 色彩脚本、四季与灯光时刻表

`docs/art/ART-COLORSCRIPT-色彩脚本、四季与灯光时刻表.md` · 6.5k 字 · 色彩脚本的组织与情绪、四季修正与英雄帧、四季内容的通用取向与阈值生长、灯光与生活痕迹的时刻表设计意图
关键词：色彩脚本、关键帧、英雄帧、四季内容、阈值生长、四季修正、难看时段、灯光时刻表、有效太阳高度、初灯组、生活痕迹

- ART-COLORSCRIPT §0 结论 · L22 · 482
  - ★ ART-COLORSCRIPT §0.1 接入要点 · L31 · 192
- ART-COLORSCRIPT §1 色彩脚本 · L36 · 2.4k
  - ART-COLORSCRIPT §1.1 组织方式 · L38 · 657
  - ART-COLORSCRIPT §1.2 秋季基准关键帧的情绪 · L47 · 624
  - ART-COLORSCRIPT §1.3 四季修正与专属覆盖 · L68 · 374
  - ART-COLORSCRIPT §1.4 难看时段的改造 · L83 · 254
  - ART-COLORSCRIPT §1.5 英雄帧 · L91 · 446
- ART-COLORSCRIPT §2 四季内容 · L108 · 1.2k
- ART-COLORSCRIPT §3 灯光与生活痕迹的时刻表 · L134 · 1.2k

## ART-FIDELITY · 视觉精度标准

`docs/art/ART-FIDELITY-视觉精度标准.md` · 7.5k 字 · 处处不粗糙、细节按区分级：精度底线 B1–B13、五个精度区、一票否决、巡检点、行走走廊加密与外围分级；细节做法在子文档
关键词：视觉精度、精度区、K1–K5、判定距离、底线清单、B1–B13、不粗糙、一票否决、区界规则、可读规则、巡检点、行走走廊、外围分级

- ART-FIDELITY §0 结论 · L21 · 1.2k
  - ★ ART-FIDELITY §0.1 接入要点 · L37 · 528
- ART-FIDELITY §1 精度区与换算基准 · L46 · 2.2k
  - ART-FIDELITY §1.1 五个精度区 · L48 · 773
  - ART-FIDELITY §1.2 区界规则 · L60 · 138
  - ART-FIDELITY §1.3 细节项在各区的取用 · L66 · 442
  - ART-FIDELITY §1.4 划分与数据 · L78 · 386
  - ART-FIDELITY §1.5 换算基准 · L83 · 403
- ART-FIDELITY §2 底线 · L89 · 1.1k
  - ART-FIDELITY §2.0 底线清单（B1–B13） · L93 · 1.1k
- ART-FIDELITY §3 一票否决清单 · L113 · 505
- ART-FIDELITY §4 巡检点 · L123 · 301
- ART-FIDELITY §5 行走走廊 · L128 · 795
  - ART-FIDELITY §5.1 范围与画面构成 · L130 · 186
  - ART-FIDELITY §5.2 走廊里的加密 · L135 · 203
  - ART-FIDELITY §5.3 细节优先级 · L144 · 395
- ART-FIDELITY §6 外围分级精度 · L155 · 74

## ART-FIDELITY-DETAIL · 细节密度与贴脸做法

`docs/art/ART-FIDELITY-DETAIL-细节密度与贴脸做法.md` · 5.6k 字 · 每栋楼的构件与立面密度、窗、地面与热点簇、倒角表与不做旧、夜间四季天气，贴脸要求，穿楼入地，细节词汇
关键词：细节密度、十类构件、立面密度、事件贴花、热点簇、倒角表、不做旧、贴脸距离、L0+ 专用几何、切面、细节词汇、防止细节堆成噪点、最值得做的细节

- ART-FIDELITY-DETAIL §0 结论 · L21 · 589
  - ★ ART-FIDELITY-DETAIL §0.1 接入要点 · L31 · 184
- ART-FIDELITY-DETAIL §1 细节密度（5–150 m） · L38 · 2.0k
  - ART-FIDELITY-DETAIL §1.1 每栋楼的构件层次 · L42 · 288
  - ART-FIDELITY-DETAIL §1.2 立面密度 · L48 · 240
  - ART-FIDELITY-DETAIL §1.3 窗 · L55 · 326
  - ART-FIDELITY-DETAIL §1.4 地面 · L62 · 299
  - ART-FIDELITY-DETAIL §1.5 倒角与不做旧 · L68 · 352
  - ART-FIDELITY-DETAIL §1.6 夜间与四季 · L74 · 219
  - ART-FIDELITY-DETAIL §1.7 天气 · L80 · 149
- ART-FIDELITY-DETAIL §2 贴脸距离（L0+，0.5–5 m） · L86 · 614
- ART-FIDELITY-DETAIL §3 镜头穿进楼体或钻入地下 · L101 · 367
- ART-FIDELITY-DETAIL §4 细节词汇（中国大学校园版） · L110 · 1.0k

## ART-MODELING · 风格化造型规则

`docs/art/ART-MODELING-风格化造型规则.md` · 7.0k 字 · 几何怎么做对：每类对象的最低合格构件、几何细节的四道门槛、收边倒角、深度层、模块修边条、贴面几何、法线与换级；比例颜色与金样在子文档
关键词：最低合格构件、四道门槛、收边构件、倒角做法、自定义法线、硬边法线、解析法线、修边条、贴面几何、换级不跳变、深度层、避开描线临界角

- ART-MODELING §0 结论 · L21 · 879
  - ★ ART-MODELING §0.1 接入要点 · L33 · 358
- ART-MODELING §1 最低合格构件与法线规则 · L42 · 4.9k
  - ART-MODELING §1.1 四道门槛 · L46 · 483
  - ART-MODELING §1.2 收边与倒角 · L55 · 567
  - ART-MODELING §1.3 轮廓、深度层与截面 · L64 · 467
  - ART-MODELING §1.4 模块与修边条 · L74 · 554
  - ART-MODELING §1.5 贴面几何与顶点数据 · L85 · 496
  - ART-MODELING §1.6 法线 · L93 · 517
  - ART-MODELING §1.7 换级不跳变 · L105 · 382
  - ART-MODELING §1.8 每类对象的最低合格构件 · L116 · 1.3k
- ART-MODELING §2 待验证 · L130 · 184

## ART-MODELING-PROPORTION · 风格化比例、颜色与金样

`docs/art/ART-MODELING-PROPORTION-风格化比例、颜色与金样.md` · 4.9k 字 · 先定身份再放开，建筑、植被、道具的风格化比例与形体，固有色约束，金样、参考板与参数化部件库
关键词：风格化造型、风格化比例、身份特征、比例表、重复上限、防摩尔纹、人体尺度锚点、树冠团块、少粗圆干净、固有色、彩度上限、金样、参考板、参数化部件库、IPartDef、共同口号

- ART-MODELING-PROPORTION §0 结论 · L21 · 579
  - ★ ART-MODELING-PROPORTION §0.1 接入要点 · L29 · 285
- ART-MODELING-PROPORTION §1 先定身份，再放开 · L37 · 279
- ART-MODELING-PROPORTION §2 现代混凝土和石材楼的比例规则 · L43 · 887
- ART-MODELING-PROPORTION §3 植被与地表 · L64 · 600
- ART-MODELING-PROPORTION §4 道具与小物件 · L72 · 607
- ART-MODELING-PROPORTION §5 颜色 · L83 · 302
- ART-MODELING-PROPORTION §6 让 20 多个建模者风格一致 · L90 · 734

## ART-SKYLIGHT · 天空、光照与大气

`docs/art/ART-SKYLIGHT-天空、光照与大气.md` · 8.0k 字 · 默认状态的光与太阳、主光与阴影、天光、受光退场、人造光、光效清单与冷暖对照；天空与雾见子文档
关键词：主光、日月交接、无影蓝调、阴影、月影、天光、天光方向、退光线、受光退场、人造光、路灯光斑、窗灯、光效清单、光柱、镜头光斑、冷暖对照

- ART-SKYLIGHT §0 结论 · L21 · 1.1k
  - ★ ART-SKYLIGHT §0.1 接入要点 · L38 · 478
- ART-SKYLIGHT §1 默认状态的光 · L48 · 248
- ART-SKYLIGHT §2 太阳本体与主光色 · L56 · 381
- ART-SKYLIGHT §3 光照骨架 · L63 · 2.7k
  - ART-SKYLIGHT §3.1 主光 · L65 · 1.1k
  - ART-SKYLIGHT §3.2 天光（环境光） · L78 · 212
  - ART-SKYLIGHT §3.3 受光分布 · L82 · 454
  - ART-SKYLIGHT §3.4 受光退场的顺序 · L89 · 170
  - ART-SKYLIGHT §3.5 人造光 · L95 · 777
- ART-SKYLIGHT §4 光效清单 · L114 · 1.4k
- ART-SKYLIGHT §5 冷暖对照 · L133 · 582
- ART-SKYLIGHT §6 待验证 · L157 · 253

## ART-SKYLIGHT-ATMOSPHERE · 天空、月亮与雾

`docs/art/ART-SKYLIGHT-ATMOSPHERE-天空、月亮与雾.md` · 5.2k 字 · 天空渐变、霾带、云、星空、银河、城市天光、日月盘，月亮外观，雾与大气透视的共用雾函数、边缘雾、换季起雾与地标豁免
关键词：天空、sky shader、天空渐变、霾带、云、飞机云、星空、银河、城市天光、夜空明度下限、阴天云盖、雨幕、闪电、彩虹、日月盘、月亮外观、月相、雾、大气透视、共用雾函数、边缘雾、换季起雾、雾海、地标豁免

- ART-SKYLIGHT-ATMOSPHERE §0 结论 · L21 · 744
  - ★ ART-SKYLIGHT-ATMOSPHERE §0.1 接入要点 · L29 · 407
- ART-SKYLIGHT-ATMOSPHERE §1 天空 · L38 · 1.8k
- ART-SKYLIGHT-ATMOSPHERE §2 月亮 · L64 · 244
- ART-SKYLIGHT-ATMOSPHERE §3 雾与大气透视 · L72 · 1.2k

## ART-STYLE · 风格基础

`docs/art/ART-STYLE-风格基础.md` · 6.8k 字 · 三渲二日式动画背景的全局画风：目标与参照、调色板与色名用法、排除元素、场景文字与情绪；明暗影色材质与描线后期在子文档
关键词：三渲二、清透干净、质感参照、色名、退役色名、跳色、色名登记、界面色、日本元素边界、场景文字、原创徽记、情绪弧线

- ART-STYLE §0 结论 · L21 · 636
  - ★ ART-STYLE §0.1 接入要点 · L33 · 222
- ART-STYLE §1 风格目标与参照 · L39 · 669
  - ART-STYLE §1.1 质感参照场景 · L47 · 379
- ART-STYLE §2 调色板 · L54 · 2.2k
  - ART-STYLE §2.1 色名分类与退役色名 · L56 · 345
  - ART-STYLE §2.2 大面积色与跳色 · L63 · 379
  - ART-STYLE §2.3 固有色与推导 · L75 · 188
  - ART-STYLE §2.4 界面色 · L81 · 145
  - ART-STYLE §2.5 色名登记 · L86 · 392
  - ART-STYLE §2.6 天气色 · L94 · 126
  - ART-STYLE §2.7 待登记的新色名 · L99 · 601
- ART-STYLE §3 排除元素 · L119 · 959
  - ART-STYLE §3.1 日本元素的边界 · L121 · 718
  - ART-STYLE §3.2 其他排除项 · L135 · 230
- ART-STYLE §4 场景内的文字与标识 · L141 · 342
- ART-STYLE §5 情绪弧线 · L149 · 449

## ART-STYLE-LINES · 描线与后期

`docs/art/ART-STYLE-LINES-描线与后期.md` · 3.6k 字 · 后处理描线的线类、线色、线宽与折角线，凸边亮线；后期的风格底线与可关项、柔光色调映射光斑移轴与抗锯齿
关键词：描线、线类、线色、线宽、材质 ID、后处理描边、折角线、凸边亮线、风格底线、柔光、色调映射、镜头光斑、移轴景深、窗灯光晕、光柱、抗锯齿、暗角

- ART-STYLE-LINES §0 结论 · L21 · 473
  - ★ ART-STYLE-LINES §0.1 接入要点 · L29 · 156
- ART-STYLE-LINES §1 描线 · L34 · 1.4k
  - ART-STYLE-LINES §1.1 描哪些线 · L38 · 258
  - ART-STYLE-LINES §1.2 线色 · L46 · 141
  - ART-STYLE-LINES §1.3 线宽 · L51 · 240
  - ART-STYLE-LINES §1.4 逐材质调线与轻重 · L55 · 94
  - ART-STYLE-LINES §1.5 后处理描边 · L60 · 208
  - ART-STYLE-LINES §1.6 折角线与低模 · L66 · 184
  - ART-STYLE-LINES §1.7 凸边亮线 · L72 · 204
- ART-STYLE-LINES §2 后期 · L78 · 966
  - ART-STYLE-LINES §2.1 柔光：白天只晕高光 · L84 · 119
  - ART-STYLE-LINES §2.2 色调映射 · L88 · 111
  - ART-STYLE-LINES §2.3 镜头光斑 · L93 · 101
  - ART-STYLE-LINES §2.4 移轴景深 · L97 · 48
  - ART-STYLE-LINES §2.5 窗灯光晕 · L101 · 71
  - ART-STYLE-LINES §2.6 特效形态与光柱 · L105 · 188
  - ART-STYLE-LINES §2.7 抗锯齿 · L110 · 81
  - ART-STYLE-LINES §2.8 暗角与分调 · L114 · 72

## ART-STYLE-SHADING · 明暗、影色与材质表现

`docs/art/ART-STYLE-SHADING-明暗、影色与材质表现.md` · 6.2k 字 · 卡通着色的明暗阶数、法线、倒角提亮，影色的推导，玻璃金属边缘光透光湿面，贴图与笔触，材质速查
关键词：卡通材质、toon.gdshaderinc、明暗分阶、接触影、影色推导、室内暖影、倒角提亮、法线展平、边缘光、透光、湿面、斑驳、构造图案线、不用法线贴图、材质速查

- ART-STYLE-SHADING §0 结论 · L21 · 600
  - ★ ART-STYLE-SHADING §0.1 接入要点 · L30 · 254
- ART-STYLE-SHADING §1 明暗分阶 · L37 · 1.5k
  - ART-STYLE-SHADING §1.1 阶数 · L39 · 518
  - ART-STYLE-SHADING §1.2 法线展平 · L53 · 138
  - ART-STYLE-SHADING §1.3 树冠法线 · L60 · 70
  - ART-STYLE-SHADING §1.4 平涂与渐变 · L64 · 126
  - ART-STYLE-SHADING §1.5 统一的卡通着色 · L70 · 291
  - ART-STYLE-SHADING §1.6 倒角提亮与只改明暗的修正 · L77 · 301
- ART-STYLE-SHADING §2 影子：明度为主，按材质定影色 · L87 · 1.3k
  - ART-STYLE-SHADING §2.1 原则 · L89 · 267
  - ART-STYLE-SHADING §2.2 推导规则 · L97 · 404
  - ART-STYLE-SHADING §2.3 天光与地面反光 · L106 · 78
  - ART-STYLE-SHADING §2.4 室内暖影 · L110 · 236
  - ART-STYLE-SHADING §2.5 哪些材质有色调变化 · L115 · 134
  - ART-STYLE-SHADING §2.6 夜间与阴雨 · L119 · 115
- ART-STYLE-SHADING §3 高光、边缘光与透光 · L124 · 767
  - ART-STYLE-SHADING §3.1 玻璃 · L126 · 144
  - ART-STYLE-SHADING §3.2 金属亮带 · L131 · 70
  - ART-STYLE-SHADING §3.3 边缘光 · L135 · 179
  - ART-STYLE-SHADING §3.4 湿面与积水 · L141 · 192
  - ART-STYLE-SHADING §3.5 透光 · L147 · 166
- ART-STYLE-SHADING §4 贴图与笔触 · L152 · 499
  - ART-STYLE-SHADING §4.1 禁止照片纹理 · L154 · 42
  - ART-STYLE-SHADING §4.2 斑驳、笔触与构造图案线 · L158 · 338
  - ART-STYLE-SHADING §4.3 不用法线贴图 · L165 · 107
- ART-STYLE-SHADING §5 材质速查 · L169 · 159

## WLD-INDEX · 世界总纲

`docs/world/WLD-INDEX-世界总纲.md` · 1.5k 字 · 不分图的世界内容规则的入口：地形、水体、植被、室内、外围、动物与街道家具、日式街景、老楼翻新
关键词：世界、世界总纲、文档树、WLD

- WLD-INDEX §0 结论 · L21 · 562
  - ★ WLD-INDEX §0.1 接入要点 · L30 · 165
- WLD-INDEX §1 文档树 · L35 · 432

## WLD-AMBIENCE · 日式日常街景

`docs/world/WLD-AMBIENCE-日式日常街景.md` · 9.3k 字 · 日式日常街景的边界与分区规则，各元素的形、色、限定与三渲二做法；声音清单在 SYS-AUDIO-AMBIENCE
关键词：日式街景、日式氛围、日系、日本味、电线杆、架空线、自动售货机、售货机光岛、弯道凸面镜、驻轮场式车棚、屋顶围网、高架水箱、通长雨庇、推拉铝窗、象牙色窗帘、开放外廊、次路标线、让行牌、L 形边沟、砌块墙、樱花行道树、绣球、夏季绿帘、杆上防犯灯、便利店夜景、暖帘、防球网、外墙圆钟、电车道口

- WLD-AMBIENCE §0 结论 · L21 · 757
  - ★ WLD-AMBIENCE §0.1 接入要点 · L28 · 358
- WLD-AMBIENCE §1 分区与通用约束 · L36 · 2.3k
  - WLD-AMBIENCE §1.1 用与不用 · L38 · 170
  - WLD-AMBIENCE §1.2 区域代号 · L43 · 453
  - WLD-AMBIENCE §1.3 方式与通用约束 · L58 · 674
  - WLD-AMBIENCE §1.4 颜色 · L72 · 260
  - WLD-AMBIENCE §1.5 禁做清单 · L83 · 700
- WLD-AMBIENCE §2 校舍外立面与屋顶 · L102 · 958
- WLD-AMBIENCE §3 外廊、楼梯与出入口 · L113 · 555
- WLD-AMBIENCE §4 车棚、售货机与街角小设施 · L122 · 514
- WLD-AMBIENCE §5 电线杆、架空线与线上的鸟 · L130 · 361
- WLD-AMBIENCE §6 道路标线、凸面镜、护栏、路缘与排水 · L136 · 764
- WLD-AMBIENCE §7 绿化 · L150 · 371
- WLD-AMBIENCE §8 照明与夜景 · L158 · 477
- WLD-AMBIENCE §9 河堤、球场设施、店头、外围街道与飞机云 · L167 · 656
- WLD-AMBIENCE §10 声音 · L181 · 131
- WLD-AMBIENCE §11 待验证 · L185 · 317

## WLD-INTERIORS · 室内

`docs/world/WLD-INTERIORS-室内.md` · 1.7k 字 · 不分图的室内规则目录：开放（原则、门、关着的楼、新开一栋）与制作（加载、生成器、风格、痕迹、碰撞）
关键词：室内、精选开放

- WLD-INTERIORS §0 结论 · L22 · 261
  - ★ WLD-INTERIORS §0.1 接入要点 · L31 · 39

## WLD-INTERIORS-ACCESS · 室内开放

`docs/world/WLD-INTERIORS-ACCESS-室内开放.md` · 5.7k 字 · 精选开放的原则与共用约定、门的开与关（含自动门的设计取向）、关着的楼、开放更多楼的做法
关键词：楼内、开放室内、开放清单、interiors.json、开放入口、自动门、封口、门厅卡、窗卡、看进去例外

- WLD-INTERIORS-ACCESS §0 结论 · L22 · 966
  - ★ WLD-INTERIORS-ACCESS §0.1 接入要点 · L31 · 547
- WLD-INTERIORS-ACCESS §1 开放原则与共用约定 · L40 · 1.1k
- WLD-INTERIORS-ACCESS §2 门的区分：开与关 · L54 · 966
  - WLD-INTERIORS-ACCESS §2.1 楼里的门：走近推开，闭门器关上 · L69 · 319
- WLD-INTERIORS-ACCESS §3 关着的楼：外壳、窗卡、门厅卡与简化室内 · L76 · 732
- WLD-INTERIORS-ACCESS §4 开放更多室内 · L85 · 604

## WLD-INTERIORS-BUILD · 室内制作

`docs/world/WLD-INTERIORS-BUILD-室内制作.md` · 6.8k 字 · 室内制作规则：加载与进楼衔接、平面生成器、风格与光、生活痕迹、楼梯门洞与碰撞
关键词：室内加载、进楼衔接、平面内核、楼型、室内风格、窗光斑、光柱、室内色名、生活痕迹、楼梯碰撞、SlopedWallCollider、门洞、落点

- WLD-INTERIORS-BUILD §0 结论 · L21 · 451
  - ★ WLD-INTERIORS-BUILD §0.1 接入要点 · L28 · 240
- WLD-INTERIORS-BUILD §1 加载与进楼衔接 · L35 · 826
  - WLD-INTERIORS-BUILD §1.1 整体加载、按层显示 · L37 · 277
  - WLD-INTERIORS-BUILD §1.2 触发 · L43 · 269
  - WLD-INTERIORS-BUILD §1.3 进楼与出楼，以及衔接保证（ENG-PERF-STREAM §1.2 第 6 条） · L50 · 266
- WLD-INTERIORS-BUILD §2 平面生成器与类型库 · L56 · 1.4k
- WLD-INTERIORS-BUILD §3 风格 · L67 · 1.3k
  - WLD-INTERIORS-BUILD §3.1 明暗与暖影 · L71 · 195
  - WLD-INTERIORS-BUILD §3.2 窗光斑与光（最重要的室内画面） · L75 · 395
  - WLD-INTERIORS-BUILD §3.3 色名、识别要素与夜间 · L83 · 599
- WLD-INTERIORS-BUILD §4 生活痕迹规则 · L89 · 614
- WLD-INTERIORS-BUILD §5 楼梯、门洞与碰撞 · L98 · 906
- WLD-INTERIORS-BUILD §6 待验证 · L116 · 252

## WLD-LIFE · 动物、生活痕迹与街道家具

`docs/world/WLD-LIFE-动物、生活痕迹与街道家具.md` · 2.5k 字 · 动物、生活痕迹、停着的车与街道家具的共同底线与显隐做法，动物痕迹与车家具分子文档
关键词：动物与生活痕迹总则、艺术化处理、世界里不画人、显隐做法、变体掩码、锁存、日夜两套、季节专属物件

- WLD-LIFE §0 结论 · L22 · 677
  - ★ WLD-LIFE §0.1 接入要点 · L35 · 137
- WLD-LIFE §1 显隐：随时段、季节、天气的做法 · L40 · 755

## WLD-LIFE-FAUNA · 动物与生活痕迹

`docs/world/WLD-LIFE-FAUNA-动物与生活痕迹.md` · 5.4k 字 · 动物与反应动物、室外生活痕迹的规则；显隐做法见 WLD-LIFE
关键词：动物、鸟、校园猫、鸽群、反应动物、黄昏乌鸦、季节性动物、生活痕迹、晾晒、黑天鹅、雨天生活痕迹、小心地滑

- WLD-LIFE-FAUNA §0 结论 · L22 · 610
  - ★ WLD-LIFE-FAUNA §0.1 接入要点 · L29 · 286
- WLD-LIFE-FAUNA §1 动物 · L36 · 2.2k
  - WLD-LIFE-FAUNA §1.1 反应动物：猫与鸽群 · L63 · 846
- WLD-LIFE-FAUNA §2 生活痕迹 · L80 · 1.4k
- WLD-LIFE-FAUNA §3 待验证（P0） · L92 · 216

## WLD-LIFE-STREET · 停着的车与街道家具

`docs/world/WLD-LIFE-STREET-停着的车与街道家具.md` · 4.6k 字 · 停着的车与匿名共享单车、街道家具的设计，行走碰撞与 Foot 材质、声音数据
关键词：停着的车、校车、街道家具、分类垃圾箱、路灯灯具、售货机、外卖柜、长椅、声音插座、家具碰撞、共享单车停放点

- WLD-LIFE-STREET §0 结论 · L22 · 645
  - ★ WLD-LIFE-STREET §0.1 接入要点 · L27 · 504
- WLD-LIFE-STREET §1 停着的车 · L36 · 782
- WLD-LIFE-STREET §2 街道家具 · L46 · 1.4k
- WLD-LIFE-STREET §3 行走模式下的碰撞 · L58 · 690
- WLD-LIFE-STREET §4 待验证（P0） · L75 · 56

## WLD-OUTSKIRTS · 边界与外围

`docs/world/WLD-OUTSKIRTS-边界与外围.md` · 2.3k 字 · 大箱庭的三条线（校界、前沿线、截面线）；外围环境带、箱庭截面与远景两份子文档的入口，校界的阻挡与提示在系统域 SYS-BORDER
关键词：外围、校外、校界、边界、前沿线、截面线、大箱庭

- WLD-OUTSKIRTS §0 结论 · L22 · 744
  - ★ WLD-OUTSKIRTS §0.1 接入要点 · L35 · 223
- WLD-OUTSKIRTS §1 三条线 · L40 · 426

## WLD-OUTSKIRTS-BAND · 外围环境带

`docs/world/WLD-OUTSKIRTS-BAND-外围环境带.md` · 5.2k 字 · 外围环境带规则：截面线位置、O0–O3 的内容与精度、灯光季节车流、保留删除新增、画法、数据与许可
关键词：外围环境带、O0–O3、距离环、外围覆盖层、offcampus.json、外围画法、楼型、程序化布局、ODbL

- WLD-OUTSKIRTS-BAND §0 结论 · L21 · 782
  - ★ WLD-OUTSKIRTS-BAND §0.1 接入要点 · L28 · 384
- WLD-OUTSKIRTS-BAND §1 截面线与截面精度 · L35 · 255
  - WLD-OUTSKIRTS-BAND §1.1 截面线的位置规则 · L37 · 181
  - WLD-OUTSKIRTS-BAND §1.2 截面精度 · L42 · 59
- WLD-OUTSKIRTS-BAND §2 外围分层 O0–O3 · L46 · 1.3k
  - WLD-OUTSKIRTS-BAND §2.1 判定原则：距离环 · L48 · 257
  - WLD-OUTSKIRTS-BAND §2.2 分层表 · L52 · 630
  - WLD-OUTSKIRTS-BAND §2.3 楼型与程序化布局 · L63 · 389
- WLD-OUTSKIRTS-BAND §3 灯光、季节与车流 · L70 · 748
  - WLD-OUTSKIRTS-BAND §3.1 灯光（夜间） · L72 · 214
  - WLD-OUTSKIRTS-BAND §3.2 时间、季节、天气 · L79 · 382
  - WLD-OUTSKIRTS-BAND §3.3 不画车流 · L88 · 137
- WLD-OUTSKIRTS-BAND §4 保留、删除与新增 · L93 · 499
  - WLD-OUTSKIRTS-BAND §4.1 删掉的 · L99 · 178
  - WLD-OUTSKIRTS-BAND §4.2 新增的规则 · L103 · 147
- WLD-OUTSKIRTS-BAND §5 外围的画法：清透、细线 · L109 · 78
- WLD-OUTSKIRTS-BAND §6 数据与许可 · L113 · 478

## WLD-OUTSKIRTS-DIORAMA · 箱庭截面与远景

`docs/world/WLD-OUTSKIRTS-DIORAMA-箱庭截面与远景.md` · 8.1k 字 · 箱庭外缘的地层截面、河渠水柱、雾海与银带、360° 远景和雪山，及其随时段、季节、天气的变化
关键词：箱庭截面、地层截面、剖面、截面上缘、地下水线、河渠水柱截面、雾海、雾海顶面、雾天雾海上升、河流银带、深夜雾海微光、远景、远景视差、色调层、坡向层、发光层、雪山、日照金山、雪线、雪山可见度、backdrop.json、diorama_section、diorama_far、Camera3D.Far

- WLD-OUTSKIRTS-DIORAMA §0 结论 · L21 · 1.7k
  - ★ WLD-OUTSKIRTS-DIORAMA §0.1 接入要点 · L34 · 734
- WLD-OUTSKIRTS-DIORAMA §1 截面线与校界 · L46 · 60
- WLD-OUTSKIRTS-DIORAMA §2 地层截面 · L50 · 1.3k
  - WLD-OUTSKIRTS-DIORAMA §2.1 截面的受光与阴影 · L63 · 309
- WLD-OUTSKIRTS-DIORAMA §3 河渠水柱截面 · L71 · 493
- WLD-OUTSKIRTS-DIORAMA §4 截面下缘与雾海 · L77 · 884
- WLD-OUTSKIRTS-DIORAMA §5 远景（360° 不留空白） · L87 · 1.3k
- WLD-OUTSKIRTS-DIORAMA §6 雪山 · L108 · 811
- WLD-OUTSKIRTS-DIORAMA §7 待验证（P0） · L117 · 297

## WLD-RENOVATION · 老楼翻新

`docs/world/WLD-RENOVATION-老楼翻新.md` · 9.0k 字 · 老楼位置体量不动、外观全换新楼标准：构件、色额、删简留、改法、验收，含普通楼宇设计规则
关键词：老楼翻新、翻新、翻新预设、翻新楼、全部翻新、旧痕、删简留、防盗网、窗套、窗楣、窗台滴水、勒脚、压顶檐口、管槽、管廊、护窗栏、伸出杆、颜色配额、斑驳、面砖、楼梯间窗带、叶影上墙、成电辨认、翻新对照楼

- WLD-RENOVATION §0 结论 · L21 · 1.0k
  - ★ WLD-RENOVATION §0.1 接入要点 · L32 · 475
- WLD-RENOVATION §1 适用范围 · L42 · 740
  - WLD-RENOVATION §1.1 哪些楼列入翻新 · L44 · 323
  - WLD-RENOVATION §1.2 不动什么 · L50 · 174
  - WLD-RENOVATION §1.3 能加多少 · L56 · 232
- WLD-RENOVATION §2 总则 · L61 · 843
  - WLD-RENOVATION §2.1 全部翻新，与新楼同标准 · L63 · 217
  - WLD-RENOVATION §2.2 会脏的地方换成构件 · L69 · 347
  - WLD-RENOVATION §2.3 生活感只靠干净的痕迹 · L82 · 270
- WLD-RENOVATION §3 材质与配色 · L88 · 1.2k
  - WLD-RENOVATION §3.1 材质族 · L90 · 303
  - WLD-RENOVATION §3.2 每栋楼的颜色配额 · L101 · 128
  - WLD-RENOVATION §3.3 取值 · L106 · 359
  - WLD-RENOVATION §3.4 斑驳、笔触与顶点色 · L113 · 169
  - WLD-RENOVATION §3.5 面砖 · L119 · 113
  - WLD-RENOVATION §3.6 描线 · L123 · 130
- WLD-RENOVATION §4 删、简、留 · L127 · 843
  - WLD-RENOVATION §4.1 删：旧痕、杂物与防盗网 · L129 · 359
  - WLD-RENOVATION §4.2 简：外机、管线、窗分格与标识 · L137 · 271
  - WLD-RENOVATION §4.3 留：干净版的生活痕迹 · L144 · 201
- WLD-RENOVATION §5 窗、檐口、楼梯间、屋顶与首层 · L148 · 1.3k
  - WLD-RENOVATION §5.1 窗 · L150 · 414
  - WLD-RENOVATION §5.2 檐口与女儿墙 · L159 · 181
  - WLD-RENOVATION §5.3 楼梯间 · L166 · 277
  - WLD-RENOVATION §5.4 屋顶 · L174 · 262
  - WLD-RENOVATION §5.5 首层、入口与阳台 · L181 · 165
- WLD-RENOVATION §6 氛围与成电辨认 · L188 · 698
  - WLD-RENOVATION §6.1 画法上的氛围手法 · L190 · 289
  - WLD-RENOVATION §6.2 日式物件上楼 · L197 · 224
  - WLD-RENOVATION §6.3 保住"认得出是成电" · L203 · 171
- WLD-RENOVATION §7 验收 · L209 · 160
- WLD-RENOVATION §8 普通楼宇的设计规则 · L215 · 1.1k

## WLD-TERRAIN · 地形

`docs/world/WLD-TERRAIN-地形.md` · 1.6k 字 · 不分图的地形规则目录：设计（原则、分层、竖直落差、水位岸型）与落地（合成、贴地、可走性、着色）
关键词：地形、地形规则

- WLD-TERRAIN §0 结论 · L22 · 381
  - ★ WLD-TERRAIN §0.1 接入要点 · L32 · 39

## WLD-TERRAIN-APPLY · 地形落地

`docs/world/WLD-TERRAIN-APPLY-地形落地.md` · 6.8k 字 · 地形落到画面和碰撞的规则：矢量操作与合成、贴地、楼宇垫层、可走性、着色
关键词：地形操作、地形覆盖层、terrain.json、合成顺序、先平后贴、贴地、垫层、勒脚、散水、PadY、Steep、碰撞带、分面平法线、地面着色

- WLD-TERRAIN-APPLY §0 结论 · L22 · 686
  - ★ WLD-TERRAIN-APPLY §0.1 接入要点 · L29 · 400
- WLD-TERRAIN-APPLY §1 地形的表达与合成 · L38 · 1.4k
- WLD-TERRAIN-APPLY §2 贴地 · L53 · 1.4k
  - WLD-TERRAIN-APPLY §2.1 先平后贴 · L55 · 322
  - WLD-TERRAIN-APPLY §2.2 道路与铺装 · L68 · 269
  - WLD-TERRAIN-APPLY §2.3 楼宇：垫层、勒脚与入口 · L75 · 560
  - WLD-TERRAIN-APPLY §2.4 树与道具落地 · L84 · 257
- WLD-TERRAIN-APPLY §3 可走性与碰撞 · L91 · 689
- WLD-TERRAIN-APPLY §4 地面着色与描线 · L100 · 906
- WLD-TERRAIN-APPLY §5 待验证 · L120 · 336

## WLD-TERRAIN-SHAPE · 地形设计

`docs/world/WLD-TERRAIN-SHAPE-地形设计.md` · 7.1k 字 · 地形设计规则：原则、高度预算与分层、手感刻度、坡度类、竖直落差、水位与岸型
关键词：地形设计原则、地形分层、高度预算、主模数、手感刻度、灰区、坡度类、零带、竖直落差、台缘带、退台、常水位、岸型、涉水带、泳岸型、遮挡与揭示

- WLD-TERRAIN-SHAPE §0 结论 · L22 · 558
  - ★ WLD-TERRAIN-SHAPE §0.1 接入要点 · L29 · 217
- WLD-TERRAIN-SHAPE §1 设计原则 · L35 · 931
  - WLD-TERRAIN-SHAPE §1.1 总骨架 · L37 · 189
  - WLD-TERRAIN-SHAPE §1.2 规则 · L43 · 731
- WLD-TERRAIN-SHAPE §2 高度预算与分层 · L57 · 1.8k
  - WLD-TERRAIN-SHAPE §2.1 分层 · L59 · 493
  - WLD-TERRAIN-SHAPE §2.2 手感刻度 · L72 · 499
  - WLD-TERRAIN-SHAPE §2.3 坡度类 · L87 · 494
  - WLD-TERRAIN-SHAPE §2.4 全图范围与零带 · L100 · 258
- WLD-TERRAIN-SHAPE §3 竖直落差 · L107 · 1.3k
  - WLD-TERRAIN-SHAPE §3.1 连接规则 · L109 · 509
  - WLD-TERRAIN-SHAPE §3.2 台缘带 · L125 · 290
  - WLD-TERRAIN-SHAPE §3.3 退台与高墙 · L132 · 180
  - WLD-TERRAIN-SHAPE §3.4 防护与谁来做 · L137 · 289
- WLD-TERRAIN-SHAPE §4 水位与岸型 · L142 · 1.6k
  - WLD-TERRAIN-SHAPE §4.1 常水位 · L144 · 263
  - WLD-TERRAIN-SHAPE §4.2 岸型 · L150 · 597
  - WLD-TERRAIN-SHAPE §4.3 涉水带、湖盆与河渠 · L161 · 341
  - WLD-TERRAIN-SHAPE §4.4 穿出校界 · L167 · 108
  - WLD-TERRAIN-SHAPE §4.5 开放泳区的泳岸型与支撑面 · L171 · 247

## WLD-VEGETATION · 植被

`docs/world/WLD-VEGETATION-植被.md` · 10.9k 字 · 植被规则：树冠画法、草坪、地面覆盖、水生植物、季相与天气、碰撞与声音数据
关键词：植被、植物、树冠画法、云团型、插片型、冬季裸枝、叶量、开花、季相、局部季节、四季变色、草坪、绿篱、荷、芦苇、落叶地毯、树冠遮蔽、覆雪、过渡绿地、species.json

- WLD-VEGETATION §0 结论 · L22 · 1.4k
  - ★ WLD-VEGETATION §0.1 接入要点 · L34 · 756
- WLD-VEGETATION §1 卡通树冠的画法 · L48 · 1.5k
  - WLD-VEGETATION §1.1 团块与三类树冠 · L52 · 312
  - WLD-VEGETATION §1.2 明暗与法线 · L61 · 160
  - WLD-VEGETATION §1.3 逆光 · L67 · 279
  - WLD-VEGETATION §1.4 描线、远景与树干 · L75 · 217
  - WLD-VEGETATION §1.5 冬季裸枝 · L81 · 515
- WLD-VEGETATION §2 树种与四季 · L90 · 858
  - WLD-VEGETATION §2.1 色名与季相行 · L98 · 471
- WLD-VEGETATION §3 草坪 · L104 · 1.5k
  - WLD-VEGETATION §3.1 颜色与长影 · L106 · 218
  - WLD-VEGETATION §3.2 开阔草坪 · L111 · 372
  - WLD-VEGETATION §3.3 积雪与季节花境 · L119 · 304
  - WLD-VEGETATION §3.4 过渡绿地的草本与草面（DECISION-185） · L124 · 238
  - WLD-VEGETATION §3.5 草叶的表示与交界（DECISION-205，取值是初值，样板同位姿定） · L131 · 357
- WLD-VEGETATION §4 落叶、花瓣与积雪（地面覆盖） · L137 · 841
- WLD-VEGETATION §5 水生植物 · L147 · 456
- WLD-VEGETATION §6 季相与天气 · L162 · 1.2k
  - WLD-VEGETATION §6.1 逐株错相与叶量 · L164 · 207
  - WLD-VEGETATION §6.2 季节颜色 · L169 · 194
  - WLD-VEGETATION §6.3 摆动与飘落（给 SYS-MOTION 的参数） · L174 · 160
  - WLD-VEGETATION §6.4 覆雪 · L179 · 375
  - WLD-VEGETATION §6.5 换季与天气切换 · L185 · 228
- WLD-VEGETATION §7 碰撞、画面设置与声音 · L191 · 1.1k
  - WLD-VEGETATION §7.1 行走碰撞 · L193 · 396
  - WLD-VEGETATION §7.2 画面设置（SYS-GRAPHICS） · L202 · 388
  - WLD-VEGETATION §7.3 声音的接口（SYS-AUDIO） · L209 · 292

## WLD-WATER · 水体

`docs/world/WLD-WATER-水体.md` · 6.7k 字 · 水体规则：动画式水面画法、反光带与倒影、四季天气、涉水与开放泳区、水体系统
关键词：水体、水面、开放泳区、波纹线、主光反光带、碎金带、月光银带、四芒星闪点、泡沫线、倒影、平面反射、伪深度色带、雨点环、灯的竖条倒影、refl_lamps

- WLD-WATER §0 结论 · L22 · 1.2k
  - ★ WLD-WATER §0.1 接入要点 · L33 · 578
- WLD-WATER §1 水面的通用画法 · L43 · 1.0k
  - WLD-WATER §1.1 底色与色带 · L47 · 332
  - WLD-WATER §1.2 波纹线与泡沫线 · L53 · 270
  - WLD-WATER §1.3 硬质驳岸的接水 · L59 · 316
- WLD-WATER §2 反光带、灯条与倒影 · L63 · 1.2k
  - WLD-WATER §2.1 主光反光带 · L65 · 420
  - WLD-WATER §2.2 灯的竖条倒影 · L73 · 355
  - WLD-WATER §2.3 倒影 · L77 · 425
- WLD-WATER §3 四季与天气的水面 · L83 · 808
- WLD-WATER §4 涉水与开放泳区 · L110 · 321
- WLD-WATER §5 水体系统的规则 · L117 · 553
- WLD-WATER §6 待验证 · L126 · 100

## MAP-QSH-INDEX · 清水河总纲

`docs/maps/qingshuihe/MAP-QSH-INDEX-清水河总纲.md` · 2.3k 字 · 清水河校区总纲：本域通用原则与文档树
关键词：清水河、清水河总纲、文档树、MAP-QSH、默认状态、区文档三件套、主线之后、跨区主题、世界文件、数据位置

- MAP-QSH-INDEX §0 结论 · L21 · 802
  - ★ MAP-QSH-INDEX §0.1 接入要点 · L30 · 176
- MAP-QSH-INDEX §1 文档树 · L35 · 951

## MAP-QSH-ADDITIONS · 设计增补

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-设计增补.md` · 6.4k 字 · 设计增补的登记与用色、默认状态与作用代号、空地整理、参照转译、行走分类，以及区文档共同口径
关键词：设计增补、增补物、增补登记、作用代号、默认状态规则、大片空地、功能开敞面、过渡绿地、参照与转译、刻字景观石、可跳上、增补物行走分类、区文档共同口径、身份特征、近景要求

- MAP-QSH-ADDITIONS §0 结论 · L22 · 1.0k
  - ★ MAP-QSH-ADDITIONS §0.1 接入要点 · L38 · 370
- MAP-QSH-ADDITIONS §1 通用规则 · L45 · 408
- MAP-QSH-ADDITIONS §2 默认状态规则与作用代号 · L52 · 840
- MAP-QSH-ADDITIONS §3 大片空地的整理 · L76 · 576
- MAP-QSH-ADDITIONS §4 参照与转译规则 · L85 · 445
- MAP-QSH-ADDITIONS §5 禁做清单的本图补充 · L92 · 127
- MAP-QSH-ADDITIONS §6 需要登记的新颜色 · L99 · 57
- MAP-QSH-ADDITIONS §7 增补物的行走分类 · L103 · 399
- MAP-QSH-ADDITIONS §8 区文档共同口径 · L113 · 882
  - MAP-QSH-ADDITIONS §8.1 身份特征与近景要求 · L117 · 393
  - MAP-QSH-ADDITIONS §8.2 四季与昼夜 · L123 · 346

## MAP-QSH-ADDITIONS-VOCAB · 增补词汇与全校摆放

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-VOCAB-增补词汇与全校摆放.md` · 8.6k 字 · 按场地分类的增补物词汇、后勤小楼清单，以及动物与街道家具的全校数量与位置
关键词：增补词汇、休憩平台、电动车充电棚、外卖挂架、徽记井盖、亲水台阶、节制闸、草阶看台、紫藤廊架、桥头卧石、读书石台、汀步、下凹雨水花园、风雨连廊、屋顶实验花园、透空栏杆、导览亭、导览桩、直饮水台、休息点、后勤小楼、动物与街道家具摆放

- MAP-QSH-ADDITIONS-VOCAB §0 结论 · L22 · 548
  - ★ MAP-QSH-ADDITIONS-VOCAB §0.1 接入要点 · L31 · 145
- MAP-QSH-ADDITIONS-VOCAB §1 宿舍组团、食堂与商业街前 · L35 · 862
- MAP-QSH-ADDITIONS-VOCAB §2 道路、路口与停车场 · L53 · 783
- MAP-QSH-ADDITIONS-VOCAB §3 河渠、湖岸与桥 · L68 · 899
- MAP-QSH-ADDITIONS-VOCAB §4 草坪、楼间空地与屋顶 · L85 · 760
- MAP-QSH-ADDITIONS-VOCAB §5 运动区、校界与步行服务设施 · L101 · 1.5k
- MAP-QSH-ADDITIONS-VOCAB §6 后勤小楼清单 · L123 · 226
- MAP-QSH-ADDITIONS-VOCAB §7 动物与街道家具的全校摆放 · L127 · 686
- MAP-QSH-ADDITIONS-VOCAB §8 日式街景物件的分区与地段 · L151 · 732
  - MAP-QSH-ADDITIONS-VOCAB §8.1 分区 · L155 · 342
  - MAP-QSH-ADDITIONS-VOCAB §8.2 物件的地段 · L170 · 326

## MAP-QSH-ATMOSPHERE · 夜间灯光与雨天

`docs/maps/qingshuihe/MAP-QSH-ATMOSPHERE-夜间灯光与雨天.md` · 4.8k 字 · 夜间灯光的分级与颜色、初灯组、雨夜雾夜，以及雨帘避雨处与积水点的放置约定
关键词：夜间灯光总图、照度三级、光斑层次、冷白两类、初灯组、逐盏调度、lighting-schedule.json、雨夜、雾夜、竖条倒影、灯锥、雨帘、避雨处、滴水点、干湿一刀切、积水点、积水放置、英雄帧

- MAP-QSH-ATMOSPHERE §0 结论 · L22 · 384
  - ★ MAP-QSH-ATMOSPHERE §0.1 接入要点 · L29 · 146
- MAP-QSH-ATMOSPHERE §1 夜间灯光总图：级别与颜色（主线之后） · L34 · 943
- MAP-QSH-ATMOSPHERE §2 初灯组与逐盏调度（主线之后） · L54 · 333
- MAP-QSH-ATMOSPHERE §3 雨夜与雾夜（主线之后） · L60 · 466
- MAP-QSH-ATMOSPHERE §4 雨帘与避雨处（主线之后） · L69 · 767
- MAP-QSH-ATMOSPHERE §5 积水点（主线之后） · L85 · 554
- MAP-QSH-ATMOSPHERE §6 待验证（主线之后） · L94 · 134

## MAP-QSH-CENTER-EAST · 中部与东区

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-中部与东区.md` · 8.2k 字 · 中部科研区与东区各建筑、雕像、球场的外观与身份特征、本区设计增补、近景要求；四季昼夜、行走与声音在子文档
关键词：中部科研区、东区、基础实验大楼、一号科研实验楼、科研楼B区、科研楼C区、麦克斯韦像、方程石、林为干院士像、科研楼碑、银杏大道跨河小桥、品学楼、第一教学楼、书法横梁门框、架空通道、叠水喷泉石台、文科楼群、友谊树、综合训练馆、筒拱、网球场树墙、绿色房间、球场标线、球场围网、高杆灯、田径场、器材室、室外游泳池组团、东区月出、world/center-east.json

- MAP-QSH-CENTER-EAST §0 结论 · L22 · 1.0k
  - ★ MAP-QSH-CENTER-EAST §0.1 接入要点 · L36 · 241
- MAP-QSH-CENTER-EAST §1 中部科研区 · L41 · 899
- MAP-QSH-CENTER-EAST §2 品学楼 · L53 · 1.0k
- MAP-QSH-CENTER-EAST §3 文科楼群 · L63 · 544
- MAP-QSH-CENTER-EAST §4 综合训练馆与球场 · L70 · 1.4k
- MAP-QSH-CENTER-EAST §5 东缘 · L79 · 712
- MAP-QSH-CENTER-EAST §6 本区身份特征、设计增补与近景要求 · L86 · 782
- MAP-QSH-CENTER-EAST §7 待验证 · L107 · 245

## MAP-QSH-CENTER-EAST-SEASONS · 中部与东区四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-SEASONS-中部与东区四季昼夜与天气.md` · 4.2k 字 · 中部与东区的月出与光、夜间角色与四季表、球场积雪、避雨处与雨帘、积水点、雷雨与雾里读形
关键词：东区月出、傍晚满月、清晨金边、球场高杆灯、筒拱采光带、夜间角色、东区四季、球场积雪、树墙四季、避雨处、架空通道雨帘、积水点、雷雨、雾里读形、雨天的生活痕迹

- MAP-QSH-CENTER-EAST-SEASONS §0 结论 · L22 · 411
  - ★ MAP-QSH-CENTER-EAST-SEASONS §0.1 接入要点 · L28 · 222
- MAP-QSH-CENTER-EAST-SEASONS §1 本区的光（主线之后） · L33 · 334
- MAP-QSH-CENTER-EAST-SEASONS §2 季节与昼夜清单 · L42 · 1.8k
- MAP-QSH-CENTER-EAST-SEASONS §3 天气要点（主线之后） · L82 · 556

## MAP-QSH-CENTER-EAST-WALK · 中部与东区行走与声音

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-WALK-中部与东区行走与声音.md` · 4.7k 字 · 中部与东区的行走主网、能看不能进、开放入口的门口表现、点位与锚点、声音要点
关键词：东区行走、行走主网、能看不能进、球场围网门、游泳池组团围网、东缘校界、开放入口、门口表现、阶梯教室、涉水区、P14、P23、R5、R14、A04、A13、地标可见、声音要点、叠水喷泉声、Sounds

- MAP-QSH-CENTER-EAST-WALK §0 结论 · L22 · 844
  - ★ MAP-QSH-CENTER-EAST-WALK §0.1 接入要点 · L29 · 593
- MAP-QSH-CENTER-EAST-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L36 · 805
- MAP-QSH-CENTER-EAST-WALK §2 开放入口与门口表现（主线之后） · L57 · 469
- MAP-QSH-CENTER-EAST-WALK §3 行走：点位、节奏与锚点（主线之后） · L65 · 1.0k
- MAP-QSH-CENTER-EAST-WALK §4 声音要点（主线之后） · L82 · 435

## MAP-QSH-CORE · 核心区

`docs/maps/qingshuihe/MAP-QSH-CORE-核心区.md` · 8.7k 字 · 图书馆、时间广场、日晷、博物馆与西湖的外观、身份特征、设计增补与近景要求
关键词：核心区、图书馆外观、八角采光顶、穹顶、方塔玻璃槽、竖缝窗、大台阶、二楼正门、品·悦吧店面、宁静苑、无主体块、时间广场、旗杆、十二时辰刻度、日晷、晷针、博物馆外观、柱廊环、西湖、折线栈桥、白色拱桥、湖心岛观景平台、西湖喷泉、箱涵、天鹅屋、入水口荷花、草阶看台、world/core.json

- MAP-QSH-CORE §0 结论 · L22 · 1.3k
  - ★ MAP-QSH-CORE §0.1 接入要点 · L38 · 458
- MAP-QSH-CORE §1 图书馆外观 · L45 · 858
- MAP-QSH-CORE §2 入口、品·悦吧与宁静苑 · L54 · 584
- MAP-QSH-CORE §3 时间广场 · L67 · 500
- MAP-QSH-CORE §4 日晷 · L75 · 445
- MAP-QSH-CORE §5 电子科技博物馆 · L82 · 576
- MAP-QSH-CORE §6 西湖的岸线与构筑物 · L93 · 986
- MAP-QSH-CORE §7 西湖喷泉 · L106 · 479
- MAP-QSH-CORE §8 本区的构图关系 · L120 · 375
- MAP-QSH-CORE §9 本区身份特征、设计增补与近景要求 · L127 · 680
- MAP-QSH-CORE §10 待验证 · L147 · 348

## MAP-QSH-CORE-SEASONS · 核心区灯光、四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-CORE-SEASONS-核心区灯光、四季昼夜与天气.md` · 5.0k 字 · 核心区图书馆灯光的全天变化、各对象的夜间角色与四季状态、交给夜景灯光与时刻表（MAP-QSH-ATMOSPHERE）的灯具、避雨处、积水点与雾里地标
关键词：图书馆灯光、檐口灯带、外墙泛光、灯岛、考试周通宵、核心区四季、核心区夜景、夜间角色表、核心区灯具、核心区避雨处、核心区积水点、雾里穹顶

- MAP-QSH-CORE-SEASONS §0 结论 · L22 · 581
  - ★ MAP-QSH-CORE-SEASONS §0.1 接入要点 · L29 · 262
- MAP-QSH-CORE-SEASONS §1 图书馆灯光（主线之后） · L34 · 640
- MAP-QSH-CORE-SEASONS §2 季节与昼夜清单 · L45 · 2.1k
  - MAP-QSH-CORE-SEASONS §2.1 夜间角色与四季 · L49 · 1.7k
  - MAP-QSH-CORE-SEASONS §2.2 交给 MAP-QSH-ATMOSPHERE 的灯具（主线之后） · L87 · 268
- MAP-QSH-CORE-SEASONS §3 天气要点（主线之后） · L91 · 572

## MAP-QSH-CORE-WALK · 核心区行走与声音

`docs/maps/qingshuihe/MAP-QSH-CORE-WALK-核心区行走与声音.md` · 3.6k 字 · 核心区行走主网、能看不能进的阻挡、可进入的室内、涉水区、点位、节奏与锚点、声音要点
关键词：核心区行走、核心区主网、能看不能进、涉水区、核心区点位、P01、P02、P03、P04、P05、P06、P16、R3 西湖跨湖、导览亭 A02、导览桩 A07、手工发声器、喷泉声、堰、天鹅屋声、草阶看台

- MAP-QSH-CORE-WALK §0 结论 · L22 · 530
  - ★ MAP-QSH-CORE-WALK §0.1 接入要点 · L29 · 246
- MAP-QSH-CORE-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L35 · 787
- MAP-QSH-CORE-WALK §2 行走：点位、节奏与锚点（主线之后） · L58 · 928
- MAP-QSH-CORE-WALK §3 声音要点（主线之后） · L77 · 408

## MAP-QSH-INTERIORS · 开放楼室内与开放的高处

`docs/maps/qingshuihe/MAP-QSH-INTERIORS-开放楼室内与开放的高处.md` · 5.6k 字 · 清水河开放楼室内的总表：13 栋的开放范围、封口与观景时刻，3 处开放的高处；图书馆与博物馆见子文档
关键词：开放楼室内、开放楼总表、开放范围要点、封口与限制、观景时刻、学知苑 4 人间、钟楼楼梯、钟室、灯笼观景环、主楼屋顶空廊、电梯关门换位、开放的屋顶与高处、屋顶危险、interiors.json

- MAP-QSH-INTERIORS §0 结论 · L22 · 979
  - ★ MAP-QSH-INTERIORS §0.1 接入要点 · L34 · 385
- MAP-QSH-INTERIORS §1 南区 · L40 · 923
- MAP-QSH-INTERIORS §2 西区、中部与东区 · L49 · 967
- MAP-QSH-INTERIORS §3 北区 · L60 · 971
- MAP-QSH-INTERIORS §4 开放的屋顶与高处（3 处） · L70 · 731

## MAP-QSH-INTERIORS-LIBRARY · 图书馆与博物馆室内

`docs/maps/qingshuihe/MAP-QSH-INTERIORS-LIBRARY-图书馆与博物馆室内.md` · 11.9k 字 · 图书馆与博物馆的开放室内、三条看进去的路径、室内光与视觉落点
关键词：图书馆室内、中庭、回廊、八字楼梯、塔楼梯、交通核、开放范围、封口、门斗、闸机、二楼门厅、看进去的路径、品·悦吧室内、光条、顶棚波光、红桌、视觉落点、八瓣光、台灯、校训木匾、五楼西窗阅览带、北侧露台、博物馆展厅、环形展线、interiors.json、lib.main

- MAP-QSH-INTERIORS-LIBRARY §0 结论 · L22 · 1.6k
  - ★ MAP-QSH-INTERIORS-LIBRARY §0.1 接入要点 · L34 · 829
- MAP-QSH-INTERIORS-LIBRARY §1 平面、交通核与开放范围 · L46 · 1.4k
- MAP-QSH-INTERIORS-LIBRARY §2 三条看进去的路径 · L74 · 621
- MAP-QSH-INTERIORS-LIBRARY §3 中庭与回廊 · L86 · 379
- MAP-QSH-INTERIORS-LIBRARY §4 一楼中厅、门斗与二楼门厅 · L94 · 772
- MAP-QSH-INTERIORS-LIBRARY §5 室外光的延伸 · L102 · 710
- MAP-QSH-INTERIORS-LIBRARY §6 视觉落点 · L112 · 518
- MAP-QSH-INTERIORS-LIBRARY §7 四季、昼夜、天气与生活痕迹 · L121 · 1.3k
- MAP-QSH-INTERIORS-LIBRARY §8 行走 · L134 · 926
- MAP-QSH-INTERIORS-LIBRARY §9 附属部分 · L145 · 620
- MAP-QSH-INTERIORS-LIBRARY §10 博物馆展厅 · L151 · 668
- MAP-QSH-INTERIORS-LIBRARY §11 声音要点 · L163 · 607
- MAP-QSH-INTERIORS-LIBRARY §12 待验证 · L173 · 405

## MAP-QSH-NORTH · 北部生活区

`docs/maps/qingshuihe/MAP-QSH-NORTH-北部生活区.md` · 8.3k 字 · 北部生活区各建筑的外观与身份特征、设计增补与近景要求
关键词：北部生活区、学生活动中心、成电会堂、赭红圆鼓、校医院、院名灯牌、商业街、玻璃拱顶步行街、店招、宿舍组团、学知苑、硕丰苑、博翰苑、留学生组团、本科楼A型、本科楼B型、组团闸机、学知一组团、阳台晾晒、银杏苑、芙蓉餐厅、思学苑、春晖苑、朝阳餐厅、西北餐厅、室外扶梯罩棚、风雨连廊、丹桂路、博士公寓、world/north.json

- MAP-QSH-NORTH §0 结论 · L22 · 912
  - ★ MAP-QSH-NORTH §0.1 接入要点 · L36 · 208
- MAP-QSH-NORTH §1 学生活动中心与成电会堂 · L41 · 653
- MAP-QSH-NORTH §2 校医院 · L53 · 459
- MAP-QSH-NORTH §3 商业街 · L60 · 984
- MAP-QSH-NORTH §4 宿舍 · L69 · 1.5k
- MAP-QSH-NORTH §5 餐厅 · L82 · 1.2k
- MAP-QSH-NORTH §6 文化地标与丹桂路 · L96 · 146
- MAP-QSH-NORTH §7 风雨连廊与本区增补 · L101 · 448
- MAP-QSH-NORTH §8 本区身份特征、设计增补与近景要求 · L111 · 556

## MAP-QSH-NORTH-SEASONS · 北部生活区四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-NORTH-SEASONS-北部生活区四季昼夜与天气.md` · 3.5k 字 · 北部生活区的夜间角色与四季表、开放室内的灯、避雨处、积水点与雾里读形
关键词：北部生活区四季、夜间角色、宿舍熄灯、23:30 熄灯、店招时刻表、开放室内的灯、毕业横幅、雪人、避雨处、雨帘、积水点、雾里读形、雨天的生活痕迹、阳台晾晒收起

- MAP-QSH-NORTH-SEASONS §0 结论 · L22 · 419
  - ★ MAP-QSH-NORTH-SEASONS §0.1 接入要点 · L28 · 195
- MAP-QSH-NORTH-SEASONS §1 季节与昼夜清单 · L33 · 1.5k
  - MAP-QSH-NORTH-SEASONS §1.1 夜间角色与四季 · L39 · 1.3k
  - MAP-QSH-NORTH-SEASONS §1.2 开放室内的灯（主线之后） · L80 · 126
- MAP-QSH-NORTH-SEASONS §2 天气要点（主线之后） · L84 · 580

## MAP-QSH-NORTH-WALK · 北部生活区行走与声音

`docs/maps/qingshuihe/MAP-QSH-NORTH-WALK-北部生活区行走与声音.md` · 5.1k 字 · 北部生活区的行走主网、能看不能进、开放入口的门口表现、点位与锚点、声音要点
关键词：北部生活区行走、行走主网、能看不能进、组团闸机、北门校界、开放入口、门口表现、涉水区、P12、P13、P21、P22、R4、R12、R13、A05、A11、A12、地标可见、声音要点、夜床、Sounds

- MAP-QSH-NORTH-WALK §0 结论 · L22 · 912
  - ★ MAP-QSH-NORTH-WALK §0.1 接入要点 · L29 · 616
- MAP-QSH-NORTH-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L37 · 928
- MAP-QSH-NORTH-WALK §2 开放入口的门口表现（主线之后） · L58 · 587
- MAP-QSH-NORTH-WALK §3 行走：点位、节奏与锚点（主线之后） · L69 · 1.1k
- MAP-QSH-NORTH-WALK §4 声音要点（主线之后） · L89 · 529

## MAP-QSH-OUTSKIRTS · 清水河外围街区

`docs/maps/qingshuihe/MAP-QSH-OUTSKIRTS-清水河外围街区.md` · 6.2k 字 · 清水河校界外街区的结构、地标、灯光、农田、氛围增补、河渠格栅与看街长椅
关键词：周边结构、西源大道、蓉 2 号线、电子科大站、龙湖时代天街、绕城高架、蓉昌高速、220 kV 变电站、顺江片区、综保区、农田、油菜花、林泽杜鹃、川西林盘、绕城绿道、草坪轨道、看街长椅、河渠格栅、清水河银带

- MAP-QSH-OUTSKIRTS §0 结论 · L21 · 1.0k
  - ★ MAP-QSH-OUTSKIRTS §0.1 接入要点 · L31 · 559
- MAP-QSH-OUTSKIRTS §1 周边结构与截面线距离 · L39 · 553
  - MAP-QSH-OUTSKIRTS §1.1 周边结构要点 · L41 · 271
  - MAP-QSH-OUTSKIRTS §1.2 各方位的截面线距离 · L45 · 265
- MAP-QSH-OUTSKIRTS §2 楼型在外围的位置 · L50 · 229
- MAP-QSH-OUTSKIRTS §3 灯光与农田 · L63 · 523
  - MAP-QSH-OUTSKIRTS §3.1 夜间灯光 · L65 · 410
  - MAP-QSH-OUTSKIRTS §3.2 农田与绿地的四季 · L79 · 101
- MAP-QSH-OUTSKIRTS §4 身份地标（保留，让人认得出） · L83 · 379
- MAP-QSH-OUTSKIRTS §5 新增的氛围元素与外围植被 · L95 · 709
- MAP-QSH-OUTSKIRTS §6 河渠格栅与看街长椅 · L107 · 528
  - MAP-QSH-OUTSKIRTS §6.1 河渠格栅（game/gen/maps/qingshuihe/boundary/） · L109 · 286
  - MAP-QSH-OUTSKIRTS §6.2 看街长椅（game/gen/maps/qingshuihe/boundary/ 做，SYS-WALK-BOUNDS 选点） · L113 · 226
- MAP-QSH-OUTSKIRTS §7 截面与远景的清水河取值 · L117 · 1.1k
  - MAP-QSH-OUTSKIRTS §7.1 地层 · L121 · 307
  - MAP-QSH-OUTSKIRTS §7.2 各方位的远景（默认状态） · L134 · 496
  - MAP-QSH-OUTSKIRTS §7.3 雪山 · L146 · 217
- MAP-QSH-OUTSKIRTS §8 待验证 · L153 · 160

## MAP-QSH-RENOVATION · 清水河翻新例外与样板

`docs/maps/qingshuihe/MAP-QSH-RENOVATION-清水河翻新例外与样板.md` · 7.7k 字 · 清水河老楼与环境的翻新：统一预设加十来条身份特征例外，部位代号、三种楼型样板、全校环境、年代未查到与不列入的楼
关键词：翻新预设、身份特征例外、部位代号、楼型、楼型样板、色名、陶红墙带、浅奶咖墙、老式宿舍楼、老教学楼、老食堂、翻新对照楼、年代未查到的楼、不列入的楼、2015 年以后的新楼、全校环境翻新、老楼翻新理由、商业街店招、体育场看台

- MAP-QSH-RENOVATION §0 结论 · L22 · 936
  - ★ MAP-QSH-RENOVATION §0.1 接入要点 · L30 · 514
- MAP-QSH-RENOVATION §1 翻新预设、部位代号与楼型 · L40 · 2.6k
  - MAP-QSH-RENOVATION §1.1 翻新预设 · L42 · 144
  - MAP-QSH-RENOVATION §1.2 例外的写法 · L46 · 111
  - MAP-QSH-RENOVATION §1.3 部位代号与规则出处 · L50 · 756
  - MAP-QSH-RENOVATION §1.4 楼型 · L69 · 177
  - MAP-QSH-RENOVATION §1.5 三种楼型的样板 · L73 · 1.0k
  - MAP-QSH-RENOVATION §1.6 色名 · L93 · 335
- MAP-QSH-RENOVATION §2 北部生活区的例外（MAP-QSH-NORTH，MAP-QSH-NORTH） · L100 · 515
- MAP-QSH-RENOVATION §3 中部与东区的例外（MAP-QSH-CENTER-EAST，MAP-QSH-CENTER-EAST） · L111 · 280
- MAP-QSH-RENOVATION §4 南区的例外（MAP-QSH-SOUTH，MAP-QSH-SOUTH） · L118 · 389
- MAP-QSH-RENOVATION §5 西区与西北区的例外（MAP-QSH-WEST，MAP-QSH-WEST） · L127 · 175
- MAP-QSH-RENOVATION §6 核心区的例外（MAP-QSH-CORE，MAP-QSH-CORE） · L133 · 203
- MAP-QSH-RENOVATION §7 全校环境 · L140 · 737
- MAP-QSH-RENOVATION §8 年代未查到的楼 · L155 · 244
- MAP-QSH-RENOVATION §9 不列入的楼 · L164 · 162
- MAP-QSH-RENOVATION §10 登记与验收 · L168 · 387

## MAP-QSH-SOUTH · 南区

`docs/maps/qingshuihe/MAP-QSH-SOUTH-南区.md` · 9.6k 字 · 南区各建筑与构筑物的外观、身份特征、设计增补与近景要求
关键词：南区、南门、校名墙、主楼、主轴线、屋顶空廊、过街楼、主楼泛光、森林公园、孔雀之家、东湖岸线、环湖跑道、里程标、六角亭、亲水平台、荷角、吴立人像、宣邦楼、钟楼、电子管穹顶、交班的钟、钟面指针、立人楼、体育馆、游泳馆、体育运动中心大平台、看台罩棚、国际创新中心、1–8 号楼、world/south.json

- MAP-QSH-SOUTH §0 结论 · L22 · 697
  - ★ MAP-QSH-SOUTH §0.1 接入要点 · L36 · 158
- MAP-QSH-SOUTH §1 南门 · L41 · 516
- MAP-QSH-SOUTH §2 主楼 · L49 · 1.1k
- MAP-QSH-SOUTH §3 过街楼、开放室内与周边 · L62 · 618
- MAP-QSH-SOUTH §4 东湖 · L70 · 919
- MAP-QSH-SOUTH §5 宣邦楼 · L81 · 1.3k
- MAP-QSH-SOUTH §6 立人楼 · L97 · 351
- MAP-QSH-SOUTH §7 体育场馆（沿西源大道从西到东） · L104 · 806
- MAP-QSH-SOUTH §8 西南角：国际创新中心与科创餐厅 · L114 · 336
- MAP-QSH-SOUTH §9 南区中部的 1–8 号楼 · L120 · 312
- MAP-QSH-SOUTH §10 本区的水道 · L124 · 242
- MAP-QSH-SOUTH §11 本区身份特征、设计增补与近景要求 · L130 · 725
- MAP-QSH-SOUTH §12 待验证 · L153 · 290

## MAP-QSH-SOUTH-SEASONS · 南区四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-SOUTH-SEASONS-南区四季昼夜与天气.md` · 4.4k 字 · 南区各对象的夜间角色与四季状态、交给夜景灯光与时刻表（MAP-QSH-ATMOSPHERE）的灯具、避雨处、积水点、雾里地标与彩虹
关键词：南区四季、南区夜景、夜间角色表、主楼泛光、钟面背光、南区灯具、南区避雨处、南区积水点、雾里地标、钟楼雾豁免、彩虹、雨天生活痕迹

- MAP-QSH-SOUTH-SEASONS §0 结论 · L22 · 690
  - ★ MAP-QSH-SOUTH-SEASONS §0.1 接入要点 · L29 · 413
- MAP-QSH-SOUTH-SEASONS §1 季节与昼夜清单 · L36 · 2.0k
  - MAP-QSH-SOUTH-SEASONS §1.1 夜间角色与四季 · L40 · 1.7k
  - MAP-QSH-SOUTH-SEASONS §1.2 交给 MAP-QSH-ATMOSPHERE 的灯具（主线之后） · L82 · 273
- MAP-QSH-SOUTH-SEASONS §2 天气要点（主线之后） · L86 · 627

## MAP-QSH-SOUTH-WALK · 南区行走与声音

`docs/maps/qingshuihe/MAP-QSH-SOUTH-WALK-南区行走与声音.md` · 4.5k 字 · 南区行走主网、能看不能进的阻挡、可进入的室内与开放入口、涉水区、点位、节奏与锚点、声音要点
关键词：南区行走、南区主网、能看不能进、开放入口、涉水区、南区点位、P07、P09、P17、P18、P19、R1 南区段、R2 环湖、R7、R8、R9、R10、导览亭 A01、A03、A06、南区声音、钟声、草阶看台

- MAP-QSH-SOUTH-WALK §0 结论 · L22 · 991
  - ★ MAP-QSH-SOUTH-WALK §0.1 接入要点 · L30 · 630
- MAP-QSH-SOUTH-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L37 · 1.0k
- MAP-QSH-SOUTH-WALK §2 行走：点位、节奏与锚点（主线之后） · L61 · 1.0k
- MAP-QSH-SOUTH-WALK §3 声音要点（主线之后） · L80 · 478

## MAP-QSH-TERRAIN · 全图地形骨架

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-全图地形骨架.md` · 6.4k 字 · 清水河地形的全图骨架：层分布、红枫丘、遮挡与揭示路线、各水体常水位、坡度与着色的本图实例
关键词：地形骨架、红枫丘、地标丘、下沉时间广场、常水位表、湖口跌水、箱涵堰口、遮挡与揭示、揭示路线、中轴落金线、湿地堤、起伏的理由、水位阶梯、分面平法线

- MAP-QSH-TERRAIN §0 结论 · L22 · 1.2k
  - ★ MAP-QSH-TERRAIN §0.1 接入要点 · L38 · 264
- MAP-QSH-TERRAIN §1 全图骨架 · L44 · 1.9k
  - MAP-QSH-TERRAIN §1.1 分层分布 · L46 · 778
  - MAP-QSH-TERRAIN §1.2 地标丘：红枫丘 · L61 · 534
  - MAP-QSH-TERRAIN §1.3 遮挡与揭示的三条路线 · L69 · 592
- MAP-QSH-TERRAIN §2 水系与常水位 · L79 · 1.0k
- MAP-QSH-TERRAIN §3 坡度与着色 · L100 · 414

## MAP-QSH-TERRAIN-CHECKS · 地形验收与点位标高

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-CHECKS-地形验收与点位标高.md` · 3.5k 字 · 地形验收的四个镜头、冲突检查结论与点位、锚点的标高处理
关键词：地形验收镜头、冲突检查、垫层与楼体高差、点位标高、锚点标高、室内同位点、地形求值、仰望镜头、水边镜头、俯瞰镜头、回望镜头、跳跃链、道路走廊、栏杆碰撞顶

- MAP-QSH-TERRAIN-CHECKS §0 结论 · L22 · 640
  - ★ MAP-QSH-TERRAIN-CHECKS §0.1 接入要点 · L28 · 405
- MAP-QSH-TERRAIN-CHECKS §1 验收镜头 · L35 · 249
- MAP-QSH-TERRAIN-CHECKS §2 冲突检查 · L46 · 370
- MAP-QSH-TERRAIN-CHECKS §3 点位与锚点标高 · L52 · 613

## MAP-QSH-TERRAIN-DISTRICTS · 十处分区地形

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-DISTRICTS-十处分区地形.md` · 9.6k 字 · 清水河十处分区地形的标高、坡度、连接与对点位的约束，以及地形文件归属
关键词：分区地形、下沉时间广场、落金沟、湖岸丘链、草阶看台台地、东湖亭丘、岛丘、品学楼台基、坡广场、宿舍台原、上台、下台、台口、通学坡、看月台、堤上路、球场堤路、下沉草阶草坪、河岸低步道、渠堤、湿地低地、扇形草坪浅碟、中庭草丘、零带、terrain.json

- MAP-QSH-TERRAIN-DISTRICTS §0 结论 · L22 · 971
  - ★ MAP-QSH-TERRAIN-DISTRICTS §0.1 接入要点 · L33 · 493
- MAP-QSH-TERRAIN-DISTRICTS §1 文件归属 · L40 · 547
- MAP-QSH-TERRAIN-DISTRICTS §2 中轴广场与图书馆 · L52 · 464
- MAP-QSH-TERRAIN-DISTRICTS §3 银杏大道 · L61 · 359
- MAP-QSH-TERRAIN-DISTRICTS §4 西湖与红枫丘 · L68 · 798
- MAP-QSH-TERRAIN-DISTRICTS §5 东湖 · L78 · 458
- MAP-QSH-TERRAIN-DISTRICTS §6 河渠与箱涵 · L85 · 733
- MAP-QSH-TERRAIN-DISTRICTS §7 宿舍区 · L94 · 1.2k
- MAP-QSH-TERRAIN-DISTRICTS §8 运动场与体育区 · L106 · 620
- MAP-QSH-TERRAIN-DISTRICTS §9 食堂与商业街 · L114 · 326
- MAP-QSH-TERRAIN-DISTRICTS §10 教学科研区 · L120 · 917
- MAP-QSH-TERRAIN-DISTRICTS §11 校门与校界 · L128 · 304

## MAP-QSH-VEGETATION · 清水河植被

`docs/maps/qingshuihe/MAP-QSH-VEGETATION-清水河植被.md` · 4.9k 字 · 清水河各树种的四季观感与种在哪、线性与片区种植、地面覆盖与水生植物的位置
关键词：种在哪、银杏大道、水杉路、丹桂路、春季粉带、森林公园红枫、校友林、湿地公园、友谊树、银杏林、樱花林、蓝花楹、入水口荷、水下森林位置、油菜花带位置

- MAP-QSH-VEGETATION §0 结论 · L22 · 434
  - ★ MAP-QSH-VEGETATION §0.1 接入要点 · L31 · 105
- MAP-QSH-VEGETATION §1 落叶树的四季与种在哪 · L35 · 804
- MAP-QSH-VEGETATION §2 常绿树与灌木的四季与种在哪 · L56 · 346
- MAP-QSH-VEGETATION §3 种植 · L69 · 1.1k
  - MAP-QSH-VEGETATION §3.1 线性种植 · L71 · 481
  - MAP-QSH-VEGETATION §3.2 片区种植 · L79 · 572
- MAP-QSH-VEGETATION §4 地面覆盖的具体位置 · L87 · 382
- MAP-QSH-VEGETATION §5 水生植物的位置 · L98 · 210
- MAP-QSH-VEGETATION §6 验收视角 · L104 · 169
- MAP-QSH-VEGETATION §7 待验证 · L109 · 57

## MAP-QSH-WATER · 清水河水体

`docs/maps/qingshuihe/MAP-QSH-WATER-清水河水体.md` · 5.7k 字 · 西湖、东湖、河渠、池水与叠水喷泉的具体水面，开放泳区名单、倒影候选湖与各时段倒影
关键词：西湖水下森林、西湖喷泉水柱、东湖荷区、锦鲤、冬季浅滩、走马河、红光右支渠、磨底河、河渠中线、漂浮物、游泳馆池水、品学楼叠水喷泉、主楼水池、泳区名单、倒影候选湖、主要倒影按时段

- MAP-QSH-WATER §0 结论 · L22 · 637
  - ★ MAP-QSH-WATER §0.1 接入要点 · L31 · 315
- MAP-QSH-WATER §1 西湖 · L38 · 535
  - MAP-QSH-WATER §1.1 水下森林与反光带 · L40 · 314
  - MAP-QSH-WATER §1.2 扇形喷泉 · L46 · 212
- MAP-QSH-WATER §2 东湖 · L53 · 464
- MAP-QSH-WATER §3 河渠 · L60 · 812
- MAP-QSH-WATER §4 池水、叠水喷泉与主楼水池 · L75 · 1.1k
  - MAP-QSH-WATER §4.1 游泳馆与室外游泳池 · L77 · 559
  - MAP-QSH-WATER §4.2 品学楼门前叠水喷泉 · L87 · 469
  - MAP-QSH-WATER §4.3 主楼水池 · L97 · 85
- MAP-QSH-WATER §5 开放泳区的名单与登记 · L101 · 296
- MAP-QSH-WATER §6 倒影候选湖与主要倒影 · L109 · 426
- MAP-QSH-WATER §7 待验证 · L124 · 180

## MAP-QSH-WEST · 西区与西北区

`docs/maps/qingshuihe/MAP-QSH-WEST-西区与西北区.md` · 6.6k 字 · 西区与西北区各楼、石刻、湿地与西门桥头的外观、身份特征与近景要求
关键词：西区、西北区、六号科研楼、基础与前沿研究院、公式台阶、2F室外连廊、校训石、创新中心、经管楼、三号科研实验楼、宾诺咖啡、立人托幼公园、键盘立面、四号科研楼、扇形草坪、深蓝玻璃盒入口、五号科研楼、电子科学技术研究院大楼、微波暗室、工程训练中心、荟文楼、综合楼、核磁共振中心、国防科技园石刻、湿地公园、湿地栈道、西门桥头、水杉路、孤儿楼 6151 与 6154、world/west.json

- MAP-QSH-WEST §0 结论 · L22 · 840
  - ★ MAP-QSH-WEST §0.1 接入要点 · L36 · 130
- MAP-QSH-WEST §1 六号科研楼 · L40 · 651
- MAP-QSH-WEST §2 校训石 · L49 · 255
- MAP-QSH-WEST §3 创新中心 · L54 · 444
- MAP-QSH-WEST §4 经管楼 · L61 · 89
- MAP-QSH-WEST §5 立人托幼公园 · L65 · 471
- MAP-QSH-WEST §6 四号科研楼 · L70 · 368
- MAP-QSH-WEST §7 五号科研楼、研究院大楼、核磁共振中心 · L76 · 341
- MAP-QSH-WEST §8 荟文楼（综合楼） · L83 · 68
- MAP-QSH-WEST §9 石刻 · L87 · 65
- MAP-QSH-WEST §10 湿地公园与西门桥头 · L91 · 625
- MAP-QSH-WEST §11 本区身份特征、设计增补与近景要求 · L101 · 861

## MAP-QSH-WEST-SEASONS · 西区与西北区四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-WEST-SEASONS-西区与西北区四季昼夜与天气.md` · 3.7k 字 · 西区与西北区的夜间角色与四季表、开放室内的灯、避雨处与雨帘、积水点、雾里读形与推荐状态
关键词：西区四季、西区夜间角色、六号科研楼受光、湿地暗区、萤火虫、避雨处、候车亭雨帘、光伏车棚、积水点、雨水花园、雾里读形、晨雾、雨天的生活痕迹、P10 推荐状态

- MAP-QSH-WEST-SEASONS §0 结论 · L22 · 345
  - ★ MAP-QSH-WEST-SEASONS §0.1 接入要点 · L29 · 117
- MAP-QSH-WEST-SEASONS §1 季节与昼夜清单 · L33 · 1.7k
- MAP-QSH-WEST-SEASONS §2 天气要点（主线之后） · L80 · 611

## MAP-QSH-WEST-WALK · 西区与西北区行走与声音

`docs/maps/qingshuihe/MAP-QSH-WEST-WALK-西区与西北区行走与声音.md` · 4.8k 字 · 西区与西北区的行走主网、能看不能进、开放入口的门口表现、点位与锚点、声音要点
关键词：西区行走、行走主网、水杉路、湿地栈道、能看不能进、托幼园围栏、西门校界、开放入口、门口表现、涉水区、卵石滩、P05、P10、P11、P20、R11、A08、A09、A10、地标可见、声音要点、Sounds

- MAP-QSH-WEST-WALK §0 结论 · L22 · 900
  - ★ MAP-QSH-WEST-WALK §0.1 接入要点 · L29 · 587
- MAP-QSH-WEST-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L38 · 916
- MAP-QSH-WEST-WALK §2 开放入口与门口表现（主线之后） · L57 · 500
- MAP-QSH-WEST-WALK §3 行走：点位、节奏与锚点（主线之后） · L64 · 1.1k
- MAP-QSH-WEST-WALK §4 声音要点（主线之后） · L81 · 305

## SYS-INDEX · 系统总纲

`docs/systems/SYS-INDEX-系统总纲.md` · 1.7k 字 · 系统域总纲：运行时各系统怎么工作的通用原则和文档树
关键词：系统、系统总纲、文档树、SYS、观察模式、行走、时间与季节、天气、存档、设置文件、多地图、转场、过渡不硬切、音频、画面设置、开始页

- SYS-INDEX §0 结论 · L21 · 570
  - ★ SYS-INDEX §0.1 接入要点 · L29 · 169
- SYS-INDEX §1 文档树 · L34 · 597

## SYS-ATLAS · 平面地图与传送

`docs/systems/SYS-ATLAS-平面地图与传送.md` · 3.1k 字 · 平面地图总则：允许与禁止什么、多图数据归属、设置里存什么；界面与传送见子文档
关键词：平面地图、地图只活在界面层、3D 里不标注、IAtlasService、MarkerId、前往方式、图层筛选、AtlasSave、TransitPreference、小地图、大地图、传送、锚点、全开放、自定义标记、追踪

- SYS-ATLAS §0 结论 · L21 · 699
  - ★ SYS-ATLAS §0.1 接入要点 · L32 · 360
- SYS-ATLAS §1 允许什么、禁止什么 · L38 · 399
- SYS-ATLAS §2 多地图下的数据 · L50 · 537
- SYS-ATLAS §3 存档里存什么、设置里存什么 · L57 · 426

## SYS-ATLAS-TRAVEL · 标记、锚点与传送

`docs/systems/SYS-ATLAS-TRAVEL-标记、锚点与传送.md` · 5.6k 字 · 标记类型与可传送范围、锚点实体（导览亭与导览桩）、传送流程与落点；主线全开放，解锁策略在主线之后
关键词：地图标记、标记类型、锚点、传送点、导览亭、导览桩、传送、加载界面、飞过去、传送落点、落地检查、解锁策略、all-unlocked、proximity、开放室内标记、停放点标记、IUnlockPolicy、TeleportSpec、TravelStyle、TravelRefusal、UnlockState、AnchorForm、MarkerKind、AtlasMarker

- SYS-ATLAS-TRAVEL §0 结论 · L22 · 582
  - ★ SYS-ATLAS-TRAVEL §0.1 接入要点 · L29 · 324
- SYS-ATLAS-TRAVEL §1 标记类型 · L36 · 1.2k
- SYS-ATLAS-TRAVEL §2 锚点实体：导览亭与导览桩 · L54 · 708
- SYS-ATLAS-TRAVEL §3 传送流程 · L67 · 1.4k
  - SYS-ATLAS-TRAVEL §3.1 两种方式 · L69 · 299
  - SYS-ATLAS-TRAVEL §3.2 传送的时序 · L75 · 384
  - SYS-ATLAS-TRAVEL §3.3 落点 · L86 · 452
  - SYS-ATLAS-TRAVEL §3.4 预取、打断与边界情况 · L98 · 270
- SYS-ATLAS-TRAVEL §4 解锁 · L103 · 339
- SYS-ATLAS-TRAVEL §5 待验证 · L113 · 37

## SYS-ATLAS-UI · 地图界面

`docs/systems/SYS-ATLAS-UI-地图界面.md` · 6.2k 字 · 平面地图的界面：底图与上色、外围与校界画法、标注、小地图、大地图；自定义标记与追踪在主线之后
关键词：底图、小地图、大地图、地图页、atlasRect、距离场、map.* 色值、区域名、地名标注、labels.json、缩放档、目录页、传送卡、图层

- SYS-ATLAS-UI §0 结论 · L22 · 417
  - ★ SYS-ATLAS-UI §0.1 接入要点 · L27 · 157
- SYS-ATLAS-UI §1 底图 · L33 · 1.9k
  - SYS-ATLAS-UI §1.1 来源：世界模型 · L35 · 414
  - SYS-ATLAS-UI §1.2 上色 · L44 · 497
  - SYS-ATLAS-UI §1.3 外围、校界与箱庭外缘 · L50 · 426
  - SYS-ATLAS-UI §1.4 季节与昼夜：只做轻微变化 · L57 · 193
  - SYS-ATLAS-UI §1.5 文字与标注 · L62 · 388
- SYS-ATLAS-UI §2 小地图 · L69 · 878
- SYS-ATLAS-UI §3 大地图 · L81 · 1.7k
  - SYS-ATLAS-UI §3.1 布局与开合 · L83 · 466
  - SYS-ATLAS-UI §3.2 缩放与平移 · L90 · 288
  - SYS-ATLAS-UI §3.3 图层与目录 · L95 · 240
  - SYS-ATLAS-UI §3.4 传送卡 · L100 · 354
  - SYS-ATLAS-UI §3.5 自定义标记与追踪（主线之后） · L106 · 285
  - SYS-ATLAS-UI §3.6 按键 · L111 · 90
- SYS-ATLAS-UI §4 待验证 · L115 · 121

## SYS-AUDIO · 音频

`docs/systems/SYS-AUDIO-音频.md` · 7.0k 字 · 声音共守的规则：内核与生命周期、混音与响度、加速节制与转场、人迹禁区、界面音、开场音乐、设置、素材许可、预算验收
关键词：音频、声音、混音、响度、加速节制、转场、人迹声、界面音效、开场音乐、音频设置、素材许可、署名页、IAudioService、AudioBuses、AudioSettings、settings.json、G21

- SYS-AUDIO §0 结论 · L21 · 1.1k
  - ★ SYS-AUDIO §0.1 接入要点 · L38 · 394
- SYS-AUDIO §1 总体与生命周期 · L44 · 770
- SYS-AUDIO §2 混音与响度 · L56 · 497
- SYS-AUDIO §3 加速流逝的节制与转场 · L75 · 497
- SYS-AUDIO §4 人迹声：确保不出现清晰人声 · L87 · 511
- SYS-AUDIO §5 界面音效 · L98 · 415
- SYS-AUDIO §6 开场音乐 · L106 · 290
- SYS-AUDIO §7 设置 · L111 · 377
- SYS-AUDIO §8 素材策略与许可 · L117 · 557
- SYS-AUDIO §9 预算（设计目标） · L125 · 174
- SYS-AUDIO §10 验收 · L132 · 234
- SYS-AUDIO §11 待验证（P0） · L138 · 110

## SYS-AUDIO-AMBIENCE · 环境声景与天气事件声

`docs/systems/SYS-AUDIO-AMBIENCE-环境声景与天气事件声.md` · 5.3k 字 · 环境声七层与数据驱动发声器、季节出没、天气声、远雷钟声广播与偶发事件的声音
关键词：环境声、声景、七层、发声器、声景场、床、鸟声、生命感、室内档案、英雄声景、天气声、雨声分层、远雷、钟声、校园广播、偶发事件声、IAmbienceService、IAmbientEmitterSource、AmbientEmitter、SoundSocketDecl、AmbienceCue、ILightningFeed

- SYS-AUDIO-AMBIENCE §0 结论 · L20 · 647
  - ★ SYS-AUDIO-AMBIENCE §0.1 接入要点 · L27 · 383
- SYS-AUDIO-AMBIENCE §1 环境声景 · L33 · 2.0k
  - SYS-AUDIO-AMBIENCE §1.1 七层与驱动量 · L35 · 344
  - SYS-AUDIO-AMBIENCE §1.2 数据驱动的发声器（主线之后） · L42 · 410
  - SYS-AUDIO-AMBIENCE §1.3 季节与出没 · L49 · 460
  - SYS-AUDIO-AMBIENCE §1.4 生命感规则 · L55 · 296
  - SYS-AUDIO-AMBIENCE §1.5 室内档案（主线之后） · L62 · 122
  - SYS-AUDIO-AMBIENCE §1.6 校外、外围与边界 · L66 · 126
  - SYS-AUDIO-AMBIENCE §1.7 离地高度与介质 · L70 · 118
  - SYS-AUDIO-AMBIENCE §1.8 英雄声景 · L74 · 102
- SYS-AUDIO-AMBIENCE §2 天气声 · L78 · 763
  - SYS-AUDIO-AMBIENCE §2.1 分天气的声音 · L82 · 336
  - SYS-AUDIO-AMBIENCE §2.2 雨与风的分层、起止与残留 · L97 · 377
- SYS-AUDIO-AMBIENCE §3 远雷、钟声、广播与偶发事件 · L104 · 690

## SYS-AUDIO-FOLEY · 观者声与空间声学

`docs/systems/SYS-AUDIO-FOLEY-观者声与空间声学.md` · 3.9k 字 · 脚步与交互音、材质与覆盖物、飞行风声，窄处脚步更明显的空间声学、遮挡与室内外传声
关键词：观者声、脚步声、步相、落地、材质、覆盖物、交互音、飞行风声、空间声学、混响、声学网格、门户、遮挡、室内外传声、FootMat、IGroundCover、IAcousticEnv、IFoleyService、IWalkTelemetry、洗牌袋

- SYS-AUDIO-FOLEY §0 结论 · L20 · 325
  - ★ SYS-AUDIO-FOLEY §0.1 接入要点 · L26 · 135
- SYS-AUDIO-FOLEY §1 观者声 · L30 · 1.4k
  - SYS-AUDIO-FOLEY §1.1 原则 · L32 · 156
  - SYS-AUDIO-FOLEY §1.2 步相与步频 · L37 · 220
  - SYS-AUDIO-FOLEY §1.3 材质与覆盖物 · L43 · 358
  - SYS-AUDIO-FOLEY §1.4 分层与变体 · L49 · 171
  - SYS-AUDIO-FOLEY §1.5 交互音 · L54 · 295
  - SYS-AUDIO-FOLEY §1.6 飞行风声 · L61 · 160
- SYS-AUDIO-FOLEY §2 空间声学（主线之后） · L65 · 1.0k
  - SYS-AUDIO-FOLEY §2.1 "窄处脚步更明显" · L67 · 187
  - SYS-AUDIO-FOLEY §2.2 方案与类别 · L73 · 342
  - SYS-AUDIO-FOLEY §2.3 遮挡与室内外传声 · L79 · 342
  - SYS-AUDIO-FOLEY §2.4 介质与天气的消音感 · L87 · 116

## SYS-BORDER · 边界阻挡与提示

`docs/systems/SYS-BORDER-边界阻挡与提示.md` · 4.9k 字 · 校界的行走阻挡与缓停、旁观飞行的可达空间与推回、edge_fog、提示文案、多地图衔接
关键词：行走阻挡、飞行阻挡、缓停、推回、缓冲带、软地板、可达空间、活动范围、edge_fog、边缘雾、前方暂未开放、合法化、地图入口

- SYS-BORDER §0 结论 · L22 · 643
  - ★ SYS-BORDER §0.1 接入要点 · L27 · 456
- SYS-BORDER §1 行走阻挡 · L34 · 767
  - SYS-BORDER §1.1 挡什么（game/gen/maps/qingshuihe/boundary/、SYS-WALK-BOUNDS） · L36 · 499
  - SYS-BORDER §1.2 0.6 m 内的缓停（game/camera/walk/） · L46 · 192
  - SYS-BORDER §1.3 兜底 · L51 · 65
- SYS-BORDER §2 飞行阻挡（旁观模式） · L55 · 1.5k
  - SYS-BORDER §2.1 可达空间 · L57 · 377
  - SYS-BORDER §2.2 缓冲带与推回（SYS-VIEW） · L66 · 436
  - SYS-BORDER §2.3 边缘雾 `edge_fog` · L75 · 426
  - SYS-BORDER §2.4 参数、合法化与不受约束的镜头 · L82 · 265
- SYS-BORDER §3 提示文案与多地图衔接 · L88 · 664
  - SYS-BORDER §3.1 提示文案 · L90 · 316
  - SYS-BORDER §3.2 与多地图、平面地图、存档的衔接 · L101 · 331

## SYS-GRAPHICS · 画面设置

`docs/systems/SYS-GRAPHICS-画面设置.md` · 8.0k 字 · 主线三个效果预设与十余个单项、风格底线、应用方式、持久化；自动调节与首次分档在主线之后
关键词：画面设置、画质、画面预设、设备档、均衡、精致、极致、单项、作用域、自定义、风格底线、风格默认、渲染精度、FSR 锐化、暗角、加载类设置、画面页、恢复默认、IQualityService、IQualityAware、QualityResponsive、QualityKnob、quality-presets.json、settings.json

- SYS-GRAPHICS §0 结论 · L22 · 1.7k
  - ★ SYS-GRAPHICS §0.1 接入要点 · L33 · 943
- SYS-GRAPHICS §1 预设 · L45 · 709
- SYS-GRAPHICS §2 风格底线与风格默认 · L61 · 707
- SYS-GRAPHICS §3 单项与生效方式 · L74 · 1.4k
- SYS-GRAPHICS §4 应用方式 · L97 · 591
- SYS-GRAPHICS §5 自动推荐与自动调节（主线之后） · L107 · 398
- SYS-GRAPHICS §6 持久化与恢复 · L116 · 418
- SYS-GRAPHICS §7 画面页 · L122 · 193
- SYS-GRAPHICS §8 待验证（P0） · L126 · 358

## SYS-MAPS · 多地图

`docs/systems/SYS-MAPS-多地图.md` · 10.9k 字 · 按图数据与全局的划分、map.json 约定、归属、map_rect、单图规模；先卸后装的切图运行时在主线之后
关键词：多地图、换图、切图、地图服务、按图数据、先卸后装、同图重载、天气按图、单图半径、本机烘焙缓存、IMapService、IMapAware、MapScope、MapDescriptor、map.json、map_rect

- SYS-MAPS §0 结论 · L21 · 1.5k
  - ★ SYS-MAPS §0.1 接入要点 · L32 · 877
- SYS-MAPS §1 数据归属 · L43 · 954
  - SYS-MAPS §1.1 按图与全局 · L45 · 788
  - SYS-MAPS §1.2 烘焙缓存 · L63 · 155
- SYS-MAPS §2 地图描述 `map.json` · L68 · 651
- SYS-MAPS §3 切换 · L79 · 2.6k
  - SYS-MAPS §3.1 一个 World3D，先卸后装 · L83 · 151
  - SYS-MAPS §3.2 切图的步骤（切图部分主线之后） · L87 · 1.2k
  - SYS-MAPS §3.3 规则（主线之后） · L101 · 582
  - SYS-MAPS §3.4 入口 · L110 · 334
  - SYS-MAPS §3.5 同图重载不泄漏（主线之后） · L119 · 234
- SYS-MAPS §4 世界状态：时间与季节全局，天气按图求值 · L125 · 612
- SYS-MAPS §5 相机、行走与安全点（主线之后） · L143 · 430
- SYS-MAPS §6 归属与 `MapScope` · L154 · 868
- SYS-MAPS §7 全局参数 `map_rect` · L162 · 686
- SYS-MAPS §8 内存、包体与单图半径 · L172 · 233
- SYS-MAPS §9 以后的方向 · L178 · 259
- SYS-MAPS §10 待验证（P0） · L182 · 261

## SYS-MOTION · 动态与事件

`docs/systems/SYS-MOTION-动态与事件.md` · 6.0k 字 · 两个时钟与一拍二、动作幅度上限、转场的唯一详述；风与环境动态、生命体与偶发事件见子文档
关键词：动态、两个时钟、动画时钟、AnimTime、一拍二、step_rate、480 s 回绕、转场、换季转场、改时间、同时转场、减少动态效果

- SYS-MOTION §0 结论 · L21 · 998
  - ★ SYS-MOTION §0.1 接入要点 · L36 · 371
- SYS-MOTION §1 时钟与计时 · L44 · 1.1k
  - SYS-MOTION §1.1 两个时钟 · L46 · 365
  - SYS-MOTION §1.2 计时规则 · L52 · 433
  - SYS-MOTION §1.3 各档流逝的表现 · L59 · 157
  - SYS-MOTION §1.4 镜头 · L63 · 92
- SYS-MOTION §2 节奏规则 · L67 · 678
  - SYS-MOTION §2.1 幅度 · L69 · 47
  - SYS-MOTION §2.2 一拍二 · L73 · 385
  - SYS-MOTION §2.3 偶发事件 · L82 · 122
  - SYS-MOTION §2.4 声音的节奏 · L86 · 113
- SYS-MOTION §3 转场 · L90 · 2.4k
  - SYS-MOTION §3.1 改时间 · L94 · 242
  - SYS-MOTION §3.2 换季 · L102 · 721
  - SYS-MOTION §3.3 跳转、传送与模式切换 · L118 · 202
  - SYS-MOTION §3.4 换天气 · L122 · 440
  - SYS-MOTION §3.5 叠加与优先级 · L130 · 206
  - SYS-MOTION §3.6 减少动态效果 · L138 · 141
  - SYS-MOTION §3.7 验收原则 · L142 · 56
  - SYS-MOTION §3.8 协调者 · L146 · 95
  - SYS-MOTION §3.9 开始页、进入游戏与读档 · L150 · 132

## SYS-MOTION-ENV · 风与环境动态

`docs/systems/SYS-MOTION-ENV-风与环境动态.md` · 4.9k 字 · 风及其连锁反应、飘落物与瞬时痕迹、流体、光与远景的缓慢变化、季节氛围、粒子类的解析运动
关键词：风场、阵风、IWindField、飘落池、飘落物、萤火虫、解析运动、季节氛围、风量、雾海、云漂移、MultiMesh

- SYS-MOTION-ENV §0 结论 · L21 · 565
  - ★ SYS-MOTION-ENV §0.1 接入要点 · L30 · 177
- SYS-MOTION-ENV §1 风：一个起因带动一串变化 · L36 · 509
- SYS-MOTION-ENV §2 悬浮物与瞬时痕迹 · L44 · 619
  - SYS-MOTION-ENV §2.1 飘落物 · L46 · 407
  - SYS-MOTION-ENV §2.2 行走时的瞬时痕迹 · L59 · 197
- SYS-MOTION-ENV §3 流体 · L63 · 182
- SYS-MOTION-ENV §4 光的缓慢变化 · L68 · 417
- SYS-MOTION-ENV §5 远景与天象 · L77 · 377
  - SYS-MOTION-ENV §5.1 雾海、山、云与星空 · L79 · 238
  - SYS-MOTION-ENV §5.2 闪电、远雷与彩虹 · L84 · 127
- SYS-MOTION-ENV §6 季节氛围 · L88 · 651
  - SYS-MOTION-ENV §6.1 季节权重 · L92 · 74
  - SYS-MOTION-ENV §6.2 萤火虫（夏夜） · L96 · 151
  - SYS-MOTION-ENV §6.3 雪（冬季） · L100 · 148
  - SYS-MOTION-ENV §6.4 风量 · L104 · 139
  - SYS-MOTION-ENV §6.5 夏季的小动态 · L108 · 58
- SYS-MOTION-ENV §7 粒子类与解析运动 · L112 · 752
- SYS-MOTION-ENV §8 待验证 · L123 · 53

## SYS-MOTION-LIFE · 生命体与偶发事件

`docs/systems/SYS-MOTION-LIFE-生命体与偶发事件.md` · 4.4k 字 · 动物的小动作节奏与对行走身体作出反应的动物、室内与钟楼的微动、全片唯一的偶发事件
关键词：生命体、反应动物、动物小动作、一拍二、室内微动、钟楼、钟锤、偶发事件、晚归者、late-returner、规则集 incidents、天气版本

- SYS-MOTION-LIFE §0 结论 · L21 · 440
  - ★ SYS-MOTION-LIFE §0.1 接入要点 · L29 · 129
- SYS-MOTION-LIFE §1 生命体 · L34 · 1.3k
  - SYS-MOTION-LIFE §1.1 对行走身体作出反应的动物 · L49 · 625
- SYS-MOTION-LIFE §2 室内与钟楼的微动 · L61 · 440
- SYS-MOTION-LIFE §3 偶发事件（全片唯一） · L71 · 1.3k
  - SYS-MOTION-LIFE §3.1 触发 · L75 · 284
  - SYS-MOTION-LIFE §3.2 路线 · L79 · 62
  - SYS-MOTION-LIFE §3.3 夜间版本 · L83 · 194
  - SYS-MOTION-LIFE §3.4 白天版本 · L87 · 126
  - SYS-MOTION-LIFE §3.5 克制 · L91 · 83
  - SYS-MOTION-LIFE §3.6 数据与写法 · L95 · 231
  - SYS-MOTION-LIFE §3.7 天气版本 · L99 · 146
- SYS-MOTION-LIFE §4 待验证 · L103 · 153

## SYS-SAVE · 存档与持久化

`docs/systems/SYS-SAVE-存档与持久化.md` · 4.1k 字 · 存档位与世界线、各存档段存什么不存什么；保存读档与格式安全见子文档
关键词：存档、存档位、世界线、存档段、单实例锁、条目、自动池、手动池、立即保存、删除存档、继续、新游戏、不存什么、恢复阶段、ISaveService、ISaveSection、SaveSectionAttribute、RestorePhase

- SYS-SAVE §0 结论 · L22 · 770
  - ★ SYS-SAVE §0.1 接入要点 · L30 · 317
- SYS-SAVE §1 存档模型 · L36 · 926
- SYS-SAVE §2 存什么、不存什么 · L48 · 1.3k

## SYS-SAVE-FLOW · 保存时机与读档流程

`docs/systems/SYS-SAVE-FLOW-保存时机与读档流程.md` · 6.0k 字 · 保存时机、安全时刻与落定快照、退出保存、读档顺序、世界实例、状态一致、新游戏建档、校验修复
关键词：保存、读档、自动保存、安全时刻、落定快照、续跑等价、世界实例、校验修复、退出保存、现实时间不计、SaveReason、LoadScope、CaptureContext、RestoreContext、NewGameContext、SaveStatus、SaveResult、LoadStatus、LoadResult、SaveState、IWorldState.Instance

- SYS-SAVE-FLOW §0 结论 · L22 · 552
  - ★ SYS-SAVE-FLOW §0.1 接入要点 · L29 · 278
- SYS-SAVE-FLOW §1 保存时机与可靠性 · L35 · 1.5k
  - SYS-SAVE-FLOW §1.1 时机 · L37 · 466
  - SYS-SAVE-FLOW §1.2 安全时刻与落定快照 · L53 · 397
  - SYS-SAVE-FLOW §1.3 写盘与失败 · L59 · 119
  - SYS-SAVE-FLOW §1.4 退出 · L63 · 306
  - SYS-SAVE-FLOW §1.5 保存状态与失败反馈 · L72 · 150
- SYS-SAVE-FLOW §2 加载与一致性 · L76 · 2.5k
  - SYS-SAVE-FLOW §2.1 加载流程 · L78 · 841
  - SYS-SAVE-FLOW §2.2 世界实例：`IWorldState.Instance` · L92 · 358
  - SYS-SAVE-FLOW §2.3 "状态一致"的定义 · L98 · 391
  - SYS-SAVE-FLOW §2.4 天气 · L106 · 103
  - SYS-SAVE-FLOW §2.5 新游戏 · L110 · 302
  - SYS-SAVE-FLOW §2.6 校验修复 · L116 · 451
- SYS-SAVE-FLOW §3 关着游戏的那段现实时间不计 · L131 · 243
- SYS-SAVE-FLOW §4 待验证（P0） · L138 · 104

## SYS-SAVE-FORMAT · 格式、损坏处理与安全

`docs/systems/SYS-SAVE-FORMAT-格式、损坏处理与安全.md` · 4.4k 字 · 存档容器与序列化、格式号与段版本、坏档退回上一代、唯一的设置文件 settings.json、读档输入的安全
关键词：存档容器、序列化、格式号、段版本、存档损坏、设置文件、settings.json、存档安全、缩略图、ReadOnlyReason

- SYS-SAVE-FORMAT §0 结论 · L22 · 644
  - ★ SYS-SAVE-FORMAT §0.1 接入要点 · L30 · 264
- SYS-SAVE-FORMAT §1 格式与版本 · L37 · 1.1k
  - SYS-SAVE-FORMAT §1.1 容器 · L39 · 432
  - SYS-SAVE-FORMAT §1.2 缩略图 · L49 · 166
  - SYS-SAVE-FORMAT §1.3 序列化约定 · L53 · 195
  - SYS-SAVE-FORMAT §1.4 版本 · L59 · 295
- SYS-SAVE-FORMAT §2 存档损坏 · L66 · 560
- SYS-SAVE-FORMAT §3 设置文件：唯一的 `settings.json` · L80 · 716
- SYS-SAVE-FORMAT §4 安全：为什么禁止用 Resource 和 ConfigFile 存读档 · L90 · 543

## SYS-TIME · 时间与季节

`docs/systems/SYS-TIME-时间与季节.md` · 4.7k 字 · 世界状态三个量与只向前的时间规则、默认状态落金初灯、美术太阳与月亮、过渡的定目标顺序与用时、质量底线
关键词：世界状态、钟点、季节、世界时间、世界实例、流逝倍率、落金初灯、默认状态、只向前、美术太阳、美术月亮、满月、定目标顺序、质量底线

- SYS-TIME §0 结论 · L22 · 1.0k
  - ★ SYS-TIME §0.1 接入要点 · L32 · 538
- SYS-TIME §1 世界状态与时间规则 · L42 · 1.4k
  - SYS-TIME §1.1 默认状态 · L44 · 359
  - SYS-TIME §1.2 三个量与时间规则 · L51 · 1.0k
- SYS-TIME §2 美术太阳与月亮 · L65 · 631
  - SYS-TIME §2.1 太阳 · L67 · 370
  - SYS-TIME §2.2 月亮 · L80 · 166
  - SYS-TIME §2.3 与主光的关系 · L84 · 81
- SYS-TIME §3 过渡 · L88 · 211
- SYS-TIME §4 质量底线 · L95 · 444

## SYS-TITLE · 开始页与启动

`docs/systems/SYS-TITLE-开始页与启动.md` · 7.1k 字 · 冷启动扉页与终态失败的出口、开始页（扉页、菜单、片名块）、从双击起的并行加载与预载、各应用状态下的约定、新游戏与继续与返回标题的加载界面、多地图
关键词：开始页、标题页、主菜单、扉页、终态失败、片名块、应用状态、加载界面、进度条、新游戏、继续、返回标题、并行加载、预载

- SYS-TITLE §0 结论 · L22 · 896
  - ★ SYS-TITLE §0.1 接入要点 · L29 · 420
- SYS-TITLE §1 启动序列与扉页 · L38 · 1.1k
- SYS-TITLE §2 开始页 · L59 · 1.3k
  - SYS-TITLE §2.1 扉页 · L61 · 203
  - SYS-TITLE §2.2 菜单 · L67 · 462
  - SYS-TITLE §2.3 片名块 · L75 · 359
  - SYS-TITLE §2.4 首次启动与再次启动 · L83 · 302
- SYS-TITLE §3 应用状态下的约定 · L90 · 2.2k
  - SYS-TITLE §3.1 进入游戏与返回标题 · L105 · 1.4k
- SYS-TITLE §4 多地图 · L120 · 124
- SYS-TITLE §5 不做的事 · L125 · 170
- SYS-TITLE §6 待验证 · L131 · 102

## SYS-VIEW · 观察模式与旁观漫游

`docs/systems/SYS-VIEW-观察模式与旁观漫游.md` · 4.8k 字 · 两种观察模式总述、旁观漫游的操作与活动范围、初始位置、镜头规则、3D 与平面地图的分工
关键词：旁观模式、观察模式、飞行速度档、视场角、鼠标灵敏度、活动范围、初始位置、镜头不会自己动、自动运镜、命令行参数、ICameraRig、Pose

- SYS-VIEW §0 结论 · L22 · 957
  - ★ SYS-VIEW §0.1 接入要点 · L37 · 297
- SYS-VIEW §1 不做的事 · L44 · 70
- SYS-VIEW §2 旁观者漫游 · L48 · 1.9k
  - SYS-VIEW §2.1 操作 · L52 · 460
  - SYS-VIEW §2.2 鼠标与碰撞 · L59 · 149
  - SYS-VIEW §2.3 活动范围 · L64 · 235
  - SYS-VIEW §2.4 初始位置 · L72 · 452
  - SYS-VIEW §2.5 镜头不会自己动 · L81 · 282
  - SYS-VIEW §2.6 操作说明与提示 · L89 · 123
- SYS-VIEW §3 命令行参数 · L97 · 197
- SYS-VIEW §4 环境痕迹与平面地图的分工 · L101 · 231
- SYS-VIEW §5 待验证 · L107 · 68

## SYS-VIEW-MEDIUM · 介质呈现

`docs/systems/SYS-VIEW-MEDIUM-介质呈现.md` · 3.9k 字 · 镜头处在空气、楼里、地下、水下、雾海时的画面与声音规则：介质判定与仲裁、楼体剖切变体、地层雾、水下与半入水、雾海兜底
关键词：介质呈现、介质判定、介质仲裁、剖切变体、剖切色、I0 兜底层、简化室内、完整室内、地层雾、水下、半入水、水下回差、雾海、共用雾函数、IMediumState、IMediumPresenter、CameraMedium、MediumKind

- SYS-VIEW-MEDIUM §0 结论 · L21 · 547
  - ★ SYS-VIEW-MEDIUM §0.1 接入要点 · L29 · 220
- SYS-VIEW-MEDIUM §1 判定、仲裁与渲染原则 · L35 · 659
- SYS-VIEW-MEDIUM §2 楼里 · L45 · 721
- SYS-VIEW-MEDIUM §3 地下 · L55 · 277
- SYS-VIEW-MEDIUM §4 水下 · L63 · 539
- SYS-VIEW-MEDIUM §5 雾海 · L72 · 165
- SYS-VIEW-MEDIUM §6 待验证 · L76 · 78

## SYS-WALK · 行走模式

`docs/systems/SYS-WALK-行走模式.md` · 6.5k 字 · 行走模式的玩家身体与步相、手感目标、第一与第三人称镜头与舒适度、旁观与行走的切换、控制器的规则
关键词：行走模式、手感、模式切换、玩家身体、步相、自动登阶、视角舒适度、视角晃动、第一人称、第三人称、吊杆、身体淡入淡出、控制器规则、运动快照、土狼时间、IViewControl、ModeSwitchVia、ModeSwitchResult、IWalkTelemetry

- SYS-WALK §0 结论 · L22 · 592
  - ★ SYS-WALK §0.1 接入要点 · L34 · 200
- SYS-WALK §1 身体、声音与步相 · L40 · 562
- SYS-WALK §2 手感目标 · L52 · 620
- SYS-WALK §3 视角、镜头与舒适度 · L66 · 1.8k
  - SYS-WALK §3.1 视场角、舒适度与键位 · L68 · 641
  - SYS-WALK §3.2 行走模式的镜头 · L78 · 1.2k
- SYS-WALK §4 模式切换 · L109 · 626
- SYS-WALK §5 控制器的规则 · L118 · 708
- SYS-WALK §6 待验证 · L129 · 293

## SYS-WALK-BIKE · 骑行

`docs/systems/SYS-WALK-BIKE-骑行.md` · 2.2k 字 · 行走里的骑行分支：运动规则、可骑范围与许可、边界与下车、取车还车临停回收与存档
关键词：骑行、共享单车、可骑许可、可骑范围、停放点、取车、还车、临停、回收、下车、制动、后退

- SYS-WALK-BIKE §0 结论 · L21 · 376
  - ★ SYS-WALK-BIKE §0.1 接入要点 · L28 · 153
- SYS-WALK-BIKE §1 骑行 · L34 · 1.1k
  - SYS-WALK-BIKE §1.1 运动 · L38 · 268
  - SYS-WALK-BIKE §1.2 可骑范围 · L44 · 292
  - SYS-WALK-BIKE §1.3 边界与下车 · L50 · 177
  - SYS-WALK-BIKE §1.4 取车、还车、临停与回收 · L55 · 291
- SYS-WALK-BIKE §2 待验证 · L63 · 79

## SYS-WALK-BOUNDS · 可行走范围、边界与碰撞

`docs/systems/SYS-WALK-BOUNDS-可行走范围、边界与碰撞.md` · 8.9k 字 · 可行走范围与禁行区、边界收口与隐形墙、楼内的门与电梯（含自动门运行时）、涉水游泳回位与安全点、行走走廊与路线、碰撞生成与就绪、导航图
关键词：可行走范围、可行走数据、禁行区、高度场地面、边界收口、隐形墙、校界缓停、碰撞顶加高、水平外扩、安全点、回位、涉水、开放泳区、游泳、碰撞就绪、自动门、电梯关门换位、行走走廊、推荐路线、导航图、路线预览、所在空间、碰撞级 C、SurfaceTag、FootMat、WalkClass、Collider、ICollisionWorld

- SYS-WALK-BOUNDS §0 结论 · L22 · 712
  - ★ SYS-WALK-BOUNDS §0.1 接入要点 · L31 · 400
- SYS-WALK-BOUNDS §1 可行走范围 · L41 · 753
- SYS-WALK-BOUNDS §2 边界收口 · L60 · 831
- SYS-WALK-BOUNDS §3 楼内的门与电梯 · L81 · 1.3k
- SYS-WALK-BOUNDS §4 水、游泳、回位与安全点 · L104 · 962
- SYS-WALK-BOUNDS §5 行走走廊与节奏 · L129 · 188
- SYS-WALK-BOUNDS §6 推荐路线 · L135 · 238
- SYS-WALK-BOUNDS §7 碰撞与就绪 · L139 · 1.7k
- SYS-WALK-BOUNDS §8 导航图 · L168 · 455

## SYS-WEATHER · 天气

`docs/systems/SYS-WEATHER-天气.md` · 7.5k 字 · 八格天气与季节合法性、每种天气的画面规则、雨的可读性、闪电彩虹的条件与安全规则、湿润积水降水的规则、质量底线
关键词：天气、天气格、手动天气、天气向量、太阳雨、雨的可读性、闪电、彩虹、降水池、湿润、积水、天光方向、钉住天气、保持晴、天气巡回

- SYS-WEATHER §0 结论 · L22 · 1.1k
  - ★ SYS-WEATHER §0.1 接入要点 · L36 · 579
- SYS-WEATHER §1 天气清单与界面格 · L47 · 630
- SYS-WEATHER §2 每种天气的画面规则 · L64 · 1.7k
- SYS-WEATHER §3 雨的可读性规则 · L91 · 583
- SYS-WEATHER §4 与四季、昼夜的配合 · L103 · 178
- SYS-WEATHER §5 闪电与彩虹 · L110 · 467
- SYS-WEATHER §6 湿润、积水与降水的规则 · L121 · 942
- SYS-WEATHER §7 质量底线 · L135 · 375
- SYS-WEATHER §8 待验证 · L145 · 72

## SYS-WEATHER-PROCESS · 自动天气与存档语义

`docs/systems/SYS-WEATHER-PROCESS-自动天气与存档语义.md` · 4.1k 字 · 自动天气是种子、世界时间、地图的纯函数：片段模板与硬规则、随机数与种子、时间与天气、固定点、存档只存手动与钉住
关键词：自动天气、纯函数、天气种子、计数器子流、不应期、片段模板、保持晴、钉住、固定点、天气段、天气存档、IWeatherProcess、WeatherBoot、WeatherTick

- SYS-WEATHER-PROCESS §0 结论 · L22 · 594
  - ★ SYS-WEATHER-PROCESS §0.1 接入要点 · L31 · 220
- SYS-WEATHER-PROCESS §1 自动天气：前向随机过程 · L37 · 2.2k
  - SYS-WEATHER-PROCESS §1.1 模型 · L39 · 332
  - SYS-WEATHER-PROCESS §1.2 硬规则（生成时满足） · L46 · 214
  - SYS-WEATHER-PROCESS §1.3 各季的气候风味 · L52 · 392
  - SYS-WEATHER-PROCESS §1.4 随机数与复现 · L63 · 310
  - SYS-WEATHER-PROCESS §1.5 时间与天气 · L69 · 555
  - SYS-WEATHER-PROCESS §1.6 固定点 · L80 · 332
- SYS-WEATHER-PROCESS §2 状态与存档语义 · L87 · 372
- SYS-WEATHER-PROCESS §3 待验证 · L93 · 94

## UI-INDEX · 界面总纲

`docs/ui/UI-INDEX-界面总纲.md` · 1.2k 字 · 界面域总纲：界面有什么、怎么操作、长什么样；本域通用原则与文档树
关键词：界面、界面总纲、文档树、UI

- UI-INDEX §0 结论 · L21 · 587
  - ★ UI-INDEX §0.1 接入要点 · L30 · 177
- UI-INDEX §1 文档树 · L35 · 177

## UI-NAV · 界面与操作说明

`docs/ui/UI-NAV-界面与操作说明.md` · 9.2k 字 · 界面有什么、怎么操作：常驻界面与呼出光标、组件的内容与交互、输入优先级与关键键位、无障碍与文本约定；点位与跳转见子文档
关键词：界面交互、快捷键、输入优先级、常驻界面、呼出光标、四角归属、时间环操作、天气签条、速度档、临时提示、到达标题、设置项、无障碍、读屏、文本键、地名来源

- UI-NAV §0 结论 · L21 · 1.0k
  - ★ UI-NAV §0.1 接入要点 · L36 · 459
- UI-NAV §1 允许什么、禁止什么 · L44 · 422
- UI-NAV §2 常驻界面、光标与隐藏 · L53 · 907
- UI-NAV §3 组件的内容与交互 · L72 · 3.3k
  - UI-NAV §3.1 时间环 · L74 · 534
  - UI-NAV §3.2 季节、速度与观察模式 · L83 · 369
  - UI-NAV §3.3 点位目录 · L89 · 218
  - UI-NAV §3.4 到达标题 · L93 · 267
  - UI-NAV §3.5 操作说明与设置 · L97 · 975
  - UI-NAV §3.6 临时提示 · L105 · 450
  - UI-NAV §3.7 天气签条 · L113 · 506
- UI-NAV §4 输入 · L123 · 1.5k
  - UI-NAV §4.1 Esc 与单键快捷键 · L125 · 320
  - UI-NAV §4.2 关键键位 · L131 · 639
  - UI-NAV §4.3 输入优先级 · L151 · 542
- UI-NAV §5 无障碍与语言 · L165 · 832
- UI-NAV §6 待验证 · L178 · 102

## UI-NAV-WAYPOINTS · 点位、跳转与传送

`docs/ui/UI-NAV-WAYPOINTS-点位、跳转与传送.md` · 3.2k 字 · 观景点位的设计意图与合法性，飞过去的方式与到达打断，预取与传送的界面侧约定
关键词：点位、观景点、点位目录编号、waypoints.json、锚点不是点位、推荐状态、进场路径、飞过去、滑移、弧线飞行、到达与打断、传送、跳转压力

- UI-NAV-WAYPOINTS §0 结论 · L21 · 549
  - ★ UI-NAV-WAYPOINTS §0.1 接入要点 · L29 · 183
- UI-NAV-WAYPOINTS §1 点位 · L34 · 812
- UI-NAV-WAYPOINTS §2 跳转与传送 · L45 · 910
  - UI-NAV-WAYPOINTS §2.1 飞过去 · L47 · 220
  - UI-NAV-WAYPOINTS §2.2 到达与打断 · L53 · 232
  - UI-NAV-WAYPOINTS §2.3 预取与速度律豁免 · L58 · 201
  - UI-NAV-WAYPOINTS §2.4 传送与减少动态效果 · L62 · 245
- UI-NAV-WAYPOINTS §3 待验证 · L69 · 93

## UI-VISUAL · 界面视觉语言

`docs/ui/UI-VISUAL-界面视觉语言.md` · 6.5k 字 · 界面外观的根：设计原则、色彩分工与令牌、晶白与深屏两套主题、实现边界；字体形状图标组件布局与动效见子文档
关键词：界面视觉、色彩令牌、清蓝、晶白、深屏、昼夜交班、常驻件、对比度规则、ui.json、ui_night、UiThemeBuilder、UiThemeClock、ui_panel、ui_screen

- UI-VISUAL §0 结论 · L21 · 1.0k
  - ★ UI-VISUAL §0.1 接入要点 · L37 · 280
- UI-VISUAL §1 设计原则 · L42 · 560
- UI-VISUAL §2 色彩分工与常驻件 · L58 · 995
  - UI-VISUAL §2.1 场景暖，界面冷 · L60 · 356
  - UI-VISUAL §2.2 与场景色不冲突的规则 · L69 · 227
  - UI-VISUAL §2.3 高调画面上的面板与常驻件 · L75 · 397
- UI-VISUAL §3 令牌 · L83 · 1.7k
  - UI-VISUAL §3.1 清蓝阶梯 · L87 · 180
  - UI-VISUAL §3.2 中性、专用与语义色 · L91 · 245
  - UI-VISUAL §3.3 两套主题的语义令牌 · L96 · 248
  - UI-VISUAL §3.4 昼夜切换 · L102 · 423
  - UI-VISUAL §3.5 对比度规则 · L110 · 248
  - UI-VISUAL §3.6 登记 · L117 · 155
- UI-VISUAL §4 实现边界 · L121 · 991

## UI-VISUAL-COMPONENTS · 字体、形状、图标、组件与布局

`docs/ui/UI-VISUAL-COMPONENTS-字体、形状、图标、组件与布局.md` · 6.4k 字 · 界面字体与字号、形状与材质、图标体系，组件外观（含时间环、存档卡、加载界面）与画面布局（含大地图、设置卡）
关键词：加载界面、界面字体、思源黑体、字号阶梯、切角、1号脚、发丝线、过孔灯、走线、衍射光边、面板材质、轻度磨砂、图标体系、窗口图标、八角晶片、气象填图符号、通电隐喻、时间环外观、天气签条、速度LED条、存档卡、存储模组、等待灯、画面布局、中央60%、ui_metrics.json、IPaletteProbe

- UI-VISUAL-COMPONENTS §0 结论 · L21 · 493
  - ★ UI-VISUAL-COMPONENTS §0.1 接入要点 · L29 · 177
- UI-VISUAL-COMPONENTS §1 字体与字号 · L34 · 394
- UI-VISUAL-COMPONENTS §2 形状与材质 · L40 · 833
- UI-VISUAL-COMPONENTS §3 图标体系 · L50 · 514
  - UI-VISUAL-COMPONENTS §3.1 规格 · L52 · 163
  - UI-VISUAL-COMPONENTS §3.2 意象 · L56 · 238
  - UI-VISUAL-COMPONENTS §3.3 窗口图标"八角晶片" · L63 · 102
- UI-VISUAL-COMPONENTS §4 组件外观 · L67 · 2.0k
  - UI-VISUAL-COMPONENTS §4.1 六种状态（"通电"隐喻） · L69 · 216
  - UI-VISUAL-COMPONENTS §4.2 控件与容器 · L74 · 320
  - UI-VISUAL-COMPONENTS §4.3 时间环 · L80 · 352
  - UI-VISUAL-COMPONENTS §4.4 天气签条、速度条与到达标题 · L89 · 306
  - UI-VISUAL-COMPONENTS §4.5 存档卡"存储模组"、等待灯与加载界面 · L95 · 793
- UI-VISUAL-COMPONENTS §5 画面布局 · L104 · 936
- UI-VISUAL-COMPONENTS §6 待验证 · L121 · 163

## UI-VISUAL-MOTION · 界面动效与减少动态效果

`docs/ui/UI-VISUAL-MOTION-界面动效与减少动态效果.md` · 3.6k 字 · 界面动效原则、八个母题、频率分级、曲线时长与辉光闪烁上限，减少动态效果下的替代与无障碍
关键词：动效语言、动效母题、M1走线流光、M2扫描揭示、M3网格逐格点亮、M4数值滚动、M5衍射边、M6量化出入场、M7示波器、M8呼吸灯、频率分级、辉光上限、闪烁上限、减少动态效果、MotionPolicy、IMotionSettings、降低透明度、主题锁定

- UI-VISUAL-MOTION §0 结论 · L21 · 484
  - ★ UI-VISUAL-MOTION §0.1 接入要点 · L29 · 148
- UI-VISUAL-MOTION §1 动效语言 · L34 · 1.8k
  - UI-VISUAL-MOTION §1.1 原则与两种时间 · L36 · 321
  - UI-VISUAL-MOTION §1.2 八个母题 · L44 · 653
  - UI-VISUAL-MOTION §1.3 频率分级与声音 · L57 · 325
  - UI-VISUAL-MOTION §1.4 曲线、时长与上限 · L63 · 485
- UI-VISUAL-MOTION §2 减少动态效果与无障碍 · L70 · 490
  - UI-VISUAL-MOTION §2.1 "减少动态效果"下的替代 · L72 · 317
  - UI-VISUAL-MOTION §2.2 无障碍 · L80 · 156

## ENG-INDEX · 工程总纲

`docs/engineering/ENG-INDEX-工程总纲.md` · 1.6k 字 · 工程域总纲：架构、性能、验收、许可、开发工具（内容预览）与扩展路线的通用原则和文档树
关键词：工程、工程总纲、文档树、ENG、工程架构、性能预算、性能工程、硬规则、验收、门禁、许可、内容预览、扩展路线、主线之后

- ENG-INDEX §0 结论 · L21 · 630
  - ★ ENG-INDEX §0.1 接入要点 · L29 · 168
- ENG-INDEX §1 文档树 · L34 · 452

## ENG-ARCH · 工程架构

`docs/engineering/ENG-ARCH-工程架构.md` · 13.7k 字 · 当前分层、依赖方向与唯一生产链，边界、确定性、契约、烘焙与桩的架构准则与后续导向；实现细节以代码为准
关键词：工程架构、分层、依赖方向、命名空间边界、程序集、唯一生产链、KernelStage、组合根、数据流、自动发现、特性注册、Autoload、原生资源创建方、确定性、同机同输入同输出、StableHash、Rng、BannedApiAnalyzers、生命周期、取消与完成、就绪与等待、停滞判据、契约、契约申请、契约分批冻结、枚举穷举、本机烘焙、.tbin、英雄资产、桩、架构导向、net10.0、技术栈

- ENG-ARCH §0 结论 · L21 · 1.8k
  - ★ ENG-ARCH §0.1 接入要点 · L33 · 1.0k
- ENG-ARCH §1 当前架构 · L47 · 1.8k
  - ENG-ARCH 技术栈 · L49 · 337
  - ENG-ARCH 分层 · L53 · 851
  - ENG-ARCH 生产链 · L64 · 487
  - ENG-ARCH 数据流 · L73 · 95
- ENG-ARCH §2 边界与确定性 · L77 · 4.0k
  - ENG-ARCH 依赖方向 · L79 · 1.5k
  - ENG-ARCH 确定性与禁用写法 · L90 · 584
  - ENG-ARCH 自动发现与模块接入 · L102 · 701
  - ENG-ARCH 仓库脚本自成一体 · L110 · 101
  - ENG-ARCH 生命周期与发布 · L114 · 826
  - ENG-ARCH 就绪与等待 · L125 · 261
- ENG-ARCH §3 契约 · L132 · 1.2k
- ENG-ARCH §4 资源与烘焙 · L145 · 2.1k
- ENG-ARCH §5 未交付能力的占位边界 · L161 · 257
- ENG-ARCH §6 架构导向 · L169 · 545
- ENG-ARCH §7 扩展点（RPG） · L177 · 70

## ENG-BUDGET · 性能预算

`docs/engineering/ENG-BUDGET-性能预算.md` · 4.8k 字 · 性能预算的口径：设备档与预设、GPU 帧时分配、份额怎么分与超了怎么办、怎么测；细节级别、速度律与硬规则在子文档
关键词：性能预算、份额、份额分配、budgets.json、KernelBudgets、主会话余量、预算核账、预设资源上限、设备档目标、帧率目标、最低配置、独显中、独显高、楼内视角份额、GPU 帧时分配、阴影图档位、纹理上限

- ENG-BUDGET §0 结论 · L21 · 888
  - ★ ENG-BUDGET §0.1 接入要点 · L35 · 184
- ENG-BUDGET §1 界面、外围、室内与音频的份额 · L41 · 148
- ENG-BUDGET §2 预设与设备档的预算 · L45 · 1.7k
- ENG-BUDGET §3 份额怎么分 · L79 · 442
- ENG-BUDGET §4 怎么测 · L87 · 156
- ENG-BUDGET §5 待验证（P0） · L91 · 253

## ENG-BUDGET-LOD · 细节级别、速度律与渲染参数

`docs/engineering/ENG-BUDGET-LOD-细节级别、速度律与渲染参数.md` · 4.4k 字 · 细节级别 L0+–L4 与 64 m 分块、旁观速度律与目的地预取、深度与近裁剪面、阴影后期倒影灯光的预算参数
关键词：细节级别、L0+、L4、分块、四叉树、屏幕误差、速度律、目的地预取、到达保证、近裁剪面、远裁剪面、反向 Z、坐标精度、PSSM、光柱数量、倒影分辨率、灯盏数

- ENG-BUDGET-LOD §0 结论 · L20 · 668
  - ★ ENG-BUDGET-LOD §0.1 接入要点 · L27 · 330
- ENG-BUDGET-LOD §1 细节级别与分块 · L33 · 1.4k
- ENG-BUDGET-LOD §2 流式加载与速度 · L57 · 485
- ENG-BUDGET-LOD §3 深度与近裁剪面 · L64 · 360
- ENG-BUDGET-LOD §4 阴影、后期、倒影、灯光 · L72 · 594
- ENG-BUDGET-LOD §5 待验证（P0） · L82 · 135

## ENG-BUDGET-RULES · 硬规则与工程设置

`docs/engineering/ENG-BUDGET-RULES-硬规则与工程设置.md` · 6.5k 字 · 违反即不能推送的 23 条硬规则，及 project.godot、渲染器等工程设置
关键词：硬规则、性能规则、白名单着色器、不得新增光源、线程与同步边界、同步读回、GPU 端复制、DECISION-341、全局着色器参数登记、风格底线不可关、存档只走一条路、本机烘焙同机确定、工程设置、渲染器、版本锁定

- ENG-BUDGET-RULES §0 结论 · L21 · 949
  - ★ ENG-BUDGET-RULES §0.1 接入要点 · L27 · 685
- ENG-BUDGET-RULES §1 硬规则（违反任何一条都不能推送） · L38 · 3.5k
  - ENG-BUDGET-RULES §1.1 渲染、材质与时间（第 1–10 条） · L40 · 1.6k
  - ENG-BUDGET-RULES §1.2 烘焙、线程、存档、音频与确定性（第 11–23 条） · L53 · 1.9k
- ENG-BUDGET-RULES §2 工程设置 · L69 · 856

## ENG-FAILS · 游戏失败观测

`docs/engineering/ENG-FAILS-游戏失败观测.md` · 2.1k 字 · 游戏本体开发的失败按根因类别登记：什么算一次、先登记再继续、判类与新增类别、提交窗口、同类反复时的整类排查；类别清单以 failure-types.json 为准，健康页看次数与频率
关键词：游戏失败观测、失败登记、失败类别、fail.py、failure-types.json、根因类别、表现桶、候选类别、提交窗口、逃逸的失败、健康页、整类排查、DECISION-285、DECISION-296

- ENG-FAILS §0 结论 · L21 · 1.3k
  - ★ ENG-FAILS §0.1 接入要点 · L31 · 224

## ENG-GATES · 验收与门禁

`docs/engineering/ENG-GATES-验收与门禁.md` · 5.0k 字 · 测试三档与何时跑、11 项门禁各验什么、入口与交付分工；审查视角与截图、一票否决与收尾清单在子文档
关键词：门禁、验收、测试三档、快检、核心专项、集中测试、gate.ps1、门禁表、门禁编号、DECISION-238、入口与交付分工、测试只锁本模块输出、构建锁

- ENG-GATES §0 结论 · L21 · 937
  - ★ ENG-GATES §0.1 接入要点 · L34 · 338
- ENG-GATES §1 门禁与测试节奏 · L41 · 3.0k
  - ENG-GATES §1.1 测试三档 · L45 · 1.4k
  - ENG-GATES §1.2 门禁表 · L67 · 1.3k
  - ENG-GATES §1.3 入口与交付分工 · L85 · 191

## ENG-GATES-SHOTS · 审查视角与截图

`docs/engineering/ENG-GATES-SHOTS-审查视角与截图.md` · 6.5k 字 · 命令行规则与截图默认串、截图方式、固定机位表；V/E/T 视角表、飞行与行走路线、时季抽样与巡检点轮换（主线之后）
关键词：审查视角、截图默认串、命令行规则、截图方式、截图就绪、流式屏障、录像、视角表、V1、E1、飞行路线、压力路线、点位 P00–P23、行走路线 W1–W14、时季抽样、巡检点轮换、review.json

- ENG-GATES-SHOTS §0 结论 · L21 · 717
  - ★ ENG-GATES-SHOTS §0.1 接入要点 · L29 · 398
- ENG-GATES-SHOTS §1 审查视角 · L35 · 4.7k
  - ENG-GATES-SHOTS §1.1 命令行规则与截图默认串 · L39 · 696
  - ENG-GATES-SHOTS §1.2 截图方式 · L54 · 1.1k
  - ENG-GATES-SHOTS §1.3 固定机位表与审查视角表 · L67 · 1.7k
  - ENG-GATES-SHOTS §1.4 路线与点位（主线之后） · L98 · 786
  - ENG-GATES-SHOTS §1.5 时季抽样（主线之后） · L109 · 350

## ENG-GATES-VETO · 一票否决与收尾清单

`docs/engineering/ENG-GATES-VETO-一票否决与收尾清单.md` · 3.1k 字 · 23 条一票否决（任一条即不通过，系统类条目指向各系统文档）与收尾阶段的全局清单
关键词：一票否决、全局清单、收尾清单、死黑、季节残留、硬切、切图残留、丢档、声音否决、观光规则集判据

- ENG-GATES-VETO §0 结论 · L21 · 346
  - ★ ENG-GATES-VETO §0.1 接入要点 · L27 · 85
- ENG-GATES-VETO §1 一票否决 · L31 · 1.4k
- ENG-GATES-VETO §2 全局清单 · L59 · 484

## ENG-LICENSE · 许可、署名与本机目录

`docs/engineering/ENG-LICENSE-许可、署名与本机目录.md` · 3.3k 字 · 只从项目运行、不打包不设版本号，用户目录与日志，各部分许可与插件清单，OSM 署名、窗口标题与图标
关键词：许可、署名、本机目录、不打包、不发布、不设版本号、用户目录、%APPDATA%\UESTC、日志、MIT、ODbL、third_party.json、插件清单、第三方许可白名单、OSM 署名、窗口标题、窗口图标、八角晶片

- ENG-LICENSE §0 结论 · L21 · 605
  - ★ ENG-LICENSE §0.1 接入要点 · L28 · 325
- ENG-LICENSE §1 用户目录与日志 · L36 · 391
- ENG-LICENSE §2 许可 · L42 · 1.1k
- ENG-LICENSE §3 署名、窗口标题与图标 · L59 · 395

## ENG-PERF · 性能工程

`docs/engineering/ENG-PERF-性能工程.md` · 6.1k 字 · 性能总原则与绘制管线：容器与合批、着色器白名单与全局参数、预热、填充率、剔除与图层
关键词：着色器白名单、全局着色器参数、ShaderGlobals、数据纹理、合批、自动实例化、MultiMesh、容器选型、管线预热、抖动淡化、级别过渡、填充率、遮挡剔除、渲染图层、顶点格式、D3D12

- ENG-PERF §0 结论 · L21 · 1.3k
  - ★ ENG-PERF §0.1 接入要点 · L38 · 466
- ENG-PERF §1 绘制管线与合批 · L48 · 1.8k
  - ENG-PERF §1.1 容器选型 · L50 · 344
  - ENG-PERF §1.2 MultiMesh、网格 LOD 与级别过渡 · L57 · 389
  - ENG-PERF §1.3 调用口径 · L63 · 123
  - ENG-PERF §1.4 着色器白名单 · L67 · 482
  - ENG-PERF §1.5 全局参数与数据纹理 · L76 · 476
- ENG-PERF §2 着色器与编译 · L85 · 779
- ENG-PERF §3 填充率与带宽 · L95 · 251
- ENG-PERF §4 剔除 · L101 · 496
- ENG-PERF §5 待验证（P0） · L107 · 166

## ENG-PERF-ADAPT · 自适应与帧节奏

`docs/engineering/ENG-PERF-ADAPT-自适应与帧节奏.md` · 2.0k 字 · 运行时自适应的降级阶梯与控制律、帧节奏与画面设置的生效约束
关键词：自适应、自动调节画质、降级阶梯、控制律、帧节奏、跟随刷新率、软件上限、情境限帧、画面设置生效、静止降频

- ENG-PERF-ADAPT §0 结论 · L21 · 444
  - ★ ENG-PERF-ADAPT §0.1 接入要点 · L27 · 163
- ENG-PERF-ADAPT §1 自适应与帧节奏 · L32 · 814
  - ENG-PERF-ADAPT §1.1 运行时自适应（主线之后） · L34 · 428
  - ENG-PERF-ADAPT §1.2 帧节奏与画面设置约束 · L43 · 372

## ENG-PERF-DATA · 性能数据与分析

`docs/engineering/ENG-PERF-DATA-性能数据与分析.md` · 11.7k 字 · 性能记录的四层与写入方、记录开关与会话寿命、原始流与时间基、指标口径与非稳态帧、测量范围、异常、比较键与保留、开销目标
关键词：性能记录、记录性能数据、记录服务、性能会话、六件、原始流、perf-fields.json、schema、calc、指标口径、体验口径、成本口径、五段、B0–B4、主线程周期、受测视口、受测视口 GPU 合计、成本尖峰、非稳态帧、掉帧、卡顿分级、1% low、scope、性能异常、比较键、性能数据保留、分析器

- ENG-PERF-DATA §0 结论 · L21 · 1.3k
  - ★ ENG-PERF-DATA §0.1 接入要点 · L34 · 451
- ENG-PERF-DATA §1 四层与写入方 · L43 · 1.3k
  - ENG-PERF-DATA §1.1 四层 · L45 · 449
  - ENG-PERF-DATA §1.2 记录开关与会话 · L53 · 593
  - ENG-PERF-DATA §1.3 线程分工 · L63 · 242
- ENG-PERF-DATA §2 原始流与时间基 · L70 · 1.5k
  - ENG-PERF-DATA §2.1 六件与存放 · L72 · 992
  - ENG-PERF-DATA §2.2 字段、版本与时间基 · L91 · 518
- ENG-PERF-DATA §3 指标口径 · L99 · 2.8k
  - ENG-PERF-DATA §3.1 体验口径 · L108 · 861
  - ENG-PERF-DATA §3.2 成本口径 · L123 · 1.4k
- ENG-PERF-DATA §4 测量范围与 `scope` · L138 · 247
- ENG-PERF-DATA §5 异常 · L146 · 2.2k
  - ENG-PERF-DATA §5.1 体验类 · L154 · 788
  - ENG-PERF-DATA §5.2 成本与数据类 · L170 · 837
  - ENG-PERF-DATA §5.3 归因与合并 · L184 · 365
- ENG-PERF-DATA §6 比较键、状态与保留 · L194 · 991
- ENG-PERF-DATA §7 开销目标 · L213 · 290

## ENG-PERF-LIGHT · 植被、阴影、后期与灯光

`docs/engineering/ENG-PERF-LIGHT-植被、阴影、后期与灯光.md` · 4.2k 字 · 植被分级与风、飘落池、投影代理；阴影量化与日月交接的做法；后期链顺序、描线、抗锯齿；窗灯路灯通道与倒影
关键词：植被分级、树冠云、风三层、ShadowsOnly 代理、投影物、方向量化、太阳贴地、退光线、日月交接、后期链、Compositor、描线材质 ID、CDL、LUT、灯光通道、路灯光斑、倒影视口

- ENG-PERF-LIGHT §0 结论 · L21 · 546
  - ★ ENG-PERF-LIGHT §0.1 接入要点 · L28 · 214
- ENG-PERF-LIGHT §1 植被 · L33 · 667
- ENG-PERF-LIGHT §2 阴影、光照与后期 · L42 · 1.9k
  - ENG-PERF-LIGHT §2.1 阴影 · L44 · 668
  - ENG-PERF-LIGHT §2.2 后期链 · L55 · 651
  - ENG-PERF-LIGHT §2.3 窗灯与路灯 · L63 · 411
  - ENG-PERF-LIGHT §2.4 倒影 · L69 · 165
- ENG-PERF-LIGHT §3 待验证（P0） · L73 · 128

## ENG-PERF-MEASURE · 测量与调优

`docs/engineering/ENG-PERF-MEASURE-测量与调优.md` · 7.1k 字 · 性能测量的机器与剖析权限、AI 采集批次与场景套、以最新数据为准、诊断档与深挖、不采用的工具、调优轮次的长期规则与判停
关键词：性能测量、AI 批次、run.ps1、perf.py、perf.py latest、固定机位套、路线套、传送套、采样前稳定、A/B/A、作废、污染标记、以最新为准、诊断档、deepen、采集条件标记、调优轮次、第 0 轮、距离表、必判集、判停、结束原因、单边判、隔离桌面、剖析权限

- ENG-PERF-MEASURE §0 结论 · L21 · 861
  - ★ ENG-PERF-MEASURE §0.1 接入要点 · L31 · 345
- ENG-PERF-MEASURE §1 机器与剖析权限 · L39 · 526
- ENG-PERF-MEASURE §2 AI 批次 · L48 · 1.5k
- ENG-PERF-MEASURE §3 以最新为准 · L62 · 319
- ENG-PERF-MEASURE §4 诊断档与 `deepen` · L69 · 821
- ENG-PERF-MEASURE §5 不采用的工具 · L82 · 606
- ENG-PERF-MEASURE §6 调优规则与判停 · L98 · 1.6k
  - ENG-PERF-MEASURE §6.1 第 0 轮 · L102 · 492
  - ENG-PERF-MEASURE §6.2 第 1–3 轮 · L112 · 327
  - ENG-PERF-MEASURE §6.3 判停与收尾 · L119 · 658

## ENG-PERF-STREAM · 流式加载、本机烘焙与内存

`docs/engineering/ENG-PERF-STREAM-流式加载、本机烘焙与内存.md` · 4.0k 字 · 本机烘焙与缓存、帧内请求与六条不变量、碰撞级与室内包、目的地预取的做法，主线程与 C#，内存显存，磁盘布局
关键词：TileBundle、流式加载、本机烘焙缓存、tbin、六条不变量、看门狗、碰撞级、室内包、目的地预取、零分配、GC、显存记账、磁盘布局、不泄漏

- ENG-PERF-STREAM §0 结论 · L21 · 518
  - ★ ENG-PERF-STREAM §0.1 接入要点 · L28 · 217
- ENG-PERF-STREAM §1 流式加载 · L34 · 1.8k
  - ENG-PERF-STREAM §1.1 本机烘焙、缓存与加载 · L36 · 586
  - ENG-PERF-STREAM §1.2 帧内请求、挂载与六条不变量 · L44 · 564
  - ENG-PERF-STREAM §1.3 碰撞级与室内包 · L57 · 364
  - ENG-PERF-STREAM §1.4 外围与目的地预取 · L61 · 324
- ENG-PERF-STREAM §2 主线程与 C# · L65 · 315
- ENG-PERF-STREAM §3 内存与显存 · L71 · 271
- ENG-PERF-STREAM §4 磁盘布局 · L75 · 128
- ENG-PERF-STREAM §5 待验证（P0） · L79 · 79

## ENG-PREVIEW · 内容预览

`docs/engineering/ENG-PREVIEW-内容预览.md` · 2.6k 字 · 开发时预览与回看自己做的内容：用 --pose 放机位、预览只读不写、改了以后重启回看；按实体定位与机位恢复在主线之后
关键词：内容预览、预览、回看、机位、--pose、预览只读不写、同图重载、按实体定位、机位恢复、开发工具

- ENG-PREVIEW §0 结论 · L21 · 668
  - ★ ENG-PREVIEW §0.1 接入要点 · L28 · 380
- ENG-PREVIEW §1 启动 · L36 · 168
- ENG-PREVIEW §2 改了以后怎么回看 · L43 · 256
- ENG-PREVIEW §3 主线之后 · L53 · 298

## ENG-ROADMAP · 扩展路线

`docs/engineering/ENG-ROADMAP-扩展路线.md` · 3.4k 字 · 观光到 RPG 的扩展原则、现在不能写死的地方、写法纪律和 6 条最小预留
关键词：扩展路线、从观光到 RPG、扩展原则、规则集、sightseeing、观光规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位、RPG 预留目录、实体 ID 前缀、存档预留段名、最小预留、写法纪律

- ENG-ROADMAP §0 结论 · L21 · 813
  - ★ ENG-ROADMAP §0.1 接入要点 · L30 · 420
- ENG-ROADMAP §1 扩展原则 · L38 · 580
- ENG-ROADMAP §2 写死的规定与写法纪律 · L49 · 374
- ENG-ROADMAP §3 最小预留 · L55 · 843
  - ENG-ROADMAP §3.1 清单 · L57 · 695
  - ENG-ROADMAP §3.2 不需要现在预留的 · L68 · 137

## WF-INDEX · 协作总纲

`docs/workflow/WF-INDEX-协作总纲.md` · 1.0k 字 · 协作域总纲：两个指挥和子代理怎么干活：入口、守则、施工边界、流程、通信、角色、模型与队列
关键词：协作、协作总纲、文档树、WF、全员守则、施工队列、子代理、主会话

- WF-INDEX §0 结论 · L21 · 393
  - ★ WF-INDEX §0.1 接入要点 · L27 · 191
- WF-INDEX §1 文档树 · L32 · 181

## WF-BUILD · 施工边界

`docs/workflow/WF-BUILD-施工边界.md` · 4.1k 字 · 施工时能改哪里、共享文件、契约申请与冻结、构建与测试排队、新模块怎么接入
关键词：独占目录、共享文件、契约申请、契约冻结、构建锁、构建与测试排队、.uid、特性接入、paths、G3

- WF-BUILD §0 结论 · L22 · 854
  - ★ WF-BUILD §0.1 接入要点 · L31 · 415
- WF-BUILD §1 改哪里 · L40 · 493
- WF-BUILD §2 G4 拦不住的约定在哪 · L48 · 144
- WF-BUILD §3 契约申请与冻结 · L52 · 338
- WF-BUILD §4 构建与测试排队 · L59 · 832

## WF-COMMS · 交接与通信

`docs/workflow/WF-COMMS-交接与通信.md` · 7.1k 字 · .collab 交接目录与文件布局，命名与写法，任务单与请求文件的格式，契约申请短头部，完成回报格式，来信与消息，Codex 会话与引导信箱，作者观察台
关键词：.collab、交接目录、文件布局、结案即删、任务单、旁路单、调研目录、契约申请、评审请求、六行短头部、完成回报、回报格式、文件命名、来信、收件箱、一条一个文件、【需定】【待审】【知会】、Codex 会话、引导信箱、steer、HANDOVER.md、DECISION-207、DECISION-208、DECISION-228、作者观察台、dashboard、post.py、审计项、通知

- WF-COMMS §0 结论 · L22 · 719
  - ★ WF-COMMS §0.1 接入要点 · L32 · 205
- WF-COMMS §1 交接目录与文件 · L39 · 1.2k
- WF-COMMS §2 命名与写法 · L61 · 398
- WF-COMMS §3 任务单、请求文件、契约申请与完成回报 · L68 · 1.0k
  - WF-COMMS §3.1 施工单与旁路单 · L70 · 247
  - WF-COMMS §3.2 请求文件 · L75 · 50
  - WF-COMMS §3.3 契约申请的短头部（DECISION-201） · L79 · 178
  - WF-COMMS §3.4 完成回报 · L89 · 513
- WF-COMMS §4 来信与消息 · L108 · 1.5k
  - WF-COMMS §4.1 什么时候发 · L110 · 260
  - WF-COMMS §4.2 一条来信一个新文件（DECISION-207） · L119 · 272
  - WF-COMMS §4.3 三级标记与写法 · L125 · 446
  - WF-COMMS §4.4 来信与即时送达 · L133 · 530
- WF-COMMS §5 Codex 会话与引导信箱 · L147 · 276
- WF-COMMS §6 作者观察台 · L153 · 991

## WF-ENTRY · 角色入口卡

`docs/workflow/WF-ENTRY-角色入口卡.md` · 4.6k 字 · 每个角色的入口卡：身份与回报对象、开工动作、本角色独有的必守、做某事前读哪一节；由钩子按节标题注入
关键词：角色入口卡、入口卡、主会话、子代理、Codex 主线、重置后先读、开工动作、收尾、做 X 前读 Y、钩子注入、SessionStart、SubagentStart、引擎路径、UESTC_GODOT

- WF-ENTRY §0 结论 · L21 · 384
  - ★ WF-ENTRY §0.1 接入要点 · L27 · 127
- WF-ENTRY §1 主会话 · L33 · 1.1k
- WF-ENTRY §2 子代理 · L59 · 1.2k
- WF-ENTRY §3 Codex 主线 · L87 · 1.2k

## WF-FLOW · 开发流程

`docs/workflow/WF-FLOW-开发流程.md` · 6.4k 字 · 一项任务从派工、施工（在临时工作树里写，交回前跑 G1 与受影响 G2）、统一编译推送、回报到评审的流程，防打架，按范围拆单，可调参数集中放
关键词：开发流程、派工、施工单、按范围拆单、回报、评审、防打架、邻居、可调参数、DECISION-184、DECISION-211、评审通用结论

- WF-FLOW §0 结论 · L22 · 526
  - ★ WF-FLOW §0.1 接入要点 · L29 · 187
- WF-FLOW §1 全流程一览 · L35 · 616
- WF-FLOW §2 派工与拆单 · L46 · 1.4k
  - WF-FLOW §2.1 防打架（DECISION-211） · L62 · 367
- WF-FLOW §3 施工与回报 · L71 · 2.4k
  - WF-FLOW §3.1 统一编译（DECISION-219） · L77 · 2.1k
- WF-FLOW §4 评审：审美与契约的交叉评审 · L90 · 702

## WF-MODEL · 模型、子代理与额度

`docs/workflow/WF-MODEL-模型、子代理与额度.md` · 8.6k 字 · 按七问与难度选施工模型和推理强度，怎么开子代理、写简报，额度、并发与冻结，两家模型的易犯行为
关键词：七问、选模型、Opus 与 Sonnet 的分界、机械活、gpt-6-luna 子代理、推理强度、升级先找原因、简单少量的活直接做、子代理、Agent 工具、spawn_agent、子代理生命周期、默认新开、简报模板、接续段、额度、5 小时窗口、每周额度、并发、额度刹车、get_usage、临近重置用足、Codex 额度、额度见底、冻结、切换账号、易犯行为、DECISION-184、DECISION-191、DECISION-226

- WF-MODEL §0 结论 · L22 · 1.1k
  - ★ WF-MODEL §0.1 接入要点 · L35 · 282
- WF-MODEL §1 按七问选施工模型 · L43 · 843
- WF-MODEL §2 Opus 与 Sonnet 的分界（DECISION-193） · L57 · 440
- WF-MODEL §3 机械活交谁 · L70 · 556
- WF-MODEL §4 推理强度按难度选 · L79 · 679
- WF-MODEL §5 升级、派单与直接做 · L94 · 424
- WF-MODEL §6 怎么开子代理 · L101 · 726
- WF-MODEL §7 子代理的生命周期：默认新开，靠任务单交接 · L109 · 451
- WF-MODEL §8 简报模板与"接续"段 · L119 · 697
  - WF-MODEL §8.1 简报模板 · L121 · 402
  - WF-MODEL §8.2 "接续"段 · L136 · 278
- WF-MODEL §9 额度与并发 · L146 · 1.0k
  - WF-MODEL §9.1 口径、开多少与刹车 · L148 · 365
  - WF-MODEL §9.2 节奏（DECISION-196） · L156 · 214
  - WF-MODEL §9.3 Codex 账号 · L162 · 204
  - WF-MODEL §9.4 额度见底时冻结 · L167 · 206
- WF-MODEL §10 两家模型的易犯行为与对应做法 · L171 · 805

## WF-ROLE · 参与者与分工

`docs/workflow/WF-ROLE-参与者与分工.md` · 5.4k 字 · 角色表与汇报关系，会话的运行方式，各模型的长处，GPT 模型不做审美，决定权与事归谁，总指挥只做什么，Codex 主线当指挥
关键词：角色表、汇报给、决定权、执行人、会话、运行方式、单 Claude 账户、模型能力、长处、Opus、Sonnet、gpt-6-astra、gpt-6.1-sol、GPT 不做审美、视觉风格归属、事归谁、交叉、总指挥、主会话、风格负责人、集中测试、Codex 主线、工程指挥、DECISION-179

- WF-ROLE §0 结论 · L22 · 606
  - ★ WF-ROLE §0.1 接入要点 · L29 · 225
- WF-ROLE §1 角色与运行方式 · L35 · 652
- WF-ROLE §2 模型的长处与短板 · L49 · 523
- WF-ROLE §3 GPT 模型不做审美工作 · L60 · 704
- WF-ROLE §4 决定权与事归谁 · L68 · 752
- WF-ROLE §5 总指挥只做什么 · L77 · 656
- WF-ROLE §6 Codex 主线当指挥，施工默认交 gpt-6.1-sol 子代理 · L90 · 789

## WF-RULE · 全员守则

`docs/workflow/WF-RULE-全员守则.md` · 11.3k 字 · 所有会话和子代理都要守的规则：不过度工程化、不干等、文件操作、git 与直接改主线、主线优先、测试最小、少打架、一次性产物、bug 分级、精度按 demo 阶段定
关键词：全员守则、不过度工程化、不向作者出选项、不预判时间、不短轮询、不干等、不重复复核、文件操作、删除规则、git、直接改主线、只提交自己的文件、出了问题向前修不回滚、不 stash、主线优先、测试最小、少和别人打架、一次迁完、拿不准就问、一次性产物、用完就删、bug 分级、阻断、先提交后修、修不收敛就停、数量级判断、demo 阶段、精度与画面相称

- WF-RULE §0 结论 · L22 · 140
  - ★ WF-RULE §0.1 接入要点 · L26 · 59
- WF-RULE §1 不过度工程化（DECISION-201） · L30 · 453
- WF-RULE §2 不向作者出选项 · L38 · 380
- WF-RULE §3 不预判时间 · L45 · 303
- WF-RULE §4 不短轮询、不干等、不重复复核 · L52 · 290
- WF-RULE §5 来信 · L58 · 40
- WF-RULE §6 文件操作 · L62 · 564
- WF-RULE §7 git 与直接改主线（DECISION-211） · L72 · 1.1k
- WF-RULE §8 主线优先（DECISION-168） · L84 · 327
- WF-RULE §9 测试最小（DECISION-167） · L91 · 531
- WF-RULE §10 少和别人打架（DECISION-199） · L100 · 1.4k
- WF-RULE §11 拿不准就问 · L118 · 137
- WF-RULE §12 写文档与记忆 · L123 · 71
- WF-RULE §13 一次性产物（DECISION-221） · L127 · 692
- WF-RULE §14 按严重程度修 bug（DECISION-223） · L136 · 1.3k
- WF-RULE §15 精度按 demo 阶段定（DECISION-225、DECISION-231、DECISION-290） · L151 · 1.1k
- WF-RULE §16 改文件即维护体系（DECISION-264） · L161 · 1.2k

## WF-STATUS · 施工状态、队列与调研入队

`docs/workflow/WF-STATUS-施工状态、队列与调研入队.md` · 6.4k 字 · 施工状态文件怎么填、阶段与主线闸口、施工队列的格式与即时维护、队列平衡、交接单、调研入队流水线与旁路单
关键词：阶段、主线闸口、P0–P3、施工队列、一行一件、即时维护、拆分重活、等前置、就绪、进行中、待集成、暂停、行号、施工状态、状态文件、当前 main、最近可运行入口、已冻结的契约组、队列平衡、积压、交接单、HANDOVER.md、接手、已集成、调研入队流水线、审计、交叉审核、调研简报、旁路单、DECISION-224、DECISION-324

- WF-STATUS §0 结论 · L22 · 682
  - ★ WF-STATUS §0.1 接入要点 · L31 · 260
- WF-STATUS §1 施工状态文件怎么填 · L38 · 969
  - WF-STATUS §1.1 阶段与闸口 · L46 · 534
- WF-STATUS §2 施工队列与队列平衡 · L56 · 2.0k
- WF-STATUS §3 交接单 · L94 · 89
- WF-STATUS §4 调研入队流水线 · L98 · 2.0k
  - WF-STATUS §4.1 调研与旁路单 · L114 · 747

## STD-INDEX · 规范总纲

`docs/standards/STD-INDEX-规范总纲.md` · 938 字 · 规范域总纲：写文档、写代码，以及命名、坐标与单位等跨文档约定的规范，对所有角色、文档和代码生效
关键词：规范、规范总纲、文档树、STD、文档写法、代码结构、坐标单位与命名约定

- STD-INDEX §0 结论 · L21 · 362
  - ★ STD-INDEX §0.1 接入要点 · L27 · 181
- STD-INDEX §1 文档树 · L32 · 99

## STD-CODE · 代码结构准则

`docs/standards/STD-CODE-代码结构准则.md` · 8.4k 字 · 写代码时的结构规矩：先用 Godot 原生、依赖方向落地、规则值与数据各一份、失败交到底、控制规模、测试写法、异步与线程，以及 Godot API 先查签名
关键词：代码结构准则、先用原生、重复造轮子、判断次序、失败交付、结果载体、保留原异常、异步、任务有主、async void、线程边界、依赖方向、内核引用系统、生成层实现类、点名模块、单一来源、规则值、规则类、数据与代码分离、Tune 只放规则值、实体 ID 写数据、复用、公共层、死代码、死桩、诊断残留、单实现接口、吞错、兜底、文件规模、构造参数、清单文件、程序集过碎、测试写法、测试夹具、假实现、性质断言、signatures.txt、Godot API、GDScript

- STD-CODE §0 结论 · L21 · 1.4k
  - ★ STD-CODE §0.1 接入要点 · L31 · 1.0k
- STD-CODE §1 依赖方向 · L46 · 351
- STD-CODE §2 规则值与数据各一份 · L55 · 590
- STD-CODE §3 先用现成，再复用，不复制 · L65 · 1.7k
- STD-CODE §4 不留死代码，不写补丁 · L79 · 1.3k
- STD-CODE §5 规模 · L93 · 269
- STD-CODE §6 测试代码怎么写 · L99 · 1.1k
- STD-CODE §7 异步与线程 · L113 · 437

## STD-CONVENTIONS · 坐标单位与命名约定

`docs/standards/STD-CONVENTIONS-坐标单位与命名约定.md` · 10.2k 字 · 跨文档共同约定：坐标与单位、方位角、时间写法，编号体系与命名（地图与实体 ID、色名、枚举纪律、持久化 ID、文件与编号格式）、确定性原则、数据路径
关键词：约定、坐标系、单位、角度、方位角、时间写法、编号体系、命名、地图 ID、实体 ID、色名、枚举纪律、持久化 ID、文件命名、提交说明格式、确定性、数据路径

- STD-CONVENTIONS §0 结论 · L22 · 805
  - ★ STD-CONVENTIONS §0.1 接入要点 · L32 · 422
- STD-CONVENTIONS §1 坐标与单位 · L42 · 1.1k
  - STD-CONVENTIONS §1.1 坐标系 · L44 · 497
  - STD-CONVENTIONS §1.2 角度 · L52 · 366
  - STD-CONVENTIONS §1.3 其他单位 · L60 · 267
- STD-CONVENTIONS §2 时间写法 · L68 · 495
- STD-CONVENTIONS §3 标识与命名 · L77 · 6.3k
  - STD-CONVENTIONS §3.1 地图、实体与世界文件 · L79 · 846
  - STD-CONVENTIONS §3.2 编号体系 · L93 · 892
  - STD-CONVENTIONS §3.3 同号不同义的读法 · L115 · 696
  - STD-CONVENTIONS §3.4 命名写法 · L128 · 1.2k
  - STD-CONVENTIONS §3.5 色名 · L149 · 364
  - STD-CONVENTIONS §3.6 枚举与扩展纪律 · L156 · 569
  - STD-CONVENTIONS §3.7 持久化 ID 的稳定 · L167 · 423
  - STD-CONVENTIONS §3.8 文件与编号的命名格式 · L176 · 1.3k
- STD-CONVENTIONS §4 确定性 · L196 · 214
- STD-CONVENTIONS §5 路径 · L203 · 186

## STD-DOC · 文档写法

`docs/standards/STD-DOC-文档写法.md` · 11.5k 字 · 文档的分层与四级文档树、每份文档的形状（文件头、标题、范围块）、编码与引用、只写什么不写什么、体量上限、冲突层级、新增与维护
关键词：文档写法、分层、文档树、域总纲、主题文档、子文档、信息地图、渐进式披露、顶层规则、接入要点、文件头、范围块、查阅行、简题、编码、文件名、小节编号、引用写法、全局替换、只写现行规则、不写实现、一份权威、体量上限、冲突层级、规则放哪份、新增文档、登记决策、gen-index、check-docs、SECTIONS.md

- STD-DOC §0 结论 · L21 · 1.1k
  - ★ STD-DOC §0.1 接入要点 · L31 · 458
- STD-DOC §1 分层与文档树 · L40 · 1.1k
  - STD-DOC §1.1 上下文分层 · L42 · 797
  - STD-DOC §1.2 文档树 · L55 · 244
- STD-DOC §2 文档形状 · L60 · 1.4k
- STD-DOC §3 编码、小节编号与引用 · L89 · 1.4k
- STD-DOC §4 写什么，怎么写 · L113 · 1.5k
  - STD-DOC §4.1 取舍 · L115 · 590
  - STD-DOC §4.2 写法（DECISION-220） · L124 · 930
- STD-DOC §5 体量上限 · L139 · 1.8k
  - STD-DOC §5.1 长大了怎么拆（DECISION-222） · L159 · 577
  - STD-DOC §5.2 新内容放哪 · L166 · 306
- STD-DOC §6 冲突与规则放哪 · L174 · 895
- STD-DOC §7 新增文档 · L206 · 300
- STD-DOC §8 登记新决策 · L212 · 218
- STD-DOC §9 维护 · L218 · 921

## REG-INDEX · 登记总纲

`docs/registry/REG-INDEX-登记总纲.md` · 934 字 · 登记域总纲：按编号或按词查的登记：决策与术语
关键词：登记、登记总纲、文档树、REG、决策、术语表、决策号

- REG-INDEX §0 结论 · L21 · 342
  - ★ REG-INDEX §0.1 接入要点 · L27 · 170
- REG-INDEX §1 文档树 · L32 · 178

## REG-DECISIONS · 决策登记

`docs/registry/REG-DECISIONS-决策登记.md` · 1.7k 字 · 全项目已定决策登记的入口：编号、状态、细则所在的写法与归卷规则；决策本体分世界与画面、系统与工程、协作与流程三卷
关键词：决策、决策号、DECISION、决策登记、生效、待原型验证、细则所在、推翻决策、归卷、新增决策、登记新决策

- REG-DECISIONS §0 用法 · L21 · 951
  - ★ REG-DECISIONS §0.1 接入要点 · L37 · 181

## REG-DECISIONS-PROCESS · 协作与流程决策

`docs/registry/REG-DECISIONS-PROCESS-协作与流程决策.md` · 19.7k 字 · 计划、协作、规范、登记四个域的已定决策：协作、流程、文档与代码规范、门禁流程
关键词：决策、流程、协作、文档规则、门禁流程、派工、主会话、子代理

- REG-DECISIONS-PROCESS §0 结论 · L21 · 129
  - ★ REG-DECISIONS-PROCESS §0.1 接入要点 · L25 · 56
- REG-DECISIONS-PROCESS §1 流程 · L29 · 19.1k

## REG-DECISIONS-SYSTEMS · 系统与工程决策

`docs/registry/REG-DECISIONS-SYSTEMS-系统与工程决策.md` · 27.9k 字 · 界面、存档、音频、性能与画面设置、工程五个领域的已定决策
关键词：决策、系统、工程、界面、存档、音频、性能、画面设置、预算、契约

- REG-DECISIONS-SYSTEMS §0 结论 · L21 · 126
  - ★ REG-DECISIONS-SYSTEMS §0.1 接入要点 · L25 · 56
- REG-DECISIONS-SYSTEMS §1 界面 · L29 · 4.6k
- REG-DECISIONS-SYSTEMS §2 存档 · L65 · 1.9k
- REG-DECISIONS-SYSTEMS §3 音频 · L85 · 2.2k
- REG-DECISIONS-SYSTEMS §4 性能与画面设置 · L107 · 9.5k
- REG-DECISIONS-SYSTEMS §5 工程 · L166 · 5.5k
- REG-DECISIONS-SYSTEMS §6 地图与行走 · L202 · 1.0k
- REG-DECISIONS-SYSTEMS §7 时间、季节、天气 · L213 · 2.4k

## REG-DECISIONS-WORLD · 世界与画面决策

`docs/registry/REG-DECISIONS-WORLD-世界与画面决策.md` · 10.3k 字 · 作品、画面、世界、地图四个域的已定决策：作品定位与范围、世界与地图、美术风格
关键词：决策、世界、画面、作品定位、地图、美术风格、视觉精度、翻新、日式街景、不变量

- REG-DECISIONS-WORLD §0 结论 · L21 · 129
  - ★ REG-DECISIONS-WORLD §0.1 接入要点 · L25 · 56
- REG-DECISIONS-WORLD §1 作品定位与范围 · L29 · 2.3k
- REG-DECISIONS-WORLD §2 世界与地图 · L50 · 4.4k
- REG-DECISIONS-WORLD §3 美术风格 · L81 · 2.9k

## REG-GLOSSARY · 术语表

`docs/registry/REG-GLOSSARY-术语表.md` · 21.8k 字 · 文档里专有说法的统一解释，按领域分组，每条一两句并给出细则所在的小节
关键词：术语、术语表、名词解释、同名异义、缩写、别名

- REG-GLOSSARY §0 用法 · L21 · 261
  - ★ REG-GLOSSARY §0.1 接入要点 · L27 · 92
- REG-GLOSSARY §1 作品与默认状态 · L32 · 766
- REG-GLOSSARY §2 世界状态与时间 · L46 · 1.3k
- REG-GLOSSARY §3 天气 · L69 · 1.3k
- REG-GLOSSARY §4 美术与光照 · L91 · 1.3k
- REG-GLOSSARY §5 箱庭、地形、边界与外围 · L113 · 1.5k
- REG-GLOSSARY §6 室内与门 · L138 · 981
- REG-GLOSSARY §7 观察模式、行走与跳转 · L154 · 1.4k
- REG-GLOSSARY §8 平面地图与传送 · L178 · 932
- REG-GLOSSARY §9 开始页与存档 · L195 · 1.1k
- REG-GLOSSARY §10 多地图 · L214 · 620
- REG-GLOSSARY §11 界面 · L226 · 1.4k
- REG-GLOSSARY §12 音频 · L253 · 1.1k
- REG-GLOSSARY §13 画面设置与性能 · L274 · 2.9k
- REG-GLOSSARY §14 工程与流程 · L316 · 2.4k
- REG-GLOSSARY §15 同名异义 · L353 · 2.0k
