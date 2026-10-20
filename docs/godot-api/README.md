# Godot C# API 参考（Godot 4.7.2 .NET 版）

本目录是项目锁定的引擎版本的完整 API 参考，由命令行从引擎本身导出，再生成按类拆分的 Markdown。开发代理写 C# 代码时，**以这里的签名为准**，不要凭记忆写 Godot API。模型的记忆里混着 Godot 3 和 4 的旧写法，这是最常见的错误来源。

- **引擎**：Godot Engine v4.7.2.stable.mono.official（`ed1daf0bf`），2026-08-18 发布。单精度构建。
- **许可**：本目录的签名、类说明和 `raw/` 下的导出来自 Godot 引擎，按 MIT 许可（© Godot Engine contributors，Juan Linietsky、Ariel Manzur）；`shaders/` 来自 godot-docs，按 CC BY 3.0（见该目录的 README）。
- **本机路径**：`E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\`，已开启自包含模式（同目录有 `_sc_` 文件，编辑器设置存在这个目录里）。
  - 编辑器：`Godot_v4.7.2-stable_mono_win64.exe`
  - 命令行（带控制台输出，脚本和门禁用这个）：`Godot_v4.7.2-stable_mono_win64_console.exe`
- **来源**：[官方发布页](https://github.com/godotengine/godot/releases/tag/4.7.2-stable)的 `Godot_v4.7.2-stable_mono_win64.zip`（116.6 MB）。SHA-512 已与官方 `SHA512-SUMS.txt` 核对一致。
- **.NET**：本机已有 .NET SDK 8.0.201。Godot 4.7.2 的 C# 项目默认目标是 net8.0。

## 怎么用

| 要做什么 | 看哪里 |
| --- | --- |
| 找一个类、看它是干什么的 | [index.md](index.md)：按类别列出全部 1151 个顶层类型，每个附一句简介；开头有"本项目常用速查" |
| 查一个类的完整成员 | `classes/<类名>.md`，例如 [classes/Node3D.md](classes/Node3D.md)、[classes/RenderingServer.md](classes/RenderingServer.md) |
| 确认某个方法、属性到底存在不存在、参数怎么写 | 在 [signatures.txt](signatures.txt) 里搜。每行一个成员：`<类型全名> :: <C# 签名>`，共 33065 行 |
| 读官方英文文档、对照 GDScript 示例 | 每个成员下的"引擎名"就是官方文档里的 snake_case 名字 |
| 比对两个引擎版本的 API 差异 | `raw/extension_api.json`，配合 `--validate-extension-api` |
| 写着色器：内置变量、内置函数、render_mode、全局和实例 uniform、Compositor | [shaders/README.md](shaders/README.md)（godot-docs 4.7 分支的离线副本） |

检索示例：

```powershell
Select-String -Path docs\godot-api\signatures.txt -Pattern 'Godot.MultiMesh ::'
Select-String -Path docs\godot-api\signatures.txt -Pattern 'GlobalShaderParameter'
```

## C# 命名规则

GodotSharp 把引擎 API 的名字按固定规则转成 C#：

| 引擎（GDScript / 官方文档） | C# |
| --- | --- |
| 方法 `get_global_position()`、`look_at()` | 方法 `LookAt()`。有对应属性的 getter/setter 通常直接用属性：`GlobalPosition` |
| 属性 `global_position` | 属性 `GlobalPosition` |
| 信号 `visibility_changed` | 事件 `VisibilityChanged`；按名字连接时用 `SignalName.VisibilityChanged` |
| 常量、枚举值 `NOTIFICATION_READY`、`SHADOW_PARALLEL_4_SPLITS` | `Node.NotificationReady`、`DirectionalLight3D.ShadowMode.Parallel4Splits`。枚举名和属性名冲突时，枚举名加 `Enum` 后缀，例如 `OpTypeEnum` |
| 单例 `RenderingServer.xxx()` | 静态类 `RenderingServer.Xxx()` |
| `Object` | `GodotObject` |
| `@GlobalScope` 的数学函数 `deg_to_rad` | `Mathf.DegToRad`；打印、随机数等在 `GD` 里 |
| `PackedVector3Array`、`Array`、`Dictionary` | `Vector3[]`、`Godot.Collections.Array`、`Godot.Collections.Dictionary` |
| `float` 参数 | 单精度构建下是 `float`；内置数学类型的分量是 `float` |

每个类页面里，还会列出 `MethodName`、`PropertyName`、`SignalName` 三个嵌套类。它们只是按名字调用时用的 `StringName` 常量，本参考不单独列出。

## 已知局限

- **说明文字是英文**：来自引擎自带的官方文档注释。生成脚本已经把常见的 GDScript 写法换成了 C# 写法（例如 `Vector3.ZERO` → `Vector3.Zero`、`@GlobalScope.deg_to_rad` → `Mathf.DegToRad`），但个别句子仍可能保留 GDScript 名字。**写代码时以签名为准。**
- **着色语言单独放**：spatial、sky、compute 着色器的内置变量和函数不在类 API 里，见 [shaders/README.md](shaders/README.md)。那里是官方 godot-docs 4.7 分支着色器文档的离线副本。
- **`--doctool` 不可用**：它导出的 XML 只有结构、没有说明文字（它的用途是合并更新引擎源码树里的文档），所以没有采用。

## 目录

```
docs/godot-api/
├─ README.md                 本文件
├─ index.md                  全部类型的分类索引（生成）
├─ classes/<类名>.md          每个顶层类型一页，嵌套枚举和类并在所属页里（生成）
├─ signatures.txt            全部成员签名，一行一个（生成）
├─ shaders/                  着色器文档离线副本（godot-docs 4.7 分支原文 .rst）与索引 README
└─ raw/
   ├─ extension_api.json            godot --dump-extension-api
   ├─ extension_api_with_docs.json  godot --dump-extension-api-with-docs（带英文说明）
   └─ csharp/
      ├─ api.json                   ApiDump 用反射从 GodotSharp.dll / GodotSharpEditor.dll 导出的签名
      ├─ GodotSharp.xml             .NET 版自带的 C# 文档注释
      └─ GodotSharpEditor.xml
```

`classes/`、`index.md`、`signatures.txt` 都是生成物，不要手改；要改就改生成脚本。

## 重新生成（升级引擎版本时）

```powershell
$G = 'E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$R = 'E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\GodotSharp\Api\Release'
$Raw = 'E:\Project\UESTC_RPG\docs\godot-api\raw'

# 1. 引擎 API（JSON）。这两个命令把文件写到当前目录，所以先切到一个空目录
Push-Location $env:TEMP
& $G --headless --dump-extension-api;           Move-Item -Force extension_api.json "$Raw\extension_api.json"
& $G --headless --dump-extension-api-with-docs; Move-Item -Force extension_api.json "$Raw\extension_api_with_docs.json"
Pop-Location

# 2. C# 签名与文档注释
Copy-Item "$R\GodotSharp.xml", "$R\GodotSharpEditor.xml" "$Raw\csharp\"
dotnet run -c Release --project E:\Project\UESTC_RPG\scripts\godot-api\ApiDump -- "$R\GodotSharp.dll" "$R\GodotSharpEditor.dll" "$Raw\csharp\api.json"

# 3. 生成 Markdown
python -I E:\Project\UESTC_RPG\scripts\godot-api\gen-api-docs.py
```

工具源码：`scripts/godot-api/ApiDump/`（C# 反射导出器）、`scripts/godot-api/gen-api-docs.py`（Markdown 生成器）。
