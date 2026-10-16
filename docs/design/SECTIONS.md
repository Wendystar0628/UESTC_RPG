# 小节目录

> 由 `scripts/docs/gen_index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（ENG-ONBOARD §4.1）。行号以生成时为准，原文改过以后可能差几行，以标题文字为准。

## CORE-INDEX · 《电光年华》开发文档总索引

`docs/design/README.md` · 43.6k 字 · 作品定位、当前范围、全局不变量 INV-1–INV-12、坐标与数据约定、阅读路径、文档地图与依赖
关键词：总索引、作品定位、电光年华、当前范围、主线、全局不变量、INV、坐标约定、阅读路径、文档地图、工作包矩阵、依赖图、清水河

- CORE-INDEX §0 结论 · L23 · 667
  - ★ CORE-INDEX §0.1 接入要点 · L30 · 204
- CORE-INDEX §1 作品定位 · L37 · 2.6k
- CORE-INDEX §2 当前范围 · L72 · 1.2k
- CORE-INDEX §3 全局不变量 · L97 · 4.0k
- CORE-INDEX §4 坐标与数据约定 · L116 · 1.3k
- CORE-INDEX §5 文档怎么组织 · L135 · 764
- CORE-INDEX §6 阅读路径 · L145 · 1.1k
- CORE-INDEX §7 文档地图 · L163 · 9.6k
  - CORE-INDEX §7.1 共识层 `core/` · L165 · 381
  - CORE-INDEX §7.2 美术方向 `art/` · L173 · 925
  - CORE-INDEX §7.3 世界内容（通用） `world/` · L184 · 1.2k
  - CORE-INDEX §7.4 地图：清水河 `maps/qingshuihe/` · L196 · 1.7k
  - CORE-INDEX §7.5 运行时系统 `systems/` · L210 · 1.8k
  - CORE-INDEX §7.6 界面 `ui/` · L226 · 335
  - CORE-INDEX §7.7 工程 `engineering/` · L233 · 1.4k
  - CORE-INDEX §7.8 参考 `reference/` · L247 · 171
  - CORE-INDEX §7.9 工具 `tools/` · L253 · 178
  - CORE-INDEX §7.10 运维 `ops/` · L259 · 173
  - CORE-INDEX §7.11 提案 `proposals/` · L265 · 420
  - CORE-INDEX §7.12 待用的文件夹 · L274 · 896
- CORE-INDEX §8 工作包与文档 · L290 · 9.4k
- CORE-INDEX §9 文档依赖图 · L368 · 12.1k

## CORE-CONVENTIONS · 约定

`docs/design/core/CORE-CONVENTIONS.md` · 36.7k 字 · 跨文档共同约定速查：坐标单位、时间、编号命名、帧顺序、目录归属、确定性、文档写法与施工规则
关键词：约定、坐标系、单位、角度、编号体系、命名、程序集、命名空间、色名、帧顺序、FramePriority、目录归属、ownership.json、user://、确定性、文件头、front matter、keywords、范围块、查阅行、接入要点、小节引用、施工约定、文件操作、删除规则

- CORE-CONVENTIONS §0 原则与权威表 · L23 · 1.1k
  - ★ CORE-CONVENTIONS §0.1 接入要点 · L45 · 372
- CORE-CONVENTIONS §1 坐标与单位 · L56 · 2.8k
  - CORE-CONVENTIONS §1.1 坐标系 · L58 · 604
  - CORE-CONVENTIONS §1.2 角度 · L65 · 531
  - CORE-CONVENTIONS §1.3 范围数（清水河） · L73 · 1.3k
  - CORE-CONVENTIONS §1.4 其他单位 · L88 · 370
- CORE-CONVENTIONS §2 时间单位 · L97 · 2.3k
- CORE-CONVENTIONS §3 标识与命名 · L120 · 10.4k
  - CORE-CONVENTIONS §3.1 地图、实体与覆盖层 · L122 · 1.3k
  - CORE-CONVENTIONS §3.2 编号体系 · L136 · 1.6k
  - CORE-CONVENTIONS §3.3 同号不同义的读法 · L163 · 885
  - CORE-CONVENTIONS §3.4 C# 程序集、命名空间与文件 · L176 · 2.6k
  - CORE-CONVENTIONS §3.5 色名 · L201 · 799
  - CORE-CONVENTIONS §3.6 着色器文件 · L210 · 1.2k
  - CORE-CONVENTIONS §3.7 其他命名 · L237 · 495
  - CORE-CONVENTIONS §3.8 枚举与扩展纪律 · L244 · 1.0k
  - CORE-CONVENTIONS §3.9 持久化 ID 的稳定 · L256 · 525
- CORE-CONVENTIONS §4 帧顺序 · L265 · 1.9k
- CORE-CONVENTIONS §5 目录与归属 · L294 · 5.5k
  - CORE-CONVENTIONS §5.1 仓库 · L296 · 2.2k
  - CORE-CONVENTIONS §5.2 按图的数据 · L324 · 1.6k
  - CORE-CONVENTIONS §5.3 `user://` 下的文件 · L354 · 1.4k
  - CORE-CONVENTIONS §5.4 文档目录 · L370 · 278
- CORE-CONVENTIONS §6 确定性 · L376 · 2.2k
- CORE-CONVENTIONS §7 文档约定 · L400 · 5.4k
  - CORE-CONVENTIONS §7.1 文件头 · L402 · 1.2k
  - CORE-CONVENTIONS §7.2 标题与范围块 · L442 · 727
  - CORE-CONVENTIONS §7.3 编码、小节编号与引用 · L461 · 1.5k
  - CORE-CONVENTIONS §7.4 正文写法 · L492 · 372
  - CORE-CONVENTIONS §7.5 待验证（P0）节 · L500 · 172
  - CORE-CONVENTIONS §7.6 冲突与决定层级 · L507 · 663
  - CORE-CONVENTIONS §7.7 新增文档与小节 · L521 · 438
  - CORE-CONVENTIONS §7.8 登记新决策 · L527 · 310
- CORE-CONVENTIONS §8 子代理施工约定 · L533 · 3.5k
  - CORE-CONVENTIONS §8.1 开工 · L535 · 263
  - CORE-CONVENTIONS §8.2 改什么 · L540 · 1.0k
  - CORE-CONVENTIONS §8.3 门禁与回报 · L555 · 684
  - CORE-CONVENTIONS §8.4 文件操作 · L562 · 755
  - CORE-CONVENTIONS §8.5 主代理的角色 · L577 · 478
  - CORE-CONVENTIONS §8.6 主线优先（D-168） · L584 · 307
- CORE-CONVENTIONS §9 待对齐 · L592 · 48

## CORE-DECISIONS · 决策登记

`docs/design/core/CORE-DECISIONS.md` · 28.5k 字 · 全项目已定决策的登记：每条一句结论，附适用范围、细则所在和状态
关键词：决策、已定、D 编号、决策登记、生效、待原型验证、细则所在、推翻决策、ADR

- CORE-DECISIONS §0 用法 · L24 · 400
  - ★ CORE-DECISIONS §0.1 接入要点 · L32 · 172
- CORE-DECISIONS §1 作品定位与范围 · L38 · 2.0k
- CORE-DECISIONS §2 世界与地图 · L55 · 3.9k
- CORE-DECISIONS §3 时间、季节、天气 · L86 · 2.4k
- CORE-DECISIONS §4 美术风格 · L111 · 2.2k
- CORE-DECISIONS §5 界面 · L132 · 2.8k
- CORE-DECISIONS §6 存档 · L161 · 1.2k
- CORE-DECISIONS §7 音频 · L176 · 2.0k
- CORE-DECISIONS §8 性能与画面设置 · L198 · 1.8k
- CORE-DECISIONS §9 工程 · L218 · 3.3k
- CORE-DECISIONS §10 流程 · L243 · 4.6k
- CORE-DECISIONS §11 待对齐 · L274 · 61

## CORE-GLOSSARY · 术语表

`docs/design/core/CORE-GLOSSARY.md` · 38.9k 字 · 文档里专有说法的统一解释，按领域分组，每条一两句并给出细则所在的小节
关键词：术语、术语表、名词解释、同名异义、缩写、别名

- CORE-GLOSSARY §0 用法 · L23 · 287
  - ★ CORE-GLOSSARY §0.1 接入要点 · L29 · 104
- CORE-GLOSSARY §1 作品与默认状态 · L34 · 2.6k
- CORE-GLOSSARY §2 世界状态与时间 · L59 · 2.8k
- CORE-GLOSSARY §3 天气 · L89 · 3.1k
- CORE-GLOSSARY §4 美术与光照 · L122 · 2.9k
- CORE-GLOSSARY §5 箱庭、地形、边界与外围 · L156 · 3.3k
- CORE-GLOSSARY §6 室内与门 · L192 · 2.2k
- CORE-GLOSSARY §7 观察模式、行走与跳转 · L215 · 2.5k
- CORE-GLOSSARY §8 平面地图与传送 · L244 · 1.7k
- CORE-GLOSSARY §9 开始页、过场与存档 · L265 · 2.2k
- CORE-GLOSSARY §10 多地图 · L293 · 1.1k
- CORE-GLOSSARY §11 界面 · L306 · 2.1k
- CORE-GLOSSARY §12 音频 · L335 · 2.0k
- CORE-GLOSSARY §13 画面设置与性能 · L360 · 3.0k
- CORE-GLOSSARY §14 工程与流程 · L390 · 4.3k
- CORE-GLOSSARY §15 同名异义 · L434 · 2.0k

## ART-AMBIENCE · 日式日常街景：元素、分区与做法

`docs/design/art/ART-AMBIENCE.md` · 19.7k 字 · 日式日常街景元素的边界与分区，各元素的形、色、限定与三渲二做法，以及对应的鸟与声音
关键词：日式街景、日式氛围、日系、日本味、电线杆、架空线、自动售货机、售货机光岛、弯道凸面镜、驻轮场式车棚、屋顶围网、高架水箱、通长雨庇、推拉铝窗、象牙色窗帘、开放外廊、次路标线、让行牌、L 形边沟、砌块墙、樱花行道树、绣球、夏季绿帘、杆上防犯灯、便利店夜景、透明塑料伞、暖帘、防球网、外墙圆钟、电车道口

  - ART-AMBIENCE 分工 · L22 · 610
- ART-AMBIENCE §0 结论 · L34 · 1.6k
  - ★ ART-AMBIENCE §0.1 接入要点 · L43 · 821
- ART-AMBIENCE §1 边界、分区与通用约束 · L56 · 2.6k
  - ART-AMBIENCE §1.1 用与不用 · L58 · 531
  - ART-AMBIENCE §1.2 区域代号 · L71 · 491
  - ART-AMBIENCE §1.3 方式与通用约束 · L84 · 929
  - ART-AMBIENCE §1.4 颜色 · L99 · 605
- ART-AMBIENCE §2 校舍外立面与屋顶 · L112 · 1.8k
- ART-AMBIENCE §3 外廊、楼梯与出入口 · L125 · 1.1k
- ART-AMBIENCE §4 车棚、售货机与街角小设施 · L136 · 1.0k
- ART-AMBIENCE §5 电线杆、架空线与线上的鸟 · L146 · 976
- ART-AMBIENCE §6 道路标线、凸面镜、护栏、路缘与排水 · L154 · 1.8k
- ART-AMBIENCE §7 绿化 · L171 · 796
- ART-AMBIENCE §8 照明与夜景 · L181 · 960
- ART-AMBIENCE §9 生活痕迹 · L192 · 851
- ART-AMBIENCE §10 河堤、球场设施、店头、外围街道与飞机云 · L206 · 1.4k
- ART-AMBIENCE §11 声音 · L222 · 749
- ART-AMBIENCE §12 先做的 10 项 · L237 · 886
- ART-AMBIENCE §13 待验证（P0） · L256 · 648

## ART-FIDELITY · 视觉精度标准：处处不粗糙，细节按区分级

`docs/design/art/ART-FIDELITY.md` · 38.1k 字 · 任何位置都成立的精度底线、五个精度区的细节密度与判定距离、一票否决与验收方法，含行走走廊加密和外围分级精度
关键词：视觉精度、精度区、精度分区、K1、K2、K3、K4、K5、判定距离、d_K、底线清单、B1–B13、不粗糙、细节密度、一票否决、贴脸距离、L0+ 专用几何、热点簇、事件贴花、倒角表、不做旧、立面密度、可读规则、近景巡检点、人工评分、裸面检测、跳变检测、行走走廊、走廊加密

  - ART-FIDELITY 分工 · L22 · 197
- ART-FIDELITY §0 结论 · L27 · 6.3k
  - ART-FIDELITY §0.1 五个精度区 · L55 · 1.8k
  - ART-FIDELITY §0.2 区界规则 · L65 · 262
  - ART-FIDELITY §0.3 细节项在各区的取用 · L72 · 968
  - ART-FIDELITY §0.4 划分、数据与预算 · L91 · 738
  - ★ ART-FIDELITY §0.5 接入要点 · L104 · 1.0k
- ART-FIDELITY §1 换算基准 · L119 · 1.6k
- ART-FIDELITY §2 精致感的九个来源与底线清单 · L147 · 3.2k
  - ART-FIDELITY §2.0 底线清单（B1–B13） · L159 · 2.0k
- ART-FIDELITY §3 量化标准（L0 和 L1，即 5–150 m，按精度区取用） · L181 · 5.9k
  - ART-FIDELITY §3.1 每栋楼的构件层次 · L185 · 418
  - ART-FIDELITY §3.2 立面密度 · L200 · 378
  - ART-FIDELITY §3.3 窗 · L214 · 690
  - ART-FIDELITY §3.4 地面（每 100 m²） · L227 · 887
  - ART-FIDELITY §3.5 倒角与不做旧 · L251 · 1.0k
  - ART-FIDELITY §3.6 夜间与四季 · L274 · 890
  - ART-FIDELITY §3.7 天气 · L293 · 1.4k
- ART-FIDELITY §4 贴脸距离（L0+，0.5–5 m，人眼高度 1.6–1.7 m） · L318 · 1.9k
- ART-FIDELITY §5 镜头穿进楼体或钻入地下：最低内容 · L337 · 1.7k
- ART-FIDELITY §6 细节词汇（中国大学校园版） · L357 · 5.0k
- ART-FIDELITY §7 一票否决清单（每条都能测） · L445 · 1.7k
- ART-FIDELITY §8 验收方法 · L477 · 3.4k
- ART-FIDELITY §9 行走走廊（配合 SYS-WALK §10） · L518 · 2.2k
  - ART-FIDELITY §9.1 范围 · L522 · 290
  - ART-FIDELITY §9.2 画面构成 · L528 · 240
  - ART-FIDELITY §9.3 走廊里的加密标准 · L541 · 378
  - ART-FIDELITY §9.4 细节优先级 · L554 · 558
  - ART-FIDELITY §9.5 步行巡检线 · L570 · 563
- ART-FIDELITY §10 外围分级精度（WLD-OUTSKIRTS） · L581 · 1.3k
- ART-FIDELITY §11 待验证（P0） · L597 · 1.7k

## ART-MODELING · 风格化造型规则：比例、形体、部件与风格一致性

`docs/design/art/ART-MODELING.md` · 24.7k 字 · 建筑、植被、道具与室内的风格化比例和形体，固有色约束，部件库写法，每类对象的最低合格构件与法线规则，以及金样与跨包风格一致性流程
关键词：风格化造型、风格化比例、身份特征、比例表、重复上限、防摩尔纹、人体尺度锚点、树冠团块、道具、少粗圆干净、固有色、彩度上限、金样、参考板、参数化部件库、IPartDef、最低合格构件、三道门槛、收边构件、倒角做法、自定义法线、硬边法线、解析法线、修边条、贴面几何、换级不跳变、网格 LOD 简化、共同口号

  - ART-MODELING 分工 · L22 · 316
- ART-MODELING §0 结论 · L29 · 2.2k
  - ★ ART-MODELING §0.1 接入要点 · L44 · 1.0k
- ART-MODELING §1 先定身份，再放开 · L59 · 496
- ART-MODELING §2 现代混凝土和石材楼的比例规则 · L67 · 1.3k
- ART-MODELING §3 植被、水面与地面 · L96 · 1.1k
- ART-MODELING §4 道具与小物件 · L111 · 1.1k
- ART-MODELING §5 颜色 · L134 · 512
- ART-MODELING §6 让 20 多个建模者风格一致 · L145 · 3.6k
- ART-MODELING §7 最低合格构件与法线规则 · L188 · 11.3k
  - ART-MODELING §7.1 三道门槛 · L194 · 968
  - ART-MODELING §7.2 收边与倒角 · L207 · 1.4k
  - ART-MODELING §7.3 轮廓、深度层与截面 · L235 · 856
  - ART-MODELING §7.4 模块与修边条 · L253 · 990
  - ART-MODELING §7.5 贴面几何与顶点数据 · L267 · 1.0k
  - ART-MODELING §7.6 法线 · L283 · 1.0k
  - ART-MODELING §7.7 换级不跳变 · L295 · 1.2k
  - ART-MODELING §7.8 每类对象的最低合格构件 · L314 · 3.6k
- ART-MODELING §8 待验证（P0） · L329 · 809

## ART-RENOVATION · 老楼翻新：全部翻新的规则

`docs/design/art/ART-RENOVATION.md` · 18.8k 字 · 老楼位置、体量与身份特征不动，外观全部换成新楼标准：材质配色、删简留、窗檐楼梯屋顶的改法、成电辨认与验收
关键词：老楼翻新、翻新、翻修、翻新预设、翻新楼、全部翻新、旧痕、删简留、防盗网、窗套、窗楣、窗台滴水、勒脚、压顶檐口、管槽、管廊、护窗栏、伸出杆、颜色配额、色额、陶红墙带、浅奶咖墙、斑驳、顶点色、面砖、楼梯间窗带、叶影上墙、成电辨认、翻新对照楼

  - ART-RENOVATION 分工 · L22 · 483
- ART-RENOVATION §0 结论 · L30 · 1.3k
  - ★ ART-RENOVATION §0.1 接入要点 · L38 · 793
- ART-RENOVATION §1 适用范围 · L50 · 931
  - ART-RENOVATION §1.1 哪些楼列入翻新 · L52 · 422
  - ART-RENOVATION §1.2 不动什么 · L59 · 219
  - ART-RENOVATION §1.3 能加多少 · L65 · 279
- ART-RENOVATION §2 总则 · L70 · 1.3k
  - ART-RENOVATION §2.1 全部翻新，与新楼同标准 · L72 · 262
  - ART-RENOVATION §2.2 会脏的地方换成构件 · L78 · 677
  - ART-RENOVATION §2.3 生活感只靠干净的痕迹 · L91 · 317
- ART-RENOVATION §3 材质与配色 · L97 · 2.8k
  - ART-RENOVATION §3.1 材质族 · L99 · 462
  - ART-RENOVATION §3.2 每栋楼的颜色配额 · L113 · 195
  - ART-RENOVATION §3.3 取值 · L118 · 1.3k
  - ART-RENOVATION §3.4 斑驳、笔触与顶点色 · L149 · 460
  - ART-RENOVATION §3.5 面砖 · L159 · 191
  - ART-RENOVATION §3.6 描线 · L165 · 194
- ART-RENOVATION §4 删、简、留 · L173 · 1.8k
  - ART-RENOVATION §4.1 删：旧痕、杂物与防盗网 · L175 · 574
  - ART-RENOVATION §4.2 简：外机、管线、窗分格与标识 · L190 · 487
  - ART-RENOVATION §4.3 留：干净版的生活痕迹 · L202 · 755
