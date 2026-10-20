<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JniSingleton

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The JNISingleton is implemented only in the Android export. It's used to call methods and connect signals from an Android plugin written in Java or Kotlin. Methods and signals can be called and connected to the JNISingleton as if it is a Node. See [Java Native Interface - Wikipedia](https://en.wikipedia.org/wiki/Java_Native_Interface) for more information.

## 构造函数

### JniSingleton

```csharp
public JniSingleton()
```

## 方法

### HasJavaMethod

```csharp
public bool HasJavaMethod(StringName method)
```

Returns `true` if the given `method` name exists in the JNISingleton's Java methods.
