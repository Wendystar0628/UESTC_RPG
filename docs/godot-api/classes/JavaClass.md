<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JavaClass

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a class from the Java Native Interface. It is returned from [`JavaClassWrapper.Wrap`](JavaClassWrapper.md).

**Note:** This class only works on Android. On any other platform, this class does nothing.

**Note:** This class is not to be confused with [`JavaScriptObject`](JavaScriptObject.md).

## 构造函数

### JavaClass

```csharp
public JavaClass()
```

## 方法

### GetJavaClassName

```csharp
public string GetJavaClassName()
```

引擎名：`get_java_class_name`

Returns the Java class name.

### GetJavaMethodList

```csharp
public Array<Dictionary> GetJavaMethodList()
```

引擎名：`get_java_method_list`

Returns the object's Java methods and their signatures as an [`Collections.Array`](Collections.Array.md) of dictionaries, in the same format as [`GodotObject.GetMethodList`](GodotObject.md).

### GetJavaParentClass

```csharp
public JavaClass GetJavaParentClass()
```

引擎名：`get_java_parent_class`

Returns a `JavaClass` representing the Java parent class of this class.

### HasJavaMethod

```csharp
public bool HasJavaMethod(StringName method)
```

引擎名：`has_java_method`

Returns `true` if the given `method` name exists in the object's Java methods.
