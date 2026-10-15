<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeSync

命名空间 `Godot`；class；继承 [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An animation node used to combine, mix, or blend two or more animations together while keeping them synchronized within an [`AnimationTree`](AnimationTree.md).

## 构造函数

### AnimationNodeSync

```csharp
public AnimationNodeSync()
```

## 属性

### Sync

```csharp
public bool Sync { get; set; }
```

引擎名：`sync`

If `false`, the blended animations' frame are stopped when the blend value is `0`.

If `true`, forcing the blended animations to advance frame.

## 方法

### IsUsingSync

```csharp
public bool IsUsingSync()
```

引擎名：`is_using_sync`

### SetUseSync

```csharp
public void SetUseSync(bool enable)
```

引擎名：`set_use_sync`
