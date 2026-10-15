<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeCubemap

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translated to `texture(cubemap, vec3)` in the shader language. Returns a color vector and alpha channel as scalar.

## 构造函数

### VisualShaderNodeCubemap

```csharp
public VisualShaderNodeCubemap()
```

## 属性

### CubeMap

```csharp
public TextureLayered CubeMap { get; set; }
```

引擎名：`cube_map`

The [`Cubemap`](Cubemap.md) texture to sample when using [`VisualShaderNodeCubemap.SourceEnum.Texture`](VisualShaderNodeCubemap.md) as `Source`.

### Source

```csharp
public VisualShaderNodeCubemap.SourceEnum Source { get; set; }
```

引擎名：`source`

Defines which source should be used for the sampling.

### TextureType

```csharp
public VisualShaderNodeCubemap.TextureTypeEnum TextureType { get; set; }
```

引擎名：`texture_type`

Defines the type of data provided by the source texture.

## 方法

### GetCubeMap

```csharp
public TextureLayered GetCubeMap()
```

引擎名：`get_cube_map`

### GetSource

```csharp
public VisualShaderNodeCubemap.SourceEnum GetSource()
```

引擎名：`get_source`

### GetTextureType

```csharp
public VisualShaderNodeCubemap.TextureTypeEnum GetTextureType()
```

引擎名：`get_texture_type`

### SetCubeMap

```csharp
public void SetCubeMap(Cubemap value)
```

引擎名：`set_cube_map`

### SetCubeMap

```csharp
public void SetCubeMap(TextureLayered value)
```

引擎名：`set_cube_map`

### SetSource

```csharp
public void SetSource(VisualShaderNodeCubemap.SourceEnum value)
```

引擎名：`set_source`

### SetTextureType

```csharp
public void SetTextureType(VisualShaderNodeCubemap.TextureTypeEnum value)
```

引擎名：`set_texture_type`

## SourceEnum

命名空间 `Godot`；enum

### 枚举值

- `Texture = 0`：Use the [`Cubemap`](Cubemap.md) set via [`VisualShaderNodeCubemap.CubeMap`](VisualShaderNodeCubemap.md).
- `Port = 1`：Use the [`Cubemap`](Cubemap.md) sampler reference passed via the `samplerCube` port.
- `Max = 2`：Represents the size of the `VisualShaderNodeCubemap.SourceEnum` enum.

## TextureTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Data = 0`：No hints are added to the uniform declaration.
- `Color = 1`：Adds `source_color` as hint to the uniform declaration for proper conversion from nonlinear sRGB encoding to linear encoding.
- `NormalMap = 2`：Adds `hint_normal` as hint to the uniform declaration, which internally converts the texture for proper usage as normal map.
- `Max = 3`：Represents the size of the `VisualShaderNodeCubemap.TextureTypeEnum` enum.
