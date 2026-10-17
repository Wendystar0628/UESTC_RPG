---
id: CORE-CONTENT
title: 开发内容核心准则
category: core
summary: 做内容、写代码时每次都要守的开发内容核心准则，一条一句，细则见各条指向；钩子在子代理开工和会话首次命中内容路线时注入 §0
keywords: [开发内容核心准则, 内容准则, 画风, 画面不能变差, 视觉精度底线, 精度区, 内容禁区, 不是地图复刻, 世界状态, 预算, 代码边界, 禁用写法, 架构要好, 改哪里, 坐标, Godot API, 共同口号, 内容与画面底线, INV, 提醒钩子]
owners: [WP-00]
contributors: []
depends: [CORE-INDEX, ART-STYLE, ART-FIDELITY, ENG-ARCH, ENG-BUDGET]
related: [CORE-CONVENTIONS, ART-MODELING, ART-AMBIENCE, MAP-QSH-ADDITIONS, SYS-TIME, SYS-MOTION, WF-CORE, WF-BUILD, WF-DOC]
contracts: []
data: []
gates: [G4]
---

# CORE-CONTENT · 开发内容核心准则

> **范围**：本文只列开发内容的核心准则，一条一句；细则以指向的小节为准。与行为侧的 WF-CORE 平行：WF-CORE 管模型怎么干活，本文管做出来的东西。提醒钩子把下面 §0 注入子代理（开工时一次）和会话（第一次命中内容路线时一次）。
> **不在本文**：全局不变量的完整条文（CORE-INDEX §3）；视觉与风格细则（ART-STYLE、ART-FIDELITY）；代码写法与禁用清单（ENG-ARCH §2）；预算数值（ENG-BUDGET）；行为准则（WF-CORE）。
> **查阅**：读 §0 即可；细则按每条后面的指向查；内容与画面底线的补充看 §1。
> **关联**：[CORE-INDEX 总索引](../README.md)、[ART-STYLE 风格基础](../art/ART-STYLE-风格基础.md)、[ART-FIDELITY 视觉精度标准](../art/ART-FIDELITY-视觉精度标准.md)、[ART-MODELING 风格化造型规则](../art/ART-MODELING-风格化造型规则.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH-工程架构.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET-性能预算与硬规则.md)、[CORE-CONVENTIONS 约定](CORE-CONVENTIONS-约定.md)、[WF-CORE 项目核心准则](../workflow/rules/WF-CORE-项目核心准则.md)

## 0 结论

1. **画风**：清透干净的三渲二日式动画背景，不做旧、不脏，影子浅而透（INV-5，ART-STYLE，D-174）。
2. **画面不能变差**：视觉优先；任何时刻、季节、天气的组合都要好看；碰渲染、生成器、材质的改动要能和基线截图对上（INV-5、INV-8）。
3. **精度底线处处满足**：比例对，有倒角和收边，没有光秃的拉伸体块；细节密度按精度区 K1–K5 分级（INV-11，ART-FIDELITY §0，D-176）。
4. **内容禁区**：画面和展陈不出现人物与肖像，场景里的字一律简体中文，日式元素只限日常街景，关着的楼只做外壳、窗卡和简化室内（INV-4、INV-6、INV-7、INV-11）。
5. **不是地图复刻**：平面格局大体对，其余可风格化；增删与设计增补只写覆盖层并写明理由（INV-9，MAP-QSH-ADDITIONS，ENG-ARCH §3）。
6. **世界状态**：时间只向前；几何不随时季天气变，只走调色板、灯光通道和全局参数；动态轻慢持续，全片一个偶发事件（INV-8、INV-10，SYS-TIME §0.1）。
7. **界面与观察**：只有旁观与行走两种观察模式，不做自动运镜；界面只放 UI-NAV 登记的元素，3D 画面里不叠加标签和标记（INV-2、INV-3）。
8. **预算是合入条件**：三角形、绘制调用、帧时、体积都在额度内（INV-12，ENG-BUDGET §7）。
9. **代码边界**：gen 是纯 .NET，显示经 `IRenderHost`，材质经 `ToonSpec`，声音、存档、天气、地图数据、观者与镜头各走自己的接口；禁用写法由 G4 拦截（ENG-ARCH §2）。
10. **代码架构要好**：发现明显不好的架构，先说清怎么改，不绕过、不打补丁糊过去（WF-ROLE）。
11. **改哪里**：只改本包独占目录；共享文件与契约不自己改，写申请（WF-BUILD §1）。
12. **坐标与 API**：单位米，x 东、y 上、z 南，地面高度读 `ITerrain`；Godot API 先查 `docs/godot-api/signatures.txt`（CORE-INDEX §4，WF-ONBOARD §3.3）。

### 0.1 接入要点

- **新增或改动一条内容准则**：先改它指向的细则小节，再改这里的一句；钩子直接读本文 §0，不用动钩子。
- **派单、写简报**：不抄本文，写"内容准则见 CORE-CONTENT"。

## 1 细则在哪

每条的细则见它后面括号里的小节；全局不变量 INV-1–INV-12 的条文见 CORE-INDEX §3，门禁见 ENG-GATES §2。内容与画面的底线补充如下：

- **去粗糙靠最低合格构件、自定义法线和描线，不靠堆细节**（ART-MODELING §7）。表面属于哪个精度区读导出的精度区数据，不自己判断；底线不拿来换预算，细节密度在本包额度内按区取舍（ENG-BUDGET §8）。
- **共同口号**："走进剧场版动画的校园，清透、敦实、干净、有生活。"建模时拿不准风格取舍，回到这句话（ART-MODELING §6）；造型与质感拿不准时，看 ART-STYLE §1 的参考场景怎么做，读代码，不只看图。
- **任何时刻、季节、天气都要好看**：四季、昼夜和天气一律视觉效果优先，真实情况只作参考；照片和 OSM 只用来定结构，材质、色彩、陈设和标识按氛围做艺术化处理（CORE-INDEX §1）。
