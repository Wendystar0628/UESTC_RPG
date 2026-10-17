---
id: SYS-SAVE
title: "存档与持久化：存档模型、存什么、格式与迁移、保存时机、加载与一致性、安全、契约与验收"
category: systems
summary: "规定存档位与世界线、存档段、文件格式与迁移、保存时机、读档流程与一致性、设置文件安全与 G20"
keywords: [存档, 保存, 读档, 自动保存, 存档位, 世界线, 存档段, 段版本, 存档迁移, 金档, 落定快照, 安全时刻, 续跑等价, 世界实例, 校验修复, 存档损坏, 单实例锁, 设置文件安全, 退出保存, ISaveService, SaveSectionAttribute, ISaveSection, SaveReason, LoadScope, RestorePhase, SaveMeta, IWorldState.Instance, FlushForQuit, CompleteResume, "user://saves/"]
owners: [WP-50]
contributors: [WP-00, WP-01, WP-02, WP-03, WP-06, WP-23, WP-25, WP-26, WP-27, WP-32, WP-34, WP-35, WP-36, WP-39, WP-42, WP-43, WP-45a, WP-46, WP-48c, WP-49, WP-53a, WP-54a, WP-55]
depends: [ENG-ARCH, SYS-TIME, SYS-WEATHER, SYS-MAPS]
related: [CORE-INDEX, SYS-MOTION, SYS-VIEW, ENG-GATES, ENG-BUDGET, ENG-PERF, UI-NAV, SYS-WALK, SYS-GRAPHICS, UI-VISUAL, SYS-ATLAS, SYS-TITLE, WLD-OUTSKIRTS, WLD-INTERIORS, SYS-AUDIO, ENG-ROADMAP, CORE-CONVENTIONS]
contracts: [ISaveService, ISaveSection, SaveSectionAttribute, SlotId, SlotKind, SaveScope, SaveReason, SlotHealth, SaveMeta, SlotInfo, RestorePhase, LoadScope, CaptureContext, RestoreContext, NewGameContext, SaveStatus, SaveResult, LoadStatus, LoadResult, SaveState, ReadOnlyReason]
data: ["user://saves/", "game/kernel/save/tests/golden/"]
gates: [G2, G4, G5, G10, G16, G20]
---

# SYS-SAVE · 存档与持久化：存档模型、存什么、格式与迁移、保存时机、加载与一致性、安全、契约与验收

> **范围**：本文规定存档服务 `ISaveService`：存档位与世界线、各存档段存什么、文件格式与版本迁移、保存时机与可靠性、读档流程与"状态一致"的定义、关游戏期间不计时、损坏与版本不兼容、设置类文件的安全读写、契约、帧顺序里的存档一步、命令行，以及 G20 门禁。
> **不在本文**：各段的字段由各自的包负责（例如平面地图段见 SYS-ATLAS §12）；开始页与继续过场的流程（SYS-TITLE）；换图（SYS-MAPS）；天气过程（SYS-WEATHER）；音频设置的内容（SYS-AUDIO）。
> **查阅**：先读 §0；各存档段存什么、恢复阶段与顺序看 §2；文件格式、DTO 约定与版本迁移看 §3；保存时机、安全时刻与退出流程看 §4；读档顺序与世界实例看 §5；设置类文件的安全读写看 §8；契约签名与一段的写法示例看 §10；G20 看 §14
> **关联**：[ENG-ARCH 工程架构](../engineering/ENG-ARCH-工程架构.md)、[SYS-TIME 时间季节](SYS-TIME-时间与季节.md)、[SYS-WEATHER 天气](SYS-WEATHER-天气.md)、[SYS-MAPS 多地图](SYS-MAPS-多地图.md)；[CORE-INDEX 总索引](../README.md)、[SYS-MOTION 动态与事件](SYS-MOTION-动态与事件.md)、[SYS-VIEW 观察模式与介质](SYS-VIEW-观察模式介质与开场.md)、[ENG-GATES 验收门禁](../engineering/ENG-GATES-验收与门禁.md)、[ENG-BUDGET 性能预算](../engineering/ENG-BUDGET-性能预算与硬规则.md)、[ENG-PERF 性能工程](../engineering/ENG-PERF-性能工程.md)、[UI-NAV 界面与跳转](../ui/UI-NAV-界面与操作说明.md)、[SYS-WALK 行走](SYS-WALK-行走模式.md)、[SYS-GRAPHICS 画面设置](SYS-GRAPHICS-画面设置.md)、[UI-VISUAL 界面视觉语言](../ui/UI-VISUAL-界面视觉语言.md)、[SYS-ATLAS 平面地图与传送](SYS-ATLAS-平面地图与传送.md)、[SYS-TITLE 开始页与过场](SYS-TITLE-开始页与进入过场.md)、[WLD-OUTSKIRTS 边界与外围](../world/WLD-OUTSKIRTS-边界与外围.md)、[WLD-INTERIORS 室内](../world/WLD-INTERIORS-室内.md)、[SYS-AUDIO 音频](SYS-AUDIO-音频.md)、[ENG-ROADMAP 扩展路线](../engineering/ENG-ROADMAP-扩展路线.md)、[CORE-CONVENTIONS 约定](../core/CORE-CONVENTIONS-约定.md)

**分工**：WP-50 存档与持久化，独占 `game/kernel/save`，P0 交付。WP-00、01、02、06、25、26、27、32、34、35、36、42、43、45、46、48、49、53、54a、55 各有增量（§15）。

## 0 结论

1. **存档做成 Kernel 服务 `ISaveService`（WP-50），不加 Autoload**。各系统在自己的服务或节点上用 `[SaveSection]` 声明一段，自动发现（ENG-ARCH §2）。存档包只管四件事：容器、版本、保存时机、可靠性。各段存什么由各包自己负责。
2. **3 个存档位 `s1`–`s3`**。每个存档位是一条世界线，进入后持续自动保存；"继续"取最近保存的存档位。手动槽 `m1`–`m9` 只在格式和契约里预留，观光期不出现（§1）。
3. **文件格式**：
   - 每次保存写一个新文件 `g<代号>.sav`，保留 3 代，不覆盖、不改名；
   - 文件依次是 64 字节的头（以魔数开头）、未压缩的 meta JSON、Brotli 压缩的分段 JSON、WebP 缩略图；
   - SHA-256 只用来发现损坏；
   - 序列化用 System.Text.Json 源生成。
   - **禁止**用 Godot Resource、ConfigFile、`StrToVar`、`BytesToVar*`、`GetVar` 存读档。原因：`.tres` 里嵌的 GDScript 会在加载时执行，ConfigFile 会按文件内容创建对象（§9）。
4. **保存时机**：
   - 退出时：`AutoAcceptQuit=false`，后台写完再 `Quit()`；
   - 每 5 分钟；
   - 关键动作落定后；
   - 窗口失焦时。

   定时、落定、失焦这几类请求两次之间至少隔 30 s，在安全时刻拍快照，最多顺延 60 s 后拍强制快照；退出、返回标题、新游戏交还控制不限频、立即执行（§4.1、§4.2）。Godot 在 Windows 上收不到关机消息，所以定时保存是唯一的兜底（§4）。
5. **一致性**：
   - 读档后的状态等于存档那一刻的"落定状态"；
   - 在安全时刻拍的存档，读档后给相同输入，世界和天气与没退出时逐帧一致（续跑等价，§5.3）；
   - 读档换一个世界实例，时间单调的要求收窄到"同一个世界实例内"；
   - 读档先按 meta 的 `ruleset` 定规则集，再恢复世界段与天气段，然后换图，最后落位；开始页不做最后一步，视角留作"继续"的终点（§5.1）。
6. **两点代价**：
   - 断电、强杀进程、系统关机时收不到退出信号，最多丢最近 5 分钟（定时保存的间隔）；
   - "状态一致"指世界的落定状态。正在进行的过渡、偶发事件、打开着的面板不续播：读档后过渡直接落到终点，界面回到界面态。做到"连过渡中途都续上"代价很高，收益很小，不做。
7. **共享单车**：首版读档一律恢复成步行，借的车视为已归还，不恢复骑姿；车辆的停放归属由 WP-55 的一段存，与 `view`、`walk`、`maps` 出自同一次落定（§2、§4.2）。

### 0.1 接入要点

- **有要跨读档保留的状态**：在本包的 `[KernelService]` 或 `[KernelNode]` 类上实现 `ISaveSection<T>` 并标 `[SaveSection]`，写明 Id、Owner、Version、Phase、Order，由注册器自动发现；不自己读写 `user://`，不加 Autoload（§2、§10）
- **决定一段存什么**：只存读档后重建不出来的落定状态；音频状态、打开的面板、偶发事件计时、流式与画质统计、`user://cache/bake/`、玩家身体的瞬态（湿度、姿态、平滑历史）、动物的反应状态都不进存档，读档后各自重建（§2、§5.3）
- **定义段的数据**：每段一个只读 `sealed record`，只放基础类型、`System.Numerics` 向量、契约里的枚举和 ID；本包自写源生成的 `JsonSerializerContext`，约定与 `ContractJson.Options` 相同；不放 `object`、`JsonElement`、Godot 类型，不用开放多态（§3.5）
- **段的字段有任何增减**（哪怕只加一个可选字段）：`[SaveSection]` 的 `Version` 加 1，并实现 `Migrate(fromVersion, JsonNode)` 迁到下一版；迁移是纯函数，在后台线程调用，不碰 Godot、不读服务状态（§3.6）
- **实现 `Capture` 与 `Restore`**：`Capture` 在主线程、任何时刻都给出落定值（过渡中取终点，空中、水里或骑行中取最近的岸上安全点），不做 I/O，全部段合计 p99 ≤ 0.3 ms；有过渡进行时让 `IsSettled` 返回 false；`Restore` 立即生效、不做过渡，核对数组长度与数值范围，每修一处调 `RestoreContext.Repair()`（§4.2、§5.6、§9）
- **存档里要引用别的数据**：只存 ID，不存文件路径；ID 不复用，改名不改身份；确需换 ID 时由负责那份数据的包提交本图目录的 `id-remap.json`，引用它的段各升一次 `v`（§3.6、§9）
- **关键动作落定、要触发一次保存**：调 `ISaveService.Request`，原因用 `Settled`、`MapChanged`、`FocusLost` 等，由存档服务合并、限频、等安全时刻，只在漫游态有效；`SaveNow` 只用于新游戏交还控制、门禁和预留的手动槽（§4.1、§10）
- **看到 `IWorldState.Instance` 变了**：做平滑、带回差或保留历史的系统（钟点弹簧、天气插值、界面主题回差、窗灯错开、植被近池、声音平滑量等）把内部状态直接设成新世界的值，不做过渡；`WorldHours` 的单调只在同一实例内比较（§5.2）
- **读写档案级设置文件**（`settings.cfg`、`input.cfg`、`graphics.cfg`、`audio.cfg`）：先查文本里有没有 `Object(` 等写法，再 `ConfigFile.Parse`，只接受 bool、int、float、String；写入走 `.tmp` 加 `File.Replace`（§8）
- **任何代码读写 `user://`**：不用 `ResourceLoader`、`ResourceSaver`；存档段的实现里的禁用写法见 §9、ENG-ARCH §2（G4）
- **界面要显示存档或保存结果**：只经 `ISaveService`（`Slots`、`State`、`Thumbnail`、`SlotsChanged`）读，不自己解析 `.sav`；失败原因用 `save.error.*` 文案键（§4.6、§10）
- **想用现实时间**：系统时钟只能在 `Campus.Kernel.Save` 里读、只写进 meta 的 `savedAtUnix`；世界状态不读它，读档也不补关着游戏的那段时间（§6）

