<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffect

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The base [`Resource`](Resource.md) for every audio effect. In the editor, an audio effect can be added to the current bus layout through the Audio panel. At run-time, it is also possible to manipulate audio effects through [`AudioServer.AddBusEffect`](AudioServer.md), [`AudioServer.RemoveBusEffect`](AudioServer.md), and [`AudioServer.GetBusEffect`](AudioServer.md).

When applied on a bus, an audio effect creates a corresponding [`AudioEffectInstance`](AudioEffectInstance.md). The instance is directly responsible for manipulating sound, based on the original audio effect's properties.

## 构造函数

### AudioEffect

```csharp
public AudioEffect()
```

## 方法

### _Instantiate

```csharp
public virtual AudioEffectInstance _Instantiate()
```

引擎名：`_instantiate`

Override this method to customize the [`AudioEffectInstance`](AudioEffectInstance.md) created when this effect is applied on a bus in the editor's Audio panel, or through [`AudioServer.AddBusEffect`](AudioServer.md).

```csharp
extends AudioEffect

@export var strength = 4.0

func _instantiate():
var effect = CustomAudioEffectInstance.new()
effect.base = self

return effect

```

**Note:** It is recommended to keep a reference to the original `AudioEffect` in the new instance. Depending on the implementation this allows the effect instance to listen for changes at run-time and be modified accordingly.
