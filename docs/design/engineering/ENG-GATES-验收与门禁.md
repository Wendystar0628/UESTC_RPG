---
id: ENG-GATES
title: 验收：门禁、审查视角、近景巡检、回报格式
category: engineering
summary: 审查视角与命令行参数、合入门禁 G1–G21、一票否决、全局清单和回报格式
keywords: [门禁, 验收, 测试, 自查, 测试节奏, 测试三档, 快检, 核心专项, 集中测试, gate.ps1, 门禁编号, G1–G21, 审查视角, 截图默认串, 命令行参数, 用户参数, 截图方式, 截图就绪, 流式屏障, 录像, 时季抽样, 巡检点轮换, 一票否决, 全局清单, 回报格式, 回报模板, 截图命名, review.json, output/render-review, GPU 锁]
owners: [WP-27]
contributors: [WP-00, WP-01, WP-02, WP-06, WP-32, WP-35, WP-36, WP-41, WP-42, WP-43, WP-44, WP-45, WP-46, WP-48, WP-49, WP-50, WP-53]
depends: [CORE-INDEX, ART-FIDELITY, ENG-ARCH, ENG-BUDGET, ENG-PERF, SYS-TIME, UI-NAV, SYS-WALK, SYS-WEATHER, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, SYS-SAVE, SYS-MAPS, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO]
related: [ART-STYLE, SYS-MOTION, SYS-VIEW, ART-MODELING, MAP-QSH-ADDITIONS, ENG-WP, ENG-ROADMAP]
contracts: []
data: ["game/data/maps/qingshuihe/review.json"]
gates: [G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16, G17, G18, G19, G20, G21]
---
# ENG-GATES · 验收：门禁、审查视角、近景巡检、回报格式

> **范围**：本文规定审查视角与命令行参数（§1）、合入门禁 G1–G21（§2，各文档 front matter 的 `gates` 字段以这里的编号为准）、一票否决（§3）、收尾的全局清单（§4）和回报格式（§5）。准备合入时查 §2 的测试三档与 `gate.ps1`，写完成回报时查 §5；WP-27 照它实现自动化，P3 阶段的质检子代理照它审查。
> **不在本文**：各门禁引用的判据细则在对应的系统文档里（例如 SYS-TIME §8、SYS-WEATHER §9、UI-VISUAL §13、WLD-INTERIORS §19、SYS-AUDIO §22）；预算数值见 ENG-BUDGET，工作包分工见 ENG-WP。
> **查阅**：先读 §0；测试三档与 `gate.ps1` 的用法看 §2；各门禁的内容与何时运行看 §2；命令行参数、审查视角与截图方式看 §1；一票否决看 §3；回报格式与截图命名看 §5；收尾的全局清单看 §4；待标定的判据看 §6
> **关联**：[CORE-INDEX 总索引](../README.md)、[ART-FIDELITY 视觉精度](../art/ART-FIDELITY-视觉精度标准.md)、[ENG-ARCH 工程架构](ENG-ARCH-工程架构.md)、[ENG-BUDGET 性能预算](ENG-BUDGET-性能预算与硬规则.md)、[ENG-PERF 性能工程](ENG-PERF-性能工程.md)、[SYS-TIME 时间季节](../systems/SYS-TIME-时间与季节.md)、[UI-NAV 界面与跳转](../ui/UI-NAV-界面与操作说明.md)、[SYS-WALK 行走](../systems/SYS-WALK-行走模式.md)、[SYS-WEATHER 天气](../systems/SYS-WEATHER-天气.md)、[SYS-GRAPHICS 画面设置](../systems/SYS-GRAPHICS-画面设置.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL-界面视觉语言.md)、[SYS-ATLAS 平面地图与传送](../systems/SYS-ATLAS-平面地图与传送.md)、[SYS-TITLE 开始页与过场](../systems/SYS-TITLE-开始页与进入过场.md)、[SYS-SAVE 存档](../systems/SYS-SAVE-存档与持久化.md)、[SYS-MAPS 多地图](../systems/SYS-MAPS-多地图.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS-边界与外围.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS-室内.md)、[SYS-AUDIO 音频](../systems/SYS-AUDIO-音频.md)、[ART-STYLE 风格基础](../art/ART-STYLE-风格基础.md)、[SYS-MOTION 动态与事件](../systems/SYS-MOTION-动态与事件.md)、[SYS-VIEW 观察模式与介质](../systems/SYS-VIEW-观察模式介质与开场.md)、[ART-MODELING 风格化造型](../art/ART-MODELING-风格化造型规则.md)、[MAP-QSH-ADDITIONS 设计增补](../maps/qingshuihe/MAP-QSH-ADDITIONS-设计增补.md)、[ENG-WP 工作包](ENG-WP-工作包派工单.md)、[ENG-ROADMAP 扩展路线](ENG-ROADMAP-扩展路线.md)

## 0 结论

1. **门禁编号以本文为准**：合入门禁共 G1–G21，子项写作 G19a、G21b 这样的形式；各文档 front matter 的 `gates` 字段只用这些编号，全局不变量 INV-1 至 INV-12 不是门禁（§2）。
2. **测试分三档**（D-167），按最小必要原则跑，不设每天、每周的固定任务：
   - **快检**：每次合入 `main` 前（直接在 `main` 上做时每次推送前）跑编译、改动涉及的单元测试、归属与无界面注册检查，几分钟内跑完，不占 GPU 锁；
   - **核心专项**：改动碰到核心体验时，在一段工作做完、准备合入时跑一次，只有启动与主线冒烟、行走不穿地、存档不丢不坏三类；
   - **集中测试**：大量开发之后由主代理发起，全部门禁全量跑完、统一修复（§2）。
3. **统一入口 `scripts\gate.ps1 -Wp WP-xx`**：默认跑快检，按 `git diff main...HEAD` 的改动决定加不加核心专项；只改 `docs/`、注释或说明文字时不跑门禁；结果按输入的内容哈希缓存，输入没变就沿用（§2）。
4. **开发中看效果不算门禁**：用 `--pose` 截几张自己看，不走 GPU 锁，也不跑全套视角（§2）。
5. **审查视角与路线**：V1–V11、E1–E7、T0、F1–F4、P00–P23、W1–W14 定义在 §1 的视角表，数据在本图的 `review.json` 与 `waypoints.json`；截图默认串是秋 17:30、晴、精致、界面隐藏、无声，出默认状态的截图只换 `--pose`（§1）。
6. **命令行只认 §1 的两张参数表**：未知参数以非零退出码结束；门禁和基准只由命令行决定，不读 `user://`（§1）。
7. **截图有固定做法**：窗口模式、D3D12、用 `--gpu-index` 选登记的独显并断言显卡名，等流式加载与管线编译稳定后再存图；录像用 Movie Maker 加流式屏障（§1 的"截图方式"）。
8. **一票否决 24 条**，满足任意一条直接判不通过；集中测试时全面检查，平时只要求不违反（§3）。
9. **回报有固定格式**：每个子代理完成后按 §5 的模板逐行回报，截图放 `output/render-review/`，引用条目只写编号（§5）。
10. **收尾与标定**：收尾阶段由主代理逐项检查全局清单（§4）；截图就绪判据、像素容差和各项阈值的初值待 P0 标定（§6）。

### 0.1 接入要点

- **准备合入 `main`**：跑 `scripts\gate.ps1 -Wp WP-xx`，快检必须通过；改动碰到启动与主线、行走与碰撞、存档时，入口会加跑核心专项；其余门禁留到集中测试（§2）
- **只改了 `docs/`、注释或与运行无关的说明文字**：不跑任何门禁（§2）
- **只想看效果**：用 `--pose` 截几张自己看，不走 GPU 锁、不跑全套视角，不算门禁（§2）
- **要跑指定门禁**：`-Only G7,G9` 只跑指定门禁，`-Smoke` 只跑主线冒烟，`-Full` 跑集中测试的全量；集中测试由主代理发起（§2）
- **在 front matter 写 `gates`**：只用 §2 门禁表里的编号 G1–G21，子项写作 G19a，不写 INV 编号（§2）
- **交付门禁检查或测试接口**：接进 `scripts\gate.ps1` 这同一个入口，由 WP-27 接线（§2）
- **需要新的命令行参数**：`Boot` 只认 §1 两张表里的参数，其余以非零退出码结束；解析归 WP-01，用法归 WP-27（§1）
- **截图或录像**：用截图默认串，只换 `--pose`；窗口模式、D3D12、`--gpu-index` 选登记的独显，一律 `--audio=off`，按"截图方式"等就绪后存图（§1）
- **写完成回报**：照 §5 的模板逐行填；"门禁"一行先写运行的命令再写结果；时季矩阵、天气与画质、存档、声音几行不涉及时写"不涉及"；引用条目只写编号（§5）
- **存放截图**：放 `output/render-review/`，命名为"包-视角.png"或"包-Qn.png"，非默认状态依次加季节、钟点、天气、预设（§5）
- **任何改动**：不得命中 §3 的一票否决，其中包括违反 ENG-BUDGET §7 的硬规则、关掉本包前后的差值超出额度（§3 第 10 条）

## 1 审查视角

所有审查视角都是漫游相机的位姿（位置、偏航、俯仰）。用命令行参数 `--pose=<编号>` 把漫游相机放到对应位置。V 组视角用"目标点、方位、仰角、距离"描述，由辅助函数 `PoseMath.PoseFacing()`（ENG-ARCH §3）换算成位姿，相机朝向目标点。这些只是出生位置，不是另一种观看模式。F 组和 W 组是路线，用同一个参数启动脚本，脚本第一帧就是出生位置。统一用 1920×1080 截图。

- 位姿在当前图里解析：P 组查本图的 `waypoints.json`，V、E、F、W 组查本图的 `review.json`（SYS-MAPS §11）。下表是清水河的审查视角与路线，数据在 `game/data/maps/qingshuihe/review.json`（WP-27）。
- **T0 例外**：T0 是开始页的镜头（SYS-TITLE §2.1），离校区中心约 3.13 km，在活动范围之外，不是漫游相机能到的位姿；只在 `--title=1` 下有效，云卡只在开始页和过场里可见。

**命令行用户参数**（写在 `--` 之后，由 `Boot` 用 `OS.GetCmdlineUserArgs()` 读取；未知参数以非零退出码结束；SYS-VIEW §2.5）

| 参数 | 截图默认值 | 含义 |
| --- | --- | --- |
| `--pose` | `V1` | 位姿或路线编号，见本节的视角表。在当前图里解析（SYS-MAPS §11）；位姿不在可达空间里、或落在 30 m 缓冲带里时，以非零退出码结束（WLD-OUTSKIRTS §13.1）；`T0` 只在 `--title=1` 时有效 |
| `--clock` | `17.5` | 美术钟点，单位小时，取 [0, 24)（SYS-TIME §1）。世界时间从这一钟点起单调递增 |
| `--season` | `autumn` | `spring`、`summer`、`autumn`、`winter` |
| `--anim` | `240` | 动画时钟 `anim_time`，单位秒，取 [0, 480)。风、水、"一拍二"都按它走，与世界时间无关（SYS-TIME §0 第 2 条）。截图时冻结在这个值；录像和基准里按帧号推进 |
| `--flow` | `0` | 流逝倍率：`0` 暂停；`1`、`10`、`60`、`360`、`1440`（SYS-TIME §1）。截图一律用 `0`；不带参数正常启动时是 `1` |
| `--mode` | `fly` | 观察模式：`fly` 旁观，`walk` 行走（SYS-WALK） |
| `--ui` | `0` | `0` 隐藏全部界面（含小地图），只留署名；`1` 显示界面（UI-NAV §9、UI-VISUAL） |
| `--seed` | `1` | 随机种子 |
| `--weather` | `clear` | `auto`、`clear`、`few-clouds`、`cloudy`、`overcast`、`fog`、`light-rain`、`rain`、`thunderstorm`、`after-rain`、`snow`、`after-snow`（SYS-WEATHER §8）。给出具体天气时，这一天气一直保持；配 `--weather-remaining` 时只保持那么久，之后交给自动天气的生成器；`auto` 从开场起就是自动天气。不合季节的组合（例如 `--season=winter --weather=rain`）与未知值一样以非零退出码结束，日志给出替换建议 |
| `--weather-intensity` | — | 强度 0–1；不给时取该格的默认档（SYS-WEATHER §1） |
| `--wet` | — | 地面湿润度 0–1，用来直接出"雨后"一类的状态；不给时由天气推出 |
| `--weather-seed` | `1` | 天气种子（u32），只作用于新游戏：正常新游戏时取一次熵，之后跟着存档走（SYS-WEATHER §6.4、SYS-SAVE §12）；与 `--slot`、`--load` 同时给出时以非零退出码结束 |
| `--quality` | `fine` | 画面预设：`smooth` 流畅、`balanced` 均衡、`fine` 精致、`ultra` 极致（SYS-GRAPHICS §6），只在内存里生效、不写盘 |
| `--adaptive` | `0` | 自动调节画质：`0` 关、`1` 开。在 `--gate`、`--shots`、`--bench`、`--frames` 模式下默认 0（SYS-GRAPHICS §6） |
| `--map` | `qingshuihe` | 起始图（SYS-MAPS §11）。不给时取 `index.json` 的默认图（正常启动时是最近存档的图）；未知 id 以非零退出码结束 |
| `--spawn` | — | 在起始图的出生点里选一个：先查 `spawns`，再查本图的点位；未知 id 以非零退出码结束；`--pose` 给出时以 `--pose` 为准 |
| `--title` | `0` | `0` 跳过扉页和开始页，按命令行直接进入；`1` 走完整的开场流程：扉页 → 开始页 → 过场（SYS-TITLE §1）。不带参数正常启动时是 `1` |
| `--cine` | — | 只在 `--title=1` 时有效：标题页转为实时后不等输入，直接播指定的过场（`enter_new`、`enter_new_short`、`enter_continue`、`leave`），用于 G13 录像和体验脚本；不改设置里的"开场过场"（SYS-TITLE §5.3）。继续类配 `--load=<路径>`、`--save=off` |
| `--save` | `off` | `off` 或存档位 `1`–`3`，只管写（SYS-SAVE §12）：`off` 时不写、不挪任何文件、不拿锁，开始页照常只读地列出存档；`<n>` 表示自动保存写到 `s<n>`。正常启动时写回读进来的存档位；`--gate`、`--shots`、`--bench`、`--frames` 下默认 `off` |
| `--slot` | — | 启动时读存档位 `s<n>`：配 `--title=0` 时读完直接进入漫游；配 `--title=1` 时开始页的"继续"指向它。存档位为空、损坏或来自更新的版本时，以非零退出码结束 |
| `--load` | — | 读一份指定的 `.sav`（绝对路径；金档、复现用的档），源文件只读；之后写到 `--save=<n>`，没给就不写。与 `--slot` 同时给出时以非零退出码结束 |
| `--save-dir` | — | 换存档的根目录（绝对路径，默认 `user://saves`）：门禁、金档测试、开发时多开；目录不存在就新建 |
| `--audio` | `off` | `on`、`off`（SYS-AUDIO §17）。`off` 时音频内核不建播放器、只保留总线，Movie Maker 录出的 WAV 全零；正常启动为 `on`；`--gate`、`--shots`、`--bench`、`--frames` 下默认 `off`；声音门禁（G21）显式给 `on` |
| `--ui-theme` | `auto` | 界面主题（UI-VISUAL §11.12）：`auto` 按太阳高度切换，`light` 晶白，`dark` 深屏。常驻小部件（小地图、时间读数、临时提示、到达标题下的推荐小条）默认固定深屏，这个参数只影响大页 |
| `--motion` | `full` | 动效档位：`full`、`reduced`、`instant`；`--ui=1` 的界面截图用 `instant` |
| `--ui-scale` | 设置值 | 界面缩放 0.75–1.5，只在内存里生效 |
| `--ui-gallery` | 无 | 启动后直接打开界面的某一页（如时间卡、设置卡、存档页、开始页），供界面截图与 G14；只用于开发和门禁 |

