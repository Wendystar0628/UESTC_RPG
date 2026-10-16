---
id: TOOL-PREVIEW
title: 内容预览
category: tools
summary: 开发构建里预览与回看内容：按实体定位、保留机位与时季天气、复制机位、重读数据
keywords: [内容预览, 预览, 回看, 看效果, 开发构建, '--focus', '--pose-file', 按实体定位, 实体 ID, 机位, 复制机位, 机位文件, last-pose.json, F5 重读数据, F6 复制机位, F7 重启, 重启后恢复机位, 校验不过, 保留上一次有效画面, 预览只读不写]
owners: [WP-01, WP-08]
contributors: [WP-02, WP-46]
depends: [ENG-GATES, ENG-ARCH]
related: [SYS-MAPS, WLD-OUTSKIRTS, ART-FIDELITY, ENG-WP, CORE-CONVENTIONS]
contracts: []
data: []
gates: []
---

# TOOL-PREVIEW · 内容预览

> **范围**：本文说明开发时怎样预览和回看内容：从开发入口启动，按实体 ID 找到对象，改了覆盖层或参数以后在同一机位、同一时季天气下回看，把机位记下来。这些功能只在开发构建里有。
> **不在本文**：命令行参数的权威表（ENG-GATES §1）；覆盖层的格式（ENG-ARCH §3）；`--gen=live` 与烘焙（ENG-ARCH §4）；同图重载（SYS-MAPS §13）；近景巡检点（ART-FIDELITY §8）。
> **查阅**：先读 §0；启动命令与起点参数看 §1；按实体 ID 定位看 §2；机位文件与 F6 看 §3；改了以后怎么回看（F5、F7、重编译）看 §4
> **关联**：[ENG-GATES 验收门禁](../engineering/ENG-GATES.md)、[ENG-ARCH 工程架构](../engineering/ENG-ARCH.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS.md)、[ART-FIDELITY 视觉精度](../art/ART-FIDELITY.md)、[ENG-WP 工作包](../engineering/ENG-WP.md)、[CORE-CONVENTIONS 约定](../core/CORE-CONVENTIONS.md)

### 分工

- **WP-01**：预览参数的解析、三个开发按键、机位文件的读写、重载和重启后落回原机位；
- **WP-08**：按实体 ID 查世界模型；在后台重读本图的覆盖层与数据并校验，校验不过时保留上一次有效的世界模型，报出出错的文件与条目；
- **WP-46**：同图重载在开发构建里对预览开放（平时只在 `--gate=maptravel` 下启用，SYS-MAPS §13）；
- **WP-02**：`--gen=live` 下重载时按同一个 `Build()` 重新生成（ENG-ARCH §4）。

## 0 结论

1. **预览只读不写**：覆盖层（`overlay/<组>.d/` 下的分片）和各包自己的代码、数据文件是唯一的保存源。预览不写仓库里的任何文件（只在开发态的 `user://` 里记机位，§3），不存场景；代理照旧不在编辑器里打开或重存场景（ENG-ARCH §6）。
2. **一次回看的循环**：定位对象 → 改覆盖层或参数 → 在原机位、原时季天气下回看 → 满意后提交改过的文件。
3. **第一版只做整图重载和重启后恢复机位**：改数据用同图重载，改代码重新编译后重启；不做 C# 热重载，不做地图编辑器。
4. **读不进来时保留上一次有效的画面**：新数据校验不过就不卸载当前图，不崩、不黑屏。

### 0.1 接入要点

- **要预览或回看自己的内容**：从开发构建启动，起点用 `--pose`、`--focus=<实体 ID>`、`--pose-file` 三选一，同时给出以非零退出码结束（§1、§2）
- **改了覆盖层或按图的 JSON 数据**：按 F5 校验并同图重载；改了着色器、调色板等启动时读的文件按 F7 重启；改了 C# 代码关掉程序、重新编译后带 `--pose-file` 启动（§4）
- **要保存调整**：预览不写仓库文件、不存场景；位置和轮廓写进覆盖层的 `reshape`、`add`，参数改在负责包自己的文件里，不在编辑器里打开或重存场景（§0 第 1 条、§4）
- **要说明截图从哪里拍**：按 F6 复制机位，把那段 JSON 贴进回报（§3）
- **要新增点位或审查视角**：把记下的机位交给负责的包登记（`waypoints.json` 归 WP-36，`review.json` 归 WP-27），预览不改这两份文件（§3）
- **只看效果的截图**：`--pose-file` 加 `--out=<绝对路径>.png` 出一张，不算门禁（§4）
- **数据校验不过**：画面保持上一次有效的结果，错误显示在屏幕上方、完整信息写日志，改好再按 F5（§4）
- **写导出版或门禁的启动参数**：`--focus`、`--pose-file` 和 F5–F7 只在开发构建里有，导出的 release 版按未知参数以非零退出码结束；两者属世界类参数，不能与 `--slot`、`--load` 同时给出（§1）

