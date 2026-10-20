<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaceholderTexture3D

命名空间 `Godot`；class；继承 [`Texture3D`](Texture3D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is used when loading a project that uses a [`Texture3D`](Texture3D.md) subclass in 2 conditions:

- When running the project exported in dedicated server mode, only the texture's dimensions are kept (as they may be relied upon for gameplay purposes or positioning of other elements). This allows reducing the exported PCK's size significantly.

- When this subclass is missing due to using a different engine version or build (e.g. modules disabled).

**Note:** This is not intended to be used as an actual texture for rendering. It is not guaranteed to work like one in shaders or materials (for example when calculating UV).

## 构造函数

### PlaceholderTexture3D

```csharp
public PlaceholderTexture3D()
```

## 属性

### Size

```csharp
public Vector3I Size { get; set; }
```

引擎名：`size`

The texture's size (in pixels).

## 方法

### GetSize

```csharp
public Vector3I GetSize()
```

引擎名：`get_size`

### SetSize

```csharp
public void SetSize(Vector3I size)
```

引擎名：`set_size`
