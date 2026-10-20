<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModificationStack2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource is used by the Skeleton and holds a stack of [`SkeletonModification2D`](SkeletonModification2D.md)s.

This controls the order of the modifications and how they are applied. Modification order is especially important for full-body IK setups, as you need to execute the modifications in the correct order to get the desired results. For example, you want to execute a modification on the spine *before* the arms on a humanoid skeleton.

This resource also controls how strongly all of the modifications are applied to the [`Skeleton2D`](Skeleton2D.md).

## 构造函数

### SkeletonModificationStack2D

```csharp
public SkeletonModificationStack2D()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true`, the modification's in the stack will be called. This is handled automatically through the [`Skeleton2D`](Skeleton2D.md) node.

### ModificationCount

```csharp
public int ModificationCount { get; set; }
```

引擎名：`modification_count`

The number of modifications in the stack.

### Strength

```csharp
public float Strength { get; set; }
```

引擎名：`strength`

The interpolation strength of the modifications in stack. A value of `0` will make it where the modifications are not applied, a strength of `0.5` will be half applied, and a strength of `1` will allow the modifications to be fully applied and override the [`Skeleton2D`](Skeleton2D.md) [`Bone2D`](Bone2D.md) poses.

## 方法

### AddModification

```csharp
public void AddModification(SkeletonModification2D modification)
```

引擎名：`add_modification`

Adds the passed-in [`SkeletonModification2D`](SkeletonModification2D.md) to the stack.

### DeleteModification

```csharp
public void DeleteModification(int modIdx)
```

引擎名：`delete_modification`

Deletes the [`SkeletonModification2D`](SkeletonModification2D.md) at the index position `modIdx`, if it exists.

### EnableAllModifications

```csharp
public void EnableAllModifications(bool enabled)
```

引擎名：`enable_all_modifications`

Enables all [`SkeletonModification2D`](SkeletonModification2D.md)s in the stack.

### Execute

```csharp
public void Execute(float delta, int executionMode)
```

引擎名：`execute`

Executes all of the [`SkeletonModification2D`](SkeletonModification2D.md)s in the stack that use the same execution mode as the passed-in `executionMode`, starting from index `0` to `ModificationCount`.

**Note:** The order of the modifications can matter depending on the modifications. For example, modifications on a spine should operate before modifications on the arms in order to get proper results.

### GetEnabled

```csharp
public bool GetEnabled()
```

引擎名：`get_enabled`

### GetIsSetup

```csharp
public bool GetIsSetup()
```

引擎名：`get_is_setup`

Returns a boolean that indicates whether the modification stack is setup and can execute.

### GetModification

```csharp
public SkeletonModification2D GetModification(int modIdx)
```

引擎名：`get_modification`

Returns the [`SkeletonModification2D`](SkeletonModification2D.md) at the passed-in index, `modIdx`.

### GetModificationCount

```csharp
public int GetModificationCount()
```

引擎名：`get_modification_count`

### GetSkeleton

```csharp
public Skeleton2D GetSkeleton()
```

引擎名：`get_skeleton`

Returns the [`Skeleton2D`](Skeleton2D.md) node that the SkeletonModificationStack2D is bound to.

### GetStrength

```csharp
public float GetStrength()
```

引擎名：`get_strength`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetModification

```csharp
public void SetModification(int modIdx, SkeletonModification2D modification)
```

引擎名：`set_modification`

Sets the modification at `modIdx` to the passed-in modification, `modification`.

### SetModificationCount

```csharp
public void SetModificationCount(int count)
```

引擎名：`set_modification_count`

### SetStrength

```csharp
public void SetStrength(float strength)
```

引擎名：`set_strength`

### Setup

```csharp
public void Setup()
```

引擎名：`setup`

Sets up the modification stack so it can execute. This function should be called by [`Skeleton2D`](Skeleton2D.md) and shouldn't be manually called unless you know what you are doing.
