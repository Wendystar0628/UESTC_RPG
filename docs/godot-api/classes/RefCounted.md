<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RefCounted

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Base class for any object that keeps a reference count. [`Resource`](Resource.md) and many other helper objects inherit this class.

Unlike other [`GodotObject`](GodotObject.md) types, `RefCounted`s keep an internal reference counter so that they are automatically released when no longer in use, and only then. `RefCounted`s therefore do not need to be freed manually with [`GodotObject.Free`](GodotObject.md).

`RefCounted` instances caught in a cyclic reference will **not** be freed automatically. For example, if a node holds a reference to instance `A`, which directly or indirectly holds a reference back to `A`, `A`'s reference count will be 2. Destruction of the node will leave `A` dangling with a reference count of 1, and there will be a memory leak. To prevent this, one of the references in the cycle can be made weak with `@GlobalScope.weakref`.

In the vast majority of use cases, instantiating and using `RefCounted`-derived types is all you need to do. The methods provided in this class are only for advanced users, and can cause issues if misused.

**Note:** In C#, reference-counted objects will not be freed instantly after they are no longer in use. Instead, garbage collection will run periodically and will free reference-counted objects that are no longer in use. This means that unused ones will remain in memory for a while before being removed.

## 构造函数

### RefCounted

```csharp
public RefCounted()
```

## 方法

### GetReferenceCount

```csharp
public int GetReferenceCount()
```

引擎名：`get_reference_count`

Returns the current reference count.

### InitRef

```csharp
public bool InitRef()
```

引擎名：`init_ref`

Initializes the internal reference counter. Use this only if you really know what you are doing.

Returns whether the initialization was successful.

### Reference

```csharp
public bool Reference()
```

引擎名：`reference`

Increments the internal reference counter. Use this only if you really know what you are doing.

Returns `true` if the increment was successful, `false` otherwise.

### Unreference

```csharp
public bool Unreference()
```

引擎名：`unreference`

Decrements the internal reference counter. Use this only if you really know what you are doing.

Returns `true` if the object should be freed after the decrement, `false` otherwise.
