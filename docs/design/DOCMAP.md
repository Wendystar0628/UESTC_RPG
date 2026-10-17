# 文档地图与工作包矩阵

> 由 `scripts/docs/gen_index.py` 生成，不要手改。开发入口见 [总索引](README.md)，小节定位见 [SECTIONS.md](SECTIONS.md)。

## 1 文档地图

### 1.1 共识层 `core/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [CORE-CONTENT](core/CORE-CONTENT-开发内容核心准则.md) 开发内容核心准则 | 写代码、做内容时要守的核心准则：开发方式（不过度工程化、测试最小、改设计必清旧、架构）、开发流程、规范约定与内容底线，一条一句，细则见各条指向；钩子在子代理开工、首次碰内容路径时注入 §0，之后定期重注 | WP-00 | G4 |
| [CORE-CONVENTIONS](core/CORE-CONVENTIONS-约定.md) 约定 | 跨文档共同约定速查：坐标单位、时间、编号命名、帧顺序、目录归属、确定性 | WP-00 | G1、G2、G3、G4、G5 |
| [CORE-DECISIONS](core/CORE-DECISIONS-决策登记.md) 决策登记 | 全项目已定决策的登记：每条一句结论，附适用范围、细则所在和状态 | — | — |
| [CORE-GLOSSARY](core/CORE-GLOSSARY-术语表.md) 术语表 | 文档里专有说法的统一解释，按领域分组，每条一两句并给出细则所在的小节 | — | — |

### 1.2 协作与流程 `workflow/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [WF-ENTRY](workflow/WF-ENTRY-角色入口卡.md) 角色入口卡 | 每个角色的入口卡：身份与回报对象、本角色独有的必守、开工动作、做某事前读哪一节；由钩子按节标题注入 | WP-00 | — |
| [WF-INDEX](workflow/WF-INDEX-协作总纲.md) 协作总纲 | 开发期多个模型与会话怎么协作的总纲：角色表与各角色入口、决定权与执行人、规则放哪份的判定表 | WP-00 | — |
| [WF-ONBOARD](workflow/WF-ONBOARD-子代理开工手册.md) 子代理开工手册 | 子代理的查询手册，不通读：开工路径与引擎命令、怎么查资料、完成怎么自检与回报样式 | WP-00 | — |
| [WF-COMMS](workflow/collaboration/WF-COMMS-交接与通信.md) 交接与通信 | .collab 交接目录与文件布局，命名与写法，任务单与请求文件的格式，来信与消息，Codex 会话与引导信箱 | WP-00 | — |
| [WF-MODEL](workflow/collaboration/WF-MODEL-模型选择与子代理.md) 模型选择与子代理 | 按七问与难度选施工模型和推理强度，Opus 与 Sonnet 的分界，机械活交谁，怎么开子代理，子代理生命周期，简报模板与接续段 | WP-00 | — |
| [WF-ROLE](workflow/collaboration/WF-ROLE-参与者与分工.md) 参与者与分工 | 会话的运行方式，各模型的长短板，GPT 模型不做审美，事归谁与交叉处，总指挥只做什么，Astra 当指挥 | WP-00 | — |
| [WF-QUOTA](workflow/management/WF-QUOTA-账号与额度.md) 账号与额度 | 额度口径，Claude 侧按 Opus 当量的并发公式与记账，节奏档，Codex 账号与速度，额度见底时冻结 | WP-00 | — |
| [WF-STATUS](workflow/management/WF-STATUS-施工状态与队列.md) 施工状态与队列 | 施工状态文件怎么填、待派工队列与队列平衡、交接单 | WP-00 | — |
| [WF-FLOW](workflow/process/WF-FLOW-开发流程.md) 开发流程 | 一个包从派工、施工、自测推送、回报到评审的流程，防打架，按工作量拆单，可调参数集中放，契约申请与共享工程文件，调研入队流水线 | WP-00 | — |
| [WF-BUILD](workflow/rules/WF-BUILD-施工边界.md) 施工边界 | 施工时能改哪里、共享文件与契约申请、新模块怎么接入，以及禁用写法门禁拦不住的两条约定 | WP-00 | G3、G4 |
| [WF-CORE](workflow/rules/WF-CORE-项目核心准则.md) 项目核心准则 | 所有会话、子代理和执行者每轮都要守的行为准则，一条一句，细则见各条指向；提醒钩子每条消息都注入本文 §0 | WP-00 | — |
| [WF-DOC](workflow/rules/WF-DOC-文档写法.md) 文档写法 | 规范文档的文件头、范围块、编码与小节引用、正文写法、冲突层级、新增文档与登记决策、文档维护 | WP-00 | — |
| [WF-RULE](workflow/rules/WF-RULE-全员守则.md) 全员守则 | 所有会话和子代理都要守的规则：不过度工程化、不向作者出选项、不预判时间、不干等、文件操作、git 与直接改主线、主线优先、测试最小、少和别人打架、写文档与记忆 | WP-00 | — |

