---
id: WLD-OUTSKIRTS
title: 边界与外围：大箱庭、外围环境带、阻挡与提示
category: world
summary: 大箱庭的三条线（校界、前沿线、截面线）；外围环境带、边界阻挡与提示子文档的入口
keywords: [外围, 校外, 校界, 边界, 前沿线, 截面线, 大箱庭]
owners: [WP-47, WP-06, WP-30]
contributors: [WP-02, WP-04, WP-05, WP-06, WP-08, WP-13, WP-14, WP-15, WP-18, WP-22, WP-23, WP-24, WP-27, WP-28, WP-30, WP-33, WP-35, WP-36, WP-37, WP-38, WP-39, WP-41, WP-42, WP-43, WP-44, WP-45, WP-46, WP-48, WP-49, WP-53]
depends: [CORE-INDEX, ART-STYLE, WLD-DIORAMA, SYS-VIEW, ART-FIDELITY, ENG-ARCH, ENG-BUDGET, ENG-PERF, SYS-WALK, SYS-MAPS]
related: [ART-SKYLIGHT, ENG-GATES, MAP-QSH-ADDITIONS, SYS-TIME, UI-NAV, SYS-WEATHER, SYS-GRAPHICS, SYS-ATLAS, SYS-TITLE, SYS-SAVE, WLD-INTERIORS, SYS-AUDIO, OPS-RELEASE, WLD-TERRAIN, MAP-QSH-OUTSKIRTS]
contracts: []
data: []
gates: []
---

# WLD-OUTSKIRTS · 边界与外围：大箱庭、外围环境带、阻挡与提示

> **范围**：本文规定大箱庭的概念与三条线（校界、前沿线、截面线），是外围环境带、边界阻挡与提示两份子文档的入口。校界以外的内容、活动范围（SYS-VIEW）和校界收口（SYS-WALK）的细则以子文档为准。
> **不在本文**：外围分层、精度、灯光、画法、数据与许可（WLD-OUTSKIRTS-BAND）；行走与飞行阻挡、提示文案（WLD-OUTSKIRTS-BORDER）；清水河的具体街区与地标（MAP-QSH-OUTSKIRTS）；截面、雾海与远景卡片的画法（WLD-DIORAMA）；外围增补物的逐项外观（MAP-QSH-ADDITIONS）；活动范围的契约字段与按图数据（SYS-MAPS、`map.json` 的 `roam`）。
> **查阅**：先读 §0；三条线看 §1；其余按 §0 的子文档表找

## 0 结论

1. **大箱庭**：箱庭是"校园加门前一圈街区"。截面线在校界外约 250–500 m，沿支路、绿带、园区内部路切，不切楼、不切桥；截面线以内是风格化的外围街区，能看不能进；以外是雾海，雾里露出远处的楼顶，再往外是 WLD-DIORAMA 的三层卡片和雪山（§1）。

2. **子文档**：外围的内容、精度和数据归 BAND，校界的阻挡和提示归 BORDER，清水河的具体街区归 MAP。

| 文档 | 管什么 | 什么时候读 |
| --- | --- | --- |
| WLD-OUTSKIRTS-BAND 外围环境带 | 截面线的位置规则与截面精度，外围分层 O0–O3 与最近可达距离，灯光、季节与车流，保留、删除与新增，外围画法，数据与许可 | 做或判外围街区、截面、远景和外围数据时（WP-47、WP-22） |
| WLD-OUTSKIRTS-BORDER 边界阻挡与提示 | 行走阻挡与缓停，旁观飞行阻挡、缓冲带与 `edge_fog`，提示文案，与多地图、平面地图、存档的衔接 | 碰校界、活动范围、相机推回、出生与跳转位姿时（WP-06、WP-30、WP-42、WP-44） |
| MAP-QSH-OUTSKIRTS 清水河外围街区 | 清水河的周边结构、各方位截面线距离、身份地标、灯光、农田四季、氛围增补、河渠格栅、看街长椅 | 做清水河外围内容时（WP-47） |

### 0.1 接入要点

- **用到校界、前沿线、截面线**：三条线的定义以 §1 为准；阻挡与活动范围读 WLD-OUTSKIRTS-BORDER，外围内容与精度读 WLD-OUTSKIRTS-BAND，两份各有自己的接入要点
- **校界附近的地形与水位**：校界内侧 20 m 是地形零带，外围 O0、O1 与截面上缘按平地 y = 0；河渠水面高与校内该段常水位一致（WLD-TERRAIN）

## 1 三条线

校界以外的箱庭用三条线划分：

| 线 | 定义 | 作用 |
| --- | --- | --- |
| **校界** | 地图数据的 `boundary` | 行走的边界；栏杆、校门、闸机都在这条线上 |
| **前沿线** | 校界外第一条道路的**对侧路缘**；没有临界道路的地段取校界外 20 m 或匝道外侧路缘。离校界约 20–125 m | 旁观低空（h ≤ 30 m）能飞到的最远处；O0 与 O1 的分界 |
| **截面线** | 箱庭的外缘：地面沿它垂直切下，外面是雾海 | O1 与 O2 的分界；截面和 O1 地面共用它上面的点 |

两线由外围管线生成，转角做 ≥ 30 m 的圆弧或斜切。截面线的位置规则和截面精度见 WLD-OUTSKIRTS-BAND §1.1、§1.2，清水河各方位的具体距离见 MAP-QSH-OUTSKIRTS §1.2。
