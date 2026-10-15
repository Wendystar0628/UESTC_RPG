<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PropertyHint

命名空间 `Godot`；enum

## 枚举值

- `None = 0`：The property has no hint for the editor.
- `Range = 1`：Hints that an `System.Int32`, `System.Single`, or packed/typed [`Collections.Array`](Collections.Array.md) property containing `System.Int32` or `System.Single` types should be within a range specified via the hint strin…
- `Enum = 2`：Hints that an `System.Int32`, `System.String`, or [`StringName`](StringName.md) property is an enumerated value to pick in a list specified via a hint string.
- `EnumSuggestion = 3`：Hints that a `System.String` or [`StringName`](StringName.md) property can be an enumerated value to pick in a list specified via a hint string such as `"Hello,Something,Else"`.
- `ExpEasing = 4`：Hints that a `System.Single` property should be edited using a curve editor showing an exponential easing function.
- `Link = 5`：Hints that a vector property should allow its components to be linked.
- `Flags = 6`：Hints that an `System.Int32` property is a bitmask with named bit flags.
- `Layers2DRender = 7`：Hints that an `System.Int32` property is a bitmask using the optionally named 2D render layers.
- `Layers2DPhysics = 8`：Hints that an `System.Int32` property is a bitmask using the optionally named 2D physics layers.
- `Layers2DNavigation = 9`：Hints that an `System.Int32` property is a bitmask using the optionally named 2D navigation layers.
- `Layers3DRender = 10`：Hints that an `System.Int32` property is a bitmask using the optionally named 3D render layers.
- `Layers3DPhysics = 11`：Hints that an `System.Int32` property is a bitmask using the optionally named 3D physics layers.
- `Layers3DNavigation = 12`：Hints that an `System.Int32` property is a bitmask using the optionally named 3D navigation layers.
- `LayersAvoidance = 37`：Hints that an integer property is a bitmask using the optionally named avoidance layers.
- `File = 13`：Hints that a `System.String` property is a path to a file.
- `Dir = 14`：Hints that a `System.String` property is a path to a directory.
- `GlobalFile = 15`：Hints that a `System.String` property is an absolute path to a file outside the project folder.
- `GlobalDir = 16`：Hints that a `System.String` property is an absolute path to a directory outside the project folder.
- `ResourceType = 17`：Hints that a property is an instance of a [`Resource`](Resource.md)-derived type, optionally specified via the hint string (e.g.
- `MultilineText = 18`：Hints that a `System.String` property is text with line breaks.
- `Expression = 19`：Hints that a `System.String` property is an [`Expression`](Expression.md).
- `PlaceholderText = 20`：Hints that a `System.String` property should show a placeholder text on its input field, if empty.
- `ColorNoAlpha = 21`：Hints that a [`Color`](Color.md) property should be edited without affecting its transparency (`Color.A` is not editable).
- `ObjectId = 22`：Hints that the property's value is an object encoded as object ID, with its type specified in the hint string.
- `TypeString = 23`：If a property is `System.String`, hints that the property represents a particular type (class).
- `NodePathToEditedNode = 24`
- `ObjectTooBig = 25`：Hints that an object is too big to be sent via the debugger.
- `NodePathValidTypes = 26`：Hints that the hint string specifies valid node types for property of type [`NodePath`](NodePath.md).
- `SaveFile = 27`：Hints that a `System.String` property is a path to a file.
- `GlobalSaveFile = 28`：Hints that a `System.String` property is a path to a file.
- `IntIsObjectid = 29`
- `IntIsPointer = 30`：Hints that an `System.Int32` property is a pointer.
- `ArrayType = 31`：Hints that a property is an [`Collections.Array`](Collections.Array.md) with the stored type specified in the hint string.
- `DictionaryType = 38`：Hints that a property is a [`Collections.Dictionary`](Collections.Dictionary.md) with the stored types specified in the hint string.
- `LocaleId = 32`：Hints that a string property is a locale code.
- `LocalizableString = 33`：Hints that a dictionary property is string translation map.
- `NodeType = 34`：Hints that a property is an instance of a [`Node`](Node.md)-derived type, optionally specified via the hint string (e.g.
- `HideQuaternionEdit = 35`：Hints that a quaternion property should disable the temporary euler editor.
- `Password = 36`：Hints that a string property is a password, and every character is replaced with the secret character.
- `ToolButton = 39`：Hints that a [`Callable`](Callable.md) property should be displayed as a clickable button.
- `Oneshot = 40`：Hints that a property will be changed on its own after setting, such as [`AudioStreamPlayer.Playing`](AudioStreamPlayer.md) or [`GpuParticles3D.Emitting`](GpuParticles3D.md).
- `GroupEnable = 42`：Hints that a boolean property will enable the feature associated with the group that it occurs in.
- `InputName = 43`：Hints that a `System.String` or [`StringName`](StringName.md) property is the name of an input action.
- `FilePath = 44`：Like `File`, but the property is stored as a raw path, not UID.
- `Max = 45`：Represents the size of the `PropertyHint` enum.
