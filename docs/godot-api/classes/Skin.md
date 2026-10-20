<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Skin

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### Skin

```csharp
public Skin()
```

## 方法

### AddBind

```csharp
public void AddBind(int bone, Transform3D pose)
```

引擎名：`add_bind`

### AddNamedBind

```csharp
public void AddNamedBind(string name, Transform3D pose)
```

引擎名：`add_named_bind`

### ClearBinds

```csharp
public void ClearBinds()
```

引擎名：`clear_binds`

### GetBindBone

```csharp
public int GetBindBone(int bindIndex)
```

引擎名：`get_bind_bone`

### GetBindCount

```csharp
public int GetBindCount()
```

引擎名：`get_bind_count`

### GetBindName

```csharp
public StringName GetBindName(int bindIndex)
```

引擎名：`get_bind_name`

### GetBindPose

```csharp
public Transform3D GetBindPose(int bindIndex)
```

引擎名：`get_bind_pose`

### SetBindBone

```csharp
public void SetBindBone(int bindIndex, int bone)
```

引擎名：`set_bind_bone`

### SetBindCount

```csharp
public void SetBindCount(int bindCount)
```

引擎名：`set_bind_count`

### SetBindName

```csharp
public void SetBindName(int bindIndex, StringName name)
```

引擎名：`set_bind_name`

### SetBindPose

```csharp
public void SetBindPose(int bindIndex, Transform3D pose)
```

引擎名：`set_bind_pose`