- ART-RENOVATION §5 窗、檐口、楼梯间、屋顶与首层 · L216 · 2.1k
  - ART-RENOVATION §5.1 窗 · L218 · 639
  - ART-RENOVATION §5.2 檐口与女儿墙 · L228 · 321
  - ART-RENOVATION §5.3 楼梯间 · L235 · 383
  - ART-RENOVATION §5.4 屋顶 · L243 · 438
  - ART-RENOVATION §5.5 首层、入口与阳台 · L251 · 264
- ART-RENOVATION §6 氛围与成电辨认 · L258 · 1.9k
  - ART-RENOVATION §6.1 画法上的氛围手法 · L260 · 1.1k
  - ART-RENOVATION §6.2 日式物件上楼 · L278 · 521
  - ART-RENOVATION §6.3 保住"认得出是成电" · L285 · 291
- ART-RENOVATION §7 示例：三种楼型 · L292 · 3.1k
  - ART-RENOVATION §7.1 老式宿舍楼 · L294 · 1.0k
  - ART-RENOVATION §7.2 老教学楼 · L310 · 1.0k
  - ART-RENOVATION §7.3 老食堂 · L325 · 1.0k
- ART-RENOVATION §8 验收 · L341 · 488
- ART-RENOVATION §9 待验证（P0） · L350 · 583

## ART-SKYLIGHT · 天空、光照与大气

`docs/design/art/ART-SKYLIGHT.md` · 31.3k 字 · 世界状态与美术日月、主光与阴影、天光、天空云星月、雾与大气透视、光效清单与冷暖对照
关键词：主光、日月交接、无影蓝调、阴影、shadow_strength、方向量化、天光、环境光、天光方向、退光线、retreat_y、天空着色器、sky shader、霾带、云、飞机云、星空、银河、月亮外观、雾、大气透视、共用雾函数、fog.gdshaderinc、边缘雾、光柱、镜头光斑、太阳雨、彩虹、闪电、冷暖对照

  - ART-SKYLIGHT 分工 · L22 · 1.0k
- ART-SKYLIGHT §0 结论 · L48 · 2.3k
  - ★ ART-SKYLIGHT §0.1 接入要点 · L63 · 1.1k
- ART-SKYLIGHT §1 时间锚点（默认状态的参考值） · L78 · 2.8k
- ART-SKYLIGHT §2 太阳 · L114 · 1.4k
- ART-SKYLIGHT §3 光照骨架 · L142 · 5.8k
- ART-SKYLIGHT §4 天空 · L203 · 5.2k
- ART-SKYLIGHT §5 月亮 · L263 · 653
- ART-SKYLIGHT §6 雾与大气透视 · L279 · 3.2k
- ART-SKYLIGHT §7 光效清单 · L309 · 3.8k
- ART-SKYLIGHT §8 冷暖对照 · L368 · 701
- ART-SKYLIGHT WP-34 / WP-03 / WP-40 交付验收 · L392 · 1.7k
- ART-SKYLIGHT §9 待验证（P0） · L411 · 563

## ART-STYLE · 风格基础：三渲二日式动画背景规范

`docs/design/art/ART-STYLE.md` · 54.9k 字 · 全局风格：调色板与色名、明暗分阶与影色、描线、高光与透光、贴图、后期、排除元素与情绪
关键词：三渲二、卡通材质、toon.gdshaderinc、调色板、色名、调色板纹理、退役色名、明暗分阶、植被三阶、影色、影色推导、室内暖影、描线、线色、材质 ID、倒角提亮、凸边亮线、边缘光、透光、玻璃眩光、湿面、柔光、色调映射、后期链、跳色、日本元素边界、场景文字、PaletteToken、ToonSpec、LineClass

  - ART-STYLE 分工 · L22 · 1.0k
- ART-STYLE §0 结论 · L48 · 2.7k
  - ★ ART-STYLE §0.1 接入要点 · L63 · 1.5k
- ART-STYLE §1 总体风格与参照 · L78 · 2.2k
- ART-STYLE §2 明暗分阶 · L119 · 5.6k
- ART-STYLE §3 影子：明度为主，按材质定影色 · L195 · 5.5k
  - ART-STYLE §3.1 原则 · L199 · 656
  - ART-STYLE §3.2 推导规则 · L214 · 1.8k
  - ART-STYLE §3.3 天光与地面反光 · L257 · 370
  - ART-STYLE §3.4 室内暖影 · L265 · 763
  - ART-STYLE §3.5 哪些材质有色调变化 · L276 · 346
  - ART-STYLE §3.6 夜间与阴雨 · L288 · 321
  - ART-STYLE §3.7 示例 · L294 · 1.1k
- ART-STYLE §4 描线 · L315 · 6.1k
- ART-STYLE §5 高光、边缘光与透光 · L394 · 3.9k
- ART-STYLE §6 贴图与笔触 · L445 · 1.4k
- ART-STYLE §7 后期 · L468 · 5.7k
- ART-STYLE §8 调色板（全局常量，以这里的命名为准） · L537 · 9.2k
  - ART-STYLE §8.1 风格色 · L539 · 2.9k
  - ART-STYLE §8.2 大面积色与跳色的分工 · L587 · 1.1k
  - ART-STYLE §8.3 建筑固有色 · L606 · 1.2k
  - ART-STYLE §8.4 界面色 · L638 · 1.3k
  - ART-STYLE §8.5 色名登记与调色板纹理 · L663 · 1.6k
  - ART-STYLE §8.6 天气色 · L693 · 1.1k
- ART-STYLE §9 排除元素 · L712 · 2.6k
  - ART-STYLE §9.1 日本元素的边界 · L714 · 2.0k
  - ART-STYLE §9.2 其他排除项 · L740 · 631
- ART-STYLE §10 材质速查 · L749 · 694
- ART-STYLE §11 场景内的文字与标识（属于背景美术） · L767 · 1.0k
- ART-STYLE §12 远近细节分级（以 ENG-BUDGET §3 和 ART-FIDELITY 为准） · L799 · 419
- ART-STYLE §13 情绪弧线 · L812 · 923
- ART-STYLE WP-04 / WP-05 交付验收 · L837 · 1.9k
- ART-STYLE §14 待验证（P0） · L857 · 2.1k

## WLD-DIORAMA · 箱庭边界、截面与远景

`docs/design/world/WLD-DIORAMA.md` · 18.8k 字 · 箱庭外缘的地层截面、水柱截面、雾海与清水河银带、360° 远景卡片和雪山，以及它们随时段、季节、天气的变化
关键词：箱庭边界、地层截面、剖面、截面上缘、地下水线、卵石层、河渠水柱截面、暗涵断面、雾海、雾海顶面、雾天雾海上升、清水河银带、深夜雾海微光、远景、远景卡片、背景、三层环形卡片、远景视差、色调层、坡向层、发光层、雪山、日照金山、雪线、雪山可见度、backdrop.json、diorama_section、diorama_far、Camera3D.Far

  - WLD-DIORAMA 分工 · L22 · 1.0k
- WLD-DIORAMA §0 结论 · L41 · 1.9k
  - ★ WLD-DIORAMA §0.1 接入要点 · L54 · 846
- WLD-DIORAMA §1 校界与四周的外部情况 · L67 · 676
- WLD-DIORAMA §2 地层截面 · L85 · 2.8k
- WLD-DIORAMA §3 河渠水柱截面 · L120 · 965
- WLD-DIORAMA §4 截面下缘与雾海 · L144 · 2.1k
- WLD-DIORAMA §5 远景分层（360° 不留空白） · L172 · 3.8k
- WLD-DIORAMA §6 雪山 · L231 · 2.1k
- WLD-DIORAMA WP-22 交付验收 · L270 · 1.0k
- WLD-DIORAMA §7 待验证（P0） · L286 · 302

## WLD-INTERIORS · 室内：开放清单、门的区分、加载、生成器与类型库、楼梯与碰撞、预算

`docs/design/world/WLD-INTERIORS.md` · 77.6k 字 · 第一批 15 处开放室内与 3 处高处、门的开关、室内加载、平面生成器与类型库、楼梯碰撞、契约、预算与 G19
关键词：楼内、开放室内、开放清单、interiors.json、IInteriorProvider、InteriorLayout、EntranceDecl、开放入口、自动门、DoorLeafDecl、IInteriorDoors、DoorClosed、此处暂不开放、封口、门厅卡、窗卡、简化室内、I0 兜底层、室内加载、平面内核、GenericInteriorProvider、室内类型库、楼梯碰撞、SlopedWallCollider、关门换位、开放屋顶、导航图、INavGraph、室内色名、G19

  - WLD-INTERIORS 分工 · L24 · 519
- WLD-INTERIORS §0 结论 · L32 · 3.3k
  - ★ WLD-INTERIORS §0.1 接入要点 · L48 · 1.7k
- WLD-INTERIORS §1 为什么不全量开放 · L63 · 1.0k
- WLD-INTERIORS §2 第一批开放清单（15 处） · L81 · 14.9k
  - WLD-INTERIORS §2.0 共用约定 · L83 · 1.2k
  - WLD-INTERIORS §2.1 图书馆（WP-17） · L93 · 1.3k
  - WLD-INTERIORS §2.2 电子科技博物馆（WP-17） · L110 · 501
  - WLD-INTERIORS §2.3 主楼 B2（WP-31a） · L120 · 712
  - WLD-INTERIORS §2.4 宣邦楼（WP-31a） · L133 · 760
  - WLD-INTERIORS §2.5 体育馆与游泳馆（WP-31a） · L144 · 1.1k
  - WLD-INTERIORS §2.6 六号科研楼（WP-31b） · L159 · 472
  - WLD-INTERIORS §2.7 电子科学技术研究院大楼（WP-31b） · L167 · 390
  - WLD-INTERIORS §2.8 学知苑 1 栋（WP-31c） · L175 · 604
  - WLD-INTERIORS §2.9 银杏苑食堂（WP-31c） · L184 · 506
  - WLD-INTERIORS §2.10 学生活动中心与成电会堂（WP-31c） · L193 · 387
  - WLD-INTERIORS §2.11 商业街（WP-31c） · L201 · 600
  - WLD-INTERIORS §2.12 品学楼（WP-31b） · L210 · 804
  - WLD-INTERIORS §2.13 综合训练馆（WP-31b） · L223 · 497
  - WLD-INTERIORS §2.14 朝阳餐厅（WP-31c） · L231 · 525
  - WLD-INTERIORS §2.15 立人楼一层的教室走廊（WP-31a） · L240 · 496
  - WLD-INTERIORS §2.16 开放清单汇总 · L250 · 1.1k
  - WLD-INTERIORS §2.17 室内与高处的点位（P16 起，登记在 UI-NAV §6） · L273 · 3.0k
- WLD-INTERIORS §3 开放的屋顶与高处（3 处） · L303 · 1.6k
- WLD-INTERIORS §4 门的区分：开与关 · L321 · 3.9k
  - WLD-INTERIORS §4.1 楼里的门：走近推开，闭门器关上 · L343 · 2.0k
- WLD-INTERIORS §5 关着的楼：外壳、窗卡、门厅卡与简化室内 · L358 · 1.9k
  - WLD-INTERIORS §5.1 外壳与窗卡 · L360 · 888
  - WLD-INTERIORS §5.2 关着的门与门厅卡 · L370 · 397
  - WLD-INTERIORS §5.3 旁观飞进关着的楼：简化室内 · L377 · 616
- WLD-INTERIORS §6 加载与进楼衔接 · L387 · 3.1k
  - WLD-INTERIORS §6.1 整体加载、按层显示 · L389 · 1.0k
  - WLD-INTERIORS §6.2 触发 · L408 · 865
  - WLD-INTERIORS §6.3 进楼与出楼时要做的事（WP-07） · L418 · 1.0k
  - WLD-INTERIORS §6.4 进楼衔接（ENG-PERF §5 第 6 条） · L429 · 159
- WLD-INTERIORS §7 平面生成器与类型库 · L434 · 6.1k
  - WLD-INTERIORS §7.1 生成链与平面内核（WP-51a） · L436 · 1.1k
  - WLD-INTERIORS §7.2 楼型（12 种） · L445 · 733
  - WLD-INTERIORS §7.3 房间类型（35 类） · L464 · 2.1k
  - WLD-INTERIORS §7.4 规范值与行走换算 · L508 · 1.1k
  - WLD-INTERIORS §7.5 部件（约 120 个，分 8 组） · L528 · 1.1k
- WLD-INTERIORS §8 风格 · L546 · 5.6k
  - WLD-INTERIORS §8.1 艺术化处理 · L548 · 365
  - WLD-INTERIORS §8.2 明暗分阶与室内暖影 · L555 · 1.4k
  - WLD-INTERIORS §8.3 窗光斑与光（全片最重要的室内画面） · L571 · 1.6k
  - WLD-INTERIORS §8.4 室内色名 · L590 · 999
  - WLD-INTERIORS §8.5 中国高校识别要素与日式物件的分界 · L598 · 1.0k
  - WLD-INTERIORS §8.6 夜间不死黑 · L621 · 184
- WLD-INTERIORS §9 生活痕迹规则 · L625 · 1.9k
- WLD-INTERIORS §10 楼梯、门洞、电梯与碰撞 · L656 · 2.6k
- WLD-INTERIORS §11 展陈与门牌 · L705 · 379
- WLD-INTERIORS §12 室内预算 · L711 · 1.7k
- WLD-INTERIORS §13 导航图与路线 · L746 · 3.7k
- WLD-INTERIORS §14 扩展：开放更多室内 · L787 · 2.8k
- WLD-INTERIORS §15 契约增量（给 ENG-ARCH §3，这一组冻结前改，D-134） · L831 · 7.8k
- WLD-INTERIORS §16 事实修正汇总表（给各区文档用） · L944 · 2.8k
- WLD-INTERIORS §17 无主体块的归属 · L978 · 729
- WLD-INTERIORS §18 工作包 · L991 · 3.6k
- WLD-INTERIORS §19 验收：G19 路径与连通 · L1024 · 2.0k
- WLD-INTERIORS §21 待验证（P0） · L1045 · 1.7k

## WLD-LIFE · 动物、生活痕迹与街道家具

`docs/design/world/WLD-LIFE.md` · 27.9k 字 · 全校动物与反应动物、生活痕迹、停着的车与街道家具的内容，昼夜、四季与天气的显隐，行走碰撞、声音数据与 Godot 实现
关键词：动物、鸟、校园猫、鸽群、反应动物、黄昏乌鸦、季节性动物、生活痕迹、生活感、共享单车、电动车、晾晒、黑天鹅、季节生活痕迹、雨天生活痕迹、小心地滑、停着的车、校车、街道家具、分类垃圾箱、路灯灯具、售货机、外卖柜、长椅、声音插座、家具碰撞、日夜两套、变体掩码、state_bits、show_ratio

  - WLD-LIFE 分工 · L23 · 1.9k
- WLD-LIFE §0 结论 · L61 · 2.7k
  - ★ WLD-LIFE §0.1 接入要点 · L75 · 1.4k
- WLD-LIFE §1 动物 · L91 · 4.7k
  - WLD-LIFE §1.1 反应动物：猫、鸽群与水禽 · L137 · 1.8k
- WLD-LIFE §2 生活痕迹 · L157 · 3.7k
- WLD-LIFE §3 停着的车 · L231 · 1.3k
- WLD-LIFE §4 街道家具 · L252 · 2.8k
- WLD-LIFE §5 行走模式下的碰撞 · L301 · 2.1k
- WLD-LIFE §6 Godot 实现 · L336 · 3.9k
  - WLD-LIFE §6.1 容器 · L340 · 782
  - WLD-LIFE §6.2 显隐：整组可见性与变体掩码 · L352 · 1.8k
  - WLD-LIFE §6.3 动物 · L370 · 1.3k
- WLD-LIFE WP-25 交付验收 · L385 · 1.5k
- WLD-LIFE §7 待验证（P0） · L408 · 710

## WLD-OUTSKIRTS · 边界与外围：大箱庭、外围环境带、阻挡与提示

`docs/design/world/WLD-OUTSKIRTS.md` · 39.0k 字 · 大箱庭的三条线与截面精度、外围 O0–O3 的内容与精度、行走和旁观在边界的阻挡与提示、外围数据与预算
关键词：外围、校外、校界、边界、前沿线、截面线、大箱庭、外围环境带、O0–O3、最近可达距离、d_min、outskirts.json、outskirts.overlay.json、IOutskirtsModel、OutskirtsBlock、SurfaceTag.Boundary、校外暂未开放、前方暂未开放、行走阻挡、飞行阻挡、缓停、推回、缓冲带、软地板、可达空间、活动范围、edge_fog、边缘雾、看街长椅、河渠格栅

  - WLD-OUTSKIRTS 分工 · L22 · 379
- WLD-OUTSKIRTS §0 结论 · L30 · 3.1k
  - ★ WLD-OUTSKIRTS §0.1 接入要点 · L55 · 1.9k
- WLD-OUTSKIRTS §1 参考做法：挡路与精度的共同规律 · L71 · 89
- WLD-OUTSKIRTS §2 周边真实环境 · L75 · 1.6k
  - WLD-OUTSKIRTS §2.1 四边 · L79 · 966
  - WLD-OUTSKIRTS §2.2 数据缺口 · L88 · 179
  - WLD-OUTSKIRTS §2.3 结构要点 · L95 · 297
- WLD-OUTSKIRTS §3 大箱庭：三条线、截面线位置、截面精度 · L103 · 3.6k
  - WLD-OUTSKIRTS §3.1 三条线 · L105 · 440
  - WLD-OUTSKIRTS §3.2 截面线的位置 · L115 · 958
  - WLD-OUTSKIRTS §3.3 清水河与绕城高架 · L134 · 310
  - WLD-OUTSKIRTS §3.4 截面精度（与 WLD-DIORAMA 的分工） · L140 · 785
  - WLD-OUTSKIRTS §3.5 范围数 · L158 · 1.1k
- WLD-OUTSKIRTS §4 外围分层 O0–O3 · L171 · 4.6k
  - WLD-OUTSKIRTS §4.1 判定原则：最近可达距离 · L173 · 542
  - WLD-OUTSKIRTS §4.2 分层表 · L184 · 968
  - WLD-OUTSKIRTS §4.3 楼型与程序化布局 · L199 · 680
  - WLD-OUTSKIRTS §4.4 灯光（夜间） · L218 · 1.1k
  - WLD-OUTSKIRTS §4.5 时间、季节、天气 · L243 · 860
  - WLD-OUTSKIRTS §4.6 不画车流 · L264 · 448
- WLD-OUTSKIRTS §5 保留、删除与新增（艺术化处理） · L274 · 2.9k
  - WLD-OUTSKIRTS §5.1 身份地标（保留，让人认得出） · L283 · 506
  - WLD-OUTSKIRTS §5.2 删掉的（现实里有，放进画面难看） · L297 · 477
  - WLD-OUTSKIRTS §5.3 可以新增的氛围元素 · L309 · 838
  - WLD-OUTSKIRTS §5.4 画法：清透、细线 · L325 · 865
- WLD-OUTSKIRTS §6 数据管线与许可 · L345 · 2.2k
  - WLD-OUTSKIRTS §6.1 管线步骤（`pipeline/outskirts/`，Python，WP-47） · L347 · 694
  - WLD-OUTSKIRTS §6.2 产物 · L363 · 566
  - WLD-OUTSKIRTS §6.3 覆盖层 · L377 · 290
  - WLD-OUTSKIRTS §6.4 许可 · L387 · 619
