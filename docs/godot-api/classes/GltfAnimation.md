<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfAnimation

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### GltfAnimation

```csharp
public GltfAnimation()
```

## 属性

### Loop

```csharp
public bool Loop { get; set; }
```

### OriginalName

```csharp
public string OriginalName { get; set; }
```

The original name of the animation.

## 方法

### GetAdditionalData

```csharp
public Variant GetAdditionalData(StringName extensionName)
```

Gets additional arbitrary data in this `GltfAnimation` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the return value can be anything you set. If nothing was set, the return value is `null`.

### GetLoop

```csharp
public bool GetLoop()
```

### GetOriginalName

```csharp
public string GetOriginalName()
```

### SetAdditionalData

```csharp
public void SetAdditionalData(StringName extensionName, Variant additionalData)
```

Sets additional arbitrary data in this `GltfAnimation` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The first argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the second argument can be anything you want.

### SetLoop

```csharp
public void SetLoop(bool loop)
```

### SetOriginalName

```csharp
public void SetOriginalName(string originalName)
```
