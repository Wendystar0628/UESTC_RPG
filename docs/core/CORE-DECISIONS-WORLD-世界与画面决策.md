---
id: CORE-DECISIONS-WORLD
title: 世界与画面决策
category: core
parent: CORE-DECISIONS
summary: 作品定位与范围、世界与地图、时间季节天气、美术风格四个领域的已定决策
keywords: [决策, 世界, 画面, 作品定位, 地图, 时间, 季节, 天气, 美术风格]
owners: []
contributors: []
depends: [CORE-DECISIONS]
related: []
contracts: []
data: []
gates: []
---

# CORE-DECISIONS-WORLD · 世界与画面决策

> **范围**：本卷登记作品定位与范围、世界与地图、时间季节天气、美术风格四个领域的决策，每条一句结论，附适用范围、细则所在的小节和状态。
> **不在本卷**：用法、编号规则、状态含义和归卷规则在 CORE-DECISIONS §0；数值与规则的细则以"细则所在"指向的文档为准；其他领域的决策在 CORE-DECISIONS 列出的别的卷。
> **查阅**：先读 §0；按 D 编号或关键词 grep；作品与范围看 §1，世界与地图看 §2，时间季节天气看 §3，美术风格看 §4

## 0 结论

本卷装世界与画面类决策：细则所在第一份文档属世界、地图、时间、天气、美术、植被的归这里；编号与其他卷共用一个序列，不复用，作废或没用上的号不补。

### 0.1 接入要点

- **本卷只登记**：规则见主文档 CORE-DECISIONS §0 与 CORE-DECISIONS §0.1。

## 1 作品定位与范围

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| D-001 | 作品是校园题材的独立 RPG；当前阶段只做 PC 上实时渲染的三渲二世界与观光模式，不做游戏玩法 | 全项目 | CORE-INDEX §2；INV-1 | 生效 |
| D-002 | 观察方式只有两种：旁观飞行（穿透一切）和《我的世界》式行走（新游戏以行走开局，D-242）；不做环绕、俯视、导览和自动运镜 | 观光模式 | INV-2；SYS-VIEW §2；SYS-WALK §0 | 生效 |
| D-003 | 不出现 NPC 与他人：人只以生活痕迹出现，展陈不放人物肖像，声音里没有听得清的人声；旁观模式观者没有身体、影子和脚印；行走模式玩家有自己的角色本体（D-182） | 全项目 | INV-6；SYS-WALK §1；SYS-WALK §3.2；WLD-INTERIORS-BUILD §4；SYS-AUDIO §4 | 生效 |
| D-004 | 只做 PC（Windows）；手机、网页、触屏不做 | 全项目 | INV-1；SYS-VIEW §2.2；ENG-ARCH §1 | 生效 |
| D-005 | 引擎用 Godot 4.7.2-stable .NET，代码全部用 C#、不写 GDScript，渲染器 Forward+，默认 D3D12 | 全项目 | ENG-ARCH §1；CORE-INDEX §2；ENG-PERF-ADAPT §3 | 生效 |
| D-006 | 视觉效果最优先：四季、昼夜、天象、花期、天气以画面好看为准，真实数据只作参考和"认得出是成电"的身份特征 | 全项目 | CORE-INDEX §2；SYS-TIME §0 | 生效 |
| D-007 | 不是地图复刻：结构（体量、层数、平面、入口、交通核、道路、水体）以照片和底板为起点，可按设计增删改，材质、色彩、陈设、标识、生活痕迹按氛围艺术化处理 | 全部内容包 | CORE-INDEX §2；INV-9 | 生效 |
| D-008 | 可以新增校园里没有的氛围元素，也可以删掉现实里有、放进画面难看的元素（杂乱管线、临时围挡、刺眼广告、施工网架、过多指示牌）；增删一律写进覆盖层并写明理由 | 全部内容包 | INV-9；MAP-QSH-ADDITIONS §0；MAP-QSH-ADDITIONS §5；WLD-OUTSKIRTS-BAND §4 | 生效 |
| D-009 | 视觉精度分两层：底线（比例对、有厚度倒角和收边、没有光秃拉伸体块、不穿帮不跳变）对任何位置、任何距离都成立，含只有飞行才到的地方；细节密度按精度区 K1–K5 分级，越常被近看越密；外围按距离环分级，关着的楼只做外壳、窗卡和简化室内 | 全部建模包 | INV-11；ART-FIDELITY §0；WLD-OUTSKIRTS-BAND §2.1；WLD-INTERIORS-ACCESS §3 | 待原型验证（方向已定，判定距离和分数线在 P0 校准） |
| D-010 | RPG（入学到毕业）方向暂缓，只留架构接口（存档段与迁移、地图服务、解锁策略 `proximity`、多地图、门的锁状态），重新提出之前不展开设计 | 全项目 | ENG-ROADMAP；ENG-ARCH §7 | 生效 |
| D-011 | 作品名《电光年华》（英文 *Electric Years*）；默认状态的名字是"落金初灯" | 全项目 | CORE-INDEX §2；SYS-TIME §0 | 生效 |
| D-186 | 共享单车：本期载具只做匿名共享单车，不用真实品牌；骑行是行走模式里的运动分支，与步行、游泳互斥，不新增 ViewMode，用运动学解算、不做刚体平衡；首版只在许可区骑，固定停放点借还，切旁观、传送、换图时自动回收，读档一律恢复成步行；归新包 WP-55（共享单车运行与取还），按 D-183 排在建模包之后 | 行走模式 | SYS-WALK-BIKE §1；SYS-WALK §3.2；SYS-SAVE §2；UI-NAV §4；WLD-LIFE-STREET §1；ENG-WP-LIST §1 | 待原型验证 |