## 1 存档模型

```
user://saves/
├─ .lock                单实例锁：进程整个生命期独占打开；进程崩溃后由系统自动释放
├─ s1/g000041.sav       存档位 1：同一世界线最近 3 代（例如 g000039–g000041），每代一个新文件
├─ s2/  s3/             存档位 2、3，结构相同
├─ m1/ … m9/            手动槽（预留，观光期不建）
└─ corrupt/             校验失败、或被新版本占用而挪开的文件：只挪不删
```

- **存档位**：
  - 3 个，ID 是 `s1`、`s2`、`s3`，文件夹名就是 ID。
  - 一个存档位装一条**世界线**：从一次新游戏开始，到它被另一次新游戏覆盖为止。
  - 进入游戏（继续、读档、新游戏）后，这个存档位就是**当前存档位**（`ISaveService.Active`），之后的自动保存都写到这里。观光期界面上没有"保存"按钮。
- **继续**：
  - 在健康状况为 `Ok` 或 `RecoveredOlderGen` 的存档位里，取最新可读一代 `SavedAtUnix` 最大的那个。
  - 相同时按 `s1`、`s2`、`s3` 的顺序取。系统时钟被往回调过时，排序可能不符合直觉，这种情况接受。
  - 一个都没有就不显示"继续"，开始页的焦点落在"新游戏"上（SYS-TITLE）。
- **新游戏**：
  - 有空存档位，就用第一个空位；三个都满时，由开始页的存档页选一个覆盖，并二次确认（SYS-TITLE、UI-VISUAL）。
  - 旧世界线等新世界线的第一代写好、并校验通过后才删。这期间断电，旧世界线仍可读。
- **代号**（gen）：
  - 每个存档位内严格递增，取现有最大代号 + 1；
  - 文件名写成 6 位，例如 `g000042.sav`；
  - 头里、meta 里和文件名里的代号必须一致。
- **世界线号**（`Line`）：等于这条世界线第一代的代号，在存档位内唯一，不需要另取随机数。回退只在同一条世界线内进行（§7）。
- **保留 3 代**：新一代写好并校验通过后，删掉同一世界线里更早的代，只留最近 3 代；其他世界线的旧代一并删掉。存档位里永远至少有一代可读。
- **手动槽（预留）**：
  - ID 是 `m1`–`m9`，文件格式相同，`Kind = Manual`；
  - 只在观者明确保存时写，每次都带缩略图，只留 1 代；
  - meta 里记 `Origin`（拍这份手动档时的当前存档位）。读手动档之后，自动保存写回 `Origin`。
- **单实例锁**：
  - 启动时以 `FileShare.None` 打开 `user://saves/.lock`，一直持有到进程结束；锁就是这个打开的句柄，不看文件是否存在，所以崩溃后不会留下死锁。
  - 打不开（另一个窗口在运行）时 `Enabled = false`：本进程只读存档，不写文件、不挪文件，并提示一次"另一个窗口正在运行 · 本窗口不保存"（UI-NAV §4.9）。
  - 只有锁的持有者能把文件挪进 `corrupt/`。别的进程读到正在写的那一代时，哈希对不上，只当它不可读，不挪动。
- **与开始页的关系**（SYS-TITLE）：
  - 冷启动时，扉页后面读入"继续"的存档位，按"预览"范围恢复（地图、世界、天气等，视角和行走留作终点），开始页 T0 显示的就是这个世界（§5.1）；
  - 开始页（Title）和进场过场（Entering）期间不自动保存，只有漫游（Playing）期间保存。

## 2 存什么、不存什么

**存档段**（`[SaveSection]` 声明。"阶段"和"顺序"决定恢复的先后：先按阶段，阶段内按顺序，§5.1）

恢复分三个阶段。换图要先知道全局时间和那张图的天气，所以世界段和天气段排在换图之前；视角与行走在换图之后落位，开始页预览时留作"继续"的终点（SYS-TITLE）。

| 段 | 范围 | 负责 | 阶段 | 顺序 | 存 | 不存（读档时重建） |
| --- | --- | --- | --- | --- | --- | --- |
| `maps` | 全局 | WP-46 | `World` | 0 | 当前地图 `MapId`（现在只有 `"qingshuihe"`）；每张到过的图一条 `MapVisit`（SYS-MAPS定义，SYS-MAPS §8）：最后位姿、观察模式、离开时的天气 epoch、离开时的 `WorldHours`。当前图的那一条在保存时按此刻的位姿和模式写，与 `view` 段一致 | `IMapContext`、场景、格子 |
| `world` | 全局 | WP-34 | `World` | 10 | `WorldHours`（存弹簧追随的目标 x\*，它只增不减）、钟点 offset、季节落定值与目标、流逝倍率与暂停前的倍率、`AnimTime` | 日月、ω、调色板、灯光通道、`Step12`、`Rev`；快进、换季转场进行中时存终点 |
| `weather` | 全局 | WP-45a | `World` | 20 | 本存档的天气基础种子（u32） | — |
| `map:<id>/weather` | 按图 | WP-45a | `World` | 21 | epoch、下一片段号与尝试号；过去 24 h 和前瞻 24 h 已生成的队列（不应期规则要用过去的那段）；自动 / 手动、手动格与强度；钉住窗口、"保持晴"窗口；湿润、积水、新雪、檐滴余量。每张到过的图各一段 | `WeatherNow`（由队列和时间求出）、闪电节拍；手动换天气或换季替换的过渡进行中时存终点 |
| `map:<id>/atlas` | 按图 | WP-48c | `Content` | 40 | SYS-ATLAS的 `AtlasSave`（SYS-ATLAS §11）：自定义标记（每图最多 50 个）、追踪目标、各标记的解锁状态、到过的锚点（观光规则集的策略是 `all-unlocked`，解锁表为空） | 底图、标记数据（`markers.json`）、筛选偏好（存在 `settings.cfg`，SYS-ATLAS） |
| `progress` | 全局 | WP-36 | `Content` | 50 | 到过的点位（P00 起，含室内新增点 P16–P23，WLD-INTERIORS） | — |
| `view` | 全局 | WP-06 | `Pose` | 60 | 观察模式、旁观位姿（位置、偏航、俯仰）、所在空间 `NavSpace`（楼的 FeatureId 与楼层，室外为空，WLD-INTERIORS）、16 档速度的当前档、切到行走前的飞行档 | 当前速度（读档时静止）、介质、视场角（来自设置）；跳转、传送、模式切换途中，存目的地或落点 |
| `walk` | 全局 | WP-42 | `Pose` | 61 | 脚位、朝向与俯仰、潜行、5 档步速、16 槽安全点环 | 速度、土狼时间与跳跃缓冲、相机插值缓冲、泳态与水体引用、骑行状态；在空中、水里（涉水或游泳）、骑行中、回位途中时，存最近的岸上安全点（§4.2） |

- **三个阶段**：
  - `World`：世界与天气，在换图之前恢复，`IWorldState.Instance` 在这一阶段结束时加 1；
  - `Content`：换图之后恢复的按图内容和进度；
  - `Pose`：落位。完整读档时紧接着恢复；开始页预览时只解析、不恢复，留作"继续"的终点，按"继续"后由 `CompleteResume` 恢复（§5.1）。
- **按图的段**：在文件里的键是 `map:<MapId>/<段 ID>`，例如 `map:qingshuihe/weather`。
- **视角为什么只存一份**：速度档是全局的，安全点环在换图时清空（SYS-MAPS），所以视角只存"当前这一份"。每张图的最后位姿记在 `maps` 段的 `MapVisit` 里。
- **自定义标记的 ID**：用本存档内按图递增的整数（SYS-ATLAS §4.6），不用 `Guid.NewGuid`（G4 禁用，ENG-ARCH §2）。SYS-ATLAS `CustomPin` 的 ID 按这一条用整数（ENG-ARCH §3）。
- **游玩时长、代号、世界线号**：存在 meta 里（§3.2），由 WP-50 自己维护，不另设段。
- **规则集**不设段：本局的规则集 ID 记在 meta 的 `ruleset`（§3.2，即 `SaveMeta.Ruleset`），这是它在存档里唯一的位置；规则集的内容在 `game/data/rulesets/<id>.json`（ENG-ROADMAP §6 第 1 条，契约以 ENG-ARCH §3 为准），不进存档。读档先按它定规则集（§5.1），新游戏先定规则集再建各段（§5.5）。
- **不存任何音频状态**（SYS-AUDIO）：声景由世界状态和位姿重建；散布的相位、洗牌袋的位置不进存档，也不在一致性之内（§5.3）。
- **玩家身体与人称**（SYS-WALK §1、SYS-VIEW §2.7）：身体没有要存的状态，现在不设段：读档后身体静止、干燥，脚 IK 与平滑历史清空。第一人称还是第三人称、第三人称距离是偏好，存在 `settings.cfg`（WP-35），不进存档。相机位姿照旧只由 `view` 段和 `walk` 段的脚位推出，第三人称的吊杆读档后从人称偏好重建。
- **游泳**：泳态、水体引用和划相都不存。在水里保存，`walk` 段存最近的岸上安全点，`view` 段和 `maps` 段当前图的那一条、meta 的位姿与空间都写同一个点；读档总是站在岸上（§4.2）。
- **动物**（WLD-LIFE §1.1）：猫、鸽群和水禽的反应、冷却、惊飞与让开都是瞬态，不设存档段，读档、切图后按栖地与当前世界状态重建。
- **共享单车**（SYS-WALK §16）：首版读档一律恢复成步行，借的车视为已归还，不恢复骑姿。
  - 车辆一段由 WP-55 负责，按图记每辆可借车的停放归属；段名、阶段、顺序、格式与版本由 Astra 定。
  - 保存时正在骑或临停着的借车，按归还到借车时预留的来源回收槽记；人按本表 `walk` 一行，骑行中存最近的岸上安全点。`view`、`walk`、`maps`、meta 与车辆记录出自同一次落定；保存只记下这个结果，不挪动正在玩的人和车（§4.2）。
  - 骑行的速度、踏频、曲柄与车轮的相位、骑姿和声音都不存。
  - 旧档缺这一段时按基线初始化一次；停放点改了或删了，按版本迁移和校验修复处理，不重复补车（§3.6、§5.6）。

**档案级文件**（跨存档共享，不进存档，§8）：

