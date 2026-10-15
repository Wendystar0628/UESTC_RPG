<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ISerializationListener

命名空间 `Godot`；interface

Allows a GodotObject to react to the serialization/deserialization
that occurs when Godot reloads assemblies.

## 方法

### OnAfterDeserialize

```csharp
public void OnAfterDeserialize()
```

Executed after deserializing this instance's state after reloading assemblies.
Restore any state that has been lost.

### OnBeforeSerialize

```csharp
public void OnBeforeSerialize()
```

Executed before serializing this instance's state when reloading assemblies.
Clear any data that should not be serialized.
