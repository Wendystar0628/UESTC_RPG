<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CenterContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`CenterContainer` is a container that keeps all of its child controls in its center at their minimum size.

## 构造函数

### CenterContainer

```csharp
public CenterContainer()
```

## 属性

### UseTopLeft

```csharp
public bool UseTopLeft { get; set; }
```

引擎名：`use_top_left`

If `true`, centers children relative to the `CenterContainer`'s top left corner.

## 方法

### IsUsingTopLeft

```csharp
public bool IsUsingTopLeft()
```

引擎名：`is_using_top_left`

### SetUseTopLeft

```csharp
public void SetUseTopLeft(bool enable)
```

引擎名：`set_use_top_left`