## 1 启动

在工作树根目录：

```powershell
& $env:UESTC_GODOT --path game --windowed -- --gen=live --focus=qsh:way:544688571 `
  --clock=17.5 --season=autumn --weather=clear --flow=0 --title=0 --save=off --ui=1
```

- `--gen=live` 实时生成，不读也不写烘焙缓存；世界参数的写法与截图默认串相同（ENG-GATES §1）。
- **起点三选一**，同时给出以非零退出码结束：
  - `--pose=<编号>`：点位或审查视角（ENG-GATES §1）；
  - `--focus=<实体 ID>`：按实体定位（§2）；
  - `--pose-file=<绝对路径>`：回到记下的机位（§3）。
- `--focus`、`--pose-file` 和 §3、§4 的三个按键（F5、F6、F7）只在开发构建（`OS.IsDebugBuild()`）里有；导出的 release 版不认，按未知参数以非零退出码结束。两个参数登记在 ENG-GATES §1 的"门禁、基准与开发用的用户参数"表，含义以本文为准。
- `--pose-file` 的文件读不到、或字段不合法时以非零退出码结束；文件里的地图、模式、机位和世界参数（§3 的字段）覆盖命令行里的同名参数，其余参数照常。`--focus`、`--pose-file` 与 `--pose` 一样属于世界类参数，与 `--slot`、`--load` 同时给出时以非零退出码结束（ENG-GATES §1）。

## 2 按实体 ID 定位

- `--focus=<实体 ID>`：ID 是 `campus.json` 的实体或覆盖层新增的 `design:` 实体（CORE-CONVENTIONS §3.1），按 ID 查世界模型，不按名字找。
- **机位**：目标点取实体包围盒的中心，按"目标点、方位、仰角、距离"的写法（CORE-CONVENTIONS §1.2）取方位 200°、仰角 30°，距离取刚好装下包围盒的值；结果落在可达空间外时就近合法化（WLD-OUTSKIRTS §8.1），不退出。
- 查不到这个 ID 时以非零退出码结束，日志写明查不到的 ID 和本图的实体前缀。

## 3 记下机位

- **F6 复制机位**：把当前机位和世界参数写成一段 JSON，放进剪贴板，同时写到 `user://preview/last-pose.json`（开发态的 `user://` 是本工作树自己的目录，CORE-CONVENTIONS §5.3）。开发构建正常退出时也写一次这个文件。
- 字段名和取值与命令行参数一致（ENG-GATES §1）；行走模式下 `position` 是脚位：

  ```json
  { "map": "qingshuihe", "mode": "fly", "position": [-20.0, 1.7, 200.0], "yawDeg": 35, "pitchDeg": 0,
    "clock": 17.5, "season": "autumn", "weather": "clear", "anim": 240 }
  ```

- **用途**：用 `--pose-file` 回到这个机位；贴进回报，说明截图从哪里拍；要新增点位或审查视角时交给负责的包登记（`waypoints.json` 归 WP-36，`review.json` 归 WP-27），预览不改这两份文件。

## 4 改了以后回看

| 改了什么 | 怎么回看 | 机位与世界状态 |
| --- | --- | --- |
| 覆盖层、按图的 JSON 数据 | 按 **F5**：先在后台重新解析并校验本图数据，通过后做一次同图重载，`--gen=live` 下重新生成 | 落回按 F5 那一刻的机位；钟点、季节、天气不变，`--flow=0` 时不走 |
| 只在启动时读的文件：着色器、调色板、树种表等全局数据 | 按 **F7**：写下机位后自动重启（`OS.SetRestartOnExit`），重启参数是原来的参数换上 `--pose-file` | 落回按 F7 那一刻的机位和世界参数 |
| C# 代码：生成器、部件参数 | 关掉程序（自动写下 `last-pose.json`）→ `dotnet build`，资源有变再 `--headless --import` → 带 `--pose-file=<last-pose.json 的绝对路径>` 重新启动 | 落回关掉时的机位和世界参数 |

- **校验不过**（JSON 语法、schema、两个覆盖层改同一个要素，以及 G5 会报的数据错误）：不卸载当前图，画面保持上一次有效的结果；屏幕上方显示一行错误，完整信息写日志。改好后再按 F5。
- **只看效果的截图**：`--pose-file` 加 `--out=<绝对路径>.png` 出一张；这算开发中自己看效果，不算门禁（CORE-CONVENTIONS §8.3）。
- **预览里不能做的**：拖动、摆放物体，改颜色或参数后直接存盘。位置和轮廓的调整写进覆盖层的 `reshape`、`add`（ENG-ARCH §3），参数改在负责包自己的文件里。
