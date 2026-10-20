<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ViewportTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A `ViewportTexture` provides the content of a [`Viewport`](Viewport.md) as a dynamic [`Texture2D`](Texture2D.md). This can be used to combine the rendering of [`Control`](Control.md), [`Node2D`](Node2D.md) and [`Node3D`](Node3D.md) nodes. For example, you can use this texture to display a 3D scene inside a [`TextureRect`](TextureRect.md), or a 2D overlay in a [`Sprite3D`](Sprite3D.md).

To get a `ViewportTexture` in code, use the [`Viewport.GetTexture`](Viewport.md) method on the target viewport.

**Note:** A `ViewportTexture` is always local to its scene (see [`Resource.ResourceLocalToScene`](Resource.md)). If the scene root is not ready, it may return incorrect data (see [`Node.Ready`](Node.md)).

**Note:** Instantiating scenes containing a high-resolution `ViewportTexture` may cause noticeable stutter.

**Note:** When using a [`Viewport`](Viewport.md) with [`Viewport.UseHdr2D`](Viewport.md) set to `true`, the returned texture will be an HDR image that uses linear encoding. This may look darker than normal when displayed directly on screen. To convert to nonlinear sRGB encoding, you can do the following:

```csharp
img.convert(Image.FORMAT_RGBA8)
img.linear_to_srgb()

```

**Note:** Some nodes such as [`Decal`](Decal.md), [`Light3D`](Light3D.md), and [`PointLight2D`](PointLight2D.md) do not support using `ViewportTexture` directly. To use texture data from a `ViewportTexture` in these nodes, you need to create an [`ImageTexture`](ImageTexture.md) by calling [`Texture2D.GetImage`](Texture2D.md) on the `ViewportTexture` and passing the result to [`ImageTexture.CreateFromImage`](ImageTexture.md). This conversion is a slow operation, so it should not be performed every frame.

## 构造函数

### ViewportTexture

```csharp
public ViewportTexture()
```

## 属性

### ViewportPath

```csharp
public NodePath ViewportPath { get; set; }
```

引擎名：`viewport_path`

The path to the [`Viewport`](Viewport.md) node to display. This is relative to the local scene root (see [`Resource.GetLocalScene`](Resource.md)), **not** to the nodes that use this texture.

**Note:** In the editor, this path is automatically updated when the target viewport or one of its ancestors is renamed or moved. At runtime, this path may not automatically update if the scene root cannot be found.

## 方法

### GetViewportPathInScene

```csharp
public NodePath GetViewportPathInScene()
```

引擎名：`get_viewport_path_in_scene`

### SetViewportPathInScene

```csharp
public void SetViewportPathInScene(NodePath path)
```

引擎名：`set_viewport_path_in_scene`