- WLD-OUTSKIRTS §7 行走阻挡 · L397 · 1.8k
  - WLD-OUTSKIRTS §7.1 挡什么（WP-30、WP-44） · L399 · 438
  - WLD-OUTSKIRTS §7.2 0.6 m 内的缓停（WP-42） · L409 · 739
  - WLD-OUTSKIRTS §7.3 河渠格栅 · L425 · 185
  - WLD-OUTSKIRTS §7.4 看街长椅（WP-30 做，WP-44 选点） · L432 · 336
  - WLD-OUTSKIRTS §7.5 兜底 · L440 · 90
- WLD-OUTSKIRTS §8 飞行阻挡（旁观模式） · L445 · 3.3k
  - WLD-OUTSKIRTS §8.1 可达空间 · L447 · 608
  - WLD-OUTSKIRTS §8.2 缓冲带与推回（WP-06） · L466 · 709
  - WLD-OUTSKIRTS §8.3 边缘雾 `edge_fog` · L485 · 911
  - WLD-OUTSKIRTS §8.4 数据与求值 · L499 · 674
  - WLD-OUTSKIRTS §8.5 不受约束的镜头 · L511 · 340
- WLD-OUTSKIRTS §9 提示文案 · L519 · 635
- WLD-OUTSKIRTS §10 与多地图、平面地图、存档的衔接 · L534 · 754
- WLD-OUTSKIRTS §11 性能预算 · L550 · 2.4k
  - WLD-OUTSKIRTS §11.1 额度与来源 · L552 · 492
  - WLD-OUTSKIRTS §11.2 分层上限 · L567 · 541
  - WLD-OUTSKIRTS §11.3 显存、包体与 CPU · L580 · 999
  - WLD-OUTSKIRTS §11.4 低档设备 · L594 · 347
- WLD-OUTSKIRTS §12 工作包与契约增量 · L604 · 5.2k
  - WLD-OUTSKIRTS §12.1 WP-47 外围环境带 · L606 · 820
  - WLD-OUTSKIRTS §12.2 其余各包的增量 · L622 · 2.3k
  - WLD-OUTSKIRTS §12.3 契约增量 · L654 · 2.0k
- WLD-OUTSKIRTS §13 验收 · L690 · 2.6k
  - WLD-OUTSKIRTS §13.1 审查视角（ENG-GATES §1、UI-NAV §6） · L692 · 1.0k
  - WLD-OUTSKIRTS §13.2 门禁增量 · L707 · 844
  - WLD-OUTSKIRTS §13.3 WP-47 交付验收 · L736 · 686
- WLD-OUTSKIRTS §15 待验证（P0） · L749 · 1.1k

## WLD-TERRAIN · 地形：分层、地形操作与高度函数、贴地、竖直落差、水位与可走性

`docs/design/world/WLD-TERRAIN.md` · 40.4k 字 · 校园地形的设计原则与分层、地形覆盖层与确定性高度函数、贴地与垫层、竖直落差、水位、可走性、着色与性能
关键词：地形、地形分层、高度函数、地形评估器、地形操作、地形覆盖层、terrain.json、ITerrain、HeightAt、FillTile、PadY、WaterY、TerrainFacts、洞哨兵、平地快路径、主模数、灰区、零带、先平后贴、道路走廊、垫层、勒脚水平顶线、台缘带、退台、常水位、岸型、涉水带、Steep、分面平法线、遮挡与揭示

  - WLD-TERRAIN 分工 · L23 · 610
- WLD-TERRAIN §0 结论 · L30 · 2.8k
  - ★ WLD-TERRAIN §0.1 接入要点 · L43 · 1.6k
- WLD-TERRAIN §1 设计原则 · L58 · 1.2k
  - WLD-TERRAIN §1.1 总骨架 · L60 · 197
  - WLD-TERRAIN §1.2 规则 · L66 · 953
- WLD-TERRAIN §2 高度预算与分层 · L81 · 2.6k
  - WLD-TERRAIN §2.1 分层 · L83 · 448
  - WLD-TERRAIN §2.2 主模数 · L96 · 210
  - WLD-TERRAIN §2.3 手感刻度 · L102 · 560
  - WLD-TERRAIN §2.4 遮挡高度 · L117 · 325
  - WLD-TERRAIN §2.5 坡度类 · L124 · 519
  - WLD-TERRAIN §2.6 全图范围与零带 · L140 · 566
- WLD-TERRAIN §3 数据：地形覆盖层 · L147 · 10.1k
  - WLD-TERRAIN §3.1 文件与归属 · L149 · 614
  - WLD-TERRAIN §3.2 公共字段 · L157 · 440
  - WLD-TERRAIN §3.3 操作词表 · L171 · 3.7k
  - WLD-TERRAIN §3.4 合成顺序与优先级 · L207 · 550
  - WLD-TERRAIN §3.5 坡度包络：坡度上限由构造保证 · L219 · 1.2k
  - WLD-TERRAIN §3.6 确定性写法 · L232 · 737
  - WLD-TERRAIN §3.7 读取接口 · L244 · 1.8k
  - WLD-TERRAIN §3.8 校验与门禁 · L273 · 989
- WLD-TERRAIN §4 贴地 · L290 · 3.2k
  - WLD-TERRAIN §4.1 先平后贴 · L292 · 779
  - WLD-TERRAIN §4.2 道路与铺装 · L308 · 864
  - WLD-TERRAIN §4.3 楼宇：垫层、勒脚与入口 · L320 · 1.0k
  - WLD-TERRAIN §4.4 树与道具落地 · L331 · 561
- WLD-TERRAIN §5 竖直落差 · L342 · 2.4k
  - WLD-TERRAIN §5.1 连接规则 · L344 · 661
  - WLD-TERRAIN §5.2 台阶 · L360 · 270
  - WLD-TERRAIN §5.3 坡道 · L368 · 165
  - WLD-TERRAIN §5.4 台缘带（v1 的挡墙） · L374 · 549
  - WLD-TERRAIN §5.5 退台 · L384 · 203
  - WLD-TERRAIN §5.6 高墙（增强阶段） · L390 · 209
  - WLD-TERRAIN §5.7 防护 · L395 · 172
  - WLD-TERRAIN §5.8 谁来做 · L401 · 155
- WLD-TERRAIN §6 水体 · L406 · 3.5k
  - WLD-TERRAIN §6.1 常水位 · L408 · 532
  - WLD-TERRAIN §6.2 岸型 · L415 · 1.2k
  - WLD-TERRAIN §6.3 涉水带与浅滩 · L429 · 404
  - WLD-TERRAIN §6.4 湖盆 · L436 · 186
  - WLD-TERRAIN §6.5 河渠 · L442 · 258
  - WLD-TERRAIN §6.6 穿出校界 · L449 · 199
  - WLD-TERRAIN §6.7 开放泳区的水深与出口 · L455 · 680
- WLD-TERRAIN §7 可走性与碰撞 · L463 · 3.0k
  - WLD-TERRAIN §7.1 坡度分级 · L465 · 648
  - WLD-TERRAIN §7.2 与控制器的配合 · L480 · 371
  - WLD-TERRAIN §7.3 碰撞带 · L487 · 516
  - WLD-TERRAIN §7.4 瓦片接缝 · L497 · 472
  - WLD-TERRAIN §7.5 不卡脚、不穿地 · L506 · 621
  - WLD-TERRAIN §7.6 其他读地面高度的地方 · L522 · 334
- WLD-TERRAIN §8 三渲二着色与描线 · L529 · 2.6k
  - WLD-TERRAIN §8.1 为什么坡面容易脏 · L531 · 262
  - WLD-TERRAIN §8.2 规则 · L535 · 1.1k
  - WLD-TERRAIN §8.3 描线 · L552 · 582
  - WLD-TERRAIN §8.4 材质 ID 与表面 · L567 · 182
  - WLD-TERRAIN §8.5 换级与粗级法线 · L572 · 171
  - WLD-TERRAIN §8.6 与季节和光照的关系 · L577 · 222
- WLD-TERRAIN §9 LOD、分辨率与性能 · L583 · 2.3k
  - WLD-TERRAIN §9.1 分级 · L585 · 494
  - WLD-TERRAIN §9.2 几何误差与裙边 · L598 · 371
  - WLD-TERRAIN §9.3 量化范围与对角线 · L604 · 208
  - WLD-TERRAIN §9.4 烘焙、缓存与运行时求值 · L609 · 593
  - WLD-TERRAIN §9.5 预算 · L616 · 624
- WLD-TERRAIN §10 分阶段施工 · L627 · 2.0k
  - WLD-TERRAIN §10.1 第 0 阶段：接缝（现在，约 1 天，高优先级） · L629 · 510
  - WLD-TERRAIN §10.2 P0 样板区（约 2–4 天，高优先级） · L637 · 267
  - WLD-TERRAIN §10.3 主线铺开（约 1–2 周） · L644 · 254
  - WLD-TERRAIN §10.4 增强（按需） · L650 · 90
  - WLD-TERRAIN §10.5 各包的改动面 · L654 · 848
- WLD-TERRAIN §11 待验证（P0） · L672 · 1.5k

## WLD-VEGETATION · 植被：树种与四季、银杏大道与水杉路、草坪与灌木

`docs/design/world/WLD-VEGETATION.md` · 42.6k 字 · 全校树木、灌木、草坪与水生植物的画法、树种四季、种植位置、季相参数、碰撞、声音接口与 Godot 实现
关键词：植被、植物、树种、树冠画法、云团型、插片型、冬季裸枝、枝条笔触、叶量 A、开花 B、季相参数、季相行、局部季节、四季变色、银杏大道、水杉路、春季粉带、草坪、绿篱、荷、芦苇、落叶地毯、树冠投影、树冠遮蔽、覆雪、近池、远池、L2 替身、species.json、tree_species

  - WLD-VEGETATION 分工 · L23 · 908
- WLD-VEGETATION §0 结论 · L44 · 2.4k
  - ★ WLD-VEGETATION §0.1 接入要点 · L57 · 1.2k
- WLD-VEGETATION §1 卡通树冠的画法 · L72 · 2.9k
- WLD-VEGETATION §2 树种与四季 · L122 · 7.2k
  - WLD-VEGETATION §2.1 落叶树：状态、叶量与开花 · L132 · 2.0k
  - WLD-VEGETATION §2.2 常绿树与灌木 · L160 · 697
  - WLD-VEGETATION §2.3 色名与四季色值 · L173 · 4.0k
- WLD-VEGETATION §3 线性种植 · L222 · 1.2k
- WLD-VEGETATION §4 片区种植 · L252 · 2.5k
  - WLD-VEGETATION §4.1 外围（WLD-OUTSKIRTS） · L281 · 1.1k
- WLD-VEGETATION §5 草坪 · L298 · 2.0k
- WLD-VEGETATION §6 落叶、花瓣与积雪（地面覆盖） · L326 · 1.2k
- WLD-VEGETATION §7 水生植物 · L348 · 1.2k
- WLD-VEGETATION §8 提供给 SYS-MOTION 的参数 · L372 · 648
- WLD-VEGETATION §9 季相参数 · L383 · 4.3k
- WLD-VEGETATION §10 行走模式下的碰撞 · L442 · 791
- WLD-VEGETATION §11 Godot 实现 · L457 · 10.3k
  - WLD-VEGETATION §11.1 插片与团块的着色 · L464 · 1.4k
  - WLD-VEGETATION §11.2 每株与每插片的数据 · L477 · 1.2k
  - WLD-VEGETATION §11.3 阴影通道与 MAIN_CAM 门禁 · L503 · 943
  - WLD-VEGETATION §11.4 近池与远池 · L514 · 1.4k
  - WLD-VEGETATION §11.5 L2 替身：两条候选 · L531 · 697
  - WLD-VEGETATION §11.6 季节数据通道 · L543 · 2.1k
  - WLD-VEGETATION §11.7 地面遮罩与覆雪 · L579 · 786
  - WLD-VEGETATION §11.8 画面设置（SYS-GRAPHICS） · L588 · 1.4k
- WLD-VEGETATION §12 声音的接口（SYS-AUDIO） · L605 · 627
- WLD-VEGETATION WP-23 交付验收 · L615 · 1.1k
- WLD-VEGETATION §13 待验证（P0） · L635 · 668

## WLD-WATER · 水体：动画式水面、西湖、东湖、河渠、喷泉

`docs/design/world/WLD-WATER.md` · 27.8k 字 · 全校水面的动画式画法、倒影与反光带、四季与天气的水面、开放泳区与游泳的水面反馈、各湖与河渠、喷泉与池水，以及 Godot 实现
关键词：水体、水面、开放泳区、西湖、东湖、河渠、波纹线、主光反光带、碎金带、月光银带、四芒星闪点、泡沫线、倒影、平面反射、伪深度色带、雨点环、涉水涟漪、入水水花、灯的竖条倒影、refl_lamps、reflect_tex、reflect_vp、reflect_lake、西湖喷泉、叠水喷泉、游泳池池水、冬季浅滩、漂浮物、河渠中线、water.gdshader

  - WLD-WATER 分工 · L23 · 792
- WLD-WATER §0 结论 · L47 · 2.6k
  - ★ WLD-WATER §0.1 接入要点 · L61 · 1.4k
- WLD-WATER §1 动画式水面（所有水体通用） · L77 · 6.5k
- WLD-WATER §2 西湖 · L189 · 1.4k
- WLD-WATER §3 东湖 · L216 · 916
- WLD-WATER §4 河渠 · L236 · 1.7k
- WLD-WATER §5 主楼喷泉（可选） · L265 · 145
- WLD-WATER §6 截面水柱的水色 · L269 · 470
- WLD-WATER §7 Godot 实现 · L283 · 6.9k
  - WLD-WATER §7.1 水面材质（默认不透明） · L287 · 2.0k
  - WLD-WATER §7.2 倒影接口 · L311 · 2.2k
  - WLD-WATER §7.3 数据通道：反光带、灯的竖条倒影、涟漪 · L334 · 1.5k
  - WLD-WATER §7.4 透明变体（只在需要时） · L344 · 194
  - WLD-WATER §7.5 水下 · L350 · 378
  - WLD-WATER §7.6 多地图（SYS-MAPS） · L356 · 420
- WLD-WATER §8 游泳馆与室外游泳池的池水 · L363 · 767
- WLD-WATER §9 品学楼门前叠水喷泉 · L378 · 916
- WLD-WATER WP-24 交付验收 · L389 · 1.3k
- WLD-WATER §10 待验证（P0） · L416 · 804

## MAP-QSH-ADDITIONS · 设计增补：给没有设计感的地方加设计

`docs/design/maps/qingshuihe/MAP-QSH-ADDITIONS.md` · 43.7k 字 · 全校无设计感处的增补词汇、楼宇设计规则、禁做清单、各包增补任务，以及夜间灯光总图和雨天增补
关键词：设计增补、增补物、增补词汇、作用代号、默认状态规则、初灯、初灯组、照度三级、夜间灯光总图、灯光调度表、灯具标记、普通楼宇设计规则、西立面竖向鳍片、日式街景物件落位、不建议增补、禁做清单、新颜色登记、walk 列、可跳上、步行服务设施、导览亭、导览桩、直饮水台、雨帘、避雨处、积水点、lighting-schedule.json、功能开敞面、过渡绿地、空间记录

- MAP-QSH-ADDITIONS §0 通用规则 · L22 · 3.6k
  - ★ MAP-QSH-ADDITIONS §0.1 接入要点 · L58 · 1.1k
- MAP-QSH-ADDITIONS §1 中国高校"有设计感"的常见手法 · L72 · 519
- MAP-QSH-ADDITIONS §2 按场地类型的增补词汇 · L89 · 15.3k
  - MAP-QSH-ADDITIONS §2.1 宿舍组团内外 · L93 · 944
  - MAP-QSH-ADDITIONS §2.2 食堂与商业街前 · L105 · 948
  - MAP-QSH-ADDITIONS §2.3 普通道路和路口 · L116 · 861
  - MAP-QSH-ADDITIONS §2.4 停车场 · L127 · 650
  - MAP-QSH-ADDITIONS §2.5 河渠两岸（红光右支渠、走马河、磨底河） · L137 · 916
  - MAP-QSH-ADDITIONS §2.6 湖岸 · L148 · 775
  - MAP-QSH-ADDITIONS §2.7 大片草坪 · L158 · 822
  - MAP-QSH-ADDITIONS §2.8 楼与楼之间的空地 · L171 · 657
  - MAP-QSH-ADDITIONS §2.9 屋顶 · L181 · 903
  - MAP-QSH-ADDITIONS §2.10 运动区周边 · L194 · 553
  - MAP-QSH-ADDITIONS §2.11 围墙和校界 · L204 · 1.0k
  - MAP-QSH-ADDITIONS §2.12 桥 · L216 · 544
  - MAP-QSH-ADDITIONS §2.13 步行服务设施（配合 SYS-WALK §10） · L226 · 1.7k
  - MAP-QSH-ADDITIONS §2.14 外围街道带（WLD-OUTSKIRTS） · L238 · 1.2k
  - MAP-QSH-ADDITIONS §2.15 钟与广播（SYS-AUDIO） · L257 · 548
  - MAP-QSH-ADDITIONS §2.16 日式日常街景物件（ART-STYLE §9.1、ART-AMBIENCE） · L266 · 2.1k
- MAP-QSH-ADDITIONS §3 让普通楼宇有设计感 · L287 · 1.5k
- MAP-QSH-ADDITIONS §4 参照与转译规则 · L304 · 1.2k
- MAP-QSH-ADDITIONS §5 不建议增补 · L330 · 1.2k
- MAP-QSH-ADDITIONS §6 需要登记的新颜色 · L352 · 833
- MAP-QSH-ADDITIONS §7 各工作包的增补任务 · L374 · 7.4k
- MAP-QSH-ADDITIONS §8 夜间灯光总图（交 WP-39） · L419 · 6.3k
  - MAP-QSH-ADDITIONS §8.1 照度三级 · L431 · 674
  - MAP-QSH-ADDITIONS §8.2 颜色 · L443 · 479
  - MAP-QSH-ADDITIONS §8.3 初灯组 · L452 · 549
  - MAP-QSH-ADDITIONS §8.4 调度表 · L459 · 2.9k
  - MAP-QSH-ADDITIONS §8.5 雨夜与雾夜 · L506 · 896
- MAP-QSH-ADDITIONS §9 雨天：雨帘、避雨处与积水点 · L517 · 2.3k
  - MAP-QSH-ADDITIONS §9.1 雨帘与避雨处 · L521 · 1.2k
  - MAP-QSH-ADDITIONS §9.2 积水点 · L542 · 945
- MAP-QSH-ADDITIONS §10 待验证（P0） · L552 · 658

## MAP-QSH-CENTER-EAST · 中部与东区：中部科研区、雕像、品学楼、文科楼群、训练馆、球场、田径场