截图默认值就是 `-- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off`，即默认状态（秋 17:30、晴，SYS-TIME §0 第 1 条、SYS-WEATHER §6.6）、流逝暂停、界面隐藏、精致预设、自动调节关、清水河、跳过开场、不写存档、无声。精致预设 1920×1080 原生就是视觉精度的验收基准（SYS-GRAPHICS §10）。出默认状态的截图时只换 `--pose`。带不带 `--map=qingshuihe` 画面完全相同（SYS-MAPS §17）。SYS-VIEW §2.5、ENG-ARCH §7、ENG-BUDGET §9、SYS-GRAPHICS §6 里的默认串与此一致。

- **优先级**：命令行 > `user://graphics.cfg` > 自动推荐 > 默认值。门禁和基准只由命令行决定，不读 `user://`（SYS-GRAPHICS §6）；读档只经 `--slot`、`--load`，起始图不因"最近存档的图"而改变（SYS-MAPS §11）。
- **读档时世界状态只来自存档**（SYS-SAVE §12）：`--slot` 或 `--load` 与世界类参数（`--clock`、`--season`、`--anim`、`--flow`、`--weather*`、`--wet`、`--pose`、`--mode`、`--map`、`--spawn`）中的任何一个同时给出，都以非零退出码结束；`--ui`、`--quality*`、`--adaptive`、`--frames`、`--shots`、`--out` 照常可用。

**门禁、基准与开发用的用户参数**（WP-01 实现解析，WP-27 定用法。`Boot` 只认本节两张表里的参数，其余一律非零退出）

| 参数 | 含义 |
| --- | --- |
| `--shots=<编号,…>` | 一次启动按顺序截多张，例如 `--shots=V1,V2,E1`；每张都按本节的"截图方式"等就绪。另有两个组名（SYS-TITLE §1.3、SYS-TITLE §11.2）：`T0`（`--title=1 --pose=T0 --ui=0`）、`FRONTIS`（出 8 个扉页变体） |
| `--out=<绝对路径>` | 截图或基准结果写到这里（`--shots` 时是目录），写完退出 |
| `--frames=<N>` | 基准或路线录制的帧数 |
| `--bench=1` | 基准模式：锁死画面设置和设备档（ENG-PERF §12.4）。默认 `--quality=fine`，开发机另跑 `--quality=ultra`（SYS-GRAPHICS §6） |
| `--tier=<档>` | `min-spec`（最低配置）、`dgpu-mid`、`dgpu-high`：只设设备档、跳过首次分档；只给 `--tier` 时，预设取该档的推荐预设（ENG-BUDGET §2、SYS-GRAPHICS §6）。只判回归，不判达标 |
| `--quality-set=<单项>=<值>,…` | 在预设之上覆盖单项，名字是 `QualityKnob` 的 snake_case（SYS-GRAPHICS §8）；名字未知或劣于最差值（SYS-GRAPHICS §2）时以非零退出码结束 |
| `--weather-remaining=<分钟>` | 当前天气还剩多久，之后交给生成器（SYS-WEATHER §8） |
| `--weather-queue="cloudy:30,rain:90,after-rain:45"` | 直接指定一串天气和各自的分钟数，用来录指定的转移边（G13）；序列不合法（不合季节，或含 `game/data/weather/` 转移表以外的边）时以非零退出码结束（SYS-WEATHER §8） |
| `--sunshower=auto\|on\|off` | 只决定下一条"雨 → 雨后初晴"边走不走太阳雨（SYS-WEATHER §2、SYS-WEATHER §8），默认 `auto`（按概率抽），截图默认串不加。第 12 张英雄帧"秋 17:30 太阳雨"按 SYS-WEATHER §2 的录制路径出：17:29 起步、`--flow=1`、`--weather-queue="rain:1,after-rain:60" --sunshower=on`，Movie Maker 固定步长，过渡开始后约 40 s 截取；G13 在白天和昏低日两个光段里，太阳雨与普通走法各录一段（`on`、`off`，SYS-WEATHER §9） |
| `--without=<WP-xx>` | 关掉一个包，测它开关前后的差值（ENG-BUDGET §8） |
| `--gate=<名称>` | 门禁模式，用退出码报告结果，例如 `--gate=registry`（G5）、`--gate=atlas`（G18，SYS-ATLAS §14.1）只检查不渲染；`--gate=maptravel`（G16 的同图重载，SYS-MAPS §13）、`--gate=acoustics`（G21j，SYS-AUDIO §22）要窗口 |
| `--inspect=<名称>` | 检查视图：`gallery` 部件画廊；`matid` 输出描线后处理的材质 ID（G7 的闪烁检查用） |
| `--gen=live` | 开发态实时生成，不读也不写烘焙缓存（ENG-ARCH §4） |
| `--focus=<实体 ID>` | 内容预览：按实体 ID 定位起始机位（`campus.json` 的实体或覆盖层新增的 `design:` 实体），镜头对准实体包围盒的中心，取方位 200°、仰角 30°，距离取刚好装下包围盒的值，落在可达空间外时就近合法化；查不到这个 ID 时以非零退出码结束（TOOL-PREVIEW §2） |
| `--pose-file=<绝对路径>` | 内容预览：读预览记下的机位文件（F6 写的 `user://preview/last-pose.json` 或其副本），回到其中的地图、观察模式、位置（行走时是脚位）、偏航与俯仰，以及钟点、季节、天气和动画时钟（TOOL-PREVIEW §3）。`--pose`、`--focus`、`--pose-file` 三选一，同时给出以非零退出码结束；`--focus`、`--pose-file` 只在开发构建（`OS.IsDebugBuild()`）里有，导出的 release 版按未知参数以非零退出码结束（TOOL-PREVIEW §1） |
| `--bake=<范围>` | 开发和 CI 的无界面烘焙器，例如 `--bake=all`、`--bake=map`（平面地图底图，SYS-ATLAS §2.1），配合 `--headless`（ENG-ARCH §4）；加 `--map=<id>` 只烘一张图，`--map=*` 依次烘全部（SYS-MAPS §11） |
| `--bake-cache=<绝对路径>` | 本机烘焙缓存目录（ENG-ARCH §4，D-165）。默认 `user://cache/bake/`；开发态默认指向环境变量 `UESTC_BAKE_CACHE`，所以开发与玩家走同一套逻辑 |
| `--bake-bg=0\|1` | 后台预烘，默认 `1`；`0` 时关闭（门禁和基准都关） |
| `--bake-strict=0\|1` | 默认 `0`；为 `1` 时缓存不命中直接报错、以非零退出码结束，不现场生成 |
| `--bake-verify` | G6 用：导出的 release 版在空的临时缓存目录里烘一批产物，逐文件哈希与启动集里的 `bake.manifest.json`（`res://bake/<地图>/boot/`）比对，有一个不同就以非零退出码结束（§2 G6） |
| `--save-at=<帧号>` | 从这一帧起，在下一个安全时刻存一次档，供 G20 的续跑等价用（SYS-SAVE §12） |
| `--ui-gallery=<页名>` | 启动界面组件画廊 `UiGallery`（UI-VISUAL §11.12） |
| `--ui-export-theme=<路径>` | 导出 `ui_theme.tres` 后退出（UI-VISUAL §11.12） |
| `--audio-seed=<u32>`、`--audio-limiter=0\|1`、`--audio-log=1`、`--audio-probe` | 声音门禁用（SYS-AUDIO §17、SYS-AUDIO §22）：音频种子（默认由 `--seed` 派生）；`--audio-limiter=0` 检查限幅前不削波（G21b）；一次性声日志（G21c）；正弦探针（G21d） |
| `--amb-solo=<层>`、`--amb-off=<层>`、`--amb-debug=1` | 环境声调试；屏幕角落的权重表只在 `--amb-debug=1` 下出现，不违反 INV-3 |

- **门禁和基准的本机烘焙参数**（D-165）：`scripts\gate.ps1` 先按改动选门禁（§2），再决定要不要准备烘焙数据（ENG-ARCH §6 第 3 步）：快检不烘焙；不带 `--bake-strict=1` 的检查缺什么就按玩家路径现烘；截图类门禁（§1 截图方式）、基准和 G10 先把它们用到的图（`--bake=all --map=<id>`）按哈希增量补进共享缓存（`UESTC_BAKE_CACHE`），全部图的 `--map=*` 只在集中测试或所选检查确实要全部图时跑。截图类门禁、基准和 G10 的每次启动，由 gate.ps1 在命令行末尾追加 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`，计时不受本机烘焙干扰。这几个参数不写进截图默认串，上面的默认串和各文档引用的默认串都不变。例外：G10 的冷缓存子项用空的缓存目录和 `--bake-bg=1`；`--bake-verify` 用空的临时缓存目录。

**常用的引擎参数**（写在 `--` 之前）：`--path game`、`--windowed`、`--resolution 1920x1080`、`--rendering-driver d3d12`、`--gpu-index <n>`、`--disable-vsync`、`--fixed-fps 60`、`--write-movie <路径>`、`--quit-after <帧数>`、`--headless`（只用于不渲染的检查）。引擎路径取环境变量 `UESTC_GODOT`，默认 `E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`。

**雨雪截图一律加 `--fixed-fps 60`**：手动天气（`--weather`）的 3 s 混合按实时帧间隔推进，帧数少时天气还没到位；固定帧率下天气按帧数推进，截图可复现。

```powershell
& $env:UESTC_GODOT --path game --windowed --resolution 1920x1080 --rendering-driver d3d12 --gpu-index <n> `
  -- --pose=V1 --clock=17.5 --season=autumn --anim=240 --flow=0 --mode=fly --ui=0 --seed=1 `
     --weather=clear --weather-seed=1 --quality=fine --adaptive=0 --map=qingshuihe --title=0 --save=off --audio=off `
     --out=<绝对路径>\WP-18-V1.png
```

**截图方式**（G7、G9、G12、G17 共用；WP-27 实现，WP-01 提供就绪信号）

1. **窗口模式**：不能用 `--headless`，无界面模式是空渲染器，画不出图。窗口不能最小化，最小化时引擎不绘制，所以要在已登录的交互式会话里跑。截图不经 gdUnit4 的命令行，它会主动最小化窗口。
2. **固定显卡和驱动**：渲染驱动固定为 D3D12；本机有多个适配器，用 `--gpu-index` 选登记的独显。启动后断言 `RenderingServer.GetVideoAdapterName()` 与机器登记表一致（本机为 RTX 4080），不一致就以非零退出码结束。
3. **截图期间忽略鼠标和键盘输入**，不捕获鼠标。
4. **等就绪再存图**：
   - 流式加载队列为空，相机周围各环都已"加载、挂载、管线就绪"（ENG-PERF §5）；
   - `Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface)` 和 `...PipelineCompilationsDraw` 连续 30 帧不再增长；`--ui=1` 的截图再加 `PipelineCompilationsCanvas`（UI-VISUAL §11.9）；
   - 再多等 8 帧，让倒影视口（每 2 帧更新一次）和调色板、通道稳定下来；
   - 然后用 `GetViewport().GetTexture().GetImage().SavePng()` 存图。根视口的图像包含全部 CanvasLayer，所以署名和界面也在图里。
5. **像素容差**：同一台机器、同一个驱动，连续截 10 次的像素差在 P0 标定，作为比对容差（ENG-ARCH §7）。
6. **录像**（G13、W 组）：用 Movie Maker，`--write-movie <目录>\f.png --fixed-fps 60 --quit-after <帧数>`，时间按帧号推进。再加一道"流式屏障"：每帧开始前，同步等本帧要用的格子全部就绪，再推进路线和时间。这样后台加载的快慢不会改变画面，结果可以复现。Movie Maker 每次都在 PNG 序列旁写一个 WAV；声音门禁给 `--audio=on`，与 G13 用同一批录像（G21d，SYS-AUDIO §17）。
7. **界面与声音**：界面截图前确认 `rendering/viewport/hdr_2d` 为关（UI-VISUAL §11.1）；截图一律 `--audio=off`（G7、G9、G12、G17，SYS-AUDIO §22）。

| 视角 | 目标点 (x, y, z) | 方位 | 仰角 | 距离 | 重点检查 |
| --- | --- | --- | --- | --- | --- |
| V1 全景（初始位姿） | 图书馆 (−193, 10, 51) | 200° | 35° | 1600 m | 箱庭全貌（含外围一圈街区）；画面下沿的西源大道与成都合院一带；远处地面断进雾海的那条线；O2 雾中楼顶；远景；主轴线。截面的立面从这里基本看不到（WLD-OUTSKIRTS §13.1） |
| V2 主轴线 | 图书馆 | 215° | 25° | 1100 m | 南门 → 主楼门廊 → 银杏大道 → 图书馆，这条视线不被遮挡 |
| V3 俯视采光顶 | 图书馆 | 180° | 68° | 160 m | 中庭、回廊、红桌和桌上的书 |
| V4 顺光 | 图书馆 | 250° | 22° | 520 m | 西湖、杏色的西墙、满月、穹顶倒影 |
| V5 逆光 | 图书馆 | 60° | 20° | 600 m | 镜头光斑、湖面碎金、树冠逆光暖边、雪山剪影 |
| V6 东湖与宣邦楼 | (127, 0, 390) | 250° | 28° | 700 m | 电子管穹顶最后受光、驳岸、六角亭、倒影 |
| V7 北部生活区 | (200, 0, −500) | 200° | 35° | 800 m | 宿舍窗口逐个亮起、店招、晾晒、车场 |
| V8 东区与月亮 | (560, 0, 60) | 240° | 22° | 850 m | 训练馆筒拱、球场树墙、月亮 |
| V9 截面与西侧外围 | 西侧截面线与 z = 262 的交点，y = −20（相机约在 (−1640, 775, 262)） | 270° | 62° | 900 m | 外缘截面的地层、地下水线、上缘季节色、水柱、下缘渐隐；西侧外围（天街、合作安置）；雾海与 O2。高空斜俯（WLD-OUTSKIRTS §8.1、WLD-OUTSKIRTS §13.1） |
| V10 图书馆正门 | 图书馆 | 127° | 20° | 180 m | 大台阶、敞开的二楼正门（WLD-INTERIORS §2.1）、门口的暖光、地垫与开放牌（WLD-INTERIORS §4） |
| V11 东北外围全景 | 校界形心 (50, 0, 77) | 45° | 30° | 1800 m | 蓉昌与互通、园区厂房、综保区屋面；远处西南的截面线外缘与雾海；O1、O2 预算的最坏视角之一（WLD-OUTSKIRTS §13.1） |
| E1 银杏大道人眼 | 位置 (−20, 1.7, 200)，偏航 35°，俯仰 0° | — | — | — | 落叶、路缘、长椅、树干、光柱 |
| E2 商业街人眼 | 位置 (−40, 1.7, −700)，偏航 180°，俯仰 0° | — | — | — | 店招、外摆、驿站、细节密度 |
| E3 图书馆中庭 | 位置 (−193, 1.7, 51)，偏航 0°，俯仰 20° | — | — | — | 完整室内（MAP-QSH-LIBRARY） |
| E4 地层内 | 位置 (−150, −8, 80)，偏航 90°，俯仰 0° | — | — | — | 地下呈现（SYS-VIEW §5.2） |
| E5 西湖水下 | 位置 (−400, −1, 40)，偏航 90°，俯仰 10° | — | — | — | 水下呈现（SYS-VIEW §5.3） |
| E6 看街长椅人眼 | 位置约 (−214, 1.7, 884)，偏航 180°，俯仰 +2°（以 WP-44 定的长椅位置为准） | — | — | — | 隔着栏杆竖杆看西源大道、草坪轨道、电子科大站雨棚和对街；长街尽头沉进雾里的那条线（WLD-OUTSKIRTS §13.1） |
| E7 西源大道低空 | 南门正对处，西源大道靠校一侧车道上空 20 m，离前沿线不少于 30 m；偏航 215°，俯仰 −3° | — | — | — | O1 第一排立面（L1）、临街店面（L0）在最近可达距离上的精度；O0 的路面、路灯、接触网 |
| T0 云隙望穹（开始页） | 图书馆 (−193, 10, 51)；相机约 (−1240, 495, 2928) | 200° | 9° | 3100 m | 偏航 13°、俯仰 −2.5°、垂直视场角 38°（SYS-TITLE §2.1）。云隙构图、箱庭与外围、云卡受光、雾海。只在 `--title=1` 下有意义（`--title=1 --pose=T0 --ui=0`），云卡只在开始页和过场里可见；49b 调整构图后同步本行 |
| F1 飞行路线 | 60 秒脚本，遵守速度律（ENG-BUDGET §4）：从 V1 高空俯冲到 E1（随高度降低自然减速）→ 穿过一栋楼 → 入地再出地 → 升到 80 m 以上 → 沿银杏大道上空加速到 87 m/s | — | — | — | 流式加载与卡顿，是否达标 |
| F2 压力路线 | 用调试开关绕过速度律，在离地 5 m 的高度以 87 m/s 横穿全校 | — | — | — | 只检查三项：不崩溃；没有超过 200 ms 的卡顿；停下后能回到正常级别 |
| F3 跳转压力 | 旁观模式下，全部点位（P00–P23，UI-NAV §6）两两之间各跳一次，24 个点位时共 552 次（UI-NAV §10）。跳转豁免速度律，改用预取与到达保证（UI-NAV §7.5）。 | — | — | — | 每次到达帧，目的地各环必须就绪或处在雾幕里；途中不穿楼；任何一帧的卡顿不超过 100 ms |
| F4 传送压力 | 在全部可传送标记（锚点、观景点、室内门口、路线起点，SYS-ATLAS §5）之间随机传送 300 次，旁观、行走各半（SYS-ATLAS §14.3） | — | — | — | 每次到达帧目的地就绪；任何一帧卡顿不超过 100 ms；单次传送不超过 2.4 s（行走 2.6 s） |
| P00–P23 点位 | 位置、偏航与俯仰、推荐状态、行走落点，全部按 UI-NAV §6 的表。P00 即 V1。P15 与 V9 同一位姿；P07 复核接触网是否横进构图（WLD-OUTSKIRTS §13.1）；P16–P23 是室内与高处的点位（WLD-INTERIORS §2.17），`Entry = Door`，跳转时从最近的开放入口走进去 | — | — | — | 构图成立；推荐状态下的画面。其中 P00、P05、P07、P09、P10、P11、P12 是英雄帧（SYS-TIME §3.5），进入 SYS-TIME §8 的集中测试评分 |
| W1–W14 行走路线 | 行走路线 R1–R14 的脚本（SYS-WALK §11、WLD-INTERIORS §13）：W1 落金主线、W2 东湖环湖、W3 西湖跨湖、W4 生活区晚归、W5 东行望月；室内串联路线 W6 八角登高、W7 主楼登顶、W8 东湖登钟、W9 大平台一圈、W10 立人楼上课路、W11 西区科研线、W12 宿舍熄灯、W13 食堂到会堂、W14 品学楼与训练馆。`--mode=walk`，1× 步速 3.6 m/s，眼高 1.62 m，按 SYS-WALK §11 的推荐状态（含天气）起步 | — | — | — | 没有卡点；每 30–50 m 有一拍；不违反 ART-FIDELITY §7 第 4、13 条（ART-FIDELITY §9 的步行巡检线）。W6–W14 由 G19g 的巡游机器人在集中测试时跑一遍 |

