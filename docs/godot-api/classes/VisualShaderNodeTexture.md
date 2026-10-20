<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTexture

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from.

## 构造函数

### VisualShaderNodeTexture

```csharp
public VisualShaderNodeTexture()
```

## 属性

### Source

```csharp
public VisualShaderNodeTexture.SourceEnum Source { get; set; }
```

引擎名：`source`

Determines the source for the lookup.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The source texture, if needed for the selected `Source`.

### TextureType

```csharp
public VisualShaderNodeTexture.TextureTypeEnum TextureType { get; set; }
```

引擎名：`texture_type`

Specifies the type of the texture if `Source` is set to [`VisualShaderNodeTexture.SourceEnum.Texture`](VisualShaderNodeTexture.md).

## 方法

### GetSource

```csharp
public VisualShaderNodeTexture.SourceEnum GetSource()
```

引擎名：`get_source`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureType

```csharp
public VisualShaderNodeTexture.TextureTypeEnum GetTextureType()
```

引擎名：`get_texture_type`

### SetSource

```csharp
public void SetSource(VisualShaderNodeTexture.SourceEnum value)
```

引擎名：`set_source`

### SetTexture

```csharp
public void SetTexture(Texture2D value)
```

引擎名：`set_texture`

### SetTextureType

```csharp
public void SetTextureType(VisualShaderNodeTexture.TextureTypeEnum value)
```

引擎名：`set_texture_type`

## SourceEnum

命名空间 `Godot`；enum

### 枚举值

- `Texture = 0`：Use the texture given as an argument for this function.
- `Screen = 1`：Use the current viewport's texture as the source.
- `Source2DTexture = 2`：Use the texture from this shader's texture built-in (e.g.
- `Source2DNormal = 3`：Use the texture from this shader's normal map built-in.
- `Depth = 4`：Use the depth texture captured during the depth prepass.
- `Port = 5`：Use the texture provided in the input port for this function.
- `Source3DNormal = 6`：Use the normal buffer captured during the depth prepass.
- `Roughness = 7`：Use the roughness buffer captured during the depth prepass.
- `Max = 8`：Represents the size of the `VisualShaderNodeTexture.SourceEnum` enum.

## TextureTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Data = 0`：No hints are added to the uniform declaration.
- `Color = 1`：Adds `source_color` as hint to the uniform declaration for proper conversion from nonlinear sRGB encoding to linear encoding.
- `NormalMap = 2`：Adds `hint_normal` as hint to the uniform declaration, which internally converts the texture for proper usage as normal map.
- `Max = 3`：Represents the size of the `VisualShaderNodeTexture.TextureTypeEnum` enum.