`docs/design/maps/qingshuihe/MAP-QSH-CENTER-EAST.md` · 28.7k 字 · 中部科研区与东区各建筑、雕像、球场的外观与身份特征、开放入口门口表现、四季昼夜、天气、行走、碰撞与声音数据
关键词：中部科研区、东区、基础实验大楼、一号科研实验楼、科研楼B区、科研楼C区、麦克斯韦像、方程石、林为干院士像、科研楼碑、银杏大道跨河小桥、品学楼、第一教学楼、书法横梁门框、架空通道、叠水喷泉石台、文科楼群、友谊树、综合训练馆、筒拱、网球场树墙、绿色房间、球场标线、球场围网、高杆灯、田径场、器材室、室外游泳池组团、东区月出、center-east.overlay.json

  - MAP-QSH-CENTER-EAST 分工 · L23 · 1.1k
  - MAP-QSH-CENTER-EAST 实现 · L47 · 644
- MAP-QSH-CENTER-EAST §0 结论 · L56 · 2.1k
  - ★ MAP-QSH-CENTER-EAST §0.1 接入要点 · L69 · 1.0k
- MAP-QSH-CENTER-EAST 数据定位 · L81 · 2.7k
- MAP-QSH-CENTER-EAST §1 中部科研区 · L117 · 1.2k
- MAP-QSH-CENTER-EAST §2 品学楼 · L143 · 2.4k
- MAP-QSH-CENTER-EAST §3 文科楼群 · L178 · 854
- MAP-QSH-CENTER-EAST §4 综合训练馆与球场 · L198 · 1.4k
- MAP-QSH-CENTER-EAST §5 东缘 · L226 · 951
- MAP-QSH-CENTER-EAST §6 本区的光 · L237 · 522
- MAP-QSH-CENTER-EAST WP-21 交付验收 · L246 · 743
- MAP-QSH-CENTER-EAST 定位、设计增补与近景要求 · L259 · 2.2k
- MAP-QSH-CENTER-EAST §7 季节与昼夜清单 · L303 · 3.3k
- MAP-QSH-CENTER-EAST §8 行走 · L343 · 4.1k
- MAP-QSH-CENTER-EAST §9 声音要点（SYS-AUDIO） · L398 · 603
- MAP-QSH-CENTER-EAST §10 待验证（P0） · L408 · 1.1k

## MAP-QSH-CORE · 核心区：八角图书馆、时间广场、日晷、博物馆、西湖

`docs/design/maps/qingshuihe/MAP-QSH-CORE.md` · 30.6k 字 · 核心区图书馆、时间广场、日晷、博物馆外观与西湖岸线构筑物，含四季昼夜、天气、行走与声音要点
关键词：核心区、图书馆外观、八角采光顶、穹顶、方塔玻璃槽、竖缝窗、大台阶、二楼正门、品·悦吧店面、宁静苑、无主体块、时间广场、旗杆、十二时辰刻度、日晷、晷针、博物馆外观、柱廊环、西湖、折线栈桥、白色拱桥、湖心岛观景平台、西湖喷泉、箱涵、天鹅屋、入水口荷花、草阶看台、core.overlay.json

  - MAP-QSH-CORE 分工 · L27 · 998
  - MAP-QSH-CORE 实现 · L50 · 720
- MAP-QSH-CORE §0 结论 · L59 · 2.3k
  - ★ MAP-QSH-CORE §0.1 接入要点 · L74 · 1.2k
- MAP-QSH-CORE 数据定位 · L89 · 2.2k
- MAP-QSH-CORE §1 图书馆外观 · L118 · 3.1k
- MAP-QSH-CORE §2 时间广场 · L168 · 676
- MAP-QSH-CORE §3 日晷 · L182 · 595
- MAP-QSH-CORE §4 电子科技博物馆 · L197 · 792
- MAP-QSH-CORE §5 西湖的岸线与构筑物 · L214 · 2.2k
- MAP-QSH-CORE §6 本区的构图关系 · L260 · 413
- MAP-QSH-CORE WP-16 交付验收 · L268 · 929
- MAP-QSH-CORE 定位、设计增补与近景要求 · L286 · 2.4k
- MAP-QSH-CORE §7 季节与昼夜清单 · L332 · 4.9k
- MAP-QSH-CORE §8 行走 · L388 · 3.9k
  - MAP-QSH-CORE §8.1 主网路段（必须能走，SYS-WALK §6） · L392 · 335
  - MAP-QSH-CORE §8.2 能看不能进的地方与挡路物 · L407 · 792
  - MAP-QSH-CORE §8.3 可进入的室内 · L422 · 616
  - MAP-QSH-CORE §8.4 涉水区（`wade`，水深不到 0.3 m，SYS-WALK §9） · L431 · 189
  - MAP-QSH-CORE §8.5 行走落点 · L439 · 662
  - MAP-QSH-CORE §8.6 节奏与行走走廊 · L454 · 1.2k
- MAP-QSH-CORE §9 声音要点（SYS-AUDIO） · L481 · 751
- MAP-QSH-CORE §10 待验证（P0） · L491 · 815

## MAP-QSH-LIBRARY · 图书馆与博物馆室内

`docs/design/maps/qingshuihe/MAP-QSH-LIBRARY.md` · 28.5k 字 · 图书馆开放范围的室内、三条看进去的路径、室内光与视觉落点，以及博物馆展厅、行走与声音要点
关键词：图书馆室内、中庭、回廊、八字楼梯、塔楼梯、交通核、开放范围、封口、门斗、闸机、二楼门厅、看进去的路径、品·悦吧室内、光条、顶棚波光、门口的叶、红桌、视觉落点、八瓣光、台灯、校训木匾、五楼西窗阅览带、北侧露台、博物馆展厅、环形展线、interior.library、core.interior.json、lib.main

  - MAP-QSH-LIBRARY 分工 · L39 · 683
  - MAP-QSH-LIBRARY 实现 · L52 · 1.1k
- MAP-QSH-LIBRARY §0 平面与分区 · L62 · 4.7k
  - MAP-QSH-LIBRARY §0.1 平面 · L64 · 1.2k
  - MAP-QSH-LIBRARY §0.2 交通核 · L89 · 1.1k
  - MAP-QSH-LIBRARY §0.3 每层的开放范围与封口（WLD-INTERIORS §2.1） · L111 · 834
  - MAP-QSH-LIBRARY §0.4 层高与标高（初值，§12） · L125 · 248
  - MAP-QSH-LIBRARY §0.5 研修室、卫生间、直饮水 · L130 · 263
  - ★ MAP-QSH-LIBRARY §0.6 接入要点 · L138 · 1.1k
- MAP-QSH-LIBRARY §1 三条看进去的路径 · L152 · 1.2k
- MAP-QSH-LIBRARY §2 中庭与回廊 · L169 · 799
- MAP-QSH-LIBRARY §3 一楼中厅与 A、D 扇区（由中庭八字楼梯下来） · L183 · 1.5k
- MAP-QSH-LIBRARY §4 室外光的延伸 · L211 · 1.7k
- MAP-QSH-LIBRARY §5 视觉落点 · L239 · 1.2k
- MAP-QSH-LIBRARY §6 室内的生活痕迹 · L261 · 317
- MAP-QSH-LIBRARY WP-17 交付验收 · L275 · 998
- MAP-QSH-LIBRARY §7 季节与昼夜清单 · L291 · 3.4k
- MAP-QSH-LIBRARY §8 行走 · L334 · 3.4k
- MAP-QSH-LIBRARY §9 附属部分 · L397 · 1.1k
- MAP-QSH-LIBRARY §10 博物馆展厅（WP-17，WLD-INTERIORS §2.2） · L418 · 1.2k
- MAP-QSH-LIBRARY §11 声音要点（SYS-AUDIO） · L434 · 1.2k
- MAP-QSH-LIBRARY §12 待验证（P0） · L451 · 934

## MAP-QSH-NORTH · 北部生活区：学生活动中心、校医院、商业街、宿舍组团、餐厅

`docs/design/maps/qingshuihe/MAP-QSH-NORTH.md` · 33.7k 字 · 北部生活区各建筑的外观与身份特征、开放入口门口表现、四季昼夜、天气、行走挡路、碰撞与声音数据
关键词：北部生活区、学生活动中心、成电会堂、赭红圆鼓、校医院、院名灯牌、商业街、玻璃拱顶步行街、店招、宿舍组团、学知苑、硕丰苑、博翰苑、留学生组团、本科楼A型、本科楼B型、组团闸机、学知一组团、阳台晾晒、银杏苑、芙蓉餐厅、思学苑、春晖苑、朝阳餐厅、西北餐厅、室外扶梯罩棚、风雨连廊、丹桂路、博士公寓、north.overlay.json

  - MAP-QSH-NORTH 分工 · L23 · 1.1k
  - MAP-QSH-NORTH 实现 · L43 · 634
- MAP-QSH-NORTH §0 结论 · L52 · 2.1k
  - ★ MAP-QSH-NORTH §0.1 接入要点 · L65 · 1.0k
- MAP-QSH-NORTH 数据定位 · L77 · 4.3k
  - MAP-QSH-NORTH 地标与服务 · L79 · 1.8k
  - MAP-QSH-NORTH 店面与餐厅点位 · L101 · 1.3k
  - MAP-QSH-NORTH 宿舍组团 · L129 · 1.2k
- MAP-QSH-NORTH §1 学生活动中心与成电会堂 · L156 · 792
- MAP-QSH-NORTH §2 校医院 · L173 · 505
- MAP-QSH-NORTH §3 商业街 · L181 · 1.6k
- MAP-QSH-NORTH §4 宿舍 · L216 · 3.5k
- MAP-QSH-NORTH §5 餐厅 · L280 · 2.1k
- MAP-QSH-NORTH §6 文化地标与丹桂路 · L310 · 205
- MAP-QSH-NORTH WP-20 交付验收 · L317 · 807
- MAP-QSH-NORTH 定位、设计增补与近景要求 · L331 · 2.2k
- MAP-QSH-NORTH §7 季节与昼夜清单 · L374 · 3.9k
- MAP-QSH-NORTH §8 行走 · L424 · 5.7k
- MAP-QSH-NORTH §9 声音要点（SYS-AUDIO） · L495 · 780
- MAP-QSH-NORTH §10 待验证（P0） · L506 · 942

## MAP-QSH-RENOVATION · 清水河翻新清单

`docs/design/maps/qingshuihe/MAP-QSH-RENOVATION.md` · 21.8k 字 · 清水河校区要翻新的楼与环境：逐条的实体、优先级、翻新部位和所用规则，以及待核实楼的做法
关键词：翻新清单、逐栋翻新、翻新条目、条目号、翻新优先级、高中低档、部位代号、楼型、翻新对照楼、待核实的楼、核实前的口径、缺立面近景、不列入的楼、2015 年以后的新楼、宿舍屋面设备、组团门禁、商业街店招、全校环境翻新、老楼翻新理由、翻新施工顺序

- MAP-QSH-RENOVATION §0 结论 · L23 · 1.3k
  - ★ MAP-QSH-RENOVATION §0.1 接入要点 · L35 · 676
- MAP-QSH-RENOVATION §1 优先级、条目号、部位代号与楼型 · L47 · 2.5k
  - MAP-QSH-RENOVATION §1.1 优先级 · L49 · 345
  - MAP-QSH-RENOVATION §1.2 条目号 · L59 · 329
  - MAP-QSH-RENOVATION §1.3 部位代号与通用做法 · L73 · 1.4k
  - MAP-QSH-RENOVATION §1.4 楼型 · L92 · 409
- MAP-QSH-RENOVATION §2 北部生活区（MAP-QSH-NORTH，WP-20） · L104 · 5.4k
  - MAP-QSH-RENOVATION §2.1 宿舍 · L106 · 3.0k
  - MAP-QSH-RENOVATION §2.2 食堂、商业与生活服务 · L127 · 1.5k
  - MAP-QSH-RENOVATION §2.3 组团门禁与屋面设备 · L140 · 920
- MAP-QSH-RENOVATION §3 中部与东区（MAP-QSH-CENTER-EAST，WP-21） · L147 · 1.4k
- MAP-QSH-RENOVATION §4 南区（MAP-QSH-SOUTH，WP-18） · L159 · 1.6k
- MAP-QSH-RENOVATION §5 西区与西北区（MAP-QSH-WEST，WP-19） · L172 · 512
- MAP-QSH-RENOVATION §6 核心区（MAP-QSH-CORE，WP-16） · L181 · 470
- MAP-QSH-RENOVATION §7 全校环境 · L188 · 1.6k
- MAP-QSH-RENOVATION §8 待核实的楼 · L203 · 2.9k
  - MAP-QSH-RENOVATION §8.1 核实前的口径 · L205 · 387
  - MAP-QSH-RENOVATION §8.2 年代未查到的楼 · L213 · 2.0k
  - MAP-QSH-RENOVATION §8.3 缺立面近景的楼 · L238 · 492
- MAP-QSH-RENOVATION §9 不列入的楼 · L252 · 853
- MAP-QSH-RENOVATION §10 登记、分工与验收 · L271 · 1.2k
  - MAP-QSH-RENOVATION §10.1 覆盖层登记 · L273 · 311
  - MAP-QSH-RENOVATION §10.2 分工 · L279 · 459
  - MAP-QSH-RENOVATION §10.3 施工顺序 · L294 · 182
  - MAP-QSH-RENOVATION §10.4 验收 · L300 · 266
- MAP-QSH-RENOVATION §11 待验证（P0） · L306 · 361

## MAP-QSH-SOUTH · 南区：南门、主楼、东湖、宣邦楼、立人楼、体育场馆、国际创新中心

`docs/design/maps/qingshuihe/MAP-QSH-SOUTH.md` · 40.0k 字 · 南区各楼与构筑物的外观、受光与夜景、四季昼夜与天气、行走与开放入口、声音要点
关键词：南区、南门、校名墙、主楼、主轴线、屋顶空廊、过街楼、主楼泛光、森林公园、孔雀之家、东湖岸线、环湖跑道、里程标、六角亭、亲水平台、荷角、吴立人像、宣邦楼、钟楼、电子管穹顶、交班的钟、钟面指针、立人楼、体育馆、游泳馆、体育运动中心大平台、看台罩棚、国际创新中心、1–8 号楼、south.overlay.json

  - MAP-QSH-SOUTH 分工 · L22 · 1.2k
  - MAP-QSH-SOUTH 实现 · L46 · 706
- MAP-QSH-SOUTH §0 结论 · L54 · 2.1k
  - ★ MAP-QSH-SOUTH §0.1 接入要点 · L68 · 1.0k
- MAP-QSH-SOUTH 数据定位 · L80 · 4.9k
- MAP-QSH-SOUTH §1 南门 · L144 · 1.1k
- MAP-QSH-SOUTH §2 主楼 · L163 · 2.5k
- MAP-QSH-SOUTH §3 主楼周边 · L199 · 713
- MAP-QSH-SOUTH §4 东湖 · L215 · 1.6k
- MAP-QSH-SOUTH §5 宣邦楼 · L252 · 2.5k
- MAP-QSH-SOUTH §6 立人楼 · L293 · 665
- MAP-QSH-SOUTH §7 体育场馆（沿西源大道从西到东） · L306 · 1.5k
- MAP-QSH-SOUTH §8 西南角：成电国际创新中心与科创餐厅 · L333 · 509
- MAP-QSH-SOUTH §9 南区中部的 1–8 号楼 · L343 · 517
- MAP-QSH-SOUTH §10 本区的水道 · L350 · 392
- MAP-QSH-SOUTH WP-18 交付验收 · L357 · 1.0k
- MAP-QSH-SOUTH 定位、设计增补与近景要求 · L374 · 2.9k
- MAP-QSH-SOUTH §11 季节与昼夜清单 · L426 · 5.1k
- MAP-QSH-SOUTH §12 行走 · L484 · 5.6k
  - MAP-QSH-SOUTH §12.1 主网路段（必须能走，SYS-WALK §6） · L488 · 549
  - MAP-QSH-SOUTH §12.2 能看不能进的地方与挡路物 · L502 · 1.3k
  - MAP-QSH-SOUTH §12.3 可进入的室内 · L519 · 671
  - MAP-QSH-SOUTH §12.4 涉水区（`wade`，水深不到 0.3 m，SYS-WALK §9） · L531 · 170
  - MAP-QSH-SOUTH §12.5 行走落点 · L538 · 795
  - MAP-QSH-SOUTH §12.6 节奏与行走走廊 · L551 · 1.9k
- MAP-QSH-SOUTH §13 声音要点 · L586 · 936
- MAP-QSH-SOUTH §14 待验证（P0） · L597 · 813

## MAP-QSH-TERRAIN · 分区地形：骨架、地标丘、揭示路线与各区地形操作

`docs/design/maps/qingshuihe/MAP-QSH-TERRAIN.md` · 41.8k 字 · 清水河全图地形骨架与各区地形操作：分层分布、红枫丘、宿舍台原、揭示路线、常水位、点位标高、验收镜头与冲突处理
关键词：分区地形、地形骨架、红枫丘、地标丘、下沉时间广场、谷阶殿、落金沟、湖岸丘链、草阶看台台地、东湖亭丘、岛丘、品学楼台基、坡广场、宿舍台原、上台、下台、台口、台缘、通学坡、坡顶回望、缓波、看月台、堤上路、球场堤路、下沉草阶草坪、河岸低步道、渠堤、湿地低地、扇形草坪浅碟、中庭草丘、常水位表、湖口跌水、箱涵堰口、遮挡与揭示、地形验收镜头、terrain.json

  - MAP-QSH-TERRAIN 分工 · L23 · 549
- MAP-QSH-TERRAIN §0 结论 · L31 · 3.5k
  - ★ MAP-QSH-TERRAIN §0.1 接入要点 · L59 · 1.4k
- MAP-QSH-TERRAIN §1 全图骨架 · L73 · 8.2k
  - MAP-QSH-TERRAIN §1.1 分层分布 · L75 · 1.0k
  - MAP-QSH-TERRAIN §1.2 地标丘：红枫丘 · L90 · 1.1k
  - MAP-QSH-TERRAIN §1.3 遮挡与揭示的三条路线 · L103 · 1.0k
  - MAP-QSH-TERRAIN §1.4 水系与常水位 · L131 · 2.0k
  - MAP-QSH-TERRAIN §1.5 全图高差表 · L155 · 1.3k
  - MAP-QSH-TERRAIN §1.6 坡度与着色的取舍 · L187 · 512
  - MAP-QSH-TERRAIN §1.7 从高空读成一只箱庭 · L194 · 407
  - MAP-QSH-TERRAIN §1.8 文件归属与写法 · L202 · 911
- MAP-QSH-TERRAIN §2 中轴广场与图书馆 · L216 · 1.3k
  - MAP-QSH-TERRAIN §2.1 设计意图 · L218 · 193
  - MAP-QSH-TERRAIN §2.2 地形操作 · L223 · 559
  - MAP-QSH-TERRAIN §2.3 连接 · L231 · 249
  - MAP-QSH-TERRAIN §2.4 受影响的元素与点位 · L237 · 257
  - MAP-QSH-TERRAIN §2.5 验收镜头 · L244 · 42
- MAP-QSH-TERRAIN §3 银杏大道 · L248 · 1.3k
  - MAP-QSH-TERRAIN §3.1 设计意图 · L250 · 264
  - MAP-QSH-TERRAIN §3.2 地形操作 · L255 · 681
  - MAP-QSH-TERRAIN §3.3 连接 · L265 · 93
  - MAP-QSH-TERRAIN §3.4 受影响的元素与点位 · L270 · 239
  - MAP-QSH-TERRAIN §3.5 验收镜头 · L277 · 59
