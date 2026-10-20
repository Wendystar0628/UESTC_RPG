<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasGroup

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Child [`CanvasItem`](CanvasItem.md) nodes of a `CanvasGroup` are drawn as a single object. It allows to e.g. draw overlapping translucent 2D nodes without causing the overlapping sections to be more opaque than intended (set the [`CanvasItem.SelfModulate`](CanvasItem.md) property on the `CanvasGroup` to achieve this effect).

**Note:** The `CanvasGroup` uses a custom shader to read from the backbuffer to draw its children. Assigning a [`Material`](Material.md) to the `CanvasGroup` overrides the built-in shader. To duplicate the behavior of the built-in shader in a custom [`Shader`](Shader.md), use the following:

```csharp
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
vec4 c = textureLod(screen_texture, SCREEN_UV, 0.0);

if (c.a > 0.0001) {
c.rgb /= c.a;
}

COLOR *= c;
}

```

**Note:** Since `CanvasGroup` and [`CanvasItem.ClipChildren`](CanvasItem.md) both utilize the backbuffer, children of a `CanvasGroup` who have their [`CanvasItem.ClipChildren`](CanvasItem.md) set to anything other than [`CanvasItem.ClipChildrenMode.Disabled`](CanvasItem.md) will not function correctly.

## 构造函数

### CanvasGroup

```csharp
public CanvasGroup()
```

## 属性

### ClearMargin

```csharp
public float ClearMargin { get; set; }
```

引擎名：`clear_margin`

Sets the size of the margin used to expand the clearing rect of this `CanvasGroup`. This expands the area of the backbuffer that will be used by the `CanvasGroup`. A smaller margin will reduce the area of the backbuffer used which can increase performance, however if `UseMipmaps` is enabled, a small margin may result in mipmap errors at the edge of the `CanvasGroup`. Accordingly, this should be left as small as possible, but should be increased if artifacts appear along the edges of the canvas group.

### FitMargin

```csharp
public float FitMargin { get; set; }
```

引擎名：`fit_margin`

Sets the size of a margin used to expand the drawable rect of this `CanvasGroup`. The size of the `CanvasGroup` is determined by fitting a rect around its children then expanding that rect by `FitMargin`. This increases both the backbuffer area used and the area covered by the `CanvasGroup` both of which can reduce performance. This should be kept as small as possible and should only be expanded when an increased size is needed (e.g. for custom shader effects).

### UseMipmaps

```csharp
public bool UseMipmaps { get; set; }
```

引擎名：`use_mipmaps`

If `true`, calculates mipmaps for the backbuffer before drawing the `CanvasGroup` so that mipmaps can be used in a custom [`ShaderMaterial`](ShaderMaterial.md) attached to the `CanvasGroup`. Generating mipmaps has a performance cost so this should not be enabled unless required.

## 方法

### GetClearMargin

```csharp
public float GetClearMargin()
```

引擎名：`get_clear_margin`

### GetFitMargin

```csharp
public float GetFitMargin()
```

引擎名：`get_fit_margin`

### IsUsingMipmaps

```csharp
public bool IsUsingMipmaps()
```

引擎名：`is_using_mipmaps`

### SetClearMargin

```csharp
public void SetClearMargin(float clearMargin)
```

引擎名：`set_clear_margin`

### SetFitMargin

```csharp
public void SetFitMargin(float fitMargin)
```

引擎名：`set_fit_margin`

### SetUseMipmaps

```csharp
public void SetUseMipmaps(bool useMipmaps)
```

引擎名：`set_use_mipmaps`
