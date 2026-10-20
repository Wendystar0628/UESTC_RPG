<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRActionSet

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Action sets in OpenXR define a collection of actions that can be activated in unison. This allows games to easily change between different states that require different inputs or need to reinterpret inputs. For instance we could have an action set that is active when a menu is open, an action set that is active when the player is freely walking around and an action set that is active when the player is controlling a vehicle.

Action sets can contain the same action with the same name, if such action sets are active at the same time the action set with the highest priority defines which binding is active.

## 构造函数

### OpenXRActionSet

```csharp
public OpenXRActionSet()
```

## 属性

### Actions

```csharp
public Array Actions { get; set; }
```

引擎名：`actions`

Collection of actions for this action set.

### LocalizedName

```csharp
public string LocalizedName { get; set; }
```

引擎名：`localized_name`

The localized name of this action set.

### Priority

```csharp
public int Priority { get; set; }
```

引擎名：`priority`

The priority for this action set.

## 方法

### AddAction

```csharp
public void AddAction(OpenXRAction action)
```

引擎名：`add_action`

Add an action to this action set.

### GetActionCount

```csharp
public int GetActionCount()
```

引擎名：`get_action_count`

Retrieve the number of actions in our action set.

### GetActions

```csharp
public Array GetActions()
```

引擎名：`get_actions`

### GetLocalizedName

```csharp
public string GetLocalizedName()
```

引擎名：`get_localized_name`

### GetPriority

```csharp
public int GetPriority()
```

引擎名：`get_priority`

### RemoveAction

```csharp
public void RemoveAction(OpenXRAction action)
```

引擎名：`remove_action`

Remove an action from this action set.

### SetActions

```csharp
public void SetActions(Array actions)
```

引擎名：`set_actions`

### SetLocalizedName

```csharp
public void SetLocalizedName(string localizedName)
```

引擎名：`set_localized_name`

### SetPriority

```csharp
public void SetPriority(int priority)
```

引擎名：`set_priority`
