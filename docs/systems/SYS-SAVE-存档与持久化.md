---
id: SYS-SAVE
title: "存档与持久化：存档模型、存什么、不存什么"
category: systems
summary: "存档位与世界线、各存档段存什么不存什么；保存读档与格式安全见子文档"
keywords: [存档, 存档位, 世界线, 存档段, 单实例锁, ISaveService, ISaveSection, SaveSectionAttribute, RestorePhase]
owners: [WP-50]
contributors: [WP-00, WP-01, WP-02, WP-03, WP-06, WP-23, WP-25, WP-26, WP-27, WP-32, WP-34, WP-35, WP-36, WP-39, WP-42, WP-43, WP-45a, WP-46, WP-48c, WP-49, WP-53a, WP-54a, WP-55]
depends: [ENG-ARCH, SYS-TIME, SYS-WEATHER, SYS-MAPS]
related: [CORE-INDEX, SYS-MOTION, SYS-VIEW, ENG-GATES, ENG-BUDGET, ENG-PERF, UI-NAV, SYS-WALK, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP, CORE-CONVENTIONS]
contracts: [ISaveService, ISaveSection, SaveSectionAttribute, SlotId, SlotKind, SaveScope, SlotHealth, SaveMeta, SlotInfo, RestorePhase]
data: ["user://saves/"]
gates: [G5, G20]
---

# SYS-SAVE · 存档与持久化：存档模型、存什么、不存什么

> **范围**：本文规定存档服务 `ISaveService` 的存档位与世界线、各存档段存什么与不存什么。保存时机、读档流程与格式、损坏处理、安全在子文档。
> **不在本文**：文件布局、DTO、写盘步骤（`game/kernel/save/`）；契约签名（`game/contracts/Save/`）；帧顺序（ENG-ARCH §3）；命令行与 G20（ENG-GATES）；性能额度（ENG-BUDGET §3）；各段的字段（各包）；开始页与进入游戏（SYS-TITLE）；换图（SYS-MAPS）；天气过程（SYS-WEATHER）。
> **查阅**：先读 §0；存档位与世界线看 §1；各段存什么看 §2；保存时机、读档与一致性看 SYS-SAVE-FLOW；格式、损坏与安全看 SYS-SAVE-FORMAT

## 0 结论

1. **存档是 Kernel 服务 `ISaveService`，不加 Autoload**。各系统用 `[SaveSection]` 声明一段，自动发现。存档包只管容器、版本、保存时机、可靠性；各段存什么由各包负责（§2）。
2. **3 个存档位 `s1`–`s3`**，各装一条世界线，进入后持续自动保存；"继续"取最近保存的存档位（§1）。
3. **子文档**：
   - SYS-SAVE-FLOW「保存时机与读档流程」：保存时机、安全时刻、退出、读档顺序、世界实例、状态一致、新游戏、校验修复、关着游戏的现实时间不计。写 `Capture`、`Restore` 或接保存、读档编排时读它。
   - SYS-SAVE-FORMAT「格式、损坏处理与安全」：容器与序列化、段版本、坏档退回、设置文件（`settings.json`）、读档输入的安全。加或改段的字段、读写设置文件时读它。

### 0.1 接入要点

- **有要跨读档保留的状态**：在本包的 `[KernelService]` 或 `[KernelNode]` 上实现 `ISaveSection<T>` 并标 `[SaveSection]`；不自己读写 `user://`，不加 Autoload（§2）
- **做传送、跳转、换图的到达或读档**：到达不触发保存（D-241），保存时机只有 SYS-SAVE-FLOW §1.1 那几种
- **决定一段存什么**：只存读档后重建不出来的落定状态；音频状态、打开的面板、偶发事件计时、画质统计、烘焙缓存、玩家身体与动物的瞬态都不进存档（§2、SYS-SAVE-FLOW §2.3）

## 1 存档模型