- **位姿合法性**（WLD-OUTSKIRTS §13.1）：全部 V、E、P 位姿、W 组起点和 SYS-ATLAS 的锚点位姿，都必须在旁观的可达空间内，而且不在 30 m 缓冲带里；由 G2 用纯 .NET 的求值检查，`--pose` 不满足时以非零退出码结束。T0 不受这条约束（SYS-TITLE §2.1）。
- **眼高**：E 组用 1.7 m，保持截图基线可比；行走的 1.62 m 由 W 组和行走巡检点覆盖（ART-FIDELITY §8 第 2 条）。复现截图时注意两者不同（SYS-WALK §2）。
- **"重点检查"一列按默认状态写**。只在低太阳时成立的项（例如 V5 的镜头光斑、E1 的光柱、V6 的穹顶最后受光），只在默认状态和日出日落附近的组合里验收；其他组合按 SYS-TIME §8 的判据验收。
- **P 组**在门禁 G7 里截两张：默认状态一张，推荐状态一张（`--clock`、`--season` 取 UI-NAV §6 的"推荐状态"列，`--weather` 取"推荐天气"列，写"晴"的用 `clear`）。
- **W 组**在集中测试时录一遍全程，`--flow=0`。R1 另录一遍 `--flow=1`（默认流逝，从秋 17:30 起步，SYS-WALK §11），检查近处的窗灯和路灯随钟点逐盏亮起、没有成片跳变。

**时季抽样**（默认状态加 SYS-TIME §8 的抽样矩阵）

- **默认状态**：本节的全部 V、E、P 视角在集中测试时全跑（G7，§2）。
- **其他组合**：按 SYS-TIME §8 抽样，由门禁 G12 在集中测试时执行：按 L16 正交表抽 8 组"视角 × 关键帧 × 季节"；再跑 4 季 × 12 关键帧共 48 组，每组轮换 2 个视角，并拼 4 × 12 联表人工打分。
- **外围与开始页**：V11、E6、E7 加入 G12 的视角轮换，集中测试的联表至少有一格是外围视角（WLD-OUTSKIRTS §13.1）；T0 默认状态一张，另按 G12 的抽样轮换时季，因为开始页会显示任何存档的世界状态（SYS-TITLE §11.2）。
- **天气与预设**：默认状态是晴、精致预设。其他天气按 SYS-WEATHER §9 并入同一套抽样：L16 正交表的一列放"天气组"、再用一列放"预设"（SYS-GRAPHICS §10），L16 的总数仍是 8 组；集中测试时另加一组天气轮换和"季 × 天气"联表。
- **关键帧换成钟点**：按该季的美术太阳轨迹（SYS-TIME §2），求出太阳高度达到该键时的钟点，晨侧、昏侧分开求；深夜取 00:30，与 SYS-TIME §3.2 的示例钟点一致。
- **巡检点轮换**：各包的 8 个巡检点（ART-FIDELITY §8 第 2 条）在每次集中测试时除了默认状态，再轮换一个时季组合。组合取 SYS-TIME §3.2 的 6 个关键帧（日出、上午、正午、黄金、蓝调、深夜）× 4 季，共 24 个，编号 0–23。第 n 次集中测试第 k 个巡检点（Q1–Q8 对应 k = 0–7）取第 (n + 3k) mod 24 号组合，这是拉丁方排法：同一次集中测试里 8 个点的组合互不相同，轮完 24 次后每个点都看过全部 24 个组合。
- **截图一律用 `--flow=0`**。过渡、跳转、模式切换要看的是连续性，用录像检查，不用截图（G13）。

## 2 门禁与测试节奏

**门禁编号与名称**（各文档 front matter 的 `gates` 字段只用这里的编号；子项写作 G19a、G21b 这样的形式，归所在的门禁。CORE-INDEX §3 的全局不变量编号为 INV-1 至 INV-12，不是门禁，`gates` 字段不指它们）：

| 编号 | 名称 | 子项 | 主要依据 |
| --- | --- | --- | --- |
| G1 | 类型与格式 | — | — |
| G2 | 单元测试 | — | — |
| G3 | 归属 | — | — |
| G4 | 边界 | — | — |
| G5 | 注册与启动进出 | — | — |
| G6 | 构建 | — | ENG-BUDGET §8 |
| G7 | 固定视角 | — | §1、SYS-TIME §8 |
| G8 | 静态预算 | — | ENG-BUDGET §2、ENG-PERF §12.5 |
| G9 | 近景巡检 | — | ART-FIDELITY §8 |
| G10 | 性能回归 | — | ENG-PERF §12.5 |
| G11 | 核显实测 | — | ENG-PERF §12.5、SYS-GRAPHICS §5 |
| G12 | 时季矩阵 | — | SYS-TIME §8 |
| G13 | 过渡无硬切 | — | SYS-TIME §8、SYS-WEATHER §9 |
| G14 | 界面 | — | UI-NAV §10、UI-VISUAL §13 |
| G15 | 行走与碰撞 | — | SYS-WALK §15 |
| G16 | 管线编译与切图 | — | ENG-PERF §2、SYS-TITLE §11.2、SYS-MAPS §13 |
| G17 | 预设底线 | — | SYS-GRAPHICS §10 |
| G18 | 地图一致性 | — | SYS-ATLAS §14.1 |
| G19 | 路径与连通 | G19a 图连通、G19b 封口与可达、G19c 门对位与名单、G19d 楼梯、G19e 可走性与时刻无关、G19f 进楼零等待、G19g 室内巡游、G19h 窗位与视觉 | WLD-INTERIORS §19 |
| G20 | 存档 | — | SYS-SAVE §14 |
| G21 | 声音 | G21a 素材入库、G21b 混音响度、G21c 加速不爆发、G21d 转场不爆音、G21e 设置有效、G21f 确定性、G21g 预算、G21h 覆盖与"对得上"、G21i 观者声、G21j 空间声学"窄处更明显" | SYS-AUDIO §22 |

**在哪里跑**：
- 能在无界面命令行跑的：G1–G4、G5 的注册检查、G8、G18、G19a–G19e、G20 的往返、迁移与损坏，以及 G21a、G21c 的调度器测试、G21e 的一部分、G21h 的枚举、G21i 的纯 .NET 部分。
- 要真窗口和真显卡的：G5 的窗口冒烟、G6（导出要在窗口模式下烘焙着色器，ENG-PERF §2）、G7、G9、G10、G12、G13、G16（含同图重载）、G17、G19f–G19h、G20 的退出时保存与续跑等价；G21b、G21d、G21f、G21j 用 Movie Maker 录音，与 G13 同一批录像。它们在 GPU 锁下排队。

**测试节奏**（D-167）：当前以主线完成度为先（D-168），测试按最小必要原则分三档，不设每天、每周的固定任务。下表"何时运行"一列只用这三档。

| 档 | 什么时候跑 | 跑什么 |
| --- | --- | --- |
| 快检 | 每次合入 `main` 前；直接在 `main` 上做时，每次推送前 | 编译（`dotnet build -warnaserror`，含 G4 的分析器）、改动涉及的程序集的单元测试、归属检查、无界面的注册检查；资源有变化时加一次 `--headless --import`。几分钟内跑完，不占 GPU 锁 |
| 核心专项 | 改动碰到核心体验时，在一段工作做完、准备合入时跑一次 | 只有三类：启动与进出（G5 的启动与进出核心范围，见下表）；行走不穿地、不掉出世界（G15 的前四项和入树检查；改到游泳或身体时加 G15 的游泳、身体两项）；存档不丢不坏（G20 的往返、损坏、退出时保存） |
| 集中测试 | 大量开发之后由主代理发起，一次全量跑完、统一修复；不跟单个工作包、阶段或时间走，主线闸口通过后和收尾前各至少一次 | 全部门禁全量：G6 导出与体积、全部审查视角与巡检、G10 性能、G12 时季矩阵与联表人工评分、录像、巡游机器人、同图重载、`--bake-verify` 全图比对、声音子项与听评；同时做风格评审和预算归因 |

- **按改动选**：`gate.ps1` 用 `git diff main...HEAD` 得出改动的路径（直接在 `main` 上做时，比对上次推送的提交），据此决定要不要跑核心专项。只改 `docs/`、注释或与运行无关的说明文字时，不跑任何门禁。
- **测试只锁本包输出**（D-199）：单测可以锁本包的产物，以及"原有条目的字节与顺序不变"；不锁别的包会合法增减的全局集合或指纹（例如全校设计树的整套指纹）。需要守住跨包的不变量时，写成按 id 核对的规则，让新增条目能通过。
- **不重复测没变的东西**：门禁结果按输入（相关路径与依赖的内容哈希）缓存，输入没变就沿用上次通过的结果。
- **同一组合只跑一次**：合入时一批候选的公共门禁（G1、G2、G4、G5）整批只跑一次，G3 按 owner 分跑，不为每个 owner 重跑公共门禁；`-NoCache` 同一组合只用一次；失败后有改动才重跑（ENG-TEAM-FLOW §2.5、ENG-TEAM-DUTY §5）。
- **`-SkipFormat`**：`gate.ps1 -SkipFormat`（默认关闭）只跳过 G1 的 `dotnet format --verify-no-changes`，G1 的编译、命名、引擎版本检查和按需 import 照旧；G1 的明细写 `format skipped`，计划里也记录开关状态。开这个开关时 G1 不读也不写缓存，免得跳过格式的结果被后来的普通 G1 当成已查过格式。它只用于合并时省重复项（下一条），不用来绕开还没通过的格式检查，也不拿它声称格式通过。
- **G1 的 import 重试**：`--headless --import` 只对原生崩溃 `0xC0000005` 自动重试一次，首次与重试的日志都保留；普通失败不重试，`ERROR` 扫描不绕过；不关闭多线程导入。
- **合并时省掉重复项**（D-206）：候选在自己的工作树里已经通过快检、squash 时没有解冲突的，合并时跳过 G1 的格式检查和 G4 的依赖规则检查，编译、G2、G3、G5 照跑，条件见 ENG-TEAM-FLOW §2.5。这两项在合并时从没单独抓到过问题，作者那一侧已经跑过。
- **开发中看效果不算门禁**：美术和内容包迭代时，用 `--pose` 截几张自己看就行，不走 GPU 锁，也不跑全套视角。
- **阶段闸口**（ENG-WP §1）里的金样和区域评审是看图评审，不跑门禁。
- **第一波的 G5 范围**：第一波里碰到启动的包合入时，按 G5 的“启动与进出核心范围”验收；完整 G5 只在主线最终闸口要求全部通过，局部证据不标“完整 G5 通过”。第一波 WP-02 主线合入是第二波 WP-36 开工的前提，而完整 G5 又要验证 WP-36 的真实传送，因此不要求每个第一波包都跑通完整 G5。
- **一票否决**（§3）在集中测试时全面检查；平时只要求不违反。
- **集中测试发现的问题**：按 §5 的格式登记，主代理按"核心体验优先"排序后分派给对应的包修复；`main` 上每个工作包一次合入一个提交，据此定位是哪个包引入的。