- `settings.cfg`：WP-35。界面设置；首次引导是否已显示；进场过场"完整 / 简短 / 关"（SYS-TITLE）；小地图模式与地图筛选偏好（SYS-ATLAS）。
- `input.cfg`：WP-06。
- `graphics.cfg` 和 `engine_override.cfg`：WP-32，包括设备档缓存。
- `audio.cfg`：WP-53a。五组音量与声音的各项开关（SYS-AUDIO）。

换存档位时，这些都不变。

**`user://cache/bake/`**：本机烘焙缓存（WP-02，SYS-MAPS §2.4，D-165），**不是存档**，也不是档案级设置文件。存档不引用它的任何内容；删掉它（设置卡的"清除缓存"，或手动删目录）不影响存档，之后需要时重新烘焙。它的读写方是 `IBakeService`，不经 `Campus.Kernel.Save`。

**不恢复**：

- 打开着的面板、纯净模式（U）：读档后回到界面态，不开任何面板；
- 偶发事件的进度和计时：读档算一次转场，20 s 和 180–300 s 两个计时都重新开始（SYS-MOTION §10.1）；
- 流式加载的状态、自动调节画质的统计、管线缓存。

**室内**（WLD-INTERIORS）：

- 开放楼的入口门固定敞开，关着的楼的门固定关着，都不是状态；电梯"关门换位"是一段过渡。开放室内里的房门和楼梯间防火门走近自动推开、走过后由闭门器关上，也是过渡：门的状态不进存档，存档时一律按关好记（转动途中、停在开位的都算关好），读档后一律是关好的样子，观者走近时照常推开（WLD-INTERIORS §4.1）。所以室内现在没有要存的东西。
- 读档只恢复位姿和所在空间 `NavSpace`，进楼加载按离入口 25 m 的规则自然发生；在室内时，等这一层就绪再落地（ENG-PERF §5 第 6 条，WLD-INTERIORS §6.4；§5.1 第 6 步）。
- 开放清单里加楼，只改数据，再注册一个 `IInteriorProvider`，存档格式不变。

**以后 RPG 的预留段**（现在不登记，名字先占住，ENG-ROADMAP §6 第 6 条）：

| 段 | 范围 | 用途 |
| --- | --- | --- |
| `quest` | 全局 | 任务与剧情进度 |
| `inventory` | 全局 | 物品 |
| `character` | 全局 | 玩家角色本体的外观、装扮等状态；现在身体没有要存的东西，只占名字。NPC 与他人仍不允许出现（INV-6） |
| `flags` | 全局 | 全局开关表（剧情触发、一次性事件） |
| `calendar` | 全局 | 日历原点与偏移（日期由 `WorldHours` 推出），只占名字 |
| `agents` | 全局 | NPC 偏离日程的状态（按日程推得出的不存），只占名字 |
| `map:<id>/objects` | 按图 | 地图上的物体变化，记"相对烘焙基线的差分"。键是 `FeatureId` 加局部键，不用节点路径或实例序号。某栋楼的室内以后有了可变的东西（门、可拿取的物品），也记在这里，键用那栋楼的 `FeatureId`，加楼不用改格式。新构建里某个 `FeatureId` 不存在了，就丢掉这一条，并记一次修复（§5.6） |

## 3 文件格式与版本迁移

### 3.1 容器

一律小端序。头固定 64 字节，后面依次紧排 meta、正文、缩略图：

| 偏移 | 字节 | 字段 |
| --- | --- | --- |
| 0 | 8 | 魔数 `UESTCSAV`（ASCII） |
| 8 | 2 | 容器版本，u16，现为 1 |
| 10 | 2 | 标志，u16：位 0 表示正文是 Brotli（现在恒为 1），位 1 表示有缩略图；其余位必须为 0 |
| 12 | 4 | meta 长度，u32 |
| 16 | 4 | 正文长度，u32（压缩后） |
| 20 | 4 | 缩略图长度，u32；没有缩略图时为 0 |
| 24 | 8 | 代号，u64 |
| 32 | 32 | SHA-256：对第 0–31 字节和其后的全部内容计算 |

- 文件总长必须正好等于 64 加三段长度，否则视为损坏。
- **不用"临时文件 + 改名"**：每代都是用 `FileMode.CreateNew` 新建的文件，写了一半就断电，这一代的长度或哈希必然对不上，读档时退到上一代（§7）。所以不依赖改名的原子性，Windows 上改名本来也不是原子的（§4.5）。

### 3.2 meta

未压缩的 UTF-8 JSON，最多 16 KB。开始页的存档卡和外部工具只读这一段就够了，但读档时照样校验整个文件的哈希（观光档才几 KB，校验不到 1 ms）。

```json
{ "format": 1, "build": "0.4.0+a1b2c3d", "slot": "s1", "kind": "Auto", "gen": 42, "line": 31,
  "playSeconds": 11520.4, "map": "qingshuihe", "worldHours": 93.67, "clock": 21.67,
  "season": "Winter", "weather": "Snow", "mode": "Walk",
  "pose": { "position": [-41.0, 1.62, -702.0], "yawDeg": 180, "pitchDeg": 0 }, "space": null,
  "region": "north.commercial", "placeKey": "place.north.commercial",
  "savedAtUnix": 1792082400, "reason": "Quit", "ruleset": "sightseeing", "origin": null, "hasThumb": true }
```

（数值只是示例。）

- `region` 是观者所在的区域：按 `IObserverState` 的观者位置查 SYS-ATLAS区域栅格得到的区域 ID。
- `placeKey`：地点名的翻译键，保存时由区域 ID 拼出；在开放室内时取那栋楼的名字。存档卡的摘要和"继续"过场的地点字幕都用它，例如"冬 · 21:40 · 小雪 · 商业街 · 03:12:45"（SYS-TITLE、UI-VISUAL）。新构建里查不到这个键时，退回图名。
- `pose`：镜头位姿，与 `view` 段相同。开始页的"继续"过场按它定终点，不用解析正文（§5.1）。
- `space`：观者所在空间 `NavSpace`（WLD-INTERIORS），取 `IObserverState`（ENG-ARCH §3）；镜头位姿另记在 `pose`。
- `savedAtUnix` 来自系统时钟，**只用于显示**，从不进入世界状态（§6）。
- `ruleset`：本局的规则集 ID（ENG-ROADMAP §6 第 1 条），观光恒为 `"sightseeing"`，`"rpg"` 预留。这是规则集在存档里唯一的位置（§2）；读档时先按它定规则集（§5.1）。

### 3.3 正文：分段 JSON

```json
{
  "format": 1,
  "sections": {
    "maps":    { "v": 1, "data": { "current": "qingshuihe", "visits": [ { "map": "qingshuihe", "lastPose": { "position": [-41.0, 1.62, -702.0], "yawDeg": 180, "pitchDeg": 0 }, "mode": "Walk", "weatherEpoch": 3, "leftAtHours": 93.67 } ] } },
    "world":   { "v": 1, "data": { "worldHours": 93.67, "clockOffset": 0.0, "seasonPos": 3.0, "seasonTarget": "Winter", "flowRate": 1, "flowRateBeforePause": 1, "animTime": 233.4 } },
    "weather": { "v": 1, "data": { "seed": 2914470211 } },
    "map:qingshuihe/weather": { "v": 1, "data": { "epoch": 3, "segment": 17, "attempt": 2, "mode": "Auto" } }
  }
}
```

（示例里各段只列了部分字段。当前图那一条 `MapVisit` 的位姿与 meta 的 `pose`、`view` 段相同。）

- 整个正文用 Brotli 压缩，取最快档。观光档压缩前约 2.5 KB、压缩后约 0.9 KB（§13）。
- **未知段原样保留**：本构建没登记的段，在容器层按原始字节保留，不反序列化，写回时照抄。
- **缺段**：用该段的 `CreateDefault` 补上。
- 版本号分三层：
  - 头里的容器版本，管二进制布局；
  - `format`，管 `sections` 这一层的结构；
  - 每段的 `v`，管这一段的字段。

### 3.4 缩略图

- 规格：384×216 的 WebP 有损图，质量 0.8，约 1.2 KB，上限 256 KB。窗口不是 16:9 时，先居中裁成 16:9。
- 只在退出、返回标题、窗口失焦、新游戏交还控制时抓新图；定时保存和关键动作后的保存，沿用上一代的缩略图字节，避免每 5 分钟卡一帧（§4.1）。
- **抓帧分两帧做**（P0 核对，§17）：
  1. 第 N 帧，在存档那一步隐藏界面的根节点；
  2. 第 N+1 帧，在同一步调 `GetViewport().GetTexture().GetImage()`，拿到第 N 帧那张不带界面的画面，然后恢复界面。

  窗口最小化时引擎不绘制，所以不抓新图，沿用上一代。
- **后台处理**：裁切 → 连做 `ShrinkX2()`，直到宽度不超过 768 → Lanczos 缩到 384×216 → `SaveWebpToBuffer(true, 0.8f)`。
- **解码**：界面用 `Image.LoadWebpFromBuffer` 解码。先查长度上限和 RIFF/WEBP 头，解码后尺寸超过 512×288 就丢弃。

### 3.5 序列化约定

- 每个包在自己的目录里写一个 `JsonSerializerContext`（源生成），用 `[JsonSourceGenerationOptions]` 写成与 `ContractJson.Options` 相同的约定（ENG-ARCH §3）：
  - 字段用 camelCase，枚举按字符串写，所以以后改了 `WeatherKind` 的顺序也不会坏档；
  - 向量写成数组 `[x, y, z]`；
  - ID 类的 record struct（`SlotId`、`MapId` 等）写成字符串。

  G2 比对两套约定的输出一致。
- **DTO**：每段一个 `sealed record`，只读，只放基础类型、`System.Numerics` 向量、契约里的枚举和 ID。不放 `object`、`JsonElement`、Godot 类型，也不用开放的多态。
- **缺字段要报错**：字段标为必需（P0 核对 .NET 10 源生成下的行为，§17）；同一版本里出现未知字段也报错（`JsonUnmappedMemberHandling.Disallow`）；读到本构建不认识的枚举字符串同样报错（ENG-ROADMAP §6 第 4 条）。这三种情况都按"这一段不合法"处理（§7）。
- **浮点**：double 和 float 按最短往返格式写。NaN 和 ∞ 写不进 JSON，序列化会抛异常：快照里出现它们就是 bug，由 G20 抓。
- **只用带 `JsonTypeInfo` 的重载**：存档代码不用反射式序列化（G4）。如果全工程的 JSON 以后都改成源生成，可以在 csproj 设 `JsonSerializerIsReflectionEnabledByDefault=false`（待定，§17）。

### 3.6 版本与迁移

- **段版本**：一段的字段有任何增减都要升 `v`，哪怕只加了一个可选字段。这样旧构建遇到新档时只会只读，不会读进来、丢掉新字段再写回去。从 P0 起执行，纪律与契约的"只增不改"相同。
- **迁移**：
  - 每段自带逐版本的迁移函数 `Migrate(v, JsonNode) → v + 1`，读档时在后台线程按链迁到当前版本。做法与 SYS-GRAPHICS §6 的 `graphics.cfg` 一样。
  - 迁移函数是纯函数：不碰 Godot，不读服务状态。
  - 迁移后不立即写回，等下一次正常保存时按新版本写。
