# 小节目录

> 由 `scripts/docs/gen_index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（WF-ONBOARD §3.1）。行号以生成时为准，原文改过以后可能差几行，以标题文字为准。

## CORE-INDEX · 《电光年华》开发总纲

`docs/README.md` · 7.1k 字 · 全项目唯一的顶层：核心准则（钩子注入）、按功能分域的文档地图、作品定位与当前范围、全局不变量 INV-1–INV-12
关键词：总纲、核心准则、文档地图、怎么查文档、作品定位、电光年华、当前范围、主线、全局不变量、INV、清水河、实现优先、不过度工程化、测试最小、改设计必清旧、直接改主线、一份权威、文档精简、落金初灯

- CORE-INDEX §0 核心准则 · L21 · 1.5k
  - ★ CORE-INDEX §0.1 接入要点 · L46 · 130
- CORE-INDEX §1 文档地图 · L52 · 1.9k
- CORE-INDEX §2 作品与范围 · L85 · 965
- CORE-INDEX §3 全局不变量 · L95 · 2.0k

## CORE-CONVENTIONS · 约定

`docs/core/CORE-CONVENTIONS-约定.md` · 8.5k 字 · 跨文档共同约定：坐标与单位、方位角、时间写法、编号与命名、实体 ID 规则、确定性原则
关键词：约定、坐标系、单位、角度、方位角、时间写法、编号体系、命名、实体 ID、色名、枚举纪律、持久化 ID、确定性

- CORE-CONVENTIONS §0 结论 · L22 · 807
  - ★ CORE-CONVENTIONS §0.1 接入要点 · L32 · 424
- CORE-CONVENTIONS §1 坐标与单位 · L42 · 1.4k
  - CORE-CONVENTIONS §1.1 坐标系 · L44 · 510
  - CORE-CONVENTIONS §1.2 角度 · L52 · 350
  - CORE-CONVENTIONS §1.3 清水河的范围数 · L60 · 278
  - CORE-CONVENTIONS §1.4 其他单位 · L68 · 250
- CORE-CONVENTIONS §2 时间写法 · L76 · 826
- CORE-CONVENTIONS §3 标识与命名 · L94 · 4.2k
  - CORE-CONVENTIONS §3.1 地图、实体与覆盖层 · L96 · 813
  - CORE-CONVENTIONS §3.2 编号体系 · L111 · 793
  - CORE-CONVENTIONS §3.3 同号不同义的读法 · L133 · 570
  - CORE-CONVENTIONS §3.4 命名写法 · L145 · 676
  - CORE-CONVENTIONS §3.5 色名 · L155 · 364
  - CORE-CONVENTIONS §3.6 枚举与扩展纪律 · L162 · 555
  - CORE-CONVENTIONS §3.7 持久化 ID 的稳定 · L173 · 414
- CORE-CONVENTIONS §4 确定性 · L182 · 281
- CORE-CONVENTIONS §5 路径 · L190 · 251

## CORE-DECISIONS · 决策登记

`docs/core/CORE-DECISIONS-决策登记.md` · 29.0k 字 · 全项目已定决策的登记：每条一句结论，附适用范围、细则所在和状态
关键词：决策、已定、D 编号、决策登记、生效、待原型验证、细则所在、推翻决策、ADR

- CORE-DECISIONS §0 用法 · L23 · 407
  - ★ CORE-DECISIONS §0.1 接入要点 · L31 · 175
- CORE-DECISIONS §1 作品定位与范围 · L37 · 1.7k
- CORE-DECISIONS §2 世界与地图 · L54 · 3.4k
- CORE-DECISIONS §3 时间、季节、天气 · L85 · 2.4k
- CORE-DECISIONS §4 美术风格 · L110 · 2.2k
- CORE-DECISIONS §5 界面 · L131 · 2.8k
- CORE-DECISIONS §6 存档 · L160 · 1.1k
- CORE-DECISIONS §7 音频 · L175 · 2.0k
- CORE-DECISIONS §8 性能与画面设置 · L197 · 1.6k
- CORE-DECISIONS §9 工程 · L217 · 2.8k
- CORE-DECISIONS §10 流程 · L242 · 7.8k

## CORE-GLOSSARY · 术语表

`docs/core/CORE-GLOSSARY-术语表.md` · 19.7k 字 · 文档里专有说法的统一解释，按领域分组，每条一两句并给出细则所在的小节
关键词：术语、术语表、名词解释、同名异义、缩写、别名

- CORE-GLOSSARY §0 用法 · L22 · 261
  - ★ CORE-GLOSSARY §0.1 接入要点 · L28 · 92
- CORE-GLOSSARY §1 作品与默认状态 · L33 · 1.3k
- CORE-GLOSSARY §2 世界状态与时间 · L54 · 1.2k
- CORE-GLOSSARY §3 天气 · L77 · 1.2k
- CORE-GLOSSARY §4 美术与光照 · L99 · 1.1k
- CORE-GLOSSARY §5 箱庭、地形、边界与外围 · L121 · 1.4k
- CORE-GLOSSARY §6 室内与门 · L146 · 852
- CORE-GLOSSARY §7 观察模式、行走与跳转 · L162 · 1.3k
- CORE-GLOSSARY §8 平面地图与传送 · L187 · 859
- CORE-GLOSSARY §9 开始页、过场与存档 · L204 · 1.3k
- CORE-GLOSSARY §10 多地图 · L228 · 611
- CORE-GLOSSARY §11 界面 · L240 · 1.2k
- CORE-GLOSSARY §12 音频 · L267 · 1.0k
- CORE-GLOSSARY §13 画面设置与性能 · L288 · 1.6k
- CORE-GLOSSARY §14 工程与流程 · L315 · 2.3k
- CORE-GLOSSARY §15 同名异义 · L353 · 1.5k

## WF-BUILD · 施工边界

`docs/workflow/WF-BUILD-施工边界.md` · 2.2k 字 · 施工时能改哪里、共享文件与契约申请、新模块怎么接入、Godot API 先查签名，以及禁用写法门禁拦不住的两条约定
关键词：独占目录、共享文件、契约申请、特性接入、穷举 switch、几何不随时季天气变、GDScript、uid、signatures.txt、Godot API

- WF-BUILD §0 结论 · L23 · 652
  - ★ WF-BUILD §0.1 接入要点 · L30 · 381
- WF-BUILD §1 改哪里 · L37 · 621
- WF-BUILD §2 G4 拦不住的约定 · L45 · 170

## WF-COMMS · 交接与通信

`docs/workflow/WF-COMMS-交接与通信.md` · 5.4k 字 · .collab 交接目录与文件布局，命名与写法，任务单与请求文件的格式，契约申请短头部，完成回报格式，来信与消息，Codex 会话与引导信箱
关键词：.collab、交接目录、文件布局、archive、logs、结案即挪走、.ignore、任务单、施工单、旁路单、调研目录、契约申请、评审请求、六行短头部、完成回报、回报格式、截图命名、文件命名、来信、收件箱、notes、一条一个文件、【需定】【待审】【知会】、Codex 会话、codex queue、引导信箱、steer、HANDOVER.md、D-207、D-208

- WF-COMMS §0 结论 · L23 · 535
  - ★ WF-COMMS §0.1 接入要点 · L31 · 185
- WF-COMMS §1 交接目录与文件 · L38 · 1.4k
- WF-COMMS §2 命名与写法 · L63 · 427
- WF-COMMS §3 任务单、请求文件、契约申请与完成回报 · L70 · 876
  - WF-COMMS §3.1 施工单与旁路单 · L72 · 158
  - WF-COMMS §3.2 请求文件 · L77 · 50
  - WF-COMMS §3.3 契约申请的短头部（D-201） · L81 · 168
  - WF-COMMS §3.4 完成回报 · L91 · 475
- WF-COMMS §4 来信与消息 · L109 · 788
  - WF-COMMS §4.1 什么时候发 · L111 · 249
  - WF-COMMS §4.2 一条来信一个新文件（D-207） · L120 · 221
  - WF-COMMS §4.3 三级标记与写法 · L124 · 306
- WF-COMMS §5 Codex 会话与引导信箱 · L131 · 452

## WF-DOC · 文档写法

`docs/workflow/WF-DOC-文档写法.md` · 7.5k 字 · 文档网的五层、每份文档的形状、编码与引用、只写什么不写什么、体量上限、冲突层级、新增与维护
关键词：文档写法、文档网、五层、渐进式披露、核心准则、文档地图、接入要点、文件头、front matter、范围块、查阅行、编码、文件名、小节编号、引用写法、全局替换、只写现行规则、不写实现、一份权威、体量上限、字数上限、待验证、冲突层级、规则放哪份、新增文档、登记决策、gen_index、check_docs、SECTIONS.md、D-178、D-200、D-208、D-215、D-216、D-217

- WF-DOC §0 结论 · L22 · 719
  - ★ WF-DOC §0.1 接入要点 · L31 · 235
- WF-DOC §1 五层 · L38 · 583
- WF-DOC §2 文档形状 · L51 · 857
- WF-DOC §3 编码、小节编号与引用 · L76 · 850
- WF-DOC §4 写什么，怎么写 · L94 · 929
  - WF-DOC §4.1 取舍 · L96 · 524
  - WF-DOC §4.2 写法（D-220） · L105 · 391
- WF-DOC §5 体量上限 · L116 · 1.1k
  - WF-DOC §5.1 长大了怎么拆（D-222） · L133 · 374
  - WF-DOC §5.2 新内容放哪 · L140 · 239
- WF-DOC §6 冲突与规则放哪 · L147 · 784
- WF-DOC §7 新增文档 · L177 · 243
- WF-DOC §8 登记新决策 · L183 · 174
- WF-DOC §9 维护 · L189 · 368

## WF-ENTRY · 角色入口卡

`docs/workflow/WF-ENTRY-角色入口卡.md` · 4.5k 字 · 每个角色的入口卡：身份与回报对象、开工动作、本角色独有的必守、做某事前读哪一节；由钩子按节标题注入
关键词：角色入口卡、入口卡、主会话、子代理、Astra、Sol 会话、重置后先读、开工动作、收尾、做 X 前读 Y、钩子注入、SessionStart、SubagentStart、引擎路径、UESTC_GODOT

- WF-ENTRY §0 结论 · L21 · 381
  - ★ WF-ENTRY §0.1 接入要点 · L27 · 127
- WF-ENTRY §1 主会话 · L33 · 1.1k
- WF-ENTRY §2 子代理 · L57 · 997
- WF-ENTRY §3 Astra · L84 · 819
- WF-ENTRY §4 Sol 会话 · L107 · 574

## WF-FLOW · 开发流程

`docs/workflow/WF-FLOW-开发流程.md` · 6.4k 字 · 一个包从派工、施工、自测推送、回报到评审的流程，防打架，按工作量拆单，可调参数集中放，契约申请与共享工程文件，调研入队流水线
关键词：开发流程、派工、施工单、按工作量拆单、开工 30 分钟重估、回报、评审、防打架、邻居、可调参数、契约申请、共享工程文件、契约冻结、调研入队流水线、审计、待派工队列、调研简报、旁路单、D-184、D-134、D-211、评审通用结论

- WF-FLOW §0 结论 · L22 · 596
  - ★ WF-FLOW §0.1 接入要点 · L31 · 227
- WF-FLOW §1 全流程一览 · L38 · 517
- WF-FLOW §2 派工与拆单 · L49 · 1.4k
  - WF-FLOW §2.1 防打架（D-211） · L65 · 361
- WF-FLOW §3 施工与回报 · L74 · 792
  - WF-FLOW §3.1 统一编译（D-219） · L84 · 387
- WF-FLOW §4 评审：审美与契约的交叉评审 · L92 · 655
- WF-FLOW §5 契约申请与共享工程文件 · L105 · 399
- WF-FLOW §6 调研入队流水线 · L113 · 1.3k
  - WF-FLOW §6.1 调研与旁路单 · L127 · 584

## WF-MODEL · 模型选择与子代理

`docs/workflow/WF-MODEL-模型选择与子代理.md` · 5.7k 字 · 按七问与难度选施工模型和推理强度，Opus 与 Sonnet 的分界，机械活交谁，怎么开子代理，子代理生命周期，简报模板与接续段
关键词：七问、选模型、Opus 与 Sonnet 的分界、Sonnet 优先、机械活、gpt-6-luna 子代理、推理强度、升级先找原因、派单最少写、简单少量的活直接做、子代理、Agent 工具、spawn_agent、共用工作目录、子代理生命周期、默认新开、恢复还是新开、简报模板、接续段、D-184、D-191、D-193

- WF-MODEL §0 结论 · L22 · 547
  - ★ WF-MODEL §0.1 接入要点 · L31 · 170
- WF-MODEL §1 按七问选施工模型 · L37 · 747
- WF-MODEL §2 Opus 与 Sonnet 的分界（D-193） · L51 · 426
- WF-MODEL §3 机械活交谁 · L64 · 542
- WF-MODEL §4 推理强度按难度选 · L73 · 533
- WF-MODEL §5 升级、派单与直接做 · L87 · 417
- WF-MODEL §6 怎么开子代理 · L94 · 512
- WF-MODEL §7 子代理的生命周期：默认新开，靠任务单交接 · L102 · 447
- WF-MODEL §8 简报模板与"接续"段 · L112 · 775
  - WF-MODEL §8.1 简报模板 · L114 · 479
  - WF-MODEL §8.2 "接续"段 · L131 · 279

## WF-QUOTA · 账号与额度

`docs/workflow/WF-QUOTA-账号与额度.md` · 3.3k 字 · 额度口径，Claude 侧按 Opus 当量的并发公式与记账，节奏档，Codex 账号与速度，额度见底时冻结
关键词：额度、账号、Max 20x、5 小时窗口、每周额度、并发公式、Opus 当量、推理档系数、余量封顶、记账、get_usage、节奏档、非紧急段、紧急段、Codex 额度、普通速度、额度见底、冻结、切换账号、D-196、D-197

- WF-QUOTA §0 结论 · L22 · 416
  - ★ WF-QUOTA §0.1 接入要点 · L30 · 107
- WF-QUOTA §1 口径 · L35 · 77
- WF-QUOTA §2 Claude 侧的并发与额度 · L39 · 1.2k
- WF-QUOTA §3 节奏档：非紧急段与紧急段（D-196） · L55 · 548
- WF-QUOTA §4 Codex 账号（Astra 与 Sol） · L62 · 166
- WF-QUOTA §5 额度见底时冻结 · L67 · 220

## WF-ROLE · 参与者与分工

`docs/workflow/WF-ROLE-参与者与分工.md` · 4.6k 字 · 角色表与汇报关系，会话的运行方式，各模型的长短板，GPT 模型不做审美，决定权与事归谁，总指挥只做什么，Astra 当指挥
关键词：角色表、汇报给、决定权、执行人、会话、运行方式、单 Claude 账户、模型能力、长短板、Opus、Sonnet、gpt-6-astra、gpt-6.1-sol、GPT 不做审美、视觉风格归属、事归谁、交叉、总指挥、主代理、风格负责人、集中测试、Astra、工程指挥、元规则、D-179

- WF-ROLE §0 结论 · L22 · 507
  - ★ WF-ROLE §0.1 接入要点 · L29 · 213
- WF-ROLE §1 角色与运行方式 · L35 · 716
- WF-ROLE §2 模型的长处与短板 · L50 · 471
- WF-ROLE §3 GPT 模型不做审美工作 · L59 · 359
- WF-ROLE §4 决定权与事归谁 · L66 · 619
- WF-ROLE §5 总指挥只做什么 · L77 · 557
- WF-ROLE §6 Astra 当指挥，施工默认交 gpt-6.1-sol 子代理 · L89 · 692

## WF-RULE · 全员守则

`docs/workflow/WF-RULE-全员守则.md` · 5.8k 字 · 所有会话和子代理都要守的规则：不过度工程化、不向作者出选项、不预判时间、不干等、文件操作、git 与直接改主线、主线优先、测试最小、少和别人打架、写文档与记忆
关键词：全员守则、不过度工程化、不向作者出选项、不预判时间、不短轮询、不干等、不重复复核、文件操作、删除规则、git、直接改主线、只提交自己的文件、回滚只用 revert、不 stash、主线优先、测试最小、少和别人打架、一次迁完、拿不准就问、写文档、写记忆、D-201、D-168、D-167、D-199、D-209、D-211

- WF-RULE §0 结论 · L22 · 216
  - ★ WF-RULE §0.1 接入要点 · L26 · 67
- WF-RULE §1 不过度工程化（D-201） · L30 · 308
- WF-RULE §2 不向作者出选项 · L37 · 242
- WF-RULE §3 不预判时间 · L43 · 190
- WF-RULE §4 不短轮询、不干等、不重复复核 · L49 · 290
- WF-RULE §5 来信 · L55 · 74
- WF-RULE §6 文件操作 · L59 · 523
- WF-RULE §7 git 与直接改主线（D-211） · L69 · 734
- WF-RULE §8 主线优先（D-168） · L79 · 292
- WF-RULE §9 测试最小（D-167） · L86 · 366
- WF-RULE §10 少和别人打架（D-199） · L93 · 579
- WF-RULE §11 拿不准就问 · L103 · 137
- WF-RULE §12 写文档与记忆 · L108 · 247
- WF-RULE §13 一次性产物（D-221） · L116 · 457
- WF-RULE §14 按严重程度修 bug（D-223） · L123 · 351

## WF-STATUS · 施工状态与队列

`docs/workflow/WF-STATUS-施工状态与队列.md` · 2.6k 字 · 施工状态文件怎么填、待派工队列与队列平衡、交接单
关键词：施工状态、状态文件、当前 main、最近可运行入口、已冻结的契约组、活跃工作包、待派工队列、Q-nnn、排队、可派、已派、队列平衡、积压、交接单、HANDOVER.md、接手、已集成

- WF-STATUS §0 结论 · L23 · 452
  - ★ WF-STATUS §0.1 接入要点 · L31 · 183
- WF-STATUS §1 施工状态文件怎么填 · L37 · 795
- WF-STATUS §2 待派工队列与队列平衡 · L58 · 732
- WF-STATUS §3 交接单 · L74 · 87

## ART-AMBIENCE · 日式日常街景：元素、分区与做法

`docs/art/ART-AMBIENCE-日式日常街景.md` · 9.9k 字 · 日式日常街景元素的边界与分区，各元素的形、色、限定与三渲二做法，以及对应的鸟与声音
关键词：日式街景、日式氛围、日系、日本味、电线杆、架空线、自动售货机、售货机光岛、弯道凸面镜、驻轮场式车棚、屋顶围网、高架水箱、通长雨庇、推拉铝窗、象牙色窗帘、开放外廊、次路标线、让行牌、L 形边沟、砌块墙、樱花行道树、绣球、夏季绿帘、杆上防犯灯、便利店夜景、暖帘、防球网、外墙圆钟、电车道口

- ART-AMBIENCE §0 结论 · L21 · 1.1k
  - ★ ART-AMBIENCE §0.1 接入要点 · L30 · 631
- ART-AMBIENCE §1 分区与通用约束 · L43 · 1.6k
  - ART-AMBIENCE §1.1 用与不用 · L45 · 169
  - ART-AMBIENCE §1.2 区域代号 · L50 · 461
  - ART-AMBIENCE §1.3 方式与通用约束 · L63 · 623
  - ART-AMBIENCE §1.4 颜色 · L76 · 339
- ART-AMBIENCE §2 校舍外立面与屋顶 · L87 · 953
- ART-AMBIENCE §3 外廊、楼梯与出入口 · L98 · 605
- ART-AMBIENCE §4 车棚、售货机与街角小设施 · L107 · 565
- ART-AMBIENCE §5 电线杆、架空线与线上的鸟 · L115 · 486
- ART-AMBIENCE §6 道路标线、凸面镜、护栏、路缘与排水 · L121 · 828
- ART-AMBIENCE §7 绿化 · L135 · 382
- ART-AMBIENCE §8 照明与夜景 · L143 · 453
- ART-AMBIENCE §9 生活痕迹 · L152 · 45
- ART-AMBIENCE §10 河堤、球场设施、店头、外围街道与飞机云 · L156 · 703
- ART-AMBIENCE §11 声音 · L170 · 358
- ART-AMBIENCE §12 先做的 10 项 · L182 · 405
- ART-AMBIENCE §13 待验证 · L199 · 268

## ART-FIDELITY · 视觉精度标准：处处不粗糙，细节按区分级

`docs/art/ART-FIDELITY-视觉精度标准.md` · 12.0k 字 · 处处成立的精度底线 B1–B13、五个精度区的细节密度与判定距离、一票否决、巡检点、行走走廊加密与外围分级
关键词：视觉精度、精度区、K1–K5、判定距离、底线清单、B1–B13、不粗糙、细节密度、一票否决、贴脸距离、L0+ 专用几何、热点簇、事件贴花、倒角表、不做旧、立面密度、可读规则、巡检点、行走走廊

- ART-FIDELITY §0 结论 · L21 · 1.1k
  - ★ ART-FIDELITY §0.1 接入要点 · L33 · 499
- ART-FIDELITY §1 精度区与换算基准 · L45 · 2.1k
  - ART-FIDELITY §1.1 五个精度区 · L47 · 735
  - ART-FIDELITY §1.2 区界规则 · L59 · 187
  - ART-FIDELITY §1.3 细节项在各区的取用 · L66 · 470
  - ART-FIDELITY §1.4 划分与数据 · L78 · 256
  - ART-FIDELITY §1.5 换算基准 · L83 · 400
- ART-FIDELITY §2 底线 · L89 · 1.1k
  - ART-FIDELITY §2.0 底线清单（B1–B13） · L93 · 1.1k
- ART-FIDELITY §3 细节密度（5–150 m） · L113 · 2.2k
  - ART-FIDELITY §3.1 每栋楼的构件层次 · L117 · 288
  - ART-FIDELITY §3.2 立面密度 · L123 · 301
  - ART-FIDELITY §3.3 窗 · L130 · 317
  - ART-FIDELITY §3.4 地面 · L137 · 338
  - ART-FIDELITY §3.5 倒角与不做旧 · L143 · 400
  - ART-FIDELITY §3.6 夜间与四季 · L149 · 216
  - ART-FIDELITY §3.7 天气 · L155 · 222
- ART-FIDELITY §4 贴脸距离（L0+，0.5–5 m） · L162 · 663
- ART-FIDELITY §5 镜头穿进楼体或钻入地下 · L177 · 353
- ART-FIDELITY §6 细节词汇（中国大学校园版） · L186 · 1.2k
- ART-FIDELITY §7 一票否决清单 · L209 · 856
- ART-FIDELITY §8 巡检点与人工评分 · L235 · 400
- ART-FIDELITY §9 行走走廊 · L241 · 751
  - ART-FIDELITY §9.1 范围与画面构成 · L243 · 162
  - ART-FIDELITY §9.3 走廊里的加密 · L248 · 216
  - ART-FIDELITY §9.4 细节优先级 · L259 · 362
- ART-FIDELITY §10 外围分级精度 · L270 · 226

## ART-MODELING · 风格化造型规则：比例、形体、部件与风格一致性

`docs/art/ART-MODELING-风格化造型规则.md` · 11.4k 字 · 建筑、植被、道具的风格化比例与形体，固有色约束，金样与部件库，每类对象的最低合格构件与法线规则
关键词：风格化造型、风格化比例、身份特征、比例表、重复上限、防摩尔纹、人体尺度锚点、树冠团块、少粗圆干净、固有色、彩度上限、金样、参考板、参数化部件库、IPartDef、最低合格构件、三道门槛、收边构件、倒角做法、自定义法线、硬边法线、解析法线、修边条、贴面几何、换级不跳变、共同口号

- ART-MODELING §0 结论 · L21 · 1.2k
  - ★ ART-MODELING §0.1 接入要点 · L34 · 586
- ART-MODELING §1 先定身份，再放开 · L47 · 369
- ART-MODELING §2 现代混凝土和石材楼的比例规则 · L53 · 807
- ART-MODELING §3 植被、水面与地面 · L74 · 651
- ART-MODELING §4 道具与小物件 · L84 · 625
- ART-MODELING §5 颜色 · L96 · 319
- ART-MODELING §6 让 20 多个建模者风格一致 · L103 · 1.2k
- ART-MODELING §7 最低合格构件与法线规则 · L122 · 4.9k
  - ART-MODELING §7.1 三道门槛 · L128 · 459
  - ART-MODELING §7.2 收边与倒角 · L137 · 553
  - ART-MODELING §7.3 轮廓、深度层与截面 · L146 · 443
  - ART-MODELING §7.4 模块与修边条 · L156 · 576
  - ART-MODELING §7.5 贴面几何与顶点数据 · L167 · 489
  - ART-MODELING §7.6 法线 · L175 · 530
  - ART-MODELING §7.7 换级不跳变 · L187 · 378
  - ART-MODELING §7.8 每类对象的最低合格构件 · L198 · 1.3k
- ART-MODELING §8 待验证 · L212 · 209

## ART-RENOVATION · 老楼翻新：全部翻新的规则

`docs/art/ART-RENOVATION-老楼翻新.md` · 10.5k 字 · 老楼位置、体量与身份特征不动，外观全部换成新楼标准：会脏处换构件、材质色额、删简留、各部位改法、氛围与成电辨认、验收
关键词：老楼翻新、翻新、翻新预设、翻新楼、全部翻新、旧痕、删简留、防盗网、窗套、窗楣、窗台滴水、勒脚、压顶檐口、管槽、管廊、护窗栏、伸出杆、颜色配额、陶红墙带、浅奶咖墙、斑驳、面砖、楼梯间窗带、叶影上墙、成电辨认、翻新对照楼

- ART-RENOVATION §0 结论 · L21 · 1.0k
  - ★ ART-RENOVATION §0.1 接入要点 · L31 · 584
- ART-RENOVATION §1 适用范围 · L42 · 662
  - ART-RENOVATION §1.1 哪些楼列入翻新 · L44 · 285
  - ART-RENOVATION §1.2 不动什么 · L50 · 151
  - ART-RENOVATION §1.3 能加多少 · L56 · 215
- ART-RENOVATION §2 总则 · L61 · 871
  - ART-RENOVATION §2.1 全部翻新，与新楼同标准 · L63 · 216
  - ART-RENOVATION §2.2 会脏的地方换成构件 · L69 · 383
  - ART-RENOVATION §2.3 生活感只靠干净的痕迹 · L82 · 263
- ART-RENOVATION §3 材质与配色 · L88 · 1.4k
  - ART-RENOVATION §3.1 材质族 · L90 · 274
  - ART-RENOVATION §3.2 每栋楼的颜色配额 · L101 · 128
  - ART-RENOVATION §3.3 取值 · L106 · 521
  - ART-RENOVATION §3.4 斑驳、笔触与顶点色 · L115 · 280
  - ART-RENOVATION §3.5 面砖 · L122 · 100
  - ART-RENOVATION §3.6 描线 · L126 · 124
- ART-RENOVATION §4 删、简、留 · L130 · 865
  - ART-RENOVATION §4.1 删：旧痕、杂物与防盗网 · L132 · 360
  - ART-RENOVATION §4.2 简：外机、管线、窗分格与标识 · L140 · 312
  - ART-RENOVATION §4.3 留：干净版的生活痕迹 · L148 · 181
- ART-RENOVATION §5 窗、檐口、楼梯间、屋顶与首层 · L152 · 1.3k
  - ART-RENOVATION §5.1 窗 · L154 · 376
  - ART-RENOVATION §5.2 檐口与女儿墙 · L163 · 170
  - ART-RENOVATION §5.3 楼梯间 · L170 · 270
  - ART-RENOVATION §5.4 屋顶 · L178 · 235
  - ART-RENOVATION §5.5 首层、入口与阳台 · L186 · 199
- ART-RENOVATION §6 氛围与成电辨认 · L193 · 1.1k
  - ART-RENOVATION §6.1 画法上的氛围手法 · L195 · 513
  - ART-RENOVATION §6.2 日式物件上楼 · L209 · 310
  - ART-RENOVATION §6.3 保住"认得出是成电" · L216 · 226
- ART-RENOVATION §7 示例：三种楼型 · L223 · 1.6k
  - ART-RENOVATION §7.1 老式宿舍楼 · L227 · 543
  - ART-RENOVATION §7.2 老教学楼 · L236 · 465
  - ART-RENOVATION §7.3 老食堂 · L245 · 482
- ART-RENOVATION §8 验收 · L254 · 326
- ART-RENOVATION §9 待验证 · L263 · 283

## ART-SKYLIGHT · 天空、光照与大气

`docs/art/ART-SKYLIGHT-天空光照与大气.md` · 12.0k 字 · 默认状态的光与太阳、主光与阴影、天光、受光退场、人造光、天空云星月、雾与大气透视、光效清单与冷暖对照
关键词：主光、日月交接、无影蓝调、阴影、月影、天光、天光方向、退光线、受光退场、天空、霾带、云、飞机云、星空、银河、城市天光、月亮外观、雾、大气透视、共用雾函数、边缘雾、雾幕、光柱、镜头光斑、彩虹、闪电、冷暖对照

- ART-SKYLIGHT §0 结论 · L21 · 1.3k
  - ★ ART-SKYLIGHT §0.1 接入要点 · L33 · 726
- ART-SKYLIGHT §1 默认状态的光 · L46 · 384
- ART-SKYLIGHT §2 太阳本体与主光色 · L54 · 384
- ART-SKYLIGHT §3 光照骨架 · L61 · 2.9k
  - ART-SKYLIGHT §3.1 主光 · L63 · 1.3k
  - ART-SKYLIGHT §3.2 天光（环境光） · L77 · 204
  - ART-SKYLIGHT §3.3 受光分布 · L81 · 456
  - ART-SKYLIGHT §3.4 受光退场的顺序 · L88 · 188
  - ART-SKYLIGHT §3.5 人造光 · L94 · 775
- ART-SKYLIGHT §4 天空 · L113 · 2.0k
- ART-SKYLIGHT §5 月亮 · L139 · 247
- ART-SKYLIGHT §6 雾与大气透视 · L147 · 1.3k
- ART-SKYLIGHT §7 光效清单 · L166 · 1.5k
- ART-SKYLIGHT §8 冷暖对照 · L185 · 577
- ART-SKYLIGHT §9 待验证 · L209 · 243

## ART-STYLE · 风格基础：三渲二日式动画背景规范

`docs/art/ART-STYLE-风格基础.md` · 12.7k 字 · 全局画风的目标与规则：参照、明暗分阶、影色、描线、高光与透光、贴图、后期、调色板用法、排除元素、场景文字与情绪
关键词：三渲二、清透干净、质感参照、卡通材质、toon.gdshaderinc、明暗分阶、接触影、影色推导、室内暖影、描线、材质 ID、倒角提亮、凸边亮线、边缘光、透光、湿面、柔光、风格底线、色名、退役色名、跳色、日本元素边界、场景文字、原创徽记、情绪弧线

- ART-STYLE §0 结论 · L21 · 1.1k
  - ★ ART-STYLE §0.1 接入要点 · L34 · 517
- ART-STYLE §1 风格目标与参照 · L45 · 639
  - ART-STYLE §1.1 质感参照场景 · L53 · 372
- ART-STYLE §2 明暗分阶 · L60 · 1.4k
  - ART-STYLE §2.1 阶数 · L62 · 469
  - ART-STYLE §2.2 法线展平 · L76 · 168
  - ART-STYLE §2.3 树冠法线 · L84 · 70
  - ART-STYLE §2.4 平涂与渐变 · L88 · 146
  - ART-STYLE §2.5 统一的卡通着色 · L94 · 281
  - ART-STYLE §2.6 倒角提亮与只改明暗的修正 · L101 · 293
- ART-STYLE §3 影子：明度为主，按材质定影色 · L111 · 1.2k
  - ART-STYLE §3.1 原则 · L113 · 267
  - ART-STYLE §3.2 推导规则 · L121 · 499
  - ART-STYLE §3.3 天光与地面反光 · L132 · 78
  - ART-STYLE §3.4 室内暖影 · L136 · 128
  - ART-STYLE §3.5 哪些材质有色调变化 · L141 · 134
  - ART-STYLE §3.6 夜间与阴雨 · L145 · 115
- ART-STYLE §4 描线 · L150 · 1.3k
  - ART-STYLE §4.1 描哪些线 · L154 · 258
  - ART-STYLE §4.2 线色 · L162 · 134
  - ART-STYLE §4.3 线宽 · L167 · 214
  - ART-STYLE §4.4 逐材质调线与轻重 · L171 · 94
  - ART-STYLE §4.5 后处理描边 · L176 · 189
  - ART-STYLE §4.6 折角线与低模 · L182 · 162
  - ART-STYLE §4.7 凸边亮线 · L188 · 186
- ART-STYLE §5 高光、边缘光与透光 · L194 · 744
  - ART-STYLE §5.1 玻璃 · L196 · 144
  - ART-STYLE §5.2 金属亮带 · L201 · 70
  - ART-STYLE §5.3 边缘光 · L205 · 179
  - ART-STYLE §5.5 湿面与积水 · L211 · 192
  - ART-STYLE §5.6 透光 · L217 · 143
- ART-STYLE §6 贴图与笔触 · L222 · 637
  - ART-STYLE §6.1 禁止照片纹理 · L224 · 46
  - ART-STYLE §6.2 斑驳、笔触与构造图案线 · L228 · 350
  - ART-STYLE §6.3 色数限额 · L235 · 122
  - ART-STYLE §6.5 不用法线贴图 · L240 · 107
- ART-STYLE §7 后期 · L244 · 972
  - ART-STYLE §7.1 柔光：白天只晕高光 · L250 · 119
  - ART-STYLE §7.2 色调映射 · L254 · 122
  - ART-STYLE §7.3 镜头光斑 · L259 · 101
  - ART-STYLE §7.4 移轴景深 · L263 · 48
  - ART-STYLE §7.5 窗灯光晕 · L267 · 71
  - ART-STYLE §7.6 特效形态与光柱 · L271 · 210
  - ART-STYLE §7.7 抗锯齿 · L276 · 81
  - ART-STYLE §7.8 暗角与分调 · L280 · 72
- ART-STYLE §8 调色板 · L284 · 1.5k
  - ART-STYLE §8.1 色名分类与退役色名 · L286 · 355
  - ART-STYLE §8.2 大面积色与跳色 · L293 · 384
  - ART-STYLE §8.3 固有色与推导 · L305 · 163
  - ART-STYLE §8.4 界面色 · L311 · 145
  - ART-STYLE §8.5 色名登记 · L316 · 366
  - ART-STYLE §8.6 天气色 · L323 · 126
- ART-STYLE §9 排除元素 · L328 · 1.0k
  - ART-STYLE §9.1 日本元素的边界 · L330 · 829
  - ART-STYLE §9.2 其他排除项 · L350 · 196
- ART-STYLE §10 材质速查 · L356 · 154
- ART-STYLE §11 场景内的文字与标识 · L360 · 329
- ART-STYLE §13 情绪弧线 · L368 · 443

## WLD-DIORAMA · 箱庭边界、截面与远景

`docs/world/WLD-DIORAMA-箱庭边界截面与远景.md` · 8.5k 字 · 箱庭外缘的地层截面、水柱截面、雾海与清水河银带、360° 远景和雪山，及其随时段、季节、天气的变化
关键词：箱庭边界、地层截面、剖面、截面上缘、地下水线、卵石层、河渠水柱截面、雾海、雾海顶面、雾天雾海上升、清水河银带、深夜雾海微光、远景、远景视差、色调层、坡向层、发光层、雪山、日照金山、雪线、雪山可见度、backdrop.json、diorama_section、diorama_far、Camera3D.Far

- WLD-DIORAMA §0 结论 · L21 · 1.6k
  - ★ WLD-DIORAMA §0.1 接入要点 · L34 · 677
- WLD-DIORAMA §1 截面线与校界 · L46 · 131
- WLD-DIORAMA §2 地层截面 · L50 · 1.3k
  - WLD-DIORAMA §2.1 截面的受光与阴影 · L68 · 294
- WLD-DIORAMA §3 河渠水柱截面 · L76 · 375
- WLD-DIORAMA §4 截面下缘与雾海 · L82 · 989
- WLD-DIORAMA §5 远景（360° 不留空白） · L93 · 1.7k
- WLD-DIORAMA §6 雪山 · L124 · 964
- WLD-DIORAMA §7 待验证（P0） · L135 · 305

## WLD-INTERIORS · 室内：开放与关闭、门、加载、生成器与碰撞

`docs/world/WLD-INTERIORS-室内.md` · 12.7k 字 · 室内的精选开放原则、门的区分与自动门、关着的楼、加载衔接、生成器原则、室内风格与楼梯碰撞
关键词：楼内、开放室内、开放清单、interiors.json、开放入口、自动门、此处暂不开放、封口、门厅卡、窗卡、简化室内、I0 兜底层、室内加载、看进去例外、平面内核、楼梯碰撞、SlopedWallCollider、关门换位、窗光斑、光柱、室内色名、生活痕迹、导航图、G19

- WLD-INTERIORS §0 结论 · L21 · 1.2k
  - ★ WLD-INTERIORS §0.1 接入要点 · L34 · 677
- WLD-INTERIORS §1 开放原则与共用约定 · L47 · 892
- WLD-INTERIORS §2 开放的屋顶与高处（3 处） · L60 · 694
- WLD-INTERIORS §3 门的区分：开与关 · L76 · 1.4k
  - WLD-INTERIORS §3.1 楼里的门：走近推开，闭门器关上 · L92 · 665
- WLD-INTERIORS §4 关着的楼：外壳、窗卡、门厅卡与简化室内 · L102 · 797
- WLD-INTERIORS §5 加载与进楼衔接 · L112 · 937
  - WLD-INTERIORS §5.1 整体加载、按层显示 · L114 · 243
  - WLD-INTERIORS §5.2 触发 · L120 · 274
  - WLD-INTERIORS §5.3 进楼与出楼，以及衔接保证（ENG-PERF §5.2 第 6 条） · L127 · 406
- WLD-INTERIORS §6 平面生成器与类型库 · L135 · 723
- WLD-INTERIORS §7 风格 · L144 · 1.3k
  - WLD-INTERIORS §7.1 明暗与暖影 · L148 · 216
  - WLD-INTERIORS §7.2 窗光斑与光（最重要的室内画面） · L152 · 411
  - WLD-INTERIORS §7.3 色名、识别要素与夜间 · L160 · 585
- WLD-INTERIORS §8 生活痕迹规则 · L166 · 600
- WLD-INTERIORS §9 楼梯、门洞与碰撞 · L175 · 909
- WLD-INTERIORS §10 待验证 · L193 · 306
- WLD-INTERIORS §11 导航图、路线与扩展 · L202 · 768

## WLD-LIFE · 动物、生活痕迹与街道家具

`docs/world/WLD-LIFE-动物生活痕迹与街道家具.md` · 10.8k 字 · 全校动物与反应动物、室外生活痕迹、停着的车与街道家具的设计，随时段、季节、天气的显隐与行走碰撞规则
关键词：动物、鸟、校园猫、鸽群、反应动物、黄昏乌鸦、季节性动物、生活痕迹、共享单车、电动车、晾晒、黑天鹅、雨天生活痕迹、小心地滑、停着的车、校车、街道家具、分类垃圾箱、路灯灯具、售货机、外卖柜、长椅、声音插座、家具碰撞、日夜两套、变体掩码、state_bits、show_ratio

- WLD-LIFE §0 结论 · L21 · 1.3k
  - ★ WLD-LIFE §0.1 接入要点 · L32 · 751
- WLD-LIFE §1 总则 · L45 · 439
- WLD-LIFE §2 动物 · L52 · 2.3k
  - WLD-LIFE §2.1 反应动物：猫、鸽群与水禽 · L78 · 870
- WLD-LIFE §3 生活痕迹 · L95 · 1.4k
- WLD-LIFE §4 停着的车 · L110 · 775
- WLD-LIFE §5 街道家具 · L120 · 1.5k
- WLD-LIFE §6 行走模式下的碰撞 · L133 · 644
- WLD-LIFE §7 显隐：随时段、季节、天气的做法 · L148 · 834
- WLD-LIFE §8 待验证（P0） · L162 · 316

## WLD-OUTSKIRTS · 边界与外围：大箱庭、外围环境带、阻挡与提示

`docs/world/WLD-OUTSKIRTS-边界与外围.md` · 13.3k 字 · 大箱庭三条线、外围 O0–O3 的内容与精度、保留删除新增、边界阻挡与提示
关键词：外围、校外、校界、边界、前沿线、截面线、大箱庭、外围环境带、O0–O3、最近可达距离、d_min、outskirts.json、校外暂未开放、前方暂未开放、行走阻挡、飞行阻挡、缓停、推回、缓冲带、软地板、可达空间、活动范围、edge_fog、边缘雾、看街长椅、河渠格栅

- WLD-OUTSKIRTS §0 结论 · L21 · 1.7k
  - ★ WLD-OUTSKIRTS §0.1 接入要点 · L31 · 867
- WLD-OUTSKIRTS §1 大箱庭：三条线与截面 · L43 · 1.5k
  - WLD-OUTSKIRTS §1.1 周边结构要点 · L45 · 265
  - WLD-OUTSKIRTS §1.2 三条线 · L49 · 308
  - WLD-OUTSKIRTS §1.3 截面线的位置规则 · L59 · 442
  - WLD-OUTSKIRTS §1.4 截面精度 · L65 · 419
- WLD-OUTSKIRTS §2 外围分层 O0–O3 · L73 · 1.4k
  - WLD-OUTSKIRTS §2.1 判定原则：最近可达距离 · L75 · 297
  - WLD-OUTSKIRTS §2.2 分层表 · L79 · 637
  - WLD-OUTSKIRTS §2.3 楼型与程序化布局 · L90 · 420
- WLD-OUTSKIRTS §3 灯光、季节与车流 · L97 · 1.2k
  - WLD-OUTSKIRTS §3.1 灯光（夜间） · L99 · 541
  - WLD-OUTSKIRTS §3.2 时间、季节、天气 · L116 · 412
  - WLD-OUTSKIRTS §3.3 不画车流 · L125 · 225
- WLD-OUTSKIRTS §4 保留、删除与新增 · L130 · 1.1k
  - WLD-OUTSKIRTS §4.1 身份地标（保留，让人认得出） · L134 · 382
  - WLD-OUTSKIRTS §4.2 删掉的 · L146 · 202
  - WLD-OUTSKIRTS §4.3 可以新增的氛围元素 · L150 · 409
- WLD-OUTSKIRTS §5 外围的画法：清透、细线 · L162 · 465
- WLD-OUTSKIRTS §6 行走阻挡 · L171 · 1.0k
  - WLD-OUTSKIRTS §6.1 挡什么（WP-30、WP-44） · L173 · 354
  - WLD-OUTSKIRTS §6.2 0.6 m 内的缓停（WP-42） · L183 · 270
  - WLD-OUTSKIRTS §6.3 河渠格栅 · L188 · 135
  - WLD-OUTSKIRTS §6.4 看街长椅（WP-30 做，WP-44 选点） · L192 · 187
  - WLD-OUTSKIRTS §6.5 兜底 · L196 · 58
- WLD-OUTSKIRTS §7 飞行阻挡（旁观模式） · L200 · 1.5k
  - WLD-OUTSKIRTS §7.1 可达空间 · L202 · 396
  - WLD-OUTSKIRTS §7.2 缓冲带与推回（WP-06） · L210 · 407
  - WLD-OUTSKIRTS §7.3 边缘雾 `edge_fog` · L219 · 389
  - WLD-OUTSKIRTS §7.4 参数、合法化与不受约束的镜头 · L226 · 337
- WLD-OUTSKIRTS §8 提示文案与多地图衔接 · L232 · 773
  - WLD-OUTSKIRTS §8.1 提示文案 · L234 · 449
  - WLD-OUTSKIRTS §8.2 与多地图、平面地图、存档的衔接 · L246 · 307
- WLD-OUTSKIRTS §9 数据与许可 · L253 · 443
- WLD-OUTSKIRTS §10 待验证 · L259 · 345

## WLD-TERRAIN · 地形：设计原则、分层、贴地、竖直落差、水位与可走性

`docs/world/WLD-TERRAIN-地形.md` · 12.9k 字 · 不分图的地形规则：分层、手感刻度、坡度与可走性、贴地、竖直落差、水位与岸型、着色
关键词：地形、地形分层、高度预算、主模数、手感刻度、灰区、坡度类、零带、地形操作、地形覆盖层、合成顺序、先平后贴、贴地、垫层、勒脚、台缘带、退台、竖直落差、常水位、岸型、涉水带、开放泳区水深、Steep、分面平法线、遮挡与揭示、terrain.json

- WLD-TERRAIN §0 结论 · L22 · 1.2k
  - ★ WLD-TERRAIN §0.1 接入要点 · L33 · 627
- WLD-TERRAIN §1 设计原则 · L45 · 929
  - WLD-TERRAIN §1.1 总骨架 · L47 · 189
  - WLD-TERRAIN §1.2 规则 · L53 · 729
- WLD-TERRAIN §2 高度预算与分层 · L67 · 1.8k
  - WLD-TERRAIN §2.1 分层 · L69 · 493
  - WLD-TERRAIN §2.2 手感刻度 · L82 · 488
  - WLD-TERRAIN §2.3 坡度类 · L97 · 517
  - WLD-TERRAIN §2.4 全图范围与零带 · L113 · 265
- WLD-TERRAIN §3 地形的表达与合成 · L120 · 1.3k
- WLD-TERRAIN §4 贴地 · L135 · 1.5k
  - WLD-TERRAIN §4.1 先平后贴 · L137 · 303
  - WLD-TERRAIN §4.2 道路与铺装 · L150 · 321
  - WLD-TERRAIN §4.3 楼宇：垫层、勒脚与入口 · L157 · 572
  - WLD-TERRAIN §4.4 树与道具落地 · L166 · 319
- WLD-TERRAIN §5 竖直落差 · L173 · 1.3k
  - WLD-TERRAIN §5.1 连接规则 · L175 · 620
  - WLD-TERRAIN §5.2 台缘带（v1 的挡墙） · L193 · 319
  - WLD-TERRAIN §5.3 退台与高墙 · L200 · 184
  - WLD-TERRAIN §5.4 防护与谁来做 · L205 · 196
- WLD-TERRAIN §6 水位与岸型 · L210 · 1.6k
  - WLD-TERRAIN §6.1 常水位 · L212 · 257
  - WLD-TERRAIN §6.2 岸型 · L218 · 573
  - WLD-TERRAIN §6.3 涉水带、湖盆与河渠 · L229 · 354
  - WLD-TERRAIN §6.4 穿出校界 · L235 · 110
  - WLD-TERRAIN §6.5 开放泳区的水深与出口 · L239 · 280
- WLD-TERRAIN §7 可走性与碰撞 · L245 · 634
- WLD-TERRAIN §8 地面着色与描线 · L254 · 881
- WLD-TERRAIN §9 待验证 · L274 · 427

## WLD-VEGETATION · 植被：树种与四季、银杏大道与水杉路、草坪与灌木

`docs/world/WLD-VEGETATION-植被.md` · 12.5k 字 · 树木、灌木、草坪与水生植物的观感、树种四季、种植、地面覆盖与季相规则
关键词：植被、植物、树种、树冠画法、云团型、插片型、冬季裸枝、叶量、开花、季相、局部季节、四季变色、银杏大道、水杉路、春季粉带、草坪、绿篱、荷、芦苇、落叶地毯、树冠遮蔽、覆雪、过渡绿地、species.json

- WLD-VEGETATION §0 结论 · L22 · 1.3k
  - ★ WLD-VEGETATION §0.1 接入要点 · L34 · 672
- WLD-VEGETATION §1 卡通树冠的画法 · L47 · 1.4k
  - WLD-VEGETATION §1.1 团块与三类树冠 · L51 · 312
  - WLD-VEGETATION §1.2 明暗与法线 · L60 · 160
  - WLD-VEGETATION §1.3 逆光 · L66 · 279
  - WLD-VEGETATION §1.4 描线、远景与树干 · L74 · 206
  - WLD-VEGETATION §1.5 冬季裸枝 · L80 · 416
- WLD-VEGETATION §2 树种与四季 · L88 · 2.0k
  - WLD-VEGETATION §2.1 落叶树 · L94 · 805
  - WLD-VEGETATION §2.2 常绿树与灌木 · L115 · 358
  - WLD-VEGETATION §2.3 色名与季相行 · L128 · 571
- WLD-VEGETATION §3 种植 · L134 · 1.4k
  - WLD-VEGETATION §3.1 线性种植 · L136 · 490
  - WLD-VEGETATION §3.2 片区种植 · L144 · 523
  - WLD-VEGETATION §3.3 外围（WLD-OUTSKIRTS） · L152 · 384
- WLD-VEGETATION §4 草坪 · L161 · 1.4k
  - WLD-VEGETATION §4.1 颜色与长影 · L163 · 220
  - WLD-VEGETATION §4.2 开阔草坪 · L168 · 301
  - WLD-VEGETATION §4.3 积雪与季节花境 · L176 · 309
  - WLD-VEGETATION §4.4 过渡绿地的草本与草面（D-185） · L181 · 231
  - WLD-VEGETATION §4.5 草叶的表示与交界（D-205，取值是初值，样板同位姿定） · L188 · 290
- WLD-VEGETATION §5 落叶、花瓣与积雪（地面覆盖） · L193 · 973
- WLD-VEGETATION §6 水生植物 · L210 · 535
- WLD-VEGETATION §7 季相与天气 · L224 · 1.1k
  - WLD-VEGETATION §7.1 逐株错相与叶量 · L226 · 193
  - WLD-VEGETATION §7.2 季节颜色 · L231 · 194
  - WLD-VEGETATION §7.3 摆动与飘落（给 SYS-MOTION 的参数） · L236 · 160
  - WLD-VEGETATION §7.4 覆雪 · L241 · 309
  - WLD-VEGETATION §7.5 换季与天气切换 · L247 · 237
- WLD-VEGETATION §8 碰撞、画面设置与声音 · L253 · 918
  - WLD-VEGETATION §8.1 行走碰撞 · L255 · 261
  - WLD-VEGETATION §8.2 画面设置（SYS-GRAPHICS） · L263 · 348
  - WLD-VEGETATION §8.3 声音的接口（SYS-AUDIO） · L270 · 292
- WLD-VEGETATION §9 待验证 · L279 · 122

## WLD-WATER · 水体：动画式水面、西湖、东湖、河渠、喷泉

`docs/world/WLD-WATER-水体.md` · 11.1k 字 · 动画式水面画法、反光带与倒影、四季天气、开放泳区、各湖河渠与喷泉
关键词：水体、水面、开放泳区、西湖、东湖、河渠、波纹线、主光反光带、碎金带、月光银带、四芒星闪点、泡沫线、倒影、平面反射、伪深度色带、雨点环、涉水涟漪、入水水花、灯的竖条倒影、refl_lamps、西湖喷泉、叠水喷泉、游泳池池水、冬季浅滩、漂浮物、河渠中线

- WLD-WATER §0 结论 · L22 · 1.6k
  - ★ WLD-WATER §0.1 接入要点 · L34 · 887
- WLD-WATER §1 水面的通用画法 · L48 · 727
  - WLD-WATER §1.1 底色与色带 · L52 · 357
  - WLD-WATER §1.2 波纹线与泡沫线 · L58 · 284
- WLD-WATER §2 反光带、灯条与倒影 · L64 · 1.3k
  - WLD-WATER §2.1 主光反光带 · L66 · 442
  - WLD-WATER §2.2 灯的竖条倒影 · L74 · 169
  - WLD-WATER §2.3 倒影 · L78 · 698
- WLD-WATER §3 四季与天气的水面 · L95 · 834
- WLD-WATER §4 涉水、开放泳区与游泳反馈 · L122 · 971
  - WLD-WATER §4.1 涉水涟漪 · L124 · 194
  - WLD-WATER §4.2 开放泳区与出口 · L128 · 399
  - WLD-WATER §4.3 游泳的水面反馈 · L135 · 359
- WLD-WATER §5 西湖 · L143 · 785
  - WLD-WATER §5.1 水下森林与反光带 · L145 · 323
  - WLD-WATER §5.2 扇形喷泉 · L151 · 453
- WLD-WATER §6 东湖 · L161 · 470
- WLD-WATER §7 河渠 · L168 · 865
- WLD-WATER §8 池水、叠水喷泉与截面水柱 · L183 · 1.1k
  - WLD-WATER §8.1 游泳馆与室外游泳池 · L185 · 383
  - WLD-WATER §8.2 品学楼门前叠水喷泉 · L195 · 440
  - WLD-WATER §8.3 主楼水池与截面水柱 · L205 · 267
- WLD-WATER §9 水体系统的规则 · L210 · 697
- WLD-WATER §10 待验证 · L220 · 343

## MAP-QSH-ADDITIONS · 设计增补：给没有设计感的地方加设计

`docs/maps/qingshuihe/MAP-QSH-ADDITIONS-设计增补.md` · 16.7k 字 · 全校无设计感处的增补词汇、楼宇设计规则、禁做清单，夜间灯光总图和雨天增补
关键词：设计增补、增补物、增补词汇、作用代号、默认状态规则、初灯组、照度三级、夜间灯光总图、普通楼宇设计规则、西立面竖向鳍片、禁做清单、新颜色登记、可跳上、导览亭、导览桩、直饮水台、雨帘、避雨处、积水点、lighting-schedule.json、功能开敞面、过渡绿地、空间记录

- MAP-QSH-ADDITIONS §0 结论 · L22 · 1.1k
  - ★ MAP-QSH-ADDITIONS §0.1 接入要点 · L33 · 503
- MAP-QSH-ADDITIONS §1 通用规则 · L42 · 431
- MAP-QSH-ADDITIONS §2 默认状态规则与作用代号 · L49 · 855
- MAP-QSH-ADDITIONS §3 大片空地的整理 · L75 · 603
- MAP-QSH-ADDITIONS §4 参照与转译规则 · L85 · 627
- MAP-QSH-ADDITIONS §5 宿舍组团、食堂与商业街前 · L96 · 957
- MAP-QSH-ADDITIONS §6 道路、路口与停车场 · L114 · 700
- MAP-QSH-ADDITIONS §7 河渠、湖岸与桥 · L127 · 1.0k
- MAP-QSH-ADDITIONS §8 草坪、楼间空地与屋顶 · L144 · 867
- MAP-QSH-ADDITIONS §9 运动区、校界与步行服务设施 · L160 · 1.4k
- MAP-QSH-ADDITIONS §10 让普通楼宇有设计感 · L182 · 1.1k
- MAP-QSH-ADDITIONS §11 不建议增补（禁做清单） · L199 · 776
- MAP-QSH-ADDITIONS §12 需要登记的新颜色 · L221 · 569
- MAP-QSH-ADDITIONS §13 增补物的行走分类 · L241 · 554
- MAP-QSH-ADDITIONS §14 夜间灯光总图：级别与颜色 · L251 · 988
- MAP-QSH-ADDITIONS §15 初灯组与逐盏调度 · L271 · 302
- MAP-QSH-ADDITIONS §16 雨夜与雾夜 · L277 · 559
- MAP-QSH-ADDITIONS §17 雨帘与避雨处 · L287 · 776
- MAP-QSH-ADDITIONS §18 积水点 · L303 · 579
- MAP-QSH-ADDITIONS §19 待验证 · L312 · 454

## MAP-QSH-CENTER-EAST · 中部与东区：中部科研区、雕像、品学楼、文科楼群、训练馆、球场、田径场

`docs/maps/qingshuihe/MAP-QSH-CENTER-EAST-中部与东区.md` · 14.7k 字 · 中部科研区与东区各建筑、雕像、球场的外观与身份特征、开放入口门口表现、四季昼夜、天气、行走与声音要点
关键词：中部科研区、东区、基础实验大楼、一号科研实验楼、科研楼B区、科研楼C区、麦克斯韦像、方程石、林为干院士像、科研楼碑、银杏大道跨河小桥、品学楼、第一教学楼、书法横梁门框、架空通道、叠水喷泉石台、文科楼群、友谊树、综合训练馆、筒拱、网球场树墙、绿色房间、球场标线、球场围网、高杆灯、田径场、器材室、室外游泳池组团、东区月出、overlay/center-east.d

- MAP-QSH-CENTER-EAST §0 结论 · L22 · 1.4k
  - ★ MAP-QSH-CENTER-EAST §0.1 接入要点 · L33 · 708
- MAP-QSH-CENTER-EAST §1 中部科研区 · L44 · 905
- MAP-QSH-CENTER-EAST §2 品学楼 · L56 · 1.2k
- MAP-QSH-CENTER-EAST §3 品学楼开放室内 · L66 · 591
- MAP-QSH-CENTER-EAST §4 文科楼群 · L76 · 548
- MAP-QSH-CENTER-EAST §5 综合训练馆与球场 · L83 · 1.1k
- MAP-QSH-CENTER-EAST §6 东缘 · L92 · 552
- MAP-QSH-CENTER-EAST §7 本区的光 · L99 · 328
- MAP-QSH-CENTER-EAST §8 季节与昼夜清单 · L108 · 1.7k
- MAP-QSH-CENTER-EAST §9 天气要点 · L132 · 515
- MAP-QSH-CENTER-EAST §10 行走：主网、挡路物与开放入口 · L140 · 1.0k
- MAP-QSH-CENTER-EAST §11 行走：点位、节奏与锚点 · L163 · 1.0k
- MAP-QSH-CENTER-EAST §12 声音要点 · L180 · 437
- MAP-QSH-CENTER-EAST §13 本区身份特征、设计增补与近景要求 · L190 · 1.2k
- MAP-QSH-CENTER-EAST §14 待验证 · L215 · 486

## MAP-QSH-CORE · 核心区：八角图书馆、时间广场、日晷、博物馆、西湖

`docs/maps/qingshuihe/MAP-QSH-CORE-核心区.md` · 15.6k 字 · 核心区图书馆、时间广场、日晷、博物馆外观与西湖岸线构筑物，含四季昼夜、天气、行走与声音要点
关键词：核心区、图书馆外观、八角采光顶、穹顶、方塔玻璃槽、竖缝窗、大台阶、二楼正门、品·悦吧店面、宁静苑、无主体块、时间广场、旗杆、十二时辰刻度、日晷、晷针、博物馆外观、柱廊环、西湖、折线栈桥、白色拱桥、湖心岛观景平台、西湖喷泉、箱涵、天鹅屋、入水口荷花、草阶看台、overlay/core.d

- MAP-QSH-CORE §0 结论 · L22 · 1.5k
  - ★ MAP-QSH-CORE §0.1 接入要点 · L34 · 759
- MAP-QSH-CORE §1 图书馆外观 · L46 · 901
- MAP-QSH-CORE §2 图书馆灯光 · L55 · 605
- MAP-QSH-CORE §3 入口、品·悦吧与宁静苑 · L66 · 596
- MAP-QSH-CORE §4 时间广场 · L72 · 483
- MAP-QSH-CORE §5 日晷 · L80 · 445
- MAP-QSH-CORE §6 电子科技博物馆 · L87 · 557
- MAP-QSH-CORE §7 西湖的岸线与构筑物 · L95 · 1.0k
- MAP-QSH-CORE §8 西湖喷泉 · L108 · 476
- MAP-QSH-CORE §9 本区的构图关系 · L122 · 375
- MAP-QSH-CORE §10 季节与昼夜清单 · L129 · 2.0k
  - MAP-QSH-CORE §10.1 夜间角色与四季 · L133 · 1.6k
  - MAP-QSH-CORE §10.2 交给 WP-39 的灯具 · L153 · 290
- MAP-QSH-CORE §11 天气要点 · L157 · 618
- MAP-QSH-CORE §12 行走：主网、挡路物与开放入口 · L165 · 956
- MAP-QSH-CORE §13 行走：点位、节奏与锚点 · L185 · 935
- MAP-QSH-CORE §14 声音要点 · L204 · 536
- MAP-QSH-CORE §15 本区身份特征、设计增补与近景要求 · L214 · 1.2k
- MAP-QSH-CORE §16 待验证 · L238 · 669

## MAP-QSH-LIBRARY · 图书馆与博物馆室内

`docs/maps/qingshuihe/MAP-QSH-LIBRARY-图书馆与博物馆室内.md` · 14.9k 字 · 图书馆与博物馆的开放室内、三条看进去的路径、室内光与视觉落点，以及其余开放楼的设计要点总表
关键词：图书馆室内、中庭、回廊、八字楼梯、塔楼梯、交通核、开放范围、封口、门斗、闸机、二楼门厅、看进去的路径、品·悦吧室内、光条、顶棚波光、红桌、视觉落点、八瓣光、台灯、校训木匾、五楼西窗阅览带、北侧露台、博物馆展厅、环形展线、开放楼总表、interiors.json、lib.main

- MAP-QSH-LIBRARY §0 结论 · L24 · 1.4k
  - ★ MAP-QSH-LIBRARY §0.1 接入要点 · L36 · 777
- MAP-QSH-LIBRARY §1 平面、交通核与开放范围 · L49 · 1.4k
- MAP-QSH-LIBRARY §2 三条看进去的路径 · L77 · 644
- MAP-QSH-LIBRARY §3 中庭与回廊 · L89 · 390
- MAP-QSH-LIBRARY §4 一楼中厅、门斗与二楼门厅 · L97 · 775
- MAP-QSH-LIBRARY §5 室外光的延伸 · L105 · 728
- MAP-QSH-LIBRARY §6 视觉落点 · L115 · 518
- MAP-QSH-LIBRARY §7 四季、昼夜、天气与生活痕迹 · L124 · 1.3k
- MAP-QSH-LIBRARY §8 行走 · L137 · 914
- MAP-QSH-LIBRARY §9 附属部分 · L148 · 620
- MAP-QSH-LIBRARY §10 博物馆展厅 · L154 · 654
- MAP-QSH-LIBRARY §11 声音要点 · L166 · 596
- MAP-QSH-LIBRARY §12 其余开放楼总表 · L176 · 2.8k
  - MAP-QSH-LIBRARY §12.1 南区 · L180 · 940
  - MAP-QSH-LIBRARY §12.2 西区与中东区 · L189 · 817
  - MAP-QSH-LIBRARY §12.3 北区 · L198 · 921
- MAP-QSH-LIBRARY §13 待验证 · L208 · 535

## MAP-QSH-NORTH · 北部生活区：学生活动中心、校医院、商业街、宿舍组团、餐厅

`docs/maps/qingshuihe/MAP-QSH-NORTH-北部生活区.md` · 15.9k 字 · 北部生活区各建筑的外观与身份特征、开放入口的门口表现、四季昼夜、天气、行走与声音要点
关键词：北部生活区、学生活动中心、成电会堂、赭红圆鼓、校医院、院名灯牌、商业街、玻璃拱顶步行街、店招、宿舍组团、学知苑、硕丰苑、博翰苑、留学生组团、本科楼A型、本科楼B型、组团闸机、学知一组团、阳台晾晒、银杏苑、芙蓉餐厅、思学苑、春晖苑、朝阳餐厅、西北餐厅、室外扶梯罩棚、风雨连廊、丹桂路、博士公寓、overlay/north.d

- MAP-QSH-NORTH §0 结论 · L22 · 1.4k
  - ★ MAP-QSH-NORTH §0.1 接入要点 · L33 · 682
- MAP-QSH-NORTH §1 学生活动中心与成电会堂 · L44 · 613
- MAP-QSH-NORTH §2 校医院 · L56 · 443
- MAP-QSH-NORTH §3 商业街 · L63 · 1.1k
- MAP-QSH-NORTH §4 宿舍 · L72 · 1.8k
- MAP-QSH-NORTH §5 开放室内：学知苑 1 栋 · L86 · 613
- MAP-QSH-NORTH §6 餐厅 · L96 · 1.6k
- MAP-QSH-NORTH §7 文化地标与丹桂路 · L110 · 213
- MAP-QSH-NORTH §8 风雨连廊与本区增补 · L115 · 309
- MAP-QSH-NORTH §9 季节与昼夜清单 · L125 · 1.7k
  - MAP-QSH-NORTH §9.1 夜间角色与四季 · L131 · 1.2k
  - MAP-QSH-NORTH §9.2 开放室内的灯 · L154 · 156
- MAP-QSH-NORTH §10 天气要点 · L158 · 558
- MAP-QSH-NORTH §11 行走：主网、挡路物与开放入口 · L166 · 1.1k
- MAP-QSH-NORTH §12 行走：点位、节奏与锚点 · L188 · 1.1k
- MAP-QSH-NORTH §13 声音要点 · L208 · 541
- MAP-QSH-NORTH §14 本区身份特征、设计增补与近景要求 · L219 · 1.0k
- MAP-QSH-NORTH §15 待验证 · L243 · 416

## MAP-QSH-RENOVATION · 清水河翻新清单

`docs/maps/qingshuihe/MAP-QSH-RENOVATION-清水河翻新清单.md` · 12.1k 字 · 清水河校区要翻新的楼与环境：逐条的楼名、优先级、翻新部位和所用规则，以及待核实楼的做法
关键词：翻新清单、逐栋翻新、翻新条目、条目号、翻新优先级、高中低档、部位代号、楼型、翻新对照楼、待核实的楼、核实前的口径、缺立面近景、不列入的楼、2015 年以后的新楼、宿舍屋面设备、组团门禁、商业街店招、全校环境翻新、老楼翻新理由

- MAP-QSH-RENOVATION §0 结论 · L22 · 978
  - ★ MAP-QSH-RENOVATION §0.1 接入要点 · L30 · 483
- MAP-QSH-RENOVATION §1 优先级、条目号、部位代号与楼型 · L40 · 2.0k
  - MAP-QSH-RENOVATION §1.1 优先级 · L42 · 259
  - MAP-QSH-RENOVATION §1.2 条目号 · L52 · 133
  - MAP-QSH-RENOVATION §1.3 部位代号与通用做法 · L56 · 1.3k
  - MAP-QSH-RENOVATION §1.4 楼型 · L75 · 316
- MAP-QSH-RENOVATION §2 北部生活区（MAP-QSH-NORTH，WP-20） · L79 · 2.3k
  - MAP-QSH-RENOVATION §2.1 宿舍 · L81 · 1.1k
  - MAP-QSH-RENOVATION §2.2 食堂、商业与生活服务 · L96 · 827
  - MAP-QSH-RENOVATION §2.3 组团门禁与屋面设备 · L109 · 410
- MAP-QSH-RENOVATION §3 中部与东区（MAP-QSH-CENTER-EAST，WP-21） · L116 · 691
- MAP-QSH-RENOVATION §4 南区（MAP-QSH-SOUTH，WP-18） · L128 · 781
- MAP-QSH-RENOVATION §5 西区与西北区（MAP-QSH-WEST，WP-19） · L141 · 320
- MAP-QSH-RENOVATION §6 核心区（MAP-QSH-CORE，WP-16） · L150 · 240
- MAP-QSH-RENOVATION §7 全校环境 · L157 · 726
- MAP-QSH-RENOVATION §8 待核实的楼 · L172 · 2.0k
  - MAP-QSH-RENOVATION §8.1 核实前的口径 · L174 · 325
  - MAP-QSH-RENOVATION §8.2 年代未查到的楼 · L182 · 1.4k
  - MAP-QSH-RENOVATION §8.3 缺立面近景的楼 · L207 · 287
- MAP-QSH-RENOVATION §9 不列入的楼 · L211 · 174
- MAP-QSH-RENOVATION §10 登记与施工顺序 · L215 · 543
- MAP-QSH-RENOVATION §11 待验证 · L222 · 312

## MAP-QSH-SOUTH · 南区：南门、主楼、东湖、宣邦楼、立人楼、体育场馆、国际创新中心

`docs/maps/qingshuihe/MAP-QSH-SOUTH-南区.md` · 16.9k 字 · 南区各楼与构筑物的外观与身份特征、受光与夜景、四季昼夜与天气、行走与开放入口、声音要点
关键词：南区、南门、校名墙、主楼、主轴线、屋顶空廊、过街楼、主楼泛光、森林公园、孔雀之家、东湖岸线、环湖跑道、里程标、六角亭、亲水平台、荷角、吴立人像、宣邦楼、钟楼、电子管穹顶、交班的钟、钟面指针、立人楼、体育馆、游泳馆、体育运动中心大平台、看台罩棚、国际创新中心、1–8 号楼、overlay/south.d

- MAP-QSH-SOUTH §0 结论 · L22 · 1.1k
  - ★ MAP-QSH-SOUTH §0.1 接入要点 · L33 · 551
- MAP-QSH-SOUTH §1 南门 · L42 · 558
- MAP-QSH-SOUTH §2 主楼 · L50 · 1.1k
- MAP-QSH-SOUTH §3 过街楼、开放室内与周边 · L63 · 757
- MAP-QSH-SOUTH §4 东湖 · L71 · 908
- MAP-QSH-SOUTH §5 宣邦楼 · L82 · 1.4k
- MAP-QSH-SOUTH §6 立人楼 · L98 · 453
- MAP-QSH-SOUTH §7 体育场馆（沿西源大道从西到东） · L105 · 951
- MAP-QSH-SOUTH §8 西南角：国际创新中心与科创餐厅 · L115 · 370
- MAP-QSH-SOUTH §9 南区中部的 1–8 号楼 · L121 · 363
- MAP-QSH-SOUTH §10 本区的水道 · L125 · 271
- MAP-QSH-SOUTH §11 季节与昼夜清单 · L131 · 1.9k
  - MAP-QSH-SOUTH §11.1 夜间角色与四季 · L135 · 1.5k
  - MAP-QSH-SOUTH §11.2 交给 WP-39 的灯具 · L158 · 249
- MAP-QSH-SOUTH §12 天气要点 · L162 · 570
- MAP-QSH-SOUTH §13 行走：主网、挡路物与开放入口 · L170 · 1.2k
- MAP-QSH-SOUTH §14 行走：点位、节奏与锚点 · L192 · 1.1k
- MAP-QSH-SOUTH §15 声音要点 · L211 · 497
- MAP-QSH-SOUTH §16 本区身份特征、设计增补与近景要求 · L222 · 1.2k
- MAP-QSH-SOUTH §17 待验证 · L250 · 557

## MAP-QSH-TERRAIN · 分区地形：骨架、地标丘、揭示路线、常水位与各区地形设计

`docs/maps/qingshuihe/MAP-QSH-TERRAIN-分区地形.md` · 16.0k 字 · 清水河地形设计：层分布、红枫丘、揭示路线、常水位、十处分区的标高与连接
关键词：分区地形、地形骨架、红枫丘、地标丘、下沉时间广场、落金沟、湖岸丘链、草阶看台台地、东湖亭丘、岛丘、品学楼台基、坡广场、宿舍台原、上台、下台、台口、通学坡、看月台、堤上路、球场堤路、下沉草阶草坪、河岸低步道、渠堤、湿地低地、扇形草坪浅碟、中庭草丘、常水位表、湖口跌水、箱涵堰口、遮挡与揭示、地形验收镜头、terrain.json

- MAP-QSH-TERRAIN §0 结论 · L22 · 1.7k
  - ★ MAP-QSH-TERRAIN §0.1 接入要点 · L33 · 866
- MAP-QSH-TERRAIN §1 全图骨架 · L47 · 1.9k
  - MAP-QSH-TERRAIN §1.1 分层分布 · L49 · 740
  - MAP-QSH-TERRAIN §1.2 地标丘：红枫丘 · L64 · 602
  - MAP-QSH-TERRAIN §1.3 遮挡与揭示的三条路线 · L72 · 541
- MAP-QSH-TERRAIN §2 水系与常水位 · L82 · 1.1k
- MAP-QSH-TERRAIN §3 着色取舍与文件归属 · L104 · 835
  - MAP-QSH-TERRAIN §3.1 坡度与着色 · L106 · 313
  - MAP-QSH-TERRAIN §3.2 文件归属 · L112 · 506
- MAP-QSH-TERRAIN §4 中轴广场与图书馆 · L124 · 494
- MAP-QSH-TERRAIN §5 银杏大道 · L133 · 389
- MAP-QSH-TERRAIN §6 西湖与红枫丘 · L140 · 835
- MAP-QSH-TERRAIN §7 东湖 · L149 · 566
- MAP-QSH-TERRAIN §8 河渠与箱涵 · L156 · 856
- MAP-QSH-TERRAIN §9 宿舍区 · L165 · 1.3k
- MAP-QSH-TERRAIN §10 运动场与体育区 · L177 · 715
- MAP-QSH-TERRAIN §11 食堂与商业街 · L185 · 351
- MAP-QSH-TERRAIN §12 教学科研区 · L191 · 989
- MAP-QSH-TERRAIN §13 校门与校界 · L199 · 283
- MAP-QSH-TERRAIN §14 验收镜头、冲突与点位标高 · L205 · 1.7k
  - MAP-QSH-TERRAIN §14.1 验收镜头 · L207 · 681
  - MAP-QSH-TERRAIN §14.2 冲突检查的结论 · L225 · 510
  - MAP-QSH-TERRAIN §14.3 点位与锚点标高 · L233 · 538
- MAP-QSH-TERRAIN §15 待验证 · L247 · 626

## MAP-QSH-WEST · 西区与西北区：六号科研楼、校训石、创新中心、经管楼、托幼园、四号/五号科研楼、湿地、西门桥头

`docs/maps/qingshuihe/MAP-QSH-WEST-西区与西北区.md` · 13.1k 字 · 西区与西北区各楼、石刻、湿地与西门桥头的外观与身份特征、四季昼夜与天气、行走与开放入口、声音要点
关键词：西区、西北区、六号科研楼、基础与前沿研究院、公式台阶、2F室外连廊、校训石、创新中心、经管楼、三号科研实验楼、宾诺咖啡、立人托幼公园、键盘立面、四号科研楼、扇形草坪、深蓝玻璃盒入口、五号科研楼、电子科学技术研究院大楼、微波暗室、工程训练中心、荟文楼、综合楼、核磁共振中心、国防科技园石刻、湿地公园、湿地栈道、西门桥头、水杉路、孤儿楼 6151 与 6154、overlay/west.d

- MAP-QSH-WEST §0 结论 · L22 · 1.3k
  - ★ MAP-QSH-WEST §0.1 接入要点 · L33 · 571
- MAP-QSH-WEST §1 六号科研楼 · L43 · 918
- MAP-QSH-WEST §2 校训石 · L52 · 257
- MAP-QSH-WEST §3 创新中心 · L57 · 275
- MAP-QSH-WEST §4 经管楼 · L63 · 73
- MAP-QSH-WEST §5 立人托幼公园 · L67 · 204
- MAP-QSH-WEST §6 四号科研楼 · L72 · 351
- MAP-QSH-WEST §7 五号科研楼、研究院大楼、核磁共振中心 · L78 · 619
- MAP-QSH-WEST §8 荟文楼（综合楼） · L85 · 68
- MAP-QSH-WEST §9 石刻 · L89 · 65
- MAP-QSH-WEST §10 湿地公园与西门桥头 · L93 · 659
- MAP-QSH-WEST §11 季节与昼夜清单 · L103 · 1.7k
- MAP-QSH-WEST §12 天气要点 · L130 · 576
- MAP-QSH-WEST §13 行走：主网、挡路物与开放入口 · L138 · 1.2k
- MAP-QSH-WEST §14 行走：点位、节奏与锚点 · L160 · 1.0k
- MAP-QSH-WEST §15 声音要点 · L177 · 305
- MAP-QSH-WEST §16 本区身份特征、设计增补与近景要求 · L186 · 1.2k
- MAP-QSH-WEST §17 待验证 · L211 · 583

## SYS-ATLAS · 平面地图与传送：底图、小地图、大地图、标记、锚点、传送与解锁

`docs/systems/SYS-ATLAS-平面地图与传送.md` · 13.0k 字 · 平面地图的设计：底图内容与上色、小地图、大地图、标记类型、锚点实体、雾幕传送与落点、解锁策略、存档与设置
关键词：平面地图、小地图、大地图、底图、地图标记、锚点、传送点、导览亭、导览桩、传送、雾幕瞬移、飞过去、传送卡、传送落点、目录页、追踪、自定义标记、解锁策略、区域名、地名标注、atlasRect、IAtlasService

- SYS-ATLAS §0 结论 · L22 · 1.2k
  - ★ SYS-ATLAS §0.1 接入要点 · L33 · 628
- SYS-ATLAS §1 允许什么、禁止什么 · L45 · 399
- SYS-ATLAS §2 底图 · L57 · 2.1k
  - SYS-ATLAS §2.1 来源：世界模型，不是原始 OSM · L59 · 481
  - SYS-ATLAS §2.2 上色 · L68 · 497
  - SYS-ATLAS §2.3 外围、校界与箱庭外缘 · L74 · 482
  - SYS-ATLAS §2.4 季节与昼夜：只做轻微变化 · L81 · 193
  - SYS-ATLAS §2.5 文字与标注 · L86 · 391
- SYS-ATLAS §3 小地图 · L93 · 914
- SYS-ATLAS §4 大地图 · L104 · 1.8k
  - SYS-ATLAS §4.1 布局与开合 · L106 · 454
  - SYS-ATLAS §4.2 缩放与平移 · L113 · 306
  - SYS-ATLAS §4.3 图层与目录 · L118 · 296
  - SYS-ATLAS §4.4 传送卡 · L123 · 365
  - SYS-ATLAS §4.5 自定义标记与追踪 · L129 · 279
  - SYS-ATLAS §4.6 按键 · L134 · 114
- SYS-ATLAS §5 标记类型 · L138 · 996
- SYS-ATLAS §6 锚点实体：导览亭与导览桩 · L155 · 1.1k
- SYS-ATLAS §7 传送流程 · L171 · 1.4k
  - SYS-ATLAS §7.1 两种方式 · L173 · 257
  - SYS-ATLAS §7.2 传送的时序 · L179 · 411
  - SYS-ATLAS §7.3 落点 · L190 · 435
  - SYS-ATLAS §7.4 预取、打断与边界情况 · L202 · 256
- SYS-ATLAS §8 解锁策略 · L207 · 453
- SYS-ATLAS §9 多地图下的数据 · L215 · 508
- SYS-ATLAS §10 存档里存什么、设置里存什么 · L222 · 354
- SYS-ATLAS §11 待验证 · L231 · 245

## SYS-AUDIO · 音频：环境声、观者声、空间声学、界面音效与混音

`docs/systems/SYS-AUDIO-音频.md` · 13.7k 字 · 声音的设计意图与禁区、混音原则、环境声景、天气与钟声、观者声、空间声学、界面音、素材许可
关键词：音频、声音、环境声、声景、观者声、脚步声、空间声学、混响、混音、界面音效、开场音乐、远雷、钟声、校园广播、人迹声、素材许可、IAudioService、IAmbienceService、IFoleyService、IAcousticEnv、FootMat、audio.cfg、G21

- SYS-AUDIO §0 结论 · L21 · 1.4k
  - ★ SYS-AUDIO §0.1 接入要点 · L34 · 842
- SYS-AUDIO §1 总体与生命周期 · L44 · 685
- SYS-AUDIO §2 混音与响度 · L54 · 616
- SYS-AUDIO §3 环境声景 · L73 · 1.7k
  - SYS-AUDIO §3.1 七层与驱动量 · L75 · 337
  - SYS-AUDIO §3.2 数据驱动的发声器 · L82 · 393
  - SYS-AUDIO §3.3 季节与出没 · L89 · 227
  - SYS-AUDIO §3.4 生命感规则 · L94 · 296
  - SYS-AUDIO §3.5 室内档案 · L101 · 116
  - SYS-AUDIO §3.6 校外、外围与边界 · L105 · 126
  - SYS-AUDIO §3.7 离地高度与介质 · L109 · 118
  - SYS-AUDIO §3.8 英雄声景 · L113 · 109
- SYS-AUDIO §4 天气声 · L117 · 468
- SYS-AUDIO §5 远雷、钟声、广播与偶发事件 · L126 · 684
- SYS-AUDIO §6 加速流逝的节制与转场 · L134 · 530
- SYS-AUDIO §7 人迹声：确保不出现清晰人声 · L146 · 630
- SYS-AUDIO §8 观者声 · L157 · 1.4k
  - SYS-AUDIO §8.1 原则 · L159 · 181
  - SYS-AUDIO §8.2 步相与步频 · L164 · 212
  - SYS-AUDIO §8.3 材质与覆盖物 · L170 · 399
  - SYS-AUDIO §8.4 分层与变体 · L176 · 171
  - SYS-AUDIO §8.5 交互音 · L181 · 301
  - SYS-AUDIO §8.6 飞行风声 · L188 · 150
- SYS-AUDIO §9 空间声学 · L192 · 1.1k
  - SYS-AUDIO §9.1 "窄处脚步更明显" · L194 · 211
  - SYS-AUDIO §9.2 方案与类别 · L200 · 341
  - SYS-AUDIO §9.3 遮挡与室内外传声 · L206 · 371
  - SYS-AUDIO §9.4 介质与天气的消音感 · L214 · 116
- SYS-AUDIO §10 界面音效 · L218 · 459
- SYS-AUDIO §11 开场音乐 · L226 · 292
- SYS-AUDIO §12 设置 · L231 · 360
- SYS-AUDIO §13 素材策略与许可 · L237 · 528
- SYS-AUDIO §14 预算（设计目标） · L244 · 199
- SYS-AUDIO §15 验收 · L251 · 273
- SYS-AUDIO §16 待验证（P0） · L257 · 110

## SYS-GRAPHICS · 画面设置：预设、单项、风格底线、自动推荐与自适应

`docs/systems/SYS-GRAPHICS-画面设置.md` · 8.9k 字 · 四档画面预设与单项、风格底线、应用方式、首次分档与自动调节、持久化原则
关键词：画面设置、画质、画面预设、设备档、流畅、均衡、精致、极致、单项、风格底线、风格默认、自动调节画质、自适应、首次分档、渲染比例、超采样、雾幕类设置、画面页、显存估算、IQualityService、IQualityAware、QualityResponsive、QualityKnob、quality-presets.json、graphics.cfg、reset-graphics.cmd

- SYS-GRAPHICS §0 结论 · L22 · 1.6k
  - ★ SYS-GRAPHICS §0.1 接入要点 · L33 · 860
- SYS-GRAPHICS §1 预设 · L46 · 856
- SYS-GRAPHICS §2 风格底线与风格默认 · L64 · 660
- SYS-GRAPHICS §3 单项与生效方式 · L77 · 999
- SYS-GRAPHICS §4 应用方式 · L102 · 455
- SYS-GRAPHICS §5 自动推荐与自动调节 · L112 · 1.3k
- SYS-GRAPHICS §6 持久化与恢复 · L137 · 556
- SYS-GRAPHICS §7 画面页（WP-35，视觉见 UI-VISUAL §8） · L143 · 566
- SYS-GRAPHICS §8 待验证（P0） · L155 · 368

## SYS-MAPS · 多地图：按图数据、`IMapService` 与切换

`docs/systems/SYS-MAPS-多地图.md` · 11.5k 字 · 按图数据与全局的划分、先卸后装切图、时间全局天气按图、map_rect、单图半径
关键词：多地图、换图、切图、地图服务、按图数据、先卸后装、同图重载、天气按图、进图补算、单图半径、启动集 PCK、本机烘焙缓存、IMapService、IMapAware、MapScope、MapDescriptor、map.json、map_rect

- SYS-MAPS §0 结论 · L21 · 1.3k
  - ★ SYS-MAPS §0.1 接入要点 · L32 · 780
- SYS-MAPS §1 数据归属 · L42 · 1.4k
  - SYS-MAPS §1.1 按图与全局 · L44 · 742
  - SYS-MAPS §1.2 本机烘焙缓存与启动集 · L62 · 624
- SYS-MAPS §2 地图描述 `map.json` · L71 · 704
- SYS-MAPS §3 切换 · L83 · 2.4k
  - SYS-MAPS §3.1 一个 World3D，先卸后装 · L85 · 151
  - SYS-MAPS §3.2 八步 · L89 · 1.2k
  - SYS-MAPS §3.3 规则 · L109 · 561
  - SYS-MAPS §3.4 入口 · L118 · 301
  - SYS-MAPS §3.5 同图重载门禁 · L127 · 153
- SYS-MAPS §4 世界状态：时间与季节全局，天气按图 · L131 · 850
- SYS-MAPS §5 相机、行走与安全点 · L151 · 433
- SYS-MAPS §6 归属与 `MapScope` · L162 · 600
- SYS-MAPS §7 全局参数 `map_rect` · L170 · 691
- SYS-MAPS §8 内存、包体与单图半径 · L180 · 332
- SYS-MAPS §9 以后的方向 · L186 · 609
- SYS-MAPS §10 待验证（P0） · L194 · 435

## SYS-MOTION · 动态与事件

`docs/systems/SYS-MOTION-动态与事件.md` · 12.6k 字 · 两个时钟与一拍二，风、悬浮物、流体、生命体与反应动物的节奏，唯一的偶发事件、季节氛围与转场的设计规则
关键词：动态、两个时钟、动画时钟、AnimTime、一拍二、step_rate、480 s 回绕、风场、阵风、IWindField、飘落池、萤火虫、解析运动、季节氛围、反应动物、偶发事件、晚归者、incidents.json、转场、钟锤

- SYS-MOTION §0 结论 · L22 · 1.2k
  - ★ SYS-MOTION §0.1 接入要点 · L32 · 582
- SYS-MOTION §1 时钟与计时 · L45 · 1.1k
  - SYS-MOTION §1.1 两个时钟 · L47 · 342
  - SYS-MOTION §1.2 计时规则 · L53 · 487
  - SYS-MOTION §1.3 各档流逝的表现 · L60 · 126
  - SYS-MOTION §1.4 镜头 · L64 · 106
- SYS-MOTION §2 节奏规则 · L68 · 670
- SYS-MOTION §3 风：一个起因带动一串变化 · L79 · 510
- SYS-MOTION §4 悬浮物与瞬时痕迹 · L87 · 647
  - SYS-MOTION §4.1 飘落物 · L89 · 408
  - SYS-MOTION §4.2 行走时的瞬时痕迹 · L102 · 224
- SYS-MOTION §5 流体 · L106 · 181
- SYS-MOTION §6 生命体 · L111 · 1.4k
  - SYS-MOTION §6.1 对行走身体作出反应的动物 · L126 · 686
- SYS-MOTION §7 光的缓慢变化 · L139 · 450
- SYS-MOTION §8 远景 · L148 · 461
- SYS-MOTION §9 室内与钟楼的微动 · L157 · 420
- SYS-MOTION §10 偶发事件（全片唯一） · L167 · 1.2k
- SYS-MOTION §11 季节氛围 · L179 · 762
- SYS-MOTION §12 转场 · L189 · 1.7k
- SYS-MOTION §13 粒子类与解析运动 · L207 · 657
- SYS-MOTION §14 待验证 · L217 · 244

## SYS-SAVE · 存档与持久化：存档模型、存什么、保存时机、加载与一致性、安全

`docs/systems/SYS-SAVE-存档与持久化.md` · 13.1k 字 · 存档位与世界线、存档段、保存时机、读档流程与一致性、损坏处理、设置文件与存档安全
关键词：存档、保存、读档、自动保存、存档位、世界线、存档段、段版本、存档迁移、金档、落定快照、安全时刻、续跑等价、世界实例、校验修复、存档损坏、单实例锁、设置文件安全、退出保存、ISaveService、ISaveSection、SaveReason、LoadScope、RestorePhase、IWorldState.Instance

- SYS-SAVE §0 结论 · L22 · 1.4k
  - ★ SYS-SAVE §0.1 接入要点 · L32 · 841
- SYS-SAVE §1 存档模型 · L45 · 676
- SYS-SAVE §2 存什么、不存什么 · L56 · 1.4k
- SYS-SAVE §3 格式与迁移 · L79 · 1.2k
  - SYS-SAVE §3.1 容器 · L81 · 418
  - SYS-SAVE §3.2 缩略图 · L92 · 102
  - SYS-SAVE §3.3 序列化约定 · L96 · 228
  - SYS-SAVE §3.4 版本与迁移 · L102 · 485
- SYS-SAVE §4 保存时机与可靠性 · L110 · 1.7k
  - SYS-SAVE §4.1 时机 · L112 · 578
  - SYS-SAVE §4.2 安全时刻与落定快照 · L127 · 353
  - SYS-SAVE §4.3 写盘与失败 · L133 · 105
  - SYS-SAVE §4.4 退出 · L137 · 448
  - SYS-SAVE §4.5 保存状态与失败反馈 · L147 · 235
- SYS-SAVE §5 加载与一致性 · L154 · 2.7k
  - SYS-SAVE §5.1 加载流程 · L156 · 876
  - SYS-SAVE §5.2 世界实例：`IWorldState.Instance` · L170 · 384
  - SYS-SAVE §5.3 "状态一致"的定义 · L176 · 342
  - SYS-SAVE §5.4 天气 · L184 · 218
  - SYS-SAVE §5.5 新游戏 · L188 · 378
  - SYS-SAVE §5.6 校验修复 · L195 · 464
- SYS-SAVE §6 关着游戏的那段现实时间不计 · L210 · 272
- SYS-SAVE §7 存档损坏与版本不兼容 · L217 · 585
- SYS-SAVE §8 设置类文件只接受基本类型 · L231 · 624
- SYS-SAVE §9 安全：为什么禁止用 Resource 和 ConfigFile 存档 · L244 · 650
- SYS-SAVE §10 待验证（P0） · L255 · 180

## SYS-TIME · 时间与季节系统：世界状态、美术日月、色彩脚本、过渡

`docs/systems/SYS-TIME-时间与季节.md` · 12.0k 字 · 世界状态三个量与只向前的时间规则、默认状态落金初灯、美术日月、色彩脚本的组织与情绪、四季内容、改时间换季同时转场、灯光时刻表的设计意图
关键词：世界状态、钟点、季节、世界时间、世界实例、流逝倍率、落金初灯、默认状态、只向前、美术太阳、美术月亮、满月、色彩脚本、关键帧、英雄帧、四季内容、阈值生长、换季转场、改时间、同时转场、灯光时刻表、有效太阳高度、初灯组

- SYS-TIME §0 结论 · L22 · 1.2k
  - ★ SYS-TIME §0.1 接入要点 · L34 · 623
- SYS-TIME §1 世界状态与时间规则 · L45 · 1.6k
  - SYS-TIME §1.1 默认状态 · L47 · 460
  - SYS-TIME §1.2 三个量与时间规则 · L55 · 1.1k
- SYS-TIME §2 美术太阳与月亮 · L70 · 827
  - SYS-TIME §2.1 太阳 · L72 · 518
  - SYS-TIME §2.2 月亮 · L87 · 201
  - SYS-TIME §2.3 与主光的关系 · L93 · 94
- SYS-TIME §3 色彩脚本 · L97 · 2.4k
  - SYS-TIME §3.1 组织方式 · L99 · 664
  - SYS-TIME §3.2 秋季基准关键帧的情绪 · L108 · 636
  - SYS-TIME §3.3 四季修正与专属覆盖 · L129 · 382
  - SYS-TIME §3.4 难看时段的改造 · L144 · 254
  - SYS-TIME §3.5 英雄帧 · L152 · 421
- SYS-TIME §4 四季内容 · L170 · 1.4k
- SYS-TIME §5 过渡 · L196 · 1.4k
  - SYS-TIME §5.1 换季转场 · L198 · 727
  - SYS-TIME §5.2 改时间 · L214 · 193
  - SYS-TIME §5.3 换天气 · L218 · 96
  - SYS-TIME §5.4 同时转场 · L222 · 244
  - SYS-TIME §5.5 减少动态效果 · L230 · 97
- SYS-TIME §6 灯光与生活痕迹的时刻表 · L234 · 1.1k
- SYS-TIME §7 与动态、偶发事件的协调 · L257 · 300
- SYS-TIME §8 质量底线 · L262 · 536

## SYS-TITLE · 开始页与进入过场：扉页、开始页、新游戏与继续过场、跳过

`docs/systems/SYS-TITLE-开始页与进入过场.md` · 12.9k 字 · 冷启动扉页、开始页 T0 与菜单、新游戏与继续过场、返回标题、跳过与安全落点、各应用状态下的约定、云卡的规则
关键词：开始页、标题页、主菜单、扉页、启动失败页、T0、云隙望穹、片名块、新游戏过场、继续过场、返回标题、跳过过场、安全落点、开场过场设置、过场专属元素、云卡、放映层、标题卡、应用状态

- SYS-TITLE §0 结论 · L22 · 1.2k
  - ★ SYS-TITLE §0.1 接入要点 · L32 · 659
- SYS-TITLE §1 启动序列与扉页 · L43 · 1.4k
- SYS-TITLE §2 开始页 · L70 · 1.8k
  - SYS-TITLE §2.1 T0 镜头"云隙望穹" · L72 · 576
  - SYS-TITLE §2.2 菜单 · L82 · 491
  - SYS-TITLE §2.3 片名块 · L90 · 384
  - SYS-TITLE §2.4 首次启动与再次启动 · L98 · 313
- SYS-TITLE §3 新游戏过场 · L105 · 2.1k
  - SYS-TITLE §3.1 分镜 · L107 · 1.1k
  - SYS-TITLE §3.2 字幕、时间读数与标题卡 · L125 · 415
  - SYS-TITLE §3.3 过场专属元素 · L134 · 380
  - SYS-TITLE §3.4 简短版 · L144 · 132
- SYS-TITLE §4 继续过场与返回标题 · L148 · 911
  - SYS-TITLE §4.1 继续（约 9 s） · L150 · 634
  - SYS-TITLE §4.2 返回标题（约 3 s，不可跳过） · L160 · 261
- SYS-TITLE §5 跳过、安全落点与设置项 · L164 · 717
- SYS-TITLE §6 与时间不可逆的关系 · L181 · 458
- SYS-TITLE §7 应用状态下的约定 · L188 · 812
- SYS-TITLE §8 过场的规则 · L204 · 646
- SYS-TITLE §9 云卡 · L212 · 335
- SYS-TITLE §10 流式预取 · L218 · 370
- SYS-TITLE §11 多地图 · L224 · 312
- SYS-TITLE §12 不做的事 · L230 · 232
- SYS-TITLE §13 待验证 · L236 · 217

## SYS-VIEW · 观察模式、介质与开场

`docs/systems/SYS-VIEW-观察模式介质与开场.md` · 12.3k 字 · 观察模式总述与旁观漫游、行走的第一与第三人称镜头、镜头在楼里地下水下雾海的介质呈现、3D 与平面地图的分工、署名与许可
关键词：旁观模式、观察模式、飞行速度档、视场角、鼠标灵敏度、活动范围、初始位置、第一人称、第三人称、镜头不会自己动、自动运镜、命令行参数、介质呈现、剖切变体、楼里、地下、水下、半入水、雾海、ICameraRig、IMediumState、OSM 署名、LICENSES.txt

- SYS-VIEW §0 结论 · L22 · 1.5k
  - ★ SYS-VIEW §0.1 接入要点 · L35 · 613
- SYS-VIEW §1 不做的事 · L47 · 199
- SYS-VIEW §2 旁观者漫游 · L51 · 5.0k
  - SYS-VIEW §2.1 操作 · L55 · 826
  - SYS-VIEW §2.2 触屏 · L75 · 27
  - SYS-VIEW §2.3 活动范围 · L79 · 952
  - SYS-VIEW §2.4 初始位置 · L93 · 605
  - SYS-VIEW §2.5 镜头不会自己动 · L103 · 762
  - SYS-VIEW §2.6 操作说明与提示 · L116 · 250
  - SYS-VIEW §2.7 行走模式的镜头 · L125 · 1.4k
- SYS-VIEW §3 启动与开场 · L158 · 39
- SYS-VIEW §4 环境痕迹与平面地图的分工 · L162 · 477
- SYS-VIEW §5 介质呈现：镜头在楼里、地下、水下、雾海里时 · L173 · 2.4k
  - SYS-VIEW §5.1 楼里 · L183 · 732
  - SYS-VIEW §5.2 地下 · L193 · 262
  - SYS-VIEW §5.3 水下 · L201 · 567
  - SYS-VIEW §5.4 雾海 · L210 · 167
- SYS-VIEW §6 署名、窗口标题与图标 · L214 · 912
- SYS-VIEW §7 待验证 · L223 · 387

## SYS-WALK · 行走模式：手感、可行走范围、边界与碰撞

`docs/systems/SYS-WALK-行走模式.md` · 13.2k 字 · 行走模式的手感目标、玩家身体、游泳、骑行、模式切换、可行走范围与边界、开放室内的门与电梯，以及控制器与碰撞的规则
关键词：行走模式、游泳、手感、模式切换、可行走范围、可行走遮罩、边界收口、隐形墙、校界缓停、软墙提示、安全点、回位、涉水、玩家身体、开放泳区、碰撞就绪、自动登阶、自动门、电梯关门换位、推荐路线、步相、骑行、共享单车、SurfaceTag、FootMat

- SYS-WALK §0 结论 · L22 · 885
  - ★ SYS-WALK §0.1 接入要点 · L34 · 411
- SYS-WALK §1 身体、声音与步相 · L44 · 724
- SYS-WALK §2 手感目标 · L55 · 699
- SYS-WALK §3 视角、舒适度与键位 · L73 · 556
- SYS-WALK §4 模式切换 · L83 · 829
- SYS-WALK §5 可行走范围 · L98 · 868
- SYS-WALK §6 边界收口 · L117 · 1.2k
- SYS-WALK §7 楼内的门与电梯 · L145 · 575
- SYS-WALK §8 水、游泳、回位与安全点 · L159 · 997
- SYS-WALK §9 行走走廊与节奏 · L183 · 300
- SYS-WALK §10 推荐路线 · L191 · 356
- SYS-WALK §11 控制器的规则 · L198 · 767
- SYS-WALK §12 碰撞与就绪 · L209 · 1.3k
- SYS-WALK §13 骑行 · L236 · 1.2k
- SYS-WALK §14 待验证 · L265 · 168

## SYS-WEATHER · 天气：天气清单、画面规则、前向随机过程、过渡

`docs/systems/SYS-WEATHER-天气.md` · 11.2k 字 · 八格天气与季节合法性、每种天气的画面与声音规则、雨的可读性、闪电彩虹的条件与安全规则、自动天气的前向随机过程与固定点、湿润积水降水的规则、质量底线
关键词：天气、天气格、自动天气、手动天气、天气向量、前向随机过程、前瞻队列、天气种子、天气修正量、太阳雨、雨的可读性、闪电、彩虹、降水池、湿润、积水、天光方向、钉住天气、保持晴、天气巡回

- SYS-WEATHER §0 结论 · L22 · 1.3k
  - ★ SYS-WEATHER §0.1 接入要点 · L31 · 773
- SYS-WEATHER §1 天气清单与界面格 · L45 · 619
- SYS-WEATHER §2 每种天气的画面规则 · L62 · 2.1k
  - SYS-WEATHER §2.1 天气的声音 · L87 · 424
- SYS-WEATHER §3 雨的可读性规则 · L104 · 554
- SYS-WEATHER §4 与四季、昼夜的配合 · L116 · 247
- SYS-WEATHER §5 闪电与彩虹 · L124 · 559
- SYS-WEATHER §6 自动天气：前向随机过程 · L135 · 2.2k
  - SYS-WEATHER §6.1 模型 · L137 · 292
  - SYS-WEATHER §6.2 硬规则（生成时满足） · L143 · 244
  - SYS-WEATHER §6.3 各季的气候风味 · L150 · 392
  - SYS-WEATHER §6.4 随机数与复现 · L161 · 393
  - SYS-WEATHER §6.5 时间与天气 · L167 · 533
  - SYS-WEATHER §6.6 固定点 · L177 · 367
- SYS-WEATHER §7 湿润、积水与降水的规则 · L184 · 1.0k
- SYS-WEATHER §8 状态与存档语义 · L201 · 426
- SYS-WEATHER §9 质量底线 · L208 · 485
- SYS-WEATHER §10 待验证 · L217 · 129

## UI-NAV · 界面、点位跳转与操作说明

`docs/ui/UI-NAV-界面与操作说明.md` · 10.7k 字 · 界面有什么、怎么操作：界面态与漫游态、组件的内容与交互、输入优先级与关键键位、点位设计意图、飞过去与传送的规则、无障碍与文本约定
关键词：界面交互、快捷键、输入优先级、界面态、漫游态、自动隐藏、时间环操作、天气签条、速度档、临时提示、到达标题、设置项、点位、waypoints.json、飞过去、传送、无障碍、读屏、文本键、地名来源

- UI-NAV §0 结论 · L21 · 1.1k
  - ★ UI-NAV §0.1 接入要点 · L31 · 590
- UI-NAV §1 允许什么、禁止什么 · L40 · 375
- UI-NAV §2 界面态、漫游态与隐藏 · L49 · 610
- UI-NAV §3 组件的内容与交互 · L62 · 3.0k
  - UI-NAV §3.1 时间环 · L64 · 558
  - UI-NAV §3.2 季节、速度与观察模式 · L73 · 409
  - UI-NAV §3.3 点位目录 · L79 · 214
  - UI-NAV §3.4 首次进入与到达标题 · L83 · 366
  - UI-NAV §3.5 操作说明与设置 · L88 · 588
  - UI-NAV §3.6 临时提示 · L96 · 405
  - UI-NAV §3.7 天气签条 · L104 · 472
- UI-NAV §4 输入 · L114 · 1.4k
  - UI-NAV §4.1 Esc 与单键快捷键 · L116 · 318
  - UI-NAV §4.2 关键键位 · L122 · 569
  - UI-NAV §4.3 输入优先级 · L140 · 514
- UI-NAV §5 点位 · L154 · 783
- UI-NAV §6 跳转与传送 · L165 · 1.3k
  - UI-NAV §6.1 飞过去 · L167 · 255
  - UI-NAV §6.2 到达与打断 · L173 · 253
  - UI-NAV §6.3 预取与速度律豁免 · L178 · 215
  - UI-NAV §6.4 传送与减少动态效果 · L182 · 591
- UI-NAV §7 无障碍与语言 · L192 · 771
- UI-NAV §8 待验证 · L205 · 231

## UI-VISUAL · 界面视觉语言：令牌、字体、形状、材质、图标、组件、画面布局与动效

`docs/ui/UI-VISUAL-界面视觉语言.md` · 12.5k 字 · 界面外观规范：设计原则、色彩分工与令牌、晶白与深屏两套主题、字体、形状材质、图标、组件外观、画面布局、动效语言与减少动态效果、实现边界
关键词：界面视觉、色彩令牌、清蓝、晶白、深屏、昼夜交班、常驻件、界面字体、切角、发丝线、过孔灯、衍射光边、窗口图标、动效母题、减少动态效果、辉光、等待灯、ui.json、ui_metrics.json、ui_night、IMotionSettings、IPaletteProbe

- UI-VISUAL §0 结论 · L21 · 868
  - ★ UI-VISUAL §0.1 接入要点 · L29 · 422
- UI-VISUAL §1 设计原则 · L36 · 556
- UI-VISUAL §2 色彩分工与常驻件 · L52 · 1.0k
  - UI-VISUAL §2.1 场景暖，界面冷 · L54 · 377
  - UI-VISUAL §2.2 与场景色不冲突的规则 · L63 · 234
  - UI-VISUAL §2.3 高调画面上的面板与常驻件 · L69 · 409
- UI-VISUAL §3 令牌 · L77 · 1.6k
  - UI-VISUAL §3.1 清蓝阶梯 · L81 · 180
  - UI-VISUAL §3.2 中性、专用与语义色 · L85 · 245
  - UI-VISUAL §3.3 两套主题的语义令牌 · L90 · 248
  - UI-VISUAL §3.4 昼夜切换 · L96 · 427
  - UI-VISUAL §3.5 对比度规则 · L104 · 243
  - UI-VISUAL §3.6 登记 · L111 · 150
- UI-VISUAL §4 字体与字号 · L115 · 400
- UI-VISUAL §5 形状与材质 · L121 · 824
- UI-VISUAL §6 图标体系 · L131 · 540
  - UI-VISUAL §6.1 规格 · L133 · 163
  - UI-VISUAL §6.2 意象 · L137 · 238
  - UI-VISUAL §6.3 窗口图标"八角晶片" · L144 · 128
- UI-VISUAL §7 组件外观 · L148 · 1.6k
  - UI-VISUAL §7.1 六种状态（"通电"隐喻） · L150 · 216
  - UI-VISUAL §7.2 控件与容器 · L155 · 320
  - UI-VISUAL §7.3 时间环 · L161 · 351
  - UI-VISUAL §7.4 天气签条、速度条与到达标题 · L170 · 306
  - UI-VISUAL §7.5 存档卡"存储模组"与等待灯 · L176 · 379
- UI-VISUAL §8 画面布局 · L183 · 611
- UI-VISUAL §9 动效语言 · L195 · 1.8k
  - UI-VISUAL §9.1 原则与两种时间 · L197 · 321
  - UI-VISUAL §9.2 八个母题 · L205 · 666
  - UI-VISUAL §9.3 频率分级与声音 · L218 · 338
  - UI-VISUAL §9.4 曲线、时长与上限 · L224 · 422
- UI-VISUAL §10 减少动态效果与无障碍 · L230 · 471
  - UI-VISUAL §10.1 "减少动态效果"下的替代 · L232 · 327
  - UI-VISUAL §10.2 无障碍 · L240 · 126
- UI-VISUAL §11 实现边界 · L244 · 886
- UI-VISUAL §12 待验证 · L253 · 162

## ENG-ARCH · 工程架构

`docs/engineering/ENG-ARCH-工程架构.md` · 9.8k 字 · 当前分层、依赖方向与唯一生产链，边界、确定性、契约、烘焙与桩的架构准则，构建排队与后续导向；实现细节以代码为准
关键词：工程架构、分层、依赖方向、程序集边界、唯一生产链、KernelStage、组合根、数据流、自动发现、特性注册、ContentModule、KernelService、KernelNode、Autoload、原生资源创建方、确定性、DetMath、StableHash、Rng、BannedApiAnalyzers、架构规则、生命周期、契约、契约申请、枚举穷举、本机烘焙、.tbin、缓存键、启动集、英雄资产、桩、占位、构建与测试排队、构建锁、架构导向、扩展方式、net10.0、技术栈

- ENG-ARCH §0 结论 · L21 · 1.4k
  - ★ ENG-ARCH §0.1 接入要点 · L33 · 821
- ENG-ARCH §1 当前架构 · L46 · 1.2k
  - ENG-ARCH 技术栈 · L48 · 185
  - ENG-ARCH 分层 · L52 · 568
  - ENG-ARCH 生产链 · L64 · 265
  - ENG-ARCH 数据流 · L72 · 125
- ENG-ARCH §2 边界与确定性 · L76 · 1.7k
  - ENG-ARCH 依赖方向 · L78 · 616
  - ENG-ARCH 确定性与禁用写法 · L86 · 544
  - ENG-ARCH 自动发现与模块接入 · L98 · 340
  - ENG-ARCH 生命周期与发布 · L105 · 233
- ENG-ARCH §3 契约 · L112 · 551
- ENG-ARCH §4 资源与烘焙 · L121 · 942
- ENG-ARCH §5 未交付能力的占位边界 · L132 · 359
- ENG-ARCH §6 构建与测试排队 · L141 · 871
- ENG-ARCH §7 架构导向 · L151 · 514
- ENG-ARCH §8 扩展点（RPG） · L163 · 448
- ENG-ARCH §9 待验证（P0） · L175 · 583

## ENG-BUDGET · 性能预算与硬规则

`docs/engineering/ENG-BUDGET-性能预算与硬规则.md` · 12.7k 字 · 额度与硬规则的数值家：设备档与预设的预算口径、细节级别与分块、速度律、深度与阴影要点、23 条推送硬规则；逐包总账在 budgets.json
关键词：性能预算、额度、额度总账、budgets.json、主代理余量、预设资源上限、设备档目标、帧率目标、最低配置、独显中、独显高、楼内视角额度、GPU 帧时分配、硬规则、推送规则、细节级别、L0+、分块、四叉树、速度律、屏幕误差、近裁剪面、工程设置

- ENG-BUDGET §0 结论 · L21 · 1.4k
  - ★ ENG-BUDGET §0.1 接入要点 · L34 · 691
- ENG-BUDGET §1 总体要点 · L46 · 388
- ENG-BUDGET §2 预设与设备档的预算 · L53 · 1.8k
- ENG-BUDGET §3 细节级别与分块 · L84 · 1.2k
- ENG-BUDGET §4 流式加载与速度 · L107 · 460
- ENG-BUDGET §5 深度与近裁剪面 · L114 · 380
- ENG-BUDGET §6 阴影、后期、倒影、灯光 · L122 · 855
- ENG-BUDGET §7 硬规则（违反任何一条都不能推送） · L134 · 3.5k
  - ENG-BUDGET §7.1 渲染、材质与时间（第 1–10 条） · L136 · 1.5k
  - ENG-BUDGET §7.2 烘焙、线程、存档与音频（第 11–23 条） · L149 · 2.0k
- ENG-BUDGET §8 额度总账（`budgets.json`） · L165 · 349
- ENG-BUDGET §9 怎么测 · L173 · 276
- ENG-BUDGET §10 工程设置 · L177 · 733
- ENG-BUDGET §11 待验证（P0） · L189 · 351

## ENG-CODE · 代码结构准则

`docs/engineering/ENG-CODE-代码结构准则.md` · 3.1k 字 · 写代码时的结构规矩：依赖方向落地、规则值与数据各一份、先复用、不留死代码、控制规模、测试代码写法
关键词：代码结构准则、软件工程、坏味道、屎山、依赖方向、内核引用系统、生成层实现类、点名模块、单一来源、魔法数、规则值、规则类、数据与代码分离、Tune 只放规则值、实体 ID 写数据、复用、公共层、复制粘贴、死代码、死桩、诊断残留、单实现接口、吞错、兜底、文件规模、构造参数、清单文件、程序集过碎、测试写法、测试夹具、假实现、性质断言、钉哈希、输入哈希、提交后提醒

- ENG-CODE §0 结论 · L22 · 586
  - ★ ENG-CODE §0.1 接入要点 · L31 · 325
- ENG-CODE §1 依赖方向 · L40 · 351
- ENG-CODE §2 规则值与数据各一份 · L49 · 401
- ENG-CODE §3 先复用，不复制 · L58 · 158
- ENG-CODE §4 不留死代码 · L64 · 203
- ENG-CODE §5 规模 · L71 · 160
- ENG-CODE §6 测试代码怎么写 · L77 · 351

## ENG-GATES · 验收：门禁、审查视角、近景巡检

`docs/engineering/ENG-GATES-验收与门禁.md` · 12.7k 字 · 测试三档、门禁 G1–G21、审查视角与截图方式、一票否决、收尾清单
关键词：门禁、验收、测试三档、快检、核心专项、集中测试、gate.ps1、G1–G21、审查视角、截图默认串、截图方式、截图就绪、流式屏障、录像、时季抽样、巡检点轮换、一票否决、全局清单、review.json、构建锁

- ENG-GATES §0 结论 · L21 · 681
  - ★ ENG-GATES §0.1 接入要点 · L29 · 350
- ENG-GATES §1 审查视角 · L37 · 4.2k
  - ENG-GATES §1.1 命令行规则与截图默认串 · L41 · 757
  - ENG-GATES §1.2 截图方式 · L56 · 594
  - ENG-GATES §1.3 视角表 · L69 · 1.6k
  - ENG-GATES §1.4 路线与点位 · L97 · 660
  - ENG-GATES §1.5 时季抽样 · L106 · 524
- ENG-GATES §2 门禁与测试节奏 · L115 · 4.1k
  - ENG-GATES §2.1 测试三档 · L119 · 926
  - ENG-GATES §2.2 门禁 G1–G10 · L136 · 1.5k
  - ENG-GATES §2.3 门禁 G11–G21 · L151 · 1.4k
  - ENG-GATES §2.4 入口与交付分工 · L167 · 206
- ENG-GATES §3 一票否决 · L173 · 1.6k
- ENG-GATES §4 全局清单 · L202 · 552
- ENG-GATES §5 待验证（P0） · L215 · 490

## ENG-PERF · 性能工程：策略与原则（Godot 4.7.2 .NET）

`docs/engineering/ENG-PERF-性能工程.md` · 12.9k 字 · 性能策略与原则：容器与合批、着色器白名单与全局参数规则、预热、剔除、流式加载与本机烘焙、主线程、植被与阴影后期策略、自适应、测量思路；数值与硬规则在 ENG-BUDGET
关键词：着色器白名单、全局着色器参数、ShaderGlobals、合批、自动实例化、MultiMesh、管线预热、抖动淡化、遮挡剔除、渲染图层、TileBundle、流式加载、本机烘焙缓存、零分配、PSSM 阴影、方向量化、灯光通道、后期链、描线、自动调节画质、性能基准

- ENG-PERF §0 总原则 · L21 · 1.0k
  - ★ ENG-PERF §0.1 接入要点 · L32 · 434
- ENG-PERF §1 绘制管线与合批 · L42 · 1.7k
  - ENG-PERF §1.1 容器选型 · L44 · 344
  - ENG-PERF §1.2 MultiMesh、网格 LOD 与级别过渡 · L51 · 384
  - ENG-PERF §1.3 调用口径 · L57 · 122
  - ENG-PERF §1.4 着色器白名单 · L61 · 430
  - ENG-PERF §1.5 全局参数与数据纹理 · L70 · 447
- ENG-PERF §2 着色器与编译 · L79 · 607
- ENG-PERF §3 填充率与带宽 · L87 · 271
- ENG-PERF §4 剔除 · L93 · 494
- ENG-PERF §5 流式加载 · L99 · 1.5k
  - ENG-PERF §5.1 本机烘焙、缓存与加载 · L101 · 644
  - ENG-PERF §5.2 帧内请求、挂载与六条不变量 · L109 · 468
  - ENG-PERF §5.3 碰撞级与室内包 · L122 · 160
  - ENG-PERF §5.4 外围与目的地预取 · L126 · 188
- ENG-PERF §6 主线程与 C# · L130 · 325
- ENG-PERF §7 内存与显存 · L136 · 254
- ENG-PERF §8 安装包与磁盘布局 · L140 · 180
- ENG-PERF §9 植被 · L144 · 711
- ENG-PERF §10 阴影、光照与后期 · L153 · 2.0k
  - ENG-PERF §10.1 阴影 · L155 · 746
  - ENG-PERF §10.2 后期链 · L164 · 663
  - ENG-PERF §10.3 窗灯与路灯 · L173 · 360
  - ENG-PERF §10.4 倒影 · L179 · 205
- ENG-PERF §11 设备档、画面预设与自适应 · L183 · 1.2k
  - ENG-PERF §11.1 设备档与首次分档 · L185 · 189
  - ENG-PERF §11.2 运行时自适应 · L189 · 623
  - ENG-PERF §11.3 帧节奏与画面设置约束 · L197 · 322
- ENG-PERF §12 测量、剖析与门禁 · L202 · 754
- ENG-PERF §13 D3D12 与 Vulkan 的选择 · L210 · 221
- ENG-PERF §14 待验证（P0） · L214 · 343

## ENG-ROADMAP · 扩展路线：从观光到 RPG

`docs/engineering/ENG-ROADMAP-扩展路线.md` · 10.8k 字 · 规定观光版给以后 RPG 留的扩展点、现在不能写死的地方和 v1 冻结前的最小预留
关键词：扩展路线、从观光到 RPG、扩展点、扩展原则、规则集、sightseeing、观光规则集、观者状态、输入占用按原因计、枚举的穷举纪律、偶发事件数据化、名字占位、RPG 预留目录、实体 ID 前缀、存档预留段名、v1 冻结前的预留、写法纪律、RPG 子系统挂点、VRM、必需段组

- ENG-ROADMAP §0 结论 · L21 · 1.2k
  - ★ ENG-ROADMAP §0.1 接入要点 · L32 · 590
- ENG-ROADMAP §1 扩展原则 · L43 · 652
- ENG-ROADMAP §2 现有扩展点 · L55 · 1.7k
- ENG-ROADMAP §3 现在写死、以后会挡路的地方 · L78 · 1.9k
- ENG-ROADMAP §4 RPG 可能新增的子系统与挂点 · L110 · 1.4k
- ENG-ROADMAP §5 预算与性能余量 · L130 · 406
- ENG-ROADMAP §6 v1 冻结前的预留 · L140 · 1.8k
  - ENG-ROADMAP §6.1 清单 · L142 · 1.5k
  - ENG-ROADMAP §6.2 不需要现在预留的 · L153 · 249
- ENG-ROADMAP §7 RPG 开工前待办 · L157 · 782

## ENG-WP · 工作包派工单：56 个工作包，可并行 55 个

`docs/engineering/ENG-WP-工作包派工单.md` · 16.4k 字 · 56 个工作包的规格文档、依赖与阶段，开发阶段与闸口、开工波次与推送顺序，各包主线版范围，细节待办与候选包，主代理的工程维护事项
关键词：工作包、派工单、派工、开工波次、推送顺序、依赖、主线版、主线可见结果、后置细节、细节阶段待办、候选包、阶段与闸口、主线闸口、D0、阶段名 P0–P3、拆包、子包、主代理工程维护、开放室内清单审批、插件清单、ownership.json、third_party.json、目标框架

- ENG-WP §0 结论 · L24 · 954
  - ★ ENG-WP §0.1 接入要点 · L35 · 409
- ENG-WP §1 阶段与闸口 · L44 · 2.4k
  - ENG-WP §1.1 开工波次与推送顺序 · L66 · 994
- ENG-WP §2 工作包一览 · L82 · 8.8k
  - ENG-WP §2.1 三个插入项 · L150 · 455
  - ENG-WP §2.2 细节阶段待办 · L156 · 1.7k
  - ENG-WP §2.3 候选包 · L182 · 246
- ENG-WP §3 子包拆法 · L190 · 1.4k
- ENG-WP §4 主代理的工程维护事项 · L211 · 1.2k
- ENG-WP §5 待验证（P0） · L222 · 446

## TOOL-PREVIEW · 内容预览

`docs/tools/TOOL-PREVIEW-内容预览.md` · 3.2k 字 · 开发构建里预览与回看内容：按实体定位、保留机位与时季天气、复制机位、重读数据
关键词：内容预览、预览、回看、开发构建、--focus、--pose-file、按实体定位、实体 ID、机位、复制机位、机位文件、last-pose.json、F5 重读数据、F6 复制机位、F7 重启、重启后恢复机位、预览只读不写

- TOOL-PREVIEW §0 结论 · L21 · 936
  - ★ TOOL-PREVIEW §0.1 接入要点 · L28 · 630
- TOOL-PREVIEW §1 启动 · L39 · 345
- TOOL-PREVIEW §2 按实体 ID 定位 · L47 · 262
- TOOL-PREVIEW §3 记下机位 · L53 · 257
- TOOL-PREVIEW §4 改了以后回看 · L59 · 645

## OPS-RELEASE · 发布与交付

`docs/ops/OPS-RELEASE-发布与交付.md` · 5.6k 字 · 版本号与构建信息、正式用户目录、导出与发布、安装更新卸载、地图数据包、故障诊断和许可
关键词：发布、交付、版本号、version.txt、构建信息、导出、导出入口、scripts/release、分发压缩包、UESTC-<版本>-win64.7z、地图数据包、map-data-<版本>.zip、ODbL、正式用户目录、%APPDATA%\UESTC、安装、更新、卸载、诊断包、导出诊断、许可、MIT、LICENSE、干净环境验证

- OPS-RELEASE §0 结论 · L21 · 1.1k
  - ★ OPS-RELEASE §0.1 接入要点 · L30 · 670
- OPS-RELEASE §1 版本号与构建信息 · L42 · 261
- OPS-RELEASE §2 用户数据目录 · L49 · 253
- OPS-RELEASE §3 导出与发布 · L55 · 619
- OPS-RELEASE §4 安装、更新与卸载 · L64 · 501
- OPS-RELEASE §5 地图数据包（ODbL） · L76 · 501
- OPS-RELEASE §6 故障处理与诊断 · L84 · 527
- OPS-RELEASE §7 许可 · L91 · 865