| 门禁 | 内容 | 何时运行 |
| --- | --- | --- |
| G1 类型与格式 | - `dotnet build -warnaserror` 通过：开启可空引用类型，分析器的警告一律当错误（含非穷举 `switch` 表达式的警告，ENG-ROADMAP §6 第 4 条）；<br>- `dotnet format --verify-no-changes` 通过（排除 `game/addons/` 下原样保留的第三方文件）；命名空间按 CORE-CONVENTIONS §3.4 由源码门禁检查，不用 IDE0130；<br>- `--headless --path game --import` 的日志里没有 `ERROR` 和 `SCRIPT ERROR`。 | 快检；`--import` 只在资源有变化时跑 |
| G2 单元测试 | - `dotnet test`：纯 .NET 的测试（契约、生成器、选级代码等）直接跑；需要引擎的测试用 gdUnit4Net，并标 `[RequireGodotRuntime]`；<br>- 公共校验：几何数值有限且有索引；法线是单位长度；同一输入生成的哈希一致（确定性）；单个格子的三角形不超上限。<br>- 世界时间：`WorldHours` 在同一 `Instance` 内单调递增，`Instance` 变化时单调检查从头开始（SYS-SAVE §5.2）；`SetClock(h)` 跳到 h 的下一次出现、最多前进 24 h；换季后 `WorldHours` 不减（SYS-TIME §1）。<br>- 天气过程（SYS-WEATHER §9）：确定性快照（同一种子 + 同一串输入得到同样的队列）；lint（SYS-WEATHER §6.2 的硬规则：晨昏带内无自动转移、最短时长、季节约束、禁用的边）；统计范围（晴的占比、每天段数，SYS-WEATHER §6.3）；补队列节奏无关；换季模糊测试；新游戏 2000 个种子都是晴。<br>- 画面设置：预设表 `game/data/quality-presets.json` 的每项都在 `KnobSpec` 的取值范围内、不劣于最差值（SYS-GRAPHICS §2）；`graphics.cfg` 的版本迁移链；命令行解析（`--quality-set` 名字未知或劣于最差值时非零退出）；`settings.cfg`、`input.cfg`、`graphics.cfg`、`audio.cfg` 只接受基本类型，用含 `Object(…)` 的样本文件测（SYS-SAVE §8）。<br>- 过场（SYS-TITLE §11.2）：过场文件的检查（SYS-TITLE §8.4）；**跳过终态一致**：固定天气种子，在多个时刻跳过，以及"简短""关"两条路径，终态与完整播放逐项相等（`WorldHours`、钟点、季节、流逝倍率、天气选择、种子与队列的哈希、`Instance` 的增量、位姿 ±1 mm、观察模式、视场角），继续过场同样；`IAppFlow` 的转移表，非法请求返回 `Rejected`，每个状态下的关闭请求都能走到退出，Entering 中退出时存下的是终态。<br>- 多地图（SYS-MAPS §13 中不需要引擎的部分）：`index.json` 与每份 `map.json` 过 schema；切换状态机（不可取消、只留最后一次、未知图 Rejected、失败退回上一张图）；天气的进图补算与节奏无关；`--map`、`--spawn` 的解析与非零退出。<br>- 平面地图（SYS-ATLAS §14.3）：解锁策略（`all-unlocked`、`proximity` 的进出半径防抖）；自定义标记的上限与存档往返；`MarkerId` 在一张图内不重复；开放清单加一项后标记自动出现。<br>- 外围（WLD-OUTSKIRTS §13.1）：位姿合法性；可达空间的 Python 与 C# 求值在管线导出的 1000 个采样点上一致，误差 ≤ 0.5 m（WLD-OUTSKIRTS §4.1）。<br>- 声音（SYS-AUDIO §22）：`IGroundCover` 的 CPU 与着色器比对；钟声、广播的排程测试（流逝超过 10×，即 60× 起为 0；快进跨整点不补）。<br>- 存档容器本身的往返、迁移、损坏用例归 G20。 | 快检：只跑改动涉及的程序集的测试；集中测试跑全部 |
| G3 归属 | - 改动的路径都在 `ownership.json` 给本包划定的范围内；<br>- 每个 `.cs`、着色器都有对应的 `.uid` 文件并已提交；<br>- 全仓的 UID 没有重复（复制目录会把 UID 一起复制出重复）。 | 快检 |
| G4 边界 | **C# 部分**（ArchUnitNET 写依赖规则，BannedApiAnalyzers 补充）：<br>- 区与区之间不互相依赖；内容（`game/gen/`）不得依赖 GodotSharp；<br>- 内容代码禁用 `System.Random`（随机数一律用带种子的生成器）、`DateTime.Now`、`Stopwatch` 与各类计时器，禁止直接写十六进制色值；<br>- gen 与烘焙子任务（`Campus.Gen.*`、`Campus.Pipeline.Map`，以及发布版里由 `IBakeService` 调用的其他烘焙代码）另禁：`Math`、`MathF` 的超越函数（`Sin`、`Cos`、`Tan`、`Asin`、`Acos`、`Atan`、`Atan2`、`Exp`、`Log`、`Log10`、`Pow`、`Cbrt` 等），一律改用契约的 `DetMath`；`System.Numerics.Vector<T>`、`System.Runtime.Intrinsics`、`Math.FusedMultiplyAdd`、`TensorPrimitives`；`System.Numerics` 里内部走三角函数的构造也禁用：`Matrix4x4.CreateRotationX/Y/Z`、`CreateFromAxisAngle`、`CreateFromYawPitchRoll`，`Quaternion.CreateFromAxisAngle`、`CreateFromYawPitchRoll`、`Quaternion.Slerp`，以及其他涉及三角函数的方法（`Vector2`、`Vector3` 的 `Lerp` 不在此列），改用契约里基于 `DetMath` 的同名工具（`DetMath.RotationY(…)` 等）（BannedApiAnalyzers，ENG-ARCH §2；`Math.Sqrt`、四则运算和 `Math.Round` 可以用），保证本机烘焙与 CI 参考逐字节一致（D-165）；<br>- 熵源只有一处登记的例外：新游戏时由 `ISaveService.NewGame` 取一次熵，作本存档的天气种子，写进日志和存档（SYS-WEATHER §6.4、SYS-SAVE §5.5）；天气过程的每次抽取都走计数器子流，不用 `System.Random`；<br>- 只有内核能创建 `MeshInstance3D`、`MultiMeshInstance3D`、材质和 RenderingServer 实例；只有 `kernel/collision` 能创建碰撞 body、调用 `ShapeOwnerAddShape`（SYS-WALK §15），声学用的 `Area3D` 也只有它能建（SYS-AUDIO §16）；<br>- `game/hud/` 只依赖契约和 UI-NAV §9 列出的几组接口（含 `IQualityService`、`IAudioService`、设置卡"存储"行用的 `IBakeService`）；可以经 `RenderingServer.GlobalShaderParameterSet` 写全局参数 `ui_night`，只此一项（UI-VISUAL §11.6）；<br>- 存档（SYS-SAVE §6、SYS-SAVE §9）：只有 `Campus.Kernel.Save` 能写 `user://saves`，字面量 `user://saves` 只能出现在这里；全工程不得用 `ResourceLoader`、`ResourceSaver` 读写 `user://`，没有例外：本机烘焙缓存是自定义的 `.tbin`，只有 `IBakeService`（`Campus.Kernel.Bake`）用 `FileAccess` 或 .NET 流读写，字面量 `user://cache/bake` 只能出现在那里（D-165）；存档代码和各段的实现里禁用 ConfigFile、`GD.StrToVar`、`GD.BytesToVar`、`GD.BytesToVarWithObjects`、`FileAccess.GetVar`、Godot 的 `Json`，以及不带 `JsonTypeInfo` 的 `JsonSerializer` 重载；系统时钟（`Time.GetUnixTimeFromSystem()`）只能在 `Campus.Kernel.Save` 里读、只写进 meta，`Campus.Kernel.Save` 加进计时白名单；`game/gen/` 不得引用 `Campus.Contracts.Save`；<br>- 多地图（SYS-MAPS §7）：不同图的程序集互不引用，通用程序集不引用带 `MapScope` 的程序集；正则禁止通用代码里出现完整的实体 ID 字面量、地图 ID 字面量和 `res://data/maps/…` 路径字面量（例外是 `kernel/map` 与门禁夹具）；<br>- 声音（SYS-AUDIO §16）：只有 `Campus.Audio.*` 能创建 `AudioStreamPlayer*`、调用 `AudioServer.Set*`；BannedApi 禁用 `AudioStreamRandomizer`，禁止给 `AudioStreamPlaylist.Shuffle`、`AudioServer.PlaybackSpeedScale` 赋值；<br>- 画面设置：除 WP-32 外，任何包都不读写 `user://graphics.cfg`，只通过 `IQualityService` 和 `IQualityAware.ApplyQuality()` 接收设置（SYS-GRAPHICS §8）；`game/gen/` 不得引用 `Campus.Contracts.Quality`；<br>- 不在模块代码里写死几何常量；`Engine.TimeScale` 不得改；不得从脚本读回全局着色器参数。<br>**着色器部分**（扫描 `.gdshader`、`.gdshaderinc`）：<br>- 只用白名单里的着色器；不出现 StandardMaterial3D，不在运行时新建 Shader、改 render_mode 或 `#define`；<br>- 不用内置的 `TIME`；<br>- 按距离做的判断不得用 `CAMERA_POSITION_WORLD`（阴影通道里它是灯的位置），一律用 `MAIN_CAM_INV_VIEW_MATRIX[3].xyz`。 | 快检：分析器随编译，依赖规则随 G2 |
| G5 注册与启动进出 | - 无界面运行 `-- --gate=registry`，用退出码判定：模块能被反射发现和注册，ID 不重复；认领的区域不冲突；调色板扩展的数量和色差合规；覆盖层的差异报告已生成；对每张图各跑一遍（`--map=*`），另查 `MapScope` 指向存在的图、通用模块里出现 `FeatureClaim` 即报错、各图数据的实体 ID 前缀不冲突、`map.json` 的出生点、`dataRect`、倒影湖能解析（SYS-MAPS §7）；<br>- **退役色名**（ART-STYLE §8.1）：C# 代码、着色器、`ToonSpec`、调色板扩展和覆盖层里不得引用退役色名 `shadow.contact`、`line.building`、`line.tree`（色号保留、不分给别的色名），也不得引用 `shadow.interior`（室内影色取调色板第 2 行，ART-STYLE §3.4）；出现即报错；<br>- 存档段（SYS-SAVE §10）：`[SaveSection]` 的 ID 和 Order 都不重复，Owner 与 `ownership.json` 一致，类实现了 `ISaveSection<T>`、T 是 sealed record、`TypeInfo` 来自源生成；<br>- 声音（SYS-AUDIO §3.1、SYS-AUDIO §4.2、SYS-AUDIO §19）：代码建出的总线名与 `AudioBuses` 常量一致；`amb:` 发声器 ID 在一张图内不重复；<br>- RPG 名字占位（ENG-ROADMAP §6 第 6 条）：实体 ID 前缀 `npc:`、`item:`、`quest:` 保留，观光数据使用即报错；`ownership.json` 里登记为 RPG 预留的目录（`game/entities/`、`game/contracts/Entities/`、`game/data/text/`、`game/data/maps/<id>/entities/`）观光期没有文件；<br>- 开放室内：开放清单 = 注册的提供者 = 门开着的楼（G19c，WLD-INTERIORS §2.0）；<br>- **地形覆盖层**（WLD-TERRAIN §3.8）：坡度不超过所在类别的上限、过渡带都在实体脚印内，校界零带 Δh = 0，高度在 −5…+10 m，垫层、走廊、水位、落差各项校验通过，每条地形操作有 `design:terrain:` ID 和理由、不越出本区，差异报告已生成；格边整数两侧一致由 G2 的单元测试查；<br>- **精度区数据**（ENG-ARCH §3）：`fidelity.json` 记下的输入哈希与当前的世界数据、覆盖层、点位和可行走遮罩一致，不一致报"精度区过期"；校内每个立面段、每个屋顶都有精度区；`fidelity.overlay.json` 的每条提级都写了理由；<br>- 界面：`game/hud/ui_theme.tres` 与 `UiThemeBuilder` 的输出一致；`project.godot` 里 `rendering/viewport/hdr_2d` 为关（UI-VISUAL §11.1、UI-VISUAL §11.3）；<br>- **服务替换必跑真实窗口**：改动涉及已注册的 `[KernelService]` 或 `[KernelNode]`（含 partial 类和删除旧注册）时，门禁强制在 D0 场景跑一次真实窗口，显式 `-Only G1,G2,G3,G4,G5` 也不能绕过，并且禁用这一项的旧缓存；正式的 `KernelStage` 缺实现时报 `NotImplemented`，绝不降级成截图通过。<br>- **启动与进出核心范围**：第一波里碰到启动的包合入时，在真实窗口跑 Boot → Title → 新游戏或继续 → 输入 → 关窗（WM_CLOSE）退出并存档 → 第二个进程恢复，使用现有 `save-close` 与 `save-resume`。日志没有着色器编译错误；向窗口发 WM_CLOSE 后 2 s 内退出，新一代通过校验，再用 `--slot` 启动，恢复截图与退出前一致（G7 的容差，SYS-SAVE §14）。无界面模式不编译着色器，短窗口冒烟（V1，约 300 帧）属于局部证据；行走碰撞另跑 G15 对应项，存档另跑 G20 对应项；<br>- **完整 G5**：`--gate=mainline` 保留全部 UI 与传送判据，未实现项如实报 `NotImplemented`；主线最终闸口才要求全部通过，局部证据不标“完整 G5 通过”；<br>- **界面可见状态**：不新增契约。WP-48b 把小地图、大地图、目录页的实际可见状态报给已有 `IUiSurfaceSink`，其余界面归 WP-35；WP-27 按实际输入及 `flags/rev` 的观测验收，不按控件名字推断。 | 快检：无界面的注册检查；核心专项：启动与进出核心范围（改动 `game/flow`、`kernel/`、`game/camera/`、地图服务、存档、开始页时）；完整 G5：主线最终闸口 |
| G6 构建 | - `--path game --export-release "Windows Desktop" <输出路径>` 通过（需要 .NET 导出模板）。不加 `--headless`：shader baker 要用渲染设备，无界面模式下不能烘焙着色器（ENG-PERF §2），所以导出也在已登录桌面的窗口模式下跑；<br>- 导出后量体积，两个上限都在 `budgets.json`（ENG-BUDGET §8）：**安装目录 ≤ 0.9 GB**（`export.install`），其中音频单列不超过 165 MB；用 7-Zip（LZMA2，`-mx=9`）打分发包 `UESTC-<版本>-win64.7z`，**≤ 0.7 GB**（`export.archive`）。各图的启动集 `boot_<地图>.pck` 对照 `budgets.json` 的 `maps.<id>`（每图 ≤ 60 MB），总预算是全部图之和。L0–L3 的格包、碰撞、室内包、平面地图底图不随包发（本机烘焙，ENG-ARCH §4），体积里没有它们；全图常驻的小数据随启动集发；7-Zip 由 WP-01 下载，登记进 `third_party.json`；<br>- **`--bake-verify`**：用导出的 release 版，在一个空的临时缓存目录里烘全图的 L2–L4、16 个抽样格的 L0 与碰撞、1 栋开放楼的 1 层、平面地图底图；逐文件哈希与启动集里的 `bake.manifest.json`（`res://bake/<地图>/boot/`）比对，有一个不同就不过。全图逐字节比对也在集中测试时跑；<br>- 资源清单里没有照片来源的素材；音频清单 `licenses.json` 完整，许可都在白名单内（没有 NC、ND、BBC、BY-SA、GPL，SYS-AUDIO §14）；<br>- 导出版能读到 `data/maps/**` 的全部 JSON（导出过滤器生效）；各 PCK 的路径集合两两不相交（SYS-MAPS §2.4）。 | 集中测试：含两个体积上限、`--bake-verify` 与全图逐字节比对 |
| G7 固定视角 | §1 的全部 V、E、P 视角都要满足（F、W 是路线，归 G10、G13、G15），按 §1 的"截图方式"出图，一律 `--audio=off`：<br>- 在 `--ui=0` 下，画面上只有 3D 画面和署名，没有边界提示；<br>- **影色自检**（SYS-TIME §8）：中性材质的影色相落在当前天光色 `ambient.sky` 的 ±30° 内；有彩材质的影色相与固有色相差 ≤ 20°，非绿色材质的影不落在 100°–150°；夜间暗部仍落在蓝紫区间。夜间（太阳高度低于 −6°）的截图，景物暗部（影色区域）的 L* 不低于 12（SYS-TIME §8）。天空和描线不算：深夜天顶 #161B3C、深夜描线 #1E1C34 本来就在 L* 11–12；<br>- 地平线以下没有露出背景；<br>- 轻微抖动相机后，材质 ID 保持稳定（检查闪烁，用 `--inspect=matid` 的调试视图）；<br>- 生成与主分支的差异报告。<br>- 外围：V9、V11、E6、E7、P15 按 WLD-OUTSKIRTS §13.2 的位姿出图；<br>- 开始页（SYS-TITLE §11.2）：T0（`--title=1 --pose=T0 --ui=0`）默认状态一张，另按 G12 的抽样轮换时季；扉页 8 个变体都存在，元数据与当前 T0 位姿、云卡布局和世界数据一致；默认变体与同一状态的实时 T0 首帧比对，差异在 §1"截图方式"第 5 条的容差内；<br>- 截图默认串带 `--map=qingshuihe` 时画面与不带时完全相同（SYS-MAPS §17）。<br>状态：默认状态；P 组另加推荐状态（§1）。 | 集中测试：全部 V、E、P 视角 |
| G8 静态预算 | 用纯 .NET 测试跑与运行时共用的 C# 选级代码，读烘焙产物的统计 JSON，预测以下位置各包的三角形（主通道 / 阴影）、绘制调用（主通道 / 阴影）和显存，超出 `budgets.json` 即拒绝：<br>- V1–V11、E1–E7、P00–P23、T0（SYS-TITLE §11.4、WLD-OUTSKIRTS §13.2）；<br>- 全图 64 m 网格上的贴地位姿（离地 1.7 m，8 个朝向），加 O0 街道带上每 64 m 一个（WLD-OUTSKIRTS §13.2）；<br>- 行走贴地位姿：W1–W14 路线上每 20 m 一个（眼高 1.62 m，8 个朝向）；每处开放室内的门内 3 m（朝外）、主空间、走廊、楼梯各 1 个位姿，按 ENG-BUDGET §2 的楼内视角额度判（WLD-INTERIORS §12、WLD-INTERIORS §19 的 G19h）；<br>- 新游戏过场路径上每 0.25 s 一个采样点（只算秋黄昏，SYS-TITLE §11.4）。<br>**按图**（SYS-MAPS §12）：每张图各跑一遍，对照 `budgets.json` 的 `maps.<id>`；预测里单列"外围"子项，核对 WLD-OUTSKIRTS §11.2 的分层上限。<br>每个位置按"秋黄昏（默认状态）、夏正午满叶、冬夜、夏夜雷雨"四种组合各预测一次，每项取四者中的最大值：秋黄昏压满飘落叶，夏正午压满满叶的植被和荷花，冬夜压满夜间光晕和雪花（显式 `--weather=snow --weather-intensity=1`），夏夜雷雨压满满叶、灯全亮、湿路灯条、积水和雨幕（`--season=summer --clock=21.5 --weather=thunderstorm --weather-intensity=1`，ENG-BUDGET §2、SYS-WEATHER §9）。<br>**画面预设**：四个预设各算一遍，各自对照 `budgets.json` 里该预设的资源上限（ENG-BUDGET §2），并检查单调性：极致 ≥ 精致 ≥ 均衡 ≥ 流畅（SYS-GRAPHICS §10）。<br>**按精度区小计**：预测结果另按精度区 K1–K5 出小计（三角形、绘制调用，主通道与阴影分列），核对 L0+ 专用几何和热点簇确实集中在 K1、K2（ENG-BUDGET §8"按精度区的预算侧重"）。<br>这一项不占 GPU，任何工作树都能跑；用 G10 的实测校准，偏差超过 15% 报警（ENG-PERF §12.5）。 | 集中测试 |
| G9 近景巡检 | 按精度区（ART-FIDELITY §0）出图，按 §1 的"截图方式"在窗口模式下拍（`--audio=off`）。每个点所在的精度区和判定距离读 WP-44 导出的精度区数据（ENG-ARCH §3）。<br>- **巡检点**：本包登记的 8 个巡检点 Q1–Q8，按 ART-FIDELITY §8 第 2 条在各区分配。<br>- **随机点**：认领区内随机抽 20 个点，按精度区分层抽，每点看四个方向：K1、K2 不少于 10 个，落在行走走廊和开放室内的用 1.62 m 眼高、`--mode=walk`，按 ART-FIDELITY §9 判；K3 约 6 个，离地 1.7 m，或在立面前 10–30 m；K4 约 4 个，用旁观模式，在屋顶上方 2–40 m、背立面前 2–15 m 和"能看不能进"的区域里各取。本包某个区的面积为 0 时，这个区的名额并给 K1、K2。<br>- **状态**：默认状态，加上本次集中测试轮换到的时季组合（§1"巡检点轮换"）。<br>- **底线**（每张都判，以自动检查为主）：裸面检测：离镜头 5–60 m 的表面上，一个 64×64 像素的格子里只有一种材质、没有描线，也没有明度起伏和画进贴图的分缝、色块，就算裸面；裸面格超过 5% 不通过；5 m 以内的近景由人工评分判；两个距离在低预设下都按细节系数换算（ART-FIDELITY §1）。另外，ART-FIDELITY §8 第 3 条的自动检查和底线 B11（ART-FIDELITY §2.0）的穿帮检查都要通过。<br>- **人工评分**：截图交质检子代理，按 ART-FIDELITY §8 第 2 条的五项打分，3 分即"满足底线、不粗糙"。分数线按精度区：K1、K2 总分不低于 20，且没有一项低于 3；K3 总分不低于 17，且没有一项低于 3；K4、K5 只判底线，每项不低于 3。<br>- **跳变检测**：按 ART-FIDELITY §8 第 4 条，任何区都跑。<br>- **外围**（WLD-OUTSKIRTS §13.2）：WP-47 的 8 个巡检点要在可达空间内，可以落在缓冲带里（截图时 `edge_fog` 为 0）。外围没有楼内和地下，8 个点取 O0 街面 1.7 m 两个、前沿线上正对临街店面 1.7 m 一个、前沿线以内离地 25 m 朝外看第一排立面两个、离地 300 m 俯看 O1 两个、站台雨棚下一个；裸面检测的距离阈值按最近可达距离 d_min 判（WLD-OUTSKIRTS §4.1）。O0 按 K3 判，分数线同 K3；O1–O3 按 K5。<br>- 开放室内（WLD-INTERIORS §19 的 G19h）：每个开放入口门外 3 m 朝里、门内 3 m 朝外各一张（每次集中测试轮换 10%）；楼梯平台每次集中测试随机 5% 朝平台窗外拍一张；随机点里加"进入开放室内的随机房间"。巡检点里的"楼内 1 个"，没有开放室内的包用旁观穿进关着的楼、看简化室内，判不空、不黑、不透天（WLD-INTERIORS §5.3）。 | 集中测试，并做巡检点轮换（§1） |
| G10 性能回归 | 在集中测试时运行，持有 GPU 独占锁。测的是导出的 release 版，不是编辑器。<br>视角：命中本包格子的视角，加 V1、E1、F1，再加"V1 夜间"（冬 21:00 下雪，`--season=winter --clock=21 --weather=snow --weather-intensity=1`，与 ENG-PERF §12.4 相同）、"E1 夏季"（夏 12:50，`--season=summer --clock=12.83`）和"E1 夏夜雷雨"（`--season=summer --clock=21.5 --weather=thunderstorm --weather-intensity=1`）。另加 T0（默认状态，SYS-TITLE §11.4）；改动外围的包加 V11、E7（WLD-OUTSKIRTS §11.2）。<br>**存档**（SYS-SAVE §13）：快照主线程 p99 ≤ 0.3 ms；缩略图那一帧主线程 ≤ 5 ms（1080p）；从关闭请求到进程结束 ≤ 1.5 s。**声音**：G21g 的声部、音频主线程与内存随本门禁的同一套运行一起测（SYS-AUDIO §22）。<br>**冷缓存子项**（本机烘焙，ENG-ARCH §4）：空缓存目录、`--bake-bg=1`，测两项，初值如下：<br>- 首次运行到转为实时 ≤ 25 s；<br>- 沿 F1 行走一圈，碰撞冻结累计 ≤ 2 s，祖先格兜底时长另作记录。<br>其余视角和基准一律带 `--bake-cache=<UESTC_BAKE_CACHE> --bake-bg=0 --bake-strict=1`（§1）。<br>画面预设：跑精致和极致，再加跑流畅，只判回归（SYS-GRAPHICS §10）；一律 `--adaptive=0`。<br>计时：`RenderingServer.ViewportSetMeasureRenderTime` 打开后，逐帧读 `ViewportGetMeasuredRenderTimeGpu` 和 `...Cpu`。<br>方法：候选版本和合并基点按 ABBA 顺序各跑 5 次，同时满足下面两条判为疑似回归，再跑 10 对 10 复核：<br>- 单侧 Mann-Whitney U ≤ 2；<br>- Hodges-Lehmann 位移 ≥ max(3%, 0.25 ms)。<br>本次改动的位移计入本包的帧时账（ENG-BUDGET §8 的帧时占比），超出额度即拒绝。<br>详见 ENG-PERF §12.5。 | 集中测试，持有 GPU 独占锁 |
| G11 核显实测 | 设备：一台最低配置的笔记本（酷睿 Ultra 第一代的 Arc 核显或 Radeon 780M 同级，16 GB 双通道内存，SSD，ENG-BUDGET §2）；手边没有就跳过，并在回报里说明。<br>测试：V1、V2、E1 各 30 s × 3 次；F1 循环 10 分钟。<br>判定：漂移 ≤ 10%，档位最多降 1 级（ENG-PERF §12.5）；首次分档给出的推荐预设与这台机器的设备档一致（SYS-GRAPHICS §5、SYS-GRAPHICS §10）。音频主线程在最低配置档默认的降级下（在播 3D 声源上限 24）平均 ≤ 0.35 ms（SYS-AUDIO §18.1，ENG-BUDGET §8）。 | 集中测试，可选 |
| G12 时季矩阵 | 按 SYS-TIME §8 执行，抽样方式见 §1"时季抽样"：<br>- 快速版（需要时）：默认状态跑全部审查视角（即 G7）；再按 L16 正交表抽 8 组"视角 × 关键帧 × 季节"，其中一列放"天气组"、一列放"预设"，总数不变（SYS-WEATHER §9、SYS-GRAPHICS §10）。<br>- 集中测试：4 季 × 12 关键帧，共 48 组，每组轮换 2 个视角；加一组天气轮换（SYS-WEATHER §9）；再跑"前推 24 小时"（以各档流逝速度和拨时间环两种方式）和"四季轮换"两条脚本。<br>- 视角轮换：加外围视角 V11、E6、E7，联表至少有一格是外围视角（WLD-OUTSKIRTS §13.1）；T0 按同一抽样轮换时季（SYS-TITLE §11.2）。截图一律 `--audio=off`。<br>- 集中测试时拼出 4 × 12 的截图联表，另加"季 × 天气"联表（SYS-WEATHER §9），风格负责人逐格按 5 分制打"好看"分，任何一格低于 3 分就返工；每个天气格的代表帧不低于 3 分（SYS-WEATHER §2）；英雄帧（SYS-TIME §3.5：P00、P05、P07、P09、P10、P11、P12 的推荐状态，加 SYS-WEATHER §2 增补的天气英雄帧，以及第 12 张"秋 17:30 太阳雨"）必须达到 4 分以上，另加一遍流畅预设，不低于 3 分（SYS-GRAPHICS §10）。<br>- 每张矩阵截图都跑 G7 的自动判据，另加调色板逐行自检：影色自检（同 G7，SYS-TIME §8）；画面明度分布（默认格 L 中位 ≥ 0.62、白天格 ≥ 0.74，L < 0.30 的像素 ≤ 5%，不含描线，SYS-TIME §8）；主体与背景的明度差不小于 8，雨天、雾天照样适用（SYS-WEATHER §3）；夜间景物暗部的 L* 不低于 12（天空和描线不算）；夜间窗灯面积不超过画面的 15%。<br>- 浓雾下主要地标（八角穹顶、宣邦楼钟楼）仍可辨认（地标豁免，SYS-WEATHER §9）；钟面指针跟随当前钟点。 | 集中测试：48 组加天气轮换，并拼联表人工评分 |
| G13 过渡无硬切 | 录像按 §1 的"截图方式"第 6 条（Movie Maker 加流式屏障）。<br>**时间与季节**（SYS-TIME §8）：录下顺时针拖动时间环（含拖满一圈）、前向跳转钟点（跨日出、跨日落、跨午夜各一条）、相邻换季、跨两季换季、从冬到秋的换季（一律正向转场，SYS-TIME §5）、转场途中打断、"回到落金初灯"，以及"减少动态效果"开启时（UI-NAV §7.6）的同样几条。过渡期间：<br>- 相邻两帧的平均 OKLab ΔE 不超过 2，最大局部 ΔE 不超过 6，有雾幕遮罩的区间除外；<br>- 主光方向只按量化步转动，拖动、跳转、换季时连续转动（ENG-PERF）；不允许影子在某一帧突然换方向（SYS-TIME §2"主光交接"）。<br>**天气**（SYS-WEATHER §9）：<br>- 动态格是"转移边 × 光段 × 季节"，约 136 格，用约 48 段 `--weather-queue` 录像全部覆盖，集中测试时全量跑；判据同上（SYS-MOTION §12），闪电帧打标记另判；<br>- 另录：跳转与拨时间环途中的一次天气过渡、快进经过降水、换季时的天气替换、手动 ↔ 自动、过渡途中打断，以及"减少动态效果"下的同样几条；<br>- 闪电：每秒不超过 3 次闪光，全画面平均 ΔE 不超过 2，没有全屏闪白（SYS-WEATHER §5）。<br>**画面设置**（SYS-GRAPHICS §3–§5）：即时类改动和自动调节的每一级降档都在 0.5–1 s 内淡化完成；关移轴、镜头光斑、光柱、暗角时 0.5 s 淡出；雾幕类改动只在 0.3 s 雾幕里生效。<br>**跳转与模式切换**（UI-NAV §7、UI-NAV §10，SYS-WALK §5）：<br>- F3 路线全部达标（§1）；<br>- UI-NAV §7.4 的每种打断方式各录一条：移动键、鼠标累计移动超过 30 px、雾幕阶段打断、途中改选点位、再按同一点位；<br>- 旁观切行走、行走切旁观各录一条；<br>- 镜头不跳变：上面这些录像都跑 ART-FIDELITY §8 第 4 条的跳变检测，雾幕区间除外。<br>**传送**（SYS-ATLAS §7、SYS-ATLAS §14.3）：<br>- 旁观、行走各一条；室外到室内门口一条；室内观景点一条；跨 600 m 以上一条；"减少动态效果"开启时一条；<br>- 另录：雾幕升起前按 Esc 取消一条，大地图开合一条；<br>- 判据：雾幕遮罩区间以外照常判 ΔE；到达帧目的地各环就绪；镜头不跳变。<br>**开始页与过场**（SYS-TITLE §11.2）：<br>- 录 `enter_new`、`enter_new_short`、`enter_continue`（旁观室外、行走室外、室内三种存档）、`leave`，以及新游戏在 2、5、20、40 s 跳过、继续在 3 s 跳过；"减少动态效果"开启时再录一遍；<br>- 判据同上，云卡全遮、雾幕和扉页交叉淡化的区间除外；镜头跑跳变检测。<br>**外围边界**（WLD-OUTSKIRTS §13.2）：<br>- 在 20 m、200 m、900 m 三个高度，分别以 12、50、200 m/s 朝外飞；在 O1 上空往下俯冲；顶到 3 km 半径和 2000 m 上限；从校内地下朝外穿过校界；<br>- 减速和推回都连续，没有撞停和跳变；`edge_fog` 的变化满足 ΔE 判据。<br>**声音**：本门禁的全部录像同时写 WAV，供 G21d 判声音硬切（SYS-AUDIO §17）。 | 集中测试 |
| G14 界面 | 按 UI-NAV §10 与 UI-VISUAL §13（地图部分按 SYS-ATLAS §14.2，开始页部分按 SYS-TITLE §11.2）：<br>- **键盘**：能走完 UI-NAV §8 的全部控件，以及开始页、存档页、设置卡（含"画面"页、"声音"页）、大地图（SYS-ATLAS）、确认对话框；焦点不逃出打开着的大页；只用键盘完成一次传送（G → ↑↓ → Enter → Enter），另跑一遍"飞过去"；开始页 ↑ ↓ Tab Enter Esc 可用，默认焦点有存档时在"继续"、没有时在"新游戏"，禁用的"继续"可聚焦并读出原因；<br>- **对比度**：按 UI-VISUAL §3.7 的令牌表自动检测。大页（大地图、设置、存档页）晶白、深屏各验一遍；开始页按"压在画面上"验（UI-VISUAL §3.6）；常驻小部件（小地图、时间读数、临时提示、到达标题下的推荐小条）默认固定深屏，在亮墙和天空上也要达标。预报行、读数按文字判；禁用件按豁免处理，但要有可读的原因。地图上的文字 ≥ 4.5:1，可传送标记、玩家、校界、水岸 ≥ 3:1，道路不判；片名、菜单、字幕在 8 个扉页变体和抽样的 T0 状态下都达标；<br>- **交班**：只在越过回差时触发；扫过 0.6 s 内除前沿 2% 外对比度都达标；阈值附近暂停 60 s 不来回切；跨日快进只在结束时切一次；减少动态效果下直接换；锁定晶白、深屏时不切；大地图与其余大页在同一次扫描前沿里切换。常驻件（默认固定深屏）在交班前后逐像素不变，设成"常驻件：随主题"时与大页在同一次前沿里切换；默认格（秋 17:30）和白天格下，常驻件的深屏面板对背后画面的明度对比 ≥ 3:1（UI-VISUAL §13）；<br>- **常驻件检测**（UI-VISUAL §2.4、UI-VISUAL §3.6、UI-VISUAL §9.9）：按组件登记表在截图和录像里圈出常驻件的区域，包括小地图、时间读数（时间卡与收起条）、临时提示、到达标题下的推荐小条，以及同一角落里按常驻件处理的速度条与观察模式、天气签条、界面态右上的三个图标钮；默认设置下这些区域一律是深屏面板，同一条底边上不出现一半晶白一半深屏；常驻件固定深屏、又没有打开着的大页时，交班不出 `ShiftDayNight`（只在界面上真有变化时才出）；<br>- **声音页**（UI-VISUAL §11.9、SYS-AUDIO §13）：设置卡的声音页（约 30 个控件）与画面页一样随设置卡按需创建、关闭即释放，不预建隐藏，关闭后实例数回到打开前的值，`PipelineCompilationsCanvas` 不增长；只用键盘能走完全部控件，每个控件有可读的名字；界面音效关闭时 `PlayUi` 不产生声部（与 G21e 同一套测试）；<br>- **发丝线**：缩放 1.0、1.25、1.5 和 1280×720 下边线都是 1 物理像素；<br>- **闪烁**：开始页、大地图开合、传送、存档成功、交班、等待灯的序列里，同一区域亮度变化 ≥ 10% 的事件每秒 ≤ 1 次，没有全屏闪白；<br>- **辉光与色彩面积**：晶白画面上没有外发光；深屏同屏发光元素 ≤ 3 个、半径 ≤ 4 px；常驻 HUD 的清蓝像素 ≤ 界面像素的 15%、≤ 全屏的 2%；暖色点 #FFE0A8 一屏最多一处；<br>- **减少动态效果**：重录上面的序列，相邻帧差只来自透明度；<br>- **遮挡**：最小窗口 1280×720 和 21:9 的 2560×1080 下，`--ui=1`、四角组件全部展开（含小地图与它的两个按钮、天气签条和预报行）时，不遮挡画面中央 60%；设置卡只占左侧约 1/3；开始页的片名和菜单不进中央 60%；大地图、存档页是全屏页，单独登记豁免；<br>- **漫游态**：只有小地图和临时提示；U、`--ui=0`、过场、开始页、返回标题途中、存档页、大地图打开、设置卡打开时小地图都隐藏（SYS-ATLAS §3、UI-NAV §3）；`--ui=0` 时只剩 3D 画面和署名，开始页和放映层也不出现；<br>- **登记**：画面上只出现 UI-NAV、UI-VISUAL、SYS-ATLAS、SYS-TITLE 登记的组件；3D 画面里没有地点标签、选中描边、世界空间标记或光柱（`--ui=0` 下追踪、选中、悬停标记前后各截一张，像素差在 §1 的截图容差内）；界面资源清单里没有银杏、书签、纸纹意象；`ui.json` 的色值不出现在任何 3D 材质和调色板纹理里；跳过环只表示"按住"；字幕写进读屏的实时区域；<br>- **缩放**：4K、系统缩放 175% 下界面占屏比例与 1080p、100% 相同（不重复乘 DPI，UI-VISUAL §11.10）；<br>- **画廊截图比对**：`UiGallery` 的全部组件 × 六种状态 × 两套主题 × 两档缩放；<br>- **性能**：界面主线程平均 ≤ 0.2 ms（小地图 ≤ 0.02 ms，大地图打开时界面合计仍 ≤ 0.2 ms）；画布渲染 CPU ≤ 0.3 ms；`HudModel` 每秒变化事件 ≤ 10 次；每帧对地图控件 `Text`、`Size`、主题的写入次数为 0（SYS-ATLAS §10 的逐帧例外只许着色器参数和变换）；可见且在处理的 Control ≤ 400、总数 ≤ 700；最低配置档上大地图 GPU ≤ 0.3 ms；雾幕或扉页之后 `PipelineCompilationsCanvas` 不增长、新生成的 MSDF 字形为 0；<br>- **其他判据**：时间环逆时针拖动时世界时间不减；Y 单击只在本季可选的格里循环，按住 0.6 s 回到自动；画面页只经 `IQualityService`；<br>- 读屏（AccessKit，实验性）：尽力而为。自动只查"每个可获得焦点的控件都有可读的名字"，实际朗读人工抽查；传送到达时 live region 写入"已到达：……"。 | 集中测试 |
| G15 行走与碰撞 | 按 SYS-WALK §15：<br>- 每格的碰撞地面与 L0 地面相差不超过 2 cm；<br>- 每条楼体轮廓边都有对应的墙段；<br>- 抽样检查碰撞体的水平外扩不超过 0.3 m；<br>- 在可行走遮罩上随机撒 2000 个点放下角色的圆柱碰撞体：不穿透，1 秒内着地；<br>- 从 200 个随机点出发，各按脚本随机走、跑、跳 30 s：不进入关着的楼和封口以外，不穿地，卡住不超过 3 s；<br>- 可达性检查（SYS-WALK §7 第 5 条，计入地形高差）：只到得了 WLD-INTERIORS §3 登记的三处屋顶或高处，而且只经开放室内的楼梯或电梯；其余楼顶都连不到；<br>- **地形**（WLD-TERRAIN §7.4、WLD-TERRAIN §7.5）：碰撞高度场与 L0 网格的整数逐格相同（洞格除外）；相邻碰撞格接缝两侧坡度折角 > 5° 的位置出统计报告（不阻断）；`Stand` 落点周围 5 m 内坡度 ≤ 5%；<br>- 行走模式下跳到或传送到全部可传送标记（点位、锚点、室内门口、路线起点，SYS-ATLAS §5）的 `Stand`：都能正确落地；<br>- W1–W14 全程没有卡点（§1）；室内巡游机器人见 G19g；<br>- **校界缓停**（WLD-OUTSKIRTS §7、WLD-OUTSKIRTS §13.2）：从校界上 20 个随机点，以 3.6 m/s 和 5.6 m/s、正面和斜 30° 走向栏杆、闸机、格栅：都在 0.6 m 内平滑停下、速度单调减小；能沿栏杆滑走；"校外暂未开放"出现一次、60 s 内不重复；回位兜底触发 0 次；<br>- **自动门**（WLD-INTERIORS §4.1、SYS-WALK）：开放室内的房门、楼梯间防火门和走廊玻璃门，以 1× 步行和 2× 疾跑穿过时，到达门洞之前门扇已经开过 75°（触发距离 max(1.5 m, v × 0.75 s)，WLD-INTERIORS §4.1），门扇离观者而去、从不扫向观者，全程不被门扇挡住；门扇转动和停在开位时没有碰撞，关好（开度 < 2°）之后才恢复薄盒碰撞；观者在扫掠区里时门不回关；回关途中观者再走近，门立即重新推开；<br>- 碰撞格入树检查：入树前已加完全部形状，body 只在主线程创建和删除，每格入树不超过 0.3 ms（SYS-WALK §15）；开放室内的碰撞按房间簇拆成 ≤ 250 个形状的 body，每帧入树 1 个、≤ 0.3 ms（WLD-INTERIORS §6.1）；<br>- **游泳**（SYS-WALK §15）：软岸、台阶岸、游泳馆池各一处：走入、跳入、从旁观落到泳区水面都进入游泳，入水事件各一次；水面停留不来回切换；朝登记出口自动上岸，栏杆、驳岸、封口处试探失败时留在原位、不穿墙；不开放的水域进不去；泳态下不发脚步；多档帧率下同一串物理输入的子状态轨迹相同；<br>- **身体**（SYS-WALK §15）：身体开和关两种情况下同一串输入的物理轨迹与脚步事件逐步一致；第一人称平视时画面里没有身体，低头看得见脚；旁观、过场和开始页里看不到角色；瞬移之后身体不从旧位置滑过来。 | 核心专项：WP-42、WP-43、WP-44 和提供碰撞体的包改动碰撞或行走时，跑前四项和入树检查；改到游泳（WP-42、WP-44、WP-31a 的泳池）时加跑游泳一项，改到身体（WP-42、WP-54a）时加跑身体一项的物理轨迹一致；其余项在集中测试 |
| G16 管线编译与切图 | 基线见 SYS-TITLE §11.2，同图重载见 SYS-MAPS §13：<br>- **基线**：`--title=1` 时从标题页转为实时那一刻算起；`--title=0` 时从雾幕淡出时算起。基线之后，`Performance.Monitor.PipelineCompilationsSurface`、`PipelineCompilationsDraw` 和 `PipelineCompilationsCanvas`（界面着色器，UI-VISUAL §11.9）三项计数不得再增长，新生成的 MSDF 字形为 0。检查路线：F1、F3、F4、W1，以及 SYS-VIEW §5 的穿墙、入地、入水；第一次按 M、第一次打开传送卡、第一次主题切换之后（SYS-ATLAS §14.3）；外围边界的外飞录像（WLD-OUTSKIRTS §13.2）；<br>- **开始页**（`--title=1`）：开始页停 60 s；依次打开存档页、设置卡的每一页、数据手册；播 `enter_new`、`enter_continue`、`leave` 和一次跳过；进入游戏后打开大地图和设置卡；<br>- "前推 24 小时"（各档流逝速度和拨时间环两种方式）、"四季轮换"、"天气巡回"（SYS-WEATHER §9）和"过场体验"（SYS-TITLE §11.3）脚本跑完，`PipelineCompilationsDraw` 的计数为 0（ENG-PERF）；保存不增加计数（SYS-SAVE §14）；<br>- 画面设置：每次"应用"的雾幕淡出之后三项计数不再增长（计数基线在这一刻重置，SYS-GRAPHICS §4）；"预设轮换"脚本（SYS-GRAPHICS §10）跑完，每次雾幕淡出后计数不增长；<br>- **同图重载**（SYS-MAPS §13，`--gate=maptravel`）：截图默认串加 `--gate=maptravel` 启动，到 V1 就绪后记基线 B0（显存、缓冲、内核账里的实例数和字节、`ObjectCount`、`ObjectResourceCount`、`ObjectNodeCount`、`ObjectOrphanNodeCount`、物理 body 数、`GC.Collect()` 之后的托管堆、三项管线编译计数）并截 S0；连做 3 次 `Travel(qingshuihe, Cause = Test)`。判据：每次与 B0 相差在 ±5% 以内，孤儿节点不增长，托管堆增长 ≤ 10 MB；Unload 那一帧本图的 RenderingServer 实例和碰撞 body 归零；旧 `IWorldModel` 在 `GC.Collect()` 后已回收；三项编译计数全程不增长（每张图 Warm 之后同样）；截图与 S0 的差在 G7 的容差内；每个 `IMapAware` 每次各收到一次 `OnMapUnloading` 和 `OnMapLoaded`；热重载 ≤ 5 s。集中测试时另加行走模式一遍、`--weather=rain` 一遍。 | 集中测试（含同图重载） |
| G17 预设底线 | 按 SYS-GRAPHICS §10：<br>- 每个预设各加一套"最差值配置"：渲染比例等不参与自动调节的项保持该预设的值，参与自动调节的项降到 `KnobSpec` 的最差允许值（SYS-GRAPHICS §5、SYS-GRAPHICS §8；实时阴影和西湖倒影的最差值见 SYS-GRAPHICS §2）。四个预设和四套最差值配置都在 V1、V4、E1、E3 和 V7 夜间截图，按 §1 的"截图方式"出图，`--adaptive=0`；<br>- 自动判：描线像素占比不低于精致预设的 70%（用 `--inspect=matid`）；投影存在；西湖倒影存在；影色自检通过（同 G7，SYS-TIME §8）；夜间景物暗部 L* ≥ 12；没有黑洞和穿帮；裸面检测（60 m 的距离阈值乘该预设的细节系数）；<br>- "预设轮换"脚本：四个预设和若干自定义组合依次切换，不崩溃，显存回到基线 ±5%，每次雾幕淡出后管线编译计数不增长（同 G16）；<br>- 与像素无关的结构性条款（ART-FIDELITY §7 的黑洞、灰影、深度闪烁、季节残留、室内外亮灯一致、夜间 L* ≥ 12、影色自检，以及身份特征、近景 L0+ 建筑细节）在任何预设下都必须过；与像素相关的条款以精致为基准换算（SYS-GRAPHICS §10）。 | 集中测试 |
| G18 地图一致性 | 按 SYS-ATLAS §14.1。无界面运行 `-- --gate=atlas --map=<id>`，只检查、不渲染，用退出码报告；每张图各跑一遍：<br>- **底图哈希**：`map.manifest.json` 里的世界模型哈希和烘焙代码哈希，与当前的 `campus.json`、全部覆盖层、`outskirts.json` 和烘焙器代码一致；不一致就失败，并提示重烘；<br>- **标记与实体**：锚点标记与 `design:anchor:*` 实体一一对应，位置相差 ≤ 0.5 m；地标、设施标记与所引用实体的登记点相差 ≤ 0.5 m；室内门口标记与 WLD-INTERIORS开放清单里的门中心相差 ≤ 0.5 m；<br>- **观景点**：引用的 ID 在 `waypoints.json` 里都存在，标记位置等于点位 `Fly` 的 (x, z)；<br>- **落点**：每个可传送标记的 `Fly` 位姿处是空气，`Stand` 落在可行走遮罩上（真实落地归 G15）；<br>- **底图与几何**：在全部建筑轮廓上抽 2000 个点，SDF 的零值线与世界模型轮廓相差 ≤ 0.5 m；地标线描的外框与轮廓相差 ≤ 0.5 m；<br>- **区域与标注**：可行走遮罩里每一格都查得到区域名；每条标注都有中英文；每个缩放档的标注不互相重叠，也不压在可传送标记上；<br>- **数量**：锚点 12–14 个，相邻锚点不超过 500 m（只报警）；自定义标记的上限常量是 50。 | 集中测试 |
| G19 路径与连通 | 按 WLD-INTERIORS §19。G19a–G19e 是纯 .NET，任何工作树都能跑：<br>- **G19a 图连通**：导航图的强连通分量（`OneWay` 按有向边）里，除 `Locked` 外所有节点都在同一个分量，并包含 R1 起点、全部点位的 `Stand`、全部开放入口、楼梯和电梯节点；既能走到也能走回，杜绝"进得去出不来"；<br>- **G19b 封口与可达**：每层 0.25 m 栅格按圆柱半径 0.3 m 腐蚀后，从开放入口出发的可走连通块完全落在声明的开放范围里；只到得了 WLD-INTERIORS §3 的三处高处，而且只经楼梯或电梯；面积 ≥ 4 m² 却回不到起点的可站区域为零（泳池除外）；可走格上方净高 ≥ 2.1 m；<br>- **G19c 门对位与名单**：开放清单 = 注册的提供者 = 门开着的楼；开放入口都登记了门口灯、地垫和开放牌，入口字段（门中心、朝向、门外落点、净宽）齐全，门外落点离门中心 2.5 m ± 0.05 m、落在可走面上；关着的楼的入口全是关门、表面类型 `DoorClosed`；开放门洞净宽 ≥ 1.0 m；门外节点离路网 ≤ 15 m，否则有登记的门前支路；圆柱从门外扫到门内能通过（地图标记与入口的对位归 G18）。**自动门**（WLD-INTERIORS §4.1）：登记为 `DoorLeafDecl` 的门两侧的区域都在开放范围里，锁着的门至少一侧不在；每扇门在四种世界状态下的静止状态相同；<br>- **G19d 楼梯**：每个开放楼层都能经楼梯到达；梯段坡度 ≤ 40°；斜面顶边与平台高差 ≤ 1 cm、底边埋深 ≥ 0.2 m；平台深 ≥ 1.2 m；净高 ≥ 2.0 m；梯井 ≤ 0.2 m 的有整墙，> 0.2 m 的有 `SlopedWallCollider`；圆柱上下各扫一遍都通过；主楼电梯往返一次；<br>- **G19e 可走性与时刻无关**：秋 17:30、夏 12:00、冬夜雪、夏夜雷雨四种状态下各烘一次碰撞，哈希完全一致（只算烘焙碰撞；自动门的运行时门体不计入，它的静止状态由 G19c 查）；<br>- **G19f 进楼零等待**：每处开放入口从 30 m 外按 1× 步行和 2× 疾跑各进一次门（每次集中测试轮换，抽样 20%）：门口软停触发 0 次；一层加载 ≤ 100 ms；"空壳帧"为 0；ENG-PERF §5 第 6 条每帧成立（WLD-INTERIORS §6.4）。**自动门**：路线上每扇自动门按 1× 和 2× 各冲一次，观者的圆柱碰撞体进入门洞时门已开过 75°，否则算"穿门扇"失败；观者在扫掠区里时门不回关；门关好之前门洞没有门扇碰撞，关好之后有；门动才有开关门声、门不动就没有（G21h，SYS-AUDIO §22）；<br>- **G19g 室内巡游**：巡游机器人用真实控制器（`IWalkInput`）把每处开放室内的开放边走一遍，从每个开放入口进门，走遍开放房间与楼梯（穿过沿途的自动门）再出来；卡住 > 3 s、穿模超过 2 cm、掉出、走出开放范围即失败；W6–W14 由它跑；<br>- **G19h 窗位与视觉**：隔墙不穿窗洞（留 0.1 m）；内皮窗洞与立面窗洞同一开间、同一尺寸；G8、G9 的室内位姿（见上）；ART-FIDELITY §7 的室内否决项（第 21–27 条）通过（WLD-INTERIORS §19）。 | 集中测试；G19c 随 G5 的注册检查在快检里查 |
| G20 存档 | 按 SYS-SAVE §14。纯 .NET 的容器编解码、校验、迁移链用 `dotnet test` 直接跑；需要引擎的标 `[RequireGodotRuntime]`，或由脚本启动导出版来跑：<br>- **往返**：每段用带种子的生成器造 1000 个随机 DTO，走一遍容器的写和读；引擎里随机操作 30 s，存档后立即在同一进程里读回再 `Capture` 一次。全部字段相等，double、float 逐位相等；<br>- **续跑等价**：两次启动都用 `--fixed-fps 60`。A：新游戏 → 1440× 前推 72 h → `--save-at` 存档 → 再跑 24 h；B：`--load` A 的存档 → 跑同样的帧数；另做 60× 和 1× 各 10 分钟、自动天气演化中途存档三组变体。世界（`WorldHours`、钟点、季节、流逝、`AnimTime`）和天气（选择、天气向量、前瞻队列）逐帧逐位一致；偶发事件按设计重新计时，不比；<br>- **迁移**：`game/kernel/save/tests/golden/` 下的每份金档都读一遍，每一步 `Migrate(v)` 单独测，迁移后再往返一次不变；<br>- **损坏 1000 例**：随机截断、翻位、空文件、错魔数、长度或代号不一致、Brotli 解压炸弹、超深 JSON、未知段与新版本段、超长数组、越界数值、类型错误、重复键、缩略图乱码等。不崩溃、没有异常漏出；每例 ≤ 100 ms，内存峰值比基线多不超过 128 MB；有旧代时退到旧代；`NewerBuild` 从不被覆盖；文件只挪进 `corrupt/`，从不删除；<br>- **退出时保存**（并进 G5 的窗口冒烟）：`--save=1 --save-dir=<临时目录>`、流逝暂停时截图 S1 → 发 WM_CLOSE → 用 `--slot=1 --title=0 --save=off` 重启截图 S2；另测鼠标被捕获时的 Alt+F4。2 s 内进程退出，新一代通过校验，S2 与 S1 一致（G7 的容差）；<br>- **写盘途中强杀**：每 2 s 强制保存一次，在写盘途中随机 `taskkill /F`，重复 100 次；每次重启后存档位都是 `Ok` 或 `RecoveredOlderGen`，回退不超过 1 代；<br>- **保存耗时**：F1 路线循环 10 分钟，每 10 s 强制保存一次，含 3 次带缩略图的保存：快照主线程 p99 ≤ 0.3 ms；缩略图那一帧 ≤ 5 ms（1080p）；从关闭请求到进程结束 ≤ 1.5 s；第 2 代 GC 0 次；保存不增加管线编译计数（G16）；<br>- **安全**：G4 的静态规则（SYS-SAVE §9）；运行时把恶意 `.tres` 改名成 `.sav` 读一次，再读一份恶意的 `settings.cfg`：读前读后 `ObjectCount` 不变，日志里没有被执行的输出，两者都按"损坏"或"退回默认值"处理；<br>- **锁与开关**：同时开两个进程，第二个 `Enabled = false`、一个文件都不写；`--save=off` 下 `user://saves` 不被创建、不被修改。<br>G20 的测试由 WP-50 交付，WP-27 接进入口。 | 核心专项：改动 `game/kernel/save`、`Campus.Contracts.Save` 或带 `[SaveSection]` 的代码时，跑往返、迁移、损坏、退出时保存；集中测试全量（再加续跑等价、强杀 100 次、保存耗时） |
| G21 声音 | 按 SYS-AUDIO §22（子项的方法与判据以 SYS-AUDIO §22 的表为准，判据阈值在 P0 标定）。声音门禁显式给 `--audio=on`：<br>- **G21a 素材入库**：许可、电平、格式、循环、人声、重复文件（`pipeline/audio` 的检查脚本加 `dotnet test` 读 `licenses.json`）；<br>- **G21b 混音响度**：整体与分场景按 SYS-AUDIO §3.3 的表；限幅前不削波（`--audio-limiter=0` 再录一遍）；<br>- **G21c 加速不爆发**：`--audio-log=1` 跑六档流逝 × {晴、雨、雪} 和三条体验脚本：任意 1 s ≤ 3 个、任意 5 s ≤ 7 个一次性声；60× 及以上钟声、广播、交班音为 0；<br>- **G21d 转场不爆音**：跳转、传送、换图、介质切换、换天气、模式切换、电梯换位、设置里拖音量和静音，没有超阈值的瞬态（咔哒检测器、`--audio-probe` 正弦探针）；穿门时室内外过渡平滑；与 G13 共用录像；<br>- **G21e 设置有效**：五组音量与各开关；Master 置 0 后 WAV 在 400 ms 后全零；`--audio=off` 时没有播放器；`audio.cfg` 里的恶意文本退回默认值；<br>- **G21f 确定性**：同一组参数用 Movie Maker 录两次，WAV 逐字节相同；<br>- **G21g 预算**：真实声部 ≤ 72（硬上限 96）；在播 3D ≤ 32（硬上限 48），开 `AreaMask` 的 ≤ 12；主线程平均 ≤ 0.35 ms、p95 ≤ 0.5 ms；混音线程 ≤ 一个核的 10%；常驻 ≤ 96 MB；导出的音频 ≤ 165 MB（与 G10、G6 同一套运行）；<br>- **G21h 覆盖与"对得上"**：纯 .NET 枚举全部组合，任何组合都不空、室外不出现数字静音；集中测试时听评"假不假""和画面对不对得上"各 ≥ 3/5（英雄帧 ≥ 4/5）；集中测试时抽 20 段 × 60 s 跑语音识别，识别出连续 ≥ 2 个有效汉字即不合格；<br>- **G21i 观者声**：不连续重复、覆盖率、不削波、与流逝无关（1×、60×、1440× 的事件日志相同）、声画同源、CPU 与着色器的地面覆盖一致率 ≥ 99.5%；<br>- **G21j 空间声学"窄处更明显"**：`--gate=acoustics` 在登记的参照点播放 8 步金样脚步：走廊比广场响 ≥ 3 dB、C50 低 ≥ 5 dB、尾音长 ≥ 15 dB。 | 集中测试；G21a 的素材登记检查在改动 `game/assets/audio` 时随快检跑 |