## 2 世界与地图

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| D-012 | 地图边界用"大箱庭"：箱庭扩到校界外一圈街区（截面线在校界外约 250–500 m），能看不能进；截面以外照旧是雾海和远景 | 清水河 | WLD-OUTSKIRTS §0；WLD-OUTSKIRTS-BAND §1.1；WLD-DIORAMA §4 | 生效 |
| D-013 | 外围分四层：O0 街道带、O1 外围街区、O2 雾中楼顶、O3 远景卡片与雪山；只保留结构和身份地标，车流不画 | 外围 | WLD-OUTSKIRTS-BAND §2.2；WLD-OUTSKIRTS-BAND §3.3；WLD-OUTSKIRTS-BAND §4 | 生效 |
| D-014 | 行走被栏杆、关着的校门和闸机轻轻挡住（边界 0.6 m 内缓停，不撞停），顶住 0.8 s 提示"校外暂未开放"；河渠穿校界处用拦污格栅，不用隐形墙 | 行走模式 | WLD-OUTSKIRTS-BORDER §1；WLD-OUTSKIRTS-BORDER §3.1 | 生效 |
| D-015 | 旁观飞行到边缘减速，朝外一侧雾变浓（`edge_fog`），越出的部分被推回，提示"前方暂未开放"；水平范围 = 前沿线 + 0.9 × max(0, h − 30 m)，离中心不超过 3 km | 旁观模式 | WLD-OUTSKIRTS-BORDER §2；WLD-OUTSKIRTS-BORDER §3.1 | 生效 |
| D-016 | 活动范围的"中心"取校界形心约 (50, 77)，即 `MapDescriptor.Center` | 清水河 | SYS-MAPS §2 | 生效 |
| D-017 | 全图数据纹理范围 `map_rect` 取 ±1280 m，不随外围扩大；平面地图页范围另设 `atlasRect` ±1536 m | 清水河 | SYS-MAPS §7；SYS-ATLAS-UI §1.3 | 生效 |
| D-018 | 多地图只做可扩展架构，当前只有清水河一张图；按图的数据放 `game/data/maps/qingshuihe/`，代码目录不动 | 全项目 | SYS-MAPS §0；SYS-MAPS §1.1 | 生效 |
| D-019 | 世界时间、季节、流逝倍率和 `AnimTime` 全局共享一条时间线；天气按图各一份前向过程，进图时补算到当前时间 | 多地图 | SYS-MAPS §4；SYS-WEATHER-PROCESS §1.5 | 生效 |
| D-020 | 跨图切换走雾幕加载（一个 World3D，先卸后装）；`MapTimeRule.Fixed` 预留不用 | 多地图 | SYS-MAPS §2；SYS-MAPS §3 | 生效 |
| D-021 | 室内只开放精选的第一批 15 处，另开 3 处屋顶或高处；其余楼门关着，只做外壳加窗卡 | 清水河 | MAP-QSH-INTERIORS；MAP-QSH-INTERIORS §4；WLD-INTERIORS-ACCESS §3 | 生效 |
| D-022 | 开放室内的第一批 15 处以细则文档的清单为准（含图书馆、博物馆展厅、主楼、宣邦楼、体育馆、食堂、学生活动中心等） | 清水河 | MAP-QSH-INTERIORS | 生效 |
| D-023 | 开放的屋顶与高处只有三处：宣邦楼钟楼（钟室与柱廊灯笼观景环）、主楼屋顶空廊、图书馆二三楼露台；其余屋顶一律不开放 | 清水河 | MAP-QSH-INTERIORS §4；SYS-WALK-BOUNDS §3 | 生效 |
| D-024 | 只有主楼通屋顶空廊的那部电梯可乘，做成"关门换位"两站直达；其余电梯门关着、显示楼层；钟楼走楼梯，不加电梯 | 开放室内 | MAP-QSH-INTERIORS §4；WLD-INTERIORS-BUILD §5；MAP-QSH-INTERIORS §1 | 生效 |
| D-025 | 一眼看出能进：开放楼的入口门固定敞开，配暖光池、地垫和绿色开放牌；平面地图上有"可进入的室内"图层 | 开放室内 | WLD-INTERIORS-ACCESS §2；SYS-ATLAS-TRAVEL §1 | 生效 |
| D-026 | 关着的门是实体、有碰撞，门玻璃用门厅卡，顶住 0.8 s 提示"此处暂不开放"；旁观飞进关着的楼看到简化室内（楼板、隔墙、室内雾、灯） | 关着的楼 | WLD-INTERIORS-ACCESS §3；WLD-INTERIORS-ACCESS §3 | 生效 |
| D-027 | 开放室内的房门和开放路线上的防火门、玻璃门：观者走近自动推开，闭门器缓缓关上；不加交互键，门扇转动期间无碰撞，关好才有碰撞 | 开放室内 | WLD-INTERIORS-ACCESS §2.1；SYS-WALK-BOUNDS §3 | 生效 |
| D-028 | 从窗外看室内精度不重要：室内视差、窗卡、窗帘、灯光色块，窗灯不必与房间逐间对应；走进开放室内才是全精度 | 全部楼宇 | WLD-INTERIORS-ACCESS §3；INV-11 | 生效 |
| D-029 | 再开一栋楼只需在开放清单 `interiors.json` 加一项，再注册 `IInteriorProvider` 或加一条楼型配置，不改契约和流式架构；清单由主代理维护 | 室内扩展 | WLD-INTERIORS-ACCESS §4；ENG-WP §2 | 生效 |
| D-030 | 托幼园和楼里的洁净室不开放；地下层不可进 | 清水河 | WLD-INTERIORS-BUILD §2；MAP-QSH-WEST §7；ENG-GATES §1 | 生效 |
| D-031 | 室外的道路、广场、桥、台阶、球场、看台、草坪全部可走；室内只有开放部分可走 | 行走模式 | SYS-WALK §0；SYS-WALK-BOUNDS §1 | 生效 |
| D-032 | 展陈不出现人物肖像，只用实物、文献、模型、风景照；实验室门牌的楼层单位用真实名称、房间级用通用名称；主楼一楼按柱厅做主题墙，不用《沁园春·雪》书法墙 | 开放室内 | WLD-INTERIORS-BUILD §4；MAP-QSH-INTERIORS §1 | 生效 |
| D-033 | 观景点共 P00–P23，其中 P16–P23 以 WLD-INTERIORS为准；再新增的点位从 P24 起编 | 点位 | WLD-INTERIORS-ACCESS §1；UI-NAV-WAYPOINTS §1；CORE-CONVENTIONS §3.2 | 生效 |
| D-157 | 外围分层编号 O0–O3：O0 街道带、O1 外围街区、O2 雾中楼顶、O3 远景卡片与雪山；E 只指审查视角 E1–E7 | 全项目 | WLD-OUTSKIRTS-BAND §2.2；CORE-CONVENTIONS §3.2；CORE-CONVENTIONS §3.3 | 生效 |
| D-164 | 开放清单 `interiors.json` 由主代理管：schema 与格式归 WP-51（室内生成器与壳体），条目由负责包经回报提交，增删由主代理审批 | 开放室内 | ENG-WP §2 第 4 条；SYS-MAPS §1.1；WLD-INTERIORS-ACCESS §4 | 生效 |
| D-180 | 地形分层：清水河不做平地，主动设计有层次的地形（平缓底板、少量强垂直、遮挡与揭示），与真实地形不符也可以；全图高差约 −5…+10 m；地形是覆盖层里的矢量地形操作合成的确定性高度函数，竖直落差一律做成实体，楼坐在垫层上，每个水体有自己的常水位 | 清水河 | WLD-TERRAIN §0；MAP-QSH-TERRAIN；ENG-ARCH §3 | 待原型验证 |
| D-185 | 空地按游戏设计理念组织，不照校内实际布局：开阔草坪不默认中心留白；较大的空地由所在区包归为功能开敞面（保持空旷但说明用途）或过渡绿地（重组，至少有远处参照、停留点和走动中的发现，不堆设施）；重组的草丘、浅洼登记在本区地形覆盖层；区包管本区空地，无主空地的草丘、浅洼由所属区包代为登记 | 清水河 | MAP-QSH-ADDITIONS §0；WLD-VEGETATION §3.2；WLD-VEGETATION §3.4；ENG-WP-LIST §1 | 生效 |

