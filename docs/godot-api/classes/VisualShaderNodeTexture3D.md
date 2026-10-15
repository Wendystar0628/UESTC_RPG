<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTexture3D

命名空间 `Godot`；class；继承 [`VisualShaderNodeSample3D`](VisualShaderNodeSample3D.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from.

## 构造函数

### VisualShaderNodeTexture3D

```csharp
public VisualShaderNodeTexture3D()
```

## 属性

### Texture

```csharp
public Texture3D Texture { get; set; }
```

引擎名：`texture`

A source texture. Used if [`VisualShaderNodeSample3D.Source`](VisualShaderNodeSample3D.md) is set to [`VisualShaderNodeSample3D.SourceEnum.Texture`](VisualShaderNodeSample3D.md).

## 方法

### GetTexture

```csharp
public Texture3D GetTexture()
```

引擎名：`get_texture`

### SetTexture

```csharp
public void SetTexture(Texture3D value)
```

引擎名：`set_texture`
