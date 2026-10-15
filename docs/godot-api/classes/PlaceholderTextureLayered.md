<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaceholderTextureLayered

命名空间 `Godot`；class；继承 [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is used when loading a project that uses a [`TextureLayered`](TextureLayered.md) subclass in 2 conditions:

- When running the project exported in dedicated server mode, only the texture's dimensions are kept (as they may be relied upon for gameplay purposes or positioning of other elements). This allows reducing the exported PCK's size significantly.

- When this subclass is missing due to using a different engine version or build (e.g. modules disabled).

**Note:** This is not intended to be used as an actual texture for rendering. It is not guaranteed to work like one in shaders or materials (for example when calculating UV).

## 属性

### Layers

```csharp
public int Layers { get; set; }
```

引擎名：`layers`

The number of layers in the texture array.

### Size

```csharp
public Vector2I Size { get; set; }
```

引擎名：`size`

The size of each texture layer (in pixels).

## 方法

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

### SetLayers

```csharp
public void SetLayers(int layers)
```

引擎名：`set_layers`

### SetSize

```csharp
public void SetSize(Vector2I size)
```

引擎名：`set_size`