## 3 时间、季节、天气

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| D-034 | 世界状态有三个量：钟点、季节、天气 | 全项目 | SYS-TIME §0；INV-8 | 生效 |
| D-035 | 时间不能逆转：同一世界实例内 `WorldHours` 只增不减；时间环只能顺时针拨，`SetClock` 跳到下一次出现的钟点（最多前进 24 h），没有 −1 h 的按钮和快捷键 | 全项目 | SYS-TIME §1；ENG-ARCH §3；UI-NAV §3.1 | 生效 |
| D-036 | 风、水、一拍二用的 `AnimTime` 是实时秒 mod 480，与世界时间无关；不做落金循环 | 全项目 | SYS-MOTION §1；SYS-TIME §0 | 生效 |
| D-037 | 默认状态是秋 17:30、晴、自动天气、流逝 1×，天气保持晴到秋季黄昏带结束（约 19:36）；它是新游戏的终态和新存档的初值 | 清水河 | SYS-TIME §0；SYS-WEATHER-PROCESS §1.6；SYS-SAVE-FLOW §2.5 | 生效 |
| D-038 | 流逝档位为暂停 / 1× / 10× / 60× / 360× / 1440×，默认 1×；季节不自动推进 | 全项目 | SYS-TIME §1；ENG-ARCH §3 | 生效 |
| D-039 | "回到落金初灯"是正向换季到秋、快进到下一个 17:30（已在 17:22–17:40 之间则不动）、流逝 1×、自动天气、保持晴到黄昏带结束，不移动镜头；其他地图泛化为"回到本图默认状态" | 界面 | SYS-TIME §1；SYS-WEATHER-PROCESS §1.6；SYS-MAPS §4 | 生效 |
| D-040 | 季节可以任意选，一律用正向转场（扬起 → 雾幕 → 落定），不做倒放 | 全项目 | SYS-MOTION §3 | 生效 |
| D-041 | 新游戏和读档都是换世界实例（`IWorldState.Instance` 加 1），不算时间倒流；开始页停着的时间、关着游戏的现实时间都不计入世界时间 | 全项目 | SYS-TITLE §3.1；SYS-SAVE-FLOW §2.2；SYS-SAVE-FLOW §3 | 生效 |
| D-042 | 天气界面固定 8 格加"自动"；降水随季节变形（春夏秋是雨、冬季是雪），雷雨只在春夏；不做雾霾、冰雹、雨夹雪、落地闪电、镜头水滴 | 全项目 | SYS-WEATHER §1；UI-NAV §3.7 | 生效 |
| D-043 | 自动天气是前向随机过程：随机时段、随机时长，只走验证过的组合，晴约占四分之三；自动时显示一行预报（预报行） | 全项目 | SYS-WEATHER-PROCESS §1；UI-NAV §3.7；UI-VISUAL-COMPONENTS §4.4 | 生效 |
| D-044 | 天气种子每个存档一个，种子和已生成的天气队列都存进存档 | 存档 | SYS-WEATHER-PROCESS §1.4；SYS-SAVE-FLOW §2.4 | 生效 |
| D-045 | 闪电只做远处积雨云的云内闪，不画落地闪电、不全屏闪白；闪电和彩虹都不算偶发事件 | 全项目 | SYS-WEATHER §5；INV-10 | 生效 |
| D-046 | 太阳雨不新增天气格，是"雨 → 雨后初晴"这条转移边开头的 30–90 s；英雄帧第 12 张是"秋 17:30 太阳雨" | 天气 | SYS-WEATHER §2；SYS-WEATHER-PROCESS §1.3；SYS-TIME-LOOK §1.5 | 生效 |
| D-047 | 彩虹只在雨后初晴出现，主虹半径随太阳升高在 30–42° 之间放大（不按真实的固定 42°），太阳在 34–40° 之间时淡出 | 天气 | SYS-WEATHER §5；ART-SKYLIGHT-ATMOSPHERE §1 | 生效 |
| D-048 | 全片只有一个偶发事件；用户触发的时间、季节、天气、跳转、传送过渡和进入游戏的加载都不算 | 全项目 | INV-10；SYS-MOTION-LIFE §3；SYS-MOTION-LIFE §3 | 生效 |
| D-049 | 下雪由天气过程决定（冬季夜雪多在 19–23 时开始），不按固定时段 | 冬季 | SYS-WEATHER-PROCESS §1.3；SYS-TIME-LOOK §2 | 生效 |
| D-050 | 阴雨天两阶不丢：投影强度乘 (1 − 0.8 × 阴度)，白天不低于 0.2 | 天气 | SYS-WEATHER §6；ENG-PERF-LIGHT §2.1 | 生效 |
| D-051 | 所有切换都自然过渡、不硬切；每种时刻 × 季节 × 天气的组合都要好看 | 全项目 | INV-8；SYS-TIME §4；SYS-WEATHER §7 | 生效 |
| D-158 | 一拍二的 CPU 侧节拍（`IWorldState.Step12`、飘落池的发射时刻）与着色器同读 `step_rate`（WP-32（设备分档、画面设置与自适应） 写：一般 12 Hz，帧率上限 30 时 15 Hz），两侧在同一帧换姿态 | 全项目 | SYS-MOTION §2.2；ENG-ARCH §3；ENG-PERF §1.5 | 生效 |
| D-162 | 秋季黄昏带（太阳 +8° 到 −7°）约 17:16–19:36；初灯组开始亮灯的时刻、SYS-AUDIO声景的"黄昏"段和默认状态"保持晴"的终点都与它一致 | 清水河 | SYS-WEATHER-PROCESS §1.2；SYS-TIME-LOOK §3；SYS-AUDIO-AMBIENCE §1.1 | 生效 |

