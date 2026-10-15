<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ShaderMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A material that uses a custom [`Shader`](Shader.md) program to render visual items (canvas items, meshes, skies, fog), or to process particles. Compared to other materials, `ShaderMaterial` gives deeper control over the generated shader code. For more information, see the shaders documentation index below.

Multiple `ShaderMaterial`s can use the same shader and configure different values for the shader uniforms.

**Note:** For performance reasons, the [`Resource.Changed`](Resource.md) signal is only emitted when the [`Resource.ResourceName`](Resource.md) changes. Only in editor, it is also emitted for `Shader` changes.

## 构造函数

### ShaderMaterial

```csharp
public ShaderMaterial()
```

## 属性

### Shader

```csharp
public Shader Shader { get; set; }
```

引擎名：`shader`

The [`Shader`](Shader.md) program used to render this material.

## 方法

### GetShader

```csharp
public Shader GetShader()
```

引擎名：`get_shader`

### GetShaderParameter

```csharp
public Variant GetShaderParameter(StringName param)
```

引擎名：`get_shader_parameter`

Returns the current value set for this material of a uniform in the shader.

### SetShader

```csharp
public void SetShader(Shader shader)
```

引擎名：`set_shader`

### SetShaderParameter

```csharp
public void SetShaderParameter(StringName param, Variant value)
```

引擎名：`set_shader_parameter`

Changes the value set for this material of a uniform in the shader.

**Note:** `param` is case-sensitive and must match the name of the uniform in the code exactly (not the capitalized name in the inspector).

**Note:** Changes to the shader uniform will be effective on all instances using this `ShaderMaterial`. To prevent this, use per-instance uniforms with [`CanvasItem.SetInstanceShaderParameter`](CanvasItem.md), [`GeometryInstance3D.SetInstanceShaderParameter`](GeometryInstance3D.md) or duplicate the `ShaderMaterial` resource using [`Resource.Duplicate`](Resource.md). Per-instance uniforms allow for better shader reuse and are therefore faster, so they should be preferred over duplicating the `ShaderMaterial` when possible.
