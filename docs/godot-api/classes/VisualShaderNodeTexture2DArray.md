<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTexture2DArray

命名空间 `Godot`；class；继承 [`VisualShaderNodeSample3D`](VisualShaderNodeSample3D.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translated to `uniform sampler2DArray` in the shader language.

## 构造函数

### VisualShaderNodeTexture2DArray

```csharp
public VisualShaderNodeTexture2DArray()
```

## 属性

### TextureArray

```csharp
public TextureLayered TextureArray { get; set; }
```

引擎名：`texture_array`

A source texture array. Used if [`VisualShaderNodeSample3D.Source`](VisualShaderNodeSample3D.md) is set to [`VisualShaderNodeSample3D.SourceEnum.Texture`](VisualShaderNodeSample3D.md).

## 方法

### GetTextureArray

```csharp
public TextureLayered GetTextureArray()
```

引擎名：`get_texture_array`

### SetTextureArray

```csharp
public void SetTextureArray(Texture2DArray value)
```

引擎名：`set_texture_array`

### SetTextureArray

```csharp
public void SetTextureArray(TextureLayered value)
```

引擎名：`set_texture_array`
