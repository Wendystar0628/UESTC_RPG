<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeFrame

命名空间 `Godot`；class；继承 [`VisualShaderNodeResizableBase`](VisualShaderNodeResizableBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A rectangular frame that can be used to group visual shader nodes together to improve organization.

Nodes attached to the frame will move with it when it is dragged and it can automatically resize to enclose all attached nodes.

Its title, description and color can be customized.

## 构造函数

### VisualShaderNodeFrame

```csharp
public VisualShaderNodeFrame()
```

## 属性

### AttachedNodes

```csharp
public int[] AttachedNodes { get; set; }
```

引擎名：`attached_nodes`

The list of nodes attached to the frame.

### Autoshrink

```csharp
public bool Autoshrink { get; set; }
```

引擎名：`autoshrink`

If `true`, the frame will automatically resize to enclose all attached nodes.

### TintColor

```csharp
public Color TintColor { get; set; }
```

引擎名：`tint_color`

The color of the frame when `TintColorEnabled` is `true`.

### TintColorEnabled

```csharp
public bool TintColorEnabled { get; set; }
```

引擎名：`tint_color_enabled`

If `true`, the frame will be tinted with the color specified in `TintColor`.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

The title of the node.

## 方法

### AddAttachedNode

```csharp
public void AddAttachedNode(int node)
```

引擎名：`add_attached_node`

Adds a node to the list of nodes attached to the frame. Should not be called directly, use the [`VisualShader.AttachNodeToFrame`](VisualShader.md) method instead.

### GetAttachedNodes

```csharp
public int[] GetAttachedNodes()
```

引擎名：`get_attached_nodes`

### GetTintColor

```csharp
public Color GetTintColor()
```

引擎名：`get_tint_color`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### IsAutoshrinkEnabled

```csharp
public bool IsAutoshrinkEnabled()
```

引擎名：`is_autoshrink_enabled`

### IsTintColorEnabled

```csharp
public bool IsTintColorEnabled()
```

引擎名：`is_tint_color_enabled`

### RemoveAttachedNode

```csharp
public void RemoveAttachedNode(int node)
```

引擎名：`remove_attached_node`

Removes a node from the list of nodes attached to the frame. Should not be called directly, use the [`VisualShader.DetachNodeFromFrame`](VisualShader.md) method instead.

### SetAttachedNodes

```csharp
public void SetAttachedNodes(ReadOnlySpan<int> attachedNodes)
```

引擎名：`set_attached_nodes`

### SetAttachedNodes

```csharp
public void SetAttachedNodes(int[] attachedNodes)
```

引擎名：`set_attached_nodes`

### SetAutoshrinkEnabled

```csharp
public void SetAutoshrinkEnabled(bool enable)
```

引擎名：`set_autoshrink_enabled`

### SetTintColor

```csharp
public void SetTintColor(Color color)
```

引擎名：`set_tint_color`

### SetTintColorEnabled

```csharp
public void SetTintColorEnabled(bool enable)
```

引擎名：`set_tint_color_enabled`

### SetTitle

```csharp
public void SetTitle(string title)
```

引擎名：`set_title`
