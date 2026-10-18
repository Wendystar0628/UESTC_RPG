# 小节目录

> 由 `scripts/docs/gen_index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（CORE-INDEX §1）。行号以生成时为准，原文改过以后可能差几行，以标题文字为准。

## CORE-INDEX · 《电光年华》开发总纲

`docs/README.md` · 7.3k 字 · 全项目唯一的顶层：核心准则（钩子注入）、按功能分域的文档地图、作品定位与当前范围、全局不变量 INV-1–INV-12
关键词：总纲、核心准则、文档地图、怎么查文档、作品定位、电光年华、当前范围、主线、全局不变量、INV、清水河、实现优先、不过度工程化、测试最小、改设计必清旧、直接改主线、一份权威、文档精简、落金初灯

- CORE-INDEX §0 核心准则 · L21 · 1.7k
  - ★ CORE-INDEX §0.1 接入要点 · L46 · 119
- CORE-INDEX §1 文档地图 · L52 · 2.0k
- CORE-INDEX §2 作品与范围 · L85 · 965
- CORE-INDEX §3 全局不变量 · L95 · 2.0k

## CORE-CONVENTIONS · 约定

`docs/core/CORE-CONVENTIONS-约定.md` · 8.7k 字 · 跨文档共同约定：坐标与单位、方位角、时间写法、编号与命名、实体 ID 规则、确定性原则
关键词：约定、坐标系、单位、角度、方位角、时间写法、编号体系、命名、实体 ID、色名、枚举纪律、持久化 ID、确定性

- CORE-CONVENTIONS §0 结论 · L22 · 807
  - ★ CORE-CONVENTIONS §0.1 接入要点 · L32 · 424
- CORE-CONVENTIONS §1 坐标与单位 · L42 · 1.4k
  - CORE-CONVENTIONS §1.1 坐标系 · L44 · 510
  - CORE-CONVENTIONS §1.2 角度 · L52 · 366
  - CORE-CONVENTIONS §1.3 清水河的范围数 · L60 · 278
  - CORE-CONVENTIONS §1.4 其他单位 · L68 · 267
- CORE-CONVENTIONS §2 时间写法 · L76 · 831
- CORE-CONVENTIONS §3 标识与命名 · L94 · 4.3k
  - CORE-CONVENTIONS §3.1 地图、实体与覆盖层 · L96 · 826
  - CORE-CONVENTIONS §3.2 编号体系 · L111 · 835
  - CORE-CONVENTIONS §3.3 同号不同义的读法 · L133 · 576
  - CORE-CONVENTIONS §3.4 命名写法 · L145 · 682
  - CORE-CONVENTIONS §3.5 色名 · L155 · 364
  - CORE-CONVENTIONS §3.6 枚举与扩展纪律 · L162 · 567
  - CORE-CONVENTIONS §3.7 持久化 ID 的稳定 · L173 · 426
- CORE-CONVENTIONS §4 确定性 · L182 · 294
- CORE-CONVENTIONS §5 路径 · L190 · 258

## CORE-DECISIONS-PROCESS · 协作与流程决策

`docs/core/CORE-DECISIONS-PROCESS-协作与流程决策.md` · 10.0k 字 · 协作、流程、文档规则与门禁流程类的已定决策
关键词：决策、流程、协作、文档规则、门禁流程、派工、主代理、子代理

- CORE-DECISIONS-PROCESS §0 结论 · L23 · 173
  - ★ CORE-DECISIONS-PROCESS §0.1 接入要点 · L27 · 75
- CORE-DECISIONS-PROCESS §1 流程 · L31 · 9.3k

## CORE-DECISIONS-SYSTEMS · 系统与工程决策

`docs/core/CORE-DECISIONS-SYSTEMS-系统与工程决策.md` · 13.3k 字 · 界面、存档、音频、性能与画面设置、工程五个领域的已定决策
关键词：决策、系统、工程、界面、存档、音频、性能、画面设置、预算、契约

- CORE-DECISIONS-SYSTEMS §0 结论 · L23 · 164
  - ★ CORE-DECISIONS-SYSTEMS §0.1 接入要点 · L27 · 75
- CORE-DECISIONS-SYSTEMS §1 界面 · L31 · 3.2k
- CORE-DECISIONS-SYSTEMS §2 存档 · L61 · 1.4k
- CORE-DECISIONS-SYSTEMS §3 音频 · L79 · 2.1k
- CORE-DECISIONS-SYSTEMS §4 性能与画面设置 · L101 · 3.0k
- CORE-DECISIONS-SYSTEMS §5 工程 · L129 · 2.8k

## CORE-DECISIONS-WORLD · 世界与画面决策

`docs/core/CORE-DECISIONS-WORLD-世界与画面决策.md` · 11.0k 字 · 作品定位与范围、世界与地图、时间季节天气、美术风格四个领域的已定决策
关键词：决策、世界、画面、作品定位、地图、时间、季节、天气、美术风格

- CORE-DECISIONS-WORLD §0 结论 · L23 · 158
  - ★ CORE-DECISIONS-WORLD §0.1 接入要点 · L27 · 75
- CORE-DECISIONS-WORLD §1 作品定位与范围 · L31 · 1.8k
- CORE-DECISIONS-WORLD §2 世界与地图 · L48 · 3.6k
- CORE-DECISIONS-WORLD §3 时间、季节、天气 · L79 · 2.5k
- CORE-DECISIONS-WORLD §4 美术风格 · L104 · 2.3k

## CORE-DECISIONS · 决策登记

`docs/core/CORE-DECISIONS-决策登记.md` · 1.8k 字 · 全项目已定决策登记的用法与归卷规则；决策本体分世界与画面、系统与工程、协作与流程三卷
关键词：决策、D 编号、决策登记、生效、待原型验证、细则所在、推翻决策、归卷

- CORE-DECISIONS §0 用法 · L23 · 680
  - ★ CORE-DECISIONS §0.1 接入要点 · L32 · 175
- CORE-DECISIONS §1 卷目录 · L38 · 228

## CORE-GLOSSARY · 术语表

`docs/core/CORE-GLOSSARY-术语表.md` · 20.9k 字 · 文档里专有说法的统一解释，按领域分组，每条一两句并给出细则所在的小节
关键词：术语、术语表、名词解释、同名异义、缩写、别名

- CORE-GLOSSARY §0 用法 · L22 · 261
  - ★ CORE-GLOSSARY §0.1 接入要点 · L28 · 92
- CORE-GLOSSARY §1 作品与默认状态 · L33 · 1.3k
- CORE-GLOSSARY §2 世界状态与时间 · L54 · 1.3k
- CORE-GLOSSARY §3 天气 · L77 · 1.3k
- CORE-GLOSSARY §4 美术与光照 · L99 · 1.2k
- CORE-GLOSSARY §5 箱庭、地形、边界与外围 · L121 · 1.5k
- CORE-GLOSSARY §6 室内与门 · L146 · 974
- CORE-GLOSSARY §7 观察模式、行走与跳转 · L162 · 1.5k
- CORE-GLOSSARY §8 平面地图与传送 · L187 · 934
- CORE-GLOSSARY §9 开始页、过场与存档 · L204 · 1.4k
- CORE-GLOSSARY §10 多地图 · L228 · 611
- CORE-GLOSSARY §11 界面 · L240 · 1.4k
- CORE-GLOSSARY §12 音频 · L267 · 1.1k
- CORE-GLOSSARY §13 画面设置与性能 · L288 · 1.7k
- CORE-GLOSSARY §14 工程与流程 · L315 · 2.2k
- CORE-GLOSSARY §15 同名异义 · L351 · 1.6k

## WF-BUILD · 施工边界

`docs/workflow/WF-BUILD-施工边界.md` · 3.5k 字 · 施工时能改哪里、共享文件、契约申请与冻结、构建与测试排队、新模块怎么接入、Godot API 先查签名，以及禁用写法门禁拦不住的两条约定
关键词：独占目录、共享文件、契约申请、契约冻结、构建锁、构建与测试排队、.uid、特性接入、穷举 switch、几何不随时季天气变、GDScript、uid、signatures.txt、Godot API

- WF-BUILD §0 结论 · L23 · 839
  - ★ WF-BUILD §0.1 接入要点 · L32 · 430
- WF-BUILD §1 改哪里 · L40 · 513
- WF-BUILD §2 G4 拦不住的约定 · L48 · 170
- WF-BUILD §3 契约申请与冻结 · L53 · 325
- WF-BUILD §4 构建与测试排队 · L60 · 760

## WF-COMMS · 交接与通信

`docs/workflow/WF-COMMS-交接与通信.md` · 5.8k 字 · .collab 交接目录与文件布局，命名与写法，任务单与请求文件的格式，契约申请短头部，完成回报格式，来信与消息，Codex 会话与引导信箱
关键词：.collab、交接目录、文件布局、结案即删、D-228、不归档、下一个编号、任务单、施工单、旁路单、调研目录、契约申请、评审请求、六行短头部、完成回报、回报格式、截图命名、文件命名、来信、收件箱、notes、一条一个文件、【需定】【待审】【知会】、Codex 会话、codex queue、引导信箱、steer、HANDOVER.md、D-207、D-208

- WF-COMMS §0 结论 · L23 · 665
  - ★ WF-COMMS §0.1 接入要点 · L32 · 213
- WF-COMMS §1 交接目录与文件 · L39 · 1.2k
- WF-COMMS §2 命名与写法 · L61 · 451
- WF-COMMS §3 任务单、请求文件、契约申请与完成回报 · L68 · 971
  - WF-COMMS §3.1 施工单与旁路单 · L70 · 237
  - WF-COMMS §3.2 请求文件 · L75 · 50
  - WF-COMMS §3.3 契约申请的短头部（D-201） · L79 · 168
  - WF-COMMS §3.4 完成回报 · L89 · 491
- WF-COMMS §4 来信与消息 · L107 · 1.3k
  - WF-COMMS §4.1 什么时候发 · L109 · 252
  - WF-COMMS §4.2 一条来信一个新文件（D-207） · L118 · 206
  - WF-COMMS §4.3 三级标记与写法 · L122 · 306
  - WF-COMMS §4.4 来信、排队消息与引导信箱 · L129 · 513
- WF-COMMS §5 Codex 会话与引导信箱 · L142 · 319

## WF-DOC · 文档写法

`docs/workflow/WF-DOC-文档写法.md` · 7.9k 字 · 文档网的五层、每份文档的形状、编码与引用、只写什么不写什么、体量上限、冲突层级、新增与维护
关键词：文档写法、D-220、D-222、D-227、文档网、五层、渐进式披露、核心准则、文档地图、接入要点、文件头、front matter、范围块、查阅行、编码、文件名、小节编号、引用写法、全局替换、只写现行规则、不写实现、一份权威、体量上限、字数上限、待验证、冲突层级、规则放哪份、新增文档、登记决策、gen_index、check_docs、SECTIONS.md、D-178、D-200、D-208、D-215、D-216、D-217

- WF-DOC §0 结论 · L22 · 735
  - ★ WF-DOC §0.1 接入要点 · L31 · 235
- WF-DOC §1 五层 · L38 · 583
- WF-DOC §2 文档形状 · L51 · 933
- WF-DOC §3 编码、小节编号与引用 · L77 · 920
- WF-DOC §4 写什么，怎么写 · L95 · 1.0k
  - WF-DOC §4.1 取舍 · L97 · 530
  - WF-DOC §4.2 写法（D-220） · L106 · 460
- WF-DOC §5 体量上限 · L118 · 1.3k
  - WF-DOC §5.1 长大了怎么拆（D-222） · L135 · 504
  - WF-DOC §5.2 新内容放哪 · L142 · 239
- WF-DOC §6 冲突与规则放哪 · L149 · 773
- WF-DOC §7 新增文档 · L178 · 243
- WF-DOC §8 登记新决策 · L184 · 180
- WF-DOC §9 维护 · L190 · 368

## WF-ENTRY · 角色入口卡

`docs/workflow/WF-ENTRY-角色入口卡.md` · 4.8k 字 · 每个角色的入口卡：身份与回报对象、开工动作、本角色独有的必守、做某事前读哪一节；由钩子按节标题注入
关键词：角色入口卡、入口卡、主会话、子代理、Astra、Sol 会话、重置后先读、开工动作、收尾、做 X 前读 Y、钩子注入、SessionStart、SubagentStart、引擎路径、UESTC_GODOT

- WF-ENTRY §0 结论 · L21 · 381
  - ★ WF-ENTRY §0.1 接入要点 · L27 · 127
- WF-ENTRY §1 主会话 · L33 · 1.2k
- WF-ENTRY §2 子代理 · L58 · 1.1k
- WF-ENTRY §3 Astra · L86 · 980
- WF-ENTRY §4 Sol 会话 · L109 · 577

## WF-FLOW · 开发流程

`docs/workflow/WF-FLOW-开发流程.md` · 4.8k 字 · 一个包从派工、施工（只写不编译）、统一编译推送、回报到评审的流程，防打架，按工作量拆单，可调参数集中放
关键词：开发流程、派工、施工单、按工作量拆单、子单 1 小时、回报、评审、防打架、邻居、可调参数、D-184、D-211、评审通用结论

- WF-FLOW §0 结论 · L22 · 431
  - ★ WF-FLOW §0.1 接入要点 · L29 · 170
- WF-FLOW §1 全流程一览 · L35 · 520
- WF-FLOW §2 派工与拆单 · L46 · 1.4k
  - WF-FLOW §2.1 防打架（D-211） · L62 · 361
- WF-FLOW §3 施工与回报 · L71 · 1.0k
  - WF-FLOW §3.1 统一编译（D-219） · L83 · 498
- WF-FLOW §4 评审：审美与契约的交叉评审 · L92 · 702

## WF-MODEL · 模型、子代理与额度

`docs/workflow/WF-MODEL-模型、子代理与额度.md` · 7.5k 字 · 按七问与难度选施工模型和推理强度，Opus 与 Sonnet 的分界，机械活交谁，怎么开子代理，子代理生命周期，简报模板与接续段，额度、并发与冻结
关键词：七问、选模型、Opus 与 Sonnet 的分界、Sonnet 优先、机械活、gpt-6-luna 子代理、推理强度、升级先找原因、派单最少写、简单少量的活直接做、子代理、Agent 工具、spawn_agent、共用工作目录、子代理生命周期、默认新开、恢复还是新开、简报模板、接续段、额度、账号、Max 20x、5 小时窗口、每周额度、并发、按任务量开、不设个数上限、额度刹车、get_usage、用量读数、临近重置用足、Codex 额度、普通速度、额度见底、冻结、切换账号、D-184、D-191、D-193、D-196、D-197、D-226

- WF-MODEL §0 结论 · L22 · 904
  - ★ WF-MODEL §0.1 接入要点 · L34 · 278
- WF-MODEL §1 按七问选施工模型 · L42 · 762
- WF-MODEL §2 Opus 与 Sonnet 的分界（D-193） · L56 · 428
- WF-MODEL §3 机械活交谁 · L69 · 542
- WF-MODEL §4 推理强度按难度选 · L78 · 669
- WF-MODEL §5 升级、派单与直接做 · L93 · 417
- WF-MODEL §6 怎么开子代理 · L100 · 503
- WF-MODEL §7 子代理的生命周期：默认新开，靠任务单交接 · L108 · 447
- WF-MODEL §8 简报模板与"接续"段 · L118 · 794
  - WF-MODEL §8.1 简报模板 · L120 · 494
  - WF-MODEL §8.2 "接续"段 · L137 · 283
- WF-MODEL §9 额度与并发 · L147 · 1.0k
  - WF-MODEL §9.1 口径、开多少与刹车 · L149 · 429
  - WF-MODEL §9.2 节奏（D-196） · L157 · 207
  - WF-MODEL §9.3 Codex 账号（Astra 与 Sol） · L163 · 169
  - WF-MODEL §9.4 额度见底时冻结 · L168 · 223

## WF-ROLE · 参与者与分工

`docs/workflow/WF-ROLE-参与者与分工.md` · 4.7k 字 · 角色表与汇报关系，会话的运行方式，各模型的长短板，GPT 模型不做审美，决定权与事归谁，总指挥只做什么，Astra 当指挥
关键词：角色表、汇报给、决定权、执行人、会话、运行方式、单 Claude 账户、模型能力、长短板、Opus、Sonnet、gpt-6-astra、gpt-6.1-sol、GPT 不做审美、视觉风格归属、事归谁、交叉、总指挥、主代理、风格负责人、集中测试、Astra、工程指挥、元规则、D-179

- WF-ROLE §0 结论 · L22 · 533
  - ★ WF-ROLE §0.1 接入要点 · L29 · 213
- WF-ROLE §1 角色与运行方式 · L35 · 650
- WF-ROLE §2 模型的长处与短板 · L50 · 499
- WF-ROLE §3 GPT 模型不做审美工作 · L59 · 359
- WF-ROLE §4 决定权与事归谁 · L66 · 570
- WF-ROLE §5 总指挥只做什么 · L75 · 639
- WF-ROLE §6 Astra 当指挥，施工默认交 gpt-6.1-sol 子代理 · L89 · 744

## WF-RULE · 全员守则

`docs/workflow/WF-RULE-全员守则.md` · 6.5k 字 · 所有会话和子代理都要守的规则：不过度工程化、不向作者出选项、不预判时间、不干等、文件操作、git 与直接改主线、主线优先、测试最小、少打架、拿不准就问、写文档、一次性产物、bug 按严重程度修、精度按 demo 阶段定
关键词：全员守则、不过度工程化、不向作者出选项、不预判时间、不短轮询、不干等、不重复复核、文件操作、删除规则、git、直接改主线、只提交自己的文件、回滚只用 revert、不 stash、主线优先、测试最小、少和别人打架、一次迁完、拿不准就问、写文档、写记忆、D-201、D-168、D-167、D-199、D-209、D-211、一次性产物、用完就删、bug 分级、阻断、先提交后修、修不收敛就停、数量级判断、主观阈值、硬约束、demo 阶段、不照上线大型游戏、精度与画面相称、看得出差别、专项几分钟

- WF-RULE §0 结论 · L22 · 250
  - ★ WF-RULE §0.1 接入要点 · L26 · 59
- WF-RULE §1 不过度工程化（D-201） · L30 · 308
- WF-RULE §2 不向作者出选项 · L37 · 237
- WF-RULE §3 不预判时间 · L43 · 190
- WF-RULE §4 不短轮询、不干等、不重复复核 · L49 · 290
- WF-RULE §5 来信 · L55 · 74
- WF-RULE §6 文件操作 · L59 · 523
- WF-RULE §7 git 与直接改主线（D-211） · L69 · 794
- WF-RULE §8 主线优先（D-168） · L79 · 292
- WF-RULE §9 测试最小（D-167） · L86 · 366
- WF-RULE §10 少和别人打架（D-199） · L93 · 580
- WF-RULE §11 拿不准就问 · L103 · 137
- WF-RULE §12 写文档与记忆 · L108 · 247
- WF-RULE §13 一次性产物（D-221） · L116 · 457
- WF-RULE §14 按严重程度修 bug（D-223） · L123 · 384
- WF-RULE §15 精度按 demo 阶段定（D-225、D-231） · L132 · 496

## WF-STATUS · 施工状态、队列与调研入队

`docs/workflow/WF-STATUS-施工状态、队列与调研入队.md` · 4.6k 字 · 施工状态文件怎么填、施工队列的格式与即时维护、队列平衡、交接单、调研入队流水线与旁路单
关键词：施工队列、一行一件、即时维护、拆分重活、可派、进行中、待编译、等前置、暂停、行号、施工状态、状态文件、当前 main、最近可运行入口、已冻结的契约组、活跃工作包、队列平衡、积压、交接单、HANDOVER.md、接手、已集成、调研入队流水线、审计、调研简报、旁路单、D-224

- WF-STATUS §0 结论 · L23 · 607
  - ★ WF-STATUS §0.1 接入要点 · L32 · 231
- WF-STATUS §1 施工状态文件怎么填 · L39 · 799
- WF-STATUS §2 施工队列与队列平衡 · L60 · 922
- WF-STATUS §3 交接单 · L89 · 89
- WF-STATUS §4 调研入队流水线 · L93 · 1.4k
  - WF-STATUS §4.1 调研与旁路单 · L107 · 757

## ART-AMBIENCE · 日式日常街景：元素、分区与做法

`docs/art/ART-AMBIENCE-日式日常街景.md` · 9.7k 字 · 日式日常街景元素的边界与分区，各元素的形、色、限定与三渲二做法，以及对应的鸟与声音
关键词：日式街景、日式氛围、日系、日本味、电线杆、架空线、自动售货机、售货机光岛、弯道凸面镜、驻轮场式车棚、屋顶围网、高架水箱、通长雨庇、推拉铝窗、象牙色窗帘、开放外廊、次路标线、让行牌、L 形边沟、砌块墙、樱花行道树、绣球、夏季绿帘、杆上防犯灯、便利店夜景、暖帘、防球网、外墙圆钟、电车道口

- ART-AMBIENCE §0 结论 · L21 · 1.1k
  - ★ ART-AMBIENCE §0.1 接入要点 · L29 · 694
- ART-AMBIENCE §1 分区与通用约束 · L42 · 1.7k
  - ART-AMBIENCE §1.1 用与不用 · L44 · 224
  - ART-AMBIENCE §1.2 区域代号 · L49 · 461
  - ART-AMBIENCE §1.3 方式与通用约束 · L62 · 706
  - ART-AMBIENCE §1.4 颜色 · L75 · 338
- ART-AMBIENCE §2 校舍外立面与屋顶 · L86 · 970
- ART-AMBIENCE §3 外廊、楼梯与出入口 · L97 · 617
- ART-AMBIENCE §4 车棚、售货机与街角小设施 · L106 · 578
- ART-AMBIENCE §5 电线杆、架空线与线上的鸟 · L114 · 492
- ART-AMBIENCE §6 道路标线、凸面镜、护栏、路缘与排水 · L120 · 847
- ART-AMBIENCE §7 绿化 · L134 · 386
- ART-AMBIENCE §8 照明与夜景 · L142 · 468
- ART-AMBIENCE §10 河堤、球场设施、店头、外围街道与飞机云 · L151 · 720
- ART-AMBIENCE §11 声音 · L165 · 358
- ART-AMBIENCE §13 待验证 · L177 · 260

## ART-FIDELITY-DETAIL · 细节密度与贴脸做法

`docs/art/ART-FIDELITY-DETAIL-细节密度与贴脸做法.md` · 5.8k 字 · 每栋楼的构件与立面密度、窗、地面与热点簇、倒角表与不做旧、夜间四季天气，贴脸要求，穿楼入地，细节词汇与分工
关键词：细节密度、十类构件、立面密度、事件贴花、热点簇、倒角表、不做旧、贴脸距离、L0+ 专用几何、切面、细节词汇、几何贴花贴图着色器分工、防止细节堆成噪点、最值得做的细节

- ART-FIDELITY-DETAIL §0 结论 · L22 · 584
  - ★ ART-FIDELITY-DETAIL §0.1 接入要点 · L32 · 182
- ART-FIDELITY-DETAIL §1 细节密度（5–150 m） · L39 · 2.1k
  - ART-FIDELITY-DETAIL §1.1 每栋楼的构件层次 · L43 · 288
  - ART-FIDELITY-DETAIL §1.2 立面密度 · L49 · 240
  - ART-FIDELITY-DETAIL §1.3 窗 · L56 · 326
  - ART-FIDELITY-DETAIL §1.4 地面 · L63 · 299
  - ART-FIDELITY-DETAIL §1.5 倒角与不做旧 · L69 · 419
  - ART-FIDELITY-DETAIL §1.6 夜间与四季 · L75 · 216
  - ART-FIDELITY-DETAIL §1.7 天气 · L81 · 223
- ART-FIDELITY-DETAIL §2 贴脸距离（L0+，0.5–5 m） · L88 · 625
- ART-FIDELITY-DETAIL §3 镜头穿进楼体或钻入地下 · L103 · 365
- ART-FIDELITY-DETAIL §4 细节词汇（中国大学校园版） · L112 · 1.2k

## ART-FIDELITY · 视觉精度标准：处处不粗糙，细节按区分级

`docs/art/ART-FIDELITY-视觉精度标准.md` · 7.4k 字 · 处处成立的精度底线 B1–B13、五个精度区的判定距离与取用、一票否决、巡检点、行走走廊加密与外围分级；细节做法在子文档
关键词：视觉精度、精度区、K1–K5、判定距离、底线清单、B1–B13、不粗糙、一票否决、区界规则、可读规则、巡检点、行走走廊、外围分级

- ART-FIDELITY §0 结论 · L21 · 1.1k
  - ★ ART-FIDELITY §0.1 接入要点 · L37 · 398
- ART-FIDELITY §1 精度区与换算基准 · L46 · 2.1k
  - ART-FIDELITY §1.1 五个精度区 · L48 · 784
  - ART-FIDELITY §1.2 区界规则 · L60 · 187
  - ART-FIDELITY §1.3 细节项在各区的取用 · L67 · 442
  - ART-FIDELITY §1.4 划分与数据 · L79 · 224
  - ART-FIDELITY §1.5 换算基准 · L84 · 403
- ART-FIDELITY §2 底线 · L90 · 1.1k
  - ART-FIDELITY §2.0 底线清单（B1–B13） · L94 · 1.1k
- ART-FIDELITY §3 一票否决清单 · L114 · 857
- ART-FIDELITY §4 巡检点 · L140 · 230
- ART-FIDELITY §5 行走走廊 · L145 · 761
  - ART-FIDELITY §5.1 范围与画面构成 · L147 · 162
  - ART-FIDELITY §5.2 走廊里的加密 · L152 · 203
  - ART-FIDELITY §5.3 细节优先级 · L161 · 385
- ART-FIDELITY §6 外围分级精度 · L172 · 218

## ART-MODELING-PROPORTION · 风格化比例、颜色与金样

`docs/art/ART-MODELING-PROPORTION-风格化比例颜色与金样.md` · 5.4k 字 · 先定身份再放开，建筑、植被、道具的风格化比例与形体，固有色约束，金样、参考板与参数化部件库
关键词：风格化造型、风格化比例、身份特征、比例表、重复上限、防摩尔纹、人体尺度锚点、树冠团块、少粗圆干净、固有色、彩度上限、金样、参考板、参数化部件库、IPartDef、共同口号

- ART-MODELING-PROPORTION §0 结论 · L22 · 594
  - ★ ART-MODELING-PROPORTION §0.1 接入要点 · L30 · 288
- ART-MODELING-PROPORTION §1 先定身份，再放开 · L38 · 369
- ART-MODELING-PROPORTION §2 现代混凝土和石材楼的比例规则 · L44 · 813
- ART-MODELING-PROPORTION §3 植被、水面与地面 · L65 · 671
- ART-MODELING-PROPORTION §4 道具与小物件 · L75 · 625
- ART-MODELING-PROPORTION §5 颜色 · L86 · 277
- ART-MODELING-PROPORTION §6 让 20 多个建模者风格一致 · L93 · 887
- ART-MODELING-PROPORTION §7 待验证 · L109 · 119

## ART-MODELING · 风格化造型规则：比例、形体、部件与风格一致性

`docs/art/ART-MODELING-风格化造型规则.md` · 7.1k 字 · 每类对象的最低合格构件，几何细节的三道门槛、收边倒角、深度层、模块修边条、贴面几何、法线与换级；比例颜色与金样在子文档
关键词：最低合格构件、三道门槛、收边构件、倒角做法、自定义法线、硬边法线、解析法线、修边条、贴面几何、换级不跳变、深度层、避开描线临界角

- ART-MODELING §0 结论 · L21 · 834
  - ★ ART-MODELING §0.1 接入要点 · L33 · 360
- ART-MODELING §1 最低合格构件与法线规则 · L42 · 5.1k
  - ART-MODELING §1.1 三道门槛 · L48 · 483
  - ART-MODELING §1.2 收边与倒角 · L57 · 567
  - ART-MODELING §1.3 轮廓、深度层与截面 · L66 · 467
  - ART-MODELING §1.4 模块与修边条 · L76 · 560
  - ART-MODELING §1.5 贴面几何与顶点数据 · L87 · 496
  - ART-MODELING §1.6 法线 · L95 · 585
  - ART-MODELING §1.7 换级不跳变 · L107 · 382
  - ART-MODELING §1.8 每类对象的最低合格构件 · L118 · 1.3k
- ART-MODELING §2 待验证 · L132 · 149

## ART-RENOVATION · 老楼翻新：全部翻新的规则

`docs/art/ART-RENOVATION-老楼翻新.md` · 10.0k 字 · 老楼位置、体量与身份特征不动，外观全部换成新楼标准：会脏处换构件、材质色额、删简留、各部位改法、氛围与成电辨认、验收
关键词：老楼翻新、翻新、翻新预设、翻新楼、全部翻新、旧痕、删简留、防盗网、窗套、窗楣、窗台滴水、勒脚、压顶檐口、管槽、管廊、护窗栏、伸出杆、颜色配额、陶红墙带、浅奶咖墙、斑驳、面砖、楼梯间窗带、叶影上墙、成电辨认、翻新对照楼

- ART-RENOVATION §0 结论 · L21 · 968
  - ★ ART-RENOVATION §0.1 接入要点 · L31 · 508
- ART-RENOVATION §1 适用范围 · L41 · 734
  - ART-RENOVATION §1.1 哪些楼列入翻新 · L43 · 329
  - ART-RENOVATION §1.2 不动什么 · L49 · 174
  - ART-RENOVATION §1.3 能加多少 · L55 · 220
- ART-RENOVATION §2 总则 · L60 · 890
  - ART-RENOVATION §2.1 全部翻新，与新楼同标准 · L62 · 223
  - ART-RENOVATION §2.2 会脏的地方换成构件 · L68 · 388
  - ART-RENOVATION §2.3 生活感只靠干净的痕迹 · L81 · 270
- ART-RENOVATION §3 材质与配色 · L87 · 1.4k
  - ART-RENOVATION §3.1 材质族 · L89 · 274
  - ART-RENOVATION §3.2 每栋楼的颜色配额 · L100 · 128
  - ART-RENOVATION §3.3 取值 · L105 · 595
  - ART-RENOVATION §3.4 斑驳、笔触与顶点色 · L114 · 201
  - ART-RENOVATION §3.5 面砖 · L120 · 100
  - ART-RENOVATION §3.6 描线 · L124 · 130
- ART-RENOVATION §4 删、简、留 · L128 · 882
  - ART-RENOVATION §4.1 删：旧痕、杂物与防盗网 · L130 · 365
  - ART-RENOVATION §4.2 简：外机、管线、窗分格与标识 · L138 · 318
  - ART-RENOVATION §4.3 留：干净版的生活痕迹 · L146 · 187
- ART-RENOVATION §5 窗、檐口、楼梯间、屋顶与首层 · L150 · 1.3k
  - ART-RENOVATION §5.1 窗 · L152 · 376
  - ART-RENOVATION §5.2 檐口与女儿墙 · L161 · 181
  - ART-RENOVATION §5.3 楼梯间 · L168 · 275
  - ART-RENOVATION §5.4 屋顶 · L176 · 247
  - ART-RENOVATION §5.5 首层、入口与阳台 · L184 · 209
- ART-RENOVATION §6 氛围与成电辨认 · L191 · 993
  - ART-RENOVATION §6.1 画法上的氛围手法 · L193 · 513
  - ART-RENOVATION §6.2 日式物件上楼 · L207 · 224
  - ART-RENOVATION §6.3 保住"认得出是成电" · L213 · 242
- ART-RENOVATION §7 示例：三种楼型 · L220 · 1.3k
  - ART-RENOVATION §7.1 老式宿舍楼 · L224 · 390
  - ART-RENOVATION §7.2 老教学楼 · L232 · 426
  - ART-RENOVATION §7.3 老食堂 · L240 · 430
- ART-RENOVATION §8 验收 · L248 · 163
- ART-RENOVATION §9 待验证 · L254 · 233

## ART-SKYLIGHT-ATMOS · 天空、月亮与雾

`docs/art/ART-SKYLIGHT-ATMOS-天空月亮与雾.md` · 5.2k 字 · 天空渐变、霾带、云、星空、银河、城市天光、日月盘，月亮外观，雾与大气透视的共用雾函数、边缘雾、雾幕与地标豁免
关键词：天空、sky shader、天空渐变、霾带、云、飞机云、星空、银河、城市天光、夜空明度下限、阴天云盖、雨幕、闪电、彩虹、日月盘、月亮外观、月相、雾、大气透视、共用雾函数、边缘雾、雾幕、雾海、地标豁免

- ART-SKYLIGHT-ATMOS §0 结论 · L22 · 753
  - ★ ART-SKYLIGHT-ATMOS §0.1 接入要点 · L30 · 406
- ART-SKYLIGHT-ATMOS §1 天空 · L39 · 2.0k
- ART-SKYLIGHT-ATMOS §2 月亮 · L65 · 253
- ART-SKYLIGHT-ATMOS §3 雾与大气透视 · L73 · 1.3k

## ART-SKYLIGHT · 天空、光照与大气

`docs/art/ART-SKYLIGHT-天空光照与大气.md` · 8.4k 字 · 默认状态的光与太阳、主光与阴影、天光、受光退场、人造光、光效清单与冷暖对照；天空与雾见子文档
关键词：主光、日月交接、无影蓝调、阴影、月影、天光、天光方向、退光线、受光退场、人造光、路灯光斑、窗灯、光效清单、光柱、镜头光斑、冷暖对照

- ART-SKYLIGHT §0 结论 · L21 · 1.2k
  - ★ ART-SKYLIGHT §0.1 接入要点 · L38 · 489
- ART-SKYLIGHT §1 默认状态的光 · L48 · 321
- ART-SKYLIGHT §2 太阳本体与主光色 · L56 · 384
- ART-SKYLIGHT §3 光照骨架 · L63 · 3.0k
  - ART-SKYLIGHT §3.1 主光 · L65 · 1.3k
  - ART-SKYLIGHT §3.2 天光（环境光） · L79 · 212
  - ART-SKYLIGHT §3.3 受光分布 · L83 · 452
  - ART-SKYLIGHT §3.4 受光退场的顺序 · L90 · 192
  - ART-SKYLIGHT §3.5 人造光 · L96 · 796
- ART-SKYLIGHT §4 光效清单 · L115 · 1.5k
- ART-SKYLIGHT §5 冷暖对照 · L134 · 580
- ART-SKYLIGHT §6 待验证 · L158 · 243

## ART-STYLE-LINES · 描线与后期

`docs/art/ART-STYLE-LINES-描线与后期.md` · 3.6k 字 · 后处理描线的线类、线色、线宽与折角线，凸边亮线；后期的风格底线与可关项、柔光色调映射光斑移轴与抗锯齿
关键词：描线、线类、线色、线宽、材质 ID、后处理描边、折角线、凸边亮线、风格底线、柔光、色调映射、镜头光斑、移轴景深、窗灯光晕、光柱、抗锯齿、暗角

- ART-STYLE-LINES §0 结论 · L22 · 487
  - ★ ART-STYLE-LINES §0.1 接入要点 · L30 · 186
- ART-STYLE-LINES §1 描线 · L35 · 1.3k
  - ART-STYLE-LINES §1.1 描哪些线 · L39 · 258
  - ART-STYLE-LINES §1.2 线色 · L47 · 139
  - ART-STYLE-LINES §1.3 线宽 · L52 · 214
  - ART-STYLE-LINES §1.4 逐材质调线与轻重 · L56 · 94
  - ART-STYLE-LINES §1.5 后处理描边 · L61 · 196
  - ART-STYLE-LINES §1.6 折角线与低模 · L67 · 162
  - ART-STYLE-LINES §1.7 凸边亮线 · L73 · 204
- ART-STYLE-LINES §2 后期 · L79 · 967
  - ART-STYLE-LINES §2.1 柔光：白天只晕高光 · L85 · 119
  - ART-STYLE-LINES §2.2 色调映射 · L89 · 111
  - ART-STYLE-LINES §2.3 镜头光斑 · L94 · 101
  - ART-STYLE-LINES §2.4 移轴景深 · L98 · 48
  - ART-STYLE-LINES §2.5 窗灯光晕 · L102 · 71
  - ART-STYLE-LINES §2.6 特效形态与光柱 · L106 · 216
  - ART-STYLE-LINES §2.7 抗锯齿 · L111 · 81
  - ART-STYLE-LINES §2.8 暗角与分调 · L115 · 72

## ART-STYLE-SHADING · 明暗、影色与材质表现

`docs/art/ART-STYLE-SHADING-明暗影色与材质.md` · 5.7k 字 · 卡通着色的明暗阶数、法线、倒角提亮，影色的推导，玻璃金属边缘光透光湿面，贴图与笔触，材质速查
关键词：卡通材质、toon.gdshaderinc、明暗分阶、接触影、影色推导、室内暖影、倒角提亮、法线展平、边缘光、透光、湿面、斑驳、构造图案线、不用法线贴图、材质速查

- ART-STYLE-SHADING §0 结论 · L22 · 636
  - ★ ART-STYLE-SHADING §0.1 接入要点 · L31 · 300
- ART-STYLE-SHADING §1 明暗分阶 · L38 · 1.5k
  - ART-STYLE-SHADING §1.1 阶数 · L40 · 485
  - ART-STYLE-SHADING §1.2 法线展平 · L54 · 174
  - ART-STYLE-SHADING §1.3 树冠法线 · L62 · 70
  - ART-STYLE-SHADING §1.4 平涂与渐变 · L66 · 152
  - ART-STYLE-SHADING §1.5 统一的卡通着色 · L72 · 291
  - ART-STYLE-SHADING §1.6 倒角提亮与只改明暗的修正 · L79 · 309
- ART-STYLE-SHADING §2 影子：明度为主，按材质定影色 · L89 · 1.2k
  - ART-STYLE-SHADING §2.1 原则 · L91 · 267
  - ART-STYLE-SHADING §2.2 推导规则 · L99 · 443
  - ART-STYLE-SHADING §2.3 天光与地面反光 · L109 · 78
  - ART-STYLE-SHADING §2.4 室内暖影 · L113 · 134
  - ART-STYLE-SHADING §2.5 哪些材质有色调变化 · L118 · 134
  - ART-STYLE-SHADING §2.6 夜间与阴雨 · L122 · 115
- ART-STYLE-SHADING §3 高光、边缘光与透光 · L127 · 744
  - ART-STYLE-SHADING §3.1 玻璃 · L129 · 144
  - ART-STYLE-SHADING §3.2 金属亮带 · L134 · 70
  - ART-STYLE-SHADING §3.3 边缘光 · L138 · 179
  - ART-STYLE-SHADING §3.4 湿面与积水 · L144 · 192
  - ART-STYLE-SHADING §3.5 透光 · L150 · 143
- ART-STYLE-SHADING §4 贴图与笔触 · L155 · 624
  - ART-STYLE-SHADING §4.1 禁止照片纹理 · L157 · 46
  - ART-STYLE-SHADING §4.2 斑驳、笔触与构造图案线 · L161 · 331
  - ART-STYLE-SHADING §4.3 色数限额 · L168 · 128
  - ART-STYLE-SHADING §4.4 不用法线贴图 · L173 · 107
- ART-STYLE-SHADING §5 材质速查 · L177 · 159

## ART-STYLE · 风格基础：三渲二日式动画背景规范

`docs/art/ART-STYLE-风格基础.md` · 5.7k 字 · 全局画风的目标与参照、调色板与色名用法、排除元素、场景文字与情绪；明暗影色材质与描线后期在子文档
关键词：三渲二、清透干净、质感参照、色名、退役色名、跳色、色名登记、界面色、日本元素边界、场景文字、原创徽记、情绪弧线

- ART-STYLE §0 结论 · L21 · 814
  - ★ ART-STYLE §0.1 接入要点 · L34 · 303
- ART-STYLE §1 风格目标与参照 · L42 · 687
  - ART-STYLE §1.1 质感参照场景 · L50 · 379
- ART-STYLE §2 调色板 · L57 · 1.6k
  - ART-STYLE §2.1 色名分类与退役色名 · L59 · 355
  - ART-STYLE §2.2 大面积色与跳色 · L66 · 379
  - ART-STYLE §2.3 固有色与推导 · L78 · 188
  - ART-STYLE §2.4 界面色 · L84 · 145
  - ART-STYLE §2.5 色名登记 · L89 · 366
  - ART-STYLE §2.6 天气色 · L96 · 126
- ART-STYLE §3 排除元素 · L101 · 913
  - ART-STYLE §3.1 日本元素的边界 · L103 · 701
  - ART-STYLE §3.2 其他排除项 · L117 · 201
- ART-STYLE §4 场景内的文字与标识 · L123 · 328
- ART-STYLE §5 情绪弧线 · L131 · 447

## WLD-DIORAMA · 箱庭边界、截面与远景

`docs/world/WLD-DIORAMA-箱庭边界截面与远景.md` · 8.6k 字 · 箱庭外缘的地层截面、水柱截面、雾海与清水河银带、360° 远景和雪山，及其随时段、季节、天气的变化
关键词：箱庭边界、地层截面、剖面、截面上缘、地下水线、卵石层、河渠水柱截面、雾海、雾海顶面、雾天雾海上升、清水河银带、深夜雾海微光、远景、远景视差、色调层、坡向层、发光层、雪山、日照金山、雪线、雪山可见度、backdrop.json、diorama_section、diorama_far、Camera3D.Far

- WLD-DIORAMA §0 结论 · L21 · 1.6k
  - ★ WLD-DIORAMA §0.1 接入要点 · L34 · 687
- WLD-DIORAMA §1 截面线与校界 · L46 · 135
- WLD-DIORAMA §2 地层截面 · L50 · 1.3k
  - WLD-DIORAMA §2.1 截面的受光与阴影 · L68 · 307
- WLD-DIORAMA §3 河渠水柱截面 · L76 · 388
- WLD-DIORAMA §4 截面下缘与雾海 · L82 · 1.0k
- WLD-DIORAMA §5 远景（360° 不留空白） · L93 · 1.7k
- WLD-DIORAMA §6 雪山 · L124 · 981
- WLD-DIORAMA §7 待验证（P0） · L135 · 251

## WLD-INTERIORS-ACCESS · 室内开放：原则、门、关着的楼、新开一栋

`docs/world/WLD-INTERIORS-ACCESS-室内开放与门.md` · 6.3k 字 · 室内的精选开放原则、门的区分与自动门、关着的楼、导航图与新开一栋楼的做法
关键词：楼内、开放室内、开放清单、interiors.json、开放入口、自动门、此处暂不开放、封口、门厅卡、窗卡、简化室内、I0 兜底层、看进去例外、导航图、G19

- WLD-INTERIORS-ACCESS §0 结论 · L23 · 890
  - ★ WLD-INTERIORS-ACCESS §0.1 接入要点 · L32 · 522
- WLD-INTERIORS-ACCESS §1 开放原则与共用约定 · L41 · 952
- WLD-INTERIORS-ACCESS §2 门的区分：开与关 · L55 · 1.4k
  - WLD-INTERIORS-ACCESS §2.1 楼里的门：走近推开，闭门器关上 · L71 · 665
- WLD-INTERIORS-ACCESS §3 关着的楼：外壳、窗卡、门厅卡与简化室内 · L81 · 822
- WLD-INTERIORS-ACCESS §4 导航图、路线与扩展 · L91 · 815

## WLD-INTERIORS-BUILD · 室内制作：加载、生成器、风格、痕迹、楼梯与碰撞

`docs/world/WLD-INTERIORS-BUILD-室内制作.md` · 6.4k 字 · 室内的加载与进楼衔接、平面生成器、风格与光、生活痕迹、楼梯门洞与碰撞
关键词：室内加载、进楼衔接、平面内核、楼型、室内风格、窗光斑、光柱、室内色名、生活痕迹、楼梯碰撞、SlopedWallCollider、门洞、落点

- WLD-INTERIORS-BUILD §0 结论 · L23 · 456
  - ★ WLD-INTERIORS-BUILD §0.1 接入要点 · L30 · 245
- WLD-INTERIORS-BUILD §1 加载与进楼衔接 · L37 · 938
  - WLD-INTERIORS-BUILD §1.1 整体加载、按层显示 · L39 · 243
  - WLD-INTERIORS-BUILD §1.2 触发 · L45 · 274
  - WLD-INTERIORS-BUILD §1.3 进楼与出楼，以及衔接保证（ENG-PERF-STREAM §1.2 第 6 条） · L52 · 407
- WLD-INTERIORS-BUILD §2 平面生成器与类型库 · L60 · 734
- WLD-INTERIORS-BUILD §3 风格 · L69 · 1.4k
  - WLD-INTERIORS-BUILD §3.1 明暗与暖影 · L73 · 224
  - WLD-INTERIORS-BUILD §3.2 窗光斑与光（最重要的室内画面） · L77 · 418
  - WLD-INTERIORS-BUILD §3.3 色名、识别要素与夜间 · L85 · 597
- WLD-INTERIORS-BUILD §4 生活痕迹规则 · L91 · 613
- WLD-INTERIORS-BUILD §5 楼梯、门洞与碰撞 · L100 · 914
- WLD-INTERIORS-BUILD §6 待验证 · L118 · 217

## WLD-INTERIORS · 室内：开放与制作

`docs/world/WLD-INTERIORS-室内.md` · 1.5k 字 · 不分图的室内规则目录：开放（原则、门、关着的楼、新开一栋）与制作（加载、生成器、风格、碰撞）
关键词：室内、精选开放

- WLD-INTERIORS §0 结论 · L22 · 301
  - ★ WLD-INTERIORS §0.1 接入要点 · L31 · 39

## WLD-LIFE-FAUNA · 动物与生活痕迹：动物、反应动物、痕迹、显隐

`docs/world/WLD-LIFE-FAUNA-动物与生活痕迹.md` · 6.4k 字 · 全校动物与反应动物、室外生活痕迹，以及随时段、季节、天气的显隐做法
关键词：动物、鸟、校园猫、鸽群、反应动物、黄昏乌鸦、季节性动物、生活痕迹、晾晒、黑天鹅、雨天生活痕迹、小心地滑、日夜两套、变体掩码、state_bits、show_ratio

- WLD-LIFE-FAUNA §0 结论 · L23 · 697
  - ★ WLD-LIFE-FAUNA §0.1 接入要点 · L30 · 368
- WLD-LIFE-FAUNA §1 动物 · L38 · 2.3k
  - WLD-LIFE-FAUNA §1.1 反应动物：猫、鸽群与水禽 · L64 · 870
- WLD-LIFE-FAUNA §2 生活痕迹 · L81 · 1.4k
- WLD-LIFE-FAUNA §3 显隐：随时段、季节、天气的做法 · L96 · 845
- WLD-LIFE-FAUNA §4 待验证（P0） · L110 · 212

## WLD-LIFE-STREET · 停着的车与街道家具：车、家具、碰撞

`docs/world/WLD-LIFE-STREET-停着的车与街道家具.md` · 4.6k 字 · 停着的车与匿名共享单车、街道家具的设计，行走模式下的碰撞与 Foot 材质、声音数据
关键词：停着的车、校车、街道家具、分类垃圾箱、路灯灯具、售货机、外卖柜、长椅、声音插座、家具碰撞、共享单车停放点

- WLD-LIFE-STREET §0 结论 · L23 · 611
  - ★ WLD-LIFE-STREET §0.1 接入要点 · L28 · 470
- WLD-LIFE-STREET §1 停着的车 · L37 · 788
- WLD-LIFE-STREET §2 街道家具 · L47 · 1.6k
- WLD-LIFE-STREET §3 行走模式下的碰撞 · L60 · 679
- WLD-LIFE-STREET §4 待验证（P0） · L75 · 59

## WLD-LIFE · 动物、生活痕迹与街道家具

`docs/world/WLD-LIFE-动物生活痕迹与街道家具.md` · 1.7k 字 · 动物、生活痕迹、停着的车与街道家具的目录：共同底线，动物痕迹与车家具分子文档
关键词：动物与生活痕迹总则、艺术化处理、世界里不画人

- WLD-LIFE §0 结论 · L22 · 665
  - ★ WLD-LIFE §0.1 接入要点 · L34 · 39

## WLD-OUTSKIRTS-BAND · 外围环境带：截面、分层与精度、灯光季节、保留删除新增、画法、数据

`docs/world/WLD-OUTSKIRTS-BAND-外围环境带.md` · 6.4k 字 · 截面线位置规则与截面精度、外围 O0–O3 的内容与精度、灯光季节车流的通用规则、保留删除新增、外围画法、数据与许可
关键词：外围环境带、O0–O3、距离环、截面精度、截面厚度、外围覆盖层、outskirts.json、外围画法、楼型、程序化布局、ODbL

- WLD-OUTSKIRTS-BAND §0 结论 · L23 · 802
  - ★ WLD-OUTSKIRTS-BAND §0.1 接入要点 · L30 · 330
- WLD-OUTSKIRTS-BAND §1 截面线与截面精度 · L37 · 627
  - WLD-OUTSKIRTS-BAND §1.1 截面线的位置规则 · L39 · 184
  - WLD-OUTSKIRTS-BAND §1.2 截面精度 · L44 · 428
- WLD-OUTSKIRTS-BAND §2 外围分层 O0–O3 · L52 · 1.3k
  - WLD-OUTSKIRTS-BAND §2.1 判定原则：距离环 · L54 · 257
  - WLD-OUTSKIRTS-BAND §2.2 分层表 · L58 · 620
  - WLD-OUTSKIRTS-BAND §2.3 楼型与程序化布局 · L69 · 380
- WLD-OUTSKIRTS-BAND §3 灯光、季节与车流 · L76 · 791
  - WLD-OUTSKIRTS-BAND §3.1 灯光（夜间） · L78 · 191
  - WLD-OUTSKIRTS-BAND §3.2 时间、季节、天气 · L85 · 375
  - WLD-OUTSKIRTS-BAND §3.3 不画车流 · L94 · 210
- WLD-OUTSKIRTS-BAND §4 保留、删除与新增 · L99 · 530
  - WLD-OUTSKIRTS-BAND §4.1 删掉的 · L105 · 202
  - WLD-OUTSKIRTS-BAND §4.2 新增的规则 · L109 · 147
- WLD-OUTSKIRTS-BAND §5 外围的画法：清透、细线 · L115 · 465
- WLD-OUTSKIRTS-BAND §6 数据与许可 · L124 · 443
- WLD-OUTSKIRTS-BAND §7 待验证 · L130 · 55

## WLD-OUTSKIRTS-BORDER · 边界阻挡与提示：行走阻挡、飞行阻挡、提示文案

`docs/world/WLD-OUTSKIRTS-BORDER-边界阻挡与提示.md` · 5.3k 字 · 校界的行走阻挡与缓停、旁观飞行的可达空间与推回、edge_fog、提示文案、多地图衔接
关键词：行走阻挡、飞行阻挡、缓停、推回、缓冲带、软地板、可达空间、活动范围、edge_fog、边缘雾、校外暂未开放、前方暂未开放、合法化、地图入口

- WLD-OUTSKIRTS-BORDER §0 结论 · L23 · 692
  - ★ WLD-OUTSKIRTS-BORDER §0.1 接入要点 · L28 · 463
- WLD-OUTSKIRTS-BORDER §1 行走阻挡 · L35 · 781
  - WLD-OUTSKIRTS-BORDER §1.1 挡什么（WP-30、WP-44） · L37 · 428
  - WLD-OUTSKIRTS-BORDER §1.2 0.6 m 内的缓停（WP-42） · L47 · 277
  - WLD-OUTSKIRTS-BORDER §1.3 兜底 · L52 · 65
- WLD-OUTSKIRTS-BORDER §2 飞行阻挡（旁观模式） · L56 · 1.5k
  - WLD-OUTSKIRTS-BORDER §2.1 可达空间 · L60 · 396
  - WLD-OUTSKIRTS-BORDER §2.2 缓冲带与推回（WP-06） · L68 · 407
  - WLD-OUTSKIRTS-BORDER §2.3 边缘雾 `edge_fog` · L77 · 389
  - WLD-OUTSKIRTS-BORDER §2.4 参数、合法化与不受约束的镜头 · L84 · 337
- WLD-OUTSKIRTS-BORDER §3 提示文案与多地图衔接 · L90 · 783
  - WLD-OUTSKIRTS-BORDER §3.1 提示文案 · L92 · 456
  - WLD-OUTSKIRTS-BORDER §3.2 与多地图、平面地图、存档的衔接 · L104 · 310

## WLD-OUTSKIRTS · 边界与外围：大箱庭、外围环境带、阻挡与提示

`docs/world/WLD-OUTSKIRTS-边界与外围.md` · 2.4k 字 · 大箱庭的三条线（校界、前沿线、截面线）；外围环境带、边界阻挡与提示子文档的入口
关键词：外围、校外、校界、边界、前沿线、截面线、大箱庭

- WLD-OUTSKIRTS §0 结论 · L22 · 812
  - ★ WLD-OUTSKIRTS §0.1 接入要点 · L34 · 204
- WLD-OUTSKIRTS §1 三条线 · L39 · 401

## WLD-TERRAIN-APPLY · 地形落地：表达合成、贴地、可走性、着色

`docs/world/WLD-TERRAIN-APPLY-地形落地.md` · 6.4k 字 · 地形落到画面和碰撞的规则：矢量操作与合成、贴地与楼宇垫层、可走性与碰撞、着色
关键词：地形操作、地形覆盖层、terrain.json、合成顺序、先平后贴、贴地、垫层、勒脚、散水、PadY、Steep、碰撞带、分面平法线、地面着色

- WLD-TERRAIN-APPLY §0 结论 · L23 · 675
  - ★ WLD-TERRAIN-APPLY §0.1 接入要点 · L30 · 396
- WLD-TERRAIN-APPLY §1 地形的表达与合成 · L39 · 1.3k
- WLD-TERRAIN-APPLY §2 贴地 · L54 · 1.5k
  - WLD-TERRAIN-APPLY §2.1 先平后贴 · L56 · 333
  - WLD-TERRAIN-APPLY §2.2 道路与铺装 · L69 · 269
  - WLD-TERRAIN-APPLY §2.3 楼宇：垫层、勒脚与入口 · L76 · 590
  - WLD-TERRAIN-APPLY §2.4 树与道具落地 · L85 · 257
- WLD-TERRAIN-APPLY §3 可走性与碰撞 · L92 · 665
- WLD-TERRAIN-APPLY §4 地面着色与描线 · L101 · 881
- WLD-TERRAIN-APPLY §5 待验证 · L121 · 341

## WLD-TERRAIN-SHAPE · 地形设计：原则、分层、竖直落差、水位与岸型

`docs/world/WLD-TERRAIN-SHAPE-地形设计.md` · 7.1k 字 · 地形设计规则：设计原则、高度预算与分层、手感刻度、坡度类、竖直落差、水位与岸型
关键词：地形设计原则、地形分层、高度预算、主模数、手感刻度、灰区、坡度类、零带、竖直落差、台缘带、退台、常水位、岸型、涉水带、开放泳区出口岸型、遮挡与揭示

- WLD-TERRAIN-SHAPE §0 结论 · L23 · 573
  - ★ WLD-TERRAIN-SHAPE §0.1 接入要点 · L30 · 240
- WLD-TERRAIN-SHAPE §1 设计原则 · L36 · 929
  - WLD-TERRAIN-SHAPE §1.1 总骨架 · L38 · 189
  - WLD-TERRAIN-SHAPE §1.2 规则 · L44 · 729
- WLD-TERRAIN-SHAPE §2 高度预算与分层 · L58 · 1.8k
  - WLD-TERRAIN-SHAPE §2.1 分层 · L60 · 493
  - WLD-TERRAIN-SHAPE §2.2 手感刻度 · L73 · 499
  - WLD-TERRAIN-SHAPE §2.3 坡度类 · L88 · 494
  - WLD-TERRAIN-SHAPE §2.4 全图范围与零带 · L101 · 258
- WLD-TERRAIN-SHAPE §3 竖直落差 · L108 · 1.2k
  - WLD-TERRAIN-SHAPE §3.1 连接规则 · L110 · 509
  - WLD-TERRAIN-SHAPE §3.2 台缘带（v1 的挡墙） · L126 · 337
  - WLD-TERRAIN-SHAPE §3.3 退台与高墙 · L133 · 184
  - WLD-TERRAIN-SHAPE §3.4 防护与谁来做 · L138 · 196
- WLD-TERRAIN-SHAPE §4 水位与岸型 · L143 · 1.6k
  - WLD-TERRAIN-SHAPE §4.1 常水位 · L145 · 271
  - WLD-TERRAIN-SHAPE §4.2 岸型 · L151 · 550
  - WLD-TERRAIN-SHAPE §4.3 涉水带、湖盆与河渠 · L162 · 346
  - WLD-TERRAIN-SHAPE §4.4 穿出校界 · L168 · 110
  - WLD-TERRAIN-SHAPE §4.5 开放泳区的出口岸型与支撑面 · L172 · 277
- WLD-TERRAIN-SHAPE §5 待验证 · L178 · 128

## WLD-TERRAIN · 地形：设计规则与落地规则

`docs/world/WLD-TERRAIN-地形.md` · 1.4k 字 · 不分图的地形规则目录：设计（原则、分层、竖直落差、岸型）与落地（合成、贴地、可走性、着色）
关键词：地形、地形规则

- WLD-TERRAIN §0 结论 · L22 · 398
  - ★ WLD-TERRAIN §0.1 接入要点 · L32 · 39

## WLD-VEGETATION · 植被：树冠画法、草坪、地面覆盖与季相

`docs/world/WLD-VEGETATION-植被.md` · 9.9k 字 · 树木、灌木、草坪与水生植物的观感规则、树种色名与季相行、地面覆盖、季相与天气、碰撞与声音数据
关键词：植被、植物、树冠画法、云团型、插片型、冬季裸枝、叶量、开花、季相、局部季节、四季变色、草坪、绿篱、荷、芦苇、落叶地毯、树冠遮蔽、覆雪、过渡绿地、species.json

- WLD-VEGETATION §0 结论 · L22 · 1.3k
  - ★ WLD-VEGETATION §0.1 接入要点 · L34 · 643
- WLD-VEGETATION §1 卡通树冠的画法 · L46 · 1.4k
  - WLD-VEGETATION §1.1 团块与三类树冠 · L50 · 312
  - WLD-VEGETATION §1.2 明暗与法线 · L59 · 160
  - WLD-VEGETATION §1.3 逆光 · L65 · 279
  - WLD-VEGETATION §1.4 描线、远景与树干 · L73 · 206
  - WLD-VEGETATION §1.5 冬季裸枝 · L79 · 379
- WLD-VEGETATION §2 树种与四季 · L87 · 944
  - WLD-VEGETATION §2.1 色名与季相行 · L95 · 571
- WLD-VEGETATION §3 草坪 · L101 · 1.4k
  - WLD-VEGETATION §3.1 颜色与长影 · L103 · 220
  - WLD-VEGETATION §3.2 开阔草坪 · L108 · 323
  - WLD-VEGETATION §3.3 积雪与季节花境 · L116 · 309
  - WLD-VEGETATION §3.4 过渡绿地的草本与草面（D-185） · L121 · 231
  - WLD-VEGETATION §3.5 草叶的表示与交界（D-205，取值是初值，样板同位姿定） · L128 · 290
- WLD-VEGETATION §4 落叶、花瓣与积雪（地面覆盖） · L133 · 745
- WLD-VEGETATION §5 水生植物 · L143 · 464
- WLD-VEGETATION §6 季相与天气 · L158 · 1.1k
  - WLD-VEGETATION §6.1 逐株错相与叶量 · L160 · 193
  - WLD-VEGETATION §6.2 季节颜色 · L165 · 194
  - WLD-VEGETATION §6.3 摆动与飘落（给 SYS-MOTION 的参数） · L170 · 160
  - WLD-VEGETATION §6.4 覆雪 · L175 · 309
  - WLD-VEGETATION §6.5 换季与天气切换 · L181 · 237
- WLD-VEGETATION §7 碰撞、画面设置与声音 · L187 · 1.0k
  - WLD-VEGETATION §7.1 行走碰撞 · L189 · 361
  - WLD-VEGETATION §7.2 画面设置（SYS-GRAPHICS） · L198 · 362
  - WLD-VEGETATION §7.3 声音的接口（SYS-AUDIO） · L205 · 292
- WLD-VEGETATION §8 待验证 · L214 · 80

## WLD-WATER · 水体：动画式水面画法、反光带与倒影、涉水与泳区规则

`docs/world/WLD-WATER-水体.md` · 6.9k 字 · 动画式水面画法、反光带与倒影、四季天气、涉水与开放泳区规则、水体系统规则
关键词：水体、水面、开放泳区、波纹线、主光反光带、碎金带、月光银带、四芒星闪点、泡沫线、倒影、平面反射、伪深度色带、雨点环、涉水涟漪、入水水花、灯的竖条倒影、refl_lamps

- WLD-WATER §0 结论 · L22 · 1.2k
  - ★ WLD-WATER §0.1 接入要点 · L33 · 582
- WLD-WATER §1 水面的通用画法 · L43 · 727
  - WLD-WATER §1.1 底色与色带 · L47 · 357
  - WLD-WATER §1.2 波纹线与泡沫线 · L53 · 284
- WLD-WATER §2 反光带、灯条与倒影 · L59 · 1.0k
  - WLD-WATER §2.1 主光反光带 · L61 · 441
  - WLD-WATER §2.2 灯的竖条倒影 · L69 · 169
  - WLD-WATER §2.3 倒影 · L73 · 402
- WLD-WATER §3 四季与天气的水面 · L79 · 834
- WLD-WATER §4 涉水、开放泳区与游泳反馈 · L106 · 857
  - WLD-WATER §4.1 涉水涟漪 · L108 · 194
  - WLD-WATER §4.2 开放泳区与出口 · L112 · 285
  - WLD-WATER §4.3 游泳的水面反馈 · L118 · 359
- WLD-WATER §5 水体系统的规则 · L126 · 697
- WLD-WATER §6 待验证 · L136 · 215

## MAP-QSH-ADDITIONS-ATMOSPHERE · 夜间灯光总图与雨天增补

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-ATMOSPHERE-夜间灯光与雨天.md` · 4.6k 字 · 夜间灯光的分级与颜色、初灯组与逐盏调度、雨夜雾夜，以及雨帘避雨处与积水点的放置约定
关键词：夜间灯光总图、照度三级、光斑层次、冷白两类、初灯组、逐盏调度、lighting-schedule.json、雨夜、雾夜、竖条倒影、灯锥、雨帘、避雨处、滴水点、干湿一刀切、积水点、积水放置、英雄帧