### 1.3 美术方向 `art/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [ART-AMBIENCE](art/ART-AMBIENCE-日式日常街景.md) 日式日常街景：元素、分区与做法 | 日式日常街景元素的边界与分区，各元素的形、色、限定与三渲二做法，以及对应的鸟与声音 | WP-13 | G5、G7、G9、G12 |
| [ART-FIDELITY](art/ART-FIDELITY-视觉精度标准.md) 视觉精度标准：处处不粗糙，细节按区分级 | 任何位置都成立的精度底线、五个精度区的细节密度与判定距离、一票否决与验收方法，含行走走廊加密和外围分级精度 | WP-27 | G2、G7、G8、G9、G12、G13、G15、G17、G19 |
| [ART-MODELING](art/ART-MODELING-风格化造型规则.md) 风格化造型规则：比例、形体、部件与风格一致性 | 建筑、植被、道具与室内的风格化比例和形体，固有色约束，部件库写法，每类对象的最低合格构件与法线规则，以及金样与跨包风格一致性流程 | WP-09 | G2、G4、G5、G7、G12 |
| [ART-RENOVATION](art/ART-RENOVATION-老楼翻新.md) 老楼翻新：全部翻新的规则 | 老楼位置、体量与身份特征不动，外观全部换成新楼标准：材质配色、删简留、窗檐楼梯屋顶的改法、成电辨认与验收 | WP-14 | G5、G7、G9、G12 |
| [ART-SKYLIGHT](art/ART-SKYLIGHT-天空光照与大气.md) 天空、光照与大气 | 世界状态与美术日月、主光与阴影、天光、天空云星月、雾与大气透视、光效清单与冷暖对照 | WP-34、WP-03、WP-40 | G2、G7、G10、G12、G13、G16、G17 |
| [ART-STYLE](art/ART-STYLE-风格基础.md) 风格基础：三渲二日式动画背景规范 | 全局风格：调色板与色名、明暗分阶与影色、描线、高光与透光、贴图、后期、排除元素与情绪 | WP-04、WP-05 | G4、G5、G6、G7、G12、G13、G16、G17 |

### 1.4 世界内容（通用） `world/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [WLD-DIORAMA](world/WLD-DIORAMA-箱庭边界截面与远景.md) 箱庭边界、截面与远景 | 箱庭外缘的地层截面、水柱截面、雾海与清水河银带、360° 远景卡片和雪山，以及它们随时段、季节、天气的变化 | WP-22 | G4、G5、G7、G8、G12、G13、G16 |
| [WLD-INTERIORS](world/WLD-INTERIORS-室内.md) 室内：开放清单、门的区分、加载、生成器与类型库、楼梯与碰撞、预算 | 第一批 15 处开放室内与 3 处高处、门的开关、室内加载、平面生成器与类型库、楼梯碰撞、契约、预算与 G19 | WP-51、WP-52、WP-17、WP-31a、WP-31b、WP-31c | G5、G6、G8、G9、G15、G18、G19 |
| [WLD-LIFE](world/WLD-LIFE-动物生活痕迹与街道家具.md) 动物、生活痕迹与街道家具 | 全校动物与反应动物、生活痕迹、停着的车与街道家具的内容，昼夜、四季与天气的显隐，行走碰撞、声音数据与 Godot 实现 | WP-25、WP-13 | G4、G7、G8、G12、G13、G15、G18、G21 |
| [WLD-OUTSKIRTS](world/WLD-OUTSKIRTS-边界与外围.md) 边界与外围：大箱庭、外围环境带、阻挡与提示 | 大箱庭的三条线与截面精度、外围 O0–O3 的内容与精度、行走和旁观在边界的阻挡与提示、外围数据与预算 | WP-47 | G2、G6、G7、G8、G9、G12、G13、G15、G16 |
| [WLD-TERRAIN](world/WLD-TERRAIN-地形.md) 地形：分层、地形操作与高度函数、贴地、竖直落差、水位与可走性 | 校园地形的设计原则与分层、地形覆盖层与确定性高度函数、贴地与垫层、竖直落差、水位、可走性、着色与性能 | WP-08、WP-15 | G2、G5、G6、G8、G15 |
| [WLD-VEGETATION](world/WLD-VEGETATION-植被.md) 植被：树种与四季、银杏大道与水杉路、草坪与灌木 | 全校树木、灌木、草坪与水生植物的画法、树种四季、种植位置、季相参数、碰撞、声音接口与 Godot 实现 | WP-23 | G2、G4、G5、G7、G8、G10、G12、G13、G15、G16、G17、G21 |
| [WLD-WATER](world/WLD-WATER-水体.md) 水体：动画式水面、西湖、东湖、河渠、喷泉 | 全校水面的动画式画法、倒影与反光带、四季与天气的水面、开放泳区与游泳的水面反馈、各湖与河渠、喷泉与池水，以及 Godot 实现 | WP-24 | G4、G7、G8、G10、G12、G13、G16、G17 |

