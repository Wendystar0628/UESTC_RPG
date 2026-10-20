<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectBandLimitFilter

命名空间 `Godot`；class；继承 [`AudioEffectFilter`](AudioEffectFilter.md) → [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "band-limit" filter attenuates the frequencies at [`AudioEffectFilter.CutoffHz`](AudioEffectFilter.md), and allows frequencies outside the frequency threshold to pass unchanged. It is a wider and weaker version of [`AudioEffectNotchFilter`](AudioEffectNotchFilter.md), and is the opposite of [`AudioEffectBandPassFilter`](AudioEffectBandPassFilter.md).

This filter can be used to give more room for other sounds to play at that frequency.

## 构造函数

### AudioEffectBandLimitFilter

```csharp
public AudioEffectBandLimitFilter()
```
