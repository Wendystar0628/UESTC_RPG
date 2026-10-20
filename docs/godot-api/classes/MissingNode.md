<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MissingNode

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This is an internal editor class intended for keeping data of nodes of unknown type (most likely this type was supplied by an extension that is no longer loaded). It can't be manually instantiated or placed in a scene.

**Warning:** Ignore missing nodes unless you know what you are doing. Existing properties on a missing node can be freely modified in code, regardless of the type they are intended to be.

## 构造函数

### MissingNode

```csharp
public MissingNode()
```

## 属性

### OriginalClass

```csharp
public string OriginalClass { get; set; }
```

引擎名：`original_class`

The name of the class this node was supposed to be (see [`GodotObject.GetClass`](GodotObject.md)).

### OriginalScene

```csharp
public string OriginalScene { get; set; }
```

引擎名：`original_scene`

Returns the path of the scene this node was instance of originally.

### RecordingProperties

```csharp
public bool RecordingProperties { get; set; }
```

引擎名：`recording_properties`

If `true`, allows new properties to be set along with existing ones. If `false`, only existing properties' values can be set, and new properties cannot be added.

### RecordingSignals

```csharp
public bool RecordingSignals { get; set; }
```

引擎名：`recording_signals`

If `true`, allows new signals to be connected to along with existing ones. If `false`, only existing signals can be connected to, and new signals cannot be added.

## 方法

### GetOriginalClass

```csharp
public string GetOriginalClass()
```

引擎名：`get_original_class`

### GetOriginalScene

```csharp
public string GetOriginalScene()
```

引擎名：`get_original_scene`

### IsRecordingProperties

```csharp
public bool IsRecordingProperties()
```

引擎名：`is_recording_properties`

### IsRecordingSignals

```csharp
public bool IsRecordingSignals()
```

引擎名：`is_recording_signals`

### SetOriginalClass

```csharp
public void SetOriginalClass(string name)
```

引擎名：`set_original_class`

### SetOriginalScene

```csharp
public void SetOriginalScene(string name)
```

引擎名：`set_original_scene`

### SetRecordingProperties

```csharp
public void SetRecordingProperties(bool enable)
```

引擎名：`set_recording_properties`

### SetRecordingSignals

```csharp
public void SetRecordingSignals(bool enable)
```

引擎名：`set_recording_signals`
