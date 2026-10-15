<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectInstance

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An audio effect instance manipulates the audio it receives for a given effect. This instance is automatically created by an [`AudioEffect`](AudioEffect.md) when it is added to a bus, and should usually not be created directly. If necessary, it can be fetched at run-time with [`AudioServer.GetBusEffectInstance`](AudioServer.md).

## 构造函数

### AudioEffectInstance

```csharp
public AudioEffectInstance()
```

## 方法

### _ProcessSilence

```csharp
public virtual bool _ProcessSilence()
```

引擎名：`_process_silence`

Override this method to customize the processing behavior of this effect instance.

Should return `true` to force the [`AudioServer`](AudioServer.md) to always call `_process`, even if the bus has been muted or cannot otherwise be heard.
