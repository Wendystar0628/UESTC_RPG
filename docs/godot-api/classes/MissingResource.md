<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MissingResource

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is an internal editor class intended for keeping data of resources of unknown type (most likely this type was supplied by an extension that is no longer loaded). It can't be manually instantiated or placed in a scene.

**Warning:** Ignore missing resources unless you know what you are doing. Existing properties on a missing resource can be freely modified in code, regardless of the type they are intended to be.

## 构造函数

### MissingResource

```csharp
public MissingResource()
```

## 属性

### OriginalClass

```csharp
public string OriginalClass { get; set; }
```

引擎名：`original_class`

The name of the class this resource was supposed to be (see [`GodotObject.GetClass`](GodotObject.md)).

### RecordingProperties

```csharp
public bool RecordingProperties { get; set; }
```

引擎名：`recording_properties`

If set to `true`, allows new properties to be added on top of the existing ones with [`GodotObject.Set`](GodotObject.md).

## 方法

### GetOriginalClass

```csharp
public string GetOriginalClass()
```

引擎名：`get_original_class`

### IsRecordingProperties

```csharp
public bool IsRecordingProperties()
```

引擎名：`is_recording_properties`

### SetOriginalClass

```csharp
public void SetOriginalClass(string name)
```

引擎名：`set_original_class`

### SetRecordingProperties

```csharp
public void SetRecordingProperties(bool enable)
```

引擎名：`set_recording_properties`