- MAP-QSH-ADDITIONS-ATMOSPHERE §0 结论 · L23 · 391
  - ★ MAP-QSH-ADDITIONS-ATMOSPHERE §0.1 接入要点 · L30 · 150
- MAP-QSH-ADDITIONS-ATMOSPHERE §1 夜间灯光总图：级别与颜色（主线之后） · L35 · 923
- MAP-QSH-ADDITIONS-ATMOSPHERE §2 初灯组与逐盏调度（主线之后） · L55 · 310
- MAP-QSH-ADDITIONS-ATMOSPHERE §3 雨夜与雾夜（主线之后） · L61 · 546
- MAP-QSH-ADDITIONS-ATMOSPHERE §4 雨帘与避雨处（主线之后） · L71 · 767
- MAP-QSH-ADDITIONS-ATMOSPHERE §5 积水点（主线之后） · L87 · 532
- MAP-QSH-ADDITIONS-ATMOSPHERE §6 待验证（主线之后） · L96 · 127

## MAP-QSH-ADDITIONS-VOCAB · 设计增补词汇与楼宇规则

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-VOCAB-增补词汇与楼宇规则.md` · 7.8k 字 · 按场地分类的增补物词汇（宿舍食堂商业街、道路停车场、河渠湖岸桥、草坪楼间屋顶、运动区校界步行设施）和普通楼宇设计十二条
关键词：增补词汇、休憩平台、电动车充电棚、外卖挂架、徽记井盖、亲水台阶、节制闸、草阶看台、紫藤廊架、桥头卧石、读书石台、汀步、下凹雨水花园、风雨连廊、屋顶实验花园、透空栏杆、导览亭、导览桩、直饮水台、休息点、楼宇十二条、入口雨棚、学院竖牌、西立面竖向鳍片、空调机位格栅、架空层、勒脚与散水、后勤小楼

- MAP-QSH-ADDITIONS-VOCAB §0 结论 · L23 · 393
  - ★ MAP-QSH-ADDITIONS-VOCAB §0.1 接入要点 · L30 · 105
- MAP-QSH-ADDITIONS-VOCAB §1 宿舍组团、食堂与商业街前 · L34 · 946
- MAP-QSH-ADDITIONS-VOCAB §2 道路、路口与停车场 · L52 · 728
- MAP-QSH-ADDITIONS-VOCAB §3 河渠、湖岸与桥 · L65 · 1.0k
- MAP-QSH-ADDITIONS-VOCAB §4 草坪、楼间空地与屋顶 · L82 · 867
- MAP-QSH-ADDITIONS-VOCAB §5 运动区、校界与步行服务设施 · L98 · 1.4k
- MAP-QSH-ADDITIONS-VOCAB §6 让普通楼宇有设计感 · L120 · 1.1k
- MAP-QSH-ADDITIONS-VOCAB §7 待验证 · L137 · 71

## MAP-QSH-ADDITIONS · 设计增补：通用规则与区文档共同口径

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-设计增补.md` · 7.9k 字 · 全校设计增补的通用规则（登记、用色、默认状态与作用代号、空地整理、转译、禁做、新颜色、行走分类）和五个区文档共用的身份特征、近景与四季昼夜口径
关键词：设计增补、增补物、增补登记、作用代号、默认状态规则、大片空地、功能开敞面、过渡绿地、参照与转译、禁做清单、新颜色登记、可跳上、增补物行走分类、区文档共同口径、身份特征、近景要求

