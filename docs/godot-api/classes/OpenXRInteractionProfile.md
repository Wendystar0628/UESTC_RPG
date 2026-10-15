<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRInteractionProfile

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object stores suggested bindings for an interaction profile. Interaction profiles define the metadata for a tracked XR device such as an XR controller.

For more information see the [interaction profiles info in the OpenXR specification](https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#semantic-path-interaction-profiles).

## 构造函数

### OpenXRInteractionProfile

```csharp
public OpenXRInteractionProfile()
```

## 属性

### BindingModifiers

```csharp
public Array BindingModifiers { get; set; }
```

引擎名：`binding_modifiers`

Binding modifiers for this interaction profile.

### Bindings

```csharp
public Array Bindings { get; set; }
```

引擎名：`bindings`

Action bindings for this interaction profile.

### InteractionProfilePath

```csharp
public string InteractionProfilePath { get; set; }
```

引擎名：`interaction_profile_path`

The interaction profile path identifying the XR device.

## 方法

### GetBinding

```csharp
public OpenXRIPBinding GetBinding(int index)
```

引擎名：`get_binding`

Retrieve the binding at this index.

### GetBindingCount

```csharp
public int GetBindingCount()
```

引擎名：`get_binding_count`

Get the number of bindings in this interaction profile.

### GetBindingModifier

```csharp
public OpenXripBindingModifier GetBindingModifier(int index)
```

引擎名：`get_binding_modifier`

Get the [`OpenXRBindingModifier`](OpenXRBindingModifier.md) at this index.

### GetBindingModifierCount

```csharp
public int GetBindingModifierCount()
```

引擎名：`get_binding_modifier_count`

Get the number of binding modifiers in this interaction profile.

### GetBindingModifiers

```csharp
public Array GetBindingModifiers()
```

引擎名：`get_binding_modifiers`

### GetBindings

```csharp
public Array GetBindings()
```

引擎名：`get_bindings`

### GetInteractionProfilePath

```csharp
public string GetInteractionProfilePath()
```

引擎名：`get_interaction_profile_path`

### SetBindingModifiers

```csharp
public void SetBindingModifiers(Array bindingModifiers)
```

引擎名：`set_binding_modifiers`

### SetBindings

```csharp
public void SetBindings(Array bindings)
```

引擎名：`set_bindings`

### SetInteractionProfilePath

```csharp
public void SetInteractionProfilePath(string interactionProfilePath)
```

引擎名：`set_interaction_profile_path`
