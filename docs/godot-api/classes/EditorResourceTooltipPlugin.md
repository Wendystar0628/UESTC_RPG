<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorResourceTooltipPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Resource tooltip plugins are used by [`FileSystemDock`](FileSystemDock.md) to generate customized tooltips for specific resources. E.g. tooltip for a [`Texture2D`](Texture2D.md) displays a bigger preview and the texture's dimensions.

A plugin must be first registered with [`FileSystemDock.AddResourceTooltipPlugin`](FileSystemDock.md). When the user hovers a resource in filesystem dock which is handled by the plugin, `_MakeTooltipForPath()` is called to create the tooltip. It works similarly to [`Control._MakeCustomTooltip()`](Control.md).

## 构造函数

### EditorResourceTooltipPlugin

```csharp
public EditorResourceTooltipPlugin()
```

## 方法

### RequestThumbnail

```csharp
public void RequestThumbnail(string path, TextureRect control)
```

引擎名：`request_thumbnail`

Requests a thumbnail for the given [`TextureRect`](TextureRect.md). The thumbnail is created asynchronously by [`EditorResourcePreview`](EditorResourcePreview.md) and automatically set when available.

### _Handles

```csharp
public virtual bool _Handles(string type)
```

引擎名：`_handles`

Return `true` if the plugin is going to handle the given [`Resource`](Resource.md) `type`.

### _MakeTooltipForPath

```csharp
public virtual Control _MakeTooltipForPath(string path, Dictionary metadata, Control base)
```

引擎名：`_make_tooltip_for_path`

Create and return a tooltip that will be displayed when the user hovers a resource under the given `path` in filesystem dock.

The `metadata` dictionary is provided by preview generator (see [`EditorResourcePreviewGenerator._Generate()`](EditorResourcePreviewGenerator.md)).

`base` is the base default tooltip, which is a [`VBoxContainer`](VBoxContainer.md) with a file name, type and size labels. If another plugin handled the same file type, `base` will be output from the previous plugin. For best result, make sure the base tooltip is part of the returned [`Control`](Control.md).

**Note:** It's unadvised to use [`ResourceLoader.Load`](ResourceLoader.md), especially with heavy resources like models or textures, because it will make the editor unresponsive when creating the tooltip. You can use `RequestThumbnail()` if you want to display a preview in your tooltip.

**Note:** If you decide to discard the `base`, make sure to call [`Node.QueueFree`](Node.md), because it's not freed automatically.

```csharp
func _make_tooltip_for_path(path, metadata, base):
var t_rect = TextureRect.new()
request_thumbnail(path, t_rect)
base.add_child(t_rect) # The TextureRect will appear at the bottom of the tooltip.
return base

```