- MAP-QSH-ADDITIONS §0 结论 · L22 · 1.0k
  - ★ MAP-QSH-ADDITIONS §0.1 接入要点 · L39 · 320
- MAP-QSH-ADDITIONS §1 通用规则 · L46 · 428
- MAP-QSH-ADDITIONS §2 默认状态规则与作用代号 · L53 · 891
- MAP-QSH-ADDITIONS §3 大片空地的整理 · L79 · 489
- MAP-QSH-ADDITIONS §4 参照与转译规则 · L88 · 709
- MAP-QSH-ADDITIONS §5 不建议增补（禁做清单） · L99 · 812
- MAP-QSH-ADDITIONS §6 需要登记的新颜色 · L121 · 573
- MAP-QSH-ADDITIONS §7 增补物的行走分类 · L141 · 571
- MAP-QSH-ADDITIONS §8 区文档共同口径 · L151 · 830
  - MAP-QSH-ADDITIONS §8.1 身份特征与近景要求 · L155 · 393
  - MAP-QSH-ADDITIONS §8.2 四季与昼夜 · L161 · 294

## MAP-QSH-CENTER-EAST-SEASONS · 中部与东区四季昼夜与天气：月出与光、夜间角色、四季表、避雨处与积水点

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-SEASONS-中部与东区四季昼夜与天气.md` · 4.2k 字 · 中部与东区的月出与光、夜间角色与四季表、球场积雪、避雨处与雨帘、积水点、雷雨与雾里读形
关键词：东区月出、傍晚满月、清晨金边、球场高杆灯、筒拱采光带、夜间角色、东区四季、球场积雪、树墙四季、避雨处、架空通道雨帘、积水点、雷雨、雾里读形、雨天的生活痕迹

- MAP-QSH-CENTER-EAST-SEASONS §0 结论 · L23 · 484
  - ★ MAP-QSH-CENTER-EAST-SEASONS §0.1 接入要点 · L30 · 258
- MAP-QSH-CENTER-EAST-SEASONS §1 本区的光（主线之后） · L35 · 334
- MAP-QSH-CENTER-EAST-SEASONS §2 季节与昼夜清单 · L44 · 1.9k
- MAP-QSH-CENTER-EAST-SEASONS §3 天气要点（主线之后） · L84 · 531

## MAP-QSH-CENTER-EAST-WALK · 中部与东区行走与声音：主网、挡路物、开放入口、点位与锚点、声音

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-WALK-中部与东区行走与声音.md` · 5.0k 字 · 中部与东区的行走主网、能看不能进、开放入口的门口表现、涉水区、点位节奏与锚点、声音要点
关键词：东区行走、行走主网、能看不能进、球场围网门、游泳池组团围网、东缘校界、开放入口、门口表现、阶梯教室、涉水区、P14、P23、R5、R14、A04、A13、地标可见、声音要点、叠水喷泉声、Sounds

