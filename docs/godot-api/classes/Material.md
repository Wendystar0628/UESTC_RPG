<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Material

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`Material` is a base resource used for coloring and shading geometry. All materials inherit from it and almost all [`VisualInstance3D`](VisualInstance3D.md) derived nodes carry a `Material`. A few flags and parameters are shared between all material types and are configured here.

Importantly, you can inherit from `Material` to create your own custom material type in script or in GDExtension.

## 构造函数

### Material

```csharp
public Material()
```

## 属性

### NextPass

```csharp
public Material NextPass { get; set; }
```

引擎名：`next_pass`

Sets the `Material` to be used for the next pass. This renders the object again using a different material.

**Note:** `NextPass` materials are not necessarily drawn immediately after the source `Material`. Draw order is determined by material properties, `RenderPriority`, and distance to camera.

**Note:** This only applies to [`StandardMaterial3D`](StandardMaterial3D.md)s and [`ShaderMaterial`](ShaderMaterial.md)s with type "Spatial".

### RenderPriority

```csharp
public int RenderPriority { get; set; }
```

引擎名：`render_priority`

Sets the render priority for objects in 3D scenes. Higher priority objects will be sorted in front of lower priority objects. In other words, all objects with `RenderPriority` `1` will render on top of all objects with `RenderPriority` `0`.

**Note:** This only applies to [`StandardMaterial3D`](StandardMaterial3D.md)s and [`ShaderMaterial`](ShaderMaterial.md)s with type "Spatial".

**Note:** This will not impact how transparent objects are sorted relative to opaque objects or how dynamic meshes will be sorted relative to other opaque meshes. This is because all transparent objects are drawn after all opaque objects and all dynamic opaque meshes are drawn before other opaque meshes.

## 方法

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderMaterial`](PlaceholderMaterial.md)).

### GetNextPass

```csharp
public Material GetNextPass()
```

引擎名：`get_next_pass`

### GetRenderPriority

```csharp
public int GetRenderPriority()
```

引擎名：`get_render_priority`

### InspectNativeShaderCode

```csharp
public void InspectNativeShaderCode()
```

引擎名：`inspect_native_shader_code`

Only available when running in the editor. Opens a popup that visualizes the generated shader code, including all variants and internal shader code. See also [`Shader.InspectNativeShaderCode`](Shader.md).

### SetNextPass

```csharp
public void SetNextPass(Material nextPass)
```

引擎名：`set_next_pass`

### SetRenderPriority

```csharp
public void SetRenderPriority(int priority)
```

引擎名：`set_render_priority`

### _CanDoNextPass

```csharp
public virtual bool _CanDoNextPass()
```

引擎名：`_can_do_next_pass`

Only exposed for the purpose of overriding. You cannot call this function directly. Used internally to determine if `NextPass` should be shown in the editor or not.

### _CanUseRenderPriority

```csharp
public virtual bool _CanUseRenderPriority()
```

引擎名：`_can_use_render_priority`

Only exposed for the purpose of overriding. You cannot call this function directly. Used internally to determine if `RenderPriority` should be shown in the editor or not.

### _GetShaderMode

```csharp
public virtual Shader.Mode _GetShaderMode()
```

引擎名：`_get_shader_mode`

Only exposed for the purpose of overriding. You cannot call this function directly. Used internally by various editor tools.

### _GetShaderRid

```csharp
public virtual Rid _GetShaderRid()
```

引擎名：`_get_shader_rid`

Only exposed for the purpose of overriding. You cannot call this function directly. Used internally by various editor tools. Used to access the RID of the `Material`'s [`Shader`](Shader.md).

## 常量

### RenderPriorityMax

```csharp
public const long RenderPriorityMax = 127
```

Maximum value for the `RenderPriority` parameter.

### RenderPriorityMin

```csharp
public const long RenderPriorityMin = -128
```

Minimum value for the `RenderPriority` parameter.
