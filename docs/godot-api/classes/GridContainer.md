<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GridContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`GridContainer` arranges its child controls in a grid layout. The number of columns is specified by the `Columns` property, whereas the number of rows depends on how many are needed for the child controls. The number of rows and columns is preserved for every size of the container.

**Note:** `GridContainer` only works with child nodes inheriting from [`Control`](Control.md). It won't rearrange child nodes inheriting from [`Node2D`](Node2D.md).

## 构造函数

### GridContainer

```csharp
public GridContainer()
```

## 属性

### Columns

```csharp
public int Columns { get; set; }
```

引擎名：`columns`

The number of columns in the `GridContainer`. If modified, `GridContainer` reorders its Control-derived children to accommodate the new layout.

## 方法

### GetColumns

```csharp
public int GetColumns()
```

引擎名：`get_columns`

### SetColumns

```csharp
public void SetColumns(int columns)
```

引擎名：`set_columns`
