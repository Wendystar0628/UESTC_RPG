<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioListener2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Once added to the scene tree and enabled using `MakeCurrent()`, this node will override the location sounds are heard from. Only one `AudioListener2D` can be current. Using `MakeCurrent()` will disable the previous `AudioListener2D`.

If there is no active `AudioListener2D` in the current [`Viewport`](Viewport.md), center of the screen will be used as a hearing point for the audio. `AudioListener2D` needs to be inside [`SceneTree`](SceneTree.md) to function.

## 构造函数

### AudioListener2D

```csharp
public AudioListener2D()
```

## 方法

### ClearCurrent

```csharp
public void ClearCurrent()
```

引擎名：`clear_current`

Disables the `AudioListener2D`. If it's not set as current, this method will have no effect.

### IsCurrent

```csharp
public bool IsCurrent()
```

引擎名：`is_current`

Returns `true` if this `AudioListener2D` is currently active.

### MakeCurrent

```csharp
public void MakeCurrent()
```

引擎名：`make_current`

Makes the `AudioListener2D` active, setting it as the hearing point for the sounds. If there is already another active `AudioListener2D`, it will be disabled.

This method will have no effect if the `AudioListener2D` is not added to [`SceneTree`](SceneTree.md).