- **新于本构建**：容器版本、`format` 或任何一个已登记段的 `v` 比本构建新，整个存档位就是 `NewerBuild`：只读，不读档，不自动写（§7）。
- **金档**：
  - 每个发布过的格式各留一份金档，放在 `game/kernel/save/tests/golden/`，从 P0 起累积；
  - 每一份都必须能读，每一步迁移单独测（G20）。
- **容器版本 1 在 P0 定死**：RPG 开工时只加段，不改容器。
- **持久化 ID 换名**（CORE-CONVENTIONS §3.9）：存档里引用的 ID（地图、点位、锚点与其余标记、室内入口、区域与 `placeKey`、`FeatureId`）不复用，改名不改身份，所以平时不需要迁移。确实要换 ID 时：
  - 负责那份数据的包随数据提交一份旧 ID → 新 ID 的映射，放在本图目录的 `id-remap.json`（每条写旧 ID、新 ID 和生效的数据版本）；
  - 引用这些 ID 的存档段各升一次 `v`；WP-50 提供按映射改写 ID 的公共迁移函数，各段的 `Migrate` 调它。映射在构建时生成为常量表传给迁移代码，迁移仍是纯函数、不读文件；
  - 只有真正删除的对象不给映射，读档时照旧按 §5.6 的校验修复去掉；`placeKey` 查不到时按 §3.2 退回图名；
  - 每次换 ID 补一份带旧 ID 的金档，G20 的迁移项验证改写结果。

## 4 保存时机与可靠性

### 4.1 时机

| 时机 | `SaveReason` | 谁发请求 | 限频 | 缩略图 |
| --- | --- | --- | --- | --- |
| 关闭窗口、Alt+F4、菜单"退出" | `Quit` | WP-01（经 SYS-TITLE的 `IAppFlow` 进入 Quitting；Leaving 是返回标题） | 不限，立即执行（`FlushForQuit`） | 抓新图 |
| 返回开始页 | `ToTitle` | WP-49 | 不限，立即执行 | 抓新图 |
| 漫游态累计 5 分钟 | `Interval` | WP-50 | 30 s 合并 | 沿用上一代 |
| 跳转到达 | `Settled` | WP-36 | 30 s 合并 | 沿用 |
| 传送到达 | `Settled` | WP-48c | 30 s 合并 | 沿用 |
| 换季转场落定、`Apply` 落定、"回到落金初灯"落定 | `Settled` | WP-34 | 30 s 合并 | 沿用 |
| 换图到达（读档、冷启动引起的换图除外） | `MapChanged` | WP-46 | 30 s 合并 | 沿用 |
| 窗口失焦（`NotificationApplicationFocusOut`） | `FocusLost` | WP-06（与失焦时切到界面态是同一处，UI-NAV §5） | 30 s 合并 | 抓新图；最小化时沿用 |
| 新游戏交还控制 | `NewGame` | WP-49 | 不限（`SaveNow`） | 抓新图（P00 全景） |
| 门禁的 `--save-at` | `Test` | WP-50 | 不限 | 不抓 |

- **合并与限频**：
  - 距上次成功保存不到 30 s 的请求挂起，到满 30 s 时再执行；挂起期间再来的请求合并成一次，原因取最新的那个。
  - 每次成功保存后，5 分钟的计时从零开始。
  - 同一时间只有一次写盘；写盘期间再来的请求排成一个待办。
- **只在漫游态保存**：开始页、进场过场、返回标题途中（Leaving）、退出途中（Quitting）、换图途中，`Request` 都被忽略。退出和返回标题走 `FlushForQuit`，不受这条限制。
- **5 分钟怎么计**：按漫游态每帧的真实 delta 累加，单帧上限 0.1 s，不用 `Timer`。游玩时长也按同样的方法累加。

### 4.2 安全时刻与落定快照

- **安全时刻**同时满足以下几条：
  - `IAppFlow` 处于漫游态；
  - 没有雾幕：换图、传送、画面设置的雾幕都算；
  - 每一段的 `IsSettled` 都为真。

  `IsSettled` 由各段自己判断：
  - `world`：没有快进和换季转场。自动天气的自然演化不算过渡，因为它由队列和时间决定，读档后自然接得上；
  - `map:<id>/weather`：没有手动换天气或换季替换的过渡；
  - `view`：没有跳转、传送、降落或起飞；
  - `walk`：着地、不在水里（涉水、游泳都算在水里）、不在骑行中、不在回位途中；
  - `maps`：没有正在换图；
  - 预留段由各自的负责包定义。
- **顺延**：条件不满足就顺延，最多 60 s；到期后按"强制快照"拍。长时间游泳时同样最多顺延 60 s，到期拍强制快照，不无限挂起。
- **落定快照**：每段的 `Capture` 必须在任何时刻都能给出落定值，所以退出时（`CaptureContext.Forced` 为真）也能存：
  - 过渡、快进、跳转、降落、换图进行中，取终点；
  - 行走时在空中或水里，取最近的岸上安全点（SYS-WALK §9；室内池取本层的池岸安全点，不取楼外）；`view`、`walk`、`maps` 三段与 meta 的位姿、空间都指向这同一个点。保存只记下这个点，不把正在游泳的角色真的挪到岸上；
  - 骑行中或借着共享单车时：人在骑行中取最近的岸上安全点，已下车步行时取原地的步行站位；借的车按归还到来源回收槽记，车辆记录与 `view`、`walk`、`maps`、meta 出自这同一次决定。保存不把正在骑的人和车真的挪走（§2，SYS-WALK §16）；
  - 电梯"关门换位"途中（WLD-INTERIORS），取目的站；
  - 开放室内的自动门正在转动或停在开位，按关好记（§2）。
- **快照的形式**：主线程在 `FramePriority.Save` 那一步调用各段的 `Capture`（§11），各段返回不可变的 DTO，不做任何 I/O。全部段合计 p99 不超过 0.3 ms。

### 4.3 写盘流程（线程池）

1. 把各段的 DTO 依次写进同一个 `Utf8JsonWriter`；未知段的原始字节照抄；再写 meta。
2. 正文用 Brotli 最快档压缩；缩略图按 §3.4 处理；拼装文件并算 SHA-256。
3. 用 `new FileStream(路径, FileMode.CreateNew, FileAccess.Write, FileShare.None)` 写 `g<新代号>.sav`，然后调 `Flush(true)`。Windows 上它调用 FlushFileBuffers，真正落盘。路径由 `ProjectSettings.GlobalizePath("user://saves/…")` 换算，或取 `--save-dir`。
4. 读回校验一次：重新读文件并核对哈希，观光档约 0.4 ms。
5. 按 §1 删掉多余的旧代。
6. 用 `CallDeferred` 回到主线程，更新 `Slots`，发出 `SlotsChanged`。

- 第 3、4 步失败（磁盘满、无权限、杀毒软件占着文件、读回校验不符）时：删掉这次新写的、不完整的文件（只删自己刚写的），旧代不动；写日志，把结果和原因记进保存状态，按 §4.6 让观者知道，下一个时机再试。

### 4.4 退出流程

1. **关闭请求**：`NotificationWMCloseRequest` 到达，或观者点了菜单里的"退出"。WP-01 的 Boot 在第一帧之前就把 `GetTree().AutoAcceptQuit` 设为 false。
2. **进入退出态**：`IAppFlow` 切到 Quitting，关闭输入（SYS-TITLE §7.1；Leaving 是"返回标题"，不是退出）。
   - 进场过场途中退出：先按"跳过"把世界落到终态，然后保存。跳过的终态与完整播放一致（SYS-TITLE）。
   - 换图途中退出：等 `IMapTransition.Done`，最多 2 s；超时，或换图以 `Failed` 结束（SYS-MAPS §4.3），就不写这一代，上一代仍然有效，`FlushForQuit` 返回 `Deferred`（有意不写，不算失败）。
3. **写设置**：同步写设置文件（几 KB、几毫秒，§8）。
4. **抓缩略图**：按 §3.4 分两帧抓。
5. **保存**：调 `FlushForQuit(SaveReason.Quit, 2 s)`，拍强制快照、交给后台写盘。同时雾幕用 0.3 s 淡入，画面照常渲染；声音的 Master 组同步 0.3 s 淡出，不额外拖延退出（SYS-AUDIO）；等待超过 1.5 s 时亮三颗等待灯（UI-VISUAL）。
6. **退出**：按 `FlushForQuit` 返回的结果走：`Saved`；`Disabled`（`--save=off`，或另一个窗口持有锁，本来就不保存，§1）；`Deferred`（第 2 步有意不写）这三种调 `GetTree().Quit()`。`Failed`（写盘出错、读回校验不符，或 2 s 内没写完）不退出，转第 7 步。
7. **保存明确失败时让观者选**：`IAppFlow` 停在 Quitting，雾幕上弹确认对话框（UI-VISUAL §8.8）"这次没能保存"，正文写原因和上一次成功保存的时间，例如"磁盘空间不足。上一次保存在 21:40（3 分钟前），之后的进度还没有存下"。三个按钮：
   - **重试**（默认焦点）：等仍在进行的那次写盘结束，没写成就重新拍强制快照再写一次；写成就照常退出，又失败就留在这个对话框，原因随之更新；
   - **返回游戏**（Esc 同此）：撤雾幕，恢复声音和输入，回到 Playing（SYS-TITLE §7.1），世界从退出前接着走；
   - **仍然退出**：不再写，直接 `Quit()`；旧代不受影响。
   - 超时的那次写盘不取消：对话框打开期间它若写成，对话框自动关闭，照常退出。对话框打开时再收到关闭请求，按"仍然退出"处理。

- **时限**：保存成功或有意不写时，从关闭请求到进程结束不超过 1.5 s（G10），硬上限 2 s（G5）；保存明确失败时等观者选，不计时限。
- **崩溃**：收到 `NotificationCrash` 时不保存，这时状态可能已经坏了。
- **系统关机、注销**：Godot 在 Windows 上不处理 WM_QUERYENDSESSION，收不到关闭请求，只能靠每 5 分钟的定时保存兜底。P2 可以评估是否自己子类化窗口过程，接住 WM_QUERYENDSESSION 后同步保存（§17）。

### 4.5 Godot 与 Windows 的几个坑