- MAP-QSH-CENTER-EAST-WALK §0 结论 · L23 · 749
  - ★ MAP-QSH-CENTER-EAST-WALK §0.1 接入要点 · L30 · 444
- MAP-QSH-CENTER-EAST-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L37 · 1.1k
- MAP-QSH-CENTER-EAST-WALK §2 开放入口与门口表现（主线之后） · L60 · 465
- MAP-QSH-CENTER-EAST-WALK §3 行走：点位、节奏与锚点（主线之后） · L68 · 1.0k
- MAP-QSH-CENTER-EAST-WALK §4 声音要点（主线之后） · L85 · 442
- MAP-QSH-CENTER-EAST-WALK §5 待验证（主线之后） · L95 · 114

## MAP-QSH-CENTER-EAST · 中部与东区：中部科研区、雕像、品学楼、文科楼群、训练馆、球场、田径场

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-中部与东区.md` · 7.6k 字 · 中部科研区与东区各建筑、雕像、球场的外观与身份特征、本区设计增补、近景要求；四季昼夜、行走与声音在子文档
关键词：中部科研区、东区、基础实验大楼、一号科研实验楼、科研楼B区、科研楼C区、麦克斯韦像、方程石、林为干院士像、科研楼碑、银杏大道跨河小桥、品学楼、第一教学楼、书法横梁门框、架空通道、叠水喷泉石台、文科楼群、友谊树、综合训练馆、筒拱、网球场树墙、绿色房间、球场标线、球场围网、高杆灯、田径场、器材室、室外游泳池组团、东区月出、overlay/center-east.d

- MAP-QSH-CENTER-EAST §0 结论 · L22 · 949
  - ★ MAP-QSH-CENTER-EAST §0.1 接入要点 · L32 · 194
- MAP-QSH-CENTER-EAST §1 中部科研区 · L37 · 859
- MAP-QSH-CENTER-EAST §2 品学楼 · L49 · 1.0k
- MAP-QSH-CENTER-EAST §3 文科楼群 · L59 · 553
- MAP-QSH-CENTER-EAST §4 综合训练馆与球场 · L66 · 1.0k
- MAP-QSH-CENTER-EAST §5 东缘 · L75 · 556
- MAP-QSH-CENTER-EAST §6 本区身份特征、设计增补与近景要求 · L82 · 783
- MAP-QSH-CENTER-EAST §7 待验证 · L103 · 357

## MAP-QSH-CORE-SEASONS · 核心区灯光、四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-CORE-SEASONS-核心区灯光四季昼夜与天气.md` · 5.2k 字 · 核心区图书馆灯光的全天变化、各对象的夜间角色与四季状态、交给 WP-39 的灯具、避雨处、积水点与雾里地标
关键词：图书馆灯光、檐口灯带、外墙泛光、灯岛、考试周通宵、核心区四季、核心区夜景、夜间角色表、核心区灯具、门口灯 lamp.first、核心区避雨处、核心区积水点、雾里穹顶、雨天生活痕迹

- MAP-QSH-CORE-SEASONS §0 结论 · L23 · 733
  - ★ MAP-QSH-CORE-SEASONS §0.1 接入要点 · L31 · 325
- MAP-QSH-CORE-SEASONS §1 图书馆灯光（主线之后） · L36 · 595
- MAP-QSH-CORE-SEASONS §2 季节与昼夜清单 · L47 · 2.2k
  - MAP-QSH-CORE-SEASONS §2.1 夜间角色与四季 · L51 · 1.8k
  - MAP-QSH-CORE-SEASONS §2.2 交给 WP-39 的灯具（主线之后） · L89 · 295
- MAP-QSH-CORE-SEASONS §3 天气要点（主线之后） · L93 · 633

## MAP-QSH-CORE-WALK · 核心区行走与声音

`docs/maps/qingshuihe/MAP-QSH-CORE-WALK-核心区行走与声音.md` · 4.1k 字 · 核心区行走主网、能看不能进的阻挡、可进入的室内、涉水区、点位、节奏与锚点、声音要点
关键词：核心区行走、核心区主网、能看不能进、涉水区、核心区点位、P01、P02、P03、P04、P05、P06、P16、R3 西湖跨湖、导览亭 A02、导览桩 A07、手工发声器、喷泉声、堰、天鹅屋声、草阶看台

- MAP-QSH-CORE-WALK §0 结论 · L23 · 628
  - ★ MAP-QSH-CORE-WALK §0.1 接入要点 · L31 · 267
- MAP-QSH-CORE-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L37 · 992
- MAP-QSH-CORE-WALK §2 行走：点位、节奏与锚点（主线之后） · L57 · 931
- MAP-QSH-CORE-WALK §3 声音要点（主线之后） · L76 · 554
- MAP-QSH-CORE-WALK §4 待验证（主线之后） · L86 · 20

## MAP-QSH-CORE · 核心区：八角图书馆、时间广场、日晷、博物馆、西湖

`docs/maps/qingshuihe/MAP-QSH-CORE-核心区.md` · 8.8k 字 · 核心区图书馆、时间广场、日晷、博物馆外观与西湖岸线构筑物的外观与身份特征、设计增补、近景要求；灯光、四季、天气和行走、声音在子文档
关键词：核心区、图书馆外观、八角采光顶、穹顶、方塔玻璃槽、竖缝窗、大台阶、二楼正门、品·悦吧店面、宁静苑、无主体块、时间广场、旗杆、十二时辰刻度、日晷、晷针、博物馆外观、柱廊环、西湖、折线栈桥、白色拱桥、湖心岛观景平台、西湖喷泉、箱涵、天鹅屋、入水口荷花、草阶看台、overlay/core.d

- MAP-QSH-CORE §0 结论 · L22 · 1.3k
  - ★ MAP-QSH-CORE §0.1 接入要点 · L38 · 454
- MAP-QSH-CORE §1 图书馆外观 · L46 · 906
- MAP-QSH-CORE §2 入口、品·悦吧与宁静苑 · L55 · 603
- MAP-QSH-CORE §3 时间广场 · L61 · 510
- MAP-QSH-CORE §4 日晷 · L69 · 445
- MAP-QSH-CORE §5 电子科技博物馆 · L76 · 553
- MAP-QSH-CORE §6 西湖的岸线与构筑物 · L84 · 1.0k
- MAP-QSH-CORE §7 西湖喷泉 · L97 · 484
- MAP-QSH-CORE §8 本区的构图关系 · L111 · 375
- MAP-QSH-CORE §9 本区身份特征、设计增补与近景要求 · L118 · 751
- MAP-QSH-CORE §10 待验证 · L138 · 389

## MAP-QSH-INTERIORS · 其余开放楼室内与开放的高处

`docs/maps/qingshuihe/MAP-QSH-INTERIORS-其余开放楼室内.md` · 5.1k 字 · 清水河其余 13 处开放楼的开放范围、封口与观景时刻总表，以及 3 处开放的屋顶与高处
关键词：其余开放楼、开放楼总表、开放范围要点、封口与限制、观景时刻、学知苑 4 人间、钟楼楼梯、钟室、灯笼观景环、主楼屋顶空廊、电梯关门换位、开放的屋顶与高处、屋顶危险、interiors.json

- MAP-QSH-INTERIORS §0 结论 · L22 · 693
  - ★ MAP-QSH-INTERIORS §0.1 接入要点 · L30 · 349
- MAP-QSH-INTERIORS §1 南区 · L36 · 943
- MAP-QSH-INTERIORS §2 西区与中东区 · L45 · 818
- MAP-QSH-INTERIORS §3 北区 · L54 · 923
- MAP-QSH-INTERIORS §4 开放的屋顶与高处（3 处） · L64 · 707

## MAP-QSH-LIBRARY · 图书馆与博物馆室内

`docs/maps/qingshuihe/MAP-QSH-LIBRARY-图书馆与博物馆室内.md` · 12.0k 字 · 图书馆与博物馆的开放室内、三条看进去的路径、室内光与视觉落点
关键词：图书馆室内、中庭、回廊、八字楼梯、塔楼梯、交通核、开放范围、封口、门斗、闸机、二楼门厅、看进去的路径、品·悦吧室内、光条、顶棚波光、红桌、视觉落点、八瓣光、台灯、校训木匾、五楼西窗阅览带、北侧露台、博物馆展厅、环形展线、interiors.json、lib.main

- MAP-QSH-LIBRARY §0 结论 · L24 · 1.3k
  - ★ MAP-QSH-LIBRARY §0.1 接入要点 · L35 · 777
- MAP-QSH-LIBRARY §1 平面、交通核与开放范围 · L48 · 1.4k
- MAP-QSH-LIBRARY §2 三条看进去的路径 · L76 · 621
- MAP-QSH-LIBRARY §3 中庭与回廊 · L88 · 384
- MAP-QSH-LIBRARY §4 一楼中厅、门斗与二楼门厅 · L96 · 775
- MAP-QSH-LIBRARY §5 室外光的延伸 · L104 · 717
- MAP-QSH-LIBRARY §6 视觉落点 · L114 · 518
- MAP-QSH-LIBRARY §7 四季、昼夜、天气与生活痕迹 · L123 · 1.3k
- MAP-QSH-LIBRARY §8 行走 · L136 · 923
- MAP-QSH-LIBRARY §9 附属部分 · L147 · 620
- MAP-QSH-LIBRARY §10 博物馆展厅 · L153 · 654
- MAP-QSH-LIBRARY §11 声音要点 · L165 · 593
- MAP-QSH-LIBRARY §13 待验证 · L175 · 540

## MAP-QSH-NORTH-SEASONS · 北部生活区四季昼夜与天气：夜间角色、四季表、开放室内的灯、避雨处与积水点

`docs/maps/qingshuihe/MAP-QSH-NORTH-SEASONS-北部生活区四季昼夜与天气.md` · 3.8k 字 · 北部生活区的夜间角色与四季表、开放室内的灯、避雨处与雨帘、积水点、雾里读形与雨天痕迹
关键词：北部生活区四季、夜间角色、宿舍熄灯、23:30 熄灯、店招时刻表、开放室内的灯、门口灯、毕业横幅、雪人、避雨处、雨帘、积水点、雾里读形、雨天的生活痕迹、阳台晾晒收起

- MAP-QSH-NORTH-SEASONS §0 结论 · L23 · 399
  - ★ MAP-QSH-NORTH-SEASONS §0.1 接入要点 · L30 · 148
- MAP-QSH-NORTH-SEASONS §1 季节与昼夜清单 · L35 · 1.8k
  - MAP-QSH-NORTH-SEASONS §1.1 夜间角色与四季 · L41 · 1.3k
  - MAP-QSH-NORTH-SEASONS §1.2 开放室内的灯（主线之后） · L82 · 162
- MAP-QSH-NORTH-SEASONS §2 天气要点（主线之后） · L86 · 577

## MAP-QSH-NORTH-WALK · 北部生活区行走与声音：主网、挡路物、开放入口、点位与锚点、声音

`docs/maps/qingshuihe/MAP-QSH-NORTH-WALK-北部生活区行走与声音.md` · 5.3k 字 · 北部生活区的行走主网、能看不能进、开放入口的门口表现、点位节奏与锚点、声音要点
关键词：北部生活区行走、行走主网、能看不能进、组团闸机、北门校界、开放入口、门口表现、涉水区、P12、P13、P21、P22、R4、R12、R13、A05、A11、A12、地标可见、声音要点、夜床、Sounds

- MAP-QSH-NORTH-WALK §0 结论 · L23 · 810
  - ★ MAP-QSH-NORTH-WALK §0.1 接入要点 · L30 · 487
- MAP-QSH-NORTH-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L38 · 1.0k
- MAP-QSH-NORTH-WALK §2 开放入口的门口表现（主线之后） · L60 · 579
- MAP-QSH-NORTH-WALK §3 行走：点位、节奏与锚点（主线之后） · L71 · 1.1k
- MAP-QSH-NORTH-WALK §4 声音要点（主线之后） · L91 · 546
- MAP-QSH-NORTH-WALK §5 待验证（主线之后） · L102 · 214

## MAP-QSH-NORTH · 北部生活区：学生活动中心、校医院、商业街、宿舍组团、餐厅

`docs/maps/qingshuihe/MAP-QSH-NORTH-北部生活区.md` · 7.9k 字 · 北部生活区各建筑的外观与身份特征、本区设计增补、近景要求；四季昼夜、行走与声音在子文档
关键词：北部生活区、学生活动中心、成电会堂、赭红圆鼓、校医院、院名灯牌、商业街、玻璃拱顶步行街、店招、宿舍组团、学知苑、硕丰苑、博翰苑、留学生组团、本科楼A型、本科楼B型、组团闸机、学知一组团、阳台晾晒、银杏苑、芙蓉餐厅、思学苑、春晖苑、朝阳餐厅、西北餐厅、室外扶梯罩棚、风雨连廊、丹桂路、博士公寓、overlay/north.d

- MAP-QSH-NORTH §0 结论 · L22 · 950
  - ★ MAP-QSH-NORTH §0.1 接入要点 · L33 · 166
- MAP-QSH-NORTH §1 学生活动中心与成电会堂 · L38 · 429
- MAP-QSH-NORTH §2 校医院 · L50 · 417
- MAP-QSH-NORTH §3 商业街 · L57 · 798
- MAP-QSH-NORTH §4 宿舍 · L66 · 1.8k
- MAP-QSH-NORTH §5 餐厅 · L80 · 984
- MAP-QSH-NORTH §6 文化地标与丹桂路 · L94 · 182
- MAP-QSH-NORTH §7 风雨连廊与本区增补 · L99 · 315
- MAP-QSH-NORTH §8 本区身份特征、设计增补与近景要求 · L109 · 570
- MAP-QSH-NORTH §9 待验证 · L129 · 116

## MAP-QSH-OUTSKIRTS · 清水河外围街区：周边结构、地标、灯光、农田与氛围增补

`docs/maps/qingshuihe/MAP-QSH-OUTSKIRTS-清水河外围街区.md` · 4.7k 字 · 清水河校界外一圈街区的方位结构、截面线距离、身份地标、夜间灯光、农田四季、氛围增补、河渠格栅与看街长椅
关键词：周边结构、西源大道、蓉 2 号线、电子科大站、龙湖时代天街、绕城高架、蓉昌高速、220 kV 变电站、顺江片区、综保区、农田、油菜花、林泽杜鹃、川西林盘、绕城绿道、草坪轨道、看街长椅、河渠格栅、清水河银带

- MAP-QSH-OUTSKIRTS §0 结论 · L22 · 821
  - ★ MAP-QSH-OUTSKIRTS §0.1 接入要点 · L31 · 362
- MAP-QSH-OUTSKIRTS §1 周边结构与截面线距离 · L38 · 566
  - MAP-QSH-OUTSKIRTS §1.1 周边结构要点 · L40 · 284
  - MAP-QSH-OUTSKIRTS §1.2 各方位的截面线距离 · L44 · 265
