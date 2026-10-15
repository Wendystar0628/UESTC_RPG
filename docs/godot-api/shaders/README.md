# Godot 4.7 着色器文档（离线副本）

C# 类 API 里没有着色语言的内置变量和函数，这里补上。内容是官方文档仓库 [godotengine/godot-docs](https://github.com/godotengine/godot-docs) **4.7 分支**的原始 reStructuredText 文件，没有改动。

- **来源提交**：`9adca4c1c72917bfe1b7be3108abed5ce26696a6`（2026-10-15 下载，见 `rst/_commit.txt`）
- **范围**：`tutorials/shaders/` 下的全部 `.rst`（图片除外），加上与本项目直接相关的两篇：`tutorials/rendering/compositor.rst`、`tutorials/performance/pipeline_compilations.rst`。共 27 篇，约 574 KB。清单见 `rst/_list.txt`。
- **许可**：godot-docs 的文字采用 CC BY 3.0，作者为 Juan Linietsky、Ariel Manzur 和 Godot 社区。这里只在仓库内部作参考，不再分发。

## 先读哪些

| 优先 | 文件 | 内容 | 本项目用在哪 |
| --- | --- | --- | --- |
| ★ | [spatial_shader.rst](rst/tutorials/shaders/shader_reference/spatial_shader.rst) | 3D 材质着色器：render_mode、vertex/fragment/light 三个函数的全部内置变量 | 三渲二材质、植被、水、剖切色（01、10、11） |
| ★ | [shading_language.rst](rst/tutorials/shaders/shader_reference/shading_language.rst) | 着色语言本身：类型、uniform 提示、varying、全局 uniform、实例 uniform、数组、结构体 | 全部着色器 |
| ★ | [shader_functions.rst](rst/tutorials/shaders/shader_reference/shader_functions.rst) | 内置函数全表 | 全部着色器 |
| ★ | [shader_preprocessor.rst](rst/tutorials/shaders/shader_reference/shader_preprocessor.rst) | `#include`、`#define`、条件编译 | `toon.gdshaderinc` 等公共代码 |
| ★ | [sky_shader.rst](rst/tutorials/shaders/shader_reference/sky_shader.rst) | 天空着色器 | 天空、星空、银河、日月盘（02、23） |
| ★ | [compositor.rst](rst/tutorials/rendering/compositor.rst) | CompositorEffect：在渲染管线里插入自定义计算通道 | 描线、边缘光、移轴、光斑（01 §01.4–01.7） |
| ★ | [compute_shaders.rst](rst/tutorials/shaders/compute_shaders.rst) | 计算着色器与 RenderingDevice | 同上；调色板等 GPU 侧处理 |
| ★ | [pipeline_compilations.rst](rst/tutorials/performance/pipeline_compilations.rst) | 管线编译、ubershader、什么操作会引起运行时编译 | 着色器白名单、雾幕预热、管线编译门禁（20、22） |
| ○ | [advanced_postprocessing.rst](rst/tutorials/shaders/advanced_postprocessing.rst) | 全屏四边形后处理、从深度重建位置 | 介质呈现的全屏覆盖层（14 §14.5） |
| ○ | [screen-reading_shaders.rst](rst/tutorials/shaders/screen-reading_shaders.rst) | 读屏幕、深度、法线粗糙度纹理的提示 | 透明水面变体（11） |
| ○ | [using_viewport_as_texture.rst](rst/tutorials/shaders/using_viewport_as_texture.rst) | 把 SubViewport 当纹理用 | 西湖平面倒影（22 §22.10.4） |
| ○ | [particle_shader.rst](rst/tutorials/shaders/shader_reference/particle_shader.rst) | 粒子着色器 | 只作备选：飘落物、雪、萤火虫默认用 MultiMesh（13） |
| ○ | [canvas_item_shader.rst](rst/tutorials/shaders/shader_reference/canvas_item_shader.rst) | 2D 与界面着色器 | 界面的水彩斑驳、雾幕（24） |
| ○ | [shaders_style_guide.rst](rst/tutorials/shaders/shaders_style_guide.rst) | 官方着色器代码风格 | 代码风格 |
| — | fog_shader.rst、texture_blit_shader.rst、visual_shaders.rst、making_trees.rst、custom_postprocessing.rst、converting_glsl_to_godot_shaders.rst、introduction_to_shaders.rst、your_first_shader/ | 体积雾着色器、贴图拷贝、可视化着色器、入门教程等 | 本项目基本用不到（不用体积雾，不用可视化着色器） |

## 本项目最依赖的几条事实（已在原文核对）

| 事实 | 出处 |
| --- | --- |
| 阴影通道里 `INV_VIEW_MATRIX` 是灯的视角；按相机距离做判断要用 `MAIN_CAM_INV_VIEW_MATRIX` | spatial_shader.rst 的 vertex 内置变量表及其后的注释 |
| `IN_SHADOW_PASS` 在阴影通道里为真 | spatial_shader.rst 的内置变量表 |
| `light()` 里的 `ATTENUATION` 是距离衰减或阴影；`LIGHT_COLOR` 已乘了 π，官方示例写成 `... * LIGHT_COLOR / PI` | spatial_shader.rst 的 light 内置变量表与示例 |
| varying 可以从 `fragment()` 传到 `light()`，但不能在自定义函数和 `light()` 里赋值 | shading_language.rst 的 Varyings 一节 |
| 全局 uniform 对所有着色器类型可用；全局数组之外，实例 uniform 不支持纹理和数组，每个着色器最多 16 个 | shading_language.rst 的 Global uniforms、Per-instance uniforms 两节 |

## 更新

升级引擎小版本时，把 `rst/_list.txt` 里的文件按新分支重新下载：

```powershell
$Sha = (Invoke-RestMethod https://api.github.com/repos/godotengine/godot-docs/commits/4.7).sha
Get-Content docs\godot-api\shaders\rst\_list.txt | ForEach-Object {
  $out = "docs\godot-api\shaders\rst\$_"
  New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
  Invoke-WebRequest "https://raw.githubusercontent.com/godotengine/godot-docs/$Sha/$_" -OutFile $out
}
$Sha | Set-Content docs\godot-api\shaders\rst\_commit.txt
```