| 坑 | 依据 | 对策 |
| --- | --- | --- |
| 默认 `AutoAcceptQuit = true`：收到关闭请求后引擎在一帧（约 34 ms）内就退出，后台任务来不及写 | 文件没写出来 | Boot 设为 false，后台写完再 `Quit()` |
| 在关闭通知的回调里同步写：能写成，但那一帧卡住 | 模拟 400 ms 写盘，那一帧 437 ms | 不在回调里写盘 |
| `AutoAcceptQuit = false` + 后台写 + `Quit()` | 模拟 400 ms 写盘，从请求到 `Quit()` 共 433 ms，期间每帧 30–37 ms 照常渲染，文件写成 | 采用（真实的观光档写盘约 2 ms） |
| Windows 关机、注销时没有关闭请求 | `display_server_windows.cpp` 不处理 WM_QUERYENDSESSION | 定时保存兜底（§4.4） |
| `FileAccess.Flush()` 只调 `fflush`，不保证落盘 | `file_access_windows.cpp` | 写存档用 .NET `FileStream.Flush(true)` |
| Godot 的"安全写"（临时文件 + ReplaceFileW）只在开了 `backup_save` 时生效，导出版默认关 | — | 不依赖它 |
| `DirAccess.Rename` 在 Windows 上是"先删目标，再 MoveFileW"，不是原子的 | `dir_access_windows.cpp`；覆盖已有文件时返回 Ok | 存档不改名；设置文件改用 `File.Replace`（§8） |
| .NET `File.Move(…, overwrite: true)` 在目标被其他句柄打开时报拒绝访问，即使对方用 `FileShare.Read \| Delete` 打开也一样 | `UnauthorizedAccessException` | 杀毒、备份软件可能正占着文件：存档不覆盖；设置文件写失败就重试（§8） |
| 鼠标被捕获时的 Alt+F4 由 Godot 自己转成关闭请求 | — | 与关窗口走同一流程 |
| 有独占的子窗口（`Window` 的 exclusive）打开时，关闭请求会被忽略 | `window.cpp` | 界面不用独占的 `Window` 弹窗（UI-NAV §5，UI-VISUAL） |
| 第一次序列化 29 ms，是 JIT | — | 雾幕或扉页期间空跑一次"序列化 + 压缩 + 哈希"，写进内存流（ENG-PERF §6） |
| `GetImage()` 在 1080p 下占主线程 3.8–4.1 ms（第一次 9.1–9.5 ms），1440p 下 6.6–7.1 ms | — | 只在退出、返回标题、失焦、新游戏时抓；那一帧不计入自动调节画质的统计（ENG-PERF §11.3） |
| `TextureGetDataAsync` 下一帧才回数据，调用本身仍占 2.2 ms，还会分配 8.3 MB 的托管数组 | — | 不用它，用 `GetImage()` |
| Lanczos 缩到 384×216 要 23 ms（1440p 下 38–41 ms） | — | 放到线程池里做 |

### 4.6 保存状态与失败反馈

保住旧代不等于观者知道这次没存上，所以保存结果一直摆在界面上，不只提示一次。

- **保存状态** `ISaveService.State`（§10）：当前存档位；本次运行最近一次写盘的结果和失败原因；当前存档位最后一次成功保存的时间（取最新可读一代 meta 的 `savedAtUnix`，重启后照样有）；不保存的原因（`--save=off`、另一个窗口持有锁）。值变时发 `SlotsChanged`。
- **原因**写成文案键（UI-NAV §8）：`save.error.disk_full`（`IOException` 的 HResult 低 16 位是 `ERROR_DISK_FULL` 或 `ERROR_HANDLE_DISK_FULL`）、`save.error.locked`（`ERROR_SHARING_VIOLATION`、`ERROR_LOCK_VIOLATION`）、`save.error.denied`（`UnauthorizedAccessException`）、`save.error.verify`（读回的哈希不符）、`save.error.timeout`（退出时 2 s 内没写完）、`save.error.other`（其余，异常类型写日志）。
- **持续显示**：存档页的存档卡和详情栏（UI-VISUAL §7.9）、设置卡"设置"页的"存档"一行（UI-NAV §4.6）都读这份状态。失败后一直显示"最近一次保存失败 · 原因 · 上次成功 21:40"，直到下一次保存成功；只读时显示原因。
- **临时提示**：一串连续失败只在第一次时提示，中间成功过一次再失败会再提示；遇上过场、雾幕时延后到它们结束，不丢（UI-NAV §4.9）。
- **退出时失败**：按 §4.4 第 7 步让观者选，不静默退出。返回标题时失败照常回到开始页，状态留在存档页上。
- **分工**：WP-50 维护保存状态和 `FlushForQuit` 的结果；WP-49a 的 `IAppFlow` 处理退出时的三选一；WP-35（35b、35d）做对话框、存档卡、设置行和延后的提示。

## 5 加载流程与一致性

### 5.1 加载流程

什么时候读档：

- 冷启动时，在扉页后面读"继续"的存档位，供开始页的 T0 显示；
- 在存档页选了另一个存档位；
- 命令行给了 `--slot` 或 `--load`。

两种读档范围：

| 范围 | 恢复哪些阶段 | 用在哪里 |
| --- | --- | --- |
| `LoadScope.Full` | `World` → 换图 → `Content` → `Pose`，一次做完 | `--slot`、`--load` 配 `--title=0`；走雾幕直达的读档 |
| `LoadScope.Preview` | `World` → 换图 → `Content`；`Pose` 阶段的段只解析、校验，留作终点 | 开始页：冷启动读"继续"的存档位、在存档页换存档位、继续过场的全遮段里换世界（SYS-TITLE）。交还控制时 `IAppFlow` 调 `CompleteResume()` 恢复留下的 `Pose` 段 |

步骤（先定规则集，再恢复世界段与天气段，然后换图）：

1. **遮挡**：雾幕保持不透明；开始页阶段由扉页或过场的云卡遮挡（SYS-TITLE）。输入关闭。
2. **后台读取**：读最新一代 → 校验 → 解压 → 解析 → 迁移。失败就退到同一世界线的上一代（§7）。
3. **规则集、世界与天气**：校验通过后，先按 meta 的 `ruleset` 定本局的规则集（`IRuleset`，ENG-ROADMAP §6 第 1 条），再进 `World` 阶段：在某一帧的存档那一步（`FramePriority.Save`），按顺序调用 `maps`、`world`、`weather` 和每一段 `map:<id>/weather` 的 `Restore`。`maps` 只登记当前图和各图的 `MapVisit`，不动镜头。这一阶段结束时 `IWorldState.Instance` 加 1，`Active` 设为这个存档位。
4. **换图**：存档的当前地图不是已加载的那张图时，经 `IMapService.Travel(MapTravelCause.LoadSave)` 换图（SYS-MAPS）：
   - 位姿和模式取这张图的 `MapVisit`，它在保存时与 `view` 段一致；`Preview` 范围不给位姿，镜头留在 T0（SYS-TITLE）；
   - 进图时天气按刚恢复的状态补算到当前世界时间，起点就是存档那一刻，所以补算量为零；
   - 换图本身不改 `Instance`，途中不是安全时刻，到达后也不再请求 `MapChanged` 保存。

   现在只有一张图，而且冷启动的 `Travel(Boot)` 本来就进的是最近存档的图（SYS-MAPS），所以这一步通常不做事。
5. **内容与落位**（`Content`、`Pose` 阶段）：换图完成后（同图时就在第 3 步的同一帧），在存档那一步依次恢复每张到过的图的 `map:<id>/atlas`（平面地图服务按图保存，当前图的立即生效，别的图在进图时生效）和 `progress`，`Full` 范围再恢复 `view`、`walk`。缺的段一律用 `CreateDefault` 补；全部立即生效，不做过渡动画；各段顺手做校验修复（§5.6），位姿按 WLD-OUTSKIRTS的可达空间合法化。从下一帧起，所有系统同时看到新世界（§5.2）。
6. **预取**：恢复的位姿作为"已知目的地"预取各级格子（ENG-PERF §5）。`NavSpace` 指向开放室内时，按 WLD-INTERIORS的规则加载这栋楼的开放部分，等这一层的 IF 与碰撞就绪再落地（ENG-PERF §5 第 6 条，WLD-INTERIORS §6.4）；行走模式下还要等碰撞格就绪（SYS-WALK §14 第 5 条）。
7. **撤遮挡**：就绪之前不撤。
   - `--title=0`，或走雾幕直达的读档时，雾幕 2–3 s 淡出（SYS-TITLE §1）；
   - 从开始页按"继续"时，由约 9 s 的继续过场落到 meta 里的 `pose`（SYS-TITLE）；交还控制那一帧调 `CompleteResume()`，`view`、`walk` 两段在存档那一步恢复，`Instance` 不变。

   之后放开输入，恢复存档里的流逝倍率。

- **开始页**：
  - 在开始页（Title）时，世界的流逝由 `IAppFlow` 冻结，不改写存档里的倍率；`AnimTime` 照走。
  - 相机由过场播放器放在 T0；行走的物理照常冻结，因为输入已关、碰撞格还没就绪。
  - 冷启动按 `Preview` 读一次档；按"继续"时不再读第二次，只调 `CompleteResume()`。"继续"的是另一个存档位时，在继续过场的全遮段里对它再做一次 `Preview` 读档（`Instance` 加 1），同样在交还时 `CompleteResume()`。

### 5.2 世界实例：`IWorldState.Instance`

- **定义**：一个**世界实例**是一段连续演化的世界，从一次读档或新游戏开始，到下一次读档或新游戏为止。
  - `Instance` 是进程内的计数：启动后第一次得到世界状态时为 1，此后每次 `Load`、`LoadFile`、`NewGame` 加 1。它不进存档。
  - 对应到开始页（SYS-TITLE）：开始页预览恢复、新游戏建时间线、读另一个存档位，各加 1；按"继续"进入正在预览的那个存档位（`CompleteResume`）不加；换图也不加（SYS-MAPS）。
- **时间单调的范围**：`WorldHours` 只增不减（SYS-TIME §1），这条只在同一个实例内要求。读档是换了一个世界，不是让同一个世界倒流；跨实例比较 `WorldHours` 没有意义。
- **恢复不经 `IWorldControl`**：`IWorldControl` 仍然不提供任何让时间倒退的方法；恢复走 WP-34 自己实现的 `world` 段。
- **"回到落金初灯"不换实例**：它是同一个实例里的正向换季加快进，种子不变（SYS-WEATHER §6.6）。
- **换实例时直接落到新值**：凡是做平滑、带回差或保留历史的系统，看到 `Instance` 变了，就把内部状态直接设成新世界的值，不做过渡。例如：
  - 钟点的弹簧追随、换季转场的状态机（WP-34）；
  - 主光的方向量化与月影淡入（WP-03）；
  - 天气向量的插值（WP-45）；
  - 界面"晶白 / 深屏"主题的回差切换（WP-35，UI-VISUAL）；
  - 窗灯与路灯的逐盏错开（WP-39）；
  - 自动调节画质的统计（WP-32）；
  - 植被近池（WP-23）；
  - 声音的平滑量、虚拟相位、散布状态（WP-53，SYS-AUDIO）。

  这一刻总在遮挡之下，不算硬切，G13 也不录它。

### 5.3 "状态一致"的定义

- **落定一致**（任何一份存档）：读档后的状态等于存档那一刻的落定状态（§4.2）。
- **续跑等价**（在安全时刻拍的存档）：同一构建、同一份数据，读档后给相同的输入和相同的 dt 序列，以下各量与没退出时逐帧、逐位一致：
  - 世界：`WorldHours`、钟点、季节、流逝、`AnimTime`；
  - 天气：选择、天气向量、前瞻队列。
