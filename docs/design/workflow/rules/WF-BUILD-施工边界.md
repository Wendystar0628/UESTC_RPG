---
id: WF-BUILD
title: 施工边界
category: workflow
summary: 施工时能改哪里、共享文件与契约申请、新模块怎么接入，以及禁用写法门禁拦不住的两条约定
keywords: [独占目录, 共享文件, 契约申请, 特性接入, ContentModule, Part, KernelNode, SaveSection, 穷举 switch, 几何不随时季天气变, GDScript, uid, signatures.txt]
owners: [WP-00]
contributors: []
depends: [WF-INDEX, WF-RULE, CORE-INDEX, ENG-ARCH]
related: [WF-ONBOARD, WF-FLOW, ENG-WP, CORE-CONVENTIONS]
contracts: []
data: ["ownership.json"]
gates: [G3, G4]
paths: ["game/**/*.cs", "game/**/*.gdshader*"]
---

# WF-BUILD · 施工边界

> **范围**：本文规定施工子代理在写代码和做内容时的硬边界：能改哪些文件，哪些要提申请，新模块怎么接入。
> **不在本文**：代码边界、接入方式与禁用写法的完整清单：ENG-ARCH §2（G4 门禁检查）；契约的内容与冻结状态：ENG-ARCH §3；目录与归属明细：`ownership.json`；共享文件清单：CORE-CONVENTIONS §5.1；内容与画面的底线：CORE-CONTENT；全员守则：WF-RULE。
> **查阅**：先读 §0；改哪里与申请看 §1；G4 拦不住的约定看 §2。
> **关联**：[WF-INDEX 协作总纲](../WF-INDEX-协作总纲.md)、[WF-RULE 全员守则](WF-RULE-全员守则.md)、[WF-ONBOARD 子代理开工手册](../WF-ONBOARD-子代理开工手册.md)、[WF-FLOW 开发流程](../process/WF-FLOW-开发流程.md)、[CORE-INDEX 总索引](../../README.md)、[ENG-ARCH 工程架构](../../engineering/ENG-ARCH-工程架构.md)、[ENG-WP 工作包](../../engineering/ENG-WP-工作包派工单.md)、[CORE-CONVENTIONS 约定](../../core/CORE-CONVENTIONS-约定.md)

## 0 结论

1. **只改本包的独占目录**（§1）：独占目录见 ENG-WP §2 与 `ownership.json`，越界由门禁 G3 拒绝。
2. **共享文件不自己改**（§1）：工程共享件由 Astra 改，设计文档由主会话改；要改契约、全局着色器参数、着色器文件、程序集引用或 NuGet 包，写契约申请。
3. **新模块用特性接入**（§1）：不改注册表、不改主场景、不加 Autoload。
4. **代码边界和禁用写法的权威是 ENG-ARCH §2**，由门禁 G4 拦截；G4 拦不住的只有 §2 的两条。
5. **内容与画面的底线见 CORE-CONTENT**。

### 0.1 接入要点

- **新增契约、全局着色器参数、着色器文件、程序集引用、NuGet 包**：不自己加，写 `requests/contract-<包号>-<主题>.md` 交 Astra（WF-FLOW §5）。
- **要改的路径不在本包独占目录里**：不改，写进回报或契约申请（§1）。
- **新模块**：用 §1 列出的特性接入，不碰注册表（§1）。
- **写 gen 或烘焙子任务**：只用 `DetMath` 的超越函数，随机只用契约的 `Rng` 与 `StableHash`（ENG-ARCH §2）。
- **写 Godot 代码前**：先在 `docs/godot-api/signatures.txt` 查到签名再用（WF-ONBOARD §3.3）。

## 1 改哪里

- **只改本包的独占目录**：见 ENG-WP §2 的"独占目录"一列和 `ownership.json`；子包各自独占原包目录下的一个子目录（ENG-WP §3）。独占目录就是"同一批文件同一时间只有一个写者"的分工依据（WF-FLOW §2.1）。
- **共享文件不自己改**：清单见 CORE-CONVENTIONS §5.1。工程共享件（契约、全部 csproj、`Directory.*.props`、`project.godot`、`ownership.json`、`gate.ps1`、`scripts/` 下的工程脚本等）由 Astra 直接提交推送；`docs/design/` 由主会话改。
- **要提申请的事**：改契约、加全局着色器参数、加着色器文件、加程序集引用或 NuGet 包，都写成契约申请（`.collab/requests/contract-*.md`）交 Astra。契约分批冻结，已冻结的组只增不改（ENG-ARCH §3，D-134）。
- **新模块用特性接入**：`[ContentModule]`、`[Part]`、`[KernelNode]`、`[KernelService]`、`[SaveSection]`、`[AmbientEmitter]`、`[InteriorProvider]`、`[MediumPresenter]`、`[QualityResponsive]`；不改注册表、不改主场景、不加 Autoload（ENG-ARCH §2）。
- **场景与路径**：不在编辑器里打开或重存场景，场景尽量由代码生成；代码里只用 `res://` 路径，不手写 `uid://`；新文件的 `.uid` 由 `--headless --import` 生成，跟源文件一起提交，复制文件时删掉复制来的 `.uid`；不写 GDScript。

## 2 G4 拦不住的约定

代码边界、接入方式和禁用写法以 ENG-ARCH §2 和各系统文档的接入要点为准，G4 检查其中能机械检查的部分。拦不住的两条：
- **几何不随时季和天气变**：只走调色板、灯光通道、变体掩码和天气向量（SYS-TIME §0.1、SYS-WEATHER §0.1）。
- **规则集与枚举**：观光专属常量读 `IRuleset`；会追加值的枚举写穷举 `switch`，`default` 报错（ENG-ROADMAP §6）。