- MAP-QSH-OUTSKIRTS §2 楼型在外围的位置 · L49 · 229
- MAP-QSH-OUTSKIRTS §3 灯光与农田 · L62 · 523
  - MAP-QSH-OUTSKIRTS §3.1 夜间灯光 · L64 · 410
  - MAP-QSH-OUTSKIRTS §3.2 农田与绿地的四季 · L78 · 101
- MAP-QSH-OUTSKIRTS §4 身份地标（保留，让人认得出） · L82 · 379
- MAP-QSH-OUTSKIRTS §5 新增的氛围元素与外围植被 · L94 · 514
- MAP-QSH-OUTSKIRTS §6 河渠格栅与看街长椅 · L106 · 345
  - MAP-QSH-OUTSKIRTS §6.1 河渠格栅（WP-30） · L108 · 142
  - MAP-QSH-OUTSKIRTS §6.2 看街长椅（WP-30 做，WP-44 选点） · L112 · 187
- MAP-QSH-OUTSKIRTS §7 待验证 · L116 · 193

## MAP-QSH-RENOVATION · 清水河翻新清单

`docs/maps/qingshuihe/MAP-QSH-RENOVATION-清水河翻新清单.md` · 6.6k 字 · 清水河校区老楼与环境的翻新：所有老楼按同一套翻新预设做，另列十来条身份特征例外；部位代号、全校环境、年代未查到的楼和不列入的新楼
关键词：翻新预设、身份特征例外、部位代号、楼型、翻新对照楼、年代未查到的楼、不列入的楼、2015 年以后的新楼、全校环境翻新、老楼翻新理由、商业街店招、体育场看台

- MAP-QSH-RENOVATION §0 结论 · L22 · 822
  - ★ MAP-QSH-RENOVATION §0.1 接入要点 · L30 · 432
- MAP-QSH-RENOVATION §1 翻新预设、部位代号与楼型 · L40 · 1.8k
  - MAP-QSH-RENOVATION §1.1 翻新预设 · L42 · 144
  - MAP-QSH-RENOVATION §1.2 例外的写法 · L46 · 111
  - MAP-QSH-RENOVATION §1.3 部位代号与通用做法 · L50 · 1.3k
  - MAP-QSH-RENOVATION §1.4 楼型 · L69 · 202
- MAP-QSH-RENOVATION §2 北部生活区的例外（MAP-QSH-NORTH，WP-20） · L73 · 515
- MAP-QSH-RENOVATION §3 中部与东区的例外（MAP-QSH-CENTER-EAST，WP-21） · L84 · 266
- MAP-QSH-RENOVATION §4 南区的例外（MAP-QSH-SOUTH，WP-18） · L91 · 373
- MAP-QSH-RENOVATION §5 西区与西北区的例外（MAP-QSH-WEST，WP-19） · L100 · 168
- MAP-QSH-RENOVATION §6 核心区的例外（MAP-QSH-CORE，WP-16） · L106 · 202
- MAP-QSH-RENOVATION §7 全校环境 · L113 · 644
- MAP-QSH-RENOVATION §8 年代未查到的楼 · L128 · 262
- MAP-QSH-RENOVATION §9 不列入的楼 · L137 · 174
- MAP-QSH-RENOVATION §10 登记与验收 · L141 · 366

## MAP-QSH-SOUTH-SEASONS · 南区四季昼夜与天气

`docs/maps/qingshuihe/MAP-QSH-SOUTH-SEASONS-南区四季昼夜与天气.md` · 4.5k 字 · 南区各对象的夜间角色与四季状态、交给 WP-39 的灯具、避雨处、积水点、雾里地标与彩虹
关键词：南区四季、南区夜景、夜间角色表、主楼泛光、钟面背光、南区灯具、南区避雨处、南区积水点、雾里地标、钟楼雾豁免、彩虹、雨天生活痕迹

- MAP-QSH-SOUTH-SEASONS §0 结论 · L23 · 820
  - ★ MAP-QSH-SOUTH-SEASONS §0.1 接入要点 · L31 · 400
- MAP-QSH-SOUTH-SEASONS §1 季节与昼夜清单 · L38 · 2.2k
  - MAP-QSH-SOUTH-SEASONS §1.1 夜间角色与四季 · L42 · 1.7k
  - MAP-QSH-SOUTH-SEASONS §1.2 交给 WP-39 的灯具（主线之后） · L84 · 268
- MAP-QSH-SOUTH-SEASONS §2 天气要点（主线之后） · L88 · 592

## MAP-QSH-SOUTH-WALK · 南区行走与声音

`docs/maps/qingshuihe/MAP-QSH-SOUTH-WALK-南区行走与声音.md` · 5.2k 字 · 南区行走主网、能看不能进的阻挡、可进入的室内与开放入口、涉水区、点位、节奏与锚点、声音要点
关键词：南区行走、南区主网、能看不能进、开放入口、涉水区、南区点位、P07、P09、P17、P18、P19、R1 南区段、R2 环湖、R7、R8、R9、R10、导览亭 A01、A03、A06、南区声音、钟声、草阶看台

- MAP-QSH-SOUTH-WALK §0 结论 · L23 · 951
  - ★ MAP-QSH-SOUTH-WALK §0.1 接入要点 · L31 · 511
- MAP-QSH-SOUTH-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L39 · 1.5k
- MAP-QSH-SOUTH-WALK §2 行走：点位、节奏与锚点（主线之后） · L68 · 1.1k
- MAP-QSH-SOUTH-WALK §3 声音要点（主线之后） · L87 · 502
- MAP-QSH-SOUTH-WALK §4 待验证（主线之后） · L98 · 143

## MAP-QSH-SOUTH · 南区：南门、主楼、东湖、宣邦楼、立人楼、体育场馆、国际创新中心

`docs/maps/qingshuihe/MAP-QSH-SOUTH-南区.md` · 9.8k 字 · 南区各楼与构筑物的外观与身份特征、设计增补、近景要求；四季昼夜与天气、行走与声音在子文档
关键词：南区、南门、校名墙、主楼、主轴线、屋顶空廊、过街楼、主楼泛光、森林公园、孔雀之家、东湖岸线、环湖跑道、里程标、六角亭、亲水平台、荷角、吴立人像、宣邦楼、钟楼、电子管穹顶、交班的钟、钟面指针、立人楼、体育馆、游泳馆、体育运动中心大平台、看台罩棚、国际创新中心、1–8 号楼、overlay/south.d

- MAP-QSH-SOUTH §0 结论 · L22 · 690
  - ★ MAP-QSH-SOUTH §0.1 接入要点 · L36 · 153
- MAP-QSH-SOUTH §1 南门 · L41 · 519
- MAP-QSH-SOUTH §2 主楼 · L49 · 1.1k
- MAP-QSH-SOUTH §3 过街楼、开放室内与周边 · L62 · 637
- MAP-QSH-SOUTH §4 东湖 · L70 · 880
- MAP-QSH-SOUTH §5 宣邦楼 · L81 · 1.3k
- MAP-QSH-SOUTH §6 立人楼 · L97 · 358
- MAP-QSH-SOUTH §7 体育场馆（沿西源大道从西到东） · L104 · 826
- MAP-QSH-SOUTH §8 西南角：国际创新中心与科创餐厅 · L114 · 370
- MAP-QSH-SOUTH §9 南区中部的 1–8 号楼 · L120 · 321
- MAP-QSH-SOUTH §10 本区的水道 · L124 · 273
- MAP-QSH-SOUTH §11 本区身份特征、设计增补与近景要求 · L130 · 797
- MAP-QSH-SOUTH §12 待验证 · L153 · 364

## MAP-QSH-TERRAIN-CHECKS · 地形验收镜头、冲突检查与点位标高

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-CHECKS-验收镜头冲突与点位标高.md` · 3.2k 字 · 地形验收用的四个镜头、垫层与跳跃链等冲突检查结论、点位与锚点标高，以及地形初值
关键词：地形验收镜头、冲突检查、垫层与楼体高差、点位标高、锚点标高、室内同位点、开始页与过场、地形求值

- MAP-QSH-TERRAIN-CHECKS §0 结论 · L23 · 615
  - ★ MAP-QSH-TERRAIN-CHECKS §0.1 接入要点 · L29 · 368
- MAP-QSH-TERRAIN-CHECKS §1 验收镜头 · L37 · 250
- MAP-QSH-TERRAIN-CHECKS §2 冲突检查的结论 · L48 · 470
- MAP-QSH-TERRAIN-CHECKS §3 点位与锚点标高 · L56 · 550
- MAP-QSH-TERRAIN-CHECKS §4 初值 · L70 · 392

## MAP-QSH-TERRAIN-DISTRICTS · 十处分区地形

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-DISTRICTS-十处分区地形.md` · 9.0k 字 · 清水河十处分区（中轴广场、银杏大道、西湖、东湖、河渠、宿舍、运动场、食堂商业街、教学科研区、校门校界）的标高、坡度、连接与对点位的约束，以及地形文件归属
关键词：分区地形、下沉时间广场、落金沟、湖岸丘链、草阶看台台地、东湖亭丘、岛丘、品学楼台基、坡广场、宿舍台原、上台、下台、台口、通学坡、看月台、堤上路、球场堤路、下沉草阶草坪、河岸低步道、渠堤、湿地低地、扇形草坪浅碟、中庭草丘、零带、terrain.json

- MAP-QSH-TERRAIN-DISTRICTS §0 结论 · L23 · 849
  - ★ MAP-QSH-TERRAIN-DISTRICTS §0.1 接入要点 · L34 · 370
- MAP-QSH-TERRAIN-DISTRICTS §1 文件归属 · L41 · 503
- MAP-QSH-TERRAIN-DISTRICTS §2 中轴广场与图书馆 · L53 · 483
- MAP-QSH-TERRAIN-DISTRICTS §3 银杏大道 · L62 · 359
- MAP-QSH-TERRAIN-DISTRICTS §4 西湖与红枫丘 · L69 · 715
- MAP-QSH-TERRAIN-DISTRICTS §5 东湖 · L78 · 553
- MAP-QSH-TERRAIN-DISTRICTS §6 河渠与箱涵 · L85 · 711
- MAP-QSH-TERRAIN-DISTRICTS §7 宿舍区 · L94 · 1.3k
- MAP-QSH-TERRAIN-DISTRICTS §8 运动场与体育区 · L106 · 645
- MAP-QSH-TERRAIN-DISTRICTS §9 食堂与商业街 · L114 · 350
- MAP-QSH-TERRAIN-DISTRICTS §10 教学科研区 · L120 · 970
- MAP-QSH-TERRAIN-DISTRICTS §11 校门与校界 · L128 · 304