- MAP-QSH-TERRAIN §4 西湖与红枫丘 · L281 · 2.5k
  - MAP-QSH-TERRAIN §4.1 设计意图 · L283 · 215
  - MAP-QSH-TERRAIN §4.2 地形操作 · L288 · 1.1k
  - MAP-QSH-TERRAIN §4.3 连接 · L301 · 629
  - MAP-QSH-TERRAIN §4.4 水位与岸型 · L310 · 182
  - MAP-QSH-TERRAIN §4.5 受影响的元素与点位 · L316 · 302
  - MAP-QSH-TERRAIN §4.6 验收镜头 · L324 · 54
- MAP-QSH-TERRAIN §5 东湖 · L328 · 1.8k
  - MAP-QSH-TERRAIN §5.1 设计意图 · L330 · 182
  - MAP-QSH-TERRAIN §5.2 地形操作 · L335 · 934
  - MAP-QSH-TERRAIN §5.3 连接 · L347 · 340
  - MAP-QSH-TERRAIN §5.4 受影响的元素与点位 · L353 · 295
  - MAP-QSH-TERRAIN §5.5 验收镜头 · L361 · 56
- MAP-QSH-TERRAIN §6 河渠与箱涵 · L365 · 2.6k
  - MAP-QSH-TERRAIN §6.1 设计意图 · L367 · 231
  - MAP-QSH-TERRAIN §6.2 地形操作 · L372 · 1.6k
  - MAP-QSH-TERRAIN §6.3 连接 · L390 · 566
  - MAP-QSH-TERRAIN §6.4 受影响的元素与点位 · L397 · 170
  - MAP-QSH-TERRAIN §6.5 验收镜头 · L403 · 35
- MAP-QSH-TERRAIN §7 宿舍区 · L407 · 5.2k
  - MAP-QSH-TERRAIN §7.1 设计意图 · L409 · 526
  - MAP-QSH-TERRAIN §7.2 台原的范围 · L416 · 855
  - MAP-QSH-TERRAIN §7.3 地形操作 · L427 · 1.6k
  - MAP-QSH-TERRAIN §7.4 连接：台缘、台口与路 · L442 · 1.7k
  - MAP-QSH-TERRAIN §7.5 受影响的元素与点位 · L471 · 445
  - MAP-QSH-TERRAIN §7.6 验收镜头 · L479 · 97
- MAP-QSH-TERRAIN §8 运动场与体育区 · L483 · 2.0k
  - MAP-QSH-TERRAIN §8.1 设计意图 · L485 · 378
  - MAP-QSH-TERRAIN §8.2 地形操作 · L490 · 945
  - MAP-QSH-TERRAIN §8.3 连接与受影响的元素 · L501 · 560
  - MAP-QSH-TERRAIN §8.4 验收镜头 · L508 · 79
- MAP-QSH-TERRAIN §9 食堂与商业街 · L512 · 673
  - MAP-QSH-TERRAIN §9.1 设计意图 · L514 · 307
  - MAP-QSH-TERRAIN §9.2 地形操作 · L519 · 223
  - MAP-QSH-TERRAIN §9.3 受影响的元素 · L525 · 130
- MAP-QSH-TERRAIN §10 教学科研区 · L530 · 2.9k
  - MAP-QSH-TERRAIN §10.1 设计意图 · L532 · 536
  - MAP-QSH-TERRAIN §10.2 地形操作 · L539 · 1.4k
  - MAP-QSH-TERRAIN §10.3 连接 · L551 · 610
  - MAP-QSH-TERRAIN §10.4 受影响的元素与点位 · L559 · 272
  - MAP-QSH-TERRAIN §10.5 验收镜头 · L566 · 70
- MAP-QSH-TERRAIN §11 校门与校界 · L570 · 613
  - MAP-QSH-TERRAIN §11.1 设计意图 · L572 · 270
  - MAP-QSH-TERRAIN §11.2 河渠穿出校界处 · L577 · 241
  - MAP-QSH-TERRAIN §11.3 受影响的元素 · L587 · 89
- MAP-QSH-TERRAIN §12 验收镜头与冲突检查 · L592 · 5.1k
  - MAP-QSH-TERRAIN §12.1 验收镜头总表 · L594 · 1.4k
  - MAP-QSH-TERRAIN §12.2 冲突检查 · L616 · 2.6k
  - MAP-QSH-TERRAIN §12.3 点位与锚点标高 · L635 · 1.1k
- MAP-QSH-TERRAIN §13 待验证（P0） · L658 · 1.2k

## MAP-QSH-WEST · 西区与西北区：六号科研楼、校训石、创新中心、经管楼、托幼园、四号/五号科研楼、湿地、西门桥头

`docs/design/maps/qingshuihe/MAP-QSH-WEST.md` · 27.3k 字 · 西区与西北区各楼、石刻、湿地与西门桥头的外观、四季昼夜与天气、行走与开放入口、声音要点
关键词：西区、西北区、六号科研楼、基础与前沿研究院、公式台阶、2F室外连廊、校训石、创新中心、经管楼、三号科研实验楼、宾诺咖啡、立人托幼公园、键盘立面、四号科研楼、扇形草坪、深蓝玻璃盒入口、五号科研楼、电子科学技术研究院大楼、微波暗室、工程训练中心、荟文楼、综合楼、核磁共振中心、国防科技园石刻、湿地公园、湿地栈道、西门桥头、水杉路、孤儿楼 6151 与 6154、west.overlay.json

  - MAP-QSH-WEST 分工 · L22 · 783
  - MAP-QSH-WEST 实现 · L39 · 604
- MAP-QSH-WEST §0 结论 · L49 · 1.8k
  - ★ MAP-QSH-WEST §0.1 接入要点 · L62 · 791
- MAP-QSH-WEST 数据定位 · L73 · 3.3k
- MAP-QSH-WEST §1 六号科研楼 · L125 · 1.2k
- MAP-QSH-WEST §2 校训石 · L148 · 412
- MAP-QSH-WEST §3 创新中心 · L162 · 431
- MAP-QSH-WEST §4 经管楼 · L172 · 128
- MAP-QSH-WEST §5 立人托幼公园 · L178 · 276
- MAP-QSH-WEST §6 四号科研楼 · L185 · 480
- MAP-QSH-WEST §7 五号科研楼、电子科学技术研究院大楼、核磁共振中心 · L198 · 806
- MAP-QSH-WEST §8 荟文楼（综合楼） · L211 · 110
- MAP-QSH-WEST §9 石刻 · L216 · 89
- MAP-QSH-WEST §10 湿地公园与西门桥头 · L222 · 972
- MAP-QSH-WEST WP-19 交付验收 · L241 · 775
- MAP-QSH-WEST 定位、设计增补与近景要求 · L256 · 2.3k
- MAP-QSH-WEST §11 季节与昼夜清单 · L298 · 3.7k
- MAP-QSH-WEST §12 行走 · L341 · 4.9k
- MAP-QSH-WEST §13 声音要点 · L413 · 468
- MAP-QSH-WEST §14 待验证（P0） · L422 · 797

## SYS-ATLAS · 平面地图与传送：底图、小地图、大地图、标记、锚点、传送与解锁

`docs/design/systems/SYS-ATLAS.md` · 45.9k 字 · 规定平面地图：底图烘焙与上色、小地图、大地图、标记、锚点、雾幕传送、解锁、契约与 G18 验收
关键词：平面地图、小地图、大地图、底图、地图标记、锚点、传送点、导览亭、导览桩、传送、雾幕瞬移、飞过去、传送卡、传送落点、目录页、追踪、自定义标记、解锁策略、区域名、地名标注、atlasRect、G18 地图一致性、IAtlasService、IUnlockPolicy、MarkerId、AtlasSave、map_base.gdshader、markers.json、labels.json、atlas.overlay.json

- SYS-ATLAS §0 结论 · L30 · 3.1k
  - ★ SYS-ATLAS §0.1 接入要点 · L61 · 1.5k
- SYS-ATLAS §1 允许什么、禁止什么 · L77 · 491
- SYS-ATLAS §2 底图（WP-48a 烘焙，WP-48b 上色） · L90 · 7.8k
  - SYS-ATLAS §2.1 来源：世界模型，不是原始 OSM · L92 · 3.1k
  - SYS-ATLAS §2.2 上色：`map_base.gdshader` · L145 · 1.7k
  - SYS-ATLAS §2.3 外围、校界与箱庭外缘 · L184 · 1.2k
  - SYS-ATLAS §2.4 季节与昼夜：只做轻微变化 · L196 · 354
  - SYS-ATLAS §2.5 文字与标注 · L207 · 1.4k
- SYS-ATLAS §3 小地图（WP-48b） · L230 · 2.4k
- SYS-ATLAS §4 大地图（WP-48b） · L306 · 4.8k
  - SYS-ATLAS §4.1 布局与开合 · L308 · 2.1k
  - SYS-ATLAS §4.2 缩放与平移 · L344 · 451
  - SYS-ATLAS §4.3 图层、筛选与图例 · L361 · 203
  - SYS-ATLAS §4.4 目录页（G） · L368 · 369
  - SYS-ATLAS §4.5 传送卡 · L378 · 695
  - SYS-ATLAS §4.6 自定义标记与追踪 · L403 · 471
  - SYS-ATLAS §4.7 大地图内的按键 · L418 · 473
- SYS-ATLAS §5 标记类型 · L438 · 2.5k
- SYS-ATLAS §6 锚点实体：导览亭与导览桩（WP-13 做部件，WP-48c 登记，WP-44 配合选点） · L471 · 3.2k
- SYS-ATLAS §7 传送流程（WP-48c 调用，WP-36 实现过渡） · L523 · 2.8k
  - SYS-ATLAS §7.1 两种方式 · L525 · 702
  - SYS-ATLAS §7.2 传送的时序 · L536 · 923
  - SYS-ATLAS §7.3 落点 · L553 · 675
  - SYS-ATLAS §7.4 预取与到达保证 · L565 · 202
  - SYS-ATLAS §7.5 打断与边界情况 · L573 · 232
- SYS-ATLAS §8 解锁策略（WP-48c） · L581 · 698
- SYS-ATLAS §9 多地图下的数据（WP-48c，与 SYS-MAPS一致） · L596 · 1.7k
- SYS-ATLAS §10 实现与性能（WP-48b、WP-48c） · L621 · 2.4k
- SYS-ATLAS §11 C# 契约（照 ENG-ARCH §3 的写法；纯 .NET，只增不改） · L684 · 5.4k
- SYS-ATLAS §12 存档里存什么、设置里存什么 · L776 · 978
- SYS-ATLAS §13 各包的分工 · L804 · 1.8k
- SYS-ATLAS §14 验收 · L826 · 2.9k
  - SYS-ATLAS §14.1 G18 地图一致性（ENG-GATES §2） · L828 · 1.3k
  - SYS-ATLAS §14.2 G14 界面门禁的地图部分（G14 的总则见 UI-VISUAL与 ENG-GATES §2，这里只列地图部分） · L848 · 983
  - SYS-ATLAS §14.3 G2、G13、G15、G16 与 F4 的地图增量 · L860 · 661
- SYS-ATLAS §16 待验证（P0） · L879 · 592

## SYS-AUDIO · 音频：环境声、观者声、空间声学、界面音效与混音

`docs/design/systems/SYS-AUDIO.md` · 113.5k 字 · 声音的总体架构、环境声景、天气与钟声、观者声、空间声学、界面音、素材许可、契约、预算与 G21 门禁
关键词：音频、声音、环境声、声景、观者声、脚步声、空间声学、混响、混音、音频总线、界面音效、开场音乐、音量设置、远雷、钟声、校园广播、人迹声、素材许可、IAudioService、IAmbienceService、IFoleyService、IAcousticEnv、IWalkTelemetry、AudioBuses、FootMat、AmbientEmitter、SoundSocketDecl、audio.cfg、licenses.json、G21

  - SYS-AUDIO 分工 · L22 · 121
- SYS-AUDIO §0 结论 · L27 · 4.5k
  - ★ SYS-AUDIO §0.1 接入要点 · L43 · 2.4k
- SYS-AUDIO §2 总体架构 · L59 · 8.8k
  - SYS-AUDIO §2.1 一个内核、三条管线 · L61 · 2.6k
  - SYS-AUDIO §2.2 输入：只读世界状态 · L100 · 1.3k
  - SYS-AUDIO §2.3 混音公式与平滑 · L122 · 991
  - SYS-AUDIO §2.4 发声器的四种形态 · L142 · 604
  - SYS-AUDIO §2.5 声源池、仲裁与虚拟化 · L153 · 1.6k
  - SYS-AUDIO §2.6 二维与三维的分工、距离衰减 · L178 · 1.1k
  - SYS-AUDIO §2.7 生命周期：启动、换图、读档、退出 · L198 · 628
- SYS-AUDIO §3 总线布局与混音铁律 · L206 · 3.2k
  - SYS-AUDIO §3.1 总线树（代码生成，不用 `.tres`） · L208 · 1.6k
  - SYS-AUDIO §3.2 混音铁律 · L239 · 869
  - SYS-AUDIO §3.3 响度与增益规划 · L249 · 765
- SYS-AUDIO §4 环境声景 · L267 · 12.0k
  - SYS-AUDIO §4.1 七层与两类驱动量 · L269 · 1.7k
  - SYS-AUDIO §4.2 数据驱动的发声器 · L300 · 3.7k
  - SYS-AUDIO §4.3 季节与出没 · L354 · 1.7k
  - SYS-AUDIO §4.4 生命感规则 · L379 · 1.2k
  - SYS-AUDIO §4.5 室内档案 · L393 · 1.5k
  - SYS-AUDIO §4.6 校外、外围与活动边界 · L419 · 689
  - SYS-AUDIO §4.7 离地高度与介质 · L430 · 429
  - SYS-AUDIO §4.8 英雄声景 · L438 · 1.1k
- SYS-AUDIO §5 天气声与画面同参 · L456 · 1.7k
  - SYS-AUDIO §5.1 雨：按落点表面分 · L460 · 1.0k
  - SYS-AUDIO §5.2 雪、雾、风 · L480 · 561
- SYS-AUDIO §6 远雷、钟声、广播与偶发事件 · L488 · 3.6k
  - SYS-AUDIO §6.1 远雷 · L490 · 589
  - SYS-AUDIO §6.2 宣邦楼的钟声 · L499 · 905
  - SYS-AUDIO §6.3 傍晚的校园广播 · L509 · 662
  - SYS-AUDIO §6.4 偶发事件与其他同步提示 · L518 · 1.4k
- SYS-AUDIO §7 加速流逝的节制与转场 · L544 · 2.4k
  - SYS-AUDIO §7.1 按真实秒排程（RateGovernor） · L546 · 1.1k
  - SYS-AUDIO §7.2 转场 · L566 · 1.3k
- SYS-AUDIO §8 人迹声：确保不出现清晰人声 · L587 · 2.0k
- SYS-AUDIO §9 观者声 · L625 · 14.8k
  - SYS-AUDIO §9.1 范围与原则 · L627 · 401
  - SYS-AUDIO §9.2 步相与步频 · L634 · 1.9k
  - SYS-AUDIO §9.3 `FootMat` 与 `SurfaceTag` · L665 · 3.3k
  - SYS-AUDIO §9.4 地面覆盖物 · L711 · 948
  - SYS-AUDIO §9.5 分层 · L727 · 728
  - SYS-AUDIO §9.6 变体与随机化参数 · L744 · 2.3k
  - SYS-AUDIO §9.7 位置固定的微变化 · L786 · 425
  - SYS-AUDIO §9.8 交互音的范围 · L795 · 2.9k
  - SYS-AUDIO §9.9 飞行风声：实时合成 · L818 · 785
  - SYS-AUDIO §9.10 控制器事件 `IWalkTelemetry` · L830 · 1.0k
- SYS-AUDIO §10 空间声学 · L837 · 12.7k
  - SYS-AUDIO §10.1 "窄处脚步更明显"的原理与参数 · L839 · 2.1k
  - SYS-AUDIO §10.2 方案组合：不上 Steam Audio · L867 · 636
  - SYS-AUDIO §10.3 声学网格（随启动集发布，全图常驻） · L880 · 981
  - SYS-AUDIO §10.4 房间与门户 · L888 · 1.6k
  - SYS-AUDIO §10.5 射线细化（只在行走模式） · L914 · 958
  - SYS-AUDIO §10.6 预设与声源侧混响区 · L924 · 3.0k
  - SYS-AUDIO §10.7 听者空间槽与交叉切换 · L956 · 1.1k
  - SYS-AUDIO §10.8 遮挡与室内外传声 · L971 · 1.2k
  - SYS-AUDIO §10.9 介质与天气的消音感 · L981 · 1.0k
- SYS-AUDIO §11 界面音效 · L996 · 2.2k
- SYS-AUDIO §12 开场音乐 · L1038 · 1.0k
- SYS-AUDIO §13 设置：五组音量与减少干扰 · L1051 · 1.3k
- SYS-AUDIO §14 素材策略与许可 · L1075 · 2.9k
  - SYS-AUDIO §14.1 来源白名单 · L1077 · 1.2k
  - SYS-AUDIO §14.2 处理流程（53e 的 `pipeline/audio/`） · L1094 · 1.1k
  - SYS-AUDIO §14.3 版本库：Git LFS · L1111 · 275
  - SYS-AUDIO §14.4 署名页 · L1117 · 308
- SYS-AUDIO §15 格式与导入 · L1124 · 1.6k
- SYS-AUDIO §16 Godot 4.7 实现要点与引擎坑 · L1149 · 3.6k
- SYS-AUDIO §17 确定性与 Movie Maker 录音 · L1185 · 928
- SYS-AUDIO §18 性能预算与包体 · L1193 · 2.6k
  - SYS-AUDIO §18.1 CPU · L1195 · 1.6k
  - SYS-AUDIO §18.2 内存与包体 · L1215 · 937
- SYS-AUDIO §19 C# 契约 · L1232 · 15.7k
- SYS-AUDIO §20 帧顺序：`FramePriority` 里的两步 · L1470 · 2.0k
- SYS-AUDIO §21 工作包 · L1498 · 5.5k
  - SYS-AUDIO §21.1 WP-53 音频 · L1500 · 2.8k
  - SYS-AUDIO §21.2 其他包的增量 · L1524 · 2.7k
- SYS-AUDIO §22 验收：G21 声音 · L1552 · 4.2k
- SYS-AUDIO §25 待验证（P0） · L1572 · 995

## SYS-GRAPHICS · 画面设置：预设、单项、风格底线、自动推荐与自适应

`docs/design/systems/SYS-GRAPHICS.md` · 22.0k 字 · 四档画面预设与单项、风格底线、应用流程、首次分档与自动调节、持久化与命令行、契约与验收
关键词：画面设置、画质、画面预设、设备档、流畅、均衡、精致、极致、单项、风格底线、风格默认、自动调节画质、自适应、首次分档、渲染比例、超采样、雾幕类设置、画面页、显存估算、IQualityService、IQualityAware、QualityResponsive、QualityKnob、QualityView、KnobSpec、quality-presets.json、graphics.cfg、engine_override.cfg、reset-graphics.cmd、预设轮换

- SYS-GRAPHICS §0 结论 · L24 · 2.1k
  - ★ SYS-GRAPHICS §0.1 接入要点 · L35 · 1.3k
