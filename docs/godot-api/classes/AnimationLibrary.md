<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationLibrary

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An animation library stores a set of animations accessible through [`StringName`](StringName.md) keys, for use with [`AnimationPlayer`](AnimationPlayer.md) nodes.

## 构造函数

### AnimationLibrary

```csharp
public AnimationLibrary()
```

## 属性

### _Data

```csharp
public Dictionary _Data { get; set; }
```

## 方法

### AddAnimation

```csharp
public Error AddAnimation(StringName name, Animation animation)
```

引擎名：`add_animation`

Adds the `animation` to the library, accessible by the key `name`.

### GetAnimation

```csharp
public Animation GetAnimation(StringName name)
```

引擎名：`get_animation`

Returns the [`Animation`](Animation.md) with the key `name`. If the animation does not exist, `null` is returned and an error is logged.

### GetAnimationList

```csharp
public Array<StringName> GetAnimationList()
```

引擎名：`get_animation_list`

Returns the keys for the [`Animation`](Animation.md)s stored in the library.

### GetAnimationListSize

```csharp
public int GetAnimationListSize()
```

引擎名：`get_animation_list_size`

Returns the key count for the [`Animation`](Animation.md)s stored in the library.

### HasAnimation

```csharp
public bool HasAnimation(StringName name)
```

引擎名：`has_animation`

Returns `true` if the library stores an [`Animation`](Animation.md) with `name` as the key.

### RemoveAnimation

```csharp
public void RemoveAnimation(StringName name)
```

引擎名：`remove_animation`

Removes the [`Animation`](Animation.md) with the key `name`.

### RenameAnimation

```csharp
public void RenameAnimation(StringName name, StringName newname)
```

引擎名：`rename_animation`

Changes the key of the [`Animation`](Animation.md) associated with the key `name` to `newname`.

## 事件（信号）

### AnimationAdded

```csharp
public event AnimationLibrary.AnimationAddedEventHandler AnimationAdded
```

引擎名：`animation_added`

Emitted when an [`Animation`](Animation.md) is added, under the key `animName`.

### AnimationChanged

```csharp
public event AnimationLibrary.AnimationChangedEventHandler AnimationChanged
```

引擎名：`animation_changed`

Emitted when there's a change in one of the animations, e.g. tracks are added, moved or have changed paths. `animName` is the key of the animation that was changed.

See also [`Resource.Changed`](Resource.md), which this acts as a relay for.

### AnimationRemoved

```csharp
public event AnimationLibrary.AnimationRemovedEventHandler AnimationRemoved
```

引擎名：`animation_removed`

Emitted when an [`Animation`](Animation.md) stored with the key `animName` is removed.

### AnimationRenamed

```csharp
public event AnimationLibrary.AnimationRenamedEventHandler AnimationRenamed
```

引擎名：`animation_renamed`

Emitted when the key for an [`Animation`](Animation.md) is changed, from `oldName` to `newName`.

## AnimationAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationLibrary.AnimationAdded`](AnimationLibrary.md) event of a [`AnimationLibrary`](AnimationLibrary.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationAddedEventHandler(StringName animName)
```

## AnimationChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationLibrary.AnimationChanged`](AnimationLibrary.md) event of a [`AnimationLibrary`](AnimationLibrary.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationChangedEventHandler(StringName animName)
```

## AnimationRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationLibrary.AnimationRemoved`](AnimationLibrary.md) event of a [`AnimationLibrary`](AnimationLibrary.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationRemovedEventHandler(StringName animName)
```

## AnimationRenamedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationLibrary.AnimationRenamed`](AnimationLibrary.md) event of a [`AnimationLibrary`](AnimationLibrary.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationRenamedEventHandler(StringName oldName, StringName newName)
```