## MAP-QSH-TERRAIN · 分区地形：全图骨架、地标丘、揭示路线与常水位

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-分区地形.md` · 5.7k 字 · 清水河地形的全图骨架：层分布、红枫丘、遮挡与揭示路线、各水体常水位、坡度与着色的本图实例
关键词：地形骨架、红枫丘、地标丘、下沉时间广场、常水位表、湖口跌水、箱涵堰口、遮挡与揭示、揭示路线、中轴落金线、湿地堤、起伏的理由、水位阶梯、分面平法线

- MAP-QSH-TERRAIN §0 结论 · L22 · 1.2k
  - ★ MAP-QSH-TERRAIN §0.1 接入要点 · L38 · 247
- MAP-QSH-TERRAIN §1 全图骨架 · L44 · 1.9k
  - MAP-QSH-TERRAIN §1.1 分层分布 · L46 · 746
  - MAP-QSH-TERRAIN §1.2 地标丘：红枫丘 · L61 · 539
  - MAP-QSH-TERRAIN §1.3 遮挡与揭示的三条路线 · L69 · 592
- MAP-QSH-TERRAIN §2 水系与常水位 · L79 · 970
- MAP-QSH-TERRAIN §3 坡度与着色 · L100 · 414

## MAP-QSH-VEGETATION · 清水河植被：树种、种植与地面覆盖的位置

`docs/maps/qingshuihe/MAP-QSH-VEGETATION-清水河树种与种植.md` · 4.1k 字 · 清水河各树种的四季观感与种在哪、线性与片区种植、地面覆盖的具体位置、水下森林与荷的位置
关键词：种在哪、银杏大道、水杉路、丹桂路、春季粉带、森林公园红枫、校友林、湿地公园、友谊树、银杏林、樱花林、蓝花楹、入水口荷、水下森林位置、油菜花带位置

- MAP-QSH-VEGETATION §0 结论 · L22 · 374
  - ★ MAP-QSH-VEGETATION §0.1 接入要点 · L30 · 93
- MAP-QSH-VEGETATION §1 落叶树的四季与种在哪 · L34 · 799
- MAP-QSH-VEGETATION §2 常绿树与灌木的四季与种在哪 · L55 · 346
- MAP-QSH-VEGETATION §3 种植 · L68 · 1.0k
  - MAP-QSH-VEGETATION §3.1 线性种植 · L70 · 500
  - MAP-QSH-VEGETATION §3.2 片区种植 · L78 · 518
- MAP-QSH-VEGETATION §4 地面覆盖的具体位置 · L86 · 349
- MAP-QSH-VEGETATION §5 水生植物的位置 · L97 · 217
- MAP-QSH-VEGETATION §6 待验证 · L103 · 53

## MAP-QSH-WATER · 清水河水体：西湖、东湖、河渠、池水与叠水喷泉

`docs/maps/qingshuihe/MAP-QSH-WATER-清水河水体.md` · 5.3k 字 · 清水河各水体的具体水面：西湖与喷泉水柱、东湖、河渠、游泳池池水、叠水喷泉、截面水柱，开放泳区名单与倒影时段
关键词：西湖水下森林、西湖喷泉水柱、东湖荷区、锦鲤、冬季浅滩、走马河、红光右支渠、磨底河、河渠中线、漂浮物、游泳馆池水、品学楼叠水喷泉、截面水柱、泳区名单、主要倒影按时段

- MAP-QSH-WATER §0 结论 · L22 · 620
  - ★ MAP-QSH-WATER §0.1 接入要点 · L31 · 320
- MAP-QSH-WATER §1 西湖 · L38 · 642
  - MAP-QSH-WATER §1.1 水下森林与反光带 · L40 · 314
  - MAP-QSH-WATER §1.2 扇形喷泉 · L46 · 319
- MAP-QSH-WATER §2 东湖 · L55 · 456
- MAP-QSH-WATER §3 河渠 · L62 · 791
- MAP-QSH-WATER §4 池水、叠水喷泉与截面水柱 · L77 · 1.1k
  - MAP-QSH-WATER §4.1 游泳馆与室外游泳池 · L79 · 406
  - MAP-QSH-WATER §4.2 品学楼门前叠水喷泉 · L89 · 440
  - MAP-QSH-WATER §4.3 主楼水池与截面水柱 · L99 · 267
- MAP-QSH-WATER §5 开放泳区的名单与登记 · L104 · 258
- MAP-QSH-WATER §6 主要倒影按时段 · L112 · 375

## MAP-QSH-WEST-SEASONS · 西区与西北区四季昼夜与天气：夜间角色、四季表、避雨处与积水点

`docs/maps/qingshuihe/MAP-QSH-WEST-SEASONS-西区与西北区四季昼夜与天气.md` · 3.7k 字 · 西区与西北区的夜间角色与四季表、开放室内的灯、避雨处与雨帘、积水点、雾里读形与推荐状态
关键词：西区四季、西区夜间角色、六号科研楼受光、湿地暗区、萤火虫、开放入口门口灯、避雨处、候车亭雨帘、光伏车棚、积水点、雨水花园、雾里读形、晨雾、雨天的生活痕迹、P10 推荐状态

- MAP-QSH-WEST-SEASONS §0 结论 · L23 · 343
  - ★ MAP-QSH-WEST-SEASONS §0.1 接入要点 · L30 · 109
- MAP-QSH-WEST-SEASONS §1 季节与昼夜清单 · L34 · 1.8k
- MAP-QSH-WEST-SEASONS §2 天气要点（主线之后） · L81 · 587

## MAP-QSH-WEST-WALK · 西区与西北区行走与声音：主网、挡路物、开放入口、点位与锚点、声音

`docs/maps/qingshuihe/MAP-QSH-WEST-WALK-西区与西北区行走与声音.md` · 5.3k 字 · 西区与西北区的行走主网、能看不能进、开放入口的门口表现、涉水区、点位节奏与锚点、声音要点
关键词：西区行走、行走主网、水杉路、湿地栈道、能看不能进、托幼园围栏、西门校界、开放入口、门口表现、涉水区、卵石滩、P05、P10、P11、P20、R11、A08、A09、A10、地标可见、声音要点、Sounds

- MAP-QSH-WEST-WALK §0 结论 · L23 · 865
  - ★ MAP-QSH-WEST-WALK §0.1 接入要点 · L30 · 528
- MAP-QSH-WEST-WALK §1 行走：主网、挡路物与开放入口（主线之后） · L39 · 1.3k
- MAP-QSH-WEST-WALK §2 开放入口与门口表现（主线之后） · L61 · 477
- MAP-QSH-WEST-WALK §3 行走：点位、节奏与锚点（主线之后） · L68 · 1.0k
- MAP-QSH-WEST-WALK §4 声音要点（主线之后） · L85 · 310
- MAP-QSH-WEST-WALK §5 待验证（主线之后） · L94 · 235

## MAP-QSH-WEST · 西区与西北区：六号科研楼、校训石、创新中心、经管楼、托幼园、四号/五号科研楼、湿地、西门桥头

`docs/maps/qingshuihe/MAP-QSH-WEST-西区与西北区.md` · 6.3k 字 · 西区与西北区各楼、石刻、湿地与西门桥头的外观与身份特征、设计增补、近景要求；四季昼夜、行走与声音在子文档
关键词：西区、西北区、六号科研楼、基础与前沿研究院、公式台阶、2F室外连廊、校训石、创新中心、经管楼、三号科研实验楼、宾诺咖啡、立人托幼公园、键盘立面、四号科研楼、扇形草坪、深蓝玻璃盒入口、五号科研楼、电子科学技术研究院大楼、微波暗室、工程训练中心、荟文楼、综合楼、核磁共振中心、国防科技园石刻、湿地公园、湿地栈道、西门桥头、水杉路、孤儿楼 6151 与 6154、overlay/west.d

- MAP-QSH-WEST §0 结论 · L22 · 910
  - ★ MAP-QSH-WEST §0.1 接入要点 · L33 · 92
- MAP-QSH-WEST §1 六号科研楼 · L37 · 630
- MAP-QSH-WEST §2 校训石 · L46 · 257
- MAP-QSH-WEST §3 创新中心 · L51 · 282
- MAP-QSH-WEST §4 经管楼 · L57 · 73
- MAP-QSH-WEST §5 立人托幼公园 · L61 · 204
- MAP-QSH-WEST §6 四号科研楼 · L66 · 361
- MAP-QSH-WEST §7 五号科研楼、研究院大楼、核磁共振中心 · L72 · 341
- MAP-QSH-WEST §8 荟文楼（综合楼） · L79 · 68
- MAP-QSH-WEST §9 石刻 · L83 · 65
- MAP-QSH-WEST §10 湿地公园与西门桥头 · L87 · 651
- MAP-QSH-WEST §11 本区身份特征、设计增补与近景要求 · L97 · 798
- MAP-QSH-WEST §12 待验证 · L118 · 134

## SYS-ATLAS-TRAVEL · 标记、锚点与传送

`docs/systems/SYS-ATLAS-TRAVEL-标记锚点与传送.md` · 6.1k 字 · 标记类型与可传送范围、锚点实体（导览亭与导览桩，主线先放 3–4 座）、传送流程与落点；主线全开放，解锁策略在主线之后
关键词：地图标记、标记类型、锚点、传送点、导览亭、导览桩、传送、雾幕瞬移、飞过去、传送落点、落地检查、解锁策略、all-unlocked、proximity、开放室内标记、停放点标记、IUnlockPolicy、TeleportSpec、TravelStyle、TravelRefusal、UnlockState、AnchorForm、MarkerKind、AtlasMarker

- SYS-ATLAS-TRAVEL §0 结论 · L23 · 492
  - ★ SYS-ATLAS-TRAVEL §0.1 接入要点 · L30 · 234
- SYS-ATLAS-TRAVEL §1 标记类型 · L36 · 1.1k
- SYS-ATLAS-TRAVEL §2 锚点实体：导览亭与导览桩 · L54 · 1.2k
- SYS-ATLAS-TRAVEL §3 传送流程 · L70 · 1.4k
  - SYS-ATLAS-TRAVEL §3.1 两种方式 · L72 · 267
  - SYS-ATLAS-TRAVEL §3.2 传送的时序 · L78 · 413
  - SYS-ATLAS-TRAVEL §3.3 落点 · L89 · 445
  - SYS-ATLAS-TRAVEL §3.4 预取、打断与边界情况 · L101 · 266
- SYS-ATLAS-TRAVEL §4 解锁 · L106 · 439
- SYS-ATLAS-TRAVEL §5 待验证 · L117 · 77

## SYS-ATLAS-UI · 地图界面：底图、小地图与大地图

`docs/systems/SYS-ATLAS-UI-地图界面.md` · 6.3k 字 · 平面地图的界面：底图内容与上色、外围与校界画法、文字与标注、小地图、大地图的布局缩放图层目录传送卡；自定义标记与追踪在主线之后
关键词：底图、小地图、大地图、地图页、atlasRect、距离场、map.* 色值、区域名、地名标注、labels.json、缩放档、目录页、传送卡、图层

- SYS-ATLAS-UI §0 结论 · L23 · 372
  - ★ SYS-ATLAS-UI §0.1 接入要点 · L28 · 152
- SYS-ATLAS-UI §1 底图 · L34 · 2.0k
  - SYS-ATLAS-UI §1.1 来源：世界模型，不是原始 OSM · L36 · 479
  - SYS-ATLAS-UI §1.2 上色 · L45 · 497
  - SYS-ATLAS-UI §1.3 外围、校界与箱庭外缘 · L51 · 426
  - SYS-ATLAS-UI §1.4 季节与昼夜：只做轻微变化 · L58 · 193
  - SYS-ATLAS-UI §1.5 文字与标注 · L63 · 349
- SYS-ATLAS-UI §2 小地图 · L70 · 903
- SYS-ATLAS-UI §3 大地图 · L82 · 1.8k
  - SYS-ATLAS-UI §3.1 布局与开合 · L84 · 454
  - SYS-ATLAS-UI §3.2 缩放与平移 · L91 · 260
  - SYS-ATLAS-UI §3.3 图层与目录 · L96 · 309
  - SYS-ATLAS-UI §3.4 传送卡 · L101 · 354
  - SYS-ATLAS-UI §3.5 自定义标记与追踪（主线之后） · L107 · 285
  - SYS-ATLAS-UI §3.6 按键 · L112 · 114
- SYS-ATLAS-UI §4 待验证 · L116 · 152

## SYS-ATLAS · 平面地图与传送

`docs/systems/SYS-ATLAS-平面地图与传送.md` · 3.2k 字 · 平面地图的总则：允许与禁止什么、多地图下的数据归属、设置里存什么；主线全开放，自定义标记、追踪、解锁策略在主线之后；界面与传送分在两份子文档
关键词：平面地图、地图只活在界面层、3D 里不标注、IAtlasService、MarkerId、前往方式、图层筛选、AtlasSave、TransitPreference

- SYS-ATLAS §0 结论 · L22 · 660
  - ★ SYS-ATLAS §0.1 接入要点 · L33 · 311
- SYS-ATLAS §1 允许什么、禁止什么 · L39 · 405
- SYS-ATLAS §2 多地图下的数据 · L51 · 534
- SYS-ATLAS §3 存档里存什么、设置里存什么 · L58 · 423

## SYS-AUDIO-AMBIENCE · 环境声景与天气事件声

`docs/systems/SYS-AUDIO-AMBIENCE-环境声景与天气事件声.md` · 5.1k 字 · 环境声七层与数据驱动发声器、季节出没、天气声、远雷钟声广播与偶发事件的声音
关键词：环境声、声景、七层、发声器、声景场、床、鸟声、生命感、室内档案、英雄声景、天气声、雨声分层、远雷、钟声、校园广播、偶发事件声、IAmbienceService、IAmbientEmitterSource、AmbientEmitter、SoundSocketDecl、AmbienceCue、ILightningFeed

- SYS-AUDIO-AMBIENCE §0 结论 · L22 · 615
  - ★ SYS-AUDIO-AMBIENCE §0.1 接入要点 · L29 · 361
- SYS-AUDIO-AMBIENCE §1 环境声景 · L35 · 1.7k
  - SYS-AUDIO-AMBIENCE §1.1 七层与驱动量 · L37 · 342
  - SYS-AUDIO-AMBIENCE §1.2 数据驱动的发声器（主线之后） · L44 · 402
  - SYS-AUDIO-AMBIENCE §1.3 季节与出没 · L51 · 223
  - SYS-AUDIO-AMBIENCE §1.4 生命感规则 · L56 · 296
  - SYS-AUDIO-AMBIENCE §1.5 室内档案（主线之后） · L63 · 122
  - SYS-AUDIO-AMBIENCE §1.6 校外、外围与边界 · L67 · 126
  - SYS-AUDIO-AMBIENCE §1.7 离地高度与介质 · L71 · 118
  - SYS-AUDIO-AMBIENCE §1.8 英雄声景 · L75 · 108
- SYS-AUDIO-AMBIENCE §2 天气声 · L79 · 763
  - SYS-AUDIO-AMBIENCE §2.1 分天气的声音 · L83 · 336
  - SYS-AUDIO-AMBIENCE §2.2 雨与风的分层、起止与残留 · L98 · 377
- SYS-AUDIO-AMBIENCE §3 远雷、钟声、广播与偶发事件 · L105 · 709

## SYS-AUDIO-FOLEY · 观者声与空间声学

`docs/systems/SYS-AUDIO-FOLEY-观者声与空间声学.md` · 4.0k 字 · 脚步与交互音、材质与覆盖物、飞行风声，窄处脚步更明显的空间声学、遮挡与室内外传声
关键词：观者声、脚步声、步相、落地、材质、覆盖物、交互音、飞行风声、空间声学、混响、声学网格、门户、遮挡、室内外传声、FootMat、IGroundCover、IAcousticEnv、IFoleyService、IWalkTelemetry、洗牌袋

- SYS-AUDIO-FOLEY §0 结论 · L22 · 325
  - ★ SYS-AUDIO-FOLEY §0.1 接入要点 · L28 · 135
- SYS-AUDIO-FOLEY §1 观者声 · L32 · 1.4k
  - SYS-AUDIO-FOLEY §1.1 原则 · L34 · 191
  - SYS-AUDIO-FOLEY §1.2 步相与步频 · L39 · 212
  - SYS-AUDIO-FOLEY §1.3 材质与覆盖物 · L45 · 399
  - SYS-AUDIO-FOLEY §1.4 分层与变体 · L51 · 171
  - SYS-AUDIO-FOLEY §1.5 交互音 · L56 · 305
  - SYS-AUDIO-FOLEY §1.6 飞行风声 · L63 · 150
- SYS-AUDIO-FOLEY §2 空间声学（主线之后） · L67 · 1.0k
  - SYS-AUDIO-FOLEY §2.1 "窄处脚步更明显" · L69 · 187
  - SYS-AUDIO-FOLEY §2.2 方案与类别 · L75 · 342
  - SYS-AUDIO-FOLEY §2.3 遮挡与室内外传声 · L81 · 342
  - SYS-AUDIO-FOLEY §2.4 介质与天气的消音感 · L89 · 116

## SYS-AUDIO · 音频：内核、混音、加速节制、人迹禁区、界面音效与素材

`docs/systems/SYS-AUDIO-音频.md` · 7.0k 字 · 声音共守的规则：内核与生命周期、混音与响度、加速节制与转场、人迹禁区、界面音、开场音乐、设置、素材许可、预算验收
关键词：音频、声音、混音、响度、加速节制、转场、人迹声、界面音效、开场音乐、音频设置、素材许可、署名页、IAudioService、AudioBuses、AudioSettings、settings.json、G21

- SYS-AUDIO §0 结论 · L21 · 1.1k
  - ★ SYS-AUDIO §0.1 接入要点 · L38 · 388
- SYS-AUDIO §1 总体与生命周期 · L44 · 757
- SYS-AUDIO §2 混音与响度 · L56 · 497
- SYS-AUDIO §3 加速流逝的节制与转场 · L75 · 487
- SYS-AUDIO §4 人迹声：确保不出现清晰人声 · L87 · 511
- SYS-AUDIO §5 界面音效 · L98 · 425
- SYS-AUDIO §6 开场音乐 · L106 · 301
- SYS-AUDIO §7 设置 · L111 · 376
- SYS-AUDIO §8 素材策略与许可 · L117 · 504
- SYS-AUDIO §9 预算（设计目标） · L124 · 167
- SYS-AUDIO §10 验收 · L131 · 234
- SYS-AUDIO §11 待验证（P0） · L137 · 110

## SYS-GRAPHICS · 画面设置：预设、单项、风格底线、应用与持久化

`docs/systems/SYS-GRAPHICS-画面设置.md` · 8.1k 字 · 主线三个画面预设与约 10 个单项、风格底线、应用方式、持久化；自动调节、首次分档、15 秒确认等在主线之后
关键词：画面设置、画质、画面预设、设备档、均衡、精致、极致、单项、风格底线、风格默认、渲染比例、雾幕类设置、画面页、IQualityService、IQualityAware、QualityResponsive、QualityKnob、quality-presets.json、settings.json

- SYS-GRAPHICS §0 结论 · L22 · 1.6k
  - ★ SYS-GRAPHICS §0.1 接入要点 · L33 · 850
- SYS-GRAPHICS §1 预设 · L46 · 900
- SYS-GRAPHICS §2 风格底线与风格默认 · L66 · 778
- SYS-GRAPHICS §3 单项与生效方式 · L81 · 979
- SYS-GRAPHICS §4 应用方式 · L105 · 441
- SYS-GRAPHICS §5 自动推荐与自动调节（主线之后） · L115 · 796
- SYS-GRAPHICS §6 持久化与恢复 · L134 · 305
- SYS-GRAPHICS §7 画面页（WP-35，视觉见 UI-VISUAL-COMPONENTS §5） · L140 · 555
- SYS-GRAPHICS §8 待验证（P0） · L152 · 243

## SYS-MAPS · 多地图：按图数据、`IMapService` 与切换

`docs/systems/SYS-MAPS-多地图.md` · 10.6k 字 · 按图数据与全局的划分、map_rect、单图半径等多图约定；先卸后装的切图运行时在主线之后
关键词：多地图、换图、切图、地图服务、按图数据、先卸后装、同图重载、天气按图、单图半径、本机烘焙缓存、IMapService、IMapAware、MapScope、MapDescriptor、map.json、map_rect

- SYS-MAPS §0 结论 · L21 · 1.3k
  - ★ SYS-MAPS §0.1 接入要点 · L32 · 787
- SYS-MAPS §1 数据归属 · L42 · 1.2k
  - SYS-MAPS §1.1 按图与全局 · L44 · 711
  - SYS-MAPS §1.2 本机烘焙缓存与启动数据 · L62 · 436
- SYS-MAPS §2 地图描述 `map.json` · L70 · 693
- SYS-MAPS §3 切换 · L82 · 2.4k
  - SYS-MAPS §3.1 一个 World3D，先卸后装 · L86 · 151
  - SYS-MAPS §3.2 八步（切图部分主线之后） · L90 · 1.1k
  - SYS-MAPS §3.3 规则（主线之后） · L110 · 567
  - SYS-MAPS §3.4 入口 · L119 · 307
  - SYS-MAPS §3.5 同图重载不泄漏（主线之后） · L128 · 179
- SYS-MAPS §4 世界状态：时间与季节全局，天气按图求值 · L132 · 699
- SYS-MAPS §5 相机、行走与安全点（主线之后） · L150 · 429
- SYS-MAPS §6 归属与 `MapScope` · L161 · 600
- SYS-MAPS §7 全局参数 `map_rect` · L169 · 691
- SYS-MAPS §8 内存、包体与单图半径 · L179 · 331
- SYS-MAPS §9 以后的方向 · L185 · 184
- SYS-MAPS §10 待验证（P0） · L189 · 265

## SYS-MOTION-ENV · 风、飘落、远景与季节氛围

`docs/systems/SYS-MOTION-ENV-风与环境动态.md` · 4.9k 字 · 风及其连锁反应、飘落物与瞬时痕迹、流体、光与远景的缓慢变化、季节氛围、粒子类的解析运动
关键词：风场、阵风、IWindField、飘落池、飘落物、萤火虫、解析运动、季节氛围、风量、雾海、云漂移、MultiMesh

- SYS-MOTION-ENV §0 结论 · L23 · 569
  - ★ SYS-MOTION-ENV §0.1 接入要点 · L32 · 165
- SYS-MOTION-ENV §1 风：一个起因带动一串变化 · L38 · 509
- SYS-MOTION-ENV §2 悬浮物与瞬时痕迹 · L46 · 678
  - SYS-MOTION-ENV §2.1 飘落物 · L48 · 407
  - SYS-MOTION-ENV §2.2 行走时的瞬时痕迹 · L61 · 256
- SYS-MOTION-ENV §3 流体 · L65 · 185
- SYS-MOTION-ENV §4 光的缓慢变化 · L70 · 450
- SYS-MOTION-ENV §5 远景 · L79 · 343
- SYS-MOTION-ENV §6 季节氛围 · L85 · 646
- SYS-MOTION-ENV §7 粒子类与解析运动 · L95 · 656
- SYS-MOTION-ENV §8 待验证 · L105 · 53

## SYS-MOTION-LIFE · 生命体、室内微动与偶发事件

`docs/systems/SYS-MOTION-LIFE-生命体与偶发事件.md` · 4.4k 字 · 动物的小动作节奏与对行走身体作出反应的动物、室内与钟楼的微动、全片唯一的偶发事件
关键词：反应动物、动物小动作、室内微动、钟锤、偶发事件、晚归者、late-returner、incidents.json

- SYS-MOTION-LIFE §0 结论 · L23 · 434
  - ★ SYS-MOTION-LIFE §0.1 接入要点 · L31 · 121
- SYS-MOTION-LIFE §1 生命体 · L36 · 1.4k
  - SYS-MOTION-LIFE §1.1 对行走身体作出反应的动物 · L51 · 692
- SYS-MOTION-LIFE §2 室内与钟楼的微动 · L64 · 440
- SYS-MOTION-LIFE §3 偶发事件（全片唯一） · L74 · 1.2k
- SYS-MOTION-LIFE §4 待验证 · L86 · 153

## SYS-MOTION · 动态与事件：时钟、节奏与转场

`docs/systems/SYS-MOTION-动态与事件.md` · 6.2k 字 · 两个时钟与一拍二、动作幅度上限、转场的唯一详述（改时间、换季、换天气、跳转与同时转场）；风与环境动态、生命体与偶发事件见子文档
关键词：动态、两个时钟、动画时钟、AnimTime、一拍二、step_rate、480 s 回绕、转场、换季转场、改时间、同时转场、减少动态效果

- SYS-MOTION §0 结论 · L22 · 1.0k
  - ★ SYS-MOTION §0.1 接入要点 · L37 · 361
- SYS-MOTION §1 时钟与计时 · L45 · 1.0k
  - SYS-MOTION §1.1 两个时钟 · L47 · 365
  - SYS-MOTION §1.2 计时规则 · L53 · 433
  - SYS-MOTION §1.3 各档流逝的表现 · L60 · 126
  - SYS-MOTION §1.4 镜头 · L64 · 106
- SYS-MOTION §2 节奏规则 · L68 · 710
- SYS-MOTION §3 转场 · L79 · 2.4k
  - SYS-MOTION §3.1 改时间 · L83 · 227
  - SYS-MOTION §3.2 换季 · L91 · 716
  - SYS-MOTION §3.3 跳转、传送与模式切换 · L107 · 175
  - SYS-MOTION §3.4 换天气 · L111 · 559
  - SYS-MOTION §3.5 叠加与优先级 · L119 · 210
  - SYS-MOTION §3.6 减少动态效果 · L127 · 127
  - SYS-MOTION §3.7 验收原则 · L131 · 53
  - SYS-MOTION §3.8 协调者 · L135 · 95
  - SYS-MOTION §3.9 开始页、进场过场与读档 · L139 · 132
- SYS-MOTION §4 待验证 · L143 · 46

## SYS-SAVE-FLOW · 保存时机与读档流程：安全时刻、退出、读档、世界实例、一致性

`docs/systems/SYS-SAVE-FLOW-保存时机与读档流程.md` · 6.2k 字 · 保存时机与限频、安全时刻与落定快照、退出保存、读档顺序、世界实例、状态一致、新游戏、校验修复
关键词：保存、读档、自动保存、安全时刻、落定快照、续跑等价、世界实例、校验修复、退出保存、现实时间不计、SaveReason、LoadScope、CaptureContext、RestoreContext、NewGameContext、SaveStatus、SaveResult、LoadStatus、LoadResult、SaveState、IWorldState.Instance

- SYS-SAVE-FLOW §0 结论 · L23 · 521
  - ★ SYS-SAVE-FLOW §0.1 接入要点 · L30 · 277
- SYS-SAVE-FLOW §1 保存时机与可靠性 · L36 · 1.2k
  - SYS-SAVE-FLOW §1.1 时机 · L38 · 376
  - SYS-SAVE-FLOW §1.2 安全时刻与落定快照 · L51 · 341
  - SYS-SAVE-FLOW §1.3 写盘与失败 · L57 · 103
  - SYS-SAVE-FLOW §1.4 退出 · L61 · 289
  - SYS-SAVE-FLOW §1.5 保存状态与失败反馈 · L70 · 87
- SYS-SAVE-FLOW §2 加载与一致性 · L74 · 2.7k
  - SYS-SAVE-FLOW §2.1 加载流程 · L76 · 892
  - SYS-SAVE-FLOW §2.2 世界实例：`IWorldState.Instance` · L90 · 384
  - SYS-SAVE-FLOW §2.3 "状态一致"的定义 · L96 · 379
  - SYS-SAVE-FLOW §2.4 天气 · L104 · 156
  - SYS-SAVE-FLOW §2.5 新游戏 · L108 · 396
  - SYS-SAVE-FLOW §2.6 校验修复 · L115 · 453
- SYS-SAVE-FLOW §3 关着游戏的那段现实时间不计 · L130 · 272
- SYS-SAVE-FLOW §4 待验证（P0） · L137 · 172

## SYS-SAVE-FORMAT · 格式、迁移、损坏处理与安全：容器、段版本、坏档、设置文件

`docs/systems/SYS-SAVE-FORMAT-格式迁移损坏与安全.md` · 4.4k 字 · 存档容器与序列化约定、格式号与段版本、坏档退回上一代、唯一的设置文件 settings.json、读档输入的安全
关键词：存档容器、序列化、格式号、段版本、存档迁移、存档损坏、设置文件、settings.json、存档安全、缩略图、ReadOnlyReason

- SYS-SAVE-FORMAT §0 结论 · L23 · 626
  - ★ SYS-SAVE-FORMAT §0.1 接入要点 · L31 · 264
- SYS-SAVE-FORMAT §1 格式与迁移 · L38 · 1.1k
  - SYS-SAVE-FORMAT §1.1 容器 · L40 · 429
  - SYS-SAVE-FORMAT §1.2 缩略图 · L51 · 79
  - SYS-SAVE-FORMAT §1.3 序列化约定 · L55 · 228
  - SYS-SAVE-FORMAT §1.4 版本与迁移 · L61 · 329
- SYS-SAVE-FORMAT §2 存档损坏 · L68 · 424
- SYS-SAVE-FORMAT §3 设置文件：唯一的 `settings.json` · L81 · 672
- SYS-SAVE-FORMAT §4 安全：为什么禁止用 Resource 和 ConfigFile 存读档 · L91 · 549

## SYS-SAVE · 存档与持久化：存档模型、存什么、不存什么

`docs/systems/SYS-SAVE-存档与持久化.md` · 3.7k 字 · 存档位与世界线、各存档段存什么不存什么；保存读档与格式安全见子文档
关键词：存档、存档位、世界线、存档段、单实例锁、ISaveService、ISaveSection、SaveSectionAttribute、RestorePhase

- SYS-SAVE §0 结论 · L22 · 659
  - ★ SYS-SAVE §0.1 接入要点 · L30 · 242
- SYS-SAVE §1 存档模型 · L35 · 634
- SYS-SAVE §2 存什么、不存什么 · L45 · 1.2k

## SYS-TIME-LOOK · 色彩脚本、四季内容与灯光时刻表

`docs/systems/SYS-TIME-LOOK-色彩脚本四季与灯光.md` · 6.3k 字 · 色彩脚本的组织与情绪、四季修正与英雄帧、四季内容与阈值生长、灯光与生活痕迹的时刻表设计意图
关键词：色彩脚本、关键帧、英雄帧、四季内容、阈值生长、四季修正、难看时段、灯光时刻表、有效太阳高度、初灯组、生活痕迹

- SYS-TIME-LOOK §0 结论 · L23 · 459
  - ★ SYS-TIME-LOOK §0.1 接入要点 · L32 · 169
- SYS-TIME-LOOK §1 色彩脚本 · L37 · 2.4k
  - SYS-TIME-LOOK §1.1 组织方式 · L39 · 708
  - SYS-TIME-LOOK §1.2 秋季基准关键帧的情绪 · L48 · 636
  - SYS-TIME-LOOK §1.3 四季修正与专属覆盖 · L69 · 387
  - SYS-TIME-LOOK §1.4 难看时段的改造 · L84 · 254
  - SYS-TIME-LOOK §1.5 英雄帧 · L92 · 438
- SYS-TIME-LOOK §2 四季内容 · L109 · 1.4k
- SYS-TIME-LOOK §3 灯光与生活痕迹的时刻表 · L135 · 1.1k

## SYS-TIME · 时间与季节系统：世界状态、美术日月、过渡口径

`docs/systems/SYS-TIME-时间与季节.md` · 5.2k 字 · 世界状态三个量与只向前的时间规则、默认状态落金初灯、美术太阳与月亮、过渡的定目标顺序与用时、质量底线
关键词：世界状态、钟点、季节、世界时间、世界实例、流逝倍率、落金初灯、默认状态、只向前、美术太阳、美术月亮、满月、定目标顺序、质量底线

- SYS-TIME §0 结论 · L22 · 1.1k
  - ★ SYS-TIME §0.1 接入要点 · L38 · 480
- SYS-TIME §1 世界状态与时间规则 · L47 · 1.7k
  - SYS-TIME §1.1 默认状态 · L49 · 478
  - SYS-TIME §1.2 三个量与时间规则 · L57 · 1.2k
- SYS-TIME §2 美术太阳与月亮 · L72 · 748
  - SYS-TIME §2.1 太阳 · L74 · 483
  - SYS-TIME §2.2 月亮 · L89 · 175
  - SYS-TIME §2.3 与主光的关系 · L93 · 76
- SYS-TIME §3 过渡 · L97 · 243
- SYS-TIME §4 质量底线 · L104 · 443

## SYS-TITLE-CINEMATIC · 进入过场与返回标题：新游戏、继续、跳过、云卡

`docs/systems/SYS-TITLE-CINEMATIC-进入过场与返回标题.md` · 7.8k 字 · 主线先做新游戏完整版与雾幕直达、跳过与安全落点、过场规则、云卡；简短版、继续过场、返回过场、整条路径预取在主线之后
关键词：新游戏过场、继续过场、返回标题、跳过过场、安全落点、开场过场设置、过场专属元素、云卡、放映层、标题卡

- SYS-TITLE-CINEMATIC §0 结论 · L23 · 723
  - ★ SYS-TITLE-CINEMATIC §0.1 接入要点 · L32 · 255
- SYS-TITLE-CINEMATIC §1 新游戏过场 · L38 · 2.1k
  - SYS-TITLE-CINEMATIC §1.1 分镜 · L40 · 1.1k
  - SYS-TITLE-CINEMATIC §1.2 字幕、时间读数与标题卡 · L58 · 415
  - SYS-TITLE-CINEMATIC §1.3 过场专属元素 · L67 · 380
  - SYS-TITLE-CINEMATIC §1.4 简短版（主线之后） · L77 · 138
- SYS-TITLE-CINEMATIC §2 继续过场与返回标题 · L81 · 1.1k
  - SYS-TITLE-CINEMATIC §2.1 继续过场（约 9 s，主线之后） · L85 · 648
  - SYS-TITLE-CINEMATIC §2.2 返回标题过场（约 3 s，不可跳过，主线之后） · L95 · 268
- SYS-TITLE-CINEMATIC §3 跳过、安全落点与设置项 · L99 · 744
- SYS-TITLE-CINEMATIC §4 与时间不可逆的关系 · L116 · 390
- SYS-TITLE-CINEMATIC §5 过场的规则 · L123 · 640
- SYS-TITLE-CINEMATIC §6 云卡 · L131 · 301
- SYS-TITLE-CINEMATIC §7 流式预取 · L137 · 333
- SYS-TITLE-CINEMATIC §8 待验证 · L145 · 87

## SYS-TITLE · 开始页与启动：扉页、开始页、应用状态

`docs/systems/SYS-TITLE-开始页与启动.md` · 6.6k 字 · 冷启动扉页与失败页、开始页 T0 与菜单和片名块、各应用状态下的约定、多地图登记
关键词：开始页、标题页、主菜单、扉页、启动失败页、T0、云隙望穹、片名块、应用状态

- SYS-TITLE §0 结论 · L22 · 796
  - ★ SYS-TITLE §0.1 接入要点 · L29 · 433
- SYS-TITLE §1 启动序列与扉页 · L38 · 1.3k
- SYS-TITLE §2 开始页 · L65 · 1.8k
  - SYS-TITLE §2.1 T0 镜头"云隙望穹" · L67 · 585
  - SYS-TITLE §2.2 菜单 · L77 · 514
  - SYS-TITLE §2.3 片名块 · L85 · 384
  - SYS-TITLE §2.4 首次启动与再次启动 · L93 · 314
- SYS-TITLE §3 应用状态下的约定 · L100 · 824
- SYS-TITLE §4 多地图 · L116 · 336
- SYS-TITLE §5 不做的事 · L122 · 231
- SYS-TITLE §6 待验证 · L128 · 157

## SYS-VIEW-MEDIUM · 介质呈现：镜头在楼里、地下、水下、雾海里

`docs/systems/SYS-VIEW-MEDIUM-介质呈现.md` · 4.0k 字 · 镜头处在空气、楼里、地下、水下、雾海时的画面与声音规则：介质判定与仲裁、楼体剖切变体、地层雾、水下与半入水、雾海兜底
关键词：介质呈现、介质判定、介质仲裁、剖切变体、剖切色、I0 兜底层、简化室内、完整室内、地层雾、水下、半入水、水下回差、雾海、共用雾函数、IMediumState、IMediumPresenter、CameraMedium、MediumKind

- SYS-VIEW-MEDIUM §0 结论 · L23 · 531
  - ★ SYS-VIEW-MEDIUM §0.1 接入要点 · L31 · 219
- SYS-VIEW-MEDIUM §1 判定、仲裁与渲染原则 · L37 · 660
- SYS-VIEW-MEDIUM §2 楼里 · L47 · 710
- SYS-VIEW-MEDIUM §3 地下 · L57 · 257
- SYS-VIEW-MEDIUM §4 水下 · L65 · 560
- SYS-VIEW-MEDIUM §5 雾海 · L74 · 164
- SYS-VIEW-MEDIUM §6 待验证 · L78 · 86

## SYS-VIEW · 观察模式与旁观漫游

`docs/systems/SYS-VIEW-观察模式与开场.md` · 5.6k 字 · 两种观察模式总述、旁观漫游的操作与活动范围、初始位置、镜头规则、3D 与平面地图的分工
关键词：旁观模式、观察模式、飞行速度档、视场角、鼠标灵敏度、活动范围、初始位置、镜头不会自己动、自动运镜、命令行参数、ICameraRig、Pose

- SYS-VIEW §0 结论 · L22 · 1.1k
  - ★ SYS-VIEW §0.1 接入要点 · L37 · 362
- SYS-VIEW §1 不做的事 · L44 · 199
- SYS-VIEW §2 旁观者漫游 · L48 · 2.8k
  - SYS-VIEW §2.1 操作 · L52 · 846
  - SYS-VIEW §2.2 触屏 · L72 · 27
  - SYS-VIEW §2.3 活动范围 · L76 · 245
  - SYS-VIEW §2.4 初始位置 · L84 · 533
  - SYS-VIEW §2.5 镜头不会自己动 · L93 · 768
  - SYS-VIEW §2.6 操作说明与提示 · L106 · 250
- SYS-VIEW §3 启动与开场 · L115 · 39
- SYS-VIEW §4 环境痕迹与平面地图的分工 · L119 · 250
- SYS-VIEW §5 待验证 · L125 · 68

## SYS-WALK-BIKE · 骑行：共享单车的运动、可骑范围与取还

`docs/systems/SYS-WALK-BIKE-骑行.md` · 2.4k 字 · 行走里的骑行分支：运动规则、可骑范围与许可、边界与下车、取车还车临停回收与存档
关键词：骑行、共享单车、可骑许可、可骑范围、停放点、取车、还车、临停、回收、下车、制动、后退

- SYS-WALK-BIKE §0 结论 · L23 · 451
  - ★ SYS-WALK-BIKE §0.1 接入要点 · L30 · 251
- SYS-WALK-BIKE §1 骑行 · L36 · 1.1k
  - SYS-WALK-BIKE §1.1 运动 · L40 · 268
  - SYS-WALK-BIKE §1.2 可骑范围 · L46 · 269
  - SYS-WALK-BIKE §1.3 边界与下车 · L52 · 179
  - SYS-WALK-BIKE §1.4 取车、还车、临停与回收 · L57 · 297
- SYS-WALK-BIKE §2 待验证 · L65 · 79

## SYS-WALK-BOUNDS · 可行走范围、边界与碰撞

`docs/systems/SYS-WALK-BOUNDS-可行走范围边界与碰撞.md` · 7.6k 字 · 可行走范围与禁行区、边界收口与隐形墙、楼内的门与电梯、涉水游泳回位与安全点、行走走廊与路线、碰撞生成与就绪
关键词：可行走范围、可行走数据、禁行区、高度场地面、边界收口、隐形墙、校界缓停、软墙提示、碰撞顶加高、水平外扩、安全点、回位、涉水、开放泳区、游泳出口、碰撞就绪、自动门、电梯关门换位、行走走廊、推荐路线、碰撞级 C、SurfaceTag、FootMat、WalkClass、Collider、ICollisionWorld

- SYS-WALK-BOUNDS §0 结论 · L23 · 553
  - ★ SYS-WALK-BOUNDS §0.1 接入要点 · L31 · 292
- SYS-WALK-BOUNDS §1 可行走范围 · L39 · 850
- SYS-WALK-BOUNDS §2 边界收口 · L58 · 1.2k
- SYS-WALK-BOUNDS §3 楼内的门与电梯 · L86 · 623
- SYS-WALK-BOUNDS §4 水、游泳、回位与安全点 · L100 · 999
- SYS-WALK-BOUNDS §5 行走走廊与节奏 · L124 · 212
- SYS-WALK-BOUNDS §6 推荐路线 · L130 · 193
- SYS-WALK-BOUNDS §7 碰撞与就绪 · L134 · 1.4k

## SYS-WALK · 行走模式：身体、手感、视角与切换

`docs/systems/SYS-WALK-行走模式.md` · 7.4k 字 · 行走模式的玩家身体与步相、手感目标、第一与第三人称镜头与舒适度、旁观与行走的切换、控制器的规则
关键词：行走模式、手感、模式切换、玩家身体、步相、自动登阶、视角舒适度、视角晃动、第一人称、第三人称、吊杆、身体淡入淡出、控制器规则、运动快照、土狼时间、IViewControl、ModeSwitchVia、ModeSwitchResult、IWalkTelemetry

- SYS-WALK §0 结论 · L22 · 657
  - ★ SYS-WALK §0.1 接入要点 · L34 · 252
- SYS-WALK §1 身体、声音与步相 · L41 · 715
- SYS-WALK §2 手感目标 · L52 · 630
- SYS-WALK §3 视角、镜头与舒适度 · L66 · 2.0k
  - SYS-WALK §3.1 视场角、舒适度与键位 · L68 · 598
  - SYS-WALK §3.2 行走模式的镜头 · L78 · 1.4k
- SYS-WALK §4 模式切换 · L111 · 872
- SYS-WALK §5 控制器的规则 · L126 · 778
- SYS-WALK §6 待验证 · L137 · 328

## SYS-WEATHER-PROCESS · 自动天气与存档语义

`docs/systems/SYS-WEATHER-PROCESS-自动天气与存档语义.md` · 4.2k 字 · 自动天气是种子、世界时间、地图的纯函数：片段模板与硬规则、随机数与种子、时间与天气、固定点、存档只存手动与钉住
关键词：自动天气、纯函数、天气种子、计数器子流、不应期、片段模板、保持晴、钉住、固定点、天气段、天气存档、IWeatherProcess、WeatherBoot、WeatherTick

- SYS-WEATHER-PROCESS §0 结论 · L23 · 584
  - ★ SYS-WEATHER-PROCESS §0.1 接入要点 · L32 · 220
- SYS-WEATHER-PROCESS §1 自动天气：前向随机过程 · L38 · 2.1k
  - SYS-WEATHER-PROCESS §1.1 模型 · L40 · 332
  - SYS-WEATHER-PROCESS §1.2 硬规则（生成时满足） · L47 · 214
  - SYS-WEATHER-PROCESS §1.3 各季的气候风味 · L53 · 392
  - SYS-WEATHER-PROCESS §1.4 随机数与复现 · L64 · 310
  - SYS-WEATHER-PROCESS §1.5 时间与天气 · L70 · 415
  - SYS-WEATHER-PROCESS §1.6 固定点 · L80 · 387
- SYS-WEATHER-PROCESS §2 状态与存档语义 · L87 · 417
- SYS-WEATHER-PROCESS §3 确定性 · L94 · 108
- SYS-WEATHER-PROCESS §4 待验证 · L101 · 66

## SYS-WEATHER · 天气：天气清单、画面规则、可读性与湿润积水

`docs/systems/SYS-WEATHER-天气.md` · 7.7k 字 · 八格天气与季节合法性、每种天气的画面规则、雨的可读性、闪电彩虹的条件与安全规则、湿润积水降水的规则、质量底线
关键词：天气、天气格、手动天气、天气向量、太阳雨、雨的可读性、闪电、彩虹、降水池、湿润、积水、天光方向、钉住天气、保持晴、天气巡回

- SYS-WEATHER §0 结论 · L22 · 1.1k
  - ★ SYS-WEATHER §0.1 接入要点 · L36 · 573
- SYS-WEATHER §1 天气清单与界面格 · L47 · 630
- SYS-WEATHER §2 每种天气的画面规则 · L64 · 1.7k
- SYS-WEATHER §3 雨的可读性规则 · L91 · 550
- SYS-WEATHER §4 与四季、昼夜的配合 · L103 · 247
- SYS-WEATHER §5 闪电与彩虹 · L111 · 527
- SYS-WEATHER §6 湿润、积水与降水的规则 · L122 · 1.1k
- SYS-WEATHER §7 质量底线 · L139 · 375
- SYS-WEATHER §8 待验证 · L149 · 72

## UI-NAV-WAYPOINTS · 点位、跳转与传送

`docs/ui/UI-NAV-WAYPOINTS-点位跳转与传送.md` · 3.3k 字 · 24 个观景点位的设计意图与合法性，飞过去的方式与到达打断，预取与传送的界面侧约定
关键词：点位、观景点、点位目录编号、waypoints.json、锚点不是点位、推荐状态、进场路径、飞过去、滑移、弧线飞行、雾幕弧线、到达与打断、传送、跳转压力

- UI-NAV-WAYPOINTS §0 结论 · L22 · 533
  - ★ UI-NAV-WAYPOINTS §0.1 接入要点 · L30 · 171
- UI-NAV-WAYPOINTS §1 点位 · L35 · 808
- UI-NAV-WAYPOINTS §2 跳转与传送 · L46 · 1.0k
  - UI-NAV-WAYPOINTS §2.1 飞过去 · L48 · 255
  - UI-NAV-WAYPOINTS §2.2 到达与打断 · L54 · 246
  - UI-NAV-WAYPOINTS §2.3 预取与速度律豁免 · L59 · 201
  - UI-NAV-WAYPOINTS §2.4 传送与减少动态效果 · L63 · 289
- UI-NAV-WAYPOINTS §3 待验证 · L70 · 150

## UI-NAV · 界面与操作说明

`docs/ui/UI-NAV-界面与操作说明.md` · 8.7k 字 · 界面有什么、怎么操作：界面态与漫游态、组件的内容与交互、输入优先级与关键键位、无障碍与文本约定；点位与跳转见子文档
关键词：界面交互、快捷键、输入优先级、界面态、漫游态、自动隐藏、四角归属、时间环操作、天气签条、速度档、临时提示、到达标题、设置项、无障碍、读屏、文本键、地名来源

- UI-NAV §0 结论 · L21 · 1.0k
  - ★ UI-NAV §0.1 接入要点 · L36 · 467
- UI-NAV §1 允许什么、禁止什么 · L44 · 375
- UI-NAV §2 界面态、漫游态与隐藏 · L53 · 878
- UI-NAV §3 组件的内容与交互 · L72 · 3.1k
  - UI-NAV §3.1 时间环 · L74 · 577
  - UI-NAV §3.2 季节、速度与观察模式 · L83 · 415
  - UI-NAV §3.3 点位目录 · L89 · 217
  - UI-NAV §3.4 首次进入与到达标题 · L93 · 386
  - UI-NAV §3.5 操作说明与设置 · L98 · 592
  - UI-NAV §3.6 临时提示 · L106 · 410
  - UI-NAV §3.7 天气签条 · L114 · 506
- UI-NAV §4 输入 · L124 · 1.4k
  - UI-NAV §4.1 Esc 与单键快捷键 · L126 · 318
  - UI-NAV §4.2 关键键位 · L132 · 575
  - UI-NAV §4.3 输入优先级 · L150 · 504
- UI-NAV §5 无障碍与语言 · L164 · 821
- UI-NAV §6 待验证 · L177 · 92

## UI-VISUAL-COMPONENTS · 界面字体、形状、图标、组件外观与画面布局

`docs/ui/UI-VISUAL-COMPONENTS-字体形状图标与组件.md` · 5.9k 字 · 界面字体与字号、形状与材质、图标体系、组件六种状态与时间环等外观、画面布局
关键词：界面字体、思源黑体、字号阶梯、切角、1号脚、发丝线、过孔灯、走线、衍射光边、面板材质、轻度磨砂、图标体系、窗口图标、八角晶片、气象填图符号、通电隐喻、时间环外观、天气签条、速度LED条、存档卡、存储模组、等待灯、画面布局、中央60%、ui_metrics.json、IPaletteProbe

- UI-VISUAL-COMPONENTS §0 结论 · L22 · 528
  - ★ UI-VISUAL-COMPONENTS §0.1 接入要点 · L30 · 175
- UI-VISUAL-COMPONENTS §1 字体与字号 · L35 · 400
- UI-VISUAL-COMPONENTS §2 形状与材质 · L41 · 841
- UI-VISUAL-COMPONENTS §3 图标体系 · L51 · 545
  - UI-VISUAL-COMPONENTS §3.1 规格 · L53 · 163
  - UI-VISUAL-COMPONENTS §3.2 意象 · L57 · 238
  - UI-VISUAL-COMPONENTS §3.3 窗口图标"八角晶片" · L64 · 133
- UI-VISUAL-COMPONENTS §4 组件外观 · L68 · 1.6k
  - UI-VISUAL-COMPONENTS §4.1 六种状态（"通电"隐喻） · L70 · 216
  - UI-VISUAL-COMPONENTS §4.2 控件与容器 · L75 · 320
  - UI-VISUAL-COMPONENTS §4.3 时间环 · L81 · 351
  - UI-VISUAL-COMPONENTS §4.4 天气签条、速度条与到达标题 · L90 · 306
  - UI-VISUAL-COMPONENTS §4.5 存档卡"存储模组"与等待灯 · L96 · 363
- UI-VISUAL-COMPONENTS §5 画面布局 · L103 · 817
- UI-VISUAL-COMPONENTS §6 待验证 · L120 · 162

## UI-VISUAL-MOTION · 界面动效与减少动态效果

`docs/ui/UI-VISUAL-MOTION-动效与减少动态效果.md` · 3.6k 字 · 界面动效原则、八个母题、频率分级、曲线时长与辉光闪烁上限，减少动态效果下的替代与无障碍
关键词：动效语言、动效母题、M1走线流光、M2扫描揭示、M3网格逐格点亮、M4数值滚动、M5衍射边、M6量化出入场、M7示波器、M8呼吸灯、频率分级、辉光上限、闪烁上限、减少动态效果、MotionPolicy、IMotionSettings、降低透明度、主题锁定

- UI-VISUAL-MOTION §0 结论 · L22 · 489
  - ★ UI-VISUAL-MOTION §0.1 接入要点 · L30 · 148
- UI-VISUAL-MOTION §1 动效语言 · L35 · 1.8k
  - UI-VISUAL-MOTION §1.1 原则与两种时间 · L37 · 321
  - UI-VISUAL-MOTION §1.2 八个母题 · L45 · 659
  - UI-VISUAL-MOTION §1.3 频率分级与声音 · L58 · 329
  - UI-VISUAL-MOTION §1.4 曲线、时长与上限 · L64 · 478
- UI-VISUAL-MOTION §2 减少动态效果与无障碍 · L71 · 509
  - UI-VISUAL-MOTION §2.1 "减少动态效果"下的替代 · L73 · 336
  - UI-VISUAL-MOTION §2.2 无障碍 · L81 · 156

## UI-VISUAL · 界面视觉语言：设计原则、色彩令牌、主题与实现边界

`docs/ui/UI-VISUAL-界面视觉语言.md` · 6.4k 字 · 界面设计原则、色彩分工与令牌、晶白与深屏两套主题、实现边界；字体形状图标组件布局与动效见子文档
关键词：界面视觉、色彩令牌、清蓝、晶白、深屏、昼夜交班、常驻件、对比度规则、ui.json、ui_night、UiThemeBuilder、UiThemeClock、ui_panel、ui_screen

- UI-VISUAL §0 结论 · L21 · 1.0k
  - ★ UI-VISUAL §0.1 接入要点 · L37 · 275
- UI-VISUAL §1 设计原则 · L42 · 556
- UI-VISUAL §2 色彩分工与常驻件 · L58 · 1.0k
  - UI-VISUAL §2.1 场景暖，界面冷 · L60 · 362
  - UI-VISUAL §2.2 与场景色不冲突的规则 · L69 · 227
  - UI-VISUAL §2.3 高调画面上的面板与常驻件 · L75 · 414
- UI-VISUAL §3 令牌 · L83 · 1.7k
  - UI-VISUAL §3.1 清蓝阶梯 · L87 · 180
  - UI-VISUAL §3.2 中性、专用与语义色 · L91 · 245
  - UI-VISUAL §3.3 两套主题的语义令牌 · L96 · 248
  - UI-VISUAL §3.4 昼夜切换 · L102 · 427
  - UI-VISUAL §3.5 对比度规则 · L110 · 248
  - UI-VISUAL §3.6 登记 · L117 · 150
- UI-VISUAL §4 实现边界 · L121 · 1.0k

## ENG-ARCH · 工程架构

`docs/engineering/ENG-ARCH-工程架构.md` · 9.4k 字 · 当前分层、依赖方向与唯一生产链，边界、确定性、契约、烘焙与桩的架构准则与后续导向；实现细节以代码为准
关键词：工程架构、分层、依赖方向、命名空间边界、程序集、唯一生产链、KernelStage、组合根、数据流、自动发现、特性注册、ContentModule、KernelService、KernelNode、Autoload、原生资源创建方、确定性、同机同输入同输出、StableHash、Rng、BannedApiAnalyzers、架构规则、生命周期、契约、契约申请、枚举穷举、本机烘焙、.tbin、缓存键、英雄资产、桩、占位、仓库脚本自成一体、架构导向、扩展方式、net10.0、技术栈

- ENG-ARCH §0 结论 · L21 · 1.4k
  - ★ ENG-ARCH §0.1 接入要点 · L33 · 753
- ENG-ARCH §1 当前架构 · L45 · 1.2k
  - ENG-ARCH 技术栈 · L47 · 185
  - ENG-ARCH 分层 · L51 · 619
  - ENG-ARCH 生产链 · L63 · 265
  - ENG-ARCH 数据流 · L71 · 119
- ENG-ARCH §2 边界与确定性 · L75 · 1.9k
  - ENG-ARCH 依赖方向 · L77 · 629
  - ENG-ARCH 确定性与禁用写法 · L85 · 560
  - ENG-ARCH 自动发现与模块接入 · L97 · 340
  - ENG-ARCH 仓库脚本自成一体 · L104 · 101
  - ENG-ARCH 生命周期与发布 · L108 · 233
- ENG-ARCH §3 契约 · L115 · 566
- ENG-ARCH §4 资源与烘焙 · L124 · 1.0k
- ENG-ARCH §5 未交付能力的占位边界 · L135 · 359
- ENG-ARCH §6 架构导向 · L144 · 807
- ENG-ARCH §7 扩展点（RPG） · L157 · 448
- ENG-ARCH §8 待验证（P0） · L169 · 583

## ENG-BUDGET-LOD · 细节级别、速度律与渲染参数

`docs/engineering/ENG-BUDGET-LOD-细节级别与速度律.md` · 4.3k 字 · 细节级别 L0+–L4 与 64 m 分块、旁观速度律与目的地预取、深度与近裁剪面、阴影后期倒影灯光的预算参数
关键词：细节级别、L0+、L4、分块、四叉树、屏幕误差、速度律、目的地预取、到达保证、近裁剪面、远裁剪面、反向 Z、坐标精度、PSSM、光柱数量、倒影分辨率、灯盏数

- ENG-BUDGET-LOD §0 结论 · L22 · 609
  - ★ ENG-BUDGET-LOD §0.1 接入要点 · L29 · 278
- ENG-BUDGET-LOD §1 细节级别与分块 · L35 · 1.3k
- ENG-BUDGET-LOD §2 流式加载与速度 · L59 · 454
- ENG-BUDGET-LOD §3 深度与近裁剪面 · L66 · 387
- ENG-BUDGET-LOD §4 阴影、后期、倒影、灯光 · L74 · 650
- ENG-BUDGET-LOD §5 待验证（P0） · L84 · 125

## ENG-BUDGET-RULES · 推送硬规则与工程设置

`docs/engineering/ENG-BUDGET-RULES-推送硬规则.md` · 5.9k 字 · 23 条推送硬规则与 project.godot、导出、Environment 等工程设置的规则
关键词：硬规则、推送规则、白名单着色器、不得新增光源、线程边界、全局着色器参数登记、风格底线不可关、存档只走一条路、本机烘焙同机确定、工程设置、渲染器、Shader Baker、版本锁定

- ENG-BUDGET-RULES §0 结论 · L23 · 846
  - ★ ENG-BUDGET-RULES §0.1 接入要点 · L29 · 570
- ENG-BUDGET-RULES §1 硬规则（违反任何一条都不能推送） · L39 · 3.4k
  - ENG-BUDGET-RULES §1.1 渲染、材质与时间（第 1–10 条） · L41 · 1.5k
  - ENG-BUDGET-RULES §1.2 烘焙、线程、存档与音频（第 11–23 条） · L54 · 1.9k
- ENG-BUDGET-RULES §2 工程设置 · L70 · 737

## ENG-BUDGET · 性能预算

`docs/engineering/ENG-BUDGET-性能预算.md` · 4.5k 字 · 性能预算的口径：设备档与预设、GPU 帧时分配、额度怎么分与超了怎么办、怎么测；细节级别、速度律与推送硬规则在子文档
关键词：性能预算、额度、额度分配、budgets.json、KernelBudgets、主代理余量、预设资源上限、设备档目标、帧率目标、最低配置、独显中、独显高、楼内视角额度、GPU 帧时分配、阴影图档位、纹理上限

- ENG-BUDGET §0 结论 · L21 · 770
  - ★ ENG-BUDGET §0.1 接入要点 · L35 · 187
- ENG-BUDGET §1 总体要点 · L41 · 347
- ENG-BUDGET §2 预设与设备档的预算 · L48 · 1.5k
- ENG-BUDGET §3 额度怎么分 · L79 · 393
- ENG-BUDGET §4 怎么测 · L87 · 195
- ENG-BUDGET §5 待验证（P0） · L91 · 196

## ENG-CODE · 代码结构准则

`docs/engineering/ENG-CODE-代码结构准则.md` · 4.1k 字 · 写代码时的结构规矩：依赖方向落地、规则值与数据各一份、先复用、不留死代码、控制规模、测试代码写法
关键词：代码结构准则、软件工程、坏味道、屎山、依赖方向、内核引用系统、生成层实现类、点名模块、单一来源、魔法数、规则值、规则类、数据与代码分离、Tune 只放规则值、实体 ID 写数据、复用、公共层、复制粘贴、死代码、死桩、诊断残留、单实现接口、吞错、兜底、文件规模、构造参数、清单文件、程序集过碎、测试写法、测试夹具、假实现、性质断言、钉哈希、输入哈希、提交后提醒

- ENG-CODE §0 结论 · L22 · 702
  - ★ ENG-CODE §0.1 接入要点 · L31 · 407
- ENG-CODE §1 依赖方向 · L41 · 351
- ENG-CODE §2 规则值与数据各一份 · L50 · 656
- ENG-CODE §3 先用现成，再复用，不复制 · L61 · 678
- ENG-CODE §4 不留死代码 · L70 · 209
- ENG-CODE §5 规模 · L77 · 160
- ENG-CODE §6 测试代码怎么写 · L83 · 513

## ENG-GATES-SHOTS · 审查视角与截图

`docs/engineering/ENG-GATES-SHOTS-审查视角与截图.md` · 5.6k 字 · 命令行规则与截图默认串、截图方式、固定机位表；V/E/T 视角表、飞行与行走路线、时季抽样与巡检点轮换（主线之后）
关键词：审查视角、截图默认串、命令行规则、截图方式、截图就绪、流式屏障、录像、视角表、V1、E1、T0、飞行路线、压力路线、点位 P00–P23、行走路线 W1–W14、时季抽样、巡检点轮换、review.json

- ENG-GATES-SHOTS §0 结论 · L22 · 505
  - ★ ENG-GATES-SHOTS §0.1 接入要点 · L30 · 226
- ENG-GATES-SHOTS §1 审查视角 · L36 · 4.1k
  - ENG-GATES-SHOTS §1.1 命令行规则与截图默认串 · L40 · 726
  - ENG-GATES-SHOTS §1.2 截图方式 · L55 · 501
  - ENG-GATES-SHOTS §1.3 固定机位表与审查视角表 · L67 · 1.8k
  - ENG-GATES-SHOTS §1.4 路线与点位（主线之后） · L99 · 660
  - ENG-GATES-SHOTS §1.5 时季抽样（主线之后） · L108 · 365
- ENG-GATES-SHOTS §2 待验证（P0） · L117 · 144

## ENG-GATES-VETO · 一票否决与收尾清单

`docs/engineering/ENG-GATES-VETO-一票否决与收尾清单.md` · 2.9k 字 · 23 条一票否决（任一条即不通过；系统类条目指向各系统文档）与收尾阶段主代理逐项检查的全局清单
关键词：一票否决、全局清单、收尾清单、死黑、季节残留、硬切、切图残留、丢档、声音否决、观光规则集判据

- ENG-GATES-VETO §0 结论 · L22 · 346
  - ★ ENG-GATES-VETO §0.1 接入要点 · L28 · 85
- ENG-GATES-VETO §1 一票否决 · L32 · 1.4k
- ENG-GATES-VETO §2 全局清单 · L60 · 532

## ENG-GATES · 验收：测试三档与门禁

`docs/engineering/ENG-GATES-验收与门禁.md` · 5.0k 字 · 测试三档与何时跑（日常只有编译、受影响的测试、G4）、14 项门禁各验什么、入口与交付分工；审查视角与截图、一票否决与收尾清单在子文档
关键词：门禁、验收、测试三档、快检、核心专项、集中测试、gate.ps1、门禁表、门禁编号、D-238、入口与交付分工、测试只锁本包输出、构建锁

- ENG-GATES §0 结论 · L21 · 808
  - ★ ENG-GATES §0.1 接入要点 · L34 · 255
- ENG-GATES §1 门禁与测试节奏 · L40 · 2.9k
  - ENG-GATES §1.1 测试三档 · L44 · 1.2k
  - ENG-GATES §1.2 门禁表 · L65 · 1.4k
  - ENG-GATES §1.4 入口与交付分工 · L86 · 207
- ENG-GATES §2 待验证（P0） · L92 · 152

## ENG-PERF-ADAPT · 设备档、自适应与测量

`docs/engineering/ENG-PERF-ADAPT-设备档与自适应.md` · 3.0k 字 · 设备档与首次分档、运行时自适应的降级阶梯与控制律、帧节奏、测量与基准的做法、D3D12 与 Vulkan 的选择
关键词：自动调节画质、降级阶梯、控制律、剔除样本、帧节奏、吞吐基准、体验基准、掉帧、卡顿、GPU 计时、D3D12、Vulkan

- ENG-PERF-ADAPT §0 结论 · L22 · 426
  - ★ ENG-PERF-ADAPT §0.1 接入要点 · L29 · 160
- ENG-PERF-ADAPT §1 设备档、画面预设与自适应 · L34 · 944
  - ENG-PERF-ADAPT §1.1 设备档与首次分档（主线之后） · L36 · 194
  - ENG-PERF-ADAPT §1.2 运行时自适应（主线之后） · L40 · 410
  - ENG-PERF-ADAPT §1.3 帧节奏与画面设置约束 · L48 · 321
- ENG-PERF-ADAPT §2 测量、剖析与门禁 · L53 · 678
- ENG-PERF-ADAPT §3 D3D12 与 Vulkan 的选择 · L61 · 213
- ENG-PERF-ADAPT §4 待验证（P0） · L65 · 58

## ENG-PERF-LIGHT · 植被、阴影、后期与灯光

`docs/engineering/ENG-PERF-LIGHT-植被阴影与后期.md` · 4.1k 字 · 植被分级与风、飘落池、投影代理；阴影量化与日月交接的做法；后期链顺序、描线、抗锯齿；窗灯路灯通道与倒影
关键词：植被分级、树冠云、风三层、ShadowsOnly 代理、投影物、方向量化、太阳贴地、退光线、日月交接、后期链、Compositor、描线材质 ID、CDL、LUT、灯光通道、路灯光斑、倒影视口

- ENG-PERF-LIGHT §0 结论 · L22 · 546
  - ★ ENG-PERF-LIGHT §0.1 接入要点 · L29 · 214
- ENG-PERF-LIGHT §1 植被 · L34 · 705
- ENG-PERF-LIGHT §2 阴影、光照与后期 · L43 · 1.9k
  - ENG-PERF-LIGHT §2.1 阴影 · L45 · 657
  - ENG-PERF-LIGHT §2.2 后期链 · L56 · 662
  - ENG-PERF-LIGHT §2.3 窗灯与路灯 · L65 · 358
  - ENG-PERF-LIGHT §2.4 倒影 · L71 · 204
- ENG-PERF-LIGHT §3 待验证（P0） · L75 · 140

## ENG-PERF-STREAM · 流式加载、本机烘焙与内存

`docs/engineering/ENG-PERF-STREAM-流式加载与烘焙.md` · 3.7k 字 · 本机烘焙与缓存、帧内请求与六条不变量、碰撞级与室内包、目的地预取的做法，主线程与 C#，内存显存，安装包布局
关键词：TileBundle、流式加载、本机烘焙缓存、tbin、六条不变量、看门狗、碰撞级、室内包、目的地预取、零分配、GC、显存记账、安装包布局、不泄漏

- ENG-PERF-STREAM §0 结论 · L22 · 610
  - ★ ENG-PERF-STREAM §0.1 接入要点 · L30 · 216
- ENG-PERF-STREAM §1 流式加载 · L36 · 1.4k
  - ENG-PERF-STREAM §1.1 本机烘焙、缓存与加载 · L38 · 615
  - ENG-PERF-STREAM §1.2 帧内请求、挂载与六条不变量 · L46 · 474
  - ENG-PERF-STREAM §1.3 碰撞级与室内包 · L59 · 168
  - ENG-PERF-STREAM §1.4 外围与目的地预取 · L63 · 179
- ENG-PERF-STREAM §2 主线程与 C# · L67 · 319
- ENG-PERF-STREAM §3 内存与显存 · L73 · 264
- ENG-PERF-STREAM §4 安装包与磁盘布局 · L77 · 155
- ENG-PERF-STREAM §5 待验证（P0） · L81 · 79

## ENG-PERF · 性能工程：总原则、绘制管线与剔除

`docs/engineering/ENG-PERF-性能工程.md` · 5.5k 字 · 性能策略总原则与绘制管线：容器与合批、着色器白名单与全局参数、预热、填充率、剔除与渲染图层
关键词：着色器白名单、全局着色器参数、ShaderGlobals、数据纹理、合批、自动实例化、MultiMesh、容器选型、管线预热、抖动淡化、级别过渡、填充率、遮挡剔除、渲染图层、顶点格式、D3D12

- ENG-PERF §0 总原则 · L21 · 1.1k
  - ★ ENG-PERF §0.1 接入要点 · L36 · 410
- ENG-PERF §1 绘制管线与合批 · L45 · 1.8k
  - ENG-PERF §1.1 容器选型 · L47 · 344
  - ENG-PERF §1.2 MultiMesh、网格 LOD 与级别过渡 · L54 · 389
  - ENG-PERF §1.3 调用口径 · L60 · 123
  - ENG-PERF §1.4 着色器白名单 · L64 · 430
  - ENG-PERF §1.5 全局参数与数据纹理 · L73 · 452
- ENG-PERF §2 着色器与编译 · L82 · 607
- ENG-PERF §3 填充率与带宽 · L90 · 201
- ENG-PERF §4 剔除 · L96 · 494
- ENG-PERF §5 待验证（P0） · L102 · 166

## ENG-ROADMAP · 扩展路线：从观光到 RPG

`docs/engineering/ENG-ROADMAP-扩展路线.md` · 4.2k 字 · 规定观光版给以后 RPG 留的扩展原则、现在不能写死的地方和 v1 冻结前的最小预留（6 条）
关键词：扩展路线、从观光到 RPG、扩展原则、规则集、sightseeing、观光规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位、RPG 预留目录、实体 ID 前缀、存档预留段名、v1 冻结前的预留、写法纪律

- ENG-ROADMAP §0 结论 · L21 · 934
  - ★ ENG-ROADMAP §0.1 接入要点 · L30 · 530
- ENG-ROADMAP §1 扩展原则 · L40 · 588
- ENG-ROADMAP §2 写死的规定与写法纪律 · L51 · 497
- ENG-ROADMAP §3 v1 冻结前的预留 · L57 · 1.3k
  - ENG-ROADMAP §3.1 清单 · L59 · 1.1k
  - ENG-ROADMAP §3.2 不需要现在预留的 · L70 · 144

## ENG-WP-LIST · 工作包一览与子包拆法

`docs/engineering/ENG-WP-LIST-工作包一览与子包拆法.md` · 11.4k 字 · 每个工作包一行的包表（规格文档、依赖、阶段、主线版），三个插入项，闸口之后的细节待办与候选包，子包拆法
关键词：工作包一览、包表、规格文档列、主线版列、三个插入项、细节阶段待办、候选包、拆包、子包、子包拆法、备选拆法

- ENG-WP-LIST §0 结论 · L23 · 380
  - ★ ENG-WP-LIST §0.1 接入要点 · L30 · 145
- ENG-WP-LIST §1 工作包一览 · L35 · 8.9k
  - ENG-WP-LIST §1.1 三个插入项 · L103 · 455
  - ENG-WP-LIST §1.2 细节阶段待办 · L109 · 1.7k
  - ENG-WP-LIST §1.3 候选包 · L135 · 246
- ENG-WP-LIST §2 子包拆法 · L143 · 1.4k

## ENG-WP · 工作包派工单：56 个工作包，可并行 55 个

`docs/engineering/ENG-WP-工作包派工单.md` · 6.2k 字 · 56 个工作包的划分、开发阶段与闸口、开工波次与推送顺序，主代理的工程维护事项；包表与子包拆法在 ENG-WP-LIST
关键词：工作包、派工单、派工、开工波次、推送顺序、依赖、主线版、主线可见结果、后置细节、阶段与闸口、主线闸口、D0、阶段名 P0–P3、主代理工程维护、开放室内清单审批、插件清单、ownership.json、third_party.json、目标框架

- ENG-WP §0 结论 · L24 · 994
  - ★ ENG-WP §0.1 接入要点 · L37 · 293
- ENG-WP §1 阶段与闸口 · L44 · 2.4k
  - ENG-WP §1.1 开工波次与推送顺序 · L66 · 1.0k
- ENG-WP §2 主代理的工程维护事项 · L82 · 1.1k
- ENG-WP §3 待验证（P0） · L93 · 425

## TOOL-PREVIEW · 内容预览

`docs/tools/TOOL-PREVIEW-内容预览.md` · 3.3k 字 · 开发构建里预览与回看内容：按实体定位、保留机位与时季天气、复制机位、重读数据
关键词：内容预览、预览、回看、开发构建、--focus、--pose-file、按实体定位、实体 ID、机位、复制机位、机位文件、last-pose.json、F5 重读数据、F6 复制机位、F7 重启、重启后恢复机位、预览只读不写

- TOOL-PREVIEW §0 结论 · L21 · 936
  - ★ TOOL-PREVIEW §0.1 接入要点 · L28 · 630
- TOOL-PREVIEW §1 启动 · L39 · 351
- TOOL-PREVIEW §2 按实体 ID 定位 · L47 · 269
- TOOL-PREVIEW §3 记下机位 · L53 · 257
- TOOL-PREVIEW §4 改了以后回看 · L59 · 645

## OPS-RELEASE · 发布与交付

`docs/ops/OPS-RELEASE-发布与交付.md` · 6.7k 字 · 版本号与构建信息、正式用户目录、导出与发布、安装更新卸载、地图数据包、故障诊断、许可、署名与图标
关键词：发布、交付、版本号、version.txt、构建信息、导出、导出入口、scripts/release、分发压缩包、UESTC-<版本>-win64.7z、地图数据包、map-data-<版本>.zip、ODbL、正式用户目录、%APPDATA%\UESTC、安装、更新、卸载、诊断包、导出诊断、许可、MIT、LICENSE、干净环境验证、OSM 署名、许可页、制作人员与许可、窗口标题、窗口图标、八角晶片

- OPS-RELEASE §0 结论 · L21 · 1.2k
  - ★ OPS-RELEASE §0.1 接入要点 · L31 · 750
- OPS-RELEASE §1 版本号与构建信息 · L44 · 268
- OPS-RELEASE §2 用户数据目录 · L51 · 273
- OPS-RELEASE §3 导出与发布 · L57 · 584
- OPS-RELEASE §4 安装、更新与卸载 · L66 · 504
- OPS-RELEASE §5 地图数据包（ODbL） · L78 · 506
- OPS-RELEASE §6 故障处理与诊断 · L86 · 547
- OPS-RELEASE §7 许可 · L93 · 1.7k
  - OPS-RELEASE §7.1 署名、许可页、窗口标题与图标 · L108 · 801
