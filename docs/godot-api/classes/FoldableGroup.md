<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FoldableGroup

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A group of [`FoldableContainer`](FoldableContainer.md)-derived nodes. Only one container can be expanded at a time.

## 构造函数

### FoldableGroup

```csharp
public FoldableGroup()
```

## 属性

### AllowFoldingAll

```csharp
public bool AllowFoldingAll { get; set; }
```

引擎名：`allow_folding_all`

If `true`, it is possible to fold all containers in this FoldableGroup.

## 方法

### GetContainers

```csharp
public Array<FoldableContainer> GetContainers()
```

引擎名：`get_containers`

Returns an [`Collections.Array`](Collections.Array.md) of [`FoldableContainer`](FoldableContainer.md)s that have this as their FoldableGroup (see [`FoldableContainer.FoldableGroup`](FoldableContainer.md)). This is equivalent to [`ButtonGroup`](ButtonGroup.md) but for FoldableContainers.

### GetExpandedContainer

```csharp
public FoldableContainer GetExpandedContainer()
```

引擎名：`get_expanded_container`

Returns the current expanded container.

### IsAllowFoldingAll

```csharp
public bool IsAllowFoldingAll()
```

引擎名：`is_allow_folding_all`

### SetAllowFoldingAll

```csharp
public void SetAllowFoldingAll(bool enabled)
```

引擎名：`set_allow_folding_all`

## 事件（信号）

### Expanded

```csharp
public event FoldableGroup.ExpandedEventHandler Expanded
```

引擎名：`expanded`

Emitted when one of the containers of the group is expanded.

## ExpandedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FoldableGroup.Expanded`](FoldableGroup.md) event of a [`FoldableGroup`](FoldableGroup.md) class.

### 签名

#### Invoke

```csharp
delegate void ExpandedEventHandler(FoldableContainer container)
```
