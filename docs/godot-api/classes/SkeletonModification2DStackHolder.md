<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DStackHolder

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModification2D`](SkeletonModification2D.md) holds a reference to a [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md), allowing you to use multiple modification stacks on a single [`Skeleton2D`](Skeleton2D.md).

**Note:** The modifications in the held [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) will only be executed if their execution mode matches the execution mode of the SkeletonModification2DStackHolder.

## 构造函数

### SkeletonModification2DStackHolder

```csharp
public SkeletonModification2DStackHolder()
```

## 方法

### GetHeldModificationStack

```csharp
public SkeletonModificationStack2D GetHeldModificationStack()
```

引擎名：`get_held_modification_stack`

Returns the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) that this modification is holding.

### SetHeldModificationStack

```csharp
public void SetHeldModificationStack(SkeletonModificationStack2D heldModificationStack)
```

引擎名：`set_held_modification_stack`

Sets the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) that this modification is holding. This modification stack will then be executed when this modification is executed.
