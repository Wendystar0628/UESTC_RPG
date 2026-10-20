<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FbxState

命名空间 `Godot`；class；继承 [`GltfState`](GltfState.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The FBXState handles the state data imported from FBX files.

## 构造函数

### FbxState

```csharp
public FbxState()
```

## 属性

### AllowGeometryHelperNodes

```csharp
public bool AllowGeometryHelperNodes { get; set; }
```

If `true`, the import process used auxiliary nodes called geometry helper nodes. These nodes help preserve the pivots and transformations of the original 3D model during import.

## 方法

### GetAllowGeometryHelperNodes

```csharp
public bool GetAllowGeometryHelperNodes()
```

### SetAllowGeometryHelperNodes

```csharp
public void SetAllowGeometryHelperNodes(bool allow)
```
