<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WeakRef

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A weakref can hold a [`RefCounted`](RefCounted.md) without contributing to the reference counter. A weakref can be created from an [`GodotObject`](GodotObject.md) using `@GlobalScope.weakref`. If this object is not a reference, weakref still works, however, it does not have any effect on the object. Weakrefs are useful in cases where multiple classes have variables that refer to each other. Without weakrefs, using these classes could lead to memory leaks, since both references keep each other from being released. Making part of the variables a weakref can prevent this cyclic dependency, and allows the references to be released.

## 构造函数

### WeakRef

```csharp
public WeakRef()
```

## 方法

### GetRef

```csharp
public Variant GetRef()
```

引擎名：`get_ref`

Returns the [`GodotObject`](GodotObject.md) this weakref is referring to. Returns `null` if that object no longer exists.
