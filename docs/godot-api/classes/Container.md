<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Container

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class for all GUI containers. A `Container` automatically arranges its child controls in a certain way. This class can be inherited to make custom container types.

## 构造函数

### Container

```csharp
public Container()
```

## 属性

### AccessibilityRegion

```csharp
public bool AccessibilityRegion { get; set; }
```

引擎名：`accessibility_region`

If `true`, this container is marked as a region for accessibility. Use [`Control.AccessibilityName`](Control.md) to give the region a descriptive name. Screen readers can navigate between regions using landmark navigation.

## 方法

### FitChildInRect

```csharp
public void FitChildInRect(Control child, Rect2 rect)
```

引擎名：`fit_child_in_rect`

Fit a child control in a given rect. This is mainly a helper for creating custom container classes.

### IsAccessibilityRegion

```csharp
public bool IsAccessibilityRegion()
```

引擎名：`is_accessibility_region`

### QueueSort

```csharp
public void QueueSort()
```

引擎名：`queue_sort`

Queue resort of the contained children. This is called automatically anyway, but can be called upon request.

### SetAccessibilityRegion

```csharp
public void SetAccessibilityRegion(bool region)
```

引擎名：`set_accessibility_region`

### _GetAllowedSizeFlagsHorizontal

```csharp
public virtual int[] _GetAllowedSizeFlagsHorizontal()
```

引擎名：`_get_allowed_size_flags_horizontal`

Implement to return a list of allowed horizontal [`Control.SizeFlags`](Control.md) for child nodes. This doesn't technically prevent the usages of any other size flags, if your implementation requires that. This only limits the options available to the user in the Inspector dock.

**Note:** Having no size flags is equal to having [`Control.SizeFlags.ShrinkBegin`](Control.md). As such, this value is always implicitly allowed.

### _GetAllowedSizeFlagsVertical

```csharp
public virtual int[] _GetAllowedSizeFlagsVertical()
```

引擎名：`_get_allowed_size_flags_vertical`

Implement to return a list of allowed vertical [`Control.SizeFlags`](Control.md) for child nodes. This doesn't technically prevent the usages of any other size flags, if your implementation requires that. This only limits the options available to the user in the Inspector dock.

**Note:** Having no size flags is equal to having [`Control.SizeFlags.ShrinkBegin`](Control.md). As such, this value is always implicitly allowed.

## 事件（信号）

### PreSortChildren

```csharp
public event Action PreSortChildren
```

引擎名：`pre_sort_children`

Emitted when children are going to be sorted.

### SortChildren

```csharp
public event Action SortChildren
```

引擎名：`sort_children`

Emitted when sorting the children is needed.

## 常量

### NotificationPreSortChildren

```csharp
public const long NotificationPreSortChildren = 50
```

Notification just before children are going to be sorted, in case there's something to process beforehand.

### NotificationSortChildren

```csharp
public const long NotificationSortChildren = 51
```

Notification for when sorting the children, it must be obeyed immediately.
