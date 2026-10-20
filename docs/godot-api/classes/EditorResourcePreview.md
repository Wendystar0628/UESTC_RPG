<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorResourcePreview

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This node is used to generate previews for resources or files.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetResourcePreviewer`](EditorInterface.md).

## 方法

### AddPreviewGenerator

```csharp
public void AddPreviewGenerator(EditorResourcePreviewGenerator generator)
```

引擎名：`add_preview_generator`

Create an own, custom preview generator.

### CheckForInvalidation

```csharp
public void CheckForInvalidation(string path)
```

引擎名：`check_for_invalidation`

Check if the resource changed, if so, it will be invalidated and the corresponding signal emitted.

### QueueEditedResourcePreview

```csharp
public void QueueEditedResourcePreview(Resource resource, GodotObject receiver, StringName receiverFunc, Variant userdata)
```

引擎名：`queue_edited_resource_preview`

Queue the `resource` being edited for preview. Once the preview is ready, the `receiver`'s `receiverFunc` will be called. The `receiverFunc` must take the following four arguments: `System.String` path, [`Texture2D`](Texture2D.md) preview, [`Texture2D`](Texture2D.md) thumbnail_preview, [`Variant`](Variant.md) userdata. `userdata` can be anything, and will be returned when `receiverFunc` is called.

**Note:** If it was not possible to create the preview the `receiverFunc` will still be called, but the preview will be `null`.

### QueueResourcePreview

```csharp
public void QueueResourcePreview(string path, GodotObject receiver, StringName receiverFunc, Variant userdata)
```

引擎名：`queue_resource_preview`

Queue a resource file located at `path` for preview. Once the preview is ready, the `receiver`'s `receiverFunc` will be called. The `receiverFunc` must take the following four arguments: `System.String` path, [`Texture2D`](Texture2D.md) preview, [`Texture2D`](Texture2D.md) thumbnail_preview, [`Variant`](Variant.md) userdata. `userdata` can be anything, and will be returned when `receiverFunc` is called.

**Note:** If it was not possible to create the preview the `receiverFunc` will still be called, but the preview will be `null`.

### RemovePreviewGenerator

```csharp
public void RemovePreviewGenerator(EditorResourcePreviewGenerator generator)
```

引擎名：`remove_preview_generator`

Removes a custom preview generator.

## 事件（信号）

### PreviewInvalidated

```csharp
public event EditorResourcePreview.PreviewInvalidatedEventHandler PreviewInvalidated
```

引擎名：`preview_invalidated`

Emitted if a preview was invalidated (changed). `path` corresponds to the path of the preview.

## PreviewInvalidatedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorResourcePreview.PreviewInvalidated`](EditorResourcePreview.md) event of a [`EditorResourcePreview`](EditorResourcePreview.md) class.

### 签名

#### Invoke

```csharp
delegate void PreviewInvalidatedEventHandler(string path)
```
