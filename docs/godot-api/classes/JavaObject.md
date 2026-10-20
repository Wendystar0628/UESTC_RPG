<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JavaObject

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents an object from the Java Native Interface. It can be returned from Java methods called on [`JavaClass`](JavaClass.md) or other `JavaObject`s. See [`JavaClassWrapper`](JavaClassWrapper.md) for an example.

**Note:** This class only works on Android. On any other platform, this class does nothing.

**Note:** This class is not to be confused with [`JavaScriptObject`](JavaScriptObject.md).

## 构造函数

### JavaObject

```csharp
public JavaObject()
```

## 方法

### GetJavaClass

```csharp
public JavaClass GetJavaClass()
```

引擎名：`get_java_class`

Returns the [`JavaClass`](JavaClass.md) that this object is an instance of.

### HasJavaMethod

```csharp
public bool HasJavaMethod(StringName method)
```

引擎名：`has_java_method`

Returns `true` if the given `method` name exists in the object's Java methods.