- **强制快照**（退出时、顺延满 60 s 时）：过渡直接落到终点，所以终点以前的几秒不等价；从终点以后起续跑等价。
- **不在一致性之内**的：
  - 面板、纯净模式；
  - 偶发事件（重新计时）；
  - 当前速度（读档后静止）；
  - 渲染侧的状态：选级、淘汰、自动调节。它们由位姿和设置重新推出，不影响世界状态；
  - 声景：散布声的具体时刻、变体选择、洗牌袋的位置，由世界状态和种子重新推出（SYS-AUDIO）；
  - 开放室内里自动开关的房门：读档后一律是关好的样子（§2）；
  - 玩家身体的姿态、湿度与平滑历史，第三人称的吊杆：读档后身体静止、干燥，吊杆按人称偏好重建（§2）；
  - 动物的反应、冷却与惊飞：按栖地和当前世界状态重建（§2，WLD-LIFE §1.1）；
  - 骑行：读档后是步行，借的车已归还；骑姿、速度、踏频与相位不续（§2）。
- **补丁更新后读旧档**：世界状态仍然一致，但画面可以不同，因为模型和调色板可能改了；已经生成的天气队列不变（§5.4）。

### 5.4 天气

- **没有别的随机状态**：自动天气的每次抽取由（种子、mapId、epoch、片段号、尝试号、用途）决定（SYS-WEATHER §6.4、SYS-MAPS）。存了种子和这几个计数，就能接着往下抽。
- **种子每个存档一个**：
  - 新游戏时取一次熵，或用 `--weather-seed`；
  - 存进 `weather` 段，之后跟着存档走，读档时不重新取。
- **已生成的队列也存**：过去 24 h 和前瞻 24 h 都存进 `map:<id>/weather`。这样补丁调了 `game/data/weather` 的参数，观者已经看到的天气和预报也不会变；队列之后的部分才按新参数生成。
- **读档时**：不调用 `IWeatherProcess.Reset`，也不重新"保持晴"。保持晴的窗口本身存在存档里，到点自然结束。
- **换图**：进图时，把本图的队列补算到当前的 `WorldHours`（SYS-MAPS）。

### 5.5 新游戏

- **交还控制时一律是落金初灯**：
  - 世界：秋季、17:30、晴、自动天气、流逝 1×，天气保持晴到秋季黄昏带结束（约 19:36，SYS-WEATHER §6.6）；
  - 视角：旁观模式；
  - 地图：默认图 `qingshuihe`（SYS-MAPS）。

  交还控制时镜头在 P00，这是新游戏过场的终点（SYS-TITLE）。
- **建实例**：`NewGame` 先定规则集（观光恒为 `sightseeing`，`IRuleset`，ENG-ROADMAP §6 第 1 条），再取新种子，`Instance` 加 1；各段用 `CreateDefault` 建出初值；记下新的世界线号。
- **起点预设**：新游戏的世界过程只有一种，完整、简短、关、跳过都一样（SYS-TITLE）。
  - `NewGame` 一律用本图 `map.json` 的 `newGameStart` 建实例（清水河：秋、17:05:00、晴、自动天气，保持晴到秋季黄昏带结束，SYS-MAPS）；
  - 再由 `IWorldControl.FlowTo(17:30, 倍率)` 正向推到 17:30 停住，交还时转 1×。倍率随路径不同（60×，或遮罩下的 360×、1440×），终态逐位相同，都等于本图默认状态"落金初灯"（G2）；
  - `start` 参数只给门禁和测试用，默认取 `newGameStart`；某张图没写 `newGameStart` 时，实例直接建在本图默认状态。
- **第一代**：在交还控制的那一刻写（`SaveReason.NewGame`）。过场途中退出，按 §4.4 第 2 步先跳到终态再写。
- **存档位**：按 §1 选，覆盖已有的存档位要二次确认。

### 5.6 校验修复

各段在 `Restore` 里检查，每修一处都通过 `RestoreContext.Repair()` 记一条日志：

| 情况 | 修复 |
| --- | --- |
| 位置落在楼体里、碰撞检查不过，或在已经不开放的室内（WLD-INTERIORS 的开放清单改了） | 挪到 50 m 内最近的安全点（SYS-WALK §5、SYS-WALK §9）；行走模式下找不到安全点时切到旁观，并提示"这里无法落地" |
| 行走的脚位在水里（旧档，或泳区已不开放、水位数据变了） | 按上一行挪到最近的岸上安全点；室内池挪到本层的池岸安全点，不挪到楼外；读档不发入水、落脚这类事件 |
| 位置超出可达空间，或落在缓冲带里（SYS-VIEW §2.3；WLD-OUTSKIRTS的前沿线锥面、软地板与 3 km 半径） | 按 WLD-OUTSKIRTS的合法化规则就近投影进可达空间，不播推回动画。外围数据更新后截面线、前沿线可能移动，旧档照样能读，只是位姿被投影一次 |
| `NavSpace` 指向的楼或楼层已不开放，或不在那栋楼里 | 按本表第一行"已经不开放的室内"处理；`NavSpace` 改为挪到的那个安全点所在的空间 |
| 手动天气不合本季 | 按换季的替换表替换（SYS-WEATHER §1） |
| 点位、锚点、标记已被删除 | 从已访问、解锁、追踪的列表里去掉 |
| 自定义标记落在地图范围以外，或超过 50 个 | 去掉多出来的 |
| 存档里的地图已不存在 | 回到默认地图的默认出生点（SYS-MAPS） |
| `map:<id>/objects` 里的 `FeatureId` 已不存在（预留） | 丢掉这一条 |
| 共享单车记录里的停放点或槽位已不存在（停放点改了、删了，SYS-WALK §16） | 按 WP-55 的确定迁移挪到登记的备选点；不复制、不丢车，不按显示数量补车 |
| 数值越界：档位、倍率不在合法取值里，钟点不在 [0, 24) | 取最近的合法值 |

## 6 关着游戏的那段现实时间不计

- **读档时不补时间**：读档后 `WorldHours` 就是存档里的值，不按"现在离上次退出过了多久"补算世界时间，也不补算天气。
- **不像动森那样跟现实走，原因有三条**：
  - 再进同一存档时，应该看到离开时的那一刻；
  - 关了一周再回来，世界一下子跳过 168 小时，换季和天气都不受观者控制，不符合"一切切换由观者触发、自然过渡"；
  - 改系统时钟就能操纵世界，没有必要。
- **几种"没在玩"的情况**：
  - 开始页停留：流逝由 `IAppFlow` 冻结（§5.1），不计；
  - 系统睡眠后恢复：第一帧的 dt 被夹到 0.1 s（SYS-TIME §1、ENG-PERF §11.4），等于不计；
  - 窗口失焦：游戏照常在跑，世界照常流逝（UI-NAV §5），这算"开着游戏"。
- **系统时钟只用于显示**：meta 里的 `savedAtUnix` 只用来在存档卡上显示现实日期。读取它是 G4 登记的例外：只能在 `Campus.Kernel.Save` 里读（`Time.GetUnixTimeFromSystem()`），只能写进 meta。

## 7 存档损坏与版本不兼容

读一个存档位时，从最新一代往旧代依次试。第一份同时通过以下检查的就用它：

1. 魔数、长度、代号一致性；
2. 哈希；
3. 解压上限；
4. JSON 解析与深度；
5. 核心段的反序列化。

| 情况 | 判定 | 处理 | 观者看到 |
| --- | --- | --- | --- |
| 最新一代坏了，旧代可读 | 上面五项任一失败 | 坏文件挪进 `corrupt/`，改用同一世界线里最新的可读代；健康状况记为 `RecoveredOlderGen` | 读档后一条临时提示："最近一次保存不完整 · 已回到 5 分钟前"（按两代 meta 的时间差写） |
| 三代全坏 | — | 存档位记为 `Corrupt`；文件先不动，等观者在这个存档位开新游戏时才挪进 `corrupt/` | 存档卡显示"存档已损坏"，可以选"新游戏" |
| 来自更新的版本 | 容器版本、`format` 或任一已登记段的 `v` 比本构建新 | 记为 `NewerBuild`：只读，不能读档，不自动写；在这里开新游戏要二次确认，旧文件挪进 `corrupt/`，文件名加前缀 `newer-` | "来自更新的版本（构建 …）" |
| 缺段 | 文件里没有某个已登记的段 | 用 `CreateDefault` 补上 | — |
| 未知段 | 文件里有本构建没登记的段 | 原样保留，写回时照抄 | — |
| 某一段不合法 | 反序列化失败、缺字段、未知字段、数值越界 | `maps`、`world`、`weather`、`map:*/weather` 这四个核心段不合法，整代视为损坏，退到上一代；其余段只把这一段换成 `CreateDefault`，并记一次修复 | — |
| 另一个进程占着锁 | 打不开 `.lock` | `Enabled = false`，只读 | "另一个窗口正在运行 · 本窗口不保存" |
| 写盘失败 | §4.3 | 删掉这次写的不完整文件，旧代不动；保存状态记下结果和原因（§4.6） | "无法保存：……"，一串连续失败只提示一次；存档页和设置卡持续显示，直到下一次保存成功；退出时按 §4.4 第 7 步三选一 |

- `corrupt/` 里的文件**从不自动删除**。观光档每代只有几 KB，攒多了也不占地方。
- 坏档和新版本的档都不会被自动覆盖，观者明确确认之后才会把它们挪开。

## 8 设置类文件只接受基本类型

适用于 `settings.cfg`（WP-35）、`input.cfg`（WP-06）、`graphics.cfg`（WP-32）和 `audio.cfg`（WP-53a，SYS-AUDIO）。它们是档案级文件，仍用 ConfigFile 格式，读写方各一个（ENG-ARCH §2）。`audio.cfg` 与 `graphics.cfg` 同一做法：开机时音频要比界面更早拿到音量，所以独立成文件，不并进 `settings.cfg`。

问题出在 ConfigFile 的解析器：它支持 `Object(类名, 属性…)` 写法，**在 `Load` 或 `Parse` 返回之前就已经按文件内容创建了对象**。所以光在读出来之后检查类型挡不住，要按下面的顺序来：

1. **先查文本**：把文件读成字符串。只要含有 `Object(`、`Resource(`、`SubResource(`、`ExtResource(` 中的任意一个，整份不用：先退到 `.bak`，`.bak` 也不行就用默认值，并写日志。合法的设置文件里不会出现这些写法。
2. **再解析**：`ConfigFile.Parse(text)`。每个键按登记表取值，只接受 bool、int、float、String 四种类型（`Variant.Type`）。类型不对或越界就用默认值；未知的键忽略。
3. **只存基本类型**：
   - 向量、颜色拆成几个数字存；
   - `input.cfg` 存物理键码（int）和鼠标键号，不存 `InputEvent` 对象。用 ConfigFile 直接存对象，写出来就是 `Object(InputEventKey,…)`，下次读时就落进第 1 条。