统一入口：`scripts\gate.ps1 -Wp WP-xx`（PowerShell）。由 WP-27 实现，WP-00 先放骨架。默认跑快检，改动碰到核心体验时加跑核心专项；`-Smoke` 只跑主线冒烟；`-Full` 跑集中测试的全量；`-Only G7,G9` 只跑指定门禁。

- 入口先检查引擎：路径取环境变量 `UESTC_GODOT`（默认值见 §1）；`--version` 的输出必须与 `tools/godot-version.txt` 一致，否则直接失败。
- 要显卡的门禁拿 GPU 锁排队，同时最多 2 个工作包在跑截图（ENG-ARCH §6）。
- G12 和 G13 中时间、季节与天气的部分由 WP-41（`scripts/gates/matrix`）实现；G13 的跳转部分、G14、G15、G16 由 WP-27 实现；G17 和"预设轮换"脚本由 WP-27、WP-41 实现（SYS-GRAPHICS §7）；G2 的天气过程测试由 WP-45a 交付（SYS-WEATHER §10），画面设置的测试由 WP-32a 交付；WP-01、WP-32、WP-35、WP-36、WP-42–44、WP-45 提供测试接口。全部接进同一个入口。
- G18 的检查器由 WP-48c 交付、WP-27 定跑法（SYS-ATLAS §13）；G19 和 G8、G9、G15 的室内扩展、室内巡游机器人由 WP-27 实现（WLD-INTERIORS §18）；G20 的测试由 WP-50 交付（SYS-SAVE §14）；G21 的检查由 WP-53 各子包交付，录音与听评的编排归 WP-27、WP-41（SYS-AUDIO §21）；G16 的同图重载驱动由 WP-46 在 `game/kernel/map` 里实现（SYS-MAPS §13）；过场文件的检查（SYS-TITLE §8.4）由 WP-49a 交付，G2、G7、G13、G14、G16 的开始页与过场增项由 WP-27、WP-41 实现（SYS-TITLE §14）。WP-27 都接进同一个入口。

