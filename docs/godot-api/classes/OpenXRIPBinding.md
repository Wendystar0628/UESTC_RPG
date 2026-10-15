<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRIPBinding

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This binding resource binds an [`OpenXRAction`](OpenXRAction.md) to an input or output. As most controllers have left hand and right versions that are handled by the same interaction profile we can specify multiple bindings. For instance an action "Fire" could be bound to both "/user/hand/left/input/trigger" and "/user/hand/right/input/trigger". This would require two binding entries.

## 构造函数

### OpenXRIPBinding

```csharp
public OpenXRIPBinding()
```

## 属性

### Action

```csharp
public OpenXRAction Action { get; set; }
```

引擎名：`action`

[`OpenXRAction`](OpenXRAction.md) that is bound to `BindingPath`.

### BindingModifiers

```csharp
public Array BindingModifiers { get; set; }
```

引擎名：`binding_modifiers`

Binding modifiers for this binding.

### BindingPath

```csharp
public string BindingPath { get; set; }
```

引擎名：`binding_path`

Binding path that defines the input or output bound to `Action`.

**Note:** Binding paths are suggestions, an XR runtime may choose to bind the action to a different input or output emulating this input or output.

### Paths

```csharp
public string[] Paths { get; set; }
```

引擎名：`paths`

Paths that define the inputs or outputs bound on the device.

## 方法

### AddPath

```csharp
public void AddPath(string path)
```

引擎名：`add_path`

Add an input/output path to this binding.

### GetAction

```csharp
public OpenXRAction GetAction()
```

引擎名：`get_action`

### GetBindingModifier

```csharp
public OpenXRActionBindingModifier GetBindingModifier(int index)
```

引擎名：`get_binding_modifier`

Get the [`OpenXRBindingModifier`](OpenXRBindingModifier.md) at this index.

### GetBindingModifierCount

```csharp
public int GetBindingModifierCount()
```

引擎名：`get_binding_modifier_count`

Get the number of binding modifiers for this binding.

### GetBindingModifiers

```csharp
public Array GetBindingModifiers()
```

引擎名：`get_binding_modifiers`

### GetBindingPath

```csharp
public string GetBindingPath()
```

引擎名：`get_binding_path`

### GetPathCount

```csharp
public int GetPathCount()
```

引擎名：`get_path_count`

Get the number of input/output paths in this binding.

### GetPaths

```csharp
public string[] GetPaths()
```

引擎名：`get_paths`

### HasPath

```csharp
public bool HasPath(string path)
```

引擎名：`has_path`

Returns `true` if this input/output path is part of this binding.

### RemovePath

```csharp
public void RemovePath(string path)
```

引擎名：`remove_path`

Removes this input/output path from this binding.

### SetAction

```csharp
public void SetAction(OpenXRAction action)
```

引擎名：`set_action`

### SetBindingModifiers

```csharp
public void SetBindingModifiers(Array bindingModifiers)
```

引擎名：`set_binding_modifiers`

### SetBindingPath

```csharp
public void SetBindingPath(string bindingPath)
```

引擎名：`set_binding_path`

### SetPaths

```csharp
public void SetPaths(ReadOnlySpan<string> paths)
```

引擎名：`set_paths`

### SetPaths

```csharp
public void SetPaths(string[] paths)
```

引擎名：`set_paths`