- SYS-GRAPHICS §1 预设 · L50 · 1.8k
- SYS-GRAPHICS §2 风格底线与风格默认 · L80 · 854
- SYS-GRAPHICS §3 单项清单与生效方式 · L93 · 3.4k
- SYS-GRAPHICS §4 应用流程 · L138 · 632
- SYS-GRAPHICS §5 自动推荐与自动调节 · L153 · 2.5k
- SYS-GRAPHICS §6 持久化与命令行 · L204 · 2.3k
- SYS-GRAPHICS §7 各包的分工 · L226 · 992
- SYS-GRAPHICS §8 契约 · L247 · 2.7k
- SYS-GRAPHICS §9 界面（WP-35，视觉见 UI-VISUAL §8.5） · L303 · 1.1k
- SYS-GRAPHICS §10 验收 · L317 · 836
- SYS-GRAPHICS §12 待验证（P0） · L329 · 512

## SYS-MAPS · 多地图：按图数据、`IMapService` 与切换

`docs/design/systems/SYS-MAPS.md` · 43.7k 字 · 按图数据与 map.json、IMapService 先卸后装切图、时间全局天气按图、map_rect、同图重载门禁
关键词：多地图、换图、切图、地图服务、按图数据、按图与全局、先卸后装、切换 8 步、同图重载、天气按图、进图补算、地图归属、本图默认状态、单图半径、启动集 PCK、IMapService、IMapContext、IMapAware、MapScope、MapDescriptor、MapId、MapVisit、MapTravelRequest、IMapTransition、map.json、index.json、map_rect、boot_<地图>.pck、FramePriority.Map、--gate=maptravel

  - SYS-MAPS 分工与约定 · L22 · 235
- SYS-MAPS §0 结论 · L28 · 2.3k
  - ★ SYS-MAPS §0.1 接入要点 · L38 · 1.3k
- SYS-MAPS §2 数据模型与目录 · L51 · 6.6k
  - SYS-MAPS §2.1 目录 · L53 · 3.1k
  - SYS-MAPS §2.2 按图与全局的划分 · L100 · 1.1k
  - SYS-MAPS §2.4 本机烘焙缓存与启动集 PCK · L117 · 2.3k
- SYS-MAPS §3 `map.json`：`MapDescriptor` · L134 · 4.5k
- SYS-MAPS §4 运行时与切换 · L203 · 4.5k
  - SYS-MAPS §4.1 一个 World3D，先卸后装 · L205 · 263
  - SYS-MAPS §4.2 切换的 8 步 · L211 · 2.7k
  - SYS-MAPS §4.3 规则 · L232 · 903
  - SYS-MAPS §4.4 各入口 · L244 · 671
- SYS-MAPS §5 世界状态：时间与季节全局，天气按图 · L256 · 1.3k
- SYS-MAPS §6 相机、行走与安全点 · L281 · 633
- SYS-MAPS §7 自动发现与 `MapScope` · L292 · 1.6k
- SYS-MAPS §8 C# 契约 · L313 · 6.9k
- SYS-MAPS §9 帧顺序：`FramePriority.Map = −1300` · L430 · 1.4k
- SYS-MAPS §10 全局参数 `map_rect` · L457 · 1.4k
- SYS-MAPS §11 命令行 · L475 · 998
- SYS-MAPS §12 内存、包体与单图半径 · L490 · 1.0k
- SYS-MAPS §13 同图重载门禁 · L500 · 1.4k
- SYS-MAPS §14 以后的方向 · L525 · 1.3k
- SYS-MAPS §15 风险与 P0 验证 · L549 · 1.1k
- SYS-MAPS §16 工作包与各包增量 · L564 · 2.5k
- SYS-MAPS §17 验收 · L596 · 642
- SYS-MAPS §19 待验证（P0） · L607 · 706

## SYS-MOTION · 动态与事件

`docs/design/systems/SYS-MOTION.md` · 46.5k 字 · 两个时钟与一拍二，风、悬浮物、流体、生命体与反应动物的节奏，唯一的偶发事件、季节氛围与转场
关键词：动态、两个时钟、动画时钟、AnimTime、anim_time、一拍二、step_rate、Step12、T12、480 s 回绕、内置 TIME、风场、阵风、IWindField、WindSample、wind、飘落池、萤火虫、解析运动、季节氛围、反应动物、根变换单一写者、偶发事件、晚归者、IncidentDirector、incidents.json、incident、转场、TransitionDirector、钟锤

  - SYS-MOTION 分工 · L23 · 861
- SYS-MOTION §0 结论 · L41 · 3.0k
  - ★ SYS-MOTION §0.1 接入要点 · L56 · 1.4k
- SYS-MOTION §1 世界时钟、流逝与动画时钟 · L72 · 6.5k
- SYS-MOTION §2 节奏规则（所有动态通用） · L147 · 1.7k
- SYS-MOTION §3 风：一个起因带动一串变化 · L167 · 1.2k
- SYS-MOTION §4 悬浮物 · L196 · 2.9k
- SYS-MOTION §5 流体 · L237 · 519
- SYS-MOTION §6 生命体 · L246 · 3.1k
  - SYS-MOTION §6.1 对行走身体作出反应的动物 · L271 · 1.6k
- SYS-MOTION §7 光的缓慢变化（由世界时钟驱动） · L288 · 1.1k
- SYS-MOTION §8 远景 · L309 · 1.3k
- SYS-MOTION §9 图书馆、开放室内与钟楼的微动 · L324 · 1.6k
- SYS-MOTION §10 偶发事件（全片唯一） · L345 · 5.6k
- SYS-MOTION §11 季节氛围 · L426 · 2.3k
- SYS-MOTION §12 转场 · L456 · 6.3k
- SYS-MOTION §13 粒子类与解析运动 · L530 · 4.0k
  - SYS-MOTION §13.1 飘落池（13.3.3、13.4.1、13.4.3、13.10.5、13.12.2） · L542 · 938
  - SYS-MOTION §13.2 萤火虫（13.11.2） · L553 · 622
  - SYS-MOTION §13.3 雪（13.11.3） · L563 · 689
  - SYS-MOTION §13.4 河面漂浮物（WLD-WATER §4.2、13.5.2） · L573 · 454
  - SYS-MOTION §13.5 开销 · L579 · 150
  - SYS-MOTION §13.6 降水池（SYS-WEATHER §7） · L583 · 395
- SYS-MOTION WP-26 交付验收 · L589 · 1.9k
- SYS-MOTION §14 待验证（P0） · L614 · 695

## SYS-SAVE · 存档与持久化：存档模型、存什么、格式与迁移、保存时机、加载与一致性、安全、契约与验收

`docs/design/systems/SYS-SAVE.md` · 54.0k 字 · 规定存档位与世界线、存档段、文件格式与迁移、保存时机、读档流程与一致性、设置文件安全与 G20
关键词：存档、保存、读档、自动保存、存档位、世界线、存档段、段版本、存档迁移、金档、落定快照、安全时刻、续跑等价、世界实例、校验修复、存档损坏、单实例锁、设置文件安全、退出保存、ISaveService、SaveSectionAttribute、ISaveSection、SaveReason、LoadScope、RestorePhase、SaveMeta、IWorldState.Instance、FlushForQuit、CompleteResume、user://saves/

- SYS-SAVE §0 结论 · L25 · 3.2k
  - ★ SYS-SAVE §0.1 接入要点 · L52 · 2.0k
- SYS-SAVE §1 存档模型 · L67 · 1.6k
- SYS-SAVE §2 存什么、不存什么 · L107 · 4.5k
- SYS-SAVE §3 文件格式与版本迁移 · L178 · 4.9k
  - SYS-SAVE §3.1 容器 · L180 · 518
  - SYS-SAVE §3.2 meta · L198 · 1.2k
  - SYS-SAVE §3.3 正文：分段 JSON · L220 · 955
  - SYS-SAVE §3.4 缩略图 · L244 · 510
  - SYS-SAVE §3.5 序列化约定 · L256 · 796
  - SYS-SAVE §3.6 版本与迁移 · L269 · 931
- SYS-SAVE §4 保存时机与可靠性 · L287 · 6.4k
  - SYS-SAVE §4.1 时机 · L289 · 1.1k
  - SYS-SAVE §4.2 安全时刻与落定快照 · L311 · 955
  - SYS-SAVE §4.3 写盘流程（线程池） · L334 · 562
  - SYS-SAVE §4.4 退出流程 · L345 · 1.4k
  - SYS-SAVE §4.5 Godot 与 Windows 的几个坑 · L365 · 1.5k
  - SYS-SAVE §4.6 保存状态与失败反馈 · L384 · 939
- SYS-SAVE §5 加载流程与一致性 · L395 · 5.8k
  - SYS-SAVE §5.1 加载流程 · L397 · 2.1k
  - SYS-SAVE §5.2 世界实例：`IWorldState.Instance` · L436 · 855
  - SYS-SAVE §5.3 "状态一致"的定义 · L456 · 658
  - SYS-SAVE §5.4 天气 · L475 · 440
  - SYS-SAVE §5.5 新游戏 · L485 · 775
  - SYS-SAVE §5.6 校验修复 · L501 · 877
- SYS-SAVE §6 关着游戏的那段现实时间不计 · L519 · 524
- SYS-SAVE §7 存档损坏与版本不兼容 · L532 · 1.0k
- SYS-SAVE §8 设置类文件只接受基本类型 · L556 · 1.3k
- SYS-SAVE §9 安全：为什么禁止用 Resource 和 ConfigFile 存档 · L577 · 1.6k
- SYS-SAVE §10 C# 契约 · L608 · 9.5k
- SYS-SAVE §11 帧顺序：`FramePriority.Save = 1000` · L778 · 994
- SYS-SAVE §12 命令行 · L799 · 1.2k
- SYS-SAVE §13 性能实测 · L815 · 2.5k
- SYS-SAVE §14 验收：G20 · L861 · 3.0k
- SYS-SAVE §15 工作包 · L893 · 2.9k
- SYS-SAVE §17 待验证（P0） · L928 · 476

## SYS-TIME · 时间与季节系统：世界状态、美术日月、色彩脚本、过渡

`docs/design/systems/SYS-TIME.md` · 41.4k 字 · 世界状态与控制契约、美术日月、色彩脚本、四季内容、时间季节天气的过渡、灯光时刻表与时季验收
关键词：世界状态、钟点、季节、世界时间、世界实例、流逝倍率、落金初灯、默认状态、IWorldState、IWorldControl、WorldPreset、WorldTransition、ClockSetMode、FlowTo、美术太阳、美术月亮、主光交接、退光线、色彩脚本、调色板关键帧、季相行、英雄帧、四季内容、换季转场、改时间、延时摄影快进、每帧顺序、灯光时刻表、lighting-schedule.json、有效太阳高度

  - SYS-TIME 分工 · L23 · 166
- SYS-TIME §0 结论 · L34 · 2.9k
  - ★ SYS-TIME §0.1 接入要点 · L56 · 1.4k
- SYS-TIME §1 世界状态与控制（WP-34） · L71 · 10.6k
- SYS-TIME §2 美术太阳与月亮（WP-34） · L201 · 2.6k
- SYS-TIME §3 色彩脚本（WP-37） · L250 · 8.6k
  - SYS-TIME §3.1 组织方式 · L252 · 2.4k
  - SYS-TIME §3.2 秋季基准关键帧 · L278 · 3.8k
  - SYS-TIME §3.3 四季修正与季节专属覆盖 · L304 · 974
  - SYS-TIME §3.4 难看时段的改造 · L323 · 516
  - SYS-TIME §3.5 英雄帧 · L332 · 986
- SYS-TIME §4 四季内容（WP-38，配合 WP-23、WP-15、WP-24） · L358 · 4.7k
- SYS-TIME §5 过渡 · L417 · 1.9k
- SYS-TIME §6 灯光与生活痕迹的时刻表（WP-39，各区配合） · L449 · 3.6k
- SYS-TIME §7 与动态、偶发事件的协调（WP-26） · L507 · 867
- SYS-TIME §8 验收（WP-41，配合 ENG-GATES） · L520 · 2.9k
- SYS-TIME §10 待验证（P0） · L550 · 445

## SYS-TITLE · 开始页与进入过场：扉页、开始页、新游戏与继续过场、跳过、`IAppFlow`

`docs/design/systems/SYS-TITLE.md` · 44.0k 字 · 规定冷启动扉页、开始页 T0、新游戏与继续过场、跳过、IAppFlow 状态机与过场数据格式
关键词：开始页、标题页、主菜单、扉页、启动序列、启动失败页、T0、云隙望穹、片名块、新游戏过场、继续过场、开场动画、返回标题、跳过过场、安全落点、开场过场设置、过场专属元素、云卡、放映层、标题卡、IAppFlow、AppState、EnterRequest、CinematicPlayer、CinematicDef、cine_cloud.gdshader、enter_new.json、game/data/maps/qingshuihe/cinematics/

- SYS-TITLE §0 结论 · L25 · 3.2k
  - ★ SYS-TITLE §0.1 接入要点 · L36 · 1.9k
- SYS-TITLE §1 启动序列（SYS-VIEW §3 的启动流程以本节为准） · L51 · 6.1k
  - SYS-TITLE §1.1 步骤 · L53 · 3.7k
  - SYS-TITLE §1.2 目标 · L83 · 482
  - SYS-TITLE §1.3 扉页 · L97 · 572
  - SYS-TITLE §1.4 接线（`Campus.Contracts.Startup` 与 `IAppBootFlow`） · L105 · 1.3k
- SYS-TITLE §2 开始页 · L125 · 4.0k
  - SYS-TITLE §2.1 T0 镜头"云隙望穹" · L127 · 1.2k
  - SYS-TITLE §2.2 菜单 · L145 · 1.5k
  - SYS-TITLE §2.3 片名块 · L161 · 778
  - SYS-TITLE §2.4 首次启动与再次启动 · L177 · 589
- SYS-TITLE §3 新游戏过场 · L189 · 4.5k
  - SYS-TITLE §3.1 分镜表 · L191 · 2.3k
  - SYS-TITLE §3.2 字幕、时间读数与标题卡 · L212 · 1.1k
  - SYS-TITLE §3.3 过场专属元素 · L231 · 690
  - SYS-TITLE §3.4 简短版 · L248 · 412
- SYS-TITLE §4 继续过场与返回标题 · L261 · 1.7k
  - SYS-TITLE §4.1 继续（约 9 s） · L263 · 1.0k
  - SYS-TITLE §4.2 返回标题（约 3 s） · L280 · 628
- SYS-TITLE §5 跳过、安全落点与设置项 · L295 · 1.4k
  - SYS-TITLE §5.1 跳过 · L297 · 323
  - SYS-TITLE §5.2 安全落点 · L304 · 457
  - SYS-TITLE §5.3 设置项"开场过场" · L317 · 642
- SYS-TITLE §6 与时间不可逆的关系 · L331 · 1.1k
- SYS-TITLE §7 `IAppFlow` 状态机 · L353 · 5.1k
  - SYS-TITLE §7.1 状态与转移 · L355 · 1.5k
  - SYS-TITLE §7.2 各状态下的约定 · L381 · 918
  - SYS-TITLE §7.3 交还控制 · L400 · 351
  - SYS-TITLE §7.4 契约 · L409 · 2.3k
- SYS-TITLE §8 过场数据格式与 `CinematicPlayer` · L447 · 7.6k
  - SYS-TITLE §8.1 文件 · L449 · 521
  - SYS-TITLE §8.2 结构 · L463 · 3.4k
  - SYS-TITLE §8.3 `CinematicPlayer` · L528 · 2.0k
  - SYS-TITLE §8.4 检查（G2 的一部分，纯 .NET） · L557 · 647
  - SYS-TITLE §8.5 放映层（WP-35d） · L568 · 261
  - SYS-TITLE §8.6 音频挂点 · L575 · 692
- SYS-TITLE §9 云卡 `cine_cloud.gdshader` · L586 · 843
- SYS-TITLE §10 流式预取 · L599 · 751
- SYS-TITLE §11 性能与门禁 · L612 · 2.0k
  - SYS-TITLE §11.1 预算 · L614 · 365
  - SYS-TITLE §11.2 门禁增项（写进 ENG-GATES §2） · L625 · 1.3k
  - SYS-TITLE §11.3 体验脚本（写进 ENG-PERF §12.4） · L641 · 197
  - SYS-TITLE §11.4 静态预算、性能回归与自适应画质 · L645 · 202
- SYS-TITLE §12 多地图 · L651 · 803
- SYS-TITLE §13 不做的事 · L665 · 355
- SYS-TITLE §14 工作包分工 · L679 · 1.1k
- SYS-TITLE §16 待验证（P0） · L698 · 724

## SYS-VIEW · 观察模式、介质与开场

`docs/design/systems/SYS-VIEW.md` · 29.0k 字 · 观察模式总述与旁观漫游、行走的第一与第三人称镜头、镜头在楼里地下水下雾海的介质呈现、启动开场与署名、旧网页栈归档
关键词：旁观模式、第一人称、观察模式、飞行速度档、视场角、鼠标灵敏度、初始位置、第三人称、命令行参数、--pose、--title=0、镜头不会自己动、自动运镜、启动预热、引擎启动画面、介质呈现、剖切变体、楼里、地下、水下、半入水、雾海、ICameraRig、Pose、IMediumState、MediumKind、medium_overlay.gdshader、OSM 署名、LICENSES.txt、旧网页栈归档

  - SYS-VIEW 分工 · L23 · 348
- SYS-VIEW §0 结论 · L32 · 2.8k
  - ★ SYS-VIEW §0.1 接入要点 · L47 · 1.4k
- SYS-VIEW §1 旧网页栈的归档（WP-00） · L64 · 1.6k
- SYS-VIEW §2 旁观者漫游（WP-06） · L90 · 9.2k
  - SYS-VIEW §2.1 操作 · L94 · 2.0k
  - SYS-VIEW §2.2 触屏操作 · L129 · 30
  - SYS-VIEW §2.3 活动范围 · L134 · 1.6k
  - SYS-VIEW §2.4 初始位置 · L155 · 869
  - SYS-VIEW §2.5 镜头不会自己动 · L165 · 2.1k
  - SYS-VIEW §2.6 操作说明与提示 · L193 · 340
  - SYS-VIEW §2.7 行走模式 · L202 · 2.1k
- SYS-VIEW §3 启动与开场（WP-01；流程以 SYS-TITLE §1 为准） · L239 · 2.9k
- SYS-VIEW §4 环境痕迹与平面地图的分工 · L270 · 758
- SYS-VIEW §5 介质呈现：镜头在楼里、地下、水下、雾海里时（WP-07） · L283 · 5.0k
  - SYS-VIEW §5.1 楼里 · L305 · 1.7k
  - SYS-VIEW §5.2 地下 · L328 · 502
  - SYS-VIEW §5.3 水下 · L339 · 772
  - SYS-VIEW §5.4 雾海 · L350 · 281
- SYS-VIEW §6 署名、窗口标题与图标（WP-01） · L358 · 1.8k
- SYS-VIEW 交付验收 · L378 · 1.7k
- SYS-VIEW §7 待验证（P0） · L407 · 772

## SYS-WALK · 行走模式：手感、可行走范围、边界、碰撞与角色控制器

`docs/design/systems/SYS-WALK.md` · 59.7k 字 · 行走模式的手感、玩家身体、游泳、骑行、模式切换、可行走范围与边界、开放室内的门与电梯、角色控制器、碰撞数据与验收
关键词：行走模式、游泳、角色控制器、手感参数、模式切换、可行走范围、可行走遮罩、边界收口、隐形墙、校界缓停、软墙提示、安全点、回位、涉水、玩家身体、开放泳区、碰撞级 C、碰撞就绪、自动登阶、电梯关门换位、推荐路线、步相、WalkClass、SurfaceTag、Collider、ICollisionWorld、IViewControl、IWalkTelemetry、walkable.overlay.json、routes.json

  - SYS-WALK 分工 · L23 · 414
