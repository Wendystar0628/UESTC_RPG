<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamMicrophone

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

When used directly in an [`AudioStreamPlayer`](AudioStreamPlayer.md) node, `AudioStreamMicrophone` plays back microphone input in real-time. This can be used in conjunction with [`AudioEffectCapture`](AudioEffectCapture.md) to process the data or save it.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

## 构造函数

### AudioStreamMicrophone

```csharp
public AudioStreamMicrophone()
```