4. **写盘**：
   1. `EncodeToText()` 转成 UTF-8；
   2. 写 `<名>.tmp`，调 `FileStream.Flush(true)`；
   3. `File.Replace(tmp, 目标, 目标.bak)`。第一次写、目标还不存在时，用 `File.Move`。
   - 目标被占用时重试 3 次，间隔 50 ms；仍然失败就放弃这次写盘、写日志，内存里的设置照常生效，下次改动或退出时再写。
   - 不用 `ConfigFile.Save`：它走 Godot 的 `FileAccess`，只 `fflush`。也不用 `DirAccess.Rename`（§4.5）。
5. **`engine_override.cfg`**：引擎在任何 C# 代码运行之前就读它，没法先查文本。WP-32 只写登记过的几个键，写法同第 4 条。能改这个文件的只有本机用户自己，风险可以接受。

- 门禁和基准照旧不读任何 `user://` 文件（SYS-GRAPHICS §6）；声音门禁的设置由测试注入，同样不读 `audio.cfg`（SYS-AUDIO）。

## 9 安全：为什么禁止用 Resource 和 ConfigFile 存档

**威胁模型**：存档会被玩家互相分享（论坛、网盘、"全收集存档"），所以读档的输入不可信。读档最多只能把世界摆成一个合法的状态，不能执行代码，不能创建任意对象，也不能耗尽内存。

**实测**

| 写法 | 结果 | 结论 |
| --- | --- | --- |
| `ResourceLoader.Load("user://evil_save.tres", "", CacheMode.Ignore)`，文件里嵌一个 GDScript 子资源，写一个 `_init()` | `_init` 里的打印执行了，日志里出现"PWNED: code inside a save file ran" | Resource（`.tres`、`.res`）不能用来存档。.NET 版引擎照样内置 GDScript 模块，所以工程里不写 GDScript 也挡不住 |
| `ConfigFile.Load`：值写成 `Object(Node,"name":"Injected")`，下一行故意写错 | 返回 `ParseError`，但 Node 已经被创建，退出时报 2 个 ObjectDB 泄漏。嵌入脚本的那一行因为解析中止，这次没有测到（同一解析器支持嵌套对象，按有风险处理） | ConfigFile 不能用来存档；设置文件按 §8 处理 |
| `GD.StrToVar("Object(Node,\"name\":\"X\")")` | 创建了一个 Node | 禁用 |
| Godot 的 `Json.ParseString` | 只得到 Dictionary，没有对象 | 本身安全，但存档统一用 System.Text.Json |
| 社区的 Safe Resource Loader：加载前扫描文件里的脚本 | 黑名单式的补救 | 不用：漏掉一种写法就失守 |

**规则**

1. **存读档只经 `Campus.Kernel.Save`**：正文只用 System.Text.Json 源生成，反序列化到登记过的 DTO 类型（§3.5）；未知段只在容器层按原始字节保留。
2. **G4 禁止的写法**（ENG-ARCH §2）：
   - 全工程：用 `ResourceLoader` 或 `ResourceSaver` 读写 `user://`；
   - 存档代码和各段的实现里：ConfigFile、`GD.StrToVar`、`GD.BytesToVar`、`GD.BytesToVarWithObjects`、`FileAccess.GetVar`、Godot 的 `Json`，以及不带 `JsonTypeInfo` 的 `JsonSerializer` 重载。
3. **资源上限**：
   - 文件不超过 20 MB，meta 不超过 16 KB；
   - 正文压缩后不超过 16 MB，解压后不超过 64 MB。边解压边计数，超了立即停，用来防解压炸弹；
   - JSON 深度不超过 64；
   - 缩略图按 §3.4 的上限。
4. **各段在 `Restore` 里核对内容**：
   - 数组长度：自定义标记不超过 50 个，安全点环正好 16 槽，天气队列不超过 256 条；
   - 数值：都是有限数，钟点在 [0, 24)，流逝倍率是 6 档之一，速度档在 1–16 之间，位置在活动范围内。
5. **SHA-256 只查损坏**：不防修改，也不加密。观光作品里改存档不算作弊；以后做 RPG 也是单机，同样不做防篡改。
6. **存档里不存文件路径，只存 ID**：读档不会去加载任何外部文件，也不联网。

## 10 C# 契约

存档契约类型与签名以 `game/contracts/Save/` 为准。

**未落地示例**（WP-34）：`WorldSaveV1`、`WorldSaveJson`、`WorldStateService` 尚未出现在 `game/contracts/`；落地前保留此处定义。

```csharp
public sealed record WorldSaveV1(double WorldHours, double ClockOffset, double SeasonPos, Season SeasonTarget,
    int FlowRate, int FlowRateBeforePause, double AnimTime);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WorldSaveV1))]
internal partial class WorldSaveJson : JsonSerializerContext { }

[KernelService(typeof(IWorldState), Owner = "WP-34")]
[SaveSection("world", Owner = "WP-34", Version = 1, Phase = RestorePhase.World, Order = 10)]
public sealed partial class WorldStateService : IWorldState, IWorldControl, ISaveSection<WorldSaveV1> { … }
```

## 11 帧顺序：`FramePriority.Save = 1000`

- **位置**：排在流式挂载（`FramePriority.StreamMount` = 900）之后，是每帧的最后一步。
  - 快照要看到本帧所有系统都推进完、本帧该挂的格子也挂完的状态。行走的物理在 `_Process` 之前已经按 120 Hz 补完步（ENG-PERF §6）。
  - 读档时的主线程恢复也放在这一步，下一帧所有系统同时看到新世界。
- **平时几乎不做事**：只累加漫游态的时长和 5 分钟的计时；有请求时检查安全时刻，满足就调各段的 `Capture`，然后把 DTO 交给线程池。
- **缩略图**：分两帧，在这一步里先隐藏界面、下一帧再抓图（§3.4）。
- **不得与别的步骤同值**：Godot 对 `ProcessPriority` 相同的节点按场景树顺序执行，同值就说不清谁先谁后。流式拆成两步：`Stream` 只做选级与请求，`StreamMount` 做挂载与碰撞入树；契约 `FramePriority`（ENG-ARCH §3）、ENG-PERF §5–§6、SYS-TIME §1 与下表一致：

| `ProcessPriority` | 步骤 | 与存档的关系 |
| --- | --- | --- |
| −1300 | 地图（`Map`，WP-46） | 换图途中不是安全时刻，不拍快照（§4.2） |
| −1100 … −600 | 相机 … 介质（SYS-TIME §1） | 本帧推进完之后才拍 |
| −590 | 声学（`Acoustics`，WP-53c，SYS-AUDIO） | 音频不进存档 |
| −500 | 流式（`Stream`）：只做选级与请求 | — |
| 0、50、100 | 系统、音频（`Audio`，WP-53，SYS-AUDIO）、界面 | — |
| 900 | 流式挂载（`StreamMount`）：挂载与碰撞入树，预算内分帧 | 读档后的就绪屏障看它（§5.1 第 6 步） |
| **1000** | **存档（`Save`）** | 拍快照；读档时在这一步恢复 |

  音频的两步 `Acoustics = −590`、`Audio = 50` 都在存档之前，也不与任何一步同值（SYS-AUDIO）。

## 12 命令行

参数登记进 ENG-GATES §1、SYS-VIEW §2.5，由 WP-01 解析，WP-50 落实。

| 参数 | 默认 | 含义 |
| --- | --- | --- |
| `--save=off\|<n>` | 正常启动：写回读进来的存档位；`--gate`、`--shots`、`--bench`、`--frames` 下为 `off` | 只管写：`off` 时不写、不挪任何文件，也不拿锁；`<n>` 取 1–3，表示自动保存写到 `s<n>`。读档由 `--slot`、`--load` 决定。`--save=off` 时开始页照常只读地列出存档 |
| `--slot=<n>` | 无 | 启动时读 `s<n>`：配 `--title=0` 时读完直接进入漫游；配 `--title=1` 时，开始页的"继续"指向它（SYS-TITLE）。这个存档位为空、损坏或来自更新的版本时，以非零退出码结束 |
| `--load=<绝对路径>` | 无 | 读一份指定的 `.sav`（金档、复现用的档），源文件只读。之后写到 `--save=<n>`，没给就不写。与 `--slot` 同时给出时，以非零退出码结束 |
| `--save-dir=<绝对路径>` | `user://saves` | 换存档的根目录：门禁、金档测试、开发时多开。目录不存在就新建。开发态的 `user://` 是 `UESTC_DEV`（ENG-ARCH §6） |
| `--save-at=<帧号>` | 无 | 门禁用：从这一帧起，在下一个安全时刻调 `SaveNow(SaveReason.Test)`（G20 的续跑等价） |
| `--weather-seed=<u32>` | 截图时为 1 | 只作用于新游戏（SYS-WEATHER §6.4） |

- **读档时世界状态只来自存档**：`--slot` 或 `--load` 与 `--clock`、`--season`、`--anim`、`--flow`、`--weather*`、`--wet`、`--pose`、`--mode`、`--map`、`--spawn` 中的任何一个同时给出，都以非零退出码结束。`--ui`、`--quality*`、`--adaptive`、`--frames`、`--shots`、`--out` 照常可用。
- **截图默认串**：末尾加 `--map=qingshuihe --title=0 --save=off`，与 SYS-TITLE、SYS-MAPS的同一条合并，只加一次；与 ENG-GATES §1 合并时一并加 SYS-AUDIO的 `--audio=off`，即末尾是 `--map=qingshuihe --title=0 --save=off --audio=off`。

## 13 性能实测

环境：Godot 4.7.2.stable.mono.official.ed1daf0bf、RTX 4080、NVMe、Windows 11。耗时取多次运行的中位数，括号里是最大值。

**序列化、压缩、写盘与读档**（"RPG 中等"按 2000 个标志位加 500 件物品构造，"RPG 大"按 2 万个标志位加 5000 件物品构造）

| 规模 | JSON | Brotli 最快档 | Brotli 最优档 | gzip 最快档 | 序列化 | SHA-256 | 写盘：tmp + `Flush(true)` + Move | tmp + `Flush(true)` + Replace | 不 fsync + Move | 读档全程（读、哈希、解压、解析、反序列化） |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 观光 | 2465 B | 914 B · 0.011 ms | 839 B · 0.040 ms | 1094 B · 0.007 ms | 0.029 ms（0.100）；**第一次 29.5 ms，是 JIT** | 0.0005 ms | 1.36 ms（1.55） | 2.22 ms（3.28） | 0.97 ms（1.15） | 0.39 ms（7.7，最大值出现在第一次，含 JIT） |
| RPG 中等 | 104 KB | 14.0 KB · 0.25 ms | 8.9 KB · 0.59 ms | 22.7 KB · 0.10 ms | 0.47 ms（2.08） | 0.005 ms | 1.31 ms（2.62） | 7.11 ms（8.99） | 0.98 ms（1.50） | 3.9 ms（6.7） |
| RPG 大 | 1.05 MB | 107 KB · 1.25 ms | 61 KB · 5.16 ms | 212 KB · 1.11 ms | 4.5 ms（6.3） | 0.04 ms | 1.48 ms（2.09） | 7.54 ms（8.72） | 1.07 ms（1.52） | 16 ms（44） |