## 4 美术风格

| 编号 | 决定 | 适用范围 | 细则所在 | 状态 |
| --- | --- | --- | --- | --- |
| D-052 | 画风是三渲二日式动画背景，基调"清透干净"：明度整体提亮，减弱水彩斑驳和墙脚暗带，斑驳只留在近处 | 全项目 | ART-STYLE §1；ART-STYLE-SHADING §4；ART-MODELING-PROPORTION §4 | 生效 |
| D-053 | 明暗分阶：建筑两阶，曲面道具与软物两阶半，植被三阶；不做接触影 | 全部材质 | ART-STYLE-SHADING §1；ART-FIDELITY §2 | 生效 |
| D-054 | 影子以明度为主、按材质推导：影的明度按固定比例压低，彩度略升，色相向天光微转，黄橙不往绿转；影不一律取蓝紫 | 全部材质 | ART-STYLE-SHADING §2.1；ART-STYLE-SHADING §2.2 | 生效 |
| D-055 | 只有部分材质带色调变化（中性面在影里带一点天光色）；透光元素（树叶、花瓣、窗帘、灯罩、薄布、玻璃）按逆光透射；室内用暖影；夜间与阴雨的影调另按 SYS-TIME、SYS-WEATHER 处理 | 全部材质 | ART-STYLE-SHADING §2.4；ART-STYLE-SHADING §2.5；ART-STYLE-SHADING §2.6；ART-STYLE-SHADING §3 | 生效 |
| D-056 | 描线约 1.25 px（1080p），线色由底色压暗（暖处暖棕、冷处青灰），受光面上线的 L 约 0.49；粗细有轻重变化，树冠内的线变淡；线宽按内部分辨率换算并设最小宽度 | 全项目 | ART-STYLE-LINES §1；SYS-GRAPHICS §2 | 生效 |
| D-057 | 柔光白天只让高光晕开，黄昏以后才叠全帧柔光；17:30 默认格的全帧柔光取 0.02，在 0–0.04 之间看图定 | 后期 | ART-STYLE-LINES §2 | 待原型验证 |
| D-058 | 后期加轻微暗角（风格默认，可关）和分调（烘在 LUT 里，属风格底线） | 后期 | ART-STYLE-LINES §2；SYS-GRAPHICS §2 | 生效 |
| D-059 | 室内用窗格光柱和光斑把画面灌满光；低角度阳光下，地板上的格子光斑只出现在落地窗、门和竖缝窗处 | 开放室内 | WLD-INTERIORS-BUILD §3.2；ART-STYLE-LINES §2 | 生效 |
| D-060 | 进入室内时，阴影方向的最低高度从 4° 抬到 14°（在 12–16° 之间看图定） | 开放室内 | WLD-INTERIORS-BUILD §1.3；ENG-PERF-LIGHT §2.1；ENG-BUDGET-LOD §4 | 待原型验证 |
| D-061 | 樱花、桂花这类团块型树冠做成厚实的云团 | 植被 | WLD-VEGETATION §1；ART-MODELING-PROPORTION §3 | 生效 |
| D-062 | 不设收束意象：画面的变化由时间、季节、天气的组合产生；落日金光、初灯、八角穹顶、西湖、满月、宣邦楼的钟只是组合里效果好的画面，保留但不当统领原则；英雄帧只是验收样张；"落金"指落日的金光，不是银杏叶 | 全项目 | CORE-INDEX §2；SYS-TIME-LOOK §1.5 | 生效 |
| D-063 | 银杏只作真实植被按实际分布种植；不当界面母题、图标、锚点造型、窗口图标，文案不夸大银杏 | 全项目 | CORE-INDEX §2；WLD-VEGETATION §2；UI-VISUAL-COMPONENTS §3.2 | 生效 |
| D-064 | 日本元素只放开日常街景物件（成组售货机、背街的电线杆与架空线、自行车棚等），室内同样适用，主轴保持成电身份；日本的宗教符号、地标符号、日文文字和传统和式建筑构件不出现；声音允许黄昏乌鸦叫和傍晚广播里的一段原创旋律，钟楼仍只敲单音铜钟 | 全项目 | INV-7；ART-AMBIENCE；ART-STYLE §3.1；WLD-INTERIORS-BUILD §3.3；SYS-AUDIO | 生效 |
| D-065 | 场景里本来就有的字（楼名题字、店招、路牌）保留，一律用简体中文；校徽、红十字这类受专门规定保护的标志不原样使用，换成风格相近的原创图形，医院用院名灯牌或原创医疗图标 | 全项目 | INV-4；ART-STYLE §4 | 生效 |
| D-066 | 色彩分工：场景暖、界面冷；清蓝只在界面层，不进 3D 材质，世界里不放蓝色全息；场景里的 `uestc.blue` 只给旗面、校车、单车 | 全项目 | UI-VISUAL §2.1；ART-STYLE §2.4 | 生效 |
| D-163 | 树种表有栾树：云团型落叶树，秋叶金黄、枝头挂红褐蒴果；叶色用 `deciduous.*`，蒴果用开花色 `bloom.capsule`；种在 MAP-QSH-ADDITIONS 的树阵、方树池和停车场绿岛 | 植被 | WLD-VEGETATION §1.1；MAP-QSH-VEGETATION §1；WLD-VEGETATION §2.1；MAP-QSH-ADDITIONS-VOCAB §1；MAP-QSH-ADDITIONS-VOCAB §2 | 生效 |
