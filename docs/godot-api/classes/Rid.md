<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Rid

命名空间 `Godot`；struct

The RID type is used to access a low-level resource by its unique ID.
RIDs are opaque, which means they do not grant access to the resource
by themselves. They are used by the low-level server classes, such as
[`DisplayServer`](DisplayServer.md), [`RenderingServer`](RenderingServer.md),
[`TextServer`](TextServer.md), etc.

A low-level resource may correspond to a high-level [`Resource`](Resource.md),
such as [`Texture`](Texture.md) or [`Mesh`](Mesh.md)

## 构造函数

### Rid

```csharp
public Rid(GodotObject from)
```

Constructs a new `Rid` for the given [`GodotObject`](GodotObject.md) `from`.

## 属性

### Id

```csharp
public ulong Id { get; }
```

Returns the ID of the referenced low-level resource.

返回：The ID of the referenced resource.

### IsValid

```csharp
public bool IsValid { get; }
```

Returns `true` if the `Rid` is not `0`.

返回：Whether or not the ID is valid.

## 方法

### Equals

```csharp
public bool Equals(Rid other)
```

Returns `true` if the RIDs are equal.

参数：
- `other`：The other RID.

返回：Whether or not the RIDs are equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this RID and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the color and the other object are equal.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Rid`.

返回：A hash code for this RID.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Rid` to a string.

返回：A string representation of this Rid.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Rid left, Rid right)
```

Returns `true` if both `Rid`s are equal,
which means they both refer to the same low-level resource.

参数：
- `left`：The left RID.
- `right`：The right RID.

返回：Whether or not the RIDs are equal.

### op_Inequality

```csharp
public static bool op_Inequality(Rid left, Rid right)
```

Returns `true` if the `Rid`s are not equal.

参数：
- `left`：The left RID.
- `right`：The right RID.

返回：Whether or not the RIDs are equal.
