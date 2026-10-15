<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfSkeleton

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### GltfSkeleton

```csharp
public GltfSkeleton()
```

## 属性

### GodotBoneNode

```csharp
public Dictionary GodotBoneNode { get; set; }
```

### Joints

```csharp
public int[] Joints { get; set; }
```

### Roots

```csharp
public int[] Roots { get; set; }
```

### UniqueNames

```csharp
public Array<string> UniqueNames { get; set; }
```

## 方法

### GetBoneAttachment

```csharp
public BoneAttachment3D GetBoneAttachment(int idx)
```

### GetBoneAttachmentCount

```csharp
public int GetBoneAttachmentCount()
```

### GetGodotBoneNode

```csharp
public Dictionary GetGodotBoneNode()
```

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) that maps skeleton bone indices to the indices of glTF nodes. This property is unused during import, and only set during export. In a glTF file, a bone is a node, so Godot converts skeleton bones to glTF nodes.

### GetGodotSkeleton

```csharp
public Skeleton3D GetGodotSkeleton()
```

### GetJoints

```csharp
public int[] GetJoints()
```

### GetRoots

```csharp
public int[] GetRoots()
```

### GetUniqueNames

```csharp
public Array<string> GetUniqueNames()
```

### SetGodotBoneNode

```csharp
public void SetGodotBoneNode(Dictionary godotBoneNode)
```

Sets a [`Collections.Dictionary`](Collections.Dictionary.md) that maps skeleton bone indices to the indices of glTF nodes. This property is unused during import, and only set during export. In a glTF file, a bone is a node, so Godot converts skeleton bones to glTF nodes.

### SetJoints

```csharp
public void SetJoints(ReadOnlySpan<int> joints)
```

### SetJoints

```csharp
public void SetJoints(int[] joints)
```

### SetRoots

```csharp
public void SetRoots(ReadOnlySpan<int> roots)
```

### SetRoots

```csharp
public void SetRoots(int[] roots)
```

### SetUniqueNames

```csharp
public void SetUniqueNames(Array<string> uniqueNames)
```
