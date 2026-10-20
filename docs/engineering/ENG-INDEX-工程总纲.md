---
id: ENG-INDEX
title: 工程总纲
category: engineering
summary: 工程域总纲：架构、性能、验收、发布、开发工具（内容预览）与扩展路线的通用原则和文档树
keywords: [工程, 工程总纲, 文档树, ENG, 工程架构, 性能预算, 性能工程, 硬规则, 验收, 门禁, 发布, 内容预览, 扩展路线, 主线之后]
owners: []
paths: ["docs/engineering/**"]
contributors: []
depends: [CORE-INDEX]
related: []
contracts: []
data: []
gates: []
---

# ENG-INDEX · 工程总纲

> **范围**：本文是工程域的入口：本域管代码怎么组织、性能、验收、发布、开发工具（内容预览）与扩展路线；列出本域通用的原则和本域全部文档的上下级。
> **不在本文**：各主题的规则与设计（在下面列出的文档里）；全项目的顶层规则与信息地图（CORE-INDEX）。
> **查阅**：先读 §0；要找文档看 §1，按标题挑要读的那份，再读它的 §0 与接入要点

## 0 结论

1. **数值以代码和数据为准**：预算在 `budgets.json` 与 `KernelBudgets.cs`，签名在 `game/contracts/`，文档写口径和规则；性能看数量级，同一量级又不挡体验的照常推送（WF-RULE §15）。
2. **硬规则 23 条违反任何一条都不能推送**：写渲染、逐帧代码、烘焙、存档先读 ENG-BUDGET-RULES；写代码先读 STD-CODE，架构边界看 ENG-ARCH。
3. **预算、基准和截图用同一个默认口径**：精致预设、窗口模式、默认状态（SYS-TIME §1.1）；最坏状态另加夏夜雷雨（ENG-BUDGET §2、ENG-GATES-SHOTS §1.1）。
4. **标题或节末带"主线之后"的内容是设计目标**，现在不施工，试玩流程打通后再做（STD-DOC §2，D-233）；"待验证（P0）"里的数是初值，按初值施工，验证后改写原位。
5. **测试最小**：门禁三档与何时跑见 ENG-GATES。

### 0.1 接入要点

- **新增工程文档**：先判断是不是规范（归 STD-INDEX）或计划（归 PLAN-INDEX）。
- **在本域新增文档**：主题文档写 `parent: ENG-INDEX`，列进本总纲 §1；子文档写 `parent: <主题文档编码>`，列进主题文档 §0，并列进本总纲 §1 那个主题下（STD-DOC §5.1、§7）。

## 1 文档树

主题文档在前，缩进的是它的子文档；只列编码与标题。

- ENG-ARCH 工程架构
- ENG-BUDGET 性能预算
  - ENG-BUDGET-LOD 细节级别、速度律与渲染参数
  - ENG-BUDGET-RULES 硬规则与工程设置
- ENG-GATES 验收与门禁
  - ENG-GATES-SHOTS 审查视角与截图
  - ENG-GATES-VETO 一票否决与收尾清单
- ENG-FAILS 游戏失败观测
- ENG-PERF 性能工程
  - ENG-PERF-ADAPT 自适应与测量
  - ENG-PERF-LIGHT 植被、阴影、后期与灯光
  - ENG-PERF-STREAM 流式加载、本机烘焙与内存
- ENG-PREVIEW 内容预览
- ENG-RELEASE 发布与交付
- ENG-ROADMAP 扩展路线