- **往返**：world、view、天气队列的 record 存了再读全部相等，`WorldHours` 逐位一致；10 万个随机 double 存了再读，不一致 0 个；float 的 0.1 + 0.2 往返后仍相等。
- **不用 MessagePack**：观光档才 2.5 KB，不值得多引一个依赖。

**缩略图**（窗口模式下的测试场景）

| 源分辨率 | `GetImage()`（主线程） | Lanczos → 384×216 | WebP 有损 0.8 | PNG | 放到后台的缩放加编码合计 |
| --- | --- | --- | --- | --- | --- |
| 1920×1080 | 第一次 9.1–9.5 ms，之后 3.8–4.1 ms | 22.5–24.4 ms | 1244 B · 4.3–5.6 ms | 16040 B · 3.6–4.1 ms | 29.2–29.7 ms |
| 2560×1440 | 第一次 12.9 ms，之后 6.6–7.1 ms | 37.5–40.7 ms | 1262 B · 4.4–5.4 ms | 12198 B · 2.8–3.0 ms | 45.5 ms |

- **异步回读**：`TextureGetDataAsync` 调用本身 2.17 ms，下一帧返回 8,294,400 B；`CreateFromData` 1.39 ms；连做 3 次 `ShrinkX2` 得到 240×135，用时 1.78 ms；WebP 1088 B，2.46 ms。

**退出**（写盘用 `Thread.Sleep(400)` 模拟，让时序看得清楚）

| 写法 | 从关闭请求到结束 | 那几帧 | 文件 |
| --- | --- | --- | --- |
| 默认 `AutoAcceptQuit` + 后台任务 | 34 ms 后进程就退出了 | — | 没写出来 |
| 在通知回调里同步写 | 402 ms | 那一帧 437 ms | 写成 |
| `AutoAcceptQuit = false` + 后台写 + `Quit()` | 433 ms | 每帧 30–37 ms，照常渲染 | 写成 |

**改名**：`DirAccess.RenameAbsolute` 覆盖已有文件返回 Ok（但按源码不是原子操作）；`File.Move` 覆盖可以成功；目标被别的句柄以 `FileShare.Read` 或 `Read | Delete` 打开时，`File.Move` 报 `UnauthorizedAccessException`。

**由此定的预算**（写进 ENG-BUDGET §8，G10 检查）：

| 项 | 预算 |
| --- | --- |
| 快照主线程（全部段的 `Capture` 合计） | p99 ≤ 0.3 ms |
| 缩略图那一帧主线程 | 1080p ≤ 5 ms，1440p ≤ 8 ms；只在退出、返回标题、失焦、新游戏时出现 |
| 后台一次保存（序列化、压缩、缩略图、哈希、写盘、校验） | 观光档 ≤ 50 ms |
| 读档的主线程恢复 | 观光档 ≤ 2 ms（初值，§17） |
| 从关闭请求到进程结束 | ≤ 1.5 s（含 0.3 s 雾幕），硬上限 2 s |
| 内存 | 峰值 < 1 MB（观光档） |
| 磁盘 | 每代约 3 KB，3 个存档位 × 3 代约 30 KB |

## 14 验收：G20

门禁 G20（ENG-GATES §2）。测试由 WP-50 交付，WP-27 接进 `scripts\gate.ps1`。

- **纯 .NET 测试**：容器编解码、校验、迁移链写成不碰 GodotSharp 的纯 C# 类，放在 `game/kernel/save/format/`，用 `dotnet test` 直接跑，不启动引擎。
- **需要引擎的项**：标 `[RequireGodotRuntime]`，或由脚本启动导出版来跑。

| 项 | 做法 | 判据 |
| --- | --- | --- |
| **往返** | 每段用带种子的生成器造 1000 个随机 DTO，走一遍容器的写和读；引擎里随机操作 30 s，存档后立即在同一进程里读回，再 `Capture` 一次 | 全部字段相等，double、float 逐位相等；第二次 `Capture` 与存进去的相同（`Instance` 除外） |
| **续跑等价** | 两次启动，都用 `--fixed-fps 60`：<br>A：新游戏 → 1440× 前推 72 h → `--save-at` 存档 → 再跑 24 h 世界时间；<br>B：`--load` A 的存档 → 跑同样的帧数。<br>另做三组变体：60× 和 1× 各跑 10 分钟，以及在自动天气的自然演化中途存档 | 世界（`WorldHours`、钟点、季节、流逝、`AnimTime`）和天气（选择、天气向量、前瞻队列）逐帧逐位一致；偶发事件按设计重新计时，不比 |
| **迁移** | `game/kernel/save/tests/golden/` 下的每份金档都读一遍；每一步 `Migrate(v)` 单独测；迁移后存一次、再读一次 | 都能读；迁移结果与期望的 JSON 一致；第二次往返不变 |
| **损坏 1000 例** | 随机截断（含 0 字节、截在头里、截在 meta 里）、翻 1–8 位、空文件、错魔数、长度不一致、代号不一致、Brotli 解压炸弹（超过 64 MB）、JSON 深度 65 和 1000、未知段、新版本段、新容器版本、新 `format`、超长数组（标记 10⁶ 个）、越界数值、类型错误、重复键、缩略图乱码 | 不崩溃、没有异常漏出；每例 ≤ 100 ms，内存峰值比基线多不超过 128 MB；健康状况判对；有旧代时退到旧代；`NewerBuild` 从不被覆盖；文件只挪进 `corrupt/`，从不删除 |
| **退出时保存** | 并进 G5 的窗口冒烟：<br>1. `--save=1 --save-dir=<临时目录>`，流逝暂停，截图 S1；<br>2. 向窗口发 WM_CLOSE；<br>3. 用 `--slot=1 --title=0 --save=off` 重新启动，截图 S2。<br>另测：鼠标被捕获时的 Alt+F4 | 2 s 内进程退出；新一代通过校验；S2 与 S1 一致（G7 的像素容差） |
| **写盘途中强杀** | 每 2 s 强制保存一次，在写盘途中随机 `taskkill /F`，重复 100 次 | 每次重启后存档位都是 `Ok` 或 `RecoveredOlderGen`，回退不超过 1 代 |
| **保存耗时** | F1 路线循环 10 分钟，每 10 s 强制保存一次，并含 3 次带缩略图的保存 | 快照主线程 p99 ≤ 0.3 ms；缩略图那一帧 ≤ 5 ms（1080p）；从关闭请求到进程结束 ≤ 1.5 s；第 2 代 GC 0 次；保存不增加管线编译计数（G16） |
| **安全** | G4 的静态规则（§9）；运行时：把实验里的恶意 `.tres` 改名成 `.sav` 读一次，再分别读一份恶意的 `settings.cfg`、`input.cfg`、`graphics.cfg`、`audio.cfg`（`audio.cfg` 与 SYS-AUDIO G21e 的设置文件安全测试是同一套） | 读前读后 `Performance.Monitor.ObjectCount` 不变；日志里没有被执行的输出；都按"损坏"或"退回默认值"处理 |
| **读档顺序与预览** | 引擎里：`Preview` 读档后核对 `Pose` 阶段的段没有被恢复、`HasPendingPose` 为真；`CompleteResume` 后再 `Capture` 一次，与 `Full` 读同一份存档的结果相同；用门禁夹具图（SYS-MAPS，若做）存一份"当前图不是默认图"的档，读它 | `World` 阶段先于换图恢复，换图后的天气补算量为零；`Preview` + `CompleteResume` 与 `Full` 逐位相同；`CompleteResume` 不改 `Instance` |
| **水中保存** | 在 P0 样板的湖与游泳馆池里：连续游泳超过 60 s 等定时保存；水中失焦、退出、返回标题各存一次；再分别读回 | 60 s 内必有一代写成，不无限顺延；`view`、`walk`、`maps` 三段与 meta 指向同一个岸上安全点，室内池的落在池岸、不在楼外；读回后站在岸上，没有泳态、旧水体和入水事件，等碰撞就绪才开物理 |
| **骑行中保存** | 在 P0 功能样板上：骑行中、临停后步行时各存一次，骑行中失焦、退出各存一次；再分别读回 | `view`、`walk`、`maps` 三段与 meta 指向同一个合法的步行站位；读回是步行，借的车回到来源回收槽，各停放点的库存不多不少；没有骑姿、速度与相位；保存前后正在玩的人和车没被挪动 |
| **锁与开关** | 同时开两个进程；门禁模式跑一整轮 | 第二个进程 `Enabled = false`，一个文件都不写；`--save=off` 下 `user://saves` 不被创建、不被修改 |
| **保存失败时退出** | `--save=1 --save-dir=<临时目录>`，用测试构建的故障注入让写盘抛"磁盘已满"，再发 WM_CLOSE；三个选项各走一遍（重试前先撤掉注入） | 进程不静默退出，出现三选一，原因写对；重试后照常退出、新一代通过校验；返回游戏后回到 Playing，`State` 仍记着失败；仍然退出后旧代完好可读 |

- **何时运行**：
  - 核心专项：改动 `game/kernel/save`、`Campus.Contracts.Save`，或者带 `[SaveSection]` 的包时，跑往返、损坏、退出时保存；
  - 集中测试全量：再加迁移、续跑等价、强杀 100 次、保存耗时（ENG-GATES §2）。
- **一票否决**（ENG-GATES §3）：
  - 丢档：正常退出后再进，状态不是退出时的落定状态；
  - 读档时执行了存档里的任何代码，或创建了存档内容指定的对象；
  - 存档被覆盖或删除，导致无法回退到上一代；
  - 来自更新版本的存档被旧构建覆盖。
- **回报行**：完成回报的"存档"一行写本包的存档段是否已登记、G20 是否通过，以及段的版本号和有没有迁移函数；没有存档段时写"不涉及"。

## 15 工作包

包的独占目录与交付以 [ENG-WP](../engineering/ENG-WP-工作包派工单.md) 为准。

## 17 待验证（P0）

| 待验证 | 验证前用的初值 | 验证方法 · 负责 |
| --- | --- | --- |
| 缩略图分两帧抓（§3.4） | 第 N 帧在存档那一步隐藏界面根节点，第 N+1 帧 `GetImage()` 拿到第 N 帧不带界面的画面 | P0 在引擎里核对；WP-50 |
| 源生成下"缺字段要报错"（§3.5） | 字段标为必需 | P0 核对 .NET 10 源生成下的行为；WP-50 |
| 全工程关闭反射式 JSON（§3.5） | 不设 `JsonSerializerIsReflectionEnabledByDefault=false` | 全工程的 JSON 都改成源生成之后由 WP-00 决定 |
| 读档的主线程恢复（§13） | 观光档 ≤ 2 ms | P0 实测；WP-50 |
| 系统关机、注销时的保存（§4.4） | 不处理 WM_QUERYENDSESSION，靠每 5 分钟的定时保存兜底 | P2 评估是否子类化窗口过程、接住后同步保存；WP-01、WP-50 |