- SYS-WALK §0 结论 · L31 · 4.0k
  - ★ SYS-WALK §0.1 接入要点 · L44 · 2.4k
- SYS-WALK §1 两种模式的关系 · L64 · 1.9k
- SYS-WALK §2 手感参数 · L81 · 1.6k
- SYS-WALK §3 视角与舒适度 · L110 · 966
- SYS-WALK §4 键位 · L131 · 921
- SYS-WALK §5 模式切换（WP-42） · L152 · 2.3k
- SYS-WALK §6 可行走范围（WP-44） · L197 · 2.3k
- SYS-WALK §7 边界收口（WP-44，各区配合） · L220 · 2.9k
- SYS-WALK §8 楼内（WP-17、WP-31a–c、WP-51、WP-07） · L257 · 6.2k
- SYS-WALK §9 水、游泳、回位与安全点（WP-42、WP-44） · L339 · 2.5k
- SYS-WALK §10 行走走廊与节奏（配合 ART-FIDELITY） · L382 · 1.5k
- SYS-WALK §11 推荐路线 · L419 · 3.0k
- SYS-WALK §12 角色控制器（WP-42） · L446 · 7.1k
- SYS-WALK §13 碰撞数据（WP-43） · L534 · 10.9k
- SYS-WALK §14 碰撞就绪、进楼衔接与性能 · L665 · 828
- SYS-WALK §15 验收 · L678 · 3.2k
- SYS-WALK §16 骑行（WP-42、WP-55） · L718 · 2.5k
- SYS-WALK §17 待验证（P0） · L759 · 1.9k

## SYS-WEATHER · 天气：天气清单、画面规则、前向随机过程、过渡与验收

`docs/design/systems/SYS-WEATHER.md` · 25.9k 字 · 八格天气的画面与声音规则、自动天气的前向随机过程、各系统的实现、状态与契约、存档段与验收
关键词：天气、天气格、自动天气、手动天气、天气向量、WeatherNow、WeatherKind、WeatherVec、WeatherSpan、IWeatherProcess、WeatherProcessState、ILightningFeed、LightningFlash、前向随机过程、前瞻队列、天气预报、天气种子、天气修正量、太阳雨、雨的可读性、闪电、彩虹、降水池、precip.gdshader、precip_occ、湿润、积水、地面遮罩 2、refl_lamps、天气巡回

- SYS-WEATHER §0 结论 · L24 · 2.0k
  - ★ SYS-WEATHER §0.1 接入要点 · L33 · 1.4k
- SYS-WEATHER §1 天气清单与界面格 · L48 · 780
- SYS-WEATHER §2 每种天气的画面规则 · L68 · 3.6k
- SYS-WEATHER §3 雨的可读性规则 · L129 · 995
- SYS-WEATHER §4 与四季、昼夜的配合 · L142 · 266
- SYS-WEATHER §5 闪电与彩虹 · L150 · 1.3k
- SYS-WEATHER §6 自动天气：前向随机过程（WP-45a） · L159 · 4.0k
  - SYS-WEATHER §6.1 模型 · L161 · 374
  - SYS-WEATHER §6.2 硬规则（生成时满足，G2 的 lint 复查） · L169 · 304
  - SYS-WEATHER §6.3 参数初值 · L177 · 939
  - SYS-WEATHER §6.4 随机数与复现 · L194 · 846
  - SYS-WEATHER §6.5 时间与天气 · L205 · 706
  - SYS-WEATHER §6.6 固定点 · L218 · 805
- SYS-WEATHER §7 光照、云与降水的实现（WP-45 及各系统包） · L226 · 2.8k
- SYS-WEATHER §8 状态、参数与接口 · L249 · 4.6k
- SYS-WEATHER §9 验收 · L313 · 1.4k
- SYS-WEATHER §10 工作包 · L326 · 1.3k
- SYS-WEATHER §12 待验证（P0） · L335 · 585

## UI-NAV · 界面、点位跳转与操作说明

`docs/design/ui/UI-NAV.md` · 52.6k 字 · 界面的内容与交互：组件、快捷键、点位清单、跳转与传送过渡、无障碍、实现与性能
关键词：界面交互、快捷键、键位、改键、输入优先级、界面态、漫游态、鼠标捕获、自动隐藏、时间环操作、天气签条、速度档、临时提示、到达标题、设置项、首次引导、点位、跳转点、Waypoint、LocalizedName、waypoints.json、跳转、飞过去、无障碍、读屏、文本键、翻译表、地名来源、settings.cfg、input.cfg

  - UI-NAV 分工 · L22 · 194
- UI-NAV §0 结论 · L28 · 2.8k
  - ★ UI-NAV §0.1 接入要点 · L36 · 1.9k
- UI-NAV §1 允许什么、禁止什么 · L52 · 1.1k
- UI-NAV §2 视觉风格（WP-35；以 UI-VISUAL 为准） · L71 · 1.3k
- UI-NAV §3 布局 · L83 · 2.3k
- UI-NAV §4 组件 · L131 · 14.4k
  - UI-NAV §4.1 时间环（时间） · L135 · 2.0k
  - UI-NAV §4.2 季节 · L177 · 461
  - UI-NAV §4.3 速度 · L187 · 462
  - UI-NAV §4.4 观察模式 · L201 · 543
  - UI-NAV §4.5 点位目录（大地图的目录页） · L216 · 504
  - UI-NAV §4.6 操作说明与设置 · L227 · 3.8k
  - UI-NAV §4.7 首次进入 · L283 · 268
  - UI-NAV §4.8 到达标题 · L291 · 521
  - UI-NAV §4.9 临时提示 · L303 · 1.4k
  - UI-NAV §4.10 天气格 · L330 · 2.5k
  - UI-NAV §4.11 小地图（摘要） · L383 · 669
  - UI-NAV §4.12 大地图（摘要） · L395 · 645
  - UI-NAV §4.13 开始页、存档页与放映层（摘要） · L406 · 664
- UI-NAV §5 鼠标捕获、界面态与快捷键（WP-35，WP-06 配合） · L416 · 5.6k
- UI-NAV §6 点位清单（WP-36，`game/data/maps/qingshuihe/waypoints.json`） · L517 · 6.6k
- UI-NAV §7 跳转过渡（WP-36） · L608 · 4.2k
  - UI-NAV §7.1 跳转方式 · L610 · 1.0k
  - UI-NAV §7.2 镜头朝向 · L630 · 155
  - UI-NAV §7.3 到达 · L639 · 321
  - UI-NAV §7.4 打断 · L646 · 285
  - UI-NAV §7.5 预取与速度律豁免 · L654 · 697
  - UI-NAV §7.6 减少动态效果 · L670 · 236
  - UI-NAV §7.7 为什么飞过去，又为什么有传送 · L674 · 388
  - UI-NAV §7.8 传送（地图） · L680 · 1.1k
- UI-NAV §8 无障碍与语言（WP-35） · L699 · 3.6k
- UI-NAV §9 实现与性能（WP-35） · L736 · 5.9k
- UI-NAV §10 验收 · L795 · 1.3k
- UI-NAV §12 待验证（P0） · L816 · 1.0k

## UI-VISUAL · 界面视觉语言：令牌、字体、形状、材质、图标、组件、画面布局、动效与实现

`docs/design/ui/UI-VISUAL.md` · 74.4k 字 · 界面视觉规范：色彩令牌与两套主题、字体、形状材质、图标、组件、布局、动效、Godot 实现与验收
关键词：界面视觉、色彩令牌、主题色、清蓝、晶白、深屏、深色主题、昼夜交班、常驻件、界面字体、切角、发丝线、过孔灯、衍射光边、焦点环、界面图标、窗口图标、动效母题、界面动画、减少动态效果、辉光、等待灯、界面缩放、ui.json、ui_theme.tres、ui_panel.gdshader、ui_screen.gdshader、ui_night、IMotionSettings、IPaletteProbe

  - UI-VISUAL 分工 · L22 · 376
- UI-VISUAL §0 结论 · L31 · 3.9k
  - ★ UI-VISUAL §0.1 接入要点 · L47 · 2.1k
- UI-VISUAL §1 设计原则 · L62 · 1.8k
  - UI-VISUAL §1.1 母题从哪里来 · L64 · 273
  - UI-VISUAL §1.2 七条硬规则 · L70 · 393
  - UI-VISUAL §1.3 专业与"土"的对照 · L80 · 1.1k
- UI-VISUAL §2 与三渲二画面的配合与色彩分工 · L97 · 3.4k
  - UI-VISUAL §2.1 场景的色彩坐标 · L99 · 1.0k
  - UI-VISUAL §2.2 色彩分工 · L120 · 800
  - UI-VISUAL §2.3 界面色与场景色不冲突的七条规则 · L135 · 547
  - UI-VISUAL §2.4 高调画面上的面板与常驻件 · L145 · 1.1k
- UI-VISUAL §3 令牌 · L159 · 10.2k
  - UI-VISUAL §3.1 清蓝阶梯（OKLCH，sRGB 色域内） · L163 · 1.2k
  - UI-VISUAL §3.2 中性：冷灰阶梯（H 248–262，C ≤ 0.022） · L184 · 662
  - UI-VISUAL §3.3 专用色：墨、深屏底、夜字、暖色点 · L203 · 650
  - UI-VISUAL §3.4 语义色 · L216 · 375
  - UI-VISUAL §3.5 两套主题的语义令牌 · L227 · 1.4k
  - UI-VISUAL §3.6 昼夜切换 · L253 · 994
  - UI-VISUAL §3.7 关键组合的对比度（WCAG 2.2：文字 ≥ 4.5:1，图形与控件 ≥ 3:1） · L271 · 1.6k
  - UI-VISUAL §3.8 登记：`game/style/palette/ext/ui.json` · L306 · 3.3k
- UI-VISUAL §4 字体与字号阶梯 · L359 · 2.3k
  - UI-VISUAL §4.1 字体与许可 · L361 · 587
  - UI-VISUAL §4.2 回退链与数字 · L374 · 662
  - UI-VISUAL §4.3 字号阶梯（1920×1080 基准的逻辑像素，随界面缩放） · L383 · 1.0k
- UI-VISUAL §5 形状与材质 · L403 · 2.7k
  - UI-VISUAL §5.1 网格、间距与尺寸 · L405 · 283
  - UI-VISUAL §5.2 切角：1 号脚 · L411 · 319
  - UI-VISUAL §5.3 发丝线与刻度 · L426 · 202
  - UI-VISUAL §5.4 过孔灯（状态点） · L432 · 149
  - UI-VISUAL §5.5 走线 · L436 · 158
  - UI-VISUAL §5.6 衍射光边 · L440 · 388
  - UI-VISUAL §5.7 面板材质 · L449 · 696
  - UI-VISUAL §5.8 阴影与辉光 · L461 · 334
  - UI-VISUAL §5.9 明确不做 · L467 · 168
- UI-VISUAL §6 图标体系 · L475 · 1.6k
  - UI-VISUAL §6.1 图标规格：网格、线宽、颜色与文件 · L477 · 438
  - UI-VISUAL §6.2 意象表 · L485 · 876
  - UI-VISUAL §6.3 窗口图标"八角晶片" · L516 · 297
- UI-VISUAL §7 组件与六种状态 · L523 · 7.4k
  - UI-VISUAL §7.1 六种状态（"通电"隐喻） · L525 · 491
  - UI-VISUAL §7.2 按钮 · L536 · 653
  - UI-VISUAL §7.3 输入控件 · L546 · 834
  - UI-VISUAL §7.4 容器与反馈 · L558 · 978
  - UI-VISUAL §7.5 时间环 · L569 · 1.8k
  - UI-VISUAL §7.6 季节与天气签条 · L599 · 494
  - UI-VISUAL §7.7 速度条与观察模式 · L610 · 378
  - UI-VISUAL §7.8 到达标题与压在画面上的字 · L615 · 368
  - UI-VISUAL §7.9 存档卡："存储模组" · L624 · 1.0k
  - UI-VISUAL §7.10 等待灯与存储指示 · L653 · 370
- UI-VISUAL §8 画面清单与布局 · L658 · 12.5k
  - UI-VISUAL §8.1 画面清单：出现时机、主题与压暗 · L662 · 940
  - UI-VISUAL §8.2 漫游态 · L678 · 1.2k
  - UI-VISUAL §8.3 界面态 · L699 · 1.6k
  - UI-VISUAL §8.4 大地图（框架） · L725 · 1.1k
  - UI-VISUAL §8.5 设置卡（含画面页、声音页） · L745 · 1.8k
  - UI-VISUAL §8.6 开始页（界面部分；流程以 SYS-TITLE 为准） · L771 · 1.8k
  - UI-VISUAL §8.7 存档页（界面部分；存档语义以 SYS-SAVE 为准） · L797 · 1.2k
  - UI-VISUAL §8.8 确认对话框与临时提示 · L818 · 350
  - UI-VISUAL §8.9 过场字幕与标题卡（视觉部分；时长、镜头、跳过逻辑以 SYS-TITLE 为准） · L832 · 659
  - UI-VISUAL §8.10 加载与等待 · L842 · 232
  - UI-VISUAL §8.11 制作人员与许可（"数据手册"样式） · L848 · 1.1k
  - UI-VISUAL §8.12 以后的多地图与 RPG（预留，只定视觉） · L869 · 338
- UI-VISUAL §9 动效语言 · L877 · 7.4k
  - UI-VISUAL §9.1 动效原则 · L879 · 302
  - UI-VISUAL §9.2 两种时间 · L888 · 254
  - UI-VISUAL §9.3 八个母题 · L893 · 1.8k
  - UI-VISUAL §9.4 频率分级 · L906 · 656
  - UI-VISUAL §9.5 曲线 · L923 · 885
  - UI-VISUAL §9.6 时长表 · L939 · 985
  - UI-VISUAL §9.7 进入与退出、交错、打断 · L959 · 247
  - UI-VISUAL §9.8 辉光与闪烁的上限 · L965 · 491
  - UI-VISUAL §9.9 关键编排 · L981 · 1.8k
- UI-VISUAL §10 减少动态效果与无障碍 · L1001 · 1.4k
  - UI-VISUAL §10.1 "减少动态效果"下的替代 · L1003 · 847
  - UI-VISUAL §10.2 无障碍 · L1028 · 551
- UI-VISUAL §11 Godot 实现 · L1040 · 12.7k
  - UI-VISUAL §11.1 分层与 `hdr_2d` · L1042 · 662
  - UI-VISUAL §11.2 目录与类 · L1052 · 1.8k
  - UI-VISUAL §11.3 `ui.json` 与 Theme 的生成 · L1084 · 1.2k
  - UI-VISUAL §11.4 StyleBox、切角与发丝线 · L1097 · 1.1k
  - UI-VISUAL §11.5 两个界面着色器（白名单） · L1115 · 1.4k
  - UI-VISUAL §11.6 主题切换：`ui_night` 的锁存与扫描前沿 · L1130 · 934
  - UI-VISUAL §11.7 动效的驱动 · L1140 · 1.5k
  - UI-VISUAL §11.8 字体与 MSDF · L1152 · 787
  - UI-VISUAL §11.9 预热与预建 · L1162 · 718
  - UI-VISUAL §11.10 界面缩放公式 · L1172 · 938
  - UI-VISUAL §11.11 契约增量（写进 ENG-ARCH §3） · L1189 · 982
  - UI-VISUAL §11.12 命令行与组件画廊（写进 ENG-GATES §1） · L1212 · 607
- UI-VISUAL §12 性能 · L1224 · 1.3k
- UI-VISUAL §13 验收 · L1243 · 2.5k
- UI-VISUAL §15 待验证（P0） · L1276 · 608

## ENG-ARCH · 工程架构：模块边界、接口契约与并行开发流程

`docs/design/engineering/ENG-ARCH.md` · 187.9k 字 · 目录与归属、程序集边界、全部 C# 契约、帧顺序、资源管线、桩与工作树流程
关键词：工程架构、目录归属、ownership.json、程序集边界、禁用写法、BannedApiAnalyzers、ArchUnitNET、确定性、DetMath、StableHash、自动发现、特性注册、ContentModule、KernelNode、KernelService、Autoload、契约、契约冻结、PublicApiAnalyzers、ContractJson、桩、占位实现、资源管线、本机烘焙、.tbin、缓存键、工作树、git 流程、合并队列、net10.0

- ENG-ARCH §0 结论 · L67 · 2.3k
  - ★ ENG-ARCH §0.1 接入要点 · L79 · 1.3k
- ENG-ARCH §1 旧网页栈怎么处理 · L93 · 5.1k
- ENG-ARCH §2 目录结构与归属 · L156 · 34.0k
- ENG-ARCH §3 关键接口（契约草案） · L475 · 102.3k
- ENG-ARCH §4 资源管线 · L2056 · 10.6k
- ENG-ARCH §5 接口先定、桩先合入 · L2144 · 3.3k
- ENG-ARCH §6 git 工作树流程 · L2183 · 5.8k
- ENG-ARCH §7 主要风险与对策 · L2255 · 6.3k
- ENG-ARCH §8 扩展点（RPG） · L2308 · 8.6k
- ENG-ARCH §9 待验证（P0） · L2352 · 2.3k

## ENG-BUDGET · 性能预算与硬规则

`docs/design/engineering/ENG-BUDGET.md` · 54.7k 字 · 预设与设备档的预算、细节级别、各包额度总账（budgets.json）、合入硬规则、测法与工程设置
关键词：性能预算、预算、额度、份额、配额、额度总账、budgets.json、主代理余量、预设资源上限、设备档目标、帧率目标、最低配置、独显中、独显高、楼内视角额度、GPU 帧时分配、主线程额度、硬规则、合入规则、细节级别、L0+、分块、四叉树、速度律、屏幕误差、近裁剪面、导出包体、本机缓存预算、工程设置、project.godot 设置

- ENG-BUDGET §0 结论 · L23 · 2.4k
  - ★ ENG-BUDGET §0.1 接入要点 · L36 · 1.3k
- ENG-BUDGET §1 总体要点（引擎、管线与各系统的预算影响） · L51 · 3.1k
- ENG-BUDGET §2 预设与设备档的预算 · L77 · 7.2k
- ENG-BUDGET §3 细节级别与分块（统一定义） · L195 · 2.9k
- ENG-BUDGET §4 流式加载与速度 · L233 · 1.1k
- ENG-BUDGET §5 深度与近裁剪面 · L250 · 654
- ENG-BUDGET §6 阴影、后期、倒影、灯光 · L258 · 2.7k
- ENG-BUDGET §7 硬规则（违反任何一条都不能合入） · L278 · 7.5k
- ENG-BUDGET §8 额度总账（`budgets.json`） · L316 · 11.5k
- ENG-BUDGET §9 怎么测 · L451 · 4.0k
- ENG-BUDGET §10 工程设置清单 · L484 · 7.0k
- ENG-BUDGET §11 待验证（P0） · L586 · 2.8k

## ENG-GATES · 验收：门禁、审查视角、近景巡检、回报格式

`docs/design/engineering/ENG-GATES.md` · 57.4k 字 · 审查视角与命令行参数、合入门禁 G1–G21、一票否决、全局清单和回报格式
关键词：门禁、验收、测试、自查、测试节奏、测试三档、快检、核心专项、集中测试、gate.ps1、门禁编号、G1–G21、审查视角、截图默认串、命令行参数、用户参数、截图方式、截图就绪、流式屏障、录像、时季抽样、巡检点轮换、一票否决、全局清单、回报格式、回报模板、截图命名、review.json、output/render-review、GPU 锁