## 3 一票否决

满足以下任意一条，直接判不通过：

1. 出现 UI-NAV、UI-VISUAL、SYS-ATLAS、SYS-TITLE 登记的界面与署名以外的任何叠加：文字、图标、按钮、加载提示或操作提示，包括引擎启动画面上的标志（SYS-VIEW §3）；**3D 画面里**出现地点标签、选中描边、世界空间标记、光柱、路线线条或追踪标记，或者锚点实体发光、做传送特效（SYS-ATLAS §1）；`--ui=0` 下出现署名以外的任何叠加（小地图、开始页、放映层也不得出现）。开始页、存档页、放映层、过场字幕属于 UI-VISUAL、SYS-TITLE 登记的界面；扉页是三维静帧，不算加载提示；扉页下方首次运行的一行进度（ENG-ARCH §4）也是 SYS-TITLE 登记的界面，烘完就淡出。
2. 画面里出现人、人影，或行驶中的车辆（过场里也不出现人，SYS-TITLE）；行走模式下画出观者的手、脚、影子或脚印（SYS-WALK §1）；展陈里出现人物肖像（WLD-INTERIORS §11）。声音的对应项见第 24 条。人物的几项属于观光规则集，判据取自 `sightseeing` 规则集的内容规则 `Characters = false`（ENG-ROADMAP §6 第 1 条）。
3. 出现日本的宗教或地标符号、日文文字或传统和式建筑构件（ART-STYLE §9.1 第 1、5、9 条）；或者 ART-STYLE §9.1 放开的日式日常物件超出了它给的地点、数量和文字限定（ART-AMBIENCE、MAP-QSH-ADDITIONS §2.16）。
4. 使用照片纹理，或出现灰黑色的阴影。
5. 镜头原地转一圈时，某个方位的远景是空的；看到天空穿帮、黑洞，或里外翻转的面；剖切面闪烁；外围露出未建模的背面，或者截面线以外露出地面和空洞（WLD-OUTSKIRTS）。
6. 偶发事件超过一种，或出现快速、突兀的动作。观者触发的过渡（改时间、换季、换天气、点位跳转、传送、模式切换、回位、应用画面设置、进场过场、返回标题、切图）不算偶发事件，也不算自动运镜，但时长和曲线必须符合 SYS-TIME §1、SYS-TIME §5，UI-NAV §7，SYS-WALK §5、SYS-WALK §9，SYS-GRAPHICS §4，SYS-ATLAS §7，SYS-TITLE §3–§5，SYS-MAPS §4。闪电是雷雨的持续氛围、彩虹是初晴的天象状态，都不算偶发事件（SYS-WEATHER §5）。偶发事件的数量属于观光规则集，允许的事件取自 `sightseeing` 规则集的 `Incidents`（ENG-ROADMAP §6 第 1、5 条）。
7. 时间广场上有喷泉（MAP-QSH-CORE §2.5）。春季的樱花（SYS-TIME §4）、夏季压在西岭雪山剪影上的日落（SYS-TIME §2）都是设计，不在此列。
8. 改动了 OSM 原始数据；或者覆盖层的调整超出容差却没写理由。
9. 触发 ART-FIDELITY §7 一票否决清单的任意一条，含第 19–27 条：影发脏、墙面发脏（ENG-ARCH、ENG-BUDGET，ART-STYLE §3），以及开放室内的空房间、墙体穿帮、楼内穿帮、楼梯对不上、门洞过窄、空门厅、自动门的门扇扫过镜头或观者（21–27，WLD-INTERIORS §19）。清单里属于底线的条目在任何位置、任何距离都判；与距离、密度有关的条目，距离按所在精度区的判定距离换算（ART-FIDELITY §0.1），离得比判定距离更近时只判底线。
10. 违反 ENG-BUDGET §7 的任意一条硬规则，或者本包关掉前后的差值超出额度。
11. **时间、季节、天气或画面设置硬切**：过渡期间超出 G13 的 ΔE 上限，又不在雾幕遮罩的区间里；或者影子在某一帧突然换了方向（SYS-TIME §2"主光交接"）；或者自动调节画质的某一级降档没有淡化（SYS-GRAPHICS §5）。
12. **夜间大块死黑**：太阳高度低于 −6° 时，画面里有任何一个以景物为主（天空像素不到一半）的 64×64 像素格，平均 L* 低于 12；或者夜间的暗部没有落在蓝紫色相里（SYS-TIME §3.4、SYS-TIME §8）。雨夜、雾夜同样适用（SYS-WEATHER §4"夜雨不死黑"）。天空不参与这一条，地下、水下、雾海这些介质呈现状态（SYS-VIEW §5）也不参与。64×64 的格子大小沿用 G9 的裸面检测，可以在 P0 按截图校准。
13. **季节残留**：叶量为 0 的树上还有悬空的叶片或插片（应已换成枝条笔触，SYS-TIME §4）；转场落定后，仍有上一季的粒子、地面覆盖、植被色或积雪；草与雪、花与叶直接混色（SYS-TIME §5）。检测时间点：换季转场结束、地面覆盖生长到位之后（SYS-TIME §5 的表）。
14. **行走**（SYS-WALK §15、WLD-INTERIORS §19）：行走模式下镜头进入关着的楼、封口以外、楼体实心部分、未登记的屋顶或地下层；走进一栋显示空壳的楼；进得去出不来（G19a 不通过）；掉出世界；卡死超过 3 秒；能走上 WLD-INTERIORS §3 登记以外的楼顶；在 SYS-WALK §7 允许的三处以外，被没有任何视觉提示的隐形墙挡住。
15. **天气残留**（SYS-WEATHER §9）：雨停后仍有雨丝；换季替换后仍是上一种相态（例如冬季还在下雨）。检测时间点：天气过渡或换季转场结束之后。
16. **闪电硬切或全屏闪白**（SYS-WEATHER §5）：闪光帧超出 G13 的闪电判据，或者整屏翻白、画出落地闪电。
17. **未验证的转移边**（SYS-WEATHER §6、SYS-WEATHER §9）：自动天气走了动态格里没有验过的"转移边 × 光段 × 季节"，或者在晨昏带里发生了自动转移。
18. **风格底线缺失**（SYS-GRAPHICS §2）：任何预设、任何手动值、任何自动调节级别下，描线、分阶明暗、按材质的影色、柔光（高光晕开）、高光肩部、平涂贴图、窗灯与路灯光斑、实时阴影、西湖倒影之中有任何一项消失；或者实时阴影、西湖倒影低于最差值。
19. **切图残留**（SYS-MAPS）：切图或同图重载之后，残留旧图的东西：实例、碰撞 body、天气状态、安全点。
20. **丢档或读档后不一致**（SYS-SAVE §5.3）：正常退出后再进同一存档，状态不是退出时的落定状态；或者读档后的世界、天气、视角与存进去的落定状态不一致。
21. **读档执行了内容**（SYS-SAVE §9）：读档时执行了存档里的任何代码，或创建了存档内容指定的对象。
22. **无法回退**（SYS-SAVE §1、SYS-SAVE §7）：存档被覆盖或删除，导致回不到上一代。
23. **新档被覆盖**（SYS-SAVE §7）：来自更新版本的存档被旧构建覆盖。
24. **声音**（SYS-AUDIO §22）：听得清的人声；可辨认的近处车辆开过；加速时的一次性声爆发；室外的数字静音；循环接缝的咔哒；天气残留的声音（雨停后还有雨落声）；声音硬切或爆音（直接切静音，或在有声的槽上改参数造成的瞬态）。人声一项属于观光规则集，判据取自 `sightseeing` 规则集的内容规则 `ClearVoices = false`（ENG-ROADMAP §6 第 1 条）。

