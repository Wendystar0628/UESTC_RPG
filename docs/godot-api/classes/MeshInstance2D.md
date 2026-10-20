<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshInstance2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Node used for displaying a [`Mesh`](Mesh.md) in 2D. This can be faster to render compared to displaying a [`Sprite2D`](Sprite2D.md) node with large transparent areas, especially if the node takes up a lot of space on screen at high viewport resolutions. This is because using a mesh designed to fit the sprite's opaque areas will reduce GPU fill rate utilization (at the cost of increased vertex processing utilization).

When a [`Mesh`](Mesh.md) has to be instantiated more than thousands of times close to each other, consider using a [`MultiMesh`](MultiMesh.md) in a [`MultiMeshInstance2D`](MultiMeshInstance2D.md) instead.

A `MeshInstance2D` can be created from an existing [`Sprite2D`](Sprite2D.md) via a tool in the editor toolbar. Select the [`Sprite2D`](Sprite2D.md) node, then choose **Sprite2D > Convert to MeshInstance2D** at the top of the 2D editor viewport.

## 构造函数

### MeshInstance2D

```csharp
public MeshInstance2D()
```

## 属性

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

引擎名：`mesh`

The [`Mesh`](Mesh.md) that will be drawn by the `MeshInstance2D`.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The [`Texture2D`](Texture2D.md) that will be used if using the default [`CanvasItemMaterial`](CanvasItemMaterial.md). Can be accessed as `TEXTURE` in CanvasItem shader.

## 方法

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

引擎名：`set_mesh`

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
