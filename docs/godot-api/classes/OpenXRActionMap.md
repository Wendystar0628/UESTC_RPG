<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRActionMap

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

OpenXR uses an action system similar to Godots Input map system to bind inputs and outputs on various types of XR controllers to named actions. OpenXR specifies more detail on these inputs and outputs than Godot supports.

Another important distinction is that OpenXR offers no control over these bindings. The bindings we register are suggestions, it is up to the XR runtime to offer users the ability to change these bindings. This allows the XR runtime to fill in the gaps if new hardware becomes available.

The action map therefore needs to be loaded at startup and can't be changed afterwards. This resource is a container for the entire action map.

## 构造函数

### OpenXRActionMap

```csharp
public OpenXRActionMap()
```

## 属性

### ActionSets

```csharp
public Array ActionSets { get; set; }
```

引擎名：`action_sets`

Collection of [`OpenXRActionSet`](OpenXRActionSet.md)s that are part of this action map.

### InteractionProfiles

```csharp
public Array InteractionProfiles { get; set; }
```

引擎名：`interaction_profiles`

Collection of [`OpenXRInteractionProfile`](OpenXRInteractionProfile.md)s that are part of this action map.

## 方法

### AddActionSet

```csharp
public void AddActionSet(OpenXRActionSet actionSet)
```

引擎名：`add_action_set`

Add an action set.

### AddInteractionProfile

```csharp
public void AddInteractionProfile(OpenXRInteractionProfile interactionProfile)
```

引擎名：`add_interaction_profile`

Add an interaction profile.

### CreateDefaultActionSets

```csharp
public void CreateDefaultActionSets()
```

引擎名：`create_default_action_sets`

Setup this action set with our default actions.

### FindActionSet

```csharp
public OpenXRActionSet FindActionSet(string name)
```

引擎名：`find_action_set`

Retrieve an action set by name.

### FindInteractionProfile

```csharp
public OpenXRInteractionProfile FindInteractionProfile(string name)
```

引擎名：`find_interaction_profile`

Find an interaction profile by its name (path).

### GetActionSet

```csharp
public OpenXRActionSet GetActionSet(int idx)
```

引擎名：`get_action_set`

Retrieve the action set at this index.

### GetActionSetCount

```csharp
public int GetActionSetCount()
```

引擎名：`get_action_set_count`

Retrieve the number of actions sets in our action map.

### GetActionSets

```csharp
public Array GetActionSets()
```

引擎名：`get_action_sets`

### GetInteractionProfile

```csharp
public OpenXRInteractionProfile GetInteractionProfile(int idx)
```

引擎名：`get_interaction_profile`

Get the interaction profile at this index.

### GetInteractionProfileCount

```csharp
public int GetInteractionProfileCount()
```

引擎名：`get_interaction_profile_count`

Retrieve the number of interaction profiles in our action map.

### GetInteractionProfiles

```csharp
public Array GetInteractionProfiles()
```

引擎名：`get_interaction_profiles`

### RemoveActionSet

```csharp
public void RemoveActionSet(OpenXRActionSet actionSet)
```

引擎名：`remove_action_set`

Remove an action set.

### RemoveInteractionProfile

```csharp
public void RemoveInteractionProfile(OpenXRInteractionProfile interactionProfile)
```

引擎名：`remove_interaction_profile`

Remove an interaction profile.

### SetActionSets

```csharp
public void SetActionSets(Array actionSets)
```

引擎名：`set_action_sets`

### SetInteractionProfiles

```csharp
public void SetInteractionProfiles(Array interactionProfiles)
```

引擎名：`set_interaction_profiles`