### 1.5 地图：清水河 `maps/qingshuihe/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [MAP-QSH-ADDITIONS](maps/qingshuihe/MAP-QSH-ADDITIONS-设计增补.md) 设计增补：给没有设计感的地方加设计 | 全校无设计感处的增补词汇、楼宇设计规则、禁做清单、各包增补任务，以及夜间灯光总图和雨天增补 | WP-13、WP-14、WP-28、WP-29、WP-30、WP-39 | G5、G7、G9、G12、G15 |
| [MAP-QSH-CENTER-EAST](maps/qingshuihe/MAP-QSH-CENTER-EAST-中部与东区.md) 中部与东区：中部科研区、雕像、品学楼、文科楼群、训练馆、球场、田径场 | 中部科研区与东区各建筑、雕像、球场的外观与身份特征、开放入口门口表现、四季昼夜、天气、行走、碰撞与声音数据 | WP-21 | G4、G5、G7、G8、G9、G10、G12、G15、G19、G21 |
| [MAP-QSH-CORE](maps/qingshuihe/MAP-QSH-CORE-核心区.md) 核心区：八角图书馆、时间广场、日晷、博物馆、西湖 | 核心区图书馆、时间广场、日晷、博物馆外观与西湖岸线构筑物，含四季昼夜、天气、行走与声音要点 | WP-16 | G4、G7、G9、G12、G13、G15、G18、G19、G21 |
| [MAP-QSH-LIBRARY](maps/qingshuihe/MAP-QSH-LIBRARY-图书馆与博物馆室内.md) 图书馆与博物馆室内 | 图书馆开放范围的室内、三条看进去的路径、室内光与视觉落点，以及博物馆展厅、行走与声音要点 | WP-17 | G4、G7、G9、G12、G13、G15、G19、G21 |
| [MAP-QSH-NORTH](maps/qingshuihe/MAP-QSH-NORTH-北部生活区.md) 北部生活区：学生活动中心、校医院、商业街、宿舍组团、餐厅 | 北部生活区各建筑的外观与身份特征、开放入口门口表现、四季昼夜、天气、行走挡路、碰撞与声音数据 | WP-20 | G4、G5、G7、G8、G9、G10、G12、G15、G19、G21 |
| [MAP-QSH-RENOVATION](maps/qingshuihe/MAP-QSH-RENOVATION-清水河翻新清单.md) 清水河翻新清单 | 清水河校区要翻新的楼与环境：逐条的实体、优先级、翻新部位和所用规则，以及待核实楼的做法 | WP-14 | G5、G7、G9、G12 |
| [MAP-QSH-SOUTH](maps/qingshuihe/MAP-QSH-SOUTH-南区.md) 南区：南门、主楼、东湖、宣邦楼、立人楼、体育场馆、国际创新中心 | 南区各楼与构筑物的外观、受光与夜景、四季昼夜与天气、行走与开放入口、声音要点 | WP-18 | G4、G5、G7、G9、G12、G13、G15、G19、G21 |
| [MAP-QSH-TERRAIN](maps/qingshuihe/MAP-QSH-TERRAIN-分区地形.md) 分区地形：骨架、地标丘、揭示路线与各区地形操作 | 清水河全图地形骨架与各区地形操作：分层分布、红枫丘、宿舍台原、揭示路线、常水位、点位标高、验收镜头与冲突处理 | WP-16、WP-18、WP-19、WP-20、WP-21 | G2、G7、G9、G15、G19 |
| [MAP-QSH-WEST](maps/qingshuihe/MAP-QSH-WEST-西区与西北区.md) 西区与西北区：六号科研楼、校训石、创新中心、经管楼、托幼园、四号/五号科研楼、湿地、西门桥头 | 西区与西北区各楼、石刻、湿地与西门桥头的外观、四季昼夜与天气、行走与开放入口、声音要点 | WP-19 | G4、G5、G7、G9、G12、G15、G19、G21 |