## 4 全局清单（收尾阶段由主代理逐项检查）

- [ ] CORE-INDEX 的全局不变量 INV-1 至 INV-12 全部成立。
- [ ] 风格符合 ART-STYLE，造型符合 ART-MODELING；从 V1、V4、V7 看，金样风格一致，四季各看一遍。
- [ ] 视觉精度（ART-FIDELITY；基准是精致预设、1920×1080 原生，SYS-GRAPHICS §10）：§1 的所有位置和全部巡检点、随机点都满足底线 B1–B13（ART-FIDELITY §2.0）；各区巡检的分数按精度区达标（K1、K2 ≥ 20/25，K3 ≥ 17/25，K4、K5 每项 ≥ 3），没有任何一项低于 3 分；外围按最近可达距离判（WLD-OUTSKIRTS §4.1）；历次集中测试轮换到的组合也满足这条。
- [ ] 设计增补全部落实（MAP-QSH-ADDITIONS §7），并且没有踩到"不建议增补"的项（MAP-QSH-ADDITIONS §5）；夜间灯光符合 MAP-QSH-ADDITIONS §8。翻新清单（MAP-QSH-RENOVATION）全部落实，翻新楼按 ART-RENOVATION 验收，全校立面和地面没有做旧痕迹（D-174）。
- [ ] ART-SKYLIGHT §8 的冷暖对照全部成立；太阳和月亮按美术轨迹运行（SYS-TIME §2），秋 17:30 的太阳正好是 5.5°、241°。
- [ ] 大箱庭完整：外围 O0–O3、截面、雾海和 360° 远景（WLD-DIORAMA、WLD-OUTSKIRTS）；校界的缓停与提示、旁观的可达空间按 WLD-OUTSKIRTS §7–§9 工作。
- [ ] 各区（MAP-QSH-CORE 至 MAP-QSH-CENTER-EAST）、植被（WLD-VEGETATION）、水（WLD-WATER）、动物与家具（WLD-LIFE）、动态（SYS-MOTION）的验收项全部通过。
- [ ] 两种观察模式（SYS-VIEW、SYS-WALK）和四种介质的呈现符合 SYS-VIEW。
- [ ] 三档 PC 设备（独显高、独显中、最低配置）在各自推荐的预设（极致、精致、均衡，SYS-GRAPHICS §1）下都满足 ENG-BUDGET §2 的帧率目标，F1 没有卡顿超标；最低配置没有真机时按 G11 跳过并在回报里说明。
- [ ] **导出版**：G6 的安装目录不超过 0.9 GB（音频 ≤ 165 MB）、分发压缩包不超过 0.7 GB，`--bake-verify` 通过；在一台没有装 .NET SDK 的 Windows 机器上，解压后直接能运行（首次运行在扉页期间本机生成起始视角周围的格子和底图，ENG-ARCH §4）；窗口标题、图标、许可页（含音频署名、外围数据的 ODbL 声明）符合 SYS-VIEW §6。
- [ ] **管线编译**：G16 通过，标题页转为实时（或雾幕淡出）之后没有卡顿式的着色器编译，界面也没有首现卡顿；同图重载 3 次满足 SYS-MAPS §13。
- [ ] **时季矩阵**：最近一次集中测试的联表（G12）里 48 格都不低于 3 分；"前推 24 小时"和"四季轮换"两条脚本通过 G13。
- [ ] **天气**：SYS-WEATHER §9 的静态格、动态格都已验过；最近一次"季 × 天气"联表和每个天气格的代表帧都不低于 3 分；"天气巡回"通过 G16；新游戏在任何天气种子下都是秋 17:30 晴（SYS-WEATHER §6.6、SYS-SAVE §5.5）。
- [ ] **画面设置**：G17 通过；四个预设的风格底线都在（SYS-GRAPHICS §2）；设置页的"应用"、15 秒确认和自动调节按 SYS-GRAPHICS §4–§5 工作。
- [ ] **英雄帧**：SYS-TIME §3.5 的英雄帧（P00、P05、P07、P09、P10、P11、P12 的推荐状态，加 SYS-WEATHER §2 增补的天气英雄帧和第 12 张"秋 17:30 太阳雨"）都达到 4 分以上；集中测试的流畅预设复评不低于 3 分（SYS-GRAPHICS §10）。
- [ ] **界面**：G14（按 UI-VISUAL §13）通过；画面上只有 UI-NAV、UI-VISUAL、SYS-ATLAS、SYS-TITLE 登记的组件（含天气格与预报行、设置卡的"画面"页、小地图）；晶白、深屏两套主题都验过，交班不塌对比度，常驻小部件默认固定深屏；窗口图标是八角晶片（UI-VISUAL §6.3）；首次进入的一行引导只出现一次（UI-NAV §4.7）。
- [ ] **行走与室内**：G15、G19 通过；W1–W14 全程没有卡点，每 30–50 m 有一拍（SYS-WALK §10）；15 处开放室内都能一眼看出能进（WLD-INTERIORS §4），关着的楼从门玻璃和窗看进去都不是空盒子。
- [ ] **跳转**：F3 的 552 次跳转全部达标；每种打断方式都不跳变（UI-NAV §10）。
- [ ] **地图与传送**：G18 通过；F4 的 300 次传送全部达标；13 个锚点和全部室内门口在行走模式下都能落地（G15）；3D 画面里没有任何地图元素（G14 的"登记"一项）。
- [ ] **开始页与过场**：G2、G7、G13、G14、G16 的开始页与过场增项通过；新游戏在任何天气种子和画面预设下都在秋 17:30、晴、P00 交还；"过场体验"脚本通过（SYS-TITLE §11.3）。
- [ ] **存档**：G20 通过；退出后再进状态一致；3 个存档位、"继续"、新游戏覆盖确认都按 SYS-SAVE §1 工作；新游戏在任何种子下都从落金初灯开始。
- [ ] **声音**：G21 通过；最近一次集中测试的听评里"假不假""和画面对不对得上"两项不低于 3 分，英雄声景不低于 4 分；任何组合都不空，没有清晰人声。
- [ ] **地标雾豁免**：浓雾下主要地标（八角穹顶、宣邦楼钟楼）仍可辨认（SYS-WEATHER §9）；钟面指针跟随界面上的时间。

