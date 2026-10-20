<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Simple texture that uses a mesh to draw itself. It's limited because flags can't be changed and region drawing is not supported.

## 构造函数

### MeshTexture

```csharp
public MeshTexture()
```

## 属性

### BaseTexture

```csharp
public Texture2D BaseTexture { get; set; }
```

引擎名：`base_texture`

Sets the base texture that the Mesh will use to draw.

### ImageSize

```csharp
public Vector2 ImageSize { get; set; }
```

引擎名：`image_size`

Sets the size of the image, needed for reference.

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

引擎名：`mesh`

Sets the mesh used to draw. It must be a mesh using 2D vertices.

## 方法

### GetBaseTexture

```csharp
public Texture2D GetBaseTexture()
```

引擎名：`get_base_texture`

### GetImageSize

```csharp
public Vector2 GetImageSize()
```

引擎名：`get_image_size`

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

### SetBaseTexture

```csharp
public void SetBaseTexture(Texture2D texture)
```

引擎名：`set_base_texture`

### SetImageSize

```csharp
public void SetImageSize(Vector2 size)
```

引擎名：`set_image_size`

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

引擎名：`set_mesh`
