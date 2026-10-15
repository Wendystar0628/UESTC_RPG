<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CustomGCHandle

命名空间 `Godot`；static class

Provides a GCHandle that becomes weak when unloading the assembly load context, without having
to manually replace the GCHandle. This hides all the complexity of releasing strong GC handles
to allow the assembly load context to unload properly.

Internally, a strong CustomGCHandle actually contains a weak GCHandle, while the actual strong
reference is stored in a static table.

## 方法

### AllocStrong

```csharp
public static GCHandle AllocStrong(object value)
```

### AllocStrong

```csharp
public static GCHandle AllocStrong(object value, Type valueType)
```

### AllocWeak

```csharp
public static GCHandle AllocWeak(object value)
```

### Free

```csharp
public static void Free(GCHandle handle)
```

### IsAlcBeingUnloaded

```csharp
public static bool IsAlcBeingUnloaded(AssemblyLoadContext alc)
```
