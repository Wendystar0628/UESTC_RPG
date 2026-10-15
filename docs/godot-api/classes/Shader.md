<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Shader

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A custom shader program implemented in the Godot shading language, saved with the `.gdshader` extension.

This class is used by a [`ShaderMaterial`](ShaderMaterial.md) and allows you to write your own custom behavior for rendering visual items or updating particle information. For a detailed explanation and usage, please see the tutorials linked below.

## 构造函数

### Shader

```csharp
public Shader()
```

## 属性

### Code

```csharp
public string Code { get; set; }
```

引擎名：`code`

Returns the shader's code as the user has written it, not the full generated code used internally.

## 方法

### GetCode

```csharp
public string GetCode()
```

引擎名：`get_code`

### GetDefaultTextureParameter

```csharp
public Texture GetDefaultTextureParameter(StringName name, int index = 0)
```

引擎名：`get_default_texture_parameter`

Returns the texture that is set as default for the specified parameter.

**Note:** `name` must match the name of the uniform in the code exactly.

**Note:** If the sampler array is used use `index` to access the specified texture.

### GetMode

```csharp
public Shader.Mode GetMode()
```

引擎名：`get_mode`

Returns the shader mode for the shader.

### GetShaderUniformList

```csharp
public Array GetShaderUniformList(bool getGroups = false)
```

引擎名：`get_shader_uniform_list`

Returns the list of shader uniforms that can be assigned to a [`ShaderMaterial`](ShaderMaterial.md), for use with [`ShaderMaterial.SetShaderParameter`](ShaderMaterial.md) and [`ShaderMaterial.GetShaderParameter`](ShaderMaterial.md). The parameters returned are contained in dictionaries in a similar format to the ones returned by [`GodotObject.GetPropertyList`](GodotObject.md).

If argument `getGroups` is `true`, parameter grouping hints are also included in the list.

### InspectNativeShaderCode

```csharp
public void InspectNativeShaderCode()
```

引擎名：`inspect_native_shader_code`

Only available when running in the editor. Opens a popup that visualizes the generated shader code, including all variants and internal shader code. See also [`Material.InspectNativeShaderCode`](Material.md).

### SetCode

```csharp
public void SetCode(string code)
```

引擎名：`set_code`

### SetDefaultTextureParameter

```csharp
public void SetDefaultTextureParameter(StringName name, Texture texture, int index = 0)
```

引擎名：`set_default_texture_parameter`

Sets the default texture to be used with a texture uniform. The default is used if a texture is not set in the [`ShaderMaterial`](ShaderMaterial.md).

**Note:** `name` must match the name of the uniform in the code exactly.

**Note:** If the sampler array is used use `index` to access the specified texture.

### SetDefaultTextureParameter

```csharp
public void SetDefaultTextureParameter(StringName name, Texture2D texture, int index)
```

引擎名：`set_default_texture_parameter`

Sets the default texture to be used with a texture uniform. The default is used if a texture is not set in the [`ShaderMaterial`](ShaderMaterial.md).

**Note:** `name` must match the name of the uniform in the code exactly.

**Note:** If the sampler array is used use `index` to access the specified texture.

## Mode

命名空间 `Godot`；enum

### 枚举值

- `Spatial = 0`：Mode used to draw all 3D objects.
- `CanvasItem = 1`：Mode used to draw all 2D objects.
- `Particles = 2`：Mode used to calculate particle information on a per-particle basis.
- `Sky = 3`：Mode used for drawing skies.
- `Fog = 4`：Mode used for setting the color and density of volumetric fog effect.
- `TextureBlit = 5`：Mode used for drawing to DrawableTexture resources via blit calls.