- **存档位**：3 个，ID 是 `s1`、`s2`、`s3`。一个存档位装一条**世界线**：从一次新游戏开始，到它被另一次新游戏覆盖为止。进入游戏后这个存档位就是**当前存档位**（`ISaveService.Active`），自动保存都写到这里；观光期界面上没有"保存"按钮。
- **继续**：在有可读代的存档位里，取最新可读一代保存时间最晚的那个，相同时按 `s1`、`s2`、`s3` 取。没有就不显示"继续"，开始页焦点落在"新游戏"（SYS-TITLE）。
- **新游戏**：有空位用第一个空位；三个都满时由开始页的存档页选一个覆盖并二次确认。旧世界线要等新世界线的第一代校验通过后才删。
- **代号与世界线号**：每个存档位内代号严格递增；世界线号等于这条世界线第一代的代号；回退只在同一条世界线内进行（SYS-SAVE-FORMAT §2）。
- **保留 3 代**：新一代校验通过后删掉同一世界线里更早的代，其他世界线的旧代一并删掉。存档位里永远至少有一代可读。
- **单实例锁**：进程整个生命期独占一把锁，崩溃后由系统自动释放。打不开锁（另一个窗口在运行）时本进程只读存档，不写、不挪文件，并提示一次"另一个窗口正在运行 · 本窗口不保存"。
- 冷启动时在扉页后面读入"继续"的存档位并预览恢复，开始页 T0 显示的就是这个世界（SYS-SAVE-FLOW §2.1）；开始页和加载界面期间不自动保存。

## 2 存什么、不存什么

恢复分三个阶段：`World`（世界与天气）→ 换图 → `Content`（按图内容与进度）→ `Pose`（视角与行走）。换图要先知道全局时间和那张图的天气；视角与行走在换图之后落位。

| 段 | 范围 | 负责 | 阶段 | 存 |
| --- | --- | --- | --- | --- |
| `maps` | 全局 | WP-46（地图服务与多地图） | World | 当前地图；每张到过的图一条 `MapVisit`（最后位姿、模式） |
| `world` | 全局 | WP-34（世界状态与美术星历） | World | `WorldHours`、钟点 offset、季节、流逝倍率、`AnimTime`（过渡进行中存终点） |
| `weather` | 全局 | WP-45a（过程与剧本） | World | 天气种子；自动 / 手动与手动格、钉住与"保持晴"窗口、湿润积水余量（自动天气是纯函数，不存队列，SYS-WEATHER-PROCESS §2） |
| `progress` | 全局 | WP-36（点位与跳转） | Content | 到过的点位 |
| `view` | 全局 | WP-06（相机与输入） | Pose | 观察模式、旁观位姿、所在空间、速度档 |
| `walk` | 全局 | WP-42（行走控制器与模式切换） | Pose | 脚位、朝向、潜行、步速档、安全点环 |

- 按图的段键是 `map:<MapId>/<段 ID>`；主线只有一张图，暂无按图的段（平面地图全开放，不存标记与解锁，SYS-ATLAS）。视角只存当前一份，每张图的最后位姿在 `MapVisit`。
- 游玩时长、代号、世界线号、本局规则集 ID 存在 meta 里，不另设段；规则集的内容不进存档。
- **不存**（读档后重建）：音频状态；打开的面板、纯净模式；偶发事件的进度与计时；流式与画质的统计；玩家身体的姿态、湿度与平滑历史；泳态；动物的反应；骑姿与速度；开放室内房门与防火门的状态（存档时一律按关好记）。
- **游泳与骑行**：在水里或骑行中保存，`walk`、`view`、`maps` 与 meta 的位姿都指向同一个最近的岸上安全点。保存只记下这个点，不把人真的挪走。
- **共享单车**（SYS-WALK-BIKE §1）：车辆一段由 WP-55（共享单车运行与取还） 负责，按图记每辆可借车的停放归属；保存时借的车按归还记；旧档缺这一段时按基线初始化一次。
- **设置文件**不进存档，换存档位不变：只有一份 `user://settings.json`，界面、输入、画面、声音各占一节（SYS-SAVE-FORMAT §3）。`user://cache/bake/` 是本机烘焙缓存，不是存档（SYS-MAPS §1.2）。