## 5 回报格式

每个子代理完成后，按下面的格式回报：

```
工作包：WP-18 南区
改动文件：game/gen/zones/south/**、game/data/maps/qingshuihe/overlay/south.d/
已完成条目：MAP-QSH-SOUTH §1.*、§2.1–§2.9、§4.*、§5.*、§6.*、§7.*；MAP-QSH-ADDITIONS §7 中南区的增补共 11 项
未完成条目及原因：MAP-QSH-SOUTH §2.6 主楼喷泉（可选），未做
覆盖层：reshape 2 处（均为轻微调整），add 23 处（design:east-lake-*），详见差异报告
需要其他包配合：WP-23 种植主楼两侧的银杏林，范围 x −470…−150、z 430…620
预算：三角形 7.4% / 8%，绘制调用 5.1% / 6%；关掉本包前后的帧时差 1.9 ms
门禁：scripts\gate.ps1 -Wp WP-18；快检通过；核心专项 G15 前四项通过（本包提供碰撞体）；其余门禁留到集中测试
天气与画质：本区"天气要点"已落实（避雨处 4 处、积水点 6 处）；天气增量（SYS-WEATHER §10）不涉及；负责的画面单项（SYS-GRAPHICS §7）无
存档：不涉及
声音：本区登记的发声器 9 个、插座 3 个（SYS-AUDIO §4.2）；Foot 已填；G21 不涉及；音频主线程与声部不涉及
环境：引擎 4.7.2.stable.mono.official.ed1daf0bf；截图显卡 RTX 4080（G7 断言通过）
截图：output/render-review/WP-18-*.png
契约修改申请：无
```

- 截图统一放在 `output/render-review/`（不进 git），命名为"包-视角.png"或"包-Qn.png"（Qn 是巡检点编号 Q1–Q8，与点位 P00–P23 区分）。非默认状态的截图在后面加季节和钟点，例如 `WP-18-V6-winter-2100.png`；天气不是晴、预设不是精致时再依次加天气和预设，例如 `WP-18-V6-winter-2100-snow.png`、`WP-18-V1-autumn-1730-clear-smooth.png`（名字与命令行取值相同）。
- "门禁"一行先写运行的命令，再写结果。
- "时季矩阵"一行写 G12 抽样的结果、G13 的 ΔE 最大值，以及本包涉及的英雄帧得分；不涉及时写"不涉及"。
- "天气与画质"一行写本包的天气增量（SYS-WEATHER §10）和负责的画面单项（SYS-GRAPHICS §7）的落实情况，以及 G17 的结果；不涉及时写"不涉及"。
- "存档"一行（SYS-SAVE §2）：写本包的存档段是否已登记、G20 是否通过，以及段的版本号和有没有迁移函数；没有存档段时写"不涉及"。
- "声音"一行（SYS-AUDIO §22）：写本包登记的发声器与插座、`Foot` 的填写情况、G21 相关子项的结果、音频主线程与声部；不涉及时写"不涉及"。
- 报告里引用条目时只写编号，不复述内容。

## 6 待验证（P0）

以下做法或数值在验证前按"初值"执行，验证后改写原位并删去本条。

| 待验证 | 位置 | 验证方法或负责包 | 验证前的初值 |
| --- | --- | --- | --- |
| 截图就绪判据：管线编译计数不再增长的连续帧数 | §1 截图方式第 4 条 | WP-27 在开发机上实测，看更短的窗口是否会漏掉晚到的编译 | 连续 30 帧 |
| 存图前额外等待的帧数（倒影视口、调色板、通道稳定） | §1 截图方式第 4 条 | WP-27 实测：逐帧比对，取像素差稳定之后的帧数 | 8 帧 |
| 同机同驱动的截图像素容差 | §1 截图方式第 5 条 | WP-27 连续截 10 次求像素差（ENG-ARCH §7） | —（标定后写入） |
| 流式屏障能否让录像逐帧复现 | §1 截图方式第 6 条 | WP-27 用同一组参数录两遍，逐帧比对 | 按可复现处理 |
| V9 的相机位置 | §1 视角表 V9 | WP-27 写 `review.json` 时按 WLD-OUTSKIRTS 的西侧截面线复算 | 约 (−1640, 775, 262) |
| 同图重载的热重载耗时 | G16 同图重载 | WP-46 实测（SYS-MAPS §13） | ≤ 5 s |
| 最差值配置下描线像素占比的下限 | G17 | WP-27 在四个预设的最差值配置下截图标定 | 不低于精致预设的 70% |
| G21 各子项的判据阈值 | G21 | WP-53 与 WP-27 按 SYS-AUDIO §22 的表标定 | SYS-AUDIO §22 表中的值 |
| 夜间死黑检测的格子大小 | §3 第 12 条 | WP-27 按截图校准 | 64×64 像素 |
| 导出版在没有装 .NET SDK 的 Windows 机器上解压即可运行 | §4"导出版" | 用 G6 的分发压缩包在一台干净的 Windows 机器上验证 | 按可直接运行处理 |
| 首次运行到转为实时的时间 | G10 冷缓存子项 | WP-27、WP-02 在开发机的空缓存目录上实测 | ≤ 25 s（开发机） |
| 沿 F1 行走一圈的碰撞冻结累计与祖先格兜底时长 | G10 冷缓存子项 | WP-27、WP-02 在空缓存目录、`--bake-bg=1` 下实测 | 碰撞冻结累计 ≤ 2 s；祖先格兜底时长只记录 |
| 各类产物的单件生成耗时，以及全图预烘总时长 | G10 冷缓存子项 | WP-02 在开发机单线程实测 | L0 一格 ≤ 150 ms，碰撞一格 ≤ 50 ms，室内一层 ≤ 100 ms；开始页烘满全图 ≤ 4 min |
| 后台烘焙期间第 2 代 GC 的暂停 | G10 | WP-02、WP-27 在空缓存目录、`--bake-bg=1` 下实测 | 暂停上限在 P0 标定；GC 判据仍在缓存已烘满时测 |
| 安装目录与分发压缩包的实测体积 | G6 | 主代理在 P0 导出一次实测（ENG-BUDGET §8） | 安装目录 ≤ 0.9 GB，分发压缩包 ≤ 0.7 GB |
| 裸面检测的距离下限与"有变化"的口径 | G9 | WP-27 用贴墙与中景截图校准：同一组金样里的光板墙要判成裸面，留白墙不判 | 离镜头 5–60 m；格内有描线、第二种材质，或 L* 极差不小于 3，都算有变化 |
| 随机点在各精度区的名额 | G9 | WP-27 看第一次集中测试的点位分布和问题检出 | K1、K2 不少于 10 个，K3 约 6 个，K4 约 4 个 |
| 各精度区的人工评分线 | G9、§4 | WP-27 看第一次集中测试的分数分布 | K1、K2 总分 ≥ 20；K3 总分 ≥ 17；K4、K5 每项 ≥ 3 |
| 精度区的判定距离 | §3 第 9 条、G9 | 随 ART-FIDELITY 的金样一起校准（ART-FIDELITY §11）；WP-27 | K1、K2 0.5 m；K3 5 m；K4 屋顶 40 m、背立面 15 m，只有飞行才到的位置取 max(d_walk, 15 m) |
