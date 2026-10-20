<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRBindingModifier

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Binding modifier base class. Subclasses implement various modifiers that alter how an OpenXR runtime processes inputs.

## 方法

### _GetDescription

```csharp
public virtual string _GetDescription()
```

引擎名：`_get_description`

Return the description of this class that is used for the title bar of the binding modifier editor.

### _GetIPModification

```csharp
public virtual byte[] _GetIPModification()
```

引擎名：`_get_ip_modification`

Returns the data that is sent to OpenXR when submitting the suggested interacting bindings this modifier is a part of.

**Note:** This must be data compatible with an `XrBindingModificationBaseHeaderKHR` structure.
