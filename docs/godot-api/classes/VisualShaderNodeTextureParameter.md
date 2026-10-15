<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTextureParameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Performs a lookup operation on the texture provided as a uniform for the shader.

## 属性

### ColorDefault

```csharp
public VisualShaderNodeTextureParameter.ColorDefaultEnum ColorDefault { get; set; }
```

引擎名：`color_default`

Sets the default color if no texture is assigned to the uniform.

### TextureFilter

```csharp
public VisualShaderNodeTextureParameter.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

Sets the texture filtering mode.

### TextureRepeat

```csharp
public VisualShaderNodeTextureParameter.TextureRepeatEnum TextureRepeat { get; set; }
```

引擎名：`texture_repeat`

Sets the texture repeating mode.

### TextureSource

```csharp
public VisualShaderNodeTextureParameter.TextureSourceEnum TextureSource { get; set; }
```

引擎名：`texture_source`

Sets the texture source mode. Used for reading from the screen, depth, or normal_roughness texture.

### TextureType

```csharp
public VisualShaderNodeTextureParameter.TextureTypeEnum TextureType { get; set; }
```

引擎名：`texture_type`

Defines the type of data provided by the source texture.

## 方法

### GetColorDefault

```csharp
public VisualShaderNodeTextureParameter.ColorDefaultEnum GetColorDefault()
```

引擎名：`get_color_default`

### GetTextureFilter

```csharp
public VisualShaderNodeTextureParameter.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

### GetTextureRepeat

```csharp
public VisualShaderNodeTextureParameter.TextureRepeatEnum GetTextureRepeat()
```

引擎名：`get_texture_repeat`

### GetTextureSource

```csharp
public VisualShaderNodeTextureParameter.TextureSourceEnum GetTextureSource()
```

引擎名：`get_texture_source`

### GetTextureType

```csharp
public VisualShaderNodeTextureParameter.TextureTypeEnum GetTextureType()
```

引擎名：`get_texture_type`

### SetColorDefault

```csharp
public void SetColorDefault(VisualShaderNodeTextureParameter.ColorDefaultEnum color)
```

引擎名：`set_color_default`

### SetTextureFilter

```csharp
public void SetTextureFilter(VisualShaderNodeTextureParameter.TextureFilterEnum filter)
```

引擎名：`set_texture_filter`

### SetTextureRepeat

```csharp
public void SetTextureRepeat(VisualShaderNodeTextureParameter.TextureRepeatEnum repeat)
```

引擎名：`set_texture_repeat`

### SetTextureSource

```csharp
public void SetTextureSource(VisualShaderNodeTextureParameter.TextureSourceEnum source)
```

引擎名：`set_texture_source`

### SetTextureType

```csharp
public void SetTextureType(VisualShaderNodeTextureParameter.TextureTypeEnum type)
```

引擎名：`set_texture_type`

## ColorDefaultEnum

命名空间 `Godot`；enum

### 枚举值

- `White = 0`：Defaults to fully opaque white color.
- `Black = 1`：Defaults to fully opaque black color.
- `Transparent = 2`：Defaults to fully transparent black color.
- `Max = 3`：Represents the size of the `VisualShaderNodeTextureParameter.ColorDefaultEnum` enum.

## TextureFilterEnum

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Sample the texture using the filter determined by the node this shader is attached to.
- `Nearest = 1`：The texture filter reads from the nearest pixel only.
- `Linear = 2`：The texture filter blends between the nearest 4 pixels.
- `NearestMipmap = 3`：The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `LinearMipmap = 4`：The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `NearestMipmapAnisotropic = 5`：The texture filter reads from the nearest pixel and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the angle bet…
- `LinearMipmapAnisotropic = 6`：The texture filter blends between the nearest 4 pixels and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the an…
- `Max = 7`：Represents the size of the `VisualShaderNodeTextureParameter.TextureFilterEnum` enum.

## TextureRepeatEnum

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Sample the texture using the repeat mode determined by the node this shader is attached to.
- `Enabled = 1`：Texture will repeat normally.
- `Disabled = 2`：Texture will not repeat.
- `Max = 3`：Represents the size of the `VisualShaderNodeTextureParameter.TextureRepeatEnum` enum.

## TextureSourceEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The texture source is not specified in the shader.
- `Screen = 1`：The texture source is the screen texture which captures all opaque objects drawn this frame.
- `Depth = 2`：The texture source is the depth texture from the depth prepass.
- `NormalRoughness = 3`：The texture source is the normal-roughness buffer from the depth prepass.
- `Max = 4`：Represents the size of the `VisualShaderNodeTextureParameter.TextureSourceEnum` enum.

## TextureTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Data = 0`：No hints are added to the uniform declaration.
- `Color = 1`：Adds `source_color` as hint to the uniform declaration for proper conversion from nonlinear sRGB encoding to linear encoding.
- `NormalMap = 2`：Adds `hint_normal` as hint to the uniform declaration, which internally converts the texture for proper usage as normal map.
- `Anisotropy = 3`：Adds `hint_anisotropy` as hint to the uniform declaration to use for a flowmap.
- `Max = 4`：Represents the size of the `VisualShaderNodeTextureParameter.TextureTypeEnum` enum.
