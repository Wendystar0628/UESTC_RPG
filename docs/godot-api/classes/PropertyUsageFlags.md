<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PropertyUsageFlags

命名空间 `Godot`；enum

## 枚举值

- `None = 0`：The property is not stored, and does not display in the editor.
- `Storage = 2`：The property is serialized and saved in the scene file (default for exported properties).
- `Editor = 4`：The property is shown in the `EditorInspector` (default for exported properties).
- `Internal = 8`：The property is excluded from the class reference.
- `Checkable = 16`：The property can be checked in the `EditorInspector`.
- `Checked = 32`：The property is checked in the `EditorInspector`.
- `Group = 64`：Used to group properties together in the editor.
- `Category = 128`：Used to categorize properties together in the editor.
- `Subgroup = 256`：Used to group properties together in the editor in a subgroup (under a group).
- `ClassIsBitfield = 512`：The property is a bitfield, i.e.
- `NoInstanceState = 1024`：The property does not save its state in [`PackedScene`](PackedScene.md).
- `RestartIfChanged = 2048`：Editing the property prompts the user for restarting the editor.
- `ScriptVariable = 4096`：The property is a script variable.
- `StoreIfNull = 8192`：The property value of type [`GodotObject`](GodotObject.md) will be stored even if its value is `null`.
- `UpdateAllIfModified = 16384`：If this property is modified, all inspector fields will be refreshed.
- `ScriptDefaultValue = 32768`
- `ClassIsEnum = 65536`：The property is a variable of enum type, i.e.
- `NilIsVariant = 131072`：If property has `nil` as default value, its type will be [`Variant`](Variant.md).
- `Array = 262144`：The property is the element count of a property array, i.e.
- `AlwaysDuplicate = 524288`：When duplicating a resource with [`Resource.Duplicate`](Resource.md), and this flag is set on a property of that resource, the property should always be duplicated, regardless of the `subresources` bool parameter.
- `NeverDuplicate = 1048576`：When duplicating a resource with [`Resource.Duplicate`](Resource.md), and this flag is set on a property of that resource, the property should never be duplicated, regardless of the `subresources` bool parameter.
- `HighEndGfx = 2097152`：The property is only shown in the editor if modern renderers are supported (the Compatibility rendering method is excluded).
- `NodePathFromSceneRoot = 4194304`：The [`NodePath`](NodePath.md) property will always be relative to the scene's root.
- `ResourceNotPersistent = 8388608`：Use when a resource is created on the fly, i.e.
- `KeyingIncrements = 16777216`：Inserting an animation key frame of this property will automatically increment the value, allowing to easily keyframe multiple values in a row.
- `DeferredSetResource = 33554432`
- `EditorInstantiateObject = 67108864`：When this property is a [`Resource`](Resource.md) and base object is a [`Node`](Node.md), a resource instance will be automatically created whenever the node is created in the editor.
- `EditorBasicSetting = 134217728`：The property is considered a basic setting and will appear even when advanced mode is disabled.
- `ReadOnly = 268435456`：The property is read-only in the `EditorInspector`.
- `Secret = 536870912`：An export preset property with this flag contains confidential information and is stored separately from the rest of the export preset configuration.
- `Default = 6`：Default usage (storage and editor).
- `NoEditor = 2`：Default usage but without showing the property in the editor (storage).