### 1.6 运行时系统 `systems/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [SYS-ATLAS](systems/SYS-ATLAS-平面地图与传送.md) 平面地图与传送：底图、小地图、大地图、标记、锚点、传送与解锁 | 规定平面地图：底图烘焙与上色、小地图、大地图、标记、锚点、雾幕传送、解锁、契约与 G18 验收 | WP-48 | G2、G13、G14、G15、G16、G18 |
| [SYS-AUDIO](systems/SYS-AUDIO-音频.md) 音频：环境声、观者声、空间声学、界面音效与混音 | 声音的总体架构、环境声景、天气与钟声、观者声、空间声学、界面音、素材许可、契约、预算与 G21 门禁 | WP-53 | G2、G4、G5、G6、G13、G14、G21 |
| [SYS-GRAPHICS](systems/SYS-GRAPHICS-画面设置.md) 画面设置：预设、单项、风格底线、自动推荐与自适应 | 四档画面预设与单项、风格底线、应用流程、首次分档与自动调节、持久化与命令行、契约与验收 | WP-32 | G2、G5、G7、G8、G9、G10、G11、G12、G13、G14、G16、G17 |
| [SYS-MAPS](systems/SYS-MAPS-多地图.md) 多地图：按图数据、`IMapService` 与切换 | 按图数据与 map.json、IMapService 先卸后装切图、时间全局天气按图、map_rect、同图重载门禁 | WP-46 | G2、G4、G5、G6、G7、G8、G13、G16 |
| [SYS-MOTION](systems/SYS-MOTION-动态与事件.md) 动态与事件 | 两个时钟与一拍二，风、悬浮物、流体、生命体与反应动物的节奏，唯一的偶发事件、季节氛围与转场 | WP-26 | G4、G12、G13、G16、G21 |
| [SYS-SAVE](systems/SYS-SAVE-存档与持久化.md) 存档与持久化：存档模型、存什么、格式与迁移、保存时机、加载与一致性、安全、契约与验收 | 规定存档位与世界线、存档段、文件格式与迁移、保存时机、读档流程与一致性、设置文件安全与 G20 | WP-50 | G2、G4、G5、G10、G16、G20 |
| [SYS-TIME](systems/SYS-TIME-时间与季节.md) 时间与季节系统：世界状态、美术日月、色彩脚本、过渡 | 世界状态与控制契约、美术日月、色彩脚本、四季内容、时间季节天气的过渡、灯光时刻表与时季验收 | WP-34、WP-37、WP-38、WP-39、WP-41 | G2、G7、G12、G13、G16 |
| [SYS-TITLE](systems/SYS-TITLE-开始页与进入过场.md) 开始页与进入过场：扉页、开始页、新游戏与继续过场、跳过、`IAppFlow` | 规定冷启动扉页、开始页 T0、新游戏与继续过场、跳过、IAppFlow 状态机与过场数据格式 | WP-49、WP-35d | G2、G7、G8、G10、G13、G14、G16 |
| [SYS-VIEW](systems/SYS-VIEW-观察模式介质与开场.md) 观察模式、介质与开场 | 观察模式总述与旁观漫游、行走的第一与第三人称镜头、镜头在楼里地下水下雾海的介质呈现、启动开场与署名、旧网页栈归档 | WP-06、WP-07、WP-01 | G1、G2、G7、G13、G16、G21 |
| [SYS-WALK](systems/SYS-WALK-行走模式.md) 行走模式：手感、可行走范围、边界、碰撞与角色控制器 | 行走模式的手感、玩家身体、游泳、骑行、模式切换、可行走范围与边界、开放室内的门与电梯、角色控制器、碰撞数据与验收 | WP-42、WP-43、WP-44 | G4、G8、G15、G16、G19、G21 |
| [SYS-WEATHER](systems/SYS-WEATHER-天气.md) 天气：天气清单、画面规则、前向随机过程、过渡与验收 | 八格天气的画面与声音规则、自动天气的前向随机过程、各系统的实现、状态与契约、存档段与验收 | WP-45 | G2、G4、G10、G12、G13、G14、G16、G20、G21 |

### 1.7 界面 `ui/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [UI-NAV](ui/UI-NAV-界面与操作说明.md) 界面、点位跳转与操作说明 | 界面的内容与交互：组件、快捷键、点位清单、跳转与传送过渡、无障碍、实现与性能 | WP-35、WP-36 | G2、G4、G7、G13、G14、G15、G16 |
| [UI-VISUAL](ui/UI-VISUAL-界面视觉语言.md) 界面视觉语言：令牌、字体、形状、材质、图标、组件、画面布局、动效与实现 | 界面视觉规范：色彩令牌与两套主题、字体、形状材质、图标、组件、布局、动效、Godot 实现与验收 | WP-35 | G4、G5、G14、G16 |

