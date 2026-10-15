<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EncodedObjectAsId

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Utility class which holds a reference to the internal identifier of an [`GodotObject`](GodotObject.md) instance, as given by [`GodotObject.GetInstanceId`](GodotObject.md). This ID can then be used to retrieve the object instance with `GodotObject.InstanceFromId`.

This class is used internally by the editor inspector and script debugger, but can also be used in plugins to pass and display objects as their IDs.

## 构造函数

### EncodedObjectAsId

```csharp
public EncodedObjectAsId()
```

## 属性

### ObjectId

```csharp
public ulong ObjectId { get; set; }
```

The [`GodotObject`](GodotObject.md) identifier stored in this `EncodedObjectAsId` instance. The object instance can be retrieved with `GodotObject.InstanceFromId`.

## 方法

### GetObjectId

```csharp
public ulong GetObjectId()
```

### SetObjectId

```csharp
public void SetObjectId(ulong id)
```
