<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectNotchFilter

命名空间 `Godot`；class；继承 [`AudioEffectFilter`](AudioEffectFilter.md) → [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "notch" filter attenuates frequencies at [`AudioEffectFilter.CutoffHz`](AudioEffectFilter.md) and allows frequencies outside the frequency threshold to pass unchanged. It is a narrower and stronger version of [`AudioEffectBandLimitFilter`](AudioEffectBandLimitFilter.md), and is the opposite of [`AudioEffectBandPassFilter`](AudioEffectBandPassFilter.md).

This filter can be used to give more room for other sounds to play at that frequency. Because of how much it attenuates frequencies, it can also be used to completely remove undesired frequencies.

## 构造函数

### AudioEffectNotchFilter

```csharp
public AudioEffectNotchFilter()
```