### 1.8 工程 `engineering/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [ENG-ARCH](engineering/ENG-ARCH-工程架构.md) 工程架构：模块边界、接口契约与并行开发流程 | 目录与归属、程序集边界、全部 C# 契约、帧顺序、资源管线、桩与构建测试排队 | WP-00 | G1、G2、G3、G4、G5、G6 |
| [ENG-BUDGET](engineering/ENG-BUDGET-性能预算与硬规则.md) 性能预算与硬规则 | 预设与设备档的预算、细节级别、各包额度总账（budgets.json）、推送硬规则、测法与工程设置 | WP-00 | G4、G5、G6、G8、G10、G11、G13、G14、G15、G16、G17、G20、G21 |
| [ENG-GATES](engineering/ENG-GATES-验收与门禁.md) 验收：门禁、审查视角、近景巡检 | 审查视角与命令行参数、推送前门禁 G1–G21、一票否决、全局清单 | WP-27 | G1、G2、G3、G4、G5、G6、G7、G8、G9、G10、G11、G12、G13、G14、G15、G16、G17、G18、G19、G20、G21 |
| [ENG-PERF](engineering/ENG-PERF-性能工程.md) 性能工程：高质量实现设计（Godot 4.7.2 .NET） | 性能的实现方法：合批、着色器白名单、全局参数登记、预热、剔除、流式、主线程、内存、阴影后期、自适应与测量 | WP-01、WP-02 | G2、G4、G5、G6、G8、G10、G11、G13、G16、G17、G19、G20、G21 |
| [ENG-ROADMAP](engineering/ENG-ROADMAP-扩展路线.md) 扩展路线：从观光到 RPG | 规定观光版给以后 RPG 留的扩展点、现在不能写死的地方和 v1 冻结前的最小预留 | WP-00 | G4、G5、G20 |
| [ENG-WP](engineering/ENG-WP-工作包派工单.md) 工作包派工单：56 个工作包，可并行 55 个 | 56 个工作包的划分、归属、独占目录、起读、交付与额度，开发阶段与闸口、开工波次与推送顺序，各包主线版范围，细节阶段待办与候选包，主代理的工程维护事项 | WP-00 | G1、G3、G5 |

### 1.9 工具 `tools/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [TOOL-PREVIEW](tools/TOOL-PREVIEW-内容预览.md) 内容预览 | 开发构建里预览与回看内容：按实体定位、保留机位与时季天气、复制机位、重读数据 | WP-01、WP-08 | — |

### 1.10 运维 `ops/`

| 文档 | 规定什么 | 主责 | 门禁 |
| --- | --- | --- | --- |
| [OPS-RELEASE](ops/OPS-RELEASE-发布与交付.md) 发布与交付 | 版本号与构建信息、正式用户目录、导出与发布、安装更新卸载、地图数据包、故障诊断和许可 | WP-00 | G6 |

## 2 工作包与文档

主责与增量取自各文档文件头的 `owners`、`contributors`；独占目录、起读与交付以 ENG-WP 为准。