- ENG-GATES §0 结论 · L22 · 1.9k
  - ★ ENG-GATES §0.1 接入要点 · L38 · 850
- ENG-GATES §1 审查视角 · L52 · 14.6k
- ENG-GATES §2 门禁与测试节奏 · L195 · 29.5k
- ENG-GATES §3 一票否决 · L274 · 3.2k
- ENG-GATES §4 全局清单（收尾阶段由主代理逐项检查） · L303 · 2.6k
- ENG-GATES §5 回报格式 · L329 · 1.4k
- ENG-GATES §6 待验证（P0） · L358 · 1.8k

## ENG-ONBOARD · 子代理开工手册

`docs/design/engineering/ENG-ONBOARD.md` · 12.3k 字 · 子代理开工时唯一要通读的文档：开工环境、硬规则、查资料的方法与路由、完成与回报
关键词：开工手册、子代理、必读、查阅、按需查、信息获取、小节目录、SECTIONS.md、接入要点、硬规则、禁用写法、工作树、回报、CLAUDE.md、AGENTS.md

- ENG-ONBOARD §0 结论 · L26 · 675
  - ★ ENG-ONBOARD §0.1 接入要点 · L43 · 69
- ENG-ONBOARD §1 项目与当前阶段 · L47 · 603
- ENG-ONBOARD §2 开工环境 · L62 · 878
- ENG-ONBOARD §3 硬规则 · L78 · 4.5k
  - ENG-ONBOARD §3.1 改哪里 · L80 · 709
  - ENG-ONBOARD §3.2 代码边界与接入方式 · L91 · 1.5k
  - ENG-ONBOARD §3.3 禁用写法 · L121 · 1.4k
  - ENG-ONBOARD §3.4 内容与画面的底线 · L150 · 587
  - ENG-ONBOARD §3.5 文件操作 · L166 · 384
- ENG-ONBOARD §4 查资料 · L177 · 3.8k
  - ENG-ONBOARD §4.1 文档在哪、怎么找 · L179 · 858
  - ENG-ONBOARD §4.2 按要做的事查哪里 · L199 · 2.5k
  - ENG-ONBOARD §4.3 Godot API · L272 · 223
  - ENG-ONBOARD §4.4 文档不清楚或冲突时 · L278 · 219
- ENG-ONBOARD §5 完成与回报 · L286 · 461
- ENG-ONBOARD §6 改文档时 · L297 · 331

## ENG-PERF · 性能工程：高质量实现设计（Godot 4.7.2 .NET）

`docs/design/engineering/ENG-PERF.md` · 133.4k 字 · 性能的实现方法：合批、着色器白名单、全局参数登记、预热、剔除、流式、主线程、内存、阴影后期、自适应与测量
关键词：着色器白名单、全局着色器参数、全局数据纹理、ShaderGlobals、globals.gdshaderinc、合批、自动实例化、MultiMesh、绘制调用口径、管线预热、管线编译计数、顶点格式、抖动淡化、遮挡剔除、渲染图层、TileBundle、流式加载、卡帧、本机烘焙缓存、.tbin、零分配、显存账、PSSM 阴影、方向量化、灯光通道、后期链、描线、设备档、自动调节画质、性能基准

- ENG-PERF §0 十条总原则 · L24 · 4.2k
  - ★ ENG-PERF §0.1 接入要点 · L37 · 2.4k
- ENG-PERF §1 绘制管线与合批（WP-02，WP-01 配合） · L52 · 22.7k
  - ENG-PERF §1.1 容器选型规则 · L54 · 1.1k
  - ENG-PERF §1.2 MultiMesh 与 RenderingServer 挂载的用法 · L74 · 3.6k
  - ENG-PERF §1.3 调用口径 · L113 · 716
  - ENG-PERF §1.4 着色器白名单 · L126 · 6.8k
  - ENG-PERF §1.5 全局参数与数据纹理表 · L196 · 10.1k
  - ENG-PERF §1.6 排序与绘制顺序 · L306 · 373
- ENG-PERF §2 着色器与编译（WP-04、WP-05、WP-01） · L313 · 7.0k
- ENG-PERF §3 填充率与带宽（WP-05、WP-23） · L377 · 1.6k
  - ENG-PERF §3.1 渲染目标格式 · L379 · 220
  - ENG-PERF §3.3 过度绘制 · L383 · 830
  - ENG-PERF §3.4 动态分辨率 · L395 · 526
- ENG-PERF §4 剔除（WP-02、WP-33、WP-07） · L402 · 2.6k
- ENG-PERF §5 流式加载（WP-02） · L443 · 13.9k
- ENG-PERF §6 主线程与 C#（WP-01） · L603 · 4.0k
- ENG-PERF §7 内存与显存（WP-01、WP-02） · L659 · 3.0k
- ENG-PERF §8 安装包与磁盘布局（WP-01、WP-02、WP-04、WP-27） · L703 · 4.9k
- ENG-PERF §9 植被（WP-23） · L762 · 10.7k
- ENG-PERF §10 阴影、光照与后期（WP-03、WP-05、WP-24） · L928 · 24.3k
  - ENG-PERF §10.1 阴影（WP-03，引擎原生 PSSM） · L930 · 8.1k
  - ENG-PERF §10.2 后期链（WP-05） · L1042 · 10.0k
  - ENG-PERF §10.3 窗灯与路灯（WP-03 负责通道与着色，WP-39 负责时刻表） · L1127 · 4.3k
  - ENG-PERF §10.4 倒影（WP-24） · L1172 · 1.9k
- ENG-PERF §11 设备档、画面预设与自适应（WP-32，WP-01 配合） · L1192 · 10.5k
  - ENG-PERF §11.1 设备档 → 推荐预设 · L1196 · 782
  - ENG-PERF §11.2 首次分档（扉页后面完成，不超过 2.5 s） · L1210 · 1.5k
  - ENG-PERF §11.3 运行时自适应（自动调节画质） · L1227 · 2.2k
  - ENG-PERF §11.4 帧节奏与输入 · L1272 · 1.2k
  - ENG-PERF §11.5 Windows 平台的已知问题 · L1283 · 447
  - ENG-PERF §11.6 集成显卡 · L1291 · 486
  - ENG-PERF §11.7 画面设置（WP-32a、32b，WP-01、WP-35 配合） · L1299 · 3.7k
- ENG-PERF §12 测量、剖析与门禁（WP-27，WP-01 提供接口） · L1354 · 12.0k
  - ENG-PERF §12.1 测量机器 · L1356 · 282
  - ENG-PERF §12.2 运行时测量 · L1360 · 1.6k
  - ENG-PERF §12.3 剖析工具 · L1374 · 408
  - ENG-PERF §12.4 自动化基准 · L1385 · 4.1k
  - ENG-PERF §12.5 门禁（写入 ENG-GATES §2） · L1436 · 4.3k
  - ENG-PERF §12.6 日志与诊断（WP-01；WP-02、WP-32、WP-50 提供读数，WP-35 提供入口） · L1490 · 1.2k
- ENG-PERF §13 D3D12 与 Vulkan 的选择 · L1502 · 776
- ENG-PERF §14 每个工作包的性能自检清单 · L1528 · 2.0k
- ENG-PERF §16 待验证（P0） · L1550 · 6.0k

## ENG-ROADMAP · 扩展路线：从观光到 RPG

`docs/design/engineering/ENG-ROADMAP.md` · 24.7k 字 · 规定观光版给以后 RPG 留的扩展点、现在不能写死的地方和 v1 冻结前的最小预留
关键词：扩展路线、从观光到 RPG、RPG 扩展、扩展点、扩展原则、规则集、sightseeing、观光规则集、观光禁令、观者状态、输入占用按原因计、枚举的穷举纪律、二值判断、偶发事件数据化、名字占位、RPG 预留目录、实体 ID 前缀、存档预留段名、v1 冻结前的预留、写法纪律、帧顺序草案、RPG 子系统挂点、角色资产、VRoid、VRM、规则集预算、RPG 开工前待办、必需段组

- ENG-ROADMAP §0 结论 · L23 · 1.6k
  - ★ ENG-ROADMAP §0.1 接入要点 · L36 · 744
- ENG-ROADMAP §1 扩展原则 · L47 · 1.1k
- ENG-ROADMAP §2 现有扩展点清单 · L63 · 4.1k
- ENG-ROADMAP §3 现在写死、以后会挡路的地方 · L85 · 4.6k
- ENG-ROADMAP §4 RPG 可能新增的子系统与挂点 · L124 · 4.3k
- ENG-ROADMAP §5 预算与性能余量 · L161 · 1.3k
- ENG-ROADMAP §6 v1 冻结前的预留 · L186 · 4.4k
  - ENG-ROADMAP §6.1 清单 · L188 · 3.7k
  - ENG-ROADMAP §6.2 不需要现在预留的（以后只增即可） · L201 · 685
- ENG-ROADMAP §7 RPG 开工前待办 · L218 · 1.6k

## ENG-STATUS · 施工状态

`docs/design/engineering/ENG-STATUS.md` · 19.8k 字 · 主代理维护的当前施工状态：main、最近可运行入口、活跃工作包的进度和待派工队列
关键词：施工状态、施工进度、当前 main、最近可运行入口、活跃工作包、已冻结的契约组、契约冻结记录、候选提交、待合入、已集成、交接、接手、换会话、换账号、阻塞、下一步、待派工队列、待处理流水线、调研入队流水线、Q-nnn、排队、可派、已派

- ENG-STATUS §0 结论 · L23 · 569
  - ★ ENG-STATUS §0.1 接入要点 · L30 · 287
- ENG-STATUS §1 填写规则 · L37 · 932
- ENG-STATUS §2 当前 main、可运行入口与已冻结契约 · L61 · 11.7k
- ENG-STATUS §3 活跃工作包 · L106 · 3.4k
- ENG-STATUS §4 待派工队列 · L135 · 2.4k

## ENG-TEAM · 多模型协作规程：分工、额度、通信与开发流程

`docs/design/engineering/ENG-TEAM.md` · 34.0k 字 · 作者、主会话、二号会话、Astra 与两个固定 Sol 会话的参与者表，按模型能力与任务难度的分工，包的归属与开工顺序，一个包从派工到合入的流程，.collab 交接与消息，三个账号的额度，值班、完成度调研与旁路任务
关键词：多模型协作、协作规程、参与者、会话、账号、作者、主会话、总指挥、主代理、决定权、二号会话、project-09、wendystar1145、Max 20x、Opus、Sonnet、Astra、GPT-6 Astra、gpt-6-astra、gpt-6.1-sol、Sol 值班会话、Sol 旁路会话、旁路单、调研单、side/cards、ROLE.md、Codex、codex exec、thread、模型能力、视觉风格归属、审美、工程负责人、模型等级、任务难度、补 GPT 短板、包的归属、开工分波、D0、第一波、第二波、合并顺序、开发流程、派工、任务单、max 单、通用简报、建工作树、施工、回报、初步核对、评审、Opus 评审、交叉评审、可调参数、批准行、批准合入、带条件的批准行、合并执行、squash、施工状态、rebase 通知、契约申请、共享工程文件、合并请求、评审请求、.collab、merged.log、流水、消息频率、确认框、转问作者、唤醒脚本、sol_duty_watch、sol_side_runner、上下文重置、额度、5 小时窗口、并发公式、记账、快速模式、普通速度、service_tier、主会话移交、交接单、HANDOVER.md、goal 模式、get_usage、完成度调研、持续调研、调研入队流水线、待派工队列、调研索引、INDEX.md、00-总览、Opus 审计、巡检、spawn_agent、子代理、计数锁

- ENG-TEAM §0 结论 · L37 · 3.2k
  - ★ ENG-TEAM §0.1 接入要点 · L54 · 1.5k
- ENG-TEAM §1 参与者与会话 · L71 · 2.0k
  - ENG-TEAM §1.1 决定权与执行人 · L89 · 434
- ENG-TEAM §2 按模型能力分工 · L107 · 1.6k
  - ENG-TEAM §2.1 各模型的长处与短板 · L111 · 604
  - ENG-TEAM §2.2 硬规则：GPT 模型不做审美工作 · L120 · 457
  - ENG-TEAM §2.3 事归谁 · L129 · 233
  - ENG-TEAM §2.4 交叉的地方 · L135 · 205
- ENG-TEAM §3 模型等级与任务划分 · L141 · 2.9k
  - ENG-TEAM §3.1 按七问选施工模型 · L145 · 1.6k
  - ENG-TEAM §3.2 总指挥只做什么 · L174 · 465
  - ENG-TEAM §3.3 Astra 名下交给 6.1-sol 子代理的包 · L188 · 716
  - ENG-TEAM §3.4 包内的实现路径由施工模型规划 · L202 · 110
- ENG-TEAM §4 包的归属与开工顺序 · L207 · 2.5k
  - ENG-TEAM §4.1 包的归属 · L209 · 1.2k
  - ENG-TEAM §4.2 开工顺序与合并顺序 · L224 · 1.3k
- ENG-TEAM §5 开发流程 · L238 · 7.0k
  - ENG-TEAM §5.1 全流程一览 · L242 · 610
  - ENG-TEAM §5.2 派工 · L256 · 1.5k
  - ENG-TEAM §5.3 施工与回报 · L284 · 779
  - ENG-TEAM §5.4 评审：审美与契约的交叉评审 · L303 · 639
  - ENG-TEAM §5.5 批准与合并 · L315 · 1.3k
  - ENG-TEAM §5.6 合入以后：施工状态与 rebase · L336 · 502
  - ENG-TEAM §5.7 契约申请与共享工程文件 · L347 · 475
  - ENG-TEAM §5.8 调研入队流水线 · L355 · 1.2k
- ENG-TEAM §6 会话通信与工作交接 · L373 · 5.2k
  - ENG-TEAM §6.1 交接目录与文件 · L377 · 1.7k
  - ENG-TEAM §6.2 命名与写法 · L402 · 304
  - ENG-TEAM §6.3 任务单与请求文件的格式 · L409 · 534
  - ENG-TEAM §6.4 消息与流水 · L427 · 714
  - ENG-TEAM §6.5 唤醒脚本 · L446 · 1.4k
  - ENG-TEAM §6.6 上下文重置后怎么接着做 · L473 · 542
- ENG-TEAM §7 账号与额度管理 · L486 · 2.9k
  - ENG-TEAM §7.1 主账号（主会话） · L494 · 133
  - ENG-TEAM §7.2 二号会话的 Max 20x 账号 · L499 · 1.5k
  - ENG-TEAM §7.3 Codex 账号（Astra 与 Sol） · L526 · 231
  - ENG-TEAM §7.4 主会话移交 · L532 · 765
- ENG-TEAM §8 旁路任务与长期值班 · L554 · 4.4k
  - ENG-TEAM §8.1 Sol 值班会话 · L558 · 720
  - ENG-TEAM §8.2 完成度调研 · L574 · 1.7k
  - ENG-TEAM §8.3 Sol 旁路会话 · L616 · 828
  - ENG-TEAM §8.4 怎么开子代理 · L636 · 1.1k
- ENG-TEAM §9 待验证（P0） · L663 · 229

## ENG-WP · 工作包派工单：56 个工作包，可并行 55 个

`docs/design/engineering/ENG-WP.md` · 109.0k 字 · 56 个工作包的划分、独占目录、起读文档、交付与额度，各包主线版范围，细节阶段待办与候选包，开发阶段与闸口，子代理简报与主代理职责
关键词：工作包、派工单、派工、独占目录、起读文档、起读小节、交付条目、额度、规模、依赖、主线版、主线可见结果、验收重点、后置细节、细节阶段待办、候选包、观光照片留存、阶段与闸口、主线闸口、D0 骨架、阶段名 P0–P3、拆包、子包、并行度、派工简报、子代理简报、简报模板、主代理职责、开放室内清单审批、插件清单、ownership.json、third_party.json、待验证（P0）、目标框架

- ENG-WP §0 结论 · L25 · 1.7k
  - ★ ENG-WP §0.1 接入要点 · L39 · 737
- ENG-WP §1 阶段与闸口 · L52 · 6.6k
- ENG-WP §2 工作包一览 · L96 · 89.2k
  - ENG-WP §2.1 各包主线版的范围 · L168 · 14.5k
  - ENG-WP §2.2 细节阶段待办 · L240 · 3.3k
  - ENG-WP §2.3 候选包 · L279 · 255
- ENG-WP §3 还能再拆的包（并行度提到 80 左右） · L287 · 3.9k
- ENG-WP §4 子代理通用简报模板 · L312 · 630
- ENG-WP §5 主代理的职责 · L330 · 3.2k
- ENG-WP §6 待验证（P0） · L350 · 1.2k

## REF-PROMPT · 完整提示词（备份）

`docs/design/reference/REF-PROMPT.md` · 12.6k 字 · 作品《电光年华》的完整描述，用作提示词，供通读全貌；不是施工文档

- REF-PROMPT 提示词正文 · L20 · 12.1k

## TOOL-PREVIEW · 内容预览

`docs/design/tools/TOOL-PREVIEW.md` · 4.7k 字 · 开发构建里预览与回看内容：按实体定位、保留机位与时季天气、复制机位、重读数据
关键词：内容预览、预览、回看、看效果、开发构建、--focus、--pose-file、按实体定位、实体 ID、机位、复制机位、机位文件、last-pose.json、F5 重读数据、F6 复制机位、F7 重启、重启后恢复机位、校验不过、保留上一次有效画面、预览只读不写

  - TOOL-PREVIEW 分工 · L23 · 264
- TOOL-PREVIEW §0 结论 · L30 · 1.0k
  - ★ TOOL-PREVIEW §0.1 接入要点 · L37 · 691
- TOOL-PREVIEW §1 启动 · L48 · 741
- TOOL-PREVIEW §2 按实体 ID 定位 · L65 · 297
- TOOL-PREVIEW §3 记下机位 · L71 · 528
- TOOL-PREVIEW §4 改了以后回看 · L83 · 746

## OPS-RELEASE · 发布与交付

`docs/design/ops/OPS-RELEASE.md` · 7.8k 字 · 版本号与构建信息、正式用户目录、导出与发布、安装更新卸载、地图数据包、故障诊断和许可
关键词：发布、发版、交付、版本号、version.txt、构建信息、导出、导出入口、scripts/release、分发压缩包、安装包、UESTC-<版本>-win64.7z、地图数据包、map-data-<版本>.zip、ODbL、正式用户目录、%APPDATA%\UESTC、安装、更新、升级、卸载、清理用户数据、诊断包、导出诊断、试玩反馈、许可、MIT、LICENSE、发布页、干净环境验证

  - OPS-RELEASE 分工 · L23 · 415
- OPS-RELEASE §0 结论 · L32 · 1.3k
  - ★ OPS-RELEASE §0.1 接入要点 · L41 · 870
- OPS-RELEASE §1 版本号与构建信息 · L54 · 406
- OPS-RELEASE §2 用户数据目录 · L61 · 321
- OPS-RELEASE §3 导出与发布 · L67 · 699
- OPS-RELEASE §4 安装、更新与卸载 · L75 · 549
- OPS-RELEASE §5 地图数据包（ODbL） · L87 · 696
- OPS-RELEASE §6 故障处理与诊断 · L98 · 845
- OPS-RELEASE §7 许可 · L115 · 1.0k
