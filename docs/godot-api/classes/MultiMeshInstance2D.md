<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiMeshInstance2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`MultiMeshInstance2D` is a specialized node to instance a [`MultiMesh`](MultiMesh.md) resource in 2D. This can be faster to render compared to displaying many [`Sprite2D`](Sprite2D.md) nodes with large transparent areas, especially if the nodes take up a lot of space on screen at high viewport resolutions. This is because using a mesh designed to fit the sprites' opaque areas will reduce GPU fill rate utilization (at the cost of increased vertex processing utilization).

Usage is the same as [`MultiMeshInstance3D`](MultiMeshInstance3D.md).

## 构造函数

### MultiMeshInstance2D

```csharp
public MultiMeshInstance2D()
```

## 属性

### Multimesh

```csharp
public MultiMesh Multimesh { get; set; }
```

引擎名：`multimesh`

The [`MultiMesh`](MultiMesh.md) that will be drawn by the `MultiMeshInstance2D`.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The [`Texture2D`](Texture2D.md) that will be used if using the default [`CanvasItemMaterial`](CanvasItemMaterial.md). Can be accessed as `TEXTURE` in CanvasItem shader.

## 方法

### GetMultimesh

```csharp
public MultiMesh GetMultimesh()
```

引擎名：`get_multimesh`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### SetMultimesh

```csharp
public void SetMultimesh(MultiMesh multimesh)
```

引擎名：`set_multimesh`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

## 事件（信号）

### TextureChanged

```csharp
public event Action TextureChanged
```

引擎名：`texture_changed`

Emitted when the `Texture` is changed.