| 工作包 | 主责文档 | 有增量的文档 |
| --- | --- | --- |
| WP-00 | CORE-CONTENT、CORE-CONVENTIONS、ENG-ARCH、ENG-BUDGET、ENG-ROADMAP、ENG-WP、OPS-RELEASE、WF-BUILD、WF-COMMS、WF-CORE、WF-DOC、WF-ENTRY、WF-FLOW、WF-INDEX、WF-MODEL、WF-ONBOARD、WF-QUOTA、WF-ROLE、WF-RULE、WF-STATUS | ENG-GATES、SYS-AUDIO、SYS-MAPS、SYS-SAVE、SYS-VIEW、SYS-WALK、WLD-INTERIORS、WLD-TERRAIN |
| WP-01 | ENG-PERF、SYS-VIEW、TOOL-PREVIEW | ENG-ARCH、ENG-GATES、OPS-RELEASE、SYS-ATLAS、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-SAVE、SYS-TITLE、SYS-WALK、SYS-WEATHER、UI-NAV、UI-VISUAL |
| WP-02 | ENG-PERF | ENG-ARCH、ENG-BUDGET、ENG-GATES、OPS-RELEASE、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-SAVE、SYS-TITLE、SYS-WEATHER、TOOL-PREVIEW、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-VEGETATION |
| WP-03 | ART-SKYLIGHT | ENG-PERF、MAP-QSH-LIBRARY、MAP-QSH-NORTH、MAP-QSH-SOUTH、SYS-AUDIO、SYS-GRAPHICS、SYS-SAVE、SYS-TIME、SYS-WEATHER、WLD-INTERIORS |
| WP-04 | ART-STYLE | ART-AMBIENCE、ART-FIDELITY、ART-RENOVATION、ART-SKYLIGHT、ENG-ARCH、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-LIBRARY、MAP-QSH-SOUTH、SYS-ATLAS、SYS-GRAPHICS、SYS-TIME、SYS-VIEW、SYS-WEATHER、UI-VISUAL、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-05 | ART-STYLE | ART-SKYLIGHT、ENG-PERF、SYS-GRAPHICS、SYS-WEATHER、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-06 | SYS-VIEW | ART-SKYLIGHT、ENG-GATES、ENG-ROADMAP、SYS-ATLAS、SYS-AUDIO、SYS-SAVE、SYS-TITLE、SYS-WALK、UI-NAV、WLD-OUTSKIRTS |
| WP-07 | SYS-VIEW | ART-FIDELITY、ART-SKYLIGHT、ENG-PERF、ENG-ROADMAP、SYS-AUDIO、SYS-GRAPHICS、SYS-WALK、WLD-DIORAMA、WLD-INTERIORS、WLD-WATER |
| WP-08 | TOOL-PREVIEW、WLD-TERRAIN | ENG-ARCH、MAP-QSH-TERRAIN、OPS-RELEASE、SYS-MAPS、SYS-VIEW、SYS-WALK、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-VEGETATION、WLD-WATER |
| WP-09 | ART-MODELING | ART-RENOVATION |
| WP-10 | — | ART-FIDELITY、ART-MODELING、SYS-WALK、WLD-INTERIORS |
| WP-11 | — | ART-MODELING、SYS-AUDIO、SYS-WALK、WLD-INTERIORS、WLD-TERRAIN |
| WP-12 | — | ART-FIDELITY、ART-MODELING、MAP-QSH-ADDITIONS、MAP-QSH-WEST、WLD-INTERIORS、WLD-LIFE |
| WP-13 | ART-AMBIENCE、MAP-QSH-ADDITIONS、WLD-LIFE | ART-MODELING、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-RENOVATION、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-ATLAS、SYS-AUDIO、SYS-WALK、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-14 | ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-RENOVATION | ART-AMBIENCE、ART-FIDELITY、ART-MODELING、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-15 | WLD-TERRAIN | ART-AMBIENCE、ART-FIDELITY、ART-MODELING、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-RENOVATION、MAP-QSH-SOUTH、MAP-QSH-TERRAIN、MAP-QSH-WEST、SYS-AUDIO、SYS-MAPS、SYS-WALK、SYS-WEATHER、WLD-OUTSKIRTS、WLD-VEGETATION |
| WP-16 | MAP-QSH-CORE、MAP-QSH-TERRAIN | ART-AMBIENCE、ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-LIBRARY、MAP-QSH-RENOVATION、MAP-QSH-WEST、SYS-WALK、WLD-INTERIORS、WLD-LIFE、WLD-TERRAIN、WLD-WATER |
| WP-17 | MAP-QSH-LIBRARY、WLD-INTERIORS | ART-RENOVATION、MAP-QSH-CORE、SYS-AUDIO、SYS-WALK |
| WP-18 | MAP-QSH-SOUTH、MAP-QSH-TERRAIN | ART-AMBIENCE、ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-RENOVATION、SYS-WALK、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-19 | MAP-QSH-TERRAIN、MAP-QSH-WEST | ART-AMBIENCE、ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-RENOVATION、WLD-INTERIORS、WLD-TERRAIN |
| WP-20 | MAP-QSH-NORTH、MAP-QSH-TERRAIN | ART-AMBIENCE、ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-RENOVATION、SYS-WALK、WLD-INTERIORS、WLD-LIFE、WLD-TERRAIN、WLD-VEGETATION |
| WP-21 | MAP-QSH-CENTER-EAST、MAP-QSH-TERRAIN | ART-AMBIENCE、ART-RENOVATION、MAP-QSH-ADDITIONS、MAP-QSH-RENOVATION、WLD-INTERIORS、WLD-TERRAIN、WLD-WATER |
| WP-22 | WLD-DIORAMA | SYS-MAPS、SYS-TIME、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-23 | WLD-VEGETATION | ART-AMBIENCE、ART-FIDELITY、ART-MODELING、ART-STYLE、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-GRAPHICS、SYS-MAPS、SYS-SAVE、SYS-WALK、SYS-WEATHER、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-24 | WLD-WATER | ART-FIDELITY、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-SOUTH、MAP-QSH-TERRAIN、MAP-QSH-WEST、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-VIEW、SYS-WALK、SYS-WEATHER、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-25 | WLD-LIFE | ART-AMBIENCE、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-AUDIO、SYS-MOTION、SYS-SAVE、SYS-TITLE、SYS-WEATHER、WLD-TERRAIN、WLD-VEGETATION |
| WP-26 | SYS-MOTION | ENG-ROADMAP、MAP-QSH-ADDITIONS、MAP-QSH-CORE、MAP-QSH-SOUTH、SYS-AUDIO、SYS-MAPS、SYS-SAVE、SYS-TIME、SYS-WEATHER、WLD-INTERIORS、WLD-LIFE、WLD-VEGETATION、WLD-WATER |
| WP-27 | ART-FIDELITY、ENG-GATES | CORE-CONVENTIONS、ENG-ARCH、ENG-BUDGET、ENG-PERF、ENG-ROADMAP、SYS-ATLAS、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-SAVE、SYS-TITLE、SYS-WEATHER、UI-NAV、UI-VISUAL、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-VEGETATION |
| WP-28 | MAP-QSH-ADDITIONS | MAP-QSH-CENTER-EAST、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-AUDIO、SYS-WALK、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-29 | MAP-QSH-ADDITIONS | — |
| WP-30 | MAP-QSH-ADDITIONS | MAP-QSH-CENTER-EAST、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-AUDIO、SYS-WALK、WLD-DIORAMA、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-31 | — | SYS-AUDIO |
| WP-31a | WLD-INTERIORS | MAP-QSH-ADDITIONS、MAP-QSH-SOUTH、SYS-WALK、WLD-WATER |
| WP-31b | WLD-INTERIORS | MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-WEST、SYS-WALK |
| WP-31c | WLD-INTERIORS | ART-MODELING、MAP-QSH-ADDITIONS、MAP-QSH-NORTH、SYS-WALK |
| WP-32 | SYS-GRAPHICS | ENG-BUDGET、ENG-GATES、ENG-PERF、SYS-SAVE、SYS-TITLE |
| WP-33 | — | ENG-PERF、WLD-INTERIORS、WLD-OUTSKIRTS |
| WP-34 | ART-SKYLIGHT、SYS-TIME | ENG-PERF、SYS-MAPS、SYS-MOTION、SYS-SAVE、SYS-TITLE、SYS-WEATHER |
| WP-35 | UI-NAV、UI-VISUAL | ENG-GATES、ENG-ROADMAP、OPS-RELEASE、SYS-ATLAS、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-SAVE、SYS-TIME、SYS-VIEW、SYS-WALK、SYS-WEATHER、WLD-INTERIORS、WLD-OUTSKIRTS |
| WP-35d | SYS-TITLE | — |
| WP-36 | UI-NAV | ENG-BUDGET、ENG-GATES、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-ATLAS、SYS-AUDIO、SYS-MAPS、SYS-SAVE、SYS-VIEW、SYS-WALK、SYS-WEATHER、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-37 | SYS-TIME | ART-MODELING、ART-SKYLIGHT、ART-STYLE、ENG-PERF、MAP-QSH-ADDITIONS、SYS-WEATHER、UI-VISUAL、WLD-DIORAMA、WLD-INTERIORS、WLD-OUTSKIRTS、WLD-VEGETATION、WLD-WATER |
| WP-38 | SYS-TIME | ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-AUDIO、SYS-GRAPHICS、SYS-MOTION、SYS-WEATHER、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-VEGETATION |
| WP-39 | MAP-QSH-ADDITIONS、SYS-TIME | ART-SKYLIGHT、ART-STYLE、ENG-PERF、ENG-ROADMAP、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-LIBRARY、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-ATLAS、SYS-AUDIO、SYS-MAPS、SYS-MOTION、SYS-SAVE、SYS-TITLE、SYS-WEATHER、WLD-INTERIORS、WLD-LIFE、WLD-OUTSKIRTS |
| WP-40 | ART-SKYLIGHT | ENG-PERF、SYS-AUDIO、SYS-MAPS、SYS-MOTION、SYS-TIME、SYS-WEATHER、WLD-DIORAMA |
| WP-41 | SYS-TIME | ENG-GATES、SYS-AUDIO、SYS-GRAPHICS、SYS-TITLE、SYS-WEATHER、WLD-OUTSKIRTS |
| WP-42 | SYS-WALK | ENG-GATES、ENG-PERF、ENG-ROADMAP、SYS-AUDIO、SYS-SAVE、SYS-VIEW、WLD-INTERIORS、WLD-LIFE、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-43 | SYS-WALK | ENG-GATES、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-LIBRARY、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-AUDIO、SYS-MAPS、SYS-SAVE、WLD-INTERIORS、WLD-LIFE、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-VEGETATION |
| WP-44 | SYS-WALK | ART-FIDELITY、ENG-GATES、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-ATLAS、SYS-MAPS、WLD-INTERIORS、WLD-LIFE、WLD-OUTSKIRTS、WLD-TERRAIN、WLD-WATER |
| WP-45 | SYS-WEATHER | ART-SKYLIGHT、ART-STYLE、ENG-GATES、ENG-PERF、MAP-QSH-ADDITIONS、SYS-AUDIO、SYS-GRAPHICS、SYS-MAPS、SYS-MOTION、SYS-TIME、WLD-OUTSKIRTS |
| WP-45a | — | SYS-SAVE、WLD-VEGETATION |
| WP-45b | — | WLD-VEGETATION、WLD-WATER |
| WP-46 | SYS-MAPS | ENG-GATES、ENG-PERF、ENG-ROADMAP、SYS-AUDIO、SYS-MOTION、SYS-SAVE、SYS-TIME、SYS-TITLE、SYS-WEATHER、TOOL-PREVIEW、WLD-OUTSKIRTS |
| WP-47 | WLD-OUTSKIRTS | ART-AMBIENCE、ART-FIDELITY、ENG-BUDGET、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、OPS-RELEASE、SYS-GRAPHICS、SYS-MAPS、SYS-TIME、SYS-WALK、SYS-WEATHER、WLD-DIORAMA、WLD-TERRAIN、WLD-VEGETATION、WLD-WATER |
| WP-48 | SYS-ATLAS | ENG-GATES、ENG-PERF、MAP-QSH-ADDITIONS、SYS-AUDIO、SYS-MAPS、UI-NAV、UI-VISUAL、WLD-INTERIORS、WLD-OUTSKIRTS |
| WP-48a | — | WLD-LIFE |
| WP-48b | — | ENG-ROADMAP |
| WP-48c | — | ENG-ROADMAP、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-SAVE、SYS-WALK、WLD-LIFE |
| WP-49 | SYS-TITLE | ENG-GATES、ENG-PERF、SYS-AUDIO、SYS-MAPS、SYS-SAVE、SYS-VIEW、UI-NAV、UI-VISUAL、WLD-OUTSKIRTS、WLD-TERRAIN |
| WP-49a | — | ENG-ROADMAP |
| WP-49b | — | WLD-LIFE |
| WP-50 | SYS-SAVE | ENG-GATES、ENG-PERF、ENG-ROADMAP、OPS-RELEASE、SYS-MAPS、SYS-TITLE、SYS-WALK、SYS-WEATHER、UI-NAV、UI-VISUAL |
| WP-51 | WLD-INTERIORS | ART-FIDELITY、ART-MODELING、ENG-PERF、MAP-QSH-ADDITIONS、MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-LIBRARY、MAP-QSH-NORTH、SYS-ATLAS、SYS-AUDIO、SYS-MAPS、SYS-VIEW、SYS-WALK |
| WP-52 | WLD-INTERIORS | ART-FIDELITY、ART-MODELING、MAP-QSH-ADDITIONS、MAP-QSH-LIBRARY、MAP-QSH-NORTH、WLD-LIFE |
| WP-53 | SYS-AUDIO | ART-AMBIENCE、ENG-GATES、ENG-PERF、MAP-QSH-LIBRARY、MAP-QSH-SOUTH、MAP-QSH-WEST、SYS-MAPS、SYS-MOTION、SYS-VIEW、UI-NAV、UI-VISUAL、WLD-INTERIORS、WLD-LIFE、WLD-OUTSKIRTS、WLD-VEGETATION、WLD-WATER |
| WP-53a | — | SYS-ATLAS、SYS-SAVE、SYS-TITLE |
| WP-53b | — | MAP-QSH-CENTER-EAST、MAP-QSH-CORE、MAP-QSH-NORTH、SYS-WEATHER |
| WP-53d | — | SYS-WALK、SYS-WEATHER |
| WP-54a | — | SYS-SAVE、SYS-VIEW、SYS-WALK |
| WP-54b | — | SYS-WALK |
| WP-55 | — | SYS-ATLAS、SYS-SAVE、SYS-WALK、UI-NAV、WLD-LIFE |
